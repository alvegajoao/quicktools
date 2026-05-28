using QuickTools.Helpers;
using QuickTools.Services;

namespace QuickTools.Models;

public sealed class CursorOption : ObservableObject
{
    public required string NameKey { get; init; }
    public required string CursorKey { get; init; }
    public required string Symbol { get; init; }
    public required string IconData { get; init; }

    public string Name => LocalizationService.Instance[NameKey];

    public CursorOption()
    {
        LocalizationService.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Name));
    }
}
