using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Windows;
using IWshRuntimeLibrary;
using Shell32;

namespace GameLauncher
{
    public class Installer
    {
        private static Assembly currentAssembly;

        public static string GetAppDirectory()
        {
            return Path.GetDirectoryName(currentAssembly.Location);
        }

        public static string GetAppName()
        {
            return Path.GetFileNameWithoutExtension(currentAssembly.Location);
        }

        public static string GetAppExtention()
        {
            return Path.GetExtension(currentAssembly.Location);
        }

        private static string archivePath;

        public static bool Updating = false;

        private static LauncherWindow MainWindow;

        /// <summary>
        /// Takes in the mainwindow, and applies the archive values.
        /// </summary>
        /// <param name="launcherWindow"></param>
        public static void Initialize(LauncherWindow launcherWindow)
        {
            MainWindow = launcherWindow;

            currentAssembly = Assembly.GetEntryAssembly();
            if (currentAssembly == null)
                currentAssembly = Assembly.GetCallingAssembly();

            archivePath = Path.Combine(GetAppDirectory(), $"{GetAppName()}_OldVersion{GetAppExtention()}");
        }

        public static bool GameExists()
        {
            if (Directory.Exists(Launcher.GameDataDirectory))
            {
                if (System.IO.File.Exists($"{GetAppDirectory()}/{Launcher.GameTitle}.exe"))
                {
                    return true;
                }
            }

            return false;
        }

        public static void InstallNewVersion()
        {
            Debug.Print($"Install new varsion called.");

            WebClient Client = new WebClient();

            Uri uri = new Uri("https://github.com/onesketchyguy/Goblin-Inc/archive/master.zip");

            try
            {
                MainWindow.UpdateProgressBar(10);

                Client.DownloadFileAsync(uri, "master.zip");

                //Extract file
                Client.DownloadFileCompleted += Client_DownloadFileCompleted;

                MainWindow.HideButtons();
            }
            catch
            {
                Updating = false;

                MessageBox.Show("Unable to download files...");
            }
        }

        private static void LoadUpdate()
        {
            Debug.Print($"Load update called.");

            DirectoryInfo directorySelected = new DirectoryInfo(GetAppDirectory());

            FileInfo[] filesToDecompress = directorySelected.GetFiles("*.zip");

            //Check if decompression was successful
            if (Unzip(filesToDecompress[0]) == true)
            {
                MainWindow.UpdateProgressBar(60);

                //Remove old files
                RemoveOldVersion();
                MainWindow.UpdateProgressBar(65);

                //Move Files
                MoveNewFoldersIntoPlace();
                MainWindow.UpdateProgressBar(90);
            }
            else
            {
                MessageBox.Show($"Failed to unzip files.\nExiting program.", "Failure to install!", MessageBoxButton.OK, MessageBoxImage.Error);

                Environment.Exit(2);
            }

            MessageBoxResult result = MessageBox.Show("Create desktop shortcut?", "", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                CreateDesktopShortcut();
            }

            MainWindow.UpdateProgressBar(100);

            MessageBox.Show($"Completed update. Restart of launcher required...");

            try
            {
                Process.Start($"{GetAppName()}.exe");
            }
            catch
            {
                MessageBox.Show("Failure to restart. A manual action will be required.");
            }

            Environment.Exit(2);
        }

        private static void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Debug.Print($"Downloaded update complete called.");

            MainWindow.UpdateProgressBar(50);

            LoadUpdate();
        }

        private static bool Unzip(FileInfo fileToDecompress)
        {
            Debug.Print($"Unzip update called.");

            bool success = false;

            using (ZipArchive zip = ZipFile.OpenRead(fileToDecompress.FullName))
            {
                try
                {
                    zip.ExtractToDirectory(GetAppDirectory());

                    success = true;
                }
                catch
                {
                    Directory.Delete($"{GetAppDirectory()}/Goblin-Inc-master", true);

                    try
                    {
                        Directory.Delete($"{GetAppDirectory()}/Goblin-Inc-master", true);

                        zip.ExtractToDirectory(GetAppDirectory());

                        success = true;
                    }
                    catch
                    {
                        MessageBox.Show("Unable to unzip file...", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            //Delete old folder.
            try
            {
                System.IO.File.Delete($"{GetAppDirectory()}/master.zip");

                success = true;
            }
            catch
            {
                MessageBox.Show("Unable to cleanup zip file...", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return success;
        }

        private static void RemoveOldVersion()
        {
            string[] files = Directory.GetFiles(GetAppDirectory());
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);

                try
                {
                    System.IO.File.Delete(file);
                }
                catch
                {
                    if (file == $"{Launcher.GameTitle}_Data/version.txt")
                    {
                        string c = System.IO.File.ReadAllText($"{GetAppDirectory()}/Goblin-Inc-master/{Launcher.GameTitle}_Data/version.txt");

                        System.IO.File.WriteAllText(file, c);

                        continue;
                    }

                    if (file.Contains(GetAppName())) continue;

                    MessageBox.Show($"Unable to delete file: {file}...\nWill continue regardless.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private static void MoveNewFoldersIntoPlace()
        {
            string dir = $"{GetAppDirectory()}/Goblin-Inc-master";

            CopyDir(dir, GetAppDirectory());
        }

        public static void CopyDir(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            // Get Files & Copy
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);

                // ADD Unique File Name Check to Below!!!!
                string dest = Path.Combine(destFolder, name);

                try
                {
                    System.IO.File.Copy(file, dest, true);

                    System.IO.File.Delete(file);
                }
                catch
                {
                    if (file.Contains(GetAppName()))
                    {
                        try
                        {
                            string app = Path.GetFileName(currentAssembly.Location);

                            System.IO.File.Move(app, archivePath);

                            System.IO.File.Copy(file, app, true);
                        }
                        catch
                        {
                            MessageBox.Show($"Unable to replace file: {file}...\nWill continue regardless.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                        MessageBox.Show($"Unable to replace file: {file}...\nWill continue regardless.", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Get dirs recursively and copy files
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyDir(folder, dest);
            }
        }

        public static void CleanUp()
        {
            //Remove all temp created files
            if (Directory.Exists($"{GetAppDirectory()}/Goblin-Inc-master"))
            {
                Directory.Delete($"{GetAppDirectory()}/Goblin-Inc-master", true);
            }

            if (System.IO.File.Exists(archivePath))
                System.IO.File.Delete(archivePath);
        }

        public static bool UpdateNeeded()
        {
            string url = "https://github.com/onesketchyguy/Goblin-Inc/blob/master/Goblin%20Inc_Data/version.txt";

            WebRequest request = WebRequest.Create(url);

            WebResponse response = request.GetResponse();

            StreamReader streamReader = new StreamReader(response.GetResponseStream());

            string newestVersion = streamReader.ReadToEnd();

            streamReader.Close();

            if (newestVersion.Contains(Launcher.GetVersion()))
            {
                return false; // No update needed, we are up to date.
            }
            else
            {
                return true; //There is a new update, it will need to be downloaded.
            }
        }

        public static void CreateDesktopShortcut()
        {
            string shortCutName = Launcher.GameTitle + ".lnk";
            string description = $"{Launcher.GameTitle} shortcut.";
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string exePos = $"{GetAppDirectory()}/{GetAppName()}{GetAppExtention()}";

            try
            {
                CreateShortcut(shortCutName, description, desktop, exePos);
                MessageBox.Show("Done!");
            }
            catch
            {
                MessageBox.Show("Unable to create desktop shortcut!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void CreateShortcut(string shortcutName, string description, string shortcutPath, string targetFileLocation)
        {
            string shortcutLocation = Path.Combine(shortcutPath, shortcutName);
            WshShell shell = new WshShell();

            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.WorkingDirectory = targetFileLocation;
            shortcut.Description = description;

            shortcut.TargetPath = targetFileLocation;
            shortcut.Save();
        }

        public static string GetShortcutTargetFile(string shortcutFilename)
        {
            string pathOnly = Path.GetDirectoryName(shortcutFilename);
            string filenameOnly = Path.GetFileName(shortcutFilename);

            Shell shell = new Shell();
            Shell32.Folder folder = shell.NameSpace(pathOnly);
            FolderItem folderItem = folder.ParseName(filenameOnly);
            if (folderItem != null)
            {
                ShellLinkObject link = (ShellLinkObject)folderItem.GetLink;
                return link.Path;
            }

            return "Unable to get path!";
        }
    }
}