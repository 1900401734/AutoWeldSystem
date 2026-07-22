using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.DeviceApi;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.ViewModels;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// 设备端远程接口业务实现。
/// 平台下发的设备编号配置只保存本地，不反向调用 MES。
/// </summary>
public sealed class DeviceApiEndpointService : IDeviceApiEndpointService
{
    private readonly IAppSettingsService _settingsService;
    private readonly IDeviceStatusService _deviceStatusService;
    private readonly IOperationLogService _operationLogService;
    private readonly IDeviceLifecycleLogService _deviceLifecycleLogService;

    public DeviceApiEndpointService(
        IAppSettingsService settingsService,
        IDeviceStatusService deviceStatusService,
        IOperationLogService operationLogService,
        IDeviceLifecycleLogService deviceLifecycleLogService)
    {
        _settingsService = settingsService;
        _deviceStatusService = deviceStatusService;
        _operationLogService = operationLogService;
        _deviceLifecycleLogService = deviceLifecycleLogService;
    }

    /// <summary>
    /// 查询当前设备状态，返回 MES 设备状态码。
    /// </summary>
    public BasicRes<DeviceStatusQueryRes> GetDeviceStatus(string? deviceId)
    {
        var settings = _settingsService.Get();
        var currentDeviceId = DeviceApiEndpointRules.NormalizeText(settings.DeviceId);
        BasicRes<DeviceStatusQueryRes> response = Failure<DeviceStatusQueryRes>("设备状态查询未完成");

        try
        {
            if (string.IsNullOrWhiteSpace(currentDeviceId))
            {
                response = Failure<DeviceStatusQueryRes>("本地设备编号未配置");
                return response;
            }

            if (!DeviceApiEndpointRules.IsRequestedDeviceIdAllowed(deviceId, currentDeviceId))
            {
                response = Failure<DeviceStatusQueryRes>("设备编号不匹配");
                return response;
            }

            var currentStatus = _deviceStatusService.GetCurrentStatus();
            if (currentStatus is null)
            {
                response = Failure<DeviceStatusQueryRes>("暂无设备状态记录");
                return response;
            }

            var statusCode = DeviceStatusReportRules.NormalizeMesDeviceStatusCode(currentStatus.DeviceStatus);
            response = Success(new DeviceStatusQueryRes
            {
                DeviceId = currentDeviceId,
                DeviceStatus = statusCode
            });
            return response;
        }
        catch (Exception ex)
        {
            response = Failure<DeviceStatusQueryRes>($"设备状态查询失败：{ex.Message}");
            return response;
        }
        finally
        {
            WriteDeviceStatusQueryLifecycleLog(deviceId, currentDeviceId, response);
        }
    }

    /// <summary>
    /// 保存平台下发的设备配置，并将新设备编号标记为已同步。
    /// </summary>
    public Task<BasicRes<DeviceIdSetRes>> SetDeviceIdAsync(
        AddDeviceReq request,
        CancellationToken cancellationToken = default)
    {
        BasicRes<DeviceIdSetRes> response = Failure<DeviceIdSetRes>("设备编号设置未完成");
        AppSettings? currentSettings = null;
        AppSettings? savedSettings = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(request);

            var newDeviceId = DeviceApiEndpointRules.NormalizeText(request.DeviceId);
            if (string.IsNullOrWhiteSpace(newDeviceId))
            {
                response = Failure<DeviceIdSetRes>("设备编号不能为空");
                return Task.FromResult(response);
            }

            currentSettings = _settingsService.Get();
            if (!DeviceApiEndpointRules.IsKnownOldDeviceId(
                    request.OldDeviceId,
                    currentSettings.DeviceId,
                    currentSettings.MesSyncedDeviceId))
            {
                response = Failure<DeviceIdSetRes>("旧设备编号不匹配");
                return Task.FromResult(response);
            }

            var settingsToSave = BuildUpdatedSettings(currentSettings, request, newDeviceId);
            savedSettings = _settingsService.Save(settingsToSave);
            WriteSetDeviceIdOperationLog(currentSettings, savedSettings, request);

            response = Success(new DeviceIdSetRes
            {
                DeviceId = savedSettings.DeviceId,
                DeviceName = savedSettings.DeviceName,
                DevStatusUrl = DeviceApiEndpointRules.BuildDeviceStatusUrl(
                    savedSettings.DeviceBaseUrl,
                    savedSettings.DeviceId),
                PostDataDomain = DeviceApiEndpointRules.NormalizeBaseUrl(savedSettings.MesBaseUrl)
            });
            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            response = Failure<DeviceIdSetRes>($"设备编号设置失败：{ex.Message}");
            return Task.FromResult(response);
        }
        finally
        {
            WriteSetDeviceIdLifecycleLog(currentSettings, savedSettings, request, response);
        }
    }

    private static AppSettings BuildUpdatedSettings(
        AppSettings currentSettings,
        AddDeviceReq request,
        string newDeviceId)
    {
        var settings = currentSettings.Clone();
        settings.DeviceId = newDeviceId;
        settings.MesSyncedDeviceId = newDeviceId;

        var deviceName = DeviceApiEndpointRules.NormalizeText(request.DeviceName);
        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            settings.DeviceName = deviceName;
        }

        if (DeviceApiEndpointRules.TryExtractBaseUrlFromStatusUrl(request.DevStatusUrl, out var deviceBaseUrl))
        {
            settings.DeviceBaseUrl = deviceBaseUrl;
        }

        var mesBaseUrl = DeviceApiEndpointRules.NormalizeText(request.PostDataDomain);
        if (!string.IsNullOrWhiteSpace(mesBaseUrl))
        {
            settings.MesBaseUrl = DeviceApiEndpointRules.NormalizeBaseUrl(mesBaseUrl);
        }

        return settings;
    }

    private void WriteSetDeviceIdOperationLog(
        AppSettings previousSettings,
        AppSettings savedSettings,
        AddDeviceReq request)
    {
        try
        {
            _operationLogService.Write(
                "DeviceApi.SetDeviceId",
                $"远程设置设备编号，OldDeviceId={request.OldDeviceId}, PreviousDeviceId={previousSettings.DeviceId}, DeviceId={savedSettings.DeviceId}, DeviceName={savedSettings.DeviceName}, DeviceBaseUrl={savedSettings.DeviceBaseUrl}, MesBaseUrl={savedSettings.MesBaseUrl}");
        }
        catch
        {
            // 操作日志失败不应导致平台配置下发失败。
        }
    }

    private void WriteDeviceStatusQueryLifecycleLog(
        string? requestedDeviceId,
        string currentDeviceId,
        BasicRes<DeviceStatusQueryRes> response)
    {
        TryWriteLifecycleLog(DeviceLifecycleLogRules.CreateDeviceApiRemoteAccessEntry(
            currentDeviceId,
            "GET /api/DeviceStatus",
            DeviceApiEndpointRules.NormalizeText(requestedDeviceId),
            currentDeviceId,
            response.Status,
            response.Msg,
            response.IsSuccess,
            DateTime.Now));
    }

    private void WriteSetDeviceIdLifecycleLog(
        AppSettings? previousSettings,
        AppSettings? savedSettings,
        AddDeviceReq request,
        BasicRes<DeviceIdSetRes> response)
    {
        var logDeviceId = savedSettings?.DeviceId
            ?? previousSettings?.DeviceId
            ?? DeviceApiEndpointRules.NormalizeText(request.DeviceId);

        TryWriteLifecycleLog(DeviceLifecycleLogRules.CreateDeviceApiRemoteConfigChangedEntry(
            logDeviceId,
            DeviceApiEndpointRules.NormalizeText(request.OldDeviceId),
            DeviceApiEndpointRules.NormalizeText(request.DeviceId),
            DeviceApiEndpointRules.NormalizeText(request.DeviceName),
            DeviceApiEndpointRules.NormalizeText(request.DevStatusUrl),
            DeviceApiEndpointRules.NormalizeText(request.PostDataDomain),
            response.Status,
            response.Msg,
            response.IsSuccess,
            DateTime.Now));
    }

    private void TryWriteLifecycleLog(DeviceLifecycleLogEntry entry)
    {
        try
        {
            _deviceLifecycleLogService.Write(entry);
        }
        catch
        {
            // 设备日志是审计增强，不能影响远程接口主流程。
        }
    }

    private static BasicRes<T> Success<T>(T data)
    {
        return new BasicRes<T>
        {
            Status = AppConstants.MesStatus.Success,
            Msg = "成功",
            Data = data
        };
    }

    private static BasicRes<T> Failure<T>(string message)
    {
        return new BasicRes<T>
        {
            Status = AppConstants.MesStatus.Error,
            Msg = message
        };
    }
}
