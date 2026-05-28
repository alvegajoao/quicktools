using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using QuickTools.Helpers;
using QuickTools.Models;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class PowerSchedulerViewModel : ObservableObject
{
    private readonly PowerService _powerService;
    private string _selectedAction = "Shutdown";
    private string _selectedHour = DateTime.Now.AddHours(1).Hour.ToString("00");
    private string _selectedMinute = "00";
    private string _message = "";

    public PowerSchedulerViewModel(PowerService powerService)
    {
        _powerService = powerService;
        _powerService.EventsChanged += (_, _) => System.Windows.Application.Current.Dispatcher.Invoke(SyncEvents);

        AddEventCommand = new RelayCommand(_ => AddEvent());
        RemoveEventCommand = new RelayCommand(parameter => RemoveEvent(parameter as ScheduledEvent));
        ToggleEventCommand = new RelayCommand(parameter => ToggleEvent(parameter as ScheduledEvent));
        PauseAllCommand = new RelayCommand(_ => PauseAll());
    }

    public ObservableCollection<ScheduledEvent> Events { get; } = [];
    public ObservableCollection<string> Actions { get; } = ["Shutdown", "Restart", "Suspend", "Hibernate"];
    public static IReadOnlyList<string> Hours { get; } = Enumerable.Range(0, 24).Select(hour => hour.ToString("00")).ToList();
    public static IReadOnlyList<string> Minutes { get; } = Enumerable.Range(0, 12).Select(step => (step * 5).ToString("00")).ToList();

    public string OverallStatus
    {
        get
        {
            var activeCount = Events.Count(item => item.IsActive);
            return activeCount switch
            {
                0 => "No active events",
                1 => "1 active event",
                _ => $"{activeCount} active events"
            };
        }
    }

    public string NextEventLabel
    {
        get
        {
            var next = Events.Where(item => item.IsActive).OrderBy(item => item.ExecuteAt).FirstOrDefault();
            return next is null
                ? "No upcoming events"
                : $"Next: {next.Action} at {next.TimeLabel} ({next.DateLabel})";
        }
    }

    public string EventsCountLabel
    {
        get
        {
            var activeCount = Events.Count(item => item.IsActive);
            var pausedCount = Events.Count(item => !item.IsActive);
            return pausedCount > 0
                ? $"{activeCount} active / {pausedCount} paused"
                : $"{Events.Count} events";
        }
    }

    public Visibility EmptyVisibility => Events.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public string SelectedAction
    {
        get => _selectedAction;
        set => SetProperty(ref _selectedAction, value);
    }

    public string SelectedHour
    {
        get => _selectedHour;
        set => SetProperty(ref _selectedHour, value);
    }

    public string SelectedMinute
    {
        get => _selectedMinute;
        set => SetProperty(ref _selectedMinute, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public ICommand AddEventCommand { get; }
    public ICommand RemoveEventCommand { get; }
    public ICommand ToggleEventCommand { get; }
    public ICommand PauseAllCommand { get; }

    private void AddEvent()
    {
        var executeAt = ParseExecuteAt();
        if (executeAt is null)
        {
            Message = "Choose a valid time.";
            return;
        }

        _powerService.AddEvent(SelectedAction, executeAt.Value);
        Message = $"{SelectedAction} scheduled for {executeAt.Value:HH:mm} ({GetFriendlyDate(executeAt.Value)}).";
    }

    private void RemoveEvent(ScheduledEvent? scheduledEvent)
    {
        if (scheduledEvent is null)
        {
            return;
        }

        _powerService.RemoveEvent(scheduledEvent.Id);
        Message = "Event removed.";
    }

    private void ToggleEvent(ScheduledEvent? scheduledEvent)
    {
        if (scheduledEvent is null)
        {
            return;
        }

        if (scheduledEvent.IsActive)
        {
            _powerService.PauseEvent(scheduledEvent.Id);
            Message = "Event paused.";
            return;
        }

        if (scheduledEvent.ExecuteAt <= DateTime.Now)
        {
            Message = "Cannot resume because this event time has already passed.";
            return;
        }

        _powerService.ResumeEvent(scheduledEvent.Id);
        Message = "Event resumed.";
    }

    private void PauseAll()
    {
        if (Events.All(item => !item.IsActive))
        {
            return;
        }

        _powerService.PauseAllEvents();
        Message = "All events paused.";
    }

    private void SyncEvents()
    {
        var current = _powerService.ScheduledEvents;

        foreach (var scheduledEvent in current.Where(item => Events.All(existing => existing.Id != item.Id)))
        {
            Events.Add(scheduledEvent);
        }

        for (var index = Events.Count - 1; index >= 0; index--)
        {
            if (current.All(item => item.Id != Events[index].Id))
            {
                Events.RemoveAt(index);
            }
        }

        OnPropertyChanged(nameof(OverallStatus));
        OnPropertyChanged(nameof(NextEventLabel));
        OnPropertyChanged(nameof(EventsCountLabel));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    private DateTime? ParseExecuteAt()
    {
        if (!int.TryParse(SelectedHour, out var hour) ||
            !int.TryParse(SelectedMinute, out var minute))
        {
            return null;
        }

        var executeAt = DateTime.Today.AddHours(hour).AddMinutes(minute);
        if (executeAt <= DateTime.Now)
        {
            executeAt = executeAt.AddDays(1);
        }

        return executeAt;
    }

    private static string GetFriendlyDate(DateTime executeAt)
    {
        if (executeAt.Date == DateTime.Today)
        {
            return "today";
        }

        if (executeAt.Date == DateTime.Today.AddDays(1))
        {
            return "tomorrow";
        }

        return executeAt.ToString("dd MMM");
    }
}
