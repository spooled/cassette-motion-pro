/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CassetteMotionPro.Clients
{
    public class NewClientForm : Form
    {
        private readonly TextBox txtFirstName = new TextBox();
        private readonly TextBox txtLastName = new TextBox();
        private readonly TextBox txtEmail = new TextBox();
        private readonly TextBox txtPhone = new TextBox();
        private readonly TextBox txtBikeMake = new TextBox();
        private readonly TextBox txtBikeModel = new TextBox();
        private readonly TextBox txtBikeYear = new TextBox();
        private readonly ComboBox cmbBikeType = new ComboBox();
        private readonly TextBox txtNotes = new TextBox();

        public ClientRecord Client { get; private set; }

        public NewClientForm()
        {
            Text = "New Client - Cassette Motion Pro";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(245, 247, 246);
            ForeColor = Color.FromArgb(24, 31, 29);
            ClientSize = new Size(620, 590);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            BuildInterface();
        }

        private void BuildInterface()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 92;
            header.BackColor = Color.FromArgb(13, 19, 17);

            Label title = new Label();
            title.Text = "NEW CLIENT";
            title.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(24, 18);

            Label subtitle = new Label();
            subtitle.Text = "Create the client and their first bicycle profile.";
            subtitle.Font = new Font("Segoe UI", 9.5F);
            subtitle.ForeColor = Color.FromArgb(175, 187, 181);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(27, 57);

            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            TableLayoutPanel fields = new TableLayoutPanel();
            fields.Dock = DockStyle.Fill;
            fields.Padding = new Padding(24, 18, 24, 12);
            fields.ColumnCount = 2;
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            ConfigureTextBox(txtFirstName);
            ConfigureTextBox(txtLastName);
            ConfigureTextBox(txtEmail);
            ConfigureTextBox(txtPhone);
            ConfigureTextBox(txtBikeMake);
            ConfigureTextBox(txtBikeModel);
            ConfigureTextBox(txtBikeYear);

            cmbBikeType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBikeType.Items.AddRange(new object[] { "Road", "Triathlon / TT", "Gravel", "Mountain", "Hybrid", "Track", "Other" });
            cmbBikeType.SelectedIndex = 0;
            cmbBikeType.Dock = DockStyle.Fill;

            txtNotes.Multiline = true;
            txtNotes.ScrollBars = ScrollBars.Vertical;
            txtNotes.Dock = DockStyle.Fill;

            AddField(fields, "First name", txtFirstName, 34);
            AddField(fields, "Last name", txtLastName, 34);
            AddField(fields, "Email", txtEmail, 34);
            AddField(fields, "Phone", txtPhone, 34);
            AddField(fields, "Bike make", txtBikeMake, 34);
            AddField(fields, "Bike model", txtBikeModel, 34);
            AddField(fields, "Bike year", txtBikeYear, 34);
            AddField(fields, "Bike type", cmbBikeType, 34);
            AddField(fields, "Notes", txtNotes, 90);

            Panel actions = new Panel();
            actions.Dock = DockStyle.Fill;
            actions.Height = 62;
            actions.Padding = new Padding(24, 10, 24, 12);

            Button cancel = CreateButton("Cancel", false);
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Dock = DockStyle.Right;
            cancel.Width = 100;

            Button save = CreateButton("Create Client", true);
            save.Dock = DockStyle.Right;
            save.Width = 130;
            save.Margin = new Padding(8, 0, 0, 0);
            save.Click += Save_Click;

            actions.Controls.Add(cancel);
            actions.Controls.Add(save);
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(fields, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            Controls.Add(layout);

            AcceptButton = save;
            CancelButton = cancel;
        }

        private static void ConfigureTextBox(TextBox textBox)
        {
            textBox.Dock = DockStyle.Fill;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void AddField(TableLayoutPanel table, string labelText, Control control, int height)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

            Label label = new Label();
            label.Text = labelText;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            label.ForeColor = Color.FromArgb(74, 87, 81);

            control.Margin = new Padding(0, 3, 0, 3);
            table.Controls.Add(label, 0, row);
            table.Controls.Add(control, 1, row);
        }

        private static Button CreateButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.BackColor = primary ? Color.FromArgb(184, 243, 74) : Color.White;
            button.ForeColor = Color.FromArgb(13, 19, 17);
            button.Font = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular);
            return button;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text) && string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show(this, "Enter at least a first or last name.", "Client name required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtFirstName.Focus();
                return;
            }

            Client = new ClientRecord
            {
                FirstName = txtFirstName.Text.Trim(),
                LastName = txtLastName.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                BikeMake = txtBikeMake.Text.Trim(),
                BikeModel = txtBikeModel.Text.Trim(),
                BikeYear = txtBikeYear.Text.Trim(),
                BikeType = Convert.ToString(cmbBikeType.SelectedItem),
                Notes = txtNotes.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
