/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.Root
{
    // About dialog: brand header, version, and the credit to the Kinovea engine.
    public class CassetteAboutForm : Form
    {
        private static readonly Color Ink = Color.FromArgb(11, 16, 14);
        private static readonly Color Card = Color.FromArgb(17, 25, 22);
        private static readonly Color TextColor = Color.FromArgb(228, 236, 231);
        private static readonly Color Soft = Color.FromArgb(182, 196, 188);
        private static readonly Color Muted = Color.FromArgb(143, 161, 152);
        private static readonly Color Lime = Color.FromArgb(191, 244, 74);

        public CassetteAboutForm()
        {
            Text = "About Cassette Motion Pro";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Ink;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(560, 640);

            PictureBox header = new PictureBox();
            header.Dock = DockStyle.Top;
            header.Height = 353;
            header.SizeMode = PictureBoxSizeMode.Zoom;
            header.BackColor = Ink;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CassetteMotionPro.Brand.Splash.png"))
            {
                if (stream != null)
                    header.Image = Image.FromStream(stream);
            }

            Label version = new Label();
            version.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            version.Font = new Font("Consolas", 10.5F, FontStyle.Bold);
            version.ForeColor = Lime;
            version.AutoSize = false;
            version.SetBounds(24, 366, 512, 22);

            Label credit = new Label();
            credit.Text = "Cassette Motion Pro is built on the Kinovea video analysis engine by Joan Charmant and contributors, " +
                "used under the GNU General Public License version 2. The Kinovea name and its work belong to their authors.";
            credit.ForeColor = Soft;
            credit.AutoSize = false;
            credit.SetBounds(24, 396, 512, 62);

            LinkLabel kinovea = MakeLink("Kinovea project: kinovea.org", "https://www.kinovea.org");
            kinovea.SetBounds(24, 462, 512, 22);

            Label copyright = new Label();
            copyright.Text = "© 2026 Cassette Fit Studio. Kinovea engine © 2006-2026 Joan Charmant and contributors.";
            copyright.ForeColor = Muted;
            copyright.Font = new Font("Segoe UI", 8.75F);
            copyright.AutoSize = false;
            copyright.SetBounds(24, 494, 512, 40);

            Button close = new Button();
            close.Text = "Close";
            close.DialogResult = DialogResult.OK;
            close.FlatStyle = FlatStyle.Flat;
            close.FlatAppearance.BorderColor = Lime;
            close.BackColor = Lime;
            close.ForeColor = Color.FromArgb(16, 26, 8);
            close.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            close.Cursor = Cursors.Hand;
            close.SetBounds(440, 584, 96, 36);

            Controls.Add(header);
            Controls.Add(version);
            Controls.Add(credit);
            Controls.Add(kinovea);
            Controls.Add(copyright);
            Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;
        }

        private static LinkLabel MakeLink(string text, string url)
        {
            LinkLabel link = new LinkLabel();
            link.Text = text;
            link.AutoSize = false;
            link.LinkColor = Lime;
            link.ActiveLinkColor = Color.White;
            link.VisitedLinkColor = Lime;
            link.LinkBehavior = LinkBehavior.HoverUnderline;
            link.LinkClicked += delegate
            {
                try { Process.Start(url); }
                catch (Exception) { }
            };
            return link;
        }
    }
}
