using System.Drawing.Drawing2D;
using System.Text.Json;

namespace KeyMacro.Services;

public class VkSkinLoader
{
    private readonly string _skinDir;
    private SkinData? _skin;
    private readonly Dictionary<string, Image> _imageCache = [];

    public VkSkinLoader(string? skinPath)
    {
        _skinDir = string.IsNullOrEmpty(skinPath)
            ? ""
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KeyMacro", "skins", skinPath);
    }

    public bool HasSkin => _skin != null;

    public void Load()
    {
        if (string.IsNullOrEmpty(_skinDir) || !Directory.Exists(_skinDir)) return;

        var jsonPath = Path.Combine(_skinDir, "skin.json");
        if (!File.Exists(jsonPath)) return;

        try
        {
            var json = File.ReadAllText(jsonPath);
            _skin = JsonSerializer.Deserialize<SkinData>(json) ?? new SkinData();
        }
        catch
        {
            _skin = null;
        }
    }

    public Color GetColor(string key, Color defaultColor)
    {
        if (_skin?.Colors == null || !_skin.Colors.TryGetValue(key, out var hex))
            return defaultColor;
        try { return ColorTranslator.FromHtml(hex); }
        catch { return defaultColor; }
    }

    public Image? GetButtonImage(string state)
    {
        if (string.IsNullOrEmpty(_skinDir)) return null;
        if (_imageCache.TryGetValue(state, out var cached)) return cached;

        var path = Path.Combine(_skinDir, $"btn_{state}.png");
        if (!File.Exists(path)) return null;

        try
        {
            var img = Image.FromFile(path);
            _imageCache[state] = img;
            return img;
        }
        catch { return null; }
    }

    public Image? GetWindowBackground()
    {
        if (string.IsNullOrEmpty(_skinDir)) return null;
        if (_imageCache.TryGetValue("_window_bg", out var cached)) return cached;

        var path = Path.Combine(_skinDir, "window_bg.png");
        if (!File.Exists(path)) return null;

        try
        {
            var img = Image.FromFile(path);
            _imageCache["_window_bg"] = img;
            return img;
        }
        catch { return null; }
    }

    /// <summary>Draw a 9-slice scaled image to the target rectangle.</summary>
    public static void DrawNineSlice(Graphics g, Image img, Rectangle target, int margin = 4)
    {
        var src = new Rectangle(0, 0, img.Width, img.Height);
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
