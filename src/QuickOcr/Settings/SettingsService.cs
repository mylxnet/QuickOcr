using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickOcr.Settings;

/// <summary>
/// 设置读写服务（单例）。失败容错：读取异常回退默认值，写入异常静默忽略。
/// </summary>
public sealed class SettingsService
{
    private static readonly string AppDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickOcr");
    private static readonly string SettingsPath = Path.Combine(AppDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static SettingsService Instance { get; } = new();

    public AppSettings Current { get; private set; } = new();

    private SettingsService() { }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
                if (s != null) Current = s;
            }
        }
        catch
        {
            // 损坏则用默认值
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        // 先写同目录临时文件，再原子替换目标文件，
        // 避免写入中途崩溃/断电留下半截 settings.json 导致设置全部丢失
        var tmpPath = SettingsPath + ".tmp";
        try
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(Current, JsonOpts);
            File.WriteAllText(tmpPath, json);

            if (File.Exists(SettingsPath))
                File.Replace(tmpPath, SettingsPath, destinationBackupFileName: null);
            else
                File.Move(tmpPath, SettingsPath);
        }
        catch
        {
            // 写入失败不影响主流程；清理残留临时文件（尽力而为）
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
        }
    }

    public static string GetSettingsFilePath() => SettingsPath;
}
