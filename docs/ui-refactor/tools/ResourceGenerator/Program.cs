using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json;

namespace ResourceGenerator;

internal static class Program
{
    private static readonly Color App = C("#2B2B2B");
    private static readonly Color Panel = C("#454547");
    private static readonly Color PanelAlt = C("#505052");
    private static readonly Color ControlWell = C("#1E1E20");
    private static readonly Color Input = C("#262628");
    private static readonly Color List = C("#202023");
    private static readonly Color Border = C("#5E5E60");
    private static readonly Color BorderStrong = C("#727274");
    private static readonly Color Text = C("#E6E6E6");
    private static readonly Color Muted = C("#B8B8B8");
    private static readonly Color Blue = C("#2388C9");
    private static readonly Color Cyan = C("#39C5D8");
    private static readonly Color Orange = C("#F08A3C");
    private static readonly Color Green = C("#6BBF59");
    private static readonly Color Red = C("#D95C5C");

    private static readonly List<AssetInfo> Manifest = [];

    [STAThread]
    private static void Main()
    {
        var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
        var assetRoot = Path.Combine(repoRoot, "KeyMacro", "assets", "ui");
        var previewPath = Path.Combine(repoRoot, "docs", "ui-refactor", "resource-preview.png");

        Directory.CreateDirectory(assetRoot);
        foreach (var sub in new[] { "buttons", "inputs", "panels", "lists", "tabs", "progress", "checks", "menus", "dialogs", "titlebar", "icons" })
            Directory.CreateDirectory(Path.Combine(assetRoot, sub));

        GenerateButtons(assetRoot);
        GenerateInputs(assetRoot);
        GeneratePanels(assetRoot);
        GenerateLists(assetRoot);
        GenerateTabs(assetRoot);
        GenerateProgress(assetRoot);
        GenerateChecks(assetRoot);
        GenerateMenus(assetRoot);
        GenerateDialogs(assetRoot);
        GenerateTitleBar(assetRoot);
        GenerateIcons(assetRoot);
        GenerateReadme(assetRoot);
        GenerateManifest(assetRoot);
        GeneratePreview(previewPath);

        Console.WriteLine($"Generated {Manifest.Count} UI assets");
        Console.WriteLine(assetRoot);
        Console.WriteLine(previewPath);
    }

    private static void GenerateButtons(string root)
    {
        var kinds = new[]
        {
            ("neutral", PanelAlt),
            ("primary", Blue),
            ("danger", Red),
            ("success", Green),
            ("tool", Cyan),
            ("spine", Orange),
            ("cli", C("#5A458A"))
        };

        foreach (var (kind, color) in kinds)
        {
            Save(root, $"buttons/button_{kind}_normal.png", 160, 40, g => DrawButtonBase(g, new(0, 0, 160, 40), color, "normal"));
            Save(root, $"buttons/button_{kind}_hover.png", 160, 40, g => DrawButtonBase(g, new(0, 0, 160, 40), Light(color, 0.10f), "hover"));
            Save(root, $"buttons/button_{kind}_pressed.png", 160, 40, g => DrawButtonBase(g, new(0, 0, 160, 40), Dark(color, 0.16f), "pressed"));
            Save(root, $"buttons/button_{kind}_active.png", 160, 40, g => DrawButtonBase(g, new(0, 0, 160, 40), Light(color, 0.18f), "active"));
            Save(root, $"buttons/button_{kind}_disabled.png", 160, 40, g => DrawButtonBase(g, new(0, 0, 160, 40), C("#3D3D3F"), "disabled"));
        }
    }

    private static void GenerateInputs(string root)
    {
        Save(root, "inputs/input_normal.png", 360, 36, g => DrawInput(g, new(0, 0, 360, 36), BorderStrong, Input));
        Save(root, "inputs/input_focused.png", 360, 36, g => DrawInput(g, new(0, 0, 360, 36), Blue, C("#202023")));
        Save(root, "inputs/input_readonly.png", 360, 36, g => DrawInput(g, new(0, 0, 360, 36), Border, C("#222224")));
        Save(root, "inputs/input_disabled.png", 360, 36, g => DrawInput(g, new(0, 0, 360, 36), C("#4A4A4C"), C("#2F2F31")));
        Save(root, "inputs/combo_normal.png", 360, 36, g =>
        {
            DrawInput(g, new(0, 0, 360, 36), BorderStrong, Input);
            DrawArrowBox(g, new(326, 7, 26, 22), Muted);
        });
        Save(root, "inputs/search_normal.png", 360, 36, g =>
        {
            DrawInput(g, new(0, 0, 360, 36), BorderStrong, Input);
            DrawSearchIcon(g, new(12, 10, 16, 16), Muted);
        });
    }

    private static void GeneratePanels(string root)
    {
        Save(root, "panels/panel_section.png", 480, 220, g => DrawPanel(g, new(0, 0, 480, 220), true));
        Save(root, "panels/panel_plain.png", 480, 220, g => DrawPanel(g, new(0, 0, 480, 220), false));
        Save(root, "panels/toolbar_well.png", 520, 40, g =>
        {
            FillRound(g, new(0, 0, 520, 40), C("#252527"), 6);
            StrokeRound(g, new(0, 0, 519, 39), Border, 6);
        });
    }

    private static void GenerateLists(string root)
    {
        Save(root, "lists/list_container.png", 640, 260, g => DrawListShell(g, new(0, 0, 640, 260)));
        Save(root, "lists/table_header.png", 640, 34, g =>
        {
            FillRound(g, new(0, 0, 640, 34), C("#2B2B2D"), 5);
            StrokeRound(g, new(0, 0, 639, 33), BorderStrong, 5);
        });
        Save(root, "lists/row_normal.png", 640, 40, g => Fill(g, new(0, 0, 640, 40), C("#454547")));
        Save(root, "lists/row_alt.png", 640, 40, g => Fill(g, new(0, 0, 640, 40), C("#404042")));
        Save(root, "lists/row_selected.png", 640, 40, g => Fill(g, new(0, 0, 640, 40), C("#5B7782")));
        Save(root, "lists/row_hover.png", 640, 40, g => Fill(g, new(0, 0, 640, 40), C("#4E5558")));
    }

    private static void GenerateTabs(string root)
    {
        Save(root, "tabs/tab_active.png", 150, 34, g => DrawTab(g, new(0, 0, 150, 34), true));
        Save(root, "tabs/tab_inactive.png", 150, 34, g => DrawTab(g, new(0, 0, 150, 34), false));
    }

    private static void GenerateProgress(string root)
    {
        foreach (var (name, color) in new[] { ("running", Blue), ("complete", Green), ("error", Red), ("idle", C("#555557")) })
        {
            Save(root, $"progress/progress_{name}.png", 520, 28, g => DrawProgress(g, new(0, 0, 520, 28), color, name == "idle" ? 0f : .58f));
        }
    }

    private static void GenerateChecks(string root)
    {
        Save(root, "checks/checkbox_unchecked.png", 24, 24, g => DrawCheck(g, new(2, 2, 20, 20), false, BorderStrong));
        Save(root, "checks/checkbox_checked.png", 24, 24, g => DrawCheck(g, new(2, 2, 20, 20), true, Blue));
        Save(root, "checks/checkbox_disabled.png", 24, 24, g => DrawCheck(g, new(2, 2, 20, 20), false, C("#4A4A4C")));
    }

    private static void GenerateMenus(string root)
    {
        Save(root, "menus/context_menu_shell.png", 280, 360, g =>
        {
            FillRound(g, new(0, 0, 280, 360), C("#29292B"), 6);
            StrokeRound(g, new(0, 0, 279, 359), BorderStrong, 6);
        });
        Save(root, "menus/menu_item_normal.png", 260, 30, g => FillRound(g, new(0, 0, 260, 30), C("#29292B"), 4));
        Save(root, "menus/menu_item_hover.png", 260, 30, g =>
        {
            FillRound(g, new(0, 0, 260, 30), C("#39474D"), 4);
            StrokeRound(g, new(0, 0, 259, 29), C("#4D6B78"), 4);
        });
        Save(root, "menus/menu_item_danger.png", 260, 30, g =>
        {
            FillRound(g, new(0, 0, 260, 30), C("#473434"), 4);
            StrokeRound(g, new(0, 0, 259, 29), C("#7A4848"), 4);
        });
        Save(root, "menus/menu_separator.png", 260, 8, g => Stroke(g, new(8, 4, 244, 1), C("#48484A")));
    }

    private static void GenerateDialogs(string root)
    {
        Save(root, "dialogs/dialog_shell.png", 520, 280, g =>
        {
            FillRound(g, new(0, 0, 520, 280), Panel, 8);
            StrokeRound(g, new(0, 0, 519, 279), BorderStrong, 8);
            FillRound(g, new(0, 0, 520, 42), C("#242426"), 8);
            Stroke(g, new(0, 41, 520, 1), Border);
        });
        Save(root, "dialogs/warning_strip.png", 520, 44, g =>
        {
            FillRound(g, new(0, 0, 520, 44), C("#493829"), 5);
            StrokeRound(g, new(0, 0, 519, 43), Orange, 5);
        });
        Save(root, "dialogs/error_strip.png", 520, 44, g =>
        {
            FillRound(g, new(0, 0, 520, 44), C("#493030"), 5);
            StrokeRound(g, new(0, 0, 519, 43), Red, 5);
        });
        Save(root, "dialogs/success_strip.png", 520, 44, g =>
        {
            FillRound(g, new(0, 0, 520, 44), C("#32442F"), 5);
            StrokeRound(g, new(0, 0, 519, 43), Green, 5);
        });
    }

    private static void GenerateTitleBar(string root)
    {
        foreach (var state in new[] { "normal", "hover", "pressed" })
        {
            Save(root, $"titlebar/window_minimize_{state}.png", 24, 24, g => DrawWindowButton(g, new(0, 0, 24, 24), state, "min"));
            Save(root, $"titlebar/window_maximize_{state}.png", 24, 24, g => DrawWindowButton(g, new(0, 0, 24, 24), state, "max"));
            Save(root, $"titlebar/window_close_{state}.png", 24, 24, g => DrawWindowButton(g, new(0, 0, 24, 24), state, "close"));
        }
    }

    private static void GenerateIcons(string root)
    {
        var icons = new[]
        {
            "add", "edit", "delete", "delete_all", "copy", "pause", "release", "folder", "file", "search",
            "refresh", "spine", "vk", "cli", "batch", "warning", "success", "error"
        };
        foreach (var icon in icons)
        {
            Save(root, $"icons/{icon}.png", 24, 24, g => DrawIcon(g, icon, Muted));
            Save(root, $"icons/{icon}_active.png", 24, 24, g => DrawIcon(g, icon, IconColor(icon)));
        }
    }

    private static void GenerateReadme(string root)
    {
        var path = Path.Combine(root, "README.md");
        File.WriteAllText(path, """
# KeyMacro UI Resource Pack

This directory contains deterministic UI art resources for the future dark professional UI refactor.

Scope:
- General WinForms windows, menus, dialogs, lists, inputs, buttons, tabs, and progress bars.
- These assets are not for `KeyMacro/skins/*` and must not replace `VirtualKeyWindow` button skin images.
- Asset images intentionally contain no button text, so existing code-owned labels and functionality remain the source of truth.

State naming:
- `normal`: default unactive state.
- `hover`: mouse-over state.
- `pressed`: mouse-down state.
- `active`: selected/running/enabled state.
- `disabled`: disabled state.

The source generator is `docs/ui-refactor/tools/ResourceGenerator`.
""", System.Text.Encoding.UTF8);
        Manifest.Add(new("README.md", "docs", 0, 0, "usage notes"));
    }

    private static void GenerateManifest(string root)
    {
        var manifestPath = Path.Combine(root, "manifest.json");
        var payload = new
        {
            version = "ui-refactor-draft-001",
            generatedBy = "docs/ui-refactor/tools/ResourceGenerator",
            excluded = new[] { "KeyMacro/skins/*", "VirtualKeyWindow body", "VirtualButtonWidget skin states" },
            assets = Manifest
        };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }), System.Text.Encoding.UTF8);
    }

    private static void GeneratePreview(string path)
    {
        using var bmp = NewBitmap(1600, 1100);
        using var g = Graphics.FromImage(bmp);
        Setup(g);
        Fill(g, new(0, 0, 1600, 1100), App);
        Label(g, "KeyMacro UI Resource Preview", 36, 30, 22, Text);
        Label(g, "Deep gray workbench style. Text in this preview is not baked into runtime button assets.", 36, 66, 10, Muted);

        DrawPreviewButtons(g, 36, 120);
        DrawPreviewInputs(g, 36, 372);
        DrawPreviewLists(g, 36, 512);
        DrawPreviewSystem(g, 760, 120);
        DrawPreviewIcons(g, 760, 720);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        bmp.Save(path, ImageFormat.Png);
    }

    private static void DrawPreviewButtons(Graphics g, int x, int y)
    {
        Label(g, "Buttons", x, y, 14, Text);
        var kinds = new[] { ("neutral", PanelAlt), ("primary", Blue), ("danger", Red), ("success", Green), ("tool", Cyan), ("spine", Orange), ("cli", C("#5A458A")) };
        var states = new[] { "normal", "hover", "pressed", "active", "disabled" };
        for (var r = 0; r < kinds.Length; r++)
        {
            Label(g, kinds[r].Item1, x, y + 38 + r * 30, 9, Muted);
            for (var c = 0; c < states.Length; c++)
            {
                var bg = states[c] switch
                {
                    "hover" => Light(kinds[r].Item2, .10f),
                    "pressed" => Dark(kinds[r].Item2, .16f),
                    "active" => Light(kinds[r].Item2, .18f),
                    "disabled" => C("#3D3D3F"),
                    _ => kinds[r].Item2
                };
                DrawButtonBase(g, new(x + 96 + c * 104, y + 30 + r * 30, 92, 26), bg, states[c]);
                Label(g, states[c], x + 96 + c * 104, y + 36 + r * 30, 8, Text, 92, align: StringAlignment.Center);
            }
        }
    }

    private static void DrawPreviewInputs(Graphics g, int x, int y)
    {
        Label(g, "Inputs and Checks", x, y, 14, Text);
        DrawInput(g, new(x, y + 40, 360, 36), BorderStrong, Input);
        Label(g, "normal input", x + 16, y + 50, 9, Muted);
        DrawInput(g, new(x + 386, y + 40, 260, 36), Blue, C("#202023"));
        Label(g, "focused", x + 402, y + 50, 9, Text);
        DrawCheck(g, new(x, y + 96, 20, 20), false, BorderStrong);
        Label(g, "unchecked", x + 30, y + 96, 9, Muted);
        DrawCheck(g, new(x + 148, y + 96, 20, 20), true, Blue);
        Label(g, "checked", x + 178, y + 96, 9, Text);
    }

    private static void DrawPreviewLists(Graphics g, int x, int y)
    {
        Label(g, "List and Progress", x, y, 14, Text);
        DrawListShell(g, new(x, y + 40, 650, 230));
        Fill(g, new(x + 8, y + 48, 634, 34), C("#2B2B2D"));
        Label(g, "File", x + 22, y + 58, 9, Text);
        Label(g, "Path", x + 260, y + 58, 9, Text);
        var rows = new[] { ("201.png", @"source\201.png", C("#454547")), ("zzps-skin.json", @"source\zzps-skin.json", C("#5B7782")), ("run.atlas", @"source\run.atlas", C("#404042")) };
        for (var i = 0; i < rows.Length; i++)
        {
            Fill(g, new(x + 8, y + 82 + i * 40, 634, 40), rows[i].Item3);
            Label(g, rows[i].Item1, x + 22, y + 94 + i * 40, 9, Text);
            Label(g, rows[i].Item2, x + 260, y + 94 + i * 40, 9, Text);
        }
        Label(g, "processing: zzps-skin.json -> targets4", x, y + 296, 9, Text, 650, align: StringAlignment.Center);
        DrawProgress(g, new(x, y + 320, 650, 28), Blue, .42f);
        Label(g, "4/10", x, y + 326, 9, Text, 650, align: StringAlignment.Center);
    }

    private static void DrawPreviewSystem(Graphics g, int x, int y)
    {
        Label(g, "Panels, Tabs, Menus, Dialogs", x, y, 14, Text);
        DrawPanel(g, new(x, y + 40, 360, 170), true);
        Label(g, "Section panel", x + 16, y + 52, 9, Text);
        DrawTab(g, new(x, y + 238, 130, 34), true);
        Label(g, "active tab", x, y + 247, 9, Text, 130, align: StringAlignment.Center);
        DrawTab(g, new(x + 134, y + 238, 130, 34), false);
        Label(g, "inactive", x + 134, y + 247, 9, Muted, 130, align: StringAlignment.Center);
        FillRound(g, new(x + 390, y + 40, 260, 250), C("#29292B"), 6);
        StrokeRound(g, new(x + 390, y + 40, 260, 250), BorderStrong, 6);
        Label(g, "[ Button ]", x + 410, y + 60, 9, Muted);
        FillRound(g, new(x + 402, y + 92, 236, 30), C("#39474D"), 4);
        Label(g, "修改按钮名称", x + 418, y + 99, 9, Text);
        FillRound(g, new(x + 402, y + 132, 236, 30), C("#473434"), 4);
        Label(g, "强制停止", x + 418, y + 139, 9, Red);
        FillRound(g, new(x, y + 324, 520, 230), Panel, 8);
        StrokeRound(g, new(x, y + 324, 520, 230), BorderStrong, 8);
        FillRound(g, new(x, y + 324, 520, 42), C("#242426"), 8);
        Label(g, "确认删除", x + 16, y + 336, 11, Text);
        FillRound(g, new(x + 20, y + 388, 480, 44), C("#493030"), 5);
        StrokeRound(g, new(x + 20, y + 388, 480, 44), Red, 5);
        Label(g, "危险操作需要明确按钮状态", x + 42, y + 401, 10, Text);
        DrawButtonBase(g, new(x + 294, y + 490, 88, 34), PanelAlt, "normal");
        Label(g, "取消", x + 294, y + 499, 9, Text, 88, align: StringAlignment.Center);
        DrawButtonBase(g, new(x + 398, y + 490, 88, 34), Red, "normal");
        Label(g, "删除", x + 398, y + 499, 9, Text, 88, align: StringAlignment.Center);
    }

    private static void DrawPreviewIcons(Graphics g, int x, int y)
    {
        Label(g, "Icon set", x, y, 14, Text);
        var icons = new[] { "add", "edit", "delete", "copy", "pause", "release", "folder", "file", "search", "warning", "success", "error", "spine", "vk", "cli", "batch" };
        for (var i = 0; i < icons.Length; i++)
        {
            var ix = x + (i % 8) * 88;
            var iy = y + 42 + (i / 8) * 66;
            using var temp = NewBitmap(24, 24);
            using (var tg = Graphics.FromImage(temp))
            {
                Setup(tg);
                DrawIcon(tg, icons[i], IconColor(icons[i]));
            }
            g.DrawImage(temp, ix, iy);
            Label(g, icons[i], ix - 22, iy + 30, 8, Muted, 68, align: StringAlignment.Center);
        }
    }

    private static void DrawButtonBase(Graphics g, Rectangle rect, Color bg, string state)
    {
        var outer = rect;
        FillRound(g, outer, state == "disabled" ? C("#2D2D2F") : ControlWell, 7);
        StrokeRound(g, new(outer.X, outer.Y, outer.Width - 1, outer.Height - 1), state == "active" ? Light(bg, .28f) : C("#4A4A4C"), 7);
        var inner = Rectangle.Inflate(outer, -5, -5);
        if (state == "pressed")
            inner.Offset(0, 1);
        using var brush = new LinearGradientBrush(inner, Light(bg, .08f), Dark(bg, .10f), LinearGradientMode.Vertical);
        FillRound(g, inner, brush, 5);
        StrokeRound(g, new(inner.X, inner.Y, inner.Width - 1, inner.Height - 1), state == "disabled" ? C("#555557") : Light(bg, .22f), 5);
        if (state == "active")
            FillRound(g, new(inner.X + 4, inner.Y + 3, inner.Width - 8, 3), Light(bg, .38f), 2);
        if (state != "pressed")
            Stroke(g, new(inner.X + 4, inner.Bottom - 4, inner.Width - 8, 1), Dark(bg, .25f));
    }

    private static void DrawInput(Graphics g, Rectangle rect, Color border, Color innerColor)
    {
        FillRound(g, rect, ControlWell, 7);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), C("#4A4A4C"), 7);
        var inner = Rectangle.Inflate(rect, -5, -5);
        FillRound(g, inner, innerColor, 5);
        StrokeRound(g, new(inner.X, inner.Y, inner.Width - 1, inner.Height - 1), border, 5);
    }

    private static void DrawPanel(Graphics g, Rectangle rect, bool withHeader)
    {
        FillRound(g, rect, Panel, 6);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), Border, 6);
        if (!withHeader)
            return;
        FillRound(g, new(rect.X, rect.Y, rect.Width, 34), C("#343436"), 6);
        Stroke(g, new(rect.X, rect.Y + 33, rect.Width, 1), Border);
    }

    private static void DrawListShell(Graphics g, Rectangle rect)
    {
        FillRound(g, rect, ControlWell, 7);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), C("#4A4A4C"), 7);
        var inner = Rectangle.Inflate(rect, -6, -6);
        FillRound(g, inner, List, 5);
        StrokeRound(g, new(inner.X, inner.Y, inner.Width - 1, inner.Height - 1), BorderStrong, 5);
    }

    private static void DrawTab(Graphics g, Rectangle rect, bool active)
    {
        FillRound(g, rect, active ? Panel : C("#333335"), 6);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), Border, 6);
        if (active)
            FillRound(g, new(rect.X + 8, rect.Y, rect.Width - 16, 3), Blue, 2);
    }

    private static void DrawProgress(Graphics g, Rectangle rect, Color color, float value)
    {
        FillRound(g, rect, ControlWell, 7);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), BorderStrong, 7);
        var inner = Rectangle.Inflate(rect, -4, -4);
        FillRound(g, inner, C("#19191B"), 5);
        if (value > 0)
        {
            var fillW = Math.Max(10, (int)(inner.Width * Math.Clamp(value, 0, 1)));
            FillRound(g, new(inner.X, inner.Y, fillW, inner.Height), color, 5);
        }
    }

    private static void DrawCheck(Graphics g, Rectangle rect, bool isChecked, Color color)
    {
        FillRound(g, rect, ControlWell, 4);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), color, 4);
        if (!isChecked)
            return;
        using var pen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pen, new[] { new Point(rect.X + 5, rect.Y + 10), new Point(rect.X + 9, rect.Y + 14), new Point(rect.X + 16, rect.Y + 6) });
    }

    private static void DrawArrowBox(Graphics g, Rectangle rect, Color color)
    {
        FillRound(g, rect, C("#242426"), 4);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), Border, 4);
        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, new[] { new Point(rect.X + 7, rect.Y + 9), new Point(rect.X + 19, rect.Y + 9), new Point(rect.X + 13, rect.Y + 15) });
    }

    private static void DrawSearchIcon(Graphics g, Rectangle rect, Color color)
    {
        using var pen = new Pen(color, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawEllipse(pen, rect.X, rect.Y, rect.Width - 6, rect.Height - 6);
        g.DrawLine(pen, rect.X + 12, rect.Y + 12, rect.X + 17, rect.Y + 17);
    }

    private static void DrawWindowButton(Graphics g, Rectangle rect, string state, string kind)
    {
        var bg = state switch
        {
            "hover" when kind == "close" => C("#7A3434"),
            "pressed" when kind == "close" => C("#5E2A2A"),
            "hover" => C("#3A3A3C"),
            "pressed" => C("#272729"),
            _ => C("#2E2E30")
        };
        var fg = kind == "close" && state != "normal" ? Text : Muted;
        FillRound(g, rect, bg, 5);
        StrokeRound(g, new(rect.X, rect.Y, rect.Width - 1, rect.Height - 1), C("#555557"), 5);
        using var pen = new Pen(fg, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var cx = rect.X + rect.Width / 2;
        var cy = rect.Y + rect.Height / 2;
        if (kind == "min")
            g.DrawLine(pen, cx - 5, cy + 4, cx + 5, cy + 4);
        else if (kind == "max")
            g.DrawRectangle(pen, cx - 5, cy - 5, 10, 10);
        else
        {
            g.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
            g.DrawLine(pen, cx + 5, cy - 5, cx - 5, cy + 5);
        }
    }

    private static void DrawIcon(Graphics g, string icon, Color color)
    {
        using var pen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(color);
        switch (icon)
        {
            case "add":
                g.DrawLine(pen, 12, 5, 12, 19); g.DrawLine(pen, 5, 12, 19, 12); break;
            case "edit":
                g.DrawLine(pen, 6, 18, 18, 6); g.DrawLine(pen, 5, 19, 10, 18); g.DrawLine(pen, 16, 5, 19, 8); break;
            case "delete":
                g.DrawLine(pen, 7, 8, 17, 8); g.DrawRectangle(pen, 8, 10, 8, 9); g.DrawLine(pen, 10, 5, 14, 5); break;
            case "delete_all":
                g.DrawRectangle(pen, 5, 7, 7, 12); g.DrawRectangle(pen, 12, 5, 7, 14); break;
            case "copy":
                g.DrawRectangle(pen, 5, 7, 10, 12); g.DrawRectangle(pen, 9, 4, 10, 12); break;
            case "pause":
                g.DrawLine(pen, 9, 6, 9, 18); g.DrawLine(pen, 15, 6, 15, 18); break;
            case "release":
                g.DrawArc(pen, 5, 5, 14, 14, 30, 300); g.DrawLine(pen, 17, 5, 17, 11); break;
            case "folder":
                g.DrawRectangle(pen, 4, 8, 16, 10); g.DrawLine(pen, 4, 8, 9, 5); g.DrawLine(pen, 9, 5, 13, 8); break;
            case "file":
                g.DrawRectangle(pen, 6, 4, 12, 16); g.DrawLine(pen, 13, 4, 18, 9); break;
            case "search":
                DrawSearchIcon(g, new(5, 5, 14, 14), color); break;
            case "refresh":
                g.DrawArc(pen, 5, 5, 14, 14, 40, 285); g.DrawLine(pen, 17, 5, 18, 11); break;
            case "spine":
                g.DrawEllipse(pen, 5, 5, 14, 14); g.DrawLine(pen, 12, 5, 12, 19); g.DrawLine(pen, 7, 10, 17, 10); break;
            case "vk":
                g.DrawRectangle(pen, 5, 7, 14, 10); g.DrawLine(pen, 8, 10, 8, 10); g.DrawLine(pen, 12, 10, 12, 10); g.DrawLine(pen, 16, 10, 16, 10); break;
            case "cli":
                g.DrawLine(pen, 5, 7, 10, 12); g.DrawLine(pen, 5, 17, 19, 17); break;
            case "batch":
                g.DrawRectangle(pen, 5, 5, 6, 6); g.DrawRectangle(pen, 13, 5, 6, 6); g.DrawRectangle(pen, 5, 13, 6, 6); g.DrawRectangle(pen, 13, 13, 6, 6); break;
            case "warning":
                g.DrawPolygon(pen, new[] { new Point(12, 4), new Point(20, 19), new Point(4, 19), new Point(12, 4) }); g.DrawLine(pen, 12, 9, 12, 14); g.FillEllipse(brush, 11, 17, 2, 2); break;
            case "success":
                g.DrawEllipse(pen, 4, 4, 16, 16); g.DrawLines(pen, new[] { new Point(8, 12), new Point(11, 15), new Point(17, 9) }); break;
            case "error":
                g.DrawEllipse(pen, 4, 4, 16, 16); g.DrawLine(pen, 8, 8, 16, 16); g.DrawLine(pen, 16, 8, 8, 16); break;
            default:
                g.FillEllipse(brush, 8, 8, 8, 8); break;
        }
    }

    private static Color IconColor(string icon) => icon switch
    {
        "delete" or "delete_all" or "release" or "error" => Red,
        "warning" or "spine" => Orange,
        "success" => Green,
        "vk" or "search" or "folder" => Cyan,
        "cli" => C("#9A82D6"),
        "batch" => C("#4ECAD0"),
        _ => Blue
    };

    private static void Save(string root, string relative, int width, int height, Action<Graphics> draw)
    {
        using var bmp = NewBitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        Setup(g);
        g.Clear(Color.Transparent);
        draw(g);
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        bmp.Save(path, ImageFormat.Png);
        Manifest.Add(new(relative, Category(relative), width, height, ""));
    }

    private static Bitmap NewBitmap(int width, int height)
    {
        var bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        bmp.SetResolution(96, 96);
        return bmp;
    }

    private static void Setup(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
    }

    private static string Category(string relative)
    {
        var slash = relative.IndexOf('/');
        return slash < 0 ? "root" : relative[..slash];
    }

    private static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "KeyMacro", "KeyMacro.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find repo root containing KeyMacro/KeyMacro.csproj");
    }

    private static Color C(string hex)
    {
        var h = hex.TrimStart('#');
        return Color.FromArgb(255, Convert.ToInt32(h[..2], 16), Convert.ToInt32(h[2..4], 16), Convert.ToInt32(h[4..6], 16));
    }

    private static Color Light(Color c, float amount) => Color.FromArgb(c.A, Lerp(c.R, 255, amount), Lerp(c.G, 255, amount), Lerp(c.B, 255, amount));

    private static Color Dark(Color c, float amount) => Color.FromArgb(c.A, Lerp(c.R, 0, amount), Lerp(c.G, 0, amount), Lerp(c.B, 0, amount));

    private static int Lerp(int a, int b, float t) => Math.Clamp((int)(a + (b - a) * t), 0, 255);

    private static void Fill(Graphics g, Rectangle rect, Color color)
    {
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);
    }

    private static void Stroke(Graphics g, Rectangle rect, Color color)
    {
        using var pen = new Pen(color, Math.Max(1, rect.Height));
        g.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Top);
    }

    private static void FillRound(Graphics g, Rectangle rect, Color color, int radius)
    {
        using var brush = new SolidBrush(color);
        using var path = RoundRect(rect, radius);
        g.FillPath(brush, path);
    }

    private static void FillRound(Graphics g, Rectangle rect, Brush brush, int radius)
    {
        using var path = RoundRect(rect, radius);
        g.FillPath(brush, path);
    }

    private static void StrokeRound(Graphics g, Rectangle rect, Color color, int radius)
    {
        using var pen = new Pen(color, 1);
        using var path = RoundRect(rect, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath RoundRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Max(1, radius * 2);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void Label(Graphics g, string text, int x, int y, float size, Color color, int width = 500, StringAlignment align = StringAlignment.Near)
    {
        using var font = new Font("Microsoft YaHei UI", size, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = align, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(text, font, brush, new RectangleF(x, y, width, size + 8), format);
    }

    private sealed record AssetInfo(string Path, string Category, int Width, int Height, string Note);
}
