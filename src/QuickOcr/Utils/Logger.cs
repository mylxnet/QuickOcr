using System.IO;

namespace QuickOcr.Utils;

/// <summary>
/// 简单文件日志：按日期滚动，路径 %AppData%\QuickOcr\logs\QuickOcr_yyyyMMdd.log。
/// 写入失败静默吞掉，绝不影响主流程。
/// </summary>
public static class Logger
{
    private static readonly object _sync = new();

    private static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "QuickOcr", "logs");

    public static void Info(string msg) => Write("INFO ", msg);

    public static void Warn(string msg) => Write("WARN ", msg);

    public static void Error(string msg, Exception? ex = null) =>
        Write("ERROR", ex == null ? msg : $"{msg} | {ex}");

    private static void Write(string level, string msg)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var path = Path.Combine(Dir, $"QuickOcr_{DateTime.Now:yyyyMMdd}.log");
            var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {msg}{Environment.NewLine}";
            lock (_sync)
            {
                File.AppendAllText(path, line);
            }
        }
        catch
        {
            // 日志失败不影响主流程
        }
    }
}
