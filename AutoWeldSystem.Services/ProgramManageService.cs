using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;
using SqlSugar;

namespace AutoWeldSystem.Services;

/// <summary>
/// 加工程序管理服务。
/// 本服务采用“轻量 Git 化”思路：每次保存生成一次本地提交和快照，MES 同步状态独立维护。
/// </summary>
public sealed class ProgramManageService : IProgramManageService
{
    private const int MaxLocalProgramCount = 256;

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly IOperationLogService _operationLogService;
    private AppSettings _currentSettings;

    public ProgramManageService(
        SqlSugarDbContext dbContext,
        IAppSettingsService settingsService,
        IMesProvider mesProvider,
        IOperationLogService operationLogService)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _currentSettings = settingsService.Get();
        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
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

    public string BuildProgramName(string productNum, string componentCode, int sequenceNumber, string? description = null)
    {
        return ProgramNameRules.BuildProgramName(
            CurrentSettings.DeviceId,
            componentCode,
            sequenceNumber,
            productNum,
            description);
    }

    public async Task<BizProgram> SaveAsync(SaveProgramReq request, bool syncNow, CancellationToken cancellationToken = default)
    {
        var result = await SaveWithSyncDecisionAsync(request, cancellationToken);
        var entity = result.Program;

        if (syncNow && result.ShouldSyncNow)
        {
            await SyncProgramAsync(entity.Id, cancellationToken);
            entity = _dbContext.Db.Queryable<BizProgram>().InSingle(entity.Id);
        }

        return entity;
    }

    public Task<SaveProgramResult> SaveWithSyncDecisionAsync(SaveProgramReq request, CancellationToken cancellationToken = default)
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

        var original = entity.Id > 0 ? CloneProgram(entity) : null;
        var hadPendingAction = ProgramMesSyncRules.HasPendingSyncAction(entity);
        ApplyRequest(entity, request);
        if (!string.IsNullOrWhiteSpace(request.MesRemark))
        {
            // 用户显式填写 MES 备注时才覆盖；空值由真实同步动作兜底。
            entity.Remark = request.MesRemark;
        }

        var currentSaveSyncAction = ProgramMesSyncRules.ResolveCurrentSaveAction(original, entity);
        var syncAction = ResolveSaveSyncAction(original, entity, hadPendingAction);
        var commitMessage = ResolveSaveCommitMessage(request.MesRemark, currentSaveSyncAction);
        entity.VersionNumber = entity.Id == 0 ? 1 : entity.VersionNumber + 1;
        entity.CommitId = CreateCommitId(entity, commitMessage);
        entity.CommitMessage = commitMessage;
        ApplySaveSyncState(entity, syncAction, currentSaveSyncAction, request.MesRemark, original);
        entity.UpdatedTime = DateTime.Now;

        entity = entity.Id == 0
            ? _dbContext.Db.Insertable(entity).ExecuteReturnEntity()
            : UpdateAndReturn(entity);

        AddRevision(entity, commitMessage);
        _operationLogService.Write("ProgramSave", $"保存程序：{entity.ProgramName}，版本：v{entity.VersionNumber}");

        return Task.FromResult(new SaveProgramResult
        {
            Program = entity,
            CurrentSaveSyncAction = currentSaveSyncAction
        });
    }

    public async Task DeleteAsync(int id, bool syncNow, string? remarkOverride = null, CancellationToken cancellationToken = default)
    {
        _dbContext.InitDatabase();

        var entity = _dbContext.Db.Queryable<BizProgram>().InSingle(id);
        if (entity is null)
        {
            throw new InvalidOperationException("程序不存在，无法删除。");
        }

        entity.IsDeleted = true;
        entity.VersionNumber++;
        var deleteRemark = ProgramRemarkRules.ResolveForAction(remarkOverride, AppConstants.ProgramSyncActions.Delete);
        entity.Remark = deleteRemark;
        entity.CommitMessage = deleteRemark;
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
            var executableAction = ProgramMesSyncRules.ResolveExecutableSyncAction(entity.SyncAction, entity.ProgramId);
            if (string.IsNullOrWhiteSpace(executableAction))
            {
                if (string.IsNullOrWhiteSpace(entity.SyncAction))
                {
                    return;
                }

                throw new InvalidOperationException("缺少 MES 程序ID，无法执行当前程序同步动作。");
            }

            var responseMessage = executableAction switch
            {
                AppConstants.ProgramSyncActions.Delete => await SyncDeleteAsync(entity, cancellationToken),
                AppConstants.ProgramSyncActions.Update => await SyncUpdateAsync(entity, cancellationToken),
                AppConstants.ProgramSyncActions.Create => await SyncCreateAsync(entity, cancellationToken),
                _ => throw new InvalidOperationException($"未知程序同步动作：{entity.SyncAction}")
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

    public async Task<int> PullFromMesAsync(CancellationToken cancellationToken = default)
    {
        _dbContext.InitDatabase();

        var settings = CurrentSettings;
        var listResponse = await _mesProvider.GetProgramListAsync(settings.DeviceId, null, cancellationToken);
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

    private void ApplyRequest(BizProgram entity, SaveProgramReq request)
    {
        var settings = CurrentSettings;
        var previousProgramFilePath = entity.Id > 0
            ? ProgramFileRules.BuildFilePath(settings.ProgramFileDirectory, entity.RecipeCode, entity.ProgramName)
            : null;

        entity.ProgramName = entity.Id == 0
            ? BuildProgramName(request.ProductNum, request.ComponentCode, request.SequenceNumber, request.LocalRemark)
            : string.IsNullOrWhiteSpace(request.ProgramName)
                ? entity.ProgramName
                : request.ProgramName;
        entity.ProductNum = request.ProductNum;
        entity.RecipeCode = request.RecipeCode;
        entity.ComponentCode = request.ComponentCode;
        entity.SequenceNumber = Math.Max(1, request.SequenceNumber);
        if (entity.Id == 0)
        {
            entity.DeviceId = settings.DeviceId;
        }

        entity.ProgramType = string.IsNullOrWhiteSpace(request.ProgramType) ? "0" : request.ProgramType;
        entity.ProgramContent = string.IsNullOrWhiteSpace(request.ProgramContentJson) ? "{}" : request.ProgramContentJson.Trim();
        entity.Description = request.LocalRemark;
        entity.IsDeleted = false;

        if (ProgramContentJsonRules.HasConfiguredValues(entity.ProgramContent))
        {
            WriteProgramContentFile(entity, settings);
        }
        else
        {
            ClearProgramContentFile(entity, settings, previousProgramFilePath);
        }

        if (entity.Id == 0)
        {
            entity.CreatedTime = DateTime.Now;
        }
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        Interlocked.Exchange(ref _currentSettings, e.CurrentSettings);
    }

    private static string? ResolveSaveSyncAction(BizProgram? original, BizProgram entity, bool hadPendingAction)
    {
        return ProgramMesSyncRules.ResolveSaveAction(original, entity, hadPendingAction);
    }

    private static string ResolveSaveCommitMessage(string? mesRemark, string? syncAction)
    {
        if (!string.IsNullOrWhiteSpace(mesRemark))
        {
            return mesRemark.Trim();
        }

        return string.IsNullOrWhiteSpace(syncAction)
            ? "本地保存"
            : ProgramRemarkRules.ResolveForAction(null, syncAction);
    }

    private static void ApplySaveSyncState(
        BizProgram entity,
        string? syncAction,
        string? currentSaveSyncAction,
        string? mesRemark,
        BizProgram? original)
    {
        if (string.IsNullOrWhiteSpace(syncAction))
        {
            entity.SyncAction = null;
            entity.SyncStatus = original?.SyncStatus ?? AppConstants.ProgramSyncStatus.Synced;
            entity.SyncMessage = "本地辅助字段已保存，无需同步至 MES。";
            return;
        }

        if (string.IsNullOrWhiteSpace(currentSaveSyncAction))
        {
            // 本次只改本地字段时，保留历史待同步动作，但不把它当作本次保存触发的同步。
            entity.SyncAction = syncAction;
            entity.SyncStatus = original?.SyncStatus ?? entity.SyncStatus;
            entity.SyncMessage = original?.SyncMessage ?? entity.SyncMessage;
            return;
        }

        entity.SyncAction = syncAction;
        entity.Remark = ProgramRemarkRules.ResolveForAction(mesRemark, syncAction);
        entity.SyncStatus = syncAction switch
        {
            AppConstants.ProgramSyncActions.Create => AppConstants.ProgramSyncStatus.PendingCreate,
            AppConstants.ProgramSyncActions.Delete => AppConstants.ProgramSyncStatus.PendingDelete,
            _ => AppConstants.ProgramSyncStatus.PendingUpdate
        };
        entity.SyncMessage = "本地已保存，等待同步至 MES。";
    }

    private static BizProgram CloneProgram(BizProgram source)
    {
        return new BizProgram
        {
            Id = source.Id,
            ProgramId = source.ProgramId,
            ProgramName = source.ProgramName,
            DeviceId = source.DeviceId,
            ProgramContent = source.ProgramContent,
            ProgramType = source.ProgramType,
            ProductNum = source.ProductNum,
            ProgramFile = source.ProgramFile,
            Remark = source.Remark,
            RecipeCode = source.RecipeCode,
            ProductModel = source.ProductModel,
            ComponentCode = source.ComponentCode,
            SequenceNumber = source.SequenceNumber,
            ProgramFileName = source.ProgramFileName,
            Description = source.Description,
            VersionNumber = source.VersionNumber,
            CommitId = source.CommitId,
            CommitMessage = source.CommitMessage,
            SyncStatus = source.SyncStatus,
            SyncAction = source.SyncAction,
            SyncMessage = source.SyncMessage,
            LastSyncTime = source.LastSyncTime,
            IsDeleted = source.IsDeleted,
            CreatedTime = source.CreatedTime,
            UpdatedTime = source.UpdatedTime
        };
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

    private static void WriteProgramContentFile(BizProgram entity, AppSettings settings)
    {
        var json = string.IsNullOrWhiteSpace(entity.ProgramContent) ? "{}" : entity.ProgramContent.Trim();
        var filePath = ProgramFileRules.BuildFilePath(
            settings.ProgramFileDirectory,
            entity.RecipeCode,
            entity.ProgramName);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        entity.ProgramContent = json;
        entity.ProgramFile = ProgramFileRules.EncodeJsonToBase64(json);
        entity.ProgramFileName = Path.GetFileName(filePath);
    }

    /// <summary>
    /// 删除当前程序及其改名前自动生成的 JSON 文件，并清空实体中的文件关联信息。
    /// </summary>
    private static void ClearProgramContentFile(
        BizProgram entity,
        AppSettings settings,
        string? previousProgramFilePath)
    {
        var currentProgramFilePath = ProgramFileRules.BuildFilePath(
            settings.ProgramFileDirectory,
            entity.RecipeCode,
            entity.ProgramName);
        DeleteProgramContentFile(currentProgramFilePath);

        if (!string.IsNullOrWhiteSpace(previousProgramFilePath)
            && !string.Equals(previousProgramFilePath, currentProgramFilePath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteProgramContentFile(previousProgramFilePath);
        }

        entity.ProgramFile = string.Empty;
        entity.ProgramFileName = string.Empty;
    }

    /// <summary>
    /// 删除单个系统自动生成的程序内容文件；文件不存在时视为已清理。
    /// </summary>
    private static void DeleteProgramContentFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
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
            RecipeCode = entity.RecipeCode,
            ProgramContentJson = entity.ProgramContent,
            LocalRemark = entity.Description,
            ProgramFileBase64 = entity.ProgramFile,
            UserNumber = user?.UserNumber ?? "system",
            UserName = user?.UserName ?? "system",
            CreatedTime = DateTime.Now
        };

        _dbContext.Db.Insertable(revision).ExecuteCommand();
    }

    private async Task<string> SyncCreateAsync(BizProgram entity, CancellationToken cancellationToken)
    {
        var request = ProgramMesPayloadRules.ToCreateRequest(
            entity,
            ProgramRemarkRules.ResolveForAction(entity.Remark, AppConstants.ProgramSyncActions.Create));
        var response = await _mesProvider.AddExpProgramAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(response.Msg);
        }

        entity.Remark = request.Remark;
        entity.ProgramId = response.Data?.Id ?? entity.ProgramId;
        return "新增程序已同步至 MES。";
    }

    private async Task<string> SyncUpdateAsync(BizProgram entity, CancellationToken cancellationToken)
    {
        var request = ProgramMesPayloadRules.ToWriteRequest(
            entity,
            ProgramRemarkRules.ResolveForAction(entity.Remark, AppConstants.ProgramSyncActions.Update));
        var response = await _mesProvider.UpdateExpProgramAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(response.Msg);
        }

        entity.Remark = request.Remark;

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

    private void UpsertRemoteProgram(ProgramDataRes data)
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
        entity.ProgramContent = data.ProgramContent;
        entity.ProgramType = data.ProgramType;
        entity.ProductNum = data.ProductNum;
        if (ProgramNameRules.TryParse(data.ProgramName, out var parsedName))
        {
            entity.ComponentCode = parsedName.ComponentCode;
            entity.SequenceNumber = parsedName.SequenceNumber;
            entity.Description = parsedName.Description;
        }
        else
        {
            if (ProgramNameRules.TryExtractComponentCode(data.ProgramName, out var componentCode))
            {
                entity.ComponentCode = componentCode;
            }

            if (entity.SequenceNumber <= 0)
            {
                entity.SequenceNumber = 1;
            }

            entity.Description = string.Empty;
        }

        entity.ProgramFile = data.ProgramFile;
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

    private static ProgramDataRes ToMesProgramData(BizProgram entity)
    {
        return new ProgramDataRes
        {
            Id = entity.ProgramId ?? string.Empty,
            ProgramName = entity.ProgramName,
            DeviceId = entity.DeviceId,
            ProgramContent = entity.ProgramContent ?? string.Empty,
            ProgramType = entity.ProgramType,
            ProductNum = entity.ProductNum,
            ProgramFile = entity.ProgramFile ?? string.Empty,
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

    private static void NormalizeRequest(SaveProgramReq request)
    {
        request.ProgramName = request.ProgramName.Trim();
        request.ProductNum = request.ProductNum.Trim();
        request.RecipeCode = request.RecipeCode.Trim();
        request.ComponentCode = request.ComponentCode.Trim();
        request.ProgramType = request.ProgramType.Trim();
        request.ProgramContentJson = request.ProgramContentJson.Trim();
        request.ProgramFilePath = request.ProgramFilePath.Trim();
        request.WeldJobName = request.WeldJobName.Trim();
        request.RobotJobName = request.RobotJobName.Trim();
        request.MesRemark = request.MesRemark.Trim();
        request.LocalRemark = request.LocalRemark.Trim();

        if (string.IsNullOrWhiteSpace(request.ProductNum))
        {
            throw new InvalidOperationException("产品工号不能为空。");
        }

        if (string.IsNullOrWhiteSpace(request.ComponentCode))
        {
            throw new InvalidOperationException("零组件代码不能为空。");
        }
    }

    private static string CreateCommitId(BizProgram entity, string? commitMessage)
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            entity.ProgramName,
            entity.ProductNum,
            entity.RecipeCode,
            LocalRemark = entity.Description,
            entity.ProgramContent,
            entity.ProgramFile,
            entity.VersionNumber,
            commitMessage,
            Timestamp = DateTime.Now.Ticks
        });

        return CreateHash(snapshot);
    }

    private static string CreateCommitId(ProgramDataRes data)
    {
        return CreateHash(JsonSerializer.Serialize(data) + DateTime.Now.Ticks);
    }

    private static string CreateHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }
}
