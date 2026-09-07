using System.Text;

namespace Chronowalker.Core.Services;

/// <summary>
/// Performs focused, order-preserving edits to an INI document.
/// </summary>
public sealed class IniDocument
{
    private readonly List<string> _lines;

    private IniDocument(IEnumerable<string> lines)
    {
        _lines = [.. lines];
    }

    /// <summary>
    /// Parses an INI document.
    /// </summary>
    /// <param name="text">The document text.</param>
    /// <returns>A mutable INI document.</returns>
    public static IniDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return new IniDocument([]);
        }

        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        string[] lines = normalized.Split('\n');
        if (lines.Length > 0 && lines[^1].Length == 0)
        {
            lines = lines[..^1];
        }

        return new IniDocument(lines);
    }

    /// <summary>
    /// Gets a value from a section.
    /// </summary>
    /// <param name="section">The section name without brackets.</param>
    /// <param name="key">The key name.</param>
    /// <returns>The trimmed value, or <see langword="null"/> when missing.</returns>
    public string? GetValue(string section, string key)
    {
        ValidateNames(section, key);
        bool insideSection = false;

        foreach (string line in _lines)
        {
            if (TryGetSection(line, out string? currentSection))
            {
                insideSection = string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (insideSection && TryGetAssignment(line, out string? currentKey, out string? value) &&
                string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Sets one value while preserving unrelated lines.
    /// </summary>
    /// <param name="section">The section name without brackets.</param>
    /// <param name="key">The key name.</param>
    /// <param name="value">The new value.</param>
    public void SetValue(string section, string key, string value)
    {
        ValidateNames(section, key);
        ArgumentNullException.ThrowIfNull(value);

        bool insideSection = false;
        bool sectionFound = false;
        bool keyWritten = false;
        var result = new List<string>(_lines.Count + 2);

        foreach (string line in _lines)
        {
            if (TryGetSection(line, out string? currentSection))
            {
                if (insideSection && !keyWritten)
                {
                    result.Add($"{key}={value}");
                    keyWritten = true;
                }

                insideSection = string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase);
                sectionFound |= insideSection;
                result.Add(line);
                continue;
            }

            if (insideSection && TryGetAssignment(line, out string? currentKey, out _) &&
                string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                if (!keyWritten)
                {
                    result.Add($"{key}={value}");
                    keyWritten = true;
                }

                continue;
            }

            result.Add(line);
        }

        if (sectionFound)
        {
            if (insideSection && !keyWritten)
            {
                result.Add($"{key}={value}");
            }
        }
        else
        {
            if (result.Count > 0 && result[^1].Length > 0)
            {
                result.Add(string.Empty);
            }

            result.Add($"[{section}]");
            result.Add($"{key}={value}");
        }

        _lines.Clear();
        _lines.AddRange(result);
    }

    /// <summary>
    /// Removes every occurrence of a key from one section.
    /// </summary>
    /// <param name="section">The section name without brackets.</param>
    /// <param name="key">The key name.</param>
    public void RemoveValue(string section, string key)
    {
        ValidateNames(section, key);
        bool insideSection = false;
        var result = new List<string>(_lines.Count);

        foreach (string line in _lines)
        {
            if (TryGetSection(line, out string? currentSection))
            {
                insideSection = string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase);
                result.Add(line);
                continue;
            }

            if (insideSection && TryGetAssignment(line, out string? currentKey, out _) &&
                string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(line);
        }

        _lines.Clear();
        _lines.AddRange(result);
    }

    /// <summary>
    /// Serializes the document using Windows line endings.
    /// </summary>
    /// <returns>The serialized INI text.</returns>
    public override string ToString()
    {
        return _lines.Count == 0 ? string.Empty : string.Join("\r\n", _lines) + "\r\n";
    }

    private static void ValidateNames(string section, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }

    private static bool TryGetSection(string line, out string? section)
    {
        string trimmed = line.Trim();
        if (trimmed.Length > 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            section = trimmed[1..^1].Trim();
            return true;
        }

        section = null;
        return false;
    }

    private static bool TryGetAssignment(string line, out string? key, out string? value)
    {
        int equalsIndex = line.IndexOf('=', StringComparison.Ordinal);
        if (equalsIndex <= 0)
        {
            key = null;
            value = null;
            return false;
        }

        key = line[..equalsIndex].Trim();
        value = line[(equalsIndex + 1)..].Trim();
        return key.Length > 0;
    }
}
