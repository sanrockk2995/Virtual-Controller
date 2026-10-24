using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VirtualController.App.Services;
using VirtualController.App.Views;
using VirtualController.Input;
using VirtualController.Profiles;
using VirtualController.Services;
using VirtualController.Backend;

namespace VirtualController.App
{
    public partial class MainWindow : Window
    {
        private readonly InputManager _inputManager;
        private readonly ProfileManager _profileManager;
        private readonly IVirtualController _controller;
        private readonly AppSettings _settings;
        private readonly TrayService _trayService;

        private readonly DashboardView _dashboardView;
        private readonly MappingEditorView _mappingEditorView;
        private readonly AnalogSettingsView _analogSettingsView;
        private readonly ControllerTesterView _controllerTesterView;
        private readonly ProfilesView _profilesView;
        private readonly SettingsView _settingsView;

        private bool _isExiting;
        private bool _hasShownMinimizeBalloon;

        public MainWindow(
            InputManager inputManager,
            ProfileManager profileManager,
            IVirtualController controller,
            AppSettings settings,
            TrayService trayService)
        {
            InitializeComponent();

            _inputManager = inputManager;
            _profileManager = profileManager;
            _controller = controller;
            _settings = settings;
            _trayService = trayService;

            // Instantiate views
            _controllerTesterView = new ControllerTesterView(_inputManager);
            _dashboardView = new DashboardView(_inputManager, _profileManager, _controller, () => SelectNavigation(NavTester, _controllerTesterView));
            _mappingEditorView = new MappingEditorView(_profileManager);
            _analogSettingsView = new AnalogSettingsView(_profileManager);
            _profilesView = new ProfilesView(_profileManager);
            _settingsView = new SettingsView(_settings);

            _inputManager.EnabledChanged += (s, enabled) => Dispatcher.Invoke(UpdateSidebarStatus);
            LocalizationService.LanguageChanged += (s, e) => Dispatcher.Invoke(UpdateSidebarStatus);

            // Default view
            SelectNavigation(NavDashboard, _dashboardView);
            UpdateSidebarStatus();
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn == NavDashboard) SelectNavigation(btn, _dashboardView);
                else if (btn == NavMapping) SelectNavigation(btn, _mappingEditorView);
                else if (btn == NavAnalog) SelectNavigation(btn, _analogSettingsView);
                else if (btn == NavTester) SelectNavigation(btn, _controllerTesterView);
                else if (btn == NavProfiles) SelectNavigation(btn, _profilesView);
                else if (btn == NavSettings) SelectNavigation(btn, _settingsView);
            }
        }

        private void SelectNavigation(Button activeBtn, UserControl view)
        {
            Button[] allNavButtons = { NavDashboard, NavMapping, NavAnalog, NavTester, NavProfiles, NavSettings };

            foreach (var b in allNavButtons)
            {
                b.Background = Brushes.Transparent;
                b.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 171));
            }

            activeBtn.Background = new SolidColorBrush(Color.FromRgb(34, 34, 43));
            activeBtn.Foreground = Brushes.White;

            MainContentHost.Content = view;

            if (view is DashboardView dv) dv.RefreshData();
            if (view is MappingEditorView mv) mv.LoadMappingsFromProfile();
            if (view is AnalogSettingsView av) av.LoadSettingsFromProfile();
            if (view is ProfilesView pv) pv.RefreshUI();
        }

        private void UpdateSidebarStatus()
        {
            if (_inputManager.IsEnabled)
            {
                SidebarStatusDot.Fill = (Brush)FindResource("AccentGreenBrush");
                SidebarStatusText.Text = LocalizationService.GetString("Status_Active", "Active (Connected)");
                SidebarStatusText.Foreground = (Brush)FindResource("AccentGreenBrush");
            }
            else
            {
                SidebarStatusDot.Fill = (Brush)FindResource("AccentRedBrush");
                SidebarStatusText.Text = LocalizationService.GetString("Status_Disabled", "Disabled");
                SidebarStatusText.Foreground = (Brush)FindResource("TextSecondaryBrush");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();

                if (!_hasShownMinimizeBalloon)
                {
                    _trayService.ShowNotification(
                        "Virtual Controller",
                        "Application minimized to system tray. Double-click tray icon to restore.");
                    _hasShownMinimizeBalloon = true;
                }
            }
            else
            {
                base.OnClosing(e);
            }
        }

        public void RefreshViews()
        {
            _dashboardView.RefreshData();
            UpdateSidebarStatus();
        }

        public void ForceClose()
        {
            _isExiting = true;
            Close();
        }
    }
}
