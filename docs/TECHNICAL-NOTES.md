# Technical notes

## Configuration model

`ChronowalkerOptions` validates the supported segment presets, positive deadlines, and concrete game platform. Preset value 30 maps to stored value 31; custom values are written exactly as entered so the UI can explain and preview the required +1 adjustment.

## INI preservation

`IniDocument` parses the file as ordered lines. Setting a value changes the last matching key in the requested section, removes duplicate managed keys, and leaves unrelated content in place. Removing a managed value also removes duplicates without reconstructing the rest of the document.

The managed sections and keys are centralized in `GameIniService`. Writes use a same-directory temporary file followed by replacement/move so a partial write cannot become the active configuration.

## Rollback

Before the first managed write, `GameIniService` records:

- target configuration path;
- whether the target existed;
- its prior Read-only state;
- the original values of every managed key; and
- the hash of the content Chronowalker installed.

Uninstall restores or removes only the managed keys, preserving unrelated edits. A file created solely by Chronowalker is deleted only when it still matches the content Chronowalker installed; if another program changed it afterward, the file is kept and only Chronowalker's managed keys are removed.

## Detection

`GamePlatformDetector` combines evidence from the `Windows` and `WinGDK` configuration directories with install markers supplied by `WindowsGameLocator`. The locator checks Steam registry/library locations plus GOG and Windows uninstall records. Conflicting evidence produces `Ambiguous`; missing evidence produces `Unknown`. Neither state is silently guessed during installation.

## User data

- Settings: `%LOCALAPPDATA%\Chronowalker\Settings.json`
- Rollback state: `%LOCALAPPDATA%\Chronowalker\State`
- Game configuration: `%LOCALAPPDATA%\Dawnwalker\Saved\Config\Windows|WinGDK\Game.ini`

Chronowalker makes no network requests and stores no credentials or Nexus Mods data.
