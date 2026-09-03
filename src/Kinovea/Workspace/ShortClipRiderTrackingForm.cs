/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class ShortClipRiderTrackingForm : Form
    {
        private readonly string[] framePaths;
        private readonly string outputDirectory;
        private readonly string side;
        private readonly bool pedalCycleMode;
        private readonly ShortClipTrackingCanvas canvas = new ShortClipTrackingCanvas();
        private readonly Label frameStatus = new Label();
        private readonly Label quality = new Label();
        private readonly Label ranges = new Label();
        private readonly Button previous = new Button();
        private readonly Button approveNext = new Button();
        private readonly Button finish = new Button();
        private readonly List<TrackedFrame> frames = new List<TrackedFrame>();
        private readonly Dictionary<string, TrackingImageQuality> frameQualityCache = new Dictionary<string, TrackingImageQuality>(StringComparer.OrdinalIgnoreCase);
        private int frameIndex;
        private int correctionCount;

        public Dictionary<string, string> ResultValues { get; private set; }
        public string TrackingSummary { get; private set; }
        public string EvidenceImagePath { get; private set; }
        public string SmartFrameSummary { get; private set; }
        public string SmartFrameEvidencePath { get; private set; }

        public ShortClipRiderTrackingForm(string[] framePaths, string outputDirectory, string side)
            : this(framePaths, outputDirectory, side, false)
        {
        }

        public ShortClipRiderTrackingForm(string[] framePaths, string outputDirectory, string side, bool pedalCycleMode)
        {
            if (framePaths == null || framePaths.Length < 3)
                throw new ArgumentException("At least three ordered checkpoint frames are required.");
            if (pedalCycleMode && framePaths.Length < 6)
                throw new ArgumentException("At least six ordered checkpoint frames are required for a pedal-cycle review.");
            this.framePaths = framePaths.Take(pedalCycleMode ? 16 : 12).ToArray();
            this.outputDirectory = outputDirectory;
            this.side = side;
            this.pedalCycleMode = pedalCycleMode;

            Text = pedalCycleMode ? "Cassette Motion Pro - Pedal-Cycle Measurement Review" : "Cassette Motion Pro - Short-Clip Rider Tracking";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ClientSize = new Size(1280, 800);
            MinimumSize = new Size(1040, 680);
            StartPosition = FormStartPosition.CenterParent;
            BuildInterface();
            LoadFirstFrame();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(20, 27, 24);
            Label title = NewLabel((pedalCycleMode ? "Pedal-Cycle Measurement Review" : "Short-Clip Rider Tracking") + " · " + side, 18F, true);
            title.ForeColor = Color.White;
            title.Location = new Point(22, 12);
            title.AutoSize = true;
            Label intro = NewLabel(pedalCycleMode
                ? "Review one complete pedal turn. The ankle path identifies top, bottom, front, and rear crank positions after every checkpoint is approved."
                : "Approve the first pose, then review each tracked checkpoint. Drag an orange point whenever tracking drifts.", 9F, false);
            intro.ForeColor = Color.FromArgb(205, 216, 210);
            intro.Location = new Point(24, 48);
            intro.AutoSize = true;
            header.Controls.Add(title);
            header.Controls.Add(intro);
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            canvas.Dock = DockStyle.Fill;
            canvas.Margin = new Padding(12);
            canvas.PoseCorrected += delegate { correctionCount++; UpdateStatus(); };
            root.Controls.Add(canvas, 0, 1);

            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.BackColor = Color.White;
            panel.Padding = new Padding(20);

            frameStatus.Dock = DockStyle.Top;
            frameStatus.Height = 58;
            frameStatus.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            frameStatus.Padding = new Padding(10);
            quality.Dock = DockStyle.Top;
            quality.Height = 115;
            quality.Padding = new Padding(10);
            ranges.Dock = DockStyle.Top;
            ranges.Height = pedalCycleMode ? 430 : 180;
            ranges.Font = new Font("Consolas", 9.5F, FontStyle.Bold);
            ranges.Padding = new Padding(4, 12, 4, 4);

            ConfigureButton(previous, "Previous Checkpoint", false);
            ConfigureButton(approveNext, "Approve & Track Next", true);
            ConfigureButton(finish, pedalCycleMode ? "Save Pedal-Cycle Review" : "Save Accepted Motion Range", true);
            Button reset = new Button();
            ConfigureButton(reset, "Reset This Checkpoint", false);
            Button flip = new Button();
            ConfigureButton(flip, "Flip Rider Direction", false);
            Button cancel = new Button();
            ConfigureButton(cancel, "Cancel", false);
            previous.Click += Previous_Click;
            approveNext.Click += ApproveNext_Click;
            finish.Click += Finish_Click;
            reset.Click += delegate { RestoreCurrentFrame(); };
            flip.Click += delegate { if (frameIndex == 0) { canvas.Flip(); SaveCanvasToCurrent(false); UpdateStatus(); } };
            cancel.Click += delegate { Close(); };

            panel.Controls.Add(cancel);
            panel.Controls.Add(finish);
            panel.Controls.Add(approveNext);
            panel.Controls.Add(previous);
            panel.Controls.Add(reset);
            panel.Controls.Add(flip);
            panel.Controls.Add(ranges);
            panel.Controls.Add(quality);
            panel.Controls.Add(frameStatus);
            root.Controls.Add(panel, 1, 1);
            Controls.Add(root);
        }

        private void LoadFirstFrame()
        {
            canvas.LoadFrame(framePaths[0], null);
            canvas.Suggest();
            frames.Add(new TrackedFrame(framePaths[0], canvas.Points, canvas.Confidence, false));
            UpdateStatus();
        }

        private void ApproveNext_Click(object sender, EventArgs e)
        {
            SaveCanvasToCurrent(true);
            if (frameIndex >= framePaths.Length - 1)
            {
                UpdateStatus();
                return;
            }

            List<PointF> seed = canvas.Points;
            string previousPath = framePaths[frameIndex];
            frameIndex++;
            TrackResult tracked = ShortClipPointTracker.Track(previousPath, framePaths[frameIndex], seed);
            canvas.LoadFrame(framePaths[frameIndex], tracked.Points);
            canvas.Confidence = tracked.Confidence;
            TrackedFrame nextFrame = new TrackedFrame(framePaths[frameIndex], tracked.Points, tracked.Confidence, false);
            if (frameIndex < frames.Count)
                frames[frameIndex] = nextFrame;
            else
                frames.Add(nextFrame);
            UpdateStatus();
        }

        private void Previous_Click(object sender, EventArgs e)
        {
            SaveCanvasToCurrent(false);
            if (frameIndex == 0)
                return;
            frameIndex--;
            RestoreCurrentFrame();
        }

        private void RestoreCurrentFrame()
        {
            TrackedFrame frame = frames[frameIndex];
            canvas.LoadFrame(frame.Path, frame.Points);
            canvas.Confidence = frame.Confidence;
            UpdateStatus();
        }

        private void SaveCanvasToCurrent(bool approved)
        {
            TrackedFrame updated = new TrackedFrame(framePaths[frameIndex], canvas.Points, canvas.Confidence, approved);
            if (frameIndex < frames.Count)
                frames[frameIndex] = updated;
            else
                frames.Add(updated);
        }

        private void UpdateStatus()
        {
            frameStatus.Text = "CHECKPOINT " + (frameIndex + 1) + " OF " + framePaths.Length +
                (frames[frameIndex].Approved ? " · APPROVED" : " · REVIEW");
            bool drift = canvas.Confidence < 55;
            quality.Text = drift
                ? "TRACKING CHECK: CORRECTION RECOMMENDED\nConfidence " + canvas.Confidence.ToString("0", CultureInfo.InvariantCulture) + "% · Drag any point that has moved away from the joint, then approve."
                : "TRACKING CHECK: READY TO REVIEW\nConfidence " + canvas.Confidence.ToString("0", CultureInfo.InvariantCulture) + "% · Confirm every orange point before continuing.";
            quality.BackColor = drift ? Color.FromArgb(255, 244, 214) : Color.FromArgb(232, 246, 226);
            quality.ForeColor = drift ? Color.FromArgb(128, 82, 12) : Color.FromArgb(46, 108, 55);
            if (pedalCycleMode && frames.Count == framePaths.Length)
            {
                Dictionary<string, int> positions = FindCrankPositions();
                int distinctPositions = new HashSet<int>(positions.Values).Count;
                if (distinctPositions < 4)
                {
                    quality.Text = "CYCLE COVERAGE: REVIEW\nTwo crank extremes landed on the same checkpoint. Add more evenly spaced frames or correct the foot-contact point before saving.";
                    quality.BackColor = Color.FromArgb(255, 244, 214);
                    quality.ForeColor = Color.FromArgb(128, 82, 12);
                }
            }
            previous.Enabled = frameIndex > 0;
            approveNext.Text = frameIndex == framePaths.Length - 1 ? "Approve Final Checkpoint" : "Approve & Track Next";
            finish.Enabled = frames.Count == framePaths.Length && frames.All(f => f.Approved);
            ranges.Text = BuildRangeText(false);
        }

        private void Finish_Click(object sender, EventArgs e)
        {
            SaveCanvasToCurrent(true);
            if (!frames.All(f => f.Approved))
            {
                MessageBox.Show(this, "Review and approve every checkpoint before saving the motion range.", pedalCycleMode ? "Pedal-Cycle Measurement Review" : "Short-Clip Tracking", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult confirm = MessageBox.Show(this,
                "Save this accepted " + side + " motion range to the client fit session?\n\n" + BuildRangeText(true) +
                "\nThe tracking is advisory; the fitter remains responsible for each corrected point.",
                pedalCycleMode ? "Save Pedal-Cycle Review" : "Save Short-Clip Tracking", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            Dictionary<string, List<double>> values = CollectValues();
            ResultValues = values.ToDictionary(p => p.Key, p => FormatAngle(p.Value.OrderBy(v => v).ElementAt(p.Value.Count / 2)));
            TrackingSummary = BuildRangeText(true).Replace("\n", "; ");
            EvidenceImagePath = SaveEvidenceStrip();
            if (pedalCycleMode)
            {
                SmartFrameSummary = BuildSmartFrameSuggestionText().Replace("\n", "; ");
                SmartFrameEvidencePath = SaveSmartFrameEvidence();
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private string BuildRangeText(bool includeCounts)
        {
            Dictionary<string, List<double>> values = CollectValues();
            string text = "ACCEPTED MOTION RANGE\n" + RangeLine("Knee", values, "KneeAngle") +
                RangeLine("Hip", values, "HipAngle") + RangeLine("Ankle", values, "AnkleAngle") +
                RangeLine("Body reach", values, "TorsoAngle") + RangeLine("Back", values, "ShoulderAngle");
            if (pedalCycleMode && frames.Count == framePaths.Length)
                text += BuildCrankPositionText() + BuildSmartFrameSuggestionText();
            if (includeCounts)
                text += frames.Count + " checkpoints · " + correctionCount + " manual corrections\n";
            return text;
        }

        private string BuildCrankPositionText()
        {
            Dictionary<string, int> positions = FindCrankPositions();
            string text = "\nCRANK POSITION CHECKPOINTS\n";
            foreach (string position in new[] { "Top", "Bottom", "Front", "Rear" })
            {
                int index = positions[position];
                Dictionary<string, double> pose = ShortClipTrackingCanvas.Calculate(frames[index].Points);
                text += position.PadRight(9) + "#" + (index + 1).ToString(CultureInfo.InvariantCulture) +
                    " · knee " + FormatAngle(pose["KneeAngle"]) +
                    " · hip " + FormatAngle(pose["HipAngle"]) +
                    " · ankle " + FormatAngle(pose["AnkleAngle"]) + "\n";
            }
            return text;
        }

        private Dictionary<string, int> FindCrankPositions()
        {
            Dictionary<string, int> positions = new Dictionary<string, int>();
            bool facingRight = frames[0].Points[6].X >= frames[0].Points[4].X;
            positions["Top"] = IndexOfExtreme(false, false);
            positions["Bottom"] = IndexOfExtreme(false, true);
            positions["Front"] = IndexOfExtreme(true, facingRight);
            positions["Rear"] = IndexOfExtreme(true, !facingRight);
            return positions;
        }

        private string BuildSmartFrameSuggestionText()
        {
            if (!pedalCycleMode || frames.Count != framePaths.Length)
                return string.Empty;
            List<SmartFrameSuggestion> suggestions = FindSmartFrameSuggestions();
            string text = "\nSMART MEASUREMENT FRAMES\n";
            foreach (SmartFrameSuggestion suggestion in suggestions)
            {
                text += suggestion.Label.PadRight(16) + "#" + (suggestion.FrameIndex + 1).ToString(CultureInfo.InvariantCulture) +
                    " · " + FormatAngle(suggestion.Angle) + " · clarity " + suggestion.Clarity.ToString("0", CultureInfo.InvariantCulture) + "/100\n";
            }
            text += "Suggestions are starting points—confirm the frame and measurement in Kinovea.\n";
            return text;
        }

        private List<SmartFrameSuggestion> FindSmartFrameSuggestions()
        {
            List<SmartFrameCandidate> candidates = new List<SmartFrameCandidate>();
            for (int i = 0; i < frames.Count; i++)
            {
                Dictionary<string, double> pose = ShortClipTrackingCanvas.Calculate(frames[i].Points);
                TrackingImageQuality qualityCheck;
                if (!frameQualityCache.TryGetValue(frames[i].Path, out qualityCheck))
                {
                    qualityCheck = TrackingImageQuality.Analyze(frames[i].Path, "Checkpoint " + (i + 1));
                    frameQualityCache[frames[i].Path] = qualityCheck;
                }
                double lightPenalty = Math.Abs(qualityCheck.MeanBrightness - 140.0) * 0.18;
                double score = 38.0 + frames[i].Confidence * 0.35 + Math.Min(qualityCheck.EdgeStrength, 25.0) * 1.6 -
                    lightPenalty - qualityCheck.Warnings.Count * 4.0;
                candidates.Add(new SmartFrameCandidate(i, pose, Math.Max(0, Math.Min(100, score))));
            }

            Dictionary<string, int> crank = FindCrankPositions();
            double medianTorso = Median(candidates.Select(c => c.Pose["TorsoAngle"]));
            double medianBack = Median(candidates.Select(c => c.Pose["ShoulderAngle"]));
            SmartFrameCandidate knee = Clearest(candidates.OrderByDescending(c => c.Pose["KneeAngle"]).Take(3));
            SmartFrameCandidate hip = Clearest(candidates.OrderBy(c => c.Pose["HipAngle"]).Take(3));
            SmartFrameCandidate ankle = Clearest(Neighborhood(candidates, crank["Bottom"]));
            SmartFrameCandidate reach = Clearest(candidates.OrderBy(c => Math.Abs(c.Pose["TorsoAngle"] - medianTorso)).Take(3));
            SmartFrameCandidate back = Clearest(candidates.OrderBy(c => Math.Abs(c.Pose["ShoulderAngle"] - medianBack)).Take(3));

            return new List<SmartFrameSuggestion>
            {
                new SmartFrameSuggestion("Knee extension", knee.Index, knee.Pose["KneeAngle"], knee.Clarity),
                new SmartFrameSuggestion("Hip closure", hip.Index, hip.Pose["HipAngle"], hip.Clarity),
                new SmartFrameSuggestion("Ankle position", ankle.Index, ankle.Pose["AnkleAngle"], ankle.Clarity),
                new SmartFrameSuggestion("Body reach", reach.Index, reach.Pose["TorsoAngle"], reach.Clarity),
                new SmartFrameSuggestion("Back angle", back.Index, back.Pose["ShoulderAngle"], back.Clarity)
            };
        }

        private static IEnumerable<SmartFrameCandidate> Neighborhood(List<SmartFrameCandidate> candidates, int center)
        {
            int count = candidates.Count;
            return new[] { candidates[(center - 1 + count) % count], candidates[center], candidates[(center + 1) % count] };
        }

        private static SmartFrameCandidate Clearest(IEnumerable<SmartFrameCandidate> candidates)
        {
            return candidates.OrderByDescending(c => c.Clarity).First();
        }

        private static double Median(IEnumerable<double> source)
        {
            double[] values = source.OrderBy(v => v).ToArray();
            return values[values.Length / 2];
        }

        private int IndexOfExtreme(bool horizontal, bool maximum)
        {
            int selected = 0;
            float selectedValue = horizontal ? frames[0].Points[3].X : frames[0].Points[3].Y;
            for (int i = 1; i < frames.Count; i++)
            {
                float value = horizontal ? frames[i].Points[3].X : frames[i].Points[3].Y;
                if ((maximum && value > selectedValue) || (!maximum && value < selectedValue))
                {
                    selected = i;
                    selectedValue = value;
                }
            }
            return selected;
        }

        private Dictionary<string, List<double>> CollectValues()
        {
            Dictionary<string, List<double>> values = new Dictionary<string, List<double>>();
            foreach (string key in new[] { "KneeAngle", "HipAngle", "AnkleAngle", "TorsoAngle", "ShoulderAngle" })
                values[key] = new List<double>();
            foreach (TrackedFrame frame in frames)
            {
                Dictionary<string, double> pose = ShortClipTrackingCanvas.Calculate(frame.Points);
                foreach (string key in values.Keys.ToArray())
                    values[key].Add(pose[key]);
            }
            return values;
        }

        private static string RangeLine(string label, Dictionary<string, List<double>> values, string key)
        {
            if (!values.ContainsKey(key) || values[key].Count == 0)
                return label.PadRight(16) + "--\n";
            return label.PadRight(16) + FormatAngle(values[key].Min()) + " – " + FormatAngle(values[key].Max()) + "\n";
        }

        private string SaveEvidenceStrip()
        {
            Directory.CreateDirectory(outputDirectory);
            string path = Path.Combine(outputDirectory, side + (pedalCycleMode ? "-Pedal-Cycle-Review-" : "-Short-Clip-Tracking-") + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".png");
            Dictionary<string, int> positions = pedalCycleMode ? FindCrankPositions() : null;
            int[] selected = pedalCycleMode
                ? new[] { positions["Top"], positions["Bottom"], positions["Front"], positions["Rear"] }
                : new[] { 0, frames.Count / 2, frames.Count - 1 };
            int columns = pedalCycleMode ? 2 : 3;
            using (Bitmap output = new Bitmap(1800, pedalCycleMode ? 1400 : 720))
            using (Graphics graphics = Graphics.FromImage(output))
            using (Font title = new Font("Segoe UI", 22F, FontStyle.Bold))
            using (Brush white = new SolidBrush(Color.White))
            {
                graphics.Clear(Color.FromArgb(20, 27, 24));
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.DrawString(side.ToUpperInvariant() + (pedalCycleMode ? " PEDAL-CYCLE MEASUREMENT REVIEW" : " SHORT-CLIP RIDER TRACKING"), title, white, 24, 18);
                for (int i = 0; i < selected.Length; i++)
                {
                    TrackedFrame frame = frames[selected[i]];
                    using (Image image = Image.FromFile(frame.Path))
                    {
                        float width = pedalCycleMode ? 870 : 570;
                        float x = 20 + (i % columns) * (pedalCycleMode ? 890 : 595);
                        float y = 70 + (i / columns) * 545;
                        string label = pedalCycleMode ? new[] { "TOP", "BOTTOM", "FRONT", "REAR" }[i] + " · CHECKPOINT " + (selected[i] + 1) : "CHECKPOINT " + (selected[i] + 1);
                        ShortClipTrackingCanvas.RenderPose(graphics, image, frame.Points, new RectangleF(x, y, width, 520), label);
                    }
                }
                float summaryY = pedalCycleMode ? 1160 : 625;
                graphics.DrawString(BuildRangeText(true), new Font("Segoe UI", 10F, FontStyle.Bold), white, new RectangleF(28, summaryY, 1740, pedalCycleMode ? 225 : 85));
                output.Save(path, ImageFormat.Png);
            }
            return path;
        }

        private string SaveSmartFrameEvidence()
        {
            Directory.CreateDirectory(outputDirectory);
            string path = Path.Combine(outputDirectory, side + "-Smart-Measurement-Frames-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".png");
            List<SmartFrameSuggestion> suggestions = FindSmartFrameSuggestions();
            using (Bitmap output = new Bitmap(1800, 1780))
            using (Graphics graphics = Graphics.FromImage(output))
            using (Font title = new Font("Segoe UI", 22F, FontStyle.Bold))
            using (Font note = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (Brush white = new SolidBrush(Color.White))
            {
                graphics.Clear(Color.FromArgb(20, 27, 24));
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.DrawString(side.ToUpperInvariant() + " SMART MEASUREMENT CAPTURE", title, white, 24, 18);
                for (int i = 0; i < suggestions.Count; i++)
                {
                    SmartFrameSuggestion suggestion = suggestions[i];
                    TrackedFrame frame = frames[suggestion.FrameIndex];
                    using (Image image = Image.FromFile(frame.Path))
                    {
                        float x = 20 + (i % 2) * 890;
                        float y = 70 + (i / 2) * 520;
                        string label = suggestion.Label.ToUpperInvariant() + " · CHECKPOINT " + (suggestion.FrameIndex + 1) +
                            " · " + FormatAngle(suggestion.Angle) + " · CLARITY " + suggestion.Clarity.ToString("0", CultureInfo.InvariantCulture) + "/100";
                        ShortClipTrackingCanvas.RenderPose(graphics, image, frame.Points, new RectangleF(x, y, 870, 490), label);
                    }
                }
                graphics.DrawString("Frame suggestions are advisory. Confirm each pose and final measurement with Kinovea playback and drawing tools.", note, white, new RectangleF(28, 1650, 1740, 80));
                output.Save(path, ImageFormat.Png);
            }
            return path;
        }

        private static Label NewLabel(string text, float size, bool bold)
        {
            Label label = new Label(); label.Text = text; label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular); return label;
        }

        private static void ConfigureButton(Button button, string text, bool primary)
        {
            button.Text = text; button.Dock = DockStyle.Top; button.Height = 42; button.FlatStyle = FlatStyle.Flat;
            button.BackColor = primary ? Color.FromArgb(184, 243, 74) : Color.White;
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(85, 122, 18) : Color.FromArgb(184, 193, 188);
        }

        private static string FormatAngle(double value) { return value.ToString("0.0", CultureInfo.InvariantCulture) + "°"; }
    }

    internal class TrackedFrame
    {
        public string Path; public List<PointF> Points; public double Confidence; public bool Approved;
        public TrackedFrame(string path, IEnumerable<PointF> points, double confidence, bool approved)
        { Path = path; Points = points.ToList(); Confidence = confidence; Approved = approved; }
    }

    internal class SmartFrameCandidate
    {
        public int Index; public Dictionary<string, double> Pose; public double Clarity;
        public SmartFrameCandidate(int index, Dictionary<string, double> pose, double clarity)
        { Index = index; Pose = pose; Clarity = clarity; }
    }

    internal class SmartFrameSuggestion
    {
        public string Label; public int FrameIndex; public double Angle; public double Clarity;
        public SmartFrameSuggestion(string label, int frameIndex, double angle, double clarity)
        { Label = label; FrameIndex = frameIndex; Angle = angle; Clarity = clarity; }
    }

    internal class TrackResult
    {
        public List<PointF> Points = new List<PointF>();
        public double Confidence;
    }

    internal static class ShortClipPointTracker
    {
        public static TrackResult Track(string previousPath, string nextPath, IList<PointF> seeds)
        {
            using (Bitmap previous = new Bitmap(previousPath))
            using (Bitmap next = new Bitmap(nextPath))
            {
                float sx = (float)next.Width / previous.Width;
                float sy = (float)next.Height / previous.Height;
                int patchRadius = Math.Max(4, Math.Min(previous.Width, previous.Height) / 90);
                int searchRadius = Math.Max(18, Math.Min(previous.Width, previous.Height) / 14);
                List<double> scores = new List<double>();
                TrackResult result = new TrackResult();
                foreach (PointF seed in seeds)
                {
                    PointF scaled = new PointF(seed.X * sx, seed.Y * sy);
                    double best = double.MaxValue;
                    Point bestPoint = Point.Round(scaled);
                    for (int y = Math.Max(patchRadius, bestPoint.Y - searchRadius); y <= Math.Min(next.Height - patchRadius - 1, bestPoint.Y + searchRadius); y += 4)
                    for (int x = Math.Max(patchRadius, bestPoint.X - searchRadius); x <= Math.Min(next.Width - patchRadius - 1, bestPoint.X + searchRadius); x += 4)
                    {
                        double score = PatchDifference(previous, next, seed, new PointF(x, y), patchRadius, sx, sy);
                        if (score < best) { best = score; bestPoint = new Point(x, y); }
                    }
                    result.Points.Add(bestPoint);
                    scores.Add(Math.Max(0, 100 - best * 1.5));
                }
                result.Confidence = scores.Count == 0 ? 0 : scores.Average();
                return result;
            }
        }

        private static double PatchDifference(Bitmap previous, Bitmap next, PointF a, PointF b, int radius, float sx, float sy)
        {
            double total = 0; int count = 0;
            for (int dy = -radius; dy <= radius; dy += 3)
            for (int dx = -radius; dx <= radius; dx += 3)
            {
                int ax = Clamp((int)a.X + dx, 0, previous.Width - 1);
                int ay = Clamp((int)a.Y + dy, 0, previous.Height - 1);
                int bx = Clamp((int)b.X + (int)(dx * sx), 0, next.Width - 1);
                int by = Clamp((int)b.Y + (int)(dy * sy), 0, next.Height - 1);
                Color ca = previous.GetPixel(ax, ay); Color cb = next.GetPixel(bx, by);
                total += Math.Abs(ca.R - cb.R) + Math.Abs(ca.G - cb.G) + Math.Abs(ca.B - cb.B); count += 3;
            }
            return count == 0 ? 765 : total / count;
        }

        private static int Clamp(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
    }

    internal class ShortClipTrackingCanvas : Control
    {
        private readonly List<PointF> points = new List<PointF>();
        private Image image;
        private int dragIndex = -1;
        private bool facingRight = true;
        private bool correctedThisDrag;
        public event EventHandler PoseCorrected;
        public double Confidence { get; set; }
        public List<PointF> Points { get { return points.ToList(); } }

        public ShortClipTrackingCanvas()
        {
            BackColor = Color.FromArgb(13, 19, 17);
            DoubleBuffered = true;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += delegate
            {
                dragIndex = -1;
                if (correctedThisDrag && PoseCorrected != null)
                    PoseCorrected(this, EventArgs.Empty);
                correctedThisDrag = false;
            };
        }

        public void LoadFrame(string path, IEnumerable<PointF> trackedPoints)
        {
            if (image != null) image.Dispose();
            image = Image.FromFile(path);
            points.Clear();
            if (trackedPoints != null) points.AddRange(trackedPoints);
            Invalidate();
        }

        public void Suggest()
        {
            if (image == null) return;
            float left = image.Width * 0.22F, top = image.Height * 0.12F, width = image.Width * 0.58F, height = image.Height * 0.78F;
            points.Clear();
            float direction = facingRight ? 1F : -1F;
            float center = left + width * 0.5F;
            Func<float, float> x = p => center + direction * (p - 0.5F) * width;
            points.Add(new PointF(x(.47F), top + height * .53F));
            points.Add(new PointF(x(.60F), top + height * .70F));
            points.Add(new PointF(x(.45F), top + height * .88F));
            points.Add(new PointF(x(.68F), top + height * .91F));
            points.Add(new PointF(x(.42F), top + height * .30F));
            points.Add(new PointF(x(.62F), top + height * .40F));
            points.Add(new PointF(x(.78F), top + height * .45F));
            points.Add(new PointF(x(.38F), top + height * .16F));
            Confidence = 60;
            Invalidate();
        }

        public void Flip() { facingRight = !facingRight; Suggest(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing && image != null)
            {
                image.Dispose();
                image = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (image == null) return;
            RenderPose(e.Graphics, image, points, ClientRectangle, string.Empty);
        }

        public static void RenderPose(Graphics graphics, Image image, IList<PointF> pose, RectangleF bounds, string label)
        {
            RectangleF fitted = Fit(bounds, image.Width, image.Height);
            graphics.DrawImage(image, fitted);
            using (Pen line = new Pen(Color.FromArgb(220, 184, 243, 74), 3F))
            using (Brush dot = new SolidBrush(Color.FromArgb(242, 126, 44)))
            using (Brush white = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                int[,] links = { {0,1},{1,2},{2,3},{0,4},{4,5},{5,6},{4,7} };
                for (int i = 0; i < links.GetLength(0); i++) if (pose.Count > links[i,0] && pose.Count > links[i,1])
                    graphics.DrawLine(line, Map(pose[links[i,0]], fitted, image), Map(pose[links[i,1]], fitted, image));
                for (int i = 0; i < pose.Count; i++) { PointF p = Map(pose[i], fitted, image); graphics.FillEllipse(dot, p.X - 7, p.Y - 7, 14, 14); graphics.DrawString((i + 1).ToString(), font, white, p.X + 9, p.Y - 10); }
                if (!string.IsNullOrEmpty(label)) graphics.DrawString(label, font, white, fitted.X + 8, fitted.Y + 8);
            }
        }

        public static Dictionary<string, double> Calculate(IList<PointF> p)
        {
            return new Dictionary<string, double> { {"KneeAngle", Angle(p[0],p[1],p[2])}, {"HipAngle",Angle(p[4],p[0],p[1])}, {"AnkleAngle",Angle(p[1],p[2],p[3])}, {"TorsoAngle",Angle(p[0],p[4],p[6])}, {"ShoulderAngle",LineAngle(p[0],p[4])} };
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (image == null || e.Button != MouseButtons.Left) return;
            PointF p; if (!TryImagePoint(e.Location, out p)) return;
            double threshold = Math.Max(image.Width, image.Height) * .035;
            for (int i = points.Count - 1; i >= 0; i--) if (Distance(points[i], p) < threshold) { dragIndex = i; correctedThisDrag = false; break; }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (dragIndex < 0 || e.Button != MouseButtons.Left) return;
            PointF p; if (!TryImagePoint(e.Location, out p)) return;
            points[dragIndex] = p; Confidence = Math.Max(Confidence, 70); correctedThisDrag = true; Invalidate();
        }

        private bool TryImagePoint(Point location, out PointF point)
        {
            RectangleF fitted = Fit(ClientRectangle, image.Width, image.Height);
            if (!fitted.Contains(location)) { point = PointF.Empty; return false; }
            point = new PointF((location.X - fitted.X) * image.Width / fitted.Width, (location.Y - fitted.Y) * image.Height / fitted.Height); return true;
        }

        private static RectangleF Fit(RectangleF bounds, int width, int height)
        {
            float scale = Math.Min(bounds.Width / width, bounds.Height / height);
            float w = width * scale, h = height * scale;
            return new RectangleF(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
        }
        private static PointF Map(PointF p, RectangleF r, Image image) { return new PointF(r.X + p.X * r.Width / image.Width, r.Y + p.Y * r.Height / image.Height); }
        private static double Distance(PointF a, PointF b) { double x=a.X-b.X,y=a.Y-b.Y; return Math.Sqrt(x*x+y*y); }
        private static double Angle(PointF a, PointF v, PointF b) { double ax=a.X-v.X,ay=a.Y-v.Y,bx=b.X-v.X,by=b.Y-v.Y,d=Math.Sqrt(ax*ax+ay*ay)*Math.Sqrt(bx*bx+by*by); if(d<.0001)return 0; return Math.Acos(Math.Max(-1,Math.Min(1,(ax*bx+ay*by)/d)))*180/Math.PI; }
        private static double LineAngle(PointF a, PointF b) { double n=Math.Abs(Math.Atan2(b.Y-a.Y,b.X-a.X)*180/Math.PI); return n>90?180-n:n; }
    }
}
