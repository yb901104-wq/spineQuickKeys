#nullable disable
using System.ComponentModel;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public class SubfolderSelectDialog : Form
{
    private readonly CheckedListBox _clb;
    private readonly TextBox _txtSearch;
    private readonly TextBox _txtExclude;
    private readonly List<string> _allItems;
    private readonly HashSet<int> _checkedIndices = [];

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> SelectedFolders { get; private set; } = [];

    public SubfolderSelectDialog(List<string> items)
    {
        _allItems = items;
        Text = "选择要导入的文件（勾选后确认）";
        Icon = IconService.AppIcon;
        Size = new Size(820, 620);
        MinimumSize = new Size(640, 460);
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

        // Exclude box
        _txtExclude = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10),
            Text = "",
            ForeColor = Color.Gray
        };
        _txtExclude.Enter += (_, _) => { if (_txtExclude.ForeColor == Color.Gray) { _txtExclude.Text = ""; _txtExclude.ForeColor = Color.Black; } };
        _txtExclude.Leave += (_, _) => { if (string.IsNullOrWhiteSpace(_txtExclude.Text)) { _txtExclude.Text = ""; _txtExclude.ForeColor = Color.Gray; } };
        _txtExclude.TextChanged += (_, _) => ApplyFilter();

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

        // Row 0: search + exclude
        var searchPanel = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(0, 0, 0, 6) };
        _txtSearch.Size = new Size(240, 24);
        searchPanel.Controls.Add(new Label { Text = "搜索:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        searchPanel.Controls.Add(_txtSearch);
        _txtExclude.Size = new Size(240, 24);
        searchPanel.Controls.Add(new Label { Text = "不包含:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft });
        searchPanel.Controls.Add(_txtExclude);
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
            Dock = DockStyle.Fill,
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
        UiTheme.Apply(this, UiWindowProfile.SubfolderSelect);
    }

    private string GetDisplayText(string path)
    {
        // Show filename (path) for readability
        var fileName = Path.GetFileName(path);
        var dir = Path.GetDirectoryName(path);
        return $"{fileName}  — {dir}";
    }

    private bool ShouldShow(int index)
    {
        var path = _allItems[index];
        var searchText = _txtSearch?.Text?.Trim();
        var excludeText = _txtExclude?.Text?.Trim();

        // Search (include) filter
        if (!string.IsNullOrEmpty(searchText) && _txtSearch?.ForeColor != Color.Gray)
        {
            if (!path.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Exclude filter
        if (!string.IsNullOrEmpty(excludeText) && _txtExclude?.ForeColor != Color.Gray)
        {
            if (path.Contains(excludeText, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private int GetMasterIndex(int displayIndex)
    {
        int count = 0;
        for (int i = 0; i < _allItems.Count; i++)
        {
            if (!ShouldShow(i)) continue;
            if (count == displayIndex) return i;
            count++;
        }
        return displayIndex;
    }

    private void ApplyFilter()
    {
        _clb.ItemCheck -= _clb_ItemCheck;
        _clb.Items.Clear();
        for (int i = 0; i < _allItems.Count; i++)
        {
            if (ShouldShow(i))
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
