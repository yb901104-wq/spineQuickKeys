using System.Text;
using System.Text.RegularExpressions;
using KeyMacro.Models;

namespace KeyMacro.Services;

public sealed record ScanProgress(int Done, int Total, string Current);

/// <summary>
/// 导出文件发现与三件套配对。全程只读：不复制、不改名、不写回、不在源目录生成文件。
///
/// 同时兼容两类导出：
///   · Spine 原生：hero.skel / hero.json + hero.atlas + hero.png
///   · Unity 附加后缀（spine-unity 常用）：hero.skel.bytes / hero.json.txt
///     + hero.atlas.txt + hero.png（页图偶尔也带 .bytes）
/// 用户无需为了本工具额外导出一份不带后缀的文件。
/// </summary>
public static class ExportPreviewScanner
{
    private static readonly HashSet<string> ExcludedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "export.json", "finish.export.json", "demotion.export.json",
        "skin.json", "config.json", "package.json", "settings.json",
        "virtual_layout.json", "export_presets.json", "export_preview_settings.json"
    };

    private static readonly string[] ExcludedSuffixes =
        [".export.json", ".annotations.json"];

    /// <summary>主文件后缀表，长后缀必须排前面；Binary 表示按 .skel 二进制解析</summary>
    private static readonly (string Suffix, bool Binary)[] SkeletonSuffixes =
    [
        (".skel.bytes", true),
        (".json.bytes", false),
        (".json.txt", false),
        (".skel", true),
        (".json", false)
    ];

    /// <summary>atlas 后缀表，长后缀优先</summary>
    private static readonly string[] AtlasSuffixes = [".atlas.txt", ".atlas"];

    private static readonly string[] PageExtensions = [".png", ".jpg", ".jpeg", ".webp"];

    private static readonly Regex VersionRegex = new(
        @"\b(\d+\.\d+(?:\.\d+)?)(?:-beta)?\b", RegexOptions.Compiled);

    private static readonly Regex SpineVersionInJson = new(
        @"""spine""\s*:\s*""([^""\s]+)""", RegexOptions.Compiled);

    private const int MaxFiles = 30000;
    private const int ProbeBytes = 1024 * 1024;

    /// <summary>
    /// 识别导出主文件并给出「逻辑基名 + 格式」。
    /// 复合后缀（Unity）与原生后缀走同一条路径，配对逻辑因此与磁盘扩展名解耦。
    /// </summary>
    public static bool TryClassifySkeleton(string path, out bool binary, out string logicalStem)
    {
        binary = false;
        logicalStem = "";
        var name = Path.GetFileName(path);
        if (name.Length == 0 || name.StartsWith('.')) return false;
        if (ExcludedFileNames.Contains(name)) return false;
        foreach (var suffix in ExcludedSuffixes)
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return false;

        foreach (var (suffix, isBinary) in SkeletonSuffixes)
        {
            if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
            var stem = name[..^suffix.Length];
            if (stem.Length == 0) return false;
            binary = isBinary;
            logicalStem = stem;
            return true;
        }
        return false;
    }

    public static bool IsMainFileCandidate(string path) => TryClassifySkeleton(path, out _, out _);

    /// <summary>atlas 文件（含 Unity 的 .atlas.txt），用于目录内兜底枚举</summary>
    public static bool IsAtlasFile(string path)
    {
        var name = Path.GetFileName(path);
        foreach (var suffix in AtlasSuffixes)
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    /// 合并「直接选中的文件」与「选中目录（可选递归）」的扫描结果，去重后逐项配对。
    /// </summary>
    public static async Task<List<ExportPreviewEntry>> ScanAsync(
        IEnumerable<string> files,
        IEnumerable<string> directories,
        bool includeSubfolders,
        IReadOnlySet<string> availableVersions,
        IProgress<ScanProgress>? progress,
        CancellationToken ct,
        int maxFiles = MaxFiles)
    {
        var candidates = new List<string>();

        foreach (var f in files)
            if (!string.IsNullOrWhiteSpace(f) && File.Exists(f) && IsMainFileCandidate(f))
                candidates.Add(Path.GetFullPath(f));

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = includeSubfolders,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System
        };

        foreach (var dir in directories)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(dir, "*", options))
                {
                    ct.ThrowIfCancellationRequested();
                    if (!IsMainFileCandidate(f)) continue;
                    candidates.Add(Path.GetFullPath(f));
                    if (candidates.Count >= maxFiles) break;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                OperationLogger.Warn($"[ExportPreview] 目录跳过 {dir}: {ex.Message}");
            }
            if (candidates.Count >= maxFiles) break;
        }

        var unique = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in candidates) unique.TryAdd(c, c);

        var result = new List<ExportPreviewEntry>(unique.Count);
        var done = 0;
        foreach (var path in unique.Keys.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            var entry = Inspect(path, availableVersions);
            result.Add(entry);
            done++;
            if (done % 20 == 0 || done == unique.Count)
                progress?.Report(new ScanProgress(done, unique.Count, Path.GetFileName(path)));
            if (done % 64 == 0) await Task.Yield();
        }

        OperationLogger.Info($"[ExportPreview] 扫描完成：候选 {unique.Count} 项，就绪 {result.Count(x => x.Status == ExportPreviewStatus.Ready)} 项");
        return result;
    }

    /// <summary>单个主文件的结构探测 + 三件套配对 + 状态判定</summary>
    public static ExportPreviewEntry Inspect(string path, IReadOnlySet<string>? availableVersions = null)
    {
        var entry = new ExportPreviewEntry { FullPath = Path.GetFullPath(path) };

        if (!TryClassifySkeleton(entry.FullPath, out var isBinary, out var logicalStem))
        {
            entry.Status = ExportPreviewStatus.InvalidData;
            entry.StatusDetail = "不是可识别的 Spine 导出主文件";
            return entry;
        }

        entry.IsBinarySkeleton = isBinary;
        entry.LogicalStem = logicalStem;
        entry.Format = isBinary ? "二进制" : "JSON";

        if (!TryReadVersion(entry.FullPath, isBinary, out var version, out var reason))
        {
            entry.Status = ExportPreviewStatus.InvalidData;
            entry.StatusDetail = reason;
            return entry;
        }

        entry.SpineVersion = version;
        entry.VersionLine = ExtractVersionLine(version);
        PairFiles(entry);

        // "*" 表示 BYO 根目录放了一份通用 player，任何版本线都允许尝试。
        if (availableVersions is { Count: > 0 }
            && !availableVersions.Contains("*")
            && entry.VersionLine.Length > 0
            && !availableVersions.Contains(entry.VersionLine))
        {
            entry.Status = ExportPreviewStatus.VersionUnsupported;
            entry.StatusDetail = $"导出版本 {version}（版本线 {entry.VersionLine}）无内置 runtime，且未配置对应的 BYO 资源目录";
        }
        return entry;
    }

    private static string ExtractVersionLine(string version)
    {
        var parts = version.Split('.');
        return parts.Length >= 2 && int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _)
            ? parts[0] + "." + parts[1]
            : "";
    }

    /// <summary>只读文件头，提取 Spine 版本串；JSON 同时做 skeleton 结构特征校验</summary>
    private static bool TryReadVersion(string path, bool isBinary, out string version, out string reason)
    {
        version = "";
        reason = "";
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length == 0)
            {
                reason = "文件不存在或为空";
                return false;
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var len = (int)Math.Min(info.Length, ProbeBytes);
            var buffer = new byte[len];
            var read = fs.Read(buffer, 0, len);
            var text = Encoding.UTF8.GetString(buffer, 0, read);

            var match = isBinary
                ? VersionRegex.Match(text)
                : SpineVersionInJson.Match(text);

            if (!match.Success)
            {
                reason = isBinary
                    ? "无法从 .skel 文件头读取版本串（可能不是 Spine 二进制导出）"
                    : "JSON 头部未找到 skeleton.spine 版本字段";
                return false;
            }

            if (!isBinary)
            {
                // 结构特征：必须是 skeleton 数据，而不是导出配置 / 注解等普通 JSON
                if (!text.Contains("\"skeleton\"", StringComparison.Ordinal)
                    || !text.Contains("\"bones\"", StringComparison.Ordinal))
                {
                    reason = "JSON 缺少 skeleton / bones 结构，判定为非骨骼数据";
                    return false;
                }
            }

            version = match.Groups[1].Value;
            if (ExtractVersionLine(version).Length == 0)
            {
                reason = "版本串格式无法识别：" + version;
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            reason = "读取失败：" + ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 按逻辑基名找 atlas，再解析并校验页图。
    /// 匹配顺序：精确基名（原生 / Unity 后缀都试）→ 去常见中缀后缀的基名 → 目录内唯一 atlas → PMA 变体组。
    /// </summary>
    public static void PairFiles(ExportPreviewEntry entry)
    {
        var dir = entry.DirectoryName;
        var stem = entry.LogicalStem.Length > 0 ? entry.LogicalStem : entry.Stem;
        var baseStem = StripKnownSuffix(stem);

        var candidates = new List<string>();
        foreach (var s in new[] { stem, baseStem }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            // pma 变体放后面，优先选普通 atlas
            candidates.Add(Path.Combine(dir, s + ".atlas"));
            candidates.Add(Path.Combine(dir, s + ".atlas.txt"));
            candidates.Add(Path.Combine(dir, s + "-pma.atlas"));
            candidates.Add(Path.Combine(dir, s + "-pma.atlas.txt"));
        }

        string? atlas = candidates.FirstOrDefault(File.Exists);
        var fallbackNote = "";
        if (atlas == null)
        {
            try
            {
                var all = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
                    .Where(IsAtlasFile)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                if (all.Count == 1) atlas = all[0];
                else if (all.Count > 1)
                {
                    // 官方示例与真实工程常见：数据文件名带 -pro/-ess，atlas 用基名，
                    // 且同一套资源同时存在普通与 -pma 两个 atlas（Unity 下还可能再加 .txt）。
                    var groups = all.Select(AtlasBaseName)
                        .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    if (groups.Count == 1)
                    {
                        atlas = all.FirstOrDefault(p => !AtlasIsPma(p)) ?? all[0];
                        fallbackNote = "按目录内 PMA 变体组兜底匹配";
                    }
                    else
                    {
                        var match = all.FirstOrDefault(p =>
                            string.Equals(AtlasBaseName(p), baseStem, StringComparison.OrdinalIgnoreCase)
                            && !AtlasIsPma(p));
                        if (match != null)
                        {
                            atlas = match;
                            fallbackNote = "按主文件基名兜底匹配";
                        }
                    }
                }
            }
            catch { /* 目录不可读时保持未找到 */ }
        }

        if (atlas == null)
        {
            entry.Status = ExportPreviewStatus.MissingAtlas;
            entry.StatusDetail = "同目录未找到同名 .atlas / .atlas.txt；官方 Skeleton Viewer 会显示半透明白块";
            return;
        }

        entry.AtlasPath = atlas;
        ParseAtlasPages(entry, atlas);

        if (entry.MissingPages.Count > 0)
        {
            entry.Status = ExportPreviewStatus.MissingPages;
            entry.StatusDetail = "缺失页图：" + string.Join("、", entry.MissingPages.Take(4))
                                 + (entry.MissingPages.Count > 4 ? " …" : "");
            return;
        }

        if (entry.PageImages.Count == 0)
        {
            entry.Status = ExportPreviewStatus.InvalidData;
            entry.StatusDetail = "atlas 中未解析到任何页图声明";
            return;
        }

        entry.Status = ExportPreviewStatus.Ready;
        entry.StatusDetail = fallbackNote;
    }

    /// <summary>去掉 .atlas.txt / .atlas 后的基名（保留 -pma 供判断）</summary>
    private static string AtlasStem(string path)
    {
        var name = Path.GetFileName(path);
        foreach (var suffix in AtlasSuffixes)
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name[..^suffix.Length];
        return Path.GetFileNameWithoutExtension(path);
    }

    private static bool AtlasIsPma(string path) =>
        AtlasStem(path).EndsWith("-pma", StringComparison.OrdinalIgnoreCase);

    private static string AtlasBaseName(string path)
    {
        var name = AtlasStem(path);
        return name.EndsWith("-pma", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }

    private static string StripKnownSuffix(string stem)
    {
        foreach (var suffix in new[] { "-pro", "-ess", "-pma", "-skel", "-export" })
            if (stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                && stem.Length > suffix.Length)
                return stem[..^suffix.Length];
        return stem;
    }

    /// <summary>
    /// 解析 atlas 页图声明，并登记「URL 名 → 磁盘实际路径」。
    /// Unity 工程可能把页图也加上 .bytes / .txt，因此按候选后缀逐一探测。
    /// </summary>
    private static void ParseAtlasPages(ExportPreviewEntry entry, string atlasPath)
    {
        try
        {
            var atlasDir = Path.GetDirectoryName(atlasPath) ?? "";
            foreach (var raw in File.ReadLines(atlasPath))
            {
                if (raw.Length == 0 || char.IsWhiteSpace(raw[0])) continue;
                var line = raw.Trim();
                if (!PageExtensions.Any(e => line.EndsWith(e, StringComparison.OrdinalIgnoreCase))) continue;

                var normalized = line.Replace('\\', '/');
                entry.PageImages.Add(normalized);
                var resolved = ResolvePageFile(atlasDir, normalized);
                if (resolved == null) entry.MissingPages.Add(normalized);
                else entry.PageImageMap[normalized] = resolved;
            }
        }
        catch (Exception ex)
        {
            entry.StatusDetail = "atlas 解析失败：" + ex.Message;
        }
    }

    private static string? ResolvePageFile(string atlasDir, string relative)
    {
        foreach (var candidate in new[] { relative, relative + ".bytes", relative + ".txt" })
        {
            try
            {
                var full = Path.GetFullPath(Path.Combine(atlasDir, candidate.Replace('/', Path.DirectorySeparatorChar)));
                if (File.Exists(full)) return full;
            }
            catch { /* 非法路径片段，继续下一个候选 */ }
        }
        return null;
    }
}
