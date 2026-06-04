#nullable enable
using System.ComponentModel;
using KeyMacro.Controls;
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
    private readonly Button _btnCancelCli = new();
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
    private readonly Button _btnAnimSelect = new();
    private readonly Button _btnTargetAdd = new();
    private readonly Button _btnTargetRemove = new();
    private readonly Button _btnMergeExecute = new();
    private readonly Label _lblMergeHint = new();
    private readonly TextBox _txtFromName = new();
    private readonly TextBox _txtToName = new();
    private readonly CheckBox _chkExperimental = new();

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
    private readonly RadioButton _rbFinish = new();
    private readonly RadioButton _rbDemotion = new();
    private readonly RadioButton _rbOther = new();
    private string _exportConfigName = "export.json";

    private readonly BindingList<SpineCliEntry> _exportEntries = [];
    private CancellationTokenSource? _cliCts;

    // ── Progress bar ──
    private readonly Label _lblProgressCurrent = new();
    private readonly TextProgressBar _progressBar = new();

    private static readonly Color ColorOk = Color.FromArgb(0xE8, 0xFF, 0xE8);
    private static readonly Color ColorMissing = Color.FromArgb(0xFF, 0xE8, 0xE8);

    public BatchCliWindow()
    {
        Text = "CLI批量合并/导出";
        Icon = IconService.AppIcon;
        ClientSize = new Size(1440, 900);
        MinimumSize = new Size(1040, 680);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        layout.Controls.Add(BuildTopBar(), 0, 0);
        BuildTabControl();
        layout.Controls.Add(_tabControl, 0, 1);

        var progressPanel = BuildProgressPanel();
        layout.Controls.Add(progressPanel, 0, 2);

        Controls.Add(layout);
        UiTheme.Apply(this, UiWindowProfile.BatchCli);
        LoadSavedPath();
    }

    // ────────────────────── Top bar (shared) ──────────────────────

    private Panel BuildTopBar()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(10, 6, 10, 0),
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4)
        };

        var lbl = new Label { Text = "Spine.com 路径:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        _txtSpinePath.Size = new Size(620, 24);
        _txtSpinePath.BorderStyle = BorderStyle.FixedSingle;
        _txtSpinePath.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        _btnDetect.Text = "检测";
        _btnDetect.AutoSize = true;
        _btnDetect.MinimumSize = new Size(60, 28);
        _btnDetect.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnDetect);
        _btnDetect.Click += (_, _) => DetectSpine();

        _btnBrowseSpine.Text = "选择";
        _btnBrowseSpine.AutoSize = true;
        _btnBrowseSpine.MinimumSize = new Size(60, 28);
        _btnBrowseSpine.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnBrowseSpine);
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

        _btnCancelCli.Text = "取消CLI";
        _btnCancelCli.AutoSize = true;
        _btnCancelCli.MinimumSize = new Size(70, 28);
        _btnCancelCli.FlatStyle = FlatStyle.Flat;
        _btnCancelCli.Enabled = false;
        StyleButton(_btnCancelCli, Color.FromArgb(0xD9, 0x5C, 0x5C), Color.White);
        _btnCancelCli.Click += (_, _) => CancelCliOperation();

        flow.Controls.AddRange([lbl, _txtSpinePath, _btnDetect, _btnBrowseSpine, _btnCancelCli, _lblSpineStatus]);
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4) };
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

    private CancellationToken BeginCliOperation()
    {
        _cliCts?.Dispose();
        _cliCts = new CancellationTokenSource();
        _btnCancelCli.Enabled = true;
        return _cliCts.Token;
    }

    private void EndCliOperation()
    {
        _btnCancelCli.Enabled = false;
        _cliCts?.Dispose();
        _cliCts = null;
    }

    private void CancelCliOperation()
    {
        _cliCts?.Cancel();
        _lblProgressCurrent.Text = "正在取消...";
    }

    // ────────────────────── TabControl ──────────────────────

    private void BuildTabControl()
    {
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Padding = new Point(10, 10);
        _tabControl.BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);
        BuildMergeTab();
        BuildExportTab();
        _tabControl.TabPages.Add(_tabMerge);
        _tabControl.TabPages.Add(_tabExport);
    }

    // ────────────────────── Merge Tab ──────────────────────

    private readonly BindingList<SpineCliEntry> _sourceEntries = [];
    private readonly BindingList<SpineCliEntry> _targetEntries = [];

    private static TableLayoutPanel BuildSectionContainer(string title)
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        container.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold),
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Color.FromArgb(0xE1, 0xE7, 0xEA),
            ForeColor = Color.Black
        }, 0, 0);
        return container;
    }

    private void BuildMergeTab()
    {
        _tabMerge.Text = "合并";
        _tabMerge.Padding = new Padding(10);
        _tabMerge.BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);

        var mergeContainer = BuildSectionContainer("合并任务");
        var mergePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 6,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3)
        };
        mergePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        mergePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
        mergePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mergePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

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
        StyleListView(_lvSource);
        _lvSource.Columns.Add("文件名", 170);
        _lvSource.Columns.Add("动画", 150);
        _lvSource.Columns.Add("路径", 260);
        _lvSource.DoubleClick += (_, _) => ShowAnimSelectForSource();
        mergePanel.Controls.Add(_lvSource, 0, 1);

        _lvTarget.View = View.Details;
        _lvTarget.FullRowSelect = true;
        _lvTarget.MultiSelect = true;
        _lvTarget.Dock = DockStyle.Fill;
        StyleListView(_lvTarget);
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
        StyleButton(_btnSourceAdd);
        _btnSourceAdd.Click += (_, _) => AddSourceFile();

        _btnSourceRemove.Text = "删除源文件";
        _btnSourceRemove.AutoSize = true;
        _btnSourceRemove.MinimumSize = new Size(100, 30);
        _btnSourceRemove.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnSourceRemove);
        _btnSourceRemove.Click += (_, _) => RemoveSelected(_lvSource, _sourceEntries);

        _btnAnimSelect.Text = "动画选择";
        _btnAnimSelect.AutoSize = true;
        _btnAnimSelect.MinimumSize = new Size(80, 30);
        _btnAnimSelect.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnAnimSelect, Color.FromArgb(0x14, 0x87, 0x92), Color.White);
        _btnAnimSelect.Click += (_, _) => ShowAnimSelectForSource();

        _btnTargetAdd.Text = "添加目标文件";
        _btnTargetAdd.AutoSize = true;
        _btnTargetAdd.MinimumSize = new Size(100, 30);
        _btnTargetAdd.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnTargetAdd);
        _btnTargetAdd.Click += (_, _) => AddTargetFiles();

        _btnTargetRemove.Text = "删除目标文件";
        _btnTargetRemove.AutoSize = true;
        _btnTargetRemove.MinimumSize = new Size(100, 30);
        _btnTargetRemove.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnTargetRemove);
        _btnTargetRemove.Click += (_, _) => RemoveSelected(_lvTarget, _targetEntries);

        var leftBtns = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3) };
        leftBtns.Controls.AddRange([_btnSourceAdd, _btnSourceRemove, _btnAnimSelect]);
        var rightBtns = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3) };
        rightBtns.Controls.AddRange([_btnTargetRemove, _btnTargetAdd]);

        btnRow.Controls.Add(leftBtns, 0, 0);
        btnRow.Controls.Add(rightBtns, 1, 0);
        mergePanel.Controls.Add(btnRow, 0, 2);
        mergePanel.SetColumnSpan(btnRow, 3);

        // Row 3: from/to skeleton name inputs
        var fromPanel = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(0, 4, 0, 0) };
        fromPanel.Controls.Add(new Label { Text = "--from:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _txtFromName.Size = new Size(140, 24);
        _txtFromName.PlaceholderText = "默认";
        _txtFromName.BorderStyle = BorderStyle.FixedSingle;
        _txtFromName.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        fromPanel.Controls.Add(_txtFromName);
        mergePanel.Controls.Add(fromPanel, 0, 3);

        var toPanel = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(0, 4, 0, 0) };
        toPanel.Controls.Add(new Label { Text = "--to:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        _txtToName.Size = new Size(140, 24);
        _txtToName.PlaceholderText = "默认";
        _txtToName.BorderStyle = BorderStyle.FixedSingle;
        _txtToName.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        toPanel.Controls.Add(_txtToName);
        mergePanel.Controls.Add(toPanel, 2, 3);

        // Row 4: Hint + execute + experimental checkbox
        _lblMergeHint.Text = "规则：只能一边多选（1源→N目标 或 N源→1目标）";
        _lblMergeHint.ForeColor = Color.Gray;
        _lblMergeHint.AutoSize = true;
        mergePanel.Controls.Add(_lblMergeHint, 0, 4);

        var execPanel = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(0, 4, 0, 0) };

        _btnMergeExecute.Text = "执行合并";
        _btnMergeExecute.AutoSize = true;
        _btnMergeExecute.MinimumSize = new Size(120, 36);
        _btnMergeExecute.BackColor = Color.FromArgb(0x00, 0x78, 0xD7);
        _btnMergeExecute.ForeColor = Color.White;
        _btnMergeExecute.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnMergeExecute, Color.FromArgb(0x00, 0x78, 0xD7), Color.White);
        _btnMergeExecute.Click += async (_, _) => await ExecuteMerge();

        _chkExperimental.Text = "实验功能：CLI骨架合并(4.3)";
        _chkExperimental.AutoSize = true;
        _chkExperimental.Margin = new Padding(8, 0, 0, 0);
        _chkExperimental.TextAlign = ContentAlignment.MiddleLeft;

        execPanel.Controls.AddRange([_btnMergeExecute, _chkExperimental]);
        mergePanel.Controls.Add(execPanel, 2, 4);

        mergeContainer.Controls.Add(mergePanel, 0, 1);
        _tabMerge.Controls.Add(mergeContainer);
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
        if (list == _lvSource)
        {
            // Source has 3 columns: 文件名, 动画, 路径
            item.SubItems.Add("全部"); // animations
            item.SubItems.Add(filePath);
        }
        else
        {
            // Target has 2 columns: 文件名, 路径
            item.SubItems.Add(filePath);
        }
        item.Tag = entry;
        list.Items.Add(item);
    }

    // For export tab: with export.json status
    private void AddExportEntry(ListView list, BindingList<SpineCliEntry> entries, string filePath)
    {
        if (entries.Any(e => e.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))) return;

        var dir = Path.GetDirectoryName(filePath);
        var configPath = !string.IsNullOrEmpty(dir) ? Path.Combine(dir, _exportConfigName) : null;
        var hasConfig = configPath != null && File.Exists(configPath);

        var entry = new SpineCliEntry
        {
            FilePath = filePath,
            HasExportConfig = hasConfig,
            ExportConfigPath = hasConfig ? configPath : null
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

    /// <summary>Show animation selection dialog for selected source entry.</summary>
    private async void ShowAnimSelectForSource()
    {
        if (_lvSource.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "请先在源文件列表中选择一个文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var entry = _lvSource.SelectedItems[0].Tag as SpineCliEntry;
        if (entry == null) return;

        // Get animation list from Spine -i
        var path = entry.FilePath;
        if (!File.Exists(path)) return;

        var info = await _cli.GetProjectInfo(path);
        if (info.AnimationNames.Count == 0)
        {
            MessageBox.Show(this, "未能获取动画列表，请确认文件有效。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new Form
        {
            Text = $"选择动画 - {entry.FileName}",
            Size = new Size(400, 450),
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.Sizable,
            BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA)
        };

        var clb = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White
        };
        for (int i = 0; i < info.AnimationNames.Count; i++)
            clb.Items.Add(info.AnimationNames[i],
                entry.SelectedAnimations.Count == 0 || entry.SelectedAnimations.Contains(info.AnimationNames[i]));

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4)
        };

        var btnCancel = new Button { Text = "取消", AutoSize = true, MinimumSize = new Size(80, 32), FlatStyle = FlatStyle.Flat };
        StyleButton(btnCancel);
        btnCancel.Click += (_, _) => { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); };

        var btnOk = new Button
        {
            Text = "确认", AutoSize = true, MinimumSize = new Size(80, 32), FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x00, 0x78, 0xD7), ForeColor = Color.White
        };
        StyleButton(btnOk, Color.FromArgb(0x00, 0x78, 0xD7), Color.White);
        btnOk.Click += (_, _) =>
        {
            entry.SelectedAnimations = clb.CheckedItems.Cast<string>().ToList();
            // Update ListView animation column
            var item = _lvSource.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == entry);
            if (item != null) item.SubItems[1].Text = entry.SelectedAnimations.Count > 0
                ? string.Join(",", entry.SelectedAnimations) : "全部";
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        bottomPanel.Controls.AddRange([btnCancel, btnOk]);
        dialog.Controls.Add(clb);
        dialog.Controls.Add(bottomPanel);
        UiTheme.Apply(dialog, UiWindowProfile.AnimationSelect);
        dialog.ShowDialog(this);
    }

    private async Task ExecuteMerge()
    {
        if (!_cli.IsValid)
        {
            MessageBox.Show(this, "请先设置有效的 Spine.com 路径。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Experimental branch
        if (_chkExperimental.Checked)
        {
            await ExecuteExperimentalMerge();
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
        var ct = BeginCliOperation();
        var totalPairs = Math.Max(srcCount, tgtCount);
        var currentPair = 0;
        ShowProgress(true);

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
                    ct.ThrowIfCancellationRequested();
                    currentPair++;
                    SetProgress(currentPair, totalPairs, $"普通合并: {target.FileName}");
                    var result = await MergeOne(source, target, tempDir, ct);
                    if (!result.Success) errors.Add($"{target.FileName}: {result.Error}");
                }
            }
            else
            {
                // Many sources to one target
                var target = _targetEntries[0];
                foreach (var source in _sourceEntries)
                {
                    ct.ThrowIfCancellationRequested();
                    currentPair++;
                    SetProgress(currentPair, totalPairs, $"普通合并: {source.FileName}");
                    var result = await MergeOne(source, target, tempDir, ct);
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
        catch (OperationCanceledException)
        {
            MessageBox.Show(this, "合并已取消，当前临时文件已清理，已完成结果保留。", "已取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            ShowProgress(false);
            EndCliOperation();
            _btnMergeExecute.Enabled = true;
            _btnMergeExecute.Text = "执行合并";
        }
    }

    /// <summary>Show warning dialog for experimental feature. Returns true if user confirms.</summary>
    private bool ShowExperimentalWarning()
    {
        var msg = "实验功能：CLI骨架合并(4.3)\n\n"
            + "此功能依赖 Spine 4.3 CLI 的新功能（--merge / -a），请注意：\n\n"
            + "• 使用前请确认已下载 Spine 4.3 版本\n"
            + "• 合并可能导致目标文件版本不可逆升级\n"
            + "• 源文件与目标文件的骨架名、插槽名、动画名等如有冲突将报错终止\n"
            + "• 目前文档未完整，功能行为可能随 Spine 更新变化\n"
            + "• 建议提前备份源文件和目标文件\n\n"
            + "是否继续？";
        return MessageBox.Show(this, msg, "实验功能警告",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    /// <summary>Execute experimental merge using Spine 4.3 CLI --merge and -a.</summary>
    private async Task ExecuteExperimentalMerge()
    {
        // Show warning
        if (!ShowExperimentalWarning()) return;

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
        _btnMergeExecute.Text = "实验合并中...";
        var ct = BeginCliOperation();

        var errors = new List<string>();
        var successes = new List<string>();
        var totalPairs = Math.Max(srcCount, tgtCount);
        var currentPair = 0;

        ShowProgress(true);

        try
        {
            if (srcCount == 1)
            {
                // 1 source → N targets
                var source = _sourceEntries[0];
                foreach (var target in _targetEntries)
                {
                    ct.ThrowIfCancellationRequested();
                    currentPair++;
                    SetProgress(currentPair, totalPairs, $"实验合并: {target.FileName}");
                    var result = await ExperimentalMergeOne(source, target, ct);
                    if (result.Success)
                        successes.Add($"{target.FileName} → {result.Output}");
                    else
                        errors.Add($"{target.FileName}: {result.Error}");
                }
            }
            else
            {
                // N sources → 1 target
                var target = _targetEntries[0];
                foreach (var source in _sourceEntries)
                {
                    ct.ThrowIfCancellationRequested();
                    currentPair++;
                    SetProgress(currentPair, totalPairs, $"实验合并: {source.FileName}");
                    var result = await ExperimentalMergeOne(source, target, ct);
                    if (result.Success)
                        successes.Add($"{source.FileName} → {result.Output}");
                    else
                        errors.Add($"{source.FileName}: {result.Error}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(this, "实验合并已取消，当前临时文件已清理，已完成结果保留。", "已取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            ShowProgress(false);
            EndCliOperation();
            _btnMergeExecute.Enabled = true;
            _btnMergeExecute.Text = "执行合并";
        }

        // Report summary
        var msg = $"合并完成！成功 {successes.Count}，失败 {errors.Count}";
        if (successes.Count > 0)
            msg += $"\n\n成功：\n{string.Join("\n", successes)}";
        if (errors.Count > 0)
            msg += $"\n\n失败：\n{string.Join("\n", errors)}";

        MessageBox.Show(this, msg, "合并结果", MessageBoxButtons.OK,
            errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    /// <summary>Merge one source skeleton into one target's _merged copy.</summary>
    private async Task<CliResult> ExperimentalMergeOne(SpineCliEntry source, SpineCliEntry target, CancellationToken ct)
    {
        var srcPath = source.FilePath;
        var tgtPath = target.FilePath;

        if (!File.Exists(srcPath) || !File.Exists(tgtPath))
            return new CliResult { ExitCode = -1, Error = "文件不存在" };

        // Step 1: Create target copy as _merged.spine
        var tgtDir = Path.GetDirectoryName(tgtPath)!;
        var tgtName = Path.GetFileNameWithoutExtension(tgtPath);
        var mergedPath = Path.Combine(tgtDir, $"{tgtName}_merged.spine");
        var tempMergedPath = Path.Combine(tgtDir, $"{tgtName}_merged.{Guid.NewGuid():N}.tmp.spine");
        File.Copy(tgtPath, tempMergedPath, overwrite: true);

        // Step 2: Collect project info
        var srcInfo = await _cli.GetProjectInfo(srcPath, ct);
        var tgtInfo = await _cli.GetProjectInfo(tempMergedPath, ct);

        // Step 3: Version compatibility check
        var srcVer = ParseVersion(srcInfo.Version);
        var tgtVer = ParseVersion(tgtInfo.Version);
        var threshold = new Version(4, 3, 6);

        if (srcVer == null || tgtVer == null)
        {
            TryDeleteFile(tempMergedPath);
            return new CliResult { ExitCode = -1, Error = "无法读取项目版本号" };
        }

        string? versionArg = null;
        bool srcBelow = srcVer < threshold;
        bool tgtBelow = tgtVer < threshold;

        if (srcBelow && tgtBelow)
            versionArg = "4.3.06";
        else if (srcVer != tgtVer)
        {
            TryDeleteFile(tempMergedPath);
            return new CliResult { ExitCode = -1, Error = $"版本不一致（源 {srcInfo.Version}，目标 {tgtInfo.Version}）" };
        }

        // Step 4: Resolve --from/--to skeleton names
        string? fromName = null, toName = null;
        if (!string.IsNullOrWhiteSpace(_txtFromName.Text))
        {
            var name = _txtFromName.Text.Trim();
            if (srcInfo.SkeletonNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                fromName = name;
        }
        if (!string.IsNullOrWhiteSpace(_txtToName.Text))
        {
            var name = _txtToName.Text.Trim();
            if (tgtInfo.SkeletonNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                toName = name;
        }

        // Step 5: Execute --merge
        var mergeResult = await _cli.MergeSkeleton(srcPath, tempMergedPath, fromName, toName, versionArg, ct);
        if (!mergeResult.Success)
        {
            var errMsg = mergeResult.Error;
            if (string.IsNullOrEmpty(errMsg)) errMsg = mergeResult.Output;
            TryDeleteFile(tempMergedPath);
            return new CliResult { ExitCode = -1, Error = $"骨架合并失败：{errMsg}" };
        }

        // Step 6: Execute -a animation import
        var animNames = source.SelectedAnimations.Count > 0
            ? source.SelectedAnimations
            : srcInfo.AnimationNames;

        if (animNames.Count > 0)
        {
            var conflicts = animNames.Intersect(tgtInfo.AnimationNames, StringComparer.OrdinalIgnoreCase).ToList();
            if (conflicts.Count > 0)
            {
                TryDeleteFile(tempMergedPath);
                return new CliResult { ExitCode = -1, Error = $"动画冲突：{string.Join(", ", conflicts)}" };
            }

            var animResult = await _cli.ImportAnimations(srcPath, tempMergedPath, animNames, versionArg, ct);
            if (!animResult.Success)
            {
                var errMsg = animResult.Error;
                if (string.IsNullOrEmpty(errMsg)) errMsg = animResult.Output;
                TryDeleteFile(tempMergedPath);
                return new CliResult { ExitCode = -1, Error = $"动画导入失败：{errMsg}" };
            }
        }

        if (File.Exists(mergedPath)) File.Delete(mergedPath);
        File.Move(tempMergedPath, mergedPath);
        return new CliResult { ExitCode = 0, Output = mergedPath };
    }

    private void ShowProgress(bool visible)
    {
        _progressBar.Visible = visible;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;
        _progressBar.ProgressText = "0%";
        _lblProgressCurrent.Visible = visible;
        _lblProgressCurrent.Text = "";
        Application.DoEvents();
    }

    private void SetProgress(int current, int total, string text)
    {
        if (total <= 0) return;
        current = Math.Clamp(current, 0, total);
        _progressBar.Maximum = total;
        _progressBar.Value = current;
        _progressBar.ProgressText = $"{current}/{total}";
        _lblProgressCurrent.Text = text;
        Application.DoEvents();
    }

    private Control BuildProgressPanel()
    {
        var progressPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 3, 10, 4),
            BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA)
        };
        progressPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        progressPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblProgressCurrent.Dock = DockStyle.Fill;
        _lblProgressCurrent.TextAlign = ContentAlignment.MiddleCenter;
        _lblProgressCurrent.AutoEllipsis = true;
        _lblProgressCurrent.Visible = false;
        _lblProgressCurrent.ForeColor = Color.FromArgb(55, 55, 55);

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Visible = false;

        progressPanel.Controls.Add(_lblProgressCurrent, 0, 0);
        progressPanel.Controls.Add(_progressBar, 0, 1);
        return progressPanel;
    }

    private static void StyleListView(ListView listView)
    {
        listView.BorderStyle = BorderStyle.FixedSingle;
        listView.BackColor = Color.White;
        listView.ForeColor = Color.Black;
    }

    private static void StyleButton(Button button)
    {
        StyleButton(button, Color.FromArgb(0xF2, 0xF2, 0xF2), Color.Black);
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderColor = Color.FromArgb(0x8A, 0x8A, 0x8A);
        button.FlatAppearance.MouseOverBackColor = Lighten(backColor);
        button.FlatAppearance.MouseDownBackColor = Darken(backColor);
    }

    private static Color Lighten(Color color)
    {
        return Color.FromArgb(
            Math.Min(255, color.R + 20),
            Math.Min(255, color.G + 20),
            Math.Min(255, color.B + 20));
    }

    private static Color Darken(Color color)
    {
        return Color.FromArgb(
            Math.Max(0, color.R - 25),
            Math.Max(0, color.G - 25),
            Math.Max(0, color.B - 25));
    }

    private static Version? ParseVersion(string? version)
    {
        if (string.IsNullOrEmpty(version)) return null;
        if (System.Version.TryParse(version, out var v)) return v;
        // Handle 3-part versions like "4.3.06"
        var parts = version.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[0], out var maj)
            && int.TryParse(parts[1], out var min)
            && int.TryParse(parts[2], out var bld))
            return new Version(maj, min, bld);
        return null;
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch { }
    }

    private static void MoveDirectoryContents(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(targetDir, relative));
        }
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(targetDir, relative);
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(file, dest);
        }
    }

    private async Task<CliResult> MergeOne(SpineCliEntry source, SpineCliEntry target, string tempDir, CancellationToken ct)
    {
        var ext = Path.GetExtension(source.FilePath).ToLowerInvariant();
        var targetDir = Path.GetDirectoryName(target.FilePath)!;
        var targetName = Path.GetFileNameWithoutExtension(target.FilePath);
        var outputPath = Path.Combine(targetDir, $"{targetName}_merged.spine");
        var tempOutputPath = Path.Combine(tempDir, $"{targetName}_{Guid.NewGuid():N}_merged.tmp.spine");

        try
        {
            // Step 1: copy target as base for output
            File.Copy(target.FilePath, tempOutputPath, overwrite: true);

            // Step 2: resolve source data (import json/skel → temp .spine if needed)
            string sourceForImport = source.FilePath;
            if (ext is ".json" or ".skel")
            {
                sourceForImport = Path.Combine(tempDir, $"{Guid.NewGuid():N}.spine");
                var importResult = await _cli.ImportToTemp(source.FilePath, sourceForImport, ct);
                if (!importResult.Success) return importResult;
            }

            // Step 3: import source data into the target copy (this merges into the existing project)
            var result = await _cli.ImportMerge(sourceForImport, tempOutputPath, ct);
            if (!result.Success) return result;

            if (File.Exists(outputPath)) File.Delete(outputPath);
            File.Move(tempOutputPath, outputPath);
            return new CliResult { ExitCode = 0, Output = outputPath };
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(tempOutputPath);
            return new CliResult { ExitCode = -1, Error = "操作已取消。" };
        }
        catch (Exception ex)
        {
            TryDeleteFile(tempOutputPath);
            return new CliResult { ExitCode = -1, Error = ex.Message };
        }
    }

    // ────────────────────── Export Tab ──────────────────────

    private void BuildExportTab()
    {
        _tabExport.Text = "批量导出";
        _tabExport.Padding = new Padding(10);
        _tabExport.BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);

        var exportContainer = BuildSectionContainer("导出任务");
        var exportPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3)
        };
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        exportPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Row 0: Source dir
        exportPanel.Controls.Add(new Label { Text = "源目录:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var srcDirPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3) };
        _txtSourceDir.Size = new Size(620, 24);
        _txtSourceDir.BorderStyle = BorderStyle.FixedSingle;
        _txtSourceDir.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        _btnBrowseSource.Text = "浏览";
        _btnBrowseSource.AutoSize = true;
        _btnBrowseSource.MinimumSize = new Size(60, 28);
        _btnBrowseSource.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnBrowseSource);
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
        StyleButton(_btnScan, Color.FromArgb(0x00, 0x78, 0xD7), Color.White);
        _btnScan.Click += ScanSourceDir;
        srcDirPanel.Controls.AddRange([_txtSourceDir, _btnBrowseSource, _btnScan]);
        exportPanel.Controls.Add(srcDirPanel, 1, 0);

        // Row 1: File list
        _lvExportFiles.View = View.Details;
        _lvExportFiles.FullRowSelect = true;
        _lvExportFiles.Dock = DockStyle.Fill;
        StyleListView(_lvExportFiles);
        _lvExportFiles.Columns.Add("文件名", 200);
        _lvExportFiles.Columns.Add("export.json", 100);
        _lvExportFiles.Columns.Add("路径", 400);

        var listPanel = new Panel { Dock = DockStyle.Fill };
        _lvExportFiles.Parent = listPanel;
        _lvExportFiles.Dock = DockStyle.Fill;
        exportPanel.Controls.Add(listPanel, 0, 1);
        exportPanel.SetColumnSpan(listPanel, 2);

        // Export config selection + refresh button
        _btnRefresh.Text = "刷新状态";
        _btnRefresh.AutoSize = true;
        _btnRefresh.MinimumSize = new Size(96, 34);
        _btnRefresh.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnRefresh);
        _btnRefresh.Click += (_, _) => RefreshExportStatus();

        _rbFinish.Text = "finish.export.json";
        _rbFinish.AutoSize = true;
        _rbFinish.Margin = new Padding(10, 5, 4, 0);
        _rbFinish.Checked = true;
        _rbFinish.CheckedChanged += (_, _) => { if (_rbFinish.Checked) { _exportConfigName = "finish.export.json"; RefreshExportStatus(); } };

        _rbDemotion.Text = "demotion.export.json";
        _rbDemotion.AutoSize = true;
        _rbDemotion.Margin = new Padding(10, 5, 4, 0);
        _rbDemotion.CheckedChanged += (_, _) => { if (_rbDemotion.Checked) { _exportConfigName = "demotion.export.json"; RefreshExportStatus(); } };

        _rbOther.Text = "其他";
        _rbOther.AutoSize = true;
        _rbOther.Margin = new Padding(10, 5, 4, 0);
        _rbOther.CheckedChanged += (_, _) =>
        {
            if (!_rbOther.Checked) return;
            var name = Microsoft.VisualBasic.Interaction.InputBox(
                "输入 export.json 文件名：", "指定导出配置", _exportConfigName);
            if (!string.IsNullOrWhiteSpace(name))
            {
                _exportConfigName = name.Trim();
                RefreshExportStatus();
            }
            else
            {
                _rbFinish.Checked = true;
            }
        };

        var refreshPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(0, 8, 0, 4), BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3) };
        refreshPanel.Controls.Add(_btnRefresh);
        refreshPanel.Controls.Add(new Label { Text = "  |  导出配置:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        refreshPanel.Controls.Add(_rbFinish);
        refreshPanel.Controls.Add(_rbDemotion);
        refreshPanel.Controls.Add(_rbOther);
        exportPanel.Controls.Add(refreshPanel, 1, 2);

        // Row 3: Output dir
        exportPanel.Controls.Add(new Label { Text = "输出目录:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        var outDirPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3) };
        _txtOutputDir.Size = new Size(620, 24);
        _txtOutputDir.BorderStyle = BorderStyle.FixedSingle;
        _txtOutputDir.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        _btnBrowseOutput.Text = "浏览";
        _btnBrowseOutput.AutoSize = true;
        _btnBrowseOutput.MinimumSize = new Size(60, 28);
        _btnBrowseOutput.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnBrowseOutput);
        _btnBrowseOutput.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                _txtOutputDir.Text = dlg.SelectedPath;
        };
        outDirPanel.Controls.AddRange([_txtOutputDir, _btnBrowseOutput]);
        exportPanel.Controls.Add(outDirPanel, 1, 3);

        // Row 4: Action buttons
        var actionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 8, 0, 0), BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3) };

        _btnExport.Text = "导出";
        _btnExport.AutoSize = true;
        _btnExport.MinimumSize = new Size(100, 36);
        _btnExport.BackColor = Color.FromArgb(0x00, 0x78, 0xD7);
        _btnExport.ForeColor = Color.White;
        _btnExport.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnExport, Color.FromArgb(0x00, 0x78, 0xD7), Color.White);
        _btnExport.Click += async (_, _) => await ExecuteExport();

        _btnPack.Text = "单纹理图";
        _btnPack.AutoSize = true;
        _btnPack.MinimumSize = new Size(100, 36);
        _btnPack.BackColor = Color.FromArgb(0x6B, 0x46, 0xC3);
        _btnPack.ForeColor = Color.White;
        _btnPack.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnPack, Color.FromArgb(0x6B, 0x46, 0xC3), Color.White);
        _btnPack.Click += async (_, _) => await ExecutePack();

        actionPanel.Controls.AddRange([_btnExport, _btnPack]);
        exportPanel.Controls.Add(actionPanel, 1, 4);

        exportContainer.Controls.Add(exportPanel, 0, 1);
        _tabExport.Controls.Add(exportContainer);
    }

    private void ScanSourceDir(object? sender, EventArgs e)
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
                var configPath = !string.IsNullOrEmpty(dir) ? Path.Combine(dir, _exportConfigName) : null;
                var hasConfig = configPath != null && File.Exists(configPath);
                entry.HasExportConfig = hasConfig;
                entry.ExportConfigPath = hasConfig ? configPath : null;
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
        var ct = BeginCliOperation();
        var current = 0;
        var total = _exportEntries.Count;
        ShowProgress(true);

        try
        {
            var outputDir = _txtOutputDir.Text.Trim();
            Directory.CreateDirectory(outputDir);

            var missingList = new List<string>();
            int ok = 0, fail = 0;

            foreach (var entry in _exportEntries)
            {
                ct.ThrowIfCancellationRequested();
                current++;
                SetProgress(current, total, $"批量导出: {entry.FileName}");
                var tempOutputDir = Path.Combine(Path.GetTempPath(), "KeyMacro", "cli_export", Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(tempOutputDir);
                    CliResult result;
                    if (entry.HasExportConfig && entry.ExportConfigPath != null)
                        result = await _cli.Export(entry.FilePath, tempOutputDir, entry.ExportConfigPath, ct);
                    else
                    {
                        result = await _cli.ExportDefault(entry.FilePath, tempOutputDir, ct: ct);
                        missingList.Add(entry.FileName);
                    }

                    if (result.Success)
                    {
                        MoveDirectoryContents(tempOutputDir, outputDir);
                        ok++;
                    }
                    else { fail++; OperationLogger.Error($"CLI export failed: {entry.FileName}: {result.Error}"); }
                }
                catch (OperationCanceledException)
                {
                    TryDeleteDirectory(tempOutputDir);
                    throw;
                }
                catch (Exception ex)
                {
                    fail++;
                    OperationLogger.Error($"CLI export error: {entry.FileName}: {ex.Message}");
                }
                finally
                {
                    TryDeleteDirectory(tempOutputDir);
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
        catch (OperationCanceledException)
        {
            MessageBox.Show(this, "导出已取消，当前临时文件已清理，已完成结果保留。", "已取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            ShowProgress(false);
            EndCliOperation();
            _btnExport.Enabled = true;
            _btnExport.Text = "导出";
        }
    }

    private async Task ExecutePack()
    {
        if (!ValidateExportReady()) return;

        _btnPack.Enabled = false;
        _btnPack.Text = "打包中...";
        var ct = BeginCliOperation();
        var current = 0;
        var total = _exportEntries.Count;
        ShowProgress(true);

        try
        {
            var outputDir = _txtOutputDir.Text.Trim();
            Directory.CreateDirectory(outputDir);

            int ok = 0, fail = 0;
            foreach (var entry in _exportEntries)
            {
                ct.ThrowIfCancellationRequested();
                current++;
                SetProgress(current, total, $"单纹理图: {entry.FileName}");
                var tempOutputDir = Path.Combine(Path.GetTempPath(), "KeyMacro", "cli_pack", Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(tempOutputDir);
                    var name = Path.GetFileNameWithoutExtension(entry.FileName);
                    var result = await _cli.Pack(entry.FilePath, tempOutputDir, name, ct);
                    if (result.Success)
                    {
                        MoveDirectoryContents(tempOutputDir, outputDir);
                        ok++;
                    }
                    else { fail++; OperationLogger.Error($"CLI pack failed: {entry.FileName}: {result.Error}"); }
                }
                catch (OperationCanceledException)
                {
                    TryDeleteDirectory(tempOutputDir);
                    throw;
                }
                catch (Exception ex)
                {
                    fail++;
                    OperationLogger.Error($"CLI pack error: {entry.FileName}: {ex.Message}");
                }
                finally
                {
                    TryDeleteDirectory(tempOutputDir);
                }
            }

            MessageBox.Show(this, $"纹理打包完成！成功 {ok}，失败 {fail}", "打包结果", MessageBoxButtons.OK,
                fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(this, "打包已取消，当前临时文件已清理，已完成结果保留。", "已取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            ShowProgress(false);
            EndCliOperation();
            _btnPack.Enabled = true;
            _btnPack.Text = "单纹理图";
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
