/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using Kinovea.Services;
using System;
using System.IO;
using System.Xml.Serialization;

namespace CassetteMotionPro.Workspace
{
    public static class StudioSettingsRepository
    {
        private const string FileName = "studio-settings.xml";
        private static readonly object SyncRoot = new object();
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(StudioSettings));
        private static StudioSettings current;

        public static StudioSettings Current
        {
            get
            {
                lock (SyncRoot)
                {
                    if (current == null)
                        current = LoadFromDisk();
                    return current;
                }
            }
        }

        public static void Save(StudioSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            lock (SyncRoot)
            {
                Directory.CreateDirectory(Software.SettingsDirectory);
                using (FileStream stream = File.Create(GetPath()))
                    Serializer.Serialize(stream, settings);
                current = settings;
            }
        }

        public static void Reload()
        {
            lock (SyncRoot)
                current = LoadFromDisk();
        }

        private static StudioSettings LoadFromDisk()
        {
            try
            {
                string path = GetPath();
                if (!File.Exists(path))
                    return StudioSettings.CreateDefault();
                using (FileStream stream = File.OpenRead(path))
                    return Serializer.Deserialize(stream) as StudioSettings ?? StudioSettings.CreateDefault();
            }
            catch (InvalidOperationException)
            {
                return StudioSettings.CreateDefault();
            }
            catch (IOException)
            {
                return StudioSettings.CreateDefault();
            }
            catch (UnauthorizedAccessException)
            {
                return StudioSettings.CreateDefault();
            }
        }

        private static string GetPath()
        {
            return Path.Combine(Software.SettingsDirectory, FileName);
        }
    }
}
