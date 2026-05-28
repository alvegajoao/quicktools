namespace QuickTools.Models;

public static class DashboardCard
{
    public const string AutoClicker = "auto_clicker";
    public const string QuickToggle = "quick_toggle";
    public const string PowerMode = "power_mode";
    public const string PowerScheduler = "power_scheduler";
    public const string SystemPerformance = "system_performance";

    public static readonly IReadOnlyList<string> DefaultOrder =
    [
        AutoClicker, QuickToggle, PowerMode, PowerScheduler, SystemPerformance
    ];

    /// <summary>Cards that always span the full row width (ColSpan=3).</summary>
    public static bool IsFullWidth(string cardId) => cardId == SystemPerformance;
}
