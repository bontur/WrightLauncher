using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WrightLauncher.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private Dictionary<string, string>? _translations = new();
        
        private string _currentCulture = "en_US";
        public string CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    OnPropertyChanged(nameof(CurrentCulture));
                    OnPropertyChanged(nameof(CurrentLanguage));
                }
            }
        }

        public string CurrentLanguage => CurrentCulture;
        
        public Dictionary<string, object>? Metadata { get; private set; }

        public event EventHandler? LanguageChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Load(string? culture = null)
        {
            try
            {
                var newCulture = culture ?? CurrentCulture;
                
                var resourceName = $"WrightLauncher.Assets.Lang.{newCulture}.json";
                
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        var path = $"Assets/Lang/{newCulture}.json";
                        if (File.Exists(path))
                        {
                            var json = File.ReadAllText(path);
                            ProcessLanguageData(json, newCulture);
                        }
                        else
                        {
                        }
                        return;
                    }
                    
                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        ProcessLanguageData(json, newCulture);
                    }
                }
            }
            catch (Exception)
            {
                if (culture != "en_US")
                {
                    Load("en_US");
                }
            }
        }

        private void ProcessLanguageData(string json, string newCulture)
        {
            var parsed = JToken.Parse(json);
            
            if (parsed is JArray array && array.Count > 1)
            {
                var metadata = array[0] as JObject;
                var translations = array[1] as JObject;
                
                if (metadata != null)
                {
                    Metadata = metadata.ToObject<Dictionary<string, object>>();
                }
                
                if (translations != null)
                {
                    _translations = translations.ToObject<Dictionary<string, string>>();
                    if (_translations != null)
                    {
                    }
                }
            }
            else if (parsed is JObject jsonObject)
            {
                if (jsonObject["_metadata"] != null)
                {
                    Metadata = jsonObject["_metadata"]?.ToObject<Dictionary<string, object>>();
                    jsonObject.Remove("_metadata");
                }
                
                var dict = jsonObject.ToObject<Dictionary<string, string>>();
                if (dict != null)
                {
                    _translations = dict;
                }
            }
            
            CurrentCulture = newCulture;
            
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public string Translate(string key)
        {
            if (_translations != null && _translations.TryGetValue(key, out var value))
            {
                return value;
            }
            return key;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



