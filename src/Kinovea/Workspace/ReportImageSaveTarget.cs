/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal static class ReportImageSaveTarget
    {
        private static string activeFolderPath;

        public static event Action<string, string> ReportImageSaved;

        public static void SetActiveFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            activeFolderPath = folderPath;
            Directory.CreateDirectory(activeFolderPath);
        }

        public static void Clear()
        {
            activeFolderPath = null;
        }

        public static bool TrySave(IWin32Window owner, Bitmap bitmap, string suggestedFileName)
        {
            if (bitmap == null || string.IsNullOrWhiteSpace(activeFolderPath))
                return false;

            Directory.CreateDirectory(activeFolderPath);

            using (BeforeAfterReportImageDialog dialog = new BeforeAfterReportImageDialog(activeFolderPath))
            {
                DialogResult result = dialog.ShowDialog(owner);
                if (result == DialogResult.Ignore)
                    return false;
                if (result != DialogResult.OK)
                    return true;

                string path = BuildUniquePath(activeFolderPath, BuildFileName(dialog.SelectedSlot, suggestedFileName));
                using (Bitmap copy = new Bitmap(bitmap))
                {
                    copy.Save(path, ImageFormat.Png);
                }

                NotifyReportImageSaved(dialog.SelectedSlot, path);

                MessageBox.Show(
                    owner,
                    dialog.SelectedSlot + " report image saved to this fit session:\n\n" + path,
                    "Save report image",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
        }

        private static void NotifyReportImageSaved(string slot, string path)
        {
            Action<string, string> reportImageSaved = ReportImageSaved;
            if (reportImageSaved == null)
                return;

            try
            {
                reportImageSaved(slot, path);
            }
            catch
            {
                // The image has already been saved. Keep the Kinovea save action successful even
                // if the workspace cannot automatically attach the image for some reason.
            }
        }

        private static string BuildFileName(string slot, string suggestedFileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(suggestedFileName);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "Kinovea";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(invalid, '-');

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            return slot + "-ReportImage-" + timestamp + "-" + baseName + ".png";
        }

        private static string BuildUniquePath(string folderPath, string fileName)
        {
            string candidate = Path.Combine(folderPath, fileName);
            if (!File.Exists(candidate))
                return candidate;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            for (int index = 2; index < 1000; index++)
            {
                candidate = Path.Combine(folderPath, name + "-" + index.ToString(CultureInfo.InvariantCulture) + extension);
                if (!File.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(folderPath, name + "-" + Guid.NewGuid().ToString("N") + extension);
        }
    }

    internal class BeforeAfterReportImageDialog : Form
    {
        public string SelectedSlot { get; private set; }

        public BeforeAfterReportImageDialog(string folderPath)
        {
            Text = "Save report image";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 178);

            Label title = new Label();
            title.Text = "Save this Kinovea image as:";
            title.Font = new Font(Font, FontStyle.Bold);
            title.AutoSize = false;
            title.Location = new Point(18, 16);
            title.Size = new Size(380, 24);
            Controls.Add(title);

            Label folder = new Label();
            folder.Text = "Active report folder:\n" + folderPath;
            folder.AutoSize = false;
            folder.Location = new Point(18, 46);
            folder.Size = new Size(382, 48);
            folder.ForeColor = SystemColors.ControlDarkDark;
            Controls.Add(folder);

            Button before = CreateButton("Before", 18, DialogResult.OK);
            before.Click += delegate { SelectedSlot = "Before"; };
            Controls.Add(before);

            Button after = CreateButton("After", 116, DialogResult.OK);
            after.Click += delegate { SelectedSlot = "After"; };
            Controls.Add(after);

            Button regular = CreateButton("Regular Save", 214, DialogResult.Ignore);
            regular.Width = 92;
            Controls.Add(regular);

            Button cancel = CreateButton("Cancel", 320, DialogResult.Cancel);
            Controls.Add(cancel);

            AcceptButton = before;
            CancelButton = cancel;
        }

        private static Button CreateButton(string text, int left, DialogResult result)
        {
            Button button = new Button();
            button.Text = text;
            button.DialogResult = result;
            button.Location = new Point(left, 116);
            button.Size = new Size(82, 32);
            return button;
        }
    }
}
