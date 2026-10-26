using GHelper.UI;

namespace GHelper;

public partial class SettingsForm
{
    private void InitializeAppearance()
    {
        SuspendLayout();
        // Keep upstream's compact, content-sized window. There is no extra title block
        // or fixed-height viewport competing with the device controls.
        Padding = new Padding(DesignSystem.Scale(this, 12), DesignSystem.Scale(this, 10),
            DesignSystem.Scale(this, 12), DesignSystem.Scale(this, 4));
        foreach (var card in Controls.OfType<RCardPanel>())
            card.Padding = new Padding(DesignSystem.Scale(this, 12), DesignSystem.Scale(this, 8),
                DesignSystem.Scale(this, 12), DesignSystem.Scale(this, 14));

        foreach (var table in new[] { tablePerf, tableGPU })
        {
            foreach (RowStyle row in table.RowStyles)
                if (row.SizeType == SizeType.Absolute) row.Height = DesignSystem.Scale(this, 50);
            foreach (var button in table.Controls.OfType<RButton>())
            {
                button.Height = DesignSystem.Scale(this, 46);
                button.Margin = new Padding(DesignSystem.Scale(this, 2));
            }
        }
        tableScreen.RowStyles[0].Height = DesignSystem.Scale(this, 34);
        // The second designer row is unused; removing its empty height keeps the
        // segmented display controls compact without removing any hardware action.
        for (int i = 1; i < tableScreen.RowStyles.Count; i++) tableScreen.RowStyles[i].Height = 0;
        var hints = toolTip;
        hints.InitialDelay = 250;
        hints.ReshowDelay = 100;
        hints.AutoPopDelay = 8000;
        UseHoverHint(hints, labelTipGPU, buttonEco, buttonStandard, buttonUltimate, buttonOptimized, buttonXGM);
        UseHoverHint(hints, labelTipScreen, buttonScreenAuto, button60Hz, button120Hz, buttonFHD);
        void SizeBacklightNotice() => labelBacklight.Height = string.IsNullOrEmpty(labelBacklight.Text) ? 0 : DesignSystem.Scale(this, 18);
        labelBacklight.TextChanged += (_, _) => SizeBacklightNotice();
        SizeBacklightNotice();

        // A fixed table row replaces the old absolute/anchored battery positions.
        // This keeps the 100% action aligned with the slider at every window width.
        var chargeRow = new TableLayoutPanel
        {
            Name = "batteryChargeRow", Dock = DockStyle.Top, Height = DesignSystem.Scale(this, 24),
            ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty,
            TabIndex = 1
        };
        chargeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        chargeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesignSystem.Scale(this, 44)));
        chargeRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sliderBattery.Dock = DockStyle.Fill;
        sliderBattery.Margin = Padding.Empty;
        buttonBatteryFull.Dock = DockStyle.Fill;
        buttonBatteryFull.Margin = new Padding(DesignSystem.Scale(this, 4), DesignSystem.Scale(this, 2), 0, DesignSystem.Scale(this, 2));
        chargeRow.Controls.Add(sliderBattery, 0, 0);
        chargeRow.Controls.Add(buttonBatteryFull, 1, 0);
        panelBattery.Controls.Add(chargeRow);
        chargeRow.BringToFront();
        panelBatteryTitle.TabIndex = 0;
        panelBatteryTitle.Height = DesignSystem.Scale(this, 22);
        panelStartup.TabIndex = 2;
        panelStartup.Padding = Padding.Empty;
        panelStartup.Height = DesignSystem.Scale(this, 28);
        checkStartup.AutoSize = false;
        checkStartup.Dock = DockStyle.Left;
        checkStartup.Width = DesignSystem.Scale(this, 178);
        labelCharge.Width = DesignSystem.Scale(this, 145);
        panelBattery.Controls.Add(panelStartup);
        panelStartup.BringToFront();

        panelFooter.Dock = DockStyle.Top;
        panelFooter.Padding = new Padding(DesignSystem.Scale(this, 2), 0, DesignSystem.Scale(this, 2), DesignSystem.Scale(this, 6));
        panelVersion.Padding = new Padding(DesignSystem.Scale(this, 8), 0, DesignSystem.Scale(this, 8), 0);
        MinimumSize = Size.Empty;
        ClientSize = new Size(DesignSystem.Scale(this, 400), 1);
        MinimumSize = new Size(Width, 0);
        // Scrolling is only a fallback for unusually short screens or extra hardware.
        // The common G16 layout fits fully, including startup, charge and footer.
        MaximumSize = new Size(Width, Screen.FromControl(this).WorkingArea.Height - DesignSystem.Scale(this, 16));
        AutoScroll = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ResumeLayout(true);
    }
    private static void UseHoverHint(ToolTip hints, Label label, params Control[] targets)
    {
        label.Visible = false;
        // Existing translated hover handlers still supply the text. Present it as
        // a tooltip instead of permanently reserving an empty line in every card.
        label.TextChanged += (_, _) =>
        {
            foreach (var target in targets) hints.SetToolTip(target, label.Text);
        };
    }
}
