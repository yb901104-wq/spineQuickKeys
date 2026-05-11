using KeyMacro.Services;

namespace KeyMacro.Forms;

public class SpineHotkeyViewer : Form
{
    private readonly List<SpineHotkeyCategory> _categories;
    private TreeView _treeView = null!;
    private DataGridView _dgv = null!;
    private Button _btnRecord = null!, _btnOk = null!, _btnCancel = null!;

    public bool HasImportedEntries => _categories.Any(c => c.Entries.Any(e => !string.IsNullOrEmpty(e.NormalizedShortcut)));

    public List<SpineHotkeyEntry> GetImportedEntries() =>
        _categories.SelectMany(c => c.Entries).Where(e => !string.IsNullOrEmpty(e.NormalizedShortcut)).ToList();

    public SpineHotkeyViewer(List<SpineHotkeyCategory> categories)
    {
        _categories = categories;
        Text = "Spine 热键查看器";
        Size = new Size(850, 700);
        MinimumSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;

        BuildUI();
        PopulateTree();
    }

    private void BuildUI()
    {
        _treeView = new TreeView
        {
            Dock = DockStyle.Left,
            Width = 200,
            HideSelection = false
        };
        _treeView.AfterSelect += TreeView_AfterSelect;

        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "热键名称" });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "快捷键", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 200 });

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
        _btnOk.Click += BtnOk_Click;

        _btnRecord = new Button
        {
            Text = "录入",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            Margin = new Padding(0, 0, 16, 0),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnRecord.Click += BtnRecord_Click;

        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnOk);
        bottomPanel.Controls.Add(_btnRecord);

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
            _dgv.Rows.Add(
                entry.Name,
                string.IsNullOrEmpty(entry.NormalizedShortcut) ? "(未设置)" : entry.NormalizedShortcut
            );
        }
    }

    private void BtnRecord_Click(object? sender, EventArgs e)
    {
        if (_treeView.SelectedNode?.Tag is not SpineHotkeyCategory cat) return;
        if (_dgv.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先在表格中选择要修改的热键行。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var rowIndex = _dgv.SelectedRows[0].Index;
        if (rowIndex < 0 || rowIndex >= cat.Entries.Count) return;

        using var recorder = new HotkeyRecorderForm(allowNoModifier: true);
        if (recorder.ShowDialog() == DialogResult.OK)
        {
            cat.Entries[rowIndex].NormalizedShortcut = recorder.RecordedHotkey;
            cat.Entries[rowIndex].RawShortcut = recorder.RecordedHotkey;
            _dgv.Rows[rowIndex].Cells[1].Value = recorder.RecordedHotkey;
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        var count = _categories.Sum(c => c.Entries.Count(e => !string.IsNullOrEmpty(e.NormalizedShortcut)));
        if (count == 0)
        {
            MessageBox.Show("没有已设置快捷键的条目，请先录入快捷键。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
