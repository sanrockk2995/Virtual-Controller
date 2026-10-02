using System;
using System.Drawing;
using System.Windows.Forms;
using VirtualController.Input;
using VirtualController.Profiles;
using VirtualController.Services;

namespace VirtualController.App.Services
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly InputManager _inputManager;
        private readonly ProfileManager _profileManager;
        private readonly Action _showWindowAction;
        private readonly Action _exitAction;
        private bool _disposed;

        public TrayService(
            InputManager inputManager,
            ProfileManager profileManager,
            Action showWindowAction,
            Action exitAction)
        {
            _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _showWindowAction = showWindowAction ?? throw new ArgumentNullException(nameof(showWindowAction));
            _exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateAppIcon(),
                Text = "Virtual Xbox 360 Controller",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => _showWindowAction();
            _inputManager.EnabledChanged += (s, enabled) => UpdateContextMenu();
            _profileManager.ActiveProfileChanged += (s, p) => UpdateContextMenu();

            UpdateContextMenu();
        }

        private static Icon CreateAppIcon()
        {
            // Generate a crisp 32x32 gamepad icon dynamically
            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Controller body
                using var bodyBrush = new SolidBrush(Color.FromArgb(30, 144, 255)); // DodgerBlue
                g.FillRoundedRectangle(bodyBrush, 2, 8, 28, 16, 6);

                // Thumbsticks
                using var stickBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
                g.FillEllipse(stickBrush, 6, 12, 6, 6);
                g.FillEllipse(stickBrush, 18, 14, 6, 6);

                // Buttons
                using var btnBrush = new SolidBrush(Color.FromArgb(50, 205, 50)); // LimeGreen
                g.FillEllipse(btnBrush, 22, 10, 3, 3);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        public void UpdateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var titleItem = new ToolStripMenuItem("Virtual Controller") { Enabled = false };
            titleItem.Font = new Font(titleItem.Font, FontStyle.Bold);
            menu.Items.Add(titleItem);

            menu.Items.Add(new ToolStripSeparator());

            // Enable / Disable
            if (_inputManager.IsEnabled)
            {
                var disableItem = new ToolStripMenuItem("Disable Controller", null, (s, e) => _inputManager.Disable());
                menu.Items.Add(disableItem);
            }
            else
            {
                var enableItem = new ToolStripMenuItem("Enable Controller", null, (s, e) => _inputManager.Enable());
                menu.Items.Add(enableItem);
            }

            // Profiles Submenu
            var profilesSubmenu = new ToolStripMenuItem($"Profile ({_profileManager.ActiveProfile.Name})");
            foreach (var p in _profileManager.Profiles)
            {
                var item = new ToolStripMenuItem(p.Name, null, (s, e) => _profileManager.SetActiveProfile(p));
                item.Checked = (p == _profileManager.ActiveProfile);
                profilesSubmenu.DropDownItems.Add(item);
            }
            menu.Items.Add(profilesSubmenu);

            menu.Items.Add(new ToolStripSeparator());

            // Open & Exit
            menu.Items.Add(new ToolStripMenuItem("Open Dashboard", null, (s, e) => _showWindowAction()));
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) => _exitAction()));

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.Text = $"Virtual Controller ({(_inputManager.IsEnabled ? "Enabled" : "Disabled")})";
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon.ShowBalloonTip(2000, title, message, icon);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
