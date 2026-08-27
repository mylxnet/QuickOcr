using System.Drawing;
using System.Windows.Forms;
using QuickOcr.Ocr;

namespace QuickOcr.Tray;

/// <summary>
/// 系统托盘图标 + 右键菜单。
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notify;
    private readonly OcrOrchestrator _orchestrator;

    public event Action? ExitRequested;
    public event Action? SettingsRequested;
    public event Action? HelpRequested;
    public event Action? AboutRequested;

    public TrayIcon(OcrOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        _notify = new NotifyIcon
        {
            Text = "QuickOcr",
            Visible = true
        };
        LoadIcon();
        BuildMenu();
        _notify.DoubleClick += (s, e) => SettingsRequested?.Invoke();
    }

    // 从嵌入资源加载应用图标，失败回退系统默认图标
    private void LoadIcon()
    {
        try
        {
            var res = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/app.ico"));
            if (res != null)
            {
                _notify.Icon = new Icon(res.Stream);
                res.Stream.Dispose();
                return;
            }
        }
        catch { }
        _notify.Icon = SystemIcons.Application;
    }

    private void BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("立即识别", null, (s, e) => _ = _orchestrator.TriggerAsync());
        menu.Items.Add("-");

        menu.Items.Add("设置...", null, (s, e) => SettingsRequested?.Invoke());
        menu.Items.Add("帮助手册", null, (s, e) => HelpRequested?.Invoke());
        menu.Items.Add("关于", null, (s, e) => AboutRequested?.Invoke());
        menu.Items.Add("-");

        menu.Items.Add("退出", null, (s, e) => ExitRequested?.Invoke());
        _notify.ContextMenuStrip = menu;
    }

    public void ShowTooltip(string title, string message)
    {
        _notify.ShowBalloonTip(2000, title, message, ToolTipIcon.Info);
    }

    public void Dispose()
    {
        _notify.Visible = false;
        _notify.Dispose();
    }
}
