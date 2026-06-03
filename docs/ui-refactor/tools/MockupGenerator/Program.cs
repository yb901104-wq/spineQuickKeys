using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace UiRefactorMockupGenerator;

internal static class Program
{
    private static readonly FontFamily UiFont = new("Microsoft YaHei UI");
    private static readonly FontFamily MonoFont = new("Consolas");
    private static readonly Color App = C("#2B2B2B");
    private static readonly Color Workspace = C("#3A3A3C");
    private static readonly Color Panel = C("#454547");
    private static readonly Color PanelAlt = C("#505052");
    private static readonly Color Input = C("#262628");
    private static readonly Color Border = C("#5E5E60");
    private static readonly Color BorderStrong = C("#727274");
    private static readonly Color Text = C("#E6E6E6");
    private static readonly Color Muted = C("#B8B8B8");
    private static readonly Color Disabled = C("#777777");
    private static readonly Color Blue = C("#2388C9");
    private static readonly Color Cyan = C("#39C5D8");
    private static readonly Color Orange = C("#F08A3C");
    private static readonly Color Green = C("#6BBF59");
    private static readonly Color Red = C("#D95C5C");

    [STAThread]
    private static void Main()
    {
        var outDir = Path.Combine(Directory.GetCurrentDirectory(), "docs", "ui-refactor", "mockups");
        Directory.CreateDirectory(outDir);

        Save(outDir, "00-style-board.png", DrawStyleBoard);
        Save(outDir, "01-main-window.png", DrawMainWindow);
        Save(outDir, "02-sequence-editor.png", DrawSequenceEditor);
        Save(outDir, "03-hotkey-recorder.png", DrawHotkeyRecorder);
        Save(outDir, "04-spine-hotkey-editor.png", DrawSpineHotkeyEditor);
        Save(outDir, "05-vk-manager.png", DrawVkManager);
        Save(outDir, "06-batch-copy.png", DrawBatchCopy);
        Save(outDir, "07-source-file-picker.png", DrawSourceFilePicker);
        Save(outDir, "08-conflict-dialog.png", DrawConflictDialog);
        Save(outDir, "09-cli-merge.png", DrawCliMerge);
        Save(outDir, "10-cli-export.png", DrawCliExport);
        Save(outDir, "11-cli-animation-select.png", DrawAnimationSelect);
        Save(outDir, "12-rename-tool-rename.png", DrawRenameToolRename);
        Save(outDir, "13-rename-tool-organize.png", DrawRenameToolOrganize);
        Save(outDir, "14-rename-tool-unpack.png", DrawRenameToolUnpack);
        Save(outDir, "15-input-dialog.png", DrawInputDialog);
        Save(outDir, "16-subfolder-select.png", DrawSubfolderSelect);
        Save(outDir, "17-core-dialogs-and-menus.png", DrawCoreDialogsAndMenus);

        Console.WriteLine($"Generated mockups: {outDir}");
    }

    private static void Save(string outDir, string fileName, Action<Graphics, Rectangle> draw)
    {
        using var bmp = new Bitmap(1440, 900, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        draw(g, new Rectangle(0, 0, bmp.Width, bmp.Height));
        var path = Path.Combine(outDir, fileName);
        bmp.Save(path, ImageFormat.Png);
        Console.WriteLine(path);
    }

    private static void DrawStyleBoard(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "KeyMacro UI Style Board / Spine dark workbench");
        Header(g, 38, "全局视觉语言", "深灰工作台、紧凑面板、细边框、蓝色强调态、橙色警示、青色工具态");
        var left = new Rectangle(36, 120, 520, 700);
        var mid = new Rectangle(588, 120, 380, 700);
        var right = new Rectangle(1000, 120, 388, 700);
        PanelBox(g, left, "颜色 Token");
        var colors = new (string, Color)[] {
            ("Bg.App", App), ("Bg.Workspace", Workspace), ("Bg.Panel", Panel), ("Bg.Input", Input),
            ("Text.Primary", Text), ("Text.Secondary", Muted), ("Accent.Blue", Blue), ("Accent.Cyan", Cyan),
            ("Accent.Orange", Orange), ("Accent.Green", Green), ("Accent.Red", Red)
        };
        for (var i = 0; i < colors.Length; i++)
        {
            var y = left.Y + 54 + i * 54;
            FillRound(g, new Rectangle(left.X + 24, y, 84, 34), colors[i].Item2, 5);
            StrokeRound(g, new Rectangle(left.X + 24, y, 84, 34), BorderStrong, 5);
            Label(g, colors[i].Item1, left.X + 128, y + 7, 10, Text);
            Label(g, ToHex(colors[i].Item2), left.X + 330, y + 7, 10, Muted);
        }

        PanelBox(g, mid, "控件样式");
        Button(g, new Rectangle(mid.X + 26, mid.Y + 64, 140, 34), "主操作", Blue);
        Button(g, new Rectangle(mid.X + 186, mid.Y + 64, 140, 34), "危险操作", Red);
        Button(g, new Rectangle(mid.X + 26, mid.Y + 116, 140, 34), "普通按钮", PanelAlt);
        InputBox(g, new Rectangle(mid.X + 26, mid.Y + 170, 300, 34), "搜索 / 路径 / 快捷键");
        Progress(g, new Rectangle(mid.X + 26, mid.Y + 240, 300, 24), 0.58f, "7/12");
        Table(g, new Rectangle(mid.X + 26, mid.Y + 310, 318, 230),
            ["名称", "状态", "绑定"], [
                ["G5 绑定", "启用", "Ctrl+G"],
                ["导出序列", "循环", "VK/播放"],
                ["测试动作", "暂停", "-"]
            ]);
        Label(g, "规则：紧凑但不拥挤，信息优先，装饰克制。", mid.X + 26, mid.Y + 580, 10, Muted);

        PanelBox(g, right, "面板与菜单");
        DrawChecker(g, new Rectangle(right.X + 24, right.Y + 60, 340, 160));
        ToolStrip(g, new Rectangle(right.X + 24, right.Y + 244, 340, 36), ["新增", "编辑", "删除", "导出"]);
        ContextMenu(g, new Rectangle(right.X + 86, right.Y + 320, 220, 226), ["修改名称", "循环延迟", "按钮间距", "强制停止", "删除按钮"], 3);
        Label(g, "弹窗使用同一深灰面板，核心按钮靠右，危险项红色。", right.X + 24, right.Y + 590, 10, Muted);
    }

    private static void DrawMainWindow(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "spine宏助手 (TANRY) V3 UI Concept");
        MainToolbar(g, new Rectangle(18, 46, 1404, 42));
        PanelBox(g, new Rectangle(18, 104, 1404, 758), "宏序列列表");
        Table(g, new Rectangle(38, 154, 1364, 650),
            ["启用", "选择", "名称", "触发快捷键", "目标软件", "间隔(ms)", "循环(次)", "步骤", "清除"],
            [
                ["✓", "选择", "G5 常用绑定", "Ctrl+Shift+G", "Spine.exe", "0", "1", "5", "清除"],
                ["✓", "选择", "循环预览", "虚拟按键/播放", "Spine.exe", "120", "无限", "3", "清除"],
                ["", "选择", "导出准备", "-", "不限", "0", "1", "4", "清除"],
                ["✓", "选择", "清理 UI", "Alt+Q", "Spine.exe", "80", "2", "6", "清除"]
            ],
            [0.45f, 0.55f, 1.35f, 1.35f, 1.15f, 0.85f, 0.85f, 0.5f, 0.6f]);
        Status(g, new Rectangle(18, 870, 1404, 20), "就绪 | 配置: AppData | VK 窗口: 2 | 热键: 4");
    }

    private static void DrawSequenceEditor(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "序列编辑器");
        PanelBox(g, new Rectangle(24, 50, 1392, 148), "触发与绑定");
        Label(g, "序列名称", 48, 110, 10, Muted); InputBox(g, new Rectangle(150, 102, 420, 34), "G5 常用绑定");
        Label(g, "触发快捷键", 48, 156, 10, Muted); InputBox(g, new Rectangle(150, 148, 420, 34), "Ctrl+Shift+G");
        Button(g, new Rectangle(590, 148, 116, 34), "键盘录入", Blue);
        Button(g, new Rectangle(720, 148, 104, 34), "虚拟按键", PanelAlt);
        Button(g, new Rectangle(838, 148, 82, 34), "清除", PanelAlt);
        Label(g, "关联虚拟按键", 850, 110, 10, Muted); InputBox(g, new Rectangle(964, 102, 320, 34), "主窗口/播放");

        PanelBox(g, new Rectangle(24, 216, 1392, 660), "步骤列表");
        ToolStrip(g, new Rectangle(44, 262, 720, 36), ["添加步骤", "删除步骤", "录制按键", "上移", "下移"]);
        Table(g, new Rectangle(44, 314, 1372, 520),
            ["类型", "按键/文本", "延迟(ms)", "触发方式", "按压时长(ms)", "操作"],
            [
                ["组合键", "Ctrl+S", "80", "点按", "0", "复制"],
                ["单键", "F5", "120", "点按", "0", "复制"],
                ["文本", "skin_G5", "60", "点按", "0", "复制"],
                ["组合键", "Win+E", "100", "长按", "300", "复制"]
            ],
            [0.9f, 1.4f, 0.75f, 0.9f, 1.0f, 0.55f]);
        Button(g, new Rectangle(1194, 838, 90, 36), "取消", PanelAlt);
        Button(g, new Rectangle(1296, 838, 90, 36), "确定", Blue);
    }

    private static void DrawHotkeyRecorder(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DialogShell(g, new Rectangle(430, 220, 580, 360), "键盘录入");
        Label(g, "等待按键输入", 570, 294, 18, Text);
        Label(g, "按下一个单键或组合键，录制结果会显示在下方。", 500, 340, 10, Muted);
        Pill(g, new Rectangle(560, 392, 320, 48), "Ctrl + Shift + G", Blue);
        Button(g, new Rectangle(728, 502, 100, 36), "取消", PanelAlt);
        Button(g, new Rectangle(842, 502, 100, 36), "确认", Blue);
    }

    private static void DrawSpineHotkeyEditor(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "Spine 热键编辑器");
        PanelBox(g, new Rectangle(20, 48, 1400, 92), "TXT 文件");
        InputBox(g, new Rectangle(42, 99, 720, 32), @"D:\Spine\hotkeys-1.txt");
        Button(g, new Rectangle(778, 99, 110, 32), "载入文件", Blue);
        Button(g, new Rectangle(904, 99, 110, 32), "录制按键", PanelAlt);
        InputBox(g, new Rectangle(1030, 99, 360, 32), "搜索: rotate");

        PanelBox(g, new Rectangle(20, 156, 1400, 720), "快捷键列表");
        Table(g, new Rectangle(42, 206, 1360, 606),
            ["名称", "快捷键", "中文说明"],
            [
                ["Transform / Rotate", "R", "旋转工具"],
                ["Transform / Translate", "T", "移动工具"],
                ["Animation / Key Dopesheet", "Ctrl+K", "记录关键帧"],
                ["File / Save", "Ctrl+S", "保存项目"]
            ],
            [1.8f, 1.0f, 1.8f]);
        Button(g, new Rectangle(1194, 826, 90, 36), "取消", PanelAlt);
        Button(g, new Rectangle(1296, 826, 90, 36), "保存", Blue);
    }

    private static void DrawVirtualKeyHorizontal(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        Header(g, 40, "虚拟按键窗口 - 横向 / 已捕获目标", "浮动窗口保持紧凑皮肤，但与全局暗色风格一致");
        var win = new Rectangle(230, 270, 980, 220);
        Fill(g, win, C("#333335")); Stroke(g, win, BorderStrong);
        Fill(g, new Rectangle(win.X, win.Y, win.Width, 32), C("#242426"));
        Label(g, "[Spine.exe] 虚拟按键 (6)", win.X + 14, win.Y + 8, 10, Text);
        var x = win.X + 28;
        VirtualButton(g, new Rectangle(x, win.Y + 74, 112, 72), "姿势", Cyan, false); x += 126;
        VirtualButton(g, new Rectangle(x, win.Y + 74, 112, 72), "权重", Orange, false); x += 126;
        VirtualButton(g, new Rectangle(x, win.Y + 74, 138, 72), "循环预览", Green, true); x += 154;
        VirtualButton(g, new Rectangle(x, win.Y + 74, 112, 72), "本地", Blue, false); x += 126;
        VirtualButton(g, new Rectangle(x, win.Y + 74, 112, 72), "世界", Blue, false); x += 126;
            FillRound(g, new Rectangle(x + 10, win.Y + 72, 28, 76), C("#1F1F21"), 5); StrokeRound(g, new Rectangle(x + 10, win.Y + 72, 28, 76), Border, 5);
        Label(g, "目标窗口: G5-skin | 右键菜单期间暂停循环发送", win.X + 28, win.Y + 166, 9, Muted);
    }

    private static void DrawVirtualKeyVerticalMenu(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        Header(g, 40, "虚拟按键窗口 - 竖向 / 右键菜单", "菜单分组清楚，强制停止和删除保持危险态");
        var win = new Rectangle(460, 126, 300, 700);
        Fill(g, win, C("#333335")); Stroke(g, win, BorderStrong);
        Fill(g, new Rectangle(win.X, win.Y, win.Width, 32), C("#242426"));
        Label(g, "[Spine.exe] 虚拟按键", win.X + 12, win.Y + 8, 10, Text);
        var y = win.Y + 58;
        foreach (var name in new[] { "姿势", "权重", "循环预览", "本地", "父级", "世界" })
        {
            VirtualButton(g, new Rectangle(win.X + 42, y, 210, 62), name, name == "循环预览" ? Green : Blue, name == "循环预览");
            y += 76;
        }
        ContextMenu(g, new Rectangle(790, 196, 270, 386),
            ["[ 循环预览 ]", "修改按钮名称", "按钮循环延迟", "按钮间距", "强制停止", "删除当前按钮"], 4);
        ContextMenu(g, new Rectangle(1080, 196, 270, 420),
            ["增加按钮", "删除所有按钮", "置顶", "透明度 80%", "捕获目标窗口", "竖向模式 ✓", "缩放 100%", "关闭窗口"], 1);
    }

    private static void DrawVkManager(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "虚拟按键管理器");
        PanelBox(g, new Rectangle(24, 56, 1392, 762), "窗口列表");
        Table(g, new Rectangle(46, 104, 1368, 630),
            ["允许显示", "窗口名称", "目标进程", "按钮数", "状态", "操作"],
            [
                ["✓", "主窗口", "Spine.exe", "6", "显示中", "显示 / 删除"],
                ["✓", "权重工具", "Spine.exe", "4", "隐藏", "显示 / 删除"],
                ["", "导出辅助", "-", "3", "隐藏", "显示 / 删除"]
            ],
            [0.8f, 1.4f, 1.4f, 0.8f, 0.9f, 1.2f]);
        Button(g, new Rectangle(1128, 760, 110, 36), "新增窗口", Blue);
        Button(g, new Rectangle(1254, 760, 110, 36), "关闭", PanelAlt);
    }

    private static void DrawBatchCopy(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "文件批量复制");
        PanelBox(g, new Rectangle(24, 54, 1392, 236), "源文件");
        ToolStrip(g, new Rectangle(44, 92, 520, 34), ["选择文件", "移除选中", "清空列表"]);
        Table(g, new Rectangle(44, 138, 1368, 136), ["文件名", "路径"], [["201.png", @"source\201.png"], ["zzps-skin.json", @"source\zzps-skin.json"], ["run.atlas", @"source\run.atlas"]]);
        PanelBox(g, new Rectangle(24, 306, 1392, 410), "目标路径");
        InputBox(g, new Rectangle(116, 354, 600, 32), @"D:\exports");
        Label(g, "前缀", 46, 361, 10, Muted);
        Button(g, new Rectangle(730, 354, 92, 32), "选择文件夹", PanelAlt);
        TextArea(g, new Rectangle(116, 400, 600, 116), "targets1\ntargets2\ntargets3\ntargets4");
        Label(g, "中间", 46, 408, 10, Muted);
        Button(g, new Rectangle(730, 400, 92, 32), "添加行", PanelAlt);
        Button(g, new Rectangle(730, 442, 92, 32), "删除行", PanelAlt);
        InputBox(g, new Rectangle(116, 532, 600, 32), "images");
        Label(g, "后缀", 46, 539, 10, Muted);
        Button(g, new Rectangle(730, 574, 92, 32), "清理历史", PanelAlt);
        Table(g, new Rectangle(880, 354, 500, 220), ["预览目标"], [[@"D:\exports\targets1\images"], [@"D:\exports\targets2\images"], [@"D:\exports\targets3\images"]]);
        Button(g, new Rectangle(24, 734, 1368, 42), "开始复制", Green);
        Label(g, "复制中: zzps-skin.json -> targets4", 24, 798, 10, Text, 1368, align: StringAlignment.Center);
        Progress(g, new Rectangle(24, 824, 1368, 26), .4f, "4/10");
    }

    private static void DrawSourceFilePicker(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "源文件选择器");
        PanelBox(g, new Rectangle(24, 56, 1392, 760), "缩略图浏览");
        InputBox(g, new Rectangle(48, 102, 780, 32), @"D:\assets\images");
        Button(g, new Rectangle(846, 102, 82, 32), "浏览", Blue);
        Button(g, new Rectangle(940, 102, 82, 32), "刷新", PanelAlt);
        var grid = new Rectangle(48, 158, 1350, 600);
        FillRound(g, grid, Workspace, 5);
        StrokeRound(g, grid, Border, 5);
        for (var i = 0; i < 18; i++)
        {
            var col = i % 6;
            var row = i / 6;
            var cell = new Rectangle(grid.X + 26 + col * 218, grid.Y + 24 + row * 178, 178, 146);
            FillRound(g, cell, Panel, 5);
            StrokeRound(g, cell, i < 3 ? Blue : Border, 5);
            DrawChecker(g, new Rectangle(cell.X + 12, cell.Y + 12, 154, 88));
            Label(g, i < 3 ? "✓ 201.png" : $"image_{i + 1}.png", cell.X + 12, cell.Y + 106, 9, i < 3 ? Text : Muted);
        }
        Label(g, "已选 3 个文件", 48, 786, 10, Muted);
        Button(g, new Rectangle(1188, 776, 90, 36), "取消", PanelAlt);
        Button(g, new Rectangle(1292, 776, 90, 36), "确认", Blue);
    }

    private static void DrawConflictDialog(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DialogShell(g, new Rectangle(360, 160, 720, 560), "复制冲突");
        WarningStripe(g, new Rectangle(390, 226, 660, 62), "目标目录存在同名文件");
        Label(g, @"D:\exports\targets3\images", 390, 314, 10, Muted);
        Table(g, new Rectangle(390, 352, 660, 230), ["冲突文件", "处理建议"], [["201.png", "可覆盖"], ["zzps-skin.json", "可跳过"]]);
        Button(g, new Rectangle(480, 632, 120, 38), "打开文件夹", PanelAlt);
        Button(g, new Rectangle(616, 632, 120, 38), "跳过", PanelAlt);
        Button(g, new Rectangle(752, 632, 132, 38), "取消全部", Red);
        Button(g, new Rectangle(900, 632, 120, 38), "覆盖", Blue);
    }

    private static void DrawCliMerge(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "CLI 批量合并/导出 - 合并");
        DrawCliTop(g);
        Tabs(g, new Rectangle(24, 104, 1392, 44), ["合并", "批量导出"], 0);
        PanelBox(g, new Rectangle(24, 150, 1392, 640), "合并任务");
        Table(g, new Rectangle(46, 206, 620, 330), ["源文件", "动画", "路径"], [["G5.json", "全部", @"projects\G5.json"], ["ribbon.json", "walk,idle", @"projects\ribbon.json"]]);
        Table(g, new Rectangle(720, 206, 640, 330), ["目标文件", "路径"], [["G5.spine", @"projects\G5.spine"]]);
        ToolStrip(g, new Rectangle(46, 558, 600, 36), ["添加源文件", "删除源文件", "动画选择"]);
        ToolStrip(g, new Rectangle(720, 558, 460, 36), ["添加目标文件", "删除目标文件"]);
        InputBox(g, new Rectangle(120, 628, 220, 32), "默认");
        Label(g, "--from", 48, 636, 10, Muted);
        InputBox(g, new Rectangle(792, 628, 220, 32), "默认");
        Label(g, "--to", 720, 636, 10, Muted);
        Button(g, new Rectangle(970, 690, 142, 38), "执行合并", Blue);
        CheckOption(g, new Rectangle(1128, 692, 282, 34), "实验功能: CLI骨架合并(4.3)", true, Orange);
        Label(g, "实验合并: G5_target_copy.spine", 24, 812, 10, Text, 1392, align: StringAlignment.Center);
        Progress(g, new Rectangle(24, 838, 1392, 26), .4f, "2/5");
    }

    private static void DrawCliExport(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "CLI 批量合并/导出 - 批量导出");
        DrawCliTop(g);
        Tabs(g, new Rectangle(24, 104, 1392, 44), ["合并", "批量导出"], 1);
        PanelBox(g, new Rectangle(24, 150, 1392, 640), "批量导出");
        InputBox(g, new Rectangle(132, 204, 720, 32), @"manual_test_assets\cli\projects");
        Label(g, "源目录", 48, 212, 10, Muted);
        Button(g, new Rectangle(866, 204, 82, 32), "浏览", PanelAlt);
        Button(g, new Rectangle(960, 204, 82, 32), "扫描", Blue);
        Table(g, new Rectangle(48, 258, 1350, 360), ["文件名", "export.json", "路径"], [["G5.spine", "✓ finish.export.json", @"projects\G5.spine"], ["ribbon_test2.json", "缺省导出", @"projects\ribbon_test2.json"]]);
        Button(g, new Rectangle(48, 634, 104, 42), "刷新状态", PanelAlt);
        Label(g, "导出配置", 174, 648, 10, Muted);
        CheckOption(g, new Rectangle(260, 634, 220, 42), "finish.export.json", true, Blue);
        CheckOption(g, new Rectangle(494, 634, 230, 42), "demotion.export.json", false, BorderStrong);
        CheckOption(g, new Rectangle(738, 634, 96, 42), "其他", false, BorderStrong);
        InputBox(g, new Rectangle(132, 704, 720, 32), @"docs\verification\cli-output");
        Label(g, "输出目录", 48, 712, 10, Muted);
        Button(g, new Rectangle(866, 704, 82, 32), "浏览", PanelAlt);
        Button(g, new Rectangle(1148, 704, 110, 38), "导出", Blue);
        Button(g, new Rectangle(1272, 704, 110, 38), "单纹理图", C("#6B46C3"));
        Label(g, "批量导出: G5.spine", 24, 812, 10, Text, 1392, align: StringAlignment.Center);
        Progress(g, new Rectangle(24, 838, 1392, 26), .5f, "1/2");
    }

    private static void DrawAnimationSelect(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DialogShell(g, new Rectangle(430, 130, 580, 640), "选择动画 - G5.json");
        InputBox(g, new Rectangle(464, 198, 512, 32), "搜索动画");
        Table(g, new Rectangle(464, 252, 512, 400), ["选择", "动画名称"], [["✓", "atk"], ["✓", "atkfx"], ["", "die"], ["✓", "idle"], ["✓", "walk"]]);
        Button(g, new Rectangle(736, 694, 100, 36), "取消", PanelAlt);
        Button(g, new Rectangle(852, 694, 100, 36), "确认", Blue);
    }

    private static void DrawRenameToolRename(Graphics g, Rectangle r)
    {
        DrawRenameShell(g, "批量重命名 - 重命名", 0);
        Table(g, new Rectangle(42, 150, 760, 430), ["文件路径"], [[@"D:\assets\a.png"], [@"D:\assets\b.png"], [@"D:\assets\c.png"]]);
        PanelBox(g, new Rectangle(836, 150, 530, 430), "命名规则");
        Label(g, "全名替换", 866, 208, 10, Muted); InputBox(g, new Rectangle(966, 200, 220, 32), "new_name"); Button(g, new Rectangle(1204, 200, 120, 32), "统一重命名", Blue);
        Label(g, "关键词", 866, 310, 10, Muted); InputBox(g, new Rectangle(966, 302, 220, 32), "old");
        Label(g, "替换词", 866, 360, 10, Muted); InputBox(g, new Rectangle(966, 352, 220, 32), "new"); Button(g, new Rectangle(1204, 352, 120, 32), "局部替换", Blue);
        ToolStrip(g, new Rectangle(42, 612, 620, 36), ["选择文件", "选择文件夹", "清空列表"]);
        InputBox(g, new Rectangle(690, 612, 480, 34), @"D:\assets");
        Label(g, "局部替换: b.png", 42, 708, 10, Text, 1320, align: StringAlignment.Center);
        Progress(g, new Rectangle(42, 734, 1320, 26), .66f, "2/3");
    }

    private static void DrawRenameToolOrganize(Graphics g, Rectangle r)
    {
        DrawRenameShell(g, "批量重命名 - SPINE 文件整理", 1);
        Table(g, new Rectangle(42, 150, 1320, 320), ["待整理文件"], [[@"G5.skel.bytes"], [@"G5.atlas.txt"], [@"G5.png"]]);
        PanelBox(g, new Rectangle(42, 492, 1320, 168), "整理配置");
        Label(g, "源文件夹", 70, 538, 10, Muted); InputBox(g, new Rectangle(160, 530, 420, 32), @"D:\apk\spine"); Button(g, new Rectangle(596, 530, 104, 32), "源文件位置", PanelAlt);
        Label(g, "保存位置", 70, 590, 10, Muted); InputBox(g, new Rectangle(160, 582, 420, 32), @"D:\organized"); Button(g, new Rectangle(596, 582, 104, 32), "保存位置", PanelAlt);
        CheckOption(g, new Rectangle(760, 530, 360, 34), ".bytes 及 .txt 后缀处理", true, Cyan);
        Button(g, new Rectangle(1018, 582, 116, 36), "清空列表", PanelAlt);
        Button(g, new Rectangle(1148, 582, 116, 36), "开始整理", Blue);
        Label(g, "整理: G5.atlas.txt", 42, 708, 10, Text, 1320, align: StringAlignment.Center);
        Progress(g, new Rectangle(42, 734, 1320, 26), .66f, "2/3");
    }

    private static void DrawRenameToolUnpack(Graphics g, Rectangle r)
    {
        DrawRenameShell(g, "批量重命名 - SPINE 图集自动解包", 2);
        Table(g, new Rectangle(42, 150, 1320, 460), ["Atlas 文件"], [[@"G5.atlas"], [@"ribbon.atlas"], [@"skin.atlas"]]);
        PanelBox(g, new Rectangle(42, 630, 1320, 108), "解包操作");
        Label(g, "图集所在目标文件夹", 70, 694, 10, Muted); InputBox(g, new Rectangle(220, 686, 420, 32), @"D:\atlas"); Button(g, new Rectangle(666, 686, 128, 32), "选择目标文件夹", PanelAlt);
        Button(g, new Rectangle(890, 686, 110, 32), "清空列表", PanelAlt);
        Button(g, new Rectangle(1210, 686, 116, 32), "开始解包", Blue);
        Label(g, "解包: ribbon.atlas", 42, 772, 10, Text, 1320, align: StringAlignment.Center);
        Progress(g, new Rectangle(42, 798, 1320, 26), .66f, "2/3");
    }

    private static void DrawInputDialog(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DialogShell(g, new Rectangle(450, 240, 540, 300), "输入");
        Label(g, "请输入新的按钮名称", 490, 310, 11, Text);
        InputBox(g, new Rectangle(490, 356, 440, 36), "循环预览");
        Label(g, "名称会同步保存到 VK 布局。", 490, 410, 9, Muted);
        Button(g, new Rectangle(708, 476, 92, 36), "取消", PanelAlt);
        Button(g, new Rectangle(816, 476, 92, 36), "确认", Blue);
    }

    private static void DrawSubfolderSelect(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "子文件夹 / 文件选择");
        PanelBox(g, new Rectangle(24, 56, 1392, 760), "批量选择");
        ToolStrip(g, new Rectangle(48, 104, 620, 36), ["全选", "全不选", "反选"]);
        InputBox(g, new Rectangle(48, 158, 360, 32), "搜索: G5");
        InputBox(g, new Rectangle(430, 158, 360, 32), "不包含: temp");
        Table(g, new Rectangle(48, 212, 1368, 536), ["选择", "名称", "路径"], [["✓", "G5.spine", @"projects\G5.spine"], ["✓", "G5.json", @"projects\G5.json"], ["", "temp_test.spine", @"projects\temp_test.spine"]]);
        Button(g, new Rectangle(1190, 766, 96, 36), "取消", PanelAlt);
        Button(g, new Rectangle(1302, 766, 96, 36), "确认选择", Blue);
    }

    private static void DrawCoreDialogsAndMenus(Graphics g, Rectangle r)
    {
        Fill(g, r, App);
        DrawTitleBar(g, r, "核心弹窗与菜单样式");
        DialogShell(g, new Rectangle(58, 90, 430, 270), "删除确认");
        WarningStripe(g, new Rectangle(86, 154, 374, 58), "确定删除序列 \"循环预览\"？");
        Label(g, "此操作不可撤销。", 86, 236, 10, Muted);
        Button(g, new Rectangle(250, 302, 88, 34), "取消", PanelAlt);
        Button(g, new Rectangle(352, 302, 88, 34), "删除", Red);

        DialogShell(g, new Rectangle(530, 90, 430, 270), "导入确认");
        Label(g, "检测到 Spine 热键结构一致，推荐完整覆盖。", 558, 160, 10, Text);
        Label(g, "也可以按名称合并迁移，风险项将跳过。", 558, 202, 10, Muted);
        Button(g, new Rectangle(612, 302, 100, 34), "取消", PanelAlt);
        Button(g, new Rectangle(724, 302, 100, 34), "名称合并", PanelAlt);
        Button(g, new Rectangle(836, 302, 100, 34), "完整覆盖", Blue);

        DialogShell(g, new Rectangle(1002, 90, 380, 270), "操作完成");
        Label(g, "导出完成", 1032, 160, 14, Text);
        Label(g, @"文件已保存到 D:\exports\test.kmp", 1032, 204, 9, Muted);
        Button(g, new Rectangle(1260, 302, 88, 34), "确认", Blue);

        ContextMenu(g, new Rectangle(70, 468, 260, 280), ["显示主窗口", "暂停全部 ✓", "打开日志目录", "退出"], 3);
        Label(g, "托盘菜单", 70, 430, 12, Text);
        ContextMenu(g, new Rectangle(390, 468, 300, 360), ["增加按钮", "删除所有按钮", "置顶 ✓", "透明度", "捕获目标窗口", "缩放", "窗口锁定", "关闭窗口"], 1);
        Label(g, "VK 空白右键菜单", 390, 430, 12, Text);
        ContextMenu(g, new Rectangle(750, 468, 300, 316), ["[ 播放 ]", "修改按钮名称", "按钮循环延迟", "按钮间距", "强制停止", "删除当前按钮"], 4);
        Label(g, "VK 按钮右键菜单", 750, 430, 12, Text);
        DialogShell(g, new Rectangle(1100, 468, 300, 230), "修改按钮名称");
        Label(g, "输入新的按钮名称", 1124, 532, 10, Text);
        InputBox(g, new Rectangle(1124, 574, 248, 34), "播放");
        Button(g, new Rectangle(1184, 644, 78, 32), "取消", PanelAlt);
        Button(g, new Rectangle(1274, 644, 78, 32), "确认", Blue);
    }

    private static void DrawRenameShell(Graphics g, string title, int tab)
    {
        Fill(g, new Rectangle(0, 0, 1440, 900), App);
        DrawTitleBar(g, new Rectangle(0, 0, 1440, 900), title);
        Tabs(g, new Rectangle(24, 56, 1392, 44), ["重命名", "SPINE文件整理", "SPINE图集自动解包"], tab);
        PanelBox(g, new Rectangle(24, 104, 1392, 758), "");
    }

    private static void DrawCliTop(Graphics g)
    {
        PanelBox(g, new Rectangle(24, 48, 1392, 46), "");
        Label(g, "Spine.com 路径", 48, 62, 10, Muted);
        InputBox(g, new Rectangle(164, 56, 620, 32), @"D:\Program Files\Spine\Spine.com");
        Button(g, new Rectangle(800, 56, 76, 32), "检测", PanelAlt);
        Button(g, new Rectangle(888, 56, 76, 32), "选择", PanelAlt);
        Button(g, new Rectangle(976, 56, 90, 32), "取消CLI", PanelAlt);
        Pill(g, new Rectangle(1084, 58, 88, 28), "有效", Green);
    }

    private static void DrawTitleBar(Graphics g, Rectangle r, string title)
    {
        Fill(g, r, App);
        Fill(g, new Rectangle(r.X, r.Y, r.Width, 34), C("#222224"));
        Stroke(g, new Rectangle(r.X, r.Y, r.Width, 34), C("#121214"));
        Label(g, title, r.X + 14, r.Y + 8, 10, Text);
        DrawWindowControls(g, new Rectangle(r.Right - 92, r.Y + 7, 72, 20));
    }

    private static void Header(Graphics g, int y, string title, string subtitle)
    {
        Label(g, title, 36, y + 6, 20, Text);
        Label(g, subtitle, 38, y + 44, 10, Muted);
    }

    private static void PanelBox(Graphics g, Rectangle rect, string title)
    {
        FillRound(g, rect, Panel, 5);
        StrokeRound(g, rect, Border, 5);
        if (!string.IsNullOrWhiteSpace(title))
        {
            FillRound(g, new Rectangle(rect.X, rect.Y, rect.Width, 34), C("#343436"), 5);
            Stroke(g, new Rectangle(rect.X, rect.Y + 33, rect.Width, 1), Border);
            Label(g, title, rect.X + 12, rect.Y + 8, 10, Text);
        }
    }

    private static void DialogShell(Graphics g, Rectangle rect, string title)
    {
        FillRound(g, rect, Panel, 6);
        StrokeRound(g, rect, BorderStrong, 6);
        FillRound(g, new Rectangle(rect.X, rect.Y, rect.Width, 40), C("#242426"), 6);
        Stroke(g, new Rectangle(rect.X, rect.Y + 39, rect.Width, 1), Border);
        Label(g, title, rect.X + 16, rect.Y + 11, 11, Text);
    }

    private static void Button(Graphics g, Rectangle rect, string label, Color bg)
    {
        FillRound(g, rect, C("#1E1E20"), 6);
        StrokeRound(g, rect, C("#4A4A4C"), 6);
        var inner = Rectangle.Inflate(rect, -5, -5);
        FillRound(g, inner, bg, 4);
        StrokeRound(g, inner, ControlPaint.Light(bg), 4);
        Stroke(g, new Rectangle(inner.X + 2, inner.Y + inner.Height - 3, inner.Width - 4, 1), ControlPaint.Dark(bg));
        Label(g, label, inner.X, inner.Y + (inner.Height - 16) / 2, 9, Text, inner.Width, align: StringAlignment.Center, ellipsis: true);
    }

    private static void InputBox(Graphics g, Rectangle rect, string value)
    {
        FillRound(g, rect, C("#1E1E20"), 6);
        StrokeRound(g, rect, C("#4A4A4C"), 6);
        var inner = Rectangle.Inflate(rect, -5, -5);
        FillRound(g, inner, C("#19191B"), 4);
        StrokeRound(g, inner, BorderStrong, 4);
        Label(g, value, inner.X + 10, inner.Y + Math.Max(3, (inner.Height - 17) / 2), 9, Muted, inner.Width - 20, ellipsis: true);
    }

    private static void ComboBox(Graphics g, Rectangle rect, string value)
    {
        InputBox(g, rect, value);
        var inner = Rectangle.Inflate(rect, -5, -5);
        var arrowBox = new Rectangle(inner.Right - 28, inner.Y + 1, 26, inner.Height - 2);
        FillRound(g, arrowBox, C("#242426"), 4);
        Stroke(g, new Rectangle(arrowBox.X, arrowBox.Y + 3, 1, arrowBox.Height - 6), Border);
        Label(g, "▼", arrowBox.X, arrowBox.Y + Math.Max(2, (arrowBox.Height - 17) / 2), 8, Muted, arrowBox.Width, align: StringAlignment.Center);
    }

    private static void TextArea(Graphics g, Rectangle rect, string value)
    {
        FillRound(g, rect, C("#1E1E20"), 6);
        StrokeRound(g, rect, C("#4A4A4C"), 6);
        var inner = Rectangle.Inflate(rect, -5, -5);
        FillRound(g, inner, C("#19191B"), 4);
        StrokeRound(g, inner, BorderStrong, 4);
        var lines = value.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            Label(g, lines[i], inner.X + 10, inner.Y + 10 + i * 24, 9, Muted, inner.Width - 20);
    }

    private static void ToolStrip(Graphics g, Rectangle rect, string[] items)
    {
        FillRound(g, rect, C("#252527"), 5);
        StrokeRound(g, rect, Border, 5);
        var x = rect.X + 6;
        foreach (var item in items)
        {
            var w = Math.Max(64, item.Length * 14 + 24);
            var available = rect.Right - x - 6;
            if (available < 58) break;
            w = Math.Min(w, available);
            Button(g, new Rectangle(x, rect.Y + 5, w, rect.Height - 10), item, PanelAlt);
            x += w + 6;
        }
    }

    private static void MainToolbar(Graphics g, Rectangle rect)
    {
        FillRound(g, rect, C("#252527"), 5);
        StrokeRound(g, rect, Border, 5);
        var seq = C("#4F6948");
        var edit = C("#555557");
        var danger = C("#8A4A4A");
        var utility = C("#2E6F78");
        var spine = C("#8A5A33");
        var vk = C("#2F5F82");
        var batch = C("#2F7478");
        var copy = C("#4C7044");
        var cli = C("#5A458A");
        var items = new (string Text, Color Color)[] {
            ("添加", edit),
            ("编辑", edit),
            ("删除", edit),
            ("删除全部", danger),
            ("复制序列", edit),
            ("暂停全部", edit),
            ("Spine热键编辑", spine),
            ("释放", danger),
            ("开启虚拟按键", vk),
            ("关闭虚拟按键", edit),
            ("管理虚拟按键", vk),
            ("批量重命名/spine解包整理", batch),
            ("批量复制", copy),
            ("CLI批量合并/导出", cli),
            ("导入", edit),
            ("导出", edit)
        };

        var x = rect.X + 6;
        foreach (var item in items)
        {
            var w = EstimateToolbarButtonWidth(item.Text);
            var available = rect.Right - x - 6;
            if (available < 50) break;
            w = Math.Min(w, available);
            Button(g, new Rectangle(x, rect.Y + 5, w, rect.Height - 10), item.Text, item.Color);
            x += w + 6;
        }
    }

    private static int EstimateToolbarButtonWidth(string text)
    {
        var width = 24;
        foreach (var ch in text)
            width += ch < 128 ? 7 : 12;
        return Math.Max(52, width);
    }

    private static void CheckOption(Graphics g, Rectangle rect, string label, bool isChecked, Color color)
    {
        FillRound(g, rect, C("#252527"), 5);
        StrokeRound(g, rect, color, 5);
        var box = new Rectangle(rect.X + 12, rect.Y + (rect.Height - 16) / 2, 16, 16);
        FillRound(g, box, C("#19191B"), 3);
        StrokeRound(g, box, isChecked ? color : BorderStrong, 3);
        if (isChecked)
            Label(g, "✓", box.X + 2, box.Y - 1, 9, color, box.Width, align: StringAlignment.Center);
        Label(g, label, rect.X + 38, rect.Y + (rect.Height - 17) / 2, 9, Text, rect.Width - 46, ellipsis: true);
    }

    private static void Tabs(Graphics g, Rectangle rect, string[] tabs, int active)
    {
        Fill(g, rect, App);
        var x = rect.X;
        for (var i = 0; i < tabs.Length; i++)
        {
            var w = Math.Max(110, tabs[i].Length * 13 + 34);
            var tab = new Rectangle(x, rect.Y, w, rect.Height);
            FillRound(g, tab, i == active ? Panel : C("#333335"), 5);
            StrokeRound(g, tab, Border, 5);
            if (i == active) Fill(g, new Rectangle(tab.X, tab.Y, tab.Width, 3), Blue);
            Label(g, tabs[i], tab.X, tab.Y + 13, 9, i == active ? Text : Muted, tab.Width, align: StringAlignment.Center);
            x += w + 2;
        }
    }

    private static void Table(Graphics g, Rectangle rect, string[] headers, string[][] rows, float[]? columnWeights = null)
    {
        FillRound(g, rect, C("#1E1E20"), 6);
        StrokeRound(g, rect, C("#4A4A4C"), 6);
        rect = Rectangle.Inflate(rect, -6, -6);
        FillRound(g, rect, C("#202023"), 5);
        StrokeRound(g, rect, BorderStrong, 5);
        var state = g.Save();
        g.SetClip(rect);

        var headerH = Math.Min(34, Math.Max(24, rect.Height / 4));
        var rowArea = Math.Max(0, rect.Height - headerH);
        var rowH = rows.Length == 0 ? 42 : Math.Min(42, Math.Max(24, rowArea / Math.Max(rows.Length, 1)));

        FillRound(g, new Rectangle(rect.X, rect.Y, rect.Width, headerH), C("#2B2B2D"), 5);
        var weights = NormalizeColumnWeights(headers.Length, columnWeights);
        var totalWeight = 0f;
        for (var i = 0; i < weights.Length; i++) totalWeight += weights[i];
        var colX = new int[headers.Length];
        var colWidths = new int[headers.Length];
        var x = rect.X;
        for (var c = 0; c < headers.Length; c++)
        {
            colX[c] = x;
            var w = c == headers.Length - 1 ? rect.Right - x : (int)Math.Round(rect.Width * (weights[c] / totalWeight));
            colWidths[c] = Math.Max(36, w);
            x += colWidths[c];
        }
        for (var c = 0; c < headers.Length; c++)
        {
            Label(g, headers[c], colX[c] + 10, rect.Y + Math.Max(5, (headerH - 16) / 2), 9, Text, colWidths[c] - 20, ellipsis: true);
            Stroke(g, new Rectangle(colX[c], rect.Y, colWidths[c], rect.Height), C("#4C4C4E"));
        }
        for (var i = 0; i < rows.Length; i++)
        {
            var y = rect.Y + headerH + i * rowH;
            if (y >= rect.Bottom) break;
            var rowRect = new Rectangle(rect.X, y, rect.Width, rowH);
            Fill(g, rowRect, i % 2 == 0 ? Panel : PanelAlt);
            if (i == 1) Fill(g, rowRect, C("#536A76"));
            Stroke(g, rowRect, C("#4A4A4C"));
            for (var c = 0; c < headers.Length && c < rows[i].Length; c++)
                TableCell(g, rows[i][c], colX[c], y, colWidths[c], rowH);
        }

        g.Restore(state);
    }

    private static void TableCell(Graphics g, string value, int x, int y, int width, int height)
    {
        if (value == "✓")
        {
            var box = new Rectangle(x + 10, y + Math.Max(5, (height - 18) / 2), 18, 18);
            FillRound(g, box, C("#19191B"), 3);
            StrokeRound(g, box, Blue, 3);
            Label(g, "✓", box.X + 1, box.Y, 9, Blue, box.Width, align: StringAlignment.Center);
            return;
        }

        if (value is "选择" or "清除" or "复制")
        {
            var buttonW = Math.Min(width - 18, value.Length * 16 + 28);
            if (buttonW >= 42)
                Button(g, new Rectangle(x + 9, y + Math.Max(5, (height - 28) / 2), buttonW, 28), value, PanelAlt);
            return;
        }

        Label(g, value, x + 10, y + Math.Max(5, (height - 16) / 2), 9, Text, width - 20, ellipsis: true);
    }

    private static float[] NormalizeColumnWeights(int count, float[]? weights)
    {
        var result = new float[count];
        for (var i = 0; i < count; i++)
            result[i] = weights != null && i < weights.Length && weights[i] > 0 ? weights[i] : 1f;
        return result;
    }

    private static void Progress(Graphics g, Rectangle rect, float pct, string text)
    {
        FillRound(g, rect, C("#1E1E20"), 5);
        StrokeRound(g, rect, C("#4A4A4C"), 5);
        var inner = Rectangle.Inflate(rect, -4, -4);
        FillRound(g, inner, C("#19191B"), 4);
        StrokeRound(g, inner, BorderStrong, 4);
        FillRound(g, new Rectangle(inner.X, inner.Y, (int)(inner.Width * pct), inner.Height), Blue, 4);
        Label(g, text, inner.X, inner.Y + 4, 9, Text, inner.Width, align: StringAlignment.Center);
    }

    private static void Status(Graphics g, Rectangle rect, string text)
    {
        Fill(g, rect, C("#222224"));
        Label(g, text, rect.X + 10, rect.Y + 3, 8, Muted);
    }

    private static void ContextMenu(Graphics g, Rectangle rect, string[] items, int dangerIndex)
    {
        FillRound(g, rect, C("#2F2F31"), 5);
        StrokeRound(g, rect, BorderStrong, 5);
        var y = rect.Y + 8;
        for (var i = 0; i < items.Length; i++)
        {
            var itemRect = new Rectangle(rect.X + 6, y, rect.Width - 12, 34);
            if (i == 0 && items[i].StartsWith("["))
                FillRound(g, itemRect, C("#242426"), 4);
            Label(g, items[i], itemRect.X + 12, itemRect.Y + 9, 9, i >= dangerIndex ? (i == dangerIndex ? Red : Text) : Text, itemRect.Width - 24, ellipsis: true);
            y += 36;
            if (i == 0 || i == dangerIndex - 1)
            {
                Stroke(g, new Rectangle(rect.X + 8, y, rect.Width - 16, 1), Border);
                y += 6;
            }
        }
    }

    private static void VirtualButton(Graphics g, Rectangle rect, string label, Color accent, bool active)
    {
        FillRound(g, rect, active ? C("#405745") : C("#4A4A4C"), 6);
        StrokeRound(g, rect, active ? accent : BorderStrong, 6);
        FillRound(g, new Rectangle(rect.X + 8, rect.Y + 8, 12, rect.Height - 16), accent, 4);
        Label(g, label, rect.X + 24, rect.Y + rect.Height / 2 - 8, 10, Text, rect.Width - 30, align: StringAlignment.Center, ellipsis: true);
    }

    private static void Pill(Graphics g, Rectangle rect, string label, Color color)
    {
        FillRound(g, rect, Color.FromArgb(80, color), 5);
        StrokeRound(g, rect, color, 5);
        Label(g, label, rect.X, rect.Y + (rect.Height - 16) / 2, 9, Text, rect.Width, align: StringAlignment.Center, ellipsis: true);
    }

    private static void WarningStripe(Graphics g, Rectangle rect, string message)
    {
        FillRound(g, rect, C("#3A3028"), 5);
        StrokeRound(g, rect, Orange, 5);
        FillRound(g, new Rectangle(rect.X, rect.Y, 8, rect.Height), Orange, 4);
        Label(g, message, rect.X + 24, rect.Y + 19, 11, Text, rect.Width - 32, ellipsis: true);
    }

    private static void DrawChecker(Graphics g, Rectangle rect)
    {
        var state = g.Save();
        using var path = RoundedRect(rect, 4);
        g.SetClip(path);
        var s = 32;
        for (var y = rect.Y; y < rect.Bottom; y += s)
        {
            for (var x = rect.X; x < rect.Right; x += s)
            {
                var alt = ((x - rect.X) / s + (y - rect.Y) / s) % 2 == 0;
                Fill(g, new Rectangle(x, y, Math.Min(s, rect.Right - x), Math.Min(s, rect.Bottom - y)), alt ? C("#555558") : C("#4C4C50"));
            }
        }
        g.Restore(state);
        StrokeRound(g, rect, Border, 4);
    }

    private static void DrawWindowControls(Graphics g, Rectangle rect)
    {
        using var pen = new Pen(Muted, 1.2f);
        for (var i = 0; i < 3; i++)
        {
            var btn = new Rectangle(rect.X + i * 24, rect.Y, 18, 18);
            FillRound(g, btn, C("#2B2B2D"), 4);
            StrokeRound(g, btn, C("#3F3F41"), 4);
            if (i == 0)
            {
                g.DrawLine(pen, btn.X + 5, btn.Y + 11, btn.Right - 5, btn.Y + 11);
            }
            else if (i == 1)
            {
                g.DrawRectangle(pen, btn.X + 5, btn.Y + 5, 8, 8);
            }
            else
            {
                g.DrawLine(pen, btn.X + 5, btn.Y + 5, btn.Right - 5, btn.Bottom - 5);
                g.DrawLine(pen, btn.Right - 5, btn.Y + 5, btn.X + 5, btn.Bottom - 5);
            }
        }
    }

    private static void FillRound(Graphics g, Rectangle rect, Color color, int radius)
    {
        using var path = RoundedRect(rect, radius);
        using var b = new SolidBrush(color);
        g.FillPath(b, path);
    }

    private static void StrokeRound(Graphics g, Rectangle rect, Color color, int radius)
    {
        using var path = RoundedRect(rect, radius);
        using var p = new Pen(color);
        g.DrawPath(p, path);
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Max(1, radius * 2);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d - 1, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d - 1, rect.Bottom - d - 1, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d - 1, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void Fill(Graphics g, Rectangle rect, Color color)
    {
        using var b = new SolidBrush(color);
        g.FillRectangle(b, rect);
    }

    private static void Stroke(Graphics g, Rectangle rect, Color color)
    {
        using var p = new Pen(color);
        g.DrawRectangle(p, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
    }

    private static void Label(Graphics g, string text, int x, int y, float size, Color color, int width = 800, StringAlignment align = StringAlignment.Near, bool ellipsis = false)
    {
        using var font = new Font(UiFont, size, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = align, LineAlignment = StringAlignment.Near, Trimming = ellipsis ? StringTrimming.EllipsisCharacter : StringTrimming.None };
        g.DrawString(text, font, brush, new RectangleF(x, y, width, size + 8), format);
    }

    private static Color C(string hex)
    {
        return ColorTranslator.FromHtml(hex);
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
