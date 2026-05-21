#nullable disable
using System.ComponentModel;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public class SubfolderSelectDialog : Form
{
    private readonly CheckedListBox _clb;
    private readonly TextBox _txtSearch;
    private readonly List<string> _allItems;
    private readonly HashSet<int> _checkedIndices = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> SelectedFolders { get; private set; } = [];

    public SubfolderSelectDialog(List<string> items)
    {
        _allItems = items;
        Text = "选择要导入的文件（勾选后确认）";
        Icon = IconService.AppIcon;
        Size = new Size(700, 550);
        MinimumSize = new Size(400, 300);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;

        // Search box
        _txtSearch = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10),
            Text = "",
            ForeColor = Color.Gray
        };
        _txtSearch.Enter += (_, _) => { if (_txtSearch.ForeColor == Color.Gray) { _txtSearch.Text = ""; _txtSearch.ForeColor = Color.Black; } };
        _txtSearch.Leave += (_, _) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = ""; _txtSearch.ForeColor = Color.Gray; } };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        // Checked list box
        _clb = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10),
            CheckOnClick = true,
            HorizontalScrollbar = true,
            IntegralHeight = false
        };
        _clb.ItemCheck += _clb_ItemCheck;

        // Populate
        for (int i = 0; i < _allItems.Count; i++)
            _clb.Items.Add(GetDisplayText(_allItems[i]), false);

        // Layout
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 4,
            ColumnCount = 1
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Row 0: search
        var searchPanel = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(0, 0, 0, 6) };
        _txtSearch.Size = new Size(300, 24);
        searchPanel.Controls.Add(new Label { Text = "搜索:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        searchPanel.Controls.Add(_txtSearch);
        mainPanel.Controls.Add(searchPanel, 0, 0);

        // Row 1: 全选 / 全不选
        var topPanel = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(0, 0, 0, 6) };

        var btnSelectAll = new Button
        {
            Text = "全选",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnSelectAll.Click += (_, _) =>
        {
            for (int i = 0; i < _clb.Items.Count; i++)
                _clb.SetItemChecked(i, true);
        };

        var btnDeselectAll = new Button
        {
            Text = "全不选",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnDeselectAll.Click += (_, _) =>
        {
            for (int i = 0; i < _clb.Items.Count; i++)
                _clb.SetItemChecked(i, false);
        };

        topPanel.Controls.AddRange([btnSelectAll, btnDeselectAll]);
        mainPanel.Controls.Add(topPanel, 0, 1);

        // Row 2: list
        mainPanel.Controls.Add(_clb, 0, 2);

        // Row 3: bottom buttons
        var bottomPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnCancel = new Button
        {
            Text = "取消",
            AutoSize = true,
            MinimumSize = new Size(80, 32),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnOk = new Button
        {
            Text = "确认添加",
            AutoSize = true,
            MinimumSize = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x00, 0x78, 0xD7),
            ForeColor = Color.White
        };
        btnOk.Click += (_, _) =>
        {
            SelectedFolders = _checkedIndices.Select(i => _allItems[i]).ToList();
            DialogResult = DialogResult.OK;
            Close();
        };

        bottomPanel.Controls.AddRange([btnCancel, btnOk]);
        mainPanel.Controls.Add(bottomPanel, 0, 3);

        Controls.Add(mainPanel);
    }

    private string GetDisplayText(string path)
    {
        // Show filename (path) for readability
        var fileName = Path.GetFileName(path);
        var dir = Path.GetDirectoryName(path);
        return $"{fileName}  — {dir}";
    }

    private int GetMasterIndex(int displayIndex)
    {
        var filter = _txtSearch?.Text?.Trim();
        if (string.IsNullOrEmpty(filter) || filter == "" || _txtSearch?.ForeColor == Color.Gray)
            return displayIndex;

        int count = 0;
        for (int i = 0; i < _allItems.Count; i++)
        {
            if (_allItems[i].Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                if (count == displayIndex) return i;
                count++;
            }
        }
        return displayIndex;
    }

    private void ApplyFilter()
    {
        var filter = _txtSearch.Text?.Trim();
        if (string.IsNullOrEmpty(filter) || _txtSearch.ForeColor == Color.Gray)
        {
            // Show all
            _clb.ItemCheck -= _clb_ItemCheck;
            _clb.Items.Clear();
            for (int i = 0; i < _allItems.Count; i++)
            {
                _clb.Items.Add(GetDisplayText(_allItems[i]), _checkedIndices.Contains(i));
            }
            _clb.ItemCheck += _clb_ItemCheck;
            return;
        }

        // Filter
        _clb.ItemCheck -= _clb_ItemCheck;
        _clb.Items.Clear();
        for (int i = 0; i < _allItems.Count; i++)
        {
            if (_allItems[i].Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                _clb.Items.Add(GetDisplayText(_allItems[i]), _checkedIndices.Contains(i));
            }
        }
        _clb.ItemCheck += _clb_ItemCheck;
    }

    private void _clb_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        var masterIdx = GetMasterIndex(e.Index);
        if (e.NewValue == CheckState.Checked) _checkedIndices.Add(masterIdx);
        else _checkedIndices.Remove(masterIdx);
    }
}
