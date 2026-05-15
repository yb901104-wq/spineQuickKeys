using KeyMacro.Services;

namespace KeyMacro.Models;

public class DataBundle
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<SpineHotkeyEntry>? SpineHotkeys { get; set; }
    public List<MacroSequence>? Sequences { get; set; }
    public VirtualLayoutSerializer.LayoutData? VkData { get; set; }
}
