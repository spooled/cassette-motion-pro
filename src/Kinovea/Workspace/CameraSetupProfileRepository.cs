/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using Kinovea.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CassetteMotionPro.Workspace
{
    [Serializable]
    public class CameraSetupProfile
    {
        public string Name { get; set; }
        public string LeftRole { get; set; }
        public string RightRole { get; set; }
        public string LeftCamera { get; set; }
        public string RightCamera { get; set; }
        public string Resolution { get; set; }
        public string FrameRate { get; set; }
        public string Notes { get; set; }

        [XmlIgnore]
        public bool IsBuiltIn { get; set; }

        public override string ToString()
        {
            return Name ?? "Camera setup";
        }
    }

    public class CameraSetupProfileRepository
    {
        private readonly string rootPath;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(CameraSetupProfile));

        public CameraSetupProfileRepository()
        {
            rootPath = Path.Combine(Software.SettingsDirectory, "Camera Profiles");
            Directory.CreateDirectory(rootPath);
        }

        public IList<CameraSetupProfile> LoadAll()
        {
            List<CameraSetupProfile> profiles = new List<CameraSetupProfile>(BuildBuiltIns());
            foreach (string path in Directory.GetFiles(rootPath, "*.xml"))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(path))
                    {
                        CameraSetupProfile profile = serializer.Deserialize(stream) as CameraSetupProfile;
                        if (profile != null && !string.IsNullOrWhiteSpace(profile.Name))
                            profiles.Add(profile);
                    }
                }
                catch (InvalidOperationException) { }
                catch (IOException) { }
            }
            return profiles.OrderBy(p => p.IsBuiltIn ? 0 : 1).ThenBy(p => p.Name).ToList();
        }

        public void Save(CameraSetupProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
                throw new ArgumentException("A camera profile name is required.", "profile");
            string path = Path.Combine(rootPath, MakeSafeFileName(profile.Name) + ".xml");
            using (FileStream stream = File.Create(path))
                serializer.Serialize(stream, profile);
        }

        public void Delete(CameraSetupProfile profile)
        {
            if (profile == null || profile.IsBuiltIn || string.IsNullOrWhiteSpace(profile.Name))
                return;
            string path = Path.Combine(rootPath, MakeSafeFileName(profile.Name) + ".xml");
            if (File.Exists(path))
                File.Delete(path);
        }

        private static IList<CameraSetupProfile> BuildBuiltIns()
        {
            return new List<CameraSetupProfile>
            {
                BuiltIn("Side + Front", "Drive-side profile", "Front profile", "1920 × 1080", "60 fps", "Place both cameras level, square to the rider, and at matching height."),
                BuiltIn("Drive + Non-drive", "Drive-side profile", "Non-drive profile", "1920 × 1080", "60 fps", "Match camera height, distance, zoom, and exposure on both sides."),
                BuiltIn("Side + Rear", "Drive-side profile", "Rear alignment", "1920 × 1080", "60 fps", "Keep the rear camera centered on the bike and the side camera square to the rider.")
            };
        }

        private static CameraSetupProfile BuiltIn(string name, string leftRole, string rightRole, string resolution, string frameRate, string notes)
        {
            return new CameraSetupProfile
            {
                Name = name,
                LeftRole = leftRole,
                RightRole = rightRole,
                LeftCamera = "Select in left Kinovea capture screen",
                RightCamera = "Select in right Kinovea capture screen",
                Resolution = resolution,
                FrameRate = frameRate,
                Notes = notes,
                IsBuiltIn = true
            };
        }

        private static string MakeSafeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] characters = value.Trim().ToCharArray();
            for (int index = 0; index < characters.Length; index++)
            {
                if (invalid.Contains(characters[index]) || char.IsWhiteSpace(characters[index]))
                    characters[index] = '_';
            }
            return new string(characters).Trim('_');
        }
    }
}
