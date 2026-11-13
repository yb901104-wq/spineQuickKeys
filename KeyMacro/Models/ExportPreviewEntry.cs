namespace KeyMacro.Models;

/// <summary>
/// 一个可预览的 Spine 导出单元：主文件 + 配套 atlas + 全部页图。
/// 纯内存模型，不持久化；扫描与预览全程只读源文件。
/// </summary>
public enum ExportPreviewStatus
{
    Ready,
    MissingAtlas,
    MissingPages,
    VersionUnsupported,
    InvalidData
}

public sealed class ExportPreviewEntry
{
    public string FullPath { get; set; } = "";

    public string FileName => Path.GetFileName(FullPath);
    public string Stem => Path.GetFileNameWithoutExtension(FullPath);
    public string DirectoryName => Path.GetDirectoryName(FullPath) ?? "";

    /// <summary>JSON / 二进制</summary>
    public string Format { get; set; } = "";

    /// <summary>
    /// 是否为二进制 skeleton。由复合扩展名判定（.skel 与 .skel.bytes 都是二进制），
    /// 不能依赖 Path.GetExtension —— Unity 导出的 .skel.bytes 扩展名是 .bytes。
    /// </summary>
    public bool IsBinarySkeleton { get; set; }

    /// <summary>
    /// 去掉复合扩展名后的逻辑基名：hero.skel.bytes / hero.json.txt / hero.skel → hero。
    /// 用于与 atlas 配对，不再受 Path.GetFileNameWithoutExtension 影响。
    /// </summary>
    public string LogicalStem { get; set; } = "";

    /// <summary>
    /// 交给 Web Player 的规范化 URL 名。官方按 config.skeleton.endsWith(".json")
    /// 判定格式，因此无论磁盘上叫什么，URL 必须以 .json / .skel 结尾且不带 query。
    /// </summary>
    public string SkeletonUrlName => "skeleton" + (IsBinarySkeleton ? ".skel" : ".json");

    /// <summary>atlas 的规范化 URL 名（Web Player 只读文本内容，扩展名不影响解析）</summary>
    public string AtlasUrlName => "atlas.atlas";

    /// <summary>atlas 声明的页图名 → 磁盘实际绝对路径（支持 .png.bytes 等 Unity 别名）</summary>
    public Dictionary<string, string> PageImageMap { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>完整版本串，如 4.2.43；读取失败为 "?"</summary>
    public string SpineVersion { get; set; } = "";

    /// <summary>版本线 major.minor，如 4.2；用于匹配 BYO 播放资源子目录</summary>
    public string VersionLine { get; set; } = "";

    public string? AtlasPath { get; set; }

    /// <summary>atlas 中声明的页图名（相对 atlas 所在目录，可能含子路径）</summary>
    public List<string> PageImages { get; } = [];

    /// <summary>缺失的页图名</summary>
    public List<string> MissingPages { get; } = [];

    public ExportPreviewStatus Status { get; set; } = ExportPreviewStatus.InvalidData;

    public string StatusDetail { get; set; } = "";

    /// <summary>加载成功后由 metadata 回填（不参与扫描状态判定）</summary>
    public List<string> Skins { get; set; } = [];
    public List<string> Animations { get; set; } = [];

    public string StatusText => Status switch
    {
        ExportPreviewStatus.Ready => "✅就绪",
        ExportPreviewStatus.MissingAtlas => "⚠缺atlas",
        ExportPreviewStatus.MissingPages => "⚠缺页图",
        ExportPreviewStatus.VersionUnsupported => "❌版本不支持",
        _ => "❌数据无效"
    };

    public string CompanionText => AtlasPath == null
        ? "缺 atlas"
        : $"atlas 1 · png {PageImages.Count}";

    /// <summary>缺 atlas / 缺页图仍允许点选，由画布给出说明；数据无效与版本不支持禁止</summary>
    public bool CanPreview => Status is ExportPreviewStatus.Ready
        or ExportPreviewStatus.MissingAtlas
        or ExportPreviewStatus.MissingPages;

    public string ToolTip =>
        $"{FullPath}{Environment.NewLine}" +
        $"版本：{SpineVersion}    格式：{Format}{Environment.NewLine}" +
        $"atlas：{AtlasPath ?? "（未找到）"}" +
        (StatusDetail.Length > 0 ? Environment.NewLine + StatusDetail : "");
}
