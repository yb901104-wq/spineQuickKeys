using KeyMacro.Controls;
using KeyMacro.Forms;
using System.Reflection;

namespace KeyMacro.Services;

public enum UiWindowProfile
{
    Main,
    SequenceEditor,
    HotkeyRecorder,
    SpineHotkeyEditor,
    VkWindowManager,
    BatchCopy,
    SourceFilePicker,
    ConflictDialog,
    BatchCli,
    AnimationSelect,
    ReNameTool,
    InputDialog,
    SubfolderSelect,
    GenericDialog
}

public static class UiTheme
{
    public static readonly Color App = FromHex("#2B2B2B");
    public static readonly Color Workspace = FromHex("#3A3A3C");
    public static readonly Color Panel = FromHex("#454547");
    public static readonly Color PanelAlt = FromHex("#505052");
    public static readonly Color Input = FromHex("#262628");
    public static readonly Color ControlWell = FromHex("#1E1E20");
    public static readonly Color List = FromHex("#202023");
    public static readonly Color Border = FromHex("#5E5E60");
    public static readonly Color BorderStrong = FromHex("#727274");
    public static readonly Color Text = FromHex("#E6E6E6");
    public static readonly Color Muted = FromHex("#B8B8B8");
    public static readonly Color Disabled = FromHex("#777777");
    public static readonly Color Blue = FromHex("#2388C9");
    public static readonly Color Cyan = FromHex("#39C5D8");
    public static readonly Color Orange = FromHex("#F08A3C");
    public static readonly Color Green = FromHex("#6BBF59");
    public static readonly Color Red = FromHex("#D95C5C");

    private static readonly Dictionary<string, Image> ImageCache = [];
    private static readonly HashSet<Button> ThemedButtons = [];

    public static void Apply(Form form, UiWindowProfile profile)
    {
        if (form is VirtualKeyWindow)
            return;

        // Do not recursively restyle or resize entire forms here. UI refactor must
        // adjust each window and each control explicitly after function mapping.
        foreach (var menu in FindContextMenus(form))
            Apply(menu);
    }

    public static void Apply(ContextMenuStrip menu)
    {
        menu.Renderer = new DarkMenuRenderer();
        menu.BackColor = FromHex("#29292B");
        menu.ForeColor = Text;
        menu.Font = BodyFont(9f);
        foreach (ToolStripItem item in menu.Items)
        {
            item.BackColor = FromHex("#29292B");
            item.ForeColor = IsDangerText(item.Text) ? Red : Text;
            item.Font = item.Enabled ? BodyFont(9f) : BodyFont(9f);
        }
    }

    public static void ApplyDefaultSize(Form form, UiWindowProfile profile)
    {
        var (size, min) = profile switch
        {
            UiWindowProfile.Main => (new Size(1200, 760), new Size(900, 560)),
            UiWindowProfile.SequenceEditor => (new Size(1100, 850), new Size(780, 560)),
            UiWindowProfile.HotkeyRecorder => (new Size(580, 360), new Size(420, 240)),
            UiWindowProfile.SpineHotkeyEditor => (new Size(1100, 720), new Size(820, 520)),
            UiWindowProfile.VkWindowManager => (new Size(760, 520), new Size(560, 360)),
            UiWindowProfile.BatchCopy => (new Size(1100, 760), new Size(820, 620)),
            UiWindowProfile.SourceFilePicker => (new Size(1000, 700), new Size(760, 520)),
            UiWindowProfile.ConflictDialog => (new Size(620, 430), new Size(520, 360)),
            UiWindowProfile.BatchCli => (new Size(1100, 760), new Size(880, 620)),
            UiWindowProfile.AnimationSelect => (new Size(520, 560), new Size(420, 420)),
            UiWindowProfile.ReNameTool => (new Size(900, 560), new Size(820, 500)),
            UiWindowProfile.InputDialog => (new Size(520, 220), new Size(420, 180)),
            UiWindowProfile.SubfolderSelect => (new Size(820, 620), new Size(640, 460)),
            _ => (new Size(620, 430), new Size(420, 260))
        };

        form.Size = size;
        if (form.MinimumSize.Width < min.Width || form.MinimumSize.Height < min.Height)
            form.MinimumSize = min;
    }

    private static void ApplyRecursive(Control root)
    {
        foreach (Control control in root.Controls)
        {
            ApplyControl(control);
            if (control.HasChildren)
                ApplyRecursive(control);
        }
    }

    private static void ApplyControl(Control control)
    {
        control.Font = control is TextBoxBase or ComboBox ? BodyFont(10f) : BodyFont();
        control.ForeColor = Text;

        switch (control)
        {
            case Button button:
                Apply(button);
                break;
            case DataGridView grid:
                Apply(grid);
                break;
            case TextBox textBox:
                textBox.BackColor = textBox.ReadOnly ? FromHex("#222224") : Input;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox combo:
                combo.BackColor = Input;
                combo.ForeColor = Text;
                combo.FlatStyle = FlatStyle.Flat;
                break;
            case CheckedListBox checkedList:
                checkedList.BackColor = List;
                checkedList.ForeColor = Text;
                checkedList.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox listBox:
                listBox.BackColor = List;
                listBox.ForeColor = Text;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListView listView:
                listView.BackColor = List;
                listView.ForeColor = Text;
                listView.BorderStyle = BorderStyle.FixedSingle;
                break;
            case TabControl tab:
                tab.Appearance = TabAppearance.Normal;
                tab.BackColor = App;
                break;
            case TabPage page:
                page.BackColor = Panel;
                page.ForeColor = Text;
                break;
            case TextProgressBar progress:
                progress.BackColor = ControlWell;
                progress.ForeColor = Text;
                progress.BarColor = Blue;
                progress.BorderColor = BorderStrong;
                progress.Height = Math.Max(progress.Height, 26);
                break;
            case CheckBox checkBox:
                checkBox.BackColor = TransparentParentColor(checkBox);
                checkBox.ForeColor = Text;
                checkBox.FlatStyle = FlatStyle.Flat;
                break;
            case RadioButton radio:
                radio.BackColor = TransparentParentColor(radio);
                radio.ForeColor = Text;
                radio.FlatStyle = FlatStyle.Flat;
                break;
            case Label label:
                Apply(label);
                break;
            case System.Windows.Forms.Panel:
                if (control.BackColor != Color.FromArgb(0xFF, 0xCC, 0x00))
                    control.BackColor = control.Parent is Form ? App : Panel;
                break;
        }
    }

    private static void Apply(Label label)
    {
        if (label.Parent?.BackColor == Color.FromArgb(0xFF, 0xCC, 0x00))
        {
            label.ForeColor = Color.Black;
            return;
        }

        if (label.ForeColor == Color.Gray || label.ForeColor == SystemColors.GrayText)
            label.ForeColor = Muted;
        else
            label.ForeColor = Text;
    }

    private static void Apply(Button button)
    {
        if (ThemedButtons.Contains(button))
            return;

        ThemedButtons.Add(button);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = Color.Transparent;
        button.FlatAppearance.MouseOverBackColor = Color.Transparent;
        button.UseVisualStyleBackColor = false;
        button.ForeColor = Text;
        button.Font = BodyFont(9f);
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(4, 0, 4, 1);
        if (button.AutoSize && button.MinimumSize.Height < 32)
            button.MinimumSize = new Size(button.MinimumSize.Width, 32);

        var kind = ButtonKind(button.Text);
        SetButtonImage(button, kind, button.Enabled ? "normal" : "disabled");

        button.MouseEnter += (_, _) => SetButtonImage(button, kind, button.Enabled ? "hover" : "disabled");
        button.MouseLeave += (_, _) => SetButtonImage(button, kind, button.Enabled ? "normal" : "disabled");
        button.MouseDown += (_, _) => SetButtonImage(button, kind, button.Enabled ? "pressed" : "disabled");
        button.MouseUp += (_, _) => SetButtonImage(button, kind, button.Enabled ? "hover" : "disabled");
        button.EnabledChanged += (_, _) =>
        {
            button.ForeColor = button.Enabled ? Text : Disabled;
            SetButtonImage(button, kind, button.Enabled ? "normal" : "disabled");
        };
    }

    private static void Apply(DataGridView grid)
    {
        grid.BackgroundColor = List;
        grid.GridColor = Border;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = FromHex("#2B2B2D");
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = FromHex("#2B2B2D");
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = BodyFont(9f, FontStyle.Bold);
        grid.DefaultCellStyle.BackColor = FromHex("#454547");
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = FromHex("#5B7782");
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = FromHex("#404042");
        grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
        grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 34);
    }

    private static IEnumerable<ContextMenuStrip> FindContextMenus(Form form)
    {
        foreach (var field in form.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.GetValue(form) is ContextMenuStrip menu)
                yield return menu;
        }
    }

    private static void SetButtonImage(Button button, string kind, string state)
    {
        button.BackgroundImage = LoadImage($"buttons/button_{kind}_{state}.png");
        button.BackgroundImageLayout = ImageLayout.Stretch;
        button.BackColor = Color.Transparent;
    }

    private static string ButtonKind(string text)
    {
        if (IsDangerText(text))
            return "danger";
        if (text.Contains("Spine", StringComparison.OrdinalIgnoreCase) || text.Contains("录制") || text.Contains("载入"))
            return "spine";
        if (text.Contains("CLI", StringComparison.OrdinalIgnoreCase))
            return "cli";
        if (text.Contains("虚拟") || text.Contains("VK", StringComparison.OrdinalIgnoreCase) || text.Contains("浏览") || text.Contains("选择") || text.Contains("检测") || text.Contains("刷新"))
            return "tool";
        if (text.Contains("开始") || text.Contains("执行") || text.Contains("导出") || text.Contains("确认") || text.Contains("确定") || text.Contains("保存") || text.Contains("覆盖"))
            return "primary";
        return "neutral";
    }

    private static bool IsDangerText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        return text.Contains("删除全部")
            || text.Contains("取消全部")
            || text.Contains("强制停止")
            || text.Contains("释放");
    }

    public static Image LoadImage(string relativePath)
    {
        relativePath = relativePath.Replace('\\', '/');
        if (ImageCache.TryGetValue(relativePath, out var cached))
            return cached;

        var disk = FindAssetOnDisk(relativePath);
        if (File.Exists(disk))
        {
            using var src = Image.FromFile(disk);
            return ImageCache[relativePath] = new Bitmap(src);
        }

        var asm = Assembly.GetExecutingAssembly();
        var resource = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(relativePath.Replace('/', '.'), StringComparison.OrdinalIgnoreCase));
        if (resource != null)
        {
            using var stream = asm.GetManifestResourceStream(resource);
            if (stream != null)
            {
                using var src = Image.FromStream(stream);
                return ImageCache[relativePath] = new Bitmap(src);
            }
        }

        return ImageCache[relativePath] = new Bitmap(1, 1);
    }

    private static string FindAssetOnDisk(string relativePath)
    {
        var localPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "ui", localPath);
            if (File.Exists(candidate))
                return candidate;

            var projectCandidate = Path.Combine(dir.FullName, "KeyMacro", "assets", "ui", localPath);
            if (File.Exists(projectCandidate))
                return projectCandidate;

            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "assets", "ui", localPath);
    }

    private static Color TransparentParentColor(Control control) => control.Parent?.BackColor == Color.Empty ? Panel : control.Parent?.BackColor ?? Panel;

    private static Font BodyFont(float size = 9f, FontStyle style = FontStyle.Regular) => new("Microsoft YaHei UI", size, style, GraphicsUnit.Point);

    private static Color FromHex(string hex)
    {
        var h = hex.TrimStart('#');
        return Color.FromArgb(255, Convert.ToInt32(h[..2], 16), Convert.ToInt32(h[2..4], 16), Convert.ToInt32(h[4..6], 16));
    }

    private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(FromHex("#29292B"));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            var bg = e.Item.Selected ? FromHex("#39474D") : FromHex("#29292B");
            if (IsDangerText(e.Item.Text) && e.Item.Selected)
                bg = FromHex("#473434");
            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, rect);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(FromHex("#48484A"));
            e.Graphics.DrawLine(pen, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Muted;
            base.OnRenderArrow(e);
        }
    }
}
