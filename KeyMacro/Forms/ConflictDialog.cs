using KeyMacro.Services;

namespace KeyMacro.Forms;

public partial class ConflictDialog : Form
{
    private ConflictAction _result = ConflictAction.Overwrite;

    public ConflictAction Result => _result;

    public ConflictDialog(string targetDir, List<string> conflictingFiles)
    {
        Text = "同名文件冲突";
        Icon = IconService.AppIcon;
        Size = new Size(620, 430);
        MinimumSize = new Size(520, 360);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = UiTheme.App;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 4,
            ColumnCount = 1,
            BackColor = UiTheme.App
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        // Title label
        var lblTitle = new Label
        {
            Text = $"目标目录存在同名文件:",
            Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
            AutoSize = true,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 0, 0, 8)
        };
        mainPanel.Controls.Add(lblTitle);

        // Target directory
        var lblDir = new Label
        {
            Text = targetDir,
            Font = new Font("Consolas", 9),
            AutoSize = false,
            Dock = DockStyle.Fill,
            Height = 28,
            ForeColor = UiTheme.Cyan,
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        mainPanel.Controls.Add(lblDir);

        // File list
        var listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            SelectionMode = SelectionMode.None,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = UiTheme.List,
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 0, 0, 12)
        };
        foreach (var f in conflictingFiles)
            listBox.Items.Add(f);
        mainPanel.Controls.Add(listBox);

        // Buttons panel
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = UiTheme.App
        };

        var btnSkip = new Button
        {
            Text = "跳过冲突",
            AutoSize = true,
            MinimumSize = new Size(100, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.PanelAlt
        };
        StyleButton(btnSkip, UiTheme.PanelAlt, UiTheme.Text);
        btnSkip.Click += (_, _) => { _result = ConflictAction.Skip; DialogResult = DialogResult.No; Close(); };

        var btnCancelAll = new Button
        {
            Text = "取消全部复制",
            AutoSize = true,
            MinimumSize = new Size(130, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Red,
            ForeColor = Color.White
        };
        StyleButton(btnCancelAll, UiTheme.Red, Color.White);
        btnCancelAll.Click += (_, _) => { _result = ConflictAction.CancelAll; DialogResult = DialogResult.Cancel; Close(); };

        var btnOverwrite = new Button
        {
            Text = "覆盖",
            AutoSize = true,
            MinimumSize = new Size(100, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Blue,
            ForeColor = Color.White
        };
        StyleButton(btnOverwrite, UiTheme.Blue, Color.White);
        btnOverwrite.Click += (_, _) => { _result = ConflictAction.Overwrite; DialogResult = DialogResult.Yes; Close(); };

        var btnOpenFolder = new Button
        {
            Text = "打开文件夹",
            AutoSize = true,
            MinimumSize = new Size(120, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.PanelAlt
        };
        StyleButton(btnOpenFolder, UiTheme.PanelAlt, UiTheme.Text);
        btnOpenFolder.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(targetDir)); }
            catch { }
        };

        btnPanel.Controls.AddRange([btnCancelAll, btnSkip, btnOverwrite, btnOpenFolder]);
        mainPanel.Controls.Add(btnPanel);

        Controls.Add(mainPanel);
        UiTheme.Apply(this, UiWindowProfile.ConflictDialog);
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
