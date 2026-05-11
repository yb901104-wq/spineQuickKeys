using System.Text.RegularExpressions;

namespace KeyMacro.Services;

public class SpineHotkeyCategory
{
    public string Name { get; set; } = "";
    public List<SpineHotkeyEntry> Entries { get; set; } = [];
}

public class SpineHotkeyEntry
{
    public string Name { get; set; } = "";
    public string RawShortcut { get; set; } = "";
    public string NormalizedShortcut { get; set; } = "";
}

public static partial class SpineHotkeyParser
{
    [GeneratedRegex(@"^---\s*(.+?)\s*---$")]
    private static partial Regex CategoryPattern();

    private static readonly Dictionary<string, string> KeyNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PERIOD"] = "OemPeriod",
        ["COMMA"] = "Oemcomma",
        ["MINUS"] = "OemMinus",
        ["NUMPAD_MINUS"] = "Subtract",
        ["PLUS"] = "Oemplus",
        ["NUMPAD_PLUS"] = "Add",
        ["EQUALS"] = "Oemplus",
        ["NUMPAD_EQUALS"] = "Oemplus",
        ["SLASH"] = "Oem2",
        ["LEFT_BRACKET"] = "OemOpenBrackets",
        ["RIGHT_BRACKET"] = "OemCloseBrackets",
        ["BACKSLASH"] = "Oem5",
        ["SEMICOLON"] = "Oem1",
        ["SPACE"] = "Space",
        ["ESCAPE"] = "Escape",
        ["PAGE_DOWN"] = "PageDown",
        ["PAGE_UP"] = "PageUp",
        ["HOME"] = "Home",
        ["END"] = "End",
        ["TAB"] = "Tab",
        ["ENTER"] = "Enter",
        ["DELETE"] = "Delete",
        ["BACKSPACE"] = "Back",
        ["INSERT"] = "Insert",
        ["UP"] = "Up",
        ["DOWN"] = "Down",
        ["LEFT"] = "Left",
        ["RIGHT"] = "Right",
        ["NUMLOCK"] = "NumLock",
        ["SCROLLLOCK"] = "Scroll",
        ["PRINTSCREEN"] = "PrintScreen",
        ["BREAK"] = "Pause",
        ["PAUSE"] = "Pause",
        ["CAPSLOCK"] = "CapsLock",
    };

    public static List<SpineHotkeyCategory> Parse(string filePath)
    {
        var categories = new List<SpineHotkeyCategory>();
        var lines = File.ReadAllLines(filePath);

        SpineHotkeyCategory? currentCategory = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var catMatch = CategoryPattern().Match(trimmed);
            if (catMatch.Success)
            {
                currentCategory = new SpineHotkeyCategory { Name = catMatch.Groups[1].Value };
                categories.Add(currentCategory);
                continue;
            }

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx < 0) continue;

            var name = trimmed[..colonIdx].Trim();
            var rawShortcut = trimmed[(colonIdx + 1)..].Trim();
            var normalized = NormalizeShortcut(rawShortcut);

            var entry = new SpineHotkeyEntry
            {
                Name = name,
                RawShortcut = rawShortcut,
                NormalizedShortcut = normalized
            };

            if (currentCategory != null)
                currentCategory.Entries.Add(entry);
            else
            {
                currentCategory = new SpineHotkeyCategory { Name = "General" };
                categories.Add(currentCategory);
                currentCategory.Entries.Add(entry);
            }
        }

        return categories;
    }

    private static string NormalizeShortcut(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";

        var parts = raw.Split('+', StringSplitOptions.TrimEntries);
        var result = new List<string>();

        foreach (var part in parts)
        {
            var normalized = NormalizeKeyPart(part);
            if (!string.IsNullOrEmpty(normalized))
                result.Add(normalized);
        }

        return string.Join("+", result);
    }

    private static string NormalizeKeyPart(string part)
    {
        // Quoted single char: 'w', ''' , ';' etc.
        if (part.StartsWith('\'') && part.EndsWith('\'') && part.Length >= 3)
        {
            var ch = part[1];
            return ch switch
            {
                '\'' => "Oem7",
                ';' => "Oem1",
                ':' => "Oem1",
                '"' => "Oem7",
                _ => ch.ToString().ToUpper()
            };
        }

        // Modifier names
        var modMap = part.ToLower() switch
        {
            "ctrl" => "Ctrl",
            "alt" => "Alt",
            "shift" => "Shift",
            "win" => "Win",
            _ => null
        };
        if (modMap != null) return modMap;

        // Named keys via mapping table
        if (KeyNameMap.TryGetValue(part, out var mapped))
            return mapped;

        // F1-F24: pass through
        if (part.Length >= 2 && part[0] == 'F' && int.TryParse(part[1..], out _))
            return part;

        // Single letter/char
        if (part.Length == 1)
            return part.ToUpper();

        // Try direct Keys enum parse
        if (Enum.TryParse<Keys>(part, true, out _))
            return part;

        return part;
    }
}
