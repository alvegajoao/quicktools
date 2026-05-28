using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using QuickTools.Models;

namespace QuickTools.Services;

public sealed class SettingsService
{
    private const string AppName = "QuickTools";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string AppDataFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

    public string SettingsPath => Path.Combine(AppDataFolder, "settings.json");

    public AppSettings Load()
    {
        Directory.CreateDirectory(AppDataFolder);

        if (!File.Exists(SettingsPath))
        {
            var defaults = new AppSettings { DataFolder = AppDataFolder };
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            settings.DataFolder = string.IsNullOrWhiteSpace(settings.DataFolder) ? AppDataFolder : settings.DataFolder;
            settings.StartWithWindows = IsStartWithWindowsEnabled();
            return settings;
        }
        catch
        {
            return new AppSettings { DataFolder = AppDataFolder, StartWithWindows = IsStartWithWindowsEnabled() };
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataFolder);
        settings.DataFolder = string.IsNullOrWhiteSpace(settings.DataFolder) ? AppDataFolder : settings.DataFolder;
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, _jsonOptions));
    }

    public string ExportSettings(AppSettings settings)
    {
        return JsonSerializer.Serialize(settings, _jsonOptions);
    }

    public AppSettings ImportSettings(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        Save(settings);
        SetStartWithWindows(settings.StartWithWindows);
        return settings;
    }

    public bool IsStartWithWindowsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(AppName) is string;
    }

    public void SetStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                key.SetValue(AppName, $"\"{executablePath}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
