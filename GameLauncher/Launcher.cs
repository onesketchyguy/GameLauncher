using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GameLauncher
{
    internal class Launcher
    {
        public const string GameTitle = "Goblin Inc";

        public static readonly string GameDataDirectory = $"{Installer.GetAppDirectory()}/{GameTitle}_Data";

        private static string currentVersion = "0.0.0";

        public static string GetChangeLog()
        {
            string changes = string.Empty;

            if (File.Exists($"{GameDataDirectory}/changelog.txt"))
            {
                string content = File.ReadAllText($"{GameDataDirectory}/changelog.txt");

                changes = content;
            }

            return changes;
        }

        public static string GetVersion()
        {
            if (File.Exists($"{GameDataDirectory}/version.txt"))
            {
                string content = File.ReadAllText($"{GameDataDirectory}/version.txt");

                string[] versionCut = content.Split(':');

                foreach (var value in versionCut)
                {
                    if (value.Contains("version")) continue;

                    currentVersion = value;
                }
            }
            else
            {
                Directory.CreateDirectory(GameDataDirectory);
            }

            File.WriteAllText($"{GameDataDirectory}/version.txt", $"version:{currentVersion}");

            return currentVersion;
        }

        public static void PlayGame()
        {
            //Open the exe file.
            try
            {
                Process.Start($"{Installer.GetAppDirectory()}/{GameTitle}.exe");

                Environment.Exit(-1);
            }
            catch
            {
                Installer.InstallNewVersion();
            }
        }

        public static void DownloadUpdates()
        {
            if (Installer.Updating == false)
            {
                Installer.Updating = true;

                MessageBox.Show($"Update available.\nDownloadning... This may take a while.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                Installer.InstallNewVersion();
            }
            else
            {
                MessageBox.Show($"Please wait, still updating. Do not close this window, it may result in file losses.");
            }
        }

        public static void LaunchWebSite(string url)
        {
            //Open the url in the users default browser.
            Process.Start(url);
        }
    }
}