using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace VirtualController.Services
{
    public class AppSettings
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "VirtualXboxController";

        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool AutoEnableController { get; set; } = false;
        public string ActiveProfileName { get; set; } = "Default Keyboard + Mouse";
        public string Hotkey { get; set; } = "Ctrl + Alt + F10";
        public string Language { get; set; } = "vi";
        public bool BlockPhysicalKeyboard { get; set; } = false;

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static string SettingsFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                string path = SettingsFilePath;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to load settings.json, using defaults.", ex);
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsFilePath)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
                ApplyStartupRegistry(StartWithWindows);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to save settings.json.", ex);
            }
        }

        public static void ApplyStartupRegistry(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppRegistryName, $"\"{exePath}\" --minimized");
                    }
                }
                else
                {
                    if (key.GetValue(AppRegistryName) != null)
                    {
                        key.DeleteValue(AppRegistryName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Failed to update Run registry key.", ex);
            }
        }
    }
}
