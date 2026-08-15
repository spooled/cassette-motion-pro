/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public static class VideoSaveTarget
    {
        public const string CancelSaveToken = "__CASSETTE_VIDEO_SAVE_CANCEL__";

        private static string beforeFolderPath;
        private static string afterFolderPath;

        public static event Action<string, string> VideoSaved;

        public static void SetActiveFolders(string beforeFolder, string afterFolder)
        {
            beforeFolderPath = beforeFolder;
            afterFolderPath = afterFolder;
        }

        public static void Clear()
        {
            beforeFolderPath = null;
            afterFolderPath = null;
        }

        public static string ChooseSavePath(IWin32Window owner, string suggestedFileName, string preferredFormat)
        {
            if (string.IsNullOrWhiteSpace(beforeFolderPath) || string.IsNullOrWhiteSpace(afterFolderPath))
            {
                MessageBox.Show(
                    owner,
                    "Open a client fit session first so Cassette Motion Pro knows where the Before and After video folders are.",
                    "Save video",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return CancelSaveToken;
            }

            using (Form dialog = new Form())
            {
                dialog.Text = "Save video";
                dialog.StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ClientSize = new System.Drawing.Size(430, 190);

                Label title = new Label();
                title.Text = "Save this Kinovea video as:";
                title.Left = 18;
                title.Top = 18;
                title.Width = 390;
                title.Height = 22;
                title.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

                Label body = new Label();
                body.Text = "Choose Before or After to save straight into this client's video folders. Regular Save keeps Kinovea's normal save window.";
                body.Left = 18;
                body.Top = 48;
                body.Width = 390;
                body.Height = 48;

                Button before = Button("Before Video", 18, 115);
                Button after = Button("After Video", 124, 115);
                Button regular = Button("Regular Save", 230, 115);
                Button cancel = Button("Cancel", 340, 115);

                before.Click += delegate
                {
                    dialog.Tag = BuildPath("Before", beforeFolderPath, suggestedFileName, preferredFormat);
                    dialog.DialogResult = DialogResult.OK;
                };

                after.Click += delegate
                {
                    dialog.Tag = BuildPath("After", afterFolderPath, suggestedFileName, preferredFormat);
                    dialog.DialogResult = DialogResult.OK;
                };

                regular.Click += delegate
                {
                    dialog.Tag = string.Empty;
                    dialog.DialogResult = DialogResult.OK;
                };

                cancel.Click += delegate
                {
                    dialog.Tag = CancelSaveToken;
                    dialog.DialogResult = DialogResult.Cancel;
                };

                dialog.Controls.Add(title);
                dialog.Controls.Add(body);
                dialog.Controls.Add(before);
                dialog.Controls.Add(after);
                dialog.Controls.Add(regular);
                dialog.Controls.Add(cancel);
                dialog.AcceptButton = before;
                dialog.CancelButton = cancel;

                DialogResult result = dialog.ShowDialog(owner);
                if (result == DialogResult.Cancel)
                    return CancelSaveToken;

                return Convert.ToString(dialog.Tag);
            }
        }

        public static void NotifyVideoSaved(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string slot = null;
            if (IsInside(path, beforeFolderPath))
                slot = "Before";
            else if (IsInside(path, afterFolderPath))
                slot = "After";

            if (slot == null)
                return;

            Action<string, string> handler = VideoSaved;
            if (handler != null)
                handler(slot, path);
        }

        private static Button Button(string text, int left, int top)
        {
            Button button = new Button();
            button.Text = text;
            button.Left = left;
            button.Top = top;
            if (text == "Regular Save")
                button.Width = 104;
            else if (text == "Cancel")
                button.Width = 72;
            else
                button.Width = 100;
            button.Height = 30;
            return button;
        }

        private static string BuildPath(string slot, string folderPath, string suggestedFileName, string preferredFormat)
        {
            Directory.CreateDirectory(folderPath);

            string name = Path.GetFileNameWithoutExtension(suggestedFileName);
            if (string.IsNullOrWhiteSpace(name))
                name = "video";

            string extension = GetExtension(preferredFormat);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string fileName = slot + "-ExportVideo-" + stamp + "-" + SanitizeFileName(name) + extension;
            return Path.Combine(folderPath, fileName);
        }

        private static string GetExtension(string preferredFormat)
        {
            if (string.Equals(preferredFormat, "MP4", StringComparison.OrdinalIgnoreCase))
                return ".mp4";
            if (string.Equals(preferredFormat, "AVI", StringComparison.OrdinalIgnoreCase))
                return ".avi";
            return ".mkv";
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '-');

            return value.Trim();
        }

        private static bool IsInside(string filePath, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(folderPath))
                return false;

            string fullFile = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullFolder = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullFile.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase);
        }
    }
}
