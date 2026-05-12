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
    private readonly FlowLayoutPanel _panel;
    private readonly Dictionary<string, VirtualButtonWidget> _widgets = [];
    private bool _isDraggingWindow;
    private Point _dragStart;
    private bool _topMostState = true;

    public VirtualKeyWindow(
        VirtualButtonManager btnManager,
        VirtualKeyBindingManager bindingManager,
        VirtualLoopExecutor loopExecutor,
        VirtualLayoutSerializer serializer,
        List<MacroSequence> sequences)
    {
        _btnManager = btnManager;
        _bindingManager = bindingManager;
        _loopExecutor = loopExecutor;
        _serializer = serializer;
        _sequences = sequences;

        Text = "虚拟按键";
        BackColor = Color.FromArgb(0x1C, 0x1C, 0x1C);
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        Size = new Size(400, 300);
        MinimumSize = new Size(200, 150);

        _panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(0x1C, 0x1C, 0x1C)
        };
        Controls.Add(_panel);

        // Blank area context menu
        var blankMenu = new ContextMenuStrip();
        blankMenu.Items.Add("增加按钮", null, (_, _) => AddButton(VirtualButtonStyle.SmallIcon));
        blankMenu.Items.Add("增加大图标", null, (_, _) => AddButton(VirtualButtonStyle.LargeIcon));
        blankMenu.Items.Add("增加循环按钮", null, (_, _) => AddButton(VirtualButtonStyle.LoopIcon));
        blankMenu.Items.Add("-");
        blankMenu.Items.Add("删除最后按钮", null, (_, _) => { _btnManager.RemoveLast(); RebuildWidgets(); });
        blankMenu.Items.Add("-");
        var toggleTop = new ToolStripMenuItem(_topMostState ? "取消置顶" : "锁定置顶");
        toggleTop.Click += (_, _) => ToggleTopMost();
        blankMenu.Items.Add(toggleTop);
        blankMenu.Items.Add("保存布局", null, (_, _) => SaveLayout());
        blankMenu.Items.Add("重置布局", null, (_, _) => ResetLayout());
        _panel.ContextMenuStrip = blankMenu;

        // Restore saved layout
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

    private void ToggleTopMost()
    {
        _topMostState = !_topMostState;
        TopMost = _topMostState;
    }

    private void AddButton(VirtualButtonStyle style)
    {
        var vbtn = _btnManager.AddButton(style);
        // Position the new button to the right of existing ones
        vbtn.PositionX = 12 + (_widgets.Count % 5) * 70;
        vbtn.PositionY = 12 + (_widgets.Count / 5) * 70;
        RebuildWidgets();
    }

    private void RebuildWidgets()
    {
        _panel.SuspendLayout();
        _panel.Controls.Clear();
        _widgets.Clear();

        foreach (var vbtn in _btnManager.Buttons)
        {
            var widget = new VirtualButtonWidget(vbtn);
            widget.Clicked += OnButtonClicked;
            widget.Dragged += OnButtonDragged;
            widget.ContextMenuRequested += OnWidgetContextMenu;
            _panel.Controls.Add(widget);
            _widgets[vbtn.Id] = widget;
        }
        _panel.ResumeLayout();
    }

    private void OnButtonClicked(VirtualButtonWidget widget)
    {
        var vbtn = widget.VirtualButton;

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
        // Move within the panel (flow layout will rearrange)
        // For now, just log position updates in the model
        _btnManager.UpdatePosition(vbtn.Id,
            widget.Location.X + dx,
            widget.Location.Y + dy);
    }

    private void OnWidgetContextMenu(VirtualButtonWidget widget, Point location)
    {
        var vbtn = widget.VirtualButton;
        var menu = new ContextMenuStrip();

        // Style submenu
        var styleMenu = new ToolStripMenuItem("修改按钮样式");
        styleMenu.DropDownItems.Add("小图标", null, (_, _) =>
        {
            vbtn.StyleType = VirtualButtonStyle.SmallIcon;
            widget.UpdateButton(vbtn);
            SaveLayout();
        });
        styleMenu.DropDownItems.Add("大图标", null, (_, _) =>
        {
            vbtn.StyleType = VirtualButtonStyle.LargeIcon;
            widget.UpdateButton(vbtn);
            SaveLayout();
        });
        styleMenu.DropDownItems.Add("循环按钮", null, (_, _) =>
        {
            vbtn.StyleType = VirtualButtonStyle.LoopIcon;
            widget.UpdateButton(vbtn);
            SaveLayout();
        });
        menu.Items.Add(styleMenu);

        // Binding info
        var bindInfo = string.IsNullOrEmpty(vbtn.BindActionId)
            ? "无快捷绑定"
            : $"当前绑定: {ResolveSequenceName(vbtn.BindActionId)}";
        menu.Items.Add(bindInfo);

        // Set binding
        menu.Items.Add("设置快捷绑定", null, (_, _) => ShowBindingDialog(widget));

        // Clear binding
        menu.Items.Add("清除快捷绑定", null, (_, _) =>
        {
            _bindingManager.Unbind(vbtn);
            SaveLayout();
        });

        // Loop settings for loop buttons
        if (vbtn.StyleType == VirtualButtonStyle.LoopIcon)
        {
            menu.Items.Add("-");
            menu.Items.Add($"循环: {(vbtn.LoopEnabled ? "开启" : "关闭")}", null, (_, _) =>
            {
                vbtn.LoopEnabled = !vbtn.LoopEnabled;
                widget.UpdateButton(vbtn);
                SaveLayout();
            });

            var intervalMenu = new ToolStripMenuItem("循环间隔");
            intervalMenu.DropDownItems.Add("100ms", null, (_, _) => { vbtn.LoopInterval = 100; widget.UpdateButton(vbtn); SaveLayout(); });
            intervalMenu.DropDownItems.Add("300ms", null, (_, _) => { vbtn.LoopInterval = 300; widget.UpdateButton(vbtn); SaveLayout(); });
            intervalMenu.DropDownItems.Add("500ms", null, (_, _) => { vbtn.LoopInterval = 500; widget.UpdateButton(vbtn); SaveLayout(); });
            menu.Items.Add(intervalMenu);

            menu.Items.Add("设置循环次数...", null, (_, _) =>
            {
                var input = Microsoft.VisualBasic.Interaction.InputBox("循环次数 (1-9999):", "循环次数", vbtn.LoopCount.ToString());
                if (int.TryParse(input, out var count) && count > 0 && count <= 9999)
                {
                    vbtn.LoopCount = count;
                    widget.UpdateButton(vbtn);
                    SaveLayout();
                }
            });
        }

        menu.Items.Add("-");
        menu.Items.Add("删除按钮", null, (_, _) =>
        {
            _loopExecutor.StopLoop(vbtn.Id);
            _btnManager.RemoveButton(vbtn.Id);
            SaveLayout();
        });

        menu.Show(widget, location);
    }

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

    private void SaveLayout()
    {
        var data = new VirtualLayoutSerializer.LayoutData
        {
            WindowX = Left,
            WindowY = Top,
            WindowWidth = Width,
            WindowHeight = Height,
            TopMost = _topMostState,
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
        }
        else
        {
            // Default position: center of screen
            var screen = Screen.PrimaryScreen;
            if (screen != null)
                Location = new Point(
                    (screen.WorkingArea.Width - Width) / 2,
                    (screen.WorkingArea.Height - Height) / 2);
        }
    }

    private void ResetLayout()
    {
        _btnManager.Clear();
        SaveLayout();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && e.Y < 30) // title bar area
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
