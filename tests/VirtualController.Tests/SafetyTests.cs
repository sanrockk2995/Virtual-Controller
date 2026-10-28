using System;
using VirtualController.Core;
using VirtualController.Backend;
using Xunit;

namespace VirtualController.Tests
{
    public class SafetyTests
    {
        [Fact]
        public void ControllerState_Reset_NeutralizesAllFields()
        {
            var state = new ControllerState
            {
                A = true,
                B = true,
                X = true,
                Y = true,
                LeftShoulder = true,
                RightShoulder = true,
                Back = true,
                Start = true,
                Guide = true,
                LeftThumb = true,
                RightThumb = true,
                DPadUp = true,
                DPadDown = true,
                DPadLeft = true,
                DPadRight = true,
                LeftTrigger = 0.85,
                RightTrigger = 0.95,
                LeftThumbX = -0.707,
                LeftThumbY = 0.707,
                RightThumbX = 0.5,
                RightThumbY = -0.5
            };

            state.Reset();

            Assert.False(state.A);
            Assert.False(state.B);
            Assert.False(state.X);
            Assert.False(state.Y);
            Assert.False(state.LeftShoulder);
            Assert.False(state.RightShoulder);
            Assert.False(state.Back);
            Assert.False(state.Start);
            Assert.False(state.Guide);
            Assert.False(state.LeftThumb);
            Assert.False(state.RightThumb);
            Assert.False(state.DPadUp);
            Assert.False(state.DPadDown);
            Assert.False(state.DPadLeft);
            Assert.False(state.DPadRight);

            Assert.Equal(0.0, state.LeftTrigger);
            Assert.Equal(0.0, state.RightTrigger);
            Assert.Equal(0.0, state.LeftThumbX);
            Assert.Equal(0.0, state.LeftThumbY);
            Assert.Equal(0.0, state.RightThumbX);
            Assert.Equal(0.0, state.RightThumbY);
        }

        [Fact]
        public void MockController_Disconnect_ResetsState()
        {
            var mock = new MockVirtualController();
            mock.Connect();

            var activeState = new ControllerState
            {
                A = true,
                LeftTrigger = 1.0,
                LeftThumbX = 1.0
            };

            mock.UpdateState(activeState);
            Assert.True(mock.LastState.A);
            Assert.Equal(1.0, mock.LastState.LeftTrigger);

            mock.Disconnect();
            Assert.False(mock.IsConnected);
            Assert.False(mock.LastState.A);
            Assert.Equal(0.0, mock.LastState.LeftTrigger);
            Assert.Equal(0.0, mock.LastState.LeftThumbX);
        }

        [Theory]
        [InlineData("Ctrl + Alt + F10", true, true, false, 0x79, "F10")]
        [InlineData("Ctrl + Shift + F12", true, false, true, 0x7B, "F12")]
        [InlineData("F11", false, false, false, 0x7A, "F11")]
        [InlineData("Alt + Q", false, true, false, 0x51, "Q")]
        public void HotkeyDefinition_Parse_ValidStrings_ParsedCorrectly(
            string text, bool expectedCtrl, bool expectedAlt, bool expectedShift, int expectedVk, string expectedName)
        {
            var hotkey = VirtualController.Input.HotkeyDefinition.Parse(text);

            Assert.Equal(expectedCtrl, hotkey.RequireCtrl);
            Assert.Equal(expectedAlt, hotkey.RequireAlt);
            Assert.Equal(expectedShift, hotkey.RequireShift);
            Assert.Equal(expectedVk, hotkey.KeyVk);
            Assert.Equal(expectedName, hotkey.KeyName);
            Assert.Equal(text, hotkey.ToString());
        }

        [Fact]
        public void HotkeyDefinition_Matches_EvaluatesCorrectModifiers()
        {
            var hotkey = new VirtualController.Input.HotkeyDefinition(true, true, false, 0x79, "F10");

            // Matches when Ctrl and Alt are down and key is F10
            Assert.True(hotkey.Matches(0x79, vk => vk == 0x11 || vk == 0x12));

            // Fails if key is different
            Assert.False(hotkey.Matches(0x78, vk => vk == 0x11 || vk == 0x12));

            // Fails if Ctrl is missing
            Assert.False(hotkey.Matches(0x79, vk => vk == 0x12));

            // Fails if Alt is missing
            Assert.False(hotkey.Matches(0x79, vk => vk == 0x11));

            // Fails if unexpected Shift is down
            Assert.False(hotkey.Matches(0x79, vk => vk == 0x11 || vk == 0x12 || vk == 0x10));
        }

        [Fact]
        public void InputManager_SetEmergencyHotkey_UpdatesHookManagerDefinition()
        {
            var mock = new MockVirtualController();
            var pm = new VirtualController.Profiles.ProfileManager();
            using var inputManager = new VirtualController.Input.InputManager(mock, pm);

            inputManager.SetEmergencyHotkey("Ctrl + Shift + F12");

            Assert.NotNull(inputManager.EmergencyHotkey);
            Assert.True(inputManager.EmergencyHotkey.RequireCtrl);
            Assert.False(inputManager.EmergencyHotkey.RequireAlt);
            Assert.True(inputManager.EmergencyHotkey.RequireShift);
            Assert.Equal(0x7B, inputManager.EmergencyHotkey.KeyVk);
            Assert.Equal("Ctrl + Shift + F12", inputManager.EmergencyHotkey.ToString());
        }
    }
}
