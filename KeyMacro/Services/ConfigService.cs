using System.Text.Json;
using KeyMacro.Models;

namespace KeyMacro.Services;

public class ConfigService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMacro");

    private static readonly string AppDataPath =
        Path.Combine(AppDataDir, "config.json");

    private static readonly string SpinePathFile =
        Path.Combine(AppDataDir, ".spine_path");

    private static readonly string ProjectPath =
        Path.Combine(Directory.GetCurrentDirectory(), "config.json");

    private static string ResolveLoadPath()
    {
        if (File.Exists(ProjectPath)) return ProjectPath;
        return AppDataPath;
    }

    public List<MacroSequence> Load()
    {
        try
        {
            var path = ResolveLoadPath();
            if (!File.Exists(path))
            {
                OperationLogger.Info("ConfigService.Load: config file not found, returning empty list");
                return [];
            }
            var json = File.ReadAllText(path);
            var sequences = JsonSerializer.Deserialize<List<MacroSequence>>(json) ?? [];
            OperationLogger.Info($"ConfigService.Load: loaded {sequences.Count} sequences from {path}");
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
            if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
            var json = JsonSerializer.Serialize(sequences, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppDataPath, json);
            OperationLogger.Info($"ConfigService.Save: saved {sequences.Count} sequences");
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"ConfigService.Save: failed: {ex.Message}");
        }
    }

    public static string? LoadSpinePath()
    {
        try
        {
            return File.Exists(SpinePathFile) ? File.ReadAllText(SpinePathFile).Trim() : null;
        }
        catch { return null; }
    }

    public static void SaveSpinePath(string path)
    {
        try
        {
            if (!Directory.Exists(AppDataDir)) Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(SpinePathFile, path);
        }
        catch { }
    }

    public static void ClearSpinePath()
    {
        try { if (File.Exists(SpinePathFile)) File.Delete(SpinePathFile); }
        catch { }
    }
}
