using System.Text.Json;
using KeyMacro.Models;

namespace KeyMacro.Services;

public class VirtualLayoutSerializer
{
    private static readonly string LayoutPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyMacro", "virtual_layout.json");

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
        public List<VirtualButton> Buttons { get; set; } = [];
    }

    public LayoutData Load()
    {
        try
        {
            if (!File.Exists(LayoutPath))
            {
                OperationLogger.Info("VirtualLayoutSerializer.Load: layout file not found, returning defaults");
                return new LayoutData();
            }
            var json = File.ReadAllText(LayoutPath);
            var data = JsonSerializer.Deserialize<LayoutData>(json) ?? new LayoutData();
            OperationLogger.Info($"VirtualLayoutSerializer.Load: loaded ({data.Buttons.Count} buttons)");
            return data;
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"VirtualLayoutSerializer.Load: failed: {ex.Message}");
            return new LayoutData();
        }
    }

    public void Save(LayoutData data)
    {
        try
        {
            var dir = Path.GetDirectoryName(LayoutPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LayoutPath, json);
            OperationLogger.Info($"VirtualLayoutSerializer.Save: saved ({data.Buttons.Count} buttons)");
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"VirtualLayoutSerializer.Save: failed: {ex.Message}");
        }
    }
}
