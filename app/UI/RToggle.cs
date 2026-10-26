using System.Drawing.Drawing2D;

namespace GHelper.UI;

// A CheckBox retains native keyboard, CheckedChanged, AutoCheck and UI Automation semantics.
internal sealed class RToggle : CheckBox
{
    internal RToggle() { DoubleBuffered = true; }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var size = base.GetPreferredSize(proposedSize);
        return new Size(size.Width + DesignSystem.Scale(this, 22), Math.Max(size.Height, DesignSystem.Scale(this, 26)));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        int width = DesignSystem.Scale(this, 32), height = DesignSystem.Scale(this, 19);
        bool rtl = RightToLeft == RightToLeft.Yes;
        var track = new Rectangle(rtl ? Width - Padding.Right - width : Padding.Left, (Height - height) / 2, width, height);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var color = Checked ? DesignSystem.Accent : DesignSystem.SecondaryText;
        if (!Enabled) color = DesignSystem.Separator;
        using var path = DesignSystem.RoundedRectangle(track, height / 2);
        using var brush = new SolidBrush(color);
        e.Graphics.FillPath(brush, path);
        int inset = DesignSystem.Scale(this, 2), thumb = height - inset * 2;
        int x = Checked ? track.Right - inset - thumb : track.Left + inset;
        if (rtl) x = Checked ? track.Left + inset : track.Right - inset - thumb;
        using var thumbBrush = new SolidBrush(SystemInformation.HighContrast ? SystemColors.HighlightText : Color.White);
        e.Graphics.FillEllipse(thumbBrush, x, track.Top + inset, thumb, thumb);
        int gap = DesignSystem.Scale(this, 9);
        var text = rtl
            ? new Rectangle(Padding.Left, 0, Math.Max(0, track.Left - gap - Padding.Left), Height)
            : new Rectangle(track.Right + gap, 0, Math.Max(0, Width - track.Right - gap - Padding.Right), Height);
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;
        if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
        else if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        if (rtl) flags |= TextFormatFlags.Right | TextFormatFlags.RightToLeft;
        TextRenderer.DrawText(e.Graphics, Text, Font, text, Enabled ? ForeColor : SystemColors.GrayText, flags);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle, ForeColor, BackColor);
    }
}
