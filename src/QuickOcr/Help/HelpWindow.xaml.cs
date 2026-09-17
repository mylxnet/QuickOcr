using System.Windows;
using QuickOcr.Hotkey;
using QuickOcr.Settings;

namespace QuickOcr.Help;

/// <summary>
/// 帮助手册：动态显示当前快捷键与使用说明。
/// </summary>
public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        var hk = HotkeyDefinition.Parse(SettingsService.Instance.Current.Hotkey);
        HotkeyText.Text = $"当前快捷键：{hk}（可在设置中自定义）";
    }
}
