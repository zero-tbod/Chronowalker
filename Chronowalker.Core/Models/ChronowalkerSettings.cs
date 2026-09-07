namespace Chronowalker.Core.Models;

/// <summary>
/// Stores the user's last Chronowalker selections.
/// </summary>
public sealed class ChronowalkerSettings
{
    /// <summary>Gets or sets the selected segment count.</summary>
    public int Segments { get; set; } = 18;

    /// <summary>Gets or sets the selected deadline value.</summary>
    public int Days { get; set; } = 365;

    /// <summary>Gets or sets a value indicating whether the deadline is custom.</summary>
    public bool IsCustomDeadline { get; set; }

    /// <summary>Gets or sets the selected platform behavior.</summary>
    public PlatformChoice PlatformChoice { get; set; } = PlatformChoice.Auto;
}
