/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    // Bike sizing from body measurements: target saddle height, hand position (Handlebar X / Y) and the frame
    // stack and reach that fit it. Results update as you type; Save writes the measurements and the estimate
    // to the session.
    internal sealed class BikeSizingForm : Form
    {
        private readonly FitSessionRecord session;
        private readonly string measuredX, measuredY, measuredLabel;

        private readonly TextBox height = new TextBox(), inseam = new TextBox(), sternum = new TextBox(), arm = new TextBox(), shoulders = new TextBox();
        private readonly ComboBox flexibility = new ComboBox(), bikeType = new ComboBox(), style = new ComboBox();
        private readonly TextBox backAngle = new TextBox(), shoulderAngle = new TextBox();
        private readonly TextBox stemLength = new TextBox(), stemAngle = new TextBox(), spacers = new TextBox(), headTube = new TextBox(), barReach = new TextBox();
        private readonly TextBox results = new TextBox();
        private readonly CheckBox showInReport = new CheckBox();
        private readonly HashSet<TextBox> edited = new HashSet<TextBox>();
        private bool filling;
        private string summary = string.Empty;

        public bool Changed { get; private set; }

        // measuredX / measuredY: the bike's current Handlebar X / Y (mm) if measured, for comparison.
        public BikeSizingForm(FitSessionRecord session, string measuredX, string measuredY, string measuredLabel)
        {
            CassetteMotionTheme.Attach(this);
            this.session = session;
            this.measuredX = measuredX;
            this.measuredY = measuredY;
            this.measuredLabel = string.IsNullOrEmpty(measuredLabel) ? "current" : measuredLabel;
            Text = "Bike Sizing";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1060, 760);
            MinimumSize = new Size(900, 640);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(16);
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            Controls.Add(layout);

            Label heading = new Label();
            heading.Text = "Enter the rider's measurements (barefoot, in cm). The estimate gives a starting position and frame size; confirm it on the bike.";
            heading.Dock = DockStyle.Fill;
            heading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            layout.Controls.Add(heading, 0, 0);
            layout.SetColumnSpan(heading, 2);

            TableLayoutPanel inputs = new TableLayoutPanel();
            inputs.Dock = DockStyle.Fill;
            inputs.AutoScroll = true;
            inputs.ColumnCount = 2;
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(inputs, 0, 1);

            Section(inputs, "Rider");
            Field(inputs, "Height (cm)", height, "Standing tall, barefoot, back against a wall.");
            Field(inputs, "Inseam (cm)", inseam, "Floor to crotch: feet 15–20 cm apart, a book pressed up firmly, measure to the top of the book.");
            Field(inputs, "Floor to sternal notch (cm)", sternum, "Floor to the dip at the top of the breastbone, between the collarbones.");
            Field(inputs, "Arm length (cm)", arm, "Shoulder bony point (acromion) to the centre of a closed fist, arm straight out to the side.");
            Field(inputs, "Shoulder width (cm)", shoulders, "Optional. Acromion to acromion (bony points on top of the shoulders). Used for drop-bar width.");
            Choice(inputs, "Flexibility", flexibility, new object[] { "Limited", "Average", "Good" }, "Limited: can't reach past the knees with straight legs. Good: palms to the floor.");

            Section(inputs, "Bike and riding");
            Choice(inputs, "Bike type", bikeType, new object[] { "Road", "Gravel", "Mountain" }, null);
            Choice(inputs, "Riding style", style, new object[] { "Performance", "Endurance", "Comfort" }, "Performance: lower and longer. Comfort: more upright.");

            Section(inputs, "Position targets (defaults follow the choices above)");
            Field(inputs, "Back angle (°)", backAngle, "Hip to shoulder line above horizontal, on the hoods (grips for mountain).");
            Field(inputs, "Body reach angle (°)", shoulderAngle, "Angle at the shoulder between the hip and the wrist / hand contact point (hip – shoulder – wrist), the same angle as Body reach in the rider measurements. Not the elbow-based shoulder angle some fit systems use, which reads smaller.");

            Section(inputs, "Cockpit, for the frame size");
            Field(inputs, "Stem length (mm)", stemLength, null);
            Field(inputs, "Stem angle (°)", stemAngle, "Negative points down, e.g. −6.");
            Field(inputs, "Spacers under stem (mm)", spacers, null);
            Field(inputs, "Head tube angle (°)", headTube, null);
            Field(inputs, "Bar reach (mm)", barReach, "Drop bars: stem clamp to the front of the bar.");

            results.Dock = DockStyle.Fill;
            results.Multiline = true;
            results.ReadOnly = true;
            results.ScrollBars = ScrollBars.Vertical;
            results.Font = new Font("Consolas", 10F);
            results.Margin = new Padding(16, 0, 0, 0);
            layout.Controls.Add(results, 1, 1);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            Button close = new Button();
            close.Text = "Close";
            close.Size = new Size(110, 34);
            close.DialogResult = DialogResult.Cancel;
            Button save = new Button();
            save.Text = "Save to Session";
            save.Size = new Size(150, 34);
            save.Click += Save_Click;
            showInReport.Text = "Include in report";
            showInReport.AutoSize = true;
            showInReport.Margin = new Padding(12, 9, 12, 0);
            buttons.Controls.Add(close);
            buttons.Controls.Add(save);
            buttons.Controls.Add(showInReport);
            layout.Controls.Add(buttons, 0, 2);
            layout.SetColumnSpan(buttons, 2);
            CancelButton = close;
            AcceptButton = save;

            LoadFrom(session);
            bikeType.SelectedIndexChanged += delegate { ApplyDefaults(); Recalculate(); };
            style.SelectedIndexChanged += delegate { ApplyDefaults(); Recalculate(); };
            flexibility.SelectedIndexChanged += delegate { ApplyDefaults(); Recalculate(); };
            foreach (TextBox box in new TextBox[] { backAngle, shoulderAngle, stemLength, stemAngle, spacers, headTube, barReach })
            {
                TextBox current = box;
                current.TextChanged += delegate { if (!filling) edited.Add(current); Recalculate(); };
            }
            foreach (TextBox box in new TextBox[] { height, inseam, sternum, arm, shoulders })
                box.TextChanged += delegate { Recalculate(); };
            Recalculate();
        }

        private static void Section(TableLayoutPanel table, string title)
        {
            Label label = new Label();
            label.Text = title;
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            label.Margin = new Padding(0, 12, 0, 4);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(label, 0, row);
            table.SetColumnSpan(label, 2);
        }

        private static void Field(TableLayoutPanel table, string caption, TextBox box, string help)
        {
            box.Width = 110;
            AddRow(table, caption, box, help);
        }

        private static void Choice(TableLayoutPanel table, string caption, ComboBox box, object[] items, string help)
        {
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Width = 150;
            box.Items.AddRange(items);
            AddRow(table, caption, box, help);
        }

        private static void AddRow(TableLayoutPanel table, string caption, Control control, string help)
        {
            Label label = new Label();
            label.Text = caption;
            label.AutoSize = true;
            label.Margin = new Padding(0, 6, 6, 0);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(control, 1, row);
            if (!string.IsNullOrEmpty(help))
            {
                ToolTip tip = new ToolTip();
                tip.AutoPopDelay = 15000;
                tip.SetToolTip(control, help);
                tip.SetToolTip(label, help);
            }
        }

        private void LoadFrom(FitSessionRecord s)
        {
            filling = true;
            height.Text = s.RiderHeight ?? string.Empty;
            inseam.Text = s.RiderInseam ?? string.Empty;
            sternum.Text = s.RiderSternalNotchHeight ?? string.Empty;
            arm.Text = s.RiderArmLength ?? string.Empty;
            shoulders.Text = s.RiderShoulderWidth ?? string.Empty;
            Choose(flexibility, s.RiderFlexibility, "Average");
            string type = s.SizingBikeType;
            if (string.IsNullOrWhiteSpace(type)) type = string.IsNullOrWhiteSpace(s.FitProtocolBikeType) ? s.FitTemplateBikeType : s.FitProtocolBikeType;
            Choose(bikeType, type, "Road");
            Choose(style, s.SizingRidingStyle, "Endurance");
            ApplyDefaults();
            filling = true;
            Restore(backAngle, s.SizingBackAngle);
            Restore(shoulderAngle, s.SizingShoulderAngle);
            Restore(stemLength, s.SizingStemLength);
            Restore(stemAngle, s.SizingStemAngle);
            Restore(spacers, s.SizingSpacers);
            Restore(headTube, s.SizingHeadTubeAngle);
            Restore(barReach, s.SizingBarReach);
            showInReport.Checked = !s.HideSizingInReport;
            filling = false;
        }

        private void Restore(TextBox box, string saved)
        {
            if (string.IsNullOrWhiteSpace(saved) || saved == box.Text) return;
            box.Text = saved;
            edited.Add(box);
        }

        private static void Choose(ComboBox box, string value, string fallback)
        {
            int index = string.IsNullOrWhiteSpace(value) ? -1 : box.FindStringExact(value.Trim());
            if (index < 0) index = box.FindStringExact(fallback);
            box.SelectedIndex = index < 0 ? 0 : index;
        }

        // Fill each target and cockpit box with the default for the current choices, unless it was typed in.
        private void ApplyDefaults()
        {
            string type = Selected(bikeType), riding = Selected(style), flex = Selected(flexibility);
            filling = true;
            Default(backAngle, BikeSizing.DefaultBackAngle(type, riding, flex));
            Default(shoulderAngle, BikeSizing.DefaultShoulderAngle(type));
            Default(stemLength, BikeSizing.DefaultStemLength(type));
            Default(stemAngle, BikeSizing.DefaultStemAngle(type));
            Default(spacers, BikeSizing.DefaultSpacers(type));
            Default(headTube, BikeSizing.DefaultHeadTubeAngle(type));
            Default(barReach, BikeSizing.DefaultBarReach(type));
            bool dropBar = type != "Mountain";
            barReach.Enabled = dropBar;
            if (!dropBar) { barReach.Text = "0"; edited.Remove(barReach); }
            filling = false;
        }

        private void Default(TextBox box, double value)
        {
            if (edited.Contains(box)) return;
            box.Text = value.ToString("0.#", CultureInfo.CurrentCulture);
        }

        private static string Selected(ComboBox box)
        {
            return box.SelectedItem == null ? string.Empty : box.SelectedItem.ToString();
        }

        private static bool Number(TextBox box, out double value)
        {
            return MeasurementNumber.TryParse(box.Text, out value);
        }

        private static double Optional(TextBox box)
        {
            double value;
            return Number(box, out value) ? value : double.NaN;
        }

        private void Recalculate()
        {
            if (filling) return;
            BikeSizingInput input = new BikeSizingInput();
            double h, i, s, a, w;
            StringBuilder text = new StringBuilder();
            if (!Number(height, out h) || !Number(inseam, out i) || !Number(sternum, out s) || !Number(arm, out a))
            {
                summary = string.Empty;
                text.AppendLine("Enter height, inseam, floor to sternal notch and arm length to see the estimate.");
                text.AppendLine();
                text.AppendLine("Hover over a field for how to measure it.");
                results.Text = text.ToString();
                return;
            }
            input.Height = h * 10; input.Inseam = i * 10; input.SternalNotch = s * 10; input.ArmLength = a * 10;
            input.ShoulderWidth = Number(shoulders, out w) ? w * 10 : 0;
            input.BikeType = Selected(bikeType);
            input.Style = Selected(style);
            input.Flexibility = Selected(flexibility);
            input.BackAngle = Optional(backAngle);
            input.ShoulderAngle = Optional(shoulderAngle);
            input.StemLength = Optional(stemLength);
            input.StemAngle = Optional(stemAngle);
            input.Spacers = Optional(spacers);
            input.HeadTubeAngle = Optional(headTube);
            input.BarReach = Optional(barReach);

            BikeSizingResult r;
            string error;
            if (!BikeSizing.TryCalculate(input, out r, out error))
            {
                summary = string.Empty;
                results.Text = "Check the measurements:" + Environment.NewLine + error;
                return;
            }

            bool dropBar = input.BikeType != "Mountain";
            string hands = dropBar ? "hood contact point (top of the hood, under the palm)" : "grip centre";
            text.AppendLine("BIKE SIZING ESTIMATE — " + input.BikeType + ", " + input.Style.ToLowerInvariant() + ", " + input.Flexibility.ToLowerInvariant() + " flexibility");
            text.AppendLine();
            text.AppendLine("Saddle height      " + Mm(r.SaddleHeight) + "   BB centre to saddle top along the seat tube");
            text.AppendLine();
            text.AppendLine("HANDS — " + hands + ", measured from the BB centre");
            text.AppendLine("Handlebar X        " + Mm(r.HandlebarX) + "   range " + Range(r.HandlebarXMin, r.HandlebarXMax));
            text.AppendLine("Handlebar Y        " + Mm(r.HandlebarY) + "   range " + Range(r.HandlebarYMin, r.HandlebarYMax));
            text.AppendLine("Saddle to hands    " + Mm(r.SaddleToHandDrop) + (r.SaddleToHandDrop >= 0 ? "   drop (hands below saddle)" : "   rise (hands above saddle)"));
            text.AppendLine();
            text.AppendLine("FRAME TO LOOK FOR");
            text.AppendLine("Stack              " + Mm(r.FrameStack) + "   range " + Range(r.FrameStackMin, r.FrameStackMax));
            text.AppendLine("Reach              " + Mm(r.FrameReach) + "   range " + Range(r.FrameReachMin, r.FrameReachMax));
            text.AppendLine("  with a " + r.StemLength.ToString("0") + " mm stem at " + r.StemAngle.ToString("+0;−0;0") + "°, " + r.Spacers.ToString("0") + " mm spacers, " + r.HeadTubeAngle.ToString("0.#") + "° head tube" + (dropBar ? ", " + r.BarReach.ToString("0") + " mm bar reach." : "."));
            text.AppendLine("  Compare with the stack and reach in the frame maker's geometry chart.");
            if (r.BarWidth > 0)
            {
                text.AppendLine();
                text.AppendLine("Bar width          " + r.BarWidth.ToString("0") + " mm   centre to centre, from shoulder width");
            }

            double currentX, currentY;
            bool hasX = MeasurementNumber.TryParse(measuredX, out currentX);
            bool hasY = MeasurementNumber.TryParse(measuredY, out currentY);
            if (hasX || hasY)
            {
                text.AppendLine();
                text.AppendLine("COMPARED WITH THE " + measuredLabel.ToUpperInvariant() + " BIKE MEASUREMENTS");
                if (hasX) text.AppendLine("Handlebar X        " + Mm(currentX) + "   " + Difference(r.HandlebarX - currentX, r.HandlebarXMin - currentX, r.HandlebarXMax - currentX, "further forward", "further back"));
                if (hasY) text.AppendLine("Handlebar Y        " + Mm(currentY) + "   " + Difference(r.HandlebarY - currentY, r.HandlebarYMin - currentY, r.HandlebarYMax - currentY, "higher", "lower"));
            }

            text.AppendLine();
            text.AppendLine("Based on back angle " + r.BackAngle.ToString("0.#") + "° and body reach angle " + r.ShoulderAngle.ToString("0.#") + "° (hip – shoulder – wrist); ranges allow ±" + BikeSizing.BackSpread.ToString("0") + "° back or ±" + BikeSizing.ShoulderSpread.ToString("0") + "° body reach.");
            foreach (string warning in r.Warnings)
                text.AppendLine("Note: " + warning);
            text.AppendLine("This is a starting estimate from body proportions. Confirm on the bike with the rider pedalling.");

            summary = text.ToString().TrimEnd();
            results.Text = summary;
        }

        private static string Mm(double value)
        {
            return (Math.Round(value).ToString("0", CultureInfo.CurrentCulture) + " mm").PadLeft(8);
        }

        private static string Range(double min, double max)
        {
            return Math.Round(min).ToString("0") + "–" + Math.Round(max).ToString("0") + " mm";
        }

        private static string Difference(double change, double low, double high, string more, string less)
        {
            if (low <= 0 && high >= 0)
                return "within the range (estimate is " + Math.Abs(Math.Round(change)).ToString("0") + " mm " + (change >= 0 ? more : less) + ")";
            return "move about " + Math.Abs(Math.Round(change)).ToString("0") + " mm " + (change >= 0 ? more : less) + " to reach the estimate";
        }

        private void Save_Click(object sender, EventArgs e)
        {
            session.RiderHeight = height.Text.Trim();
            session.RiderInseam = inseam.Text.Trim();
            session.RiderSternalNotchHeight = sternum.Text.Trim();
            session.RiderArmLength = arm.Text.Trim();
            session.RiderShoulderWidth = shoulders.Text.Trim();
            session.RiderFlexibility = Selected(flexibility);
            session.SizingBikeType = Selected(bikeType);
            session.SizingRidingStyle = Selected(style);
            // Only targets typed in are kept, so the defaults can improve later.
            session.SizingBackAngle = Kept(backAngle);
            session.SizingShoulderAngle = Kept(shoulderAngle);
            session.SizingStemLength = Kept(stemLength);
            session.SizingStemAngle = Kept(stemAngle);
            session.SizingSpacers = Kept(spacers);
            session.SizingHeadTubeAngle = Kept(headTube);
            session.SizingBarReach = Kept(barReach);
            session.SizingSummary = StripComparison(summary);
            session.HideSizingInReport = !showInReport.Checked;
            Changed = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private string Kept(TextBox box)
        {
            return edited.Contains(box) ? box.Text.Trim() : string.Empty;
        }

        // The report shows the estimate itself; the comparison with today's bike depends on measurements that may still change.
        private static string StripComparison(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            int start = text.IndexOf("COMPARED WITH THE ", StringComparison.Ordinal);
            if (start < 0) return text;
            int end = text.IndexOf(Environment.NewLine + Environment.NewLine, start, StringComparison.Ordinal);
            return end < 0 ? text.Substring(0, start).TrimEnd() : (text.Substring(0, start) + text.Substring(end + Environment.NewLine.Length * 2)).TrimEnd();
        }
    }
}
