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
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class BikeFitWorkspaceForm : Form
    {
        private readonly ClientRecord client;
        private readonly FitSessionRepository repository;
        private readonly Action<string> openVideo;
        private readonly Action<string> openBodyAngleGuide;
        private readonly ListView sessionList = new ListView();
        private readonly TextBox txtTitle = new TextBox();
        private readonly DateTimePicker dtpDate = new DateTimePicker();
        private readonly ComboBox cmbStatus = new ComboBox();
        private readonly TextBox txtGoals = new TextBox();
        private readonly TextBox txtNotes = new TextBox();
        private readonly Label saveHint = new Label();
        private readonly Dictionary<string, TextBox> mediaBoxes = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, TextBox> imageBoxes = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, TextBox> measurementBoxes = new Dictionary<string, TextBox>();
        private FitSessionRecord currentSession;

        public BikeFitWorkspaceForm(ClientRecord client, Action<string> openVideo, Action<string> openBodyAngleGuide)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            this.client = client;
            this.openVideo = openVideo;
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

            header.Controls.Add(eyebrow);
            header.Controls.Add(title);
            header.Controls.Add(bike);

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
            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Padding = new Point(18, 8);
            tabs.TabPages.Add(BuildOverviewTab());
            tabs.TabPages.Add(BuildMediaTab());
            tabs.TabPages.Add(BuildReportImagesTab());
            tabs.TabPages.Add(BuildMeasurementsTab());
            tabs.TabPages.Add(BuildBodyAnglesTab());
            tabs.TabPages.Add(BuildNotesTab());

            Panel actions = new Panel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 70;
            actions.Padding = new Padding(24, 14, 24, 14);
            actions.BackColor = Color.White;

            Button close = CreateButton("Save && Close", false);
            close.Dock = DockStyle.Right;
            close.Width = 120;
            close.Click += delegate { Close(); };

            Button save = CreateButton("Save Session", true);
            save.Dock = DockStyle.Right;
            save.Width = 130;
            save.Click += Save_Click;

            Button report = CreateButton("Generate Report", false);
            report.Dock = DockStyle.Right;
            report.Width = 145;
            report.Click += GenerateReport_Click;

            Button openReports = CreateButton("Open Reports", false);
            openReports.Dock = DockStyle.Right;
            openReports.Width = 125;
            openReports.Click += OpenReports_Click;

            saveHint.Text = "Autosaves to this client’s Measurements folder.";
            saveHint.Dock = DockStyle.Fill;
            saveHint.TextAlign = ContentAlignment.MiddleLeft;
            saveHint.ForeColor = Color.FromArgb(92, 104, 98);

            actions.Controls.Add(close);
            actions.Controls.Add(save);
            actions.Controls.Add(report);
            actions.Controls.Add(openReports);
            actions.Controls.Add(saveHint);
            parent.Controls.Add(tabs);
            parent.Controls.Add(actions);
        }

        private TabPage BuildOverviewTab()
        {
            TabPage page = NewTab("Overview");
            TableLayoutPanel table = NewEditorTable();
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
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

            AddMediaRow(table, "Before", "BeforeVideoPath");
            AddMediaRow(table, "After", "AfterVideoPath");

            FlowLayoutPanel comparisons = new FlowLayoutPanel();
            comparisons.Dock = DockStyle.Fill;
            comparisons.FlowDirection = FlowDirection.LeftToRight;
            comparisons.Padding = new Padding(0, 18, 0, 0);

            Button beforeAfter = CreateButton("Open Before + After", true);
            beforeAfter.Size = new Size(190, 38);
            beforeAfter.Click += delegate { OpenPair("BeforeVideoPath", "AfterVideoPath"); };
            comparisons.Controls.Add(beforeAfter);

            int comparisonRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            table.Controls.Add(comparisons, 1, comparisonRow);
            table.SetColumnSpan(comparisons, 3);

            Label hint = new Label();
            hint.Text = "Opening the pair loads the before and after videos into Cassette Motion Pro’s synchronized dual-player workspace.";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            int hintRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            table.Controls.Add(hint, 1, hintRow);
            table.SetColumnSpan(hint, 3);

            page.Controls.Add(table);
            return page;
        }

        private TabPage BuildMeasurementsTab()
        {
            TabPage page = NewTab("Measurements");
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.Padding = new Padding(24, 22, 24, 18);
            table.ColumnCount = 3;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            AddMeasurementHeader(table);
            AddMeasurementRow(table, "Saddle height", "SaddleHeight");
            AddMeasurementRow(table, "Saddle setback", "SaddleSetback");
            AddMeasurementRow(table, "Saddle tip to grip reach", "SaddleTipToGripReach");
            AddMeasurementRow(table, "Handlebar X", "HandlebarX");
            AddMeasurementRow(table, "Handlebar Y", "HandlebarY");
            AddMeasurementRow(table, "Handlebar reach", "HandlebarReach");
            AddMeasurementRow(table, "Handlebar drop", "HandlebarDrop");
            AddMeasurementRow(table, "Crank length", "CrankLength");
            AddMeasurementRow(table, "Cleat position", "CleatPosition");

            Label hint = new Label();
            hint.Text = "Enter the unit with the value (for example, 742 mm).";
            hint.Dock = DockStyle.Fill;
            hint.ForeColor = Color.FromArgb(92, 104, 98);
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            table.Controls.Add(hint, 1, row);
            table.SetColumnSpan(hint, 2);

            page.AutoScroll = true;
            page.Controls.Add(table);
            return page;
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

            Label hint = new Label();
            hint.Text = "Choose the images you want shown in the report. Use side-by-side for one combined before/after export. Images are copied into this client's Photos folder.";
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
            AddMeasurementRow(table, "Torso angle", "TorsoAngle");
            AddMeasurementRow(table, "Shoulder angle", "ShoulderAngle");
            AddMeasurementRow(table, "Elbow angle", "ElbowAngle");

            Label guidance = new Label();
            guidance.Text = "The guided overlay places markers at the shoulder, elbow, hand, hip, knee, ankle, and toe, then displays all six angles on the video.";
            guidance.Dock = DockStyle.Fill;
            guidance.ForeColor = Color.FromArgb(92, 104, 98);
            guidance.Padding = new Padding(0, 12, 0, 4);
            int guidanceRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            table.Controls.Add(guidance, 0, guidanceRow);
            table.SetColumnSpan(guidance, 3);

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

            Button open = CreateButton("Open", false);
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

        private void AddMeasurementHeader(TableLayoutPanel table)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            table.Controls.Add(FieldLabel("Measurement"), 0, row);
            table.Controls.Add(HeaderLabel("BEFORE"), 1, row);
            table.Controls.Add(HeaderLabel("AFTER"), 2, row);
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
            SetMeasurement("ElbowAngleBefore", session.ElbowAngleBefore);
            SetMeasurement("ElbowAngleAfter", session.ElbowAngleAfter);
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
                UpdateSaveHint("Saved to the client’s Measurements folder.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The fit session could not be saved.\n\n" + exception.Message, "Bike Fit Workspace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateReport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSession();
                string reportPath = FitSessionReportGenerator.Generate(client, currentSession);
                UpdateSaveHint("Report saved to the client’s Reports folder.");
                MessageBox.Show(this,
                    "The report was saved in this client’s Reports folder.\n\n" + reportPath,
                    "Report created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The report could not be created.\n\n" + exception.Message, "Report", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenReports_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(client.ReportsPath);
                Process.Start(client.ReportsPath);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The Reports folder could not be opened.\n\n" + exception.Message, "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            currentSession.BeforeVideoPath = mediaBoxes["BeforeVideoPath"].Text;
            currentSession.AfterVideoPath = mediaBoxes["AfterVideoPath"].Text;
            currentSession.BeforeReportImagePath = imageBoxes["BeforeReportImagePath"].Text;
            currentSession.AfterReportImagePath = imageBoxes["AfterReportImagePath"].Text;
            currentSession.SideBySideReportImagePath = imageBoxes["SideBySideReportImagePath"].Text;
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
            currentSession.ElbowAngleBefore = measurementBoxes["ElbowAngleBefore"].Text.Trim();
            currentSession.ElbowAngleAfter = measurementBoxes["ElbowAngleAfter"].Text.Trim();
            repository.Save(currentSession);
        }

        private void UpdateSaveHint(string message)
        {
            if (saveHint == null)
                return;
            saveHint.Text = message;
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
                "4. Record the six displayed values in the Body Angles tab.",
                "Bike Fit Angle Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            if (openBodyAngleGuide != null)
                openBodyAngleGuide(path);
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
            string sessionFolderName = string.Format("{0:yyyy-MM-dd}_{1}", currentSession.SessionDate, currentSession.Id.ToString("N").Substring(0, 8));
            string destinationDirectory = Path.Combine(client.VideosPath, "Fit Sessions", sessionFolderName, viewName);
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
                        SaveCurrentSession();
                        UpdateSaveHint(viewName + " report image saved to the client’s Photos folder.");
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
            return "Side-by-side";
        }

        private string ImportReportImage(string sourcePath, string viewName)
        {
            string sessionFolderName = string.Format("{0:yyyy-MM-dd}_{1}", currentSession.SessionDate, currentSession.Id.ToString("N").Substring(0, 8));
            string destinationDirectory = Path.Combine(client.PhotosPath, "Fit Sessions", sessionFolderName, "Report Images");
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
            SaveCurrentSession();
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
            SaveCurrentSession();
            Close();
            if (openVideo != null)
            {
                openVideo(first);
                openVideo(second);
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
    }
}
