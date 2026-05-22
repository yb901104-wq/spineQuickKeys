#nullable enable
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

    // ── Spine 4.3+ 实验功能 ──

    /// <summary>Run Spine -i to collect project version, skeleton names, and animation names.</summary>
    public async Task<SpineProjectInfo> GetProjectInfo(string path)
    {
        var info = new SpineProjectInfo();
        var result = await RunAsync($"--ignore-unknown-args -i \"{path}\"");
        if (!result.Success) return info;

        var output = result.Output;
        if (string.IsNullOrEmpty(output)) output = result.Error;

        // Parse version line (e.g. "Spine 4.3.06" or "version: 4.3.06")
        var versionMatch = System.Text.RegularExpressions.Regex.Match(output,
            @"(\d+\.\d+(?:\.\d+)?)");
        if (versionMatch.Success)
            info.Version = versionMatch.Groups[1].Value;

        // Parse skeleton names (lines containing "Skeleton:" or quoted names)
        var skelMatches = System.Text.RegularExpressions.Regex.Matches(output,
            @"(?:Skeleton|skeleton)[:\s]+""?([^""\r\n]+)""?");
        foreach (System.Text.RegularExpressions.Match m in skelMatches)
            info.SkeletonNames.Add(m.Groups[1].Value.Trim());

        // Parse animation names from Spine -i output
        // Format: "Animations: <count>" then indented lines with names:
        //   Animations: 1
        //     walk
        // Or single line: "Animations: walk, run"
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("Animations", StringComparison.OrdinalIgnoreCase)) continue;

            var afterColon = trimmed;
            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx >= 0) afterColon = trimmed[(colonIdx + 1)..].Trim();

            // Check if next lines are indented (contain animation names)
            if (i + 1 < lines.Length && lines[i + 1].Length > 0 && char.IsWhiteSpace(lines[i + 1][0]))
            {
                // Collect indented lines below
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(lines[j])) break;
                    if (lines[j].Length > 0 && !char.IsWhiteSpace(lines[j][0])) break;
                    var name = lines[j].Trim();
                    if (!string.IsNullOrEmpty(name))
                        info.AnimationNames.Add(name);
                }
            }
            else if (!string.IsNullOrWhiteSpace(afterColon) && !afterColon.All(char.IsDigit))
            {
                // Fallback: names on same line
                var names = afterColon
                    .Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                info.AnimationNames.AddRange(names);
            }
            break;
        }

        return info;
    }

    /// <summary>Build --merge skeleton merge command.</summary>
    public Task<CliResult> MergeSkeleton(string source, string target,
        string? fromName = null, string? toName = null, string? version = null)
    {
        var versionArg = !string.IsNullOrEmpty(version) ? $"-u {version} " : "";
        var fromArg = !string.IsNullOrEmpty(fromName) ? $"--from \"{fromName}\" " : "";
        var toArg = !string.IsNullOrEmpty(toName) ? $"--to \"{toName}\" " : "";
        return RunAsync(
            $"--ignore-unknown-args {versionArg}-i \"{source}\" -o \"{target}\" {fromArg}{toArg}--merge -r");
    }

    /// <summary>Build -a animation import command with individual animation names.</summary>
    public Task<CliResult> ImportAnimations(string source, string target,
        List<string> animNames, string? version = null)
    {
        var versionArg = !string.IsNullOrEmpty(version) ? $"-u {version} " : "";
        var animArgs = animNames.Count > 0
            ? string.Join(" ", animNames.Select(n => $"-a \"{n}\"")) + " "
            : "";
        return RunAsync(
            $"--ignore-unknown-args {versionArg}-i \"{source}\" -o \"{target}\" {animArgs}-r");
    }
}
