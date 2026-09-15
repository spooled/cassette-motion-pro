/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public sealed class SessionMediaItem
    {
        public string Path { get; set; }
        public string Kind { get; set; }
        public string Role { get; set; }
        public string Location { get; set; }
        public DateTime SavedAt { get; set; }
        public long SizeBytes { get; set; }
    }

    public sealed class SessionMediaLibraryForm : Form
    {
        private readonly List<SessionMediaItem> allItems;
        private readonly ListView mediaList = new ListView();
        private readonly ComboBox typeFilter = new ComboBox();
        private readonly ComboBox roleFilter = new ComboBox();
        private readonly TextBox search = new TextBox();
        private readonly PictureBox preview = new PictureBox();
        private readonly Label previewMessage = new Label();
        private readonly Label countStatus = new Label();
        private readonly ComboBox assignRole = new ComboBox();
        private readonly Button analyze = new Button();
        private readonly Button assign = new Button();

        public string VideoToAnalyze { get; private set; }
        public string ImageToAssign { get; private set; }
        public string AssignmentRole { get; private set; }

        public SessionMediaLibraryForm(List<SessionMediaItem> items, string sessionName)
        {
            allItems = items ?? new List<SessionMediaItem>();
            Text = "Cassette Motion Pro - Session Media Library";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ClientSize = new Size(1240, 760);
            MinimumSize = new Size(980, 640);
            StartPosition = FormStartPosition.CenterParent;
            BuildInterface(sessionName);
            RefreshList();
        }

        private void BuildInterface(string sessionName)
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(20, 27, 24);
            Label title = LabelFor("Session Media Library", 18F, true);
            title.ForeColor = Color.White;
            title.Location = new Point(22, 12);
            Label session = LabelFor(sessionName, 9F, false);
            session.ForeColor = Color.FromArgb(205, 216, 210);
            session.Location = new Point(24, 49);
            session.AutoEllipsis = true;
            session.Size = new Size(760, 22);
            header.Controls.Add(title);
            header.Controls.Add(session);

            FlowLayoutPanel filters = new FlowLayoutPanel();
            filters.Location = new Point(20, 78);
            filters.Size = new Size(920, 40);
            filters.WrapContents = false;
            filters.Controls.Add(FilterLabel("Type"));
            ConfigureFilter(typeFilter, new[] { "All media", "Videos", "Images" }, 125);
            filters.Controls.Add(typeFilter);
            filters.Controls.Add(FilterLabel("Role"));
            ConfigureFilter(roleFilter, new[] { "All roles", "Before", "After", "Dual", "Captures", "Report Images" }, 145);
            filters.Controls.Add(roleFilter);
            filters.Controls.Add(FilterLabel("Search"));
            search.Width = 230;
            search.TextChanged += delegate { RefreshList(); };
            filters.Controls.Add(search);
            header.Controls.Add(filters);
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            mediaList.Dock = DockStyle.Fill;
            mediaList.View = View.Details;
            mediaList.FullRowSelect = true;
            mediaList.HideSelection = false;
            mediaList.MultiSelect = false;
            mediaList.Columns.Add("Name", 250);
            mediaList.Columns.Add("Type", 70);
            mediaList.Columns.Add("Role", 105);
            mediaList.Columns.Add("Location", 130);
            mediaList.Columns.Add("Saved", 130);
            mediaList.Columns.Add("Size", 75);
            mediaList.SelectedIndexChanged += delegate { UpdateSelection(); };
            mediaList.DoubleClick += delegate { OpenSelected(); };
            root.Controls.Add(mediaList, 0, 1);

            Panel previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.Padding = new Padding(12);
            previewPanel.BackColor = Color.White;
            Label previewTitle = LabelFor("SELECTED EVIDENCE", 10F, true);
            previewTitle.Dock = DockStyle.Top;
            previewTitle.Height = 30;
            preview.Dock = DockStyle.Fill;
            preview.SizeMode = PictureBoxSizeMode.Zoom;
            preview.BackColor = Color.FromArgb(31, 36, 34);
            previewMessage.Dock = DockStyle.Bottom;
            previewMessage.Height = 88;
            previewMessage.Padding = new Padding(5);
            previewMessage.ForeColor = Color.FromArgb(74, 87, 81);
            previewMessage.Text = "Select a session file to preview its role and location.";
            previewPanel.Controls.Add(preview);
            previewPanel.Controls.Add(previewMessage);
            previewPanel.Controls.Add(previewTitle);
            root.Controls.Add(previewPanel, 1, 1);

            TableLayoutPanel actions = new TableLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.Padding = new Padding(12, 10, 12, 10);
            actions.ColumnCount = 7;
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));

            countStatus.Dock = DockStyle.Fill;
            countStatus.TextAlign = ContentAlignment.MiddleLeft;
            countStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            Button open = MakeButton("Open File", false);
            open.Click += delegate { OpenSelected(); };
            Button folder = MakeButton("Open Folder", false);
            folder.Click += delegate { OpenSelectedFolder(); };
            analyze.Text = "Analyze Video";
            StyleButton(analyze, true);
            analyze.Click += delegate { AnalyzeSelected(); };
            ConfigureFilter(assignRole, new[] { "Before", "After", "Dual", "Measurement Reference" }, 150);
            assign.Text = "Assign Image to Report";
            StyleButton(assign, true);
            assign.Click += delegate { AssignSelected(); };
            Button close = MakeButton("Close", false);
            close.Click += delegate { Close(); };
            actions.Controls.Add(countStatus, 0, 0);
            actions.Controls.Add(open, 1, 0);
            actions.Controls.Add(folder, 2, 0);
            actions.Controls.Add(analyze, 3, 0);
            actions.Controls.Add(assignRole, 4, 0);
            actions.Controls.Add(assign, 5, 0);
            actions.Controls.Add(close, 6, 0);
            root.Controls.Add(actions, 0, 2);
            root.SetColumnSpan(actions, 2);
            Controls.Add(root);
        }

        private void RefreshList()
        {
            mediaList.BeginUpdate();
            mediaList.Items.Clear();
            foreach (SessionMediaItem item in allItems)
            {
                if (!MatchesFilters(item)) continue;
                ListViewItem row = new ListViewItem(Path.GetFileName(item.Path));
                row.SubItems.Add(item.Kind);
                row.SubItems.Add(item.Role);
                row.SubItems.Add(item.Location);
                row.SubItems.Add(item.SavedAt.ToString("MMM d, h:mm tt"));
                row.SubItems.Add(FormatSize(item.SizeBytes));
                row.Tag = item;
                mediaList.Items.Add(row);
            }
            mediaList.EndUpdate();
            countStatus.Text = mediaList.Items.Count + " shown · " + allItems.Count + " total session files";
            if (mediaList.Items.Count > 0)
            {
                mediaList.Items[0].Selected = true;
                mediaList.Items[0].EnsureVisible();
            }
            else UpdateSelection();
        }

        private bool MatchesFilters(SessionMediaItem item)
        {
            string type = Convert.ToString(typeFilter.SelectedItem);
            if (type == "Videos" && item.Kind != "Video") return false;
            if (type == "Images" && item.Kind != "Image") return false;
            string role = Convert.ToString(roleFilter.SelectedItem);
            if (!string.IsNullOrEmpty(role) && role != "All roles" && !string.Equals(role, item.Role, StringComparison.OrdinalIgnoreCase)) return false;
            string query = search.Text.Trim();
            return query.Length == 0 || Path.GetFileName(item.Path).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                item.Location.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private SessionMediaItem SelectedItem()
        {
            return mediaList.SelectedItems.Count == 0 ? null : mediaList.SelectedItems[0].Tag as SessionMediaItem;
        }

        private void UpdateSelection()
        {
            SessionMediaItem item = SelectedItem();
            if (preview.Image != null) { preview.Image.Dispose(); preview.Image = null; }
            analyze.Enabled = item != null && item.Kind == "Video";
            assign.Enabled = item != null && item.Kind == "Image";
            assignRole.Enabled = assign.Enabled;
            if (item == null)
            {
                previewMessage.Text = "No media matches the current filters.";
                return;
            }
            previewMessage.Text = item.Role + " · " + item.Location + Environment.NewLine + Path.GetFileName(item.Path);
            if (item.Kind == "Image")
            {
                try { preview.Image = LoadUnlocked(item.Path); }
                catch { previewMessage.Text += Environment.NewLine + "Preview unavailable; the original file was not changed."; }
            }
            else previewMessage.Text += Environment.NewLine + "Use Analyze Video to open this clip in Video Studio.";
        }

        private void OpenSelected()
        {
            SessionMediaItem item = SelectedItem();
            if (item != null && File.Exists(item.Path)) Process.Start(item.Path);
        }

        private void OpenSelectedFolder()
        {
            SessionMediaItem item = SelectedItem();
            if (item != null && Directory.Exists(Path.GetDirectoryName(item.Path))) Process.Start(Path.GetDirectoryName(item.Path));
        }

        private void AnalyzeSelected()
        {
            SessionMediaItem item = SelectedItem();
            if (item == null || item.Kind != "Video") return;
            VideoToAnalyze = item.Path;
            DialogResult = DialogResult.Retry;
            Close();
        }

        private void AssignSelected()
        {
            SessionMediaItem item = SelectedItem();
            if (item == null || item.Kind != "Image") return;
            ImageToAssign = item.Path;
            AssignmentRole = Convert.ToString(assignRole.SelectedItem);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && preview.Image != null) preview.Image.Dispose();
            base.Dispose(disposing);
        }

        private static Image LoadUnlocked(string path)
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image source = Image.FromStream(stream)) return new Bitmap(source);
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1048576) return (bytes / 1048576d).ToString("0.0") + " MB";
            if (bytes >= 1024) return (bytes / 1024d).ToString("0") + " KB";
            return bytes + " B";
        }

        private static Label LabelFor(string text, float size, bool bold)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
            label.AutoSize = true;
            return label;
        }

        private static Label FilterLabel(string text)
        {
            Label label = LabelFor(text, 9F, true);
            label.ForeColor = Color.White;
            label.Margin = new Padding(8, 7, 3, 0);
            return label;
        }

        private void ConfigureFilter(ComboBox combo, string[] values, int width)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Width = width;
            combo.Items.AddRange(values);
            combo.SelectedIndex = 0;
            combo.SelectedIndexChanged += delegate { if (combo == typeFilter || combo == roleFilter) RefreshList(); };
        }

        private static Button MakeButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            StyleButton(button, primary);
            return button;
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = primary ? Color.FromArgb(184, 228, 35) : Color.White;
            button.ForeColor = Color.FromArgb(26, 34, 31);
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }
    }
}
