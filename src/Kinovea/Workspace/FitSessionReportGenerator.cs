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
            html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#e9eee9;color:#18201d;}");
            html.AppendLine(".page{max-width:1040px;margin:28px auto;background:white;border-radius:22px;box-shadow:0 18px 60px rgba(12,19,16,.14);overflow:hidden;}");
            html.AppendLine(".hero{background:linear-gradient(135deg,#0d1311 0%,#16231e 62%,#25331d 100%);color:white;padding:38px 46px 32px;}");
            html.AppendLine(".hero-top{display:flex;justify-content:space-between;align-items:flex-start;gap:20px;}");
            html.AppendLine(".eyebrow{color:#b8f34a;font-size:12px;font-weight:800;letter-spacing:.18em;text-transform:uppercase;}");
            html.AppendLine("h1{margin:8px 0 8px;font-size:36px;line-height:1.08;}");
            html.AppendLine("h2{margin:34px 0 12px;font-size:19px;letter-spacing:.01em;}");
            html.AppendLine("h3{margin:22px 0 8px;font-size:15px;color:#2f3b36;text-transform:uppercase;letter-spacing:.08em;}");
            html.AppendLine(".muted{color:#718078;}");
            html.AppendLine(".hero .muted{color:#c4cec8;}");
            html.AppendLine(".print-button{border:0;border-radius:999px;background:#b8f34a;color:#0d1311;font-weight:800;padding:12px 19px;cursor:pointer;box-shadow:0 8px 22px rgba(0,0,0,.18);}");
            html.AppendLine(".hero-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-top:28px;}");
            html.AppendLine(".hero-card{background:rgba(255,255,255,.08);border:1px solid rgba(255,255,255,.13);border-radius:15px;padding:13px 14px;}");
            html.AppendLine(".hero-card .label{color:#b8f34a;font-size:10px;font-weight:800;text-transform:uppercase;letter-spacing:.11em;}");
            html.AppendLine(".hero-card .value{font-size:14px;font-weight:700;margin-top:4px;}");
            html.AppendLine(".content{padding:34px 46px 46px;}");
            html.AppendLine(".summary{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:16px;}");
            html.AppendLine(".panel{background:#f7f9f8;border:1px solid #dfe7e2;border-radius:18px;padding:18px;}");
            html.AppendLine(".panel-title{font-weight:800;font-size:13px;text-transform:uppercase;letter-spacing:.1em;color:#51615a;margin-bottom:10px;}");
            html.AppendLine(".note{white-space:pre-wrap;background:#f7f9f8;border:1px solid #e1e8e4;border-radius:16px;padding:17px;line-height:1.45;}");
            html.AppendLine("table{width:100%;border-collapse:separate;border-spacing:0;margin-top:10px;border:1px solid #dfe7e2;border-radius:14px;overflow:hidden;}");
            html.AppendLine("th,td{text-align:left;border-bottom:1px solid #edf1ee;padding:10px 11px;vertical-align:top;}");
            html.AppendLine("tr:last-child td{border-bottom:0;}");
            html.AppendLine("th{font-size:11px;text-transform:uppercase;letter-spacing:.08em;color:#5c6862;background:#f4f7f5;}");
            html.AppendLine("td:first-child{font-weight:650;color:#24302b;}");
            html.AppendLine(".media-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:14px;}");
            html.AppendLine(".media-card{border:1px solid #c9d5cf;border-radius:18px;background:#0f1714;min-height:190px;display:flex;align-items:center;justify-content:center;text-align:center;color:#9eaba5;position:relative;overflow:hidden;}");
            html.AppendLine(".media-card.full{margin-top:14px;}");
            html.AppendLine(".media-card img{display:block;width:100%;height:270px;object-fit:contain;background:#0f1714;}");
            html.AppendLine(".media-card.full img{height:410px;}");
            html.AppendLine(".media-label{position:absolute;left:13px;top:13px;background:rgba(13,19,17,.88);color:white;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:800;}");
            html.AppendLine(".change{font-weight:800;color:#0d1311;}");
            html.AppendLine(".positive{color:#2b7c46;}.negative{color:#9b3b32;}");
            html.AppendLine(".section-kicker{color:#6d7c75;font-size:13px;margin-top:-4px;margin-bottom:12px;}");
            html.AppendLine(".footer{margin-top:36px;color:#718078;font-size:12px;border-top:1px solid #e5ebe7;padding-top:16px;display:flex;justify-content:space-between;gap:20px;}");
            html.AppendLine("@media print{body{background:white}.page{box-shadow:none;margin:0;max-width:none;border-radius:0}.print-button{display:none}.content{padding-bottom:20px}.media-card{min-height:110px}.hero-grid,.summary{break-inside:avoid}}");
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
            html.AppendLine("<div class=\"hero-grid\">");
            AddHeroCard(html, "Session", session.DisplayName);
            AddHeroCard(html, "Date", session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("MMM d, yyyy") : session.SessionDate.ToString("MMM d, yyyy"));
            AddHeroCard(html, "Status", session.Status);
            AddHeroCard(html, "Report View", session.HideBeforeMeasurementsInReport ? "Final fit only" : "Before / After");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"content\">");
            html.AppendLine("<div class=\"summary\">");
            html.AppendLine("<div class=\"panel\"><div class=\"panel-title\">Rider Goals</div><div class=\"note\">" + EncodeOrPlaceholder(session.Goals) + "</div></div>");
            html.AppendLine("<div class=\"panel\"><div class=\"panel-title\">Session Details</div>");
            html.AppendLine("<table>");
            AddDetailRow(html, "Client", client.DisplayName);
            AddDetailRow(html, "Bike", client.BikeDescription);
            AddDetailRow(html, "Measurement view", session.HideBeforeMeasurementsInReport ? "Final fit measurements only" : "Before / After comparison");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            html.AppendLine("<h2>Visual Fit Review</h2>");
            html.AppendLine("<div class=\"section-kicker\">Selected images from the session. Use the Report Images tab to choose exactly what appears here.</div>");
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
                html.AppendLine("<div class=\"section-kicker\">Image used for manual bike metric reference and scale-assisted measurements.</div>");
                AddReportImage(html, "Measurement reference", session.MeasurementReferenceImagePath, true);
            }

            html.AppendLine("<h2>Bike Measurements</h2>");
            html.AppendLine("<div class=\"section-kicker\">Position coordinates and contact-point measurements used to describe the bicycle setup.</div>");
            if (HasBikeMetricsTrace(session))
            {
                html.AppendLine("<h3>Measurement capture trace</h3>");
                AddMeasurementTable(html, new[]
                {
                    Row("Capture method", session.BikeMetricsCaptureMethodBefore, session.BikeMetricsCaptureMethodAfter),
                    Row("Level reference", session.BikeMetricsLevelReferenceBefore, session.BikeMetricsLevelReferenceAfter),
                    Row("Saddle setback convention", session.BikeMetricsSetbackConventionBefore, session.BikeMetricsSetbackConventionAfter)
                }, !session.HideBeforeMeasurementsInReport);
            }

            html.AppendLine("<h3>Contact points</h3>");
            AddMeasurementTable(html, new[]
            {
                Row("Saddle height", session.SaddleHeightBefore, session.SaddleHeightAfter),
                Row("Saddle setback", session.SaddleSetbackBefore, session.SaddleSetbackAfter),
                Row("Saddle tip to grip reach", session.SaddleTipToGripReachBefore, session.SaddleTipToGripReachAfter),
                Row("Crank length", session.CrankLengthBefore, session.CrankLengthAfter)
            }, !session.HideBeforeMeasurementsInReport);

            html.AppendLine("<h3>Handlebar position</h3>");
            AddMeasurementTable(html, new[]
            {
                Row("Handlebar X", session.HandlebarXBefore, session.HandlebarXAfter),
                Row("Handlebar Y", session.HandlebarYBefore, session.HandlebarYAfter),
                Row("Handlebar reach", session.HandlebarReachBefore, session.HandlebarReachAfter),
                Row("Handlebar drop", session.HandlebarDropBefore, session.HandlebarDropAfter)
            }, !session.HideBeforeMeasurementsInReport);

            html.AppendLine("<h3>Foot interface</h3>");
            AddMeasurementTable(html, new[]
            {
                Row("Cleat position", session.CleatPositionBefore, session.CleatPositionAfter)
            }, !session.HideBeforeMeasurementsInReport);

            html.AppendLine("<h2>Body Angles</h2>");
            html.AppendLine("<div class=\"section-kicker\">Rider posture angles captured at matched fit positions for setup comparison.</div>");
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
            html.AppendLine("<div class=\"footer\"><span>Generated by Cassette Motion Pro v0.10.0</span><span>Professional bike fitting report</span></div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private static bool HasBikeMetricsTrace(FitSessionRecord session)
        {
            return !string.IsNullOrWhiteSpace(session.BikeMetricsCaptureMethodBefore) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsCaptureMethodAfter) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsLevelReferenceBefore) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsLevelReferenceAfter) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsSetbackConventionBefore) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsSetbackConventionAfter);
        }

        private static void AddMeasurementTable(StringBuilder html, IEnumerable<ReportRow> rows, bool showBeforeMeasurements)
        {
            if (showBeforeMeasurements)
                AddBeforeAfterTable(html, rows);
            else
                AddAfterOnlyTable(html, rows);
        }

        private static void AddHeroCard(StringBuilder html, string label, string value)
        {
            html.AppendLine("<div class=\"hero-card\"><div class=\"label\">" + Encode(label) + "</div><div class=\"value\">" + EncodeOrPlaceholder(value) + "</div></div>");
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
