using Newtonsoft.Json;
using WrightLauncher.Models;
using WrightLauncher.Utilities;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Windows;

namespace WrightLauncher.Services
{
    public interface IDataService
    {
        Task<ObservableCollection<Champion>> LoadChampionsAsync();
        Task<ObservableCollection<Skin>> LoadSkinsAsync();
        Task<AppSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(AppSettings settings);
        Task<DiscordUser?> GetDiscordUserAsync(string userId);
        Task<List<ChampionSkinData>> GetChampionSkinsAsync(string championName);
    }

    public class DataService : IDataService
    {
        private readonly string _dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data");
        private readonly string _championsFile = "champions.json";
        private readonly string _settingsFile = "settings.json";
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint = $"{WrightUtils.E}/forLauncher/skins?token={WrightUtils.D}";
        private readonly string _discordLookupUrl = "https://discordlookup.mesalytic.moe/v1/user/";
        private readonly string _repositoryApiUrl = "https://api.github.com/repos/darkseal-org/lol-skins/contents/skins";

        public DataService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            
            _httpClient.DefaultRequestHeaders.Add("Authorization", "token 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");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WrightLauncher/1.0");
        }

        public async Task<ObservableCollection<Skin>> LoadSkinsAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_apiEndpoint);
                var skins = JsonConvert.DeserializeObject<ObservableCollection<Skin>>(response);
                return skins ?? new ObservableCollection<Skin>();
            }
            catch (Exception ex)
            {
                return CreateFallbackSkins();
            }
        }

        public async Task<DiscordUser?> GetDiscordUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                var url = $"{_discordLookupUrl}{userId}";
                
                var response = await _httpClient.GetStringAsync(url);
                
                var discordUser = JsonConvert.DeserializeObject<DiscordUser>(response);
                
                if (discordUser != null)
                {
                }
                
                return discordUser;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<ObservableCollection<Champion>> LoadChampionsAsync()
        {
            try
            {
                
                var champions = await FetchChampionsFromGitHubAsync();
                
                if (champions.Count > 0)
                {
                    await SaveChampionsAsync(champions);
                    return champions;
                }

var filePath = Path.Combine(_dataFolder, _championsFile);
                
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var cachedChampions = JsonConvert.DeserializeObject<ObservableCollection<Champion>>(json);
                    if (cachedChampions != null && cachedChampions.Count > 0)
                    {
                        return cachedChampions;
                    }
                    else
                    {
                    }
                }
                else
                {
                }

                var fallbackData = CreateSampleData();
                return fallbackData;
            }
            catch (Exception ex)
            {
                var emergencyData = CreateSampleData();
                return emergencyData;
            }
        }

        public async Task<List<ChampionSkinData>> GetChampionSkinsAsync(string championName)
        {
            try
            {
                var codename = GetChampionCodename(championName);
                
                var communityDragonUrl = $"https://cdn.communitydragon.org/latest/champion/{codename}/data";
                
                var communityResponse = await _httpClient.GetStringAsync(communityDragonUrl);
                var communityData = JsonConvert.DeserializeObject<CommunityDragonResponse>(communityResponse);
                
                if (communityData?.Skins == null)
                {
                    return new List<ChampionSkinData>();
                }

                var availableSkins = communityData.Skins.Where(s => !s.IsBase).ToList();

                var githubUrl = $"https://api.github.com/repos/darkseal-org/lol-skins/contents/skins/{championName}";
                
                try
                {
                    var githubResponse = await _httpClient.GetStringAsync(githubUrl);
                    var githubFiles = JsonConvert.DeserializeObject<List<GitHubApiResponse>>(githubResponse);
                    
                    if (githubFiles != null)
                    {
                        foreach (var skin in availableSkins)
                        {
                        var cleanedSkinName = skin.Name
                            .Replace(":", "")
                            .Replace("/", " ");
                            
                            var matchingFile = githubFiles.FirstOrDefault(f => 
                                f.Name.Contains(cleanedSkinName, StringComparison.OrdinalIgnoreCase) && 
                                f.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                                
                            if (matchingFile != null)
                            {
                                skin.GitHubDownloadUrl = matchingFile.DownloadUrl ?? "";
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }

                return availableSkins;
            }
            catch (Exception)
            {
                return new List<ChampionSkinData>();
            }
        }

        private async Task<ObservableCollection<Champion>> FetchChampionsFromGitHubAsync()
        {
            try
            {
                
                var response = await _httpClient.GetAsync(_repositoryApiUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new ObservableCollection<Champion>();
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                var apiResponse = JsonConvert.DeserializeObject<List<GitHubApiResponse>>(responseContent);
                
                if (apiResponse == null)
                {
                    return new ObservableCollection<Champion>();
                }

                var champions = new ObservableCollection<Champion>();
                int id = 1;
                int totalItems = apiResponse.Count;
                int dirCount = 0;

foreach (var item in apiResponse)
                {
                    if (item.Type == "dir" && !string.IsNullOrEmpty(item.Name))
                    {
                        dirCount++;
                        
                        var codename = GetChampionCodename(item.Name);
                        
                        var iconUrl = $"https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/{codename}_0.jpg";
                        
                        var champion = new Champion
                        {
                            Id = id++,
                            Name = item.Name,
                            Title = GetChampionTitle(item.Name),
                            IconPath = iconUrl
                        };
                        
                        champions.Add(champion);
                        if (dirCount <= 10)
                        {
                            if (codename != item.Name)
                            {
                            }
                        }
                    }
                }

return champions;
            }
            catch (HttpRequestException httpEx)
            {
                return new ObservableCollection<Champion>();
            }
            catch (TaskCanceledException timeoutEx)
            {
                return new ObservableCollection<Champion>();
            }
            catch (JsonException jsonEx)
            {
                return new ObservableCollection<Champion>();
            }
            catch (Exception ex)
            {
                return new ObservableCollection<Champion>();
            }
        }

        private string GetChampionCodename(string championName)
        {
            var codenames = new Dictionary<string, string>
            {
                { "Wukong", "monkeyking" },
                { "Aurelion Sol", "aurelionsol" },
                { "Bel'Veth", "belveth" },
                { "Kai'Sa", "kaisa" },
                { "Cho'Gath", "chogath" },
                { "Fiddlesticks", "fiddlesticks" },
                { "Dr. Mundo", "drmundo" },
                { "Jarvan IV", "jarvaniv" },
                { "K'Sante", "ksante" },
                { "Kha'Zix", "khazix" },
                { "Kog'Maw", "kogmaw" },
                { "LeBlanc", "leblanc" },
                { "Lee Sin", "leesin" },
                { "Master Yi", "masteryi" },
                { "Miss Fortune", "missfortune" },
                { "Nunu & Willump", "nunu" },
                { "Rek'Sai", "reksai" },
                { "Renata Glasc", "renata" },
                { "Tahm Kench", "tahmkench" },
                { "Twisted Fate", "twistedfate" },
                { "Vel'Koz", "velkoz" },
                { "Xin Zhao", "xinzhao" }
            };
            
            return codenames.ContainsKey(championName) ? codenames[championName] : championName.ToLowerInvariant();
        }

        private string GetChampionTitle(string championName)
        {
            var titles = new Dictionary<string, string>
            {
                { "Ahri", "the Nine-Tailed Fox" },
                { "Yasuo", "the Unforgiven" },
                { "Zed", "the Master of Shadows" },
                { "Jinx", "the Loose Cannon" },
                { "Katarina", "the Sinister Blade" },
                { "Akali", "the Rogue Assassin" },
                { "Lux", "the Lady of Luminosity" },
                { "Ezreal", "the Prodigal Explorer" },
                { "Ashe", "the Frost Archer" },
                { "Garen", "the Might of Demacia" }
            };
            
            return titles.ContainsKey(championName) ? titles[championName] : "Champion";
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            try
            {
                var filePath = Path.Combine(_dataFolder, _settingsFile);
                
                if (!File.Exists(filePath))
                {
                    var defaultSettings = new AppSettings();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                return new AppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(_dataFolder);
                var filePath = Path.Combine(_dataFolder, _settingsFile);
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
            }
        }

        private async Task SaveChampionsAsync(ObservableCollection<Champion> champions)
        {
            try
            {
                Directory.CreateDirectory(_dataFolder);
                var filePath = Path.Combine(_dataFolder, _championsFile);
                var json = JsonConvert.SerializeObject(champions, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
            }
        }

        private ObservableCollection<Champion> CreateSampleData()
        {
            return new ObservableCollection<Champion>
            {
                new Champion { Id = 1, Name = "Ahri", Title = "the Nine-Tailed Fox", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/Ahri_0.jpg" },
                new Champion { Id = 2, Name = "Yasuo", Title = "the Unforgiven", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/Yasuo_0.jpg" },
                new Champion { Id = 3, Name = "Zed", Title = "the Master of Shadows", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/Zed_0.jpg" },
                new Champion { Id = 4, Name = "Jinx", Title = "the Loose Cannon", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/Jinx_0.jpg" },
                new Champion { Id = 5, Name = "Katarina", Title = "the Sinister Blade", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/Katarina_0.jpg" },
                new Champion { Id = 6, Name = "Wukong", Title = "the Monkey King", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/MonkeyKing_0.jpg" },
                new Champion { Id = 7, Name = "Twisted Fate", Title = "the Card Master", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/TwistedFate_0.jpg" },
                new Champion { Id = 8, Name = "Lee Sin", Title = "the Blind Monk", IconPath = "https://raw.githubusercontent.com/InFinity54/LoL_DDragon/refs/heads/master/img/champion/tiles/LeeSin_0.jpg" }
            };
        }

        private ObservableCollection<Skin> CreateFallbackSkins()
        {
            return new ObservableCollection<Skin>
            {
                new Skin
                {
                    Id = 1,
                    Name = "Placeholder Skin",
                    Author = "RiotGames#0000",
                    Description = " ",
                    ImageCard = "https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Qiyana_0.jpg",
                    Champion = "Qiyana",
                    FileURL = "",
                    ChampionId = 1,
                    Rarity = "Epic",
                    Price = 1350,
                    IsOwned = true
                },
                new Skin
                {
                    Id = 2,
                    Name = "No Internet Connection",
                    Author = "RiotGames#0000",
                    Description = " ",
                    ImageCard = "https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Zed_13.jpg",
                    Champion = "Yasuo",
                    FileURL = "",
                    ChampionId = 2,
                    Rarity = "Legendary",
                    Price = 1820,
                    IsOwned = false
                }
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}





