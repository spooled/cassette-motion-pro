/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.IO;
using System.Xml.Serialization;

namespace CassetteMotionPro.Workspace
{
    internal sealed class FitSessionRecoveryStore
    {
        private readonly string recoveryFolder;
        private readonly string recoveryPath;
        private readonly string workspacePath;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(FitSessionRecord));

        public FitSessionRecoveryStore(string sessionsRoot)
        {
            recoveryFolder = Path.Combine(sessionsRoot, ".fit-day-recovery");
            recoveryPath = Path.Combine(recoveryFolder, "autosave.xml");
            workspacePath = Path.Combine(recoveryFolder, "workspace.txt");
        }

        public void Save(FitSessionRecord session, string workspaceSection)
        {
            if (session == null || session.Id == Guid.Empty)
                return;

            Directory.CreateDirectory(recoveryFolder);
            string temporaryPath = recoveryPath + ".tmp";
            using (FileStream stream = File.Create(temporaryPath))
                serializer.Serialize(stream, session);
            if (File.Exists(recoveryPath))
                File.Delete(recoveryPath);
            File.Move(temporaryPath, recoveryPath);
            File.WriteAllText(workspacePath, workspaceSection ?? string.Empty);
        }

        public bool TryLoad(out FitSessionRecord session, out string workspaceSection, out DateTime savedUtc)
        {
            session = null;
            workspaceSection = string.Empty;
            savedUtc = DateTime.MinValue;
            if (!File.Exists(recoveryPath))
                return false;

            try
            {
                using (FileStream stream = File.OpenRead(recoveryPath))
                    session = serializer.Deserialize(stream) as FitSessionRecord;
                if (session == null)
                    return false;
                workspaceSection = File.Exists(workspacePath) ? File.ReadAllText(workspacePath).Trim() : string.Empty;
                savedUtc = File.GetLastWriteTimeUtc(recoveryPath);
                return true;
            }
            catch (IOException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        public void Clear()
        {
            TryDelete(recoveryPath);
            TryDelete(recoveryPath + ".tmp");
            TryDelete(workspacePath);
            try
            {
                if (Directory.Exists(recoveryFolder) && Directory.GetFileSystemEntries(recoveryFolder).Length == 0)
                    Directory.Delete(recoveryFolder);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
