using KeyMacro.Models;

namespace KeyMacro.Services;

public class BatchCopyService
{
    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts is not null;

    public event Action<string>? ProgressChanged;
    public event Action<string, int, int>? ProgressReported;
    public event Action<string>? Completed;
    public event Action<string>? ErrorOccurred;

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public async Task CopyFilesAsync(List<string> sourceFiles, List<string> targetDirs,
        Func<string, List<string>, CancellationToken, Task<ConflictAction>> onConflict)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            var total = sourceFiles.Count * targetDirs.Count;
            var done = 0;

            OperationLogger.Info($"BatchCopy: starting {sourceFiles.Count} files → {targetDirs.Count} targets");

            foreach (var targetDir in targetDirs)
            {
                if (token.IsCancellationRequested) break;

                // Create target directory if needed
                try
                {
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke($"创建目录失败: {targetDir}\n{ex.Message}");
                    continue;
                }

                // Check for conflicts in this target
                var conflicts = sourceFiles
                    .Where(f => File.Exists(Path.Combine(targetDir, Path.GetFileName(f))))
                    .Select(f => Path.GetFileName(f))
                    .ToList();

                var action = ConflictAction.Overwrite;
                if (conflicts.Count > 0)
                {
                    action = await onConflict(targetDir, conflicts, token);
                    if (action == ConflictAction.CancelAll)
                    {
                        Cancel();
                        break;
                    }
                    if (token.IsCancellationRequested) break;
                }

                var conflictSet = action == ConflictAction.Skip
                    ? conflicts.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    : [];

                // Copy files
                foreach (var srcFile in sourceFiles)
                {
                    if (token.IsCancellationRequested) break;

                    try
                    {
                        var fileName = Path.GetFileName(srcFile);
                        if (conflictSet.Contains(fileName))
                        {
                            done++;
                            ReportProgress($"跳过冲突文件: {fileName} → {targetDir}", done, total);
                            continue;
                        }

                        var destPath = Path.Combine(targetDir, fileName);
                        File.Copy(srcFile, destPath, overwrite: action == ConflictAction.Overwrite);
                        done++;
                        ReportProgress($"复制中: {fileName} → {targetDir}", done, total);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.Invoke($"复制失败: {srcFile} → {targetDir}\n{ex.Message}");
                        done++;
                    }
                }
            }

            if (token.IsCancellationRequested)
            {
                OperationLogger.Info($"BatchCopy: cancelled after {done}/{total} operations");
                Completed?.Invoke($"已取消（{done}/{total} 已完成）");
            }
            else
            {
                OperationLogger.Info($"BatchCopy: completed {done}/{total} operations");
                Completed?.Invoke($"复制完成（{done}/{total}）");
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"BatchCopy: failed: {ex.Message}");
            ErrorOccurred?.Invoke($"复制出错: {ex.Message}");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void ReportProgress(string message, int done, int total)
    {
        ProgressChanged?.Invoke($"{message} ({done}/{total})");
        ProgressReported?.Invoke(message, done, total);
    }
}

public enum ConflictAction
{
    Overwrite,
    Skip,
    CancelAll
}
