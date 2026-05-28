using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using QuickTools.Services;
using QuickTools.ViewModels;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using DrawingText = System.Drawing.Text;
using MediaColor = System.Windows.Media.Color;
using WpfApplication = System.Windows.Application;

namespace QuickTools;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HotkeyService _hotkeyService = new();
    private readonly HotkeyService _quickToggleHotkeyService = new(9061);
    private readonly SystemCursorService _systemCursorService = new();
    private readonly UpdateService _updateService = new();
    private readonly QuickPickerWindow _quickPickerWindow;
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _isExitRequested;
    private bool _hasShownTrayHint;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _quickPickerWindow = new QuickPickerWindow(_viewModel.QuickToggle.QuickActionService);
        _trayIcon = CreateTrayIcon();

        SourceInitialized += (_, _) => RegisterHotkey();
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        Closing += OnClosing;
        Closed += OnClosed;
        _viewModel.AutoClickerService.StateChanged += OnAutoClickerStateChanged;
        _viewModel.SettingsChanged += (_, _) =>
        {
            ApplyTheme();
            RegisterHotkey();
            if (_viewModel.AutoClickerService.IsRunning)
            {
                _systemCursorService.Apply(_viewModel.AutoClicker.SelectedActiveCursor);
            }
        };
    }

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var trayIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "QuickTools",
            Visible = true
        };

        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        trayIcon.MouseUp += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
            {
                RestoreFromTray();
            }
        };
        trayIcon.ContextMenuStrip = CreateTrayMenu();
        trayIcon.ContextMenuStrip.Opening += (_, _) => BuildTrayMenu(trayIcon.ContextMenuStrip);
        return trayIcon;
    }

    private static Forms.ContextMenuStrip CreateTrayMenu()
    {
        return new Forms.ContextMenuStrip
        {
            BackColor = Drawing.Color.FromArgb(248, 250, 255),
            ForeColor = Drawing.Color.FromArgb(31, 35, 40),
            Font = new Drawing.Font("Segoe UI", 10.2f, Drawing.FontStyle.Regular),
            Padding = new Forms.Padding(7, 8, 7, 8),
            ShowImageMargin = true,
            ImageScalingSize = new Drawing.Size(20, 20),
            Renderer = new TrayMenuRenderer()
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        await _viewModel.Dashboard.RefreshAsync();
        await _viewModel.PowerModes.RefreshAsync();
        _ = CheckForUpdatesAsync();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            return;
        }

        Hide();
        if (_hasShownTrayHint)
        {
            return;
        }

        _trayIcon.ShowBalloonTip(
            2200,
            LocalizationService.Instance["Main_TrayStillRunningTitle"],
            LocalizationService.Instance["Main_TrayStillRunningMessage"],
            Forms.ToolTipIcon.Info);
        _hasShownTrayHint = true;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExitRequested)
        {
            return;
        }

        e.Cancel = true;
        WindowState = WindowState.Minimized;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void BuildTrayMenu(Forms.ContextMenuStrip menu)
    {
        menu.Items.Clear();

        AddTrayItem(menu, "\uE8A7", LocalizationService.Instance["Main_OpenQuickTools"], RestoreFromTray);
        AddTraySeparator(menu);

        AddTrayItem(
            menu,
            "\uE7C9",
            _viewModel.AutoClickerService.IsRunning
                ? LocalizationService.Instance["Main_StopAutoClicker"]
                : LocalizationService.Instance["Main_StartAutoClicker"],
            _viewModel.AutoClicker.Toggle);

        AddTrayItem(
            menu,
            "\uE8A7",
            _viewModel.QuickToggle.IsEnabled
                ? LocalizationService.Instance["Main_DisableQuickToggle"]
                : LocalizationService.Instance["Main_EnableQuickToggle"],
            () => _viewModel.QuickToggle.IsEnabled = !_viewModel.QuickToggle.IsEnabled);

        AddTrayItem(menu, "\uE9D9", LocalizationService.Instance["Main_OpenQuickToggleWheel"], () =>
        {
            if (_viewModel.QuickToggle.IsEnabled)
            {
                _quickPickerWindow.ShowAtCursor(_viewModel.QuickToggle.WheelActions);
            }
        });

        AddTraySeparator(menu);
        AddTrayItem(menu, "\uE74F", LocalizationService.Instance["QuickAction_mute_Name"], () => _viewModel.QuickToggle.QuickActionService.Execute("mute"));
        AddTrayItem(menu, "\uE767", LocalizationService.Instance["QuickAction_vol_up_Name"], () => _viewModel.QuickToggle.QuickActionService.Execute("vol_up"));
        AddTrayItem(menu, "\uE993", LocalizationService.Instance["QuickAction_vol_down_Name"], () => _viewModel.QuickToggle.QuickActionService.Execute("vol_down"));
        AddTrayItem(menu, "\uE8C8", LocalizationService.Instance["QuickAction_clipboard_Name"], () => _viewModel.QuickToggle.QuickActionService.Execute("clipboard"));
        AddTrayItem(menu, "\uE72E", LocalizationService.Instance["QuickAction_lock_Name"], () => _viewModel.QuickToggle.QuickActionService.Execute("lock"));

        AddTraySeparator(menu);
        AddTrayItem(menu, "\uE9D2", LocalizationService.Instance.Format("Main_PowerPrefix", LocalizationService.Instance.TranslatePowerPlanKind("Balanced")), async () => await SetPowerPlanFromTrayAsync("Balanced"));
        AddTrayItem(menu, "\uE945", LocalizationService.Instance.Format("Main_PowerPrefix", LocalizationService.Instance.TranslatePowerPlanKind("HighPerformance")), async () => await SetPowerPlanFromTrayAsync("HighPerformance"));
        AddTrayItem(menu, "\uE83F", LocalizationService.Instance.Format("Main_PowerPrefix", LocalizationService.Instance.TranslatePowerPlanKind("PowerSaver")), async () => await SetPowerPlanFromTrayAsync("PowerSaver"));
        AddTrayItem(menu, "\uE823", LocalizationService.Instance["Main_PauseScheduledPowerEvents"], () => _viewModel.PowerService.PauseAllEvents());

        AddTraySeparator(menu);
        AddTrayItem(menu, "\uE711", LocalizationService.Instance["Main_ExitQuickTools"], ExitFromTray, Drawing.Color.FromArgb(220, 38, 38));
    }

    private void AddTraySeparator(Forms.ContextMenuStrip menu)
    {
        var separator = new Forms.ToolStripSeparator
        {
            Margin = new Forms.Padding(10, 6, 10, 6)
        };
        menu.Items.Add(separator);
    }

    private void AddTrayItem(
        Forms.ContextMenuStrip menu,
        string iconGlyph,
        string text,
        Action action,
        Drawing.Color? iconColor = null)
    {
        var item = CreateTrayMenuItem(iconGlyph, text, iconColor);
        item.Click += (_, _) => Dispatcher.Invoke(action);
        menu.Items.Add(item);
    }

    private void AddTrayItem(
        Forms.ContextMenuStrip menu,
        string iconGlyph,
        string text,
        Func<Task> action,
        Drawing.Color? iconColor = null)
    {
        var item = CreateTrayMenuItem(iconGlyph, text, iconColor);
        item.Click += async (_, _) =>
        {
            var task = await Dispatcher.InvokeAsync(action);
            await task;
        };
        menu.Items.Add(item);
    }

    private static Forms.ToolStripMenuItem CreateTrayMenuItem(
        string iconGlyph,
        string text,
        Drawing.Color? iconColor = null)
    {
        return new Forms.ToolStripMenuItem(text)
        {
            AutoSize = false,
            Height = 34,
            Width = 310,
            Padding = new Forms.Padding(8, 0, 12, 0),
            Image = CreateMenuIcon(iconGlyph, iconColor ?? Drawing.Color.FromArgb(37, 99, 235)),
            ImageScaling = Forms.ToolStripItemImageScaling.None
        };
    }

    private static Drawing.Bitmap CreateMenuIcon(string glyph, Drawing.Color color)
    {
        var bitmap = new Drawing.Bitmap(22, 22);
        using var graphics = Drawing.Graphics.FromImage(bitmap);
        graphics.Clear(Drawing.Color.Transparent);
        graphics.TextRenderingHint = DrawingText.TextRenderingHint.AntiAliasGridFit;

        using var font = new Drawing.Font("Segoe MDL2 Assets", 13.5f, Drawing.FontStyle.Regular, Drawing.GraphicsUnit.Pixel);
        using var brush = new Drawing.SolidBrush(color);
        var format = new Drawing.StringFormat
        {
            Alignment = Drawing.StringAlignment.Center,
            LineAlignment = Drawing.StringAlignment.Center
        };
        graphics.DrawString(glyph, font, brush, new Drawing.RectangleF(0, 0, bitmap.Width, bitmap.Height), format);
        return bitmap;
    }

    private async Task SetPowerPlanFromTrayAsync(string kind)
    {
        try
        {
            await _viewModel.PowerService.SetPlanByKindAsync(kind);
            await _viewModel.Dashboard.RefreshAsync();
            await _viewModel.PowerModes.RefreshAsync();
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(2200, "QuickTools", ex.Message, Forms.ToolTipIcon.Warning);
        }
    }

    private void ExitFromTray()
    {
        _isExitRequested = true;
        Close();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await _updateService.CheckForUpdatesAsync(this);
        }
        catch
        {
            // Update checks should never interrupt normal app usage.
        }
    }

    private void RegisterHotkey()
    {
        var registered = _hotkeyService.Register(this, _viewModel.Settings.AutoClickerHotkey, out var errorMessage);
        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        if (!registered && !string.IsNullOrWhiteSpace(errorMessage))
        {
            System.Windows.MessageBox.Show(errorMessage, "QuickTools hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var quickToggleRegistered = _quickToggleHotkeyService.Register(this, _viewModel.Settings.QuickToggleHotkey, out var quickToggleError);
        _quickToggleHotkeyService.HotkeyPressed -= OnQuickToggleHotkeyPressed;
        _quickToggleHotkeyService.HotkeyPressed += OnQuickToggleHotkeyPressed;

        _viewModel.QuickToggle.GlobalStatus = quickToggleRegistered
            ? _viewModel.QuickToggle.IsEnabled
                ? LocalizationService.Instance.Format("Main_QuickToggleOpenWheel", _viewModel.QuickToggle.Hotkey)
                : LocalizationService.Instance["Main_WheelDisabled"]
            : quickToggleError ?? LocalizationService.Instance["Main_QuickToggleHotkeyError"];
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(_viewModel.AutoClicker.Toggle);
    }

    private void OnQuickToggleHotkeyPressed(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_viewModel.QuickToggle.IsEnabled)
            {
                _quickPickerWindow.ShowAtCursor(_viewModel.QuickToggle.WheelActions);
            }
        });
    }

    private void OnAutoClickerStateChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (_viewModel.AutoClickerService.IsRunning)
            {
                _systemCursorService.Apply(_viewModel.AutoClicker.SelectedActiveCursor);
            }
            else
            {
                _systemCursorService.Restore();
            }
        });
    }

    private void ApplyTheme()
    {
        var theme = _viewModel.Settings.Theme;
        var useDark = theme == "Dark" || (theme == "System" && IsSystemDarkTheme());

        SetColor("WindowBackgroundColor", useDark ? MediaColor.FromRgb(24, 26, 32) : MediaColor.FromRgb(246, 247, 251));
        SetColor("SurfaceColor", useDark ? MediaColor.FromRgb(34, 37, 45) : Colors.White);
        SetColor("SurfaceAltColor", useDark ? MediaColor.FromRgb(43, 47, 58) : MediaColor.FromRgb(243, 244, 248));
        SetColor("TextColor", useDark ? MediaColor.FromRgb(240, 244, 248) : MediaColor.FromRgb(31, 35, 40));
        SetColor("MutedTextColor", useDark ? MediaColor.FromRgb(166, 174, 187) : MediaColor.FromRgb(105, 113, 125));
        SetColor("BorderColor", useDark ? MediaColor.FromRgb(61, 67, 80) : MediaColor.FromRgb(226, 230, 239));

        SetBrush("WindowBackgroundBrush", "WindowBackgroundColor");
        SetBrush("SurfaceBrush", "SurfaceColor");
        WpfApplication.Current.Resources["GlassBrush"] = useDark
            ? new SolidColorBrush(MediaColor.FromArgb(220, 34, 37, 45))
            : new SolidColorBrush(MediaColor.FromArgb(224, 255, 255, 255));
        WpfApplication.Current.Resources["GlassAltBrush"] = useDark
            ? new SolidColorBrush(MediaColor.FromArgb(190, 43, 47, 58))
            : new SolidColorBrush(MediaColor.FromArgb(190, 243, 246, 251));
        SetBrush("SurfaceAltBrush", "SurfaceAltColor");
        SetBrush("TextBrush", "TextColor");
        SetBrush("MutedTextBrush", "MutedTextColor");
        SetBrush("BorderBrushSoft", "BorderColor");
    }

    private static bool IsSystemDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    private void SetColor(string resourceKey, MediaColor color)
    {
        Resources[resourceKey] = color;
        WpfApplication.Current.Resources[resourceKey] = color;
    }

    private void SetBrush(string brushKey, string colorKey)
    {
        var color = (MediaColor)WpfApplication.Current.Resources[colorKey];
        var brush = new SolidColorBrush(color);
        Resources[brushKey] = brush;
        WpfApplication.Current.Resources[brushKey] = brush;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _systemCursorService.Restore();
        _viewModel.AutoClickerService.Stop();
        _quickPickerWindow.Close();
        _hotkeyService.Dispose();
        _quickToggleHotkeyService.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.ContextMenuStrip?.Dispose();
        _trayIcon.Dispose();
    }

    private sealed class TrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        private static readonly Drawing.Color MenuBack = Drawing.Color.FromArgb(248, 250, 255);
        private static readonly Drawing.Color Border = Drawing.Color.FromArgb(226, 230, 239);
        private static readonly Drawing.Color Hover = Drawing.Color.FromArgb(234, 241, 255);
        private static readonly Drawing.Color IconColumn = Drawing.Color.FromArgb(242, 246, 251);

        public TrayMenuRenderer() : base(new TrayMenuColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
        {
            using var brush = new Drawing.SolidBrush(MenuBack);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderImageMargin(Forms.ToolStripRenderEventArgs e)
        {
            var bounds = new Drawing.Rectangle(0, 0, 43, e.ToolStrip.Height);
            using var brush = new Drawing.SolidBrush(IconColumn);
            e.Graphics.FillRectangle(brush, bounds);
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
            {
                return;
            }

            e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Drawing.Rectangle(5, 2, e.Item.Width - 10, e.Item.Height - 4);
            using var path = CreateRoundedRectangle(rect, 8);
            using var brush = new Drawing.SolidBrush(Hover);
            using var pen = new Drawing.Pen(Drawing.Color.FromArgb(190, 37, 99, 235));
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.Height / 2;
            using var pen = new Drawing.Pen(Border);
            e.Graphics.DrawLine(pen, 52, y, e.Item.Width - 12, y);
        }

        protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
        {
            var rect = new Drawing.Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using var pen = new Drawing.Pen(Border);
            e.Graphics.DrawRectangle(pen, rect);
        }

        private static Drawing2D.GraphicsPath CreateRoundedRectangle(Drawing.Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new Drawing2D.GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class TrayMenuColorTable : Forms.ProfessionalColorTable
    {
        public override Drawing.Color ToolStripDropDownBackground => Drawing.Color.FromArgb(248, 250, 255);
        public override Drawing.Color ImageMarginGradientBegin => Drawing.Color.FromArgb(242, 246, 251);
        public override Drawing.Color ImageMarginGradientMiddle => Drawing.Color.FromArgb(242, 246, 251);
        public override Drawing.Color ImageMarginGradientEnd => Drawing.Color.FromArgb(242, 246, 251);
        public override Drawing.Color MenuItemSelected => Drawing.Color.FromArgb(234, 241, 255);
        public override Drawing.Color MenuItemBorder => Drawing.Color.FromArgb(37, 99, 235);
        public override Drawing.Color SeparatorDark => Drawing.Color.FromArgb(226, 230, 239);
        public override Drawing.Color SeparatorLight => Drawing.Color.FromArgb(226, 230, 239);
    }
}
