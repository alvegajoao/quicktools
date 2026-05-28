using QuickTools.Helpers;
using QuickTools.Services;

namespace QuickTools.Models;

public sealed class PowerPlan : ObservableObject
{
    public string Guid { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public string Status => LocalizationService.Instance[IsActive ? "Common_Active" : "Common_Available"];

    public string Icon => Guid.ToLowerInvariant() switch
    {
        "381b4222-f694-41f0-9685-ff5bb260df2e" => "\uE9D2",
        "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" => "\uE945",
        "a1841308-3541-4fab-bc81-f71556f20b4a" => "\uE83F",
        "e9a42b02-d5df-448d-aa00-03f14749eb61" => "\uEC4A",
        _ => "\uE7E8"
    };

    public string Description => Guid.ToLowerInvariant() switch
    {
        "381b4222-f694-41f0-9685-ff5bb260df2e" => LocalizationService.Instance["PowerPlan_Balanced_Description"],
        "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" => LocalizationService.Instance["PowerPlan_HighPerformance_Description"],
        "a1841308-3541-4fab-bc81-f71556f20b4a" => LocalizationService.Instance["PowerPlan_PowerSaver_Description"],
        "e9a42b02-d5df-448d-aa00-03f14749eb61" => LocalizationService.Instance["PowerPlan_UltimatePerformance_Description"],
        _ => LocalizationService.Instance["PowerPlan_Custom_Description"]
    };

    public PowerPlan()
    {
        LocalizationService.Instance.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Description));
        };
    }
}
