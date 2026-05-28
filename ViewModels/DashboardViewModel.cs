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

    private string _currentPowerPlan = "Loading...";
    private string _currentPowerPlanGuid = "";
    private string _nextScheduledAction = "None";
    private string _schedulerStatus = "No active events";
    private string _schedulerSummary = "Nothing scheduled";
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
                OnPropertyChanged(nameof(AutoClickerSummary));
                OnPropertyChanged(nameof(AutoClickerSpeedLabel));
            }
        };

        _quickToggleViewModel.PropertyChanged += OnQuickTogglePropertyChanged;
        _powerService.ScheduledActionChanged += (_, _) => RefreshSchedulerStatus();
        _powerService.EventsChanged += (_, _) => RefreshSchedulerStatus();
        RefreshSchedulerStatus();
    }

    public string AutoClickerStatus => _autoClickerService.IsRunning ? "Running" : "Stopped";
    public bool IsAutoClickerRunning => _autoClickerService.IsRunning;
    public string AutoClickerElapsedTime => _autoClickerViewModel.ElapsedTime;
    public string AutoClickerHotkey => _autoClickerViewModel.Hotkey;
    public string AutoClickerSummary => _autoClickerService.IsRunning
        ? $"{_autoClickerViewModel.ElapsedTime} active"
        : $"{_autoClickerViewModel.SpeedPercent}% speed";
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

    public string QuickToggleStatus => IsQuickToggleActive ? "Enabled" : "Disabled";
    public string QuickToggleSummary => IsQuickToggleActive
        ? $"{_quickToggleViewModel.WheelCount}/8 actions on {_quickToggleViewModel.Hotkey}"
        : $"Off - {_quickToggleViewModel.WheelCount}/8 actions set";
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

    public string PowerPlanSummary => CurrentPowerPlan == "Unavailable"
        ? "Could not read powercfg"
        : "Current Windows mode";

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
            CurrentPowerPlan = activePlan?.Name ?? "Unknown";
            CurrentPowerPlanGuid = activePlan?.Guid ?? "";
        }
        catch
        {
            CurrentPowerPlan = "Unavailable";
            CurrentPowerPlanGuid = "";
        }
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
            0 when pausedEvents.Count == 0 => "No active events",
            0 => "All events paused",
            1 => "1 active event",
            _ => $"{activeEvents.Count} active events"
        };

        SchedulerSummary = pausedEvents.Count > 0
            ? $"{pausedEvents.Count} paused"
            : "Ready";

        NextScheduledAction = nextEvent is not null
            ? $"{nextEvent.Action} at {nextEvent.TimeLabel} ({nextEvent.DateLabel})"
            : _powerService.ScheduledAction?.Description ?? "None";

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
            CurrentPowerPlan = "Could not change plan";
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
