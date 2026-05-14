using System.Reflection;
using System.Text.Json;
using KeyMacro.Models;

namespace KeyMacro.Services;

public class VirtualLayoutSerializer
{
    private static readonly string AppDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMacro", "virtual_layout.json");

    private static readonly string ProjectPath =
        Path.Combine(Directory.GetCurrentDirectory(), "virtual_layout.json");

    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private const string EmbeddedResourceName = "KeyMacro.virtual_layout.json";

    public class LayoutData
    {
        public int WindowX { get; set; }
        public int WindowY { get; set; }
        public int WindowWidth { get; set; } = 400;
        public int WindowHeight { get; set; } = 300;
        public bool TopMost { get; set; } = true;
        public bool PositionLocked { get; set; }
        public bool WindowLocked { get; set; }
        public string? TargetProcessName { get; set; }
        public string? TargetWindowTitle { get; set; }
        public string? SkinPath { get; set; }
        public bool SingleLineMode { get; set; } = true;
        public float ScaleFactor { get; set; }
        public List<VirtualButton> Buttons { get; set; } = [];
    }

    private static string? ResolveLoadPath()
    {
        if (File.Exists(ProjectPath)) return ProjectPath;
        if (File.Exists(AppDataPath)) return AppDataPath;
        return null;
    }

    /// <summary>Try to load SkinPath from the embedded default layout.</summary>
    private static string? LoadEmbeddedSkinPath()
    {
        try
        {
            using var stream = _assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            var data = JsonSerializer.Deserialize<LayoutData>(reader.ReadToEnd());
            return data?.SkinPath;
        }
        catch { return null; }
    }

    public LayoutData Load()
    {
        var diskPath = ResolveLoadPath();
        if (diskPath != null)
        {
            try
            {
                var json = File.ReadAllText(diskPath);
                var data = JsonSerializer.Deserialize<LayoutData>(json) ?? new LayoutData();
                OperationLogger.Info($"VirtualLayoutSerializer.Load: loaded from {diskPath} ({data.Buttons.Count} buttons)");

                // If loaded from APPDATA and has no SkinPath, try embedded default
                if (string.IsNullOrEmpty(data.SkinPath))
                {
                    var embeddedSkin = LoadEmbeddedSkinPath();
                    if (!string.IsNullOrEmpty(embeddedSkin))
                    {
                        data.SkinPath = embeddedSkin;
                        OperationLogger.Info($"VirtualLayoutSerializer.Load: applied SkinPath \"{embeddedSkin}\" from embedded default");
                    }
                }
                return data;
            }
            catch (Exception ex)
            {
                OperationLogger.Error($"VirtualLayoutSerializer.Load: disk read failed: {ex.Message}");
            }
        }

        // No disk file — use embedded resource directly
        try
        {
            using var stream = _assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var data = JsonSerializer.Deserialize<LayoutData>(reader.ReadToEnd()) ?? new LayoutData();
                OperationLogger.Info($"VirtualLayoutSerializer.Load: loaded from embedded resource ({data.Buttons.Count} buttons)");
                return data;
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"VirtualLayoutSerializer.Load: embedded fallback failed: {ex.Message}");
        }

        OperationLogger.Info("VirtualLayoutSerializer.Load: no layout source found, returning defaults");
        return new LayoutData();
    }

    public void Save(LayoutData data)
    {
        try
        {
            var dir = Path.GetDirectoryName(AppDataPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppDataPath, json);
            OperationLogger.Info($"VirtualLayoutSerializer.Save: saved ({data.Buttons.Count} buttons)");
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"VirtualLayoutSerializer.Save: failed: {ex.Message}");
        }
    }
}
