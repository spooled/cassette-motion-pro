/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using CassetteMotionPro.Clients;
using System;
using System.Collections.Generic;
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
        private readonly ListView sessionList = new ListView();
        private readonly TextBox txtTitle = new TextBox();
        private readonly DateTimePicker dtpDate = new DateTimePicker();
        private readonly ComboBox cmbStatus = new ComboBox();
        private readonly TextBox txtGoals = new TextBox();
        private readonly TextBox txtNotes = new TextBox();
        private readonly Dictionary<string, TextBox> mediaBoxes = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, TextBox> measurementBoxes = new Dictionary<string, TextBox>();
        private FitSessionRecord currentSession;

        public BikeFitWorkspaceForm(ClientRecord client, Action<string> openVideo)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            this.client = client;
            this.openVideo = openVideo;
            repository = new FitSessionRepository(client);

            Text = "Bike Fit Workspace - Cassette Motion Pro";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ForeColor = Color.FromArgb(24, 31, 29);
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterParent;

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
            tabs.TabPages.Add(BuildMeasurementsTab());
            tabs.TabPages.Add(BuildNotesTab());

            Panel actions = new Panel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 70;
            actions.Padding = new Padding(24, 14, 24, 14);
            actions.BackColor = Color.White;

            Button close = CreateButton("Close", false);
            close.Dock = DockStyle.Right;
            close.Width = 100;
            close.Click += delegate { Close(); };

            Button save = CreateButton("Save Session", true);
            save.Dock = DockStyle.Right;
            save.Width = 130;
            save.Click += Save_Click;

            Label saveHint = new Label();
            saveHint.Text = "Save before opening videos.";
            saveHint.Dock = DockStyle.Left;
            saveHint.Width = 220;
            saveHint.TextAlign = ContentAlignment.MiddleLeft;
            saveHint.ForeColor = Color.FromArgb(92, 104, 98);

            actions.Controls.Add(close);
            actions.Controls.Add(save);
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

            AddMediaRow(table, "Left", "LeftVideoPath");
            AddMediaRow(table, "Right", "RightVideoPath");

            FlowLayoutPanel comparisons = new FlowLayoutPanel();
            comparisons.Dock = DockStyle.Fill;
            comparisons.FlowDirection = FlowDirection.LeftToRight;
            comparisons.Padding = new Padding(0, 18, 0, 0);

            Button leftRight = CreateButton("Open Left + Right", true);
            leftRight.Size = new Size(170, 38);
            leftRight.Click += delegate { OpenPair("LeftVideoPath", "RightVideoPath"); };
            comparisons.Controls.Add(leftRight);

            int comparisonRow = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            table.Controls.Add(comparisons, 1, comparisonRow);
            table.SetColumnSpan(comparisons, 3);

            Label hint = new Label();
            hint.Text = "Opening the pair loads the left and right videos into Cassette Motion Pro’s synchronized dual-player workspace.";
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

            SetMedia("LeftVideoPath", string.IsNullOrEmpty(session.LeftVideoPath) ? session.SideVideoPath : session.LeftVideoPath);
            SetMedia("RightVideoPath", string.IsNullOrEmpty(session.RightVideoPath) ? session.FrontVideoPath : session.RightVideoPath);

            SetMeasurement("SaddleHeightBefore", session.SaddleHeightBefore);
            SetMeasurement("SaddleHeightAfter", session.SaddleHeightAfter);
            SetMeasurement("SaddleSetbackBefore", session.SaddleSetbackBefore);
            SetMeasurement("SaddleSetbackAfter", session.SaddleSetbackAfter);
            SetMeasurement("HandlebarReachBefore", session.HandlebarReachBefore);
            SetMeasurement("HandlebarReachAfter", session.HandlebarReachAfter);
            SetMeasurement("HandlebarDropBefore", session.HandlebarDropBefore);
            SetMeasurement("HandlebarDropAfter", session.HandlebarDropAfter);
            SetMeasurement("CrankLengthBefore", session.CrankLengthBefore);
            SetMeasurement("CrankLengthAfter", session.CrankLengthAfter);
            SetMeasurement("CleatPositionBefore", session.CleatPositionBefore);
            SetMeasurement("CleatPositionAfter", session.CleatPositionAfter);
        }

        private void SetMedia(string key, string value)
        {
            mediaBoxes[key].Text = value ?? string.Empty;
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
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The fit session could not be saved.\n\n" + exception.Message, "Bike Fit Workspace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCurrentSession()
        {
            if (currentSession == null)
                currentSession = new FitSessionRecord();

            currentSession.Title = txtTitle.Text.Trim();
            currentSession.SessionDate = dtpDate.Value.Date;
            currentSession.Status = Convert.ToString(cmbStatus.SelectedItem);
            currentSession.Goals = txtGoals.Text.Trim();
            currentSession.Notes = txtNotes.Text.Trim();
            currentSession.LeftVideoPath = mediaBoxes["LeftVideoPath"].Text;
            currentSession.RightVideoPath = mediaBoxes["RightVideoPath"].Text;
            currentSession.SaddleHeightBefore = measurementBoxes["SaddleHeightBefore"].Text.Trim();
            currentSession.SaddleHeightAfter = measurementBoxes["SaddleHeightAfter"].Text.Trim();
            currentSession.SaddleSetbackBefore = measurementBoxes["SaddleSetbackBefore"].Text.Trim();
            currentSession.SaddleSetbackAfter = measurementBoxes["SaddleSetbackAfter"].Text.Trim();
            currentSession.HandlebarReachBefore = measurementBoxes["HandlebarReachBefore"].Text.Trim();
            currentSession.HandlebarReachAfter = measurementBoxes["HandlebarReachAfter"].Text.Trim();
            currentSession.HandlebarDropBefore = measurementBoxes["HandlebarDropBefore"].Text.Trim();
            currentSession.HandlebarDropAfter = measurementBoxes["HandlebarDropAfter"].Text.Trim();
            currentSession.CrankLengthBefore = measurementBoxes["CrankLengthBefore"].Text.Trim();
            currentSession.CrankLengthAfter = measurementBoxes["CrankLengthAfter"].Text.Trim();
            currentSession.CleatPositionBefore = measurementBoxes["CleatPositionBefore"].Text.Trim();
            currentSession.CleatPositionAfter = measurementBoxes["CleatPositionAfter"].Text.Trim();
            repository.Save(currentSession);
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
