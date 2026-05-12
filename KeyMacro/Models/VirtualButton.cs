namespace KeyMacro.Models;

public class VirtualButton
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "新按钮";
    public VirtualButtonStyle StyleType { get; set; } = VirtualButtonStyle.SmallIcon;
    public string? BindActionId { get; set; }
    public bool LoopEnabled { get; set; }
    public int LoopInterval { get; set; } = 100;
    public int LoopCount { get; set; } = 1;
    public string? IconPath { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
}

public enum VirtualButtonStyle
{
    SmallIcon,
    LargeIcon,
    LoopIcon
}
