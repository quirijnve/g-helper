using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public class RButton : Button
    {
        private int borderRadius = 5;
        private Color borderColor = Color.Transparent;
        private bool activated;
        private bool secondary;
        private GraphicsPath? outline;
        private GraphicsPath? corners;

        public int BorderRadius
        {
            get => borderRadius;
            set { if (borderRadius == value) return; borderRadius = value; ResetGeometry(); }
        }
        public Color BorderColor
        {
            get => borderColor;
            set { if (borderColor == value) return; borderColor = value; Invalidate(); }
        }
        public bool Activated
        {
            get => activated;
            set
            {
                if (activated == value) return;
                activated = value;
                AccessibleDescription = activated ? "Active" : null;
                Invalidate();
            }
        }
        public bool Secondary
        {
            get => secondary;
            set { if (secondary == value) return; secondary = value; Invalidate(); }
        }
        public bool Borderless { get; set; }

        public RButton()
        {
            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColorChanged += (_, _) => UpdateHoverColor();
            UpdateHoverColor();
        }

        private void UpdateHoverColor()
        {
            var target = BackColor.GetBrightness() > .5f ? Color.Black : Color.White;
            FlatAppearance.MouseOverBackColor = Blend(BackColor, target, .05f);
            FlatAppearance.MouseDownBackColor = Blend(BackColor, target, .10f);
        }

        private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(from.A,
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));

        private void ResetGeometry()
        {
            outline?.Dispose(); outline = null;
            corners?.Dispose(); corners = null;
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e) { ResetGeometry(); base.OnSizeChanged(e); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { ResetGeometry(); base.OnDpiChangedAfterParent(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Native painting retains text/image layout, mnemonics, pressed state and RTL.
            base.OnPaint(e);
            if (Width < 3 || Height < 3) return;
            if (outline == null)
            {
                int radius = DesignSystem.Scale(this, Math.Max(DesignSystem.CornerRadius, borderRadius / 2));
                outline = DesignSystem.RoundedRectangle(new Rectangle(1, 1, Width - 3, Height - 3), radius);
                corners = new GraphicsPath(FillMode.Alternate);
                corners.AddRectangle(ClientRectangle);
                corners.AddPath(outline, false);
            }
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(Parent?.BackColor ?? BackColor))
                e.Graphics.FillPath(brush, corners!);
            if (!Borderless && (!RForm.flatTheme || activated))
            {
                var stroke = activated && borderColor.A > 0 ? borderColor : FlatAppearance.BorderColor;
                if (SystemInformation.HighContrast) stroke = activated ? SystemColors.Highlight : SystemColors.WindowText;
                if (stroke.A > 0)
                {
                    using var pen = new Pen(stroke, activated ? Math.Max(1, DesignSystem.Scale(this, 2)) : 1);
                    pen.Alignment = PenAlignment.Inset;
                    e.Graphics.DrawPath(pen, outline);
                }
            }
            if (Focused && ShowFocusCues)
            {
                var focus = Rectangle.Inflate(ClientRectangle, -DesignSystem.Scale(this, 4), -DesignSystem.Scale(this, 4));
                ControlPaint.DrawFocusRectangle(e.Graphics, focus, ForeColor, BackColor);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { outline?.Dispose(); corners?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
