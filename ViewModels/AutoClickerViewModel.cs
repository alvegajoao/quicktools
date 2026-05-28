using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using QuickTools.Helpers;
using QuickTools.Models;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class AutoClickerViewModel : ObservableObject
{
    private readonly AutoClickerService _autoClickerService;
    private int _intervalMilliseconds = 100;
    private int _speedPercent = 95;
    private string _selectedMouseButton = "Left Click";
    private string _selectedClickType = "Single click";
    private string _selectedActiveCursor = "Cross";
    private bool _useCurrentMousePosition = true;
    private string _hotkey = "F6";
    private DateTime? _startedAt;
    private string _elapsedTime = "00:00:00";
    private readonly DispatcherTimer _elapsedTimer;

    public AutoClickerViewModel(AutoClickerService autoClickerService)
    {
        _autoClickerService = autoClickerService;
        _elapsedTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _elapsedTimer.Tick += (_, _) => UpdateElapsedTime();

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        SelectMouseButtonCommand = new RelayCommand(parameter =>
        {
            if (parameter is string mouseButton)
            {
                SelectedMouseButton = mouseButton;
            }
        });

        _autoClickerService.StateChanged += (_, _) =>
        {
            if (_autoClickerService.IsRunning)
            {
                _startedAt = DateTime.Now;
                _elapsedTimer.Start();
                UpdateElapsedTime();
            }
            else
            {
                _elapsedTimer.Stop();
                _startedAt = null;
                ElapsedTime = "00:00:00";
            }

            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsRunning));
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        };

        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IntervalLabel));
            OnPropertyChanged(nameof(ElapsedTimeLabel));
            OnPropertyChanged(nameof(HotkeyLabel));
        };
    }

    public ObservableCollection<LocalizedOption> ClickTypeOptions { get; } =
    [
        new() { Value = "Single click", TextKey = "AutoClicker_SingleClick" },
        new() { Value = "Double click", TextKey = "AutoClicker_DoubleClick" }
    ];

    public ObservableCollection<CursorOption> ActiveCursorOptions { get; } =
    [
        new() { NameKey = "Cursor_Cross", CursorKey = "Cross" },
        new() { NameKey = "Cursor_Hand", CursorKey = "Hand" },
        new() { NameKey = "Cursor_Pen", CursorKey = "Pen" },
        new() { NameKey = "Cursor_ScrollAll", CursorKey = "ScrollAll" },
        new() { NameKey = "Cursor_SizeAll", CursorKey = "SizeAll" },
        new() { NameKey = "Cursor_Wait", CursorKey = "Wait" },
        new() { NameKey = "Cursor_Help", CursorKey = "Help" },
        new() { NameKey = "Cursor_Arrow", CursorKey = "Arrow" }
    ];

    public int IntervalMilliseconds
    {
        get => _intervalMilliseconds;
        set
        {
            if (SetProperty(ref _intervalMilliseconds, Math.Max(10, value)))
            {
                OnPropertyChanged(nameof(IntervalLabel));
            }
        }
    }

    public int SpeedPercent
    {
        get => _speedPercent;
        set
        {
            var speed = Math.Clamp(value, 0, 100);
            if (!SetProperty(ref _speedPercent, speed))
            {
                return;
            }

            IntervalMilliseconds = 10 + (int)Math.Round((100 - speed) * 19.9);
            OnPropertyChanged(nameof(SpeedLabel));
        }
    }

    public string SpeedLabel => $"{SpeedPercent}%";
    public string IntervalLabel => LocalizationService.Instance.Format("AutoClicker_Interval", IntervalMilliseconds);
    public string ElapsedTimeLabel => LocalizationService.Instance.Format("AutoClicker_ActiveTime", ElapsedTime);
    public string HotkeyLabel => LocalizationService.Instance.Format("Common_Hotkey", Hotkey);

    public string SelectedMouseButton
    {
        get => _selectedMouseButton;
        set
        {
            if (!SetProperty(ref _selectedMouseButton, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsLeftClickSelected));
            OnPropertyChanged(nameof(IsRightClickSelected));
            OnPropertyChanged(nameof(IsMiddleClickSelected));
        }
    }

    public bool IsLeftClickSelected => SelectedMouseButton == "Left Click";
    public bool IsRightClickSelected => SelectedMouseButton == "Right Click";
    public bool IsMiddleClickSelected => SelectedMouseButton == "Middle Click";

    public string SelectedClickType
    {
        get => _selectedClickType;
        set => SetProperty(ref _selectedClickType, value);
    }

    public string SelectedActiveCursor
    {
        get => _selectedActiveCursor;
        set => SetProperty(ref _selectedActiveCursor, value);
    }

    public bool UseCurrentMousePosition
    {
        get => _useCurrentMousePosition;
        set => SetProperty(ref _useCurrentMousePosition, value);
    }

    public string Hotkey
    {
        get => _hotkey;
        set => SetProperty(ref _hotkey, value);
    }

    public bool IsRunning => _autoClickerService.IsRunning;
    public string Status => LocalizationService.Instance[IsRunning ? "AutoClicker_Running" : "AutoClicker_Stopped"];

    public string ElapsedTime
    {
        get => _elapsedTime;
        private set => SetProperty(ref _elapsedTime, value);
    }

    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public ICommand SelectMouseButtonCommand { get; }

    public void Start()
    {
        _autoClickerService.Start(IntervalMilliseconds, SelectedMouseButton, SelectedClickType);
    }

    public void Stop()
    {
        _autoClickerService.Stop();
    }

    public void Toggle()
    {
        _autoClickerService.Toggle(IntervalMilliseconds, SelectedMouseButton, SelectedClickType);
    }

    private void UpdateElapsedTime()
    {
        if (_startedAt is null)
        {
            ElapsedTime = "00:00:00";
            return;
        }

        ElapsedTime = (DateTime.Now - _startedAt.Value).ToString(@"hh\:mm\:ss");
    }
}
