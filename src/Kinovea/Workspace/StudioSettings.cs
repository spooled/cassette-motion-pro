/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

namespace CassetteMotionPro.Workspace
{
    public class StudioSettings
    {
        public string StudioName { get; set; }
        public string FitterName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string ReportRole { get; set; }
        public string CustomLogoPath { get; set; }

        public static StudioSettings CreateDefault()
        {
            return new StudioSettings
            {
                StudioName = "Cassette Fit Studio",
                FitterName = "Cesar Correa",
                Phone = string.Empty,
                Email = string.Empty,
                Website = string.Empty,
                ReportRole = "Professional Bike Fitting",
                CustomLogoPath = string.Empty
            };
        }
    }
}
