using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using QuickOcr.Utils;

namespace QuickOcr.Hotkey;

/// <summary>
/// 全局热键服务。基于隐藏 HwndSource 接收 WM_HOTKEY 消息。
/// 支持动态注册/卸载（供设置中自定义快捷键使用）。
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    public bool IsRegistered => _currentId.HasValue;

    public event Action<string>? ErrorOccurred;

    private HwndSource? _source;
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId = 9000;
    private int? _currentId;

    private void EnsureSource()
    {
        if (_source != null) return;

        var parameters = new HwndSourceParameters("QuickOcrHotkeySink")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = 0
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var cb))
            {
                try { cb(); } catch { /* 回调异常不影响消息循环 */ }
            }
        }
        return IntPtr.Zero;
    }

    /// <summary>注册热键。返回 false 表示组合被占用。</summary>
    public bool Register(HotkeyDefinition hk, Action callback)
    {
        EnsureSource();
        Unregister();

        int id = _nextId++;
        uint mods = (uint)hk.Modifiers;
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(hk.Key);

        if (!Win32.RegisterHotKey(_source!.Handle, id, mods, vk))
        {
            int err = Marshal.GetLastWin32Error();
            ErrorOccurred?.Invoke($"快捷键 {hk} 注册失败（可能已被占用）。Win32Err={err}");
            return false;
        }

        _callbacks[id] = callback;
        _currentId = id;
        return true;
    }

    public void Unregister()
    {
        if (_currentId.HasValue && _source != null)
        {
            try { Win32.UnregisterHotKey(_source.Handle, _currentId.Value); } catch { }
            _callbacks.Remove(_currentId.Value);
            _currentId = null;
        }
    }

    public void Dispose()
    {
        Unregister();
        _source?.Dispose();
        _source = null;
    }
}
