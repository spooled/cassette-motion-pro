/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using CassetteMotionPro.Clients;
using CassetteMotionPro;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class BikeFitWorkspaceForm : Form
    {
        private readonly ClientRecord client;
        private readonly FitSessionRepository repository;
        private readonly FitSessionTemplateRepository templateRepository = new FitSessionTemplateRepository();
        private readonly CameraSetupProfileRepository cameraProfileRepository = new CameraSetupProfileRepository();
        private readonly Action<string> openVideo;
        private readonly Action<string, string> openVideoPair;
        private readonly Action<string> prepareCaptureFolder;
        private readonly Action<string> openLiveCaptureFolder;
        private readonly Action<string, string> openDualLiveCaptureFolders;
        private readonly Action<string, string, string, string> openProfileDualLiveCaptureFolders;
        private readonly Action<string> openBodyAngleGuide;
        private readonly ListView sessionList = new ListView();
        private readonly ListView historySessionList = new ListView();
        private readonly ListView historyComparisonList = new ListView();
        private readonly TextBox historySummary = new TextBox();
        private readonly Label historyStatus = new Label();
        private readonly TextBox txtTitle = new TextBox();
        private readonly DateTimePicker dtpDate = new DateTimePicker();
        private readonly ComboBox cmbStatus = new ComboBox();
        private readonly ComboBox cmbFitTemplate = new ComboBox();
        private readonly ComboBox cmbFitProtocol = new ComboBox();
        private readonly FlowLayoutPanel fitProtocolSteps = new FlowLayoutPanel();
        private readonly Label fitProtocolSummary = new Label();
        private readonly Label fitProtocolProgress = new Label();
        private bool loadingFitProtocol;
        private readonly ComboBox cmbCameraProfile = new ComboBox();
        private readonly TextBox txtCameraLeftRole = new TextBox();
        private readonly TextBox txtCameraRightRole = new TextBox();
        private readonly TextBox txtCameraLeftDevice = new TextBox();
        private readonly TextBox txtCameraRightDevice = new TextBox();
        private readonly TextBox txtCameraResolution = new TextBox();
        private readonly TextBox txtCameraFrameRate = new TextBox();
        private readonly TextBox txtCameraNotes = new TextBox();
        private readonly Label cameraProfileStatus = new Label();
        private readonly Label fitTemplatePreview = new Label();
        private readonly TextBox txtGoals = new TextBox();
        private readonly TextBox txtNotes = new TextBox();
        private readonly TextBox txtFitSummaryMainGoal = new TextBox();
        private readonly TextBox txtFitSummaryKeyFindings = new TextBox();
        private readonly TextBox txtFitSummaryChangesMade = new TextBox();
        private readonly TextBox txtFitSummaryRecommendations = new TextBox();
        private readonly TextBox txtFitSummaryFollowUp = new TextBox();
        private readonly TextBox txtHandoffWhatToSend = new TextBox();
        private readonly TextBox txtHandoffClientMessage = new TextBox();
        private readonly TextBox txtHandoffHomework = new TextBox();
        private readonly TextBox txtHandoffNextAppointment = new TextBox();
        private readonly TextBox txtHandoffInternalNotes = new TextBox();
        private readonly Label saveHint = new Label();
        private readonly Label activeSessionStatus = new Label();
        private readonly Label analysisCapturesStatus = new Label();
        private readonly Label recordingFoldersGuide = new Label();
        private readonly Label nextRecommendedStep = new Label();
        private readonly Label fitDayHomeStatus = new Label();
        private readonly Label fitDayHomeReadiness = new Label();
        private readonly Label fitDayHomeFolders = new Label();
        private readonly FlowLayoutPanel fitDayHomeFolderButtons = new FlowLayoutPanel();
        private readonly Button fitDayPrimaryAction = new Button();
        private readonly Panel fitDayAdvancedPanel = new Panel();
        private readonly Label fitCommandCenterStatus = new Label();
        private readonly Label activeSaveTargetStatus = new Label();
        private readonly Label savedEvidenceReviewStatus = new Label();
        private readonly Label reportBuilderStatus = new Label();
        private readonly Label reportBuilderOutput = new Label();
        private readonly Label smartRecommendationStatus = new Label();
        private readonly TextBox smartRecommendationDraft = new TextBox();
        private readonly Label finalizationStatus = new Label();
        private readonly TextBox finalizationChecklist = new TextBox();
        private readonly Label combinedMeasurementReviewStatus = new Label();
        private readonly TextBox combinedMeasurementReview = new TextBox();
        private readonly Label captureActionsLabel = new Label();
        private readonly Label analysisActionsLabel = new Label();
        private readonly Button nextRecommendedStepAction = new Button();
        private readonly Button nextRecommendedFolderAction = new Button();
        private readonly CheckBox chkShowBeforeMeasurementsInReport = new CheckBox();
        private readonly CheckBox chkShowSideBySideImageInReport = new CheckBox();
        private readonly CheckBox chkShowBeforeImageInReport = new CheckBox();
        private readonly CheckBox chkShowAfterImageInReport = new CheckBox();
        private readonly CheckBox chkShowMeasurementReferenceImageInReport = new CheckBox();
        private readonly CheckBox chkShowMeasurementCaptureTraceInReport = new CheckBox();
        private readonly ComboBox cmbReportLogoStyle = new ComboBox();
        private readonly Dictionary<string, TextBox> mediaBoxes = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, Label> mediaStatusLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, TextBox> imageBoxes = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, TextBox> measurementBoxes = new Dictionary<string, TextBox>();
        private readonly List<WorkflowChecklistItem> workflowChecklistItems = new List<WorkflowChecklistItem>();
        private readonly List<FitDayFlowStep> fitDayFlowSteps = new List<FitDayFlowStep>();
        private TabControl editorTabs;
        private FitSessionRecord currentSession;
        private Action nextRecommendedStepActionHandler;
        private Action nextRecommendedFolderActionHandler;
        private string fitCommandCenterMode = "Plan";
        private const string FitDayHomeTabName = "Fit Day";
        private const string SessionSetupTabName = "Session Setup";
        private const string KinoveaVideoTabName = "Video Studio";

        public BikeFitWorkspaceForm(ClientRecord client, Action<string> openVideo, Action<string, string> openVideoPair, Action<string> prepareCaptureFolder, Action<string> openLiveCaptureFolder, Action<string, string> openDualLiveCaptureFolders, Action<string, string, string, string> openProfileDualLiveCaptureFolders, Action<string> openBodyAngleGuide)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            this.client = client;
            this.openVideo = openVideo;
            this.openVideoPair = openVideoPair;
            this.prepareCaptureFolder = prepareCaptureFolder;
            this.openLiveCaptureFolder = openLiveCaptureFolder;
            this.openDualLiveCaptureFolders = openDualLiveCaptureFolders;
            this.openProfileDualLiveCaptureFolders = openProfileDualLiveCaptureFolders;
            this.openBodyAngleGuide = openBodyAngleGuide;
            repository = new FitSessionRepository(client);

            Text = client.DisplayName + " — Cassette Motion Pro Fit Day";
            CassetteMotionTheme.ApplyForm(this);
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterParent;
            ReportImageSaveTarget.ReportImageSaved += ReportImageSaveTarget_ReportImageSaved;
            VideoSaveTarget.VideoSaved += VideoSaveTarget_VideoSaved;
            FormClosing += BikeFitWorkspaceForm_FormClosing;

            BuildInterface();
            ApplyVisualIdentity(this);
            RefreshSessions(Guid.Empty);
        }

        private void BuildInterface()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = CassetteMotionTheme.Header;

            Label brandBadge = new Label();
            brandBadge.Text = "CM";
            brandBadge.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            brandBadge.ForeColor = CassetteMotionTheme.Header;
            brandBadge.BackColor = CassetteMotionTheme.Accent;
            brandBadge.TextAlign = ContentAlignment.MiddleCenter;
            brandBadge.Size = new Size(54, 54);
            brandBadge.Location = new Point(28, 25);

            Label eyebrow = new Label();
            eyebrow.Text = "CASSETTE MOTION PRO  /  FIT DAY";
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = CassetteMotionTheme.Accent;
            eyebrow.AutoSize = true;
            eyebrow.Location = new Point(98, 17);

            Label title = new Label();
            title.Text = client.DisplayName;
            title.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(95, 34);

            Label bike = new Label();
            bike.Text = client.BikeDescription;
            bike.Font = new Font("Segoe UI", 10F);
            bike.ForeColor = Color.FromArgb(175, 187, 181);
            bike.AutoSize = true;
            bike.Location = new Point(100, 76);

            activeSessionStatus.Text = "Active session\nChoose or create a fit session";
            activeSessionStatus.Font = new Font("Segoe UI", 9F);
            activeSessionStatus.ForeColor = Color.FromArgb(205, 216, 210);
            activeSessionStatus.TextAlign = ContentAlignment.TopRight;
            activeSessionStatus.AutoSize = false;
            activeSessionStatus.Size = new Size(430, 72);
            activeSessionStatus.Location = new Point(720, 19);
            activeSessionStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            header.Controls.Add(brandBadge);
            header.Controls.Add(eyebrow);
            header.Controls.Add(title);
            header.Controls.Add(bike);
            header.Controls.Add(activeSessionStatus);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 260;
            split.Panel1.BackColor = CassetteMotionTheme.Surface;
            split.Panel2.BackColor = CassetteMotionTheme.Canvas;
            BuildSessionPanel(split.Panel1);
            BuildEditor(split.Panel2);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(split, 0, 1);
            Controls.Add(root);

            Panel accentLine = new Panel();
            accentLine.Dock = DockStyle.Bottom;
            accentLine.Height = 4;
            accentLine.BackColor = CassetteMotionTheme.Accent;
            header.Controls.Add(accentLine);
        }

        private void BuildSessionPanel(Control parent)
        {
            Panel heading = new Panel();
            heading.Dock = DockStyle.Top;
            heading.Height = 68;
            heading.Padding = new Padding(16, 14, 16, 10);

            TableLayoutPanel sessionActions = new TableLayoutPanel();
            sessionActions.Dock = DockStyle.Fill;
            sessionActions.ColumnCount = 2;
            sessionActions.RowCount = 1;
            sessionActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            sessionActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            sessionActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Button newSession = CreateButton("+ New Session", true);
            newSession.Dock = DockStyle.Fill;
            newSession.Margin = new Padding(0, 0, 4, 0);
            newSession.Click += delegate
            {
                BeginNewSession();
                SelectWorkspaceTab(SessionSetupTabName);
            };
            Button repeatFit = CreateButton("Repeat Fit", false);
            repeatFit.Dock = DockStyle.Fill;
            repeatFit.Margin = new Padding(4, 0, 0, 0);
            repeatFit.Click += delegate { OpenRepeatFitWorkflow(); };
            sessionActions.Controls.Add(newSession, 0, 0);
            sessionActions.Controls.Add(repeatFit, 1, 0);
            heading.Controls.Add(sessionActions);

            sessionList.Dock = DockStyle.Fill;
            sessionList.View = View.Details;
            sessionList.BorderStyle = BorderStyle.None;
            sessionList.FullRowSelect = true;
            sessionList.HideSelection = false;
            sessionList.MultiSelect = false;
            sessionList.Columns.Add("Fit sessions", 155);
            sessionList.Columns.Add("Status", 85);
            sessionList.SelectedIndexChanged += SessionList_SelectedIndexChanged;
            CassetteMotionTheme.StyleListView(sessionList);

            Label hint = new Label();
            hint.Text = "Sessions are saved inside the client’s Measurements folder.";
            hint.Dock = DockStyle.Bottom;
            hint.Height = 58;
            hint.Padding = new Padding(16, 8, 12, 8);
            hint.ForeColor = Color.FromArgb(92, 104, 98);

            parent.Controls.Add(sessionList);
            parent.Controls.Add(hint);
            parent.Controls.Add(heading);
        }

        private void BuildEditor(Control parent)
        {
            editorTabs = new TabControl();
            editorTabs.Dock = DockStyle.Fill;
            editorTabs.Padding = new Point(18, 8);
            CassetteMotionTheme.StyleTabs(editorTabs);
            editorTabs.SelectedIndexChanged += delegate { UpdateWorkflowChecklist(); };
            editorTabs.TabPages.Add(BuildFitDayDashboardTab());
            editorTabs.TabPages.Add(BuildOverviewTab());
            editorTabs.TabPages.Add(BuildClientFilesTab());
            editorTabs.TabPages.Add(BuildClientHistoryTab());
            editorTabs.TabPages.Add(BuildMediaTab());
            editorTabs.TabPages.Add(BuildMeasurementsWorkspaceTab());
            editorTabs.TabPages.Add(BuildReportWorkspaceTab());

            Panel actions = new Panel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 98;
            actions.Padding = new Padding(24, 10, 24, 10);
            actions.BackColor = CassetteMotionTheme.Surface;

            Button close = CreateButton("Save && Close", false);
            close.Width = 105;
            close.Click += delegate { Close(); };

            Button save = CreateButton("Save", true);
            save.Width = 82;
            save.Click += Save_Click;

            Button previewReport = CreateButton("Preview", false);
            previewReport.Width = 88;
            previewReport.Click += PreviewReport_Click;

            Button reviewSession = CreateButton("Review", true);
            reviewSession.Width = 86;
            reviewSession.Click += ReviewSession_Click;

            chkShowBeforeMeasurementsInReport.Text = "Show Before measurements in report";
            chkShowBeforeMeasurementsInReport.Checked = true;
            chkShowBeforeMeasurementsInReport.Width = 215;
            chkShowBeforeMeasurementsInReport.TextAlign = ContentAlignment.MiddleLeft;
            chkShowBeforeMeasurementsInReport.ForeColor = Color.FromArgb(24, 31, 29);
            chkShowBeforeMeasurementsInReport.CheckedChanged += delegate
            {
                if (currentSession != null)
                    UpdateSaveHint(chkShowBeforeMeasurementsInReport.Checked ? "Report will show Before, After, and Change." : "Report will show After/final measurements only.");
            };

            saveHint.Text = "Autosaves to this client’s Measurements folder.";
            saveHint.Dock = DockStyle.Top;
            saveHint.Height = 26;
            saveHint.TextAlign = ContentAlignment.MiddleLeft;
            saveHint.ForeColor = Color.FromArgb(92, 104, 98);

            FlowLayoutPanel actionButtons = new FlowLayoutPanel();
            actionButtons.Dock = DockStyle.Bottom;
            actionButtons.Height = 52;
            actionButtons.FlowDirection = FlowDirection.LeftToRight;
            actionButtons.WrapContents = true;
            actionButtons.AutoScroll = true;
            actionButtons.Padding = new Padding(0);

            actionButtons.Controls.Add(chkShowBeforeMeasurementsInReport);
            actionButtons.Controls.Add(save);
            actionButtons.Controls.Add(close);
            actionButtons.Controls.Add(reviewSession);
            actionButtons.Controls.Add(previewReport);

            actions.Controls.Add(actionButtons);
            actions.Controls.Add(saveHint);
            parent.Controls.Add(editorTabs);
            parent.Controls.Add(actions);
        }

        private TabPage BuildOverviewTab()
        {
            TabPage page = NewTab(SessionSetupTabName);
            TableLayoutPanel table = NewEditorTable();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;

            Label setupHeading = new Label();
            setupHeading.Text = "Session setup";
            setupHeading.Dock = DockStyle.Fill;
            setupHeading.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            setupHeading.ForeColor = CassetteMotionTheme.Ink;
            setupHeading.TextAlign = ContentAlignment.MiddleLeft;
            int headingRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            table.Controls.Add(setupHeading, 0, headingRow);
            table.SetColumnSpan(setupHeading, 2);

            Control templates = BuildFitTemplatePanel();
            int templateRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 226));
            table.Controls.Add(templates, 0, templateRow);
            table.SetColumnSpan(templates, 2);

            Control protocol = BuildFitProtocolPanel();
            int protocolRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 380));
            table.Controls.Add(protocol, 0, protocolRow);
            table.SetColumnSpan(protocol, 2);

            txtTitle.TextChanged += delegate { UpdateWorkflowChecklist(); };
            AddEditorRow(table, "Session title", txtTitle, 38);

            dtpDate.Format = DateTimePickerFormat.Long;
            dtpDate.Dock = DockStyle.Fill;
            AddEditorRow(table, "Session date", dtpDate, 38);

            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.Items.AddRange(new object[] { "Assessment", "In progress", "Complete" });
            cmbStatus.SelectedIndex = 0;
            cmbStatus.Dock = DockStyle.Fill;
            AddEditorRow(table, "Status", cmbStatus, 38);

            txtGoals.Multiline = true;
            txtGoals.ScrollBars = ScrollBars.Vertical;
            txtGoals.Dock = DockStyle.Fill;
            txtGoals.TextChanged += delegate { UpdateWorkflowChecklist(); };
            AddEditorRow(table, "Rider goals", txtGoals, 170);

            Label help = new Label();
            help.Text = "Capture the rider’s comfort, performance, injury, and event goals before making changes.";
            help.Dock = DockStyle.Top;
            help.Height = 54;
            help.ForeColor = Color.FromArgb(92, 104, 98);
            help.Padding = new Padding(0, 12, 0, 0);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            table.Controls.Add(help, 1, row);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private Control BuildFitProtocolPanel()
        {
            GroupBox group = new GroupBox();
            group.Text = "Guided fit protocol";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = CassetteMotionTheme.Ink;
            group.Padding = new Padding(12, 10, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            cmbFitProtocol.Dock = DockStyle.Fill;
            cmbFitProtocol.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (FitProtocol protocol in FitProtocolCatalog.LoadAll())
                cmbFitProtocol.Items.Add(protocol);
            cmbFitProtocol.SelectedIndexChanged += delegate { ChangeFitProtocol(); };

            fitProtocolProgress.Dock = DockStyle.Fill;
            fitProtocolProgress.TextAlign = ContentAlignment.MiddleRight;
            fitProtocolProgress.ForeColor = Color.FromArgb(91, 139, 0);
            fitProtocolProgress.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            fitProtocolSummary.Dock = DockStyle.Fill;
            fitProtocolSummary.BackColor = Color.FromArgb(248, 252, 238);
            fitProtocolSummary.ForeColor = CassetteMotionTheme.Muted;
            fitProtocolSummary.Padding = new Padding(10, 8, 10, 8);
            fitProtocolSummary.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            fitProtocolSteps.Dock = DockStyle.Fill;
            fitProtocolSteps.FlowDirection = FlowDirection.TopDown;
            fitProtocolSteps.WrapContents = false;
            fitProtocolSteps.AutoScroll = true;
            fitProtocolSteps.Padding = new Padding(0, 4, 0, 4);

            layout.Controls.Add(cmbFitProtocol, 0, 0);
            layout.Controls.Add(fitProtocolProgress, 1, 0);
            layout.Controls.Add(fitProtocolSummary, 0, 1);
            layout.SetColumnSpan(fitProtocolSummary, 2);
            layout.Controls.Add(fitProtocolSteps, 0, 2);
            layout.SetColumnSpan(fitProtocolSteps, 2);
            group.Controls.Add(layout);
            if (cmbFitProtocol.Items.Count > 0)
                cmbFitProtocol.SelectedIndex = 0;
            return group;
        }

        private void ChangeFitProtocol()
        {
            FitProtocol protocol = cmbFitProtocol.SelectedItem as FitProtocol;
            if (protocol == null)
                return;

            if (!loadingFitProtocol && currentSession != null && !string.Equals(currentSession.FitProtocolBikeType, protocol.BikeType, StringComparison.OrdinalIgnoreCase))
            {
                currentSession.FitProtocolBikeType = protocol.BikeType;
                currentSession.FitProtocolCompletedSteps = string.Empty;
                SaveCurrentSession();
            }
            BuildFitProtocolSteps(protocol);
        }

        private void BuildFitProtocolSteps(FitProtocol protocol)
        {
            loadingFitProtocol = true;
            fitProtocolSteps.SuspendLayout();
            fitProtocolSteps.Controls.Clear();
            HashSet<string> completed = GetCompletedFitProtocolSteps();
            foreach (FitProtocolStep step in protocol.Steps)
            {
                CheckBox check = new CheckBox();
                check.Name = step.Id;
                check.Text = step.Stage + "  ·  " + step.Title + " — " + step.Guidance;
                check.Checked = completed.Contains(step.Id);
                check.AutoSize = false;
                check.Width = Math.Max(760, fitProtocolSteps.ClientSize.Width - 28);
                check.Height = 30;
                check.Font = new Font("Segoe UI", 9F, step.Stage == "FIT" ? FontStyle.Bold : FontStyle.Regular);
                check.ForeColor = check.Checked ? CassetteMotionTheme.Success : CassetteMotionTheme.Ink;
                check.CheckedChanged += FitProtocolStep_CheckedChanged;
                fitProtocolSteps.Controls.Add(check);
            }
            fitProtocolSummary.Text = protocol.BikeType + " protocol · " + protocol.Summary + " Check each step as it is completed; progress is saved with this fit session.";
            fitProtocolSteps.ResumeLayout();
            loadingFitProtocol = false;
            UpdateFitProtocolProgress();
        }

        private void FitProtocolStep_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingFitProtocol)
                return;
            CheckBox changed = sender as CheckBox;
            if (changed == null)
                return;
            if (!HasActiveFitSession())
            {
                loadingFitProtocol = true;
                changed.Checked = false;
                loadingFitProtocol = false;
                MessageBox.Show(this, "Open or create a fit session first. Protocol progress is saved with that session.", "Fit Protocol", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HashSet<string> completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Control control in fitProtocolSteps.Controls)
            {
                CheckBox check = control as CheckBox;
                if (check != null && check.Checked)
                    completed.Add(check.Name);
                if (check != null)
                    check.ForeColor = check.Checked ? CassetteMotionTheme.Success : CassetteMotionTheme.Ink;
            }
            currentSession.FitProtocolCompletedSteps = string.Join(";", new List<string>(completed).ToArray());
            SaveCurrentSession();
            UpdateFitProtocolProgress();
        }

        private HashSet<string> GetCompletedFitProtocolSteps()
        {
            HashSet<string> completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.FitProtocolCompletedSteps))
                return completed;
            foreach (string id in currentSession.FitProtocolCompletedSteps.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                completed.Add(id.Trim());
            return completed;
        }

        private void UpdateFitProtocolProgress()
        {
            int total = 0;
            int complete = 0;
            foreach (Control control in fitProtocolSteps.Controls)
            {
                CheckBox check = control as CheckBox;
                if (check == null)
                    continue;
                total++;
                if (check.Checked)
                    complete++;
            }
            fitProtocolProgress.Text = total == 0 ? "No protocol" : complete.ToString() + " / " + total.ToString() + " complete";
            fitProtocolProgress.ForeColor = total > 0 && complete == total ? CassetteMotionTheme.Success : Color.FromArgb(91, 139, 0);
        }

        private void SelectFitProtocol(string bikeType)
        {
            if (string.IsNullOrWhiteSpace(bikeType))
                bikeType = "Road";
            loadingFitProtocol = true;
            int selected = 0;
            for (int index = 0; index < cmbFitProtocol.Items.Count; index++)
            {
                FitProtocol protocol = cmbFitProtocol.Items[index] as FitProtocol;
                if (protocol != null && string.Equals(protocol.BikeType, bikeType, StringComparison.OrdinalIgnoreCase))
                    selected = index;
            }
            if (cmbFitProtocol.Items.Count > 0)
                cmbFitProtocol.SelectedIndex = selected;
            if (currentSession != null && string.IsNullOrWhiteSpace(currentSession.FitProtocolBikeType))
            {
                FitProtocol selectedProtocol = cmbFitProtocol.SelectedItem as FitProtocol;
                if (selectedProtocol != null)
                    currentSession.FitProtocolBikeType = selectedProtocol.BikeType;
            }
            loadingFitProtocol = false;
            ChangeFitProtocol();
        }

        private Control BuildFitTemplatePanel()
        {
            GroupBox group = new GroupBox();
            group.Text = "Saved fitting template";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = Color.FromArgb(37, 48, 43);
            group.Padding = new Padding(12, 10, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            cmbFitTemplate.Dock = DockStyle.Fill;
            cmbFitTemplate.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFitTemplate.SelectedIndexChanged += delegate { UpdateFitTemplatePreview(); };

            fitTemplatePreview.Dock = DockStyle.Fill;
            fitTemplatePreview.BackColor = Color.FromArgb(248, 252, 238);
            fitTemplatePreview.ForeColor = Color.FromArgb(74, 87, 81);
            fitTemplatePreview.Padding = new Padding(10, 8, 10, 8);
            fitTemplatePreview.Font = new Font("Segoe UI", 9F);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = true;
            Button apply = CreateButton("Apply Template", true);
            apply.Size = new Size(135, 34);
            apply.Click += delegate { ApplySelectedFitTemplate(); };
            Button saveCustom = CreateButton("Save Current as Custom", false);
            saveCustom.Size = new Size(190, 34);
            saveCustom.Click += delegate { SaveCurrentFitTemplate(); };
            Button delete = CreateButton("Delete Custom", false);
            delete.Size = new Size(130, 34);
            delete.Click += delegate { DeleteSelectedFitTemplate(); };
            Button refresh = CreateButton("Refresh", false);
            refresh.Size = new Size(90, 34);
            refresh.Click += delegate { RefreshFitTemplates(null); };
            actions.Controls.Add(apply);
            actions.Controls.Add(saveCustom);
            actions.Controls.Add(delete);
            actions.Controls.Add(refresh);

            layout.Controls.Add(cmbFitTemplate, 0, 0);
            layout.Controls.Add(fitTemplatePreview, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            group.Controls.Add(layout);
            RefreshFitTemplates(null);
            return group;
        }

        private void RefreshFitTemplates(string selectName)
        {
            string name = selectName;
            FitSessionTemplate selected = cmbFitTemplate.SelectedItem as FitSessionTemplate;
            if (string.IsNullOrWhiteSpace(name) && selected != null)
                name = selected.Name;

            cmbFitTemplate.BeginUpdate();
            cmbFitTemplate.Items.Clear();
            foreach (FitSessionTemplate template in templateRepository.LoadAll())
                cmbFitTemplate.Items.Add(template);
            cmbFitTemplate.EndUpdate();

            int selectedIndex = -1;
            for (int index = 0; index < cmbFitTemplate.Items.Count; index++)
            {
                FitSessionTemplate template = cmbFitTemplate.Items[index] as FitSessionTemplate;
                if (template != null && string.Equals(template.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = index;
                    break;
                }
            }
            cmbFitTemplate.SelectedIndex = selectedIndex >= 0 ? selectedIndex : (cmbFitTemplate.Items.Count > 0 ? 0 : -1);
            UpdateFitTemplatePreview();
        }

        private void UpdateFitTemplatePreview()
        {
            FitSessionTemplate template = cmbFitTemplate.SelectedItem as FitSessionTemplate;
            if (template == null)
            {
                fitTemplatePreview.Text = "No fitting template selected.";
                return;
            }

            string active = currentSession != null && string.Equals(currentSession.FitTemplateName, template.Name, StringComparison.OrdinalIgnoreCase) ? "ACTIVE TEMPLATE\n" : string.Empty;
            fitTemplatePreview.Text = active + template.BikeType + " · " + (template.IsBuiltIn ? "Built-in" : "Custom") + Environment.NewLine +
                "Fit focus: " + template.MeasurementFocus + Environment.NewLine +
                "Client questions: " + template.GoalsPrompt;
        }

        private void ApplySelectedFitTemplate()
        {
            if (!HasActiveFitSession())
            {
                MessageBox.Show(this, "Open or create a fit session first, then apply the template.", "Fit Templates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FitSessionTemplate template = cmbFitTemplate.SelectedItem as FitSessionTemplate;
            if (template == null)
                return;

            DialogResult choice = MessageBox.Show(this,
                "Apply \"" + template.Name + "\"?\n\nYes: replace the current Fit Summary draft fields.\nNo: fill only empty Fit Summary fields.\nCancel: make no changes.\n\nVideos, images, and measurements are never changed.",
                "Apply Fit Template", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel)
                return;

            bool replace = choice == DialogResult.Yes;
            ApplyTemplateText(txtFitSummaryMainGoal, template.MainGoalPrompt, replace);
            ApplyTemplateText(txtFitSummaryRecommendations, template.RecommendationPrompt, replace);
            ApplyTemplateText(txtFitSummaryFollowUp, template.FollowUpPrompt, replace);
            currentSession.FitTemplateName = template.Name;
            currentSession.FitTemplateBikeType = template.BikeType;
            if (string.IsNullOrWhiteSpace(currentSession.FitProtocolBikeType))
            {
                currentSession.FitProtocolBikeType = template.BikeType;
                SelectFitProtocol(template.BikeType);
            }
            if (string.Equals(Convert.ToString(cmbStatus.SelectedItem), "Assessment", StringComparison.OrdinalIgnoreCase))
                cmbStatus.SelectedItem = "In progress";
            SaveCurrentSession();
            UpdateFitTemplatePreview();
            UpdateWorkflowChecklist();
            UpdateSaveHint(template.Name + " template applied. Review and personalize the Fit Summary draft before reporting.");
        }

        private static void ApplyTemplateText(TextBox destination, string value, bool replace)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            if (replace || string.IsNullOrWhiteSpace(destination.Text))
                destination.Text = value;
        }

        private void SaveCurrentFitTemplate()
        {
            if (!HasActiveFitSession())
            {
                MessageBox.Show(this, "Open or create a fit session first.", "Fit Templates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Name this reusable template. Before saving, remove client names or private details from the Fit Summary fields.",
                "Save Custom Fit Template", "My Fit Template").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;

            FitSessionTemplate selected = cmbFitTemplate.SelectedItem as FitSessionTemplate;
            FitSessionTemplate template = new FitSessionTemplate();
            template.Name = name;
            template.BikeType = selected != null ? selected.BikeType : "Custom";
            template.MeasurementFocus = selected != null ? selected.MeasurementFocus : "Use the measurements and evidence appropriate to this fitting workflow.";
            template.GoalsPrompt = selected != null ? selected.GoalsPrompt : "Clarify the rider’s goals, comfort, performance needs, and riding context.";
            template.MainGoalPrompt = txtFitSummaryMainGoal.Text.Trim();
            template.RecommendationPrompt = txtFitSummaryRecommendations.Text.Trim();
            template.FollowUpPrompt = txtFitSummaryFollowUp.Text.Trim();
            templateRepository.Save(template);
            RefreshFitTemplates(name);
            UpdateSaveHint("Custom fit template saved for use with any client: " + name + ".");
        }

        private void DeleteSelectedFitTemplate()
        {
            FitSessionTemplate template = cmbFitTemplate.SelectedItem as FitSessionTemplate;
            if (template == null)
                return;
            if (template.IsBuiltIn)
            {
                MessageBox.Show(this, "Built-in templates cannot be deleted. You can save a customized copy instead.", "Fit Templates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(this, "Delete the custom template \"" + template.Name + "\"?", "Delete Fit Template", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
            templateRepository.Delete(template);
            RefreshFitTemplates(null);
            UpdateSaveHint("Custom fit template deleted.");
        }

        private TabPage BuildMeasurementsWorkspaceTab()
        {
            return BuildGroupedWorkspaceTab("Measurements", BuildGuidedMeasurementsTab(), BuildBikeMetricsTab(), BuildBodyAnglesTab(), BuildCombinedMeasurementReviewTab());
        }

        private TabPage BuildCombinedMeasurementReviewTab()
        {
            TabPage page = NewTab("Combined Review");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24, 22, 24, 18);
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

            Label eyebrow = new Label();
            eyebrow.Text = "BIKE + RIDER MEASUREMENT REVIEW";
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);
            eyebrow.Dock = DockStyle.Fill;

            Label title = new Label();
            title.Text = "Review the complete fit in one place";
            title.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            title.Dock = DockStyle.Fill;

            combinedMeasurementReviewStatus.Dock = DockStyle.Fill;
            combinedMeasurementReviewStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            combinedMeasurementReviewStatus.ForeColor = Color.FromArgb(74, 87, 81);
            combinedMeasurementReviewStatus.BackColor = Color.FromArgb(248, 252, 238);
            combinedMeasurementReviewStatus.Padding = new Padding(12, 10, 12, 8);

            combinedMeasurementReview.Dock = DockStyle.Fill;
            combinedMeasurementReview.Multiline = true;
            combinedMeasurementReview.ReadOnly = true;
            combinedMeasurementReview.ScrollBars = ScrollBars.Both;
            combinedMeasurementReview.WordWrap = false;
            combinedMeasurementReview.BackColor = Color.White;
            combinedMeasurementReview.ForeColor = Color.FromArgb(24, 31, 29);
            combinedMeasurementReview.Font = new Font("Consolas", 10F);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = true;
            actions.Padding = new Padding(0, 8, 0, 4);

            Button refresh = CreateButton("Refresh Combined Review", true);
            refresh.Size = new Size(200, 38);
            refresh.Click += delegate { RefreshCombinedMeasurementReview(); };
            Button quality = CreateButton("Run Quality Check", false);
            quality.Size = new Size(160, 38);
            quality.Click += ReviewMetrics_Click;
            Button bike = CreateButton("Edit Bike Metrics", false);
            bike.Size = new Size(145, 38);
            bike.Click += delegate { SelectWorkspaceTab("Bike Metrics"); };
            Button rider = CreateButton("Edit Body Angles", false);
            rider.Size = new Size(145, 38);
            rider.Click += delegate { SelectWorkspaceTab("Body Angles"); };
            Button report = CreateButton("Open Report Builder", false);
            report.Size = new Size(165, 38);
            report.Click += delegate { SelectWorkspaceTab("Report Builder"); };
            actions.Controls.Add(refresh);
            actions.Controls.Add(quality);
            actions.Controls.Add(bike);
            actions.Controls.Add(rider);
            actions.Controls.Add(report);

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(combinedMeasurementReviewStatus, 0, 2);
            layout.Controls.Add(combinedMeasurementReview, 0, 3);
            layout.Controls.Add(actions, 0, 4);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage BuildGuidedMeasurementsTab()
        {
            TabPage page = NewTab("Guided Measurements");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.Padding = new Padding(24, 22, 24, 22);
            layout.ColumnCount = 1;
            layout.RowCount = 0;

            Label eyebrow = new Label();
            eyebrow.Text = "SEMI-AUTOMATIC MEASUREMENT WORKFLOW";
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Height = 28;
            layout.Controls.Add(eyebrow, 0, layout.RowCount++);

            Label title = new Label();
            title.Text = "Measure the bike with a guided image";
            title.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            title.Dock = DockStyle.Fill;
            title.Height = 52;
            layout.Controls.Add(title, 0, layout.RowCount++);

            Label intro = new Label();
            intro.Text = "Choose a clear side-view image, confirm camera setup, calibrate one known distance, then click the requested bike landmarks. Cassette Motion Pro calculates the measurements and saves them to Before or After for review.";
            intro.Font = new Font("Segoe UI", 10.5F);
            intro.ForeColor = Color.FromArgb(74, 87, 81);
            intro.Dock = DockStyle.Fill;
            intro.Height = 66;
            layout.Controls.Add(intro, 0, layout.RowCount++);

            FlowLayoutPanel imageActions = new FlowLayoutPanel();
            imageActions.Dock = DockStyle.Fill;
            imageActions.FlowDirection = FlowDirection.LeftToRight;
            imageActions.WrapContents = true;
            imageActions.Padding = new Padding(0, 8, 0, 4);

            Button useBefore = CreateButton("1. Use Before Image", false);
            useBefore.Size = new Size(170, 38);
            useBefore.Click += delegate { UseMeasurementReferenceImage("BeforeReportImagePath", "Before image"); };
            Button useAfter = CreateButton("Use After Image", false);
            useAfter.Size = new Size(155, 38);
            useAfter.Click += delegate { UseMeasurementReferenceImage("AfterReportImagePath", "After image"); };
            Button useDual = CreateButton("Use Side-by-side", false);
            useDual.Size = new Size(155, 38);
            useDual.Click += delegate { UseMeasurementReferenceImage("SideBySideReportImagePath", "Side-by-side image"); };
            Button browse = CreateButton("Browse Image…", false);
            browse.Size = new Size(145, 38);
            browse.Click += delegate { BrowseReportImage("MeasurementReferenceImagePath"); };
            imageActions.Controls.Add(useBefore);
            imageActions.Controls.Add(useAfter);
            imageActions.Controls.Add(useDual);
            imageActions.Controls.Add(browse);
            layout.Controls.Add(imageActions, 0, layout.RowCount++);

            FlowLayoutPanel workflowActions = new FlowLayoutPanel();
            workflowActions.Dock = DockStyle.Fill;
            workflowActions.FlowDirection = FlowDirection.LeftToRight;
            workflowActions.WrapContents = true;
            workflowActions.Padding = new Padding(0, 8, 0, 8);

            Button start = CreateButton("2. Start Guided Measurements", true);
            start.Size = new Size(235, 42);
            start.Click += delegate { ShowGuidedBikeMetricCapture(); };
            Button review = CreateButton("3. Review Measurements", false);
            review.Size = new Size(205, 42);
            review.Click += ReviewMetrics_Click;
            Button edit = CreateButton("Open Bike Metrics", false);
            edit.Size = new Size(175, 42);
            edit.Click += delegate { SelectWorkspaceTab("Bike Metrics"); };
            workflowActions.Controls.Add(start);
            workflowActions.Controls.Add(review);
            workflowActions.Controls.Add(edit);
            layout.Controls.Add(workflowActions, 0, layout.RowCount++);

            Label note = new Label();
            note.Text = "The guide assists with point order and calculations; you remain in control of point placement and can drag points to fine-tune them. All original Video Studio drawing and measurement tools remain available.";
            note.Dock = DockStyle.Fill;
            note.Height = 64;
            note.ForeColor = Color.FromArgb(92, 104, 98);
            note.BackColor = Color.FromArgb(248, 252, 238);
            note.Padding = new Padding(12, 12, 12, 8);
            layout.Controls.Add(note, 0, layout.RowCount++);

            page.AutoScroll = true;
            page.Controls.Add(layout);
            return page;
        }

        private TabPage BuildReportWorkspaceTab()
        {
            return BuildGroupedWorkspaceTab("Report", BuildFitSessionFinalizationTab(), BuildReportBuilderTab(), BuildFitSummaryTab(), BuildReportImagesTab(), BuildHandoffTab(), BuildNotesTab());
        }

        private TabPage BuildFitSessionFinalizationTab()
        {
            TabPage page = NewTab("Finalize Fit");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24, 22, 24, 18);
            layout.ColumnCount = 1;
            layout.RowCount = 6;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

            Label eyebrow = new Label();
            eyebrow.Text = "FIT SESSION FINALIZATION ASSISTANT";
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);

            Label title = new Label();
            title.Text = "Finish, review, and package the client fit";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);

            finalizationStatus.Dock = DockStyle.Fill;
            finalizationStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            finalizationStatus.BackColor = Color.FromArgb(248, 252, 238);
            finalizationStatus.ForeColor = Color.FromArgb(74, 87, 81);
            finalizationStatus.Padding = new Padding(12, 10, 12, 8);

            finalizationChecklist.Dock = DockStyle.Fill;
            finalizationChecklist.Multiline = true;
            finalizationChecklist.ReadOnly = true;
            finalizationChecklist.ScrollBars = ScrollBars.Vertical;
            finalizationChecklist.BackColor = Color.White;
            finalizationChecklist.ForeColor = Color.FromArgb(24, 31, 29);
            finalizationChecklist.Font = new Font("Segoe UI", 10F);

            FlowLayoutPanel reviewActions = new FlowLayoutPanel();
            reviewActions.Dock = DockStyle.Fill;
            reviewActions.FlowDirection = FlowDirection.LeftToRight;
            reviewActions.WrapContents = true;
            reviewActions.Padding = new Padding(0, 8, 0, 4);
            Button refresh = CreateButton("Refresh Final Check", true);
            refresh.Size = new Size(170, 38);
            refresh.Click += delegate { RefreshFitSessionFinalization(); };
            Button measurements = CreateButton("Combined Measurements", false);
            measurements.Size = new Size(185, 38);
            measurements.Click += delegate { SelectWorkspaceTab("Combined Review"); };
            Button recommendations = CreateButton("Smart Recommendations", false);
            recommendations.Size = new Size(180, 38);
            recommendations.Click += delegate { SelectWorkspaceTab("Report Builder"); };
            Button preview = CreateButton("Preview Report", false);
            preview.Size = new Size(135, 38);
            preview.Click += delegate { PreviewReport_Click(this, EventArgs.Empty); };
            reviewActions.Controls.Add(refresh);
            reviewActions.Controls.Add(measurements);
            reviewActions.Controls.Add(recommendations);
            reviewActions.Controls.Add(preview);

            FlowLayoutPanel finishActions = new FlowLayoutPanel();
            finishActions.Dock = DockStyle.Fill;
            finishActions.FlowDirection = FlowDirection.LeftToRight;
            finishActions.WrapContents = true;
            finishActions.Padding = new Padding(0, 8, 0, 4);
            Button complete = CreateButton("Mark Session Complete", true);
            complete.Size = new Size(190, 38);
            complete.Click += delegate { TryMarkFitSessionComplete(); };
            Button package = CreateButton("Complete + Package", false);
            package.Size = new Size(170, 38);
            package.Click += delegate { if (TryMarkFitSessionComplete()) ReportPackage_Click(this, EventArgs.Empty); };
            Button zip = CreateButton("Complete + ZIP", false);
            zip.Size = new Size(150, 38);
            zip.Click += delegate { if (TryMarkFitSessionComplete()) ZipReportPackage_Click(this, EventArgs.Empty); };
            Button open = CreateButton("Open Finished Folder", false);
            open.Size = new Size(175, 38);
            open.Click += delegate { OpenReports_Click(this, EventArgs.Empty); };
            finishActions.Controls.Add(complete);
            finishActions.Controls.Add(package);
            finishActions.Controls.Add(zip);
            finishActions.Controls.Add(open);

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(finalizationStatus, 0, 2);
            layout.Controls.Add(finalizationChecklist, 0, 3);
            layout.Controls.Add(reviewActions, 0, 4);
            layout.Controls.Add(finishActions, 0, 5);
            page.Controls.Add(layout);
            return page;
        }

        private TabPage BuildReportBuilderTab()
        {
            TabPage page = NewTab("Report Builder");
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.Padding = new Padding(24, 22, 24, 22);
            layout.ColumnCount = 1;
            layout.RowCount = 0;

            Label title = new Label();
            title.Text = "Build the client report";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            title.TextAlign = ContentAlignment.MiddleLeft;
            AddReportBuilderRow(layout, title, 46);

            Label intro = new Label();
            intro.Text = "Finish the report story, choose the evidence, check the final measurements, then preview before creating the client package.";
            intro.Dock = DockStyle.Fill;
            intro.ForeColor = Color.FromArgb(74, 87, 81);
            intro.TextAlign = ContentAlignment.TopLeft;
            AddReportBuilderRow(layout, intro, 42);

            reportBuilderStatus.Dock = DockStyle.Fill;
            reportBuilderStatus.BackColor = Color.White;
            reportBuilderStatus.BorderStyle = BorderStyle.FixedSingle;
            reportBuilderStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            reportBuilderStatus.ForeColor = Color.FromArgb(74, 87, 81);
            reportBuilderStatus.Padding = new Padding(14, 10, 14, 8);
            reportBuilderStatus.TextAlign = ContentAlignment.TopLeft;
            AddReportBuilderRow(layout, reportBuilderStatus, 142);

            GroupBox smartRecommendations = new GroupBox();
            smartRecommendations.Text = "Smart Before / After recommendations";
            smartRecommendations.Dock = DockStyle.Fill;
            smartRecommendations.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            smartRecommendations.ForeColor = Color.FromArgb(37, 48, 43);
            smartRecommendations.Padding = new Padding(12, 10, 12, 10);

            TableLayoutPanel recommendationLayout = new TableLayoutPanel();
            recommendationLayout.Dock = DockStyle.Fill;
            recommendationLayout.ColumnCount = 1;
            recommendationLayout.RowCount = 3;
            recommendationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            recommendationLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            recommendationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            smartRecommendationStatus.Dock = DockStyle.Fill;
            smartRecommendationStatus.ForeColor = Color.FromArgb(74, 87, 81);
            smartRecommendationStatus.Text = "Generate a draft after entering Before and After measurements. You decide what becomes part of the client report.";

            smartRecommendationDraft.Dock = DockStyle.Fill;
            smartRecommendationDraft.Multiline = true;
            smartRecommendationDraft.ScrollBars = ScrollBars.Vertical;
            smartRecommendationDraft.BackColor = Color.White;
            smartRecommendationDraft.ForeColor = Color.FromArgb(24, 31, 29);
            smartRecommendationDraft.Font = new Font("Segoe UI", 9.5F);

            FlowLayoutPanel recommendationActions = new FlowLayoutPanel();
            recommendationActions.Dock = DockStyle.Fill;
            recommendationActions.FlowDirection = FlowDirection.LeftToRight;
            recommendationActions.WrapContents = true;
            Button generateSuggestions = CreateButton("Generate Draft", true);
            generateSuggestions.Size = new Size(145, 36);
            generateSuggestions.Click += delegate { GenerateSmartRecommendationDraft(); };
            Button addRecommendations = CreateButton("Add to Recommendations", false);
            addRecommendations.Size = new Size(185, 36);
            addRecommendations.Click += delegate { AddSmartDraftToSummary(txtFitSummaryRecommendations, "Recommendations"); };
            Button addFollowUp = CreateButton("Add to Follow-up", false);
            addFollowUp.Size = new Size(145, 36);
            addFollowUp.Click += delegate { AddSmartDraftToSummary(txtFitSummaryFollowUp, "Follow-up plan"); };
            Button clearDraft = CreateButton("Clear Draft", false);
            clearDraft.Size = new Size(110, 36);
            clearDraft.Click += delegate { smartRecommendationDraft.Clear(); RefreshSmartRecommendationStatus(); };
            recommendationActions.Controls.Add(generateSuggestions);
            recommendationActions.Controls.Add(addRecommendations);
            recommendationActions.Controls.Add(addFollowUp);
            recommendationActions.Controls.Add(clearDraft);

            recommendationLayout.Controls.Add(smartRecommendationStatus, 0, 0);
            recommendationLayout.Controls.Add(smartRecommendationDraft, 0, 1);
            recommendationLayout.Controls.Add(recommendationActions, 0, 2);
            smartRecommendations.Controls.Add(recommendationLayout);
            AddReportBuilderRow(layout, smartRecommendations, 286);

            GroupBox sections = new GroupBox();
            sections.Text = "Report sections";
            sections.Dock = DockStyle.Fill;
            sections.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            sections.ForeColor = Color.FromArgb(37, 48, 43);
            sections.Padding = new Padding(12, 8, 12, 10);

            FlowLayoutPanel sectionButtons = new FlowLayoutPanel();
            sectionButtons.Dock = DockStyle.Fill;
            sectionButtons.FlowDirection = FlowDirection.LeftToRight;
            sectionButtons.WrapContents = true;
            sectionButtons.AutoScroll = true;
            AddReportBuilderButton(sectionButtons, "1. Fit Summary", true, delegate { SelectWorkspaceTab("Fit Summary"); });
            AddReportBuilderButton(sectionButtons, "2. Report Images", false, delegate { SelectWorkspaceTab("Report Images"); });
            AddReportBuilderButton(sectionButtons, "3. Measurements", false, delegate { SelectWorkspaceTab("Bike Metrics"); });
            AddReportBuilderButton(sectionButtons, "4. Handoff", false, delegate { SelectWorkspaceTab("Handoff"); });
            AddReportBuilderButton(sectionButtons, "Report Options", false, delegate { SelectWorkspaceTab("Report Images"); });
            sections.Controls.Add(sectionButtons);
            AddReportBuilderRow(layout, sections, 92);

            GroupBox actions = new GroupBox();
            actions.Text = "Review and output";
            actions.Dock = DockStyle.Fill;
            actions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            actions.ForeColor = Color.FromArgb(37, 48, 43);
            actions.Padding = new Padding(12, 8, 12, 10);

            FlowLayoutPanel actionButtons = new FlowLayoutPanel();
            actionButtons.Dock = DockStyle.Fill;
            actionButtons.FlowDirection = FlowDirection.LeftToRight;
            actionButtons.WrapContents = true;
            actionButtons.AutoScroll = true;
            AddReportBuilderButton(actionButtons, "Check Readiness", false, delegate { ReviewSession_Click(this, EventArgs.Empty); });
            AddReportBuilderButton(actionButtons, "Preview Report", true, delegate { PreviewReport_Click(this, EventArgs.Empty); });
            AddReportBuilderButton(actionButtons, "Generate Report", false, delegate { GenerateReport_Click(this, EventArgs.Empty); });
            AddReportBuilderButton(actionButtons, "Create Package", false, delegate { ReportPackage_Click(this, EventArgs.Empty); });
            AddReportBuilderButton(actionButtons, "Create Zip", false, delegate { ZipReportPackage_Click(this, EventArgs.Empty); });
            AddReportBuilderButton(actionButtons, "Open Reports", false, delegate { OpenReports_Click(this, EventArgs.Empty); });
            actions.Controls.Add(actionButtons);
            AddReportBuilderRow(layout, actions, 98);

            reportBuilderOutput.Dock = DockStyle.Fill;
            reportBuilderOutput.BackColor = Color.FromArgb(247, 255, 229);
            reportBuilderOutput.BorderStyle = BorderStyle.FixedSingle;
            reportBuilderOutput.ForeColor = Color.FromArgb(74, 87, 81);
            reportBuilderOutput.Padding = new Padding(12, 8, 12, 8);
            reportBuilderOutput.TextAlign = ContentAlignment.MiddleLeft;
            AddReportBuilderRow(layout, reportBuilderOutput, 66);

            txtFitSummaryMainGoal.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtFitSummaryKeyFindings.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtFitSummaryChangesMade.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtFitSummaryRecommendations.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtFitSummaryFollowUp.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtHandoffWhatToSend.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtHandoffClientMessage.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtHandoffHomework.TextChanged += delegate { UpdateReportBuilderStatus(); };
            txtHandoffNextAppointment.TextChanged += delegate { UpdateReportBuilderStatus(); };

            page.AutoScroll = true;
            page.Controls.Add(layout);
            UpdateReportBuilderStatus();
            return page;
        }

        private static void AddReportBuilderRow(TableLayoutPanel layout, Control control, int height)
        {
            int row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            control.Margin = new Padding(0, 0, 0, 8);
            layout.Controls.Add(control, 0, row);
        }

        private void AddReportBuilderButton(FlowLayoutPanel buttons, string text, bool primary, Action action)
        {
            Button button = CreateButton(text, primary);
            button.Size = new Size(148, 36);
            button.Margin = new Padding(0, 4, 8, 4);
            button.Click += delegate
            {
                if (action != null)
                    action();
                UpdateReportBuilderStatus();
            };
            buttons.Controls.Add(button);
        }

        private static TabPage BuildGroupedWorkspaceTab(string title, params TabPage[] pages)
        {
            TabPage group = NewTab(title);
            TabControl sections = new TabControl();
            sections.Dock = DockStyle.Fill;
            sections.Padding = new Point(16, 7);

            foreach (TabPage page in pages)
                sections.TabPages.Add(page);

            group.Controls.Add(sections);
            return group;
        }

        private TabPage BuildFitDayDashboardTab()
        {
            TabPage page = NewTab(FitDayHomeTabName);
            page.AutoScroll = true;
            page.Controls.Add(BuildFitDayHomePanel());
            return page;
        }

        private Control BuildFitDayHomePanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 610;
            panel.BackColor = CassetteMotionTheme.Canvas;
            panel.Padding = new Padding(28, 22, 28, 18);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 8;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            RowStyle advancedRow = new RowStyle(SizeType.Absolute, 0);
            layout.RowStyles.Add(advancedRow);

            Label title = new Label();
            title.Text = "Fit Day Dashboard";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.ForeColor = CassetteMotionTheme.Ink;
            title.TextAlign = ContentAlignment.MiddleLeft;

            Label description = new Label();
            description.Text = "One clear path for today’s fitting. Complete each stage from left to right; Cassette Motion Pro keeps every save tied to the active client session.";
            description.Dock = DockStyle.Fill;
            description.ForeColor = CassetteMotionTheme.Muted;

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = true;
            buttons.Padding = new Padding(0, 8, 0, 6);

            Button clientInfo = CreateButton("1  CLIENT + SESSION", true);
            clientInfo.Size = new Size(160, 46);
            clientInfo.Click += delegate { SelectFitSessionStart(); };

            Button video = CreateButton("2  VIDEO STUDIO", false);
            video.Size = new Size(150, 46);
            video.Click += delegate { PrepareAndSelectVideoAnalysis(); };

            Button measurements = CreateButton("3  MEASUREMENTS", false);
            measurements.Size = new Size(165, 46);
            measurements.Click += delegate { SelectWorkspaceTab("Guided Measurements"); };

            Button report = CreateButton("4  REPORT", false);
            report.Size = new Size(130, 46);
            report.Click += delegate { SelectWorkspaceTab("Report Builder"); };

            buttons.Controls.Add(clientInfo);
            buttons.Controls.Add(video);
            buttons.Controls.Add(measurements);
            buttons.Controls.Add(report);

            fitDayHomeStatus.Dock = DockStyle.Fill;
            fitDayHomeStatus.ForeColor = CassetteMotionTheme.Warning;
            fitDayHomeStatus.BackColor = Color.FromArgb(255, 248, 226);
            fitDayHomeStatus.BorderStyle = BorderStyle.FixedSingle;
            fitDayHomeStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            fitDayHomeStatus.TextAlign = ContentAlignment.MiddleLeft;
            fitDayHomeStatus.Padding = new Padding(10, 0, 10, 0);

            fitDayHomeReadiness.Dock = DockStyle.Fill;
            fitDayHomeReadiness.ForeColor = CassetteMotionTheme.Muted;
            fitDayHomeReadiness.BackColor = CassetteMotionTheme.Surface;
            fitDayHomeReadiness.BorderStyle = BorderStyle.FixedSingle;
            fitDayHomeReadiness.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            fitDayHomeReadiness.TextAlign = ContentAlignment.TopLeft;
            fitDayHomeReadiness.Padding = new Padding(10, 8, 10, 0);

            CassetteMotionTheme.StyleButton(fitDayPrimaryAction, true);
            fitDayPrimaryAction.Text = "DO NEXT STEP";
            fitDayPrimaryAction.Dock = DockStyle.Left;
            fitDayPrimaryAction.Width = 210;
            fitDayPrimaryAction.Margin = new Padding(0, 9, 0, 9);
            fitDayPrimaryAction.Click += delegate { RunNextBestFitDayStep(); };

            Button moreOptions = CreateButton("More Options + Folders", false);
            moreOptions.Dock = DockStyle.Left;
            moreOptions.Width = 190;
            moreOptions.Margin = new Padding(0, 5, 0, 5);
            moreOptions.Click += delegate
            {
                fitDayAdvancedPanel.Visible = !fitDayAdvancedPanel.Visible;
                advancedRow.Height = fitDayAdvancedPanel.Visible ? 180 : 0;
                panel.Height = fitDayAdvancedPanel.Visible ? 790 : 610;
                moreOptions.Text = fitDayAdvancedPanel.Visible ? "Hide Options" : "More Options + Folders";
            };

            fitDayAdvancedPanel.Dock = DockStyle.Fill;
            fitDayAdvancedPanel.Visible = false;
            fitDayAdvancedPanel.Controls.Add(BuildFitDayHomeFolderPanel());

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(description, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            layout.Controls.Add(fitDayHomeStatus, 0, 3);
            layout.Controls.Add(fitDayPrimaryAction, 0, 4);
            layout.Controls.Add(fitDayHomeReadiness, 0, 5);
            layout.Controls.Add(moreOptions, 0, 6);
            layout.Controls.Add(fitDayAdvancedPanel, 0, 7);
            panel.Controls.Add(layout);
            UpdateFitDayHomeStatus();
            return panel;
        }

        private Control BuildFitDayHomeFolderPanel()
        {
            GroupBox group = new GroupBox();
            group.Text = "Active client save folders";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = Color.FromArgb(37, 48, 43);
            group.Padding = new Padding(10, 8, 10, 8);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));

            fitDayHomeFolders.Dock = DockStyle.Fill;
            fitDayHomeFolders.Font = new Font("Segoe UI", 8.75F, FontStyle.Regular);
            fitDayHomeFolders.ForeColor = Color.FromArgb(74, 87, 81);
            fitDayHomeFolders.TextAlign = ContentAlignment.MiddleLeft;

            fitDayHomeFolderButtons.Dock = DockStyle.Fill;
            fitDayHomeFolderButtons.FlowDirection = FlowDirection.LeftToRight;
            fitDayHomeFolderButtons.WrapContents = true;
            fitDayHomeFolderButtons.Padding = new Padding(0, 4, 0, 0);

            AddFitDayHomeFolderButton("Before Videos", delegate { OpenFitDayHomeFolder("Before videos", delegate { return GetSessionVideoViewFolderPath("Before"); }); });
            AddFitDayHomeFolderButton("After Videos", delegate { OpenFitDayHomeFolder("After videos", delegate { return GetSessionVideoViewFolderPath("After"); }); });
            AddFitDayHomeFolderButton("Dual Videos", delegate { OpenFitDayHomeFolder("Dual videos", delegate { return GetSessionVideoViewFolderPath("Dual"); }); });
            AddFitDayHomeFolderButton("Dual Images", delegate { OpenFitDayHomeFolder("Dual images", delegate { return GetSessionSideBySideFolderPath(); }); });
            AddFitDayHomeFolderButton("Report Images", delegate { OpenFitDayHomeFolder("Report images", delegate { return GetSessionReportImagesFolderPath(); }); });
            AddFitDayHomeFolderButton("Captures", delegate { OpenFitDayHomeFolder("Analysis Captures", delegate { return GetSessionAnalysisCapturesFolderPath(); }); });

            layout.Controls.Add(fitDayHomeFolders, 0, 0);
            layout.Controls.Add(fitDayHomeFolderButtons, 0, 1);
            group.Controls.Add(layout);
            return group;
        }

        private void AddFitDayHomeFolderButton(string text, Action action)
        {
            Button button = CreateButton(text, false);
            button.Size = new Size(122, 32);
            button.Margin = new Padding(0, 3, 7, 3);
            button.Click += delegate
            {
                if (action != null)
                    action();
                UpdateWorkflowChecklist();
            };
            fitDayHomeFolderButtons.Controls.Add(button);
        }

        private void OpenFitDayHomeFolder(string folderName, Func<string> folderProvider)
        {
            if (!HasActiveFitSession())
            {
                UpdateSaveHint("Open or save a client fit session first, then these folder shortcuts will open the right Before / After / Dual folders.");
                SelectFitSessionStart();
                return;
            }

            try
            {
                string folderPath = folderProvider();
                OpenClientFolder(folderPath, folderName);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The " + folderName + " folder could not be opened.\n\n" + exception.Message, folderName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateFitDayHomeFolderPanel()
        {
            bool hasSession = HasActiveFitSession();
            foreach (Control control in fitDayHomeFolderButtons.Controls)
                control.Enabled = hasSession;

        if (!hasSession)
        {
            fitDayHomeFolders.Text = GetActiveSaveTargetFolderText();
            fitDayHomeFolders.ForeColor = Color.FromArgb(181, 118, 35);
            return;
        }

        fitDayHomeFolders.ForeColor = Color.FromArgb(74, 87, 81);
        fitDayHomeFolders.Text = GetActiveSaveTargetFolderText();
        }

        private Control BuildGuidedFitDayFlowMap()
        {
            fitDayFlowSteps.Clear();

            GroupBox group = new GroupBox();
            group.Text = "Cassette Motion Pro Fit Day Roadmap";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = Color.FromArgb(37, 48, 43);
            group.Padding = new Padding(12, 8, 12, 12);

            TableLayoutPanel path = new TableLayoutPanel();
            path.Dock = DockStyle.Fill;
            path.ColumnCount = 5;
            path.RowCount = 1;
            for (int i = 0; i < 5; i++)
                path.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            path.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            path.Controls.Add(CreateFitDayFlowCard("1", "Start Here", "Open or create the active client fit session.", "Open Session", SelectFitSessionStart, IsClientFlowStageReady, GetClientFlowDetail), 0, 0);
            path.Controls.Add(CreateFitDayFlowCard("2", "Video Studio", "Record Before / After clips into this client session.", "Record Live", OpenDualLiveCapture, HasBeforeAfterVideos, GetVideoFlowDetail), 1, 0);
            path.Controls.Add(CreateFitDayFlowCard("3", "Evidence", "Analyze latest clips and save images/videos for the report.", "Analyze", PrepareAndSelectVideoAnalysis, IsVideoFlowStageReady, GetVideoFlowDetail), 2, 0);
            path.Controls.Add(CreateFitDayFlowCard("4", "Metrics", "Enter bike metrics and body angle notes.", "Bike Metrics", delegate { SelectWorkspaceTab("Bike Metrics"); }, IsMeasurementFlowStageReady, GetMeasurementFlowDetail), 3, 0);
            path.Controls.Add(CreateFitDayFlowCard("5", "Report", "Review the story, images, measurements, and client package.", "Report Builder", delegate { SelectWorkspaceTab("Report Builder"); }, IsReportFlowStageReady, GetReportFlowDetail), 4, 0);

            group.Controls.Add(path);
            return group;
        }

        private Control CreateFitDayFlowCard(string number, string title, string detail, string buttonText, Action action, Func<bool> isReady, Func<string> getDetail)
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = Color.White;
            card.Padding = new Padding(10);
            card.Margin = new Padding(4);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            Label numberLabel = new Label();
            numberLabel.Dock = DockStyle.Fill;
            numberLabel.TextAlign = ContentAlignment.MiddleCenter;
            numberLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            numberLabel.BackColor = Color.FromArgb(224, 232, 227);
            numberLabel.ForeColor = Color.FromArgb(37, 48, 43);
            numberLabel.Text = number;

            Label titleLabel = new Label();
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.ForeColor = Color.FromArgb(24, 31, 29);
            titleLabel.Text = title;

            Label detailLabel = FieldLabel(detail);
            detailLabel.Dock = DockStyle.Fill;
            detailLabel.AutoEllipsis = true;
            detailLabel.ForeColor = Color.FromArgb(92, 104, 98);

            Button actionButton = CreateButton(buttonText, false);
            actionButton.Dock = DockStyle.Fill;
            actionButton.Margin = new Padding(0, 4, 0, 0);
            actionButton.Click += delegate
            {
                if (action != null)
                    action();
                UpdateWorkflowChecklist();
            };

            layout.Controls.Add(numberLabel, 0, 0);
            layout.SetRowSpan(numberLabel, 2);
            layout.Controls.Add(titleLabel, 1, 0);
            layout.Controls.Add(detailLabel, 0, 2);
            layout.SetColumnSpan(detailLabel, 2);
            layout.Controls.Add(actionButton, 0, 3);
            layout.SetColumnSpan(actionButton, 2);

            card.Controls.Add(layout);
            fitDayFlowSteps.Add(new FitDayFlowStep(card, numberLabel, titleLabel, detailLabel, actionButton, number, isReady, getDetail));
            return card;
        }

        private Control BuildWorkflowShortcutBar()
        {
            GroupBox group = new GroupBox();
            group.Text = "Fit Day Shortcuts";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = Color.FromArgb(37, 48, 43);
            group.Padding = new Padding(14, 8, 14, 12);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = true;
            buttons.Padding = new Padding(0, 4, 0, 0);

            AddWorkflowShortcutButton(buttons, "Client Folders", false, delegate { SelectWorkspaceTab("Client Files"); });
            AddWorkflowShortcutButton(buttons, "1. Start Session", true, SelectFitSessionStart);
            AddWorkflowShortcutButton(buttons, "2. Record / Analyze", false, SaveAndSelectVideos);
            AddWorkflowShortcutButton(buttons, "3. Metrics", false, delegate { SelectWorkspaceTab("Bike Metrics"); });
            AddWorkflowShortcutButton(buttons, "4. Report", false, delegate { SelectWorkspaceTab("Report Images"); });

            group.Controls.Add(buttons);
            return group;
        }

        private Control BuildFitCommandCenter()
        {
            GroupBox group = new GroupBox();
            group.Text = "Fit Day Command Center — Client → Video Studio → Measurements → Report";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = Color.FromArgb(37, 48, 43);
            group.Padding = new Padding(14, 8, 14, 12);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 6;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            fitCommandCenterStatus.Dock = DockStyle.Fill;
            fitCommandCenterStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            fitCommandCenterStatus.ForeColor = Color.FromArgb(74, 87, 81);
            fitCommandCenterStatus.TextAlign = ContentAlignment.MiddleLeft;
            fitCommandCenterStatus.Text = "Ready check: 0/6 complete" + Environment.NewLine +
                "□ Session  □ Before  □ After  □ Evidence  □ Metrics  □ Report image" + Environment.NewLine +
                "Next: create or choose a client fit session.";

            activeSaveTargetStatus.Dock = DockStyle.Fill;
            activeSaveTargetStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            activeSaveTargetStatus.ForeColor = Color.FromArgb(181, 118, 35);
            activeSaveTargetStatus.BackColor = Color.FromArgb(255, 248, 226);
            activeSaveTargetStatus.BorderStyle = BorderStyle.FixedSingle;
            activeSaveTargetStatus.Padding = new Padding(8, 0, 8, 0);
            activeSaveTargetStatus.TextAlign = ContentAlignment.MiddleLeft;
            activeSaveTargetStatus.Text = "START HERE: create/open a client fit session, then Save. Once active, Video Studio Save Image / Save Video can use Before / After / Dual automatically.";

            ConfigureFitCommandSectionLabel(captureActionsLabel, "Record live: send new clips straight to this client");
            ConfigureFitCommandSectionLabel(analysisActionsLabel, "Analyze + report: open latest videos, save evidence, then preview");

            Panel captureScroll = new Panel();
            captureScroll.Dock = DockStyle.Fill;
            captureScroll.AutoScroll = true;
            captureScroll.BackColor = Color.Transparent;

            FlowLayoutPanel captureButtons = new FlowLayoutPanel();
            captureButtons.AutoSize = true;
            captureButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            captureButtons.FlowDirection = FlowDirection.LeftToRight;
            captureButtons.WrapContents = true;
            captureButtons.Padding = new Padding(0, 2, 0, 0);

            AddFitCommandButton(captureButtons, "Dual Live Capture", true, OpenDualLiveCapture);
            AddFitCommandButton(captureButtons, "Record Before", false, delegate { OpenLiveCaptureForVideo("BeforeVideoPath"); });
            AddFitCommandButton(captureButtons, "Record After", false, delegate { OpenLiveCaptureForVideo("AfterVideoPath"); });
            AddFitCommandButton(captureButtons, "Use Latest Before", false, delegate { UseLatestVideo("BeforeVideoPath"); });
            AddFitCommandButton(captureButtons, "Use Latest After", false, delegate { UseLatestVideo("AfterVideoPath"); });
            AddFitCommandButton(captureButtons, "Client Folders", false, delegate { SelectWorkspaceTab("Client Files"); });
            AddFitCommandButton(captureButtons, "Open Client Folder", false, delegate { OpenClientFolder(client.FolderPath, "Client"); });

            captureScroll.Controls.Add(captureButtons);

            Panel analysisScroll = new Panel();
            analysisScroll.Dock = DockStyle.Fill;
            analysisScroll.AutoScroll = true;
            analysisScroll.BackColor = Color.Transparent;

            FlowLayoutPanel analysisButtons = new FlowLayoutPanel();
            analysisButtons.AutoSize = true;
            analysisButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            analysisButtons.FlowDirection = FlowDirection.LeftToRight;
            analysisButtons.WrapContents = true;
            analysisButtons.Padding = new Padding(0, 2, 0, 0);

            AddFitCommandButton(analysisButtons, "Analyze Latest Before + After", true, UseLatestBothVideos);
            AddFitCommandButton(analysisButtons, "Analyze Before", false, delegate { OpenSingle("BeforeVideoPath"); });
            AddFitCommandButton(analysisButtons, "Analyze After", false, delegate { OpenSingle("AfterVideoPath"); });
            AddFitCommandButton(analysisButtons, "Captures Folder", false, OpenAnalysisCapturesFolder);
            AddFitCommandButton(analysisButtons, "Report Images", false, delegate { SelectWorkspaceTab("Report Images"); });
            AddFitCommandButton(analysisButtons, "Review Evidence", true, delegate
            {
                RefreshSavedEvidenceReview();
                SelectWorkspaceTab(KinoveaVideoTabName);
            });
            AddFitCommandButton(analysisButtons, "Do Next Step", true, RunNextBestFitDayStep);
            AddFitCommandButton(analysisButtons, "Check Report Readiness", false, delegate { ReviewSession_Click(this, EventArgs.Empty); });
            AddFitCommandButton(analysisButtons, "Preview Report", false, delegate { PreviewReport_Click(this, EventArgs.Empty); });

            analysisScroll.Controls.Add(analysisButtons);

            layout.Controls.Add(fitCommandCenterStatus, 0, 0);
            layout.Controls.Add(activeSaveTargetStatus, 0, 1);
            layout.Controls.Add(captureActionsLabel, 0, 2);
            layout.Controls.Add(captureScroll, 0, 3);
            layout.Controls.Add(analysisActionsLabel, 0, 4);
            layout.Controls.Add(analysisScroll, 0, 5);
            group.Controls.Add(layout);
            return group;
        }

        private Control BuildNextRecommendedStepPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(247, 255, 229);
            panel.Padding = new Padding(18, 12, 18, 12);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 3;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label nextRecommendedStepTitle = new Label();
            nextRecommendedStepTitle.Dock = DockStyle.Fill;
            nextRecommendedStepTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            nextRecommendedStepTitle.ForeColor = Color.FromArgb(74, 87, 81);
            nextRecommendedStepTitle.TextAlign = ContentAlignment.MiddleLeft;
            nextRecommendedStepTitle.Text = "NEXT BEST STEP";

            nextRecommendedStep.Dock = DockStyle.Fill;
            nextRecommendedStep.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            nextRecommendedStep.ForeColor = Color.FromArgb(37, 48, 43);
            nextRecommendedStep.TextAlign = ContentAlignment.MiddleLeft;
            nextRecommendedStep.Text = "Next best step: enter rider goals so the fit has a clear plan.";

            nextRecommendedStepAction.Dock = DockStyle.Fill;
            nextRecommendedStepAction.FlatStyle = FlatStyle.Flat;
            nextRecommendedStepAction.FlatAppearance.BorderSize = 0;
            nextRecommendedStepAction.BackColor = Color.FromArgb(139, 214, 0);
            nextRecommendedStepAction.ForeColor = Color.FromArgb(20, 30, 24);
            nextRecommendedStepAction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            nextRecommendedStepAction.Text = "Open Goals";
            nextRecommendedStepAction.Click += delegate
            {
                if (nextRecommendedStepActionHandler != null)
                    nextRecommendedStepActionHandler();
                UpdateWorkflowChecklist();
            };

            nextRecommendedFolderAction.Dock = DockStyle.Fill;
            nextRecommendedFolderAction.FlatStyle = FlatStyle.Flat;
            nextRecommendedFolderAction.FlatAppearance.BorderSize = 1;
            nextRecommendedFolderAction.FlatAppearance.BorderColor = Color.FromArgb(139, 214, 0);
            nextRecommendedFolderAction.BackColor = Color.White;
            nextRecommendedFolderAction.ForeColor = Color.FromArgb(37, 48, 43);
            nextRecommendedFolderAction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            nextRecommendedFolderAction.Margin = new Padding(8, 0, 0, 0);
            nextRecommendedFolderAction.Text = "Open Folder";
            nextRecommendedFolderAction.Click += delegate
            {
                if (nextRecommendedFolderActionHandler != null)
                    nextRecommendedFolderActionHandler();
                UpdateWorkflowChecklist();
            };

            layout.Controls.Add(nextRecommendedStepTitle, 0, 0);
            layout.SetColumnSpan(nextRecommendedStepTitle, 3);
            layout.Controls.Add(nextRecommendedStep, 0, 1);
            layout.Controls.Add(nextRecommendedStepAction, 1, 1);
            layout.Controls.Add(nextRecommendedFolderAction, 2, 1);
            panel.Controls.Add(layout);
            return panel;
        }

        private void AddWorkflowShortcutButton(FlowLayoutPanel buttons, string text, bool primary, Action action)
        {
            Button button = CreateButton(text, primary);
            button.Size = new Size(158, 34);
            button.Margin = new Padding(0, 4, 8, 4);
            button.Click += delegate
            {
                if (action != null)
                    action();
                UpdateWorkflowChecklist();
            };
            buttons.Controls.Add(button);
        }

        private void AddFitCommandButton(FlowLayoutPanel buttons, string text, bool primary, Action action)
        {
            Button button = CreateButton(text, primary);
            button.Size = new Size(148, 34);
            button.Margin = new Padding(0, 3, 7, 3);
            button.Click += delegate
            {
                if (action != null)
                    action();
                UpdateWorkflowChecklist();
            };
            buttons.Controls.Add(button);
        }

        private void ConfigureFitCommandSectionLabel(Label label, string text)
        {
            label.Dock = DockStyle.Fill;
            label.Font = new Font("Segoe UI", 8.75F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(37, 48, 43);
            label.TextAlign = ContentAlignment.BottomLeft;
            label.Text = text;
        }

        private Control BuildWorkflowChecklist()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(0, 14, 0, 0);

            TableLayoutPanel card = new TableLayoutPanel();
            card.Dock = DockStyle.Fill;
            card.BackColor = Color.White;
            card.Padding = new Padding(18, 14, 18, 12);
            card.ColumnCount = 4;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));

            Label title = new Label();
            title.Text = "Fit Day Checklist";
            title.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;

            Label hint = new Label();
            hint.Text = "Work left to right: Session → Record → Analyze → Save Evidence → Metrics → Report.";
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            hint.Dock = DockStyle.Fill;
            hint.TextAlign = ContentAlignment.MiddleLeft;

            int headerRow = card.RowCount++;
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            card.Controls.Add(title, 0, headerRow);
            card.SetColumnSpan(title, 2);
            card.Controls.Add(hint, 2, headerRow);
            card.SetColumnSpan(hint, 2);

            workflowChecklistItems.Clear();
            AddWorkflowStageHeader(card, "1. Session", "Set up the person, bike, goals, and session before touching video.");
            AddWorkflowChecklistRow(card, "Client info", "Confirm the client folder, bike, and contact info before recording.", "Client", delegate { SelectWorkspaceTab("Client Files"); }, HasClientFolder);
            AddWorkflowChecklistRow(card, "Fit goals", "Enter the rider goals and session notes before making changes.", "Goals", SelectOverviewGoals, HasFitGoals);
            AddWorkflowStageHeader(card, "2. Video Studio", "Record, review, measure, and save useful Before / After / Dual evidence back to this client.");
            AddWorkflowChecklistRow(card, "Before video", "Record/import the starting video into this client session.", "Video", delegate { SelectWorkspaceTab(KinoveaVideoTabName); }, delegate { return HasMediaFile("BeforeVideoPath"); });
            AddWorkflowChecklistRow(card, "After video", "Record/import the comparison/final video into this client session.", "Video", delegate { SelectWorkspaceTab(KinoveaVideoTabName); }, delegate { return HasMediaFile("AfterVideoPath"); });
            AddWorkflowChecklistRow(card, "Analyze in Video Studio", "Open playback analysis, use the video tools, and save useful evidence into Analysis Captures.", "Tools", PrepareAndSelectVideoAnalysis, delegate { return HasMediaFile("BeforeVideoPath") || HasMediaFile("AfterVideoPath"); });
            AddWorkflowChecklistRow(card, "Saved evidence", "Save screenshots, exported images, or useful video evidence into Analysis Captures.", "Captures", PrepareAndSelectVideoAnalysis, HasAnalysisCaptureEvidence);
            AddWorkflowStageHeader(card, "3. Bike Metrics", "Enter the bike numbers and body angles you want reflected in the report.");
            AddWorkflowChecklistRow(card, "Measurements", "Save the measured saddle height, setback, reach, and handlebar X/Y values.", "Metrics", delegate { SelectWorkspaceTab("Bike Metrics"); }, HasCoreBikeMetrics);
            AddWorkflowStageHeader(card, "4. Report", "Choose report images, preview the client-facing report, then package/send it.");
            AddWorkflowChecklistRow(card, "Report images", "Save/capture the useful analysis photos for the client report.", "Images", delegate { SelectWorkspaceTab("Report Images"); }, HasReportImage);
            AddWorkflowChecklistRow(card, "Preview report", "Review the client-facing report before saving or sending it.", "Preview", delegate { PreviewReport_Click(this, EventArgs.Empty); }, IsReportReady);

            panel.Controls.Add(card);
            return panel;
        }

        private void AddWorkflowStageHeader(TableLayoutPanel table, string stageTitle, string description)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            Label header = new Label();
            header.Dock = DockStyle.Fill;
            header.TextAlign = ContentAlignment.MiddleLeft;
            header.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            header.ForeColor = Color.FromArgb(37, 48, 43);
            header.BackColor = Color.FromArgb(240, 246, 232);
            header.Padding = new Padding(10, 0, 0, 0);
            header.Text = stageTitle + "  —  " + description;

            table.Controls.Add(header, 0, row);
            table.SetColumnSpan(header, 4);
        }

        private void AddWorkflowChecklistRow(TableLayoutPanel table, string labelText, string description, string buttonText, Action action, Func<bool> isReady)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            Label status = new Label();
            status.Dock = DockStyle.Fill;
            status.TextAlign = ContentAlignment.MiddleLeft;
            status.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            Label label = FieldLabel(labelText);
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            Label detail = FieldLabel(description);
            detail.ForeColor = Color.FromArgb(92, 104, 98);
            detail.AutoEllipsis = true;

            Button jump = CreateButton(buttonText, false);
            jump.Dock = DockStyle.Fill;
            jump.Margin = new Padding(8, 5, 0, 5);
            jump.Click += delegate
            {
                if (action != null)
                    action();
                UpdateWorkflowChecklist();
            };

            table.Controls.Add(status, 0, row);
            table.Controls.Add(label, 1, row);
            table.Controls.Add(detail, 2, row);
            table.Controls.Add(jump, 3, row);
            workflowChecklistItems.Add(new WorkflowChecklistItem(status, isReady));
        }

        private TabPage BuildClientHistoryTab()
        {
            TabPage page = NewTab("Client History");
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22, 18, 22, 18);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            Label introduction = new Label();
            introduction.Dock = DockStyle.Fill;
            introduction.Font = new Font("Segoe UI", 10F);
            introduction.ForeColor = Color.FromArgb(74, 87, 81);
            introduction.Text = "Review this client’s saved fits without changing them. Compare a previous fit’s final measurements with the active fit’s Before and After values.";

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 300;
            split.Panel1.Padding = new Padding(0, 0, 12, 0);
            split.Panel2.Padding = new Padding(12, 0, 0, 0);

            historySessionList.Dock = DockStyle.Fill;
            historySessionList.View = View.Details;
            historySessionList.FullRowSelect = true;
            historySessionList.HideSelection = false;
            historySessionList.MultiSelect = false;
            historySessionList.Columns.Add("Previous fit", 150);
            historySessionList.Columns.Add("Date", 82);
            historySessionList.Columns.Add("Status", 78);
            historySessionList.SelectedIndexChanged += delegate { UpdateClientHistoryComparison(); };
            split.Panel1.Controls.Add(historySessionList);

            TableLayoutPanel comparison = new TableLayoutPanel();
            comparison.Dock = DockStyle.Fill;
            comparison.ColumnCount = 1;
            comparison.RowCount = 3;
            comparison.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            comparison.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            comparison.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            historyStatus.Dock = DockStyle.Fill;
            historyStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            historyStatus.ForeColor = Color.FromArgb(37, 48, 43);
            historyStatus.TextAlign = ContentAlignment.MiddleLeft;

            historySummary.Dock = DockStyle.Fill;
            historySummary.Multiline = true;
            historySummary.ReadOnly = true;
            historySummary.ScrollBars = ScrollBars.Vertical;
            historySummary.BackColor = Color.White;
            historySummary.ForeColor = Color.FromArgb(74, 87, 81);

            historyComparisonList.Dock = DockStyle.Fill;
            historyComparisonList.View = View.Details;
            historyComparisonList.FullRowSelect = true;
            historyComparisonList.GridLines = true;
            historyComparisonList.Columns.Add("Measurement", 150);
            historyComparisonList.Columns.Add("Previous final", 100);
            historyComparisonList.Columns.Add("Current before", 100);
            historyComparisonList.Columns.Add("Current final", 100);
            historyComparisonList.Columns.Add("Since previous", 110);

            comparison.Controls.Add(historyStatus, 0, 0);
            comparison.Controls.Add(historySummary, 0, 1);
            comparison.Controls.Add(historyComparisonList, 0, 2);
            split.Panel2.Controls.Add(comparison);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            Button openFit = CreateButton("Open Selected Fit", true);
            openFit.Size = new Size(150, 34);
            openFit.Click += delegate { OpenSelectedHistorySession(); };
            Button startRepeat = CreateButton("Start Repeat Fit", true);
            startRepeat.Size = new Size(150, 34);
            startRepeat.Click += delegate { StartRepeatFitFromSelectedHistory(); };
            Button openFolder = CreateButton("Open Fit Folder", false);
            openFolder.Size = new Size(135, 34);
            openFolder.Click += delegate { OpenSelectedHistoryFolder(); };
            Button refresh = CreateButton("Refresh History", false);
            refresh.Size = new Size(135, 34);
            refresh.Click += delegate { RefreshClientHistory(); };
            actions.Controls.Add(startRepeat);
            actions.Controls.Add(openFit);
            actions.Controls.Add(openFolder);
            actions.Controls.Add(refresh);

            root.Controls.Add(introduction, 0, 0);
            root.Controls.Add(split, 0, 1);
            root.Controls.Add(actions, 0, 2);
            page.Controls.Add(root);
            return page;
        }

        private void RefreshClientHistory()
        {
            Guid selectedId = Guid.Empty;
            if (historySessionList.SelectedItems.Count > 0)
            {
                FitSessionRecord selected = historySessionList.SelectedItems[0].Tag as FitSessionRecord;
                if (selected != null)
                    selectedId = selected.Id;
            }

            historySessionList.BeginUpdate();
            historySessionList.Items.Clear();
            foreach (FitSessionRecord session in repository.LoadAll())
            {
                if (currentSession != null && currentSession.Id != Guid.Empty && session.Id == currentSession.Id)
                    continue;
                ListViewItem item = new ListViewItem(new[]
                {
                    session.DisplayName,
                    session.SessionDate == DateTime.MinValue ? "" : session.SessionDate.ToString("MMM d, yyyy"),
                    session.Status ?? string.Empty
                });
                item.Tag = session;
                historySessionList.Items.Add(item);
            }
            historySessionList.EndUpdate();

            ListViewItem itemToSelect = null;
            foreach (ListViewItem item in historySessionList.Items)
            {
                FitSessionRecord session = item.Tag as FitSessionRecord;
                if (session != null && session.Id == selectedId)
                    itemToSelect = item;
            }
            if (itemToSelect == null && historySessionList.Items.Count > 0)
                itemToSelect = historySessionList.Items[0];
            if (itemToSelect != null)
                itemToSelect.Selected = true;
            else
                UpdateClientHistoryComparison();
        }

        private void UpdateClientHistoryComparison()
        {
            historyComparisonList.Items.Clear();
            historySummary.Clear();
            FitSessionRecord previous = GetSelectedHistorySession();
            if (previous == null)
            {
                historyStatus.Text = "No previous saved fit is available for this client yet.";
                return;
            }
            if (currentSession == null)
            {
                historyStatus.Text = "Open or create the current fit to compare it with history.";
                return;
            }

            string repeatSource = currentSession.RepeatFitSourceSessionId == previous.Id ? " · REPEAT-FIT SOURCE" : string.Empty;
            historyStatus.Text = previous.DisplayName + "  →  " + currentSession.DisplayName + repeatSource;
            historySummary.Text =
                "Previous fit: " + previous.SessionDate.ToString("MMM d, yyyy") + " · " + (previous.Status ?? "No status") + Environment.NewLine +
                "Template: " + (string.IsNullOrWhiteSpace(previous.FitTemplateName) ? "Not recorded" : previous.FitTemplateName) + Environment.NewLine +
                "Main goal: " + HistoryText(previous.FitSummaryMainGoal, previous.Goals) + Environment.NewLine +
                "Changes made: " + HistoryText(previous.FitSummaryChangesMade, "Not recorded") + Environment.NewLine +
                "Recommendations: " + HistoryText(previous.FitSummaryRecommendations, "Not recorded") + Environment.NewLine +
                "Follow-up: " + HistoryText(previous.FitSummaryFollowUp, "Not recorded");

            AddHistoryMetric("Saddle height", previous.SaddleHeightAfter, GetMeasurementText("SaddleHeightBefore"), GetMeasurementText("SaddleHeightAfter"), "mm");
            AddHistoryMetric("Saddle setback", previous.SaddleSetbackAfter, GetMeasurementText("SaddleSetbackBefore"), GetMeasurementText("SaddleSetbackAfter"), "mm");
            AddHistoryMetric("Saddle tip to grip", previous.SaddleTipToGripReachAfter, GetMeasurementText("SaddleTipToGripReachBefore"), GetMeasurementText("SaddleTipToGripReachAfter"), "mm");
            AddHistoryMetric("Handlebar X", previous.HandlebarXAfter, GetMeasurementText("HandlebarXBefore"), GetMeasurementText("HandlebarXAfter"), "mm");
            AddHistoryMetric("Handlebar Y", previous.HandlebarYAfter, GetMeasurementText("HandlebarYBefore"), GetMeasurementText("HandlebarYAfter"), "mm");
            AddHistoryMetric("Knee angle", previous.KneeAngleAfter, GetMeasurementText("KneeAngleBefore"), GetMeasurementText("KneeAngleAfter"), "°");
            AddHistoryMetric("Hip angle", previous.HipAngleAfter, GetMeasurementText("HipAngleBefore"), GetMeasurementText("HipAngleAfter"), "°");
            AddHistoryMetric("Ankle angle", previous.AnkleAngleAfter, GetMeasurementText("AnkleAngleBefore"), GetMeasurementText("AnkleAngleAfter"), "°");
            AddHistoryMetric("Body reach", previous.TorsoAngleAfter, GetMeasurementText("TorsoAngleBefore"), GetMeasurementText("TorsoAngleAfter"), "°");
            AddHistoryMetric("Back angle", previous.ShoulderAngleAfter, GetMeasurementText("ShoulderAngleBefore"), GetMeasurementText("ShoulderAngleAfter"), "°");
        }

        private void AddHistoryMetric(string label, string previousFinal, string currentBefore, string currentFinal, string unit)
        {
            string currentReference = string.IsNullOrWhiteSpace(currentFinal) ? currentBefore : currentFinal;
            string difference = "—";
            double previousNumber;
            double currentNumber;
            if (TryParseMeasurementNumber(previousFinal, out previousNumber) && TryParseMeasurementNumber(currentReference, out currentNumber))
            {
                double change = currentNumber - previousNumber;
                difference = (change > 0 ? "+" : "") + change.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
            }
            historyComparisonList.Items.Add(new ListViewItem(new[]
            {
                label,
                HistoryValue(previousFinal),
                HistoryValue(currentBefore),
                HistoryValue(currentFinal),
                difference
            }));
        }

        private static string HistoryValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        }

        private static string HistoryText(string preferred, string fallback)
        {
            return string.IsNullOrWhiteSpace(preferred) ? (string.IsNullOrWhiteSpace(fallback) ? "Not recorded" : fallback.Trim()) : preferred.Trim();
        }

        private FitSessionRecord GetSelectedHistorySession()
        {
            return historySessionList.SelectedItems.Count == 0 ? null : historySessionList.SelectedItems[0].Tag as FitSessionRecord;
        }

        private void OpenRepeatFitWorkflow()
        {
            if (currentSession != null && currentSession.Id != Guid.Empty)
            {
                StartRepeatFitFromSession(currentSession);
                return;
            }

            RefreshClientHistory();
            SelectWorkspaceTab("Client History");
            UpdateSaveHint(historySessionList.Items.Count == 0
                ? "No saved fit is available yet. Complete the first fit before starting a repeat fit."
                : "Choose the fit you want to use as context, then click Start Repeat Fit.");
        }

        private void StartRepeatFitFromSelectedHistory()
        {
            FitSessionRecord source = GetSelectedHistorySession();
            if (source == null)
            {
                MessageBox.Show(this, "Select a saved fit first.", "Start Repeat Fit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            StartRepeatFitFromSession(source);
        }

        private void StartRepeatFitFromSession(FitSessionRecord source)
        {
            if (source == null || source.Id == Guid.Empty)
                return;

            if (currentSession != null && currentSession.Id == Guid.Empty)
            {
                DialogResult result = MessageBox.Show(this, "Start a repeat fit and discard the current unsaved draft?", "Start Repeat Fit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;
            }
            else if (currentSession != null)
            {
                SaveCurrentSession();
            }

            FitSessionRecord repeat = new FitSessionRecord();
            repeat.SessionDate = DateTime.Today;
            repeat.Title = "Repeat Fit - " + DateTime.Today.ToString("MMM d, yyyy");
            repeat.Status = "Assessment";
            repeat.RepeatFitSourceSessionId = source.Id;
            repeat.RepeatFitSourceTitle = source.DisplayName;
            repeat.RepeatFitSourceDate = source.SessionDate;
            repeat.FitTemplateName = source.FitTemplateName;
            repeat.FitTemplateBikeType = source.FitTemplateBikeType;
            repeat.FitProtocolBikeType = string.IsNullOrWhiteSpace(source.FitProtocolBikeType) ? source.FitTemplateBikeType : source.FitProtocolBikeType;
            repeat.Goals = source.Goals;
            repeat.Notes = BuildRepeatFitContext(source);

            sessionList.SelectedItems.Clear();
            LoadSession(repeat);
            SaveCurrentSession();
            Guid repeatId = currentSession.Id;
            RefreshSessions(repeatId);
            SelectWorkspaceTab(FitDayHomeTabName);
            UpdateSaveHint("Repeat fit created from " + source.DisplayName + ". Confirm today’s goals, then record fresh videos and measurements.");
        }

        private static string BuildRepeatFitContext(FitSessionRecord source)
        {
            System.Text.StringBuilder context = new System.Text.StringBuilder();
            context.AppendLine("REPEAT FIT CONTEXT");
            context.AppendLine("Previous fit: " + source.DisplayName + " (" + (source.SessionDate == DateTime.MinValue ? "date not recorded" : source.SessionDate.ToString("MMM d, yyyy")) + ")");
            if (!string.IsNullOrWhiteSpace(source.FitSummaryRecommendations))
                context.AppendLine("Previous recommendations: " + source.FitSummaryRecommendations.Trim());
            if (!string.IsNullOrWhiteSpace(source.FitSummaryFollowUp))
                context.AppendLine("Previous follow-up: " + source.FitSummaryFollowUp.Trim());
            context.AppendLine();
            context.Append("Fresh videos, images, and measurements are required. Previous measurement values were not copied into this session.");
            return context.ToString();
        }

        private void OpenSelectedHistorySession()
        {
            FitSessionRecord selected = GetSelectedHistorySession();
            if (selected == null)
                return;
            if (currentSession != null && currentSession.Id == Guid.Empty)
            {
                DialogResult result = MessageBox.Show(this, "The current fit has not been saved yet. Open the selected previous fit and discard this unsaved draft?", "Open Previous Fit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    return;
            }
            else if (currentSession != null)
            {
                SaveCurrentSession();
            }

            foreach (ListViewItem item in sessionList.Items)
            {
                FitSessionRecord session = item.Tag as FitSessionRecord;
                if (session != null && session.Id == selected.Id)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    SelectWorkspaceTab("Client History");
                    return;
                }
            }
            RefreshSessions(selected.Id);
            SelectWorkspaceTab("Client History");
        }

        private void OpenSelectedHistoryFolder()
        {
            FitSessionRecord selected = GetSelectedHistorySession();
            if (selected == null)
                return;
            OpenClientFolder(selected.FolderPath, "Previous fit");
        }

        private TabPage BuildClientFilesTab()
        {
            TabPage page = NewTab("Client Files");
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 3;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));

            AddClientFilesSection(table, "Client folders");
            AddClientFolderRow(table, "Client folder", client.FolderPath);
            AddClientFolderRow(table, "Videos", client.VideosPath);
            AddClientFolderRow(table, "Photos", client.PhotosPath);
            AddClientFolderRow(table, "Side-by-Side", client.SideBySidePath);
            AddClientFolderRow(table, "Reports", client.ReportsPath);
            AddClientFolderRow(table, "Measurements", client.MeasurementsPath);
            AddClientFolderRow(table, "Notes", client.NotesPath);

            AddClientFilesSection(table, "Active fit session folders");
            AddSessionFolderRow(table, "Session record", "Measurements → Sessions → active session");
            AddSessionVideosRow(table, "All session videos", "Videos → Fit Sessions → active session");
            AddSessionVideoViewRow(table, "Before videos", "Videos → Fit Sessions → active session → Before", "Before");
            AddSessionVideoViewRow(table, "After videos", "Videos → Fit Sessions → active session → After", "After");
            AddSessionVideoViewRow(table, "Dual videos", "Videos → Fit Sessions → active session → Dual", "Dual");
            AddSessionPhotosRow(table, "Report images", "Photos → Fit Sessions → active session → Report Images");
            AddSessionSideBySideRow(table, "Side-by-side", "Side-by-Side → Fit Sessions → active session");
            AddSessionAnalysisCapturesRow(table, "Analysis captures", "Client folder → Analysis Captures → active session");
            AddSessionReportsRow(table, "Reports", "Reports → Fit Sessions → active session");
            AddSessionReportPackagesRow(table, "Report packages", "Reports → Fit Sessions → active session; packages and zips save here");

            AddClientFilesSection(table, "Quick actions");
            AddImportActionRow(table, "Add videos", "Copy before/after videos into this active fit session.", "Before Video", delegate { BrowseVideo("BeforeVideoPath"); }, "After Video", delegate { BrowseVideo("AfterVideoPath"); });
            AddImportActionRow(table, "Record live", "Open Video Studio live capture into this session’s Before or After video folder.", "Before Live", delegate { OpenLiveCaptureForVideo("BeforeVideoPath"); }, "After Live", delegate { OpenLiveCaptureForVideo("AfterVideoPath"); });
            AddImportActionRow(table, "Add photos", "Copy before/after report photos into this active fit session.", "Before Photo", delegate { BrowseReportImage("BeforeReportImagePath"); }, "After Photo", delegate { BrowseReportImage("AfterReportImagePath"); });

            Label hint = new Label();
            hint.Text = "Use these shortcuts during a fit when you want to confirm where this client’s files are going. Record live opens Video Studio into this session’s Before/After video folder, and Use Latest selects the newest saved recording without browsing.";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            hint.Padding = new Padding(0, 12, 0, 0);
            int hintRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            table.Controls.Add(hint, 1, hintRow);
            table.SetColumnSpan(hint, 2);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private void AddClientFilesSection(TableLayoutPanel table, string title)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            Label section = new Label();
            section.Text = title;
            section.Dock = DockStyle.Fill;
            section.TextAlign = ContentAlignment.BottomLeft;
            section.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            section.ForeColor = Color.FromArgb(24, 31, 29);
            section.Padding = new Padding(0, 8, 0, 4);

            table.Controls.Add(section, 0, row);
            table.SetColumnSpan(section, 3);
        }

        private void AddClientFolderRow(TableLayoutPanel table, string labelText, string folderPath)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            Label path = new Label();
            path.Text = folderPath;
            path.Dock = DockStyle.Fill;
            path.TextAlign = ContentAlignment.MiddleLeft;
            path.ForeColor = Color.FromArgb(92, 104, 98);
            path.AutoEllipsis = true;

            Button open = CreateButton("Open", false);
            open.Margin = new Padding(0, 8, 0, 8);
            open.Dock = DockStyle.Fill;
            open.Click += delegate { OpenClientFolder(folderPath, labelText); };

            table.Controls.Add(FieldLabel(labelText), 0, row);
            table.Controls.Add(path, 1, row);
            table.Controls.Add(open, 2, row);
        }

        private void AddSessionFolderRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionRecordFolderPath, "Active session record folder opened.");
        }

        private void AddSessionVideosRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionVideosFolderPath, "Active session videos folder opened.");
        }

        private void AddSessionVideoViewRow(TableLayoutPanel table, string labelText, string description, string viewName)
        {
            AddDynamicFolderRow(table, labelText, description, delegate { return GetSessionVideoViewFolderPath(viewName); }, labelText + " folder opened.");
        }

        private void AddSessionPhotosRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionReportImagesFolderPath, "Active session report images folder opened.");
        }

        private void AddSessionSideBySideRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionSideBySideFolderPath, "Active session side-by-side folder opened.");
        }

        private void AddSessionAnalysisCapturesRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionAnalysisCapturesFolderPath, "Active session analysis captures folder opened.");
        }

        private void AddSessionReportsRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionReportsFolderPath, "Active session reports folder opened.");
        }

        private void AddSessionReportPackagesRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionReportsFolderPath, "Active session report packages folder opened.");
        }

        private void AddImportActionRow(TableLayoutPanel table, string labelText, string description, string firstButtonText, Action firstAction, string secondButtonText, Action secondAction)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

            Label hint = new Label();
            hint.Text = description;
            hint.Dock = DockStyle.Fill;
            hint.TextAlign = ContentAlignment.MiddleLeft;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            hint.AutoEllipsis = true;

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;
            actions.Margin = new Padding(0, 8, 0, 8);

            Button first = CreateButton(firstButtonText, false);
            first.Size = new Size(112, 34);
            first.Margin = new Padding(0, 0, 8, 0);
            first.Click += delegate { RunImportAction(labelText, firstAction); };
            actions.Controls.Add(first);

            Button second = CreateButton(secondButtonText, false);
            second.Size = new Size(112, 34);
            second.Margin = new Padding(0, 0, 0, 0);
            second.Click += delegate { RunImportAction(labelText, secondAction); };
            actions.Controls.Add(second);

            table.Controls.Add(FieldLabel(labelText), 0, row);
            table.Controls.Add(hint, 1, row);
            table.Controls.Add(actions, 2, row);
        }

        private void RunImportAction(string labelText, Action importAction)
        {
            try
            {
                importAction();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The " + labelText.ToLowerInvariant() + " action could not be completed.\n\n" + exception.Message, labelText, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddDynamicFolderRow(TableLayoutPanel table, string labelText, string description, Func<string> folderProvider, string openedMessage)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            Label path = new Label();
            path.Text = description;
            path.Dock = DockStyle.Fill;
            path.TextAlign = ContentAlignment.MiddleLeft;
            path.ForeColor = Color.FromArgb(92, 104, 98);
            path.AutoEllipsis = true;

            Button open = CreateButton("Open", false);
            open.Margin = new Padding(0, 8, 0, 8);
            open.Dock = DockStyle.Fill;
            open.Click += delegate
            {
                try
                {
                    SaveCurrentSession();
                    string folderPath = folderProvider();
                    Directory.CreateDirectory(folderPath);
                    Process.Start(folderPath);
                    UpdateSaveHint(openedMessage);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(this, "The " + labelText.ToLowerInvariant() + " folder could not be opened.\n\n" + exception.Message, labelText, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            table.Controls.Add(FieldLabel(labelText), 0, row);
            table.Controls.Add(path, 1, row);
            table.Controls.Add(open, 2, row);
        }

        private string GetSessionRecordFolderPath()
        {
            return currentSession.FolderPath;
        }

        private string GetSessionVideosFolderPath()
        {
            return Path.Combine(client.VideosPath, "Fit Sessions", currentSession.StorageFolderName);
        }

        private string GetSessionVideoViewFolderPath(string viewName)
        {
            return Path.Combine(GetSessionVideosFolderPath(), viewName);
        }

        private string GetSessionPhotosFolderPath()
        {
            return Path.Combine(client.PhotosPath, "Fit Sessions", currentSession.StorageFolderName);
        }

        private string GetSessionReportImagesFolderPath()
        {
            return Path.Combine(GetSessionPhotosFolderPath(), "Report Images");
        }

        private string GetSessionSideBySideFolderPath()
        {
            return Path.Combine(client.SideBySidePath, "Fit Sessions", currentSession.StorageFolderName);
        }

        private string GetSessionAnalysisCapturesFolderPath()
        {
            return Path.Combine(client.FolderPath, "Analysis Captures", currentSession.StorageFolderName);
        }

        private string GetSessionReportsFolderPath()
        {
            return FitSessionReportGenerator.GetSessionReportsPath(client, currentSession);
        }

        private TabPage BuildFitSummaryTab()
        {
            TabPage page = NewTab("Fit Summary");
            TableLayoutPanel table = NewEditorTable();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;

            ConfigureSummaryBox(txtFitSummaryMainGoal);
            ConfigureSummaryBox(txtFitSummaryKeyFindings);
            ConfigureSummaryBox(txtFitSummaryChangesMade);
            ConfigureSummaryBox(txtFitSummaryRecommendations);
            ConfigureSummaryBox(txtFitSummaryFollowUp);

            AddEditorRow(table, "Main goal", txtFitSummaryMainGoal, 78);
            AddEditorRow(table, "Key findings", txtFitSummaryKeyFindings, 118);
            AddEditorRow(table, "Changes made", txtFitSummaryChangesMade, 118);
            AddEditorRow(table, "Recommendations", txtFitSummaryRecommendations, 118);
            AddEditorRow(table, "Follow-up plan", txtFitSummaryFollowUp, 96);

            Label help = new Label();
            help.Text = "These fields create the polished Fit Summary section in the generated report. You can keep Notes as your raw fitter notes.";
            help.Dock = DockStyle.Fill;
            help.ForeColor = Color.FromArgb(92, 104, 98);
            help.Padding = new Padding(0, 12, 0, 0);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            table.Controls.Add(help, 1, row);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private TabPage BuildMediaTab()
        {
            TabPage page = NewTab(KinoveaVideoTabName);
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 6;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));

            Label analysisHint = new Label();
            analysisHint.Text = "Use Video Studio to record Before/After clips into the active client session, analyze the latest videos, then save images or dual evidence for the report.";
            analysisHint.Dock = DockStyle.Fill;
            analysisHint.ForeColor = Color.FromArgb(92, 104, 98);
            int analysisHintRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            table.Controls.Add(analysisHint, 1, analysisHintRow);
            table.SetColumnSpan(analysisHint, 5);

            Control cameraProfiles = BuildCameraProfilePanel();
            int cameraProfilesRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 278));
            table.Controls.Add(cameraProfiles, 1, cameraProfilesRow);
            table.SetColumnSpan(cameraProfiles, 5);

            Control fitDayPath = BuildFitDayPathGuide();
            int fitDayPathRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            table.Controls.Add(fitDayPath, 1, fitDayPathRow);
            table.SetColumnSpan(fitDayPath, 5);

            recordingFoldersGuide.Text = GetRecordingFolderGuideText();
            recordingFoldersGuide.Dock = DockStyle.Fill;
            recordingFoldersGuide.ForeColor = Color.FromArgb(74, 87, 81);
            recordingFoldersGuide.Padding = new Padding(0, 4, 0, 0);
            int recordingFoldersRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            table.Controls.Add(recordingFoldersGuide, 1, recordingFoldersRow);
            table.SetColumnSpan(recordingFoldersGuide, 5);

            Control folderShortcuts = BuildSessionFolderShortcuts();
            int folderShortcutsRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            table.Controls.Add(folderShortcuts, 1, folderShortcutsRow);
            table.SetColumnSpan(folderShortcuts, 5);

            AddMediaRow(table, "Before", "BeforeVideoPath");
            AddMediaRow(table, "After", "AfterVideoPath");

            AddSavedEvidenceReview(table);

            FlowLayoutPanel comparisons = new FlowLayoutPanel();
            comparisons.Dock = DockStyle.Fill;
            comparisons.FlowDirection = FlowDirection.LeftToRight;
            comparisons.WrapContents = true;
            comparisons.Padding = new Padding(0, 18, 0, 0);

            Button dualLive = CreateButton("Dual Live Capture", true);
            dualLive.Size = new Size(190, 38);
            dualLive.Click += delegate { OpenDualLiveCapture(); };
            comparisons.Controls.Add(dualLive);

            Button latestBoth = CreateButton("Analyze Latest Before + After", true);
            latestBoth.Size = new Size(270, 38);
            latestBoth.Click += delegate { UseLatestBothVideos(); };
            comparisons.Controls.Add(latestBoth);

            int comparisonRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            table.Controls.Add(comparisons, 1, comparisonRow);
            table.SetColumnSpan(comparisons, 5);

            Label toolsTitle = new Label();
            toolsTitle.Text = "Cassette Motion Pro analysis + saved evidence";
            toolsTitle.Dock = DockStyle.Fill;
            toolsTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            toolsTitle.ForeColor = Color.FromArgb(24, 31, 29);
            int toolsTitleRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            table.Controls.Add(toolsTitle, 1, toolsTitleRow);
            table.SetColumnSpan(toolsTitle, 4);

            FlowLayoutPanel analysisActions = new FlowLayoutPanel();
            analysisActions.Dock = DockStyle.Fill;
            analysisActions.FlowDirection = FlowDirection.LeftToRight;
            analysisActions.WrapContents = true;
            analysisActions.Padding = new Padding(0, 6, 0, 0);

            Button prepare = CreateButton("Prepare Capture Folder", true);
            prepare.Size = new Size(205, 38);
            prepare.Click += delegate { PrepareAnalysisCaptureFolder(); };

            Button captures = CreateButton("Open Captures Folder", false);
            captures.Size = new Size(180, 38);
            captures.Click += delegate { OpenAnalysisCapturesFolder(); };

            Button checkCaptures = CreateButton("Check Saved Evidence", true);
            checkCaptures.Size = new Size(190, 38);
            checkCaptures.Click += delegate { CheckSavedAnalysisEvidence(); };

            analysisActions.Controls.Add(prepare);
            analysisActions.Controls.Add(captures);
            analysisActions.Controls.Add(checkCaptures);

            int analysisActionsRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            table.Controls.Add(analysisActions, 1, analysisActionsRow);
            table.SetColumnSpan(analysisActions, 4);

            analysisCapturesStatus.Text = "Evidence status: Record Live saves raw fit clips into this active session’s Before/After video folders. Prepare Capture Folder sets this session’s Analysis Captures folder for screenshots, exports, and extra evidence.";
            analysisCapturesStatus.Dock = DockStyle.Fill;
            analysisCapturesStatus.ForeColor = Color.FromArgb(92, 104, 98);
            int statusRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
            table.Controls.Add(analysisCapturesStatus, 1, statusRow);
            table.SetColumnSpan(analysisCapturesStatus, 4);

            Panel saveGuide = BuildAnalysisSaveGuide();
            int saveGuideRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));
            table.Controls.Add(saveGuide, 1, saveGuideRow);
            table.SetColumnSpan(saveGuide, 4);

            Label hint = new Label();
            hint.Text = "For a real fit, record as many live clips as needed. Analyze Latest Before + After pulls in the newest takes from this active client session, opens Video Studio playback, and the saved evidence plus Measurements become the report foundation.";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            int hintRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            table.Controls.Add(hint, 1, hintRow);
            table.SetColumnSpan(hint, 4);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private Control BuildCameraProfilePanel()
        {
            GroupBox group = new GroupBox();
            group.Text = "Reusable dual-camera setup";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = Color.FromArgb(37, 48, 43);
            group.Padding = new Padding(12, 10, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 4;
            layout.RowCount = 6;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int index = 0; index < 5; index++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            cmbCameraProfile.Dock = DockStyle.Fill;
            cmbCameraProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCameraProfile.SelectedIndexChanged += delegate { LoadSelectedCameraProfile(); };
            layout.Controls.Add(CreateFieldLabel("Profile"), 0, 0);
            layout.Controls.Add(cmbCameraProfile, 1, 0);
            layout.SetColumnSpan(cmbCameraProfile, 3);

            AddCameraProfileField(layout, "Left screen", txtCameraLeftRole, 0, 1);
            AddCameraProfileField(layout, "Right screen", txtCameraRightRole, 2, 1);
            AddCameraProfileField(layout, "Left camera", txtCameraLeftDevice, 0, 2);
            AddCameraProfileField(layout, "Right camera", txtCameraRightDevice, 2, 2);
            AddCameraProfileField(layout, "Resolution", txtCameraResolution, 0, 3);
            AddCameraProfileField(layout, "Frame rate", txtCameraFrameRate, 2, 3);
            AddCameraProfileField(layout, "Setup notes", txtCameraNotes, 0, 4);
            layout.SetColumnSpan(txtCameraNotes, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.WrapContents = false;
            Button openBefore = CreateButton("Record Before", true);
            openBefore.Size = new Size(130, 36);
            openBefore.Click += delegate { ApplyCameraProfileAndOpenCapture("Before"); };
            Button openAfter = CreateButton("Record After", true);
            openAfter.Size = new Size(125, 36);
            openAfter.Click += delegate { ApplyCameraProfileAndOpenCapture("After"); };
            Button save = CreateButton("Save as Custom", false);
            save.Size = new Size(145, 36);
            save.Click += delegate { SaveCameraProfile(); };
            Button delete = CreateButton("Delete Custom", false);
            delete.Size = new Size(135, 36);
            delete.Click += delegate { DeleteCameraProfile(); };
            cameraProfileStatus.AutoSize = false;
            cameraProfileStatus.Size = new Size(430, 38);
            cameraProfileStatus.Padding = new Padding(8, 8, 0, 0);
            cameraProfileStatus.ForeColor = Color.FromArgb(74, 87, 81);
            actions.Controls.Add(openBefore);
            actions.Controls.Add(openAfter);
            actions.Controls.Add(save);
            actions.Controls.Add(delete);
            actions.Controls.Add(cameraProfileStatus);
            layout.Controls.Add(actions, 0, 5);
            layout.SetColumnSpan(actions, 4);
            group.Controls.Add(layout);
            RefreshCameraProfiles(null);
            return group;
        }

        private static Label CreateFieldLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            return label;
        }

        private static void AddCameraProfileField(TableLayoutPanel layout, string label, TextBox box, int column, int row)
        {
            box.Dock = DockStyle.Fill;
            layout.Controls.Add(CreateFieldLabel(label), column, row);
            layout.Controls.Add(box, column + 1, row);
        }

        private void RefreshCameraProfiles(string selectName)
        {
            string name = selectName;
            if (string.IsNullOrWhiteSpace(name) && currentSession != null)
                name = currentSession.CameraSetupProfileName;
            cmbCameraProfile.BeginUpdate();
            cmbCameraProfile.Items.Clear();
            foreach (CameraSetupProfile profile in cameraProfileRepository.LoadAll())
                cmbCameraProfile.Items.Add(profile);
            cmbCameraProfile.EndUpdate();
            int selectedIndex = 0;
            for (int index = 0; index < cmbCameraProfile.Items.Count; index++)
            {
                CameraSetupProfile profile = cmbCameraProfile.Items[index] as CameraSetupProfile;
                if (profile != null && string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase))
                    selectedIndex = index;
            }
            cmbCameraProfile.SelectedIndex = cmbCameraProfile.Items.Count == 0 ? -1 : selectedIndex;
        }

        private void LoadSelectedCameraProfile()
        {
            CameraSetupProfile profile = cmbCameraProfile.SelectedItem as CameraSetupProfile;
            if (profile == null)
                return;
            txtCameraLeftRole.Text = profile.LeftRole ?? string.Empty;
            txtCameraRightRole.Text = profile.RightRole ?? string.Empty;
            txtCameraLeftDevice.Text = profile.LeftCamera ?? string.Empty;
            txtCameraRightDevice.Text = profile.RightCamera ?? string.Empty;
            txtCameraResolution.Text = profile.Resolution ?? string.Empty;
            txtCameraFrameRate.Text = profile.FrameRate ?? string.Empty;
            txtCameraNotes.Text = profile.Notes ?? string.Empty;
            cameraProfileStatus.Text = (profile.IsBuiltIn ? "Built-in" : "Custom") + " · Camera choices remain under Kinovea’s capture controls.";
        }

        private void ApplyCameraProfileAndOpenCapture(string phase)
        {
            if (!HasActiveFitSession())
            {
                MessageBox.Show(this, "Open or create a fit session first, then apply a camera profile.", "Camera Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CameraSetupProfile profile = cmbCameraProfile.SelectedItem as CameraSetupProfile;
            if (profile == null)
                return;
            currentSession.CameraSetupProfileName = profile.Name;
            currentSession.CameraSetupLeftRole = txtCameraLeftRole.Text.Trim();
            currentSession.CameraSetupRightRole = txtCameraRightRole.Text.Trim();
            SaveCurrentSession();
            OpenProfileDualLiveCapture(phase);
        }

        private void OpenProfileDualLiveCapture(string phase)
        {
            if (openProfileDualLiveCaptureFolders == null)
            {
                OpenDualLiveCapture();
                return;
            }
            try
            {
                string destination = GetSessionVideoViewFolderPath(phase);
                Directory.CreateDirectory(destination);
                WriteCaptureFolderHint(destination, phase);
                WriteCameraProfileHint(destination);
                string leftRole = string.IsNullOrWhiteSpace(currentSession.CameraSetupLeftRole) ? "Camera 1" : currentSession.CameraSetupLeftRole;
                string rightRole = string.IsNullOrWhiteSpace(currentSession.CameraSetupRightRole) ? "Camera 2" : currentSession.CameraSetupRightRole;
                SetFitCommandCenterMode("Record Live: " + phase + " · " + currentSession.CameraSetupProfileName);
                UpdateSaveHint("Dual-camera " + phase + " capture opened. Both screens save into this client's " + phase + " folder with separate camera-role filenames.");
                Close();
                openProfileDualLiveCaptureFolders(destination, destination, phase + "-" + leftRole, phase + "-" + rightRole);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The camera profile could not be opened.\n\n" + exception.Message, "Camera Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCameraProfile()
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Name this reusable dual-camera setup.", "Save Camera Profile", "My Camera Setup").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;
            CameraSetupProfile profile = new CameraSetupProfile();
            profile.Name = name;
            profile.LeftRole = txtCameraLeftRole.Text.Trim();
            profile.RightRole = txtCameraRightRole.Text.Trim();
            profile.LeftCamera = txtCameraLeftDevice.Text.Trim();
            profile.RightCamera = txtCameraRightDevice.Text.Trim();
            profile.Resolution = txtCameraResolution.Text.Trim();
            profile.FrameRate = txtCameraFrameRate.Text.Trim();
            profile.Notes = txtCameraNotes.Text.Trim();
            cameraProfileRepository.Save(profile);
            RefreshCameraProfiles(name);
            UpdateSaveHint("Camera setup saved for use with any client: " + name + ".");
        }

        private void DeleteCameraProfile()
        {
            CameraSetupProfile profile = cmbCameraProfile.SelectedItem as CameraSetupProfile;
            if (profile == null)
                return;
            if (profile.IsBuiltIn)
            {
                MessageBox.Show(this, "Built-in camera profiles cannot be deleted. Save a customized copy instead.", "Camera Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(this, "Delete the custom camera profile \"" + profile.Name + "\"?", "Camera Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            cameraProfileRepository.Delete(profile);
            RefreshCameraProfiles(null);
            UpdateSaveHint("Custom camera profile deleted.");
        }

        private Control BuildFitDayPathGuide()
        {
            TableLayoutPanel path = new TableLayoutPanel();
            path.Dock = DockStyle.Fill;
            path.ColumnCount = 5;
            path.RowCount = 1;
            path.BackColor = Color.FromArgb(248, 252, 238);
            path.Padding = new Padding(8, 8, 8, 8);

            for (int index = 0; index < 5; index++)
                path.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            AddFitDayPathStep(path, 0, "1. Client", "Confirm info + goals");
            AddFitDayPathStep(path, 1, "2. Record", "Before / After folders");
            AddFitDayPathStep(path, 2, "3. Analyze", "Video Studio playback tools");
            AddFitDayPathStep(path, 3, "4. Save", "Evidence + Bike Metrics");
            AddFitDayPathStep(path, 4, "5. Report", "Preview + package");

            return path;
        }

        private Control BuildSessionFolderShortcuts()
        {
            FlowLayoutPanel shortcuts = new FlowLayoutPanel();
            shortcuts.Dock = DockStyle.Fill;
            shortcuts.FlowDirection = FlowDirection.LeftToRight;
            shortcuts.WrapContents = true;
            shortcuts.Padding = new Padding(0, 6, 0, 0);

            Button before = CreateButton("Before Folder", false);
            before.Size = new Size(170, 36);
            before.Click += delegate { OpenSessionVideoFolder("Before"); };

            Button after = CreateButton("After Folder", false);
            after.Size = new Size(160, 36);
            after.Click += delegate { OpenSessionVideoFolder("After"); };

            shortcuts.Controls.Add(before);
            shortcuts.Controls.Add(after);
            return shortcuts;
        }

        private void AddSavedEvidenceReview(TableLayoutPanel table)
        {
            GroupBox review = new GroupBox();
            review.Text = "Saved Evidence Review";
            review.Dock = DockStyle.Fill;
            review.Padding = new Padding(12, 16, 12, 10);
            review.ForeColor = Color.FromArgb(37, 48, 43);

            TableLayoutPanel reviewLayout = new TableLayoutPanel();
            reviewLayout.Dock = DockStyle.Fill;
            reviewLayout.RowCount = 2;
            reviewLayout.ColumnCount = 1;
            reviewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            reviewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

            savedEvidenceReviewStatus.Dock = DockStyle.Fill;
            savedEvidenceReviewStatus.ForeColor = Color.FromArgb(74, 87, 81);
            savedEvidenceReviewStatus.Padding = new Padding(4, 2, 4, 0);
            savedEvidenceReviewStatus.Text = "Open or create a client fit session first from Client Files. Then Video Studio Save Image and Save Video can offer Before, After, and Dual session folders.";

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = true;
            actions.Padding = new Padding(0, 4, 0, 0);

            Button refresh = CreateButton("Refresh Review", true);
            refresh.Size = new Size(150, 32);
            refresh.Click += delegate { RefreshSavedEvidenceReview(); };

            Button before = CreateButton("Open Before", false);
            before.Size = new Size(130, 32);
            before.Click += delegate { OpenSessionVideoFolder("Before"); };

            Button after = CreateButton("Open After", false);
            after.Size = new Size(120, 32);
            after.Click += delegate { OpenSessionVideoFolder("After"); };

            Button dual = CreateButton("Open Dual", false);
            dual.Size = new Size(120, 32);
            dual.Click += delegate { OpenSessionVideoFolder("Dual"); };

            Button reportImages = CreateButton("Open Report Images", false);
            reportImages.Size = new Size(170, 32);
            reportImages.Click += delegate { OpenReportImagesFolder(); };

            Button latestBefore = CreateButton("Latest Before", false);
            latestBefore.Size = new Size(132, 32);
            latestBefore.Click += delegate { OpenLatestSavedEvidence("Before video", GetSessionVideoViewFolderPath("Before"), true); };

            Button latestAfter = CreateButton("Latest After", false);
            latestAfter.Size = new Size(124, 32);
            latestAfter.Click += delegate { OpenLatestSavedEvidence("After video", GetSessionVideoViewFolderPath("After"), true); };

            Button latestDual = CreateButton("Latest Dual", false);
            latestDual.Size = new Size(120, 32);
            latestDual.Click += delegate { OpenLatestSavedEvidence("Dual video", GetSessionVideoViewFolderPath("Dual"), true); };

            Button latestImage = CreateButton("Latest Image", false);
            latestImage.Size = new Size(128, 32);
            latestImage.Click += delegate { OpenLatestSavedEvidence("report image", GetSessionReportImagesFolderPath(), false); };

            actions.Controls.Add(refresh);
            actions.Controls.Add(before);
            actions.Controls.Add(after);
            actions.Controls.Add(dual);
            actions.Controls.Add(reportImages);
            actions.Controls.Add(latestBefore);
            actions.Controls.Add(latestAfter);
            actions.Controls.Add(latestDual);
            actions.Controls.Add(latestImage);

            reviewLayout.Controls.Add(savedEvidenceReviewStatus, 0, 0);
            reviewLayout.Controls.Add(actions, 0, 1);
            review.Controls.Add(reviewLayout);

            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 316));
            table.Controls.Add(review, 1, row);
            table.SetColumnSpan(review, 5);

            RefreshSavedEvidenceReview();
        }

        private void AddFitDayPathStep(TableLayoutPanel path, int column, string title, string detail)
        {
            Label step = new Label();
            step.Dock = DockStyle.Fill;
            step.TextAlign = ContentAlignment.MiddleCenter;
            step.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            step.ForeColor = Color.FromArgb(37, 48, 43);
            step.Text = title + Environment.NewLine + detail;
            step.Margin = new Padding(4, 0, 4, 0);
            path.Controls.Add(step, column, 0);
        }

        private static void ConfigureSummaryBox(TextBox box)
        {
            box.Multiline = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.BorderStyle = BorderStyle.FixedSingle;
        }

        private TabPage BuildBikeMetricsTab()
        {
            TabPage page = NewTab("Bike Metrics");
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 5;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));

            AddMeasurementReferenceImageControls(table);
            AddBikeMetricsWorkflowGuide(table);

            FlowLayoutPanel reviewActions = new FlowLayoutPanel();
            reviewActions.Dock = DockStyle.Fill;
            reviewActions.FlowDirection = FlowDirection.LeftToRight;
            reviewActions.Padding = new Padding(0, 0, 0, 8);

            Button reviewMetrics = CreateButton("Review Measurement Quality", true);
            reviewMetrics.Size = new Size(210, 34);
            reviewMetrics.Click += ReviewMetrics_Click;
            reviewActions.Controls.Add(reviewMetrics);

            Label reviewHint = new Label();
            reviewHint.Text = "Checks missing key bike metrics and flags values that may be worth reviewing before generating reports.";
            reviewHint.AutoSize = true;
            reviewHint.Margin = new Padding(12, 8, 0, 0);
            reviewHint.ForeColor = Color.FromArgb(92, 104, 98);
            reviewActions.Controls.Add(reviewHint);

            int reviewRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            table.Controls.Add(reviewActions, 0, reviewRow);
            table.SetColumnSpan(reviewActions, 5);

            AddBikeMetricHeader(table);
            AddBikeMetricRow(table, "Saddle height", "Use Distance: BB center → saddle top along the seat tube / saddle-height line.", "SaddleHeight");
            AddBikeMetricRow(table, "Saddle setback", "BB vertical line → saddle nose, measured horizontally.", "SaddleSetback");
            AddBikeMetricRow(table, "Saddle tip to grip reach", "Saddle tip → grip/hood contact point.", "SaddleTipToGripReach");
            AddBikeMetricRow(table, "Handlebar X", "BB center → handlebar/hood contact point, horizontal coordinate.", "HandlebarX");
            AddBikeMetricRow(table, "Handlebar Y", "BB center → handlebar/hood contact point, vertical coordinate.", "HandlebarY");
            AddBikeMetricRow(table, "Handlebar reach", "Reference point → handlebar/hood contact point, horizontal reach.", "HandlebarReach");
            AddBikeMetricRow(table, "Handlebar drop", "Saddle top → handlebar/hood contact point, vertical drop.", "HandlebarDrop");
            AddBikeMetricRow(table, "Crank length", "Crank center → pedal spindle.", "CrankLength");
            AddBikeMetricRow(table, "Wheelbase", "Rear axle center → front axle center, measured horizontally.", "Wheelbase");
            AddBikeMetricRow(table, "Cleat position", "Shoe/cleat reference point → cleat center.", "CleatPosition");

            Label hint = new Label();
            hint.Text = "Enter the unit with the value (for example, 742 mm). Use Assist when you want guided image measurement, or enter values manually after measuring in Video Studio.";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            table.Controls.Add(hint, 0, row);
            table.SetColumnSpan(hint, 5);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private void AddBikeMetricsWorkflowGuide(TableLayoutPanel table)
        {
            GroupBox guide = new GroupBox();
            guide.Text = "Bike Metrics workflow";
            guide.Dock = DockStyle.Fill;
            guide.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            guide.ForeColor = Color.FromArgb(37, 48, 43);
            guide.Padding = new Padding(12, 8, 12, 12);

            TableLayoutPanel guideTable = new TableLayoutPanel();
            guideTable.Dock = DockStyle.Fill;
            guideTable.ColumnCount = 2;
            guideTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            guideTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddBikeMetricsWorkflowGuideRow(guideTable, "1. Open video tools", "Use Video Studio to open Before, After, or Before + After in the full analysis workspace.");
            AddBikeMetricsWorkflowGuideRow(guideTable, "2. Save evidence", "Save screenshots, exports, or short clips into the active session Analysis Captures folder.");
            AddBikeMetricsWorkflowGuideRow(guideTable, "3. Record numbers", "Enter the final Before/After values here, or use Assist with the measurement reference image.");
            AddBikeMetricsWorkflowGuideRow(guideTable, "4. Review report", "Use Review Metrics, then Preview/Generate so the report reflects the saved client session.");

            guide.Controls.Add(guideTable);

            int guideRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 156));
            table.Controls.Add(guide, 0, guideRow);
            table.SetColumnSpan(guide, 5);
        }

        private void AddBikeMetricsWorkflowGuideRow(TableLayoutPanel table, string labelText, string instructions)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            Label label = FieldLabel(labelText);
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(60, 145, 76);

            Label instructionLabel = FieldLabel(instructions);
            instructionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            instructionLabel.ForeColor = Color.FromArgb(74, 87, 81);

            table.Controls.Add(label, 0, row);
            table.Controls.Add(instructionLabel, 1, row);
        }

        private TabPage BuildReportImagesTab()
        {
            TabPage page = NewTab("Report Images");
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 4;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

            AddReportImageQuickSaveRow(table);
            AddImageRow(table, "Before image", "BeforeReportImagePath");
            AddImageRow(table, "After image", "AfterReportImagePath");
            AddImageRow(table, "Side-by-side image", "SideBySideReportImagePath");
            AddReportImageDisplayOptions(table);

            FlowLayoutPanel combineActions = new FlowLayoutPanel();
            combineActions.Dock = DockStyle.Fill;
            combineActions.FlowDirection = FlowDirection.LeftToRight;
            combineActions.Padding = new Padding(0, 8, 0, 4);

            Button combine = CreateButton("Combine Before + After", true);
            combine.Size = new Size(190, 34);
            combine.Click += delegate { CombineBeforeAfterImages(true); };
            combineActions.Controls.Add(combine);

            int actionRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            table.Controls.Add(combineActions, 1, actionRow);
            table.SetColumnSpan(combineActions, 3);

            Label hint = new Label();
            hint.Text = "You can use one side-by-side image by itself. Choose Side-by-side image and it will also become the Bike Metrics measurement image. Only use Before/After images if you want the app to combine them for you.";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            table.Controls.Add(hint, 1, row);
            table.SetColumnSpan(hint, 3);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private void AddReportImageQuickSaveRow(TableLayoutPanel table)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));

            Label label = new Label();
            label.Text = "Quick save";
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(74, 87, 81);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = true;
            actions.Padding = new Padding(0, 4, 0, 0);

            Button setSaveFolder = CreateButton("Set Save Folder", true);
            setSaveFolder.Size = new Size(132, 32);
            setSaveFolder.Margin = new Padding(0, 0, 8, 6);
            setSaveFolder.Click += delegate { SetReportImagesSaveFolder(); };

            Button openFolder = CreateButton("Open Folder", false);
            openFolder.Size = new Size(104, 32);
            openFolder.Margin = new Padding(0, 0, 8, 6);
            openFolder.Click += delegate { OpenReportImagesFolderForSaving(); };

            Button copyPath = CreateButton("Copy Folder Path", false);
            copyPath.Size = new Size(136, 32);
            copyPath.Margin = new Padding(0, 0, 8, 6);
            copyPath.Click += delegate { CopyReportImagesFolderPath(); };

            Button latestBefore = CreateButton("Use Latest Before", false);
            latestBefore.Size = new Size(136, 32);
            latestBefore.Margin = new Padding(0, 0, 8, 6);
            latestBefore.Click += delegate { UseLatestReportImage("BeforeReportImagePath"); };

            Button latestAfter = CreateButton("Use Latest After", false);
            latestAfter.Size = new Size(128, 32);
            latestAfter.Margin = new Padding(0, 0, 8, 6);
            latestAfter.Click += delegate { UseLatestReportImage("AfterReportImagePath"); };

            Button latestSideBySide = CreateButton("Use Latest Side-by-side", false);
            latestSideBySide.Size = new Size(170, 32);
            latestSideBySide.Margin = new Padding(0, 0, 8, 6);
            latestSideBySide.Click += delegate { UseLatestReportImage("SideBySideReportImagePath"); };

            Label hint = new Label();
            hint.AutoSize = true;
            hint.MaximumSize = new Size(820, 0);
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            hint.Text = "Click Set Save Folder before saving images from Video Studio. If Windows still opens somewhere else, Copy Folder Path and paste it into the save dialog.";
            hint.Margin = new Padding(0, 2, 0, 0);

            actions.Controls.Add(setSaveFolder);
            actions.Controls.Add(openFolder);
            actions.Controls.Add(copyPath);
            actions.Controls.Add(latestBefore);
            actions.Controls.Add(latestAfter);
            actions.Controls.Add(latestSideBySide);
            actions.Controls.Add(hint);

            table.Controls.Add(label, 0, row);
            table.Controls.Add(actions, 1, row);
            table.SetColumnSpan(actions, 3);
        }

        private void AddReportImageDisplayOptions(TableLayoutPanel table)
        {
            Label label = new Label();
            label.Text = "Show in report";
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(74, 87, 81);

            FlowLayoutPanel options = new FlowLayoutPanel();
            options.Dock = DockStyle.Fill;
            options.FlowDirection = FlowDirection.LeftToRight;
            options.WrapContents = true;
            options.Padding = new Padding(0, 4, 0, 4);

            ConfigureReportImageCheckbox(chkShowSideBySideImageInReport, "Side-by-side");
            ConfigureReportImageCheckbox(chkShowBeforeImageInReport, "Before");
            ConfigureReportImageCheckbox(chkShowAfterImageInReport, "After");
            ConfigureReportImageCheckbox(chkShowMeasurementReferenceImageInReport, "Measurement reference");
            ConfigureReportImageCheckbox(chkShowMeasurementCaptureTraceInReport, "Measurement trace");

            options.Controls.Add(chkShowSideBySideImageInReport);
            options.Controls.Add(chkShowBeforeImageInReport);
            options.Controls.Add(chkShowAfterImageInReport);
            options.Controls.Add(chkShowMeasurementReferenceImageInReport);
            options.Controls.Add(chkShowMeasurementCaptureTraceInReport);

            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(options, 1, row);
            table.SetColumnSpan(options, 3);

            AddReportLogoStyleRow(table);
        }

        private void AddReportLogoStyleRow(TableLayoutPanel table)
        {
            Label label = new Label();
            label.Text = "Report logo";
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(74, 87, 81);

            cmbReportLogoStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbReportLogoStyle.Items.Add("Full Cassette logo");
            cmbReportLogoStyle.Items.Add("CM badge");
            cmbReportLogoStyle.Items.Add("No logo");
            cmbReportLogoStyle.SelectedIndex = 0;
            cmbReportLogoStyle.Dock = DockStyle.Left;
            cmbReportLogoStyle.Width = 220;
            cmbReportLogoStyle.SelectedIndexChanged += delegate
            {
                if (currentSession != null)
                    UpdateSaveHint("Report logo option updated.");
            };

            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            table.Controls.Add(label, 0, row);
            table.Controls.Add(cmbReportLogoStyle, 1, row);
            table.SetColumnSpan(cmbReportLogoStyle, 3);
        }

        private void ConfigureReportImageCheckbox(CheckBox checkbox, string text)
        {
            checkbox.Text = text;
            checkbox.Checked = true;
            checkbox.AutoSize = true;
            checkbox.Margin = new Padding(0, 8, 18, 4);
            checkbox.ForeColor = Color.FromArgb(24, 31, 29);
            checkbox.CheckedChanged += delegate
            {
                if (currentSession != null)
                    UpdateSaveHint("Report display options updated.");
            };
        }

        private TabPage BuildNotesTab()
        {
            TabPage page = NewTab("Notes");
            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(24, 22, 24, 22);

            Label label = new Label();
            label.Text = "Recommendations, observations, and follow-up items";
            label.Dock = DockStyle.Top;
            label.Height = 30;
            label.ForeColor = Color.FromArgb(74, 87, 81);

            txtNotes.Multiline = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;
            txtNotes.Dock = DockStyle.Fill;
            txtNotes.BorderStyle = BorderStyle.FixedSingle;

            content.Controls.Add(txtNotes);
            content.Controls.Add(label);
            page.Controls.Add(content);
            return page;
        }

        private TabPage BuildHandoffTab()
        {
            TabPage page = NewTab("Handoff");
            TableLayoutPanel table = NewEditorTable();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;

            ConfigureSummaryBox(txtHandoffWhatToSend);
            ConfigureSummaryBox(txtHandoffClientMessage);
            ConfigureSummaryBox(txtHandoffHomework);
            ConfigureSummaryBox(txtHandoffNextAppointment);
            ConfigureSummaryBox(txtHandoffInternalNotes);

            AddEditorRow(table, "What to send", txtHandoffWhatToSend, 88);
            AddEditorRow(table, "Client message", txtHandoffClientMessage, 138);
            AddEditorRow(table, "Homework / rides", txtHandoffHomework, 112);
            AddEditorRow(table, "Next appointment", txtHandoffNextAppointment, 74);
            AddEditorRow(table, "Internal notes", txtHandoffInternalNotes, 112);

            Label help = new Label();
            help.Text = "Handoff notes are saved with the session and included as a separate Client Handoff Notes.txt file in report packages and zipped packages.";
            help.Dock = DockStyle.Fill;
            help.ForeColor = Color.FromArgb(92, 104, 98);
            help.Padding = new Padding(0, 12, 0, 0);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            table.Controls.Add(help, 1, row);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private TabPage BuildVideoAnalysisTab()
        {
            TabPage page = NewTab(KinoveaVideoTabName);
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 24, 24, 18);
            table.ColumnCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label title = new Label();
            title.Text = "Measure in Cassette Motion Pro Video Studio";
            title.Dock = DockStyle.Fill;
            title.Font = new Font(Font.FontFamily, 16, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(26, 30, 34);
            int titleRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.Controls.Add(title, 0, titleRow);

            Label explanation = new Label();
            explanation.Text = "Use these buttons to open the selected fit video in Cassette Motion Pro’s main video player. This is where you should do the actual measuring with the hand tool, drawing tools, distance and angle tools, timeline, playback buttons, side-by-side view, and joint controls.";
            explanation.Dock = DockStyle.Fill;
            explanation.ForeColor = Color.FromArgb(92, 104, 98);
            int explanationRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            table.Controls.Add(explanation, 0, explanationRow);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = true;
            actions.Padding = new Padding(0, 12, 0, 0);

            Button before = CreateButton("Analyze Before Video", false);
            before.Size = new Size(170, 38);
            before.Click += delegate { OpenSingle("BeforeVideoPath"); };

            Button after = CreateButton("Analyze After Video", true);
            after.Size = new Size(170, 38);
            after.Click += delegate { OpenSingle("AfterVideoPath"); };

            Button pair = CreateButton("Analyze Latest Before + After", false);
            pair.Size = new Size(270, 38);
            pair.Click += delegate { UseLatestBothVideos(); };

            Button prepare = CreateButton("Prepare Capture Folder", true);
            prepare.Size = new Size(205, 38);
            prepare.Click += delegate { PrepareAnalysisCaptureFolder(); };

            Button captures = CreateButton("Open Captures Folder", false);
            captures.Size = new Size(180, 38);
            captures.Click += delegate { OpenAnalysisCapturesFolder(); };

            Button checkCaptures = CreateButton("Check Saved Evidence", true);
            checkCaptures.Size = new Size(190, 38);
            checkCaptures.Click += delegate { CheckSavedAnalysisEvidence(); };

            actions.Controls.Add(before);
            actions.Controls.Add(after);
            actions.Controls.Add(pair);
            actions.Controls.Add(prepare);
            actions.Controls.Add(captures);
            actions.Controls.Add(checkCaptures);

            int actionRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            table.Controls.Add(actions, 0, actionRow);

            analysisCapturesStatus.Text = "Evidence status: click Prepare Capture Folder or Analyze to set this session as the active Video Studio capture destination.";
            analysisCapturesStatus.Dock = DockStyle.Fill;
            analysisCapturesStatus.ForeColor = Color.FromArgb(92, 104, 98);
            int statusRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            table.Controls.Add(analysisCapturesStatus, 0, statusRow);

            Label reminder = new Label();
            reminder.Text = "Recommended order: prepare the capture folder, open Before/After analysis, measure in Video Studio first, save useful photos or video evidence into Analysis Captures, then return here to enter Bike Metrics and choose report images.";
            reminder.Dock = DockStyle.Fill;
            reminder.ForeColor = Color.FromArgb(92, 104, 98);
            int reminderRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            table.Controls.Add(reminder, 0, reminderRow);

            Label folderHint = new Label();
            folderHint.Text = "Active capture destination: Client folder → Analysis Captures → active session. Use Open Captures Folder if you want to confirm it before or after measuring.";
            folderHint.Dock = DockStyle.Fill;
            folderHint.ForeColor = Color.FromArgb(92, 104, 98);
            int folderHintRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            table.Controls.Add(folderHint, 0, folderHintRow);

            Panel saveGuide = BuildAnalysisSaveGuide();
            int saveGuideRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));
            table.Controls.Add(saveGuide, 0, saveGuideRow);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private Panel BuildAnalysisSaveGuide()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.White;
            panel.Padding = new Padding(16, 12, 16, 12);

            TableLayoutPanel guide = new TableLayoutPanel();
            guide.Dock = DockStyle.Fill;
            guide.ColumnCount = 3;
            guide.RowCount = 0;
            guide.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            guide.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            guide.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));

            Label title = new Label();
            title.Text = "After measuring: what goes where";
            title.Dock = DockStyle.Fill;
            title.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            int headerRow = guide.RowCount++;
            guide.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            guide.Controls.Add(title, 0, headerRow);
            guide.SetColumnSpan(title, 3);

            AddSaveGuideRow(guide, "Evidence", "Screenshots, exported frames, clips, and reference media saved from Video Studio.", "Analysis Captures");
            AddSaveGuideRow(guide, "Final numbers", "Saddle height, setback, reach, handlebar X/Y, and body-angle values after you measure.", "Bike Metrics");
            AddSaveGuideRow(guide, "Report visuals", "The images you actually want shown in the client report.", "Report Images");
            AddSaveGuideRow(guide, "Client files", "Reports, packages, zips, notes, videos, and photos organized by this fit session.", "Client Files");

            panel.Controls.Add(guide);
            return panel;
        }

        private void AddSaveGuideRow(TableLayoutPanel table, string item, string description, string destination)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            Label itemLabel = FieldLabel(item);
            itemLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            itemLabel.TextAlign = ContentAlignment.MiddleLeft;

            Label descriptionLabel = FieldLabel(description);
            descriptionLabel.ForeColor = Color.FromArgb(92, 104, 98);
            descriptionLabel.TextAlign = ContentAlignment.MiddleLeft;

            Label destinationLabel = FieldLabel(destination);
            destinationLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            destinationLabel.ForeColor = Color.FromArgb(60, 145, 76);
            destinationLabel.TextAlign = ContentAlignment.MiddleLeft;

            table.Controls.Add(itemLabel, 0, row);
            table.Controls.Add(descriptionLabel, 1, row);
            table.Controls.Add(destinationLabel, 2, row);
        }

        private TabPage BuildBodyAnglesTab()
        {
            TabPage page = NewTab("Body Angles");
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 3;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            AddMeasurementHeader(table);
            AddMeasurementRow(table, "Knee angle", "KneeAngle");
            AddMeasurementRow(table, "Hip angle", "HipAngle");
            AddMeasurementRow(table, "Ankle angle", "AnkleAngle");
            AddMeasurementRow(table, "Body reach", "TorsoAngle");
            AddMeasurementRow(table, "Back angle", "ShoulderAngle");

            Label guidance = new Label();
            guidance.Text = "Recommended process: pause the video at the same crank position, use the Video Studio angle tools, then enter the Before and After values you want in the report.";
            guidance.Dock = DockStyle.Fill;
            guidance.ForeColor = Color.FromArgb(92, 104, 98);
            guidance.Padding = new Padding(0, 12, 0, 4);
            int guidanceRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            table.Controls.Add(guidance, 0, guidanceRow);
            table.SetColumnSpan(guidance, 3);

            AddBodyAngleGuide(table);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = true;

            Button guidedBefore = CreateButton("Guided Before Image", true);
            guidedBefore.Size = new Size(180, 38);
            guidedBefore.Click += delegate { ShowGuidedRiderMeasurements("BeforeReportImagePath", "Before"); };
            Button guidedAfter = CreateButton("Guided After Image", true);
            guidedAfter.Size = new Size(175, 38);
            guidedAfter.Click += delegate { ShowGuidedRiderMeasurements("AfterReportImagePath", "After"); };

            Button measureBefore = CreateButton("Measure Before Video", false);
            measureBefore.Size = new Size(170, 38);
            measureBefore.Click += delegate { StartBodyAngleGuide("BeforeVideoPath"); };
            Button measureAfter = CreateButton("Measure After Video", true);
            measureAfter.Size = new Size(170, 38);
            measureAfter.Click += delegate { StartBodyAngleGuide("AfterVideoPath"); };
            Button reviewQuality = CreateButton("Review Measurement Quality", false);
            reviewQuality.Size = new Size(210, 38);
            reviewQuality.Click += ReviewMetrics_Click;
            actions.Controls.Add(guidedBefore);
            actions.Controls.Add(guidedAfter);
            actions.Controls.Add(measureBefore);
            actions.Controls.Add(measureAfter);
            actions.Controls.Add(reviewQuality);

            int actionRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            table.Controls.Add(actions, 0, actionRow);
            table.SetColumnSpan(actions, 3);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private void ShowGuidedRiderMeasurements(string imageKey, string defaultSide)
        {
            string path = imageBoxes.ContainsKey(imageKey) ? imageBoxes[imageKey].Text : string.Empty;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this,
                    "Choose a " + defaultSide + " report image first.\n\n" +
                    "Use Report → Report Images to select or save a clear side-view rider image, then return to Body Angles.",
                    "Guided Rider Measurements", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (RiderBodyGuidedMeasurementForm form = new RiderBodyGuidedMeasurementForm(path, defaultSide))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                foreach (KeyValuePair<string, string> measurement in form.ResultValues)
                {
                    string key = measurement.Key + form.ResultSide;
                    if (measurementBoxes.ContainsKey(key))
                        measurementBoxes[key].Text = measurement.Value;
                }

                SaveCurrentSession();
                UpdateSaveHint("Guided rider measurements saved to " + form.ResultSide.ToLowerInvariant() + ".");
            }
        }

        private void AddBodyAngleGuide(TableLayoutPanel table)
        {
            GroupBox guide = new GroupBox();
            guide.Text = "Body angle guide";
            guide.Dock = DockStyle.Fill;
            guide.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            guide.ForeColor = Color.FromArgb(37, 48, 43);
            guide.Padding = new Padding(12, 8, 12, 12);

            TableLayoutPanel guideTable = new TableLayoutPanel();
            guideTable.Dock = DockStyle.Fill;
            guideTable.ColumnCount = 2;
            guideTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            guideTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddBodyAngleGuideRow(guideTable, "Knee angle", "Click hip -> knee -> ankle. Use bottom of the pedal stroke for leg extension.");
            AddBodyAngleGuideRow(guideTable, "Hip angle", "Click shoulder/torso point -> hip -> knee. Use the same crank position before and after.");
            AddBodyAngleGuideRow(guideTable, "Ankle angle", "Click knee -> ankle -> toe/forefoot. Keep frame timing consistent.");
            AddBodyAngleGuideRow(guideTable, "Body reach", "Use the rider contact points to record how stretched the rider looks on the bike.");
            AddBodyAngleGuideRow(guideTable, "Back angle", "Use the hip-to-shoulder/back line to describe posture and back position.");

            guide.Controls.Add(guideTable);

            int guideRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
            table.Controls.Add(guide, 0, guideRow);
            table.SetColumnSpan(guide, 3);
        }

        private void AddBodyAngleGuideRow(TableLayoutPanel table, string labelText, string instructions)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            Label label = FieldLabel(labelText);
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(60, 145, 76);

            Label instructionLabel = FieldLabel(instructions);
            instructionLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            instructionLabel.ForeColor = Color.FromArgb(74, 87, 81);
            instructionLabel.Padding = new Padding(0, 0, 0, 0);

            table.Controls.Add(label, 0, row);
            table.Controls.Add(instructionLabel, 1, row);
        }

        private void AddMediaRow(TableLayoutPanel table, string labelText, string key)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

            Label label = FieldLabel(labelText);
            Panel pathPanel = new Panel();
            pathPanel.Dock = DockStyle.Fill;
            pathPanel.Margin = new Padding(0, 6, 8, 4);

            TextBox path = new TextBox();
            path.Dock = DockStyle.Top;
            path.ReadOnly = true;
            path.BorderStyle = BorderStyle.FixedSingle;
            path.Margin = new Padding(0, 0, 0, 0);
            mediaBoxes.Add(key, path);

            Label status = new Label();
            status.Dock = DockStyle.Bottom;
            status.Height = 20;
            status.ForeColor = Color.FromArgb(92, 104, 98);
            status.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            status.Text = "No " + labelText.ToLowerInvariant() + " video selected yet.";
            status.AutoEllipsis = true;
            mediaStatusLabels.Add(key, status);

            pathPanel.Controls.Add(status);
            pathPanel.Controls.Add(path);

            Button browse = CreateButton("Browse…", false);
            browse.Margin = new Padding(0, 6, 8, 6);
            browse.Dock = DockStyle.Fill;
            browse.Click += delegate { BrowseVideo(key); };

            Button record = CreateButton("Record Live", true);
            record.Margin = new Padding(0, 6, 8, 6);
            record.Dock = DockStyle.Fill;
            record.Click += delegate { OpenLiveCaptureForVideo(key); };

            Button latest = CreateButton("Use Latest", false);
            latest.Margin = new Padding(0, 6, 8, 6);
            latest.Dock = DockStyle.Fill;
            latest.Click += delegate { UseLatestVideo(key); };

            Button open = CreateButton("Analyze", false);
            open.Margin = new Padding(0, 6, 0, 6);
            open.Dock = DockStyle.Fill;
            open.Click += delegate { OpenSingle(key); };

            table.Controls.Add(label, 0, row);
            table.Controls.Add(pathPanel, 1, row);
            table.Controls.Add(browse, 2, row);
            table.Controls.Add(record, 3, row);
            table.Controls.Add(latest, 4, row);
            table.Controls.Add(open, 5, row);
        }

        private void AddImageRow(TableLayoutPanel table, string labelText, string key)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            TextBox path = new TextBox();
            path.Dock = DockStyle.Fill;
            path.ReadOnly = true;
            path.BorderStyle = BorderStyle.FixedSingle;
            path.Margin = new Padding(0, 8, 8, 8);
            imageBoxes.Add(key, path);

            Button browse = CreateButton("Browse…", false);
            browse.Margin = new Padding(0, 6, 8, 6);
            browse.Dock = DockStyle.Fill;
            browse.Click += delegate { BrowseReportImage(key); };

            Button open = CreateButton("Open", false);
            open.Margin = new Padding(0, 6, 0, 6);
            open.Dock = DockStyle.Fill;
            open.Click += delegate { OpenReportImage(key); };

            table.Controls.Add(FieldLabel(labelText), 0, row);
            table.Controls.Add(path, 1, row);
            table.Controls.Add(browse, 2, row);
            table.Controls.Add(open, 3, row);
        }

        private void AddMeasurementReferenceImageControls(TableLayoutPanel table)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));

            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(0, 8, 0, 8);

            TextBox path = new TextBox();
            path.Dock = DockStyle.Top;
            path.ReadOnly = true;
            path.BorderStyle = BorderStyle.FixedSingle;
            path.Margin = new Padding(0, 0, 0, 8);
            imageBoxes.Add("MeasurementReferenceImagePath", path);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.Height = 38;
            buttons.FlowDirection = FlowDirection.LeftToRight;

            Button browse = CreateButton("Browse…", false);
            browse.Size = new Size(86, 32);
            browse.Click += delegate { BrowseReportImage("MeasurementReferenceImagePath"); };

            Button useBefore = CreateButton("Use Before", false);
            useBefore.Size = new Size(98, 32);
            useBefore.Click += delegate { UseMeasurementReferenceImage("BeforeReportImagePath", "Before image"); };

            Button useAfter = CreateButton("Use After", false);
            useAfter.Size = new Size(90, 32);
            useAfter.Click += delegate { UseMeasurementReferenceImage("AfterReportImagePath", "After image"); };

            Button useSideBySide = CreateButton("Use Side-by-side", false);
            useSideBySide.Size = new Size(128, 32);
            useSideBySide.Click += delegate { UseMeasurementReferenceImage("SideBySideReportImagePath", "Side-by-side image"); };

            Button combine = CreateButton("Combine B+A", false);
            combine.Size = new Size(112, 32);
            combine.Click += delegate { CombineBeforeAfterImages(true); };

            Button guided = CreateButton("Guided Capture", true);
            guided.Size = new Size(128, 32);
            guided.Click += delegate { ShowGuidedBikeMetricCapture(); };

            Button open = CreateButton("Open", true);
            open.Size = new Size(70, 32);
            open.Click += delegate { OpenReportImage("MeasurementReferenceImagePath"); };

            buttons.Controls.Add(browse);
            buttons.Controls.Add(useBefore);
            buttons.Controls.Add(useAfter);
            buttons.Controls.Add(useSideBySide);
            buttons.Controls.Add(combine);
            buttons.Controls.Add(guided);
            buttons.Controls.Add(open);
            panel.Controls.Add(buttons);
            panel.Controls.Add(path);

            table.Controls.Add(FieldLabel("Measurement image"), 0, row);
            table.Controls.Add(panel, 1, row);
            table.SetColumnSpan(panel, 4);
        }

        private void AddMeasurementHeader(TableLayoutPanel table)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            table.Controls.Add(FieldLabel("Measurement"), 0, row);
            table.Controls.Add(HeaderLabel("BEFORE"), 1, row);
            table.Controls.Add(HeaderLabel("AFTER"), 2, row);
        }

        private void AddBikeMetricHeader(TableLayoutPanel table)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            table.Controls.Add(FieldLabel("Metric"), 0, row);
            table.Controls.Add(HeaderLabel("HOW TO MEASURE"), 1, row);
            table.Controls.Add(HeaderLabel("BEFORE"), 2, row);
            table.Controls.Add(HeaderLabel("AFTER"), 3, row);
            table.Controls.Add(HeaderLabel("ASSIST"), 4, row);
        }

        private void AddBikeMetricRow(TableLayoutPanel table, string labelText, string instructions, string key)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

            Label instructionLabel = FieldLabel(instructions);
            instructionLabel.Padding = new Padding(0, 6, 12, 6);

            TextBox before = NewMeasurementBox();
            TextBox after = NewMeasurementBox();
            measurementBoxes.Add(key + "Before", before);
            measurementBoxes.Add(key + "After", after);

            Button assist = CreateButton("Assist", false);
            assist.Margin = new Padding(0, 14, 0, 14);
            assist.Dock = DockStyle.Fill;
            assist.Click += delegate { ShowBikeMetricAssist(labelText, instructions, key); };

            table.Controls.Add(FieldLabel(labelText), 0, row);
            table.Controls.Add(instructionLabel, 1, row);
            table.Controls.Add(before, 2, row);
            table.Controls.Add(after, 3, row);
            table.Controls.Add(assist, 4, row);
        }

        private void AddMeasurementRow(TableLayoutPanel table, string labelText, string key)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            TextBox before = NewMeasurementBox();
            TextBox after = NewMeasurementBox();
            measurementBoxes.Add(key + "Before", before);
            measurementBoxes.Add(key + "After", after);
            table.Controls.Add(FieldLabel(labelText), 0, row);
            table.Controls.Add(before, 1, row);
            table.Controls.Add(after, 2, row);
        }

        private static TextBox NewMeasurementBox()
        {
            TextBox box = new TextBox();
            box.Dock = DockStyle.Fill;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Margin = new Padding(0, 6, 12, 6);
            return box;
        }

        private static TableLayoutPanel NewEditorTable()
        {
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.BackColor = CassetteMotionTheme.Canvas;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return table;
        }

        private static void AddEditorRow(TableLayoutPanel table, string labelText, Control control, int height)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 4, 0, 4);
            table.Controls.Add(FieldLabel(labelText), 0, row);
            table.Controls.Add(control, 1, row);
        }

        private static Label FieldLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(74, 87, 81);
            return label;
        }

        private static Label HeaderLabel(string text)
        {
            Label label = FieldLabel(text);
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(113, 127, 120);
            return label;
        }

        private static TabPage NewTab(string text)
        {
            TabPage page = new TabPage(text);
            page.BackColor = CassetteMotionTheme.Canvas;
            page.Padding = new Padding(0);
            return page;
        }

        private static void ApplyVisualIdentity(Control root)
        {
            foreach (Control control in root.Controls)
            {
                if (control is TextBox || control is ComboBox || control is DateTimePicker)
                    CassetteMotionTheme.StyleTextInput(control);
                ListView list = control as ListView;
                if (list != null)
                    CassetteMotionTheme.StyleListView(list);
                ApplyVisualIdentity(control);
            }
        }

        private void SaveAndSelectVideos()
        {
            if (!RequireActiveFitSessionBeforeKinovea("Record / Analyze"))
                return;

            try
            {
                PrepareAnalysisCaptureFolder();
                SetFitCommandCenterMode("Record / Analyze");
                SelectWorkspaceTab(KinoveaVideoTabName);
                UpdateSaveHint("Client and fit details saved. Next step: record/import videos, analyze in Video Studio, and save evidence.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The session could not be saved before opening analysis.\n\n" + exception.Message, "Open Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrepareAndSelectVideoAnalysis()
        {
            if (!RequireActiveFitSessionBeforeKinovea("Analyze Videos"))
                return;

            try
            {
                PrepareAnalysisCaptureFolder();
                SetFitCommandCenterMode("Analyze");
                SelectWorkspaceTab(KinoveaVideoTabName);
                UpdateSaveHint("Analysis Captures is ready. Open a video, measure in Video Studio, then save screenshots, exports, or clips into that folder.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The Analysis Captures folder could not be prepared.\n\n" + exception.Message, KinoveaVideoTabName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectOverviewGoals()
        {
            SelectWorkspaceTab("Overview");
            txtGoals.Focus();
        }

        private void SelectFitSessionStart()
        {
            SelectWorkspaceTab(SessionSetupTabName);
            UpdateSaveHint("Start here: use + New Session on the left or choose an existing session, enter the details, then Save before opening Video Studio.");
            if (txtTitle != null)
            {
                txtTitle.Focus();
                txtTitle.SelectAll();
            }
        }

        private bool RequireActiveFitSessionBeforeKinovea(string actionName)
        {
            if (HasActiveFitSession())
                return true;

            SelectFitSessionStart();
            MessageBox.Show(
                this,
                "Create or open a client fit session first.\n\n" +
                "Click + New Session on the left, enter the session details, then click Save. After that, " + actionName + " will know where the Before / After / Dual folders are.",
                actionName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private void SelectWorkspaceTab(string tabText)
        {
            if (editorTabs == null)
                return;

            string requestedTab = NormalizeWorkspaceTabName(tabText);
            if (SelectWorkspaceTab(editorTabs, requestedTab))
                UpdateSaveHint("Opened " + requestedTab + " from the Fit Day workspace.");
        }

        private static bool SelectWorkspaceTab(TabControl tabs, string requestedTab)
        {
            foreach (TabPage page in tabs.TabPages)
            {
                if (string.Equals(page.Text, requestedTab, StringComparison.OrdinalIgnoreCase))
                {
                    tabs.SelectedTab = page;
                    return true;
                }

                foreach (Control control in page.Controls)
                {
                    TabControl nestedTabs = control as TabControl;
                    if (nestedTabs != null && SelectWorkspaceTab(nestedTabs, requestedTab))
                    {
                        tabs.SelectedTab = page;
                        return true;
                    }
                }
            }

            return false;
        }

        private static string NormalizeWorkspaceTabName(string tabText)
        {
            if (string.Equals(tabText, "Overview", StringComparison.OrdinalIgnoreCase))
                return SessionSetupTabName;

            if (string.Equals(tabText, "Video Capture + Analysis", StringComparison.OrdinalIgnoreCase))
                return KinoveaVideoTabName;

            if (string.Equals(tabText, "Kinovea Video", StringComparison.OrdinalIgnoreCase))
                return KinoveaVideoTabName;

            return tabText;
        }

        private void UpdateWorkflowChecklist()
        {
            foreach (WorkflowChecklistItem item in workflowChecklistItems)
            {
                bool ready = false;
                if (item.IsReady != null)
                    ready = item.IsReady();

                item.StatusLabel.Text = ready ? "Ready" : "Needs step";
                item.StatusLabel.ForeColor = ready ? Color.FromArgb(60, 145, 76) : Color.FromArgb(181, 118, 35);
            }

            UpdateFitCommandCenterStatus();
            UpdateFitDayHomeStatus();
            UpdateGuidedFitDayFlow();
            UpdateNextRecommendedStep();
            UpdateReportBuilderStatus();
            RefreshCombinedMeasurementReview();
            RefreshSmartRecommendationStatus();
            RefreshFitSessionFinalization();
        }

        private void RefreshFitSessionFinalization()
        {
            if (finalizationStatus == null || finalizationChecklist == null)
                return;

            if (!HasActiveFitSession())
            {
                finalizationStatus.Text = "START HERE · Open or create a client fit session first.";
                finalizationStatus.ForeColor = Color.FromArgb(181, 118, 35);
                finalizationChecklist.Text = "The finalization assistant will check the active session’s client details, media, evidence, measurements, report writing, and output readiness.";
                return;
            }

            List<string> required = GetReportReadinessWarnings();
            List<string> qualityNotes = GetCombinedMeasurementReviewNotes();
            bool hasGoals = HasFitGoals();
            bool hasSummary = HasReportSummaryContent();
            bool hasRecommendations = !string.IsNullOrWhiteSpace(txtFitSummaryRecommendations.Text) || !string.IsNullOrWhiteSpace(txtFitSummaryFollowUp.Text);
            bool hasBodyAngles = HasAnyBodyAngleMeasurements();
            bool isComplete = string.Equals(Convert.ToString(cmbStatus.SelectedItem), "Complete", StringComparison.OrdinalIgnoreCase);

            System.Text.StringBuilder checklist = new System.Text.StringBuilder();
            checklist.AppendLine("REQUIRED FIT-DAY ITEMS");
            checklist.AppendLine(FinalizationLine(true, "Client and active fit session", currentSession.DisplayName));
            checklist.AppendLine(FinalizationLine(HasMediaFile("BeforeVideoPath"), "Before video", "Record or select the Before video."));
            checklist.AppendLine(FinalizationLine(HasMediaFile("AfterVideoPath"), "After video", "Record or select the After video."));
            checklist.AppendLine(FinalizationLine(HasAnalysisCaptureEvidence() || HasSavedSessionEvidence(), "Saved fit evidence", "Save useful Before, After, or Dual evidence."));
            checklist.AppendLine(FinalizationLine(HasCoreBikeMetrics(), "Core bike measurements", "Complete the final Bike Metrics."));
            checklist.AppendLine(FinalizationLine(HasReportImage(), "Report image", "Choose a Before, After, or Dual report image."));
            checklist.AppendLine();
            checklist.AppendLine("REPORT POLISH");
            checklist.AppendLine(FinalizationLine(hasGoals, "Rider goals", "Recommended for report context."));
            checklist.AppendLine(FinalizationLine(hasBodyAngles, "Rider body measurements", "Optional; add the rider angles used during the fit."));
            checklist.AppendLine(FinalizationLine(hasSummary, "Fit Summary", "Add findings, changes made, or recommendations."));
            checklist.AppendLine(FinalizationLine(hasRecommendations, "Recommendations / follow-up", "Review the smart draft or write your own."));
            checklist.AppendLine(FinalizationLine(qualityNotes.Count == 0, "Measurement quality review", qualityNotes.Count == 0 ? "No broad warnings found." : qualityNotes.Count.ToString() + " item(s) need professional review."));
            checklist.AppendLine();
            checklist.AppendLine("OUTPUT");
            checklist.AppendLine(FinalizationLine(isComplete, "Session status", isComplete ? "Complete" : "Mark complete after reviewing the preview."));
            checklist.AppendLine("Reports folder: " + GetSessionReportsFolderPath());

            if (required.Count > 0)
            {
                checklist.AppendLine();
                checklist.AppendLine("ITEMS TO CHECK BEFORE FINAL OUTPUT");
                foreach (string warning in required)
                    checklist.AppendLine("• " + warning);
            }

            finalizationChecklist.Text = checklist.ToString();
            bool ready = required.Count == 0;
            finalizationStatus.Text = (ready ? "READY FOR FINAL PREVIEW" : "NEEDS " + required.Count.ToString() + " REQUIRED STEP(S)") +
                "   ·   Session status: " + Convert.ToString(cmbStatus.SelectedItem) + Environment.NewLine +
                (ready ? "Preview the report, then mark complete and create the client package or ZIP." : "Use the checklist below to return to the unfinished parts of the fit.");
            finalizationStatus.ForeColor = ready ? Color.FromArgb(60, 145, 76) : Color.FromArgb(181, 118, 35);
        }

        private static string FinalizationLine(bool ready, string label, string detail)
        {
            return (ready ? "✓ READY   " : "○ CHECK   ") + label + " — " + detail;
        }

        private bool HasAnyBodyAngleMeasurements()
        {
            string[] keys = new string[] { "KneeAngle", "HipAngle", "AnkleAngle", "TorsoAngle", "ShoulderAngle" };
            foreach (string key in keys)
            {
                if (!string.IsNullOrWhiteSpace(GetMeasurementText(key + "Before")) || !string.IsNullOrWhiteSpace(GetMeasurementText(key + "After")))
                    return true;
            }
            return false;
        }

        private bool TryMarkFitSessionComplete()
        {
            if (!HasActiveFitSession())
            {
                MessageBox.Show(this, "Open or create a client fit session first.", "Finalize Fit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (string.Equals(Convert.ToString(cmbStatus.SelectedItem), "Complete", StringComparison.OrdinalIgnoreCase))
                return true;

            SaveCurrentSession();
            List<string> warnings = GetReportReadinessWarnings();
            string message = warnings.Count == 0
                ? "Mark this fit session Complete?\n\nYou can still reopen and edit it later."
                : "This session still has items to review:\n\n• " + string.Join("\n• ", warnings.ToArray()) + "\n\nMark it Complete anyway?";
            DialogResult result = MessageBox.Show(this, message, "Finalize Fit Session", MessageBoxButtons.YesNo, warnings.Count == 0 ? MessageBoxIcon.Question : MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return false;

            cmbStatus.SelectedItem = "Complete";
            SaveCurrentSession();
            Guid sessionId = currentSession.Id;
            RefreshSessions(sessionId);
            RefreshFitSessionFinalization();
            UpdateSaveHint("Fit session marked Complete. It can still be reopened and edited.");
            return true;
        }

        private void RefreshSmartRecommendationStatus()
        {
            if (smartRecommendationStatus == null || smartRecommendationDraft == null)
                return;

            string[] keys = new string[] { "SaddleHeight", "SaddleSetback", "SaddleTipToGripReach", "HandlebarX", "HandlebarY", "KneeAngle", "HipAngle", "AnkleAngle", "TorsoAngle", "ShoulderAngle" };
            int pairs = 0;
            foreach (string key in keys)
            {
                double before;
                double after;
                if (TryParseMeasurementNumber(GetMeasurementText(key + "Before"), out before) && TryParseMeasurementNumber(GetMeasurementText(key + "After"), out after))
                    pairs++;
            }

            smartRecommendationStatus.Text = pairs == 0
                ? "Add Before and After measurements, then generate an editable report draft. Nothing is added automatically."
                : pairs.ToString() + " Before/After measurement pairs are available. " + (string.IsNullOrWhiteSpace(smartRecommendationDraft.Text) ? "Generate a draft when ready." : "Draft is editable; add it to the report only after reviewing it.");
            smartRecommendationStatus.ForeColor = pairs == 0 ? Color.FromArgb(181, 118, 35) : Color.FromArgb(60, 145, 76);
        }

        private void GenerateSmartRecommendationDraft()
        {
            if (currentSession == null)
            {
                MessageBox.Show(this, "Open or create a client fit session first.", "Smart Recommendations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<string> observations = new List<string>();
            AddSmartChangeObservation(observations, "SaddleHeight", 2, "mm", "Saddle height was raised", "Saddle height was lowered");
            AddSmartChangeObservation(observations, "SaddleSetback", 2, "mm", "The saddle moved forward", "The saddle moved rearward");
            AddSmartChangeObservation(observations, "SaddleTipToGripReach", 3, "mm", "Saddle-to-grip reach increased", "Saddle-to-grip reach decreased");
            AddSmartChangeObservation(observations, "HandlebarX", 3, "mm", "The handlebar contact point moved forward", "The handlebar contact point moved rearward");
            AddSmartChangeObservation(observations, "HandlebarY", 3, "mm", "The handlebar contact point moved upward", "The handlebar contact point moved downward");
            AddSmartChangeObservation(observations, "KneeAngle", 1, "°", "Knee angle increased", "Knee angle decreased");
            AddSmartChangeObservation(observations, "HipAngle", 1, "°", "Hip angle increased", "Hip angle decreased");
            AddSmartChangeObservation(observations, "AnkleAngle", 1, "°", "Ankle angle increased", "Ankle angle decreased");
            AddSmartChangeObservation(observations, "TorsoAngle", 1, "°", "Body reach angle increased", "Body reach angle decreased");
            AddSmartChangeObservation(observations, "ShoulderAngle", 1, "°", "Back angle became more upright", "Back angle became lower");

            int availablePairs = CountSmartRecommendationPairs();
            if (availablePairs == 0)
            {
                MessageBox.Show(this, "Enter at least one matching Before and After measurement first.", "Smart Recommendations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            System.Text.StringBuilder draft = new System.Text.StringBuilder();
            draft.AppendLine("Draft Before / After observations:");
            if (observations.Count == 0)
                draft.AppendLine("• The recorded Before and After measurements show only small changes.");
            else
            {
                foreach (string observation in observations)
                    draft.AppendLine("• " + observation);
            }

            draft.AppendLine();
            draft.AppendLine("Suggested follow-up:");
            draft.AppendLine("• Confirm comfort, control, and pedaling response with the rider before treating the changes as final.");
            draft.AppendLine("• Recheck the same camera view and crank position if any measurement does not match what was observed during the fit.");
            draft.AppendLine("• Reassess after the rider has completed an appropriate adaptation period, especially if several contact points changed.");
            draft.AppendLine();
            draft.Append("Fitter note: Review and edit this draft before including it in the client report. These observations describe recorded changes and are not a diagnosis.");

            smartRecommendationDraft.Text = draft.ToString();
            RefreshSmartRecommendationStatus();
            UpdateSaveHint("Smart Before/After draft generated. Review it before adding it to the report.");
        }

        private int CountSmartRecommendationPairs()
        {
            string[] keys = new string[] { "SaddleHeight", "SaddleSetback", "SaddleTipToGripReach", "HandlebarX", "HandlebarY", "KneeAngle", "HipAngle", "AnkleAngle", "TorsoAngle", "ShoulderAngle" };
            int count = 0;
            foreach (string key in keys)
            {
                double before;
                double after;
                if (TryParseMeasurementNumber(GetMeasurementText(key + "Before"), out before) && TryParseMeasurementNumber(GetMeasurementText(key + "After"), out after))
                    count++;
            }
            return count;
        }

        private void AddSmartChangeObservation(List<string> observations, string key, double minimumChange, string unit, string positiveText, string negativeText)
        {
            double before;
            double after;
            if (!TryParseMeasurementNumber(GetMeasurementText(key + "Before"), out before) || !TryParseMeasurementNumber(GetMeasurementText(key + "After"), out after))
                return;

            double change = after - before;
            if (Math.Abs(change) < minimumChange)
                return;
            string direction = change > 0 ? positiveText : negativeText;
            observations.Add(direction + " by " + Math.Abs(change).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " " + unit + " (" + GetMeasurementText(key + "Before") + " → " + GetMeasurementText(key + "After") + ").");
        }

        private void AddSmartDraftToSummary(TextBox destination, string destinationName)
        {
            string draft = smartRecommendationDraft.Text.Trim();
            if (string.IsNullOrWhiteSpace(draft))
            {
                MessageBox.Show(this, "Generate and review a draft first.", "Smart Recommendations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string existing = destination.Text.Trim();
            destination.Text = string.IsNullOrEmpty(existing) ? draft : existing + Environment.NewLine + Environment.NewLine + draft;
            SaveCurrentSession();
            UpdateReportBuilderStatus();
            UpdateSaveHint("Reviewed smart draft added to " + destinationName + ".");
        }

        private void RefreshCombinedMeasurementReview()
        {
            if (combinedMeasurementReview == null || combinedMeasurementReviewStatus == null)
                return;

            string[] bikeKeys = new string[] { "SaddleHeight", "SaddleSetback", "SaddleTipToGripReach", "HandlebarX", "HandlebarY", "HandlebarReach", "HandlebarDrop", "CrankLength", "Wheelbase", "CleatPosition" };
            string[] bikeLabels = new string[] { "Saddle height", "Saddle setback", "Saddle tip to grip", "Handlebar X", "Handlebar Y", "Handlebar reach", "Handlebar drop", "Crank length", "Wheelbase", "Cleat position" };
            string[] riderKeys = new string[] { "KneeAngle", "HipAngle", "AnkleAngle", "TorsoAngle", "ShoulderAngle" };
            string[] riderLabels = new string[] { "Knee angle", "Hip angle", "Ankle angle", "Body reach", "Back angle" };

            int completePairs = 0;
            int partialPairs = 0;
            int missingPairs = 0;
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            text.AppendLine("MEASUREMENT                     BEFORE          AFTER           CHANGE");
            text.AppendLine("────────────────────────────────────────────────────────────────────────");
            text.AppendLine("BIKE MEASUREMENTS");
            AppendCombinedMeasurementSection(text, bikeLabels, bikeKeys, ref completePairs, ref partialPairs, ref missingPairs);
            text.AppendLine();
            text.AppendLine("RIDER BODY MEASUREMENTS");
            AppendCombinedMeasurementSection(text, riderLabels, riderKeys, ref completePairs, ref partialPairs, ref missingPairs);

            List<string> notes = GetCombinedMeasurementReviewNotes();
            text.AppendLine();
            text.AppendLine("REVIEW NOTES");
            if (notes.Count == 0)
                text.AppendLine("✓ No broad quality warnings found. Confirm the results against your professional judgment.");
            else
            {
                foreach (string note in notes)
                    text.AppendLine("• " + note);
            }

            combinedMeasurementReview.Text = text.ToString();
            string sessionName = currentSession == null ? "No active session" : currentSession.DisplayName;
            bool hasMeasurements = completePairs > 0 || partialPairs > 0;
            bool reviewReady = hasMeasurements && notes.Count == 0;
            combinedMeasurementReviewStatus.Text = sessionName + "   ·   Complete Before/After pairs: " + completePairs.ToString() + "   ·   Partial: " + partialPairs.ToString() + "   ·   Not recorded: " + missingPairs.ToString() + Environment.NewLine +
                (reviewReady ? "COMBINED VIEW READY — blank optional measurements are okay" : hasMeasurements ? "REVIEW THE NOTES BELOW BEFORE FINALIZING THE REPORT" : "ADD BIKE OR RIDER MEASUREMENTS TO BEGIN THE COMBINED REVIEW");
            combinedMeasurementReviewStatus.ForeColor = reviewReady ? Color.FromArgb(60, 145, 76) : Color.FromArgb(181, 118, 35);
        }

        private void AppendCombinedMeasurementSection(System.Text.StringBuilder text, string[] labels, string[] keys, ref int completePairs, ref int partialPairs, ref int missingPairs)
        {
            for (int index = 0; index < keys.Length; index++)
            {
                string before = GetMeasurementText(keys[index] + "Before");
                string after = GetMeasurementText(keys[index] + "After");
                if (!string.IsNullOrWhiteSpace(before) && !string.IsNullOrWhiteSpace(after))
                    completePairs++;
                else if (!string.IsNullOrWhiteSpace(before) || !string.IsNullOrWhiteSpace(after))
                    partialPairs++;
                else
                    missingPairs++;

                string change = FormatMeasurementChange(before, after);
                text.AppendLine(labels[index].PadRight(31) + DisplayReviewValue(before).PadRight(16) + DisplayReviewValue(after).PadRight(16) + change);
            }
        }

        private static string DisplayReviewValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        }

        private static string FormatMeasurementChange(string before, string after)
        {
            double beforeValue;
            double afterValue;
            if (!TryParseMeasurementNumber(before, out beforeValue) || !TryParseMeasurementNumber(after, out afterValue))
                return "—";
            double change = afterValue - beforeValue;
            return (change > 0 ? "+" : string.Empty) + change.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private List<string> GetCombinedMeasurementReviewNotes()
        {
            List<string> notes = new List<string>();
            ReviewMeasurementChange(notes, "Saddle height", "SaddleHeight", 60, "mm");
            ReviewMeasurementChange(notes, "Saddle setback", "SaddleSetback", 50, "mm");
            ReviewMeasurementChange(notes, "Knee angle", "KneeAngle", 20, "°");
            ReviewMeasurementChange(notes, "Hip angle", "HipAngle", 20, "°");
            ReviewMeasurementChange(notes, "Back angle", "ShoulderAngle", 20, "°");
            ReviewMetricRangeBothSides(notes, "Knee angle", "KneeAngle", 90, 175, "degrees", "Recheck hip, knee, and ankle landmarks.");
            ReviewMetricRangeBothSides(notes, "Hip angle", "HipAngle", 25, 150, "degrees", "Recheck shoulder, hip, and knee landmarks.");
            ReviewMetricRangeBothSides(notes, "Back angle", "ShoulderAngle", 5, 85, "degrees", "Recheck hip and shoulder landmarks.");
            return notes;
        }

        private void UpdateReportBuilderStatus()
        {
            if (reportBuilderStatus == null || reportBuilderOutput == null)
                return;

            bool hasSession = HasActiveFitSession();
            bool hasSummary = HasReportSummaryContent();
            bool hasImages = HasReportImage();
            bool hasMetrics = HasCoreBikeMetrics();
            int ready = 0;
            if (hasSession) ready++;
            if (hasSummary) ready++;
            if (hasImages) ready++;
            if (hasMetrics) ready++;

            reportBuilderStatus.Text = "REPORT READINESS  " + ready + "/4" + Environment.NewLine +
                FormatReportBuilderLine("Client and fit session", hasSession) + Environment.NewLine +
                FormatReportBuilderLine("Fit Summary story", hasSummary) + Environment.NewLine +
                FormatReportBuilderLine("Before / After / Dual report image", hasImages) + Environment.NewLine +
                FormatReportBuilderLine("Final Bike Metrics", hasMetrics) + Environment.NewLine +
                (HasReportHandoffContent() ? "Optional handoff notes are included." : "Optional: add Handoff notes if the client needs instructions.");
            reportBuilderStatus.ForeColor = ready == 4 ? Color.FromArgb(60, 145, 76) : Color.FromArgb(181, 118, 35);

            if (!hasSession)
            {
                reportBuilderOutput.Text = "Open or save a client fit session first. Report previews and packages will then stay inside that session’s Reports folder.";
                reportBuilderOutput.ForeColor = Color.FromArgb(181, 118, 35);
                return;
            }

            reportBuilderOutput.Text = "Output folder: " + GetSessionReportsFolderPath() + Environment.NewLine +
                "Preview opens the current HTML report. Package collects the report, images, summary, and handoff files; Zip makes it ready to send.";
            reportBuilderOutput.ForeColor = Color.FromArgb(74, 87, 81);
        }

        private static string FormatReportBuilderLine(string label, bool ready)
        {
            return (ready ? "READY  " : "NEEDS STEP  ") + label;
        }

        private bool HasReportSummaryContent()
        {
            return !string.IsNullOrWhiteSpace(txtFitSummaryMainGoal.Text) ||
                !string.IsNullOrWhiteSpace(txtFitSummaryKeyFindings.Text) ||
                !string.IsNullOrWhiteSpace(txtFitSummaryChangesMade.Text) ||
                !string.IsNullOrWhiteSpace(txtFitSummaryRecommendations.Text) ||
                !string.IsNullOrWhiteSpace(txtFitSummaryFollowUp.Text);
        }

        private bool HasReportHandoffContent()
        {
            return !string.IsNullOrWhiteSpace(txtHandoffWhatToSend.Text) ||
                !string.IsNullOrWhiteSpace(txtHandoffClientMessage.Text) ||
                !string.IsNullOrWhiteSpace(txtHandoffHomework.Text) ||
                !string.IsNullOrWhiteSpace(txtHandoffNextAppointment.Text);
        }

        private void UpdateFitDayHomeStatus()
        {
            if (fitDayHomeStatus == null)
                return;

            UpdateFitDayHomeFolderPanel();

            if (!HasActiveFitSession())
            {
                fitDayHomeStatus.Text = "START HERE: create or open a client fit session first.";
                fitDayHomeStatus.ForeColor = Color.FromArgb(181, 118, 35);
                fitDayHomeReadiness.Text = GetActiveSaveTargetShortText() + Environment.NewLine +
                    GetNextFitDayHint();
                fitDayHomeReadiness.ForeColor = Color.FromArgb(181, 118, 35);
                return;
            }

            fitDayHomeStatus.Text = GetActiveSaveTargetShortText();
            fitDayHomeStatus.ForeColor = Color.FromArgb(60, 145, 76);
            fitDayHomeReadiness.Text = GetFitDayHomeProgressText();
            fitDayHomeReadiness.ForeColor = IsReportReady() ? Color.FromArgb(60, 145, 76) : Color.FromArgb(74, 87, 81);
        }

        private string GetFitDayHomeProgressText()
        {
            return GetFitDayReadinessText() + " · " + GetReadinessSnapshotText() + Environment.NewLine +
                GetActiveSaveTargetShortText() + Environment.NewLine +
                GetNextFitDayHint();
        }

        private string GetActiveSaveTargetShortText()
        {
            if (!HasActiveFitSession())
                return "No active fit session yet - click + New Session on the left, enter the session details, then Save before using Video Studio Save Image / Save Video.";

            string clientName = client != null ? client.DisplayName : "Client";
            return "READY: " + clientName + " - " + currentSession.DisplayName + " - Video Studio Save Image / Save Video will ask Before, After, or Dual and save into this session.";
        }

        private string GetActiveSaveTargetFolderText()
        {
            if (!HasActiveFitSession())
                return "No active saved fit session yet." + Environment.NewLine +
                    "Click + New Session, enter the client/session details, then Save before using Video Studio Save Image or Save Video.";

            string clientName = client != null ? client.DisplayName : "Client";
            return "Active save targets for " + clientName + " - " + currentSession.DisplayName + Environment.NewLine +
                "Videos: Before / After / Dual -> " + GetSessionVideosFolderPath() + Environment.NewLine +
                "Images: Before / After / Dual -> " + GetSessionReportImagesFolderPath();
        }

        private void UpdateFitCommandCenterStatus()
        {
            if (fitCommandCenterStatus == null)
                return;

            string clientName = client != null ? client.DisplayName : "Client";
        if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
        {
            fitCommandCenterStatus.Text = "START HERE: create or open a client fit session first." + Environment.NewLine +
                GetActiveSaveTargetShortText() + Environment.NewLine +
                GetNextFitDayHint();
            fitCommandCenterStatus.ForeColor = Color.FromArgb(181, 118, 35);
            UpdateSaveTargetStatus();
                RefreshSavedEvidenceReview();
                return;
            }

    string session = "Client: " + clientName + " · Session: " + currentSession.DisplayName;
    fitCommandCenterStatus.Text = GetGuidedFitDayStageText() + " · " + GetFitDayReadinessText() + " · " + session + Environment.NewLine +
        GetReadinessSnapshotText() + Environment.NewLine +
        GetActiveSaveTargetShortText() + Environment.NewLine +
        GetNextFitDayHint();
            fitCommandCenterStatus.ForeColor = IsReportReady() ? Color.FromArgb(60, 145, 76) : Color.FromArgb(74, 87, 81);
            UpdateSaveTargetStatus();
            RefreshSavedEvidenceReview();
        }

        private string GetNextFitDayHint()
        {
            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
                return "Next best step: click + New Session on the left or choose an existing session, then Save before opening Video Studio.";
            if (!HasFitGoals())
                return "Next best step: enter rider goals before you start making fit changes.";
            if (!HasMediaFile("BeforeVideoPath"))
                return "Next best step: record or save the Before video.";
            if (!HasMediaFile("AfterVideoPath"))
                return "Next best step: record or save the After video.";
            if (!HasAnalysisCaptureEvidence() && !HasSavedSessionEvidence())
                return "Next best step: save useful Before / After / Dual evidence from Video Studio.";
            if (!HasCoreBikeMetrics())
                return "Next best step: enter Measurements from your Video Studio measurements.";
            if (!HasReportImage())
                return "Next best step: choose the report images for the report.";
            return "Ready: preview the report and make sure the client story looks right.";
        }

        private string GetFitDayReadinessText()
        {
            int ready = 0;
            const int total = 6;

            if (currentSession != null && !string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
                ready++;
            if (HasMediaFile("BeforeVideoPath"))
                ready++;
            if (HasMediaFile("AfterVideoPath"))
                ready++;
            if (HasAnalysisCaptureEvidence() || HasSavedSessionEvidence())
                ready++;
            if (HasCoreBikeMetrics())
                ready++;
            if (HasReportImage())
                ready++;

            return "Ready check: " + ready + "/" + total + " complete";
        }

        private string GetGuidedFitDayStageText()
        {
            int ready = 0;
            const int total = 5;

            if (IsClientFlowStageReady())
                ready++;
            if (HasBeforeAfterVideos())
                ready++;
            if (IsVideoFlowStageReady())
                ready++;
            if (IsMeasurementFlowStageReady())
                ready++;
            if (IsReportFlowStageReady())
                ready++;

            return "Roadmap: " + ready + "/" + total + " steps ready";
        }

        private bool HasActiveFitSession()
        {
            return currentSession != null && !string.IsNullOrWhiteSpace(currentSession.StorageFolderName);
        }

        private bool HasBeforeAfterVideos()
        {
            return HasMediaFile("BeforeVideoPath") && HasMediaFile("AfterVideoPath");
        }

        private bool IsClientFlowStageReady()
        {
            return HasActiveFitSession() && HasFitGoals();
        }

        private string GetClientFlowDetail()
        {
            if (!HasActiveFitSession())
                return "Click + New Session on the left or choose an existing session, then Save before opening Video Studio.";
            if (!HasFitGoals())
                return "Add session title and rider goals.";
            return "Client session and goals are ready.";
        }

        private bool IsVideoFlowStageReady()
        {
            return HasBeforeAfterVideos() && (HasAnalysisCaptureEvidence() || HasSavedSessionEvidence());
        }

        private string GetVideoFlowDetail()
        {
            if (!HasActiveFitSession())
                return "Create/open a session first so Before / After / Dual folders are known.";
            if (!HasMediaFile("BeforeVideoPath"))
                return "Save or choose the Before video.";
            if (!HasMediaFile("AfterVideoPath"))
                return "Save or choose the After video.";
            if (!HasAnalysisCaptureEvidence() && !HasSavedSessionEvidence())
                return "Save Before / After / Dual evidence.";
            return "Video evidence is ready.";
        }

        private bool IsMeasurementFlowStageReady()
        {
            return HasCoreBikeMetrics();
        }

        private string GetMeasurementFlowDetail()
        {
            if (!HasCoreBikeMetrics())
                return "Enter the core bike metrics from Video Studio.";
            return "Bike metrics are ready.";
        }

        private bool IsReportFlowStageReady()
        {
            return HasReportImage();
        }

        private string GetReportFlowDetail()
        {
            if (!HasReportImage())
                return "Choose Before, After, Dual, or reference image.";
            return "Report images are ready.";
        }

        private void UpdateGuidedFitDayFlow()
        {
            if (fitDayFlowSteps.Count == 0)
                return;

            bool currentAssigned = false;
            foreach (FitDayFlowStep step in fitDayFlowSteps)
            {
                bool ready = step.IsReady != null && step.IsReady();
                bool current = !ready && !currentAssigned;
                if (current)
                    currentAssigned = true;

                step.Card.BackColor = ready ? Color.FromArgb(236, 250, 225) : current ? Color.FromArgb(255, 248, 226) : Color.White;
                step.NumberLabel.Text = ready ? "✓" : step.NumberText;
                step.NumberLabel.BackColor = ready ? Color.FromArgb(60, 145, 76) : current ? Color.FromArgb(181, 118, 35) : Color.FromArgb(224, 232, 227);
                step.NumberLabel.ForeColor = ready || current ? Color.White : Color.FromArgb(37, 48, 43);
                step.TitleLabel.ForeColor = ready ? Color.FromArgb(60, 145, 76) : current ? Color.FromArgb(37, 48, 43) : Color.FromArgb(74, 87, 81);
                step.DetailLabel.Text = step.GetDetail != null ? step.GetDetail() : string.Empty;
                step.ActionButton.BackColor = current ? Color.FromArgb(184, 243, 74) : Color.White;
                step.ActionButton.FlatAppearance.BorderColor = ready ? Color.FromArgb(60, 145, 76) : Color.FromArgb(186, 197, 191);
            }
        }

        private string GetReadinessSnapshotText()
        {
            return FormatCompactReadiness("Session", currentSession != null && !string.IsNullOrWhiteSpace(currentSession.StorageFolderName)) + "  " +
                FormatCompactReadiness("Before", HasMediaFile("BeforeVideoPath")) + "  " +
                FormatCompactReadiness("After", HasMediaFile("AfterVideoPath")) + "  " +
                FormatCompactReadiness("Evidence", HasAnalysisCaptureEvidence() || HasSavedSessionEvidence()) + "  " +
                FormatCompactReadiness("Metrics", HasCoreBikeMetrics()) + "  " +
                FormatCompactReadiness("Report image", HasReportImage());
        }

        private static string FormatCompactReadiness(string label, bool ready)
        {
            return (ready ? "✓ " : "□ ") + label;
        }

        private bool HasSavedSessionEvidence()
        {
            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
                return false;

            string reportImagesFolder = GetSessionReportImagesFolderPath();
            return CountSavedEvidenceFiles(GetSessionVideoViewFolderPath("Before"), true) > 0 ||
                CountSavedEvidenceFiles(GetSessionVideoViewFolderPath("After"), true) > 0 ||
                CountSavedEvidenceFiles(GetSessionVideoViewFolderPath("Dual"), true) > 0 ||
                CountSavedEvidenceFiles(Path.Combine(reportImagesFolder, "Before"), false) > 0 ||
                CountSavedEvidenceFiles(Path.Combine(reportImagesFolder, "After"), false) > 0 ||
                CountSavedEvidenceFiles(Path.Combine(reportImagesFolder, "Dual"), false) > 0;
        }

        private void UpdateSaveTargetStatus()
        {
            if (activeSaveTargetStatus == null)
                return;

        if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
        {
            activeSaveTargetStatus.Text = GetActiveSaveTargetShortText();
            activeSaveTargetStatus.ForeColor = Color.FromArgb(181, 118, 35);
            activeSaveTargetStatus.BackColor = Color.FromArgb(255, 248, 226);
            return;
        }

        activeSaveTargetStatus.Text = GetActiveSaveTargetShortText();
            activeSaveTargetStatus.ForeColor = Color.FromArgb(60, 145, 76);
            activeSaveTargetStatus.BackColor = Color.FromArgb(235, 250, 238);
        }

        private void RefreshSavedEvidenceReview()
        {
            if (savedEvidenceReviewStatus == null)
                return;

            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
            {
                savedEvidenceReviewStatus.Text = "Open or create a client fit session first from Client Files. Then Video Studio Save Image and Save Video can offer Before, After, and Dual session folders.";
                savedEvidenceReviewStatus.ForeColor = Color.FromArgb(181, 118, 35);
                return;
            }

            savedEvidenceReviewStatus.ForeColor = Color.FromArgb(74, 87, 81);

            string reportImagesFolder = GetSessionReportImagesFolderPath();
            string beforeVideoFolder = GetSessionVideoViewFolderPath("Before");
            string afterVideoFolder = GetSessionVideoViewFolderPath("After");
            string dualVideoFolder = GetSessionVideoViewFolderPath("Dual");
            string beforeImageFolder = Path.Combine(reportImagesFolder, "Before");
            string afterImageFolder = Path.Combine(reportImagesFolder, "After");
            string dualImageFolder = Path.Combine(reportImagesFolder, "Dual");

            bool hasBeforeVideo = CountSavedEvidenceFiles(beforeVideoFolder, true) > 0;
            bool hasAfterVideo = CountSavedEvidenceFiles(afterVideoFolder, true) > 0;
            bool hasBeforeImage = CountSavedEvidenceFiles(beforeImageFolder, false) > 0;
            bool hasAfterImage = CountSavedEvidenceFiles(afterImageFolder, false) > 0;
            bool hasDualImage = CountSavedEvidenceFiles(dualImageFolder, false) > 0;
            bool hasDualEvidence = CountSavedEvidenceFiles(dualVideoFolder, true) > 0 || hasDualImage;
            bool hasReportImages = hasBeforeImage || hasAfterImage || hasDualImage;
            bool readyForReport = hasBeforeVideo && hasAfterVideo && hasReportImages;
            string nextStep = GetSavedEvidenceNextStep(hasBeforeVideo, hasAfterVideo, hasBeforeImage, hasAfterImage, hasDualEvidence);

            string text =
                "Active session: " + client.DisplayName + " · " + currentSession.DisplayName + Environment.NewLine +
                GetFitDayReadinessText() + Environment.NewLine +
                "Next best step: " + nextStep + Environment.NewLine +
                Environment.NewLine +
                "Fit-day checklist" + Environment.NewLine +
                FormatChecklistLine("Before video saved", hasBeforeVideo) + Environment.NewLine +
                FormatChecklistLine("After video saved", hasAfterVideo) + Environment.NewLine +
                FormatChecklistLine("Before report image saved", hasBeforeImage) + Environment.NewLine +
                FormatChecklistLine("After report image saved", hasAfterImage) + Environment.NewLine +
                FormatChecklistLine("Dual/composite evidence saved", hasDualEvidence) + Environment.NewLine +
                FormatChecklistLine("Ready to preview report", readyForReport) + Environment.NewLine +
                Environment.NewLine +
                "Videos" + Environment.NewLine +
                "Before: " + FormatSavedEvidenceSummary(beforeVideoFolder, true) + Environment.NewLine +
                "After: " + FormatSavedEvidenceSummary(afterVideoFolder, true) + Environment.NewLine +
                "Dual: " + FormatSavedEvidenceSummary(dualVideoFolder, true) + Environment.NewLine +
                Environment.NewLine +
                "Report Images" + Environment.NewLine +
                "Before: " + FormatSavedEvidenceSummary(beforeImageFolder, false) + Environment.NewLine +
                "After: " + FormatSavedEvidenceSummary(afterImageFolder, false) + Environment.NewLine +
                "Dual: " + FormatSavedEvidenceSummary(dualImageFolder, false);

            savedEvidenceReviewStatus.Text = text;
        }

        private static string GetSavedEvidenceNextStep(bool hasBeforeVideo, bool hasAfterVideo, bool hasBeforeImage, bool hasAfterImage, bool hasDualEvidence)
        {
            if (!hasBeforeVideo)
                return "save the Before video into this session.";

            if (!hasAfterVideo)
                return "save the After video into this session.";

            if (!hasBeforeImage && !hasAfterImage && !hasDualEvidence)
                return "save a report image from Video Studio so the report has visual evidence.";

            if (!hasBeforeImage)
                return "save a Before report image if you want the report to show the starting fit.";

            if (!hasAfterImage)
                return "save an After report image, or preview the report if the current evidence is enough.";

            return "preview the report, then generate the final client package.";
        }

        private static string FormatChecklistLine(string label, bool complete)
        {
            return (complete ? "✓ " : "□ ") + label;
        }

        private static string FormatSavedEvidenceSummary(string folder, bool videoFiles)
        {
            int count = CountSavedEvidenceFiles(folder, videoFiles);
            if (count == 0)
                return "No saved files yet";

            string latest = FindLatestEvidenceFile(folder, videoFiles);
            if (string.IsNullOrWhiteSpace(latest))
                return count + " file" + (count == 1 ? "" : "s");

            DateTime savedAt = File.GetLastWriteTime(latest);
            return Path.GetFileName(latest) + " · " + count + " file" + (count == 1 ? "" : "s") + " · " + savedAt.ToString("g");
        }

        private static int CountSavedEvidenceFiles(string folder, bool videoFiles)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return 0;

            int count = 0;
            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                if (IsEvidenceFile(file, videoFiles))
                    count++;
            }

            return count;
        }

        private static string FindLatestEvidenceFile(string folder, bool videoFiles)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return string.Empty;

            string latest = string.Empty;
            DateTime latestTime = DateTime.MinValue;
            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                if (!IsEvidenceFile(file, videoFiles))
                    continue;

                DateTime savedAt = File.GetLastWriteTime(file);
                if (string.IsNullOrEmpty(latest) || savedAt > latestTime)
                {
                    latest = file;
                    latestTime = savedAt;
                }
            }

            return latest;
        }

        private static string FindLatestEvidenceFileInFolders(bool videoFiles, params string[] folders)
        {
            string latest = string.Empty;
            DateTime latestTime = DateTime.MinValue;
            foreach (string folder in folders)
            {
                string candidate = FindLatestEvidenceFile(folder, videoFiles);
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                DateTime savedAt = File.GetLastWriteTime(candidate);
                if (string.IsNullOrEmpty(latest) || savedAt > latestTime)
                {
                    latest = candidate;
                    latestTime = savedAt;
                }
            }

            return latest;
        }

        private static bool IsEvidenceFile(string path, bool videoFiles)
        {
            string extension = Path.GetExtension(path);
            if (videoFiles)
            {
                return string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".mov", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".avi", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".mkv", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".m4v", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".mpg", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".mpeg", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(extension, ".wmv", StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase);
        }

        private void SetFitCommandCenterMode(string mode)
        {
            fitCommandCenterMode = string.IsNullOrWhiteSpace(mode) ? "Plan" : mode;
            UpdateFitCommandCenterStatus();
        }

        private string GetRecordingFolderGuideText()
        {
            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
            {
                return "Recording folders for this session:" + Environment.NewLine +
                    "Before → choose or create a fit session first" + Environment.NewLine +
                    "After  → choose or create a fit session first" + Environment.NewLine +
                    "Dual   → choose or create a fit session first";
            }

            return "Recording folders for this session:" + Environment.NewLine +
                "Before → " + GetSessionVideoViewFolderPath("Before") + Environment.NewLine +
                "After  → " + GetSessionVideoViewFolderPath("After") + Environment.NewLine +
                "Dual   → " + GetSessionVideoViewFolderPath("Dual");
        }

        private void RefreshRecordingFolderGuide()
        {
            recordingFoldersGuide.Text = GetRecordingFolderGuideText();
        }

        private void UpdateNextRecommendedStep()
        {
            if (nextRecommendedStep == null)
                return;

            string message;
            string actionText;
            Action action;
            string folderActionText;
            Action folderAction;
            Color color;

            if (!HasActiveFitSession())
            {
                message = "Next best step: click + New Session on the left, enter session details, then Save. That unlocks Before / After / Dual saving.";
                actionText = "Open Session";
                action = SelectFitSessionStart;
                folderActionText = "Session Start";
                folderAction = SelectFitSessionStart;
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasFitGoals())
            {
                message = "Next best step: enter rider goals first. This keeps the fit focused before you start changing the bike.";
                actionText = "Open Goals";
                action = SelectOverviewGoals;
                folderActionText = "Client Files";
                folderAction = delegate { SelectWorkspaceTab("Client Files"); };
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasMediaFile("BeforeVideoPath") || !HasMediaFile("AfterVideoPath"))
            {
                bool beforeMissing = !HasMediaFile("BeforeVideoPath");
                string viewName = beforeMissing ? "Before" : "After";
                message = "Next best step: record or save the " + viewName + " video into this client's folder.";
                actionText = "Record / Analyze";
                action = SaveAndSelectVideos;
                folderActionText = viewName + " Folder";
                folderAction = delegate { OpenClientFolder(GetSessionVideoViewFolderPath(viewName), viewName + " videos"); };
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasAnalysisCaptureEvidence())
            {
                message = "Next best step: review the saved videos in Video Studio, then save the best images/videos as Before, After, or Dual evidence.";
                actionText = "Review Evidence";
                action = PrepareAndSelectVideoAnalysis;
                folderActionText = "Captures";
                folderAction = OpenAnalysisCapturesFolder;
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasCoreBikeMetrics())
            {
                message = "Next best step: enter Measurements from your Video Studio measurements.";
                actionText = "Measurements";
                action = delegate { SelectWorkspaceTab("Bike Metrics"); };
                folderActionText = "Session File";
                folderAction = delegate { OpenClientFolder(GetSessionRecordFolderPath(), "Session record"); };
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasReportImage())
            {
                message = "Next best step: choose the report images — Before, After, or Dual — so the report has the right visuals.";
                actionText = "Report Images";
                action = delegate { SelectWorkspaceTab("Report Images"); };
                folderActionText = "Image Folder";
                folderAction = delegate { OpenClientFolder(GetSessionReportImagesFolderPath(), "Report images"); };
                color = Color.FromArgb(181, 118, 35);
            }
            else
            {
                message = "Ready: preview the report and confirm everything looks right.";
                actionText = "Preview Report";
                action = delegate { PreviewReport_Click(this, EventArgs.Empty); };
                folderActionText = "Reports";
                folderAction = delegate { OpenClientFolder(GetSessionReportsFolderPath(), "Reports"); };
                color = Color.FromArgb(60, 145, 76);
            }

            nextRecommendedStep.Text = message;
            nextRecommendedStep.ForeColor = color;
            nextRecommendedStepAction.Text = actionText;
            nextRecommendedStepAction.Enabled = action != null;
            nextRecommendedStepActionHandler = action;
            fitDayPrimaryAction.Text = actionText.ToUpperInvariant();
            fitDayPrimaryAction.Enabled = action != null;
            nextRecommendedFolderAction.Text = folderActionText;
            nextRecommendedFolderAction.Enabled = folderAction != null;
            nextRecommendedFolderActionHandler = folderAction;
        }

        private void RunNextBestFitDayStep()
        {
            UpdateNextRecommendedStep();
            if (nextRecommendedStepActionHandler != null)
                nextRecommendedStepActionHandler();

            UpdateWorkflowChecklist();
            UpdateFitCommandCenterStatus();
        }

        private bool HasClientFolder()
        {
            return client != null &&
                !string.IsNullOrWhiteSpace(client.FolderPath) &&
                Directory.Exists(client.FolderPath);
        }

        private bool HasFitGoals()
        {
            return !string.IsNullOrWhiteSpace(txtTitle.Text) &&
                !string.IsNullOrWhiteSpace(txtGoals.Text);
        }

        private bool HasMediaFile(string key)
        {
            if (!mediaBoxes.ContainsKey(key))
                return false;

            string path = mediaBoxes[key].Text;
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        private bool HasReportImage()
        {
            return HasImageFile("SideBySideReportImagePath") ||
                HasImageFile("BeforeReportImagePath") ||
                HasImageFile("AfterReportImagePath") ||
                HasImageFile("MeasurementReferenceImagePath");
        }

        private bool HasAnalysisCaptureEvidence()
        {
            return CountAnalysisCaptureEvidenceFiles() > 0;
        }

        private int CountAnalysisCaptureEvidenceFiles()
        {
            if (currentSession == null)
                return 0;

            string folderPath = GetSessionAnalysisCapturesFolderPath();
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return 0;

            try
            {
                return Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        private void RefreshAnalysisCapturesStatus()
        {
            if (analysisCapturesStatus == null)
                return;

            int count = CountAnalysisCaptureEvidenceFiles();
            if (count > 0)
            {
                analysisCapturesStatus.Text = "Evidence status: " + count + " saved file" + (count == 1 ? "" : "s") + " found. Active folder: " + GetSessionAnalysisCapturesFolderPath();
                analysisCapturesStatus.ForeColor = Color.FromArgb(60, 145, 76);
            }
            else
            {
                analysisCapturesStatus.Text = "Evidence status: no saved files found yet. Save screenshots, exported frames, or clips here: " + GetSessionAnalysisCapturesFolderPath();
                analysisCapturesStatus.ForeColor = Color.FromArgb(170, 104, 36);
            }
        }

        private bool HasImageFile(string key)
        {
            if (!imageBoxes.ContainsKey(key))
                return false;

            string path = imageBoxes[key].Text;
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        private bool HasCoreBikeMetrics()
        {
            return HasMeasurement("SaddleHeightAfter") &&
                HasMeasurement("SaddleSetbackAfter") &&
                HasMeasurement("SaddleTipToGripReachAfter") &&
                HasMeasurement("HandlebarXAfter") &&
                HasMeasurement("HandlebarYAfter");
        }

        private bool HasMeasurement(string key)
        {
            return measurementBoxes.ContainsKey(key) && !string.IsNullOrWhiteSpace(measurementBoxes[key].Text);
        }

        private bool IsReportReady()
        {
            return HasMediaFile("BeforeVideoPath") &&
                HasMediaFile("AfterVideoPath") &&
                HasCoreBikeMetrics();
        }

        private void RefreshSessions(Guid selectId)
        {
            IList<FitSessionRecord> sessions = repository.LoadAll();
            sessionList.BeginUpdate();
            sessionList.Items.Clear();
            foreach (FitSessionRecord session in sessions)
            {
                ListViewItem item = new ListViewItem(new[] { session.DisplayName, session.Status ?? string.Empty });
                item.Tag = session;
                sessionList.Items.Add(item);
            }
            sessionList.EndUpdate();

            if (sessionList.Items.Count == 0)
            {
                BeginNewSession();
                return;
            }

            ListViewItem selected = null;
            foreach (ListViewItem item in sessionList.Items)
            {
                FitSessionRecord session = item.Tag as FitSessionRecord;
                if (session != null && session.Id == selectId)
                    selected = item;
            }
            (selected ?? sessionList.Items[0]).Selected = true;
        }

        private void SessionList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sessionList.SelectedItems.Count == 0)
                return;
            FitSessionRecord selected = sessionList.SelectedItems[0].Tag as FitSessionRecord;
            if (selected != null)
                LoadSession(selected);
        }

        private void BeginNewSession()
        {
            sessionList.SelectedItems.Clear();
            FitSessionRecord session = new FitSessionRecord();
            session.SessionDate = DateTime.Today;
            session.Title = "Bike Fit - " + DateTime.Today.ToString("MMM d, yyyy");
            session.Status = "Assessment";
            LoadSession(session);
            txtTitle.Focus();
            txtTitle.SelectAll();
        }

        private void LoadSession(FitSessionRecord session)
        {
            currentSession = session;
            txtTitle.Text = session.Title ?? string.Empty;
            dtpDate.Value = session.SessionDate == DateTime.MinValue ? DateTime.Today : session.SessionDate;
            cmbStatus.SelectedItem = string.IsNullOrEmpty(session.Status) ? "Assessment" : session.Status;
            if (cmbStatus.SelectedIndex < 0)
                cmbStatus.SelectedIndex = 0;
            txtGoals.Text = session.Goals ?? string.Empty;
            txtNotes.Text = session.Notes ?? string.Empty;
            txtFitSummaryMainGoal.Text = session.FitSummaryMainGoal ?? string.Empty;
            txtFitSummaryKeyFindings.Text = session.FitSummaryKeyFindings ?? string.Empty;
            txtFitSummaryChangesMade.Text = session.FitSummaryChangesMade ?? string.Empty;
            txtFitSummaryRecommendations.Text = session.FitSummaryRecommendations ?? string.Empty;
            txtFitSummaryFollowUp.Text = session.FitSummaryFollowUp ?? string.Empty;
            txtHandoffWhatToSend.Text = session.HandoffWhatToSend ?? string.Empty;
            txtHandoffClientMessage.Text = session.HandoffClientMessage ?? string.Empty;
            txtHandoffHomework.Text = session.HandoffHomework ?? string.Empty;
            txtHandoffNextAppointment.Text = session.HandoffNextAppointment ?? string.Empty;
            txtHandoffInternalNotes.Text = session.HandoffInternalNotes ?? string.Empty;
            chkShowBeforeMeasurementsInReport.Checked = !session.HideBeforeMeasurementsInReport;

            string beforePath = session.BeforeVideoPath;
            if (string.IsNullOrEmpty(beforePath))
                beforePath = string.IsNullOrEmpty(session.LeftVideoPath) ? session.SideVideoPath : session.LeftVideoPath;
            string afterPath = session.AfterVideoPath;
            if (string.IsNullOrEmpty(afterPath))
                afterPath = string.IsNullOrEmpty(session.RightVideoPath) ? session.FrontVideoPath : session.RightVideoPath;
            SetMedia("BeforeVideoPath", beforePath);
            SetMedia("AfterVideoPath", afterPath);
            SetImage("BeforeReportImagePath", session.BeforeReportImagePath);
            SetImage("AfterReportImagePath", session.AfterReportImagePath);
            SetImage("SideBySideReportImagePath", session.SideBySideReportImagePath);
            SetImage("MeasurementReferenceImagePath", session.MeasurementReferenceImagePath);
            chkShowSideBySideImageInReport.Checked = !session.HideSideBySideImageInReport;
            chkShowBeforeImageInReport.Checked = !session.HideBeforeImageInReport;
            chkShowAfterImageInReport.Checked = !session.HideAfterImageInReport;
            chkShowMeasurementReferenceImageInReport.Checked = !session.HideMeasurementReferenceImageInReport;
            chkShowMeasurementCaptureTraceInReport.Checked = !session.HideMeasurementCaptureTraceInReport;
            SetReportLogoStyle(session.ReportLogoStyle);

            SetMeasurement("SaddleHeightBefore", session.SaddleHeightBefore);
            SetMeasurement("SaddleHeightAfter", session.SaddleHeightAfter);
            SetMeasurement("SaddleSetbackBefore", session.SaddleSetbackBefore);
            SetMeasurement("SaddleSetbackAfter", session.SaddleSetbackAfter);
            SetMeasurement("HandlebarReachBefore", session.HandlebarReachBefore);
            SetMeasurement("HandlebarReachAfter", session.HandlebarReachAfter);
            SetMeasurement("HandlebarDropBefore", session.HandlebarDropBefore);
            SetMeasurement("HandlebarDropAfter", session.HandlebarDropAfter);
            SetMeasurement("SaddleTipToGripReachBefore", session.SaddleTipToGripReachBefore);
            SetMeasurement("SaddleTipToGripReachAfter", session.SaddleTipToGripReachAfter);
            SetMeasurement("HandlebarXBefore", session.HandlebarXBefore);
            SetMeasurement("HandlebarXAfter", session.HandlebarXAfter);
            SetMeasurement("HandlebarYBefore", session.HandlebarYBefore);
            SetMeasurement("HandlebarYAfter", session.HandlebarYAfter);
            SetMeasurement("CrankLengthBefore", session.CrankLengthBefore);
            SetMeasurement("CrankLengthAfter", session.CrankLengthAfter);
            SetMeasurement("WheelbaseBefore", session.WheelbaseBefore);
            SetMeasurement("WheelbaseAfter", session.WheelbaseAfter);
            SetMeasurement("CleatPositionBefore", session.CleatPositionBefore);
            SetMeasurement("CleatPositionAfter", session.CleatPositionAfter);
            SetMeasurement("KneeAngleBefore", session.KneeAngleBefore);
            SetMeasurement("KneeAngleAfter", session.KneeAngleAfter);
            SetMeasurement("HipAngleBefore", session.HipAngleBefore);
            SetMeasurement("HipAngleAfter", session.HipAngleAfter);
            SetMeasurement("AnkleAngleBefore", session.AnkleAngleBefore);
            SetMeasurement("AnkleAngleAfter", session.AnkleAngleAfter);
            SetMeasurement("TorsoAngleBefore", session.TorsoAngleBefore);
            SetMeasurement("TorsoAngleAfter", session.TorsoAngleAfter);
            SetMeasurement("ShoulderAngleBefore", session.ShoulderAngleBefore);
            SetMeasurement("ShoulderAngleAfter", session.ShoulderAngleAfter);

            RefreshFitTemplates(session.FitTemplateName);
            SelectFitProtocol(string.IsNullOrWhiteSpace(session.FitProtocolBikeType) ? session.FitTemplateBikeType : session.FitProtocolBikeType);
            RefreshCameraProfiles(session.CameraSetupProfileName);
            RefreshClientHistory();
            UpdateActiveSessionStatus();
            UpdateWorkflowChecklist();
            RefreshAnalysisCapturesStatus();
            RefreshRecordingFolderGuide();
            UpdateReportImageSaveTarget();
            UpdateVideoSaveTarget();
        }

        private void SetMedia(string key, string value)
        {
            mediaBoxes[key].Text = value ?? string.Empty;
            RefreshMediaStatus(key);
        }

        private void SetImage(string key, string value)
        {
            imageBoxes[key].Text = value ?? string.Empty;
        }

        private void SetMeasurement(string key, string value)
        {
            measurementBoxes[key].Text = value ?? string.Empty;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
                RefreshSessions(currentSession.Id);
                UpdateSaveHint("Saved \"" + currentSession.DisplayName + "\" to this client’s Measurements → Sessions folder.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The fit session could not be saved.\n\n" + exception.Message, "Bike Fit Workspace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReviewMetrics_Click(object sender, EventArgs e)
        {
            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();

            ReviewRequiredMetric(issues, "Saddle height", "SaddleHeight", "Use Guided Capture or Distance from BB center to saddle top. Confirm the value is entered in mm.");
            ReviewRequiredMetric(issues, "Saddle setback", "SaddleSetback", "Use horizontal distance from BB vertical line to saddle tip. Negative is OK when the saddle tip is behind the BB.");
            ReviewRequiredMetric(issues, "Saddle tip to grip reach", "SaddleTipToGripReach", "Use Distance or horizontal assist from saddle tip to grip/hood contact point.");
            ReviewRequiredMetric(issues, "Handlebar X", "HandlebarX", "Use horizontal distance from BB center to grip/hood contact point.");
            ReviewRequiredMetric(issues, "Handlebar Y", "HandlebarY", "Use vertical distance from BB center to grip/hood contact point. Recheck image level/calibration if this looks strange.");

            ReviewMetricRangeBothSides(warnings, "Saddle height", "SaddleHeight", 500, 900, "mm", "Recheck calibration and the BB → saddle top points.");
            ReviewMetricRangeBothSides(warnings, "Saddle setback", "SaddleSetback", -120, 60, "mm", "Behind BB should be negative; check the sign and saddle-tip point.");
            ReviewMetricRangeBothSides(warnings, "Saddle tip to grip reach", "SaddleTipToGripReach", 350, 750, "mm", "Confirm saddle tip and actual grip/hood contact point.");
            ReviewMetricRangeBothSides(warnings, "Handlebar X", "HandlebarX", 300, 700, "mm", "Confirm horizontal distance from BB to the grip/hood point.");
            ReviewMetricRangeBothSides(warnings, "Handlebar Y", "HandlebarY", -180, 180, "mm", "Confirm image level and vertical direction.");

            ReviewMetricRangeBothSides(warnings, "Knee angle", "KneeAngle", 90, 175, "degrees", "Confirm hip, knee, and ankle landmarks and use the intended crank position.");
            ReviewMetricRangeBothSides(warnings, "Hip angle", "HipAngle", 25, 150, "degrees", "Confirm shoulder, hip, and knee landmarks.");
            ReviewMetricRangeBothSides(warnings, "Ankle angle", "AnkleAngle", 55, 175, "degrees", "Confirm knee, ankle, and toe landmarks.");
            ReviewMetricRangeBothSides(warnings, "Body reach", "TorsoAngle", 20, 180, "degrees", "Confirm hip, shoulder, and hand-contact landmarks.");
            ReviewMetricRangeBothSides(warnings, "Back angle", "ShoulderAngle", 5, 85, "degrees", "Confirm hip and shoulder landmarks and image level.");

            ReviewMeasurementChange(warnings, "Saddle height", "SaddleHeight", 60, "mm");
            ReviewMeasurementChange(warnings, "Saddle setback", "SaddleSetback", 50, "mm");
            ReviewMeasurementChange(warnings, "Knee angle", "KneeAngle", 20, "°");
            ReviewMeasurementChange(warnings, "Hip angle", "HipAngle", 20, "°");
            ReviewMeasurementChange(warnings, "Back angle", "ShoulderAngle", 20, "°");
            ReviewMeasurementImageQuality(warnings);
            ReviewGuidedCaptureQuality(warnings);

            string message;
            MessageBoxIcon icon;
            if (issues.Count == 0 && warnings.Count == 0)
            {
                message = "Measurement quality check passed.\n\nThe key bike measurements and entered rider angles look consistent with the broad advisory checks.\n\nNext action: preview the report and confirm the values still match your professional judgment.";
                icon = MessageBoxIcon.Information;
            }
            else
            {
                message = "Measurement Quality Review\n\n";
                if (issues.Count > 0)
                    message += "Missing key values:\n- " + string.Join("\n- ", issues.ToArray()) + "\n\n";
                if (warnings.Count > 0)
                    message += "Values to double-check:\n- " + string.Join("\n- ", warnings.ToArray()) + "\n\n";
                message += "Next action: recheck the reference frame, calibration, landmarks, or manual entries as needed.\n\nThese checks are advisory. They do not diagnose the rider and never block saving or reporting.";
                icon = issues.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            }

            UpdateSaveHint(issues.Count == 0 && warnings.Count == 0 ? "Measurement quality review passed." : "Measurement quality review found items to check.");
            MessageBox.Show(this, message, "Measurement Quality Review", MessageBoxButtons.OK, icon);
        }

        private void ReviewMetricRangeBothSides(List<string> warnings, string label, string metricKey, double minimum, double maximum, string unit, string nextAction)
        {
            ReviewMetricRange(warnings, label + " Before", metricKey + "Before", minimum, maximum, unit, nextAction);
            ReviewMetricRange(warnings, label + " After", metricKey + "After", minimum, maximum, unit, nextAction);
        }

        private void ReviewMeasurementChange(List<string> warnings, string label, string metricKey, double maximumChange, string unit)
        {
            double before;
            double after;
            if (!TryParseMeasurementNumber(GetMeasurementText(metricKey + "Before"), out before) ||
                !TryParseMeasurementNumber(GetMeasurementText(metricKey + "After"), out after))
                return;

            double change = Math.Abs(after - before);
            if (change > maximumChange)
                warnings.Add(label + ": Before/After changed by " + change.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " " + unit + ". Confirm both measurements used the same reference method and frame position.");
        }

        private void ReviewMeasurementImageQuality(List<string> warnings)
        {
            string path = imageBoxes.ContainsKey("MeasurementReferenceImagePath") ? imageBoxes["MeasurementReferenceImagePath"].Text : string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                warnings.Add("Measurement reference image is missing. Add a clear side-view image if guided measurements were used.");
                return;
            }

            try
            {
                using (Image image = Image.FromFile(path))
                {
                    if (image.Width < 960 || image.Height < 540)
                        warnings.Add("Measurement reference image is only " + image.Width.ToString() + " × " + image.Height.ToString() + ". A sharper image may improve landmark placement.");
                }
            }
            catch
            {
                warnings.Add("Measurement reference image could not be checked. Re-open it before finalizing measurements.");
            }
        }

        private void ReviewGuidedCaptureQuality(List<string> warnings)
        {
            if (currentSession == null)
                return;

            if (!string.IsNullOrWhiteSpace(currentSession.BikeMetricsCaptureMethodBefore) &&
                !string.Equals(currentSession.BikeMetricsCameraSetupBefore, "Confirmed", StringComparison.OrdinalIgnoreCase))
                warnings.Add("Before guided bike capture did not confirm the camera setup checklist.");
            if (!string.IsNullOrWhiteSpace(currentSession.BikeMetricsCaptureMethodAfter) &&
                !string.Equals(currentSession.BikeMetricsCameraSetupAfter, "Confirmed", StringComparison.OrdinalIgnoreCase))
                warnings.Add("After guided bike capture did not confirm the camera setup checklist.");
            if (!string.IsNullOrWhiteSpace(currentSession.BikeMetricsCaptureMethodBefore) &&
                string.Equals(currentSession.BikeMetricsLevelReferenceBefore, "Not set", StringComparison.OrdinalIgnoreCase))
                warnings.Add("Before guided bike capture has no level reference. This is fine for a truly level image; otherwise recheck horizontal and vertical values.");
            if (!string.IsNullOrWhiteSpace(currentSession.BikeMetricsCaptureMethodAfter) &&
                string.Equals(currentSession.BikeMetricsLevelReferenceAfter, "Not set", StringComparison.OrdinalIgnoreCase))
                warnings.Add("After guided bike capture has no level reference. This is fine for a truly level image; otherwise recheck horizontal and vertical values.");
        }

        private void ReviewSession_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
                if (currentSession == null)
                {
                    UpdateSaveHint("Open or create a client fit session first, then check report readiness.");
                    MessageBox.Show(this, "Open or create a client fit session first so Cassette Motion Pro knows what to review.", "Review Session", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                List<string> ready = new List<string>();
                List<string> missing = new List<string>();
                List<string> optional = new List<string>();

                ReviewTextField(ready, missing, "Session title", currentSession.Title, "Add a clear session title on the Overview tab.");
                ReviewTextField(ready, optional, "Rider goals", currentSession.Goals, "Optional, but useful for the report story.");
                ReviewFileField(ready, missing, "Before video", currentSession.BeforeVideoPath, "Use Client Files → Add videos → Before Video.");
                ReviewFileField(ready, missing, "After video", currentSession.AfterVideoPath, "Use Client Files → Add videos → After Video.");
                ReviewFileField(ready, optional, "Side-by-side report image", currentSession.SideBySideReportImagePath, "Use Report Images or Client Files to add before/after photos, then combine them.");
                ReviewFileField(ready, optional, "Measurement reference image", currentSession.MeasurementReferenceImagePath, "Use Bike Metrics measurement reference image if you want the report to show it.");
                ReviewMetricReady(ready, missing, "Saddle height After", "SaddleHeightAfter", "Add the final saddle height on Bike Metrics.");
                ReviewMetricReady(ready, missing, "Saddle setback After", "SaddleSetbackAfter", "Add the final saddle setback on Bike Metrics. Behind BB should be negative.");
                ReviewMetricReady(ready, missing, "Saddle tip to grip reach After", "SaddleTipToGripReachAfter", "Add the final saddle tip to grip reach on Bike Metrics.");
                ReviewMetricReady(ready, missing, "Handlebar X After", "HandlebarXAfter", "Add final Handlebar X on Bike Metrics.");
                ReviewMetricReady(ready, missing, "Handlebar Y After", "HandlebarYAfter", "Add final Handlebar Y on Bike Metrics.");
                ReviewTextField(ready, optional, "Fit Summary", currentSession.FitSummaryMainGoal + currentSession.FitSummaryKeyFindings + currentSession.FitSummaryChangesMade + currentSession.FitSummaryRecommendations + currentSession.FitSummaryFollowUp, "Optional, but this makes the report feel much more professional.");

                string message = BuildSessionReviewMessage(ready, missing, optional);
                MessageBoxIcon icon = missing.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
                UpdateSaveHint(missing.Count == 0 ? "Session review passed. Ready to preview or generate." : "Session review found items to complete before the report.");
                MessageBox.Show(this, message, "Review Session", MessageBoxButtons.OK, icon);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The session review could not be completed.\n\n" + exception.Message, "Review Session", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReviewTextField(List<string> ready, List<string> missing, string label, string value, string nextAction)
        {
            if (string.IsNullOrWhiteSpace(value))
                missing.Add(label + ": missing. Next action: " + nextAction);
            else
                ready.Add(label);
        }

        private void ReviewFileField(List<string> ready, List<string> missing, string label, string path, string nextAction)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                missing.Add(label + ": missing. Next action: " + nextAction);
                return;
            }

            if (!File.Exists(path))
            {
                missing.Add(label + ": file path is saved, but the file was not found. Next action: re-add it from Client Files.");
                return;
            }

            ready.Add(label);
        }

        private void ReviewMetricReady(List<string> ready, List<string> missing, string label, string measurementKey, string nextAction)
        {
            string value = GetMeasurementText(measurementKey);
            if (string.IsNullOrWhiteSpace(value))
                missing.Add(label + ": missing. Next action: " + nextAction);
            else
                ready.Add(label + " = " + value);
        }

        private string BuildSessionReviewMessage(List<string> ready, List<string> missing, List<string> optional)
        {
            string message = missing.Count == 0 ? "Session Review: ready for report.\n\n" : "Session Review: a few things still need attention.\n\n";

            if (ready.Count > 0)
                message += "Ready:\n- " + string.Join("\n- ", ready.ToArray()) + "\n\n";

            if (missing.Count > 0)
                message += "Complete before final report:\n- " + string.Join("\n- ", missing.ToArray()) + "\n\n";

            if (optional.Count > 0)
                message += "Optional polish:\n- " + string.Join("\n- ", optional.ToArray()) + "\n\n";

            message += missing.Count == 0
                ? "Next action: Preview the report, then Generate, Package, or Zip it."
                : "Next action: fill the missing items, then press Review again.";

            return message;
        }

        private bool ConfirmReportReadinessBeforeOutput(string actionName)
        {
            List<string> warnings = GetReportReadinessWarnings();
            if (warnings.Count == 0)
                return true;

            string message = actionName + " can continue, but this fit session is missing a few items.\n\n" +
                "Items to check:\n- " + string.Join("\n- ", warnings.ToArray()) + "\n\n" +
                "Choose Yes to continue anyway, or No to go back and finish the session.";
            DialogResult result = MessageBox.Show(this, message, "Report readiness check", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                UpdateSaveHint("Report paused. Complete the readiness items, then try again.");
                return false;
            }

            UpdateSaveHint(actionName + " continued with readiness items still needing review.");
            return true;
        }

        private List<string> GetReportReadinessWarnings()
        {
            List<string> warnings = new List<string>();

            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
            {
                warnings.Add("Open or create a client fit session first.");
                return warnings;
            }

            AddReportFileWarning(warnings, "Before video", currentSession.BeforeVideoPath, "save a Before video from Video Studio.");
            AddReportFileWarning(warnings, "After video", currentSession.AfterVideoPath, "save an After video from Video Studio.");

            if (!HasAnalysisCaptureEvidence() && !HasSavedSessionEvidence())
                warnings.Add("Save at least one useful Before, After, or Dual evidence file.");

            if (!HasReportImage())
                warnings.Add("Choose or save a report image.");

            AddReportMetricWarning(warnings, "Saddle height After", "SaddleHeightAfter");
            AddReportMetricWarning(warnings, "Saddle setback After", "SaddleSetbackAfter");
            AddReportMetricWarning(warnings, "Saddle tip to grip reach After", "SaddleTipToGripReachAfter");
            AddReportMetricWarning(warnings, "Handlebar X After", "HandlebarXAfter");
            AddReportMetricWarning(warnings, "Handlebar Y After", "HandlebarYAfter");

            return warnings;
        }

        private static void AddReportFileWarning(List<string> warnings, string label, string path, string nextAction)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                warnings.Add(label + " is missing - " + nextAction);
                return;
            }

            if (!File.Exists(path))
                warnings.Add(label + " is selected, but the file was not found - re-add it from Client Files.");
        }

        private void AddReportMetricWarning(List<string> warnings, string label, string metricKey)
        {
            if (string.IsNullOrWhiteSpace(GetMeasurementText(metricKey)))
                warnings.Add(label + " is missing on Bike Metrics.");
        }

        private void ReviewRequiredMetric(List<string> issues, string label, string metricKey, string nextAction)
        {
            string before = GetMeasurementText(metricKey + "Before");
            string after = GetMeasurementText(metricKey + "After");

            if (string.IsNullOrWhiteSpace(before) && string.IsNullOrWhiteSpace(after))
            {
                issues.Add(label + ": Before and After are empty. Next action: " + nextAction);
                return;
            }

            if (string.IsNullOrWhiteSpace(after))
                issues.Add(label + ": After is empty. Next action: enter final/After value before reporting.");

            if (string.IsNullOrWhiteSpace(before))
                issues.Add(label + ": Before is empty. This is OK for final-only reports, but fill it in if you want Before / After comparison.");
        }

        private void ReviewMetricRange(List<string> warnings, string label, string measurementKey, double minimum, double maximum, string unit, string nextAction)
        {
            string value = GetMeasurementText(measurementKey);
            if (string.IsNullOrWhiteSpace(value))
                return;

            double parsed;
            if (!TryParseMeasurementNumber(value, out parsed))
            {
                warnings.Add(label + ": could not be read as a number from \"" + value + "\". Next action: enter like 742 mm or -35 mm.");
                return;
            }

            if (parsed < minimum || parsed > maximum)
                warnings.Add(label + ": " + value + " is outside the broad review range of " + minimum.ToString("0") + " to " + maximum.ToString("0") + " " + unit + ". Next action: " + nextAction);
        }

        private string GetMeasurementText(string key)
        {
            if (!measurementBoxes.ContainsKey(key))
                return string.Empty;

            return measurementBoxes[key].Text.Trim();
        }

        private static bool TryParseMeasurementNumber(string value, out double number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim();
            int index = 0;
            while (index < text.Length && (char.IsDigit(text[index]) || text[index] == '-' || text[index] == '+' || text[index] == '.'))
                index++;

            if (index == 0)
                return false;

            return double.TryParse(text.Substring(0, index), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number);
        }

        private void GenerateReport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
                if (!ConfirmReportReadinessBeforeOutput("Generate report"))
                    return;

                string reportPath = FitSessionReportGenerator.Generate(client, currentSession);
                UpdateSaveHint("Report saved to this session’s Reports folder.");
                MessageBox.Show(this,
                    "The report was saved in this fit session’s Reports folder.\n\n" + reportPath,
                    "Report created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The report could not be created.\n\n" + exception.Message, "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PreviewReport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
                if (!ConfirmReportReadinessBeforeOutput("Preview report"))
                    return;

                string reportPath = FitSessionReportGenerator.Generate(client, currentSession);
                Process.Start(reportPath);
                UpdateSaveHint("Report preview opened. Use Print / Save PDF after reviewing it.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The report preview could not be opened.\n\n" + exception.Message, "Report Preview", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReportPackage_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
                if (!ConfirmReportReadinessBeforeOutput("Package report"))
                    return;

                string packageFolder = FitSessionReportGenerator.GeneratePackage(client, currentSession);
                Process.Start(packageFolder);
                UpdateSaveHint("Report package created and opened.");
                MessageBox.Show(this,
                    "The report package was created in this fit session’s Reports folder.\n\n" +
                    packageFolder + "\n\n" +
                    "It includes the report HTML and copied report images.",
                    "Report package created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The report package could not be created.\n\n" + exception.Message, "Report Package", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ZipReportPackage_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
                if (!ConfirmReportReadinessBeforeOutput("Zip report package"))
                    return;

                string zipPath = FitSessionReportGenerator.GeneratePackageZip(client, currentSession);
                Process.Start(Path.GetDirectoryName(zipPath));
                UpdateSaveHint("Zipped report package created in this session’s Reports folder.");
                MessageBox.Show(this,
                    "The zipped report package was created in this fit session’s Reports folder.\n\n" +
                    zipPath,
                    "Zipped report package created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The zipped report package could not be created.\n\n" + exception.Message, "Zip Report Package", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenReports_Click(object sender, EventArgs e)
        {
            string folderPath = HasActiveFitSession() ? GetSessionReportsFolderPath() : client.ReportsPath;
            OpenClientFolder(folderPath, HasActiveFitSession() ? "Active session reports" : "Reports");
        }

        private void OpenClientFolder(string folderPath, string folderName)
        {
            try
            {
                Directory.CreateDirectory(folderPath);
                Process.Start(folderPath);
                UpdateSaveHint(folderName + " folder opened.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The " + folderName + " folder could not be opened.\n\n" + exception.Message, folderName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BikeFitWorkspaceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (currentSession == null)
            {
                ReportImageSaveTarget.ReportImageSaved -= ReportImageSaveTarget_ReportImageSaved;
                VideoSaveTarget.VideoSaved -= VideoSaveTarget_VideoSaved;
                return;
            }

            try
            {
                SaveCurrentSession();
                ReportImageSaveTarget.ReportImageSaved -= ReportImageSaveTarget_ReportImageSaved;
                VideoSaveTarget.VideoSaved -= VideoSaveTarget_VideoSaved;
            }
            catch (Exception exception)
            {
                e.Cancel = true;
                MessageBox.Show(this, "The fit session could not be saved.\n\n" + exception.Message, "Bike Fit Workspace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReportImageSaveTarget_ReportImageSaved(string slot, string path)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(ReportImageSaveTarget_ReportImageSaved), slot, path);
                return;
            }

            string key;
            if (string.Equals(slot, "After", StringComparison.OrdinalIgnoreCase))
                key = "AfterReportImagePath";
            else if (string.Equals(slot, "Dual", StringComparison.OrdinalIgnoreCase))
                key = "SideBySideReportImagePath";
            else
                key = "BeforeReportImagePath";

            if (!imageBoxes.ContainsKey(key))
                return;

            imageBoxes[key].Text = path;
            SaveCurrentSession();
            UpdateWorkflowChecklist();
            UpdateFitCommandCenterStatus();
            UpdateSaveHint(slot + " report image saved to this client fit session: " + Path.GetFileName(path));
        }

        private void VideoSaveTarget_VideoSaved(string slot, string path)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(VideoSaveTarget_VideoSaved), slot, path);
                return;
            }

            if (string.Equals(slot, "Dual", StringComparison.OrdinalIgnoreCase))
            {
                SaveCurrentSession();
                UpdateWorkflowChecklist();
                UpdateFitCommandCenterStatus();
                UpdateSaveHint("Dual video saved to this client fit session: " + Path.GetFileName(path));
                return;
            }

            string key = string.Equals(slot, "After", StringComparison.OrdinalIgnoreCase) ? "AfterVideoPath" : "BeforeVideoPath";
            if (!mediaBoxes.ContainsKey(key))
                return;

            mediaBoxes[key].Text = path;
            SaveCurrentSession();
            RefreshMediaStatus(key);
            UpdateWorkflowChecklist();
            UpdateFitCommandCenterStatus();
            UpdateSaveHint(slot + " video saved to this client fit session: " + Path.GetFileName(path));
        }

        private void SaveCurrentSession()
        {
            if (currentSession == null)
                currentSession = new FitSessionRecord();

            string title = txtTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
                title = "Bike Fit - " + dtpDate.Value.ToString("MMM d, yyyy");

            currentSession.Title = title;
            currentSession.SessionDate = dtpDate.Value.Date;
            currentSession.Status = Convert.ToString(cmbStatus.SelectedItem);
            currentSession.Goals = txtGoals.Text.Trim();
            currentSession.Notes = txtNotes.Text.Trim();
            currentSession.FitSummaryMainGoal = txtFitSummaryMainGoal.Text.Trim();
            currentSession.FitSummaryKeyFindings = txtFitSummaryKeyFindings.Text.Trim();
            currentSession.FitSummaryChangesMade = txtFitSummaryChangesMade.Text.Trim();
            currentSession.FitSummaryRecommendations = txtFitSummaryRecommendations.Text.Trim();
            currentSession.FitSummaryFollowUp = txtFitSummaryFollowUp.Text.Trim();
            currentSession.HandoffWhatToSend = txtHandoffWhatToSend.Text.Trim();
            currentSession.HandoffClientMessage = txtHandoffClientMessage.Text.Trim();
            currentSession.HandoffHomework = txtHandoffHomework.Text.Trim();
            currentSession.HandoffNextAppointment = txtHandoffNextAppointment.Text.Trim();
            currentSession.HandoffInternalNotes = txtHandoffInternalNotes.Text.Trim();
            currentSession.BeforeVideoPath = mediaBoxes["BeforeVideoPath"].Text;
            currentSession.AfterVideoPath = mediaBoxes["AfterVideoPath"].Text;
            currentSession.BeforeReportImagePath = imageBoxes["BeforeReportImagePath"].Text;
            currentSession.AfterReportImagePath = imageBoxes["AfterReportImagePath"].Text;
            currentSession.SideBySideReportImagePath = imageBoxes["SideBySideReportImagePath"].Text;
            currentSession.MeasurementReferenceImagePath = imageBoxes["MeasurementReferenceImagePath"].Text;
            currentSession.HideBeforeMeasurementsInReport = !chkShowBeforeMeasurementsInReport.Checked;
            currentSession.HideSideBySideImageInReport = !chkShowSideBySideImageInReport.Checked;
            currentSession.HideBeforeImageInReport = !chkShowBeforeImageInReport.Checked;
            currentSession.HideAfterImageInReport = !chkShowAfterImageInReport.Checked;
            currentSession.HideMeasurementReferenceImageInReport = !chkShowMeasurementReferenceImageInReport.Checked;
            currentSession.HideMeasurementCaptureTraceInReport = !chkShowMeasurementCaptureTraceInReport.Checked;
            currentSession.ReportLogoStyle = GetReportLogoStyle();
            currentSession.SaddleHeightBefore = measurementBoxes["SaddleHeightBefore"].Text.Trim();
            currentSession.SaddleHeightAfter = measurementBoxes["SaddleHeightAfter"].Text.Trim();
            currentSession.SaddleSetbackBefore = measurementBoxes["SaddleSetbackBefore"].Text.Trim();
            currentSession.SaddleSetbackAfter = measurementBoxes["SaddleSetbackAfter"].Text.Trim();
            currentSession.HandlebarReachBefore = measurementBoxes["HandlebarReachBefore"].Text.Trim();
            currentSession.HandlebarReachAfter = measurementBoxes["HandlebarReachAfter"].Text.Trim();
            currentSession.HandlebarDropBefore = measurementBoxes["HandlebarDropBefore"].Text.Trim();
            currentSession.HandlebarDropAfter = measurementBoxes["HandlebarDropAfter"].Text.Trim();
            currentSession.SaddleTipToGripReachBefore = measurementBoxes["SaddleTipToGripReachBefore"].Text.Trim();
            currentSession.SaddleTipToGripReachAfter = measurementBoxes["SaddleTipToGripReachAfter"].Text.Trim();
            currentSession.HandlebarXBefore = measurementBoxes["HandlebarXBefore"].Text.Trim();
            currentSession.HandlebarXAfter = measurementBoxes["HandlebarXAfter"].Text.Trim();
            currentSession.HandlebarYBefore = measurementBoxes["HandlebarYBefore"].Text.Trim();
            currentSession.HandlebarYAfter = measurementBoxes["HandlebarYAfter"].Text.Trim();
            currentSession.CrankLengthBefore = measurementBoxes["CrankLengthBefore"].Text.Trim();
            currentSession.CrankLengthAfter = measurementBoxes["CrankLengthAfter"].Text.Trim();
            currentSession.WheelbaseBefore = measurementBoxes["WheelbaseBefore"].Text.Trim();
            currentSession.WheelbaseAfter = measurementBoxes["WheelbaseAfter"].Text.Trim();
            currentSession.CleatPositionBefore = measurementBoxes["CleatPositionBefore"].Text.Trim();
            currentSession.CleatPositionAfter = measurementBoxes["CleatPositionAfter"].Text.Trim();
            currentSession.KneeAngleBefore = measurementBoxes["KneeAngleBefore"].Text.Trim();
            currentSession.KneeAngleAfter = measurementBoxes["KneeAngleAfter"].Text.Trim();
            currentSession.HipAngleBefore = measurementBoxes["HipAngleBefore"].Text.Trim();
            currentSession.HipAngleAfter = measurementBoxes["HipAngleAfter"].Text.Trim();
            currentSession.AnkleAngleBefore = measurementBoxes["AnkleAngleBefore"].Text.Trim();
            currentSession.AnkleAngleAfter = measurementBoxes["AnkleAngleAfter"].Text.Trim();
            currentSession.TorsoAngleBefore = measurementBoxes["TorsoAngleBefore"].Text.Trim();
            currentSession.TorsoAngleAfter = measurementBoxes["TorsoAngleAfter"].Text.Trim();
            currentSession.ShoulderAngleBefore = measurementBoxes["ShoulderAngleBefore"].Text.Trim();
            currentSession.ShoulderAngleAfter = measurementBoxes["ShoulderAngleAfter"].Text.Trim();
            repository.Save(currentSession);
            UpdateActiveSessionStatus();
            UpdateWorkflowChecklist();
            RefreshRecordingFolderGuide();
        }

        private string GetReportLogoStyle()
        {
            if (cmbReportLogoStyle.SelectedIndex == 1)
                return "CM";
            if (cmbReportLogoStyle.SelectedIndex == 2)
                return "None";
            return "Full";
        }

        private void SetReportLogoStyle(string value)
        {
            if (string.Equals(value, "CM", StringComparison.OrdinalIgnoreCase))
                cmbReportLogoStyle.SelectedIndex = 1;
            else if (string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
                cmbReportLogoStyle.SelectedIndex = 2;
            else
                cmbReportLogoStyle.SelectedIndex = 0;
        }

        private void UpdateSaveHint(string message)
        {
            if (saveHint == null)
                return;
            saveHint.Text = message;
        }

        private void UpdateActiveSessionStatus()
        {
            if (activeSessionStatus == null)
                return;

            if (currentSession == null)
            {
                ReportImageSaveTarget.Clear();
                VideoSaveTarget.Clear();
                activeSessionStatus.Text = "Active session\nChoose or create a fit session";
                UpdateFitCommandCenterStatus();
                return;
            }

            UpdateReportImageSaveTarget();
            UpdateVideoSaveTarget();

            string status = string.IsNullOrWhiteSpace(currentSession.Status) ? "Assessment" : currentSession.Status.Trim();
            string folder = currentSession.Id == Guid.Empty ? "pending until saved" : currentSession.StorageFolderName;
            activeSessionStatus.Text = "Active session: " + currentSession.DisplayName + " · " + status + "\n" +
                "Client: " + client.DisplayName + "\n" +
                "Session record: Measurements → Sessions → " + folder;
            UpdateFitCommandCenterStatus();
        }

        private void UpdateReportImageSaveTarget()
        {
            if (currentSession == null)
            {
                ReportImageSaveTarget.Clear();
                return;
            }

            try
            {
                string reportImagesFolder = GetSessionReportImagesFolderPath();
                Directory.CreateDirectory(Path.Combine(reportImagesFolder, "Before"));
                Directory.CreateDirectory(Path.Combine(reportImagesFolder, "After"));
                Directory.CreateDirectory(Path.Combine(reportImagesFolder, "Dual"));
                ReportImageSaveTarget.SetActiveFolder(reportImagesFolder);
            }
            catch
            {
                ReportImageSaveTarget.Clear();
            }
        }

        private void UpdateVideoSaveTarget()
        {
            if (currentSession == null)
            {
                VideoSaveTarget.Clear();
                return;
            }

            try
            {
                string beforeFolder = GetSessionVideoViewFolderPath("Before");
                string afterFolder = GetSessionVideoViewFolderPath("After");
                string dualFolder = GetSessionVideoViewFolderPath("Dual");
                Directory.CreateDirectory(beforeFolder);
                Directory.CreateDirectory(afterFolder);
                Directory.CreateDirectory(dualFolder);
                VideoSaveTarget.SetActiveFolders(beforeFolder, afterFolder, dualFolder);
            }
            catch
            {
                VideoSaveTarget.Clear();
            }
        }

        private void StartBodyAngleGuide(string mediaKey)
        {
            string path = mediaBoxes[mediaKey].Text;
            if (!ValidateVideo(path))
                return;

            SaveCurrentSession();
            MessageBox.Show(this,
                "The Bike Fit Angles tool will be active when the video opens.\n\n" +
                "1. Pause at the measurement frame.\n" +
                "2. Click the rider to place the overlay.\n" +
                "3. Drag each marker onto the matching body landmark.\n" +
                "4. Record knee, hip, ankle, body reach, and back angle in the Body Angles tab.",
                "Bike Fit Angle Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            if (openBodyAngleGuide != null)
                openBodyAngleGuide(path);
        }

        private void ShowBikeMetricAssist(string measurementName, string instructions, string metricKey)
        {
            string referencePath = imageBoxes.ContainsKey("MeasurementReferenceImagePath") ? imageBoxes["MeasurementReferenceImagePath"].Text : string.Empty;
            if (string.IsNullOrEmpty(referencePath) || !File.Exists(referencePath))
            {
                MessageBox.Show(this,
                    "Choose a Measurement image first.\n\n" +
                    "Use Browse, Use Before, Use After, Use Side-by-side, or Combine B+A at the top of Bike Metrics.",
                    "Bike Metrics Assist",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ImageMeasurementAssistantForm.DefaultMeasurementAxis defaultAxis = GetDefaultBikeMetricAxis(metricKey);
            using (ImageMeasurementAssistantForm form = new ImageMeasurementAssistantForm(referencePath, measurementName, instructions, defaultAxis))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                string measurementKey = metricKey + form.ResultSide;
                if (!measurementBoxes.ContainsKey(measurementKey))
                    return;

                measurementBoxes[measurementKey].Text = form.ResultValue;
                SaveCurrentSession();
                UpdateSaveHint(measurementName + " " + form.ResultSide.ToLowerInvariant() + " measurement saved from the image assistant.");
            }
        }

        private void ShowGuidedBikeMetricCapture()
        {
            string referencePath = imageBoxes.ContainsKey("MeasurementReferenceImagePath") ? imageBoxes["MeasurementReferenceImagePath"].Text : string.Empty;
            if (string.IsNullOrEmpty(referencePath) || !File.Exists(referencePath))
            {
                MessageBox.Show(this,
                    "Choose a Measurement image first.\n\n" +
                    "Use Browse, Use Before, Use After, Use Side-by-side, or Combine B+A at the top of Bike Metrics.",
                    "Guided Bike Metric Capture",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using (BikeMetricGuidedCaptureForm form = new BikeMetricGuidedCaptureForm(referencePath))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                foreach (KeyValuePair<string, string> metric in form.ResultValues)
                {
                    string measurementKey = metric.Key + form.ResultSide;
                    if (measurementBoxes.ContainsKey(measurementKey))
                        measurementBoxes[measurementKey].Text = metric.Value;
                }

                ApplyGuidedCaptureTrace(form);

                SaveCurrentSession();
                UpdateSaveHint("Guided Bike Metrics saved to " + form.ResultSide.ToLowerInvariant() + ".");
            }
        }

        private void ApplyGuidedCaptureTrace(BikeMetricGuidedCaptureForm form)
        {
            if (currentSession == null || form == null)
                return;

            if (string.Equals(form.ResultSide, "Before", StringComparison.OrdinalIgnoreCase))
            {
                currentSession.BikeMetricsCaptureMethodBefore = form.CaptureMethod;
                currentSession.BikeMetricsLevelReferenceBefore = form.LevelReferenceStatus;
                currentSession.BikeMetricsSetbackConventionBefore = form.SaddleSetbackConvention;
                currentSession.BikeMetricsCameraSetupBefore = form.CameraSetupStatus;
                return;
            }

            currentSession.BikeMetricsCaptureMethodAfter = form.CaptureMethod;
            currentSession.BikeMetricsLevelReferenceAfter = form.LevelReferenceStatus;
            currentSession.BikeMetricsSetbackConventionAfter = form.SaddleSetbackConvention;
            currentSession.BikeMetricsCameraSetupAfter = form.CameraSetupStatus;
        }

        private ImageMeasurementAssistantForm.DefaultMeasurementAxis GetDefaultBikeMetricAxis(string metricKey)
        {
            if (string.Equals(metricKey, "SaddleSetback", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(metricKey, "SaddleTipToGripReach", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(metricKey, "HandlebarX", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(metricKey, "Wheelbase", StringComparison.OrdinalIgnoreCase))
            {
                return ImageMeasurementAssistantForm.DefaultMeasurementAxis.Horizontal;
            }

            if (string.Equals(metricKey, "HandlebarY", StringComparison.OrdinalIgnoreCase))
                return ImageMeasurementAssistantForm.DefaultMeasurementAxis.Vertical;

            return ImageMeasurementAssistantForm.DefaultMeasurementAxis.Free;
        }

        private void UseMeasurementReferenceImage(string sourceKey, string label)
        {
            if (!imageBoxes.ContainsKey(sourceKey) || string.IsNullOrEmpty(imageBoxes[sourceKey].Text))
            {
                MessageBox.Show(this, "Choose a " + label.ToLowerInvariant() + " first.", "Measurement image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            imageBoxes["MeasurementReferenceImagePath"].Text = imageBoxes[sourceKey].Text;
            SaveCurrentSession();
            UpdateSaveHint("Measurement image set from " + label + ".");
        }

        private void CombineBeforeAfterImages(bool useAsMeasurementReference)
        {
            string beforePath = imageBoxes.ContainsKey("BeforeReportImagePath") ? imageBoxes["BeforeReportImagePath"].Text : string.Empty;
            string afterPath = imageBoxes.ContainsKey("AfterReportImagePath") ? imageBoxes["AfterReportImagePath"].Text : string.Empty;

            if (string.IsNullOrEmpty(beforePath) || !File.Exists(beforePath))
            {
                MessageBox.Show(this, "Choose a before image first.", "Combine Before + After", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(afterPath) || !File.Exists(afterPath))
            {
                MessageBox.Show(this, "Choose an after image first.", "Combine Before + After", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Cursor previousCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    SaveCurrentSession();
                    string combinedPath = CreateBeforeAfterCombinedImage(beforePath, afterPath);
                    imageBoxes["SideBySideReportImagePath"].Text = combinedPath;

                    if (useAsMeasurementReference)
                        imageBoxes["MeasurementReferenceImagePath"].Text = combinedPath;

                    SaveCurrentSession();
                    UpdateSaveHint("Before + after image combined and saved to this session’s Side-by-Side folder.");
                }
                finally
                {
                    Cursor.Current = previousCursor;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The before and after images could not be combined.\n\n" + exception.Message, "Combine Before + After", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseVideo(string key)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                string viewName = GetVideoViewName(key);
                dialog.Title = "Import " + viewName.ToLowerInvariant() + " video";
                dialog.Filter = "Video files|*.mp4;*.mov;*.avi;*.mkv;*.m4v;*.mpg;*.mpeg;*.wmv|All files|*.*";
                dialog.RestoreDirectory = true;
                string sessionViewFolder = GetSessionVideoViewFolderPath(viewName);
                if (Directory.Exists(sessionViewFolder))
                    dialog.InitialDirectory = sessionViewFolder;
                else if (Directory.Exists(client.VideosPath))
                    dialog.InitialDirectory = client.VideosPath;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        Cursor previousCursor = Cursor.Current;
                        Cursor.Current = Cursors.WaitCursor;
                        try
                        {
                            SaveCurrentSession();
                            SetMedia(key, ImportVideo(dialog.FileName, viewName));
                            SaveCurrentSession();
                            UpdateSaveHint(viewName + " video copied into this active fit session: " + FormatLatestVideoSelection(mediaBoxes[key].Text));
                        }
                        finally
                        {
                            Cursor.Current = previousCursor;
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(this, "The video could not be imported into the client folder.\n\n" + exception.Message, "Video import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenLiveCaptureForVideo(string key)
        {
            if (!RequireActiveFitSessionBeforeKinovea("Record Live"))
                return;

            if (openDualLiveCaptureFolders != null && (key == "BeforeVideoPath" || key == "AfterVideoPath"))
            {
                OpenDualLiveCapture();
                return;
            }

            if (openLiveCaptureFolder == null)
            {
                MessageBox.Show(this, "Live capture is not available from this workspace yet.", "Live capture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string viewName = GetVideoViewName(key);
                SaveCurrentSession();
                Directory.CreateDirectory(GetSessionVideosFolderPath());
                string destinationDirectory = GetSessionVideoViewFolderPath(viewName);
                Directory.CreateDirectory(destinationDirectory);
                WriteCaptureFolderHint(destinationDirectory, viewName);
                SetFitCommandCenterMode("Record Live: " + viewName);
                UpdateSaveHint(viewName + " live recording folder is now active for this client. Record in Video Studio, save there, then return here and click Analyze Latest Before + After.");

                Close();
                openLiveCaptureFolder(destinationDirectory);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Live capture could not be opened for this session.\n\n" + exception.Message, "Live capture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenDualLiveCapture()
        {
            if (!RequireActiveFitSessionBeforeKinovea("Dual Live Capture"))
                return;

            if (openDualLiveCaptureFolders == null && openLiveCaptureFolder == null)
            {
                MessageBox.Show(this, "Live capture is not available from this workspace yet.", "Dual Live Capture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                SaveCurrentSession();
                Directory.CreateDirectory(GetSessionVideosFolderPath());

                string beforeDirectory = GetSessionVideoViewFolderPath("Before");
                string afterDirectory = GetSessionVideoViewFolderPath("After");
                Directory.CreateDirectory(beforeDirectory);
                Directory.CreateDirectory(afterDirectory);
                WriteCaptureFolderHint(beforeDirectory, "Before");
                WriteCaptureFolderHint(afterDirectory, "After");
                WriteCameraProfileHint(beforeDirectory);
                WriteCameraProfileHint(afterDirectory);
                SetFitCommandCenterMode("Record Live: Before + After");
                UpdateSaveHint("Dual live capture opened. Video Studio is pointed at this client's Before/After video folders. Record, save, then return here and click Analyze Latest Before + After.");

                Close();
                if (openDualLiveCaptureFolders != null)
                {
                    openDualLiveCaptureFolders(beforeDirectory, afterDirectory);
                }
                else
                {
                    openLiveCaptureFolder(beforeDirectory);
                    openLiveCaptureFolder(afterDirectory);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Dual live capture could not be opened for this session.\n\n" + exception.Message, "Dual Live Capture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WriteCameraProfileHint(string directory)
        {
            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.CameraSetupProfileName))
                return;
            string content = "Cassette Motion Pro camera profile: " + currentSession.CameraSetupProfileName + Environment.NewLine +
                "Left screen: " + (currentSession.CameraSetupLeftRole ?? string.Empty) + Environment.NewLine +
                "Right screen: " + (currentSession.CameraSetupRightRole ?? string.Empty) + Environment.NewLine +
                "The client Before/After destination is controlled automatically by the fit session.";
            File.WriteAllText(Path.Combine(directory, "Camera Setup.txt"), content);
        }

        private void UseLatestVideo(string key)
        {
            try
            {
                string viewName = GetVideoViewName(key);
                string latestVideoPath = PrepareVideoViewFolderAndFindLatest(viewName);
                if (string.IsNullOrEmpty(latestVideoPath))
                {
                    MessageBox.Show(this, "No saved video files were found in this session’s " + viewName + " folder yet.\n\nClick Record Live, record and save the clip, then return to this same fit session and click Use Latest.", "Use Latest Video", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SetMedia(key, latestVideoPath);
                SaveCurrentSession();
                SetFitCommandCenterMode("Use Latest: " + viewName);
                UpdateSaveHint(viewName + " video set to latest: " + FormatLatestVideoSelection(latestVideoPath));
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The newest video could not be selected for this session.\n\n" + exception.Message, "Use Latest Video", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UseLatestBothVideos()
        {
            try
            {
                string beforePath = PrepareVideoViewFolderAndFindLatest("Before");
                string afterPath = PrepareVideoViewFolderAndFindLatest("After");

                List<string> missing = new List<string>();
                if (string.IsNullOrEmpty(beforePath))
                    missing.Add("Before");
                if (string.IsNullOrEmpty(afterPath))
                    missing.Add("After");

                if (missing.Count > 0)
                {
                    MessageBox.Show(this, "No saved video files were found in the " + string.Join(" and ", missing.ToArray()) + " folder yet.\n\nUse Dual Live Capture or Record Live, record and save the clip(s), then return to this same fit session and click Analyze Latest Before + After.", "Analyze Latest Before + After", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SetMedia("BeforeVideoPath", beforePath);
                SetMedia("AfterVideoPath", afterPath);
                SaveCurrentSession();
                SetFitCommandCenterMode("Analyze Latest: Before + After");
                string beforeSummary = FormatLatestVideoSelection(beforePath);
                string afterSummary = FormatLatestVideoSelection(afterPath);
                UpdateSaveHint("Latest selected — Before: " + beforeSummary + " | After: " + afterSummary + ". Opening dual playback analysis.");
                OpenPair("BeforeVideoPath", "AfterVideoPath");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The newest Before/After videos could not be selected for this session.\n\n" + exception.Message, "Analyze Latest Before + After", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string PrepareVideoViewFolderAndFindLatest(string viewName)
        {
            SaveCurrentSession();
            Directory.CreateDirectory(GetSessionVideosFolderPath());
            string destinationDirectory = GetSessionVideoViewFolderPath(viewName);
            Directory.CreateDirectory(destinationDirectory);
            WriteCaptureFolderHint(destinationDirectory, viewName);
            return FindLatestVideoFile(destinationDirectory);
        }

        private static string FindLatestVideoFile(string folder)
        {
            string[] patterns = new string[] { "*.mp4", "*.mov", "*.avi", "*.mkv", "*.m4v", "*.mpg", "*.mpeg", "*.wmv" };
            string latestPath = null;
            DateTime latestWriteTime = DateTime.MinValue;

            foreach (string pattern in patterns)
            {
                string[] files = new string[0];
                try
                {
                    files = Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly);
                }
                catch
                {
                    continue;
                }

                foreach (string file in files)
                {
                    DateTime writeTime = File.GetLastWriteTime(file);
                    if (latestPath == null || writeTime > latestWriteTime)
                    {
                        latestPath = file;
                        latestWriteTime = writeTime;
                    }
                }
            }

            return latestPath;
        }

        private static string FormatLatestVideoSelection(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "none";

            string fileName = Path.GetFileName(path);
            try
            {
                DateTime writeTime = File.GetLastWriteTime(path);
                return fileName + " (" + writeTime.ToString("MMM d, h:mm tt") + ")";
            }
            catch
            {
                return fileName;
            }
        }

        private void RefreshMediaStatus(string key)
        {
            Label status;
            if (!mediaStatusLabels.TryGetValue(key, out status))
                return;

            string path = mediaBoxes.ContainsKey(key) ? mediaBoxes[key].Text : string.Empty;
            string viewName = GetVideoViewName(key);

            if (string.IsNullOrEmpty(path))
            {
                status.Text = "No " + viewName.ToLowerInvariant() + " video selected yet.";
                return;
            }

            if (!File.Exists(path))
            {
                status.Text = viewName + " selected, but file is missing: " + Path.GetFileName(path);
                return;
            }

            status.Text = viewName + " selected: " + FormatLatestVideoSelection(path);
        }

        private static void WriteCaptureFolderHint(string folder, string viewName)
        {
            string hintPath = Path.Combine(folder, "README - Record Live Here.txt");
            if (File.Exists(hintPath))
                return;

            string contents =
                "Cassette Motion Pro live recording folder" + Environment.NewLine +
                Environment.NewLine +
                "This is the " + viewName + " video folder for the active fit session." + Environment.NewLine +
                "Record live capture clips here, then return to the Bike Fit Workspace and click Analyze Latest Before + After to select the newest saved videos automatically." + Environment.NewLine +
                "Keep this fit session open while saving so Cassette Motion Pro knows this client is the active target." + Environment.NewLine +
                Environment.NewLine +
                "You can still use Browse when you want to pick an older take instead.";
            File.WriteAllText(hintPath, contents);
        }

        private string ImportVideo(string sourcePath, string viewName)
        {
            string destinationDirectory = GetSessionVideoViewFolderPath(viewName);
            Directory.CreateDirectory(destinationDirectory);

            string destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
            if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                return destinationPath;

            if (File.Exists(destinationPath))
            {
                string name = Path.GetFileNameWithoutExtension(sourcePath);
                string extension = Path.GetExtension(sourcePath);
                destinationPath = Path.Combine(destinationDirectory, name + "_" + DateTime.Now.ToString("HHmmss") + extension);
            }

            File.Copy(sourcePath, destinationPath, false);
            return destinationPath;
        }

        private static string GetVideoViewName(string key)
        {
            return key.Replace("VideoPath", string.Empty);
        }

        private void BrowseReportImage(string key)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                string viewName = GetReportImageViewName(key);
                dialog.Title = "Choose " + viewName.ToLowerInvariant() + " report image";
                dialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*";
                dialog.RestoreDirectory = true;
                if (Directory.Exists(client.PhotosPath))
                    dialog.InitialDirectory = client.PhotosPath;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        SaveCurrentSession();
                        imageBoxes[key].Text = ImportReportImage(dialog.FileName, viewName);
                        if (key == "SideBySideReportImagePath" && imageBoxes.ContainsKey("MeasurementReferenceImagePath"))
                            imageBoxes["MeasurementReferenceImagePath"].Text = imageBoxes[key].Text;
                        SaveCurrentSession();
                        if (key == "SideBySideReportImagePath")
                            UpdateSaveHint("Side-by-side image saved and set as the Bike Metrics measurement image.");
                        else
                            UpdateSaveHint(viewName + " report image saved to this session’s Photos folder.");
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(this, "The report image could not be imported into the client folder.\n\n" + exception.Message, "Report image", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenReportImagesFolderForSaving()
        {
            try
            {
                SaveCurrentSession();
                string folderPath = GetSessionReportImagesFolderPath();
                Directory.CreateDirectory(folderPath);
                ReportImageSaveTarget.SetActiveFolder(folderPath);
                Process.Start(folderPath);
                UpdateSaveHint("Report Images folder opened. Save or copy report screenshots here, then click Use Latest.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The Report Images folder could not be opened.\n\n" + exception.Message, "Report Images", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetReportImagesSaveFolder()
        {
            try
            {
                SaveCurrentSession();
                string folderPath = GetSessionReportImagesFolderPath();
                Directory.CreateDirectory(folderPath);
                ReportImageSaveTarget.SetActiveFolder(folderPath);

                if (prepareCaptureFolder != null)
                    prepareCaptureFolder(folderPath);

                Clipboard.SetText(folderPath);
                UpdateSaveHint("Report Images save folder set and copied. Save report screenshots here: " + folderPath);
                MessageBox.Show(
                    this,
                    "Report Images save folder is ready for this active fit session.\n\n" +
                    "The folder path was also copied, so if Windows asks where to save, paste it into the folder box.",
                    "Report Images",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The Report Images save folder could not be set.\n\n" + exception.Message, "Report Images", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyReportImagesFolderPath()
        {
            try
            {
                SaveCurrentSession();
                string folderPath = GetSessionReportImagesFolderPath();
                Directory.CreateDirectory(folderPath);
                ReportImageSaveTarget.SetActiveFolder(folderPath);
                Clipboard.SetText(folderPath);
                UpdateSaveHint("Report Images folder path copied. Paste it into the Windows save dialog if needed.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The Report Images folder path could not be copied.\n\n" + exception.Message, "Report Images", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UseLatestReportImage(string key)
        {
            try
            {
                SaveCurrentSession();
                string folderPath = GetSessionReportImagesFolderPath();
                Directory.CreateDirectory(folderPath);
                ReportImageSaveTarget.SetActiveFolder(folderPath);

                string latestImagePath = FindLatestReportImageFile(folderPath, key);
                if (string.IsNullOrEmpty(latestImagePath))
                {
                    MessageBox.Show(
                        this,
                        "No report images were found yet.\n\nOpen the Report Images folder, save or copy an image there, then click Use Latest again.",
                        "Report Images",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                imageBoxes[key].Text = latestImagePath;
                if (key == "SideBySideReportImagePath" && imageBoxes.ContainsKey("MeasurementReferenceImagePath"))
                    imageBoxes["MeasurementReferenceImagePath"].Text = latestImagePath;

                SaveCurrentSession();

                string viewName = GetReportImageViewName(key);
                if (key == "SideBySideReportImagePath")
                    UpdateSaveHint("Latest side-by-side image selected and set as the Bike Metrics measurement image: " + Path.GetFileName(latestImagePath));
                else
                    UpdateSaveHint("Latest " + viewName.ToLowerInvariant() + " report image selected: " + Path.GetFileName(latestImagePath));
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The latest report image could not be selected.\n\n" + exception.Message, "Report Images", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string FindLatestImageFile(string folderPath)
        {
            return FindLatestImageFile(folderPath, string.Empty);
        }

        private static string FindLatestReportImageFile(string folderPath, string key)
        {
            string prefix = string.Empty;
            if (key == "BeforeReportImagePath")
                prefix = "Before-ReportImage-";
            else if (key == "AfterReportImagePath")
                prefix = "After-ReportImage-";

            if (!string.IsNullOrEmpty(prefix))
            {
                string matchingImage = FindLatestImageFile(folderPath, prefix);
                if (!string.IsNullOrEmpty(matchingImage))
                    return matchingImage;
            }

            return FindLatestImageFile(folderPath);
        }

        private static string FindLatestImageFile(string folderPath, string requiredPrefix)
        {
            if (!Directory.Exists(folderPath))
                return string.Empty;

            string[] extensions = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif" };
            string latestPath = string.Empty;
            DateTime latestWriteTime = DateTime.MinValue;

            foreach (string extension in extensions)
            {
                foreach (string path in Directory.GetFiles(folderPath, extension, SearchOption.TopDirectoryOnly))
                {
                    if (!string.IsNullOrEmpty(requiredPrefix) && !Path.GetFileName(path).StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    DateTime writeTime = File.GetLastWriteTime(path);
                    if (writeTime > latestWriteTime)
                    {
                        latestWriteTime = writeTime;
                        latestPath = path;
                    }
                }
            }

            return latestPath;
        }

        private string GetReportImageViewName(string key)
        {
            if (key.StartsWith("Before"))
                return "Before";
            if (key.StartsWith("After"))
                return "After";
            if (key.StartsWith("Measurement"))
                return "Measurement reference";
            return "Side-by-side";
        }

        private string ImportReportImage(string sourcePath, string viewName)
        {
            string destinationDirectory = Path.Combine(client.PhotosPath, "Fit Sessions", currentSession.StorageFolderName, "Report Images");
            Directory.CreateDirectory(destinationDirectory);

            string extension = Path.GetExtension(sourcePath);
            string destinationPath = Path.Combine(destinationDirectory, viewName + extension);
            if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                return destinationPath;

            if (File.Exists(destinationPath))
            {
                string name = Path.GetFileNameWithoutExtension(destinationPath);
                destinationPath = Path.Combine(destinationDirectory, name + "_" + DateTime.Now.ToString("HHmmss") + extension);
            }

            File.Copy(sourcePath, destinationPath, false);
            return destinationPath;
        }

        private string CreateBeforeAfterCombinedImage(string beforePath, string afterPath)
        {
            string destinationDirectory = Path.Combine(client.SideBySidePath, "Fit Sessions", currentSession.StorageFolderName);
            Directory.CreateDirectory(destinationDirectory);

            string destinationPath = Path.Combine(destinationDirectory, "Before_After_Side_by_side.jpg");
            if (File.Exists(destinationPath))
                destinationPath = Path.Combine(destinationDirectory, "Before_After_Side_by_side_" + DateTime.Now.ToString("HHmmss") + ".jpg");

            using (Image before = Image.FromFile(beforePath))
            using (Image after = Image.FromFile(afterPath))
            {
                const int labelHeight = 46;
                const int padding = 18;
                const int gap = 16;
                const int panelWidth = 850;
                const int panelHeight = 900;
                int canvasWidth = (panelWidth * 2) + gap + (padding * 2);
                int canvasHeight = panelHeight + labelHeight + (padding * 2);

                using (Bitmap combined = new Bitmap(canvasWidth, canvasHeight))
                using (Graphics graphics = Graphics.FromImage(combined))
                using (Brush background = new SolidBrush(Color.FromArgb(18, 24, 31)))
                using (Brush imagePanelBrush = new SolidBrush(Color.FromArgb(8, 12, 16)))
                using (Brush labelBrush = new SolidBrush(Color.White))
                using (Brush accentBrush = new SolidBrush(Color.FromArgb(184, 243, 74)))
                using (Font labelFont = new Font("Segoe UI", 18F, FontStyle.Bold))
                using (Pen dividerPen = new Pen(Color.FromArgb(76, 91, 106), 2F))
                using (Pen panelPen = new Pen(Color.FromArgb(76, 91, 106), 1F))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.FillRectangle(background, 0, 0, canvasWidth, canvasHeight);

                    Rectangle beforePanel = new Rectangle(padding, padding + labelHeight, panelWidth, panelHeight);
                    Rectangle afterPanel = new Rectangle(padding + panelWidth + gap, padding + labelHeight, panelWidth, panelHeight);

                    graphics.DrawString("BEFORE", labelFont, accentBrush, beforePanel.Left, padding + 8);
                    graphics.DrawString("AFTER", labelFont, labelBrush, afterPanel.Left, padding + 8);
                    graphics.DrawLine(dividerPen, padding + panelWidth + (gap / 2), padding + 8, padding + panelWidth + (gap / 2), canvasHeight - padding);

                    graphics.FillRectangle(imagePanelBrush, beforePanel);
                    graphics.FillRectangle(imagePanelBrush, afterPanel);
                    graphics.DrawRectangle(panelPen, beforePanel);
                    graphics.DrawRectangle(panelPen, afterPanel);
                    graphics.DrawImage(before, GetCenteredImageRectangle(before, beforePanel));
                    graphics.DrawImage(after, GetCenteredImageRectangle(after, afterPanel));

                    combined.Save(destinationPath, ImageFormat.Jpeg);
                }
            }

            return destinationPath;
        }

        private static Rectangle GetCenteredImageRectangle(Image image, Rectangle bounds)
        {
            if (image == null || image.Width <= 0 || image.Height <= 0)
                return bounds;

            double widthScale = bounds.Width / (double)image.Width;
            double heightScale = bounds.Height / (double)image.Height;
            double scale = Math.Min(widthScale, heightScale);
            int width = Math.Max(1, (int)Math.Round(image.Width * scale));
            int height = Math.Max(1, (int)Math.Round(image.Height * scale));
            int left = bounds.Left + ((bounds.Width - width) / 2);
            int top = bounds.Top + ((bounds.Height - height) / 2);
            return new Rectangle(left, top, width, height);
        }

        private void OpenReportImage(string key)
        {
            string path = imageBoxes[key].Text;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "Choose an existing image file first.", "Image required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Process.Start(path);
        }

        private void OpenSingle(string key)
        {
            string path = mediaBoxes[key].Text;
            if (!ValidateVideo(path))
                return;
            PrepareAnalysisCaptureFolder();
            SetFitCommandCenterMode("Analyze");
            Close();
            if (openVideo != null)
                openVideo(path);
        }

        private void OpenPair(string firstKey, string secondKey)
        {
            string first = mediaBoxes[firstKey].Text;
            string second = mediaBoxes[secondKey].Text;
            if (!ValidateVideo(first) || !ValidateVideo(second))
                return;
            PrepareAnalysisCaptureFolder();
            SetFitCommandCenterMode("Analyze: Before + After");
            Close();
            if (openVideoPair != null)
            {
                openVideoPair(first, second);
            }
            else if (openVideo != null)
            {
                openVideo(first);
                openVideo(second);
            }
        }

        private void OpenAnalysisCapturesFolder()
        {
            try
            {
                PrepareAnalysisCaptureFolder();
                Process.Start(GetSessionAnalysisCapturesFolderPath());
                RefreshAnalysisCapturesStatus();
                UpdateSaveHint("Analysis Captures folder is ready for this fit session.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The Analysis Captures folder could not be opened.\n\n" + exception.Message, "Analysis Captures", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSessionVideoFolder(string viewName)
        {
            try
            {
                SaveCurrentSession();
                Directory.CreateDirectory(GetSessionVideosFolderPath());
                string folder = GetSessionVideoViewFolderPath(viewName);
                Directory.CreateDirectory(folder);
                WriteCaptureFolderHint(folder, viewName);
                Process.Start(folder);
                RefreshRecordingFolderGuide();
                RefreshSavedEvidenceReview();
                UpdateSaveHint(viewName + " video folder opened for this active fit session.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The " + viewName + " video folder could not be opened.\n\n" + exception.Message, viewName + " Video Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenLatestSavedEvidence(string label, string folder, bool videoFiles)
        {
            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
            {
                MessageBox.Show(this, "Open a client fit session first so Cassette Motion Pro knows which folder to check.", "Open Latest Evidence", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string latest = FindLatestEvidenceFile(folder, videoFiles);
                if (string.IsNullOrWhiteSpace(latest))
                {
                    MessageBox.Show(this, "No saved " + label + " was found for this fit session yet.", "Open Latest Evidence", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Process.Start(latest);
                UpdateSaveHint("Opened latest " + label + ": " + Path.GetFileName(latest));
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The latest " + label + " could not be opened.\n\n" + exception.Message, "Open Latest Evidence", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenLatestReportImage()
        {
            if (currentSession == null || string.IsNullOrWhiteSpace(currentSession.StorageFolderName))
            {
                MessageBox.Show(this, "Open a client fit session first so Cassette Motion Pro knows which report image folder to check.", "Open Latest Report Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string reportImagesFolder = GetSessionReportImagesFolderPath();
                string latest = FindLatestEvidenceFileInFolders(
                    false,
                    Path.Combine(reportImagesFolder, "Before"),
                    Path.Combine(reportImagesFolder, "After"),
                    Path.Combine(reportImagesFolder, "Dual"));

                if (string.IsNullOrWhiteSpace(latest))
                {
                    MessageBox.Show(this, "No saved report image was found for this fit session yet.", "Open Latest Report Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Process.Start(latest);
                UpdateSaveHint("Opened latest report image: " + Path.GetFileName(latest));
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The latest report image could not be opened.\n\n" + exception.Message, "Open Latest Report Image", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenReportImagesFolder()
        {
            try
            {
                SaveCurrentSession();
                string folder = GetSessionReportImagesFolderPath();
                Directory.CreateDirectory(folder);
                Directory.CreateDirectory(Path.Combine(folder, "Before"));
                Directory.CreateDirectory(Path.Combine(folder, "After"));
                Directory.CreateDirectory(Path.Combine(folder, "Dual"));
                Process.Start(folder);
                RefreshSavedEvidenceReview();
                UpdateSaveHint("Report Images folder opened for this active fit session.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The Report Images folder could not be opened.\n\n" + exception.Message, "Report Images Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrepareAnalysisCaptureFolder()
        {
            SaveCurrentSession();
            Directory.CreateDirectory(GetSessionVideosFolderPath());
            Directory.CreateDirectory(GetSessionPhotosFolderPath());
            Directory.CreateDirectory(GetSessionAnalysisCapturesFolderPath());

            if (prepareCaptureFolder != null)
                prepareCaptureFolder(GetSessionAnalysisCapturesFolderPath());

            UpdateSaveHint("Analysis Captures folder prepared for this fit session.");
            RefreshAnalysisCapturesStatus();
        }

        private void CheckSavedAnalysisEvidence()
        {
            try
            {
                PrepareAnalysisCaptureFolder();
                int count = CountAnalysisCaptureEvidenceFiles();
                RefreshAnalysisCapturesStatus();
                UpdateWorkflowChecklist();

                if (count > 0)
                {
                    UpdateSaveHint(count + " saved evidence file" + (count == 1 ? "" : "s") + " found in Analysis Captures.");
                    MessageBox.Show(this,
                        count + " saved evidence file" + (count == 1 ? "" : "s") + " found in this session’s Analysis Captures folder.\n\n" +
                        "Next best step: enter the final measured values in Measurements, choose report images, then preview the report.",
                        "Saved Evidence Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    UpdateSaveHint("No saved evidence files found yet in Analysis Captures.");
                    MessageBox.Show(this,
                        "No saved evidence files were found yet.\n\n" +
                        "After measuring in Video Studio, save screenshots, exported frames, or clips into this session’s Analysis Captures folder, then check again.",
                        "No Saved Evidence Yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Saved evidence could not be checked.\n\n" + exception.Message, "Analysis Captures", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateVideo(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                return true;
            MessageBox.Show(this, "Choose an existing video file first.", "Video required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private static Button CreateButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            CassetteMotionTheme.StyleButton(button, primary);
            return button;
        }

        private sealed class WorkflowChecklistItem
        {
            public WorkflowChecklistItem(Label statusLabel, Func<bool> isReady)
            {
                StatusLabel = statusLabel;
                IsReady = isReady;
            }

            public Label StatusLabel { get; private set; }

            public Func<bool> IsReady { get; private set; }
        }

        private sealed class FitDayFlowStep
        {
            public FitDayFlowStep(Panel card, Label numberLabel, Label titleLabel, Label detailLabel, Button actionButton, string numberText, Func<bool> isReady, Func<string> getDetail)
            {
                Card = card;
                NumberLabel = numberLabel;
                TitleLabel = titleLabel;
                DetailLabel = detailLabel;
                ActionButton = actionButton;
                NumberText = numberText;
                IsReady = isReady;
                GetDetail = getDetail;
            }

            public Panel Card { get; private set; }

            public Label NumberLabel { get; private set; }

            public Label TitleLabel { get; private set; }

            public Label DetailLabel { get; private set; }

            public Button ActionButton { get; private set; }

            public string NumberText { get; private set; }

            public Func<bool> IsReady { get; private set; }

            public Func<string> GetDetail { get; private set; }
        }
    }
}
