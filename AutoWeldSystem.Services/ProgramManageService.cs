using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Models;
using AutoWeldSystem.Data;
using SqlSugar;

namespace AutoWeldSystem.Services;

/// <summary>
/// 加工程序管理服务。
/// 本服务采用“轻量 Git 化”思路：每次保存生成一次本地提交和快照，MES 同步状态独立维护。
/// </summary>
public sealed class ProgramManageService : IProgramManageService
{
    private const int MaxLocalProgramCount = 128;

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly IOperationLogService _operationLogService;

    public ProgramManageService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IMesProvider mesProvider,
        IOperationLogService operationLogService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _mesProvider = mesProvider;
        _operationLogService = operationLogService;
    }

    public IReadOnlyList<BizProgram> GetPrograms(bool includeDeleted = false)
    {
        _dbContext.InitDatabase();

        var query = _dbContext.Db.Queryable<BizProgram>();
        if (!includeDeleted)
        {
            query = query.Where(it => !it.IsDeleted);
        }

        return query
            .OrderBy(it => it.UpdatedTime, OrderByType.Desc)
            .ToList();
    }

    public IReadOnlyList<BizProgramRevision> GetRevisions(int programLocalId)
    {
        _dbContext.InitDatabase();

        return _dbContext.Db.Queryable<BizProgramRevision>()
            .Where(it => it.ProgramLocalId == programLocalId)
            .OrderBy(it => it.VersionNumber, OrderByType.Desc)
            .ToList();
    }

    public IReadOnlyList<ProgramSyncSummary> GetPendingSyncPrograms()
    {
        _dbContext.InitDatabase();

        var pendingStatuses = new[]
        {
            AppConstants.ProgramSyncStatus.PendingCreate,
            AppConstants.ProgramSyncStatus.PendingUpdate,
            AppConstants.ProgramSyncStatus.PendingDelete,
            AppConstants.ProgramSyncStatus.Failed
        };

        return _dbContext.Db.Queryable<BizProgram>()
            .Where(it => pendingStatuses.Contains(it.SyncStatus))
            .OrderBy(it => it.UpdatedTime, OrderByType.Desc)
            .ToList()
            .Select(ToSyncSummary)
            .ToList();
    }

    public string BuildProgramName(string productNum, string componentCode, int sequenceNumber)
    {
        var settings = _settingsService.Get();
        var deviceId = NormalizeNamePart(settings.DeviceId);
        var component = NormalizeNamePart(componentCode);
        var product = NormalizeNamePart(productNum.Replace("#", string.Empty));
        var sequence = Math.Max(1, sequenceNumber).ToString("000");

        return $"{deviceId}_CX_{component}_DH_{sequence}_{product}";
    }

    public async Task<BizProgram> SaveAsync(ProgramSaveRequest request, bool syncNow, CancellationToken cancellationToken = default)
    {
        _dbContext.InitDatabase();
        NormalizeRequest(request);

        var entity = request.Id > 0
            ? _dbContext.Db.Queryable<BizProgram>().InSingle(request.Id)
            : new BizProgram();

        if (entity is null)
        {
            throw new InvalidOperationException("程序不存在，无法保存。");
        }

        if (entity.Id == 0 && CountActivePrograms() >= MaxLocalProgramCount)
        {
            throw new InvalidOperationException($"本地程序数量已达到 {MaxLocalProgramCount} 个上限。");
        }

        ApplyRequest(entity, request);
        entity.VersionNumber = entity.Id == 0 ? 1 : entity.VersionNumber + 1;
        entity.CommitId = CreateCommitId(entity, request.CommitMessage);
        entity.CommitMessage = request.CommitMessage;
        entity.SyncAction = string.IsNullOrWhiteSpace(entity.ProgramId)
            ? AppConstants.ProgramSyncActions.Create
            : AppConstants.ProgramSyncActions.Update;
        entity.SyncStatus = entity.SyncAction == AppConstants.ProgramSyncActions.Create
            ? AppConstants.ProgramSyncStatus.PendingCreate
            : AppConstants.ProgramSyncStatus.PendingUpdate;
        entity.SyncMessage = "本地已保存，等待同步至 MES。";
        entity.UpdatedTime = DateTime.Now;

        entity = entity.Id == 0
            ? _dbContext.Db.Insertable(entity).ExecuteReturnEntity()
            : UpdateAndReturn(entity);

        AddRevision(entity, request.CommitMessage);
        _operationLogService.Write("ProgramSave", $"保存程序：{entity.ProgramName}，版本：v{entity.VersionNumber}");

        if (syncNow)
        {
            await SyncProgramAsync(entity.Id, cancellationToken);
            entity = _dbContext.Db.Queryable<BizProgram>().InSingle(entity.Id);
        }

        return entity;
    }

    public async Task DeleteAsync(int id, bool syncNow, CancellationToken cancellationToken = default)
    {
        _dbContext.InitDatabase();

        var entity = _dbContext.Db.Queryable<BizProgram>().InSingle(id);
        if (entity is null)
        {
            throw new InvalidOperationException("程序不存在，无法删除。");
        }

        entity.IsDeleted = true;
        entity.VersionNumber++;
        entity.CommitMessage = "删除程序";
        entity.CommitId = CreateCommitId(entity, entity.CommitMessage);
        entity.UpdatedTime = DateTime.Now;

        if (string.IsNullOrWhiteSpace(entity.ProgramId))
        {
            entity.SyncAction = null;
            entity.SyncStatus = AppConstants.ProgramSyncStatus.Deleted;
            entity.SyncMessage = "本地未同步程序已删除，无需通知 MES。";
        }
        else
        {
            entity.SyncAction = AppConstants.ProgramSyncActions.Delete;
            entity.SyncStatus = AppConstants.ProgramSyncStatus.PendingDelete;
            entity.SyncMessage = "本地已删除，等待同步删除 MES 程序。";
        }

        _dbContext.Db.Updateable(entity).ExecuteCommand();
        AddRevision(entity, entity.CommitMessage);
        _operationLogService.Write("ProgramDelete", $"删除程序：{entity.ProgramName}");

        if (syncNow && entity.SyncStatus == AppConstants.ProgramSyncStatus.PendingDelete)
        {
            await SyncProgramAsync(entity.Id, cancellationToken);
        }
    }

    public async Task SyncProgramAsync(int id, CancellationToken cancellationToken = default)
    {
        _dbContext.InitDatabase();

        var entity = _dbContext.Db.Queryable<BizProgram>().InSingle(id);
        if (entity is null)
        {
            return;
        }

        try
        {
            var responseMessage = entity.SyncAction switch
            {
                AppConstants.ProgramSyncActions.Delete => await SyncDeleteAsync(entity, cancellationToken),
                AppConstants.ProgramSyncActions.Update when !string.IsNullOrWhiteSpace(entity.ProgramId) => await SyncUpdateAsync(entity, cancellationToken),
                _ => await SyncCreateAsync(entity, cancellationToken)
            };

            entity.SyncAction = null;
            entity.SyncStatus = entity.IsDeleted
                ? AppConstants.ProgramSyncStatus.Deleted
                : AppConstants.ProgramSyncStatus.Synced;
            entity.SyncMessage = responseMessage;
            entity.LastSyncTime = DateTime.Now;
            entity.UpdatedTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            entity.SyncStatus = AppConstants.ProgramSyncStatus.Failed;
            entity.SyncMessage = ex.Message;
            entity.LastSyncTime = DateTime.Now;
        }

        _dbContext.Db.Updateable(entity).ExecuteCommand();
    }

    public async Task SyncAllPendingAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in GetPendingSyncPrograms())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SyncProgramAsync(item.Id, cancellationToken);
        }
    }

    public async Task<int> PullFromMesAsync(string? productNum = null, CancellationToken cancellationToken = default)
    {
        _dbContext.InitDatabase();

        var settings = _settingsService.Get();
        var queryProductNum = settings.UseProductNumberFilter ? productNum : null;
        var listResponse = await _mesProvider.GetProgramListAsync(settings.DeviceId, queryProductNum, cancellationToken);
        if (!listResponse.IsSuccess || listResponse.Data is null)
        {
            throw new InvalidOperationException(listResponse.Msg);
        }

        var count = 0;
        foreach (var item in listResponse.Data)
        {
            var detailResponse = await _mesProvider.DownloadProgramAsync(settings.DeviceId, item.Id, cancellationToken);
            if (!detailResponse.IsSuccess || detailResponse.Data is null)
            {
                continue;
            }

            UpsertRemoteProgram(detailResponse.Data);
            count++;
        }

        _operationLogService.Write("ProgramPull", $"从 MES 下载程序 {count} 个。");
        return count;
    }

    private int CountActivePrograms()
    {
        return _dbContext.Db.Queryable<BizProgram>().Count(it => !it.IsDeleted);
    }

    private BizProgram UpdateAndReturn(BizProgram entity)
    {
        _dbContext.Db.Updateable(entity).ExecuteCommand();
        return _dbContext.Db.Queryable<BizProgram>().InSingle(entity.Id);
    }

    private void ApplyRequest(BizProgram entity, ProgramSaveRequest request)
    {
        var settings = _settingsService.Get();
        var fileBytes = GetProgramFileBytes(request.ProgramFilePath);

        entity.ProgramName = string.IsNullOrWhiteSpace(request.ProgramName)
            ? BuildProgramName(request.ProductNum, request.ComponentCode, request.SequenceNumber)
            : request.ProgramName;
        entity.ProductNum = request.ProductNum;
        entity.ProductModel = request.ProductModel;
        entity.ComponentCode = request.ComponentCode;
        entity.SequenceNumber = Math.Max(1, request.SequenceNumber);
        entity.DeviceId = settings.DeviceId;
        entity.ProgramType = string.IsNullOrWhiteSpace(request.ProgramType) ? "0" : request.ProgramType;
        entity.ProgramContentJson = request.ProgramContentJson;
        entity.WeldJobName = request.WeldJobName;
        entity.RobotJobName = request.RobotJobName;
        entity.CycleTimeSeconds = request.CycleTimeSeconds;
        entity.Remark = request.Remark;
        entity.IsDeleted = false;

        if (fileBytes is not null)
        {
            entity.ProgramFileBase64 = Convert.ToBase64String(fileBytes);
            entity.ProgramFileName = Path.GetFileName(request.ProgramFilePath);
            entity.ProgramType = "1";
        }

        if (entity.Id == 0)
        {
            entity.CreatedTime = DateTime.Now;
        }
    }

    private static byte[]? GetProgramFileBytes(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        return File.Exists(filePath)
            ? File.ReadAllBytes(filePath)
            : throw new FileNotFoundException("程序文件不存在。", filePath);
    }

    private void AddRevision(BizProgram entity, string? commitMessage)
    {
        var user = GlobalContext.CurrentUser;
        var revision = new BizProgramRevision
        {
            ProgramLocalId = entity.Id,
            ProgramId = entity.ProgramId,
            VersionNumber = entity.VersionNumber,
            CommitId = entity.CommitId ?? string.Empty,
            CommitMessage = commitMessage,
            ProgramName = entity.ProgramName,
            ProductNum = entity.ProductNum,
            ProgramContentJson = entity.ProgramContentJson,
            ProgramFileBase64 = entity.ProgramFileBase64,
            UserNumber = user?.UserNumber ?? "system",
            UserName = user?.UserName ?? "system",
            CreatedTime = DateTime.Now
        };

        _dbContext.Db.Insertable(revision).ExecuteCommand();
    }

    private async Task<string> SyncCreateAsync(BizProgram entity, CancellationToken cancellationToken)
    {
        var response = await _mesProvider.AddExpProgramAsync(ToMesProgramData(entity), cancellationToken);
        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(response.Msg);
        }

        entity.ProgramId = response.Data?.Id ?? entity.ProgramId;
        return "新增程序已同步至 MES。";
    }

    private async Task<string> SyncUpdateAsync(BizProgram entity, CancellationToken cancellationToken)
    {
        var response = await _mesProvider.UpdateExpProgramAsync(ToMesProgramData(entity), cancellationToken);
        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(response.Msg);
        }

        return "程序更新已同步至 MES。";
    }

    private async Task<string> SyncDeleteAsync(BizProgram entity, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entity.ProgramId))
        {
            return "本地程序未同步过 MES，无需远程删除。";
        }

        var response = await _mesProvider.DeleteExpProgramAsync(entity.DeviceId, entity.ProgramId, cancellationToken);
        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(response.Msg);
        }

        return "程序删除已同步至 MES。";
    }

    private void UpsertRemoteProgram(MesProgramData data)
    {
        var entity = _dbContext.Db.Queryable<BizProgram>().First(it => it.ProgramId == data.Id);
        if (entity is null)
        {
            entity = new BizProgram
            {
                ProgramId = data.Id,
                VersionNumber = 1,
                CommitId = CreateCommitId(data),
                CommitMessage = "从 MES 下载",
                CreatedTime = DateTime.Now
            };
        }

        entity.ProgramName = data.ProgramName;
        entity.DeviceId = data.DeviceId;
        entity.ProgramContentJson = data.ProgramContent;
        entity.ProgramType = data.ProgramType;
        entity.ProductNum = data.ProductNum;
        entity.ProgramFileBase64 = data.ProgramFile;
        entity.Remark = data.Remark;
        entity.SyncAction = null;
        entity.SyncStatus = AppConstants.ProgramSyncStatus.Synced;
        entity.SyncMessage = "已从 MES 下载并保存到本地。";
        entity.LastSyncTime = DateTime.Now;
        entity.IsDeleted = false;
        entity.UpdatedTime = DateTime.Now;

        entity = entity.Id == 0
            ? _dbContext.Db.Insertable(entity).ExecuteReturnEntity()
            : UpdateAndReturn(entity);

        AddRevision(entity, entity.CommitMessage);
    }

    private static MesProgramData ToMesProgramData(BizProgram entity)
    {
        return new MesProgramData
        {
            Id = entity.ProgramId ?? string.Empty,
            ProgramName = entity.ProgramName,
            DeviceId = entity.DeviceId,
            ProgramContent = entity.ProgramContentJson ?? string.Empty,
            ProgramType = entity.ProgramType,
            ProductNum = entity.ProductNum,
            ProgramFile = entity.ProgramFileBase64 ?? string.Empty,
            Remark = entity.Remark ?? string.Empty
        };
    }

    private static ProgramSyncSummary ToSyncSummary(BizProgram entity)
    {
        return new ProgramSyncSummary
        {
            Id = entity.Id,
            ProgramName = entity.ProgramName,
            ProductNum = entity.ProductNum,
            ProgramId = entity.ProgramId ?? string.Empty,
            SyncStatus = entity.SyncStatus,
            SyncAction = entity.SyncAction ?? string.Empty,
            SyncMessage = entity.SyncMessage ?? string.Empty,
            LastSyncTime = entity.LastSyncTime,
            UpdatedTime = entity.UpdatedTime
        };
    }

    private static void NormalizeRequest(ProgramSaveRequest request)
    {
        request.ProgramName = request.ProgramName.Trim();
        request.ProductNum = request.ProductNum.Trim();
        request.ProductModel = request.ProductModel.Trim();
        request.ComponentCode = request.ComponentCode.Trim();
        request.ProgramType = request.ProgramType.Trim();
        request.ProgramContentJson = request.ProgramContentJson.Trim();
        request.ProgramFilePath = request.ProgramFilePath.Trim();
        request.WeldJobName = request.WeldJobName.Trim();
        request.RobotJobName = request.RobotJobName.Trim();
        request.Remark = request.Remark.Trim();
        request.CommitMessage = string.IsNullOrWhiteSpace(request.CommitMessage)
            ? "本地保存"
            : request.CommitMessage.Trim();

        if (string.IsNullOrWhiteSpace(request.ProductNum))
        {
            throw new InvalidOperationException("产品工号不能为空。");
        }

        if (string.IsNullOrWhiteSpace(request.ComponentCode))
        {
            throw new InvalidOperationException("零组件代码不能为空。");
        }
    }

    private static string NormalizeNamePart(string value)
    {
        var chars = value
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '#')
            .ToArray();

        return chars.Length == 0 ? "NA" : new string(chars);
    }

    private static string CreateCommitId(BizProgram entity, string? commitMessage)
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            entity.ProgramName,
            entity.ProductNum,
            entity.ProgramContentJson,
            entity.ProgramFileBase64,
            entity.VersionNumber,
            commitMessage,
            Timestamp = DateTime.Now.Ticks
        });

        return CreateHash(snapshot);
    }

    private static string CreateCommitId(MesProgramData data)
    {
        return CreateHash(JsonSerializer.Serialize(data) + DateTime.Now.Ticks);
    }

    private static string CreateHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }
}
