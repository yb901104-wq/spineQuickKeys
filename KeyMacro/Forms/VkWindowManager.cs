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

    public VkWindowManager(VirtualLayoutSerializer serializer, int nextNumber)
    {
        _serializer = serializer;
        _nameCounter = nextNumber;

        Text = "虚拟按键管理";
        Size = new Size(600, 400);
        MinimumSize = new Size(400, 250);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        ShowInTaskbar = false;

        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = false
        };

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnClose = new Button { Text = "关闭", AutoSize = true, MinimumSize = new Size(70, 30), FlatStyle = FlatStyle.Flat };
        _btnClose.Click += (_, _) => Close();

        _btnAdd = new Button
        {
            Text = "+ 添加新窗口",
            AutoSize = true,
            MinimumSize = new Size(100, 30),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnAdd.Click += (_, _) => AddWindow();

        bottomPanel.Controls.Add(_btnClose);
        bottomPanel.Controls.Add(_btnAdd);

        _dgv.CellValueChanged += Dgv_CellValueChanged;
        _dgv.CellClick += Dgv_CellClick;

        Controls.Add(_dgv);
        Controls.Add(bottomPanel);

        Shown += (_, _) => RefreshList();
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
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 8
        });
        _dgv.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "允许显示",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 10
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "操作",
            Text = "显示",
            UseColumnTextForButtonValue = false,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 10
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "删除",
            Text = "×",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 7
        });

        var global = _serializer.LoadAll();
        OperationLogger.Info($"VkWindowManager.RefreshList: loaded {global.Windows.Count} windows");
        foreach (var w in global.Windows)
        {
            var target = w.TargetProcessName ?? "";
            var idx = _dgv.Rows.Add(w.Name, target, w.Buttons.Count, w.Enabled, "显示/隐藏", "×");
            _dgv.Rows[idx].Tag = w.Name;
            OperationLogger.Info($"VkWindowManager.RefreshList: row added name={w.Name} buttons={w.Buttons.Count} enabled={w.Enabled}");
        }

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
            ToggleWindowVisibility?.Invoke(
                new VirtualLayoutSerializer.WindowLayoutData { Name = name },
                show);
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

        global.Windows.Add(newWin);
        _serializer.SaveAll(global);
        OperationLogger.Info($"VkWindowManager.AddWindow: saved, now triggering show and refresh");
        ToggleWindowVisibility?.Invoke(newWin, true);
        RefreshList();
    }

    public int GetNextNumber() => _nameCounter;
}
