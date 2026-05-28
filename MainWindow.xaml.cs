using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using QuickTools.Models;
using QuickTools.Services;
using QuickTools.ViewModels;
using QuickTools.Views;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
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
    private readonly TrayMenuWindow _trayMenuWindow = new();
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
        var appIcon = Drawing.Icon.ExtractAssociatedIcon(
            System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? Drawing.SystemIcons.Application;

        var trayIcon = new Forms.NotifyIcon
        {
            Icon    = appIcon,
            Text    = "QuickTools",
            Visible = true
        };

        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        trayIcon.MouseUp += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
            {
                RestoreFromTray();
            }
            else if (args.Button == Forms.MouseButtons.Right)
            {
                Dispatcher.Invoke(() =>
                {
                    var cursor = Forms.Cursor.Position;
                    _trayMenuWindow.ShowMenu(BuildMenuEntries(), cursor);
                });
            }
        };
        return trayIcon;
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

    private List<TrayMenuEntry> BuildMenuEntries()
    {
        var loc = LocalizationService.Instance;
        var entries = new List<TrayMenuEntry>
        {
            new() { Icon = "\uE8A7", Label = loc["Main_OpenQuickTools"],
                    Action = RestoreFromTray },

            TrayMenuEntry.Sep(),

            new() { Icon = "\uE7C9",
                    Label  = _viewModel.AutoClickerService.IsRunning
                                 ? loc["Main_StopAutoClicker"]
                                 : loc["Main_StartAutoClicker"],
                    Action = () => Dispatcher.Invoke(_viewModel.AutoClicker.Toggle) },

            new() { Icon = "\uE8A7",
                    Label  = _viewModel.QuickToggle.IsEnabled
                                 ? loc["Main_DisableQuickToggle"]
                                 : loc["Main_EnableQuickToggle"],
                    Action = () => Dispatcher.Invoke(() =>
                        _viewModel.QuickToggle.IsEnabled = !_viewModel.QuickToggle.IsEnabled) },

            new() { Icon = "\uE9D9", Label = loc["Main_OpenQuickToggleWheel"],
                    Action = () => Dispatcher.Invoke(() =>
                    {
                        if (_viewModel.QuickToggle.IsEnabled)
                            _quickPickerWindow.ShowAtCursor(_viewModel.QuickToggle.WheelActions);
                    }) },

            TrayMenuEntry.Sep(),

            new() { Icon = "\uE74F", Label = loc["QuickAction_mute_Name"],
                    Action = () => Dispatcher.Invoke(() => _viewModel.QuickToggle.QuickActionService.Execute("mute")) },
            new() { Icon = "\uE767", Label = loc["QuickAction_vol_up_Name"],
                    Action = () => Dispatcher.Invoke(() => _viewModel.QuickToggle.QuickActionService.Execute("vol_up")) },
            new() { Icon = "\uE993", Label = loc["QuickAction_vol_down_Name"],
                    Action = () => Dispatcher.Invoke(() => _viewModel.QuickToggle.QuickActionService.Execute("vol_down")) },
            new() { Icon = "\uE8C8", Label = loc["QuickAction_clipboard_Name"],
                    Action = () => Dispatcher.Invoke(() => _viewModel.QuickToggle.QuickActionService.Execute("clipboard")) },
            new() { Icon = "\uE72E", Label = loc["QuickAction_lock_Name"],
                    Action = () => Dispatcher.Invoke(() => _viewModel.QuickToggle.QuickActionService.Execute("lock")) },

            TrayMenuEntry.Sep(),

            new() { Icon = "\uE9D2",
                    Label  = loc.Format("Main_PowerPrefix", loc.TranslatePowerPlanKind("Balanced")),
                    Action = () => _ = Dispatcher.InvokeAsync(async () => await SetPowerPlanFromTrayAsync("Balanced")) },
            new() { Icon = "\uE945",
                    Label  = loc.Format("Main_PowerPrefix", loc.TranslatePowerPlanKind("HighPerformance")),
                    Action = () => _ = Dispatcher.InvokeAsync(async () => await SetPowerPlanFromTrayAsync("HighPerformance")) },
            new() { Icon = "\uE83F",
                    Label  = loc.Format("Main_PowerPrefix", loc.TranslatePowerPlanKind("PowerSaver")),
                    Action = () => _ = Dispatcher.InvokeAsync(async () => await SetPowerPlanFromTrayAsync("PowerSaver")) },
            new() { Icon = "\uE823", Label = loc["Main_PauseScheduledPowerEvents"],
                    Action = () => Dispatcher.Invoke(() => _viewModel.PowerService.PauseAllEvents()) },

            TrayMenuEntry.Sep(),

            new() { Icon = "\uE711", Label = loc["Main_ExitQuickTools"],
                    Action = ExitFromTray, IsDanger = true }
        };
        return entries;
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

        SetColor("WindowBackgroundColor", useDark ? MediaColor.FromRgb(15, 18, 24) : MediaColor.FromRgb(246, 247, 251));
        SetColor("WindowGradientStartColor", useDark ? MediaColor.FromRgb(14, 17, 23) : MediaColor.FromRgb(247, 250, 255));
        SetColor("WindowGradientMidColor", useDark ? MediaColor.FromRgb(19, 24, 32) : MediaColor.FromRgb(239, 244, 251));
        SetColor("WindowGradientEndColor", useDark ? MediaColor.FromRgb(12, 14, 20) : MediaColor.FromRgb(248, 247, 255));
        SetColor("SidebarBackgroundColor", useDark ? MediaColor.FromRgb(10, 12, 17) : Colors.White);
        SetColor("SurfaceColor", useDark ? MediaColor.FromRgb(22, 27, 34) : Colors.White);
        SetColor("SurfaceAltColor", useDark ? MediaColor.FromRgb(30, 36, 45) : MediaColor.FromRgb(243, 244, 248));
        SetColor("TextColor", useDark ? MediaColor.FromRgb(232, 237, 243) : MediaColor.FromRgb(31, 35, 40));
        SetColor("MutedTextColor", useDark ? MediaColor.FromRgb(148, 163, 184) : MediaColor.FromRgb(105, 113, 125));
        SetColor("BorderColor", useDark ? MediaColor.FromRgb(52, 62, 76) : MediaColor.FromRgb(226, 230, 239));
        SetColor("SelectionColor", useDark ? MediaColor.FromRgb(30, 58, 95) : MediaColor.FromRgb(234, 241, 255));
        SetColor("HoverColor", useDark ? MediaColor.FromRgb(26, 32, 41) : MediaColor.FromRgb(241, 245, 251));
        SetColor("AccentSurfaceColor", useDark ? MediaColor.FromRgb(21, 45, 74) : MediaColor.FromRgb(234, 241, 255));
        SetColor("WarningBackgroundColor", useDark ? MediaColor.FromRgb(60, 44, 24) : MediaColor.FromRgb(255, 247, 237));
        SetColor("WarningBorderColor", useDark ? MediaColor.FromRgb(120, 84, 32) : MediaColor.FromRgb(254, 215, 170));
        SetColor("WarningTextColor", useDark ? MediaColor.FromRgb(251, 191, 36) : MediaColor.FromRgb(154, 52, 18));
        SetColor("DangerSubtleColor", useDark ? MediaColor.FromArgb(72, 239, 68, 68) : MediaColor.FromArgb(32, 204, 51, 51));
        SetColor("DangerSoftTextColor", useDark ? MediaColor.FromRgb(248, 113, 113) : MediaColor.FromArgb(204, 204, 51, 51));

        SetBrush("WindowBackgroundBrush", "WindowBackgroundColor");
        SetBrush("SidebarBackgroundBrush", "SidebarBackgroundColor");
        SetBrush("SurfaceBrush", "SurfaceColor");
        WpfApplication.Current.Resources["GlassBrush"] = useDark
            ? new SolidColorBrush(MediaColor.FromArgb(214, 23, 28, 36))
            : new SolidColorBrush(MediaColor.FromArgb(224, 255, 255, 255));
        WpfApplication.Current.Resources["GlassAltBrush"] = useDark
            ? new SolidColorBrush(MediaColor.FromArgb(196, 31, 38, 48))
            : new SolidColorBrush(MediaColor.FromArgb(190, 243, 246, 251));
        SetBrush("SurfaceAltBrush", "SurfaceAltColor");
        SetBrush("TextBrush", "TextColor");
        SetBrush("MutedTextBrush", "MutedTextColor");
        SetBrush("BorderBrushSoft", "BorderColor");
        SetBrush("SelectionBrush", "SelectionColor");
        SetBrush("HoverBrush", "HoverColor");
        SetBrush("AccentSurfaceBrush", "AccentSurfaceColor");
        SetBrush("WarningBackgroundBrush", "WarningBackgroundColor");
        SetBrush("WarningBorderBrush", "WarningBorderColor");
        SetBrush("WarningTextBrush", "WarningTextColor");
        SetBrush("DangerSubtleBrush", "DangerSubtleColor");
        SetBrush("DangerSoftTextBrush", "DangerSoftTextColor");
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
        _trayMenuWindow.Close();
        _hotkeyService.Dispose();
        _quickToggleHotkeyService.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
