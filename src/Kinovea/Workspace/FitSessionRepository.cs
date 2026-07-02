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
                if (!File.Exists(manifestPath))
                    continue;

                try
                {
                    using (FileStream stream = File.OpenRead(manifestPath))
                    {
                        FitSessionRecord session = serializer.Deserialize(stream) as FitSessionRecord;
                        if (session == null)
                            continue;
                        session.FolderPath = directory;
                        sessions.Add(session);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Keep one malformed session from blocking the whole workspace.
                }
                catch (IOException)
                {
                    // The session may be temporarily unavailable; retry on refresh.
                }
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

            if (string.IsNullOrEmpty(session.FolderPath))
            {
                string folderName = string.Format("{0:yyyy-MM-dd}_{1}", session.SessionDate, session.Id.ToString("N").Substring(0, 8));
                session.FolderPath = Path.Combine(RootPath, folderName);
            }

            Directory.CreateDirectory(session.FolderPath);
            using (FileStream stream = File.Create(Path.Combine(session.FolderPath, ManifestFileName)))
                serializer.Serialize(stream, session);

            return session;
        }
    }
}
