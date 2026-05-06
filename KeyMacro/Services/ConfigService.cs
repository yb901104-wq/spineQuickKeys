using System.Text.Json;
using KeyMacro.Models;

namespace KeyMacro.Services;

public class ConfigService
{
    private static readonly string ConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMacro", "config.json");

    public List<MacroSequence> Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return [];
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<List<MacroSequence>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(List<MacroSequence> sequences)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(sequences, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
