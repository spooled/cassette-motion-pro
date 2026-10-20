/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class FitPositionHistoryForm : Form
    {
        private readonly FitSessionRecord session;
        private readonly ListView positions = new ListView();
        private readonly ListView comparison = new ListView();
        private readonly TextBox name = new TextBox();
        private readonly ComboBox source = new ComboBox();
        private readonly TextBox notes = new TextBox();
        private readonly Label summary = new Label();

        public bool Changed { get; private set; }

        public FitPositionHistoryForm(FitSessionRecord session)
        {
            this.session = session;
            Text = "Position History — " + session.DisplayName;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1080, 720);
            MinimumSize = new Size(820, 560);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            Label intro = new Label();
            intro.Dock = DockStyle.Fill;
            intro.Font = new Font("Segoe UI", 10F);
            intro.Text = "Save each meaningful bike position as the fit develops. Baseline uses the Before fields; adjustment and final positions use the current After fields. Existing Before/After media stays unchanged.";
            root.Controls.Add(intro, 0, 0);

            TableLayoutPanel capture = new TableLayoutPanel();
            capture.Dock = DockStyle.Fill;
            capture.ColumnCount = 4;
            capture.RowCount = 2;
            capture.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            capture.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            capture.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            capture.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
            capture.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            capture.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            capture.Controls.Add(FieldLabel("Position name"), 0, 0);
            capture.Controls.Add(FieldLabel("Snapshot source"), 1, 0);
            capture.Controls.Add(FieldLabel("What changed"), 2, 0);
            name.Dock = DockStyle.Fill;
            name.Text = "Position " + NextSequence().ToString();
            source.Dock = DockStyle.Fill;
            source.DropDownStyle = ComboBoxStyle.DropDownList;
            source.Items.AddRange(new object[] { "Baseline (Before)", "Adjustment (After)", "Final (After)" });
            source.SelectedIndex = session.FitPositions == null || session.FitPositions.Count == 0 ? 0 : 1;
            notes.Dock = DockStyle.Fill;
            notes.Multiline = true;
            Button captureButton = new Button();
            captureButton.Text = "SAVE POSITION";
            captureButton.Dock = DockStyle.Fill;
            captureButton.BackColor = Color.FromArgb(203, 232, 0);
            captureButton.FlatStyle = FlatStyle.Flat;
            captureButton.Click += CapturePosition;
            capture.Controls.Add(name, 0, 1);
            capture.Controls.Add(source, 1, 1);
            capture.Controls.Add(notes, 2, 1);
            capture.Controls.Add(captureButton, 3, 1);
            root.Controls.Add(capture, 0, 1);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 440;
            positions.Dock = DockStyle.Fill;
            positions.View = View.Details;
            positions.FullRowSelect = true;
            positions.GridLines = true;
            positions.HideSelection = false;
            positions.Columns.Add("#", 38);
            positions.Columns.Add("Position", 150);
            positions.Columns.Add("Phase", 90);
            positions.Columns.Add("Saved", 130);
            positions.Columns.Add("Final", 55);
            positions.SelectedIndexChanged += delegate { RefreshComparison(); };
            split.Panel1.Controls.Add(positions);

            TableLayoutPanel review = new TableLayoutPanel();
            review.Dock = DockStyle.Fill;
            review.RowCount = 2;
            review.ColumnCount = 1;
            review.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            review.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            summary.Dock = DockStyle.Fill;
            summary.Padding = new Padding(8);
            summary.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            comparison.Dock = DockStyle.Fill;
            comparison.View = View.Details;
            comparison.GridLines = true;
            comparison.FullRowSelect = true;
            comparison.Columns.Add("Measurement", 150);
            comparison.Columns.Add("Baseline", 105);
            comparison.Columns.Add("Selected", 105);
            comparison.Columns.Add("Change", 105);
            review.Controls.Add(summary, 0, 0);
            review.Controls.Add(comparison, 0, 1);
            split.Panel2.Controls.Add(review);
            root.Controls.Add(split, 0, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button close = ActionButton("Done", delegate { Close(); });
            Button delete = ActionButton("Delete Selected", DeleteSelected);
            Button final = ActionButton("Mark Selected Final", MarkSelectedFinal);
            actions.Controls.Add(close);
            actions.Controls.Add(delete);
            actions.Controls.Add(final);
            root.Controls.Add(actions, 0, 3);
            Controls.Add(root);
            RefreshPositions();
        }

        private static Label FieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
        }

        private static Button ActionButton(string text, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(120, 34);
            button.Click += click;
            return button;
        }

        private int NextSequence()
        {
            int next = 1;
            if (session.FitPositions != null)
                foreach (FitPositionRecord item in session.FitPositions)
                    if (item != null && item.Sequence >= next)
                        next = item.Sequence + 1;
            return next;
        }

        private void CapturePosition(object sender, EventArgs e)
        {
            string positionName = name.Text.Trim();
            if (string.IsNullOrWhiteSpace(positionName))
            {
                MessageBox.Show(this, "Enter a short position name, such as Baseline, Saddle +5 mm, or Final.", "Position History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (session.FitPositions == null)
                session.FitPositions = new List<FitPositionRecord>();
            bool before = source.SelectedIndex == 0;
            bool isFinal = source.SelectedIndex == 2;
            if (isFinal)
                ClearFinalFlags();
            FitPositionRecord item = new FitPositionRecord
            {
                Id = Guid.NewGuid(),
                Sequence = NextSequence(),
                Name = positionName,
                Phase = before ? "Baseline" : (isFinal ? "Final" : "Adjustment"),
                ChangeNotes = notes.Text.Trim(),
                CapturedUtc = DateTime.UtcNow,
                IsFinal = isFinal,
                VideoPath = before ? session.BeforeVideoPath : session.AfterVideoPath,
                ImagePath = before ? session.BeforeReportImagePath : session.AfterReportImagePath,
                SaddleHeight = before ? session.SaddleHeightBefore : session.SaddleHeightAfter,
                SaddleSetback = before ? session.SaddleSetbackBefore : session.SaddleSetbackAfter,
                SaddleTipToGripReach = before ? session.SaddleTipToGripReachBefore : session.SaddleTipToGripReachAfter,
                HandlebarX = before ? session.HandlebarXBefore : session.HandlebarXAfter,
                HandlebarY = before ? session.HandlebarYBefore : session.HandlebarYAfter,
                KneeAngle = before ? session.KneeAngleBefore : session.KneeAngleAfter,
                HipAngle = before ? session.HipAngleBefore : session.HipAngleAfter,
                AnkleAngle = before ? session.AnkleAngleBefore : session.AnkleAngleAfter,
                BodyReach = before ? session.TorsoAngleBefore : session.TorsoAngleAfter,
                BackAngle = before ? session.ShoulderAngleBefore : session.ShoulderAngleAfter
            };
            session.FitPositions.Add(item);
            Changed = true;
            name.Text = "Position " + NextSequence().ToString();
            notes.Clear();
            source.SelectedIndex = 1;
            RefreshPositions(item.Id);
        }

        private void MarkSelectedFinal(object sender, EventArgs e)
        {
            FitPositionRecord selected = SelectedPosition();
            if (selected == null)
                return;
            ClearFinalFlags();
            selected.IsFinal = true;
            selected.Phase = "Final";
            Changed = true;
            RefreshPositions(selected.Id);
        }

        private void DeleteSelected(object sender, EventArgs e)
        {
            FitPositionRecord selected = SelectedPosition();
            if (selected == null)
                return;
            if (MessageBox.Show(this, "Delete “" + selected.Name + "” from this session’s position history?", "Position History", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            session.FitPositions.Remove(selected);
            Changed = true;
            RefreshPositions();
        }

        private void ClearFinalFlags()
        {
            if (session.FitPositions == null)
                return;
            foreach (FitPositionRecord item in session.FitPositions)
                if (item != null)
                    item.IsFinal = false;
        }

        private FitPositionRecord SelectedPosition()
        {
            return positions.SelectedItems.Count == 0 ? null : positions.SelectedItems[0].Tag as FitPositionRecord;
        }

        private void RefreshPositions()
        {
            RefreshPositions(Guid.Empty);
        }

        private void RefreshPositions(Guid selectId)
        {
            positions.Items.Clear();
            if (session.FitPositions != null)
            {
                session.FitPositions.Sort(delegate(FitPositionRecord left, FitPositionRecord right) { return left.Sequence.CompareTo(right.Sequence); });
                foreach (FitPositionRecord item in session.FitPositions)
                {
                    if (item == null)
                        continue;
                    DateTime local = item.CapturedUtc.Kind == DateTimeKind.Utc ? item.CapturedUtc.ToLocalTime() : item.CapturedUtc;
                    ListViewItem row = new ListViewItem(new[] { item.Sequence.ToString(), item.Name, item.Phase, local == DateTime.MinValue ? "—" : local.ToString("g"), item.IsFinal ? "YES" : "" });
                    row.Tag = item;
                    positions.Items.Add(row);
                    if (item.Id == selectId)
                        row.Selected = true;
                }
            }
            if (positions.SelectedItems.Count == 0 && positions.Items.Count > 0)
                positions.Items[positions.Items.Count - 1].Selected = true;
            RefreshComparison();
        }

        private void RefreshComparison()
        {
            comparison.Items.Clear();
            FitPositionRecord selected = SelectedPosition();
            FitPositionRecord baseline = BaselinePosition();
            if (selected == null)
            {
                summary.Text = "No saved positions yet. Save Baseline first, then save each meaningful adjustment.";
                return;
            }
            summary.Text = selected.Name + " · " + selected.Phase + (selected.IsFinal ? " · FINAL" : "") + Environment.NewLine +
                (string.IsNullOrWhiteSpace(selected.ChangeNotes) ? "No change note recorded." : selected.ChangeNotes) + MediaSummary(selected);
            AddMetric("Saddle height", baseline, selected, delegate(FitPositionRecord x) { return x.SaddleHeight; }, "mm");
            AddMetric("Saddle setback", baseline, selected, delegate(FitPositionRecord x) { return x.SaddleSetback; }, "mm");
            AddMetric("Saddle to grip", baseline, selected, delegate(FitPositionRecord x) { return x.SaddleTipToGripReach; }, "mm");
            AddMetric("Handlebar X", baseline, selected, delegate(FitPositionRecord x) { return x.HandlebarX; }, "mm");
            AddMetric("Handlebar Y", baseline, selected, delegate(FitPositionRecord x) { return x.HandlebarY; }, "mm");
            AddMetric("Knee angle", baseline, selected, delegate(FitPositionRecord x) { return x.KneeAngle; }, "°");
            AddMetric("Hip angle", baseline, selected, delegate(FitPositionRecord x) { return x.HipAngle; }, "°");
            AddMetric("Ankle angle", baseline, selected, delegate(FitPositionRecord x) { return x.AnkleAngle; }, "°");
            AddMetric("Body reach", baseline, selected, delegate(FitPositionRecord x) { return x.BodyReach; }, "°");
            AddMetric("Back angle", baseline, selected, delegate(FitPositionRecord x) { return x.BackAngle; }, "°");
        }

        private FitPositionRecord BaselinePosition()
        {
            if (session.FitPositions == null)
                return null;
            foreach (FitPositionRecord item in session.FitPositions)
                if (item != null && string.Equals(item.Phase, "Baseline", StringComparison.OrdinalIgnoreCase))
                    return item;
            return session.FitPositions.Count == 0 ? null : session.FitPositions[0];
        }

        private static string MediaSummary(FitPositionRecord item)
        {
            string media = string.Empty;
            if (!string.IsNullOrWhiteSpace(item.ImagePath))
                media += " · Image: " + Path.GetFileName(item.ImagePath);
            if (!string.IsNullOrWhiteSpace(item.VideoPath))
                media += " · Video: " + Path.GetFileName(item.VideoPath);
            return media;
        }

        private void AddMetric(string label, FitPositionRecord baseline, FitPositionRecord selected, Func<FitPositionRecord, string> value, string unit)
        {
            string before = baseline == null ? string.Empty : value(baseline);
            string current = value(selected);
            string change = "—";
            double first;
            double second;
            if (TryNumber(before, out first) && TryNumber(current, out second))
            {
                double delta = second - first;
                change = (delta > 0 ? "+" : "") + delta.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
            }
            comparison.Items.Add(new ListViewItem(new[] { label, Value(before), Value(current), change }));
        }

        private static string Value(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "—" : text.Trim();
        }

        private static bool TryNumber(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            string cleaned = text.Trim().Replace("mm", string.Empty).Replace("°", string.Empty).Trim();
            return double.TryParse(cleaned, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) ||
                double.TryParse(cleaned, out value);
        }
    }
}
