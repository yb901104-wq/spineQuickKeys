using KeyMacro.Models;
using KeyMacro.Services;
using System.Security.Cryptography;

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
    private Button _btnAdd = null!, _btnEdit = null!, _btnDelete = null!, _btnDeleteAll = null!;
    private Button _btnPause = null!, _btnDuplicate = null!;
    private Button _btnSpine = null!, _btnSpineRelease = null!;
    private Button _btnVkOpen = null!, _btnVkClose = null!, _btnVkManage = null!;
    private Button _btnImport = null!, _btnExport = null!;
    private Button _btnReName = null!;
    private Button _btnBatchCopy = null!;
    private Button _btnCli = null!;
    private ToolStripMenuItem? _pauseTrayItem;

    private readonly VirtualLayoutSerializer _vkSerializer = new();
    private List<VirtualKeyWindow> _vkWindows = [];
    private VkWindowManager? _vkManagerWindow;
    private SequenceEditor? _openEditor;
    private bool _refreshingGrid;

    public MainForm()
    {
        Text = "spine宏助手（TANRY） V2.81";
        Icon = IconService.AppIcon;
        ClientSize = new Size(1440, 900);
        MinimumSize = new Size(1100, 650);
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += MainForm_FormClosing;
        Shown += MainForm_Shown;

        BuildUI();
        SetupTray();
        UiTheme.Apply(this, UiWindowProfile.Main);

        _hotkeyService = new HotkeyService(Handle);
        _hotkeyService.HotkeyTriggered += OnHotkeyTriggered;
    }

    private void BuildUI()
    {
        var toolStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(4, 6, 4, 6),
            Margin = new Padding(14, 12, 14, 8),
            BackColor = Color.FromArgb(0xE4, 0xE4, 0xE4)
        };

        _btnAdd = CreateButton("添加", Color.FromArgb(0, 120, 215), Color.White);
        _btnEdit = CreateButton("编辑", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnDelete = CreateButton("删除", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnDeleteAll = CreateButton("删除全部", Color.FromArgb(0xD9, 0x5C, 0x5C), Color.White);
        _btnDuplicate = CreateButton("复制序列", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnPause = CreateButton("暂停全部", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnSpine = CreateButton("Spine热键编辑", Color.FromArgb(0x6B, 0x46, 0xC3), Color.White);
        _btnSpineRelease = CreateButton("释放", Color.FromArgb(0x80, 0x80, 0x80), Color.White);
        _btnSpineRelease.Enabled = false;
        _btnVkOpen = CreateButton("开启虚拟按键", Color.FromArgb(0x00, 0xC8, 0x53), Color.White);
        _btnVkClose = CreateButton("关闭虚拟按键", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnVkManage = CreateButton("管理虚拟按键", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnReName = CreateButton("批量重命名/spine解包整理", Color.FromArgb(0x6B, 0x46, 0xC3), Color.White);
        _btnBatchCopy = CreateButton("批量复制", Color.FromArgb(0x6B, 0x46, 0xC3), Color.White);
        _btnCli = CreateButton("CLI批量合并/导出", Color.FromArgb(0x6B, 0x46, 0xC3), Color.White);
        _btnImport = CreateButton("导入", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnExport = CreateButton("导出", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);

        _btnAdd.Click += (_, _) => AddSequence();
        _btnEdit.Click += (_, _) => EditSequence();
        _btnDelete.Click += (_, _) => DeleteSequence();
        _btnDeleteAll.Click += (_, _) => DeleteAllSequences();
        _btnDuplicate.Click += (_, _) => DuplicateSequence();
        _btnPause.Click += (_, _) => TogglePause();
        _btnSpine.Click += (_, _) => OpenSpineEditor();
        _btnSpineRelease.Click += (_, _) => ReleaseSpineData();
        _btnVkOpen.Click += (_, _) => OpenVirtualKeys();
        _btnVkClose.Click += (_, _) => CloseVirtualKeys();
        _btnVkManage.Click += (_, _) => OpenVkManager();
        _btnReName.Click += (_, _) => { using var f = new ReNameTool.Form1(); f.ShowDialog(this); };
        _btnBatchCopy.Click += (_, _) => { using var f = new BatchCopyWindow(); f.ShowDialog(this); };
        _btnCli.Click += (_, _) => { using var f = new BatchCliWindow(); f.ShowDialog(this); };
        _btnImport.Click += (_, _) => ImportDataBundle();
        _btnExport.Click += (_, _) => ExportDataBundle();

        toolStrip.Controls.AddRange([
            _btnAdd, _btnEdit, _btnDelete, _btnDeleteAll, _btnDuplicate, _btnPause,
            Spacer(),
            _btnSpine, _btnSpineRelease,
            Spacer(),
            _btnVkOpen, _btnVkClose, _btnVkManage,
            Spacer(),
            _btnReName, _btnBatchCopy, _btnCli,
            Spacer(),
            _btnImport, _btnExport
        ]);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var dgvPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 18),
            BackColor = Color.FromArgb(0xD7, 0xD7, 0xD7)
        };

        var listFrame = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0),
            Margin = new Padding(0),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            BackColor = Color.FromArgb(0xD7, 0xD7, 0xD7)
        };
        listFrame.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        listFrame.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var listTitle = new Label
        {
            Text = "宏序列列表",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold),
            BackColor = Color.FromArgb(0xE1, 0xE7, 0xEA),
            ForeColor = Color.Black
        };

        _dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            EditMode = DataGridViewEditMode.EditOnEnter,
            BorderStyle = BorderStyle.FixedSingle,
            BackgroundColor = Color.White,
            ColumnHeadersVisible = true,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 34,
            RowTemplate = { Height = 30 }
        };
        _dgv.GridColor = Color.FromArgb(0xB8, 0xB8, 0xB8);
        _dgv.DefaultCellStyle.BackColor = Color.White;
        _dgv.DefaultCellStyle.ForeColor = Color.Black;
        _dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0x0B, 0x78, 0xD0);
        _dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        _dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(0xF7, 0xF7, 0xF7);
        _dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0xE1, 0xE7, 0xEA);
        _dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        _dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        _dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgv.CellDoubleClick += (_, _) => EditSequence();
        _dgv.CellValueChanged += Dgv_CellValueChanged;
        _dgv.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;
        _dgv.CellEndEdit += Dgv_CellEndEdit;
        _dgv.CellFormatting += Dgv_CellFormatting;
        _dgv.CellClick += Dgv_CellClick;
        listFrame.Controls.Add(listTitle, 0, 0);
        listFrame.Controls.Add(_dgv, 0, 1);
        dgvPanel.Controls.Add(listFrame);
        rootLayout.Controls.Add(toolStrip, 0, 0);
        rootLayout.Controls.Add(dgvPanel, 0, 1);
        Controls.Add(rootLayout);
    }

    private static Button CreateButton(string text, Color backColor, Color foreColor)
    {
        return new Button
        {
            Text = text,
            AutoSize = false,
            Size = new Size(ButtonWidth(text), 30),
            MinimumSize = new Size(ButtonWidth(text), 30),
            Font = new Font("Microsoft YaHei UI", 8f),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            Margin = new Padding(1, 2, 1, 2),
            Padding = new Padding(3, 0, 3, 1),
            Cursor = Cursors.Hand,
            FlatAppearance =
            {
                BorderColor = Color.FromArgb(0xA8, 0xA8, 0xA8),
                MouseOverBackColor = Lighten(backColor),
                MouseDownBackColor = Darken(backColor)
            }
        };
    }

    private static int ButtonWidth(string text)
    {
        return text switch
        {
            "添加" or "编辑" or "删除" or "释放" or "导入" or "导出" => 52,
            "删除全部" or "复制序列" or "暂停全部" or "批量复制" => 70,
            "Spine热键编辑" => 100,
            "开启虚拟按键" or "关闭虚拟按键" => 94,
            "管理虚拟按键" => 100,
            "批量重命名/spine解包整理" => 180,
            "CLI批量合并/导出" => 125,
            _ => 76
        };
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

    private static Control Spacer()
    {
        return new Panel
        {
            AutoSize = false,
            Width = 3,
            Height = 30,
            Margin = new Padding(1, 2, 1, 2),
            BackColor = Color.Gainsboro
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
        UiTheme.Apply(_trayMenu);

        _trayIcon = new NotifyIcon
        {
            Icon = IconService.AppIcon,
            Text = "spine宏助手（TANRY）",
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
        OperationLogger.Info("Application shutting down");
        _hotkeyService.Dispose();
        _trayIcon.Dispose();
        IconService.Dispose();
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        OperationLogger.Info($"Application started, version 2.8");
        LoadSequences();

        // Auto-load spine entries if saved path exists and file is valid
        var spinePath = ConfigService.LoadSpinePath();
        if (!string.IsNullOrEmpty(spinePath) && File.Exists(spinePath))
        {
            try
            {
                var svc = new SpineHotkeyService(spinePath);
                SpineHotkeyEditor.SetLoadedEntries(svc.Load());
                OperationLogger.Info($"MainForm: auto-loaded {SpineHotkeyEditor.LastLoadedEntries?.Count} spine entries from {spinePath}");
            }
            catch (Exception ex)
            {
                OperationLogger.Error($"MainForm: auto-load spine failed: {ex.Message}");
                SpineHotkeyEditor.SetLoadedEntries(null);
                ConfigService.ClearSpinePath();
            }
        }
        UpdateSpineReleaseButton();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        if (_dgv.Parent is Panel p)
        {
            var padX = (int)(12 * DeviceDpi / 96f);
            var padTop = (int)(8 * DeviceDpi / 96f);
            p.Padding = new Padding(padX, padTop, padX, padX);
        }
        RefreshGrid();
    }

    private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0 || _failedHotkeys.Count == 0) return;
        if (_dgv.Columns.Count <= 2) return;
        if (e.ColumnIndex != _dgv.Columns[3].Index) return;

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

        if (e.ColumnIndex == 1)
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
                seq.TargetAppProcessName = Path.GetFileNameWithoutExtension(dialog.FileName);
                seq.TargetAppDisplayName = seq.TargetAppProcessName;
                SaveAndRefresh();
            }
        }
        else if (e.ColumnIndex == 8)
        {
            if (!string.IsNullOrEmpty(seq.TargetAppPath) ||
                !string.IsNullOrEmpty(seq.TargetAppProcessName) ||
                !string.IsNullOrEmpty(seq.TargetAppDisplayName))
            {
                seq.TargetAppPath = "";
                seq.TargetAppProcessName = "";
                seq.TargetAppDisplayName = "";
                SaveAndRefresh();
            }
        }
        else if (e.ColumnIndex == 5 || e.ColumnIndex == 6)
        {
            _dgv.BeginEdit(true);
        }
    }

    private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_refreshingGrid) return;
        if (e.RowIndex < 0 || e.RowIndex >= _sequences.Count) return;
        var seq = _sequences[e.RowIndex];

        switch (e.ColumnIndex)
        {
            case 0: // 启用
                var enabled = _dgv.Rows[e.RowIndex].Cells[0].Value is true;
                if (seq.Enabled != enabled)
                {
                    seq.Enabled = enabled;
                    SaveAndRefresh();
                }
                break;


            case 5: // 间隔(ms)
                int.TryParse(_dgv.Rows[e.RowIndex].Cells[5].Value?.ToString(), out var interval);
                seq.LoopIntervalMs = interval > 0 ? interval : 200;
                SaveAndRefresh();
                break;

            case 6: // 循环次数
                int.TryParse(_dgv.Rows[e.RowIndex].Cells[6].Value?.ToString(), out var count);
                seq.LoopCount = count >= 0 ? count : 0;
                SaveAndRefresh();
                break;
        }
    }

    private void Dgv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_dgv.IsCurrentCellDirty)
            _dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    private void Dgv_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (e.ColumnIndex is 5 or 6)
            _dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
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
        if (_openEditor != null && !_openEditor.IsDisposed)
        {
            _openEditor.BringToFront();
            return;
        }

        var editor = new SequenceEditor();
        _openEditor = editor;
        _btnAdd.Enabled = false;
        _btnEdit.Enabled = false;
        editor.FormClosed += (_, _) =>
        {
            if (editor.DialogResult == DialogResult.OK)
            {
                _sequences.Add(editor.Sequence);
                OperationLogger.Info($"MainForm: added sequence \"{editor.Sequence.Name}\" ({editor.Sequence.Id})");
                SaveAndRefresh();
            }
            _openEditor = null;
            _btnAdd.Enabled = true;
            _btnEdit.Enabled = _sequences.Count > 0;
            editor.Dispose();
        };
        editor.Show(this);
    }

    private void EditSequence()
    {
        if (GetSelectedSequence() is not { } seq) return;

        if (_openEditor != null && !_openEditor.IsDisposed)
        {
            _openEditor.BringToFront();
            return;
        }

        var editor = new SequenceEditor(seq);
        _openEditor = editor;
        _btnAdd.Enabled = false;
        _btnEdit.Enabled = false;
        editor.FormClosed += (_, _) =>
        {
            if (editor.DialogResult == DialogResult.OK)
            {
                OperationLogger.Info($"MainForm: edited sequence \"{seq.Name}\" ({seq.Id})");
                SaveAndRefresh();
            }
            _openEditor = null;
            _btnAdd.Enabled = true;
            _btnEdit.Enabled = _sequences.Count > 0;
            editor.Dispose();
        };
        editor.Show(this);
    }

    private void DeleteSequence()
    {
        if (GetSelectedSequence() is not { } seq) return;
        if (MessageBox.Show($"确定删除序列 \"{seq.Name}\"？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        _sequences.Remove(seq);
        OperationLogger.Info($"MainForm: deleted sequence \"{seq.Name}\" ({seq.Id})");
        SaveAndRefresh();
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
        var lastPath = ConfigService.LoadSpinePath();
        if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath) && SpineHotkeyEditor.LastLoadedEntries != null)
        {
            using var editor = new SpineHotkeyEditor(lastPath);
            editor.ShowDialog();
            UpdateSpineReleaseButton();
            return;
        }

        var spineDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Spine", "settings");
        using var dialog = new OpenFileDialog
        {
            Title = "选择 Spine 热键文件 (*.txt)",
            Filter = "文本文件 (*.txt)|*.txt",
            InitialDirectory = Directory.Exists(spineDir) ? spineDir : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        ConfigService.SaveSpinePath(dialog.FileName);
        using var editorFromDlg = new SpineHotkeyEditor(dialog.FileName);
        editorFromDlg.ShowDialog();
        UpdateSpineReleaseButton();
    }

    private void ReleaseSpineData()
    {
        SpineHotkeyEditor.SetLoadedEntries(null);
        ConfigService.ClearSpinePath();
        _btnSpineRelease.Enabled = false;
        _btnSpineRelease.BackColor = Color.FromArgb(0x80, 0x80, 0x80);
    }

    private void UpdateSpineReleaseButton()
    {
        _btnSpineRelease.Enabled = SpineHotkeyEditor.LastLoadedEntries != null;
        _btnSpineRelease.BackColor = SpineHotkeyEditor.LastLoadedEntries != null
            ? Color.FromArgb(0xD9, 0x5C, 0x5C) : Color.FromArgb(0x80, 0x80, 0x80);
    }

    private void DuplicateSequence()
    {
        if (_dgv.SelectedRows.Count == 0) return;
        if (_dgv.SelectedRows[0].Tag is not string id) return;
        var src = _sequences.Find(s => s.Id == id);
        if (src == null) return;

        var clone = new MacroSequence
        {
            Name = $"{src.Name}_副本",
            TriggerHotkey = "",
            Enabled = src.Enabled,
            LoopIntervalMs = src.LoopIntervalMs,
            LoopCount = src.LoopCount,
            TargetAppPath = src.TargetAppPath,
            TargetAppProcessName = src.TargetAppProcessName,
            TargetAppDisplayName = src.TargetAppDisplayName,
            TriggerVkButtonName = "",
            Steps = src.Steps.Select(s => new MacroStep
            {
                Type = s.Type, Keys = s.Keys, DelayMs = s.DelayMs,
                PressMode = s.PressMode, HoldDurationMs = s.HoldDurationMs
            }).ToList()
        };
        _sequences.Add(clone);
        SaveAndRefresh();
        SyncVkButtonBindings();
        OperationLogger.Info($"MainForm.DuplicateSequence: \"{src.Name}\" -> \"{clone.Name}\"");
    }

    private void DeleteAllSequences()
    {
        if (_sequences.Count == 0) return;
        if (MessageBox.Show($"确定要删除全部 {_sequences.Count} 个序列？此操作不可撤销。",
            "确认删除全部", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        _sequences.Clear();
        OperationLogger.Info("MainForm: deleted all sequences");
        SaveAndRefresh();
    }

    internal void OpenVirtualKeys()
    {
        var global = _vkSerializer.LoadAll();
        OperationLogger.Info($"MainForm.OpenVirtualKeys: total={global.Windows.Count} enabled={global.Windows.Count(w => w.Enabled)}");
        foreach (var data in global.Windows)
        {
            OperationLogger.Info($"MainForm.OpenVirtualKeys: window={data.Name} enabled={data.Enabled}");
            if (!data.Enabled) continue;
            var existing = FindVkWindow(data.Name);
            if (existing != null)
            {
                OperationLogger.Info($"MainForm.OpenVirtualKeys: found existing window {data.Name}, showing");
                existing.Show();
                existing.BringToFront();
            }
            else
            {
                OperationLogger.Info($"MainForm.OpenVirtualKeys: creating new window {data.Name}");
                CreateVkWindow(data);
            }
        }
    }

    private void CloseVirtualKeys()
    {
        OperationLogger.Info("MainForm: hiding all VK windows");
        foreach (var w in _vkWindows)
        {
            if (!w.IsDisposed)
                w.Hide();
        }
    }

    private VirtualKeyWindow CreateVkWindow(VirtualLayoutSerializer.WindowLayoutData data)
    {
        var win = new VirtualKeyWindow(_vkSerializer, data, _sequences, SaveAndRefresh);
        win.DeleteRequested += OnVkWindowDeleted;
        _vkWindows.Add(win);
        win.Show();
        return win;
    }

    private void OnVkWindowDeleted(VirtualKeyWindow win)
    {
        var name = win.WindowData.Name;
        OperationLogger.Info($"MainForm.OnVkWindowDeleted: name={name}");
        _vkWindows.Remove(win);
        var global = _vkSerializer.LoadAll();
        int beforeCount = global.Windows.Count;
        global.Windows.RemoveAll(w => w.Name == name);
        OperationLogger.Info($"MainForm.OnVkWindowDeleted: before={beforeCount} after={global.Windows.Count}");
        _vkSerializer.SaveAll(global);
    }

    internal void OpenVkManager()
    {
        if (_vkManagerWindow != null && !_vkManagerWindow.IsDisposed)
        {
            _vkManagerWindow.BringToFront();
            return;
        }

        int nextNum = 1;
        var global = _vkSerializer.LoadAll();
        foreach (var w in global.Windows)
        {
            if (w.Name.StartsWith("窗口") && int.TryParse(w.Name[2..], out var n))
                nextNum = Math.Max(nextNum, n + 1);
        }

        _vkManagerWindow = new VkWindowManager(_vkSerializer, nextNum);
        _vkManagerWindow.ToggleWindowVisibility += (data, show) =>
        {
            OperationLogger.Info($"MainForm.ToggleWindowVisibility: name={data.Name} show={show}");
            var existing = FindVkWindow(data.Name);
            if (existing != null)
            {
                if (show) { existing.ReloadSkin(); existing.Show(); existing.BringToFront(); }
                else existing.Hide();
                OperationLogger.Info($"MainForm.ToggleWindowVisibility: {(show ? "shown" : "hidden")} existing window");
            }
            else if (show)
            {
                OperationLogger.Info($"MainForm.ToggleWindowVisibility: creating new window");
                CreateVkWindow(data);
            }
        };
        _vkManagerWindow.QueryWindowVisible += name =>
        {
            var win = _vkWindows.FirstOrDefault(w => !w.IsDisposed && w.Text.Contains(name));
            return win != null && win.Visible;
        };
        _vkManagerWindow.DeleteWindowRequested += name =>
        {
            OperationLogger.Info($"MainForm.DeleteWindowRequested: name={name}");
            var win = FindVkWindow(name);
            if (win != null)
            {
                win.Close();
                win.Dispose();
                _vkWindows.Remove(win);
                OperationLogger.Info($"MainForm.DeleteWindowRequested: closed and disposed window instance");
            }
            var g = _vkSerializer.LoadAll();
            int beforeCount = g.Windows.Count;
            g.Windows.RemoveAll(w => w.Name == name);
            OperationLogger.Info($"MainForm.DeleteWindowRequested: removed from global data, before={beforeCount} after={g.Windows.Count}");
            _vkSerializer.SaveAll(g);
        };
        _vkManagerWindow.WindowRenamed += (oldName, newName) =>
        {
            OperationLogger.Info($"MainForm.WindowRenamed: \"{oldName}\" -> \"{newName}\"");
            var win = FindVkWindow(oldName);
            if (win != null)
            {
                win.WindowData.Name = newName;
                win.UpdateWindowTitle();
            }
        };
        _vkManagerWindow.FormClosed += (_, _) => _vkManagerWindow = null;
        _vkManagerWindow.Show(this);
    }

    private VirtualKeyWindow? FindVkWindow(string name)
    {
        return _vkWindows.FirstOrDefault(w =>
        {
            if (w.IsDisposed) return false;
            var title = w.Text;
            // Match by name in title: "名称 (N)" or "[target] 名称 (N)"
            var parenIdx = title.LastIndexOf('(');
            var titleName = parenIdx > 0 ? title[..parenIdx].TrimEnd() : title;
            if (titleName.Contains(']'))
                titleName = titleName[(titleName.LastIndexOf(']') + 1)..].Trim();
            return titleName == name;
        });
    }

    internal static void RequestOpenVirtualKeys()
    {
        var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
        main?.OpenVirtualKeys();
    }

    private void ExportDataBundle()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "导出数据",
            Filter = "KeyMacro 数据包 (*.kmp)|*.kmp",
            DefaultExt = "kmp"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        var bundle = new DataBundle();
        bundle.Sequences = [.. _sequences];
        bundle.VkDataList = [.. _vkSerializer.LoadAll().Windows];

        // Read spine hotkeys from saved TXT path, not from editor window
        var spinePath = ConfigService.LoadSpinePath();
        if (!string.IsNullOrEmpty(spinePath) && File.Exists(spinePath))
        {
            bundle.SpineHotkeyRawText = File.ReadAllText(spinePath);
            bundle.SpineHotkeyFileName = Path.GetFileName(spinePath);
            bundle.SpineHotkeyHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(spinePath)));
            bundle.SpineHotkeys = new SpineHotkeyService(spinePath).Load();
            bundle.SpineHotkeyNames = bundle.SpineHotkeys
                .Where(e => !e.Name.StartsWith("---"))
                .Select(e => e.Name)
                .ToList();
        }

        new DataBundleService().Export(dialog.FileName, bundle);
        MessageBox.Show($"数据已导出到：\n{dialog.FileName}", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ImportDataBundle()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "导入数据",
            Filter = "KeyMacro 数据包 (*.kmp)|*.kmp",
            DefaultExt = "kmp"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        var bundle = new DataBundleService().Import(dialog.FileName);
        if (bundle == null)
        {
            MessageBox.Show("文件格式无效或读取失败。", "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Backward compat: if VkDataList is null, migrate from old VkData
        if (bundle.VkDataList == null && bundle.VkData != null)
            bundle.VkDataList = [bundle.VkData];

        // ── 1. Spine hotkey bindings (raw restore or key-aligned merge) ──
        var spinePath = ConfigService.LoadSpinePath();
        if (bundle.SpineHotkeys?.Count > 0 && !string.IsNullOrEmpty(spinePath) && File.Exists(spinePath))
        {
            var sameStructure = IsSameSpineStructure(bundle, spinePath);
            var recommendation = sameStructure ? "完整覆盖恢复" : "按名称合并迁移";
            var prompt = sameStructure
                ? "检测到目标热键文件与导入包结构基本一致，推荐完整覆盖恢复。\n\n是：完整覆盖恢复\n否：按名称合并迁移\n取消：跳过 Spine 快捷键导入"
                : "检测到目标热键文件与导入包结构可能不同，推荐按名称合并迁移。\n\n是：按名称合并迁移\n否：完整覆盖恢复\n取消：跳过 Spine 快捷键导入";
            var choice = MessageBox.Show(prompt, $"导入 Spine 快捷键（推荐：{recommendation}）",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (choice != DialogResult.Cancel)
            {
                var useFullRestore = sameStructure
                    ? choice == DialogResult.Yes
                    : choice == DialogResult.No;

                if (useFullRestore && !string.IsNullOrEmpty(bundle.SpineHotkeyRawText))
                {
                    File.WriteAllText(spinePath, bundle.SpineHotkeyRawText);
                    var svc = new SpineHotkeyService(spinePath);
                    SpineHotkeyEditor.SetLoadedEntries(svc.Load());
                    MessageBox.Show("Spine 快捷键已按原文完整恢复。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var result = MergeSpineHotkeysByName(bundle, spinePath);
                    if (result.Changed)
                    {
                        var svc = new SpineHotkeyService(spinePath);
                        SpineHotkeyEditor.SetLoadedEntries(svc.Load());
                        var skipText = result.SkippedRiskNames.Count > 0
                            ? $"\n\n已跳过重复/近似重复风险项：\n{string.Join("\n", result.SkippedRiskNames)}"
                            : "";
                        MessageBox.Show($"Spine 快捷键绑定已按名称合并导入。{skipText}", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("没有匹配的快捷键项可导入。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        else if (bundle.SpineHotkeys?.Count > 0)
        {
            MessageBox.Show("Spine 快捷键绑定：未找到 Spine 热键文件，跳过。", "导入", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── 2. Spine key descriptions (per-hotkey-file annotations) ──
        if (bundle.SpineHotkeys?.Count > 0)
        {
            if (MessageBox.Show("是否导入按键功能说明？（将按按键名对位替换翻译内容）",
                "导入", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var importNoteMap = bundle.SpineHotkeys
                    .Where(e => !string.IsNullOrEmpty(e.ChineseNote) && !e.Name.StartsWith("---"))
                    .ToDictionary(e => e.Name, e => e.ChineseNote!, StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(spinePath) && File.Exists(spinePath))
                {
                    if (importNoteMap.Count > 0)
                    { 
                        SpineHotkeyService.SaveAnnotations(spinePath, importNoteMap);
                        MessageBox.Show("按键功能说明已导入。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("没有匹配的功能说明项可导入。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Spine 热键文件不存在，功能说明导入跳过。", "导入", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // ── 3. Sequences ──
        var sequencesImported = false;
        if (bundle.Sequences?.Count > 0)
        {
            if (MessageBox.Show($"是否导入所有序列？（共 {bundle.Sequences.Count} 个序列）", "导入", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _sequences = [.. bundle.Sequences];
                sequencesImported = true;
                _config.Save(_sequences);
                _failedHotkeys = [.. _hotkeyService.RegisterAll(_sequences)];
                SyncVkButtonBindings();
                RefreshGrid();
            }
        }
        else
        {
            MessageBox.Show("序列设置：文件中无相关数据，跳过。", "导入", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── 4. VK windows (per-window confirm) ──
        if (bundle.VkDataList?.Count > 0)
        {
            var global = _vkSerializer.LoadAll();
            int importedCount = 0;
            var importedWindows = new List<VirtualLayoutSerializer.WindowLayoutData>();
            foreach (var win in bundle.VkDataList)
            {
                var originalName = win.Name;
                var newName = GetUniqueWindowName(global.Windows, originalName);
                var renamed = !newName.Equals(originalName, StringComparison.OrdinalIgnoreCase);
                var prompt = renamed
                    ? $"窗口 \"{originalName}\" 已存在，将以 \"{newName}\" 新增导入。是否继续？（共 {win.Buttons.Count} 个按钮）"
                    : $"是否导入窗口 \"{newName}\"？（共 {win.Buttons.Count} 个按钮）";

                if (MessageBox.Show(prompt, "导入虚拟按键", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    win.Name = newName;
                    win.Enabled = true;
                    global.Windows.Add(win);
                    importedWindows.Add(win);
                    if (renamed && sequencesImported)
                        RenameImportedVkBindings(originalName, newName);
                    importedCount++;
                }
            }
            if (importedCount > 0)
            {
                _vkSerializer.SaveAll(global);
                foreach (var winData in importedWindows.Where(w => w.Enabled))
                    CreateVkWindow(winData);
                if (sequencesImported)
                    SaveAndRefresh();
                MessageBox.Show($"已导入 {importedCount} 个虚拟按键窗口。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        else
        {
            MessageBox.Show("虚拟按键布局：文件中无相关数据，跳过。", "导入", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static string GetUniqueWindowName(List<VirtualLayoutSerializer.WindowLayoutData> windows, string preferredName)
    {
        var baseName = string.IsNullOrWhiteSpace(preferredName) ? "导入窗口" : preferredName.Trim();
        if (!windows.Any(w => w.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase)))
            return baseName;

        var index = 1;
        string candidate;
        do
        {
            candidate = $"{baseName}_导入{index++}";
        }
        while (windows.Any(w => w.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)));
        return candidate;
    }

    private void RenameImportedVkBindings(string oldWindowName, string newWindowName)
    {
        var oldPrefix = oldWindowName + "/";
        var newPrefix = newWindowName + "/";
        foreach (var seq in _sequences)
        {
            if (seq.TriggerVkButtonName.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                seq.TriggerVkButtonName = newPrefix + seq.TriggerVkButtonName[oldPrefix.Length..];
        }
    }

    private static bool IsSameSpineStructure(DataBundle bundle, string targetPath)
    {
        var sourceNames = bundle.SpineHotkeyNames;
        if (sourceNames == null || sourceNames.Count == 0)
            sourceNames = bundle.SpineHotkeys?
                .Where(e => !e.Name.StartsWith("---"))
                .Select(e => e.Name)
                .ToList();
        if (sourceNames == null || sourceNames.Count == 0) return false;

        var targetNames = ExtractSpineHotkeyNames(File.ReadAllLines(targetPath));
        if (targetNames.Count == 0 || sourceNames.Count != targetNames.Count) return false;

        for (int i = 0; i < sourceNames.Count; i++)
        {
            if (!string.Equals(sourceNames[i], targetNames[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static SpineMergeResult MergeSpineHotkeysByName(DataBundle bundle, string targetPath)
    {
        var importEntries = bundle.SpineHotkeys?
            .Where(e => !e.Name.StartsWith("---"))
            .ToList() ?? [];

        var importRiskNames = FindNormalizedDuplicateNames(importEntries.Select(e => e.Name));
        var lines = File.ReadAllLines(targetPath).ToList();
        var targetNames = ExtractSpineHotkeyNames(lines);
        var targetRiskNames = FindNormalizedDuplicateNames(targetNames);
        var riskNames = new HashSet<string>(importRiskNames.Concat(targetRiskNames), StringComparer.OrdinalIgnoreCase);

        var importMap = importEntries
            .Where(e => !riskNames.Contains(e.Name))
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Keys, StringComparer.OrdinalIgnoreCase);

        var changed = false;
        var skipped = new HashSet<string>(riskNames, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < lines.Count; i++)
        {
            var raw = lines[i].TrimEnd('\r', '\n');
            var colonIdx = raw.IndexOf(':');
            if (colonIdx <= 0) continue;

            var name = raw[..colonIdx].TrimEnd();
            if (riskNames.Contains(name)) continue;

            if (importMap.TryGetValue(name, out var newKeys))
            {
                lines[i] = $"{name}: {newKeys}";
                changed = true;
            }
        }

        if (changed)
            File.WriteAllLines(targetPath, lines);

        return new SpineMergeResult(changed, [.. skipped]);
    }

    private static List<string> ExtractSpineHotkeyNames(IEnumerable<string> lines)
    {
        var names = new List<string>();
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r', '\n');
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("---")) continue;
            var colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
                names.Add(line[..colonIdx].TrimEnd());
        }
        return names;
    }

    private static HashSet<string> FindNormalizedDuplicateNames(IEnumerable<string> names)
    {
        var byNormalized = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var normalized = NormalizeSpineName(name);
            if (!byNormalized.TryGetValue(normalized, out var list))
            {
                list = [];
                byNormalized[normalized] = list;
            }
            list.Add(name);
        }

        return byNormalized.Values
            .Where(list => list.Count > 1)
            .SelectMany(list => list)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeSpineName(string name)
    {
        return string.Join(" ", name.Normalize().Trim()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }

    private sealed record SpineMergeResult(bool Changed, List<string> SkippedRiskNames);

    private void ExitApp()
    {
        OperationLogger.Info("Application exiting via tray menu");
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
        SyncVkButtonBindings();
        RefreshGrid();
    }

    private void SyncVkButtonBindings()
    {
        // Match buttons to sequences by composite key "窗口名/按钮名" or plain name.
        var global = _vkSerializer.LoadAll();
        int matchedCount = 0;
        int totalButtons = 0;

        // First pass: clear BindActionId for buttons whose sequence no longer has TriggerVkButtonName
        foreach (var winData in global.Windows)
            foreach (var vbtn in winData.Buttons)
            {
                var seq = _sequences.FirstOrDefault(s => s.Id == vbtn.BindActionId);
                if (seq != null && string.IsNullOrWhiteSpace(seq.TriggerVkButtonName))
                    vbtn.BindActionId = null;
            }

        // Second pass: match by composite key "窗口名/按钮名" then fallback to plain name
        foreach (var winData in global.Windows)
        {
            foreach (var vbtn in winData.Buttons)
            {
                totalButtons++;
                var composite = $"{winData.Name}/{vbtn.Name}";
                var seq = _sequences.FirstOrDefault(s => s.TriggerVkButtonName?.Trim() == composite)
                    ?? _sequences.FirstOrDefault(s => s.TriggerVkButtonName?.Trim() == vbtn.Name);
                if (seq != null)
                {
                    vbtn.BindActionId = seq.Id;
                    matchedCount++;
                }
            }
        }
        _vkSerializer.SaveAll(global);
        OperationLogger.Info($"[DIAG] VKSync: matched {matchedCount}/{totalButtons} buttons across {global.Windows.Count} windows, sequences={_sequences.Count}");
        foreach (var win in _vkWindows)
            win.RefreshBindingsFromSerializer();
    }

    private void RefreshGrid()
    {
        _refreshingGrid = true;
        _dgv.ColumnHeadersVisible = true;
        _dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _dgv.ColumnHeadersHeight = (int)(34 * DeviceDpi / 96f);
        _dgv.Columns.Clear();
        float ds = DeviceDpi / 96f;

        _dgv.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "启用",
            Width = (int)(72 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "选择",
            Text = "选择",
            UseColumnTextForButtonValue = true,
            Width = (int)(96 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = true
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "名称",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 22,
            MinimumWidth = (int)(140 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "触发快捷键",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 22,
            MinimumWidth = (int)(120 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "目标软件",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 18,
            MinimumWidth = (int)(120 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "间隔(ms)",
            Width = (int)(150 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "循环(次)",
            Width = (int)(150 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "步骤",
            ReadOnly = true,
            Width = (int)(88 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "清除",
            Text = "清除",
            UseColumnTextForButtonValue = true,
            Width = (int)(104 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = true
        });
        ApplyMainGridColumnStyles();

        _dgv.Rows.Clear();
        foreach (var seq in _sequences)
        {
            var appName = GetTargetAppDisplay(seq);
            var idx = _dgv.Rows.Add(
                seq.Enabled, "选择", seq.Name,
                !string.IsNullOrEmpty(seq.TriggerVkButtonName) ? $"虚拟按键({seq.TriggerVkButtonName})" : seq.TriggerHotkey,
                appName, seq.LoopIntervalMs.ToString(), seq.LoopCount.ToString(), seq.Steps.Count, "清除");
            _dgv.Rows[idx].Tag = seq.Id;
        }

        if (_sequences.Count > 0)
            _dgv.Rows[0].Selected = true;

        _dgv.ColumnHeadersVisible = true;
        _dgv.ColumnHeadersHeight = (int)(34 * DeviceDpi / 96f);

        _btnEdit.Enabled = _sequences.Count > 0;
        _btnDelete.Enabled = _sequences.Count > 0;
        _refreshingGrid = false;
    }

    private void ApplyMainGridColumnStyles()
    {
        if (_dgv.Columns.Count < 9) return;

        var editableStyle = new DataGridViewCellStyle
        {
            BackColor = UiTheme.Input,
            ForeColor = UiTheme.Text,
            SelectionBackColor = Color.FromArgb(0x6F, 0x54, 0x24),
            SelectionForeColor = Color.White
        };
        _dgv.Columns[5].DefaultCellStyle = editableStyle;
        _dgv.Columns[6].DefaultCellStyle = editableStyle.Clone();

        var chooseStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0x2D, 0x42, 0x49),
            ForeColor = UiTheme.Text,
            SelectionBackColor = Color.FromArgb(0x2E, 0x68, 0x7A),
            SelectionForeColor = Color.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _dgv.Columns[1].DefaultCellStyle = chooseStyle;

        var clearStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(0x47, 0x34, 0x34),
            ForeColor = UiTheme.Text,
            SelectionBackColor = Color.FromArgb(0x7A, 0x48, 0x48),
            SelectionForeColor = Color.White,
            Alignment = DataGridViewContentAlignment.MiddleCenter
        };
        _dgv.Columns[8].DefaultCellStyle = clearStyle;
    }

    private static string GetTargetAppDisplay(MacroSequence seq)
    {
        if (!string.IsNullOrWhiteSpace(seq.TargetAppDisplayName)) return seq.TargetAppDisplayName;
        if (!string.IsNullOrWhiteSpace(seq.TargetAppProcessName)) return seq.TargetAppProcessName;
        if (!string.IsNullOrWhiteSpace(seq.TargetAppPath)) return Path.GetFileNameWithoutExtension(seq.TargetAppPath);
        return "全局";
    }

    private void OnHotkeyTriggered(string sequenceId)
    {
        var seq = _sequences.Find(s => s.Id == sequenceId);
        if (seq == null || !seq.Enabled) return;
        if (_player.IsPlaying)
            _player.Stop();
        else
            _ = _player.Play(seq);
    }

}
