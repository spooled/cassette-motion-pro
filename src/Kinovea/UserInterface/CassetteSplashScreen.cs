/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.Root
{
    public class CassetteSplashScreen : Form
    {
        private const string SplashResourceName = "CassetteMotionPro.Brand.Splash.png";

        public CassetteSplashScreen()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(11, 16, 14);
            Width = 620;
            Height = 391;

            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.FromArgb(11, 16, 14);
            panel.Padding = new Padding(0);
            Controls.Add(panel);

            Image splash = LoadSplashImage();
            if (splash != null)
            {
                PictureBox picture = new PictureBox();
                picture.Image = splash;
                picture.SizeMode = PictureBoxSizeMode.Zoom;
                picture.Dock = DockStyle.Fill;
                panel.Controls.Add(picture);
            }
            else
            {
                Label label = new Label();
                label.Text = "CASSETTE MOTION PRO";
                label.Dock = DockStyle.Fill;
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Font = new Font("Segoe UI", 24F, FontStyle.Regular, GraphicsUnit.Point);
                label.ForeColor = Color.FromArgb(228, 236, 231);
                panel.Controls.Add(label);
            }

            Label version = new Label();
            version.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            version.AutoSize = false;
            version.Height = 24;
            version.Dock = DockStyle.Bottom;
            version.TextAlign = ContentAlignment.MiddleRight;
            version.Font = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point);
            version.ForeColor = Color.FromArgb(191, 244, 74);
            version.BackColor = Color.Transparent;
            version.Padding = new Padding(0, 0, 14, 8);
            panel.Controls.Add(version);
            version.BringToFront();
        }

        private static Image LoadSplashImage()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SplashResourceName);
            if (stream == null)
                return null;

            return Image.FromStream(stream);
        }
    }
}
