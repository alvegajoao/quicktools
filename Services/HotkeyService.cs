using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuickTools.Services;

public sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly int _hotkeyId;
    private HwndSource? _source;
    private IntPtr _handle;

    public HotkeyService(int hotkeyId = 9060)
    {
        _hotkeyId = hotkeyId;
    }

    public event EventHandler? HotkeyPressed;

    public bool Register(Window window, string keyName, out string? errorMessage)
    {
        errorMessage = null;
        Unregister();

        _handle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(OnWindowMessage);

        var virtualKey = KeyNameToVirtualKey(keyName);
        if (RegisterHotKey(_handle, _hotkeyId, 0, virtualKey))
        {
            return true;
        }

        errorMessage = $"Could not register hotkey {keyName}. It may already be in use.";
        _source?.RemoveHook(OnWindowMessage);
        _source = null;
        _handle = IntPtr.Zero;
        return false;
    }

    public void Unregister()
    {
        if (_handle != IntPtr.Zero)
        {
            UnregisterHotKey(_handle, _hotkeyId);
        }

        _source?.RemoveHook(OnWindowMessage);
        _source = null;
        _handle = IntPtr.Zero;
    }

    public void Dispose() => Unregister();

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == _hotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static uint KeyNameToVirtualKey(string keyName)
    {
        if (keyName.StartsWith('F') &&
            int.TryParse(keyName[1..], out var number) &&
            number is >= 1 and <= 24)
        {
            return (uint)(0x70 + number - 1);
        }

        return 0x75; // F6
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
