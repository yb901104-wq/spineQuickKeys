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
            if (!File.Exists(ConfigPath))
            {
                OperationLogger.Info("ConfigService.Load: config file not found, returning empty list");
                return [];
            }
            var json = File.ReadAllText(ConfigPath);
            var sequences = JsonSerializer.Deserialize<List<MacroSequence>>(json) ?? [];
            OperationLogger.Info($"ConfigService.Load: loaded {sequences.Count} sequences");
            return sequences;
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"ConfigService.Load: failed: {ex.Message}");
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
            OperationLogger.Info($"ConfigService.Save: saved {sequences.Count} sequences");
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"ConfigService.Save: failed: {ex.Message}");
        }
    }
}
