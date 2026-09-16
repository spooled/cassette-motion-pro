/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal enum PracticeModeAction
    {
        None,
        RecordDual,
        PlayLatest
    }

    internal sealed class FitDayPracticeModeForm : Form
    {
        private readonly string practiceRoot;
        private readonly bool captureConnected;
        private readonly bool playbackConnected;
        private readonly bool clientFolderReady;
        private readonly bool storageReady;
        private readonly string storageDetail;
        private readonly bool calibrationReady;
        private readonly ListView checklist = new ListView();
        private readonly Label status = new Label();
        private readonly Label lastTested = new Label();
        private readonly CheckBox cameraConfirmed = new CheckBox();
        private readonly CheckBox playbackConfirmed = new CheckBox();
        private Button playLatest;

        public PracticeModeAction RequestedAction { get; private set; }
        public string CameraOneFolder { get { return Path.Combine(practiceRoot, "Camera 1"); } }
        public string CameraTwoFolder { get { return Path.Combine(practiceRoot, "Camera 2"); } }
        public string LatestCameraOneVideo { get; private set; }
        public string LatestCameraTwoVideo { get; private set; }

        public FitDayPracticeModeForm(string practiceRoot, bool captureConnected, bool playbackConnected, bool clientFolderReady, bool storageReady, string storageDetail, bool calibrationReady)
        {
            this.practiceRoot = practiceRoot;
            this.captureConnected = captureConnected;
            this.playbackConnected = playbackConnected;
            this.clientFolderReady = clientFolderReady;
            this.storageReady = storageReady;
            this.storageDetail = storageDetail;
            this.calibrationReady = calibrationReady;
            Text = "Fit-Day Readiness and Practice Mode";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 660);
            Size = new Size(980, 760);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22);
            layout.ColumnCount = 1;
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));

            Label eyebrow = new Label();
            eyebrow.Text = "SAFE PRE-FIT REHEARSAL";
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);

            Label title = new Label();
            title.Text = "Confirm cameras and playback before the rider arrives";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);

            status.Dock = DockStyle.Fill;
            status.Padding = new Padding(12, 8, 12, 6);
            status.BackColor = Color.FromArgb(248, 252, 238);
            status.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            checklist.Dock = DockStyle.Fill;
            checklist.View = View.Details;
            checklist.FullRowSelect = true;
            checklist.GridLines = true;
            checklist.Columns.Add("Status", 90);
            checklist.Columns.Add("Readiness check", 240);
            checklist.Columns.Add("Result", 560);

            FlowLayoutPanel confirmations = new FlowLayoutPanel();
            confirmations.Dock = DockStyle.Fill;
            confirmations.FlowDirection = FlowDirection.TopDown;
            cameraConfirmed.Text = "Both camera views, framing, and height are correct.";
            cameraConfirmed.AutoSize = true;
            playbackConfirmed.Text = "I played both test clips and confirmed usable video.";
            playbackConfirmed.AutoSize = true;
            confirmations.Controls.Add(cameraConfirmed);
            confirmations.Controls.Add(playbackConfirmed);

            lastTested.Dock = DockStyle.Fill;
            lastTested.ForeColor = Color.FromArgb(74, 87, 81);
            lastTested.Padding = new Padding(0, 8, 0, 0);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = true;
            Button record = CreateButton("Record Dual Test", true, RequestRecord);
            playLatest = CreateButton("Play Latest Test", false, RequestPlayback);
            Button ready = CreateButton("Mark Ready for Fit", true, MarkReady);
            Button folder = CreateButton("Open Practice Folder", false, OpenFolder);
            Button clean = CreateButton("Clean Practice Files", false, CleanPracticeFiles);
            Button close = CreateButton("Close", false, delegate { Close(); });
            actions.Controls.Add(record);
            actions.Controls.Add(playLatest);
            actions.Controls.Add(ready);
            actions.Controls.Add(folder);
            actions.Controls.Add(clean);
            actions.Controls.Add(close);

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(status, 0, 2);
            layout.Controls.Add(checklist, 0, 3);
            layout.Controls.Add(confirmations, 0, 4);
            layout.Controls.Add(lastTested, 0, 5);
            layout.Controls.Add(actions, 0, 6);
            Controls.Add(layout);
            Shown += delegate { RefreshReadiness(); };
        }

        private static Button CreateButton(string text, bool primary, Action action)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(text.Length > 17 ? 170 : 145, 38);
            button.Margin = new Padding(0, 6, 8, 4);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = primary ? Color.FromArgb(155, 205, 38) : Color.White;
            button.Click += delegate { if (action != null) action(); };
            return button;
        }

        private void RefreshReadiness()
        {
            try
            {
                Directory.CreateDirectory(CameraOneFolder);
                Directory.CreateDirectory(CameraTwoFolder);
            }
            catch
            {
                // Folder checks below will show the actionable failure without closing Practice Mode.
            }
            LatestCameraOneVideo = FindLatestVideo(CameraOneFolder);
            LatestCameraTwoVideo = FindLatestVideo(CameraTwoFolder);

            List<FitDayDiagnosticResult> checks = new List<FitDayDiagnosticResult>();
            checks.Add(NewResult(clientFolderReady ? "PASS" : "FAIL", "Client workspace", clientFolderReady ? "Practice files have a valid client workspace." : "Client folder is unavailable."));
            checks.Add(NewResult(storageReady ? "PASS" : "FAIL", "Recording storage", storageDetail));
            checks.Add(NewResult(captureConnected ? "PASS" : "FAIL", "Dual capture connection", captureConnected ? "Practice folders can be sent to Video Studio capture." : "Live capture is unavailable."));
            checks.Add(NewResult(playbackConnected ? "PASS" : "FAIL", "Dual playback connection", playbackConnected ? "Two test clips can be opened together." : "Dual playback is unavailable."));
            checks.Add(NewResult(calibrationReady ? "PASS" : "WARN", "Measurement calibration", calibrationReady ? "The active session has a saved calibration and accuracy review." : "No saved calibration review is available yet; complete it after the physical setup is in place."));
            checks.Add(TestFolder("Camera 1 practice folder", CameraOneFolder));
            checks.Add(TestFolder("Camera 2 practice folder", CameraTwoFolder));
            checks.Add(NewResult(string.IsNullOrWhiteSpace(LatestCameraOneVideo) ? "WARN" : "PASS", "Camera 1 test clip", string.IsNullOrWhiteSpace(LatestCameraOneVideo) ? "Record a short test clip." : Path.GetFileName(LatestCameraOneVideo)));
            checks.Add(NewResult(string.IsNullOrWhiteSpace(LatestCameraTwoVideo) ? "WARN" : "PASS", "Camera 2 test clip", string.IsNullOrWhiteSpace(LatestCameraTwoVideo) ? "Record a short test clip." : Path.GetFileName(LatestCameraTwoVideo)));

            checklist.Items.Clear();
            int failures = 0;
            int warnings = 0;
            foreach (FitDayDiagnosticResult check in checks)
            {
                ListViewItem row = new ListViewItem(check.Status);
                row.SubItems.Add(check.Area);
                row.SubItems.Add(check.Detail);
                if (check.Status == "FAIL") { failures++; row.ForeColor = Color.FromArgb(178, 62, 62); }
                else if (check.Status == "WARN") { warnings++; row.ForeColor = Color.FromArgb(181, 118, 35); }
                else row.ForeColor = Color.FromArgb(52, 130, 68);
                checklist.Items.Add(row);
            }
            status.Text = failures > 0 ? "NOT READY · Resolve failed setup checks." : warnings > 0 ? "PRACTICE NEEDED · Record and review both camera tests." : "TEST CLIPS FOUND · Play them and confirm readiness below.";
            status.ForeColor = failures > 0 ? Color.FromArgb(178, 62, 62) : warnings > 0 ? Color.FromArgb(181, 118, 35) : Color.FromArgb(52, 130, 68);
            playLatest.Enabled = playbackConnected && !string.IsNullOrWhiteSpace(LatestCameraOneVideo) && !string.IsNullOrWhiteSpace(LatestCameraTwoVideo);

            string reportPath = Path.Combine(practiceRoot, "Last Readiness Report.txt");
            lastTested.Text = File.Exists(reportPath) ? "Last Ready for Fit confirmation: " + File.GetLastWriteTime(reportPath).ToString("MMM d, yyyy h:mm tt") : "No saved Ready for Fit confirmation yet.";
        }

        private static FitDayDiagnosticResult NewResult(string status, string area, string detail)
        {
            return new FitDayDiagnosticResult { Status = status, Area = area, Detail = detail };
        }

        private static FitDayDiagnosticResult TestFolder(string area, string folder)
        {
            string probe = Path.Combine(folder, ".practice-write-test.tmp");
            try
            {
                File.WriteAllText(probe, "Cassette Motion Pro practice test");
                File.Delete(probe);
                return NewResult("PASS", area, "Writable: " + folder);
            }
            catch (Exception exception)
            {
                try { if (File.Exists(probe)) File.Delete(probe); } catch { }
                return NewResult("FAIL", area, exception.Message);
            }
        }

        private static string FindLatestVideo(string folder)
        {
            if (!Directory.Exists(folder))
                return string.Empty;
            string[] extensions = new string[] { ".mp4", ".avi", ".mov", ".mkv", ".m4v", ".wmv" };
            string latest = string.Empty;
            DateTime latestUtc = DateTime.MinValue;
            foreach (string path in Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                if (Array.IndexOf(extensions, extension) < 0)
                    continue;
                DateTime written = File.GetLastWriteTimeUtc(path);
                if (written > latestUtc) { latestUtc = written; latest = path; }
            }
            return latest;
        }

        private void RequestRecord()
        {
            if (!captureConnected)
            {
                MessageBox.Show(this, "Dual live capture is not connected.", "Practice Mode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            RequestedAction = PracticeModeAction.RecordDual;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void RequestPlayback()
        {
            if (!playLatest.Enabled)
                return;
            RequestedAction = PracticeModeAction.PlayLatest;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void MarkReady()
        {
            RefreshReadiness();
            if (!clientFolderReady || !storageReady || !captureConnected || !playbackConnected || string.IsNullOrWhiteSpace(LatestCameraOneVideo) || string.IsNullOrWhiteSpace(LatestCameraTwoVideo))
            {
                MessageBox.Show(this, "Record and locate both practice clips before marking the system ready.", "Practice Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!cameraConfirmed.Checked || !playbackConfirmed.Checked)
            {
                MessageBox.Show(this, "Confirm the camera setup and test playback first.", "Practice Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("Cassette Motion Pro — Ready for Fit");
            report.AppendLine("Confirmed: " + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt"));
            report.AppendLine("Camera 1 test: " + LatestCameraOneVideo);
            report.AppendLine("Camera 2 test: " + LatestCameraTwoVideo);
            report.AppendLine("Camera views and framing confirmed: Yes");
            report.AppendLine("Dual playback confirmed: Yes");
            report.AppendLine("Measurement calibration saved: " + (calibrationReady ? "Yes" : "Not yet — complete during physical setup"));
            try
            {
                File.WriteAllText(Path.Combine(practiceRoot, "Last Readiness Report.txt"), report.ToString());
                RefreshReadiness();
                MessageBox.Show(this, "The camera and playback setup is marked Ready for Fit.", "Practice Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The readiness report could not be saved.\n\n" + exception.Message, "Practice Mode", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(practiceRoot);
                Process.Start(practiceRoot);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The practice folder could not be opened.\n\n" + exception.Message, "Practice Mode", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanPracticeFiles()
        {
            DialogResult result = MessageBox.Show(this, "Delete all temporary practice recordings and the saved readiness report?\n\nReal client session media will not be touched.", "Clean Practice Files", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
            try
            {
                if (Directory.Exists(practiceRoot))
                    Directory.Delete(practiceRoot, true);
                cameraConfirmed.Checked = false;
                playbackConfirmed.Checked = false;
                RefreshReadiness();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Practice files could not be cleaned.\n\n" + exception.Message, "Practice Mode", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
