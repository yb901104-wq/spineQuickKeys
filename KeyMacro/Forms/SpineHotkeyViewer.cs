using KeyMacro.Services;

namespace KeyMacro.Forms;

public class SpineHotkeyViewer : Form
{
    private readonly List<SpineHotkeyCategory> _categories;
    private TreeView _treeView = null!;
    private DataGridView _dgv = null!;
    private Button _btnImport = null!, _btnSelectAll = null!, _btnDeselectAll = null!;
    private Button _btnOk = null!, _btnCancel = null!;

    public List<SpineHotkeyEntry> ImportedEntries { get; private set; } = [];

    public SpineHotkeyViewer(List<SpineHotkeyCategory> categories)
    {
        _categories = categories;
        Text = "Spine 热键查看器";
        Size = new Size(850, 700);
        MinimumSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildUI();
        PopulateTree();
    }

    private void BuildUI()
    {
        // ── Left: Category Tree ──
        _treeView = new TreeView
        {
            Dock = DockStyle.Left,
            Width = 200,
            HideSelection = false
        };
        _treeView.AfterSelect += TreeView_AfterSelect;

        // ── Right: Entries Grid ──
        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true
        };

        var chkCol = new DataGridViewCheckBoxColumn
        {
            HeaderText = "选择",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        _dgv.Columns.Add(chkCol);
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "热键名称" });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "快捷键", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 200 });

        // ── Bottom Buttons ──
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnCancel = new Button { Text = "取消", AutoSize = true, MinimumSize = new Size(70, 30), FlatStyle = FlatStyle.Flat };
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnOk = new Button
        {
            Text = "确认",
            AutoSize = true,
            MinimumSize = new Size(70, 30),
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnOk.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        _btnDeselectAll = new Button { Text = "取消全选", AutoSize = true, MinimumSize = new Size(80, 30), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0) };
        _btnDeselectAll.Click += (_, _) => SetAllChecked(false);

        _btnSelectAll = new Button { Text = "全选", AutoSize = true, MinimumSize = new Size(70, 30), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 0) };
        _btnSelectAll.Click += (_, _) => SetAllChecked(true);

        _btnImport = new Button
        {
            Text = "录入选中",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            Margin = new Padding(0, 0, 16, 0),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnImport.Click += BtnImport_Click;

        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnOk);
        bottomPanel.Controls.Add(_btnDeselectAll);
        bottomPanel.Controls.Add(_btnSelectAll);
        bottomPanel.Controls.Add(_btnImport);

        Controls.Add(_dgv);
        Controls.Add(_treeView);
        Controls.Add(bottomPanel);
    }

    private void PopulateTree()
    {
        foreach (var cat in _categories)
        {
            var node = new TreeNode($"{cat.Name} ({cat.Entries.Count})")
            {
                Tag = cat
            };
            _treeView.Nodes.Add(node);
        }

        if (_treeView.Nodes.Count > 0)
            _treeView.SelectedNode = _treeView.Nodes[0];
    }

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not SpineHotkeyCategory cat) return;
        RefreshGrid(cat);
    }

    private void RefreshGrid(SpineHotkeyCategory cat)
    {
        _dgv.Rows.Clear();
        foreach (var entry in cat.Entries)
        {
            _dgv.Rows.Add(false, entry.Name, string.IsNullOrEmpty(entry.RawShortcut) ? "(未设置)" : entry.RawShortcut);
        }
    }

    private void SetAllChecked(bool check)
    {
        foreach (DataGridViewRow row in _dgv.Rows)
        {
            if (!row.IsNewRow)
                row.Cells[0].Value = check;
        }
    }

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        ImportedEntries.Clear();

        if (_treeView.SelectedNode?.Tag is not SpineHotkeyCategory cat) return;

        for (int i = 0; i < _dgv.Rows.Count && i < cat.Entries.Count; i++)
        {
            if (_dgv.Rows[i].Cells[0].Value is true && !string.IsNullOrEmpty(cat.Entries[i].NormalizedShortcut))
            {
                ImportedEntries.Add(cat.Entries[i]);
            }
        }

        if (ImportedEntries.Count == 0)
        {
            MessageBox.Show("请先勾选至少一个带有快捷键的条目。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        MessageBox.Show($"已录入 {ImportedEntries.Count} 个热键。", "录入成功",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        DialogResult = DialogResult.OK;
        Close();
    }
}
