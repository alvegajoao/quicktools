using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickTools.Services;

public static class CursorPreviewService
{
    private const int PreviewPixels = 72;
    private const uint ImageCursor = 2;
    private const uint LrShared = 0x00008000;

    private static readonly Dictionary<string, ImageSource?> Cache = new();

    public static ImageSource? GetPreview(string cursorKey)
    {
        if (Cache.TryGetValue(cursorKey, out var cached))
        {
            return cached;
        }

        var cursor = LoadImage(IntPtr.Zero, GetCursorId(cursorKey), ImageCursor, PreviewPixels, PreviewPixels, LrShared);
        if (cursor == IntPtr.Zero)
        {
            cursor = LoadCursor(IntPtr.Zero, GetCursorId(cursorKey));
        }

        if (cursor == IntPtr.Zero)
        {
            Cache[cursorKey] = null;
            return null;
        }

        var cursorCopy = CopyIcon(cursor);
        if (cursorCopy == IntPtr.Zero)
        {
            Cache[cursorKey] = null;
            return null;
        }

        try
        {
            var preview = Imaging.CreateBitmapSourceFromHIcon(
                cursorCopy,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(PreviewPixels, PreviewPixels));
            preview.Freeze();
            Cache[cursorKey] = preview;
            return preview;
        }
        finally
        {
            DestroyIcon(cursorCopy);
        }
    }

    private static IntPtr GetCursorId(string cursorKey) => cursorKey switch
    {
        "Hand" => new IntPtr(32649),
        "Pen" => new IntPtr(32631),
        "ScrollAll" => new IntPtr(32646),
        "SizeAll" => new IntPtr(32646),
        "Wait" => new IntPtr(32514),
        "Help" => new IntPtr(32651),
        "Arrow" => new IntPtr(32512),
        _ => new IntPtr(32515)
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadCursor(IntPtr instance, IntPtr cursorName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr instance, IntPtr name, uint type, int desiredWidth, int desiredHeight, uint loadFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CopyIcon(IntPtr cursor);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr icon);
}
