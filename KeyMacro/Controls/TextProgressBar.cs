using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace KeyMacro.Controls;

public class TextProgressBar : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private string _progressText = "";

    public TextProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint, true);
        Height = 22;
        BackColor = Color.White;
        ForeColor = Color.FromArgb(30, 30, 30);
        BarColor = Color.FromArgb(0x00, 0x78, 0xD7);
        BorderColor = Color.FromArgb(170, 170, 170);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum) _maximum = _minimum;
            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, _minimum, _maximum);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ProgressText
    {
        get => _progressText;
        set
        {
            _progressText = value ?? "";
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var rect = ClientRectangle;
        if (rect.Width <= 0 || rect.Height <= 0) return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using var bg = new SolidBrush(BackColor);
        using var outer = RoundedRect(new Rectangle(0, 0, rect.Width - 1, rect.Height - 1), 6);
        e.Graphics.FillPath(bg, outer);

        var range = Math.Max(1, _maximum - _minimum);
        var ratio = Math.Clamp((float)(_value - _minimum) / range, 0f, 1f);
        var innerRect = Rectangle.Inflate(rect, -4, -4);
        using var innerBg = new SolidBrush(Color.FromArgb(0x19, 0x19, 0x1B));
        using var inner = RoundedRect(new Rectangle(innerRect.X, innerRect.Y, innerRect.Width - 1, innerRect.Height - 1), 4);
        e.Graphics.FillPath(innerBg, inner);

        var fillWidth = (int)Math.Round((innerRect.Width - 1) * ratio);
        if (fillWidth > 0)
        {
            using var fill = new SolidBrush(BarColor);
            using var fillPath = RoundedRect(new Rectangle(innerRect.X, innerRect.Y, fillWidth, innerRect.Height - 1), 4);
            e.Graphics.FillPath(fill, fillPath);
        }

        using var border = new Pen(BorderColor);
        e.Graphics.DrawPath(border, outer);

        var text = string.IsNullOrWhiteSpace(_progressText)
            ? $"{(int)Math.Round(ratio * 100)}%"
            : _progressText;
        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            rect,
            ForeColor,
            TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.NoPrefix);
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Max(1, radius * 2);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
