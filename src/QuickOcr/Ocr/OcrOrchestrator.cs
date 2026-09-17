using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QuickOcr.Capture;
using QuickOcr.Output;
using QuickOcr.Settings;
using QuickOcr.Utils;

namespace QuickOcr.Ocr;

/// <summary>
/// OCR 调度器：串行锁防止并发触发，串联 框选 → 截图 → OCR → 记事本输出。
/// busy 时直接丢弃新触发，避免多个遮罩/多次 OCR 重叠。
/// </summary>
public sealed class OcrOrchestrator
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>热键/菜单触发的入口。</summary>
    public async Task TriggerAsync()
    {
        // 非阻塞获取锁：拿不到说明上一次识别未结束，直接丢弃
        if (!await _lock.WaitAsync(0))
        {
            Logger.Info("识别中触发被忽略（串行锁）");
            return;
        }

        try
        {
            Logger.Info("触发识别");
            // 1) 框选区域（主线程 UI）
            var (selection, scaleX, scaleY) = await ShowOverlayAsync();
            if (selection == null) { Logger.Info("用户取消框选"); return; }

            // 2) 截取区域位图
            using var bmp = CaptureRegion(selection.Value, scaleX, scaleY);
            if (bmp == null) { Logger.Warn("截图失败"); return; }
            Logger.Info($"截图完成：{bmp.Width}×{bmp.Height}px");

            // 3) OCR 引擎识别（工厂按设置创建并缓存）
            var settings = SettingsService.Instance.Current;
            var engine = OcrEngineFactory.Create(settings);
            var text = await engine.RecognizeAsync(bmp);

            if (string.IsNullOrWhiteSpace(text)) text = "(未识别到文字)";
            Logger.Info($"识别完成：{text.Length} 字符，引擎={settings.Engine}");

            // 4) 剪贴板（可选）+ 记事本复用打开（后台，避免阻塞 UI）
            await CopyToClipboardAsync(text);
            await Task.Run(() => NotepadWriter.WriteAndOpen(text));
        }
        catch (Exception ex)
        {
            Logger.Error("识别失败", ex);
            await ShowErrorAsync("识别失败：" + ex.Message);
        }
        finally
        {
            _lock.Release();
        }
    }

    // 剪贴板复制（需主线程 STA）
    private Task CopyToClipboardAsync(string text)
    {
        if (!SettingsService.Instance.Current.CopyToClipboard) return Task.CompletedTask;
        var tcs = new TaskCompletionSource<object?>();
        Application.Current.Dispatcher.Invoke(() =>
        {
            try { Clipboard.SetText(text); tcs.SetResult(null); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    // 主线程错误提示
    private Task ShowErrorAsync(string message)
    {
        var tcs = new TaskCompletionSource<object?>();
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                MessageBox.Show(message, "QuickOcr", MessageBoxButton.OK, MessageBoxImage.Error);
                tcs.SetResult(null);
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    // 在主线程显示框选窗口，返回 (选区, DPI缩放)
    private Task<(Rect? selection, double scaleX, double scaleY)> ShowOverlayAsync()
    {
        var tcs = new TaskCompletionSource<(Rect?, double, double)>();
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var win = new OverlayWindow();
                win.ShowDialog();
                tcs.SetResult((win.Selection, win.DpiScaleX, win.DpiScaleY));
            }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    // 截取屏幕区域：DIP 坐标 → 物理像素
    private static Bitmap? CaptureRegion(Rect dip, double scaleX, double scaleY)
    {
        int x = (int)Math.Round(dip.X * scaleX);
        int y = (int)Math.Round(dip.Y * scaleY);
        int w = (int)Math.Round(dip.Width * scaleX);
        int h = (int)Math.Round(dip.Height * scaleY);
        if (w <= 0 || h <= 0) return null;

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
        }
        return bmp;
    }
}
