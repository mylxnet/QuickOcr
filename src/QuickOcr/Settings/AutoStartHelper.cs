using Microsoft.Win32;

namespace QuickOcr.Settings;

/// <summary>
/// 开机自启：写入/删除 HKCU\Software\Microsoft\Windows\CurrentVersion\Run。
/// </summary>
public static class AutoStartHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "QuickOcr";

    public static void Apply(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null) return;

        if (enable)
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                key.SetValue(ValueName, $"\"{exe}\"");
            }
        }
        else
        {
            if (key.GetValue(ValueName) != null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
    }
}
