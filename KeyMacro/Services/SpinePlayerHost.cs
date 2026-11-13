using System.Reflection;
using System.Text;
using System.Text.Json;
using KeyMacro.Models;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace KeyMacro.Services;

public sealed record PlayerMetadata(
    List<string> Skins, List<string> Animations, double Duration, string Skin, string Animation);

public sealed record PlayerTick(double TrackTime, double Duration, bool Paused);

public sealed record PlayerLoadOptions(
    bool PremultipliedAlpha, double Speed, double Mix, bool Loop, bool AutoPlay,
    string? Skin = null, string? Animation = null, bool Bleed = false);

/// <summary>
/// WebView2 + Spine Web Player 宿主。
/// 资源走同源 S3 路由（WebResourceRequested 拦截）：
///   /host.html /bridge.js → 程序集嵌入资源
///   /player/*             → 内置 Spine Web Player（4.1 / 4.2 / 4.3，按版本线自动选择）；
///                           若用户在「高级设置」配置了 BYO 目录则优先用 BYO
///   /files/*              → 当前文件组注册的真实绝对路径（只读，不复制）
/// </summary>
public sealed class SpinePlayerHost : IDisposable
{
    public const string Origin = "https://spinepreview.local/";
    public const string DefaultVersionLine = "4.2";

    /// <summary>随应用内置的官方 runtime 版本线（资源来自 npm @esotericsoftware/spine-player）</summary>
    public static readonly string[] BuiltInVersionLines = ["4.1", "4.2", "4.3"];

    public static HashSet<string> BuiltInVersionSet() =>
        new(BuiltInVersionLines, StringComparer.OrdinalIgnoreCase);

    private static string RuntimeKey(string versionLine) => versionLine.Replace(".", "", StringComparison.Ordinal);

    public WebView2 View { get; }

    private CoreWebView2? _core;
    private readonly Dictionary<string, string> _fileMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> _embeddedCache = new(StringComparer.OrdinalIgnoreCase);
    private string _playerDir = "";
    private string _versionLine = DefaultVersionLine;
    private string _backgroundColor = "1E1E20";
    private TaskCompletionSource<bool>? _readyTcs;
    private bool _pausedByHide;
    private bool _disposed;

    public bool IsInitialized => _core != null;
    public string PlayerDirectory => _playerDir;

    /// <summary>随应用再分发的 Spine Runtimes License 全文（供「高级设置」展示）</summary>
    public string LicenseText => Encoding.UTF8.GetString(ReadEmbedded("LICENSE.txt"));

    public event Action<PlayerMetadata>? Loaded;
    public event Action<string>? LoadFailed;
    public event Action<PlayerTick>? Tick;
    public event Action<string, double>? TrackChanged;
    public event Action? BridgeReady;

    public SpinePlayerHost()
    {
        View = new WebView2 { Dock = DockStyle.Fill };
    }

    public async Task InitializeAsync(string playerDirectory, string versionLine)
    {
        if (_core != null) return;

        // 每个进程独立 userDataFolder：避免残留的 WebView2 子进程与新实例
        // 争用同一 profile 目录导致页面/脚本挂起。顺带清理一天前的旧目录。
        var root = Path.Combine(Path.GetTempPath(), "KeyMacro", "WebView2ExportPreview");
        try
        {
            if (Directory.Exists(root))
                foreach (var old in Directory.EnumerateDirectories(root))
                    if (Directory.GetCreationTimeUtc(old) < DateTime.UtcNow.AddDays(-1))
                        Directory.Delete(old, true);
        }
        catch { /* 旧目录可能仍被占用，忽略 */ }
        var userData = Path.Combine(root, Environment.ProcessId.ToString());
        var env = await CoreWebView2Environment.CreateAsync(null, userData);
        await View.EnsureCoreWebView2Async(env);
        _core = View.CoreWebView2;
        _playerDir = playerDirectory;
        _versionLine = string.IsNullOrWhiteSpace(versionLine) ? DefaultVersionLine : versionLine;

        _core.Settings.AreDevToolsEnabled = false;
        _core.Settings.IsStatusBarEnabled = false;
        _core.AddWebResourceRequestedFilter(Origin + "*",
            CoreWebView2WebResourceContext.All,
            CoreWebView2WebResourceRequestSourceKinds.All);
        _core.WebResourceRequested += OnResourceRequested;
        _core.WebMessageReceived += OnWebMessage;

        await ReloadHostAsync();
        OperationLogger.Info("[ExportPreview] WebView2 已初始化，浏览器 " + _core.Environment.BrowserVersionString
                             + "，内置 runtime 版本 " + _versionLine
                             + (string.IsNullOrEmpty(_playerDir) ? "" : "（BYO 覆盖 " + _playerDir + "）"));
    }

    /// <summary>
    /// 切换版本线 / BYO 覆盖目录；返回 true 表示宿主已重载，调用方需重新加载当前项。
    /// playerDirectory 传空串表示使用内置 runtime。
    /// </summary>
    public async Task<bool> SetPlayerResourceAsync(string playerDirectory, string versionLine)
    {
        if (_core == null) return false;
        var vl = string.IsNullOrWhiteSpace(versionLine) ? DefaultVersionLine : versionLine;
        if (_playerDir == playerDirectory && _versionLine == vl) return false;
        _playerDir = playerDirectory;
        _versionLine = vl;
        await ReloadHostAsync();
        return true;
    }

    private async Task ReloadHostAsync()
    {
        if (_core == null) return;
        _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _core.Navigate(Origin + "host.html?v=" + DateTime.UtcNow.Ticks);
        await _readyTcs.Task.WaitAsync(TimeSpan.FromSeconds(20));
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString() ?? "";
            switch (type)
            {
                case "bridgeReady":
                    var loaded = root.TryGetProperty("spinePlayerLoaded", out var sp) && sp.GetBoolean();
                    _readyTcs?.TrySetResult(loaded);
                    if (loaded) BridgeReady?.Invoke();
                    else LoadFailed?.Invoke("Spine Web Player 未能加载：请确认安装完整；若导出数据属于特殊版本线，可在「高级设置」中指定本地资源目录覆盖");
                    break;

                case "ready":
                    var meta = new PlayerMetadata(
                        ReadStringArray(root, "skins"),
                        ReadStringArray(root, "animations"),
                        ReadDouble(root, "duration"),
                        ReadString(root, "skin"),
                        ReadString(root, "animation"));
                    OperationLogger.Info($"[ExportPreview] ready: 皮肤 {meta.Skins.Count} / 动画 {meta.Animations.Count} / 时长 {meta.Duration:F3}s / 当前 {meta.Skin} · {meta.Animation}");
                    Loaded?.Invoke(meta);
                    break;

                case "error":
                    var reason = $"[{ReadString(root, "phase")}] {ReadString(root, "reason")}";
                    OperationLogger.Error("[ExportPreview] 加载失败: " + reason);
                    LoadFailed?.Invoke(reason);
                    break;

                case "tick":
                    Tick?.Invoke(new PlayerTick(
                        ReadDouble(root, "trackTime"), ReadDouble(root, "duration"),
                        root.TryGetProperty("paused", out var p) && p.GetBoolean()));
                    break;

                case "trackChanged":
                    TrackChanged?.Invoke(ReadString(root, "animation"), ReadDouble(root, "duration"));
                    break;
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Warn("[ExportPreview] 桥接消息解析失败: " + ex.Message);
        }
    }

    private static List<string> ReadStringArray(JsonElement root, string name)
    {
        var list = new List<string>();
        if (root.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var x in arr.EnumerateArray())
                if (x.ValueKind == JsonValueKind.String) list.Add(x.GetString() ?? "");
        return list;
    }

    private static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static double ReadDouble(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private void OnResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        string path;
        try { path = Uri.UnescapeDataString(new Uri(e.Request.Uri).AbsolutePath).TrimStart('/'); }
        catch { path = ""; }

        byte[] bytes = [];
        var mime = "application/octet-stream";
        try
        {
            if (path == "host.html" || path == "bridge.js")
            {
                bytes = ReadEmbedded(path);
                mime = path.EndsWith(".html") ? "text/html; charset=utf-8" : "application/javascript; charset=utf-8";
            }
            else if (path.StartsWith("player/", StringComparison.Ordinal))
            {
                var name = path[7..];
                // /player/ 只接受纯文件名，拒绝任何路径穿越
                if (!name.Contains("..") && !name.Contains('/') && !name.Contains('\\'))
                {
                    // BYO 覆盖优先：用户显式指定的本地目录
                    var full = string.IsNullOrEmpty(_playerDir) ? "" : Path.Combine(_playerDir, name);
                    if (full.Length > 0 && File.Exists(full)) bytes = File.ReadAllBytes(full);
                    // 否则（或 BYO 缺该文件）回退到按版本线内置的官方 runtime
                    if (bytes.Length == 0)
                        bytes = ReadEmbedded("Runtime." + RuntimeKey(_versionLine) + "." + name);
                }
                mime = MimeOf(name);
            }
            else if (path.StartsWith("files/", StringComparison.Ordinal))
            {
                var key = path[6..];
                if (_fileMap.TryGetValue(key, out var full) && File.Exists(full))
                    bytes = File.ReadAllBytes(full);
                mime = MimeOf(key);
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Warn($"[ExportPreview] 资源读取失败 {path}: {ex.Message}");
        }

        var status = bytes.Length > 0 ? 200 : 404;
        if (status == 404 && path.Length > 0
            && !path.EndsWith(".map", StringComparison.OrdinalIgnoreCase)
            && !path.Equals("favicon.ico", StringComparison.OrdinalIgnoreCase))
            OperationLogger.Warn("[ExportPreview] 资源 404: " + path);
        var capturedBytes = bytes;
        var capturedMime = mime;
        try
        {
            View.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (_core != null)
                        e.Response = _core.Environment.CreateWebResourceResponse(
                            new MemoryStream(capturedBytes), status,
                            status == 200 ? "OK" : "Not Found",
                            "Content-Type: " + capturedMime + "\r\n" +
                            "Access-Control-Allow-Origin: *\r\n" +
                            "Cache-Control: no-store\r\n");
                }
                catch (Exception ex) { OperationLogger.Warn("[ExportPreview] 资源响应失败: " + ex.Message); }
                finally { deferral.Complete(); }
            }));
        }
        catch { deferral.Complete(); }
    }

    private static string MimeOf(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "application/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".atlas" or ".txt" => "text/plain; charset=utf-8",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".skel" or ".bytes" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    /// <summary>按显式 LogicalName 读取嵌入资源：KeyMacro.SpinePlayer.&lt;key&gt;</summary>
    private byte[] ReadEmbedded(string key)
    {
        if (_embeddedCache.TryGetValue(key, out var cached)) return cached;
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = "KeyMacro.SpinePlayer." + key;
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            OperationLogger.Warn("[ExportPreview] 嵌入资源缺失: " + resourceName);
            return [];
        }
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        _embeddedCache[key] = bytes;
        return bytes;
    }

    /// <summary>把当前文件组注册为 /files/* 虚拟路径；返回 false 表示缺 atlas，无法交给 Web Player</summary>
    public bool LoadGroup(ExportPreviewEntry entry, PlayerLoadOptions options)
    {
        if (_core == null || entry.AtlasPath == null) return false;

        _fileMap.Clear();

        // 磁盘文件名可能是 Unity 的 .skel.bytes / .json.txt / .atlas.txt，
        // 但官方 SpinePlayer.loadSkeleton() 用 config.skeleton.endsWith(".json")
        // 判定格式，URL 必须以规范扩展名结尾、不能带 ?v= 查询串。
        // 因此统一改用逻辑 URL 名，内容仍从 _fileMap 指向的真实绝对路径读取。
        string Url(string key, string absolute)
        {
            _fileMap[key] = absolute;
            return Origin + "files/" + key.Replace('\\', '/');
        }

        var skeletonUrl = Url(entry.SkeletonUrlName, entry.FullPath);
        var atlasUrl = Url(entry.AtlasUrlName, entry.AtlasPath);
        foreach (var page in entry.PageImageMap)
            Url(page.Key, page.Value);

        Send(new
        {
            type = "loadGroup",
            group = new
            {
                skeleton = skeletonUrl,
                atlas = atlasUrl,
                premultipliedAlpha = options.PremultipliedAlpha,
                // AUD-043 ①：Bleed 只作导出设置核对记录，JS 侧不参与渲染。
                bleed = options.Bleed,
                backgroundColor = _backgroundColor,
                skin = options.Skin ?? "",
                animation = options.Animation ?? ""
            },
            mix = options.Mix,
            speed = options.Speed,
            loop = options.Loop,
            autoPlay = options.AutoPlay
        });
        OperationLogger.Info($"[ExportPreview] 加载 {entry.FileName}（{entry.Format} / Spine {entry.SpineVersion}，"
                             + $"URL {entry.SkeletonUrlName}，页图 {entry.PageImages.Count} 张，PMA={options.PremultipliedAlpha}）");
        return true;
    }

    public void Send(object payload)
    {
        if (_core == null || _disposed) return;
        try
        {
            var json = JsonSerializer.Serialize(payload);
            _core.PostWebMessageAsJson(json);
            OperationLogger.Info("[ExportPreview] →JS " + json[..Math.Min(160, json.Length)]);
        }
        catch (Exception ex) { OperationLogger.Warn("[ExportPreview] 桥接下发失败: " + ex.Message); }
    }

    public void Play() => Send(new { type = "play" });
    public void Pause() => Send(new { type = "pause" });
    public void SetSpeed(double v) => Send(new { type = "setSpeed", value = v });
    public void SetMix(double v) => Send(new { type = "setMix", value = v });
    public void SetSkin(string name) => Send(new { type = "setSkin", name });
    public void SetAnimation(string name, bool loop) => Send(new { type = "setAnimation", name, loop });
    public void SetLoop(bool loop) => Send(new { type = "setLoop", value = loop });
    public void Seek(double t) => Send(new { type = "seek", time = t });
    public void SetupPose() => Send(new { type = "setupPose" });
    public void SetDebug(bool on) => Send(new { type = "setDebug", value = on });

    /// <summary>AUD-043 ⑤：实时切换画布背景，无需重建 player。同时记住当前档位，供下次 loadGroup 使用。</summary>
    public void SetBackground(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        _backgroundColor = hex;
        Send(new { type = "setBackground", hex });
    }

    /// <summary>AUD-043 ③：把缩放 / 平移归零，回到该文件首次加载的基准视图。</summary>
    public void ResetView() => Send(new { type = "resetView" });

    public void DisposePlayer() => Send(new { type = "dispose" });

    /// <summary>主分页切走 / 切回：暂停并保留帧，切回后恢复</summary>
    public void SetActive(bool active)
    {
        if (_core == null) return;
        if (!active)
        {
            _pausedByHide = true;
            Pause();
        }
        else if (_pausedByHide)
        {
            _pausedByHide = false;
            if (_playingWanted) Play();
        }
    }

    private bool _playingWanted = true;
    public void SetPlayingWanted(bool wanted) => _playingWanted = wanted;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_core != null)
            {
                _core.WebMessageReceived -= OnWebMessage;
                _core.WebResourceRequested -= OnResourceRequested;
            }
            DisposePlayer();
            View.Dispose();
        }
        catch (Exception ex) { OperationLogger.Warn("[ExportPreview] WebView2 释放异常: " + ex.Message); }
    }
}
