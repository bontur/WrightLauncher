using System;
using System.Collections.Generic;

namespace WrightLauncher.Models
{
    public class ChampionDataResponse
    {
        public string Type { get; set; } = "";
        public string Format { get; set; } = "";
        public string Version { get; set; } = "";
        public Dictionary<string, ChampionInfo> Data { get; set; } = new();
    }

    public class ChampionInfo
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public ChampionSkin[] Skins { get; set; } = Array.Empty<ChampionSkin>();
    }

    public class ChampionSkin
    {
        public string Id { get; set; } = "";
        public int Num { get; set; }
        public string Name { get; set; } = "";
        public bool Chromas { get; set; }
    }
}


