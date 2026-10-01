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
        private static string bikeBeforeFolderPath;
        private static string bikeAfterFolderPath;
        private static string riderBeforeFolderPath;
        private static string riderAfterFolderPath;

        public static event Action<string, string> ReportImageSaved;

        public static void SetActiveFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            activeFolderPath = NormalizeReportImagesRoot(folderPath);
            Directory.CreateDirectory(activeFolderPath);
            beforeFolderPath = Path.Combine(activeFolderPath, "Before");
            afterFolderPath = Path.Combine(activeFolderPath, "After");
            dualFolderPath = Path.Combine(activeFolderPath, "Dual");
            string sessionPhotosFolder = Path.GetDirectoryName(activeFolderPath);
            string measurementsFolder = Path.Combine(sessionPhotosFolder, "Measurements");
            bikeBeforeFolderPath = Path.Combine(measurementsFolder, "Bike", "Before");
            bikeAfterFolderPath = Path.Combine(measurementsFolder, "Bike", "After");
            riderBeforeFolderPath = Path.Combine(measurementsFolder, "Rider", "Before");
            riderAfterFolderPath = Path.Combine(measurementsFolder, "Rider", "After");
            Directory.CreateDirectory(beforeFolderPath);
            Directory.CreateDirectory(afterFolderPath);
            Directory.CreateDirectory(dualFolderPath);
            Directory.CreateDirectory(bikeBeforeFolderPath);
            Directory.CreateDirectory(bikeAfterFolderPath);
            Directory.CreateDirectory(riderBeforeFolderPath);
            Directory.CreateDirectory(riderAfterFolderPath);
        }

        public static void Clear()
        {
            activeFolderPath = null;
            beforeFolderPath = null;
            afterFolderPath = null;
            dualFolderPath = null;
            bikeBeforeFolderPath = null;
            bikeAfterFolderPath = null;
            riderBeforeFolderPath = null;
            riderAfterFolderPath = null;
        }

        public static bool TrySave(IWin32Window owner, Bitmap bitmap, string suggestedFileName)
        {
            if (bitmap == null)
                return false;

            if (!HasActiveFitSession())
            {
                MessageBox.Show(
                    owner,
                    "Create or open a client fit session first so Cassette Motion Pro knows where to save this image.\n\n" +
                    "Open Client Fits, start the client’s fit session, then click Save. After that, return to Video Studio and click Save Image again to choose Before, After, or Dual.",
                    "Cassette Motion Pro — Save Image",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return true;
            }

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
                    GetSlotDisplayName(dialog.SelectedSlot) + " image saved to this fit session:\n\n" + path,
                    "Cassette Motion Pro — Save Image",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
        }

        private static bool HasActiveFitSession()
        {
            return !string.IsNullOrWhiteSpace(activeFolderPath) &&
                !string.IsNullOrWhiteSpace(beforeFolderPath) &&
                !string.IsNullOrWhiteSpace(afterFolderPath) &&
                !string.IsNullOrWhiteSpace(dualFolderPath) &&
                !string.IsNullOrWhiteSpace(bikeBeforeFolderPath) &&
                !string.IsNullOrWhiteSpace(bikeAfterFolderPath) &&
                !string.IsNullOrWhiteSpace(riderBeforeFolderPath) &&
                !string.IsNullOrWhiteSpace(riderAfterFolderPath);
        }

        private static string GetFolderForSlot(string slot)
        {
            if (string.Equals(slot, "After", StringComparison.OrdinalIgnoreCase))
                return afterFolderPath;
            if (string.Equals(slot, "Dual", StringComparison.OrdinalIgnoreCase))
                return dualFolderPath;
            if (string.Equals(slot, "BikeBefore", StringComparison.OrdinalIgnoreCase))
                return bikeBeforeFolderPath;
            if (string.Equals(slot, "BikeAfter", StringComparison.OrdinalIgnoreCase))
                return bikeAfterFolderPath;
            if (string.Equals(slot, "RiderBefore", StringComparison.OrdinalIgnoreCase))
                return riderBeforeFolderPath;
            if (string.Equals(slot, "RiderAfter", StringComparison.OrdinalIgnoreCase))
                return riderAfterFolderPath;
            return beforeFolderPath;
        }

        private static string GetSlotDisplayName(string slot)
        {
            if (string.Equals(slot, "BikeBefore", StringComparison.OrdinalIgnoreCase))
                return "Bike measurement · Before";
            if (string.Equals(slot, "BikeAfter", StringComparison.OrdinalIgnoreCase))
                return "Bike measurement · After";
            if (string.Equals(slot, "RiderBefore", StringComparison.OrdinalIgnoreCase))
                return "Rider measurement · Before";
            if (string.Equals(slot, "RiderAfter", StringComparison.OrdinalIgnoreCase))
                return "Rider measurement · After";
            return slot;
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
                baseName = "CassetteMotionPro";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(invalid, '-');

            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string prefix = slot.IndexOf("Bike", StringComparison.OrdinalIgnoreCase) >= 0 ||
                slot.IndexOf("Rider", StringComparison.OrdinalIgnoreCase) >= 0
                ? slot + "-MeasurementImage-"
                : slot + "-ReportImage-";
            return prefix + timestamp + "-" + baseName + ".png";
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

        private static string NormalizeReportImagesRoot(string folderPath)
        {
            string normalized = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folderName = Path.GetFileName(normalized);

            if (string.Equals(folderName, "Before", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(folderName, "After", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(folderName, "Dual", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Path.GetDirectoryName(normalized);
                if (!string.IsNullOrWhiteSpace(parent))
                    return parent;
            }

            return normalized;
        }
    }

    internal class BeforeAfterReportImageDialog : Form
    {
        public string SelectedSlot { get; private set; }

        public BeforeAfterReportImageDialog(string folderPath)
        {
            Text = "Cassette Motion Pro — Save Image";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(700, 330);

            Label title = new Label();
            title.Text = "Save this Video Studio image into the client fit session:";
            title.Font = new Font(Font, FontStyle.Bold);
            title.AutoSize = false;
            title.Location = new Point(18, 16);
            title.Size = new Size(660, 24);
            Controls.Add(title);

            Label folder = new Label();
            folder.Text = "Choose where this image belongs in the active client session.\n" + folderPath;
            folder.AutoSize = false;
            folder.Location = new Point(18, 46);
            folder.Size = new Size(660, 42);
            folder.ForeColor = SystemColors.ControlDarkDark;
            Controls.Add(folder);

            Label reportLabel = CreateSectionLabel("Report images", 102);
            Controls.Add(reportLabel);

            Button before = CreateButton("&Before", 18, 128, 118, DialogResult.OK);
            before.Click += delegate { SelectedSlot = "Before"; };
            Controls.Add(before);

            Button after = CreateButton("&After", 146, 128, 118, DialogResult.OK);
            after.Click += delegate { SelectedSlot = "After"; };
            Controls.Add(after);

            Button dual = CreateButton("&Dual", 274, 128, 118, DialogResult.OK);
            dual.Click += delegate { SelectedSlot = "Dual"; };
            Controls.Add(dual);

            Label measurementLabel = CreateSectionLabel("Measurement evidence", 188);
            Controls.Add(measurementLabel);

            Button bikeBefore = CreateButton("Bike · Before", 18, 214, 150, DialogResult.OK);
            bikeBefore.Click += delegate { SelectedSlot = "BikeBefore"; };
            Controls.Add(bikeBefore);

            Button bikeAfter = CreateButton("Bike · After", 178, 214, 150, DialogResult.OK);
            bikeAfter.Click += delegate { SelectedSlot = "BikeAfter"; };
            Controls.Add(bikeAfter);

            Button riderBefore = CreateButton("Rider · Before", 338, 214, 150, DialogResult.OK);
            riderBefore.Click += delegate { SelectedSlot = "RiderBefore"; };
            Controls.Add(riderBefore);

            Button riderAfter = CreateButton("Rider · After", 498, 214, 150, DialogResult.OK);
            riderAfter.Click += delegate { SelectedSlot = "RiderAfter"; };
            Controls.Add(riderAfter);

            Button regular = CreateButton("&Regular Save", 442, 280, 110, DialogResult.Ignore);
            Controls.Add(regular);

            Button cancel = CreateButton("&Cancel", 562, 280, 86, DialogResult.Cancel);
            Controls.Add(cancel);

            AcceptButton = before;
            CancelButton = cancel;
        }

        private static Label CreateSectionLabel(string text, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label.Location = new Point(18, top);
            label.Size = new Size(300, 20);
            return label;
        }

        private static Button CreateButton(string text, int left, int top, int width, DialogResult result)
        {
            Button button = new Button();
            button.Text = text;
            button.DialogResult = result;
            button.Location = new Point(left, top);
            button.Size = new Size(width, 46);
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            return button;
        }
    }
}
