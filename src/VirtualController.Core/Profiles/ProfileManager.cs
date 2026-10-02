using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using VirtualController.Services;

namespace VirtualController.Profiles
{
    public class ProfileManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _profilesDirectory;
        public ObservableCollection<Profile> Profiles { get; } = new();
        public Profile ActiveProfile { get; private set; }

        public event EventHandler<Profile>? ActiveProfileChanged;

        public ProfileManager(string? customDirectory = null)
        {
            _profilesDirectory = customDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles");

            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
            }

            ActiveProfile = Profile.CreateDefaultProfile();
            LoadAllProfiles();
        }

        public void LoadAllProfiles()
        {
            Profiles.Clear();

            try
            {
                var files = Directory.GetFiles(_profilesDirectory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var profile = JsonSerializer.Deserialize<Profile>(json, JsonOptions);
                        if (profile != null && !string.IsNullOrWhiteSpace(profile.Name))
                        {
                            Profiles.Add(profile);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to parse profile file: {file}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to list profiles directory.", ex);
            }

            // Ensure default profile exists if list is empty
            var defaultProfile = Profiles.FirstOrDefault(p => p.Name.Equals("Default Keyboard + Mouse", StringComparison.OrdinalIgnoreCase));
            if (defaultProfile == null)
            {
                defaultProfile = Profile.CreateDefaultProfile();
                Profiles.Add(defaultProfile);
                SaveProfile(defaultProfile);
            }

            SetActiveProfile(defaultProfile);
        }

        public void SetActiveProfile(Profile profile)
        {
            if (profile == null) return;
            if (ActiveProfile != null && (ReferenceEquals(ActiveProfile, profile) || string.Equals(ActiveProfile.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            ActiveProfile = profile;
            AppLogger.Info($"Active profile changed to: {profile.Name}");
            ActiveProfileChanged?.Invoke(this, profile);
        }

        public void SetActiveProfileByName(string name)
        {
            var p = Profiles.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (p != null)
            {
                SetActiveProfile(p);
            }
        }

        public Profile CreateProfile(string name)
        {
            var p = new Profile
            {
                Name = name
            };
            Profiles.Add(p);
            SaveProfile(p);
            AppLogger.Info($"Created new profile: {name}");
            return p;
        }

        public Profile DuplicateProfile(Profile source, string newName)
        {
            var p = source.Clone();
            p.Name = newName;
            Profiles.Add(p);
            SaveProfile(p);
            AppLogger.Info($"Duplicated profile '{source.Name}' to '{newName}'");
            return p;
        }

        public bool RenameProfile(Profile profile, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName) || profile.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
                return false;

            string oldFile = GetProfileFilePath(profile.Name);
            profile.Name = newName;
            SaveProfile(profile);

            try
            {
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting old profile file during rename: {oldFile}", ex);
            }

            AppLogger.Info($"Renamed profile to '{newName}'");
            return true;
        }

        public bool DeleteProfile(Profile profile)
        {
            if (Profiles.Count <= 1)
            {
                // Do not delete the last remaining profile
                return false;
            }

            Profiles.Remove(profile);
            string file = GetProfileFilePath(profile.Name);
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error deleting profile file: {file}", ex);
            }

            if (ActiveProfile == profile)
            {
                SetActiveProfile(Profiles.First());
            }

            AppLogger.Info($"Deleted profile: {profile.Name}");
            return true;
        }

        public void SaveProfile(Profile profile)
        {
            try
            {
                string file = GetProfileFilePath(profile.Name);
                string json = JsonSerializer.Serialize(profile, JsonOptions);
                File.WriteAllText(file, json);
                AppLogger.Info($"Saved profile: {profile.Name}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error saving profile: {profile.Name}", ex);
            }
        }

        public void ExportProfile(Profile profile, string destinationFilePath)
        {
            string json = JsonSerializer.Serialize(profile, JsonOptions);
            File.WriteAllText(destinationFilePath, json);
            AppLogger.Info($"Exported profile '{profile.Name}' to {destinationFilePath}");
        }

        public Profile ImportProfile(string sourceFilePath)
        {
            string json = File.ReadAllText(sourceFilePath);
            var imported = JsonSerializer.Deserialize<Profile>(json, JsonOptions)
                ?? throw new InvalidOperationException("Invalid profile JSON format.");

            // Avoid name collision
            string baseName = imported.Name;
            int counter = 1;
            while (Profiles.Any(p => p.Name.Equals(imported.Name, StringComparison.OrdinalIgnoreCase)))
            {
                imported.Name = $"{baseName} ({counter++})";
            }

            Profiles.Add(imported);
            SaveProfile(imported);
            AppLogger.Info($"Imported profile as '{imported.Name}' from {sourceFilePath}");
            return imported;
        }

        private string GetProfileFilePath(string profileName)
        {
            // Sanitize filename
            string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_profilesDirectory, $"{safeName}.json");
        }
    }
}
