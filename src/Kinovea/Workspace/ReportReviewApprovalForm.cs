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
    internal sealed class ReportReviewApprovalForm : Form
    {
        private readonly CheckBox contentConfirmed = new CheckBox();
        private readonly CheckBox evidenceConfirmed = new CheckBox();
        private readonly CheckBox privacyConfirmed = new CheckBox();
        private readonly TextBox notes = new TextBox();

        public string ApprovalNotes { get { return notes.Text.Trim(); } }

        public ReportReviewApprovalForm(string sessionName, string reviewText, string savedNotes)
        {
            Text = "Report Review and Approval";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 620);
            Size = new Size(900, 720);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22);
            layout.ColumnCount = 1;
            layout.RowCount = 8;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            Label eyebrow = new Label();
            eyebrow.Text = "CLIENT REPORT QUALITY GATE";
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);

            Label title = new Label();
            title.Text = "Approve report for " + sessionName;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);

            TextBox review = new TextBox();
            review.Text = reviewText ?? string.Empty;
            review.Dock = DockStyle.Fill;
            review.Multiline = true;
            review.ReadOnly = true;
            review.ScrollBars = ScrollBars.Vertical;
            review.BackColor = Color.White;
            review.Font = new Font("Segoe UI", 9.5F);

            contentConfirmed.Text = "I reviewed the client-facing summary, recommendations, and follow-up plan.";
            contentConfirmed.Dock = DockStyle.Fill;
            evidenceConfirmed.Text = "I reviewed the selected images and final measurement values.";
            evidenceConfirmed.Dock = DockStyle.Fill;
            privacyConfirmed.Text = "I confirmed the package contains the correct client and no unintended private notes.";
            privacyConfirmed.Dock = DockStyle.Fill;

            notes.Text = savedNotes ?? string.Empty;
            notes.Dock = DockStyle.Fill;
            notes.Multiline = true;
            notes.ScrollBars = ScrollBars.Vertical;

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button approve = new Button();
            approve.Text = "Approve for Delivery";
            approve.Size = new Size(165, 38);
            approve.BackColor = Color.FromArgb(155, 205, 38);
            approve.FlatStyle = FlatStyle.Flat;
            approve.Click += delegate
            {
                if (!contentConfirmed.Checked || !evidenceConfirmed.Checked || !privacyConfirmed.Checked)
                {
                    MessageBox.Show(this, "Confirm all three review items before approving the client report.", "Report Approval", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.Size = new Size(100, 38);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            actions.Controls.Add(approve);
            actions.Controls.Add(cancel);

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(review, 0, 2);
            layout.Controls.Add(contentConfirmed, 0, 3);
            layout.Controls.Add(evidenceConfirmed, 0, 4);
            layout.Controls.Add(privacyConfirmed, 0, 5);
            layout.Controls.Add(notes, 0, 6);
            layout.Controls.Add(actions, 0, 7);
            Controls.Add(layout);
        }
    }
}
