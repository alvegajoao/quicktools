namespace QuickTools.Models;

public sealed class NavigationItem
{
    public required string Title { get; init; }
    public required string Icon { get; init; }
    public required object ViewModel { get; init; }
}
