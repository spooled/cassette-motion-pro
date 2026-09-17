using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    internal sealed class FitSessionTimelineForm : Form
    {
        public FitSessionTimelineForm(FitSessionRecord session)
        {
            Text = "Fit Session Timeline — " + session.DisplayName;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(980, 680);
            MinimumSize = new Size(700, 480);
            Font = new Font("Segoe UI", 9F);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(18);
            layout.RowCount = 3;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

            Label note = new Label();
            note.Dock = DockStyle.Fill;
            note.Text = "Newest first. New saves are recorded as they happen. Older session items use existing timestamps where available; undated edits are not given invented times.";
            note.Font = new Font("Segoe UI", 10F);
            layout.Controls.Add(note, 0, 0);

            ListView list = new ListView();
            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.GridLines = true;
            list.Columns.Add("When", 165);
            list.Columns.Add("Type", 135);
            list.Columns.Add("What happened", 610);
            foreach (FitSessionTimelineEvent item in BuildTimeline(session))
            {
                DateTime local = item.OccurredUtc.Kind == DateTimeKind.Utc ? item.OccurredUtc.ToLocalTime() : item.OccurredUtc;
                list.Items.Add(new ListViewItem(new[] { local == DateTime.MinValue ? "Date unavailable" : local.ToString("g"), item.Category ?? "Activity", item.Description ?? string.Empty }));
            }
            layout.Controls.Add(list, 0, 1);

            Button close = new Button();
            close.Text = "Close";
            close.Dock = DockStyle.Right;
            close.Width = 100;
            close.Click += delegate { Close(); };
            layout.Controls.Add(close, 0, 2);
            Controls.Add(layout);
        }

        private static IEnumerable<FitSessionTimelineEvent> BuildTimeline(FitSessionRecord session)
        {
            List<FitSessionTimelineEvent> events = session.TimelineEvents == null
                ? new List<FitSessionTimelineEvent>() : new List<FitSessionTimelineEvent>(session.TimelineEvents);
            if (session.CreatedUtc != DateTime.MinValue && !events.Any(item => item.Category == "Session" && item.Description == "Fit session created"))
                Add(events, session.CreatedUtc, "Session", "Fit session created");
            AddMediaIfMissing(events, session.BeforeVideoPath, "Before recording");
            AddMediaIfMissing(events, session.AfterVideoPath, "After recording");
            AddMediaIfMissing(events, session.BeforeReportImagePath, "Before report image");
            AddMediaIfMissing(events, session.AfterReportImagePath, "After report image");
            AddMediaIfMissing(events, session.SideBySideReportImagePath, "Dual report image");
            if (session.ReportApprovedUtc != DateTime.MinValue && !events.Any(item => item.Category == "Approval" && item.Description != null && item.Description.Contains("Report approved")))
                Add(events, session.ReportApprovedUtc, "Approval", "Report approved by fitter");
            if (session.AssistedWorkflowReportGeneratedUtc != DateTime.MinValue && !events.Any(item => item.Category == "Report"))
                Add(events, session.AssistedWorkflowReportGeneratedUtc, "Report", "Report generated or previewed");
            if (session.ReportDeliveryPreparedUtc != DateTime.MinValue && !events.Any(item => item.Category == "Delivery"))
                Add(events, session.ReportDeliveryPreparedUtc, "Delivery", "Report package prepared: " + session.ReportDeliveryFormat);
            if (!events.Any(item => item.Category == "Measurements") &&
                (!string.IsNullOrWhiteSpace(session.SaddleHeightBefore) || !string.IsNullOrWhiteSpace(session.SaddleHeightAfter) ||
                 !string.IsNullOrWhiteSpace(session.KneeAngleBefore) || !string.IsNullOrWhiteSpace(session.KneeAngleAfter)))
                Add(events, DateTime.MinValue, "Measurements", "Earlier measurements are saved in this session; their original entry time was not recorded.");
            return events.OrderByDescending(item => item.OccurredUtc).ThenBy(item => item.Category).ToList();
        }

        private static void AddMediaIfMissing(List<FitSessionTimelineEvent> events, string path, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;
            string file = Path.GetFileName(path);
            if (events.Any(item => item.Category == "Recording" && item.Description != null && item.Description.Contains(file)))
                return;
            if (events.Any(item => item.Category == "Image" && item.Description != null && item.Description.Contains(file)))
                return;
            try
            {
                Add(events, File.GetCreationTimeUtc(path), label.Contains("image") ? "Image" : "Recording", label + ": " + file);
            }
            catch (IOException)
            {
                // A disconnected media source should not prevent opening the timeline.
            }
            catch (UnauthorizedAccessException)
            {
                // The timeline remains useful even if an older file cannot be inspected.
            }
        }

        private static void Add(List<FitSessionTimelineEvent> events, DateTime time, string category, string description)
        {
            events.Add(new FitSessionTimelineEvent { OccurredUtc = time, Category = category, Description = description });
        }
    }
}
