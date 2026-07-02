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

namespace CassetteMotionPro.Clients
{
    public class ClientRepository
    {
        private const string ManifestFileName = "client.xml";
        private static readonly string[] ClientFolders = { "Videos", "Photos", "Reports", "Measurements", "Notes" };
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(ClientRecord));

        public string RootPath { get; private set; }

        public ClientRepository()
            : this(Path.Combine(Software.SettingsDirectory, "Clients"))
        {
        }

        public ClientRepository(string rootPath)
        {
            RootPath = rootPath;
            Directory.CreateDirectory(RootPath);
        }

        public IList<ClientRecord> LoadAll()
        {
            List<ClientRecord> clients = new List<ClientRecord>();
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
                        ClientRecord client = serializer.Deserialize(stream) as ClientRecord;
                        if (client == null)
                            continue;

                        client.FolderPath = directory;
                        clients.Add(client);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignore malformed records so one damaged client cannot block the manager.
                }
                catch (IOException)
                {
                    // Ignore temporarily unavailable records and try again on refresh.
                }
            }

            return clients
                .OrderByDescending(c => c.LastOpenedUtc)
                .ThenBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToList();
        }

        public ClientRecord Create(ClientRecord client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            if (string.IsNullOrWhiteSpace(client.FirstName) && string.IsNullOrWhiteSpace(client.LastName))
                throw new ArgumentException("A client name is required.", "client");

            client.Id = client.Id == Guid.Empty ? Guid.NewGuid() : client.Id;
            client.CreatedUtc = client.CreatedUtc == DateTime.MinValue ? DateTime.UtcNow : client.CreatedUtc;
            client.LastOpenedUtc = DateTime.UtcNow;
            client.FolderPath = GetNewClientPath(client);

            Directory.CreateDirectory(client.FolderPath);
            foreach (string folder in ClientFolders)
                Directory.CreateDirectory(Path.Combine(client.FolderPath, folder));

            Save(client);
            return client;
        }

        public void MarkOpened(ClientRecord client)
        {
            if (client == null)
                return;

            client.LastOpenedUtc = DateTime.UtcNow;
            EnsureFolders(client);
            Save(client);
        }

        public void EnsureFolders(ClientRecord client)
        {
            if (client == null || string.IsNullOrEmpty(client.FolderPath))
                return;

            Directory.CreateDirectory(client.FolderPath);
            foreach (string folder in ClientFolders)
                Directory.CreateDirectory(Path.Combine(client.FolderPath, folder));
        }

        private void Save(ClientRecord client)
        {
            string manifestPath = Path.Combine(client.FolderPath, ManifestFileName);
            using (FileStream stream = File.Create(manifestPath))
                serializer.Serialize(stream, client);
        }

        private string GetNewClientPath(ClientRecord client)
        {
            string name = string.Format("{0}_{1}", client.LastName, client.FirstName).Trim('_');
            name = MakeSafeFileName(name);
            if (string.IsNullOrEmpty(name))
                name = "Client";

            string folderName = string.Format("{0}_{1}", name, client.Id.ToString("N").Substring(0, 8));
            return Path.Combine(RootPath, folderName);
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            char[] invalid = Path.GetInvalidFileNameChars();
            char[] characters = value.Trim().ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (invalid.Contains(characters[i]) || char.IsWhiteSpace(characters[i]))
                    characters[i] = '_';
            }

            return new string(characters).Trim('_');
        }
    }
}
