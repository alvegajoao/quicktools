using QuickTools.Helpers;
using QuickTools.Services;
using System.Windows.Media;

namespace QuickTools.Models;

public sealed class CursorOption : ObservableObject
{
    public required string NameKey { get; init; }
    public required string CursorKey { get; init; }

    public string Name => LocalizationService.Instance[NameKey];
    public ImageSource? Preview => CursorPreviewService.GetPreview(CursorKey);

    public CursorOption()
    {
        LocalizationService.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(Name));
    }
}
