using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace CassetteMotionPro.Workspace
{
    internal sealed class WorkspaceLayoutPreferences
    {
        public int FitPanelWidth { get; set; }
        public bool FitPanelVisible { get; set; }
        public bool FitHeaderVisible { get; set; }
        public int SessionPaneWidth { get; set; }

        public WorkspaceLayoutPreferences()
        {
            FitPanelVisible = true;
            FitHeaderVisible = true;
        }

        public static WorkspaceLayoutPreferences Load()
        {
            WorkspaceLayoutPreferences preferences = new WorkspaceLayoutPreferences();
            try
            {
                string path = GetPath();
                if (!File.Exists(path))
                    return preferences;

                foreach (string line in File.ReadAllLines(path))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                        continue;
                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    int number;
                    bool flag;
                    if (key == "FitPanelWidth" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                        preferences.FitPanelWidth = number;
                    else if (key == "FitPanelVisible" && bool.TryParse(value, out flag))
                        preferences.FitPanelVisible = flag;
                    else if (key == "FitHeaderVisible" && bool.TryParse(value, out flag))
                        preferences.FitHeaderVisible = flag;
                    else if (key == "SessionPaneWidth" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                        preferences.SessionPaneWidth = number;
                }
            }
            catch
            {
                // Layout preferences are optional; use safe defaults if unavailable.
            }
            return preferences;
        }

        public void Save()
        {
            try
            {
                string path = GetPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                List<string> lines = new List<string>();
                lines.Add("FitPanelWidth=" + FitPanelWidth.ToString(CultureInfo.InvariantCulture));
                lines.Add("FitPanelVisible=" + FitPanelVisible.ToString(CultureInfo.InvariantCulture));
                lines.Add("FitHeaderVisible=" + FitHeaderVisible.ToString(CultureInfo.InvariantCulture));
                lines.Add("SessionPaneWidth=" + SessionPaneWidth.ToString(CultureInfo.InvariantCulture));
                File.WriteAllLines(path, lines.ToArray());
            }
            catch
            {
                // Never block a fit session because a cosmetic preference could not save.
            }
        }

        private static string GetPath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(root, "Cassette Motion Pro", "workspace-layout.txt");
        }
    }
}
