/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class FitDayFieldTestForm : Form
    {
        private readonly string saveFolder;
        private readonly ListView checklist = new ListView();
        private readonly TextBox notes = new TextBox();
        private readonly Label status = new Label();

        public string FieldTestSummary { get; private set; }
        public bool HasBlockingFriction { get; private set; }
        public bool IsComplete { get; private set; }

        public FitDayFieldTestForm(string existingSummary, string saveFolder)
        {
            this.saveFolder = saveFolder;
            Text = "Real Fit-Day Field Test";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 650);
            Size = new Size(1040, 760);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22);
            layout.ColumnCount = 1;
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

            Label eyebrow = new Label();
            eyebrow.Text = "v1.6.0 · REAL-WORLD RELIABILITY PASS";
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);

            Label title = new Label();
            title.Text = "Test one complete fitting and record every friction point";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);

            status.Dock = DockStyle.Fill;
            status.Padding = new Padding(12, 9, 12, 6);
            status.BackColor = Color.FromArgb(248, 252, 238);
            status.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            checklist.Dock = DockStyle.Fill;
            checklist.View = View.Details;
            checklist.FullRowSelect = true;
            checklist.GridLines = true;
            checklist.HideSelection = false;
            checklist.Columns.Add("Result", 110);
            checklist.Columns.Add("Fit-day stage", 235);
            checklist.Columns.Add("What to confirm", 625);
            AddCheck("Client + session", "Open the correct client, create or resume the session, and see the active session context.");
            AddCheck("Dual capture", "Open both cameras, confirm framing, and save Before recordings into the correct folders.");
            AddCheck("Playback + favorites", "Open the latest Before/After clips and save useful favorite frames without browsing for folders.");
            AddCheck("Measurements", "Use saved client images, correct tracking, approve measurements, and keep values after reopening.");
            AddCheck("Fit changes", "Record changes and recommendations without losing earlier notes or approval warnings.");
            AddCheck("Report images", "Select Before, After, and side-by-side images and confirm the visible thumbnails.");
            AddCheck("PDF + branding", "Preview the client report and verify studio details, layout, images, and page breaks.");
            AddCheck("Delivery", "Approve and create the intended package, ZIP, or portal package in the client session.");
            AddCheck("Autosave + recovery", "Close and reopen safely; confirm session data, timeline, and media paths are restored.");
            RestoreExistingResults(existingSummary);
            checklist.SelectedIndexChanged += delegate { RefreshStatus(); };

            FlowLayoutPanel resultActions = new FlowLayoutPanel();
            resultActions.Dock = DockStyle.Fill;
            resultActions.FlowDirection = FlowDirection.LeftToRight;
            resultActions.Controls.Add(CreateButton("Mark Pass", true, delegate { SetSelectedResult("PASS"); }));
            resultActions.Controls.Add(CreateButton("Log Friction", false, delegate { SetSelectedResult("FRICTION"); }));
            resultActions.Controls.Add(CreateButton("Not Tested", false, delegate { SetSelectedResult("NOT TESTED"); }));

            notes.Dock = DockStyle.Fill;
            notes.Multiline = true;
            notes.ScrollBars = ScrollBars.Vertical;
            GroupBox observations = new GroupBox();
            observations.Text = "Fitter observations — what you clicked, what you expected, and what happened";
            observations.Dock = DockStyle.Fill;
            observations.Padding = new Padding(10, 8, 10, 10);
            observations.Controls.Add(notes);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.Controls.Add(CreateButton("Save Field Test", true, SaveFieldTest));
            actions.Controls.Add(CreateButton("Cancel", false, delegate { Close(); }));

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(status, 0, 2);
            layout.Controls.Add(checklist, 0, 3);
            layout.Controls.Add(resultActions, 0, 4);
            layout.Controls.Add(observations, 0, 5);
            layout.Controls.Add(actions, 0, 6);
            Controls.Add(layout);
            RefreshStatus();
        }

        private void AddCheck(string stage, string detail)
        {
            ListViewItem item = new ListViewItem("NOT TESTED");
            item.SubItems.Add(stage);
            item.SubItems.Add(detail);
            item.Tag = "NOT TESTED";
            item.ForeColor = Color.FromArgb(116, 126, 121);
            checklist.Items.Add(item);
        }

        private void RestoreExistingResults(string existingSummary)
        {
            if (string.IsNullOrWhiteSpace(existingSummary))
                return;

            string[] lines = existingSummary.Replace("\r\n", "\n").Split('\n');
            bool readingNotes = false;
            StringBuilder restoredNotes = new StringBuilder();
            foreach (string line in lines)
            {
                if (readingNotes)
                {
                    if (!string.IsNullOrWhiteSpace(line) && line != "No additional observations entered.")
                        restoredNotes.AppendLine(line);
                    continue;
                }
                if (line == "Fitter observations:")
                {
                    readingNotes = true;
                    continue;
                }

                foreach (ListViewItem item in checklist.Items)
                {
                    string marker = " | " + item.SubItems[1].Text + " | ";
                    if (line.IndexOf(marker, StringComparison.Ordinal) < 0)
                        continue;
                    string result = line.Substring(0, line.IndexOf('|')).Trim();
                    if (result != "PASS" && result != "FRICTION" && result != "NOT TESTED")
                        continue;
                    item.Text = result;
                    item.Tag = result;
                    item.ForeColor = result == "PASS" ? Color.FromArgb(52, 130, 68) : result == "FRICTION" ? Color.FromArgb(178, 62, 62) : Color.FromArgb(116, 126, 121);
                }
            }
            notes.Text = restoredNotes.ToString().Trim();
        }

        private static Button CreateButton(string text, bool primary, Action action)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(text.Length > 14 ? 165 : 130, 38);
            button.Margin = new Padding(0, 6, 8, 4);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = primary ? Color.FromArgb(155, 205, 38) : Color.White;
            button.Click += delegate { if (action != null) action(); };
            return button;
        }

        private void SetSelectedResult(string result)
        {
            if (checklist.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Select a fit-day stage first.", "Field Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (ListViewItem item in checklist.SelectedItems)
            {
                item.Text = result;
                item.Tag = result;
                item.ForeColor = result == "PASS" ? Color.FromArgb(52, 130, 68) : result == "FRICTION" ? Color.FromArgb(178, 62, 62) : Color.FromArgb(116, 126, 121);
            }
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            int pass = 0;
            int friction = 0;
            int notTested = 0;
            foreach (ListViewItem item in checklist.Items)
            {
                string result = Convert.ToString(item.Tag);
                if (result == "PASS") pass++;
                else if (result == "FRICTION") friction++;
                else notTested++;
            }
            status.Text = "PASS " + pass + "   ·   FRICTION " + friction + "   ·   NOT TESTED " + notTested + Environment.NewLine +
                (friction > 0 ? "Save the exact friction below so the next update can fix it." : notTested > 0 ? "Complete the fitting from intake through delivery." : "Full field-test path completed without a recorded blocker.");
            status.ForeColor = friction > 0 ? Color.FromArgb(178, 62, 62) : notTested > 0 ? Color.FromArgb(181, 118, 35) : Color.FromArgb(52, 130, 68);
        }

        private string BuildSummary()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Cassette Motion Pro v1.6.0 Field Test");
            text.AppendLine("Completed: " + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt"));
            text.AppendLine();
            foreach (ListViewItem item in checklist.Items)
                text.AppendLine(item.Text.PadRight(10) + " | " + item.SubItems[1].Text + " | " + item.SubItems[2].Text);
            text.AppendLine();
            text.AppendLine("Fitter observations:");
            text.AppendLine(string.IsNullOrWhiteSpace(notes.Text) ? "No additional observations entered." : notes.Text.Trim());
            return text.ToString();
        }

        private void SaveFieldTest()
        {
            HasBlockingFriction = false;
            IsComplete = true;
            foreach (ListViewItem item in checklist.Items)
            {
                if (Convert.ToString(item.Tag) == "FRICTION")
                    HasBlockingFriction = true;
                if (Convert.ToString(item.Tag) == "NOT TESTED")
                    IsComplete = false;
            }
            if (HasBlockingFriction && string.IsNullOrWhiteSpace(notes.Text))
            {
                MessageBox.Show(this, "Describe the friction before saving so it can be reproduced and fixed.", "Field Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
                notes.Focus();
                return;
            }

            FieldTestSummary = BuildSummary();
            try
            {
                Directory.CreateDirectory(saveFolder);
                string path = Path.Combine(saveFolder, "Fit-Day Field Test " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".txt");
                File.WriteAllText(path, FieldTestSummary);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The field-test record could not be saved.\n\n" + exception.Message, "Field Test", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
