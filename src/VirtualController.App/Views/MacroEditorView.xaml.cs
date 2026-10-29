using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VirtualController.App.Services;
using VirtualController.Core;
using VirtualController.Input;
using VirtualController.Profiles;
using InputManager = VirtualController.Input.InputManager;

namespace VirtualController.App.Views
{
    public partial class MacroEditorView : UserControl
    {
        private readonly ProfileManager _profileManager;
        private readonly InputManager? _inputManager;
        private readonly MacroRecorder _recorder = new();

        private MacroItemViewModel? _selectedMacro;
        private bool _isUpdatingInspector;
        private bool _isRebindingTrigger;
        private bool _isRecording;
        private readonly Dictionary<string, bool> _lastStateButtons = new(StringComparer.OrdinalIgnoreCase);

        public ObservableCollection<MacroItemViewModel> Macros { get; } = new();
        public ObservableCollection<MacroStepViewModel> SelectedSteps { get; } = new();

        public MacroEditorView(ProfileManager profileManager, InputManager? inputManager = null)
        {
            InitializeComponent();

            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _inputManager = inputManager;

            MacroListBox.ItemsSource = Macros;
            StepsItemsControl.ItemsSource = SelectedSteps;

            _profileManager.ActiveProfileChanged += (s, p) => Dispatcher.Invoke(LoadMacrosFromProfile);
            Loaded += (s, e) => LoadMacrosFromProfile();
            Unloaded += (s, e) =>
            {
                CancelRebinding();
                StopRecording();
            };
        }

        public void LoadMacrosFromProfile()
        {
            CancelRebinding();
            StopRecording();

            Macros.Clear();
            SelectedSteps.Clear();
            _selectedMacro = null;

            var profile = _profileManager.ActiveProfile;
            if (profile != null)
            {
                if (profile.Macros == null)
                {
                    profile.Macros = new List<MacroDefinition>();
                }

                foreach (var macro in profile.Macros)
                {
                    Macros.Add(new MacroItemViewModel(macro));
                }
            }

            if (Macros.Count > 0)
            {
                MacroListBox.SelectedIndex = 0;
            }
            else
            {
                UpdateInspector();
            }
        }

        private void MacroListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CancelRebinding();
            StopRecording();

            _selectedMacro = MacroListBox.SelectedItem as MacroItemViewModel;
            UpdateInspector();
        }

        private void UpdateInspector()
        {
            _isUpdatingInspector = true;
            try
            {
                if (_selectedMacro != null)
                {
                    MacroInspectorGrid.IsEnabled = true;
                    TxtMacroName.Text = _selectedMacro.Name;
                    TxtTriggerDisplay.Text = _selectedMacro.TriggerInput;
                    CmbMacroMode.SelectedIndex = (int)_selectedMacro.Mode;
                    ChkMacroEnabled.IsChecked = _selectedMacro.IsEnabled;
                    TxtLoopDelay.Text = _selectedMacro.LoopDelayMs.ToString();

                    SelectedSteps.Clear();
                    if (_selectedMacro.Definition.Steps != null)
                    {
                        for (int i = 0; i < _selectedMacro.Definition.Steps.Count; i++)
                        {
                            SelectedSteps.Add(new MacroStepViewModel(i + 1, _selectedMacro.Definition.Steps[i]));
                        }
                    }

                    ClearButtonSelectors();
                    CmbStepType.SelectedIndex = 0;
                }
                else
                {
                    MacroInspectorGrid.IsEnabled = false;
                    TxtMacroName.Text = "";
                    TxtTriggerDisplay.Text = "Unassigned";
                    CmbMacroMode.SelectedIndex = 0;
                    ChkMacroEnabled.IsChecked = false;
                    TxtLoopDelay.Text = "20";
                    SelectedSteps.Clear();
                    ClearButtonSelectors();
                }
            }
            finally
            {
                _isUpdatingInspector = false;
            }
        }

        private void ClearButtonSelectors()
        {
            foreach (var child in ButtonSelectorPanel.Children)
            {
                if (child is ToggleButton tb)
                {
                    tb.IsChecked = false;
                }
            }
        }

        #region Macro Properties Handlers

        private void TxtMacroName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInspector || _selectedMacro == null) return;
            _selectedMacro.Name = TxtMacroName.Text;
        }

        private void CmbMacroMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingInspector || _selectedMacro == null) return;
            if (CmbMacroMode.SelectedIndex >= 0)
            {
                _selectedMacro.Mode = (MacroMode)CmbMacroMode.SelectedIndex;
            }
        }

        private void ChkMacroEnabled_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingInspector || _selectedMacro == null) return;
            _selectedMacro.IsEnabled = ChkMacroEnabled.IsChecked == true;
        }

        private void TxtLoopDelay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInspector || _selectedMacro == null) return;
            if (int.TryParse(TxtLoopDelay.Text, out int delay))
            {
                _selectedMacro.LoopDelayMs = Math.Max(0, delay);
            }
        }

        private void BtnRebindTrigger_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;

            if (_isRebindingTrigger)
            {
                CancelRebinding();
                return;
            }

            _isRebindingTrigger = true;
            BtnRebindTrigger.Content = "Listening...";
            BtnRebindTrigger.Background = (Brush)FindResource("AccentRedBrush");
            StatusInfoText.Text = $"Press any Keyboard key or Mouse button for [{_selectedMacro.Name}] trigger... (ESC to cancel)";

            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown += Window_PreviewKeyDown_Rebind;
                window.PreviewMouseDown += Window_PreviewMouseDown_Rebind;
            }
        }

        private void Window_PreviewKeyDown_Rebind(object sender, KeyEventArgs e)
        {
            if (!_isRebindingTrigger) return;
            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            if (key == Key.Escape)
            {
                CancelRebinding();
                return;
            }

            string keyName = key.ToString();
            if (key == Key.Space) keyName = "Space";
            if (key == Key.Return) keyName = "Enter";
            if (key == Key.LeftShift || key == Key.RightShift) keyName = key.ToString();

            AssignTrigger(keyName);
        }

        private void Window_PreviewMouseDown_Rebind(object sender, MouseButtonEventArgs e)
        {
            if (!_isRebindingTrigger) return;
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

            AssignTrigger(mouseBtn);
        }

        private void AssignTrigger(string inputName)
        {
            if (_selectedMacro != null)
            {
                _selectedMacro.TriggerInput = inputName;
                TxtTriggerDisplay.Text = inputName;
                StatusInfoText.Text = $"Trigger assigned to [{inputName}]. Don't forget to click 'Save Macros'.";
            }

            CleanupRebindListeners();
            _isRebindingTrigger = false;
            BtnRebindTrigger.Content = LocalizationService.GetString("Macro_BtnRebind", "Bind Key");
            BtnRebindTrigger.Background = (Brush)FindResource("AccentBlueBrush");
        }

        private void CancelRebinding()
        {
            if (!_isRebindingTrigger) return;

            CleanupRebindListeners();
            _isRebindingTrigger = false;
            BtnRebindTrigger.Content = LocalizationService.GetString("Macro_BtnRebind", "Bind Key");
            BtnRebindTrigger.Background = (Brush)FindResource("AccentBlueBrush");
            StatusInfoText.Text = "Trigger rebinding cancelled.";
        }

        private void CleanupRebindListeners()
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown_Rebind;
                window.PreviewMouseDown -= Window_PreviewMouseDown_Rebind;
            }
        }

        #endregion

        #region Step Builder & Live Recording

        private void CmbStepType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbStepType == null || TxtStepDuration == null || ButtonSelectorPanel == null) return;

            var stepType = (MacroStepType)Math.Max(0, CmbStepType.SelectedIndex);
            bool isWait = (stepType == MacroStepType.Wait);
            bool isTap = (stepType == MacroStepType.Tap);

            TxtStepDuration.IsEnabled = isTap;
            ButtonSelectorPanel.IsEnabled = !isWait;
            if (isWait)
            {
                ClearButtonSelectors();
            }
        }

        private void BtnAddStep_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;

            var selectedButtons = new List<string>();
            foreach (var child in ButtonSelectorPanel.Children)
            {
                if (child is ToggleButton tb && tb.IsChecked == true)
                {
                    string btnName = tb.Content?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(btnName))
                    {
                        selectedButtons.Add(btnName);
                    }
                }
            }

            var stepType = (MacroStepType)Math.Max(0, CmbStepType.SelectedIndex);
            if (stepType != MacroStepType.Wait && selectedButtons.Count == 0)
            {
                StatusInfoText.Text = "Please select at least one controller button for this step, or choose 'Wait'.";
                return;
            }

            int duration = 50;
            if (int.TryParse(TxtStepDuration.Text, out int dur))
            {
                duration = Math.Clamp(dur, 0, 60000);
            }

            int delay = 50;
            if (int.TryParse(TxtStepDelay.Text, out int del))
            {
                delay = Math.Clamp(del, 0, 60000);
            }

            var newStep = new MacroStep
            {
                StepType = stepType,
                Buttons = selectedButtons,
                DurationMs = (stepType == MacroStepType.Tap) ? duration : 0,
                DelayAfterMs = delay
            };

            _selectedMacro.Definition.Steps.Add(newStep);
            SelectedSteps.Add(new MacroStepViewModel(SelectedSteps.Count + 1, newStep));
            _selectedMacro.RefreshSummary();

            StatusInfoText.Text = $"Added step #{SelectedSteps.Count}: {newStep.GetDisplaySummary()}";
        }

        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null)
            {
                StatusInfoText.Text = "Please select or create a macro first before recording.";
                return;
            }

            if (_isRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        private void StartRecording()
        {
            _isRecording = true;
            _lastStateButtons.Clear();
            _recorder.Start();

            BtnRecord.Content = LocalizationService.GetString("Macro_BtnStopRecord", "⏹ Stop Recording");
            BtnRecord.Background = (Brush)FindResource("AccentRedBrush");
            StatusInfoText.Text = "🔴 Recording in progress... Press controller buttons or mapped keys! Click 'Stop Recording' when finished.";

            if (_inputManager != null)
            {
                _inputManager.StateUpdated += InputManager_StateUpdated;
            }

            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown += Window_PreviewKeyDown_Record;
                window.PreviewKeyUp += Window_PreviewKeyUp_Record;
            }
        }

        private void StopRecording()
        {
            if (!_isRecording) return;

            _isRecording = false;
            BtnRecord.Content = LocalizationService.GetString("Macro_BtnRecord", "⏺ Live Record");
            BtnRecord.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));

            if (_inputManager != null)
            {
                _inputManager.StateUpdated -= InputManager_StateUpdated;
            }

            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown_Record;
                window.PreviewKeyUp -= Window_PreviewKeyUp_Record;
            }

            var recordedSteps = _recorder.Stop();
            _lastStateButtons.Clear();

            if (_selectedMacro != null && recordedSteps.Count > 0)
            {
                foreach (var s in recordedSteps)
                {
                    _selectedMacro.Definition.Steps.Add(s);
                    SelectedSteps.Add(new MacroStepViewModel(SelectedSteps.Count + 1, s));
                }
                _selectedMacro.RefreshSummary();
                StatusInfoText.Text = $"Recorded {recordedSteps.Count} steps into [{_selectedMacro.Name}].";
            }
            else
            {
                StatusInfoText.Text = "Recording stopped. No inputs detected.";
            }
        }

        private void InputManager_StateUpdated(object? sender, ControllerState state)
        {
            if (!_isRecording) return;

            CheckButtonState("A", state.A);
            CheckButtonState("B", state.B);
            CheckButtonState("X", state.X);
            CheckButtonState("Y", state.Y);
            CheckButtonState("LB", state.LeftShoulder);
            CheckButtonState("RB", state.RightShoulder);
            CheckButtonState("LT", state.LeftTrigger >= 0.5);
            CheckButtonState("RT", state.RightTrigger >= 0.5);
            CheckButtonState("LS", state.LeftThumb);
            CheckButtonState("RS", state.RightThumb);
            CheckButtonState("Back", state.Back);
            CheckButtonState("Start", state.Start);
            CheckButtonState("DPadUp", state.DPadUp);
            CheckButtonState("DPadDown", state.DPadDown);
            CheckButtonState("DPadLeft", state.DPadLeft);
            CheckButtonState("DPadRight", state.DPadRight);
            CheckButtonState("LS_Up", state.LeftThumbY >= 0.5);
            CheckButtonState("LS_Down", state.LeftThumbY <= -0.5);
            CheckButtonState("LS_Left", state.LeftThumbX <= -0.5);
            CheckButtonState("LS_Right", state.LeftThumbX >= 0.5);
        }

        private void CheckButtonState(string btn, bool isDown)
        {
            _lastStateButtons.TryGetValue(btn, out bool wasDown);
            if (isDown && !wasDown)
            {
                _recorder.RecordButtonDown(btn);
            }
            else if (!isDown && wasDown)
            {
                _recorder.RecordButtonUp(btn);
            }
            _lastStateButtons[btn] = isDown;
        }

        private void Window_PreviewKeyDown_Record(object sender, KeyEventArgs e)
        {
            if (!_isRecording || e.IsRepeat) return;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            string vBtn = ResolveKeyToVirtualButton(key);
            if (!string.IsNullOrEmpty(vBtn))
            {
                _recorder.RecordButtonDown(vBtn);
            }
        }

        private void Window_PreviewKeyUp_Record(object sender, KeyEventArgs e)
        {
            if (!_isRecording) return;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            string vBtn = ResolveKeyToVirtualButton(key);
            if (!string.IsNullOrEmpty(vBtn))
            {
                _recorder.RecordButtonUp(vBtn);
            }
        }

        private string ResolveKeyToVirtualButton(Key key)
        {
            string keyStr = key.ToString();
            if (key == Key.Space) keyStr = "Space";
            if (key == Key.Return) keyStr = "Enter";
            if (key == Key.LeftShift || key == Key.RightShift) keyStr = key.ToString();

            var profile = _profileManager.ActiveProfile;
            if (profile != null)
            {
                foreach (var kvp in profile.Buttons)
                {
                    if (string.Equals(kvp.Value, keyStr, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Key;
                    }
                }
                foreach (var kvp in profile.ExtraMappings)
                {
                    if (string.Equals(kvp.Key, keyStr, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }

            string[] knownButtons =
            {
                "A", "B", "X", "Y", "LB", "RB", "LT", "RT", "LS", "RS", "Back", "Start",
                "DPadUp", "DPadDown", "DPadLeft", "DPadRight", "LS_Up", "LS_Down", "LS_Left", "LS_Right"
            };

            foreach (var b in knownButtons)
            {
                if (string.Equals(b, keyStr, StringComparison.OrdinalIgnoreCase))
                {
                    return b;
                }
            }

            return "";
        }

        private void BtnClearSteps_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;

            _selectedMacro.Definition.Steps.Clear();
            SelectedSteps.Clear();
            _selectedMacro.RefreshSummary();
            StatusInfoText.Text = $"Cleared all steps from [{_selectedMacro.Name}].";
        }

        #endregion

        #region Timeline Step Actions (Move Up, Move Down, Delete)

        private void BtnStepMoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;
            if (sender is FrameworkElement elem && elem.Tag is MacroStepViewModel stepVm)
            {
                int index = SelectedSteps.IndexOf(stepVm);
                if (index > 0)
                {
                    SelectedSteps.Move(index, index - 1);
                    var stepDef = _selectedMacro.Definition.Steps[index];
                    _selectedMacro.Definition.Steps.RemoveAt(index);
                    _selectedMacro.Definition.Steps.Insert(index - 1, stepDef);
                    ReindexSteps();
                    _selectedMacro.RefreshSummary();
                }
            }
        }

        private void BtnStepMoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;
            if (sender is FrameworkElement elem && elem.Tag is MacroStepViewModel stepVm)
            {
                int index = SelectedSteps.IndexOf(stepVm);
                if (index >= 0 && index < SelectedSteps.Count - 1)
                {
                    SelectedSteps.Move(index, index + 1);
                    var stepDef = _selectedMacro.Definition.Steps[index];
                    _selectedMacro.Definition.Steps.RemoveAt(index);
                    _selectedMacro.Definition.Steps.Insert(index + 1, stepDef);
                    ReindexSteps();
                    _selectedMacro.RefreshSummary();
                }
            }
        }

        private void BtnStepDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;
            if (sender is FrameworkElement elem && elem.Tag is MacroStepViewModel stepVm)
            {
                int index = SelectedSteps.IndexOf(stepVm);
                if (index >= 0)
                {
                    SelectedSteps.RemoveAt(index);
                    _selectedMacro.Definition.Steps.RemoveAt(index);
                    ReindexSteps();
                    _selectedMacro.RefreshSummary();
                    StatusInfoText.Text = $"Deleted step #{index + 1}.";
                }
            }
        }

        private void ReindexSteps()
        {
            for (int i = 0; i < SelectedSteps.Count; i++)
            {
                SelectedSteps[i].Index = i + 1;
            }
        }

        #endregion

        #region Toolbar & List Management

        private void BtnNewMacro_Click(object sender, RoutedEventArgs e)
        {
            var newMacro = new MacroDefinition
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = $"Macro {Macros.Count + 1}",
                TriggerInput = "Unassigned",
                Mode = MacroMode.PlayOnce,
                IsEnabled = true,
                LoopDelayMs = 20,
                Steps = new List<MacroStep>()
            };

            var vm = new MacroItemViewModel(newMacro);
            Macros.Add(vm);
            MacroListBox.SelectedItem = vm;
            MacroListBox.ScrollIntoView(vm);
            StatusInfoText.Text = $"Created new macro '{newMacro.Name}'.";
        }

        private void BtnAddPreset_Click(object sender, RoutedEventArgs e)
        {
            var presets = MacroPresets.GetAllPresets();
            var menu = new ContextMenu();

            foreach (var preset in presets)
            {
                var mi = new MenuItem
                {
                    Header = preset.Name,
                    Tag = preset
                };

                mi.Click += (s, ev) =>
                {
                    if (s is MenuItem clickedItem && clickedItem.Tag is MacroDefinition def)
                    {
                        var cloned = def.Clone();
                        cloned.Id = Guid.NewGuid().ToString("N");
                        cloned.Name = def.Name; // Retain original preset name

                        var vm = new MacroItemViewModel(cloned);
                        Macros.Add(vm);
                        MacroListBox.SelectedItem = vm;
                        MacroListBox.ScrollIntoView(vm);
                        StatusInfoText.Text = $"Added preset '{cloned.Name}'.";
                    }
                };

                menu.Items.Add(mi);
            }

            menu.PlacementTarget = BtnAddPreset;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void BtnDuplicateMacro_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;

            var cloned = _selectedMacro.Definition.Clone();
            var vm = new MacroItemViewModel(cloned);
            Macros.Add(vm);
            MacroListBox.SelectedItem = vm;
            MacroListBox.ScrollIntoView(vm);
            StatusInfoText.Text = $"Duplicated macro as '{cloned.Name}'.";
        }

        private void BtnDeleteMacro_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMacro == null) return;

            string name = _selectedMacro.Name;
            int index = Macros.IndexOf(_selectedMacro);
            Macros.Remove(_selectedMacro);

            if (Macros.Count > 0)
            {
                MacroListBox.SelectedIndex = Math.Clamp(index, 0, Macros.Count - 1);
            }
            else
            {
                _selectedMacro = null;
                UpdateInspector();
            }

            StatusInfoText.Text = $"Deleted macro '{name}'.";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var profile = _profileManager.ActiveProfile;
            if (profile == null) return;

            profile.Macros.Clear();
            foreach (var vm in Macros)
            {
                profile.Macros.Add(vm.Definition);
            }

            _profileManager.SaveProfile(profile);

            if (_inputManager != null && _inputManager.BlockPhysicalKeyboard)
            {
                _inputManager.UpdateBlockedKeysFromActiveProfile();
            }

            string msg = LocalizationService.GetString("Macro_SavedSuccess", "Macro settings saved successfully.");
            StatusInfoText.Text = msg;
            MessageBox.Show(msg, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }
    }
}
