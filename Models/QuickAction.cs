using QuickTools.Helpers;

namespace QuickTools.Models;

public sealed class QuickAction : ObservableObject
{
    private bool _isOnWheel;

    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "";
    public string Description { get; init; } = "";

    public bool IsOnWheel
    {
        get => _isOnWheel;
        set => SetProperty(ref _isOnWheel, value);
    }
}
