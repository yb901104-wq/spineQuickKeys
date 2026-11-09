#nullable enable
using KeyMacro.Models;

namespace KeyMacro.Services;

/// <summary>
/// 「原地复制备份」只读扫描 + 原地复制引擎。
///
/// 安全不变式（design.md 6.6，优先级高于一切其它规则）：
/// INV-1 全功能对磁盘的唯一写入是 File.Copy(src, dest, overwrite:false)。
/// INV-2 后缀必带，服务层二次校验，生成名 != 源文件名。
/// INV-3 目标恒在源文件原目录，且位于所选根目录内。
/// INV-4 目标已存在一律跳过，overwrite 参数恒为 false。
/// INV-5 目标不等于任何源文件。
/// INV-6 源文件只读（仅由 File.Copy 以读方式打开）。
/// INV-7 源目录零副作用（不在源目录创建目录/日志/清单/临时文件）。
/// INV-8 失败与取消零删除。
///
/// 本文件禁止出现：File.Delete / File.Move / File.Replace / File.Create /
/// File.WriteAll* / File.SetAttributes / File.SetLastWriteTime /
/// Directory.CreateDirectory / Directory.Delete / overwrite:true。
/// 日志统一交给 OperationLogger（只写 %APPDATA%\KeyMacro\logs\）。
/// </summary>
public class InplaceBackupService
{
    /// <summary>扫描规模上限（design.md 9.12 建议默认）。</summary>
    public const int ScanLimit = 20000;

    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts is not null;

    public event Action<string>? ProgressChanged;
    public event Action<string, int, int>? ProgressReported;
    public event Action<string>? Completed;
    public event Action<string>? ErrorOccurred;

    public void Cancel()
    {
        try { _cts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    // ══════════════════════════════════════════════════════════
    //  后缀校验 / 目标命名（UI 与服务层共用同一份实现）
    // ══════════════════════════════════════════════════════════

    private static readonly char[] ExplicitIllegalChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// UI 与服务层各调用一次；服务层才是安全边界（INV-2）。
    /// </summary>
    public static bool ValidateSuffix(string? suffix, out string reason)
    {
        reason = "";
        if (string.IsNullOrEmpty(suffix) || string.IsNullOrWhiteSpace(suffix))
        {
            reason = "备份后缀不能为空或全为空格";
            return false;
        }
        if (suffix.Length > 32)
        {
            reason = "备份后缀不得超过 32 个字符";
            return false;
        }
        if (suffix is "." or "..")
        {
            reason = "备份后缀不能是 . 或 ..";
            return false;
        }
        foreach (var c in suffix)
        {
            if (char.IsControl(c) || ExplicitIllegalChars.Contains(c) || Path.GetInvalidFileNameChars().Contains(c))
            {
                reason = $"备份后缀含非法字符（{c}）";
                return false;
            }
        }

        // 用样例文件走一遍完整生成规则，确认「必带后缀」「不等于源名」「路径合法」
        const string sample = "sample.png";
        string sampleDest;
        try
        {
            sampleDest = Path.GetFileName(BuildDestination(sample, suffix));
            _ = Path.GetFullPath(Path.Combine(Path.GetTempPath(), sampleDest));
        }
        catch (Exception ex)
        {
            reason = $"备份后缀无法生成合法文件名：{ex.Message}";
            return false;
        }

        if (!sampleDest.Contains(suffix, StringComparison.Ordinal))
        {
            reason = "生成的文件名未包含备份后缀";
            return false;
        }
        if (string.Equals(sampleDest, sample, StringComparison.OrdinalIgnoreCase))
        {
            reason = "生成的文件名与源文件名相同";
            return false;
        }
        var stem = sampleDest.Split('.')[0];
        if (ReservedNames.Contains(stem))
        {
            reason = "生成的文件名命中 Windows 保留设备名";
            return false;
        }
        return true;
    }

    /// <summary>
    /// design.md 6.2：目标 = 原目录 + 原文件名(不含扩展名) + 后缀 + 原扩展名。
    /// </summary>
    public static string BuildDestination(string source, string suffix)
    {
        return Path.Combine(
            Path.GetDirectoryName(source) ?? "",
            Path.GetFileNameWithoutExtension(source) + suffix + Path.GetExtension(source));
    }

    // ══════════════════════════════════════════════════════════
    //  只读扫描
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 只读递归扫描多个根目录，返回纯内存数据；不创建/删除/修改任何文件。
    /// 显式逐目录遍历以跳过重解析点，避免目录环与越出所选目录。
    /// </summary>
    public Task<IReadOnlyList<BackupFileEntry>> ScanAsync(
        IReadOnlyList<string> roots, CancellationToken token)
    {
        var normalized = NormalizeRoots(roots);
        return Task.Run(() =>
        {
            var results = new List<BackupFileEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                // 隐藏 / 普通系统文件不额外过滤（design.md 9.7）
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            var limitReached = false;
            foreach (var root in normalized)
            {
                if (limitReached) break;
                if (IsReparsePoint(root))
                {
                    OperationLogger.Warn($"InplaceBackup.Scan: skip reparse-point root: {root}");
                    ProgressChanged?.Invoke($"已跳过重解析点目录：{root}");
                    continue;
                }

                var stack = new Stack<string>();
                stack.Push(root);
                while (stack.Count > 0)
                {
                    token.ThrowIfCancellationRequested();
                    if (limitReached) break;

                    var dir = stack.Pop();
                    if (IsReparsePoint(dir))
                        continue;

                    try
                    {
                        foreach (var sub in Directory.EnumerateDirectories(dir, "*", options))
                            stack.Push(sub);

                        foreach (var file in Directory.EnumerateFiles(dir, "*", options))
                        {
                            token.ThrowIfCancellationRequested();

                            var full = Path.GetFullPath(file);
                            if (!IsUnderAnyRoot(full, normalized))
                                continue;
                            if (!seen.Add(full))
                                continue;

                            if (results.Count >= ScanLimit)
                            {
                                limitReached = true;
                                OperationLogger.Warn($"InplaceBackup.Scan: reached limit {ScanLimit}");
                                ProgressChanged?.Invoke($"扫描已达 {ScanLimit} 个文件上限，已停止继续收集；可缩小目录范围后重试");
                                break;
                            }

                            long size = 0;
                            try { size = new FileInfo(full).Length; }
                            catch (Exception ex) { OperationLogger.Warn($"InplaceBackup.Scan: stat failed: {full} → {ex.Message}"); }

                            results.Add(new BackupFileEntry
                            {
                                FullPath = full,
                                Directory = Path.GetRelativePath(root, Path.GetDirectoryName(full) ?? root),
                                Size = size
                            });
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        // 单目录读取失败只跳过该目录，不中断整批
                        OperationLogger.Warn($"InplaceBackup.Scan: skip inaccessible dir: {dir} → {ex.Message}");
                    }
                }
            }

            // 默认排序：所在目录 → 文件名
            results.Sort((a, b) =>
            {
                var byDir = string.Compare(a.Directory, b.Directory, StringComparison.OrdinalIgnoreCase);
                return byDir != 0 ? byDir : string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
            });

            OperationLogger.Info($"InplaceBackup.Scan: {results.Count} files from {normalized.Length} roots{(limitReached ? " (limit reached)" : "")}");
            return (IReadOnlyList<BackupFileEntry>)results;
        }, token);
    }

    // ══════════════════════════════════════════════════════════
    //  原地复制（唯一写入点）
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// checkedFiles 是调用方按下按钮时冻结的快照；roots 用于 INV-3 目录断言。
    /// 入口同步执行全部不变式断言，任一失败整批拒绝、零磁盘写入。
    /// </summary>
    public async Task<BackupResult> BackupAsync(
        IReadOnlyList<string> checkedFiles,
        IReadOnlyList<string> roots,
        string suffix,
        CancellationToken token)
    {
        var result = new BackupResult();

        // ── INV-2 服务层断言（不信任 UI 校验）──
        if (!ValidateSuffix(suffix, out var suffixReason))
            return Reject(result, suffixReason);
        if (checkedFiles.Count == 0)
            return Reject(result, "没有勾选任何文件");

        string[] normalizedRoots;
        try { normalizedRoots = NormalizeRoots(roots); }
        catch (Exception ex) { return Reject(result, $"源根目录无效：{ex.Message}"); }
        if (normalizedRoots.Length == 0)
            return Reject(result, "没有有效的源根目录");

        // ── 只读预检：构建执行计划，此阶段绝不写磁盘 ──
        var sourceSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in checkedFiles)
        {
            string full;
            try { full = Path.GetFullPath(raw); }
            catch (Exception ex) { return Reject(result, $"源文件路径无效：{ex.Message}"); }
            sourceSet.Add(full);
        }

        var plan = new List<(string Src, string Dest)>();
        var destSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in checkedFiles)
        {
            string src;
            try { src = Path.GetFullPath(raw); }
            catch (Exception ex) { return Reject(result, $"源文件路径无效：{ex.Message}"); }

            // 扫描是快照：执行前只读复核，已消失的文件计入失败并继续
            if (!File.Exists(src))
            {
                result.Failed++;
                result.Failures.Add((src, "源文件已不存在"));
                OperationLogger.Warn($"InplaceBackup: source missing: {src}");
                continue;
            }

            string dest;
            try
            {
                dest = Path.GetFullPath(BuildDestination(src, suffix));
                _ = Path.GetFullPath(Path.GetDirectoryName(dest) ?? "");
            }
            catch (Exception ex) { return Reject(result, $"目标路径无效：{ex.Message}"); }

            // INV-2 逐文件：生成名必须带后缀且不等于源名
            if (!Path.GetFileName(dest).Contains(suffix, StringComparison.Ordinal))
                return Reject(result, $"生成名未包含后缀：{src}");
            if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase))
                return Reject(result, $"目标与源相同，已整批拒绝：{src}");

            // INV-3 目标恒在原目录、且位于所选根目录内
            var srcDir = Path.GetFullPath(Path.GetDirectoryName(src) ?? "");
            var destDir = Path.GetFullPath(Path.GetDirectoryName(dest) ?? "");
            if (!string.Equals(srcDir, destDir, StringComparison.OrdinalIgnoreCase))
                return Reject(result, $"目标不在源文件原目录：{src}");
            if (!IsUnderAnyRoot(dest, normalizedRoots))
                return Reject(result, $"目标越出所选根目录：{dest}");

            // INV-5 目标不得等于任一源文件
            if (sourceSet.Contains(dest))
            {
                MarkSkipped(result, dest, "目标与另一个源文件同名，自动跳过");
                continue;
            }

            // Windows 大小写不敏感：多个源映射到同一目标时只保留第一个
            if (!destSeen.Add(dest))
            {
                MarkSkipped(result, dest, "目标与本次其它文件重复（大小写碰撞），自动跳过");
                continue;
            }

            if (dest.Length > 250)
            {
                MarkSkipped(result, dest, "目标路径过长，自动跳过");
                continue;
            }

            // INV-4 预检：目标已存在（文件 / 目录 / 硬链接 / 重解析点）一律跳过，绝不覆盖
            if (File.Exists(dest) || Directory.Exists(dest))
            {
                MarkSkipped(result, dest, "目标已存在，自动跳过（不覆盖）");
                continue;
            }

            plan.Add((src, dest));
        }

        if (plan.Count == 0)
        {
            OperationLogger.Info($"InplaceBackup: nothing to copy (skipped {result.Skipped}, failed {result.Failed})");
            Completed?.Invoke(result.Summary);
            return result;
        }

        _cts = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
        OperationLogger.Info($"InplaceBackup: start {plan.Count} copies, suffix='{suffix}'");

        try
        {
            await Task.Run(() =>
            {
                var total = Math.Max(1, checkedFiles.Count);
                var done = 0;
                foreach (var (src, dest) in plan)
                {
                    if (linked.Token.IsCancellationRequested) break;
                    done++;

                    var message = $"备份: {Path.GetFileName(src)} → {Path.GetFileName(dest)}";
                    ProgressChanged?.Invoke($"{message} ({done}/{total})");
                    ProgressReported?.Invoke(message, done, total);

                    try
                    {
                        // INV-4 执行前二次只读复核，关闭预检到复制之间的时间窗
                        if (File.Exists(dest) || Directory.Exists(dest))
                        {
                            MarkSkipped(result, dest, "执行前发现目标已存在，自动跳过（不覆盖）");
                            continue;
                        }

                        // ══ INV-1：本功能对磁盘的唯一写入操作 ══
                        // 源仅以读方式打开；不覆盖、不删除、不移动、不改属性/时间戳。
                        File.Copy(src, dest, overwrite: false);
                        result.Succeeded++;
                    }
                    catch (Exception ex)
                    {
                        // INV-8：失败不删除任何文件，可能留下的不完整副本予以保留并列出
                        result.Failed++;
                        result.Failures.Add((src, ex.Message));
                        OperationLogger.Warn($"InplaceBackup: copy failed: {src} → {dest} : {ex.Message}");
                        ErrorOccurred?.Invoke($"备份失败：{src}\n{ex.Message}");
                    }
                }

                var cancelled = linked.Token.IsCancellationRequested;
                result.Cancelled = cancelled ? Math.Max(0, plan.Count - done) : 0;

                foreach (var target in result.SkippedTargets)
                    OperationLogger.Info($"InplaceBackup: skipped: {target}");

                OperationLogger.Info($"InplaceBackup: {result.Summary}");
                if (cancelled)
                    OperationLogger.Info("InplaceBackup: cancelled, no files were deleted");
            }, linked.Token);
        }
        catch (OperationCanceledException)
        {
            // INV-8：取消同样零删除
            result.Cancelled = Math.Max(result.Cancelled, plan.Count);
            OperationLogger.Info("InplaceBackup: cancelled, no files were deleted");
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"InplaceBackup: unexpected failure: {ex.Message}");
            ErrorOccurred?.Invoke($"备份出错：{ex.Message}");
        }
        finally
        {
            Completed?.Invoke(result.Summary);
            _cts?.Dispose();
            _cts = null;
        }

        return result;
    }

    // ══════════════════════════════════════════════════════════
    //  内部工具（全部只读）
    // ══════════════════════════════════════════════════════════

    private static BackupResult Reject(BackupResult result, string reason)
    {
        result.Rejected = true;
        result.RejectReason = reason;
        OperationLogger.Warn($"InplaceBackup: batch rejected (zero disk writes): {reason}");
        return result;
    }

    private static void MarkSkipped(BackupResult result, string dest, string reason)
    {
        result.Skipped++;
        result.SkippedTargets.Add($"{dest}  —  {reason}");
    }

    private static string[] NormalizeRoots(IReadOnlyList<string> roots)
    {
        return roots
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => Path.GetFullPath(r.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists)
            .ToArray();
    }

    private static bool IsReparsePoint(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (Exception ex)
        {
            OperationLogger.Warn($"InplaceBackup.Scan: cannot read attributes: {path} → {ex.Message}");
            return true; // 读不到属性时保守跳过
        }
    }

    private static bool IsUnderAnyRoot(string fullPath, IReadOnlyList<string> roots)
    {
        foreach (var root in roots)
        {
            var r = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (fullPath.Equals(r, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
