namespace KeyMacro.Models;

/// <summary>AUD-040：批量导出页「导出配置」的单选态，用于预设保存与恢复。</summary>
public enum ExportConfigSelection
{
    Finish = 0,
    Demotion = 1,
    Other = 2
}

/// <summary>
/// AUD-040：导出预设。持久化在 %APPDATA%\KeyMacro\export_presets.json
/// （用户所说的「本地软件数据文档」），与 config.json / path_history.json 相互独立。
/// 保存的是路径快照而非 SpineCliEntry 对象；纯数据模型，不持有任何 UI 引用。
/// </summary>
public class ExportPreset
{
    /// <summary>预设名，列表显示与唯一键（忽略大小写）。</summary>
    public string Name { get; set; } = "";

    /// <summary>源目录，恢复到 _txtSourceDir。</summary>
    public string SourceDir { get; set; } = "";

    /// <summary>输出目录，恢复到 _txtOutputDir。</summary>
    public string OutputDir { get; set; } = "";

    /// <summary>文件列表全部文件的完整路径（按保存时顺序）。</summary>
    public List<string> FilePaths { get; set; } = [];

    /// <summary>保存时选中的导出配置单选项。</summary>
    public ExportConfigSelection ConfigSelection { get; set; } = ExportConfigSelection.Finish;

    /// <summary>「其他」手填的配置名，仅在 ConfigSelection = Other 时使用。</summary>
    public string OtherConfigName { get; set; } = "";

    /// <summary>创建时间，仅用于 tooltip 展示。</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
