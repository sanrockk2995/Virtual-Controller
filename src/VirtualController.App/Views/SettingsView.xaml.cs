using System.Windows;
using System.Windows.Controls;
using VirtualController.App.Services;
using VirtualController.Services;
using VirtualController.Backend;

namespace VirtualController.App.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly AppSettings _settings;
        private bool _isInitializing = true;

        public SettingsView(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            Loaded += (s, e) => LoadSettingsUI();
        }

        private void LoadSettingsUI()
        {
            _isInitializing = true;
            try
            {
                ChkStartWithWindows.IsChecked = _settings.StartWithWindows;
                ChkStartMinimized.IsChecked = _settings.StartMinimized;
                ChkAutoEnable.IsChecked = _settings.AutoEnableController;
                ChkBlockKeyboard.IsChecked = _settings.BlockPhysicalKeyboard;

                ComboLanguage.SelectedIndex = (_settings.Language?.ToLowerInvariant() == "en") ? 1 : 0;

                CheckDriverStatus();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || ComboLanguage == null) return;
            string lang = (ComboLanguage.SelectedIndex == 1) ? "en" : "vi";
            LocalizationService.SetLanguage(lang, _settings);
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
            _settings.BlockPhysicalKeyboard = ChkBlockKeyboard.IsChecked == true;
            _settings.Language = (ComboLanguage.SelectedIndex == 1) ? "en" : "vi";

            if (App.InputManager != null)
            {
                App.InputManager.BlockPhysicalKeyboard = _settings.BlockPhysicalKeyboard;
            }

            _settings.Save();

            string msg = LocalizationService.GetString("Settings_SavedSuccess", "Settings saved successfully.");
            string title = LocalizationService.GetString("Settings_Title", "Settings");
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
