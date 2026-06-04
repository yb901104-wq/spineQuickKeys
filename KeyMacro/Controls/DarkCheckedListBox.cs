using KeyMacro.Services;

namespace KeyMacro.Controls;

public class DarkCheckedListBox : CheckedListBox
{
    private int _hoverIndex = -1;

    public DarkCheckedListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 28;
        ThreeDCheckBoxes = false;
        BackColor = UiTheme.List;
        ForeColor = UiTheme.Text;
        BorderStyle = BorderStyle.FixedSingle;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count)
            return;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var hovered = e.Index == _hoverIndex;
        var backColor = selected
            ? Color.FromArgb(0x5B, 0x77, 0x82)
            : hovered ? Color.FromArgb(0x4C, 0x4C, 0x4F)
            : e.Index % 2 == 0 ? Color.FromArgb(0x45, 0x45, 0x47) : Color.FromArgb(0x40, 0x40, 0x42);

        using (var background = new SolidBrush(backColor))
            e.Graphics.FillRectangle(background, e.Bounds);

        var imagePath = Enabled
            ? GetItemChecked(e.Index) ? "checks/checkbox_checked.png" : "checks/checkbox_unchecked.png"
            : "checks/checkbox_disabled.png";
        var checkbox = UiTheme.LoadImage(imagePath);
        var size = Math.Min(18, Math.Max(12, e.Bounds.Height - 8));
        var boxRect = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + (e.Bounds.Height - size) / 2, size, size);
        e.Graphics.DrawImage(checkbox, boxRect);

        var textRect = new Rectangle(boxRect.Right + 8, e.Bounds.Top, Math.Max(0, e.Bounds.Width - boxRect.Right - 14), e.Bounds.Height);
        TextRenderer.DrawText(
            e.Graphics,
            GetItemText(Items[e.Index]),
            Font,
            textRect,
            Enabled ? (selected ? Color.White : UiTheme.Text) : UiTheme.Disabled,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(e.Bounds, -2, -2), Color.White, backColor);
    }

    protected override void OnItemCheck(ItemCheckEventArgs ice)
    {
        base.OnItemCheck(ice);
        if (IsHandleCreated && !IsDisposed)
            BeginInvoke(new Action(Invalidate));
        else
            Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var index = IndexFromPoint(e.Location);
        if (index == _hoverIndex)
            return;

        _hoverIndex = index;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverIndex = -1;
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using var background = new SolidBrush(UiTheme.List);
        pevent.Graphics.FillRectangle(background, ClientRectangle);
    }
}
