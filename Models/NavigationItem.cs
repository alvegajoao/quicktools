using QuickTools.Helpers;
using QuickTools.Services;

namespace QuickTools.Models;

public sealed class NavigationItem : ObservableObject
{
    public required string TitleKey { get; init; }
    public required string Icon { get; init; }
    public required object ViewModel { get; init; }

    public string Title => LocalizationService.Instance[TitleKey];

    public NavigationItem()
    {
        LocalizationService.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Title));
    }
}
