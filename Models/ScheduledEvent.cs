using QuickTools.Helpers;
using QuickTools.Services;

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
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                OnPropertyChanged(nameof(ToggleLabel));
            }
        }
    }

    public string DisplayAction => LocalizationService.Instance.TranslatePowerAction(Action);

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
        ? LocalizationService.Instance["PowerScheduler_Today"]
        : ExecuteAt.Date == DateTime.Today.AddDays(1)
            ? LocalizationService.Instance["PowerScheduler_Tomorrow"]
            : ExecuteAt.ToString("dd MMM", LocalizationService.Instance.Culture);

    public string ToggleLabel => LocalizationService.Instance[IsActive ? "PowerScheduler_Pause" : "PowerScheduler_Resume"];

    public ScheduledEvent()
    {
        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(DisplayAction));
            OnPropertyChanged(nameof(DateLabel));
            OnPropertyChanged(nameof(ToggleLabel));
        };
    }
}
