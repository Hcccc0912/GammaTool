using System.Windows.Input;
using System.Windows.Interop;
using Gato.Interop;

namespace Gato.Services;

public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int ModAlt = 0x0001;
    private const int ModControl = 0x0002;
    private const int ModShift = 0x0004;
    private const int ModWin = 0x0008;
    private const int ModNoRepeat = 0x4000;

    private readonly HwndSource _source;
    private readonly Dictionary<int, (ModifierKeys Modifiers, Key Key)> _registered = [];
    private bool _disposed;

    public event Action<int>? HotkeyPressed;

    public HotkeyService()
    {
        var parameters = new HwndSourceParameters("Gato.HotkeyMessageWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public bool Register(int profileId, ModifierKeys modifiers, Key key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (profileId <= 0 || modifiers == ModifierKeys.None || key is Key.None or Key.System)
            return false;

        var hadPrevious = _registered.TryGetValue(profileId, out var previous);
        if (hadPrevious)
            Unregister(profileId);

        var nativeModifiers = ToNativeModifiers(modifiers) | ModNoRepeat;
        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (NativeMethods.RegisterHotKey(_source.Handle, profileId, (uint)nativeModifiers, virtualKey))
        {
            _registered[profileId] = (modifiers, key);
            return true;
        }

        if (hadPrevious)
        {
            var oldModifiers = (uint)(ToNativeModifiers(previous.Modifiers) | ModNoRepeat);
            var oldKey = (uint)KeyInterop.VirtualKeyFromKey(previous.Key);
            if (NativeMethods.RegisterHotKey(_source.Handle, profileId, oldModifiers, oldKey))
                _registered[profileId] = previous;
        }
        return false;
    }

    public void Unregister(int profileId)
    {
        if (!_registered.Remove(profileId)) return;
        NativeMethods.UnregisterHotKey(_source.Handle, profileId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var id in _registered.Keys.ToArray())
            Unregister(id);
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _disposed = true;
    }

    public static int ToNativeModifiers(ModifierKeys modifiers)
    {
        var native = 0;
        if ((modifiers & ModifierKeys.Alt) != 0) native |= ModAlt;
        if ((modifiers & ModifierKeys.Control) != 0) native |= ModControl;
        if ((modifiers & ModifierKeys.Shift) != 0) native |= ModShift;
        if ((modifiers & ModifierKeys.Windows) != 0) native |= ModWin;
        return native;
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey)
        {
            HotkeyPressed?.Invoke(wParam.ToInt32());
            handled = true;
        }
        return IntPtr.Zero;
    }
}
