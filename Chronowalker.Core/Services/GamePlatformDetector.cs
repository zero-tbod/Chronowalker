using Chronowalker.Core.Models;

namespace Chronowalker.Core.Services;

/// <summary>
/// Detects the installed game configuration family from local evidence.
/// </summary>
public sealed class GamePlatformDetector
{
    private readonly string _localAppDataPath;
    private readonly string[] _windowsInstallMarkers;

    /// <summary>
    /// Initializes a new instance of the <see cref="GamePlatformDetector"/> class.
    /// </summary>
    /// <param name="localAppDataPath">The local application data directory.</param>
    /// <param name="windowsInstallMarkers">Known Steam or GOG game marker paths.</param>
    public GamePlatformDetector(string localAppDataPath, IEnumerable<string> windowsInstallMarkers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppDataPath);
        ArgumentNullException.ThrowIfNull(windowsInstallMarkers);
        _localAppDataPath = Path.GetFullPath(localAppDataPath);
        _windowsInstallMarkers = [.. windowsInstallMarkers.Where(static path => !string.IsNullOrWhiteSpace(path))];
    }

    /// <summary>
    /// Detects a platform without changing local files.
    /// </summary>
    /// <returns>The detected platform or an ambiguous/unknown result.</returns>
    public GamePlatform Detect()
    {
        string configRoot = Path.Combine(_localAppDataPath, "Dawnwalker", "Saved", "Config");
        bool windowsDetected = Directory.Exists(Path.Combine(configRoot, "Windows")) ||
            _windowsInstallMarkers.Any(File.Exists);
        bool xboxDetected = Directory.Exists(Path.Combine(configRoot, "WinGDK"));

        if (windowsDetected && xboxDetected)
        {
            return GamePlatform.Ambiguous;
        }

        if (xboxDetected)
        {
            return GamePlatform.XboxPc;
        }

        return windowsDetected ? GamePlatform.SteamOrGog : GamePlatform.Unknown;
    }
}
