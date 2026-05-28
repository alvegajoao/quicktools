namespace QuickTools.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "en";
    public string AutoClickerHotkey { get; set; } = "F6";
    public string AutoClickerActiveCursor { get; set; } = "Cross";
    public int AutoClickerSpeedPercent { get; set; } = 95;
    public string AutoClickerMouseButton { get; set; } = "Left Click";
    public string AutoClickerClickType { get; set; } = "Single click";
    public bool AutoClickerUseCurrentMousePosition { get; set; } = true;
    public bool QuickToggleEnabled { get; set; }
    public string QuickToggleHotkey { get; set; } = "F7";
    public List<string> QuickToggleWheelIds { get; set; } = [];
    public List<ScheduledPowerEventSetting> ScheduledPowerEvents { get; set; } = [];
    public string DataFolder { get; set; } = "";
    public bool StartWithWindows { get; set; }
}
