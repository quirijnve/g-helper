using System.Drawing.Drawing2D;

namespace GHelper.UI;

internal sealed class RCardPanel : Panel
{
    internal RCardPanel() { DoubleBuffered = true; ResizeRedraw = true; }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? DesignSystem.Canvas);
        var bounds = new Rectangle(0, 0, Width - 1, Height - DesignSystem.Scale(this, DesignSystem.Gap) - 1);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = DesignSystem.RoundedRectangle(bounds, DesignSystem.Scale(this, DesignSystem.CardRadius));
        using var brush = new SolidBrush(BackColor);
        using var pen = new Pen(DesignSystem.Separator);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
    }
}
