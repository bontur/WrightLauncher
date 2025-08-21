using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WrightLauncher.Models;
using WrightLauncher.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace WrightLauncher.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private readonly ImageCacheService _imageCacheService;
        
        public static event Action? WrightProfileUpdated;
        
        [ObservableProperty]
        private ObservableCollection<Champion> _champions = new();

        [ObservableProperty]
        private ObservableCollection<Skin> _allSkins = new();

        [ObservableProperty]
        private ObservableCollection<Skin> _filteredSkins = new();

        [ObservableProperty]
        private ObservableCollection<Skin> _paginatedSkins = new();

        [ObservableProperty]
        private Champion? _selectedChampion;

        [ObservableProperty]
        private Skin? _selectedSkin;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _championSearchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Champion> _filteredChampions = new();

        [ObservableProperty]
        private bool _hasChampionSearchResults = true;

        [ObservableProperty]
        private AppSettings _settings = new();

        [ObservableProperty]
        private string _selectedMenuItem = "Home";

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private ObservableCollection<InstalledSkin> _installedSkins = new();

        [ObservableProperty]
        private string _downloadButtonText = "";

        [ObservableProperty]
        private bool _isDownloadingFromSpecialPage = false;

        [ObservableProperty]
        private bool _isDownloadButtonEnabled = true;

        [ObservableProperty]
        private Lobby? _currentLobby;

        [ObservableProperty]
        private int _currentUserId;

        [ObservableProperty]
        private ObservableCollection<Friend> _friends = new();

        [ObservableProperty]
        private ObservableCollection<Friend> _filteredFriends = new();

        [ObservableProperty]
        private string _friendSearchText = string.Empty;

        [ObservableProperty]
        private bool _isLobbyCreator = false;

        [ObservableProperty]
        private ObservableCollection<Friend> _lobbyMembers = new();

        [ObservableProperty]
        private ObservableCollection<SpecialSkin> _specialSkins = new();

        [ObservableProperty]
        private ObservableCollection<object> _homePageSkins = new();

        [ObservableProperty]
        private ObservableCollection<News> _news = new();

        [ObservableProperty]
        private bool _isNewsLoading = false;

        [ObservableProperty]
        private bool _hasPendingFriendRequests = false;

        [ObservableProperty]
        private int _pendingFriendRequestsCount = 0;

        [ObservableProperty]
        private string _appVersion = "1.0.0";

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        private const int ItemsPerPage = 5;

        [ObservableProperty]
        private ObservableCollection<InstalledSkin> _paginatedInstalledSkins = new();

        [ObservableProperty]
        private int _skinsCurrentPage = 1;

        [ObservableProperty]
        private int _skinsTotalPages = 1;

        [ObservableProperty]
        private bool _skinsCanGoPrevious = false;

        [ObservableProperty]
        private bool _skinsCanGoNext = false;

        [ObservableProperty]
        private string _skinsPageInfo = "";

        private const int SkinsItemsPerPage = 12;

        [ObservableProperty]
        private int _homePageCurrentPage = 1;

        [ObservableProperty]
        private int _homePageTotalPages = 1;

        [ObservableProperty]
        private bool _homePageCanGoPrevious = false;

        [ObservableProperty]
        private bool _homePageCanGoNext = false;

        [ObservableProperty]
        private string _homePageInfo = "";

        [ObservableProperty]
        private ObservableCollection<object> _paginatedHomePageSkins = new();

        private const int HomePageItemsPerPage = 5;

        public ICollectionView SkinsView { get; private set; }
        public ICollectionView InstalledSkinsView { get; private set; }
        public ICollectionView ChampionsView { get; private set; }
        public ICollectionView FriendsView { get; private set; }

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _imageCacheService = ImageCacheService.Instance;
            
            _downloadButtonText = LocalizationService.Instance.Translate("DownloadButtonDownload");
            
            _skinsPageInfo = string.Format(LocalizationService.Instance.Translate("SkinsPageInfo"), 1, 1, 0);
            
            SkinsView = CollectionViewSource.GetDefaultView(FilteredSkins);
            SkinsView.Filter = FilterSkins;
            
            InstalledSkinsView = CollectionViewSource.GetDefaultView(InstalledSkins);
            InstalledSkinsView.Filter = FilterInstalledSkins;
            
            ChampionsView = CollectionViewSource.GetDefaultView(FilteredChampions);
            ChampionsView.Filter = FilterChampions;
            
            FriendsView = CollectionViewSource.GetDefaultView(FilteredFriends);
            FriendsView.Filter = FilterFriends;
            
            LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
            SelectSkinCommand = new RelayCommand<Skin>(SelectSkin);
            ApplySkinCommand = new RelayCommand<Skin>(ApplySkin);
            LaunchGameCommand = new AsyncRelayCommand(LaunchGameAsync);
            NavigateCommand = new RelayCommand<string>(Navigate);
            SelectInstalledSkinCommand = new RelayCommand<InstalledSkin>(SelectInstalledSkin);
            SelectChampionCommand = new RelayCommand<Champion>(SelectChampion);
            NextPageCommand = new RelayCommand(NextPage, CanGoNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanGoPreviousPage);
            GoToPageCommand = new RelayCommand<int>(GoToPage);
            
            HomePageNextPageCommand = new RelayCommand(HomePageNextPage, CanGoHomePageNextPage);
            HomePagePreviousPageCommand = new RelayCommand(HomePagePreviousPage, CanGoHomePagePreviousPage);
            HomePageGoToPageCommand = new RelayCommand<int>(HomePageGoToPage);
            
            PropertyChanged += OnPropertyChanged;
            
            _ = LoadDataAsync();
            _ = LoadNewsAsync();
        }

        public IAsyncRelayCommand LoadDataCommand { get; }
        public IRelayCommand<Skin> SelectSkinCommand { get; }
        public IRelayCommand<Skin> ApplySkinCommand { get; }
        public IAsyncRelayCommand LaunchGameCommand { get; }
        public IRelayCommand<string> NavigateCommand { get; }
        public IRelayCommand<InstalledSkin> SelectInstalledSkinCommand { get; }
        public IRelayCommand<Champion> SelectChampionCommand { get; }
        public IRelayCommand NextPageCommand { get; }
        public IRelayCommand PreviousPageCommand { get; }
        public IRelayCommand<int> GoToPageCommand { get; }

        public IRelayCommand HomePageNextPageCommand { get; }
        public IRelayCommand HomePagePreviousPageCommand { get; }
        public IRelayCommand<int> HomePageGoToPageCommand { get; }

        partial void OnSelectedSkinChanged(Skin? value)
        {
            UpdateDownloadButtonState();
        }

        partial void OnChampionSearchTextChanged(string value)
        {
            ChampionsView.Refresh();
            UpdateFilteredChampions();
        }

        partial void OnFriendSearchTextChanged(string value)
        {
            FriendsView.Refresh();
        }

        private void SelectChampion(Champion? champion)
        {
            
            if (champion == null)
            {
                return;
            }

            SelectedChampion = champion;

try
            {
                FilterSkinsByChampion(champion.Name);
            }
            catch (Exception ex)
            {
            }
        }

        private async void FilterSkinsByChampion(string championName)
        {
            
            try
            {
                var championSkins = AllSkins.Where(s => s.Champion.Equals(championName, StringComparison.OrdinalIgnoreCase)).ToList();

FilteredSkins.Clear();
                foreach (var skin in championSkins)
                {
                    FilteredSkins.Add(skin);
                }
                
                SearchText = championName;

if (SelectedMenuItem == "Champions")
                {

var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    if (mainWindow != null)
                    {
                        try
                        {
                            mainWindow.ShowChampionsModalWithLoading(championName);
                        }
                        catch (Exception modalEx)
                        {
                        }

Task.Run(async () =>
                        {
                            try
                            {
                                var dynamicSkins = await _dataService.GetChampionSkinsAsync(championName).ConfigureAwait(false);
                                
                                await Application.Current.Dispatcher.InvokeAsync(async () =>
                                {
                                    try
                                    {
                                        if (dynamicSkins != null && dynamicSkins.Count > 0)
                                        {
                                            await mainWindow.UpdateChampionsModalWithData(championName, dynamicSkins);
                                        }
                                        else
                                        {
                                            mainWindow.UpdateChampionsModalWithPlaceholder(championName);
                                        }
                                    }
                                    catch (Exception uiEx)
                                    {
                                        mainWindow.UpdateChampionsModalWithPlaceholder(championName);
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        mainWindow.UpdateChampionsModalWithPlaceholder(championName);
                                    });
                                }
                                catch (Exception dispatcherEx)
                                {
                                }
                            }
                        }).ContinueWith(task =>
                        {
                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (task.IsFaulted)
                                {
                                }
                                else if (task.IsCompletedSuccessfully)
                                {
                                }
                                else
                                {
                                }
                            });
                        });
                        
                    }
                    else
                    {
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void UpdateDownloadButtonState()
        {
            if (SelectedSkin == null)
            {
                DownloadButtonText = LocalizationService.Instance.Translate("DownloadButtonDownload");
                IsDownloadButtonEnabled = false;
                return;
            }

            var skin = SelectedSkin;

if (skin.WadURL == "SPECIAL_SKIN_NO_DOWNLOAD")
            {
                DownloadButtonText = "🎁 " + LocalizationService.Instance.Translate("SpecialSkinPreview");
                IsDownloadButtonEnabled = false;
                return;
            }
            
            if (string.IsNullOrEmpty(skin.WadURL))
            {
                DownloadButtonText = LocalizationService.Instance.Translate("DownloadButtonNoLink");
                IsDownloadButtonEnabled = false;
                return;
            }

var existingSkin = InstalledSkins.FirstOrDefault(s => 
                s.Id == skin.Id || (s.Name == skin.Name && s.Champion == skin.Champion));

            if (existingSkin != null)
            {
                
                if (existingSkin.Version == skin.Version)
                {
                    DownloadButtonText = LocalizationService.Instance.Translate("DownloadButtonInstalled");
                    IsDownloadButtonEnabled = false;
                    return;
                }
                else
                {
                    DownloadButtonText = LocalizationService.Instance.Translate("DownloadButtonUpdate");
                    IsDownloadButtonEnabled = true;
                    return;
                }
            }

            DownloadButtonText = LocalizationService.Instance.Translate("DownloadButtonDownload");
            IsDownloadButtonEnabled = true;
        }

        partial void OnSearchTextChanged(string value)
        {
            SkinsView.Refresh();
            InstalledSkinsView.Refresh();
            SkinsCurrentPage = 1;
            HomePageCurrentPage = 1;
            UpdateHomePagePagination();
        }

        partial void OnSettingsChanged(AppSettings value)
        {
        }

        partial void OnSelectedChampionChanged(Champion? value)
        {
            if (value != null)
            {
                FilteredSkins.Clear();
                foreach (var skin in value.Skins)
                {
                    FilteredSkins.Add(skin);
                }
            }
            else
            {
                FilteredSkins.Clear();
                foreach (var skin in AllSkins)
                {
                    FilteredSkins.Add(skin);
                }
            }
            SkinsView.Refresh();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                
                await LoadConfigAsync();
                
                IsLoading = true;
                
                var skins = await _dataService.LoadSkinsAsync();
                
                AllSkins.Clear();
                FilteredSkins.Clear();
                
                foreach (var skin in skins)
                {
                    AllSkins.Add(skin);
                    FilteredSkins.Add(skin);
                }

                InitializeSkinsPagination();

                await LoadDiscordUsersAsync();

                await LoadInstalledSkinsAsync();

                await LoadChampionsAsync();

                Settings = await _dataService.LoadSettingsAsync();

}
            catch (Exception ex)
            {
            }
            finally
            {
                IsLoading = false;
                
            }
        }

        private async Task LoadChampionsAsync()
        {
            try
            {
                
                var champions = await _dataService.LoadChampionsAsync();
                
                Champions.Clear();
                FilteredChampions.Clear();
                
                foreach (var champion in champions)
                {
                    Champions.Add(champion);
                    FilteredChampions.Add(champion);
                }
                
                ChampionsView.Refresh();
            }
            catch (Exception ex)
            {
                
                try
                {
                    var fallbackChampions = new ObservableCollection<Champion>
                    {
                        new Champion { Id = 1, Name = "Ahri", Title = "the Nine-Tailed Fox", IconPath = "https://raw.communitydragon.org/latest/game/assets/characters/ahri/hud/ahri_square_0.png" },
                        new Champion { Id = 2, Name = "Yasuo", Title = "the Unforgiven", IconPath = "https://raw.communitydragon.org/latest/game/assets/characters/yasuo/hud/yasuo_square_0.png" },
                        new Champion { Id = 3, Name = "Zed", Title = "the Master of Shadows", IconPath = "https://raw.communitydragon.org/latest/game/assets/characters/zed/hud/zed_square_0.png" },
                        new Champion { Id = 4, Name = "Jinx", Title = "the Loose Cannon", IconPath = "https://raw.communitydragon.org/latest/game/assets/characters/jinx/hud/jinx_square_0.png" },
                        new Champion { Id = 5, Name = "Katarina", Title = "the Sinister Blade", IconPath = "https://raw.communitydragon.org/latest/game/assets/characters/katarina/hud/katarina_square_0.png" }
                    };
                    
                    Champions.Clear();
                    FilteredChampions.Clear();
                    
                    foreach (var champion in fallbackChampions)
                    {
                        Champions.Add(champion);
                        FilteredChampions.Add(champion);
                    }
                    
                    ChampionsView.Refresh();
                }
                catch (Exception fallbackEx)
                {
                }
            }
        }

        private async Task LoadDiscordUsersAsync()
        {
            try
            {
                
                var tasks = AllSkins.Select(async skin =>
                {
                    if (!string.IsNullOrEmpty(skin.Author))
                    {
                        skin.DiscordUser = await _dataService.GetDiscordUserAsync(skin.Author);
                        
                        if (skin.DiscordUser != null)
                        {
                            
                            if (!string.IsNullOrEmpty(skin.DiscordUser.AvatarLink))
                            {
                                skin.CachedDiscordAvatar = await _imageCacheService.GetCachedImageAsync(skin.DiscordUser.AvatarLink);
                            }
                        }
                        else
                        {
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(skin.ImageCard))
                    {
                        skin.CachedImageCard = await _imageCacheService.GetCachedImageAsync(skin.ImageCard);
                    }
                    
                    if (!string.IsNullOrEmpty(skin.ImagePreview))
                    {
                        skin.CachedImagePreview = await _imageCacheService.GetCachedImageAsync(skin.ImagePreview);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
            }
        }

        private async Task LoadInstalledSkinsAsync()
        {
            try
            {
                
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var installedJsonPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", "installed.json");
                var specialJsonPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", "special.json");

                InstalledSkins.Clear();
                HomePageSkins.Clear();

                if (File.Exists(specialJsonPath))
                {
                    var specialJsonContent = await File.ReadAllTextAsync(specialJsonPath);
                    var specialSkins = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InstalledSkin>>(specialJsonContent);
                    
                    if (specialSkins != null)
                    {
                        foreach (var specialSkin in specialSkins.AsEnumerable().Reverse())
                        {
                            specialSkin.IsSpecial = true;
                            HomePageSkins.Add(specialSkin);
                        }
                    }
                }
                else
                {
                }

                if (File.Exists(installedJsonPath))
                {
                    var jsonContent = await File.ReadAllTextAsync(installedJsonPath);
                    var installedSkins = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InstalledSkin>>(jsonContent);
                    
                    if (installedSkins != null)
                    {
                        foreach (var skin in installedSkins.AsEnumerable().Reverse())
                        {
                            InstalledSkins.Add(skin);
                            HomePageSkins.Add(skin);
                        }
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
            
            UpdateDownloadButtonState();
            
            await LoadWrightProfileSelectionsAsync();
            
            UpdatePagination();
            
            UpdateHomePagePagination();
        }

        private void SelectInstalledSkin(InstalledSkin? skin)
        {
            if (skin == null) return;

skin.IsSelected = !skin.IsSelected;

_ = UpdateWrightProfileAsync();
        }

        public async Task UpdateWrightProfileAsync()
        {
            try
            {
                
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var wrightSkinsPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
                var wrightProfilePath = Path.Combine(wrightSkinsPath, "Wright.profile");

                var selectedSkins = InstalledSkins.Where(s => s.IsSelected).ToList();
                
                var selectedLobbySkins = CurrentLobby?.LobbySkins?.Where(s => s.IsSelected).ToList() ?? new List<LobbySkin>();

var profileContent = new List<string>();
                
                foreach (var skin in selectedSkins)
                {
                    var cleanSkinName = skin.Name
                        .Replace(":", "")
                        .Replace("/", "");
                    
                    profileContent.Add(cleanSkinName);
                }
                
                foreach (var lobbySkin in selectedLobbySkins)
                {
                    var cleanSkinName = lobbySkin.SkinName
                        .Replace(":", "")
                        .Replace("/", "");
                    
                    profileContent.Add(cleanSkinName);
                }

                await File.WriteAllLinesAsync(wrightProfilePath, profileContent);

WrightProfileUpdated?.Invoke();
            }
            catch (Exception ex)
            {
            }
        }

        private string? ExtractChampionFromChromaName(string chromaName)
        {
            try
            {
                if (chromaName.Contains(" - "))
                {
                    var baseName = chromaName.Split(" - ")[0];
                    
                    var words = baseName.Trim().Split(' ');
                    if (words.Length > 0)
                    {
                        return words.Last();
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task LoadWrightProfileSelectionsAsync()
        {
            try
            {
                
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var wrightProfilePath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", "Wright.profile");

                if (!File.Exists(wrightProfilePath))
                {
                    return;
                }

                var profileLines = await File.ReadAllLinesAsync(wrightProfilePath);
                var selectedSkinNames = profileLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

foreach (var skin in InstalledSkins)
                {
                    skin.IsSelected = false;
                }

                foreach (var skinName in selectedSkinNames)
                {
                    var matchingSkin = InstalledSkins.FirstOrDefault(s => 
                        s.Name.Replace(":", "").Replace("/", "") == skinName.Trim());
                    
                    if (matchingSkin != null)
                    {
                        matchingSkin.IsSelected = true;
                    }
                    else
                    {
                    }
                }
                
            }
            catch (Exception ex)
            {
            }
        }

        private async Task LoadConfigAsync()
        {
            try
            {
                
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "config.json");

                if (File.Exists(configPath))
                {
                    var configJson = await File.ReadAllTextAsync(configPath);
                    var loadedSettings = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(configJson);
                    
                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                    }
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async Task SaveConfigAsync()
        {
            try
            {
                
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var wrightSkinsPath = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
                var configPath = Path.Combine(wrightSkinsPath, "config.json");

                Directory.CreateDirectory(wrightSkinsPath);

                var configJson = Newtonsoft.Json.JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(configPath, configJson);
                
            }
            catch (Exception ex)
            {
            }
        }

        private async Task LoadNewsAsync()
        {
            try
            {
                IsNewsLoading = true;

                var newsService = NewsService.Instance;
                var newsData = await newsService.GetNewsAsync();

                if (newsData != null && newsData.Count > 0)
                {
                    News.Clear();
                    foreach (var news in newsData.Take(5))
                    {
                        News.Add(news);
                    }
                    
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                IsNewsLoading = false;
            }
        }

        private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
        }

        private void SelectSkin(Skin? skin)
        {
            if (skin == null) return;

SelectedSkin = skin;
            
            if (SelectedMenuItem == "Champions")
            {
                
                foreach (var s in AllSkins)
                {
                    s.IsSelected = false;
                }

                skin.IsSelected = true;

                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ShowChampionsModal(skin);
                }
                else
                {
                }
            }
            else
            {
            }
        }

        private void ApplySkin(Skin? skin)
        {
            if (skin == null) return;

            SelectSkin(skin);
            
        }

        private async Task LaunchGameAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.GamePath))
                {
                    return;
                }

await Task.Delay(1000);
            }
            catch (Exception ex)
            {
            }
        }

        private void Navigate(string? menuItem)
        {
            if (string.IsNullOrEmpty(menuItem)) return;
            
            SelectedMenuItem = menuItem;
            
            switch (menuItem.ToLower())
            {
                case "home":
                    SelectedChampion = null;
                    SearchText = string.Empty;
                    _ = LoadInstalledSkinsAsync();
                    break;
                case "champions":
                    ChampionSearchText = string.Empty;
                    break;
                case "skins":
                    SelectedChampion = null;
                    SearchText = string.Empty;
                    break;
                default:
                    break;
            }
        }

        private bool FilterChampions(object item)
        {
            if (item is not Champion champion) return false;
            
            if (string.IsNullOrEmpty(ChampionSearchText) || ChampionSearchText.Length < 2) 
                return false;
            
            return champion.Name.Contains(ChampionSearchText, StringComparison.OrdinalIgnoreCase) ||
                   champion.Title.Contains(ChampionSearchText, StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateFilteredChampions()
        {
            FilteredChampions.Clear();
            
            if (string.IsNullOrEmpty(ChampionSearchText) || ChampionSearchText.Length < 2)
            {
                HasChampionSearchResults = true;
                return;
            }
            
            var filteredList = Champions.Where(champion => 
                champion.Name.Contains(ChampionSearchText, StringComparison.OrdinalIgnoreCase) ||
                champion.Title.Contains(ChampionSearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var champion in filteredList)
            {
                FilteredChampions.Add(champion);
            }
            
            HasChampionSearchResults = FilteredChampions.Count > 0;
        }

        private bool FilterSkins(object item)
        {
            if (item is not Skin skin) return false;
            
            if (string.IsNullOrEmpty(SearchText)) return true;
            
            return skin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   skin.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterInstalledSkins(object item)
        {
            if (item is not InstalledSkin installedSkin) return false;
            
            if (string.IsNullOrEmpty(SearchText)) return true;
            
            return installedSkin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   installedSkin.Champion.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterInstalledSkins(InstalledSkin installedSkin)
        {
            if (string.IsNullOrEmpty(SearchText)) return true;
            
            return installedSkin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   installedSkin.Champion.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterFriends(object item)
        {
            if (item is not Friend friend) return false;
            
            if (string.IsNullOrEmpty(FriendSearchText)) return true;
            
            return friend.Username.Contains(FriendSearchText, StringComparison.OrdinalIgnoreCase);
        }

        public void RemoveInstalledSkin(InstalledSkin skinToRemove)
        {
            try
            {
                var skinToDelete = InstalledSkins.FirstOrDefault(s => s.Id == skinToRemove.Id);
                if (skinToDelete != null)
                {
                    InstalledSkins.Remove(skinToDelete);
                    Console.WriteLine($"Skin ViewModel'den kaldırıldı: {skinToDelete.Name}");
                    
                    var homePageSkinToDelete = HomePageSkins.FirstOrDefault(s => s is InstalledSkin installedSkin && installedSkin.Id == skinToRemove.Id);
                    if (homePageSkinToDelete != null)
                    {
                        HomePageSkins.Remove(homePageSkinToDelete);
                        Console.WriteLine($"Skin HomePage'den kaldırıldı: {skinToDelete.Name}");
                        
                        UpdateHomePagePagination();
                    }
                    
                    if (SelectedSkin?.Id == skinToRemove.Id)
                    {
                        SelectedSkin = null;
                    }
                    
                    UpdateDownloadButtonState();
                    
                    UpdatePagination();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoveInstalledSkin hatası: {ex.Message}");
            }
        }

        private void UpdatePagination()
        {
            try
            {
                var filteredSkins = string.IsNullOrEmpty(SearchText) 
                    ? InstalledSkins.ToList()
                    : InstalledSkins.Where(FilterInstalledSkins).ToList();

                TotalPages = (int)Math.Ceiling((double)filteredSkins.Count / ItemsPerPage);
                
                if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = TotalPages;
                }
                else if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }

                var startIndex = (CurrentPage - 1) * ItemsPerPage;
                var pagedSkins = filteredSkins.Skip(startIndex).Take(ItemsPerPage).ToList();

                PaginatedInstalledSkins.Clear();
                foreach (var skin in pagedSkins)
                {
                    PaginatedInstalledSkins.Add(skin);
                }

                NextPageCommand?.NotifyCanExecuteChanged();
                PreviousPageCommand?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
            }
        }

        private void NextPage()
        {
            if (CanGoNextPage())
            {
                CurrentPage++;
                UpdatePagination();
            }
        }

        private bool CanGoNextPage()
        {
            return CurrentPage < TotalPages;
        }

        private void PreviousPage()
        {
            if (CanGoPreviousPage())
            {
                CurrentPage--;
                UpdatePagination();
            }
        }

        private bool CanGoPreviousPage()
        {
            return CurrentPage > 1;
        }

        private void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
                UpdatePagination();
            }
        }

        private void UpdateHomePagePagination()
        {
            var allSkins = HomePageSkins.ToList();
            
            if (!string.IsNullOrEmpty(SearchText))
            {
                allSkins = allSkins.Where(item =>
                {
                    if (item is InstalledSkin installedSkin)
                    {
                        return installedSkin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               installedSkin.Champion.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (item is SpecialSkin specialSkin)
                    {
                        return specialSkin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               specialSkin.Champion.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                }).ToList();
            }
            
            if (allSkins.Count == 0)
            {
                HomePageTotalPages = 1;
                HomePageCurrentPage = 1;
                PaginatedHomePageSkins.Clear();
                HomePageCanGoPrevious = false;
                HomePageCanGoNext = false;
                HomePageInfo = "0-0 of 0";
                return;
            }

            HomePageTotalPages = (int)Math.Ceiling((double)allSkins.Count / HomePageItemsPerPage);
            
            if (HomePageCurrentPage > HomePageTotalPages && HomePageTotalPages > 0)
                HomePageCurrentPage = HomePageTotalPages;
            else if (HomePageCurrentPage < 1)
                HomePageCurrentPage = 1;

            var startIndex = (HomePageCurrentPage - 1) * HomePageItemsPerPage;
            var pagedSkins = allSkins.Skip(startIndex).Take(HomePageItemsPerPage).ToList();

            PaginatedHomePageSkins.Clear();
            foreach (var skin in pagedSkins)
            {
                PaginatedHomePageSkins.Add(skin);
            }

            HomePageCanGoPrevious = HomePageCurrentPage > 1;
            HomePageCanGoNext = HomePageCurrentPage < HomePageTotalPages;
            
            var endIndex = Math.Min(startIndex + HomePageItemsPerPage, allSkins.Count);
            HomePageInfo = $"{startIndex + 1}-{endIndex} of {allSkins.Count}";

            HomePageNextPageCommand?.NotifyCanExecuteChanged();
            HomePagePreviousPageCommand?.NotifyCanExecuteChanged();
        }

        private void HomePageNextPage()
        {
            if (CanGoHomePageNextPage())
            {
                HomePageCurrentPage++;
                UpdateHomePagePagination();
            }
        }

        private bool CanGoHomePageNextPage()
        {
            return HomePageCurrentPage < HomePageTotalPages;
        }

        private void HomePagePreviousPage()
        {
            if (CanGoHomePagePreviousPage())
            {
                HomePageCurrentPage--;
                UpdateHomePagePagination();
            }
        }

        private bool CanGoHomePagePreviousPage()
        {
            return HomePageCurrentPage > 1;
        }

        private void HomePageGoToPage(int page)
        {
            if (page >= 1 && page <= HomePageTotalPages)
            {
                HomePageCurrentPage = page;
                UpdateHomePagePagination();
            }
        }

        public void UpdateFriendRequestBadge(int pendingCount)
        {
            try
            {
                PendingFriendRequestsCount = pendingCount;
                HasPendingFriendRequests = pendingCount > 0;
                
            }
            catch (Exception ex)
            {
            }
        }

        public void ResetFriendRequestBadge()
        {
            try
            {
                PendingFriendRequestsCount = 0;
                HasPendingFriendRequests = false;
                
            }
            catch (Exception ex)
            {
            }
        }

        private void InitializeSkinsPagination()
        {
            const int itemsPerPage = 12;
            
            SkinsTotalPages = (int)Math.Ceiling((double)FilteredSkins.Count / itemsPerPage);
            SkinsCurrentPage = 1;
            
            var pagedSkins = FilteredSkins.Take(itemsPerPage).ToList();
            
            PaginatedSkins.Clear();
            foreach (var skin in pagedSkins)
            {
                PaginatedSkins.Add(skin);
            }
            
            SkinsCanGoPrevious = false;
            SkinsCanGoNext = SkinsTotalPages > 1;
            SkinsPageInfo = string.Format(LocalizationService.Instance.Translate("SkinsPageInfo"), 1, Math.Max(1, SkinsTotalPages), FilteredSkins.Count);
        }
    }
}



