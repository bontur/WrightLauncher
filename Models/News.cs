using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WrightLauncher.Models
{
    public class News : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("tag")]
        public string Tag { get; set; } = string.Empty;

        [JsonPropertyName("tag_color")]
        public string TagColor { get; set; } = "#6366f1";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("photo")]
        public string Photo { get; set; } = string.Empty;

        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; } = string.Empty;

        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; } = string.Empty;

        public string FullPhotoUrl => string.IsNullOrEmpty(Photo) 
            ? string.Empty 
            : $"https://wrightskins.com/{Photo}";

        public string FormattedDate
        {
            get
            {
                if (DateTime.TryParse(CreatedAt, out DateTime date))
                {
                    return date.ToString("dd MMM yyyy");
                }
                return CreatedAt;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


