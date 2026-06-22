# Center Server Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a center-server project that receives telemetry from multiple AutoWeldSystem devices, stores summarized production data, and shows real-time device status on a web dashboard.

**Architecture:** Add an ASP.NET Core/Blazor Server project named `AutoWeldSystem.CenterServer`. Device clients keep their local production workflow unchanged and push center telemetry through HTTP; the server persists data with SqlSugar/MySQL and pushes dashboard updates through SignalR.

**Tech Stack:** .NET 8, ASP.NET Core, Blazor Server, SignalR, SqlSugar, MySQL, `IHttpClientFactory`, existing `AutoWeldSystem.Core` DTO/entity conventions.

---

## File Structure

- Create `AutoWeldSystem.CenterServer/AutoWeldSystem.CenterServer.csproj`: ASP.NET Core/Blazor Server project.
- Create `AutoWeldSystem.CenterServer/Program.cs`: server DI, database initialization, endpoints, SignalR, Blazor.
- Create `AutoWeldSystem.CenterServer/appsettings.json`: center database connection and listening URL defaults.
- Create `AutoWeldSystem.CenterServer/Services/CenterTelemetryIngestService.cs`: validates, deduplicates, and stores telemetry.
- Create `AutoWeldSystem.CenterServer/Services/CenterDashboardQueryService.cs`: builds dashboard snapshots.
- Create `AutoWeldSystem.CenterServer/Hubs/CenterDashboardHub.cs`: SignalR hub for real-time dashboard refresh.
- Create `AutoWeldSystem.CenterServer/Pages/Dashboard.razor`: first dashboard page.
- Create `AutoWeldSystem.CenterServer/wwwroot/css/center-dashboard.css`: dashboard styling.
- Create `AutoWeldSystem.Core/Constants/CenterServerConstants.cs`: device type names, event types, default intervals.
- Create `AutoWeldSystem.Core/DTOs/CenterServer/*.cs`: shared request/response/dashboard DTOs.
- Create `AutoWeldSystem.Core/Entities/Center*.cs`: center database tables.
- Modify `AutoWeldSystem.Data/SqlSugarDbContext.cs`: include center tables in CodeFirst, or add a focused `InitCenterDatabase()` method.
- Modify `AutoWeldSystem.Core/Entities/AppSettings.cs`: add center-server client settings.
- Modify `AutoWeldSystem.Services/AppSettingsService.cs`: normalize center-server settings.
- Modify `AutoWeldSystem.UI/Views/SystemSettingView.cs` and `.Designer.cs`: add center-server settings controls.
- Create `AutoWeldSystem.Core/Interfaces/ICenterTelemetrySyncService.cs`: device-side background sync contract.
- Create `AutoWeldSystem.Services/Center/CenterTelemetryClient.cs`: HTTP client for center server.
- Create `AutoWeldSystem.Services/Center/CenterTelemetrySyncService.cs`: builds telemetry batches and retries.
- Modify `AutoWeldSystem.UI/Program.cs`: register and start/stop center sync service.

---

## Task 1: Add Center Server Project

**Files:**
- Create: `AutoWeldSystem.CenterServer/AutoWeldSystem.CenterServer.csproj`
- Create: `AutoWeldSystem.CenterServer/Program.cs`
- Create: `AutoWeldSystem.CenterServer/appsettings.json`
- Modify: `AutoWeldSystem.sln`

- [ ] **Step 1: Create the ASP.NET Core project file**

Create `AutoWeldSystem.CenterServer/AutoWeldSystem.CenterServer.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SqlSugarCore" Version="5.1.4.191" />
    <ProjectReference Include="..\AutoWeldSystem.Core\AutoWeldSystem.Core.csproj" />
    <ProjectReference Include="..\AutoWeldSystem.Data\AutoWeldSystem.Data.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add minimal server bootstrap**

Create `AutoWeldSystem.CenterServer/Program.cs`:

```csharp
using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.CenterServer.Services;
using AutoWeldSystem.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new SqlSugarDbContext(configuration["Database:ConnectionString"]);
});
builder.Services.AddSingleton<CenterTelemetryIngestService>();
builder.Services.AddSingleton<CenterDashboardQueryService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapPost("/api/center/telemetry/batch", async (
    AutoWeldSystem.Core.DTOs.CenterServer.CenterTelemetryBatchRequest request,
    CenterTelemetryIngestService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.IngestAsync(request, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/center/dashboard/snapshot", (
    CenterDashboardQueryService service,
    CancellationToken cancellationToken) =>
{
    return Results.Ok(service.GetSnapshot(cancellationToken));
});

app.MapBlazorHub();
app.MapHub<CenterDashboardHub>("/hubs/center-dashboard");
app.MapFallbackToPage("/Dashboard");

var dbContext = app.Services.GetRequiredService<SqlSugarDbContext>();
dbContext.InitDatabase();

app.Run();
```

- [ ] **Step 3: Add server configuration**

Create `AutoWeldSystem.CenterServer/appsettings.json`:

```json
{
  "Urls": "http://0.0.0.0:7099",
  "Database": {
    "ConnectionString": "server=localhost;port=3306;database=autoweld_center_db;uid=root;pwd=123456;charset=utf8mb4;"
  },
  "Dashboard": {
    "OfflineSeconds": 30
  }
}
```

- [ ] **Step 4: Add project to solution**

Run:

```powershell
dotnet sln AutoWeldSystem.sln add AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj
```

Expected: the solution lists `AutoWeldSystem.CenterServer`.

- [ ] **Step 5: Build to expose missing DTO/service errors**

Run:

```powershell
dotnet build AutoWeldSystem.sln --no-restore
```

Expected: FAIL because `CenterDashboardHub`, `CenterTelemetryIngestService`, `CenterDashboardQueryService`, and center DTOs do not exist yet.

- [ ] **Step 6: Commit**

```powershell
git add AutoWeldSystem.sln AutoWeldSystem.CenterServer
git commit -m "feat(center): add center server project shell"
```

---

## Task 2: Add Shared Center Contracts

**Files:**
- Create: `AutoWeldSystem.Core/Constants/CenterServerConstants.cs`
- Create: `AutoWeldSystem.Core/DTOs/CenterServer/CenterTelemetryBatchRequest.cs`
- Create: `AutoWeldSystem.Core/DTOs/CenterServer/CenterTelemetryAck.cs`
- Create: `AutoWeldSystem.Core/DTOs/CenterServer/CenterDeviceRuntimeDto.cs`
- Create: `AutoWeldSystem.Core/DTOs/CenterServer/CenterWeldTaskDto.cs`
- Create: `AutoWeldSystem.Core/DTOs/CenterServer/CenterWeldPointRecordDto.cs`
- Create: `AutoWeldSystem.Core/DTOs/CenterServer/CenterDashboardSnapshotDto.cs`

- [ ] **Step 1: Add constants**

Create `AutoWeldSystem.Core/Constants/CenterServerConstants.cs`:

```csharp
namespace AutoWeldSystem.Core.Constants;

/// <summary>
/// Constants used by center-server telemetry and dashboard features.
/// </summary>
public static class CenterServerConstants
{
    public const int DefaultUploadIntervalSeconds = 5;
    public const int DefaultOfflineSeconds = 30;

    public static class DeviceTypes
    {
        public const string MonostableElectromagneticSpotWeld = "MonostableElectromagneticSpotWeld";
        public const string MagneticLatchingElectromagneticSpotWeld = "MagneticLatchingElectromagneticSpotWeld";
        public const string YokeAutomaticSpotWeld = "YokeAutomaticSpotWeld";
    }

    public static class EventTypes
    {
        public const string Runtime = "Runtime";
        public const string Task = "Task";
        public const string WeldPoint = "WeldPoint";
    }
}
```

- [ ] **Step 2: Add telemetry batch request**

Create `AutoWeldSystem.Core/DTOs/CenterServer/CenterTelemetryBatchRequest.cs`:

```csharp
namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Batch uploaded by one equipment client to the center server.
/// </summary>
public sealed class CenterTelemetryBatchRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.Now;
    public CenterDeviceRuntimeDto Runtime { get; set; } = new();
    public List<CenterWeldTaskDto> Tasks { get; set; } = new();
    public List<CenterWeldPointRecordDto> WeldPoints { get; set; } = new();
}
```

- [ ] **Step 3: Add acknowledgement DTO**

Create `AutoWeldSystem.Core/DTOs/CenterServer/CenterTelemetryAck.cs`:

```csharp
namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Center-server response returned to the equipment client after telemetry is stored.
/// </summary>
public sealed class CenterTelemetryAck
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; } = DateTime.Now;
    public int AcceptedTaskCount { get; set; }
    public int AcceptedWeldPointCount { get; set; }
}
```

- [ ] **Step 4: Add runtime DTO**

Create `AutoWeldSystem.Core/DTOs/CenterServer/CenterDeviceRuntimeDto.cs`:

```csharp
namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Current device runtime state shown on the center dashboard.
/// </summary>
public sealed class CenterDeviceRuntimeDto
{
    public bool PlcConnected { get; set; }
    public string PlcConnectionState { get; set; } = string.Empty;
    public int StationNo { get; set; }
    public string DeviceStatusCode { get; set; } = string.Empty;
    public string DeviceStatusName { get; set; } = string.Empty;
    public string AlarmMessage { get; set; } = string.Empty;
    public int ActualQty { get; set; }
    public int QualifiedQty { get; set; }
    public int FailedQty { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.Now;
}
```

- [ ] **Step 5: Add task DTO**

Create `AutoWeldSystem.Core/DTOs/CenterServer/CenterWeldTaskDto.cs`:

```csharp
namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Work-order task snapshot uploaded from one equipment client.
/// </summary>
public sealed class CenterWeldTaskDto
{
    public int LocalTaskId { get; set; }
    public int StationNo { get; set; }
    public string SN { get; set; } = string.Empty;
    public string ProductNum { get; set; } = string.Empty;
    public string ProductModel { get; set; } = string.Empty;
    public string ProcessNo { get; set; } = string.Empty;
    public string ProgramId { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public string RecipeCode { get; set; } = string.Empty;
    public string TaskStatus { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ActualQty { get; set; }
    public int QualifiedQty { get; set; }
    public int FailedQty { get; set; }
}
```

- [ ] **Step 6: Add weld-point DTO**

Create `AutoWeldSystem.Core/DTOs/CenterServer/CenterWeldPointRecordDto.cs`:

```csharp
namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Collected product or weld-point record uploaded to the center server.
/// </summary>
public sealed class CenterWeldPointRecordDto
{
    public int LocalRecordId { get; set; }
    public int LocalTaskId { get; set; }
    public int StationNo { get; set; }
    public string SN { get; set; } = string.Empty;
    public string ProductNo { get; set; } = string.Empty;
    public string TouchNo { get; set; } = string.Empty;
    public string TestResult { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Ts { get; set; }
    public bool ProductCompleted { get; set; }
    public string RawDataJson { get; set; } = string.Empty;
}
```

- [ ] **Step 7: Add dashboard snapshot DTO**

Create `AutoWeldSystem.Core/DTOs/CenterServer/CenterDashboardSnapshotDto.cs`:

```csharp
namespace AutoWeldSystem.Core.DTOs.CenterServer;

/// <summary>
/// Dashboard data returned by the center server and pushed through SignalR.
/// </summary>
public sealed class CenterDashboardSnapshotDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public List<CenterDashboardDeviceDto> Devices { get; set; } = new();
}

public sealed class CenterDashboardDeviceDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public bool Online { get; set; }
    public CenterDeviceRuntimeDto Runtime { get; set; } = new();
    public CenterWeldTaskDto? CurrentTask { get; set; }
    public int TodayProductCount { get; set; }
    public int TodayQualifiedCount { get; set; }
    public int TodayFailedCount { get; set; }
}
```

- [ ] **Step 8: Build**

Run:

```powershell
dotnet build AutoWeldSystem.Core\AutoWeldSystem.Core.csproj --no-restore
```

Expected: PASS.

- [ ] **Step 9: Commit**

```powershell
git add AutoWeldSystem.Core\Constants\CenterServerConstants.cs AutoWeldSystem.Core\DTOs\CenterServer
git commit -m "feat(center): add shared telemetry contracts"
```

---

## Task 3: Add Center Storage Entities

**Files:**
- Create: `AutoWeldSystem.Core/Entities/CenterDeviceNode.cs`
- Create: `AutoWeldSystem.Core/Entities/CenterDeviceRuntimeSnapshot.cs`
- Create: `AutoWeldSystem.Core/Entities/CenterWeldTask.cs`
- Create: `AutoWeldSystem.Core/Entities/CenterWeldPointRecord.cs`
- Modify: `AutoWeldSystem.Data/SqlSugarDbContext.cs`

- [ ] **Step 1: Add device node entity**

Create `AutoWeldSystem.Core/Entities/CenterDeviceNode.cs`:

```csharp
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// One equipment registered by telemetry upload.
/// </summary>
[SugarTable("Center_DeviceNode", TableDescription = "中心服务器设备节点")]
public sealed class CenterDeviceNode
{
    [SugarColumn(IsPrimaryKey = true, Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string DeviceName { get; set; } = string.Empty;

    [SugarColumn(Length = 80)]
    public string DeviceType { get; set; } = string.Empty;

    public DateTime FirstSeenAt { get; set; } = DateTime.Now;

    public DateTime LastSeenAt { get; set; } = DateTime.Now;
}
```

- [ ] **Step 2: Add runtime snapshot entity**

Create `AutoWeldSystem.Core/Entities/CenterDeviceRuntimeSnapshot.cs`:

```csharp
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Latest runtime state for one device.
/// </summary>
[SugarTable("Center_DeviceRuntimeSnapshot", TableDescription = "中心服务器设备运行快照")]
public sealed class CenterDeviceRuntimeSnapshot
{
    [SugarColumn(IsPrimaryKey = true, Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    public bool PlcConnected { get; set; }

    [SugarColumn(Length = 50)]
    public string PlcConnectionState { get; set; } = string.Empty;

    public int StationNo { get; set; }

    [SugarColumn(Length = 20)]
    public string DeviceStatusCode { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string DeviceStatusName { get; set; } = string.Empty;

    [SugarColumn(Length = 500)]
    public string AlarmMessage { get; set; } = string.Empty;

    public int ActualQty { get; set; }
    public int QualifiedQty { get; set; }
    public int FailedQty { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

- [ ] **Step 3: Add center task entity**

Create `AutoWeldSystem.Core/Entities/CenterWeldTask.cs`:

```csharp
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Center copy of a local equipment weld task.
/// </summary>
[SugarTable("Center_WeldTask", TableDescription = "中心服务器生产任务")]
public sealed class CenterWeldTask
{
    [SugarColumn(IsPrimaryKey = true, Length = 100)]
    public string TaskKey { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    public int LocalTaskId { get; set; }
    public int StationNo { get; set; }

    [SugarColumn(Length = 50)]
    public string SN { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string ProductNum { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string ProductModel { get; set; } = string.Empty;

    [SugarColumn(Length = 20)]
    public string ProcessNo { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string ProgramId { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string ProgramName { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string RecipeCode { get; set; } = string.Empty;

    [SugarColumn(Length = 20)]
    public string TaskStatus { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ActualQty { get; set; }
    public int QualifiedQty { get; set; }
    public int FailedQty { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

- [ ] **Step 4: Add center weld-point entity**

Create `AutoWeldSystem.Core/Entities/CenterWeldPointRecord.cs`:

```csharp
using SqlSugar;

namespace AutoWeldSystem.Core.Entities;

/// <summary>
/// Center copy of one product or weld-point collection record.
/// </summary>
[SugarTable("Center_WeldPointRecord", TableDescription = "中心服务器采集记录")]
public sealed class CenterWeldPointRecord
{
    [SugarColumn(IsPrimaryKey = true, Length = 120)]
    public string RecordKey { get; set; } = string.Empty;

    [SugarColumn(Length = 100)]
    public string TaskKey { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string DeviceId { get; set; } = string.Empty;

    public int LocalRecordId { get; set; }
    public int LocalTaskId { get; set; }
    public int StationNo { get; set; }

    [SugarColumn(Length = 50)]
    public string SN { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string ProductNo { get; set; } = string.Empty;

    [SugarColumn(Length = 50)]
    public string TouchNo { get; set; } = string.Empty;

    [SugarColumn(Length = 20)]
    public string TestResult { get; set; } = string.Empty;

    [SugarColumn(Length = 20)]
    public string Type { get; set; } = string.Empty;

    public DateTime Ts { get; set; }
    public bool ProductCompleted { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? RawDataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

- [ ] **Step 5: Add center tables to CodeFirst**

Modify `AutoWeldSystem.Data/SqlSugarDbContext.cs` by adding these types to `Db.CodeFirst.InitTables(...)` after `typeof(BizPlcAlarmAddress)`:

```csharp
typeof(CenterDeviceNode),
typeof(CenterDeviceRuntimeSnapshot),
typeof(CenterWeldTask),
typeof(CenterWeldPointRecord)
```

- [ ] **Step 6: Build**

Run:

```powershell
dotnet build AutoWeldSystem.Data\AutoWeldSystem.Data.csproj --no-restore
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add AutoWeldSystem.Core\Entities\Center*.cs AutoWeldSystem.Data\SqlSugarDbContext.cs
git commit -m "feat(center): add center storage entities"
```

---

## Task 4: Implement Center Telemetry Ingestion

**Files:**
- Create: `AutoWeldSystem.CenterServer/Services/CenterTelemetryIngestService.cs`
- Create: `AutoWeldSystem.CenterServer/Hubs/CenterDashboardHub.cs`
- Modify: `AutoWeldSystem.CenterServer/Program.cs`

- [ ] **Step 1: Add dashboard hub**

Create `AutoWeldSystem.CenterServer/Hubs/CenterDashboardHub.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Hubs;

/// <summary>
/// Pushes center dashboard changes to connected browsers.
/// </summary>
public sealed class CenterDashboardHub : Hub
{
}
```

- [ ] **Step 2: Add telemetry ingestion service**

Create `AutoWeldSystem.CenterServer/Services/CenterTelemetryIngestService.cs`:

```csharp
using AutoWeldSystem.CenterServer.Hubs;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;
using Microsoft.AspNetCore.SignalR;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// Stores telemetry uploaded by equipment clients and notifies the dashboard.
/// </summary>
public sealed class CenterTelemetryIngestService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IHubContext<CenterDashboardHub> _hubContext;

    public CenterTelemetryIngestService(SqlSugarDbContext dbContext, IHubContext<CenterDashboardHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<CenterTelemetryAck> IngestAsync(CenterTelemetryBatchRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        UpsertDevice(request);
        UpsertRuntime(request);
        var taskCount = UpsertTasks(request);
        var pointCount = UpsertWeldPoints(request);

        await _hubContext.Clients.All.SendAsync("CenterDashboardChanged", request.DeviceId, cancellationToken);

        return new CenterTelemetryAck
        {
            Success = true,
            Message = "Accepted",
            ServerTime = DateTime.Now,
            AcceptedTaskCount = taskCount,
            AcceptedWeldPointCount = pointCount
        };
    }

    private static void Validate(CenterTelemetryBatchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            throw new InvalidOperationException("DeviceId is required.");
        }
    }

    private void UpsertDevice(CenterTelemetryBatchRequest request)
    {
        var existing = _dbContext.Db.Queryable<CenterDeviceNode>().InSingle(request.DeviceId);
        var now = DateTime.Now;
        var node = existing ?? new CenterDeviceNode
        {
            DeviceId = request.DeviceId.Trim(),
            FirstSeenAt = now
        };

        node.DeviceName = request.DeviceName.Trim();
        node.DeviceType = request.DeviceType.Trim();
        node.LastSeenAt = now;

        if (existing is null)
        {
            _dbContext.Db.Insertable(node).ExecuteCommand();
            return;
        }

        _dbContext.Db.Updateable(node).ExecuteCommand();
    }

    private void UpsertRuntime(CenterTelemetryBatchRequest request)
    {
        var runtime = request.Runtime;
        var entity = new CenterDeviceRuntimeSnapshot
        {
            DeviceId = request.DeviceId.Trim(),
            PlcConnected = runtime.PlcConnected,
            PlcConnectionState = runtime.PlcConnectionState.Trim(),
            StationNo = runtime.StationNo,
            DeviceStatusCode = runtime.DeviceStatusCode.Trim(),
            DeviceStatusName = runtime.DeviceStatusName.Trim(),
            AlarmMessage = runtime.AlarmMessage.Trim(),
            ActualQty = runtime.ActualQty,
            QualifiedQty = runtime.QualifiedQty,
            FailedQty = runtime.FailedQty,
            CollectedAt = runtime.CollectedAt,
            UpdatedAt = DateTime.Now
        };

        _dbContext.Db.Storageable(entity).ExecuteCommand();
    }

    private int UpsertTasks(CenterTelemetryBatchRequest request)
    {
        var count = 0;
        foreach (var task in request.Tasks)
        {
            var entity = new CenterWeldTask
            {
                TaskKey = BuildTaskKey(request.DeviceId, task.LocalTaskId),
                DeviceId = request.DeviceId.Trim(),
                LocalTaskId = task.LocalTaskId,
                StationNo = task.StationNo,
                SN = task.SN.Trim(),
                ProductNum = task.ProductNum.Trim(),
                ProductModel = task.ProductModel.Trim(),
                ProcessNo = task.ProcessNo.Trim(),
                ProgramId = task.ProgramId.Trim(),
                ProgramName = task.ProgramName.Trim(),
                RecipeCode = task.RecipeCode.Trim(),
                TaskStatus = task.TaskStatus.Trim(),
                StartTime = task.StartTime,
                EndTime = task.EndTime,
                ActualQty = task.ActualQty,
                QualifiedQty = task.QualifiedQty,
                FailedQty = task.FailedQty,
                UpdatedAt = DateTime.Now
            };

            _dbContext.Db.Storageable(entity).ExecuteCommand();
            count++;
        }

        return count;
    }

    private int UpsertWeldPoints(CenterTelemetryBatchRequest request)
    {
        var count = 0;
        foreach (var record in request.WeldPoints)
        {
            var entity = new CenterWeldPointRecord
            {
                RecordKey = $"{request.DeviceId}:{record.LocalRecordId}",
                TaskKey = BuildTaskKey(request.DeviceId, record.LocalTaskId),
                DeviceId = request.DeviceId.Trim(),
                LocalRecordId = record.LocalRecordId,
                LocalTaskId = record.LocalTaskId,
                StationNo = record.StationNo,
                SN = record.SN.Trim(),
                ProductNo = record.ProductNo.Trim(),
                TouchNo = record.TouchNo.Trim(),
                TestResult = record.TestResult.Trim(),
                Type = record.Type.Trim(),
                Ts = record.Ts,
                ProductCompleted = record.ProductCompleted,
                RawDataJson = record.RawDataJson,
                CreatedAt = DateTime.Now
            };

            _dbContext.Db.Storageable(entity).ExecuteCommand();
            count++;
        }

        return count;
    }

    private static string BuildTaskKey(string deviceId, int localTaskId)
    {
        return $"{deviceId.Trim()}:{localTaskId}";
    }
}
```

- [ ] **Step 3: Build server**

Run:

```powershell
dotnet build AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj --no-restore
```

Expected: FAIL only if dashboard query service is still missing. Continue to Task 5.

- [ ] **Step 4: Commit**

```powershell
git add AutoWeldSystem.CenterServer\Services\CenterTelemetryIngestService.cs AutoWeldSystem.CenterServer\Hubs\CenterDashboardHub.cs AutoWeldSystem.CenterServer\Program.cs
git commit -m "feat(center): ingest equipment telemetry"
```

---

## Task 5: Implement Dashboard Query Service

**Files:**
- Create: `AutoWeldSystem.CenterServer/Services/CenterDashboardQueryService.cs`

- [ ] **Step 1: Add query service**

Create `AutoWeldSystem.CenterServer/Services/CenterDashboardQueryService.cs`:

```csharp
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Data;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// Builds read-only dashboard snapshots from center database tables.
/// </summary>
public sealed class CenterDashboardQueryService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public CenterDashboardQueryService(SqlSugarDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public CenterDashboardSnapshotDto GetSnapshot(CancellationToken cancellationToken = default)
    {
        var offlineSeconds = _configuration.GetValue("Dashboard:OfflineSeconds", CenterServerConstants.DefaultOfflineSeconds);
        var onlineThreshold = DateTime.Now.AddSeconds(-Math.Max(5, offlineSeconds));
        var today = DateTime.Today;
        var nodes = _dbContext.Db.Queryable<CenterDeviceNode>().OrderBy(it => it.DeviceId).ToList();
        var runtimes = _dbContext.Db.Queryable<CenterDeviceRuntimeSnapshot>().ToList()
            .ToDictionary(it => it.DeviceId, StringComparer.OrdinalIgnoreCase);

        var snapshot = new CenterDashboardSnapshotDto();
        foreach (var node in nodes)
        {
            runtimes.TryGetValue(node.DeviceId, out var runtime);
            snapshot.Devices.Add(new CenterDashboardDeviceDto
            {
                DeviceId = node.DeviceId,
                DeviceName = node.DeviceName,
                DeviceType = node.DeviceType,
                Online = node.LastSeenAt >= onlineThreshold,
                Runtime = ToRuntimeDto(runtime),
                CurrentTask = GetCurrentTask(node.DeviceId),
                TodayProductCount = CountTodayProducts(node.DeviceId, today),
                TodayQualifiedCount = CountTodayProducts(node.DeviceId, today, "OK"),
                TodayFailedCount = CountTodayProducts(node.DeviceId, today, "NG")
            });
        }

        return snapshot;
    }

    private CenterWeldTaskDto? GetCurrentTask(string deviceId)
    {
        var task = _dbContext.Db.Queryable<CenterWeldTask>()
            .Where(it => it.DeviceId == deviceId && it.EndTime == null)
            .OrderByDescending(it => it.StartTime)
            .First();

        return task is null ? null : new CenterWeldTaskDto
        {
            LocalTaskId = task.LocalTaskId,
            StationNo = task.StationNo,
            SN = task.SN,
            ProductNum = task.ProductNum,
            ProductModel = task.ProductModel,
            ProcessNo = task.ProcessNo,
            ProgramId = task.ProgramId,
            ProgramName = task.ProgramName,
            RecipeCode = task.RecipeCode,
            TaskStatus = task.TaskStatus,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            ActualQty = task.ActualQty,
            QualifiedQty = task.QualifiedQty,
            FailedQty = task.FailedQty
        };
    }

    private int CountTodayProducts(string deviceId, DateTime today, string? result = null)
    {
        var query = _dbContext.Db.Queryable<CenterWeldPointRecord>()
            .Where(it => it.DeviceId == deviceId && it.ProductCompleted && it.Ts >= today && it.Ts < today.AddDays(1));

        if (!string.IsNullOrWhiteSpace(result))
        {
            query = query.Where(it => it.TestResult == result);
        }

        return query.Count();
    }

    private static CenterDeviceRuntimeDto ToRuntimeDto(CenterDeviceRuntimeSnapshot? runtime)
    {
        if (runtime is null)
        {
            return new CenterDeviceRuntimeDto();
        }

        return new CenterDeviceRuntimeDto
        {
            PlcConnected = runtime.PlcConnected,
            PlcConnectionState = runtime.PlcConnectionState,
            StationNo = runtime.StationNo,
            DeviceStatusCode = runtime.DeviceStatusCode,
            DeviceStatusName = runtime.DeviceStatusName,
            AlarmMessage = runtime.AlarmMessage,
            ActualQty = runtime.ActualQty,
            QualifiedQty = runtime.QualifiedQty,
            FailedQty = runtime.FailedQty,
            CollectedAt = runtime.CollectedAt
        };
    }
}
```

- [ ] **Step 2: Build**

Run:

```powershell
dotnet build AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj --no-restore
```

Expected: PASS after Task 4 and Task 5 are complete.

- [ ] **Step 3: Commit**

```powershell
git add AutoWeldSystem.CenterServer\Services\CenterDashboardQueryService.cs
git commit -m "feat(center): query dashboard snapshots"
```

---

## Task 6: Build Center Dashboard UI

**Files:**
- Create: `AutoWeldSystem.CenterServer/Pages/_Host.cshtml`
- Create: `AutoWeldSystem.CenterServer/Pages/Dashboard.razor`
- Create: `AutoWeldSystem.CenterServer/wwwroot/css/center-dashboard.css`

- [ ] **Step 1: Add host page**

Create `AutoWeldSystem.CenterServer/Pages/_Host.cshtml`:

```cshtml
@page "/"
@namespace AutoWeldSystem.CenterServer.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
<html lang="zh-CN">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>中心服务器看板</title>
    <link rel="stylesheet" href="css/center-dashboard.css" />
</head>
<body>
    <component type="typeof(Dashboard)" render-mode="ServerPrerendered" />
    <script src="_framework/blazor.server.js"></script>
</body>
</html>
```

- [ ] **Step 2: Add dashboard page**

Create `AutoWeldSystem.CenterServer/Pages/Dashboard.razor`:

```razor
@page "/Dashboard"
@using AutoWeldSystem.Core.DTOs.CenterServer
@inject AutoWeldSystem.CenterServer.Services.CenterDashboardQueryService DashboardQuery

<main class="dashboard">
    <header class="dashboard-header">
        <div>
            <h1>中心服务器看板</h1>
            <p>实时汇总单稳态型电磁系统点焊、磁保持型电磁系统点焊、轭铁组自动点焊设备状态。</p>
        </div>
        <div class="timestamp">@_snapshot.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss")</div>
    </header>

    <section class="summary-band">
        <div><strong>@_snapshot.Devices.Count</strong><span>设备数</span></div>
        <div><strong>@_snapshot.Devices.Count(it => it.Online)</strong><span>在线</span></div>
        <div><strong>@_snapshot.Devices.Sum(it => it.TodayProductCount)</strong><span>今日产品</span></div>
        <div><strong>@_snapshot.Devices.Sum(it => it.TodayFailedCount)</strong><span>今日异常</span></div>
    </section>

    <section class="device-grid">
        @foreach (var device in _snapshot.Devices)
        {
            <article class="device-card @(device.Online ? "online" : "offline")">
                <div class="device-title">
                    <h2>@device.DeviceName</h2>
                    <span>@(device.Online ? "在线" : "离线")</span>
                </div>
                <div class="device-meta">@device.DeviceId · @FormatDeviceType(device.DeviceType)</div>
                <dl>
                    <dt>PLC</dt><dd>@device.Runtime.PlcConnectionState</dd>
                    <dt>设备状态</dt><dd>@device.Runtime.DeviceStatusName (@device.Runtime.DeviceStatusCode)</dd>
                    <dt>工单</dt><dd>@(device.CurrentTask?.SN ?? "-")</dd>
                    <dt>程序</dt><dd>@(device.CurrentTask?.ProgramName ?? "-")</dd>
                    <dt>今日产量</dt><dd>@device.TodayProductCount / OK @device.TodayQualifiedCount / NG @device.TodayFailedCount</dd>
                    <dt>报警</dt><dd>@(string.IsNullOrWhiteSpace(device.Runtime.AlarmMessage) ? "-" : device.Runtime.AlarmMessage)</dd>
                </dl>
            </article>
        }
    </section>
</main>

@code {
    private CenterDashboardSnapshotDto _snapshot = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(3));

    protected override async Task OnInitializedAsync()
    {
        _snapshot = DashboardQuery.GetSnapshot();
        _ = RefreshLoopAsync();
        await Task.CompletedTask;
    }

    private async Task RefreshLoopAsync()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            _snapshot = DashboardQuery.GetSnapshot();
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string FormatDeviceType(string deviceType)
    {
        return deviceType switch
        {
            AutoWeldSystem.Core.Constants.CenterServerConstants.DeviceTypes.MonostableElectromagneticSpotWeld => "单稳态型电磁系统点焊",
            AutoWeldSystem.Core.Constants.CenterServerConstants.DeviceTypes.MagneticLatchingElectromagneticSpotWeld => "磁保持型电磁系统点焊",
            AutoWeldSystem.Core.Constants.CenterServerConstants.DeviceTypes.YokeAutomaticSpotWeld => "轭铁组自动点焊",
            _ => deviceType
        };
    }
}
```

- [ ] **Step 3: Add dashboard CSS**

Create `AutoWeldSystem.CenterServer/wwwroot/css/center-dashboard.css`:

```css
body {
    margin: 0;
    font-family: "Microsoft YaHei", "Segoe UI", sans-serif;
    background: #f4f6f8;
    color: #1f2933;
}

.dashboard {
    padding: 20px;
}

.dashboard-header {
    display: flex;
    align-items: flex-end;
    justify-content: space-between;
    margin-bottom: 16px;
}

.dashboard-header h1 {
    margin: 0 0 6px;
    font-size: 28px;
}

.dashboard-header p {
    margin: 0;
    color: #52616b;
}

.timestamp {
    color: #52616b;
}

.summary-band {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 12px;
    margin-bottom: 16px;
}

.summary-band div,
.device-card {
    background: #ffffff;
    border: 1px solid #d9e2ec;
    border-radius: 8px;
}

.summary-band div {
    padding: 16px;
}

.summary-band strong {
    display: block;
    font-size: 26px;
}

.summary-band span {
    color: #52616b;
}

.device-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
    gap: 12px;
}

.device-card {
    padding: 16px;
}

.device-card.online {
    border-left: 5px solid #15803d;
}

.device-card.offline {
    border-left: 5px solid #9aa5b1;
}

.device-title {
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.device-title h2 {
    margin: 0;
    font-size: 18px;
}

.device-title span {
    font-weight: 600;
}

.device-meta {
    margin: 6px 0 12px;
    color: #52616b;
}

dl {
    display: grid;
    grid-template-columns: 92px 1fr;
    gap: 8px 12px;
    margin: 0;
}

dt {
    color: #52616b;
}

dd {
    margin: 0;
}
```

- [ ] **Step 4: Build and run**

Run:

```powershell
dotnet build AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj --no-restore
dotnet run --project AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj
```

Expected: server listens on `http://localhost:7099`, dashboard opens at `http://localhost:7099/Dashboard`.

- [ ] **Step 5: Commit**

```powershell
git add AutoWeldSystem.CenterServer\Pages AutoWeldSystem.CenterServer\wwwroot
git commit -m "feat(center): add real-time dashboard page"
```

---

## Task 7: Add Device-Side Center Settings

**Files:**
- Modify: `AutoWeldSystem.Core/Entities/AppSettings.cs`
- Modify: `AutoWeldSystem.Services/AppSettingsService.cs`
- Modify: `AutoWeldSystem.UI/Views/SystemSettingView.Designer.cs`
- Modify: `AutoWeldSystem.UI/Views/SystemSettingView.cs`

- [ ] **Step 1: Add settings fields**

Add this region to `AppSettings` before `#region MES配置`:

```csharp
#region 中心服务器配置

[SugarColumn(ColumnDescription = "是否启用中心服务器同步")]
public bool EnableCenterServerSync { get; set; }

[SugarColumn(Length = 300, ColumnDescription = "中心服务器地址")]
public string CenterServerBaseUrl { get; set; } = "http://127.0.0.1:7099/";

[SugarColumn(Length = 80, ColumnDescription = "中心服务器设备类型")]
public string CenterServerDeviceType { get; set; } = CenterServerConstants.DeviceTypes.MonostableElectromagneticSpotWeld;

[SugarColumn(ColumnDescription = "中心服务器同步间隔秒")]
public int CenterServerSyncIntervalSeconds { get; set; } = CenterServerConstants.DefaultUploadIntervalSeconds;

#endregion
```

- [ ] **Step 2: Normalize settings**

In `AppSettingsService.Save`, normalize these fields before saving:

```csharp
settings.CenterServerBaseUrl = NormalizeUrl(settings.CenterServerBaseUrl, "http://127.0.0.1:7099/");
settings.CenterServerDeviceType = NormalizeCenterDeviceType(settings.CenterServerDeviceType);
settings.CenterServerSyncIntervalSeconds = Math.Clamp(settings.CenterServerSyncIntervalSeconds, 2, 60);
```

Add helpers:

```csharp
private static string NormalizeUrl(string? value, string fallback)
{
    var normalized = value?.Trim();
    return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized.TrimEnd('/') + "/";
}

private static string NormalizeCenterDeviceType(string? value)
{
    return value?.Trim() switch
    {
        CenterServerConstants.DeviceTypes.MonostableElectromagneticSpotWeld => CenterServerConstants.DeviceTypes.MonostableElectromagneticSpotWeld,
        CenterServerConstants.DeviceTypes.MagneticLatchingElectromagneticSpotWeld => CenterServerConstants.DeviceTypes.MagneticLatchingElectromagneticSpotWeld,
        CenterServerConstants.DeviceTypes.YokeAutomaticSpotWeld => CenterServerConstants.DeviceTypes.YokeAutomaticSpotWeld,
        _ => CenterServerConstants.DeviceTypes.MonostableElectromagneticSpotWeld
    };
}
```

- [ ] **Step 3: Add UI controls in designer**

Add controls to the system settings page under a new “中心服务器” group:

```csharp
private AntdUI.Checkbox chkEnableCenterServerSync;
private AntdUI.Input inputCenterServerBaseUrl;
private AntdUI.Select selectCenterServerDeviceType;
private AntdUI.InputNumber inputCenterServerSyncIntervalSeconds;
```

Controls must be added in `SystemSettingView.Designer.cs`, not in `SystemSettingView.cs`.

- [ ] **Step 4: Bind UI values**

In `SystemSettingView.cs`, load settings:

```csharp
chkEnableCenterServerSync.Checked = settings.EnableCenterServerSync;
inputCenterServerBaseUrl.Text = settings.CenterServerBaseUrl;
BindCenterServerDeviceTypes(settings.CenterServerDeviceType);
inputCenterServerSyncIntervalSeconds.Value = settings.CenterServerSyncIntervalSeconds;
```

Save settings:

```csharp
settings.EnableCenterServerSync = chkEnableCenterServerSync.Checked;
settings.CenterServerBaseUrl = inputCenterServerBaseUrl.Text.Trim();
settings.CenterServerDeviceType = GetSelectedCenterDeviceType();
settings.CenterServerSyncIntervalSeconds = Convert.ToInt32(inputCenterServerSyncIntervalSeconds.Value);
```

Add device type options:

```csharp
private static readonly (string Value, string Text)[] CenterDeviceTypeOptions =
{
    (CenterServerConstants.DeviceTypes.MonostableElectromagneticSpotWeld, "单稳态型电磁系统点焊"),
    (CenterServerConstants.DeviceTypes.MagneticLatchingElectromagneticSpotWeld, "磁保持型电磁系统点焊"),
    (CenterServerConstants.DeviceTypes.YokeAutomaticSpotWeld, "轭铁组自动点焊")
};
```

- [ ] **Step 5: Build**

Run:

```powershell
dotnet build AutoWeldSystem.sln --no-restore
```

Expected: PASS. If UI output files are locked, use:

```powershell
$out = Join-Path $env:TEMP 'AutoWeldSystemCodexBuild\'
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=$out
```

- [ ] **Step 6: Commit**

```powershell
git add AutoWeldSystem.Core\Entities\AppSettings.cs AutoWeldSystem.Services\AppSettingsService.cs AutoWeldSystem.UI\Views\SystemSettingView.cs AutoWeldSystem.UI\Views\SystemSettingView.Designer.cs
git commit -m "feat(center): add device center server settings"
```

---

## Task 8: Implement Device-Side Telemetry Client

**Files:**
- Create: `AutoWeldSystem.Core/Interfaces/ICenterTelemetrySyncService.cs`
- Create: `AutoWeldSystem.Services/Center/CenterTelemetryClient.cs`
- Create: `AutoWeldSystem.Services/Center/CenterTelemetrySyncService.cs`
- Modify: `AutoWeldSystem.UI/Program.cs`

- [ ] **Step 1: Add sync service interface**

Create `AutoWeldSystem.Core/Interfaces/ICenterTelemetrySyncService.cs`:

```csharp
namespace AutoWeldSystem.Core.Interfaces;

/// <summary>
/// Background service that pushes local equipment telemetry to the center server.
/// </summary>
public interface ICenterTelemetrySyncService : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task PushOnceAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Add HTTP client**

Create `AutoWeldSystem.Services/Center/CenterTelemetryClient.cs`:

```csharp
using System.Net.Http.Json;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;

namespace AutoWeldSystem.Services.Center;

/// <summary>
/// HTTP client used by equipment software to upload telemetry to the center server.
/// </summary>
public sealed class CenterTelemetryClient
{
    private readonly HttpClient _httpClient;
    private readonly IAppSettingsService _settingsService;

    public CenterTelemetryClient(HttpClient httpClient, IAppSettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<CenterTelemetryAck> UploadAsync(CenterTelemetryBatchRequest request, CancellationToken cancellationToken)
    {
        var settings = _settingsService.Get();
        _httpClient.BaseAddress = new Uri(settings.CenterServerBaseUrl);
        var response = await _httpClient.PostAsJsonAsync("api/center/telemetry/batch", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CenterTelemetryAck>(cancellationToken: cancellationToken)
            ?? new CenterTelemetryAck { Success = false, Message = "Center server returned empty response." };
    }
}
```

- [ ] **Step 3: Add background sync service**

Create `AutoWeldSystem.Services/Center/CenterTelemetrySyncService.cs`:

```csharp
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.DTOs.CenterServer;
using AutoWeldSystem.Core.Entities;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Data;
using SqlSugar;

namespace AutoWeldSystem.Services.Center;

/// <summary>
/// Periodically uploads local device status, active tasks, and new collection records to the center server.
/// </summary>
public sealed class CenterTelemetrySyncService : ICenterTelemetrySyncService
{
    private readonly SqlSugarDbContext _dbContext;
    private readonly IAppSettingsService _settingsService;
    private readonly CenterTelemetryClient _client;
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private int _lastUploadedRecordId;

    public CenterTelemetrySyncService(SqlSugarDbContext dbContext, IAppSettingsService settingsService, CenterTelemetryClient client)
    {
        _dbContext = dbContext;
        _settingsService = settingsService;
        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask is not null)
        {
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
        {
            return;
        }

        await _cts.CancelAsync();
        if (_loopTask is not null)
        {
            await _loopTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }

        _cts.Dispose();
        _cts = null;
        _loopTask = null;
    }

    public async Task PushOnceAsync(CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Get();
        if (!settings.EnableCenterServerSync || string.IsNullOrWhiteSpace(settings.DeviceId))
        {
            return;
        }

        var request = BuildRequest(settings);
        var response = await _client.UploadAsync(request, cancellationToken);
        if (response.Success && request.WeldPoints.Count > 0)
        {
            _lastUploadedRecordId = Math.Max(_lastUploadedRecordId, request.WeldPoints.Max(it => it.LocalRecordId));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PushOnceAsync(cancellationToken);
            }
            catch
            {
                // Center sync must never block local production.
            }

            var settings = _settingsService.Get();
            var delaySeconds = Math.Clamp(settings.CenterServerSyncIntervalSeconds, 2, 60);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }
    }

    private CenterTelemetryBatchRequest BuildRequest(AppSettings settings)
    {
        var latestStatus = _dbContext.Db.Queryable<BizDeviceStatusLog>()
            .OrderByDescending(it => it.OccurredTime)
            .First();
        var activeTasks = _dbContext.Db.Queryable<BizWeldTask>()
            .Where(it => it.EndTime == null)
            .OrderByDescending(it => it.StartTime)
            .ToList();
        var newRecords = _dbContext.Db.Queryable<BizWeldPointRecord>()
            .Where(it => it.Id > _lastUploadedRecordId)
            .OrderBy(it => it.Id)
            .Take(200)
            .ToList();

        return new CenterTelemetryBatchRequest
        {
            DeviceId = settings.DeviceId.Trim(),
            DeviceName = settings.DeviceName.Trim(),
            DeviceType = settings.CenterServerDeviceType.Trim(),
            SentAt = DateTime.Now,
            Runtime = ToRuntime(latestStatus),
            Tasks = activeTasks.Select(ToTaskDto).ToList(),
            WeldPoints = newRecords.Select(ToRecordDto).ToList()
        };
    }

    private static CenterDeviceRuntimeDto ToRuntime(BizDeviceStatusLog? status)
    {
        return new CenterDeviceRuntimeDto
        {
            PlcConnected = true,
            PlcConnectionState = "Connected",
            StationNo = status?.StationNo ?? 0,
            DeviceStatusCode = status?.DeviceStatus ?? string.Empty,
            DeviceStatusName = status?.StatusName ?? string.Empty,
            AlarmMessage = status?.Remark ?? string.Empty,
            CollectedAt = status?.OccurredTime ?? DateTime.Now
        };
    }

    private static CenterWeldTaskDto ToTaskDto(BizWeldTask task)
    {
        return new CenterWeldTaskDto
        {
            LocalTaskId = task.Id,
            StationNo = task.StationNo,
            SN = task.SN,
            ProductNum = task.ProductNum,
            ProductModel = task.ProductModel,
            ProcessNo = task.ProcessNo,
            ProgramId = task.ProgramId ?? string.Empty,
            ProgramName = task.ProgramName ?? string.Empty,
            RecipeCode = task.RecipeCode ?? string.Empty,
            TaskStatus = task.TaskStatus,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            ActualQty = task.ActualQty,
            QualifiedQty = task.QualifiedQty,
            FailedQty = task.FailedQty
        };
    }

    private static CenterWeldPointRecordDto ToRecordDto(BizWeldPointRecord record)
    {
        return new CenterWeldPointRecordDto
        {
            LocalRecordId = record.Id,
            LocalTaskId = record.TaskId,
            StationNo = record.StationNo,
            SN = record.SN,
            ProductNo = record.ProductNo,
            TouchNo = record.TouchNo,
            TestResult = record.TestResult,
            Type = record.Type,
            Ts = record.Ts,
            ProductCompleted = record.ProductCompleted,
            RawDataJson = record.RawDataJson ?? string.Empty
        };
    }
}
```

- [ ] **Step 4: Register client and service**

Modify `AutoWeldSystem.UI/Program.cs` in `ConfigureServices`:

```csharp
services.AddHttpClient<CenterTelemetryClient>();
services.AddSingleton<ICenterTelemetrySyncService, CenterTelemetrySyncService>();
```

Add `using AutoWeldSystem.Services.Center;`.

- [ ] **Step 5: Start and stop center sync with existing background services**

In `Program.Main`, add `var centerSyncStarted = false;`, start after production services:

```csharp
AppHost.Services.GetRequiredService<ICenterTelemetrySyncService>().StartAsync().GetAwaiter().GetResult();
centerSyncStarted = true;
```

Update `StopBackgroundServices(...)` to accept `centerSyncStarted` and stop:

```csharp
if (centerSyncStarted)
{
    AppHost?.Services.GetRequiredService<ICenterTelemetrySyncService>().StopAsync().GetAwaiter().GetResult();
}
```

- [ ] **Step 6: Build**

Run:

```powershell
$out = Join-Path $env:TEMP 'AutoWeldSystemCodexBuild\'
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=$out
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add AutoWeldSystem.Core\Interfaces\ICenterTelemetrySyncService.cs AutoWeldSystem.Services\Center AutoWeldSystem.UI\Program.cs
git commit -m "feat(center): upload device telemetry to center server"
```

---

## Task 9: Integration Validation

**Files:**
- Modify only if tests expose compile/runtime defects in files from earlier tasks.

- [ ] **Step 1: Build all projects**

Run:

```powershell
$out = Join-Path $env:TEMP 'AutoWeldSystemCodexBuild\'
dotnet build AutoWeldSystem.sln --no-restore -p:BaseOutputPath=$out
```

Expected: `0 Error(s)`.

- [ ] **Step 2: Start center server**

Run:

```powershell
dotnet run --project AutoWeldSystem.CenterServer\AutoWeldSystem.CenterServer.csproj
```

Expected: console shows listening URL `http://0.0.0.0:7099`.

- [ ] **Step 3: Post a sample telemetry batch**

Run:

```powershell
$body = @{
  deviceId = "DEV-001"
  deviceName = "单稳态电磁点焊-01"
  deviceType = "MonostableElectromagneticSpotWeld"
  sentAt = (Get-Date).ToString("o")
  runtime = @{
    plcConnected = $true
    plcConnectionState = "Connected"
    stationNo = 1
    deviceStatusCode = "1"
    deviceStatusName = "运行"
    alarmMessage = ""
    actualQty = 12
    qualifiedQty = 11
    failedQty = 1
    collectedAt = (Get-Date).ToString("o")
  }
  tasks = @()
  weldPoints = @()
} | ConvertTo-Json -Depth 8

Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:7099/api/center/telemetry/batch" -Body $body -ContentType "application/json"
```

Expected: response has `success=true`.

- [ ] **Step 4: Verify dashboard snapshot**

Run:

```powershell
Invoke-RestMethod -Method Get -Uri "http://127.0.0.1:7099/api/center/dashboard/snapshot"
```

Expected: response contains device `DEV-001` and runtime status `运行`.

- [ ] **Step 5: Verify browser dashboard**

Open `http://127.0.0.1:7099/Dashboard`.

Expected: one device card appears, status is online, production totals match sample telemetry.

- [ ] **Step 6: Commit fixes**

```powershell
git status --short
git add <files changed during validation>
git commit -m "fix(center): stabilize telemetry dashboard integration"
```

---

## Acceptance Criteria

- Center server runs independently from equipment clients.
- Dashboard shows each device card with online/offline state, equipment type, PLC state, device status, current work order, daily product count, OK/NG counts, and alarm text.
- Device client can enable/disable center sync from System Settings.
- Device client production flow continues if center server is offline.
- Telemetry upload is idempotent for tasks and weld-point records by `DeviceId + LocalId`.
- Center server stores data in MySQL and can restart without losing dashboard history.
- Existing MES upload behavior is unchanged.
- Existing MonitorView, data history, report generation, PLC collection, and device-status upload still build.

## Explicit Defaults

- Center server UI is web-based Blazor Server.
- Device-to-center transport is HTTP POST for persistence plus SignalR for dashboard refresh.
- Database is MySQL through SqlSugar.
- Default center URL is `http://127.0.0.1:7099/`.
- Default sync interval is 5 seconds.
- Device type options are:
  - 单稳态型电磁系统点焊
  - 磁保持型电磁系统点焊
  - 轭铁组自动点焊

## Self-Review

- Spec coverage: center server project, real-time device status dashboard, summarized storage, and device-to-server data transfer are covered by Tasks 1-9.
- Placeholder scan: no `TBD`, `TODO`, or unspecified implementation steps remain.
- Type consistency: DTO names, entity names, service names, and file paths are consistent across tasks.
