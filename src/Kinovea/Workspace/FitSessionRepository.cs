/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using CassetteMotionPro.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CassetteMotionPro.Workspace
{
    public class FitSessionRepository
    {
        private const string ManifestFileName = "session.xml";
        private const string BackupSuffix = ".bak";
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(FitSessionRecord));

        public ClientRecord Client { get; private set; }
        public string RootPath { get; private set; }

        public FitSessionRepository(ClientRecord client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (string.IsNullOrEmpty(client.FolderPath))
                throw new ArgumentException("The client folder is not available.", "client");

            Client = client;
            RootPath = Path.Combine(client.MeasurementsPath, "Sessions");
            Directory.CreateDirectory(RootPath);
        }

        public IList<FitSessionRecord> LoadAll()
        {
            List<FitSessionRecord> sessions = new List<FitSessionRecord>();
            Directory.CreateDirectory(RootPath);

            foreach (string directory in Directory.GetDirectories(RootPath))
            {
                string manifestPath = Path.Combine(directory, ManifestFileName);
                if (!File.Exists(manifestPath) && !File.Exists(manifestPath + BackupSuffix))
                    continue;

                FitSessionRecord session = TryRead(manifestPath) ?? TryRead(manifestPath + BackupSuffix);
                if (session == null)
                    continue;
                session.FolderPath = directory;
                session.EnsureStorageKey();
                sessions.Add(session);
            }

            return sessions
                .OrderByDescending(s => s.SessionDate)
                .ThenByDescending(s => s.ModifiedUtc)
                .ToList();
        }

        public FitSessionRecord Save(FitSessionRecord session)
        {
            if (session == null)
                throw new ArgumentNullException("session");

            DateTime now = DateTime.UtcNow;
            if (session.Id == Guid.Empty)
                session.Id = Guid.NewGuid();
            if (session.SessionDate == DateTime.MinValue)
                session.SessionDate = DateTime.Today;
            if (session.CreatedUtc == DateTime.MinValue)
                session.CreatedUtc = now;
            session.ModifiedUtc = now;

            session.EnsureStorageKey();
            if (string.IsNullOrEmpty(session.FolderPath))
            {
                session.FolderPath = Path.Combine(RootPath, session.StorageFolderName);
            }

            Directory.CreateDirectory(session.FolderPath);
            string manifestPath = Path.Combine(session.FolderPath, ManifestFileName);
            MergeFollowUpsFromDisk(session, manifestPath);
            string temporaryPath = manifestPath + ".tmp";
            using (FileStream stream = File.Create(temporaryPath))
                serializer.Serialize(stream, session);
            if (File.Exists(manifestPath))
                File.Replace(temporaryPath, manifestPath, manifestPath + BackupSuffix);
            else
                File.Move(temporaryPath, manifestPath);

            return session;
        }

        public FitSessionRecord FindById(Guid id)
        {
            if (id == Guid.Empty)
                return null;
            foreach (FitSessionRecord session in LoadAll())
            {
                if (session.Id == id)
                    return session;
            }
            return null;
        }

        // Follow-ups can be added from Client Manager while this session is open in Fit Day.
        // Nothing deletes follow-ups, so keep any saved entry the in-memory copy has not seen.
        private void MergeFollowUpsFromDisk(FitSessionRecord session, string manifestPath)
        {
            FitSessionRecord onDisk = TryRead(manifestPath) ?? TryRead(manifestPath + BackupSuffix);
            if (onDisk == null || onDisk.Id != session.Id || onDisk.FollowUps == null || onDisk.FollowUps.Count == 0)
                return;

            if (session.FollowUps == null)
                session.FollowUps = new List<FitFollowUpEntry>();
            foreach (FitFollowUpEntry saved in onDisk.FollowUps)
            {
                if (saved == null)
                    continue;
                bool known = false;
                foreach (FitFollowUpEntry current in session.FollowUps)
                {
                    if (current != null && current.Id == saved.Id)
                    {
                        known = true;
                        break;
                    }
                }
                if (!known)
                    session.FollowUps.Add(saved);
            }
        }

        private FitSessionRecord TryRead(string path)
        {
            if (!File.Exists(path))
                return null;
            try
            {
                using (FileStream stream = File.OpenRead(path))
                    return serializer.Deserialize(stream) as FitSessionRecord;
            }
            catch (InvalidOperationException) { return null; }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }
    }
}
