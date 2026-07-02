/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using Kinovea.Services;
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
        private readonly TextBox txtSearch = new TextBox();
        private readonly ListView clientList = new ListView();
        private readonly Label lblName = new Label();
        private readonly Label lblBike = new Label();
        private readonly Label lblContact = new Label();
        private readonly Label lblLastOpened = new Label();
        private readonly Label lblNotes = new Label();
        private readonly Button btnOpenVideos = new Button();
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

        public ClientManagerForm(ClientRepository repository, Action<ClientRecord> openClient)
        {
            if (repository == null)
                throw new ArgumentNullException("repository");

            this.repository = repository;
            this.openClient = openClient;

            Text = "Client Manager - Cassette Motion Pro";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ForeColor = Color.FromArgb(24, 31, 29);
            ClientSize = new Size(1040, 650);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterParent;

            BuildInterface();
            RefreshClients();
        }

        private void BuildInterface()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 104;
            header.BackColor = Color.FromArgb(13, 19, 17);

            Label title = new Label();
            title.Text = "CLIENTS";
            title.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(26, 18);

            Label subtitle = new Label();
            subtitle.Text = "Manage riders, bicycles, videos, measurements, and reports.";
            subtitle.Font = new Font("Segoe UI", 9.5F);
            subtitle.ForeColor = Color.FromArgb(175, 187, 181);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(29, 62);

            Button newClient = CreateButton("+  New Client", true);
            newClient.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            newClient.Location = new Point(ClientSize.Width - 164, 31);
            newClient.Size = new Size(132, 40);
            newClient.Click += NewClient_Click;
            header.Resize += delegate { newClient.Left = header.ClientSize.Width - newClient.Width - 28; };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(newClient);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 450;
            split.BackColor = Color.FromArgb(218, 224, 221);
            split.Panel1.BackColor = Color.White;
            split.Panel2.BackColor = Color.FromArgb(247, 249, 248);

            Panel searchPanel = new Panel();
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Height = 62;
            searchPanel.Padding = new Padding(18, 16, 18, 10);

            Label searchLabel = new Label();
            searchLabel.Text = "Search";
            searchLabel.AutoSize = true;
            searchLabel.Location = new Point(18, 5);
            searchLabel.ForeColor = Color.FromArgb(92, 104, 98);

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
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
            clientList.DoubleClick += delegate { OpenSelectedClient(); };

            split.Panel1.Controls.Add(clientList);
            split.Panel1.Controls.Add(searchPanel);
            BuildDetailsPanel(split.Panel2);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(split, 0, 1);
            Controls.Add(layout);
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

            btnOpenVideos.Text = "Open Videos";
            btnOpenVideos.Size = new Size(130, 40);
            btnOpenVideos.Location = new Point(191, 10);
            StyleButton(btnOpenVideos, true);
            btnOpenVideos.Click += delegate { OpenSelectedClient(); };

            actions.Controls.Add(btnOpenFolder);
            actions.Controls.Add(btnOpenVideos);
            content.Controls.Add(lblName);
            content.Controls.Add(lblBike);
            content.Controls.Add(lblContact);
            content.Controls.Add(lblLastOpened);
            content.Controls.Add(notesHeading);
            content.Controls.Add(lblNotes);
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

        private static Button CreateButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            StyleButton(button, primary);
            return button;
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(186, 197, 191);
            button.BackColor = primary ? Color.FromArgb(184, 243, 74) : Color.White;
            button.ForeColor = Color.FromArgb(13, 19, 17);
            button.Font = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular);
        }
    }
}
