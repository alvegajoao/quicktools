# QuickTools

[![Build](https://github.com/alvegajoao/quicktools/actions/workflows/build.yml/badge.svg)](https://github.com/alvegajoao/quicktools/actions/workflows/build.yml)

QuickTools is a lightweight Windows desktop app built with C#/.NET 8 and WPF. It brings together practical system tools in a clean Windows 11 inspired interface.

## Features

- Dashboard with useful status cards and quick actions
- Auto Clicker with Start/Stop, native `SendInput`, visible running cursor and a global F-key hotkey
- Quick Toggle radial wheel for system actions such as mute, volume, lock, screenshot and settings
- Power Scheduler with multiple scheduled events, pause/resume per event and pause-all support
- Power Modes using Windows `powercfg`
- Settings for theme, startup, hotkeys and JSON import/export

## Screens

- Dashboard
- Auto Clicker
- Quick Toggle
- Power Scheduler
- Power Modes
- Settings

## Requirements

- Windows
- .NET 8 SDK for development
- .NET 8 Desktop Runtime for running published builds

## Run Locally

```powershell
dotnet restore
dotnet run
```

Build:

```powershell
dotnet build QuickTools.sln
```

Release build:

```powershell
dotnet build QuickTools.sln --configuration Release
```

## Project Structure

```text
QuickTools/
|-- .github/workflows/
|-- Models/
|-- Services/
|-- ViewModels/
|-- Views/
|-- Helpers/
|-- Converters/
|-- scripts/
|-- App.xaml
|-- MainWindow.xaml
|-- QuickPickerWindow.xaml
|-- QuickTools.csproj
`-- QuickTools.sln
```

## Data Files

QuickTools stores local settings in:

```text
%AppData%\QuickTools\settings.json
```

The folder is created automatically.

## Implementation Notes

### Auto Clicker

- Uses native Windows `SendInput`
- Runs on a background task with cancellation support
- Defaults to `F6` as the global Start/Stop hotkey
- Always exposes visible Start and Stop controls
- Applies a visible Windows cursor while running and restores it when stopped

### Quick Toggle

- Defaults to `F7`, configurable in Settings
- Opens a compact radial wheel at the mouse position
- Supports up to 8 pinned actions
- Uses native Windows commands or shell actions where possible
- Some actions, such as Wi-Fi changes, can require Windows elevation

### Power Scheduler

- Supports Shutdown, Restart, Suspend and Hibernate
- Uses a DatePicker plus hour/minute selectors for easier scheduling
- Supports multiple scheduled events
- Each event can be paused, resumed or removed individually
- Shutdown and Restart ask for confirmation before scheduling

### Power Modes

- Reads plans with `powercfg /L`
- Changes plans with `powercfg /S GUID`
- Shows all detected plans and highlights the active one
- Ultimate Performance is shown as unavailable when Windows does not expose it

## Limitations

- Some elevated/admin apps may not accept input from a non-elevated QuickTools process.
- Some power plan names can be localized by Windows, so direct plan selection is available in addition to quick buttons.
- Start with Windows writes to the current user's Run registry key.
- Suspend and hibernate depend on system power capabilities and policy.
- Wi-Fi toggling can require administrator approval depending on the adapter and Windows policy.

## Roadmap

- Add a tray icon with quick Stop controls
- Add custom hotkey capture
- Add packaging for a self-contained Windows release
- Add screenshots to the README
- Add tests for settings and power plan parsing

## Safety

QuickTools is designed to be transparent. It does not run hidden automation without visible controls, and actions that affect system power state require user confirmation.
