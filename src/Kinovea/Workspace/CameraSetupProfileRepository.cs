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
        public string Exposure { get; set; }
        public string ShutterSpeed { get; set; }
        public string DriverInterface { get; set; }
        public string LensCalibration { get; set; }
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
                BuiltIn("Professional USB 60 FPS", "Drive-side profile", "Front or rear profile", "1920 × 1080", "60 fps", "Manual / locked", "1/500 s", "DirectShow / UVC", "Verify with checkerboard", "Use this as the dependable starting point for supported USB cameras."),
                BuiltIn("IDS 60 FPS DirectShow", "Drive-side profile", "Front or rear profile", "1920 × 1080", "60 fps", "Manual / locked", "1/500 s", "IDS peak DirectShow", "Required before measurements", "Install IDS peak with the DirectShow component, then select the IDS device in Video Studio capture controls."),
                BuiltIn("Side + Front", "Drive-side profile", "Front profile", "1920 × 1080", "60 fps", "Manual / locked", "1/500 s", "DirectShow / UVC", "Verify with checkerboard", "Place both cameras level, square to the rider, and at matching height."),
                BuiltIn("Drive + Non-drive", "Drive-side profile", "Non-drive profile", "1920 × 1080", "60 fps", "Manual / matched", "1/500 s", "DirectShow / UVC", "Verify both lenses", "Match camera height, distance, zoom, exposure, and white balance on both sides."),
                BuiltIn("Side + Rear", "Drive-side profile", "Rear alignment", "1920 × 1080", "60 fps", "Manual / locked", "1/500 s", "DirectShow / UVC", "Verify with checkerboard", "Keep the rear camera centered on the bike and the side camera square to the rider."),
                BuiltIn("GoPro HERO webcam (USB)", "Drive-side profile", "Not used", "1920 × 1080", "30 fps (GoPro webcam limit)", "Protune, exposure locked", "1/500 s or faster", "GoPro Webcam app (DirectShow)", "Verify with checkerboard (Linear lens)", "Install the GoPro Webcam app and start it before Cassette Motion Pro. On the GoPro set USB Connection to GoPro Connect, connect with a data USB-C cable, and wait for the blue dot on the GoPro Webcam tray icon. In the GoPro Webcam app choose 1080p and the Linear lens, and close its preview window (only one program can use the camera). Then select GoPro Webcam in the Video Studio capture screen. Webcam mode is 30 fps at most; for 120 fps, record on the GoPro and open the file instead."),
                BuiltIn("ELP global shutter USB 3 (AR0234)", "Drive-side profile", "Not used", "1920 × 1080", "60–90 fps (MJPG)", "Manual / locked", "1/1000 s (1 ms) or faster", "DirectShow / UVC, MJPG", "Verify with checkerboard after setting zoom and focus", "Plug the camera into a USB 3 (blue) port directly on the computer, not a hub or front-panel port, with the cable it came with. In the camera settings choose an MJPG 1920 × 1080 media type at 60 or 90 fps; raw (YUY2) full-size modes run at only a few frames per second and look choppy or black. Turn auto exposure off and set a short exposure (about 1 ms) so the frame rate cannot drop in dim light, then raise gain and add light until the picture is bright enough. Set zoom and focus on the rider, lock them, and only then do the lens check. Cassette Motion Pro picks MJPG 1920 × 1080 by itself the first time it connects to the camera.")
            };
        }

        private static CameraSetupProfile BuiltIn(string name, string leftRole, string rightRole, string resolution, string frameRate, string exposure, string shutterSpeed, string driverInterface, string lensCalibration, string notes)
        {
            return new CameraSetupProfile
            {
                Name = name,
                LeftRole = leftRole,
                RightRole = rightRole,
                LeftCamera = "Select in left Video Studio capture screen",
                RightCamera = "Select in right Video Studio capture screen",
                Resolution = resolution,
                FrameRate = frameRate,
                Exposure = exposure,
                ShutterSpeed = shutterSpeed,
                DriverInterface = driverInterface,
                LensCalibration = lensCalibration,
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
