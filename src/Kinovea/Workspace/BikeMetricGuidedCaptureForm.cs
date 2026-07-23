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
            LevelReference,
            Landmarks
        }

        private readonly string imagePath;
        private readonly List<PointF> calibrationPoints = new List<PointF>();
        private readonly List<PointF> levelReferencePoints = new List<PointF>();
        private readonly List<PointF> landmarkPoints = new List<PointF>();
        private readonly string[] basicLandmarkNames = new string[]
        {
            "Bottom bracket center",
            "Saddle top",
            "Saddle tip",
            "Grip / hood contact point"
        };
        private readonly string[] advancedLandmarkNames = new string[]
        {
            "Bottom bracket center",
            "Saddle top",
            "Saddle tip",
            "Grip / hood contact point",
            "Pedal spindle",
            "Handlebar center",
            "Front axle",
            "Rear axle"
        };

        private PictureBox picture;
        private Label status;
        private Label currentLandmarkLabel;
        private Label nextPointHintLabel;
        private Label scaleLabel;
        private Label referenceLabel;
        private Label resultsLabel;
        private Button levelReference;
        private Button undoLast;
        private Button recalculate;
        private Button flipSetbackSign;
        private Button saveBefore;
        private Button saveAfter;
        private CheckBox advancedLandmarks;
        private Image loadedImage;
        private ClickMode mode;
        private float zoomFactor = 1F;
        private PointF panOffset = PointF.Empty;
        private bool isPanning;
        private bool isDraggingLandmark;
        private bool hasMousePosition;
        private bool suppressNextClick;
        private int draggedLandmarkIndex = -1;
        private Point panStart;
        private Point mousePosition;
        private PointF panStartOffset;
        private double millimetersPerPixel;
        private Dictionary<string, string> calculatedValues = new Dictionary<string, string>();

        private string[] ActiveLandmarkNames
        {
            get { return advancedLandmarks != null && advancedLandmarks.Checked ? advancedLandmarkNames : basicLandmarkNames; }
        }

        public Dictionary<string, string> ResultValues { get; private set; }
        public string ResultSide { get; private set; }
        public string CaptureMethod { get; private set; }
        public string LevelReferenceStatus { get; private set; }
        public string SaddleSetbackConvention { get; private set; }
        public string CameraSetupStatus { get; private set; }

        public BikeMetricGuidedCaptureForm(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException("imagePath");
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("The measurement reference image could not be found.", imagePath);

            this.imagePath = imagePath;
            CameraSetupStatus = "Not confirmed";

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
            picture.MouseLeave += Picture_MouseLeave;
            picture.MouseEnter += delegate { picture.Focus(); };
            picture.Resize += delegate { ClampPanOffset(); picture.Invalidate(); };
            picture.Paint += Picture_Paint;

            TableLayoutPanel side = new TableLayoutPanel();
            side.Dock = DockStyle.Fill;
            side.ColumnCount = 1;
            side.RowCount = 2;
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            side.BackColor = Color.White;

            Panel sideScroll = new Panel();
            sideScroll.Dock = DockStyle.Fill;
            sideScroll.AutoScroll = true;
            sideScroll.BackColor = Color.White;
            sideScroll.Padding = new Padding(22);

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
                "1. Review Camera Setup.\n" +
                "2. Calibrate scale using a known bike length.\n" +
                "3. Optional: click Level Reference using floor/axle line.\n" +
                "4. Click Start Guided Capture.\n" +
                "   • Bottom bracket center\n" +
                "   • Saddle top\n" +
                "   • Saddle tip\n" +
                "   • Grip / hood contact point\n" +
                "5. Drag any orange point to fine-tune it.\n" +
                "6. Review values, then save to Before or After.";
            guide.Dock = DockStyle.Top;
            guide.Height = 160;
            guide.ForeColor = Color.FromArgb(74, 87, 81);

            status = new Label();
            status.Text = "Start with Calibrate Scale.";
            status.Dock = DockStyle.Top;
            status.Height = 44;
            status.ForeColor = Color.FromArgb(24, 31, 29);

            currentLandmarkLabel = new Label();
            currentLandmarkLabel.Text = "Current point: --";
            currentLandmarkLabel.Dock = DockStyle.Top;
            currentLandmarkLabel.Height = 54;
            currentLandmarkLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            currentLandmarkLabel.ForeColor = Color.FromArgb(13, 19, 17);
            currentLandmarkLabel.BackColor = Color.FromArgb(238, 247, 219);
            currentLandmarkLabel.Padding = new Padding(10, 8, 10, 8);

            nextPointHintLabel = new Label();
            nextPointHintLabel.Text = "Tip: zoom in, click the point, then drag the orange dot if it needs adjustment.";
            nextPointHintLabel.Dock = DockStyle.Top;
            nextPointHintLabel.Height = 54;
            nextPointHintLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            nextPointHintLabel.ForeColor = Color.FromArgb(74, 87, 81);
            nextPointHintLabel.BackColor = Color.FromArgb(247, 250, 244);
            nextPointHintLabel.Padding = new Padding(10, 8, 10, 8);

            scaleLabel = new Label();
            scaleLabel.Text = "Scale: not calibrated";
            scaleLabel.Dock = DockStyle.Top;
            scaleLabel.Height = 30;
            scaleLabel.ForeColor = Color.FromArgb(92, 104, 98);

            referenceLabel = new Label();
            referenceLabel.Text = "Level reference: not set";
            referenceLabel.Dock = DockStyle.Top;
            referenceLabel.Height = 30;
            referenceLabel.ForeColor = Color.FromArgb(92, 104, 98);

            resultsLabel = new Label();
            resultsLabel.Text = "Calculated metrics:\n--";
            resultsLabel.Dock = DockStyle.Top;
            resultsLabel.Height = 238;
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

            advancedLandmarks = new CheckBox();
            advancedLandmarks.Text = "Advanced landmarks (8 points)";
            advancedLandmarks.Dock = DockStyle.Top;
            advancedLandmarks.Height = 34;
            advancedLandmarks.ForeColor = Color.FromArgb(24, 31, 29);
            advancedLandmarks.BackColor = Color.White;
            advancedLandmarks.CheckedChanged += AdvancedLandmarks_CheckedChanged;

            Button cameraSetup = CreateButton("1. Camera Setup", false);
            Button calibrate = CreateButton("2. Calibrate Scale", false);
            levelReference = CreateButton("3. Level Reference", false);
            Button capture = CreateButton("4. Start Guided Capture", false);
            undoLast = CreateButton("Undo Last Point", false);
            Button clear = CreateButton("Clear Points", false);
            recalculate = CreateButton("Recalculate Values", false);
            flipSetbackSign = CreateButton("Flip Setback Sign", false);
            saveBefore = CreateButton("Save to Before", false);
            saveAfter = CreateButton("Save to After", true);
            cameraSetup.Dock = DockStyle.Top;
            calibrate.Dock = DockStyle.Top;
            levelReference.Dock = DockStyle.Top;
            capture.Dock = DockStyle.Top;
            undoLast.Dock = DockStyle.Top;
            clear.Dock = DockStyle.Top;
            recalculate.Dock = DockStyle.Top;
            flipSetbackSign.Dock = DockStyle.Top;
            saveBefore.Dock = DockStyle.Top;
            saveAfter.Dock = DockStyle.Top;
            cameraSetup.Height = 34;
            calibrate.Height = 34;
            levelReference.Height = 34;
            capture.Height = 34;
            undoLast.Height = 34;
            clear.Height = 34;
            recalculate.Height = 34;
            flipSetbackSign.Height = 34;
            saveBefore.Height = 34;
            saveAfter.Height = 34;
            cameraSetup.Margin = new Padding(0, 6, 0, 0);
            calibrate.Margin = new Padding(0, 6, 0, 0);
            levelReference.Margin = new Padding(0, 6, 0, 0);
            capture.Margin = new Padding(0, 6, 0, 0);
            undoLast.Margin = new Padding(0, 6, 0, 0);
            clear.Margin = new Padding(0, 6, 0, 0);
            recalculate.Margin = new Padding(0, 6, 0, 0);
            flipSetbackSign.Margin = new Padding(0, 6, 0, 0);
            saveBefore.Margin = new Padding(0, 6, 0, 0);
            saveAfter.Margin = new Padding(0, 6, 0, 0);
            cameraSetup.Click += CameraSetup_Click;
            calibrate.Click += Calibrate_Click;
            levelReference.Click += LevelReference_Click;
            capture.Click += Capture_Click;
            undoLast.Click += UndoLast_Click;
            clear.Click += Clear_Click;
            recalculate.Click += Recalculate_Click;
            flipSetbackSign.Click += FlipSetbackSign_Click;
            saveBefore.Click += delegate { SaveResult("Before"); };
            saveAfter.Click += delegate { SaveResult("After"); };
            levelReference.Enabled = false;
            undoLast.Enabled = false;
            recalculate.Enabled = false;
            flipSetbackSign.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;

            Button close = CreateButton("Close", false);
            close.Dock = DockStyle.Bottom;
            close.Height = 40;
            close.Click += delegate { Close(); };

            sideScroll.Controls.Add(saveAfter);
            sideScroll.Controls.Add(saveBefore);
            sideScroll.Controls.Add(flipSetbackSign);
            sideScroll.Controls.Add(recalculate);
            sideScroll.Controls.Add(clear);
            sideScroll.Controls.Add(undoLast);
            sideScroll.Controls.Add(capture);
            sideScroll.Controls.Add(levelReference);
            sideScroll.Controls.Add(calibrate);
            sideScroll.Controls.Add(cameraSetup);
            sideScroll.Controls.Add(advancedLandmarks);
            sideScroll.Controls.Add(zoomPanel);
            sideScroll.Controls.Add(resultsLabel);
            sideScroll.Controls.Add(referenceLabel);
            sideScroll.Controls.Add(scaleLabel);
            sideScroll.Controls.Add(nextPointHintLabel);
            sideScroll.Controls.Add(currentLandmarkLabel);
            sideScroll.Controls.Add(status);
            sideScroll.Controls.Add(guide);
            sideScroll.Controls.Add(title);
            sideScroll.Controls.Add(eyebrow);

            side.Controls.Add(sideScroll, 0, 0);
            side.Controls.Add(close, 0, 1);

            root.Controls.Add(picture, 0, 0);
            root.Controls.Add(side, 1, 0);
            Controls.Add(root);
        }

        private void CameraSetup_Click(object sender, EventArgs e)
        {
            string checklist =
                "For the most accurate bike measurements:\n\n" +
                "✓ Camera is straight side-on to the bike.\n" +
                "✓ Camera is level, not tilted.\n" +
                "✓ Bike is upright and not leaning.\n" +
                "✓ Use 2x/telephoto or step farther back if possible.\n" +
                "✓ Avoid ultra-wide lens distortion.\n" +
                "✓ The calibration length is in the same plane as the bike.\n" +
                "✓ Use Level Reference if the image is slightly tilted.\n\n" +
                "Confirm camera setup for this Guided Capture?";

            DialogResult result = MessageBox.Show(this,
                checklist,
                "Camera Setup / Accuracy Checklist",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            CameraSetupStatus = result == DialogResult.Yes ? "Confirmed" : "Not confirmed";
            status.Text = "Camera setup: " + CameraSetupStatus + ".";
            nextPointHintLabel.Text = result == DialogResult.Yes ?
                "Good. Next: calibrate scale using a known real length." :
                "You can still continue, but measurements may be less accurate.";

            if (calculatedValues != null && calculatedValues.Count > 0)
            {
                calculatedValues["CameraSetup"] = CameraSetupStatus;
                UpdateResultsLabel();
            }

            picture.Invalidate();
        }

        private void Calibrate_Click(object sender, EventArgs e)
        {
            mode = ClickMode.Calibration;
            calibrationPoints.Clear();
            levelReferencePoints.Clear();
            landmarkPoints.Clear();
            calculatedValues.Clear();
            levelReference.Enabled = false;
            undoLast.Enabled = false;
            recalculate.Enabled = false;
            flipSetbackSign.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            referenceLabel.Text = "Level reference: not set";
            status.Text = "Calibration: click the first point of a known length.";
            currentLandmarkLabel.Text = "Current point: calibration point 1";
            nextPointHintLabel.Text = "Click point 1 of 2 on a known distance, like crank length or wheelbase.";
            picture.Invalidate();
        }

        private void LevelReference_Click(object sender, EventArgs e)
        {
            if (millimetersPerPixel <= 0)
            {
                MessageBox.Show(this, "Calibrate the scale first.", "Scale required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            mode = ClickMode.LevelReference;
            levelReferencePoints.Clear();
            calculatedValues.Clear();
            flipSetbackSign.Enabled = false;
            recalculate.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            status.Text = "Level reference: click the first point on a true horizontal line, like floor or axle line.";
            currentLandmarkLabel.Text = "Current point: level reference point 1";
            nextPointHintLabel.Text = "Click point 1 of 2 on the floor, axle line, or another true horizontal reference.";
            undoLast.Enabled = false;
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
            undoLast.Enabled = false;
            flipSetbackSign.Enabled = false;
            recalculate.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            status.Text = "Click landmark 1 of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + ActiveLandmarkNames[0] + ".";
            UpdateCurrentLandmarkInstruction();
            picture.Invalidate();
        }

        private void AdvancedLandmarks_CheckedChanged(object sender, EventArgs e)
        {
            landmarkPoints.Clear();
            calculatedValues.Clear();
            mode = ClickMode.None;
            undoLast.Enabled = false;
            recalculate.Enabled = false;
            flipSetbackSign.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            string modeName = advancedLandmarks.Checked ? "Advanced 8-point mode" : "Basic 4-point mode";
            status.Text = modeName + " selected. Click Start Guided Capture.";
            currentLandmarkLabel.Text = "Current point: --";
            nextPointHintLabel.Text = advancedLandmarks.Checked ?
                "Advanced adds pedal spindle, handlebar center, front axle, and rear axle." :
                "Basic captures the four core contact points.";
            picture.Invalidate();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            levelReferencePoints.Clear();
            landmarkPoints.Clear();
            calculatedValues.Clear();
            undoLast.Enabled = false;
            flipSetbackSign.Enabled = false;
            recalculate.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            referenceLabel.Text = "Level reference: not set";
            status.Text = millimetersPerPixel > 0 ? "Points cleared. Click Start Guided Capture." : "Points cleared. Start with Calibrate Scale.";
            currentLandmarkLabel.Text = "Current point: --";
            nextPointHintLabel.Text = millimetersPerPixel > 0 ? "Scale is still set. Start Guided Capture when ready." : "Tip: calibrate scale first, then set level reference if the camera is tilted.";
            picture.Invalidate();
        }

        private void UndoLast_Click(object sender, EventArgs e)
        {
            if (mode == ClickMode.Landmarks || landmarkPoints.Count > 0)
            {
                if (landmarkPoints.Count == 0)
                    return;

                landmarkPoints.RemoveAt(landmarkPoints.Count - 1);
                calculatedValues.Clear();
                flipSetbackSign.Enabled = false;
                recalculate.Enabled = false;
                saveBefore.Enabled = false;
                saveAfter.Enabled = false;
                resultsLabel.Text = "Calculated metrics:\n--";
                mode = ClickMode.Landmarks;
                status.Text = landmarkPoints.Count == 0 ? "Click landmark 1 of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + ActiveLandmarkNames[0] + "." : "Last point removed. Continue guided capture.";
                UpdateCurrentLandmarkInstruction();
                undoLast.Enabled = landmarkPoints.Count > 0 || calibrationPoints.Count > 0;
                picture.Invalidate();
                return;
            }

            if (mode == ClickMode.LevelReference && levelReferencePoints.Count > 0)
            {
                levelReferencePoints.RemoveAt(levelReferencePoints.Count - 1);
                referenceLabel.Text = "Level reference: not set";
                status.Text = levelReferencePoints.Count == 0 ? "Level reference: click the first point on a true horizontal line." : "Level reference: click the second point.";
                currentLandmarkLabel.Text = levelReferencePoints.Count == 0 ? "Current point: level reference point 1" : "Current point: level reference point 2";
                nextPointHintLabel.Text = levelReferencePoints.Count == 0 ? "Click point 1 of 2 on a level line." : "Click point 2 of 2 on that same level line.";
                undoLast.Enabled = levelReferencePoints.Count > 0;
                picture.Invalidate();
                return;
            }

            if (mode == ClickMode.Calibration && calibrationPoints.Count > 0)
            {
                calibrationPoints.RemoveAt(calibrationPoints.Count - 1);
                status.Text = calibrationPoints.Count == 0 ? "Calibration: click the first point of a known length." : "Calibration: click the second point of the known length.";
                currentLandmarkLabel.Text = calibrationPoints.Count == 0 ? "Current point: calibration point 1" : "Current point: calibration point 2";
                nextPointHintLabel.Text = calibrationPoints.Count == 0 ? "Click point 1 of 2 on a known distance." : "Click point 2 of 2 on that same known distance.";
                undoLast.Enabled = calibrationPoints.Count > 0;
                picture.Invalidate();
            }
        }

        private void Recalculate_Click(object sender, EventArgs e)
        {
            if (landmarkPoints.Count < ActiveLandmarkNames.Length)
                return;

            CalculateMetrics();
            flipSetbackSign.Enabled = true;
            saveBefore.Enabled = true;
            saveAfter.Enabled = true;
            status.Text = "Values recalculated. Review values, then save to Before or After.";
            currentLandmarkLabel.Text = "Current point: complete";
            nextPointHintLabel.Text = "Review the numbers. Drag any orange point to fine-tune before saving.";
            picture.Invalidate();
        }

        private void FlipSetbackSign_Click(object sender, EventArgs e)
        {
            if (calculatedValues == null || !calculatedValues.ContainsKey("SaddleSetback"))
                return;

            double setback;
            if (!TryParseMillimeters(calculatedValues["SaddleSetback"], out setback))
                return;

            calculatedValues["SaddleSetback"] = FormatMillimeters(-setback);
            UpdateResultsLabel();
            status.Text = "Saddle setback sign flipped. Review values, then save to Before or After.";
            nextPointHintLabel.Text = "Reminder: behind the bottom bracket should be negative.";
            picture.Invalidate();
        }

        private void Picture_MouseClick(object sender, MouseEventArgs e)
        {
            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            if (e.Button != MouseButtons.Left)
                return;

            PointF imagePoint;
            if (!TryConvertControlPointToImagePoint(e.Location, out imagePoint))
                return;

            if (mode == ClickMode.Calibration)
                AddCalibrationPoint(imagePoint);
            else if (mode == ClickMode.LevelReference)
                AddLevelReferencePoint(imagePoint);
            else if (mode == ClickMode.Landmarks)
                AddLandmarkPoint(imagePoint);
        }

        private void AddCalibrationPoint(PointF imagePoint)
        {
            calibrationPoints.Add(imagePoint);
            undoLast.Enabled = true;
            if (calibrationPoints.Count == 1)
            {
                status.Text = "Calibration: click the second point of the known length.";
                currentLandmarkLabel.Text = "Current point: calibration point 2";
                nextPointHintLabel.Text = "Click point 2 of 2 on the other end of that known distance.";
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
                status.Text = "Scale calibrated. Optional: click Level Reference, or start Guided Capture.";
                currentLandmarkLabel.Text = "Current point: ready for level reference or guided capture";
                nextPointHintLabel.Text = "Next: use Level Reference if the image is tilted, or Start Guided Capture.";
                levelReference.Enabled = true;
                undoLast.Enabled = false;
                mode = ClickMode.None;
                picture.Invalidate();
            }
        }

        private void AddLevelReferencePoint(PointF imagePoint)
        {
            levelReferencePoints.Add(imagePoint);
            undoLast.Enabled = true;
            if (levelReferencePoints.Count == 1)
            {
                status.Text = "Level reference: click the second point on that same true horizontal line.";
                currentLandmarkLabel.Text = "Current point: level reference point 2";
                nextPointHintLabel.Text = "Click point 2 of 2 on that same level line.";
                picture.Invalidate();
                return;
            }

            if (levelReferencePoints.Count == 2)
            {
                double pixelDistance = Distance(levelReferencePoints[0], levelReferencePoints[1]);
                if (pixelDistance <= 0)
                {
                    MessageBox.Show(this, "The level reference points are too close together.", "Level reference", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    levelReferencePoints.Clear();
                    picture.Invalidate();
                    return;
                }

                double angleDegrees = GetLevelReferenceAngleDegrees();
                referenceLabel.Text = "Level reference: set (" + angleDegrees.ToString("0.0", CultureInfo.InvariantCulture) + "° tilt correction)";
                status.Text = "Level reference set. Horizontal/vertical calculations will use this correction.";
                currentLandmarkLabel.Text = "Current point: ready for guided capture";
                nextPointHintLabel.Text = "Next: click Start Guided Capture and follow the landmark order.";
                mode = ClickMode.None;
                undoLast.Enabled = false;

                if (landmarkPoints.Count >= ActiveLandmarkNames.Length)
                {
                    CalculateMetrics();
                    flipSetbackSign.Enabled = true;
                    recalculate.Enabled = true;
                    saveBefore.Enabled = true;
                    saveAfter.Enabled = true;
                }

                picture.Invalidate();
            }
        }

        private void AddLandmarkPoint(PointF imagePoint)
        {
            landmarkPoints.Add(imagePoint);
            undoLast.Enabled = true;
            if (landmarkPoints.Count < ActiveLandmarkNames.Length)
            {
                status.Text = "Click landmark " + (landmarkPoints.Count + 1).ToString(CultureInfo.InvariantCulture) + " of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + ActiveLandmarkNames[landmarkPoints.Count] + ".";
                UpdateCurrentLandmarkInstruction();
                picture.Invalidate();
                return;
            }

            CalculateMetrics();
            mode = ClickMode.None;
            flipSetbackSign.Enabled = true;
            recalculate.Enabled = true;
            saveBefore.Enabled = true;
            saveAfter.Enabled = true;
            status.Text = "Guided capture complete. Review values, then save to Before or After.";
            currentLandmarkLabel.Text = "Current point: complete";
            nextPointHintLabel.Text = "Drag any orange point to fine-tune. Values update before saving.";
            picture.Invalidate();
        }

        private void CalculateMetrics()
        {
            PointF bottomBracket = landmarkPoints[0];
            PointF saddleTop = landmarkPoints[1];
            PointF saddleTip = landmarkPoints[2];
            PointF grip = landmarkPoints[3];

            PointF correctedBottomBracket = CorrectForLevel(bottomBracket);
            PointF correctedSaddleTop = CorrectForLevel(saddleTop);
            PointF correctedSaddleTip = CorrectForLevel(saddleTip);
            PointF correctedGrip = CorrectForLevel(grip);
            PointF handlebarReference = grip;
            PointF correctedHandlebarReference = correctedGrip;

            if (advancedLandmarks.Checked && landmarkPoints.Count >= 6)
            {
                handlebarReference = landmarkPoints[5];
                correctedHandlebarReference = CorrectForLevel(handlebarReference);
            }

            double saddleHeight = Distance(bottomBracket, saddleTop) * millimetersPerPixel;
            double saddleSetback = (correctedSaddleTip.X - correctedBottomBracket.X) * millimetersPerPixel;
            double saddleTipToGripReach = (correctedGrip.X - correctedSaddleTip.X) * millimetersPerPixel;
            double handlebarX = (correctedHandlebarReference.X - correctedBottomBracket.X) * millimetersPerPixel;
            double handlebarY = (correctedBottomBracket.Y - correctedHandlebarReference.Y) * millimetersPerPixel;

            calculatedValues = new Dictionary<string, string>();
            calculatedValues["SaddleHeight"] = FormatMillimeters(saddleHeight);
            calculatedValues["SaddleSetback"] = FormatMillimeters(saddleSetback);
            calculatedValues["SaddleTipToGripReach"] = FormatMillimeters(saddleTipToGripReach);
            calculatedValues["HandlebarX"] = FormatMillimeters(handlebarX);
            calculatedValues["HandlebarY"] = FormatMillimeters(handlebarY);

            if (advancedLandmarks.Checked && landmarkPoints.Count >= advancedLandmarkNames.Length)
            {
                PointF pedalSpindle = landmarkPoints[4];
                PointF frontAxle = landmarkPoints[6];
                PointF rearAxle = landmarkPoints[7];
                PointF correctedFrontAxle = CorrectForLevel(frontAxle);
                PointF correctedRearAxle = CorrectForLevel(rearAxle);

                double crankLength = Distance(bottomBracket, pedalSpindle) * millimetersPerPixel;
                double handlebarReach = (correctedHandlebarReference.X - correctedSaddleTip.X) * millimetersPerPixel;
                double handlebarDrop = (correctedHandlebarReference.Y - correctedSaddleTop.Y) * millimetersPerPixel;
                double wheelbase = Math.Abs(correctedFrontAxle.X - correctedRearAxle.X) * millimetersPerPixel;

                calculatedValues["CrankLength"] = FormatMillimeters(crankLength);
                calculatedValues["HandlebarReach"] = FormatMillimeters(handlebarReach);
                calculatedValues["HandlebarDrop"] = FormatMillimeters(handlebarDrop);
                calculatedValues["Wheelbase"] = FormatMillimeters(wheelbase);
            }

            calculatedValues["LevelReference"] = levelReferencePoints.Count == 2 ? "Applied" : "Not set";
            calculatedValues["SaddleSetbackConvention"] = "Behind BB = negative";
            calculatedValues["LandmarkMode"] = advancedLandmarks.Checked ? "Advanced 8-point" : "Basic 4-point";
            calculatedValues["CameraSetup"] = CameraSetupStatus;

            UpdateResultsLabel();
        }

        private void SaveResult(string side)
        {
            if (calculatedValues == null || calculatedValues.Count == 0)
                return;

            DialogResult preview = MessageBox.Show(this,
                "Save these guided measurements to " + side + "?\n\n" + BuildMetricsPreview(),
                "Confirm Guided Capture",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (preview != DialogResult.Yes)
                return;

            ResultValues = new Dictionary<string, string>(calculatedValues);
            ResultSide = side;
            CaptureMethod = advancedLandmarks.Checked ? "Guided Capture - Advanced Landmarks" : "Guided Capture";
            LevelReferenceStatus = GetCalculatedValue("LevelReference");
            SaddleSetbackConvention = GetCalculatedValue("SaddleSetbackConvention");
            CameraSetupStatus = GetCalculatedValue("CameraSetup");
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateCurrentLandmarkInstruction()
        {
            if (mode != ClickMode.Landmarks)
                return;

            int nextIndex = landmarkPoints.Count;
            if (nextIndex >= ActiveLandmarkNames.Length)
            {
                currentLandmarkLabel.Text = "Current point: complete";
                nextPointHintLabel.Text = "Drag any orange point to fine-tune. Values update before saving.";
                return;
            }

            currentLandmarkLabel.Text = "Current point " + (nextIndex + 1).ToString(CultureInfo.InvariantCulture) + " of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + ActiveLandmarkNames[nextIndex];
            nextPointHintLabel.Text = GetLandmarkHint(nextIndex);
        }

        private string GetLandmarkHint(int index)
        {
            if (index == 0)
                return "Click the exact center of the bottom bracket/crank spindle.";

            if (index == 1)
                return "Click the top of the saddle where saddle height is measured.";

            if (index == 2)
                return "Click the front tip/nose of the saddle. Behind BB will calculate as negative.";

            if (index == 3)
                return "Click the hand contact point on the grip or hood.";

            if (index == 4)
                return "Click the center of the pedal spindle to calculate crank length.";

            if (index == 5)
                return "Click the handlebar center for bar X/Y, reach, and drop.";

            if (index == 6)
                return "Click the front axle center. This starts the wheelbase reference.";

            if (index == 7)
                return "Click the rear axle center to complete the advanced landmark set.";

            return "Zoom in if needed, then click the landmark.";
        }

        private void UpdateResultsLabel()
        {
            resultsLabel.Text =
                "Calculated metrics:\n" +
                "Mode: " + GetCalculatedValue("LandmarkMode") + "\n" +
                "Saddle height: " + GetCalculatedValue("SaddleHeight") + "\n" +
                "Saddle setback: " + GetCalculatedValue("SaddleSetback") + "\n" +
                "Saddle tip to grip: " + GetCalculatedValue("SaddleTipToGripReach") + "\n" +
                "Handlebar X: " + GetCalculatedValue("HandlebarX") + "\n" +
                "Handlebar Y: " + GetCalculatedValue("HandlebarY") + "\n" +
                "Crank length: " + GetCalculatedValue("CrankLength") + "\n" +
                "Handlebar reach: " + GetCalculatedValue("HandlebarReach") + "\n" +
                "Handlebar drop: " + GetCalculatedValue("HandlebarDrop") + "\n" +
                "Wheelbase: " + GetCalculatedValue("Wheelbase") + "\n" +
                "Level reference: " + GetCalculatedValue("LevelReference") + "\n" +
                "Camera setup: " + GetCalculatedValue("CameraSetup") + "\n" +
                "Setback convention: " + GetCalculatedValue("SaddleSetbackConvention");
        }

        private string BuildMetricsPreview()
        {
            return
                "Saddle height: " + GetCalculatedValue("SaddleHeight") + "\n" +
                "Saddle setback: " + GetCalculatedValue("SaddleSetback") + "\n" +
                "Saddle tip to grip: " + GetCalculatedValue("SaddleTipToGripReach") + "\n" +
                "Handlebar X: " + GetCalculatedValue("HandlebarX") + "\n" +
                "Handlebar Y: " + GetCalculatedValue("HandlebarY") + "\n\n" +
                "Crank length: " + GetCalculatedValue("CrankLength") + "\n" +
                "Handlebar reach: " + GetCalculatedValue("HandlebarReach") + "\n" +
                "Handlebar drop: " + GetCalculatedValue("HandlebarDrop") + "\n" +
                "Wheelbase: " + GetCalculatedValue("Wheelbase") + "\n\n" +
                "Landmark mode: " + GetCalculatedValue("LandmarkMode") + "\n" +
                "Level reference: " + GetCalculatedValue("LevelReference") + "\n" +
                "Camera setup: " + GetCalculatedValue("CameraSetup") + "\n" +
                "Saddle setback convention: " + GetCalculatedValue("SaddleSetbackConvention");
        }

        private string GetCalculatedValue(string key)
        {
            return calculatedValues.ContainsKey(key) ? calculatedValues[key] : "--";
        }

        private void Picture_MouseDown(object sender, MouseEventArgs e)
        {
            picture.Focus();
            hasMousePosition = true;
            mousePosition = e.Location;

            if (e.Button == MouseButtons.Left)
            {
                int landmarkIndex = FindNearestLandmarkIndex(e.Location);
                if (landmarkIndex >= 0)
                {
                    isDraggingLandmark = true;
                    draggedLandmarkIndex = landmarkIndex;
                    suppressNextClick = true;
                    picture.Cursor = Cursors.Hand;
                    status.Text = "Adjusting landmark " + (landmarkIndex + 1).ToString(CultureInfo.InvariantCulture) + ": " + ActiveLandmarkNames[landmarkIndex] + ".";
                    nextPointHintLabel.Text = "Drag to fine-tune this point. Release to keep the new position.";
                    picture.Invalidate();
                    return;
                }
            }

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
            hasMousePosition = true;
            mousePosition = e.Location;

            if (isDraggingLandmark)
            {
                PointF imagePoint;
                if (TryConvertControlPointToImagePoint(e.Location, out imagePoint) && draggedLandmarkIndex >= 0 && draggedLandmarkIndex < landmarkPoints.Count)
                {
                    landmarkPoints[draggedLandmarkIndex] = imagePoint;
                    calculatedValues.Clear();
                    if (landmarkPoints.Count >= ActiveLandmarkNames.Length && millimetersPerPixel > 0)
                    {
                        CalculateMetrics();
                        flipSetbackSign.Enabled = true;
                        recalculate.Enabled = true;
                        saveBefore.Enabled = true;
                        saveAfter.Enabled = true;
                    }
                    picture.Invalidate();
                }
                return;
            }

            if (!isPanning)
            {
                int hoverIndex = FindNearestLandmarkIndex(e.Location);
                picture.Cursor = hoverIndex >= 0 ? Cursors.Hand : Cursors.Default;
                if (mode != ClickMode.None || hoverIndex >= 0)
                    picture.Invalidate();
                return;
            }

            panOffset = new PointF(panStartOffset.X + e.X - panStart.X, panStartOffset.Y + e.Y - panStart.Y);
            ClampPanOffset();
            picture.Invalidate();
        }

        private void Picture_MouseLeave(object sender, EventArgs e)
        {
            hasMousePosition = false;
            if (!isDraggingLandmark && !isPanning)
                picture.Cursor = Cursors.Default;
            if (mode != ClickMode.None || landmarkPoints.Count > 0)
                picture.Invalidate();
        }

        private void Picture_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDraggingLandmark)
            {
                isDraggingLandmark = false;
                int adjustedIndex = draggedLandmarkIndex;
                draggedLandmarkIndex = -1;
                picture.Cursor = Cursors.Default;

                if (landmarkPoints.Count >= ActiveLandmarkNames.Length && millimetersPerPixel > 0)
                {
                    CalculateMetrics();
                    status.Text = "Landmark adjusted. Review updated values, then save to Before or After.";
                    nextPointHintLabel.Text = "You can keep dragging any orange point to fine-tune it.";
                }
                else if (adjustedIndex >= 0 && adjustedIndex < ActiveLandmarkNames.Length)
                {
                    status.Text = "Landmark adjusted. Continue guided capture.";
                    UpdateCurrentLandmarkInstruction();
                }

                picture.Invalidate();
                return;
            }

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
            DrawLine(e.Graphics, levelReferencePoints, Color.FromArgb(74, 145, 255), "L");
            DrawLandmarks(e.Graphics);
            DrawActiveClickCue(e.Graphics);
        }

        private void DrawLine(Graphics graphics, IList<PointF> imagePoints, Color color, string label)
        {
            if (imagePoints == null || imagePoints.Count == 0)
                return;

            List<PointF> controlPoints = new List<PointF>();
            foreach (PointF imagePoint in imagePoints)
                controlPoints.Add(ConvertImagePointToControlPoint(imagePoint));

            using (Pen pen = new Pen(color, 4F))
            using (Brush brush = new SolidBrush(color))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(13, 19, 17)))
            using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
            {
                if (controlPoints.Count == 2)
                    graphics.DrawLine(pen, controlPoints[0], controlPoints[1]);

                for (int i = 0; i < controlPoints.Count; i++)
                {
                    PointF point = controlPoints[i];
                    RectangleF circle = new RectangleF(point.X - 10, point.Y - 10, 20, 20);
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
            using (Brush labelBrush = new SolidBrush(Color.FromArgb(220, 13, 19, 17)))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Pen guidePen = new Pen(Color.FromArgb(255, 176, 74), 3F))
            using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
            {
                guidePen.DashStyle = DashStyle.Dash;

                for (int i = 0; i < landmarkPoints.Count; i++)
                {
                    PointF point = ConvertImagePointToControlPoint(landmarkPoints[i]);
                    RectangleF circle = new RectangleF(point.X - 11, point.Y - 11, 22, 22);
                    graphics.FillEllipse(brush, circle);
                    if (i == draggedLandmarkIndex)
                    {
                        using (Pen selectedPen = new Pen(Color.FromArgb(184, 243, 74), 4F))
                            graphics.DrawEllipse(selectedPen, point.X - 17, point.Y - 17, 34, 34);
                    }
                    string label = (i + 1).ToString(CultureInfo.InvariantCulture) + ". " + ActiveLandmarkNames[i];
                    SizeF labelSize = graphics.MeasureString(label, font);
                    RectangleF labelRectangle = new RectangleF(point.X + 14, point.Y - 16, labelSize.Width + 12, labelSize.Height + 6);
                    graphics.FillRectangle(labelBrush, labelRectangle);
                    graphics.DrawString(label, font, textBrush, labelRectangle.Left + 6, labelRectangle.Top + 3);
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

                if (landmarkPoints.Count >= advancedLandmarkNames.Length)
                {
                    PointF bottomBracket = ConvertImagePointToControlPoint(landmarkPoints[0]);
                    PointF saddleTop = ConvertImagePointToControlPoint(landmarkPoints[1]);
                    PointF saddleTip = ConvertImagePointToControlPoint(landmarkPoints[2]);
                    PointF pedalSpindle = ConvertImagePointToControlPoint(landmarkPoints[4]);
                    PointF handlebarCenter = ConvertImagePointToControlPoint(landmarkPoints[5]);
                    PointF frontAxle = ConvertImagePointToControlPoint(landmarkPoints[6]);
                    PointF rearAxle = ConvertImagePointToControlPoint(landmarkPoints[7]);

                    graphics.DrawLine(guidePen, bottomBracket, pedalSpindle);
                    graphics.DrawLine(guidePen, saddleTip.X, saddleTip.Y, handlebarCenter.X, saddleTip.Y);
                    graphics.DrawLine(guidePen, saddleTop.X, saddleTop.Y, handlebarCenter.X, handlebarCenter.Y);
                    graphics.DrawLine(guidePen, frontAxle, rearAxle);
                }
            }
        }

        private int FindNearestLandmarkIndex(Point controlPoint)
        {
            if (landmarkPoints.Count == 0)
                return -1;

            int nearestIndex = -1;
            double nearestDistance = double.MaxValue;
            const double hitRadius = 18.0;

            for (int i = 0; i < landmarkPoints.Count; i++)
            {
                PointF point = ConvertImagePointToControlPoint(landmarkPoints[i]);
                double dx = point.X - controlPoint.X;
                double dy = point.Y - controlPoint.Y;
                double distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance <= hitRadius && distance < nearestDistance)
                {
                    nearestIndex = i;
                    nearestDistance = distance;
                }
            }

            return nearestIndex;
        }

        private void DrawActiveClickCue(Graphics graphics)
        {
            string prompt = GetActiveClickPrompt();
            if (string.IsNullOrEmpty(prompt))
                return;

            using (Font promptFont = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (Brush panelBrush = new SolidBrush(Color.FromArgb(225, 13, 19, 17)))
            using (Brush accentBrush = new SolidBrush(Color.FromArgb(184, 243, 74)))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Pen accentPen = new Pen(Color.FromArgb(184, 243, 74), 3F))
            using (Pen shadowPen = new Pen(Color.FromArgb(190, 13, 19, 17), 5F))
            {
                RectangleF promptBox = new RectangleF(18, 18, Math.Min(560, picture.ClientSize.Width - 36), 62);
                graphics.FillRectangle(panelBrush, promptBox);
                graphics.FillRectangle(accentBrush, promptBox.Left, promptBox.Top, 8, promptBox.Height);
                graphics.DrawString(prompt, promptFont, textBrush, new RectangleF(promptBox.Left + 18, promptBox.Top + 10, promptBox.Width - 28, promptBox.Height - 14));

                if (!hasMousePosition)
                    return;

                Rectangle imageRectangle = GetZoomedImageRectangle();
                if (!imageRectangle.Contains(mousePosition))
                    return;

                int radius = 18;
                graphics.DrawEllipse(shadowPen, mousePosition.X - radius, mousePosition.Y - radius, radius * 2, radius * 2);
                graphics.DrawEllipse(accentPen, mousePosition.X - radius, mousePosition.Y - radius, radius * 2, radius * 2);
                graphics.DrawLine(accentPen, mousePosition.X - radius - 10, mousePosition.Y, mousePosition.X - 6, mousePosition.Y);
                graphics.DrawLine(accentPen, mousePosition.X + 6, mousePosition.Y, mousePosition.X + radius + 10, mousePosition.Y);
                graphics.DrawLine(accentPen, mousePosition.X, mousePosition.Y - radius - 10, mousePosition.X, mousePosition.Y - 6);
                graphics.DrawLine(accentPen, mousePosition.X, mousePosition.Y + 6, mousePosition.X, mousePosition.Y + radius + 10);
            }
        }

        private string GetActiveClickPrompt()
        {
            if (mode == ClickMode.Calibration)
                return "Click calibration point " + (calibrationPoints.Count + 1).ToString(CultureInfo.InvariantCulture) + " of 2";

            if (mode == ClickMode.LevelReference)
                return "Click level reference point " + (levelReferencePoints.Count + 1).ToString(CultureInfo.InvariantCulture) + " of 2";

            if (mode == ClickMode.Landmarks)
            {
                int nextIndex = landmarkPoints.Count;
                if (nextIndex < ActiveLandmarkNames.Length)
                    return "Click landmark " + (nextIndex + 1).ToString(CultureInfo.InvariantCulture) + " of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + ActiveLandmarkNames[nextIndex];
            }

            return string.Empty;
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

        private PointF CorrectForLevel(PointF point)
        {
            if (levelReferencePoints.Count != 2)
                return point;

            PointF origin = levelReferencePoints[0];
            double angle = GetLevelReferenceAngleRadians();
            double cos = Math.Cos(-angle);
            double sin = Math.Sin(-angle);
            double dx = point.X - origin.X;
            double dy = point.Y - origin.Y;
            double x = (dx * cos) - (dy * sin);
            double y = (dx * sin) + (dy * cos);
            return new PointF((float)x, (float)y);
        }

        private double GetLevelReferenceAngleRadians()
        {
            if (levelReferencePoints.Count != 2)
                return 0;

            PointF first = levelReferencePoints[0];
            PointF second = levelReferencePoints[1];
            return Math.Atan2(second.Y - first.Y, second.X - first.X);
        }

        private double GetLevelReferenceAngleDegrees()
        {
            return GetLevelReferenceAngleRadians() * 180.0 / Math.PI;
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

        private static bool TryParseMillimeters(string text, out double value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text))
                return false;

            string raw = text.Trim().Replace("mm", string.Empty).Trim();
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
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
