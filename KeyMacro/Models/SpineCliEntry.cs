namespace KeyMacro.Models;

public class SpineCliEntry
{
    public string FilePath { get; set; } = "";
    public string FileName => Path.GetFileName(FilePath);
    public bool HasExportConfig { get; set; }
    public string? ExportConfigPath { get; set; }
    public List<string> SelectedAnimations { get; set; } = []; // empty = all
}

public class CliResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public bool Success => ExitCode == 0;
}

public class SpineProjectInfo
{
    public string Version { get; set; } = "";
    public List<string> SkeletonNames { get; set; } = [];
    public List<string> AnimationNames { get; set; } = [];
}
