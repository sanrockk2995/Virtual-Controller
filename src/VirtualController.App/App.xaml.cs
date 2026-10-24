using System;
using System.Linq;
using System.Threading;
using System.Windows;
using VirtualController.App.Services;
using VirtualController.Input;
using VirtualController.Profiles;
using VirtualController.Services;
using VirtualController.Backend;

namespace VirtualController.App
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        public static InputManager InputManager { get; private set; } = null!;
        public static ProfileManager ProfileManager { get; private set; } = null!;
        public static IVirtualController VirtualController { get; private set; } = null!;
        public static AppSettings Settings { get; private set; } = null!;
        public static TrayService TrayService { get; private set; } = null!;

        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "Local\\VirtualXbox360Controller_Antigravity";
            _singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Virtual Xbox 360 Controller is already running.", "Application Running", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    AppLogger.Error("Unhandled AppDomain exception.", ex);
                }
                InputManager?.EmergencyDisable();
            };

            DispatcherUnhandledException += (s, args) =>
            {
                AppLogger.Error("Unhandled Dispatcher exception.", args.Exception);
                InputManager?.EmergencyDisable();
                args.Handled = true;
            };

            AppDomain.CurrentDomain.ProcessExit += (s, args) =>
            {
                InputManager?.EmergencyDisable();
                InputManager?.Dispose();
            };

            // Initialize configuration and core services
            Settings = AppSettings.Load();
            LocalizationService.Initialize(Settings);
            ProfileManager = new ProfileManager();
            if (!string.IsNullOrWhiteSpace(Settings.ActiveProfileName))
            {
                ProfileManager.SetActiveProfileByName(Settings.ActiveProfileName);
            }

            VirtualController = new ViGEmController();
            InputManager = new InputManager(VirtualController, ProfileManager);

            // Tray Service
            TrayService = new TrayService(
                InputManager,
                ProfileManager,
                ShowWindow,
                ExitApplication);

            // Create Main Window
            _mainWindow = new MainWindow(InputManager, ProfileManager, VirtualController, Settings, TrayService);
            MainWindow = _mainWindow;

            bool startMinimized = Settings.StartMinimized || e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);
            if (!startMinimized)
            {
                _mainWindow.Show();
            }

            // Automatic driver check on startup
            if (!DriverDetector.CanConnect())
            {
                _mainWindow.Dispatcher.InvokeAsync(() =>
                {
                    var result = MessageBox.Show(
                        "ViGEmBus driver is required to emulate an Xbox 360 controller, but it is not installed or running on this system.\n\n" +
                        "Would you like to automatically download and install the official ViGEmBus driver now?",
                        "ViGEmBus Driver Setup",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var dialog = new Views.DriverInstallDialog(autoStart: true) { Owner = _mainWindow };
                        dialog.ShowDialog();
                        VirtualController.Reinitialize();
                        _mainWindow.RefreshViews();

                        if (Settings.AutoEnableController && DriverDetector.CanConnect())
                        {
                            InputManager.Enable();
                        }
                    }
                });
            }
            else if (Settings.AutoEnableController)
            {
                InputManager.Enable();
            }
        }

        public void ShowWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow(InputManager, ProfileManager, VirtualController, Settings, TrayService);
            }

            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
        }

        public void ExitApplication()
        {
            try
            {
                InputManager?.Disable();
                InputManager?.Dispose();
                TrayService?.Dispose();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error during application shutdown.", ex);
            }
            finally
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
                Shutdown();
            }
        }
    }
}
