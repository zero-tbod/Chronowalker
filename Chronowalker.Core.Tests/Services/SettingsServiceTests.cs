using Chronowalker.Core.Models;
using Chronowalker.Core.Services;

namespace Chronowalker.Core.Tests.Services;

/// <summary>Tests durable selection persistence.</summary>
[TestClass]
public sealed class SettingsServiceTests
{
    /// <summary>Verifies that Recommended defaults are returned when no settings exist.</summary>
    [TestMethod]
    public async Task LoadAsync_MissingFile_ReturnsRecommendedDefaults()
    {
        using var directory = new TestDirectory();
        var service = new SettingsService(directory.Path);

        ChronowalkerSettings settings = await service.LoadAsync();

        Assert.AreEqual(18, settings.Segments);
        Assert.AreEqual(365, settings.Days);
        Assert.IsFalse(settings.IsCustomDeadline);
    }

    /// <summary>Verifies that saved custom settings survive a reload.</summary>
    [TestMethod]
    public async Task SaveAsync_CustomSettings_PersistsEverySelection()
    {
        using var directory = new TestDirectory();
        var service = new SettingsService(directory.Path);
        var expected = new ChronowalkerSettings
        {
            Segments = 16,
            Days = 501,
            IsCustomDeadline = true,
            PlatformChoice = PlatformChoice.XboxPc,
        };

        await service.SaveAsync(expected);
        ChronowalkerSettings actual = await service.LoadAsync();

        Assert.AreEqual(expected.Segments, actual.Segments);
        Assert.AreEqual(expected.Days, actual.Days);
        Assert.AreEqual(expected.IsCustomDeadline, actual.IsCustomDeadline);
        Assert.AreEqual(expected.PlatformChoice, actual.PlatformChoice);
    }
}
