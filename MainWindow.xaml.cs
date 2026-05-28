using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using QuickTools.Services;
using QuickTools.ViewModels;

namespace QuickTools;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly HotkeyService _hotkeyService = new();
    private readonly HotkeyService _quickToggleHotkeyService = new(9061);
    private readonly SystemCursorService _systemCursorService = new();
    private readonly UpdateService _updateService = new();
    private readonly QuickPickerWindow _quickPickerWindow;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _quickPickerWindow = new QuickPickerWindow(_viewModel.QuickToggle.QuickActionService);

        SourceInitialized += (_, _) => RegisterHotkey();
        Loaded += OnLoaded;
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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        await _viewModel.Dashboard.RefreshAsync();
        await _viewModel.PowerModes.RefreshAsync();
        _ = CheckForUpdatesAsync();
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
            MessageBox.Show(errorMessage, "QuickTools hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var quickToggleRegistered = _quickToggleHotkeyService.Register(this, _viewModel.Settings.QuickToggleHotkey, out var quickToggleError);
        _quickToggleHotkeyService.HotkeyPressed -= OnQuickToggleHotkeyPressed;
        _quickToggleHotkeyService.HotkeyPressed += OnQuickToggleHotkeyPressed;

        _viewModel.QuickToggle.GlobalStatus = quickToggleRegistered
            ? _viewModel.QuickToggle.IsEnabled
                ? $"Press {_viewModel.QuickToggle.Hotkey} to open wheel"
                : "Wheel is disabled"
            : quickToggleError ?? "Could not register Quick Toggle hotkey";
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

        SetColor("WindowBackgroundColor", useDark ? Color.FromRgb(24, 26, 32) : Color.FromRgb(246, 247, 251));
        SetColor("SurfaceColor", useDark ? Color.FromRgb(34, 37, 45) : Colors.White);
        SetColor("SurfaceAltColor", useDark ? Color.FromRgb(43, 47, 58) : Color.FromRgb(243, 244, 248));
        SetColor("TextColor", useDark ? Color.FromRgb(240, 244, 248) : Color.FromRgb(31, 35, 40));
        SetColor("MutedTextColor", useDark ? Color.FromRgb(166, 174, 187) : Color.FromRgb(105, 113, 125));
        SetColor("BorderColor", useDark ? Color.FromRgb(61, 67, 80) : Color.FromRgb(226, 230, 239));

        SetBrush("WindowBackgroundBrush", "WindowBackgroundColor");
        SetBrush("SurfaceBrush", "SurfaceColor");
        Application.Current.Resources["GlassBrush"] = useDark
            ? new SolidColorBrush(Color.FromArgb(220, 34, 37, 45))
            : new SolidColorBrush(Color.FromArgb(224, 255, 255, 255));
        Application.Current.Resources["GlassAltBrush"] = useDark
            ? new SolidColorBrush(Color.FromArgb(190, 43, 47, 58))
            : new SolidColorBrush(Color.FromArgb(190, 243, 246, 251));
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

    private void SetColor(string resourceKey, Color color)
    {
        Resources[resourceKey] = color;
        Application.Current.Resources[resourceKey] = color;
    }

    private void SetBrush(string brushKey, string colorKey)
    {
        var color = (Color)Application.Current.Resources[colorKey];
        var brush = new SolidColorBrush(color);
        Resources[brushKey] = brush;
        Application.Current.Resources[brushKey] = brush;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _systemCursorService.Restore();
        _viewModel.AutoClickerService.Stop();
        _quickPickerWindow.Close();
        _hotkeyService.Dispose();
        _quickToggleHotkeyService.Dispose();
    }
}
