using GHelper.UI;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms.DataVisualization.Charting;

namespace GHelper;

internal static class UiVerificationEntry
{
    [DllImport("user32.dll")] static extern int GetGuiResources(nint process, int flags);

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            var results = new Dictionary<string, object>();
            var timer = Stopwatch.StartNew();
            using var main = new SettingsForm();
            main.CreateControl();
            results["mainConstructMs"] = timer.Elapsed.TotalMilliseconds;
            main.Text = "Personal device controls";
            ConfigureMain(main);
            Capture(main, args[0], "main-" + args[1]);
            main.Show(); main.PerformLayout(); Application.DoEvents();
            results["verticalScrollVisible"] = main.VerticalScroll.Visible;
            results["horizontalScrollVisible"] = main.HorizontalScroll.Visible;
            if (Descendants(main).Any(c => c.Name == "batteryChargeRow"))
            {
                Require(!main.VerticalScroll.Visible && !main.HorizontalScroll.Visible, "G16 fixture fits without scrollbars");
                foreach (var name in new[] { "checkStartup", "labelCharge", "buttonBatteryFull", "buttonQuit", "tableVisual" })
                {
                    var control = Find(main, name);
                    Require(control.Visible && main.RectangleToScreen(main.ClientRectangle).Contains(control.RectangleToScreen(control.ClientRectangle)), "Visible G16 control: " + name);
                }
                var sliderBounds = Find(main, "sliderBattery").Bounds;
                var fullBounds = Find(main, "buttonBatteryFull").Bounds;
                Require(sliderBounds.Right <= fullBounds.Left, "Battery slider/action do not overlap");
                var hintLabel = (Label)Find(main, "labelTipGPU");
                hintLabel.Text = "Translated GPU mode explanation";
                var container = (System.ComponentModel.IContainer)typeof(SettingsForm).GetField("components", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
                var tooltip = container.Components.OfType<ToolTip>().Single();
                Require(!hintLabel.Visible && tooltip.GetToolTip(Find(main, "buttonEco")) == hintLabel.Text, "GPU hover hints retained as tooltips");
                hintLabel.Text = "";
                var backlightNotice = Find(main, "labelBacklight");
                backlightNotice.Text = "Dynamic lighting notice";
                Require(backlightNotice.Height > 0, "Nonempty backlight notice retained");
                backlightNotice.Text = "";
                Require(backlightNotice.Height == 0, "Empty backlight notice collapses");
            }
            main.Hide();
            results["dpi"] = main.DeviceDpi;
            results["mainControls"] = Descendants(main).Count();
            results["mainControlNames"] = Descendants(main).Select(c => c.Name).Order().ToArray();
            if (args.Contains("--screens-only"))
            {
                results["mainSize"] = new { main.Width, main.Height };
                File.WriteAllText(Path.Combine(args[0], "layout-" + args[1] + ".json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }
            var warmConstruction = new List<double>();
            for (int i = 0; i < 5; i++)
            {
                timer.Restart();
                using var warm = new SettingsForm();
                warm.CreateControl();
                warmConstruction.Add(timer.Elapsed.TotalMilliseconds);
            }
            results["warmMainConstructMs"] = warmConstruction;
            results["mainSize"] = new { main.Width, main.Height };
            foreach (var type in new[] { typeof(Fans), typeof(Extra), typeof(Updates), typeof(Handheld), typeof(Matrix), typeof(Slash), typeof(OverlayConfig) })
            {
                using var form = (RForm)Activator.CreateInstance(type)!;
                if (form is Fans) ConfigureFans(form);
                form.MaximumSize = Screen.PrimaryScreen!.WorkingArea.Size;
                form.AutoScroll = true;
                Capture(form, args[0], type.Name.ToLowerInvariant() + "-" + args[1]);
            }
            if (Descendants(main).FirstOrDefault(c => c.Name == "controlCenterContent") is Panel viewport)
            {
                main.Size = main.MinimumSize;
                main.Show(); main.PerformLayout(); Application.DoEvents();
                var startup = Find(main, "checkStartup");
                viewport.ScrollControlIntoView(startup);
                Require(viewport.RectangleToScreen(viewport.ClientRectangle).Contains(startup.RectangleToScreen(startup.ClientRectangle)), "Compact window scroll access");
                Require(Find(main, "panelFooter").Visible, "Compact footer visibility");
                Capture(main, args[0], "main-compact-" + args[1]);
            }
            using var host = new RForm { Size = new Size(300, 180) };
            host.InitTheme();
            using var button = new ProbeButton { Text = "Balanced", Size = new Size(120, 60), BorderColor = Color.DodgerBlue, Activated = true };
            host.Controls.Add(button);
            host.Show();
            Application.DoEvents();
            using var bitmap = new Bitmap(120, 60);
            using var graphics = Graphics.FromImage(bitmap);
            using var paint = new PaintEventArgs(graphics, button.ClientRectangle);
            for (int i = 0; i < 200; i++) button.PaintOnce(paint);
            var samples = new List<object>();
            for (int sample = 0; sample < 5; sample++)
            {
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                int gdi = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                timer.Restart();
                for (int i = 0; i < 5000; i++) button.PaintOnce(paint);
                samples.Add(new { milliseconds = timer.Elapsed.TotalMilliseconds, allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated, gdiDelta = GetGuiResources(Process.GetCurrentProcess().Handle, 0) - gdi });
            }
            results["button5000Paints"] = samples;
            int clicks = 0; button.Click += (_, _) => clicks++; button.PerformClick();
            Require(clicks == 1, "Button click dispatch");
            button.Enabled = false; button.PerformClick(); Require(clicks == 1, "Disabled button dispatch");
            button.Enabled = true;
            if (typeof(RButton).GetProperty("Image")!.DeclaringType == typeof(RButton))
            {
                using var sourceIcon = new Bitmap(16, 16);
                button.Image = sourceIcon;
                var selectedIcon = ((Button)button).Image;
                Require(!ReferenceEquals(selectedIcon, sourceIcon), "Selected icon tinted");
                Require(button.BackColor != RForm.buttonMain, "Selected button filled");
                button.Activated = false;
                Require(ReferenceEquals(((Button)button).Image, sourceIcon), "Inactive source icon restored");
                button.Activated = true;
                Require(ReferenceEquals(((Button)button).Image, selectedIcon), "Selected icon cache reused");
                button.Enabled = false;
                Require(ReferenceEquals(((Button)button).Image, sourceIcon), "Disabled source icon restored");
                button.Image = null; button.Enabled = true;
            }
            using var slider = new SliderProbe { Min = 0, Max = 100, Step = 5, Value = 50 };
            int changes = 0; slider.ValueChanged += (_, _) => changes++;
            slider.Key(Keys.Right); Require(slider.Value == 55 && changes == 1, "Slider keyboard step");
            slider.Value = 100; slider.Key(Keys.Right); Require(slider.Value == 100, "Slider maximum");
            slider.supportedValues = new() { 60, 80, 100 }; slider.Value = 80; slider.Key(Keys.Left); Require(slider.Value == 60, "Supported slider values");
            var toggleType = typeof(RForm).Assembly.GetType("GHelper.UI.RToggle");
            if (toggleType != null)
            {
                using var toggle = (CheckBox)Activator.CreateInstance(toggleType, true)!;
                int events = 0; toggle.CheckedChanged += (_, _) => events++;
                toggle.Checked = true;
                Require(toggle.Checked && events == 1, "Toggle checked event");
                Require(toggle.AccessibilityObject.State.HasFlag(AccessibleStates.Checked), "Toggle accessibility state");
            }
            var process = Process.GetCurrentProcess();
            process.Refresh();
            results["privateBytesAfterScreens"] = process.PrivateMemorySize64;
            var cpu = process.TotalProcessorTime;
            timer.Restart();
            while (timer.ElapsedMilliseconds < 2000) { Application.DoEvents(); Thread.Sleep(10); }
            process.Refresh();
            results["idleCpuMsOver2Seconds"] = (process.TotalProcessorTime - cpu).TotalMilliseconds;
            results["checks"] = "passed: native click/disabled dispatch, slider arrows/bounds/discrete values, toggle events/accessibility when available";
            File.WriteAllText(Path.Combine(args[0], "metrics-" + args[1] + ".json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            host.Hide();
            return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(args[0], "failure.txt"), ex.ToString()); return 1; }
    }

    static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control control in parent.Controls) { yield return control; foreach (var child in Descendants(control)) yield return child; }
    }
    static Control Find(Control parent, string name) => Descendants(parent).First(c => c.Name == name);
    static void ConfigureMain(Form form)
    {
        foreach (var name in new[] { "panelMatrix", "panelRearLight", "panelAlly", "panelPeripherals" }) Find(form, name).Visible = false;
        Find(form, "labelCPUFan").Text = "45°C  ·  1,800 RPM";
        Find(form, "labelGPUFan").Text = "39°C  ·  0 RPM";
        foreach (var name in new[] { "buttonEnergySaver", "buttonArmoury", "buttonAmdOled", "tableAMD", "labelTipGPU" }) Find(form, name).Visible = false;
        ((RButton)Find(form, "buttonBalanced")).Activated = true;
        ((RButton)Find(form, "buttonBalanced")).BorderColor = RForm.colorStandard;
        ((RButton)Find(form, "buttonStandard")).Activated = true;
        ((RButton)Find(form, "buttonStandard")).BorderColor = RForm.colorStandard;
        ((RButton)Find(form, "buttonScreenAuto")).Activated = true;
        ((RButton)Find(form, "buttonScreenAuto")).BorderColor = RForm.colorStandard;
        ((Slider)Find(form, "sliderBattery")).Value = 80;
        Find(form, "labelBatteryTitle").Text = "Battery Charge Limit: 85%";
        Find(form, "labelCharge").Text = "Charge: 85.0%";
        Find(form, "labelVersion").Text = "Version: 0.290.0";
        Find(form, "button120Hz").Text = "240Hz + OD";
        Find(form, "buttonMiniled").Visible = false;
        Find(form, "panelGamma").Visible = true;
        Find(form, "tableVisual").Visible = true;
        Find(form, "labelGammaTitle").Text = "Visual Mode";
        foreach (var item in new[] { ("comboVisual", "Default"), ("comboColorTemp", "Neutral"), ("comboGamut", "Gamut: Native") })
        {
            var combo = (ComboBox)Find(form, item.Item1); combo.Visible = true; combo.Items.Add(item.Item2); combo.SelectedIndex = 0;
        }
        var saver = (RButton)Find(form, "buttonEnergySaver"); saver.Visible = true; saver.BackColor = RForm.colorEco; saver.ForeColor = Color.White;
        Find(form, "labelBattery").Text = "";
        var keyboard = (ComboBox)Find(form, "comboKeyboard"); keyboard.Items.Add("Static"); keyboard.SelectedIndex = 0;
    }
    static void ConfigureFans(Form form)
    {
        foreach (var chart in Descendants(form).OfType<Chart>())
        {
            var series = chart.Series.Add("Verification fixture"); series.ChartType = SeriesChartType.Line; series.Color = RForm.colorStandard; series.BorderWidth = 2;
            foreach (var point in new[] { (30, 0), (40, 10), (50, 25), (60, 40), (70, 65), (80, 80), (90, 100) }) series.Points.AddXY(point.Item1, point.Item2);
        }
        foreach (var name in new[] { "panelGPU", "panelAdvanced" }) Find(form, name).Visible = false;
    }
    static void Capture(Form form, string output, string name)
    {
        form.StartPosition = FormStartPosition.Manual; form.Location = new Point(30, 30);
        form.Show(); form.PerformLayout();
        if (form is SettingsForm) typeof(SettingsForm).GetMethod("AlignFnLock", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, null);
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(output, name + ".png"));
        form.Hide();
    }
    static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    sealed class ProbeButton : RButton { internal void PaintOnce(PaintEventArgs e) => base.OnPaint(e); }
    sealed class SliderProbe : Slider { internal void Key(Keys key) => base.OnKeyDown(new KeyEventArgs(key)); }
}
