using System.Windows;
using System.Windows.Input;
using QuickOcr.Hotkey;

namespace QuickOcr.Settings;

/// <summary>
/// 设置窗口：编辑快捷键、引擎、API、输出与自启选项。保存后由 App 重注册热键并应用自启。
/// </summary>
public partial class SettingsWindow : Window
{
    private HotkeyDefinition _hk;

    /// <summary>是否已保存。</summary>
    public bool Saved { get; private set; }

    public SettingsWindow()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        var s = SettingsService.Instance.Current;
        _hk = HotkeyDefinition.Parse(s.Hotkey);
        HotkeyBox.Text = _hk.ToString();

        bool isHttp = string.Equals(s.Engine, "HttpApi", System.StringComparison.OrdinalIgnoreCase);
        EngineTesseract.IsChecked = !isHttp;
        EngineHttpApi.IsChecked = isHttp;

        LanguageBox.Text = s.Language;
        ApiUrlBox.Text = s.ApiUrl;
        ApiHeaderNameBox.Text = s.ApiHeaderName;
        ApiHeaderValueBox.Text = s.ApiHeaderValue;

        ClipboardBox.IsChecked = s.CopyToClipboard;
        NotepadBox.IsChecked = s.OpenInNotepad;
        AutoStartBox.IsChecked = s.RunAtStartup;

        UpdateApiEnabled();
    }

    private void OnHotkeyGotFocus(object sender, RoutedEventArgs e)
    {
        // 聚焦时全选提示可替换，此处仅占位
    }

    private void OnHotkeyPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        // Alt 组合时 e.Key 为 System，用 SystemKey 取真实主键
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // 排除纯修饰键（按下即松开）
        if (key is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftShift or Key.RightShift
                or Key.LeftAlt or Key.RightAlt
                or Key.LWin or Key.RWin) return;

        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None) return;

        _hk = new HotkeyDefinition(mods, key);
        HotkeyBox.Text = _hk.ToString();
    }

    private void OnEngineChanged(object sender, RoutedEventArgs e) => UpdateApiEnabled();

    private void UpdateApiEnabled()
    {
        bool http = EngineHttpApi.IsChecked == true;
        ApiUrlBox.IsEnabled = http;
        ApiHeaderNameBox.IsEnabled = http;
        ApiHeaderValueBox.IsEnabled = http;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var s = SettingsService.Instance.Current;
        s.Hotkey = _hk.ToStorageString();
        s.Engine = EngineHttpApi.IsChecked == true ? "HttpApi" : "Tesseract";
        s.Language = string.IsNullOrWhiteSpace(LanguageBox.Text) ? "chi_sim+eng" : LanguageBox.Text.Trim();
        s.ApiUrl = ApiUrlBox.Text.Trim();
        s.ApiHeaderName = ApiHeaderNameBox.Text.Trim();
        s.ApiHeaderValue = ApiHeaderValueBox.Text.Trim();
        s.CopyToClipboard = ClipboardBox.IsChecked == true;
        s.OpenInNotepad = NotepadBox.IsChecked == true;
        s.RunAtStartup = AutoStartBox.IsChecked == true;

        SettingsService.Instance.Save();
        Saved = true;
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
