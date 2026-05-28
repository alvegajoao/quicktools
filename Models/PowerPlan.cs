namespace QuickTools.Models;

public sealed class PowerPlan
{
    public string Guid { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public string Status => IsActive ? "Active" : "Available";
}
