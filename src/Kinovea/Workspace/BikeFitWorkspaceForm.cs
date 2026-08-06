/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using CassetteMotionPro.Clients;
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
        private readonly Action<string> openVideo;
        private readonly Action<string, string> openVideoPair;
        private readonly Action<string> prepareCaptureFolder;
        private readonly Action<string> openBodyAngleGuide;
        private readonly ListView sessionList = new ListView();
        private readonly TextBox txtTitle = new TextBox();
        private readonly DateTimePicker dtpDate = new DateTimePicker();
        private readonly ComboBox cmbStatus = new ComboBox();
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
        private readonly Label nextRecommendedStep = new Label();
        private readonly Button nextRecommendedStepAction = new Button();
        private readonly CheckBox chkShowBeforeMeasurementsInReport = new CheckBox();
        private readonly CheckBox chkShowSideBySideImageInReport = new CheckBox();
        private readonly CheckBox chkShowBeforeImageInReport = new CheckBox();
        private readonly CheckBox chkShowAfterImageInReport = new CheckBox();
        private readonly CheckBox chkShowMeasurementReferenceImageInReport = new CheckBox();
        private readonly CheckBox chkShowMeasurementCaptureTraceInReport = new CheckBox();
        private readonly ComboBox cmbReportLogoStyle = new ComboBox();
        private readonly Dictionary<string, TextBox> mediaBoxes = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, TextBox> imageBoxes = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, TextBox> measurementBoxes = new Dictionary<string, TextBox>();
        private readonly List<WorkflowChecklistItem> workflowChecklistItems = new List<WorkflowChecklistItem>();
        private TabControl editorTabs;
        private FitSessionRecord currentSession;
        private Action nextRecommendedStepActionHandler;

        public BikeFitWorkspaceForm(ClientRecord client, Action<string> openVideo, Action<string, string> openVideoPair, Action<string> prepareCaptureFolder, Action<string> openBodyAngleGuide)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            this.client = client;
            this.openVideo = openVideo;
            this.openVideoPair = openVideoPair;
            this.prepareCaptureFolder = prepareCaptureFolder;
            this.openBodyAngleGuide = openBodyAngleGuide;
            repository = new FitSessionRepository(client);

            Text = "Bike Fit Workspace - Cassette Motion Pro";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ForeColor = Color.FromArgb(24, 31, 29);
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterParent;
            FormClosing += BikeFitWorkspaceForm_FormClosing;

            BuildInterface();
            RefreshSessions(Guid.Empty);
        }

        private void BuildInterface()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(13, 19, 17);

            Label eyebrow = new Label();
            eyebrow.Text = "BIKE FIT WORKSPACE";
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(184, 243, 74);
            eyebrow.AutoSize = true;
            eyebrow.Location = new Point(28, 17);

            Label title = new Label();
            title.Text = client.DisplayName;
            title.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(25, 34);

            Label bike = new Label();
            bike.Text = client.BikeDescription;
            bike.Font = new Font("Segoe UI", 10F);
            bike.ForeColor = Color.FromArgb(175, 187, 181);
            bike.AutoSize = true;
            bike.Location = new Point(30, 76);

            activeSessionStatus.Text = "Active session\nChoose or create a fit session";
            activeSessionStatus.Font = new Font("Segoe UI", 9F);
            activeSessionStatus.ForeColor = Color.FromArgb(205, 216, 210);
            activeSessionStatus.TextAlign = ContentAlignment.TopRight;
            activeSessionStatus.AutoSize = false;
            activeSessionStatus.Size = new Size(430, 72);
            activeSessionStatus.Location = new Point(720, 19);
            activeSessionStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            header.Controls.Add(eyebrow);
            header.Controls.Add(title);
            header.Controls.Add(bike);
            header.Controls.Add(activeSessionStatus);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 260;
            split.Panel1.BackColor = Color.White;
            split.Panel2.BackColor = Color.FromArgb(247, 249, 248);
            BuildSessionPanel(split.Panel1);
            BuildEditor(split.Panel2);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(split, 0, 1);
            Controls.Add(root);
        }

        private void BuildSessionPanel(Control parent)
        {
            Panel heading = new Panel();
            heading.Dock = DockStyle.Top;
            heading.Height = 68;
            heading.Padding = new Padding(16, 14, 16, 10);

            Button newSession = CreateButton("+ New Session", true);
            newSession.Dock = DockStyle.Fill;
            newSession.Click += delegate { BeginNewSession(); };
            heading.Controls.Add(newSession);

            sessionList.Dock = DockStyle.Fill;
            sessionList.View = View.Details;
            sessionList.BorderStyle = BorderStyle.None;
            sessionList.FullRowSelect = true;
            sessionList.HideSelection = false;
            sessionList.MultiSelect = false;
            sessionList.Columns.Add("Fit sessions", 155);
            sessionList.Columns.Add("Status", 85);
            sessionList.SelectedIndexChanged += SessionList_SelectedIndexChanged;

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
            editorTabs.SelectedIndexChanged += delegate { UpdateWorkflowChecklist(); };
            editorTabs.TabPages.Add(BuildOverviewTab());
            editorTabs.TabPages.Add(BuildClientFilesTab());
            editorTabs.TabPages.Add(BuildFitSummaryTab());
            editorTabs.TabPages.Add(BuildMediaTab());
            editorTabs.TabPages.Add(BuildVideoAnalysisTab());
            editorTabs.TabPages.Add(BuildReportImagesTab());
            editorTabs.TabPages.Add(BuildBikeMetricsTab());
            editorTabs.TabPages.Add(BuildBodyAnglesTab());
            editorTabs.TabPages.Add(BuildHandoffTab());
            editorTabs.TabPages.Add(BuildNotesTab());

            Panel actions = new Panel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 96;
            actions.Padding = new Padding(24, 10, 24, 10);
            actions.BackColor = Color.White;

            Button close = CreateButton("Save && Close", false);
            close.Width = 105;
            close.Click += delegate { Close(); };

            Button save = CreateButton("Save", true);
            save.Width = 82;
            save.Click += Save_Click;

            Button report = CreateButton("Generate", false);
            report.Width = 96;
            report.Click += GenerateReport_Click;

            Button previewReport = CreateButton("Preview", false);
            previewReport.Width = 88;
            previewReport.Click += PreviewReport_Click;

            Button reportPackage = CreateButton("Package", false);
            reportPackage.Width = 92;
            reportPackage.Click += ReportPackage_Click;

            Button zipReportPackage = CreateButton("Zip", false);
            zipReportPackage.Width = 70;
            zipReportPackage.Click += ZipReportPackage_Click;

            Button openReports = CreateButton("Reports", false);
            openReports.Width = 86;
            openReports.Click += OpenReports_Click;

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
            actionButtons.Height = 42;
            actionButtons.FlowDirection = FlowDirection.RightToLeft;
            actionButtons.WrapContents = false;
            actionButtons.AutoScroll = true;
            actionButtons.Padding = new Padding(0);

            actionButtons.Controls.Add(close);
            actionButtons.Controls.Add(save);
            actionButtons.Controls.Add(report);
            actionButtons.Controls.Add(previewReport);
            actionButtons.Controls.Add(reportPackage);
            actionButtons.Controls.Add(zipReportPackage);
            actionButtons.Controls.Add(openReports);
            actionButtons.Controls.Add(reviewSession);
            actionButtons.Controls.Add(chkShowBeforeMeasurementsInReport);

            actions.Controls.Add(actionButtons);
            actions.Controls.Add(saveHint);
            parent.Controls.Add(editorTabs);
            parent.Controls.Add(actions);
        }

        private TabPage BuildOverviewTab()
        {
            TabPage page = NewTab("Overview");
            TableLayoutPanel table = NewEditorTable();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;

            Control flow = BuildClientFirstFlow();
            int flowRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
            table.Controls.Add(flow, 0, flowRow);
            table.SetColumnSpan(flow, 2);

            Control shortcuts = BuildWorkflowShortcutBar();
            int shortcutsRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            table.Controls.Add(shortcuts, 0, shortcutsRow);
            table.SetColumnSpan(shortcuts, 2);

            Control nextStep = BuildNextRecommendedStepPanel();
            int nextStepRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            table.Controls.Add(nextStep, 0, nextStepRow);
            table.SetColumnSpan(nextStep, 2);

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

            Control checklist = BuildWorkflowChecklist();
            int checklistRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 610));
            table.Controls.Add(checklist, 0, checklistRow);
            table.SetColumnSpan(checklist, 2);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
        }

        private Control BuildClientFirstFlow()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.White;
            panel.Padding = new Padding(18, 14, 18, 12);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label title = new Label();
            title.Text = "Client-first fit path";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            title.TextAlign = ContentAlignment.MiddleLeft;

            Label description = new Label();
            description.Text = "Start with the client and session details, then open the videos in the full Kinovea analysis workspace. Do the measuring there first, save useful photos/video evidence into the active Analysis Captures folder, return to this session, save the values, and generate the report from the client folder.";
            description.Dock = DockStyle.Fill;
            description.ForeColor = Color.FromArgb(74, 87, 81);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;
            buttons.Padding = new Padding(0, 2, 0, 0);

            Button clientFiles = CreateButton("Client Files", false);
            clientFiles.Size = new Size(112, 34);
            clientFiles.Click += delegate { SelectWorkspaceTab("Client Files"); };

            Button startVideos = CreateButton("Open Analysis", true);
            startVideos.Size = new Size(126, 34);
            startVideos.Click += delegate { SaveAndSelectVideos(); };

            Button analyze = CreateButton("Kinovea Tools", false);
            analyze.Size = new Size(122, 34);
            analyze.Click += delegate { SelectWorkspaceTab("Video Analysis"); };

            buttons.Controls.Add(clientFiles);
            buttons.Controls.Add(startVideos);
            buttons.Controls.Add(analyze);

            Label path = new Label();
            path.Text = "Client info → Kinovea tools → Analysis Captures → Bike Metrics → Report";
            path.Dock = DockStyle.Fill;
            path.ForeColor = Color.FromArgb(92, 104, 98);
            path.TextAlign = ContentAlignment.MiddleLeft;

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(buttons, 1, 0);
            layout.Controls.Add(description, 0, 1);
            layout.Controls.Add(path, 1, 1);
            panel.Controls.Add(layout);
            return panel;
        }

        private Control BuildWorkflowShortcutBar()
        {
            GroupBox group = new GroupBox();
            group.Text = "Simplified Fit Workflow";
            group.Dock = DockStyle.Fill;
            group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            group.ForeColor = Color.FromArgb(37, 48, 43);
            group.Padding = new Padding(14, 8, 14, 12);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = true;
            buttons.Padding = new Padding(0, 4, 0, 0);

            AddWorkflowShortcutButton(buttons, "1. Client Info", true, SelectOverviewGoals);
            AddWorkflowShortcutButton(buttons, "2. Capture + Measure", false, SaveAndSelectVideos);
            AddWorkflowShortcutButton(buttons, "3. Fit Results", false, delegate { SelectWorkspaceTab("Bike Metrics"); });
            AddWorkflowShortcutButton(buttons, "4. Report", false, delegate { SelectWorkspaceTab("Report Images"); });

            group.Controls.Add(buttons);
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
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            nextRecommendedStep.Dock = DockStyle.Fill;
            nextRecommendedStep.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            nextRecommendedStep.ForeColor = Color.FromArgb(37, 48, 43);
            nextRecommendedStep.TextAlign = ContentAlignment.MiddleLeft;
            nextRecommendedStep.Text = "Next: enter rider goals.";

            nextRecommendedStepAction.Dock = DockStyle.Fill;
            nextRecommendedStepAction.FlatStyle = FlatStyle.Flat;
            nextRecommendedStepAction.FlatAppearance.BorderSize = 0;
            nextRecommendedStepAction.BackColor = Color.FromArgb(139, 214, 0);
            nextRecommendedStepAction.ForeColor = Color.FromArgb(20, 30, 24);
            nextRecommendedStepAction.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            nextRecommendedStepAction.Text = "Go";
            nextRecommendedStepAction.Click += delegate
            {
                if (nextRecommendedStepActionHandler != null)
                    nextRecommendedStepActionHandler();
                UpdateWorkflowChecklist();
            };

            layout.Controls.Add(nextRecommendedStep, 0, 0);
            layout.Controls.Add(nextRecommendedStepAction, 1, 0);
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
            title.Text = "Fit Workflow";
            title.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;

            Label hint = new Label();
            hint.Text = "Four stages: Client Info → Capture + Measure → Fit Results → Report. Green means ready.";
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
            AddWorkflowStageHeader(card, "1. Client Info", "Set up the person, bike, goals, and session before touching video.");
            AddWorkflowChecklistRow(card, "Client info", "Confirm the client folder, bike, and contact info before recording.", "Client", delegate { SelectWorkspaceTab("Client Files"); }, HasClientFolder);
            AddWorkflowChecklistRow(card, "Fit goals", "Enter the rider goals and session notes before making changes.", "Goals", SelectOverviewGoals, HasFitGoals);
            AddWorkflowStageHeader(card, "2. Capture + Measure", "Use Kinovea tools, then save videos, screenshots, exports, or clips to this client session.");
            AddWorkflowChecklistRow(card, "Before video", "Record/import the starting video into this client session.", "Videos", delegate { SelectWorkspaceTab("Videos"); }, delegate { return HasMediaFile("BeforeVideoPath"); });
            AddWorkflowChecklistRow(card, "After video", "Record/import the comparison/final video into this client session.", "Videos", delegate { SelectWorkspaceTab("Videos"); }, delegate { return HasMediaFile("AfterVideoPath"); });
            AddWorkflowChecklistRow(card, "Measure in Kinovea", "Open analysis, use the Kinovea tools, and save useful evidence into Analysis Captures.", "Tools", delegate { SelectWorkspaceTab("Video Analysis"); }, delegate { return HasMediaFile("BeforeVideoPath") || HasMediaFile("AfterVideoPath"); });
            AddWorkflowChecklistRow(card, "Evidence saved", "Save screenshots, exported images, or useful video evidence into Analysis Captures.", "Captures", delegate { SelectWorkspaceTab("Video Analysis"); }, HasAnalysisCaptureEvidence);
            AddWorkflowStageHeader(card, "3. Fit Results", "Enter the measured bike numbers and body angles you want reflected in the report.");
            AddWorkflowChecklistRow(card, "Bike Metrics", "Save the measured saddle height, setback, reach, and handlebar X/Y values.", "Metrics", delegate { SelectWorkspaceTab("Bike Metrics"); }, HasCoreBikeMetrics);
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

            AddClientFolderRow(table, "Client folder", client.FolderPath);
            AddClientFolderRow(table, "Videos", client.VideosPath);
            AddClientFolderRow(table, "Photos", client.PhotosPath);
            AddClientFolderRow(table, "Side-by-Side", client.SideBySidePath);
            AddClientFolderRow(table, "Reports", client.ReportsPath);
            AddClientFolderRow(table, "Measurements", client.MeasurementsPath);
            AddClientFolderRow(table, "Notes", client.NotesPath);
            AddSessionFolderRow(table, "Active session record", "Measurements → Sessions → active session");
            AddSessionVideosRow(table, "Active videos", "Videos → Fit Sessions → active session");
            AddSessionPhotosRow(table, "Active photos", "Photos → Fit Sessions → active session");
            AddSessionSideBySideRow(table, "Active side-by-side", "Side-by-Side → Fit Sessions → active session");
            AddSessionAnalysisCapturesRow(table, "Analysis captures", "Client/session folder prepared before opening Kinovea tools");
            AddSessionReportsRow(table, "Active reports", "Reports → Fit Sessions → active session");
            AddImportActionRow(table, "Add videos", "Copy before/after videos into this active fit session.", "Before Video", delegate { BrowseVideo("BeforeVideoPath"); }, "After Video", delegate { BrowseVideo("AfterVideoPath"); });
            AddImportActionRow(table, "Add photos", "Copy before/after report photos into this active fit session.", "Before Photo", delegate { BrowseReportImage("BeforeReportImagePath"); }, "After Photo", delegate { BrowseReportImage("AfterReportImagePath"); });

            Label hint = new Label();
            hint.Text = "Use these shortcuts when you want to check where a client’s files are being saved. The Active rows save the current session first, then open that session’s exact folder. Add videos/photos copies the selected file into this fit session and updates the matching Videos or Report Images tab.";
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

        private void AddSessionPhotosRow(TableLayoutPanel table, string labelText, string description)
        {
            AddDynamicFolderRow(table, labelText, description, GetSessionPhotosFolderPath, "Active session photos folder opened.");
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

        private string GetSessionPhotosFolderPath()
        {
            return Path.Combine(client.PhotosPath, "Fit Sessions", currentSession.StorageFolderName);
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
            TabPage page = NewTab("Videos");
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 4;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));

            Label analysisHint = new Label();
            analysisHint.Text = "Use Analyze to close this session screen and open the video in the main player with the drawing tools, timeline, playback controls, and joint controls.";
            analysisHint.Dock = DockStyle.Fill;
            analysisHint.ForeColor = Color.FromArgb(92, 104, 98);
            int analysisHintRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            table.Controls.Add(analysisHint, 1, analysisHintRow);
            table.SetColumnSpan(analysisHint, 3);

            AddMediaRow(table, "Before", "BeforeVideoPath");
            AddMediaRow(table, "After", "AfterVideoPath");

            FlowLayoutPanel comparisons = new FlowLayoutPanel();
            comparisons.Dock = DockStyle.Fill;
            comparisons.FlowDirection = FlowDirection.LeftToRight;
            comparisons.Padding = new Padding(0, 18, 0, 0);

            Button beforeAfter = CreateButton("Analyze Before + After", true);
            beforeAfter.Size = new Size(220, 38);
            beforeAfter.Click += delegate { OpenPair("BeforeVideoPath", "AfterVideoPath"); };
            comparisons.Controls.Add(beforeAfter);

            int comparisonRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            table.Controls.Add(comparisons, 1, comparisonRow);
            table.SetColumnSpan(comparisons, 3);

            Label hint = new Label();
            hint.Text = "The bike-fit controls are in the video player workspace. The Videos tab saves which files belong to this session; Analyze opens the player controls for measuring and reviewing movement.";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            int hintRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            table.Controls.Add(hint, 1, hintRow);
            table.SetColumnSpan(hint, 3);

            page.Controls.Add(table);
            return page;
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

            Button reviewMetrics = CreateButton("Review Metrics", true);
            reviewMetrics.Size = new Size(140, 34);
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
            hint.Text = "Enter the unit with the value (for example, 742 mm). Use Assist when you want guided image measurement, or enter values manually after measuring in Kinovea.";
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

            AddBikeMetricsWorkflowGuideRow(guideTable, "1. Open video tools", "Use Video Analysis to open Before, After, or Before + After in the full Kinovea workspace.");
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
            TabPage page = NewTab("Video Analysis");
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 24, 24, 18);
            table.ColumnCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label title = new Label();
            title.Text = "Measure in the full Kinovea video workspace";
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

            Button pair = CreateButton("Analyze Before + After", false);
            pair.Size = new Size(210, 38);
            pair.Click += delegate { OpenPair("BeforeVideoPath", "AfterVideoPath"); };

            Button captures = CreateButton("Open Captures Folder", false);
            captures.Size = new Size(180, 38);
            captures.Click += delegate { OpenAnalysisCapturesFolder(); };

            Button checkCaptures = CreateButton("Check Saved Evidence", true);
            checkCaptures.Size = new Size(190, 38);
            checkCaptures.Click += delegate { CheckSavedAnalysisEvidence(); };

            actions.Controls.Add(before);
            actions.Controls.Add(after);
            actions.Controls.Add(pair);
            actions.Controls.Add(captures);
            actions.Controls.Add(checkCaptures);

            int actionRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            table.Controls.Add(actions, 0, actionRow);

            analysisCapturesStatus.Text = "Evidence status: open analysis, save useful screenshots/exports, then click Check Saved Evidence.";
            analysisCapturesStatus.Dock = DockStyle.Fill;
            analysisCapturesStatus.ForeColor = Color.FromArgb(92, 104, 98);
            int statusRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            table.Controls.Add(analysisCapturesStatus, 0, statusRow);

            Label reminder = new Label();
            reminder.Text = "Recommended order: open Before/After analysis, measure in the Kinovea tools first, save useful photos or video evidence into Analysis Captures, then return here to enter Bike Metrics and choose report images.";
            reminder.Dock = DockStyle.Fill;
            reminder.ForeColor = Color.FromArgb(92, 104, 98);
            int reminderRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            table.Controls.Add(reminder, 0, reminderRow);

            Label folderHint = new Label();
            folderHint.Text = "This build prepares the active capture destination before analysis: Client folder → Analysis Captures → active session. Use Open Captures Folder if you want to confirm it before or after measuring.";
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

            AddSaveGuideRow(guide, "Evidence", "Screenshots, exported frames, clips, and reference media saved from the Kinovea workspace.", "Analysis Captures");
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
            guidance.Text = "Recommended process: pause the video at the same crank position, use the Kinovea angle tools, then enter the Before and After values you want in the report.";
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

            Button measureBefore = CreateButton("Measure Before Video", false);
            measureBefore.Size = new Size(170, 38);
            measureBefore.Click += delegate { StartBodyAngleGuide("BeforeVideoPath"); };
            Button measureAfter = CreateButton("Measure After Video", true);
            measureAfter.Size = new Size(170, 38);
            measureAfter.Click += delegate { StartBodyAngleGuide("AfterVideoPath"); };
            actions.Controls.Add(measureBefore);
            actions.Controls.Add(measureAfter);

            int actionRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            table.Controls.Add(actions, 0, actionRow);
            table.SetColumnSpan(actions, 3);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
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
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            Label label = FieldLabel(labelText);
            TextBox path = new TextBox();
            path.Dock = DockStyle.Fill;
            path.ReadOnly = true;
            path.BorderStyle = BorderStyle.FixedSingle;
            path.Margin = new Padding(0, 8, 8, 8);
            mediaBoxes.Add(key, path);

            Button browse = CreateButton("Browse…", false);
            browse.Margin = new Padding(0, 6, 8, 6);
            browse.Dock = DockStyle.Fill;
            browse.Click += delegate { BrowseVideo(key); };

            Button open = CreateButton("Analyze", false);
            open.Margin = new Padding(0, 6, 0, 6);
            open.Dock = DockStyle.Fill;
            open.Click += delegate { OpenSingle(key); };

            table.Controls.Add(label, 0, row);
            table.Controls.Add(path, 1, row);
            table.Controls.Add(browse, 2, row);
            table.Controls.Add(open, 3, row);
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
            page.BackColor = Color.FromArgb(247, 249, 248);
            page.Padding = new Padding(0);
            return page;
        }

        private void SaveAndSelectVideos()
        {
            try
            {
                SaveCurrentSession();
                SelectWorkspaceTab("Videos");
                UpdateSaveHint("Client and fit details saved. Next step: add/open videos for Kinovea measurement.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The session could not be saved before opening analysis.\n\n" + exception.Message, "Open Analysis", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectOverviewGoals()
        {
            SelectWorkspaceTab("Overview");
            txtGoals.Focus();
        }

        private void SelectWorkspaceTab(string tabText)
        {
            if (editorTabs == null)
                return;

            foreach (TabPage page in editorTabs.TabPages)
            {
                if (string.Equals(page.Text, tabText, StringComparison.OrdinalIgnoreCase))
                {
                    editorTabs.SelectedTab = page;
                    UpdateSaveHint("Opened " + tabText + " from the Fit Workflow checklist.");
                    return;
                }
            }
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

            UpdateNextRecommendedStep();
        }

        private void UpdateNextRecommendedStep()
        {
            if (nextRecommendedStep == null)
                return;

            string message;
            string actionText;
            Action action;
            Color color;

            if (!HasFitGoals())
            {
                message = "Next: enter rider goals and session notes before making changes.";
                actionText = "Go to Goals";
                action = SelectOverviewGoals;
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasMediaFile("BeforeVideoPath") || !HasMediaFile("AfterVideoPath"))
            {
                message = "Next: add Before and After videos to the active client session.";
                actionText = "Go to Videos";
                action = SaveAndSelectVideos;
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasAnalysisCaptureEvidence())
            {
                message = "Next: open Kinovea tools and save screenshots, exports, or clips into Analysis Captures.";
                actionText = "Go to Analysis";
                action = delegate { SelectWorkspaceTab("Video Analysis"); };
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasCoreBikeMetrics())
            {
                message = "Next: enter the final Bike Metrics values after measuring in Kinovea.";
                actionText = "Go to Metrics";
                action = delegate { SelectWorkspaceTab("Bike Metrics"); };
                color = Color.FromArgb(181, 118, 35);
            }
            else if (!HasReportImage())
            {
                message = "Next: choose report images or a side-by-side image for the client report.";
                actionText = "Go to Images";
                action = delegate { SelectWorkspaceTab("Report Images"); };
                color = Color.FromArgb(181, 118, 35);
            }
            else
            {
                message = "Ready: preview the report and confirm everything looks right.";
                actionText = "Preview Report";
                action = delegate { PreviewReport_Click(this, EventArgs.Empty); };
                color = Color.FromArgb(60, 145, 76);
            }

            nextRecommendedStep.Text = message;
            nextRecommendedStep.ForeColor = color;
            nextRecommendedStepAction.Text = actionText;
            nextRecommendedStepAction.Enabled = action != null;
            nextRecommendedStepActionHandler = action;
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
                analysisCapturesStatus.Text = "Evidence status: " + count + " saved file" + (count == 1 ? "" : "s") + " found in this session’s Analysis Captures folder.";
                analysisCapturesStatus.ForeColor = Color.FromArgb(60, 145, 76);
            }
            else
            {
                analysisCapturesStatus.Text = "Evidence status: no saved files found yet. Save screenshots, exported frames, or clips from Kinovea into Analysis Captures.";
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

            UpdateActiveSessionStatus();
            UpdateWorkflowChecklist();
            RefreshAnalysisCapturesStatus();
        }

        private void SetMedia(string key, string value)
        {
            mediaBoxes[key].Text = value ?? string.Empty;
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

            ReviewMetricRange(warnings, "Saddle height After", "SaddleHeightAfter", 500, 900, "mm", "If low or high, recheck calibration and the BB → saddle top click points.");
            ReviewMetricRange(warnings, "Saddle setback After", "SaddleSetbackAfter", -120, 60, "mm", "Behind BB should be negative. If the sign is backwards, use Flip Setback Sign or re-enter the value.");
            ReviewMetricRange(warnings, "Saddle tip to grip reach After", "SaddleTipToGripReachAfter", 350, 750, "mm", "If short or long, confirm you clicked saddle tip and the actual grip/hood contact point.");
            ReviewMetricRange(warnings, "Handlebar X After", "HandlebarXAfter", 300, 700, "mm", "Confirm this is horizontal distance from BB to the grip/hood contact point.");
            ReviewMetricRange(warnings, "Handlebar Y After", "HandlebarYAfter", -180, 180, "mm", "Confirm the image is level and the vertical direction is correct.");

            string message;
            MessageBoxIcon icon;
            if (issues.Count == 0 && warnings.Count == 0)
            {
                message = "Ready for report.\n\nThe key Bike Metrics are filled in and the final values look within broad expected ranges.\n\nNext action: generate, preview, package, or zip the report.";
                icon = MessageBoxIcon.Information;
            }
            else
            {
                message = "Bike Metrics Review\n\n";
                if (issues.Count > 0)
                    message += "Missing key values:\n- " + string.Join("\n- ", issues.ToArray()) + "\n\n";
                if (warnings.Count > 0)
                    message += "Values to double-check:\n- " + string.Join("\n- ", warnings.ToArray()) + "\n\n";
                message += "Next action: recheck Guided Capture, calibration, or manual entries as needed.\n\nThese checks are advisory. They do not block saving, reporting, packaging, or zipping.";
                icon = issues.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            }

            UpdateSaveHint(issues.Count == 0 && warnings.Count == 0 ? "Bike Metrics review passed." : "Bike Metrics review found items to check.");
            MessageBox.Show(this, message, "Review Metrics", MessageBoxButtons.OK, icon);
        }

        private void ReviewSession_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
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
            OpenClientFolder(client.ReportsPath, "Reports");
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
                return;

            try
            {
                SaveCurrentSession();
            }
            catch (Exception exception)
            {
                e.Cancel = true;
                MessageBox.Show(this, "The fit session could not be saved.\n\n" + exception.Message, "Bike Fit Workspace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                activeSessionStatus.Text = "Active session\nChoose or create a fit session";
                return;
            }

            string status = string.IsNullOrWhiteSpace(currentSession.Status) ? "Assessment" : currentSession.Status.Trim();
            string folder = currentSession.Id == Guid.Empty ? "pending until saved" : currentSession.StorageFolderName;
            activeSessionStatus.Text = "Active session: " + currentSession.DisplayName + " · " + status + "\n" +
                "Client: " + client.DisplayName + "\n" +
                "Session record: Measurements → Sessions → " + folder;
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
                string viewName = key.Replace("VideoPath", string.Empty);
                dialog.Title = "Import " + viewName.ToLowerInvariant() + " video";
                dialog.Filter = "Video files|*.mp4;*.mov;*.avi;*.mkv;*.m4v;*.mpg;*.mpeg;*.wmv|All files|*.*";
                dialog.RestoreDirectory = true;
                if (Directory.Exists(client.VideosPath))
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
                            mediaBoxes[key].Text = ImportVideo(dialog.FileName, viewName);
                            SaveCurrentSession();
                            UpdateSaveHint(viewName + " video copied into this active fit session.");
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

        private string ImportVideo(string sourcePath, string viewName)
        {
            string destinationDirectory = Path.Combine(client.VideosPath, "Fit Sessions", currentSession.StorageFolderName, viewName);
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
                        "Next: enter the final measured values in Bike Metrics, choose report images, then preview the report.",
                        "Saved Evidence Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    UpdateSaveHint("No saved evidence files found yet in Analysis Captures.");
                    MessageBox.Show(this,
                        "No saved evidence files were found yet.\n\n" +
                        "After measuring in Kinovea, save screenshots, exported frames, or clips into this session’s Analysis Captures folder, then check again.",
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
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(186, 197, 191);
            button.BackColor = primary ? Color.FromArgb(184, 243, 74) : Color.White;
            button.ForeColor = Color.FromArgb(13, 19, 17);
            button.Font = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular);
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
    }
}
