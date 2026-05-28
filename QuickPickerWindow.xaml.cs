using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using QuickTools.Models;
using QuickTools.Services;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;
using WpfPoint = System.Windows.Point;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfSize = System.Windows.Size;

namespace QuickTools;

public partial class QuickPickerWindow : Window
{
    private readonly QuickActionService _quickActionService;

    // Layout
    private const double Center    = 190;
    private const double Radius    = 78;
    private const double TileSize  = 50;
    private const double LabelDist = Radius + TileSize / 2 + 14; // ~117 — radialmente além do tile

    // Paleta
    private static readonly MediaColor AccentHover = MediaColor.FromRgb(29, 78, 216);
    private static readonly MediaColor TileNormal  = MediaColor.FromArgb(185, 37, 99, 235);
    private static readonly MediaColor LabelBg     = MediaColor.FromArgb(235, 15, 50, 160);
    private static readonly MediaColor DotSmall    = MediaColor.FromArgb(110, 255, 255, 255);
    private static readonly MediaColor DotLarge    = MediaColor.FromArgb(210, 37, 99, 235);
    private static readonly MediaColor IconWhite   = Colors.White;

    // Cursor personalizado
    private Ellipse?         _cursorDot;
    private WpfPoint         _mousePos = new(Center, Center);
    private const double     DotSizeNormal = 14;
    private const double     DotSizeHover  = 28;
    private DispatcherTimer? _mouseWatchTimer;

    public QuickPickerWindow(QuickActionService quickActionService)
    {
        InitializeComponent();
        _quickActionService = quickActionService;
        InitCursorDot();

        // Para o timer sempre que a janela ficar invisível (Hide, Escape, clique fora…)
        IsVisibleChanged += (_, e) =>
        {
            if (!(bool)e.NewValue)
                _mouseWatchTimer?.Stop();
        };
    }

    // ─── Cursor personalizado ─────────────────────────────────────────────────

    private void InitCursorDot()
    {
        _cursorDot = new Ellipse
        {
            Width            = DotSizeNormal,
            Height           = DotSizeNormal,
            Fill             = new SolidColorBrush(DotSmall),
            IsHitTestVisible = false
        };
    }

    // ─── Timer de vigilância do rato ──────────────────────────────────────────
    // Necessário porque AllowsTransparency=True não dispara MouseMove em zonas
    // transparentes — o WPF entrega o evento à janela por baixo.

    private void StartMouseWatch()
    {
        _mouseWatchTimer?.Stop();
        _mouseWatchTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(30)
        };
        _mouseWatchTimer.Tick += OnMouseWatchTick;
        _mouseWatchTimer.Start();
    }

    private void OnMouseWatchTick(object? sender, EventArgs e)
    {
        if (!GetCursorPos(out var pt)) return;
        var scale = GetDpiScale();

        // Posição do rato relativa ao Canvas
        var wx = pt.X / scale - Left;
        var wy = pt.Y / scale - Top;
        _mousePos = new WpfPoint(wx, wy);
        SnapDotToMouse();

        // Fecha se saiu do perímetro do círculo (raio 100 px)
        var dx = wx - Center;
        var dy = wy - Center;
        if (dx * dx + dy * dy > 100 * 100)
        {
            _mouseWatchTimer?.Stop();
            Hide();
        }
    }

    private void SnapDotToMouse()
    {
        if (_cursorDot is null) return;
        // Lê sempre o valor efectivo (inclui valor animado em curso)
        var s = _cursorDot.Width;
        Canvas.SetLeft(_cursorDot, _mousePos.X - s / 2);
        Canvas.SetTop( _cursorDot, _mousePos.Y - s / 2);
    }

    private void GrowDot()
    {
        if (_cursorDot is null) return;
        // BackEase dá o overshoot "gota de água" — expande ligeiramente além do target e assenta
        var elastic = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };
        _cursorDot.BeginAnimation(WidthProperty,
            new DoubleAnimation(DotSizeHover, TimeSpan.FromMilliseconds(420)) { EasingFunction = elastic });
        _cursorDot.BeginAnimation(HeightProperty,
            new DoubleAnimation(DotSizeHover, TimeSpan.FromMilliseconds(420)) { EasingFunction = elastic });
        ((SolidColorBrush)_cursorDot.Fill).BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(DotLarge, TimeSpan.FromMilliseconds(300))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private void ShrinkDot()
    {
        if (_cursorDot is null) return;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        _cursorDot.BeginAnimation(WidthProperty,
            new DoubleAnimation(DotSizeNormal, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });
        _cursorDot.BeginAnimation(HeightProperty,
            new DoubleAnimation(DotSizeNormal, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });
        ((SolidColorBrush)_cursorDot.Fill).BeginAnimation(SolidColorBrush.ColorProperty,
            new ColorAnimation(DotSmall, TimeSpan.FromMilliseconds(240))
                { EasingFunction = ease });
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    public void ShowAtCursor(IEnumerable<QuickAction> wheelActions)
    {
        var actions = wheelActions.ToList();
        if (actions.Count == 0) return;

        RenderActions(actions);
        PositionAtCursor();
        Show();
        Activate();
        StartMouseWatch();
    }

    // ─── Posicionamento DPI-aware ─────────────────────────────────────────────

    private void PositionAtCursor()
    {
        if (!GetCursorPos(out var pt)) return;
        var scale = GetDpiScale();
        Left = pt.X / scale - Width  / 2;
        Top  = pt.Y / scale - Height / 2;
    }

    // ─── Render ───────────────────────────────────────────────────────────────

    private void RenderActions(IReadOnlyList<QuickAction> actions)
    {
        PickerCanvas.Children.Clear();

        var count = actions.Count;
        if (count == 0) return;

        var entries = new List<(Border Tile, Border Label)>();

        for (var i = 0; i < count; i++)
        {
            var angle  = -Math.PI / 2 + i * (Math.PI * 2 / count);
            var cx     = Center + Math.Cos(angle) * Radius;
            var cy     = Center + Math.Sin(angle) * Radius;
            var action = actions[i];

            var (tile, label) = CreateIconTile(action);

            // Posição do tile
            Canvas.SetLeft(tile, cx - TileSize / 2);
            Canvas.SetTop(tile,  cy - TileSize / 2);

            // Posição do label — radialmente para fora, centrado no ponto de saída
            var lx = Center + Math.Cos(angle) * LabelDist;
            var ly = Center + Math.Sin(angle) * LabelDist;
            label.Measure(new WpfSize(220, 60));
            var lw = label.DesiredSize.Width;
            var lh = label.DesiredSize.Height;
            Canvas.SetLeft(label, lx - lw / 2);
            Canvas.SetTop(label,  ly - lh / 2);

            var captured = action;
            tile.MouseLeftButtonUp += (_, _) =>
            {
                Hide();
                _ = Task.Run(async () =>
                {
                    await Task.Delay(60);
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        _quickActionService.Execute(captured.Id));
                });
            };

            entries.Add((tile, label));
        }

        // Z-order: tiles primeiro → labels por cima → cursor dot no topo
        foreach (var (tile, _) in entries) PickerCanvas.Children.Add(tile);
        foreach (var (_, label) in entries) PickerCanvas.Children.Add(label);

        if (_cursorDot is not null)
        {
            SnapDotToMouse();
            PickerCanvas.Children.Add(_cursorDot);
        }
    }

    private (Border tile, Border label) CreateIconTile(QuickAction action)
    {
        var iconText = new TextBlock
        {
            Text                = action.Icon,
            FontFamily          = new MediaFontFamily("Segoe MDL2 Assets"),
            FontSize            = 20,
            Foreground          = new SolidColorBrush(IconWhite),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment   = System.Windows.VerticalAlignment.Center,
            IsHitTestVisible    = false
        };

        var bgBrush = new SolidColorBrush(TileNormal);

        var scale = new ScaleTransform(1, 1);
        var tile = new Border
        {
            Width                  = TileSize,
            Height                 = TileSize,
            CornerRadius           = new CornerRadius(TileSize / 2),
            Background             = bgBrush,
            Child                  = iconText,
            Cursor                 = System.Windows.Input.Cursors.None,
            SnapsToDevicePixels    = true,
            RenderTransform        = scale,
            RenderTransformOrigin  = new WpfPoint(0.5, 0.5)
        };

        // Label pill — aparece radialmente fora do tile no hover
        // Opacity=0 (não Collapsed) para que Measure() devolva o tamanho real
        var label = new Border
        {
            Background       = new SolidColorBrush(LabelBg),
            CornerRadius     = new CornerRadius(8),
            Padding          = new Thickness(10, 5, 10, 5),
            Opacity          = 0,
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text       = action.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize   = 11,
                FontWeight = FontWeights.SemiBold
            }
        };

        tile.MouseEnter += (_, _) =>
        {
            // Tile: scale com overshoot de gota de água
            var dropIn = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(1.22, TimeSpan.FromMilliseconds(400)) { EasingFunction = dropIn });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(1.22, TimeSpan.FromMilliseconds(400)) { EasingFunction = dropIn });

            // Cor do tile
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(AccentHover, TimeSpan.FromMilliseconds(250))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

            // Label: fade in suave
            label.BeginAnimation(OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(220))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });

            GrowDot();
        };
        tile.MouseLeave += (_, _) =>
        {
            // Tile: retrai suavemente
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(300)) { EasingFunction = ease });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(300)) { EasingFunction = ease });

            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(TileNormal, TimeSpan.FromMilliseconds(260))
                    { EasingFunction = ease });

            // Label: fade out
            label.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, TimeSpan.FromMilliseconds(180))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });

            ShrinkDot();
        };

        return (tile, label);
    }

    // ─── DPI ─────────────────────────────────────────────────────────────────

    private static double GetDpiScale()
    {
        var hdc = GetDC(IntPtr.Zero);
        var dpi = GetDeviceCaps(hdc, 88);
        ReleaseDC(IntPtr.Zero, hdc);
        return dpi / 96.0;
    }

    // ─── Eventos ─────────────────────────────────────────────────────────────

    private void Window_Deactivated(object sender, EventArgs e) => Hide();

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape) Hide();
    }

    // ─── P/Invoke ────────────────────────────────────────────────────────────

    [DllImport("user32.dll")] private static extern bool GetCursorPos(out PointInt point);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")]  private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct PointInt { public int X; public int Y; }
}
