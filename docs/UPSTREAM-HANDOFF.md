# Upstream handoff proposal

This document is a concise proposal for Noviathan, author of **Configurable Extended Days and Time — Including Prologue**. It describes behavior implemented independently in Chronowalker; it is not a redistribution or modification of Noviathan's files.

## Changes offered for consideration

### Custom story deadline

- Add a `365 days — 1 year` preset that stores 366 and use it as the first-run default.
- Add a `Custom` choice to the story-deadline selector.
- Accept positive whole numbers.
- Explain the game's +1 behavior beside the field: a requested 365 playable days must be stored as 366.
- Show both the intended effective deadline and stored value before installation.

### Recommended configuration

- Add a one-click `Recommended` action.
- Select 18 segments per 12-hour phase.
- Select Custom and store 366 days, which the interface labels as an effective 365-day deadline.

### Game-version handling

- Detect existing `Windows` and `WinGDK` configuration families.
- Supplement configuration detection with Steam and Windows uninstall/registry install markers.
- Require a manual choice when evidence is missing or conflicting instead of guessing.

### Safer Game.ini updates

- Remove Read-only before a write when necessary.
- Preserve unrelated sections, keys, blank lines, and comments.
- Write through a temporary file and verify the managed values afterward.
- Save sufficient rollback state to restore the prior managed values and prior Read-only state.
- Mark the installed configuration Read-only only after verification succeeds.
- If Chronowalker created the file, remove it during uninstall only when it is still the file Chronowalker wrote; otherwise remove only the managed keys.

### Modern graphical interface

- Present the same settings through a C# WinUI 3 desktop app.
- Keep user-facing text in localized resources.
- Provide keyboard navigation, automation names, accessible status reporting, and responsive scrolling.
- Use a compact two-column layout that expands for status content and scrolls only when the display cannot fit it.
- Offer a self-contained, single-file x64 executable for direct transfer.
- Separate the platform-neutral INI/configuration engine from Windows UI code and test the engine independently.

## Ownership and permission boundary

Noviathan's Nexus page requires permission before modifying or reusing the original files and assets. This repository therefore uses independently written source and original UI resources. It does not claim that the upstream author has approved, endorsed, or accepted any proposal.

If Noviathan is interested, the behaviors above can be discussed individually without transferring or republishing either author's existing installer files.
