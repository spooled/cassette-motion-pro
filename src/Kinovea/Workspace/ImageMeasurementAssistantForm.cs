/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public class ImageMeasurementAssistantForm : Form
    {
        private readonly string imagePath;
        private Image loadedImage;

        public ImageMeasurementAssistantForm(string imagePath, string measurementName, string instructions)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentNullException("imagePath");
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("The measurement reference image could not be found.", imagePath);

            this.imagePath = imagePath;

            Text = "Image Measurement Assistant - " + measurementName;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ForeColor = Color.FromArgb(24, 31, 29);
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterParent;

            BuildInterface(measurementName, instructions);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (loadedImage != null)
                loadedImage.Dispose();
            base.OnFormClosed(e);
        }

        private void BuildInterface(string measurementName, string instructions)
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));

            PictureBox picture = new PictureBox();
            picture.Dock = DockStyle.Fill;
            picture.BackColor = Color.FromArgb(13, 19, 17);
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            loadedImage = Image.FromFile(imagePath);
            picture.Image = loadedImage;

            Panel side = new Panel();
            side.Dock = DockStyle.Fill;
            side.BackColor = Color.White;
            side.Padding = new Padding(22);

            Label eyebrow = new Label();
            eyebrow.Text = "BIKE METRICS ASSIST";
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);
            eyebrow.Dock = DockStyle.Top;
            eyebrow.Height = 26;

            Label title = new Label();
            title.Text = measurementName;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.Dock = DockStyle.Top;
            title.Height = 48;

            Label guide = new Label();
            guide.Text = instructions + "\n\n" +
                "This is the foundation for 2DMA-style guided measuring.\n\n" +
                "Coming next:\n" +
                "1. Calibrate scale.\n" +
                "2. Click the required landmarks.\n" +
                "3. Review calculated value.\n" +
                "4. Save the value back to Bike Metrics.";
            guide.Dock = DockStyle.Top;
            guide.Height = 210;
            guide.ForeColor = Color.FromArgb(74, 87, 81);

            Label imageLabel = new Label();
            imageLabel.Text = "Reference image:\n" + imagePath;
            imageLabel.Dock = DockStyle.Top;
            imageLabel.Height = 70;
            imageLabel.ForeColor = Color.FromArgb(92, 104, 98);

            Button calibrate = CreateButton("1. Calibrate Scale", false);
            Button landmarks = CreateButton("2. Click Landmarks", false);
            Button save = CreateButton("3. Save Metric", true);
            calibrate.Dock = DockStyle.Top;
            landmarks.Dock = DockStyle.Top;
            save.Dock = DockStyle.Top;
            calibrate.Height = 40;
            landmarks.Height = 40;
            save.Height = 40;
            calibrate.Margin = new Padding(0, 10, 0, 0);
            landmarks.Margin = new Padding(0, 10, 0, 0);
            save.Margin = new Padding(0, 10, 0, 0);
            calibrate.Click += FutureStep_Click;
            landmarks.Click += FutureStep_Click;
            save.Click += FutureStep_Click;

            Button close = CreateButton("Close", false);
            close.Dock = DockStyle.Bottom;
            close.Height = 40;
            close.Click += delegate { Close(); };

            side.Controls.Add(close);
            side.Controls.Add(save);
            side.Controls.Add(landmarks);
            side.Controls.Add(calibrate);
            side.Controls.Add(imageLabel);
            side.Controls.Add(guide);
            side.Controls.Add(title);
            side.Controls.Add(eyebrow);

            root.Controls.Add(picture, 0, 0);
            root.Controls.Add(side, 1, 0);
            Controls.Add(root);
        }

        private void FutureStep_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this,
                "This step is planned for the next measurement assistant build.\n\n" +
                "For now, this screen confirms the selected measurement image and metric workflow.",
                "Coming soon",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
