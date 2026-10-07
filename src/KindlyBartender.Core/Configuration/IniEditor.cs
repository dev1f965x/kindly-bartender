namespace KindlyBartender.Core.Configuration;

/// <summary>
/// One setting that detection needs in an INI-style file.
/// </summary>
/// <param name="Section">The section name without brackets.</param>
/// <param name="Key">The key; matched without regard to case.</param>
/// <param name="Value">The value written when the setting is missing or blocks detection.</param>
/// <param name="IsSatisfiedBy">Whether an existing value already allows detection, so that values set by other tools are kept.</param>
public sealed record IniRequirement(string Section, string Key, string Value, Func<string, bool> IsSatisfiedBy);

/// <summary>
/// Checks and edits INI-style files such as Hearthstone's log.config and client.config. Only the required keys
/// change; every other line, comment, and the order are kept, and the file's line endings are reused.
/// </summary>
public static class IniEditor
{
    /// <summary>Returns the requirements that the text does not meet. Null text means the file does not exist.</summary>
    public static IReadOnlyList<IniRequirement> FindUnmet(string? text, IEnumerable<IniRequirement> requirements)
    {
        var values = ReadValues(text ?? string.Empty);
        return requirements
            .Where(r => !values.TryGetValue((r.Section, r.Key), out var found) || found.Any(v => !r.IsSatisfiedBy(v)))
            .ToList();
    }

    /// <summary>Returns the text with every unmet requirement written, or the same text when all are met.</summary>
    public static string Apply(string? text, IEnumerable<IniRequirement> requirements)
    {
        var unmet = FindUnmet(text, requirements);
        if (unmet.Count == 0)
        {
            return text ?? string.Empty;
        }

        var source = text ?? string.Empty;
        var newline = source.Contains("\r\n", StringComparison.Ordinal) || source.Length == 0 ? "\r\n" : "\n";
        var lines = source.Length == 0 ? [] : source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        var endsWithNewline = lines.Count > 0 && lines[^1].Length == 0;
        if (endsWithNewline)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        foreach (var requirement in unmet)
        {
            SetValue(lines, requirement);
        }

        return string.Join(newline, lines) + newline;
    }

    private static void SetValue(List<string> lines, IniRequirement requirement)
    {
        var section = string.Empty;
        var sectionEnd = -1;
        var replaced = false;

        for (var i = 0; i < lines.Count; i++)
        {
            if (TryReadSection(lines[i], out var name))
            {
                if (SameName(section, requirement.Section))
                {
                    sectionEnd = i;
                }

                section = name;
                continue;
            }

            if (SameName(section, requirement.Section)
                && TryReadEntry(lines[i], out var key, out var value)
                && SameName(key, requirement.Key)
                && !requirement.IsSatisfiedBy(value))
            {
                lines[i] = $"{requirement.Key}={requirement.Value}";
                replaced = true;
            }
        }

        if (replaced || HasKey(lines, requirement))
        {
            return;
        }

        var sectionFound = sectionEnd >= 0 || SameName(section, requirement.Section);
        if (!sectionFound)
        {
            if (lines.Count > 0 && lines[^1].Trim().Length > 0)
            {
                lines.Add(string.Empty);
            }

            lines.Add($"[{requirement.Section}]");
            lines.Add($"{requirement.Key}={requirement.Value}");
            return;
        }

        // Insert after the last non-blank line of the section, so blank separator lines stay where they were.
        var insertAt = sectionEnd >= 0 ? sectionEnd : lines.Count;
        while (insertAt > 0 && lines[insertAt - 1].Trim().Length == 0)
        {
            insertAt--;
        }

        lines.Insert(insertAt, $"{requirement.Key}={requirement.Value}");
    }

    private static bool HasKey(List<string> lines, IniRequirement requirement)
    {
        var section = string.Empty;
        foreach (var line in lines)
        {
            if (TryReadSection(line, out var name))
            {
                section = name;
            }
            else if (SameName(section, requirement.Section) && TryReadEntry(line, out var key, out _) && SameName(key, requirement.Key))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<(string Section, string Key), List<string>> ReadValues(string text)
    {
        var values = new Dictionary<(string, string), List<string>>(new NameComparer());
        var section = string.Empty;
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (TryReadSection(line, out var name))
            {
                section = name;
            }
            else if (TryReadEntry(line, out var key, out var value))
            {
                if (!values.TryGetValue((section, key), out var list))
                {
                    values[(section, key)] = list = [];
                }

                list.Add(value);
            }
        }

        return values;
    }

    private static bool TryReadSection(string line, out string name)
    {
        var trimmed = line.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '[' && trimmed[^1] == ']')
        {
            name = trimmed[1..^1].Trim();
            return true;
        }

        name = string.Empty;
        return false;
    }

    private static bool TryReadEntry(string line, out string key, out string value)
    {
        var trimmed = line.Trim();
        var equals = trimmed.IndexOf('=', StringComparison.Ordinal);
        if (trimmed.Length == 0 || trimmed[0] is ';' or '#' || equals <= 0)
        {
            key = value = string.Empty;
            return false;
        }

        key = trimmed[..equals].Trim();
        value = trimmed[(equals + 1)..].Trim();
        return true;
    }

    private static bool SameName(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private sealed class NameComparer : IEqualityComparer<(string, string)>
    {
        public bool Equals((string, string) x, (string, string) y) => SameName(x.Item1, y.Item1) && SameName(x.Item2, y.Item2);

        public int GetHashCode((string, string) obj) =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
    }
}
