/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class BikeMetricGuidedCaptureForm : Form
    {
        private enum ClickMode
        {
            None,
            Calibration,
            Landmarks
        }

        private readonly string imagePath;
        private readonly List<PointF> calibrationPoints = new List<PointF>();
        private readonly List<PointF> landmarkPoints = new List<PointF>();
        private readonly string[] landmarkNames = new string[]
        {
            "Bottom bracket center",
            "Saddle top",
            "Saddle tip",
            "Grip / hood contact point"
        };

        private PictureBox picture;
        private Label status;
        private Label scaleLabel;
        private Label resultsLabel;
        private Button saveBefore;
        private Button saveAfter;
        private Image loadedImage;
        private ClickMode mode;
        private float zoomFactor = 1F;
        private PointF panOffset = PointF.Empty;
        private bool isPanning;
        private Point panStart;
        private PointF panStartOffset;
        private double millimetersPerPixel;
        private Dictionary<string, string> calculatedValues = new Dictionary<string, string>();

        public Dictionary<string, string> ResultValues { get; private set; }
        public string ResultSide { get; private set; }

        public BikeMetricGuidedCaptureForm(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException("imagePath");
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("The measurement reference image could not be found.", imagePath);

            this.imagePath = imagePath;

            Text = "Guided Bike Metric Capture";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ForeColor = Color.FromArgb(24, 31, 29);
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterParent;

            BuildInterface();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (loadedImage != null)
                loadedImage.Dispose();
            base.OnFormClosed(e);
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));

            picture = new PictureBox();
            picture.Dock = DockStyle.Fill;
            picture.BackColor = Color.FromArgb(13, 19, 17);
            picture.SizeMode = PictureBoxSizeMode.Normal;
            picture.TabStop = true;
            loadedImage = Image.FromFile(imagePath);
            picture.MouseClick += Picture_MouseClick;
            picture.MouseDown += Picture_MouseDown;
            picture.MouseMove += Picture_MouseMove;
            picture.MouseUp += Picture_MouseUp;
            picture.MouseWheel += Picture_MouseWheel;
            picture.MouseEnter += delegate { picture.Focus(); };
            picture.Resize += delegate { ClampPanOffset(); picture.Invalidate(); };
            picture.Paint += Picture_Paint;

            Panel side = new Panel();
            side.Dock = DockStyle.Fill;
            side.BackColor = Color.White;
            side.Padding = new Padding(22);

            Label eyebrow = new Label();
            eyebrow.Text = "GUIDED LANDMARK CAPTURE";
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);
            eyebrow.Dock = DockStyle.Top;
            eyebrow.Height = 26;

            Label title = new Label();
            title.Text = "Bike Metrics";
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.Dock = DockStyle.Top;
            title.Height = 44;

            Label guide = new Label();
            guide.Text =
                "1. Calibrate scale using a known bike length.\n" +
                "2. Click Start Guided Capture.\n" +
                "3. Click these points in order:\n" +
                "   • Bottom bracket center\n" +
                "   • Saddle top\n" +
                "   • Saddle tip\n" +
                "   • Grip / hood contact point\n" +
                "4. Review calculated values.\n" +
                "5. Save to Before or After.";
            guide.Dock = DockStyle.Top;
            guide.Height = 160;
            guide.ForeColor = Color.FromArgb(74, 87, 81);

            status = new Label();
            status.Text = "Start with Calibrate Scale.";
            status.Dock = DockStyle.Top;
            status.Height = 50;
            status.ForeColor = Color.FromArgb(24, 31, 29);

            scaleLabel = new Label();
            scaleLabel.Text = "Scale: not calibrated";
            scaleLabel.Dock = DockStyle.Top;
            scaleLabel.Height = 30;
            scaleLabel.ForeColor = Color.FromArgb(92, 104, 98);

            resultsLabel = new Label();
            resultsLabel.Text = "Calculated metrics:\n--";
            resultsLabel.Dock = DockStyle.Top;
            resultsLabel.Height = 142;
            resultsLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            resultsLabel.ForeColor = Color.FromArgb(24, 31, 29);

            FlowLayoutPanel zoomPanel = new FlowLayoutPanel();
            zoomPanel.Dock = DockStyle.Top;
            zoomPanel.Height = 36;
            zoomPanel.FlowDirection = FlowDirection.LeftToRight;
            zoomPanel.WrapContents = false;

            Button zoomOut = CreateButton("−", false);
            Button zoomReset = CreateButton("Reset Zoom", false);
            Button zoomCenter = CreateButton("Center Image", false);
            Button zoomIn = CreateButton("+", false);
            zoomOut.Size = new Size(36, 32);
            zoomReset.Size = new Size(86, 32);
            zoomCenter.Size = new Size(96, 32);
            zoomIn.Size = new Size(36, 32);
            zoomOut.Click += delegate { ZoomAroundCenter(0.8F); };
            zoomReset.Click += delegate { ResetZoom(); };
            zoomCenter.Click += delegate { CenterImage(); };
            zoomIn.Click += delegate { ZoomAroundCenter(1.25F); };
            zoomPanel.Controls.Add(zoomOut);
            zoomPanel.Controls.Add(zoomReset);
            zoomPanel.Controls.Add(zoomCenter);
            zoomPanel.Controls.Add(zoomIn);

            Button calibrate = CreateButton("1. Calibrate Scale", false);
            Button capture = CreateButton("2. Start Guided Capture", false);
            Button clear = CreateButton("Clear Points", false);
            saveBefore = CreateButton("Save to Before", false);
            saveAfter = CreateButton("Save to After", true);
            calibrate.Dock = DockStyle.Top;
            capture.Dock = DockStyle.Top;
            clear.Dock = DockStyle.Top;
            saveBefore.Dock = DockStyle.Top;
            saveAfter.Dock = DockStyle.Top;
            calibrate.Height = 34;
            capture.Height = 34;
            clear.Height = 34;
            saveBefore.Height = 34;
            saveAfter.Height = 34;
            calibrate.Margin = new Padding(0, 6, 0, 0);
            capture.Margin = new Padding(0, 6, 0, 0);
            clear.Margin = new Padding(0, 6, 0, 0);
            saveBefore.Margin = new Padding(0, 6, 0, 0);
            saveAfter.Margin = new Padding(0, 6, 0, 0);
            calibrate.Click += Calibrate_Click;
            capture.Click += Capture_Click;
            clear.Click += Clear_Click;
            saveBefore.Click += delegate { SaveResult("Before"); };
            saveAfter.Click += delegate { SaveResult("After"); };
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;

            Button close = CreateButton("Close", false);
            close.Dock = DockStyle.Bottom;
            close.Height = 40;
            close.Click += delegate { Close(); };

            side.Controls.Add(close);
            side.Controls.Add(saveAfter);
            side.Controls.Add(saveBefore);
            side.Controls.Add(clear);
            side.Controls.Add(capture);
            side.Controls.Add(calibrate);
            side.Controls.Add(zoomPanel);
            side.Controls.Add(resultsLabel);
            side.Controls.Add(scaleLabel);
            side.Controls.Add(status);
            side.Controls.Add(guide);
            side.Controls.Add(title);
            side.Controls.Add(eyebrow);

            root.Controls.Add(picture, 0, 0);
            root.Controls.Add(side, 1, 0);
            Controls.Add(root);
        }

        private void Calibrate_Click(object sender, EventArgs e)
        {
            mode = ClickMode.Calibration;
            calibrationPoints.Clear();
            landmarkPoints.Clear();
            calculatedValues.Clear();
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            status.Text = "Calibration: click the first point of a known length.";
            picture.Invalidate();
        }

        private void Capture_Click(object sender, EventArgs e)
        {
            if (millimetersPerPixel <= 0)
            {
                MessageBox.Show(this, "Calibrate the scale first.", "Scale required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            mode = ClickMode.Landmarks;
            landmarkPoints.Clear();
            calculatedValues.Clear();
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            status.Text = "Click landmark 1 of 4: " + landmarkNames[0] + ".";
            picture.Invalidate();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            landmarkPoints.Clear();
            calculatedValues.Clear();
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            status.Text = millimetersPerPixel > 0 ? "Points cleared. Click Start Guided Capture." : "Points cleared. Start with Calibrate Scale.";
            picture.Invalidate();
        }

        private void Picture_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            PointF imagePoint;
            if (!TryConvertControlPointToImagePoint(e.Location, out imagePoint))
                return;

            if (mode == ClickMode.Calibration)
                AddCalibrationPoint(imagePoint);
            else if (mode == ClickMode.Landmarks)
                AddLandmarkPoint(imagePoint);
        }

        private void AddCalibrationPoint(PointF imagePoint)
        {
            calibrationPoints.Add(imagePoint);
            if (calibrationPoints.Count == 1)
            {
                status.Text = "Calibration: click the second point of the known length.";
                picture.Invalidate();
                return;
            }

            if (calibrationPoints.Count == 2)
            {
                double pixelDistance = Distance(calibrationPoints[0], calibrationPoints[1]);
                if (pixelDistance <= 0)
                {
                    MessageBox.Show(this, "The calibration points are too close together.", "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    calibrationPoints.Clear();
                    picture.Invalidate();
                    return;
                }

                double knownMillimeters;
                if (!PromptForMillimeters(this, "Known length", "Enter the real positive length between those two calibration points in millimeters:", out knownMillimeters))
                {
                    calibrationPoints.Clear();
                    status.Text = "Calibration cancelled. Click Calibrate Scale to try again.";
                    picture.Invalidate();
                    return;
                }

                millimetersPerPixel = knownMillimeters / pixelDistance;
                scaleLabel.Text = "Scale: " + millimetersPerPixel.ToString("0.0000", CultureInfo.InvariantCulture) + " mm/pixel";
                status.Text = "Scale calibrated. Click Start Guided Capture.";
                mode = ClickMode.None;
                picture.Invalidate();
            }
        }

        private void AddLandmarkPoint(PointF imagePoint)
        {
            landmarkPoints.Add(imagePoint);
            if (landmarkPoints.Count < landmarkNames.Length)
            {
                status.Text = "Click landmark " + (landmarkPoints.Count + 1).ToString(CultureInfo.InvariantCulture) + " of 4: " + landmarkNames[landmarkPoints.Count] + ".";
                picture.Invalidate();
                return;
            }

            CalculateMetrics();
            mode = ClickMode.None;
            saveBefore.Enabled = true;
            saveAfter.Enabled = true;
            status.Text = "Guided capture complete. Review values, then save to Before or After.";
            picture.Invalidate();
        }

        private void CalculateMetrics()
        {
            PointF bottomBracket = landmarkPoints[0];
            PointF saddleTop = landmarkPoints[1];
            PointF saddleTip = landmarkPoints[2];
            PointF grip = landmarkPoints[3];

            double saddleHeight = Distance(bottomBracket, saddleTop) * millimetersPerPixel;
            double saddleSetback = (bottomBracket.X - saddleTip.X) * millimetersPerPixel;
            double saddleTipToGripReach = (grip.X - saddleTip.X) * millimetersPerPixel;
            double handlebarX = (grip.X - bottomBracket.X) * millimetersPerPixel;
            double handlebarY = (bottomBracket.Y - grip.Y) * millimetersPerPixel;

            calculatedValues = new Dictionary<string, string>();
            calculatedValues["SaddleHeight"] = FormatMillimeters(saddleHeight);
            calculatedValues["SaddleSetback"] = FormatMillimeters(saddleSetback);
            calculatedValues["SaddleTipToGripReach"] = FormatMillimeters(saddleTipToGripReach);
            calculatedValues["HandlebarX"] = FormatMillimeters(handlebarX);
            calculatedValues["HandlebarY"] = FormatMillimeters(handlebarY);

            resultsLabel.Text =
                "Calculated metrics:\n" +
                "Saddle height: " + calculatedValues["SaddleHeight"] + "\n" +
                "Saddle setback: " + calculatedValues["SaddleSetback"] + "\n" +
                "Saddle tip to grip: " + calculatedValues["SaddleTipToGripReach"] + "\n" +
                "Handlebar X: " + calculatedValues["HandlebarX"] + "\n" +
                "Handlebar Y: " + calculatedValues["HandlebarY"];
        }

        private void SaveResult(string side)
        {
            if (calculatedValues == null || calculatedValues.Count == 0)
                return;

            ResultValues = new Dictionary<string, string>(calculatedValues);
            ResultSide = side;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Picture_MouseDown(object sender, MouseEventArgs e)
        {
            picture.Focus();
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                panStart = e.Location;
                panStartOffset = panOffset;
                picture.Cursor = Cursors.SizeAll;
            }
        }

        private void Picture_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isPanning)
                return;

            panOffset = new PointF(panStartOffset.X + e.X - panStart.X, panStartOffset.Y + e.Y - panStart.Y);
            ClampPanOffset();
            picture.Invalidate();
        }

        private void Picture_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isPanning)
                return;

            isPanning = false;
            picture.Cursor = Cursors.Default;
        }

        private void Picture_MouseWheel(object sender, MouseEventArgs e)
        {
            ZoomAtPoint(e.Delta > 0 ? 1.15F : 0.87F, e.Location);
        }

        private void Picture_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Rectangle imageRectangle = GetZoomedImageRectangle();
            if (loadedImage != null && imageRectangle.Width > 0 && imageRectangle.Height > 0)
                e.Graphics.DrawImage(loadedImage, imageRectangle);

            DrawLine(e.Graphics, calibrationPoints, Color.FromArgb(184, 243, 74), "C");
            DrawLandmarks(e.Graphics);
        }

        private void DrawLine(Graphics graphics, IList<PointF> imagePoints, Color color, string label)
        {
            if (imagePoints == null || imagePoints.Count == 0)
                return;

            List<PointF> controlPoints = new List<PointF>();
            foreach (PointF imagePoint in imagePoints)
                controlPoints.Add(ConvertImagePointToControlPoint(imagePoint));

            using (Pen pen = new Pen(color, 3F))
            using (Brush brush = new SolidBrush(color))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(13, 19, 17)))
            using (Font font = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                if (controlPoints.Count == 2)
                    graphics.DrawLine(pen, controlPoints[0], controlPoints[1]);

                for (int i = 0; i < controlPoints.Count; i++)
                {
                    PointF point = controlPoints[i];
                    RectangleF circle = new RectangleF(point.X - 8, point.Y - 8, 16, 16);
                    graphics.FillEllipse(brush, circle);
                    graphics.DrawString(label + (i + 1).ToString(CultureInfo.InvariantCulture), font, textBrush, point.X + 10, point.Y - 12);
                }
            }
        }

        private void DrawLandmarks(Graphics graphics)
        {
            if (landmarkPoints.Count == 0)
                return;

            using (Brush brush = new SolidBrush(Color.FromArgb(255, 176, 74)))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Pen guidePen = new Pen(Color.FromArgb(255, 176, 74), 2F))
            using (Font font = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                guidePen.DashStyle = DashStyle.Dash;

                for (int i = 0; i < landmarkPoints.Count; i++)
                {
                    PointF point = ConvertImagePointToControlPoint(landmarkPoints[i]);
                    RectangleF circle = new RectangleF(point.X - 8, point.Y - 8, 16, 16);
                    graphics.FillEllipse(brush, circle);
                    graphics.DrawString((i + 1).ToString(CultureInfo.InvariantCulture) + ". " + landmarkNames[i], font, textBrush, point.X + 10, point.Y - 12);
                }

                if (landmarkPoints.Count >= 4)
                {
                    PointF bottomBracket = ConvertImagePointToControlPoint(landmarkPoints[0]);
                    PointF saddleTop = ConvertImagePointToControlPoint(landmarkPoints[1]);
                    PointF saddleTip = ConvertImagePointToControlPoint(landmarkPoints[2]);
                    PointF grip = ConvertImagePointToControlPoint(landmarkPoints[3]);

                    graphics.DrawLine(guidePen, bottomBracket, saddleTop);
                    graphics.DrawLine(guidePen, bottomBracket.X, bottomBracket.Y, saddleTip.X, bottomBracket.Y);
                    graphics.DrawLine(guidePen, saddleTip.X, saddleTip.Y, grip.X, saddleTip.Y);
                    graphics.DrawLine(guidePen, bottomBracket.X, bottomBracket.Y, grip.X, bottomBracket.Y);
                    graphics.DrawLine(guidePen, bottomBracket.X, bottomBracket.Y, bottomBracket.X, grip.Y);
                }
            }
        }

        private bool TryConvertControlPointToImagePoint(Point controlPoint, out PointF imagePoint)
        {
            imagePoint = PointF.Empty;
            Rectangle imageRectangle = GetZoomedImageRectangle();
            if (!imageRectangle.Contains(controlPoint))
                return false;

            float x = (controlPoint.X - imageRectangle.Left) * loadedImage.Width / (float)imageRectangle.Width;
            float y = (controlPoint.Y - imageRectangle.Top) * loadedImage.Height / (float)imageRectangle.Height;
            imagePoint = new PointF(x, y);
            return true;
        }

        private PointF ConvertImagePointToControlPoint(PointF imagePoint)
        {
            Rectangle imageRectangle = GetZoomedImageRectangle();
            float x = imageRectangle.Left + imagePoint.X * imageRectangle.Width / loadedImage.Width;
            float y = imageRectangle.Top + imagePoint.Y * imageRectangle.Height / loadedImage.Height;
            return new PointF(x, y);
        }

        private Rectangle GetZoomedImageRectangle()
        {
            if (loadedImage == null || picture.ClientSize.Width <= 0 || picture.ClientSize.Height <= 0)
                return Rectangle.Empty;

            Size scaledSize = GetZoomedImageSize();
            int width = scaledSize.Width;
            int height = scaledSize.Height;

            int left = (int)Math.Round(((picture.ClientSize.Width - width) / 2.0) + panOffset.X);
            int top = (int)Math.Round(((picture.ClientSize.Height - height) / 2.0) + panOffset.Y);
            return new Rectangle(left, top, width, height);
        }

        private void ZoomAroundCenter(float multiplier)
        {
            ZoomAtPoint(multiplier, new Point(picture.ClientSize.Width / 2, picture.ClientSize.Height / 2));
        }

        private void ZoomAtPoint(float multiplier, Point focusPoint)
        {
            if (loadedImage == null)
                return;

            PointF imagePoint;
            bool hasFocusImagePoint = TryConvertControlPointToImagePoint(focusPoint, out imagePoint);
            float newZoom = Math.Max(1F, Math.Min(8F, zoomFactor * multiplier));
            if (Math.Abs(newZoom - zoomFactor) < 0.001F)
                return;

            zoomFactor = newZoom;
            if (hasFocusImagePoint)
            {
                PointF afterZoom = ConvertImagePointToControlPoint(imagePoint);
                panOffset = new PointF(panOffset.X + focusPoint.X - afterZoom.X, panOffset.Y + focusPoint.Y - afterZoom.Y);
            }

            ClampPanOffset();
            picture.Invalidate();
        }

        private void ResetZoom()
        {
            zoomFactor = 1F;
            panOffset = PointF.Empty;
            picture.Invalidate();
        }

        private void CenterImage()
        {
            panOffset = PointF.Empty;
            ClampPanOffset();
            picture.Invalidate();
        }

        private void ClampPanOffset()
        {
            if (loadedImage == null || picture.ClientSize.Width <= 0 || picture.ClientSize.Height <= 0)
                return;

            Size scaledSize = GetZoomedImageSize();
            float panX = panOffset.X;
            float panY = panOffset.Y;

            panX = ClampPanAxis(panX, scaledSize.Width, picture.ClientSize.Width);
            panY = ClampPanAxis(panY, scaledSize.Height, picture.ClientSize.Height);
            panOffset = new PointF(panX, panY);
        }

        private static float ClampPanAxis(float panValue, int imageSize, int viewportSize)
        {
            if (imageSize <= viewportSize)
                return 0F;

            float centeredStart = (viewportSize - imageSize) / 2F;
            float minimumPan = viewportSize - imageSize - centeredStart;
            float maximumPan = -centeredStart;
            return Math.Max(minimumPan, Math.Min(maximumPan, panValue));
        }

        private Size GetZoomedImageSize()
        {
            double imageRatio = loadedImage.Width / (double)loadedImage.Height;
            double boxRatio = picture.ClientSize.Width / (double)picture.ClientSize.Height;
            int width;
            int height;

            if (imageRatio > boxRatio)
            {
                width = picture.ClientSize.Width;
                height = (int)Math.Round(width / imageRatio);
            }
            else
            {
                height = picture.ClientSize.Height;
                width = (int)Math.Round(height * imageRatio);
            }

            width = Math.Max(1, (int)Math.Round(width * zoomFactor));
            height = Math.Max(1, (int)Math.Round(height * zoomFactor));
            return new Size(width, height);
        }

        private static double Distance(PointF first, PointF second)
        {
            double dx = first.X - second.X;
            double dy = first.Y - second.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static string FormatMillimeters(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture) + " mm";
        }

        private static bool PromptForMillimeters(IWin32Window owner, string title, string prompt, out double value)
        {
            value = 0;
            using (Form form = new Form())
            using (Label label = new Label())
            using (TextBox input = new TextBox())
            using (Button ok = new Button())
            using (Button cancel = new Button())
            {
                form.Text = title;
                form.Font = new Font("Segoe UI", 9F);
                form.ClientSize = new Size(380, 148);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;

                label.Text = prompt;
                label.SetBounds(14, 14, 350, 36);
                input.SetBounds(14, 58, 350, 24);
                input.Text = "172.5";

                ok.Text = "OK";
                ok.DialogResult = DialogResult.OK;
                ok.SetBounds(194, 104, 82, 28);
                cancel.Text = "Cancel";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.SetBounds(282, 104, 82, 28);

                form.Controls.Add(label);
                form.Controls.Add(input);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                while (form.ShowDialog(owner) == DialogResult.OK)
                {
                    string raw = input.Text.Trim().Replace("mm", string.Empty).Trim();
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0)
                        return true;

                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value) && value > 0)
                        return true;

                    MessageBox.Show(owner, "Calibration length must be positive, like 172.5.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            return false;
        }

        private static Button CreateButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(186, 197, 191);
            button.BackColor = primary ? Color.FromArgb(184, 243, 74) : Color.White;
            button.ForeColor = Color.FromArgb(13, 19, 17);
            button.Font = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular);
            return button;
        }
    }
}
