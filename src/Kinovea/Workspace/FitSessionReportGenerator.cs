/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using CassetteMotionPro.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            html.AppendLine(".eyebrow{color:#b8f34a;font-size:12px;font-weight:700;letter-spacing:.16em;text-transform:uppercase;}");
            html.AppendLine("h1{margin:8px 0 4px;font-size:32px;}");
            html.AppendLine(".muted{color:#718078;}");
            html.AppendLine(".hero .muted{color:#afbbb5;}");
            html.AppendLine(".content{padding:34px 42px 44px;}");
            html.AppendLine("h2{margin-top:30px;border-bottom:1px solid #dfe7e2;padding-bottom:8px;font-size:20px;}");
            html.AppendLine("table{width:100%;border-collapse:collapse;margin-top:12px;}");
            html.AppendLine("th,td{text-align:left;border-bottom:1px solid #edf1ee;padding:10px 8px;vertical-align:top;}");
            html.AppendLine("th{font-size:12px;text-transform:uppercase;letter-spacing:.08em;color:#5c6862;}");
            html.AppendLine(".note{white-space:pre-wrap;background:#f7f9f8;border:1px solid #e1e8e4;border-radius:12px;padding:16px;}");
            html.AppendLine(".footer{margin-top:34px;color:#718078;font-size:12px;}");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class=\"page\">");
            html.AppendLine("<div class=\"hero\">");
            html.AppendLine("<div class=\"eyebrow\">Cassette Motion Pro</div>");
            html.AppendLine("<h1>Bike Fit Report</h1>");
            html.AppendLine("<div class=\"muted\">" + Encode(client.DisplayName) + " · " + Encode(client.BikeDescription) + "</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"content\">");
            html.AppendLine("<h2>Session</h2>");
            html.AppendLine("<table>");
            AddDetailRow(html, "Title", session.DisplayName);
            AddDetailRow(html, "Date", session.SessionDate == DateTime.MinValue ? DateTime.Today.ToLongDateString() : session.SessionDate.ToLongDateString());
            AddDetailRow(html, "Status", session.Status);
            AddDetailRow(html, "Client", client.DisplayName);
            AddDetailRow(html, "Bike", client.BikeDescription);
            html.AppendLine("</table>");

            html.AppendLine("<h2>Rider Goals</h2>");
            html.AppendLine("<div class=\"note\">" + EncodeOrPlaceholder(session.Goals) + "</div>");

            html.AppendLine("<h2>Bike Measurements</h2>");
            AddBeforeAfterTable(html, new[]
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
            });

            html.AppendLine("<h2>Body Angles</h2>");
            AddBeforeAfterTable(html, new[]
            {
                Row("Knee angle", session.KneeAngleBefore, session.KneeAngleAfter),
                Row("Hip angle", session.HipAngleBefore, session.HipAngleAfter),
                Row("Ankle angle", session.AnkleAngleBefore, session.AnkleAngleAfter),
                Row("Torso angle", session.TorsoAngleBefore, session.TorsoAngleAfter),
                Row("Shoulder angle", session.ShoulderAngleBefore, session.ShoulderAngleAfter),
                Row("Elbow angle", session.ElbowAngleBefore, session.ElbowAngleAfter)
            });

            html.AppendLine("<h2>Recommendations and Notes</h2>");
            html.AppendLine("<div class=\"note\">" + EncodeOrPlaceholder(session.Notes) + "</div>");
            html.AppendLine("<div class=\"footer\">Generated by Cassette Motion Pro v0.5.0.</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private static void AddBeforeAfterTable(StringBuilder html, IEnumerable<ReportRow> rows)
        {
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Measurement</th><th>Before</th><th>After</th></tr>");
            foreach (ReportRow row in rows)
            {
                html.AppendLine("<tr><td>" + Encode(row.Label) + "</td><td>" + EncodeOrPlaceholder(row.Before) + "</td><td>" + EncodeOrPlaceholder(row.After) + "</td></tr>");
            }
            html.AppendLine("</table>");
        }

        private static void AddDetailRow(StringBuilder html, string label, string value)
        {
            html.AppendLine("<tr><th>" + Encode(label) + "</th><td>" + EncodeOrPlaceholder(value) + "</td></tr>");
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
