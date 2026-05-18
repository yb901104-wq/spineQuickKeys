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
        Size = new Size(1000, 700);
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterParent;

        // ── Top: file path display + load button ──
        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            Padding = new Padding(12, 6, 12, 0),
            ColumnCount = 3,
            RowCount = 1
        };
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
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
        _btnLoad = new Button { Text = "载入文件", AutoSize = true, MinimumSize = new Size(80, 28), FlatStyle = FlatStyle.Flat };
        _btnLoad.Click += BtnLoad_Click;

        topPanel.Controls.Add(_lblFilePath);
        topPanel.Controls.Add(_btnLoad, 1, 0);

        // ── Toolbar: record button + search ──
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(12, 4, 12, 0)
        };
        _btnRecord = new Button { Text = "录制按键", AutoSize = true, MinimumSize = new Size(80, 28), FlatStyle = FlatStyle.Flat };
        _btnRecord.Click += BtnRecord_Click;
        toolbar.Controls.Add(_btnRecord);

        toolbar.Controls.Add(new Label
        {
            Text = " 搜索:",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(20, 0, 0, 0)
        });
        _txtSearch = new TextBox
        {
            Width = 200,
            Margin = new Padding(4, 2, 0, 0)
        };
        _txtSearch.TextChanged += (_, _) =>
        {
            _searchFilter = _txtSearch.Text.Trim();
            RefreshGrid();
        };
        toolbar.Controls.Add(_txtSearch);

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
            BorderStyle = BorderStyle.Fixed3D
        };
        _dgv.CellFormatting += Dgv_CellFormatting;
        _dgv.CellBeginEdit += Dgv_CellBeginEdit;

        // ── Bottom: Save + Cancel ──
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnCancel = new Button { Text = "取消", AutoSize = true, MinimumSize = new Size(70, 30), FlatStyle = FlatStyle.Flat };
        _btnCancel.Click += (_, _) => Close();

        _btnSave = new Button
        {
            Text = "保存",
            AutoSize = true,
            MinimumSize = new Size(70, 30),
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += BtnSave_Click;

        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnSave);

        Controls.Add(bottomPanel);
        Controls.Add(_dgv);
        Controls.Add(toolbar);
        Controls.Add(topPanel);

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
        if (row.Index >= _entries.Count || _entries[row.Index].Name.StartsWith("---")) return;

        using var recorder = new HotkeyRecorderForm(allowNoModifier: true);
        if (recorder.ShowDialog() == DialogResult.OK)
        {
            row.Cells[1].Value = SpineHotkeyService.ToSpineFormat(recorder.RecordedHotkey);
        }
    }

    private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _entries.Count) return;
        if (_entries[e.RowIndex].Name.StartsWith("---"))
        {
            e.CellStyle.BackColor = Color.FromArgb(0xE8, 0xE8, 0xE8);
            e.CellStyle.Font = new Font("Microsoft YaHei", 9, FontStyle.Bold);
        }
    }

    private void Dgv_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _entries.Count) return;
        // Name column is read-only
        if (e.ColumnIndex == 0) e.Cancel = true;
        // Section header rows are read-only
        if (_entries[e.RowIndex].Name.StartsWith("---")) e.Cancel = true;
    }
}
