using System.Text.Json.Serialization;
using KeyMacro.Services;

namespace KeyMacro.Models;

public class DataBundle
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<SpineHotkeyEntry>? SpineHotkeys { get; set; }
    public List<MacroSequence>? Sequences { get; set; }

    // New: export/import all VK windows
    public List<VirtualLayoutSerializer.WindowLayoutData>? VkDataList { get; set; }

    // Legacy compat: single-window format, kept for deserializing old .kmp files
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public VirtualLayoutSerializer.WindowLayoutData? VkData { get; set; }
}
