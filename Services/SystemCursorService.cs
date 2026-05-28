using System.Runtime.InteropServices;

namespace QuickTools.Services;

public sealed class SystemCursorService
{
    private const uint SpiSetCursors = 0x0057;
    private const uint SpifNone = 0;
    private const uint OcrNormal = 32512;

    private static readonly IntPtr IdcArrow = new(32512);
    private static readonly IntPtr IdcCross = new(32515);
    private static readonly IntPtr IdcHand = new(32649);
    private static readonly IntPtr IdcPen = new(32631);
    private static readonly IntPtr IdcSizeAll = new(32646);
    private static readonly IntPtr IdcWait = new(32514);
    private static readonly IntPtr IdcHelp = new(32651);

    private bool _isApplied;

    public void Apply(string cursorKey)
    {
        Restore();

        var cursorId = cursorKey switch
        {
            "Hand" => IdcHand,
            "Pen" => IdcPen,
            "ScrollAll" => IdcSizeAll,
            "SizeAll" => IdcSizeAll,
            "Wait" => IdcWait,
            "Help" => IdcHelp,
            "Arrow" => IdcArrow,
            _ => IdcCross
        };

        var cursor = LoadCursor(IntPtr.Zero, cursorId);
        if (cursor == IntPtr.Zero)
        {
            return;
        }

        var cursorCopy = CopyIcon(cursor);
        if (cursorCopy == IntPtr.Zero)
        {
            return;
        }

        if (SetSystemCursor(cursorCopy, OcrNormal))
        {
            _isApplied = true;
        }
    }

    public void Restore()
    {
        if (!_isApplied)
        {
            return;
        }

        SystemParametersInfo(SpiSetCursors, 0, IntPtr.Zero, SpifNone);
        _isApplied = false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadCursor(IntPtr instance, IntPtr cursorName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CopyIcon(IntPtr cursor);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetSystemCursor(IntPtr cursor, uint cursorId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint param, IntPtr value, uint flags);
}
