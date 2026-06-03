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
    private ToolStripMenuItem? _orientMenuItem;
    private float _scaleFactor = 1.0f;
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

    private float GetEffectiveScale() => _scaleFactor * (DeviceDpi / 96f);

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

        var menu = BuildBlankMenu();
        _panel.ContextMenuStrip = menu;

        _skinLoader = new VkSkinLoader(_data.SkinPath);
        _skinLoader.Load();
        _btnManager.ButtonsChanged += RebuildWidgets;
        _btnManager.LoadFrom(_data.Buttons);
        ApplyWindowSkin();
        ApplyLayoutData();

        Shown += (_, _) => { if (_widgets.Count > 0) RecalculateSize(); };

        FormClosing += (_, e) =>
        {
            if (_isBeingDeleted) return;
            if (e.CloseReason == CloseReason.UserClosing)
            {
                _loopExecutor.StopAll(); SaveLayout(); e.Cancel = true; Hide();
            }
        };

        FormClosed += (_, _) =>
        {
            if (_isBeingDeleted)
                _loopExecutor.StopAll();
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
        _orientMenuItem = new ToolStripMenuItem("竖向模式");
        _orientMenuItem.Checked = _vertical;
        _orientMenuItem.Click += (_, _) => ToggleOrientation();
        m.Items.Add(_orientMenuItem);

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
        var lockItem = m.Items.Add(_winLocked ? "✓ 窗口已锁定" : "窗口锁定/解锁", null, (_, _) => ToggleWindowLock());
        m.Items.Add("-");
        m.Items.Add("关闭窗口", null, (_, _) => { _loopExecutor.StopAll(); SaveLayout(); Hide(); });
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
            var display = _targetTitle ?? _targetProc;
            clearTarget.Text = string.IsNullOrEmpty(display) ? "清除目标窗口" : $"清除目标窗口 ({display})";
            clearTarget.Visible = !string.IsNullOrEmpty(_targetProc);
            if (_orientMenuItem != null) _orientMenuItem.Checked = _vertical;
            lockItem.Text = _winLocked ? "✓ 窗口已锁定" : "窗口锁定/解锁";
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
        menu.Opened += (_, _) => _loopExecutor.PauseForMenu(vbtn.Id);
        menu.Closed += (_, _) => _loopExecutor.ResumeFromMenu(vbtn.Id);
        UiTheme.Apply(menu);
        menu.Show(widget, location);
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
        SaveLayout();
    }

    public bool HasBoundButtons() => _btnManager.Buttons.Any(b => !string.IsNullOrEmpty(b.BindActionId));

    // ── Orientation ──

    private void ToggleOrientation()
    {
        _vertical = !_vertical;
        _panel.FlowDirection = _vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight;
        _panel.AutoScroll = _vertical;
        foreach (var w in _widgets.Values)
            w.VerticalMode = _vertical;
        RecalculateSize();
        SaveLayout();
    }

    // ── Scale ──

    private void SetScale(float factor)
    {
        _scaleFactor = Math.Clamp(factor, 0.1f, 2.0f);
        int margin = Math.Max(1, (int)(BASE_MARGIN * GetEffectiveScale()));
        _panel.Padding = new Padding(margin);
        UpdateScale(); RecalculateSize(); SaveLayout();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        int margin = Math.Max(1, (int)(BASE_MARGIN * GetEffectiveScale()));
        _panel.Padding = new Padding(margin);
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
        _targetProc = null; _targetTitle = null; SaveLayout(); UpdateTitle();
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
            w.Dragged += OnButtonDragged;
            w.DragEnded += (ww, dx, dy) => OnButtonDragEnded(ww, dx, dy);
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
        float eff = GetEffectiveScale();
        foreach (var w in _widgets.Values) { w.ScaleFactor = eff; w.UpdateSize(); }
    }

    private void RecalculateSize()
    {
        int ncW = Width - ClientSize.Width;
        int ncH = Height - ClientSize.Height;
        float S = GetEffectiveScale();
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

        if (_vertical)
        {
            // Vertical: width = margin + max(button width + ExtraGap) + margin
            //           height = barH + margin + Σ(btnH + ExtraGap) + (N-1)×gap + margin
            int maxW = _widgets.Values.Max(w =>
            {
                var bw = Math.Max(1, (int)(BaseBtnWidth(w.VirtualButton.StyleType) * S));
                return bw + (int)(w.VirtualButton.ExtraGap * S);
            });
            int totalH = barH + margin + _widgets.Values.Sum(w =>
            {
                return btnH + (int)(w.VirtualButton.ExtraGap * S);
            }) + (n - 1) * gap + margin;
            Size = new Size(margin + maxW + margin + ncW, totalH + ncH);

            int halfGap = gap / 2;
            foreach (var w in _widgets.Values)
            {
                int eg = (int)(w.VirtualButton.ExtraGap * S);
                w.Margin = new Padding(0, halfGap, 0, halfGap + eg);
            }
        }
        else
        {
            // Horizontal: width = margin + Σ(button width + ExtraGap) + (N-1)×gap + margin
            //             height = titleBar + margin + btnH + margin
            int totalW = margin + _widgets.Values.Sum(w =>
            {
                var bw = Math.Max(1, (int)(BaseBtnWidth(w.VirtualButton.StyleType) * S));
                return bw + (int)(w.VirtualButton.ExtraGap * S);
            }) + (n - 1) * gap + margin;
            int totalH = barH + margin + btnH + margin;
            Size = new Size(totalW + ncW, totalH + ncH);

            int halfGap = gap / 2;
            foreach (var w in _widgets.Values)
            {
                int eg = (int)(w.VirtualButton.ExtraGap * S);
                w.Margin = new Padding(halfGap, 0, halfGap + eg, 0);
            }
        }
    }

    // ── Button events ──

    private async void OnButtonClicked(VirtualButtonWidget widget)
    {
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

    private void OnButtonDragged(VirtualButtonWidget widget, int dx, int dy)
    {
        // Drag tracked for reorder on mouse-up via DragEnded
    }

    private void OnButtonDragEnded(VirtualButtonWidget widget, int dx, int dy)
    {
        var vbtn = widget.VirtualButton;
        float effScale = GetEffectiveScale();

        int delta = _vertical ? dy : dx;
        if (Math.Abs(delta) < 30 * effScale) return;

        var buttons = _btnManager.Buttons.ToList();
        var idx = buttons.FindIndex(b => b.Id == vbtn.Id);
        if (idx < 0) return;

        var steps = Math.Max(1, Math.Abs(delta) / (int)(60 * effScale));
        int newIdx = delta > 0
            ? Math.Min(buttons.Count - 1, idx + steps)
            : Math.Max(0, idx - steps);

        if (newIdx != idx)
        {
            _btnManager.MoveButton(vbtn.Id, newIdx);
        }
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
        _data.VerticalMode = _vertical; _data.ScaleFactor = _scaleFactor;
        _data.Buttons = [.. _btnManager.Buttons];
    }

    private void ApplyLayoutData()
    {
        _targetProc = _data.TargetProcessName;
        _targetTitle = _data.TargetWindowTitle;
        _vertical = _data.VerticalMode;
        _scaleFactor = _data.ScaleFactor > 0 ? _data.ScaleFactor : 1.0f;
        int margin = Math.Max(1, (int)(BASE_MARGIN * GetEffectiveScale()));
        _panel.Padding = new Padding(margin);

        if (_data.Buttons.Count > 0)
        {
            var savedLoc = new Point(_data.WindowX, _data.WindowY);
            var testRect = new Rectangle(savedLoc, new Size(Math.Max(_data.WindowWidth, 100), Math.Max(_data.WindowHeight, 100)));
            Location = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(testRect))
                ? savedLoc : new Point(Screen.PrimaryScreen!.WorkingArea.Width / 2 - Width / 2,
                    Screen.PrimaryScreen!.WorkingArea.Height / 2 - Height / 2);
            _topMostState = _data.TopMost; TopMost = _topMostState;
            _posLocked = _data.PositionLocked; _winLocked = _data.WindowLocked;
            foreach (var w in _widgets.Values) w.AllowDragging = !_posLocked;
            _panel.WrapContents = false;
            _panel.FlowDirection = _vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight;
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

    public void ReloadFromData()
    {
        _scaleFactor = _data.ScaleFactor > 0 ? _data.ScaleFactor : 1.0f;
        int margin = Math.Max(1, (int)(BASE_MARGIN * GetEffectiveScale()));
        _panel.Padding = new Padding(margin);
        _btnManager.LoadFrom(_data.Buttons);
        _vertical = _data.VerticalMode;
        _topMostState = _data.TopMost; TopMost = _topMostState;
        _posLocked = _data.PositionLocked; _winLocked = _data.WindowLocked;
        _targetProc = _data.TargetProcessName;
        _targetTitle = _data.TargetWindowTitle;
        foreach (var w in _widgets.Values) w.AllowDragging = !_posLocked;
        _panel.WrapContents = false;
        _panel.FlowDirection = _vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight;
        if (_winLocked) { FormBorderStyle = FormBorderStyle.None; ControlBox = false; Text = ""; }
        else { FormBorderStyle = FormBorderStyle.FixedSingle; ControlBox = true; UpdateTitle(); }
        UpdateScale();
        RecalculateSize();
    }
}
