using KeyMacro.Services;

namespace KeyMacro.Controls;

public class DarkComboBox : ComboBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;

    public DarkComboBox()
    {
        FlatStyle = FlatStyle.Flat;
        BackColor = UiTheme.Input;
        ForeColor = UiTheme.Text;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = Math.Max(ItemHeight, 24);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count)
            return;

        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backColor = selected ? Color.FromArgb(0x5B, 0x77, 0x82) : UiTheme.Input;
        using (var background = new SolidBrush(backColor))
            e.Graphics.FillRectangle(background, e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            GetItemText(Items[e.Index]),
            Font,
            Rectangle.Inflate(e.Bounds, -7, 0),
            selected ? Color.White : UiTheme.Text,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WmPaint || m.Msg == WmNcPaint)
            PaintChrome();
    }

    protected override void OnDropDown(EventArgs e)
    {
        base.OnDropDown(e);
        Invalidate();
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
        base.OnDropDownClosed(e);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Invalidate();
    }

    private void PaintChrome()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0)
            return;

        using var graphics = CreateGraphics();
        var rect = ClientRectangle;
        var buttonWidth = Math.Max(22, SystemInformation.HorizontalScrollBarArrowWidth + 4);
        var buttonRect = new Rectangle(rect.Right - buttonWidth - 1, rect.Top + 1, buttonWidth, Math.Max(0, rect.Height - 2));
        var borderColor = Enabled ? UiTheme.BorderStrong : UiTheme.Disabled;
        var buttonColor = DroppedDown ? Color.FromArgb(0x25, 0x56, 0x68) : UiTheme.ControlWell;

        using (var button = new SolidBrush(buttonColor))
            graphics.FillRectangle(button, buttonRect);
        using (var separator = new Pen(UiTheme.Border))
            graphics.DrawLine(separator, buttonRect.Left, buttonRect.Top + 3, buttonRect.Left, buttonRect.Bottom - 3);
        using (var border = new Pen(borderColor))
            graphics.DrawRectangle(border, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

        var arrowX = buttonRect.Left + buttonRect.Width / 2 - 4;
        var arrowY = buttonRect.Top + buttonRect.Height / 2 - 2;
        using var arrow = new SolidBrush(Enabled ? UiTheme.Muted : UiTheme.Disabled);
        graphics.FillPolygon(arrow, [
            new Point(arrowX, arrowY),
            new Point(arrowX + 8, arrowY),
            new Point(arrowX + 4, arrowY + 5)
        ]);
    }
}
