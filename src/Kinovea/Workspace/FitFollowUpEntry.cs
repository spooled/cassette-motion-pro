/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */
using System;

namespace CassetteMotionPro.Workspace
{
    [Serializable]
    public class FitFollowUpEntry
    {
        public Guid Id { get; set; }
        public DateTime CheckInDate { get; set; }
        public string AdaptationStatus { get; set; }
        public int ComfortScore { get; set; }
        public int RidesCompleted { get; set; }
        public string RiderFeedback { get; set; }
        public string Symptoms { get; set; }
        public string FitterActions { get; set; }
        public bool HasNextCheckIn { get; set; }
        public DateTime NextCheckInDate { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
