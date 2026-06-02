using System.ComponentModel;

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

        using var bg = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(bg, rect);

        var range = Math.Max(1, _maximum - _minimum);
        var ratio = Math.Clamp((float)(_value - _minimum) / range, 0f, 1f);
        var fillWidth = (int)Math.Round((rect.Width - 1) * ratio);
        if (fillWidth > 0)
        {
            using var fill = new SolidBrush(BarColor);
            e.Graphics.FillRectangle(fill, 0, 0, fillWidth, rect.Height - 1);
        }

        using var border = new Pen(BorderColor);
        e.Graphics.DrawRectangle(border, 0, 0, rect.Width - 1, rect.Height - 1);

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
}
