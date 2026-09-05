/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class AssistedWorkflowRecoveryForm : Form
    {
        private readonly ComboBox stage = new ComboBox();
        private readonly TextBox note = new TextBox();

        public string SelectedStage { get { return Convert.ToString(stage.SelectedItem); } }
        public string SkipNote { get { return note.Text.Trim(); } }
        public bool ClearSkip { get; private set; }

        public AssistedWorkflowRecoveryForm(string summary, string lastStage)
        {
            Text = "Assisted Workflow Check and Recovery";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(760, 650);
            MinimumSize = new Size(640, 560);
            BackColor = Color.FromArgb(244, 247, 242);
            Font = new Font("Segoe UI", 9.5F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22, 18, 22, 18);
            layout.ColumnCount = 1;
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            Label title = new Label();
            title.Text = "Workflow recovery summary";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);

            Label intro = new Label();
            intro.Text = "Review unfinished stages and missing files. A skipped stage stays visible in the session record and should include the fitter's reason.";
            intro.Dock = DockStyle.Fill;
            intro.ForeColor = Color.FromArgb(74, 87, 81);

            TextBox review = new TextBox();
            review.Dock = DockStyle.Fill;
            review.Multiline = true;
            review.ReadOnly = true;
            review.ScrollBars = ScrollBars.Vertical;
            review.BackColor = Color.White;
            review.Font = new Font("Consolas", 9.5F);
            review.Text = summary ?? string.Empty;

            Label stageLabel = new Label();
            stageLabel.Text = "Manually skip or restore a stage";
            stageLabel.Dock = DockStyle.Fill;
            stageLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            stage.Dock = DockStyle.Left;
            stage.Width = 260;
            stage.DropDownStyle = ComboBoxStyle.DropDownList;
            stage.Items.AddRange(new object[] { "Intake", "Record", "Track", "Measure", "Approve", "Report", "Follow-up" });
            if (!string.IsNullOrWhiteSpace(lastStage) && stage.Items.Contains(lastStage))
                stage.SelectedItem = lastStage;
            else
                stage.SelectedIndex = 0;

            note.Dock = DockStyle.Fill;
            note.Multiline = true;
            note.ScrollBars = ScrollBars.Vertical;
            note.Text = "Reason for skipping this stage";

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button close = CreateButton("Close", false, 100);
            close.DialogResult = DialogResult.Cancel;
            Button clear = CreateButton("Restore Stage", false, 125);
            clear.Click += delegate { ClearSkip = true; DialogResult = DialogResult.OK; Close(); };
            Button skip = CreateButton("Skip With Note", true, 135);
            skip.Click += delegate
            {
                if (string.IsNullOrWhiteSpace(note.Text) || string.Equals(note.Text.Trim(), "Reason for skipping this stage", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "Enter the fitter's reason before skipping a stage.", "Workflow Recovery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                ClearSkip = false;
                DialogResult = DialogResult.OK;
                Close();
            };
            actions.Controls.Add(close);
            actions.Controls.Add(clear);
            actions.Controls.Add(skip);

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(intro, 0, 1);
            layout.Controls.Add(review, 0, 2);
            layout.Controls.Add(stageLabel, 0, 3);
            layout.Controls.Add(stage, 0, 4);
            layout.Controls.Add(note, 0, 5);
            layout.Controls.Add(actions, 0, 6);
            Controls.Add(layout);
            CancelButton = close;
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
