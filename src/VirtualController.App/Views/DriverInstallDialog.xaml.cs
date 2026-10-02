using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using VirtualController.Backend;

namespace VirtualController.App.Views
{
    public partial class DriverInstallDialog : Window
    {
        public bool IsInstalledSuccessfully { get; private set; }

        public DriverInstallDialog(bool autoStart = false)
        {
            InitializeComponent();

            if (autoStart)
            {
                Loaded += async (s, e) => await StartInstallationAsync();
            }
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            await StartInstallationAsync();
        }

        public async Task StartInstallationAsync()
        {
            BtnInstall.IsEnabled = false;
            BtnClose.IsEnabled = false;
            ProgressBarInstall.Visibility = Visibility.Visible;
            ProgressBarInstall.IsIndeterminate = true;

            var progress = new Progress<string>(msg =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = msg;
                });
            });

            bool success = await DriverInstaller.InstallDriverAsync(progress);

            ProgressBarInstall.Visibility = Visibility.Collapsed;
            BtnClose.IsEnabled = true;

            if (success)
            {
                IsInstalledSuccessfully = true;
                TxtStatus.Text = "ViGEmBus driver is successfully installed and verified!";
                TxtStatus.Foreground = (Brush)FindResource("AccentGreenBrush");
                BtnInstall.Visibility = Visibility.Collapsed;
                BtnClose.Content = "Done";
                BtnClose.Background = (Brush)FindResource("AccentGreenBrush");

                // Hot-reinitialize virtual controller client
                App.VirtualController?.Reinitialize();
            }
            else
            {
                TxtStatus.Foreground = (Brush)FindResource("AccentRedBrush");
                BtnInstall.IsEnabled = true;
                BtnInstall.Content = "Retry Installation";
            }
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/nefarius/ViGEmBus/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = IsInstalledSuccessfully;
            Close();
        }
    }
}
