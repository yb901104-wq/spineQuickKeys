using System.Drawing.Drawing2D;
using System.Drawing.Text;
using KeyMacro.Models;

namespace KeyMacro.Forms;

public class VirtualButtonWidget : UserControl
{
    private VirtualButton _vbtn;
    private bool _isPressed;
    private bool _isDragging;
    private Point _dragStart;
    private bool _inputHover;
    private bool _isActive;

    private static readonly Color ColorNormalTop = Color.FromArgb(0x44, 0x44, 0x44);
    private static readonly Color ColorNormalBottom = Color.FromArgb(0x38, 0x38, 0x38);
    private static readonly Color ColorActive = Color.FromArgb(0x00, 0xE5, 0xFF);
    private static readonly Color ColorText = Color.FromArgb(0xE0, 0xE0, 0xE0);
    private static readonly Color ColorRecessed = Color.FromArgb(0x12, 0x12, 0x12);
    private static readonly Color ColorHighlightBorder = Color.FromArgb(0x55, 0x55, 0x55);

    public VirtualButton VirtualButton => _vbtn;
    public event Action<VirtualButtonWidget>? Clicked;
    public event Action<VirtualButtonWidget, int, int>? Dragged;
    public event Action<VirtualButtonWidget, Point>? ContextMenuRequested;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Invalidate(); }
    }

    public VirtualButtonWidget(VirtualButton vbtn)
    {
        _vbtn = vbtn;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        UpdateSize();

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseClick += OnMouseClick;
        MouseLeave += (_, _) => _inputHover = false;
    }

    public void UpdateButton(VirtualButton vbtn)
    {
        _vbtn = vbtn;
        UpdateSize();
        Invalidate();
    }

    private void UpdateSize()
    {
        Size = _vbtn.StyleType switch
        {
            VirtualButtonStyle.SmallIcon => new Size(64, 64),
            VirtualButtonStyle.LargeIcon => new Size(96, 96),
            VirtualButtonStyle.LoopIcon => new Size(96, 128),
            _ => new Size(64, 64)
        };
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Check loop_count input zone for LoopIcon
            if (_vbtn.StyleType == VirtualButtonStyle.LoopIcon && _inputHover)
                return;
            _isPressed = true;
            _isDragging = true;
            _dragStart = e.Location;
            Invalidate();
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        // Check if hovering over the input area (bottom of loop icon)
        if (_vbtn.StyleType == VirtualButtonStyle.LoopIcon && e.Y > Height - 32)
        {
            _inputHover = true;
            Cursor = Cursors.IBeam;
        }
        else if (_inputHover)
        {
            _inputHover = false;
            Cursor = Cursors.Default;
        }

        if (_isDragging && _isPressed)
        {
            var dx = e.X - _dragStart.X;
            var dy = e.Y - _dragStart.Y;
            if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
            {
                Dragged?.Invoke(this, dx, dy);
            }
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
        if (_isPressed)
        {
            _isPressed = false;
            Invalidate();
        }
    }

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            ContextMenuRequested?.Invoke(this, e.Location);
            return;
        }
        if (e.Button == MouseButtons.Left && !_isDragging)
        {
            // Check input area for loop button
            if (_vbtn.StyleType == VirtualButtonStyle.LoopIcon && _inputHover)
                return;
            Clicked?.Invoke(this);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.Transparent);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = 4;

        // Draw main button body
        if (_isPressed)
            DrawRecessedButton(g, rect, radius);
        else if (_isActive)
            DrawActiveButton(g, rect, radius);
        else
            DrawNormalButton(g, rect, radius);

        // Draw content
        DrawContent(g, rect);
    }

    private void DrawNormalButton(Graphics g, Rectangle rect, int radius)
    {
        using var path = GetRoundRect(rect, radius);
        using var brush = new LinearGradientBrush(rect, ColorNormalTop, ColorNormalBottom, LinearGradientMode.Vertical);
        g.FillPath(brush, path);

        // Top edge highlight
        using var topPen = new Pen(Color.FromArgb(60, 255, 255, 255));
        var topRect = new Rectangle(rect.X, rect.Y, rect.Width, 1);
        using var topPath = GetRoundRect(topRect, radius);
        g.DrawPath(topPen, topPath);

        // Bottom shadow
        using var shadowPen = new Pen(Color.FromArgb(40, 0, 0, 0));
        var bottomRect = new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1);
        g.DrawLine(shadowPen, bottomRect.Left, bottomRect.Top, bottomRect.Right, bottomRect.Top);

        // Border
        using var borderPen = new Pen(ColorHighlightBorder);
        g.DrawPath(borderPen, path);
    }

    private void DrawActiveButton(Graphics g, Rectangle rect, int radius)
    {
        using var path = GetRoundRect(rect, radius);
        using var brush = new LinearGradientBrush(rect,
            Color.FromArgb(0x3A, 0x3A, 0x3A), Color.FromArgb(0x30, 0x30, 0x30), LinearGradientMode.Vertical);
        g.FillPath(brush, path);

        // Active glow border
        using var glowPen = new Pen(Color.FromArgb(180, ColorActive));
        g.DrawPath(glowPen, path);

        // Inner glow
        using var innerBrush = new SolidBrush(Color.FromArgb(20, ColorActive));
        g.FillPath(innerBrush, path);
    }

    private void DrawRecessedButton(Graphics g, Rectangle rect, int radius)
    {
        using var path = GetRoundRect(rect, radius);
        using var brush = new SolidBrush(ColorRecessed);
        g.FillPath(brush, path);

        // Inner shadow (top)
        using var innerShadow = new LinearGradientBrush(
            new Rectangle(rect.X, rect.Y, rect.Width, 8),
            Color.FromArgb(60, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical);
        g.FillPath(innerShadow, path);

        // Border
        using var borderPen = new Pen(Color.FromArgb(0x33, 0x33, 0x33));
        g.DrawPath(borderPen, path);
    }

    private void DrawContent(Graphics g, Rectangle rect)
    {
        var font = new Font("Microsoft YaHei", 9, FontStyle.Regular);
        var iconFont = new Font("Segoe UI", 18, FontStyle.Bold);
        var smallFont = new Font("Microsoft YaHei", 7, FontStyle.Regular);
        var centerX = rect.Width / 2f;
        var iconRect = new RectangleF(0, 8, rect.Width, 32);
        var nameRect = new RectangleF(4, 42, rect.Width - 8, 18);

        using var textBrush = new SolidBrush(_isActive ? ColorActive : ColorText);
        using var nameBrush = new SolidBrush(ColorText);

        // Draw icon placeholder (first character)
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var iconChar = _vbtn.Name.Length > 0 ? _vbtn.Name[0].ToString() : "?";
        g.DrawString(iconChar, iconFont, textBrush, iconRect, sf);

        // Draw name
        nameRect.Offset(0, 0);
        g.DrawString(_vbtn.Name, font, nameBrush, nameRect, sf);

        // For large_icon, draw binding status
        if (_vbtn.StyleType == VirtualButtonStyle.LargeIcon)
        {
            var statusText = string.IsNullOrEmpty(_vbtn.BindActionId) ? "未绑定" : "已绑定";
            using var statusBrush = new SolidBrush(Color.FromArgb(0x88, 0x88, 0x88));
            var statusRect = new RectangleF(4, 62, rect.Width - 8, 14);
            g.DrawString(statusText, smallFont, statusBrush, statusRect, sf);
        }

        // For loop_icon, draw input area at bottom
        if (_vbtn.StyleType == VirtualButtonStyle.LoopIcon)
        {
            var inputRect = new Rectangle(4, Height - 30, Width - 8, 26);
            DrawLoopInputArea(g, inputRect);
        }
    }

    private void DrawLoopInputArea(Graphics g, Rectangle rect)
    {
        // Recessed background for input area
        using var path = GetRoundRect(rect, 3);
        using var bgBrush = new SolidBrush(ColorRecessed);
        g.FillPath(bgBrush, path);
        using var borderPen = new Pen(Color.FromArgb(0x33, 0x33, 0x33));
        g.DrawPath(borderPen, path);

        // Inner shadow top
        using var shadowBrush = new LinearGradientBrush(
            new Rectangle(rect.X, rect.Y, rect.Width, 4),
            Color.FromArgb(50, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical);
        g.FillPath(shadowBrush, path);

        // Draw text: "x{count} {interval}ms"
        var text = $"{_vbtn.LoopCount}x {_vbtn.LoopInterval}ms";
        using var font = new Font("Microsoft YaHei", 7, FontStyle.Regular);
        using var brush = new SolidBrush(ColorText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, brush, rect, sf);
    }

    private static GraphicsPath GetRoundRect(Rectangle rect, int r)
    {
        var path = new GraphicsPath();
        r = Math.Min(r, Math.Min(rect.Width, rect.Height));
        path.AddArc(rect.X, rect.Y, r, r, 180, 90);
        path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
        path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
        path.CloseFigure();
        return path;
    }
}
