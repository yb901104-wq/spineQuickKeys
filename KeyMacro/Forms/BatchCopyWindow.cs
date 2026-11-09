using KeyMacro.Controls;
using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

/// <summary>
/// V2.91（AUD-037）：内层双 Tab 合并为单页左右分栏。
/// 左列 = InplaceBackupPanel 的 A/B/C（多文件夹只读扫描 / 后缀+关键词过滤 / 五列勾选 Grid）；
/// 右列 = 独立「原地复制」模式卡 + 目标路径卡（前缀 / 中间 / 路径后缀 / 预览目标，竖向堆叠）；
/// 底部主按钮、进度条、状态栏窗口级共享。
/// 服务层不合并：未勾选走 BatchCopyService，勾选走 InplaceBackupService（INV-1~8 安全不变式不动）。
/// </summary>
public class BatchCopyWindow : Form
{
    // ── Controls: 目标路径 ──
    private readonly DarkComboBox _cmbPrefix = new();
    private readonly Button _btnBrowsePrefix = new();
    private readonly TextBox _txtMiddle = new();
    private readonly Button _btnAddMiddle = new();
    private readonly Button _btnDelMiddle = new();
    private readonly DarkComboBox _cmbSuffix = new();
    private readonly ListBox _lbPreview = new();
    private readonly Button _btnClearHistory = new();
    private readonly Panel _targetCard = new();
    private readonly Label _lblTargetHint = new();

    // ── Controls: 模式开关 / 底部 ──
    private readonly CheckBox _chkInplace = new();
    private readonly Button _btnStart = new();
    private readonly Label _lblStatus = new();
    private readonly Label _lblProgressCurrent = new();
    private readonly TextProgressBar _progressBar = new();
    private readonly ToolTip _statusTip = new();
    private Control? _suffixConfig;

    // ── Panels / State ──
    private readonly InplaceBackupPanel _backupPanel = new();
    private readonly BatchCopyService _copyService = new();
    private PathHistory _history = new();
    private readonly System.Windows.Forms.Timer _previewDebounce = new();
    private bool _taskRunning;
    private bool _closingAfterCancel;

    public BatchCopyWindow()
    {
        Text = "文件批量复制";
        Icon = IconService.AppIcon;
        ClientSize = new Size(1440, 900);
        MinimumSize = new Size(1040, 680);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = true;
        BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);

        BuildUI();
        UiTheme.Apply(this, UiWindowProfile.BatchCopy);
        LoadHistory();
        WireBackupEvents();

        _previewDebounce.Interval = 300;
        _previewDebounce.Tick += (_, _) => { _previewDebounce.Stop(); UpdatePreview(); };

        ApplyMode();
    }

    // ══════════════════════════════════════════════════════════
    //  UI 构建：外框通栏，左右两列弹性填满（816 : 556），底部三栏通栏
    // ══════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // D 区「备份后缀 + 示例」拆到右列模式卡；A/B/C 留在左列，D 区行收起
        _backupPanel.UseExternalAction();
        _suffixConfig = _backupPanel.DetachSuffixConfig();

        var content = Grid(2, 1, new RowStyle(SizeType.Percent, 100));
        content.Padding = new Padding(12);
        content.BackColor = UiTheme.Workspace;
        // 1372 = 816 + 556（另有 10px 列间距由左右 Margin 各 5px 提供）
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 59.48f));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40.52f));

        content.Controls.Add(_backupPanel, 0, 0);
        _backupPanel.Margin = new Padding(0, 0, 5, 0);

        var right = Grid(1, 2,
            new RowStyle(SizeType.Absolute, 132),
            new RowStyle(SizeType.Percent, 100));
        right.BackColor = UiTheme.Workspace;
        Add(right, BuildModeCard(), 0, 0);
        Add(right, BuildTargetCard(), 0, 1);
        content.Controls.Add(right, 1, 0);
        right.Margin = new Padding(5, 0, 0, 0);

        var mainPanel = Grid(1, 4,
            new RowStyle(SizeType.Percent, 100),
            new RowStyle(SizeType.Absolute, 48),
            new RowStyle(SizeType.Absolute, 56),
            new RowStyle(SizeType.Absolute, 24));
        mainPanel.Padding = new Padding(8);

        Add(mainPanel, content, 0, 0);
        Add(mainPanel, BuildActionPanel(), 0, 1);
        Add(mainPanel, BuildProgressPanel(), 0, 2);

        _lblStatus.Text = "就绪";
        _lblStatus.ForeColor = Color.Gray;
        _lblStatus.Dock = DockStyle.Fill;
        _lblStatus.AutoEllipsis = true;
        _lblStatus.Margin = new Padding(0, 4, 0, 0);
        Add(mainPanel, _lblStatus, 0, 3);

        Controls.Add(mainPanel);
    }

    private static TableLayoutPanel Grid(int columns, int rows, params RowStyle[] rowStyles)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = rows,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        foreach (var style in rowStyles) table.RowStyles.Add(style);
        return table;
    }

    private static void Add(TableLayoutPanel table, Control child, int column, int row)
    {
        child.Margin = Padding.Empty;
        table.Controls.Add(child, column, row);
    }

    private static Label Lbl(string text, ContentAlignment align = ContentAlignment.MiddleLeft)
        => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = align,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

    private static Panel NewCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Panel,
            Padding = new Padding(12, 8, 12, 8),
            Margin = Padding.Empty
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(1, card.Width - 1), Math.Max(1, card.Height - 1));
        };
        return card;
    }

    /// <summary>右列上半：独立勾选项 + 从 Tab2 D 区拆来的「备份后缀 + 生成示例」。</summary>
    private Control BuildModeCard()
    {
        var card = NewCard();
        var layout = Grid(1, 4,
            new RowStyle(SizeType.Absolute, 26),
            new RowStyle(SizeType.Absolute, 22),
            new RowStyle(SizeType.Absolute, 58),
            new RowStyle(SizeType.Percent, 100));
        layout.BackColor = UiTheme.Panel;

        _chkInplace.Text = "原地复制（不设置目标路径）";
        _chkInplace.Dock = DockStyle.Fill;
        _chkInplace.Checked = false; // D2：默认目标路径模式，保持 Tab1 使用习惯
        _chkInplace.CheckedChanged += (_, _) => ApplyMode();
        Add(layout, _chkInplace, 0, 0);

        Add(layout, Lbl("勾选后在各自原目录生成带后缀副本，右侧目标路径整卡不可用"), 0, 1);

        _suffixConfig!.Dock = DockStyle.Fill;
        Add(layout, _suffixConfig, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    /// <summary>右列下半：目标路径卡，整体可置灰（D9）。</summary>
    private Control BuildTargetCard()
    {
        _targetCard.Dock = DockStyle.Fill;
        _targetCard.BackColor = UiTheme.Panel;
        _targetCard.Padding = new Padding(12, 8, 12, 8);
        _targetCard.Margin = Padding.Empty;
        _targetCard.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(1, _targetCard.Width - 1), Math.Max(1, _targetCard.Height - 1));
        };

        var layout = Grid(1, 3,
            new RowStyle(SizeType.Absolute, 26),
            new RowStyle(SizeType.Absolute, 250),
            new RowStyle(SizeType.Percent, 100));
        layout.BackColor = UiTheme.Panel;

        var head = Grid(2, 1, new RowStyle(SizeType.Percent, 100));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var title = Lbl("目标路径");
        title.Font = new Font("微软雅黑", 10, FontStyle.Bold);
        Add(head, title, 0, 0);
        _lblTargetHint.Dock = DockStyle.Fill;
        _lblTargetHint.TextAlign = ContentAlignment.MiddleRight;
        _lblTargetHint.AutoEllipsis = true;
        _lblTargetHint.ForeColor = UiTheme.Muted;
        Add(head, _lblTargetHint, 1, 0);
        Add(layout, head, 0, 0);

        Add(layout, BuildTargetInputs(), 0, 1);
        Add(layout, BuildPreviewPanel(), 0, 2);

        _targetCard.Controls.Add(layout);
        return _targetCard;
    }

    /// <summary>三段式输入：由原 Tab1 的 60/40 横向分改为竖向堆叠在右列上半。</summary>
    private Control BuildTargetInputs()
    {
        var input = Grid(3, 3,
            new RowStyle(SizeType.Absolute, 34),
            new RowStyle(SizeType.Percent, 100),
            new RowStyle(SizeType.Absolute, 34));
        input.Padding = new Padding(0, 4, 0, 4);
        input.BackColor = UiTheme.Panel;
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

        Add(input, Lbl("前缀"), 0, 0);
        _cmbPrefix.DropDownStyle = ComboBoxStyle.DropDown;
        _cmbPrefix.Font = new Font("Microsoft YaHei", 9);
        _cmbPrefix.Dock = DockStyle.Fill;
        _cmbPrefix.Margin = new Padding(0, 3, 10, 3);
        _cmbPrefix.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        _cmbPrefix.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbPrefix.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cmbPrefix.TextUpdate += (_, _) => DebouncePreview();
        _cmbPrefix.SelectedIndexChanged += (_, _) => DebouncePreview();
        Add(input, _cmbPrefix, 1, 0);

        _btnBrowsePrefix.Text = "选择文件夹";
        _btnBrowsePrefix.Dock = DockStyle.Fill;
        _btnBrowsePrefix.AutoSize = false;
        _btnBrowsePrefix.MinimumSize = new Size(82, 28);
        _btnBrowsePrefix.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnBrowsePrefix, Color.FromArgb(0xF2, 0xF2, 0xF2), Color.Black);
        _btnBrowsePrefix.Click += BtnBrowsePrefix_Click;
        Add(input, _btnBrowsePrefix, 2, 0);

        Add(input, Lbl("中间", ContentAlignment.TopLeft), 0, 1);
        _txtMiddle.Multiline = true;
        _txtMiddle.Dock = DockStyle.Fill;
        _txtMiddle.Font = new Font("Consolas", 10);
        _txtMiddle.ScrollBars = ScrollBars.Vertical;
        _txtMiddle.AcceptsReturn = true;
        _txtMiddle.WordWrap = false;
        _txtMiddle.BorderStyle = BorderStyle.FixedSingle;
        _txtMiddle.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        _txtMiddle.Margin = new Padding(0, 4, 10, 4);
        _txtMiddle.TextChanged += (_, _) => DebouncePreview();
        Add(input, _txtMiddle, 1, 1);

        var middleBtnCol = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoSize = false,
            Padding = new Padding(0, 4, 0, 0),
            WrapContents = false,
            Margin = Padding.Empty
        };
        _btnAddMiddle.Text = "添加行";
        _btnAddMiddle.AutoSize = false;
        _btnAddMiddle.Size = new Size(82, 30);
        _btnAddMiddle.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnAddMiddle);
        _btnAddMiddle.Click += BtnAddMiddle_Click;
        middleBtnCol.Controls.Add(_btnAddMiddle);

        _btnDelMiddle.Text = "删除行";
        _btnDelMiddle.AutoSize = false;
        _btnDelMiddle.Size = new Size(82, 30);
        _btnDelMiddle.Margin = new Padding(3, 8, 3, 3);
        _btnDelMiddle.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnDelMiddle);
        _btnDelMiddle.Click += BtnDelMiddle_Click;
        middleBtnCol.Controls.Add(_btnDelMiddle);
        Add(input, middleBtnCol, 2, 1);

        // D4：与「备份后缀」区分，明确这是目标目录末段
        Add(input, Lbl("路径后缀"), 0, 2);
        _cmbSuffix.DropDownStyle = ComboBoxStyle.DropDown;
        _cmbSuffix.Font = new Font("Microsoft YaHei", 9);
        _cmbSuffix.Dock = DockStyle.Fill;
        _cmbSuffix.Margin = new Padding(0, 3, 10, 3);
        _cmbSuffix.BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6);
        _cmbSuffix.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbSuffix.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cmbSuffix.TextUpdate += (_, _) => DebouncePreview();
        _cmbSuffix.SelectedIndexChanged += (_, _) => DebouncePreview();
        Add(input, _cmbSuffix, 1, 2);

        _btnClearHistory.Text = "清理历史";
        _btnClearHistory.AutoSize = false;
        _btnClearHistory.Size = new Size(82, 30);
        _btnClearHistory.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnClearHistory);
        _btnClearHistory.Click += (_, _) =>
        {
            var result = MessageBox.Show(this, "确定要清理前缀和路径后缀的历史记录吗？",
                "清理历史记录", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                ConfigService.ClearPathHistory();
                _history = new PathHistory();
                _cmbPrefix.Items.Clear();
                _cmbSuffix.Items.Clear();
            }
        };
        Add(input, _btnClearHistory, 2, 2);

        return input;
    }

    private Control BuildPreviewPanel()
    {
        var panel = Grid(1, 2,
            new RowStyle(SizeType.Absolute, 28),
            new RowStyle(SizeType.Percent, 100));
        panel.BackColor = UiTheme.Panel;
        var title = Lbl("预览目标");
        title.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        Add(panel, title, 0, 0);

        _lbPreview.Dock = DockStyle.Fill;
        _lbPreview.Font = new Font("Consolas", 10);
        _lbPreview.IntegralHeight = false;
        _lbPreview.SelectionMode = SelectionMode.None;
        _lbPreview.BorderStyle = BorderStyle.FixedSingle;
        _lbPreview.BackColor = Color.White;
        Add(panel, _lbPreview, 0, 1);
        return panel;
    }

    private Control BuildActionPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        _btnStart.Text = "开始复制";
        _btnStart.Dock = DockStyle.Fill;
        _btnStart.Font = new Font("微软雅黑", 12, FontStyle.Bold);
        _btnStart.FlatStyle = FlatStyle.Flat;
        _btnStart.BackColor = Color.FromArgb(0x00, 0xC8, 0x53);
        _btnStart.ForeColor = Color.White;
        StyleButton(_btnStart, Color.FromArgb(0x00, 0xC8, 0x53), Color.White);
        _btnStart.Click += BtnStart_Click;
        panel.Controls.Add(_btnStart);
        return panel;
    }

    private Control BuildProgressPanel()
    {
        var panel = Grid(1, 2,
            new RowStyle(SizeType.Absolute, 20),
            new RowStyle(SizeType.Percent, 100));
        panel.Padding = new Padding(0, 2, 0, 2);

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

        Add(panel, _lblProgressCurrent, 0, 0);
        Add(panel, _progressBar, 0, 1);
        return panel;
    }

    // ══════════════════════════════════════════════════════════
    //  模式开关：只做 UI 路由，服务层不合并
    // ══════════════════════════════════════════════════════════

    private void WireBackupEvents()
    {
        _backupPanel.ProgressRequested += (text, current, total) => SetProgress(current, total, text);
        _backupPanel.StatusRequested += (text, tip) => ApplyStatus(text, tip);
        _backupPanel.BusyChanged += _ =>
        {
            UpdateTaskLock();
            ApplyActionButtonState();
        };
        // UiTheme.Apply 会重设 Label 前景色，套用主题后恢复示例文字的青/红状态
        _backupPanel.RefreshHostVisualState();
        UpdateTaskLock();
    }

    private void ApplyMode()
    {
        if (IsDisposed) return;
        var inplace = _chkInplace.Checked;
        // 不清空源/扫描/勾选/过滤/目标路径输入，不触发磁盘 I/O
        _backupPanel.SetHostInplaceMode(inplace);
        _lblTargetHint.Text = inplace ? "原地复制模式下不可用 · 无需目标路径" : "";
        UpdateTaskLock();
        _backupPanel.RefreshHostVisualState();
        ApplyActionButtonState();
    }

    private void ApplyStatus(string text, string tip)
    {
        if (IsDisposed) return;
        _lblStatus.Text = text;
        _statusTip.SetToolTip(_lblStatus, tip);
    }

    /// <summary>
    /// 单任务互斥：任一引擎运行时锁定模式开关与两侧输入；主按钮始终可用，作为取消入口。
    /// 替代原「禁止切 Tab」。
    /// </summary>
    private void UpdateTaskLock()
    {
        if (IsDisposed) return;
        _taskRunning = _copyService.IsRunning || _backupPanel.IsBusy;
        var inplace = _chkInplace.Checked;

        _chkInplace.Enabled = !_taskRunning;
        _btnStart.Enabled = true;
        _targetCard.Enabled = !inplace && !_taskRunning;
        if (_suffixConfig != null)
            _suffixConfig.Enabled = inplace && !_taskRunning;
        _backupPanel.SetSuffixInputEnabled(inplace && !_taskRunning);
        _backupPanel.SetExternalBusy(_copyService.IsRunning);
    }

    private void ApplyActionButtonState()
    {
        if (IsDisposed) return;
        var inplace = _chkInplace.Checked;
        if (_taskRunning)
        {
            _btnStart.Text = inplace ? "取消备份" : "取消复制";
            _btnStart.BackColor = Color.FromArgb(0xD9, 0x5C, 0x5C);
        }
        else
        {
            _btnStart.Text = inplace ? "开始批量备份" : "开始复制";
            _btnStart.BackColor = Color.FromArgb(0x00, 0xC8, 0x53);
        }
        _btnStart.ForeColor = Color.White;
    }

    // ══════════════════════════════════════════════════════════
    //  执行：按模式分派，执行集合统一为「勾选快照」
    // ══════════════════════════════════════════════════════════

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_taskRunning)
        {
            if (_copyService.IsRunning) _copyService.Cancel();
            else _backupPanel.Cancel();
            return;
        }

        // D7：统一以点按钮瞬间的勾选快照为准（含被过滤隐藏项）
        var files = _backupPanel.CheckedFilesSnapshot;
        if (files.Count == 0)
        {
            MessageBox.Show(this, "请先勾选要处理的文件。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_chkInplace.Checked)
            await RunInplaceAsync();
        else
            await RunTargetCopyAsync(files);
    }

    private async Task RunInplaceAsync()
    {
        // 校验、快照、命名、INV-1~8 全部在 InplaceBackupPanel/InplaceBackupService 内部完成
        try
        {
            await _backupPanel.StartBackupFromHostAsync();
        }
        finally
        {
            UpdateTaskLock();
            ApplyActionButtonState();
        }
    }

    private async Task RunTargetCopyAsync(IReadOnlyList<string> files)
    {
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
            }));
            _copyService.Completed -= onCompleted;
            _copyService.ProgressChanged -= onProgress;
            _copyService.ProgressReported -= onProgressReported;
        };
        _copyService.Completed += onCompleted;

        _lblStatus.Text = "复制中...";
        SetProgress(0, files.Count * targetPaths.Count, "准备复制...");

        var copyTask = _copyService.CopyFilesAsync(files.ToList(), targetPaths, OnConflictAsync);
        UpdateTaskLock();
        ApplyActionButtonState();
        try
        {
            await copyTask;
        }
        finally
        {
            // 服务异常路径不触发 Completed，必须在 await 返回后统一解锁
            UpdateTaskLock();
            ApplyActionButtonState();
        }
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

    // ══════════════════════════════════════════════════════════
    //  目标路径：三段式拼接 / 预览 / 历史（逻辑与改造前一致）
    // ══════════════════════════════════════════════════════════

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

    // ══════════════════════════════════════════════════════════
    //  关闭保护 / 历史写回（嵌入态依赖 OnFormClosed，不可动）
    // ══════════════════════════════════════════════════════════

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_taskRunning && !_closingAfterCancel)
        {
            // 关闭保护：先取消当前任务并等待结束，再真正关闭；所有事件回调先判 IsDisposed
            e.Cancel = true;
            _copyService.Cancel();
            _backupPanel.Cancel();
            OperationLogger.Info("BatchCopyWindow: close while task running, cancelling first");
            _ = WaitForTasksAndCloseAsync();
        }
        base.OnFormClosing(e);
    }

    private async Task WaitForTasksAndCloseAsync()
    {
        var start = Environment.TickCount64;
        while (_taskRunning && Environment.TickCount64 - start < 30000)
            await Task.Delay(100);
        if (_taskRunning)
            OperationLogger.Warn("BatchCopyWindow: wait for task cancellation timed out (30s)");
        _closingAfterCancel = true;
        if (!IsDisposed)
        {
            try { BeginInvoke(new Action(Close)); }
            catch (InvalidOperationException) { }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _copyService.Cancel();
        _backupPanel.Cancel();

        // 路径前缀/后缀历史
        if (!string.IsNullOrEmpty(_cmbPrefix.Text))
            SavePrefixHistory(_cmbPrefix.Text.Trim());
        if (!string.IsNullOrEmpty(_cmbSuffix.Text))
            SaveSuffixHistory(_cmbSuffix.Text.Trim());

        // 路径历史写回之后再写备份后缀历史（内部会从磁盘重载最新历史，避免互相覆盖）
        try { InplaceBackupPanel.PersistSuffixHistory(_backupPanel.CurrentSuffix); }
        catch (Exception ex) { OperationLogger.Warn($"BatchCopyWindow: persist backup suffix failed: {ex.Message}"); }
    }

    // ══════════════════════════════════════════════════════════
    //  按钮样式（沿用原实现）
    // ══════════════════════════════════════════════════════════

    private static void StyleButton(Button button)
    {
        StyleButton(button, button.BackColor == default ? Color.FromArgb(0xF2, 0xF2, 0xF2) : button.BackColor, button.ForeColor);
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
}
