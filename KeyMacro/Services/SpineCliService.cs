#nullable disable
using System.Diagnostics;
using System.Runtime.Versioning;
using KeyMacro.Models;
using Microsoft.Win32;

namespace KeyMacro.Services;

public class SpineCliService
{
    public string SpinePath { get; set; } = "";

    public bool IsValid => !string.IsNullOrEmpty(SpinePath) && File.Exists(SpinePath);

    /// <summary>Detect Spine.com from registry or common install paths.</summary>
    [SupportedOSPlatform("windows")]
    public string DetectFromRegistry()
    {
        // Check registry for Spine install
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key != null)
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using var subKey = key.OpenSubKey(subKeyName);
                    if (subKey?.GetValue("DisplayName") is string name &&
                        name.Contains("Spine", StringComparison.OrdinalIgnoreCase))
                    {
                        var installPath = subKey.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            var spineCom = Path.Combine(installPath, "Spine.com");
                            if (File.Exists(spineCom)) return Path.GetFullPath(spineCom);
                        }
                    }
                }
            }
        }
        catch { }

        // Common install paths
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Spine", "Spine.com"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Spine", "Spine.com"),
            @"C:\Program Files\Spine\Spine.com",
            @"C:\Program Files (x86)\Spine\Spine.com",
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public async Task<CliResult> RunAsync(string args)
    {
        if (!IsValid)
            return new CliResult { ExitCode = -1, Error = "Spine.com 路径未设置或文件不存在。" };

        try
        {
            var psi = new ProcessStartInfo(SpinePath, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };

            var process = new Process { StartInfo = psi };
            CliResult result;
            try
            {
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                process.WaitForExit();
                // Small delay to ensure file handles are released by Spine.com
                await Task.Delay(200);

                result = new CliResult
                {
                    ExitCode = process.ExitCode,
                    Output = await outputTask,
                    Error = await errorTask
                };
            }
            finally
            {
                process.Close();
                process.Dispose();
            }

            OperationLogger.Info($"SpineCliService.Run: exit={result.ExitCode} args={args}");
            if (!result.Success)
                OperationLogger.Error($"SpineCliService.Run: {result.Error}");

            return result;
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"SpineCliService.Run: exception: {ex.Message}");
            return new CliResult { ExitCode = -1, Error = ex.Message };
        }
    }

    public Task<CliResult> Export(string project, string outputDir, string exportConfig)
    {
        return RunAsync($"-i \"{project}\" -o \"{outputDir}\" -e \"{exportConfig}\"");
    }

    public Task<CliResult> ExportDefault(string project, string outputDir, string exportType = "json+pack")
    {
        return RunAsync($"-i \"{project}\" -o \"{outputDir}\" -e {exportType}");
    }

    public Task<CliResult> Pack(string project, string outputDir, string packName)
    {
        return RunAsync($"-i \"{project}\" -o \"{outputDir}\" -p \"{packName}\"");
    }

    public Task<CliResult> ImportMerge(string source, string target)
    {
        return RunAsync($"-i \"{source}\" -o \"{target}\" -r");
    }

    public Task<CliResult> ImportToTemp(string source, string tempOutput)
    {
        return RunAsync($"-i \"{source}\" -o \"{tempOutput}\" -r");
    }

    public Task<CliResult> UpdateVersion(string project, string version, string outputPath)
    {
        return RunAsync($"-i \"{project}\" --update {version} -o \"{outputPath}\"");
    }
}
