using System;
using System.Drawing;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class AssistedMeasurementAccuracyReviewForm : Form
    {
        private readonly CheckBox verifyKinovea = new CheckBox();
        private readonly CheckBox acknowledgeWarnings = new CheckBox();
        private readonly TextBox notes = new TextBox();

        public bool Approved { get; private set; }
        public string ReviewNotes { get { return notes.Text.Trim(); } }

        public AssistedMeasurementAccuracyReviewForm(string sessionName, string scoreText, string summary, string savedNotes, bool previouslyApproved)
        {
            Text = "Assisted Measurement Accuracy Review";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(780, 600);
            Size = new Size(940, 720);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22);
            layout.ColumnCount = 1;
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            Label eyebrow = new Label();
            eyebrow.Text = "ASSISTED MEASUREMENT QUALITY GATE";
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);

            Label title = new Label();
            title.Text = sessionName + "   ·   " + scoreText;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);

            TextBox results = new TextBox();
            results.Text = summary ?? string.Empty;
            results.Dock = DockStyle.Fill;
            results.Multiline = true;
            results.ReadOnly = true;
            results.ScrollBars = ScrollBars.Both;
            results.WordWrap = false;
            results.Font = new Font("Consolas", 9.5F);
            results.BackColor = Color.White;

            verifyKinovea.Text = "I verified flagged landmarks and measurements in Kinovea.";
            verifyKinovea.Dock = DockStyle.Fill;
            verifyKinovea.Checked = previouslyApproved;
            acknowledgeWarnings.Text = "I reviewed the warnings and accept the approved values for this fit.";
            acknowledgeWarnings.Dock = DockStyle.Fill;
            acknowledgeWarnings.Checked = previouslyApproved;

            notes.Text = savedNotes ?? string.Empty;
            notes.Dock = DockStyle.Fill;
            notes.Multiline = true;
            notes.ScrollBars = ScrollBars.Vertical;

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button approve = new Button();
            approve.Text = "Approve Review";
            approve.Size = new Size(145, 38);
            approve.BackColor = Color.FromArgb(155, 205, 38);
            approve.FlatStyle = FlatStyle.Flat;
            approve.Click += delegate
            {
                if (!verifyKinovea.Checked || !acknowledgeWarnings.Checked)
                {
                    MessageBox.Show(this, "Confirm both fitter-review checks before approving. Cassette Motion Pro will never approve measurements automatically.", "Accuracy Review", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Approved = true;
                DialogResult = DialogResult.OK;
                Close();
            };
            Button close = new Button();
            close.Text = "Close Without Approval";
            close.Size = new Size(170, 38);
            close.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            actions.Controls.Add(approve);
            actions.Controls.Add(close);

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(results, 0, 2);
            layout.Controls.Add(verifyKinovea, 0, 3);
            layout.Controls.Add(acknowledgeWarnings, 0, 4);
            layout.Controls.Add(notes, 0, 5);
            layout.Controls.Add(actions, 0, 6);
            Controls.Add(layout);
        }
    }
}
