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
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    private static readonly IntPtr HwndTop = new(0);
    private static readonly IntPtr HwndTopMost = new(-1);
    private const int SwShownoactivate = 4;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;

    private readonly VirtualButtonManager _btnManager = new();
    private readonly VirtualKeyBindingManager _bindingManager;
    private readonly VirtualLoopExecutor _loopExecutor;
    private readonly VirtualLayoutSerializer _serializer;
    private VirtualLayoutSerializer.WindowLayoutData _data;
    private readonly MacroPlayer _player = new();
    private readonly List<MacroSequence> _sequences;
    private readonly Action? _sequencesChangedCallback;
    private readonly FlowLayoutPanel _panel;
    private readonly Dictionary<string, VirtualButtonWidget> _widgets = [];

    public Action<VirtualKeyWindow>? DeleteRequested;

    public VirtualLayoutSerializer.WindowLayoutData WindowData => _data;

    private bool _topMostState = true;
    private double _opacityValue = 1.0;
    private bool _posLocked;
    private bool _winLocked;
    private string? _targetProc;
    private string? _targetTitle;
    private bool _vertical;
    private bool _isBeingDeleted;
    private ToolStripMenuItem? _autoVisibilityMenuItem;
    private ToolStripMenuItem? _edgeSnapMenuItem;
    private ToolStripMenuItem? _autoHideMenuItem;
    private readonly System.Windows.Forms.Timer _behaviorTimer;
    private readonly ForegroundProcessWatcher _foregroundWatcher = new();
    private bool _manualHidden;
    private bool _behaviorVisible;
    private bool _interactionHold;
    private bool _draggingWindow;
    private Point _dragStartCursor;
    private Point _dragStartLocation;
    private bool _edgeHidden;
    private Point _locationBeforeEdgeHide;
    private DateTime _lastInteractionUtc = DateTime.UtcNow;
    private VirtualLayoutSerializer.LayoutMode _layoutMode = VirtualLayoutSerializer.LayoutMode.AutoGrid;
    private int _fixedColumns;
    private int _fixedRows;
    private float _buttonScale = 1.0f;
    private int _innerPadding = 10;
    private int _buttonGap = 4;
    private bool _windowSizeLocked;
    private bool _windowSizeInitialized;
    private bool _resizingWindow;
    private bool _layouting;
    private Point _resizeStartCursor;
    private Size _resizeStartSize;
    private readonly Panel _resizeGrip = new();
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

    private float GetEffectiveScale() => _buttonScale * (DeviceDpi / 96f);

    public VirtualKeyWindow(
        VirtualLayoutSerializer serializer,
        VirtualLayoutSerializer.WindowLayoutData data,
        List<MacroSequence> sequences,
        Action? sequencesChangedCallback = null)
    {
        _serializer = serializer;
        _data = data;
        _sequences = sequences;
        _sequencesChangedCallback = sequencesChangedCallback;
        _bindingManager = new VirtualKeyBindingManager(new HotkeyService(IntPtr.Zero), _btnManager);
        _loopExecutor = new VirtualLoopExecutor(_player);
        _loopExecutor.LoopEnded += OnLoopEnded;
        _behaviorTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _behaviorTimer.Tick += (_, _) => EvaluateWindowBehavior();

        Text = "虚拟按键";
        Icon = IconService.AppIcon;
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
            AutoScroll = false,
            FlowDirection = FlowDirection.LeftToRight
        };
        Controls.Add(_panel);
        _resizeGrip.BackColor = Color.Transparent;
        _resizeGrip.Cursor = Cursors.SizeNWSE;
        _resizeGrip.Size = new Size(20, 20);
        _resizeGrip.Paint += ResizeGrip_Paint;
        _resizeGrip.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _resizeGrip.BringToFront();
        _resizeGrip.MouseDown += ResizeGrip_MouseDown;
        _resizeGrip.MouseMove += ResizeGrip_MouseMove;
        _resizeGrip.MouseUp += ResizeGrip_MouseUp;
        Controls.Add(_resizeGrip);
        _resizeGrip.BringToFront();

        var menu = BuildBlankMenu();
        _panel.ContextMenuStrip = menu;
        _panel.MouseDown += (_, e) => OnPanelWindowDrag(e);
        _panel.MouseMove += (_, e) => OnPanelWindowDrag(e);
        _panel.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Middle)
                OnWidgetWindowDragEnded();
        };
        MouseDown += (_, e) => OnPanelWindowDrag(e);
        MouseMove += (_, e) => OnPanelWindowDrag(e);
        MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Middle)
                OnWidgetWindowDragEnded();
        };

        _skinLoader = new VkSkinLoader(_data.SkinPath);
        _skinLoader.Load();
        _btnManager.ButtonsChanged += RebuildWidgets;
        _btnManager.LoadFrom(_data.Buttons);
        ApplyWindowSkin();
        ApplyLayoutData();

        Shown += (_, _) => { if (_widgets.Count > 0) RecalculateSize(!_windowSizeInitialized); };

        FormClosing += (_, e) =>
        {
            if (_isBeingDeleted) return;
            if (e.CloseReason == CloseReason.UserClosing)
            {
                _loopExecutor.StopAll();
                _manualHidden = true;
                _behaviorVisible = false;
                SaveLayout();
                e.Cancel = true;
                Hide();
            }
        };
        VisibleChanged += (_, _) =>
        {
            if (Visible && !_manualHidden)
                _behaviorVisible = true;
            PositionResizeGrip();
        };
        Resize += (_, _) => PositionResizeGrip();

        FormClosed += (_, _) =>
        {
            _behaviorTimer.Stop();
            _foregroundWatcher.Dispose();
            if (_isBeingDeleted)
                _loopExecutor.StopAll();
        };
        Shown += (_, _) => StartBehaviorMonitoring();
        _foregroundWatcher.ForegroundChanged += _ =>
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke((Action)EvaluateWindowBehavior);
            else EvaluateWindowBehavior();
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
                { _btnManager.Clear(); RebuildWidgets(); SaveLayout(); }
        });
        m.Items.Add("-");
        m.Items.Add("置顶/取消置顶", null, (_, _) => { _topMostState = !_topMostState; TopMost = _topMostState; SaveLayout(); });

        _autoVisibilityMenuItem = new ToolStripMenuItem("目标进程自动显示/隐藏")
        {
            Checked = _data.AutoVisibilityEnabled,
            CheckOnClick = true
        };
        _autoVisibilityMenuItem.CheckedChanged += (_, _) =>
        {
            _data.AutoVisibilityEnabled = _autoVisibilityMenuItem.Checked;
            _manualHidden = false;
            if (!_data.AutoVisibilityEnabled)
            {
                _behaviorVisible = true;
                ShowManually();
            }
            SaveLayout();
        };
        m.Items.Add(_autoVisibilityMenuItem);

        _edgeSnapMenuItem = new ToolStripMenuItem("屏幕边缘吸附")
        {
            Checked = _data.EdgeSnapEnabled,
            CheckOnClick = true
        };
        _edgeSnapMenuItem.CheckedChanged += (_, _) =>
        {
            _data.EdgeSnapEnabled = _edgeSnapMenuItem.Checked;
            SaveLayout();
        };
        m.Items.Add(_edgeSnapMenuItem);

        _autoHideMenuItem = new ToolStripMenuItem("边缘自动隐藏")
        {
            Checked = _data.AutoHideEnabled,
            CheckOnClick = true,
            Enabled = _data.EdgeSnapEnabled
        };
        _autoHideMenuItem.CheckedChanged += (_, _) =>
        {
            _data.AutoHideEnabled = _autoHideMenuItem.Checked;
            if (!_data.AutoHideEnabled) RestoreFromEdgeHide();
            SaveLayout();
        };
        m.Items.Add(_autoHideMenuItem);

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
            SaveLayout();
        });
        m.Items.Add("-");
        m.Items.Add("捕获目标窗口", null, (_, _) => CaptureTargetWindow());
        var clearTarget = new ToolStripMenuItem("清除目标窗口");
        clearTarget.Click += (_, _) => ClearTargetWindow();
        m.Items.Add(clearTarget);

        m.Items.Add("-");
        var layoutMenu = new ToolStripMenuItem("布局设置");
        AddLayoutModeItem(layoutMenu, "自适应网格", VirtualLayoutSerializer.LayoutMode.AutoGrid);
        AddLayoutModeItem(layoutMenu, "单行", VirtualLayoutSerializer.LayoutMode.SingleRow);
        AddLayoutModeItem(layoutMenu, "单列", VirtualLayoutSerializer.LayoutMode.SingleColumn);
        AddLayoutModeItem(layoutMenu, "固定 2 列", VirtualLayoutSerializer.LayoutMode.FixedColumns, 2);
        AddLayoutModeItem(layoutMenu, "固定 3 列", VirtualLayoutSerializer.LayoutMode.FixedColumns, 3);
        AddLayoutModeItem(layoutMenu, "固定 4 列", VirtualLayoutSerializer.LayoutMode.FixedColumns, 4);
        var buttonScaleMenu = new ToolStripMenuItem("按钮比例");
        buttonScaleMenu.DropDownItems.Add("减小", null, (_, _) => SetButtonScale(_buttonScale - 0.05f));
        buttonScaleMenu.DropDownItems.Add("增大", null, (_, _) => SetButtonScale(_buttonScale + 0.05f));
        buttonScaleMenu.DropDownItems.Add("重置为 100%", null, (_, _) => SetButtonScale(1.0f));
        buttonScaleMenu.DropDownItems.Add("自定义...", null, (_, _) =>
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("输入按钮比例 (40-200):", "按钮比例",
                ((int)(_buttonScale * 100)).ToString());
            if (int.TryParse(input, out var pct) && pct >= 40 && pct <= 200)
                SetButtonScale(pct / 100f);
        });
        layoutMenu.DropDownItems.Add(buttonScaleMenu);
        layoutMenu.DropDownItems.Add("内边距...", null, (_, _) => SetLayoutMetric(true));
        layoutMenu.DropDownItems.Add("按钮间距...", null, (_, _) => SetLayoutMetric(false));
        AddLayoutModeItem(layoutMenu, "固定 2 行", VirtualLayoutSerializer.LayoutMode.FixedRows, 2);
        AddLayoutModeItem(layoutMenu, "固定 3 行", VirtualLayoutSerializer.LayoutMode.FixedRows, 3);
        AddLayoutModeItem(layoutMenu, "固定 4 行", VirtualLayoutSerializer.LayoutMode.FixedRows, 4);
        var resizeItem = layoutMenu.DropDownItems.Add(_windowSizeLocked ? "✓ 窗口尺寸已锁定" : "窗口尺寸锁定/解锁");
        resizeItem.Click += (_, _) =>
        {
            _windowSizeLocked = !_windowSizeLocked;
            _data.WindowSizeLocked = _windowSizeLocked;
            PositionResizeGrip();
            resizeItem.Text = _windowSizeLocked ? "✓ 窗口尺寸已锁定" : "窗口尺寸锁定/解锁";
            SaveLayout();
        };
        m.Items.Add(layoutMenu);

        m.Items.Add("-");
        var lockItem = m.Items.Add(_winLocked ? "✓ 窗口已锁定" : "窗口锁定/解锁", null, (_, _) => ToggleWindowLock());
        m.Items.Add("-");
        m.Items.Add("关闭窗口", null, (_, _) => { _loopExecutor.StopAll(); _manualHidden = true; SaveLayout(); Hide(); });
        m.Items.Add("删除当前窗口", null, (_, _) =>
        {
            if (MessageBox.Show(this, $"确定删除窗口\"{_data.Name}\"及其所有按钮？", "删除确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                OperationLogger.Info($"VKWindow.RightClickDelete: name={_data.Name}");
                _isBeingDeleted = true;
                _loopExecutor.StopAll();
                DeleteRequested?.Invoke(this);
                Close();
            }
        });

        m.Opened += (_, _) =>
        {
            _interactionHold = true;
            _lastInteractionUtc = DateTime.UtcNow;
            var display = _targetTitle ?? _targetProc;
            clearTarget.Text = string.IsNullOrEmpty(display) ? "清除目标窗口" : $"清除目标窗口 ({display})";
            clearTarget.Visible = !string.IsNullOrEmpty(_targetProc);
            if (_autoVisibilityMenuItem != null) _autoVisibilityMenuItem.Checked = _data.AutoVisibilityEnabled;
            if (_edgeSnapMenuItem != null) _edgeSnapMenuItem.Checked = _data.EdgeSnapEnabled;
            if (_autoHideMenuItem != null)
            {
                _autoHideMenuItem.Checked = _data.AutoHideEnabled;
                _autoHideMenuItem.Enabled = _data.EdgeSnapEnabled;
            }
            lockItem.Text = _winLocked ? "✓ 窗口已锁定" : "窗口锁定/解锁";
        };
        m.Closed += (_, _) =>
        {
            _interactionHold = false;
            _lastInteractionUtc = DateTime.UtcNow;
        };
        UiTheme.Apply(m);
        return m;
    }

    private void OnWidgetContextMenu(VirtualButtonWidget widget, Point location)
    {
        var vbtn = widget.VirtualButton;
        var menu = new ContextMenuStrip();
        var nameItem = menu.Items.Add($"[ {vbtn.Name} ]");
        nameItem.Enabled = false;
        menu.Items.Add("-");
        var moveMenu = new ToolStripMenuItem("按钮排序");
        moveMenu.DropDownItems.Add("上移", null, (_, _) => MoveButton(widget, -1));
        moveMenu.DropDownItems.Add("下移", null, (_, _) => MoveButton(widget, 1));
        moveMenu.DropDownItems.Add("移到最前", null, (_, _) => MoveButtonTo(widget, 0));
        moveMenu.DropDownItems.Add("移到最后", null, (_, _) => MoveButtonTo(widget, _btnManager.Buttons.Count - 1));
        menu.Items.Add(moveMenu);
        menu.Items.Add("-");
        menu.Items.Add("修改按钮名称", null, (_, _) =>
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("输入新的按钮名称:", "修改按钮名称", vbtn.Name);
            if (!string.IsNullOrWhiteSpace(input) && input.Trim() != vbtn.Name)
            {
                var oldName = vbtn.Name;
                vbtn.Name = input.Trim();
                widget.UpdateButton(vbtn);
                // Sync sequences that were bound to this button
                var compositeOld = $"{_data.Name}/{oldName}";
                var compositeNew = $"{_data.Name}/{vbtn.Name}";
                bool seqChanged = false;
                foreach (var seq in _sequences)
                {
                    if (seq.TriggerVkButtonName?.Trim() == compositeOld)
                    {
                        seq.TriggerVkButtonName = compositeNew; seqChanged = true;
                    }
                    else if (seq.TriggerVkButtonName?.Trim() == oldName)
                    {
                        seq.TriggerVkButtonName = vbtn.Name; seqChanged = true;
                    }
                }
                SaveLayout();
                if (seqChanged)
                    _sequencesChangedCallback?.Invoke();
            }
        });
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
        var gapMenu = new ToolStripMenuItem("按钮间距");
        gapMenu.DropDownItems.Add("增加间距 (+10)", null, (_, _) => { vbtn.ExtraGap += 10; SaveLayout(); RecalculateSize(); });
        gapMenu.DropDownItems.Add("减少间距 (-10)", null, (_, _) => { vbtn.ExtraGap = Math.Max(0, vbtn.ExtraGap - 10); SaveLayout(); RecalculateSize(); });
        gapMenu.DropDownItems.Add("-");
        gapMenu.DropDownItems.Add("自定义间距...", null, (_, _) =>
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("当前按钮后的额外间距 (像素):", "设置间距", vbtn.ExtraGap.ToString());
            if (int.TryParse(input, out var val) && val >= 0) { vbtn.ExtraGap = val; SaveLayout(); RecalculateSize(); }
        });
        menu.Items.Add(gapMenu);
        menu.Items.Add("强制停止", null, (_, _) => _loopExecutor.ForceStopLoop(vbtn.Id));
        menu.Items.Add("-");
        menu.Items.Add("删除当前按钮", null, (_, _) =>
        {
            _loopExecutor.ForceStopLoop(vbtn.Id); _btnManager.RemoveButton(vbtn.Id); SaveLayout();
        });
        menu.Opened += (_, _) =>
        {
            _interactionHold = true;
            _lastInteractionUtc = DateTime.UtcNow;
            _loopExecutor.PauseForMenu(vbtn.Id);
        };
        menu.Closed += (_, _) =>
        {
            _interactionHold = false;
            _lastInteractionUtc = DateTime.UtcNow;
            _loopExecutor.ResumeFromMenu(vbtn.Id);
        };
        UiTheme.Apply(menu);
        menu.Show(widget, location);
    }

    private void MoveButton(VirtualButtonWidget widget, int delta)
    {
        var buttons = _btnManager.Buttons.ToList();
        var index = buttons.FindIndex(b => b.Id == widget.VirtualButton.Id);
        if (index < 0) return;
        MoveButtonTo(widget, index + delta);
    }

    private void MoveButtonTo(VirtualButtonWidget widget, int targetIndex)
    {
        _btnManager.MoveButton(widget.VirtualButton.Id, targetIndex);
        SaveLayout();
    }

    private void SetLoopInterval(VirtualButton vbtn, VirtualButtonWidget widget, int ms)
    {
        vbtn.LoopInterval = ms;
        var seq = _sequences.Find(s => s.Id == vbtn.BindActionId);
        if (seq != null) { seq.LoopIntervalMs = ms; _sequencesChangedCallback?.Invoke(); }
        widget.UpdateButton(vbtn); SaveLayout();
    }

    // ── Op / Lock ──

    private void SetOpacity(double val) { _opacityValue = val; Opacity = val; SaveLayout(); }

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
        PositionResizeGrip();
        SaveLayout();
    }

    public bool HasBoundButtons() => _btnManager.Buttons.Any(b => !string.IsNullOrEmpty(b.BindActionId));

    public bool IsNativeWindowVisible => IsHandleCreated && IsWindowVisible(Handle);

    public void ShowManually()
    {
        _manualHidden = false;
        _behaviorVisible = true;
        RestoreFromEdgeHide();

        // A newly created Form has no native handle until it is shown. Use the
        // normal manual path once to create/show it; existing hidden windows,
        // including those hidden by target behavior, use the non-activating path.
        if (!IsHandleCreated)
        {
            Show();
            StartBehaviorMonitoring();
        }
        else
        {
            ShowFromBehavior();
            StartBehaviorMonitoring();
        }

        _lastInteractionUtc = DateTime.UtcNow;
    }

    public void HideManually()
    {
        _manualHidden = true;
        _behaviorVisible = false;
        _interactionHold = false;
        _loopExecutor.StopAll();
        Hide();
    }

    // ── Scale ──

    private void SetButtonScale(float factor)
    {
        _buttonScale = Math.Clamp(factor, 0.4f, 2.0f);
        UpdateScale();
        RecalculateSize(false);
        SaveLayout();
    }

    private void SetLayoutMetric(bool padding)
    {
        var current = padding ? _innerPadding : _buttonGap;
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            padding ? "输入四边内边距 (2-64):" : "输入按钮间距 (0-32):",
            padding ? "内边距" : "按钮间距", current.ToString());
        if (!int.TryParse(input, out var value)) return;
        if (padding) _innerPadding = Math.Clamp(value, 2, 64);
        else _buttonGap = Math.Clamp(value, 0, 32);
        RecalculateSize();
        SaveLayout();
    }

    private void AddLayoutModeItem(ToolStripMenuItem menu, string text, VirtualLayoutSerializer.LayoutMode mode, int fixedCount = 0)
    {
        var item = menu.DropDownItems.Add(text);
        item.Click += (_, _) =>
        {
            _layoutMode = mode;
            if (mode == VirtualLayoutSerializer.LayoutMode.FixedColumns) _fixedColumns = fixedCount;
            if (mode == VirtualLayoutSerializer.LayoutMode.FixedRows) _fixedRows = fixedCount;
            ApplyLayoutMode();
            SaveLayout();
        };
    }

    private void ApplyLayoutMode()
    {
        _vertical = _layoutMode == VirtualLayoutSerializer.LayoutMode.SingleColumn;
        foreach (var w in _widgets.Values) w.VerticalMode = _vertical;
        RecalculateSize();
    }

    private void PositionResizeGrip()
    {
        if (!IsHandleCreated) return;
        _resizeGrip.Location = new Point(Math.Max(0, ClientSize.Width - _resizeGrip.Width), Math.Max(0, ClientSize.Height - _resizeGrip.Height));
        // 无标题栏锁定与尺寸锁定是两个独立状态；隐藏标题栏时仍应保留右下角缩放手柄。
        _resizeGrip.Visible = _data.ResizeHandleEnabled && !_windowSizeLocked;
        _resizeGrip.BringToFront();
        _resizeGrip.Invalidate();
    }

    private void ResizeGrip_Paint(object? sender, PaintEventArgs e)
    {
        if (!_resizeGrip.Visible) return;
        using var pen = new Pen(Color.FromArgb(210, Color.White), 2f);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        for (int i = 0; i < 3; i++)
        {
            int offset = 5 + i * 5;
            e.Graphics.DrawLine(pen, _resizeGrip.Width - offset, _resizeGrip.Height - 2,
                _resizeGrip.Width - 2, _resizeGrip.Height - offset);
        }
    }

    private void ResizeGrip_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _windowSizeLocked) return;
        _resizingWindow = true;
        _interactionHold = true;
        _resizeStartCursor = Cursor.Position;
        _resizeStartSize = Size;
        _lastInteractionUtc = DateTime.UtcNow;
    }

    private void ResizeGrip_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_resizingWindow) return;
        var cursor = Cursor.Position;
        var deltaX = cursor.X - _resizeStartCursor.X;
        var deltaY = cursor.Y - _resizeStartCursor.Y;
        Size = new Size(Math.Max(160, _resizeStartSize.Width + deltaX), Math.Max(100, _resizeStartSize.Height + deltaY));
        RecalculateSize(false);
    }

    private void ResizeGrip_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !_resizingWindow) return;
        _resizingWindow = false;
        _interactionHold = false;
        _windowSizeInitialized = true;
        SnapToScreenEdge();
        RecalculateSize(false);
        SaveLayout();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        UpdateScale();
        RecalculateSize();
    }

    // ── Title ──

    public void UpdateWindowTitle() => UpdateTitle();

    /// <summary>Reload skin from serializer data and reapply.</summary>
    public void ReloadSkin()
    {
        var global = _serializer.LoadAll();
        OperationLogger.Info($"[DIAG] VKWindow.ReloadSkin: loaded {global.Windows.Count} windows");
        var freshData = global.Windows.Find(w => w.Name == _data.Name);
        if (freshData != null)
        {
            OperationLogger.Info($"[DIAG] VKWindow.ReloadSkin: found window name=\"{freshData.Name}\" SkinPath=\"{freshData.SkinPath}\" (current _data.SkinPath=\"{_data.SkinPath}\")");
            if (freshData.SkinPath != _data.SkinPath)
            {
                _data.SkinPath = freshData.SkinPath;
                OperationLogger.Info($"[DIAG] VKWindow.ReloadSkin: updated _data.SkinPath to \"{_data.SkinPath}\"");
            }
        }
        else
        {
            OperationLogger.Warn($"[DIAG] VKWindow.ReloadSkin: window \"{_data.Name}\" NOT FOUND in serialized data");
        }
        _skinLoader = new VkSkinLoader(_data.SkinPath);
        _skinLoader.Load();
        OperationLogger.Info($"[DIAG] VKWindow.ReloadSkin: _skinLoader.HasSkin={_skinLoader.HasSkin}");
        ApplyWindowSkin();
        foreach (var w in _widgets.Values)
            w.ApplySkin(_skinLoader);
        var bgImg = _skinLoader.GetWindowBackground();
        OperationLogger.Info($"[DIAG] VKWindow.ReloadSkin: bgImage={(bgImg != null ? $"loaded ({bgImg.Width}x{bgImg.Height})" : "null")}");
        Invalidate();
    }

    /// <summary>Reload BindActionId from saved layout data after SyncVkButtonBindings.</summary>
    public void RefreshBindingsFromSerializer()
    {
        var global = _serializer.LoadAll();
        var winData = global.Windows.Find(w => w.Name == _data.Name);
        if (winData == null) return;
        int updated = 0;
        foreach (var liveBtn in _btnManager.Buttons)
        {
            var savedBtn = winData.Buttons.Find(b => b.Id == liveBtn.Id);
            if (savedBtn != null && savedBtn.BindActionId != liveBtn.BindActionId)
            {
                liveBtn.BindActionId = savedBtn.BindActionId;
                updated++;
            }
        }
        OperationLogger.Info($"[DIAG] VKWindow.RefreshBindings: updated {updated} buttons for \"{_data.Name}\"");
    }

    private void UpdateTitle()
    {
        Text = (_targetProc != null ? $"[{_targetTitle ?? _targetProc}] " : "") + $"{_data.Name} ({_widgets.Count})";
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
                    _data.AutoVisibilityEnabled = true;
                    _manualHidden = false;
                    _behaviorVisible = true;
                    SaveLayout();
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
        _targetProc = null; _targetTitle = null;
        _data.AutoVisibilityEnabled = false;
        _manualHidden = false;
        _behaviorVisible = true;
        ShowFromBehavior();
        SaveLayout(); UpdateTitle();
    }

    private IntPtr ResolveTargetWindow()
    {
        if (string.IsNullOrEmpty(_targetProc))
        {
            OperationLogger.Info($"[DIAG] VKTarget: no target configured");
            return IntPtr.Zero;
        }
        var procs = System.Diagnostics.Process.GetProcessesByName(_targetProc);
        OperationLogger.Info($"[DIAG] VKTarget: proc=\"{_targetProc}\" title=\"{_targetTitle ?? ""}\" found={procs.Length} processes");
        IntPtr firstValidHwnd = IntPtr.Zero;
        foreach (var proc in procs)
        {
            var hwnd = proc.MainWindowHandle;
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) continue;
            if (firstValidHwnd == IntPtr.Zero) firstValidHwnd = hwnd;
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
            OperationLogger.Info($"[DIAG] VKTarget: matched hwnd=0x{hwnd:X8} proc=\"{_targetProc}\"");
            return hwnd;
        }
        // Title exact match failed — fall back to process-only match
        if (firstValidHwnd != IntPtr.Zero)
        {
            OperationLogger.Info($"[DIAG] VKTarget: title match failed, fallback to process-only hwnd=0x{firstValidHwnd:X8}");
            return firstValidHwnd;
        }
        OperationLogger.Warn($"[DIAG] VKTarget: no valid hwnd found for \"{_targetProc}\" (found {procs.Length} processes, none had matching window)");
        return IntPtr.Zero;
    }

    // ── Button mgmt ──

    private void AddButton(VirtualButtonStyle style) { _btnManager.AddButton(style); RebuildWidgets(); SaveLayout(); }

    private void RebuildWidgets()
    {
        _panel.SuspendLayout();
        _panel.Controls.Clear();
        _widgets.Clear();
        var buttons = _btnManager.Buttons;
        for (int i = 0; i < buttons.Count; i++)
        {
            var w = new VirtualButtonWidget(buttons[i]);
            w.ApplySkin(_skinLoader);
            w.IsFirstInRow = i == 0; w.IsLastInRow = i == buttons.Count - 1;
            w.Clicked += OnButtonClicked;
            w.WindowDragRequested += OnWidgetWindowDragRequested;
            w.WindowDragEnded += _ => OnWidgetWindowDragEnded();
            w.ContextMenuRequested += OnWidgetContextMenu;
            w.LoopCountEdited += OnLoopCountEdited;
            w.VerticalMode = _vertical;
            _panel.Controls.Add(w);
            _widgets[buttons[i].Id] = w;
        }
        UpdateScale();
        RecalculateSize();
        _panel.ResumeLayout();
        _panel.Invalidate();
        UpdateTitle();
        OperationLogger.Info($"VKWindow.Rebuild: {_widgets.Count} widgets");
    }

    private void UpdateScale()
    {
        float eff = _buttonScale * (DeviceDpi / 96f);
        foreach (var w in _widgets.Values) { w.ScaleFactor = eff; w.UpdateSize(); }
    }

    private void RecalculateSize(bool resizeWindow = false)
    {
        if (_layouting) return;
        _layouting = true;
        try
        {
            int ncW = Width - ClientSize.Width;
            int ncH = Height - ClientSize.Height;
            float dpi = DeviceDpi / 96f;
            float S = _buttonScale * dpi;
            int margin = Math.Max(2, (int)(_innerPadding * dpi));
            int gap = Math.Max(0, (int)(_buttonGap * dpi));
            int n = _widgets.Count;

            if (n == 0)
            {
                if (resizeWindow) Size = new Size(Math.Max(160, margin * 2 + 20 + ncW), Math.Max(100, margin * 2 + 20 + ncH));
                PositionResizeGrip();
                return;
            }

            var items = _widgets.Values.ToList();
            int columns = _layoutMode switch
            {
                VirtualLayoutSerializer.LayoutMode.SingleRow => n,
                VirtualLayoutSerializer.LayoutMode.SingleColumn => 1,
                VirtualLayoutSerializer.LayoutMode.FixedColumns => Math.Clamp(_fixedColumns, 1, n),
                VirtualLayoutSerializer.LayoutMode.FixedRows => Math.Max(1, (int)Math.Ceiling(n / (double)Math.Max(1, _fixedRows))),
                _ => Math.Max(1, (int)Math.Ceiling(Math.Sqrt(n * Math.Max(0.5, (double)ClientSize.Width / Math.Max(1, ClientSize.Height)))) )
            };
            int rows = (int)Math.Ceiling(n / (double)columns);

            if (resizeWindow)
            {
                var rowWidths = new int[rows];
                for (int row = 0; row < rows; row++)
                {
                    var rowItems = items.Skip(row * columns).Take(columns);
                    rowWidths[row] = rowItems.Sum(w => Math.Max(1, (int)(BaseBtnWidth(w.VirtualButton.StyleType) * S)) + (int)(w.VirtualButton.ExtraGap * S))
                        + Math.Max(0, rowItems.Count() - 1) * gap;
                }
                int contentW = rowWidths.Max();
                int contentH = rows * Math.Max(1, (int)(BASE_BTN_H * S)) + Math.Max(0, rows - 1) * gap;
                Size = new Size(Math.Max(160, margin * 2 + contentW + ncW), Math.Max(100, margin * 2 + contentH + ncH));
            }

            _panel.WrapContents = true;
            _panel.FlowDirection = FlowDirection.LeftToRight;
            _panel.Padding = new Padding(margin);
            _panel.AutoScroll = false;
            for (int i = 0; i < items.Count; i++)
            {
                var w = items[i];
                int row = i / columns;
                int rowStart = row * columns;
                int rowEnd = Math.Min(items.Count, rowStart + columns);
                int extra = Math.Max(0, (int)(w.VirtualButton.ExtraGap * S));
                int rightGap = i < rowEnd - 1 ? gap : 0;
                int bottomGap = row < rows - 1 ? gap : 0;
                w.Margin = new Padding(0, 0, rightGap + extra, bottomGap);
                w.IsFirstInRow = i == rowStart;
                w.IsLastInRow = i == rowEnd - 1;
            }
            _panel.PerformLayout();
            ApplyActualRowFlags(items);
            PositionResizeGrip();
        }
        finally
        {
            _layouting = false;
        }
    }

    private void ApplyActualRowFlags(IReadOnlyList<VirtualButtonWidget> items)
    {
        if (items.Count == 0) return;
        var rowStarts = new List<int>();
        var rowEnds = new List<int>();
        int previousTop = int.MinValue;
        for (int i = 0; i < items.Count; i++)
        {
            var bounds = items[i].Bounds;
            if (i == 0 || bounds.Top != previousTop)
                rowStarts.Add(i);
            previousTop = bounds.Top;
        }
        for (int row = 0; row < rowStarts.Count; row++)
        {
            int end = row + 1 < rowStarts.Count ? rowStarts[row + 1] - 1 : items.Count - 1;
            rowEnds.Add(end);
        }
        for (int i = 0; i < items.Count; i++)
        {
            items[i].IsFirstInRow = rowStarts.Contains(i);
            items[i].IsLastInRow = rowEnds.Contains(i);
        }
    }

    // ── Button events ──

    private async void OnButtonClicked(VirtualButtonWidget widget)
    {
        _lastInteractionUtc = DateTime.UtcNow;
        var vbtn = widget.VirtualButton;
        OperationLogger.Info($"[DIAG] VKClick: button=\"{vbtn.Name}\" bindActionId=\"{vbtn.BindActionId}\" style={vbtn.StyleType} isPlaying={_player.IsPlaying}");

        if (SequenceEditor.IsVkPickMode)
        {
            var seq = _bindingManager.ResolveBinding(vbtn, _sequences);
            SequenceEditor.ReceiveVkPick(vbtn.Name, _data.Name, seq?.TriggerHotkey);
            return;
        }

        if (vbtn.StyleType == VirtualButtonStyle.LoopIcon && vbtn.LoopEnabled)
        {
            // LoopIcon: second click requests outer-loop stop after current round.
            if (_loopExecutor.IsLooping(vbtn.Id)) { _loopExecutor.StopLoop(vbtn.Id); return; }
            var seq = _bindingManager.ResolveBinding(vbtn, _sequences);
            if (seq != null) { _loopExecutor.StartLoop(vbtn, seq); widget.IsActive = true; }
            return;
        }

        // Toggle: if already playing this button, stop (after current round)
        if (_player.IsPlaying) { _player.Stop(); return; }

        var sequence = _bindingManager.ResolveBinding(vbtn, _sequences);
        if (sequence == null) { OperationLogger.Warn($"[DIAG] VKClick: no binding for \"{vbtn.Name}\" bindActionId=\"{vbtn.BindActionId}\""); return; }
        OperationLogger.Info($"[DIAG] VKBinding: button=\"{vbtn.Name}\" bindActionId=\"{vbtn.BindActionId}\" -> seq=\"{sequence.Name}\" ({sequence.Id})");

        var hwnd = ResolveTargetWindow();
        if (hwnd == IntPtr.Zero)
        {
            OperationLogger.Info($"[DIAG] VKPlay: scheme=DirectPlay target=null seq=\"{sequence.Name}\"");
            _ = _player.Play(sequence);
            return;
        }

        // Activate target window, wait for it to settle, then play
        SetForegroundWindow(hwnd);
        await Task.Delay(300);
        OperationLogger.Info($"[DIAG] VKPlay: scheme=ActivateWindow hwnd=0x{hwnd:X8} seq=\"{sequence.Name}\"");
        _ = _player.Play(sequence, skipInitialDelay: true);
    }

    private void OnLoopEnded(string buttonId)
    {
        if (IsDisposed) return;
        void ClearActive()
        {
            if (_widgets.TryGetValue(buttonId, out var widget))
                widget.IsActive = false;
        }

        if (InvokeRequired)
            BeginInvoke((Action)ClearActive);
        else
            ClearActive();
    }

    private void OnPanelWindowDrag(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle || _draggingWindow)
            OnWidgetWindowDragRequested(null!, e);
    }

    private void OnWidgetWindowDragEnded()
    {
        if (!_draggingWindow) return;
        _draggingWindow = false;
        _interactionHold = false;
        SnapToScreenEdge();
        SaveLayout();
    }

    private void OnWidgetWindowDragRequested(VirtualButtonWidget widget, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle && !_draggingWindow)
        {
            _draggingWindow = true;
            _interactionHold = true;
            _lastInteractionUtc = DateTime.UtcNow;
            GetCursorPos(out _dragStartCursor);
            _dragStartLocation = Location;
            return;
        }

        if (_draggingWindow)
        {
            GetCursorPos(out var cursor);
            Location = new Point(
                _dragStartLocation.X + cursor.X - _dragStartCursor.X,
                _dragStartLocation.Y + cursor.Y - _dragStartCursor.Y);
            _lastInteractionUtc = DateTime.UtcNow;
            return;
        }

    }

    private void SnapToScreenEdge()
    {
        if (!_data.EdgeSnapEnabled || IsDisposed || !IsHandleCreated) return;
        var area = Screen.FromRectangle(Bounds).WorkingArea;
        var distance = Math.Max(0, _data.EdgeSnapDistance);
        var x = Left;
        var y = Top;
        if (Math.Abs(Left - area.Left) <= distance) x = area.Left;
        else if (Math.Abs(area.Right - Right) <= distance) x = area.Right - Width;
        if (Math.Abs(Top - area.Top) <= distance) y = area.Top;
        else if (Math.Abs(area.Bottom - Bottom) <= distance) y = area.Bottom - Height;
        Location = new Point(x, y);
    }

    private bool IsTargetForeground()
    {
        if (string.IsNullOrWhiteSpace(_targetProc)) return false;
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || hwnd == Handle) return false;
        GetWindowThreadProcessId(hwnd, out var pid);
        try
        {
            using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
            return string.Equals(proc.ProcessName, _targetProc, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private void StartBehaviorMonitoring()
    {
        if (IsDisposed) return;
        _foregroundWatcher.Start();
        _behaviorTimer.Start();
        EvaluateWindowBehavior();
    }

    private void EvaluateWindowBehavior()
    {
        if (IsDisposed || _isBeingDeleted || _manualHidden || _interactionHold || _draggingWindow) return;
        if (_data.AutoVisibilityEnabled && string.IsNullOrWhiteSpace(_targetProc))
        {
            if (!_behaviorVisible) ShowFromBehavior();
            return;
        }

        var targetForeground = !_data.AutoVisibilityEnabled || IsTargetForeground();
        if (!targetForeground)
        {
            HideFromBehavior();
            return;
        }

        if (!_behaviorVisible)
            ShowFromBehavior();

        if (!Visible || !_data.AutoHideEnabled || !_data.EdgeSnapEnabled || _edgeHidden) return;
        if ((DateTime.UtcNow - _lastInteractionUtc).TotalMilliseconds >= Math.Max(100, _data.AutoHideDelayMs))
            HideToEdge();
    }

    private void ShowFromBehavior()
    {
        RestoreFromEdgeHide();
        _behaviorVisible = true;
        if (!IsHandleCreated) return;
        if (!IsWindowVisible(Handle))
            ShowWindow(Handle, SwShownoactivate);
        SetWindowPos(
            Handle,
            _topMostState ? HwndTopMost : HwndTop,
            0, 0, 0, 0,
            SwpNoActivate | SwpShowWindow | SwpNoMove | SwpNoSize);
    }

    private void HideFromBehavior()
    {
        _behaviorVisible = false;
        if (IsHandleCreated && IsWindowVisible(Handle))
            ShowWindow(Handle, 0);
    }

    private void HideToEdge()
    {
        if (_edgeHidden || !Visible) return;
        var area = Screen.FromRectangle(Bounds).WorkingArea;
        _locationBeforeEdgeHide = Location;
        var peek = Math.Max(1, _data.AutoHidePeekPixels);
        var x = Left;
        var y = Top;
        if (Math.Abs(Left - area.Left) <= Math.Max(1, _data.EdgeSnapDistance)) x = area.Left - Width + peek;
        else if (Math.Abs(area.Right - Right) <= Math.Max(1, _data.EdgeSnapDistance)) x = area.Right - peek;
        else if (Math.Abs(Top - area.Top) <= Math.Max(1, _data.EdgeSnapDistance)) y = area.Top - Height + peek;
        else if (Math.Abs(area.Bottom - Bottom) <= Math.Max(1, _data.EdgeSnapDistance)) y = area.Bottom - peek;
        else return;
        _edgeHidden = true;
        _behaviorVisible = true;
        Location = new Point(x, y);
    }

    private void RestoreFromEdgeHide()
    {
        if (!_edgeHidden) return;
        _edgeHidden = false;
        _behaviorVisible = true;
        Location = _locationBeforeEdgeHide;
        _lastInteractionUtc = DateTime.UtcNow;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        RestoreFromEdgeHide();
        _lastInteractionUtc = DateTime.UtcNow;
    }

    private void OnLoopCountEdited(VirtualButtonWidget widget, int count)
    {
        var vbtn = widget.VirtualButton;
        var seq = _sequences.Find(s => s.Id == vbtn.BindActionId);
        if (seq != null) { seq.LoopIntervalMs = vbtn.LoopInterval; seq.LoopCount = vbtn.LoopCount; _sequencesChangedCallback?.Invoke(); }
        SaveLayout();
    }

    // ── Skin ──

    private Image? _bgImage;
    private static readonly Color ChromaKey = Color.FromArgb(0x0D, 0x0E, 0x0D); // Near-black chroma key (no visible fringe)

    private void ApplyWindowSkin()
    {
        _bgImage = _skinLoader.GetWindowBackground();
        if (_bgImage != null)
        {
            // Use dark chroma key for OS-level transparency (see-through to desktop)
            // Dark color avoids visible fringe from anti-aliased edges
            BackColor = ChromaKey;
            TransparencyKey = ChromaKey;
            _panel.BackColor = ChromaKey;
            _panel.BackgroundImage = null;
            _panel.Paint -= Panel_PaintBg;
            _panel.Paint += Panel_PaintBg;
            return;
        }

        _panel.Paint -= Panel_PaintBg;
        TransparencyKey = Color.Empty;
        _panel.BackgroundImage = null;
        var fallback = _skinLoader.GetColor("window_bg", Color.FromArgb(0x0D, 0x0D, 0x0D));
        BackColor = fallback;
        _panel.BackColor = fallback;
    }

    private void Panel_PaintBg(object? sender, PaintEventArgs e)
    {
        if (_bgImage == null) return;
        VkSkinLoader.DrawNineSlice(e.Graphics, _bgImage, _panel.ClientRectangle, 10);
    }

    // ── Layout persistence ──

    private void SaveLayout()
    {
        FlushToData();
        var global = _serializer.LoadAll();
        var idx = global.Windows.FindIndex(w => w.Name == _data.Name);
        if (idx >= 0)
            global.Windows[idx] = _data;
        _serializer.SaveAll(global);
    }

    private void FlushToData()
    {
        _data.WindowX = Left; _data.WindowY = Top;
        _data.WindowWidth = Width; _data.WindowHeight = Height;
        _data.TopMost = _topMostState; _data.PositionLocked = _posLocked; _data.WindowLocked = _winLocked;
        _data.TargetProcessName = _targetProc; _data.TargetWindowTitle = _targetTitle;
        _data.VerticalMode = _layoutMode == VirtualLayoutSerializer.LayoutMode.SingleColumn;
        _data.LayoutMode = _layoutMode; _data.FixedColumns = _fixedColumns; _data.FixedRows = _fixedRows;
        _data.ButtonScale = _buttonScale; _data.InnerPadding = _innerPadding; _data.ButtonGap = _buttonGap;
        // ScaleFactor remains only as a legacy mirror for older readers; it is never read at runtime when ButtonScale exists.
        _data.ScaleFactor = _buttonScale;
        _data.WindowSizeLocked = _windowSizeLocked;
        _data.Buttons = [.. _btnManager.Buttons];
    }

    private void ApplyLayoutData()
    {
        _targetProc = _data.TargetProcessName;
        _targetTitle = _data.TargetWindowTitle;
        _vertical = _data.VerticalMode;
        _layoutMode = _data.LayoutMode;
        if (_layoutMode == VirtualLayoutSerializer.LayoutMode.AutoGrid && _data.VerticalMode)
            _layoutMode = VirtualLayoutSerializer.LayoutMode.SingleColumn;
        _fixedColumns = Math.Clamp(_data.FixedColumns, 0, 12);
        _fixedRows = Math.Clamp(_data.FixedRows, 0, 12);
        _buttonScale = Math.Clamp(_data.ButtonScale > 0 ? _data.ButtonScale : _data.ScaleFactor, 0.4f, 2.0f);
        _innerPadding = Math.Clamp(_data.InnerPadding, 2, 64);
        _buttonGap = Math.Clamp(_data.ButtonGap, 0, 32);
        _windowSizeLocked = _data.WindowSizeLocked;
        _windowSizeInitialized = _data.WindowWidth > 0 && _data.WindowHeight > 0;
        _data.EdgeSnapDistance = Math.Clamp(_data.EdgeSnapDistance, 0, 200);
        _data.AutoHideDelayMs = Math.Clamp(_data.AutoHideDelayMs, 100, 60000);
        _data.AutoHidePeekPixels = Math.Clamp(_data.AutoHidePeekPixels, 1, 32);
        _buttonScale = Math.Clamp(_data.ButtonScale > 0 ? _data.ButtonScale : _data.ScaleFactor, 0.4f, 2.0f);
        _innerPadding = Math.Clamp(_data.InnerPadding, 2, 64);
        _buttonGap = Math.Clamp(_data.ButtonGap, 0, 32);
        _fixedColumns = Math.Clamp(_data.FixedColumns, 0, 12);
        _fixedRows = Math.Clamp(_data.FixedRows, 0, 12);
        _layoutMode = _data.LayoutMode == VirtualLayoutSerializer.LayoutMode.AutoGrid && _data.VerticalMode
            ? VirtualLayoutSerializer.LayoutMode.SingleColumn : _data.LayoutMode;
        _windowSizeLocked = _data.WindowSizeLocked;
        _windowSizeInitialized = _data.WindowWidth > 0 && _data.WindowHeight > 0;
        _panel.Padding = new Padding(Math.Max(2, _innerPadding));
        PositionResizeGrip();

        if (_data.Buttons.Count > 0)
        {
            var savedSize = new Size(Math.Max(160, _data.WindowWidth), Math.Max(100, _data.WindowHeight));
            Size = savedSize;
            _windowSizeInitialized = true;
            PositionResizeGrip();
            var savedLoc = new Point(_data.WindowX, _data.WindowY);
            var testRect = new Rectangle(savedLoc, savedSize);
            Location = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(testRect))
                ? savedLoc : new Point(Screen.PrimaryScreen!.WorkingArea.Width / 2 - Width / 2,
                    Screen.PrimaryScreen!.WorkingArea.Height / 2 - Height / 2);
            _topMostState = _data.TopMost; TopMost = _topMostState;
            _posLocked = _data.PositionLocked; _winLocked = _data.WindowLocked;
            foreach (var w in _widgets.Values) w.AllowDragging = !_posLocked;
            _panel.WrapContents = true;
            _panel.FlowDirection = FlowDirection.LeftToRight;
            if (_winLocked) { FormBorderStyle = FormBorderStyle.None; ControlBox = false; Text = ""; }
            else { FormBorderStyle = FormBorderStyle.FixedSingle; ControlBox = true; }
            UpdateScale();
            RecalculateSize(!_windowSizeInitialized);
        }
        else
        {
            Size = new Size(320, 100);
            var s = Screen.PrimaryScreen;
            if (s != null) Location = new Point((s.WorkingArea.Width - Width) / 2, (s.WorkingArea.Height - Height) / 2);
        }
        UpdateTitle();
    }

    public void ReloadFromData()
    {
        _buttonScale = Math.Clamp(_data.ButtonScale > 0 ? _data.ButtonScale : _data.ScaleFactor, 0.4f, 2.0f);
        _innerPadding = Math.Clamp(_data.InnerPadding, 2, 64);
        _buttonGap = Math.Clamp(_data.ButtonGap, 0, 32);
        _fixedColumns = Math.Clamp(_data.FixedColumns, 0, 12);
        _fixedRows = Math.Clamp(_data.FixedRows, 0, 12);
        _layoutMode = _data.LayoutMode == VirtualLayoutSerializer.LayoutMode.AutoGrid && _data.VerticalMode
            ? VirtualLayoutSerializer.LayoutMode.SingleColumn : _data.LayoutMode;
        _windowSizeLocked = _data.WindowSizeLocked;
        _windowSizeInitialized = _data.WindowWidth > 0 && _data.WindowHeight > 0;
        _panel.Padding = new Padding(Math.Max(2, _innerPadding));
        PositionResizeGrip();
        _btnManager.LoadFrom(_data.Buttons);
        _vertical = _data.VerticalMode;
        _topMostState = _data.TopMost; TopMost = _topMostState;
        _posLocked = _data.PositionLocked; _winLocked = _data.WindowLocked;
        _targetProc = _data.TargetProcessName;
        _targetTitle = _data.TargetWindowTitle;
        _data.EdgeSnapDistance = Math.Clamp(_data.EdgeSnapDistance, 0, 200);
        _data.AutoHideDelayMs = Math.Clamp(_data.AutoHideDelayMs, 100, 60000);
        _data.AutoHidePeekPixels = Math.Clamp(_data.AutoHidePeekPixels, 1, 32);
        foreach (var w in _widgets.Values) w.AllowDragging = !_posLocked;
        _panel.WrapContents = true;
        _panel.FlowDirection = FlowDirection.LeftToRight;
        if (_winLocked) { FormBorderStyle = FormBorderStyle.None; ControlBox = false; Text = ""; }
        else { FormBorderStyle = FormBorderStyle.FixedSingle; ControlBox = true; UpdateTitle(); }
        UpdateScale();
        RecalculateSize();
    }
}
