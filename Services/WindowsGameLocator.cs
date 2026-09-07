using System.Security;
using Microsoft.Win32;

namespace Chronowalker.Services;

internal static class WindowsGameLocator
{
    private const string MarkerRelativePath = "Dawnwalker\\Content\\Paks\\Dawnwalker-Windows.utoc";

    public static IReadOnlyCollection<string> GetWindowsInstallMarkers()
    {
        var markers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string steamRoot in GetSteamRoots())
        {
            AddMarker(markers, Path.Combine(steamRoot, "steamapps", "common", "The Blood of Dawnwalker"));
            AddSteamLibraryMarkers(markers, steamRoot);
        }

        foreach (string basePath in GetProgramFileRoots())
        {
            AddMarker(markers, Path.Combine(basePath, "GOG Galaxy", "Games", "The Blood of Dawnwalker"));
            AddMarker(markers, Path.Combine(basePath, "The Blood of Dawnwalker"));
        }

        AddUninstallRegistryMarkers(markers, Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall");
        AddUninstallRegistryMarkers(markers, Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall");
        AddUninstallRegistryMarkers(markers, Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        return markers;
    }

    private static void AddMarker(ISet<string> markers, string gameRoot)
    {
        if (!string.IsNullOrWhiteSpace(gameRoot))
        {
            markers.Add(Path.GetFullPath(Path.Combine(gameRoot, MarkerRelativePath)));
        }
    }

    private static void AddSteamLibraryMarkers(ISet<string> markers, string steamRoot)
    {
        string libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFile))
        {
            return;
        }

        try
        {
            foreach (string line in File.ReadLines(libraryFile))
            {
                string[] parts = line.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2 && string.Equals(parts[0], "path", StringComparison.OrdinalIgnoreCase))
                {
                    string libraryPath = parts[1].Replace("\\\\", "\\", StringComparison.Ordinal);
                    AddMarker(markers, Path.Combine(libraryPath, "steamapps", "common", "The Blood of Dawnwalker"));
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void AddUninstallRegistryMarkers(ISet<string> markers, RegistryKey baseKey, string subkeyPath)
    {
        try
        {
            using RegistryKey? uninstallKey = baseKey.OpenSubKey(subkeyPath);
            if (uninstallKey is null)
            {
                return;
            }

            foreach (string subkeyName in uninstallKey.GetSubKeyNames())
            {
                using RegistryKey? entry = uninstallKey.OpenSubKey(subkeyName);
                string? displayName = entry?.GetValue("DisplayName") as string;
                string? installLocation = entry?.GetValue("InstallLocation") as string;
                if (!string.IsNullOrWhiteSpace(displayName) &&
                    !string.IsNullOrWhiteSpace(installLocation) &&
                    displayName.Contains("Dawnwalker", StringComparison.OrdinalIgnoreCase))
                {
                    AddMarker(markers, installLocation);
                }
            }
        }
        catch (IOException)
        {
        }
        catch (SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void AddSteamRegistryRoot(HashSet<string> roots, RegistryKey baseKey, string subkeyPath, string valueName)
    {
        try
        {
            using RegistryKey? steamKey = baseKey.OpenSubKey(subkeyPath);
            if (steamKey?.GetValue(valueName) is string value && !string.IsNullOrWhiteSpace(value))
            {
                roots.Add(Path.GetFullPath(value.Replace('/', '\\')));
            }
        }
        catch (IOException)
        {
        }
        catch (SecurityException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static IEnumerable<string> GetProgramFileRoots()
    {
        string? programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        string? programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        return new[] { programFiles, programFilesX86 }.OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetSteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSteamRegistryRoot(roots, Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
        AddSteamRegistryRoot(roots, Registry.LocalMachine, @"Software\Valve\Steam", "InstallPath");
        AddSteamRegistryRoot(roots, Registry.LocalMachine, @"Software\WOW6432Node\Valve\Steam", "InstallPath");

        foreach (string basePath in GetProgramFileRoots())
        {
            roots.Add(Path.Combine(basePath, "Steam"));
        }

        return roots;
    }
}
