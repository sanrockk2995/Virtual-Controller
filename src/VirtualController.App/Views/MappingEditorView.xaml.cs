using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VirtualController.Profiles;

namespace VirtualController.App.Views
{
    public class MappingItemViewModel : INotifyPropertyChanged
    {
        private string _physicalInput = "";
        private string _actionText = "Rebind";
        private Brush _actionBackground = new SolidColorBrush(Color.FromRgb(37, 99, 235));

        public string VirtualButton { get; set; } = "";
        public string Description { get; set; } = "";

        public string PhysicalInput
        {
            get => _physicalInput;
            set { _physicalInput = value; OnPropertyChanged(); }
        }

        public string ActionText
        {
            get => _actionText;
            set { _actionText = value; OnPropertyChanged(); }
        }

        public Brush ActionBackground
        {
            get => _actionBackground;
            set { _actionBackground = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MappingEditorView : UserControl
    {
        private readonly ProfileManager _profileManager;
        private MappingItemViewModel? _currentlyRebindingItem;
        public ObservableCollection<MappingItemViewModel> Items { get; } = new();

        public MappingEditorView(ProfileManager profileManager)
        {
            InitializeComponent();
            _profileManager = profileManager;

            _profileManager.ActiveProfileChanged += (s, p) => Dispatcher.Invoke(LoadMappingsFromProfile);
            Loaded += (s, e) => LoadMappingsFromProfile();
        }

        public void LoadMappingsFromProfile()
        {
            Items.Clear();
            var profile = _profileManager.ActiveProfile;
            if (profile == null) return;

            var buttonDefinitions = new (string vBtn, string desc)[]
            {
                ("A", "Jump / Accept"),
                ("B", "Melee / Cancel"),
                ("X", "Shoot / Action"),
                ("Y", "Special / Weapon"),
                ("LB", "Left Bumper"),
                ("RB", "Right Bumper"),
                ("LT", "Left Trigger (Analog)"),
                ("RT", "Right Trigger (Analog)"),
                ("Back", "Back / Map"),
                ("Start", "Start / Menu"),
                ("LS", "Left Stick Click"),
                ("RS", "Right Stick Click"),
                ("DPadUp", "D-Pad Up"),
                ("DPadDown", "D-Pad Down"),
                ("DPadLeft", "D-Pad Left"),
                ("DPadRight", "D-Pad Right")
            };

            foreach (var (vBtn, desc) in buttonDefinitions)
            {
                profile.Buttons.TryGetValue(vBtn, out string? pInput);
                Items.Add(new MappingItemViewModel
                {
                    VirtualButton = vBtn,
                    Description = desc,
                    PhysicalInput = string.IsNullOrEmpty(pInput) ? "Unassigned" : pInput
                });
            }

            MappingItemsControl.ItemsSource = Items;
        }

        private void BtnRebind_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string vBtn)
            {
                if (_currentlyRebindingItem != null)
                {
                    CancelRebinding();
                }

                foreach (var item in Items)
                {
                    if (item.VirtualButton == vBtn)
                    {
                        _currentlyRebindingItem = item;
                        item.ActionText = "Listening...";
                        item.ActionBackground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                        StatusInfoText.Text = $"Press any Keyboard key or Mouse button for [{item.VirtualButton}]... (ESC to cancel)";
                        break;
                    }
                }

                // Attach global preview listener on Window
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown += Window_PreviewKeyDown;
                    window.PreviewMouseDown += Window_PreviewMouseDown;
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_currentlyRebindingItem == null) return;
            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.Escape)
            {
                CancelRebinding();
                return;
            }

            string keyName = key.ToString();
            // Handle common keys
            if (key == Key.LeftShift || key == Key.RightShift) keyName = key.ToString();
            if (key == Key.Space) keyName = "Space";
            if (key == Key.Return) keyName = "Enter";

            AssignInput(keyName);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentlyRebindingItem == null) return;
            e.Handled = true;

            string mouseBtn = e.ChangedButton switch
            {
                MouseButton.Left => "LeftMouse",
                MouseButton.Right => "RightMouse",
                MouseButton.Middle => "MiddleMouse",
                MouseButton.XButton1 => "XButton1",
                MouseButton.XButton2 => "XButton2",
                _ => "LeftMouse"
            };

            AssignInput(mouseBtn);
        }

        private void AssignInput(string inputName)
        {
            if (_currentlyRebindingItem != null)
            {
                _currentlyRebindingItem.PhysicalInput = inputName;
                _currentlyRebindingItem.ActionText = "Rebind";
                _currentlyRebindingItem.ActionBackground = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                StatusInfoText.Text = $"Assigned [{_currentlyRebindingItem.VirtualButton}] → {inputName}. Don't forget to click 'Save Mappings'.";
            }

            CleanupListeners();
            _currentlyRebindingItem = null;
        }

        private void CancelRebinding()
        {
            if (_currentlyRebindingItem != null)
            {
                _currentlyRebindingItem.ActionText = "Rebind";
                _currentlyRebindingItem.ActionBackground = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                StatusInfoText.Text = "Rebinding cancelled.";
            }

            CleanupListeners();
            _currentlyRebindingItem = null;
        }

        private void CleanupListeners()
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown;
                window.PreviewMouseDown -= Window_PreviewMouseDown;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var profile = _profileManager.ActiveProfile;
            if (profile == null) return;

            foreach (var item in Items)
            {
                if (item.PhysicalInput != "Unassigned")
                {
                    profile.Buttons[item.VirtualButton] = item.PhysicalInput;
                }
                else
                {
                    profile.Buttons.Remove(item.VirtualButton);
                }
            }

            _profileManager.SaveProfile(profile);
            MessageBox.Show($"Mappings saved for profile '{profile.Name}'.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnResetDefault_Click(object sender, RoutedEventArgs e)
        {
            var defaultProfile = Profile.CreateDefaultProfile();
            var profile = _profileManager.ActiveProfile;
            if (profile != null)
            {
                profile.Buttons = new System.Collections.Generic.Dictionary<string, string>(defaultProfile.Buttons, StringComparer.OrdinalIgnoreCase);
                profile.ExtraMappings = new System.Collections.Generic.Dictionary<string, string>(defaultProfile.ExtraMappings, StringComparer.OrdinalIgnoreCase);
                _profileManager.SaveProfile(profile);
                LoadMappingsFromProfile();
                MessageBox.Show("Reset to default mappings.", "Reset Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
