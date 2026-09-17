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
        try
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(Current, JsonOpts);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // 写入失败不影响主流程
        }
    }

    public static string GetSettingsFilePath() => SettingsPath;
}
