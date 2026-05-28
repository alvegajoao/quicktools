using QuickTools.Helpers;
using QuickTools.Services;

namespace QuickTools.Models;

public sealed class QuickAction : ObservableObject
{
    private bool _isOnWheel;

    public string Id { get; init; } = "";
    public string Icon { get; init; } = "";

    public string Name => LocalizationService.Instance[$"QuickAction_{Id}_Name"];
    public string Description => LocalizationService.Instance[$"QuickAction_{Id}_Description"];
    public string WheelButtonLabel => LocalizationService.Instance[IsOnWheel ? "QuickToggle_OnWheel" : "QuickToggle_AddToWheel"];

    public bool IsOnWheel
    {
        get => _isOnWheel;
        set
        {
            if (SetProperty(ref _isOnWheel, value))
            {
                OnPropertyChanged(nameof(WheelButtonLabel));
            }
        }
    }

    public QuickAction()
    {
        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(WheelButtonLabel));
        };
    }
}
