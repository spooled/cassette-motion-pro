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
        private static string beforeFolderPath;
        private static string afterFolderPath;
        private static string dualFolderPath;

        public static event Action<string, string> ReportImageSaved;

        public static void SetActiveFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            activeFolderPath = folderPath;
            Directory.CreateDirectory(activeFolderPath);
            beforeFolderPath = Path.Combine(activeFolderPath, "Before");
            afterFolderPath = Path.Combine(activeFolderPath, "After");
            dualFolderPath = Path.Combine(activeFolderPath, "Dual");
            Directory.CreateDirectory(beforeFolderPath);
            Directory.CreateDirectory(afterFolderPath);
            Directory.CreateDirectory(dualFolderPath);
        }

        public static void Clear()
        {
            activeFolderPath = null;
            beforeFolderPath = null;
            afterFolderPath = null;
            dualFolderPath = null;
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

                string folderPath = GetFolderForSlot(dialog.SelectedSlot);
                Directory.CreateDirectory(folderPath);
                string path = BuildUniquePath(folderPath, BuildFileName(dialog.SelectedSlot, suggestedFileName));
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

        private static string GetFolderForSlot(string slot)
        {
            if (string.Equals(slot, "After", StringComparison.OrdinalIgnoreCase))
                return afterFolderPath;
            if (string.Equals(slot, "Dual", StringComparison.OrdinalIgnoreCase))
                return dualFolderPath;
            return beforeFolderPath;
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
            ClientSize = new Size(540, 178);

            Label title = new Label();
            title.Text = "Save this Kinovea image as:";
            title.Font = new Font(Font, FontStyle.Bold);
            title.AutoSize = false;
            title.Location = new Point(18, 16);
            title.Size = new Size(500, 24);
            Controls.Add(title);

            Label folder = new Label();
            folder.Text = "Active report folders:\nBefore / After / Dual inside:\n" + folderPath;
            folder.AutoSize = false;
            folder.Location = new Point(18, 46);
            folder.Size = new Size(500, 48);
            folder.ForeColor = SystemColors.ControlDarkDark;
            Controls.Add(folder);

            Button before = CreateButton("Before", 18, DialogResult.OK);
            before.Click += delegate { SelectedSlot = "Before"; };
            Controls.Add(before);

            Button after = CreateButton("After", 116, DialogResult.OK);
            after.Click += delegate { SelectedSlot = "After"; };
            Controls.Add(after);

            Button dual = CreateButton("Dual", 214, DialogResult.OK);
            dual.Click += delegate { SelectedSlot = "Dual"; };
            Controls.Add(dual);

            Button regular = CreateButton("Regular Save", 312, DialogResult.Ignore);
            regular.Width = 92;
            Controls.Add(regular);

            Button cancel = CreateButton("Cancel", 424, DialogResult.Cancel);
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
