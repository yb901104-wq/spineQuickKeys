using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public class ActivationAppForm : Form
{
    private readonly VirtualButtonManager _btnManager;
    private ComboBox _cmbButton = null!;
    private TextBox _txtPath = null!;
    private Button _btnPick = null!, _btnClear = null!, _btnClose = null!;

    public ActivationAppForm(VirtualButtonManager btnManager)
    {
        _btnManager = btnManager;
        InitializeComponent();
        LoadButtons();
    }

    private void InitializeComponent()
    {
        Text = "窗口激活应用管理";
        Size = new Size(450, 230);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Row 0: button selector
        var selectorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        selectorPanel.Controls.Add(new Label
        {
            Text = "选择按钮:",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 0);

        _cmbButton = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbButton.SelectedIndexChanged += (_, _) => OnSelectedButtonChanged();
        selectorPanel.Controls.Add(_cmbButton, 1, 0);
        mainPanel.Controls.Add(selectorPanel, 0, 0);

        // Row 1: activation path + action buttons
        var pathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        pathPanel.Controls.Add(new Label
        {
            Text = "激活程序:",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 0);

        _txtPath = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Microsoft YaHei", 9)
        };
        pathPanel.Controls.Add(_txtPath, 1, 0);

        _btnPick = new Button
        {
            Text = "选取激活",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 0, 4, 0)
        };
        _btnPick.Click += (_, _) => OnPickApp();
        pathPanel.Controls.Add(_btnPick, 2, 0);

        _btnClear = new Button
        {
            Text = "删除此激活",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(0xD9, 0x5C, 0x5C)
        };
        _btnClear.Click += (_, _) => OnClearApp();
        pathPanel.Controls.Add(_btnClear, 3, 0);

        mainPanel.Controls.Add(pathPanel, 0, 1);

        // Row 2: close button (bottom-right)
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };

        _btnClose = new Button
        {
            Text = "关闭",
            AutoSize = true,
            MinimumSize = new Size(70, 30),
            FlatStyle = FlatStyle.Flat
        };
        _btnClose.Click += (_, _) => Close();
        bottomPanel.Controls.Add(_btnClose);

        mainPanel.Controls.Add(bottomPanel, 0, 2);

        Controls.Add(mainPanel);
    }

    private void LoadButtons()
    {
        _cmbButton.Items.Clear();
        foreach (var btn in _btnManager.Buttons)
        {
            _cmbButton.Items.Add(new ButtonItem(btn));
        }
        if (_cmbButton.Items.Count > 0)
            _cmbButton.SelectedIndex = 0;
    }

    private void OnSelectedButtonChanged()
    {
        if (_cmbButton.SelectedItem is not ButtonItem item) return;
        _txtPath.Text = item.Button.TargetActivateAppPath ?? "";
    }

    private void OnPickApp()
    {
        if (_cmbButton.SelectedItem is not ButtonItem item) return;

        using var dialog = new OpenFileDialog
        {
            Title = "选择要激活的程序",
            Filter = "可执行文件 (*.exe)|*.exe",
            CheckFileExists = true
        };
        if (!string.IsNullOrEmpty(item.Button.TargetActivateAppPath))
            dialog.InitialDirectory = Path.GetDirectoryName(item.Button.TargetActivateAppPath);

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            item.Button.TargetActivateAppPath = dialog.FileName;
            _txtPath.Text = dialog.FileName;
        }
    }

    private void OnClearApp()
    {
        if (_cmbButton.SelectedItem is not ButtonItem item) return;
        item.Button.TargetActivateAppPath = null;
        _txtPath.Text = "";
    }

    private sealed class ButtonItem(VirtualButton button)
    {
        public VirtualButton Button => button;
        public override string ToString() => $"{button.Name} ({button.Id[..8]}...)";
    }
}
