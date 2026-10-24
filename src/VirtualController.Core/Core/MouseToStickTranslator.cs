using System;
using VirtualController.Profiles;

namespace VirtualController.Core
{
    /// <summary>
    /// Translates raw mouse displacement deltas into normalized gamepad analog-stick coordinates (-1.0 to +1.0).
    /// Implements rate-of-turn velocity modeling with anti-deadzone baseline jump,
    /// configurable response curves, independent X/Y sensitivity, radial and square geometry,
    /// frame-rate-independent smoothing, and configurable return-to-center dynamics.
    /// </summary>
    public class MouseToStickTranslator
    {
        private const double BaseReferenceDt = 0.008; // 8ms reference tick (125 Hz)
        private const double BaseScaleFactor = 0.05;  // Mickey count to normalized velocity calibration factor

        private double _currentX = 0.0;
        private double _currentY = 0.0;

        /// <summary>
        /// Current smoothed X output (-1.0 to 1.0).
        /// </summary>
        public double CurrentX => _currentX;

        /// <summary>
        /// Current smoothed Y output (-1.0 to 1.0).
        /// </summary>
        public double CurrentY => _currentY;

        /// <summary>
        /// Resets the translator state to neutral center (0, 0).
        /// </summary>
        public void Reset()
        {
            _currentX = 0.0;
            _currentY = 0.0;
        }

        /// <summary>
        /// Processes a relative mouse delta (dx, dy) over an input tick dt (seconds) and produces normalized stick coordinates.
        /// </summary>
        /// <param name="dx">Relative horizontal mouse displacement (mickeys).</param>
        /// <param name="dy">Relative vertical mouse displacement (mickeys, screen positive downwards).</param>
        /// <param name="config">Thumbstick configuration containing sensitivity, anti-deadzone, and curve parameters.</param>
        /// <param name="dt">Elapsed time in seconds since the last update tick (default: 0.008s = 8ms).</param>
        /// <returns>Normalized stick coordinates (X, Y) clamped to [-1.0, 1.0].</returns>
        public (double x, double y) Process(int dx, int dy, StickConfig config, double dt = BaseReferenceDt)
        {
            if (config == null)
            {
                Reset();
                return (0.0, 0.0);
            }

            // Ensure valid non-zero delta time
            if (dt <= 0.0001 || double.IsNaN(dt) || double.IsInfinity(dt))
            {
                dt = BaseReferenceDt;
            }

            // 1. Convert raw screen displacement to velocity-scaled delta
            // Screen Y is down-positive; Gamepad thumbstick Y is up-positive
            double rawX = dx;
            double rawY = -dy;

            // Inversion
            if (config.InvertX) rawX = -rawX;
            if (config.InvertY) rawY = -rawY;

            // Normalize velocity relative to standard 125 Hz reference frame
            double timeScale = BaseReferenceDt / dt;
            double velX = rawX * timeScale;
            double velY = rawY * timeScale;

            // 2. Apply independent X and Y sensitivities
            double sensX = Math.Max(0.0, config.SensitivityX) * Math.Max(0.0, config.Sensitivity);
            double sensY = Math.Max(0.0, config.SensitivityY) * Math.Max(0.0, config.Sensitivity);

            double scaledX = velX * sensX * BaseScaleFactor;
            double scaledY = velY * sensY * BaseScaleFactor;

            double inputMagnitude = Math.Sqrt(scaledX * scaledX + scaledY * scaledY);

            // 3. Minimum speed / noise-gate threshold check (InitialSpeed / Deadzone)
            double threshold = Math.Max(Math.Clamp(config.Deadzone, 0.0, 0.5), Math.Clamp(config.InitialSpeed, 0.0, 10.0) * 0.01);
            if (inputMagnitude <= threshold || inputMagnitude < 1e-6)
            {
                // Mouse is stationary or below threshold: apply decay or return to center
                double decay = Math.Clamp(config.DecayRate, 0.0, 1.0);
                if (decay <= 0.01 || string.Equals(config.ReturnToCenterMode, "Instant", StringComparison.OrdinalIgnoreCase))
                {
                    _currentX = 0.0;
                    _currentY = 0.0;
                }
                else
                {
                    double timeCorrectedDecay = Math.Pow(decay, dt / BaseReferenceDt);
                    _currentX *= timeCorrectedDecay;
                    _currentY *= timeCorrectedDecay;

                    // Eliminate residual floating-point creep
                    if (Math.Abs(_currentX) < 0.001) _currentX = 0.0;
                    if (Math.Abs(_currentY) < 0.001) _currentY = 0.0;
                }
                return (_currentX, _currentY);
            }

            // 4. Direction unit vector (protected against divide-by-zero)
            double dirX = scaledX / inputMagnitude;
            double dirY = scaledY / inputMagnitude;

            // 5. Acceleration curve
            double accelMultiplier = 1.0;
            double acceleration = Math.Clamp(config.Acceleration, 1.0, 4.0);
            if (acceleration > 1.0)
            {
                accelMultiplier = Math.Pow(inputMagnitude, acceleration - 1.0);
            }
            double acceleratedMag = inputMagnitude * accelMultiplier;

            // 6. Anti-Deadzone (InitialDeflection) and MaxDeflection scaling
            double antiDeadzone = Math.Clamp(config.AntiDeadzone, 0.0, 0.95);
            double maxDeflection = Math.Clamp(config.MaxDeflection, Math.Min(antiDeadzone + 0.01, 1.0), 1.0);

            // Normalize input magnitude to [0.0, 1.0] domain for curve evaluation
            double normalizedInput = Math.Clamp((acceleratedMag - threshold) / (1.0 - threshold), 0.0, 1.0);

            // 7. Apply Response Curve
            double curvedOutput = EvaluateCurve(normalizedInput, config.ResponseCurve);

            // 8. Scale output between AntiDeadzone and MaxDeflection
            double targetMagnitude = antiDeadzone + (maxDeflection - antiDeadzone) * curvedOutput;
            targetMagnitude = Math.Clamp(targetMagnitude, 0.0, maxDeflection);

            // 9. Diagonal Normalization (Radial vs Squared)
            double targetX;
            double targetY;

            if (config.IsRadial)
            {
                // Radial: magnitude clamped within circle of radius maxDeflection
                targetX = dirX * targetMagnitude;
                targetY = dirY * targetMagnitude;
            }
            else
            {
                // Square: circle-to-square projection allowing corners to reach (±maxDeflection, ±maxDeflection)
                double maxComponent = Math.Max(Math.Abs(dirX), Math.Abs(dirY));
                double squareScale = (maxComponent > 1e-6) ? (1.0 / maxComponent) : 1.0;

                targetX = Math.Clamp(dirX * targetMagnitude * squareScale, -maxDeflection, maxDeflection);
                targetY = Math.Clamp(dirY * targetMagnitude * squareScale, -maxDeflection, maxDeflection);
            }

            // 10. Low-pass Smoothing (Time-dependent Exponential Moving Average)
            double smoothing = Math.Clamp(config.Smoothing, 0.0, 0.95);
            if (smoothing > 0.0)
            {
                double timeCorrectedSmoothing = Math.Pow(smoothing, dt / BaseReferenceDt);
                _currentX = (_currentX * timeCorrectedSmoothing) + (targetX * (1.0 - timeCorrectedSmoothing));
                _currentY = (_currentY * timeCorrectedSmoothing) + (targetY * (1.0 - timeCorrectedSmoothing));
            }
            else
            {
                _currentX = targetX;
                _currentY = targetY;
            }

            // Final safety clamp
            _currentX = Math.Clamp(_currentX, -1.0, 1.0);
            _currentY = Math.Clamp(_currentY, -1.0, 1.0);

            return (_currentX, _currentY);
        }

        /// <summary>
        /// Evaluates a normalized input value [0.0, 1.0] against the specified response curve.
        /// </summary>
        public static double EvaluateCurve(double t, string? curveName)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            if (string.IsNullOrWhiteSpace(curveName)) return t;

            switch (curveName.Trim().ToLowerInvariant())
            {
                case "delay":
                case "exponential":
                    // Quadratic ease-in: Lower micro-sensitivity, steep ramp at high speed
                    return t * t;

                case "aggressive":
                    // Square-root ease-out: High initial responsiveness, steep initial slope
                    return Math.Sqrt(t);

                case "instant":
                    // Instant step: Immediate jump to near-maximum turn speed
                    return t > 0.001 ? (0.80 + 0.20 * t) : 0.0;

                case "smooth":
                    // Cubic Hermite S-Curve (smoothstep): Ease-in and ease-out
                    return t * t * (3.0 - 2.0 * t);

                case "linear":
                default:
                    // Standard linear 1:1 mapping
                    return t;
            }
        }
    }
}
