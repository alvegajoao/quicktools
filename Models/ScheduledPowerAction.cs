namespace QuickTools.Models;

public sealed class ScheduledPowerAction
{
    public string Action { get; set; } = "Shutdown";
    public DateTime ExecuteAt { get; set; }
    public string Description => $"{Action} at {ExecuteAt:g}";
}
