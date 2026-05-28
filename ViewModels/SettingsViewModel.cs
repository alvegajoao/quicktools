using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using QuickTools.Helpers;
using QuickTools.Models;
using QuickTools.Services;

namespace QuickTools.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private string _message = "";

    public SettingsViewModel(SettingsService settingsService, AppSettings settings)
    {
        _settingsService = settingsService;
        _settings = settings;

        SaveCommand = new RelayCommand(Save);
        ExportCommand = new RelayCommand(Export);
        ImportCommand = new RelayCommand(Import);
    }

    public ObservableCollection<LocalizedOption> ThemeOptions { get; } =
    [
        new() { Value = "Light", TextKey = "Theme_Light" },
        new() { Value = "Dark", TextKey = "Theme_Dark" },
        new() { Value = "System", TextKey = "Theme_System" }
    ];

    public ObservableCollection<LocalizedOption> LanguageOptions { get; } =
    [
        new() { Value = "en", TextKey = "Language_en" },
        new() { Value = "pt", TextKey = "Language_pt" }
    ];

    public ObservableCollection<string> Hotkeys { get; } = ["F6", "F7", "F8", "F9", "F10", "F11", "F12"];

    public string SelectedTheme
    {
        get => _settings.Theme;
        set
        {
            if (_settings.Theme == value)
            {
                return;
            }

            _settings.Theme = value;
            OnPropertyChanged();
        }
    }

    public bool StartWithWindows
    {
        get => _settings.StartWithWindows;
        set
        {
            if (_settings.StartWithWindows == value)
            {
                return;
            }

            _settings.StartWithWindows = value;
            OnPropertyChanged();
        }
    }

    public string AutoClickerHotkey
    {
        get => _settings.AutoClickerHotkey;
        set
        {
            if (_settings.AutoClickerHotkey == value) return;
            _settings.AutoClickerHotkey = value;
            OnPropertyChanged();
        }
    }

    public string QuickToggleHotkey
    {
        get => _settings.QuickToggleHotkey;
        set
        {
            if (_settings.QuickToggleHotkey == value) return;
            _settings.QuickToggleHotkey = value;
            OnPropertyChanged();
        }
    }

    public string SelectedLanguage
    {
        get => LocalizationService.NormalizeLanguage(_settings.Language);
        set
        {
            var normalized = LocalizationService.NormalizeLanguage(value);
            if (_settings.Language == normalized)
            {
                return;
            }

            _settings.Language = normalized;
            OnPropertyChanged();
        }
    }

    public string AutoClickerActiveCursor
    {
        get => _settings.AutoClickerActiveCursor;
        set
        {
            if (_settings.AutoClickerActiveCursor == value)
            {
                return;
            }

            _settings.AutoClickerActiveCursor = value;
            OnPropertyChanged();
        }
    }

    public string DataFolder
    {
        get => _settings.DataFolder;
        set
        {
            if (_settings.DataFolder == value)
            {
                return;
            }

            _settings.DataFolder = value;
            OnPropertyChanged();
        }
    }

    public string SettingsPath => _settingsService.SettingsPath;

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }

    public event EventHandler? SettingsSaved;

    private void Save()
    {
        try
        {
            _settingsService.SetStartWithWindows(StartWithWindows);
            _settingsService.Save(_settings);
            LocalizationService.Instance.SetLanguage(_settings.Language);
            SettingsSaved?.Invoke(this, EventArgs.Empty);
            Message = LocalizationService.Instance["Settings_SettingsSaved"];
        }
        catch (Exception ex)
        {
            Message = LocalizationService.Instance.Format("Settings_CouldNotSave", ex.Message);
        }
    }

    private void Export()
    {
        System.Windows.Clipboard.SetText(_settingsService.ExportSettings(_settings));
        Message = LocalizationService.Instance["Settings_ExportCopied"];
    }

    private void Import()
    {
        try
        {
            if (!System.Windows.Clipboard.ContainsText())
            {
                Message = LocalizationService.Instance["Settings_ClipboardNoJson"];
                return;
            }

            var imported = _settingsService.ImportSettings(System.Windows.Clipboard.GetText());
            SelectedTheme = imported.Theme;
            SelectedLanguage = imported.Language;
            StartWithWindows = imported.StartWithWindows;
            AutoClickerHotkey  = imported.AutoClickerHotkey;
            QuickToggleHotkey  = imported.QuickToggleHotkey;
            DataFolder = string.IsNullOrWhiteSpace(imported.DataFolder)
                ? Path.GetDirectoryName(_settingsService.SettingsPath) ?? ""
                : imported.DataFolder;
            LocalizationService.Instance.SetLanguage(imported.Language);
            SettingsSaved?.Invoke(this, EventArgs.Empty);
            Message = LocalizationService.Instance["Settings_Imported"];
        }
        catch (Exception ex)
        {
            Message = LocalizationService.Instance.Format("Settings_CouldNotImport", ex.Message);
        }
    }
}
