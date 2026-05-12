using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public partial class MainForm : Form
{
    private readonly ConfigService _config = new();
    private readonly HotkeyService _hotkeyService;
    private readonly MacroPlayer _player = new();
    private NotifyIcon _trayIcon = null!;
    private ContextMenuStrip _trayMenu = null!;
    private List<MacroSequence> _sequences = [];
    private HashSet<string> _failedHotkeys = [];

    private DataGridView _dgv = null!;
    private Button _btnAdd = null!, _btnEdit = null!, _btnDelete = null!;
    private Button _btnTest = null!, _btnPause = null!;
    private Button _btnSpine = null!, _btnDeleteAll = null!;
    private ToolStripMenuItem? _pauseTrayItem;

    public MainForm()
    {
        Text = "快捷键助手 V1.31";
        Size = new Size(900, 600);
        MinimumSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += MainForm_FormClosing;
        Shown += MainForm_Shown;

        BuildUI();
        SetupTray();

        _hotkeyService = new HotkeyService(Handle);
        _hotkeyService.HotkeyTriggered += OnHotkeyTriggered;
    }

    private void BuildUI()
    {
        var toolStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(8, 8, 8, 0)
        };

        _btnAdd = CreateButton("添加", Color.FromArgb(0, 120, 215), Color.White);
        _btnEdit = CreateButton("编辑", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnDelete = CreateButton("删除", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnTest = CreateButton("测试", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnPause = CreateButton("暂停全部", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnSpine = CreateButton("Spine热键编辑", Color.FromArgb(0x6B, 0x46, 0xC3), Color.White);
        _btnDeleteAll = CreateButton("删除全部", Color.FromArgb(0xD9, 0x5C, 0x5C), Color.White);

        _btnAdd.Click += (_, _) => AddSequence();
        _btnEdit.Click += (_, _) => EditSequence();
        _btnDelete.Click += (_, _) => DeleteSequence();
        _btnTest.Click += (_, _) => TestSequence();
        _btnPause.Click += (_, _) => TogglePause();
        _btnSpine.Click += (_, _) => OpenSpineEditor();
        _btnDeleteAll.Click += (_, _) => DeleteAllSequences();

        toolStrip.Controls.AddRange([_btnAdd, _btnEdit, _btnDelete, _btnTest, _btnPause, _btnSpine, _btnDeleteAll]);
        Controls.Add(toolStrip);

        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true
        };
        _dgv.CellDoubleClick += (_, _) => EditSequence();
        _dgv.CellValueChanged += Dgv_CellValueChanged;
        _dgv.CellFormatting += Dgv_CellFormatting;
        _dgv.CellClick += Dgv_CellClick;
        Controls.Add(_dgv);
    }

    private static Button CreateButton(string text, Color backColor, Color foreColor)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(80, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatAppearance = { BorderColor = Color.Gainsboro }
        };
    }

    private void SetupTray()
    {
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("打开主窗口", null, (_, _) => ShowWindow());
        _trayMenu.Items.Add(new ToolStripSeparator());
        _pauseTrayItem = new ToolStripMenuItem("暂停全部");
        _pauseTrayItem.Click += (_, _) => TogglePause();
        _trayMenu.Items.Add(_pauseTrayItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add("退出", null, (_, _) => ExitApp());

        _trayIcon = new NotifyIcon
        {
            Icon = CreateAppIcon(),
            Text = "快捷键助手",
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        _hotkeyService?.HandleWindowMessage(ref m);
    }

    // ── Events ──

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        _hotkeyService.Dispose();
        _trayIcon.Dispose();
    }

    private void MainForm_Shown(object? sender, EventArgs e) => LoadSequences();

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0 || _failedHotkeys.Count == 0) return;
        if (_dgv.Columns.Count <= 2) return;
        if (e.ColumnIndex != _dgv.Columns[2].Index) return;

        var value = _dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
        if (!string.IsNullOrEmpty(value) && _failedHotkeys.Contains(value))
        {
            e.CellStyle.ForeColor = Color.Red;
            e.CellStyle.Font = new Font(_dgv.Font, FontStyle.Bold);
        }
    }

    private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _sequences.Count) return;
        var seq = _sequences[e.RowIndex];

        if (e.ColumnIndex == 6)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "选择目标程序",
                Filter = "可执行文件 (*.exe)|*.exe",
                CheckFileExists = true
            };
            if (!string.IsNullOrEmpty(seq.TargetAppPath))
                dialog.InitialDirectory = Path.GetDirectoryName(seq.TargetAppPath);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                seq.TargetAppPath = dialog.FileName;
                SaveAndRefresh();
            }
        }
        else if (e.ColumnIndex == 7)
        {
            if (!string.IsNullOrEmpty(seq.TargetAppPath))
            {
                seq.TargetAppPath = "";
                SaveAndRefresh();
            }
        }
    }

    private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != 0 || e.RowIndex < 0 || e.RowIndex >= _sequences.Count) return;
        var seq = _sequences[e.RowIndex];
        var newVal = _dgv.Rows[e.RowIndex].Cells[0].Value;
        var enabled = newVal is true;
        if (seq.Enabled != enabled)
        {
            seq.Enabled = enabled;
            _config.Save(_sequences);
            _hotkeyService.RegisterAll(_sequences);
        }
    }

    private void LoadSequences()
    {
        _sequences = _config.Load();
        _failedHotkeys = [.. _hotkeyService.RegisterAll(_sequences)];
        RefreshGrid();
        if (_failedHotkeys.Count > 0)
        {
            Show();
            WindowState = FormWindowState.Normal;
            MessageBox.Show(
                $"以下快捷键注册失败，可能已被其他程序占用：\n{string.Join("\n", _failedHotkeys)}",
                "热键注册失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddSequence()
    {
        using var editor = new SequenceEditor();
        if (editor.ShowDialog() == DialogResult.OK)
        {
            _sequences.Add(editor.Sequence);
            SaveAndRefresh();
        }
    }

    private void EditSequence()
    {
        if (GetSelectedSequence() is not { } seq) return;
        using var editor = new SequenceEditor(seq);
        if (editor.ShowDialog() == DialogResult.OK)
            SaveAndRefresh();
    }

    private void DeleteSequence()
    {
        if (GetSelectedSequence() is not { } seq) return;
        if (MessageBox.Show($"确定删除序列 \"{seq.Name}\"？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        _sequences.Remove(seq);
        SaveAndRefresh();
    }

    private void TestSequence()
    {
        if (GetSelectedSequence() is not { } seq) return;
        if (_player.IsPlaying)
        {
            _player.Stop();
            _btnTest.Text = "测试";
            return;
        }
        Hide();
        _ = Task.Run(async () =>
        {
            await _player.Play(seq);
            BeginInvoke(() => _btnTest.Text = "测试");
        });
        _btnTest.Text = "停止";
    }

    private void TogglePause()
    {
        var paused = !_hotkeyService.IsPaused;
        _hotkeyService.SetPaused(paused);
        _btnPause.Text = paused ? "恢复全部" : "暂停全部";
        if (_pauseTrayItem != null)
            _pauseTrayItem.Text = paused ? "恢复全部" : "暂停全部";
    }

    private void OpenSpineEditor()
    {
        var spineDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Spine", "settings");
        using var dialog = new OpenFileDialog
        {
            Title = "选择 Spine 热键文件 (*.txt)",
            Filter = "文本文件 (*.txt)|*.txt",
            InitialDirectory = Directory.Exists(spineDir) ? spineDir : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        using var editor = new SpineHotkeyEditor(dialog.FileName);
        editor.ShowDialog();
    }

    private void DeleteAllSequences()
    {
        if (_sequences.Count == 0) return;
        if (MessageBox.Show($"确定要删除全部 {_sequences.Count} 个序列？此操作不可撤销。",
            "确认删除全部", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        _sequences.Clear();
        SaveAndRefresh();
    }

    private void ExitApp()
    {
        _hotkeyService.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // ── Helpers ──

    private MacroSequence? GetSelectedSequence()
    {
        if (_dgv.SelectedRows.Count == 0) return null;
        var index = _dgv.SelectedRows[0].Index;
        return index >= 0 && index < _sequences.Count ? _sequences[index] : null;
    }

    private void SaveAndRefresh()
    {
        _config.Save(_sequences);
        _failedHotkeys = [.. _hotkeyService.RegisterAll(_sequences)];
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        _dgv.Columns.Clear();

        _dgv.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "启用",
            DataPropertyName = "Enabled",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "序列名称" });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "触发快捷键" });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "目标软件" });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "步骤数" });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "循环",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "选择",
            Text = "...",
            UseColumnTextForButtonValue = true,
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "清除",
            Text = "✕",
            UseColumnTextForButtonValue = true,
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });

        _dgv.Rows.Clear();
        foreach (var seq in _sequences)
        {
            var appName = string.IsNullOrEmpty(seq.TargetAppPath)
                ? "全局"
                : Path.GetFileName(seq.TargetAppPath);
            var idx = _dgv.Rows.Add(seq.Enabled, seq.Name, seq.TriggerHotkey, appName, seq.Steps.Count, seq.Loop ? "✓" : "");
            _dgv.Rows[idx].Tag = seq.Id;
        }

        if (_sequences.Count > 0)
            _dgv.Rows[0].Selected = true;

        _btnEdit.Enabled = _sequences.Count > 0;
        _btnDelete.Enabled = _sequences.Count > 0;
        _btnTest.Enabled = _sequences.Count > 0;
    }

    private void OnHotkeyTriggered(string sequenceId)
    {
        var seq = _sequences.Find(s => s.Id == sequenceId);
        if (seq != null && seq.Enabled && !_player.IsPlaying)
            _ = _player.Play(seq);
    }

    private static Icon CreateAppIcon()
    {
        using var bmp = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillRectangle(brush, 0, 0, 16, 16);
        g.DrawString("K", new Font("Segoe UI", 9, FontStyle.Bold), Brushes.White, 3, 2);
        return Icon.FromHandle(bmp.GetHicon());
    }
}
