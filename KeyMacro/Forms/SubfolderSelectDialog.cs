using KeyMacro.Services;

namespace KeyMacro.Forms;

public class SubfolderSelectDialog : Form
{
    private readonly CheckedListBox _clb;
    private readonly List<string> _subfolders;

    public List<string> SelectedFolders { get; private set; } = [];

    public SubfolderSelectDialog(List<string> subfolders)
    {
        _subfolders = subfolders;
        Text = "选择要导入的子文件夹";
        Icon = IconService.AppIcon;
        Size = new Size(350, 450);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        // Checked list box — create before buttons to fix nullable flow analysis
        _clb = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10),
            CheckOnClick = true
        };
        foreach (var folder in subfolders)
            _clb.Items.Add(folder, false);

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1
        };

        // Top buttons: 全选 / 全不选
        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8)
        };

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
        mainPanel.Controls.Add(topPanel);
        mainPanel.Controls.Add(_clb);

        // Bottom buttons
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
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
            SelectedFolders = _clb.CheckedItems.Cast<string>().ToList();
            DialogResult = DialogResult.OK;
            Close();
        };

        bottomPanel.Controls.AddRange([btnCancel, btnOk]);
        mainPanel.Controls.Add(bottomPanel);

        Controls.Add(mainPanel);
    }
}
