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
        private static string dualFolderPath;

        public static event Action<string, string> VideoSaved;

        public static void SetActiveFolders(string beforeFolder, string afterFolder)
        {
            string parent = string.IsNullOrWhiteSpace(beforeFolder) ? null : GetSessionVideosRoot(beforeFolder);
            SetActiveFolders(beforeFolder, afterFolder, string.IsNullOrWhiteSpace(parent) ? null : Path.Combine(parent, "Dual"));
        }

        public static void SetActiveFolders(string beforeFolder, string afterFolder, string dualFolder)
        {
            string root = GetBestSessionVideosRoot(beforeFolder, afterFolder, dualFolder);
            beforeFolderPath = string.IsNullOrWhiteSpace(root) ? beforeFolder : Path.Combine(root, "Before");
            afterFolderPath = string.IsNullOrWhiteSpace(root) ? afterFolder : Path.Combine(root, "After");
            dualFolderPath = string.IsNullOrWhiteSpace(root) ? dualFolder : Path.Combine(root, "Dual");
        }

        public static void Clear()
        {
            beforeFolderPath = null;
            afterFolderPath = null;
            dualFolderPath = null;
        }

        public static string ChooseSavePath(IWin32Window owner, string suggestedFileName, string preferredFormat)
        {
            if (string.IsNullOrWhiteSpace(beforeFolderPath) || string.IsNullOrWhiteSpace(afterFolderPath) || string.IsNullOrWhiteSpace(dualFolderPath))
            {
                MessageBox.Show(
                    owner,
                    "Create or open a client fit session first so Cassette Motion Pro knows where to save this video.\n\n" +
                    "Open Client Fits, start the client’s fit session, then click Save. After that, return to Video Studio and click Save Video again to choose Before, After, or Dual.",
                    "Cassette Motion Pro — Save Video",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return CancelSaveToken;
            }

            using (BeforeAfterVideoSaveDialog dialog = new BeforeAfterVideoSaveDialog(beforeFolderPath, afterFolderPath, dualFolderPath))
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

                if (string.Equals(dialog.SelectedSlot, "Dual", StringComparison.OrdinalIgnoreCase))
                    return BuildPath("Dual", dualFolderPath, suggestedFileName, preferredFormat);

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
            else if (IsInside(path, dualFolderPath))
                slot = "Dual";

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
                "Cassette Motion Pro — Save Video",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static string BuildPath(string slot, string folderPath, string suggestedFileName, string preferredFormat)
        {
            Directory.CreateDirectory(folderPath);

            string name = Path.GetFileNameWithoutExtension(suggestedFileName);
            if (string.IsNullOrWhiteSpace(name))
                name = "CassetteMotionPro-Video";

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

        private static string GetBestSessionVideosRoot(string beforeFolder, string afterFolder, string dualFolder)
        {
            string root = GetSessionVideosRoot(beforeFolder);
            if (!string.IsNullOrWhiteSpace(root))
                return root;

            root = GetSessionVideosRoot(afterFolder);
            if (!string.IsNullOrWhiteSpace(root))
                return root;

            return GetSessionVideosRoot(dualFolder);
        }

        private static string GetSessionVideosRoot(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return null;

            string normalized = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folderName = Path.GetFileName(normalized);

            if (string.Equals(folderName, "Before", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(folderName, "After", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(folderName, "Dual", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetDirectoryName(normalized);
            }

            return normalized;
        }

        private sealed class BeforeAfterVideoSaveDialog : Form
        {
            private readonly Button beforeButton;
            private readonly Button afterButton;
            private readonly Button dualButton;
            private readonly Button regularButton;
            private readonly Button cancelButton;

            public string SelectedSlot { get; private set; }

            public BeforeAfterVideoSaveDialog(string beforeFolder, string afterFolder, string dualFolder)
            {
                Text = "Cassette Motion Pro — Save Video";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(620, 236);

                Label titleLabel = new Label();
                titleLabel.AutoSize = false;
                titleLabel.Text = "Save this Video Studio video into the client fit session:";
                titleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                titleLabel.Location = new Point(18, 18);
                titleLabel.Size = new Size(580, 22);

                Label folderLabel = new Label();
                folderLabel.AutoSize = false;
                folderLabel.Text = "Choose where this video belongs:\nBefore: " + beforeFolder + "\nAfter: " + afterFolder + "\nDual: " + dualFolder;
                folderLabel.ForeColor = SystemColors.GrayText;
                folderLabel.Location = new Point(18, 54);
                folderLabel.Size = new Size(580, 76);

                Label hintLabel = new Label();
                hintLabel.AutoSize = false;
                hintLabel.Text = "Regular Save opens the standard save dialog when you do not want to attach this video to the client.";
                hintLabel.ForeColor = SystemColors.GrayText;
                hintLabel.Location = new Point(18, 134);
                hintLabel.Size = new Size(580, 18);

                beforeButton = CreateButton("Before", 18, 176, 82);
                afterButton = CreateButton("After", 116, 176, 82);
                dualButton = CreateButton("Dual", 214, 176, 82);
                regularButton = CreateButton("Regular Save", 312, 176, 104);
                cancelButton = CreateButton("Cancel", 432, 176, 82);

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

                dualButton.Click += delegate
                {
                    SelectedSlot = "Dual";
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
                Controls.Add(dualButton);
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
