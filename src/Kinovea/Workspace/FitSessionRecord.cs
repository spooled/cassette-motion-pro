/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace CassetteMotionPro.Workspace
{
    [Serializable]
    public class FitSessionRecord
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime SessionDate { get; set; }
        public string Status { get; set; }
        public string FitTemplateName { get; set; }
        public string FitTemplateBikeType { get; set; }
        public string FitProtocolBikeType { get; set; }
        public string FitProtocolCompletedSteps { get; set; }
        public string CameraSetupProfileName { get; set; }
        public string CameraSetupLeftRole { get; set; }
        public string CameraSetupRightRole { get; set; }
        public Guid RepeatFitSourceSessionId { get; set; }
        public string RepeatFitSourceTitle { get; set; }
        public DateTime RepeatFitSourceDate { get; set; }
        public string Goals { get; set; }
        public string Notes { get; set; }
        public string FitSummaryMainGoal { get; set; }
        public string FitSummaryKeyFindings { get; set; }
        public string FitSummaryChangesMade { get; set; }
        public string FitSummaryRecommendations { get; set; }
        public string FitChangeRecommendationDraft { get; set; }
        public string FitChangeRecommendationAppliedText { get; set; }
        public string FitSummaryFollowUp { get; set; }
        public string AssistedWorkflowLastStage { get; set; }
        public string AssistedWorkflowSkippedStages { get; set; }
        public string AssistedWorkflowSkipNotes { get; set; }
        public string AssistedWorkflowRecoverySummary { get; set; }
        public DateTime AssistedWorkflowLastSavedUtc { get; set; }
        public DateTime AssistedWorkflowReportGeneratedUtc { get; set; }
        public string HandoffWhatToSend { get; set; }
        public string HandoffClientMessage { get; set; }
        public string HandoffHomework { get; set; }
        public string HandoffNextAppointment { get; set; }
        public string HandoffInternalNotes { get; set; }
        public List<FitFollowUpEntry> FollowUps { get; set; }

        public string LeftVideoPath { get; set; }
        public string RightVideoPath { get; set; }

        // Retained so sessions created by the first 0.3 build remain readable.
        public string SideVideoPath { get; set; }
        public string FrontVideoPath { get; set; }
        public string RearVideoPath { get; set; }
        public string BeforeVideoPath { get; set; }
        public string AfterVideoPath { get; set; }
        public string BeforeReportImagePath { get; set; }
        public string AfterReportImagePath { get; set; }
        public string SideBySideReportImagePath { get; set; }
        public string MeasurementReferenceImagePath { get; set; }
        public bool HideBeforeMeasurementsInReport { get; set; }
        public bool HideSideBySideImageInReport { get; set; }
        public bool HideBeforeImageInReport { get; set; }
        public bool HideAfterImageInReport { get; set; }
        public bool HideMeasurementReferenceImageInReport { get; set; }
        public bool HideMeasurementCaptureTraceInReport { get; set; }
        public string ReportLogoStyle { get; set; }

        public string SaddleHeightBefore { get; set; }
        public string SaddleHeightAfter { get; set; }
        public string SaddleSetbackBefore { get; set; }
        public string SaddleSetbackAfter { get; set; }
        public string HandlebarReachBefore { get; set; }
        public string HandlebarReachAfter { get; set; }
        public string HandlebarDropBefore { get; set; }
        public string HandlebarDropAfter { get; set; }
        public string SaddleTipToGripReachBefore { get; set; }
        public string SaddleTipToGripReachAfter { get; set; }
        public string HandlebarXBefore { get; set; }
        public string HandlebarXAfter { get; set; }
        public string HandlebarYBefore { get; set; }
        public string HandlebarYAfter { get; set; }
        public string CrankLengthBefore { get; set; }
        public string CrankLengthAfter { get; set; }
        public string WheelbaseBefore { get; set; }
        public string WheelbaseAfter { get; set; }
        public string CleatPositionBefore { get; set; }
        public string CleatPositionAfter { get; set; }
        public string BikeMetricsCaptureMethodBefore { get; set; }
        public string BikeMetricsCaptureMethodAfter { get; set; }
        public string BikeMetricsLevelReferenceBefore { get; set; }
        public string BikeMetricsLevelReferenceAfter { get; set; }
        public string BikeMetricsSetbackConventionBefore { get; set; }
        public string BikeMetricsSetbackConventionAfter { get; set; }
        public string BikeMetricsCameraSetupBefore { get; set; }
        public string BikeMetricsCameraSetupAfter { get; set; }
        public string AssistedBikeLandmarksBeforeSummary { get; set; }
        public string AssistedBikeLandmarksAfterSummary { get; set; }
        public string AssistedBikeLandmarksBeforeEvidencePath { get; set; }
        public string AssistedBikeLandmarksAfterEvidencePath { get; set; }

        public string KneeAngleBefore { get; set; }
        public string KneeAngleAfter { get; set; }
        public string HipAngleBefore { get; set; }
        public string HipAngleAfter { get; set; }
        public string AnkleAngleBefore { get; set; }
        public string AnkleAngleAfter { get; set; }
        public string TorsoAngleBefore { get; set; }
        public string TorsoAngleAfter { get; set; }
        public string ShoulderAngleBefore { get; set; }
        public string ShoulderAngleAfter { get; set; }
        public string ElbowAngleBefore { get; set; }
        public string ElbowAngleAfter { get; set; }
        public string ShortClipTrackingBeforeSummary { get; set; }
        public string ShortClipTrackingAfterSummary { get; set; }
        public string ShortClipTrackingBeforeEvidencePath { get; set; }
        public string ShortClipTrackingAfterEvidencePath { get; set; }
        public string PedalCycleBeforeSummary { get; set; }
        public string PedalCycleAfterSummary { get; set; }
        public string PedalCycleBeforeEvidencePath { get; set; }
        public string PedalCycleAfterEvidencePath { get; set; }
        public string TrackingQualityReviewSummary { get; set; }
        public string TrackingCalibrationReferenceName { get; set; }
        public string TrackingCalibrationKnownDimensionMm { get; set; }
        public string TrackingCalibrationPixelSpan { get; set; }
        public string TrackingCalibrationTestOneMm { get; set; }
        public string TrackingCalibrationTestTwoMm { get; set; }
        public string TrackingCalibrationTestThreeMm { get; set; }
        public string TrackingCalibrationAccuracySummary { get; set; }
        public string SmartMeasurementBeforeSummary { get; set; }
        public string SmartMeasurementAfterSummary { get; set; }
        public string SmartMeasurementBeforeEvidencePath { get; set; }
        public string SmartMeasurementAfterEvidencePath { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }

        [XmlIgnore]
        public string FolderPath { get; set; }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Title))
                    return Title.Trim();
                return SessionDate == DateTime.MinValue ? "Bike Fit Session" : SessionDate.ToString("MMM d, yyyy");
            }
        }

        [XmlIgnore]
        public string StorageFolderName
        {
            get
            {
                DateTime date = SessionDate == DateTime.MinValue ? DateTime.Today : SessionDate;
                string id = Id == Guid.Empty ? "pending" : Id.ToString("N").Substring(0, 8);
                return string.Format("{0:yyyy-MM-dd}_{1}", date, id);
            }
        }

        [XmlIgnore]
        public string ManifestPath
        {
            get { return Path.Combine(FolderPath ?? string.Empty, "session.xml"); }
        }
    }
}
