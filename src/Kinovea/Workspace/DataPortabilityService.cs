/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using CassetteMotionPro.Clients;
using Kinovea.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CassetteMotionPro.Workspace
{
    public enum ClientImportChoice
    {
        KeepBoth,
        Replace
    }

    public class ClientPackageInfo
    {
        public ClientRecord Client { get; set; }
        public bool AlreadyExists { get; set; }
    }

    public static class DataPortabilityService
    {
        private const string ManifestName = "cassette-motion-package.txt";
        private const string PackageHeader = "CASSETTE-MOTION-PRO-DATA";
        private static readonly string[] SettingsFolders = { "Clients", "Fit Templates", "Camera Profiles", "Studio Branding" };
        private static readonly string[] SettingsFiles = { "studio-settings.xml" };

        public static void CreateFullBackup(string archivePath)
        {
            CreateArchive(archivePath, "Full", null, delegate(ZipArchive archive)
            {
                foreach (string folderName in SettingsFolders)
                    AddDirectory(archive, Path.Combine(Software.SettingsDirectory, folderName), folderName);
                foreach (string fileName in SettingsFiles)
                    AddFile(archive, Path.Combine(Software.SettingsDirectory, fileName), fileName);

                string customLogo = StudioSettingsRepository.Current.CustomLogoPath;
                string brandingPath = Path.Combine(Software.SettingsDirectory, "Studio Branding") + Path.DirectorySeparatorChar;
                if (!string.IsNullOrWhiteSpace(customLogo) && File.Exists(customLogo) &&
                    !Path.GetFullPath(customLogo).StartsWith(Path.GetFullPath(brandingPath), StringComparison.OrdinalIgnoreCase))
                    AddFile(archive, customLogo, "Studio Branding/custom-logo" + Path.GetExtension(customLogo).ToLowerInvariant());
            });
        }

        public static void ExportClient(ClientRecord client, string archivePath)
        {
            if (client == null || string.IsNullOrWhiteSpace(client.FolderPath) || !Directory.Exists(client.FolderPath))
                throw new InvalidOperationException("The selected client folder is not available.");

            string folderName = Path.GetFileName(client.FolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            CreateArchive(archivePath, "Client", client, delegate(ZipArchive archive)
            {
                AddDirectory(archive, client.FolderPath, "Clients/" + folderName);
            });
        }

        public static string RestoreFullBackup(string archivePath)
        {
            ValidatePackage(archivePath, "Full");
            string safetyPath = GetAutomaticSafetyBackupPath();
            CreateFullBackup(safetyPath);

            string tempPath = ExtractSafely(archivePath);
            try
            {
                foreach (string folderName in SettingsFolders)
                {
                    string source = Path.Combine(tempPath, folderName);
                    if (Directory.Exists(source))
                        ReplaceDirectory(source, Path.Combine(Software.SettingsDirectory, folderName));
                }

                foreach (string fileName in SettingsFiles)
                {
                    string source = Path.Combine(tempPath, fileName);
                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(Software.SettingsDirectory, fileName), true);
                }

                StudioSettingsRepository.Reload();
                RelinkRestoredStudioLogo();
                return safetyPath;
            }
            finally
            {
                TryDeleteDirectory(tempPath);
            }
        }

        public static ClientPackageInfo InspectClientPackage(string archivePath)
        {
            ValidatePackage(archivePath, "Client");
            ClientRecord imported = ReadClientManifest(archivePath);
            ClientRepository repository = new ClientRepository();
            bool exists = repository.LoadAll().Any(client => client.Id == imported.Id);
            return new ClientPackageInfo { Client = imported, AlreadyExists = exists };
        }

        public static ClientRecord ImportClient(string archivePath, ClientImportChoice choice)
        {
            ClientPackageInfo info = InspectClientPackage(archivePath);
            string tempPath = ExtractSafely(archivePath);
            try
            {
                string clientsPath = Path.Combine(tempPath, "Clients");
                ClientRepository extractedRepository = new ClientRepository(clientsPath);
                ClientRecord imported = extractedRepository.LoadAll().Single();
                ClientRepository repository = new ClientRepository();
                ClientRecord existing = repository.LoadAll().FirstOrDefault(client => client.Id == imported.Id);

                string sourceFolder = imported.FolderPath;
                string destinationFolder;
                if (existing != null && choice == ClientImportChoice.Replace)
                {
                    string safetyFolder = Path.Combine(GetDefaultBackupFolder(), "Automatic Safety Backups");
                    Directory.CreateDirectory(safetyFolder);
                    string safetyName = "Before Client Replace " + SafeFileName(existing.DisplayName) + " " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".zip";
                    ExportClient(existing, Path.Combine(safetyFolder, safetyName));
                    destinationFolder = existing.FolderPath;
                    Directory.Delete(destinationFolder, true);
                }
                else
                {
                    if (existing != null)
                        imported.Id = Guid.NewGuid();
                    destinationFolder = GetUniqueClientFolder(repository.RootPath, Path.GetFileName(sourceFolder), imported.Id);
                }

                CopyDirectory(sourceFolder, destinationFolder);
                imported.FolderPath = destinationFolder;
                WriteClientManifest(imported);
                repository.EnsureFolders(imported);
                return imported;
            }
            finally
            {
                TryDeleteDirectory(tempPath);
            }
        }

        public static string GetDefaultBackupFolder()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = Path.Combine(documents, "Cassette Motion Pro Backups");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void CreateArchive(string archivePath, string type, ClientRecord client, Action<ZipArchive> addContent)
        {
            if (string.IsNullOrWhiteSpace(archivePath))
                throw new ArgumentException("A backup file path is required.", "archivePath");
            string parent = Path.GetDirectoryName(archivePath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            if (File.Exists(archivePath))
                File.Delete(archivePath);

            using (FileStream stream = File.Create(archivePath))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                ZipArchiveEntry manifest = archive.CreateEntry(ManifestName, CompressionLevel.Optimal);
                using (StreamWriter writer = new StreamWriter(manifest.Open(), Encoding.UTF8))
                {
                    writer.WriteLine(PackageHeader);
                    writer.WriteLine("FormatVersion=1");
                    writer.WriteLine("Type=" + type);
                    writer.WriteLine("CreatedUtc=" + DateTime.UtcNow.ToString("o"));
                    writer.WriteLine("ApplicationVersion=0.58.0");
                    if (client != null)
                    {
                        writer.WriteLine("ClientId=" + client.Id.ToString("D"));
                        writer.WriteLine("ClientName=" + client.DisplayName);
                    }
                }
                addContent(archive);
            }
        }

        private static void ValidatePackage(string archivePath, string expectedType)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException("The selected backup file could not be found.", archivePath);
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    ZipArchiveEntry manifest = archive.GetEntry(ManifestName);
                    if (manifest == null)
                        throw new InvalidDataException("This is not a Cassette Motion Pro data package.");
                    using (StreamReader reader = new StreamReader(manifest.Open(), Encoding.UTF8))
                    {
                        string contents = reader.ReadToEnd();
                        if (!contents.StartsWith(PackageHeader, StringComparison.Ordinal) ||
                            contents.IndexOf("Type=" + expectedType, StringComparison.OrdinalIgnoreCase) < 0)
                            throw new InvalidDataException("This package is not a valid " + expectedType.ToLowerInvariant() + " backup.");
                    }

                    int clientRecords = archive.Entries.Count(entry =>
                        entry.FullName.StartsWith("Clients/", StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.EndsWith("/client.xml", StringComparison.OrdinalIgnoreCase));
                    bool hasClientsRoot = archive.Entries.Any(entry =>
                        string.Equals(entry.FullName.TrimEnd('/'), "Clients", StringComparison.OrdinalIgnoreCase));
                    if (string.Equals(expectedType, "Full", StringComparison.OrdinalIgnoreCase) && !hasClientsRoot)
                        throw new InvalidDataException("The full backup is missing its client-data section.");
                    if (string.Equals(expectedType, "Client", StringComparison.OrdinalIgnoreCase) && clientRecords != 1)
                        throw new InvalidDataException("A client package must contain exactly one client record.");
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("The selected ZIP could not be read as a Cassette Motion Pro data package.", ex);
            }
        }

        private static ClientRecord ReadClientManifest(string archivePath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                ZipArchiveEntry entry = archive.Entries.FirstOrDefault(item =>
                    item.FullName.StartsWith("Clients/", StringComparison.OrdinalIgnoreCase) &&
                    item.FullName.EndsWith("/client.xml", StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                    throw new InvalidDataException("The client package does not contain a client record.");
                XmlSerializer serializer = new XmlSerializer(typeof(ClientRecord));
                using (Stream stream = entry.Open())
                {
                    ClientRecord client = serializer.Deserialize(stream) as ClientRecord;
                    if (client == null || client.Id == Guid.Empty)
                        throw new InvalidDataException("The client record in this package is invalid.");
                    return client;
                }
            }
        }

        private static void AddDirectory(ZipArchive archive, string sourcePath, string archiveRoot)
        {
            if (!Directory.Exists(sourcePath))
                return;
            archive.CreateEntry(NormalizeEntryName(archiveRoot) + "/");
            foreach (string file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                AddFile(archive, file, NormalizeEntryName(archiveRoot + "/" + relative));
            }
        }

        private static void AddFile(ZipArchive archive, string sourcePath, string entryName)
        {
            if (File.Exists(sourcePath))
                archive.CreateEntryFromFile(sourcePath, NormalizeEntryName(entryName), CompressionLevel.Optimal);
        }

        private static string ExtractSafely(string archivePath)
        {
            string destination = Path.Combine(Path.GetTempPath(), "CassetteMotionPro-Restore-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(destination);
            try
            {
                string root = Path.GetFullPath(destination + Path.DirectorySeparatorChar);
                using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string target = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("The backup contains an unsafe file path.");
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(target);
                            continue;
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        using (Stream source = entry.Open())
                        using (FileStream output = File.Create(target))
                            source.CopyTo(output);
                    }
                }
                return destination;
            }
            catch
            {
                TryDeleteDirectory(destination);
                throw;
            }
        }

        private static void ReplaceDirectory(string source, string destination)
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
            CopyDirectory(source, destination);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destination, directory.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }

        private static void RelinkRestoredStudioLogo()
        {
            string brandingFolder = Path.Combine(Software.SettingsDirectory, "Studio Branding");
            if (!Directory.Exists(brandingFolder))
                return;
            string logo = Directory.GetFiles(brandingFolder, "custom-logo.*").FirstOrDefault();
            if (string.IsNullOrEmpty(logo))
                return;
            StudioSettings settings = StudioSettingsRepository.Current;
            settings.CustomLogoPath = logo;
            StudioSettingsRepository.Save(settings);
        }

        private static string GetUniqueClientFolder(string root, string preferredName, Guid id)
        {
            string name = string.IsNullOrWhiteSpace(preferredName) ? "Imported_Client_" + id.ToString("N").Substring(0, 8) : preferredName;
            string path = Path.Combine(root, name);
            int suffix = 2;
            while (Directory.Exists(path))
                path = Path.Combine(root, name + "_" + suffix++);
            return path;
        }

        private static void WriteClientManifest(ClientRecord client)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ClientRecord));
            using (FileStream stream = File.Create(Path.Combine(client.FolderPath, "client.xml")))
                serializer.Serialize(stream, client);
        }

        private static string GetAutomaticSafetyBackupPath()
        {
            string folder = Path.Combine(GetDefaultBackupFolder(), "Automatic Safety Backups");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "Before Restore " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".zip");
        }

        private static string NormalizeEntryName(string value)
        {
            return value.Replace('\\', '/').TrimStart('/');
        }

        private static string SafeFileName(string value)
        {
            foreach (char character in Path.GetInvalidFileNameChars())
                value = value.Replace(character, '_');
            return value;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
