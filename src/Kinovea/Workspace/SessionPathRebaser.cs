/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using CassetteMotionPro.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CassetteMotionPro.Workspace
{
    // Session records store absolute media and report paths. When a client folder is restored from
    // a backup or imported (possibly on another PC, drive or under a new folder name), those paths
    // still point at the old location. This points the ones that lived inside the client folder
    // at the folder's new location. Paths outside the client folder are left alone.
    public static class SessionPathRebaser
    {
        private static readonly string[] ClientSubfolders = { "Videos", "Photos", "Side-by-Side", "Reports", "Measurements", "Notes" };
        private static readonly PropertyInfo[] SessionPathProperties = typeof(FitSessionRecord)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(string) &&
                property.CanRead && property.CanWrite &&
                property.Name.EndsWith("Path", StringComparison.Ordinal) &&
                property.Name != "FolderPath")
            .ToArray();

        public static string RebasePath(string value, string oldClientFolderName, string newClientFolder)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(oldClientFolderName) || string.IsNullOrWhiteSpace(newClientFolder))
                return value;
            if (!IsRooted(value))
                return value;

            string[] parts = value.Split('\\', '/');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!string.Equals(parts[i], oldClientFolderName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!ClientSubfolders.Contains(parts[i + 1], StringComparer.OrdinalIgnoreCase))
                    continue;

                List<string> segments = new List<string> { newClientFolder };
                segments.AddRange(parts.Skip(i + 1).Where(part => part.Length > 0));
                return Path.Combine(segments.ToArray());
            }
            return value;
        }

        public static bool RebaseSession(FitSessionRecord session, string oldClientFolderName, string newClientFolder)
        {
            if (session == null)
                return false;

            bool changed = false;
            foreach (PropertyInfo property in SessionPathProperties)
            {
                string current = (string)property.GetValue(session, null);
                string rebased = RebasePath(current, oldClientFolderName, newClientFolder);
                if (!string.Equals(current, rebased, StringComparison.Ordinal))
                {
                    property.SetValue(session, rebased, null);
                    changed = true;
                }
            }

            if (session.FitPositions != null)
            {
                foreach (FitPositionRecord position in session.FitPositions)
                {
                    if (position == null)
                        continue;
                    string video = RebasePath(position.VideoPath, oldClientFolderName, newClientFolder);
                    if (!string.Equals(video, position.VideoPath, StringComparison.Ordinal))
                    {
                        position.VideoPath = video;
                        changed = true;
                    }
                    string image = RebasePath(position.ImagePath, oldClientFolderName, newClientFolder);
                    if (!string.Equals(image, position.ImagePath, StringComparison.Ordinal))
                    {
                        position.ImagePath = image;
                        changed = true;
                    }
                }
            }
            return changed;
        }

        // Rebases every saved session of a client. Returns how many sessions were updated.
        // A session that cannot be rewritten keeps its old paths and the rest still proceed.
        public static int RebaseClientSessions(ClientRecord client, string oldClientFolderName)
        {
            if (client == null || string.IsNullOrWhiteSpace(client.FolderPath))
                return 0;

            FitSessionRepository repository = new FitSessionRepository(client);
            int updated = 0;
            foreach (FitSessionRecord session in repository.LoadAll())
            {
                try
                {
                    if (!RebaseSession(session, oldClientFolderName, client.FolderPath))
                        continue;
                    repository.Save(session);
                    updated++;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return updated;
        }

        private static bool IsRooted(string value)
        {
            return value.StartsWith("\\", StringComparison.Ordinal) ||
                value.StartsWith("/", StringComparison.Ordinal) ||
                (value.Length >= 2 && value[1] == ':');
        }
    }
}
