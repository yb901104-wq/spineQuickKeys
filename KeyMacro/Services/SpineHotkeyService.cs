using System.Text.Json;

namespace KeyMacro.Services;

public class SpineHotkeyEntry
{
    public string Name { get; set; } = "";
    public string Keys { get; set; } = "";
    public string? Section { get; set; }
    public string? ChineseNote { get; set; }
}

public class SpineHotkeyService
{
    public string FilePath { get; }
    private readonly string _annotationPath;

    public SpineHotkeyService(string filePath)
    {
        FilePath = filePath;
        _annotationPath = filePath + ".annotations.json";
    }

    public List<SpineHotkeyEntry> Load()
    {
        var lines = File.ReadAllLines(FilePath);
        var entries = new List<SpineHotkeyEntry>();
        string? currentSection = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r', '\n');
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Section header: --- Section Name ---
            if (line.StartsWith("---") && line.EndsWith("---"))
            {
                currentSection = line.Trim('-').Trim();
                entries.Add(new SpineHotkeyEntry
                {
                    Name = line,
                    Keys = "",
                    Section = currentSection,
                    ChineseNote = null
                });
                continue;
            }

            // Hotkey line: Name: keys
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                var name = line[..colonIdx].TrimEnd();
                var keys = line[(colonIdx + 1)..].TrimStart();
                entries.Add(new SpineHotkeyEntry
                {
                    Name = name,
                    Keys = keys,
                    Section = currentSection
                });
            }
        }

        // Load annotations from companion file
        var annotations = LoadAnnotations();
        foreach (var entry in entries)
        {
            if (annotations.TryGetValue(entry.Name, out var note))
                entry.ChineseNote = note;
        }

        return entries;
    }

    public void Save(List<SpineHotkeyEntry> entries)
    {
        var annotations = new Dictionary<string, string>();

        using var writer = new StreamWriter(FilePath, false);
        foreach (var entry in entries)
        {
            if (entry.Name.StartsWith("---"))
            {
                writer.WriteLine(entry.Name);
                if (!string.IsNullOrEmpty(entry.ChineseNote))
                    annotations[entry.Name] = entry.ChineseNote;
            }
            else
            {
                writer.WriteLine($"{entry.Name}: {entry.Keys}");
                if (!string.IsNullOrEmpty(entry.ChineseNote))
                    annotations[entry.Name] = entry.ChineseNote;
            }
        }

        SaveAnnotations(annotations);
    }

    private Dictionary<string, string> LoadAnnotations()
    {
        try
        {
            if (!File.Exists(_annotationPath)) return [];
            var json = File.ReadAllText(_annotationPath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Convert WinForms key name (e.g. "OemPeriod") to Spine format (e.g. "PERIOD").
    /// Used when recording keys via HotkeyRecorderForm.
    /// </summary>
    public static string ToSpineFormat(string winFormsHotkey)
    {
        if (string.IsNullOrWhiteSpace(winFormsHotkey)) return "";

        var parts = winFormsHotkey.Split('+');
        var result = new List<string>();
        foreach (var part in parts)
        {
            result.Add(ReverseMap.TryGetValue(part, out var spine) ? spine : part);
        }
        return string.Join(" + ", result);
    }

    private static readonly Dictionary<string, string> ReverseMap = new()
    {
        // WinForms Keys enum → Spine format (uppercase)
        ["OemPeriod"] = "PERIOD",
        ["Oemcomma"] = "COMMA",
        ["OemMinus"] = "MINUS",
        ["Subtract"] = "NUMPAD_MINUS",
        ["Add"] = "NUMPAD_PLUS",
        ["Oemplus"] = "PLUS",
        ["Oem2"] = "SLASH",
        ["OemOpenBrackets"] = "LEFT_BRACKET",
        ["OemCloseBrackets"] = "RIGHT_BRACKET",
        ["Oem5"] = "BACKSLASH",
        ["Oem1"] = "SEMICOLON",
        ["Oem7"] = "APOSTROPHE",
        ["Space"] = "SPACE",
        ["Escape"] = "ESCAPE",
        ["PageDown"] = "PAGE_DOWN",
        ["PageUp"] = "PAGE_UP",
        ["Home"] = "HOME",
        ["End"] = "END",
        ["Tab"] = "TAB",
        ["Enter"] = "ENTER",
        ["Delete"] = "DELETE",
        ["Back"] = "BACKSPACE",
        ["Insert"] = "INSERT",
        ["Up"] = "UP",
        ["Down"] = "DOWN",
        ["Left"] = "LEFT",
        ["Right"] = "RIGHT",
        ["NumLock"] = "NUMLOCK",
        ["Scroll"] = "SCROLLLOCK",
        ["PrintScreen"] = "PRINTSCREEN",
        ["Pause"] = "PAUSE",
        ["CapsLock"] = "CAPSLOCK",
        ["Capital"] = "CAPSLOCK",
        ["Next"] = "PAGE_DOWN",
        ["Prior"] = "PAGE_UP",
        ["D0"] = "0",
        ["D1"] = "1",
        ["D2"] = "2",
        ["D3"] = "3",
        ["D4"] = "4",
        ["D5"] = "5",
        ["D6"] = "6",
        ["D7"] = "7",
        ["D8"] = "8",
        ["D9"] = "9",
        ["NumPad0"] = "NUMPAD_0",
        ["NumPad1"] = "NUMPAD_1",
        ["NumPad2"] = "NUMPAD_2",
        ["NumPad3"] = "NUMPAD_3",
        ["NumPad4"] = "NUMPAD_4",
        ["NumPad5"] = "NUMPAD_5",
        ["NumPad6"] = "NUMPAD_6",
        ["NumPad7"] = "NUMPAD_7",
        ["NumPad8"] = "NUMPAD_8",
        ["NumPad9"] = "NUMPAD_9",
    };

    private void SaveAnnotations(Dictionary<string, string> annotations)
    {
        try
        {
            var json = JsonSerializer.Serialize(annotations, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_annotationPath, json);
        }
        catch { }
    }
}
