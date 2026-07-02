/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
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
        public string Goals { get; set; }
        public string Notes { get; set; }

        public string SideVideoPath { get; set; }
        public string FrontVideoPath { get; set; }
        public string RearVideoPath { get; set; }
        public string BeforeVideoPath { get; set; }
        public string AfterVideoPath { get; set; }

        public string SaddleHeightBefore { get; set; }
        public string SaddleHeightAfter { get; set; }
        public string SaddleSetbackBefore { get; set; }
        public string SaddleSetbackAfter { get; set; }
        public string HandlebarReachBefore { get; set; }
        public string HandlebarReachAfter { get; set; }
        public string HandlebarDropBefore { get; set; }
        public string HandlebarDropAfter { get; set; }
        public string CrankLengthBefore { get; set; }
        public string CrankLengthAfter { get; set; }
        public string CleatPositionBefore { get; set; }
        public string CleatPositionAfter { get; set; }

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
        public string ManifestPath
        {
            get { return Path.Combine(FolderPath ?? string.Empty, "session.xml"); }
        }
    }
}
