using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public class RTrackBar : TrackBar
    {
        private const int WM_PAINT = 0x000F;

        private const int TBM_GETCHANNELRECT = 0x041A;
        private const int TBM_GETTHUMBRECT = 0x0419;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public RTrackBar()
        {
            DoubleBuffered = true;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Announce($"{AccessibleName}, slider, {Value}");
        }

        protected override void OnValueChanged(EventArgs e)
        {
            base.OnValueChanged(e);
            if (Focused) Announce(Value.ToString());
        }

        private void Announce(string text)
        {
            try
            {
                AccessibilityObject.RaiseAutomationNotification(
                    System.Windows.Forms.Automation.AutomationNotificationKind.Other,
                    System.Windows.Forms.Automation.AutomationNotificationProcessing.MostRecent,
                    text);
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                PaintChannel();
            }
        }

        private void PaintChannel()
        {
            // Keep native input, range, tick frequency and UI Automation. Only paint changes.
            if (Orientation != Orientation.Horizontal || Width < 4 || Height < 4) return;
            RECT channel = new();
            SendMessage(Handle, TBM_GETCHANNELRECT, 0, ref channel);
            RECT thumb = new();
            SendMessage(Handle, TBM_GETTHUMBRECT, 0, ref thumb);
            using var g = Graphics.FromHwnd(Handle);
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int center = (thumb.Left + thumb.Right) / 2;
            int y = (thumb.Top + thumb.Bottom) / 2;
            int thickness = Math.Max(2, DesignSystem.Scale(this, 3));
            var track = new Rectangle(channel.Left, y - thickness / 2, Math.Max(1, channel.Right - channel.Left), thickness);
            using var empty = new SolidBrush(DesignSystem.Separator);
            using var accent = new SolidBrush(Enabled ? DesignSystem.Accent : DesignSystem.SecondaryText);
            using var path = DesignSystem.RoundedRectangle(track, thickness / 2);
            g.FillPath(empty, path);
            var filled = RightToLeft == RightToLeft.Yes
                ? new Rectangle(Math.Clamp(center, track.Left, track.Right), track.Top, Math.Max(0, track.Right - center), track.Height)
                : new Rectangle(track.Left, track.Top, Math.Max(0, Math.Min(center, track.Right) - track.Left), track.Height);
            if (filled.Width > 0) g.FillRectangle(accent, filled);
            if (TickStyle != TickStyle.None && Maximum > Minimum && TickFrequency > 0)
            {
                using var tickPen = new Pen(DesignSystem.Separator);
                // Bound paint work for large native ranges without changing the range or steps.
                long step = (long)TickFrequency * Math.Max(1, ((long)Maximum - Minimum) / TickFrequency / 100);
                for (long value = Minimum; value <= Maximum; value += step)
                {
                    float x = track.Left + track.Width * (float)(value - Minimum) / ((long)Maximum - Minimum);
                    if (TickStyle is TickStyle.TopLeft or TickStyle.Both)
                        g.DrawLine(tickPen, x, track.Top - DesignSystem.Scale(this, 5), x, track.Top - DesignSystem.Scale(this, 3));
                    if (TickStyle is TickStyle.BottomRight or TickStyle.Both)
                        g.DrawLine(tickPen, x, track.Bottom + DesignSystem.Scale(this, 3), x, track.Bottom + DesignSystem.Scale(this, 5));
                }
            }
            int diameter = Math.Max(8, Math.Min(thumb.Bottom - thumb.Top, DesignSystem.Scale(this, 14)));
            var circle = new Rectangle(center - diameter / 2, y - diameter / 2, diameter, diameter);
            using var knob = new SolidBrush(DesignSystem.Surface);
            using var outline = new Pen(Enabled ? DesignSystem.Accent : DesignSystem.Separator);
            g.FillEllipse(knob, circle);
            g.DrawEllipse(outline, circle);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, ClientRectangle, ForeColor, BackColor);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, ref RECT lParam);
    }
}
