/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class DualCameraSynchronizationForm : Form
    {
        private readonly TextBox leftPath = new TextBox();
        private readonly TextBox rightPath = new TextBox();
        private readonly TextBox leftRole = new TextBox();
        private readonly TextBox rightRole = new TextBox();
        private readonly TextBox eventOneLeft = new TextBox();
        private readonly TextBox eventOneRight = new TextBox();
        private readonly TextBox eventTwoLeft = new TextBox();
        private readonly TextBox eventTwoRight = new TextBox();
        private readonly Label result = new Label();

        public string LeftVideoPath { get { return leftPath.Text.Trim(); } }
        public string RightVideoPath { get { return rightPath.Text.Trim(); } }
        public string LeftCameraRole { get { return leftRole.Text.Trim(); } }
        public string RightCameraRole { get { return rightRole.Text.Trim(); } }
        public string EventOneLeftMs { get { return eventOneLeft.Text.Trim(); } }
        public string EventOneRightMs { get { return eventOneRight.Text.Trim(); } }
        public string EventTwoLeftMs { get { return eventTwoLeft.Text.Trim(); } }
        public string EventTwoRightMs { get { return eventTwoRight.Text.Trim(); } }
        public string SynchronizationSummary { get; private set; }
        public bool OpenComparisonRequested { get; private set; }

        public DualCameraSynchronizationForm(string folder, string savedLeftPath, string savedRightPath, string savedLeftRole, string savedRightRole, string savedEventOneLeft, string savedEventOneRight, string savedEventTwoLeft, string savedEventTwoRight)
        {
            Text = "Dual-Camera Synchronization and Comparison";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(850, 730);
            MinimumSize = new Size(720, 650);
            BackColor = Color.FromArgb(244, 247, 242);
            Font = new Font("Segoe UI", 9.5F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22, 18, 22, 18);
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            layout.RowCount = 12;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            for (int i = 0; i < 8; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            Label title = new Label();
            title.Text = "Align two camera views using matching events";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 3);

            Label intro = new Label();
            intro.Text = "Choose the two clips from the same recording. Enter the timestamp in milliseconds where the same visible event occurs in each clip. A second event checks synchronization drift.";
            intro.Dock = DockStyle.Fill;
            intro.ForeColor = Color.FromArgb(74, 87, 81);
            layout.Controls.Add(intro, 0, 1);
            layout.SetColumnSpan(intro, 3);

            AddPathRow(layout, 2, "Camera 1 video", leftPath, folder);
            AddPathRow(layout, 3, "Camera 2 video", rightPath, folder);
            AddTextRow(layout, 4, "Camera 1 role", leftRole, "Example: Drive side");
            AddTextRow(layout, 5, "Camera 2 role", rightRole, "Example: Front");
            AddTextRow(layout, 6, "Event 1 — Camera 1 (ms)", eventOneLeft, "Example: 1250");
            AddTextRow(layout, 7, "Event 1 — Camera 2 (ms)", eventOneRight, "Example: 1316");
            AddTextRow(layout, 8, "Event 2 — Camera 1 (ms)", eventTwoLeft, "Optional drift check");
            AddTextRow(layout, 9, "Event 2 — Camera 2 (ms)", eventTwoRight, "Optional drift check");

            result.Dock = DockStyle.Fill;
            result.BackColor = Color.White;
            result.BorderStyle = BorderStyle.FixedSingle;
            result.Padding = new Padding(14, 12, 14, 10);
            result.Text = "Choose two videos and enter both Event 1 timestamps to calculate their synchronization offset.";
            result.ForeColor = Color.FromArgb(74, 87, 81);
            layout.Controls.Add(result, 0, 10);
            layout.SetColumnSpan(result, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button cancel = CreateButton("Cancel", false, 100);
            cancel.DialogResult = DialogResult.Cancel;
            Button save = CreateButton("Save Sync", false, 120);
            save.Click += delegate { Complete(false); };
            Button open = CreateButton("Save + Open Pair", true, 155);
            open.Click += delegate { Complete(true); };
            Button calculate = CreateButton("Calculate", false, 115);
            calculate.Click += delegate { Calculate(false); };
            actions.Controls.Add(cancel);
            actions.Controls.Add(open);
            actions.Controls.Add(save);
            actions.Controls.Add(calculate);
            layout.Controls.Add(actions, 0, 11);
            layout.SetColumnSpan(actions, 3);

            leftPath.Text = savedLeftPath ?? string.Empty;
            rightPath.Text = savedRightPath ?? string.Empty;
            leftRole.Text = string.IsNullOrWhiteSpace(savedLeftRole) ? "Side view" : savedLeftRole;
            rightRole.Text = string.IsNullOrWhiteSpace(savedRightRole) ? "Front view" : savedRightRole;
            eventOneLeft.Text = savedEventOneLeft ?? string.Empty;
            eventOneRight.Text = savedEventOneRight ?? string.Empty;
            eventTwoLeft.Text = savedEventTwoLeft ?? string.Empty;
            eventTwoRight.Text = savedEventTwoRight ?? string.Empty;
            Controls.Add(layout);
            CancelButton = cancel;
        }

        private static void AddPathRow(TableLayoutPanel layout, int row, string labelText, TextBox input, string folder)
        {
            AddTextRow(layout, row, labelText, input, string.Empty);
            Button browse = CreateButton("Browse…", false, 86);
            browse.Margin = new Padding(4, 5, 4, 4);
            browse.Click += delegate
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = "Choose " + labelText;
                    dialog.Filter = "Video files|*.mp4;*.mov;*.avi;*.mkv;*.m4v;*.mpg;*.mpeg;*.wmv|All files|*.*";
                    if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                        dialog.InitialDirectory = folder;
                    if (dialog.ShowDialog() == DialogResult.OK)
                        input.Text = dialog.FileName;
                }
            };
            layout.Controls.Add(browse, 2, row);
        }

        private static void AddTextRow(TableLayoutPanel layout, int row, string labelText, TextBox input, string cue)
        {
            Label label = new Label();
            label.Text = labelText;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(4, 6, 4, 5);
            input.AccessibleDescription = cue;
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(input, 1, row);
        }

        private void Complete(bool openPair)
        {
            if (!File.Exists(LeftVideoPath) || !File.Exists(RightVideoPath))
            {
                MessageBox.Show(this, "Choose two existing video files first.", "Dual-Camera Synchronization", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!Calculate(true))
                return;
            OpenComparisonRequested = openPair;
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool Calculate(bool showMessage)
        {
            double leftOne;
            double rightOne;
            if (!TryNumber(eventOneLeft.Text, out leftOne) || !TryNumber(eventOneRight.Text, out rightOne))
            {
                result.Text = "Enter valid Event 1 timestamps for both cameras.";
                result.ForeColor = Color.FromArgb(176, 61, 49);
                if (showMessage)
                    MessageBox.Show(this, result.Text, "Dual-Camera Synchronization", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            double offset = rightOne - leftOne;
            double leftTwo;
            double rightTwo;
            bool hasDriftCheck = TryNumber(eventTwoLeft.Text, out leftTwo) && TryNumber(eventTwoRight.Text, out rightTwo);
            double drift = hasDriftCheck ? (rightTwo - leftTwo) - offset : 0;
            double confidence = hasDriftCheck ? Math.Max(0, 100 - Math.Abs(drift) * 0.5) : 75;
            string grade = confidence >= 90 ? "High" : confidence >= 75 ? "Moderate" : "Low — recheck matching events or camera frame rates";

            StringBuilder summary = new StringBuilder();
            summary.Append("Camera 1: ").Append(string.IsNullOrWhiteSpace(LeftCameraRole) ? "View 1" : LeftCameraRole);
            summary.Append(" | Camera 2: ").Append(string.IsNullOrWhiteSpace(RightCameraRole) ? "View 2" : RightCameraRole);
            summary.Append(" | Camera 2 offset: ").Append(offset >= 0 ? "+" : string.Empty).Append(offset.ToString("0.0", CultureInfo.InvariantCulture)).Append(" ms relative to Camera 1");
            if (hasDriftCheck)
                summary.Append(" | Drift at Event 2: ").Append(drift.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)).Append(" ms");
            else
                summary.Append(" | Drift: not tested");
            summary.Append(" | Sync confidence: ").Append(confidence.ToString("0", CultureInfo.InvariantCulture)).Append("/100 — ").Append(grade);
            summary.Append(". Use the offset to align playback timelines; visually confirm the matching event before measuring.");
            SynchronizationSummary = summary.ToString();
            result.Text = "SYNCHRONIZATION RESULT\r\n\r\n" + SynchronizationSummary;
            result.ForeColor = confidence >= 90 ? Color.FromArgb(47, 126, 61) : confidence >= 75 ? Color.FromArgb(181, 118, 35) : Color.FromArgb(176, 61, 49);
            return true;
        }

        private static bool TryNumber(string text, out double value)
        {
            string cleaned = (text ?? string.Empty).Trim().Replace("ms", string.Empty).Trim();
            return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.TryParse(cleaned, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
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
    }
}
