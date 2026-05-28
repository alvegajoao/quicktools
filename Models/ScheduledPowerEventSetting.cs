namespace QuickTools.Models;

public sealed class ScheduledPowerEventSetting
{
    public string Action { get; set; } = "Shutdown";
    public DateTime ExecuteAt { get; set; }
    public bool IsActive { get; set; } = true;
}
