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
    public class RiderTrackingComparisonForm : Form
    {
        private readonly RiderComparisonCanvas beforeCanvas;
        private readonly RiderComparisonCanvas afterCanvas;
        private readonly Label results = new Label();
        private readonly Label quality = new Label();
        private readonly CheckBox crankMatched = new CheckBox();
        private readonly string outputDirectory;

        public Dictionary<string, string> BeforeValues { get; private set; }
        public Dictionary<string, string> AfterValues { get; private set; }
        public string ComparisonImagePath { get; private set; }

        public RiderTrackingComparisonForm(string beforePath, string afterPath, string outputDirectory)
        {
            if (!File.Exists(beforePath) || !File.Exists(afterPath))
                throw new FileNotFoundException("Both Before and After rider images are required.");
            this.outputDirectory = outputDirectory;
            beforeCanvas = new RiderComparisonCanvas(beforePath, "BEFORE");
            afterCanvas = new RiderComparisonCanvas(afterPath, "AFTER");
            beforeCanvas.PoseChanged += delegate { UpdateComparison(); };
            afterCanvas.PoseChanged += delegate { UpdateComparison(); };

            Text = "Cassette Motion Pro - Before / After Rider Tracking";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ClientSize = new Size(1360, 820);
            MinimumSize = new Size(1080, 700);
            StartPosition = FormStartPosition.CenterParent;
            BuildInterface();
            UpdateComparison();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(20, 27, 24);
            Label title = new Label();
            title.Text = "Before / After Rider Tracking Comparison";
            title.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.Location = new Point(22, 13);
            title.AutoSize = true;
            header.Controls.Add(title);
            Label intro = new Label();
            intro.Text = "Drag every orange point onto the same anatomical landmark in both matched frames. Suggested points require fitter approval.";
            intro.ForeColor = Color.FromArgb(205, 216, 210);
            intro.Location = new Point(24, 47);
            intro.AutoSize = true;
            header.Controls.Add(intro);
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            beforeCanvas.Dock = DockStyle.Fill;
            afterCanvas.Dock = DockStyle.Fill;
            beforeCanvas.Margin = new Padding(12, 12, 6, 8);
            afterCanvas.Margin = new Padding(6, 12, 12, 8);
            root.Controls.Add(beforeCanvas, 0, 1);
            root.Controls.Add(afterCanvas, 1, 1);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Fill;
            footer.BackColor = Color.White;
            footer.Padding = new Padding(18);

            results.Location = new Point(20, 16);
            results.Size = new Size(610, 145);
            results.Font = new Font("Consolas", 10F, FontStyle.Bold);
            footer.Controls.Add(results);

            quality.Location = new Point(650, 16);
            quality.Size = new Size(680, 88);
            quality.Padding = new Padding(10);
            footer.Controls.Add(quality);

            crankMatched.Text = "I confirmed both frames use the same crank position";
            crankMatched.Location = new Point(660, 112);
            crankMatched.Size = new Size(360, 28);
            crankMatched.CheckedChanged += delegate { UpdateComparison(); };
            footer.Controls.Add(crankMatched);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Location = new Point(18, 168);
            actions.Size = new Size(1315, 46);
            actions.WrapContents = false;
            actions.Controls.Add(ActionButton("Suggest Both Again", delegate { beforeCanvas.Suggest(); afterCanvas.Suggest(); }));
            actions.Controls.Add(ActionButton("Flip Before", delegate { beforeCanvas.Flip(); }));
            actions.Controls.Add(ActionButton("Flip After", delegate { afterCanvas.Flip(); }));
            actions.Controls.Add(ActionButton("Save Approved Comparison", SaveComparison));
            actions.Controls.Add(ActionButton("Cancel", delegate { Close(); }));
            footer.Controls.Add(actions);

            root.Controls.Add(footer, 0, 2);
            root.SetColumnSpan(footer, 2);
            Controls.Add(root);
        }

        private Button ActionButton(string text, Action action)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.Height = 38;
            button.Padding = new Padding(12, 0, 12, 0);
            button.Click += delegate { action(); };
            return button;
        }

        private void UpdateComparison()
        {
            Dictionary<string, double> before = beforeCanvas.Values;
            Dictionary<string, double> after = afterCanvas.Values;
            results.Text =
                "ANGLE                     BEFORE       AFTER        CHANGE\n" +
                ComparisonLine("Knee", before, after, "KneeAngle") +
                ComparisonLine("Hip", before, after, "HipAngle") +
                ComparisonLine("Ankle", before, after, "AnkleAngle") +
                ComparisonLine("Body reach", before, after, "TorsoAngle") +
                ComparisonLine("Back", before, after, "ShoulderAngle");

            List<string> warnings = new List<string>();
            double aspectDifference = Math.Abs(beforeCanvas.ImageAspect - afterCanvas.ImageAspect);
            double scaleDifference = Math.Abs(beforeCanvas.SubjectScale - afterCanvas.SubjectScale);
            if (aspectDifference > 0.08)
                warnings.Add("Before and After image shapes do not closely match");
            if (scaleDifference > 0.12)
                warnings.Add("Rider scale/framing differs between the two images");
            if (!crankMatched.Checked)
                warnings.Add("Confirm that both images use the same crank position");
            if (beforeCanvas.Confidence < 45 || afterCanvas.Confidence < 45)
                warnings.Add("One automatic subject estimate has low confidence");

            quality.Text = warnings.Count == 0
                ? "COMPARISON QUALITY: READY\nFraming and crank-position checks are confirmed. Review every joint before saving."
                : "COMPARISON QUALITY: REVIEW\n• " + string.Join("\n• ", warnings.ToArray());
            quality.BackColor = warnings.Count == 0 ? Color.FromArgb(232, 246, 226) : Color.FromArgb(255, 244, 214);
            quality.ForeColor = warnings.Count == 0 ? Color.FromArgb(46, 108, 55) : Color.FromArgb(128, 82, 12);
        }

        private static string ComparisonLine(string label, Dictionary<string, double> before, Dictionary<string, double> after, string key)
        {
            double beforeValue = before[key];
            double afterValue = after[key];
            string change = (afterValue - beforeValue).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "°";
            return label.PadRight(26) + FormatAngle(beforeValue).PadRight(13) + FormatAngle(afterValue).PadRight(13) + change + "\n";
        }

        private void SaveComparison()
        {
            DialogResult confirm = MessageBox.Show(this,
                "Save these approved Before and After measurements and create one annotated comparison image?\n\n" +
                quality.Text + "\n\nThe fitter remains responsible for confirming every suggested joint point.",
                "Save Rider Comparison", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            Directory.CreateDirectory(outputDirectory);
            ComparisonImagePath = Path.Combine(outputDirectory, "Before-After-Rider-Comparison-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".png");
            using (Bitmap comparison = new Bitmap(1920, 820))
            using (Graphics graphics = Graphics.FromImage(comparison))
            using (Font titleFont = new Font("Segoe UI", 24F, FontStyle.Bold))
            using (Font resultFont = new Font("Segoe UI", 15F, FontStyle.Bold))
            using (Brush white = new SolidBrush(Color.White))
            using (Brush muted = new SolidBrush(Color.FromArgb(205, 216, 210)))
            {
                graphics.Clear(Color.FromArgb(20, 27, 24));
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                beforeCanvas.Render(graphics, new RectangleF(20, 60, 930, 610));
                afterCanvas.Render(graphics, new RectangleF(970, 60, 930, 610));
                graphics.DrawString("BEFORE", titleFont, white, 24, 18);
                graphics.DrawString("AFTER", titleFont, white, 974, 18);
                string summary = "Knee " + ChangeText("KneeAngle") + "   Hip " + ChangeText("HipAngle") + "   Ankle " + ChangeText("AnkleAngle") +
                    "   Body reach " + ChangeText("TorsoAngle") + "   Back " + ChangeText("ShoulderAngle");
                graphics.DrawString(summary, resultFont, white, new RectangleF(30, 700, 1860, 42));
                graphics.DrawString("Cassette Motion Pro · fitter-approved paused-frame comparison", resultFont, muted, new RectangleF(30, 758, 1860, 38));
                comparison.Save(ComparisonImagePath, ImageFormat.Png);
            }

            BeforeValues = FormatValues(beforeCanvas.Values);
            AfterValues = FormatValues(afterCanvas.Values);
            DialogResult = DialogResult.OK;
            Close();
        }

        private string ChangeText(string key)
        {
            double change = afterCanvas.Values[key] - beforeCanvas.Values[key];
            return change.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "°";
        }

        private static Dictionary<string, string> FormatValues(Dictionary<string, double> values)
        {
            return values.ToDictionary(pair => pair.Key, pair => FormatAngle(pair.Value));
        }

        private static string FormatAngle(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + "°";
        }
    }

    internal class RiderComparisonCanvas : Control
    {
        private readonly string[] landmarkNames = { "Hip", "Knee", "Ankle", "Forefoot", "Shoulder", "Elbow", "Wrist", "Ear" };
        private readonly Image image;
        private readonly List<PointF> points = new List<PointF>();
        private readonly string label;
        private int dragIndex = -1;
        private bool facingRight = true;
        private RectangleF subjectBounds;

        public event EventHandler PoseChanged;
        public double Confidence { get; private set; }
        public double ImageAspect { get { return (double)image.Width / image.Height; } }
        public double SubjectScale { get { return subjectBounds.Height / image.Height; } }
        public Dictionary<string, double> Values
        {
            get
            {
                return new Dictionary<string, double>
                {
                    { "KneeAngle", Angle(points[0], points[1], points[2]) },
                    { "HipAngle", Angle(points[4], points[0], points[1]) },
                    { "AnkleAngle", Angle(points[1], points[2], points[3]) },
                    { "TorsoAngle", Angle(points[0], points[4], points[6]) },
                    { "ShoulderAngle", LineAngleFromHorizontal(points[0], points[4]) }
                };
            }
        }

        public RiderComparisonCanvas(string imagePath, string label)
        {
            image = Image.FromFile(imagePath);
            this.label = label;
            BackColor = Color.FromArgb(13, 19, 17);
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            Suggest();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                image.Dispose();
            base.Dispose(disposing);
        }

        public void Suggest()
        {
            subjectBounds = DetectSubjectBounds();
            points.Clear();
            AddPosePoint(0.50F, 0.49F);
            AddPosePoint(0.57F, 0.68F);
            AddPosePoint(0.49F, 0.86F);
            AddPosePoint(0.64F, 0.88F);
            AddPosePoint(0.38F, 0.29F);
            AddPosePoint(0.52F, 0.35F);
            AddPosePoint(0.67F, 0.40F);
            AddPosePoint(0.34F, 0.18F);
            Invalidate();
            RaisePoseChanged();
        }

        public void Flip()
        {
            facingRight = !facingRight;
            Suggest();
        }

        public void Render(Graphics graphics, RectangleF destination)
        {
            RectangleF imageRect = FitRectangle(destination, image.Width, image.Height);
            graphics.DrawImage(image, imageRect);
            DrawPose(graphics, imageRect, Math.Max(3F, destination.Width / 310F), true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = FitRectangle(ClientRectangle, image.Width, image.Height);
            e.Graphics.DrawImage(image, rect);
            DrawPose(e.Graphics, rect, 3F, false);
            using (Brush background = new SolidBrush(Color.FromArgb(210, 20, 27, 24)))
            using (Brush foreground = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
            {
                e.Graphics.FillRectangle(background, rect.Left + 8, rect.Top + 8, 185, 28);
                e.Graphics.DrawString(label + " · confidence " + Confidence.ToString("0", CultureInfo.InvariantCulture) + "%", font, foreground, rect.Left + 14, rect.Top + 13);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            PointF imagePoint;
            if (!TryToImagePoint(e.Location, out imagePoint))
                return;
            dragIndex = FindPoint(imagePoint);
            Cursor = dragIndex >= 0 ? Cursors.SizeAll : Cursors.Cross;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragIndex < 0 || e.Button != MouseButtons.Left)
                return;
            PointF imagePoint;
            if (!TryToImagePoint(e.Location, out imagePoint))
                return;
            points[dragIndex] = imagePoint;
            Invalidate();
            RaisePoseChanged();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragIndex = -1;
            Cursor = Cursors.Cross;
            base.OnMouseUp(e);
        }

        private void DrawPose(Graphics graphics, RectangleF rect, float lineWidth, bool showNames)
        {
            using (Pen line = new Pen(Color.FromArgb(184, 243, 74), lineWidth))
            using (Brush pointBrush = new SolidBrush(Color.FromArgb(242, 126, 44)))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", showNames ? 11F : 9F, FontStyle.Bold))
            {
                DrawLine(graphics, line, rect, 0, 1); DrawLine(graphics, line, rect, 1, 2);
                DrawLine(graphics, line, rect, 2, 3); DrawLine(graphics, line, rect, 0, 4);
                DrawLine(graphics, line, rect, 4, 5); DrawLine(graphics, line, rect, 5, 6);
                DrawLine(graphics, line, rect, 4, 7);
                float radius = showNames ? 7F : 6F;
                for (int i = 0; i < points.Count; i++)
                {
                    PointF point = ToControlPoint(points[i], rect);
                    graphics.FillEllipse(pointBrush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
                    graphics.DrawString(showNames ? landmarkNames[i] : (i + 1).ToString(CultureInfo.InvariantCulture), font, textBrush, point.X + radius + 2, point.Y - radius);
                }
            }
        }

        private void DrawLine(Graphics graphics, Pen pen, RectangleF rect, int first, int second)
        {
            graphics.DrawLine(pen, ToControlPoint(points[first], rect), ToControlPoint(points[second], rect));
        }

        private void AddPosePoint(float x, float y)
        {
            float adjustedX = facingRight ? x : 1F - x;
            points.Add(new PointF(subjectBounds.Left + subjectBounds.Width * adjustedX, subjectBounds.Top + subjectBounds.Height * y));
        }

        private RectangleF DetectSubjectBounds()
        {
            using (Bitmap bitmap = new Bitmap(image))
            {
                Color background = AverageCorners(bitmap);
                int step = Math.Max(2, Math.Max(bitmap.Width, bitmap.Height) / 220);
                int minX = bitmap.Width, minY = bitmap.Height, maxX = 0, maxY = 0, samples = 0, different = 0;
                for (int y = step; y < bitmap.Height - step; y += step)
                {
                    for (int x = step; x < bitmap.Width - step; x += step)
                    {
                        samples++;
                        Color pixel = bitmap.GetPixel(x, y);
                        int difference = Math.Abs(pixel.R - background.R) + Math.Abs(pixel.G - background.G) + Math.Abs(pixel.B - background.B);
                        if (difference < 95)
                            continue;
                        different++;
                        minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                    }
                }
                RectangleF fallback = new RectangleF(image.Width * 0.16F, image.Height * 0.07F, image.Width * 0.68F, image.Height * 0.88F);
                if (different < 40 || maxX <= minX || maxY <= minY)
                {
                    Confidence = 25;
                    return fallback;
                }
                RectangleF detected = RectangleF.FromLTRB(minX, minY, maxX, maxY);
                float coverage = detected.Width * detected.Height / (image.Width * image.Height);
                if (coverage < 0.12F || coverage > 0.96F)
                {
                    Confidence = 35;
                    return fallback;
                }
                Confidence = Math.Max(40, Math.Min(78, 48 + different * 100.0 / Math.Max(1, samples)));
                return detected;
            }
        }

        private static Color AverageCorners(Bitmap bitmap)
        {
            int x = Math.Min(2, bitmap.Width - 1);
            int y = Math.Min(2, bitmap.Height - 1);
            int right = Math.Max(0, bitmap.Width - 1 - x);
            int bottom = Math.Max(0, bitmap.Height - 1 - y);
            Color[] samples = { bitmap.GetPixel(x, y), bitmap.GetPixel(right, y), bitmap.GetPixel(x, bottom), bitmap.GetPixel(right, bottom) };
            return Color.FromArgb((int)samples.Average(c => c.R), (int)samples.Average(c => c.G), (int)samples.Average(c => c.B));
        }

        private int FindPoint(PointF point)
        {
            double threshold = Math.Max(image.Width, image.Height) * 0.035;
            for (int i = points.Count - 1; i >= 0; i--)
                if (Distance(points[i], point) <= threshold)
                    return i;
            return -1;
        }

        private bool TryToImagePoint(Point point, out PointF imagePoint)
        {
            RectangleF rect = FitRectangle(ClientRectangle, image.Width, image.Height);
            if (!rect.Contains(point))
            {
                imagePoint = PointF.Empty;
                return false;
            }
            imagePoint = new PointF((point.X - rect.X) * image.Width / rect.Width, (point.Y - rect.Y) * image.Height / rect.Height);
            return true;
        }

        private PointF ToControlPoint(PointF point, RectangleF rect)
        {
            return new PointF(rect.X + point.X * rect.Width / image.Width, rect.Y + point.Y * rect.Height / image.Height);
        }

        private static RectangleF FitRectangle(RectangleF bounds, int width, int height)
        {
            float scale = Math.Min(bounds.Width / width, bounds.Height / height);
            float fittedWidth = width * scale;
            float fittedHeight = height * scale;
            return new RectangleF(bounds.X + (bounds.Width - fittedWidth) / 2F, bounds.Y + (bounds.Height - fittedHeight) / 2F, fittedWidth, fittedHeight);
        }

        private void RaisePoseChanged()
        {
            if (PoseChanged != null)
                PoseChanged(this, EventArgs.Empty);
        }

        private static double Angle(PointF a, PointF vertex, PointF b)
        {
            double ax = a.X - vertex.X, ay = a.Y - vertex.Y, bx = b.X - vertex.X, by = b.Y - vertex.Y;
            double denominator = Math.Sqrt(ax * ax + ay * ay) * Math.Sqrt(bx * bx + by * by);
            if (denominator <= 0.0001)
                return 0;
            double cosine = Math.Max(-1, Math.Min(1, (ax * bx + ay * by) / denominator));
            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        private static double LineAngleFromHorizontal(PointF a, PointF b)
        {
            double angle = Math.Abs(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180.0 / Math.PI);
            return angle > 90 ? 180 - angle : angle;
        }

        private static double Distance(PointF a, PointF b)
        {
            double x = a.X - b.X, y = a.Y - b.Y;
            return Math.Sqrt(x * x + y * y);
        }
    }
}
