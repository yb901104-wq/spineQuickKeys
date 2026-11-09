namespace KeyMacro.Models;

public class PathHistory
{
    public List<string> PrefixHistory { get; set; } = [];
    public List<string> SuffixHistory { get; set; } = [];

    /// <summary>
    /// 「原地复制备份」页的备份后缀历史（最近 10 条）。
    /// 与 Tab1 的目标路径后缀历史分开存放，避免两个 Tab 互相污染；
    /// 旧版 config 缺少该字段时反序列化为空列表，既有字段结构不变。
    /// </summary>
    public List<string> BackupSuffixHistory { get; set; } = [];
}
