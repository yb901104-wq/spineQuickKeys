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
        if (key == "verify-animation-logic")
        {
            File.WriteAllText(output, VerifyAnimationLogic());
            return;
        }
        if (key == "verify-n1-filter")
        {
            File.WriteAllText(output, VerifyN1Filter());
            return;
        }
        if (key == "verify-export-preset")
        {
            File.WriteAllText(output, VerifyExportPreset());
            return;
        }
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

    /// <summary>
    /// AUD-038：不启动窗口，反射调用产品代码里的纯逻辑方法，验证并集同名合并、
    /// 交集写入（保留 A(f) 原始拼写）、缺项标红、零匹配文案与 G2 扩展名口径。
    /// 结果写到 output 指定的文本文件；全部为内存操作，零磁盘数据改动。
    /// </summary>
    private static string VerifyAnimationLogic()
    {
        var lines = new List<string>();
        var pass = 0;
        var fail = 0;
        void Check(string name, bool ok, string detail)
        {
            lines.Add($"{(ok ? "PASS" : "FAIL")} | {name} | {detail}");
            if (ok) pass++; else fail++;
        }

        using var panel = new InplaceBackupPanel();
        var scanned = GetField<List<BackupFileEntry>>(panel, "_scanned")!;
        var f1 = new BackupFileEntry { FullPath = @"D:\p\f1.spine", Directory = "p", Size = 1 };
        var f2 = new BackupFileEntry { FullPath = @"D:\p\f2.spine", Directory = "p", Size = 1 };
        var f3 = new BackupFileEntry { FullPath = @"D:\p\f3.spine", Directory = "p", Size = 1 };
        scanned.AddRange([f1, f2, f3]);

        // A(f)：f1 {walk,idle} / f2 {Walk,attack} / f3 {idle,attack,talk}
        var byPath = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [f1.FullPath] = ["walk", "idle"],
            [f2.FullPath] = ["Walk", "attack"],
            [f3.FullPath] = ["idle", "attack", "talk"]
        };
        IReadOnlyList<string> snapshot = [f1.FullPath, f2.FullPath, f3.FullPath];

        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var buildUnion = typeof(InplaceBackupPanel).GetMethod("BuildUnion", flags)!;
        var union = (List<string>)buildUnion.Invoke(null, [snapshot, byPath])!;
        Check("V10/V11 并集同名合并（忽略大小写、保留首次拼写）",
            string.Join(",", union) == "walk,idle,attack,talk", string.Join(",", union));

        var apply = typeof(InplaceBackupPanel).GetMethod("ApplyBatchSelection",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var columnText = typeof(InplaceBackupPanel).GetMethod("AnimationColumnText", flags)!;
        string Text(BackupFileEntry e) => (string)columnText.Invoke(null, [e])!;

        // 轮 1：S = {walk, idle}
        apply.Invoke(panel, [snapshot, (List<string>)["walk", "idle"], byPath]);
        Check("V14 f1 命中全部所选动画 → 不标红",
            string.Join(",", f1.SelectedAnimations) == "walk,idle" && !f1.HasMissingAnimations,
            $"E(f1)=[{string.Join(",", f1.SelectedAnimations)}] red={f1.HasMissingAnimations}");
        Check("V14/D8 f2 大小写同名求交并保留 A(f) 原始拼写 → 标红",
            string.Join(",", f2.SelectedAnimations) == "Walk" && f2.HasMissingAnimations,
            $"E(f2)=[{string.Join(",", f2.SelectedAnimations)}] red={f2.HasMissingAnimations}");
        Check("V14 f3 只写自身已有动画 → 标红",
            string.Join(",", f3.SelectedAnimations) == "idle" && f3.HasMissingAnimations,
            $"E(f3)=[{string.Join(",", f3.SelectedAnimations)}] red={f3.HasMissingAnimations}");
        Check("V17 动画列文本", Text(f1) == "walk, idle", Text(f1));

        // 轮 2：S = {run}，所有文件零匹配；验证全量覆盖、无上轮残留
        apply.Invoke(panel, [snapshot, (List<string>)["run"], byPath]);
        Check("V21 再次确认全量覆盖、无旧结果残留",
            f1.SelectedAnimations.Count == 0 && f2.SelectedAnimations.Count == 0 && f3.SelectedAnimations.Count == 0,
            $"E=({f1.SelectedAnimations.Count},{f2.SelectedAnimations.Count},{f3.SelectedAnimations.Count})");
        Check("V15 零匹配显示「（无匹配动画）」并标红",
            Text(f2) == "（无匹配动画）" && f2.HasMissingAnimations, Text(f2));
        Check("V18 未参与本轮的行显示「—」",
            Text(new BackupFileEntry { FullPath = @"D:\p\other.spine" }) == "—", "—");

        // G2 扩展名口径（与产品代码同一表达式）
        bool IsSpine(string p) => string.Equals(Path.GetExtension(p), ".spine", StringComparison.OrdinalIgnoreCase);
        Check("G2 大小写不敏感", IsSpine("a.SPINE") && IsSpine("a.spine"), ".SPINE/.spine 通过");
        Check("G2 非 .spine 一律拦截",
            !IsSpine("a.spine.json") && !IsSpine("a.png") && !IsSpine("a.atlas") && !IsSpine("a"),
            ".spine.json/.png/.atlas/无扩展名 均拦截");

        lines.Insert(0, $"AUD-038 动画语义断言：{pass} PASS / {fail} FAIL");
        lines.Add("说明：门槛弹窗、并集弹窗的模态显示与真实 Spine.com 读取需用户真机手工确认。");
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// AUD-039：反射调用产品代码 RebuildVisible，验证取消「按备份后缀静默排除」后
    /// *-n1.spine 一律进入可见列表，且扩展名 / 关键词过滤回归正常。
    /// 另用用户真实工程目录做只读扫描复核。全部为内存操作 + 只读扫描。
    /// </summary>
    private static string VerifyN1Filter()
    {
        var lines = new List<string>();
        var pass = 0;
        var fail = 0;
        var skip = 0;
        void Check(string name, bool ok, string detail)
        {
            lines.Add($"{(ok ? "PASS" : "FAIL")} | {name} | {detail}");
            if (ok) pass++; else fail++;
        }

        using var panel = new InplaceBackupPanel();
        var scanned = GetField<List<BackupFileEntry>>(panel, "_scanned")!;
        var suffix = GetField<TextBox>(panel, "_txtSuffix")!;
        var extCombo = GetField<ComboBox>(panel, "_cmbExtension")!;
        var keyword = GetField<TextBox>(panel, "_txtKeyword")!;
        List<BackupFileEntry> Visible() => GetField<List<BackupFileEntry>>(panel, "_visible")!;
        void Rebuild() => Invoke(panel, "RebuildVisible");

        const string root = @"D:\动画工程\monster-spine-h细线";
        BackupFileEntry[] samples =
        [
            new() { FullPath = Path.Combine(root, @"G2\G2-n1.spine"), Directory = "G2", Size = 13_686 },
            new() { FullPath = Path.Combine(root, @"G10\G10-skin_merged-n1.spine"), Directory = "G10", Size = 9_120 },
            new() { FullPath = Path.Combine(root, @"G2\G2.spine"), Directory = "G2", Size = 18_176 },
            new() { FullPath = Path.Combine(root, @"G2\tx-xy.spine"), Directory = "G2", Size = 1_221 },
            new() { FullPath = Path.Combine(root, @"G2\G2_bf.spine"), Directory = "G2", Size = 17_900 },
            new() { FullPath = Path.Combine(root, @"G2\201.png"), Directory = "G2", Size = 662_142 },
            new() { FullPath = Path.Combine(root, @"G2\hero.atlas"), Directory = "G2", Size = 4_822 }
        ];
        scanned.AddRange(samples);
        lines.Add($"INFO | 产品面板构造时从 path_history.json 读到的初始后缀 = \"{suffix.Text}\"");
        lines.Add($"INFO | 样本 {samples.Length} 个文件，其中 2 个 *-n1.spine、1 个 *_bf.spine");

        // ── 核心回归：修复前 suffix="-n1" 会把 2 个 -n1.spine 静默剔除 ──
        suffix.Text = "-n1";
        Rebuild();
        Check("AUD-039 后缀 -n1 不再排除任何文件", Visible().Count == samples.Length,
            $"可见 {Visible().Count} / 扫描 {samples.Length}");
        Check("AUD-039 G2-n1.spine 可见", Visible().Any(e => e.FileName == "G2-n1.spine"),
            string.Join(", ", Visible().Select(e => e.FileName)));
        Check("AUD-039 G10-skin_merged-n1.spine 可见",
            Visible().Any(e => e.FileName == "G10-skin_merged-n1.spine"), "跨目录同名规则同样不再误伤");

        // ── 后缀无论取何值都不再影响可见性 ──
        suffix.Text = "_bf";
        Rebuild();
        Check("后缀 _bf 同样不影响可见性（G2_bf.spine 仍在）",
            Visible().Count == samples.Length && Visible().Any(e => e.FileName == "G2_bf.spine"),
            $"可见 {Visible().Count}");
        suffix.Text = "G2";
        Rebuild();
        Check("极端后缀 G2 也不排除 G2.spine",
            Visible().Count == samples.Length && Visible().Any(e => e.FileName == "G2.spine"),
            $"可见 {Visible().Count}");
        suffix.Text = "";
        Rebuild();
        Check("非法后缀（空）也不排除", Visible().Count == samples.Length, $"可见 {Visible().Count}");
        suffix.Text = "-n1";

        // ── 过滤回归 ──
        Invoke(panel, "RebuildExtensionCombo");
        extCombo.SelectedItem = ".spine";
        Rebuild();
        var spineOnly = Visible();
        Check("扩展名过滤回归：只剩 .spine",
            spineOnly.Count == 5 && spineOnly.All(e => e.Extension == ".spine"),
            string.Join(", ", spineOnly.Select(e => e.FileName)));
        Check("扩展名过滤下 2 个 -n1.spine 仍在",
            spineOnly.Count(e => e.FileName.Contains("-n1", StringComparison.OrdinalIgnoreCase)) == 2,
            $"命中 {spineOnly.Count(e => e.FileName.Contains("-n1", StringComparison.OrdinalIgnoreCase))} 个");
        extCombo.SelectedIndex = 0;
        keyword.Text = "-n1";
        Rebuild();
        Check("关键词过滤回归：-n1 精确命中 2 个", Visible().Count == 2,
            string.Join(", ", Visible().Select(e => e.FileName)));
        keyword.Text = "";
        Rebuild();

        // ── 状态栏与产品代码残留检查 ──
        var status = "";
        panel.StatusRequested += (text, _) => status = text;
        Invoke(panel, "RefreshStatusHint");
        Check("状态栏不再出现「疑似备份文件」", !status.Contains("疑似备份", StringComparison.Ordinal), status);
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        Check("产品代码已移除 IsLikelyBackup",
            typeof(InplaceBackupPanel).GetMethod("IsLikelyBackup", flags) == null, "方法不存在");
        Check("产品代码已移除 _excludedCount",
            typeof(InplaceBackupPanel).GetField("_excludedCount", flags) == null, "字段不存在");

        // ── 用户真实工程目录：只读扫描复核 ──
        if (Directory.Exists(root))
        {
            var entries = new InplaceBackupService().ScanAsync([root], CancellationToken.None)
                .GetAwaiter().GetResult();
            var n1Files = entries.Where(e => e.FileName.EndsWith("-n1.spine", StringComparison.OrdinalIgnoreCase)).ToList();
            scanned.Clear();
            scanned.AddRange(entries);
            Rebuild();
            var visiblePaths = Visible().Select(e => e.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Check("真实目录：可见数 == 扫描数（零静默排除）", Visible().Count == entries.Count,
                $"扫描 {entries.Count} / 可见 {Visible().Count}");
            Check("真实目录：全部 -n1.spine 进入可见列表",
                n1Files.Count > 0 && n1Files.All(e => visiblePaths.Contains(e.FullPath)),
                $"共 {n1Files.Count} 个 -n1.spine，逐个核对通过");
        }
        else
        {
            skip++;
            lines.Add($"SKIP | 真实目录不存在：{root}");
        }

        lines.Insert(0, $"AUD-039 后缀静默排除修复断言：{pass} PASS / {fail} FAIL / {skip} SKIP");
        lines.Add("说明：真实复制到磁盘与 INV-1~8 回归需用户真机手工确认。");
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// AUD-040：不启动窗口，反射 + 直接调用产品代码，验证 R1/R2 改名、
    /// F1–F7 状态真值源、U2 预设持久化往返、U1 CLI 参数构造与 D15 目录去重。
    /// 临时文件只写系统临时目录，不触碰用户 %APPDATA% 与工程目录。
    /// </summary>
    private static string VerifyExportPreset()
    {
        var lines = new List<string>();
        var pass = 0;
        var fail = 0;
        void Check(string name, bool ok, string detail)
        {
            lines.Add($"{(ok ? "PASS" : "FAIL")} | {name} | {detail}");
            if (ok) pass++; else fail++;
        }

        const System.Reflection.BindingFlags inst = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static |
                                                   System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var t = typeof(BatchCliWindow);
        string ConfigName(object f) => (string)t.GetProperty("CurrentExportConfigName", all)!.GetValue(f)!;

        using var cli = new BatchCliWindow();

        // ── R1/R2 改名 ──
        Check("V1 顶层窗口标题改名", cli.Text == "批量复制/合成/导出", cli.Text);
        var tabMerge = GetField<TabPage>(cli, "_tabMerge")!;
        Check("V2 子标签「合并」→「批量合并」", tabMerge.Text == "批量合并", tabMerge.Text);
        using (var main = new MainForm())
        {
            var outer = FindControl<TabControl>(main);
            var outerTabText = outer != null && outer.TabPages.Count > 2 ? outer.TabPages[2].Text : "(无第3页)";
            Check("V1 外层主标签改名", outerTabText == "批量复制/合成/导出", outerTabText);
            Check("V1 外层仍是 3 个主标签", outer?.TabPages.Count == 3, $"{outer?.TabPages.Count} 个");
        }

        // ── F1/F2 状态 bug 根治 ──
        Check("V21 初始真值源=finish.export.json（旧代码为 export.json）",
            ConfigName(cli) == "finish.export.json", ConfigName(cli));
        var rbFinish = GetField<RadioButton>(cli, "_rbFinish")!;
        var rbDemotion = GetField<RadioButton>(cli, "_rbDemotion")!;
        var rbOther = GetField<RadioButton>(cli, "_rbOther")!;
        Check("V21 初始 radio = finish", rbFinish.Checked && !rbDemotion.Checked && !rbOther.Checked, "finish=on");
        Check("V21 产品代码已删除 _exportConfigName 字段",
            t.GetField("_exportConfigName", all) == null, "字段不存在");

        rbDemotion.Checked = true;
        Check("V23 切 demotion 后真值源立即变化", ConfigName(cli) == "demotion.export.json", ConfigName(cli));
        rbFinish.Checked = true;
        Check("V23 切回 finish 后真值源立即变化", ConfigName(cli) == "finish.export.json", ConfigName(cli));

        // F5：模拟用户已输入完「其他」，验证真值源跟随手填值
        t.GetField("_otherConfigName", all)!.SetValue(cli, "mycfg.export.json");
        t.GetField("_suppressConfigEvents", all)!.SetValue(cli, true);
        try { rbOther.Checked = true; }
        finally { t.GetField("_suppressConfigEvents", all)!.SetValue(cli, false); }
        Check("V24 「其他」手填值成为真值源", ConfigName(cli) == "mycfg.export.json", ConfigName(cli));
        rbFinish.Checked = true;

        // ── V22 状态列按真值源计算（真实临时目录）──
        var tempDir = Path.Combine(Path.GetTempPath(), "KeyMacro", "aud040_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var project = Path.Combine(tempDir, "hero.spine");
            File.WriteAllText(project, "dummy");
            var config = Path.Combine(tempDir, "finish.export.json");
            var entries = GetField<System.ComponentModel.BindingList<SpineCliEntry>>(cli, "_exportEntries")!;
            var list = GetField<ListView>(cli, "_lvExportFiles")!;
            entries.Clear();
            list.Items.Clear();
            t.GetMethod("AddExportEntry", inst)!.Invoke(cli, [list, entries, project]);
            Check("V22 配置不存在时状态=❌", list.Items[0].SubItems[1].Text == "❌", list.Items[0].SubItems[1].Text);
            File.WriteAllText(config, "{}");
            Invoke(cli, "RefreshExportStatus");
            Check("V22 点刷新状态后状态=✅（不再需要切配置名）",
                list.Items[0].SubItems[1].Text == "✅", list.Items[0].SubItems[1].Text);
            File.Delete(config);
            Invoke(cli, "RefreshExportStatus");
            Check("V22 删除配置后刷新回=❌", list.Items[0].SubItems[1].Text == "❌", list.Items[0].SubItems[1].Text);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }

        // ── U1 备注 / R3/R6/R8 控件 ──
        var note = GetField<Label>(cli, "_lblVersionNote")!;
        Check("U1 4.3 版本备注存在", note.Text.Contains("4.3") && note.Text.Contains("必须"), note.Text);
        Check("U1 备注使用 UiTheme.Orange 醒目色", note.ForeColor == UiTheme.Orange, note.ForeColor.ToString());
        Check("R3 保存为导出预设按钮", GetField<Button>(cli, "_btnSavePreset")!.Text == "保存为导出预设", "按钮存在");
        Check("R8 批量生成导出配置按钮", GetField<Button>(cli, "_btnBatchGenerate")!.Text == "批量生成导出配置", "按钮存在");
        Check("R6 三个预设按钮文本",
            GetField<Button>(cli, "_btnActivatePreset")!.Text == "激活预设" &&
            GetField<Button>(cli, "_btnDeletePreset")!.Text == "删除预设" &&
            GetField<Button>(cli, "_btnDeleteAllPresets")!.Text == "删除全部预设", "激活/删除/删除全部");
        var presetList = GetField<ListView>(cli, "_lvPresets")!;
        Check("R4 预设列表 3 列", presetList.Columns.Count == 3,
            string.Join("/", presetList.Columns.Cast<ColumnHeader>().Select(c => c.Text)));
        Check("V9 空列表时三个按钮禁用",
            !GetField<Button>(cli, "_btnActivatePreset")!.Enabled &&
            !GetField<Button>(cli, "_btnDeletePreset")!.Enabled &&
            !GetField<Button>(cli, "_btnDeleteAllPresets")!.Enabled, "均禁用");

        // ── U2 持久化往返（临时文件，不写用户 AppData）──
        var presetFile = Path.Combine(Path.GetTempPath(), "KeyMacro", "presets_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var samples = new List<ExportPreset>
            {
                new() { Name = "G2 系列导出", SourceDir = @"D:\动画工程\G2", OutputDir = @"D:\exports",
                    FilePaths = [@"D:\动画工程\G2\G2.spine", @"D:\动画工程\G2\G2-n1.spine"],
                    ConfigSelection = ExportConfigSelection.Finish, CreatedAt = new DateTime(2026, 11, 9, 20, 0, 0) },
                new() { Name = "测试用-其他", SourceDir = @"D:\p", OutputDir = @"D:\o",
                    FilePaths = [@"D:\p\a.spine"], ConfigSelection = ExportConfigSelection.Other,
                    OtherConfigName = "mycfg.export.json" }
            };
            ConfigService.SaveExportPresetsTo(presetFile, samples);
            var loaded = ConfigService.LoadExportPresetsFrom(presetFile);
            Check("U2 预设 JSON 往返：数量", loaded.Count == 2, $"{loaded.Count} 个");
            Check("U2 中文预设名与路径保留", loaded[0].Name == "G2 系列导出" && loaded[0].FilePaths.Count == 2 &&
                loaded[0].FilePaths[1].EndsWith("G2-n1.spine"), $"{loaded[0].Name} / {loaded[0].FilePaths.Count} 个文件");
            Check("U2 其他手填名与单选项保留",
                loaded[1].ConfigSelection == ExportConfigSelection.Other && loaded[1].OtherConfigName == "mycfg.export.json",
                $"{loaded[1].ConfigSelection} / {loaded[1].OtherConfigName}");
            Check("U2 创建时间保留", loaded[0].CreatedAt == new DateTime(2026, 11, 9, 20, 0, 0), loaded[0].CreatedAt.ToString("s"));
            File.WriteAllText(presetFile, "{broken json");
            Check("U2 损坏文件返回空列表不崩溃", ConfigService.LoadExportPresetsFrom(presetFile).Count == 0, "返回空列表");

            var defaultPath = (string)typeof(ConfigService).GetField("ExportPresetsFile", all)!.GetValue(null)!;
            Check("U2 默认落盘位置是本地软件数据文档 export_presets.json",
                defaultPath.EndsWith("KeyMacro\\export_presets.json", StringComparison.OrdinalIgnoreCase) &&
                defaultPath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.OrdinalIgnoreCase),
                defaultPath);
        }
        finally
        {
            try { if (File.Exists(presetFile)) File.Delete(presetFile); } catch { }
        }

        // ── R7 预设激活（含「其他」不弹 InputBox）──
        var presets = GetField<List<ExportPreset>>(cli, "_presets")!;
        presets.Clear();
        var presetOther = new ExportPreset
        {
            Name = "测试用-其他", SourceDir = @"D:\p", OutputDir = @"D:\o",
            FilePaths = [], ConfigSelection = ExportConfigSelection.Other,
            OtherConfigName = "custom.export.json"
        };
        presets.Add(presetOther);
        Invoke(cli, "RefreshPresetList");
        Check("R7 列表加载预设后按钮启用态",
            GetField<Button>(cli, "_btnDeleteAllPresets")!.Enabled, "删除全部已启用");
        // 断言里窗口未 Show，需先强制创建 ListView 句柄，否则 Selected 不会落到 SelectedItems
        _ = presetList.Handle;
        if (presetList.Items.Count > 0) presetList.Items[0].Selected = true;
        Invoke(cli, "RefreshPresetButtons");
        Check("V9 选中预设后「激活预设 / 删除预设」均启用",
            GetField<Button>(cli, "_btnActivatePreset")!.Enabled &&
            GetField<Button>(cli, "_btnDeletePreset")!.Enabled,
            $"列表选中数={presetList.SelectedItems.Count}");
        t.GetMethod("ActivatePreset", inst)!.Invoke(cli, [presetOther]);
        Check("V17 激活「其他」预设后不弹 InputBox、手填值完整恢复",
            rbOther.Checked && ConfigName(cli) == "custom.export.json", ConfigName(cli));
        Check("R7 激活预设恢复源目录/输出目录",
            GetField<TextBox>(cli, "_txtSourceDir")!.Text == @"D:\p" &&
            GetField<TextBox>(cli, "_txtOutputDir")!.Text == @"D:\o", "目录已录入");

        // ── R9 CLI 参数与目录去重 ──
        var args = SpineCliService.BuildLastExportSettingsArgs(@"D:\p\hero.spine", @"D:\p\finish.export.json");
        Check("U1 CLI 参数构造（--ignore-unknown 透传 + -o 完整文件名）",
            string.Join(" ", args) == "--ignore-unknown -i D:\\p\\hero.spine -o D:\\p\\finish.export.json --last-export-settings",
            string.Join(" ", args));
        var targets = BatchCliWindow.BuildGenerateTargets(
            [@"D:\p\a.spine", @"D:\p\b.spine", @"D:\q\c.spine"], "finish.export.json");
        Check("D15 同目录多 .spine 只生成一个目标",
            targets.Count == 2 && targets[0].Target == @"D:\p\finish.export.json" &&
            targets[1].Target == @"D:\q\finish.export.json",
            string.Join(" | ", targets.Select(x => x.Target)));
        var targetsOther = BatchCliWindow.BuildGenerateTargets([@"D:\p\a.spine"], "mycfg.export.json");
        Check("V24 「其他」手填名作为生成文件名",
            targetsOther.Count == 1 && targetsOther[0].Target == @"D:\p\mycfg.export.json", targetsOther[0].Target);

        lines.Insert(0, $"AUD-040 导出预设 / 状态修复 / CLI 生成断言：{pass} PASS / {fail} FAIL");
        lines.Add("说明：真实 Spine.com 4.3+ 生成配置文件、命名弹窗与删除确认的模态显示、重启后预设恢复，需用户真机手工确认。");
        return string.Join(Environment.NewLine, lines);
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
            // AUD-038：验证「批量复制」子页（顶层 CLI 窗口）与真实两层非顶层 Form 嵌套（MainForm → CLI → 批量复制）
            "cli-batch-copy" => new BatchCliWindow(),
            "mainform-cli-copy" => new MainForm(),
            "cli-batch-gate" => new MainForm(),
            // AUD-039：用户真实工程目录 + 后缀 -n1，验证 -n1.spine 出现在列表里
            "mainform-cli-n1" => new MainForm(),
            // AUD-040：两层嵌套 + 导出页预设列 / 激活结果
            "export-preset-overview" => new MainForm(),
            "export-preset-activated" => new MainForm(),
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
            // AUD-038 后内层子页顺序为 0 批量复制 / 1 合并 / 2 批量导出
            if (key == "batch-cli-merge" && tab.TabPages.Count > 1) tab.SelectedIndex = 1;
            if (key == "batch-cli-export" && tab.TabPages.Count > 2) tab.SelectedIndex = 2;
            if (key == "cli-batch-copy") tab.SelectedIndex = 0;
            if (key == "mainform-cli-copy" || key == "cli-batch-gate" || key == "mainform-cli-n1") tab.SelectedIndex = 2;
            if (key == "rename-tool-organize" && tab.TabPages.Count > 1) tab.SelectedIndex = 1;
            if (key == "rename-tool-unpack" && tab.TabPages.Count > 2) tab.SelectedIndex = 2;
        }

        // AUD-040：先切外层到 CLI 页（触发 _cliPage 懒加载），再切内层到「批量导出」
        if (key == "export-preset-overview" || key == "export-preset-activated")
        {
            var outer = FindControl<TabControl>(form);
            if (outer != null && outer.TabPages.Count > 2) outer.SelectedIndex = 2;
            Application.DoEvents();
            var inner = FindAllControls<TabControl>(form).Skip(1).FirstOrDefault();
            if (inner != null && inner.TabPages.Count > 2) inner.SelectedIndex = 2;
            Application.DoEvents();
        }

        if (key == "batch-copy") SeedBatchCopy(form);
        if (key == "source-file-picker") SeedSourceFilePicker(form);
        if (key == "vk-manager") SeedVkManager(form);
        if (key == "batch-cli-merge" || key == "batch-cli-export") SeedBatchCli(form, key);
        if (key == "mainform-cli-copy" || key == "cli-batch-copy") SeedBatchCopyAnimations(form);
        if (key == "mainform-cli-n1") SeedRealN1Folder(form);
        if (key == "export-preset-overview" || key == "export-preset-activated")
            SeedExportPresets(form, activated: key == "export-preset-activated");
        if (key == "cli-batch-gate")
        {
            SeedBatchCopyAnimations(form, checkNonSpine: true);
            TriggerGateDialog(form);
        }
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

    /// <summary>
    /// AUD-040：向两层嵌套窗口的导出页注入示例预设与文件行，用于实机截图。
    /// 只写内存，不调用 SaveExportPresets，因此不会在用户 %APPDATA% 生成预设文件。
    /// </summary>
    private static void SeedExportPresets(Form form, bool activated)
    {
        var cli = form as BatchCliWindow ?? GetField<BatchCliWindow>(form, "_cliPage");
        if (cli == null) return;

        if (GetField<TextBox>(cli, "_txtSpinePath") is { } spine)
            spine.Text = @"D:\Program Files\Spine\Spine.com";
        if (GetField<TextBox>(cli, "_txtSourceDir") is { } src)
            src.Text = activated ? @"D:\动画工程\monster-spine-h细线\G2" : @"D:\projects";
        if (GetField<TextBox>(cli, "_txtOutputDir") is { } outDir)
            outDir.Text = activated ? @"D:\exports\G2" : @"D:\exports";

        AddListViewRows(GetField<ListView>(cli, "_lvExportFiles"), activated
            ? [
                ["G2.spine", "✅", @"D:\动画工程\monster-spine-h细线\G2\G2.spine"],
                ["G2-n1.spine", "✅", @"D:\动画工程\monster-spine-h细线\G2\G2-n1.spine"],
                ["tx-xy.spine", "✅", @"D:\动画工程\monster-spine-h细线\G2\tx-xy.spine"]
              ]
            : [
                ["G2.spine", "✅", @"D:\projects\G2.spine"],
                ["G2-n1.spine", "✅", @"D:\projects\G2-n1.spine"],
                ["tx-xy.spine", "❌", @"D:\projects\tx-xy.spine"]
              ]);

        var presets = GetField<List<ExportPreset>>(cli, "_presets")!;
        presets.Clear();
        presets.AddRange(
        [
            new ExportPreset
            {
                Name = "G2 系列导出", SourceDir = @"D:\动画工程\monster-spine-h细线\G2",
                OutputDir = @"D:\exports\G2",
                FilePaths =
                [
                    @"D:\动画工程\monster-spine-h细线\G2\G2.spine",
                    @"D:\动画工程\monster-spine-h细线\G2\G2-n1.spine",
                    @"D:\动画工程\monster-spine-h细线\G2\tx-xy.spine"
                ],
                ConfigSelection = ExportConfigSelection.Finish
            },
            new ExportPreset
            {
                Name = "怪物合集", SourceDir = @"D:\动画工程\monster-spine-h细线",
                OutputDir = @"D:\exports\all",
                FilePaths = Enumerable.Range(1, 12)
                    .Select(i => $@"D:\动画工程\monster-spine-h细线\G{i}\G{i}.spine").ToList(),
                ConfigSelection = ExportConfigSelection.Demotion
            },
            new ExportPreset
            {
                Name = "测试用-其他", SourceDir = @"D:\projects", OutputDir = @"D:\exports\test",
                FilePaths = [@"D:\projects\a.spine"],
                ConfigSelection = ExportConfigSelection.Other, OtherConfigName = "mycfg.export.json"
            }
        ]);

        Invoke(cli, "RefreshPresetList");
        var lv = GetField<ListView>(cli, "_lvPresets")!;
        if (lv.Items.Count > 1)
        {
            var index = activated ? 0 : 1;
            lv.Items[index].Selected = true;
            lv.Items[index].Focused = true;
        }
        Application.DoEvents();   // 让 SelectedIndexChanged 先落地，再按产品逻辑刷新按钮启用态
        Invoke(cli, "RefreshPresetButtons");
        if (GetField<Label>(cli, "_lblPresetHeader") is { } header)
            header.Text = activated ? "导出预设（共 3 个）· 已激活「G2 系列导出」" : "导出预设（共 3 个）· 双击可激活";
    }

    /// <summary>
    /// AUD-038：向批量复制源列表注入示例行，验证新增的「动画」列、整行标红、未参与行「—」
    /// 以及动画工具条备注。仅注入内存数据，不产生任何磁盘读写。
    /// </summary>
    private static void SeedBatchCopyAnimations(Form form, bool checkNonSpine = false)
    {
        var cli = form as BatchCliWindow ?? GetField<BatchCliWindow>(form, "_cliPage");
        var copy = cli != null ? GetField<BatchCopyWindow>(cli, "_copyPage") : null;
        var panel = copy != null ? GetField<InplaceBackupPanel>(copy, "_backupPanel") : null;
        if (panel == null) return;

        var scanned = GetField<List<BackupFileEntry>>(panel, "_scanned");
        var visible = GetField<List<BackupFileEntry>>(panel, "_visible");
        var checkedSet = GetField<HashSet<string>>(panel, "_checked");
        var roots = GetField<HashSet<string>>(panel, "_roots");
        if (scanned == null || visible == null || checkedSet == null) return;

        BackupFileEntry[] samples =
        [
            new() { FullPath = @"D:\projects\hero\hero.spine", Directory = "hero", Size = 248_320, Checked = true,
                AvailableAnimations = ["idle", "walk", "attack", "run"],
                SelectedAnimations = ["idle", "walk", "attack"] },
            new() { FullPath = @"D:\projects\hero\ui_icon.spine", Directory = "hero", Size = 96_112, Checked = true,
                AvailableAnimations = ["idle"], SelectedAnimations = ["idle"], HasMissingAnimations = true },
            new() { FullPath = @"D:\projects\npc\npc_boss.spine", Directory = "npc", Size = 512_640, Checked = true,
                AvailableAnimations = ["idle", "walk", "attack", "run", "talk", "pop", "loop"],
                SelectedAnimations = ["idle", "walk", "attack"] },
            new() { FullPath = @"D:\projects\hero\hero.atlas", Directory = "hero", Size = 18_432 },
            new() { FullPath = @"D:\projects\hero\hero.png", Directory = "hero", Size = 1_204_224 }
        ];
        foreach (var entry in samples)
        {
            scanned.Add(entry);
            visible.Add(entry);
            var shouldCheck = entry.Checked ||
                (checkNonSpine && !entry.FullPath.EndsWith(".spine", StringComparison.OrdinalIgnoreCase));
            if (shouldCheck) checkedSet.Add(entry.FullPath);
        }
        roots?.Add(@"D:\projects");

        panel.GetType().GetField("_lastSelectedAnimationCount",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(panel, 3);
        Invoke(panel, "UpdateCounts");
        Invoke(panel, "RefreshGrid");
        Invoke(panel, "RefreshEnabledState");
    }

    /// <summary>
    /// AUD-039：用用户真实目录 D:\动画工程\monster-spine-h细线\G2 走产品
    /// ScanAsync → RebuildVisible → RefreshGrid 全链路，后缀保持现场值 -n1，
    /// 筛选 .spine，验证 G2-n1.spine 出现在列表里。只读扫描，零磁盘数据改动。
    /// </summary>
    private static void SeedRealN1Folder(Form form)
    {
        var cli = form as BatchCliWindow ?? GetField<BatchCliWindow>(form, "_cliPage");
        var copy = cli != null ? GetField<BatchCopyWindow>(cli, "_copyPage") : null;
        var panel = copy != null ? GetField<InplaceBackupPanel>(copy, "_backupPanel") : null;
        const string root = @"D:\动画工程\monster-spine-h细线\G2";
        if (panel == null || !Directory.Exists(root)) return;

        var entries = new InplaceBackupService().ScanAsync([root], CancellationToken.None)
            .GetAwaiter().GetResult();
        var scanned = GetField<List<BackupFileEntry>>(panel, "_scanned");
        var roots = GetField<HashSet<string>>(panel, "_roots");
        if (scanned == null) return;
        scanned.Clear();
        scanned.AddRange(entries);
        roots?.Add(root);

        // 复现用户现场：备份后缀历史第一项为 -n1
        if (GetField<TextBox>(panel, "_txtSuffix") is { } suffix) suffix.Text = "-n1";

        Invoke(panel, "RebuildExtensionCombo");
        if (GetField<ComboBox>(panel, "_cmbExtension") is { } ext) ext.SelectedItem = ".spine";
        Invoke(panel, "RebuildVisible");
        Invoke(panel, "RefreshGrid");
        Invoke(panel, "UpdateCounts");
        Invoke(panel, "RefreshEnabledState");
        panel.RefreshHostVisualState();
    }

    /// <summary>触发「选择动画」按钮，验证 G2 门槛在勾选项混入 .atlas / .png 时报错且不打开动画窗。</summary>
    private static void TriggerGateDialog(Form form)
    {
        var cli = GetField<BatchCliWindow>(form, "_cliPage");
        var copy = cli != null ? GetField<BatchCopyWindow>(cli, "_copyPage") : null;
        var panel = copy != null ? GetField<InplaceBackupPanel>(copy, "_backupPanel") : null;
        var button = panel != null ? GetField<Button>(panel, "_btnSelectAnimations") : null;
        if (button == null) return;
        form.BeginInvoke(new Action(() => button.PerformClick()));
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

    /// <summary>AUD-040：深度优先收集全部同类型控件，用于定位两层嵌套里的内层 TabControl。</summary>
    private static List<T> FindAllControls<T>(Control root) where T : Control
    {
        var result = new List<T>();
        void Walk(Control c)
        {
            if (c is T matched) result.Add(matched);
            foreach (Control child in c.Controls) Walk(child);
        }
        Walk(root);
        return result;
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
        if (key.Contains("menu", StringComparison.OrdinalIgnoreCase)
            || key == "mainform-layout" || key == "cli-batch-gate")
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









