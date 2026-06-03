using KeyMacro.Services;

namespace KeyMacro.Forms;

public class InputDialog : Form
{
    public string Result => _txtInput.Text;

    private readonly TextBox _txtInput = new();

    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        Text = title;
        Icon = IconService.AppIcon;
        Size = new Size(520, 220);
        MinimumSize = new Size(420, 180);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var lblPrompt = new Label
        {
            Text = prompt,
            AutoSize = true
        };
        layout.Controls.Add(lblPrompt);

        _txtInput.Text = defaultValue;
        _txtInput.Dock = DockStyle.Fill;
        _txtInput.Margin = new Padding(0, 8, 0, 8);
        layout.Controls.Add(_txtInput);

        var btnPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Bottom
        };

        var btnCancel = new Button
        {
            Text = "取消",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnOk = new Button
        {
            Text = "确定",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x00, 0x78, 0xD7),
            ForeColor = Color.White
        };
        btnOk.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        btnPanel.Controls.AddRange([btnCancel, btnOk]);
        layout.Controls.Add(btnPanel);

        Controls.Add(layout);
        UiTheme.Apply(this, UiWindowProfile.InputDialog);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
