using KeyMacro.Controls;
using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public class BatchCopyWindow : Form
{
    // ── Controls: Source ──
    private readonly ListBox _lbSourceFiles = new();
    private readonly Button _btnSelectFiles = new();
    private readonly Button _btnRemoveSelected = new();
    private readonly Button _btnClearFiles = new();
    private readonly Label _lblSourceCount = new();
    private readonly List<string> _sourceFiles = [];

    // ── Controls: Target ──
    private readonly ComboBox _cmbPrefix = new();
    private readonly Button _btnBrowsePrefix = new();
    private readonly TextBox _txtMiddle = new();
    private readonly Button _btnAddMiddle = new();
    private readonly Button _btnDelMiddle = new();
    private readonly ComboBox _cmbSuffix = new();
    private readonly ListBox _lbPreview = new();

    // ── Controls: Action ──
    private readonly Button _btnClearHistory = new();
    private readonly Button _btnStartCopy = new();
    private readonly Label _lblStatus = new();
    private readonly Label _lblProgressCurrent = new();
    private readonly TextProgressBar _progressBar = new();

    // ── State ──
    private readonly BatchCopyService _copyService = new();
    private PathHistory _history = new();
    private readonly System.Windows.Forms.Timer _previewDebounce = new();

    public BatchCopyWindow()
    {
        Text = "文件批量复制";
        Icon = IconService.AppIcon;
        Size = new Size(1100, 760);
        MinimumSize = new Size(820, 620);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = true;

        BuildUI();
        UiTheme.Apply(this, UiWindowProfile.BatchCopy);
        LoadHistory();

        _previewDebounce.Interval = 300;
        _previewDebounce.Tick += (_, _) => { _previewDebounce.Stop(); UpdatePreview(); };
    }

    private void BuildUI()
    {
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            ColumnCount = 1,
            RowCount = 7
        };

        mainPanel.Controls.Add(BuildSourcePanel(), 0, 0);
        mainPanel.Controls.Add(new Label
        {
            Text = "目标路径",
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Height = 24,
            Margin = new Padding(0, 4, 0, 0)
        }, 0, 1);
        mainPanel.Controls.Add(BuildTargetPanel(), 0, 2);
        mainPanel.Controls.Add(BuildHistoryRow(), 0, 3);
        mainPanel.Controls.Add(BuildActionPanel(), 0, 4);
        mainPanel.Controls.Add(BuildProgressPanel(), 0, 5);

        _lblStatus.Text = "就绪";
        _lblStatus.ForeColor = Color.Gray;
        _lblStatus.Height = 20;
        _lblStatus.Margin = new Padding(0, 4, 0, 0);
        mainPanel.Controls.Add(_lblStatus, 0, 6);

        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 236));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        Controls.Add(mainPanel);
    }

    // ══════════════════════════════════════════════
    //  Source File Panel
    // ══════════════════════════════════════════════

    private Panel BuildSourcePanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

        // Toolbar
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, Padding = new Padding(0, 2, 0, 4) };
        _btnSelectFiles.Text = "选择文件";
        _btnSelectFiles.AutoSize = true;
        _btnSelectFiles.MinimumSize = new Size(90, 28);
        _btnSelectFiles.FlatStyle = FlatStyle.Flat;
        _btnSelectFiles.BackColor = Color.FromArgb(0x00, 0x78, 0xD7);
        _btnSelectFiles.ForeColor = Color.White;
        _btnSelectFiles.Click += BtnSelectFiles_Click;
        toolbar.Controls.Add(_btnSelectFiles);

        _btnRemoveSelected.Text = "移除选中";
        _btnRemoveSelected.AutoSize = true;
        _btnRemoveSelected.MinimumSize = new Size(80, 28);
        _btnRemoveSelected.FlatStyle = FlatStyle.Flat;
        _btnRemoveSelected.Click += (_, _) =>
        {
            var indices = _lbSourceFiles.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
            foreach (var i in indices)
            {
                if (i >= 0 && i < _sourceFiles.Count)
                    _sourceFiles.RemoveAt(i);
            }
            RefreshSourceList();
        };
        toolbar.Controls.Add(_btnRemoveSelected);

        _btnClearFiles.Text = "清空列表";
        _btnClearFiles.AutoSize = true;
        _btnClearFiles.MinimumSize = new Size(80, 28);
        _btnClearFiles.FlatStyle = FlatStyle.Flat;
        _btnClearFiles.Click += (_, _) => { _sourceFiles.Clear(); RefreshSourceList(); };
        toolbar.Controls.Add(_btnClearFiles);

        panel.Controls.Add(toolbar, 0, 0);

        // File list
        _lbSourceFiles.Dock = DockStyle.Fill;
        _lbSourceFiles.Font = new Font("Consolas", 10);
        _lbSourceFiles.IntegralHeight = false;
        _lbSourceFiles.SelectionMode = SelectionMode.MultiExtended;
        panel.Controls.Add(_lbSourceFiles, 0, 1);

        // Count label
        _lblSourceCount.Dock = DockStyle.Bottom;
        _lblSourceCount.Text = "已选 0 个文件";
        _lblSourceCount.Height = 20;
        _lblSourceCount.ForeColor = Color.Gray;
        panel.Controls.Add(_lblSourceCount, 0, 2);

        return panel;
    }

    private Control BuildProgressPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 2, 0, 2)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _lblProgressCurrent.Dock = DockStyle.Fill;
        _lblProgressCurrent.AutoEllipsis = true;
        _lblProgressCurrent.TextAlign = ContentAlignment.MiddleCenter;
        _lblProgressCurrent.ForeColor = Color.FromArgb(55, 55, 55);
        _lblProgressCurrent.Text = "";

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;
        _progressBar.ProgressText = "0%";

        panel.Controls.Add(_lblProgressCurrent, 0, 0);
        panel.Controls.Add(_progressBar, 0, 1);
        return panel;
    }

    private void BtnSelectFiles_Click(object? sender, EventArgs e)
    {
        using var picker = new SourceFilePicker();
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            foreach (var f in picker.SelectedFiles)
            {
                if (!_sourceFiles.Contains(f))
                    _sourceFiles.Add(f);
            }
            RefreshSourceList();
        }
    }

    private void RefreshSourceList()
    {
        _lbSourceFiles.Items.Clear();
        foreach (var f in _sourceFiles)
            _lbSourceFiles.Items.Add(f);
        _lblSourceCount.Text = _sourceFiles.Count > 0
            ? $"已选 {_sourceFiles.Count} 个文件"
            : "已选 0 个文件";
    }

    // ══════════════════════════════════════════════
    //  Target Path Builder Panel
    // ══════════════════════════════════════════════

    private Panel BuildTargetPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 5,
            Padding = new Padding(0)
        };

        // Row 0: Prefix
        layout.Controls.Add(new Label { Text = "前缀", TextAlign = ContentAlignment.MiddleLeft, Height = 28 }, 0, 0);
        _cmbPrefix.DropDownStyle = ComboBoxStyle.DropDown;
        _cmbPrefix.Font = new Font("微软雅黑", 9);
        _cmbPrefix.Dock = DockStyle.Fill;
        _cmbPrefix.Margin = new Padding(0, 3, 0, 3);
        _cmbPrefix.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbPrefix.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cmbPrefix.TextUpdate += (_, _) => DebouncePreview();
        _cmbPrefix.SelectedIndexChanged += (_, _) => DebouncePreview();
        layout.Controls.Add(_cmbPrefix, 1, 0);

        _btnBrowsePrefix.Text = "•••";
        _btnBrowsePrefix.Dock = DockStyle.Fill;
        _btnBrowsePrefix.AutoSize = false;
        _btnBrowsePrefix.MinimumSize = new Size(36, 28);
        _btnBrowsePrefix.FlatStyle = FlatStyle.Flat;
        _btnBrowsePrefix.Click += BtnBrowsePrefix_Click;
        layout.Controls.Add(_btnBrowsePrefix, 2, 0);

        // Row 1: Middle label
        layout.Controls.Add(new Label { Text = "中间", TextAlign = ContentAlignment.MiddleLeft, Height = 28 }, 0, 1);

        _txtMiddle.Multiline = true;
        _txtMiddle.Dock = DockStyle.Fill;
        _txtMiddle.Font = new Font("Consolas", 10);
        _txtMiddle.ScrollBars = ScrollBars.Vertical;
        _txtMiddle.AcceptsReturn = true;
        _txtMiddle.WordWrap = false;
        _txtMiddle.TextChanged += (_, _) => DebouncePreview();

        var middleBtnCol = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = false,
            Padding = new Padding(4, 0, 0, 0),
            WrapContents = false
        };
        _btnAddMiddle.Text = "添加行";
        _btnAddMiddle.AutoSize = true;
        _btnAddMiddle.MinimumSize = new Size(60, 26);
        _btnAddMiddle.FlatStyle = FlatStyle.Flat;
        _btnAddMiddle.Click += BtnAddMiddle_Click;
        middleBtnCol.Controls.Add(_btnAddMiddle);

        _btnDelMiddle.Text = "删除行";
        _btnDelMiddle.AutoSize = true;
        _btnDelMiddle.MinimumSize = new Size(60, 26);
        _btnDelMiddle.FlatStyle = FlatStyle.Flat;
        _btnDelMiddle.Click += BtnDelMiddle_Click;
        middleBtnCol.Controls.Add(_btnDelMiddle);

        layout.Controls.Add(_txtMiddle, 1, 1);
        layout.Controls.Add(middleBtnCol, 2, 1);

        // Row 2: Suffix
        layout.Controls.Add(new Label { Text = "后缀", TextAlign = ContentAlignment.MiddleLeft, Height = 28 }, 0, 2);
        _cmbSuffix.DropDownStyle = ComboBoxStyle.DropDown;
        _cmbSuffix.Font = new Font("微软雅黑", 9);
        _cmbSuffix.Dock = DockStyle.Fill;
        _cmbSuffix.Margin = new Padding(0, 3, 0, 3);
        _cmbSuffix.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbSuffix.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cmbSuffix.TextUpdate += (_, _) => DebouncePreview();
        _cmbSuffix.SelectedIndexChanged += (_, _) => DebouncePreview();
        layout.Controls.Add(_cmbSuffix, 1, 2);

        // Row 3: Preview label
        layout.Controls.Add(new Label { Text = "预览", TextAlign = ContentAlignment.MiddleLeft, Height = 22 }, 0, 3);

        // Row 4: Preview list
        _lbPreview.Dock = DockStyle.Fill;
        _lbPreview.Font = new Font("Consolas", 10);
        _lbPreview.IntegralHeight = false;
        _lbPreview.SelectionMode = SelectionMode.None;
        layout.Controls.Add(_lbPreview, 1, 4);

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));

        panel.Controls.Add(layout);
        return panel;
    }

    private void BtnBrowsePrefix_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (!string.IsNullOrEmpty(_cmbPrefix.Text))
            dialog.SelectedPath = _cmbPrefix.Text;

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _cmbPrefix.Text = dialog.SelectedPath;
            SavePrefixHistory(dialog.SelectedPath);

            try
            {
                var subfolders = Directory.GetDirectories(dialog.SelectedPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith('.'))
                    .Select(name => name!)
                    .ToList();

                if (subfolders.Count > 1)
                {
                    var result = MessageBox.Show(this,
                        $"检测到 {subfolders.Count} 个子文件夹，是否导入到中间层列表？",
                        "导入子文件夹", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        using var selectDialog = new SubfolderSelectDialog(subfolders);
                        if (selectDialog.ShowDialog(this) == DialogResult.OK)
                        {
                            var existing = _txtMiddle.Text.Trim();
                            var lines = selectDialog.SelectedFolders;
                            if (!string.IsNullOrEmpty(existing))
                                _txtMiddle.Text = existing + Environment.NewLine + string.Join(Environment.NewLine, lines);
                            else
                                _txtMiddle.Text = string.Join(Environment.NewLine, lines);
                            DebouncePreview();
                        }
                    }
                }
            }
            catch { }
        }
    }

    private void BtnAddMiddle_Click(object? sender, EventArgs e)
    {
        using var dialog = new InputDialog("添加中间路径", "输入中间路径项（可用 / 分隔多层级）:");
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.Result))
        {
            var text = _txtMiddle.Text.Trim();
            if (!string.IsNullOrEmpty(text))
                _txtMiddle.Text = text + Environment.NewLine + dialog.Result.Trim();
            else
                _txtMiddle.Text = dialog.Result.Trim();
        }
    }

    private void BtnDelMiddle_Click(object? sender, EventArgs e)
    {
        var selStart = _txtMiddle.SelectionStart;
        var lines = _txtMiddle.Lines.ToList();
        if (lines.Count == 0) return;

        // Determine which line(s) to remove based on cursor position
        var lineIdx = _txtMiddle.GetLineFromCharIndex(selStart);
        if (lineIdx >= 0 && lineIdx < lines.Count)
        {
            lines.RemoveAt(lineIdx);
            _txtMiddle.Lines = [.. lines];
            DebouncePreview();
        }
    }

    private void DebouncePreview()
    {
        _previewDebounce.Stop();
        _previewDebounce.Start();
    }

    private void UpdatePreview()
    {
        _lbPreview.Items.Clear();
        foreach (var path in GetTargetPaths())
            _lbPreview.Items.Add(path);
    }

    private List<string> GetTargetPaths()
    {
        var prefix = _cmbPrefix.Text.Trim();
        var suffix = _cmbSuffix.Text.Trim();
        var middle = _txtMiddle.Lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (middle.Count == 0 && string.IsNullOrEmpty(suffix))
            return !string.IsNullOrEmpty(prefix) ? [prefix] : [];

        var middleItems = middle.Count > 0 ? middle : [""];
        return middleItems.Select(m =>
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(prefix))
                parts.Add(prefix.TrimEnd('/', '\\'));
            parts.AddRange(SplitRelativePath(m));
            parts.AddRange(SplitRelativePath(suffix));
            return Path.Combine([.. parts]);
        }).ToList();
    }

    private static IEnumerable<string> SplitRelativePath(string value)
    {
        return value.Trim()
            .Trim('/', '\\')
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0 && !Path.IsPathRooted(part));
    }

    // ══════════════════════════════════════════════
    //  History Management
    // ══════════════════════════════════════════════

    private Panel BuildHistoryRow()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 0)
        };

        _btnClearHistory.Text = "清理历史记录";
        _btnClearHistory.AutoSize = true;
        _btnClearHistory.MinimumSize = new Size(110, 28);
        _btnClearHistory.FlatStyle = FlatStyle.Flat;
        _btnClearHistory.Click += (_, _) =>
        {
            var result = MessageBox.Show(this, "确定要清理前缀和后缀的历史记录吗？",
                "清理历史记录", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                ConfigService.ClearPathHistory();
                _history = new PathHistory();
                _cmbPrefix.Items.Clear();
                _cmbSuffix.Items.Clear();
            }
        };
        panel.Controls.Add(_btnClearHistory);

        return panel;
    }

    private void LoadHistory()
    {
        _history = ConfigService.LoadPathHistory();
        _cmbPrefix.Items.Clear();
        foreach (var item in _history.PrefixHistory)
            _cmbPrefix.Items.Add(item);
        _cmbSuffix.Items.Clear();
        foreach (var item in _history.SuffixHistory)
            _cmbSuffix.Items.Add(item);
    }

    private void SavePrefixHistory(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        _history.PrefixHistory.RemoveAll(p => p == path);
        _history.PrefixHistory.Insert(0, path);
        if (_history.PrefixHistory.Count > 50)
            _history.PrefixHistory.RemoveRange(50, _history.PrefixHistory.Count - 50);
        ConfigService.SavePathHistory(_history);
        // Refresh dropdown without losing current text
        var current = _cmbPrefix.Text;
        _cmbPrefix.Items.Clear();
        foreach (var item in _history.PrefixHistory)
            _cmbPrefix.Items.Add(item);
        _cmbPrefix.Text = current;
    }

    private void SaveSuffixHistory(string suffix)
    {
        if (string.IsNullOrEmpty(suffix)) return;
        _history.SuffixHistory.RemoveAll(s => s == suffix);
        _history.SuffixHistory.Insert(0, suffix);
        if (_history.SuffixHistory.Count > 50)
            _history.SuffixHistory.RemoveRange(50, _history.SuffixHistory.Count - 50);
        ConfigService.SavePathHistory(_history);
        var current = _cmbSuffix.Text;
        _cmbSuffix.Items.Clear();
        foreach (var item in _history.SuffixHistory)
            _cmbSuffix.Items.Add(item);
        _cmbSuffix.Text = current;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        // Save current combo text to history on close
        if (!string.IsNullOrEmpty(_cmbPrefix.Text))
            SavePrefixHistory(_cmbPrefix.Text.Trim());
        if (!string.IsNullOrEmpty(_cmbSuffix.Text))
            SaveSuffixHistory(_cmbSuffix.Text.Trim());
    }

    // ══════════════════════════════════════════════
    //  Action Panel
    // ══════════════════════════════════════════════

    private Panel BuildActionPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        _btnStartCopy.Text = "开始复制";
        _btnStartCopy.Dock = DockStyle.Fill;
        _btnStartCopy.Font = new Font("微软雅黑", 12, FontStyle.Bold);
        _btnStartCopy.FlatStyle = FlatStyle.Flat;
        _btnStartCopy.BackColor = Color.FromArgb(0x00, 0xC8, 0x53);
        _btnStartCopy.ForeColor = Color.White;
        _btnStartCopy.Click += BtnStartCopy_Click;
        panel.Controls.Add(_btnStartCopy);
        return panel;
    }

    private async void BtnStartCopy_Click(object? sender, EventArgs e)
    {
        if (_copyService.IsRunning)
        {
            _copyService.Cancel();
            return;
        }

        if (_sourceFiles.Count == 0)
        {
            MessageBox.Show(this, "请先选择要复制的文件。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var targetPaths = GetTargetPaths();
        if (targetPaths.Count == 0)
        {
            MessageBox.Show(this, "请配置目标路径（至少一个有效目录）。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Save current inputs to history
        SavePrefixHistory(_cmbPrefix.Text.Trim());
        SaveSuffixHistory(_cmbSuffix.Text.Trim());

        Action<string, int, int>? onProgressReported = null;
        onProgressReported = (msg, current, total) =>
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() => SetProgress(current, total, msg)));
        };
        _copyService.ProgressReported += onProgressReported;

        Action<string>? onProgress = null;
        onProgress = msg =>
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() => _lblStatus.Text = msg));
        };
        _copyService.ProgressChanged += onProgress;

        Action<string>? onCompleted = null;
        onCompleted = msg =>
        {
            if (IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                _lblStatus.Text = msg;
                _lblProgressCurrent.Text = msg;
                _btnStartCopy.Text = "开始复制";
                _btnStartCopy.BackColor = Color.FromArgb(0x00, 0xC8, 0x53);
            }));
            _copyService.Completed -= onCompleted;
            _copyService.ProgressChanged -= onProgress;
            _copyService.ProgressReported -= onProgressReported;
        };
        _copyService.Completed += onCompleted;

        _btnStartCopy.Text = "取消复制";
        _btnStartCopy.BackColor = Color.FromArgb(0xD9, 0x5C, 0x5C);
        _lblStatus.Text = "复制中...";
        _lblStatus.ForeColor = Color.DarkBlue;
        SetProgress(0, _sourceFiles.Count * targetPaths.Count, "准备复制...");

        await _copyService.CopyFilesAsync(_sourceFiles, targetPaths, OnConflictAsync);
    }

    private void SetProgress(int current, int total, string text)
    {
        total = Math.Max(1, total);
        current = Math.Clamp(current, 0, total);
        _progressBar.Maximum = total;
        _progressBar.Value = current;
        _progressBar.ProgressText = $"{current}/{total}";
        _lblProgressCurrent.Text = text;
    }

    private Task<ConflictAction> OnConflictAsync(string targetDir, List<string> files, CancellationToken token)
    {
        if (IsDisposed) return Task.FromResult(ConflictAction.Overwrite);
        var tcs = new TaskCompletionSource<ConflictAction>();
        BeginInvoke(new Action(() =>
        {
            using var dialog = new ConflictDialog(targetDir, files);
            dialog.ShowDialog(this);
            if (dialog.Result == ConflictAction.CancelAll)
                _copyService.Cancel();
            tcs.TrySetResult(dialog.Result);
        }));
        return tcs.Task;
    }
}
