using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace QuickOcr.SingleInstance;

/// <summary>
/// 应用单实例守卫：Mutex 判定 + 命名管道唤醒。
/// 二次启动时通知已运行实例聚焦托盘/设置，自身退出。
/// </summary>
public sealed class InstanceGuard : IDisposable
{
    // Local 前缀限制在当前会话，避免跨会话权限问题
    private const string MutexName = "Local\\QuickOcr.SingleInstance";
    private const string PipeName = "QuickOcr.Pipe";
    private const string WakeupMessage = "SHOW";

    private Mutex? _mutex;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public bool IsFirstInstance { get; private set; }

    /// <summary>已运行实例收到二次启动唤醒时触发（在管道线程）。</summary>
    public Action? OnWakeup { get; set; }

    /// <summary>启动守卫。返回 true 表示本进程为首实例；false 表示已有实例运行（已通知唤醒）。</summary>
    public bool TryStart()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        IsFirstInstance = createdNew;

        if (!createdNew)
        {
            NotifyExisting();
            return false;
        }

        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(() => RunServerAsync(_cts.Token));
        return true;
    }

    // 通知已运行实例唤醒
    private static void NotifyExisting()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client);
            writer.WriteLine(WakeupMessage);
            writer.Flush();
        }
        catch
        {
            // 已运行实例无响应则忽略
        }
    }

    // 首实例：循环监听唤醒消息
    private async Task RunServerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                await server.WaitForConnectionAsync(ct);
                using var reader = new StreamReader(server);
                var line = await reader.ReadLineAsync(ct);
                if (line == WakeupMessage)
                {
                    OnWakeup?.Invoke();
                }
            }
            catch (OperationCanceledException) { break; }
            catch
            {
                // 单次连接异常不影响后续监听
            }
        }
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { if (IsFirstInstance && _mutex != null) _mutex.ReleaseMutex(); } catch { }
        _cts?.Dispose();
        _mutex?.Dispose();
    }
}
