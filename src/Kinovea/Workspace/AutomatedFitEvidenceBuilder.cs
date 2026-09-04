/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CassetteMotionPro.Workspace
{
    internal class FitEvidenceImage
    {
        public string Label;
        public string Path;
        public string ApprovalSource;

        public FitEvidenceImage(string label, string path, string approvalSource)
        {
            Label = label; Path = path; ApprovalSource = approvalSource;
        }
    }

    internal class FitEvidenceBundle
    {
        public readonly List<FitEvidenceImage> Images = new List<FitEvidenceImage>();
        public int BeforeBikeMeasurements;
        public int AfterBikeMeasurements;
        public int BeforeRiderMeasurements;
        public int AfterRiderMeasurements;
        public int ApprovedWorkflowSummaries;

        public int TotalMeasurements
        {
            get { return BeforeBikeMeasurements + AfterBikeMeasurements + BeforeRiderMeasurements + AfterRiderMeasurements; }
        }

        public bool HasBeforeAfterVisual
        {
            get
            {
                bool before = Images.Exists(i => i.Label.IndexOf("Before", StringComparison.OrdinalIgnoreCase) >= 0);
                bool after = Images.Exists(i => i.Label.IndexOf("After", StringComparison.OrdinalIgnoreCase) >= 0);
                bool comparison = Images.Exists(i => i.Label.IndexOf("comparison", StringComparison.OrdinalIgnoreCase) >= 0 || i.Label.IndexOf("Side-by-side", StringComparison.OrdinalIgnoreCase) >= 0);
                return comparison || (before && after);
            }
        }
    }

    internal static class AutomatedFitEvidenceBuilder
    {
        public static FitEvidenceBundle Collect(FitSessionRecord session)
        {
            if (session == null)
                throw new ArgumentNullException("session");
            FitEvidenceBundle bundle = new FitEvidenceBundle();

            if (!session.HideSideBySideImageInReport) AddImage(bundle, "Before/After comparison", session.SideBySideReportImagePath, "Fitter-selected report comparison");
            if (!session.HideBeforeImageInReport) AddImage(bundle, "Before report image", session.BeforeReportImagePath, "Fitter-selected report image");
            if (!session.HideAfterImageInReport) AddImage(bundle, "After report image", session.AfterReportImagePath, "Fitter-selected report image");
            if (!session.HideMeasurementReferenceImageInReport) AddImage(bundle, "Measurement reference", session.MeasurementReferenceImagePath, "Fitter-selected measurement reference");
            AddImage(bundle, "Before short-clip rider tracking", session.ShortClipTrackingBeforeEvidencePath, "Approved rider tracking");
            AddImage(bundle, "After short-clip rider tracking", session.ShortClipTrackingAfterEvidencePath, "Approved rider tracking");
            AddImage(bundle, "Before pedal-cycle review", session.PedalCycleBeforeEvidencePath, "Approved pedal-cycle review");
            AddImage(bundle, "After pedal-cycle review", session.PedalCycleAfterEvidencePath, "Approved pedal-cycle review");
            AddImage(bundle, "Before smart measurement frames", session.SmartMeasurementBeforeEvidencePath, "Approved smart-frame review");
            AddImage(bundle, "After smart measurement frames", session.SmartMeasurementAfterEvidencePath, "Approved smart-frame review");
            AddImage(bundle, "Before assisted bike landmarks", session.AssistedBikeLandmarksBeforeEvidencePath, "Fitter-confirmed bike landmarks");
            AddImage(bundle, "After assisted bike landmarks", session.AssistedBikeLandmarksAfterEvidencePath, "Fitter-confirmed bike landmarks");

            bundle.BeforeBikeMeasurements = CountValues(new[] { session.SaddleHeightBefore, session.SaddleSetbackBefore, session.SaddleTipToGripReachBefore, session.HandlebarXBefore, session.HandlebarYBefore, session.HandlebarReachBefore, session.HandlebarDropBefore, session.CrankLengthBefore, session.WheelbaseBefore, session.CleatPositionBefore });
            bundle.AfterBikeMeasurements = CountValues(new[] { session.SaddleHeightAfter, session.SaddleSetbackAfter, session.SaddleTipToGripReachAfter, session.HandlebarXAfter, session.HandlebarYAfter, session.HandlebarReachAfter, session.HandlebarDropAfter, session.CrankLengthAfter, session.WheelbaseAfter, session.CleatPositionAfter });
            bundle.BeforeRiderMeasurements = CountValues(new[] { session.KneeAngleBefore, session.HipAngleBefore, session.AnkleAngleBefore, session.TorsoAngleBefore, session.ShoulderAngleBefore });
            bundle.AfterRiderMeasurements = CountValues(new[] { session.KneeAngleAfter, session.HipAngleAfter, session.AnkleAngleAfter, session.TorsoAngleAfter, session.ShoulderAngleAfter });
            bundle.ApprovedWorkflowSummaries = CountValues(new[] {
                session.ShortClipTrackingBeforeSummary, session.ShortClipTrackingAfterSummary,
                session.PedalCycleBeforeSummary, session.PedalCycleAfterSummary,
                session.SmartMeasurementBeforeSummary, session.SmartMeasurementAfterSummary,
                session.AssistedBikeLandmarksBeforeSummary, session.AssistedBikeLandmarksAfterSummary,
                session.TrackingQualityReviewSummary
            });
            return bundle;
        }

        public static string BuildManifest(FitSessionRecord session)
        {
            FitEvidenceBundle bundle = Collect(session);
            StringBuilder text = new StringBuilder();
            text.AppendLine("Cassette Motion Pro - Approved Fit Evidence");
            text.AppendLine("============================================");
            text.AppendLine();
            text.AppendLine("Evidence was collected automatically from fitter-approved session records.");
            text.AppendLine("Saved images: " + bundle.Images.Count.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("Recorded measurements: " + bundle.TotalMeasurements.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("Approved workflow summaries: " + bundle.ApprovedWorkflowSummaries.ToString(CultureInfo.InvariantCulture));
            text.AppendLine("Before/After visual evidence: " + (bundle.HasBeforeAfterVisual ? "Ready" : "Not complete"));
            text.AppendLine();
            text.AppendLine("Measurements");
            text.AppendLine("------------");
            text.AppendLine("Before bike: " + bundle.BeforeBikeMeasurements + " · After bike: " + bundle.AfterBikeMeasurements);
            text.AppendLine("Before rider: " + bundle.BeforeRiderMeasurements + " · After rider: " + bundle.AfterRiderMeasurements);
            text.AppendLine();
            text.AppendLine("Approved images");
            text.AppendLine("---------------");
            if (bundle.Images.Count == 0)
                text.AppendLine("No approved report images were found.");
            foreach (FitEvidenceImage image in bundle.Images)
                text.AppendLine("- " + image.Label + " · " + image.ApprovalSource + " · " + image.Path);
            text.AppendLine();
            text.AppendLine("Only saved session evidence is included. Review the HTML report before sharing it with the client.");
            return text.ToString();
        }

        private static void AddImage(FitEvidenceBundle bundle, string label, string path, string approvalSource)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;
            string fullPath = Path.GetFullPath(path);
            if (bundle.Images.Exists(i => string.Equals(Path.GetFullPath(i.Path), fullPath, StringComparison.OrdinalIgnoreCase)))
                return;
            bundle.Images.Add(new FitEvidenceImage(label, path, approvalSource));
        }

        private static int CountValues(string[] values)
        {
            int count = 0;
            foreach (string value in values)
                if (!string.IsNullOrWhiteSpace(value)) count++;
            return count;
        }
    }
}
