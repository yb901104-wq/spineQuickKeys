namespace KeyMacro.Models;

public class MacroSequence
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "新序列";
    public string TriggerHotkey { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool Loop { get; set; } = false;
    public int LoopIntervalMs { get; set; } = 1000;
    public int LoopCount { get; set; } = 0;
    public string TargetAppPath { get; set; } = "";
    public List<MacroStep> Steps { get; set; } = [];
}

public class MacroStep
{
    public StepType Type { get; set; } = StepType.Key;
    public string Keys { get; set; } = "";
    public int DelayMs { get; set; } = 200;
    public PressMode PressMode { get; set; } = PressMode.Tap;
    public int HoldDurationMs { get; set; } = 0;
}

public enum StepType
{
    Key,
    Combo,
    Text
}

public enum PressMode
{
    Tap,
    Hold
}
