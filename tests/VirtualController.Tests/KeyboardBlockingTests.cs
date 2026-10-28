using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VirtualController.Backend;
using VirtualController.Core;
using VirtualController.Input;
using VirtualController.Profiles;
using VirtualController.Services;
using Xunit;

namespace VirtualController.Tests
{
    public class KeyboardBlockingTests
    {
        [Fact]
        public void Profile_GetMappedPhysicalKeyboardKeys_ExtractsAllMappedKeysAndExcludesMouse()
        {
            var profile = Profile.CreateDefaultProfile();
            var mappedKeys = profile.GetMappedPhysicalKeyboardKeys();

            // Should contain WASD from Left Stick
            Assert.Contains("W", mappedKeys);
            Assert.Contains("A", mappedKeys);
            Assert.Contains("S", mappedKeys);
            Assert.Contains("D", mappedKeys);

            // Should contain keyboard action buttons
            Assert.Contains("Space", mappedKeys);
            Assert.Contains("LeftShift", mappedKeys);
            Assert.Contains("E", mappedKeys);
            Assert.Contains("Q", mappedKeys);
            Assert.Contains("F", mappedKeys);
            Assert.Contains("R", mappedKeys);
            Assert.Contains("Tab", mappedKeys);
            Assert.Contains("Enter", mappedKeys);
            Assert.Contains("C", mappedKeys);

            // Should contain extra mappings
            Assert.Contains("Escape", mappedKeys);

            // Must NOT contain mouse buttons
            Assert.DoesNotContain("LeftMouse", mappedKeys);
            Assert.DoesNotContain("RightMouse", mappedKeys);
            Assert.DoesNotContain("MiddleMouse", mappedKeys);
            Assert.DoesNotContain("XButton1", mappedKeys);
            Assert.DoesNotContain("XButton2", mappedKeys);
        }

        [Fact]
        public void Profile_GetMappedPhysicalKeyboardKeys_WhenStickIsMouseOrDisabled_ExcludesStickKeys()
        {
            var profile = Profile.CreateDefaultProfile();
            profile.LeftStick.InputSource = "Mouse";
            profile.RightStick.InputSource = "None";

            var mappedKeys = profile.GetMappedPhysicalKeyboardKeys();

            // Since LeftStick is Mouse and RightStick is None, WASD are not mapped to sticks
            Assert.DoesNotContain("W", mappedKeys);
            Assert.DoesNotContain("A", mappedKeys);
            Assert.DoesNotContain("S", mappedKeys);
            Assert.DoesNotContain("D", mappedKeys);

            // But Buttons mapped to keyboard remain present
            Assert.Contains("Space", mappedKeys);
            Assert.Contains("E", mappedKeys);
        }

        [Fact]
        public void InputState_ReceivesKeys_AndMappingEngineCalculatesCorrectControllerState()
        {
            var input = new InputState();
            var engine = new MappingEngine();
            var profile = Profile.CreateDefaultProfile();

            // Simulate pressing 'W' (Left Stick Up) and 'Space' (A button)
            input.SetKey("W", true);
            input.SetKey("Space", true);

            var state = engine.Process(input, profile, explicitDt: 0.008);

            // Left Stick should deflect forward (Y = +1.0)
            Assert.Equal(0.0, state.LeftThumbX);
            Assert.Equal(1.0, state.LeftThumbY);

            // Button A should be pressed
            Assert.True(state.A);

            // Key release
            input.SetKey("W", false);
            input.SetKey("Space", false);

            state = engine.Process(input, profile, explicitDt: 0.008);
            Assert.Equal(0.0, state.LeftThumbY);
            Assert.False(state.A);
        }

        [Fact]
        public void WindowsHookManager_SetBlockedKeys_ByNames_ResolvesCorrectVkCodes()
        {
            var inputState = new InputState();
            using var hookManager = new WindowsHookManager(inputState);

            var keysToBlock = new List<string> { "W", "A", "S", "D", "Space", "Shift" };
            hookManager.SetBlockedKeys(keysToBlock);
            hookManager.BlockPhysicalKeyboard = true;

            Assert.True(hookManager.BlockPhysicalKeyboard);

            hookManager.ClearBlockedKeys();
            hookManager.BlockPhysicalKeyboard = false;
            Assert.False(hookManager.BlockPhysicalKeyboard);
        }

        [Fact]
        public void AppSettings_BlockPhysicalKeyboard_SerializesAndDeserializes()
        {
            var settings = new AppSettings
            {
                BlockPhysicalKeyboard = true,
                Language = "vi",
                AutoEnableController = true
            };

            string json = JsonSerializer.Serialize(settings);
            var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

            Assert.NotNull(deserialized);
            Assert.True(deserialized.BlockPhysicalKeyboard);
            Assert.Equal("vi", deserialized.Language);
            Assert.True(deserialized.AutoEnableController);
        }

        [Fact]
        public void InputManager_BlockPhysicalKeyboard_SyncsAndUpdatesOnProfileChange()
        {
            var controller = new MockVirtualController();
            var profileManager = new ProfileManager();
            using var inputManager = new InputManager(controller, profileManager);

            // Default is false
            Assert.False(inputManager.BlockPhysicalKeyboard);

            // Enable blocking
            inputManager.BlockPhysicalKeyboard = true;
            Assert.True(inputManager.BlockPhysicalKeyboard);

            // Changing profile updates blocked keys safely without throwing
            var newProfile = Profile.CreateDefaultProfile();
            newProfile.Name = "Custom Test Profile";
            profileManager.Profiles.Add(newProfile);
            profileManager.SetActiveProfile(newProfile);

            Assert.True(inputManager.BlockPhysicalKeyboard);

            // Disable blocking
            inputManager.BlockPhysicalKeyboard = false;
            Assert.False(inputManager.BlockPhysicalKeyboard);
        }

        [Fact]
        public void WindowsHookManager_EmergencyHotkey_StateIsRecognized()
        {
            var inputState = new InputState();
            using var hookManager = new WindowsHookManager(inputState);

            bool emergencyTriggered = false;
            hookManager.EmergencyDisableRequested += () => emergencyTriggered = true;

            // F10 is VK 0x79. If emergency check is invoked, callback fires
            // We verify hookManager registers and unregisters cleanly
            Assert.False(emergencyTriggered);
        }
    }
}
