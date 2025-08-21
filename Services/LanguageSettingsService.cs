using System;
using System.IO;
using System.Text.Json;
using WrightLauncher.Models;

namespace WrightLauncher.Services
{
    public class LanguageSettingsService
    {
        private static readonly Lazy<LanguageSettingsService> _instance = new Lazy<LanguageSettingsService>(() => new LanguageSettingsService());
        public static LanguageSettingsService Instance => _instance.Value;

        private readonly string _settingsPath;

        private LanguageSettingsService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightSkinsPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            Directory.CreateDirectory(wrightSkinsPath);
            _settingsPath = Path.Combine(wrightSkinsPath, "lang.json");
        }

        public LanguageSettings LoadLanguageSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<LanguageSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
                
                var defaultSettings = new LanguageSettings
                {
                    LanguageCode = "en_US",
                    LanguageName = "English",
                    LastUpdated = DateTime.Now
                };

                return defaultSettings;
            }
            catch (Exception ex)
            {
                
                return new LanguageSettings
                {
                    LanguageCode = "tr_TR",
                    LanguageName = "Türkçe",
                    LastUpdated = DateTime.Now
                };
            }
        }

        public void SaveLanguageSettings(LanguageSettings settings)
        {
            try
            {
                settings.LastUpdated = DateTime.Now;
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                
            }
            catch (Exception ex)
            {
            }
        }

        public void SaveLanguageSettings(string languageCode, string languageName)
        {
            var settings = new LanguageSettings
            {
                LanguageCode = languageCode,
                LanguageName = languageName,
                LastUpdated = DateTime.Now
            };
            
            SaveLanguageSettings(settings);
        }
    }
}



