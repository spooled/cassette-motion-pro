/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */
using System;
using System.Drawing;
using System.Windows.Forms;
using CassetteMotionPro;

namespace CassetteMotionPro.Workspace
{
    public class FitFollowUpForm : Form
    {
        private readonly DateTimePicker checkInDate = new DateTimePicker();
        private readonly ComboBox status = new ComboBox();
        private readonly NumericUpDown comfort = new NumericUpDown();
        private readonly NumericUpDown rides = new NumericUpDown();
        private readonly TextBox feedback = new TextBox();
        private readonly TextBox symptoms = new TextBox();
        private readonly TextBox actions = new TextBox();
        private readonly CheckBox scheduleNext = new CheckBox();
        private readonly DateTimePicker nextCheckIn = new DateTimePicker();

        public FitFollowUpEntry Entry { get; private set; }

        public FitFollowUpForm(string fitName)
        {
            Text = "Client Follow-up — " + fitName;
            CassetteMotionTheme.ApplyForm(this);
            ClientSize = new Size(720, 620);
            MinimumSize = new Size(640, 560);
            StartPosition = FormStartPosition.CenterParent;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24, 20, 24, 16);
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label intro = new Label();
            intro.Text = "Record how the rider is adapting after this fit. This creates a dated entry in the client’s fit history.";
            intro.Dock = DockStyle.Fill;
            intro.ForeColor = CassetteMotionTheme.Muted;
            AddRow(layout, "", intro, 58);

            checkInDate.Format = DateTimePickerFormat.Long;
            checkInDate.Value = DateTime.Today;
            checkInDate.Dock = DockStyle.Fill;
            AddRow(layout, "Check-in date", checkInDate, 38);

            status.DropDownStyle = ComboBoxStyle.DropDownList;
            status.Items.AddRange(new object[] { "Adapting well", "Monitor", "Needs adjustment", "Resolved" });
            status.SelectedIndex = 0;
            status.Dock = DockStyle.Fill;
            AddRow(layout, "Adaptation", status, 38);

            comfort.Minimum = 1;
            comfort.Maximum = 10;
            comfort.Value = 8;
            comfort.Dock = DockStyle.Left;
            comfort.Width = 90;
            AddRow(layout, "Comfort (1–10)", comfort, 38);

            rides.Minimum = 0;
            rides.Maximum = 999;
            rides.Dock = DockStyle.Left;
            rides.Width = 90;
            AddRow(layout, "Rides completed", rides, 38);

            ConfigureNotes(feedback);
            AddRow(layout, "Rider feedback", feedback, 96);
            ConfigureNotes(symptoms);
            AddRow(layout, "Symptoms / concerns", symptoms, 82);
            ConfigureNotes(actions);
            AddRow(layout, "Actions / advice", actions, 82);

            FlowLayoutPanel next = new FlowLayoutPanel();
            next.Dock = DockStyle.Fill;
            next.WrapContents = false;
            scheduleNext.Text = "Schedule another check-in";
            scheduleNext.Width = 190;
            scheduleNext.CheckedChanged += delegate { nextCheckIn.Enabled = scheduleNext.Checked; };
            nextCheckIn.Format = DateTimePickerFormat.Short;
            nextCheckIn.Value = DateTime.Today.AddDays(14);
            nextCheckIn.Enabled = false;
            next.Controls.Add(scheduleNext);
            next.Controls.Add(nextCheckIn);
            AddRow(layout, "Next step", next, 44);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            Button save = new Button();
            save.Text = "Save Follow-up";
            save.Size = new Size(145, 36);
            CassetteMotionTheme.StyleButton(save, true);
            save.Click += Save_Click;
            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.Size = new Size(95, 36);
            CassetteMotionTheme.StyleButton(cancel, false);
            cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(save);
            buttons.Controls.Add(cancel);
            AddRow(layout, "", buttons, 52);

            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private static void ConfigureNotes(TextBox box)
        {
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.Dock = DockStyle.Fill;
        }

        private static void AddRow(TableLayoutPanel table, string labelText, Control control, int height)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            Label label = new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            table.Controls.Add(label, 0, row);
            table.Controls.Add(control, 1, row);
        }

        private void Save_Click(object sender, EventArgs e)
        {
            Entry = new FitFollowUpEntry
            {
                Id = Guid.NewGuid(),
                CheckInDate = checkInDate.Value.Date,
                AdaptationStatus = Convert.ToString(status.SelectedItem),
                ComfortScore = Convert.ToInt32(comfort.Value),
                RidesCompleted = Convert.ToInt32(rides.Value),
                RiderFeedback = feedback.Text.Trim(),
                Symptoms = symptoms.Text.Trim(),
                FitterActions = actions.Text.Trim(),
                HasNextCheckIn = scheduleNext.Checked,
                NextCheckInDate = scheduleNext.Checked ? nextCheckIn.Value.Date : DateTime.MinValue,
                CreatedUtc = DateTime.UtcNow
            };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
