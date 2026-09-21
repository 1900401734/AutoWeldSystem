using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.CenterServer.Services;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // 发布后由 Run 启动项拉起时工作目录可能是 System32。
    ContentRootPath = AppContext.BaseDirectory
});
var dashboardDemo = builder.Configuration.GetValue<bool>("DashboardDemo");

builder.Host.UseSerilog((context, configuration) =>
{
    var logRoot = context.Configuration.GetValue<string>("CenterServer:LogDirectory");
    if (string.IsNullOrWhiteSpace(logRoot))
    {
        logRoot = Path.Combine(AppContext.BaseDirectory, "CenterLogs");
    }

    configuration
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(logRoot, "Server", "center-server-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new SqlSugarDbContext(configuration.GetConnectionString("Default"));
});
builder.Services.AddSingleton<CenterServerSettingsService>();
builder.Services.AddSingleton<CenterServerWindowsStartupService>();
builder.Services.AddSingleton<CenterDashboardLaunchService>();
builder.Services.AddSingleton<CenterDashboardChangeNotifier>();
builder.Services.AddSingleton<CenterPushJsonlLogService>();
builder.Services.AddSingleton<CenterTelemetryIngestService>();
builder.Services.AddSingleton<CenterDashboardQueryService>();
builder.Services.AddSingleton<CenterDeviceRemovalService>();
builder.Services.AddSingleton<CenterProductReportFileStore>();
builder.Services.AddSingleton<ICenterProductReportIngestSideEffects, CenterProductReportIngestSideEffects>();
builder.Services.AddSingleton<CenterProductReportIngestService>();

var app = builder.Build();
if (!dashboardDemo)
{
    var settingsService = app.Services.GetRequiredService<CenterServerSettingsService>();
    var startupService = app.Services.GetRequiredService<CenterServerWindowsStartupService>();
    if (!app.Configuration.GetValue<bool>("DisableDesktopIntegration"))
    {
        startupService.Apply(settingsService.Get().EnableAutoStart);
        settingsService.SettingsChanged += (_, settings) => startupService.Apply(settings.EnableAutoStart);
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            if (settingsService.Get().OpenDashboardOnStart)
            {
                app.Services.GetRequiredService<CenterDashboardLaunchService>().OpenOnce(app.Urls);
            }
        });
    }
}

app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/center/telemetry" || context.Request.Path == "/api/center/heartbeat")
    {
        var limit = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false }) limit.MaxRequestBodySize = 256 * 1024;
    }
    await next();
});
app.MapGet("/healthz", () => Results.Ok(new { status = "ready" }));

// 演示实例只供视觉验证，拒绝设备写入且不访问真实数据库。
if (dashboardDemo)
{
    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/center"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        await next();
    });
}

app.MapPost("/api/center/telemetry", async (
    CenterTelemetrySnapshotRequest request,
    CenterTelemetryIngestService service,
    CenterPushJsonlLogService pushLogService,
    CancellationToken cancellationToken) =>
{
    return await HandleTelemetryAsync(AppConstants.CenterInteractionTypes.Telemetry, request, service, pushLogService, cancellationToken);
});

app.MapPost("/api/center/heartbeat", async (
    CenterTelemetrySnapshotRequest request,
    CenterTelemetryIngestService service,
    CenterPushJsonlLogService pushLogService,
    CancellationToken cancellationToken) =>
{
    return await HandleTelemetryAsync(AppConstants.CenterInteractionTypes.Heartbeat, request, service, pushLogService, cancellationToken);
});

app.MapPost("/api/center/product-report", async (
    CenterProductReportRequest request,
    CenterProductReportIngestService service,
    CenterPushJsonlLogService pushLogService,
    CancellationToken cancellationToken) =>
{
    CenterTelemetryAck result;
    try
    {
        result = await service.IngestAsync(request, cancellationToken);
        pushLogService.WriteProductReport(request, result);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
    catch (Exception ex)
    {
        result = BuildFailureAck(ex);
        pushLogService.WriteProductReport(request, result, ex);
        return Results.Json(result, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/api/center/dashboard", (
    CenterDashboardQueryService service,
    CancellationToken cancellationToken) => service.GetSnapshot(cancellationToken));

app.MapBlazorHub();
app.MapHub<CenterDashboardHub>("/hubs/center-dashboard");
app.MapFallbackToPage("/_Host");

Log.Information("Center server configured URLs: {Urls}. Ensure the industrial PC firewall allows TCP 7099 when accessed from LAN.", app.Configuration["Urls"]);
app.Run();

static async Task<IResult> HandleTelemetryAsync(
    string requestType,
    CenterTelemetrySnapshotRequest request,
    CenterTelemetryIngestService service,
    CenterPushJsonlLogService pushLogService,
    CancellationToken cancellationToken)
{
    CenterTelemetryAck result;
    try
    {
        // 只有完整遥测端点会枚举设备当前的全部工位；心跳从不携带工位数据，
        // 不能让它参与工位行的写入与清理。
        var carriesStations = string.Equals(
            requestType,
            AppConstants.CenterInteractionTypes.Telemetry,
            StringComparison.Ordinal);
        result = await service.IngestAsync(request, carriesStations, cancellationToken);
        pushLogService.WriteTelemetry(requestType, request, result);
        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
    catch (Exception ex)
    {
        result = BuildFailureAck(ex);
        pushLogService.WriteTelemetry(requestType, request, result, ex);
        return Results.Json(result, statusCode: StatusCodes.Status500InternalServerError);
    }
}

static CenterTelemetryAck BuildFailureAck(Exception exception)
{
    return new CenterTelemetryAck
    {
        Success = false,
        Message = exception.Message,
        ServerTime = DateTime.Now
    };
}
