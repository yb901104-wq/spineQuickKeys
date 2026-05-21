#nullable disable
using System.ComponentModel;
using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public class BatchCliWindow : Form
{
    private readonly SpineCliService _cli = new();
    private readonly ConfigService _config = new();

    // ── Top bar: Spine.com path ──
    private readonly TextBox _txtSpinePath = new();
    private readonly Button _btnDetect = new();
    private readonly Button _btnBrowseSpine = new();
    private readonly Label _lblSpineStatus = new();

    // ── Tab control ──
    private readonly TabControl _tabControl = new();
    private readonly TabPage _tabMerge = new();
    private readonly TabPage _tabExport = new();

    // ── Merge tab ──
    private readonly ListView _lvSource = new();
    private readonly ListView _lvTarget = new();
    private readonly Button _btnSourceAdd = new();
    private readonly Button _btnSourceRemove = new();
    private readonly Button _btnTargetAdd = new();
    private readonly Button _btnTargetRemove = new();
    private readonly Button _btnMergeExecute = new();
    private readonly Label _lblMergeHint = new();

    // ── Export tab ──
    private readonly TextBox _txtSourceDir = new();
    private readonly Button _btnBrowseSource = new();
    private readonly Button _btnScan = new();
    private readonly ListView _lvExportFiles = new();
    private readonly Button _btnRefresh = new();
    private readonly TextBox _txtOutputDir = new();
    private readonly Button _btnBrowseOutput = new();
    private readonly Button _btnExport = new();
    private readonly Button _btnPack = new();
    private readonly Button _btnVersion = new();

    private readonly BindingList<SpineCliEntry> _exportEntries = [];

    private static readonly Color ColorOk = Color.FromArgb(0xE8, 0xFF, 0xE8);
    private static readonly Color ColorMissing = Color.FromArgb(0xFF, 0xE8, 0xE8);

    public BatchCliWindow()
    {
        Text = "CLI批量合并/导出";
        Icon = IconService.AppIcon;
        Size = new Size(900, 650);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterParent;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(BuildTopBar(), 0, 0);
        BuildTabControl();
        layout.Controls.Add(_tabControl, 0, 1);

        Controls.Add(layout);
        LoadSavedPath();
    }

    // ────────────────────── Top bar (shared) ──────────────────────

    private Panel BuildTopBar()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(10, 6, 10, 0)
        };

        var lbl = new Label { Text = "Spine.com 路径:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        _txtSpinePath.Size = new Size(350, 24);
        _btnDetect.Text = "检测";
        _btnDetect.AutoSize = true;
        _btnDetect.MinimumSize = new Size(60, 28);
        _btnDetect.FlatStyle = FlatStyle.Flat;
        _btnDetect.Click += (_, _) => DetectSpine();

        _btnBrowseSpine.Text = "选择";
        _btnBrowseSpine.AutoSize = true;
        _btnBrowseSpine.MinimumSize = new Size(60, 28);
        _btnBrowseSpine.FlatStyle = FlatStyle.Flat;
        _btnBrowseSpine.Click += (_, _) =>
        {
            using var dlg = new OpenFileDialog
            {
                Title = "选择 Spine.com",
                Filter = "Spine.com|Spine.com|可执行文件|*.exe",
                CheckFileExists = true
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _txtSpinePath.Text = dlg.FileName;
                SaveAndValidatePath();
            }
        };

        _lblSpineStatus.AutoSize = true;
        _lblSpineStatus.TextAlign = ContentAlignment.MiddleLeft;

        flow.Controls.AddRange([lbl, _txtSpinePath, _btnDetect, _btnBrowseSpine, _lblSpineStatus]);
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(flow);
        return panel;
    }

    private void DetectSpine()
    {
        var found = _cli.DetectFromRegistry();
        if (found != null)
        {
            _txtSpinePath.Text = found;
            SaveAndValidatePath();
        }
        else
        {
            MessageBox.Show(this, "未找到 Spine.com，请手动选择路径。", "未检测到", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SaveAndValidatePath()
    {
        _cli.SpinePath = _txtSpinePath.Text.Trim();
        ConfigService.SaveCliSpinePath(_cli.SpinePath);
        _lblSpineStatus.Text = _cli.IsValid ? "✅ 有效" : "❌ 文件不存在";
        _lblSpineStatus.ForeColor = _cli.IsValid ? Color.Green : Color.Red;
    }

    private void LoadSavedPath()
    {
        var saved = ConfigService.LoadCliSpinePath();
        if (!string.IsNullOrEmpty(saved))
        {
            _txtSpinePath.Text = saved;
            SaveAndValidatePath();
        }
    }

    // ────────────────────── TabControl ──────────────────────

    private void BuildTabControl()
    {
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Padding = new Point(10, 10);
        BuildMergeTab();
        BuildExportTab();
        _tabControl.TabPages.Add(_tabMerge);
        _tabControl.TabPages.Add(_tabExport);
    }

    // ────────────────────── Merge Tab ──────────────────────

    private readonly BindingList<SpineCliEntry> _sourceEntries = [];
    private readonly BindingList<SpineCliEntry> _targetEntries = [];

    private void BuildMergeTab()
    {
        _tabMerge.Text = "合并";
        _tabMerge.Padding = new Padding(10);

        var mergePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(10)
        };
        mergePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        mergePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
        mergePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Row 0: Headers
        var srcHeader = new Label { Text = "源文件", Font = new Font("Microsoft YaHei", 10, FontStyle.Bold), Dock = DockStyle.Fill };
        mergePanel.Controls.Add(srcHeader, 0, 0);
        var tgtHeader = new Label { Text = "目标文件", Font = new Font("Microsoft YaHei", 10, FontStyle.Bold), Dock = DockStyle.Fill };
        mergePanel.Controls.Add(tgtHeader, 2, 0);

        // Row 1: Lists
        _lvSource.View = View.Details;
        _lvSource.FullRowSelect = true;
        _lvSource.MultiSelect = true;
        _lvSource.Dock = DockStyle.Fill;
        _lvSource.Columns.Add("文件名", 220);
        _lvSource.Columns.Add("路径", 380);
        mergePanel.Controls.Add(_lvSource, 0, 1);

        _lvTarget.View = View.Details;
        _lvTarget.FullRowSelect = true;
        _lvTarget.MultiSelect = true;
        _lvTarget.Dock = DockStyle.Fill;
        _lvTarget.Columns.Add("文件名", 220);
        _lvTarget.Columns.Add("路径", 380);
        mergePanel.Controls.Add(_lvTarget, 2, 1);

        // Row 2: Buttons below lists (span both columns)
        var btnRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 0)
        };
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _btnSourceAdd.Text = "添加源文件";
        _btnSourceAdd.AutoSize = true;
        _btnSourceAdd.MinimumSize = new Size(100, 30);
        _btnSourceAdd.FlatStyle = FlatStyle.Flat;
        _btnSourceAdd.Click += (_, _) => AddSourceFile();

        _btnSourceRemove.Text = "删除源文件";
        _btnSourceRemove.AutoSize = true;
        _btnSourceRemove.MinimumSize = new Size(100, 30);
        _btnSourceRemove.FlatStyle = FlatStyle.Flat;
        _btnSourceRemove.Click += (_, _) => RemoveSelected(_lvSource, _sourceEntries);

        _btnTargetAdd.Text = "添加目标文件";
        _btnTargetAdd.AutoSize = true;
        _btnTargetAdd.MinimumSize = new Size(100, 30);
        _btnTargetAdd.FlatStyle = FlatStyle.Flat;
        _btnTargetAdd.Click += (_, _) => AddTargetFiles();

        _btnTargetRemove.Text = "删除目标文件";
        _btnTargetRemove.AutoSize = true;
        _btnTargetRemove.MinimumSize = new Size(100, 30);
        _btnTargetRemove.FlatStyle = FlatStyle.Flat;
        _btnTargetRemove.Click += (_, _) => RemoveSelected(_lvTarget, _targetEntries);

        var leftBtns = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        leftBtns.Controls.AddRange([_btnSourceAdd, _btnSourceRemove]);
        var rightBtns = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft };
        rightBtns.Controls.AddRange([_btnTargetRemove, _btnTargetAdd]);

        btnRow.Controls.Add(leftBtns, 0, 0);
        btnRow.Controls.Add(rightBtns, 1, 0);
        mergePanel.Controls.Add(btnRow, 0, 2);
        mergePanel.SetColumnSpan(btnRow, 3);

        // Row 3: Hint + execute
        _lblMergeHint.Text = "规则：只能一边多选（1源→N目标 或 N源→1目标）";
        _lblMergeHint.ForeColor = Color.Gray;
        _lblMergeHint.AutoSize = true;
        mergePanel.Controls.Add(_lblMergeHint, 0, 3);

        _btnMergeExecute.Text = "执行合并";
        _btnMergeExecute.AutoSize = true;
        _btnMergeExecute.MinimumSize = new Size(120, 36);
        _btnMergeExecute.BackColor = Color.FromArgb(0x00, 0x78, 0xD7);
        _btnMergeExecute.ForeColor = Color.White;
        _btnMergeExecute.FlatStyle = FlatStyle.Flat;
        _btnMergeExecute.Click += async (_, _) => await ExecuteMerge();
        mergePanel.Controls.Add(_btnMergeExecute, 2, 3);

        _tabMerge.Controls.Add(mergePanel);
    }

    private void AddSourceFile()
    {
        // Try file selection first
        using var fileDlg = new OpenFileDialog
        {
            Title = "选择源文件（.spine/.json/.skel），取消则浏览文件夹",
            Filter = "Spine 文件 (*.spine;*.json;*.skel)|*.spine;*.json;*.skel",
            Multiselect = true,
            CheckFileExists = true
        };
        if (fileDlg.ShowDialog() == DialogResult.OK)
        {
            foreach (var f in fileDlg.FileNames)
                AddMergeEntry(_lvSource, _sourceEntries, f);
            return;
        }

        // Fallback to folder traversal
        using var folderDlg = new FolderBrowserDialog { Description = "选择包含 Spine 文件的目录" };
        if (folderDlg.ShowDialog() != DialogResult.OK) return;

        var allFiles = new List<string>();
        allFiles.AddRange(Directory.GetFiles(folderDlg.SelectedPath, "*.spine", SearchOption.AllDirectories));
        allFiles.AddRange(Directory.GetFiles(folderDlg.SelectedPath, "*.json", SearchOption.AllDirectories));
        allFiles.AddRange(Directory.GetFiles(folderDlg.SelectedPath, "*.skel", SearchOption.AllDirectories));

        if (allFiles.Count == 0)
        {
            MessageBox.Show(this, "该目录下未找到 Spine 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new SubfolderSelectDialog([.. allFiles]);
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            foreach (var f in picker.SelectedFolders)
                AddMergeEntry(_lvSource, _sourceEntries, f);
        }
    }

    private void AddTargetFiles()
    {
        using var dlg = new FolderBrowserDialog { Description = "选择包含 .spine 文件的目录" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var files = Directory.GetFiles(dlg.SelectedPath, "*.spine", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            MessageBox.Show(this, "该目录下未找到 .spine 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new SubfolderSelectDialog([.. files]);
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            foreach (var f in picker.SelectedFolders)
                AddMergeEntry(_lvTarget, _targetEntries, f);
        }
    }

    // For merge tab: no export.json status
    private void AddMergeEntry(ListView list, BindingList<SpineCliEntry> entries, string filePath)
    {
        if (entries.Any(e => e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))) return;

        var entry = new SpineCliEntry { FilePath = filePath };
        entries.Add(entry);

        var item = new ListViewItem(entry.FileName);
        item.SubItems.Add(filePath);
        item.Tag = entry;
        list.Items.Add(item);
    }

    // For export tab: with export.json status
    private void AddExportEntry(ListView list, BindingList<SpineCliEntry> entries, string filePath)
    {
        if (entries.Any(e => e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))) return;

        var dir = Path.GetDirectoryName(filePath);
        var hasConfig = !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "export.json"));

        var entry = new SpineCliEntry
        {
            FilePath = filePath,
            HasExportConfig = hasConfig,
            ExportConfigPath = hasConfig ? Path.Combine(dir!, "export.json") : null
        };
        entries.Add(entry);

        var item = new ListViewItem(entry.FileName);
        item.SubItems.Add(hasConfig ? "✅" : "❌");
        item.SubItems.Add(filePath);
        item.BackColor = hasConfig ? ColorOk : ColorMissing;
        item.Tag = entry;
        list.Items.Add(item);
    }

    private void RemoveSelected(ListView list, BindingList<SpineCliEntry> entries)
    {
        if (list.SelectedItems.Count == 0) return;
        var toRemove = list.SelectedItems.Cast<ListViewItem>()
            .Select(i => i.Tag as SpineCliEntry)
            .Where(e => e != null)
            .Cast<SpineCliEntry>()
            .ToList();
        foreach (var e in toRemove)
        {
            entries.Remove(e);
            var item = list.Items.Cast<ListViewItem>().FirstOrDefault(i => ReferenceEquals(i.Tag, e));
            if (item != null) list.Items.Remove(item);
        }
    }

    private async Task ExecuteMerge()
    {
        if (!_cli.IsValid)
        {
            MessageBox.Show(this, "请先设置有效的 Spine.com 路径。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var srcCount = _sourceEntries.Count;
        var tgtCount = _targetEntries.Count;

        if (srcCount == 0 || tgtCount == 0)
        {
            MessageBox.Show(this, "源列表和目标列表都不能为空。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (srcCount > 1 && tgtCount > 1)
        {
            MessageBox.Show(this, "源列表和目标列表只能一边多选。\n（1源→N目标 或 N源→1目标）", "规则错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnMergeExecute.Enabled = false;
        _btnMergeExecute.Text = "合并中...";

        try
        {
            var errors = new List<string>();
            var tempDir = Path.Combine(Path.GetTempPath(), "KeyMacro", "cli_merge");
            Directory.CreateDirectory(tempDir);

            if (srcCount == 1)
            {
                // One source to many targets
                var source = _sourceEntries[0];
                foreach (var target in _targetEntries)
                {
                    var result = await MergeOne(source, target, tempDir);
                    if (!result.Success) errors.Add($"{target.FileName}: {result.Error}");
                }
            }
            else
            {
                // Many sources to one target
                var target = _targetEntries[0];
                foreach (var source in _sourceEntries)
                {
                    var result = await MergeOne(source, target, tempDir);
                    if (!result.Success) errors.Add($"{source.FileName}: {result.Error}");
                }
            }

            // Cleanup temp
            try { Directory.Delete(tempDir, true); } catch { }

            if (errors.Count > 0)
                MessageBox.Show(this, $"合并完成，但有 {errors.Count} 个错误：\n{string.Join("\n", errors)}", "合并结果", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show(this, "合并全部完成！", "合并结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            _btnMergeExecute.Enabled = true;
            _btnMergeExecute.Text = "执行合并";
        }
    }

    private async Task<CliResult> MergeOne(SpineCliEntry source, SpineCliEntry target, string tempDir)
    {
        var ext = Path.GetExtension(source.FilePath).ToLowerInvariant();
        var targetDir = Path.GetDirectoryName(target.FilePath)!;
        var targetName = Path.GetFileNameWithoutExtension(target.FilePath);
        var outputPath = Path.Combine(targetDir, $"{targetName}_merged.spine");

        try
        {
            // Step 1: copy target as base for output
            File.Copy(target.FilePath, outputPath, overwrite: true);

            // Step 2: resolve source data (import json/skel → temp .spine if needed)
            string sourceForImport = source.FilePath;
            if (ext is ".json" or ".skel")
            {
                sourceForImport = Path.Combine(tempDir, $"{Guid.NewGuid():N}.spine");
                var importResult = await _cli.ImportToTemp(source.FilePath, sourceForImport);
                if (!importResult.Success) return importResult;
            }

            // Step 3: import source data into the target copy (this merges into the existing project)
            return await _cli.ImportMerge(sourceForImport, outputPath);
        }
        catch (Exception ex)
        {
            return new CliResult { ExitCode = -1, Error = ex.Message };
        }
    }

    // ────────────────────── Export Tab ──────────────────────

    private void BuildExportTab()
    {
        _tabExport.Text = "批量导出";
        _tabExport.Padding = new Padding(10);

        var exportPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10)
        };
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Row 0: Source dir
        exportPanel.Controls.Add(new Label { Text = "源目录:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var srcDirPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _txtSourceDir.Size = new Size(350, 24);
        _btnBrowseSource.Text = "浏览";
        _btnBrowseSource.AutoSize = true;
        _btnBrowseSource.MinimumSize = new Size(60, 28);
        _btnBrowseSource.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                _txtSourceDir.Text = dlg.SelectedPath;
        };
        _btnScan.Text = "扫描";
        _btnScan.AutoSize = true;
        _btnScan.MinimumSize = new Size(60, 28);
        _btnScan.BackColor = Color.FromArgb(0x00, 0x78, 0xD7);
        _btnScan.ForeColor = Color.White;
        _btnScan.FlatStyle = FlatStyle.Flat;
        _btnScan.Click += ScanSourceDir;
        srcDirPanel.Controls.AddRange([_txtSourceDir, _btnBrowseSource, _btnScan]);
        exportPanel.Controls.Add(srcDirPanel, 1, 0);

        // Row 1: File list
        _lvExportFiles.View = View.Details;
        _lvExportFiles.FullRowSelect = true;
        _lvExportFiles.Dock = DockStyle.Fill;
        _lvExportFiles.Columns.Add("文件名", 200);
        _lvExportFiles.Columns.Add("export.json", 100);
        _lvExportFiles.Columns.Add("路径", 400);

        var listPanel = new Panel { Dock = DockStyle.Fill };
        _lvExportFiles.Parent = listPanel;
        _lvExportFiles.Dock = DockStyle.Fill;
        exportPanel.Controls.Add(listPanel, 0, 1);
        exportPanel.SetColumnSpan(listPanel, 2);

        // Refresh button next to list
        _btnRefresh.Text = "刷新状态";
        _btnRefresh.AutoSize = true;
        _btnRefresh.MinimumSize = new Size(80, 28);
        _btnRefresh.FlatStyle = FlatStyle.Flat;
        _btnRefresh.Click += (_, _) => RefreshExportStatus();

        var refreshPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, 2, 0, 0) };
        refreshPanel.Controls.Add(_btnRefresh);
        exportPanel.Controls.Add(refreshPanel, 1, 2);

        // Row 3: Output dir
        exportPanel.Controls.Add(new Label { Text = "输出目录:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        var outDirPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _txtOutputDir.Size = new Size(350, 24);
        _btnBrowseOutput.Text = "浏览";
        _btnBrowseOutput.AutoSize = true;
        _btnBrowseOutput.MinimumSize = new Size(60, 28);
        _btnBrowseOutput.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                _txtOutputDir.Text = dlg.SelectedPath;
        };
        outDirPanel.Controls.AddRange([_txtOutputDir, _btnBrowseOutput]);
        exportPanel.Controls.Add(outDirPanel, 1, 3);

        // Row 4: Action buttons
        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };

        _btnExport.Text = "导出";
        _btnExport.AutoSize = true;
        _btnExport.MinimumSize = new Size(100, 36);
        _btnExport.BackColor = Color.FromArgb(0x00, 0x78, 0xD7);
        _btnExport.ForeColor = Color.White;
        _btnExport.FlatStyle = FlatStyle.Flat;
        _btnExport.Click += async (_, _) => await ExecuteExport();

        _btnPack.Text = "单纹理图";
        _btnPack.AutoSize = true;
        _btnPack.MinimumSize = new Size(100, 36);
        _btnPack.BackColor = Color.FromArgb(0x6B, 0x46, 0xC3);
        _btnPack.ForeColor = Color.White;
        _btnPack.FlatStyle = FlatStyle.Flat;
        _btnPack.Click += async (_, _) => await ExecutePack();

        _btnVersion.Text = "改版本号";
        _btnVersion.AutoSize = true;
        _btnVersion.MinimumSize = new Size(100, 36);
        _btnVersion.BackColor = Color.FromArgb(0xE8, 0xE8, 0xE8);
        _btnVersion.ForeColor = Color.Black;
        _btnVersion.FlatStyle = FlatStyle.Flat;
        _btnVersion.Click += async (_, _) => await ExecuteVersionUpdate();

        actionPanel.Controls.AddRange([_btnExport, _btnPack, _btnVersion]);
        exportPanel.Controls.Add(actionPanel, 1, 4);

        _tabExport.Controls.Add(exportPanel);
    }

    private void ScanSourceDir(object sender, EventArgs e)
    {
        var dir = _txtSourceDir.Text.Trim();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            MessageBox.Show(this, "请输入有效的目录路径。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var files = Directory.GetFiles(dir, "*.spine", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            MessageBox.Show(this, "该目录下未找到 .spine 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new SubfolderSelectDialog([.. files]);
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            _exportEntries.Clear();
            _lvExportFiles.Items.Clear();
            foreach (var f in picker.SelectedFolders)
                AddExportEntry(_lvExportFiles, _exportEntries, f);
        }
    }

    private void RefreshExportStatus()
    {
        foreach (ListViewItem item in _lvExportFiles.Items)
        {
            if (item.Tag is SpineCliEntry entry)
            {
                var dir = Path.GetDirectoryName(entry.FilePath);
                var hasConfig = !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "export.json"));
                entry.HasExportConfig = hasConfig;
                entry.ExportConfigPath = hasConfig ? Path.Combine(dir, "export.json") : null;
                item.SubItems[1].Text = hasConfig ? "✅" : "❌";
                item.BackColor = hasConfig ? ColorOk : ColorMissing;
            }
        }
    }

    private async Task ExecuteExport()
    {
        if (!ValidateExportReady()) return;

        _btnExport.Enabled = false;
        _btnExport.Text = "导出中...";

        try
        {
            var outputDir = _txtOutputDir.Text.Trim();
            Directory.CreateDirectory(outputDir);

            var missingList = new List<string>();
            int ok = 0, fail = 0;

            foreach (var entry in _exportEntries)
            {
                try
                {
                    CliResult result;
                    if (entry.HasExportConfig && entry.ExportConfigPath != null)
                        result = await _cli.Export(entry.FilePath, outputDir, entry.ExportConfigPath);
                    else
                    {
                        result = await _cli.ExportDefault(entry.FilePath, outputDir);
                        missingList.Add(entry.FileName);
                    }

                    if (result.Success) ok++;
                    else { fail++; OperationLogger.Error($"CLI export failed: {entry.FileName}: {result.Error}"); }
                }
                catch (Exception ex)
                {
                    fail++;
                    OperationLogger.Error($"CLI export error: {entry.FileName}: {ex.Message}");
                }
            }

            if (missingList.Count > 0)
            {
                var logPath = Path.Combine(outputDir, "cli_export_log.txt");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logLines = new List<string>
                {
                    $"[{timestamp}] 以下文件缺少 export.json，使用默认导出：",
                    string.Join("\n", missingList.Select(f => $"  - {f}")),
                    "---",
                };
                File.AppendAllLines(logPath, logLines);

                MessageBox.Show(this,
                    $"导出完成！成功 {ok}，失败 {fail}\n\n以下文件缺少 export.json，已使用默认导出：\n{string.Join("\n", missingList)}",
                    "导出结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, $"导出完成！成功 {ok}，失败 {fail}", "导出结果", MessageBoxButtons.OK,
                    fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
        }
        finally
        {
            _btnExport.Enabled = true;
            _btnExport.Text = "导出";
        }
    }

    private async Task ExecutePack()
    {
        if (!ValidateExportReady()) return;

        _btnPack.Enabled = false;
        _btnPack.Text = "打包中...";

        try
        {
            var outputDir = _txtOutputDir.Text.Trim();
            Directory.CreateDirectory(outputDir);

            int ok = 0, fail = 0;
            foreach (var entry in _exportEntries)
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(entry.FileName);
                    var result = await _cli.Pack(entry.FilePath, outputDir, name);
                    if (result.Success) ok++;
                    else { fail++; OperationLogger.Error($"CLI pack failed: {entry.FileName}: {result.Error}"); }
                }
                catch (Exception ex)
                {
                    fail++;
                    OperationLogger.Error($"CLI pack error: {entry.FileName}: {ex.Message}");
                }
            }

            MessageBox.Show(this, $"纹理打包完成！成功 {ok}，失败 {fail}", "打包结果", MessageBoxButtons.OK,
                fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        finally
        {
            _btnPack.Enabled = true;
            _btnPack.Text = "单纹理图";
        }
    }

    private async Task ExecuteVersionUpdate()
    {
        if (_exportEntries.Count == 0)
        {
            MessageBox.Show(this, "请先扫描并选择文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var version = Microsoft.VisualBasic.Interaction.InputBox("输入目标 Spine 版本号（如 4.2.34）：", "修改版本号", "4.2");
        if (string.IsNullOrWhiteSpace(version)) return;

        _btnVersion.Enabled = false;
        _btnVersion.Text = "更新中...";

        try
        {
            int ok = 0, fail = 0;
            foreach (var entry in _exportEntries.ToList())
            {
                try
                {
                    var dir = Path.GetDirectoryName(entry.FilePath)!;
                    var name = Path.GetFileNameWithoutExtension(entry.FileName);
                    var newName = $"{name}_{version}.spine";
                    var newPath = Path.Combine(dir, newName);
                    var result = await _cli.UpdateVersion(entry.FilePath, version, newPath);

                    if (result.Success)
                    {
                        // Update entry and ListView
                        entry.FilePath = newPath;
                        var item = _lvExportFiles.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == entry);
                        if (item != null)
                        {
                            item.Text = entry.FileName;
                            item.SubItems[2].Text = newPath;
                        }
                        ok++;
                    }
                    else
                    {
                        fail++;
                        OperationLogger.Error($"CLI version update failed: {entry.FileName}: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    fail++;
                    OperationLogger.Error($"CLI version update error: {entry.FileName}: {ex.Message}");
                }
            }

            // Re-scan export.json status for updated files
            RefreshExportStatus();

            MessageBox.Show(this, $"版本更新完成！成功 {ok}，失败 {fail}", "更新结果", MessageBoxButtons.OK,
                fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        finally
        {
            _btnVersion.Enabled = true;
            _btnVersion.Text = "改版本号";
        }
    }

    private bool ValidateExportReady()
    {
        if (!_cli.IsValid)
        {
            MessageBox.Show(this, "请先设置有效的 Spine.com 路径。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (_exportEntries.Count == 0)
        {
            MessageBox.Show(this, "请先扫描并选择文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        var outputDir = _txtOutputDir.Text.Trim();
        if (string.IsNullOrEmpty(outputDir))
        {
            MessageBox.Show(this, "请设置输出目录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }
}
