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
    internal sealed class TrackingCalibrationAccuracyForm : Form
    {
        private readonly TextBox referenceName = new TextBox();
        private readonly TextBox knownDimension = new TextBox();
        private readonly TextBox pixelSpan = new TextBox();
        private readonly TextBox testOne = new TextBox();
        private readonly TextBox testTwo = new TextBox();
        private readonly TextBox testThree = new TextBox();
        private readonly Label result = new Label();

        public string ReferenceName { get { return referenceName.Text.Trim(); } }
        public string KnownDimensionMm { get { return knownDimension.Text.Trim(); } }
        public string PixelSpan { get { return pixelSpan.Text.Trim(); } }
        public string TestOneMm { get { return testOne.Text.Trim(); } }
        public string TestTwoMm { get { return testTwo.Text.Trim(); } }
        public string TestThreeMm { get { return testThree.Text.Trim(); } }
        public string AccuracySummary { get; private set; }

        public TrackingCalibrationAccuracyForm(string savedReferenceName, string savedKnownDimension, string savedPixelSpan, string savedTestOne, string savedTestTwo, string savedTestThree)
        {
            Text = "Tracking Calibration and Accuracy Test";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(650, 610);
            Size = new Size(760, 680);
            BackColor = Color.FromArgb(244, 247, 242);
            Font = new Font("Segoe UI", 9.5F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22, 18, 22, 18);
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowCount = 10;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            for (int i = 0; i < 6; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            Label title = new Label();
            title.Text = "Test measurements against a known dimension";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 2);

            Label intro = new Label();
            intro.Text = "Measure the same reference three times without moving it. Cassette Motion Pro compares the readings with the known size and reports accuracy, repeatability, and confidence. Manual verification remains required.";
            intro.Dock = DockStyle.Fill;
            intro.ForeColor = Color.FromArgb(74, 87, 81);
            layout.Controls.Add(intro, 0, 1);
            layout.SetColumnSpan(intro, 2);

            AddField(layout, 2, "Reference name", referenceName, "Example: calibration bar or wheel diameter");
            AddField(layout, 3, "Known dimension (mm)", knownDimension, "Physical dimension measured with a trusted tool");
            AddField(layout, 4, "Reference span (pixels)", pixelSpan, "Optional: pixel distance used for image scale");
            AddField(layout, 5, "Repeated reading 1 (mm)", testOne, "Measure the same reference");
            AddField(layout, 6, "Repeated reading 2 (mm)", testTwo, "Repeat without changing the reference");
            AddField(layout, 7, "Repeated reading 3 (mm)", testThree, "A third reading improves the check");

            result.Dock = DockStyle.Fill;
            result.BackColor = Color.White;
            result.BorderStyle = BorderStyle.FixedSingle;
            result.Padding = new Padding(14, 12, 14, 10);
            result.ForeColor = Color.FromArgb(52, 63, 58);
            result.Text = "Enter a known dimension and at least two repeated readings, then calculate the result.";
            layout.Controls.Add(result, 0, 8);
            layout.SetColumnSpan(result, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button save = CreateButton("Save Test", true, 120);
            save.Click += Save_Click;
            Button calculate = CreateButton("Calculate", false, 120);
            calculate.Click += delegate { CalculateResult(false); };
            Button cancel = CreateButton("Cancel", false, 100);
            cancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(save);
            actions.Controls.Add(calculate);
            actions.Controls.Add(cancel);
            layout.Controls.Add(actions, 0, 9);
            layout.SetColumnSpan(actions, 2);

            referenceName.Text = savedReferenceName ?? string.Empty;
            knownDimension.Text = savedKnownDimension ?? string.Empty;
            pixelSpan.Text = savedPixelSpan ?? string.Empty;
            testOne.Text = savedTestOne ?? string.Empty;
            testTwo.Text = savedTestTwo ?? string.Empty;
            testThree.Text = savedTestThree ?? string.Empty;

            AcceptButton = save;
            CancelButton = cancel;
            Controls.Add(layout);
            if (!string.IsNullOrWhiteSpace(knownDimension.Text))
                CalculateResult(false);
        }

        private static void AddField(TableLayoutPanel layout, int row, string labelText, TextBox input, string hint)
        {
            Label label = new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(4, 6, 4, 5);
            input.AccessibleDescription = hint;
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(input, 1, row);
        }

        private static Button CreateButton(string text, bool primary, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(width, 38);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = primary ? Color.FromArgb(138, 196, 32) : Color.White;
            button.ForeColor = Color.FromArgb(24, 31, 29);
            button.FlatAppearance.BorderColor = Color.FromArgb(154, 166, 159);
            return button;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (!CalculateResult(true))
                return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool CalculateResult(bool showErrors)
        {
            double known;
            if (!TryNumber(knownDimension.Text, out known) || known <= 0)
                return ShowInputError("Enter a known dimension greater than zero.", showErrors);

            List<double> readings = new List<double>();
            AddReading(readings, testOne.Text);
            AddReading(readings, testTwo.Text);
            AddReading(readings, testThree.Text);
            if (readings.Count < 2)
                return ShowInputError("Enter at least two valid repeated readings.", showErrors);

            double total = 0;
            double minimum = double.MaxValue;
            double maximum = double.MinValue;
            foreach (double reading in readings)
            {
                total += reading;
                minimum = Math.Min(minimum, reading);
                maximum = Math.Max(maximum, reading);
            }

            double average = total / readings.Count;
            double errorMm = average - known;
            double errorPercent = Math.Abs(errorMm) / known * 100.0;
            double spreadMm = maximum - minimum;
            double spreadPercent = spreadMm / known * 100.0;
            double pixels;
            bool hasPixels = TryNumber(pixelSpan.Text, out pixels) && pixels > 0;
            double confidence = 100.0 - Math.Min(55.0, errorPercent * 5.0) - Math.Min(35.0, spreadPercent * 4.0) - (hasPixels ? 0.0 : 5.0);
            confidence = Math.Max(0.0, Math.Min(100.0, confidence));
            string grade = confidence >= 90 ? "High" : confidence >= 75 ? "Moderate" : "Low — recalibrate and repeat";

            StringBuilder summary = new StringBuilder();
            summary.Append("Reference: ").Append(string.IsNullOrWhiteSpace(referenceName.Text) ? "Known reference" : referenceName.Text.Trim());
            summary.Append(" | Known: ").Append(known.ToString("0.00", CultureInfo.InvariantCulture)).Append(" mm");
            if (hasPixels)
                summary.Append(" | Scale: ").Append((pixels / known).ToString("0.000", CultureInfo.InvariantCulture)).Append(" px/mm");
            summary.Append(" | Tests: ").Append(readings.Count);
            summary.Append(" | Average: ").Append(average.ToString("0.00", CultureInfo.InvariantCulture)).Append(" mm");
            summary.Append(" | Accuracy error: ").Append(Math.Abs(errorMm).ToString("0.00", CultureInfo.InvariantCulture)).Append(" mm (").Append(errorPercent.ToString("0.0", CultureInfo.InvariantCulture)).Append("%)");
            summary.Append(" | Repeatability spread: ").Append(spreadMm.ToString("0.00", CultureInfo.InvariantCulture)).Append(" mm (").Append(spreadPercent.ToString("0.0", CultureInfo.InvariantCulture)).Append("%)");
            summary.Append(" | Confidence: ").Append(confidence.ToString("0", CultureInfo.InvariantCulture)).Append("/100 — ").Append(grade);
            summary.Append(". Advisory accuracy check; confirm calibration and landmark placement manually.");
            AccuracySummary = summary.ToString();

            result.Text = "ACCURACY RESULT\r\n\r\n" + AccuracySummary;
            result.ForeColor = confidence >= 90 ? Color.FromArgb(47, 126, 61) : confidence >= 75 ? Color.FromArgb(181, 118, 35) : Color.FromArgb(176, 61, 49);
            return true;
        }

        private bool ShowInputError(string message, bool showErrors)
        {
            AccuracySummary = string.Empty;
            result.Text = message;
            result.ForeColor = Color.FromArgb(176, 61, 49);
            if (showErrors)
                MessageBox.Show(this, message, "Calibration and Accuracy Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private static void AddReading(List<double> readings, string text)
        {
            double value;
            if (TryNumber(text, out value) && value > 0)
                readings.Add(value);
        }

        private static bool TryNumber(string text, out double value)
        {
            string cleaned = (text ?? string.Empty).Trim().Replace("mm", string.Empty).Trim();
            return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.TryParse(cleaned, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }
    }
}
