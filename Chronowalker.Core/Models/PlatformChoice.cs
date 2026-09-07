namespace Chronowalker.Core.Models;

/// <summary>
/// Describes how Chronowalker chooses the target game configuration.
/// </summary>
public enum PlatformChoice
{
    /// <summary>Use automatic detection.</summary>
    Auto,

    /// <summary>Target Steam or GOG.</summary>
    SteamOrGog,

    /// <summary>Target Xbox PC.</summary>
    XboxPc,
}
