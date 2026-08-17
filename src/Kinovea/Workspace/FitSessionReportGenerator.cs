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
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace CassetteMotionPro.Workspace
{
    public static class FitSessionReportGenerator
    {
        private const string StudioName = "Cassette Fit Studio";
        private const string FitterName = "Cesar Correa";
        private const string StudioPhone = "Add phone";
        private const string StudioEmail = "Add email";
        private const string StudioWebsite = "Add website";
        private const string PreparedByRole = "Professional Bike Fitting";
        private const string ConfidentialNotice = "Confidential bike fit report prepared for the named client.";
        private const string ReportVersion = "0.18.12";
        private const string BrandLogoResourceName = "CassetteMotionPro.Brand.Logo.png";

        public static string Generate(ClientRecord client, FitSessionRecord session)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (session == null)
                throw new ArgumentNullException("session");
            string reportsPath = GetSessionReportsPath(client, session);

            string fileName = BuildFileName(session);
            string path = Path.Combine(reportsPath, fileName);
            File.WriteAllText(path, BuildHtml(client, session, ResolveAbsoluteImageSource), Encoding.UTF8);
            return path;
        }

        public static string GeneratePackage(ClientRecord client, FitSessionRecord session)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (session == null)
                throw new ArgumentNullException("session");
            string reportsPath = GetSessionReportsPath(client, session);

            string packageFolder = GetUniqueDirectoryPath(Path.Combine(reportsPath, BuildPackageFolderName(client, session)));
            string imagesFolder = Path.Combine(packageFolder, "Images");
            Directory.CreateDirectory(packageFolder);
            Directory.CreateDirectory(imagesFolder);

            Dictionary<string, string> imageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CopyPackageImages(session, imagesFolder, imageMap);

            string reportPath = Path.Combine(packageFolder, "Bike Fit Report.html");
            File.WriteAllText(reportPath, BuildHtml(client, session, delegate(string imagePath)
            {
                return ResolvePackageImageSource(imagePath, imageMap);
            }), Encoding.UTF8);
            File.WriteAllText(Path.Combine(packageFolder, "README - Open This First.txt"), BuildPackageReadmeText(client, session), Encoding.UTF8);
            File.WriteAllText(Path.Combine(packageFolder, "Session Summary.txt"), BuildSessionSummaryText(client, session), Encoding.UTF8);
            File.WriteAllText(Path.Combine(packageFolder, "Client Handoff Notes.txt"), BuildHandoffText(client, session), Encoding.UTF8);
            File.WriteAllText(Path.Combine(packageFolder, "Bike Metrics Review.txt"), BuildBikeMetricsReviewText(client, session), Encoding.UTF8);

            return packageFolder;
        }

        public static string GeneratePackageZip(ClientRecord client, FitSessionRecord session)
        {
            string packageFolder = GeneratePackage(client, session);
            string zipPath = packageFolder + ".zip";

            if (File.Exists(zipPath))
                zipPath = packageFolder + " " + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture) + ".zip";

            ZipFile.CreateFromDirectory(packageFolder, zipPath);
            return zipPath;
        }

        private static string BuildFileName(FitSessionRecord session)
        {
            string title = CleanFileName(string.IsNullOrWhiteSpace(session.Title) ? "Bike Fit Report" : session.Title);
            string date = session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("yyyy-MM-dd") : session.SessionDate.ToString("yyyy-MM-dd");
            return date + " - " + title + ".html";
        }

        public static string GetSessionReportsPath(ClientRecord client, FitSessionRecord session)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (session == null)
                throw new ArgumentNullException("session");
            if (string.IsNullOrEmpty(client.ReportsPath))
                throw new InvalidOperationException("The client Reports folder is not available.");

            string reportsPath = Path.Combine(client.ReportsPath, "Fit Sessions", session.StorageFolderName);
            Directory.CreateDirectory(reportsPath);
            return reportsPath;
        }

        private static string BuildPackageReadmeText(ClientRecord client, FitSessionRecord session)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Cassette Motion Pro - Open This First");
            text.AppendLine("=====================================");
            text.AppendLine();
            text.AppendLine("Client: " + ValueOrPlaceholder(client.DisplayName));
            text.AppendLine("Bike: " + ValueOrPlaceholder(client.BikeDescription));
            text.AppendLine("Session: " + ValueOrPlaceholder(session.DisplayName));
            text.AppendLine("Date: " + (session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("MMM d, yyyy") : session.SessionDate.ToString("MMM d, yyyy")));
            AddStudioContactText(text);
            text.AppendLine();
            text.AppendLine("Start here");
            text.AppendLine("----------");
            text.AppendLine("1. Open Bike Fit Report.html to view the polished report.");
            text.AppendLine("2. Open Session Summary.txt for a quick plain-text overview of the fit.");
            text.AppendLine("3. Open Bike Metrics Review.txt to check missing values or values that may need another look.");
            text.AppendLine("4. Open Client Handoff Notes.txt only if you used the handoff tab or want to copy follow-up notes.");
            text.AppendLine("5. The Images folder contains the report images copied into this package.");
            text.AppendLine();
            text.AppendLine("Suggested flow");
            text.AppendLine("--------------");
            text.AppendLine("- Review the metrics text file before sending the report.");
            text.AppendLine("- Open the HTML report and use Print / Save PDF if you want a PDF copy.");
            text.AppendLine("- Keep this whole folder together so the report can find its Images folder.");
            text.AppendLine();
            text.AppendLine("Reminder");
            text.AppendLine("--------");
            text.AppendLine("Bike Metrics Review is advisory. It helps catch missing or unusual values, but it does not block reporting.");
            text.AppendLine("Saddle setback behind BB should be negative; in front of BB should be positive.");
            return text.ToString();
        }

        private static string BuildSessionSummaryText(ClientRecord client, FitSessionRecord session)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Cassette Motion Pro - Session Summary");
            text.AppendLine("=====================================");
            text.AppendLine();
            text.AppendLine("Client: " + ValueOrPlaceholder(client.DisplayName));
            text.AppendLine("Bike: " + ValueOrPlaceholder(client.BikeDescription));
            text.AppendLine("Session: " + ValueOrPlaceholder(session.DisplayName));
            text.AppendLine("Date: " + (session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("MMM d, yyyy") : session.SessionDate.ToString("MMM d, yyyy")));
            AddStudioContactText(text);
            text.AppendLine("Status: " + ValueOrPlaceholder(session.Status));
            text.AppendLine("Report view: " + (session.HideBeforeMeasurementsInReport ? "Final fit only" : "Before / After"));
            text.AppendLine();

            AddSummarySection(text, "Rider goals", session.Goals);
            AddSummarySection(text, "Fit summary - Main goal", session.FitSummaryMainGoal);
            AddSummarySection(text, "Fit summary - Key findings", session.FitSummaryKeyFindings);
            AddSummarySection(text, "Fit summary - Changes made", session.FitSummaryChangesMade);
            AddSummarySection(text, "Fit summary - Recommendations", session.FitSummaryRecommendations);
            AddSummarySection(text, "Fit summary - Follow-up plan", session.FitSummaryFollowUp);

            text.AppendLine("Key bike metrics");
            text.AppendLine("----------------");
            AddSummaryMetric(text, "Saddle height", session.SaddleHeightBefore, session.SaddleHeightAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Saddle setback", session.SaddleSetbackBefore, session.SaddleSetbackAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Saddle tip to grip reach", session.SaddleTipToGripReachBefore, session.SaddleTipToGripReachAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Handlebar X", session.HandlebarXBefore, session.HandlebarXAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Handlebar Y", session.HandlebarYBefore, session.HandlebarYAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Crank length", session.CrankLengthBefore, session.CrankLengthAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Wheelbase", session.WheelbaseBefore, session.WheelbaseAfter, !session.HideBeforeMeasurementsInReport);
            text.AppendLine();

            text.AppendLine("Body angles");
            text.AppendLine("-----------");
            AddSummaryMetric(text, "Knee angle", session.KneeAngleBefore, session.KneeAngleAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Hip angle", session.HipAngleBefore, session.HipAngleAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Ankle angle", session.AnkleAngleBefore, session.AnkleAngleAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Body reach", session.TorsoAngleBefore, session.TorsoAngleAfter, !session.HideBeforeMeasurementsInReport);
            AddSummaryMetric(text, "Back angle", session.ShoulderAngleBefore, session.ShoulderAngleAfter, !session.HideBeforeMeasurementsInReport);
            text.AppendLine();

            AddSummarySection(text, "Recommendations and notes", session.Notes);

            if (HasHandoffContent(session))
            {
                text.AppendLine("Handoff reminder");
                text.AppendLine("----------------");
                text.AppendLine("This session has handoff notes. Open Client Handoff Notes.txt before sending follow-up.");
                text.AppendLine();
            }

            text.AppendLine("Package files");
            text.AppendLine("-------------");
            text.AppendLine("- Bike Fit Report.html");
            text.AppendLine("- Session Summary.txt");
            text.AppendLine("- Bike Metrics Review.txt");
            text.AppendLine("- Client Handoff Notes.txt");
            text.AppendLine("- Images folder");
            return text.ToString();
        }

        private static string BuildHandoffText(ClientRecord client, FitSessionRecord session)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Cassette Motion Pro - Client Handoff Notes");
            text.AppendLine("==========================================");
            text.AppendLine();
            text.AppendLine("Client: " + ValueOrPlaceholder(client.DisplayName));
            text.AppendLine("Bike: " + ValueOrPlaceholder(client.BikeDescription));
            text.AppendLine("Session: " + ValueOrPlaceholder(session.DisplayName));
            text.AppendLine("Date: " + (session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("MMM d, yyyy") : session.SessionDate.ToString("MMM d, yyyy")));
            text.AppendLine();
            AddHandoffSection(text, "What to send", session.HandoffWhatToSend);
            AddHandoffSection(text, "Client follow-up message", session.HandoffClientMessage);
            AddHandoffSection(text, "Homework / ride instructions", session.HandoffHomework);
            AddHandoffSection(text, "Next appointment", session.HandoffNextAppointment);
            AddHandoffSection(text, "Internal notes", session.HandoffInternalNotes);
            return text.ToString();
        }

        private static string BuildBikeMetricsReviewText(ClientRecord client, FitSessionRecord session)
        {
            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();

            ReviewRequiredMetric(issues, "Saddle height", session.SaddleHeightBefore, session.SaddleHeightAfter, "Use Guided Capture or Distance from BB center to saddle top. Confirm the value is entered in mm.");
            ReviewRequiredMetric(issues, "Saddle setback", session.SaddleSetbackBefore, session.SaddleSetbackAfter, "Use horizontal distance from BB vertical line to saddle tip. Negative is OK when the saddle tip is behind the BB.");
            ReviewRequiredMetric(issues, "Saddle tip to grip reach", session.SaddleTipToGripReachBefore, session.SaddleTipToGripReachAfter, "Use Distance or horizontal assist from saddle tip to grip/hood contact point.");
            ReviewRequiredMetric(issues, "Handlebar X", session.HandlebarXBefore, session.HandlebarXAfter, "Use horizontal distance from BB center to grip/hood contact point.");
            ReviewRequiredMetric(issues, "Handlebar Y", session.HandlebarYBefore, session.HandlebarYAfter, "Use vertical distance from BB center to grip/hood contact point. Recheck image level/calibration if this looks strange.");

            ReviewMetricRange(warnings, "Saddle height After", session.SaddleHeightAfter, 500, 900, "mm", "If low or high, recheck calibration and the BB to saddle top click points.");
            ReviewMetricRange(warnings, "Saddle setback After", session.SaddleSetbackAfter, -120, 60, "mm", "Behind BB should be negative. If the sign is backwards, use Flip Setback Sign or re-enter the value.");
            ReviewMetricRange(warnings, "Saddle tip to grip reach After", session.SaddleTipToGripReachAfter, 350, 750, "mm", "If short or long, confirm you clicked saddle tip and the actual grip/hood contact point.");
            ReviewMetricRange(warnings, "Handlebar X After", session.HandlebarXAfter, 300, 700, "mm", "Confirm this is horizontal distance from BB to the grip/hood contact point.");
            ReviewMetricRange(warnings, "Handlebar Y After", session.HandlebarYAfter, -180, 180, "mm", "Confirm the image is level and the vertical direction is correct.");

            StringBuilder text = new StringBuilder();
            text.AppendLine("Cassette Motion Pro - Bike Metrics Review");
            text.AppendLine("==========================================");
            text.AppendLine();
            text.AppendLine("Client: " + ValueOrPlaceholder(client.DisplayName));
            text.AppendLine("Bike: " + ValueOrPlaceholder(client.BikeDescription));
            text.AppendLine("Session: " + ValueOrPlaceholder(session.DisplayName));
            text.AppendLine("Date: " + (session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("MMM d, yyyy") : session.SessionDate.ToString("MMM d, yyyy")));
            text.AppendLine();

            if (issues.Count == 0 && warnings.Count == 0)
            {
                text.AppendLine("Status: Ready for report");
                text.AppendLine();
                text.AppendLine("The key Bike Metrics are filled in and the final values look within broad expected ranges.");
                text.AppendLine();
                text.AppendLine("Next action: generate, preview, package, or zip the report.");
            }
            else
            {
                text.AppendLine("Status: Needs review");
                text.AppendLine();
                AddReviewSection(text, "Missing key values", issues);
                AddReviewSection(text, "Values to double-check", warnings);
                text.AppendLine("Next action");
                text.AppendLine("-----------");
                text.AppendLine("Recheck Guided Capture, calibration, or manual entries as needed.");
            }

            text.AppendLine();
            text.AppendLine("Reminder");
            text.AppendLine("--------");
            text.AppendLine("These checks are advisory. They do not block saving, reporting, packaging, or zipping.");
            text.AppendLine("Saddle setback behind BB should be negative; in front of BB should be positive.");
            return text.ToString();
        }

        private static void AddReviewSection(StringBuilder text, string label, List<string> items)
        {
            text.AppendLine(label);
            text.AppendLine(new string('-', label.Length));
            if (items.Count == 0)
            {
                text.AppendLine("None");
                text.AppendLine();
                return;
            }

            foreach (string item in items)
                text.AppendLine("- " + item);
            text.AppendLine();
        }

        private static void ReviewRequiredMetric(List<string> issues, string label, string before, string after, string nextAction)
        {
            if (string.IsNullOrWhiteSpace(before) && string.IsNullOrWhiteSpace(after))
            {
                issues.Add(label + ": Before and After are empty. Next action: " + nextAction);
                return;
            }

            if (string.IsNullOrWhiteSpace(after))
                issues.Add(label + ": After is empty. Next action: enter final/After value before reporting.");

            if (string.IsNullOrWhiteSpace(before))
                issues.Add(label + ": Before is empty. This is OK for final-only reports, but fill it in if you want Before / After comparison.");
        }

        private static void ReviewMetricRange(List<string> warnings, string label, string value, double minimum, double maximum, string unit, string nextAction)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            double parsed;
            if (!TryParseMeasurementNumber(value, out parsed))
            {
                warnings.Add(label + ": could not be read as a number from \"" + value + "\". Next action: enter like 742 mm or -35 mm.");
                return;
            }

            if (parsed < minimum || parsed > maximum)
                warnings.Add(label + ": " + value + " is outside the broad review range of " + minimum.ToString("0") + " to " + maximum.ToString("0") + " " + unit + ". Next action: " + nextAction);
        }

        private static bool TryParseMeasurementNumber(string value, out double number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim();
            int index = 0;
            while (index < text.Length && (char.IsDigit(text[index]) || text[index] == '-' || text[index] == '+' || text[index] == '.'))
                index++;

            if (index == 0)
                return false;

            return double.TryParse(text.Substring(0, index), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        private static void AddHandoffSection(StringBuilder text, string label, string value)
        {
            text.AppendLine(label);
            text.AppendLine(new string('-', label.Length));
            text.AppendLine(ValueOrPlaceholder(value));
            text.AppendLine();
        }

        private static void AddSummarySection(StringBuilder text, string label, string value)
        {
            text.AppendLine(label);
            text.AppendLine(new string('-', label.Length));
            text.AppendLine(ValueOrPlaceholder(value));
            text.AppendLine();
        }

        private static void AddStudioContactText(StringBuilder text)
        {
            text.AppendLine("Prepared by: " + StudioName);
            text.AppendLine("Role: " + PreparedByRole);
            text.AppendLine("Fitter: " + FitterName);
            text.AppendLine("Phone: " + StudioPhone);
            text.AppendLine("Email: " + StudioEmail);
            text.AppendLine("Website: " + StudioWebsite);
        }

        private static void AddStudioContactGrid(StringBuilder html)
        {
            html.AppendLine("<div class=\"contact-grid\">");
            AddStudioContactGridRow(html, "Fitter", FitterName);
            AddStudioContactGridRow(html, "Phone", StudioPhone);
            AddStudioContactGridRow(html, "Email", StudioEmail);
            AddStudioContactGridRow(html, "Website", StudioWebsite);
            html.AppendLine("</div>");
        }

        private static void AddStudioContactGridRow(StringBuilder html, string label, string value)
        {
            html.AppendLine("<div class=\"contact-label\">" + Encode(label) + "</div><div>" + Encode(value) + "</div>");
        }

        private static void AddSummaryMetric(StringBuilder text, string label, string before, string after, bool includeBefore)
        {
            if (includeBefore)
                text.AppendLine("- " + label + ": Before " + ValueOrPlaceholder(before) + " | After " + ValueOrPlaceholder(after) + " | Change " + FormatTextChange(before, after));
            else
                text.AppendLine("- " + label + ": " + ValueOrPlaceholder(after));
        }

        private static string FormatTextChange(string before, string after)
        {
            double beforeValue;
            double afterValue;
            string beforeUnit;
            string afterUnit;

            if (!TryParseMeasurement(before, out beforeValue, out beforeUnit) || !TryParseMeasurement(after, out afterValue, out afterUnit))
                return "Not calculated";

            double difference = afterValue - beforeValue;
            string unit = string.IsNullOrWhiteSpace(afterUnit) ? beforeUnit : afterUnit;
            if (!string.IsNullOrWhiteSpace(unit))
                unit = " " + unit.Trim();
            string sign = difference > 0 ? "+" : string.Empty;
            return sign + difference.ToString("0.##", CultureInfo.InvariantCulture) + unit;
        }

        private static bool HasHandoffContent(FitSessionRecord session)
        {
            return !string.IsNullOrWhiteSpace(session.HandoffWhatToSend) ||
                !string.IsNullOrWhiteSpace(session.HandoffClientMessage) ||
                !string.IsNullOrWhiteSpace(session.HandoffHomework) ||
                !string.IsNullOrWhiteSpace(session.HandoffNextAppointment) ||
                !string.IsNullOrWhiteSpace(session.HandoffInternalNotes);
        }

        private static string ValueOrPlaceholder(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Not recorded" : value.Trim();
        }

        private static string BuildPackageFolderName(ClientRecord client, FitSessionRecord session)
        {
            string clientName = CleanFileName(string.IsNullOrWhiteSpace(client.DisplayName) ? "Client" : client.DisplayName);
            string title = CleanFileName(string.IsNullOrWhiteSpace(session.Title) ? "Bike Fit" : session.Title);
            string date = session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("yyyy-MM-dd") : session.SessionDate.ToString("yyyy-MM-dd");
            return (date + " - " + clientName + " - " + title + " - Cassette Motion Pro Report Package").Trim();
        }

        private static string GetUniqueDirectoryPath(string basePath)
        {
            if (!Directory.Exists(basePath))
                return basePath;

            for (int index = 2; index < 1000; index++)
            {
                string candidate = basePath + " " + index.ToString(CultureInfo.InvariantCulture);
                if (!Directory.Exists(candidate))
                    return candidate;
            }

            return basePath + " " + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture);
        }

        private static void CopyPackageImages(FitSessionRecord session, string imagesFolder, Dictionary<string, string> imageMap)
        {
            if (!session.HideSideBySideImageInReport)
                CopyPackageImage(session.SideBySideReportImagePath, "Side-by-side", imagesFolder, imageMap);
            if (!session.HideBeforeImageInReport)
                CopyPackageImage(session.BeforeReportImagePath, "Before", imagesFolder, imageMap);
            if (!session.HideAfterImageInReport)
                CopyPackageImage(session.AfterReportImagePath, "After", imagesFolder, imageMap);
            if (!session.HideMeasurementReferenceImageInReport)
                CopyPackageImage(session.MeasurementReferenceImagePath, "Measurement reference", imagesFolder, imageMap);
        }

        private static void CopyPackageImage(string sourcePath, string label, string imagesFolder, Dictionary<string, string> imageMap)
        {
            if (!HasReportImage(sourcePath))
                return;

            string sourceKey = Path.GetFullPath(sourcePath);
            if (imageMap.ContainsKey(sourceKey))
                return;

            string extension = Path.GetExtension(sourcePath);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            string fileName = CleanFileName(label);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "Image";

            string destinationPath = Path.Combine(imagesFolder, fileName + extension);
            int index = 2;
            while (File.Exists(destinationPath))
            {
                destinationPath = Path.Combine(imagesFolder, fileName + " " + index.ToString(CultureInfo.InvariantCulture) + extension);
                index++;
            }

            File.Copy(sourcePath, destinationPath, false);
            imageMap[sourceKey] = "Images/" + Uri.EscapeDataString(Path.GetFileName(destinationPath)).Replace("%20", " ");
        }

        private static string ResolveAbsoluteImageSource(string imagePath)
        {
            return new Uri(imagePath).AbsoluteUri;
        }

        private static string ResolvePackageImageSource(string imagePath, Dictionary<string, string> imageMap)
        {
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                string sourceKey = Path.GetFullPath(imagePath);
                if (imageMap.ContainsKey(sourceKey))
                    return imageMap[sourceKey];
            }

            return ResolveAbsoluteImageSource(imagePath);
        }

        private static string BuildHtml(ClientRecord client, FitSessionRecord session, Func<string, string> imageSourceResolver)
        {
            StringBuilder html = new StringBuilder();
            bool useCmBadge = string.Equals(session.ReportLogoStyle, "CM", StringComparison.OrdinalIgnoreCase);
            bool hideBrandLogo = string.Equals(session.ReportLogoStyle, "None", StringComparison.OrdinalIgnoreCase);
            string brandLogoDataUri = useCmBadge || hideBrandLogo
                ? null
                : GetBrandLogoDataUri();
            html.AppendLine("<!doctype html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"utf-8\">");
            html.AppendLine("<title>" + Encode(client.DisplayName) + " Bike Fit Report</title>");
            html.AppendLine("<style>");
            html.AppendLine(":root{--ink:#18201d;--muted:#718078;--line:#dfe7e2;--soft:#f7f9f8;--brand:#b8f34a;--deep:#0d1311;}");
            html.AppendLine("*{box-sizing:border-box;}");
            html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:linear-gradient(180deg,#edf2ed 0%,#dde6df 100%);color:var(--ink);}");
            html.AppendLine(".page{max-width:1080px;margin:30px auto;background:white;border-radius:26px;box-shadow:0 24px 72px rgba(12,19,16,.16);overflow:hidden;}");
            html.AppendLine(".hero{background:radial-gradient(circle at 82% 18%,rgba(184,243,74,.18),transparent 27%),linear-gradient(135deg,#0d1311 0%,#17261f 58%,#2a351f 100%);color:white;padding:44px 50px 36px;position:relative;}");
            html.AppendLine(".hero:after{content:\"\";position:absolute;left:50px;right:50px;bottom:0;height:1px;background:linear-gradient(90deg,rgba(184,243,74,.7),rgba(255,255,255,.08));}");
            html.AppendLine(".hero-top{display:flex;justify-content:space-between;align-items:flex-start;gap:20px;}");
            html.AppendLine(".brand-lockup{display:flex;align-items:center;gap:14px;}");
            html.AppendLine(".brand-logo{width:54px;height:54px;border-radius:16px;object-fit:contain;background:rgba(255,255,255,.08);border:1px solid rgba(255,255,255,.16);padding:7px;box-shadow:0 10px 22px rgba(0,0,0,.18);}");
            html.AppendLine(".brand-mark{width:54px;height:54px;border-radius:16px;background:rgba(184,243,74,.16);border:1px solid rgba(184,243,74,.34);display:flex;align-items:center;justify-content:center;color:var(--brand);font-weight:900;font-size:22px;letter-spacing:-.08em;box-shadow:0 10px 22px rgba(0,0,0,.18);}");
            html.AppendLine(".eyebrow{color:var(--brand);font-size:12px;font-weight:900;letter-spacing:.2em;text-transform:uppercase;}");
            html.AppendLine("h1{margin:9px 0 9px;font-size:42px;line-height:1.03;letter-spacing:-.04em;}");
            html.AppendLine("h2{margin:38px 0 12px;font-size:22px;letter-spacing:-.01em;display:flex;align-items:center;gap:10px;}");
            html.AppendLine("h2:before{content:\"\";width:8px;height:26px;border-radius:999px;background:var(--brand);display:inline-block;}");
            html.AppendLine("h3{margin:22px 0 8px;font-size:15px;color:#2f3b36;text-transform:uppercase;letter-spacing:.08em;}");
            html.AppendLine(".muted{color:var(--muted);}");
            html.AppendLine(".hero .muted{color:#c4cec8;}");
            html.AppendLine(".report-subtitle{font-size:16px;margin-top:8px;max-width:620px;}");
            html.AppendLine(".print-button{border:0;border-radius:999px;background:var(--brand);color:var(--deep);font-weight:900;padding:13px 20px;cursor:pointer;box-shadow:0 10px 26px rgba(0,0,0,.22);white-space:nowrap;}");
            html.AppendLine(".hero-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:13px;margin-top:32px;}");
            html.AppendLine(".hero-card{background:rgba(255,255,255,.085);border:1px solid rgba(255,255,255,.16);border-radius:17px;padding:15px 16px;box-shadow:inset 0 1px 0 rgba(255,255,255,.06);}");
            html.AppendLine(".hero-card .label{color:var(--brand);font-size:10px;font-weight:900;text-transform:uppercase;letter-spacing:.12em;}");
            html.AppendLine(".hero-card .value{font-size:15px;font-weight:800;margin-top:6px;line-height:1.25;}");
            html.AppendLine(".prepared-card{margin-top:18px;background:rgba(255,255,255,.07);border:1px solid rgba(255,255,255,.14);border-radius:18px;padding:15px 16px;display:grid;grid-template-columns:1fr 1.2fr;gap:18px;align-items:start;}");
            html.AppendLine(".prepared-card .label{color:var(--brand);font-size:10px;font-weight:900;text-transform:uppercase;letter-spacing:.12em;margin-bottom:4px;}");
            html.AppendLine(".prepared-card .value{font-size:14px;font-weight:800;line-height:1.35;}");
            html.AppendLine(".prepared-card .contact{text-align:right;color:#c4cec8;font-size:13px;line-height:1.45;}");
            html.AppendLine(".contact-grid{display:grid;grid-template-columns:auto 1fr;gap:3px 10px;}");
            html.AppendLine(".contact-grid .contact-label{font-weight:900;color:var(--brand);text-transform:uppercase;font-size:10px;letter-spacing:.1em;}");
            html.AppendLine(".review-strip{background:#f4ffe8;border-bottom:1px solid #d2e6b6;color:#24302b;padding:17px 50px;}");
            html.AppendLine(".review-title{font-weight:900;font-size:12px;text-transform:uppercase;letter-spacing:.13em;margin-bottom:9px;}");
            html.AppendLine(".review-list{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin:0;padding:0;list-style:none;font-size:13px;}");
            html.AppendLine(".review-list li{background:rgba(255,255,255,.7);border:1px solid rgba(36,48,43,.1);border-radius:999px;padding:8px 12px;font-weight:650;}");
            html.AppendLine(".content{padding:38px 50px 50px;}");
            html.AppendLine(".summary{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:16px;}");
            html.AppendLine(".fit-summary{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:14px;}");
            html.AppendLine(".fit-summary .panel{min-height:118px;border-left:5px solid var(--brand);}");
            html.AppendLine(".fit-summary .wide{grid-column:1 / -1;}");
            html.AppendLine(".panel{background:linear-gradient(180deg,#fbfcfb 0%,var(--soft) 100%);border:1px solid var(--line);border-radius:20px;padding:20px;box-shadow:0 8px 22px rgba(31,45,38,.05);}");
            html.AppendLine(".panel-title{font-weight:900;font-size:12px;text-transform:uppercase;letter-spacing:.12em;color:#51615a;margin-bottom:11px;}");
            html.AppendLine(".panel table{border:1px solid var(--line);border-radius:14px;overflow:hidden;}");
            html.AppendLine(".note{white-space:pre-wrap;background:var(--soft);border:1px solid #e1e8e4;border-radius:18px;padding:18px;line-height:1.5;}");
            html.AppendLine(".summary-text{white-space:pre-wrap;line-height:1.5;}");
            html.AppendLine(".table-wrap{border:1px solid var(--line);border-radius:16px;overflow:hidden;margin-top:11px;box-shadow:0 8px 18px rgba(31,45,38,.04);}");
            html.AppendLine("table{width:100%;border-collapse:separate;border-spacing:0;margin:0;}");
            html.AppendLine("th,td{text-align:left;border-bottom:1px solid #edf1ee;padding:12px 13px;vertical-align:top;}");
            html.AppendLine("tr:last-child td{border-bottom:0;}");
            html.AppendLine("th{font-size:11px;text-transform:uppercase;letter-spacing:.09em;color:#5c6862;background:#f4f7f5;}");
            html.AppendLine("tr:nth-child(even) td{background:#fbfcfb;}");
            html.AppendLine("td:first-child{font-weight:650;color:#24302b;}");
            html.AppendLine(".media-grid{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-top:14px;}");
            html.AppendLine(".media-card{border:1px solid #c9d5cf;border-radius:20px;background:#0f1714;min-height:190px;display:flex;align-items:center;justify-content:center;text-align:center;color:#9eaba5;position:relative;overflow:hidden;box-shadow:0 10px 24px rgba(15,23,20,.12);}");
            html.AppendLine(".media-card.full{margin-top:14px;}");
            html.AppendLine(".media-card img{display:block;width:100%;height:270px;object-fit:contain;background:#0f1714;}");
            html.AppendLine(".media-card.full img{height:410px;}");
            html.AppendLine(".media-label{position:absolute;left:13px;top:13px;background:rgba(13,19,17,.88);color:white;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:800;}");
            html.AppendLine(".change{font-weight:900;color:#0d1311;}");
            html.AppendLine(".positive{color:#2b7c46;}.negative{color:#9b3b32;}");
            html.AppendLine(".section-kicker{color:#6d7c75;font-size:13px;margin-top:-4px;margin-bottom:13px;max-width:760px;}");
            html.AppendLine(".section-card{background:white;border:1px solid #e6ece8;border-radius:22px;padding:22px;margin-top:16px;box-shadow:0 10px 26px rgba(31,45,38,.045);}");
            html.AppendLine(".prepared-footer{margin-top:38px;border-top:1px solid #e5ebe7;padding-top:20px;display:grid;grid-template-columns:1.2fr 1.2fr;gap:22px;color:#4f5f58;}");
            html.AppendLine(".prepared-footer .label{font-weight:900;font-size:11px;text-transform:uppercase;letter-spacing:.12em;color:#51615a;margin-bottom:6px;}");
            html.AppendLine(".prepared-footer .value{font-weight:800;color:#1f2b25;}");
            html.AppendLine(".confidential{margin-top:12px;background:#f6f8f6;border:1px solid #e5ebe7;border-radius:14px;padding:12px 14px;font-size:12px;color:#607169;}");
            html.AppendLine(".footer{margin-top:18px;color:var(--muted);font-size:12px;display:flex;justify-content:space-between;gap:20px;}");
            html.AppendLine("@media print{body{background:white}.page{box-shadow:none;margin:0;max-width:none;border-radius:0}.hero{padding:30px 34px 24px}.content{padding:24px 34px}.print-button,.review-strip{display:none}.media-card{min-height:110px}.hero-grid,.summary,.fit-summary,.section-card,.prepared-footer{break-inside:avoid}h2{break-after:avoid}.table-wrap{box-shadow:none}}");
            html.AppendLine("@media(max-width:760px){.hero-grid,.summary,.fit-summary,.media-grid,.review-list,.prepared-footer{grid-template-columns:1fr}.fit-summary .wide{grid-column:auto}.prepared-card{display:block}.prepared-card .contact{text-align:left;margin-top:10px}}");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class=\"page\">");
            html.AppendLine("<div class=\"hero\">");
            html.AppendLine("<div class=\"hero-top\">");
            html.AppendLine("<div class=\"brand-lockup\">");
            if (useCmBadge)
                html.AppendLine("<div class=\"brand-mark\">CM</div>");
            else if (!hideBrandLogo && !string.IsNullOrEmpty(brandLogoDataUri))
                html.AppendLine("<img class=\"brand-logo\" src=\"" + brandLogoDataUri + "\" alt=\"Cassette Motion Pro logo\">");
            else if (!hideBrandLogo)
                html.AppendLine("<div class=\"brand-mark\">CM</div>");
            html.AppendLine("<div>");
            html.AppendLine("<div class=\"eyebrow\">Cassette Motion Pro</div>");
            html.AppendLine("<h1>Bike Fit Report</h1>");
            html.AppendLine("<div class=\"muted report-subtitle\">" + Encode(client.DisplayName) + " · " + Encode(client.BikeDescription) + "</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("<button class=\"print-button\" onclick=\"window.print()\">Print / Save PDF</button>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"hero-grid\">");
            AddHeroCard(html, "Session", session.DisplayName);
            AddHeroCard(html, "Date", session.SessionDate == DateTime.MinValue ? DateTime.Today.ToString("MMM d, yyyy") : session.SessionDate.ToString("MMM d, yyyy"));
            AddHeroCard(html, "Status", session.Status);
            AddHeroCard(html, "Report View", session.HideBeforeMeasurementsInReport ? "Final fit only" : "Before / After");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"prepared-card\">");
            html.AppendLine("<div><div class=\"label\">Report prepared by</div><div class=\"value\">" + Encode(StudioName) + "<br>" + Encode(PreparedByRole) + "</div></div>");
            html.AppendLine("<div class=\"contact\">");
            AddStudioContactGrid(html);
            html.AppendLine("<div style=\"margin-top:8px\">" + Encode(ConfidentialNotice) + "</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            AddReviewStrip(html, session);
            html.AppendLine("<div class=\"content\">");
            html.AppendLine("<div class=\"summary\">");
            html.AppendLine("<div class=\"panel\"><div class=\"panel-title\">Rider Goals</div><div class=\"note\">" + EncodeOrPlaceholder(session.Goals) + "</div></div>");
            html.AppendLine("<div class=\"panel\"><div class=\"panel-title\">Session Details</div>");
            html.AppendLine("<table>");
            AddDetailRow(html, "Client", client.DisplayName);
            AddDetailRow(html, "Bike", client.BikeDescription);
            AddDetailRow(html, "Prepared by", StudioName);
            AddDetailRow(html, "Fitter", FitterName);
            AddDetailRow(html, "Measurement view", session.HideBeforeMeasurementsInReport ? "Final fit measurements only" : "Before / After comparison");
            html.AppendLine("</table>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");

            if (HasFitSummaryContent(session))
                AddFitSummarySection(html, session);

            html.AppendLine("<h2>Visual Fit Review</h2>");
            html.AppendLine("<div class=\"section-kicker\">Selected images from the session. Use the Report Images tab to choose exactly what appears here.</div>");
            html.AppendLine("<div class=\"section-card\">");
            if (!session.HideSideBySideImageInReport && HasReportImage(session.SideBySideReportImagePath))
                AddReportImage(html, "Side-by-side", session.SideBySideReportImagePath, true, imageSourceResolver);
            if (!session.HideBeforeImageInReport || !session.HideAfterImageInReport)
            {
                html.AppendLine("<div class=\"media-grid\">");
                if (!session.HideBeforeImageInReport)
                    AddReportImage(html, "Before", session.BeforeReportImagePath, false, imageSourceResolver);
                if (!session.HideAfterImageInReport)
                    AddReportImage(html, "After", session.AfterReportImagePath, false, imageSourceResolver);
                html.AppendLine("</div>");
            }
            if (session.HideSideBySideImageInReport && session.HideBeforeImageInReport && session.HideAfterImageInReport)
                html.AppendLine("<div class=\"note\"><span class=\"muted\">Report images hidden for this session.</span></div>");
            html.AppendLine("</div>");

            if (!session.HideMeasurementReferenceImageInReport)
            {
                html.AppendLine("<h2>Measurement Reference Image</h2>");
                html.AppendLine("<div class=\"section-kicker\">Image used for manual bike metric reference and scale-assisted measurements.</div>");
                html.AppendLine("<div class=\"section-card\">");
                AddReportImage(html, "Measurement reference", session.MeasurementReferenceImagePath, true, imageSourceResolver);
                html.AppendLine("</div>");
            }

            html.AppendLine("<h2>Bike Measurements</h2>");
            html.AppendLine("<div class=\"section-kicker\">Position coordinates and contact-point measurements used to describe the bicycle setup.</div>");
            html.AppendLine("<div class=\"section-card\">");
            if (!session.HideMeasurementCaptureTraceInReport && HasBikeMetricsTrace(session))
            {
                html.AppendLine("<h3>Measurement capture trace</h3>");
                AddMeasurementTable(html, new[]
                {
                    Row("Capture method", session.BikeMetricsCaptureMethodBefore, session.BikeMetricsCaptureMethodAfter),
                    Row("Camera setup", session.BikeMetricsCameraSetupBefore, session.BikeMetricsCameraSetupAfter),
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
                Row("Crank length", session.CrankLengthBefore, session.CrankLengthAfter),
                Row("Wheelbase", session.WheelbaseBefore, session.WheelbaseAfter)
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
            html.AppendLine("</div>");

            html.AppendLine("<h2>Body Angles</h2>");
            html.AppendLine("<div class=\"section-kicker\">Rider posture angles captured at matched fit positions for setup comparison.</div>");
            html.AppendLine("<div class=\"section-card\">");
            AddMeasurementTable(html, new[]
            {
                Row("Knee angle", session.KneeAngleBefore, session.KneeAngleAfter),
                Row("Hip angle", session.HipAngleBefore, session.HipAngleAfter),
                Row("Ankle angle", session.AnkleAngleBefore, session.AnkleAngleAfter),
                Row("Body reach", session.TorsoAngleBefore, session.TorsoAngleAfter),
                Row("Back angle", session.ShoulderAngleBefore, session.ShoulderAngleAfter)
            }, !session.HideBeforeMeasurementsInReport);
            html.AppendLine("</div>");

            html.AppendLine("<h2>Recommendations and Notes</h2>");
            html.AppendLine("<div class=\"note\">" + EncodeOrPlaceholder(session.Notes) + "</div>");
            html.AppendLine("<div class=\"prepared-footer\">");
            html.AppendLine("<div><div class=\"label\">Report prepared by</div><div class=\"value\">" + Encode(StudioName) + "</div><div>" + Encode(PreparedByRole) + "</div><div>Fitter: " + Encode(FitterName) + "</div></div>");
            html.AppendLine("<div><div class=\"label\">Studio contact</div>");
            AddStudioContactGrid(html);
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("<div class=\"confidential\">" + Encode(ConfidentialNotice) + "</div>");
            html.AppendLine("<div class=\"footer\"><span>Generated by Cassette Motion Pro v" + ReportVersion + "</span><span>Professional bike fitting report</span></div>");
            html.AppendLine("</div>");
            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private static string GetBrandLogoDataUri()
        {
            try
            {
                using (Stream stream = typeof(FitSessionReportGenerator).Assembly.GetManifestResourceStream(BrandLogoResourceName))
                {
                    if (stream == null)
                        return string.Empty;

                    using (MemoryStream memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        return "data:image/png;base64," + Convert.ToBase64String(memory.ToArray());
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool HasBikeMetricsTrace(FitSessionRecord session)
        {
            return !string.IsNullOrWhiteSpace(session.BikeMetricsCaptureMethodBefore) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsCaptureMethodAfter) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsLevelReferenceBefore) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsLevelReferenceAfter) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsSetbackConventionBefore) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsSetbackConventionAfter) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsCameraSetupBefore) ||
                !string.IsNullOrWhiteSpace(session.BikeMetricsCameraSetupAfter);
        }

        private static bool HasFitSummaryContent(FitSessionRecord session)
        {
            return !string.IsNullOrWhiteSpace(session.FitSummaryMainGoal) ||
                !string.IsNullOrWhiteSpace(session.FitSummaryKeyFindings) ||
                !string.IsNullOrWhiteSpace(session.FitSummaryChangesMade) ||
                !string.IsNullOrWhiteSpace(session.FitSummaryRecommendations) ||
                !string.IsNullOrWhiteSpace(session.FitSummaryFollowUp);
        }

        private static void AddReviewStrip(StringBuilder html, FitSessionRecord session)
        {
            html.AppendLine("<div class=\"review-strip\">");
            html.AppendLine("<div class=\"review-title\">Review before sending</div>");
            html.AppendLine("<ul class=\"review-list\">");
            AddReviewItem(html, HasFitSummaryContent(session) ? "Fit Summary filled in" : "Add Fit Summary if needed");
            AddReviewItem(html, HasAnyReportImage(session) ? "Report images selected" : "Add or hide report images");
            AddReviewItem(html, HasAnyBikeMeasurement(session) ? "Bike metrics recorded" : "Check Bike Metrics values");
            AddReviewItem(html, session.HideBeforeMeasurementsInReport ? "Final-only report view" : "Before / After report view");
            html.AppendLine("</ul>");
            html.AppendLine("</div>");
        }

        private static void AddReviewItem(StringBuilder html, string text)
        {
            html.AppendLine("<li>" + Encode(text) + "</li>");
        }

        private static bool HasAnyReportImage(FitSessionRecord session)
        {
            return HasReportImage(session.SideBySideReportImagePath) ||
                HasReportImage(session.BeforeReportImagePath) ||
                HasReportImage(session.AfterReportImagePath) ||
                HasReportImage(session.MeasurementReferenceImagePath);
        }

        private static bool HasAnyBikeMeasurement(FitSessionRecord session)
        {
            return !string.IsNullOrWhiteSpace(session.SaddleHeightBefore) ||
                !string.IsNullOrWhiteSpace(session.SaddleHeightAfter) ||
                !string.IsNullOrWhiteSpace(session.SaddleSetbackBefore) ||
                !string.IsNullOrWhiteSpace(session.SaddleSetbackAfter) ||
                !string.IsNullOrWhiteSpace(session.SaddleTipToGripReachBefore) ||
                !string.IsNullOrWhiteSpace(session.SaddleTipToGripReachAfter) ||
                !string.IsNullOrWhiteSpace(session.HandlebarXBefore) ||
                !string.IsNullOrWhiteSpace(session.HandlebarXAfter) ||
                !string.IsNullOrWhiteSpace(session.HandlebarYBefore) ||
                !string.IsNullOrWhiteSpace(session.HandlebarYAfter) ||
                !string.IsNullOrWhiteSpace(session.HandlebarReachBefore) ||
                !string.IsNullOrWhiteSpace(session.HandlebarReachAfter) ||
                !string.IsNullOrWhiteSpace(session.HandlebarDropBefore) ||
                !string.IsNullOrWhiteSpace(session.HandlebarDropAfter) ||
                !string.IsNullOrWhiteSpace(session.CrankLengthBefore) ||
                !string.IsNullOrWhiteSpace(session.CrankLengthAfter) ||
                !string.IsNullOrWhiteSpace(session.WheelbaseBefore) ||
                !string.IsNullOrWhiteSpace(session.WheelbaseAfter) ||
                !string.IsNullOrWhiteSpace(session.CleatPositionBefore) ||
                !string.IsNullOrWhiteSpace(session.CleatPositionAfter);
        }

        private static void AddFitSummarySection(StringBuilder html, FitSessionRecord session)
        {
            html.AppendLine("<h2>Fit Summary</h2>");
            html.AppendLine("<div class=\"section-kicker\">Plain-language summary for the rider: goals, changes, recommendations, and follow-up.</div>");
            html.AppendLine("<div class=\"fit-summary\">");
            AddFitSummaryPanelIfRecorded(html, "Main goal", session.FitSummaryMainGoal, false);
            AddFitSummaryPanelIfRecorded(html, "Key findings", session.FitSummaryKeyFindings, false);
            AddFitSummaryPanelIfRecorded(html, "Changes made", session.FitSummaryChangesMade, false);
            AddFitSummaryPanelIfRecorded(html, "Follow-up plan", session.FitSummaryFollowUp, false);
            AddFitSummaryPanelIfRecorded(html, "Recommendations", session.FitSummaryRecommendations, true);
            html.AppendLine("</div>");
        }

        private static void AddFitSummaryPanelIfRecorded(StringBuilder html, string label, string value, bool wide)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            AddFitSummaryPanel(html, label, value, wide);
        }

        private static void AddFitSummaryPanel(StringBuilder html, string label, string value, bool wide)
        {
            string className = wide ? "panel wide" : "panel";
            html.AppendLine("<div class=\"" + className + "\"><div class=\"panel-title\">" + Encode(label) + "</div><div class=\"summary-text\">" + EncodeOrPlaceholder(value) + "</div></div>");
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
            html.AppendLine("<div class=\"table-wrap\">");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Measurement</th><th>Before</th><th>After</th><th>Change</th></tr>");
            foreach (ReportRow row in rows)
            {
                html.AppendLine("<tr><td>" + Encode(row.Label) + "</td><td>" + EncodeOrPlaceholder(row.Before) + "</td><td>" + EncodeOrPlaceholder(row.After) + "</td><td>" + FormatChange(row.Before, row.After) + "</td></tr>");
            }
            html.AppendLine("</table>");
            html.AppendLine("</div>");
        }

        private static void AddAfterOnlyTable(StringBuilder html, IEnumerable<ReportRow> rows)
        {
            html.AppendLine("<div class=\"table-wrap\">");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Measurement</th><th>Final / After</th></tr>");
            foreach (ReportRow row in rows)
            {
                html.AppendLine("<tr><td>" + Encode(row.Label) + "</td><td>" + EncodeOrPlaceholder(row.After) + "</td></tr>");
            }
            html.AppendLine("</table>");
            html.AppendLine("</div>");
        }

        private static void AddDetailRow(StringBuilder html, string label, string value)
        {
            html.AppendLine("<tr><th>" + Encode(label) + "</th><td>" + EncodeOrPlaceholder(value) + "</td></tr>");
        }

        private static bool HasReportImage(string imagePath)
        {
            return !string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath);
        }

        private static void AddReportImage(StringBuilder html, string label, string imagePath, bool fullWidth, Func<string, string> imageSourceResolver)
        {
            string cardClass = fullWidth ? "media-card full" : "media-card";
            if (HasReportImage(imagePath))
            {
                html.AppendLine("<div class=\"" + cardClass + "\"><img src=\"" + Encode(imageSourceResolver(imagePath)) + "\" alt=\"" + Encode(label) + " report image\"><div class=\"media-label\">" + Encode(label) + "</div></div>");
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
