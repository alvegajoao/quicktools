using System.Windows.Input;
using QuickTools.Helpers;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly AutoClickerService _autoClickerService;
    private readonly PowerService _powerService;
    private readonly AutoClickerViewModel _autoClickerViewModel;

    private string _currentPowerPlan = "Loading...";
    private string _nextScheduledAction = "None";
    private bool _isQuickToggleActive;

    public DashboardViewModel(
        AutoClickerService autoClickerService,
        PowerService powerService,
        AutoClickerViewModel autoClickerViewModel)
    {
        _autoClickerService = autoClickerService;
        _powerService = powerService;
        _autoClickerViewModel = autoClickerViewModel;

        StartAutoClickerCommand = _autoClickerViewModel.StartCommand;
        StopAutoClickerCommand = _autoClickerViewModel.StopCommand;
        CancelShutdownCommand = new RelayCommand(() => _powerService.PauseAllEvents());
        SetBalancedCommand = new AsyncRelayCommand(async () => await SetPlanAsync("Balanced"));
        SetHighPerformanceCommand = new AsyncRelayCommand(async () => await SetPlanAsync("HighPerformance"));

        _autoClickerService.StateChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AutoClickerStatus));
            OnPropertyChanged(nameof(IsAutoClickerRunning));
        };
        _autoClickerViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AutoClickerViewModel.ElapsedTime))
            {
                OnPropertyChanged(nameof(AutoClickerElapsedTime));
            }
        };
        _powerService.ScheduledActionChanged += (_, _) => RefreshNextScheduledAction();
        _powerService.EventsChanged += (_, _) => RefreshNextScheduledAction();
    }

    public string AutoClickerStatus => _autoClickerService.IsRunning ? "Running" : "Stopped";
    public bool IsAutoClickerRunning => _autoClickerService.IsRunning;
    public string AutoClickerElapsedTime => _autoClickerViewModel.ElapsedTime;
    public string AutoClickerHotkey => _autoClickerViewModel.Hotkey;

    public bool IsQuickToggleActive
    {
        get => _isQuickToggleActive;
        set
        {
            if (SetProperty(ref _isQuickToggleActive, value))
            {
                OnPropertyChanged(nameof(QuickToggleStatus));
            }
        }
    }

    public string QuickToggleStatus => IsQuickToggleActive ? "Enabled" : "Disabled";

    public string CurrentPowerPlan
    {
        get => _currentPowerPlan;
        set => SetProperty(ref _currentPowerPlan, value);
    }

    public string NextScheduledAction
    {
        get => _nextScheduledAction;
        set => SetProperty(ref _nextScheduledAction, value);
    }

    public ICommand StartAutoClickerCommand { get; }
    public ICommand StopAutoClickerCommand { get; }
    public ICommand CancelShutdownCommand { get; }
    public ICommand SetBalancedCommand { get; }
    public ICommand SetHighPerformanceCommand { get; }

    public async Task RefreshAsync()
    {
        try
        {
            var activePlan = (await _powerService.GetPowerPlansAsync()).FirstOrDefault(plan => plan.IsActive);
            CurrentPowerPlan = activePlan?.Name ?? "Unknown";
        }
        catch
        {
            CurrentPowerPlan = "Unavailable";
        }
    }

    private void RefreshNextScheduledAction()
    {
        var nextEvent = _powerService.ScheduledEvents
            .Where(item => item.IsActive)
            .OrderBy(item => item.ExecuteAt)
            .FirstOrDefault();

        NextScheduledAction = nextEvent is not null
            ? $"{nextEvent.Action} at {nextEvent.TimeLabel}"
            : _powerService.ScheduledAction?.Description ?? "None";
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
}
