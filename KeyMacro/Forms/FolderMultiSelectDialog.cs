#nullable enable
using System.ComponentModel;
using KeyMacro.Controls;
using KeyMacro.Services;

namespace KeyMacro.Forms;

/// <summary>
/// 多选文件夹弹窗。原生 FolderBrowserDialog 不支持多选，故沿用项目自绘勾选弹窗路线
/// （视觉参考 SubfolderSelectDialog）。
///
/// 安全：本弹窗只做**只读浏览**——Directory.EnumerateDirectories / File.GetAttributes /
/// 系统浏览对话框，不创建、删除、修改任何文件，也不在所选目录写任何内容。
/// </summary>
public class FolderMultiSelectDialog : Form
{
    private readonly DarkCheckedListBox _clb = new()
    {
        Dock = DockStyle.Fill,
        CheckOnClick = true,
        HorizontalScrollbar = true,
        IntegralHeight = false
    };

    private readonly TextBox _txtRoot = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _txtFilter = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _chkIncludeRoot = new() { Text = "包含当前根目录本身", AutoSize = true };
    private readonly Label _lblCount = new() { AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _lblHint = new()
    {
        AutoSize = true,
        Text = "勾选多个文件夹后确认；列表展示当前根目录的直接子文件夹，扫描时会自动递归收集其下全部文件。"
    };

    private readonly List<string> _allDirs = [];
    private readonly List<string> _visibleDirs = [];
    private readonly HashSet<string> _checkedPaths = new(StringComparer.OrdinalIgnoreCase);
    private string _root = "";

    /// <summary>确认后返回的绝对路径列表（已去重）。</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> SelectedFolders { get; private set; } = [];

    public FolderMultiSelectDialog(string? initialRoot = null)
    {
        Text = "选择文件夹（可多选）";
        Icon = IconService.AppIcon;
        Size = new Size(820, 620);
        MinimumSize = new Size(640, 460);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = UiTheme.App;

        _root = !string.IsNullOrWhiteSpace(initialRoot) && Directory.Exists(initialRoot)
            ? Path.GetFullPath(initialRoot)
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _txtRoot.Text = _root;

        _clb.ItemCheck += Clb_ItemCheck;

        BuildUI();
        UiTheme.Apply(this, UiWindowProfile.FolderMultiSelect);
        ReloadDirectories();
    }

    private void BuildUI()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 6
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        // Row 0: 根目录 + 浏览 / 刷新
        var rootRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1 };
        rootRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));
        rootRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        rootRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        rootRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8));

        var btnBrowse = new Button { Text = "浏览", Dock = DockStyle.Fill, MinimumSize = new Size(76, 28) };
        btnBrowse.Click += (_, _) => BrowseRoot();
        var btnRefresh = new Button { Text = "刷新", Dock = DockStyle.Fill, MinimumSize = new Size(76, 28) };
        btnRefresh.Click += (_, _) => ReloadDirectories();

        rootRow.Controls.Add(new Label { Text = "根目录", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        rootRow.Controls.Add(_txtRoot, 1, 0);
        rootRow.Controls.Add(btnBrowse, 2, 0);
        rootRow.Controls.Add(btnRefresh, 3, 0);
        main.Controls.Add(rootRow, 0, 0);

        // Row 1: 筛选
        var filterRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _txtFilter.TextChanged += (_, _) => ApplyFilter();
        filterRow.Controls.Add(new Label { Text = "名称包含", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        filterRow.Controls.Add(_txtFilter, 1, 0);
        main.Controls.Add(filterRow, 0, 1);

        // Row 2: 全选 / 全不选 / 包含根目录 / 计数
        var toolRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 2, 0, 2) };
        var btnAll = new Button { Text = "全选", AutoSize = true, MinimumSize = new Size(76, 28) };
        btnAll.Click += (_, _) =>
        {
            foreach (var dir in _visibleDirs) _checkedPaths.Add(dir);
            ApplyFilter();
        };
        var btnNone = new Button { Text = "全不选", AutoSize = true, MinimumSize = new Size(76, 28) };
        btnNone.Click += (_, _) =>
        {
            _checkedPaths.Clear();
            _chkIncludeRoot.Checked = false;
            ApplyFilter();
        };
        toolRow.Controls.AddRange([btnAll, btnNone, _chkIncludeRoot]);
        _chkIncludeRoot.CheckedChanged += (_, _) => UpdateCount();
        main.Controls.Add(toolRow, 0, 2);

        // Row 3: 提示 + 计数
        var hintRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        hintRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hintRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        hintRow.Controls.Add(_lblHint, 0, 0);
        hintRow.Controls.Add(_lblCount, 1, 0);
        main.Controls.Add(hintRow, 0, 3);

        // Row 4: 勾选列表
        main.Controls.Add(_clb, 0, 4);

        // Row 5: 底部按钮
        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var btnCancel = new Button { Text = "取消", AutoSize = true, MinimumSize = new Size(84, 32) };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        var btnOk = new Button { Text = "确认选择", AutoSize = true, MinimumSize = new Size(106, 32) };
        btnOk.Click += (_, _) => Confirm();
        bottom.Controls.AddRange([btnCancel, btnOk]);
        main.Controls.Add(bottom, 0, 5);

        Controls.Add(main);
    }

    private void BrowseRoot()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "选择要扫描的根目录（可在此定位，再回到列表中勾选多个子文件夹）",
            UseDescriptionForTitle = true
        };
        if (Directory.Exists(_root))
            dlg.SelectedPath = _root;

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        _root = Path.GetFullPath(dlg.SelectedPath);
        _txtRoot.Text = _root;
        _checkedPaths.Clear();
        ReloadDirectories();
    }

    /// <summary>只读枚举当前根目录下的直接子文件夹。</summary>
    private void ReloadDirectories()
    {
        _allDirs.Clear();
        if (Directory.Exists(_root))
        {
            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = false,
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false,
                    AttributesToSkip = FileAttributes.ReparsePoint
                };
                foreach (var dir in Directory.EnumerateDirectories(_root, "*", options))
                    _allDirs.Add(Path.GetFullPath(dir));
            }
            catch (Exception ex)
            {
                OperationLogger.Warn($"FolderMultiSelectDialog: enumerate failed: {_root} → {ex.Message}");
            }
        }
        _allDirs.Sort(StringComparer.OrdinalIgnoreCase);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = _txtFilter.Text.Trim();
        _clb.ItemCheck -= Clb_ItemCheck;
        _visibleDirs.Clear();
        _clb.Items.Clear();

        foreach (var dir in _allDirs)
        {
            if (keyword.Length > 0 && !Path.GetFileName(dir).Contains(keyword, StringComparison.OrdinalIgnoreCase))
                continue;
            _visibleDirs.Add(dir);
            _clb.Items.Add($"{Path.GetFileName(dir)}  —  {dir}", _checkedPaths.Contains(dir));
        }

        _clb.ItemCheck += Clb_ItemCheck;
        UpdateCount();
    }

    private void Clb_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _visibleDirs.Count)
            return;
        var path = _visibleDirs[e.Index];
        if (e.NewValue == CheckState.Checked) _checkedPaths.Add(path);
        else _checkedPaths.Remove(path);
        BeginInvoke(new Action(UpdateCount));
    }

    private void UpdateCount()
    {
        var total = _checkedPaths.Count + (_chkIncludeRoot.Checked ? 1 : 0);
        _lblCount.Text = $"已选 {total} 个文件夹";
    }

    private void Confirm()
    {
        var selected = new List<string>(_checkedPaths);
        if (_chkIncludeRoot.Checked && Directory.Exists(_root))
            selected.Add(_root);

        SelectedFolders = selected
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (SelectedFolders.Count == 0)
        {
            MessageBox.Show(this, "请至少勾选一个文件夹，或勾选「包含当前根目录本身」。",
                "未选择文件夹", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
