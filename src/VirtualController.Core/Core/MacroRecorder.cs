using System;
using System.Collections.Generic;
using System.Diagnostics;
using VirtualController.Profiles;

namespace VirtualController.Core
{
    public class MacroRecorder
    {
        private readonly object _lock = new();
        private readonly Stopwatch _stopwatch = new();
        private readonly Dictionary<string, long> _heldButtons = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<MacroStep> _recordedSteps = new();
        private MacroStep? _lastStep;
        private long? _lastReleaseTimestampMs;
        private bool _isRecording;

        public bool IsRecording
        {
            get
            {
                lock (_lock)
                {
                    return _isRecording;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _isRecording = value;
                }
            }
        }

        public bool UseFixedTiming { get; set; } = false;
        public int FixedDurationMs { get; set; } = 50;
        public int FixedDelayAfterMs { get; set; } = 50;
        public int MinDurationMs { get; set; } = 15;
        public int MaxDelayMs { get; set; } = 5000;

        public void Start(long? explicitTimestampMs = null)
        {
            lock (_lock)
            {
                _recordedSteps.Clear();
                _heldButtons.Clear();
                _lastStep = null;
                _lastReleaseTimestampMs = null;
                _stopwatch.Restart();
                _isRecording = true;
            }
        }

        public void RecordButtonDown(string virtualButton, long? explicitTimestampMs = null)
        {
            lock (_lock)
            {
                if (!_isRecording || string.IsNullOrWhiteSpace(virtualButton))
                    return;

                string btn = virtualButton.Trim();
                if (_heldButtons.ContainsKey(btn))
                    return;

                long downTime = GetCurrentTimeMs(explicitTimestampMs);
                _heldButtons[btn] = downTime;
            }
        }

        public void RecordButtonUp(string virtualButton, long? explicitTimestampMs = null)
        {
            lock (_lock)
            {
                if (!_isRecording || string.IsNullOrWhiteSpace(virtualButton))
                    return;

                string btn = virtualButton.Trim();
                if (!_heldButtons.Remove(btn, out long downTime))
                    return;

                long upTime = GetCurrentTimeMs(explicitTimestampMs);
                FinalizeStep(btn, downTime, upTime);
            }
        }

        public List<MacroStep> Stop(long? explicitTimestampMs = null)
        {
            lock (_lock)
            {
                if (_isRecording)
                {
                    long stopTime = GetCurrentTimeMs(explicitTimestampMs);
                    if (_heldButtons.Count > 0)
                    {
                        var heldList = new List<KeyValuePair<string, long>>(_heldButtons);
                        heldList.Sort((a, b) => a.Value.CompareTo(b.Value));

                        foreach (var kvp in heldList)
                        {
                            FinalizeStep(kvp.Key, kvp.Value, stopTime);
                        }

                        _heldButtons.Clear();
                    }

                    _stopwatch.Stop();
                    _isRecording = false;
                }

                var copy = new List<MacroStep>(_recordedSteps.Count);
                foreach (var step in _recordedSteps)
                {
                    copy.Add(step.Clone());
                }
                return copy;
            }
        }

        public void Cancel()
        {
            lock (_lock)
            {
                _isRecording = false;
                _stopwatch.Stop();
                _stopwatch.Reset();
                _heldButtons.Clear();
                _recordedSteps.Clear();
                _lastStep = null;
                _lastReleaseTimestampMs = null;
            }
        }

        private void FinalizeStep(string virtualButton, long downTime, long upTime)
        {
            int duration;
            if (UseFixedTiming)
            {
                duration = FixedDurationMs;
            }
            else
            {
                long rawDuration = Math.Max(0, upTime - downTime);
                duration = Clamp(rawDuration, MinDurationMs, MaxDelayMs);
            }

            if (_lastStep != null && _lastReleaseTimestampMs.HasValue)
            {
                if (UseFixedTiming)
                {
                    _lastStep.DelayAfterMs = FixedDelayAfterMs;
                }
                else
                {
                    long rawGap = downTime - _lastReleaseTimestampMs.Value;
                    _lastStep.DelayAfterMs = Clamp(rawGap, 0, MaxDelayMs);
                }
            }

            var step = new MacroStep
            {
                StepType = MacroStepType.Tap,
                Buttons = new List<string> { virtualButton },
                DurationMs = duration,
                DelayAfterMs = FixedDelayAfterMs
            };

            _recordedSteps.Add(step);
            _lastStep = step;
            _lastReleaseTimestampMs = upTime;
        }

        private long GetCurrentTimeMs(long? explicitTimestampMs)
        {
            return explicitTimestampMs ?? _stopwatch.ElapsedMilliseconds;
        }

        private static int Clamp(long value, int min, int max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }
            if (value < min) return min;
            if (value > max) return max;
            return (int)value;
        }
    }
}
