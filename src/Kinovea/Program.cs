/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Kinovea.Services;
using log4net;

namespace Kinovea.Root
{
    internal static class Program
    {
        private static bool IsFirstInstance
        {
            get
            {
                bool gotMutex;
                mutex = new Mutex(false, "Local\\" + appGuid, out gotMutex);
                return gotMutex;
            }
        }

        private static Mutex mutex;
        private static string appGuid = "b049b83e-90f3-4e84-9289-52ee6ea2a9ea";
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

            try
            {
                RunApplication();
            }
            catch (Exception exception)
            {
                ReportStartupCrash(exception);
            }
        }

        private static void RunApplication()
        {
            Thread.CurrentThread.Name = "Main";

            Assembly assembly = Assembly.GetExecutingAssembly();
            Software.Initialize(assembly.GetName().Version);

            Software.LogInfo();
            Software.SanityCheckDirectories();
            PreferencesManager.Initialize();
            PreferencesManager.Refresh();

            bool isFirstInstance = IsFirstInstance;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                CommandLineArgumentManager.Instance.ParseArguments(args);

            WindowManager.Startup(isFirstInstance);

            if (WindowManager.ActiveWindow == null)
                return;

            Software.ConfigureInstanceLogging();

            log.InfoFormat("-----------------------------------------------------------");
            log.InfoFormat(
                "Window:{0} ({1}). {2:yyyy-MM-dd HH:mm:ss}",
                WindowManager.ActiveWindow.Id,
                WindowManager.ActiveWindow.Name,
                DateTime.Now);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CassetteSplashScreen splashForm = new CassetteSplashScreen();
            splashForm.Show();
            splashForm.Update();

            RootKernel kernel = new RootKernel();
            kernel.Prepare();

            splashForm.Close();

            kernel.Launch();
        }

        private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception exception = args.ExceptionObject as Exception;
            if (exception == null)
                exception = new Exception(Convert.ToString(args.ExceptionObject));

            ReportStartupCrash(exception);
        }

        private static void ReportStartupCrash(Exception exception)
        {
            string logPath = WriteCrashLog(exception);

            try
            {
                MessageBox.Show(
                    "Cassette Motion Pro could not start.\n\nA crash log was saved here:\n" + logPath,
                    "Cassette Motion Pro Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // If Windows cannot show a message box, the text file is still the source of truth.
            }

            try
            {
                log.Error("----------------- Cassette Motion Pro Startup Crash -----------------", exception);
            }
            catch
            {
                // Logging may not be initialized when startup fails early.
            }
        }

        private static string WriteCrashLog(Exception exception)
        {
            string folder = GetCrashLogFolder();
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, "CassetteMotionPro-Crash-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
            File.WriteAllText(path, BuildCrashReport(exception), Encoding.UTF8);
            return path;
        }

        private static string GetCrashLogFolder()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (!string.IsNullOrEmpty(desktop))
                    return desktop;
            }
            catch
            {
            }

            try
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrEmpty(documents))
                    return documents;
            }
            catch
            {
            }

            return Path.GetTempPath();
        }

        private static string BuildCrashReport(Exception exception)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Cassette Motion Pro startup crash");
            builder.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine("Application: " + Application.ProductName);
            builder.AppendLine("Version: " + Application.ProductVersion);
            builder.AppendLine();
            builder.AppendLine(exception.ToString());
            return builder.ToString();
        }
    }
}
