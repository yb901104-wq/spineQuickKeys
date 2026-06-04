using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using KeyMacro.Controls;
using KeyMacro.Forms;
using KeyMacro.Forms.ReNameTool;
using KeyMacro.Models;
using KeyMacro.Services;

namespace RuntimeWindowCapture;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            MessageBox.Show("Usage: RuntimeWindowCapture <window-key> <output.png>", "RuntimeWindowCapture");
            return;
        }

        ApplicationConfiguration.Initialize();

        var key = args[0].Trim().ToLowerInvariant();
        var output = Path.GetFullPath(args[1]);
        using var form = CreateForm(key);
        form.Shown += (_, _) => PrepareWindow(form, key);
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(80, 80);

        var timer = new System.Windows.Forms.Timer { Interval = 900 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Capture(form, output, key);
            CloseForm(form, key);
        };

        form.Shown += (_, _) => timer.Start();
        Application.Run(form);
    }

    private static Form CreateForm(string key)
    {
        return key switch
        {
            "sequence-editor" => new SequenceEditor(CreateSampleSequence()),
            "spine-hotkey-editor" => new SpineHotkeyEditor(@"D:\AI_cc\spineQuickKeys\test-hotkeys.txt", CreateSampleSpineHotkeys()),
            "vk-manager" => new VkWindowManager(new VirtualLayoutSerializer(), 2),
            "batch-copy" => new BatchCopyWindow(),
            "source-file-picker" => new SourceFilePicker(),
            "conflict-dialog" => new ConflictDialog(@"D:\test\targets1\images", ["body.png", "head.png", "weapon.png"]),
            "input-dialog" => new InputDialog("修改按钮名称", "输入新的按钮名称:", "按钮1"),
            "subfolder-select" => new SubfolderSelectDialog(["targets1", "targets2", "targets3", "targets4", "targets5", @"images\角色A", @"images\角色B", "demotion.export.json"]),
            "batch-cli-merge" => new BatchCliWindow(),
            "batch-cli-export" => new BatchCliWindow(),
            "cli-animation-select" => CreateSampleAnimationSelectDialog(),
            "rename-tool-rename" => new Form1(),
            "rename-tool-organize" => new Form1(),
            "rename-tool-unpack" => new Form1(),
            "hotkey-recorder" => new HotkeyRecorderForm(allowNoModifier: true),
            "mainform-layout" => new MainForm(),
            "vk-blank-menu" => CreateSampleVirtualKeyWindow(),
            "vk-button-menu" => CreateSampleVirtualKeyWindow(),
            "tray-menu" => new MainForm(),
            _ => throw new ArgumentException($"Unsupported window key: {key}")
        };
    }

    private static void PrepareWindow(Form form, string key)
    {
        var tab = FindControl<TabControl>(form);
        if (tab != null)
        {
            if (key == "batch-cli-export" && tab.TabPages.Count > 1) tab.SelectedIndex = 1;
            if (key == "rename-tool-organize" && tab.TabPages.Count > 1) tab.SelectedIndex = 1;
            if (key == "rename-tool-unpack" && tab.TabPages.Count > 2) tab.SelectedIndex = 2;
        }

        if (key == "batch-copy") SeedBatchCopy(form);
        if (key == "source-file-picker") SeedSourceFilePicker(form);
        if (key == "vk-manager") SeedVkManager(form);
        if (key == "batch-cli-merge" || key == "batch-cli-export") SeedBatchCli(form, key);
        if (key.StartsWith("rename-tool-")) SeedRenameTool(form, key);

        if (key == "vk-blank-menu")
        {
            var panel = FindControl<FlowLayoutPanel>(form);
            panel?.ContextMenuStrip?.Show(panel, new Point(20, 20));
        }

        if (key == "vk-button-menu")
        {
            var widget = FindControl<VirtualButtonWidget>(form);
            var method = form.GetType().GetMethod("OnWidgetContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (widget != null && method != null)
            {
                method.Invoke(form, [widget, new Point(8, 8)]);
            }
        }

        if (key == "tray-menu")
        {
            var field = form.GetType().GetField("_trayMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(form) is ContextMenuStrip menu)
            {
                menu.Show(form, new Point(20, 20));
            }
        }

    }

    private static void SeedBatchCopy(Form form)
    {
        var sourceFiles = GetField<List<string>>(form, "_sourceFiles");
        sourceFiles?.AddRange([@"D:\assets\201.png", @"D:\assets\zzps-skin.json", @"D:\assets\run.atlas"]);
        Invoke(form, "RefreshSourceList");
        if (GetField<ComboBox>(form, "_cmbPrefix") is { } prefix) prefix.Text = @"D:\exports";
        if (GetField<TextBox>(form, "_txtMiddle") is { } middle) middle.Text = "targets1" + Environment.NewLine + "targets2" + Environment.NewLine + "targets3" + Environment.NewLine + "targets4";
        if (GetField<ComboBox>(form, "_cmbSuffix") is { } suffix) suffix.Text = "images";
        Invoke(form, "UpdatePreview");
        Invoke(form, "SetProgress", 4, 10, "复制中: zzps-skin.json -> targets4");
    }

    private static void SeedVkManager(Form form)
    {
        var grid = GetField<DataGridView>(form, "_dgv");
        if (grid == null || grid.Columns.Count < 6) return;

        grid.Rows.Clear();
        grid.Rows.Add("主窗口", "Spine.exe", 6, true, "显示", "×");
        grid.Rows.Add("权重工具", "Spine.exe", 4, true, "显示", "×");
        grid.Rows.Add("导出辅助", "-", 3, false, "显示", "×");
        foreach (DataGridViewRow row in grid.Rows)
            row.Tag = row.Cells[0].Value?.ToString();
    }

    private static void SeedSourceFilePicker(Form form)
    {
        if (GetField<TextBox>(form, "_txtDir") is { } dir)
            dir.Text = @"D:\assets\images";
        var imageList = GetField<ImageList>(form, "_imageList");
        var listView = GetField<ListView>(form, "_lvThumbnails");
        if (imageList == null || listView == null) return;

        imageList.Images.Clear();
        listView.Items.Clear();
        for (var i = 1; i <= 18; i++)
        {
            var thumb = new Bitmap(96, 96);
            using (var g = Graphics.FromImage(thumb))
            {
                var a = i % 2 == 0 ? Color.FromArgb(0x62, 0x62, 0x66) : Color.FromArgb(0x50, 0x50, 0x54);
                var b = i % 2 == 0 ? Color.FromArgb(0x55, 0x55, 0x59) : Color.FromArgb(0x68, 0x68, 0x6C);
                using var brushA = new SolidBrush(a);
                using var brushB = new SolidBrush(b);
                for (var y = 0; y < 4; y++)
                for (var x = 0; x < 5; x++)
                    g.FillRectangle(((x + y) % 2 == 0) ? brushA : brushB, x * 20, y * 24, 20, 24);
            }
            imageList.Images.Add(thumb);
            var name = i <= 3 ? "201.png" : $"image_{i}.png";
            var item = new ListViewItem(name)
            {
                ImageIndex = imageList.Images.Count - 1,
                Tag = $@"D:\assets\images\{name}",
                Checked = i <= 3
            };
            listView.Items.Add(item);
        }
        if (GetField<Label>(form, "_lblCount") is { } count)
            count.Text = "已选 3 个文件";
    }

    private static void SeedBatchCli(Form form, string key)
    {
        if (GetField<TextBox>(form, "_txtSpinePath") is { } spine) spine.Text = @"D:\Program Files\Spine\Spine.com";
        if (key == "batch-cli-merge")
        {
            AddListViewRows(GetField<ListView>(form, "_lvSource"), [
                ["G5.json", "全部", @"projects\G5.json"],
                ["ribbon.json", "walk,idle", @"projects\ribbon.json"]
            ]);
            AddListViewRows(GetField<ListView>(form, "_lvTarget"), [["G5.spine", @"projects\G5.spine"]]);
            if (GetField<TextBox>(form, "_txtFromName") is { } from) from.Text = "默认";
            if (GetField<TextBox>(form, "_txtToName") is { } to) to.Text = "默认";
            if (GetField<CheckBox>(form, "_chkExperimental") is { } exp) exp.Checked = true;
            Invoke(form, "ShowProgress", true);
            Invoke(form, "SetProgress", 2, 5, "实验合并: G5_target_copy.spine");
        }
        else
        {
            AddListViewRows(GetField<ListView>(form, "_lvExportFiles"), [
                ["G5.spine", "✓", @"projects\G5.spine"],
                ["ribbon.spine", "✓", @"projects\ribbon.spine"],
                ["demo.spine", "×", @"projects\demo.spine"]
            ]);
            if (GetField<TextBox>(form, "_txtSourceDir") is { } src) src.Text = @"D:\projects";
            if (GetField<TextBox>(form, "_txtOutputDir") is { } outDir) outDir.Text = @"D:\exports";
            Invoke(form, "ShowProgress", true);
            Invoke(form, "SetProgress", 2, 5, "批量导出: ribbon.spine");
        }
    }

    private static void SeedRenameTool(Form form, string key)
    {
        var list = key switch
        {
            "rename-tool-rename" => FindByName<ListBox>(form, "listBox1"),
            "rename-tool-organize" => FindByName<ListBox>(form, "listBox2"),
            "rename-tool-unpack" => FindByName<ListBox>(form, "listBox3"),
            _ => null
        };
        if (list != null)
        {
            list.Items.Clear();
            list.Items.AddRange(key switch
            {
                "rename-tool-rename" => [@"D:\assets\a.png", @"D:\assets\b.png", @"D:\assets\c.png"],
                "rename-tool-organize" => ["G5.skel.bytes", "G5.atlas.txt", "G5.png"],
                _ => ["G5.atlas", "ribbon.atlas", "skin.atlas"]
            });
        }
        if (FindByName<TextBox>(form, "textBox3") is { } folder) folder.Text = @"D:\assets";
        if (FindByName<TextBox>(form, "textBox4") is { } newName) newName.Text = "new_name";
        if (FindByName<TextBox>(form, "textBox1") is { } oldText) oldText.Text = "old";
        if (FindByName<TextBox>(form, "textBox2") is { } newText) newText.Text = "new";
        if (FindByName<TextBox>(form, "textBox5") is { } srcFolder) srcFolder.Text = @"D:\apk\spine";
        if (FindByName<TextBox>(form, "textBox6") is { } saveFolder) saveFolder.Text = @"D:\organized";
        if (FindByName<TextBox>(form, "textBox7") is { } atlasFolder) atlasFolder.Text = @"D:\atlas";
        if (FindByName<CheckBox>(form, "checkBox1") is { } cb) cb.Checked = true;
        SeedProgress(form, key);
    }

    private static void SeedProgress(Form form, string key)
    {
        var labelName = key switch { "rename-tool-rename" => "_renameProgressLabel", "rename-tool-organize" => "_organizeProgressLabel", _ => "_unpackProgressLabel" };
        var barName = key switch { "rename-tool-rename" => "_renameProgressBar", "rename-tool-organize" => "_organizeProgressBar", _ => "_unpackProgressBar" };
        if (GetField<Label>(form, labelName) is { } label) label.Text = key switch { "rename-tool-rename" => "局部替换: b.png", "rename-tool-organize" => "整理: G5.atlas.txt", _ => "解包: ribbon.atlas" };
        if (GetField<KeyMacro.Controls.TextProgressBar>(form, barName) is { } bar) { bar.Maximum = 3; bar.Value = 2; bar.ProgressText = "2/3"; }
    }

    private static void AddListViewRows(ListView? list, string[][] rows)
    {
        if (list == null) return;
        list.Items.Clear();
        foreach (var row in rows)
        {
            var item = new ListViewItem(row[0]);
            foreach (var value in row.Skip(1)) item.SubItems.Add(value);
            list.Items.Add(item);
        }
    }

    private static T? GetField<T>(object target, string name) where T : class
    {
        return target.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(target) as T;
    }

    private static void Invoke(object target, string name, params object[] args)
    {
        target.GetType().GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(target, args);
    }

    private static T? FindByName<T>(Control root, string name) where T : Control
    {
        if (root is T matched && root.Name == name) return matched;
        foreach (Control child in root.Controls)
        {
            var result = FindByName<T>(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static T? FindControl<T>(Control root) where T : Control
    {
        if (root is T matched) return matched;
        foreach (Control child in root.Controls)
        {
            var result = FindControl<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private static MacroSequence CreateSampleSequence()
    {
        return new MacroSequence
        {
            Name = "示例序列",
            TriggerHotkey = "Ctrl+Alt+Q",
            TriggerVkButtonName = "主窗口/按钮1",
            Steps =
            [
                new MacroStep
                {
                    Type = StepType.Key,
                    Keys = "A",
                    DelayMs = 100,
                    PressMode = PressMode.Tap,
                    HoldDurationMs = 0
                },
                new MacroStep
                {
                    Type = StepType.Combo,
                    Keys = "Ctrl+S",
                    DelayMs = 150,
                    PressMode = PressMode.Hold,
                    HoldDurationMs = 300
                },
                new MacroStep
                {
                    Type = StepType.Text,
                    Keys = "skin_G5",
                    DelayMs = 60,
                    PressMode = PressMode.Tap,
                    HoldDurationMs = 0
                },
                new MacroStep
                {
                    Type = StepType.Combo,
                    Keys = "Win+E",
                    DelayMs = 100,
                    PressMode = PressMode.Hold,
                    HoldDurationMs = 300
                }
            ]
        };
    }

    private static List<SpineHotkeyEntry> CreateSampleSpineHotkeys()
    {
        return
        [
            new SpineHotkeyEntry { Name = "Translate X", Keys = "X", ChineseNote = "平移 X 轴" },
            new SpineHotkeyEntry { Name = "Rotate", Keys = "R", ChineseNote = "旋转工具" },
            new SpineHotkeyEntry { Name = "Scale", Keys = "S", ChineseNote = "缩放工具" },
            new SpineHotkeyEntry { Name = "File / Save", Keys = "Ctrl+S", ChineseNote = "保存项目" }
        ];
    }

    private static Form CreateSampleAnimationSelectDialog()
    {
        var dialog = new Form
        {
            Text = "选择动画 - G5.json",
            Size = new Size(400, 450),
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.Sizable,
            BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA)
        };

        var clb = new DarkCheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = UiTheme.List
        };
        foreach (var item in new[] { "walk", "idle", "attack", "run", "hit" })
            clb.Items.Add(item, item is "walk" or "idle");

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4)
        };
        bottomPanel.Controls.Add(new Button { Text = "取消", AutoSize = true, MinimumSize = new Size(80, 32), FlatStyle = FlatStyle.Flat });
        bottomPanel.Controls.Add(new Button { Text = "确认", AutoSize = true, MinimumSize = new Size(80, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0x00, 0x78, 0xD7), ForeColor = Color.White });

        dialog.Controls.Add(clb);
        dialog.Controls.Add(bottomPanel);
        UiTheme.Apply(dialog, UiWindowProfile.AnimationSelect);
        return dialog;
    }

    private static VirtualKeyWindow CreateSampleVirtualKeyWindow()
    {
        var data = new VirtualLayoutSerializer.WindowLayoutData
        {
            Name = "截图测试",
            Enabled = true,
            Buttons =
            [
                new VirtualButton { Name = "按钮1", StyleType = VirtualButtonStyle.SmallIcon },
                new VirtualButton { Name = "循环按钮", StyleType = VirtualButtonStyle.LoopIcon, LoopInterval = 100 }
            ]
        };
        return new VirtualKeyWindow(new VirtualLayoutSerializer(), data, [], null);
    }

    private static void Capture(Form form, string output, string key)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        form.TopMost = true;
        form.BringToFront();
        form.Activate();
        Application.DoEvents();
        Thread.Sleep(150);
        Application.DoEvents();

        var bounds = form.Bounds;
        if (key.Contains("menu", StringComparison.OrdinalIgnoreCase))
        {
            bounds = new Rectangle(bounds.Left, bounds.Top, Math.Max(bounds.Width, 420), Math.Max(bounds.Height, 560));
        }

        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size);
        bitmap.Save(output, ImageFormat.Png);
    }

    private static void CloseForm(Form form, string key)
    {
        if (key.Contains("menu", StringComparison.OrdinalIgnoreCase) || key == "mainform-layout")
        {
            Environment.Exit(0);
        }

        if (form is MainForm)
        {
            var method = form.GetType().GetMethod("ExitApp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method?.Invoke(form, null);
            return;
        }

        form.Close();
    }
}









