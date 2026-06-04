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
        BackColor = UiTheme.App;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1,
            BackColor = UiTheme.App
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var lblPrompt = new Label
        {
            Text = prompt,
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular),
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(lblPrompt);

        _txtInput.Text = defaultValue;
        _txtInput.Dock = DockStyle.Top;
        _txtInput.Height = 32;
        _txtInput.Margin = new Padding(0, 8, 0, 8);
        _txtInput.BorderStyle = BorderStyle.FixedSingle;
        _txtInput.BackColor = UiTheme.Input;
        _txtInput.ForeColor = UiTheme.Text;
        _txtInput.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Regular);
        layout.Controls.Add(_txtInput);

        var btnPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Bottom,
            BackColor = UiTheme.App
        };

        var btnCancel = new Button
        {
            Text = "取消",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            FlatStyle = FlatStyle.Flat
        };
        StyleButton(btnCancel, UiTheme.PanelAlt, UiTheme.Text);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnOk = new Button
        {
            Text = "确定",
            AutoSize = true,
            MinimumSize = new Size(80, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Blue,
            ForeColor = Color.White
        };
        StyleButton(btnOk, UiTheme.Blue, Color.White);
        btnOk.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        btnPanel.Controls.AddRange([btnCancel, btnOk]);
        layout.Controls.Add(btnPanel);

        Controls.Add(layout);
        UiTheme.Apply(this, UiWindowProfile.InputDialog);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Regular);
        button.Padding = new Padding(10, 0, 10, 1);
        button.FlatAppearance.BorderColor = UiTheme.BorderStrong;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Lighten(backColor);
        button.FlatAppearance.MouseDownBackColor = Darken(backColor);
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
}
