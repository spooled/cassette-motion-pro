/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class TrackingQualityReviewForm : Form
    {
        private readonly TrackingImageQuality before;
        private readonly TrackingImageQuality after;
        private readonly CheckBox jointsConfirmed = new CheckBox();
        private readonly CheckBox cameraConfirmed = new CheckBox();
        private readonly Label overall = new Label();

        public string ReviewSummary { get; private set; }

        public TrackingQualityReviewForm(string beforePath, string afterPath)
        {
            if (!File.Exists(beforePath) || !File.Exists(afterPath))
                throw new FileNotFoundException("Both Before and After reference images are required.");
            before = TrackingImageQuality.Analyze(beforePath, "Before");
            after = TrackingImageQuality.Analyze(afterPath, "After");
            Text = "Cassette Motion Pro - Tracking Quality and Camera Guidance";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ClientSize = new Size(1180, 780);
            MinimumSize = new Size(980, 680);
            StartPosition = FormStartPosition.CenterParent;
            BuildInterface();
            UpdateOverall();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 185));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(20, 27, 24);
            header.Controls.Add(LabelAt("Tracking Quality and Camera Guidance", 22, 12, 18F, true, Color.White));
            header.Controls.Add(LabelAt("Automatic checks are advisory. Confirm joint visibility and camera placement before tracking.", 24, 49, 9F, false, Color.FromArgb(205, 216, 210)));
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);
            root.Controls.Add(BuildImageCard(before), 0, 1);
            root.Controls.Add(BuildImageCard(after), 1, 1);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Fill;
            footer.BackColor = Color.White;
            overall.Location = new Point(18, 14);
            overall.Size = new Size(1110, 62);
            overall.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            overall.Padding = new Padding(10);
            footer.Controls.Add(overall);

            jointsConfirmed.Text = "All eight rider joints are clearly visible in both images";
            jointsConfirmed.Location = new Point(24, 84);
            jointsConfirmed.Size = new Size(620, 26);
            jointsConfirmed.CheckedChanged += delegate { UpdateOverall(); };
            footer.Controls.Add(jointsConfirmed);
            cameraConfirmed.Text = "Camera is near hip height, level, and square to the bicycle";
            cameraConfirmed.Location = new Point(24, 112);
            cameraConfirmed.Size = new Size(620, 26);
            cameraConfirmed.CheckedChanged += delegate { UpdateOverall(); };
            footer.Controls.Add(cameraConfirmed);

            Button save = NewButton("Save Quality Review", true);
            save.Location = new Point(815, 94);
            save.Click += Save_Click;
            footer.Controls.Add(save);
            Button cancel = NewButton("Cancel", false);
            cancel.Location = new Point(995, 94);
            cancel.Click += delegate { Close(); };
            footer.Controls.Add(cancel);
            root.Controls.Add(footer, 0, 2);
            root.SetColumnSpan(footer, 2);
            Controls.Add(root);
        }

        private Control BuildImageCard(TrackingImageQuality result)
        {
            TableLayoutPanel card = new TableLayoutPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(12);
            card.Padding = new Padding(12);
            card.BackColor = Color.White;
            card.ColumnCount = 1;
            card.RowCount = 2;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
            PictureBox picture = new PictureBox();
            picture.Dock = DockStyle.Fill;
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            picture.BackColor = Color.FromArgb(13, 19, 17);
            picture.Image = Image.FromFile(result.Path);
            picture.Disposed += delegate { if (picture.Image != null) picture.Image.Dispose(); };
            card.Controls.Add(picture, 0, 0);
            Label details = new Label();
            details.Dock = DockStyle.Fill;
            details.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            details.Padding = new Padding(8);
            details.Text = result.BuildDisplayText();
            details.BackColor = result.Warnings.Count == 0 ? Color.FromArgb(232, 246, 226) : Color.FromArgb(255, 244, 214);
            details.ForeColor = result.Warnings.Count == 0 ? Color.FromArgb(46, 108, 55) : Color.FromArgb(128, 82, 12);
            card.Controls.Add(details, 0, 1);
            return card;
        }

        private void UpdateOverall()
        {
            List<string> warnings = CombinedWarnings();
            if (!jointsConfirmed.Checked)
                warnings.Add("joint visibility not confirmed");
            if (!cameraConfirmed.Checked)
                warnings.Add("camera placement not confirmed");
            overall.Text = warnings.Count == 0
                ? "READY FOR TRACKING · Lighting, sharpness, camera, framing, and joint visibility are confirmed."
                : "REVIEW BEFORE TRACKING · " + warnings.Count.ToString(CultureInfo.InvariantCulture) + " item(s) need attention.";
            overall.BackColor = warnings.Count == 0 ? Color.FromArgb(232, 246, 226) : Color.FromArgb(255, 244, 214);
            overall.ForeColor = warnings.Count == 0 ? Color.FromArgb(46, 108, 55) : Color.FromArgb(128, 82, 12);
        }

        private List<string> CombinedWarnings()
        {
            List<string> warnings = new List<string>();
            warnings.AddRange(before.Warnings.Select(w => "Before: " + w));
            warnings.AddRange(after.Warnings.Select(w => "After: " + w));
            if (Math.Abs(before.SubjectScale - after.SubjectScale) > 0.12)
                warnings.Add("Before/After rider size differs");
            if (Math.Abs(before.SubjectCenterX - after.SubjectCenterX) > 0.10)
                warnings.Add("Before/After centering differs");
            if (Math.Abs(before.AspectRatio - after.AspectRatio) > 0.08)
                warnings.Add("Before/After image shape differs");
            return warnings;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            List<string> warnings = CombinedWarnings();
            if (!jointsConfirmed.Checked) warnings.Add("joint visibility not confirmed");
            if (!cameraConfirmed.Checked) warnings.Add("camera height/level not confirmed");
            ReviewSummary = "Before: " + before.CompactSummary() + "; After: " + after.CompactSummary() +
                "; framing scale difference " + Math.Abs(before.SubjectScale - after.SubjectScale).ToString("0%", CultureInfo.InvariantCulture) +
                "; " + (warnings.Count == 0 ? "Ready for tracking" : "Review advised - " + string.Join(", ", warnings.ToArray()));
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label LabelAt(string text, int x, int y, float size, bool bold, Color color)
        {
            Label label = new Label(); label.Text = text; label.Location = new Point(x, y); label.AutoSize = true;
            label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular); label.ForeColor = color; return label;
        }

        private static Button NewButton(string text, bool primary)
        {
            Button button = new Button(); button.Text = text; button.Size = new Size(primary ? 165 : 125, 40); button.FlatStyle = FlatStyle.Flat;
            button.BackColor = primary ? Color.FromArgb(184, 243, 74) : Color.White;
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(85, 122, 18) : Color.FromArgb(184, 193, 188); return button;
        }
    }

    internal class TrackingImageQuality
    {
        public string Path;
        public string Label;
        public int Width;
        public int Height;
        public double MeanBrightness;
        public double DarkFraction;
        public double BrightFraction;
        public double EdgeStrength;
        public double TiltDegrees;
        public double SubjectScale;
        public double SubjectCenterX;
        public double SubjectCenterY;
        public double AspectRatio;
        public List<string> LowVisibilityJoints = new List<string>();
        public List<string> Warnings = new List<string>();

        public static TrackingImageQuality Analyze(string path, string label)
        {
            TrackingImageQuality result = new TrackingImageQuality();
            result.Path = path; result.Label = label;
            using (Bitmap bitmap = new Bitmap(path))
            {
                result.Width = bitmap.Width; result.Height = bitmap.Height; result.AspectRatio = (double)bitmap.Width / bitmap.Height;
                int step = Math.Max(2, Math.Min(bitmap.Width, bitmap.Height) / 220);
                double brightness = 0, edges = 0; int count = 0, dark = 0, bright = 0;
                for (int y = step; y < bitmap.Height - step; y += step)
                for (int x = step; x < bitmap.Width - step; x += step)
                {
                    double value = Luma(bitmap.GetPixel(x, y));
                    brightness += value; count++;
                    if (value < 35) dark++;
                    if (value > 235) bright++;
                    edges += Math.Abs(value - Luma(bitmap.GetPixel(x + step, y))) + Math.Abs(value - Luma(bitmap.GetPixel(x, y + step)));
                }
                result.MeanBrightness = brightness / Math.Max(1, count);
                result.DarkFraction = (double)dark / Math.Max(1, count);
                result.BrightFraction = (double)bright / Math.Max(1, count);
                result.EdgeStrength = edges / Math.Max(1, count * 2);
                RectangleF subject = DetectSubject(bitmap, step);
                result.SubjectScale = subject.Height / bitmap.Height;
                result.SubjectCenterX = (subject.Left + subject.Width / 2) / bitmap.Width;
                result.SubjectCenterY = (subject.Top + subject.Height / 2) / bitmap.Height;
                result.TiltDegrees = EstimateTilt(bitmap, step);
                result.LowVisibilityJoints = FindLowVisibilityJoints(bitmap, subject);
            }
            if (result.MeanBrightness < 65 || result.DarkFraction > 0.35)
                result.Warnings.Add("poor lighting—add even side light");
            if (result.MeanBrightness > 220 || result.BrightFraction > 0.30)
                result.Warnings.Add("clipped highlights—reduce exposure/backlight");
            if (result.EdgeStrength < 11)
                result.Warnings.Add("possible motion blur—use more light or faster shutter");
            if (Math.Abs(result.TiltDegrees) > 3.0)
                result.Warnings.Add("camera tilt " + result.TiltDegrees.ToString("+0.0;-0.0", CultureInfo.InvariantCulture) + "°—level camera");
            if (result.SubjectScale < 0.48)
                result.Warnings.Add("rider too small—move closer");
            if (result.SubjectScale > 0.94)
                result.Warnings.Add("rider tightly cropped—leave room around joints");
            if (result.SubjectCenterY < 0.37 || result.SubjectCenterY > 0.68)
                result.Warnings.Add("camera height/framing may be off—center near hip");
            if (result.Width < 960 || result.Height < 540)
                result.Warnings.Add("low image resolution");
            if (result.LowVisibilityJoints.Count > 0)
                result.Warnings.Add("possible blocked/unclear joints: " + string.Join(", ", result.LowVisibilityJoints.ToArray()));
            return result;
        }

        public string BuildDisplayText()
        {
            string text = Label.ToUpperInvariant() + " QUALITY: " + (Warnings.Count == 0 ? "PASS" : "REVIEW") + "\n" +
                "Light " + MeanBrightness.ToString("0", CultureInfo.InvariantCulture) + "/255 · Sharpness " + EdgeStrength.ToString("0.0", CultureInfo.InvariantCulture) +
                " · Tilt " + TiltDegrees.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "°\n" +
                "Rider scale " + SubjectScale.ToString("0%", CultureInfo.InvariantCulture) + " · " + Width + " × " + Height;
            if (Warnings.Count > 0) text += "\n• " + string.Join("\n• ", Warnings.ToArray());
            return text;
        }

        public string CompactSummary()
        {
            return "light " + MeanBrightness.ToString("0", CultureInfo.InvariantCulture) + "/255, sharpness " + EdgeStrength.ToString("0.0", CultureInfo.InvariantCulture) +
                ", tilt " + TiltDegrees.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "°, rider scale " + SubjectScale.ToString("0%", CultureInfo.InvariantCulture);
        }

        private static RectangleF DetectSubject(Bitmap bitmap, int step)
        {
            Color background = AverageCorners(bitmap);
            int minX = bitmap.Width, minY = bitmap.Height, maxX = 0, maxY = 0, hits = 0;
            for (int y = 0; y < bitmap.Height; y += step)
            for (int x = 0; x < bitmap.Width; x += step)
            {
                Color color = bitmap.GetPixel(x, y);
                int difference = Math.Abs(color.R - background.R) + Math.Abs(color.G - background.G) + Math.Abs(color.B - background.B);
                if (difference < 70) continue;
                hits++; minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
            if (hits < 40 || maxX <= minX || maxY <= minY)
                return new RectangleF(bitmap.Width * .18F, bitmap.Height * .08F, bitmap.Width * .64F, bitmap.Height * .84F);
            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }

        private static double EstimateTilt(Bitmap bitmap, int step)
        {
            List<PointF> edgePoints = new List<PointF>();
            int columnStep = Math.Max(step * 3, bitmap.Width / 80);
            for (int x = columnStep; x < bitmap.Width - columnStep; x += columnStep)
            {
                double strongest = 0; int bestY = bitmap.Height * 3 / 4;
                for (int y = bitmap.Height / 2; y < bitmap.Height - step; y += step)
                {
                    double edge = Math.Abs(Luma(bitmap.GetPixel(x, y)) - Luma(bitmap.GetPixel(x, y + step)));
                    if (edge > strongest) { strongest = edge; bestY = y; }
                }
                if (strongest > 28) edgePoints.Add(new PointF(x, bestY));
            }
            if (edgePoints.Count < 8) return 0;
            double meanX = edgePoints.Average(p => p.X), meanY = edgePoints.Average(p => p.Y), numerator = 0, denominator = 0;
            foreach (PointF point in edgePoints) { numerator += (point.X - meanX) * (point.Y - meanY); denominator += (point.X - meanX) * (point.X - meanX); }
            return Math.Atan2(numerator, Math.Max(1, denominator)) * 180.0 / Math.PI;
        }

        private static List<string> FindLowVisibilityJoints(Bitmap bitmap, RectangleF subject)
        {
            string[] names = { "hip", "knee", "ankle", "forefoot", "shoulder", "elbow", "wrist", "ear" };
            PointF[] normalized =
            {
                new PointF(.47F,.53F), new PointF(.60F,.70F), new PointF(.45F,.88F), new PointF(.68F,.91F),
                new PointF(.42F,.30F), new PointF(.62F,.40F), new PointF(.78F,.45F), new PointF(.38F,.16F)
            };
            List<string> unclear = new List<string>();
            int radius = Math.Max(5, Math.Min(bitmap.Width, bitmap.Height) / 80);
            for (int i = 0; i < normalized.Length; i++)
            {
                int cx = (int)(subject.Left + subject.Width * normalized[i].X);
                int cy = (int)(subject.Top + subject.Height * normalized[i].Y);
                if (cx - radius < 0 || cy - radius < 0 || cx + radius >= bitmap.Width || cy + radius >= bitmap.Height)
                {
                    unclear.Add(names[i]);
                    continue;
                }
                double sum = 0, square = 0; int count = 0;
                for (int y = cy - radius; y <= cy + radius; y += 3)
                for (int x = cx - radius; x <= cx + radius; x += 3)
                {
                    double value = Luma(bitmap.GetPixel(x, y));
                    sum += value; square += value * value; count++;
                }
                double mean = sum / Math.Max(1, count);
                double contrast = Math.Sqrt(Math.Max(0, square / Math.Max(1, count) - mean * mean));
                if (contrast < 7.5)
                    unclear.Add(names[i]);
            }
            return unclear;
        }

        private static Color AverageCorners(Bitmap bitmap)
        {
            int right = Math.Max(0, bitmap.Width - 3), bottom = Math.Max(0, bitmap.Height - 3);
            Color[] colors = { bitmap.GetPixel(Math.Min(2, right), Math.Min(2, bottom)), bitmap.GetPixel(right, Math.Min(2, bottom)), bitmap.GetPixel(Math.Min(2, right), bottom), bitmap.GetPixel(right, bottom) };
            return Color.FromArgb((int)colors.Average(c => c.R), (int)colors.Average(c => c.G), (int)colors.Average(c => c.B));
        }

        private static double Luma(Color color) { return color.R * .2126 + color.G * .7152 + color.B * .0722; }
    }
}
