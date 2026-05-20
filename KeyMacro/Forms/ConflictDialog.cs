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
        Size = new Size(500, 350);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 4,
            ColumnCount = 1
        };

        // Title label
        var lblTitle = new Label
        {
            Text = $"目标目录存在同名文件:",
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            AutoSize = true
        };
        mainPanel.Controls.Add(lblTitle);

        // Target directory
        var lblDir = new Label
        {
            Text = targetDir,
            Font = new Font("Consolas", 9),
            AutoSize = true,
            ForeColor = Color.DarkBlue
        };
        mainPanel.Controls.Add(lblDir);

        // File list
        var listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            SelectionMode = SelectionMode.None
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
            Padding = new Padding(0, 8, 0, 0)
        };

        var btnSkip = new Button
        {
            Text = "跳过",
            AutoSize = true,
            MinimumSize = new Size(100, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0)
        };
        btnSkip.Click += (_, _) => { _result = ConflictAction.Skip; DialogResult = DialogResult.No; Close(); };

        var btnOverwrite = new Button
        {
            Text = "覆盖",
            AutoSize = true,
            MinimumSize = new Size(100, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0x00, 0x78, 0xD7),
            ForeColor = Color.White
        };
        btnOverwrite.Click += (_, _) => { _result = ConflictAction.Overwrite; DialogResult = DialogResult.Yes; Close(); };

        var btnOpenFolder = new Button
        {
            Text = "打开文件夹",
            AutoSize = true,
            MinimumSize = new Size(120, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0)
        };
        btnOpenFolder.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(targetDir)); }
            catch { }
        };

        btnPanel.Controls.AddRange([btnSkip, btnOverwrite, btnOpenFolder]);
        mainPanel.Controls.Add(btnPanel);

        Controls.Add(mainPanel);
    }
}
