using System;
using System.Diagnostics;
using System.Windows;
using VirtualController.Services;

namespace VirtualController.App.Views
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateInfo _updateInfo;

        public UpdateDialog(UpdateInfo updateInfo)
        {
            InitializeComponent();
            _updateInfo = updateInfo ?? throw new ArgumentNullException(nameof(updateInfo));

            string currentVer = $"v{_updateInfo.CurrentVersion.Major}.{_updateInfo.CurrentVersion.Minor}.{_updateInfo.CurrentVersion.Build}";
            TxtVersionComparison.Text = $"{_updateInfo.TagName} (Current: {currentVer})";
            TxtReleaseNotes.Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes) 
                ? "No detailed release notes provided." 
                : _updateInfo.ReleaseNotes;
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            string url = !string.IsNullOrEmpty(_updateInfo.DownloadUrl) ? _updateInfo.DownloadUrl : _updateInfo.HtmlUrl;
            OpenBrowserUrl(url);
            DialogResult = true;
            Close();
        }

        private void BtnViewGitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowserUrl(_updateInfo.HtmlUrl);
        }

        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static void OpenBrowserUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to open URL: {url}", ex);
            }
        }
    }
}
