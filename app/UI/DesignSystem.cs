using System.Drawing.Drawing2D;

namespace GHelper.UI;

// All dimensions are logical 96-DPI pixels. No images, timers or background work.
internal static class DesignSystem
{
    internal const string FontFamily = "Segoe UI";
    internal const float TitlePoints = 21;
    internal const float CaptionPoints = 9;
    internal const int CornerRadius = 8;
    internal const int CardRadius = 14;
    internal const int Gap = 10;
    internal static Color Canvas { get; private set; }
    internal static Color Surface { get; private set; }
    internal static Color Inset { get; private set; }
    internal static Color Text { get; private set; }
    internal static Color SecondaryText { get; private set; }
    internal static Color Separator { get; private set; }
    internal static Color Accent { get; private set; }

    internal static void SetTheme(bool dark)
    {
        if (SystemInformation.HighContrast)
        {
            Canvas = SystemColors.Control;
            Surface = Inset = SystemColors.Window;
            Text = SecondaryText = SystemColors.WindowText;
            Separator = SystemColors.WindowText;
            Accent = SystemColors.Highlight;
            return;
        }
        Canvas = dark ? Color.FromArgb(28, 28, 30) : Color.FromArgb(242, 242, 247);
        Surface = dark ? Color.FromArgb(44, 44, 46) : Color.White;
        Inset = dark ? Color.FromArgb(58, 58, 60) : Color.FromArgb(245, 245, 247);
        Text = dark ? Color.FromArgb(245, 245, 247) : Color.FromArgb(29, 29, 31);
        SecondaryText = dark ? Color.FromArgb(174, 174, 182) : Color.FromArgb(99, 99, 106);
        Separator = dark ? Color.FromArgb(68, 68, 72) : Color.FromArgb(222, 222, 227);
        Accent = dark ? Color.FromArgb(10, 132, 255) : Color.FromArgb(0, 102, 219);
    }

    internal static int Scale(Control control, int value) => (int)Math.Round(value * control.DeviceDpi / 96f);

    internal static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = Math.Min(Math.Min(bounds.Width, bounds.Height), Math.Max(0, radius * 2));
        if (diameter < 2) { path.AddRectangle(bounds); return path; }
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
