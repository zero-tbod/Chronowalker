# Changelog

All notable project changes will be recorded here. The project has not yet published a versioned binary release.

## Unreleased

### Added

- Original C# and WinUI 3 implementation of Chronowalker.
- Presets for 8, 12, 16, 18, 26, or 32 segments per 12-hour phase.
- Preset and custom story deadlines with explicit +1 guidance and live preview.
- One-year preset that displays 365 playable days and writes the required stored value of 366.
- Recommended configuration of 18 segments and a stored deadline of 366 days.
- Steam/GOG and Xbox PC configuration detection with manual override.
- Safe `Game.ini` merge, verification, Read-only handling, rollback, uninstall, and manual export.
- Local settings persistence and localized, accessible user interface resources.
- Compact two-column layout with an integrated draggable header and low-resolution scrolling fallback.
- Gold Chronowalker wordmark, compact near-square controls, and a native small-corner window frame.
- Dynamic startup and status-banner sizing that avoids unnecessary scrollbars while retaining a low-resolution fallback.
- Reproducible compressed, self-contained, single-file x64 publishing.
- Unit tests for options, INI preservation, platform detection, settings, application, and rollback.

### Excluded intentionally

- No installer code or assets copied verbatim from Noviathan's Dawnwalker Extended Time; Chronowalker is a new C# implementation of the established behavior.
