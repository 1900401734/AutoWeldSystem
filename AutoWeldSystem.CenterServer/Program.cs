using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.CenterServer.Services;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<CenterDashboardChangeNotifier>();
builder.Services.AddSingleton<CenterPushJsonlLogService>();
builder.Services.AddSingleton<CenterTelemetryIngestService>();
builder.Services.AddSingleton<CenterDashboardQueryService>();
builder.Services.AddSingleton<CenterDeviceRemovalService>();
builder.Services.AddSingleton<CenterProductReportFileStore>();
builder.Services.AddSingleton<ICenterProductReportIngestSideEffects, CenterProductReportIngestSideEffects>();
builder.Services.AddSingleton<CenterProductReportIngestService>();

var app = builder.Build();
var settingsService = app.Services.GetRequiredService<CenterServerSettingsService>();
var startupService = app.Services.GetRequiredService<CenterServerWindowsStartupService>();
startupService.Apply(settingsService.Get().EnableAutoStart);
settingsService.SettingsChanged += (_, settings) => startupService.Apply(settings.EnableAutoStart);

app.UseStaticFiles();
app.UseRouting();

app.MapPost("/api/center/telemetry", async (
    CenterTelemetrySnapshotRequest request,
    CenterTelemetryIngestService service,
    CenterPushJsonlLogService pushLogService,
    CancellationToken cancellationToken) =>
{
    return await HandleTelemetryAsync("telemetry", request, service, pushLogService, cancellationToken);
});

app.MapPost("/api/center/heartbeat", async (
    CenterTelemetrySnapshotRequest request,
    CenterTelemetryIngestService service,
    CenterPushJsonlLogService pushLogService,
    CancellationToken cancellationToken) =>
{
    return await HandleTelemetryAsync("heartbeat", request, service, pushLogService, cancellationToken);
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
        result = await service.IngestAsync(request, cancellationToken);
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
