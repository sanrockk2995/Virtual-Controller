using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VirtualController.App.Services;
using VirtualController.Input;
using VirtualController.Profiles;
using VirtualController.Backend;

namespace VirtualController.App.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly InputManager _inputManager;
        private readonly ProfileManager _profileManager;
        private readonly IVirtualController _controller;
        private readonly Action _openTesterAction;

        public DashboardView(
            InputManager inputManager,
            ProfileManager profileManager,
            IVirtualController controller,
            Action openTesterAction)
        {
            InitializeComponent();
            _inputManager = inputManager;
            _profileManager = profileManager;
            _controller = controller;
            _openTesterAction = openTesterAction;

            _inputManager.EnabledChanged += (s, e) => Dispatcher.Invoke(UpdateStatusUI);
            _profileManager.ActiveProfileChanged += (s, p) => Dispatcher.Invoke(UpdateProfileUI);

            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateStatusUI();
            UpdateProfileUI();
        }

        public void RefreshData()
        {
            UpdateStatusUI();
            UpdateProfileUI();
        }

        private void UpdateStatusUI()
        {
            if (_inputManager.IsEnabled)
            {
                StatusDot.Fill = (Brush)FindResource("AccentGreenBrush");
                StatusText.Text = LocalizationService.GetString("Dash_StatusActive", "Status: Active & Connected");
                StatusText.Foreground = (Brush)FindResource("AccentGreenBrush");
                BtnToggleController.Content = LocalizationService.GetString("Dash_BtnDisable", "Disable Controller");
                BtnToggleController.Background = (Brush)FindResource("AccentRedBrush");
            }
            else
            {
                StatusDot.Fill = (Brush)FindResource("AccentRedBrush");
                StatusText.Text = LocalizationService.GetString("Dash_StatusDisconnected", "Status: Disconnected");
                StatusText.Foreground = (Brush)FindResource("TextPrimaryBrush");
                BtnToggleController.Content = LocalizationService.GetString("Dash_BtnEnable", "Enable Controller");
                BtnToggleController.Background = (Brush)FindResource("AccentGreenBrush");
            }

            bool canConnect = DriverDetector.CanConnect();
            if (!canConnect)
            {
                DriverStatusText.Text = "ViGEmBus driver is missing or stopped.";
                DriverStatusText.Foreground = (Brush)FindResource("AccentRedBrush");
                BtnInstallDriver.Visibility = Visibility.Visible;
            }
            else
            {
                DriverStatusText.Text = _controller.DriverStatusMessage;
                DriverStatusText.Foreground = (Brush)FindResource("TextSecondaryBrush");
                BtnInstallDriver.Visibility = Visibility.Collapsed;
            }

            // Keyboard blocking status
            if (_inputManager.BlockPhysicalKeyboard)
            {
                TxtBlockStatus.Text = LocalizationService.GetString("Dash_BlockKeyboardActive", "Keyboard Blocking: ON");
                TxtBlockStatus.Foreground = (Brush)FindResource("AccentGreenBrush");
                BtnToggleBlock.Content = LocalizationService.GetString("Dash_BtnDisable", "Disable");
            }
            else
            {
                TxtBlockStatus.Text = LocalizationService.GetString("Dash_BlockKeyboardOff", "Keyboard Blocking: OFF");
                TxtBlockStatus.Foreground = (Brush)FindResource("TextSecondaryBrush");
                BtnToggleBlock.Content = LocalizationService.GetString("Dash_BtnEnable", "Enable");
            }
        }

        private void BtnToggleBlock_Click(object sender, RoutedEventArgs e)
        {
            _inputManager.BlockPhysicalKeyboard = !_inputManager.BlockPhysicalKeyboard;
            if (App.Settings != null)
            {
                App.Settings.BlockPhysicalKeyboard = _inputManager.BlockPhysicalKeyboard;
                App.Settings.Save();
            }
            UpdateStatusUI();
        }

        private bool _isUpdatingUi;

        private void UpdateProfileUI()
        {
            if (_isUpdatingUi) return;
            _isUpdatingUi = true;
            try
            {
                ProfileComboBox.ItemsSource = _profileManager.Profiles;
                ProfileComboBox.SelectedItem = _profileManager.ActiveProfile;

                ProfileSummaryTitle.Text = _profileManager.ActiveProfile?.Name ?? "No Profile Selected";
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void BtnToggleController_Click(object sender, RoutedEventArgs e)
        {
            if (_inputManager.IsEnabled)
            {
                _inputManager.Disable();
            }
            else
            {
                if (!DriverDetector.CanConnect())
                {
                    var result = MessageBox.Show(
                        "ViGEmBus driver is required to emulate an Xbox 360 controller, but it is not installed or running.\n\n" +
                        "Would you like to automatically download and install ViGEmBus now?",
                        "ViGEmBus Driver Required",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var dialog = new DriverInstallDialog(autoStart: true) { Owner = Window.GetWindow(this) };
                        dialog.ShowDialog();
                        _controller.Reinitialize();
                        UpdateStatusUI();

                        if (DriverDetector.CanConnect())
                        {
                            _inputManager.Enable();
                        }
                    }
                    UpdateStatusUI();
                    return;
                }

                bool success = _inputManager.Enable();
                if (!success)
                {
                    MessageBox.Show(
                        "Unable to connect virtual Xbox 360 controller.\n\n" +
                        _controller.DriverStatusMessage + "\n\n" +
                        "Please verify that the ViGEmBus driver is installed and running.",
                        "Driver Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            UpdateStatusUI();
        }

        private void BtnInstallDriver_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DriverInstallDialog(autoStart: true) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
            _controller.Reinitialize();
            UpdateStatusUI();
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi) return;

            if (ProfileComboBox.SelectedItem is Profile selected && selected != _profileManager.ActiveProfile)
            {
                _profileManager.SetActiveProfile(selected);
            }
        }

        private void BtnTestController_Click(object sender, RoutedEventArgs e)
        {
            _openTesterAction?.Invoke();
        }
    }
}
