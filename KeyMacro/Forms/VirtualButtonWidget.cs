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
    private bool _settingsHover;
    private bool _isActive;
    private bool _isFirstInRow;
    private bool _isLastInRow;

    // Bar constants
    private const int BaseHeight = 48;
    private const int SmallWidth = 48;
    private const int LargeWidth = 96;
    private const int LoopWidth = 110;

    // Colors per spec
    private static readonly Color ColorTopRim = Color.FromArgb(0x3C, 0x3C, 0x3C);
    private static readonly Color ColorBarTop = Color.FromArgb(0x4A, 0x4A, 0x4A);
    private static readonly Color ColorBarBottom = Color.FromArgb(0x38, 0x38, 0x38);
    private static readonly Color ColorGrooveDark = Color.FromArgb(0x1A, 0x1A, 0x1A);
    private static readonly Color ColorGrooveLight = Color.FromArgb(0x4A, 0x4A, 0x4A);
    private static readonly Color ColorRecessed = Color.FromArgb(0x0D, 0x0D, 0x0D);
    private static readonly Color ColorActiveGlow = Color.FromArgb(0x00, 0xE5, 0xFF);
    private static readonly Color ColorText = Color.FromArgb(0xE0, 0xE0, 0xE0);

    public VirtualButton VirtualButton => _vbtn;
    public event Action<VirtualButtonWidget>? Clicked;
    public event Action<VirtualButtonWidget, int, int>? Dragged;
    public event Action<VirtualButtonWidget, Point>? ContextMenuRequested;
    public event Action<VirtualButtonWidget>? SettingsClicked;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Invalidate(); }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool IsFirstInRow
    {
        get => _isFirstInRow;
        set { _isFirstInRow = value; Invalidate(); }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool IsLastInRow
    {
        get => _isLastInRow;
        set { _isLastInRow = value; Invalidate(); }
    }

    /// <summary>Scale factor based on window width (1.0 = default).</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public float ScaleFactor { get; set; } = 1f;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool AllowDragging { get; set; } = true;

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
        MouseLeave += (_, _) => _settingsHover = false;
    }

    public void UpdateButton(VirtualButton vbtn)
    {
        _vbtn = vbtn;
        Invalidate();
    }

    public int Scaled(int val) => Math.Max(1, (int)(val * ScaleFactor));

    public void UpdateSize()
    {
        int w = _vbtn.StyleType switch
        {
            VirtualButtonStyle.SmallIcon => Scaled(SmallWidth),
            VirtualButtonStyle.LargeIcon => Scaled(LargeWidth),
            VirtualButtonStyle.LoopIcon => Scaled(LoopWidth),
            _ => Scaled(SmallWidth)
        };
        Size = new Size(w, Scaled(BaseHeight));
    }

    // ── Hit detection ──

    private Rectangle GetSettingsRect()
    {
        if (_vbtn.StyleType != VirtualButtonStyle.LoopIcon)
            return Rectangle.Empty;
        var sw = Scaled(44);
        return new Rectangle(Width - sw, 0, sw, Height);
    }

    // ── Mouse ──

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isPressed = true;
            _isDragging = true;
            _dragStart = e.Location;
            Invalidate();
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        var sr = GetSettingsRect();
        if (!sr.IsEmpty && sr.Contains(e.Location))
        {
            _settingsHover = true;
            Cursor = Cursors.Hand;
        }
        else if (_settingsHover)
        {
            _settingsHover = false;
            Cursor = Cursors.Default;
        }

        if (_isDragging && _isPressed && AllowDragging)
        {
            var dx = e.X - _dragStart.X;
            var dy = e.Y - _dragStart.Y;
            if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)
                Dragged?.Invoke(this, dx, dy);
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
            if (GetSettingsRect().Contains(e.Location))
            {
                SettingsClicked?.Invoke(this);
                return;
            }
            Clicked?.Invoke(this);
        }
    }

    // ── Painting ──

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.Transparent);

        var rect = new Rectangle(0, 0, Width, Height);
        var radius = 2;

        if (_isPressed)
            DrawPressed(g, rect, radius);
        else
            DrawStatic(g, rect, radius);

        DrawContent(g, rect);

        if (_isActive && !_isPressed)
            DrawActiveOverlay(g, rect, radius);
    }

    private void DrawStatic(Graphics g, Rectangle rect, int radius)
    {
        var barRect = new Rectangle(0, 0, Width - 1, Height - 1);

        using var bodyPath = MakeRoundedPath(barRect, radius, _isFirstInRow, _isLastInRow);
        using var bodyBrush = new LinearGradientBrush(barRect, ColorBarTop, ColorBarBottom, LinearGradientMode.Vertical);
        g.FillPath(bodyBrush, bodyPath);

        // Top rim 1px #3C3C3C
        using var rimPen = new Pen(ColorTopRim);
        var rimRect = new Rectangle(barRect.X, barRect.Y, barRect.Width, 1);
        using var rimPath = MakeRoundedPath(rimRect, radius, _isFirstInRow, _isLastInRow);
        g.DrawPath(rimPen, rimPath);

        // V-groove left (except first)
        if (!_isFirstInRow)
        {
            using var darkPen = new Pen(ColorGrooveDark);
            g.DrawLine(darkPen, 0, 2, 0, Height - 3);
            using var lightPen = new Pen(ColorGrooveLight);
            g.DrawLine(lightPen, 1, 2, 1, Height - 3);
        }

        // Outer 1px #000000 border
        using var borderPen = new Pen(Color.FromArgb(0x00, 0x00, 0x00));
        if (_isFirstInRow)
            g.DrawLine(borderPen, 0, 0, 0, Height - 1);
        if (_isLastInRow)
            g.DrawLine(borderPen, Width - 1, 0, Width - 1, Height - 1);
        g.DrawLine(borderPen, 0, Height - 1, Width - 1, Height - 1);
    }

    private void DrawPressed(Graphics g, Rectangle rect, int radius)
    {
        var barRect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var bodyPath = MakeRoundedPath(barRect, radius, _isFirstInRow, _isLastInRow);
        using var bodyBrush = new LinearGradientBrush(barRect,
            Color.FromArgb(0x35, 0x35, 0x35), Color.FromArgb(0x2A, 0x2A, 0x2A), LinearGradientMode.Vertical);
        g.FillPath(bodyBrush, bodyPath);
    }

    private void DrawActiveOverlay(Graphics g, Rectangle rect, int radius)
    {
        var innerRect = new Rectangle(3, 3, Width - 7, Height - 7);
        using var glowPen = new Pen(Color.FromArgb(120, ColorActiveGlow), 1);
        using var glowPath = MakeRoundedPath(innerRect, 1, true, true);
        g.DrawPath(glowPen, glowPath);
    }

    private void DrawContent(Graphics g, Rectangle rect)
    {
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var textBrush = new SolidBrush(_isActive ? ColorActiveGlow : ColorText);
        using var dimBrush = new SolidBrush(Color.FromArgb(0x88, 0x88, 0x88));

        var iconChar = _vbtn.Name.Length > 0 ? _vbtn.Name[0].ToString() : "☐";

        switch (_vbtn.StyleType)
        {
            case VirtualButtonStyle.SmallIcon:
            {
                using var iconFont = new Font("Segoe UI", Scaled(14), FontStyle.Bold);
                using var nameFont = new Font("Microsoft YaHei", Scaled(7), FontStyle.Regular);
                var iconR = new RectangleF(0, Scaled(4), Width, Scaled(24));
                g.DrawString(iconChar, iconFont, textBrush, iconR, sf);
                var nameR = new RectangleF(2, Scaled(30), Width - 4, Scaled(16));
                g.DrawString(_vbtn.Name, nameFont, dimBrush, nameR, sf);
                break;
            }
            case VirtualButtonStyle.LargeIcon:
            {
                int halfW = Width / 2;
                // Icon left
                using var iconFont = new Font("Segoe UI", Scaled(18), FontStyle.Bold);
                var iconR = new RectangleF(0, Scaled(4), halfW, Height);
                g.DrawString(iconChar, iconFont, textBrush, iconR, sf);
                // Name + status right
                using var nameFont = new Font("Microsoft YaHei", Scaled(8), FontStyle.Regular);
                var nameR = new RectangleF(halfW, Scaled(6), halfW - 4, Scaled(18));
                g.DrawString(_vbtn.Name, nameFont, dimBrush, nameR, sf);
                var status = string.IsNullOrEmpty(_vbtn.BindActionId) ? "未绑定" : "已绑定";
                var statusR = new RectangleF(halfW, Scaled(26), halfW - 4, Scaled(14));
                using var tinyFont = new Font("Microsoft YaHei", Scaled(6), FontStyle.Regular);
                g.DrawString(status, tinyFont, dimBrush, statusR, sf);
                break;
            }
            case VirtualButtonStyle.LoopIcon:
            {
                int leftW = Width - Scaled(44);
                // Icon + Name on the left
                using var iconFont = new Font("Segoe UI", Scaled(14), FontStyle.Bold);
                var iconR = new RectangleF(0, Scaled(4), leftW, Scaled(22));
                g.DrawString(iconChar, iconFont, textBrush, iconR, sf);
                using var nameFont = new Font("Microsoft YaHei", Scaled(7), FontStyle.Regular);
                var nameR = new RectangleF(2, Scaled(28), leftW - 4, Scaled(16));
                g.DrawString(_vbtn.Name, nameFont, dimBrush, nameR, sf);

                // Right: recessed settings panel
                var sr = GetSettingsRect();
                DrawSettingsPanel(g, sr);
                break;
            }
        }
    }

    private void DrawSettingsPanel(Graphics g, Rectangle rect)
    {
        // Recessed background
        var inner = new Rectangle(rect.X + 2, rect.Y + 3, rect.Width - 4, rect.Height - 6);
        using var path = MakeRoundedPath(inner, 2, true, true);
        using var bgBrush = new SolidBrush(ColorRecessed);
        g.FillPath(bgBrush, path);

        // Inner shadow top
        using var shadowBrush = new LinearGradientBrush(
            new Rectangle(inner.X, inner.Y, inner.Width, 4),
            Color.FromArgb(140, 0, 0, 0), Color.Transparent, LinearGradientMode.Vertical);
        g.FillPath(shadowBrush, path);

        // Count text
        var text = $"{_vbtn.LoopCount}x";
        using var font = new Font("Microsoft YaHei", Scaled(8), FontStyle.Bold);
        using var brush = new SolidBrush(_settingsHover ? ColorActiveGlow : ColorText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, brush, inner, sf);
    }

    // ── Geometry ──

    private static GraphicsPath MakeRoundedPath(Rectangle rect, int r, bool roundLeft, bool roundRight)
    {
        var path = new GraphicsPath();
        r = Math.Min(r, Math.Min(rect.Width / 2, rect.Height / 2));
        if (r <= 0) { path.AddRectangle(rect); path.CloseFigure(); return path; }

        if (roundLeft && roundRight)
        {
            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
        }
        else if (roundLeft)
        {
            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.AddLine(rect.X + r, rect.Bottom, rect.Right, rect.Bottom);
        }
        else if (roundRight)
        {
            path.AddLine(rect.X, rect.Y, rect.Right - r, rect.Y);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddLine(rect.X, rect.Bottom, rect.Right - r, rect.Bottom);
        }
        else
            path.AddRectangle(rect);

        path.CloseFigure();
        return path;
    }
}
