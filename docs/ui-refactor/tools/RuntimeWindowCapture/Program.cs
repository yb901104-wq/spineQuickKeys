using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
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
            "rename-tool-rename" => new Form1(),
            "rename-tool-organize" => new Form1(),
            "rename-tool-unpack" => new Form1(),
            "hotkey-recorder" => new HotkeyRecorderForm(allowNoModifier: true),
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
            new SpineHotkeyEntry { Name = "Scale", Keys = "S", ChineseNote = "缩放工具" }
        ];
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
        form.Activate();
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
        if (key.Contains("menu", StringComparison.OrdinalIgnoreCase))
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









