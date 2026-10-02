using System.Windows;
using System.Windows.Controls;
using VirtualController.Services;
using VirtualController.Backend;

namespace VirtualController.App.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly AppSettings _settings;

        public SettingsView(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            Loaded += (s, e) => LoadSettingsUI();
        }

        private void LoadSettingsUI()
        {
            ChkStartWithWindows.IsChecked = _settings.StartWithWindows;
            ChkStartMinimized.IsChecked = _settings.StartMinimized;
            ChkAutoEnable.IsChecked = _settings.AutoEnableController;

            CheckDriverStatus();
        }

        private void CheckDriverStatus()
        {
            bool connected = DriverDetector.CanConnect();
            TxtDriverDiagnosis.Text = DriverDetector.GetStatusReport();
            TxtDriverDiagnosis.Foreground = (System.Windows.Media.Brush)FindResource(connected ? "AccentGreenBrush" : "AccentRedBrush");
            BtnInstallDriver.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnCheckDriver_Click(object sender, RoutedEventArgs e)
        {
            CheckDriverStatus();
            MessageBox.Show(TxtDriverDiagnosis.Text, "ViGEmBus Driver Check", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnInstallDriver_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DriverInstallDialog(autoStart: true) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
            App.VirtualController.Reinitialize();
            CheckDriverStatus();
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.StartWithWindows = ChkStartWithWindows.IsChecked == true;
            _settings.StartMinimized = ChkStartMinimized.IsChecked == true;
            _settings.AutoEnableController = ChkAutoEnable.IsChecked == true;

            _settings.Save();

            MessageBox.Show("Settings saved successfully.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
