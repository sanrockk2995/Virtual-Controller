using System;
using System.Windows.Controls;
using System.Windows.Media;
using VirtualController.Core;
using VirtualController.Input;

namespace VirtualController.App.Views
{
    public partial class ControllerTesterView : UserControl
    {
        private readonly InputManager _inputManager;

        private readonly SolidColorBrush _bgNeutral = new(Color.FromRgb(22, 22, 26));
        private readonly SolidColorBrush _highlightBlue = new(Color.FromRgb(56, 189, 248));
        private readonly SolidColorBrush _fillA = new(Color.FromRgb(22, 163, 74));   // Green
        private readonly SolidColorBrush _fillB = new(Color.FromRgb(220, 38, 38));   // Red
        private readonly SolidColorBrush _fillX = new(Color.FromRgb(37, 99, 235));   // Blue
        private readonly SolidColorBrush _fillY = new(Color.FromRgb(202, 138, 4));   // Yellow

        public ControllerTesterView(InputManager inputManager)
        {
            InitializeComponent();
            _inputManager = inputManager;

            _inputManager.StateUpdated += OnStateUpdated;
        }

        private void OnStateUpdated(object? sender, ControllerState state)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => UpdateUI(state));
                return;
            }

            UpdateUI(state);
        }

        private void UpdateUI(ControllerState state)
        {
            // Triggers
            int ltPercent = (int)(state.LeftTrigger * 100);
            int rtPercent = (int)(state.RightTrigger * 100);

            BarLT.Value = ltPercent;
            TxtLTVal.Text = $"{ltPercent}%";

            BarRT.Value = rtPercent;
            TxtRTVal.Text = $"{rtPercent}%";

            // Bumpers
            BoxLB.Background = state.LeftShoulder ? _highlightBlue : _bgNeutral;
            BoxRB.Background = state.RightShoulder ? _highlightBlue : _bgNeutral;

            // Nav
            BoxBack.Background = state.Back ? _highlightBlue : _bgNeutral;
            BoxStart.Background = state.Start ? _highlightBlue : _bgNeutral;
            BoxGuide.Background = state.Guide ? _highlightBlue : _bgNeutral;

            // Thumb clicks
            BoxLS.Background = state.LeftThumb ? _highlightBlue : _bgNeutral;
            BoxRS.Background = state.RightThumb ? _highlightBlue : _bgNeutral;

            // D-Pad
            BoxDPadUp.Background = state.DPadUp ? _highlightBlue : _bgNeutral;
            BoxDPadDown.Background = state.DPadDown ? _highlightBlue : _bgNeutral;
            BoxDPadLeft.Background = state.DPadLeft ? _highlightBlue : _bgNeutral;
            BoxDPadRight.Background = state.DPadRight ? _highlightBlue : _bgNeutral;

            // Face Buttons
            BtnA.Background = state.A ? _fillA : _bgNeutral;
            BtnB.Background = state.B ? _fillB : _bgNeutral;
            BtnX.Background = state.X ? _fillX : _bgNeutral;
            BtnY.Background = state.Y ? _fillY : _bgNeutral;

            // Left Stick (WASD): Center at (56, 56) with range ~48px
            double leftCanvasX = 56 + (state.LeftThumbX * 45);
            double leftCanvasY = 56 - (state.LeftThumbY * 45);
            Canvas.SetLeft(DotLeftStick, Math.Clamp(leftCanvasX, 8, 104));
            Canvas.SetTop(DotLeftStick, Math.Clamp(leftCanvasY, 8, 104));
            TxtLeftStickCoords.Text = $"X: {state.LeftThumbX:+0.00;-0.00;0.00}  Y: {state.LeftThumbY:+0.00;-0.00;0.00}";

            // Right Stick (Mouse): Center at (47, 47) with range ~38px
            double rightCanvasX = 47 + (state.RightThumbX * 38);
            double rightCanvasY = 47 - (state.RightThumbY * 38);
            Canvas.SetLeft(DotRightStick, Math.Clamp(rightCanvasX, 6, 88));
            Canvas.SetTop(DotRightStick, Math.Clamp(rightCanvasY, 6, 88));
            TxtRightStickCoords.Text = $"X: {state.RightThumbX:+0.00;-0.00;0.00}  Y: {state.RightThumbY:+0.00;-0.00;0.00}";
        }
    }
}
