using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Reflection;
using WrightLauncher.ViewModels;
using WrightLauncher.Services;
using WrightLauncher.Utilities;
using WrightLauncher.Views;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using IOPath = System.IO.Path;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WrightLauncher.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Shapes;
using System.Linq;
using System.Text;
using System.Media;

namespace WrightLauncher
{

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const string APP_VERSION = "beta0.1";
    
    private string _currentSortType = "newest";
    
    private int _currentPage = 1;
    private int _itemsPerPage = 12;
    private ObservableCollection<SkinData> _paginatedSkins = new ObservableCollection<SkinData>();
    
    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);
    
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MONITORINFO
    {
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        public RECT rcMonitor = new RECT();
        public RECT rcWork = new RECT();
        public int dwFlags = 0;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }
    
    private System.Diagnostics.Process? _modToolsProcess = null;
    private readonly object _processLock = new object();
    private bool _isInjecting = false;
    private bool _processStarted = false;
    private readonly DiscordService _discordService;
    private readonly SocketIORealtimeService _socketIOService;
    
    private Skin? _currentPreviewSkin = null;
    
    private Dictionary<string, Dictionary<string, string>> _championSkinMappings = new();
    private readonly HttpClient _skinDataHttpClient = new HttpClient();
    
    private bool _isInitializing = true;
    
    private void LoadAndApplyLanguageSettings()
    {
        try
        {
            var languageSettings = LanguageSettingsService.Instance.LoadLanguageSettings();
            
            LocalizationService.Instance.Load(languageSettings.LanguageCode);
            DebugConsoleWindow.Instance.WriteLine($"? Dil ayarlarý yüklendi ve uygulandý: {languageSettings.LanguageCode} ({languageSettings.LanguageName})", "SUCCESS");
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"? Dil ayarlarý yüklenirken hata: {ex.Message}", "ERROR");
            
            LocalizationService.Instance.Load("en_US");
            DebugConsoleWindow.Instance.WriteLine("?? Varsayýlan dil (en_US) yüklendi", "WARNING");
        }
    }

    private bool ShouldShowFirstUseModal()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            string dataPath = System.IO.Path.Combine(wrightSkinsPath, "data.json");
            
            if (!File.Exists(dataPath))
            {
                return true;
            }

            string dataJson = File.ReadAllText(dataPath);
            if (string.IsNullOrWhiteSpace(dataJson))
            {
                return true;
            }

            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(dataJson);
            
            if (data?.firstSettingsDone == null || data.firstSettingsDone != true)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"FirstUseModal kontrolü sýrasýnda hata: {ex.Message}", "ERROR");
            return true;
        }
    }

    private void ShowFirstUseModal()
    {
        try
        {
            DebugConsoleWindow.Instance.WriteLine("FirstUseModal gösteriliyor", "INFO");
            
            var firstUseModal = new FirstUseModal();
            var result = firstUseModal.ShowDialog();
            
            if (result == true)
            {
                DebugConsoleWindow.Instance.WriteLine("FirstUseModal baþarýyla tamamlandý", "SUCCESS");
                
                CreateFirstSettingsCompletedFlag();
                
                LoadAndApplyLanguageSettings();
            }
            else
            {
                DebugConsoleWindow.Instance.WriteLine("FirstUseModal atlandý", "WARNING");
                
                CreateFirstSettingsCompletedFlag();
            }
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"FirstUseModal gösterilirken hata: {ex.Message}", "ERROR");
        }
    }

    private void CreateFirstSettingsCompletedFlag()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            string dataPath = System.IO.Path.Combine(wrightSkinsPath, "data.json");
            
            if (!Directory.Exists(wrightSkinsPath))
            {
                Directory.CreateDirectory(wrightSkinsPath);
                DebugConsoleWindow.Instance.WriteLine($"WrightSkins klasörü oluþturuldu: {wrightSkinsPath}", "INFO");
            }
            
            dynamic data;
            if (File.Exists(dataPath))
            {
                string existingJson = File.ReadAllText(dataPath);
                data = string.IsNullOrWhiteSpace(existingJson) 
                    ? new { } 
                    : Newtonsoft.Json.JsonConvert.DeserializeObject(existingJson);
            }
            else
            {
                data = new { };
            }
            
            var updatedData = new 
            {
                firstSettingsDone = true,
                completedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(updatedData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(dataPath, jsonContent);
            
            DebugConsoleWindow.Instance.WriteLine($"Ýlk ayarlar tamamlandý flag'i kaydedildi: {dataPath}", "SUCCESS");
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"data.json oluþturulurken hata: {ex.Message}", "ERROR");
        }
    }

    private async void LoadLanguageComboBoxSetting()
    {
        try
        {
            var languageSettings = LanguageSettingsService.Instance.LoadLanguageSettings();
            var languageComboBox = FindName("LanguageComboBox") as ComboBox;
            
            if (languageComboBox != null)
            {
                languageComboBox.Items.Clear();
                
                LoadAvailableLanguages(languageComboBox, languageSettings.LanguageCode);
                
                await LoadSkinNamesForLanguage(languageSettings.LanguageCode);
                
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }
    
    private void LoadAvailableLanguages(ComboBox languageComboBox, string currentLanguageCode)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("WrightLauncher.Assets.Lang.") && name.EndsWith(".json"))
                .ToArray();

foreach (var resourceName in resourceNames)
            {
                try
                {
                    var languageCode = resourceName.Replace("WrightLauncher.Assets.Lang.", "").Replace(".json", "");
                    
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        var parsed = Newtonsoft.Json.Linq.JToken.Parse(json);
                        
                        if (parsed is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                        {
                            var metadata = array[0] as Newtonsoft.Json.Linq.JObject;
                            if (metadata != null)
                            {
                                var langCode = metadata["langCode"]?.ToString();
                                var langName = metadata["lang"]?.ToString();
                                
                                if (!string.IsNullOrEmpty(langCode) && !string.IsNullOrEmpty(langName))
                                {
                                    var comboBoxItem = new ComboBoxItem
                                    {
                                        Content = langName,
                                        Tag = langCode,
                                        Style = (Style)FindResource("ModernComboBoxItem")
                                    };
                                    
                                    if (langCode == currentLanguageCode)
                                    {
                                        comboBoxItem.IsSelected = true;
                                    }
                                    
                                    languageComboBox.Items.Add(comboBoxItem);
                                }
                            }
                        }
                    }
                }
                catch (Exception fileEx)
                {
                }
            }
            
            if (languageComboBox.Items.Count == 0)
            {
                AddFallbackLanguages(languageComboBox, currentLanguageCode);
            }
        }
        catch (Exception ex)
        {
            AddFallbackLanguages(languageComboBox, currentLanguageCode);
        }
    }

    private void AddFallbackLanguages(ComboBox languageComboBox, string currentLanguageCode)
    {
        try
        {
            var englishItem = new ComboBoxItem
            {
                Content = "English",
                Tag = "en_US",
                Style = (Style)FindResource("ModernComboBoxItem"),
                IsSelected = currentLanguageCode == "en_US"
            };
            languageComboBox.Items.Add(englishItem);
            
            var turkishItem = new ComboBoxItem
            {
                Content = "Türkçe",
                Tag = "tr_TR",
                Style = (Style)FindResource("ModernComboBoxItem"),
                IsSelected = currentLanguageCode == "tr_TR"
            };
            languageComboBox.Items.Add(turkishItem);
            
        }
        catch (Exception ex)
        {
        }
    }
    
    private async Task LoadSkinNamesForLanguage(string languageCode)
    {
        try
        {
            
            var skinNamesUrl = await GetSkinNamesUrlForLanguage(languageCode);
            if (string.IsNullOrEmpty(skinNamesUrl))
            {
                return;
            }

var response = await _skinDataHttpClient.GetStringAsync(skinNamesUrl);
            
            var championData = JsonConvert.DeserializeObject<WrightLauncher.Models.ChampionDataResponse>(response);
            
            if (championData?.Data != null)
            {
                var skinMapping = new Dictionary<string, string>();
                
                foreach (var champion in championData.Data.Values)
                {
                    if (champion.Skins != null)
                    {
                        foreach (var skin in champion.Skins)
                        {
                            if (!string.IsNullOrEmpty(skin.Name) && skin.Name != "default")
                            {
                                var key = skin.Id;
                                skinMapping[key] = skin.Name;
                            }
                        }
                    }
                }
                
                _championSkinMappings[languageCode] = skinMapping;
            }
        }
        catch (Exception ex)
        {
        }
    }
    
    private async Task<string> GetSkinNamesUrlForLanguage(string languageCode)
    {
        try
        {
            
            var resourceName = $"WrightLauncher.Assets.Lang.{languageCode}.json";
            
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }
                
                using (var reader = new StreamReader(stream))
                {
                    var jsonContent = await reader.ReadToEndAsync();
                    
                    var langArray = JsonConvert.DeserializeObject<JArray>(jsonContent);
                    
                    if (langArray?.Count > 0 && langArray[0] is JObject metadata)
                    {
                        var skinNamesUrl = metadata["skinNames"]?.ToString();
                        return skinNamesUrl;
                    }
                    else
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
        
        return null;
    }
    
    private string GetOriginalSkinName(string championId, int skinNum)
    {
        return $"{championId}_skin_{skinNum}";
    }
    
    private string GetTranslatedSkinName(string originalSkinName, string languageCode)
    {
        try
        {
            if (_championSkinMappings.ContainsKey(languageCode) && 
                _championSkinMappings[languageCode].ContainsKey(originalSkinName))
            {
                return _championSkinMappings[languageCode][originalSkinName];
            }
        }
        catch (Exception ex)
        {
        }
        
        return originalSkinName;
    }
    
    private string GetLocalizedSkinName(string championKey, int skinId, string originalSkinName, string languageCode)
    {
        try
        {
            var mappingKey = skinId.ToString();
            
            if (_championSkinMappings.ContainsKey(languageCode) && 
                _championSkinMappings[languageCode].ContainsKey(mappingKey))
            {
                var translatedName = _championSkinMappings[languageCode][mappingKey];
                return translatedName;
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
        
        return originalSkinName;
    }
    
    private bool _soundEffectsEnabled = false;
    private bool _soundSettingsLoaded = false;
    private bool _autoLoadEnabled = true;
    public bool SoundEffectsEnabled 
    { 
        get => _soundEffectsEnabled; 
        set 
        { 
            if (_soundEffectsEnabled != value && _soundSettingsLoaded)
            {
                _soundEffectsEnabled = value;
                SaveSoundSettings();
            }
            else if (!_soundSettingsLoaded)
            {
                _soundEffectsEnabled = value;
            }
        } 
    }
    private DiscordUser? _currentDiscordUser = null;
    private User? _currentUser = null;
    
    public bool IsDiscordConnected => _currentDiscordUser != null;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private LobbyInvitationPopup? _lobbyPopup = null;
    private AppSettings? _appSettings = null;
    
    private string _selectedFantomeFilePath = string.Empty;
    private readonly List<string> _invalidCharacters = new() { ":", "/", "\\", "<", ">", "\"", "|", "?", "*", "&" };
    
    private FileSystemWatcher? _wrightProfileWatcher = null;
    
    public MainWindow()
    {
        try
        {
            DebugConsoleWindow.Instance.WriteLine("=== WrightLauncher DEBUG CONSOLE BAÞLATILDI ===", "SUCCESS");
            DebugConsoleWindow.Instance.WriteLine("MainWindow constructor baþladý", "INFO");
            
            if (ShouldShowFirstUseModal())
            {
                ShowFirstUseModal();
            }
            
            LoadAndApplyLanguageSettings();
            
            InitializeComponent();
            DebugConsoleWindow.Instance.WriteLine("InitializeComponent tamamlandý", "INFO");

            if (Application.Current != null)
            {
                Application.Current.MainWindow = this;
                DebugConsoleWindow.Instance.WriteLine("MainWindow Application.Current.MainWindow olarak set edildi", "INFO");
            }

            PerformanceOptimizationService.OptimizeWindow(this);
            DebugConsoleWindow.Instance.WriteLine("Performance optimizations uygulandý", "SUCCESS");

            InitializeNotificationManager();

            PerformanceOptimizationService.OptimizeWindow(this);
            DebugConsoleWindow.Instance.WriteLine("Performance optimizations uygulandý", "SUCCESS");

            InitializeNotificationManager();

            _discordService = new DiscordService();
            _discordService.UserAuthenticated += OnDiscordUserAuthenticated;
            _discordService.AuthenticationFailed += OnDiscordAuthenticationFailed;
            DebugConsoleWindow.Instance.WriteLine("DiscordService oluþturuldu", "INFO");
            
            DebugConsoleWindow.Instance.WriteLine("UguuUploadService oluþturuldu", "INFO");
            
            _socketIOService = new SocketIORealtimeService();
            
            _socketIOService.GetCurrentLobbyCode = () => {
                var viewModel = DataContext as ViewModels.MainViewModel;
                return viewModel?.CurrentLobby?.LobbyCode ?? "WRIGHT-UNKNOWN";
            };
            
            _socketIOService.UserJoined += OnUserJoined;
            _socketIOService.UserLeft += OnUserLeft;
            _socketIOService.SkinAdded += OnSkinAdded;
            _socketIOService.SkinRemoved += OnSkinRemoved;
            _socketIOService.FileRequest += OnFileRequest;
            _socketIOService.FilesReceived += OnFilesReceived;
            _socketIOService.ExistingSkinFileReceived += OnExistingSkinFileReceived;
            _socketIOService.FriendStatusChanged += OnFriendStatusChanged;
            _socketIOService.FriendAdded += OnFriendAdded;
            _socketIOService.FriendRemoved += OnFriendRemoved;
            _socketIOService.FriendsListUpdateRequired += OnFriendsListUpdateRequired;
            
            _socketIOService.FriendRequestReceived += OnFriendRequestReceived;
            _socketIOService.FriendRequestSent += OnFriendRequestSent;
            _socketIOService.FriendRequestAccepted += OnFriendRequestAccepted;
            _socketIOService.FriendRequestDeclined += OnFriendRequestDeclined;
            
            _socketIOService.LobbyInviteReceived += OnLobbyInviteReceived;
            _socketIOService.LobbyInviteSent += OnLobbyInviteSent;
            _socketIOService.LobbyInviteAccepted += OnLobbyInviteAccepted;
            _socketIOService.LobbyInviteDeclined += OnLobbyInviteDeclined;
            _socketIOService.LobbyInviteError += OnLobbyInviteError;
            
            _socketIOService.LobbyCreated += OnLobbyCreated;
            _socketIOService.LobbyJoined += OnLobbyJoined;
            _socketIOService.LobbyLeft += OnLobbyLeft;
            _socketIOService.LobbyDisbanded += OnLobbyDisbanded;
            _socketIOService.LobbyError += OnLobbyError;
            _socketIOService.SkinAdded += OnLobbySkinAdded;
            _socketIOService.SkinRemoved += OnLobbySkinRemoved;
            _socketIOService.LobbyMembersUpdated += OnLobbyMembersUpdated;
            _socketIOService.UserLeftLobby += OnUserLeftLobby;
            _socketIOService.NewModsInLobbyDetected += OnNewModsInLobbyDetected;
            _socketIOService.SkinUploadUIUpdate += OnSkinUploadUIUpdate;
            DebugConsoleWindow.Instance.WriteLine("SocketIORealtimeService oluþturuldu", "INFO");
            
            var dataService = new DataService();
            DebugConsoleWindow.Instance.WriteLine("DataService oluþturuldu", "INFO");
            var viewModel = new MainViewModel(dataService);
            DebugConsoleWindow.Instance.WriteLine("MainViewModel oluþturuldu", "INFO");
            
            viewModel.AppVersion = APP_VERSION;
            
            DataContext = viewModel;
            DebugConsoleWindow.Instance.WriteLine("DataContext ayarlandý", "SUCCESS");

            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            ShowPage("Home");

            if (DataContext is MainViewModel vm)
            {
                vm.NavigateCommand.Execute("Home");
            }

            SizeChanged += OnWindowSizeChanged;
            
            Closing += MainWindow_Closing;

            StateChanged += OnWindowStateChanged;
            
            Loaded += (s, e) => {
                UpdateSelectedSkinsCount();
                _ = LoadDownloadServerSetting();
                _ = LoadGamePathToUI();
                
                OnWindowStateChanged(this, EventArgs.Empty);
                
                InitializeWrightProfileWatcher();
                
                LoadAutoLoadSettings();
                
                LoadLanguageComboBoxSetting();
                
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    await TestStaffSystemAsync();
                });
                
                _isInitializing = false;
            };
            
            WrightLauncher.Converters.ChampionImageConverter.ImageCacheUpdated += OnChampionImageCacheUpdated;
            WrightLauncher.Converters.ChampionLoadingImageConverter.ImageCacheUpdated += OnChampionImageCacheUpdated;
            
            MainViewModel.WrightProfileUpdated += RefreshInjectButton;

            UpdateDiscordConnectionUI();
            
            _ = TryHashTokenAutoLoginAsync();
            
            _ = LoadPerformanceSettingsAsync();
            
            InitializeSlideshow();
            
            this.Closing += MainWindow_Closing;
            
            DebugConsoleWindow.Instance.WriteLine("MainWindow constructor tamamlandý", "SUCCESS");
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"MainWindow constructor hatasý: {ex.Message}", "ERROR");
            DebugConsoleWindow.Instance.WriteLine($"Stack trace: {ex.StackTrace}", "ERROR");
            throw;
        }
    }

    private void InitializeWrightProfileWatcher()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var profilePath = System.IO.Path.Combine(wrightPath, "Wright.profile");
            
            Directory.CreateDirectory(wrightPath);
            
            if (!File.Exists(profilePath))
            {
                File.WriteAllText(profilePath, "");
            }
            
            _wrightProfileWatcher = new FileSystemWatcher
            {
                Path = wrightPath,
                Filter = "Wright.profile",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            
            _wrightProfileWatcher.Changed += OnWrightProfileChanged;
            _wrightProfileWatcher.Error += OnWrightProfileWatcherError;
            
        }
        catch (Exception ex)
        {
        }
    }
    
    private void OnWrightProfileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                UpdateSelectedSkinsCount();
            }
            catch (Exception ex)
            {
            }
        });
    }
    
    private void OnWrightProfileWatcherError(object sender, ErrorEventArgs e)
    {
    }

    private async Task TestStaffSystemAsync(string? discordId = null)
    {
        try
        {
            
            var currentDiscordId = discordId ?? _currentDiscordUser?.Id;
            string testDiscordId = !string.IsNullOrEmpty(currentDiscordId) ? currentDiscordId : " ";
            
            var staffCheck = await StaffService.CheckStaffAsync(testDiscordId);
            if (staffCheck.Success && staffCheck.IsStaff)
            {
                var staff = staffCheck.StaffInfo;
            }
            else
            {
            }
            
            var permissionCheck = await StaffService.CheckPermissionAsync(testDiscordId, "all");
            if (permissionCheck.Success)
            {
            }
            
            if (!string.IsNullOrEmpty(currentDiscordId))
            {
                var isCurrentUserStaff = await StaffService.CheckStaffAsync(currentDiscordId);
                if (isCurrentUserStaff.Success && isCurrentUserStaff.IsStaff)
                {
                    var currentStaffInfo = isCurrentUserStaff.StaffInfo;
                    if (currentStaffInfo != null)
                    {
                    }
                }
                else
                {
                }
            }
            else
            {
                var source = !string.IsNullOrEmpty(discordId) ? "hash token" : "Discord user object";
            }
            
        }
        catch (Exception ex)
        {
        }
    }

    private void PlayUIClickSound()
    {
        if (!SoundEffectsEnabled) return;
        
        try
        {
            DebugConsoleWindow.Instance.WriteLine($"?? Playing UI Click sound from embedded resource...", "INFO");
            
            var resourceUri = new Uri("pack://application:,,,/Assets/Sounds/ui-click.wav");
            var player = new SoundPlayer();
            
            var streamInfo = Application.GetResourceStream(resourceUri);
            if (streamInfo != null)
            {
                player.Stream = streamInfo.Stream;
                player.Play();
                DebugConsoleWindow.Instance.WriteLine($"? UI Click sound played successfully!", "SUCCESS");
            }
            else
            {
                DebugConsoleWindow.Instance.WriteLine($"? UI Click sound resource not found in embedded resources", "ERROR");
            }
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"?? UI Click sound error: {ex.Message}", "ERROR");
        }
    }

    private void PlaySkinHoverSound()
    {
        if (!SoundEffectsEnabled) return;
        
        try
        {
            DebugConsoleWindow.Instance.WriteLine($"?? Playing Skin Hover sound from embedded resource...", "INFO");
            
            var resourceUri = new Uri("pack://application:,,,/Assets/Sounds/skin-hover.wav");
            var player = new SoundPlayer();
            
            var streamInfo = Application.GetResourceStream(resourceUri);
            if (streamInfo != null)
            {
                player.Stream = streamInfo.Stream;
                player.Play();
                DebugConsoleWindow.Instance.WriteLine($"? Skin Hover sound played successfully!", "SUCCESS");
            }
            else
            {
                DebugConsoleWindow.Instance.WriteLine($"? Skin Hover sound resource not found in embedded resources", "ERROR");
            }
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"?? Skin hover sound error: {ex.Message}", "ERROR");
        }
    }

    private void OnSkinCardMouseEnter(object sender, MouseEventArgs e)
    {
        PlaySkinHoverSound();
    }

    private void OnSkinCardMouseLeave(object sender, MouseEventArgs e)
    {
    }

    private void LoadSoundSettings()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "sound.json");
            
            if (File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
                
                if (config != null && config.ContainsKey("soundEffectsEnabled"))
                {
                    if (config["soundEffectsEnabled"] is JsonElement element && element.ValueKind == JsonValueKind.True)
                    {
                        _soundEffectsEnabled = true;
                    }
                    else if (config["soundEffectsEnabled"] is JsonElement element2 && element2.ValueKind == JsonValueKind.False)
                    {
                        _soundEffectsEnabled = false;
                    }
                }
                else
                {
                    _soundEffectsEnabled = false;
                    SaveSoundSettings();
                }
            }
            else
            {
                _soundEffectsEnabled = false;
                var directory = System.IO.Path.GetDirectoryName(configPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                SaveSoundSettings();
            }
            
            var soundEffectsCheckBox = FindName("SoundEffectsCheckBox") as CheckBox;
            if (soundEffectsCheckBox != null)
            {
                soundEffectsCheckBox.IsChecked = _soundEffectsEnabled;
                DebugConsoleWindow.Instance.WriteLine($"??? Checkbox güncellendi: {_soundEffectsEnabled}", "INFO");
            }
            else
            {
                DebugConsoleWindow.Instance.WriteLine("?? SoundEffectsCheckBox bulunamadý!", "WARNING");
            }
            
            DebugConsoleWindow.Instance.WriteLine($"?? Ses ayarlarý yüklendi: {(_soundEffectsEnabled ? "Açýk" : "Kapalý")}", "INFO");
            
            _soundSettingsLoaded = true;
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"? Ses ayarlarý yüklenirken hata: {ex.Message}", "ERROR");
            _soundEffectsEnabled = false;
            _soundSettingsLoaded = true;
        }
    }

    private void SaveSoundSettings()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "sound.json");
            
            Dictionary<string, object> config;
            
            if (File.Exists(configPath))
            {
                var configJson = File.ReadAllText(configPath);
                config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(configJson) ?? new Dictionary<string, object>();
            }
            else
            {
                config = new Dictionary<string, object>();
            }
            
            config["soundEffectsEnabled"] = _soundEffectsEnabled;
            
            var jsonString = System.Text.Json.JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, jsonString);
            
            DebugConsoleWindow.Instance.WriteLine($"?? Ses ayarlarý kaydedildi: {(_soundEffectsEnabled ? "Açýk" : "Kapalý")}", "SUCCESS");
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"? Ses ayarlarý kaydedilirken hata: {ex.Message}", "ERROR");
        }
    }

    private void SoundEffectsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        SoundEffectsEnabled = true;
        DebugConsoleWindow.Instance.WriteLine("?? Ses efektleri açýldý", "INFO");
    }

    private void SoundEffectsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        SoundEffectsEnabled = false;
        DebugConsoleWindow.Instance.WriteLine("?? Ses efektleri kapatýldý", "INFO");
    }

    private void MinimizeWindow(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeWindow(object sender, RoutedEventArgs e)
    {
        
        if (WindowState == WindowState.Maximized || IsWindowMaximized())
        {
            RestoreWindow();
        }
        else
        {
            MaximizeToWorkingArea();
        }
    }

    private bool IsWindowMaximized()
    {
        var workArea = SystemParameters.WorkArea;
        return Math.Abs(Left - workArea.Left) < 10 && 
               Math.Abs(Top - workArea.Top) < 10 && 
               Math.Abs(Width - workArea.Width) < 10 && 
               Math.Abs(Height - workArea.Height) < 10;
    }

    private void MaximizeToWorkingArea()
    {
        if (WindowState == WindowState.Normal)
        {
            _normalLeft = Left;
            _normalTop = Top;
            _normalWidth = Width;
            _normalHeight = Height;
        }

        var workArea = SystemParameters.WorkArea;

WindowState = WindowState.Normal;
        Left = workArea.Left;
        Top = workArea.Top;
        Width = workArea.Width;
        Height = workArea.Height;
        
        if (MaximizeButton != null)
        {
            MaximizeButton.Content = "?";
        }
        
    }

    private double _normalLeft, _normalTop, _normalWidth, _normalHeight;

    private void RestoreWindow()
    {
        if (_normalWidth > 0 && _normalHeight > 0)
        {
            Left = _normalLeft;
            Top = _normalTop;
            Width = _normalWidth;
            Height = _normalHeight;
        }
        else
        {
            Width = 1415;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        WindowState = WindowState.Normal;
        
        if (MaximizeButton != null)
        {
            MaximizeButton.Content = "?";
        }
        
    }

    private void OnWindowStateChanged(object sender, EventArgs e)
    {
        
        if (WindowState == WindowState.Maximized)
        {
            
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                MaximizeToWorkingArea();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            
            if (MaximizeButton != null)
            {
                MaximizeButton.Content = "?";
            }
        }
        else if (WindowState == WindowState.Normal)
        {
            
            if (IsWindowMaximized())
            {
                if (MaximizeButton != null)
                {
                    MaximizeButton.Content = "?";
                }
            }
            else
            {
                if (MaximizeButton != null)
                {
                    MaximizeButton.Content = "?";
                }
            }
        }
    }

    private void CloseWindow(object sender, RoutedEventArgs e)
    {
        CleanupWebView();
        Application.Current.Shutdown();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            MaximizeWindow(sender, e);
        }
        else if (e.ClickCount == 1 && !IsWindowMaximized())
        {
            DragMove();
        }
    }

    private void ClosePreview(object sender, RoutedEventArgs e)
    {
        CleanupWebView(() => {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(250),
                AccelerationRatio = 0.3,
                DecelerationRatio = 0.3
            };

            fadeOut.Completed += (s, args) =>
            {
                PreviewModal.Visibility = Visibility.Collapsed;
                PreviewModal.Opacity = 1.0;
            };

            PreviewModal.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        });
    }

    public void ShowPreview()
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedSkin != null)
        {
            _currentPreviewSkin = viewModel.SelectedSkin;
        }
        
        CleanupWebView();
        
        PreviewModal.Opacity = 0.0;
        PreviewModal.Visibility = Visibility.Visible;
        
        _ = UpdatePreviewModalRoleAsync();
        
        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200),
            AccelerationRatio = 0.3,
            DecelerationRatio = 0.3
        };

        PreviewModal.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        
        CreateYouTubeWebView();
    }

    private void CreateYouTubeWebView()
    {
        try
        {
            var container = YouTubeContainer;
            if (container == null) return;

            container.Children.Clear();

            if (DataContext is MainViewModel viewModel && viewModel.SelectedSkin != null)
            {
                var skin = viewModel.SelectedSkin;
                
                if (!string.IsNullOrEmpty(skin.YoutubePreview))
                {
                    var webView = new WebView2();
                    
                    try
                    {
                        webView.Source = new Uri(skin.YoutubePreview);
                    }
                    catch (Exception ex)
                    {
                        var errorBlock = new TextBlock
                        {
                            Text = string.Format(LocalizationService.Instance.Translate("youtube_url_invalid"), ex.Message),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 14,
                            Foreground = System.Windows.Media.Brushes.Yellow,
                            TextWrapping = TextWrapping.Wrap
                        };
                        container.Children.Add(errorBlock);
                        return;
                    }

                    container.Children.Add(webView);
                    
                    container.Opacity = 0.0;
                    var containerFadeIn = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        AccelerationRatio = 0.3,
                        DecelerationRatio = 0.3
                    };
                    
                    container.BeginAnimation(UIElement.OpacityProperty, containerFadeIn);
                }
                else
                {
                    var noYouTubeBlock = new TextBlock
                    {
                        Text = "YouTube Preview Yok",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = System.Windows.Media.Brushes.White
                    };
                    container.Children.Add(noYouTubeBlock);
                }
            }
        }
        catch
        {
        }
    }

    private void CleanupWebView(Action? onCompleted = null)
    {
        try
        {
            var container = YouTubeContainer;
            if (container != null)
            {
                if (container.Children.Count > 0)
                {
                    
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        AccelerationRatio = 0.3,
                        DecelerationRatio = 0.3
                    };

                    fadeOut.Completed += (s, args) =>
                    {
                        for (int i = container.Children.Count - 1; i >= 0; i--)
                        {
                            if (container.Children[i] is WebView2 webView)
                            {
                                try
                                {
                                    if (webView.CoreWebView2 != null)
                                    {
                                        webView.CoreWebView2.Stop();
                                    }
                                    
                                    webView.NavigateToString("about:blank");
                                    webView.Dispose();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                        
                        container.Children.Clear();
                        container.Opacity = 1.0;
                        
                        onCompleted?.Invoke();
                    };

                    container.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
                else
                {
                    container.Opacity = 1.0;
                    onCompleted?.Invoke();
                }
            }
            else
            {
                onCompleted?.Invoke();
            }
        }
        catch (Exception ex)
        {
            onCompleted?.Invoke();
        }
    }

    private void OnSkinCardClick(object sender, MouseButtonEventArgs e)
    {
        PlayUIClickSound();
        
        if (sender is FrameworkElement element && element.DataContext is WrightLauncher.Models.Skin clickedSkin)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SelectedSkin = clickedSkin;
                
                if (viewModel.SelectedMenuItem == "Champions")
                {
                    viewModel.SelectSkinCommand.Execute(clickedSkin);
                }
                else
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.IsDownloadingFromSpecialPage = false;
                    }
                    ShowPreview();
                }
            }
        }
    }

    private async void OpenVideoPreview(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedSkin?.VideoPreview != null)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = viewModel.SelectedSkin.VideoPreview,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                await ShowErrorModalAsync(
                    LocalizationService.Instance.Translate("VideoPreviewError"), 
                    string.Format(LocalizationService.Instance.Translate("VideoPreviewErrorMessage"), ex.Message)
                );
            }
        }
    }

    private void YouTubeWebView_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private void YouTubeWebView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is WebView2 webView)
        {
            try
            {
                webView.NavigateToString("about:blank");
            }
            catch (Exception ex)
            {
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugConsoleWindow.Instance.Show();
            
            LoadSoundSettings();
            DebugConsoleWindow.Instance.WriteLine("Window_Loaded: Ses ayarlarý yüklendi", "SUCCESS");
            
            if (_currentDiscordUser != null)
            {
                _ = Task.Run(async () => 
                {
                    await CheckAndCacheGuildMembership(_currentDiscordUser.Id);
                });
            }

Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                catch (Exception testEx)
                {
                }
            }).ConfigureAwait(false);
            
            _ = PerformAutoUpdateCheckAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugConsoleWindow.Instance.WriteLine($"Window_Loaded hatasý: {ex.Message}", "ERROR");
        }
    }

    private void OnHomeClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        ShowPage("Home");
        
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.NavigateCommand.Execute("Home");
        }
    }

    private void OnChampionsClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        ShowPage("Champions");
    }

    private void OnSkinsClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        ShowPage("Skins");
    }

    private bool? _isInWrightGuild = null;
    private DateTime _lastGuildCheck = DateTime.MinValue;
    
    public bool IsInWrightGuild => _isInWrightGuild == true;
    
    private void OnLobbyClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        if (_currentDiscordUser == null)
        {
            ShowWrightAdvModal();
            return;
        }
        
        if (_isInWrightGuild == true)
        {
            ShowPage("Lobby");
        }
        else
        {
            ShowWrightAdvModal();
        }
    }

public async Task CheckAndCacheGuildMembership(string discordId)
    {
        try
        {
            
            var isInGuild = await CheckDiscordGuildMembership(discordId);
            _isInWrightGuild = isInGuild;
            _lastGuildCheck = DateTime.Now;
            
            string status = isInGuild ? "Wright sunucusunda ?" : "Wright sunucusunda deðil ?";
        }
        catch (Exception ex)
        {
            _isInWrightGuild = false;
        }
    }
    
    private async Task<bool> CheckDiscordGuildMembership(string discordId)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var apiUrl = $"{WrightUtils.F}?discord_id={discordId}";
                
                var response = await httpClient.GetAsync(apiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    
                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    
                    bool exists = result?.exists ?? false;
                    
                    return exists;
                }
                else
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    
    public async Task<bool> CheckGuildMembershipAsync(string discordId)
    {
        return await CheckDiscordGuildMembership(discordId);
    }
    
    public void OpenLobbyPage()
    {
        try
        {
            PlayUIClickSound();
            ShowPage("Lobby");
        }
        catch (Exception ex)
        {
        }
    }
    
    private void ShowWrightAdvModal()
    {
        try
        {
            var wrightAdvModal = new WrightAdvModal(_currentDiscordUser);
            wrightAdvModal.Owner = this;
            wrightAdvModal.Show();
        }
        catch (Exception ex)
        {
        }
    }

    private void OnDirectoryClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        ShowPage("Directory");
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        ShowPage("Settings");
    }

    private async void OnSpecialClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        if (_currentUser == null)
        {
            await ShowWarningModalAsync("Giriþ yapmanýz gerekiyor!", "Eriþim Reddedildi");
            return;
        }
        
        ShowPage("Special");
        await LoadSpecialSkins();
    }

    private void OnSpecialSkinCardClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            if (sender is Border border && border.DataContext is SpecialSkin specialSkin)
            {
                
                ShowSpecialSkinPreview(specialSkin);
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void ShowSpecialSkinPreview(SpecialSkin specialSkin)
    {
        try
        {
            
            if (DataContext is MainViewModel viewModel)
            {
                string downloadUrl = specialSkin.ActualDownloadUrl;
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    downloadUrl = "SPECIAL_SKIN_NO_DOWNLOAD";
                }
                else
                {
                }
                
                var tempSkin = new Skin
                {
                    Id = int.TryParse(specialSkin.Id, out int id) ? id : 0,
                    Name = specialSkin.Name,
                    Champion = specialSkin.Champion ?? "Special Champion",
                    Author = specialSkin.Author ?? "Special Creator",
                    Description = specialSkin.Description ?? "Özel tasarlanmýþ premium skin",
                    Version = specialSkin.Version ?? "1.0",
                    ImageCard = specialSkin.ImageCard,
                    ImagePreview = !string.IsNullOrEmpty(specialSkin.ImagePreview) ? specialSkin.ImagePreview : specialSkin.ImageCard,
                    CachedImageCard = specialSkin.ImageCard,
                    CachedImagePreview = !string.IsNullOrEmpty(specialSkin.ImagePreview) ? specialSkin.ImagePreview : specialSkin.ImageCard,
                    FileURL = downloadUrl,
                    Tags = new List<string> { "Special", "Premium" },
                    DiscordUser = new DiscordUser
                    {
                        Id = "special_" + specialSkin.Id,
                        Username = specialSkin.Author ?? "special_creator",
                        GlobalName = specialSkin.Author ?? "Special Creator",
                        Avatar = new DiscordAvatar
                        {
                            Id = "",
                            Link = "",
                            IsAnimated = false
                        }
                    }
                };
                
                viewModel.SelectedSkin = tempSkin;
                
                viewModel.IsDownloadingFromSpecialPage = true;
                
                ShowPreview();
            }
        }
        catch (Exception ex)
        {
        }
    }

    public void OpenSettingsPage()
    {
        PlayUIClickSound();
        ShowPage("Settings");
    }

    private async void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }
        
        try
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string? languageCode = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(languageCode))
                {
                    
                    string languageName = GetLanguageNameFromMetadata(languageCode);
                    
                    LanguageSettingsService.Instance.SaveLanguageSettings(languageCode, languageName);
                    
                    LocalizationService.Instance.Load(languageCode);
                    
                    await LoadSkinNamesForLanguage(languageCode);
                    
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private string GetLanguageNameFromMetadata(string languageCode)
    {
        try
        {
            var path = $"Assets/Lang/{languageCode}.json";
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = Newtonsoft.Json.Linq.JToken.Parse(json);
                
                if (parsed is Newtonsoft.Json.Linq.JArray array && array.Count > 0)
                {
                    var metadata = array[0] as Newtonsoft.Json.Linq.JObject;
                    if (metadata != null && metadata["lang"] != null)
                    {
                        var langName = metadata["lang"]?.ToString();
                        if (!string.IsNullOrEmpty(langName))
                        {
                            return langName;
                        }
                    }
                }
            }
            
            return languageCode switch
            {
                "tr_TR" => "Türkçe",
                "en_US" => "English", 
                _ => "English"
            };
        }
        catch (Exception ex)
        {
            return "English";
        }
    }

    private void RestartApplication()
    {
        try
        {
            
            System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
            
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
        }
    }

    private void RefreshUI()
    {
        try
        {
            
            InvalidateVisual();
            UpdateLayout();
            
        }
        catch (Exception ex)
        {
        }
    }

    private async void ShowPage(string pageName)
    {
        SkinsPage.Visibility = Visibility.Collapsed;
        DirectoryPage.Visibility = Visibility.Collapsed;
        LobbyPage.Visibility = Visibility.Collapsed;
        ChampionsPage.Visibility = Visibility.Collapsed;
        HomePage.Visibility = Visibility.Collapsed;
        HomePageContent.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        SpecialPage.Visibility = Visibility.Collapsed;

        switch (pageName)
        {
            case "Home":
                HomePageContent.Visibility = Visibility.Visible;
                
                HomePageContent.Opacity = 0;
                var homeAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                HomePageContent.BeginAnimation(OpacityProperty, homeAnimation);
                break;
                
            case "Champions":
                ChampionsPage.Visibility = Visibility.Visible;
                
                ChampionsPage.Opacity = 0;
                var championsAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                ChampionsPage.BeginAnimation(OpacityProperty, championsAnimation);
                
                await CheckDownloadServerSetting();
                break;
                
            case "Skins":
                SkinsPage.Visibility = Visibility.Visible;
                
                SkinsPage.Opacity = 0;
                var skinsAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                SkinsPage.BeginAnimation(OpacityProperty, skinsAnimation);
                
                if (DataContext is MainViewModel skinsViewModel)
                {
                    skinsViewModel.SearchText = "";
                }
                PopulateTagsDropdown();
                break;
                
            case "Directory":
                DirectoryPage.Visibility = Visibility.Visible;
                
                DirectoryPage.Opacity = 0;
                var directoryAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                DirectoryPage.BeginAnimation(OpacityProperty, directoryAnimation);
                break;
                
            case "Lobby":
                LobbyPage.Visibility = Visibility.Visible;
                
                LobbyPage.Opacity = 0;
                var lobbyAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                LobbyPage.BeginAnimation(OpacityProperty, lobbyAnimation);
                
                if (_currentUser != null && _friendsUpdateTimer == null)
                {
                    StartFriendsUpdates();
                }
                break;
                
            case "Settings":
                SettingsPage.Visibility = Visibility.Visible;
                
                SettingsPage.Opacity = 0;
                var settingsAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                SettingsPage.BeginAnimation(OpacityProperty, settingsAnimation);
                break;
                
            case "Special":
                SpecialPage.Visibility = Visibility.Visible;
                
                SpecialPage.Opacity = 0;
                var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                SpecialPage.BeginAnimation(OpacityProperty, fadeInAnimation);
                
                await LoadSpecialSkins();
                break;
        }

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedMenuItem = pageName;
        }
    }

    private async Task<string> ConvertGitHubUrlToWrightSkinsUrl(string gitHubUrl, string championName, string skinName)
    {
        try
        {
            
            if (string.IsNullOrEmpty(championName))
            {
                var urlParts = gitHubUrl.Split('/');
                var skinsIndex = Array.IndexOf(urlParts, "skins");
                if (skinsIndex >= 0 && skinsIndex + 1 < urlParts.Length)
                {
                    championName = Uri.UnescapeDataString(urlParts[skinsIndex + 1]);
                }
                else
                {
                    var words = skinName.Split(' ');
                    championName = words.LastOrDefault() ?? "Unknown";
                }
            }
            
            var wrightSkinsCodename = GetChampionCodename(championName);
            if (wrightSkinsCodename != championName)
            {
            }
            
            var cleanSkinName = skinName;
            cleanSkinName = cleanSkinName.Replace("/", " ");
            cleanSkinName = cleanSkinName.Replace(":", "");
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (c != ':' && c != '/')
                {
                    cleanSkinName = cleanSkinName.Replace(c, '_');
                }
            }

var wrightSkinsUrl = $"{WrightUtils.E}/{Uri.EscapeDataString(championName)}/{Uri.EscapeDataString(cleanSkinName)}/WAD/";
            
            return wrightSkinsUrl;
        }
        catch (Exception ex)
        {
            return gitHubUrl;
        }
    }

    private async Task DownloadFolderFromWrightSkinsForSkinPage(string baseUrl, string localFolderPath, string skinName, MainViewModel viewModel, string championCodename)
    {
        try
        {
            
            var wadFolderPath = System.IO.Path.Combine(localFolderPath, "WAD");
            Directory.CreateDirectory(wadFolderPath);
            
            using (var httpClient = new HttpClient())
            {

var skinFiles = new[]
                {
                    $"{championCodename}.wad.client",
                    $"{championCodename}.wad"
                };
                
                bool fileFound = false;
                
                foreach (var fileName in skinFiles)
                {
                    try
                    {
                        var fileUrl = baseUrl + fileName;
                        
                        var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            
                            var localFilePath = System.IO.Path.Combine(wadFolderPath, fileName);
                            var totalBytes = response.Content.Headers.ContentLength ?? 0;
                            var downloadedBytes = 0L;
                            
                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = File.Create(localFilePath))
                            {
                                var buffer = new byte[8192];
                                int bytesRead;
                                
                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    downloadedBytes += bytesRead;
                                    
                                    if (totalBytes > 0)
                                    {
                                        var percentage = (int)((downloadedBytes * 100) / totalBytes);
                                        viewModel.DownloadButtonText = string.Format(
                                            LocalizationService.Instance.Translate("DownloadProgress"), 
                                            percentage
                                        );
                                    }
                                }
                            }
                            
                            fileFound = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                
                if (!fileFound)
                {
                    try
                    {
                        var indexResponse = await httpClient.GetStringAsync(baseUrl);
                        
                        var wadFilePattern = @"href=[""']([^""']*\.wad(?:\.client)?)[""']";
                        var matches = System.Text.RegularExpressions.Regex.Matches(indexResponse, wadFilePattern, 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (matches.Count > 0)
                        {
                            var foundFile = matches[0].Groups[1].Value;
                            var fileUrl = baseUrl + foundFile;

var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                            if (response.IsSuccessStatusCode)
                            {
                                var localFilePath = System.IO.Path.Combine(wadFolderPath, foundFile);
                                using (var contentStream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = File.Create(localFilePath))
                                {
                                    await contentStream.CopyToAsync(fileStream);
                                }
                                
                                fileFound = true;
                            }
                        }
                    }
                    catch (Exception indexEx)
                    {
                    }
                }
                
                if (!fileFound)
                {
                    throw new Exception($"WrightSkins sunucusunda '{championCodename}' için WAD dosyasý bulunamadý. Base URL: {baseUrl}");
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task DownloadChromaFolderFromWrightSkins(string baseUrl, string localFolderPath, string championName)
    {
        try
        {
            
            var wrightSkinsCodename = GetChampionCodename(championName);
            if (wrightSkinsCodename != championName)
            {
            }
            
            var wadFolderPath = System.IO.Path.Combine(localFolderPath, "WAD");
            Directory.CreateDirectory(wadFolderPath);
            
            using (var httpClient = new HttpClient())
            {
                
                var chromaFiles = new[]
                {
                    $"{wrightSkinsCodename}.wad.client",
                    $"{wrightSkinsCodename}.wad",
                    $"{wrightSkinsCodename.ToLower()}.wad.client",
                    $"{wrightSkinsCodename.ToLower()}.wad"
                };
                
                bool fileFound = false;
                
                foreach (var fileName in chromaFiles)
                {
                    try
                    {
                        var fileUrl = baseUrl + fileName;
                        
                        var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            
                            var localFilePath = System.IO.Path.Combine(wadFolderPath, fileName);
                            var totalBytes = response.Content.Headers.ContentLength ?? 0;
                            var downloadedBytes = 0L;
                            
                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = File.Create(localFilePath))
                            {
                                var buffer = new byte[8192];
                                int bytesRead;
                                
                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    downloadedBytes += bytesRead;
                                    
                                    if (totalBytes > 0)
                                    {
                                        var percentage = (int)((downloadedBytes * 100) / totalBytes);
                                        ChampionsModalDownloadButton.Content = $"? {percentage}%";
                                    }
                                }
                            }
                            
                            fileFound = true;
                            
                            ChampionsModalDownloadButton.Content = "? %100";
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                
                if (!fileFound)
                {
                    throw new Exception($"WrightSkins sunucusunda '{championName}' (codename: {wrightSkinsCodename}) için chroma dosyasý bulunamadý. URL: {baseUrl}");
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task DownloadFolderFromWrightSkins(string baseUrl, string localFolderPath, string skinName)
    {
        try
        {
            
            var wadFolderPath = System.IO.Path.Combine(localFolderPath, "WAD");
            Directory.CreateDirectory(wadFolderPath);
            
            using (var httpClient = new HttpClient())
            {

var championName = skinName.Split(' ')[0];
                var lastWord = skinName.Split(' ').LastOrDefault();
                
                string championCodename = "";
                try
                {
                    var uri = new Uri(baseUrl);
                    var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var skinsIndex = Array.IndexOf(pathParts, "skins");
                    
                    if (skinsIndex >= 0 && skinsIndex + 1 < pathParts.Length)
                    {
                        var championOriginalName = Uri.UnescapeDataString(pathParts[skinsIndex + 1]);
                        championCodename = GetChampionCodename(championOriginalName);
                    }
                }
                catch (Exception ex)
                {
                }
                
                var possibleChampionNames = new[]
                {
                    championCodename,
                    lastWord,
                    championName,
                    skinName.Replace(" ", ""),
                };
                
                var commonFiles = new List<string>();
                foreach (var champ in possibleChampionNames.Where(c => !string.IsNullOrEmpty(c)).Distinct())
                {
                    commonFiles.AddRange(new[]
                    {
                        $"{champ}.wad.client",
                        $"{champ}.wad"
                    });
                }
                
                commonFiles.AddRange(new[]
                {
                    "skin.wad.client",
                    "skin.wad"
                });
                
                bool fileFound = false;
                
                foreach (var fileName in commonFiles.Distinct())
                {
                    try
                    {
                        var fileUrl = baseUrl + Uri.EscapeDataString(fileName);
                        
                        var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            
                            var localFilePath = System.IO.Path.Combine(wadFolderPath, fileName);
                            var totalBytes = response.Content.Headers.ContentLength ?? 0;
                            var downloadedBytes = 0L;
                            
                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = File.Create(localFilePath))
                            {
                                var buffer = new byte[8192];
                                int bytesRead;
                                
                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    downloadedBytes += bytesRead;
                                    
                                    if (totalBytes > 0)
                                    {
                                        var percentage = (int)((downloadedBytes * 100) / totalBytes);
                                        ChampionsModalDownloadButton.Content = $"? {percentage}%";
                                    }
                                }
                            }
                            
                            fileFound = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
                
                if (!fileFound)
                {
                    try
                    {
                        var indexResponse = await httpClient.GetStringAsync(baseUrl);
                        
                        var wadFilePattern = @"href=[""']([^""']*\.wad(?:\.client)?)[""']";
                        var matches = System.Text.RegularExpressions.Regex.Matches(indexResponse, wadFilePattern, 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (matches.Count > 0)
                        {
                            var foundFile = matches[0].Groups[1].Value;
                            
                            string fileUrl;
                            if (foundFile.StartsWith("/"))
                            {
                                var baseUri = new Uri(baseUrl);
                                fileUrl = $"{baseUri.Scheme}://{baseUri.Host}{foundFile}";
                            }
                            else
                            {
                                fileUrl = baseUrl + foundFile;
                            }

var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                            if (response.IsSuccessStatusCode)
                            {
                                var fileName = System.IO.Path.GetFileName(foundFile);
                                var localFilePath = System.IO.Path.Combine(wadFolderPath, fileName);
                                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                                var downloadedBytes = 0L;
                                
                                using (var contentStream = await response.Content.ReadAsStreamAsync())
                                using (var fileStream = File.Create(localFilePath))
                                {
                                    var buffer = new byte[8192];
                                    int bytesRead;
                                    
                                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        downloadedBytes += bytesRead;
                                        
                                        if (totalBytes > 0)
                                        {
                                            var percentage = (int)((downloadedBytes * 100) / totalBytes);
                                            ChampionsModalDownloadButton.Content = $"? {percentage}%";
                                        }
                                    }
                                }
                                
                                fileFound = true;
                            }
                            else
                            {
                            }
                        }
                    }
                    catch (Exception indexEx)
                    {
                    }
                }
                
                if (!fileFound)
                {
                    throw new Exception("WrightSkins sunucusunda hiçbir WAD dosyasý bulunamadý");
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task CheckDownloadServerSetting()
    {
        try
        {
            string currentServer = await GetCurrentDownloadServer();
            
            if (currentServer == "WrightSkins")
            {
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void DownloadSkin(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedSkin != null)
        {
            var skin = viewModel.SelectedSkin;

if (skin.WadURL == "SPECIAL_SKIN_NO_DOWNLOAD")
            {
                await ShowWarningModalAsync(
                    "Özel Skin", 
                    "Bu özel skin sadece önizleme amaçlýdýr. Ýndirme baðlantýsý henüz mevcut deðil."
                );
                return;
            }
            
            if (string.IsNullOrEmpty(skin.WadURL))
            {
                await ShowWarningModalAsync(
                    LocalizationService.Instance.Translate("NoDownloadLink"), 
                    LocalizationService.Instance.Translate("NoDownloadLinkMessage")
                );
                return;
            }

try
            {
                var vm = DataContext as MainViewModel;
                
                string currentServer = await GetCurrentDownloadServer();
                
                if (vm != null)
                {
                    vm.DownloadButtonText = LocalizationService.Instance.Translate("DownloadProgressZero");
                    vm.IsDownloadButtonEnabled = false;
                }

                var downloadLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var downloadWrightSkinsPath = System.IO.Path.Combine(downloadLocalAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
                
                var safeSkinName = CleanSkinNameForFileSystem(skin.Name);
                
                var downloadSkinFolderPath = System.IO.Path.Combine(downloadWrightSkinsPath, safeSkinName);
                var downloadWadFolderPath = System.IO.Path.Combine(downloadSkinFolderPath, "WAD");
                var metaFolderPath = System.IO.Path.Combine(downloadSkinFolderPath, "META");
                
                Directory.CreateDirectory(downloadWadFolderPath);
                Directory.CreateDirectory(metaFolderPath);
                
                DirectoryInfo skinFolder = new DirectoryInfo(downloadSkinFolderPath);
                skinFolder.Attributes |= FileAttributes.Hidden | FileAttributes.System;

                bool isFromChampionsPage = false;
                string? actualChampionCodename = null;
                
                if (DataContext is MainViewModel currentViewModel)
                {
                    isFromChampionsPage = currentViewModel.SelectedMenuItem == "Champions";
                }

                if (currentServer == "WrightSkins" && isFromChampionsPage)
                {
                    
                    var championName = skin.Champion;
                    if (string.IsNullOrEmpty(championName))
                    {
                        var urlParts = skin.WadURL.Split('/');
                        var skinsIndex = Array.IndexOf(urlParts, "skins");
                        if (skinsIndex >= 0 && skinsIndex + 1 < urlParts.Length)
                        {
                            championName = Uri.UnescapeDataString(urlParts[skinsIndex + 1]);
                        }
                        else
                        {
                            var words = skin.Name.Split(' ');
                            championName = words.LastOrDefault() ?? "Unknown";
                        }
                    }
                    
                    var wrightSkinsUrl = await ConvertGitHubUrlToWrightSkinsUrl(skin.WadURL, championName, skin.Name);
                    
                    var wrightSkinsCodename = GetChampionCodename(championName);
                    actualChampionCodename = wrightSkinsCodename;
                    
                    await DownloadFolderFromWrightSkinsForSkinPage(wrightSkinsUrl, downloadSkinFolderPath, skin.Name, vm, wrightSkinsCodename);
                }
                else
                {
                    
                    var championName = skin.Champion;
                    if (string.IsNullOrEmpty(championName))
                    {
                        var urlParts = skin.WadURL.Split('/');
                        var skinsIndex = Array.IndexOf(urlParts, "skins");
                        if (skinsIndex >= 0 && skinsIndex + 1 < urlParts.Length)
                        {
                            championName = Uri.UnescapeDataString(urlParts[skinsIndex + 1]);
                        }
                        else
                        {
                            championName = skin.Champion ?? "Unknown";
                        }
                    }
                    actualChampionCodename = GetChampionCodename(championName);
                    
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync(skin.WadURL, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        var totalBytes = response.Content.Headers.ContentLength ?? 0;
                        var downloadedBytes = 0L;

                        var fileName = System.IO.Path.GetFileName(new Uri(skin.WadURL).LocalPath);
                        var isFantome = fileName.EndsWith(".fantome", StringComparison.OrdinalIgnoreCase);
                        
                        string downloadFilePath;
                        if (isFantome)
                        {
                            downloadFilePath = System.IO.Path.Combine(downloadWadFolderPath, fileName);
                        }
                        else
                        {
                            downloadFilePath = System.IO.Path.Combine(downloadWadFolderPath, $"{actualChampionCodename}.wad.client");
                        }
                        
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(downloadFilePath))
                        {
                            var buffer = new byte[8192];
                            int bytesRead;
                            
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloadedBytes += bytesRead;
                                
                                if (totalBytes > 0)
                                {
                                    var percentage = (int)((downloadedBytes * 100) / totalBytes);
                                    if (DataContext is MainViewModel progressVm)
                                    {
                                        progressVm.DownloadButtonText = string.Format(
                                            LocalizationService.Instance.Translate("DownloadProgress"), 
                                            percentage
                                        );
                                    }
                                }
                            }
                        }
                        
                        if (isFantome)
                        {
                            await ProcessFantomeFile(downloadFilePath, downloadWadFolderPath);
                        }
                    }
                }

                var infoData = new
                {
                    Author = "WRIGHTSKINS",
                    Description = "WRIGHTSKINS", 
                    Heart = "https://www.bontur.com.tr/discord",
                    Home = "https://www.bontur.com.tr/discord",
                    Name = "WRIGHTSKINS",
                    Version = "WRIGHTSKINS"
                };
                
                var infoJson = JsonConvert.SerializeObject(infoData, Formatting.Indented);
                var infoFilePath = System.IO.Path.Combine(metaFolderPath, "info.json");
                await File.WriteAllTextAsync(infoFilePath, infoJson);

                var versionData = new[]
                {
                    new { version = skin.Version, isChampion = skin.IsChampion }
                };
                
                var versionJson = JsonConvert.SerializeObject(versionData, Formatting.Indented);
                var versionFilePath = System.IO.Path.Combine(downloadSkinFolderPath, "version.json");
                await File.WriteAllTextAsync(versionFilePath, versionJson);

                if (vm.IsDownloadingFromSpecialPage)
                {
                    await UpdateSpecialSkinsJson(skin, actualChampionCodename);
                    vm.IsDownloadingFromSpecialPage = false;
                }
                else
                {
                    await UpdateInstalledSkinsJson(skin, false, actualChampionCodename);
                }

                if (DataContext is MainViewModel updateVm)
                {
                    await updateVm.LoadDataCommand.ExecuteAsync(null);
                    
                }

                await ShowSuccessModalAsync(
                    LocalizationService.Instance.Translate("DownloadSuccess"), 
                    string.Format(LocalizationService.Instance.Translate("DownloadSuccessMessage"), skin.Name)
                );
            }
            catch (Exception ex)
            {
                await ShowErrorModalAsync(
                    LocalizationService.Instance.Translate("DownloadErrorMessage"), 
                    string.Format(LocalizationService.Instance.Translate("DownloadError"), ex.Message)
                );
            }
        }
    }

    private void ReportSkin(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentPreviewSkin == null) return;

            var previewModal = FindName("PreviewModal") as Grid;
            if (previewModal != null)
            {
                CleanupWebView(() => {
                    previewModal.Visibility = Visibility.Collapsed;
                });
            }

            var reportSkinNameText = FindName("ReportSkinNameText") as TextBlock;
            if (reportSkinNameText != null)
            {
                reportSkinNameText.Text = $"{_currentPreviewSkin.Name} ({_currentPreviewSkin.Champion})";
            }

            var reportModal = FindName("ReportModalOverlay") as Grid;
            if (reportModal != null)
            {
                reportModal.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening report modal: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnReportModalClose(object sender, RoutedEventArgs e)
    {
        try
        {
            var reportModal = FindName("ReportModalOverlay") as Grid;
            if (reportModal != null)
            {
                reportModal.Visibility = Visibility.Collapsed;
            }

            var reasonComboBox = FindName("ReportReasonComboBox") as ComboBox;
            var commentsTextBox = FindName("ReportCommentsTextBox") as TextBox;
            var submitButton = FindName("ReportSubmitButton") as Button;

            if (reasonComboBox != null) reasonComboBox.SelectedIndex = -1;
            if (commentsTextBox != null) commentsTextBox.Text = "";
            if (submitButton != null) submitButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing report modal: {ex.Message}");
        }
    }

    private void OnReportReasonSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var comboBox = sender as ComboBox;
            var submitButton = FindName("ReportSubmitButton") as Button;
            
            if (comboBox != null && submitButton != null)
            {
                submitButton.IsEnabled = comboBox.SelectedIndex >= 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling report reason selection: {ex.Message}");
        }
    }

    private void OnReportCommentsChanged(object sender, TextChangedEventArgs e)
    {
    }

    private async void OnReportSubmitClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentPreviewSkin == null) return;

            var reasonComboBox = FindName("ReportReasonComboBox") as ComboBox;
            var commentsTextBox = FindName("ReportCommentsTextBox") as TextBox;

            if (reasonComboBox?.SelectedItem == null) return;

            var selectedReason = ((ComboBoxItem)reasonComboBox.SelectedItem).Content.ToString();
            var reasonTag = ((ComboBoxItem)reasonComboBox.SelectedItem).Tag?.ToString() ?? "other";
            var additionalComments = commentsTextBox?.Text ?? "";

            var submitButton = FindName("ReportSubmitButton") as Button;
            if (submitButton != null)
            {
                submitButton.IsEnabled = false;
                submitButton.Content = "Submitting...";
            }

            var reportData = new
            {
                skin_id = _currentPreviewSkin.Id,
                skin_nick = _currentPreviewSkin.Name,
                reporter_uid = _currentDiscordUser?.Id ?? "0",
                reason = additionalComments,
                reportType = reasonTag
            };

            var success = await SubmitReportToApi(reportData);

            if (success)
            {
                if (submitButton != null)
                {
                    submitButton.IsEnabled = true;
                    submitButton.Content = "Submit Report";
                }

                OnReportModalClose(sender, e);

                await ShowSuccessModalAsync(
                    "Thank you for your report! Your feedback helps us maintain a safe and quality community.",
                    "Report Submitted"
                );
            }
            else
            {
                await ShowWarningModalAsync(
                    "Failed to submit report. Please try again later.",
                    "Report Failed"
                );
            }
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(
                $"Error submitting report: {ex.Message}",
                "Error"
            );
        }
        finally
        {
            var submitButton = FindName("ReportSubmitButton") as Button;
            if (submitButton != null && submitButton.Content.ToString() == "Submitting...")
            {
                submitButton.IsEnabled = true;
                submitButton.Content = "Submit Report";
            }
        }
    }

    private async Task<bool> SubmitReportToApi(object reportData)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(reportData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await WrightSkinsApiService.PostAsync(WrightUtils.K, content);

var responseText = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(responseText);
                
                var success = document.RootElement.TryGetProperty("success", out var successProp) && 
                             successProp.GetBoolean();

return success;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Report API error: {ex.Message}");
            return false;
        }
    }

    private async Task UpdateInstalledSkinsJson(Skin skin, bool isFromChampionsPage = false, string? championCodename = null)
    {
        try
        {
            var updateLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var updateWrightSkinsPath = System.IO.Path.Combine(updateLocalAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var updateInstalledJsonPath = System.IO.Path.Combine(updateWrightSkinsPath, "installed.json");

            List<InstalledSkin> installedSkins;

            if (File.Exists(updateInstalledJsonPath))
            {
                var existingJson = await File.ReadAllTextAsync(updateInstalledJsonPath);
                installedSkins = JsonConvert.DeserializeObject<List<InstalledSkin>>(existingJson) ?? new List<InstalledSkin>();
            }
            else
            {
                installedSkins = new List<InstalledSkin>();
            }

            var actualChampion = !string.IsNullOrEmpty(championCodename) ? championCodename : skin.Champion;
            
            var cleanSkinName = CleanSkinNameForFileSystem(skin.Name);

var finalImageCard = !string.IsNullOrEmpty(skin.ListImage) ? skin.ListImage : skin.ImageCard;
            
            var existingSkin = installedSkins.FirstOrDefault(s => s.Id == skin.Id || (s.Name == cleanSkinName && s.Champion == actualChampion));
            
            if (existingSkin != null)
            {
                existingSkin.Id = skin.Id;
                existingSkin.Name = cleanSkinName;
                existingSkin.Version = skin.Version;
                existingSkin.Champion = actualChampion;
                existingSkin.ImageCard = finalImageCard;
                existingSkin.WadFile = System.IO.Path.Combine("WAD", $"{actualChampion}.wad.client");
                existingSkin.IsChampion = skin.IsChampion;
                existingSkin.UpToDate = true;
                existingSkin.InstallDate = DateTime.Now;
                existingSkin.IsBuilded = isFromChampionsPage;
                existingSkin.IsCustom = false;
            }
            else
            {
                installedSkins.Add(new InstalledSkin
                {
                    Id = skin.Id,
                    Name = cleanSkinName,
                    Version = skin.Version,
                    Champion = actualChampion,
                    ImageCard = finalImageCard,
                    WadFile = System.IO.Path.Combine("WAD", $"{actualChampion}.wad.client"),
                    IsChampion = skin.IsChampion,
                    UpToDate = true,
                    InstallDate = DateTime.Now,
                    IsBuilded = isFromChampionsPage,
                    IsCustom = false
                });
            }

            var updatedJson = JsonConvert.SerializeObject(installedSkins, Formatting.Indented);
            await File.WriteAllTextAsync(updateInstalledJsonPath, updatedJson);
        }
        catch (Exception ex)
        {
        }
    }

    private bool isSpecialSkin(Skin skin)
    {
        return !string.IsNullOrEmpty(skin.WadURL) && 
               (skin.WadURL.StartsWith("WrightUtils.E/") || 
                skin.WadURL == "SPECIAL_SKIN_NO_DOWNLOAD");
    }

    private async Task UpdateSpecialSkinsJson(Skin skin, string? championCodename = null)
    {
        try
        {
            var updateLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var updateWrightSkinsPath = System.IO.Path.Combine(updateLocalAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var specialJsonPath = System.IO.Path.Combine(updateWrightSkinsPath, "special.json");

            List<InstalledSkin> specialSkins;

            if (File.Exists(specialJsonPath))
            {
                var existingJson = await File.ReadAllTextAsync(specialJsonPath);
                specialSkins = JsonConvert.DeserializeObject<List<InstalledSkin>>(existingJson) ?? new List<InstalledSkin>();
            }
            else
            {
                specialSkins = new List<InstalledSkin>();
            }

            var actualChampion = !string.IsNullOrEmpty(championCodename) ? championCodename : skin.Champion;
            
            var cleanSkinName = skin.Name.Replace(":", "");

var finalImageCard = !string.IsNullOrEmpty(skin.ListImage) ? skin.ListImage : skin.ImageCard;
            
            var existingSkin = specialSkins.FirstOrDefault(s => s.Id == skin.Id || (s.Name == cleanSkinName && s.Champion == actualChampion));
            
            if (existingSkin != null)
            {
                existingSkin.Id = skin.Id;
                existingSkin.Name = cleanSkinName;
                existingSkin.Version = skin.Version;
                existingSkin.Champion = actualChampion;
                existingSkin.ImageCard = finalImageCard;
                existingSkin.WadFile = System.IO.Path.Combine("WAD", $"{actualChampion}.wad.client");
                existingSkin.IsChampion = skin.IsChampion;
                existingSkin.UpToDate = true;
                existingSkin.InstallDate = DateTime.Now;
                existingSkin.IsBuilded = false;
                existingSkin.IsCustom = false;
                
            }
            else
            {
                specialSkins.Add(new InstalledSkin
                {
                    Id = skin.Id,
                    Name = cleanSkinName,
                    Version = skin.Version,
                    Champion = actualChampion,
                    ImageCard = finalImageCard,
                    WadFile = System.IO.Path.Combine("WAD", $"{actualChampion}.wad.client"),
                    IsChampion = skin.IsChampion,
                    UpToDate = true,
                    InstallDate = DateTime.Now,
                    IsBuilded = false,
                    IsCustom = false,
                    IsSelected = false
                });
                
            }

            var updatedJson = JsonConvert.SerializeObject(specialSkins, Formatting.Indented);
            await File.WriteAllTextAsync(specialJsonPath, updatedJson);
            
        }
        catch (Exception ex)
        {
        }
    }

    private void OnInstalledSkinCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is InstalledSkin clickedSkin)
        {
            clickedSkin.IsSelected = !clickedSkin.IsSelected;
            
            UpdateWrightProfileWithSkinToggle(clickedSkin.Name, clickedSkin.IsSelected);
            
        }
    }

    private void OnLobbySkinCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is LobbySkin clickedSkin)
        {
            clickedSkin.IsSelected = !clickedSkin.IsSelected;
            
            if (!clickedSkin.IsSelected)
            {
                clickedSkin.ResetDownloadStatus();
            }
            
            UpdateWrightProfileWithSkinToggle(clickedSkin.SkinName, clickedSkin.IsSelected);
            
        }
    }
    
    private void UpdateWrightProfileWithSkinToggle(string skinName, bool isSelected)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var profilePath = System.IO.Path.Combine(wrightPath, "Wright.profile");
            
            Directory.CreateDirectory(wrightPath);
            
            var lines = new List<string>();
            if (File.Exists(profilePath))
            {
                lines = File.ReadAllLines(profilePath).ToList();
            }
            else
            {
                lines = new List<string>();
            }
            
            var existingLineIndex = lines.FindIndex(line => 
                !string.IsNullOrWhiteSpace(line) && 
                !line.StartsWith("#") && 
                string.Equals(line.Trim(), skinName, StringComparison.OrdinalIgnoreCase));
            
            if (isSelected)
            {
                if (existingLineIndex == -1)
                {
                    lines.Add(skinName);
                }
            }
            else
            {
                var initialCount = lines.Count;
                lines.RemoveAll(line => 
                    !string.IsNullOrWhiteSpace(line) && 
                    !line.StartsWith("#") && 
                    string.Equals(line.Trim(), skinName, StringComparison.OrdinalIgnoreCase));
                    
                var removedCount = initialCount - lines.Count;
                if (removedCount > 0)
                {
                }
            }
            
            File.WriteAllLines(profilePath, lines);
        }
        catch (Exception ex)
        {
        }
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGridColumns();
    }

    private void UpdateGridColumns()
    {
        int columns = CalculateOptimalColumns();
        
        Console.WriteLine($"Optimal sütun sayýsý: {columns}");
    }

    private int CalculateOptimalColumns()
    {
        double windowWidth = this.ActualWidth;
        
        double availableWidth = windowWidth - 80 - 70;
        
        double cardWidth = 300;
        
        int columns = Math.Max(1, (int)(availableWidth / cardWidth));
        
        return Math.Min(6, Math.Max(1, columns));
    }

    private async void OnInjectClick(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        try
        {
            if (_isInjecting)
            {
                await StopInjectProcess();
            }
            else
            {
                var selectedCount = GetSelectedSkinsCount();
                if (selectedCount == 0)
                {
                    await ShowWarningModalAsync(
                        LocalizationService.Instance.Translate("SkinSelection"), 
                        LocalizationService.Instance.Translate("SelectSkinMessage")
                    );
                    return;
                }
                
                await StartInjectProcess();
            }
        }
        catch (Exception ex)
        {
            _isInjecting = false;
            _processStarted = false;
            UpdateInjectButtonUI();
            
            await ShowErrorModalAsync(
                LocalizationService.Instance.Translate("InjectErrorMessage"),
                string.Format(LocalizationService.Instance.Translate("InjectError"), ex.Message)
            );
        }
    }

    private async Task StartInjectProcess()
    {
        try
        {
            _isInjecting = true;
            _processStarted = false;
            UpdateInjectButtonUI();

var gamePath = await GetGamePathFromConfig();
            
            var modsList = await GetModsListFromProfile();
            
            if (string.IsNullOrEmpty(modsList))
            {
                await ShowWarningModalAsync(LocalizationService.Instance.Translate("noActiveSkin"), LocalizationService.Instance.Translate("noActiveSkinMessage"));
                throw new Exception("Wright.profile'da aktif skin bulunamadý!");
            }

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var cslolToolsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cslol-tools");
            var modToolsPath = System.IO.Path.Combine(cslolToolsPath, "mod-tools.exe");
            
            var wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            var alrPath = System.IO.Path.Combine(wrightSkinsPath, "ALR");
            var overlayPath = System.IO.Path.Combine(wrightSkinsPath, "TODO", "OVERLAY");

            Directory.CreateDirectory(System.IO.Path.Combine(wrightSkinsPath, "TODO"));

            if (!File.Exists(modToolsPath))
            {
                await ShowErrorModalAsync("Dosya Bulunamadý", "mod-tools.exe bulunamadý!");
                throw new Exception($"mod-tools.exe bulunamadý: {modToolsPath}");
            }

            var mkOverlayArgs = $"mkoverlay \"{alrPath}\" \"{overlayPath}\" --game:\"{gamePath}\" --mods:\"{modsList}\" --noTFT --ignoreConflict";

var mkOverlayProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = modToolsPath,
                    Arguments = mkOverlayArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            mkOverlayProcess.Start();
            var mkOverlayOutput = await mkOverlayProcess.StandardOutput.ReadToEndAsync();
            var mkOverlayError = await mkOverlayProcess.StandardError.ReadToEndAsync();
            await mkOverlayProcess.WaitForExitAsync();

            if (mkOverlayProcess.ExitCode != 0)
            {
                
                string errorMessage;
                string errorTitle;
                
                if (mkOverlayError.Contains("Not a valid Game folder") || mkOverlayError.Contains("error: Not a valid Game folder"))
                {
                    errorMessage = LocalizationService.Instance.Translate("error_invalid_game_path") ?? "League of Legends konumu doðru deðil! Lütfen ayarlardan doðru konumu seçtiðinizden emin olunuz.";
                    errorTitle = LocalizationService.Instance.Translate("error_game_path_title") ?? "Oyun Konumu Hatasý";
                }
                else if (mkOverlayError.Contains("Not valid mod!") || mkOverlayError.Contains("error: Not valid mod!"))
                {
                    var corruptedMods = ExtractCorruptedModsFromError(mkOverlayError);
                    var corruptedModsList = string.Join("\n- ", corruptedMods);
                    
                    errorMessage = (LocalizationService.Instance.Translate("error_corrupted_mods") ?? "Aþaðýda belirtilen skinlerin formatý bozuk. Güncellemeyi deneyin veya hatayý bize bildirin:") + $"\n- {corruptedModsList}";
                    errorTitle = LocalizationService.Instance.Translate("error_corrupted_mods_title") ?? "Bozuk Skin Formatý";
                }
                else
                {
                    errorMessage = LocalizationService.Instance.Translate("error_injection_general") ?? 
                        "Injection esnasýnda bir hata oluþtu! Þu sebeplerden dolayý olabilir:\n\n- League of Legends'i kapatýp injection'u baþlatýn.\n- Arkada çalýþan baþka bir injection yazýlýmý varsa (LCS-Manager gibi) kapatmayý deneyin.\n\nHata düzelmez ise Discord adresimizden bizlere ulaþýn.";
                    errorTitle = LocalizationService.Instance.Translate("error_title") ?? "Hata";
                }
                
                await ShowErrorModalAsync(errorMessage, errorTitle);
                
                _isInjecting = false;
                _processStarted = false;
                UpdateInjectButtonUI();
                
                return;
            }

var wrightProfilePath = System.IO.Path.Combine(alrPath, "Wright.profile");
            var runOverlayArgs = $"runoverlay \"{overlayPath}\" \"{wrightProfilePath}\"";

lock (_processLock)
            {
                _modToolsProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = modToolsPath,
                        Arguments = runOverlayArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _modToolsProcess.Start();
            }

_processStarted = true;
            UpdateInjectButtonUI();
        }
        catch (Exception ex)
        {
            _isInjecting = false;
            _processStarted = false;
            UpdateInjectButtonUI();
            throw;
        }
    }

    private List<string> ExtractCorruptedModsFromError(string errorOutput)
    {
        var corruptedMods = new List<string>();
        
        try
        {
            var lines = errorOutput.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("mod_path = ") && line.Contains("WrightSkins/ALR/"))
                {
                    var modPath = line.Split('=')[1].Trim();
                    var modName = System.IO.Path.GetFileName(modPath);
                    if (!string.IsNullOrEmpty(modName))
                    {
                        corruptedMods.Add(modName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
        
        return corruptedMods;
    }

    private async Task StopInjectProcess()
    {
        try
        {

            lock (_processLock)
            {
                if (_modToolsProcess != null && !_modToolsProcess.HasExited)
                {
                    try
                    {
                        _modToolsProcess.Kill();
                        _modToolsProcess.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        _modToolsProcess.Dispose();
                        _modToolsProcess = null;
                    }
                }
            }

            _isInjecting = false;
            _processStarted = false;
            UpdateInjectButtonUI();

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<string> GetGamePathFromConfig()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "config.json");

            if (File.Exists(configPath))
            {
                var configJson = await File.ReadAllTextAsync(configPath);
                var config = JsonConvert.DeserializeObject<AppSettings>(configJson);
                
                if (config != null && !string.IsNullOrEmpty(config.GamePath))
                {
                    return config.GamePath;
                }
            }

            var defaultPath = @"C:\Riot Games\League of Legends\Game";
            return defaultPath;
        }
        catch (Exception ex)
        {
            return @"C:\Riot Games\League of Legends\Game";
        }
    }

    private async Task SaveConfigAsync()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configDir = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            var configPath = System.IO.Path.Combine(configDir, "config.json");

            Directory.CreateDirectory(configDir);

            if (!File.Exists(configPath))
            {
                var defaultConfig = new AppSettings();
                var jsonString = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                await File.WriteAllTextAsync(configPath, jsonString);
                return;
            }

            var existingJson = await File.ReadAllTextAsync(configPath);
            var config = JsonConvert.DeserializeObject<AppSettings>(existingJson) ?? new AppSettings();

            var gamePathTextBox = FindName("GamePathTextBox") as TextBox;
            if (gamePathTextBox != null && !string.IsNullOrEmpty(gamePathTextBox.Text))
                config.GamePath = gamePathTextBox.Text;
            
            var performanceModeCheckBox = FindName("PerformanceModeCheckBox") as CheckBox;
            if (performanceModeCheckBox != null)
                config.Performance.EnablePerformanceMode = performanceModeCheckBox.IsChecked ?? false;
                
            var gpuAccelerationCheckBox = FindName("GpuAccelerationCheckBox") as CheckBox;
            if (gpuAccelerationCheckBox != null)
                config.Performance.EnableGPUAcceleration = gpuAccelerationCheckBox.IsChecked ?? true;
                
            var reduceAnimationsCheckBox = FindName("ReduceAnimationsCheckBox") as CheckBox;
            if (reduceAnimationsCheckBox != null)
                config.Performance.ReduceAnimations = reduceAnimationsCheckBox.IsChecked ?? false;

            var updatedJsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
            await File.WriteAllTextAsync(configPath, updatedJsonString);

        }
        catch (Exception ex)
        {
        }
    }

    private async void OnAutoDetectGamePathClick(object sender, RoutedEventArgs e)
    {
        try
        {
            
            var detectedPath = AutoDetectLeagueOfLegends();
            
            if (!string.IsNullOrEmpty(detectedPath))
            {
                var gamePathTextBox = FindName("GamePathTextBox") as TextBox;
                if (gamePathTextBox != null)
                {
                    gamePathTextBox.Text = detectedPath;
                }
                
                await SaveConfigAsync();
                
                var button = sender as Button;
                if (button != null)
                {
                    var originalBackground = button.Background;
                    button.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                    
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            button.Background = originalBackground;
                        });
                    });
                }
            }
            else
            {
                await ShowCustomModalAsync(LocalizationService.Instance.Translate("autoDetection"), LocalizationService.Instance.Translate("leagueNotFoundMessage"), ModalType.Information);
            }
        }
        catch (Exception ex)
        {
        }
    }

    private string AutoDetectLeagueOfLegends()
    {
        try
        {
            
            var registryPath = GetRiotGamesPathFromRegistry();
            if (!string.IsNullOrEmpty(registryPath))
            {
                var gameDirectory = System.IO.Path.Combine(registryPath, "Game");
                if (Directory.Exists(gameDirectory))
                {
                    return gameDirectory;
                }
            }

            var executablePath = FindLeagueExecutable();
            if (!string.IsNullOrEmpty(executablePath))
            {
                return executablePath;
            }

            var processPath = GetLeaguePathFromProcessesSafely();
            if (!string.IsNullOrEmpty(processPath))
            {
                return processPath;
            }

            string[] defaultPaths = {
                @"C:\Riot Games\League of Legends\Game",
                @"D:\Riot Games\League of Legends\Game", 
                @"E:\Riot Games\League of Legends\Game",
                @"F:\Riot Games\League of Legends\Game",
                @"G:\Riot Games\League of Legends\Game"
            };

            foreach (var path in defaultPaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return "";
        }
        catch (Exception ex)
        {
            return "";
        }
    }

    private string GetRiotGamesPathFromRegistry()
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Riot Games\League of Legends"))
            {
                if (key != null)
                {
                    var installPath = key.GetValue("InstallPath")?.ToString();
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        return installPath;
                    }
                }
            }

            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Riot Games\League of Legends"))
            {
                if (key != null)
                {
                    var installPath = key.GetValue("InstallPath")?.ToString();
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        return installPath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
        return "";
    }

    private string FindLeagueExecutable()
    {
        try
        {
            string[] searchPaths = {
                @"C:\Riot Games",
                @"D:\Riot Games", 
                @"E:\Riot Games",
                @"F:\Riot Games",
                @"G:\Riot Games"
            };

            foreach (var basePath in searchPaths)
            {
                if (Directory.Exists(basePath))
                {
                    var leagueDir = System.IO.Path.Combine(basePath, "League of Legends");
                    if (Directory.Exists(leagueDir))
                    {
                        var gameDir = System.IO.Path.Combine(leagueDir, "Game");
                        if (Directory.Exists(gameDir))
                        {
                            var leagueExe = System.IO.Path.Combine(gameDir, "League of Legends.exe");
                            if (File.Exists(leagueExe))
                            {
                                return gameDir;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
        return "";
    }

    private string GetLeaguePathFromProcessesSafely()
    {
        try
        {
            string[] processNames = { "League of Legends", "LeagueClient", "LeagueClientUx", "RiotClientServices" };
            
            foreach (var processName in processNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (process.ProcessName.Contains("League"))
                            {
                                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                                var riotGamesPath = System.IO.Path.Combine(programFiles, "Riot Games", "League of Legends", "Game");
                                if (Directory.Exists(riotGamesPath))
                                {
                                    return riotGamesPath;
                                }
                                
                                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                                riotGamesPath = System.IO.Path.Combine(programFilesX86, "Riot Games", "League of Legends", "Game");
                                if (Directory.Exists(riotGamesPath))
                                {
                                    return riotGamesPath;
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                        finally
                        {
                            try { process.Dispose(); } catch { }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
        }
        return "";
    }

    private async void OnBrowseGamePathClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = LocalizationService.Instance.Translate("selectLeagueExecutable"),
                Filter = "League of Legends|League of Legends.exe",
                CheckFileExists = true
            };
            
            if (dialog.ShowDialog() == true)
            {
                var selectedFile = dialog.FileName;
                var gameDirectory = System.IO.Path.GetDirectoryName(selectedFile);
                
                if (!string.IsNullOrEmpty(gameDirectory))
                {
                    var gamePathTextBox = FindName("GamePathTextBox") as TextBox;
                    if (gamePathTextBox != null)
                    {
                        gamePathTextBox.Text = gameDirectory;
                    }
                }
                await SaveConfigAsync();
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async Task<string> GetModsListFromProfile()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightProfilePath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", "Wright.profile");

            if (!File.Exists(wrightProfilePath))
            {
                return string.Empty;
            }

            var profileLines = await File.ReadAllLinesAsync(wrightProfilePath);
            var activeMods = profileLines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToList();

            if (activeMods.Count == 0)
            {
                return string.Empty;
            }

            var modsList = string.Join("/", activeMods);
            return modsList;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            PerformanceOptimizationService.CleanupMemory();

            MainViewModel.WrightProfileUpdated -= RefreshInjectButton;
            
            if (_wrightProfileWatcher != null)
            {
                _wrightProfileWatcher.Changed -= OnWrightProfileChanged;
                _wrightProfileWatcher.Error -= OnWrightProfileWatcherError;
                _wrightProfileWatcher.EnableRaisingEvents = false;
                _wrightProfileWatcher.Dispose();
                _wrightProfileWatcher = null;
            }
            
            if (_discordService != null)
            {
                _discordService.UserAuthenticated -= OnDiscordUserAuthenticated;
                _discordService.AuthenticationFailed -= OnDiscordAuthenticationFailed;
                _discordService.Dispose();
            }

if (_socketIOService != null)
            {
                _socketIOService.LobbyInviteReceived -= OnLobbyInviteReceived;
                _socketIOService.LobbyInviteSent -= OnLobbyInviteSent;
                _socketIOService.LobbyInviteAccepted -= OnLobbyInviteAccepted;
                _socketIOService.LobbyInviteDeclined -= OnLobbyInviteDeclined;
                _socketIOService.LobbyInviteError -= OnLobbyInviteError;
                
                await _socketIOService.DisconnectAsync();
                _socketIOService.Dispose();
            }
            
            if (_modToolsProcess != null && !_modToolsProcess.HasExited)
            {
                await StopInjectProcess();
            }
            
            CleanupAllApplicationProcesses();
        }
        catch (Exception ex)
        {
        }
    }
    
    private void CleanupAllApplicationProcesses()
    {
        try
        {
            var currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var processesToKill = new[] { "mod-tools", "WrightLauncher" };

            foreach (var processName in processesToKill)
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.Id != currentProcessId)
                        {
                            process.CloseMainWindow();
                            
                            if (!process.WaitForExit(2000))
                            {
                                process.Kill();
                            }
                        }
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void LoadAutoLoadSettings()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var autoLoadPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", "AutoLoad.json");
            
            if (File.Exists(autoLoadPath))
            {
                var json = File.ReadAllText(autoLoadPath);
                var autoLoadData = JsonConvert.DeserializeObject<dynamic>(json);
                _autoLoadEnabled = autoLoadData?.enabled ?? true;
            }
            else
            {
                _autoLoadEnabled = true;
                var directory = System.IO.Path.GetDirectoryName(autoLoadPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                SaveAutoLoadSetting(_autoLoadEnabled);
            }
            
            var saveProfileCheckBox = FindName("SaveProfileCheckBox") as CheckBox;
            if (saveProfileCheckBox != null)
            {
                saveProfileCheckBox.IsChecked = _autoLoadEnabled;
                DebugConsoleWindow.Instance.WriteLine($"??? Checkbox güncellendi: {_autoLoadEnabled}", "INFO");
            }
            else
            {
                DebugConsoleWindow.Instance.WriteLine("?? SaveProfileCheckBox bulunamadý!", "WARNING");
            }
            
            if (!_autoLoadEnabled)
            {
                ClearWrightProfile();
            }
            
            DebugConsoleWindow.Instance.WriteLine($"?? AutoLoad setting loaded: {(_autoLoadEnabled ? "Enabled" : "Disabled")}", "INFO");
        }
        catch (Exception ex)
        {
        }
    }
    
    private void SaveAutoLoadSetting(bool enabled)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var autoLoadPath = System.IO.Path.Combine(wrightPath, "AutoLoad.json");
            
            Directory.CreateDirectory(wrightPath);
            
            var autoLoadData = new
            {
                enabled = enabled,
                lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            var json = JsonConvert.SerializeObject(autoLoadData, Formatting.Indented);
            File.WriteAllText(autoLoadPath, json);
            
        }
        catch (Exception ex)
        {
        }
    }
    
    private void ClearWrightProfile()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var profilePath = System.IO.Path.Combine(wrightPath, "Wright.profile");
            
            if (File.Exists(profilePath))
            {
                File.WriteAllText(profilePath, "");
            }
        }
        catch (Exception ex)
        {
        }
    }
    
    private void SaveProfileCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        _autoLoadEnabled = true;
        SaveAutoLoadSetting(true);
    }
    
    private void SaveProfileCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        _autoLoadEnabled = false;
        SaveAutoLoadSetting(false);
    }

    private async void OnDownloadSkinClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = LocalizationService.Instance.Translate("selectFantomeFile"),
                Filter = LocalizationService.Instance.Translate("fantomeFileFilter"),
                DefaultExt = ".fantome",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFile = openFileDialog.FileName;
                
                _selectedFantomeFilePath = selectedFile;
                ShowFantomeModal(selectedFile);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(LocalizationService.Instance.Translate("fileSelectionError"), $"{LocalizationService.Instance.Translate("fileSelectionErrorMessage")}: {ex.Message}");
        }
    }

    private async void OnDeleteSkinClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.DataContext is InstalledSkin skinToDelete)
            {
                DeleteSkinCompletely(skinToDelete);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(LocalizationService.Instance.Translate("skinDeleteError"), $"{LocalizationService.Instance.Translate("skinDeleteErrorOccurred")}: {ex.Message}");
        }
    }

    private void DeleteSkinCompletely(InstalledSkin skinToDelete)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string alrPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            string skinFolderPath = System.IO.Path.Combine(alrPath, skinToDelete.Name);

            if (Directory.Exists(skinFolderPath))
            {
                Directory.Delete(skinFolderPath, true);
                Console.WriteLine($"Skin klasörü silindi: {skinFolderPath}");
            }

            string installedJsonPath = System.IO.Path.Combine(alrPath, "installed.json");
            if (File.Exists(installedJsonPath))
            {
                string jsonContent = File.ReadAllText(installedJsonPath);
                var installedSkins = JsonConvert.DeserializeObject<List<InstalledSkin>>(jsonContent) ?? new List<InstalledSkin>();
                
                installedSkins.RemoveAll(s => s.Id == skinToDelete.Id);
                
                string updatedJson = JsonConvert.SerializeObject(installedSkins, Formatting.Indented);
                File.WriteAllText(installedJsonPath, updatedJson);
                Console.WriteLine($"Skin installed.json'dan kaldýrýldý: {skinToDelete.Name}");
            }

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.RemoveInstalledSkin(skinToDelete);
            }

            var cleanSkinName = CleanSkinNameForFileSystem(skinToDelete.Name);
            UpdateWrightProfileWithSkinToggle(cleanSkinName, false);

            Console.WriteLine($"Skin baþarýyla silindi: {skinToDelete.Name}");
        }
        catch (Exception ex)
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                await ShowErrorModalAsync(LocalizationService.Instance.Translate("skinDeleteError"), $"{LocalizationService.Instance.Translate("skinDeleteErrorMessage")}: {ex.Message}");
            }));
        }
    }

    public async Task ShowChampionsModal(Skin skin)
    {
        
        if (skin == null)
        {
            return;
        }

ChampionsModalChampion.Text = skin.Champion;
        ChampionsModalVersion.Text = skin.Version;
        ChampionsModalDescription.Text = skin.Description ?? "No description available.";

if (!string.IsNullOrEmpty(skin.ImagePreview))
        {
            var imageUrl = await GetImageUrlForCurrentServer(skin.ImagePreview, "splash", skin.Id);
            ChampionsModalImageBrush.ImageSource = new BitmapImage(new Uri(imageUrl));
        }
        else if (!string.IsNullOrEmpty(skin.ImageCard))
        {
            var imageUrl = await GetImageUrlForCurrentServer(skin.ImageCard, "splash", skin.Id);
            ChampionsModalImageBrush.ImageSource = new BitmapImage(new Uri(imageUrl));
        }

        UpdateChampionsModalDownloadButton(skin);

        ChampionsModalOverlay.Visibility = Visibility.Visible;
        ChampionsModalOverlay.Opacity = 0.0;
        
        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(250),
            AccelerationRatio = 0.3,
            DecelerationRatio = 0.3
        };

        fadeIn.Completed += (s, args) =>
        {
        };

        ChampionsModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    public void ShowChampionsModalWithLoading(string championName)
    {
        
        currentChampionName = championName;
        
        if (FindName("ChampionsModalChampion") is TextBlock championTextBlock)
        {
            championTextBlock.Text = championName;
        }
        
        ChampionsModalOverlay.Visibility = Visibility.Visible;
        ChampionsModalOverlay.Opacity = 0.0;
        
        if (FindName("ChampionsModalLoadingOverlay") is Border loadingOverlay)
        {
            loadingOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            
            loadingOverlay.Visibility = Visibility.Visible;
            loadingOverlay.Opacity = 1.0;
        }
        
        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(250),
            AccelerationRatio = 0.3,
            DecelerationRatio = 0.3
        };

        fadeIn.Completed += (s, args) =>
        {
        };

        ChampionsModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    public async Task UpdateChampionsModalWithData(string championName, List<ChampionSkinData> dynamicSkins)
    {
        
        if (dynamicSkins == null || dynamicSkins.Count == 0)
        {
            UpdateChampionsModalWithPlaceholder(championName);
            return;
        }

var firstSkin = dynamicSkins.First();
        
        if (!string.IsNullOrEmpty(firstSkin.CommunityDragonSplashUrl))
        {
            try
            {
                var bitmap = await LoadBitmapImageWithTimeoutAsync(firstSkin.CommunityDragonSplashUrl);
                if (bitmap != null)
                {
                    ChampionsModalImageBrush.ImageSource = bitmap;
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        await PopulateSkinList(dynamicSkins);
        
        UpdateChampionsModalDownloadButtonForDynamic(firstSkin);
        
        if (firstSkin.HasChromas)
        {
            PopulateChromas(firstSkin.Chromas);
        }
        
        PopulatePreviewChromas(firstSkin.Chromas);

    }

    public void UpdateChampionsModalWithPlaceholder(string championName)
    {
        
        if (FindName("ChampionsModalLoadingOverlay") is Border loadingOverlay)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            fadeOut.Completed += (s, e) => 
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
                loadingOverlay.Opacity = 1.0;
            };
            
            loadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        var placeholderSkin = new Skin
        {
            Id = -1,
            Name = $"{championName} - No Skins Available",
            Description = $"Bu champion için henüz skin bulunmuyor.",
            Champion = championName,
            ImageCard = "https://via.placeholder.com/300x400/333333/ffffff?text=No+Skin",
            ImagePreview = "https://via.placeholder.com/800x600/333333/ffffff?text=No+Preview",
            FileURL = "",
            Version = "1.0",
            Author = "System",
            IsSelected = false
        };

        ChampionsModalChampion.Text = championName;
        ChampionsModalVersion.Text = "N/A";
        ChampionsModalDescription.Text = LocalizationService.Instance.Translate("NoSkinsAvailableForChampion");
        
        try
        {
            ChampionsModalImageBrush.ImageSource = new BitmapImage(new Uri(placeholderSkin.ImagePreview));
        }
        catch (Exception ex)
        {
        }

        ChampionsModalSkinList.Children.Clear();
        
        ChampionsModalChromaPanel.Visibility = Visibility.Collapsed;
        ChampionsModalPreviewChromaPanel.Visibility = Visibility.Collapsed;

    }

    public async Task ShowChampionsModalWithDynamicSkins(string championName, List<ChampionSkinData> dynamicSkins)
    {

currentChampionName = championName;
        
        if (dynamicSkins == null || dynamicSkins.Count == 0)
        {
            UpdateChampionsModalWithPlaceholder(championName);
            return;
        }

if (FindName("ChampionsModalChampion") is TextBlock championTextBlock)
        {
            championTextBlock.Text = championName;
        }

        var firstSkin = dynamicSkins.First();
        
        if (!string.IsNullOrEmpty(firstSkin.CommunityDragonSplashUrl))
        {
            try
            {
                var bitmap = await LoadBitmapImageWithTimeoutAsync(firstSkin.CommunityDragonSplashUrl);
                if (bitmap != null)
                {
                    ChampionsModalImageBrush.ImageSource = bitmap;
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        await PopulateSkinList(dynamicSkins);
        
        UpdateChampionsModalDownloadButtonForDynamic(firstSkin);
        
        if (firstSkin.HasChromas)
        {
            PopulateChromas(firstSkin.Chromas);
        }
        
        PopulatePreviewChromas(firstSkin.Chromas);

        ChampionsModalOverlay.Visibility = Visibility.Visible;
        ChampionsModalOverlay.Opacity = 0.0;
        
        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(250),
            AccelerationRatio = 0.3,
            DecelerationRatio = 0.3
        };

        fadeIn.Completed += (s, args) =>
        {
        };

        ChampionsModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    private async Task PopulateSkinList(List<ChampionSkinData> skins)
    {
        try
        {
            _championsModalCancellationTokenSource?.Cancel();
            _championsModalCancellationTokenSource?.Dispose();
            _championsModalCancellationTokenSource = new CancellationTokenSource();

ChampionsModalSkinList.Children.Clear();
            
            try
            {
                await Task.Delay(10, _championsModalCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            
            for (int i = 0; i < skins.Count; i++)
            {
                if (_championsModalCancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    return;
                }
                
                var skin = skins[i];
                
                var skinCard = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(8),
                    Margin = i == skins.Count - 1 
                        ? new Thickness(0, 0, 0, 35)
                        : new Thickness(0, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Tag = i,
                    Height = 100,
                    ClipToBounds = true
                };

                var cardBackgroundGrid = new Grid();
                
                var placeholderRect = new System.Windows.Shapes.Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromRgb(45, 55, 72)),
                    RadiusX = 12,
                    RadiusY = 12
                };
                
                var overlayRect = new System.Windows.Shapes.Rectangle
                {
                    Fill = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(200, 45, 55, 72), 0),
                            new GradientStop(Color.FromArgb(180, 30, 40, 60), 1)
                        }
                    },
                    RadiusX = 12,
                    RadiusY = 12
                };
                
                cardBackgroundGrid.Children.Add(placeholderRect);
                cardBackgroundGrid.Children.Add(overlayRect);
                
                skinCard.Child = cardBackgroundGrid;
                
                if (!string.IsNullOrEmpty(skin.CommunityDragonSplashUrl))
                {
                    if (_championsModalCancellationTokenSource?.Token.IsCancellationRequested == true)
                    {
                        continue;
                    }

var wrightSkinsUrl = $"WrightUtils.E/splash_{skin.Id}.jpg";
                    
                    try
                    {
                        if (_championsModalCancellationTokenSource?.Token.IsCancellationRequested == true)
                        {
                            continue;
                        }

var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(wrightSkinsUrl);
                        bitmap.EndInit();
                        
                        var imageBrush = new ImageBrush
                        {
                            ImageSource = bitmap,
                            Stretch = Stretch.UniformToFill
                        };
                        placeholderRect.Fill = imageBrush;
                        
                    }
                    catch (Exception ex)
                    {
                    }
                }
                
                skinCard.MouseEnter += (s, e) => 
                {
                    var border = (Border)s;
                    border.Opacity = 0.8;
                };
                skinCard.MouseLeave += (s, e) => 
                {
                    var border = (Border)s;
                    border.Opacity = 1.0;
                };
                
                skinCard.MouseLeftButtonUp += async (s, e) => 
                {
                    var index = (int)((Border)s).Tag;
                    var selectedSkin = skins[index];
                    
                    await SelectSkinFromList(selectedSkin);
                    
                    if (DataContext is MainViewModel viewModel)
                    {
                        var previewSkin = new Skin
                        {
                            Id = selectedSkin.Id,
                            Name = selectedSkin.Name,
                            Champion = selectedSkin.ChampionKey,
                            Version = "Community Dragon",
                            ImagePreview = selectedSkin.CommunityDragonSplashUrl,
                            ImageCard = selectedSkin.CommunityDragonSplashUrl,
                            YoutubePreview = selectedSkin.VideoPreview,
                            VideoPreview = selectedSkin.VideoPreview,
                            FileURL = selectedSkin.GitHubDownloadUrl,
                            IsChampion = true,
                            Description = selectedSkin.Description ?? "Community skin from Community Dragon"
                        };
                        
                        viewModel.SelectedSkin = previewSkin;
                    }
                };
                
                var stackPanel = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(8)
                };
                
                var rarityPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                
                var rarityIcon = new Image
                {
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 2,
                        Opacity = 0.6,
                        ShadowDepth = 1
                    }
                };
                
                string rarityIconUrl = GetRarityIconUrl(skin.Rarity);
                if (!string.IsNullOrEmpty(rarityIconUrl))
                {
                    rarityIcon.Source = await LoadBitmapImageWithTimeoutAsync(rarityIconUrl);
                }
                
                var rarityText = new TextBlock
                {
                    Text = GetDisplayRarity(skin.Rarity),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 3,
                        Opacity = 0.7,
                        ShadowDepth = 1
                    }
                };
                
                rarityPanel.Children.Add(rarityIcon);
                rarityPanel.Children.Add(rarityText);
                
                var currentLanguageCode = LanguageSettingsService.Instance.LoadLanguageSettings().LanguageCode;
                var localizedSkinName = GetLocalizedSkinName(skin.ChampionKey, skin.Id, skin.Name, currentLanguageCode);
                
                var nameText = new TextBlock
                {
                    Text = localizedSkinName,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 1),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 4,
                        Opacity = 0.8,
                        ShadowDepth = 1
                    }
                };
                
                if (skin.HasChromas)
                {
                    var chromaText = new TextBlock
                    {
                        Text = $"?? {skin.Chromas.Count} {LocalizationService.Instance.Translate("chromas")}",
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        Effect = new DropShadowEffect
                        {
                            Color = Colors.Black,
                            BlurRadius = 2,
                            Opacity = 0.6,
                            ShadowDepth = 1
                        }
                    };
                    stackPanel.Children.Add(chromaText);
                }
                
                stackPanel.Children.Add(rarityPanel);
                stackPanel.Children.Add(nameText);
                
                if (skinCard.Child is Grid existingBackgroundGrid)
                {
                    existingBackgroundGrid.Children.Add(stackPanel);
                }
                else
                {
                    skinCard.Child = stackPanel;
                }
                ChampionsModalSkinList.Children.Add(skinCard);
                
                await AnimateSkinCardEntrance(skinCard, i);
                
                try
                {
                    await Task.Delay(5, _championsModalCancellationTokenSource?.Token ?? CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                
            }

if (FindName("ChampionsModalLoadingOverlay") is Border loadingOverlay)
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                fadeOut.Completed += (s, e) => 
                {
                    loadingOverlay.Visibility = Visibility.Collapsed;
                    loadingOverlay.Opacity = 1.0;
                };
                
                loadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }
        catch (Exception ex)
        {
            
            if (FindName("ChampionsModalLoadingOverlay") is Border loadingOverlay)
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                
                fadeOut.Completed += (s, e) => 
                {
                    loadingOverlay.Visibility = Visibility.Collapsed;
                    loadingOverlay.Opacity = 1.0;
                };
                
                loadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }
    }

    private async Task SelectSkinFromList(ChampionSkinData skin)
    {
        try
        {
            
            if (!string.IsNullOrEmpty(skin.CommunityDragonSplashUrl))
            {
                var imageUrl = await GetImageUrlForCurrentServer(skin.CommunityDragonSplashUrl, "splash", skin.Id);
                
                if (FindName("ChampionsModalImageBrush") is ImageBrush imageBrush)
                {
                    await AnimateImageTransition(imageBrush, imageUrl);
                }
            }
            
            UpdateChampionsModalDownloadButtonForDynamic(skin);
            
            if (skin.HasChromas)
            {
                PopulateChromas(skin.Chromas);
                PopulatePreviewChromas(skin.Chromas);
            }
            else
            {
                ChampionsModalChromaPanel.Visibility = Visibility.Collapsed;
                ChampionsModalPreviewChromaPanel.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void PopulateChromas(List<ChromaData> chromas)
    {
        try
        {
            
            ChampionsModalChromaButtons.Children.Clear();
            
            foreach (var chroma in chromas)
            {
                var colorButton = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Margin = new Thickness(4),
                    Cursor = Cursors.Hand,
                    Tag = chroma,
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
                };
                
                var shadow = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 4,
                    Opacity = 0.6,
                    BlurRadius = 8
                };
                colorButton.Effect = shadow;
                
                if (chroma.Colors != null && chroma.Colors.Count > 0)
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(chroma.Colors[0]);
                        colorButton.Background = new SolidColorBrush(color);
                    }
                    catch
                    {
                        colorButton.Background = new SolidColorBrush(Colors.Gray);
                    }
                }
                else
                {
                    colorButton.Background = new SolidColorBrush(Colors.Gray);
                }
                
                colorButton.MouseEnter += (s, e) => 
                {
                    var border = (Border)s;
                    var scaleTransform = new ScaleTransform(1.2, 1.2);
                    border.RenderTransform = scaleTransform;
                    border.RenderTransformOrigin = new Point(0.5, 0.5);
                    
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(150, 124, 58, 237));
                    border.BorderThickness = new Thickness(3);
                    
                    var animation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.2,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
                };
                
                colorButton.MouseLeave += (s, e) => 
                {
                    var border = (Border)s;
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    border.RenderTransform = scaleTransform;
                    
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                    border.BorderThickness = new Thickness(2);
                    
                    var animation = new DoubleAnimation
                    {
                        From = 1.2,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(250),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
                };
                
                colorButton.MouseLeftButtonUp += (s, e) => 
                {
                    var selectedChromaData = (ChromaData)((Border)s).Tag;
                    selectedChroma = selectedChromaData;
                    
                    foreach (UIElement element in ChampionsModalChromaButtons.Children)
                    {
                        if (element is Border border)
                        {
                            border.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                            border.BorderThickness = new Thickness(2);
                        }
                    }
                    
                    var clickedBorder = (Border)s;
                    clickedBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 34, 197, 94));
                    clickedBorder.BorderThickness = new Thickness(4);
                    
                    var pulseAnimation = new DoubleAnimation
                    {
                        From = 1.2,
                        To = 1.35,
                        Duration = TimeSpan.FromMilliseconds(150),
                        AutoReverse = true,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    var scaleTransform = clickedBorder.RenderTransform as ScaleTransform ?? new ScaleTransform(1.2, 1.2);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
                };
                
                ChampionsModalChromaButtons.Children.Add(colorButton);
            }
            
            ChampionsModalChromaPanel.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ChampionsModalChromaPanel.Visibility = Visibility.Collapsed;
        }
    }

    private ChromaData selectedChroma = null;
    private string currentChampionName = null;
    private CancellationTokenSource? _championsModalCancellationTokenSource;

    private void PopulatePreviewChromas(List<ChromaData> chromas)
    {
        try
        {
            ChampionsModalPreviewChromaButtons.Children.Clear();
            selectedChroma = null;
            
            if (chromas == null || chromas.Count == 0)
            {
                ChampionsModalPreviewChromaPanel.Visibility = Visibility.Collapsed;
                return;
            }

foreach (var chroma in chromas)
            {
                var colorCircle = new Button
                {
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(4),
                    Cursor = Cursors.Hand,
                    Tag = chroma,
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                    Content = "",
                    Style = null
                };
                
                var template = new ControlTemplate(typeof(Button));
                var outerBorder = new FrameworkElementFactory(typeof(Border));
                outerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
                outerBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                outerBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
                outerBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
                
                var shadow = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 3,
                    Opacity = 0.5,
                    BlurRadius = 6
                };
                outerBorder.SetValue(Border.EffectProperty, shadow);
                
                template.VisualTree = outerBorder;
                colorCircle.Template = template;
                
                var tooltipContent = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    MaxWidth = 280
                };
                
                if (!string.IsNullOrEmpty(chroma.ChromaPath))
                {
                    var chromaImage = new Image
                    {
                        Width = 180,
                        Height = 240,
                        Stretch = Stretch.UniformToFill,
                        Margin = new Thickness(0, 0, 0, 12)
                    };
                    
                    chromaImage.Clip = new RectangleGeometry(new Rect(0, 0, 180, 240), 8, 8);
                    
                    try
                    {
                        
                        var imageUrl = chroma.ChromaPath.ToLowerInvariant();
                        if (imageUrl.StartsWith("/lol-game-data/assets/"))
                        {
                            imageUrl = imageUrl.Substring("/lol-game-data/assets/".Length);
                        }
                        var fullImageUrl = $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/{imageUrl}";
                        
                        var wrightSkinsUrl = $"WrightUtils.E/chroma_{chroma.Id}.jpg";
                        
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(wrightSkinsUrl);
                        bitmap.EndInit();
                        chromaImage.Source = bitmap;
                        
                    }
                    catch (Exception ex)
                    {
                    }
                    
                    tooltipContent.Children.Add(chromaImage);
                }
                
                var nameBlock = new TextBlock
                {
                    Text = chroma.Name,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                tooltipContent.Children.Add(nameBlock);
                
                if (!string.IsNullOrEmpty(chroma.Description))
                {
                    var descBlock = new TextBlock
                    {
                        Text = chroma.Description,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 8),
                        LineHeight = 16
                    };
                    tooltipContent.Children.Add(descBlock);
                }
                
                colorCircle.ToolTip = new ToolTip
                {
                    Content = tooltipContent,
                    Background = new LinearGradientBrush(
                        Color.FromRgb(30, 41, 59),
                        Color.FromRgb(51, 65, 85),
                        90),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 124, 58, 237)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(16, 12, 16, 12),
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                    VerticalOffset = -8,
                    HasDropShadow = true
                };
                
                if (chroma.Colors != null && chroma.Colors.Count > 0)
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(chroma.Colors[0]);
                        colorCircle.Background = new SolidColorBrush(color);
                    }
                    catch
                    {
                        colorCircle.Background = new SolidColorBrush(Colors.Gray);
                    }
                }
                else
                {
                    colorCircle.Background = new SolidColorBrush(Colors.Gray);
                }
                
                colorCircle.MouseEnter += (s, e) => 
                {
                    var button = (Button)s;
                    var scaleTransform = new ScaleTransform(1.15, 1.15);
                    button.RenderTransform = scaleTransform;
                    button.RenderTransformOrigin = new Point(0.5, 0.5);
                    
                    button.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 124, 58, 237));
                    button.BorderThickness = new Thickness(3);
                    
                    var animation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.15,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
                };
                
                colorCircle.MouseLeave += (s, e) => 
                {
                    var button = (Button)s;
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    button.RenderTransform = scaleTransform;
                    
                    var buttonChromaData = (ChromaData)button.Tag;
                    bool isSelected = selectedChroma != null && buttonChromaData.Id == selectedChroma.Id;
                    
                    if (isSelected)
                    {
                        button.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 34, 197, 94));
                        button.BorderThickness = new Thickness(4);
                    }
                    else
                    {
                        button.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                        button.BorderThickness = new Thickness(2);
                    }
                    
                    var animation = new DoubleAnimation
                    {
                        From = 1.15,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
                };
                
                colorCircle.Click += (s, e) => 
                {
                    try
                    {
                        
                        var clickedButton = (Button)s;
                        var selectedChromaData = (ChromaData)clickedButton.Tag;
                        selectedChroma = selectedChromaData;

var pulseAnimation = new DoubleAnimation
                        {
                            From = 1.15,
                            To = 1.25,
                            Duration = TimeSpan.FromMilliseconds(100),
                            AutoReverse = true,
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        
                        var scaleTransform = new ScaleTransform(1.15, 1.15);
                        clickedButton.RenderTransform = scaleTransform;
                        clickedButton.RenderTransformOrigin = new Point(0.5, 0.5);
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
                        
                        var chromaButtonsPanel = FindName("ChampionsModalPreviewChromaButtons") as StackPanel;
                        if (chromaButtonsPanel != null)
                        {
                            foreach (UIElement element in chromaButtonsPanel.Children)
                            {
                                if (element is Button btn)
                                {
                                    btn.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                                    btn.BorderThickness = new Thickness(2);
                                }
                            }
                        }
                        
                        clickedButton.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 34, 197, 94));
                        var borderAnimation = new ThicknessAnimation
                        {
                            To = new Thickness(4),
                            Duration = TimeSpan.FromMilliseconds(200),
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        
                        clickedButton.BeginAnimation(Button.BorderThicknessProperty, borderAnimation);
                        
                    }
                    catch (Exception ex)
                    {
                    }
                };
                
                var chromaButtonsPanel = FindName("ChampionsModalPreviewChromaButtons") as StackPanel;
                chromaButtonsPanel?.Children.Add(colorCircle);
            }
            
            var chromaPanel = FindName("ChampionsModalPreviewChromaPanel") as UIElement;
            if (chromaPanel != null)
                chromaPanel.Visibility = Visibility.Visible;

AddChromaInfoText();
            
        }
        catch (Exception)
        {
            var chromaPanel = FindName("ChampionsModalPreviewChromaPanel") as UIElement;
            if (chromaPanel != null)
                chromaPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void AddChromaInfoText()
    {
        try
        {
            
            var chromaPanel = FindName("ChampionsModalPreviewChromaPanel") as FrameworkElement;
            var parentPanel = chromaPanel?.Parent as StackPanel;
            
            if (parentPanel != null)
            {
                
                var existingInfoTexts = parentPanel.Children.OfType<TextBlock>()
                    .Where(tb => tb.Text.Contains("Chroma'yý önizlemek için"))
                    .ToList();

foreach (var text in existingInfoTexts)
                {
                    parentPanel.Children.Remove(text);
                }
                
                var infoText = new TextBlock
                {
                    Text = LocalizationService.Instance.Translate("chromaPreviewInstruction"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0),
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                var chromaPanelIndex = chromaPanel != null ? parentPanel.Children.IndexOf(chromaPanel) : -1;
                
                if (chromaPanelIndex >= 0)
                {
                    parentPanel.Children.Insert(chromaPanelIndex + 1, infoText);
                }
                else
                {
                }
            }
            else
            {
            }
        }
        catch (Exception)
        {
        }
    }

    private void UpdateChampionsModalDownloadButtonForDynamic(ChampionSkinData skin)
    {
        var downloadUrl = skin.GitHubDownloadUrl;
        
        if (string.IsNullOrEmpty(downloadUrl) && DataContext is MainViewModel viewModel && viewModel.SelectedSkin != null)
        {
            downloadUrl = viewModel.SelectedSkin.WadURL;
        }
        
        if (!string.IsNullOrEmpty(downloadUrl))
        {
            ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("DownloadButtonText");
            ChampionsModalDownloadButton.IsEnabled = true;
            ChampionsModalDownloadButton.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
        }
        else
        {
            ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("NotAvailableText");
            ChampionsModalDownloadButton.IsEnabled = false;
            ChampionsModalDownloadButton.Background = new SolidColorBrush(Color.FromRgb(107, 114, 128));
        }
    }

    private void UpdateChampionsModalDownloadButton(Skin skin)
    {
        if (DataContext is MainViewModel viewModel)
        {
            if (string.IsNullOrEmpty(skin.WadURL))
            {
                ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("NoLinkText");
                ChampionsModalDownloadButton.IsEnabled = false;
                ChampionsModalDownloadButton.Background = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                return;
            }

            var existingSkin = viewModel.InstalledSkins.FirstOrDefault(s => 
                s.Id == skin.Id || (s.Name == skin.Name && s.Champion == skin.Champion));

            if (existingSkin != null)
            {
                if (existingSkin.Version == skin.Version)
                {
                    ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("ButtonInstalled");
                    ChampionsModalDownloadButton.IsEnabled = false;
                    ChampionsModalDownloadButton.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                }
                else
                {
                    ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("ButtonUpdate");
                    ChampionsModalDownloadButton.IsEnabled = true;
                    ChampionsModalDownloadButton.Background = new LinearGradientBrush(
                        new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(124, 58, 237), 0.0),
                            new GradientStop(Color.FromRgb(139, 92, 246), 0.5),
                            new GradientStop(Color.FromRgb(109, 40, 217), 1.0)
                        });
                }
            }
            else
            {
                ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("DownloadButtonText");
                ChampionsModalDownloadButton.IsEnabled = true;
                ChampionsModalDownloadButton.Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(124, 58, 237), 0.0),
                        new GradientStop(Color.FromRgb(139, 92, 246), 0.5),
                        new GradientStop(Color.FromRgb(109, 40, 217), 1.0)
                    });
            }
        }
    }

    private void CloseChampionsModal(object sender, RoutedEventArgs e)
    {
        _championsModalCancellationTokenSource?.Cancel();
        _championsModalCancellationTokenSource?.Dispose();
        _championsModalCancellationTokenSource = null;

var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(200),
            AccelerationRatio = 0.3,
            DecelerationRatio = 0.3
        };

        fadeOut.Completed += (s, args) =>
        {
            ChampionsModalOverlay.Visibility = Visibility.Collapsed;
            ChampionsModalOverlay.Opacity = 1.0;
            
            if (FindName("ChampionsModalLoadingOverlay") is Border loadingOverlay)
            {
                loadingOverlay.Visibility = Visibility.Collapsed;
                loadingOverlay.Opacity = 1.0;
            }
            
            selectedChroma = null;
            currentChampionName = null;
        };

        ChampionsModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private async void DownloadSkinFromChampionsModal(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedSkin != null)
        {
            var skin = viewModel.SelectedSkin;
            
            if (selectedChroma != null)
            {
                await DownloadChromaFromGitHub(skin, selectedChroma, "");
                return;
            }
            
            if (string.IsNullOrEmpty(skin.WadURL))
            {
                return;
            }

            try
            {
                string currentServer = await GetCurrentDownloadServer();
                
                ChampionsModalDownloadButton.Content = "? DOWNLOADING...";
                ChampionsModalDownloadButton.IsEnabled = false;

                var downloadLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var downloadWrightSkinsPath = System.IO.Path.Combine(downloadLocalAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
                
                var safeSkinName = CleanSkinNameForFileSystem(skin.Name);
                
                var downloadSkinFolderPath = System.IO.Path.Combine(downloadWrightSkinsPath, safeSkinName);
                var downloadWadFolderPath = System.IO.Path.Combine(downloadSkinFolderPath, "WAD");
                var metaFolderPath = System.IO.Path.Combine(downloadSkinFolderPath, "META");

Directory.CreateDirectory(downloadWadFolderPath);
                Directory.CreateDirectory(metaFolderPath);
                
                DirectoryInfo skinFolder = new DirectoryInfo(downloadSkinFolderPath);
                skinFolder.Attributes |= FileAttributes.Hidden | FileAttributes.System;

                string? actualChampionCodename = null;
                var championName = skin.Champion;
                if (string.IsNullOrEmpty(championName))
                {
                    var urlParts = skin.WadURL.Split('/');
                    var skinsIndex = Array.IndexOf(urlParts, "skins");
                    if (skinsIndex >= 0 && skinsIndex + 1 < urlParts.Length)
                    {
                        championName = Uri.UnescapeDataString(urlParts[skinsIndex + 1]);
                    }
                    else
                    {
                        var words = skin.Name.Split(' ');
                        championName = words.LastOrDefault() ?? "Unknown";
                    }
                }
                actualChampionCodename = GetChampionCodename(championName);

                if (currentServer == "WrightSkins")
                {
                    
                    var wrightSkinsUrl = await ConvertGitHubUrlToWrightSkinsUrl(skin.WadURL, championName, skin.Name);
                    
                    await DownloadFolderFromWrightSkins(wrightSkinsUrl, downloadSkinFolderPath, skin.Name);
                }
                else
                {
                    
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync(skin.WadURL, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        var totalBytes = response.Content.Headers.ContentLength ?? 0;
                        var downloadedBytes = 0L;
                        
                        var fileName = System.IO.Path.GetFileName(skin.WadURL) ?? $"{actualChampionCodename}.wad.client";
                        var isZipFile = fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
                        
                        var downloadedFilePath = isZipFile ? 
                            System.IO.Path.Combine(downloadWadFolderPath, fileName) : 
                            System.IO.Path.Combine(downloadWadFolderPath, $"{actualChampionCodename}.wad.client");
                        
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(downloadedFilePath))
                        {
                            var buffer = new byte[8192];
                            int bytesRead;
                            
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                downloadedBytes += bytesRead;
                                
                                if (totalBytes > 0)
                                {
                                    var percentage = (int)((downloadedBytes * 100) / totalBytes);
                                    ChampionsModalDownloadButton.Content = $"? {percentage}%";
                                }
                            }
                        }
                        
                        if (isZipFile)
                        {
                            ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("ButtonExtracting");
                            
                                try
                            {
                                var extractPath = System.IO.Path.Combine(downloadWadFolderPath, "extracted");
                                Directory.CreateDirectory(extractPath);
                                
                                System.IO.Compression.ZipFile.ExtractToDirectory(downloadedFilePath, extractPath);
                                
                                var extractedWadPath = System.IO.Path.Combine(extractPath, "WAD");
                                if (Directory.Exists(extractedWadPath))
                                {
                                    
                                    var wadFiles = Directory.GetFiles(extractedWadPath, "*.*", SearchOption.AllDirectories);
                                    
                                    foreach (var sourceFile in wadFiles)
                                    {
                                        var fileNameOnly = System.IO.Path.GetFileName(sourceFile);
                                        var targetFile = System.IO.Path.Combine(downloadWadFolderPath, fileNameOnly);
                                        
                                        File.Copy(sourceFile, targetFile, true);
                                    }
                                    
                                }
                                else
                                {
                                }
                                
                                Directory.Delete(extractPath, true);
                                
                                File.Delete(downloadedFilePath);
                            }
                            catch (Exception extractEx)
                            {
                            }
                        }
                    }
                }

                var infoData = new
                {
                    Author = "WRIGHTSKINS",
                    Description = "WRIGHTSKINS", 
                    Heart = "https://www.bontur.com.tr/discord",
                    Home = "https://www.bontur.com.tr/discord",
                    Name = "WRIGHTSKINS",
                    Version = "WRIGHTSKINS"
                };
                
                var infoJson = JsonConvert.SerializeObject(infoData, Formatting.Indented);
                var infoFilePath = System.IO.Path.Combine(metaFolderPath, "info.json");
                await File.WriteAllTextAsync(infoFilePath, infoJson);

                var versionData = new[] { new { version = skin.Version, isChampion = skin.IsChampion } };
                var versionJson = JsonConvert.SerializeObject(versionData, Formatting.Indented);
                var versionFilePath = System.IO.Path.Combine(downloadSkinFolderPath, "version.json");
                await File.WriteAllTextAsync(versionFilePath, versionJson);

                await UpdateInstalledSkinsJson(skin, isFromChampionsPage: true, championCodename: actualChampionCodename);

                if (currentServer == "Github")
                {
                    ChampionsModalDownloadButton.Content = "BUILDING .WAD FILE";
                    try
                    {
                        await ProcessChromaSupport(downloadSkinFolderPath);
                    }
                    catch (Exception chromaEx)
                    {
                    }
                }

                await viewModel.LoadDataCommand.ExecuteAsync(null);

                UpdateChampionsModalDownloadButton(skin);
                
            }
            catch (Exception ex)
            {
                
                UpdateChampionsModalDownloadButton(skin);
            }
        }
    }

    private async void PreviewSkinFromChampionsModal(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedSkin != null)
        {
            var skin = viewModel.SelectedSkin;
            
            if (!string.IsNullOrEmpty(skin.VideoPreview))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = skin.VideoPreview,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    await ShowErrorModalAsync("Video Önizleme Hatasý", $"Video önizleme açýlýrken hata: {ex.Message}");
                }
            }
            else if (!string.IsNullOrEmpty(skin.YoutubePreview))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = skin.YoutubePreview,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    await ShowErrorModalAsync("YouTube Önizleme Hatasý", $"YouTube önizleme açýlýrken hata: {ex.Message}");
                }
            }
            else
            {
                await ShowCustomModalAsync("Önizleme Bulunamadý", "Bu skin için önizleme bulunamadý.", ModalType.Information);
            }
        }
    }

    private async Task DownloadChromaFromGitHub(Skin skin, ChromaData chroma, string actualSkinName)
    {
        try
        {
            string currentServer = await GetCurrentDownloadServer();
            
            ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("ButtonDownloadingChroma");
            ChampionsModalDownloadButton.IsEnabled = false;

            var championName = currentChampionName;
            var skinName = actualSkinName;
            var chromaId = chroma.Id;
            
            if (string.IsNullOrEmpty(skinName))
            {
                skinName = chroma.Name;
                
                if (!string.IsNullOrEmpty(championName) && skinName.Contains(championName))
                {
                }
                
            }

if (string.IsNullOrEmpty(championName))
            {
                return;
            }

            var downloadLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var downloadWrightSkinsPath = System.IO.Path.Combine(downloadLocalAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            
            var cleanSkinNameForFolder = CleanSkinNameForFileSystem(skinName);
            
            var chromaFolderName = $"{cleanSkinNameForFolder} - {chromaId}";
            var downloadSkinFolderPath = System.IO.Path.Combine(downloadWrightSkinsPath, chromaFolderName);
            var downloadWadFolderPath = System.IO.Path.Combine(downloadSkinFolderPath, "WAD");
            var metaFolderPath = System.IO.Path.Combine(downloadSkinFolderPath, "META");
            
            Directory.CreateDirectory(downloadWadFolderPath);
            Directory.CreateDirectory(metaFolderPath);
            
            DirectoryInfo skinFolder = new DirectoryInfo(downloadSkinFolderPath);
            skinFolder.Attributes |= FileAttributes.Hidden | FileAttributes.System;

            if (currentServer == "WrightSkins")
            {
                
                var cleanSkinName = skinName;
                cleanSkinName = cleanSkinName.Replace(":", "");
                char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
                foreach (char c in invalidChars)
                {
                    if (c != ':')
                    {
                        cleanSkinName = cleanSkinName.Replace(c, '_');
                    }
                }

var encodedSkinName = Uri.EscapeDataString(cleanSkinName);
                
                var wrightSkinsChromaUrl = $"WrightUtils.E/{championName}/chromas/{encodedSkinName}/{encodedSkinName}%20{chromaId}/WAD/";
                
                await DownloadChromaFolderFromWrightSkins(wrightSkinsChromaUrl, downloadSkinFolderPath, championName);
            }
            else
            {
                
                var cleanSkinName = skinName
                    .Replace(":", "")
                    .Replace("/", " ");
                var encodedSkinName = Uri.EscapeDataString(cleanSkinName);
                var directDownloadUrl = $"https://raw.githubusercontent.com/darkseal-org/lol-skins/main/skins/{championName}/chromas/{encodedSkinName}/{encodedSkinName}%20{chromaId}.zip";

using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "WrightSkins-Launcher");

var zipResponse = await httpClient.GetAsync(directDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                zipResponse.EnsureSuccessStatusCode();

                var totalBytes = zipResponse.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;
                var targetFileName = $"{skinName} {chromaId}.zip";
                var zipFilePath = System.IO.Path.Combine(downloadWadFolderPath, targetFileName);
                
                using (var contentStream = await zipResponse.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(zipFilePath))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;
                        
                        if (totalBytes > 0)
                        {
                            var percentage = (int)((downloadedBytes * 100) / totalBytes);
                            ChampionsModalDownloadButton.Content = $"? {percentage}%";
                        }
                    }
                }

ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("ButtonExtracting");
                
                try
                {
                    var extractPath = System.IO.Path.Combine(downloadWadFolderPath, "extracted");
                    Directory.CreateDirectory(extractPath);
                    
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                    
                    var extractedWadPath = System.IO.Path.Combine(extractPath, "WAD");
                    if (Directory.Exists(extractedWadPath))
                    {
                        
                        var wadFiles = Directory.GetFiles(extractedWadPath, "*.*", SearchOption.AllDirectories);
                        
                        foreach (var sourceFile in wadFiles)
                        {
                            var fileName = System.IO.Path.GetFileName(sourceFile);
                            var targetFile = System.IO.Path.Combine(downloadWadFolderPath, fileName);
                            
                            File.Copy(sourceFile, targetFile, true);
                        }
                        
                    }
                    else
                    {
                    }
                    
                    Directory.Delete(extractPath, true);
                    
                    File.Delete(zipFilePath);
                }
                catch (Exception extractEx)
                {
                    }
                }
            }
            var infoData = new
            {
                Author = "WRIGHTSKINS",
                Description = $"WRIGHTSKINS - {chroma.Name} Chroma", 
                Heart = "https://www.bontur.com.tr/discord",
                Home = "https://www.bontur.com.tr/discord",
                Name = "WRIGHTSKINS",
                Version = "WRIGHTSKINS"
            };
            
            var infoJson = JsonConvert.SerializeObject(infoData, Formatting.Indented);
            var infoFilePath = System.IO.Path.Combine(metaFolderPath, "info.json");
            await File.WriteAllTextAsync(infoFilePath, infoJson);

            var versionData = new[] { new { version = $"Chroma ID: {chromaId}", isChampion = false } };
            var versionJson = JsonConvert.SerializeObject(versionData, Formatting.Indented);
            var versionFilePath = System.IO.Path.Combine(downloadSkinFolderPath, "version.json");
            await File.WriteAllTextAsync(versionFilePath, versionJson);

            await UpdateInstalledSkinsJsonForChroma(skin, chroma, chromaFolderName);

            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.LoadDataCommand.ExecuteAsync(null);
            }

            if (currentServer == "Github")
            {
                ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("ButtonAddingChromaSupport");
                try
                {
                    await ProcessChromaSupport(downloadSkinFolderPath);
                }
                catch (Exception chromaEx)
                {
                }
            }

            ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("ButtonInstalled");
            ChampionsModalDownloadButton.IsEnabled = false;
            ChampionsModalDownloadButton.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            
        }
        catch (Exception ex)
        {
            
            ChampionsModalDownloadButton.Content = LocalizationService.Instance.Translate("DownloadButtonText");
            ChampionsModalDownloadButton.IsEnabled = true;
        }
    }

    private async Task UpdateInstalledSkinsJsonForChroma(Skin skin, ChromaData chroma, string folderName)
    {
        try
        {
            var updateLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var updateWrightSkinsPath = System.IO.Path.Combine(updateLocalAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var updateInstalledJsonPath = System.IO.Path.Combine(updateWrightSkinsPath, "installed.json");

            List<InstalledSkin> installedSkins;

            if (File.Exists(updateInstalledJsonPath))
            {
                var existingJson = await File.ReadAllTextAsync(updateInstalledJsonPath);
                installedSkins = JsonConvert.DeserializeObject<List<InstalledSkin>>(existingJson) ?? new List<InstalledSkin>();
            }
            else
            {
                installedSkins = new List<InstalledSkin>();
            }

            var cleanSkinName = skin.Name.Replace(":", "");
            var chromaName = $"{cleanSkinName} - {chroma.Id}";

var existingChroma = installedSkins.FirstOrDefault(s => s.Name == chromaName);
            
            if (existingChroma != null)
            {
                existingChroma.Version = $"Chroma ID: {chroma.Id}";
                existingChroma.Champion = currentChampionName ?? skin.Champion;
                existingChroma.InstallDate = DateTime.Now;
                existingChroma.UpToDate = true;
                existingChroma.IsBuilded = true;
                existingChroma.IsCustom = false;
            }
            else
            {
                var combinedId = skin.Id * 10000 + chroma.Id;
                
                installedSkins.Add(new InstalledSkin
                {
                    Id = combinedId,
                    Name = chromaName,
                    Version = $"Chroma ID: {chroma.Id}",
                    Champion = currentChampionName ?? skin.Champion,
                    ImageCard = skin.ImageCard,
                    WadFile = System.IO.Path.Combine("WAD", $"{currentChampionName ?? skin.Champion}.wad.client"),
                    IsChampion = false,
                    UpToDate = true,
                    InstallDate = DateTime.Now,
                    IsBuilded = true,
                    IsCustom = false
                });
                
            }

            var updatedJson = JsonConvert.SerializeObject(installedSkins, Formatting.Indented);
            await File.WriteAllTextAsync(updateInstalledJsonPath, updatedJson);
        }
        catch (Exception ex)
        {
        }
    }

    private void OnChampionsModalImageClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SelectedSkin != null)
        {
            CloseChampionsModal(null, null);
            
            viewModel.IsDownloadingFromSpecialPage = false;
            
            ShowPreview();
        }
    }

    private string GetRarityIconUrl(string rarity)
    {
        if (string.IsNullOrEmpty(rarity))
            return "https://raw.communitydragon.org/latest/plugins/rcp-fe-lol-collections/global/default/images/control-panel/icon-legacy.png";

        return rarity.ToLower() switch
        {
            "kepic" or "epic" => "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/rarity-gem-icons/epic.png",
            "kmythic" or "mythic" => "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/rarity-gem-icons/mythic.png",
            "kultimate" or "ultimate" => "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/rarity-gem-icons/ultimate.png",
            "ktranscendent" or "transcendent" => "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/rarity-gem-icons/transcendent.png",
            "klegendary" or "legendary" => "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/rarity-gem-icons/legendary.png",
            "kstandard" or "standard" => "https://raw.communitydragon.org/latest/plugins/rcp-fe-lol-collections/global/default/images/control-panel/icon-legacy.png",
            _ => "https://raw.communitydragon.org/latest/plugins/rcp-fe-lol-collections/global/default/images/control-panel/icon-legacy.png"
        };
    }

    private string GetDisplayRarity(string rarity)
    {
        if (string.IsNullOrEmpty(rarity))
            return LocalizationService.Instance.Translate("rarityStandard");

        return rarity.ToLower() switch
        {
            "kepic" => LocalizationService.Instance.Translate("rarityEpic"),
            "kmythic" => LocalizationService.Instance.Translate("rarityMythic"), 
            "kultimate" => LocalizationService.Instance.Translate("rarityUltimate"),
            "ktranscendent" => LocalizationService.Instance.Translate("rarityTranscendent"),
            "klegendary" => LocalizationService.Instance.Translate("rarityLegendary"),
            "knorarity" => LocalizationService.Instance.Translate("rarityStandard"),
            _ => rarity
        };
    }

    private void UpdateInjectButtonUI()
    {
        Dispatcher.Invoke(() =>
        {
            var selectedCount = GetSelectedSkinsCount();
            
            if (_isInjecting && !_processStarted)
            {
                var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = LocalizationService.Instance.Translate("InjectingText"), 
                    FontSize = 14, 
                    FontWeight = FontWeights.Bold 
                });
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = string.Format(LocalizationService.Instance.Translate("SkinsSelectedText"), selectedCount), 
                    FontSize = 10, 
                    Opacity = 0.8, 
                    Margin = new Thickness(0, 2, 0, 0) 
                });
                
                InjectButton.Content = stackPanel;
                InjectButton.Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(245, 158, 11), 0.0),
                        new GradientStop(Color.FromRgb(251, 191, 36), 0.5),
                        new GradientStop(Color.FromRgb(217, 119, 6), 1.0)
                    });
                InjectButton.IsEnabled = false;
            }
            else if (_isInjecting && _processStarted)
            {
                var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = LocalizationService.Instance.Translate("StopText"), 
                    FontSize = 14, 
                    FontWeight = FontWeights.Bold 
                });
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = string.Format(LocalizationService.Instance.Translate("SkinsInjectedText"), selectedCount), 
                    FontSize = 10, 
                    Opacity = 0.8, 
                    Margin = new Thickness(0, 2, 0, 0) 
                });
                
                InjectButton.Content = stackPanel;
                InjectButton.Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(239, 68, 68), 0.0),
                        new GradientStop(Color.FromRgb(248, 113, 113), 0.5),
                        new GradientStop(Color.FromRgb(220, 38, 38), 1.0)
                    });
                InjectButton.IsEnabled = true;
            }
            else if (selectedCount == 0)
            {
                var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = LocalizationService.Instance.Translate("InjectButtonMainText"), 
                    FontSize = 14, 
                    FontWeight = FontWeights.Bold 
                });
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = LocalizationService.Instance.Translate("ZeroSkinsSelectedText"), 
                    FontSize = 10, 
                    Opacity = 0.8, 
                    Margin = new Thickness(0, 2, 0, 0) 
                });
                
                InjectButton.Content = stackPanel;
                InjectButton.Background = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                InjectButton.IsEnabled = false;
            }
            else
            {
                var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = LocalizationService.Instance.Translate("InjectButtonMainText"), 
                    FontSize = 14, 
                    FontWeight = FontWeights.Bold 
                });
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = string.Format(LocalizationService.Instance.Translate("SkinsSelectedText"), selectedCount), 
                    FontSize = 10, 
                    Opacity = 0.8, 
                    Margin = new Thickness(0, 2, 0, 0) 
                });
                
                InjectButton.Content = stackPanel;
                InjectButton.Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(124, 58, 237), 0.0),
                        new GradientStop(Color.FromRgb(139, 92, 246), 0.5),
                        new GradientStop(Color.FromRgb(109, 40, 217), 1.0)
                    });
                InjectButton.IsEnabled = true;
            }
        });
    }

    public void UpdateSelectedSkinsCount()
    {
        UpdateInjectButtonUI();
    }

    private int GetSelectedSkinsCount()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var profilePath = System.IO.Path.Combine(wrightPath, "Wright.profile");

            if (!File.Exists(profilePath))
                return 0;

            var lines = File.ReadAllLines(profilePath);
            
            var skinNames = lines
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .Select(line => line.Trim())
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

return skinNames.Count;
        }
        catch (Exception ex)
        {
            return 0;
        }
    }

    public void RefreshInjectButton()
    {
        UpdateSelectedSkinsCount();
    }

    private async void CheckAndAutoSelectExistingSkin(Models.LobbySkin lobbySkin)
    {
        try
        {
            
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var installedJsonPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", "installed.json");
            
            if (!File.Exists(installedJsonPath))
            {
                return;
            }
            
            var installedJsonContent = await File.ReadAllTextAsync(installedJsonPath);
            var installedSkins = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InstalledSkin>>(installedJsonContent);
            
            if (installedSkins == null)
            {
                return;
            }
            
            var existingSkin = installedSkins.FirstOrDefault(skin => 
                skin.Name.Equals(lobbySkin.SkinName, StringComparison.OrdinalIgnoreCase) ||
                skin.Name.Contains(lobbySkin.SkinName) ||
                lobbySkin.SkinName.Contains(skin.Name));
                
            if (existingSkin != null)
            {
                
                lobbySkin.IsSelected = true;
                
                UpdateSelectedSkinsCount();
                
                if (DataContext is MainViewModel viewModel)
                {
                    _ = viewModel.UpdateWrightProfileAsync();
                }
                
                _socketIOService?.TriggerNewModsInLobbyDetected();
                
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async Task CheckAndDownloadExistingSkin(RealtimeSkinData skinData, string uploaderUserId)
    {
        try
        {
            if (uploaderUserId == _currentUser?.UserID.ToString())
            {
                return;
            }

            var skinName = skinData.Name;

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var skinsDirectory = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var extractPath = System.IO.Path.Combine(skinsDirectory, skinName);

            if (Directory.Exists(extractPath))
            {
                
                await Dispatcher.InvokeAsync(() =>
                {
                    AutoSelectDownloadedSkin(skinName);
                });
                return;
            }

            var installedJsonPath = System.IO.Path.Combine(skinsDirectory, "installed.json");
            if (File.Exists(installedJsonPath))
            {
                var installedJsonContent = await File.ReadAllTextAsync(installedJsonPath);
                var installedSkins = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InstalledSkin>>(installedJsonContent);
                
                if (installedSkins?.Any(s => s.Name.Equals(skinName, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AutoSelectDownloadedSkin(skinName);
                    });
                    return;
                }
            }

}
        catch (Exception ex)
        {
        }
    }

    private void AutoSelectDownloadedSkin(string skinName)
    {
        try
        {
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel?.CurrentLobby != null)
                {
                    var lobbySkin = viewModel.CurrentLobby.LobbySkins.FirstOrDefault(s => 
                        s.SkinName.Equals(skinName, StringComparison.OrdinalIgnoreCase));
                    
                    if (lobbySkin != null && !lobbySkin.IsSelected)
                    {
                        lobbySkin.IsSelected = true;
                        UpdateSelectedSkinsCount();
                        
                        _ = viewModel.UpdateWrightProfileAsync();
                        
                        _socketIOService?.TriggerNewModsInLobbyDetected();
                        
                    }
                    else if (lobbySkin != null && lobbySkin.IsSelected)
                    {
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            });
        }
        catch (Exception ex)
        {
        }
    }

    private async void OnDiscordConnectClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentDiscordUser == null)
            {
                
                try
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
                    var discordLoginPath = System.IO.Path.Combine(wrightSkinsPath, "discord_login.json");
                    
                    if (File.Exists(discordLoginPath))
                    {
                        File.Delete(discordLoginPath);
                    }
                }
                catch (Exception ex)
                {
                }
                
                DiscordConnectButtonIcon.Text = "?";
                DiscordConnectButtonText.Text = LocalizationService.Instance.Translate("ConnectingText");
                DiscordConnectButton.IsEnabled = false;
                
                _discordService.StartAuthentication();
            }
            else
            {
                
                _discordService.Disconnect();
                _currentDiscordUser = null;
                OnPropertyChanged(nameof(IsDiscordConnected));
                _currentUser = null;
                
                var staffBadgeBorder = FindName("StaffBadgeBorder") as Border;
                if (staffBadgeBorder != null)
                {
                    staffBadgeBorder.Visibility = Visibility.Collapsed;
                }
                
                StopFriendsUpdates();
                
                try
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
                    var discordLoginPath = System.IO.Path.Combine(wrightSkinsPath, "discord_login.json");
                    
                    if (File.Exists(discordLoginPath))
                    {
                        File.Delete(discordLoginPath);
                    }
                }
                catch (Exception ex)
                {
                }
                
                UpdateDiscordConnectionUI();
                UpdateTopBarUserInfo();
                
                await ShowCustomModalAsync(
                    LocalizationService.Instance.Translate("discordDisconnectionTitle"), 
                    LocalizationService.Instance.Translate("discordDisconnectionMessage"), 
                    ModalType.Information
                );
            }
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(
                string.Format(LocalizationService.Instance.Translate("DiscordConnectionErrorMessage"), ex.Message),
                LocalizationService.Instance.Translate("DiscordConnectionError"));
            
            UpdateDiscordConnectionUI();
        }
    }

    public async void DisconnectDiscord()
    {
        try
        {
            if (_currentDiscordUser != null)
            {
                
                _discordService.Disconnect();
                _currentDiscordUser = null;
                OnPropertyChanged(nameof(IsDiscordConnected));
                _currentUser = null;
                
                var staffBadgeBorder = FindName("StaffBadgeBorder") as Border;
                if (staffBadgeBorder != null)
                {
                    staffBadgeBorder.Visibility = Visibility.Collapsed;
                }
                
                _isInWrightGuild = null;
                _lastGuildCheck = DateTime.MinValue;
                
                StopFriendsUpdates();
                
                var specialButton = FindName("SpecialButton") as UIElement;
                if (specialButton != null)
                {
                    specialButton.Visibility = Visibility.Collapsed;
                }
                
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.SpecialSkins?.Clear();
                }
                
                try
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
                    var discordLoginPath = System.IO.Path.Combine(wrightSkinsPath, "discord_login.json");
                    
                    if (File.Exists(discordLoginPath))
                    {
                        File.Delete(discordLoginPath);
                    }
                }
                catch (Exception ex)
                {
                }
                
                UpdateDiscordConnectionUI();
                UpdateTopBarUserInfo();
                
                await ShowCustomModalAsync(
                    LocalizationService.Instance.Translate("discordDisconnectionTitle"), 
                    LocalizationService.Instance.Translate("discordDisconnectionMessage"), 
                    ModalType.Information
                );
            }
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(
                LocalizationService.Instance.Translate("discordDisconnectionErrorTitle"), 
                string.Format(LocalizationService.Instance.Translate("discordDisconnectionErrorMessage"), ex.Message)
            );
        }
    }

    private async void OnDiscordUserAuthenticated(object? sender, DiscordUser user)
    {
        try
        {

            Dispatcher.BeginInvoke(async () =>
            {
                _currentDiscordUser = user;
                OnPropertyChanged(nameof(IsDiscordConnected));

                UpdateDiscordConnectionUI();
                UpdateTopBarUserInfo();
                
                _ = TestStaffSystemAsync();
                
                await CheckAndCacheGuildMembership(user.Id);

                await ShowSuccessModalAsync(
                    string.Format(LocalizationService.Instance.Translate("discordConnectionSuccessMessage"), user.DisplayName), 
                    LocalizationService.Instance.Translate("discordConnectionSuccessTitle")
                );
            });

            try
            {
                string? refreshToken = null;
                var discordServiceType = _discordService.GetType();
                var refreshTokenField = discordServiceType.GetField("_refreshToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (refreshTokenField != null)
                {
                    refreshToken = refreshTokenField.GetValue(_discordService) as string;
                }

                string hashedToken = GenerateRandomToken(24);

                static string GenerateRandomToken(int length)
                {
                    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    var data = new byte[length];
                    System.Security.Cryptography.RandomNumberGenerator.Fill(data);
                    var result = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        result[i] = chars[data[i] % chars.Length];
                    }
                    return new string(result);
                }

            var userModel = new WrightLauncher.Models.User
            {
                UserID = 0,
                DiscordID = user.Id,
                Username = user.Username,
                Tokens = 0,
                Friends = new System.Collections.Generic.List<int>(),
                HashedToken = hashedToken
            };

            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
                Directory.CreateDirectory(wrightSkinsPath);
                var discordLoginPath = System.IO.Path.Combine(wrightSkinsPath, "discord_login.json");
                var hashedTokenObj = new { hashedtoken = hashedToken };
                var hashedTokenJson = Newtonsoft.Json.JsonConvert.SerializeObject(hashedTokenObj, Newtonsoft.Json.Formatting.Indented);
                await System.IO.File.WriteAllTextAsync(discordLoginPath, hashedTokenJson);
                var fileInfo = new System.IO.FileInfo(discordLoginPath);
                fileInfo.Attributes |= System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System;
            }
            catch (Exception ex)
            {
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(userModel);
            
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await WrightSkinsApiService.PostAsync($"WrightUtils.E/register.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
                
                try
                {
                    var registerResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseString);
                    
                    if (registerResponse?.action != null && registerResponse?.hashedtoken != null)
                    {
                        string action = registerResponse.action;
                        string newHashedToken = registerResponse.hashedtoken;

if (action == "updated" || action == "created")
                        {
                            await UpdateDiscordLoginJsonAsync(newHashedToken);
                        }
                    }
                    else if (registerResponse?.message != null)
                    {
                        string message = registerResponse.message.ToString();
                        
                        if (message.Contains("successfully"))
                        {
                        }
                    }
                    else
                    {
                    }
                    
                    _ = TryHashTokenAutoLoginAsync();
                }
                catch (Exception parseEx)
                {
                }
                
                await FetchUserDetailsAsync(user.Id);
            }
            catch (Exception ex)
            {
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void OnDiscordAuthenticationFailed(object? sender, EventArgs e)
    {
        try
        {
            
            _isInWrightGuild = null;
            _lastGuildCheck = DateTime.MinValue;
            
            Dispatcher.BeginInvoke(async () =>
            {
                UpdateDiscordConnectionUI();
                
                await ShowErrorModalAsync(
                    LocalizationService.Instance.Translate("discordConnectionFailedTitle"), 
                    LocalizationService.Instance.Translate("discordConnectionFailedMessage")
                );
            });
        }
        catch (Exception ex)
        {
        }
    }

    private void UpdateDiscordConnectionUI()
    {
        try
        {
            if (_currentDiscordUser == null)
            {
                
                DiscordConnectButtonIcon.Text = "??";
                DiscordConnectButtonText.Text = LocalizationService.Instance.Translate("ConnectToDiscord");
                DiscordConnectButton.IsEnabled = true;
                DiscordConnectButton.Background = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(124, 58, 237), 0.0),
                        new GradientStop(Color.FromRgb(139, 92, 246), 0.5),
                        new GradientStop(Color.FromRgb(109, 40, 217), 1.0)
                    });
                
                DiscordUserPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                
                var connectedText = LocalizationService.Instance.Translate("ConnectedFromDiscord");
                var statusText = $"{connectedText}: {_currentDiscordUser.DisplayName}";
                if (_currentUser != null && _currentUser.UserID > 0)
                {
                    statusText += $"\nUser ID: {_currentUser.UserID}";
                }
                
                DiscordConnectButtonIcon.Text = "??";
                DiscordConnectButtonText.Text = LocalizationService.Instance.Translate("DisconnectFromDiscord");
                DiscordConnectButton.IsEnabled = true;
                DiscordConnectButton.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                
                DiscordUserDisplayName.Text = _currentDiscordUser.DisplayName;
                DiscordUserStatus.Text = LocalizationService.Instance.Translate("ConnectedFromDiscord");
                
                if (!string.IsNullOrEmpty(_currentDiscordUser.AvatarLink))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var avatarImage = await LoadBitmapImageWithTimeoutAsync(_currentDiscordUser.AvatarLink);
                            Dispatcher.Invoke(() =>
                            {
                                DiscordUserAvatar.ImageSource = avatarImage;
                            });
                        }
                        catch (Exception ex)
                        {
                        }
                    });
                }
                
                DiscordUserPanel.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void UpdateTopBarUserInfo()
    {
        try
        {
            
            var nicknameTextBlock = FindName("ProGamerNickname") as TextBlock;
            var profilePhotoImageBrush = FindName("ProfilePhotoImageBrush") as ImageBrush;
            var defaultProfileIcon = FindName("DefaultProfileIcon") as TextBlock;
            var discordProfilePhoto = FindName("DiscordProfilePhoto") as Border;

if (_currentDiscordUser != null)
            {
                
                if (nicknameTextBlock != null)
                {
                    nicknameTextBlock.Text = _currentDiscordUser.DisplayName;
                }
                
                _ = UpdateStaffBadgeAsync(_currentDiscordUser.Id);
                
                if (profilePhotoImageBrush != null && discordProfilePhoto != null && defaultProfileIcon != null)
                {
                    if (!string.IsNullOrEmpty(_currentDiscordUser.AvatarLink))
                    {
                        
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var avatarImage = await LoadBitmapImageWithTimeoutAsync(_currentDiscordUser.AvatarLink);
                                
                                if (avatarImage != null)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        profilePhotoImageBrush.ImageSource = avatarImage;
                                        
                                        discordProfilePhoto.Visibility = Visibility.Visible;
                                        defaultProfileIcon.Visibility = Visibility.Collapsed;
                                        
                                    });
                                }
                                else
                                {
                                }
                            }
                            catch (Exception ex)
                            {
                                
                                Dispatcher.Invoke(() =>
                                {
                                    if (discordProfilePhoto != null && defaultProfileIcon != null)
                                    {
                                        discordProfilePhoto.Visibility = Visibility.Collapsed;
                                        defaultProfileIcon.Visibility = Visibility.Visible;
                                    }
                                });
                            }
                        });
                    }
                    else
                    {
                        
                        if (discordProfilePhoto != null && defaultProfileIcon != null)
                        {
                            discordProfilePhoto.Visibility = Visibility.Collapsed;
                            defaultProfileIcon.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                }
            }
            else
            {
                
                if (nicknameTextBlock != null)
                {
                    nicknameTextBlock.Text = "ProGamer";
                }
                
                if (discordProfilePhoto != null && defaultProfileIcon != null)
                {
                    discordProfilePhoto.Visibility = Visibility.Collapsed;
                    defaultProfileIcon.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async Task UpdateStaffBadgeAsync(string userId)
    {
        try
        {
            var staffBadge = FindName("StaffBadge") as TextBlock;
            if (staffBadge != null)
            {
                var staffResponse = await StaffService.CheckStaffAsync(userId);
                bool isStaff = staffResponse?.IsStaff == true;
                staffBadge.Visibility = isStaff ? Visibility.Visible : Visibility.Collapsed;
                
                if (isStaff)
                {
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async Task TryHashTokenAutoLoginAsync()
    {
        try
        {
            
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            var discordLoginPath = System.IO.Path.Combine(wrightSkinsPath, "discord_login.json");
            
            if (!File.Exists(discordLoginPath))
            {
                return;
            }
            
            var discordLoginJson = await File.ReadAllTextAsync(discordLoginPath);
            
            var discordLoginData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(discordLoginJson);
            var hashedToken = discordLoginData?.hashedtoken?.ToString();
            
            if (string.IsNullOrEmpty(hashedToken))
            {
                return;
            }

var apiUrl = $"{WrightUtils.E}/launcher/api/hash_check.php?hashedtoken={hashedToken}";
            var response = await WrightSkinsApiService.GetAsync(apiUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var apiResponseJson = await response.Content.ReadAsStringAsync();
                var apiData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(apiResponseJson);
                    
                    var userId = apiData?.id?.ToString();
                    var discordId = apiData?.discord_id?.ToString();
                    
                    if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(discordId))
                    {
                        
                        _ = TestStaffSystemAsync(discordId);
                        
                        _ = UpdateStaffBadgeAsync(discordId);
                        
                        await AutoLoginWithUserIdAsync(int.Parse(userId), discordId);
                        
                        if (_currentUser == null)
                        {
                            
                            string username = _currentDiscordUser?.Username ?? $"User #{userId}";
                            
                            _currentUser = new User 
                            { 
                                UserID = int.Parse(userId), 
                                Username = username, 
                                DiscordID = discordId,
                                Tokens = 0,
                                Friends = new List<int>(),
                                HashedToken = ""
                            };
                            
                            if (DataContext is MainViewModel viewModel)
                            {
                                viewModel.CurrentUserId = _currentUser.UserID;
                            }
                            
                            StartFriendsUpdates();
                            
                            _ = LoadSpecialSkins();
                        }
                    }
                    else
                    {
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
    
    private async Task AutoLoginWithUserIdAsync(int userId, string discordId)
    {
        try
        {
            
            using (var httpClient = new HttpClient())
            {
                var discordLookupUrl = $"https://discordlookup.mesalytic.moe/v1/user/{discordId}";
                var response = await httpClient.GetAsync(discordLookupUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var discordUserJson = await response.Content.ReadAsStringAsync();
                    var discordUserData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(discordUserJson);
                    
                    if (discordUserData != null)
                    {
                        
                        var avatarId = discordUserData.avatar?.id?.ToString();
                        var avatarLink = discordUserData.avatar?.link?.ToString();
                        var isAnimated = discordUserData.avatar?.is_animated ?? false;

var discordUser = new DiscordUser
                        {
                            Id = discordId,
                            Username = discordUserData.username?.ToString() ?? "",
                            GlobalName = discordUserData.global_name?.ToString(),
                            Avatar = new DiscordAvatar
                            {
                                Id = avatarId ?? "",
                                Link = avatarLink ?? "",
                                IsAnimated = isAnimated
                            }
                        };
                        
                        Dispatcher.Invoke(() =>
                        {
                            _currentDiscordUser = discordUser;
                            OnPropertyChanged(nameof(IsDiscordConnected));
                            UpdateDiscordConnectionUI();
                            UpdateTopBarUserInfo();
                        });

await CheckAndCacheGuildMembership(discordId);
                        
                        SetupUserManually(userId, discordId, discordUser.Username);
                    }
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
    
    private string GetDiscordAvatarUrl(string userId, string? avatarHash)
    {
        if (string.IsNullOrEmpty(avatarHash))
        {
            return "https://cdn.discordapp.com/embed/avatars/0.png";
        }
        
        var extension = avatarHash.StartsWith("a_") ? "gif" : "png";
        return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.{extension}?size=128";
    }
    
    private async Task LoginWithUserIdAsync(int userId)
    {
        try
        {
            var loginData = new { id = userId };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

var response = await WrightSkinsApiService.PostAsync($"WrightUtils.E/login.php", content);

if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                    var loginResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                    
                    if (loginResponse?.success == true)
                    {
                        var user = loginResponse.user;
                        if (user != null)
                        {
                            var userModel = new User
                            {
                                UserID = (int)user.id,
                                DiscordID = user.discord_id?.ToString() ?? "",
                                Username = user.username?.ToString() ?? "",
                                Tokens = (int)(user.tokens ?? 0),
                                Friends = new System.Collections.Generic.List<int>(),
                                HashedToken = user.hashedtoken?.ToString() ?? ""
                            };
                            
                            Dispatcher.Invoke(() =>
                            {
                                _currentUser = userModel;
                                UpdateTopBarUserInfo();
                            });

StartFriendsUpdates();
                            
                            await FetchUserDetailsAsync(_currentDiscordUser?.Id);
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
            }
    }

    private void SetupUserManually(int userId, string discordId, string username)
    {
        try
        {
            
            var userModel = new User
            {
                UserID = userId,
                DiscordID = discordId,
                Username = username
            };

            _currentUser = userModel;
            
            if (DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.CurrentUserId = userId;
            }

StartFriendsUpdates();
            
            _ = LoadSpecialSkins();
        }
        catch (Exception ex)
        {
        }
    }
    
    private async Task FetchUserDetailsAsync(string? discordId)
    {
        if (string.IsNullOrEmpty(discordId))
        {
            return;
        }
        
        try
        {
            var userDetailsUrl = $"WrightUtils.E/?discord_id={discordId}";
            var response = await WrightSkinsApiService.GetAsync(userDetailsUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var userDetails = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);
                    
                    if (userDetails != null)
                    {
                        var userApiId = userDetails.id?.ToString();
                        
                        if (_currentUser != null && !string.IsNullOrEmpty(userApiId))
                        {
                            if (int.TryParse(userApiId, out int parsedUserId))
                            {
                                _currentUser.UserID = parsedUserId;
                                
                                if (DataContext is MainViewModel viewModel)
                                {
                                    viewModel.CurrentUserId = _currentUser.UserID;
                                }
                                
                                if (_friendsUpdateTimer == null)
                                {
                                    StartFriendsUpdates();
                                }
                                
                                Dispatcher.Invoke(() =>
                                {
                                    UpdateDiscordConnectionUI();
                                });
                            }
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
    }

    #region Fantome Modal Methods

    private void ShowFantomeModal(string filePath)
    {
        try
        {
            
            if (FantomeFileNameText != null)
            {
                FantomeFileNameText.Text = System.IO.Path.GetFileName(filePath);
            }
            
            ClearFantomeInputs();
            
            PopulateFantomeChampionDropdown();
            
            if (FantomeModalOverlay != null)
            {
                FantomeModalOverlay.Visibility = Visibility.Visible;
                FantomeModalOverlay.Opacity = 0;
                
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    AccelerationRatio = 0.3,
                    DecelerationRatio = 0.3
                };
                
                FantomeModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                
            }
        }
        catch (Exception ex)
        {
            Dispatcher.BeginInvoke(async () =>
            {
                await ShowErrorModalAsync("Modal Hatasý", $"Modal açýlýrken hata oluþtu: {ex.Message}");
            });
        }
    }

    private void ClearFantomeInputs()
    {
        if (FantomeSkinNameTextBox != null)
            FantomeSkinNameTextBox.Text = string.Empty;
        
        if (FantomeAuthorTextBox != null)
            FantomeAuthorTextBox.Text = string.Empty;
        
        if (FantomeChampionComboBox != null)
            FantomeChampionComboBox.SelectedItem = null;
        
        UpdateFantomeInstallButtonState();
    }

    private void PopulateFantomeChampionDropdown()
    {
        try
        {
            if (FantomeChampionComboBox != null && DataContext is MainViewModel viewModel)
            {
                FantomeChampionComboBox.ItemsSource = viewModel.Champions;
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void OnFantomeModalClose(object sender, RoutedEventArgs e)
    {
        try
        {
            
            if (FantomeModalOverlay != null)
            {
                FantomeModalOverlay.Visibility = Visibility.Collapsed;
            }
            
            _selectedFantomeFilePath = string.Empty;
            
        }
        catch (Exception ex)
        {
        }
    }

    private void OnFantomeSkinNameChanged(object sender, TextChangedEventArgs e)
    {
        ValidateFantomeInput(sender as TextBox);
        UpdateFantomeInstallButtonState();
    }

    private void OnFantomeAuthorChanged(object sender, TextChangedEventArgs e)
    {
        ValidateFantomeInput(sender as TextBox);
        UpdateFantomeInstallButtonState();
    }

    private void ValidateFantomeInput(TextBox? textBox)
    {
        if (textBox == null) return;

        var text = textBox.Text;
        var hasInvalidChars = _invalidCharacters.Any(invalidChar => text.Contains(invalidChar));

        if (hasInvalidChars)
        {
            foreach (var invalidChar in _invalidCharacters)
            {
                text = text.Replace(invalidChar, "");
            }
            
            var cursorPosition = textBox.CaretIndex;
            textBox.Text = text;
            textBox.CaretIndex = Math.Min(cursorPosition, text.Length);
            
        }

        var filteredText = new string(text.Where(c => IsLatinCharacter(c) || char.IsDigit(c) || char.IsWhiteSpace(c)).ToArray());
        
        if (filteredText != text)
        {
            var cursorPosition = textBox.CaretIndex;
            textBox.Text = filteredText;
            textBox.CaretIndex = Math.Min(cursorPosition, filteredText.Length);
            
        }
    }

    private static bool IsLatinCharacter(char c)
    {
        return (c >= 'A' && c <= 'Z') || 
               (c >= 'a' && c <= 'z') || 
               (c >= 'À' && c <= 'ÿ') ||
               c == ' ' || c == '-' || c == '_';
    }

    private void OnFantomeChampionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateFantomeInstallButtonState();
    }

    private void OnFantomeChampionDropDownOpened(object sender, EventArgs e)
    {
        PopulateFantomeChampionDropdown();
    }

    private void UpdateFantomeInstallButtonState()
    {
        try
        {
            bool canInstall = !string.IsNullOrWhiteSpace(FantomeSkinNameTextBox?.Text) &&
                            !string.IsNullOrWhiteSpace(FantomeAuthorTextBox?.Text);

            if (FantomeInstallButton != null)
            {
                FantomeInstallButton.IsEnabled = canInstall;
            }

        }
        catch (Exception ex)
        {
        }
    }

    private async void OnFantomeInstallClick(object sender, RoutedEventArgs e)
    {
        try
        {

            if (string.IsNullOrEmpty(_selectedFantomeFilePath))
            {
                await ShowErrorModalAsync(LocalizationService.Instance.Translate("FileNotSelected"));
                return;
            }

            var skinName = FantomeSkinNameTextBox?.Text?.Trim() ?? "";
            var author = FantomeAuthorTextBox?.Text?.Trim() ?? "";
            var selectedChampion = FantomeChampionComboBox?.SelectedItem as Champion;

            if (string.IsNullOrEmpty(skinName) || string.IsNullOrEmpty(author))
            {
                await ShowWarningModalAsync(LocalizationService.Instance.Translate("FillRequiredFields"), LocalizationService.Instance.Translate("MissingInfo"));
                return;
            }

            if (FantomeInstallButton != null)
                FantomeInstallButton.IsEnabled = false;

await ProcessFantomeFileAsync(skinName, author, selectedChampion?.Name);

            OnFantomeModalClose(sender, e);

            await ShowSuccessModalAsync($"'{skinName}' baþarýyla yüklendi!");
            
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync($"Yükleme sýrasýnda hata oluþtu: {ex.Message}");
        }
        finally
        {
            if (FantomeInstallButton != null)
                FantomeInstallButton.IsEnabled = true;
        }
    }

    private async Task ProcessFantomeFileAsync(string skinName, string author, string? championName)
    {
        try
        {

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var alrPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
            var skinFolderPath = System.IO.Path.Combine(alrPath, skinName);
            var installedJsonPath = System.IO.Path.Combine(alrPath, "installed.json");

            Directory.CreateDirectory(alrPath);

            if (Directory.Exists(skinFolderPath))
            {
                Directory.Delete(skinFolderPath, true);
            }
            Directory.CreateDirectory(skinFolderPath);

            using (var archive = ZipFile.OpenRead(_selectedFantomeFilePath))
            {
                
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = System.IO.Path.Combine(skinFolderPath, entry.FullName);
                    
                    if (entry.FullName.EndsWith("/"))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    var fileDirectory = System.IO.Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }

                    entry.ExtractToFile(destinationPath, true);
                }
                
            }

            await UpdateInstalledJsonAsync(installedJsonPath, skinName, author, championName);
            
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.LoadDataCommand.ExecuteAsync(null);
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task UpdateInstalledJsonAsync(string installedJsonPath, string skinName, string author, string? championName)
    {
        try
        {

            List<InstalledSkin> installedSkins = new();

            if (File.Exists(installedJsonPath))
            {
                var existingJson = await File.ReadAllTextAsync(installedJsonPath);
                installedSkins = JsonConvert.DeserializeObject<List<InstalledSkin>>(existingJson) ?? new();
            }

            var newSkin = new InstalledSkin
            {
                Id = installedSkins.Count > 0 ? installedSkins.Max(s => s.Id) + 1 : 1,
                Name = skinName,
                Author = author,
                Champion = championName ?? "Unknown",
                ImageCard = "",
                Version = "1.0",
                InstallDate = DateTime.Now,
                IsSelected = false,
                IsChampion = false,
                IsCustom = true,
                IsBuilded = false
            };

            var existingSkin = installedSkins.FirstOrDefault(s => s.Name.Equals(skinName, StringComparison.OrdinalIgnoreCase));
            if (existingSkin != null)
            {
                existingSkin.Author = author;
                existingSkin.Champion = championName ?? "Unknown";
                existingSkin.Version = "1.0";
                existingSkin.InstallDate = DateTime.Now;
                existingSkin.IsCustom = true;
                existingSkin.IsBuilded = false;
            }
            else
            {
                installedSkins.Add(newSkin);
            }

            var updatedJson = JsonConvert.SerializeObject(installedSkins, Formatting.Indented);
            await File.WriteAllTextAsync(installedJsonPath, updatedJson);
            
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #region Chroma Processing Methods

    private async Task ProcessChromaSupport(string skinFolderPath)
    {
        try
        {

            var wadDirectory = System.IO.Path.Combine(skinFolderPath, "WAD");
            if (!Directory.Exists(wadDirectory))
            {
                return;
            }

            var wadClientFile = Directory.GetFiles(wadDirectory, "*.wad.client").FirstOrDefault();
            if (string.IsNullOrEmpty(wadClientFile))
            {
                return;
            }

            var wadFileName = System.IO.Path.GetFileNameWithoutExtension(wadClientFile.Replace(".client", ""));

            await ExtractWadFile(wadClientFile, wadDirectory);

            var extractedWadPath = System.IO.Path.Combine(wadDirectory, wadFileName + ".wad");
            await ProcessSkinBinFiles(extractedWadPath);

            await RebuildWadFile(extractedWadPath);

            if (Directory.Exists(extractedWadPath))
            {
                Directory.Delete(extractedWadPath, true);
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task ExtractWadFile(string wadClientFile, string wadDirectory)
    {
        try
        {
            var wadExtractPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "cslol-tools", "wad-extract.exe");
            if (!File.Exists(wadExtractPath))
            {
                throw new FileNotFoundException($"wad-extract.exe bulunamadý: {wadExtractPath}");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = wadExtractPath,
                Arguments = $"\"{wadClientFile}\"",
                WorkingDirectory = wadDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new Exception("WAD extract iþlemi baþlatýlamadý");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"WAD extract hatasý (kod: {process.ExitCode}): {error}");
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task ProcessSkinBinFiles(string extractedWadPath)
    {
        try
        {
            var charactersPath = System.IO.Path.Combine(extractedWadPath, "data", "characters");
            if (!Directory.Exists(charactersPath))
            {
                return;
            }

            var ritoBindPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "cslol-tools", "bin", "ritobin_cli.exe");
            if (!File.Exists(ritoBindPath))
            {
                throw new FileNotFoundException($"ritobin_cli.exe bulunamadý: {ritoBindPath}");
            }

            foreach (var characterDir in Directory.GetDirectories(charactersPath))
            {
                var skinsPath = System.IO.Path.Combine(characterDir, "skins");
                if (!Directory.Exists(skinsPath))
                    continue;

                var skin0BinPath = System.IO.Path.Combine(skinsPath, "skin0.bin");
                if (!File.Exists(skin0BinPath))
                    continue;

                var characterName = System.IO.Path.GetFileName(characterDir);

                await ConvertBinToPy(ritoBindPath, skin0BinPath);

                await CreateChromaSkins(skinsPath, characterName);
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task ConvertBinToPy(string ritoBindPath, string binFilePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = ritoBindPath,
                Arguments = $"\"{binFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new Exception("ritobin_cli iþlemi baþlatýlamadý");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Bin to Py dönüþtürme hatasý (kod: {process.ExitCode}): {error}");
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task CreateChromaSkins(string skinsPath, string characterName)
    {
        try
        {
            var skin0PyPath = System.IO.Path.Combine(skinsPath, "skin0.py");
            if (!File.Exists(skin0PyPath))
            {
                return;
            }

            var skin0Content = await File.ReadAllTextAsync(skin0PyPath);
            var ritoBindPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "cslol-tools", "bin", "ritobin_cli.exe");

            for (int i = 1; i <= 150; i++)
            {
                var skinNumber = $"Skin{i}";
                var skinPyPath = System.IO.Path.Combine(skinsPath, $"skin{i}.py");

                var modifiedContent = skin0Content
                    .Replace($"/Skins/Skin0\" = SkinCharacterDataProperties {{", $"/Skins/{skinNumber}\" = SkinCharacterDataProperties {{")
                    .Replace($"/Skins/Skin0/Resources\" = ResourceResolver {{", $"/Skins/{skinNumber}/Resources\" = ResourceResolver {{");

                await File.WriteAllTextAsync(skinPyPath, modifiedContent);

                await ConvertPyToBin(ritoBindPath, skinPyPath);

                File.Delete(skinPyPath);
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task ConvertPyToBin(string ritoBindPath, string pyFilePath)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = ritoBindPath,
                Arguments = $"\"{pyFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new Exception("ritobin_cli iþlemi baþlatýlamadý");
            }

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Py to Bin dönüþtürme hatasý (kod: {process.ExitCode}): {error}");
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task RebuildWadFile(string extractedWadPath)
    {
        try
        {
            var wadMakePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "cslol-tools", "wad-make.exe");
            if (!File.Exists(wadMakePath))
            {
                throw new FileNotFoundException($"wad-make.exe bulunamadý: {wadMakePath}");
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = wadMakePath,
                Arguments = $"\"{extractedWadPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

using var process = Process.Start(processInfo);
            if (process == null)
            {
                throw new Exception("WAD rebuild iþlemi baþlatýlamadý");
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill();
                await Task.Delay(2000);
                return;
            }

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"WAD rebuild hatasý (kod: {process.ExitCode}): {error}");
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #endregion

    private System.Windows.Threading.DispatcherTimer? _friendsUpdateTimer;
    private List<Models.Friend> _currentFriends = new List<Models.Friend>();

    private System.Windows.Threading.DispatcherTimer? _slideshowTimer;
    private int _currentSlideIndex = 0;
    private List<SlideSkinData> _slideData = new List<SlideSkinData>();

    public class SlideSkinData
    {
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string Description { get; set; } = "Latest Skins";
    }

    private async Task UpdateDiscordLoginJsonAsync(string hashedToken)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            Directory.CreateDirectory(wrightSkinsPath);
            var discordLoginPath = System.IO.Path.Combine(wrightSkinsPath, "discord_login.json");
            
            var hashedTokenObj = new { hashedtoken = hashedToken };
            var hashedTokenJson = Newtonsoft.Json.JsonConvert.SerializeObject(hashedTokenObj, Newtonsoft.Json.Formatting.Indented);
            await System.IO.File.WriteAllTextAsync(discordLoginPath, hashedTokenJson);
            
            var fileInfo = new System.IO.FileInfo(discordLoginPath);
            fileInfo.Attributes |= System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System;
            
        }
        catch (Exception ex)
        {
        }
    }

    private async void StartFriendsUpdates()
    {
        if (_currentUser == null) 
        {
            return;
        }

if (_socketIOService != null)
        {
            var connectSuccess = await _socketIOService.ConnectAsync(_currentUser.UserID.ToString(), _currentUser.Username);
            if (connectSuccess)
            {
            }
            else
            {
                StartFriendsUpdatesTimer();
                return;
            }
        }
        else
        {
            StartFriendsUpdatesTimer();
            return;
        }

        try
        {
            await UpdateFriendsListAsync();
        }
        catch (Exception ex)
        {
        }
        
        try
        {
            await LoadIncomingFriendRequestsAsync();
            await UpdateFriendRequestBadgeAsync();
        }
        catch (Exception ex)
        {
        }
        
    }

    private void StartFriendsUpdatesTimer()
    {

        _friendsUpdateTimer = new System.Windows.Threading.DispatcherTimer();
        _friendsUpdateTimer.Interval = TimeSpan.FromSeconds(10);
        _friendsUpdateTimer.Tick += async (s, e) => {
            await UpdateFriendsListAsync();
            await LoadIncomingFriendRequestsAsync();
        };
        _friendsUpdateTimer.Start();
        
    }

    private void StopFriendsUpdates()
    {
        _friendsUpdateTimer?.Stop();
        _friendsUpdateTimer = null;
    }

    private async Task UpdateFriendsListAsync()
    {
        
        if (_currentUser == null)
        {
            Dispatcher.Invoke(() =>
            {
                NoFriendsText.Text = "Login...";
                NoFriendsText.Visibility = Visibility.Visible;
            });
            return;
        }

        try
        {
            Dispatcher.Invoke(() =>
            {
                NoFriendsText.Visibility = Visibility.Collapsed;
            });

            using (var httpClient = new HttpClient())
            {
                var requestData = new { user_id = _currentUser.UserID };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/friends.php", content);
                var responseString = await response.Content.ReadAsStringAsync();

var friendsResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.FriendsResponse>(responseString);
                
                if (friendsResponse?.Success == true)
                {
                    _currentFriends = friendsResponse.Friends ?? new List<Models.Friend>();
                    Dispatcher.Invoke(() => UpdateFriendsUI());
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        NoFriendsText.Text = LocalizationService.Instance.Translate("FriendsListError");
                        NoFriendsText.Visibility = Visibility.Visible;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                NoFriendsText.Text = LocalizationService.Instance.Translate("FriendsListError");
                NoFriendsText.Visibility = Visibility.Visible;
            });
        }
    }

    private void UpdateFriendsUI()
    {
        
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Friends.Clear();
            viewModel.FilteredFriends.Clear();
            
            if (!_currentFriends.Any())
            {
                return;
            }

foreach (var friend in _currentFriends)
            {
                viewModel.Friends.Add(friend);
                viewModel.FilteredFriends.Add(friend);
            }
            
        }
    }

    private async void InviteFriend(Models.Friend friend)
    {
        try
        {
            
            if (_currentUser == null)
            {
                await ShowErrorModalAsync(LocalizationService.Instance.Translate("UserInfoNotFound"));
                return;
            }

            if (DataContext is not MainViewModel viewModel)
            {
                await ShowErrorModalAsync(LocalizationService.Instance.Translate("ViewModelNotFound"));
                return;
            }

            if (viewModel.CurrentLobby == null)
            {
                await ShowWarningModalAsync(LocalizationService.Instance.Translate("joinLobbyFirst"), LocalizationService.Instance.Translate("warning"));
                return;
            }

            if (!_socketIOService.IsConnected)
            {
                await ShowErrorModalAsync(LocalizationService.Instance.Translate("NoServerConnection"));
                return;
            }

            await _socketIOService.SendLobbyInviteAsync(
                friend.UserID, 
                viewModel.CurrentLobby.lobby_code, 
                _currentUser.Username
            );

MessageBox.Show(
                string.Format(LocalizationService.Instance.Translate("lobby_invite_friend_success"), friend.Username, viewModel.CurrentLobby.lobby_code), 
                LocalizationService.Instance.Translate("invite_sent_title"), 
                MessageBoxButton.OK, 
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Lobby daveti gönderilirken bir hata oluþtu!\n{ex.Message}", 
                "Hata", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error
            );
        }
    }

    private async void RemoveFriend(Models.Friend friend)
    {
        try
        {
            
            var result = System.Windows.MessageBox.Show(
                $"{friend.Username} arkadaþýný listenden çýkarmak istediðin emin misin?", 
                "Arkadaþ Çýkar", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question
            );
            
            if (result == MessageBoxResult.Yes)
            {
                await RemoveFriendFromDatabaseAsync(friend);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                "Arkadaþ çýkarýlýrken bir hata oluþtu!", 
                "Hata", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error
            );
        }
    }

    #endregion

    #region Lobby API Methods

    private async Task CreateLobbyAsync()
    {
        if (_currentUser == null)
        {
            ShowLobbyNotFound();
            return;
        }

        try
        {
            
            bool success = await _socketIOService.CreateLobbyAsync(_currentUser.UserID.ToString(), _currentUser.Username);
            
            if (!success)
            {
                ShowLobbyNotFound();
                return;
            }
            
        }
        catch (Exception ex)
        {
            ShowLobbyNotFound();
        }
    }

    private async Task JoinLobbyAsync(string lobbyCode)
    {
        if (_currentUser == null)
        {
            ShowLobbyNotFound();
            return;
        }

        try
        {
            
            bool success = await _socketIOService.JoinLobbyByCodeAsync(_currentUser.UserID.ToString(), _currentUser.Username, lobbyCode);
            
            if (!success)
            {
                ShowLobbyNotFound();
                return;
            }
            
        }
        catch (Exception ex)
        {
            ShowLobbyNotFound();
        }
    }

    private bool _isUpdatingLobbyMembers = false;
    private async Task UpdateLobbyMembersAsync(List<int> memberIds)
    {
        try
        {
            if (_isUpdatingLobbyMembers)
            {
                return;
            }
            
            _isUpdatingLobbyMembers = true;
            
            if (DataContext is not MainViewModel viewModel)
            {
                return;
            }

            viewModel.LobbyMembers.Clear();
            
            var uniqueMemberIds = memberIds.Distinct().ToList();
            
            foreach (var userId in uniqueMemberIds)
            {
                
                var discordId = await GetDiscordIdFromUserIdAsync(userId);
                
                if (!string.IsNullOrEmpty(discordId))
                {
                    var existingUser = viewModel.LobbyMembers.FirstOrDefault(m => m.DiscordId == discordId);
                    if (existingUser != null)
                    {
                        continue;
                    }
                    
                    var userInfo = await GetUserInfoFromDiscordIdAsync(discordId);
                    
                    if (userInfo != null)
                    {
                        viewModel.LobbyMembers.Add(userInfo);
                    }
                    else
                    {
                        var fallbackUser = new Friend
                        {
                            Id = userId,
                            DiscordId = discordId,
                            Username = $"Discord#{discordId.Substring(discordId.Length - 4)}",
                            AvatarUrl = null
                        };
                        viewModel.LobbyMembers.Add(fallbackUser);
                    }
                }
                else
                {
                    var fallbackUser = new Friend
                    {
                        Id = userId,
                        DiscordId = userId.ToString(),
                        Username = $"User #{userId}",
                        AvatarUrl = null
                    };
                    viewModel.LobbyMembers.Add(fallbackUser);
                }
            }
            
        }
        catch (Exception ex)
        {
        }
        finally
        {
            _isUpdatingLobbyMembers = false;
        }
    }
    
    private async Task<string?> GetDiscordIdFromUserIdAsync(int userId)
    {
        try
        {
            using var httpClient = new HttpClient();
            var requestData = new { user_id = userId };
            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/get-discord-id.php", content);
            var responseString = await response.Content.ReadAsStringAsync();

var result = JsonConvert.DeserializeObject<dynamic>(responseString);
            
            if (result?.success == true && result.discord_id != null)
            {
                return result.discord_id.ToString();
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
        
        return null;
    }
    
    private async Task<Friend?> GetUserInfoFromDiscordIdAsync(string discordId)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"https://discordlookup.mesalytic.moe/v1/user/{discordId}");
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<dynamic>(responseString);
            if (result != null)
            {
                var username = result.username?.ToString() ?? result.global_name?.ToString() ?? discordId;

                string? avatarUrl = null;
                try
                {
                    if (result.avatar != null && result.avatar is Newtonsoft.Json.Linq.JObject)
                    {
                        var avatarObj = result.avatar;
                        if (avatarObj.link != null)
                        {
                            avatarUrl = avatarObj.link.ToString();
                        }
                        else if (avatarObj.id != null)
                        {
                            var avatarId = avatarObj.id.ToString();
                            var ext = avatarId.StartsWith("a_") ? "gif" : "png";
                            avatarUrl = $"https://cdn.discordapp.com/avatars/{discordId}/{avatarId}.{ext}";
                        }
                    }
                    else if (result.avatar != null && result.avatar.Type == Newtonsoft.Json.Linq.JTokenType.String)
                    {
                        var avatarId = result.avatar.ToString();
                        var ext = avatarId.StartsWith("a_") ? "gif" : "png";
                        avatarUrl = $"https://cdn.discordapp.com/avatars/{discordId}/{avatarId}.{ext}";
                    }
                }
                catch {  }

return new Friend
                {
                    Id = 0,
                    DiscordId = discordId,
                    Username = username,
                    AvatarUrl = avatarUrl
                };
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
        return null;
    }

    #endregion

    #region Lobby Event Handlers

private void JoinLobbyButton_Click(object sender, RoutedEventArgs e)
    {
        ShowJoinLobbyModal();
    }

    private void ShowJoinLobbyModal()
    {
        if (JoinLobbyModalOverlay != null)
        {
            if (LobbyCodeTextBox != null)
            {
                LobbyCodeTextBox.Text = "WRIGHT-";
                LobbyCodeTextBox.Focus();
                LobbyCodeTextBox.CaretIndex = LobbyCodeTextBox.Text.Length;
            }

            JoinLobbyModalOverlay.Visibility = Visibility.Visible;

            var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            JoinLobbyModalOverlay.BeginAnimation(OpacityProperty, fadeInAnimation);
        }
    }

    private void CloseJoinLobbyModal(object sender, RoutedEventArgs e)
    {
        CloseJoinLobbyModal();
    }

    private void CloseJoinLobbyModal()
    {
        if (JoinLobbyModalOverlay != null)
        {
            var fadeOutAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };

            fadeOutAnimation.Completed += (s, e) =>
            {
                JoinLobbyModalOverlay.Visibility = Visibility.Collapsed;
            };

            JoinLobbyModalOverlay.BeginAnimation(OpacityProperty, fadeOutAnimation);
        }
    }

    private void ShowLobbyNotFound()
    {
        var lobbyNotFoundText = FindName("LobbyNotFoundText") as TextBlock;
        if (lobbyNotFoundText != null)
        {
            lobbyNotFoundText.Visibility = Visibility.Visible;
            
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (lobbyNotFoundText != null)
                {
                    lobbyNotFoundText.Visibility = Visibility.Collapsed;
                }
            };
            
            timer.Start();
        }
    }

    private async void ConfirmJoinLobby(object sender, RoutedEventArgs e)
    {
        var lobbyCodeTextBox = FindName("LobbyCodeTextBox") as TextBox;
        if (lobbyCodeTextBox != null)
        {
            var lobbyCode = lobbyCodeTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(lobbyCode) && lobbyCode != "WRIGHT-")
            {
                await JoinLobbyAsync(lobbyCode);
            }
            else
            {
                ShowLobbyNotFound();
                
                lobbyCodeTextBox.Focus();
            }
        }
    }

    private void LobbyCodeTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            ConfirmJoinLobby(sender, new RoutedEventArgs());
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            CloseJoinLobbyModal();
        }
    }

    private async void CreateLobbyButton_Click(object sender, RoutedEventArgs e)
    {
        await CreateLobbyAsync();
    }

    private void AddFriendButton_Click(object sender, RoutedEventArgs e)
    {
        ShowFriendModal();
    }

    private async void InviteFriendButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            
            if (!(sender is Button button) || !(button.DataContext is Friend friend))
            {
                return;
            }

            if (!(DataContext is MainViewModel viewModel))
            {
                return;
            }
            
            if (viewModel.CurrentLobby == null)
            {
                await ShowWarningModalAsync(LocalizationService.Instance.Translate("lobby_invite_first_error"), LocalizationService.Instance.Translate("ErrorTitle"));
                return;
            }

            if (_socketIOService?.IsConnected != true)
            {
                await ShowWarningModalAsync(LocalizationService.Instance.Translate("no_server_connection_error"), LocalizationService.Instance.Translate("ErrorTitle"));
                return;
            }

            var lobbyCode = viewModel.CurrentLobby.LobbyCode;

await _socketIOService.SendLobbyInviteAsync(friend.UserID, lobbyCode, _currentUser?.Username ?? "Unknown");

await ShowSuccessModalAsync(string.Format(LocalizationService.Instance.Translate("lobby_invite_direct_success"), friend.Username), LocalizationService.Instance.Translate("SuccessModalTitle"));
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(LocalizationService.Instance.Translate("invite_send_error"), LocalizationService.Instance.Translate("ErrorTitle"));
        }
    }

private async void ShowFriendInvitePopup()
    {
        try
        {
            
            if (!(DataContext is MainViewModel viewModel))
            {
                return;
            }
            
            if (viewModel.CurrentLobby == null)
            {
                await ShowWarningModalAsync(LocalizationService.Instance.Translate("lobby_invite_first_error"), LocalizationService.Instance.Translate("ErrorTitle"));
                return;
            }

            if (_socketIOService?.IsConnected != true)
            {
                await ShowWarningModalAsync(LocalizationService.Instance.Translate("no_server_connection_error"), LocalizationService.Instance.Translate("ErrorTitle"));
                return;
            }

            var friends = new List<Friend>();
            if (viewModel.Friends != null)
            {
                friends = viewModel.Friends.ToList();
            }
            
            if (!friends.Any())
            {
                MessageBox.Show("Davet edebileceðiniz arkadaþ bulunamadý!", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

var popup = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Width = 400,
                Height = 500,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 74)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = string.Format(LocalizationService.Instance.Translate("lobby_invite_text"), viewModel.CurrentLobby.LobbyCode),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(15, 15, 15, 10),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);
            headerGrid.Children.Add(titleText);

            var closeButton = new Button
            {
                Content = "?",
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 10, 15, 5),
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, e) => popup.Close();
            Grid.SetColumn(closeButton, 1);
            headerGrid.Children.Add(closeButton);

            Grid.SetRow(headerGrid, 0);
            mainGrid.Children.Add(headerGrid);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(15, 0, 15, 10)
            };

            var friendsPanel = new StackPanel { Orientation = Orientation.Vertical };

            foreach (var friend in friends)
            {
                var friendBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 2, 0, 2),
                    Padding = new Thickness(10),
                    Cursor = Cursors.Hand
                };

                var friendGrid = new Grid();
                friendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                friendGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var friendText = new TextBlock
                {
                    Text = friend.Username,
                    FontSize = 12,
                    Foreground = System.Windows.Media.Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(friendText, 0);
                friendGrid.Children.Add(friendText);

                var inviteButton = new Button
                {
                    Content = LocalizationService.Instance.Translate("invite_button_text"),
                    Width = 80,
                    Height = 25,
                    Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                    Foreground = System.Windows.Media.Brushes.White,
                    BorderThickness = new Thickness(0),
                    FontSize = 10,
                    Cursor = Cursors.Hand
                };

                inviteButton.Click += async (s, e) =>
                {
                    try
                    {
                        inviteButton.IsEnabled = false;
                        inviteButton.Content = LocalizationService.Instance.Translate("invite_sending_status");

await _socketIOService.SendLobbyInviteAsync(
                            friend.UserID, 
                            viewModel.CurrentLobby.LobbyCode, 
                            _currentUser?.Username ?? "Unknown"
                        );

                        inviteButton.Content = LocalizationService.Instance.Translate("invite_sent_status");
                        inviteButton.Background = new SolidColorBrush(Color.FromRgb(0, 150, 0));

                        await ShowSuccessModalAsync(string.Format(LocalizationService.Instance.Translate("lobby_invite_sent_success"), friend.Username), LocalizationService.Instance.Translate("SuccessModalTitle"));

                        await Task.Delay(2000);
                        inviteButton.Content = LocalizationService.Instance.Translate("invite_button_text");
                        inviteButton.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                        inviteButton.IsEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorModalAsync(LocalizationService.Instance.Translate("invite_send_error"), LocalizationService.Instance.Translate("ErrorTitle"));
                        
                        inviteButton.Content = LocalizationService.Instance.Translate("invite_button_text");
                        inviteButton.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                        inviteButton.IsEnabled = true;
                    }
                };

                Grid.SetColumn(inviteButton, 1);
                friendGrid.Children.Add(inviteButton);

                friendBorder.Child = friendGrid;
                friendsPanel.Children.Add(friendBorder);
            }

            scrollViewer.Content = friendsPanel;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            var buttonGrid = new Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var closeBottomButton = new Button
            {
                Content = "Kapat",
                Width = 80,
                Height = 30,
                Margin = new Thickness(15, 5, 15, 15),
                Background = new SolidColorBrush(Color.FromRgb(70, 70, 74)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            closeBottomButton.Click += (s, e) => popup.Close();
            Grid.SetColumn(closeBottomButton, 1);
            buttonGrid.Children.Add(closeBottomButton);

            Grid.SetRow(buttonGrid, 2);
            mainGrid.Children.Add(buttonGrid);

            border.Child = mainGrid;
            popup.Content = border;

            popup.Show();

        }
        catch (Exception ex)
        {
            MessageBox.Show("Arkadaþ davet popup'ý açýlýrken bir hata oluþtu!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RemoveFriendButton_Click(object sender, RoutedEventArgs e)
    {
        
        if (sender is Button button)
        {
            
            if (button.Tag is Models.Friend friend)
            {
                
                var result = await ShowConfirmationModalAsync(
                    $"{friend.Username} adlý arkadaþýný kaldýrmak istediðin emin misin?",
                    "Arkadaþ Kaldýr");

                if (result)
                {
                    await RemoveFriendFromDatabaseAsync(friend);
                }
                else
                {
                }
            }
            else
            {
            }
        }
        else
        {
        }
    }

    #endregion

    #region Public Helper Methods

    public User? GetCurrentUser()
    {
        return _currentUser;
    }
    
    public Models.Lobby? GetCurrentLobby()
    {
        if (DataContext is MainViewModel viewModel)
        {
            return viewModel.CurrentLobby;
        }
        return null;
    }

    public async Task RefreshCurrentLobbyAsync()
    {
        try
        {
            if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null && _currentUser != null)
            {
                
                using (var httpClient = new HttpClient())
                {
                    var requestData = new { lobby_id = viewModel.CurrentLobby.LobbyId };
                    var json = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/lobby/get-lobby-info.php", content);
                    var responseString = await response.Content.ReadAsStringAsync();

var result = JsonConvert.DeserializeObject<dynamic>(responseString);
                    
                    if (result?.success == true && result.lobby != null)
                    {
                        var lobby = result.lobby;
                        
                        viewModel.CurrentLobby.LobbySkins.Clear();
                        if (lobby.lobby_skins != null)
                        {
                            foreach (var skin in lobby.lobby_skins)
                            {
                                viewModel.CurrentLobby.LobbySkins.Add(new Models.LobbySkin
                                {
                                    SkinId = skin.skin_id?.ToString() ?? "",
                                    SkinName = skin.skin_name?.ToString() ?? "",
                                    DownloadUrl = skin.download_url?.ToString() ?? "",
                                    ChampionName = skin.champion_name?.ToString() ?? "",
                                    Version = skin.version?.ToString() ?? "",
                                    IsChroma = skin.is_chroma ?? false,
                                    IsBuilded = skin.is_builded ?? false,
                                    IsCustom = skin.is_custom ?? false,
                                    UploadedBy = skin.uploaded_by ?? 0
                                });
                            }
                        }
                    }
                    else
                    {
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Add Skin to Lobby Functionality

    private void AddSkinButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var skinSelectionModal = new Views.SkinSelectionModal(null, _socketIOService);
            skinSelectionModal.Owner = this;
            
            var result = skinSelectionModal.ShowDialog();
            
            if (result == true)
            {
                var selectedSkins = skinSelectionModal.SelectedSkins;
            }
        }
        catch (Exception ex)
        {
            Views.CustomMessageModal.ShowError($"Skin seçim modalý açýlýrken hata oluþtu: {ex.Message}");
        }
    }

    private async void SelectOnlyLobbySkinsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            
            if (DataContext is MainViewModel viewModel)
            {
                
                if (viewModel.CurrentLobby?.LobbySkins != null)
                {
                    
                    var lobbySkinNames = viewModel.CurrentLobby.LobbySkins
                        .Select(ls => ls.SkinName?.Trim())
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var wrightPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
                    var profilePath = System.IO.Path.Combine(wrightPath, "Wright.profile");
                    var installedJsonPath = System.IO.Path.Combine(wrightPath, "installed.json");
                    
                    Directory.CreateDirectory(wrightPath);
                    
                    var installedSkinNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (File.Exists(installedJsonPath))
                    {
                        try
                        {
                            var installedJson = await File.ReadAllTextAsync(installedJsonPath);
                            var installedSkins = JsonConvert.DeserializeObject<List<InstalledSkin>>(installedJson);
                            if (installedSkins != null)
                            {
                                foreach (var installedSkin in installedSkins)
                                {
                                    if (!string.IsNullOrEmpty(installedSkin.Name))
                                    {
                                        installedSkinNames.Add(installedSkin.Name.Trim());
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                    
                    var selectableLobbySkinsCount = 0;
                    var newProfileLines = new List<string>();
                    
                    foreach (var lobbySkinName in lobbySkinNames)
                    {
                        if (installedSkinNames.Contains(lobbySkinName))
                        {
                            newProfileLines.Add(lobbySkinName);
                            selectableLobbySkinsCount++;
                        }
                        else
                        {
                        }
                    }
                    
                    File.WriteAllLines(profilePath, newProfileLines);

}
                else
                {
                    await ShowWarningModalAsync(
                        LocalizationService.Instance.Translate("lobby_skins_select_join_lobby_message"), 
                        LocalizationService.Instance.Translate("lobby_not_found_title")
                    );
                }
            }
            else
            {
                await ShowErrorModalAsync(
                    LocalizationService.Instance.Translate("viewmodel_not_found_message"), 
                    LocalizationService.Instance.Translate("error_title")
                );
            }
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(
                string.Format(LocalizationService.Instance.Translate("lobby_skins_select_error_message"), ex.Message), 
                LocalizationService.Instance.Translate("error_title")
            );
        }
    }

    #endregion

    #region Remove Skin from Lobby Functionality

    private async void RemoveSkinButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.DataContext is LobbySkin skinToRemove)
            {
                if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null && _currentUser != null)
                {
                    if (viewModel.CurrentLobby.LobbyCreatorId != _currentUser.UserID)
                    {
                        await ShowWarningModalAsync("Sadece lobby kurucusu skin silebilir.", "Yetki Hatasý");
                        return;
                    }

                    var result = await ShowConfirmationModalAsync($"'{skinToRemove.SkinName}' adlý skini lobiden kaldýrmak istediðinizden emin misiniz?", 
                        "Skin Kaldýrma");

                    if (result)
                    {
                        await _socketIOService.RemoveSkinFromLobbyAsync(_currentUser.UserID, skinToRemove.SkinId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing skin from lobby: {ex.Message}");
            MessageBox.Show("Skin kaldýrýlýrken hata oluþtu", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

#endregion

    #region Lobby Exit/Disband Functionality

    private void LobbyExitButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null && _currentUser != null)
        {
            bool isCreator = viewModel.CurrentLobby.LobbyCreatorId == _currentUser.UserID;

if (isCreator)
            {
                HandleDisbandLobby(viewModel.CurrentLobby);
            }
            else
            {
                HandleExitLobby(viewModel.CurrentLobby);
            }
        }
        else
        {
        }
    }

    private async void HandleExitLobby(Lobby lobby)
    {
        if (_currentUser == null)
        {
            Views.CustomMessageModal.ShowWarning(LocalizationService.Instance.Translate("login_required"));
            return;
        }

        try
        {
            var result = Views.CustomMessageModal.ShowQuestion(
                LocalizationService.Instance.Translate("lobby_leave_confirm_message"), 
                LocalizationService.Instance.Translate("lobby_leave_confirm_title"));

            if (result == Views.CustomMessageModal.MessageResult.Yes)
            {
                
                bool success = await _socketIOService.LeaveLobbyAsync(_currentUser.UserID.ToString(), lobby.LobbyId.ToString());
                
                if (success)
                {
                }
                else
                {
                    Views.CustomMessageModal.ShowError("Lobiden ayrýlma isteði gönderilemedi. Socket.IO baðlantýsýný kontrol edin.");
                }
            }
        }
        catch (Exception ex)
        {
            Views.CustomMessageModal.ShowError($"Lobiden ayrýlýrken hata oluþtu: {ex.Message}");
        }
    }

    private async void HandleDisbandLobby(Lobby lobby)
    {
        if (_currentUser == null)
        {
            Views.CustomMessageModal.ShowWarning(LocalizationService.Instance.Translate("login_required"));
            return;
        }

        try
        {
            var result = Views.CustomMessageModal.ShowQuestion(
                LocalizationService.Instance.Translate("lobby_disband_confirm_message"), 
                LocalizationService.Instance.Translate("lobby_disband_confirm_title"));

            if (result == Views.CustomMessageModal.MessageResult.Yes)
            {
                
                bool success = await _socketIOService.DisbandLobbyAsync(_currentUser.UserID.ToString(), lobby.LobbyCode);
                
                if (success)
                {
                }
                else
                {
                    Views.CustomMessageModal.ShowError(LocalizationService.Instance.Translate("lobby_disband_failed"));
                }
            }
        }
        catch (Exception ex)
        {
            Views.CustomMessageModal.ShowError(string.Format(LocalizationService.Instance.Translate("lobby_disband_error"), ex.Message));
        }
    }

    public void UpdateLobbyExitButton()
    {
        if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null && _currentUser != null)
        {
            bool isCreator = viewModel.CurrentLobby.LobbyCreatorId == _currentUser.UserID;
            LobbyExitButton.Content = isCreator ? LocalizationService.Instance.Translate("lobby_disband_button") : LocalizationService.Instance.Translate("lobby_leave_button");
            
            if (LobbySettingsButton != null)
            {
                LobbySettingsButton.Visibility = isCreator ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void LobbySettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null && _currentUser != null)
        {
            if (viewModel.CurrentLobby.LobbyCreatorId == _currentUser.UserID)
            {
                
                var settingsModal = new Views.LobbySettingsModal(viewModel.CurrentLobby, _socketIOService);
                settingsModal.ShowDialog();
                
            }
            else
            {
                Views.CustomMessageModal.ShowWarning("Sadece lobi kurucusu ayarlarý deðiþtirebilir!");
            }
        }
    }

    #endregion

    #region Lobby Code Functionality

    private bool _isCodeVisible = false;

    private void ToggleCodeVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        _isCodeVisible = !_isCodeVisible;
        UpdateCodeDisplay();
        UpdateEyeIcon();
    }

    private void UpdateCodeDisplay()
    {
        if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null)
        {
            if (_isCodeVisible)
            {
                LobbyCodeText.Text = viewModel.CurrentLobby.LobbyCode;
            }
            else
            {
                var converter = new Converters.LobbyCodeMaskConverter();
                var maskedCode = converter.Convert(viewModel.CurrentLobby.LobbyCode, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
                LobbyCodeText.Text = maskedCode?.ToString() ?? "WRIGHT-*******";
            }
        }
    }

    private void UpdateEyeIcon()
    {
        if (EyeIcon != null)
        {
            if (_isCodeVisible)
            {
                EyeIcon.Data = Geometry.Parse("M2,5.27L3.28,4L20,20.72L18.73,22L15.65,18.92C14.5,19.3 13.28,19.5 12,19.5C7,19.5 2.73,16.39 1,12C1.69,10.24 2.79,8.69 4.19,7.46L2,5.27M12,9A3,3 0 0,1 15,12C15,12.35 14.94,12.69 14.83,13L11,9.17C11.31,9.06 11.65,9 12,9M12,4.5C17,4.5 21.27,7.61 23,12C22.18,14.08 20.79,15.88 19,17.19L17.58,15.76C18.94,14.82 20.06,13.54 20.82,12C19.17,8.64 15.76,6.5 12,6.5C10.91,6.5 9.84,6.68 8.84,7L7.3,5.47C8.74,4.85 10.33,4.5 12,4.5M3.18,12C4.83,15.36 8.24,17.5 12,17.5C12.69,17.5 13.37,17.43 14,17.29L11.72,15C10.29,14.85 9.15,13.71 9,12.28L5.6,8.87C4.61,9.72 3.78,10.78 3.18,12Z");
            }
            else
            {
                EyeIcon.Data = Geometry.Parse("M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z");
            }
        }
    }

    private async void LobbyCodeContainer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null)
        {
            try
            {
                System.Windows.Clipboard.SetText(viewModel.CurrentLobby.LobbyCode);
                
                await ShowSuccessModalAsync("Lobi kodu panoya kopyalandý!", "Baþarýlý");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kopyalama hatasý: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region RealtimeService Event Handlers

    private void OnUserJoined(string userId, string username)
    {
        Dispatcher.Invoke(() =>
        {
        });
    }

    private void OnUserLeft(string userId, string username)
    {
        Dispatcher.Invoke(() =>
        {
        });
    }

    private void OnSkinAdded(string userId, string username, RealtimeSkinData skinData, bool isExistingSkin)
    {
        Dispatcher.Invoke(() =>
        {
        });
    }

    private void OnSkinRemoved(string skinId)
    {
        Dispatcher.Invoke(() =>
        {
            
            if (DataContext is MainViewModel viewModel && viewModel.CurrentLobby != null)
            {
                var skinToRemove = viewModel.CurrentLobby.LobbySkins.FirstOrDefault(s => s.SkinId == skinId);
                if (skinToRemove != null)
                {
                    viewModel.CurrentLobby.LobbySkins.Remove(skinToRemove);
                }
            }
        });
    }

    private void OnFileRequest(string fromUserId, string fromUsername, string skinName, string requestId)
    {
        Dispatcher.Invoke(async () =>
        {
            
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var skinFolderPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", skinName);
                
                if (Directory.Exists(skinFolderPath))
                {
                    var files = new List<RealtimeFileData>();
                    foreach (var filePath in Directory.GetFiles(skinFolderPath, "*", SearchOption.AllDirectories))
                    {
                        var fileInfo = new FileInfo(filePath);
                        var fileContent = await File.ReadAllBytesAsync(filePath);
                        var base64Content = Convert.ToBase64String(fileContent);
                        
                        files.Add(new RealtimeFileData
                        {
                            Name = System.IO.Path.GetFileName(filePath),
                            Path = System.IO.Path.GetRelativePath(skinFolderPath, filePath),
                            Size = fileInfo.Length,
                            Content = base64Content,
                            Hash = ComputeFileHash(fileContent)
                        });
                    }
                    
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFilesReceived(string fromUserId, string fromUsername, string skinName, List<RealtimeFileData> files, string requestId)
    {
        Dispatcher.Invoke(async () =>
        {
            
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var skinFolderPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", skinName);
                
                if (!Directory.Exists(skinFolderPath))
                {
                    Directory.CreateDirectory(skinFolderPath);
                }
                
                foreach (var file in files)
                {
                    var fullFilePath = System.IO.Path.Combine(skinFolderPath, file.Path);
                    var fileDirectory = System.IO.Path.GetDirectoryName(fullFilePath);
                    
                    if (!Directory.Exists(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    
                    var fileContent = Convert.FromBase64String(file.Content);
                    await File.WriteAllBytesAsync(fullFilePath, fileContent);
                }

if (DataContext is MainViewModel viewModel)
                {
                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnExistingSkinFileReceived(string skinName, bool autoSelect)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                if (autoSelect)
                {
                    AutoSelectDownloadedSkin(skinName);
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private string ComputeFileHash(byte[] fileContent)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha256.ComputeHash(fileContent);
            return Convert.ToBase64String(hash);
        }
    }

    private async Task RemoveFriendFromDatabaseAsync(Models.Friend friend)
    {
        try
        {
            if (_currentUser == null)
            {
                return;
            }

if (_socketIOService != null)
            {
                var success = await _socketIOService.RemoveFriendAsync(_currentUser.UserID.ToString(), friend.Id.ToString());
                
                if (success)
                {
                }
                else
                {
                }
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
            
            Dispatcher.Invoke(() => {
                System.Windows.MessageBox.Show(
                    "Arkadaþ çýkarýlýrken bir hata oluþtu!", 
                    "Hata", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error
                );
            });
        }
    }

#endregion

    #region Friend RealtimeService Event Handlers

    private void OnFriendStatusChanged(string friendId, string friendUsername, bool isOnline)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                if (DataContext is MainViewModel viewModel)
                {
                    var friend = viewModel.Friends.FirstOrDefault(f => f.Id.ToString() == friendId);
                    if (friend != null)
                    {
                    }
                    
                    var filteredFriend = viewModel.FilteredFriends.FirstOrDefault(f => f.Id.ToString() == friendId);
                    if (filteredFriend != null)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFriendAdded(string friendId, string friendUsername, string friendAvatarUrl)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                if (DataContext is MainViewModel viewModel)
                {
                    var newFriend = new Models.Friend
                    {
                        Id = int.TryParse(friendId, out int id) ? id : 0,
                        Username = friendUsername,
                        AvatarUrl = friendAvatarUrl,
                        DiscordId = friendId
                    };
                    
                    viewModel.Friends.Add(newFriend);
                    viewModel.FilteredFriends.Add(newFriend);
                    _currentFriends.Add(newFriend);
                    
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFriendRemoved(string friendId)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                if (DataContext is MainViewModel viewModel)
                {
                    var friendToRemove = viewModel.Friends.FirstOrDefault(f => f.Id.ToString() == friendId);
                    if (friendToRemove != null)
                    {
                        viewModel.Friends.Remove(friendToRemove);
                        viewModel.FilteredFriends.Remove(friendToRemove);
                        _currentFriends.RemoveAll(f => f.Id.ToString() == friendId);
                        
                    }
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFriendsListUpdateRequired(string reason, string friendId)
    {
        Dispatcher.Invoke(async () =>
        {
            try
            {
                
                await UpdateFriendsListAsync();
                
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFriendRequestReceived(string fromUserId, string fromUsername)
    {
        Dispatcher.Invoke(async () =>
        {
            try
            {
                
                await UpdateFriendRequestBadgeAsync();
                
                if (FriendModalOverlay.Visibility == Visibility.Visible)
                {
                    await LoadIncomingFriendRequestsAsync();
                }
                
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFriendRequestSent(string targetUserId)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFriendRequestAccepted(string byUserId, string byUsername)
    {
        Dispatcher.Invoke(async () =>
        {
            try
            {
                
                await UpdateFriendsListAsync();
                
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnFriendRequestDeclined(string byUserId)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyInviteReceived(string fromUserId, string fromUsername, string lobbyCode, string lobbyCreator, int memberCount)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                ShowLobbyInvitePopup(fromUsername, lobbyCode, lobbyCreator, memberCount);
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyInviteSent(string targetUserId, string lobbyCode)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyInviteAccepted(string byUserId, string byUsername, string lobbyCode)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyInviteDeclined(string byUserId, string lobbyCode)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyInviteError(string error)
    {
        Dispatcher.Invoke(async () =>
        {
            try
            {
                await ShowErrorModalAsync($"Lobby davet hatasý: {error}", "Hata");
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyCreated(dynamic lobbyData)
    {
        Dispatcher.Invoke(async () =>
        {
            try
            {
                
                var jsonElement = (JsonElement)lobbyData;

JsonElement lobbyElement = jsonElement;
                
                string lobbyCode = lobbyElement.GetProperty("lobby_code").GetString();
                long lobbyIdLong = lobbyElement.GetProperty("lobby_id").GetInt64();
                int lobbyId = (int)lobbyIdLong;
                int creatorId = lobbyElement.GetProperty("lobby_creator_id").GetInt32();
                
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    var membersArray = lobbyElement.GetProperty("lobby_members");
                    var members = new List<int>();
                    foreach (var member in membersArray.EnumerateArray())
                    {
                        members.Add(member.GetInt32());
                    }

viewModel.CurrentLobby = new Models.Lobby
                    {
                        LobbyCode = lobbyCode,
                        LobbyId = lobbyId,
                        LobbyCreatorId = creatorId,
                        LobbyMembers = members,
                        LobbySkins = new ObservableCollection<Models.LobbySkin>()
                    };
                    viewModel.IsLobbyCreator = creatorId == _currentUser.UserID;

UpdateLobbyExitButton();
                    
                    _ = UpdateLobbyMembersAsync(members);
                    
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyJoined(dynamic lobbyData)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                var jsonElement = (JsonElement)lobbyData;
                
                JsonElement lobbyElement = jsonElement;
                
                string lobbyCode = lobbyElement.GetProperty("lobby_code").GetString();
                long lobbyIdLong = lobbyElement.GetProperty("lobby_id").GetInt64();
                int lobbyId = (int)lobbyIdLong;
                int creatorId = lobbyElement.GetProperty("lobby_creator_id").GetInt32();
                
                CloseJoinLobbyModal();

var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    var membersArray = lobbyElement.GetProperty("lobby_members");
                    var members = new List<int>();
                    foreach (var member in membersArray.EnumerateArray())
                    {
                        members.Add(member.GetInt32());
                    }

viewModel.CurrentLobby = new Models.Lobby
                    {
                        LobbyCode = lobbyCode,
                        LobbyId = lobbyId,
                        LobbyCreatorId = creatorId,
                        LobbyMembers = members,
                        LobbySkins = new ObservableCollection<Models.LobbySkin>()
                    };
                    viewModel.IsLobbyCreator = creatorId == _currentUser.UserID;

UpdateLobbyExitButton();
                    
                    _ = UpdateLobbyMembersAsync(members);
                    
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyLeft(string lobbyCode)
    {
        Dispatcher.Invoke(async () =>
        {
            try
            {
                
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    Views.LobbySettingsModal.ClearLobbyPermissions(lobbyCode);
                    viewModel.CurrentLobby = null;
                    viewModel.IsLobbyCreator = false;
                    
                    UpdateLobbyExitButton();
                }
                
                await ShowSuccessModalAsync("Lobby'den baþarýyla ayrýldýnýz.", "Lobby'den Ayrýldýnýz");
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyDisbanded(string reason)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    if (viewModel.CurrentLobby != null)
                    {
                        Views.LobbySettingsModal.ClearLobbyPermissions(viewModel.CurrentLobby.LobbyCode);
                    }
                    viewModel.CurrentLobby = null;
                    viewModel.IsLobbyCreator = false;
                    
                    viewModel.LobbyMembers.Clear();
                    
                    UpdateLobbyExitButton();
                    
                    Views.CustomMessageModal.ShowSuccess($"Lobby daðýtýldý: {reason}");
                    
                    ShowPage("Home");
                    viewModel?.NavigateCommand.Execute("Home");
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnUserLeftLobby(int userId, dynamic lobby)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null && lobby != null)
                {
                    try
                    {
                        var lobbyJson = lobby.ToString();
                        var updatedLobby = Newtonsoft.Json.JsonConvert.DeserializeObject<Lobby>(lobbyJson);
                        
                        if (updatedLobby != null)
                        {
                            viewModel.CurrentLobby = updatedLobby;
                            
                            _ = UpdateLobbyMembersAsync(updatedLobby.LobbyMembers);
                            
                        }
                    }
                    catch (Exception parseEx)
                    {
                        
                        if (viewModel.CurrentLobby != null)
                        {
                            viewModel.CurrentLobby.LobbyMembers.RemoveAll(m => m == userId);
                            _ = UpdateLobbyMembersAsync(viewModel.CurrentLobby.LobbyMembers);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyError(string error)
    {
        Dispatcher.Invoke(() =>
        {
            ShowLobbyNotFound();
        });
    }

    private void OnLobbySkinAdded(string userId, string username, RealtimeSkinData skinData, bool isExistingSkin)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                var viewModel = DataContext as MainViewModel;
                if (viewModel?.CurrentLobby != null)
                {
                    var lobbySkin = new Models.LobbySkin
                    {
                        SkinId = skinData.Id,
                        SkinName = skinData.Name,
                        ChampionName = skinData.Champion,
                        Version = skinData.Version,
                        IsBuilded = skinData.IsBuilded,
                        IsCustom = skinData.IsCustom,
                        ImageCard = skinData.ImageCard,
                        UploadedBy = int.Parse(userId),
                        UploadedByUsername = username
                    };
                    
                    var existingSkin = viewModel.CurrentLobby.LobbySkins.FirstOrDefault(s => s.SkinId == skinData.Id);
                    if (existingSkin != null)
                    {
                        viewModel.CurrentLobby.LobbySkins.Remove(existingSkin);
                    }
                    
                    viewModel.CurrentLobby.LobbySkins.Add(lobbySkin);
                    
                    CheckAndAutoSelectExistingSkin(lobbySkin);
                    
                    if (isExistingSkin)
                    {
                        
                        _ = CheckAndDownloadExistingSkin(skinData, userId);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbySkinRemoved(string skinId)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                var viewModel = DataContext as MainViewModel;
                if (viewModel?.CurrentLobby != null)
                {
                    var skinToRemove = viewModel.CurrentLobby.LobbySkins.FirstOrDefault(s => s.SkinId == skinId);
                    if (skinToRemove != null)
                    {
                        viewModel.CurrentLobby.LobbySkins.Remove(skinToRemove);
                    }
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnLobbyMembersUpdated(dynamic lobbyData)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                
                var jsonElement = (JsonElement)lobbyData;
                
                var membersArray = jsonElement.GetProperty("lobby_members");
                var members = new List<int>();
                foreach (var member in membersArray.EnumerateArray())
                {
                    members.Add(member.GetInt32());
                }

var viewModel = DataContext as MainViewModel;
                if (viewModel?.CurrentLobby != null)
                {
                    viewModel.CurrentLobby.LobbyMembers = members;
                    
                    _ = UpdateLobbyMembersAsync(members);
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private void OnNewModsInLobbyDetected()
    {
        Dispatcher.Invoke(async () =>
        {
            try
            {
                
                if (_isInjecting && _processStarted)
                {
                    if (IsLeagueOfLegendsRunning())
                    {
                        
                        await ShowLeagueRunningWarningMessage();
                        return;
                    }

await ShowFoundNewModsMessage();
                    
                    await RestartInjectionProcess();
                }
            }
            catch (Exception ex)
            {
            }
        });
    }

    private async Task ShowFoundNewModsMessage()
    {
        try
        {
            var wasInjecting = _isInjecting;
            var wasProcessStarted = _processStarted;
            
            _isInjecting = true;
            _processStarted = false;
            
            var selectedCount = GetSelectedSkinsCount();
            var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "?? Found new mods in lobby", 
                FontSize = 14, 
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0))
            });
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = string.Format(LocalizationService.Instance.Translate("SkinsSelectedText"), selectedCount), 
                FontSize = 10, 
                Opacity = 0.8, 
                HorizontalAlignment = HorizontalAlignment.Center 
            });
            
            InjectButton.Content = stackPanel;
            InjectButton.IsEnabled = false;

await Task.Delay(2000);
            
            _isInjecting = wasInjecting;
            _processStarted = wasProcessStarted;
            
            UpdateInjectButtonUI();
            
        }
        catch (Exception ex)
        {
        }
    }

    private async Task RestartInjectionProcess()
    {
        try
        {
            
            await StopInjectProcess();
            
            await Task.Delay(500);
            
            await StartInjectProcess();
            
        }
        catch (Exception ex)
        {
        }
    }

    private bool IsLeagueOfLegendsRunning()
    {
        try
        {
            var leagueProcesses = System.Diagnostics.Process.GetProcessesByName("League of Legends");
            bool isRunning = leagueProcesses.Length > 0;

foreach (var process in leagueProcesses)
            {
                process.Dispose();
            }
            
            return isRunning;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task ShowLeagueRunningWarningMessage()
    {
        try
        {
            var wasInjecting = _isInjecting;
            var wasProcessStarted = _processStarted;
            
            _isInjecting = true;
            _processStarted = false;
            
            var selectedCount = GetSelectedSkinsCount();
            var stackPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "?? League is running - can't re-inject", 
                FontSize = 14, 
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0))
            });
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Close League to allow mod updates", 
                FontSize = 10, 
                Opacity = 0.8, 
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0))
            });
            
            InjectButton.Content = stackPanel;
            InjectButton.IsEnabled = false;

await Task.Delay(3000);
            
            _isInjecting = wasInjecting;
            _processStarted = wasProcessStarted;
            
            UpdateInjectButtonUI();
            
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Friend Modal Methods

    private void ShowFriendModal()
    {
        try
        {
            FriendModalOverlay.Opacity = 0.0;
            FriendModalOverlay.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            FriendModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            
            ClearUserSearchResults();
            UserSearchTextBox.Text = "";
            
            LoadFriendRequests();
            
        }
        catch (Exception ex)
        {
        }
    }

    private void CloseFriendModal(object sender, RoutedEventArgs e)
    {
        try
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, args) =>
            {
                FriendModalOverlay.Visibility = Visibility.Collapsed;
                FriendModalOverlay.Opacity = 1.0;
            };

            FriendModalOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            
        }
        catch (Exception ex)
        {
        }
    }

    private void UserSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                ClearUserSearchResults();
                NoSearchResultsText.Text = LocalizationService.Instance.Translate("UserSearchInstructions");
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void UserSearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.Enter)
            {
                SearchUsersButton_Click(sender, new RoutedEventArgs());
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void SearchUsersButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var searchText = UserSearchTextBox.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            if (searchText.Length < 3)
            {
                return;
            }

var searchResults = await SearchUsersAsync(searchText);

foreach (var user in searchResults.Take(10))
            {
            }

            DisplayUserSearchResults(searchResults);
        }
        catch (Exception ex)
        {
        }
    }

    private void DisplayUserSearchResults(List<User> users)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                var userSearchResults = this.FindName("UserSearchResults") as StackPanel;
                var noSearchResultsText = this.FindName("NoSearchResultsText") as TextBlock;

                if (userSearchResults == null || noSearchResultsText == null)
                {
                    return;
                }

                userSearchResults.Children.Clear();

                if (users.Count == 0)
                {
                    noSearchResultsText.Text = "No users found with this username.";
                    noSearchResultsText.Visibility = Visibility.Visible;
                    userSearchResults.Children.Add(noSearchResultsText);
                    return;
                }

                noSearchResultsText.Visibility = Visibility.Collapsed;

                foreach (var user in users.Take(10))
                {
                    var userCard = CreateUserSearchCard(user);
                    userSearchResults.Children.Add(userCard);
                }

            });
        }
        catch (Exception ex)
        {
        }
    }

    private Border CreateUserSearchCard(User user)
    {
        var userCard = new Border
        {
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(12, 10, 12, 10),
            BorderThickness = new Thickness(1)
        };

        var gradientBrush = new LinearGradientBrush();
        gradientBrush.StartPoint = new Point(0, 0);
        gradientBrush.EndPoint = new Point(1, 1);
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 45, 45, 55), 0.0));
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 35, 35, 45), 1.0));
        userCard.Background = gradientBrush;

        var borderBrush = new SolidColorBrush(Color.FromArgb(255, 124, 58, 237));
        userCard.BorderBrush = borderBrush;

        var dropShadow = new DropShadowEffect
        {
            Color = Color.FromArgb(255, 124, 58, 237),
            BlurRadius = 8,
            Opacity = 0.2,
            ShadowDepth = 0
        };
        userCard.Effect = dropShadow;

        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var avatarBorder = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        LoadDiscordAvatarAsync(avatarBorder, user.DiscordID, user.Username);

        var userInfo = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var usernameText = new TextBlock
        {
            Text = user.Username,
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 2)
        };

        var usernameGlow = new DropShadowEffect
        {
            Color = Color.FromArgb(255, 124, 58, 237),
            BlurRadius = 3,
            Opacity = 0.3,
            ShadowDepth = 0
        };
        usernameText.Effect = usernameGlow;

        var discordText = new TextBlock
        {
            Text = !string.IsNullOrEmpty(user.DiscordID) ? $"?? {user.DiscordID}" : "?? No Discord",
            Foreground = new SolidColorBrush(Color.FromArgb(255, 156, 163, 175)),
            FontSize = 11,
            FontWeight = FontWeights.Normal
        };

        userInfo.Children.Add(usernameText);
        userInfo.Children.Add(discordText);

        LoadDiscordUsernameAsync(usernameText, user.DiscordID);

        var addButton = new Button
        {
            Content = "?",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Width = 28,
            Height = 28,
            Margin = new Thickness(12, 0, 0, 0),
            Cursor = Cursors.Hand,
            Tag = user.UserID,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(1),
            ToolTip = "Add Friend"
        };

        var buttonGradient = new LinearGradientBrush();
        buttonGradient.StartPoint = new Point(0, 0);
        buttonGradient.EndPoint = new Point(1, 1);
        buttonGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 124, 58, 237), 0.0));
        buttonGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 139, 92, 246), 1.0));
        
        addButton.Background = buttonGradient;
        addButton.Foreground = new SolidColorBrush(Colors.White);
        addButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 124, 58, 237));

        var buttonTemplate = new ControlTemplate(typeof(Button));
        var buttonBorder = new FrameworkElementFactory(typeof(Border));
        buttonBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        buttonBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        buttonBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        buttonBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

        buttonBorder.AppendChild(contentPresenter);
        buttonTemplate.VisualTree = buttonBorder;
        addButton.Template = buttonTemplate;

        addButton.Click += async (s, e) =>
        {
            var btn = s as Button;
            if (btn?.Tag is int userId)
            {
                btn.IsEnabled = false;
                btn.Content = "?";

                try
                {
                    var success = await SendFriendRequestAsync(userId);
                    if (success)
                    {
                        btn.Content = "?";
                        
                        var successGradient = new LinearGradientBrush();
                        successGradient.StartPoint = new Point(0, 0);
                        successGradient.EndPoint = new Point(1, 1);
                        successGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 34, 197, 94), 0.0));
                        successGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 22, 163, 74), 1.0));
                        btn.Background = successGradient;
                        
                    }
                    else
                    {
                        btn.Content = "?";
                        
                        var errorGradient = new LinearGradientBrush();
                        errorGradient.StartPoint = new Point(0, 0);
                        errorGradient.EndPoint = new Point(1, 1);
                        errorGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 239, 68, 68), 0.0));
                        errorGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 220, 38, 38), 1.0));
                        btn.Background = errorGradient;
                        
                        btn.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    btn.Content = "?";
                    btn.Background = buttonGradient;
                    btn.IsEnabled = true;
                }
            }
        };

        stackPanel.Children.Add(avatarBorder);
        stackPanel.Children.Add(userInfo);
        
        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        Grid.SetColumn(stackPanel, 0);
        Grid.SetColumn(addButton, 1);
        
        mainGrid.Children.Add(stackPanel);
        mainGrid.Children.Add(addButton);
        
        userCard.Child = mainGrid;

        return userCard;
    }

    private async void LoadDiscordAvatarAsync(Border avatarBorder, string discordId, string fallbackUsername)
    {
        try
        {
            if (!string.IsNullOrEmpty(discordId))
            {
                var discordUser = await Services.DiscordLookupService.GetDiscordUserAsync(discordId);
                
                if (discordUser != null && !string.IsNullOrEmpty(discordUser.AvatarLink))
                {
                    var avatarImage = new Image
                    {
                        Source = await LoadBitmapImageWithTimeoutAsync(discordUser.AvatarLink),
                        Stretch = Stretch.UniformToFill,
                        Width = 36,
                        Height = 36
                    };
                    
                    avatarBorder.Child = avatarImage;
                    return;
                }
            }
            
            var avatarGradient = new LinearGradientBrush();
            avatarGradient.StartPoint = new Point(0, 0);
            avatarGradient.EndPoint = new Point(1, 1);
            avatarGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 124, 58, 237), 0.0));
            avatarGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 139, 92, 246), 1.0));
            avatarBorder.Background = avatarGradient;

            var avatarText = new TextBlock
            {
                Text = !string.IsNullOrEmpty(fallbackUsername) ? fallbackUsername.Substring(0, 1).ToUpper() : "?",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatarBorder.Child = avatarText;
        }
        catch (Exception ex)
        {
            
            var avatarGradient = new LinearGradientBrush();
            avatarGradient.StartPoint = new Point(0, 0);
            avatarGradient.EndPoint = new Point(1, 1);
            avatarGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 124, 58, 237), 0.0));
            avatarGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 139, 92, 246), 1.0));
            avatarBorder.Background = avatarGradient;

            var avatarText = new TextBlock
            {
                Text = !string.IsNullOrEmpty(fallbackUsername) ? fallbackUsername.Substring(0, 1).ToUpper() : "?",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatarBorder.Child = avatarText;
        }
    }

    private async void LoadDiscordUsernameAsync(TextBlock usernameText, string discordId)
    {
        try
        {
            if (!string.IsNullOrEmpty(discordId))
            {
                var discordUser = await Services.DiscordLookupService.GetDiscordUserAsync(discordId);
                
                if (discordUser != null && !string.IsNullOrEmpty(discordUser.Username))
                {
                    usernameText.Text = discordUser.DisplayName;
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void RefreshFriendRequestsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            
            LoadFriendRequests();
        }
        catch (Exception ex)
        {
        }
    }

    private void ClearUserSearchResults()
    {
        try
        {
            var userSearchResults = this.FindName("UserSearchResults") as StackPanel;
            var noSearchResultsText = this.FindName("NoSearchResultsText") as TextBlock;
            
            if (userSearchResults != null && noSearchResultsText != null)
            {
                userSearchResults.Children.Clear();
                userSearchResults.Children.Add(noSearchResultsText);
            }
        }
        catch (Exception ex)
        {
        }
    }

    private Border CreateUserSearchResultCard(User user)
    {
        var border = new Border
        {
            Background = (Brush)FindResource("CardBrush"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        var avatarBorder = new Border
        {
            Width = 35,
            Height = 35,
            CornerRadius = new CornerRadius(17.5),
            Background = (Brush)FindResource("AccentBrush"),
            Margin = new Thickness(0, 0, 12, 0)
        };

        var avatarText = new TextBlock
        {
            Text = user.Username?.Substring(0, 1).ToUpper() ?? "?",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        avatarBorder.Child = avatarText;
        Grid.SetColumn(avatarBorder, 0);

        var usernameText = new TextBlock
        {
            Text = user.Username ?? "Unknown",
            FontSize = 14,
            Foreground = (Brush)FindResource("TextBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(usernameText, 1);

        var addButton = new Button
        {
            Content = "? Ekle",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 6, 12, 6),
            Background = (Brush)FindResource("AccentBrush"),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Tag = user
        };

        addButton.Click += SendFriendRequestButton_Click;
        Grid.SetColumn(addButton, 2);

        grid.Children.Add(avatarBorder);
        grid.Children.Add(usernameText);
        grid.Children.Add(addButton);

        border.Child = grid;
        return border;
    }

    private async void SendFriendRequestButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var user = button?.Tag as User;
            
            if (user == null) return;

            button.IsEnabled = false;
            button.Content = LocalizationService.Instance.Translate("friend_request_sending");

await Task.Delay(1000);

            button.Content = "? Gönderildi";

await Task.Delay(2000);
            button.Content = "? Ekle";
            button.IsEnabled = true;
        }
        catch (Exception ex)
        {
        }
    }

    private async void LoadFriendRequests()
    {
        try
        {

            var friendRequests = await LoadIncomingFriendRequestsAsync();

if (!friendRequests.Any())
            {
                return;
            }

            foreach (var request in friendRequests)
            {
            }

        }
        catch (Exception ex)
        {
        }
    }

    private Border CreateFriendRequestCard(User user)
    {
        var requestCard = new Border
        {
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(12, 10, 12, 10),
            BorderThickness = new Thickness(1)
        };

        var gradientBrush = new LinearGradientBrush();
        gradientBrush.StartPoint = new Point(0, 0);
        gradientBrush.EndPoint = new Point(1, 1);
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 45, 45, 55), 0.0));
        gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 35, 35, 45), 1.0));
        requestCard.Background = gradientBrush;

        var borderBrush = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
        requestCard.BorderBrush = borderBrush;

        var dropShadow = new DropShadowEffect
        {
            Color = Color.FromArgb(255, 34, 197, 94),
            BlurRadius = 8,
            Opacity = 0.2,
            ShadowDepth = 0
        };
        requestCard.Effect = dropShadow;

        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var avatarBorder = new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(18),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        LoadDiscordAvatarAsync(avatarBorder, user.DiscordID, user.Username);

        var userInfo = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var usernameText = new TextBlock
        {
            Text = user.Username,
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 2)
        };

        var usernameGlow = new DropShadowEffect
        {
            Color = Color.FromArgb(255, 34, 197, 94),
            BlurRadius = 3,
            Opacity = 0.3,
            ShadowDepth = 0
        };
        usernameText.Effect = usernameGlow;

        var discordText = new TextBlock
        {
            Text = !string.IsNullOrEmpty(user.DiscordID) ? $"?? {user.DiscordID}" : "?? No Discord",
            Foreground = new SolidColorBrush(Color.FromArgb(255, 156, 163, 175)),
            FontSize = 11,
            FontWeight = FontWeights.Normal
        };

        userInfo.Children.Add(usernameText);
        userInfo.Children.Add(discordText);

        LoadDiscordUsernameAsync(usernameText, user.DiscordID);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };

        var acceptButton = new Button
        {
            Content = "?",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Width = 28,
            Height = 28,
            Margin = new Thickness(0, 0, 4, 0),
            Cursor = Cursors.Hand,
            Tag = user,
            BorderThickness = new Thickness(1),
            ToolTip = "Accept Request"
        };

        var acceptGradient = new LinearGradientBrush();
        acceptGradient.StartPoint = new Point(0, 0);
        acceptGradient.EndPoint = new Point(1, 1);
        acceptGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 34, 197, 94), 0.0));
        acceptGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 22, 163, 74), 1.0));
        
        acceptButton.Background = acceptGradient;
        acceptButton.Foreground = new SolidColorBrush(Colors.White);
        acceptButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));

        var declineButton = new Button
        {
            Content = "?",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Width = 28,
            Height = 28,
            Cursor = Cursors.Hand,
            Tag = user,
            BorderThickness = new Thickness(1),
            ToolTip = "Decline Request"
        };

        var declineGradient = new LinearGradientBrush();
        declineGradient.StartPoint = new Point(0, 0);
        declineGradient.EndPoint = new Point(1, 1);
        declineGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 239, 68, 68), 0.0));
        declineGradient.GradientStops.Add(new GradientStop(Color.FromArgb(255, 220, 38, 38), 1.0));
        
        declineButton.Background = declineGradient;
        declineButton.Foreground = new SolidColorBrush(Colors.White);
        declineButton.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));

        var buttonTemplate = new ControlTemplate(typeof(Button));
        var buttonBorder = new FrameworkElementFactory(typeof(Border));
        buttonBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        buttonBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        buttonBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        buttonBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

        buttonBorder.AppendChild(contentPresenter);
        buttonTemplate.VisualTree = buttonBorder;
        
        acceptButton.Template = buttonTemplate;
        declineButton.Template = buttonTemplate;

        acceptButton.Click += AcceptFriendRequestButton_Click;
        declineButton.Click += DeclineFriendRequestButton_Click;

        buttonPanel.Children.Add(acceptButton);
        buttonPanel.Children.Add(declineButton);

        stackPanel.Children.Add(avatarBorder);
        stackPanel.Children.Add(userInfo);
        
        var mainGrid = new Grid();
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        Grid.SetColumn(stackPanel, 0);
        Grid.SetColumn(buttonPanel, 1);
        
        mainGrid.Children.Add(stackPanel);
        mainGrid.Children.Add(buttonPanel);
        
        requestCard.Child = mainGrid;

        return requestCard;
    }

    private async void AcceptFriendRequestButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var user = button?.Tag as User;
            
            if (user == null) return;

var success = await AcceptFriendRequestAsync(user.UserID);

            if (success)
            {
                FrameworkElement? card = button;
                while (card != null && !(card is Border))
                {
                    card = card.Parent as FrameworkElement;
                }

                if (card != null)
                {
                    var parentPanel = card.Parent as Panel;
                    parentPanel?.Children.Remove(card);
                }

await UpdateFriendRequestBadgeAsync();

                await LoadIncomingFriendRequestsAsync();
                
                await UpdateFriendsListAsync();
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void DeclineFriendRequestButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            var user = button?.Tag as User;
            
            if (user == null) return;

var success = await DeclineFriendRequestAsync(user.UserID);

            if (success)
            {
                FrameworkElement? card = button;
                while (card != null && !(card is Border))
                {
                    card = card.Parent as FrameworkElement;
                }

                if (card != null)
                {
                    var parentPanel = card.Parent as Panel;
                    parentPanel?.Children.Remove(card);
                }

await UpdateFriendRequestBadgeAsync();

            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Friend API Methods

    private List<User>? _allUsersCache = null;
    private DateTime _cacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    private async Task<List<User>> GetAllUsersAsync()
    {
        if (_allUsersCache != null && DateTime.Now - _cacheTime < _cacheExpiry)
        {
            return _allUsersCache;
        }

        try
        {
            using (var client = new HttpClient())
            {
                
                var response = await WrightSkinsApiService.GetAsync("WrightUtils.E/launcher/api/all-users.php");
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseString);

                var allUsers = new List<User>();
                if (result?.success == true && result?.users != null)
                {
                    foreach (var userJson in result.users!)
                    {
                        allUsers.Add(new User
                        {
                            UserID = (int)userJson.id,
                            Username = (string)userJson.username,
                            DiscordID = (string)userJson.discord_id
                        });
                    }
                }

                _allUsersCache = allUsers;
                _cacheTime = DateTime.Now;

                return allUsers;
            }
        }
        catch (Exception ex)
        {
            return _allUsersCache ?? new List<User>();
        }
    }

    private async Task<bool> AcceptFriendRequestAsync(int requesterId)
    {
        try
        {
            var requestData = new
            {
                action = "accept_request",
                user_id = _currentUser?.UserID ?? 0,
                requester_id = requesterId
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/friend-requests.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseString);

                return result?.success == true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task<bool> DeclineFriendRequestAsync(int requesterId)
    {
        try
        {
            var requestData = new
            {
                action = "decline_request",
                user_id = _currentUser?.UserID ?? 0,
                requester_id = requesterId
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/friend-requests.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseString);

            return result?.success == true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task<bool> SendFriendRequestAsync(int targetUserId)
    {
        try
        {
            var requestData = new
            {
                action = "send_request",
                from_user_id = _currentUser?.UserID ?? 0,
                to_user_id = targetUserId
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/friend-requests.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseString);

            return result?.success == true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        try
        {
            var allUsers = await GetAllUsersAsync();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return allUsers.Take(20).ToList();
            }

            var filteredUsers = allUsers.Where(u => 
                (u.Username?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                (u.DiscordID?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
            ).Take(20).ToList();

            return filteredUsers;
        }
        catch (Exception ex)
        {
            return new List<User>();
        }
    }

    private async Task<List<User>> LoadIncomingFriendRequestsAsync()
    {
        try
        {
            var requestData = new
            {
                action = "get_incoming_requests",
                user_id = _currentUser?.UserID ?? 0
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/friend-requests.php", content);
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(responseString);

                var users = new List<User>();
                if (result?.success == true && result?.requests != null)
                {
                    var requestsCount = 0;
                    foreach (var req in result.requests)
                    {
                        requestsCount++;
                    }
                    
                    foreach (var requestJson in result.requests)
                    {
                        var user = new User
                        {
                            UserID = (int)requestJson.user_id,
                            Username = (string)requestJson.username,
                            DiscordID = (string)requestJson.discord_id
                        };
                        users.Add(user);
                    }
                }

Dispatcher.Invoke(() => UpdateFriendRequestsUI(users));

                return users;
        }
        catch (Exception ex)
        {
            return new List<User>();
        }
    }

    private void UpdateFriendRequestsUI(List<User> friendRequests)
    {
        try
        {

            var friendRequestsList = FindName("FriendRequestsList") as StackPanel;
            var noFriendRequestsText = FindName("NoFriendRequestsText") as TextBlock;

            if (friendRequestsList == null)
            {
                return;
            }

            friendRequestsList.Children.Clear();

            if (friendRequests.Count == 0)
            {
                if (noFriendRequestsText != null)
                {
                    noFriendRequestsText.Visibility = Visibility.Visible;
                    noFriendRequestsText.Text = "Arkadaþ isteði bulunmuyor";
                }
                return;
            }

            if (noFriendRequestsText != null)
            {
                noFriendRequestsText.Visibility = Visibility.Collapsed;
            }

            foreach (var request in friendRequests)
            {
                var requestCard = CreateFriendRequestCard(request);
                friendRequestsList.Children.Add(requestCard);
            }

        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Friend Invitation Popup Methods

    public void ShowFriendInvitationPopup(string inviterName, int inviterId)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                InviterNameText.Text = inviterName;
                InviterAvatarText.Text = inviterName.Substring(0, 1).ToUpper();
                
                FriendInvitationPopup.Tag = inviterId;
                
                FriendInvitationPopup.Visibility = Visibility.Visible;
                
                var slideIn = new DoubleAnimation
                {
                    From = 400,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                var transform = new TranslateTransform();
                FriendInvitationPopup.RenderTransform = transform;
                transform.BeginAnimation(TranslateTransform.XProperty, slideIn);
                
            });
        }
        catch (Exception ex)
        {
        }
    }

    private void CloseInvitationPopupButton_Click(object sender, RoutedEventArgs e)
    {
        HideFriendInvitationPopup();
    }

    private void HideFriendInvitationPopup()
    {
        try
        {
            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = 400,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            
            slideOut.Completed += (s, e) =>
            {
                FriendInvitationPopup.Visibility = Visibility.Collapsed;
                FriendInvitationPopup.Tag = null;
            };
            
            var transform = FriendInvitationPopup.RenderTransform as TranslateTransform;
            if (transform != null)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            }
            else
            {
                FriendInvitationPopup.Visibility = Visibility.Collapsed;
                FriendInvitationPopup.Tag = null;
            }
            
        }
        catch (Exception ex)
        {
        }
    }

    private async void AcceptInvitationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var inviterId = (int?)FriendInvitationPopup.Tag;
            if (inviterId == null || _currentUser == null) return;

var success = await AcceptFriendRequestAsync(inviterId.Value);

            if (success)
            {
                
                await UpdateFriendsListAsync();
                
                await LoadIncomingFriendRequestsAsync();
            }
            else
            {
            }

            HideFriendInvitationPopup();
        }
        catch (Exception ex)
        {
        }
    }

    private async void DeclineInvitationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var inviterId = (int?)FriendInvitationPopup.Tag;
            if (inviterId == null || _currentUser == null) return;

var success = await DeclineFriendRequestAsync(inviterId.Value);

            if (success)
            {
                
                await LoadIncomingFriendRequestsAsync();
            }
            else
            {
            }

            HideFriendInvitationPopup();
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Lobby Invite Popup Methods (Simple)

    public void ShowLobbyInvitePopup(string inviterName, string lobbyCode, string lobbyCreator, int memberCount)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                if (_lobbyPopup != null)
                {
                    var parentGrid = _lobbyPopup.Parent as Grid;
                    parentGrid?.Children.Remove(_lobbyPopup);
                }

                _lobbyPopup = new LobbyInvitationPopup();
                _lobbyPopup.SetInvitationData(lobbyCode, "unknown", inviterName, lobbyCreator, memberCount);
                
                _lobbyPopup.OnAccept += () => AcceptLobbyPopupButton_Click(null!, null!);
                _lobbyPopup.OnDecline += () => DeclineLobbyPopupButton_Click(null!, null!);
                _lobbyPopup.OnClose += () => HideLobbyInvitePopup();

                _lobbyPopup.HorizontalAlignment = HorizontalAlignment.Right;
                _lobbyPopup.VerticalAlignment = VerticalAlignment.Bottom;
                _lobbyPopup.Margin = new Thickness(0, 0, 20, 80);
                
                var mainGrid = this.FindName("MainGrid") as Grid;
                if (mainGrid != null)
                {
                    Grid.SetRowSpan(_lobbyPopup, 2);
                    Panel.SetZIndex(_lobbyPopup, 99999);
                    
                    mainGrid.Children.Add(_lobbyPopup);
                    _lobbyPopup.ShowWithAnimation();
                }
                
            });
        }
        catch (Exception ex)
        {
        }
    }

    private void CloseLobbyPopupButton_Click(object sender, RoutedEventArgs e)
    {
        HideLobbyInvitePopup();
    }

    private void HideLobbyInvitePopup()
    {
        try
        {
            if (_lobbyPopup != null)
            {
                _lobbyPopup.HideWithAnimation();
                var parentGrid = _lobbyPopup.Parent as Grid;
                parentGrid?.Children.Remove(_lobbyPopup);
                _lobbyPopup = null;
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void AcceptLobbyPopupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_lobbyPopup == null || _currentUser == null) return;

            var lobbyCode = _lobbyPopup.LobbyCode;
            var fromUserId = _lobbyPopup.FromUserId;
            var fromUsername = _lobbyPopup.FromUsername;

await _socketIOService.AcceptLobbyInviteAsync(lobbyCode, fromUserId, _currentUser.Username);

HideLobbyInvitePopup();
        }
        catch (Exception ex)
        {
        }
    }

    private async void DeclineLobbyPopupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_lobbyPopup == null || _currentUser == null) return;

            var lobbyCode = _lobbyPopup.LobbyCode;

await _socketIOService.DeclineLobbyInviteAsync(lobbyCode, _currentUser.UserID.ToString());

HideLobbyInvitePopup();
        }
        catch (Exception ex)
        {
        }
    }

    private void InitializeNotificationManager()
    {
        try
        {
        }
        catch (Exception ex)
        {
        }
    }

    #region Download Server Settings

    private async void GithubServerButton_Click(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        try
        {
            await SetDownloadServer("Github");
        }
        catch (Exception ex)
        {
        }
    }

    private async void WrightSkinsServerButton_Click(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        try
        {
            await SetDownloadServer("WrightSkins");
        }
        catch (Exception ex)
        {
        }
    }

    private async Task SetDownloadServer(string serverName)
    {
        try
        {
            if (serverName == "Github")
            {
                GithubServerButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
                GithubServerButton.Foreground = Brushes.White;
                WrightSkinsServerButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F23"));
                WrightSkinsServerButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            }
            else
            {
                WrightSkinsServerButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
                WrightSkinsServerButton.Foreground = Brushes.White;
                GithubServerButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F1F23"));
                GithubServerButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            }

            await SaveDownloadServerSetting(serverName);
        }
        catch (Exception ex)
        {
        }
    }

    private async Task SaveDownloadServerSetting(string serverName)
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wrightSkinsPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins");
            
            if (!Directory.Exists(wrightSkinsPath))
            {
                Directory.CreateDirectory(wrightSkinsPath);
            }

            string settingsFilePath = System.IO.Path.Combine(wrightSkinsPath, "download_server.json");
            var setting = new { DownloadServer = serverName };
            string jsonContent = JsonConvert.SerializeObject(setting, Formatting.Indented);
            
            await File.WriteAllTextAsync(settingsFilePath, jsonContent);
        }
        catch (Exception ex)
        {
        }
    }

private async Task<string> GetCurrentDownloadServer()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string settingsFilePath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "download_server.json");

            if (File.Exists(settingsFilePath))
            {
                string jsonContent = await File.ReadAllTextAsync(settingsFilePath);
                var setting = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                string savedServer = setting?.DownloadServer ?? "Github";
                return savedServer;
            }
            else
            {
                return "Github";
            }
        }
        catch (Exception ex)
        {
            return "Github";
        }
    }

    private async Task LoadDownloadServerSetting()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string settingsFilePath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "download_server.json");
            
            if (File.Exists(settingsFilePath))
            {
                string jsonContent = await File.ReadAllTextAsync(settingsFilePath);
                var setting = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                string savedServer = setting?.DownloadServer ?? "Github";
                
                await SetDownloadServer(savedServer);
                
            }
            else
            {
                await SetDownloadServer("Github");
            }
        }
        catch (Exception ex)
        {
            await SetDownloadServer("Github");
        }
    }

    private string GetWrightSkinsImageUrl(string type, string skinId)
    {
        return $"WrightUtils.E/cdn/ingame/skin_assets/{type}_{skinId}.jpg";
    }

    private async Task<string> GetImageUrlForCurrentServer(string communityDragonUrl, string type, int? skinId = null)
    {
        try
        {
            var currentServer = await GetCurrentDownloadServer();
            
            if (currentServer == "WrightSkins" && skinId.HasValue)
            {
                var wrightSkinsUrl = GetWrightSkinsImageUrl(type, skinId.Value.ToString());
                return wrightSkinsUrl;
            }
            
            return communityDragonUrl;
        }
        catch (Exception ex)
        {
            return communityDragonUrl;
        }
    }

    private async Task LoadGamePathToUI()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string configPath = System.IO.Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "config.json");
            
            if (File.Exists(configPath))
            {
                string jsonContent = await File.ReadAllTextAsync(configPath);
                var config = JsonConvert.DeserializeObject<AppSettings>(jsonContent);
                
                if (config != null && !string.IsNullOrEmpty(config.GamePath))
                {
                    var gamePathTextBox = FindName("GamePathTextBox") as TextBox;
                    if (gamePathTextBox != null)
                    {
                        gamePathTextBox.Text = config.GamePath;
                    }
                }
                else
                {
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

    private string GetChampionCodename(string championName)
    {
        var codenames = new Dictionary<string, string>
        {
            { "Wukong", "MonkeyKing" },
            { "Aurelion Sol", "AurelionSol" },
            { "Bel'Veth", "Belveth" },
            { "Kai'Sa", "Kaisa" },
            { "Cho'Gath", "Chogath" },
            { "Fiddlesticks", "FiddleSticks" },
            { "Dr. Mundo", "DrMundo" },
            { "Jarvan IV", "JarvanIV" },
            { "K'Sante", "KSante" },
            { "Kha'Zix", "Khazix" },
            { "Kog'Maw", "KogMaw" },
            { "LeBlanc", "Leblanc" },
            { "Lee Sin", "LeeSin" },
            { "Master Yi", "MasterYi" },
            { "Miss Fortune", "MissFortune" },
            { "Nunu & Willump", "Nunu" },
            { "Rek'Sai", "RekSai" },
            { "Renata Glasc", "Renata" },
            { "Tahm Kench", "TahmKench" },
            { "Twisted Fate", "TwistedFate" },
            { "Vel'Koz", "Velkoz" },
            { "Xin Zhao", "XinZhao" }
        };
        
        return codenames.ContainsKey(championName) ? codenames[championName] : championName;
    }

    #endregion

    #region Performance Event Handlers

    private async void PerformanceModeCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            PerformanceOptimizationService.SetPerformanceMode(true);
            
            if (_appSettings?.Performance != null)
            {
                _appSettings.Performance.EnablePerformanceMode = true;
                await SaveAppSettingsAsync();
            }
            
        }
        catch (Exception ex)
        {
        }
    }

    private async void PerformanceModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            PerformanceOptimizationService.SetPerformanceMode(false);
            
            if (_appSettings?.Performance != null)
            {
                _appSettings.Performance.EnablePerformanceMode = false;
                await SaveAppSettingsAsync();
            }
            
        }
        catch (Exception ex)
        {
        }
    }

    private void MemoryCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        try
        {
            PerformanceOptimizationService.CleanupMemory();
            
            string cacheDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                                         "Riot Games", "League of Legends", "WrightSkins", "Cache");
            
            if (Directory.Exists(cacheDir))
            {
                int deletedFiles = 0;
                int deletedDirs = 0;
                
                foreach (string file in Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                        deletedFiles++;
                    }
                    catch (Exception fileEx)
                    {
                    }
                }
                
                foreach (string dir in Directory.GetDirectories(cacheDir, "*", SearchOption.AllDirectories).Reverse())
                {
                    try
                    {
                        Directory.Delete(dir, false);
                        deletedDirs++;
                    }
                    catch (Exception dirEx)
                    {
                    }
                }
                
            }
            else
            {
            }
            
            var button = sender as Button;
            if (button != null)
            {
                var originalText = ((TextBlock)button.Content).Text;
                ((TextBlock)button.Content).Text = "? Temizlendi!";
                
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s, args) =>
                {
                    ((TextBlock)button.Content).Text = originalText;
                    timer.Stop();
                };
                timer.Start();
            }
        }
        catch (Exception ex)
        {
        }
    }

    private void OnDiscordServerClick(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://bontur.com.tr/discord",
                UseShellExecute = true
            });
            
        }
        catch (Exception ex)
        {
        }
    }

    private async void ReduceAnimationsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_appSettings?.Performance != null)
            {
                _appSettings.Performance.ReduceAnimations = true;
                await SaveAppSettingsAsync();
            }
            
            SetAnimationsEnabled(false);
            
        }
        catch (Exception ex)
        {
        }
    }

    private async void ReduceAnimationsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_appSettings?.Performance != null)
            {
                _appSettings.Performance.ReduceAnimations = false;
                await SaveAppSettingsAsync();
            }
            
            SetAnimationsEnabled(true);
            
        }
        catch (Exception ex)
        {
        }
    }

    private void SetAnimationsEnabled(bool enabled)
    {
        try
        {
        }
        catch (Exception ex)
        {
        }
    }

    private async void GpuAccelerationCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_appSettings?.Performance != null)
            {
                _appSettings.Performance.EnableGPUAcceleration = true;
                await SaveAppSettingsAsync();
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void GpuAccelerationCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_appSettings?.Performance != null)
            {
                _appSettings.Performance.EnableGPUAcceleration = false;
                await SaveAppSettingsAsync();
            }
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #endregion

    #region Performance Settings Management

    private async Task LoadPerformanceSettingsAsync()
    {
        try
        {
            var appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WrightLauncher");
            var settingsPath = System.IO.Path.Combine(appDataPath, "appsettings.json");

            if (File.Exists(settingsPath))
            {
                var json = await File.ReadAllTextAsync(settingsPath);
                _appSettings = JsonConvert.DeserializeObject<AppSettings>(json);
            }

            if (_appSettings == null)
            {
                _appSettings = new AppSettings();
                await SaveAppSettingsAsync();
            }

            Dispatcher.Invoke(() =>
            {
                PerformanceModeCheckBox.IsChecked = _appSettings.Performance.EnablePerformanceMode;
                ReduceAnimationsCheckBox.IsChecked = _appSettings.Performance.ReduceAnimations;
                GpuAccelerationCheckBox.IsChecked = _appSettings.Performance.EnableGPUAcceleration;
                
                SetAnimationsEnabled(!_appSettings.Performance.ReduceAnimations);
            });

            if (_appSettings.Performance.EnablePerformanceMode)
            {
                PerformanceOptimizationService.SetPerformanceMode(true);
            }

        }
        catch (Exception ex)
        {
            _appSettings = new AppSettings();
        }
    }

    private async Task SaveAppSettingsAsync()
    {
        try
        {
            if (_appSettings == null) return;

            var appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WrightLauncher");
            Directory.CreateDirectory(appDataPath);
            
            var settingsPath = System.IO.Path.Combine(appDataPath, "appsettings.json");
            var json = JsonConvert.SerializeObject(_appSettings, Formatting.Indented);
            await File.WriteAllTextAsync(settingsPath, json);

        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Skin Upload UI Management
    
    private void OnSkinUploadUIUpdate(bool showAddButton, bool isCreator, bool everyoneCanUpload, bool hasSpecificPermission, string reason)
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                
                var addSkinButton = FindName("AddSkinButton") as Button;
                if (addSkinButton != null)
                {
                    addSkinButton.Visibility = showAddButton ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    var plusButton = FindName("PlusButton") as Button;
                    if (plusButton != null)
                    {
                        plusButton.Visibility = showAddButton ? Visibility.Visible : Visibility.Collapsed;
                    }
                    else
                    {
                    }
                }
            });
        }
        catch (Exception ex)
        {
        }
    }

    #region Friend Request Badge Methods

    private async Task UpdateFriendRequestBadgeAsync()
    {
        try
        {
            var requestData = new
            {
                action = "get_incoming_requests",
                user_id = _currentUser?.UserID ?? 0
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await WrightSkinsApiService.PostAsync("WrightUtils.E/launcher/api/friend-requests.php", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseString);

            if (result?.success == true && result?.requests != null)
            {
                var requestsCount = 0;
                foreach (var req in result.requests)
                {
                    requestsCount++;
                }

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.UpdateFriendRequestBadge(requestsCount);
                }
            }
            else
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ResetFriendRequestBadge();
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Custom Modal Methods

    private TaskCompletionSource<bool>? _modalResult;

    public Task<bool> ShowCustomModalAsync(string title, string message, ModalType type = ModalType.Information, bool showCancel = false)
    {
        return Dispatcher.Invoke(() =>
        {
            _modalResult = new TaskCompletionSource<bool>();

            if (ModalTitle != null) ModalTitle.Text = title;
            if (ModalMessage != null) ModalMessage.Text = message;

            switch (type)
            {
                case ModalType.Information:
                    if (ModalIcon != null) ModalIcon.Text = "??";
                    if (ModalOkButton != null) ModalOkButton.Background = new SolidColorBrush(Color.FromRgb(91, 33, 182));
                    break;
                case ModalType.Warning:
                    if (ModalIcon != null) ModalIcon.Text = "??";
                    if (ModalOkButton != null) ModalOkButton.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    break;
                case ModalType.Error:
                    if (ModalIcon != null) ModalIcon.Text = "?";
                    if (ModalOkButton != null) ModalOkButton.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    break;
                case ModalType.Success:
                    if (ModalIcon != null) ModalIcon.Text = "?";
                    if (ModalOkButton != null) ModalOkButton.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                    break;
                case ModalType.Question:
                    if (ModalIcon != null) ModalIcon.Text = "?";
                    if (ModalOkButton != null) ModalOkButton.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                    showCancel = true;
                    break;
            }

            if (ModalCancelButton != null) ModalCancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;

            if (CustomModalOverlay != null) CustomModalOverlay.Visibility = Visibility.Visible;
            
            if (CustomModalBorder != null)
            {
                var scaleTransform = new ScaleTransform(0.8, 0.8);
                CustomModalBorder.RenderTransform = scaleTransform;
                CustomModalBorder.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleAnimation = new DoubleAnimation
                {
                    From = 0.8,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var opacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(200)
                };

                CustomModalBorder.BeginAnimation(OpacityProperty, opacityAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }

            return _modalResult.Task;
        });
    }

    public Task ShowInfoModalAsync(string message, string? title = null)
    {
        return ShowCustomModalAsync(
            title ?? LocalizationService.Instance.Translate("InfoModalTitle"), 
            message, 
            ModalType.Information
        );
    }

    public Task ShowErrorModalAsync(string message, string? title = null)
    {
        return ShowCustomModalAsync(
            title ?? LocalizationService.Instance.Translate("ErrorModalTitle"), 
            message, 
            ModalType.Error
        );
    }

    public Task ShowWarningModalAsync(string message, string? title = null)
    {
        return ShowCustomModalAsync(
            title ?? LocalizationService.Instance.Translate("WarningModalTitle"), 
            message, 
            ModalType.Warning
        );
    }

    public Task ShowSuccessModalAsync(string message, string? title = null)
    {
        return ShowCustomModalAsync(
            title ?? LocalizationService.Instance.Translate("SuccessModalTitle"), 
            message, 
            ModalType.Success
        );
    }

    public Task<bool> ShowConfirmationModalAsync(string message, string? title = null)
    {
        return ShowCustomModalAsync(
            title ?? LocalizationService.Instance.Translate("ConfirmationModalTitle"), 
            message, 
            ModalType.Question, 
            true
        );
    }

    private void CloseModal(bool result = false)
    {
        var scaleAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.8,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };

        var opacityAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(150)
        };

        scaleAnimation.Completed += (s, e) =>
        {
            CustomModalOverlay.Visibility = Visibility.Collapsed;
            if (_modalResult != null && !_modalResult.Task.IsCompleted)
            {
                _modalResult.SetResult(result);
            }
        };

        if (CustomModalBorder.RenderTransform is ScaleTransform scaleTransform)
        {
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }
        CustomModalBorder.BeginAnimation(OpacityProperty, opacityAnimation);
    }

    private void ModalOkButton_Click(object sender, RoutedEventArgs e)
    {
        CloseModal(true);
    }

    private void ModalCancelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseModal(false);
    }

    private void ModalCloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseModal(false);
    }

    #endregion

    #endregion

    #region Wright ADV Modal Methods

    private void ShowTestModal(object sender, RoutedEventArgs e)
    {
        PlayUIClickSound();
        
        var modal = new Views.WrightAdvModal(_currentDiscordUser);
        modal.Show();
    }

    private async Task<string> GetUserRoleNameAsync(string discordId)
    {
        try
        {
            if (string.IsNullOrEmpty(discordId))
                return "Skin Creator";

            var staffInfo = await StaffService.GetCurrentUserStaffInfoAsync(discordId);
            
            if (staffInfo != null && !string.IsNullOrEmpty(staffInfo.CustomRoleName))
            {
                return staffInfo.CustomRoleName;
            }
            
            return "Skin Creator";
        }
        catch (Exception)
        {
            return "Skin Creator";
        }
    }

    private async Task UpdatePreviewModalRoleAsync()
    {
        try
        {
            if (DataContext is not MainViewModel viewModel || viewModel.SelectedSkin == null)
                return;

            var roleText = FindName("PreviewModalRoleText") as TextBlock;
            var authorName = FindName("PreviewModalAuthorName") as TextBlock;
            var authorNameDetail = FindName("PreviewModalAuthorNameDetail") as TextBlock;
            var modalIcon = FindName("ModalIcon") as TextBlock;
            var modalIconImage = FindName("ModalIconImage") as Image;
            
            if (roleText == null || authorName == null || authorNameDetail == null || modalIcon == null || modalIconImage == null)
                return;

            bool isSpecialSkin = false;
            
            if (viewModel.HomePageSkins != null)
            {
                var homePageSkin = viewModel.HomePageSkins.OfType<InstalledSkin>()
                    .FirstOrDefault(s => s.Id == viewModel.SelectedSkin.Id);
                isSpecialSkin = homePageSkin?.IsSpecial == true;
            }
            
            if (isSpecialSkin)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    authorName.Text = "WrightSkins Team";
                    authorNameDetail.Text = "WrightSkins Team";
                    roleText.Text = "wrightskins.com";
                    
                    modalIcon.Visibility = System.Windows.Visibility.Collapsed;
                    modalIconImage.Visibility = System.Windows.Visibility.Visible;
                    modalIconImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/wrightSmall.png"));
                });
            }
            else
            {
                if (viewModel.SelectedSkin.DiscordUser?.Id == null)
                    return;

                string customRoleName = await GetUserRoleNameAsync(viewModel.SelectedSkin.DiscordUser.Id);
                
                Dispatcher.BeginInvoke(() =>
                {
                    authorName.Text = viewModel.SelectedSkin.DiscordUser.DisplayName;
                    authorNameDetail.Text = viewModel.SelectedSkin.DiscordUser.DisplayName;
                    roleText.Text = customRoleName;
                    
                    modalIcon.Visibility = System.Windows.Visibility.Visible;
                    modalIconImage.Visibility = System.Windows.Visibility.Collapsed;
                });
            }
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Slideshow Methods

    private void InitializeSlideshow()
    {
        try
        {
            _ = LoadLatestSkinsForSlideshow();
            
            _slideshowTimer = new System.Windows.Threading.DispatcherTimer();
            _slideshowTimer.Interval = TimeSpan.FromSeconds(4);
            _slideshowTimer.Tick += OnSlideshowTimerTick;
            _slideshowTimer.Start();
            
        }
        catch (Exception ex)
        {
        }
    }

    private async Task LoadLatestSkinsForSlideshow()
    {
        try
        {
            
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync($"{WrightUtils.E}/forLauncher/skins.php?token={WrightUtils.D}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    
                    var apiResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    
                    var skinsData = apiResponse;
                    if (apiResponse is Newtonsoft.Json.Linq.JObject jObject && jObject["skins"] != null)
                    {
                        skinsData = jObject["skins"];
                    }
                    
                    if (skinsData is Newtonsoft.Json.Linq.JArray skinsArray)
                    {
                        var skins = skinsArray.ToObject<List<dynamic>>();
                        
                        if (skins != null && skins.Count > 0)
                        {
                            var latestSkins = skins
                                .OrderByDescending(s => {
                                    try 
                                    { 
                                        return Convert.ToInt32(s.id ?? s.Id ?? 0); 
                                    } 
                                    catch 
                                    { 
                                        return 0; 
                                    }
                                })
                                .Take(3)
                                .ToList();
                            
                            _slideData.Clear();
                            
                            foreach (var skin in latestSkins)
                            {
                                var slideData = new SlideSkinData
                                {
                                    Name = (skin.name ?? skin.Name ?? "Unknown Skin").ToString(),
                                    ImageUrl = (skin.image_card ?? skin.ImageCard ?? skin.imageCard ?? "").ToString(),
                                    Description = "Latest Skins"
                                };
                                _slideData.Add(slideData);
                            }

Dispatcher.Invoke(() => UpdateSlideshow(0));
                        }
                        else
                        {
                            LoadDefaultSlideData();
                        }
                    }
                }
                else
                {
                    LoadDefaultSlideData();
                }
            }
        }
        catch (Exception ex)
        {
            LoadDefaultSlideData();
        }
    }

    private void LoadDefaultSlideData()
    {
        _slideData.Clear();
        _slideData.Add(new SlideSkinData { Name = "WrightSkins Launcher", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Yasuo_0.jpg", Description = "Latest Skins" });
        _slideData.Add(new SlideSkinData { Name = "Premium Deneyim", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Akali_0.jpg", Description = "Latest Skins" });
        _slideData.Add(new SlideSkinData { Name = "Topluluk", ImageUrl = "https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Zed_0.jpg", Description = "Latest Skins" });
    }

    private void OnSlideshowTimerTick(object? sender, EventArgs e)
    {
        try
        {
            if (_slideData.Count > 0)
            {
                _currentSlideIndex = (_currentSlideIndex + 1) % _slideData.Count;
                UpdateSlideshow(_currentSlideIndex);
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async void UpdateSlideshow(int slideIndex)
    {
        try
        {
            if (_slideData.Count == 0) return;
            
            var slideImage1 = FindName("SlideImage1") as Image;
            var slideImage2 = FindName("SlideImage2") as Image;
            var slideImage3 = FindName("SlideImage3") as Image;
            var indicator1 = FindName("Indicator1") as Ellipse;
            var indicator2 = FindName("Indicator2") as Ellipse;
            var indicator3 = FindName("Indicator3") as Ellipse;
            var slideTitle = FindName("SlideTitle") as TextBlock;
            var slideDescription = FindName("SlideDescription") as TextBlock;

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
            slideImage1?.BeginAnimation(OpacityProperty, fadeOut);
            slideImage2?.BeginAnimation(OpacityProperty, fadeOut);
            slideImage3?.BeginAnimation(OpacityProperty, fadeOut);

            if (indicator1 != null && indicator2 != null && indicator3 != null)
            {
                indicator1.Opacity = slideIndex == 0 ? 1.0 : 0.4;
                indicator2.Opacity = slideIndex == 1 ? 1.0 : 0.4;
                indicator3.Opacity = slideIndex == 2 ? 1.0 : 0.4;
            }

            if (slideIndex < _slideData.Count)
            {
                var currentSlide = _slideData[slideIndex];
                if (slideTitle != null)
                    slideTitle.Text = currentSlide.Name;
                if (slideDescription != null)
                    slideDescription.Text = currentSlide.Description;
            }

            Image? currentImage = slideIndex switch
            {
                0 => slideImage1,
                1 => slideImage2,
                2 => slideImage3,
                _ => slideImage1
            };

            if (currentImage != null && slideIndex < _slideData.Count)
            {
                var imageUrl = _slideData[slideIndex].ImageUrl;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try
                    {
                        await LoadImageWithTimeoutAsync(currentImage, imageUrl);
                    }
                    catch (Exception imageEx)
                    {
                    }
                }
            }

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(500));
                
                switch (slideIndex)
                {
                    case 0:
                        slideImage1?.BeginAnimation(OpacityProperty, fadeIn);
                        break;
                    case 1:
                        slideImage2?.BeginAnimation(OpacityProperty, fadeIn);
                        break;
                    case 2:
                        slideImage3?.BeginAnimation(OpacityProperty, fadeIn);
                        break;
                }
            };
            timer.Start();
        }
        catch (Exception ex)
        {
        }
    }

    private void OnIndicatorClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (sender is not Ellipse indicator) return;

            var indicator2 = FindName("Indicator2") as Ellipse;
            var indicator3 = FindName("Indicator3") as Ellipse;

            _slideshowTimer?.Stop();

            int targetIndex = 0;
            if (indicator == indicator2) targetIndex = 1;
            else if (indicator == indicator3) targetIndex = 2;

            _currentSlideIndex = targetIndex;
            UpdateSlideshow(_currentSlideIndex);

            _slideshowTimer?.Start();
        }
        catch (Exception ex)
        {
        }
    }

    #endregion

    #region Update System

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button == null) return;

            button.IsEnabled = false;
            
            var stackPanel = button.Content as StackPanel;
            if (stackPanel?.Children.Count >= 2 && stackPanel.Children[1] is TextBlock textBlock)
            {
                textBlock.Text = LocalizationService.Instance.Translate("update_check_status");
            }

            var updateExePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WrightUpdate.exe");
            if (!File.Exists(updateExePath))
            {
                await ShowErrorModalAsync(LocalizationService.Instance.Translate("ErrorTitle"), LocalizationService.Instance.Translate("wright_update_not_found"));
                return;
            }

            var currentVersion = APP_VERSION;

var startInfo = new ProcessStartInfo
            {
                FileName = updateExePath,
                Arguments = $"-v \"{currentVersion}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

using var process = Process.Start(startInfo);
            if (process != null)
            {
                var outputBuffer = new StringBuilder();
                var errorBuffer = new StringBuilder();
                bool restartNeeded = false;
                bool updateFound = false;
                
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuffer.AppendLine(e.Data);
                        
                        _ = Dispatcher.InvokeAsync(() =>
                        {
                        });
                        
                        if (e.Data.Contains("UPDATE_RESULT: RESTART_NEEDED"))
                        {
                            restartNeeded = true;
                            updateFound = true;
                            
                            _ = Dispatcher.InvokeAsync(async () =>
                            {
                                try
                                {
                                    
                                    var modalTask = ShowInfoModalAsync(WrightUtils.UpdateMessage, WrightUtils.UpdateAvailable);
                                    var delayTask = Task.Delay(5000);
                                    
                                    await delayTask;
                                    Application.Current.Shutdown();
                                }
                                catch (Exception ex)
                                {
                                    Application.Current.Shutdown();
                                }
                            });
                        }
                        else if (e.Data.Contains("UPDATE_RESULT: NO_UPDATE_NEEDED"))
                        {
                            updateFound = true;
                            _ = Dispatcher.InvokeAsync(() =>
                            {
                            });
                        }
                    }
                };
                
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorBuffer.AppendLine(e.Data);
                        _ = Dispatcher.InvokeAsync(() =>
                        {
                        });
                    }
                };
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync();
                
                var finalOutput = outputBuffer.ToString();
                var finalError = errorBuffer.ToString();
                
                if (!string.IsNullOrEmpty(finalError))
                {
                }
                
                if (!restartNeeded && updateFound && finalOutput.Contains("UPDATE_RESULT: NO_UPDATE_NEEDED"))
                {
                    await ShowInfoModalAsync(LocalizationService.Instance.Translate("update_available_no_update_message"), LocalizationService.Instance.Translate("check_updates_button"));
                }
                else if (process.ExitCode != 0)
                {
                    await ShowErrorModalAsync(LocalizationService.Instance.Translate("error_title"), LocalizationService.Instance.Translate("update_check_error"));
                }
            }
            else
            {
                await ShowErrorModalAsync(LocalizationService.Instance.Translate("error_title"), LocalizationService.Instance.Translate("wright_update_failed"));
            }
        }
        catch (Exception ex)
        {
            await ShowErrorModalAsync(
                LocalizationService.Instance.Translate("ErrorModalTitle"),
                $"Güncelleme kontrolü sýrasýnda hata: {ex.Message}"
            );
        }
        finally
        {
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = true;
                
                var stackPanel = button.Content as StackPanel;
                if (stackPanel?.Children.Count >= 2 && stackPanel.Children[1] is TextBlock textBlock)
                {
                    textBlock.Text = LocalizationService.Instance.Translate("check_updates_button");
                }
            }
        }
    }

    private async Task PerformAutoUpdateCheckAsync()
    {
        try
        {

try
            {
                await Task.Delay(3000).ConfigureAwait(false);
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(() =>
                    {
                    });
                }
                else
                {
                }
            }
            catch (Exception delayEx)
            {
                return;
            }
            
            var writeLog = new Action<string, string>((message, level) =>
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(() => {
                    });
                }
                else
                {
                }
            });
            
            writeLog("?? Otomatik güncelleme kontrolü baþlatýlýyor...", "INFO");
            
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            writeLog($"?? Current Directory: {currentDir}", "INFO");
            
            var updateExePath = System.IO.Path.Combine(currentDir, "WrightUpdate.exe");
            writeLog($"?? Update path: {updateExePath}", "INFO");
            writeLog($"?? File exists: {File.Exists(updateExePath)}", "INFO");
            
            if (!File.Exists(updateExePath))
            {
                writeLog("?? WrightUpdate.exe bulunamadý - otomatik güncelleme kontrolü atlandý", "WARNING");
                return;
            }

            var currentVersion = APP_VERSION;
            
            writeLog($"?? Otomatik güncelleme kontrolü - Mevcut sürüm: {currentVersion}", "INFO");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = updateExePath,
                Arguments = $"-v \"{currentVersion}\" --silent",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            writeLog($"?? WrightUpdate baþlatýlýyor - Arguments: {startInfo.Arguments}", "DEBUG");

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var outputBuffer = new StringBuilder();
                var errorBuffer = new StringBuilder();
                bool restartNeeded = false;
                
                process.OutputDataReceived += (sender, e) =>
                {
                    writeLog($"?? OutputDataReceived event triggered - Data: '{e.Data ?? "NULL"}'", "DEBUG");
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuffer.AppendLine(e.Data);
                        writeLog($"?? Real-time UPDATE: {e.Data}", "DEBUG");
                        
                        if (e.Data.Contains("UPDATE_RESULT: RESTART_NEEDED"))
                        {
                            restartNeeded = true;
                            writeLog("?? REAL-TIME: Güncelleme mevcut - yeniden baþlatma gerekiyor", "WARNING");
                            
                            _ = Dispatcher.InvokeAsync(async () =>
                            {
                                try
                                {
                                    var modalTask = ShowInfoModalAsync(WrightUtils.UpdateMessage, WrightUtils.UpdateAvailable);
                                    var delayTask = Task.Delay(5000);
                                    
                                    await delayTask;
                                    writeLog("?? 5 saniye geçti - uygulama kapatýlýyor", "INFO");
                                    Application.Current.Shutdown();
                                }
                                catch (Exception ex)
                                {
                                    Application.Current.Shutdown();
                                }
                            });
                        }
                        else if (e.Data.Contains("UPDATE_RESULT: NO_UPDATE_NEEDED"))
                        {
                            writeLog("? REAL-TIME: Otomatik güncelleme kontrolü - uygulama güncel", "SUCCESS");
                        }
                    }
                };
                
                process.ErrorDataReceived += (sender, e) =>
                {
                    writeLog($"?? ErrorDataReceived event triggered - Data: '{e.Data ?? "NULL"}'", "DEBUG");
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorBuffer.AppendLine(e.Data);
                        writeLog($"? Real-time ERROR: {e.Data}", "ERROR");
                    }
                };
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                await process.WaitForExitAsync().ConfigureAwait(false);
                
                var finalOutput = outputBuffer.ToString();
                var finalError = errorBuffer.ToString();
                
                if (!string.IsNullOrEmpty(finalOutput))
                {
                    writeLog($"?? Final STDOUT:\n{finalOutput}", "DEBUG");
                }
                
                if (!string.IsNullOrEmpty(finalError))
                {
                    writeLog($"?? Final STDERR:\n{finalError}", "ERROR");
                }
                
                if (!restartNeeded && process.ExitCode == 0)
                {
                    if (finalOutput.Contains("UPDATE_RESULT: SUCCESS"))
                    {
                    }
                    else if (!finalOutput.Contains("UPDATE_RESULT: NO_UPDATE_NEEDED"))
                    {
                    }
                }
                else if (process.ExitCode != 0)
                {
                }
            }
        }
        catch (Exception ex)
        {
        }
    }

    private async Task LoadImageWithTimeoutAsync(Image imageControl, string imageUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            using var response = await httpClient.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                
                await Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    imageControl.Source = bitmap;
                });
            }
            else
            {
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (HttpRequestException httpEx)
        {
        }
        catch (Exception ex)
        {
        }
    }

    private async Task<BitmapImage?> LoadBitmapImageWithTimeoutAsync(string imageUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            using var response = await httpClient.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                return bitmap;
            }
            else
            {
            }
        }
        catch (TaskCanceledException)
        {
        }
        catch (HttpRequestException httpEx)
        {
        }
        catch (Exception ex)
        {
        }
        
        return null;
    }

    #region Filter and Sort Event Handlers

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.AllSkins))
        {
            PopulateTagsDropdown();
        }
        else if (e.PropertyName == nameof(MainViewModel.SearchText))
        {
            ApplyFiltersAndSort();
        }
    }

    private void NSFWCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFiltersAndSort();
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFiltersAndSort();
    }

    private void ApplyFiltersAndSort()
    {
        if (!(DataContext is MainViewModel viewModel) || viewModel.AllSkins == null) return;

        var filteredSkins = new List<Skin>(viewModel.AllSkins);

        if (!string.IsNullOrEmpty(viewModel.SearchText))
        {
            filteredSkins = filteredSkins.Where(s => 
                s.Name.Contains(viewModel.SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Champion.Contains(viewModel.SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Author.Contains(viewModel.SearchText, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        var selectedTags = TagsListPanel?.Children.OfType<CheckBox>()
            .Where(cb => cb.IsChecked == true && cb.Tag?.ToString() != "")
            .Select(cb => cb.Tag?.ToString())
            .ToList() ?? new List<string>();

        if (selectedTags.Count > 0)
        {
            filteredSkins = filteredSkins.Where(s => 
                s.Tags != null && selectedTags.Any(selectedTag =>
                    s.Tags.Contains(selectedTag, StringComparer.OrdinalIgnoreCase))
            ).ToList();
        }

        if (NSFWCheckBox?.IsChecked == true)
        {
            filteredSkins = filteredSkins.Where(s => s.Nsfw == true).ToList();
        }

        switch (_currentSortType)
        {
            case "newest":
                filteredSkins = filteredSkins.OrderByDescending(s => 
                    DateTime.TryParse(s.ApprovedTime, out var date) ? date : DateTime.MinValue).ToList();
                break;
            case "oldest":
                filteredSkins = filteredSkins.OrderBy(s => s.Id).ToList();
                break;
            case "popular":
                filteredSkins = filteredSkins.OrderByDescending(s => 
                    int.TryParse(s.Downloads, out var downloads) ? downloads : 0).ToList();
                break;
            case "name_asc":
                filteredSkins = filteredSkins.OrderBy(s => s.Name).ToList();
                break;
            case "name_desc":
                filteredSkins = filteredSkins.OrderByDescending(s => s.Name).ToList();
                break;
            default:
                filteredSkins = filteredSkins.OrderByDescending(s => 
                    DateTime.TryParse(s.ApprovedTime, out var date) ? date : DateTime.MinValue).ToList();
                break;
        }

        viewModel.FilteredSkins.Clear();
        foreach (var skin in filteredSkins)
        {
            viewModel.FilteredSkins.Add(skin);
        }

        UpdateSkinsPagination(filteredSkins, viewModel);
    }

    private void UpdateSkinsPagination(List<Skin> filteredSkins, MainViewModel viewModel)
    {
        const int itemsPerPage = 12;
        
        viewModel.SkinsTotalPages = (int)Math.Ceiling((double)filteredSkins.Count / itemsPerPage);
        
        if (viewModel.SkinsCurrentPage > viewModel.SkinsTotalPages && viewModel.SkinsTotalPages > 0)
        {
            viewModel.SkinsCurrentPage = viewModel.SkinsTotalPages;
        }
        else if (viewModel.SkinsCurrentPage < 1)
        {
            viewModel.SkinsCurrentPage = 1;
        }

        var startIndex = (viewModel.SkinsCurrentPage - 1) * itemsPerPage;
        var pagedSkins = filteredSkins.Skip(startIndex).Take(itemsPerPage).ToList();

        viewModel.PaginatedSkins.Clear();
        foreach (var skin in pagedSkins)
        {
            viewModel.PaginatedSkins.Add(skin);
        }

        viewModel.SkinsCanGoPrevious = viewModel.SkinsCurrentPage > 1;
        viewModel.SkinsCanGoNext = viewModel.SkinsCurrentPage < viewModel.SkinsTotalPages;
        viewModel.SkinsPageInfo = string.Format(LocalizationService.Instance.Translate("SkinsPageInfo"), viewModel.SkinsCurrentPage, Math.Max(1, viewModel.SkinsTotalPages), filteredSkins.Count);
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SkinsCurrentPage > 1)
        {
            viewModel.SkinsCurrentPage--;
            ApplyFiltersAndSort();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.SkinsCurrentPage < viewModel.SkinsTotalPages)
        {
            viewModel.SkinsCurrentPage++;
            ApplyFiltersAndSort();
        }
    }

    private void PopulateTagsDropdown()
    {
        try
        {
            if (TagsListPanel == null)
            {
                return;
            }

            if (!(DataContext is MainViewModel viewModel))
            {
                return;
            }

            if (viewModel.AllSkins == null)
            {
                return;
            }

TagsListPanel.Children.Clear();

            var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var skin in viewModel.AllSkins)
            {
                if (skin.Tags != null)
                {
                    foreach (var tag in skin.Tags)
                    {
                        if (!string.IsNullOrWhiteSpace(tag))
                        {
                            allTags.Add(tag.Trim());
                        }
                    }
                }
            }

var allTagsCheckBox = new CheckBox
            {
                Content = LocalizationService.Instance.Translate("all_tags_content"),
                Tag = "",
                IsChecked = true,
                Margin = new Thickness(0, 6, 0, 6),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(168, 85, 247)),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            
            var allTagsTemplate = new ControlTemplate(typeof(CheckBox));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(20, 168, 85, 247)));
            factory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(168, 85, 247)));
            factory.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            factory.SetValue(Border.PaddingProperty, new Thickness(12, 8, 12, 8));
            factory.SetValue(Border.MarginProperty, new Thickness(0, 2, 0, 2));
            
            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(contentFactory);
            
            allTagsTemplate.VisualTree = factory;
            
            var allTagsStyle = new Style(typeof(CheckBox));
            allTagsStyle.Setters.Add(new Setter(CheckBox.TemplateProperty, allTagsTemplate));
            
            var hoverTrigger = new Trigger { Property = CheckBox.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(CheckBox.BackgroundProperty, 
                new LinearGradientBrush(
                    Color.FromArgb(40, 168, 85, 247), 
                    Color.FromArgb(20, 124, 58, 237), 
                    90)));
            hoverTrigger.Setters.Add(new Setter(CheckBox.ForegroundProperty, new SolidColorBrush(Colors.White)));
            allTagsStyle.Triggers.Add(hoverTrigger);
            
            var checkedTrigger = new Trigger { Property = CheckBox.IsCheckedProperty, Value = true };
            checkedTrigger.Setters.Add(new Setter(CheckBox.BackgroundProperty, 
                new LinearGradientBrush(
                    Color.FromArgb(80, 168, 85, 247), 
                    Color.FromArgb(60, 124, 58, 237), 
                    45)));
            checkedTrigger.Setters.Add(new Setter(CheckBox.ForegroundProperty, new SolidColorBrush(Colors.White)));
            allTagsStyle.Triggers.Add(checkedTrigger);
            
            allTagsCheckBox.Style = allTagsStyle;
            allTagsCheckBox.Checked += TagCheckBox_Changed;
            allTagsCheckBox.Unchecked += TagCheckBox_Changed;
            TagsListPanel.Children.Add(allTagsCheckBox);

            foreach (var tag in allTags.OrderBy(t => t))
            {
                var checkBox = new CheckBox
                {
                    Content = $"??? {tag}",
                    Tag = tag,
                    IsChecked = false,
                    Margin = new Thickness(0, 3, 0, 3),
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                
                var tagTemplate = new ControlTemplate(typeof(CheckBox));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(15, 203, 213, 225)));
                borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(80, 156, 163, 175)));
                borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
                borderFactory.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
                borderFactory.SetValue(Border.MarginProperty, new Thickness(0, 1, 0, 1));
                
                var checkboxContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                checkboxContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                checkboxContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                borderFactory.AppendChild(checkboxContentFactory);
                
                tagTemplate.VisualTree = borderFactory;
                
                var tagStyle = new Style(typeof(CheckBox));
                tagStyle.Setters.Add(new Setter(CheckBox.TemplateProperty, tagTemplate));
                
                var tagHoverTrigger = new Trigger { Property = CheckBox.IsMouseOverProperty, Value = true };
                tagHoverTrigger.Setters.Add(new Setter(CheckBox.BackgroundProperty, 
                    new RadialGradientBrush(
                        Color.FromArgb(40, 168, 85, 247), 
                        Color.FromArgb(20, 124, 58, 237))
                    {
                        Center = new Point(0.5, 0.5),
                        RadiusX = 1.2,
                        RadiusY = 1.2
                    }));
                tagHoverTrigger.Setters.Add(new Setter(CheckBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(255, 255, 255))));
                tagStyle.Triggers.Add(tagHoverTrigger);
                
                var tagCheckedTrigger = new Trigger { Property = CheckBox.IsCheckedProperty, Value = true };
                tagCheckedTrigger.Setters.Add(new Setter(CheckBox.BackgroundProperty, 
                    new LinearGradientBrush(
                        Color.FromArgb(120, 168, 85, 247), 
                        Color.FromArgb(80, 124, 58, 237), 
                        90)));
                tagCheckedTrigger.Setters.Add(new Setter(CheckBox.ForegroundProperty, new SolidColorBrush(Colors.White)));
                tagCheckedTrigger.Setters.Add(new Setter(CheckBox.FontWeightProperty, FontWeights.SemiBold));
                tagStyle.Triggers.Add(tagCheckedTrigger);
                
                checkBox.Style = tagStyle;
                checkBox.Checked += TagCheckBox_Changed;
                checkBox.Unchecked += TagCheckBox_Changed;
                
                if (FindName("TagsListPanel") is StackPanel panel)
                {
                    panel.Children.Add(checkBox);
                }
            }

            UpdateTagsDisplayText();
        }
        catch (Exception ex)
        {
        }
    }

    private void TagsDropdownButton_Click(object sender, RoutedEventArgs e)
    {
        if (FindName("TagsPopup") is Popup popup)
        {
            popup.IsOpen = !popup.IsOpen;
            
            if (popup.IsOpen && FindName("TagsDropdownArrow") is System.Windows.Shapes.Path arrow)
            {
                var rotateAnimation = new DoubleAnimation(0, 180, TimeSpan.FromMilliseconds(200));
                var rotateTransform = new RotateTransform();
                arrow.RenderTransform = rotateTransform;
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            }
            else if (!popup.IsOpen && FindName("TagsDropdownArrow") is System.Windows.Shapes.Path arrow2)
            {
                var rotateAnimation = new DoubleAnimation(180, 0, TimeSpan.FromMilliseconds(200));
                var rotateTransform = new RotateTransform();
                arrow2.RenderTransform = rotateTransform;
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
            }
        }
    }

    private void TagCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!(sender is CheckBox checkBox)) return;
        if (!(FindName("TagsListPanel") is StackPanel panel)) return;

        if (checkBox.Tag?.ToString() == "")
        {
            if (checkBox.IsChecked == true)
            {
                foreach (CheckBox cb in panel.Children.OfType<CheckBox>())
                {
                    if (cb.Tag?.ToString() != "")
                    {
                        cb.IsChecked = false;
                    }
                }
            }
        }
        else
        {
            if (checkBox.IsChecked == true)
            {
                var allTagsCheckBox = panel.Children.OfType<CheckBox>().FirstOrDefault(cb => cb.Tag?.ToString() == "");
                if (allTagsCheckBox != null)
                {
                    allTagsCheckBox.IsChecked = false;
                }
            }

            if (!(FindName("TagsListPanel") is StackPanel panelCheck)) return;
            
            if (!panelCheck.Children.OfType<CheckBox>().Any(cb => cb.Tag?.ToString() != "" && cb.IsChecked == true))
            {
                var allTagsCheckBox = panelCheck.Children.OfType<CheckBox>().FirstOrDefault(cb => cb.Tag?.ToString() == "");
                if (allTagsCheckBox != null)
                {
                    allTagsCheckBox.IsChecked = true;
                }
            }
        }

        UpdateTagsDisplayText();
        if (DataContext is MainViewModel viewModel)
        {
            ApplyFiltersAndSort();
        }
    }

    private void UpdateTagsDisplayText()
    {
        if (!(FindName("TagsSelectedText") is TextBlock selectedText) || 
            !(FindName("TagsPlaceholder") is TextBlock placeholder) ||
            !(FindName("TagsListPanel") is StackPanel panel)) return;

        var selectedTags = panel.Children.OfType<CheckBox>()
            .Where(cb => cb.IsChecked == true && cb.Tag?.ToString() != "")
            .Select(cb => cb.Tag?.ToString())
            .Where(tag => !string.IsNullOrEmpty(tag))
            .ToList();

        var allTagsSelected = panel.Children.OfType<CheckBox>()
            .FirstOrDefault(cb => cb.Tag?.ToString() == "")?.IsChecked == true;

        if (allTagsSelected || selectedTags.Count == 0)
        {
            selectedText.Text = "";
            placeholder.Visibility = Visibility.Visible;
        }
        else
        {
            placeholder.Visibility = Visibility.Collapsed;
            if (selectedTags.Count == 1)
            {
                selectedText.Text = $"??? {selectedTags[0]}";
            }
            else
            {
                selectedText.Text = string.Format(LocalizationService.Instance.Translate("tags_selected_text"), selectedTags.Count);
            }
        }
    }

    #region Sort Dropdown Events

    private void SortDropdownButton_Click(object sender, RoutedEventArgs e)
    {
        if (FindName("SortDropdownPopup") is Popup popup)
        {
            popup.IsOpen = !popup.IsOpen;
            
            if (FindName("SortArrowRotation") is RotateTransform arrowRotation)
            {
                var animation = new DoubleAnimation
                {
                    To = popup.IsOpen ? 180 : 0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                arrowRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }
    }

    private void SortOption_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
        {
            var animation = new ColorAnimation
            {
                To = Color.FromArgb(51, 124, 58, 237),
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var brush = new SolidColorBrush(Colors.Transparent);
            border.Background = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
    }

    private void SortOption_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
        {
            var animation = new ColorAnimation
            {
                To = Colors.Transparent,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            if (border.Background is SolidColorBrush brush)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
        }
    }

    private void SortOption_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string sortType)
        {
            _currentSortType = sortType;
            
            if (FindName("SelectedSortText") is TextBlock selectedText)
            {
                selectedText.Text = sortType switch
                {
                    "newest" => "Sort by: Newest",
                    "oldest" => "Sort by: Oldest First", 
                    "popular" => "Sort by: Most Popular",
                    "name_asc" => "Sort by: Name (A-Z)",
                    "name_desc" => "Sort by: Name (Z-A)",
                    _ => "Sort by: Newest"
                };
            }

            if (FindName("SortDropdownPopup") is Popup popup)
            {
                popup.IsOpen = false;
                
                if (FindName("SortArrowRotation") is RotateTransform arrowRotation)
                {
                    var animation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    arrowRotation.BeginAnimation(RotateTransform.AngleProperty, animation);
                }
            }

            ApplyFiltersAndSort();
        }
    }

    #endregion

    #endregion

    private async Task ProcessFantomeFile(string fantomeFilePath, string wadFolderPath)
    {
        try
        {
            
            var tempExtractPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempExtractPath);
            
            try
            {
                ZipFile.ExtractToDirectory(fantomeFilePath, tempExtractPath);
                
                var extractedWadPath = System.IO.Path.Combine(tempExtractPath, "WAD");
                if (Directory.Exists(extractedWadPath))
                {
                    
                    var wadFiles = Directory.GetFiles(extractedWadPath, "*", SearchOption.AllDirectories);
                    foreach (var wadFile in wadFiles)
                    {
                        var fileName = System.IO.Path.GetFileName(wadFile);
                        var destPath = System.IO.Path.Combine(wadFolderPath, fileName);
                        
                        if (File.Exists(destPath))
                        {
                            File.Delete(destPath);
                        }
                        
                        File.Move(wadFile, destPath);
                    }
                }
                else
                {
                }
                
                if (File.Exists(fantomeFilePath))
                {
                    File.Delete(fantomeFilePath);
                }
            }
            finally
            {
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
            }
            
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public enum ModalType
    {
        Information,
        Warning,
        Error,
        Success,
        Question
    }

    private async Task LoadSpecialSkins()
    {
        try
        {
            if (_currentUser == null)
            {
                
                var specialButton = FindName("SpecialButton") as UIElement;
                if (specialButton != null)
                {
                    specialButton.Visibility = Visibility.Collapsed;
                }
                return;
            }

            var currentUserId = _currentUser.UserID.ToString();
            
            using (var client = new HttpClient())
            {
                string url = $"{WrightUtils.E}/forLauncher/personalskins.php?token={WrightUtils.D}";
                var response = await client.GetStringAsync(url);

var allSpecialSkins = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SpecialSkin>>(response);
                
                if (allSpecialSkins != null)
                {
                    
                    for (int i = 0; i < Math.Min(allSpecialSkins.Count, 5); i++)
                    {
                        var skin = allSpecialSkins[i];
                    }
                    
                    if (allSpecialSkins.Count > 5)
                    {
                    }
                    
                    var userSpecialSkins = allSpecialSkins
                        .Where(skin => skin.TargetID != null && skin.TargetID.Contains(currentUserId))
                        .ToList();

if (userSpecialSkins.Count > 0)
                    {
                        foreach (var userSkin in userSpecialSkins)
                        {
                        }
                    }

                    if (DataContext is MainViewModel viewModel)
                    {
                        viewModel.SpecialSkins = new System.Collections.ObjectModel.ObservableCollection<SpecialSkin>(userSpecialSkins);
                    }
                    
                    var specialButton = FindName("SpecialButton") as UIElement;
                    if (specialButton != null)
                    {
                        if (userSpecialSkins.Count > 0)
                        {
                            specialButton.Visibility = Visibility.Visible;
                            
                            specialButton.Opacity = 0;
                            var specialButtonAnimation = new System.Windows.Media.Animation.DoubleAnimation
                            {
                                From = 0,
                                To = 1,
                                Duration = TimeSpan.FromMilliseconds(300),
                                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                            };
                            specialButton.BeginAnimation(OpacityProperty, specialButtonAnimation);
                            
                        }
                        else
                        {
                            specialButton.Visibility = Visibility.Collapsed;
                        }
                    }
                    
                    var loadingPanel = FindName("SpecialSkinsLoadingPanel") as StackPanel;
                    if (loadingPanel != null)
                    {
                        loadingPanel.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    
                    var specialButton = FindName("SpecialButton") as UIElement;
                    if (specialButton != null)
                    {
                        specialButton.Visibility = Visibility.Collapsed;
                    }
                    
                    var loadingPanel = FindName("SpecialSkinsLoadingPanel") as StackPanel;
                    if (loadingPanel != null)
                    {
                        loadingPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            
            var specialButton = FindName("SpecialButton") as UIElement;
            if (specialButton != null)
            {
                specialButton.Visibility = Visibility.Collapsed;
            }
            
            var loadingPanel = FindName("SpecialSkinsLoadingPanel") as StackPanel;
            if (loadingPanel != null)
            {
                loadingPanel.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void PostYourOwnSkin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "WrightUtils.E/dashboard",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private void OnChampionImageCacheUpdated(string championName)
    {
        DebugConsoleWindow.Instance?.WriteLine($"[MainWindow] OnChampionImageCacheUpdated called for: {championName}", "DEBUG");
        
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                DebugConsoleWindow.Instance?.WriteLine($"[MainWindow] Processing UI update for: {championName}", "DEBUG");
                
                var championsListBox = FindName("ChampionsListBox") as System.Windows.Controls.ListBox;
                if (championsListBox != null)
                {
                    DebugConsoleWindow.Instance?.WriteLine($"[MainWindow] Forcing ListBox refresh for: {championName}", "DEBUG");
                    championsListBox.Items.Refresh();
                    DebugConsoleWindow.Instance?.WriteLine($"[SUCCESS] ListBox refreshed for: {championName}", "SUCCESS");
                }
                else
                {
                    DebugConsoleWindow.Instance?.WriteLine($"[WARNING] ChampionsListBox not found", "WARNING");
                }
                
                if (DataContext is MainViewModel viewModel && viewModel.ChampionsView != null)
                {
                    DebugConsoleWindow.Instance?.WriteLine($"[MainWindow] Refreshing ChampionsView for: {championName}", "DEBUG");
                    viewModel.ChampionsView.Refresh();
                    DebugConsoleWindow.Instance?.WriteLine($"[SUCCESS] ChampionsView refreshed for: {championName}", "SUCCESS");
                }
                
                DebugConsoleWindow.Instance?.WriteLine($"[SUCCESS] UI refresh completed for: {championName}", "SUCCESS");
            }
            catch (Exception ex)
            {
                DebugConsoleWindow.Instance?.WriteLine($"[ERROR] Error in OnChampionImageCacheUpdated: {ex.Message}", "ERROR");
            }
        }));
    }

    #region Skin Card Animation Methods
    
    private async Task AnimateImageTransition(ImageBrush imageBrush, string newImageUrl)
    {
        try
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeOutCompleted = new TaskCompletionSource<bool>();
            
            fadeOut.Completed += async (s, e) =>
            {
                var newBitmap = await LoadBitmapImageWithTimeoutAsync(newImageUrl);
                imageBrush.ImageSource = newBitmap;
                
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                
                fadeIn.Completed += (s2, e2) => fadeOutCompleted.SetResult(true);
                imageBrush.BeginAnimation(ImageBrush.OpacityProperty, fadeIn);
            };
            
            imageBrush.BeginAnimation(ImageBrush.OpacityProperty, fadeOut);
            
            await fadeOutCompleted.Task;
        }
        catch (Exception ex)
        {
            try
            {
                var fallbackBitmap = await LoadBitmapImageWithTimeoutAsync(newImageUrl);
                imageBrush.ImageSource = fallbackBitmap;
                imageBrush.Opacity = 1.0;
            }
            catch
            {
                imageBrush.Opacity = 1.0;
            }
        }
    }

    private async Task AnimateSkinCardEntrance(Border skinCard, int index)
    {
        skinCard.Opacity = 0.0;
        
        var transform = new TranslateTransform(0, -30);
        skinCard.RenderTransform = transform;
        
        var delay = index * 50;
        if (delay > 0)
        {
            await Task.Delay(delay);
        }
        
        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        var slideDown = new DoubleAnimation
        {
            From = -30,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(500),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        skinCard.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        transform.BeginAnimation(TranslateTransform.YProperty, slideDown);
    }
    
    private string CleanSkinNameForFileSystem(string skinName)
    {
        if (string.IsNullOrEmpty(skinName))
            return "Unknown";
            
        var cleanName = skinName;
        
        cleanName = cleanName.Replace("/", " ");
        cleanName = cleanName.Replace(":", "");
        
        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            if (c != '/' && c != ':')
            {
                cleanName = cleanName.Replace(c, '_');
            }
        }
        
        cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\s+", " ").Trim();
        
        return cleanName;
    }
    
    #endregion

    #endregion
}
}








