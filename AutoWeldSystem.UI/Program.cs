using AutoWeldSystem.Core;
using AutoWeldSystem.Core.Constants;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Data;
using AutoWeldSystem.Services;
using AutoWeldSystem.Services.Logging;
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
                    services.AddSingleton<IPlcAddressService, PlcAddressService>();
                    services.AddSingleton<IOperationLogService, OperationLogService>();
                    services.AddSingleton<IMesInteractionLogService, MesInteractionLogService>();
                    services.AddSingleton<IProgramExceptionLogService, ProgramExceptionLogService>();
                    services.AddSingleton<ILocalizationService, LocalizationService>();
                    services.AddSingleton<IWeldTaskService, WeldTaskService>();
                    services.AddSingleton<IProgramManageService, ProgramManageService>();
                    services.AddSingleton<IPlcCommunicationService, HslPlcCommunicationService>();
                    services.AddSingleton<IMesConnectionMonitorService, MesConnectionMonitorService>();
                    services.AddSingleton<IPlcProductionMonitorService, PlcProductionMonitorService>();
                    services.AddSingleton<IPlcWorkIdMonitorService, PlcWorkIdMonitorService>();
                    services.AddTransient<PermissionUiBinder>();

                    services.AddHttpClient<IMesProvider, MesProvider>();

                    services.AddTransient<LoginForm>();
                    services.AddTransient<MainForm>();
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
            AppHost.Services.GetRequiredService<ISysUserService>().InitDb();
            // Resolve the localizer early so startup warnings follow the current language.
            var localizer = AppHost.Services.GetRequiredService<ILocalizationService>();
            AppHost.Services.GetRequiredService<IPlcCommunicationService>().StartAsync().GetAwaiter().GetResult();
            plcServiceStarted = true;
            AppHost.Services.GetRequiredService<IMesConnectionMonitorService>().StartAsync().GetAwaiter().GetResult();
            mesMonitorStarted = true;
            AppHost.Services.GetRequiredService<IPlcProductionMonitorService>().StartAsync().GetAwaiter().GetResult();
            productionMonitorStarted = true;
            AppHost.Services.GetRequiredService<IPlcWorkIdMonitorService>().StartAsync().GetAwaiter().GetResult();
            workIdMonitorStarted = true;

            var taskService = AppHost.Services.GetRequiredService<IWeldTaskService>();
            var timeSyncResult = taskService.SyncServerTimeAsync().GetAwaiter().GetResult();
            if (!timeSyncResult.IsSuccess)
            {
                ShowStartupMessage(
                    localizer.GetString(TextKeys.Common.StartupTimeSyncFailed, timeSyncResult.Msg),
                    MessageBoxIcon.Warning);
            }

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
            ShowStartupError(ex);
        }
        finally
        {
            StopBackgroundServices(workIdMonitorStarted, productionMonitorStarted, mesMonitorStarted, plcServiceStarted);
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

    private static void StopBackgroundServices(bool workIdMonitorStarted, bool productionMonitorStarted, bool mesMonitorStarted, bool plcServiceStarted)
    {
        try
        {
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
                AppHost?.Services.GetRequiredService<IMesConnectionMonitorService>().StopAsync().GetAwaiter().GetResult();
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
