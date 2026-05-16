using System.Reflection;

namespace KeyMacro.Services;

public static class IconService
{
    private const string IconsEmbeddedPrefix = "KeyMacro.icons.";
    private const string IcoFileName = "app.ico";
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static Icon? _cachedIcon;
    private static bool _loaded;

    /// <summary>Get the application icon. Loads on first access with embedded→disk→code fallback.</summary>
    public static Icon AppIcon
    {
        get
        {
            if (!_loaded) { _cachedIcon = LoadIcon(); _loaded = true; }
            return _cachedIcon!;
        }
    }

    public static void Dispose()
    {
        _cachedIcon?.Dispose();
        _cachedIcon = null;
        _loaded = false;
    }

    private static Icon LoadIcon()
    {
        // 1. Embedded resource (published single-file exe)
        try
        {
            var name = $"{IconsEmbeddedPrefix}{IcoFileName}";
            OperationLogger.Info($"IconService: trying embedded \"{name}\"");
            using var stream = _assembly.GetManifestResourceStream(name);
            if (stream != null)
            {
                var icon = new Icon(stream);
                OperationLogger.Info($"IconService: loaded from embedded ({icon.Width}x{icon.Height})");
                return icon;
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Warn($"IconService: embedded load failed: {ex.Message}");
        }

        // 2. Disk (development)
        try
        {
            var dirs = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "KeyMacro", "icons"),
                Path.Combine(Directory.GetCurrentDirectory(), "icons"),
                Path.Combine(AppContext.BaseDirectory, "icons")
            };
            foreach (var dir in dirs)
            {
                var path = Path.Combine(dir, IcoFileName);
                if (File.Exists(path))
                {
                    var icon = new Icon(path);
                    OperationLogger.Info($"IconService: loaded from disk \"{path}\" ({icon.Width}x{icon.Height})");
                    return icon;
                }
            }
        }
        catch (Exception ex)
        {
            OperationLogger.Warn($"IconService: disk load failed: {ex.Message}");
        }

        // 3. Code-generated fallback (32x32 blue + white K)
        OperationLogger.Info("IconService: using code-generated fallback icon");
        return CreateDefaultIcon();
    }

    private static Icon CreateDefaultIcon()
    {
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215));
        g.FillRectangle(brush, 0, 0, 32, 32);
        using var font = new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold);
        g.DrawString("K", font, System.Drawing.Brushes.White, 6, 4);
        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }
}
