namespace Chronowalker.Core.Models;

/// <summary>
/// Identifies the supported game configuration families and detection outcomes.
/// </summary>
public enum GamePlatform
{
    /// <summary>The Steam or GOG configuration family.</summary>
    SteamOrGog,

    /// <summary>The Xbox PC configuration family.</summary>
    XboxPc,

    /// <summary>Both supported configuration families were detected.</summary>
    Ambiguous,

    /// <summary>No supported configuration family was detected.</summary>
    Unknown,
}
