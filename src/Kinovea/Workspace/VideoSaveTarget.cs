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
                    "Open a client fit session first so Cassette Motion Pro knows where the Before and After video folders are.\n\n" +
                    "Go to Clients, open the client, then open the fit session. After that, come back to Kinovea and click Save video again.",
                    "Save video",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return CancelSaveToken;
            }

            using (BeforeAfterVideoSaveDialog dialog = new BeforeAfterVideoSaveDialog(beforeFolderPath, afterFolderPath))
            {
                dialog.StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;

                DialogResult result = dialog.ShowDialog(owner);
                if (result == DialogResult.Ignore)
                    return string.Empty;

                if (result != DialogResult.OK)
                    return CancelSaveToken;

                if (string.Equals(dialog.SelectedSlot, "Before", StringComparison.OrdinalIgnoreCase))
                    return BuildPath("Before", beforeFolderPath, suggestedFileName, preferredFormat);

                if (string.Equals(dialog.SelectedSlot, "After", StringComparison.OrdinalIgnoreCase))
                    return BuildPath("After", afterFolderPath, suggestedFileName, preferredFormat);

                return CancelSaveToken;
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
            {
                try
                {
                    handler(slot, path);
                }
                catch
                {
                    // The video was already saved. Keep the Kinovea save action successful even
                    // if the workspace cannot automatically attach the video for some reason.
                }
            }

            MessageBox.Show(
                slot + " video saved to this fit session:\n\n" + path,
                "Save video",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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

        private sealed class BeforeAfterVideoSaveDialog : Form
        {
            private readonly Button beforeButton;
            private readonly Button afterButton;
            private readonly Button regularButton;
            private readonly Button cancelButton;

            public string SelectedSlot { get; private set; }

            public BeforeAfterVideoSaveDialog(string beforeFolder, string afterFolder)
            {
                Text = "Save video";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(500, 214);

                Label titleLabel = new Label();
                titleLabel.AutoSize = false;
                titleLabel.Text = "Save this Kinovea video as:";
                titleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                titleLabel.Location = new Point(18, 18);
                titleLabel.Size = new Size(455, 22);

                Label folderLabel = new Label();
                folderLabel.AutoSize = false;
                folderLabel.Text = "Active video folders:\nBefore: " + beforeFolder + "\nAfter: " + afterFolder;
                folderLabel.ForeColor = SystemColors.GrayText;
                folderLabel.Location = new Point(18, 54);
                folderLabel.Size = new Size(455, 58);

                Label hintLabel = new Label();
                hintLabel.AutoSize = false;
                hintLabel.Text = "Regular Save keeps Kinovea's normal save dialog.";
                hintLabel.ForeColor = SystemColors.GrayText;
                hintLabel.Location = new Point(18, 114);
                hintLabel.Size = new Size(455, 18);

                beforeButton = CreateButton("Before", 18, 154, 82);
                afterButton = CreateButton("After", 116, 154, 82);
                regularButton = CreateButton("Regular Save", 214, 154, 104);
                cancelButton = CreateButton("Cancel", 334, 154, 82);

                beforeButton.Click += delegate
                {
                    SelectedSlot = "Before";
                    DialogResult = DialogResult.OK;
                };

                afterButton.Click += delegate
                {
                    SelectedSlot = "After";
                    DialogResult = DialogResult.OK;
                };

                regularButton.Click += delegate
                {
                    DialogResult = DialogResult.Ignore;
                };

                cancelButton.Click += delegate
                {
                    DialogResult = DialogResult.Cancel;
                };

                Controls.Add(titleLabel);
                Controls.Add(folderLabel);
                Controls.Add(hintLabel);
                Controls.Add(beforeButton);
                Controls.Add(afterButton);
                Controls.Add(regularButton);
                Controls.Add(cancelButton);

                AcceptButton = beforeButton;
                CancelButton = cancelButton;
            }

            private static Button CreateButton(string text, int left, int top, int width)
            {
                Button button = new Button();
                button.Text = text;
                button.Location = new Point(left, top);
                button.Size = new Size(width, 32);
                return button;
            }
        }
    }
}
