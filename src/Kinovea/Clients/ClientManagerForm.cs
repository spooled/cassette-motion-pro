/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using Kinovea.Services;
using CassetteMotionPro;
using CassetteMotionPro.Workspace;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CassetteMotionPro.Clients
{
    public class ClientManagerForm : Form
    {
        private readonly ClientRepository repository;
        private readonly Action<ClientRecord> openClient;
        private readonly Action<ClientRecord> openWorkspace;
        private readonly TextBox txtSearch = new TextBox();
        private readonly ComboBox cmbFilter = new ComboBox();
        private readonly ComboBox cmbSort = new ComboBox();
        private readonly CheckBox chkShowArchived = new CheckBox();
        private readonly Label lblResults = new Label();
        private readonly ListView clientList = new ListView();
        private readonly Label lblName = new Label();
        private readonly Label lblBike = new Label();
        private readonly Label lblContact = new Label();
        private readonly Label lblLastOpened = new Label();
        private readonly Label lblNotes = new Label();
        private readonly Label lblFitStatus = new Label();
        private readonly Button btnOpenVideos = new Button();
        private readonly Button btnWorkspace = new Button();
        private readonly Button btnOpenFolder = new Button();
        private readonly Button btnFollowUp = new Button();
        private readonly Button btnArchive = new Button();
        private IList<ClientRecord> clients = new List<ClientRecord>();
        private readonly Dictionary<Guid, ClientFitSummary> fitSummaries = new Dictionary<Guid, ClientFitSummary>();

        private ClientRecord SelectedClient
        {
            get
            {
                if (clientList.SelectedItems.Count == 0)
                    return null;
                return clientList.SelectedItems[0].Tag as ClientRecord;
            }
        }

        public ClientManagerForm(ClientRepository repository, Action<ClientRecord> openClient, Action<ClientRecord> openWorkspace)
        {
            if (repository == null)
                throw new ArgumentNullException("repository");

            this.repository = repository;
            this.openClient = openClient;
            this.openWorkspace = openWorkspace;

            Text = "Client Fits — Cassette Motion Pro";
            CassetteMotionTheme.ApplyForm(this);
            ClientSize = new Size(1180, 700);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterParent;

            BuildInterface();
            ApplyVisualIdentity(this);
            RefreshClients();
        }

        private void BuildInterface()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 128;
            header.BackColor = CassetteMotionTheme.Header;

            Label brandBadge = new Label();
            brandBadge.Text = "CM";
            brandBadge.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            brandBadge.ForeColor = CassetteMotionTheme.Header;
            brandBadge.BackColor = CassetteMotionTheme.Accent;
            brandBadge.TextAlign = ContentAlignment.MiddleCenter;
            brandBadge.Size = new Size(54, 54);
            brandBadge.Location = new Point(26, 28);

            Label brand = new Label();
            brand.Text = "CASSETTE MOTION PRO";
            brand.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            brand.ForeColor = CassetteMotionTheme.Accent;
            brand.AutoSize = true;
            brand.Location = new Point(98, 15);

            Label title = new Label();
            title.Text = "Client Fits";
            title.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(95, 31);

            Label subtitle = new Label();
            subtitle.Text = "Choose a client, start a fit session, then save videos, measurements, and reports into that client folder.";
            subtitle.Font = new Font("Segoe UI", 9.5F);
            subtitle.ForeColor = Color.FromArgb(175, 187, 181);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(100, 75);

            Button newClient = CreateButton("+  New Client", true);
            newClient.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            newClient.Location = new Point(ClientSize.Width - 164, 38);
            newClient.Size = new Size(132, 40);
            newClient.Click += NewClient_Click;
            header.Resize += delegate { newClient.Left = header.ClientSize.Width - newClient.Width - 28; };

            header.Controls.Add(brandBadge);
            header.Controls.Add(brand);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(newClient);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 520;
            split.BackColor = CassetteMotionTheme.Border;
            split.Panel1.BackColor = CassetteMotionTheme.Surface;
            split.Panel2.BackColor = CassetteMotionTheme.Canvas;

            Panel searchPanel = new Panel();
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Height = 126;
            searchPanel.Padding = new Padding(18, 10, 18, 10);
            searchPanel.BackColor = CassetteMotionTheme.SurfaceSoft;

            Label searchLabel = new Label();
            searchLabel.Text = "Search";
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(18, 6);
            searchLabel.ForeColor = Color.FromArgb(92, 104, 98);

            txtSearch.Location = new Point(18, 25);
            txtSearch.Size = new Size(484, 28);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.Font = new Font("Segoe UI", 10F);
            txtSearch.TextChanged += delegate { PopulateList(); };

            cmbFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFilter.Items.AddRange(new object[] { "All active clients", "Follow-ups due", "Needs attention", "Fits in progress", "Completed fits" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.Location = new Point(18, 65);
            cmbFilter.Size = new Size(170, 28);
            cmbFilter.SelectedIndexChanged += delegate { PopulateList(); };

            cmbSort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSort.Items.AddRange(new object[] { "Recently opened", "Newest fit", "Next follow-up", "Client name" });
            cmbSort.SelectedIndex = 0;
            cmbSort.Location = new Point(198, 65);
            cmbSort.Size = new Size(145, 28);
            cmbSort.SelectedIndexChanged += delegate { PopulateList(); };

            chkShowArchived.Text = "Show archived";
            chkShowArchived.Location = new Point(353, 66);
            chkShowArchived.Size = new Size(125, 26);
            chkShowArchived.CheckedChanged += delegate { PopulateList(); };

            lblResults.Location = new Point(18, 98);
            lblResults.Size = new Size(470, 22);
            lblResults.ForeColor = CassetteMotionTheme.Muted;
            searchPanel.Controls.Add(txtSearch);
            searchPanel.Controls.Add(searchLabel);
            searchPanel.Controls.Add(cmbFilter);
            searchPanel.Controls.Add(cmbSort);
            searchPanel.Controls.Add(chkShowArchived);
            searchPanel.Controls.Add(lblResults);

            clientList.Dock = DockStyle.Fill;
            clientList.BorderStyle = BorderStyle.None;
            clientList.FullRowSelect = true;
            clientList.HideSelection = false;
            clientList.MultiSelect = false;
            clientList.View = View.Details;
            clientList.Columns.Add("Client", 145);
            clientList.Columns.Add("Bike", 125);
            clientList.Columns.Add("Latest fit", 85);
            clientList.Columns.Add("Follow-up", 120);
            clientList.SelectedIndexChanged += delegate { UpdateDetails(); };
            clientList.DoubleClick += delegate { OpenSelectedWorkspace(); };
            CassetteMotionTheme.StyleListView(clientList);

            split.Panel1.Controls.Add(clientList);
            split.Panel1.Controls.Add(searchPanel);
            BuildDetailsPanel(split.Panel2);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(split, 0, 1);
            Controls.Add(layout);

            Panel accentLine = new Panel();
            accentLine.Dock = DockStyle.Bottom;
            accentLine.Height = 4;
            accentLine.BackColor = CassetteMotionTheme.Accent;
            header.Controls.Add(accentLine);
        }

        private void BuildDetailsPanel(Control parent)
        {
            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(36, 34, 36, 24);

            lblName.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblName.ForeColor = Color.FromArgb(13, 19, 17);
            lblName.AutoSize = true;
            lblName.Location = new Point(34, 34);

            lblBike.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            lblBike.ForeColor = Color.FromArgb(88, 102, 95);
            lblBike.AutoSize = true;
            lblBike.Location = new Point(37, 82);

            lblContact.AutoSize = true;
            lblContact.Location = new Point(38, 137);
            lblContact.MaximumSize = new Size(470, 0);

            lblLastOpened.AutoSize = true;
            lblLastOpened.ForeColor = Color.FromArgb(88, 102, 95);
            lblLastOpened.Location = new Point(38, 174);

            lblFitStatus.AutoSize = true;
            lblFitStatus.ForeColor = CassetteMotionTheme.Warning;
            lblFitStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblFitStatus.Location = new Point(38, 200);

            Label notesHeading = new Label();
            notesHeading.Text = "NOTES";
            notesHeading.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            notesHeading.ForeColor = Color.FromArgb(113, 127, 120);
            notesHeading.AutoSize = true;
            notesHeading.Location = new Point(38, 242);

            lblNotes.AutoSize = true;
            lblNotes.MaximumSize = new Size(470, 96);
            lblNotes.Location = new Point(38, 268);
            lblNotes.ForeColor = Color.FromArgb(42, 51, 47);

            Label workflowHeading = new Label();
            workflowHeading.Text = "FIT WORKFLOW";
            workflowHeading.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            workflowHeading.ForeColor = Color.FromArgb(113, 127, 120);
            workflowHeading.AutoSize = true;
            workflowHeading.Location = new Point(38, 370);

            Label workflow = new Label();
            workflow.Text = "1. Open the client fit session\n2. Record, analyze, and save evidence\n3. Finish measurements and the report";
            workflow.AutoSize = true;
            workflow.MaximumSize = new Size(470, 0);
            workflow.Location = new Point(38, 396);
            workflow.ForeColor = Color.FromArgb(42, 51, 47);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 96;
            actions.Padding = new Padding(30, 8, 20, 6);
            actions.WrapContents = true;

            btnOpenFolder.Text = "Open Client Folder";
            btnOpenFolder.Size = new Size(145, 40);
            btnOpenFolder.Margin = new Padding(6, 4, 6, 4);
            StyleButton(btnOpenFolder, false);
            btnOpenFolder.Click += delegate
            {
                ClientRecord client = SelectedClient;
                if (client != null)
                    FilesystemHelper.LocateDirectory(client.FolderPath);
            };

            btnOpenVideos.Text = "Video Studio";
            btnOpenVideos.Size = new Size(110, 40);
            btnOpenVideos.Margin = new Padding(6, 4, 6, 4);
            StyleButton(btnOpenVideos, false);
            btnOpenVideos.Click += delegate { OpenSelectedClient(); };

            btnWorkspace.Text = "Start Fit Session";
            btnWorkspace.Size = new Size(130, 40);
            btnWorkspace.Margin = new Padding(6, 4, 6, 4);
            StyleButton(btnWorkspace, true);
            btnWorkspace.Click += delegate { OpenSelectedWorkspace(); };

            btnFollowUp.Text = "Add Follow-up";
            btnFollowUp.Size = new Size(130, 40);
            btnFollowUp.Margin = new Padding(6, 4, 6, 4);
            StyleButton(btnFollowUp, false);
            btnFollowUp.Click += delegate { AddFollowUpToLatestFit(); };

            btnArchive.Text = "Archive Client";
            btnArchive.Size = new Size(130, 40);
            btnArchive.Margin = new Padding(6, 4, 6, 4);
            StyleButton(btnArchive, false);
            btnArchive.Click += delegate { ToggleSelectedClientArchive(); };

            actions.Controls.Add(btnOpenFolder);
            actions.Controls.Add(btnOpenVideos);
            actions.Controls.Add(btnWorkspace);
            actions.Controls.Add(btnFollowUp);
            actions.Controls.Add(btnArchive);
            content.Controls.Add(lblName);
            content.Controls.Add(lblBike);
            content.Controls.Add(lblContact);
            content.Controls.Add(lblLastOpened);
            content.Controls.Add(lblFitStatus);
            content.Controls.Add(notesHeading);
            content.Controls.Add(lblNotes);
            content.Controls.Add(workflowHeading);
            content.Controls.Add(workflow);
            content.Controls.Add(actions);
            parent.Controls.Add(content);
        }

        private void RefreshClients()
        {
            clients = repository.LoadAll();
            fitSummaries.Clear();
            foreach (ClientRecord client in clients)
                fitSummaries[client.Id] = BuildFitSummary(client);
            PopulateList();
        }

        private void PopulateList()
        {
            string query = txtSearch.Text.Trim();
            clientList.BeginUpdate();
            clientList.Items.Clear();

            IEnumerable<ClientRecord> filtered = clients;
            if (!chkShowArchived.Checked)
                filtered = filtered.Where(c => !c.IsArchived);
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(c =>
                    Contains(c.DisplayName, query) ||
                    Contains(c.BikeDescription, query) ||
                    Contains(c.Email, query) ||
                    Contains(c.Phone, query) ||
                    SummaryContains(c, query));
            }

            string filter = Convert.ToString(cmbFilter.SelectedItem);
            if (filter == "Follow-ups due")
                filtered = filtered.Where(c => GetSummary(c).FollowUpDue);
            else if (filter == "Needs attention")
                filtered = filtered.Where(c => GetSummary(c).NeedsAttention);
            else if (filter == "Fits in progress")
                filtered = filtered.Where(c => GetSummary(c).HasInProgressFit);
            else if (filter == "Completed fits")
                filtered = filtered.Where(c => GetSummary(c).HasCompletedFit);

            string sort = Convert.ToString(cmbSort.SelectedItem);
            if (sort == "Newest fit")
                filtered = filtered.OrderByDescending(c => GetSummary(c).LatestFitDate).ThenBy(c => c.DisplayName);
            else if (sort == "Next follow-up")
                filtered = filtered.OrderBy(c => GetSummary(c).NextFollowUpDate == DateTime.MinValue ? DateTime.MaxValue : GetSummary(c).NextFollowUpDate).ThenBy(c => c.DisplayName);
            else if (sort == "Client name")
                filtered = filtered.OrderBy(c => c.DisplayName);
            else
                filtered = filtered.OrderByDescending(c => c.LastOpenedUtc);

            List<ClientRecord> visible = filtered.ToList();

            foreach (ClientRecord client in visible)
            {
                ClientFitSummary summary = GetSummary(client);
                string latestFit = summary.LatestFitDate == DateTime.MinValue ? "No fits" : summary.LatestFitDate.ToString("MMM d, yy");
                string followUp = summary.FollowUpLabel;
                ListViewItem item = new ListViewItem(new[] { client.DisplayName + (client.IsArchived ? " [Archived]" : ""), client.BikeDescription, latestFit, followUp });
                item.Tag = client;
                if (summary.FollowUpDue || summary.NeedsAttention)
                    item.ForeColor = CassetteMotionTheme.Warning;
                clientList.Items.Add(item);
            }

            int dueCount = clients.Count(c => !c.IsArchived && GetSummary(c).FollowUpDue);
            lblResults.Text = visible.Count.ToString() + " client(s) shown · " + dueCount.ToString() + " follow-up(s) due";

            clientList.EndUpdate();
            if (clientList.Items.Count > 0)
                clientList.Items[0].Selected = true;
            else
                UpdateDetails();
        }

        private static bool Contains(string source, string value)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void UpdateDetails()
        {
            ClientRecord client = SelectedClient;
            bool hasClient = client != null;
            btnOpenFolder.Enabled = hasClient;
            btnOpenVideos.Enabled = hasClient;
            btnWorkspace.Enabled = hasClient;
            btnFollowUp.Enabled = hasClient && GetSummary(client).LatestSession != null;
            btnArchive.Enabled = hasClient;

            if (!hasClient)
            {
                lblName.Text = "No client selected";
                lblBike.Text = "Create a client to begin a fit session.";
                lblContact.Text = string.Empty;
                lblLastOpened.Text = string.Empty;
                lblNotes.Text = string.Empty;
                lblFitStatus.Text = string.Empty;
                return;
            }

            lblName.Text = client.DisplayName;
            lblBike.Text = string.IsNullOrEmpty(client.BikeType)
                ? client.BikeDescription
                : string.Format("{0} · {1}", client.BikeDescription, client.BikeType);
            lblContact.Text = BuildContact(client);
            lblLastOpened.Text = client.LastOpenedUtc == DateTime.MinValue
                ? "Not opened yet"
                : "Last opened " + client.LastOpenedUtc.ToLocalTime().ToString("MMMM d, yyyy 'at' h:mm tt");
            lblNotes.Text = string.IsNullOrWhiteSpace(client.Notes) ? "No notes yet." : client.Notes;
            ClientFitSummary summary = GetSummary(client);
            lblFitStatus.Text = summary.StatusLine;
            btnArchive.Text = client.IsArchived ? "Restore Client" : "Archive Client";
        }

        private static string BuildContact(ClientRecord client)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(client.Email))
                parts.Add(client.Email);
            if (!string.IsNullOrWhiteSpace(client.Phone))
                parts.Add(client.Phone);
            return parts.Count == 0 ? "No contact information" : string.Join("  ·  ", parts.ToArray());
        }

        private ClientFitSummary GetSummary(ClientRecord client)
        {
            if (client == null)
                return new ClientFitSummary();
            ClientFitSummary summary;
            return fitSummaries.TryGetValue(client.Id, out summary) ? summary : new ClientFitSummary();
        }

        private ClientFitSummary BuildFitSummary(ClientRecord client)
        {
            ClientFitSummary summary = new ClientFitSummary();
            try
            {
                FitSessionRepository fitRepository = new FitSessionRepository(client);
                IList<FitSessionRecord> sessions = fitRepository.LoadAll();
                summary.Sessions = sessions;
                foreach (FitSessionRecord session in sessions)
                {
                    if (summary.LatestSession == null || session.SessionDate > summary.LatestFitDate ||
                        (session.SessionDate == summary.LatestFitDate && session.ModifiedUtc > summary.LatestSession.ModifiedUtc))
                    {
                        summary.LatestSession = session;
                        summary.LatestFitDate = session.SessionDate;
                    }
                    if (string.Equals(session.Status, "Complete", StringComparison.OrdinalIgnoreCase))
                        summary.HasCompletedFit = true;
                    else
                        summary.HasInProgressFit = true;

                    FitFollowUpEntry latest = GetLatestFollowUp(session);
                    if (latest != null && (summary.LatestFollowUp == null || latest.CheckInDate > summary.LatestFollowUp.CheckInDate))
                        summary.LatestFollowUp = latest;
                    if (latest != null && latest.HasNextCheckIn && latest.NextCheckInDate != DateTime.MinValue &&
                        (summary.NextFollowUpDate == DateTime.MinValue || latest.NextCheckInDate < summary.NextFollowUpDate))
                        summary.NextFollowUpDate = latest.NextCheckInDate;
                }
            }
            catch (Exception)
            {
                summary.LoadFailed = true;
            }

            summary.FollowUpDue = summary.NextFollowUpDate != DateTime.MinValue && summary.NextFollowUpDate.Date <= DateTime.Today;
            string adaptation = summary.LatestFollowUp == null ? string.Empty : summary.LatestFollowUp.AdaptationStatus;
            summary.NeedsAttention = string.Equals(adaptation, "Needs adjustment", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(adaptation, "Monitor", StringComparison.OrdinalIgnoreCase);
            if (summary.FollowUpDue)
                summary.FollowUpLabel = "Due " + summary.NextFollowUpDate.ToString("MMM d");
            else if (summary.NextFollowUpDate != DateTime.MinValue)
                summary.FollowUpLabel = "Next " + summary.NextFollowUpDate.ToString("MMM d");
            else if (!string.IsNullOrWhiteSpace(adaptation))
                summary.FollowUpLabel = adaptation;
            else
                summary.FollowUpLabel = "Not scheduled";

            if (summary.LoadFailed)
                summary.StatusLine = "Fit history is temporarily unavailable.";
            else if (summary.LatestSession == null)
                summary.StatusLine = "No saved fit sessions yet.";
            else
                summary.StatusLine = "Latest fit: " + summary.LatestSession.DisplayName + " · " + summary.FollowUpLabel;
            return summary;
        }

        private static FitFollowUpEntry GetLatestFollowUp(FitSessionRecord session)
        {
            if (session == null || session.FollowUps == null)
                return null;
            FitFollowUpEntry latest = null;
            foreach (FitFollowUpEntry entry in session.FollowUps)
            {
                if (entry != null && (latest == null || entry.CheckInDate > latest.CheckInDate ||
                    (entry.CheckInDate == latest.CheckInDate && entry.CreatedUtc > latest.CreatedUtc)))
                    latest = entry;
            }
            return latest;
        }

        private bool SummaryContains(ClientRecord client, string query)
        {
            ClientFitSummary summary = GetSummary(client);
            if (Contains(summary.FollowUpLabel, query))
                return true;
            foreach (FitSessionRecord session in summary.Sessions)
            {
                if (Contains(session.DisplayName, query) || Contains(session.Status, query) ||
                    Contains(session.FitTemplateBikeType, query) || Contains(session.FitProtocolBikeType, query))
                    return true;
            }
            return false;
        }

        private void AddFollowUpToLatestFit()
        {
            ClientRecord client = SelectedClient;
            ClientFitSummary summary = GetSummary(client);
            if (client == null || summary.LatestSession == null)
            {
                MessageBox.Show(this, "This client needs a saved fit session before adding a follow-up.", "Client Follow-up", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (FitFollowUpForm form = new FitFollowUpForm(summary.LatestSession.DisplayName))
            {
                if (form.ShowDialog(this) != DialogResult.OK || form.Entry == null)
                    return;
                if (summary.LatestSession.FollowUps == null)
                    summary.LatestSession.FollowUps = new List<FitFollowUpEntry>();
                summary.LatestSession.FollowUps.Add(form.Entry);
                new FitSessionRepository(client).Save(summary.LatestSession);
            }
            Guid selectedId = client.Id;
            RefreshClients();
            SelectClient(selectedId);
        }

        private void ToggleSelectedClientArchive()
        {
            ClientRecord client = SelectedClient;
            if (client == null)
                return;
            bool archive = !client.IsArchived;
            string action = archive ? "Archive" : "Restore";
            string detail = archive
                ? "The client will be hidden from the active list. No files, sessions, videos, or reports will be deleted."
                : "The client will return to the active list.";
            if (MessageBox.Show(this, action + " " + client.DisplayName + "?\n\n" + detail, action + " Client", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            repository.SetArchived(client, archive);
            RefreshClients();
        }

        private void NewClient_Click(object sender, EventArgs e)
        {
            using (NewClientForm form = new NewClientForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    ClientRecord created = repository.Create(form.Client);
                    RefreshClients();
                    SelectClient(created.Id);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(this, "The client could not be created.\n\n" + exception.Message, "Client Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SelectClient(Guid id)
        {
            foreach (ListViewItem item in clientList.Items)
            {
                ClientRecord client = item.Tag as ClientRecord;
                if (client != null && client.Id == id)
                {
                    item.Selected = true;
                    item.EnsureVisible();
                    return;
                }
            }
        }

        private void OpenSelectedClient()
        {
            ClientRecord client = SelectedClient;
            if (client == null)
                return;

            repository.MarkOpened(client);
            if (openClient != null)
                openClient(client);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OpenSelectedWorkspace()
        {
            ClientRecord client = SelectedClient;
            if (client == null)
                return;

            repository.MarkOpened(client);
            DialogResult = DialogResult.OK;
            Close();
            if (openWorkspace != null)
                openWorkspace(client);
        }

        private static Button CreateButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            StyleButton(button, primary);
            return button;
        }

        private static void StyleButton(Button button, bool primary)
        {
            CassetteMotionTheme.StyleButton(button, primary);
        }

        private static void ApplyVisualIdentity(Control root)
        {
            foreach (Control control in root.Controls)
            {
                if (control is TextBox || control is ComboBox)
                    CassetteMotionTheme.StyleTextInput(control);
                ApplyVisualIdentity(control);
            }
        }

        private sealed class ClientFitSummary
        {
            public IList<FitSessionRecord> Sessions = new List<FitSessionRecord>();
            public FitSessionRecord LatestSession;
            public DateTime LatestFitDate;
            public FitFollowUpEntry LatestFollowUp;
            public DateTime NextFollowUpDate;
            public bool FollowUpDue;
            public bool NeedsAttention;
            public bool HasInProgressFit;
            public bool HasCompletedFit;
            public bool LoadFailed;
            public string FollowUpLabel = "Not scheduled";
            public string StatusLine = string.Empty;
        }
    }
}
