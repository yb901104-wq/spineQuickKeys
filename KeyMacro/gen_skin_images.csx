#!/usr/bin/env dotnet-script
#r "System.Drawing.Common"

using System.Drawing;
using System.Drawing.Imaging;

var skinDir = @"skins/SpineSkin";
var styles = new[] { "small", "large", "loop" };
var states = new[] { "pressed", "active" };

foreach (var style in styles)
{
    var normalPath = Path.Combine(skinDir, $"btn_{style}_normal.png");
    if (!File.Exists(normalPath)) { Console.WriteLine($"Skip: {normalPath}"); continue; }

    using var normal = Image.FromFile(normalPath);

    foreach (var state in states)
    {
        var outPath = Path.Combine(skinDir, $"btn_{style}_{state}.png");
        if (File.Exists(outPath)) { Console.WriteLine($"Exists: {outPath}"); continue; }

        using var bmp = new Bitmap(normal.Width, normal.Height);
        using var g = Graphics.FromImage(bmp);
        g.DrawImage(normal, 0, 0);

        if (state == "pressed")
        {
            // Darken overlay
            using var brush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
            g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
        }
        else // active
        {
            // Cyan border glow
            using var pen = new Pen(Color.FromArgb(180, 0, 229, 255), 2);
            g.DrawRectangle(pen, 1, 1, bmp.Width - 3, bmp.Height - 3);
        }

        bmp.Save(outPath, ImageFormat.Png);
        Console.WriteLine($"Created: {outPath}");
    }
}
