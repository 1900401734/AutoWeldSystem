using System.Text.Json;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.DeviceApi;
using AutoWeldSystem.Core.DTOs.Mes.Request;
using AutoWeldSystem.Core.DTOs.Mes.Response;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Mes;
using AutoWeldSystem.Core.Production;
using AutoWeldSystem.Core.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// WinForms 进程内的轻量设备端 HTTP 服务。
/// 该服务只负责监听和路由，具体业务交给 IDeviceApiEndpointService。
/// </summary>
internal sealed class DeviceApiServerService : IDeviceApiServerService
{
    private readonly IAppSettingsService _settingsService;
    private readonly IDeviceApiEndpointService _endpointService;
    private readonly IDeviceLifecycleLogService _deviceLifecycleLogService;
    private readonly IProgramExceptionLogService _exceptionLogService;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    private WebApplication? _app;
    private string? _listeningBaseUrl;
    private bool _settingsChangedSubscribed;

    public DeviceApiServerService(
        IAppSettingsService settingsService,
        IDeviceApiEndpointService endpointService,
        IDeviceLifecycleLogService deviceLifecycleLogService,
        IProgramExceptionLogService exceptionLogService)
    {
        _settingsService = settingsService;
        _endpointService = endpointService;
        _deviceLifecycleLogService = deviceLifecycleLogService;
        _exceptionLogService = exceptionLogService;
    }

    /// <summary>
    /// 按当前 DeviceBaseUrl 启动 HTTP 监听。
    /// 监听失败只写日志，不阻断 WinForms 主流程。
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = DeviceApiEndpointRules.DefaultBaseUrl;
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            SubscribeSettingsChanged();
            if (_app is not null)
            {
                return;
            }

            baseUrl = DeviceApiEndpointRules.NormalizeBaseUrl(_settingsService.Get().DeviceBaseUrl);
            await StartLockedAsync(baseUrl, cancellationToken);
            WriteHttpSelfCheckLog(baseUrl, success: true, "HTTP 服务监听成功");
        }
        catch (Exception ex)
        {
            WriteHttpSelfCheckLog(baseUrl, success: false, ex.Message);
            LogFailure(ex, "DeviceApiServer.Start");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// 停止 HTTP 监听，并取消配置变更订阅。
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            UnsubscribeSettingsChanged();
            await StopLockedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogFailure(ex, "DeviceApiServer.Stop");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private async Task StartLockedAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>(),
            ApplicationName = typeof(DeviceApiServerService).Assembly.GetName().Name
        });

        // 保持响应字段为 Status/Msg/Data，与 MES/平台现有示例一致。
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = null;
        });
        builder.WebHost.UseUrls(baseUrl);

        var app = builder.Build();
        MapEndpoints(app);

        try
        {
            await app.StartAsync(cancellationToken);
            _app = app;
            _listeningBaseUrl = baseUrl;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    private static DeviceApiResponse<T> ToResponse<T>(BasicRes<T> response)
    {
        return new DeviceApiResponse<T>
        {
            Status = response.Status,
            Msg = response.Msg,
            Data = response.Data
        };
    }

    private void MapEndpoints(WebApplication app)
    {
        var settings = _settingsService.Get();
        var deviceStatusQueryRoute = MesEndpointRouteRules.NormalizeRoute(settings.DeviceStatusQueryRoute, MesEndpointRouteRules.DeviceStatusQueryDefaultRoute);
        var deviceIdSetRoute = MesEndpointRouteRules.NormalizeRoute(settings.DeviceIdSetRoute, MesEndpointRouteRules.DeviceIdSetDefaultRoute);

        app.MapGet($"/{deviceStatusQueryRoute}", (HttpRequest request) =>
        {
            try
            {
                var deviceId = request.Query["DeviceId"].FirstOrDefault();
                return Results.Json(ToResponse(_endpointService.GetDeviceStatus(deviceId)));
            }
            catch (Exception ex)
            {
                LogFailure(ex, "DeviceApiServer.DeviceStatus");
                return Results.Json(ToResponse(Failure<DeviceStatusQueryRes>($"设备状态查询失败：{ex.Message}")));
            }
        });

        app.MapPost($"/{deviceIdSetRoute}", async (HttpContext context) =>
        {
            try
            {
                var request = await context.Request.ReadFromJsonAsync<AddDeviceReq>(
                    cancellationToken: context.RequestAborted);
                if (request is null)
                {
                    return Results.Json(ToResponse(Failure<DeviceIdSetRes>("请求报文不能为空")));
                }

                return Results.Json(ToResponse(await _endpointService.SetDeviceIdAsync(request, context.RequestAborted)));
            }
            catch (JsonException ex)
            {
                return Results.Json(ToResponse(Failure<DeviceIdSetRes>($"请求报文格式错误：{ex.Message}")));
            }
            catch (Exception ex)
            {
                LogFailure(ex, "DeviceApiServer.DeviceID");
                return Results.Json(ToResponse(Failure<DeviceIdSetRes>($"设备编号设置失败：{ex.Message}")));
            }
        });
    }

    private async Task StopLockedAsync(CancellationToken cancellationToken)
    {
        var app = _app;
        _app = null;
        _listeningBaseUrl = null;
        if (app is null)
        {
            return;
        }

        await app.StopAsync(cancellationToken);
        await app.DisposeAsync();
    }

    private void SubscribeSettingsChanged()
    {
        if (_settingsChangedSubscribed)
        {
            return;
        }

        _settingsService.SettingsChanged += SettingsService_SettingsChanged;
        _settingsChangedSubscribed = true;
    }

    private void UnsubscribeSettingsChanged()
    {
        if (!_settingsChangedSubscribed)
        {
            return;
        }

        _settingsService.SettingsChanged -= SettingsService_SettingsChanged;
        _settingsChangedSubscribed = false;
    }

    private void SettingsService_SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        if (!e.HasChanged(nameof(AppSettings.DeviceBaseUrl)))
        {
            return;
        }

        _ = RestartAfterSettingsChangedAsync(e.CurrentSettings.DeviceBaseUrl);
    }

    private async Task RestartAfterSettingsChangedAsync(string deviceBaseUrl)
    {
        // POST /api/DeviceID 保存 DeviceBaseUrl 后也会触发这里，短暂延迟可避免中断当前响应。
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        await _lifecycleLock.WaitAsync();
        try
        {
            if (!_settingsChangedSubscribed)
            {
                return;
            }

            var nextBaseUrl = DeviceApiEndpointRules.NormalizeBaseUrl(deviceBaseUrl);
            if (string.Equals(_listeningBaseUrl, nextBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await StopLockedAsync(CancellationToken.None);
            await StartLockedAsync(nextBaseUrl, CancellationToken.None);
            WriteHttpSelfCheckLog(nextBaseUrl, success: true, "HTTP 服务重启成功");
        }
        catch (Exception ex)
        {
            WriteHttpSelfCheckLog(deviceBaseUrl, success: false, $"HTTP 服务重启失败：{ex.Message}");
            LogFailure(ex, "DeviceApiServer.Restart", $"DeviceBaseUrl={deviceBaseUrl}");
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private void WriteHttpSelfCheckLog(string deviceBaseUrl, bool success, string message)
    {
        try
        {
            var settings = _settingsService.Get();
            _deviceLifecycleLogService.Write(DeviceLifecycleLogRules.CreateDeviceApiHttpSelfCheckEntry(
                settings.DeviceId,
                DeviceApiEndpointRules.NormalizeBaseUrl(deviceBaseUrl),
                success,
                message,
                DateTime.Now));
        }
        catch
        {
            // HTTP 服务自检日志失败不能影响程序启动或接口监听。
        }
    }

    private void LogFailure(Exception exception, string source, string? context = null)
    {
        try
        {
            _exceptionLogService.Write(exception, source, context);
        }
        catch
        {
            // 设备 API 是附加能力，日志写入失败时不能影响主程序关闭或启动。
        }
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
