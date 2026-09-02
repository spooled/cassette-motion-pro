/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class StudioSettingsForm : Form
    {
        private readonly TextBox txtStudioName = new TextBox();
        private readonly TextBox txtFitterName = new TextBox();
        private readonly TextBox txtPhone = new TextBox();
        private readonly TextBox txtEmail = new TextBox();
        private readonly TextBox txtWebsite = new TextBox();
        private readonly TextBox txtReportRole = new TextBox();
        private readonly TextBox txtLogoPath = new TextBox();

        public StudioSettingsForm()
        {
            Text = "Studio Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(650, 475);
            BuildInterface();
            LoadSettings();
        }

        private void BuildInterface()
        {
            Label heading = new Label();
            heading.Text = "Studio and report branding";
            heading.Font = new Font(Font, FontStyle.Bold);
            heading.AutoSize = true;
            heading.Location = new Point(24, 22);
            Controls.Add(heading);

            Label help = new Label();
            help.Text = "These details appear on new reports and client packages. Leave optional contact fields blank to hide them.";
            help.AutoSize = false;
            help.Size = new Size(590, 38);
            help.Location = new Point(24, 49);
            Controls.Add(help);

            TableLayoutPanel table = new TableLayoutPanel();
            table.ColumnCount = 3;
            table.RowCount = 7;
            table.Location = new Point(24, 94);
            table.Size = new Size(600, 290);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            for (int i = 0; i < 7; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            AddRow(table, 0, "Studio name", txtStudioName);
            AddRow(table, 1, "Fitter name", txtFitterName);
            AddRow(table, 2, "Phone", txtPhone);
            AddRow(table, 3, "Email", txtEmail);
            AddRow(table, 4, "Website", txtWebsite);
            AddRow(table, 5, "Report subtitle", txtReportRole);
            AddRow(table, 6, "Custom logo", txtLogoPath);

            Button browse = new Button();
            browse.Text = "Browse...";
            browse.Dock = DockStyle.Fill;
            browse.Click += BrowseLogo;
            table.Controls.Add(browse, 2, 6);
            Controls.Add(table);

            Label logoHint = new Label();
            logoHint.Text = "Optional PNG or JPG. It replaces the standard report logo when “Full Cassette logo” is selected.";
            logoHint.AutoSize = false;
            logoHint.Size = new Size(590, 34);
            logoHint.Location = new Point(24, 381);
            Controls.Add(logoHint);

            Button save = new Button();
            save.Text = "Save Settings";
            save.Size = new Size(120, 34);
            save.Location = new Point(384, 425);
            save.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            save.Click += SaveSettings;
            Controls.Add(save);
            AcceptButton = save;

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Size = new Size(110, 34);
            cancel.Location = new Point(514, 425);
            cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Controls.Add(cancel);
            CancelButton = cancel;
        }

        private static void AddRow(TableLayoutPanel table, int row, string labelText, TextBox textBox)
        {
            Label label = new Label();
            label.Text = labelText;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            textBox.Dock = DockStyle.Fill;
            table.Controls.Add(label, 0, row);
            table.Controls.Add(textBox, 1, row);
            if (row != 6)
                table.SetColumnSpan(textBox, 2);
        }

        private void LoadSettings()
        {
            StudioSettings settings = StudioSettingsRepository.Current;
            txtStudioName.Text = settings.StudioName;
            txtFitterName.Text = settings.FitterName;
            txtPhone.Text = settings.Phone;
            txtEmail.Text = settings.Email;
            txtWebsite.Text = settings.Website;
            txtReportRole.Text = settings.ReportRole;
            txtLogoPath.Text = settings.CustomLogoPath;
        }

        private void BrowseLogo(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose studio report logo";
                dialog.Filter = "Image files|*.png;*.jpg;*.jpeg|All files|*.*";
                if (!string.IsNullOrWhiteSpace(txtLogoPath.Text))
                    dialog.InitialDirectory = Path.GetDirectoryName(txtLogoPath.Text);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    txtLogoPath.Text = dialog.FileName;
            }
        }

        private void SaveSettings(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStudioName.Text))
            {
                MessageBox.Show(this, "Enter the studio name used on reports.", "Studio Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtStudioName.Focus();
                return;
            }

            string logoPath = txtLogoPath.Text.Trim();
            if (!string.IsNullOrEmpty(logoPath) && !File.Exists(logoPath))
            {
                MessageBox.Show(this, "The selected logo file could not be found. Choose the logo again or clear the field.", "Studio Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            StudioSettingsRepository.Save(new StudioSettings
            {
                StudioName = txtStudioName.Text.Trim(),
                FitterName = txtFitterName.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Website = txtWebsite.Text.Trim(),
                ReportRole = txtReportRole.Text.Trim(),
                CustomLogoPath = logoPath
            });
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
