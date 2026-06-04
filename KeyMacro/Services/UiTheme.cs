using KeyMacro.Controls;
using KeyMacro.Forms;
using System.Reflection;

namespace KeyMacro.Services;

public enum UiWindowProfile
{
    Main,
    SequenceEditor,
    HotkeyRecorder,
    SpineHotkeyEditor,
    VkWindowManager,
    BatchCopy,
    SourceFilePicker,
    ConflictDialog,
    BatchCli,
    AnimationSelect,
    ReNameTool,
    InputDialog,
    SubfolderSelect,
    GenericDialog
}

public static class UiTheme
{
    public static readonly Color App = FromHex("#2B2B2B");
    public static readonly Color Workspace = FromHex("#3A3A3C");
    public static readonly Color Panel = FromHex("#454547");
    public static readonly Color PanelAlt = FromHex("#505052");
    public static readonly Color Input = FromHex("#262628");
    public static readonly Color ControlWell = FromHex("#1E1E20");
    public static readonly Color List = FromHex("#202023");
    public static readonly Color Border = FromHex("#5E5E60");
    public static readonly Color BorderStrong = FromHex("#727274");
    public static readonly Color Text = FromHex("#E6E6E6");
    public static readonly Color Muted = FromHex("#B8B8B8");
    public static readonly Color Disabled = FromHex("#777777");
    public static readonly Color Blue = FromHex("#2388C9");
    public static readonly Color Cyan = FromHex("#39C5D8");
    public static readonly Color Orange = FromHex("#F08A3C");
    public static readonly Color Green = FromHex("#6BBF59");
    public static readonly Color Red = FromHex("#D95C5C");

    private static readonly Dictionary<string, Image> ImageCache = [];
    private static readonly HashSet<Button> ThemedButtons = [];
    private static readonly HashSet<DataGridView> ThemedGrids = [];
    private static readonly HashSet<ListView> ThemedListViews = [];
    private static readonly HashSet<TabControl> ThemedTabs = [];
    private static readonly HashSet<CheckedListBox> ThemedCheckedLists = [];
    private static readonly HashSet<ListBox> ThemedListBoxes = [];
    private static readonly HashSet<CheckBox> ThemedCheckBoxes = [];
    private static readonly HashSet<RadioButton> ThemedRadioButtons = [];
    private static readonly HashSet<ComboBox> ThemedCombos = [];

    public static void Apply(Form form, UiWindowProfile profile)
    {
        if (form is VirtualKeyWindow)
            return;

        form.BackColor = App;
        NativeWindowTheme.ApplyDarkTitleBar(form);
        ApplyRecursive(form);

        foreach (var menu in FindContextMenus(form))
            Apply(menu);
    }

    public static void Apply(ContextMenuStrip menu)
    {
        menu.Renderer = new DarkMenuRenderer();
        menu.BackColor = FromHex("#29292B");
        menu.ForeColor = Text;
        menu.Font = BodyFont(9f);
        foreach (ToolStripItem item in menu.Items)
        {
            item.BackColor = FromHex("#29292B");
            item.ForeColor = IsDangerText(item.Text) ? Red : Text;
            item.Font = item.Enabled ? BodyFont(9f) : BodyFont(9f);
        }
    }

    public static void ApplyDefaultSize(Form form, UiWindowProfile profile)
    {
        var (size, min) = profile switch
        {
            UiWindowProfile.Main => (new Size(1440, 900), new Size(1100, 650)),
            UiWindowProfile.SequenceEditor => (new Size(1440, 900), new Size(1100, 650)),
            UiWindowProfile.HotkeyRecorder => (new Size(580, 360), new Size(420, 240)),
            UiWindowProfile.SpineHotkeyEditor => (new Size(1440, 900), new Size(1100, 650)),
            UiWindowProfile.VkWindowManager => (new Size(1440, 900), new Size(900, 560)),
            UiWindowProfile.BatchCopy => (new Size(1440, 900), new Size(1040, 680)),
            UiWindowProfile.SourceFilePicker => (new Size(1440, 900), new Size(980, 620)),
            UiWindowProfile.ConflictDialog => (new Size(620, 430), new Size(520, 360)),
            UiWindowProfile.BatchCli => (new Size(1440, 900), new Size(1040, 680)),
            UiWindowProfile.AnimationSelect => (new Size(520, 560), new Size(420, 420)),
            UiWindowProfile.ReNameTool => (new Size(1440, 900), new Size(1080, 680)),
            UiWindowProfile.InputDialog => (new Size(520, 220), new Size(420, 180)),
            UiWindowProfile.SubfolderSelect => (new Size(820, 620), new Size(640, 460)),
            _ => (new Size(620, 430), new Size(420, 260))
        };

        form.Size = size;
        if (form.MinimumSize.Width < min.Width || form.MinimumSize.Height < min.Height)
            form.MinimumSize = min;
    }

    private static void ApplyRecursive(Control root)
    {
        foreach (Control control in root.Controls)
        {
            ApplyControl(control);
            if (control.HasChildren)
                ApplyRecursive(control);
        }
    }

    private static void ApplyControl(Control control)
    {
        NativeWindowTheme.ApplyDarkControlChrome(control);
        control.Font = control is TextBoxBase or ComboBox ? BodyFont(10f) : BodyFont();
        control.ForeColor = Text;

        switch (control)
        {
            case Button button:
                Apply(button);
                break;
            case DataGridView grid:
                Apply(grid);
                break;
            case TextBox textBox:
                textBox.BackColor = textBox.ReadOnly ? FromHex("#222224") : Input;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox combo:
                combo.BackColor = Input;
                combo.ForeColor = Text;
                combo.FlatStyle = FlatStyle.Flat;
                Apply(combo);
                break;
            case CheckedListBox checkedList:
                checkedList.BackColor = List;
                checkedList.ForeColor = Text;
                checkedList.BorderStyle = BorderStyle.FixedSingle;
                Apply(checkedList);
                break;
            case ListBox listBox:
                listBox.BackColor = List;
                listBox.ForeColor = Text;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                Apply(listBox);
                break;
            case ListView listView:
                listView.BackColor = List;
                listView.ForeColor = Text;
                listView.BorderStyle = BorderStyle.FixedSingle;
                ApplyDetailsListView(listView);
                break;
            case TabControl tab:
                Apply(tab);
                break;
            case TabPage page:
                page.BackColor = Panel;
                page.ForeColor = Text;
                break;
            case TextProgressBar progress:
                progress.BackColor = ControlWell;
                progress.ForeColor = Text;
                progress.BarColor = Blue;
                progress.BorderColor = BorderStrong;
                progress.Height = Math.Max(progress.Height, 26);
                break;
            case CheckBox checkBox:
                Apply(checkBox);
                break;
            case RadioButton radio:
                Apply(radio);
                break;
            case Label label:
                Apply(label);
                break;
            case TableLayoutPanel or FlowLayoutPanel or System.Windows.Forms.Panel:
                if (control.BackColor != Color.FromArgb(0xFF, 0xCC, 0x00))
                    control.BackColor = control.Parent is Form ? App : Panel;
                break;
            case GroupBox groupBox:
                groupBox.BackColor = Panel;
                groupBox.ForeColor = Text;
                break;
        }
    }

    private static void Apply(Label label)
    {
        if (label.Parent?.BackColor == Color.FromArgb(0xFF, 0xCC, 0x00))
        {
            label.ForeColor = Color.Black;
            return;
        }

        if (IsLight(label.BackColor))
            label.BackColor = TransparentParentColor(label);

        if (label.ForeColor == Color.Gray || label.ForeColor == SystemColors.GrayText)
            label.ForeColor = Muted;
        else
            label.ForeColor = Text;
    }

    private static void Apply(Button button)
    {
        if (ThemedButtons.Contains(button))
            return;

        ThemedButtons.Add(button);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = Color.Transparent;
        button.FlatAppearance.MouseOverBackColor = Color.Transparent;
        button.UseVisualStyleBackColor = false;
        button.ForeColor = Text;
        button.Font = BodyFont(9f);
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(4, 0, 4, 1);
        if (button.AutoSize && button.MinimumSize.Height < 32)
            button.MinimumSize = new Size(button.MinimumSize.Width, 32);

        var kind = ButtonKind(button.Text);
        SetButtonImage(button, kind, button.Enabled ? "normal" : "disabled");

        button.MouseEnter += (_, _) => SetButtonImage(button, kind, button.Enabled ? "hover" : "disabled");
        button.MouseLeave += (_, _) => SetButtonImage(button, kind, button.Enabled ? "normal" : "disabled");
        button.MouseDown += (_, _) => SetButtonImage(button, kind, button.Enabled ? "pressed" : "disabled");
        button.MouseUp += (_, _) => SetButtonImage(button, kind, button.Enabled ? "hover" : "disabled");
        button.EnabledChanged += (_, _) =>
        {
            button.ForeColor = button.Enabled ? Text : Disabled;
            SetButtonImage(button, kind, button.Enabled ? "normal" : "disabled");
        };
    }

    private static void Apply(TabControl tab)
    {
        tab.Appearance = TabAppearance.FlatButtons;
        tab.BackColor = App;
        tab.ForeColor = Text;
        tab.DrawMode = TabDrawMode.OwnerDrawFixed;
        tab.ItemSize = new Size(Math.Max(tab.ItemSize.Width, 88), Math.Max(tab.ItemSize.Height, 34));

        if (ThemedTabs.Add(tab))
            tab.DrawItem += DrawTabItem;
    }

    private static void DrawTabItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tab || e.Index < 0 || e.Index >= tab.TabPages.Count)
            return;

        var selected = e.Index == tab.SelectedIndex;
        var rect = tab.GetTabRect(e.Index);
        rect.Inflate(-1, -1);

        using (var background = new SolidBrush(selected ? Panel : FromHex("#333335")))
            e.Graphics.FillRectangle(background, rect);
        using (var border = new Pen(selected ? BorderStrong : Border))
            e.Graphics.DrawRectangle(border, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

        if (selected)
        {
            using var accent = new SolidBrush(Blue);
            e.Graphics.FillRectangle(accent, rect.X + 6, rect.Y, Math.Max(1, rect.Width - 12), 3);
        }

        TextRenderer.DrawText(
            e.Graphics,
            tab.TabPages[e.Index].Text,
            BodyFont(9f, selected ? FontStyle.Bold : FontStyle.Regular),
            rect,
            selected ? Text : Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void Apply(CheckedListBox checkedList)
    {
        checkedList.DrawMode = DrawMode.OwnerDrawFixed;
        checkedList.ItemHeight = Math.Max(checkedList.ItemHeight, 24);
        checkedList.ThreeDCheckBoxes = false;

        if (ThemedCheckedLists.Add(checkedList))
            checkedList.DrawItem += DrawCheckedListItem;
    }

    private static void DrawCheckedListItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not CheckedListBox list || e.Index < 0 || e.Index >= list.Items.Count)
            return;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backColor = selected
            ? FromHex("#5B7782")
            : e.Index % 2 == 0 ? FromHex("#454547") : FromHex("#404042");

        using (var background = new SolidBrush(backColor))
            e.Graphics.FillRectangle(background, e.Bounds);

        var checkedImage = LoadImage(list.GetItemChecked(e.Index) ? "checks/checkbox_checked.png" : "checks/checkbox_unchecked.png");
        var size = Math.Min(18, Math.Max(12, e.Bounds.Height - 6));
        var checkRect = new Rectangle(e.Bounds.Left + 6, e.Bounds.Top + (e.Bounds.Height - size) / 2, size, size);
        e.Graphics.DrawImage(checkedImage, checkRect);

        var textRect = new Rectangle(checkRect.Right + 8, e.Bounds.Top, Math.Max(0, e.Bounds.Width - checkRect.Right - 12), e.Bounds.Height);
        TextRenderer.DrawText(
            e.Graphics,
            list.GetItemText(list.Items[e.Index]),
            BodyFont(9f),
            textRect,
            selected ? Color.White : Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static void Apply(ListBox listBox)
    {
        if (listBox.DrawMode != DrawMode.Normal)
            return;

        listBox.DrawMode = DrawMode.OwnerDrawFixed;
        listBox.ItemHeight = Math.Max(listBox.ItemHeight, 22);

        if (ThemedListBoxes.Add(listBox))
            listBox.DrawItem += DrawListBoxItem;
    }

    private static void DrawListBoxItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ListBox list || e.Index < 0 || e.Index >= list.Items.Count)
            return;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backColor = selected
            ? FromHex("#5B7782")
            : e.Index % 2 == 0 ? FromHex("#454547") : FromHex("#404042");

        using (var background = new SolidBrush(backColor))
            e.Graphics.FillRectangle(background, e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            list.GetItemText(list.Items[e.Index]),
            BodyFont(9f),
            Rectangle.Inflate(e.Bounds, -6, 0),
            selected ? Color.White : Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static void Apply(CheckBox checkBox)
    {
        checkBox.BackColor = TransparentParentColor(checkBox);
        checkBox.ForeColor = Text;
        checkBox.FlatStyle = FlatStyle.Flat;
        checkBox.Appearance = Appearance.Normal;

        if (ThemedCheckBoxes.Add(checkBox))
        {
            checkBox.Paint += DrawCheckBox;
            checkBox.CheckedChanged += (_, _) => checkBox.Invalidate();
            checkBox.EnabledChanged += (_, _) => checkBox.Invalidate();
            checkBox.MouseEnter += (_, _) => checkBox.Invalidate();
            checkBox.MouseLeave += (_, _) => checkBox.Invalidate();
        }
    }

    private static void DrawCheckBox(object? sender, PaintEventArgs e)
    {
        if (sender is not CheckBox checkBox)
            return;

        var backColor = TransparentParentColor(checkBox);
        using (var background = new SolidBrush(backColor))
            e.Graphics.FillRectangle(background, checkBox.ClientRectangle);

        var boxSize = Math.Min(16, Math.Max(12, checkBox.Height - 6));
        var boxRect = new Rectangle(0, (checkBox.Height - boxSize) / 2, boxSize, boxSize);
        var borderColor = checkBox.Enabled ? (checkBox.Checked ? Blue : BorderStrong) : Disabled;

        using (var fill = new SolidBrush(checkBox.Checked ? FromHex("#183D4D") : ControlWell))
            e.Graphics.FillRectangle(fill, boxRect);
        using (var border = new Pen(borderColor))
            e.Graphics.DrawRectangle(border, boxRect.X, boxRect.Y, boxRect.Width - 1, boxRect.Height - 1);

        if (checkBox.Checked)
        {
            using var pen = new Pen(Cyan, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
            e.Graphics.DrawLines(pen, [
                new Point(boxRect.X + 3, boxRect.Y + boxRect.Height / 2),
                new Point(boxRect.X + 7, boxRect.Bottom - 4),
                new Point(boxRect.Right - 3, boxRect.Y + 4)
            ]);
        }

        var textRect = new Rectangle(boxRect.Right + 6, 0, Math.Max(0, checkBox.Width - boxRect.Right - 6), checkBox.Height);
        TextRenderer.DrawText(
            e.Graphics,
            checkBox.Text,
            checkBox.Font,
            textRect,
            checkBox.Enabled ? Text : Disabled,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static void Apply(RadioButton radio)
    {
        radio.BackColor = TransparentParentColor(radio);
        radio.ForeColor = Text;
        radio.FlatStyle = FlatStyle.Flat;
        radio.Appearance = Appearance.Normal;

        if (ThemedRadioButtons.Add(radio))
        {
            radio.Paint += DrawRadioButton;
            radio.CheckedChanged += (_, _) => radio.Invalidate();
            radio.EnabledChanged += (_, _) => radio.Invalidate();
            radio.MouseEnter += (_, _) => radio.Invalidate();
            radio.MouseLeave += (_, _) => radio.Invalidate();
        }
    }

    private static void DrawRadioButton(object? sender, PaintEventArgs e)
    {
        if (sender is not RadioButton radio)
            return;

        var backColor = TransparentParentColor(radio);
        using (var background = new SolidBrush(backColor))
            e.Graphics.FillRectangle(background, radio.ClientRectangle);

        var dotSize = Math.Min(16, Math.Max(12, radio.Height - 6));
        var dotRect = new Rectangle(0, (radio.Height - dotSize) / 2, dotSize, dotSize);
        var borderColor = radio.Enabled ? (radio.Checked ? Blue : BorderStrong) : Disabled;

        using (var fill = new SolidBrush(radio.Checked ? FromHex("#183D4D") : ControlWell))
            e.Graphics.FillEllipse(fill, dotRect);
        using (var border = new Pen(borderColor))
            e.Graphics.DrawEllipse(border, dotRect.X, dotRect.Y, dotRect.Width - 1, dotRect.Height - 1);

        if (radio.Checked)
        {
            var inner = Rectangle.Inflate(dotRect, -4, -4);
            using var fill = new SolidBrush(Cyan);
            e.Graphics.FillEllipse(fill, inner);
        }

        var textRect = new Rectangle(dotRect.Right + 6, 0, Math.Max(0, radio.Width - dotRect.Right - 6), radio.Height);
        TextRenderer.DrawText(
            e.Graphics,
            radio.Text,
            radio.Font,
            textRect,
            radio.Enabled ? Text : Disabled,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static void Apply(ComboBox combo)
    {
        combo.BackColor = Input;
        combo.ForeColor = Text;
        combo.FlatStyle = FlatStyle.Flat;
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.ItemHeight = Math.Max(combo.ItemHeight, 22);

        if (ThemedCombos.Add(combo))
            combo.DrawItem += DrawComboItem;
    }

    private static void DrawComboItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox combo || e.Index < 0)
            return;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backColor = selected ? FromHex("#5B7782") : Input;
        using (var background = new SolidBrush(backColor))
            e.Graphics.FillRectangle(background, e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            combo.GetItemText(combo.Items[e.Index]),
            combo.Font,
            Rectangle.Inflate(e.Bounds, -6, 0),
            selected ? Color.White : Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static void Apply(DataGridView grid)
    {
        grid.BackgroundColor = List;
        grid.GridColor = Border;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = FromHex("#2B2B2D");
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = FromHex("#2B2B2D");
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
        grid.ColumnHeadersDefaultCellStyle.Font = BodyFont(9f, FontStyle.Bold);
        grid.DefaultCellStyle.BackColor = FromHex("#454547");
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = FromHex("#5B7782");
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.AlternatingRowsDefaultCellStyle.BackColor = FromHex("#404042");
        grid.AlternatingRowsDefaultCellStyle.ForeColor = Text;
        grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 34);

        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.DefaultCellStyle.BackColor = grid.DefaultCellStyle.BackColor;
            column.DefaultCellStyle.ForeColor = Text;
            column.DefaultCellStyle.SelectionBackColor = grid.DefaultCellStyle.SelectionBackColor;
            column.DefaultCellStyle.SelectionForeColor = Color.White;

            if (column is DataGridViewButtonColumn buttonColumn)
                buttonColumn.FlatStyle = FlatStyle.Flat;
            if (column is DataGridViewComboBoxColumn comboColumn)
                comboColumn.FlatStyle = FlatStyle.Flat;
        }

        if (ThemedGrids.Add(grid))
        {
            grid.CellPainting += PaintGridInteractiveCell;
            grid.EditingControlShowing += ThemeGridEditingControl;
        }
    }

    private static void PaintGridInteractiveCell(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var cell = grid[e.ColumnIndex, e.RowIndex];
        if (cell is DataGridViewButtonCell)
        {
            PaintGridButtonCell(grid, e);
            return;
        }

        if (cell is DataGridViewComboBoxCell)
        {
            PaintGridComboBoxCell(grid, e);
            return;
        }

        if (cell is DataGridViewCheckBoxCell)
            PaintGridCheckBoxCell(grid, e);
    }

    private static void PaintGridButtonCell(DataGridView grid, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics == null)
            return;

        var selected = (e.State & DataGridViewElementStates.Selected) != 0;
        var background = selected
            ? grid.DefaultCellStyle.SelectionBackColor
            : e.RowIndex % 2 == 0 ? grid.DefaultCellStyle.BackColor : grid.AlternatingRowsDefaultCellStyle.BackColor;

        using var bg = new SolidBrush(background);
        e.Graphics.FillRectangle(bg, e.CellBounds);

        var buttonRect = Rectangle.Inflate(e.CellBounds, -5, -4);
        if (buttonRect.Width > 0 && buttonRect.Height > 0)
        {
            var text = Convert.ToString(e.FormattedValue) ?? "";
            var kind = ButtonKind(text);
            e.Graphics.DrawImage(LoadImage($"buttons/button_{kind}_normal.png"), buttonRect);

            var foreColor = text == "×" || IsDangerText(text) ? Color.White : Text;
            TextRenderer.DrawText(
                e.Graphics,
                text,
                BodyFont(9f),
                buttonRect,
                foreColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        e.Handled = true;
    }

    private static void PaintGridComboBoxCell(DataGridView grid, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics == null)
            return;

        var selected = (e.State & DataGridViewElementStates.Selected) != 0;
        var background = selected
            ? grid.DefaultCellStyle.SelectionBackColor
            : e.RowIndex % 2 == 0 ? grid.DefaultCellStyle.BackColor : grid.AlternatingRowsDefaultCellStyle.BackColor;

        using var bg = new SolidBrush(background);
        e.Graphics.FillRectangle(bg, e.CellBounds);

        var rect = Rectangle.Inflate(e.CellBounds, -2, -2);
        using (var inner = new SolidBrush(selected ? FromHex("#39474D") : Input))
            e.Graphics.FillRectangle(inner, rect);
        using (var border = new Pen(selected ? Cyan : BorderStrong))
            e.Graphics.DrawRectangle(border, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

        var textRect = new Rectangle(rect.X + 6, rect.Y, Math.Max(0, rect.Width - 24), rect.Height);
        TextRenderer.DrawText(
            e.Graphics,
            Convert.ToString(e.FormattedValue) ?? "",
            BodyFont(9f),
            textRect,
            selected ? Color.White : Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        var arrowX = rect.Right - 14;
        var arrowY = rect.Top + rect.Height / 2 - 2;
        using var arrow = new SolidBrush(Muted);
        e.Graphics.FillPolygon(arrow, [new Point(arrowX, arrowY), new Point(arrowX + 8, arrowY), new Point(arrowX + 4, arrowY + 5)]);

        e.Handled = true;
    }

    private static void PaintGridCheckBoxCell(DataGridView grid, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics == null)
            return;

        var selected = (e.State & DataGridViewElementStates.Selected) != 0;
        var background = selected
            ? grid.DefaultCellStyle.SelectionBackColor
            : e.RowIndex % 2 == 0 ? grid.DefaultCellStyle.BackColor : grid.AlternatingRowsDefaultCellStyle.BackColor;

        using var bg = new SolidBrush(background);
        e.Graphics.FillRectangle(bg, e.CellBounds);

        var value = e.Value is bool b && b;
        var image = LoadImage(value ? "checks/checkbox_checked.png" : "checks/checkbox_unchecked.png");
        var size = Math.Min(18, Math.Min(e.CellBounds.Width - 6, e.CellBounds.Height - 6));
        if (size > 0)
        {
            var rect = new Rectangle(
                e.CellBounds.Left + (e.CellBounds.Width - size) / 2,
                e.CellBounds.Top + (e.CellBounds.Height - size) / 2,
                size,
                size);
            e.Graphics.DrawImage(image, rect);
        }

        e.Handled = true;
    }

    private static void ThemeGridEditingControl(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        switch (e.Control)
        {
            case TextBox textBox:
                textBox.BackColor = Input;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox combo:
                combo.BackColor = Input;
                combo.ForeColor = Text;
                combo.FlatStyle = FlatStyle.Flat;
                break;
        }
    }

    private static void ApplyDetailsListView(ListView listView)
    {
        if (listView.View != View.Details || !ThemedListViews.Add(listView))
            return;

        listView.OwnerDraw = true;
        StretchLastListViewColumn(listView);
        listView.DrawColumnHeader += DrawDetailsListColumnHeader;
        listView.DrawSubItem += DrawDetailsListSubItem;
        listView.Resize += (_, _) => StretchLastListViewColumn(listView);
    }

    private static void StretchLastListViewColumn(ListView listView)
    {
        if (listView.Columns.Count == 0 || listView.ClientSize.Width <= 0)
            return;

        var last = listView.Columns[^1];
        var fixedWidth = 0;
        for (var i = 0; i < listView.Columns.Count - 1; i++)
            fixedWidth += listView.Columns[i].Width;

        var desired = listView.ClientSize.Width - fixedWidth - 4;
        if (desired > last.Width)
            last.Width = desired;
    }

    private static void DrawDetailsListColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using var background = new SolidBrush(FromHex("#2B2B2D"));
        e.Graphics.FillRectangle(background, e.Bounds);
        using var border = new Pen(Border);
        e.Graphics.DrawRectangle(border, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
        TextRenderer.DrawText(
            e.Graphics,
            e.Header?.Text ?? "",
            BodyFont(9f, FontStyle.Bold),
            Rectangle.Inflate(e.Bounds, -6, 0),
            Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void DrawDetailsListSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item == null)
            return;

        var selected = e.Item.Selected;
        var backColor = selected
            ? FromHex("#5B7782")
            : NormalizeListItemBackColor(e.Item.BackColor, e.ItemIndex);

        using (var brush = new SolidBrush(backColor))
            e.Graphics.FillRectangle(brush, e.Bounds);
        using (var border = new Pen(Border))
            e.Graphics.DrawRectangle(border, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);

        var foreColor = e.Item.ForeColor == Color.Empty || IsLight(e.Item.ForeColor) ? Text : e.Item.ForeColor;
        if (selected)
            foreColor = Color.White;

        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem?.Text ?? "",
            BodyFont(9f),
            Rectangle.Inflate(e.Bounds, -6, 0),
            foreColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static Color NormalizeListItemBackColor(Color color, int itemIndex)
    {
        if (color == Color.Empty || color == Color.Transparent || color == Color.White)
            return itemIndex % 2 == 0 ? FromHex("#454547") : FromHex("#404042");

        if (IsLight(color))
        {
            if (color.G > color.R && color.G > color.B)
                return FromHex("#32442F");
            if (color.R > color.G && color.R > color.B)
                return FromHex("#493030");
            return PanelAlt;
        }

        return color;
    }

    private static IEnumerable<ContextMenuStrip> FindContextMenus(Form form)
    {
        foreach (var field in form.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.GetValue(form) is ContextMenuStrip menu)
                yield return menu;
        }
    }

    private static void SetButtonImage(Button button, string kind, string state)
    {
        button.BackgroundImage = LoadImage($"buttons/button_{kind}_{state}.png");
        button.BackgroundImageLayout = ImageLayout.Stretch;
        button.BackColor = Color.Transparent;
    }

    private static string ButtonKind(string text)
    {
        if (IsDangerText(text))
            return "danger";
        if (text.Contains("Spine", StringComparison.OrdinalIgnoreCase) || text.Contains("录制") || text.Contains("载入"))
            return "spine";
        if (text.Contains("CLI", StringComparison.OrdinalIgnoreCase))
            return "cli";
        if (text.Contains("虚拟") || text.Contains("VK", StringComparison.OrdinalIgnoreCase) || text.Contains("浏览") || text.Contains("选择") || text.Contains("检测") || text.Contains("刷新"))
            return "tool";
        if (text.Contains("开始") || text.Contains("执行") || text.Contains("导出") || text.Contains("确认") || text.Contains("确定") || text.Contains("保存") || text.Contains("覆盖"))
            return "primary";
        return "neutral";
    }

    private static bool IsDangerText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        return text.Contains("删除全部")
            || text.Contains("取消全部")
            || text.Contains("强制停止")
            || text.Contains("释放");
    }

    public static Image LoadImage(string relativePath)
    {
        relativePath = relativePath.Replace('\\', '/');
        if (ImageCache.TryGetValue(relativePath, out var cached))
            return cached;

        var disk = FindAssetOnDisk(relativePath);
        if (File.Exists(disk))
        {
            using var src = Image.FromFile(disk);
            return ImageCache[relativePath] = new Bitmap(src);
        }

        var asm = Assembly.GetExecutingAssembly();
        var resource = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(relativePath.Replace('/', '.'), StringComparison.OrdinalIgnoreCase));
        if (resource != null)
        {
            using var stream = asm.GetManifestResourceStream(resource);
            if (stream != null)
            {
                using var src = Image.FromStream(stream);
                return ImageCache[relativePath] = new Bitmap(src);
            }
        }

        return ImageCache[relativePath] = new Bitmap(1, 1);
    }

    private static string FindAssetOnDisk(string relativePath)
    {
        var localPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "ui", localPath);
            if (File.Exists(candidate))
                return candidate;

            var projectCandidate = Path.Combine(dir.FullName, "KeyMacro", "assets", "ui", localPath);
            if (File.Exists(projectCandidate))
                return projectCandidate;

            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "assets", "ui", localPath);
    }

    private static Color TransparentParentColor(Control control) => control.Parent?.BackColor == Color.Empty ? Panel : control.Parent?.BackColor ?? Panel;

    private static bool IsLight(Color color)
    {
        if (color == Color.Empty || color == Color.Transparent)
            return false;
        return color.R + color.G + color.B > 560;
    }

    private static Font BodyFont(float size = 9f, FontStyle style = FontStyle.Regular) => new("Microsoft YaHei UI", size, style, GraphicsUnit.Point);

    private static Color FromHex(string hex)
    {
        var h = hex.TrimStart('#');
        return Color.FromArgb(255, Convert.ToInt32(h[..2], 16), Convert.ToInt32(h[2..4], 16), Convert.ToInt32(h[4..6], 16));
    }

    private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(FromHex("#29292B"));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            var bg = e.Item.Selected ? FromHex("#39474D") : FromHex("#29292B");
            if (IsDangerText(e.Item.Text) && e.Item.Selected)
                bg = FromHex("#473434");
            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, rect);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(FromHex("#48484A"));
            e.Graphics.DrawLine(pen, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Muted;
            base.OnRenderArrow(e);
        }
    }
}
