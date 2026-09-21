using System.Diagnostics;

namespace AutoWeldSystem.CenterServer.Services;

/// <summary>
/// 使用与正式报表绑定的独占文件句柄，跨服务实例和进程串行化同一路径的读改写流程。
/// </summary>
internal sealed class CenterReportPathLock
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// 在有限时间内获取独占锁；超时后抛出 IO 异常，让上层上传任务按既有策略重试。
    /// </summary>
    public IDisposable Acquire(string reportPath)
    {
        var lockPath = reportPath + ".lock";
        var stopwatch = Stopwatch.StartNew();
        IOException? lastException = null;

        while (stopwatch.Elapsed < DefaultTimeout)
        {
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException ex)
            {
                lastException = ex;
                Thread.Sleep(RetryDelay);
            }
        }

        throw new IOException($"Timed out waiting for center report lock: {reportPath}", lastException);
    }
}
