/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public sealed class FavoriteFrameComparisonForm : Form
    {
        private string beforePath;
        private string afterPath;
        private readonly PictureBox beforePreview = new PictureBox();
        private readonly PictureBox afterPreview = new PictureBox();
        private readonly Label qualityStatus = new Label();
        private readonly TextBox beforeNotes = new TextBox();
        private readonly TextBox afterNotes = new TextBox();
        private readonly CheckBox framingApproved = new CheckBox();
        private readonly CheckBox positionApproved = new CheckBox();
        private readonly Button approve = new Button();

        public string BeforePath { get { return beforePath; } }
        public string AfterPath { get { return afterPath; } }
        public string BeforeNotes { get { return beforeNotes.Text.Trim(); } }
        public string AfterNotes { get { return afterNotes.Text.Trim(); } }
        public string QualitySummary { get; private set; }

        public FavoriteFrameComparisonForm(string beforePath, string afterPath, string savedBeforeNotes, string savedAfterNotes)
        {
            if (!File.Exists(beforePath) || !File.Exists(afterPath))
                throw new FileNotFoundException("Both favorite report frames are required.");
            this.beforePath = beforePath;
            this.afterPath = afterPath;

            Text = "Cassette Motion Pro - Favorite Frame Comparison";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ClientSize = new Size(1280, 820);
            MinimumSize = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterParent;
            BuildInterface();
            beforeNotes.Text = savedBeforeNotes ?? string.Empty;
            afterNotes.Text = savedAfterNotes ?? string.Empty;
            LoadPreviews();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 4;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(20, 27, 24);
            Label title = NewLabel("Favorite-Frame Comparison and Approval", 18F, true);
            title.ForeColor = Color.White;
            title.Location = new Point(22, 12);
            Label intro = NewLabel("Confirm matching framing and a useful rider/crank position before creating the Dual report image.", 9F, false);
            intro.ForeColor = Color.FromArgb(205, 216, 210);
            intro.Location = new Point(24, 51);
            header.Controls.Add(title);
            header.Controls.Add(intro);
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            root.Controls.Add(BuildImagePanel("BEFORE", beforePreview, delegate { ReplaceFrame(true); }), 0, 1);
            root.Controls.Add(BuildImagePanel("AFTER", afterPreview, delegate { ReplaceFrame(false); }), 1, 1);
            root.Controls.Add(BuildNotesPanel("Before frame notes", beforeNotes), 0, 2);
            root.Controls.Add(BuildNotesPanel("After frame notes", afterNotes), 1, 2);

            TableLayoutPanel approval = new TableLayoutPanel();
            approval.Dock = DockStyle.Fill;
            approval.Padding = new Padding(14, 8, 14, 10);
            approval.ColumnCount = 4;
            approval.RowCount = 2;
            approval.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            approval.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245));
            approval.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            approval.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            approval.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            approval.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            qualityStatus.Dock = DockStyle.Fill;
            qualityStatus.Padding = new Padding(10, 5, 10, 5);
            qualityStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            qualityStatus.TextAlign = ContentAlignment.MiddleLeft;
            framingApproved.Text = "Framing and camera view match";
            framingApproved.Dock = DockStyle.Fill;
            framingApproved.CheckedChanged += delegate { UpdateApproveState(); };
            positionApproved.Text = "Rider/crank positions are useful";
            positionApproved.Dock = DockStyle.Fill;
            positionApproved.CheckedChanged += delegate { UpdateApproveState(); };
            approve.Text = "Approve + Create Dual";
            StyleButton(approve, true);
            approve.Enabled = false;
            approve.Click += Approve_Click;
            Button cancel = new Button();
            cancel.Text = "Cancel";
            StyleButton(cancel, false);
            cancel.Click += delegate { Close(); };

            approval.Controls.Add(qualityStatus, 0, 0);
            approval.Controls.Add(framingApproved, 1, 0);
            approval.Controls.Add(positionApproved, 2, 0);
            approval.Controls.Add(cancel, 3, 0);
            approval.Controls.Add(approve, 1, 1);
            approval.SetColumnSpan(approve, 2);
            root.Controls.Add(approval, 0, 3);
            root.SetColumnSpan(approval, 2);
            Controls.Add(root);
        }

        private static Panel BuildImagePanel(string title, PictureBox preview, Action replace)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(12);
            panel.BackColor = Color.FromArgb(31, 36, 34);
            Label label = NewLabel(title, 11F, true);
            label.Dock = DockStyle.Top;
            label.Height = 30;
            label.ForeColor = title == "BEFORE" ? Color.FromArgb(184, 228, 35) : Color.White;
            Button button = new Button();
            button.Text = "Replace " + title.Substring(0, 1) + title.Substring(1).ToLowerInvariant() + " Frame";
            StyleButton(button, false);
            button.Dock = DockStyle.Bottom;
            button.Height = 38;
            button.Click += delegate { replace(); };
            preview.Dock = DockStyle.Fill;
            preview.SizeMode = PictureBoxSizeMode.Zoom;
            preview.BackColor = Color.Black;
            panel.Controls.Add(preview);
            panel.Controls.Add(button);
            panel.Controls.Add(label);
            return panel;
        }

        private static Panel BuildNotesPanel(string title, TextBox notes)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(12, 6, 12, 8);
            Label label = NewLabel(title, 9F, true);
            label.Dock = DockStyle.Top;
            label.Height = 25;
            notes.Dock = DockStyle.Fill;
            notes.Multiline = true;
            notes.ScrollBars = ScrollBars.Vertical;
            panel.Controls.Add(notes);
            panel.Controls.Add(label);
            return panel;
        }

        private void ReplaceFrame(bool before)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose replacement " + (before ? "Before" : "After") + " frame";
                dialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*";
                string current = before ? beforePath : afterPath;
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(Path.GetDirectoryName(current)))
                    dialog.InitialDirectory = Path.GetDirectoryName(current);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                if (before) beforePath = dialog.FileName;
                else afterPath = dialog.FileName;
                framingApproved.Checked = false;
                positionApproved.Checked = false;
                LoadPreviews();
            }
        }

        private void LoadPreviews()
        {
            ReplacePreview(beforePreview, beforePath);
            ReplacePreview(afterPreview, afterPath);
            using (Image before = Image.FromFile(beforePath))
            using (Image after = Image.FromFile(afterPath))
            {
                double beforeRatio = before.Width / (double)before.Height;
                double afterRatio = after.Width / (double)after.Height;
                double ratioDifference = Math.Abs(beforeRatio - afterRatio) / Math.Max(beforeRatio, afterRatio);
                bool orientationMatches = (before.Width >= before.Height) == (after.Width >= after.Height);
                if (!orientationMatches || ratioDifference > 0.08)
                {
                    QualitySummary = "Framing warning: image shape or orientation differs. Confirm camera placement and rider scale before approval.";
                    qualityStatus.BackColor = Color.FromArgb(255, 244, 214);
                    qualityStatus.ForeColor = Color.FromArgb(128, 82, 12);
                }
                else
                {
                    QualitySummary = "Framing check ready: image shape and orientation are closely matched. Visually confirm camera angle and crank position.";
                    qualityStatus.BackColor = Color.FromArgb(232, 246, 226);
                    qualityStatus.ForeColor = Color.FromArgb(46, 108, 55);
                }
                qualityStatus.Text = QualitySummary;
            }
            UpdateApproveState();
        }

        private static void ReplacePreview(PictureBox preview, string path)
        {
            Image next;
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image source = Image.FromStream(stream))
                next = new Bitmap(source);
            Image previous = preview.Image;
            preview.Image = next;
            if (previous != null) previous.Dispose();
        }

        private void UpdateApproveState()
        {
            approve.Enabled = framingApproved.Checked && positionApproved.Checked;
        }

        private void Approve_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this,
                "Approve these Before and After frames and create the Dual report comparison?\n\nThe source images will remain unchanged.",
                "Approve Favorite Frames", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (beforePreview.Image != null) beforePreview.Image.Dispose();
                if (afterPreview.Image != null) afterPreview.Image.Dispose();
            }
            base.Dispose(disposing);
        }

        private static Label NewLabel(string text, float size, bool bold)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
            label.AutoSize = true;
            return label;
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(142, 182, 0) : Color.FromArgb(188, 198, 193);
            button.BackColor = primary ? Color.FromArgb(184, 228, 35) : Color.White;
            button.ForeColor = Color.FromArgb(26, 34, 31);
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }
    }
}
