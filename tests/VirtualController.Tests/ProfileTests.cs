using System;
using System.IO;
using System.Text.Json;
using VirtualController.Profiles;
using Xunit;

namespace VirtualController.Tests
{
    public class ProfileTests : IDisposable
    {
        private readonly string _testProfileDir;

        public ProfileTests()
        {
            _testProfileDir = Path.Combine(Path.GetTempPath(), "VC_ProfileTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testProfileDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testProfileDir))
                {
                    Directory.Delete(_testProfileDir, true);
                }
            }
            catch
            {
                // Ignored
            }
        }

        [Fact]
        public void Profile_JsonSerializationRoundTrip()
        {
            var original = Profile.CreateDefaultProfile();
            original.RightStick.SensitivityX = 1.25;
            original.RightStick.SensitivityY = 0.85;
            original.RightStick.AntiDeadzone = 0.18;
            original.RightStick.ResponseCurve = "Aggressive";
            original.RightStick.IsRadial = false;

            string json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<Profile>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.LeftStick.Up, deserialized.LeftStick.Up);
            Assert.Equal(original.RightStick.Sensitivity, deserialized.RightStick.Sensitivity);
            Assert.Equal(1.25, deserialized.RightStick.SensitivityX);
            Assert.Equal(0.85, deserialized.RightStick.SensitivityY);
            Assert.Equal(0.18, deserialized.RightStick.AntiDeadzone);
            Assert.Equal("Aggressive", deserialized.RightStick.ResponseCurve);
            Assert.False(deserialized.RightStick.IsRadial);
            Assert.Equal(original.Buttons["A"], deserialized.Buttons["A"]);
            Assert.Equal(original.ExtraMappings["Escape"], deserialized.ExtraMappings["Escape"]);
        }

        [Fact]
        public void LegacyProfile_WithoutNewProperties_LoadsWithSafeDefaults()
        {
            // Legacy JSON strictly lacking SensitivityX, SensitivityY, AntiDeadzone, IsRadial
            string legacyJson = @"
            {
              ""Name"": ""Old Legacy Profile"",
              ""LeftStick"": {
                ""Up"": ""W"",
                ""Down"": ""S"",
                ""Left"": ""A"",
                ""Right"": ""D"",
                ""Deadzone"": 0.1,
                ""Sensitivity"": 1.0,
                ""ResponseCurve"": ""Linear""
              },
              ""RightStick"": {
                ""InputSource"": ""Mouse"",
                ""Sensitivity"": 1.2,
                ""Deadzone"": 0.05,
                ""Smoothing"": 0.25,
                ""MaxMagnitude"": 1.0,
                ""InvertX"": false,
                ""InvertY"": false,
                ""Acceleration"": 1.0,
                ""DecayRate"": 0.8
              },
              ""Buttons"": {
                ""A"": ""Space""
              }
            }";

            var profile = JsonSerializer.Deserialize<Profile>(legacyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(profile);
            Assert.Equal("Old Legacy Profile", profile.Name);
            // Verify safe defaults assigned for missing new properties
            Assert.Equal(1.0, profile.RightStick.SensitivityX);
            Assert.Equal(1.0, profile.RightStick.SensitivityY);
            Assert.True(profile.RightStick.AntiDeadzone >= 0.0);
            Assert.True(profile.RightStick.IsRadial);
            Assert.True(profile.RightStick.IsMouse);
        }

        [Fact]
        public void ProfileManager_CanCreate_Duplicate_Rename_Delete()
        {
            var manager = new ProfileManager(_testProfileDir);

            // Initially default profile is created
            Assert.Single(manager.Profiles);
            Assert.Equal("Default Keyboard + Mouse", manager.ActiveProfile.Name);

            // Create new profile
            var custom = manager.CreateProfile("Custom Profile");
            Assert.Equal(2, manager.Profiles.Count);

            // Duplicate
            var dup = manager.DuplicateProfile(custom, "Custom Profile Copy");
            Assert.Equal(3, manager.Profiles.Count);

            // Rename
            bool renamed = manager.RenameProfile(dup, "Custom Profile Renamed");
            Assert.True(renamed);
            Assert.Equal("Custom Profile Renamed", dup.Name);

            // Delete
            bool deleted = manager.DeleteProfile(custom);
            Assert.True(deleted);
            Assert.Equal(2, manager.Profiles.Count);

            // Import & Export
            string exportPath = Path.Combine(_testProfileDir, "exported.json");
            manager.ExportProfile(dup, exportPath);
            Assert.True(File.Exists(exportPath));

            var imported = manager.ImportProfile(exportPath);
            Assert.NotNull(imported);
            Assert.StartsWith("Custom Profile Renamed", imported.Name);
        }

        [Fact]
        public void AppSettings_Language_DefaultsToVietnamese_AndPersistsCorrectly()
        {
            var settings = new VirtualController.Services.AppSettings();
            Assert.Equal("vi", settings.Language);

            settings.Language = "en";
            string json = System.Text.Json.JsonSerializer.Serialize(settings);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<VirtualController.Services.AppSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("en", deserialized.Language);
        }
    }
}
