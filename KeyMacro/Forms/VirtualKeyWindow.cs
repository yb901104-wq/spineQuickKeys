using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public class VirtualKeyWindow : Form
{
    private readonly VirtualButtonManager _btnManager;
    private readonly VirtualKeyBindingManager _bindingManager;
    private readonly VirtualLoopExecutor _loopExecutor;
    private readonly VirtualLayoutSerializer _serializer;
    private readonly List<MacroSequence> _sequences;
    private readonly Action? _sequencesChangedCallback;
    private readonly FlowLayoutPanel _panel;
    private readonly Dictionary<string, VirtualButtonWidget> _widgets = [];
    private bool _isDraggingWindow;
    private Point _dragStart;
    private bool _topMostState = true;
    private double _opacityValue = 1.0;
    private bool _positionLocked;
    private bool _windowLocked;

    private const int BasePanelWidth = 400;

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
        FormBorderStyle = FormBorderStyle.Sizable;
        Opacity = _opacityValue;
        Size = new Size(400, 300);
        MinimumSize = new Size(160, 100);

        // Dual border
        Padding = new Padding(1);
        Paint += (_, e) =>
        {
            using var outerPen = new Pen(Color.FromArgb(0x00, 0x00, 0x00));
            e.Graphics.DrawRectangle(outerPen, 0, 0, Width - 1, Height - 1);
            using var rimPen = new Pen(Color.FromArgb(0x3C, 0x3C, 0x3C));
            e.Graphics.DrawLine(rimPen, 1, 1, Width - 2, 1);
        };

        _panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(0x0D, 0x0D, 0x0D)
        };
        Controls.Add(_panel);
        Resize += (_, _) => UpdateScale();

        // Blank area context menu
        var blankMenu = BuildBlankMenu();
        _panel.ContextMenuStrip = blankMenu;

        LoadLayout();
        _btnManager.ButtonsChanged += RebuildWidgets;
        FormClosing += (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                _loopExecutor.StopAll();
                SaveLayout();
                e.Cancel = true;
                Hide();
            }
        };
    }

    // ── Blank area context menu ──

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
            {
                _btnManager.Clear();
                RebuildWidgets();
            }
        });
        m.Items.Add("-");

        // Topmost toggle
        m.Items.Add("置顶/取消置顶", null, (_, _) => ToggleTopMost());

        // Opacity submenu
        var opacityMenu = new ToolStripMenuItem("透明度");
        opacityMenu.DropDownItems.Add("100%", null, (_, _) => SetOpacity(1.0));
        opacityMenu.DropDownItems.Add("80%", null, (_, _) => SetOpacity(0.8));
        opacityMenu.DropDownItems.Add("60%", null, (_, _) => SetOpacity(0.6));
        opacityMenu.DropDownItems.Add("40%", null, (_, _) => SetOpacity(0.4));
        m.Items.Add(opacityMenu);

        // Position lock toggle
        m.Items.Add(_positionLocked ? "✓ 按钮位置已锁定" : "按钮位置锁定/解锁", null, (_, _) => TogglePositionLock());

        m.Items.Add("-");

        m.Items.Add("保存布局", null, (_, _) => SaveLayout());
        m.Items.Add("重置布局", null, (_, _) => ResetLayout());

        // Window lock toggle
        m.Items.Add(_windowLocked ? "✓ 窗口已锁定" : "窗口锁定/解锁", null, (_, _) => ToggleWindowLock());

        return m;
    }

    private void ToggleTopMost()
    {
        _topMostState = !_topMostState;
        TopMost = _topMostState;
        // Rebuild menu to reflect new state next time
    }

    private void SetOpacity(double val)
    {
        _opacityValue = val;
        Opacity = val;
    }

    private void TogglePositionLock()
    {
        _positionLocked = !_positionLocked;
        foreach (var w in _widgets.Values)
            w.AllowDragging = !_positionLocked;
    }


    private void ToggleWindowLock()
    {
        _windowLocked = !_windowLocked;
    }

    public bool HasBoundButtons()
    {
        return _btnManager.Buttons.Any(b => !string.IsNullOrEmpty(b.BindActionId));
    }

    // ── Button management ──

    private void AddButton(VirtualButtonStyle style)
    {
        _btnManager.AddButton(style);
        RebuildWidgets();
    }

    private void RebuildWidgets()
    {
        _panel.SuspendLayout();
        _panel.Controls.Clear();
        _widgets.Clear();

        var buttons = _btnManager.Buttons;
        for (int i = 0; i < buttons.Count; i++)
        {
            var widget = new VirtualButtonWidget(buttons[i]);
            widget.IsFirstInRow = i == 0;
            widget.IsLastInRow = i == buttons.Count - 1;
            widget.Clicked += OnButtonClicked;
            widget.Dragged += OnButtonDragged;
            widget.ContextMenuRequested += OnWidgetContextMenu;
            widget.LoopCountEdited += OnLoopCountEdited;
            _panel.Controls.Add(widget);
            _widgets[buttons[i].Id] = widget;
        }
        UpdateScale();
        _panel.ResumeLayout();
    }

    private void UpdateScale()
    {
        float scale = (float)Math.Max(0.5, Math.Min(2.0, (double)ClientSize.Width / BasePanelWidth));
        foreach (var w in _widgets.Values)
        {
            w.ScaleFactor = scale;
            w.UpdateSize();
        }
    }

    // ── Button events ──

    private void OnButtonClicked(VirtualButtonWidget widget)
    {
        var vbtn = widget.VirtualButton;

        // VK pick mode — send bound hotkey to SequenceEditor
        if (SequenceEditor.IsVkPickMode)
        {
            var seq = _bindingManager.ResolveBinding(vbtn, _sequences);
            if (seq != null && !string.IsNullOrEmpty(seq.TriggerHotkey))
                SequenceEditor.ReceiveVkHotkey(seq.TriggerHotkey);
            return;
        }

        if (vbtn.StyleType == VirtualButtonStyle.LoopIcon && vbtn.LoopEnabled)
        {
            var seq = _bindingManager.ResolveBinding(vbtn, _sequences);
            if (seq != null)
            {
                _loopExecutor.StartLoop(vbtn, seq);
                widget.IsActive = true;
            }
            return;
        }

        var sequence = _bindingManager.ResolveBinding(vbtn, _sequences);
        if (sequence != null)
        {
            var player = new MacroPlayer();
            _ = player.Play(sequence);
        }
    }

    private void OnButtonDragged(VirtualButtonWidget widget, int dx, int dy)
    {
        var vbtn = widget.VirtualButton;
        _btnManager.UpdatePosition(vbtn.Id,
            widget.Location.X + dx,
            widget.Location.Y + dy);
    }

    private void OnLoopCountEdited(VirtualButtonWidget widget, int count)
    {
        var vbtn = widget.VirtualButton;
        SyncSequence(vbtn);
        SaveLayout();
    }

    // ── Context menu on button ──

    private void OnWidgetContextMenu(VirtualButtonWidget widget, Point location)
    {
        var vbtn = widget.VirtualButton;
        var menu = new ContextMenuStrip();

        // 1. Modify button name
        menu.Items.Add("修改按钮名称", null, (_, _) =>
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "输入新的按钮名称:", "修改按钮名称", vbtn.Name);
            if (!string.IsNullOrWhiteSpace(input) && input.Trim() != vbtn.Name)
            {
                vbtn.Name = input.Trim();
                widget.UpdateButton(vbtn);
                SaveLayout();
            }
        });

        // 2. Bind shortcut (submenu: set/clear)
        var bindItem = new ToolStripMenuItem("绑定快捷键");
        bindItem.DropDownItems.Add("设置绑定", null, (_, _) => ShowBindingDialog(widget));
        if (!string.IsNullOrEmpty(vbtn.BindActionId))
        {
            bindItem.DropDownItems.Add("清除绑定", null, (_, _) =>
            {
                _bindingManager.Unbind(vbtn);
                widget.UpdateButton(vbtn);
                SaveLayout();
            });
        }
        menu.Items.Add(bindItem);

        // 3. Loop delay (loop-only)
        if (vbtn.StyleType == VirtualButtonStyle.LoopIcon)
        {
            var intervalMenu = new ToolStripMenuItem("按钮循环延迟");
            intervalMenu.DropDownItems.Add("100ms", null, (_, _) => UpdateLoopInterval(vbtn, widget, 100));
            intervalMenu.DropDownItems.Add("300ms", null, (_, _) => UpdateLoopInterval(vbtn, widget, 300));
            intervalMenu.DropDownItems.Add("500ms", null, (_, _) => UpdateLoopInterval(vbtn, widget, 500));
            intervalMenu.DropDownItems.Add("-");
            intervalMenu.DropDownItems.Add("自定义...", null, (_, _) =>
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox(
                    "循环延迟 (ms):", "设置循环延迟", vbtn.LoopInterval.ToString());
                if (int.TryParse(input, out var ms) && ms > 0)
                {
                    vbtn.LoopInterval = ms;
                    SyncSequence(vbtn);
                    widget.UpdateButton(vbtn);
                    SaveLayout();
                }
            });
            menu.Items.Add(intervalMenu);
        }

        menu.Items.Add("-");

        // 4. Delete current button
        menu.Items.Add("删除当前按钮", null, (_, _) =>
        {
            _loopExecutor.StopLoop(vbtn.Id);
            _btnManager.RemoveButton(vbtn.Id);
            SaveLayout();
        });

        menu.Show(widget, location);
    }

    private void SetStyle(VirtualButton vbtn, VirtualButtonWidget widget, VirtualButtonStyle style)
    {
        vbtn.StyleType = style;
        widget.UpdateButton(vbtn);
        widget.UpdateSize();
        SaveLayout();
    }

    private void UpdateLoopInterval(VirtualButton vbtn, VirtualButtonWidget widget, int ms)
    {
        vbtn.LoopInterval = ms;
        SyncSequence(vbtn);
        widget.UpdateButton(vbtn);
        SaveLayout();
    }

    /// <summary>Sync loop params to bound MacroSequence, then notify MainForm.</summary>
    private void SyncSequence(VirtualButton vbtn)
    {
        var seq = _sequences.Find(s => s.Id == vbtn.BindActionId);
        if (seq != null)
        {
            seq.LoopIntervalMs = vbtn.LoopInterval;
            seq.LoopCount = vbtn.LoopCount;
            _sequencesChangedCallback?.Invoke();
        }
    }

    // ── Binding ──

    private void ShowBindingDialog(VirtualButtonWidget widget)
    {
        var vbtn = widget.VirtualButton;
        var available = _sequences.Where(s => !string.IsNullOrEmpty(s.Name)).ToList();
        if (available.Count == 0)
        {
            MessageBox.Show("没有可绑定的快捷动作，请先在主窗口创建序列。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var menu = new ContextMenuStrip();
        foreach (var seq in available)
        {
            var item = menu.Items.Add(seq.Name);
            item.Click += (_, _) =>
            {
                if (!_bindingManager.TryBind(vbtn, seq.Id))
                {
                    MessageBox.Show("该动作已被其他虚拟按钮绑定。", "冲突",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                widget.UpdateButton(vbtn);
                SaveLayout();
            };
        }
        menu.Show(widget, new Point(0, 0));
    }

    private string ResolveSequenceName(string id)
    {
        var seq = _sequences.Find(s => s.Id == id);
        return seq?.Name ?? "(已删除)";
    }

    // ── Layout save/load ──

    private void SaveLayout()
    {
        var data = new VirtualLayoutSerializer.LayoutData
        {
            WindowX = Left, WindowY = Top,
            WindowWidth = Width, WindowHeight = Height,
            TopMost = _topMostState,
            PositionLocked = _positionLocked,
            WindowLocked = _windowLocked,
            Buttons = [.. _btnManager.Buttons]
        };
        _serializer.Save(data);
    }

    private void LoadLayout()
    {
        var data = _serializer.Load();
        if (data.Buttons.Count > 0)
        {
            _btnManager.LoadFrom(data.Buttons);
            Location = new Point(data.WindowX, data.WindowY);
            Size = new Size(data.WindowWidth, data.WindowHeight);
            _topMostState = data.TopMost;
            TopMost = _topMostState;
            _positionLocked = data.PositionLocked;
            _windowLocked = data.WindowLocked;
            foreach (var w in _widgets.Values)
                w.AllowDragging = !_positionLocked;
        }
        else
        {
            var screen = Screen.PrimaryScreen;
            if (screen != null)
                Location = new Point(
                    (screen.WorkingArea.Width - Width) / 2,
                    (screen.WorkingArea.Height - Height) / 2);
        }
        UpdateScale();
    }

    private void ResetLayout() { _btnManager.Clear(); SaveLayout(); }

    // ── Window dragging ──

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && e.Y < 24 && !_windowLocked)
        {
            _isDraggingWindow = true;
            _dragStart = e.Location;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isDraggingWindow)
        {
            Left += e.X - _dragStart.X;
            Top += e.Y - _dragStart.Y;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isDraggingWindow = false;
    }
}
