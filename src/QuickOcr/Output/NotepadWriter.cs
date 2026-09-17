using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using QuickOcr.Utils;

namespace QuickOcr.Output;

/// <summary>
/// 识别结果写入固定临时文件并用记事本打开。同一应用生命周期内复用记事本进程：
/// 已打开则聚焦窗口，避免堆叠多个记事本；内容相同则跳过写文件仅聚焦。
/// </summary>
public static class NotepadWriter
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "QuickOcr");
    private static readonly string TempFile = Path.Combine(TempDir, "result.txt");
    private static Process? _notepad;
    private static string? _lastHash;

    public static void WriteAndOpen(string text)
    {
        Directory.CreateDirectory(TempDir);

        var hash = Hash(text);
        bool same = hash == _lastHash;
        _lastHash = hash;

        // 内容相同仅聚焦，不重复写文件
        if (!same)
        {
            File.WriteAllText(TempFile, text, new UTF8Encoding(false));
        }

        // 记事本进程已存活 → 聚焦；否则启动新进程
        if (_notepad == null || _notepad.HasExited)
        {
            var psi = new ProcessStartInfo("notepad.exe", TempFile) { UseShellExecute = true };
            _notepad = Process.Start(psi);

            // 等待主窗口句柄就绪（刚启动可能为 0）
            if (_notepad != null)
            {
                for (int i = 0; i < 20 && _notepad.MainWindowHandle == IntPtr.Zero; i++)
                {
                    _notepad.Refresh();
                    Thread.Sleep(50);
                }
            }
        }
        else
        {
            var hwnd = _notepad.MainWindowHandle;
            if (hwnd != IntPtr.Zero)
            {
                Win32.ShowWindow(hwnd, Win32.SwRestore);
                Win32.SetForegroundWindow(hwnd);
            }
        }
    }

    private static string Hash(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
