/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using Kinovea.Services;
using CassetteMotionPro;
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
        private readonly ListView clientList = new ListView();
        private readonly Label lblName = new Label();
        private readonly Label lblBike = new Label();
        private readonly Label lblContact = new Label();
        private readonly Label lblLastOpened = new Label();
        private readonly Label lblNotes = new Label();
        private readonly Button btnOpenVideos = new Button();
        private readonly Button btnWorkspace = new Button();
        private readonly Button btnOpenFolder = new Button();
        private IList<ClientRecord> clients = new List<ClientRecord>();

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
            ClientSize = new Size(1040, 650);
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
            split.SplitterDistance = 450;
            split.BackColor = CassetteMotionTheme.Border;
            split.Panel1.BackColor = CassetteMotionTheme.Surface;
            split.Panel2.BackColor = CassetteMotionTheme.Canvas;

            Panel searchPanel = new Panel();
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Height = 72;
            searchPanel.Padding = new Padding(18, 16, 18, 10);
            searchPanel.BackColor = CassetteMotionTheme.SurfaceSoft;

            Label searchLabel = new Label();
            searchLabel.Text = "Search";
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(18, 5);
            searchLabel.ForeColor = Color.FromArgb(92, 104, 98);

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.Font = new Font("Segoe UI", 10F);
            txtSearch.TextChanged += delegate { PopulateList(); };
            searchPanel.Controls.Add(txtSearch);
            searchPanel.Controls.Add(searchLabel);

            clientList.Dock = DockStyle.Fill;
            clientList.BorderStyle = BorderStyle.None;
            clientList.FullRowSelect = true;
            clientList.HideSelection = false;
            clientList.MultiSelect = false;
            clientList.View = View.Details;
            clientList.Columns.Add("Client", 175);
            clientList.Columns.Add("Bike", 155);
            clientList.Columns.Add("Last opened", 105);
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

            Label notesHeading = new Label();
            notesHeading.Text = "NOTES";
            notesHeading.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            notesHeading.ForeColor = Color.FromArgb(113, 127, 120);
            notesHeading.AutoSize = true;
            notesHeading.Location = new Point(38, 226);

            lblNotes.AutoSize = true;
            lblNotes.MaximumSize = new Size(470, 160);
            lblNotes.Location = new Point(38, 252);
            lblNotes.ForeColor = Color.FromArgb(42, 51, 47);

            Label workflowHeading = new Label();
            workflowHeading.Text = "FIT WORKFLOW";
            workflowHeading.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            workflowHeading.ForeColor = Color.FromArgb(113, 127, 120);
            workflowHeading.AutoSize = true;
            workflowHeading.Location = new Point(38, 420);

            Label workflow = new Label();
            workflow.Text = "1. Create or select the client\n2. Start/open the fit session first\n3. Record/analyze in Video Studio\n4. Save Before / After / Dual evidence\n5. Preview, package, and save the report";
            workflow.AutoSize = true;
            workflow.MaximumSize = new Size(470, 0);
            workflow.Location = new Point(38, 446);
            workflow.ForeColor = Color.FromArgb(42, 51, 47);

            Panel actions = new Panel();
            actions.Dock = DockStyle.Bottom;
            actions.Height = 64;

            btnOpenFolder.Text = "Open Client Folder";
            btnOpenFolder.Size = new Size(145, 40);
            btnOpenFolder.Location = new Point(36, 10);
            StyleButton(btnOpenFolder, false);
            btnOpenFolder.Click += delegate
            {
                ClientRecord client = SelectedClient;
                if (client != null)
                    FilesystemHelper.LocateDirectory(client.FolderPath);
            };

            btnOpenVideos.Text = "Video Studio";
            btnOpenVideos.Size = new Size(110, 40);
            btnOpenVideos.Location = new Point(191, 10);
            StyleButton(btnOpenVideos, false);
            btnOpenVideos.Click += delegate { OpenSelectedClient(); };

            btnWorkspace.Text = "Start Fit Session";
            btnWorkspace.Size = new Size(130, 40);
            btnWorkspace.Location = new Point(311, 10);
            StyleButton(btnWorkspace, true);
            btnWorkspace.Click += delegate { OpenSelectedWorkspace(); };

            actions.Controls.Add(btnOpenFolder);
            actions.Controls.Add(btnOpenVideos);
            actions.Controls.Add(btnWorkspace);
            content.Controls.Add(lblName);
            content.Controls.Add(lblBike);
            content.Controls.Add(lblContact);
            content.Controls.Add(lblLastOpened);
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
            PopulateList();
        }

        private void PopulateList()
        {
            string query = txtSearch.Text.Trim();
            clientList.BeginUpdate();
            clientList.Items.Clear();

            IEnumerable<ClientRecord> filtered = clients;
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(c =>
                    Contains(c.DisplayName, query) ||
                    Contains(c.BikeDescription, query) ||
                    Contains(c.Email, query) ||
                    Contains(c.Phone, query));
            }

            foreach (ClientRecord client in filtered)
            {
                string lastOpened = client.LastOpenedUtc == DateTime.MinValue
                    ? "Never"
                    : client.LastOpenedUtc.ToLocalTime().ToString("MMM d, yyyy");
                ListViewItem item = new ListViewItem(new[] { client.DisplayName, client.BikeDescription, lastOpened });
                item.Tag = client;
                clientList.Items.Add(item);
            }

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

            if (!hasClient)
            {
                lblName.Text = "No client selected";
                lblBike.Text = "Create a client to begin a fit session.";
                lblContact.Text = string.Empty;
                lblLastOpened.Text = string.Empty;
                lblNotes.Text = string.Empty;
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
    }
}
