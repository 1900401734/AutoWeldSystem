using System.ComponentModel;
using System.Runtime.InteropServices;
using AutoWeldSystem.Core.Interfaces;
using AutoWeldSystem.Core.Production;

namespace AutoWeldSystem.Services.Production;

/// <summary>
/// Changes the local Windows clock through the system API.
/// </summary>
public sealed class WindowsSystemClockService : ISystemClockService
{
    /// <summary>
    /// Reads the current local machine time.
    /// </summary>
    public DateTime GetLocalTime() => DateTime.Now;

    /// <summary>
    /// Sets the local Windows clock to the MES server time.
    /// </summary>
    public SystemClockSyncResult SetLocalTime(DateTime serverTime, DateTime localTimeBefore)
    {
        var offsetSeconds = (serverTime - localTimeBefore).TotalSeconds;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return SystemClockSyncResult.Failed(
                serverTime,
                localTimeBefore,
                offsetSeconds,
                "当前系统不是 Windows，无法修改系统时间。");
        }

        var systemTime = SystemTime.FromDateTime(serverTime);
        if (SetLocalTime(ref systemTime))
        {
            return SystemClockSyncResult.ChangedResult(
                serverTime,
                localTimeBefore,
                offsetSeconds,
                "已校时。");
        }

        var errorCode = Marshal.GetLastWin32Error();
        var errorMessage = new Win32Exception(errorCode).Message;
        return SystemClockSyncResult.Failed(
            serverTime,
            localTimeBefore,
            offsetSeconds,
            $"系统时间修改失败：{errorMessage} (Win32={errorCode})");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetLocalTime(ref SystemTime systemTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemTime
    {
        public ushort Year;
        public ushort Month;
        public ushort DayOfWeek;
        public ushort Day;
        public ushort Hour;
        public ushort Minute;
        public ushort Second;
        public ushort Milliseconds;

        /// <summary>
        /// Converts a local DateTime into the Win32 SYSTEMTIME structure.
        /// </summary>
        public static SystemTime FromDateTime(DateTime value)
        {
            var local = DateTime.SpecifyKind(value, DateTimeKind.Local);
            return new SystemTime
            {
                Year = (ushort)local.Year,
                Month = (ushort)local.Month,
                DayOfWeek = (ushort)local.DayOfWeek,
                Day = (ushort)local.Day,
                Hour = (ushort)local.Hour,
                Minute = (ushort)local.Minute,
                Second = (ushort)local.Second,
                Milliseconds = (ushort)local.Millisecond
            };
        }
    }
}
