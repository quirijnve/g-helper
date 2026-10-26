namespace GHelper.UI;

internal sealed class ControlCenterHeader : Control
{
    private readonly Font titleFont = new(DesignSystem.FontFamily, DesignSystem.TitlePoints, FontStyle.Bold);
    private readonly Font captionFont = new(DesignSystem.FontFamily, DesignSystem.CaptionPoints);

    internal ControlCenterHeader()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = false;
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = "G-Helper";
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int inset = DesignSystem.Scale(this, 6);
        TextRenderer.DrawText(e.Graphics, "G-Helper", titleFont,
            new Rectangle(inset, DesignSystem.Scale(this, 8), Width - inset * 2, DesignSystem.Scale(this, 35)),
            DesignSystem.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics, Text, captionFont,
            new Rectangle(inset + 1, DesignSystem.Scale(this, 45), Width - inset * 2, DesignSystem.Scale(this, 22)),
            DesignSystem.SecondaryText, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { titleFont.Dispose(); captionFont.Dispose(); }
        base.Dispose(disposing);
    }
}
