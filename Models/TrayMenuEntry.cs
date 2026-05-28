namespace QuickTools.Models;

public enum TrayMenuEntryKind { Item, Separator }

public sealed class TrayMenuEntry
{
    public TrayMenuEntryKind Kind  { get; init; } = TrayMenuEntryKind.Item;
    public string            Icon  { get; init; } = "";
    public string            Label { get; init; } = "";
    public Action?           Action { get; init; }
    public bool              IsDanger { get; init; }

    public static TrayMenuEntry Sep() => new() { Kind = TrayMenuEntryKind.Separator };
}
