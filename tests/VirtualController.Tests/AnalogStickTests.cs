using System;
using VirtualController.Core;
using Xunit;

namespace VirtualController.Tests
{
    public class AnalogStickTests
    {
        private const double Tolerance = 0.001;

        [Fact]
        public void KeyW_ShouldProduce_UpVector()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: true, down: false, left: false, right: false);
            Assert.Equal(0.0, x, Tolerance);
            Assert.Equal(1.0, y, Tolerance);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6);
        }

        [Fact]
        public void KeyS_ShouldProduce_DownVector()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: false, down: true, left: false, right: false);
            Assert.Equal(0.0, x, Tolerance);
            Assert.Equal(-1.0, y, Tolerance);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6);
        }

        [Fact]
        public void KeyA_ShouldProduce_LeftVector()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: false, down: false, left: true, right: false);
            Assert.Equal(-1.0, x, Tolerance);
            Assert.Equal(0.0, y, Tolerance);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6);
        }

        [Fact]
        public void KeyD_ShouldProduce_RightVector()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: false, down: false, left: false, right: true);
            Assert.Equal(1.0, x, Tolerance);
            Assert.Equal(0.0, y, Tolerance);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6);
        }

        [Fact]
        public void Diagonal_WD_ShouldBeNormalized_AndMagnitudeNeverExceedOne()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: true, down: false, left: false, right: true);

            double expected = 1.0 / Math.Sqrt(2.0); // ~0.707106
            Assert.Equal(expected, x, 3);
            Assert.Equal(expected, y, 3);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6, $"Diagonal magnitude {mag} exceeded 1.0!");
            Assert.Equal(1.0, mag, 3);
        }

        [Fact]
        public void Diagonal_WA_ShouldBeNormalized_AndMagnitudeNeverExceedOne()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: true, down: false, left: true, right: false);

            double expected = 1.0 / Math.Sqrt(2.0);
            Assert.Equal(-expected, x, 3);
            Assert.Equal(expected, y, 3);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6, $"Diagonal magnitude {mag} exceeded 1.0!");
            Assert.Equal(1.0, mag, 3);
        }

        [Fact]
        public void Diagonal_SD_ShouldBeNormalized_AndMagnitudeNeverExceedOne()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: false, down: true, left: false, right: true);

            double expected = 1.0 / Math.Sqrt(2.0);
            Assert.Equal(expected, x, 3);
            Assert.Equal(-expected, y, 3);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6, $"Diagonal magnitude {mag} exceeded 1.0!");
            Assert.Equal(1.0, mag, 3);
        }

        [Fact]
        public void Diagonal_SA_ShouldBeNormalized_AndMagnitudeNeverExceedOne()
        {
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: false, down: true, left: true, right: false);

            double expected = 1.0 / Math.Sqrt(2.0);
            Assert.Equal(-expected, x, 3);
            Assert.Equal(-expected, y, 3);

            double mag = Math.Sqrt(x * x + y * y);
            Assert.True(mag <= 1.0 + 1e-6, $"Diagonal magnitude {mag} exceeded 1.0!");
            Assert.Equal(1.0, mag, 3);
        }

        [Fact]
        public void Opposites_CancelOutToZero()
        {
            // W + S
            var (x1, y1) = MappingEngine.CalculateKeyboardStick(up: true, down: true, left: false, right: false);
            Assert.Equal(0.0, x1);
            Assert.Equal(0.0, y1);

            // A + D
            var (x2, y2) = MappingEngine.CalculateKeyboardStick(up: false, down: false, left: true, right: true);
            Assert.Equal(0.0, x2);
            Assert.Equal(0.0, y2);

            // All 4 keys
            var (x3, y3) = MappingEngine.CalculateKeyboardStick(up: true, down: true, left: true, right: true);
            Assert.Equal(0.0, x3);
            Assert.Equal(0.0, y3);
        }

        [Fact]
        public void Deadzone_FiltersSmallInputs()
        {
            // When deadzone is 0.2 and magnitude is 1.0, scaled magnitude is (1.0 - 0.2) / 0.8 = 1.0
            var (x, y) = MappingEngine.CalculateKeyboardStick(up: true, down: false, left: false, right: false, deadzone: 0.2);
            Assert.Equal(0.0, x, Tolerance);
            Assert.Equal(1.0, y, Tolerance);

            // If deadzone > 1.0, it should be 0
            var (zeroX, zeroY) = MappingEngine.CalculateKeyboardStick(up: true, down: false, left: false, right: false, deadzone: 1.1);
            Assert.Equal(0.0, zeroX);
            Assert.Equal(0.0, zeroY);
        }

        [Fact]
        public void Sensitivity_ScalesOutputAndClampsToOne()
        {
            // Half sensitivity
            var (hx, hy) = MappingEngine.CalculateKeyboardStick(up: true, down: false, left: false, right: false, deadzone: 0.0, sensitivity: 0.5);
            Assert.Equal(0.0, hx, Tolerance);
            Assert.Equal(0.5, hy, Tolerance);

            // Double sensitivity - must clamp to 1.0
            var (cx, cy) = MappingEngine.CalculateKeyboardStick(up: true, down: false, left: false, right: false, deadzone: 0.0, sensitivity: 2.0);
            Assert.Equal(0.0, cx, Tolerance);
            Assert.Equal(1.0, cy, Tolerance);
        }
    }
}
