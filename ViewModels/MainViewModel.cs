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
        Settings.Language = LocalizationService.NormalizeLanguage(Settings.Language);
        LocalizationService.Instance.SetLanguage(Settings.Language);

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

        Dashboard = new DashboardViewModel(AutoClickerService, PowerService, AutoClicker, QuickToggle)
        {
            IsQuickToggleActive = QuickToggle.IsEnabled
        };

        if (Settings.DashboardCardOrder.Count > 0)
            Dashboard.LoadCardOrder(Settings.DashboardCardOrder);

        Dashboard.CardOrder.CollectionChanged += (_, _) =>
        {
            Settings.DashboardCardOrder = [.. Dashboard.GetCardOrder()];
            SettingsService.Save(Settings);
        };

        QuickToggle.PropertyChanged += OnQuickTogglePropertyChanged;

        PowerScheduler = new PowerSchedulerViewModel(PowerService);
        PowerService.EventsChanged += OnPowerEventsChanged;
        PowerService.LoadEvents(Settings.ScheduledPowerEvents);
        PowerModes = new PowerModesViewModel(PowerService);
        SettingsViewModel = new SettingsViewModel(SettingsService, Settings);
        SettingsViewModel.SettingsSaved += (_, _) =>
        {
            LocalizationService.Instance.SetLanguage(Settings.Language);
            AutoClicker.Hotkey = Settings.AutoClickerHotkey;
            AutoClicker.SelectedActiveCursor = Settings.AutoClickerActiveCursor;
            QuickToggle.Hotkey = Settings.QuickToggleHotkey;
            QuickToggle.GlobalStatus = QuickToggle.IsEnabled
                ? LocalizationService.Instance.Format("Main_QuickToggleOpenWheel", QuickToggle.Hotkey)
                : LocalizationService.Instance["Main_WheelDisabled"];
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        };

        NavigationItems =
        [
            new NavigationItem { TitleKey = "Nav_Dashboard", Icon = "\uE80F", ViewModel = Dashboard },
            new NavigationItem { TitleKey = "Nav_AutoClicker", Icon = "\uE7C9", ViewModel = AutoClicker },
            new NavigationItem { TitleKey = "Nav_QuickToggle", Icon = "\uE8A7", ViewModel = QuickToggle },
            new NavigationItem { TitleKey = "Nav_PowerScheduler", Icon = "\uE823", ViewModel = PowerScheduler },
            new NavigationItem { TitleKey = "Nav_PowerModes", Icon = "\uE945", ViewModel = PowerModes },
            new NavigationItem { TitleKey = "Nav_Settings", Icon = "\uE713", ViewModel = SettingsViewModel }
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
                ? LocalizationService.Instance.Format("Main_QuickToggleOpenWheel", QuickToggle.Hotkey)
                : LocalizationService.Instance["Main_WheelDisabled"];
        }

        if (e.PropertyName == nameof(QuickToggleViewModel.WheelCount))
        {
            Settings.QuickToggleWheelIds = QuickToggle.GetWheelIds().ToList();
            SettingsService.Save(Settings);
        }
    }

    private void OnPowerEventsChanged(object? sender, EventArgs e)
    {
        Settings.ScheduledPowerEvents = PowerService.ScheduledEvents
            .Where(item => item.ExecuteAt > DateTime.Now)
            .Select(item => new ScheduledPowerEventSetting
            {
                Action = item.Action,
                ExecuteAt = item.ExecuteAt,
                IsActive = item.IsActive
            })
            .ToList();
        SettingsService.Save(Settings);
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
