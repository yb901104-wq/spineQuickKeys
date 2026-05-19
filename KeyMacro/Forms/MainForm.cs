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
    private Button _btnTest = null!, _btnPause = null!, _btnDuplicate = null!;
    private Button _btnSpine = null!, _btnSpineRelease = null!, _btnDeleteAll = null!;
    private Button _btnVkOpen = null!, _btnVkClose = null!, _btnVkManage = null!;
    private Button _btnImport = null!, _btnExport = null!;
    private ToolStripMenuItem? _pauseTrayItem;

    private readonly VirtualLayoutSerializer _vkSerializer = new();
    private List<VirtualKeyWindow> _vkWindows = [];
    private VkWindowManager? _vkManagerWindow;
    private SequenceEditor? _openEditor;

    public MainForm()
    {
        Text = "spine宏助手（TANRY） V2.16";
        Icon = IconService.AppIcon;
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
        _btnDuplicate = CreateButton("复制序列", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnPause = CreateButton("暂停全部", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnSpine = CreateButton("Spine热键编辑", Color.FromArgb(0x6B, 0x46, 0xC3), Color.White);
        _btnSpineRelease = CreateButton("释放", Color.FromArgb(0x80, 0x80, 0x80), Color.White);
        _btnSpineRelease.Enabled = false;
        _btnDeleteAll = CreateButton("删除全部", Color.FromArgb(0xD9, 0x5C, 0x5C), Color.White);
        _btnVkOpen = CreateButton("开启虚拟按键", Color.FromArgb(0x00, 0xC8, 0x53), Color.White);
        _btnVkClose = CreateButton("关闭虚拟按键", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnVkManage = CreateButton("管理虚拟按键", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnImport = CreateButton("导入", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);
        _btnExport = CreateButton("导出", Color.FromArgb(0xF0, 0xF0, 0xF0), Color.Black);

        _btnAdd.Click += (_, _) => AddSequence();
        _btnEdit.Click += (_, _) => EditSequence();
        _btnDelete.Click += (_, _) => DeleteSequence();
        _btnTest.Click += (_, _) => TestSequence();
        _btnDuplicate.Click += (_, _) => DuplicateSequence();
        _btnPause.Click += (_, _) => TogglePause();
        _btnSpine.Click += (_, _) => OpenSpineEditor();
        _btnSpineRelease.Click += (_, _) => ReleaseSpineData();
        _btnDeleteAll.Click += (_, _) => DeleteAllSequences();
        _btnVkOpen.Click += (_, _) => OpenVirtualKeys();
        _btnVkClose.Click += (_, _) => CloseVirtualKeys();
        _btnVkManage.Click += (_, _) => OpenVkManager();
        _btnImport.Click += (_, _) => ImportDataBundle();
        _btnExport.Click += (_, _) => ExportDataBundle();

        toolStrip.Controls.AddRange([_btnAdd, _btnEdit, _btnDelete, _btnTest, _btnDuplicate, _btnPause, _btnSpine, _btnSpineRelease, _btnDeleteAll, _btnVkOpen, _btnVkClose, _btnVkManage, _btnImport, _btnExport]);
        Controls.Add(toolStrip);

        var dgvPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, (int)(48 * DeviceDpi / 96f), 0, 0)
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
            EditMode = DataGridViewEditMode.EditOnEnter
        };
        _dgv.CellDoubleClick += (_, _) => EditSequence();
        _dgv.CellValueChanged += Dgv_CellValueChanged;
        _dgv.CellFormatting += Dgv_CellFormatting;
        _dgv.CellClick += Dgv_CellClick;
        dgvPanel.Controls.Add(_dgv);
        Controls.Add(dgvPanel);
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
        OperationLogger.Info($"Application started, version 2.16");
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
            p.Padding = new Padding(0, (int)(48 * DeviceDpi / 96f), 0, 0);
        RefreshGrid();
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

        if (e.ColumnIndex == 7)
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
        else if (e.ColumnIndex == 8)
        {
            if (!string.IsNullOrEmpty(seq.TargetAppPath))
            {
                seq.TargetAppPath = "";
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
        if (e.RowIndex < 0 || e.RowIndex >= _sequences.Count) return;
        var seq = _sequences[e.RowIndex];

        switch (e.ColumnIndex)
        {
            case 0: // 启用
                var enabled = _dgv.Rows[e.RowIndex].Cells[0].Value is true;
                if (seq.Enabled != enabled)
                {
                    seq.Enabled = enabled;
                    _config.Save(_sequences);
                    _hotkeyService.RegisterAll(_sequences);
                }
                break;


            case 5: // 间隔(ms)
                int.TryParse(_dgv.Rows[e.RowIndex].Cells[5].Value?.ToString(), out var interval);
                seq.LoopIntervalMs = interval > 0 ? interval : 200;
                _config.Save(_sequences);
                break;

            case 6: // 循环次数
                int.TryParse(_dgv.Rows[e.RowIndex].Cells[6].Value?.ToString(), out var count);
                seq.LoopCount = count >= 0 ? count : 0;
                _config.Save(_sequences);
                break;
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

    private void TestSequence()
    {
        if (GetSelectedSequence() is not { } seq) return;
        if (_player.IsPlaying)
        {
            _player.Stop();
            _btnTest.Text = "测试";
            return;
        }
        OperationLogger.Info($"MainForm: testing sequence \"{seq.Name}\" ({seq.Id})");
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
            TriggerVkButtonName = src.TriggerVkButtonName,
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
        bundle.VkData = _vkSerializer.LoadAll().Windows.FirstOrDefault();

        var editor = Application.OpenForms.OfType<SpineHotkeyEditor>().FirstOrDefault();
        bundle.SpineHotkeys = editor?.GetCurrentEntries();

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

        MessageBox.Show("导入操作将分项确认，包含以下 4 部分：\n\n" +
            "1. Spine 热键编辑\n" +
            "2. 序列设置\n" +
            "3. 虚拟按键布局\n" +
            "4. 虚拟按键设置",
            "导入确认", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // 1. Spine hotkeys
        if (bundle.SpineHotkeys?.Count > 0)
        {
            if (MessageBox.Show("是否导入 Spine 热键编辑？", "导入", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var editor = new SpineHotkeyEditor(dialog.FileName, bundle.SpineHotkeys);
                editor.Show();
            }
        }
        else
        {
            MessageBox.Show("Spine 热键编辑：文件中无相关数据，跳过。", "导入", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 2. Sequences
        if (bundle.Sequences?.Count > 0)
        {
            if (MessageBox.Show($"是否导入序列设置？（共 {bundle.Sequences.Count} 个序列）", "导入", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _sequences = [.. bundle.Sequences];
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

        // 3. VK layout (buttons)
        if (bundle.VkData?.Buttons?.Count > 0)
        {
            if (MessageBox.Show($"是否导入虚拟按键布局？（共 {bundle.VkData.Buttons.Count} 个按钮）", "导入", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var global = _vkSerializer.LoadAll();
                bundle.VkData.Name = $"导入_{global.Windows.Count + 1}";
                bundle.VkData.Enabled = true;
                global.Windows.Add(bundle.VkData);
                _vkSerializer.SaveAll(global);
            }
        }
        else
        {
            MessageBox.Show("虚拟按键布局：文件中无相关数据，跳过。", "导入", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 4. VK settings (merged into multi-window, skip as separate section)
    }

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
        _dgv.Columns.Clear();
        float ds = DeviceDpi / 96f;

        _dgv.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "启用",
            Width = (int)(50 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "序列名称",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 28,
            MinimumWidth = (int)(80 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "触发快捷键",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 18,
            MinimumWidth = (int)(60 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "目标软件",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 18,
            MinimumWidth = (int)(60 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "步骤数",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 10,
            MinimumWidth = (int)(40 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "间隔(ms)",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 12,
            MinimumWidth = (int)(50 * ds)
        });
        _dgv.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "循环(次)",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 12,
            MinimumWidth = (int)(50 * ds)
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "选择",
            Text = "...",
            UseColumnTextForButtonValue = true,
            Width = (int)(50 * ds),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = true
        });
        _dgv.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "清除",
            Text = "✕",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 2,
            ReadOnly = true
        });

        _dgv.Rows.Clear();
        foreach (var seq in _sequences)
        {
            var appName = string.IsNullOrEmpty(seq.TargetAppPath)
                ? "全局"
                : Path.GetFileName(seq.TargetAppPath);
            var idx = _dgv.Rows.Add(
                seq.Enabled, seq.Name, !string.IsNullOrEmpty(seq.TriggerVkButtonName) ? $"虚拟按键({seq.TriggerVkButtonName})" : seq.TriggerHotkey, appName, seq.Steps.Count,
                seq.LoopIntervalMs.ToString(), seq.LoopCount.ToString());
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
        if (seq == null || !seq.Enabled) return;
        if (_player.IsPlaying)
            _player.Stop();
        else
            _ = _player.Play(seq);
    }

}
