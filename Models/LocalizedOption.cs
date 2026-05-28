using QuickTools.Helpers;
using QuickTools.Services;

namespace QuickTools.Models;

public sealed class LocalizedOption : ObservableObject
{
    public required string Value { get; init; }
    public required string TextKey { get; init; }

    public string DisplayName => LocalizationService.Instance[TextKey];

    public LocalizedOption()
    {
        LocalizationService.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(DisplayName));
    }
}
