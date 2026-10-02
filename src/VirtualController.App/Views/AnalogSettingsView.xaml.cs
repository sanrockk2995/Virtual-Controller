using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VirtualController.Profiles;

namespace VirtualController.App.Views
{
    public partial class AnalogSettingsView : UserControl
    {
        private readonly ProfileManager _profileManager;
        private bool _isUpdatingUi;
        private Button? _currentlyRebindingButton;
        private string? _rebindTargetTag;

        public AnalogSettingsView(ProfileManager profileManager)
        {
            InitializeComponent();
            _profileManager = profileManager;

            _profileManager.ActiveProfileChanged += (s, p) => Dispatcher.Invoke(LoadSettingsFromProfile);
            Loaded += (s, e) => LoadSettingsFromProfile();
        }

        public void LoadSettingsFromProfile()
        {
            var profile = _profileManager.ActiveProfile;
            if (profile == null) return;

            _isUpdatingUi = true;
            try
            {
                // --- 1. LEFT STICK ---
                bool leftIsMouse = profile.LeftStick.IsMouse;
                ComboLeftSource.SelectedIndex = leftIsMouse ? 1 : 0;
                PanelLeftKeyboard.Visibility = leftIsMouse ? Visibility.Collapsed : Visibility.Visible;
                PanelLeftMouse.Visibility = leftIsMouse ? Visibility.Visible : Visibility.Collapsed;

                // Left Keyboard controls
                BtnLeftKeyUp.Content = profile.LeftStick.Up;
                BtnLeftKeyDown.Content = profile.LeftStick.Down;
                BtnLeftKeyLeft.Content = profile.LeftStick.Left;
                BtnLeftKeyRight.Content = profile.LeftStick.Right;

                SliderLeftKbdDeadzone.Value = profile.LeftStick.Deadzone;
                TxtLeftKbdDeadzone.Text = profile.LeftStick.Deadzone.ToString("0.00");

                SliderLeftKbdSens.Value = profile.LeftStick.Sensitivity;
                TxtLeftKbdSens.Text = profile.LeftStick.Sensitivity.ToString("0.00");

                ComboLeftKbdCurve.SelectedIndex = string.Equals(profile.LeftStick.ResponseCurve, "Exponential", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

                // Left Mouse controls
                SliderLeftMouseSens.Value = profile.LeftStick.Sensitivity;
                TxtLeftMouseSens.Text = profile.LeftStick.Sensitivity.ToString("0.00");

                SliderLeftMouseDeadzone.Value = profile.LeftStick.Deadzone;
                TxtLeftMouseDeadzone.Text = profile.LeftStick.Deadzone.ToString("0.00");

                SliderLeftMouseSmoothing.Value = profile.LeftStick.Smoothing;
                TxtLeftMouseSmoothing.Text = profile.LeftStick.Smoothing.ToString("0.00");

                SliderLeftMouseAccel.Value = profile.LeftStick.Acceleration;
                TxtLeftMouseAccel.Text = profile.LeftStick.Acceleration.ToString("0.00");

                ChkLeftInvertX.IsChecked = profile.LeftStick.InvertX;
                ChkLeftInvertY.IsChecked = profile.LeftStick.InvertY;

                // --- 2. RIGHT STICK ---
                bool rightIsMouse = profile.RightStick.IsMouse;
                ComboRightSource.SelectedIndex = rightIsMouse ? 0 : 1;
                PanelRightMouse.Visibility = rightIsMouse ? Visibility.Visible : Visibility.Collapsed;
                PanelRightKeyboard.Visibility = rightIsMouse ? Visibility.Collapsed : Visibility.Visible;

                // Right Mouse controls
                SliderRightMouseSens.Value = profile.RightStick.Sensitivity;
                TxtRightMouseSens.Text = profile.RightStick.Sensitivity.ToString("0.00");

                SliderRightMouseDeadzone.Value = profile.RightStick.Deadzone;
                TxtRightMouseDeadzone.Text = profile.RightStick.Deadzone.ToString("0.00");

                SliderRightMouseSmoothing.Value = profile.RightStick.Smoothing;
                TxtRightMouseSmoothing.Text = profile.RightStick.Smoothing.ToString("0.00");

                SliderRightMouseAccel.Value = profile.RightStick.Acceleration;
                TxtRightMouseAccel.Text = profile.RightStick.Acceleration.ToString("0.00");

                ChkRightInvertX.IsChecked = profile.RightStick.InvertX;
                ChkRightInvertY.IsChecked = profile.RightStick.InvertY;

                // Right Keyboard controls
                BtnRightKeyUp.Content = string.IsNullOrEmpty(profile.RightStick.Up) ? "Up" : profile.RightStick.Up;
                BtnRightKeyDown.Content = string.IsNullOrEmpty(profile.RightStick.Down) ? "Down" : profile.RightStick.Down;
                BtnRightKeyLeft.Content = string.IsNullOrEmpty(profile.RightStick.Left) ? "Left" : profile.RightStick.Left;
                BtnRightKeyRight.Content = string.IsNullOrEmpty(profile.RightStick.Right) ? "Right" : profile.RightStick.Right;

                SliderRightKbdDeadzone.Value = profile.RightStick.Deadzone;
                TxtRightKbdDeadzone.Text = profile.RightStick.Deadzone.ToString("0.00");

                SliderRightKbdSens.Value = profile.RightStick.Sensitivity;
                TxtRightKbdSens.Text = profile.RightStick.Sensitivity.ToString("0.00");

                ComboRightKbdCurve.SelectedIndex = string.Equals(profile.RightStick.ResponseCurve, "Exponential", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void ComboLeftSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || PanelLeftKeyboard == null || PanelLeftMouse == null) return;
            bool isMouse = ComboLeftSource.SelectedIndex == 1;
            PanelLeftKeyboard.Visibility = isMouse ? Visibility.Collapsed : Visibility.Visible;
            PanelLeftMouse.Visibility = isMouse ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ComboRightSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || PanelRightKeyboard == null || PanelRightMouse == null) return;
            bool isMouse = ComboRightSource.SelectedIndex == 0;
            PanelRightMouse.Visibility = isMouse ? Visibility.Visible : Visibility.Collapsed;
            PanelRightKeyboard.Visibility = isMouse ? Visibility.Collapsed : Visibility.Visible;
        }

        #region Slider Handlers
        private void SliderLeftKbdDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtLeftKbdDeadzone != null) TxtLeftKbdDeadzone.Text = e.NewValue.ToString("0.00");
        }
        private void SliderLeftKbdSens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtLeftKbdSens != null) TxtLeftKbdSens.Text = e.NewValue.ToString("0.00");
        }
        private void SliderLeftMouseSens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtLeftMouseSens != null) TxtLeftMouseSens.Text = e.NewValue.ToString("0.00");
        }
        private void SliderLeftMouseDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtLeftMouseDeadzone != null) TxtLeftMouseDeadzone.Text = e.NewValue.ToString("0.00");
        }
        private void SliderLeftMouseSmoothing_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtLeftMouseSmoothing != null) TxtLeftMouseSmoothing.Text = e.NewValue.ToString("0.00");
        }
        private void SliderLeftMouseAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtLeftMouseAccel != null) TxtLeftMouseAccel.Text = e.NewValue.ToString("0.00");
        }

        private void SliderRightMouseSens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtRightMouseSens != null) TxtRightMouseSens.Text = e.NewValue.ToString("0.00");
        }
        private void SliderRightMouseDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtRightMouseDeadzone != null) TxtRightMouseDeadzone.Text = e.NewValue.ToString("0.00");
        }
        private void SliderRightMouseSmoothing_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtRightMouseSmoothing != null) TxtRightMouseSmoothing.Text = e.NewValue.ToString("0.00");
        }
        private void SliderRightMouseAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtRightMouseAccel != null) TxtRightMouseAccel.Text = e.NewValue.ToString("0.00");
        }
        private void SliderRightKbdDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtRightKbdDeadzone != null) TxtRightKbdDeadzone.Text = e.NewValue.ToString("0.00");
        }
        private void SliderRightKbdSens_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isUpdatingUi && TxtRightKbdSens != null) TxtRightKbdSens.Text = e.NewValue.ToString("0.00");
        }
        #endregion

        #region Interactive Stick Direction Key Rebinding
        private void BtnStickKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                if (_currentlyRebindingButton != null)
                {
                    CancelKeyRebind();
                }

                _currentlyRebindingButton = btn;
                _rebindTargetTag = tag;

                btn.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                btn.Content = "Press key...";
                StatusInfoText.Text = $"Press any key for [{tag.Replace('_', ' ')}]... (ESC to cancel)";

                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown += Window_PreviewKeyDown;
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_currentlyRebindingButton == null) return;
            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            if (key == Key.Escape)
            {
                CancelKeyRebind();
                return;
            }

            string keyName = key.ToString();
            if (key == Key.Return) keyName = "Enter";
            if (key == Key.Space) keyName = "Space";

            _currentlyRebindingButton.Content = keyName;
            _currentlyRebindingButton.Background = new SolidColorBrush(Color.FromRgb(40, 40, 56));
            StatusInfoText.Text = $"Assigned [{_rebindTargetTag?.Replace('_', ' ')}] → {keyName}. Click 'Save Analog Settings' to persist.";

            CleanupKeyListener();
        }

        private void CancelKeyRebind()
        {
            if (_currentlyRebindingButton != null)
            {
                _currentlyRebindingButton.Background = new SolidColorBrush(Color.FromRgb(40, 40, 56));
                StatusInfoText.Text = "Rebinding cancelled.";
            }
            CleanupKeyListener();
        }

        private void CleanupKeyListener()
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown;
            }
            _currentlyRebindingButton = null;
            _rebindTargetTag = null;
        }
        #endregion

        private void BtnSaveAnalog_Click(object sender, RoutedEventArgs e)
        {
            var profile = _profileManager.ActiveProfile;
            if (profile == null) return;

            // 1. Save Left Stick
            bool leftIsMouse = ComboLeftSource.SelectedIndex == 1;
            profile.LeftStick.InputSource = leftIsMouse ? "Mouse" : "Keyboard";

            if (leftIsMouse)
            {
                profile.LeftStick.Sensitivity = Math.Round(SliderLeftMouseSens.Value, 2);
                profile.LeftStick.Deadzone = Math.Round(SliderLeftMouseDeadzone.Value, 2);
                profile.LeftStick.Smoothing = Math.Round(SliderLeftMouseSmoothing.Value, 2);
                profile.LeftStick.Acceleration = Math.Round(SliderLeftMouseAccel.Value, 2);
                profile.LeftStick.InvertX = ChkLeftInvertX.IsChecked == true;
                profile.LeftStick.InvertY = ChkLeftInvertY.IsChecked == true;
            }
            else
            {
                profile.LeftStick.Up = BtnLeftKeyUp.Content?.ToString() ?? "W";
                profile.LeftStick.Down = BtnLeftKeyDown.Content?.ToString() ?? "S";
                profile.LeftStick.Left = BtnLeftKeyLeft.Content?.ToString() ?? "A";
                profile.LeftStick.Right = BtnLeftKeyRight.Content?.ToString() ?? "D";
                profile.LeftStick.Deadzone = Math.Round(SliderLeftKbdDeadzone.Value, 2);
                profile.LeftStick.Sensitivity = Math.Round(SliderLeftKbdSens.Value, 2);
                profile.LeftStick.ResponseCurve = (ComboLeftKbdCurve.SelectedIndex == 1) ? "Exponential" : "Linear";
            }

            // 2. Save Right Stick
            bool rightIsMouse = ComboRightSource.SelectedIndex == 0;
            profile.RightStick.InputSource = rightIsMouse ? "Mouse" : "Keyboard";

            if (rightIsMouse)
            {
                profile.RightStick.Sensitivity = Math.Round(SliderRightMouseSens.Value, 2);
                profile.RightStick.Deadzone = Math.Round(SliderRightMouseDeadzone.Value, 2);
                profile.RightStick.Smoothing = Math.Round(SliderRightMouseSmoothing.Value, 2);
                profile.RightStick.Acceleration = Math.Round(SliderRightMouseAccel.Value, 2);
                profile.RightStick.InvertX = ChkRightInvertX.IsChecked == true;
                profile.RightStick.InvertY = ChkRightInvertY.IsChecked == true;
            }
            else
            {
                profile.RightStick.Up = BtnRightKeyUp.Content?.ToString() ?? "Up";
                profile.RightStick.Down = BtnRightKeyDown.Content?.ToString() ?? "Down";
                profile.RightStick.Left = BtnRightKeyLeft.Content?.ToString() ?? "Left";
                profile.RightStick.Right = BtnRightKeyRight.Content?.ToString() ?? "Right";
                profile.RightStick.Deadzone = Math.Round(SliderRightKbdDeadzone.Value, 2);
                profile.RightStick.Sensitivity = Math.Round(SliderRightKbdSens.Value, 2);
                profile.RightStick.ResponseCurve = (ComboRightKbdCurve.SelectedIndex == 1) ? "Exponential" : "Linear";
            }

            _profileManager.SaveProfile(profile);
            MessageBox.Show("Analog stick settings saved successfully.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
