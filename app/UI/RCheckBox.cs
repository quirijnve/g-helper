using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public class RCheckBox : CheckBox
    {
        private int borderRadius = 5;
        public int BorderRadius
        {
            get => borderRadius;
            set => borderRadius = value;
        }

        public RCheckBox()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            if (Parent == null || FlatAppearance.BorderColor.A == 0 || RForm.flatTheme) return;

            int radius = DesignSystem.Scale(this, Math.Max(DesignSystem.CornerRadius, borderRadius / 2));

            Rectangle borderRect = new Rectangle(0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
            Color sideColor = FlatAppearance.BorderColor.A > 0 ? FlatAppearance.BorderColor : RForm.borderSecond;

            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath roundedPath = RComboBox.RoundedRect(borderRect, radius, radius))
            using (GraphicsPath cutoutPath = new GraphicsPath())
            using (Brush parentBrush = new SolidBrush(Parent.BackColor))
            {
                cutoutPath.AddRectangle(ClientRectangle);
                cutoutPath.AddPath(roundedPath, false);
                cutoutPath.FillMode = FillMode.Alternate;
                pevent.Graphics.FillPath(parentBrush, cutoutPath);
            }

            using var pen = new Pen(sideColor);
            using var outline = DesignSystem.RoundedRectangle(borderRect, DesignSystem.Scale(this, DesignSystem.CornerRadius));
            pevent.Graphics.DrawPath(pen, outline);
        }
    }
}
