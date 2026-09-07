using Chronowalker.Core.Models;
using Chronowalker.Core.Services;

namespace Chronowalker.Core.Tests.Services;

/// <summary>Tests installation, verification, export, and rollback behavior.</summary>
[TestClass]
public sealed class GameIniServiceTests
{
    /// <summary>Verifies that Recommended settings are written and the file becomes read-only.</summary>
    [TestMethod]
    public async Task ApplyAsync_RecommendedSettings_WritesExpectedValues()
    {
        using var directory = new TestDirectory();
        var service = new GameIniService(directory.Path, System.IO.Path.Combine(directory.Path, "Exports"));
        var options = new ChronowalkerOptions(18, 366, true, GamePlatform.SteamOrGog);

        OperationResult result = await service.ApplyAsync(options);

        string text = await File.ReadAllTextAsync(result.ConfigPath);
        StringAssert.Contains(text, "DaysToPass=366");
        StringAssert.Contains(text, "TimeSegmentsPer12H=18");
        Assert.AreNotEqual(FileAttributes.None, File.GetAttributes(result.ConfigPath) & FileAttributes.ReadOnly);
    }

    /// <summary>Verifies that uninstall restores unrelated and original values.</summary>
    [TestMethod]
    public async Task UninstallAsync_ExistingConfig_RestoresOriginalValues()
    {
        using var directory = new TestDirectory();
        var service = new GameIniService(directory.Path, System.IO.Path.Combine(directory.Path, "Exports"));
        string configPath = service.GetConfigPath(GamePlatform.SteamOrGog);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath)!);
        await File.WriteAllTextAsync(configPath, "[Unrelated]\r\nKeep=Yes\r\n[/Script/Quest.QuestSettings]\r\nDaysToPass=45\r\n");
        var options = new ChronowalkerOptions(18, 366, true, GamePlatform.SteamOrGog);
        await service.ApplyAsync(options);

        OperationResult result = await service.UninstallAsync(GamePlatform.SteamOrGog);

        string restored = await File.ReadAllTextAsync(configPath);
        Assert.IsTrue(result.Changed);
        StringAssert.Contains(restored, "Keep=Yes");
        StringAssert.Contains(restored, "DaysToPass=45");
        Assert.AreEqual(FileAttributes.None, File.GetAttributes(configPath) & FileAttributes.ReadOnly);
    }

    /// <summary>Verifies that uninstall removes an unchanged file created by Chronowalker.</summary>
    [TestMethod]
    public async Task UninstallAsync_AppCreatedConfig_RemovesConfig()
    {
        using var directory = new TestDirectory();
        var service = new GameIniService(directory.Path, System.IO.Path.Combine(directory.Path, "Exports"));
        var options = new ChronowalkerOptions(18, 366, true, GamePlatform.XboxPc);
        OperationResult installed = await service.ApplyAsync(options);

        await service.UninstallAsync(GamePlatform.XboxPc);

        Assert.IsFalse(File.Exists(installed.ConfigPath));
    }

    /// <summary>Verifies that a changed app-created file is kept while managed values are removed.</summary>
    [TestMethod]
    public async Task UninstallAsync_AppCreatedConfigChangedExternally_PreservesExternalEdit()
    {
        using var directory = new TestDirectory();
        var service = new GameIniService(directory.Path, System.IO.Path.Combine(directory.Path, "Exports"));
        var options = new ChronowalkerOptions(18, 366, true, GamePlatform.SteamOrGog);
        OperationResult installed = await service.ApplyAsync(options);
        File.SetAttributes(installed.ConfigPath, FileAttributes.None);
        await File.AppendAllTextAsync(installed.ConfigPath, "[External]\r\nKeep=Yes\r\n");

        await service.UninstallAsync(GamePlatform.SteamOrGog);

        string preserved = await File.ReadAllTextAsync(installed.ConfigPath);
        StringAssert.Contains(preserved, "Keep=Yes");
        Assert.IsFalse(preserved.Contains("DaysToPass=", StringComparison.Ordinal));
        Assert.IsFalse(preserved.Contains("TimeSegmentsPer12H=", StringComparison.Ordinal));
    }

    /// <summary>Verifies that installation clears Read-only and uninstall restores its original state.</summary>
    [TestMethod]
    public async Task ApplyAndUninstallAsync_ReadOnlyExistingConfig_RestoresReadOnlyState()
    {
        using var directory = new TestDirectory();
        var service = new GameIniService(directory.Path, System.IO.Path.Combine(directory.Path, "Exports"));
        string configPath = service.GetConfigPath(GamePlatform.XboxPc);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath)!);
        await File.WriteAllTextAsync(configPath, "[/Script/Quest.QuestSettings]\r\nDaysToPass=30\r\n");
        File.SetAttributes(configPath, FileAttributes.ReadOnly);
        var options = new ChronowalkerOptions(18, 366, true, GamePlatform.XboxPc);

        await service.ApplyAsync(options);
        await service.UninstallAsync(GamePlatform.XboxPc);

        string restored = await File.ReadAllTextAsync(configPath);
        StringAssert.Contains(restored, "DaysToPass=30");
        Assert.AreNotEqual(FileAttributes.None, File.GetAttributes(configPath) & FileAttributes.ReadOnly);
    }

    /// <summary>Verifies that manual export writes a read-only platform-specific file.</summary>
    [TestMethod]
    public async Task ExportAsync_ValidOptions_CreatesReadOnlyFile()
    {
        using var directory = new TestDirectory();
        string exportDirectory = System.IO.Path.Combine(directory.Path, "Exports");
        var service = new GameIniService(directory.Path, exportDirectory);
        var options = new ChronowalkerOptions(12, 30, false, GamePlatform.SteamOrGog);

        string path = await service.ExportAsync(options);

        string text = await File.ReadAllTextAsync(path);
        StringAssert.Contains(text, "DaysToPass=31");
        Assert.AreNotEqual(FileAttributes.None, File.GetAttributes(path) & FileAttributes.ReadOnly);
    }
}
