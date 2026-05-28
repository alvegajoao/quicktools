using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using QuickTools.Models;
using Drawing = System.Drawing;

// ── Resolve WPF ↔ WinForms / System.Drawing ambiguities (UseWindowsForms=true) ──
using WpfApp            = System.Windows.Application;
using Brush             = System.Windows.Media.Brush;
using Brushes           = System.Windows.Media.Brushes;
using Color             = System.Windows.Media.Color;
using FontFamily        = System.Windows.Media.FontFamily;
using Cursors           = System.Windows.Input.Cursors;
using Orientation       = System.Windows.Controls.Orientation;
using HorzAlign         = System.Windows.HorizontalAlignment;
using VertAlign         = System.Windows.VerticalAlignment;

namespace QuickTools.Views;

public partial class TrayMenuWindow : Window
{
    // ─── Win32 ────────────────────────────────────────────────────────────────
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

    // ─── Construction ─────────────────────────────────────────────────────────
    public TrayMenuWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) => HideMenu();
        KeyDown     += (_, e) => { if (e.Key == Key.Escape) HideMenu(); };
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void ShowMenu(IReadOnlyList<TrayMenuEntry> entries, Drawing.Point cursorPhysical)
    {
        BuildItems(entries);
        PositionWindow(cursorPhysical);

        // Reset opacity before animating in
        MenuRoot.Opacity = 0;
        MenuRoot.RenderTransform = new TranslateTransform(0, 10);

        Show();

        // Force foreground so Deactivated fires on outside click
        var helper = new System.Windows.Interop.WindowInteropHelper(this);
        SetForegroundWindow(helper.Handle);
        Activate();

        AnimateIn();
    }

    public void HideMenu()
    {
        if (Visibility != Visibility.Visible) return;

        var fade = new DoubleAnimation(MenuRoot.Opacity, 0, TimeSpan.FromMilliseconds(110))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var slide = new DoubleAnimation(0, 6, TimeSpan.FromMilliseconds(110))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => Hide();
        MenuRoot.BeginAnimation(OpacityProperty, fade);
        ((TranslateTransform)MenuRoot.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slide);
    }

    // ─── Build ────────────────────────────────────────────────────────────────

    private void BuildItems(IReadOnlyList<TrayMenuEntry> entries)
    {
        MenuStack.Children.Clear();

        // ── App header ────────────────────────────────────────────────────────
        MenuStack.Children.Add(BuildHeader());

        foreach (var entry in entries)
        {
            MenuStack.Children.Add(
                entry.Kind == TrayMenuEntryKind.Separator
                    ? BuildSeparator()
                    : BuildItem(entry));
        }
    }

    private UIElement BuildHeader()
    {
        var accentBrush = Res<Brush>("AccentBrush");
        var accentSurf  = Res<Brush>("AccentSurfaceBrush");
        var textBrush   = Res<Brush>("TextBrush");
        var mutedBrush  = Res<Brush>("MutedTextBrush");

        var iconBox = new Border
        {
            Width          = 40,
            Height         = 40,
            CornerRadius   = new CornerRadius(14),
            Background     = accentSurf,
            BorderBrush    = accentBrush,
            BorderThickness = new Thickness(1),
            Margin         = new Thickness(0, 0, 12, 0),
            Child = new TextBlock
            {
                Text                = "",   // Segoe MDL2 "Settings / Tool"
                FontFamily          = new FontFamily("Segoe MDL2 Assets"),
                FontSize            = 18,
                Foreground          = accentBrush,
                HorizontalAlignment = HorzAlign.Center,
                VerticalAlignment   = VertAlign.Center
            }
        };

        var nameLabel = new TextBlock
        {
            Text       = "QuickTools",
            FontSize   = 14,
            FontWeight = FontWeights.Bold,
            Foreground = textBrush
        };
        var subLabel = new TextBlock
        {
            Text       = "Menu rápido",
            FontSize   = 11,
            Foreground = mutedBrush,
            Margin     = new Thickness(0, 2, 0, 0)
        };

        var textStack = new StackPanel { VerticalAlignment = VertAlign.Center };
        textStack.Children.Add(nameLabel);
        textStack.Children.Add(subLabel);

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin      = new Thickness(8, 6, 8, 10)
        };
        row.Children.Add(iconBox);
        row.Children.Add(textStack);
        return row;
    }

    private UIElement BuildSeparator()
    {
        var borderColor = Res<Brush>("BorderBrushSoft");
        return new Border
        {
            Height          = 1,
            Background      = borderColor,
            Margin          = new Thickness(8, 4, 8, 4),
            IsHitTestVisible = false
        };
    }

    private UIElement BuildItem(TrayMenuEntry entry)
    {
        var accentBrush = Res<Brush>("AccentBrush");
        var accentSurf  = Res<Brush>("AccentSurfaceBrush");
        var textBrush   = Res<Brush>("TextBrush");
        var hoverBrush  = Res<Brush>("SelectionBrush");

        // Danger overrides
        var dangerIcon = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        var dangerSurf = new SolidColorBrush(Color.FromArgb(28, 239, 68, 68));
        var dangerBord = new SolidColorBrush(Color.FromArgb(70, 239, 68, 68));
        var dangerHov  = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68));

        var iconGlyph = new TextBlock
        {
            Text                = entry.Icon,
            FontFamily          = new FontFamily("Segoe MDL2 Assets"),
            FontSize            = 15,
            Foreground          = entry.IsDanger ? dangerIcon : accentBrush,
            HorizontalAlignment = HorzAlign.Center,
            VerticalAlignment   = VertAlign.Center
        };

        var iconBox = new Border
        {
            Width           = 32,
            Height          = 32,
            CornerRadius    = new CornerRadius(10),
            Background      = entry.IsDanger ? dangerSurf : accentSurf,
            BorderBrush     = entry.IsDanger ? dangerBord : accentBrush,
            BorderThickness = new Thickness(1),
            Child           = iconGlyph
        };

        var label = new TextBlock
        {
            Text              = entry.Label,
            FontSize          = 13,
            FontWeight        = FontWeights.SemiBold,
            Foreground        = entry.IsDanger ? dangerIcon : textBrush,
            VerticalAlignment = VertAlign.Center,
            Margin            = new Thickness(11, 0, 0, 0),
            TextTrimming      = TextTrimming.CharacterEllipsis
        };

        var innerGrid = new Grid { Margin = new Thickness(8, 0, 10, 0) };
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(iconBox, 0);
        Grid.SetColumn(label,   1);
        innerGrid.Children.Add(iconBox);
        innerGrid.Children.Add(label);

        var itemBorder = new Border
        {
            Height          = 44,
            CornerRadius    = new CornerRadius(10),
            Background      = Brushes.Transparent,
            Cursor          = Cursors.Hand,
            Child           = innerGrid
        };

        // Hover ────────────────────────────────────────────────────────────────
        itemBorder.MouseEnter += (_, _) =>
            itemBorder.Background = entry.IsDanger ? dangerHov : hoverBrush;

        itemBorder.MouseLeave += (_, _) =>
            itemBorder.Background = Brushes.Transparent;

        // Press tint
        itemBorder.PreviewMouseLeftButtonDown += (_, _) =>
        {
            var pressColor = entry.IsDanger
                ? Color.FromArgb(65, 239, 68, 68)
                : Color.FromArgb(90, 37, 99, 235);
            itemBorder.Background = new SolidColorBrush(pressColor);
        };

        // Click ────────────────────────────────────────────────────────────────
        itemBorder.MouseLeftButtonUp += (_, _) =>
        {
            HideMenu();
            entry.Action?.Invoke();
        };

        return itemBorder;
    }

    // ─── Animation ────────────────────────────────────────────────────────────

    private void AnimateIn()
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        MenuRoot.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170)) { EasingFunction = ease });

        ((TranslateTransform)MenuRoot.RenderTransform).BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(190)) { EasingFunction = ease });
    }

    // ─── Positioning ─────────────────────────────────────────────────────────

    private void PositionWindow(Drawing.Point cursorPhysical)
    {
        // Convert physical → logical pixels using GDI DPI
        using var g  = Drawing.Graphics.FromHwnd(IntPtr.Zero);
        double scaleX = g.DpiX / 96.0;
        double scaleY = g.DpiY / 96.0;

        double cx = cursorPhysical.X / scaleX;
        double cy = cursorPhysical.Y / scaleY;

        // Window content size: 292 width + 44px margin each side
        const double winW = 292 + 44;
        const double winH = 760; // conservative estimate

        var work = SystemParameters.WorkArea;

        double left = cx - winW / 2.0;
        double top  = cy - winH - 6;   // above cursor (tray at bottom)

        // Clamp to work area
        if (left + winW > work.Right)  left = work.Right  - winW;
        if (left < work.Left)          left = work.Left;
        if (top  < work.Top)           top  = cy + 8;     // show below if too high
        if (top + winH > work.Bottom)  top  = work.Bottom - winH;

        Left = left;
        Top  = top;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static T Res<T>(string key) => (T)WpfApp.Current.Resources[key]!;
}
