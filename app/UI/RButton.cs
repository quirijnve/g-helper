using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GHelper.UI
{
    public class RButton : Button
    {
        private int borderRadius = 5;
        private Color borderColor = Color.Transparent;
        private bool activated;
        private bool secondary;
        private Color restingBackColor;
        private Color restingForeColor;
        private bool applyingSelection;
        private Image? sourceImage;
        private Image? selectedImage;
        private Color selectedImageColor;

        public new Image? Image
        {
            get => sourceImage;
            set
            {
                if (ReferenceEquals(sourceImage, value)) return;
                base.Image = null;
                sourceImage = value;
                selectedImage?.Dispose(); selectedImage = null;
                ApplySelection();
            }
        }
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
            set { if (borderColor == value) return; borderColor = value; ApplySelection(); Invalidate(); }
        }
        public bool Activated
        {
            get => activated;
            set
            {
                if (activated == value) return;
                activated = value;
                ApplySelection();
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

        protected override void OnBackColorChanged(EventArgs e)
        {
            if (!applyingSelection) { restingBackColor = BackColor; ApplySelection(); }
            base.OnBackColorChanged(e);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            if (!applyingSelection) { restingForeColor = ForeColor; ApplySelection(); }
            base.OnForeColorChanged(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            ApplySelection();
            base.OnEnabledChanged(e);
        }

        private void ApplySelection()
        {
            if (applyingSelection) return;
            bool selected = activated && Enabled && !Borderless && borderColor.A > 0 && !DesignSystem.Accent.IsEmpty;
            applyingSelection = true;
            try
            {
                BackColor = selected
                    ? SystemInformation.HighContrast ? SystemColors.Highlight : Blend(restingBackColor, DesignSystem.Accent, ControlHelper.DarkMode ? .22f : .10f)
                    : restingBackColor;
                ForeColor = selected
                    ? SystemInformation.HighContrast ? SystemColors.HighlightText : ControlHelper.DarkMode ? Blend(DesignSystem.Accent, Color.White, .35f) : DesignSystem.Accent
                    : restingForeColor;
                if (selected && sourceImage != null)
                {
                    if (selectedImage == null || selectedImageColor != ForeColor)
                    {
                        base.Image = null;
                        selectedImage?.Dispose();
                        selectedImageColor = ForeColor;
                        selectedImage = TintIcon(sourceImage, ForeColor);
                    }
                    base.Image = selectedImage;
                }
                else base.Image = sourceImage;
            }
            finally { applyingSelection = false; }
        }

        private static Bitmap TintIcon(Image source, Color color)
        {
            var bitmap = new Bitmap(source.Width, source.Height);
            using var graphics = Graphics.FromImage(bitmap);
            using var attributes = new ImageAttributes();
            attributes.SetColorMatrix(new ColorMatrix(new[]
            {
                new float[] { 0, 0, 0, 0, 0 }, new float[] { 0, 0, 0, 0, 0 },
                new float[] { 0, 0, 0, 0, 0 }, new float[] { 0, 0, 0, 1, 0 },
                new float[] { color.R / 255f, color.G / 255f, color.B / 255f, 0, 1 }
            }));
            graphics.DrawImage(source, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            return bitmap;
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
                var stroke = activated && borderColor.A > 0 ? DesignSystem.Accent : FlatAppearance.BorderColor;
                if (SystemInformation.HighContrast) stroke = activated ? SystemColors.Highlight : SystemColors.WindowText;
                if (stroke.A > 0)
                {
                    using var pen = new Pen(stroke, activated ? Math.Max(1, DeviceDpi / 96f) : 1);
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
            if (disposing) { base.Image = null; selectedImage?.Dispose(); outline?.Dispose(); corners?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
