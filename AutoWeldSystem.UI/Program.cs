using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Interfaces.UserManage;
using AutoWeldSystem.Core.Runtime;
using AutoWeldSystem.Data;
using AutoWeldSystem.Services;
using AutoWeldSystem.Services.Center;
using AutoWeldSystem.Services.Log;
using AutoWeldSystem.Services.Mes;
using AutoWeldSystem.Services.Plc;
using AutoWeldSystem.Services.Production;
using AutoWeldSystem.UI.Forms;
using AutoWeldSystem.UI.Infrastructure;
using AutoWeldSystem.UI.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoWeldSystem.UI;

public static class Program
{
    public static IHost? AppHost { get; private set; }

    [STAThread]
    private static void Main()
    {
        using var singleInstanceMutex = TryAcquireSingleInstance();
        if (singleInstanceMutex is null)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        var plcServiceStarted = false;
        var mesMonitorStarted = false;
        var productionMonitorStarted = false;
        var workIdMonitorStarted = false;
        var weldCycleMonitorStarted = false;
        var recipeReconcileMonitorStarted = false;
        var realtimePreviewStarted = false;
        var centerTelemetrySyncStarted = false;
        var centerProductForwardingStarted = false;
        var deviceLifecycleLogStarted = false;
        var deviceApiServerStarted = false;

        try
        {
            var localConfiguration = PrepareLocalConfiguration();
            if (localConfiguration is null)
            {
                return;
            }

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(configuration => UseLocalConfigurationFile(configuration, localConfiguration.FilePath))
                .ConfigureServices(services =>
                {
                    services.AddSingleton(provider =>
                    {
                        var configuration = provider.GetRequiredService<IConfiguration>();
                        return new SqlSugarDbContext(configuration[LocalConfigurationFile.ConnectionStringKey]);
                    });
                    services.AddSingleton<IRbacService, RbacService>();
                    services.AddSingleton<ISysUserService, SysUserService>();
                    services.AddSingleton<IAppSettingsService, AppSettingsService>();
                    services.AddSingleton<IPlcAddressService, AddressService>();
                    services.AddSingleton<IPlcAlarmAddressService, PlcAlarmAddressService>();
                    services.AddSingleton<IPlcAlarmAcknowledgementService, PlcAlarmAcknowledgementService>();
                    services.AddSingleton<IPlcRecipeNameConfigService, PlcRecipeNameConfigService>();
                    services.AddSingleton<IPlcRecipeNameReaderService, PlcRecipeNameReaderService>();
                    services.AddSingleton<IPlcBusinessSignalService, BusinessSignalService>();
                    services.AddSingleton<IOperationLogService, OperationLogService>();
                    services.AddSingleton<IMesInteractionLogService, MesInteractionLogService>();
                    services.AddSingleton<ICenterInteractionLogService, CenterInteractionLogService>();
                    services.AddSingleton<IProductionFlowLogService, ProductionFlowLogService>();
                    services.AddSingleton<IProgramExceptionLogService, ProgramExceptionLogService>();
                    services.AddSingleton<IDeviceLifecycleLogService, DeviceLifecycleLogService>();
                    services.AddSingleton<IDeviceLifecycleLogCoordinator, DeviceLifecycleLogCoordinator>();
                    services.AddSingleton<IUiThreadDispatcher, WinFormsUiThreadDispatcher>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();
                    services.AddSingleton<IWindowsShellIntegrationService, WindowsShellIntegrationService>();
                    services.AddSingleton<PlcWriteDebugLauncher>();
                    services.AddSingleton<ISystemClockService, WindowsSystemClockService>();
                    services.AddSingleton<IDeviceApiEndpointService, DeviceApiEndpointService>();
                    services.AddSingleton<IDeviceApiServerService, DeviceApiServerService>();
                    services.AddSingleton<IWeldTaskService, WeldTaskService>();
                    services.AddSingleton<IProgramManageService, ProgramManageService>();
                    services.AddSingleton<IPlcCommunicationService, CommunicationService>();
                    services.AddSingleton<IPlcExpressionReadService, ExpressionReadService>();
                    services.AddSingleton<IMesConnectionMonitor, MesConnectionMonitor>();
                    services.AddSingleton<IPlcProductionMonitorService, ProductionMonitorService>();
                    services.AddSingleton<IPlcWorkIdMonitorService, WorkIdMonitorService>();
                    services.AddSingleton<IPlcWeldCycleMonitorService, WeldCycleMonitorService>();
                    services.AddSingleton<IPlcRecipeReconcileMonitorService, RecipeCodeReconcileMonitorService>();
                    services.AddSingleton<IProductProcessConfigService, ProductProcessConfigService>();
                    services.AddSingleton<ITestSchemeConfigService, TestSchemeConfigService>();
                    services.AddSingleton<IProductCycleCollectionService, ProductCycleCollectionService>();
                    // 采集准入协调器按数据库上下文共享，采集、完工与异常结束必须看到同一份在途登记。
                    services.AddSingleton<ITaskCollectionLifecycleCoordinator>(provider =>
                        TaskCollectionLifecycleCoordinator.GetShared(
                            provider.GetRequiredService<SqlSugarDbContext>(),
                            provider.GetRequiredService<IAppSettingsService>()));
                    services.AddSingleton<IProductRealtimePreviewService, ProductRealtimePreviewService>();
                    services.AddSingleton<IProductHistoryService, ProductHistoryService>();
                    services.AddSingleton<IProductionCountService, ProductionCountService>();
                    services.AddSingleton<IWeldPointUploadCoordinatorService, WeldPointUploadCoordinatorService>();
                    services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
                    services.AddSingleton<IRuntimeTipStateService, RuntimeTipStateService>();
                    services.AddSingleton<IUploadTaskService, UploadTaskService>();
                    services.AddSingleton<IUploadStatusSummaryService, UploadStatusSummaryService>();
                    services.AddSingleton<IProductionReportFileService, ProductionReportFileService>();
                    services.AddSingleton<IDataHistoryQueryService, DataHistoryQueryService>();
                    services.AddSingleton<IDataHistoryMaintenanceService, DataHistoryMaintenanceService>();
                    services.AddSingleton<ICenterTelemetrySyncService, CenterTelemetrySyncService>();
                    services.AddSingleton<ICenterProductForwardingService, CenterProductForwardingService>();
                    services.AddTransient<PermissionUiBinder>();

                    services.AddHttpClient<IMesProvider, MesProvider>();
                    services.AddSingleton<CenterServerAvailabilityLogGate>();
                    services.AddHttpClient<CenterTelemetryClient>();

                    services.AddTransient<LoginForm>();
                    services.AddTransient<MainForm>();
                    services.AddTransient<PlcWriteDebugForm>();
                    services.AddTransient<OperatorInputForm>();
                    services.AddTransient<RoleEditForm>();
                    services.AddTransient<UserEditForm>();
                    services.AddTransient<MonitorView>();
                    services.AddTransient<DataManageView>();
                    services.AddTransient<UserManageView>();
                    services.AddTransient<ProgramManageView>();
                    services.AddTransient<LogManageView>();
                    services.AddTransient<StateManageView>();
                    services.AddTransient<SystemSettingView>();
                    services.AddTransient<AddressManageView>();
                })
                .Build();

            if (string.IsNullOrWhiteSpace(AppHost.Services.GetRequiredService<IConfiguration>()[LocalConfigurationFile.ConnectionStringKey]))
            {
                ShowStartupMessage(
                    $"配置文件缺少数据库连接串 {LocalConfigurationFile.ConnectionStringKey}，请填写后重新启动程序：\n\n{localConfiguration.FilePath}",
                    MessageBoxIcon.Warning);
                return;
            }

            InstallExceptionHandlers(AppHost.Services.GetRequiredService<IProgramExceptionLogService>());
            UiThreadDispatcherProvider.Configure(AppHost.Services.GetRequiredService<IUiThreadDispatcher>());
            AppHost.Services.GetRequiredService<ISysUserService>().InitDb();
            AppHost.Services.GetRequiredService<ILocalizationService>();
            AppHost.Services.GetRequiredService<IWindowsShellIntegrationService>().ApplyStartupIntegration();
            TrySyncStartupServerTime();
            AppHost.Services.GetRequiredService<IDeviceLifecycleLogCoordinator>().Start();
            deviceLifecycleLogStarted = true;
            AppHost.Services.GetRequiredService<IDeviceApiServerService>().StartAsync().GetAwaiter().GetResult();
            deviceApiServerStarted = true;
            AppHost.Services.GetRequiredService<IPlcCommunicationService>().StartAsync().GetAwaiter().GetResult();
            plcServiceStarted = true;
            AppHost.Services.GetRequiredService<IMesConnectionMonitor>().StartAsync().GetAwaiter().GetResult();
            mesMonitorStarted = true;
            AppHost.Services.GetRequiredService<IPlcProductionMonitorService>().StartAsync().GetAwaiter().GetResult();
            productionMonitorStarted = true;
            AppHost.Services.GetRequiredService<IPlcWorkIdMonitorService>().StartAsync().GetAwaiter().GetResult();
            workIdMonitorStarted = true;
            // 先构造实时预览服务，确保它能收到焊接周期监控启动后的首个产品就绪事件。
            AppHost.Services.GetRequiredService<IProductRealtimePreviewService>();
            AppHost.Services.GetRequiredService<IPlcWeldCycleMonitorService>().StartAsync().GetAwaiter().GetResult();
            weldCycleMonitorStarted = true;
            AppHost.Services.GetRequiredService<IPlcRecipeReconcileMonitorService>().StartAsync().GetAwaiter().GetResult();
            recipeReconcileMonitorStarted = true;
            AppHost.Services.GetRequiredService<IProductRealtimePreviewService>().StartAsync().GetAwaiter().GetResult();
            realtimePreviewStarted = true;
            AppHost.Services.GetRequiredService<ICenterTelemetrySyncService>().StartAsync().GetAwaiter().GetResult();
            centerTelemetrySyncStarted = true;
            AppHost.Services.GetRequiredService<ICenterProductForwardingService>().StartAsync().GetAwaiter().GetResult();
            centerProductForwardingStarted = true;

            while (true)
            {
                var loginForm = AppHost.Services.GetRequiredService<LoginForm>();
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    break;
                }

                var mainForm = AppHost.Services.GetRequiredService<MainForm>();
                Application.Run(mainForm);

                if (!GlobalContext.IsLogout)
                {
                    break;
                }

                GlobalContext.IsLogout = false;
                GlobalContext.Clear();
            }
        }
        catch (Exception ex)
        {
            TryLogProgramException(ex, "Startup");
            WriteStartupFallbackLog(ex);
            ShowStartupError(ex);
        }
        finally
        {
            StopBackgroundServices(
                deviceApiServerStarted,
                deviceLifecycleLogStarted,
                centerProductForwardingStarted,
                centerTelemetrySyncStarted,
                realtimePreviewStarted,
                recipeReconcileMonitorStarted,
                weldCycleMonitorStarted,
                workIdMonitorStarted,
                productionMonitorStarted,
                mesMonitorStarted,
                plcServiceStarted);
            AppHost?.Dispose();
        }
    }

    private static Mutex? TryAcquireSingleInstance()
    {
        try
        {
            var mutex = new Mutex(
                initiallyOwned: true,
                name: @"Global\AutoWeldSystem",
                createdNew: out var createdNew);
            if (createdNew)
            {
                return mutex;
            }

            mutex.Dispose();
            MessageBox.Show(
                "AutoWeldSystem 已经在运行，不能重复启动。",
                AppConstants.ApplicationName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return null;
        }
        catch (Exception ex)
        {
            // 无法确认单实例状态时拒绝启动，避免多个进程同时连接 PLC。
            MessageBox.Show(
                $"无法确认 AutoWeldSystem 是否已在运行，程序将退出。\n\n{ex.Message}",
                AppConstants.ApplicationName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return null;
        }
    }

    /// <summary>
    /// 数据库连接配置固定读取 ProgramData 独立目录，现场只替换 exe 更新时不会被覆盖。
    /// 找不到配置时生成模板并提示退出，不再静默回退到代码内置的默认连接串。
    /// </summary>
    private static LocalConfigurationResult? PrepareLocalConfiguration()
    {
        var configDirectory = LocalConfigurationFile.ResolveDirectory(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        var result = LocalConfigurationFile.Prepare(configDirectory, AppContext.BaseDirectory, ReadConfigurationTemplate());
        if (result.State != LocalConfigurationState.TemplateCreated)
        {
            return result;
        }

        ShowStartupMessage(
            $"未找到数据库配置文件，已生成模板：\n\n{result.FilePath}\n\n请填写 MySQL 连接串后重新启动程序。",
            MessageBoxIcon.Warning);
        return null;
    }

    /// <summary>
    /// 模板内容取自嵌入 exe 的 appsettings.example.json，与仓库示例文件保持同一份。
    /// </summary>
    private static string ReadConfigurationTemplate()
    {
        using var stream = typeof(Program).Assembly.GetManifestResourceStream("appsettings.example.json")
            ?? throw new InvalidOperationException("程序内未嵌入配置模板 appsettings.example.json。");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 只读取 ProgramData 下的配置文件：移除默认按工作目录查找的 appsettings.json，
    /// 避免程序目录残留的旧配置与现场实际配置不一致。
    /// </summary>
    private static void UseLocalConfigurationFile(IConfigurationBuilder configuration, string filePath)
    {
        foreach (var source in configuration.Sources.OfType<JsonConfigurationSource>().ToList())
        {
            configuration.Sources.Remove(source);
        }

        configuration.AddJsonFile(filePath, optional: false, reloadOnChange: false);
    }

    private static void TrySyncStartupServerTime()
    {
        try
        {
            // 先等待校时，再生成开机时间；在线程池执行，避免同步等待阻塞 UI 上下文中的异步续体。
            Task.Run(() => AppHost!.Services.GetRequiredService<IWeldTaskService>().SyncServerTimeAsync())
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // MES 离线或校时失败不能阻断离线启动，开机记录继续使用当前本机时间。
            TryLogProgramException(ex, "Startup.ServerTimeSync");
        }
    }

    private static void InstallExceptionHandlers(IProgramExceptionLogService exceptionLogService)
    {
        Application.ThreadException += (_, e) =>
        {
            exceptionLogService.Write(e.Exception, "Application.ThreadException");
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                exceptionLogService.Write(exception, "AppDomain.UnhandledException", $"IsTerminating: {e.IsTerminating}");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            exceptionLogService.Write(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    private static void TryLogProgramException(Exception exception, string source)
    {
        try
        {
            AppHost?.Services.GetService<IProgramExceptionLogService>()?.Write(exception, source);
        }
        catch
        {
            // 启动阶段异常日志写入失败时，仍然优先显示原始启动错误。
        }
    }

    /// <summary>
    /// 启动早期可能还没有成功连接数据库，此时普通异常日志无法读取系统设置目录。
    /// 这里写入程序目录下的兜底日志，方便工控机现场直接定位启动失败原因。
    /// </summary>
    private static void WriteStartupFallbackLog(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs", "startup");
            Directory.CreateDirectory(logDirectory);
            var filePath = Path.Combine(logDirectory, "startup-fatal.log");
            var content = string.Join(
                Environment.NewLine,
                $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                $"Machine: {Environment.MachineName}",
                $"User: {Environment.UserName}",
                $"BaseDirectory: {AppContext.BaseDirectory}",
                $"ExceptionType: {exception.GetType().FullName}",
                $"Message: {exception.Message}",
                "StackTrace:",
                exception.ToString(),
                new string('-', 80),
                string.Empty);

            File.AppendAllText(filePath, content, System.Text.Encoding.UTF8);
        }
        catch
        {
            // 兜底日志也不能影响启动异常弹窗。
        }
    }

    private static void StopBackgroundServices(
        bool deviceApiServerStarted,
        bool deviceLifecycleLogStarted,
        bool centerProductForwardingStarted,
        bool centerTelemetrySyncStarted,
        bool realtimePreviewStarted,
        bool recipeReconcileMonitorStarted,
        bool weldCycleMonitorStarted,
        bool workIdMonitorStarted,
        bool productionMonitorStarted,
        bool mesMonitorStarted,
        bool plcServiceStarted)
    {
        if (deviceApiServerStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IDeviceApiServerService>().StopAsync().GetAwaiter().GetResult());
        }

        if (productionMonitorStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IPlcProductionMonitorService>().StopAsync().GetAwaiter().GetResult());
        }

        if (centerProductForwardingStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<ICenterProductForwardingService>().StopAsync().GetAwaiter().GetResult());
        }

        if (centerTelemetrySyncStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<ICenterTelemetrySyncService>().StopAsync().GetAwaiter().GetResult());
        }

        if (realtimePreviewStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IProductRealtimePreviewService>().StopAsync().GetAwaiter().GetResult());
        }

        if (recipeReconcileMonitorStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IPlcRecipeReconcileMonitorService>().StopAsync().GetAwaiter().GetResult());
        }

        if (weldCycleMonitorStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IPlcWeldCycleMonitorService>().StopAsync().GetAwaiter().GetResult());
        }

        if (workIdMonitorStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IPlcWorkIdMonitorService>().StopAsync().GetAwaiter().GetResult());
        }

        if (deviceLifecycleLogStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IDeviceLifecycleLogCoordinator>().Stop());
        }

        if (mesMonitorStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IMesConnectionMonitor>().StopAsync().GetAwaiter().GetResult());
        }

        if (plcServiceStarted)
        {
            TryStopBackgroundService(
                () => AppHost?.Services.GetRequiredService<IPlcCommunicationService>().StopAsync().GetAwaiter().GetResult());
        }
    }

    private static void TryStopBackgroundService(Action stop)
    {
        try
        {
            stop();
        }
        catch
        {
            // Shutdown cleanup should not replace the original UI/initialization error.
        }
    }

    private static void ShowStartupError(Exception ex)
    {
        // Fall back to the raw message only when DI/localization is not available yet.
        var localizer = AppHost?.Services.GetService<ILocalizationService>();
        var message = localizer is null
            ? $"Program initialization failed: {ex.Message}"
            : localizer.GetString(TextKeys.Common.StartupInitFailed, ex.Message);

        ShowStartupMessage(message, MessageBoxIcon.Error);
    }

    private static void ShowStartupMessage(string message, MessageBoxIcon icon)
    {
        MessageBox.Show(message, AppConstants.ApplicationName, MessageBoxButtons.OK, icon);
    }
}
