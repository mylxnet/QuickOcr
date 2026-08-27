using System.Windows.Input;

namespace QuickOcr.Hotkey;

/// <summary>
/// 快捷键定义。存储格式 "Modifier|Modifier|Key"，例如 "Control|Shift|O"。
/// 解析失败回退默认 Ctrl+Shift+O，避免坏配置导致无法触发。
/// </summary>
public readonly record struct HotkeyDefinition(ModifierKeys Modifiers, Key Key)
{
    public static readonly HotkeyDefinition Default = new(ModifierKeys.Control | ModifierKeys.Shift, Key.O);

    public static HotkeyDefinition Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Default;

        try
        {
            var parts = text.Split('|');
            if (parts.Length < 2) return Default;

            ModifierKeys mods = ModifierKeys.None;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                mods |= (ModifierKeys)Enum.Parse(typeof(ModifierKeys), parts[i].Trim(), ignoreCase: true);
            }
            Key key = (Key)Enum.Parse(typeof(Key), parts[^1].Trim(), ignoreCase: true);

            // 至少一个修饰键 + 一个有效主键，防止单键误触
            if (mods == ModifierKeys.None || key == Key.None) return Default;
            return new HotkeyDefinition(mods, key);
        }
        catch
        {
            return Default;
        }
    }

    public string ToStorageString()
    {
        var list = new List<string>(4);
        foreach (var m in new[] { ModifierKeys.Control, ModifierKeys.Alt, ModifierKeys.Shift, ModifierKeys.Windows })
        {
            if ((Modifiers & m) == m) list.Add(m.ToString());
        }
        list.Add(Key.ToString());
        return string.Join("|", list);
    }

    public override string ToString()
    {
        var parts = new List<string>(4);
        foreach (var m in new[] { ModifierKeys.Control, ModifierKeys.Alt, ModifierKeys.Shift, ModifierKeys.Windows })
        {
            if ((Modifiers & m) == m) parts.Add(m.ToString());
        }
        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }
}
