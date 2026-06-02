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
    public string? DetectFromRegistry()
    {
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

        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Spine", "Spine.com"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Spine", "Spine.com"),
            @"C:\Program Files\Spine\Spine.com",
            @"C:\Program Files (x86)\Spine\Spine.com",
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public async Task<CliResult> RunAsync(IEnumerable<string> args, CancellationToken cancellationToken = default)
    {
        if (!IsValid)
            return new CliResult { ExitCode = -1, Error = "Spine.com 路径未设置或文件不存在。" };

        var argList = args.ToList();
        try
        {
            var psi = new ProcessStartInfo(SpinePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };
            foreach (var arg in argList)
                psi.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = psi };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await Task.Delay(200, cancellationToken);

            var result = new CliResult
            {
                ExitCode = process.ExitCode,
                Output = await outputTask,
                Error = await errorTask
            };

            OperationLogger.Info($"SpineCliService.Run: exit={result.ExitCode} args={FormatArgs(argList)}");
            if (!result.Success)
                OperationLogger.Error($"SpineCliService.Run: {result.Error}");

            return result;
        }
        catch (OperationCanceledException)
        {
            OperationLogger.Warn($"SpineCliService.Run: cancelled args={FormatArgs(argList)}");
            return new CliResult { ExitCode = -1, Error = "操作已取消。" };
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"SpineCliService.Run: exception: {ex.Message}");
            return new CliResult { ExitCode = -1, Error = ex.Message };
        }
    }

    public Task<CliResult> Export(string project, string outputDir, string exportConfig, CancellationToken ct = default)
    {
        return RunAsync(["-i", project, "-o", outputDir, "-e", exportConfig], ct);
    }

    public Task<CliResult> ExportDefault(string project, string outputDir, string exportType = "json+pack", CancellationToken ct = default)
    {
        return RunAsync(["-i", project, "-o", outputDir, "-e", exportType], ct);
    }

    public Task<CliResult> Pack(string project, string outputDir, string packName, CancellationToken ct = default)
    {
        return RunAsync(["-i", project, "-o", outputDir, "-p", packName], ct);
    }

    public Task<CliResult> ImportMerge(string source, string target, CancellationToken ct = default)
    {
        return RunAsync(["-i", source, "-o", target, "-r"], ct);
    }

    public Task<CliResult> ImportToTemp(string source, string tempOutput, CancellationToken ct = default)
    {
        return RunAsync(["-i", source, "-o", tempOutput, "-r"], ct);
    }

    public Task<CliResult> UpdateVersion(string project, string version, string outputPath, CancellationToken ct = default)
    {
        return RunAsync(["-i", project, "--update", version, "-o", outputPath], ct);
    }

    public async Task<SpineProjectInfo> GetProjectInfo(string path, CancellationToken ct = default)
    {
        var info = new SpineProjectInfo();
        var result = await RunAsync(["--ignore-unknown-args", "-i", path], ct);
        if (!result.Success) return info;

        var output = result.Output;
        if (string.IsNullOrEmpty(output)) output = result.Error;

        var versionMatch = System.Text.RegularExpressions.Regex.Match(output,
            @"(\d+\.\d+(?:\.\d+)?)");
        if (versionMatch.Success)
            info.Version = versionMatch.Groups[1].Value;

        var skelMatches = System.Text.RegularExpressions.Regex.Matches(output,
            @"(?:Skeleton|skeleton)[:\s]+""?([^""\r\n]+)""?");
        foreach (System.Text.RegularExpressions.Match m in skelMatches)
            info.SkeletonNames.Add(m.Groups[1].Value.Trim());

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("Animations", StringComparison.OrdinalIgnoreCase)) continue;

            var afterColon = trimmed;
            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx >= 0) afterColon = trimmed[(colonIdx + 1)..].Trim();

            if (i + 1 < lines.Length && lines[i + 1].Length > 0 && char.IsWhiteSpace(lines[i + 1][0]))
            {
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
                var names = afterColon
                    .Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                info.AnimationNames.AddRange(names);
            }
            break;
        }

        return info;
    }

    public Task<CliResult> MergeSkeleton(string source, string target,
        string? fromName = null, string? toName = null, string? version = null, CancellationToken ct = default)
    {
        var args = new List<string> { "--ignore-unknown-args" };
        if (!string.IsNullOrEmpty(version)) { args.Add("-u"); args.Add(version); }
        args.AddRange(["-i", source, "-o", target]);
        if (!string.IsNullOrEmpty(fromName)) { args.Add("--from"); args.Add(fromName); }
        if (!string.IsNullOrEmpty(toName)) { args.Add("--to"); args.Add(toName); }
        args.AddRange(["--merge", "-r"]);
        return RunAsync(args, ct);
    }

    public Task<CliResult> ImportAnimations(string source, string target,
        List<string> animNames, string? version = null, CancellationToken ct = default)
    {
        var args = new List<string> { "--ignore-unknown-args" };
        if (!string.IsNullOrEmpty(version)) { args.Add("-u"); args.Add(version); }
        args.AddRange(["-i", source, "-o", target]);
        foreach (var name in animNames)
            args.AddRange(["-a", name]);
        args.Add("-r");
        return RunAsync(args, ct);
    }

    private static string FormatArgs(IEnumerable<string> args)
    {
        return string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
    }
}
