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
    public event EventHandler? ProgramLookupsChanged;
    private const int MaxLocalProgramCount = 256;

    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly IMesProvider _mesProvider;
    private readonly IOperationLogService _operationLogService;
    private readonly SemaphoreSlim _mutationGate = new(1, 1);
    private readonly SemaphoreSlim _lookupGate = new(1, 1);
    private ProgramLookup[]? _programLookupSnapshot;
    private long _programLookupVersion;
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
            .ToArray();
    }

    // 列表查询不参与程序变更门锁：查询与删除互斥会在“删除后立即刷新”的链路上形成互相等待。
    public Task<IReadOnlyList<BizProgram>> GetProgramsAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var programs = GetPrograms(includeDeleted);
                cancellationToken.ThrowIfCancellationRequested();
                return programs;
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProgramLookup>> GetProgramLookupsAsync(
        CancellationToken cancellationToken = default)
    {
        var cached = Volatile.Read(ref _programLookupSnapshot);
        if (cached is not null)
        {
            return cached;
        }

        await _lookupGate.WaitAsync(cancellationToken);
        try
        {
            while (true)
            {
                cached = Volatile.Read(ref _programLookupSnapshot);
                if (cached is not null)
                {
                    return cached;
                }

                var version = Volatile.Read(ref _programLookupVersion);
                var loaded = await Task.Run(
                    () => QueryProgramLookups(cancellationToken),
                    CancellationToken.None);
                if (version != Volatile.Read(ref _programLookupVersion))
                {
                    continue;
                }

                Volatile.Write(ref _programLookupSnapshot, loaded);
                return loaded;
            }
        }
        finally
        {
            _lookupGate.Release();
        }
    }

    public Task<BizProgram?> GetProgramAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _dbContext.InitDatabase();
                return (BizProgram?)_dbContext.Db.Queryable<BizProgram>().InSingle(id);
            },
            cancellationToken);
    }

    private ProgramLookup[] QueryProgramLookups(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dbContext.InitDatabase();
        return _dbContext.Db.Queryable<BizProgram>()
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.UpdatedTime, OrderByType.Desc)
            .Select(it => new ProgramLookup
            {
                Id = it.Id,
                ProgramId = it.ProgramId,
                ProgramName = it.ProgramName,
                DeviceId = it.DeviceId,
                ProductNum = it.ProductNum,
                ProductModel = it.ProductModel,
                RecipeCode = it.RecipeCode,
                Station2RecipeCode = it.Station2RecipeCode,
                ComponentCode = it.ComponentCode,
                ProgramType = it.ProgramType,
                SequenceNumber = it.SequenceNumber,
                Description = it.Description,
                VersionNumber = it.VersionNumber,
                SyncStatus = it.SyncStatus,
                UpdatedTime = it.UpdatedTime
            })
            .ToArray();
    }

    private void InvalidateProgramLookups()
    {
        Interlocked.Increment(ref _programLookupVersion);
        Volatile.Write(ref _programLookupSnapshot, null);
        ProgramLookupsChanged?.Invoke(this, EventArgs.Empty);
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
            .ToArray();
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

    public int GetNextSequenceNumber(string productNum)
    {
        _dbContext.InitDatabase();

        var normalized = productNum?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return 1;
        }

        var maxSequence = _dbContext.Db.Queryable<BizProgram>()
            .Where(it => !it.IsDeleted && it.ProductNum == normalized)
            .Max(it => (int?)it.SequenceNumber) ?? 0;
        return Math.Max(1, maxSequence + 1);
    }

    public Task<int> GetNextSequenceNumberAsync(
        string productNum,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return GetNextSequenceNumber(productNum);
            },
            cancellationToken);
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

    public async Task<SaveProgramResult> SaveWithSyncDecisionAsync(
        SaveProgramReq request,
        CancellationToken cancellationToken = default)
    {
        await _mutationGate.WaitAsync(cancellationToken);
        try
        {
            var result = await Task.Run(
                () => SaveWithSyncDecisionCore(request, cancellationToken),
                CancellationToken.None);
            InvalidateProgramLookups();
            return result;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    private SaveProgramResult SaveWithSyncDecisionCore(
        SaveProgramReq request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

        return new SaveProgramResult
        {
            Program = entity,
            CurrentSaveSyncAction = currentSaveSyncAction
        };
    }

    public async Task<ProgramDeleteResult> DeleteLocalAsync(
        int id,
        string? remarkOverride = null,
        CancellationToken cancellationToken = default)
    {
        await _mutationGate.WaitAsync(cancellationToken);
        try
        {
            var result = await Task.Run(
                () => DeleteLocalCore(id, remarkOverride, cancellationToken),
                CancellationToken.None);
            InvalidateProgramLookups();
            return result;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    public async Task DeleteAsync(int id, bool syncNow, string? remarkOverride = null, CancellationToken cancellationToken = default)
    {
        var result = await DeleteLocalAsync(id, remarkOverride, cancellationToken);
        if (syncNow && result.RequiresMesSync)
        {
            await SyncProgramAsync(id, cancellationToken);
        }
    }

    private ProgramDeleteResult DeleteLocalCore(int id, string? remarkOverride, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

        return new ProgramDeleteResult
        {
            Id = entity.Id,
            ProgramName = entity.ProgramName,
            RequiresMesSync = entity.SyncStatus == AppConstants.ProgramSyncStatus.PendingDelete
        };
    }

    public async Task SyncProgramAsync(int id, CancellationToken cancellationToken = default)
    {
        await _mutationGate.WaitAsync(cancellationToken);
        try
        {
            await Task.Run(
                () => SyncProgramCoreAsync(id, cancellationToken),
                CancellationToken.None);
            InvalidateProgramLookups();
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    private async Task SyncProgramCoreAsync(int id, CancellationToken cancellationToken)
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
        InvalidateProgramLookups();
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
        var previousDescription = entity.Description?.Trim() ?? string.Empty;
        var currentDescription = request.LocalRemark.Trim();
        var descriptionChanged = !string.Equals(
            previousDescription,
            currentDescription,
            StringComparison.Ordinal);
        // 程序名称由工号、部件图号、流水号和程序备注拼成，任一变化都必须重算，
        // 否则会出现流水号已改、名称仍是旧值的名实不符。
        // 注意：名称是 MES 上传字段，因此改流水号会经名称间接触发一次 MES 更新。
        var nameInputsChanged = entity.Id > 0
            && (!string.Equals(entity.ProductNum?.Trim(), request.ProductNum, StringComparison.Ordinal)
                || !string.Equals(entity.ComponentCode?.Trim(), request.ComponentCode, StringComparison.Ordinal)
                || entity.SequenceNumber != Math.Max(1, request.SequenceNumber));

        entity.ProgramName = entity.Id == 0 || descriptionChanged || nameInputsChanged
            ? BuildProgramName(request.ProductNum, request.ComponentCode, request.SequenceNumber, request.LocalRemark)
            : string.IsNullOrWhiteSpace(request.ProgramName)
                ? entity.ProgramName
                : request.ProgramName;
        EnsureProgramNameNotDuplicated(entity);
        entity.ProductNum = request.ProductNum;
        entity.RecipeCode = request.RecipeCode;
        entity.Station2RecipeCode = request.Station2RecipeCode;
        entity.ComponentCode = request.ComponentCode;
        entity.SequenceNumber = Math.Max(1, request.SequenceNumber);
        if (entity.Id == 0)
        {
            entity.DeviceId = settings.DeviceId;
        }

        entity.ProgramType = string.IsNullOrWhiteSpace(request.ProgramType) ? "0" : request.ProgramType;
        entity.ProgramContent = string.IsNullOrWhiteSpace(request.ProgramContentJson) ? "{}" : request.ProgramContentJson.Trim();
        entity.Description = currentDescription;
        entity.IsDeleted = false;

        if (entity.Id == 0)
        {
            entity.CreatedTime = DateTime.Now;
        }
    }

    private AppSettings CurrentSettings => Volatile.Read(ref _currentSettings);

    /// <summary>
    /// 阻止保存出同名程序。
    /// 程序 JSON 文件仅按程序名命名，重名会互相覆盖，且删除其中一个会连带删掉幸存者的文件。
    /// </summary>
    private void EnsureProgramNameNotDuplicated(BizProgram entity)
    {
        var duplicated = _dbContext.Db.Queryable<BizProgram>()
            .Any(it => it.ProgramName == entity.ProgramName && it.Id != entity.Id && !it.IsDeleted);
        if (duplicated)
        {
            throw new InvalidOperationException($"已存在同名程序：{entity.ProgramName}，请调整流水号或程序备注。");
        }
    }

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
            Station2RecipeCode = source.Station2RecipeCode,
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
            Station2RecipeCode = entity.Station2RecipeCode,
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

    private void NormalizeRequest(SaveProgramReq request)
    {
        request.ProgramName = request.ProgramName.Trim();
        request.ProductNum = request.ProductNum.Trim();
        request.RecipeCode = ProgramRecipeMappingRules.Normalize(request.RecipeCode);
        request.Station2RecipeCode = ProgramRecipeMappingRules.Normalize(request.Station2RecipeCode);
        request.ComponentCode = request.ComponentCode.Trim();
        request.ProgramType = request.ProgramType.Trim();
        request.ProgramContentJson = request.ProgramContentJson.Trim();
        request.WeldJobName = request.WeldJobName.Trim();
        request.RobotJobName = request.RobotJobName.Trim();
        request.MesRemark = request.MesRemark.Trim();
        request.LocalRemark = request.LocalRemark.Trim();

        ProgramSaveRecipeRules.Validate(
            request.RecipeCode,
            request.Station2RecipeCode,
            CurrentSettings.EnableDualStation);

        if (string.IsNullOrWhiteSpace(request.ProductNum))
        {
            throw new InvalidOperationException("产品工号不能为空。");
        }

        if (string.IsNullOrWhiteSpace(request.ComponentCode))
        {
            throw new InvalidOperationException("部件图号不能为空。");
        }
    }

    private static string CreateCommitId(BizProgram entity, string? commitMessage)
    {
        var snapshot = JsonSerializer.Serialize(new
        {
            entity.ProgramName,
            entity.ProductNum,
            entity.RecipeCode,
            entity.Station2RecipeCode,
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

    /// <summary>
    /// 更新所有本地程序的设备编号，用于设备编号变更后统一修正历史程序。
    /// 同时将处于同步失败或等待状态的程序标记为待更新，保证下次同步使用新设备编号。
    /// </summary>
    public Task UpdateAllProgramsDeviceIdAsync(string newDeviceId)
    {
        _dbContext.InitDatabase();
        var normalized = newDeviceId.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.CompletedTask;
        }

        var programs = _dbContext.Db.Queryable<BizProgram>()
            .Where(it => !it.IsDeleted)
            .ToArray();

        foreach (var p in programs)
        {
            if (string.Equals(p.DeviceId, normalized, StringComparison.Ordinal))
            {
                continue;
            }

            p.DeviceId = normalized;
            p.UpdatedTime = DateTime.Now;
            _dbContext.Db.Updateable(p).UpdateColumns(it => new { it.DeviceId, it.UpdatedTime }).ExecuteCommand();
        }

        _operationLogService.Write("ProgramDeviceIdUpdate", $"设备编号变更，已将所有程序的设备编号更新为 {normalized}。");
        InvalidateProgramLookups();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量删除指定程序（仅本地软删除，不同步 MES）。
    /// 用于清理因设备编号变更等原因导致无法同步的历史程序。
    /// </summary>
    public async Task<int> BatchDeleteLocalProgramsAsync(
        IEnumerable<int> programIds,
        CancellationToken cancellationToken = default)
    {
        var ids = programIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return 0;
        }

        await _mutationGate.WaitAsync(cancellationToken);
        try
        {
            var result = await Task.Run(
                () => BatchDeleteLocalProgramsCore(ids, cancellationToken),
                CancellationToken.None);
            InvalidateProgramLookups();
            return result;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    private int BatchDeleteLocalProgramsCore(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dbContext.InitDatabase();

        var programs = _dbContext.Db.Queryable<BizProgram>()
            .Where(it => ids.Contains(it.Id))
            .ToList();

        if (programs.Count == 0)
        {
            return 0;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var deletedCount = _dbContext.Db.Deleteable(programs).ExecuteCommand();
        _operationLogService.Write(
            "ProgramBatchDelete",
            $"批量删除程序：{deletedCount} 条");
        return deletedCount;
    }
}
