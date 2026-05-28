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
    }

    public ObservableCollection<string> ClickTypes { get; } = ["Single click", "Double click"];

    public ObservableCollection<CursorOption> ActiveCursorOptions { get; } =
    [
        new() { Name = "Crosshair", CursorKey = "Cross", Symbol = "+", IconData = "M12,2 L12,22 M2,12 L22,12" },
        new() { Name = "Hand", CursorKey = "Hand", Symbol = "\u261D", IconData = "M9,21 L8,13 C8,12 9,11 10,12 L10,4 C10,3 11,2 12,2 C13,2 14,3 14,4 L14,11 L15,9 C16,8 18,9 18,11 L18,12 L19,11 C20,10 22,11 22,13 L22,17 C22,20 20,22 17,22 L12,22 C11,22 10,22 9,21 Z" },
        new() { Name = "Precision", CursorKey = "Pen", Symbol = "\u270E", IconData = "M4,20 L8,19 L19,8 C20,7 20,5 19,4 C18,3 16,3 15,4 L4,15 Z M14,5 L18,9" },
        new() { Name = "Target", CursorKey = "ScrollAll", Symbol = "\u2725", IconData = "M12,2 L15,6 L13,6 L13,11 L18,11 L18,9 L22,12 L18,15 L18,13 L13,13 L13,18 L15,18 L12,22 L9,18 L11,18 L11,13 L6,13 L6,15 L2,12 L6,9 L6,11 L11,11 L11,6 L9,6 Z" },
        new() { Name = "Move", CursorKey = "SizeAll", Symbol = "\u2194", IconData = "M3,12 L7,8 L7,11 L17,11 L17,8 L21,12 L17,16 L17,13 L7,13 L7,16 Z" },
        new() { Name = "Wait", CursorKey = "Wait", Symbol = "\u25CC", IconData = "M7,3 L17,3 L17,7 C17,9 15,11 13,12 C15,13 17,15 17,17 L17,21 L7,21 L7,17 C7,15 9,13 11,12 C9,11 7,9 7,7 Z M9,5 L15,5 M9,19 L15,19" },
        new() { Name = "Help", CursorKey = "Help", Symbol = "?", IconData = "M8,8 C8,5 10,3 13,3 C16,3 18,5 18,8 C18,11 15,12 13,14 L13,16 M13,20 L13,21" },
        new() { Name = "Arrow", CursorKey = "Arrow", Symbol = "\u27A4", IconData = "M5,3 L19,13 L13,14 L16,21 L13,22 L10,15 L5,19 Z" }
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
    public string IntervalLabel => $"{IntervalMilliseconds} ms between clicks";

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
    public string Status => IsRunning ? "Running" : "Stopped";

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
