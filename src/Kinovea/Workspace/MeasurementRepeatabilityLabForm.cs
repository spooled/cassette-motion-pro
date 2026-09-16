using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class MeasurementRepeatabilityLabForm : Form
    {
        private readonly ComboBox measurement = new ComboBox();
        private readonly ComboBox phase = new ComboBox();
        private readonly TextBox[] readings = { new TextBox(), new TextBox(), new TextBox() };
        private readonly NumericUpDown limit = new NumericUpDown();
        private readonly TextBox history = new TextBox();
        private readonly CheckBox approve = new CheckBox();
        private readonly List<MeasurementRepeatabilityCheck> checks;
        public bool Changed { get; private set; }

        public MeasurementRepeatabilityLabForm(List<MeasurementRepeatabilityCheck> existing)
        {
            checks = existing == null ? new List<MeasurementRepeatabilityCheck>() : new List<MeasurementRepeatabilityCheck>(existing);
            Text = "Measurement Repeatability Lab";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(850, 650);
            MinimumSize = new Size(700, 560);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(18);
            layout.ColumnCount = 1;
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            Label heading = new Label();
            heading.Text = "Repeat the same landmark placement three times on the same view. Compare spread; this does not prove absolute accuracy.";
            heading.Dock = DockStyle.Fill;
            heading.AutoSize = false;
            heading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            layout.Controls.Add(heading, 0, 0);

            FlowLayoutPanel selection = Row();
            selection.Controls.Add(Caption("Measurement"));
            measurement.DropDownStyle = ComboBoxStyle.DropDownList;
            measurement.Width = 240;
            measurement.Items.AddRange(new object[] { "Saddle height", "Saddle setback", "Handlebar reach", "Handlebar drop", "Knee angle", "Hip angle", "Ankle angle", "Body reach", "Back angle" });
            measurement.SelectedIndex = 0;
            measurement.SelectedIndexChanged += delegate { SetDefaultLimit(); };
            selection.Controls.Add(measurement);
            selection.Controls.Add(Caption("View"));
            phase.DropDownStyle = ComboBoxStyle.DropDownList;
            phase.Width = 100;
            phase.Items.AddRange(new object[] { "Before", "After" });
            phase.SelectedIndex = 0;
            selection.Controls.Add(phase);
            layout.Controls.Add(selection, 0, 1);

            FlowLayoutPanel values = Row();
            for (int i = 0; i < readings.Length; i++)
            {
                values.Controls.Add(Caption("Reading " + (i + 1)));
                readings[i].Width = 85;
                values.Controls.Add(readings[i]);
            }
            layout.Controls.Add(values, 0, 2);

            FlowLayoutPanel threshold = Row();
            threshold.Controls.Add(Caption("Maximum acceptable spread"));
            limit.DecimalPlaces = 1;
            limit.Increment = 0.5M;
            limit.Maximum = 1000;
            limit.Width = 85;
            threshold.Controls.Add(limit);
            threshold.Controls.Add(Caption("mm for bike measurements; degrees for angles. Set your own tolerance."));
            layout.Controls.Add(threshold, 0, 3);

            approve.Text = "Fitter reviewed the readings; include this check in the report";
            approve.AutoSize = true;
            layout.Controls.Add(approve, 0, 4);

            history.Dock = DockStyle.Fill;
            history.Multiline = true;
            history.ReadOnly = true;
            history.ScrollBars = ScrollBars.Vertical;
            history.Font = new Font("Consolas", 9F);
            layout.Controls.Add(history, 0, 5);

            FlowLayoutPanel actions = Row();
            Button save = new Button();
            save.Text = "Save Check";
            save.Size = new Size(120, 34);
            save.Click += Save_Click;
            actions.Controls.Add(save);
            Button close = new Button();
            close.Text = "Close";
            close.Size = new Size(100, 34);
            close.Click += delegate { Close(); };
            actions.Controls.Add(close);
            layout.Controls.Add(actions, 0, 6);
            Controls.Add(layout);
            SetDefaultLimit();
            RefreshHistory();
        }

        public List<MeasurementRepeatabilityCheck> Checks { get { return checks; } }

        private void SetDefaultLimit()
        {
            limit.Value = measurement.SelectedIndex >= 4 ? 3M : 5M;
        }

        private static FlowLayoutPanel Row()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.WrapContents = false;
            panel.AutoScroll = true;
            panel.Padding = new Padding(0, 6, 0, 0);
            return panel;
        }

        private static Label Caption(string value)
        {
            Label label = new Label();
            label.Text = value;
            label.AutoSize = true;
            label.Margin = new Padding(8, 8, 6, 0);
            return label;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            double[] parsed = new double[3];
            for (int i = 0; i < 3; i++)
            {
                if (!double.TryParse(readings[i].Text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed[i]) || Double.IsNaN(parsed[i]) || Double.IsInfinity(parsed[i]))
                {
                    MessageBox.Show(this, "Enter three valid numeric readings from repeated placements.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            MeasurementRepeatabilityCheck check = new MeasurementRepeatabilityCheck();
            check.Measurement = measurement.Text;
            check.Phase = phase.Text;
            check.Unit = measurement.SelectedIndex >= 4 ? "degrees" : "mm";
            check.ReadingOne = parsed[0];
            check.ReadingTwo = parsed[1];
            check.ReadingThree = parsed[2];
            check.AllowedRange = (double)limit.Value;
            check.ApprovedForReport = approve.Checked;
            check.CheckedUtc = DateTime.UtcNow;
            checks.RemoveAll(item => item.Measurement == check.Measurement && item.Phase == check.Phase);
            checks.Add(check);
            Changed = true;
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            history.Text = checks.Count == 0 ? "No repeatability checks saved for this session." :
                string.Join(Environment.NewLine + Environment.NewLine, checks.OrderBy(item => item.Phase).ThenBy(item => item.Measurement)
                    .Select(item => item.Summary + (item.ApprovedForReport ? " [FITTER APPROVED]" : " [SESSION ONLY]")).ToArray());
        }
    }
}
