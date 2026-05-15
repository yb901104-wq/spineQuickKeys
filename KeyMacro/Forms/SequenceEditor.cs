using KeyMacro.Models;
using KeyMacro.Services;

namespace KeyMacro.Forms;

public partial class SequenceEditor : Form
{
    private readonly MacroSequence _sequence;
    private bool _suppressEvents;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal static bool IsVkPickMode { get; set; }
    private TextBox _txtName = null!;
    private TextBox _txtHotkey = null!;
    private TextBox _txtVkBind = null!;
    private DataGridView _dgvSteps = null!;
    private Button _btnKeyboardRecord = null!;
    private Button _btnVkPick = null!;
    private Button _btnAddStep = null!, _btnDelStep = null!;
    private Button _btnRecordKey = null!;
    private Button _btnMoveUp = null!, _btnMoveDown = null!;
    private Button _btnOk = null!, _btnCancel = null!;
    private Panel _statusPanel = null!;
    private Label _lblStatus = null!;
    private Button _btnCancelPick = null!;
    private ToolStripDropDown _suggestionDropDown = null!;
    private ToolStripControlHost _suggestionHost = null!;
    private ListBox _suggestionListBox = null!;

    public MacroSequence Sequence => _sequence;

    public SequenceEditor() : this(new MacroSequence()) { }

    public SequenceEditor(MacroSequence sequence)
    {
        _sequence = sequence;
        _suppressEvents = true;
        InitializeComponent();
        LoadSequence();
        _suppressEvents = false;
    }

    private void InitializeComponent()
    {
        Text = "编辑序列";
        Size = new Size(1100, 850);
        MinimumSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;

        // ── Top: Name + Hotkey + VK Bind ──
        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 130,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12, 12, 12, 0)
        };
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        topPanel.Controls.Add(new Label { Text = "序列名称:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        topPanel.Controls.Add(new Label { Text = "触发快捷键:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        topPanel.Controls.Add(new Label { Text = "关联虚拟按键:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);

        _txtName = new TextBox { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei", 10) };
        topPanel.Controls.Add(_txtName, 1, 0);

        var hotkeyPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        hotkeyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hotkeyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        hotkeyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        _txtHotkey = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Microsoft YaHei", 10),
            BackColor = Color.White
        };
        _btnKeyboardRecord = new Button { Text = "键盘录入", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        _btnKeyboardRecord.Click += BtnKeyboardRecord_Click;
        _btnVkPick = new Button { Text = "虚拟按键", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
        _btnVkPick.Click += BtnVkPick_Click;
        hotkeyPanel.Controls.Add(_txtHotkey);
        hotkeyPanel.Controls.Add(_btnKeyboardRecord);
        hotkeyPanel.Controls.Add(_btnVkPick);
        topPanel.Controls.Add(hotkeyPanel, 1, 1);

        _txtVkBind = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 10),
            PlaceholderText = "输入虚拟按键名称（如: 按钮1）"
        };
        topPanel.Controls.Add(_txtVkBind, 1, 2);

        // ── Steps Toolbar ──
        var stepsToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 38,
            Padding = new Padding(12, 4, 12, 0)
        };

        _btnAddStep = MakeStepButton("添加步骤");
        _btnDelStep = MakeStepButton("删除步骤");
        _btnRecordKey = MakeStepButton("录制按键");
        _btnMoveUp = MakeStepButton("上移");
        _btnMoveDown = MakeStepButton("下移");

        _btnAddStep.Click += BtnAddStep_Click;
        _btnDelStep.Click += BtnDelStep_Click;
        _btnRecordKey.Click += BtnRecordKey_Click;
        _btnMoveUp.Click += (_, _) => MoveStep(-1);
        _btnMoveDown.Click += (_, _) => MoveStep(1);

        stepsToolbar.Controls.AddRange([_btnAddStep, _btnDelStep, _btnRecordKey, _btnMoveUp, _btnMoveDown]);

        // ── Steps Grid ──
        _dgvSteps = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Padding = new Padding(12, 0, 12, 0),
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _dgvSteps.CellValueChanged += DgvSteps_CellValueChanged;
        _dgvSteps.CellBeginEdit += DgvSteps_CellBeginEdit;
        _dgvSteps.CellEndEdit += (_, _) => { OperationLogger.Info("Suggest: CellEndEdit"); BeginInvoke(HideSuggestion); };
        _dgvSteps.EditingControlShowing += DgvSteps_EditingControlShowing;

        // ── Autocomplete suggestion dropdown (ToolStripDropDown, no focus steal) ──
        _suggestionListBox = new ListBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Microsoft YaHei", 9),
            IntegralHeight = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 20,
            Width = 300,
            Height = 200
        };
        _suggestionListBox.DrawItem += SuggestionList_DrawItem;
        _suggestionListBox.MouseDoubleClick += (_, _) => { OperationLogger.Info("Suggest: MouseDoubleClick"); ApplySuggestion(); };
        _suggestionListBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { OperationLogger.Info("Suggest: ListBox Enter"); ApplySuggestion(); e.SuppressKeyPress = true; }
            if (e.KeyCode == Keys.Escape) { HideSuggestion(); e.SuppressKeyPress = true; }
        };
        _suggestionHost = new ToolStripControlHost(_suggestionListBox)
        {
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false,
            Width = 300,
            Height = 200
        };
        _suggestionDropDown = new ToolStripDropDown
        {
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            AutoClose = false
        };
        _suggestionDropDown.Items.Add(_suggestionHost);

        // ── VkPickMode Status Bar ──
        _statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(0xFF, 0xCC, 0x00),
            Visible = false,
            Padding = new Padding(8, 0, 8, 0)
        };
        _lblStatus = new Label
        {
            Text = "虚拟按键拾取模式... 请在虚拟按键窗口中点击一个按钮。按 Esc 取消。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 9)
        };
        _btnCancelPick = new Button
        {
            Text = "取消拾取",
            Dock = DockStyle.Right,
            Width = 100,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0xD9, 0x5C, 0x5C),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _btnCancelPick.Click += (_, _) => ExitVkPickMode();
        _statusPanel.Controls.Add(_lblStatus);
        _statusPanel.Controls.Add(_btnCancelPick);

        // ── Bottom Buttons ──
        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnCancel = new Button { Text = "取消", AutoSize = true, MinimumSize = new Size(70, 30), FlatStyle = FlatStyle.Flat };
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnOk = new Button
        {
            Text = "确定",
            AutoSize = true,
            MinimumSize = new Size(70, 30),
            Margin = new Padding(0, 0, 8, 0),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnOk.Click += (_, _) => { SaveToSequence(); DialogResult = DialogResult.OK; Close(); };

        bottomPanel.Controls.Add(_btnCancel);
        bottomPanel.Controls.Add(_btnOk);

        // ── Add all panels in correct Z-order ──
        Controls.Add(bottomPanel);  // Dock=Bottom (lowest Z = allocated first)
        Controls.Add(_statusPanel); // Dock=Bottom (above bottomPanel)
        Controls.Add(_dgvSteps);    // Dock=Fill
        Controls.Add(stepsToolbar); // Dock=Top (higher priority)
        Controls.Add(topPanel);     // Dock=Top (highest Z = allocated last = wins top spot)

        // ── Form-level KeyPreview for Esc handling ──
        KeyPreview = true;
        KeyDown += SequenceEditor_KeyDown;

        // ── FormClosed cleanup ──
        FormClosed += (_, _) => IsVkPickMode = false;
    }

    private static Button MakeStepButton(string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(70, 28),
            FlatStyle = FlatStyle.Flat
        };
    }

    private void LoadSequence()
    {
        _txtName.Text = _sequence.Name;
        _txtHotkey.Text = _sequence.TriggerHotkey;
        _txtVkBind.Text = _sequence.TriggerVkButtonName;
        RefreshSteps();
    }

    private void SaveToSequence()
    {
        _sequence.Name = _txtName.Text.Trim();
        _sequence.TriggerHotkey = _txtHotkey.Text.Trim();
        _sequence.TriggerVkButtonName = _txtVkBind.Text.Trim();
        SaveStepsFromGrid();
    }

    private void RefreshSteps()
    {
        _suppressEvents = true;
        _dgvSteps.Columns.Clear();

        var typeCol = new DataGridViewComboBoxColumn
        {
            HeaderText = "类型",
            Width = 90,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        };
        typeCol.Items.AddRange("单键", "组合键", "文本");
        _dgvSteps.Columns.Add(typeCol);
        _dgvSteps.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "按键/文本",
            DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
        });
        _dgvSteps.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _dgvSteps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "延迟(ms)", Width = 80 });

        var pressCol = new DataGridViewComboBoxColumn
        {
            HeaderText = "触发方式",
            Width = 90,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        };
        pressCol.Items.AddRange("点按", "长按");
        _dgvSteps.Columns.Add(pressCol);
        _dgvSteps.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "按压时长(ms)", Width = 100 });

        _dgvSteps.Rows.Clear();
        foreach (var step in _sequence.Steps)
        {
            _dgvSteps.Rows.Add(
                step.Type switch
                {
                    StepType.Key => "单键",
                    StepType.Combo => "组合键",
                    StepType.Text => "文本",
                    _ => "单键"
                },
                step.Keys,
                step.DelayMs,
                step.PressMode == PressMode.Hold ? "长按" : "点按",
                step.HoldDurationMs
            );
        }
        _suppressEvents = false;
    }

    private void SaveStepsFromGrid()
    {
        if (_suppressEvents) return;
        _sequence.Steps.Clear();
        foreach (DataGridViewRow row in _dgvSteps.Rows)
        {
            if (row.IsNewRow) continue;
            var typeStr = row.Cells[0].Value?.ToString() ?? "单键";
            var keys = row.Cells[1].Value?.ToString() ?? "";
            var delayStr = row.Cells[2].Value?.ToString() ?? "50";
            int.TryParse(delayStr, out var delay);
            if (delay < 0) delay = 0;
            var pressStr = row.Cells[3].Value?.ToString() ?? "点按";
            var holdStr = row.Cells[4].Value?.ToString() ?? "0";
            int.TryParse(holdStr, out var holdMs);
            if (holdMs < 0) holdMs = 0;
            _sequence.Steps.Add(new MacroStep
            {
                Type = typeStr switch
                {
                    "组合键" => StepType.Combo,
                    "文本" => StepType.Text,
                    _ => StepType.Key
                },
                Keys = keys,
                DelayMs = delay,
                PressMode = pressStr == "长按" ? PressMode.Hold : PressMode.Tap,
                HoldDurationMs = holdMs
            });
        }
    }

    private void CommitGridEdit()
    {
        _dgvSteps.EndEdit();
        _dgvSteps.CommitEdit(DataGridViewDataErrorContexts.Commit);
    }

    // ── Autocomplete suggestion ──

    private void DgvSteps_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_dgvSteps.CurrentCell?.ColumnIndex != 1)
        {
            HideSuggestion();
            return;
        }
        if (e.Control is TextBox tb)
        {
            tb.TextChanged -= EditingControl_TextChanged;
            tb.TextChanged += EditingControl_TextChanged;
            tb.KeyDown -= SuggestionList_KeyDown;
            tb.KeyDown += SuggestionList_KeyDown;
            OperationLogger.Info("Suggest: hooks attached");
        }
    }

    private void EditingControl_TextChanged(object? sender, EventArgs e)
    {
        if (SpineHotkeyEditor.LastLoadedEntries == null || _dgvSteps.CurrentCell == null)
        { HideSuggestion(); return; }

        if (sender is not TextBox tb || string.IsNullOrEmpty(tb.Text))
        { HideSuggestion(); return; }

        var input = tb.Text;
        OperationLogger.Info($"Suggest: input=\"{input}\" entries={SpineHotkeyEditor.LastLoadedEntries.Count}");
        var field = DetectSearchField(input);
        var results = SpineHotkeyEditor.LastLoadedEntries
            .Where(entry => !entry.Name.StartsWith("---")) // exclude section headers (no Keys)
            .Where(entry => MatchesField(entry, field, input))
            .Take(50)
            .ToList();

        OperationLogger.Info($"Suggest: field={field} results={results.Count}");
        if (results.Count == 0)
        { HideSuggestion(); return; }

        _suggestionListBox.Items.Clear();
        foreach (var r in results)
            _suggestionListBox.Items.Add(r);
        _suggestionListBox.SelectedIndex = 0;
        ShowSuggestionPopup();
    }

    private static string DetectSearchField(string input)
    {
        if (input.Any(c => c >= 0x4E00 && c <= 0x9FFF))
            return "ChineseNote";
        if (input.Contains('+'))
            return "Keys";
        return "Name";
    }

    private static bool MatchesField(SpineHotkeyEntry entry, string field, string input)
    {
        var searchVal = input.Replace(" ", "");
        var val = field switch
        {
            "ChineseNote" => entry.ChineseNote ?? "",
            "Keys" => entry.Keys.Replace(" ", ""),
            _ => entry.Name
        };
        return val.StartsWith(searchVal, StringComparison.OrdinalIgnoreCase)
            || val.Contains(searchVal, StringComparison.OrdinalIgnoreCase);
    }

    private void ShowSuggestionPopup()
    {
        if (_dgvSteps.CurrentCell == null || _suggestionListBox.Items.Count == 0) return;

        var cellRect = _dgvSteps.GetCellDisplayRectangle(
            _dgvSteps.CurrentCell.ColumnIndex, _dgvSteps.CurrentCell.RowIndex, true);
        var cellScreen = _dgvSteps.RectangleToScreen(cellRect);

        int listH = Math.Min(_suggestionListBox.Items.Count * _suggestionListBox.ItemHeight + 4, 200);
        int listW = Math.Max(cellRect.Width, 300);

        _suggestionHost.Width = listW;
        _suggestionHost.Height = listH;
        _suggestionListBox.Height = listH;
        _suggestionListBox.Width = listW;
        _suggestionDropDown.Width = listW;

        var screen = Screen.FromPoint(new Point(cellScreen.Left, cellScreen.Top));
        int posX = cellScreen.Left;
        int posY = cellScreen.Bottom;
        if (posY + listH > screen.WorkingArea.Bottom)
            posY = cellScreen.Top - listH;

        _suggestionDropDown.Show(new Point(posX, posY));
        OperationLogger.Info("Suggest: dropdown shown");
    }

    private void HideSuggestion()
    {
        if (_suggestionDropDown.Visible)
        {
            _suggestionDropDown.Close();
            OperationLogger.Info("Suggest: dropdown closed");
        }
    }

    private void SuggestionList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_suggestionDropDown.Visible) return;

        switch (e.KeyCode)
        {
            case Keys.Down:
                if (_suggestionListBox.SelectedIndex < _suggestionListBox.Items.Count - 1)
                    _suggestionListBox.SelectedIndex++;
                e.SuppressKeyPress = true;
                break;
            case Keys.Up:
                if (_suggestionListBox.SelectedIndex > 0)
                    _suggestionListBox.SelectedIndex--;
                e.SuppressKeyPress = true;
                break;
            case Keys.Enter:
                OperationLogger.Info("Suggest: KeyDown Enter");
                ApplySuggestion();
                e.SuppressKeyPress = true;
                break;
            case Keys.Escape:
                HideSuggestion();
                e.SuppressKeyPress = true;
                break;
        }
    }

    private void SuggestionList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _suggestionListBox.Items.Count) return;
        var entry = (SpineHotkeyEntry)_suggestionListBox.Items[e.Index];
        var name = entry.Name;
        var note = entry.ChineseNote;
        var keys = entry.Keys;
        var text = string.IsNullOrEmpty(note) ? $"{name}" : $"{name}    ({note})";
        if (!string.IsNullOrEmpty(keys))
            text += $"\t{keys}";

        e.DrawBackground();
        TextRenderer.DrawText(e.Graphics, text, _suggestionListBox.Font, e.Bounds,
            (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : _suggestionListBox.ForeColor);
        e.DrawFocusRectangle();
    }

    private void ApplySuggestion()
    {
        OperationLogger.Info($"Suggest: ApplySuggestion start");
        if (_suggestionListBox.SelectedItem is not SpineHotkeyEntry entry)
        { OperationLogger.Info("Suggest: no SelectedItem"); HideSuggestion(); return; }
        if (_dgvSteps.CurrentCell == null)
        { OperationLogger.Info("Suggest: CurrentCell null"); HideSuggestion(); return; }
        if (_dgvSteps.CurrentRow == null)
        { OperationLogger.Info("Suggest: CurrentRow null"); HideSuggestion(); return; }

        var keys = entry.Keys;
        OperationLogger.Info($"Suggest: entry=\"{entry.Name}\" keys=\"{keys}\"");
        if (string.IsNullOrEmpty(keys))
        { OperationLogger.Info("Suggest: empty keys"); HideSuggestion(); return; }

        var row = _dgvSteps.CurrentRow;
        var cell = _dgvSteps.CurrentCell;

        // Use the editing control directly if available
        if (_dgvSteps.EditingControl is TextBox tb)
        {
            OperationLogger.Info($"Suggest: setting EditingControl text=\"{keys}\"");
            tb.Text = keys;
            CommitGridEdit();
        }
        else
        {
            OperationLogger.Info($"Suggest: no EditingControl, setting cell.Value");
            cell.Value = keys;
            CommitGridEdit();
        }

        // Sync type
        if (keys.Contains('+'))
            row.Cells[0].Value = "组合键";
        else
        {
            var currentType = row.Cells[0].Value?.ToString();
            if (string.IsNullOrEmpty(currentType) || currentType == "文本")
                row.Cells[0].Value = "单键";
        }

        HideSuggestion();
        // Re-focus and re-enter edit mode on the cell
        _dgvSteps.CurrentCell = cell;
        _dgvSteps.BeginEdit(true);
        OperationLogger.Info($"Suggest: applied \"{keys}\"");
    }

    private void DgvSteps_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_suppressEvents) return;
        CommitGridEdit();
        SaveStepsFromGrid();
    }

    private void DgvSteps_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (e.ColumnIndex != 4) return;
        var pressStr = _dgvSteps.Rows[e.RowIndex].Cells[3].Value?.ToString() ?? "点按";
        if (pressStr != "长按")
            e.Cancel = true;
    }

    private void BtnAddStep_Click(object? sender, EventArgs e)
    {
        _dgvSteps.EndEdit();
        int newIdx = _dgvSteps.Rows.Add("单键", "", 50);
        _dgvSteps.CurrentCell = _dgvSteps.Rows[newIdx].Cells[1];
        _dgvSteps.Rows[newIdx].Selected = true;
        SaveStepsFromGrid();
    }

    private void BtnDelStep_Click(object? sender, EventArgs e)
    {
        if (_dgvSteps.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先在表格中选择要删除的步骤。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _dgvSteps.EndEdit();
        var idx = _dgvSteps.SelectedRows[0].Index;
        if (idx >= 0 && idx < _dgvSteps.Rows.Count)
        {
            _dgvSteps.Rows.RemoveAt(idx);
            SaveStepsFromGrid();
            if (_dgvSteps.Rows.Count > 0)
            {
                var selectIdx = Math.Min(idx, _dgvSteps.Rows.Count - 1);
                _dgvSteps.Rows[selectIdx].Selected = true;
            }
        }
    }

    private void BtnRecordKey_Click(object? sender, EventArgs e)
    {
        if (_dgvSteps.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先在表格中选择要录制按键的步骤。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var recorder = new HotkeyRecorderForm(allowNoModifier: true);
        if (recorder.ShowDialog(this) == DialogResult.OK)
        {
            CommitGridEdit();
            var row = _dgvSteps.SelectedRows[0];
            row.Cells[1].Value = recorder.RecordedHotkey;
            if (recorder.RecordedHotkey.Contains('+'))
                row.Cells[0].Value = "组合键";
            else
            {
                var currentType = row.Cells[0].Value?.ToString();
                if (string.IsNullOrEmpty(currentType) || currentType == "文本")
                    row.Cells[0].Value = "单键";
            }
            CommitGridEdit();
            SaveStepsFromGrid();
        }
    }

    private void MoveStep(int direction)
    {
        if (_dgvSteps.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先在表格中选择要移动的步骤。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        CommitGridEdit();
        SaveStepsFromGrid();
        var idx = _dgvSteps.SelectedRows[0].Index;
        var target = idx + direction;
        if (target < 0 || target >= _sequence.Steps.Count) return;
        (_sequence.Steps[idx], _sequence.Steps[target]) = (_sequence.Steps[target], _sequence.Steps[idx]);
        RefreshSteps();
        _dgvSteps.Rows[target].Selected = true;
    }

    private void BtnKeyboardRecord_Click(object? sender, EventArgs e)
    {
        OperationLogger.Info($"SequenceEditor: keyboard record clicked for \"{_sequence.Name}\"");
        using var recorder = new HotkeyRecorderForm();
        if (recorder.ShowDialog(this) == DialogResult.OK)
        {
            _txtHotkey.Text = recorder.RecordedHotkey;
            OperationLogger.Info($"SequenceEditor: hotkey recorded: {recorder.RecordedHotkey}");
        }
    }

    private void BtnVkPick_Click(object? sender, EventArgs e)
    {
        var vkWindow = Application.OpenForms.OfType<VirtualKeyWindow>().FirstOrDefault();
        bool vkAvailable = vkWindow != null && vkWindow.Visible;

        if (!vkAvailable)
        {
            var result = MessageBox.Show(this,
                "虚拟按键窗口未打开，是否打开虚拟按键窗口？",
                "虚拟按键不可用",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                MainForm.RequestOpenVirtualKeys();
                vkWindow = Application.OpenForms.OfType<VirtualKeyWindow>().FirstOrDefault();
                vkAvailable = vkWindow != null && vkWindow.Visible;
            }
            if (!vkAvailable)
            {
                MessageBox.Show(this, "无法打开虚拟按键窗口，请手动在主窗口点击「开启虚拟按键」。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        EnterVkPickMode();
    }

    private void EnterVkPickMode()
    {
        IsVkPickMode = true;
        _txtHotkey.Text = "";
        _statusPanel.Visible = true;
        OperationLogger.Info("SequenceEditor: entered VkPickMode");
    }

    internal void ExitVkPickMode()
    {
        if (!IsVkPickMode) return;
        IsVkPickMode = false;
        _statusPanel.Visible = false;
        OperationLogger.Info("SequenceEditor: exited VkPickMode");
    }

    private void SequenceEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && IsVkPickMode)
        {
            ExitVkPickMode();
            e.SuppressKeyPress = true;
        }
    }

    internal static void ReceiveVkPick(string buttonName, string? hotkey)
    {
        var editor = Application.OpenForms.OfType<SequenceEditor>().FirstOrDefault();
        if (editor != null && IsVkPickMode)
        {
            editor._txtVkBind.Text = buttonName;
            if (!string.IsNullOrEmpty(hotkey))
                editor._txtHotkey.Text = hotkey;
            editor.ExitVkPickMode();
            OperationLogger.Info($"SequenceEditor: received VK pick: button=\"{buttonName}\", hotkey=\"{hotkey}\"");
            editor.BringToFront();
        }
    }

    }

public class HotkeyRecorderForm : Form
{
    private bool _ctrl, _alt, _shift, _win;
    private Keys _keyCode = Keys.None;
    private readonly Label _lblStatus;
    private readonly bool _allowNoModifier;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string RecordedHotkey { get; private set; } = "";

    public HotkeyRecorderForm(bool allowNoModifier = false)
    {
        _allowNoModifier = allowNoModifier;
        Text = "录制快捷键";
        Size = new Size(400, 200);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        KeyPreview = true;
        TopMost = true;

        var msg = allowNoModifier
            ? "请按下要录制的按键或组合键...\n\n支持的修饰键: Ctrl, Alt, Shift, Win\n仅按单键也可录制（如 A, Enter, F1）"
            : "请按下你要设置的快捷键组合...\n\n必须包含至少一个修饰键 (Ctrl/Alt/Shift/Win)";

        _lblStatus = new Label
        {
            Text = msg,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 11),
            ForeColor = Color.FromArgb(0x33, 0x33, 0x33),
            AutoSize = false
        };
        Controls.Add(_lblStatus);

        KeyDown += HotkeyRecorderForm_KeyDown;
        KeyUp += HotkeyRecorderForm_KeyUp;
    }

    private void HotkeyRecorderForm_KeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        _ctrl = e.Control || e.KeyCode is Keys.LControlKey or Keys.RControlKey;
        _alt = e.Alt || e.KeyCode is Keys.Menu;
        _shift = e.Shift || e.KeyCode is Keys.LShiftKey or Keys.RShiftKey;
        _win = _win || e.KeyCode is Keys.LWin or Keys.RWin;

        if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
            or Keys.LWin or Keys.RWin
            or Keys.LControlKey or Keys.RControlKey
            or Keys.LShiftKey or Keys.RShiftKey)
            return;

        _keyCode = e.KeyCode;
        var formatted = Services.HotkeyService.FormatHotkey(_keyCode, _ctrl, _alt, _shift, _win);
        if (string.IsNullOrEmpty(formatted))
        {
            _lblStatus.Text = "请包含至少一个修饰键 (Ctrl/Alt/Shift/Win)";
            return;
        }
        if (!_allowNoModifier && !formatted.Contains('+'))
        {
            _lblStatus.Text = "请包含至少一个修饰键 (Ctrl/Alt/Shift/Win)";
            return;
        }

        // Preserve lowercase intent: Keys enum is always uppercase, but when
        // no Shift is held and the result is a bare letter, the user pressed lowercase.
        if (!_shift && !_ctrl && !_alt && !_win && formatted.Length == 1 && char.IsLetter(formatted[0]))
            formatted = formatted.ToLowerInvariant();

        RecordedHotkey = formatted;
        _lblStatus.Text = $"已录制: {formatted}\n\n松开按键确认，按 Esc 取消";
    }

    private void HotkeyRecorderForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }
        if (e.KeyCode is Keys.LControlKey or Keys.RControlKey) _ctrl = false;
        if (e.KeyCode is Keys.LShiftKey or Keys.RShiftKey) _shift = false;
        if (e.KeyCode is Keys.Menu) _alt = false;
        if (e.KeyCode is Keys.LWin or Keys.RWin) _win = false;
        if (!string.IsNullOrEmpty(RecordedHotkey))
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
