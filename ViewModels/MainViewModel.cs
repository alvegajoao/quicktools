using System.Collections.ObjectModel;
using System.ComponentModel;
using QuickTools.Helpers;
using QuickTools.Models;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private NavigationItem _selectedNavigationItem;

    public MainViewModel()
    {
        SettingsService = new SettingsService();
        Settings = SettingsService.Load();

        PowerService = new PowerService();
        var mouseInputService = new MouseInputService();
        AutoClickerService = new AutoClickerService(mouseInputService);
        var quickActionService = new QuickActionService();

        AutoClicker = new AutoClickerViewModel(AutoClickerService)
        {
            Hotkey = Settings.AutoClickerHotkey,
            SelectedActiveCursor = Settings.AutoClickerActiveCursor,
            SpeedPercent = Settings.AutoClickerSpeedPercent,
            SelectedMouseButton = Settings.AutoClickerMouseButton,
            SelectedClickType = Settings.AutoClickerClickType,
            UseCurrentMousePosition = Settings.AutoClickerUseCurrentMousePosition
        };
        AutoClicker.PropertyChanged += OnAutoClickerPropertyChanged;

        QuickToggle = new QuickToggleViewModel(quickActionService)
        {
            IsEnabled = Settings.QuickToggleEnabled,
            Hotkey = Settings.QuickToggleHotkey
        };
        QuickToggle.LoadWheelIds(Settings.QuickToggleWheelIds);

        Dashboard = new DashboardViewModel(AutoClickerService, PowerService, AutoClicker)
        {
            IsQuickToggleActive = QuickToggle.IsEnabled
        };
        QuickToggle.PropertyChanged += OnQuickTogglePropertyChanged;

        PowerScheduler = new PowerSchedulerViewModel(PowerService);
        PowerModes = new PowerModesViewModel(PowerService);
        SettingsViewModel = new SettingsViewModel(SettingsService, Settings);
        SettingsViewModel.SettingsSaved += (_, _) =>
        {
            AutoClicker.Hotkey = Settings.AutoClickerHotkey;
            AutoClicker.SelectedActiveCursor = Settings.AutoClickerActiveCursor;
            QuickToggle.Hotkey = Settings.QuickToggleHotkey;
            QuickToggle.GlobalStatus = QuickToggle.IsEnabled
                ? $"Press {QuickToggle.Hotkey} to open wheel"
                : "Wheel is disabled";
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        };

        NavigationItems =
        [
            new NavigationItem { Title = "Dashboard", Icon = "\uE80F", ViewModel = Dashboard },
            new NavigationItem { Title = "Auto Clicker", Icon = "\uE7C9", ViewModel = AutoClicker },
            new NavigationItem { Title = "Quick Toggle", Icon = "\uE7C9", ViewModel = QuickToggle },
            new NavigationItem { Title = "Power Scheduler", Icon = "\uE823", ViewModel = PowerScheduler },
            new NavigationItem { Title = "Power Modes", Icon = "\uE945", ViewModel = PowerModes },
            new NavigationItem { Title = "Settings", Icon = "\uE713", ViewModel = SettingsViewModel }
        ];

        _selectedNavigationItem = NavigationItems[0];
    }

    public SettingsService SettingsService { get; }
    public AppSettings Settings { get; }
    public PowerService PowerService { get; }
    public AutoClickerService AutoClickerService { get; }
    public DashboardViewModel Dashboard { get; }
    public AutoClickerViewModel AutoClicker { get; }
    public QuickToggleViewModel QuickToggle { get; }
    public PowerSchedulerViewModel PowerScheduler { get; }
    public PowerModesViewModel PowerModes { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public NavigationItem SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
    }

    public object CurrentViewModel => SelectedNavigationItem.ViewModel;

    public event EventHandler? SettingsChanged;

    private void OnQuickTogglePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuickToggleViewModel.IsEnabled))
        {
            Dashboard.IsQuickToggleActive = QuickToggle.IsEnabled;
            Settings.QuickToggleEnabled = QuickToggle.IsEnabled;
            Settings.QuickToggleWheelIds = QuickToggle.GetWheelIds().ToList();
            SettingsService.Save(Settings);
            QuickToggle.GlobalStatus = QuickToggle.IsEnabled
                ? $"Press {QuickToggle.Hotkey} to open wheel"
                : "Wheel is disabled";
        }

        if (e.PropertyName == nameof(QuickToggleViewModel.WheelCount))
        {
            Settings.QuickToggleWheelIds = QuickToggle.GetWheelIds().ToList();
            SettingsService.Save(Settings);
        }
    }

    private void OnAutoClickerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AutoClickerViewModel.SelectedActiveCursor))
        {
            Settings.AutoClickerActiveCursor = AutoClicker.SelectedActiveCursor;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        if (e.PropertyName is nameof(AutoClickerViewModel.SelectedActiveCursor)
            or nameof(AutoClickerViewModel.SpeedPercent)
            or nameof(AutoClickerViewModel.SelectedMouseButton)
            or nameof(AutoClickerViewModel.SelectedClickType)
            or nameof(AutoClickerViewModel.UseCurrentMousePosition))
        {
            Settings.AutoClickerActiveCursor = AutoClicker.SelectedActiveCursor;
            Settings.AutoClickerSpeedPercent = AutoClicker.SpeedPercent;
            Settings.AutoClickerMouseButton = AutoClicker.SelectedMouseButton;
            Settings.AutoClickerClickType = AutoClicker.SelectedClickType;
            Settings.AutoClickerUseCurrentMousePosition = AutoClicker.UseCurrentMousePosition;
            SettingsService.Save(Settings);
        }
    }
}
