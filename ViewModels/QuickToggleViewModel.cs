using System.Collections.ObjectModel;
using QuickTools.Helpers;
using QuickTools.Models;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class QuickToggleViewModel : ObservableObject
{
    private bool _isEnabled;
    private string _hotkey = "F7";
    private string _globalStatus = "Wheel is disabled";

    public QuickToggleViewModel(QuickActionService quickActionService)
    {
        QuickActionService = quickActionService;
        AllActions = new ObservableCollection<QuickAction>(QuickActionService.AllActions);

        EnableCommand = new RelayCommand(() => IsEnabled = true, () => !IsEnabled);
        DisableCommand = new RelayCommand(() => IsEnabled = false, () => IsEnabled);

        ToggleWheelCommand = new RelayCommand(param =>
        {
            if (param is not QuickAction action)
            {
                return;
            }

            var onWheel = AllActions.Count(item => item.IsOnWheel);
            if (!action.IsOnWheel && onWheel >= 8)
            {
                return;
            }

            action.IsOnWheel = !action.IsOnWheel;
            OnPropertyChanged(nameof(WheelCount));
            OnPropertyChanged(nameof(WheelCountLabel));
        });
    }

    public QuickActionService QuickActionService { get; }
    public ObservableCollection<QuickAction> AllActions { get; }

    public IEnumerable<QuickAction> WheelActions => AllActions.Where(action => action.IsOnWheel).Take(8);

    public int WheelCount => AllActions.Count(action => action.IsOnWheel);
    public string WheelCountLabel => $"{WheelCount}/8 on wheel";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (!SetProperty(ref _isEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(Status));
            EnableCommand.RaiseCanExecuteChanged();
            DisableCommand.RaiseCanExecuteChanged();
        }
    }

    public string Status => IsEnabled ? "Enabled" : "Disabled";

    public string Hotkey
    {
        get => _hotkey;
        set
        {
            if (SetProperty(ref _hotkey, value))
            {
                OnPropertyChanged(nameof(GlobalStatus));
            }
        }
    }

    public string GlobalStatus
    {
        get => _globalStatus;
        set => SetProperty(ref _globalStatus, value);
    }

    public RelayCommand EnableCommand { get; }
    public RelayCommand DisableCommand { get; }
    public RelayCommand ToggleWheelCommand { get; }

    public void LoadWheelIds(IEnumerable<string> ids)
    {
        var set = ids.ToHashSet();
        foreach (var action in AllActions)
        {
            action.IsOnWheel = set.Contains(action.Id);
        }

        OnPropertyChanged(nameof(WheelCount));
        OnPropertyChanged(nameof(WheelCountLabel));
    }

    public IEnumerable<string> GetWheelIds() => AllActions.Where(action => action.IsOnWheel).Select(action => action.Id);
}
