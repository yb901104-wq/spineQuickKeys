using KeyMacro.Services;

namespace KeyMacro.Forms;

public class VkWindowManager : Form
{
    private readonly VirtualLayoutSerializer _serializer;
    private readonly DataGridView _dgv;
    private readonly Button _btnAdd;
    private readonly Button _btnClose;
    private int _nameCounter;

    public event Action<VirtualLayoutSerializer.WindowLayoutData, bool>? ToggleWindowVisibility;
    public event Action<string>? DeleteWindowRequested;
    public event Action<string, string>? WindowRenamed; // oldName, newName
    public event Func<string, bool>? QueryWindowVisible; // returns true if window is currently shown

    public VkWindowManager(VirtualLayoutSerializer serializer, int nextNumber)
    {
        _serializer = serializer;
        _nameCounter = nextNumber;

        Text = "虚拟按键管理";
        Icon = IconService.AppIcon;
        Size = new Size(760, 520);
        MinimumSize = new Size(560, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);

        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(0xB8, 0xB8, 0xB8),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 32,
            RowTemplate = { Height = 30 }
        };
        _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0xE1, 0xE7, 0xEA);
        _dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        _dgv.DefaultCellStyle.BackColor = Color.White;
        _dgv.DefaultCellStyle.ForeColor = Color.Black;
        _dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0x0B, 0x78, 0xD0);
        _dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        _dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xF7, 0xF7, 0xF7);

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4)
        };

        _btnClose = MakeButton("关闭", Color.FromArgb(0xF2, 0xF2, 0xF2), Color.Black, new Size(70, 30));
        _btnClose.Click += (_, _) => Close();

        _btnAdd = MakeButton("+ 添加新窗口", Color.FromArgb(0, 120, 215), Color.White, new Size(110, 30));
        _btnAdd.Click += (_, _) => AddWindow();

        bottomPanel.Controls.Add(_btnClose);
        bottomPanel.Controls.Add(_btnAdd);

        _dgv.CellValueChanged += Dgv_CellValueChanged;
        _dgv.CellClick += Dgv_CellClick;

        Controls.Add(_dgv);
        Controls.Add(bottomPanel);
        UiTheme.Apply(this, UiWindowProfile.VkWindowManager);

        Shown += (_, _) => RefreshList();
    }

    private static Button MakeButton(string text, Color backColor, Color foreColor, Size minimumSize)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = minimumSize,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(0x8A, 0x8A, 0x8A);
        button.FlatAppearance.MouseOverBackColor = Lighten(backColor);
        button.FlatAppearance.MouseDownBackColor = Darken(backColor);
        return button;
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

    private void RefreshList()
    {
        _dgv.Columns.Clear();
        _dgv.Rows.Clear();

        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "窗口名称",
            ReadOnly = false,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 40
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "目标",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 25
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "按钮",
            ReadOnly = true,
            Width = 70,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "允许显示",
            Width = 90,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "操作",
            Text = "显示",
            UseColumnTextForButtonValue = false,
            Width = 82,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "删除",
            Text = "×",
            UseColumnTextForButtonValue = true,
            Width = 70,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        ApplyGridColumnStyles();

        var global = _serializer.LoadAll();
        OperationLogger.Info($"VkWindowManager.RefreshList: loaded {global.Windows.Count} windows");
        foreach (var w in global.Windows)
        {
            var target = w.TargetProcessName ?? "";
            bool visible = QueryWindowVisible?.Invoke(w.Name) ?? false;
            var idx = _dgv.Rows.Add(w.Name, target, w.Buttons.Count, w.Enabled, visible ? "隐藏" : "显示", "×");
            _dgv.Rows[idx].Tag = w.Name;
            OperationLogger.Info($"VkWindowManager.RefreshList: row added name={w.Name} buttons={w.Buttons.Count} enabled={w.Enabled}");
        }

    }

    private void ApplyGridColumnStyles()
    {
        if (_dgv.Columns.Count < 6) return;

        _dgv.Columns[0].DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0xFF, 0xFA, 0xE6),
            SelectionBackColor = Color.FromArgb(0xC7, 0x8F, 0x24),
            SelectionForeColor = Color.White
        };

        var readOnlyStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0xF2, 0xF4, 0xF5),
            SelectionBackColor = Color.FromArgb(0x5B, 0x73, 0x84),
            SelectionForeColor = Color.White
        };
        _dgv.Columns[1].DefaultCellStyle = readOnlyStyle;
        _dgv.Columns[2].DefaultCellStyle = readOnlyStyle.Clone();

        _dgv.Columns[3].DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0xE7, 0xF1, 0xFB),
            SelectionBackColor = Color.FromArgb(0x1D, 0x6F, 0xB8),
            SelectionForeColor = Color.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };

        _dgv.Columns[4].DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0xE7, 0xF1, 0xFB),
            SelectionBackColor = Color.FromArgb(0x1D, 0x6F, 0xB8),
            SelectionForeColor = Color.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };

        _dgv.Columns[5].DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0xFA, 0xE6, 0xE6),
            SelectionBackColor = Color.FromArgb(0xB8, 0x42, 0x42),
            SelectionForeColor = Color.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
    }

    private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var oldName = _dgv.Rows[e.RowIndex].Tag?.ToString();
        if (oldName == null) return;
        var global = _serializer.LoadAll();

        if (e.ColumnIndex == 0)
        {
            var newName = _dgv.Rows[e.RowIndex].Cells[0].Value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(newName) || newName == oldName) return;

            var w = global.Windows.Find(x => x.Name == oldName);
            if (w != null)
            {
                w.Name = newName;
                _serializer.SaveAll(global);
                _dgv.Rows[e.RowIndex].Tag = newName;
                WindowRenamed?.Invoke(oldName, newName);
            }
            return;
        }

        if (e.ColumnIndex != 3) return;
        var enabled = _dgv.Rows[e.RowIndex].Cells[3].Value is true;
        var w2 = global.Windows.Find(x => x.Name == oldName);
        if (w2 != null)
        {
            w2.Enabled = enabled;
            _serializer.SaveAll(global);
        }
    }

    private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var name = _dgv.Rows[e.RowIndex].Tag?.ToString();
        if (name == null) return;

        if (e.ColumnIndex == 4)
        {
            var btnCell = _dgv.Rows[e.RowIndex].Cells[4];
            bool currentlyVisible = btnCell.Value?.ToString() == "隐藏";
            bool show = !currentlyVisible;
            OperationLogger.Info($"VkWindowManager.CellClick: toggle name={name} show={show}");
            btnCell.Value = show ? "隐藏" : "显示";
            var global = _serializer.LoadAll();
            var fullData = global.Windows.Find(w => w.Name == name);
            if (fullData == null)
            {
                OperationLogger.Error($"VkWindowManager.CellClick: window '{name}' not found in layout");
                return;
            }
            ToggleWindowVisibility?.Invoke(fullData, show);
        }
        else if (e.ColumnIndex == 5)
        {
            if (MessageBox.Show(this, $"确定删除窗口\"{name}\"及其所有按钮？", "删除确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                OperationLogger.Info($"VkWindowManager.CellClick: delete name={name}");
                DeleteWindowRequested?.Invoke(name);
                RefreshList();
            }
        }
    }

    private void AddWindow()
    {
        var name = $"窗口{_nameCounter++}";
        OperationLogger.Info($"VkWindowManager.AddWindow: creating name={name}");
        var global = _serializer.LoadAll();
        OperationLogger.Info($"VkWindowManager.AddWindow: before add count={global.Windows.Count} windows=[{string.Join(", ", global.Windows.Select(w => w.Name))}]");
        var newWin = new VirtualLayoutSerializer.WindowLayoutData
        {
            Name = name,
            Enabled = true,
            ScaleFactor = 1.0f
        };
        if (global.Windows.Count > 0)
            newWin.SkinPath = global.Windows[0].SkinPath;
        if (string.IsNullOrEmpty(newWin.SkinPath))
            newWin.SkinPath = VirtualLayoutSerializer.LoadEmbeddedSkinPath() ?? "SpineSkin";

        global.Windows.Add(newWin);
        _serializer.SaveAll(global);
        OperationLogger.Info($"VkWindowManager.AddWindow: saved, now triggering show and refresh");
        ToggleWindowVisibility?.Invoke(newWin, true);
        RefreshList();
    }

    public int GetNextNumber() => _nameCounter;
}
