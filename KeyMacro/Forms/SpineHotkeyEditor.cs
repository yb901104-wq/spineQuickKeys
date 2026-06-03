using KeyMacro.Services;

namespace KeyMacro.Forms;

public class SpineHotkeyEditor : Form
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public static List<SpineHotkeyEntry>? LastLoadedEntries { get; private set; }

    public static void SetLoadedEntries(List<SpineHotkeyEntry>? entries) => LastLoadedEntries = entries;
    private SpineHotkeyService _service;
    private List<SpineHotkeyEntry> _entries = [];
    private string _searchFilter = "";

    private readonly Label _lblFilePath;
    private readonly TextBox _txtSearch;
    private readonly DataGridView _dgv;
    private readonly Button _btnLoad;
    private readonly Button _btnSave;
    private readonly Button _btnCancel;
    private readonly Button _btnRecord;

    public SpineHotkeyEditor(string filePath)
        : this(filePath, null)
    {
    }

    /// <summary>Construct with pre-loaded data (used by import).</summary>
    public SpineHotkeyEditor(string filePath, List<SpineHotkeyEntry>? entries)
    {
        _service = new SpineHotkeyService(filePath);

        Text = "Spine 快捷键编辑";
        Icon = IconService.AppIcon;
        Size = new Size(1100, 720);
        MinimumSize = new Size(820, 520);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);

        // ── Top: file path display + load button ──
        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(14, 10, 14, 8),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3)
        };
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _lblFilePath = new Label
        {
            Text = filePath,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei", 9),
            ForeColor = Color.Gray,
            AutoEllipsis = true
        };
        _btnLoad = MakeButton("载入文件", Color.FromArgb(0xF2, 0xF2, 0xF2), Color.Black, new Size(80, 28));
        _btnLoad.Click += BtnLoad_Click;

        topPanel.Controls.Add(_lblFilePath);
        topPanel.Controls.Add(_btnLoad, 1, 0);

        // ── Toolbar: record button + search ──
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(14, 6, 14, 6),
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _btnRecord = MakeButton("录制按键", Color.FromArgb(0x1D, 0x6F, 0xB8), Color.White, new Size(80, 28));
        _btnRecord.Click += BtnRecord_Click;
        toolbar.Controls.Add(_btnRecord, 0, 0);

        toolbar.Controls.Add(new Label
        {
            Text = "搜索:",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(20, 0, 6, 0)
        }, 1, 0);
        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6)
        };
        _txtSearch.TextChanged += (_, _) =>
        {
            _searchFilter = _txtSearch.Text.Trim();
            RefreshGrid();
        };
        toolbar.Controls.Add(_txtSearch, 2, 0);

        // ── DataGridView ──
        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(0xB8, 0xB8, 0xB8),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 32,
            AllowUserToOrderColumns = false,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable
        };
        _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0xE1, 0xE7, 0xEA);
        _dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        _dgv.DefaultCellStyle.BackColor = Color.White;
        _dgv.DefaultCellStyle.ForeColor = Color.Black;
        _dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0x0B, 0x78, 0xD0);
        _dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        _dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xF7, 0xF7, 0xF7);
        _dgv.CellFormatting += Dgv_CellFormatting;
        _dgv.CellBeginEdit += Dgv_CellBeginEdit;

        // ── Bottom: Save + Cancel ──
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4)
        };

        _btnCancel = MakeButton("取消", Color.FromArgb(0xF2, 0xF2, 0xF2), Color.Black, new Size(70, 30));
        _btnCancel.Click += (_, _) => Close();

        _btnSave = MakeButton("保存", Color.FromArgb(0, 120, 215), Color.White, new Size(70, 30));
        _btnSave.Margin = new Padding(0, 0, 8, 0);
        _btnSave.Click += BtnSave_Click;

        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnSave);

        Controls.Add(bottomPanel);
        Controls.Add(_dgv);
        Controls.Add(toolbar);
        Controls.Add(topPanel);
        UiTheme.Apply(this, UiWindowProfile.SpineHotkeyEditor);

        // Keep LastLoadedEntries after close for other forms (SequenceEditor autocomplete).
        // Only clear on explicit file close (BtnLoad_Click will overwrite with new data).

        if (entries != null)
        {
            _entries = entries;
            LastLoadedEntries = entries;
            RefreshGrid();
        }
        else
        {
            LoadEntries();
        }
    }

    public List<SpineHotkeyEntry>? GetCurrentEntries() => _entries.Count > 0 ? [.. _entries] : null;

    private static Button MakeButton(string text, Color backColor, Color foreColor, Size minimumSize)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = minimumSize,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(0x8A, 0x8A, 0x8A);
        button.FlatAppearance.MouseOverBackColor = Lighten(backColor);
        button.FlatAppearance.MouseDownBackColor = Darken(backColor);
        return button;
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

    private void LoadEntries()
    {
        try
        {
            _entries = _service.Load();
            LastLoadedEntries = _entries;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"载入文件失败: {ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _entries = [];
            LastLoadedEntries = null;
        }
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        _dgv.Columns.Clear();
        _dgv.Rows.Clear();

        // Name column (read-only)
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "快捷键名称",
            ReadOnly = true,
            Width = 260,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        // Keys column (editable)
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "快捷键",
            Width = 200,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        // Chinese note column (editable)
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "功能说明",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        ApplyGridColumnStyles();

        var filter = _searchFilter;
        var hasFilter = filter.Length > 0;

        foreach (var entry in _entries)
        {
            // Section headers always show; other entries filter by name
            if (hasFilter && !entry.Name.StartsWith("---"))
            {
                if (!entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                    !entry.Keys.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            var rowIdx = _dgv.Rows.Add(entry.Name, entry.Keys, entry.ChineseNote ?? "");
            if (entry.Name.StartsWith("---"))
            {
                _dgv.Rows[rowIdx].ReadOnly = true;
                _dgv.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(0xE8, 0xE8, 0xE8);
                _dgv.Rows[rowIdx].DefaultCellStyle.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
            }
        }

        if (hasFilter && _dgv.Rows.Count == 0)
        {
            var idx = _dgv.Rows.Add("", $"无匹配结果: {_searchFilter}", "");
            _dgv.Rows[idx].ReadOnly = true;
            _dgv.Rows[idx].DefaultCellStyle.ForeColor = Color.Gray;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // Commit any in-progress cell edit before reading values
        _dgv.EndEdit();

        // Match grid rows to entries by Name, not by row index
        foreach (DataGridViewRow row in _dgv.Rows)
        {
            var name = row.Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(name) || name.StartsWith("---")) continue;
            var entry = _entries.FirstOrDefault(e => e.Name == name);
            if (entry == null) continue;
            entry.Keys = row.Cells[1].Value?.ToString() ?? "";
            entry.ChineseNote = row.Cells[2].Value?.ToString();
        }

        try
        {
            _service.Save(_entries);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存文件失败: {ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnLoad_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 Spine 快捷键文件",
            Filter = "快捷键文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _service = new SpineHotkeyService(dialog.FileName);
            _lblFilePath.Text = dialog.FileName;
            LoadEntries();
        }
    }

    private void BtnRecord_Click(object? sender, EventArgs e)
    {
        if (_dgv.SelectedRows.Count == 0) return;
        var row = _dgv.SelectedRows[0];
        var name = row.Cells[0].Value?.ToString();
        var entry = FindEntryByName(name);
        if (entry == null || entry.Name.StartsWith("---")) return;

        using var recorder = new HotkeyRecorderForm(allowNoModifier: true);
        if (recorder.ShowDialog() == DialogResult.OK)
        {
            row.Cells[1].Value = SpineHotkeyService.ToSpineFormat(recorder.RecordedHotkey);
        }
    }

    private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _dgv.Rows.Count) return;
        var name = _dgv.Rows[e.RowIndex].Cells[0].Value?.ToString();
        if (FindEntryByName(name)?.Name.StartsWith("---") == true)
        {
            e.CellStyle.BackColor = Color.FromArgb(0xE8, 0xE8, 0xE8);
            e.CellStyle.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
        }
    }

    private void ApplyGridColumnStyles()
    {
        if (_dgv.Columns.Count < 3) return;

        _dgv.Columns[0].DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0xF2, 0xF4, 0xF5),
            SelectionBackColor = Color.FromArgb(0x5B, 0x73, 0x84),
            SelectionForeColor = Color.White
        };

        var editableStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6),
            SelectionBackColor = Color.FromArgb(0xC7, 0x8F, 0x24),
            SelectionForeColor = Color.White
        };
        _dgv.Columns[1].DefaultCellStyle = editableStyle;
        _dgv.Columns[2].DefaultCellStyle = editableStyle.Clone();
    }

    private void Dgv_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _dgv.Rows.Count) return;
        var name = _dgv.Rows[e.RowIndex].Cells[0].Value?.ToString();
        var entry = FindEntryByName(name);
        // Name column is read-only
        if (e.ColumnIndex == 0) e.Cancel = true;
        // Section header rows are read-only
        if (entry?.Name.StartsWith("---") == true) e.Cancel = true;
    }

    private SpineHotkeyEntry? FindEntryByName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _entries.FirstOrDefault(e => e.Name == name);
    }
}
