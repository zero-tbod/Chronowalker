# Chronowalker

![Chronowalker](Assets/ChronowalkerWordmark.png)

[![CI](https://github.com/zero-tbod/Chronowalker/actions/workflows/ci.yml/badge.svg)](https://github.com/zero-tbod/Chronowalker/actions/workflows/ci.yml)

Chronowalker is an independent, open-source Windows app for configuring the time system in *The Blood of Dawnwalker*. It provides a modern WinUI 3 interface, safe `Game.ini` editing, automatic game-version detection, and reversible installation.

## Features

- Segment presets: 8, 12, 16, 18, 26, and 32 segments per 12-hour phase.
- Story deadline presets: 30, 45, 60, 80, 100, 365 (one year), and 9999 days.
- A **Custom** story deadline field with live effective-day preview.
- A **Recommended** button that selects 18 segments and stores 366 days, producing an effective 365-day deadline.
- Automatic Steam/GOG versus Xbox PC detection, with a manual override when detection is ambiguous.
- Safe updates that preserve unrelated INI sections, comments, and values.
- Automatic rollback state so **Uninstall and restore** can restore the prior values and Read-only state.
- Manual `Game.ini` export for users who prefer to install the changes themselves.
- Keyboard-accessible, localized WinUI controls and status messages.
- A single-file, self-contained x64 release that can be transferred and run without installing .NET or the Windows App Runtime.

## Important: custom deadlines use +1

The game treats its stored deadline value with an off-by-one adjustment. Add **1** to the number of playable days you actually want:

| Intended playable time | Enter in Custom |
|---:|---:|
| 30 days | 31 |
| 90 days | 91 |
| 365 days | 366 |

Chronowalker shows both the effective deadline and stored value before it writes anything. The 30-day preset writes 31, and the one-year preset displays 365 days while writing 366. Other preset values retain the established values shown in the menu.

## What the app changes

Chronowalker manages these values in `Game.ini`:

- `TimeSegmentsPer12H`
- `DaysToPass`
- `QuestTimeProgressionTypes`
- `DayTimeNamesByStartSegment`
- `NightTimeNamesByStartSegment`

The target configuration is selected from the standard local app-data locations:

- Steam/GOG: `%LOCALAPPDATA%\Dawnwalker\Saved\Config\Windows\Game.ini`
- Xbox PC: `%LOCALAPPDATA%\Dawnwalker\Saved\Config\WinGDK\Game.ini`

Before writing, the app clears the file's Read-only attribute if necessary. It writes through a temporary file, verifies the result, records the original managed values under `%LOCALAPPDATA%\Chronowalker\State`, and then marks the installed `Game.ini` Read-only. Uninstall restores both the original managed values and the file's prior Read-only state.

Close the game before applying or restoring settings. Other mods that manage either of the same INI keys can conflict.

## Recommended companion mod

For perks and traits that consume no time, see **Traits Have No Time Cost**, an optional file in [Better Story Timer by Caites](https://www.nexusmods.com/thebloodofdawnwalker/mods/10?tab=files).

That companion is a separate third-party mod. Chronowalker does **not** include, download, install, update, or uninstall it. Use Nexus Mods or Vortex and follow its author's instructions.

## Relationship to Dawnwalker Extended Time

[Configurable Extended Days and Time — Including Prologue](https://www.nexusmods.com/thebloodofdawnwalker/mods/103) by Noviathan established the preset-based workflow that motivated this community project. Chronowalker is a new C# implementation and does not contain, modify, or redistribute Noviathan's installer, files, or assets. It is not an official release by, or endorsed by, Noviathan.

The feature-by-feature proposal for the original author is documented in [docs/UPSTREAM-HANDOFF.md](docs/UPSTREAM-HANDOFF.md).

## Build and run

Requirements for development:

- Windows 10 version 1809 or later
- .NET 10 SDK
- Windows App SDK workload/templates for WinUI development

From PowerShell on an x64 machine:

```powershell
dotnet build .\Chronowalker.csproj -c Debug -p:Platform=x64
dotnet test .\Chronowalker.Core.Tests\Chronowalker.Core.Tests.csproj -c Debug
Start-Process .\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\Chronowalker.exe
```

To create and run the self-contained, single-file x64 distribution:

```powershell
dotnet publish .\Chronowalker.csproj -p:PublishProfile=win-x64
Start-Process .\artifacts\publish\win-x64\Chronowalker.exe
```

The publish profile produces one portable `Chronowalker.exe` with the .NET and Windows App SDK dependencies embedded. It does not require Developer Mode, MSIX registration, a .NET installation, or a separately installed Windows App Runtime. Like other supported single-file WinUI apps, it extracts its embedded runtime to a temporary per-user directory when launched.

## Project layout

- `Chronowalker.Core` contains platform-neutral INI, settings, detection, and rollback logic.
- `Chronowalker.Core.Tests` contains the MSTest test suite.
- `Services` contains Windows-specific install discovery and localization adapters.
- `ViewModels` contains the MVVM presentation logic.
- `Strings/en-us` contains user-facing resources.
- `Properties/PublishProfiles` contains the reproducible portable x64 release profile.
- `docs` contains implementation and upstream handoff notes.

## Status and license

Chronowalker is under active development and has not yet been published as a versioned binary release. The source is licensed under GPL-3.0; see [LICENSE](LICENSE).
