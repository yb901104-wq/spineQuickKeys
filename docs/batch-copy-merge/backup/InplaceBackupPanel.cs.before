#nullable enable
using KeyMacro.Controls;
using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

/// <summary>
/// Tab2「原地复制备份」页面。
///
/// 安全约束：本面板所有「移除 / 清空 / 过滤 / 勾选」都只操作内存列表，
/// 零磁盘 I/O；唯一磁盘写入发生在 InplaceBackupService.BackupAsync 内部。
/// 列表采用 DataGridView 复选框列（design.md 9.8 备选 / review.md R-17 授权路径）：
/// UiTheme 对 DataGridView 复选框列有完整的深灰自绘，避免 OwnerDraw ListView
/// 未处理 DrawItem 时勾选框不显示的风险；列结构与渲染图一致。
/// </summary>
public class InplaceBackupPanel : UserControl
{
    // ── 对外契约（BatchCopyWindow 订阅；全部已在 UI 线程触发）──
    public event Action<string, int, int>? ProgressRequested;
    public event Action<string, string>? StatusRequested;
    public event Action<bool>? BusyChanged;

    public bool IsBusy => _scanning || _backupRunning || _service.IsRunning;
    public string CurrentSuffix => _txtSuffix.Text;

    private readonly InplaceBackupService _service = new();
    private readonly System.Windows.Forms.Timer _filterDebounce = new();
    private readonly System.Windows.Forms.Timer _statusDebounce = new();
    private CancellationTokenSource? _scanCts;
    private bool _scanning;
    private bool _backupRunning;
    private bool _externalBusy;
    private bool _suppressEvents;
    private bool _suppressGridEvents;
    private string _completionDetail = "";
    private int _excludedCount;

    // ── 纯内存数据 ──
    private readonly List<BackupFileEntry> _scanned = [];
    private readonly HashSet<string> _checked = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _roots = new(StringComparer.OrdinalIgnoreCase);
    private List<BackupFileEntry> _visible = [];

    // ── A 区 ──
    private readonly Button _btnSelectFiles = new() { Text = "选择文件", Size = new Size(92, 30) };
    private readonly Button _btnRemoveSelected = new() { Text = "移除选中", Size = new Size(88, 30) };
    private readonly Button _btnClearFiles = new() { Text = "清空列表", Size = new Size(88, 30) };
    private readonly Label _lblCount = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true };

    // ── B 区 ──
    private readonly DarkComboBox _cmbExtension = new() { DropDownStyle = ComboBoxStyle.DropDownList, Size = new Size(160, 30), Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly TextBox _txtKeyword = new() { Size = new Size(240, 30), Anchor = AnchorStyles.Left | AnchorStyles.Right };

    // ── C 区 ──
    private readonly Button _btnSelectAll = new() { Text = "全选", Size = new Size(76, 28) };
    private readonly Button _btnDeselectAll = new() { Text = "全不选", Size = new Size(76, 28) };
    private readonly Label _lblVisibleCount = new() { Size = new Size(110, 28), TextAlign = ContentAlignment.MiddleLeft };
    private readonly DataGridView _grid = new();

    // ── D 区 ──
    private readonly TextBox _txtSuffix = new() { Size = new Size(160, 30), Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Label _lblSample = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly Button _btnStartBackup = new() { Text = "开始批量备份", Dock = DockStyle.Fill };
    private readonly Button _btnCancelBackup = new() { Text = "取消备份", Dock = DockStyle.Fill };

    public InplaceBackupPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Workspace; // R-18：显式复设为 workspace，避免 TabPage 被 UiTheme 刷成 Panel

        BuildUI();
        WireEvents();

        _filterDebounce.Interval = 300;
        _filterDebounce.Tick += (_, _) =>
        {
            _filterDebounce.Stop();
            RebuildVisible();
            RefreshGrid();
            UpdateCounts();
            RefreshStatusHint();
        };

        _statusDebounce.Interval = 250;
        _statusDebounce.Tick += (_, _) => { _statusDebounce.Stop(); RefreshStatusHint(); };

        _txtSuffix.Text = LoadInitialSuffix();
        _cmbExtension.Items.Add("全部");
        _cmbExtension.SelectedIndex = 0;
        LoadSuffixAutoComplete();
        UpdateSampleLabel();
        RefreshStatusHint();
        RefreshEnabledState();
    }

    // ══════════════════════════════════════════════════════════
    //  UI 构建（A / B / C / D 四区；进度区由窗口级共享）
    // ══════════════════════════════════════════════════════════

    private void BuildUI()
    {
        var main = Grid(1, 4,
            new RowStyle(SizeType.Absolute, 82),
            new RowStyle(SizeType.Absolute, 66),
            new RowStyle(SizeType.Percent, 100),
            new RowStyle(SizeType.Absolute, 86));
        main.Padding = new Padding(12);

        Add(main, BuildSourceCard(), 0, 0);
        Add(main, BuildFilterCard(), 0, 1);
        Add(main, BuildListCard(), 0, 2);
        Add(main, BuildSettingsCard(), 0, 3);
        Controls.Add(main);
    }

    /// <summary>
    /// 显式 RowStyles + 零 Margin 是这里的硬性要求：TableLayoutPanel 单行若没有 RowStyle
    /// 会退化成 AutoSize 行，被子控件撑到 100px 以上，把固定高度卡片里的按钮和标签裁掉。
    /// </summary>
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
        if (table.RowStyles.Count == 0)
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
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
        var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Panel, Padding = new Padding(12, 8, 12, 8), Margin = Padding.Empty };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(UiTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(1, card.Width - 1), Math.Max(1, card.Height - 1));
        };
        return card;
    }

    private Control BuildSourceCard()
    {
        var card = NewCard();
        var inner = Grid(1, 2, new RowStyle(SizeType.Absolute, 24), new RowStyle(SizeType.Percent, 100));

        var head = Grid(2, 1, new RowStyle(SizeType.Percent, 100));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Add(head, Lbl("源文件夹"), 0, 0);
        Add(head, Lbl("选择多个文件夹，自动递归收集其下全部文件（只读扫描）"), 1, 0);
        Add(inner, head, 0, 0);

        var tool = Grid(2, 1, new RowStyle(SizeType.Percent, 100));
        tool.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tool.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(0, 4, 0, 4),
            Margin = Padding.Empty
        };
        _btnSelectFiles.Margin = new Padding(0, 0, 8, 0);
        _btnRemoveSelected.Margin = new Padding(0, 0, 8, 0);
        _btnClearFiles.Margin = Padding.Empty;
        buttons.Controls.AddRange([_btnSelectFiles, _btnRemoveSelected, _btnClearFiles]);
        Add(tool, buttons, 0, 0);
        Add(tool, _lblCount, 1, 0);
        Add(inner, tool, 0, 1);

        card.Controls.Add(inner);
        return card;
    }

    private Control BuildFilterCard()
    {
        var card = NewCard();
        var row = Grid(6, 1, new RowStyle(SizeType.Percent, 100));
        row.Padding = new Padding(0, 4, 0, 4);
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 10));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));

        Add(row, Lbl("后缀"), 0, 0);
        Add(row, _cmbExtension, 1, 0);
        Add(row, Lbl("文件名包含"), 2, 0);
        Add(row, _txtKeyword, 3, 0);
        Add(row, new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty }, 4, 0);
        Add(row, Lbl("两项条件同时生效 · 过滤只影响显示", ContentAlignment.MiddleRight), 5, 0);

        card.Controls.Add(row);
        return card;
    }

    private Control BuildListCard()
    {
        var card = NewCard();
        var inner = Grid(1, 2, new RowStyle(SizeType.Absolute, 34), new RowStyle(SizeType.Percent, 100));

        var head = Grid(2, 1, new RowStyle(SizeType.Percent, 100));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        Add(head, Lbl("文件列表"), 0, 0);
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(0, 2, 0, 2),
            Margin = Padding.Empty
        };
        _btnSelectAll.Margin = new Padding(3, 0, 3, 0);
        _btnDeselectAll.Margin = new Padding(3, 0, 3, 0);
        _lblVisibleCount.Margin = Padding.Empty;
        actions.Controls.AddRange([_btnDeselectAll, _btnSelectAll, _lblVisibleCount]);
        Add(head, actions, 1, 0);
        Add(inner, head, 0, 0);

        BuildGrid();
        Add(inner, _grid, 0, 1);
        card.Controls.Add(inner);
        return card;
    }

    private void BuildGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToOrderColumns = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        _grid.ColumnHeadersHeight = 28;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.BackgroundColor = UiTheme.List;
        _grid.ShowCellToolTips = true;

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "colChecked", HeaderText = "", Width = 44, ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.NotSortable, Frozen = true
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colFileName", HeaderText = "文件名", Width = 280, ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colType", HeaderText = "类型", Width = 100, ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colSize", HeaderText = "大小", Width = 100, ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colDirectory", HeaderText = "所在目录", Width = 260, ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private Control BuildSettingsCard()
    {
        var card = NewCard();
        var row = Grid(4, 1, new RowStyle(SizeType.Percent, 100));
        row.Padding = new Padding(0, 4, 0, 4);
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208));

        Add(row, Lbl("备份后缀"), 0, 0);
        Add(row, _txtSuffix, 1, 0);
        Add(row, _lblSample, 2, 0);

        // 两枚按钮叠放：UiTheme 在 Apply 时按初始文本固定按钮状态图，
        // 「开始批量备份」= primary(绿)、「取消备份」= danger(红)。
        var buttonHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 12, 0, 12),
            Margin = Padding.Empty
        };
        _btnStartBackup.Margin = Padding.Empty;
        _btnCancelBackup.Margin = Padding.Empty;
        buttonHost.Controls.Add(_btnStartBackup);
        buttonHost.Controls.Add(_btnCancelBackup);
        _btnCancelBackup.Visible = false;
        Add(row, buttonHost, 3, 0);

        card.Controls.Add(row);
        return card;
    }

    // ══════════════════════════════════════════════════════════
    //  事件绑定
    // ══════════════════════════════════════════════════════════

    private void WireEvents()
    {
        _btnSelectFiles.Click += async (_, _) => await SelectFoldersAsync();
        _btnRemoveSelected.Click += (_, _) => RemoveSelectedRows();
        _btnClearFiles.Click += (_, _) => ClearList();
        _btnSelectAll.Click += (_, _) => SetVisibleChecked(true);
        _btnDeselectAll.Click += (_, _) => SetVisibleChecked(false);
        _btnStartBackup.Click += async (_, _) => await StartBackupAsync();
        _btnCancelBackup.Click += (_, _) => CancelCurrentOperation();

        _cmbExtension.SelectedIndexChanged += (_, _) => { if (!_suppressEvents) DebounceFilter(); };
        _txtKeyword.TextChanged += (_, _) => DebounceFilter();
        _txtSuffix.TextChanged += (_, _) =>
        {
            UpdateSampleLabel();
            DebounceFilter(); // 排除规则与占用预检随后缀重算
            RefreshEnabledState();
        };

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        _grid.CellValueChanged += Grid_CellValueChanged;

        // 服务事件在后台线程触发：一律切回 UI 线程并先判 IsDisposed
        _service.ProgressChanged += msg => Ui(() => StatusRequested?.Invoke(msg, _completionDetail));
        _service.ProgressReported += (msg, done, total) => Ui(() => ProgressRequested?.Invoke(msg, done, total));
        _service.ErrorOccurred += msg => Ui(() =>
        {
            _completionDetail = msg;
            StatusRequested?.Invoke("备份中有文件失败，详见完成汇总", _completionDetail);
        });
        _service.Completed += _ => { /* 明细由 BackupAsync 返回值统一汇总 */ };
    }

    private void Ui(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        // ObjectDisposedException 派生自 InvalidOperationException，一个 catch 即可覆盖
        try { BeginInvoke(action); }
        catch (InvalidOperationException) { }
    }

    private void DebounceFilter()
    {
        _filterDebounce.Stop();
        _filterDebounce.Start();
    }

    /// <summary>勾选变化频繁时，占用预检做防抖，避免逐格 stat 卡顿。</summary>
    private void DebounceStatusHint()
    {
        _statusDebounce.Stop();
        _statusDebounce.Start();
    }

    // ══════════════════════════════════════════════════════════
    //  A 区：选择 / 移除 / 清空（全部只操作内存列表，零磁盘 I/O）
    // ══════════════════════════════════════════════════════════

    private async Task SelectFoldersAsync()
    {
        if (IsBusy || _externalBusy) return;
        using var dialog = new FolderMultiSelectDialog(_roots.FirstOrDefault());
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var added = 0;
        foreach (var dir in dialog.SelectedFolders)
            if (_roots.Add(dir)) added++;
        if (added == 0 && _roots.Count > 0)
            StatusRequested?.Invoke("所选文件夹已在列表中，重新扫描以刷新文件（只读）", "");

        await RunScanAsync();
    }

    private void RemoveSelectedRows()
    {
        if (IsBusy || _externalBusy) return;
        var paths = _grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(r => (r.Tag as BackupFileEntry)?.FullPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (paths.Count == 0) return;

        _scanned.RemoveAll(e => paths.Contains(e.FullPath));
        foreach (var path in paths) _checked.Remove(path);

        RebuildExtensionCombo();
        RebuildVisible();
        RefreshGrid();
        UpdateCounts();
        RefreshStatusHint();
        StatusRequested?.Invoke($"已从列表移除 {paths.Count} 项（仅内存列表，磁盘文件未改动）", "");
    }

    private void ClearList()
    {
        if (IsBusy || _externalBusy) return;
        _scanned.Clear();
        _checked.Clear();
        _roots.Clear();
        _visible.Clear();
        _excludedCount = 0;
        _completionDetail = "";

        _suppressEvents = true;
        _cmbExtension.Items.Clear();
        _cmbExtension.Items.Add("全部");
        _cmbExtension.SelectedIndex = 0;
        _suppressEvents = false;

        RefreshGrid();
        UpdateCounts();
        RefreshStatusHint();
        StatusRequested?.Invoke("列表已清空（仅内存列表，磁盘文件未改动）", "");
    }

    // ══════════════════════════════════════════════════════════
    //  扫描（只读）
    // ══════════════════════════════════════════════════════════

    private async Task RunScanAsync()
    {
        if (_scanning || _service.IsRunning || _roots.Count == 0) return;

        _scanning = true;
        _scanCts = new CancellationTokenSource();
        RefreshEnabledState();
        BusyChanged?.Invoke(true);
        StatusRequested?.Invoke("扫描中…（只读，可点「取消扫描」中止）", "");

        try
        {
            var entries = await _service.ScanAsync(_roots.ToList(), _scanCts.Token);
            if (IsDisposed) return;

            var existing = _scanned.Select(e => e.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
                if (existing.Add(entry.FullPath)) _scanned.Add(entry);

            _scanned.Sort((a, b) =>
            {
                var byDir = string.Compare(a.Directory, b.Directory, StringComparison.OrdinalIgnoreCase);
                return byDir != 0 ? byDir : string.Compare(a.FileName, b.FileName, StringComparison.OrdinalIgnoreCase);
            });

            RebuildExtensionCombo();
            RebuildVisible();
            RefreshGrid();
            UpdateCounts();
            RefreshStatusHint();
            StatusRequested?.Invoke($"扫描完成：共 {_scanned.Count} 个文件（只读，未改动任何文件）", "");
        }
        catch (OperationCanceledException)
        {
            StatusRequested?.Invoke("扫描已取消（未改动任何文件）", "");
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"InplaceBackupPanel: scan failed: {ex.Message}");
            _completionDetail = $"扫描失败：{ex.Message}（未改动任何文件）";
            StatusRequested?.Invoke(_completionDetail, _completionDetail);
        }
        finally
        {
            _scanCts?.Dispose();
            _scanCts = null;
            _scanning = false;
            RefreshEnabledState();
            BusyChanged?.Invoke(false);
            RefreshStatusHint();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  过滤 / 列表
    // ══════════════════════════════════════════════════════════

    private static string ExtensionKey(BackupFileEntry entry) =>
        string.IsNullOrEmpty(entry.Extension) ? "(无扩展名)" : entry.Extension;

    private static bool IsLikelyBackup(BackupFileEntry entry, string suffix)
    {
        if (!InplaceBackupService.ValidateSuffix(suffix, out _)) return false;
        var stem = Path.GetFileNameWithoutExtension(entry.FileName);
        return stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    private void RebuildVisible()
    {
        var suffix = _txtSuffix.Text;
        var entries = _scanned.Where(e => !IsLikelyBackup(e, suffix)).ToList();
        _excludedCount = _scanned.Count - entries.Count;

        var ext = _cmbExtension.SelectedItem as string ?? "全部";
        if (ext != "全部")
            entries = entries.Where(e => ExtensionKey(e) == ext).ToList();

        var keyword = _txtKeyword.Text.Trim();
        if (keyword.Length > 0)
            entries = entries.Where(e => e.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        _visible = entries;
    }

    private void RebuildExtensionCombo()
    {
        var current = _cmbExtension.SelectedItem as string ?? "全部";
        var keys = _scanned.Select(ExtensionKey).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        _suppressEvents = true;
        _cmbExtension.Items.Clear();
        _cmbExtension.Items.Add("全部");
        foreach (var key in keys) _cmbExtension.Items.Add(key);
        _cmbExtension.SelectedItem = current != "全部" && keys.Contains(current, StringComparer.OrdinalIgnoreCase) ? current : "全部";
        _suppressEvents = false;
    }

    private void RefreshGrid()
    {
        _suppressGridEvents = true;
        _grid.Rows.Clear();
        foreach (var entry in _visible)
        {
            var dir = string.IsNullOrEmpty(entry.Directory) || entry.Directory == "." ? "(根目录)" : entry.Directory;
            var index = _grid.Rows.Add(
                _checked.Contains(entry.FullPath),
                entry.FileName,
                entry.ExtensionText,
                entry.SizeText,
                dir);
            var row = _grid.Rows[index];
            row.Tag = entry;
            row.Cells[4].ToolTipText = entry.FullPath;
        }
        _suppressGridEvents = false;
        _lblVisibleCount.Text = $"当前显示 {_visible.Count} 个   ";
    }

    private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressGridEvents || e.RowIndex < 0 || e.ColumnIndex != 0) return;
        if (IsBusy || _externalBusy) return;
        var row = _grid.Rows[e.RowIndex];
        if (row.Tag is not BackupFileEntry entry) return;

        var isChecked = row.Cells[0].Value is bool b && b;
        entry.Checked = isChecked;
        if (isChecked) _checked.Add(entry.FullPath);
        else _checked.Remove(entry.FullPath);

        UpdateCounts();
        RefreshEnabledState();
        DebounceStatusHint();
    }

    private void SetVisibleChecked(bool value)
    {
        if (IsBusy || _externalBusy) return;
        foreach (var entry in _visible)
        {
            entry.Checked = value;
            if (value) _checked.Add(entry.FullPath);
            else _checked.Remove(entry.FullPath);
        }
        RefreshGrid();
        UpdateCounts();
        RefreshEnabledState();
        DebounceStatusHint();
    }

    private void UpdateCounts()
    {
        _lblCount.Text = $"已扫描 {_scanned.Count} 个文件 / 显示 {_visible.Count} / 已勾选 {_checked.Count}";
    }

    // ══════════════════════════════════════════════════════════
    //  后缀 / 示例 / 预检 / 状态栏
    // ══════════════════════════════════════════════════════════

    private bool _startButtonSkinWired;

    /// <summary>UiTheme.Apply 之后调用，恢复被主题重置的示例文字颜色与按钮锁定态。</summary>
    public void RefreshVisualState()
    {
        if (!_startButtonSkinWired)
        {
            _startButtonSkinWired = true;
            // 订阅晚于 UiTheme.Apply，确保禁用态文字不用过暗的 Disabled 色
            _btnStartBackup.EnabledChanged += (_, _) => RefreshActionButtonText();
            _btnCancelBackup.EnabledChanged += (_, _) => RefreshActionButtonText();
        }
        RefreshActionButtonText();
        UpdateSampleLabel();
        RefreshEnabledState();
        RefreshStatusHint();
    }

    private void RefreshActionButtonText()
    {
        var font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold);
        _btnStartBackup.Font = font;
        _btnCancelBackup.Font = font;
        _btnStartBackup.ForeColor = _btnStartBackup.Enabled ? UiTheme.Text : UiTheme.Muted;
        _btnCancelBackup.ForeColor = _btnCancelBackup.Enabled ? Color.White : UiTheme.Muted;
    }

    private bool SuffixValid => InplaceBackupService.ValidateSuffix(_txtSuffix.Text, out _);

    private void UpdateSampleLabel()
    {
        var suffix = _txtSuffix.Text;
        if (!InplaceBackupService.ValidateSuffix(suffix, out var reason))
        {
            _lblSample.ForeColor = UiTheme.Red;
            _lblSample.Text = $"后缀不合法：{reason}";
            return;
        }
        var sample = InplaceBackupService.BuildDestination("hero.png", suffix);
        _lblSample.ForeColor = UiTheme.Cyan;
        _lblSample.Text = $"生成示例　hero.png → {Path.GetFileName(sample)}　（复制到各自原目录）";
    }

    private int CountOccupiedTargets()
    {
        if (!SuffixValid) return 0;
        var suffix = _txtSuffix.Text;
        var count = 0;
        foreach (var path in _checked)
        {
            try
            {
                var dest = InplaceBackupService.BuildDestination(path, suffix);
                if (File.Exists(dest) || Directory.Exists(dest)) count++;
            }
            catch { /* 只读预检，异常忽略 */ }
        }
        return count;
    }

    private void RefreshStatusHint()
    {
        if (IsBusy || _externalBusy) return;
        var actionHint = _checked.Count > 0
            ? $"已勾选 {_checked.Count} 个，点击「开始批量备份」在原目录生成带后缀副本"
            : "勾选文件后点击「开始批量备份」，在原目录生成带后缀副本";
        var parts = new List<string> { $"就绪 · 已扫描 {_scanned.Count} 个文件 · {actionHint}" };
        if (!SuffixValid)
            parts.Add("备份后缀不合法，主按钮已禁用");
        if (_excludedCount > 0)
            parts.Add($"已排除 {_excludedCount} 个疑似备份文件");
        var occupied = CountOccupiedTargets();
        if (occupied > 0)
            parts.Add($"{occupied} 个目标已存在，将自动跳过");
        StatusRequested?.Invoke(string.Join(" · ", parts), _completionDetail);
    }

    private static string LoadInitialSuffix()
    {
        try
        {
            foreach (var item in ConfigService.LoadPathHistory().BackupSuffixHistory)
                if (InplaceBackupService.ValidateSuffix(item, out _)) return item;
        }
        catch { /* 历史数据不可信时回落默认值 */ }
        return "_bf";
    }

    private void LoadSuffixAutoComplete()
    {
        try
        {
            var source = new AutoCompleteStringCollection();
            foreach (var item in ConfigService.LoadPathHistory().BackupSuffixHistory)
                if (InplaceBackupService.ValidateSuffix(item, out _)) source.Add(item);
            _txtSuffix.AutoCompleteMode = AutoCompleteMode.Suggest;
            _txtSuffix.AutoCompleteSource = AutoCompleteSource.CustomSource;
            _txtSuffix.AutoCompleteCustomSource = source;
        }
        catch { }
    }

    /// <summary>
    /// 由窗口在 OnFormClosed 中、Tab1 保存完自身历史之后调用：
    /// 从磁盘重新载入最新历史再写回，避免与 Tab1 的内存 _history 互相覆盖。
    /// </summary>
    public static void PersistSuffixHistory(string suffix)
    {
        if (!InplaceBackupService.ValidateSuffix(suffix, out _)) return;
        try
        {
            var history = ConfigService.LoadPathHistory();
            history.BackupSuffixHistory.RemoveAll(s => string.Equals(s, suffix, StringComparison.Ordinal));
            history.BackupSuffixHistory.Insert(0, suffix);
            if (history.BackupSuffixHistory.Count > 10)
                history.BackupSuffixHistory.RemoveRange(10, history.BackupSuffixHistory.Count - 10);
            ConfigService.SavePathHistory(history);
        }
        catch (Exception ex)
        {
            OperationLogger.Warn($"InplaceBackupPanel: save suffix history failed: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════
    //  执行 / 取消 / 锁定
    // ══════════════════════════════════════════════════════════

    private async Task StartBackupAsync()
    {
        if (IsBusy || _externalBusy) return;
        var suffix = _txtSuffix.Text;
        if (!InplaceBackupService.ValidateSuffix(suffix, out var reason))
        {
            StatusRequested?.Invoke($"后缀不合法：{reason}（未执行，零磁盘写入）", "");
            return;
        }

        // 执行集合快照：包含全部勾选项（过滤只影响显示，design.md 4.2 规则 2）
        var snapshot = _scanned.Where(e => _checked.Contains(e.FullPath)).Select(e => e.FullPath).ToList();
        if (snapshot.Count == 0)
        {
            StatusRequested?.Invoke("请先勾选要备份的文件", "");
            return;
        }

        var hidden = snapshot.Count - _visible.Count(e => _checked.Contains(e.FullPath));
        var hiddenHint = hidden > 0
            ? $"已勾选 {snapshot.Count} 个（其中 {hidden} 个当前被过滤隐藏），执行时一并备份"
            : "";

        _completionDetail = "";
        _backupRunning = true; // 在 await 之前置位，确保锁定与取消按钮立即生效
        RefreshEnabledState();
        BusyChanged?.Invoke(true);
        ProgressRequested?.Invoke($"准备备份：{snapshot.Count} 个文件", 0, snapshot.Count);
        StatusRequested?.Invoke("备份中…（不覆盖、不删改源文件）", hiddenHint);

        try
        {
            // 服务层是安全边界：二次校验 + INV-1~8 全部断言
            var result = await _service.BackupAsync(snapshot, _roots.ToList(), suffix, CancellationToken.None);
            _completionDetail = result.DetailText();
            ProgressRequested?.Invoke(result.Summary, snapshot.Count, snapshot.Count);
            StatusRequested?.Invoke(result.Summary, _completionDetail);
        }
        catch (Exception ex)
        {
            OperationLogger.Error($"InplaceBackupPanel: backup failed: {ex.Message}");
            _completionDetail = ex.Message;
            StatusRequested?.Invoke($"备份出错：{ex.Message}", _completionDetail);
        }
        finally
        {
            // 不自动刷新列表（避免把新生成的 _bf 文件带入下一次全选）；
            // 不覆盖状态栏上的完成汇总。
            _backupRunning = false;
            RefreshEnabledState();
            BusyChanged?.Invoke(false);
        }
    }

    private void CancelCurrentOperation()
    {
        if (_service.IsRunning) _service.Cancel();
        else if (_scanning) _scanCts?.Cancel();
    }

    public void Cancel()
    {
        try { _scanCts?.Cancel(); }
        catch (ObjectDisposedException) { }
        _service.Cancel();
    }

    public void SetExternalBusy(bool busy)
    {
        _externalBusy = busy;
        RefreshEnabledState();
    }

    private void RefreshEnabledState()
    {
        if (IsDisposed) return;
        var busy = IsBusy;
        var locked = busy || _externalBusy;

        _btnSelectFiles.Enabled = !locked;
        _btnRemoveSelected.Enabled = !locked;
        _btnClearFiles.Enabled = !locked;
        _btnSelectAll.Enabled = !locked;
        _btnDeselectAll.Enabled = !locked;
        _cmbExtension.Enabled = !locked;
        _txtKeyword.Enabled = !locked;
        _txtSuffix.Enabled = !locked;
        _grid.Enabled = !locked;

        var canStart = !locked && _checked.Count > 0 && SuffixValid;
        _btnStartBackup.Visible = !busy;
        _btnStartBackup.Enabled = canStart;
        _btnCancelBackup.Visible = busy;
        _btnCancelBackup.Enabled = busy;
        _btnCancelBackup.Text = _scanning ? "取消扫描" : "取消备份";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _filterDebounce.Stop();
            _filterDebounce.Dispose();
            _statusDebounce.Stop();
            _statusDebounce.Dispose();
            _scanCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}
