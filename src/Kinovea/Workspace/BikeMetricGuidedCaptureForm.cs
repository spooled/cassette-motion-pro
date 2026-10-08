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
            BikeCalibration,
            WheelPerspectiveCalibration,
            Verification,
            LevelReference,
            Landmarks
        }

        private readonly string imagePath;
        private readonly string outputDirectory;
        private readonly string preferredSide;
        private readonly List<PointF> calibrationPoints = new List<PointF>();
        private readonly List<PointF> bikeCalibrationPoints = new List<PointF>();
        private readonly List<PointF> wheelPerspectivePoints = new List<PointF>();
        private readonly List<PointF> verificationPoints = new List<PointF>();
        private readonly List<PointF> levelReferencePoints = new List<PointF>();
        private readonly List<PointF> landmarkPoints = new List<PointF>();
        private readonly string[] basicLandmarkNames = new string[]
        {
            "Bottom bracket center",
            "Saddle top",
            "Saddle tip",
            "Hood hand-contact point"
        };
        private readonly string[] advancedLandmarkNames = new string[]
        {
            "Bottom bracket center",
            "Saddle top",
            "Saddle tip",
            "Hood hand-contact point",
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
        private Label verificationLabel;
        private Label referenceLabel;
        private Label resultsLabel;
        private Label progressLabel;
        private Button primaryAction;
        private Button levelReference;
        private Button verifyCalibration;
        private Button undoLast;
        private Button recalculate;
        private Button flipSetbackSign;
        private Button saveBefore;
        private Button saveAfter;
        private CheckBox advancedLandmarks;
        private ComboBox handlebarReferenceMode;
        private NumericUpDown handlebarDiameter;
        private CheckBox useTapeSaddleTipToGrip;
        private NumericUpDown tapeSaddleTipToGrip;
        private ComboBox calibrationMethod;
        private NumericUpDown knownWheelbase;
        private NumericUpDown knownTireDiameter;
        private NumericUpDown verifiedSaddleHeight;
        private Button calibrateVerticalScale;
        private Label verticalCalibrationLabel;
        private Button calibrateButton;
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
        private double horizontalMillimetersPerPixel;
        private double verticalMillimetersPerPixel;
        private double[] perspectiveTransform;
        private double perspectiveResidualMillimeters = double.NaN;
        private double knownCalibrationMillimeters;
        private double verificationErrorPercent = double.NaN;
        private double verticalScaleCorrection = 1.0;
        private bool verticalScaleCalibrated;
        private string calibrationVerificationStatus = "Not verified";
        private string cameraProfileName = "Standard camera · 70–90°";
        private Dictionary<string, string> calculatedValues = new Dictionary<string, string>();
        private bool landmarksSuggested;
        private double landmarkSuggestionConfidence;

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
        public string AssistedLandmarkSummary { get; private set; }
        public string AnnotatedImagePath { get; private set; }

        public BikeMetricGuidedCaptureForm(string imagePath)
            : this(imagePath, string.IsNullOrEmpty(imagePath) ? null : Path.GetDirectoryName(imagePath))
        {
        }

        public BikeMetricGuidedCaptureForm(string imagePath, string outputDirectory)
            : this(imagePath, outputDirectory, null)
        {
        }

        public BikeMetricGuidedCaptureForm(string imagePath, string outputDirectory, string preferredSide)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException("imagePath");
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("The measurement reference image could not be found.", imagePath);

            this.imagePath = imagePath;
            this.outputDirectory = string.IsNullOrWhiteSpace(outputDirectory) ? Path.GetDirectoryName(imagePath) : outputDirectory;
            this.preferredSide = string.Equals(preferredSide, "Before", StringComparison.OrdinalIgnoreCase) ? "Before" : string.Equals(preferredSide, "After", StringComparison.OrdinalIgnoreCase) ? "After" : string.Empty;
            CameraSetupStatus = "Not confirmed";

            Text = "Cassette Motion Pro - Guided Measurements";
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
                "1. Confirm the camera profile and setup.\n" +
                "2. Choose Dual-wheel Perspective, Quick Bike Reference, or Known Reference.\n" +
                "3. Dual-wheel: enter wheelbase, leave 700 for a 700c wheel, then click eight wheel points.\n" +
                "4. Optional: set a floor or axle level reference.\n" +
                "5. Suggest Bike Landmarks, or place them manually.\n" +
                "   • Bottom bracket center\n" +
                "   • Saddle top\n" +
                "   • Saddle tip\n" +
                "   • Hood hand-contact point (top of rubber hood where the palm rests)\n" +
                "   • Advanced: click bar center or an edge using its diameter\n" +
                "5. Confirm and drag every orange point to fine-tune it.\n" +
                "6. Review confidence, then save to Before or After.";
            guide.Dock = DockStyle.Top;
            guide.Height = 190;
            guide.ForeColor = Color.FromArgb(74, 87, 81);

            progressLabel = new Label();
            progressLabel.Text = "STEP 1 OF 5 · Confirm camera setup";
            progressLabel.Dock = DockStyle.Top;
            progressLabel.Height = 36;
            progressLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            progressLabel.ForeColor = Color.White;
            progressLabel.BackColor = Color.FromArgb(60, 145, 76);
            progressLabel.Padding = new Padding(10, 8, 10, 6);

            status = new Label();
            status.Text = "Start with Dual-wheel Perspective calibration.";
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

            verificationLabel = new Label();
            verificationLabel.Text = "Verification: not completed";
            verificationLabel.Dock = DockStyle.Top;
            verificationLabel.Height = 34;
            verificationLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            verificationLabel.ForeColor = Color.FromArgb(166, 92, 34);

            referenceLabel = new Label();
            referenceLabel.Text = "Level reference: not set";
            referenceLabel.Dock = DockStyle.Top;
            referenceLabel.Height = 30;
            referenceLabel.ForeColor = Color.FromArgb(92, 104, 98);

            resultsLabel = new Label();
            resultsLabel.Text = "Calculated metrics:\n--";
            resultsLabel.Dock = DockStyle.Top;
            resultsLabel.Height = 286;
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

            TableLayoutPanel handlebarReferencePanel = new TableLayoutPanel();
            handlebarReferencePanel.Dock = DockStyle.Top;
            handlebarReferencePanel.Height = 82;
            handlebarReferencePanel.ColumnCount = 2;
            handlebarReferencePanel.RowCount = 2;
            handlebarReferencePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            handlebarReferencePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            handlebarReferencePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            handlebarReferencePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            handlebarReferencePanel.BackColor = Color.FromArgb(247, 250, 244);
            handlebarReferencePanel.Padding = new Padding(4);
            Label handlebarModeLabel = new Label();
            handlebarModeLabel.Text = "Handlebar point:";
            handlebarModeLabel.Dock = DockStyle.Fill;
            handlebarModeLabel.TextAlign = ContentAlignment.MiddleLeft;
            handlebarReferenceMode = new ComboBox();
            handlebarReferenceMode.DropDownStyle = ComboBoxStyle.DropDownList;
            handlebarReferenceMode.Items.AddRange(new object[] { "Bar center", "Rear edge → calculate center", "Front edge → calculate center" });
            handlebarReferenceMode.SelectedIndex = 1;
            handlebarReferenceMode.Dock = DockStyle.Fill;
            handlebarReferenceMode.Margin = new Padding(3, 5, 3, 3);
            handlebarReferenceMode.Enabled = false;
            handlebarReferenceMode.SelectedIndexChanged += HandlebarReferenceChanged;
            Label diameterLabel = new Label();
            diameterLabel.Text = "Bar diameter (mm):";
            diameterLabel.Dock = DockStyle.Fill;
            diameterLabel.TextAlign = ContentAlignment.MiddleLeft;
            handlebarDiameter = new NumericUpDown();
            handlebarDiameter.DecimalPlaces = 1;
            handlebarDiameter.Minimum = 20;
            handlebarDiameter.Maximum = 60;
            handlebarDiameter.Increment = 0.1M;
            handlebarDiameter.Value = 31.8M;
            handlebarDiameter.Width = 72;
            handlebarDiameter.Anchor = AnchorStyles.Left;
            handlebarDiameter.Enabled = false;
            handlebarDiameter.ValueChanged += HandlebarReferenceChanged;
            handlebarReferencePanel.Controls.Add(handlebarModeLabel, 0, 0);
            handlebarReferencePanel.Controls.Add(handlebarReferenceMode, 1, 0);
            handlebarReferencePanel.Controls.Add(diameterLabel, 0, 1);
            handlebarReferencePanel.Controls.Add(handlebarDiameter, 1, 1);

            TableLayoutPanel reachOverridePanel = new TableLayoutPanel();
            reachOverridePanel.Dock = DockStyle.Top;
            reachOverridePanel.Height = 76;
            reachOverridePanel.ColumnCount = 2;
            reachOverridePanel.RowCount = 2;
            reachOverridePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            reachOverridePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            reachOverridePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            reachOverridePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            reachOverridePanel.BackColor = Color.FromArgb(248, 252, 238);
            reachOverridePanel.Padding = new Padding(4);
            useTapeSaddleTipToGrip = new CheckBox();
            useTapeSaddleTipToGrip.Text = "Use tape value when camera view is angled";
            useTapeSaddleTipToGrip.Dock = DockStyle.Fill;
            useTapeSaddleTipToGrip.CheckedChanged += SaddleTipToGripOverrideChanged;
            Label tapeReachLabel = new Label();
            tapeReachLabel.Text = "Saddle tip → hood (mm):";
            tapeReachLabel.Dock = DockStyle.Fill;
            tapeReachLabel.TextAlign = ContentAlignment.MiddleLeft;
            tapeSaddleTipToGrip = new NumericUpDown();
            tapeSaddleTipToGrip.DecimalPlaces = 1;
            tapeSaddleTipToGrip.Minimum = 200;
            tapeSaddleTipToGrip.Maximum = 1000;
            tapeSaddleTipToGrip.Increment = 1;
            tapeSaddleTipToGrip.Value = 600;
            tapeSaddleTipToGrip.Width = 94;
            tapeSaddleTipToGrip.Anchor = AnchorStyles.Left;
            tapeSaddleTipToGrip.Enabled = false;
            tapeSaddleTipToGrip.ValueChanged += SaddleTipToGripOverrideChanged;
            reachOverridePanel.Controls.Add(useTapeSaddleTipToGrip, 0, 0);
            reachOverridePanel.SetColumnSpan(useTapeSaddleTipToGrip, 2);
            reachOverridePanel.Controls.Add(tapeReachLabel, 0, 1);
            reachOverridePanel.Controls.Add(tapeSaddleTipToGrip, 1, 1);

            TableLayoutPanel verticalCalibrationPanel = new TableLayoutPanel();
            verticalCalibrationPanel.Dock = DockStyle.Top;
            verticalCalibrationPanel.Height = 108;
            verticalCalibrationPanel.ColumnCount = 2;
            verticalCalibrationPanel.RowCount = 3;
            verticalCalibrationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            verticalCalibrationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            verticalCalibrationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            verticalCalibrationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            verticalCalibrationPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            verticalCalibrationPanel.BackColor = Color.FromArgb(238, 247, 219);
            verticalCalibrationPanel.Padding = new Padding(4);
            Label verifiedSaddleHeightLabel = new Label();
            verifiedSaddleHeightLabel.Text = "Tape saddle height (mm):";
            verifiedSaddleHeightLabel.Dock = DockStyle.Fill;
            verifiedSaddleHeightLabel.TextAlign = ContentAlignment.MiddleLeft;
            verifiedSaddleHeight = new NumericUpDown();
            verifiedSaddleHeight.DecimalPlaces = 1;
            verifiedSaddleHeight.Minimum = 400;
            verifiedSaddleHeight.Maximum = 1000;
            verifiedSaddleHeight.Increment = 1;
            verifiedSaddleHeight.Value = 684;
            verifiedSaddleHeight.Width = 94;
            verifiedSaddleHeight.Anchor = AnchorStyles.Left;
            calibrateVerticalScale = CreateButton("Calibrate Vertical Scale", true);
            calibrateVerticalScale.Dock = DockStyle.Fill;
            calibrateVerticalScale.Enabled = false;
            calibrateVerticalScale.Click += CalibrateVerticalScale_Click;
            verticalCalibrationLabel = new Label();
            verticalCalibrationLabel.Text = "Vertical scale: not tape calibrated";
            verticalCalibrationLabel.Dock = DockStyle.Fill;
            verticalCalibrationLabel.TextAlign = ContentAlignment.MiddleLeft;
            verticalCalibrationLabel.ForeColor = Color.FromArgb(92, 104, 98);
            verticalCalibrationPanel.Controls.Add(verifiedSaddleHeightLabel, 0, 0);
            verticalCalibrationPanel.Controls.Add(verifiedSaddleHeight, 1, 0);
            verticalCalibrationPanel.Controls.Add(calibrateVerticalScale, 0, 1);
            verticalCalibrationPanel.SetColumnSpan(calibrateVerticalScale, 2);
            verticalCalibrationPanel.Controls.Add(verticalCalibrationLabel, 0, 2);
            verticalCalibrationPanel.SetColumnSpan(verticalCalibrationLabel, 2);

            TableLayoutPanel calibrationOptions = new TableLayoutPanel();
            calibrationOptions.Dock = DockStyle.Top;
            calibrationOptions.Height = 116;
            calibrationOptions.ColumnCount = 2;
            calibrationOptions.RowCount = 3;
            calibrationOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            calibrationOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            calibrationOptions.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
            calibrationOptions.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            calibrationOptions.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
            calibrationOptions.BackColor = Color.FromArgb(238, 247, 219);
            calibrationOptions.Padding = new Padding(4);
            Label calibrationMethodLabel = new Label();
            calibrationMethodLabel.Text = "Calibration:";
            calibrationMethodLabel.Dock = DockStyle.Fill;
            calibrationMethodLabel.TextAlign = ContentAlignment.MiddleLeft;
            calibrationMethod = new ComboBox();
            calibrationMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            calibrationMethod.Items.AddRange(new object[]
            {
                "Dual-wheel perspective",
                "Quick bike reference",
                "Known reference / board"
            });
            calibrationMethod.SelectedIndex = 0;
            calibrationMethod.Dock = DockStyle.Fill;
            calibrationMethod.Margin = new Padding(3, 5, 3, 3);
            calibrationMethod.SelectedIndexChanged += CalibrationMethodChanged;
            Label wheelbaseLabel = new Label();
            wheelbaseLabel.Text = "Wheelbase (mm):";
            wheelbaseLabel.Dock = DockStyle.Fill;
            wheelbaseLabel.TextAlign = ContentAlignment.MiddleLeft;
            knownWheelbase = CreateCalibrationNumber(700, 1400, 1050);
            Label tireDiameterLabel = new Label();
            tireDiameterLabel.Text = "Wheel preset (700c = 700):";
            tireDiameterLabel.Dock = DockStyle.Fill;
            tireDiameterLabel.TextAlign = ContentAlignment.MiddleLeft;
            knownTireDiameter = CreateCalibrationNumber(400, 900, 700);
            knownWheelbase.ValueChanged += BikeCalibrationDimensionChanged;
            knownTireDiameter.ValueChanged += BikeCalibrationDimensionChanged;
            calibrationOptions.Controls.Add(calibrationMethodLabel, 0, 0);
            calibrationOptions.Controls.Add(calibrationMethod, 1, 0);
            calibrationOptions.Controls.Add(wheelbaseLabel, 0, 1);
            calibrationOptions.Controls.Add(knownWheelbase, 1, 1);
            calibrationOptions.Controls.Add(tireDiameterLabel, 0, 2);
            calibrationOptions.Controls.Add(knownTireDiameter, 1, 2);

            Button cameraSetup = CreateButton("1. Camera Setup", false);
            calibrateButton = CreateButton("2. Calibrate From Both Wheels", false);
            verifyCalibration = CreateButton("3. Verify Calibration", true);
            levelReference = CreateButton("4. Level Reference (optional)", false);
            Button capture = CreateButton("5. Start Guided Capture", false);
            Button suggest = CreateButton("Suggest Bike Landmarks", true);
            primaryAction = CreateButton("Continue: Camera Setup", true);
            undoLast = CreateButton("Undo Last Point", false);
            Button clear = CreateButton("Clear Points", false);
            recalculate = CreateButton("Recalculate Values", false);
            flipSetbackSign = CreateButton("Flip Setback Sign", false);
            saveBefore = CreateButton("Save to Before", string.Equals(preferredSide, "Before", StringComparison.OrdinalIgnoreCase));
            saveAfter = CreateButton("Save to After", string.IsNullOrEmpty(preferredSide) || string.Equals(preferredSide, "After", StringComparison.OrdinalIgnoreCase));
            if (string.Equals(preferredSide, "Before", StringComparison.OrdinalIgnoreCase))
            {
                saveBefore.Text = "Save Before Bike Measurements";
                saveAfter.Visible = false;
            }
            else if (string.Equals(preferredSide, "After", StringComparison.OrdinalIgnoreCase))
            {
                saveAfter.Text = "Save After Bike Measurements";
                saveBefore.Visible = false;
            }
            cameraSetup.Dock = DockStyle.Top;
            primaryAction.Dock = DockStyle.Top;
            calibrateButton.Dock = DockStyle.Top;
            verifyCalibration.Dock = DockStyle.Top;
            levelReference.Dock = DockStyle.Top;
            capture.Dock = DockStyle.Top;
            suggest.Dock = DockStyle.Top;
            undoLast.Dock = DockStyle.Top;
            clear.Dock = DockStyle.Top;
            recalculate.Dock = DockStyle.Top;
            flipSetbackSign.Dock = DockStyle.Top;
            saveBefore.Dock = DockStyle.Top;
            saveAfter.Dock = DockStyle.Top;
            cameraSetup.Height = 34;
            primaryAction.Height = 42;
            calibrateButton.Height = 34;
            verifyCalibration.Height = 38;
            levelReference.Height = 34;
            capture.Height = 34;
            suggest.Height = 42;
            undoLast.Height = 34;
            clear.Height = 34;
            recalculate.Height = 34;
            flipSetbackSign.Height = 34;
            saveBefore.Height = 34;
            saveAfter.Height = 34;
            cameraSetup.Margin = new Padding(0, 6, 0, 0);
            primaryAction.Margin = new Padding(0, 8, 0, 4);
            calibrateButton.Margin = new Padding(0, 6, 0, 0);
            verifyCalibration.Margin = new Padding(0, 6, 0, 0);
            levelReference.Margin = new Padding(0, 6, 0, 0);
            capture.Margin = new Padding(0, 6, 0, 0);
            suggest.Margin = new Padding(0, 6, 0, 0);
            undoLast.Margin = new Padding(0, 6, 0, 0);
            clear.Margin = new Padding(0, 6, 0, 0);
            recalculate.Margin = new Padding(0, 6, 0, 0);
            flipSetbackSign.Margin = new Padding(0, 6, 0, 0);
            saveBefore.Margin = new Padding(0, 6, 0, 0);
            saveAfter.Margin = new Padding(0, 6, 0, 0);
            cameraSetup.Click += CameraSetup_Click;
            primaryAction.Click += PrimaryAction_Click;
            calibrateButton.Click += Calibrate_Click;
            verifyCalibration.Click += VerifyCalibration_Click;
            levelReference.Click += LevelReference_Click;
            capture.Click += Capture_Click;
            suggest.Click += SuggestLandmarks_Click;
            undoLast.Click += UndoLast_Click;
            clear.Click += Clear_Click;
            recalculate.Click += Recalculate_Click;
            flipSetbackSign.Click += FlipSetbackSign_Click;
            saveBefore.Click += delegate { SaveResult("Before"); };
            saveAfter.Click += delegate { SaveResult("After"); };
            levelReference.Enabled = false;
            verifyCalibration.Enabled = false;
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
            sideScroll.Controls.Add(suggest);
            sideScroll.Controls.Add(levelReference);
            sideScroll.Controls.Add(verifyCalibration);
            sideScroll.Controls.Add(calibrateButton);
            sideScroll.Controls.Add(calibrationOptions);
            sideScroll.Controls.Add(cameraSetup);
            sideScroll.Controls.Add(primaryAction);
            sideScroll.Controls.Add(advancedLandmarks);
            sideScroll.Controls.Add(handlebarReferencePanel);
            sideScroll.Controls.Add(reachOverridePanel);
            sideScroll.Controls.Add(verticalCalibrationPanel);
            sideScroll.Controls.Add(zoomPanel);
            sideScroll.Controls.Add(resultsLabel);
            sideScroll.Controls.Add(referenceLabel);
            sideScroll.Controls.Add(verificationLabel);
            sideScroll.Controls.Add(scaleLabel);
            sideScroll.Controls.Add(nextPointHintLabel);
            sideScroll.Controls.Add(currentLandmarkLabel);
            sideScroll.Controls.Add(status);
            sideScroll.Controls.Add(guide);
            sideScroll.Controls.Add(progressLabel);
            sideScroll.Controls.Add(title);
            sideScroll.Controls.Add(eyebrow);

            side.Controls.Add(sideScroll, 0, 0);
            side.Controls.Add(close, 0, 1);

            root.Controls.Add(picture, 0, 0);
            root.Controls.Add(side, 1, 0);
            Controls.Add(root);
            UpdateWizardProgress();
        }

        private void PrimaryAction_Click(object sender, EventArgs e)
        {
            if (!IsCameraSetupConfirmed())
            {
                CameraSetup_Click(sender, e);
                return;
            }

            if (millimetersPerPixel <= 0)
            {
                Calibrate_Click(sender, e);
                return;
            }

            if (double.IsNaN(verificationErrorPercent))
            {
                VerifyCalibration_Click(sender, e);
                return;
            }

            if (landmarkPoints.Count < ActiveLandmarkNames.Length)
            {
                if (mode != ClickMode.Landmarks)
                    Capture_Click(sender, e);
                return;
            }

            Recalculate_Click(sender, e);
        }

        private void CalibrationMethodChanged(object sender, EventArgs e)
        {
            bool usesBikeDimensions = calibrationMethod.SelectedIndex <= 1;
            bool wheelPerspective = calibrationMethod.SelectedIndex == 0;
            knownWheelbase.Enabled = usesBikeDimensions;
            knownTireDiameter.Enabled = usesBikeDimensions;
            calibrateButton.Text = wheelPerspective ? "2. Calibrate From Both Wheels" : calibrationMethod.SelectedIndex == 1 ? "2. Quick Bike Calibration" : "2. Calibrate Known Reference";
            verifyCalibration.Text = usesBikeDimensions ? "3. Bike Scale Check" : "3. Verify Calibration";
            millimetersPerPixel = 0;
            horizontalMillimetersPerPixel = 0;
            verticalMillimetersPerPixel = 0;
            perspectiveTransform = null;
            perspectiveResidualMillimeters = double.NaN;
            ResetVerticalScaleCalibration();
            verificationErrorPercent = double.NaN;
            mode = ClickMode.None;
            calibrationPoints.Clear();
            bikeCalibrationPoints.Clear();
            wheelPerspectivePoints.Clear();
            verificationPoints.Clear();
            levelReferencePoints.Clear();
            landmarkPoints.Clear();
            calculatedValues.Clear();
            scaleLabel.Text = "Scale: not calibrated";
            verificationLabel.Text = "Verification: not completed";
            referenceLabel.Text = "Level reference: not set";
            resultsLabel.Text = "Calculated metrics:\n--";
            levelReference.Enabled = false;
            verifyCalibration.Enabled = false;
            undoLast.Enabled = false;
            recalculate.Enabled = false;
            flipSetbackSign.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            status.Text = wheelPerspective
                ? "Dual-wheel perspective selected. Enter wheelbase; leave the wheel preset at 700 for 700c."
                : calibrationMethod.SelectedIndex == 1
                    ? "Quick bike reference selected. Enter wheelbase and outside tire diameter."
                    : "Known reference selected. Use a measured line or calibration board in the bike plane.";
            currentLandmarkLabel.Text = "Current point: --";
            nextPointHintLabel.Text = "Confirm the entered reference dimensions, then start calibration.";
            UpdateWizardProgress();
            picture.Invalidate();
        }

        private void BikeCalibrationDimensionChanged(object sender, EventArgs e)
        {
            if (calibrationMethod == null || calibrationMethod.SelectedIndex > 1)
                return;
            if (millimetersPerPixel <= 0 && bikeCalibrationPoints.Count == 0 && wheelPerspectivePoints.Count == 0)
                return;

            CalibrationMethodChanged(sender, e);
            status.Text = "Bike dimensions changed. Run calibration again so saved measurements use the new values.";
        }

        private void UpdateWizardProgress()
        {
            if (progressLabel == null || primaryAction == null)
                return;

            if (!IsCameraSetupConfirmed())
            {
                progressLabel.Text = "STEP 1 OF 5 · Confirm camera setup";
                primaryAction.Text = "Continue: Camera Setup";
                return;
            }

            if (millimetersPerPixel <= 0)
            {
                int calibrationIndex = calibrationMethod == null ? 0 : calibrationMethod.SelectedIndex;
                progressLabel.Text = calibrationIndex == 0 ? "STEP 2 OF 5 · Correct perspective from both wheels" : calibrationIndex == 1 ? "STEP 2 OF 5 · Quick wheelbase + tire calibration" : "STEP 2 OF 5 · Calibrate one known distance";
                primaryAction.Text = calibrationIndex == 0 ? "Continue: Dual-wheel Calibration" : calibrationIndex == 1 ? "Continue: Quick Calibration" : "Continue: Calibrate Scale";
                return;
            }

            if (double.IsNaN(verificationErrorPercent))
            {
                progressLabel.Text = "STEP 3 OF 5 · Verify with a second known distance";
                primaryAction.Text = "Continue: Verify Calibration";
                return;
            }

            if (landmarkPoints.Count < ActiveLandmarkNames.Length)
            {
                progressLabel.Text = "STEP 4 OF 5 · Place landmarks (" + landmarkPoints.Count.ToString(CultureInfo.InvariantCulture) + "/" + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ")";
                primaryAction.Text = mode == ClickMode.Landmarks ? "Follow the highlighted point" : "Continue: Start Landmarks";
                return;
            }

            progressLabel.Text = "STEP 5 OF 5 · Review confidence and save";
            primaryAction.Text = "Recalculate Measurements";
        }

        private void CameraSetup_Click(object sender, EventArgs e)
        {
            cameraProfileName = PromptForCameraProfile(this, cameraProfileName);
            string checklist =
                "For the most accurate bike measurements:\n\n" +
                "✓ Camera is straight side-on to the bike.\n" +
                "✓ Camera is level, not tilted.\n" +
                "✓ Bike is upright and not leaning.\n" +
                "✓ Use 2x/telephoto or step farther back if possible.\n" +
                "✓ Avoid ultra-wide lens distortion.\n" +
                "✓ The calibration length is in the same plane as the bike.\n" +
                "✓ Use Level Reference if the image is slightly tilted.\n\n" +
                "Selected profile: " + cameraProfileName + "\n" +
                (cameraProfileName.IndexOf("120°", StringComparison.OrdinalIgnoreCase) >= 0 ? "⚠ Ultra-wide lens: keep the entire bike near the image center.\n" : string.Empty) +
                "\n" +
                "Confirm camera setup for this Guided Capture?";

            DialogResult result = MessageBox.Show(this,
                checklist,
                "Camera Setup / Accuracy Checklist",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            CameraSetupStatus = (result == DialogResult.Yes ? "Confirmed · " : "Not confirmed · ") + cameraProfileName;
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
            UpdateWizardProgress();
        }

        private void Calibrate_Click(object sender, EventArgs e)
        {
            if (calibrationMethod.SelectedIndex == 0)
            {
                StartWheelPerspectiveCalibration();
                return;
            }

            if (calibrationMethod.SelectedIndex == 1)
            {
                StartBikeCalibration();
                return;
            }

            mode = ClickMode.Calibration;
            calibrationPoints.Clear();
            bikeCalibrationPoints.Clear();
            wheelPerspectivePoints.Clear();
            verificationPoints.Clear();
            levelReferencePoints.Clear();
            landmarkPoints.Clear();
            calculatedValues.Clear();
            millimetersPerPixel = 0;
            horizontalMillimetersPerPixel = 0;
            verticalMillimetersPerPixel = 0;
            perspectiveTransform = null;
            perspectiveResidualMillimeters = double.NaN;
            knownCalibrationMillimeters = 0;
            verificationErrorPercent = double.NaN;
            calibrationVerificationStatus = "Not verified";
            verificationLabel.Text = "Verification: not completed";
            verificationLabel.ForeColor = Color.FromArgb(166, 92, 34);
            levelReference.Enabled = false;
            verifyCalibration.Enabled = false;
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
            UpdateWizardProgress();
        }

        private void StartBikeCalibration()
        {
            ResetVerticalScaleCalibration();
            mode = ClickMode.BikeCalibration;
            bikeCalibrationPoints.Clear();
            wheelPerspectivePoints.Clear();
            calibrationPoints.Clear();
            verificationPoints.Clear();
            levelReferencePoints.Clear();
            landmarkPoints.Clear();
            calculatedValues.Clear();
            millimetersPerPixel = 0;
            horizontalMillimetersPerPixel = 0;
            verticalMillimetersPerPixel = 0;
            perspectiveTransform = null;
            perspectiveResidualMillimeters = double.NaN;
            knownCalibrationMillimeters = Decimal.ToDouble(knownWheelbase.Value);
            verificationErrorPercent = double.NaN;
            calibrationVerificationStatus = "Not verified";
            verificationLabel.Text = "Bike scale check: waiting for four points";
            verificationLabel.ForeColor = Color.FromArgb(166, 92, 34);
            levelReference.Enabled = false;
            verifyCalibration.Enabled = false;
            undoLast.Enabled = false;
            recalculate.Enabled = false;
            flipSetbackSign.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            referenceLabel.Text = "Level reference: axle line supplied by bike calibration";
            status.Text = "Bike calibration: click the REAR axle center.";
            currentLandmarkLabel.Text = "Bike calibration point 1 of 4: rear axle center";
            nextPointHintLabel.Text = "Use the exact center of the rear wheel axle.";
            picture.Invalidate();
            UpdateWizardProgress();
        }

        private void StartWheelPerspectiveCalibration()
        {
            ResetVerticalScaleCalibration();
            mode = ClickMode.WheelPerspectiveCalibration;
            wheelPerspectivePoints.Clear();
            bikeCalibrationPoints.Clear();
            calibrationPoints.Clear();
            verificationPoints.Clear();
            levelReferencePoints.Clear();
            landmarkPoints.Clear();
            calculatedValues.Clear();
            millimetersPerPixel = 0;
            horizontalMillimetersPerPixel = 0;
            verticalMillimetersPerPixel = 0;
            perspectiveTransform = null;
            perspectiveResidualMillimeters = double.NaN;
            knownCalibrationMillimeters = Decimal.ToDouble(knownWheelbase.Value);
            verificationErrorPercent = double.NaN;
            calibrationVerificationStatus = "Not verified";
            verificationLabel.Text = "Perspective check: waiting for eight wheel points";
            verificationLabel.ForeColor = Color.FromArgb(166, 92, 34);
            levelReference.Enabled = false;
            verifyCalibration.Enabled = false;
            undoLast.Enabled = false;
            recalculate.Enabled = false;
            flipSetbackSign.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            referenceLabel.Text = "Level reference: supplied by both axle centers";
            status.Text = "Perspective calibration: click the REAR axle center.";
            currentLandmarkLabel.Text = "Wheel point 1 of 8: rear axle center";
            nextPointHintLabel.Text = "Zoom in and click the exact center of the rear axle.";
            picture.Invalidate();
            UpdateWizardProgress();
        }

        private void AddWheelPerspectivePoint(PointF imagePoint)
        {
            wheelPerspectivePoints.Add(imagePoint);
            undoLast.Enabled = true;
            string[] names = GetWheelPerspectivePointNames();
            if (wheelPerspectivePoints.Count < names.Length)
            {
                int next = wheelPerspectivePoints.Count;
                status.Text = "Perspective calibration: click the " + names[next] + ".";
                currentLandmarkLabel.Text = "Wheel point " + (next + 1).ToString(CultureInfo.InvariantCulture) + " of 8: " + names[next];
                nextPointHintLabel.Text = next == 1 || next == 5
                    ? "Click the outer tread directly above the axle center."
                    : "Click the outer tire edge on the horizontal line through that axle. Use the magnifier for exact placement.";
                picture.Invalidate();
                return;
            }

            double wheelbase = Decimal.ToDouble(knownWheelbase.Value);
            double diameter = Decimal.ToDouble(knownTireDiameter.Value);
            bool frontWheelIsRightOfRear = wheelPerspectivePoints[4].X >= wheelPerspectivePoints[0].X;
            List<PointF> idealPoints = BuildIdealWheelPoints(wheelbase, diameter, frontWheelIsRightOfRear);
            double[] transform;
            double residual;
            if (!TryBuildPerspectiveTransform(wheelPerspectivePoints, idealPoints, out transform, out residual))
            {
                MessageBox.Show(this, "The wheel points could not produce a stable perspective correction. Recheck the axle centers and tire edges.", "Perspective calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                StartWheelPerspectiveCalibration();
                return;
            }
            if (residual > 25.0)
            {
                MessageBox.Show(this,
                    "The wheel reference points disagree by " + residual.ToString("0.0", CultureInfo.InvariantCulture) + " mm. Measurements would not be reliable, so calibration cannot continue.\n\nConfirm that each left/right tire point is on the axle-height guide. For a 700c wheel, leave the wheel preset at 700.",
                    "Wheel calibration needs review",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                StartWheelPerspectiveCalibration();
                return;
            }

            PointF rearUpperImage = new PointF(
                wheelPerspectivePoints[0].X + 2F * (wheelPerspectivePoints[1].X - wheelPerspectivePoints[0].X),
                wheelPerspectivePoints[0].Y + 2F * (wheelPerspectivePoints[1].Y - wheelPerspectivePoints[0].Y));
            PointF frontUpperImage = new PointF(
                wheelPerspectivePoints[4].X + 2F * (wheelPerspectivePoints[5].X - wheelPerspectivePoints[4].X),
                wheelPerspectivePoints[4].Y + 2F * (wheelPerspectivePoints[5].Y - wheelPerspectivePoints[4].Y));
            PointF rearUpper = PointF.Empty;
            PointF frontUpper = PointF.Empty;
            PointF transformedRearAxle = PointF.Empty;
            PointF transformedFrontAxle = PointF.Empty;
            bool stableAboveWheels = TryApplyPerspectiveTransform(transform, rearUpperImage, out rearUpper) &&
                TryApplyPerspectiveTransform(transform, frontUpperImage, out frontUpper) &&
                TryApplyPerspectiveTransform(transform, wheelPerspectivePoints[0], out transformedRearAxle) &&
                TryApplyPerspectiveTransform(transform, wheelPerspectivePoints[4], out transformedFrontAxle);
            if (!stableAboveWheels ||
                Distance(rearUpper, transformedRearAxle) < diameter * 0.5 ||
                Distance(rearUpper, transformedRearAxle) > diameter * 1.5 ||
                Distance(frontUpper, transformedFrontAxle) < diameter * 0.5 ||
                Distance(frontUpper, transformedFrontAxle) > diameter * 1.5 ||
                Math.Abs(frontUpper.X - rearUpper.X) < wheelbase * 0.5 ||
                Math.Abs(frontUpper.X - rearUpper.X) > wheelbase * 1.5)
            {
                MessageBox.Show(this, "The perspective correction is unstable above the wheels and would make saddle measurements inaccurate. Recheck the axle and tire-top points, or use Quick Bike Reference.", "Perspective calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                StartWheelPerspectiveCalibration();
                return;
            }

            perspectiveTransform = transform;
            perspectiveResidualMillimeters = residual;
            PointF imageCenter = new PointF(loadedImage.Width / 2F, loadedImage.Height / 2F);
            PointF centerMillimeters = ApplyPerspectiveTransform(imageCenter);
            PointF onePixelRight = ApplyPerspectiveTransform(new PointF(imageCenter.X + 1F, imageCenter.Y));
            PointF onePixelDown = ApplyPerspectiveTransform(new PointF(imageCenter.X, imageCenter.Y + 1F));
            horizontalMillimetersPerPixel = Distance(centerMillimeters, onePixelRight);
            verticalMillimetersPerPixel = Distance(centerMillimeters, onePixelDown);
            if (horizontalMillimetersPerPixel < 0.01 || horizontalMillimetersPerPixel > 20 || verticalMillimetersPerPixel < 0.01 || verticalMillimetersPerPixel > 20)
            {
                MessageBox.Show(this, "The perspective correction produced an unrealistic scale. Confirm the point order and run the eight wheel points again.", "Perspective calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                StartWheelPerspectiveCalibration();
                return;
            }
            millimetersPerPixel = (horizontalMillimetersPerPixel + verticalMillimetersPerPixel) / 2.0;
            verificationErrorPercent = residual / Math.Max(1.0, diameter) * 100.0;
            string grade = residual <= 3.0 ? "HIGH" : residual <= 7.0 ? "MODERATE" : "REVIEW";
            calibrationVerificationStatus = "DUAL-WHEEL · " + grade + " · fit residual " + residual.ToString("0.0", CultureInfo.InvariantCulture) + " mm";
            scaleLabel.Text = "Perspective scale near image center: X " + horizontalMillimetersPerPixel.ToString("0.0000", CultureInfo.InvariantCulture) + " · Y " + verticalMillimetersPerPixel.ToString("0.0000", CultureInfo.InvariantCulture) + " mm/pixel";
            verificationLabel.Text = "Perspective check: " + calibrationVerificationStatus;
            verificationLabel.ForeColor = residual <= 3.0 ? Color.FromArgb(60, 145, 76) : residual <= 7.0 ? Color.FromArgb(166, 92, 34) : Color.FromArgb(176, 52, 52);
            status.Text = residual <= 7.0
                ? "Perspective correction complete. Calibration is now locked; continue to landmarks."
                : "Perspective fit needs review. Recheck the camera angle and all eight wheel points.";
            currentLandmarkLabel.Text = "Dual-wheel calibration complete";
            nextPointHintLabel.Text = "Measurements will use the corrected bike-plane coordinates. Expected precision is shown in the perspective check.";
            levelReferencePoints.Add(wheelPerspectivePoints[0]);
            levelReferencePoints.Add(wheelPerspectivePoints[4]);
            referenceLabel.Text = "Level reference: rear-to-front axle line locked";
            levelReference.Enabled = false;
            verifyCalibration.Enabled = false;
            undoLast.Enabled = false;
            mode = ClickMode.None;
            picture.Invalidate();
            UpdateWizardProgress();
        }

        private void AddBikeCalibrationPoint(PointF imagePoint)
        {
            bikeCalibrationPoints.Add(imagePoint);
            undoLast.Enabled = true;
            string[] names = new string[] { "rear axle center", "front axle center", "top of tire", "bottom of tire" };
            if (bikeCalibrationPoints.Count < 4)
            {
                int next = bikeCalibrationPoints.Count;
                status.Text = "Bike calibration: click the " + names[next] + ".";
                currentLandmarkLabel.Text = "Bike calibration point " + (next + 1).ToString(CultureInfo.InvariantCulture) + " of 4: " + names[next];
                nextPointHintLabel.Text = next == 1
                    ? "The two axle centers set the horizontal scale from the measured wheelbase."
                    : "Use the same wheel for the top and bottom tire points; click the outside tread edges.";
                picture.Invalidate();
                return;
            }

            PointF rearAxle = bikeCalibrationPoints[0];
            PointF frontAxle = bikeCalibrationPoints[1];
            PointF tireTop = bikeCalibrationPoints[2];
            PointF tireBottom = bikeCalibrationPoints[3];
            double wheelbasePixels = Distance(rearAxle, frontAxle);
            double tirePixels = Distance(tireTop, tireBottom);
            if (wheelbasePixels < 20 || tirePixels < 20)
            {
                MessageBox.Show(this, "The bike calibration points are too close together. Recheck both axle centers and the outside top/bottom tire edges.", "Bike calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                StartBikeCalibration();
                return;
            }

            horizontalMillimetersPerPixel = Decimal.ToDouble(knownWheelbase.Value) / wheelbasePixels;
            verticalMillimetersPerPixel = Decimal.ToDouble(knownTireDiameter.Value) / tirePixels;
            millimetersPerPixel = (horizontalMillimetersPerPixel + verticalMillimetersPerPixel) / 2.0;
            double scaleDifference = Math.Abs(horizontalMillimetersPerPixel - verticalMillimetersPerPixel) / millimetersPerPixel * 100.0;
            verificationErrorPercent = scaleDifference;
            string grade = scaleDifference <= 1.5 ? "HIGH" : scaleDifference <= 3.0 ? "MODERATE" : "REVIEW";
            calibrationVerificationStatus = "BIKE REFERENCE · " + grade + " · X/Y difference " + scaleDifference.ToString("0.0", CultureInfo.InvariantCulture) + "%";
            scaleLabel.Text = "Scale: horizontal " + horizontalMillimetersPerPixel.ToString("0.0000", CultureInfo.InvariantCulture) + " · vertical " + verticalMillimetersPerPixel.ToString("0.0000", CultureInfo.InvariantCulture) + " mm/pixel";
            verificationLabel.Text = "Bike scale check: " + calibrationVerificationStatus;
            verificationLabel.ForeColor = scaleDifference <= 1.5 ? Color.FromArgb(60, 145, 76) : scaleDifference <= 3.0 ? Color.FromArgb(166, 92, 34) : Color.FromArgb(176, 52, 52);
            status.Text = scaleDifference <= 3.0
                ? "Bike calibration complete. Continue to landmarks."
                : "Bike calibration needs review. Recheck the camera position and the four calibration points.";
            currentLandmarkLabel.Text = "Bike calibration complete";
            nextPointHintLabel.Text = "Horizontal scale uses wheelbase; vertical scale uses outside tire diameter.";
            levelReferencePoints.Clear();
            levelReferencePoints.Add(rearAxle);
            levelReferencePoints.Add(frontAxle);
            referenceLabel.Text = "Level reference: rear-to-front axle line applied";
            levelReference.Enabled = true;
            verifyCalibration.Enabled = false;
            undoLast.Enabled = false;
            mode = ClickMode.None;
            picture.Invalidate();
            UpdateWizardProgress();
        }

        private void VerifyCalibration_Click(object sender, EventArgs e)
        {
            if (millimetersPerPixel <= 0)
            {
                MessageBox.Show(this, "Calibrate the scale first.", "Calibration required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            mode = ClickMode.Verification;
            verificationPoints.Clear();
            verificationErrorPercent = double.NaN;
            calibrationVerificationStatus = "Not verified";
            verificationLabel.Text = "Verification: click a second known length";
            verificationLabel.ForeColor = Color.FromArgb(166, 92, 34);
            status.Text = "Verification: click the first point of a different known length in the bike plane.";
            currentLandmarkLabel.Text = "Current point: verification point 1";
            nextPointHintLabel.Text = "Use a long reference away from the original calibration when possible.";
            undoLast.Enabled = false;
            picture.Invalidate();
            UpdateWizardProgress();
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

            if (double.IsNaN(verificationErrorPercent))
            {
                DialogResult continueWithoutVerification = MessageBox.Show(this,
                    "Calibration has not been checked against a second known length.\n\nContinue to landmarks anyway? The saved result will be marked for review.",
                    "Verify calibration",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (continueWithoutVerification != DialogResult.Yes)
                {
                    VerifyCalibration_Click(sender, e);
                    return;
                }

                verificationErrorPercent = -1;
                calibrationVerificationStatus = "Skipped · review required";
                verificationLabel.Text = "Verification: skipped · review required";
                verificationLabel.ForeColor = Color.FromArgb(176, 52, 52);
            }

            mode = ClickMode.Landmarks;
            landmarkPoints.Clear();
            calculatedValues.Clear();
            ResetVerticalScaleCalibration();
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

        private void SuggestLandmarks_Click(object sender, EventArgs e)
        {
            if (millimetersPerPixel <= 0)
            {
                MessageBox.Show(this, "Calibrate the scale first. The assisted landmarks use that same calibrated image for the measurements.", "Scale required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (double.IsNaN(verificationErrorPercent))
            {
                MessageBox.Show(this, "Verify the calibration with a second known length before using assisted bike landmarks.", "Verify calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                VerifyCalibration_Click(sender, e);
                return;
            }

            advancedLandmarks.Checked = true;
            BikeLandmarkSuggestion suggestion;
            using (Bitmap bitmap = new Bitmap(loadedImage))
                suggestion = BikeLandmarkSuggester.Suggest(bitmap);
            landmarkPoints.Clear();
            landmarkPoints.AddRange(suggestion.Points);
            landmarksSuggested = true;
            landmarkSuggestionConfidence = suggestion.Confidence;
            mode = ClickMode.None;
            CalculateMetrics();
            undoLast.Enabled = true;
            recalculate.Enabled = true;
            flipSetbackSign.Enabled = true;
            saveBefore.Enabled = true;
            saveAfter.Enabled = true;
            calibrateVerticalScale.Enabled = CanUseWheelbaseVerticalCalibration();
            status.Text = "Eight bike landmarks suggested. Confirm every orange point before saving.";
            currentLandmarkLabel.Text = "Assisted landmarks · confidence " + landmarkSuggestionConfidence.ToString("0", CultureInfo.InvariantCulture) + "%";
            nextPointHintLabel.Text = "Drag BB, saddle, handlebar, pedal, and both axle points onto their exact centers. Suggestions are advisory.";
            picture.Invalidate();
            UpdateWizardProgress();
        }

        private void AdvancedLandmarks_CheckedChanged(object sender, EventArgs e)
        {
            handlebarReferenceMode.Enabled = advancedLandmarks.Checked;
            handlebarDiameter.Enabled = advancedLandmarks.Checked;
            landmarkPoints.Clear();
            calculatedValues.Clear();
            ResetVerticalScaleCalibration();
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
            UpdateWizardProgress();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            levelReferencePoints.Clear();
            if (perspectiveTransform != null && wheelPerspectivePoints.Count >= 5)
            {
                levelReferencePoints.Add(wheelPerspectivePoints[0]);
                levelReferencePoints.Add(wheelPerspectivePoints[4]);
            }
            landmarkPoints.Clear();
            calculatedValues.Clear();
            ResetVerticalScaleCalibration();
            undoLast.Enabled = false;
            flipSetbackSign.Enabled = false;
            recalculate.Enabled = false;
            saveBefore.Enabled = false;
            saveAfter.Enabled = false;
            resultsLabel.Text = "Calculated metrics:\n--";
            referenceLabel.Text = perspectiveTransform != null ? "Level reference: rear-to-front axle line locked" : "Level reference: not set";
            status.Text = millimetersPerPixel > 0 ? "Points cleared. Click Start Guided Capture." : "Points cleared. Start with Calibrate Scale.";
            currentLandmarkLabel.Text = "Current point: --";
            nextPointHintLabel.Text = millimetersPerPixel > 0 ? "Scale is still set. Start Guided Capture when ready." : "Tip: calibrate scale first, then set level reference if the camera is tilted.";
            picture.Invalidate();
            UpdateWizardProgress();
        }

        private void UndoLast_Click(object sender, EventArgs e)
        {
            if (mode == ClickMode.Landmarks || landmarkPoints.Count > 0)
            {
                if (landmarkPoints.Count == 0)
                    return;

                landmarkPoints.RemoveAt(landmarkPoints.Count - 1);
                calculatedValues.Clear();
                ResetVerticalScaleCalibration();
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

            if (mode == ClickMode.Verification && verificationPoints.Count > 0)
            {
                verificationPoints.RemoveAt(verificationPoints.Count - 1);
                status.Text = verificationPoints.Count == 0 ? "Verification: click the first point of a second known length." : "Verification: click the second point.";
                currentLandmarkLabel.Text = verificationPoints.Count == 0 ? "Current point: verification point 1" : "Current point: verification point 2";
                undoLast.Enabled = verificationPoints.Count > 0;
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
                UpdateWizardProgress();
            }

            if (mode == ClickMode.BikeCalibration && bikeCalibrationPoints.Count > 0)
            {
                bikeCalibrationPoints.RemoveAt(bikeCalibrationPoints.Count - 1);
                string[] names = new string[] { "rear axle center", "front axle center", "top of tire", "bottom of tire" };
                int next = bikeCalibrationPoints.Count;
                status.Text = "Bike calibration: click the " + names[next] + ".";
                currentLandmarkLabel.Text = "Bike calibration point " + (next + 1).ToString(CultureInfo.InvariantCulture) + " of 4: " + names[next];
                undoLast.Enabled = bikeCalibrationPoints.Count > 0;
                picture.Invalidate();
                UpdateWizardProgress();
            }

            if (mode == ClickMode.WheelPerspectiveCalibration && wheelPerspectivePoints.Count > 0)
            {
                wheelPerspectivePoints.RemoveAt(wheelPerspectivePoints.Count - 1);
                string[] names = GetWheelPerspectivePointNames();
                int next = wheelPerspectivePoints.Count;
                status.Text = "Perspective calibration: click the " + names[next] + ".";
                currentLandmarkLabel.Text = "Wheel point " + (next + 1).ToString(CultureInfo.InvariantCulture) + " of 8: " + names[next];
                undoLast.Enabled = wheelPerspectivePoints.Count > 0;
                picture.Invalidate();
                UpdateWizardProgress();
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
            calibrateVerticalScale.Enabled = CanUseWheelbaseVerticalCalibration();
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
            else if (mode == ClickMode.BikeCalibration)
                AddBikeCalibrationPoint(imagePoint);
            else if (mode == ClickMode.WheelPerspectiveCalibration)
                AddWheelPerspectivePoint(imagePoint);
            else if (mode == ClickMode.Verification)
                AddVerificationPoint(imagePoint);
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
                horizontalMillimetersPerPixel = millimetersPerPixel;
                verticalMillimetersPerPixel = millimetersPerPixel;
                knownCalibrationMillimeters = knownMillimeters;
                scaleLabel.Text = "Scale: " + millimetersPerPixel.ToString("0.0000", CultureInfo.InvariantCulture) + " mm/pixel";
                status.Text = "Scale calibrated. Optional: click Level Reference, or start Guided Capture.";
                currentLandmarkLabel.Text = "Current point: ready for level reference or guided capture";
                nextPointHintLabel.Text = "Next: use Level Reference if the image is tilted, or Start Guided Capture.";
                levelReference.Enabled = true;
                verifyCalibration.Enabled = true;
                undoLast.Enabled = false;
                mode = ClickMode.None;
                picture.Invalidate();
                UpdateWizardProgress();
            }
        }

        private void AddVerificationPoint(PointF imagePoint)
        {
            verificationPoints.Add(imagePoint);
            undoLast.Enabled = true;
            if (verificationPoints.Count == 1)
            {
                status.Text = "Verification: click the second point of that known length.";
                currentLandmarkLabel.Text = "Current point: verification point 2";
                nextPointHintLabel.Text = "Click the opposite end of the second reference.";
                picture.Invalidate();
                return;
            }

            double pixelDistance = Distance(verificationPoints[0], verificationPoints[1]);
            double knownMillimeters;
            if (pixelDistance <= 0 || !PromptForMillimeters(this, "Verify calibration", "Enter the real length of this second reference in millimeters:", out knownMillimeters))
            {
                verificationPoints.Clear();
                status.Text = "Verification cancelled. Click Verify Calibration to try again.";
                picture.Invalidate();
                return;
            }

            double measuredMillimeters = ScaledDistance(verificationPoints[0], verificationPoints[1]);
            verificationErrorPercent = Math.Abs(measuredMillimeters - knownMillimeters) / knownMillimeters * 100.0;
            string grade = verificationErrorPercent <= 1.0 ? "HIGH" : verificationErrorPercent <= 2.0 ? "MODERATE" : "REVIEW";
            calibrationVerificationStatus = grade + " · " + verificationErrorPercent.ToString("0.0", CultureInfo.InvariantCulture) + "% error";
            verificationLabel.Text = "Verification: " + calibrationVerificationStatus + " · expected " + knownMillimeters.ToString("0.0", CultureInfo.InvariantCulture) + " mm, measured " + measuredMillimeters.ToString("0.0", CultureInfo.InvariantCulture) + " mm";
            verificationLabel.ForeColor = verificationErrorPercent <= 1.0 ? Color.FromArgb(60, 145, 76) : verificationErrorPercent <= 2.0 ? Color.FromArgb(166, 92, 34) : Color.FromArgb(176, 52, 52);
            status.Text = verificationErrorPercent <= 2.0 ? "Calibration verified. Continue to landmarks." : "Calibration needs review. Recheck the reference plane, camera angle, and lens distortion.";
            currentLandmarkLabel.Text = "Current point: calibration verification complete";
            nextPointHintLabel.Text = verificationErrorPercent <= 2.0 ? "Next: set Level Reference if needed, then place bike landmarks." : "For an ultra-wide camera, move references and bike toward the image center, then recalibrate.";
            mode = ClickMode.None;
            undoLast.Enabled = false;
            picture.Invalidate();
            UpdateWizardProgress();
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
                UpdateWizardProgress();
            }
        }

        private void AddLandmarkPoint(PointF imagePoint)
        {
            landmarkPoints.Add(imagePoint);
            undoLast.Enabled = true;
            if (landmarkPoints.Count < ActiveLandmarkNames.Length)
            {
                status.Text = "Click landmark " + (landmarkPoints.Count + 1).ToString(CultureInfo.InvariantCulture) + " of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + GetLandmarkDisplayName(landmarkPoints.Count) + ".";
                UpdateCurrentLandmarkInstruction();
                picture.Invalidate();
                UpdateWizardProgress();
                return;
            }

            CalculateMetrics();
            mode = ClickMode.None;
            flipSetbackSign.Enabled = true;
            recalculate.Enabled = true;
            saveBefore.Enabled = true;
            saveAfter.Enabled = true;
            calibrateVerticalScale.Enabled = CanUseWheelbaseVerticalCalibration();
            status.Text = "Guided capture complete. Review values, then save to Before or After.";
            currentLandmarkLabel.Text = "Current point: complete";
            nextPointHintLabel.Text = "Drag any orange point to fine-tune. Values update before saving.";
            picture.Invalidate();
            UpdateWizardProgress();
        }

        private void CalculateMetrics()
        {
            PointF bottomBracket = landmarkPoints[0];
            PointF saddleTop = landmarkPoints[1];
            PointF saddleTip = landmarkPoints[2];
            PointF grip = landmarkPoints[3];

            PointF correctedBottomBracket = GetMeasurementPoint(bottomBracket);
            PointF correctedSaddleTop = GetMeasurementPoint(saddleTop);
            PointF correctedSaddleTip = GetMeasurementPoint(saddleTip);
            PointF correctedGrip = GetMeasurementPoint(grip);
            PointF handlebarReference = grip;
            PointF correctedHandlebarReference = correctedGrip;

            if (advancedLandmarks.Checked && landmarkPoints.Count >= 6)
            {
                handlebarReference = landmarkPoints[5];
                correctedHandlebarReference = GetMeasurementPoint(handlebarReference);

                if (landmarkPoints.Count >= advancedLandmarkNames.Length && handlebarReferenceMode.SelectedIndex > 0)
                {
                    PointF correctedFrontAxleForDirection = GetMeasurementPoint(landmarkPoints[6]);
                    PointF correctedRearAxleForDirection = GetMeasurementPoint(landmarkPoints[7]);
                    double forwardDirection = correctedFrontAxleForDirection.X >= correctedRearAxleForDirection.X ? 1.0 : -1.0;
                    double radiusPixels = GetHorizontalUnitsForMillimeters(Decimal.ToDouble(handlebarDiameter.Value) / 2.0);
                    double edgeDirection = handlebarReferenceMode.SelectedIndex == 1 ? forwardDirection : -forwardDirection;
                    correctedHandlebarReference = new PointF(
                        (float)(correctedHandlebarReference.X + edgeDirection * radiusPixels),
                        correctedHandlebarReference.Y);
                }
            }

            double saddleHeight;
            bool hybridSaddleHeight = TryCalculateWheelbaseScaledDistance(bottomBracket, saddleTop, out saddleHeight);
            if (!hybridSaddleHeight)
                saddleHeight = MeasurementDistance(correctedBottomBracket, correctedSaddleTop);
            double saddleSetback = MeasurementHorizontalDifference(correctedSaddleTip, correctedBottomBracket);
            // Match the physical tape measurement. Handlebar reach remains the
            // separate horizontal measurement calculated below.
            double imageSaddleTipToGripReach = MeasurementDistance(correctedSaddleTip, correctedGrip);
            double saddleTipToGripReach = useTapeSaddleTipToGrip.Checked
                ? Decimal.ToDouble(tapeSaddleTipToGrip.Value)
                : imageSaddleTipToGripReach;
            double handlebarX = MeasurementHorizontalDifference(correctedHandlebarReference, correctedBottomBracket);
            double handlebarY = MeasurementVerticalDifference(correctedBottomBracket, correctedHandlebarReference);

            calculatedValues = new Dictionary<string, string>();
            calculatedValues["SaddleHeight"] = FormatMillimeters(saddleHeight);
            calculatedValues["SaddleHeightSource"] = hybridSaddleHeight
                ? verticalScaleCalibrated
                    ? "Tape-calibrated vertical scale (" + verticalScaleCorrection.ToString("0.000", CultureInfo.InvariantCulture) + "×)"
                    : "Wheelbase-only estimate (vertical calibration recommended)"
                : "Calibrated image points";
            calculatedValues["SaddleSetback"] = FormatMillimeters(saddleSetback);
            calculatedValues["SaddleTipToGripReach"] = FormatMillimeters(saddleTipToGripReach);
            calculatedValues["HandlebarX"] = FormatMillimeters(handlebarX);
            calculatedValues["HandlebarY"] = FormatMillimeters(handlebarY);

            if (advancedLandmarks.Checked && landmarkPoints.Count >= advancedLandmarkNames.Length)
            {
                PointF pedalSpindle = landmarkPoints[4];
                PointF frontAxle = landmarkPoints[6];
                PointF rearAxle = landmarkPoints[7];
                PointF correctedFrontAxle = GetMeasurementPoint(frontAxle);
                PointF correctedRearAxle = GetMeasurementPoint(rearAxle);

                PointF correctedPedalSpindle = GetMeasurementPoint(pedalSpindle);
                double crankLength = MeasurementDistance(correctedBottomBracket, correctedPedalSpindle);
                double handlebarReach = MeasurementHorizontalDifference(correctedHandlebarReference, correctedSaddleTip);
                double handlebarDrop = MeasurementVerticalDifference(correctedHandlebarReference, correctedSaddleTop);
                double wheelbase = Math.Abs(MeasurementHorizontalDifference(correctedFrontAxle, correctedRearAxle));

                calculatedValues["CrankLength"] = FormatMillimeters(crankLength);
                calculatedValues["HandlebarReach"] = FormatMillimeters(handlebarReach);
                calculatedValues["HandlebarDrop"] = FormatMillimeters(handlebarDrop);
                calculatedValues["Wheelbase"] = FormatMillimeters(wheelbase);
            }

            calculatedValues["LevelReference"] = levelReferencePoints.Count == 2 ? "Applied" : "Not set";
            calculatedValues["SaddleSetbackConvention"] = "Behind BB = negative";
            calculatedValues["LandmarkMode"] = advancedLandmarks.Checked ? "Advanced 8-point" : "Basic 4-point";
            calculatedValues["CameraSetup"] = CameraSetupStatus;
            calculatedValues["CalibrationReference"] = calibrationMethod.SelectedIndex == 0
                ? "Dual-wheel perspective · wheelbase " + knownWheelbase.Value.ToString("0.0", CultureInfo.InvariantCulture) + " mm · tire " + knownTireDiameter.Value.ToString("0.0", CultureInfo.InvariantCulture) + " mm"
                : calibrationMethod.SelectedIndex == 1
                ? "Quick bike reference · wheelbase " + knownWheelbase.Value.ToString("0.0", CultureInfo.InvariantCulture) + " mm · tire " + knownTireDiameter.Value.ToString("0.0", CultureInfo.InvariantCulture) + " mm"
                : knownCalibrationMillimeters > 0 ? knownCalibrationMillimeters.ToString("0.0", CultureInfo.InvariantCulture) + " mm" : "Not set";
            calculatedValues["CalibrationVerification"] = calibrationVerificationStatus;
            double estimatedTolerance = perspectiveTransform != null
                ? Math.Max(3.0, Math.Ceiling(perspectiveResidualMillimeters))
                : 5.0;
            calculatedValues["MeasurementTolerance"] = "approximately ±" + estimatedTolerance.ToString("0", CultureInfo.InvariantCulture) + " mm; point placement and out-of-plane parts may add error";
            calculatedValues["HandlebarReference"] = GetHandlebarReferenceSummary();
            calculatedValues["SaddleTipToGripSource"] = useTapeSaddleTipToGrip.Checked
                ? "Tape verified (preferred report value)"
                : "Camera estimate (use tape verification when view is angled)";

            UpdateResultsLabel();
        }

        private void SaveResult(string side)
        {
            if (calculatedValues == null || calculatedValues.Count == 0)
                return;

            DialogResult preview = MessageBox.Show(this,
                "Save these guided measurements to " + side + "?\n\n" + BuildMetricsPreview() + "\n\n" + BuildQualitySummary() + "\n\nQuality warnings are advisory and do not block saving.",
                "Confirm Guided Capture",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (preview != DialogResult.Yes)
                return;

            ResultValues = new Dictionary<string, string>(calculatedValues);
            ResultSide = side;
            CaptureMethod = advancedLandmarks.Checked ? "Guided Capture - Advanced Landmarks" : "Guided Capture";
            if (landmarksSuggested)
                CaptureMethod = "Assisted Bike Landmark Tracking - Fitter Confirmed";
            LevelReferenceStatus = GetCalculatedValue("LevelReference");
            SaddleSetbackConvention = GetCalculatedValue("SaddleSetbackConvention");
            CameraSetupStatus = GetCalculatedValue("CameraSetup");
            AssistedLandmarkSummary = landmarksSuggested
                ? "Eight suggested bike landmarks reviewed for bottom bracket, saddle top/tip, grip, pedal spindle, handlebar center, and wheel axles; starting confidence " + landmarkSuggestionConfidence.ToString("0", CultureInfo.InvariantCulture) + "%"
                : "Bike landmarks placed manually by fitter";
            AssistedLandmarkSummary += "; camera profile " + cameraProfileName + "; calibration " + calibrationVerificationStatus;
            AssistedLandmarkSummary += "; handlebar reference " + GetHandlebarReferenceSummary();
            AssistedLandmarkSummary += "; saddle-to-hood source " + GetCalculatedValue("SaddleTipToGripSource");
            AnnotatedImagePath = SaveAnnotatedLandmarkImage(side);
            DialogResult = DialogResult.OK;
            Close();
        }

        private string SaveAnnotatedLandmarkImage(string side)
        {
            string directory = string.IsNullOrWhiteSpace(outputDirectory) ? Directory.GetCurrentDirectory() : outputDirectory;
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, side + "-Assisted-Bike-Landmarks-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".png");
            using (Bitmap output = new Bitmap(loadedImage))
            using (Graphics graphics = Graphics.FromImage(output))
            using (Pen guide = new Pen(Color.FromArgb(255, 176, 74), Math.Max(3F, output.Width / 500F)))
            using (Brush pointBrush = new SolidBrush(Color.FromArgb(255, 176, 74)))
            using (Brush labelBrush = new SolidBrush(Color.FromArgb(220, 13, 19, 17)))
            using (Brush textBrush = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", Math.Max(10F, output.Width / 110F), FontStyle.Bold))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                guide.DashStyle = DashStyle.Dash;
                if (landmarkPoints.Count >= 4)
                {
                    graphics.DrawLine(guide, landmarkPoints[0], landmarkPoints[1]);
                    graphics.DrawLine(guide, landmarkPoints[0], landmarkPoints[3]);
                }
                if (landmarkPoints.Count >= 8)
                {
                    graphics.DrawLine(guide, landmarkPoints[0], landmarkPoints[4]);
                    graphics.DrawLine(guide, landmarkPoints[6], landmarkPoints[7]);
                }
                float radius = Math.Max(8F, output.Width / 120F);
                for (int i = 0; i < landmarkPoints.Count; i++)
                {
                    PointF point = landmarkPoints[i];
                    graphics.FillEllipse(pointBrush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
                    string label = GetLandmarkDisplayName(i);
                    SizeF size = graphics.MeasureString(label, font);
                    RectangleF box = new RectangleF(point.X + radius, point.Y - size.Height / 2, size.Width + 12, size.Height + 4);
                    graphics.FillRectangle(labelBrush, box);
                    graphics.DrawString(label, font, textBrush, box.Left + 6, box.Top + 2);
                }
                output.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            return path;
        }

        private string BuildQualitySummary()
        {
            List<string> warnings = new List<string>();
            if (!IsCameraSetupConfirmed())
                warnings.Add("Camera setup checklist was not confirmed");
            if (levelReferencePoints.Count != 2 && perspectiveTransform == null)
                warnings.Add("No level reference is set; confirm the image is truly level");
            if (double.IsNaN(verificationErrorPercent))
                warnings.Add("Calibration was not verified with a second known length");
            else if (verificationErrorPercent < 0)
                warnings.Add("Calibration verification was skipped");
            else if (verificationErrorPercent > 2.0)
                warnings.Add("Calibration verification error is " + verificationErrorPercent.ToString("0.0", CultureInfo.InvariantCulture) + "% — recheck camera alignment, reference plane, and wide-angle distortion");
            if (perspectiveTransform != null && perspectiveResidualMillimeters > 7.0)
                warnings.Add("Dual-wheel perspective fit residual is " + perspectiveResidualMillimeters.ToString("0.0", CultureInfo.InvariantCulture) + " mm — recheck all wheel points");
            if (cameraProfileName.IndexOf("120°", StringComparison.OrdinalIgnoreCase) >= 0)
                warnings.Add("Ultra-wide camera profile selected; keep every landmark near the image center");
            AddBikeMetricQualityWarning(warnings, "Saddle height", "SaddleHeight", 500, 900);
            AddBikeMetricQualityWarning(warnings, "Saddle setback", "SaddleSetback", -120, 60);
            AddBikeMetricQualityWarning(warnings, "Saddle tip to grip", "SaddleTipToGripReach", 350, 750);
            AddBikeMetricQualityWarning(warnings, "Handlebar X", "HandlebarX", 300, 700);
            AddBikeMetricQualityWarning(warnings, "Handlebar Y", "HandlebarY", -180, 180);
            return warnings.Count == 0 ? "QUALITY CHECK: PASS" : "QUALITY CHECK: REVIEW\n• " + string.Join("\n• ", warnings.ToArray());
        }

        private void AddBikeMetricQualityWarning(List<string> warnings, string label, string key, double minimum, double maximum)
        {
            double value;
            if (!TryParseMillimeters(GetCalculatedValue(key), out value))
            {
                warnings.Add(label + " could not be calculated");
                return;
            }
            if (value < minimum || value > maximum)
                warnings.Add(label + " is outside the broad review range");
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

            currentLandmarkLabel.Text = "Current point " + (nextIndex + 1).ToString(CultureInfo.InvariantCulture) + " of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + GetLandmarkDisplayName(nextIndex);
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
                return "Click the TOP of the rubber hood where the rider's palm rests—not the brake lever blade. If the camera is angled, use the optional tape value.";

            if (index == 4)
                return "Click the center of the pedal spindle to calculate crank length.";

            if (index == 5)
                return handlebarReferenceMode.SelectedIndex == 0
                    ? "Click the handlebar clamp center for bar X/Y, reach, and drop."
                    : handlebarReferenceMode.SelectedIndex == 1
                        ? "Click the rear edge of the round bar at the clamp. The entered diameter shifts this point forward to calculate center."
                        : "Click the front edge of the round bar at the clamp. The entered diameter shifts this point rearward to calculate center.";

            if (index == 6)
                return "Click the front axle center. This starts the wheelbase reference.";

            if (index == 7)
                return "Click the rear axle center to complete the advanced landmark set.";

            return "Zoom in if needed, then click the landmark.";
        }

        private string GetLandmarkDisplayName(int index)
        {
            if (advancedLandmarks.Checked && index == 5 && handlebarReferenceMode != null)
            {
                if (handlebarReferenceMode.SelectedIndex == 1)
                    return "Handlebar rear edge";
                if (handlebarReferenceMode.SelectedIndex == 2)
                    return "Handlebar front edge";
            }

            return index >= 0 && index < ActiveLandmarkNames.Length ? ActiveLandmarkNames[index] : "Landmark";
        }

        private string GetHandlebarReferenceSummary()
        {
            if (!advancedLandmarks.Checked || handlebarReferenceMode == null)
                return "Grip / hood contact point";
            if (handlebarReferenceMode.SelectedIndex == 0)
                return "Bar center clicked directly";

            string edge = handlebarReferenceMode.SelectedIndex == 1 ? "rear edge" : "front edge";
            return edge + " clicked; center calculated using " + handlebarDiameter.Value.ToString("0.0", CultureInfo.InvariantCulture) + " mm diameter";
        }

        private void HandlebarReferenceChanged(object sender, EventArgs e)
        {
            if (landmarkPoints.Count >= ActiveLandmarkNames.Length && millimetersPerPixel > 0)
            {
                CalculateMetrics();
                status.Text = "Handlebar center reference updated. Review the recalculated values.";
            }
            picture.Invalidate();
        }

        private void SaddleTipToGripOverrideChanged(object sender, EventArgs e)
        {
            tapeSaddleTipToGrip.Enabled = useTapeSaddleTipToGrip.Checked;
            if (landmarkPoints.Count >= ActiveLandmarkNames.Length && millimetersPerPixel > 0)
            {
                CalculateMetrics();
                status.Text = useTapeSaddleTipToGrip.Checked
                    ? "Tape saddle-to-hood value applied to the saved measurement."
                    : "Image-point saddle-to-hood value restored.";
            }
        }

        private void CalibrateVerticalScale_Click(object sender, EventArgs e)
        {
            if (landmarkPoints.Count < ActiveLandmarkNames.Length || !CanUseWheelbaseVerticalCalibration())
            {
                MessageBox.Show(this, "Complete Dual-wheel or Quick Bike calibration and place the bike landmarks first.", "Vertical calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            double dx;
            double dy;
            if (!TryGetWheelbaseScaledComponents(landmarkPoints[0], landmarkPoints[1], out dx, out dy) || Math.Abs(dy) < 1.0)
            {
                MessageBox.Show(this, "The bottom-bracket and saddle points cannot produce a stable vertical correction. Recheck both points.", "Vertical calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double target = Decimal.ToDouble(verifiedSaddleHeight.Value);
            double verticalSquared = (target * target) - (dx * dx);
            if (verticalSquared <= 0)
            {
                MessageBox.Show(this, "The tape saddle height is shorter than the horizontal part of the selected points. Recheck the bottom bracket and saddle top.", "Vertical calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double correction = Math.Sqrt(verticalSquared) / Math.Abs(dy);
            if (correction < 0.75 || correction > 1.35)
            {
                MessageBox.Show(this, "The required correction is " + correction.ToString("0.000", CultureInfo.InvariantCulture) + "×, which is too large to trust. Recheck the wheelbase, axle centers, bottom bracket, and saddle point.", "Vertical calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            verticalScaleCorrection = correction;
            verticalScaleCalibrated = true;
            verticalCalibrationLabel.Text = "Vertical scale: tape calibrated · " + correction.ToString("0.000", CultureInfo.InvariantCulture) + "×";
            verticalCalibrationLabel.ForeColor = Color.FromArgb(60, 145, 76);
            CalculateMetrics();
            status.Text = "Tape-calibrated vertical scale applied. Future point adjustments use this locked correction.";
            picture.Invalidate();
        }

        private void ResetVerticalScaleCalibration()
        {
            verticalScaleCorrection = 1.0;
            verticalScaleCalibrated = false;
            if (verticalCalibrationLabel != null)
            {
                verticalCalibrationLabel.Text = "Vertical scale: not tape calibrated";
                verticalCalibrationLabel.ForeColor = Color.FromArgb(92, 104, 98);
            }
            if (calibrateVerticalScale != null)
                calibrateVerticalScale.Enabled = false;
        }

        private void UpdateResultsLabel()
        {
            resultsLabel.Text =
                "Calculated metrics:\n" +
                "Mode: " + GetCalculatedValue("LandmarkMode") + "\n" +
                "Saddle height: " + GetCalculatedValue("SaddleHeight") + "\n" +
                "Saddle-height source: " + GetCalculatedValue("SaddleHeightSource") + "\n" +
                "Saddle setback: " + GetCalculatedValue("SaddleSetback") + "\n" +
                "Saddle tip to grip (straight line): " + GetCalculatedValue("SaddleTipToGripReach") + "\n" +
                "Saddle-to-hood source: " + GetCalculatedValue("SaddleTipToGripSource") + "\n" +
                "Handlebar X: " + GetCalculatedValue("HandlebarX") + "\n" +
                "Handlebar Y: " + GetCalculatedValue("HandlebarY") + "\n" +
                "Crank length: " + GetCalculatedValue("CrankLength") + "\n" +
                "Handlebar reach: " + GetCalculatedValue("HandlebarReach") + "\n" +
                "Handlebar drop: " + GetCalculatedValue("HandlebarDrop") + "\n" +
                "Wheelbase: " + GetCalculatedValue("Wheelbase") + "\n" +
                "Level reference: " + GetCalculatedValue("LevelReference") + "\n" +
                "Camera setup: " + GetCalculatedValue("CameraSetup") + "\n" +
                "Calibration: " + GetCalculatedValue("CalibrationReference") + " · " + GetCalculatedValue("CalibrationVerification") + "\n" +
                "Expected precision: " + GetCalculatedValue("MeasurementTolerance") + "\n" +
                "Handlebar reference: " + GetCalculatedValue("HandlebarReference") + "\n" +
                "Setback convention: " + GetCalculatedValue("SaddleSetbackConvention");
        }

        private string BuildMetricsPreview()
        {
            return
                "Saddle height: " + GetCalculatedValue("SaddleHeight") + "\n" +
                "Saddle-height source: " + GetCalculatedValue("SaddleHeightSource") + "\n" +
                "Saddle setback: " + GetCalculatedValue("SaddleSetback") + "\n" +
                "Saddle tip to grip (straight line): " + GetCalculatedValue("SaddleTipToGripReach") + "\n" +
                "Saddle-to-hood source: " + GetCalculatedValue("SaddleTipToGripSource") + "\n" +
                "Handlebar X: " + GetCalculatedValue("HandlebarX") + "\n" +
                "Handlebar Y: " + GetCalculatedValue("HandlebarY") + "\n\n" +
                "Crank length: " + GetCalculatedValue("CrankLength") + "\n" +
                "Handlebar reach: " + GetCalculatedValue("HandlebarReach") + "\n" +
                "Handlebar drop: " + GetCalculatedValue("HandlebarDrop") + "\n" +
                "Wheelbase: " + GetCalculatedValue("Wheelbase") + "\n\n" +
                "Landmark mode: " + GetCalculatedValue("LandmarkMode") + "\n" +
                "Level reference: " + GetCalculatedValue("LevelReference") + "\n" +
                "Camera setup: " + GetCalculatedValue("CameraSetup") + "\n" +
                "Calibration verification: " + GetCalculatedValue("CalibrationVerification") + "\n" +
                "Expected precision: " + GetCalculatedValue("MeasurementTolerance") + "\n" +
                "Handlebar reference: " + GetCalculatedValue("HandlebarReference") + "\n" +
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
                    status.Text = "Adjusting landmark " + (landmarkIndex + 1).ToString(CultureInfo.InvariantCulture) + ": " + GetLandmarkDisplayName(landmarkIndex) + ".";
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
            DrawLine(e.Graphics, bikeCalibrationPoints, Color.FromArgb(90, 205, 120), "B");
            DrawWheelPerspectiveCalibration(e.Graphics);
            DrawLine(e.Graphics, verificationPoints, Color.FromArgb(255, 176, 74), "V");
            DrawLine(e.Graphics, levelReferencePoints, Color.FromArgb(74, 145, 255), "L");
            DrawLandmarks(e.Graphics);
            DrawActiveClickCue(e.Graphics);
            DrawPlacementMagnifier(e.Graphics);
        }

        private void DrawPlacementMagnifier(Graphics graphics)
        {
            if (!hasMousePosition || loadedImage == null || mode == ClickMode.None)
                return;

            PointF imagePoint;
            if (!TryConvertControlPointToImagePoint(mousePosition, out imagePoint))
                return;

            const int sourceSize = 80;
            const int insetSize = 184;
            int sourceWidth = Math.Min(sourceSize, loadedImage.Width);
            int sourceHeight = Math.Min(sourceSize, loadedImage.Height);
            int sourceX = Math.Max(0, Math.Min(loadedImage.Width - sourceWidth, (int)imagePoint.X - sourceWidth / 2));
            int sourceY = Math.Max(0, Math.Min(loadedImage.Height - sourceHeight, (int)imagePoint.Y - sourceHeight / 2));
            Rectangle source = new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight);
            Rectangle inset = new Rectangle(Math.Max(12, picture.ClientSize.Width - insetSize - 18), 92, insetSize, insetSize);
            using (Brush background = new SolidBrush(Color.FromArgb(230, 13, 19, 17)))
            using (Pen border = new Pen(Color.FromArgb(184, 243, 74), 3F))
            using (Pen crosshair = new Pen(Color.FromArgb(255, 176, 74), 2F))
            {
                graphics.FillRectangle(background, inset);
                graphics.DrawImage(loadedImage, inset, source, GraphicsUnit.Pixel);
                graphics.DrawRectangle(border, inset);
                int centerX = inset.Left + inset.Width / 2;
                int centerY = inset.Top + inset.Height / 2;
                graphics.DrawLine(crosshair, centerX - 18, centerY, centerX + 18, centerY);
                graphics.DrawLine(crosshair, centerX, centerY - 18, centerX, centerY + 18);
            }
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
                else if (label == "B" && controlPoints.Count >= 2)
                {
                    graphics.DrawLine(pen, controlPoints[0], controlPoints[1]);
                    if (controlPoints.Count >= 4)
                        graphics.DrawLine(pen, controlPoints[2], controlPoints[3]);
                }

                for (int i = 0; i < controlPoints.Count; i++)
                {
                    PointF point = controlPoints[i];
                    RectangleF circle = new RectangleF(point.X - 10, point.Y - 10, 20, 20);
                    graphics.FillEllipse(brush, circle);
                    graphics.DrawString(label + (i + 1).ToString(CultureInfo.InvariantCulture), font, textBrush, point.X + 10, point.Y - 12);
                }
            }
        }

        private void DrawWheelPerspectiveCalibration(Graphics graphics)
        {
            if (wheelPerspectivePoints.Count == 0)
                return;

            List<PointF> points = new List<PointF>();
            foreach (PointF imagePoint in wheelPerspectivePoints)
                points.Add(ConvertImagePointToControlPoint(imagePoint));

            using (Pen pen = new Pen(Color.FromArgb(74, 196, 214), 3F))
            using (Pen axleGuide = new Pen(Color.FromArgb(210, 184, 243, 74), 2F))
            using (Brush brush = new SolidBrush(Color.FromArgb(74, 196, 214)))
            using (Brush guideLabelBrush = new SolidBrush(Color.FromArgb(220, 13, 19, 17)))
            using (Brush guideTextBrush = new SolidBrush(Color.White))
            using (Brush textBrush = new SolidBrush(Color.FromArgb(13, 19, 17)))
            using (Font font = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                axleGuide.DashStyle = DashStyle.Dash;
                Rectangle imageRectangle = GetZoomedImageRectangle();
                if (mode == ClickMode.WheelPerspectiveCalibration)
                {
                    DrawAxleReferenceGuides(graphics, axleGuide, guideLabelBrush, guideTextBrush, font, imageRectangle, points, 0, "REAR AXLE HEIGHT");
                    DrawAxleReferenceGuides(graphics, axleGuide, guideLabelBrush, guideTextBrush, font, imageRectangle, points, 4, "FRONT AXLE HEIGHT");
                }
                if (points.Count >= 5)
                    graphics.DrawLine(pen, points[0], points[4]);
                DrawWheelCross(graphics, pen, points, 0);
                DrawWheelCross(graphics, pen, points, 4);

                for (int i = 0; i < points.Count; i++)
                {
                    PointF point = points[i];
                    graphics.FillEllipse(brush, point.X - 9, point.Y - 9, 18, 18);
                    graphics.DrawString("W" + (i + 1).ToString(CultureInfo.InvariantCulture), font, textBrush, point.X + 9, point.Y - 10);
                }
            }
        }

        private static void DrawWheelCross(Graphics graphics, Pen pen, IList<PointF> points, int start)
        {
            if (points.Count > start + 1)
                graphics.DrawLine(pen, points[start], points[start + 1]);
            if (points.Count > start + 2)
                graphics.DrawLine(pen, points[start], points[start + 2]);
            if (points.Count > start + 3)
                graphics.DrawLine(pen, points[start], points[start + 3]);
        }

        private static void DrawAxleReferenceGuides(Graphics graphics, Pen pen, Brush labelBrush, Brush textBrush, Font font, Rectangle imageRectangle, IList<PointF> points, int axleIndex, string label)
        {
            if (points.Count <= axleIndex)
                return;

            PointF axle = points[axleIndex];
            graphics.DrawLine(pen, imageRectangle.Left, axle.Y, imageRectangle.Right, axle.Y);
            graphics.DrawLine(pen, axle.X, imageRectangle.Top, axle.X, imageRectangle.Bottom);
            SizeF labelSize = graphics.MeasureString(label, font);
            float labelX = (float)Math.Max(imageRectangle.Left + 4, Math.Min(imageRectangle.Right - labelSize.Width - 16, axle.X + 12));
            float labelY = (float)Math.Max(imageRectangle.Top + 4, axle.Y - labelSize.Height - 10);
            RectangleF labelRectangle = new RectangleF(labelX, labelY, labelSize.Width + 12, labelSize.Height + 6);
            graphics.FillRectangle(labelBrush, labelRectangle);
            graphics.DrawString(label, font, textBrush, labelRectangle.Left + 6, labelRectangle.Top + 3);
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
                    string label = (i + 1).ToString(CultureInfo.InvariantCulture) + ". " + GetLandmarkDisplayName(i);
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

            if (mode == ClickMode.BikeCalibration)
            {
                string[] names = new string[] { "rear axle", "front axle", "top of tire", "bottom of tire" };
                int next = Math.Min(bikeCalibrationPoints.Count, names.Length - 1);
                return "Bike calibration " + (next + 1).ToString(CultureInfo.InvariantCulture) + " of 4 · click " + names[next];
            }

            if (mode == ClickMode.WheelPerspectiveCalibration)
            {
                string[] names = GetWheelPerspectivePointNames();
                int next = Math.Min(wheelPerspectivePoints.Count, names.Length - 1);
                return "Perspective calibration " + (next + 1).ToString(CultureInfo.InvariantCulture) + " of 8 · click " + names[next];
            }

            if (mode == ClickMode.Verification)
                return "Click verification point " + (verificationPoints.Count + 1).ToString(CultureInfo.InvariantCulture) + " of 2";

            if (mode == ClickMode.LevelReference)
                return "Click level reference point " + (levelReferencePoints.Count + 1).ToString(CultureInfo.InvariantCulture) + " of 2";

            if (mode == ClickMode.Landmarks)
            {
                int nextIndex = landmarkPoints.Count;
                if (nextIndex < ActiveLandmarkNames.Length)
                    return "Click landmark " + (nextIndex + 1).ToString(CultureInfo.InvariantCulture) + " of " + ActiveLandmarkNames.Length.ToString(CultureInfo.InvariantCulture) + ": " + GetLandmarkDisplayName(nextIndex);
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

        private static string[] GetWheelPerspectivePointNames()
        {
            return new string[]
            {
                "rear axle center",
                "rear tire top edge",
                "rear tire left edge at axle height",
                "rear tire right edge at axle height",
                "front axle center",
                "front tire top edge",
                "front tire left edge at axle height",
                "front tire right edge at axle height"
            };
        }

        private static List<PointF> BuildIdealWheelPoints(double wheelbase, double diameter, bool frontWheelIsRightOfRear)
        {
            float wheelbaseValue = (float)wheelbase;
            float radius = (float)(diameter / 2.0);
            float screenLeftOffset = frontWheelIsRightOfRear ? -radius : radius;
            float screenRightOffset = -screenLeftOffset;
            return new List<PointF>
            {
                new PointF(0, 0),
                new PointF(0, -radius),
                new PointF(screenLeftOffset, 0),
                new PointF(screenRightOffset, 0),
                new PointF(wheelbaseValue, 0),
                new PointF(wheelbaseValue, -radius),
                new PointF(wheelbaseValue + screenLeftOffset, 0),
                new PointF(wheelbaseValue + screenRightOffset, 0)
            };
        }

        private static bool TryBuildPerspectiveTransform(IList<PointF> imagePoints, IList<PointF> idealPoints, out double[] transform, out double residualMillimeters)
        {
            transform = null;
            residualMillimeters = double.NaN;
            if (imagePoints == null || idealPoints == null || imagePoints.Count != idealPoints.Count || imagePoints.Count < 4)
                return false;

            // Use the two axle centers and the two tire-top points as four
            // stable anchors. Fitting all eight points through normal equations
            // can become ill-conditioned because six points share the axle
            // line, producing extreme values when landmarks are above it.
            int[] anchorIndices = new int[] { 0, 1, 4, 5 };
            double[,] system = new double[8, 8];
            double[] right = new double[8];
            int equation = 0;
            for (int anchor = 0; anchor < anchorIndices.Length; anchor++)
            {
                int i = anchorIndices[anchor];
                double x = imagePoints[i].X;
                double y = imagePoints[i].Y;
                double destinationX = idealPoints[i].X;
                double destinationY = idealPoints[i].Y;
                double[] xRow = new double[] { x, y, 1, 0, 0, 0, -destinationX * x, -destinationX * y };
                double[] yRow = new double[] { 0, 0, 0, x, y, 1, -destinationY * x, -destinationY * y };
                for (int column = 0; column < 8; column++)
                {
                    system[equation, column] = xRow[column];
                    system[equation + 1, column] = yRow[column];
                }
                right[equation] = destinationX;
                right[equation + 1] = destinationY;
                equation += 2;
            }

            double[] solution;
            if (!TrySolveLinearSystem(system, right, out solution))
                return false;

            double squaredError = 0;
            for (int i = 0; i < imagePoints.Count; i++)
            {
                PointF projected;
                if (!TryApplyPerspectiveTransform(solution, imagePoints[i], out projected))
                    return false;
                double error = Distance(projected, idealPoints[i]);
                squaredError += error * error;
            }

            transform = solution;
            residualMillimeters = Math.Sqrt(squaredError / imagePoints.Count);
            return !double.IsNaN(residualMillimeters) && !double.IsInfinity(residualMillimeters);
        }

        private static bool TrySolveLinearSystem(double[,] matrix, double[] right, out double[] solution)
        {
            const int size = 8;
            solution = new double[size];
            double[,] augmented = new double[size, size + 1];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                    augmented[row, column] = matrix[row, column];
                augmented[row, size] = right[row];
            }

            for (int pivot = 0; pivot < size; pivot++)
            {
                int bestRow = pivot;
                double bestValue = Math.Abs(augmented[pivot, pivot]);
                for (int row = pivot + 1; row < size; row++)
                {
                    double value = Math.Abs(augmented[row, pivot]);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        bestRow = row;
                    }
                }

                if (bestValue < 0.0000000001)
                    return false;

                if (bestRow != pivot)
                {
                    for (int column = pivot; column <= size; column++)
                    {
                        double temporary = augmented[pivot, column];
                        augmented[pivot, column] = augmented[bestRow, column];
                        augmented[bestRow, column] = temporary;
                    }
                }

                double divisor = augmented[pivot, pivot];
                for (int column = pivot; column <= size; column++)
                    augmented[pivot, column] /= divisor;

                for (int row = 0; row < size; row++)
                {
                    if (row == pivot)
                        continue;
                    double factor = augmented[row, pivot];
                    for (int column = pivot; column <= size; column++)
                        augmented[row, column] -= factor * augmented[pivot, column];
                }
            }

            for (int row = 0; row < size; row++)
                solution[row] = augmented[row, size];
            return true;
        }

        private static bool TryApplyPerspectiveTransform(double[] transform, PointF point, out PointF result)
        {
            result = PointF.Empty;
            if (transform == null || transform.Length != 8)
                return false;
            double denominator = transform[6] * point.X + transform[7] * point.Y + 1.0;
            if (Math.Abs(denominator) < 0.0000001)
                return false;
            double x = (transform[0] * point.X + transform[1] * point.Y + transform[2]) / denominator;
            double y = (transform[3] * point.X + transform[4] * point.Y + transform[5]) / denominator;
            if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
                return false;
            result = new PointF((float)x, (float)y);
            return true;
        }

        private PointF ApplyPerspectiveTransform(PointF point)
        {
            PointF result;
            return TryApplyPerspectiveTransform(perspectiveTransform, point, out result) ? result : point;
        }

        private PointF GetMeasurementPoint(PointF point)
        {
            return perspectiveTransform != null ? ApplyPerspectiveTransform(point) : CorrectForLevel(point);
        }

        private bool TryCalculateWheelbaseScaledDistance(PointF first, PointF second, out double millimeters)
        {
            millimeters = 0;
            double dx;
            double dy;
            if (!TryGetWheelbaseScaledComponents(first, second, out dx, out dy))
                return false;

            dy *= verticalScaleCorrection;
            millimeters = Math.Sqrt((dx * dx) + (dy * dy));
            return !double.IsNaN(millimeters) && !double.IsInfinity(millimeters) && millimeters > 0;
        }

        private bool TryGetWheelbaseScaledComponents(PointF first, PointF second, out double dx, out double dy)
        {
            dx = 0;
            dy = 0;
            if (!CanUseWheelbaseVerticalCalibration())
                return false;

            PointF rearAxlePoint = wheelPerspectivePoints.Count >= 5 ? wheelPerspectivePoints[0] : bikeCalibrationPoints[0];
            PointF frontAxlePoint = wheelPerspectivePoints.Count >= 5 ? wheelPerspectivePoints[4] : bikeCalibrationPoints[1];
            PointF rearAxle = CorrectForLevel(rearAxlePoint);
            PointF frontAxle = CorrectForLevel(frontAxlePoint);
            PointF leveledFirst = CorrectForLevel(first);
            PointF leveledSecond = CorrectForLevel(second);

            double axleSeparation = Math.Abs(frontAxle.X - rearAxle.X);
            if (axleSeparation < 20)
                return false;

            // Pixels are square. Once the axle line is leveled, the known
            // wheelbase supplies one stable scale for both axes. This avoids
            // treating "700c" as a measurable 700 mm outside tire diameter.
            double wheelbaseScale = Decimal.ToDouble(knownWheelbase.Value) / axleSeparation;
            dx = (leveledFirst.X - leveledSecond.X) * wheelbaseScale;
            dy = (leveledFirst.Y - leveledSecond.Y) * wheelbaseScale;
            return !double.IsNaN(dx) && !double.IsInfinity(dx) && !double.IsNaN(dy) && !double.IsInfinity(dy);
        }

        private bool CanUseWheelbaseVerticalCalibration()
        {
            return knownWheelbase != null && knownWheelbase.Value > 0 &&
                (wheelPerspectivePoints.Count >= 5 || bikeCalibrationPoints.Count >= 2);
        }

        private double MeasurementDistance(PointF first, PointF second)
        {
            return perspectiveTransform != null ? Distance(first, second) : ScaledDistance(first, second);
        }

        private double MeasurementHorizontalDifference(PointF first, PointF second)
        {
            double difference = first.X - second.X;
            return perspectiveTransform != null ? difference : difference * GetHorizontalScale();
        }

        private double MeasurementVerticalDifference(PointF first, PointF second)
        {
            double difference = first.Y - second.Y;
            return perspectiveTransform != null ? difference : difference * GetVerticalScale();
        }

        private double GetHorizontalUnitsForMillimeters(double millimeters)
        {
            return perspectiveTransform != null ? millimeters : millimeters / GetHorizontalScale();
        }

        private double GetHorizontalScale()
        {
            return horizontalMillimetersPerPixel > 0 ? horizontalMillimetersPerPixel : millimetersPerPixel;
        }

        private double GetVerticalScale()
        {
            return verticalMillimetersPerPixel > 0 ? verticalMillimetersPerPixel : millimetersPerPixel;
        }

        private double ScaledDistance(PointF first, PointF second)
        {
            double dx = (first.X - second.X) * GetHorizontalScale();
            double dy = (first.Y - second.Y) * GetVerticalScale();
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

        private bool IsCameraSetupConfirmed()
        {
            return !string.IsNullOrWhiteSpace(CameraSetupStatus) &&
                CameraSetupStatus.StartsWith("Confirmed", StringComparison.OrdinalIgnoreCase);
        }

        private static string PromptForCameraProfile(IWin32Window owner, string currentProfile)
        {
            using (Form form = new Form())
            using (Label label = new Label())
            using (ComboBox profiles = new ComboBox())
            using (Label hint = new Label())
            using (Button ok = new Button())
            {
                form.Text = "Camera profile";
                form.Font = new Font("Segoe UI", 9F);
                form.ClientSize = new Size(440, 190);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;

                label.Text = "Choose the camera/lens used for this bike image:";
                label.SetBounds(16, 16, 400, 24);
                profiles.DropDownStyle = ComboBoxStyle.DropDownList;
                profiles.Items.AddRange(new object[]
                {
                    "NexiGo N980P · 120° ultra-wide",
                    "NexiGo N680P · 80°",
                    "Standard camera · 70–90°",
                    "Custom / unknown camera"
                });
                profiles.SetBounds(16, 46, 408, 28);
                int selectedIndex = profiles.Items.IndexOf(currentProfile);
                profiles.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 2;

                hint.Text = "Ultra-wide profiles receive an accuracy warning. Calibration and the bike must be in the same physical plane.";
                hint.ForeColor = SystemColors.GrayText;
                hint.SetBounds(16, 84, 408, 42);
                ok.Text = "Continue";
                ok.DialogResult = DialogResult.OK;
                ok.SetBounds(320, 140, 104, 32);
                form.Controls.Add(label);
                form.Controls.Add(profiles);
                form.Controls.Add(hint);
                form.Controls.Add(ok);
                form.AcceptButton = ok;

                form.ShowDialog(owner);
                return profiles.SelectedItem == null ? currentProfile : profiles.SelectedItem.ToString();
            }
        }

        private static NumericUpDown CreateCalibrationNumber(decimal minimum, decimal maximum, decimal value)
        {
            NumericUpDown number = new NumericUpDown();
            number.Minimum = minimum;
            number.Maximum = maximum;
            number.Value = value;
            number.DecimalPlaces = 1;
            number.Increment = 1;
            number.Width = 100;
            number.Anchor = AnchorStyles.Left;
            return number;
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

    internal class BikeLandmarkSuggestion
    {
        public List<PointF> Points = new List<PointF>();
        public double Confidence;
    }

    internal static class BikeLandmarkSuggester
    {
        public static BikeLandmarkSuggestion Suggest(Bitmap bitmap)
        {
            int step = Math.Max(2, Math.Min(bitmap.Width, bitmap.Height) / 220);
            double border = EstimateBorderBrightness(bitmap, step);
            int left = bitmap.Width, top = bitmap.Height, right = 0, bottom = 0, samples = 0, contrastSamples = 0;
            for (int y = 0; y < bitmap.Height; y += step)
            for (int x = 0; x < bitmap.Width; x += step)
            {
                double luma = Luma(bitmap.GetPixel(x, y));
                samples++;
                if (Math.Abs(luma - border) < 42 && luma > 55)
                    continue;
                left = Math.Min(left, x); right = Math.Max(right, x);
                top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                contrastSamples++;
            }

            if (right <= left || bottom <= top)
            {
                left = (int)(bitmap.Width * 0.08); right = (int)(bitmap.Width * 0.92);
                top = (int)(bitmap.Height * 0.10); bottom = (int)(bitmap.Height * 0.90);
            }

            float width = right - left;
            float height = bottom - top;
            float rearX = left + width * 0.18F;
            float frontX = left + width * 0.82F;
            float axleY = top + height * 0.74F;
            float bbX = left + width * 0.44F;
            float bbY = top + height * 0.66F;
            PointF bottomBracket = RefineDarkCenter(bitmap, new PointF(bbX, bbY), width * 0.09F, height * 0.10F);
            PointF saddleTop = RefineDarkCenter(bitmap, new PointF(left + width * 0.40F, top + height * 0.23F), width * 0.10F, height * 0.10F);
            PointF saddleTip = RefineDarkCenter(bitmap, new PointF(left + width * 0.48F, top + height * 0.24F), width * 0.09F, height * 0.08F);
            PointF grip = RefineDarkCenter(bitmap, new PointF(left + width * 0.72F, top + height * 0.25F), width * 0.11F, height * 0.12F);
            PointF pedal = RefineDarkCenter(bitmap, new PointF(bottomBracket.X + width * 0.07F, bottomBracket.Y + height * 0.05F), width * 0.06F, height * 0.07F);
            PointF handlebar = RefineDarkCenter(bitmap, new PointF(left + width * 0.70F, top + height * 0.30F), width * 0.11F, height * 0.13F);
            PointF frontAxle = RefineDarkCenter(bitmap, new PointF(frontX, axleY), width * 0.08F, height * 0.08F);
            PointF rearAxle = RefineDarkCenter(bitmap, new PointF(rearX, axleY), width * 0.08F, height * 0.08F);

            BikeLandmarkSuggestion result = new BikeLandmarkSuggestion();
            result.Points.Add(bottomBracket);
            result.Points.Add(saddleTop);
            result.Points.Add(saddleTip);
            result.Points.Add(grip);
            result.Points.Add(pedal);
            result.Points.Add(handlebar);
            result.Points.Add(frontAxle);
            result.Points.Add(rearAxle);
            double coverage = (double)contrastSamples / Math.Max(1, samples);
            result.Confidence = Math.Max(35, Math.Min(82, 48 + coverage * 85));
            return result;
        }

        private static PointF RefineDarkCenter(Bitmap bitmap, PointF seed, float radiusX, float radiusY)
        {
            int left = Math.Max(0, (int)(seed.X - radiusX));
            int right = Math.Min(bitmap.Width - 1, (int)(seed.X + radiusX));
            int top = Math.Max(0, (int)(seed.Y - radiusY));
            int bottom = Math.Min(bitmap.Height - 1, (int)(seed.Y + radiusY));
            int step = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 320);
            double weightedX = 0, weightedY = 0, total = 0;
            for (int y = top; y <= bottom; y += step)
            for (int x = left; x <= right; x += step)
            {
                double weight = Math.Max(0, 175 - Luma(bitmap.GetPixel(x, y)));
                weightedX += x * weight; weightedY += y * weight; total += weight;
            }
            if (total <= 0)
                return seed;
            return new PointF((float)(weightedX / total), (float)(weightedY / total));
        }

        private static double EstimateBorderBrightness(Bitmap bitmap, int step)
        {
            double total = 0; int count = 0;
            for (int x = 0; x < bitmap.Width; x += step)
            {
                total += Luma(bitmap.GetPixel(x, 0)) + Luma(bitmap.GetPixel(x, bitmap.Height - 1)); count += 2;
            }
            for (int y = 0; y < bitmap.Height; y += step)
            {
                total += Luma(bitmap.GetPixel(0, y)) + Luma(bitmap.GetPixel(bitmap.Width - 1, y)); count += 2;
            }
            return total / Math.Max(1, count);
        }

        private static double Luma(Color color)
        {
            return color.R * 0.2126 + color.G * 0.7152 + color.B * 0.0722;
        }
    }
}
