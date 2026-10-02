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

            string json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<Profile>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.LeftStick.Up, deserialized.LeftStick.Up);
            Assert.Equal(original.RightStick.Sensitivity, deserialized.RightStick.Sensitivity);
            Assert.Equal(original.Buttons["A"], deserialized.Buttons["A"]);
            Assert.Equal(original.ExtraMappings["Escape"], deserialized.ExtraMappings["Escape"]);
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
    }
}
