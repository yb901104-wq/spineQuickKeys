namespace KeyMacro.Models;

public class SpineCliEntry
{
    public string FilePath { get; set; } = "";
    public string FileName => Path.GetFileName(FilePath);
    public bool HasExportConfig { get; set; }
    public string? ExportConfigPath { get; set; }
}

public class CliResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public bool Success => ExitCode == 0;
}
