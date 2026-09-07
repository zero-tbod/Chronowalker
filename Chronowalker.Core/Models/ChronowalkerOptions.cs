namespace Chronowalker.Core.Models;

/// <summary>
/// Contains a validated Chronowalker configuration selection.
/// </summary>
public sealed class ChronowalkerOptions
{
    private static readonly int[] AllowedSegments = [8, 12, 16, 18, 26, 32];
    private static readonly int[] AllowedPresetDays = [30, 45, 60, 80, 100, 9999];

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronowalkerOptions"/> class.
    /// </summary>
    /// <param name="segments">The number of time segments in each 12-hour phase.</param>
    /// <param name="days">The selected stored deadline value.</param>
    /// <param name="isCustomDeadline">Whether the deadline was entered as a custom value.</param>
    /// <param name="platform">The resolved target platform.</param>
    public ChronowalkerOptions(int segments, int days, bool isCustomDeadline, GamePlatform platform)
    {
        Segments = segments;
        Days = days;
        IsCustomDeadline = isCustomDeadline;
        Platform = platform;
    }

    /// <summary>Gets the number of segments in each 12-hour phase.</summary>
    public int Segments { get; }

    /// <summary>Gets the selected deadline value.</summary>
    public int Days { get; }

    /// <summary>Gets a value indicating whether the deadline is custom.</summary>
    public bool IsCustomDeadline { get; }

    /// <summary>Gets the resolved game platform.</summary>
    public GamePlatform Platform { get; }

    /// <summary>
    /// Returns the value that must be written to <c>DaysToPass</c>.
    /// </summary>
    /// <returns>The game-facing deadline value.</returns>
    public int GetWrittenDeadline()
    {
        Validate();
        return !IsCustomDeadline && Days == 30 ? 31 : Days;
    }

    /// <summary>
    /// Validates that every option is supported.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An option is outside the supported range.</exception>
    public void Validate()
    {
        if (!AllowedSegments.Contains(Segments))
        {
            throw new ArgumentOutOfRangeException(nameof(Segments));
        }

        if (Days < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Days));
        }

        if (!IsCustomDeadline && !AllowedPresetDays.Contains(Days))
        {
            throw new ArgumentOutOfRangeException(nameof(Days));
        }

        if (Platform is not GamePlatform.SteamOrGog and not GamePlatform.XboxPc)
        {
            throw new ArgumentOutOfRangeException(nameof(Platform));
        }
    }
}
