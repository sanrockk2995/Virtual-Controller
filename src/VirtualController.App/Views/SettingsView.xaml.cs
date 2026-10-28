using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VirtualController.App.Services;
using VirtualController.Services;
using VirtualController.Backend;

namespace VirtualController.App.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly AppSettings _settings;
        private bool _isInitializing = true;
        private bool _isRecordingHotkey = false;
        private string _currentHotkeyText = "Ctrl + Alt + F10";

        public SettingsView(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            Loaded += (s, e) => LoadSettingsUI();
            Unloaded += (s, e) => StopRecordingHotkey();
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

                _currentHotkeyText = string.IsNullOrWhiteSpace(_settings.Hotkey) ? "Ctrl + Alt + F10" : _settings.Hotkey;
                if (TxtHotkeyDisplay != null)
                {
                    TxtHotkeyDisplay.Text = _currentHotkeyText;
                }

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

        private void BtnChangeHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecordingHotkey)
            {
                StopRecordingHotkey();
                return;
            }

            _isRecordingHotkey = true;
            BtnChangeHotkey.Content = "Listening...";
            BtnChangeHotkey.Background = (Brush)FindResource("AccentRedBrush");
            TxtHotkeyHint.Text = LocalizationService.GetString("Settings_HotkeyListening", "Listening... (Press new combo or ESC to cancel)");
            TxtHotkeyHint.Foreground = (Brush)FindResource("AccentRedBrush");

            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown += Window_PreviewKeyDown;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRecordingHotkey) return;
            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.Escape)
            {
                StopRecordingHotkey();
                return;
            }

            // Ignore standalone modifier presses while waiting for the main key
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

            string keyName = key.ToString();
            if (key >= Key.F1 && key <= Key.F12) keyName = key.ToString();
            else if (key == Key.Return) keyName = "Enter";
            else if (key == Key.Space) keyName = "Space";

            var parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            parts.Add(keyName);

            _currentHotkeyText = string.Join(" + ", parts);
            TxtHotkeyDisplay.Text = _currentHotkeyText;

            StopRecordingHotkey();
        }

        private void StopRecordingHotkey()
        {
            if (!_isRecordingHotkey) return;
            _isRecordingHotkey = false;

            BtnChangeHotkey.Content = LocalizationService.GetString("Settings_BtnChangeHotkey", "Change Hotkey");
            BtnChangeHotkey.Background = (Brush)FindResource("AccentBlueBrush");
            TxtHotkeyHint.Text = LocalizationService.GetString("Settings_HotkeyDesc", "Global system hook hotkey: press anywhere to instantly freeze/disable virtual gamepad and release all inputs.");
            TxtHotkeyHint.Foreground = (Brush)FindResource("TextSecondaryBrush");

            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown;
            }
        }

        private void BtnResetHotkey_Click(object sender, RoutedEventArgs e)
        {
            StopRecordingHotkey();
            _currentHotkeyText = "Ctrl + Alt + F10";
            TxtHotkeyDisplay.Text = _currentHotkeyText;
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.StartWithWindows = ChkStartWithWindows.IsChecked == true;
            _settings.StartMinimized = ChkStartMinimized.IsChecked == true;
            _settings.AutoEnableController = ChkAutoEnable.IsChecked == true;
            _settings.BlockPhysicalKeyboard = ChkBlockKeyboard.IsChecked == true;
            _settings.Hotkey = _currentHotkeyText;
            _settings.Language = (ComboLanguage.SelectedIndex == 1) ? "en" : "vi";

            if (App.InputManager != null)
            {
                App.InputManager.BlockPhysicalKeyboard = _settings.BlockPhysicalKeyboard;
                App.InputManager.SetEmergencyHotkey(_settings.Hotkey);
            }

            _settings.Save();

            string msg = LocalizationService.GetString("Settings_SavedSuccess", "Settings saved successfully.");
            string title = LocalizationService.GetString("Settings_Title", "Settings");
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
