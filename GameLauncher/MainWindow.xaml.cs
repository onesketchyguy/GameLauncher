using System;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace GameLauncher
{
    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LauncherWindow : Window
    {
        public LauncherWindow()
        {
            InitializeComponent();

            Installer.Initialize(this);

            UpdateProgressBar(0);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (Installer.GetAppDirectory() == desktopPath)
            {
                System.Windows.MessageBox.Show("Please place this executable within a folder.\nThis deletes all files within this folder so you DO NOT want it on your desktop!", "Desktop detected!", MessageBoxButton.OK, MessageBoxImage.Stop);

                Environment.Exit(3);

                return; // Not necisarry since we are leaving the application, but just in case of error!
            }

            string version = Launcher.GetVersion();

            GameTitle.Content = Launcher.GameTitle;

            if (Installer.GameExists() == false)
            {
                ShowInstallButton();
            }
            else
            {
                try
                {
                    if (Installer.UpdateNeeded())
                    {
                        VersionCode.Text = $"Version:{version} - Update avialable!";
                    }
                    else
                    {
                        VersionCode.Text = $"Version:{version}";

                        if (Directory.GetCurrentDirectory() != Installer.GetAppDirectory())
                        {
                            HideUpdate();
                        }
                        else
                        {
                            ShowShortCutButton();
                        }
                    }
                }
                catch
                {
                    VersionCode.Text = (version != string.Empty) ? $"Version:{version}" : $"Failure to fetch version...";

                    HideUpdate();
                }
            }

            Installer.CleanUp();

            ChangeLog.Text = Launcher.GetChangeLog();

            SetTimer(5);
        }

        private static System.Timers.Timer aTimer;

        /// <summary>
        /// Wait for seconds before doing an action.
        /// </summary>
        /// <param name="time">Amount of time to wait in seconds,</param>
        private void SetTimer(int time)
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(time * 1000);
            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            UpdateBanner();

            aTimer.Stop();
            aTimer.Dispose();
        }

        private int currentBannerIndex = 0;

        public void UpdateBanner()
        {
            Thread.Yield();

            if (currentBannerIndex < 2)
            {
                currentBannerIndex++;
            }
            else
            {
                currentBannerIndex = 0;
            }

            this.Dispatcher.Invoke
                (() =>
                {
                    switch (currentBannerIndex)
                    {
                        case 0:
                            Banner.Source = file_0.Source;
                            break;

                        case 1:
                            Banner.Source = file_1.Source;
                            break;

                        case 2:
                            Banner.Source = file_2.Source;
                            break;
                    }
                }

                );

            SetTimer(5);
        }

        public void UpdateProgressBar(int progress)
        {
            try
            {
                Dispatcher.Invoke(() => ProgressBar.Value = progress);
            }
            catch
            {
                ProgressBar.Value = progress;

                Debug.Print("Unable to call dispatcher.");
            }

            if (progress == 0)
            {
                ProgressText.Text = $"";
            }
            else
            {
                ProgressText.Text = $"{ProgressBar.Value}%";
            }

            Debug.Print($"Updated progress bar. New value = {progress}");
        }

        public void ShowPlayButton()
        {
            //Play button
            if (PlayButton.IsEnabled == false)
            {
                PlayButton.IsEnabled = true;
                PlayButton.Visibility = Visibility.Visible;
            }
        }

        public void HideButtons()
        {
            //Play button
            if (PlayButton.IsEnabled)
            {
                PlayButton.IsEnabled = false;
                PlayButton.Visibility = Visibility.Hidden;
            }

            HideUpdate();

            //Website button
            if (WebsiteButton.IsEnabled)
            {
                WebsiteButton.IsEnabled = false;
                WebsiteButton.Visibility = Visibility.Hidden;
            }

            Debug.Print($"Hidden buttons");
        }

        private void HideUpdate()
        {
            //Update button
            if (UpdateButton.IsEnabled)
            {
                UpdateButton.IsEnabled = false;
                UpdateButton.Visibility = Visibility.Hidden;
            }
        }

        private void ShowShortCutButton()
        {
            UpdateButton.Content = "Create desktop shortcut";
        }

        private void ShowInstallButton()
        {
            VersionCode.Text = $"No current version installed..";

            PlayButton.Content = "Install game";
            UpdateButton.Visibility = Visibility.Hidden;
            UpdateButton.IsEnabled = false;
        }

        private void OpenWebsite_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchWebSite("https://www.lowbros.us/");
        }

        private void LaunchGameButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.PlayGame();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateButton.Content.ToString().ToLower() == "download updates")
            {
                Launcher.DownloadUpdates();
            }
            else
            {
                Installer.CreateDesktopShortcut();
            }
        }
    }
}