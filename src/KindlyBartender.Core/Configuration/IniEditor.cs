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
/// change; every other line, comment, the order, and each line's own line ending are kept.
/// </summary>
public static class IniEditor
{
    /// <summary>Returns the requirements that the text does not meet. Null text means the file does not exist.</summary>
    public static IReadOnlyList<IniRequirement> FindUnmet(string? text, IEnumerable<IniRequirement> requirements)
    {
        var lines = Split(text ?? string.Empty);
        return requirements
            .Where(r =>
            {
                var values = ValuesOf(lines, r).ToList();
                return values.Count == 0 || values.Any(v => !r.IsSatisfiedBy(v));
            })
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

        var lines = Split(text ?? string.Empty);
        foreach (var requirement in unmet)
        {
            SetValue(lines, requirement);
        }

        return string.Concat(lines.Select(l => l.Text + l.Ending));
    }

    private static void SetValue(List<Line> lines, IniRequirement requirement)
    {
        var replaced = false;
        var lastSectionEnd = -1;
        var section = string.Empty;

        for (var i = 0; i < lines.Count; i++)
        {
            if (TryReadSection(lines[i].Text, out var name))
            {
                if (SameName(section, requirement.Section))
                {
                    lastSectionEnd = i;
                }

                section = name;
                continue;
            }

            if (SameName(section, requirement.Section) && TryReadEntry(lines[i].Text, out var key, out var value) && SameName(key, requirement.Key))
            {
                if (!requirement.IsSatisfiedBy(value))
                {
                    lines[i] = lines[i] with { Text = Entry(requirement) };
                }

                replaced = true;
            }
        }

        if (SameName(section, requirement.Section))
        {
            lastSectionEnd = lines.Count;
        }

        if (replaced)
        {
            return;
        }

        if (lastSectionEnd < 0)
        {
            if (lines.Count > 0 && lines[^1].Text.Trim().Length > 0)
            {
                Insert(lines, lines.Count, string.Empty);
            }

            Insert(lines, lines.Count, $"[{requirement.Section}]");
            Insert(lines, lines.Count, Entry(requirement));
            return;
        }

        // Insert after the last non-blank line of the section, so blank separator lines stay where they were.
        var insertAt = lastSectionEnd;
        while (insertAt > 0 && lines[insertAt - 1].Text.Trim().Length == 0)
        {
            insertAt--;
        }

        Insert(lines, insertAt, Entry(requirement));
    }

    /// <summary>Inserts a line, taking its line ending from a neighbour so the file keeps its style.</summary>
    private static void Insert(List<Line> lines, int index, string text)
    {
        var neighbour = lines.Count == 0 ? null : lines[Math.Min(Math.Max(index - 1, 0), lines.Count - 1)];
        var ending = neighbour?.Ending is { Length: > 0 } e ? e : DefaultEnding(lines);

        if (index == lines.Count && lines.Count > 0 && lines[^1].Ending.Length == 0)
        {
            // The file had no final line break: give the old last line one and keep the new last line without.
            lines[^1] = lines[^1] with { Ending = ending };
            lines.Add(new Line(text, string.Empty));
            return;
        }

        lines.Insert(index, new Line(text, ending));
    }

    private static string DefaultEnding(List<Line> lines) =>
        lines.FirstOrDefault(l => l.Ending.Length > 0)?.Ending ?? "\r\n";

    private static string Entry(IniRequirement requirement) => $"{requirement.Key}={requirement.Value}";

    private static IEnumerable<string> ValuesOf(List<Line> lines, IniRequirement requirement)
    {
        var section = string.Empty;
        foreach (var line in lines)
        {
            if (TryReadSection(line.Text, out var name))
            {
                section = name;
            }
            else if (SameName(section, requirement.Section) && TryReadEntry(line.Text, out var key, out var value) && SameName(key, requirement.Key))
            {
                yield return value;
            }
        }
    }

    /// <summary>Splits text into lines, each with its own line ending ("\r\n", "\n", or none for the last line).</summary>
    private static List<Line> Split(string text)
    {
        var lines = new List<Line>();
        var start = 0;
        while (start < text.Length)
        {
            var newline = text.IndexOf('\n', start);
            if (newline < 0)
            {
                lines.Add(new Line(text[start..], string.Empty));
                break;
            }

            var hasCarriageReturn = newline > start && text[newline - 1] == '\r';
            var end = hasCarriageReturn ? newline - 1 : newline;
            lines.Add(new Line(text[start..end], hasCarriageReturn ? "\r\n" : "\n"));
            start = newline + 1;
        }

        return lines;
    }

    private static bool TryReadSection(string line, out string name)
    {
        var trimmed = line.Trim();
        var close = trimmed.IndexOf(']', StringComparison.Ordinal);
        // A comment may follow the closing bracket: "[Power] ; note".
        if (trimmed.StartsWith('[') && close > 0 && trimmed[(close + 1)..].Trim() is var rest && (rest.Length == 0 || rest[0] is ';' or '#'))
        {
            name = trimmed[1..close].Trim();
            return true;
        }

        name = string.Empty;
        return false;
    }

    private static bool TryReadEntry(string line, out string key, out string value)
    {
        var trimmed = line.Trim();
        var equals = trimmed.IndexOf('=', StringComparison.Ordinal);
        if (trimmed.Length == 0 || trimmed[0] is ';' or '#' or '[' || equals <= 0)
        {
            key = value = string.Empty;
            return false;
        }

        key = trimmed[..equals].Trim();
        value = trimmed[(equals + 1)..].Trim();
        return true;
    }

    private static bool SameName(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private sealed record Line(string Text, string Ending);
}
