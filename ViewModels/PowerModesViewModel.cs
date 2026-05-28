using System.Collections.ObjectModel;
using System.Windows.Input;
using QuickTools.Helpers;
using QuickTools.Models;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class PowerModesViewModel : ObservableObject
{
    private const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
    private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string PowerSaverGuid = "a1841308-3541-4fab-bc81-f71556f20b4a";
    private const string UltimatePerformanceGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private readonly PowerService _powerService;
    private string _currentPlan = "";
    private string _currentPlanGuid = "";
    private string _message = "";

    public PowerModesViewModel(PowerService powerService)
    {
        _powerService = powerService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SetPlanCommand = new AsyncRelayCommand<string>(SetPlanAsync);
        SetBalancedCommand = new AsyncRelayCommand(async () => await SetByKindAsync("Balanced"));
        SetHighPerformanceCommand = new AsyncRelayCommand(async () => await SetByKindAsync("HighPerformance"));
        SetPowerSaverCommand = new AsyncRelayCommand(async () => await SetByKindAsync("PowerSaver"));
        SetUltimateCommand = new AsyncRelayCommand(async () => await SetByKindAsync("UltimatePerformance"));
        _currentPlan = LocalizationService.Instance["Common_Loading"];
    }

    public ObservableCollection<PowerPlan> Plans { get; } = [];

    public string CurrentPlan
    {
        get => _currentPlan;
        set => SetProperty(ref _currentPlan, value);
    }

    public string CurrentPlanGuid
    {
        get => _currentPlanGuid;
        set
        {
            if (!SetProperty(ref _currentPlanGuid, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsBalancedActive));
            OnPropertyChanged(nameof(IsHighPerformanceActive));
            OnPropertyChanged(nameof(IsPowerSaverActive));
            OnPropertyChanged(nameof(IsUltimateActive));
        }
    }

    public bool IsBalancedActive => IsCurrentPlan(BalancedGuid);
    public bool IsHighPerformanceActive => IsCurrentPlan(HighPerformanceGuid);
    public bool IsPowerSaverActive => IsCurrentPlan(PowerSaverGuid);
    public bool IsUltimateActive => IsCurrentPlan(UltimatePerformanceGuid);

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand SetPlanCommand { get; }
    public ICommand SetBalancedCommand { get; }
    public ICommand SetHighPerformanceCommand { get; }
    public ICommand SetPowerSaverCommand { get; }
    public ICommand SetUltimateCommand { get; }

    public async Task RefreshAsync()
    {
        try
        {
            Plans.Clear();
            foreach (var plan in await _powerService.GetPowerPlansAsync())
            {
                Plans.Add(plan);
            }

            var activePlan = Plans.FirstOrDefault(plan => plan.IsActive);
            CurrentPlan = activePlan?.Name ?? LocalizationService.Instance["Common_Unknown"];
            CurrentPlanGuid = activePlan?.Guid ?? "";
            Message = Plans.Count == 0 ? LocalizationService.Instance["PowerModes_NoPowerPlansFound"] : "";
        }
        catch (Exception ex)
        {
            Message = LocalizationService.Instance.Format("PowerModes_CouldNotReadPowerPlans", ex.Message);
            CurrentPlan = LocalizationService.Instance["Common_Unavailable"];
            CurrentPlanGuid = "";
        }
    }

    private async Task SetPlanAsync(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            return;
        }

        try
        {
            await _powerService.SetPowerPlanAsync(guid);
            Message = LocalizationService.Instance["PowerModes_PowerPlanChanged"];
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Message = LocalizationService.Instance.Format("PowerModes_CouldNotChangePowerPlan", ex.Message);
        }
    }

    private async Task SetByKindAsync(string kind)
    {
        try
        {
            await _powerService.SetPlanByKindAsync(kind);
            Message = LocalizationService.Instance["PowerModes_PowerPlanChanged"];
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Message = kind == "UltimatePerformance"
                ? LocalizationService.Instance["PowerModes_UltimateUnavailable"]
                : LocalizationService.Instance.Format("PowerModes_CouldNotChangePowerPlan", ex.Message);
        }
    }

    private bool IsCurrentPlan(string guid)
    {
        return CurrentPlanGuid.Equals(guid, StringComparison.OrdinalIgnoreCase);
    }
}
