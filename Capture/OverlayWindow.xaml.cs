using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QuickOcr.Utils;

namespace QuickOcr.Capture;

/// <summary>
/// 全屏框选窗口：拖拽框选 → 释放进入微调模式（8 手柄调整/选区拖动）→ 单击/Enter/双击确认 → ESC 取消。
/// 选区内透明高亮，选区外暗化遮罩。跨所有显示器覆盖虚拟屏。
/// </summary>
public partial class OverlayWindow : Window
{
    private enum Mode { Idle, DragSelecting, MicroAdjust, Moving, Resizing, Reselecting }
    private enum Handle { TL, TR, BL, BR, T, B, L, R }

    private Mode _mode = Mode.Idle;
    private Handle _handle;
    private Point? _dragStart;
    private Rect? _origRect; // 微调前的选区，用于取消/重新框选回退
    private readonly Dictionary<Handle, Rectangle> _handles = new();

    public Rect? Selection { get; private set; }
    public double DpiScaleX { get; private set; } = 1.0;
    public double DpiScaleY { get; private set; } = 1.0;

    public OverlayWindow()
    {
        InitializeComponent();
        // 跨所有显示器：覆盖虚拟屏（含副屏，坐标可能为负）
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Loaded += OnLoaded;
        CreateHandles();
    }

    private void CreateHandles()
    {
        var stroke = new SolidColorBrush(Color.FromRgb(0x4B, 0x3F, 0xE3));
        foreach (Handle h in Enum.GetValues(typeof(Handle)))
        {
            var r = new Rectangle
            {
                Width = 10, Height = 10,
                Fill = Brushes.White,
                Stroke = stroke,
                StrokeThickness = 1,
                Visibility = Visibility.Collapsed
            };
            HandleLayer.Children.Add(r);
            _handles[h] = r;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var src = PresentationSource.FromVisual(this);
        if (src != null && src.CompositionTarget != null)
        {
            DpiScaleX = src.CompositionTarget.TransformToDevice.M11;
            DpiScaleY = src.CompositionTarget.TransformToDevice.M22;
        }
        MaskPath.Data = BuildMask(DrawCanvas.ActualWidth, DrawCanvas.ActualHeight, null);
        Topmost = true;
        Focus();
        Keyboard.Focus(this);
        Activate();
        ShowHint("拖拽框选识别区域");
        Logger.Info($"框选窗口就绪：虚拟屏 {Width:F0}×{Height:F0} @({Left:F0},{Top:F0}) DPI={DpiScaleX:F2}");
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Cancel();
        else if (e.Key == Key.Enter) Confirm();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(this);

        if (_mode == Mode.Idle)
        {
            // 初次框选
            _mode = Mode.DragSelecting;
            _dragStart = p;
            Selection = new Rect(p, p);
            DrawCanvas.CaptureMouse();
            UpdateVisual();
        }
        else if (_mode == Mode.MicroAdjust)
        {
            _dragStart = p;
            _origRect = Selection;
            var h = HitHandle(p);
            if (h.HasValue) { _mode = Mode.Resizing; _handle = h.Value; ApplyCursor(h.Value); }
            else if (Selection!.Value.Contains(p)) { _mode = Mode.Moving; Cursor = Cursors.SizeAll; }
            else { _mode = Mode.Reselecting; _dragStart = p; Cursor = Cursors.Cross; }
            DrawCanvas.CaptureMouse();
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart == null) return;
        var p = e.GetPosition(this);

        switch (_mode)
        {
            case Mode.DragSelecting:
            case Mode.Reselecting:
                Selection = new Rect(_dragStart.Value, p);
                break;
            case Mode.Moving:
                Selection = MoveRect(_origRect!.Value, p.X - _dragStart.Value.X, p.Y - _dragStart.Value.Y);
                break;
            case Mode.Resizing:
                Selection = ResizeRect(_origRect!.Value, p, _handle);
                break;
            default:
                return;
        }
        UpdateVisual();
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart == null) return;
        var p = e.GetPosition(this);
        DrawCanvas.ReleaseMouseCapture();
        Cursor = Cursors.Cross;

        switch (_mode)
        {
            case Mode.DragSelecting:
                if (Selection!.Value.Width >= 4 && Selection!.Value.Height >= 4)
                { EnterMicroAdjust(); }
                else { _mode = Mode.Idle; Selection = null; ShowHint("拖拽框选识别区域"); }
                break;
            case Mode.Reselecting:
                if (Selection!.Value.Width >= 4 && Selection!.Value.Height >= 4)
                { EnterMicroAdjust(); }
                else { Selection = _origRect; _mode = Mode.MicroAdjust; }
                break;
            case Mode.Moving:
                // 移动距离极小视为单击 → 确认执行识别
                if ((p - _dragStart.Value).Length < 3) { Confirm(); return; }
                _mode = Mode.MicroAdjust;
                break;
            case Mode.Resizing:
                _mode = Mode.MicroAdjust;
                break;
        }
        UpdateVisual();
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 微调模式下双击选区内 → 确认
        if (_mode == Mode.MicroAdjust && Selection!.Value.Contains(e.GetPosition(this)))
            Confirm();
    }

    private void Confirm()
    {
        if (Selection == null || Selection.Value.Width < 4 || Selection.Value.Height < 4) return;
        Logger.Info($"框选确认：{Selection.Value.Width:F0}×{Selection.Value.Height:F0} @({Selection.Value.X:F0},{Selection.Value.Y:F0})");
        DialogResult = true;
        Close();
    }

    private void Cancel()
    {
        Logger.Info("框选取消");
        Selection = null;
        DialogResult = false;
        Close();
    }

    // ---- 视觉更新 ----
    private void UpdateVisual()
    {
        double w = DrawCanvas.ActualWidth, h = DrawCanvas.ActualHeight;
        var sel = Selection;
        MaskPath.Data = BuildMask(w, h, sel);

        if (sel.HasValue)
        {
            var s = sel.Value;
            Canvas.SetLeft(SelectionBorder, s.X);
            Canvas.SetTop(SelectionBorder, s.Y);
            SelectionBorder.Width = s.Width;
            SelectionBorder.Height = s.Height;
            SelectionBorder.Visibility = Visibility.Visible;

            UpdateHandlePositions(s);

            bool hasSize = s.Width > 1 && s.Height > 1;
            SizeBadge.Visibility = hasSize ? Visibility.Visible : Visibility.Collapsed;
            if (hasSize)
            {
                int pxW = (int)Math.Round(s.Width * DpiScaleX);
                int pxH = (int)Math.Round(s.Height * DpiScaleY);
                SizeLabel.Text = $"{pxW} × {pxH}";
                double bx = s.Right + 6, by = s.Bottom + 6;
                if (bx > w - 80) bx = Math.Max(0, s.Left - 70);
                if (by > h - 30) by = Math.Max(0, s.Top - 28);
                Canvas.SetLeft(SizeBadge, bx);
                Canvas.SetTop(SizeBadge, by);
            }
        }
        else
        {
            SelectionBorder.Visibility = Visibility.Collapsed;
            SizeBadge.Visibility = Visibility.Collapsed;
            foreach (var hd in _handles.Values) hd.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateHandlePositions(Rect s)
    {
        bool show = _mode == Mode.MicroAdjust;
        SetHandle(Handle.TL, s.X, s.Y, show);
        SetHandle(Handle.TR, s.Right, s.Y, show);
        SetHandle(Handle.BL, s.X, s.Bottom, show);
        SetHandle(Handle.BR, s.Right, s.Bottom, show);
        SetHandle(Handle.T, (s.X + s.Right) / 2, s.Y, show);
        SetHandle(Handle.B, (s.X + s.Right) / 2, s.Bottom, show);
        SetHandle(Handle.L, s.X, (s.Y + s.Bottom) / 2, show);
        SetHandle(Handle.R, s.Right, (s.Y + s.Bottom) / 2, show);
    }

    private void SetHandle(Handle h, double x, double y, bool show)
    {
        var r = _handles[h];
        Canvas.SetLeft(r, x - 5);
        Canvas.SetTop(r, y - 5);
        r.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---- 命中测试 ----
    private Handle? HitHandle(Point p)
    {
        if (Selection == null) return null;
        var s = Selection.Value;
        if (Near(p, s.X, s.Y)) return Handle.TL;
        if (Near(p, s.Right, s.Y)) return Handle.TR;
        if (Near(p, s.X, s.Bottom)) return Handle.BL;
        if (Near(p, s.Right, s.Bottom)) return Handle.BR;
        if (Near(p, (s.X + s.Right) / 2, s.Y)) return Handle.T;
        if (Near(p, (s.X + s.Right) / 2, s.Bottom)) return Handle.B;
        if (Near(p, s.X, (s.Y + s.Bottom) / 2)) return Handle.L;
        if (Near(p, s.Right, (s.Y + s.Bottom) / 2)) return Handle.R;
        return null;
    }

    private static bool Near(Point p, double x, double y) => Math.Abs(p.X - x) <= 7 && Math.Abs(p.Y - y) <= 7;

    private void ApplyCursor(Handle h)
    {
        Cursor = h switch
        {
            Handle.TL or Handle.BR => Cursors.SizeNWSE,
            Handle.TR or Handle.BL => Cursors.SizeNESW,
            Handle.T or Handle.B => Cursors.SizeNS,
            _ => Cursors.SizeWE,
        };
    }

    // ---- 选区几何 ----
    private static Rect MoveRect(Rect o, double dx, double dy) => new(o.X + dx, o.Y + dy, o.Width, o.Height);

    private static Rect ResizeRect(Rect o, Point p, Handle h)
    {
        double l = o.X, t = o.Y, r = o.Right, b = o.Bottom;
        switch (h)
        {
            case Handle.TL: l = p.X; t = p.Y; break;
            case Handle.TR: r = p.X; t = p.Y; break;
            case Handle.BL: l = p.X; b = p.Y; break;
            case Handle.BR: r = p.X; b = p.Y; break;
            case Handle.T: t = p.Y; break;
            case Handle.B: b = p.Y; break;
            case Handle.L: l = p.X; break;
            case Handle.R: r = p.X; break;
        }
        return new Rect(new Point(l, t), new Point(r, b)); // 自动规范化
    }

    private void EnterMicroAdjust()
    {
        _mode = Mode.MicroAdjust;
        ShowHint("拖动选区/手柄微调 · 选区内单击或 Enter 确认 · ESC 取消");
        // 确保窗口拥有键盘焦点，使 Enter/ESC 生效
        Activate();
        Focus();
        Keyboard.Focus(this);
    }

    private void ShowHint(string text)
    {
        HintText.Text = text;
        HintBar.Visibility = Visibility.Visible;
    }

    private static StreamGeometry BuildMask(double w, double h, Rect? sel)
    {
        var g = new StreamGeometry { FillRule = FillRule.EvenOdd };
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(0, 0), isFilled: true, isClosed: true);
            ctx.LineTo(new Point(w, 0), isStroked: true, isSmoothJoin: false);
            ctx.LineTo(new Point(w, h), isStroked: true, isSmoothJoin: false);
            ctx.LineTo(new Point(0, h), isStroked: true, isSmoothJoin: false);

            if (sel.HasValue && sel.Value.Width > 0 && sel.Value.Height > 0)
            {
                var s = sel.Value;
                ctx.BeginFigure(new Point(s.X, s.Y), isFilled: true, isClosed: true);
                ctx.LineTo(new Point(s.Right, s.Y), isStroked: true, isSmoothJoin: false);
                ctx.LineTo(new Point(s.Right, s.Bottom), isStroked: true, isSmoothJoin: false);
                ctx.LineTo(new Point(s.X, s.Bottom), isStroked: true, isSmoothJoin: false);
            }
        }
        g.Freeze();
        return g;
    }
}
