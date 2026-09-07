using Chronowalker.Core.Models;
using Chronowalker.Core.Services;

namespace Chronowalker.Core.Tests.Services;

/// <summary>Tests game-platform detection outcomes.</summary>
[TestClass]
public sealed class GamePlatformDetectorTests
{
    /// <summary>Verifies detection from a Steam or GOG installation marker.</summary>
    [TestMethod]
    public void Detect_WindowsMarkerExists_ReturnsSteamOrGog()
    {
        using var directory = new TestDirectory();
        string marker = System.IO.Path.Combine(directory.Path, "Dawnwalker-Windows.utoc");
        File.WriteAllText(marker, string.Empty);
        var detector = new GamePlatformDetector(directory.Path, [marker]);

        GamePlatform result = detector.Detect();

        Assert.AreEqual(GamePlatform.SteamOrGog, result);
    }

    /// <summary>Verifies that Xbox configuration evidence is recognized.</summary>
    [TestMethod]
    public void Detect_WinGdkConfigExists_ReturnsXboxPc()
    {
        using var directory = new TestDirectory();
        Directory.CreateDirectory(System.IO.Path.Combine(directory.Path, "Dawnwalker", "Saved", "Config", "WinGDK"));
        var detector = new GamePlatformDetector(directory.Path, []);

        GamePlatform result = detector.Detect();

        Assert.AreEqual(GamePlatform.XboxPc, result);
    }

    /// <summary>Verifies that conflicting evidence is reported instead of guessed.</summary>
    [TestMethod]
    public void Detect_BothConfigsExist_ReturnsAmbiguous()
    {
        using var directory = new TestDirectory();
        string configRoot = System.IO.Path.Combine(directory.Path, "Dawnwalker", "Saved", "Config");
        Directory.CreateDirectory(System.IO.Path.Combine(configRoot, "Windows"));
        Directory.CreateDirectory(System.IO.Path.Combine(configRoot, "WinGDK"));
        var detector = new GamePlatformDetector(directory.Path, []);

        GamePlatform result = detector.Detect();

        Assert.AreEqual(GamePlatform.Ambiguous, result);
    }
}
