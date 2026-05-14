using KeyMacro.Models;
using KeyMacro.Services;
using System.Runtime.InteropServices;

namespace KeyMacro.Forms;

public class VirtualKeyWindow : Form
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int maxLen);
    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    private readonly VirtualButtonManager _btnManager;
    private readonly VirtualKeyBindingManager _bindingManager;
    private readonly VirtualLoopExecutor _loopExecutor;
    private readonly VirtualLayoutSerializer _serializer;
    private readonly List<MacroSequence> _sequences;
    private readonly Action? _sequencesChangedCallback;
    private readonly FlowLayoutPanel _panel;
    private readonly Dictionary<string, VirtualButtonWidget> _widgets = [];

    private bool _topMostState = true;
    private double _opacityValue = 1.0;
    private bool _posLocked;
    private bool _winLocked;
    private string? _targetProc;
    private string? _targetTitle;
    private bool _singleLine = true;
    private float _scaleFactor = 1.0f;
    private bool _schemeAFailed;

    // ── Layout base metrics at 100% scale ──
    private const int BASE_BTN_H = 48;
    private const int BASE_GAP = 4;
    private const int BASE_MARGIN = 10;
    private static int BaseBtnWidth(VirtualButtonStyle style) => style switch
    {
        VirtualButtonStyle.LargeIcon => 96,
        VirtualButtonStyle.LoopIcon => 110,
        _ => 48
    };
    private VkSkinLoader _skinLoader = new(null);

    public VirtualKeyWindow(
        VirtualButtonManager btnManager,
        VirtualKeyBindingManager bindingManager,
        VirtualLoopExecutor loopExecutor,
        VirtualLayoutSerializer serializer,
        List<MacroSequence> sequences,
        Action? sequencesChangedCallback = null)
    {
        _btnManager = btnManager;
        _bindingManager = bindingManager;
        _loopExecutor = loopExecutor;
        _serializer = serializer;
        _sequences = sequences;
        _sequencesChangedCallback = sequencesChangedCallback;

        Text = "虚拟按键";
        BackColor = Color.FromArgb(0x0D, 0x0D, 0x0D);
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(320, 100);
        Opacity = _opacityValue;

        _panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(BASE_MARGIN),
            BackColor = Color.FromArgb(0x0D, 0x0D, 0x0D),
            WrapContents = false,
            AutoScroll = false
        };
        Controls.Add(_panel);

        var menu = BuildBlankMenu();
        _panel.ContextMenuStrip = menu;

        _btnManager.ButtonsChanged += RebuildWidgets;
        var layoutData = _serializer.Load();
        _skinLoader = new VkSkinLoader(layoutData.SkinPath);
        _skinLoader.Load();
        LoadLayoutData(layoutData);

        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                _loopExecutor.StopAll(); SaveLayout(); e.Cancel = true; Hide();
            }
        };
    }

    // ── Context menu ──

    private ContextMenuStrip BuildBlankMenu()
    {
        var m = new ContextMenuStrip();
        m.Items.Add("增加标准按钮", null, (_, _) => AddButton(VirtualButtonStyle.SmallIcon));
        m.Items.Add("增加大按钮", null, (_, _) => AddButton(VirtualButtonStyle.LargeIcon));
        m.Items.Add("增加循环按钮", null, (_, _) => AddButton(VirtualButtonStyle.LoopIcon));
        m.Items.Add("-");
        m.Items.Add("删除所有按钮", null, (_, _) =>
        {
            if (_btnManager.Buttons.Count == 0) return;
            if (MessageBox.Show("确定删除所有虚拟按钮？此操作不可撤销。", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { _btnManager.Clear(); RebuildWidgets(); }
        });
        m.Items.Add("-");
        m.Items.Add("置顶/取消置顶", null, (_, _) => { _topMostState = !_topMostState; TopMost = _topMostState; });

        var opMenu = new ToolStripMenuItem("透明度");
        opMenu.DropDownItems.Add("100%", null, (_, _) => SetOpacity(1.0));
        opMenu.DropDownItems.Add("80%", null, (_, _) => SetOpacity(0.8));
        opMenu.DropDownItems.Add("60%", null, (_, _) => SetOpacity(0.6));
        opMenu.DropDownItems.Add("40%", null, (_, _) => SetOpacity(0.4));
        m.Items.Add(opMenu);

        m.Items.Add(_posLocked ? "✓ 按钮位置已锁定" : "按钮位置锁定/解锁", null, (_, _) =>
        {
            _posLocked = !_posLocked;
            foreach (var w in _widgets.Values) w.AllowDragging = !_posLocked;
        });
        m.Items.Add("-");
        m.Items.Add("捕获目标窗口", null, (_, _) => CaptureTargetWindow());
        var clearTarget = new ToolStripMenuItem("清除目标窗口");
        clearTarget.Click += (_, _) => ClearTargetWindow();
        m.Items.Add(clearTarget);

        m.Items.Add(_singleLine ? "✓ 单排" : "单排/多排", null, (_, _) => ToggleLayoutMode());

        var scaleMenu = new ToolStripMenuItem("缩放");
        foreach (var pct in new[] { 50, 75, 100, 150, 200 })
        {
            var item = scaleMenu.DropDownItems.Add($"{pct}%");
            item.Click += (_, _) => SetScale(pct / 100f);
        }
        scaleMenu.DropDownItems.Add("-");
        var cust = scaleMenu.DropDownItems.Add("自定义...");
        cust.Click += (_, _) =>
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("输入缩放比例 (10-200):", "自定义缩放",
                ((int)(_scaleFactor * 100)).ToString());
            if (int.TryParse(input, out var pct) && pct >= 10 && pct <= 200) SetScale(pct / 100f);
        };
        m.Items.Add(scaleMenu);

        m.Items.Add("-");
        m.Items.Add("保存布局", null, (_, _) => SaveLayout());
        m.Items.Add("重置布局", null, (_, _) => ResetLayout());
        var lockItem = m.Items.Add(_winLocked ? "✓ 窗口已锁定" : "窗口锁定/解锁", null, (_, _) => ToggleWindowLock());
        m.Items.Add("-");
        m.Items.Add("关闭窗口", null, (_, _) => { _loopExecutor.StopAll(); SaveLayout(); Hide(); });

        m.Opened += (_, _) =>
        {
            var display = _targetTitle ?? _targetProc;
            clearTarget.Text = string.IsNullOrEmpty(display) ? "清除目标窗口" : $"清除目标窗口 ({display})";
            clearTarget.Visible = !string.IsNullOrEmpty(_targetProc);
            var layoutIdx = m.Items.IndexOf(clearTarget) + 1;
            if (layoutIdx < m.Items.Count)
                m.Items[layoutIdx].Text = _singleLine ? "✓ 单排" : "单排/多排";
            lockItem.Text = _winLocked ? "✓ 窗口已锁定" : "窗口锁定/解锁";
        };
        return m;
    }

    private void OnWidgetContextMenu(VirtualButtonWidget widget, Point location)
    {
        var vbtn = widget.VirtualButton;
        var menu = new ContextMenuStrip();
        var nameItem = menu.Items.Add($"[ {vbtn.Name} ]");
        nameItem.Enabled = false;
        menu.Items.Add("-");
        menu.Items.Add("修改按钮名称", null, (_, _) =>
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("输入新的按钮名称:", "修改按钮名称", vbtn.Name);
            if (!string.IsNullOrWhiteSpace(input) && input.Trim() != vbtn.Name)
                { vbtn.Name = input.Trim(); widget.UpdateButton(vbtn); SaveLayout(); }
        });
        var bindItem = new ToolStripMenuItem("绑定快捷键");
        bindItem.DropDownItems.Add("设置绑定", null, (_, _) => ShowBindingDialog(widget));
        if (!string.IsNullOrEmpty(vbtn.BindActionId))
            bindItem.DropDownItems.Add("清除绑定", null, (_, _) => { _bindingManager.Unbind(vbtn); widget.UpdateButton(vbtn); SaveLayout(); });
        menu.Items.Add(bindItem);
        if (vbtn.StyleType == VirtualButtonStyle.LoopIcon)
        {
            var intvMenu = new ToolStripMenuItem("按钮循环延迟");
            intvMenu.DropDownItems.Add("100ms", null, (_, _) => SetLoopInterval(vbtn, widget, 100));
            intvMenu.DropDownItems.Add("300ms", null, (_, _) => SetLoopInterval(vbtn, widget, 300));
            intvMenu.DropDownItems.Add("500ms", null, (_, _) => SetLoopInterval(vbtn, widget, 500));
            intvMenu.DropDownItems.Add("-");
            intvMenu.DropDownItems.Add("自定义...", null, (_, _) =>
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox("循环延迟 (ms):", "设置循环延迟", vbtn.LoopInterval.ToString());
                if (int.TryParse(input, out var ms) && ms > 0) SetLoopInterval(vbtn, widget, ms);
            });
            menu.Items.Add(intvMenu);
        }
        menu.Items.Add("-");
        menu.Items.Add("删除当前按钮", null, (_, _) =>
        {
            _loopExecutor.StopLoop(vbtn.Id); _btnManager.RemoveButton(vbtn.Id); SaveLayout();
        });
        menu.Show(widget, location);
    }

    private void SetLoopInterval(VirtualButton vbtn, VirtualButtonWidget widget, int ms)
    {
        vbtn.LoopInterval = ms;
        var seq = _sequences.Find(s => s.Id == vbtn.BindActionId);
        if (seq != null) { seq.LoopIntervalMs = ms; _sequencesChangedCallback?.Invoke(); }
        widget.UpdateButton(vbtn); SaveLayout();
    }

    private void ShowBindingDialog(VirtualButtonWidget widget)
    {
        var vbtn = widget.VirtualButton;
        var avail = _sequences.Where(s => !string.IsNullOrEmpty(s.Name)).ToList();
        if (avail.Count == 0) { MessageBox.Show("没有可绑定的快捷动作，请先在主窗口创建序列。", "提示"); return; }
        using var menu = new ContextMenuStrip();
        foreach (var seq in avail)
        {
            var item = menu.Items.Add(seq.Name);
            item.Click += (_, _) =>
            {
                if (!_bindingManager.TryBind(vbtn, seq.Id))
                    { MessageBox.Show("该动作已被其他虚拟按钮绑定。", "冲突", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                widget.UpdateButton(vbtn); SaveLayout();
            };
        }
        menu.Show(widget, new Point(0, 0));
    }

    // ── Op / Lock ──

    private void SetOpacity(double val) { _opacityValue = val; Opacity = val; }

    private void ToggleWindowLock()
    {
        _winLocked = !_winLocked;
        if (_winLocked)
        {
            FormBorderStyle = FormBorderStyle.None;
            ControlBox = false;
            Text = "";
        }
        else
        {
            FormBorderStyle = FormBorderStyle.FixedSingle;
            ControlBox = true;
            UpdateTitle();
        }
    }

    public bool HasBoundButtons() => _btnManager.Buttons.Any(b => !string.IsNullOrEmpty(b.BindActionId));

    // ── Layout mode ──

    private void ToggleLayoutMode()
    {
        _singleLine = !_singleLine;
        if (_singleLine) { _panel.WrapContents = false; _panel.AutoScroll = false; }
        else { _panel.WrapContents = true; _panel.AutoScroll = true; }
        RecalculateSize(); SaveLayout();
    }

    // ── Scale ──

    private void SetScale(float factor)
    {
        _scaleFactor = Math.Clamp(factor, 0.1f, 2.0f);
        int margin = Math.Max(1, (int)(BASE_MARGIN * _scaleFactor));
        _panel.Padding = new Padding(margin);
        UpdateScale(); RecalculateSize(); SaveLayout();
    }

    // ── Title ──

    private void UpdateTitle()
    {
        Text = (_targetProc != null ? $"[{_targetTitle ?? _targetProc}] " : "") + $"虚拟按键 ({_widgets.Count})";
    }

    // ── Target window capture ──

    private void CaptureTargetWindow()
    {
        Hide();
        using var overlay = new Form
        {
            Text = "", StartPosition = FormStartPosition.CenterScreen, Size = new Size(400, 120),
            FormBorderStyle = FormBorderStyle.None, BackColor = Color.Black, Opacity = 0.85,
            TopMost = true, ShowInTaskbar = false
        };
        var lbl = new Label { Text = "请在 3 秒内切换到目标窗口...", Dock = DockStyle.Fill,
            ForeColor = Color.White, Font = new Font("Microsoft YaHei", 14, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
        overlay.Controls.Add(lbl);
        overlay.Show();
        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var hwnd = GetForegroundWindow();
            overlay.Close(); overlay.Dispose();
            if (hwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out var pid);
                try
                {
                    using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                    _targetProc = proc.ProcessName;
                    var len = GetWindowTextLength(hwnd);
                    var sb = new System.Text.StringBuilder(len + 1);
                    GetWindowText(hwnd, sb, sb.Capacity);
                    _targetTitle = len > 0 ? sb.ToString() : null;
                    MessageBox.Show(this, $"目标窗口已捕获: {_targetTitle ?? _targetProc}", "捕获成功");
                }
                catch { _targetProc = null; _targetTitle = null; MessageBox.Show(this, "无法获取目标进程信息。", "捕获失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
            Show();
            UpdateTitle();
        };
        timer.Start();
    }

    private void ClearTargetWindow()
    {
        _targetProc = null; _targetTitle = null; _schemeAFailed = false; SaveLayout(); UpdateTitle();
    }

    private IntPtr ResolveTargetWindow()
    {
        if (string.IsNullOrEmpty(_targetProc)) return IntPtr.Zero;
        var procs = System.Diagnostics.Process.GetProcessesByName(_targetProc);
        foreach (var proc in procs)
        {
            var hwnd = proc.MainWindowHandle;
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) continue;
            if (!string.IsNullOrEmpty(_targetTitle))
            {
                var len = GetWindowTextLength(hwnd);
                if (len > 0)
                {
                    var sb = new System.Text.StringBuilder(len + 1);
                    GetWindowText(hwnd, sb, sb.Capacity);
                    if (sb.ToString() == _targetTitle) return hwnd;
                }
                continue;
            }
            return hwnd;
        }
        return IntPtr.Zero;
    }

    // ── Button mgmt ──

    private void AddButton(VirtualButtonStyle style) { _btnManager.AddButton(style); RebuildWidgets(); }

    private void RebuildWidgets()
    {
        _panel.SuspendLayout();
        _panel.Controls.Clear();
        _widgets.Clear();
        var buttons = _btnManager.Buttons;
        for (int i = 0; i < buttons.Count; i++)
        {
            var w = new VirtualButtonWidget(buttons[i]);
            w.IsFirstInRow = i == 0; w.IsLastInRow = i == buttons.Count - 1;
            w.Clicked += OnButtonClicked;
            w.Dragged += OnButtonDragged;
            w.ContextMenuRequested += OnWidgetContextMenu;
            w.LoopCountEdited += OnLoopCountEdited;
            _panel.Controls.Add(w);
            _widgets[buttons[i].Id] = w;
        }
        UpdateScale();
        RecalculateSize();
        _panel.ResumeLayout();
        UpdateTitle();
        OperationLogger.Info($"VKWindow.Rebuild: {_widgets.Count} widgets");
    }

    private void UpdateScale()
    {
        foreach (var w in _widgets.Values) { w.ScaleFactor = _scaleFactor; w.UpdateSize(); }
    }

    private void RecalculateSize()
    {
        int ncW = Width - ClientSize.Width;
        int ncH = Height - ClientSize.Height;
        float S = _scaleFactor;
        int btnH = Math.Max(1, (int)(BASE_BTN_H * S));
        int gap = Math.Max(1, (int)(BASE_GAP * S));
        int margin = Math.Max(1, (int)(BASE_MARGIN * S));
        int n = _widgets.Count;
        int barH = !_winLocked ? 28 : 0; // native title bar

        if (n == 0)
        {
            Size = new Size(margin * 2 + 20 + ncW, barH + margin * 2 + 20 + ncH);
            return;
        }

        // Window width  = margin + sum(各按钮宽) + (N-1)×gap + margin
        // Window height = titleBar + margin + btnH + margin
        int totalW = margin + _widgets.Values.Sum(w => Math.Max(1, (int)(BaseBtnWidth(w.VirtualButton.StyleType) * S))) + (n - 1) * gap + margin;
        int totalH = barH + margin + btnH + margin;
        Size = new Size(totalW + ncW, totalH + ncH);

        // Update widget margins for consistent gaps
        int halfGap = gap / 2;
        foreach (var w in _widgets.Values)
            w.Margin = new Padding(halfGap);
    }

    // ── Button events ──

    private async void OnButtonClicked(VirtualButtonWidget widget)
    {
        var vbtn = widget.VirtualButton;
        OperationLogger.Info($"VKWindow.Click: \"{vbtn.Name}\" VkPick={SequenceEditor.IsVkPickMode}");

        if (SequenceEditor.IsVkPickMode)
        {
            var seq = _bindingManager.ResolveBinding(vbtn, _sequences);
            SequenceEditor.ReceiveVkPick(vbtn.Name, seq?.TriggerHotkey);
            return;
        }

        if (vbtn.StyleType == VirtualButtonStyle.LoopIcon && vbtn.LoopEnabled)
        {
            var seq = _bindingManager.ResolveBinding(vbtn, _sequences);
            if (seq != null) { _loopExecutor.StartLoop(vbtn, seq); widget.IsActive = true; }
            return;
        }

        var sequence = _bindingManager.ResolveBinding(vbtn, _sequences);
        if (sequence == null) { OperationLogger.Warn($"VKWindow.Click: no binding"); return; }

        var hwnd = ResolveTargetWindow();
        if (hwnd == IntPtr.Zero)
        {
            _ = new MacroPlayer().Play(sequence);
            return;
        }

        if (GetForegroundWindow() == hwnd)
        {
            _ = new MacroPlayer().Play(sequence);
        }
        else if (!_schemeAFailed)
        {
            var player = new MacroPlayer();
            await player.PlayToWindow(sequence, hwnd);
            await Task.Delay(100);
            if (GetForegroundWindow() != hwnd) { _schemeAFailed = true; }
        }
        else
        {
            SetForegroundWindow(hwnd);
            await Task.Delay(200);
            _ = new MacroPlayer().Play(sequence);
        }
    }

    private void OnButtonDragged(VirtualButtonWidget widget, int dx, int dy)
    {
        var vbtn = widget.VirtualButton;
        _btnManager.UpdatePosition(vbtn.Id, widget.Location.X + dx, widget.Location.Y + dy);
    }

    private void OnLoopCountEdited(VirtualButtonWidget widget, int count)
    {
        var vbtn = widget.VirtualButton;
        var seq = _sequences.Find(s => s.Id == vbtn.BindActionId);
        if (seq != null) { seq.LoopIntervalMs = vbtn.LoopInterval; seq.LoopCount = vbtn.LoopCount; _sequencesChangedCallback?.Invoke(); }
        SaveLayout();
    }

    // ── Layout persistence ──

    private void SaveLayout()
    {
        _serializer.Save(new VirtualLayoutSerializer.LayoutData
        {
            WindowX = Left, WindowY = Top, WindowWidth = Width, WindowHeight = Height,
            TopMost = _topMostState, PositionLocked = _posLocked, WindowLocked = _winLocked,
            TargetProcessName = _targetProc, TargetWindowTitle = _targetTitle,
            SingleLineMode = _singleLine, ScaleFactor = _scaleFactor,
            Buttons = [.. _btnManager.Buttons]
        });
    }

    private void LoadLayoutData(VirtualLayoutSerializer.LayoutData data)
    {
        _targetProc = data.TargetProcessName;
        _targetTitle = data.TargetWindowTitle;
        _singleLine = data.SingleLineMode;
        _scaleFactor = data.ScaleFactor > 0 ? data.ScaleFactor : 1.0f;
        int margin = Math.Max(1, (int)(BASE_MARGIN * _scaleFactor));
        _panel.Padding = new Padding(margin);

        if (data.Buttons.Count > 0)
        {
            _btnManager.LoadFrom(data.Buttons);
            var savedLoc = new Point(data.WindowX, data.WindowY);
            var testRect = new Rectangle(savedLoc, new Size(Math.Max(data.WindowWidth, 100), Math.Max(data.WindowHeight, 100)));
            Location = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(testRect))
                ? savedLoc : new Point(Screen.PrimaryScreen!.WorkingArea.Width / 2 - Width / 2,
                    Screen.PrimaryScreen!.WorkingArea.Height / 2 - Height / 2);
            _topMostState = data.TopMost; TopMost = _topMostState;
            _posLocked = data.PositionLocked; _winLocked = data.WindowLocked;
            foreach (var w in _widgets.Values) w.AllowDragging = !_posLocked;
            if (_singleLine) { _panel.WrapContents = false; _panel.AutoScroll = false; }
            else { _panel.WrapContents = true; _panel.AutoScroll = true; }
            if (_winLocked) { FormBorderStyle = FormBorderStyle.None; ControlBox = false; Text = ""; }
            else { FormBorderStyle = FormBorderStyle.FixedSingle; ControlBox = true; }
            UpdateScale();
            RecalculateSize();
        }
        else
        {
            Size = new Size(320, 100);
            var s = Screen.PrimaryScreen;
            if (s != null) Location = new Point((s.WorkingArea.Width - Width) / 2, (s.WorkingArea.Height - Height) / 2);
        }
        UpdateTitle();
    }

    private void LoadLayout() => LoadLayoutData(_serializer.Load());
    private void ResetLayout() { _btnManager.Clear(); SaveLayout(); }
}
