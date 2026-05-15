using System.Reflection;
using System.Text.Json;
using KeyMacro.Models;

namespace KeyMacro.Services;

public class VirtualLayoutSerializer
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMacro");

    private static readonly string AppDataPath =
        Path.Combine(AppDataDir, "virtual_layout.json");

    private static readonly string ProjectPath =
        Path.Combine(Directory.GetCurrentDirectory(), "virtual_layout.json");

    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private const string EmbeddedResourceName = "KeyMacro.virtual_layout.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private string? _lastLoadPath;

    /// <summary>Single window data (matches old LayoutData fields + Name + Enabled).</summary>
    public class WindowLayoutData
    {
        public string Name { get; set; } = "窗口1";
        public bool Enabled { get; set; } = true;
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
        public bool VerticalMode { get; set; }
        public float ScaleFactor { get; set; } = 1.0f;
        public List<VirtualButton> Buttons { get; set; } = [];
    }

    public class GlobalLayoutData
    {
        public List<WindowLayoutData> Windows { get; set; } = [];
    }

    private static string? ResolveLoadPath()
    {
        if (File.Exists(ProjectPath)) return ProjectPath;
        if (File.Exists(AppDataPath)) return AppDataPath;
        return null;
    }

    private static string? LoadEmbeddedSkinPath()
    {
        try
        {
            using var stream = _assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            var data = JsonSerializer.Deserialize<WindowLayoutData>(reader.ReadToEnd(), JsonOptions);
            return data?.SkinPath;
        }
        catch { return null; }
    }

    public GlobalLayoutData LoadAll()
    {
        _lastLoadPath = ResolveLoadPath();
        if (_lastLoadPath != null)
        {
            try
            {
                var json = File.ReadAllText(_lastLoadPath);

                // Try new format (GlobalLayoutData with "Windows" key — PascalCase from System.Text.Json default)
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Windows", out _))
                {
                    var global = JsonSerializer.Deserialize<GlobalLayoutData>(json, JsonOptions) ?? new GlobalLayoutData();
                    OperationLogger.Info($"VirtualLayoutSerializer.LoadAll: loaded {global.Windows.Count} windows from {_lastLoadPath}");
                    ApplyDefaultSkin(global);
                    return global;
                }

                // Old format: single WindowLayoutData at root
                var single = JsonSerializer.Deserialize<WindowLayoutData>(json, JsonOptions);
                if (single != null)
                {
                    var global = new GlobalLayoutData { Windows = [single] };
                    OperationLogger.Info($"VirtualLayoutSerializer.LoadAll: migrated old format, 1 window");
                    ApplyDefaultSkin(global);
                    return global;
                }
            }
            catch (Exception ex)
            {
                OperationLogger.Error($"VirtualLayoutSerializer.LoadAll: failed: {ex.Message}");
            }
        }

        // No disk file — try embedded default
        try
        {
            using var stream = _assembly.GetManifestResourceStream(EmbeddedResourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var single = JsonSerializer.Deserialize<WindowLayoutData>(reader.ReadToEnd(), JsonOptions);
                if (single != null)
                {
                    var global = new GlobalLayoutData { Windows = [single] };
                    OperationLogger.Info($"VirtualLayoutSerializer.LoadAll: loaded from embedded resource");
                    return global;
                }
            }
        }
        catch { }

        OperationLogger.Info("VirtualLayoutSerializer.LoadAll: no layout source, returning defaults");
        return new GlobalLayoutData
        {
            Windows = [new WindowLayoutData { Name = "窗口1" }]
        };
    }

    public void SaveAll(GlobalLayoutData global)
    {
        try
        {
            var path = _lastLoadPath ?? AppDataPath;
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(global, JsonOptions);
            var windowNames = string.Join(", ", global.Windows.Select(w => w.Name));
            OperationLogger.Info($"VirtualLayoutSerializer.SaveAll: {global.Windows.Count} windows [{windowNames}] to {path}");
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"VirtualLayoutSerializer.SaveAll: failed: {ex.Message}");
        }
    }

    private void ApplyDefaultSkin(GlobalLayoutData global)
    {
        foreach (var w in global.Windows)
        {
            if (string.IsNullOrEmpty(w.SkinPath))
            {
                var embeddedSkin = LoadEmbeddedSkinPath();
                if (!string.IsNullOrEmpty(embeddedSkin))
                    w.SkinPath = embeddedSkin;
            }
        }
    }

    // ── Legacy compatibility methods ──

    /// <summary>Legacy: treat first window as the only window data.</summary>
    public WindowLayoutData Load()
    {
        var global = LoadAll();
        if (global.Windows.Count == 0)
            global.Windows.Add(new WindowLayoutData { Name = "窗口1" });
        return global.Windows[0];
    }

    /// <summary>Legacy: save a single window.</summary>
    public void Save(WindowLayoutData data)
    {
        var global = LoadAll();
        if (global.Windows.Count > 0)
            global.Windows[0] = data;
        else
            global.Windows.Add(data);
        SaveAll(global);
    }
}
