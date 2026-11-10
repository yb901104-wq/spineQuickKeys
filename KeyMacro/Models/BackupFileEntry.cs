#nullable enable
namespace KeyMacro.Models;

/// <summary>
/// 「原地复制备份」列表项。纯内存数据，不代表任何磁盘写入意图。
/// 勾选状态由 InplaceBackupPanel 以 FullPath（OrdinalIgnoreCase）为键维护。
/// </summary>
public sealed class BackupFileEntry
{
    /// <summary>绝对全路径，勾选态唯一键。</summary>
    public string FullPath { get; init; } = "";

    public string FileName => Path.GetFileName(FullPath);

    public string Extension => Path.GetExtension(FullPath).ToLowerInvariant();

    /// <summary>相对所选根目录的所在目录（用于区分不同目录下的同名文件）。</summary>
    public string Directory { get; init; } = "";

    public long Size { get; init; }

    /// <summary>数据层勾选态（UI 显示与数据解耦）。</summary>
    public bool Checked { get; set; }

    // ── AUD-038 批量动画选择：纯内存展示元数据 ──
    // 不写入 config.json / PathHistory，不参与复制与备份执行集合，不产生任何磁盘写入。
    /// <summary>该文件自身拥有的动画名列表（本轮读取结果；读取失败为空列表）。</summary>
    public List<string> AvailableAnimations { get; set; } = [];

    /// <summary>本轮实际写入的动画 = 批量勾选集合 ∩ AvailableAnimations。</summary>
    public List<string> SelectedAnimations { get; set; } = [];

    /// <summary>本轮存在「勾选了但该文件没有」的动画；仅用于整行标红提醒，不是执行门槛。</summary>
    public bool HasMissingAnimations { get; set; }

    public string SizeText => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024L * 1024 => $"{Size / 1024.0:0.#} KB",
        < 1024L * 1024 * 1024 => $"{Size / (1024.0 * 1024):0.#} MB",
        _ => $"{Size / (1024.0 * 1024 * 1024):0.##} GB"
    };

    public string ExtensionText => string.IsNullOrEmpty(Extension) ? "（无扩展名）" : Extension;
}

/// <summary>
/// 一次备份任务的结果明细。四类计数独立维护，不照抄 BatchCopyService 的单一 done 计数。
/// </summary>
public sealed class BackupResult
{
    public int Succeeded { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int Cancelled { get; set; }

    public List<(string Source, string Reason)> Failures { get; } = [];
    public List<string> SkippedTargets { get; } = [];

    /// <summary>true 表示服务层不变式断言失败，整批拒绝执行，零磁盘写入。</summary>
    public bool Rejected { get; set; }

    public string RejectReason { get; set; } = "";

    public string Summary => Rejected
        ? $"已拒绝执行：{RejectReason}（零磁盘写入）"
        : $"完成：成功 {Succeeded} / 跳过 {Skipped} / 失败 {Failed} / 取消 {Cancelled}";

    public string DetailText()
    {
        var lines = new List<string> { Summary };
        if (Rejected)
            lines.Add(RejectReason);
        if (SkippedTargets.Count > 0)
        {
            lines.Add("── 跳过清单 ──");
            lines.AddRange(SkippedTargets.Take(200));
            if (SkippedTargets.Count > 200)
                lines.Add($"… 另有 {SkippedTargets.Count - 200} 条，详见日志");
        }
        if (Failures.Count > 0)
        {
            lines.Add("── 失败清单 ──");
            lines.AddRange(Failures.Take(200).Select(f => $"{f.Source}  —  {f.Reason}"));
            if (Failures.Count > 200)
                lines.Add($"… 另有 {Failures.Count - 200} 条，详见日志");
        }
        if (!Rejected)
            lines.Add("注：失败或取消时不删除任何文件；可能留下的不完整副本由你自行处理。");
        return string.Join(Environment.NewLine, lines);
    }
}
