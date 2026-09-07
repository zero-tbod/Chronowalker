using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Chronowalker.Core.Models;

namespace Chronowalker.Core.Services;

/// <summary>
/// Applies, verifies, exports, and safely rolls back Chronowalker settings.
/// </summary>
public sealed class GameIniService
{
    private const string QuestSection = "/Script/Quest.QuestSettings";
    private const string TimeSection = "/Script/DogwoodSystem.DogwoodSystemSettings";
    private const string QuestProgressionValue = "((Minimal,(TimeSegments=1,Minutes=30)),(Small,(TimeSegments=1,Minutes=30)),(Medium,(TimeSegments=1,Minutes=30)),(Large,(TimeSegments=1,Minutes=30)),(Huge,(TimeSegments=1,Minutes=30)),(HalfDay,(TimeSegments=1,Minutes=30)),(FullDay,(TimeSegments=1,Minutes=30)))";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _exportDirectory;
    private readonly string _localAppDataPath;
    private readonly string _stateDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameIniService"/> class.
    /// </summary>
    /// <param name="localAppDataPath">The local application data directory.</param>
    /// <param name="exportDirectory">The directory used for manual exports.</param>
    public GameIniService(string localAppDataPath, string exportDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localAppDataPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(exportDirectory);
        _localAppDataPath = Path.GetFullPath(localAppDataPath);
        _exportDirectory = Path.GetFullPath(exportDirectory);
        _stateDirectory = Path.Combine(_localAppDataPath, "Chronowalker", "State");
    }

    /// <summary>
    /// Gets the target <c>Game.ini</c> path for a resolved platform.
    /// </summary>
    /// <param name="platform">The target platform.</param>
    /// <returns>The absolute configuration path.</returns>
    public string GetConfigPath(GamePlatform platform)
    {
        string folderName = platform switch
        {
            GamePlatform.SteamOrGog => "Windows",
            GamePlatform.XboxPc => "WinGDK",
            _ => throw new ArgumentOutOfRangeException(nameof(platform)),
        };

        return Path.Combine(_localAppDataPath, "Dawnwalker", "Saved", "Config", folderName, "Game.ini");
    }

    /// <summary>
    /// Applies and verifies the selected settings, preserving the original values for rollback.
    /// </summary>
    /// <param name="options">The selected Chronowalker options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The operation result.</returns>
    public async Task<OperationResult> ApplyAsync(
        ChronowalkerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        string configPath = GetConfigPath(options.Platform);
        string statePath = GetStatePath(options.Platform);
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        Directory.CreateDirectory(_stateDirectory);

        bool fileExisted = File.Exists(configPath);
        bool wasReadOnly = fileExisted && IsReadOnly(configPath);
        if (wasReadOnly)
        {
            SetReadOnly(configPath, false);
        }

        try
        {
            IniDocument document = await ReadDocumentAsync(configPath, cancellationToken).ConfigureAwait(false);
            GameIniState state = File.Exists(statePath)
                ? await ReadStateAsync(statePath, cancellationToken).ConfigureAwait(false)
                : CaptureState(document, fileExisted, wasReadOnly);

            if (!File.Exists(statePath))
            {
                await WriteStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);
            }

            ApplyOptions(document, options);
            await WriteDocumentAsync(configPath, document, cancellationToken).ConfigureAwait(false);
            IniDocument writtenDocument = await ReadDocumentAsync(configPath, cancellationToken).ConfigureAwait(false);
            Verify(writtenDocument, options);

            state.ManagedHash = ComputeHash(writtenDocument.ToString());
            await WriteStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);
            SetReadOnly(configPath, true);
            return new OperationResult(true, configPath);
        }
        catch
        {
            if (wasReadOnly && File.Exists(configPath))
            {
                SetReadOnly(configPath, true);
            }

            throw;
        }
    }

    /// <summary>
    /// Restores original values when a Chronowalker rollback state exists.
    /// </summary>
    /// <param name="platform">The target platform.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The operation result.</returns>
    public async Task<OperationResult> UninstallAsync(
        GamePlatform platform,
        CancellationToken cancellationToken = default)
    {
        string configPath = GetConfigPath(platform);
        string statePath = GetStatePath(platform);
        if (!File.Exists(statePath))
        {
            return new OperationResult(false, configPath);
        }

        GameIniState state = await ReadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
        if (!File.Exists(configPath))
        {
            File.Delete(statePath);
            return new OperationResult(true, configPath);
        }

        if (IsReadOnly(configPath))
        {
            SetReadOnly(configPath, false);
        }

        IniDocument document = await ReadDocumentAsync(configPath, cancellationToken).ConfigureAwait(false);
        if (!state.FileExisted &&
            !string.IsNullOrWhiteSpace(state.ManagedHash) &&
            string.Equals(state.ManagedHash, ComputeHash(document.ToString()), StringComparison.Ordinal))
        {
            File.Delete(configPath);
        }
        else
        {
            RestoreOriginalValues(document, state);
            await WriteDocumentAsync(configPath, document, cancellationToken).ConfigureAwait(false);
            SetReadOnly(configPath, state.WasReadOnly);
        }

        File.Delete(statePath);
        return new OperationResult(true, configPath);
    }

    /// <summary>
    /// Exports a standalone <c>Game.ini</c> containing only Chronowalker settings.
    /// </summary>
    /// <param name="options">The selected Chronowalker options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The absolute exported file path.</returns>
    public async Task<string> ExportAsync(
        ChronowalkerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        Directory.CreateDirectory(_exportDirectory);

        string platformName = options.Platform == GamePlatform.XboxPc ? "Xbox-PC" : "Steam-GOG";
        string exportPath = Path.Combine(_exportDirectory, $"Chronowalker-{platformName}-Game.ini");
        if (File.Exists(exportPath) && IsReadOnly(exportPath))
        {
            SetReadOnly(exportPath, false);
        }

        IniDocument document = IniDocument.Parse(string.Empty);
        ApplyOptions(document, options);
        await WriteDocumentAsync(exportPath, document, cancellationToken).ConfigureAwait(false);
        SetReadOnly(exportPath, true);
        return exportPath;
    }

    private static void ApplyOptions(IniDocument document, ChronowalkerOptions options)
    {
        (string day, string night) = BuildTimeNameLayout(options.Segments);
        document.SetValue(QuestSection, "DaysToPass", options.GetWrittenDeadline().ToString(System.Globalization.CultureInfo.InvariantCulture));
        document.SetValue(TimeSection, "TimeSegmentsPer12H", options.Segments.ToString(System.Globalization.CultureInfo.InvariantCulture));
        document.SetValue(TimeSection, "QuestTimeProgressionTypes", QuestProgressionValue);
        document.SetValue(TimeSection, "DayTimeNamesByStartSegment", day);
        document.SetValue(TimeSection, "NightTimeNamesByStartSegment", night);
    }

    private static (string Day, string Night) BuildTimeNameLayout(int segments)
    {
        int firstQuarter = segments / 4;
        int half = segments / 2;
        int thirdQuarter = (segments * 3) / 4;
        const string table = "/Game/_Dawnwalker/UI/_Unified/StringTables/ST_UI_TimeLineCommon.ST_UI_TimeLineCommon";
        string day = $"((0, LOCTABLE(\"{table}\", \"DayTimeNames0\")),({firstQuarter}, LOCTABLE(\"{table}\", \"DayTimeNames2\")),({half}, LOCTABLE(\"{table}\", \"DayTimeNames4\")),({thirdQuarter}, LOCTABLE(\"{table}\", \"DayTimeNames6\")))";
        string night = $"((0, LOCTABLE(\"{table}\", \"NightTimeNames0\")),({firstQuarter}, LOCTABLE(\"{table}\", \"NightTimeNames2\")),({half}, LOCTABLE(\"{table}\", \"NightTimeNames4\")),({thirdQuarter}, LOCTABLE(\"{table}\", \"NightTimeNames6\")))";
        return (day, night);
    }

    private static GameIniState CaptureState(IniDocument document, bool fileExisted, bool wasReadOnly)
    {
        return new GameIniState
        {
            FileExisted = fileExisted,
            WasReadOnly = wasReadOnly,
            OriginalDays = document.GetValue(QuestSection, "DaysToPass"),
            OriginalSegments = document.GetValue(TimeSection, "TimeSegmentsPer12H"),
            OriginalQuestProgression = document.GetValue(TimeSection, "QuestTimeProgressionTypes"),
            OriginalDayTimeNames = document.GetValue(TimeSection, "DayTimeNamesByStartSegment"),
            OriginalNightTimeNames = document.GetValue(TimeSection, "NightTimeNamesByStartSegment"),
        };
    }

    private static string ComputeHash(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }

    private static bool IsReadOnly(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0;
    }

    private static async Task<IniDocument> ReadDocumentAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return IniDocument.Parse(string.Empty);
        }

        string text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return IniDocument.Parse(text);
    }

    private static async Task<GameIniState> ReadStateAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        GameIniState? state = await JsonSerializer.DeserializeAsync<GameIniState>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        return state ?? throw new InvalidDataException("The Chronowalker rollback state is empty.");
    }

    private static void RestoreOriginalValues(IniDocument document, GameIniState state)
    {
        RestoreValue(document, QuestSection, "DaysToPass", state.OriginalDays);
        RestoreValue(document, TimeSection, "TimeSegmentsPer12H", state.OriginalSegments);
        RestoreValue(document, TimeSection, "QuestTimeProgressionTypes", state.OriginalQuestProgression);
        RestoreValue(document, TimeSection, "DayTimeNamesByStartSegment", state.OriginalDayTimeNames);
        RestoreValue(document, TimeSection, "NightTimeNamesByStartSegment", state.OriginalNightTimeNames);
    }

    private static void RestoreValue(IniDocument document, string section, string key, string? value)
    {
        if (value is null)
        {
            document.RemoveValue(section, key);
        }
        else
        {
            document.SetValue(section, key, value);
        }
    }

    private static void SetReadOnly(string path, bool isReadOnly)
    {
        FileAttributes attributes = File.GetAttributes(path);
        attributes = isReadOnly ? attributes | FileAttributes.ReadOnly : attributes & ~FileAttributes.ReadOnly;
        File.SetAttributes(path, attributes);
    }

    private static void Verify(IniDocument document, ChronowalkerOptions options)
    {
        (string day, string night) = BuildTimeNameLayout(options.Segments);
        var expected = new Dictionary<(string Section, string Key), string>
        {
            [(QuestSection, "DaysToPass")] = options.GetWrittenDeadline().ToString(System.Globalization.CultureInfo.InvariantCulture),
            [(TimeSection, "TimeSegmentsPer12H")] = options.Segments.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [(TimeSection, "QuestTimeProgressionTypes")] = QuestProgressionValue,
            [(TimeSection, "DayTimeNamesByStartSegment")] = day,
            [(TimeSection, "NightTimeNamesByStartSegment")] = night,
        };

        foreach (((string section, string key), string value) in expected)
        {
            if (!string.Equals(document.GetValue(section, key), value, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Game.ini verification failed for {key}.");
            }
        }
    }

    private static async Task WriteDocumentAsync(
        string path,
        IniDocument document,
        CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, document.ToString(), new UTF8Encoding(true), cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task WriteStateAsync(
        string path,
        GameIniState state,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(state, JsonOptions);
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, json, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private string GetStatePath(GamePlatform platform)
    {
        string platformName = platform switch
        {
            GamePlatform.SteamOrGog => "Windows",
            GamePlatform.XboxPc => "WinGDK",
            _ => throw new ArgumentOutOfRangeException(nameof(platform)),
        };

        return Path.Combine(_stateDirectory, $"GameIniState-{platformName}.json");
    }

    private sealed class GameIniState
    {
        public bool FileExisted { get; set; }

        public bool WasReadOnly { get; set; }

        public string? OriginalDays { get; set; }

        public string? OriginalSegments { get; set; }

        public string? OriginalQuestProgression { get; set; }

        public string? OriginalDayTimeNames { get; set; }

        public string? OriginalNightTimeNames { get; set; }

        public string? ManagedHash { get; set; }
    }
}
