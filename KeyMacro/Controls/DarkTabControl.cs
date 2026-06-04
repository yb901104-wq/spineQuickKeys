using System.Drawing.Drawing2D;
using KeyMacro.Services;

namespace KeyMacro.Controls;

public class DarkTabControl : TabControl
{
    public DarkTabControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Appearance = TabAppearance.Normal;
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(118, 36);
        Padding = new Point(14, 6);
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage page)
            ApplyPageTheme(page);
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        foreach (TabPage page in TabPages)
            ApplyPageTheme(page);
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using var brush = new SolidBrush(UiTheme.App);
        pevent.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var background = new SolidBrush(UiTheme.App))
            e.Graphics.FillRectangle(background, ClientRectangle);

        var pageRect = DisplayRectangle;
        pageRect.Inflate(3, 3);
        if (pageRect.Width > 0 && pageRect.Height > 0)
        {
            using var pageBack = new SolidBrush(UiTheme.Workspace);
            using var border = new Pen(UiTheme.Border);
            e.Graphics.FillRectangle(pageBack, pageRect);
            e.Graphics.DrawRectangle(border, pageRect.X, pageRect.Y, pageRect.Width - 1, pageRect.Height - 1);
        }

        for (var i = 0; i < TabPages.Count; i++)
            DrawTab(e.Graphics, i);
    }

    protected override void WndProc(ref Message m)
    {
        const int wmEraseBackground = 0x0014;
        if (m.Msg == wmEraseBackground)
        {
            m.Result = 1;
            return;
        }

        base.WndProc(ref m);
    }

    private void DrawTab(Graphics graphics, int index)
    {
        var selected = index == SelectedIndex;
        var rect = GetTabRect(index);
        rect = new Rectangle(rect.X + 2, rect.Y + 4, Math.Max(0, rect.Width - 4), Math.Max(0, rect.Height - 5));
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var fillColor = selected ? UiTheme.PanelAlt : Color.FromArgb(0x33, 0x33, 0x35);
        var borderColor = selected ? UiTheme.Blue : UiTheme.Border;
        using (var path = RoundedRect(rect, 7))
        using (var fill = new SolidBrush(fillColor))
        using (var border = new Pen(borderColor))
        {
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }

        if (selected)
        {
            var accent = new Rectangle(rect.X + 10, rect.Y + 3, Math.Max(1, rect.Width - 20), 3);
            using var accentBrush = new SolidBrush(UiTheme.Cyan);
            graphics.FillRectangle(accentBrush, accent);
        }

        TextRenderer.DrawText(
            graphics,
            TabPages[index].Text,
            new Font("Microsoft YaHei UI", 9f, selected ? FontStyle.Bold : FontStyle.Regular),
            rect,
            selected ? UiTheme.Text : UiTheme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void ApplyPageTheme(TabPage page)
    {
        page.BackColor = UiTheme.Workspace;
        page.ForeColor = UiTheme.Text;
        page.UseVisualStyleBackColor = false;
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
