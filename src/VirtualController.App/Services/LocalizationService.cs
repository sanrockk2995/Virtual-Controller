using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using VirtualController.Services;

namespace VirtualController.App.Services
{
    /// <summary>
    /// Provides dynamic runtime multi-language localization (Vietnamese and English).
    /// Swaps XAML string resource dictionaries without restarting the application.
    /// </summary>
    public static class LocalizationService
    {
        public static event EventHandler? LanguageChanged;

        private static string _currentLanguage = "vi";
        public static string CurrentLanguage => _currentLanguage;

        public static void Initialize(AppSettings settings)
        {
            string lang = settings?.Language ?? "";
            if (string.IsNullOrWhiteSpace(lang))
            {
                lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "vi";
            }
            SetLanguage(lang, settings);
        }

        public static void SetLanguage(string languageCode, AppSettings? settings = null)
        {
            if (string.IsNullOrWhiteSpace(languageCode)) languageCode = "vi";
            languageCode = languageCode.ToLowerInvariant().StartsWith("en") ? "en" : "vi";
            _currentLanguage = languageCode;

            if (settings != null)
            {
                settings.Language = languageCode;
            }

            string uriStr = $"pack://application:,,,/VirtualController;component/Resources/Strings.{languageCode}.xaml";
            var dictUri = new Uri(uriStr, UriKind.Absolute);

            var app = Application.Current;
            if (app != null)
            {
                try
                {
                    var existingDict = app.Resources.MergedDictionaries
                        .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Strings."));

                    var newDict = new ResourceDictionary { Source = dictUri };

                    if (existingDict != null)
                    {
                        int index = app.Resources.MergedDictionaries.IndexOf(existingDict);
                        app.Resources.MergedDictionaries[index] = newDict;
                    }
                    else
                    {
                        app.Resources.MergedDictionaries.Insert(0, newDict);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"Failed to load language resource dictionary: {dictUri}. {ex.Message}");
                }
            }

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string GetString(string key, string fallback = "")
        {
            try
            {
                if (Application.Current != null && Application.Current.TryFindResource(key) is string text)
                {
                    return text;
                }
            }
            catch { }

            return !string.IsNullOrEmpty(fallback) ? fallback : key;
        }
    }
}
