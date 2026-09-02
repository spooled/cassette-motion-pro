/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using CassetteMotionPro.Clients;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class BackupDataTransferForm : Form
    {
        private readonly ClientRepository clientRepository = new ClientRepository();
        private readonly ComboBox cmbClients = new ComboBox();
        private readonly Label lblStatus = new Label();

        public BackupDataTransferForm()
        {
            Text = "Backup & Data Transfer";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(720, 560);
            BuildInterface();
            LoadClients();
        }

        private void BuildInterface()
        {
            Label heading = new Label();
            heading.Text = "Protect or move your Cassette Motion Pro data";
            heading.Font = new Font(Font, FontStyle.Bold);
            heading.AutoSize = true;
            heading.Location = new Point(26, 22);
            Controls.Add(heading);

            Label intro = new Label();
            intro.Text = "Full backups include clients, fit sessions, videos, images, measurements, reports, follow-ups, studio settings, custom branding, templates, and camera profiles.";
            intro.AutoSize = false;
            intro.Size = new Size(660, 48);
            intro.Location = new Point(26, 50);
            Controls.Add(intro);

            GroupBox full = new GroupBox();
            full.Text = "Whole studio backup";
            full.Location = new Point(26, 108);
            full.Size = new Size(668, 150);
            Controls.Add(full);

            AddAction(full, "Back Up All Data", "Create one dated ZIP containing all Cassette Motion Pro client and studio data.", 22, CreateFullBackup);
            AddAction(full, "Restore Backup", "Restore a validated full backup. A safety backup is created automatically first.", 82, RestoreFullBackup);

            GroupBox client = new GroupBox();
            client.Text = "Move one client to another computer";
            client.Location = new Point(26, 272);
            client.Size = new Size(668, 180);
            Controls.Add(client);

            Label choose = new Label();
            choose.Text = "Client";
            choose.Location = new Point(22, 32);
            choose.Size = new Size(90, 24);
            choose.TextAlign = ContentAlignment.MiddleLeft;
            client.Controls.Add(choose);

            cmbClients.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbClients.Location = new Point(116, 32);
            cmbClients.Size = new Size(526, 24);
            client.Controls.Add(cmbClients);

            AddAction(client, "Export Client", "Package the selected client and every file in their client folder.", 70, ExportClient);
            AddAction(client, "Import Client", "Import a Cassette Motion Pro client ZIP, with duplicate protection.", 128, ImportClient);

            lblStatus.Text = "Ready. Backups are saved only where you choose.";
            lblStatus.AutoSize = false;
            lblStatus.Size = new Size(660, 48);
            lblStatus.Location = new Point(28, 466);
            lblStatus.ForeColor = Color.FromArgb(45, 94, 48);
            Controls.Add(lblStatus);

            Button close = new Button();
            close.Text = "Close";
            close.DialogResult = DialogResult.OK;
            close.Size = new Size(110, 34);
            close.Location = new Point(584, 514);
            Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;
        }

        private static void AddAction(Control parent, string buttonText, string description, int top, EventHandler handler)
        {
            Button button = new Button();
            button.Text = buttonText;
            button.Size = new Size(145, 38);
            button.Location = new Point(22, top);
            button.Click += handler;
            parent.Controls.Add(button);

            Label label = new Label();
            label.Text = description;
            label.AutoSize = false;
            label.Size = new Size(470, 38);
            label.Location = new Point(178, top);
            label.TextAlign = ContentAlignment.MiddleLeft;
            parent.Controls.Add(label);
        }

        private void LoadClients()
        {
            object selectedId = cmbClients.SelectedValue;
            IList<ClientRecord> clients = clientRepository.LoadAll().OrderBy(client => client.DisplayName).ToList();
            cmbClients.DataSource = clients;
            cmbClients.DisplayMember = "DisplayName";
            cmbClients.ValueMember = "Id";
            if (selectedId != null)
                cmbClients.SelectedValue = selectedId;
        }

        private void CreateFullBackup(object sender, EventArgs e)
        {
            string defaultName = "Cassette Motion Pro Full Backup " + DateTime.Now.ToString("yyyy-MM-dd HHmm") + ".zip";
            using (SaveFileDialog dialog = CreateSaveDialog("Save full studio backup", defaultName))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                RunOperation("Creating full backup...", delegate
                {
                    DataPortabilityService.CreateFullBackup(dialog.FileName);
                    return "Full backup created:\n" + dialog.FileName;
                });
            }
        }

        private void RestoreFullBackup(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = CreateOpenDialog("Choose a full studio backup"))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                DialogResult confirm = MessageBox.Show(this,
                    "Restore this backup now?\n\nCurrent client and studio data will be replaced. Cassette Motion Pro will first create an automatic safety backup in Documents.",
                    "Restore Full Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes)
                    return;

                RunOperation("Validating backup and creating a safety copy...", delegate
                {
                    string safetyPath = DataPortabilityService.RestoreFullBackup(dialog.FileName);
                    LoadClients();
                    return "Restore complete. Automatic safety backup:\n" + safetyPath + "\n\nClose and reopen Client Fits to refresh any open client list.";
                });
            }
        }

        private void ExportClient(object sender, EventArgs e)
        {
            ClientRecord client = cmbClients.SelectedItem as ClientRecord;
            if (client == null)
            {
                MessageBox.Show(this, "Choose a client to export.", "Export Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string defaultName = SafeFileName(client.DisplayName) + " - Cassette Motion Pro Client.zip";
            using (SaveFileDialog dialog = CreateSaveDialog("Export client", defaultName))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                RunOperation("Exporting " + client.DisplayName + "...", delegate
                {
                    DataPortabilityService.ExportClient(client, dialog.FileName);
                    return client.DisplayName + " exported with all client files:\n" + dialog.FileName;
                });
            }
        }

        private void ImportClient(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = CreateOpenDialog("Choose a client package"))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    ClientPackageInfo info = DataPortabilityService.InspectClientPackage(dialog.FileName);
                    ClientImportChoice choice = ClientImportChoice.KeepBoth;
                    if (info.AlreadyExists)
                    {
                        DialogResult duplicate = MessageBox.Show(this,
                            info.Client.DisplayName + " already exists.\n\nYes: replace the existing client and files\nNo: keep both copies\nCancel: do not import",
                            "Client Already Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (duplicate == DialogResult.Cancel)
                            return;
                        choice = duplicate == DialogResult.Yes ? ClientImportChoice.Replace : ClientImportChoice.KeepBoth;
                    }

                    RunOperation("Importing " + info.Client.DisplayName + "...", delegate
                    {
                        ClientRecord imported = DataPortabilityService.ImportClient(dialog.FileName, choice);
                        LoadClients();
                        cmbClients.SelectedValue = imported.Id;
                        return imported.DisplayName + " imported successfully. Open Client Fits to review the client and sessions.";
                    });
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }

        private void RunOperation(string workingText, Func<string> operation)
        {
            try
            {
                UseWaitCursor = true;
                lblStatus.Text = workingText;
                lblStatus.Refresh();
                string result = operation();
                lblStatus.Text = result;
                MessageBox.Show(this, result, "Backup & Data Transfer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void ShowError(Exception ex)
        {
            lblStatus.Text = "Nothing was changed. " + ex.Message;
            MessageBox.Show(this, ex.Message, "Backup & Data Transfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static SaveFileDialog CreateSaveDialog(string title, string fileName)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = title;
            dialog.Filter = "Cassette Motion Pro ZIP|*.zip";
            dialog.DefaultExt = "zip";
            dialog.AddExtension = true;
            dialog.FileName = fileName;
            dialog.InitialDirectory = DataPortabilityService.GetDefaultBackupFolder();
            return dialog;
        }

        private static OpenFileDialog CreateOpenDialog(string title)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = title;
            dialog.Filter = "Cassette Motion Pro ZIP|*.zip";
            dialog.InitialDirectory = DataPortabilityService.GetDefaultBackupFolder();
            return dialog;
        }

        private static string SafeFileName(string value)
        {
            foreach (char character in Path.GetInvalidFileNameChars())
                value = value.Replace(character, '_');
            return value;
        }
    }
}
