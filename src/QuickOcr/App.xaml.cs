using System.Windows;
using QuickOcr.About;
using QuickOcr.Help;
using QuickOcr.Hotkey;
using QuickOcr.Ocr;
using QuickOcr.Settings;
using QuickOcr.SingleInstance;
using QuickOcr.Tray;
using QuickOcr.Utils;

namespace QuickOcr;

public partial class App : Application
{
    private InstanceGuard? _instanceGuard;
    private TrayIcon? _tray;
    private GlobalHotkeyService? _hotkey;
    private OcrOrchestrator? _orchestrator;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Logger.Info("应用启动");

        // 1) 单实例守卫
        _instanceGuard = new InstanceGuard();
        _instanceGuard.OnWakeup += OnInstanceWakeup;
        if (!_instanceGuard.TryStart())
        {
            Logger.Info("已有实例运行，自身退出");
            Shutdown(0);
            return;
        }

        // 2) 加载设置
        SettingsService.Instance.Load();
        Logger.Info($"设置已加载：热键={SettingsService.Instance.Current.Hotkey} 引擎={SettingsService.Instance.Current.Engine}");

        // 3) 调度器 + 托盘
        _orchestrator = new OcrOrchestrator();
        _tray = new TrayIcon(_orchestrator);
        _tray.ExitRequested += () => Shutdown();
        _tray.SettingsRequested += ShowSettings;
        _tray.HelpRequested += ShowHelp;
        _tray.AboutRequested += ShowAbout;

        // 4) 全局热键
        _hotkey = new GlobalHotkeyService();
        _hotkey.ErrorOccurred += msg => { Logger.Warn($"热键错误：{msg}"); _tray.ShowTooltip("热键", msg); };
        RegisterCurrentHotkey();
    }

    private void RegisterCurrentHotkey()
    {
        var hk = HotkeyDefinition.Parse(SettingsService.Instance.Current.Hotkey);
        if (!_hotkey!.Register(hk, () => _ = _orchestrator!.TriggerAsync()))
        {
            Logger.Warn($"快捷键 {hk} 注册失败");
            _tray!.ShowTooltip("热键", $"快捷键 {hk} 注册失败，请到设置中更换。");
        }
        else
        {
            Logger.Info($"快捷键 {hk} 已注册");
            _tray!.ShowTooltip("QuickOcr", $"已就绪，按 {hk} 框选识别。");
        }
    }

    // 单实例二次启动唤醒：切回主线程聚焦
    private void OnInstanceWakeup()
    {
        Logger.Info("单实例唤醒");
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _tray?.ShowTooltip("QuickOcr", "已在后台运行");
            ShowSettings();
        }));
    }

    private void ShowSettings()
    {
        var win = new SettingsWindow();
        win.ShowDialog();
        if (win.Saved)
        {
            Logger.Info("设置已保存，应用变更");
            ApplySettings();
        }
    }

    // 设置变更后：重新注册热键 + 应用开机自启
    private void ApplySettings()
    {
        var hk = HotkeyDefinition.Parse(SettingsService.Instance.Current.Hotkey);
        if (!_hotkey!.Register(hk, () => _ = _orchestrator!.TriggerAsync()))
        {
            Logger.Warn($"快捷键 {hk} 重新注册失败");
            _tray!.ShowTooltip("热键", $"快捷键 {hk} 注册失败，请更换。");
        }
        else
        {
            Logger.Info($"快捷键 {hk} 重新注册成功");
        }
        AutoStartHelper.Apply(SettingsService.Instance.Current.RunAtStartup);
        Logger.Info($"开机自启：{(SettingsService.Instance.Current.RunAtStartup ? "开" : "关")}");
    }

    private void ShowHelp() => new HelpWindow().Show();

    private void ShowAbout() => new AboutWindow().Show();

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("应用退出");
        _hotkey?.Dispose();
        _tray?.Dispose();
        _instanceGuard?.Dispose();
        SettingsService.Instance.Save();
        base.OnExit(e);
    }
}
