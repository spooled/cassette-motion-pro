/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.IO;
using System.Xml.Serialization;

namespace CassetteMotionPro.Clients
{
    [Serializable]
    public class ClientRecord
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string BikeMake { get; set; }
        public string BikeModel { get; set; }
        public string BikeYear { get; set; }
        public string BikeType { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastOpenedUtc { get; set; }

        [XmlIgnore]
        public string FolderPath { get; set; }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                string name = string.Format("{0} {1}", FirstName, LastName).Trim();
                return string.IsNullOrEmpty(name) ? "Unnamed Client" : name;
            }
        }

        [XmlIgnore]
        public string BikeDescription
        {
            get
            {
                string description = string.Format("{0} {1}", BikeMake, BikeModel).Trim();
                if (!string.IsNullOrEmpty(BikeYear))
                    description = string.Format("{0} ({1})", description, BikeYear).Trim();

                return string.IsNullOrEmpty(description) ? "No bike recorded" : description;
            }
        }

        [XmlIgnore]
        public string VideosPath { get { return Path.Combine(FolderPath ?? string.Empty, "Videos"); } }

        [XmlIgnore]
        public string PhotosPath { get { return Path.Combine(FolderPath ?? string.Empty, "Photos"); } }

        [XmlIgnore]
        public string ReportsPath { get { return Path.Combine(FolderPath ?? string.Empty, "Reports"); } }

        [XmlIgnore]
        public string MeasurementsPath { get { return Path.Combine(FolderPath ?? string.Empty, "Measurements"); } }

        [XmlIgnore]
        public string NotesPath { get { return Path.Combine(FolderPath ?? string.Empty, "Notes"); } }
    }
}
