/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class RiderBodyGuidedMeasurementForm : Form
    {
        private readonly string[] landmarkNames = new string[]
        {
            "Hip joint center",
            "Knee joint center",
            "Ankle joint center",
            "Toe / forefoot",
            "Shoulder joint center",
            "Elbow joint center",
            "Wrist / hand contact point",
            "Ear center"
        };

        private readonly List<PointF> points = new List<PointF>();
        private readonly string defaultSide;
        private readonly PictureBox picture = new PictureBox();
        private readonly Label progress = new Label();
        private readonly Label currentPoint = new Label();
        private readonly Label results = new Label();
        private readonly Label quality = new Label();
        private readonly Button undo = new Button();
        private readonly Button clear = new Button();
        private readonly Button autoSuggest = new Button();
        private readonly Button flipDirection = new Button();
        private readonly Button saveBefore = new Button();
        private readonly Button saveAfter = new Button();
        private readonly string imagePath;
        private Image image;
        private int dragIndex = -1;
        private bool automaticSuggestion;
        private bool facingRight = true;
        private double suggestionConfidence;
        private RectangleF detectedBounds;
        private Dictionary<string, string> calculatedValues = new Dictionary<string, string>();

        public Dictionary<string, string> ResultValues { get; private set; }
        public string ResultSide { get; private set; }
        public string AnnotatedImagePath { get; private set; }

        public RiderBodyGuidedMeasurementForm(string imagePath, string defaultSide)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException("The rider reference image could not be found.", imagePath);

            this.defaultSide = string.Equals(defaultSide, "Before", StringComparison.OrdinalIgnoreCase) ? "Before" : "After";
            this.imagePath = imagePath;
            image = Image.FromFile(imagePath);
            Text = "Cassette Motion Pro - Automatic Rider Tracking Assistant";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterParent;
            BuildInterface();
            SuggestLandmarks();
            UpdateGuide();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (image != null)
                image.Dispose();
            base.OnFormClosed(e);
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 370));

            picture.Dock = DockStyle.Fill;
            picture.BackColor = Color.FromArgb(13, 19, 17);
            picture.Image = image;
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            picture.Paint += Picture_Paint;
            picture.MouseDown += Picture_MouseDown;
            picture.MouseMove += Picture_MouseMove;
            picture.MouseUp += delegate { dragIndex = -1; };

            Panel side = new Panel();
            side.Dock = DockStyle.Fill;
            side.AutoScroll = true;
            side.BackColor = Color.White;
            side.Padding = new Padding(22);

            Label eyebrow = NewLabel("AUTOMATIC RIDER TRACKING ASSISTANT", 26, true);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);
            Label title = NewLabel("Rider Angles", 44, true);
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);

            Label instructions = NewLabel(
                "Suggested points are placed automatically from the rider image. Drag every orange point onto the correct joint before saving. Use matching side views and crank positions for Before and After.",
                92, false);
            instructions.ForeColor = Color.FromArgb(74, 87, 81);

            progress.Dock = DockStyle.Top;
            progress.Height = 38;
            progress.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            progress.ForeColor = Color.White;
            progress.BackColor = Color.FromArgb(60, 145, 76);
            progress.Padding = new Padding(10, 9, 10, 6);

            currentPoint.Dock = DockStyle.Top;
            currentPoint.Height = 60;
            currentPoint.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            currentPoint.BackColor = Color.FromArgb(238, 247, 219);
            currentPoint.ForeColor = Color.FromArgb(24, 31, 29);
            currentPoint.Padding = new Padding(10, 10, 10, 8);

            results.Dock = DockStyle.Top;
            results.Height = 190;
            results.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            results.ForeColor = Color.FromArgb(24, 31, 29);
            results.Padding = new Padding(0, 12, 0, 4);

            quality.Dock = DockStyle.Top;
            quality.Height = 150;
            quality.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            quality.Padding = new Padding(10, 8, 10, 8);

            ConfigureButton(undo, "Undo Last Point", false);
            ConfigureButton(clear, "Clear All Points", false);
            ConfigureButton(autoSuggest, "Suggest Rider Points Again", true);
            ConfigureButton(flipDirection, "Flip Rider Direction", false);
            ConfigureButton(saveBefore, "Save to Before", string.Equals(defaultSide, "Before", StringComparison.OrdinalIgnoreCase));
            ConfigureButton(saveAfter, "Save to After", string.Equals(defaultSide, "After", StringComparison.OrdinalIgnoreCase));
            undo.Click += Undo_Click;
            clear.Click += Clear_Click;
            autoSuggest.Click += delegate { SuggestLandmarks(); UpdateGuide(); picture.Invalidate(); };
            flipDirection.Click += FlipDirection_Click;
            saveBefore.Click += delegate { SaveResult("Before"); };
            saveAfter.Click += delegate { SaveResult("After"); };

            Button close = new Button();
            ConfigureButton(close, "Close", false);
            close.Click += delegate { Close(); };

            side.Controls.Add(close);
            side.Controls.Add(saveAfter);
            side.Controls.Add(saveBefore);
            side.Controls.Add(clear);
            side.Controls.Add(undo);
            side.Controls.Add(flipDirection);
            side.Controls.Add(autoSuggest);
            side.Controls.Add(quality);
            side.Controls.Add(results);
            side.Controls.Add(currentPoint);
            side.Controls.Add(progress);
            side.Controls.Add(instructions);
            side.Controls.Add(title);
            side.Controls.Add(eyebrow);

            root.Controls.Add(picture, 0, 0);
            root.Controls.Add(side, 1, 0);
            Controls.Add(root);
        }

        private static Label NewLabel(string text, int height, bool bold)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Top;
            label.Height = height;
            label.Font = new Font("Segoe UI", 9F, bold ? FontStyle.Bold : FontStyle.Regular);
            return label;
        }

        private static void ConfigureButton(Button button, string text, bool primary)
        {
            button.Text = text;
            button.Dock = DockStyle.Top;
            button.Height = 40;
            button.Margin = new Padding(0, 6, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(85, 122, 18) : Color.FromArgb(184, 193, 188);
            button.BackColor = primary ? Color.FromArgb(184, 243, 74) : Color.White;
            button.ForeColor = Color.FromArgb(24, 31, 29);
        }

        private void Picture_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            PointF imagePoint;
            if (!TryToImagePoint(e.Location, out imagePoint))
                return;

            dragIndex = FindPoint(imagePoint);
            if (dragIndex >= 0)
                return;

            if (points.Count >= landmarkNames.Length)
                return;

            points.Add(imagePoint);
            automaticSuggestion = false;
            if (points.Count == landmarkNames.Length)
                CalculateAngles();
            UpdateGuide();
            picture.Invalidate();
        }

        private void Picture_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragIndex < 0 || e.Button != MouseButtons.Left)
                return;

            PointF imagePoint;
            if (!TryToImagePoint(e.Location, out imagePoint))
                return;

            points[dragIndex] = imagePoint;
            if (points.Count == landmarkNames.Length)
                CalculateAngles();
            UpdateGuide();
            picture.Invalidate();
        }

        private void Undo_Click(object sender, EventArgs e)
        {
            if (points.Count == 0)
                return;
            points.RemoveAt(points.Count - 1);
            calculatedValues.Clear();
            UpdateGuide();
            picture.Invalidate();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            points.Clear();
            calculatedValues.Clear();
            automaticSuggestion = false;
            UpdateGuide();
            picture.Invalidate();
        }

        private void CalculateAngles()
        {
            calculatedValues = new Dictionary<string, string>();
            calculatedValues["KneeAngle"] = FormatAngle(Angle(points[0], points[1], points[2]));
            calculatedValues["HipAngle"] = FormatAngle(Angle(points[4], points[0], points[1]));
            calculatedValues["AnkleAngle"] = FormatAngle(Angle(points[1], points[2], points[3]));
            calculatedValues["TorsoAngle"] = FormatAngle(Angle(points[0], points[4], points[6]));
            calculatedValues["ShoulderAngle"] = FormatAngle(LineAngleFromHorizontal(points[0], points[4]));
        }

        private void UpdateGuide()
        {
            undo.Enabled = points.Count > 0;
            clear.Enabled = points.Count > 0;
            bool complete = points.Count == landmarkNames.Length;
            saveBefore.Enabled = complete;
            saveAfter.Enabled = complete;

            if (!complete)
            {
                progress.Text = "LANDMARK " + (points.Count + 1).ToString(CultureInfo.InvariantCulture) + " OF " + landmarkNames.Length.ToString(CultureInfo.InvariantCulture);
                currentPoint.Text = "Click: " + landmarkNames[points.Count];
                results.Text = "Calculated results will appear after all seven landmarks are placed.";
                quality.Text = GetImageQualityText();
                quality.BackColor = Color.FromArgb(247, 250, 244);
                quality.ForeColor = Color.FromArgb(74, 87, 81);
                return;
            }

            progress.Text = automaticSuggestion ? "AUTO SUGGESTED · Review every orange point" : "REVIEW · Drag any point to fine-tune";
            currentPoint.Text = "All eight landmarks placed — approve only after checking each joint";
            results.Text =
                "Knee angle: " + Value("KneeAngle") + "\n" +
                "Hip angle: " + Value("HipAngle") + "\n" +
                "Ankle angle: " + Value("AnkleAngle") + "\n" +
                "Body reach: " + Value("TorsoAngle") + "\n" +
                "Back angle: " + Value("ShoulderAngle");

            List<string> warnings = GetQualityWarnings();
            string confidence = automaticSuggestion ? "\nTracking confidence: " + suggestionConfidence.ToString("0", CultureInfo.InvariantCulture) + "% — fitter confirmation required." : string.Empty;
            quality.Text = warnings.Count == 0 ? "QUALITY CHECK: PASS\nLandmarks and broad angle ranges look consistent." + confidence : "QUALITY CHECK: REVIEW\n• " + string.Join("\n• ", warnings.ToArray()) + confidence;
            quality.BackColor = warnings.Count == 0 ? Color.FromArgb(232, 246, 226) : Color.FromArgb(255, 244, 214);
            quality.ForeColor = warnings.Count == 0 ? Color.FromArgb(46, 108, 55) : Color.FromArgb(128, 82, 12);
        }

        private void SaveResult(string side)
        {
            if (points.Count != landmarkNames.Length)
                return;
            DialogResult confirm = MessageBox.Show(this,
                "Save these rider measurements to " + side + "?\n\n" + results.Text + "\n\n" + quality.Text + "\n\nQuality warnings are advisory and do not block saving.",
                "Confirm Rider Measurements", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;
            ResultValues = new Dictionary<string, string>(calculatedValues);
            ResultSide = side;
            AnnotatedImagePath = SaveAnnotatedImage(side);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Picture_Paint(object sender, PaintEventArgs e)
        {
            RectangleF rect = GetImageRectangle();
            using (Pen line = new Pen(Color.FromArgb(220, 184, 243, 74), 3F))
            using (Brush pointBrush = new SolidBrush(Color.FromArgb(242, 126, 44)))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                DrawConnection(e.Graphics, line, rect, 0, 1);
                DrawConnection(e.Graphics, line, rect, 1, 2);
                DrawConnection(e.Graphics, line, rect, 2, 3);
                DrawConnection(e.Graphics, line, rect, 0, 4);
                DrawConnection(e.Graphics, line, rect, 4, 5);
                DrawConnection(e.Graphics, line, rect, 5, 6);
                DrawConnection(e.Graphics, line, rect, 4, 7);

                for (int i = 0; i < points.Count; i++)
                {
                    PointF p = ToControlPoint(points[i], rect);
                    e.Graphics.FillEllipse(pointBrush, p.X - 7, p.Y - 7, 14, 14);
                    e.Graphics.DrawString((i + 1).ToString(CultureInfo.InvariantCulture), font, textBrush, p.X + 9, p.Y - 10);
                }
            }
        }

        private void DrawConnection(Graphics graphics, Pen pen, RectangleF rect, int first, int second)
        {
            if (points.Count <= first || points.Count <= second)
                return;
            graphics.DrawLine(pen, ToControlPoint(points[first], rect), ToControlPoint(points[second], rect));
        }

        private int FindPoint(PointF point)
        {
            double threshold = Math.Max(image.Width, image.Height) * 0.025;
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (Distance(points[i], point) <= threshold)
                    return i;
            }
            return -1;
        }

        private bool TryToImagePoint(Point controlPoint, out PointF imagePoint)
        {
            RectangleF rect = GetImageRectangle();
            if (!rect.Contains(controlPoint))
            {
                imagePoint = PointF.Empty;
                return false;
            }
            imagePoint = new PointF(
                (controlPoint.X - rect.X) * image.Width / rect.Width,
                (controlPoint.Y - rect.Y) * image.Height / rect.Height);
            return true;
        }

        private PointF ToControlPoint(PointF imagePoint, RectangleF rect)
        {
            return new PointF(rect.X + imagePoint.X * rect.Width / image.Width, rect.Y + imagePoint.Y * rect.Height / image.Height);
        }

        private RectangleF GetImageRectangle()
        {
            if (image == null || picture.ClientSize.Width <= 0 || picture.ClientSize.Height <= 0)
                return RectangleF.Empty;
            float scale = Math.Min((float)picture.ClientSize.Width / image.Width, (float)picture.ClientSize.Height / image.Height);
            float width = image.Width * scale;
            float height = image.Height * scale;
            return new RectangleF((picture.ClientSize.Width - width) / 2F, (picture.ClientSize.Height - height) / 2F, width, height);
        }

        private static double Angle(PointF a, PointF vertex, PointF b)
        {
            double ax = a.X - vertex.X;
            double ay = a.Y - vertex.Y;
            double bx = b.X - vertex.X;
            double by = b.Y - vertex.Y;
            double denominator = Math.Sqrt(ax * ax + ay * ay) * Math.Sqrt(bx * bx + by * by);
            if (denominator <= 0.0001)
                return 0;
            double cosine = Math.Max(-1, Math.Min(1, (ax * bx + ay * by) / denominator));
            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        private static double LineAngleFromHorizontal(PointF a, PointF b)
        {
            double angle = Math.Abs(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180.0 / Math.PI);
            if (angle > 90)
                angle = 180 - angle;
            return angle;
        }

        private static double Distance(PointF a, PointF b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static string FormatAngle(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + "°";
        }

        private string Value(string key)
        {
            return calculatedValues.ContainsKey(key) ? calculatedValues[key] : "--";
        }

        private string GetImageQualityText()
        {
            if (image.Width < 960 || image.Height < 540)
                return "IMAGE CHECK: REVIEW\nResolution is " + image.Width.ToString() + " × " + image.Height.ToString() + ". Use a sharper side-view image when possible.";
            return "IMAGE CHECK: PASS\nResolution is " + image.Width.ToString() + " × " + image.Height.ToString() + ".";
        }

        private List<string> GetQualityWarnings()
        {
            List<string> warnings = new List<string>();
            if (image.Width < 960 || image.Height < 540)
                warnings.Add("Low-resolution reference image");
            AddAngleWarning(warnings, "Knee", "KneeAngle", 90, 175);
            AddAngleWarning(warnings, "Hip", "HipAngle", 25, 150);
            AddAngleWarning(warnings, "Ankle", "AnkleAngle", 55, 175);
            AddAngleWarning(warnings, "Body reach", "TorsoAngle", 20, 180);
            AddAngleWarning(warnings, "Back", "ShoulderAngle", 5, 85);

            double minimumSegment = Math.Max(image.Width, image.Height) * 0.025;
            if (Distance(points[0], points[1]) < minimumSegment || Distance(points[1], points[2]) < minimumSegment)
                warnings.Add("Hip, knee, or ankle points may be too close together");
            if (Distance(points[4], points[5]) < minimumSegment || Distance(points[5], points[6]) < minimumSegment)
                warnings.Add("Shoulder, elbow, or hand-contact points may be too close together");
            if (automaticSuggestion)
                warnings.Add("Automatic joint locations are suggestions and must be visually confirmed");
            return warnings;
        }

        private void SuggestLandmarks()
        {
            detectedBounds = DetectSubjectBounds();
            float left = detectedBounds.Left;
            float top = detectedBounds.Top;
            float width = detectedBounds.Width;
            float height = detectedBounds.Height;
            points.Clear();
            points.Add(PosePoint(left, top, width, height, 0.50f, 0.49f)); // Hip
            points.Add(PosePoint(left, top, width, height, 0.57f, 0.68f)); // Knee
            points.Add(PosePoint(left, top, width, height, 0.49f, 0.86f)); // Ankle
            points.Add(PosePoint(left, top, width, height, 0.64f, 0.88f)); // Toe
            points.Add(PosePoint(left, top, width, height, 0.38f, 0.29f)); // Shoulder
            points.Add(PosePoint(left, top, width, height, 0.52f, 0.35f)); // Elbow
            points.Add(PosePoint(left, top, width, height, 0.67f, 0.40f)); // Wrist
            points.Add(PosePoint(left, top, width, height, 0.34f, 0.18f)); // Ear
            automaticSuggestion = true;
            CalculateAngles();
        }

        private PointF PosePoint(float left, float top, float width, float height, float x, float y)
        {
            float adjustedX = facingRight ? x : 1F - x;
            return new PointF(left + width * adjustedX, top + height * y);
        }

        private RectangleF DetectSubjectBounds()
        {
            using (Bitmap bitmap = new Bitmap(image))
            {
                Color background = AverageCorners(bitmap);
                int step = Math.Max(2, Math.Max(bitmap.Width, bitmap.Height) / 220);
                int minX = bitmap.Width;
                int minY = bitmap.Height;
                int maxX = 0;
                int maxY = 0;
                int samples = 0;
                int different = 0;
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
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }

                RectangleF fallback = new RectangleF(image.Width * 0.16F, image.Height * 0.07F, image.Width * 0.68F, image.Height * 0.88F);
                if (different < 40 || maxX <= minX || maxY <= minY)
                {
                    suggestionConfidence = 25;
                    return fallback;
                }

                RectangleF detected = RectangleF.FromLTRB(minX, minY, maxX, maxY);
                float coverage = detected.Width * detected.Height / (image.Width * image.Height);
                if (coverage < 0.12F || coverage > 0.96F)
                {
                    suggestionConfidence = 35;
                    return fallback;
                }
                suggestionConfidence = Math.Max(40, Math.Min(78, 48 + different * 100.0 / Math.Max(1, samples)));
                return detected;
            }
        }

        private static Color AverageCorners(Bitmap bitmap)
        {
            Color[] samples =
            {
                bitmap.GetPixel(2, 2), bitmap.GetPixel(bitmap.Width - 3, 2),
                bitmap.GetPixel(2, bitmap.Height - 3), bitmap.GetPixel(bitmap.Width - 3, bitmap.Height - 3)
            };
            return Color.FromArgb((int)samples.Average(c => c.R), (int)samples.Average(c => c.G), (int)samples.Average(c => c.B));
        }

        private void FlipDirection_Click(object sender, EventArgs e)
        {
            facingRight = !facingRight;
            SuggestLandmarks();
            UpdateGuide();
            picture.Invalidate();
        }

        private string SaveAnnotatedImage(string side)
        {
            string directory = Path.GetDirectoryName(imagePath);
            string path = Path.Combine(directory, side + "-Tracked-Rider-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".png");
            using (Bitmap output = new Bitmap(image.Width, image.Height))
            using (Graphics graphics = Graphics.FromImage(output))
            using (Pen line = new Pen(Color.FromArgb(184, 243, 74), Math.Max(3F, image.Width / 420F)))
            using (Brush pointBrush = new SolidBrush(Color.FromArgb(242, 126, 44)))
            using (Brush labelBrush = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", Math.Max(11F, image.Width / 115F), FontStyle.Bold))
            {
                graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                DrawImageLine(graphics, line, 0, 1);
                DrawImageLine(graphics, line, 1, 2);
                DrawImageLine(graphics, line, 2, 3);
                DrawImageLine(graphics, line, 0, 4);
                DrawImageLine(graphics, line, 4, 5);
                DrawImageLine(graphics, line, 5, 6);
                DrawImageLine(graphics, line, 4, 7);
                float radius = Math.Max(7F, image.Width / 180F);
                for (int i = 0; i < points.Count; i++)
                {
                    PointF point = points[i];
                    graphics.FillEllipse(pointBrush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
                    graphics.DrawString(landmarkNames[i], font, labelBrush, point.X + radius + 3, point.Y - radius);
                }
                output.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            return path;
        }

        private void DrawImageLine(Graphics graphics, Pen pen, int first, int second)
        {
            graphics.DrawLine(pen, points[first], points[second]);
        }

        private void AddAngleWarning(List<string> warnings, string label, string key, double minimum, double maximum)
        {
            string value = Value(key).Replace("°", string.Empty);
            double parsed;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                warnings.Add(label + " angle could not be calculated");
                return;
            }
            if (parsed < minimum || parsed > maximum)
                warnings.Add(label + " angle is outside the broad review range");
        }
    }
}
