using System.ComponentModel;
using System.Windows.Input;
using QuickTools.Helpers;
using QuickTools.Models;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
    private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string PowerSaverGuid = "a1841308-3541-4fab-bc81-f71556f20b4a";
    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private readonly AutoClickerService _autoClickerService;
    private readonly PowerService _powerService;
    private readonly AutoClickerViewModel _autoClickerViewModel;
    private readonly QuickToggleViewModel _quickToggleViewModel;
    private bool _powerPlanReadFailed;

    private string _currentPowerPlan = "";
    private string _currentPowerPlanGuid = "";
    private string _nextScheduledAction = "";
    private string _schedulerStatus = "";
    private string _schedulerSummary = "";
    private bool _isQuickToggleActive;

    public DashboardViewModel(
        AutoClickerService autoClickerService,
        PowerService powerService,
        AutoClickerViewModel autoClickerViewModel,
        QuickToggleViewModel quickToggleViewModel)
    {
        _autoClickerService = autoClickerService;
        _powerService = powerService;
        _autoClickerViewModel = autoClickerViewModel;
        _quickToggleViewModel = quickToggleViewModel;

        StartAutoClickerCommand = _autoClickerViewModel.StartCommand;
        StopAutoClickerCommand = _autoClickerViewModel.StopCommand;
        PauseAllEventsCommand = new RelayCommand(() => _powerService.PauseAllEvents());
        SetBalancedCommand = new AsyncRelayCommand(async () => await SetPlanAsync("Balanced"));
        SetHighPerformanceCommand = new AsyncRelayCommand(async () => await SetPlanAsync("HighPerformance"));
        SetPowerSaverCommand = new AsyncRelayCommand(async () => await SetPlanAsync("PowerSaver"));
        SetUltimateCommand = new AsyncRelayCommand(async () => await SetPlanAsync("UltimatePerformance"));

        _autoClickerService.StateChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AutoClickerStatus));
            OnPropertyChanged(nameof(IsAutoClickerRunning));
            OnPropertyChanged(nameof(AutoClickerSummary));
        };

        _autoClickerViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AutoClickerViewModel.ElapsedTime)
                or nameof(AutoClickerViewModel.Hotkey)
                or nameof(AutoClickerViewModel.SpeedPercent))
            {
                OnPropertyChanged(nameof(AutoClickerElapsedTime));
                OnPropertyChanged(nameof(AutoClickerHotkey));
                OnPropertyChanged(nameof(AutoClickerHotkeyLabel));
                OnPropertyChanged(nameof(AutoClickerSummary));
                OnPropertyChanged(nameof(AutoClickerSpeedLabel));
            }
        };

        _quickToggleViewModel.PropertyChanged += OnQuickTogglePropertyChanged;
        _powerService.ScheduledActionChanged += (_, _) => RefreshSchedulerStatus();
        _powerService.EventsChanged += (_, _) => RefreshSchedulerStatus();
        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AutoClickerStatus));
            OnPropertyChanged(nameof(AutoClickerSummary));
            OnPropertyChanged(nameof(AutoClickerHotkeyLabel));
            OnPropertyChanged(nameof(QuickToggleStatus));
            OnPropertyChanged(nameof(QuickToggleSummary));
            OnPropertyChanged(nameof(PowerPlanSummary));
            RefreshSchedulerStatus();
        };
        RefreshSchedulerStatus();
    }

    public string AutoClickerStatus => LocalizationService.Instance[_autoClickerService.IsRunning ? "AutoClicker_Running" : "AutoClicker_Stopped"];
    public bool IsAutoClickerRunning => _autoClickerService.IsRunning;
    public string AutoClickerElapsedTime => _autoClickerViewModel.ElapsedTime;
    public string AutoClickerHotkey => _autoClickerViewModel.Hotkey;
    public string AutoClickerHotkeyLabel => LocalizationService.Instance.Format("Common_Hotkey", _autoClickerViewModel.Hotkey);
    public string AutoClickerSummary => _autoClickerService.IsRunning
        ? LocalizationService.Instance.Format("Dashboard_AutoClickerSummaryActive", _autoClickerViewModel.ElapsedTime)
        : LocalizationService.Instance.Format("Dashboard_AutoClickerSummarySpeed", _autoClickerViewModel.SpeedPercent);
    public string AutoClickerSpeedLabel => $"{_autoClickerViewModel.SpeedPercent}%";

    public bool IsQuickToggleActive
    {
        get => _isQuickToggleActive;
        set
        {
            if (SetProperty(ref _isQuickToggleActive, value))
            {
                OnPropertyChanged(nameof(QuickToggleStatus));
                OnPropertyChanged(nameof(QuickToggleSummary));
            }
        }
    }

    public string QuickToggleStatus => LocalizationService.Instance[IsQuickToggleActive ? "Common_Enabled" : "Common_Disabled"];
    public string QuickToggleSummary => IsQuickToggleActive
        ? LocalizationService.Instance.Format("Dashboard_QuickToggleSummaryEnabled", _quickToggleViewModel.WheelCount, _quickToggleViewModel.Hotkey)
        : LocalizationService.Instance.Format("Dashboard_QuickToggleSummaryDisabled", _quickToggleViewModel.WheelCount);
    public IEnumerable<QuickAction> QuickToggleActions => _quickToggleViewModel.WheelActions;

    public string CurrentPowerPlan
    {
        get => _currentPowerPlan;
        set
        {
            if (SetProperty(ref _currentPowerPlan, value))
            {
                OnPropertyChanged(nameof(PowerPlanSummary));
            }
        }
    }

    public string PowerPlanSummary => _powerPlanReadFailed
        ? LocalizationService.Instance["Dashboard_CouldNotReadPowerCfg"]
        : LocalizationService.Instance["Dashboard_CurrentWindowsMode"];

    public bool IsBalancedActive => IsCurrentPlan(BalancedGuid);
    public bool IsHighPerformanceActive => IsCurrentPlan(HighPerformanceGuid);
    public bool IsPowerSaverActive => IsCurrentPlan(PowerSaverGuid);
    public bool IsUltimateActive => IsCurrentPlan(UltimatePerformanceGuid);

    public string NextScheduledAction
    {
        get => _nextScheduledAction;
        set => SetProperty(ref _nextScheduledAction, value);
    }

    public string SchedulerStatus
    {
        get => _schedulerStatus;
        set => SetProperty(ref _schedulerStatus, value);
    }

    public string SchedulerSummary
    {
        get => _schedulerSummary;
        set => SetProperty(ref _schedulerSummary, value);
    }

    public IEnumerable<ScheduledEvent> ActiveSchedulerEvents => _powerService.ScheduledEvents
        .Where(item => item.IsActive)
        .OrderBy(item => item.ExecuteAt)
        .Take(3);

    public ICommand StartAutoClickerCommand { get; }
    public ICommand StopAutoClickerCommand { get; }
    public ICommand PauseAllEventsCommand { get; }
    public ICommand SetBalancedCommand { get; }
    public ICommand SetHighPerformanceCommand { get; }
    public ICommand SetPowerSaverCommand { get; }
    public ICommand SetUltimateCommand { get; }

    public async Task RefreshAsync()
    {
        try
        {
            var activePlan = (await _powerService.GetPowerPlansAsync()).FirstOrDefault(plan => plan.IsActive);
            _powerPlanReadFailed = false;
            CurrentPowerPlan = activePlan?.Name ?? LocalizationService.Instance["Common_Unknown"];
            CurrentPowerPlanGuid = activePlan?.Guid ?? "";
        }
        catch
        {
            _powerPlanReadFailed = true;
            CurrentPowerPlan = LocalizationService.Instance["Common_Unavailable"];
            CurrentPowerPlanGuid = "";
        }

        OnPropertyChanged(nameof(PowerPlanSummary));
    }

    private void OnQuickTogglePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(QuickToggleViewModel.IsEnabled)
            or nameof(QuickToggleViewModel.WheelCount)
            or nameof(QuickToggleViewModel.Hotkey))
        {
            IsQuickToggleActive = _quickToggleViewModel.IsEnabled;
            OnPropertyChanged(nameof(QuickToggleStatus));
            OnPropertyChanged(nameof(QuickToggleSummary));
            OnPropertyChanged(nameof(QuickToggleActions));
        }
    }

    private void RefreshSchedulerStatus()
    {
        var activeEvents = _powerService.ScheduledEvents.Where(item => item.IsActive).ToList();
        var pausedEvents = _powerService.ScheduledEvents.Where(item => !item.IsActive).ToList();
        var nextEvent = activeEvents.OrderBy(item => item.ExecuteAt).FirstOrDefault();

        SchedulerStatus = activeEvents.Count switch
        {
            0 when pausedEvents.Count == 0 => LocalizationService.Instance["Dashboard_NoActiveEvents"],
            0 => LocalizationService.Instance["Dashboard_AllEventsPaused"],
            1 => LocalizationService.Instance["Dashboard_OneActiveEvent"],
            _ => LocalizationService.Instance.Format("Dashboard_ManyActiveEvents", activeEvents.Count)
        };

        SchedulerSummary = pausedEvents.Count > 0
            ? LocalizationService.Instance.Format("Dashboard_PausedCount", pausedEvents.Count)
            : LocalizationService.Instance["Common_Ready"];

        NextScheduledAction = nextEvent is not null
            ? LocalizationService.Instance.Format("Dashboard_NextScheduledAction", nextEvent.DisplayAction, nextEvent.TimeLabel, nextEvent.DateLabel)
            : _powerService.ScheduledAction?.Description ?? LocalizationService.Instance["Common_None"];

        OnPropertyChanged(nameof(ActiveSchedulerEvents));
    }

    private async Task SetPlanAsync(string keyword)
    {
        try
        {
            await _powerService.SetPlanByKindAsync(keyword);
            await RefreshAsync();
        }
        catch
        {
            CurrentPowerPlan = LocalizationService.Instance.Format("PowerModes_CouldNotChangePowerPlan", "").TrimEnd();
        }
    }

    private string CurrentPowerPlanGuid
    {
        get => _currentPowerPlanGuid;
        set
        {
            if (!SetProperty(ref _currentPowerPlanGuid, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsBalancedActive));
            OnPropertyChanged(nameof(IsHighPerformanceActive));
            OnPropertyChanged(nameof(IsPowerSaverActive));
            OnPropertyChanged(nameof(IsUltimateActive));
        }
    }

    private bool IsCurrentPlan(string guid)
    {
        return CurrentPowerPlanGuid.Equals(guid, StringComparison.OrdinalIgnoreCase);
    }
}
