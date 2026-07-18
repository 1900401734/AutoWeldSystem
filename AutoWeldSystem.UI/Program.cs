using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Interfaces.Log;
using AutoWeldSystem.Core.Interfaces.MES;
using AutoWeldSystem.Core.Interfaces.PLC;
using AutoWeldSystem.Core.Interfaces.UserManage;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoWeldSystem.UI;

public static class Program
{
    public static IHost? AppHost { get; private set; }

    [STAThread]
    private static void Main()
    {
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
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(provider =>
                    {
                        var configuration = provider.GetRequiredService<IConfiguration>();
                        return new SqlSugarDbContext(configuration["Database:ConnectionString"]);
                    });
                    services.AddSingleton<IRbacService, RbacService>();
                    services.AddSingleton<ISysUserService, SysUserService>();
                    services.AddSingleton<IAppSettingsService, AppSettingsService>();
                    services.AddSingleton<IPlcAddressService, AddressService>();
                    services.AddSingleton<IPlcAlarmAddressService, PlcAlarmAddressService>();
                    services.AddSingleton<IPlcRecipeNameConfigService, PlcRecipeNameConfigService>();
                    services.AddSingleton<IPlcRecipeNameReaderService, PlcRecipeNameReaderService>();
                    services.AddSingleton<IPlcBusinessSignalService, BusinessSignalService>();
                    services.AddSingleton<IOperationLogService, OperationLogService>();
                    services.AddSingleton<IMesInteractionLogService, MesInteractionLogService>();
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
                    services.AddSingleton<IProductRealtimePreviewService, ProductRealtimePreviewService>();
                    services.AddSingleton<IProductHistoryService, ProductHistoryService>();
                    services.AddSingleton<IWeldPointUploadCoordinatorService, WeldPointUploadCoordinatorService>();
                    services.AddSingleton<IDeviceStatusService, DeviceStatusService>();
                    services.AddSingleton<IRuntimeTipStateService, RuntimeTipStateService>();
                    services.AddSingleton<IUploadTaskService, UploadTaskService>();
                    services.AddSingleton<IUploadStatusSummaryService, UploadStatusSummaryService>();
                    services.AddSingleton<IProductionReportFileService, ProductionReportFileService>();
                    services.AddSingleton<IDataHistoryQueryService, DataHistoryQueryService>();
                    services.AddSingleton<ICenterTelemetrySyncService, CenterTelemetrySyncService>();
                    services.AddSingleton<ICenterProductForwardingService, CenterProductForwardingService>();
                    services.AddTransient<PermissionUiBinder>();

                    services.AddHttpClient<IMesProvider, MesProvider>();
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

            InstallExceptionHandlers(AppHost.Services.GetRequiredService<IProgramExceptionLogService>());
            UiThreadDispatcherProvider.Configure(AppHost.Services.GetRequiredService<IUiThreadDispatcher>());
            AppHost.Services.GetRequiredService<ISysUserService>().InitDb();
            AppHost.Services.GetRequiredService<ILocalizationService>();
            AppHost.Services.GetRequiredService<IWindowsShellIntegrationService>().ApplyStartupIntegration();
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
            AppHost.Services.GetRequiredService<IDeviceLifecycleLogCoordinator>().Start();
            deviceLifecycleLogStarted = true;

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
        try
        {
            if (deviceApiServerStarted)
            {
                AppHost?.Services.GetRequiredService<IDeviceApiServerService>().StopAsync().GetAwaiter().GetResult();
            }

            if (deviceLifecycleLogStarted)
            {
                AppHost?.Services.GetRequiredService<IDeviceLifecycleLogCoordinator>().Stop();
            }

            if (centerProductForwardingStarted)
            {
                AppHost?.Services.GetRequiredService<ICenterProductForwardingService>().StopAsync().GetAwaiter().GetResult();
            }

            if (centerTelemetrySyncStarted)
            {
                AppHost?.Services.GetRequiredService<ICenterTelemetrySyncService>().StopAsync().GetAwaiter().GetResult();
            }

            if (realtimePreviewStarted)
            {
                AppHost?.Services.GetRequiredService<IProductRealtimePreviewService>().StopAsync().GetAwaiter().GetResult();
            }

            if (recipeReconcileMonitorStarted)
            {
                AppHost?.Services.GetRequiredService<IPlcRecipeReconcileMonitorService>().StopAsync().GetAwaiter().GetResult();
            }

            if (weldCycleMonitorStarted)
            {
                AppHost?.Services.GetRequiredService<IPlcWeldCycleMonitorService>().StopAsync().GetAwaiter().GetResult();
            }

            if (workIdMonitorStarted)
            {
                AppHost?.Services.GetRequiredService<IPlcWorkIdMonitorService>().StopAsync().GetAwaiter().GetResult();
            }

            if (productionMonitorStarted)
            {
                AppHost?.Services.GetRequiredService<IPlcProductionMonitorService>().StopAsync().GetAwaiter().GetResult();
            }

            if (mesMonitorStarted)
            {
                AppHost?.Services.GetRequiredService<IMesConnectionMonitor>().StopAsync().GetAwaiter().GetResult();
            }

            if (plcServiceStarted)
            {
                AppHost?.Services.GetRequiredService<IPlcCommunicationService>().StopAsync().GetAwaiter().GetResult();
            }
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
