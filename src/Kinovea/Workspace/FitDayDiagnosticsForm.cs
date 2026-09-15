/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class FitDayDiagnosticResult
    {
        public string Status { get; set; }
        public string Area { get; set; }
        public string Detail { get; set; }
    }

    internal sealed class FitDayDiagnosticsForm : Form
    {
        private readonly Func<List<FitDayDiagnosticResult>> runDiagnostics;
        private readonly string saveFolder;
        private readonly ListView results = new ListView();
        private readonly Label summary = new Label();
        private List<FitDayDiagnosticResult> latest = new List<FitDayDiagnosticResult>();

        public FitDayDiagnosticsForm(Func<List<FitDayDiagnosticResult>> runDiagnostics, string saveFolder)
        {
            this.runDiagnostics = runDiagnostics;
            this.saveFolder = saveFolder;
            Text = "Fit-Day Diagnostics";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(780, 560);
            Size = new Size(980, 700);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22);
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            Label eyebrow = new Label();
            eyebrow.Text = "FIT-DAY RELIABILITY TESTS";
            eyebrow.Dock = DockStyle.Fill;
            eyebrow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            eyebrow.ForeColor = Color.FromArgb(85, 122, 18);

            Label title = new Label();
            title.Text = "Check the workflow before the rider arrives";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(24, 31, 29);

            summary.Dock = DockStyle.Fill;
            summary.Padding = new Padding(12, 8, 12, 6);
            summary.BackColor = Color.FromArgb(248, 252, 238);
            summary.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            results.Dock = DockStyle.Fill;
            results.View = View.Details;
            results.FullRowSelect = true;
            results.GridLines = true;
            results.Columns.Add("Status", 90);
            results.Columns.Add("Area", 210);
            results.Columns.Add("Result and next action", 610);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            Button rerun = CreateButton("Run Tests Again", true, RunTests);
            Button copy = CreateButton("Copy Results", false, CopyResults);
            Button save = CreateButton("Save Diagnostic Report", false, SaveResults);
            Button close = CreateButton("Close", false, delegate { Close(); });
            actions.Controls.Add(rerun);
            actions.Controls.Add(copy);
            actions.Controls.Add(save);
            actions.Controls.Add(close);

            layout.Controls.Add(eyebrow, 0, 0);
            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(summary, 0, 2);
            layout.Controls.Add(results, 0, 3);
            layout.Controls.Add(actions, 0, 4);
            Controls.Add(layout);
            Shown += delegate { RunTests(); };
        }

        private static Button CreateButton(string text, bool primary, Action action)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(text.Length > 15 ? 175 : 130, 38);
            button.Margin = new Padding(0, 6, 8, 4);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = primary ? Color.FromArgb(155, 205, 38) : Color.White;
            button.Click += delegate { if (action != null) action(); };
            return button;
        }

        private void RunTests()
        {
            UseWaitCursor = true;
            try
            {
                latest = runDiagnostics == null ? new List<FitDayDiagnosticResult>() : runDiagnostics();
                results.BeginUpdate();
                results.Items.Clear();
                int pass = 0;
                int warning = 0;
                int failure = 0;
                foreach (FitDayDiagnosticResult item in latest)
                {
                    ListViewItem row = new ListViewItem(item.Status);
                    row.SubItems.Add(item.Area);
                    row.SubItems.Add(item.Detail);
                    if (item.Status == "PASS") { pass++; row.ForeColor = Color.FromArgb(52, 130, 68); }
                    else if (item.Status == "WARN") { warning++; row.ForeColor = Color.FromArgb(181, 118, 35); }
                    else { failure++; row.ForeColor = Color.FromArgb(178, 62, 62); }
                    results.Items.Add(row);
                }
                results.EndUpdate();
                summary.Text = "PASS " + pass.ToString() + "   ·   WARN " + warning.ToString() + "   ·   FAIL " + failure.ToString() + Environment.NewLine +
                    (failure == 0 ? warning == 0 ? "Fit-day system checks passed." : "Core checks passed; review the setup warnings below." : "Resolve failed checks before relying on automatic session saves.");
                summary.ForeColor = failure > 0 ? Color.FromArgb(178, 62, 62) : warning > 0 ? Color.FromArgb(181, 118, 35) : Color.FromArgb(52, 130, 68);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Diagnostics could not finish.\n\n" + exception.Message, "Fit-Day Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { UseWaitCursor = false; }
        }

        private string BuildTextReport()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Cassette Motion Pro Fit-Day Diagnostics");
            text.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt"));
            text.AppendLine(summary.Text.Replace(Environment.NewLine, " "));
            text.AppendLine();
            foreach (FitDayDiagnosticResult item in latest)
                text.AppendLine(item.Status.PadRight(6) + " | " + item.Area + " | " + item.Detail);
            return text.ToString();
        }

        private void CopyResults()
        {
            try
            {
                Clipboard.SetText(BuildTextReport());
                MessageBox.Show(this, "Diagnostic results copied.", "Fit-Day Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Results could not be copied.\n\n" + exception.Message, "Fit-Day Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveResults()
        {
            try
            {
                Directory.CreateDirectory(saveFolder);
                string path = Path.Combine(saveFolder, "Fit-Day Diagnostics " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".txt");
                File.WriteAllText(path, BuildTextReport());
                MessageBox.Show(this, "Diagnostic report saved.\n\n" + path, "Fit-Day Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "The diagnostic report could not be saved.\n\n" + exception.Message, "Fit-Day Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
