/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using CassetteMotionPro.Clients;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace CassetteMotionPro.Workspace
{
    public static class FitSessionReportGenerator
    {
        public static string Generate(ClientRecord client, FitSessionRecord session)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (session == null)
                throw new ArgumentNullException("session");
            if (string.IsNullOrEmpty(client.ReportsPath))
                throw new InvalidOperationException("The client Reports folder is not available.");

            Directory.CreateDirectory(client.ReportsPath);

            string fileName = BuildFileName(session);
            string path = Path.Combine(client.ReportsPath, fileName);
            File.WriteAllText(path, BuildHtml(client, session), Encoding.UTF8);
            return path;
        }

        private static string BuildFileName(FitSessionRecord session)
        {
            string title = CleanFileName(string.IsNullOrWhiteSpace(session.Title) ? "Bike Fit Report" : session.Title);
            string date = session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("yyyy-MM-dd") : session.SessionDate.ToString("yyyy-MM-dd");
            return date + " - " + title + ".html";
        }

        private static string BuildHtml(ClientRecord client, FitSessionRecord session)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\">");
            html.AppendLine("<title>" + Encode(client.DisplayName) + " Bike Fit Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f3f6f4;color:#18201d;}");
            html.AppendLine(".page{max-width:960px;margin:32px auto;background:white;border-radius:18px;box-shadow:0 16px 48px rgba(12,19,16,.12);overflow:hidden;}");
            html.AppendLine(".hero{background:#0d1311;color:white;padding:34px 42px;}");
            html.AppendLine(".hero-top{display:flex;justify-content:space-between;align-items:flex-start;gap:20px;}");
            html.AppendLine(".eyebrow{color:#b8f34a;font-size:12px;font-weight:700;letter-spacing:.16em;text-transform:uppercase;}");
            html.AppendLine("h1{margin:8px 0 4px;font-size:32px;}");
            html.AppendLine(".muted{color:#718078;}");
            html.AppendLine(".hero .muted{color:#afbbb5;}");
            html.AppendLine(".print-button{border:0;border-radius:999px;background:#b8f34a;color:#0d1311;font-weight:700;padding:11px 18px;cursor:pointer;}");
            html.AppendLine(".content{padding:34px 42px 44px;}");
            html.AppendLine("h2{margin-top:30px;border-bottom:1px solid #dfe7e2;padding-bottom:8px;font-size:20px;}");
            html.AppendLine("table{width:100%;border-collapse:collapse;margin-top:12px;}");
            html.AppendLine("th,td{text-align:left;border-bottom:1px solid #edf1ee;padding:10px 8px;vertical-align:top;}");
            html.AppendLine("th{font-size:12px;text-transform:uppercase;letter-spacing:.08em;color:#5c6862;}");
            html.AppendLine(".media-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:14px;}");
            html.AppendLine(".media-card{border:1px dashed #c9d5cf;border-radius:16px;background:#f7f9f8;min-height:160px;display:flex;align-items:center;justify-content:center;text-align:center;color:#718078;position:relative;overflow:hidden;}");
            html.AppendLine(".media-card.full{margin-top:14px;}");
            html.AppendLine(".media-card img{display:block;width:100%;height:240px;object-fit:cover;}");
            html.AppendLine(".media-card.full img{height:360px;}");
            html.AppendLine(".media-label{position:absolute;left:12px;top:12px;background:rgba(13,19,17,.82);color:white;border-radius:999px;padding:6px 11px;font-size:12px;font-weight:700;}");
            html.AppendLine(".change{font-weight:700;color:#0d1311;}");
            html.AppendLine(".positive{color:#2b7c46;}.negative{color:#9b3b32;}");
            html.AppendLine(".note{white-space:pre-wrap;background:#f7f9f8;border:1px solid #e1e8e4;border-radius:12px;padding:16px;}");
            html.AppendLine(".footer{margin-top:34px;color:#718078;font-size:12px;}");
            html.AppendLine("@media print{body{background:white}.page{box-shadow:none;margin:0;max-width:none;border-radius:0}.print-button{display:none}.content{padding-bottom:20px}.media-card{min-height:110px}}");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class=\"page\">");
            html.AppendLine("<div class=\"hero\">");
            html.AppendLine("<div class=\"hero-top\">");
            html.AppendLine("<div>");
            html.AppendLine("<div class=\"eyebrow\">Cassette Motion Pro</div>");
            html.AppendLine("<h1>Bike Fit Report</h1>");
            html.AppendLine("<div class=\"muted\">" + Encode(client.DisplayName) + " · " + Encode(client.BikeDescription) + "</div>");
            html.AppendLine("</div>");
            html.AppendLine("<button class=\"print-button\" onclick=\"window.print()\">Print / Save PDF</button>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"content\">");
            html.AppendLine("<h2>Session</h2>");
            html.AppendLine("<table>");
            AddDetailRow(html, "Title", session.DisplayName);
            AddDetailRow(html, "Date", session.SessionDate == DateTime.MinValue ? DateTime.Today.ToLongDateString() : session.SessionDate.ToLongDateString());
            AddDetailRow(html, "Status", session.Status);
            AddDetailRow(html, "Client", client.DisplayName);
            AddDetailRow(html, "Bike", client.BikeDescription);
            AddDetailRow(html, "Measurement view", session.HideBeforeMeasurementsInReport ? "Final fit measurements only" : "Before / After comparison");
            html.AppendLine("</table>");

            html.AppendLine("<h2>Rider Goals</h2>");
            html.AppendLine("<div class=\"note\">" + EncodeOrPlaceholder(session.Goals) + "</div>");

            html.AppendLine("<h2>Before / After Images</h2>");
            if (!session.HideSideBySideImageInReport && HasReportImage(session.SideBySideReportImagePath))
                AddReportImage(html, "Side-by-side", session.SideBySideReportImagePath, true);
            if (!session.HideBeforeImageInReport || !session.HideAfterImageInReport)
            {
                html.AppendLine("<div class=\"media-grid\">");
                if (!session.HideBeforeImageInReport)
                    AddReportImage(html, "Before", session.BeforeReportImagePath, false);
                if (!session.HideAfterImageInReport)
                    AddReportImage(html, "After", session.AfterReportImagePath, false);
                html.AppendLine("</div>");
            }
            if (session.HideSideBySideImageInReport && session.HideBeforeImageInReport && session.HideAfterImageInReport)
                html.AppendLine("<div class=\"note\"><span class=\"muted\">Report images hidden for this session.</span></div>");

            if (!session.HideMeasurementReferenceImageInReport)
            {
                html.AppendLine("<h2>Measurement Reference Image</h2>");
                AddReportImage(html, "Measurement reference", session.MeasurementReferenceImagePath, true);
            }

            html.AppendLine("<h2>Bike Measurements</h2>");
            AddMeasurementTable(html, new[]
            {
                Row("Saddle height", session.SaddleHeightBefore, session.SaddleHeightAfter),
                Row("Saddle setback", session.SaddleSetbackBefore, session.SaddleSetbackAfter),
                Row("Saddle tip to grip reach", session.SaddleTipToGripReachBefore, session.SaddleTipToGripReachAfter),
                Row("Handlebar X", session.HandlebarXBefore, session.HandlebarXAfter),
                Row("Handlebar Y", session.HandlebarYBefore, session.HandlebarYAfter),
                Row("Handlebar reach", session.HandlebarReachBefore, session.HandlebarReachAfter),
                Row("Handlebar drop", session.HandlebarDropBefore, session.HandlebarDropAfter),
                Row("Crank length", session.CrankLengthBefore, session.CrankLengthAfter),
                Row("Cleat position", session.CleatPositionBefore, session.CleatPositionAfter)
            }, !session.HideBeforeMeasurementsInReport);

            html.AppendLine("<h2>Body Angles</h2>");
            AddMeasurementTable(html, new[]
            {
                Row("Knee angle", session.KneeAngleBefore, session.KneeAngleAfter),
                Row("Hip angle", session.HipAngleBefore, session.HipAngleAfter),
                Row("Ankle angle", session.AnkleAngleBefore, session.AnkleAngleAfter),
                Row("Torso angle", session.TorsoAngleBefore, session.TorsoAngleAfter),
                Row("Shoulder angle", session.ShoulderAngleBefore, session.ShoulderAngleAfter),
                Row("Elbow angle", session.ElbowAngleBefore, session.ElbowAngleAfter)
            }, !session.HideBeforeMeasurementsInReport);

            html.AppendLine("<h2>Recommendations and Notes</h2>");
            html.AppendLine("<div class=\"note\">" + EncodeOrPlaceholder(session.Notes) + "</div>");
            html.AppendLine("<div class=\"footer\">Generated by Cassette Motion Pro v0.7.13.</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private static void AddMeasurementTable(StringBuilder html, IEnumerable<ReportRow> rows, bool showBeforeMeasurements)
        {
            if (showBeforeMeasurements)
                AddBeforeAfterTable(html, rows);
            else
                AddAfterOnlyTable(html, rows);
        }

        private static void AddBeforeAfterTable(StringBuilder html, IEnumerable<ReportRow> rows)
        {
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Measurement</th><th>Before</th><th>After</th><th>Change</th></tr>");
            foreach (ReportRow row in rows)
            {
                html.AppendLine("<tr><td>" + Encode(row.Label) + "</td><td>" + EncodeOrPlaceholder(row.Before) + "</td><td>" + EncodeOrPlaceholder(row.After) + "</td><td>" + FormatChange(row.Before, row.After) + "</td></tr>");
            }
            html.AppendLine("</table>");
        }

        private static void AddAfterOnlyTable(StringBuilder html, IEnumerable<ReportRow> rows)
        {
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Measurement</th><th>Final / After</th></tr>");
            foreach (ReportRow row in rows)
            {
                html.AppendLine("<tr><td>" + Encode(row.Label) + "</td><td>" + EncodeOrPlaceholder(row.After) + "</td></tr>");
            }
            html.AppendLine("</table>");
        }

        private static void AddDetailRow(StringBuilder html, string label, string value)
        {
            html.AppendLine("<tr><th>" + Encode(label) + "</th><td>" + EncodeOrPlaceholder(value) + "</td></tr>");
        }

        private static bool HasReportImage(string imagePath)
        {
            return !string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath);
        }

        private static void AddReportImage(StringBuilder html, string label, string imagePath, bool fullWidth)
        {
            string cardClass = fullWidth ? "media-card full" : "media-card";
            if (HasReportImage(imagePath))
            {
                html.AppendLine("<div class=\"" + cardClass + "\"><img src=\"" + Encode(new Uri(imagePath).AbsoluteUri) + "\" alt=\"" + Encode(label) + " report image\"><div class=\"media-label\">" + Encode(label) + "</div></div>");
                return;
            }

            html.AppendLine("<div class=\"" + cardClass + "\"><div><strong>" + Encode(label) + "</strong><br><span>Image not added yet</span></div></div>");
        }

        private static ReportRow Row(string label, string before, string after)
        {
            return new ReportRow(label, before, after);
        }

        private static string EncodeOrPlaceholder(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<span class=\"muted\">Not recorded</span>" : Encode(value);
        }

        private static string Encode(string value)
        {
            return HttpUtility.HtmlEncode(value ?? string.Empty);
        }

        private static string FormatChange(string before, string after)
        {
            double beforeValue;
            double afterValue;
            string beforeUnit;
            string afterUnit;

            if (!TryParseMeasurement(before, out beforeValue, out beforeUnit) || !TryParseMeasurement(after, out afterValue, out afterUnit))
                return "<span class=\"muted\">—</span>";

            double difference = afterValue - beforeValue;
            string unit = string.IsNullOrWhiteSpace(afterUnit) ? beforeUnit : afterUnit;
            if (!string.IsNullOrWhiteSpace(unit))
                unit = " " + unit.Trim();
            string className = difference < 0 ? "change negative" : difference > 0 ? "change positive" : "change";
            string sign = difference > 0 ? "+" : string.Empty;
            return "<span class=\"" + className + "\">" + sign + difference.ToString("0.##", CultureInfo.InvariantCulture) + Encode(unit) + "</span>";
        }

        private static bool TryParseMeasurement(string value, out double number, out string unit)
        {
            number = 0;
            unit = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            Match match = Regex.Match(value.Trim(), @"^\s*(-?\d+(?:\.\d+)?)\s*(.*)$");
            if (!match.Success)
                return false;

            unit = match.Groups[2].Value;
            return double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        private static string CleanFileName(string value)
        {
            string cleaned = value;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                cleaned = cleaned.Replace(invalid, '-');
            return cleaned.Trim();
        }

        private sealed class ReportRow
        {
            public string Label { get; private set; }
            public string Before { get; private set; }
            public string After { get; private set; }

            public ReportRow(string label, string before, string after)
            {
                Label = label;
                Before = before;
                After = after;
            }
        }
    }
}
