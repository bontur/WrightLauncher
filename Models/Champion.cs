using System.Collections.ObjectModel;

namespace WrightLauncher.Models
{
    public class Champion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public ObservableCollection<Skin> Skins { get; set; } = new();
    }
}

