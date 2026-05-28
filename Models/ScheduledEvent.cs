using QuickTools.Helpers;

namespace QuickTools.Models;

public sealed class ScheduledEvent : ObservableObject
{
    private bool _isActive = true;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Action { get; init; } = "Shutdown";
    public DateTime ExecuteAt { get; init; }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string Icon => Action switch
    {
        "Shutdown" => "\uE7E8",
        "Restart" => "\uE777",
        "Suspend" => "\uE708",
        "Hibernate" => "\uE823",
        _ => "\uE823"
    };

    public string TimeLabel => ExecuteAt.ToString("HH:mm");

    public string DateLabel => ExecuteAt.Date == DateTime.Today
        ? "Today"
        : ExecuteAt.Date == DateTime.Today.AddDays(1)
            ? "Tomorrow"
            : ExecuteAt.ToString("dd MMM");
}
