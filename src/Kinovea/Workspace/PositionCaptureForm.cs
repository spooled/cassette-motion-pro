/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    // Asks for a clip, a time range and a position name, then pulls evenly spaced frames out of the clip.
    internal sealed class PositionCaptureForm : Form
    {
        private readonly TextBox clipPath = new TextBox();
        private readonly Label clipInfo = new Label();
        private readonly NumericUpDown startSeconds = new NumericUpDown();
        private readonly NumericUpDown endSeconds = new NumericUpDown();
        private readonly RadioButton shortClip = new RadioButton();
        private readonly RadioButton pedalTurn = new RadioButton();
        private readonly TextBox positionName = new TextBox();
        private readonly ComboBox phase = new ComboBox();
        private readonly TextBox notes = new TextBox();
        private readonly string framesRoot;
        private double clipSeconds;

        public string[] Frames { get; private set; }
        public bool PedalTurn { get { return pedalTurn.Checked; } }
        public string PositionName { get { return positionName.Text.Trim(); } }
        public string Phase { get { return Convert.ToString(phase.SelectedItem); } }
        public string Notes { get { return notes.Text.Trim(); } }
        public string VideoPath { get { return clipPath.Text.Trim(); } }
        public string Side { get { return Phase == "Baseline" ? "Before" : "After"; } }

        public PositionCaptureForm(string suggestedClip, string suggestedName, string framesRoot)
        {
            CassetteMotionTheme.Attach(this);
            this.framesRoot = framesRoot;
            Text = "Capture Position";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(640, 560);

            Label intro = new Label();
            intro.Text = "Pick the clip and the few seconds where the rider is pedalling steadily. The app pulls evenly spaced frames from it, then opens the tracking review so you can check the joints.";
            intro.SetBounds(20, 16, 600, 48);

            AddLabel("Clip", 76);
            clipPath.SetBounds(20, 96, 500, 26);
            clipPath.Text = suggestedClip ?? string.Empty;
            clipPath.Leave += delegate { ReadClipInfo(); };
            Button browse = new Button();
            browse.Text = "Browse...";
            browse.SetBounds(530, 94, 90, 30);
            browse.Click += Browse_Click;
            clipInfo.SetBounds(20, 126, 600, 22);
            clipInfo.ForeColor = Color.FromArgb(92, 105, 98);

            AddLabel("Start (seconds)", 160);
            AddLabel("End (seconds)", 160, 200);
            ConfigureSeconds(startSeconds, 20, 180, 1m);
            ConfigureSeconds(endSeconds, 200, 180, 4m);

            AddLabel("How many frames", 224);
            shortClip.Text = "Short clip, 12 frames";
            shortClip.Checked = true;
            shortClip.SetBounds(20, 246, 280, 26);
            pedalTurn.Text = "One full pedal turn, 16 frames";
            pedalTurn.SetBounds(310, 246, 300, 26);

            AddLabel("Position name", 290);
            positionName.SetBounds(20, 310, 290, 26);
            positionName.Text = suggestedName ?? "Position 1";
            AddLabel("What is this position?", 290, 330);
            phase.DropDownStyle = ComboBoxStyle.DropDownList;
            phase.Items.AddRange(new object[] { "Baseline", "Adjustment", "Final" });
            phase.SelectedIndex = 1;
            phase.SetBounds(330, 310, 290, 26);

            AddLabel("Notes (what changed, optional)", 352);
            notes.Multiline = true;
            notes.SetBounds(20, 372, 600, 70);

            Label hint = new Label();
            hint.Text = "Tip: camera square to the bike at saddle height, rider pedalling at a steady cadence, at least one full pedal turn in the range.";
            hint.ForeColor = Color.FromArgb(92, 105, 98);
            hint.SetBounds(20, 452, 600, 40);

            Button go = new Button();
            go.Text = "Extract Frames and Review";
            go.SetBounds(360, 506, 170, 36);
            go.Click += Go_Click;
            CassetteMotionTheme.StyleButton(go, true);
            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.SetBounds(540, 506, 80, 36);
            CassetteMotionTheme.StyleButton(cancel, false);
            CassetteMotionTheme.StyleButton(browse, false);

            Controls.AddRange(new Control[] { intro, clipPath, browse, clipInfo, startSeconds, endSeconds, shortClip, pedalTurn,
                positionName, phase, notes, hint, go, cancel });
            CancelButton = cancel;
            Shown += delegate { ReadClipInfo(); };
        }

        private void AddLabel(string text, int top)
        {
            AddLabel(text, top, 20);
        }

        private void AddLabel(string text, int top, int left)
        {
            Label label = new Label();
            label.Text = text;
            label.SetBounds(left, top, 290, 20);
            Controls.Add(label);
        }

        private static void ConfigureSeconds(NumericUpDown box, int left, int top, decimal value)
        {
            box.SetBounds(left, top, 150, 26);
            box.DecimalPlaces = 1;
            box.Increment = 0.5m;
            box.Minimum = 0;
            box.Maximum = 3600;
            box.Value = value;
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose the clip";
                dialog.Filter = "Video files|*.mp4;*.mov;*.avi;*.mkv;*.m4v;*.wmv|All files|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                clipPath.Text = dialog.FileName;
                ReadClipInfo();
            }
        }

        private void ReadClipInfo()
        {
            clipSeconds = 0;
            if (!File.Exists(clipPath.Text))
            {
                clipInfo.Text = string.IsNullOrWhiteSpace(clipPath.Text) ? "Choose a clip." : "That file was not found.";
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                ClipProbe probe = ClipFrameExtractor.Probe(clipPath.Text);
                clipSeconds = probe.DurationSeconds;
                clipInfo.Text = string.Format(CultureInfo.InvariantCulture, "Clip length {0:0.0} s  ·  {1:0.#} fps  ·  {2} x {3}",
                    probe.DurationSeconds, probe.FramesPerSecond, probe.FrameSize.Width, probe.FrameSize.Height);
                decimal max = (decimal)Math.Max(0.1, Math.Floor(probe.DurationSeconds * 10) / 10);
                startSeconds.Maximum = max;
                endSeconds.Maximum = max;
                if (endSeconds.Value > max)
                    endSeconds.Value = max;
                if (startSeconds.Value > max)
                    startSeconds.Value = 0;
            }
            catch (Exception exception)
            {
                clipInfo.Text = exception.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void Go_Click(object sender, EventArgs e)
        {
            if (!File.Exists(clipPath.Text))
            {
                MessageBox.Show(this, "Choose a clip first.", "Capture Position", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(positionName.Text))
            {
                MessageBox.Show(this, "Give the position a short name, such as Baseline or Saddle +5 mm.", "Capture Position", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (endSeconds.Value <= startSeconds.Value)
            {
                MessageBox.Show(this, "The end time must be after the start time.", "Capture Position", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int count = pedalTurn.Checked ? 16 : 12;
            string folder = Path.Combine(framesRoot, "Position-Frames", SafeName(positionName.Text) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            try
            {
                Cursor = Cursors.WaitCursor;
                Frames = ClipFrameExtractor.Extract(clipPath.Text, (double)startSeconds.Value, (double)endSeconds.Value, count, folder, "frame");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "Capture Position", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            int needed = pedalTurn.Checked ? 6 : 3;
            if (Frames.Length < needed)
            {
                MessageBox.Show(this, "Only " + Frames.Length + " distinct frames could be read from that range, and at least " + needed + " are needed. Try a longer range.", "Capture Position", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string SafeName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '-');
            return value.Trim();
        }
    }
}
