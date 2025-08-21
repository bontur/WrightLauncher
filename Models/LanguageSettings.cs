using System;

namespace WrightLauncher.Models
{
    public class LanguageSettings
    {
        public string LanguageCode { get; set; } = "tr_TR";
        public string LanguageName { get; set; } = "Türkçe";
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}

