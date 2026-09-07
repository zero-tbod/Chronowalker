using System.Text;
using System.Text.Json;
using Chronowalker.Core.Models;

namespace Chronowalker.Core.Services;

/// <summary>
/// Persists Chronowalker selections in local application data.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="localAppDataPath">The local application data directory.</param>
    public SettingsService(string localAppDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppDataPath);
        _settingsPath = Path.Combine(Path.GetFullPath(localAppDataPath), "Chronowalker", "Settings.json");
    }

    /// <summary>
    /// Loads the last valid selections or recommended defaults.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The persisted or default settings.</returns>
    public async Task<ChronowalkerSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            return new ChronowalkerSettings();
        }

        try
        {
            await using FileStream stream = File.OpenRead(_settingsPath);
            ChronowalkerSettings? settings = await JsonSerializer.DeserializeAsync<ChronowalkerSettings>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            return settings is not null && IsValid(settings) ? settings : new ChronowalkerSettings();
        }
        catch (JsonException)
        {
            return new ChronowalkerSettings();
        }
    }

    /// <summary>
    /// Saves the selected settings atomically.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveAsync(
        ChronowalkerSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!IsValid(settings))
        {
            throw new ArgumentOutOfRangeException(nameof(settings));
        }

        string directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(_settingsPath)}.{Guid.NewGuid():N}.tmp");
        string json = JsonSerializer.Serialize(settings, JsonOptions);

        try
        {
            await File.WriteAllTextAsync(temporaryPath, json, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, _settingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool IsValid(ChronowalkerSettings settings)
    {
        bool validSegments = settings.Segments is 8 or 12 or 16 or 18 or 26 or 32;
        bool validDays = settings.Days > 0;
        bool validPreset = settings.IsCustomDeadline || settings.Days is 30 or 45 or 60 or 80 or 100 or 365 or 9999;
        bool validPlatform = Enum.IsDefined(settings.PlatformChoice);
        return validSegments && validDays && validPreset && validPlatform;
    }
}
