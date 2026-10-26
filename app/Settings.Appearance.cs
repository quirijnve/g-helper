using GHelper.UI;

namespace GHelper;

public partial class SettingsForm
{
    // Keep the designer's controls, order, visibility rules and all hardware event wiring.
    // Only the presentation shell changes; hidden hardware-specific sections stay hidden.
    private void InitializeAppearance()
    {
        SuspendLayout();
        var sections = Controls.Cast<Control>().Where(c => c != panelFooter).ToArray();
        var viewport = new Panel
        {
            Name = "controlCenterContent", Dock = DockStyle.Fill, AutoScroll = true,
            TabIndex = 1, AccessibleRole = AccessibleRole.Grouping, AccessibleName = "G-Helper",
            Padding = new Padding(0, 0, DesignSystem.Scale(this, 4), 0)
        };
        viewport.SuspendLayout();
        foreach (var section in sections) viewport.Controls.Add(section);
        viewport.ResumeLayout(false);
        var header = new ControlCenterHeader
        {
            Name = "controlCenterHeader", Dock = DockStyle.Top,
            Height = DesignSystem.Scale(this, 80), TabIndex = 0, Text = Text
        };
        TextChanged += (_, _) => header.Text = Text;
        Controls.Add(viewport);
        Controls.Add(header);
        viewport.BringToFront();
        panelFooter.Dock = DockStyle.Bottom;
        panelFooter.TabIndex = 2;
        panelFooter.Padding = new Padding(DesignSystem.Scale(this, 6));
        Padding = new Padding(DesignSystem.Scale(this, 16), 0, DesignSystem.Scale(this, 12), DesignSystem.Scale(this, 8));
        checkStartup.AutoSize = false;
        checkStartup.Dock = DockStyle.Fill;
        panelStartup.Height = DesignSystem.Scale(this, 32);
        AutoSize = false;
        MinimumSize = new Size(DesignSystem.Scale(this, 460), DesignSystem.Scale(this, 400));
        var area = Screen.FromControl(this).WorkingArea;
        ClientSize = new Size(DesignSystem.Scale(this, 520), Math.Min(DesignSystem.Scale(this, 720), area.Height - DesignSystem.Scale(this, 56)));
        ResumeLayout(true);
    }
}
