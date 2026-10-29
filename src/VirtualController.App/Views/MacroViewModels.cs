using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using VirtualController.Profiles;

namespace VirtualController.App.Views
{
    public class MacroItemViewModel : INotifyPropertyChanged
    {
        public MacroDefinition Definition { get; }

        public MacroItemViewModel(MacroDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public string Id => Definition.Id;

        public string Name
        {
            get => Definition.Name;
            set
            {
                Definition.Name = value;
                OnPropertyChanged();
            }
        }

        public string TriggerInput
        {
            get => string.IsNullOrWhiteSpace(Definition.TriggerInput) ? "Unassigned" : Definition.TriggerInput;
            set
            {
                Definition.TriggerInput = value;
                OnPropertyChanged();
            }
        }

        public MacroMode Mode
        {
            get => Definition.Mode;
            set
            {
                Definition.Mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModeBadgeText));
                OnPropertyChanged(nameof(ModeBadgeBrush));
            }
        }

        public bool IsEnabled
        {
            get => Definition.IsEnabled;
            set
            {
                Definition.IsEnabled = value;
                OnPropertyChanged();
            }
        }

        public int LoopDelayMs
        {
            get => Definition.LoopDelayMs;
            set
            {
                Definition.LoopDelayMs = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public string StepCountText => $"{Definition.Steps.Count} steps";

        public string ModeBadgeText => Mode switch
        {
            MacroMode.PlayOnce => "ONCE",
            MacroMode.RepeatWhileHeld => "TURBO",
            MacroMode.ToggleLoop => "TOGGLE",
            _ => "ONCE"
        };

        public Brush ModeBadgeBrush => Mode switch
        {
            MacroMode.PlayOnce => new SolidColorBrush(Color.FromRgb(37, 99, 235)),
            MacroMode.RepeatWhileHeld => new SolidColorBrush(Color.FromRgb(217, 119, 6)),
            MacroMode.ToggleLoop => new SolidColorBrush(Color.FromRgb(147, 51, 234)),
            _ => Brushes.Gray
        };

        public void RefreshSummary()
        {
            OnPropertyChanged(nameof(StepCountText));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(TriggerInput));
            OnPropertyChanged(nameof(ModeBadgeText));
            OnPropertyChanged(nameof(ModeBadgeBrush));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(LoopDelayMs));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MacroStepViewModel : INotifyPropertyChanged
    {
        private int _index;

        public MacroStep Step { get; }

        public MacroStepViewModel(int index, MacroStep step)
        {
            _index = index;
            Step = step ?? throw new ArgumentNullException(nameof(step));
        }

        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IndexText));
            }
        }

        public string IndexText => $"#{Index}";

        public MacroStepType StepType
        {
            get => Step.StepType;
            set
            {
                Step.StepType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StepTypeBadge));
                OnPropertyChanged(nameof(StepTypeBrush));
                OnPropertyChanged(nameof(ButtonsText));
                OnPropertyChanged(nameof(IsTapStep));
                OnPropertyChanged(nameof(SummaryText));
            }
        }

        public string StepTypeBadge => Step.StepType switch
        {
            MacroStepType.Tap => "TAP",
            MacroStepType.PressDown => "HOLD",
            MacroStepType.ReleaseUp => "RELEASE",
            MacroStepType.Wait => "WAIT",
            _ => "TAP"
        };

        public Brush StepTypeBrush => Step.StepType switch
        {
            MacroStepType.Tap => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            MacroStepType.PressDown => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            MacroStepType.ReleaseUp => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            MacroStepType.Wait => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
            _ => Brushes.Gray
        };

        public string ButtonsText => Step.StepType == MacroStepType.Wait
            ? "—"
            : (Step.Buttons != null && Step.Buttons.Count > 0 ? string.Join(" + ", Step.Buttons) : "None");

        public int DurationMs
        {
            get => Step.DurationMs;
            set
            {
                Step.DurationMs = Math.Clamp(value, 0, 60000);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SummaryText));
            }
        }

        public int DelayAfterMs
        {
            get => Step.DelayAfterMs;
            set
            {
                Step.DelayAfterMs = Math.Clamp(value, 0, 60000);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SummaryText));
            }
        }

        public bool IsTapStep => Step.StepType == MacroStepType.Tap;

        public string SummaryText => Step.GetDisplaySummary();

        public void RefreshSummary()
        {
            OnPropertyChanged(nameof(IndexText));
            OnPropertyChanged(nameof(StepTypeBadge));
            OnPropertyChanged(nameof(StepTypeBrush));
            OnPropertyChanged(nameof(ButtonsText));
            OnPropertyChanged(nameof(DurationMs));
            OnPropertyChanged(nameof(DelayAfterMs));
            OnPropertyChanged(nameof(IsTapStep));
            OnPropertyChanged(nameof(SummaryText));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
