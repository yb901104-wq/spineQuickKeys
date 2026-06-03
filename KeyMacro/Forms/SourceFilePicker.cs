using System.ComponentModel;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public class SourceFilePicker : Form
{
    private readonly TextBox _txtDir = new();
    private readonly Button _btnBrowse = new();
    private readonly Button _btnRefresh = new();
    private readonly ListView _lvThumbnails = new();
    private readonly ImageList _imageList = new();
    private readonly Label _lblCount = new();
    private readonly Button _btnOk = new();
    private readonly Button _btnCancel = new();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> SelectedFiles { get; private set; } = [];

    private const int MaxThumbnails = 200;

    public SourceFilePicker()
    {
        Text = "选择源文件";
        Icon = IconService.AppIcon;
        Size = new Size(1000, 700);
        MinimumSize = new Size(760, 520);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);

        BuildUI();
        UiTheme.Apply(this, UiWindowProfile.SourceFilePicker);
    }

    private void BuildUI()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA)
        };

        // Toolbar
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4),
            Padding = new Padding(6, 4, 6, 4)
        };
        _txtDir.Size = new Size(620, 24);
        _txtDir.ReadOnly = true;
        _txtDir.BorderStyle = BorderStyle.FixedSingle;
        _txtDir.BackColor = Color.FromArgb(0xF8, 0xFB, 0xFE);
        _btnBrowse.Text = "浏览";
        _btnBrowse.AutoSize = true;
        _btnBrowse.MinimumSize = new Size(60, 28);
        _btnBrowse.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnBrowse);
        _btnBrowse.Click += (_, _) => BrowseDir();
        _btnRefresh.Text = "刷新";
        _btnRefresh.AutoSize = true;
        _btnRefresh.MinimumSize = new Size(60, 28);
        _btnRefresh.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnRefresh);
        _btnRefresh.Click += async (_, _) => await LoadThumbnailsAsync();
        toolbar.Controls.AddRange([_txtDir, _btnBrowse, _btnRefresh]);
        layout.Controls.Add(toolbar, 0, 0);

        // Thumbnail list
        _lvThumbnails.Dock = DockStyle.Fill;
        _lvThumbnails.View = View.LargeIcon;
        _lvThumbnails.LargeImageList = _imageList;
        _lvThumbnails.CheckBoxes = true;
        _lvThumbnails.MultiSelect = false;
        _lvThumbnails.BorderStyle = BorderStyle.FixedSingle;
        _lvThumbnails.BackColor = Color.White;
        _lvThumbnails.ItemCheck += (_, _) =>
            BeginInvoke(new Action(() =>
                _lblCount.Text = $"已选 {_lvThumbnails.CheckedItems.Count} 个文件"));
        layout.Controls.Add(_lvThumbnails, 0, 1);

        _imageList.ImageSize = new Size(96, 96);
        _imageList.ColorDepth = ColorDepth.Depth32Bit;

        // Bottom bar
        var bottomBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4),
            Padding = new Padding(8, 4, 8, 4)
        };
        _lblCount.Text = "已选 0 个文件";
        _lblCount.AutoSize = true;
        _lblCount.Margin = new Padding(0, 6, 8, 0);

        _btnCancel.Text = "取消";
        _btnCancel.AutoSize = true;
        _btnCancel.MinimumSize = new Size(80, 32);
        _btnCancel.FlatStyle = FlatStyle.Flat;
        StyleButton(_btnCancel);
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnOk.Text = "确认选择";
        _btnOk.AutoSize = true;
        _btnOk.MinimumSize = new Size(100, 32);
        _btnOk.FlatStyle = FlatStyle.Flat;
        _btnOk.BackColor = Color.FromArgb(0x00, 0x78, 0xD7);
        _btnOk.ForeColor = Color.White;
        StyleButton(_btnOk, Color.FromArgb(0x00, 0x78, 0xD7), Color.White);
        _btnOk.Click += (_, _) =>
        {
            SelectedFiles = _lvThumbnails.CheckedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag?.ToString())
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => p!)
                .ToList();
            DialogResult = DialogResult.OK;
            Close();
        };

        bottomBar.Controls.AddRange([_btnCancel, _btnOk, _lblCount]);
        layout.Controls.Add(bottomBar, 0, 2);

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        Controls.Add(layout);
    }

    private static void StyleButton(Button button)
    {
        StyleButton(button, Color.FromArgb(0xF2, 0xF2, 0xF2), Color.Black);
    }

    private static void StyleButton(Button button, Color backColor, Color foreColor)
    {
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderColor = Color.FromArgb(0x8A, 0x8A, 0x8A);
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

    private void BrowseDir()
    {
        using var dialog = new FolderBrowserDialog();
        if (!string.IsNullOrEmpty(_txtDir.Text))
            dialog.SelectedPath = _txtDir.Text;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _txtDir.Text = dialog.SelectedPath;
            _ = LoadThumbnailsAsync();
        }
    }

    private async Task LoadThumbnailsAsync()
    {
        var dir = _txtDir.Text;
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

        _lvThumbnails.Items.Clear();
        _imageList.Images.Clear();
        _lblCount.Text = "加载中...";

        var files = await Task.Run(() =>
        {
            try
            {
                var allFiles = Directory.GetFiles(dir)
                    .OrderBy(f => Path.GetFileName(f))
                    .ToList();
                if (allFiles.Count > MaxThumbnails)
                {
                    BeginInvoke(new Action(() => MessageBox.Show(this,
                        $"目录内文件超过 {MaxThumbnails} 个（共 {allFiles.Count} 个），\n仅显示前 200 个。",
                        "文件过多", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                    return allFiles.Take(MaxThumbnails).ToList();
                }
                return allFiles;
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() => MessageBox.Show(this, $"读取目录失败: {ex.Message}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                return new List<string>();
            }
        });

        var batchSize = 20;
        for (int i = 0; i < files.Count; i += batchSize)
        {
            var batch = files.Skip(i).Take(batchSize).ToList();
            var thumbnails = await Task.Run(() =>
                batch.Select(t =>
                {
                    try
                    {
                        using var img = Image.FromFile(t);
                        var thumb = new Bitmap(96, 96);
                        using var g = Graphics.FromImage(thumb);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        var ratio = Math.Min(96f / img.Width, 96f / img.Height);
                        var w = (int)(img.Width * ratio);
                        var h = (int)(img.Height * ratio);
                        var x = (96 - w) / 2;
                        var y = (96 - h) / 2;
                        g.Clear(Color.Transparent);
                        g.DrawImage(img, x, y, w, h);
                        return (name: Path.GetFileName(t), thumb);
                    }
                    catch
                    {
                        var placeholder = new Bitmap(96, 96);
                        using var g = Graphics.FromImage(placeholder);
                        g.Clear(Color.LightGray);
                        var ext = Path.GetExtension(t).TrimStart('.').ToUpperInvariant();
                        ext = string.IsNullOrEmpty(ext) ? "?" : ext.Length > 4 ? ext[..4] : ext;
                        g.DrawString(ext, new Font("Arial", 18, FontStyle.Bold), Brushes.Gray, 20, 35);
                        return (name: Path.GetFileName(t), thumb: placeholder);
                    }
                }).ToList());

            var startIdx = _imageList.Images.Count;
            foreach (var t in thumbnails)
                _imageList.Images.Add(t.thumb);

            for (int j = 0; j < thumbnails.Count; j++)
            {
                _lvThumbnails.Items.Add(new ListViewItem
                {
                    Text = thumbnails[j].name,
                    ImageIndex = startIdx + j,
                    Tag = files[i + j]
                });
            }
        }

        _lblCount.Text = "已选 0 个文件";
    }
}
