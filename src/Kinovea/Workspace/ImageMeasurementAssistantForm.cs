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
    public class ImageMeasurementAssistantForm : Form
    {
        private enum ClickMode
        {
            None,
            Calibration,
            Measurement
        }

        private readonly string imagePath;
        private readonly bool horizontalMeasurement;
        private readonly List<PointF> calibrationPoints = new List<PointF>();
        private readonly List<PointF> measurementPoints = new List<PointF>();
        private PictureBox picture;
        private Label status;
        private Label scaleLabel;
        private Label resultLabel;
        private Button flipSign;
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
        private double measuredMillimeters;
        private double manualSignedMillimeters;
        private bool hasManualSignedMeasurement;
        private bool resultIsNegative;

        public string ResultValue { get; private set; }
        public string ResultSide { get; private set; }

        public ImageMeasurementAssistantForm(string imagePath, string measurementName, string instructions)
            : this(imagePath, measurementName, instructions, false)
        {
        }

        public ImageMeasurementAssistantForm(string imagePath, string measurementName, string instructions, bool horizontalMeasurement)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException("imagePath");
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("The measurement reference image could not be found.", imagePath);

            this.imagePath = imagePath;
            this.horizontalMeasurement = horizontalMeasurement;

            Text = "Image Measurement Assistant - " + measurementName;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ForeColor = Color.FromArgb(24, 31, 29);
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterParent;

            BuildInterface(measurementName, instructions);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (loadedImage != null)
                loadedImage.Dispose();
            base.OnFormClosed(e);
        }

        private void BuildInterface(string measurementName, string instructions)
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));

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
            picture.Paint += Picture_Paint;

            Panel side = new Panel();
            side.Dock = DockStyle.Fill;
            side.BackColor = Color.White;
            side.Padding = new Padding(22);

            Label eyebrow = new Label();
            eyebrow.Text = "BIKE METRICS ASSIST";
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);
            eyebrow.Dock = DockStyle.Top;
            eyebrow.Height = 26;

            Label title = new Label();
            title.Text = measurementName;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.Dock = DockStyle.Top;
            title.Height = 48;

            Label guide = new Label();
            guide.Text = instructions + "\n\n" +
                "1. Click Calibrate Scale.\n" +
                "2. Click two points for a known length.\n" +
                "3. Enter the real length in mm.\n" +
                "4. Click Measure Points.\n" +
                "5. Click the two measurement points" + (horizontalMeasurement ? " from left to right." : ".") + "\n" +
                "6. Use Make Negative when needed.\n" +
                "7. Or enter a manual signed value.\n" +
                "8. Save to Before or After.";
            guide.Dock = DockStyle.Top;
            guide.Height = 174;
            guide.ForeColor = Color.FromArgb(74, 87, 81);

            status = new Label();
            status.Text = "Start with Calibrate Scale.";
            status.Dock = DockStyle.Top;
            status.Height = 54;
            status.ForeColor = Color.FromArgb(24, 31, 29);

            scaleLabel = new Label();
            scaleLabel.Text = "Scale: not calibrated";
            scaleLabel.Dock = DockStyle.Top;
            scaleLabel.Height = 30;
            scaleLabel.ForeColor = Color.FromArgb(92, 104, 98);

            resultLabel = new Label();
            resultLabel.Text = "Result: --";
            resultLabel.Dock = DockStyle.Top;
            resultLabel.Height = 34;
            resultLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            resultLabel.ForeColor = Color.FromArgb(24, 31, 29);

            FlowLayoutPanel zoomPanel = new FlowLayoutPanel();
            zoomPanel.Dock = DockStyle.Top;
            zoomPanel.Height = 38;
            zoomPanel.FlowDirection = FlowDirection.LeftToRight;
            zoomPanel.WrapContents = false;

            Button zoomOut = CreateButton("−", false);
            Button zoomReset = CreateButton("Reset Zoom", false);
            Button zoomIn = CreateButton("+", false);
            zoomOut.Size = new Size(42, 32);
            zoomReset.Size = new Size(104, 32);
            zoomIn.Size = new Size(42, 32);
            zoomOut.Click += delegate { ZoomAroundCenter(0.8F); };
            zoomReset.Click += delegate { ResetZoom(); };
            zoomIn.Click += delegate { ZoomAroundCenter(1.25F); };
            zoomPanel.Controls.Add(zoomOut);
            zoomPanel.Controls.Add(zoomReset);
            zoomPanel.Controls.Add(zoomIn);

            Button calibrate = CreateButton("1. Calibrate Scale", false);
            Button measure = CreateButton("2. Measure Points", false);
            Button manual = CreateButton("Enter Manual Value", false);
            flipSign = CreateButton("Make Negative", false);
            saveBefore = CreateButton("Save to Before", false);
            saveAfter = CreateButton("Save to After", true);
            calibrate.Dock = DockStyle.Top;
            measure.Dock = DockStyle.Top;
            manual.Dock = DockStyle.Top;
            flipSign.Dock = DockStyle.Top;
            saveBefore.Dock = DockStyle.Top;
            saveAfter.Dock = DockStyle.Top;
            calibrate.Height = 40;
            measure.Height = 40;
            manual.Height = 40;
            flipSign.Height = 40;
            saveBefore.Height = 40;
            saveAfter.Height = 40;
            calibrate.Margin = new Padding(0, 10, 0, 0);
            measure.Margin = new Padding(0, 10, 0, 0);
            manual.Margin = new Padding(0, 10, 0, 0);
            flipSign.Margin = new Padding(0, 10, 0, 0);
            saveBefore.Margin = new Padding(0, 10, 0, 0);
            saveAfter.Margin = new Padding(0, 10, 0, 0);
            calibrate.Click += Calibrate_Click;
            measure.Click += Measure_Click;
            manual.Click += Manual_Click;
            flipSign.Click += FlipSign_Click;
            saveBefore.Click += delegate { SaveResult("Before"); };
            saveAfter.Click += delegate { SaveResult("After"); };
            flipSign.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;

            Label imageLabel = new Label();
            imageLabel.Text = "Reference image:\n" + imagePath;
            imageLabel.Dock = DockStyle.Top;
            imageLabel.Height = 64;
            imageLabel.ForeColor = Color.FromArgb(92, 104, 98);

            Button close = CreateButton("Close", false);
            close.Dock = DockStyle.Bottom;
            close.Height = 40;
            close.Click += delegate { Close(); };

            side.Controls.Add(close);
            side.Controls.Add(saveAfter);
            side.Controls.Add(saveBefore);
            side.Controls.Add(flipSign);
            side.Controls.Add(manual);
            side.Controls.Add(measure);
            side.Controls.Add(calibrate);
            side.Controls.Add(zoomPanel);
            side.Controls.Add(resultLabel);
            side.Controls.Add(scaleLabel);
            side.Controls.Add(status);
            side.Controls.Add(imageLabel);
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
            measurementPoints.Clear();
            measuredMillimeters = 0;
            hasManualSignedMeasurement = false;
            manualSignedMillimeters = 0;
            resultIsNegative = false;
            flipSign.Enabled = false;
            flipSign.Text = "Make Negative";
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultLabel.Text = "Result: --";
            status.Text = "Calibration: click the first point of a known length.";
            picture.Invalidate();
        }

        private void Measure_Click(object sender, EventArgs e)
        {
            if (millimetersPerPixel <= 0)
            {
                MessageBox.Show(this, "Calibrate the scale first.", "Scale required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            mode = ClickMode.Measurement;
            measurementPoints.Clear();
            measuredMillimeters = 0;
            hasManualSignedMeasurement = false;
            manualSignedMillimeters = 0;
            resultIsNegative = false;
            flipSign.Enabled = false;
            flipSign.Text = "Make Negative";
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultLabel.Text = "Result: --";
            status.Text = "Measurement: click the first measurement point.";
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
            else if (mode == ClickMode.Measurement)
                AddMeasurementPoint(imagePoint);
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
                status.Text = "Scale calibrated. Now click Measure Points.";
                mode = ClickMode.None;
                picture.Invalidate();
            }
        }

        private void AddMeasurementPoint(PointF imagePoint)
        {
            measurementPoints.Add(imagePoint);
            if (measurementPoints.Count == 1)
            {
                status.Text = "Measurement: click the second measurement point.";
                picture.Invalidate();
                return;
            }

            if (measurementPoints.Count == 2)
            {
                measuredMillimeters = MeasurementDistance(measurementPoints[0], measurementPoints[1]) * millimetersPerPixel;
                hasManualSignedMeasurement = false;
                manualSignedMillimeters = 0;
                resultIsNegative = false;
                UpdateResultLabel();
                status.Text = horizontalMeasurement ? "Horizontal measurement ready. Use Make Negative if setback should save below zero." : "Measurement ready. Use Make Negative if this value should save below zero.";
                flipSign.Enabled = true;
                saveBefore.Enabled = true;
                saveAfter.Enabled = true;
                mode = ClickMode.None;
                picture.Invalidate();
            }
        }

        private void FlipSign_Click(object sender, EventArgs e)
        {
            if (measuredMillimeters <= 0 || hasManualSignedMeasurement)
                return;

            resultIsNegative = !resultIsNegative;
            UpdateResultLabel();
            status.Text = resultIsNegative ? "Negative value selected. Save to Before or After." : "Positive value selected. Save to Before or After.";
        }

        private void Manual_Click(object sender, EventArgs e)
        {
            double signedMillimeters;
            if (!PromptForSignedMillimeters(this, "Manual measurement", "Enter the measurement value in millimeters. Negative values are OK, like -9:", out signedMillimeters))
                return;

            manualSignedMillimeters = signedMillimeters;
            hasManualSignedMeasurement = true;
            measuredMillimeters = Math.Abs(signedMillimeters);
            resultIsNegative = signedMillimeters < 0;
            resultLabel.Text = "Result: " + signedMillimeters.ToString("0.0", CultureInfo.InvariantCulture) + " mm";
            flipSign.Enabled = false;
            saveBefore.Enabled = true;
            saveAfter.Enabled = true;
            status.Text = "Manual value ready. Save it to Before or After.";
        }

        private void SaveResult(string side)
        {
            if (!hasManualSignedMeasurement && measuredMillimeters <= 0)
                return;

            double value = hasManualSignedMeasurement ? manualSignedMillimeters : GetSignedMeasurement();
            ResultValue = value.ToString("0.0", CultureInfo.InvariantCulture) + " mm";
            ResultSide = side;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateResultLabel()
        {
            double signedMeasurement = GetSignedMeasurement();
            resultLabel.Text = "Result: " + signedMeasurement.ToString("0.0", CultureInfo.InvariantCulture) + " mm";
            flipSign.Text = resultIsNegative ? "Make Positive" : "Make Negative";
        }

        private double GetSignedMeasurement()
        {
            return resultIsNegative ? -measuredMillimeters : measuredMillimeters;
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
            DrawLine(e.Graphics, measurementPoints, Color.FromArgb(255, 176, 74), "M", horizontalMeasurement);
        }

        private void DrawLine(Graphics graphics, IList<PointF> imagePoints, Color color, string label)
        {
            DrawLine(graphics, imagePoints, color, label, false);
        }

        private void DrawLine(Graphics graphics, IList<PointF> imagePoints, Color color, string label, bool horizontalOnly)
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
                {
                    if (horizontalOnly)
                        graphics.DrawLine(pen, controlPoints[0].X, controlPoints[0].Y, controlPoints[1].X, controlPoints[0].Y);
                    else
                        graphics.DrawLine(pen, controlPoints[0], controlPoints[1]);
                }

                for (int i = 0; i < controlPoints.Count; i++)
                {
                    PointF point = controlPoints[i];
                    RectangleF circle = new RectangleF(point.X - 8, point.Y - 8, 16, 16);
                    graphics.FillEllipse(brush, circle);
                    graphics.DrawString(label + (i + 1).ToString(CultureInfo.InvariantCulture), font, textBrush, point.X + 10, point.Y - 12);
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

            picture.Invalidate();
        }

        private void ResetZoom()
        {
            zoomFactor = 1F;
            panOffset = PointF.Empty;
            picture.Invalidate();
        }

        private static double Distance(PointF first, PointF second)
        {
            double dx = first.X - second.X;
            double dy = first.Y - second.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private double MeasurementDistance(PointF first, PointF second)
        {
            if (horizontalMeasurement)
                return Math.Abs(first.X - second.X);

            return Distance(first, second);
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

                    MessageBox.Show(owner, "Calibration length must be positive, like 172.5. Use Enter Manual Value later for negative measurements such as -9.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            return false;
        }

        private static bool PromptForSignedMillimeters(IWin32Window owner, string title, string prompt, out double value)
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
                form.ClientSize = new Size(390, 148);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;

                label.Text = prompt;
                label.SetBounds(14, 14, 360, 36);
                input.SetBounds(14, 58, 360, 24);
                input.Text = "-9";

                ok.Text = "OK";
                ok.DialogResult = DialogResult.OK;
                ok.SetBounds(204, 104, 82, 28);
                cancel.Text = "Cancel";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.SetBounds(292, 104, 82, 28);

                form.Controls.Add(label);
                form.Controls.Add(input);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                while (form.ShowDialog(owner) == DialogResult.OK)
                {
                    string raw = input.Text.Trim().Replace("mm", string.Empty).Trim();
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && Math.Abs(value) > 0)
                        return true;

                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value) && Math.Abs(value) > 0)
                        return true;

                    MessageBox.Show(owner, "Enter a non-zero number, like -9 or 9.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
