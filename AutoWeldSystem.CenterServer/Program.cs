using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.CenterServer.Services;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new SqlSugarDbContext(configuration.GetConnectionString("Default"));
});
builder.Services.AddSingleton<CenterTelemetryIngestService>();
builder.Services.AddSingleton<CenterDashboardQueryService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapPost("/api/center/telemetry", async (
    CenterTelemetrySnapshotRequest request,
    CenterTelemetryIngestService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.IngestAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/api/center/heartbeat", async (
    CenterTelemetrySnapshotRequest request,
    CenterTelemetryIngestService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.IngestAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
});


app.MapGet("/api/center/dashboard", (
    CenterDashboardQueryService service,
    CancellationToken cancellationToken) => service.GetSnapshot(cancellationToken));

app.MapBlazorHub();
app.MapHub<CenterDashboardHub>("/hubs/center-dashboard");
app.MapFallbackToPage("/_Host");

app.Run();
