using System.Drawing.Drawing2D;
using System.Reflection;
using System.Text.Json;

namespace KeyMacro.Services;

public class VkSkinLoader
{
    private readonly string _skinName;
    private readonly string _skinDir = "";
    private SkinData? _skin;
    private readonly Dictionary<string, Image> _imageCache = [];
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private const string EmbeddedPrefix = "KeyMacro.skins.";

    public VkSkinLoader(string? skinPath)
    {
        _skinName = skinPath ?? "";
        OperationLogger.Info($"VkSkinLoader: skinPath=\"{skinPath}\"");

        // Disk path for development fallback
        if (!string.IsNullOrEmpty(skinPath))
        {
            var cwdDir = Path.Combine(Directory.GetCurrentDirectory(), "skins", skinPath);
            OperationLogger.Info($"VkSkinLoader: trying CWD path \"{cwdDir}\" exists={Directory.Exists(cwdDir)}");
            if (Directory.Exists(cwdDir))
            { _skinDir = cwdDir; OperationLogger.Info($"VkSkinLoader: using CWD path \"{_skinDir}\""); return; }

            var exeDir = Path.Combine(AppContext.BaseDirectory, "skins", skinPath);
            OperationLogger.Info($"VkSkinLoader: trying BaseDir path \"{exeDir}\" exists={Directory.Exists(exeDir)}");
            _skinDir = Directory.Exists(exeDir) ? exeDir : "";
        }
        OperationLogger.Info($"VkSkinLoader: _skinDir=\"{_skinDir}\" _skinName=\"{_skinName}\"");
    }

    public bool HasSkin => _skin != null;

    /// <summary>Open an embedded resource stream for a file in the current skin.</summary>
    private Stream? OpenEmbedded(string fileName)
    {
        if (string.IsNullOrEmpty(_skinName)) return null;
        var name = $"{EmbeddedPrefix}{_skinName}.{fileName}";
        OperationLogger.Info($"VkSkinLoader.OpenEmbedded: looking for \"{name}\"");
        var stream = _assembly.GetManifestResourceStream(name);
        OperationLogger.Info($"VkSkinLoader.OpenEmbedded: \"{name}\" -> {(stream != null ? "FOUND" : "null")}");
        return stream;
    }

    /// <summary>Load an image from embedded resources, then fall back to disk.</summary>
    private Image? LoadSkinImage(string fileName)
    {
        var cacheKey = fileName;
        if (_imageCache.TryGetValue(cacheKey, out var cached)) return cached;

        Image? img = null;

        // 1. Try embedded resource (published single-file exe)
        try
        {
            using var stream = OpenEmbedded(fileName);
            if (stream != null)
            {
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                img = Image.FromStream(ms);
                OperationLogger.Info($"VkSkinLoader.LoadSkinImage: loaded \"{fileName}\" from embedded ({ms.Length} bytes)");
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"VkSkinLoader.LoadSkinImage: embedded failed for \"{fileName}\": {ex.Message}");
        }

        // 2. Fall back to disk (development)
        if (img == null && !string.IsNullOrEmpty(_skinDir))
        {
            var path = Path.Combine(_skinDir, fileName);
            if (File.Exists(path))
            {
                try { img = Image.FromFile(path); }
                catch { }
            }
        }

        if (img != null)
            _imageCache[cacheKey] = img;
        return img;
    }

    public void Load()
    {
        // 1. Try embedded skin.json
        try
        {
            using var stream = OpenEmbedded("skin.json");
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                _skin = JsonSerializer.Deserialize<SkinData>(json) ?? new SkinData();
                return;
            }
        }
        catch { }

        // 2. Fall back to disk
        if (string.IsNullOrEmpty(_skinDir) || !Directory.Exists(_skinDir)) return;
        var jsonPath = Path.Combine(_skinDir, "skin.json");
        if (!File.Exists(jsonPath)) return;

        try
        {
            var json = File.ReadAllText(jsonPath);
            _skin = JsonSerializer.Deserialize<SkinData>(json) ?? new SkinData();
        }
        catch { _skin = null; }
    }

    public Color GetColor(string key, Color defaultColor)
    {
        if (_skin?.Colors == null || !_skin.Colors.TryGetValue(key, out var hex))
            return defaultColor;
        try { return ColorTranslator.FromHtml(hex); }
        catch { return defaultColor; }
    }

    public Image? GetButtonImage(string state) => LoadSkinImage($"btn_{state}.png");

    public Image? GetWindowBackground() => LoadSkinImage("window_bg.png");

    /// <summary>Draw a 9-slice scaled image to the target rectangle.</summary>
    public static void DrawNineSlice(Graphics g, Image img, Rectangle target, int margin = 4)
    {
        int m = Math.Min(margin, Math.Min(img.Width / 2, img.Height / 2));

        // corners
        DrawImage(g, img, new Rectangle(target.X, target.Y, m, m), new Rectangle(0, 0, m, m));
        DrawImage(g, img, new Rectangle(target.Right - m, target.Y, m, m), new Rectangle(img.Width - m, 0, m, m));
        DrawImage(g, img, new Rectangle(target.X, target.Bottom - m, m, m), new Rectangle(0, img.Height - m, m, m));
        DrawImage(g, img, new Rectangle(target.Right - m, target.Bottom - m, m, m), new Rectangle(img.Width - m, img.Height - m, m, m));

        // edges
        DrawImage(g, img, new Rectangle(target.X + m, target.Y, target.Width - m * 2, m), new Rectangle(m, 0, img.Width - m * 2, m));
        DrawImage(g, img, new Rectangle(target.X + m, target.Bottom - m, target.Width - m * 2, m), new Rectangle(m, img.Height - m, img.Width - m * 2, m));
        DrawImage(g, img, new Rectangle(target.X, target.Y + m, m, target.Height - m * 2), new Rectangle(0, m, m, img.Height - m * 2));
        DrawImage(g, img, new Rectangle(target.Right - m, target.Y + m, m, target.Height - m * 2), new Rectangle(img.Width - m, m, m, img.Height - m * 2));

        // center
        DrawImage(g, img, new Rectangle(target.X + m, target.Y + m, target.Width - m * 2, target.Height - m * 2), new Rectangle(m, m, img.Width - m * 2, img.Height - m * 2));
    }

    private static void DrawImage(Graphics g, Image img, Rectangle dest, Rectangle src)
    {
        g.DrawImage(img, dest, src, GraphicsUnit.Pixel);
    }
}

public class SkinData
{
    public string Name { get; set; } = "default";
    public string Author { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public Dictionary<string, string>? Colors { get; set; }
}
