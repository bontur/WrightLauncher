using System;
using System.Text.Json;
using System.Threading.Tasks;
using SocketIOClient;
using Newtonsoft.Json;
using WrightLauncher.Views;
using WrightLauncher.Utilities;
using System.Net.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;

namespace WrightLauncher.Services
{
    public class SocketIORealtimeService
    {
        private SocketIOClient.SocketIO _socket;
        private string _currentUserId;
        private string _currentUsername;
        private bool _isConnected = false;
        private readonly HttpClient _httpClient;
        private readonly string _serverBaseUrl = WrightUtils.F; // Uses configuration from WrightUtils
        
        public bool IsConnected => _socket?.Connected ?? false;
        
        public Func<string> GetCurrentLobbyCode { get; set; }

        public event Action<string, string> UserJoined;
        public event Action<string, string> UserLeft;
        public event Action<string, string, RealtimeSkinData, bool> SkinAdded;
        public event Action<string, string, string, string> FileRequest;
        public event Action<string, string, string, List<RealtimeFileData>, string> FilesReceived;

        public event Action<string, string, string> P2PSkinRequest;
        public event Action<string> SkinRequestSent;
        public event Action<string> SkinRequestError;
        public event Action<int> SkinAnnouncementSuccess;
        public event Action<string, string, string, bool> P2PConnectionOffer;
        public event Action<string, dynamic> P2PSignaling;

        public event Action<string, string, long, string> SkinAvailableForDownload;
        public event Action<string, string> SkinCleanupCompleted;
        public event Action<string, bool> ExistingSkinFileReceived;

        public event Action<string, string, bool> FriendStatusChanged;
        public event Action<string, string, string> FriendAdded;
        public event Action<string> FriendRemoved;
        public event Action<string, string> FriendsListUpdateRequired;
        
        public event Action<string, string> FriendRequestReceived;
        public event Action<string> FriendRequestSent;
        public event Action<string, string> FriendRequestAccepted;
        public event Action<string> FriendRequestDeclined;
        
        public event Action<string, string, string, string, int> LobbyInviteReceived;
        public event Action<string, string> LobbyInviteSent;
        public event Action<string, string, string> LobbyInviteAccepted;
        public event Action<string, string> LobbyInviteDeclined;
        public event Action<string> LobbyInviteError;
        
        public event Action<string> SkinRemoved;
        public event Action<string> LobbyUpdated;
        public event Action<dynamic> LobbyCreated;
        public event Action<dynamic> LobbyJoined;
        public event Action<string> LobbyLeft;
        public event Action<string> LobbyDisbanded;
        public event Action<string> LobbyError;
        public event Action<dynamic> LobbyMembersUpdated;
        public event Action<int, dynamic> UserLeftLobby;
        public event Action<dynamic> SkinAddSuccess;
        public event Action<string> SkinAddError;
        
        public event Action NewModsInLobbyDetected;
        
        public event Action<bool, bool, bool, bool, string>? SkinUploadUIUpdate;

        public SocketIORealtimeService()
        {
            _socket = new SocketIOClient.SocketIO(_serverBaseUrl);
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            _socket.OnConnected += async (sender, e) =>
            {
                _isConnected = true;
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                if (!string.IsNullOrEmpty(_currentUserId))
                {
                    await JoinUserChannel();
                }
            };

            _socket.OnDisconnected += (sender, e) =>
            {
                _isConnected = false;
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            };

            _socket.OnError += (sender, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            };

            _socket.On("friend_removed", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string friendId = null;
                    
                    if (jsonElement.TryGetProperty("friendId", out var friendIdElement))
                    {
                        friendId = friendIdElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    FriendRemoved?.Invoke(friendId);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("friend_request_received", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string fromUserId = null;
                    string fromUsername = null;
                    
                    if (jsonElement.TryGetProperty("fromUserId", out var fromUserIdElement))
                    {
                        fromUserId = fromUserIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("fromUsername", out var fromUsernameElement))
                    {
                        fromUsername = fromUsernameElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    FriendRequestReceived?.Invoke(fromUserId, fromUsername);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("friend_request_sent", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string targetUserId = null;
                    
                    if (jsonElement.TryGetProperty("targetUserId", out var targetUserIdElement))
                    {
                        targetUserId = targetUserIdElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    FriendRequestSent?.Invoke(targetUserId);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("friend_request_accepted", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string byUserId = null;
                    string byUsername = null;
                    
                    if (jsonElement.TryGetProperty("byUserId", out var byUserIdElement))
                    {
                        byUserId = byUserIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("byUsername", out var byUsernameElement))
                    {
                        byUsername = byUsernameElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    FriendRequestAccepted?.Invoke(byUserId, byUsername);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("friend_request_declined", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string byUserId = null;
                    
                    if (jsonElement.TryGetProperty("byUserId", out var byUserIdElement))
                    {
                        byUserId = byUserIdElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    FriendRequestDeclined?.Invoke(byUserId);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_invite_received", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string fromUserId = null;
                    string fromUsername = null;
                    string lobbyCode = null;
                    string lobbyCreator = null;
                    int memberCount = 0;
                    
                    if (jsonElement.TryGetProperty("fromUserId", out var fromUserIdElement))
                    {
                        fromUserId = fromUserIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("fromUsername", out var fromUsernameElement))
                    {
                        fromUsername = fromUsernameElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("lobbyCode", out var lobbyCodeElement))
                    {
                        lobbyCode = lobbyCodeElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("lobbyCreator", out var lobbyCreatorElement))
                    {
                        lobbyCreator = lobbyCreatorElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("memberCount", out var memberCountElement))
                    {
                        memberCount = memberCountElement.GetInt32();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyInviteReceived?.Invoke(fromUserId, fromUsername, lobbyCode, lobbyCreator, memberCount);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_invite_sent", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string targetUserId = null;
                    string lobbyCode = null;
                    
                    if (jsonElement.TryGetProperty("targetUserId", out var targetUserIdElement))
                    {
                        targetUserId = targetUserIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("lobbyCode", out var lobbyCodeElement))
                    {
                        lobbyCode = lobbyCodeElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyInviteSent?.Invoke(targetUserId, lobbyCode);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_invite_accepted", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string byUserId = null;
                    string byUsername = null;
                    string lobbyCode = null;
                    
                    if (jsonElement.TryGetProperty("byUserId", out var byUserIdElement))
                    {
                        byUserId = byUserIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("byUsername", out var byUsernameElement))
                    {
                        byUsername = byUsernameElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("lobbyCode", out var lobbyCodeElement))
                    {
                        lobbyCode = lobbyCodeElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyInviteAccepted?.Invoke(byUserId, byUsername, lobbyCode);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_invite_declined", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string byUserId = null;
                    string lobbyCode = null;
                    
                    if (jsonElement.TryGetProperty("byUserId", out var byUserIdElement))
                    {
                        byUserId = byUserIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("lobbyCode", out var lobbyCodeElement))
                    {
                        lobbyCode = lobbyCodeElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyInviteDeclined?.Invoke(byUserId, lobbyCode);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_invite_error", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string error = null;
                    
                    if (jsonElement.TryGetProperty("error", out var errorElement))
                    {
                        error = errorElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyInviteError?.Invoke(error);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("friends_list_update_required", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string reason = null;
                    string friendId = null;
                    
                    if (jsonElement.TryGetProperty("reason", out var reasonElement))
                    {
                        reason = reasonElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("friendId", out var friendIdElement))
                    {
                        friendId = friendIdElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    FriendsListUpdateRequired?.Invoke(reason, friendId);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("remove_skin_from_profile", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinName = null;
                    
                    if (jsonElement.TryGetProperty("skinName", out var skinNameElement))
                    {
                        skinName = skinNameElement.GetString();
                    }
                    
                    if (!string.IsNullOrEmpty(skinName))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                            RemoveSkinFromWrightProfile(skinName);
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("injected_counter_updated", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinName = null;
                    int newCount = 0;
                    string action = null;
                    
                    if (jsonElement.TryGetProperty("skinName", out var skinNameElement))
                    {
                        skinName = skinNameElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("newCount", out var newCountElement))
                    {
                        newCount = newCountElement.GetInt32();
                    }
                    
                    if (jsonElement.TryGetProperty("action", out var actionElement))
                    {
                        action = actionElement.GetString();
                    }
                    
                    if (!string.IsNullOrEmpty(skinName))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("force_skin_deselect", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = null;
                    string skinName = null;
                    
                    if (jsonElement.TryGetProperty("skinId", out var skinIdElement))
                    {
                        skinId = skinIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("skinName", out var skinNameElement))
                    {
                        skinName = skinNameElement.GetString();
                    }
                    
                    if (!string.IsNullOrEmpty(skinId) && !string.IsNullOrEmpty(skinName))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                            ForceSkinDeselect(skinId, skinName);
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                    });
                }
            });

            _socket.On("clear_skin_selection", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = null;
                    string skinName = null;
                    
                    if (jsonElement.TryGetProperty("skinId", out var skinIdElement))
                    {
                        skinId = skinIdElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("skinName", out var skinNameElement))
                    {
                        skinName = skinNameElement.GetString();
                    }
                    
                    if (!string.IsNullOrEmpty(skinId) && !string.IsNullOrEmpty(skinName))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                            ClearSkinSelection(skinId, skinName);
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                    });
                }
            });

            _socket.On("existing_skin_file_transfer", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = jsonElement.GetProperty("skinId").GetString();
                    string skinName = jsonElement.GetProperty("skinName").GetString();
                    string uploadedBy = jsonElement.GetProperty("uploadedBy").GetString();
                    long fileSize = jsonElement.GetProperty("fileSize").GetInt64();
                    string originalName = jsonElement.GetProperty("originalName").GetString();
                    string filePath = jsonElement.GetProperty("filePath").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        
                        UpdateSkinDownloadProgress(skinName, 0);
                    });
                    
                    Task.Run(async () => {
                        try
                        {
                            if (uploadedBy == _currentUserId)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    CompleteSkinDownload(skinName);
                                });
                                return;
                            }

                            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            string skinsDirectory = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
                            string extractPath = Path.Combine(skinsDirectory, skinName);

                            if (Directory.Exists(extractPath))
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    
                                    CompleteSkinDownload(skinName);
                                    
                                    ExistingSkinFileReceived?.Invoke(skinName, true);
                                });
                                return;
                            }
                            
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                
                                QueueSkinForDownload(skinName);
                                
                                StartSkinDownload(skinName);
                                
                                UpdateSkinDownloadProgress(skinName, 10);
                            });
                            
                            _socket.EmitAsync("request_existing_skin_file", new {
                                skinId = skinId,
                                skinName = skinName,
                                filePath = filePath
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                CompleteSkinDownload(skinName);
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                    });
                }
            });

            _socket.On("existing_skin_file_data", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = jsonElement.GetProperty("skinId").GetString();
                    string skinName = jsonElement.GetProperty("skinName").GetString();
                    string fileData = jsonElement.GetProperty("fileData").GetString();
                    long fileSize = jsonElement.GetProperty("fileSize").GetInt64();
                    string originalName = jsonElement.GetProperty("originalName").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    Task.Run(async () => {
                        try
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                UpdateSkinDownloadProgress(skinName, 50);
                            });
                            
                            await ProcessExistingSkinFileData(skinName, fileData, fileSize, originalName);
                            
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                CompleteSkinDownload(skinName);
                                ExistingSkinFileReceived?.Invoke(skinName, true);
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                ResetSkinDownloadStatus(skinName);
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                    });
                }
            });

            _socket.On("existing_skin_file_error", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinName = jsonElement.GetProperty("skinName").GetString();
                    string error = jsonElement.GetProperty("error").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        
                        if (!string.IsNullOrEmpty(skinName))
                        {
                            ResetSkinDownloadStatus(skinName);
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                    });
                }
            });

            _socket.On("friends_list_update_required", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string reason = null;
                    string friendId = null;
                    
                    if (jsonElement.TryGetProperty("reason", out var reasonElement))
                    {
                        reason = reasonElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("friendId", out var friendIdElement))
                    {
                        friendId = friendIdElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    FriendsListUpdateRequired?.Invoke(reason, friendId);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_added", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    var skinDataElement = jsonElement.GetProperty("skinData");
                    var addedByElement = jsonElement.GetProperty("addedBy");
                    
                    var skinData = new RealtimeSkinData();
                    if (skinDataElement.TryGetProperty("skin_id", out var skinIdProp))
                        skinData.Id = skinIdProp.GetString();
                    if (skinDataElement.TryGetProperty("skin_name", out var skinNameProp))
                        skinData.Name = skinNameProp.GetString();
                    if (skinDataElement.TryGetProperty("champion_name", out var championProp))
                        skinData.Champion = championProp.GetString();
                    if (skinDataElement.TryGetProperty("version", out var versionProp))
                        skinData.Version = versionProp.GetString();
                    if (skinDataElement.TryGetProperty("is_builded", out var buildedProp))
                        skinData.IsBuilded = buildedProp.GetBoolean();
                    if (skinDataElement.TryGetProperty("is_custom", out var customProp))
                        skinData.IsCustom = customProp.GetBoolean();
                    if (skinDataElement.TryGetProperty("image_card", out var imageCardProp))
                        skinData.ImageCard = imageCardProp.GetString();
                    
                    string addedBy;
                    if (addedByElement.ValueKind == JsonValueKind.String)
                    {
                        addedBy = addedByElement.GetString();
                    }
                    else if (addedByElement.ValueKind == JsonValueKind.Number)
                    {
                        addedBy = addedByElement.GetInt32().ToString();
                    }
                    else
                    {
                        addedBy = "Unknown";
                    }
                    string addedByUsername = "Unknown";
                    
                    if (jsonElement.TryGetProperty("addedByUsername", out var usernameElement))
                    {
                        addedByUsername = usernameElement.GetString();
                    }
                    
                    bool isExistingSkin = false;
                    if (jsonElement.TryGetProperty("isExistingSkin", out var existingSkinElement))
                    {
                        isExistingSkin = existingSkinElement.GetBoolean();
                    }
                    
                    if (jsonElement.TryGetProperty("availablePeers", out var peersElement) && peersElement.GetArrayLength() > 0)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        });
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        });
                    }
                    
                    SkinAdded?.Invoke(addedBy, addedByUsername, skinData, isExistingSkin);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_removed", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = null;
                    
                    if (jsonElement.TryGetProperty("skinId", out var skinIdElement))
                    {
                        skinId = skinIdElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    SkinRemoved?.Invoke(skinId);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_updated", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string reason = null;
                    
                    if (jsonElement.TryGetProperty("reason", out var reasonElement))
                    {
                        reason = reasonElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyUpdated?.Invoke(reason);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_created", response =>
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    var jsonElement = response.GetValue<JsonElement>();
                    var lobby = jsonElement.GetProperty("lobby");
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyCreated?.Invoke(lobby);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_joined", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    var lobby = jsonElement.GetProperty("lobby");
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyJoined?.Invoke(lobby);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_left", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string lobbyCode = null;
                    
                    if (jsonElement.TryGetProperty("lobbyCode", out var lobbyCodeElement))
                    {
                        lobbyCode = lobbyCodeElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyLeft?.Invoke(lobbyCode);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("exit_lobby", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string reason = "Lobbiden çıkartıldınız";
                    string? lobbyCode = null;
                    string kickedBy = "0";
                    
                    if (jsonElement.TryGetProperty("reason", out var reasonElement))
                    {
                        reason = reasonElement.GetString() ?? reason;
                    }
                    
                    if (jsonElement.TryGetProperty("lobbyCode", out var lobbyCodeElement))
                    {
                        lobbyCode = lobbyCodeElement.GetString();
                    }
                    
                    if (jsonElement.TryGetProperty("kickedBy", out var kickedByElement))
                    {
                        if (kickedByElement.ValueKind == JsonValueKind.String)
                        {
                            kickedBy = kickedByElement.GetString() ?? "0";
                        }
                        else if (kickedByElement.ValueKind == JsonValueKind.Number)
                        {
                            kickedBy = kickedByElement.GetInt32().ToString();
                        }
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    LobbyDisbanded?.Invoke(reason);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_disbanded", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string reason = "Lobby disbanded";
                    
                    if (jsonElement.TryGetProperty("reason", out var reasonElement))
                    {
                        reason = reasonElement.GetString() ?? reason;
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyDisbanded?.Invoke(reason);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("user_left_lobby", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    int userId = 0;
                    dynamic lobby = null;
                    
                    if (jsonElement.TryGetProperty("userId", out var userIdElement))
                    {
                        userId = userIdElement.GetInt32();
                    }
                    
                    if (jsonElement.TryGetProperty("lobby", out var lobbyElement))
                    {
                        var lobbyJson = lobbyElement.GetRawText();
                        lobby = Newtonsoft.Json.JsonConvert.DeserializeObject(lobbyJson);
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    UserLeftLobby?.Invoke(userId, lobby);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_create_error", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string error = null;
                    
                    if (jsonElement.TryGetProperty("error", out var errorElement))
                    {
                        error = errorElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyError?.Invoke(error);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_join_error", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string error = null;
                    
                    if (jsonElement.TryGetProperty("error", out var errorElement))
                    {
                        error = errorElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyError?.Invoke(error);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("joined", response =>
            {
                try
                {
                    var data = response.GetValue<dynamic>();
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    Task.Run(async () => {
                        try
                        {
                            var availableSkins = GetAvailableCustomSkins();
                            if (availableSkins.Count > 0)
                            {
                                await AnnounceSkinAvailabilityAsync(availableSkins);
                            }
                        }
                        catch (Exception announceEx)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("lobby_members_updated", response =>
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    var jsonElement = response.GetValue<JsonElement>();
                    var lobby = jsonElement.GetProperty("lobby");
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    LobbyMembersUpdated?.Invoke(lobby);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_add_success", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    var skinData = jsonElement.GetProperty("skinData");
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            string skinName = skinData.GetProperty("skin_name").GetString();
                            bool isCustom = skinData.TryGetProperty("is_custom", out var customProp) && customProp.GetBoolean();
                            bool isBuilded = skinData.TryGetProperty("is_builded", out var buildedProp) && buildedProp.GetBoolean();

if (!string.IsNullOrEmpty(skinName) && (isCustom || isBuilded))
                            {
                                string lobbyCode = await GetCurrentLobbyCodeAsync();
                                string userId = _currentUserId;

await UploadSkinToServerAsync(skinName, userId, lobbyCode);
                            }
                        }
                        catch (Exception uploadEx)
                        {
                        }
                    }));
                    
                    SkinAddSuccess?.Invoke(skinData);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_add_error", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string error = null;
                    
                    if (jsonElement.TryGetProperty("error", out var errorElement))
                    {
                        error = errorElement.GetString();
                    }
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    SkinAddError?.Invoke(error);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.OnAny((eventName, response) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            });

            _socket.On("friend_removal_success", response =>
            {
                try
                {
                    var data = response.GetValue<dynamic>();
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("friend_removal_error", response =>
            {
                try
                {
                    var data = response.GetValue<dynamic>();
                    string error = data.error?.ToString();
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("p2p_skin_request", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = jsonElement.GetProperty("skinId").GetString();
                    string requesterId = jsonElement.GetProperty("requesterId").GetString();
                    string requesterName = jsonElement.GetProperty("requesterName").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    P2PSkinRequest?.Invoke(skinId, requesterId, requesterName);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_request_sent", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = jsonElement.GetProperty("skinId").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    SkinRequestSent?.Invoke(skinId);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_request_error", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string error = jsonElement.GetProperty("error").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    SkinRequestError?.Invoke(error);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_announcement_success", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    int count = jsonElement.GetProperty("count").GetInt32();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    SkinAnnouncementSuccess?.Invoke(count);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("p2p_connection_offer", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = jsonElement.GetProperty("skinId").GetString();
                    string peerId = jsonElement.GetProperty("peerId").GetString();
                    string peerName = jsonElement.GetProperty("peerName").GetString();
                    bool isInitiator = jsonElement.GetProperty("isInitiator").GetBoolean();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    P2PConnectionOffer?.Invoke(skinId, peerId, peerName, isInitiator);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("p2p_signaling", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string fromUserId = jsonElement.GetProperty("fromUserId").GetString();
                    var signalData = jsonElement.GetProperty("signalData");
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    P2PSignaling?.Invoke(fromUserId, signalData);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_available_for_download", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = jsonElement.GetProperty("skinId").GetString();
                    string uploadedBy = jsonElement.GetProperty("uploadedBy").GetString();
                    long fileSize = jsonElement.GetProperty("fileSize").GetInt64();
                    string originalName = jsonElement.GetProperty("originalName").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        
                        string skinName = skinId;
                        if (originalName != null && !string.IsNullOrEmpty(originalName))
                        {
                            skinName = originalName;
                            if (skinName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                skinName = skinName.Substring(0, skinName.Length - 4);
                            }
                        }
                        
                        if (uploadedBy != _currentUserId)
                        {
                            QueueSkinForDownload(skinName);
                        }
                    });
                    
                    SkinAvailableForDownload?.Invoke(skinId, uploadedBy, fileSize, originalName);
                    
                    if (uploadedBy != _currentUserId)
                    {
                        Task.Run(async () => {
                            try
                            {
                                string skinName = originalName;
                                if (skinName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                {
                                    skinName = skinName.Substring(0, skinName.Length - 4);
                                }
                                if (skinName.Contains("_202"))
                                {
                                    int timestampIndex = skinName.LastIndexOf("_202");
                                    if (timestampIndex > 0)
                                    {
                                        skinName = skinName.Substring(0, timestampIndex);
                                    }
                                }
                                
                                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                                string skinsDirectory = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
                                string extractPath = Path.Combine(skinsDirectory, skinName);
                                
                                if (Directory.Exists(extractPath))
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    });
                                    return;
                                }
                                
                                string installedJsonPath = Path.Combine(skinsDirectory, "installed.json");
                                if (File.Exists(installedJsonPath) && await IsSkinAlreadyInstalledAsync(installedJsonPath, skinName))
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    });
                                    return;
                                }
                                
                                string tempDownloadPath = Path.Combine(Path.GetTempPath(), "WrightSkins_Download", $"{skinId}_{originalName}");
                                
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    
                                    QueueSkinForDownload(skinName);
                                });
                                
                                await Task.Delay(500);
                                
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    StartSkinDownload(skinName);
                                });
                                
                                bool success = await DownloadSkinFromServerAsync(skinId, _currentUserId, tempDownloadPath);
                                
                                if (success)
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                    });
                                    
                                    Directory.CreateDirectory(extractPath);
                                    
                                    ZipFile.ExtractToDirectory(tempDownloadPath, extractPath);
                                    
                                    await SetDirectoryAttributesAsync(extractPath);
                                    
                                    await AddToInstalledJsonAsync(installedJsonPath, skinName, skinId, fileSize);
                                    
                                    File.Delete(tempDownloadPath);
                                    
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                        
                                        CompleteSkinDownload(skinName);
                                        
                                        AutoSelectDownloadedSkin(skinName);
                                        
                                        TriggerNewModsInLobbyDetected();
                                    });
                                }
                                else
                                {
                                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                        
                                        var lobbySkin = FindLobbySkinByName(skinName);
                                        if (lobbySkin != null)
                                        {
                                            lobbySkin.ResetDownloadStatus();
                                        }
                                    });
                                }
                            }
                            catch (Exception downloadEx)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                                });
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("skin_cleanup_completed", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    string skinId = jsonElement.GetProperty("skinId").GetString();
                    string reason = jsonElement.GetProperty("reason").GetString();
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    SkinCleanupCompleted?.Invoke(skinId, reason);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });

            _socket.On("update_skin_upload_ui", response =>
            {
                try
                {
                    var jsonElement = response.GetValue<JsonElement>();
                    bool showAddButton = jsonElement.GetProperty("showAddButton").GetBoolean();
                    bool isCreator = jsonElement.GetProperty("isCreator").GetBoolean();
                    bool everyoneCanUpload = jsonElement.GetProperty("everyoneCanUpload").GetBoolean();
                    bool hasSpecificPermission = jsonElement.GetProperty("hasSpecificPermission").GetBoolean();
                    string reason = jsonElement.GetProperty("reason").GetString() ?? "unknown";
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    
                    SkinUploadUIUpdate?.Invoke(showAddButton, isCreator, everyoneCanUpload, hasSpecificPermission, reason);
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            });
        }

        public async Task<bool> ConnectAsync(string userId, string username)
        {
            try
            {
                _currentUserId = userId;
                _currentUsername = username;

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.ConnectAsync();
                
                await Task.Delay(1000);
                
                return _isConnected;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        private async Task JoinUserChannel()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                await _socket.EmitAsync("join_user_channel", _currentUserId);
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        public async Task<bool> RemoveFriendAsync(string userId, string friendId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("remove_friend", new { userId, friendId });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_isConnected)
                {
                    await _socket.DisconnectAsync();
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        public async Task<bool> JoinLobbyAsync(string userId, string lobbyId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("join_lobby", new { userId, lobbyId });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> LeaveLobbyAsync(string userId, string lobbyId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("leave_lobby", new { userId });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> DisbandLobbyAsync(string userId, string lobbyCode)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("disband_lobby", new { userId, lobbyCode });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> CreateLobbyAsync(string userId, string username)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("create_lobby", new { userId, username });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> JoinLobbyByCodeAsync(string userId, string username, string lobbyCode)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("join_lobby_by_code", new { userId, username, lobbyCode });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> AddSkinToLobbyAsync(string userId, dynamic skinData)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("add_skin_to_lobby", new { userId, skinData });
                
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> RemoveSkinFromLobbyAsync(int userId, string skinId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("remove_skin_from_lobby", new { userId, skinId });
                
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> UpdateLobbySettings(Views.LobbySettings settings)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("update_lobby_settings", new { 
                    userId = _currentUserId,
                    lobbyCode = settings.LobbyCode,
                    everyoneCanUpload = settings.EveryoneCanUpload,
                    uploadPermissions = settings.UploadPermissions
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> KickLobbyMemberAsync(string lobbyCode, int userId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("kick_lobby_member", new { 
                    hostUserId = _currentUserId,
                    lobbyCode = lobbyCode,
                    targetUserId = userId
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> AnnounceSkinAvailabilityAsync(List<string> skinIds)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("announce_skin_availability", new { userId = _currentUserId, skinIds });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> RequestSkinFromPeerAsync(string skinId, string peerId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("request_skin_from_peer", new { 
                    requesterId = _currentUserId, 
                    skinId, 
                    peerId 
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> AcceptSkinRequestAsync(string skinId, string requesterId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("accept_skin_request", new { 
                    skinId, 
                    requesterId, 
                    peerId = _currentUserId 
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> SendP2PSignalingAsync(string targetUserId, dynamic signalData)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                await _socket.EmitAsync("p2p_signaling", new { targetUserId, signalData });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> NotifySkinTransferCompletedAsync(string skinId, string receiverId)
        {
            try
            {
                if (!_isConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                await _socket.EmitAsync("skin_transfer_completed", new { skinId, receiverId });
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task SendLobbyInviteAsync(int targetUserId, string lobbyCode, string senderUsername)
        {
            try
            {
                if (!this.IsConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return;
                }

                await _socket.EmitAsync("send_lobby_invite", new { 
                    senderId = _currentUserId, 
                    targetUserId = targetUserId.ToString(),
                    lobbyCode = lobbyCode,
                    senderUsername = senderUsername
                });

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        public async Task AcceptLobbyInviteAsync(string lobbyCode, string inviterId, string username)
        {
            try
            {
                if (!this.IsConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return;
                }

                await _socket.EmitAsync("accept_lobby_invite", new { 
                    userId = _currentUserId, 
                    username = username,
                    lobbyCode = lobbyCode,
                    inviterId = inviterId
                });

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        public async Task DeclineLobbyInviteAsync(string lobbyCode, string inviterId)
        {
            try
            {
                if (!this.IsConnected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return;
                }

                await _socket.EmitAsync("decline_lobby_invite", new { 
                    userId = _currentUserId, 
                    lobbyCode = lobbyCode,
                    inviterId = inviterId
                });

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        private List<string> GetAvailableCustomSkins()
        {
            try
            {
                var customSkins = new List<string>();

System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                return customSkins;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return new List<string>();
            }
        }

        public async Task<bool> UploadSkinToServerAsync(string skinId, string userId, string lobbyCode)
        {
            try
            {
                string skinFilePath = GetSkinFilePath(skinId);
                
                if (string.IsNullOrEmpty(skinFilePath) || !File.Exists(skinFilePath))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }

                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(skinId), "skinId");
                    content.Add(new StringContent(userId), "userId");
                    content.Add(new StringContent(lobbyCode ?? ""), "lobbyCode");
                    
                    var fileBytes = await File.ReadAllBytesAsync(skinFilePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    content.Add(fileContent, "skinFile", Path.GetFileName(skinFilePath));

                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });

                    var response = await _httpClient.PostAsync($"{_serverBaseUrl}/upload-skin", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        });
                        return true;
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        });
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> DownloadSkinFromServerAsync(string skinId, string userId, string downloadPath)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });

                var response = await _httpClient.GetAsync($"{_serverBaseUrl}/download-skin/{skinId}?userId={userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    
                    var downloadDir = Path.GetDirectoryName(downloadPath);
                    if (!Directory.Exists(downloadDir))
                    {
                        Directory.CreateDirectory(downloadDir);
                    }
                    
                    await File.WriteAllBytesAsync(downloadPath, fileBytes);

                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });

                    await CompleteSkinDownloadAsync(skinId, userId);
                    
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        public async Task<bool> CompleteSkinDownloadAsync(string skinId, string userId)
        {
            try
            {
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { skinId, userId }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_serverBaseUrl}/complete-skin-download", content);
                
                if (response.IsSuccessStatusCode)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return true;
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        private string GetSkinDirectoryPath(string skinName)
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string skinDirectory = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR", skinName);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                if (!Directory.Exists(skinDirectory))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return null;
                }
                
                string metaPath = Path.Combine(skinDirectory, "META");
                string wadPath = Path.Combine(skinDirectory, "WAD");
                
                if (Directory.Exists(metaPath) && Directory.Exists(wadPath))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return skinDirectory;
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return skinDirectory;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return null;
            }
        }

        private string GetSkinFilePath(string skinName)
        {
            try
            {
                string skinDirectory = GetSkinDirectoryPath(skinName);
                if (string.IsNullOrEmpty(skinDirectory))
                    return null;

                string tempPath = Path.GetTempPath();
                string zipFileName = $"{skinName}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                string zipFilePath = Path.Combine(tempPath, "WrightSkins_Upload", zipFileName);
                
                Directory.CreateDirectory(Path.GetDirectoryName(zipFilePath));
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                System.IO.Compression.ZipFile.CreateFromDirectory(skinDirectory, zipFilePath);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                
                return zipFilePath;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return null;
            }
        }

        private async Task<string> GetCurrentLobbyCodeAsync()
        {
            string lobbyCode = GetCurrentLobbyCode?.Invoke() ?? "WRIGHT-UNKNOWN";
            
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
            });
            
            return lobbyCode;
        }

        private async Task<bool> IsSkinAlreadyInstalledAsync(string installedJsonPath, string skinName)
        {
            try
            {
                string jsonContent = await File.ReadAllTextAsync(installedJsonPath);
                var installedSkins = System.Text.Json.JsonSerializer.Deserialize<List<InstalledSkinEntry>>(jsonContent);
                
                return installedSkins?.Any(s => s.name.Equals(skinName, StringComparison.OrdinalIgnoreCase)) == true;
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
                return false;
            }
        }

        private async Task SetDirectoryAttributesAsync(string directoryPath)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(directoryPath);
                directoryInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System;
                
                await Task.Run(() => {
                    SetAttributesRecursive(directoryPath);
                });
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        private void SetAttributesRecursive(string path)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                dirInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System;
                
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    fileInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System;
                }
                
                foreach (var subDir in Directory.GetDirectories(path))
                {
                    SetAttributesRecursive(subDir);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        private async Task AddToInstalledJsonAsync(string installedJsonPath, string skinName, string skinId, long fileSize)
        {
            try
            {
                List<InstalledSkinEntry> installedSkins = new List<InstalledSkinEntry>();
                
                if (File.Exists(installedJsonPath))
                {
                    string existingContent = await File.ReadAllTextAsync(installedJsonPath);
                    installedSkins = System.Text.Json.JsonSerializer.Deserialize<List<InstalledSkinEntry>>(existingContent) ?? new List<InstalledSkinEntry>();
                }
                
                if (installedSkins.Any(s => s.name.Equals(skinName, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    });
                    return;
                }
                
                var lobbySkin = FindLobbySkinByName(skinName);
                
                var newEntry = new InstalledSkinEntry
                {
                    id = int.TryParse(skinId, out int parsedId) ? parsedId : skinName.GetHashCode(),
                    name = skinName,
                    author = lobbySkin?.UploadedByUsername ?? "Lobby Share",
                    version = lobbySkin?.Version ?? "1.0",
                    uptodate = true,
                    installDate = DateTime.Now,
                    champion = lobbySkin?.ChampionName ?? "",
                    wadFile = "",
                    imageCard = lobbySkin?.ImageCard ?? "",
                    isChampion = !string.IsNullOrEmpty(lobbySkin?.ChampionName),
                    isBuilded = lobbySkin?.IsBuilded ?? false,
                    isCustom = lobbySkin?.IsCustom ?? true,
                    IsSelected = false,
                    fromLobby = true,
                    fileSizeMB = Math.Round(fileSize / 1024.0 / 1024.0, 1)
                };
                
                installedSkins.Add(newEntry);
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true
                };
                
                string jsonContent = System.Text.Json.JsonSerializer.Serialize(installedSkins, options);
                await File.WriteAllTextAsync(installedJsonPath, jsonContent);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                });
            }
        }

        private void AutoSelectDownloadedSkin(string skinName)
        {
            try
            {
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    try
                    {
                        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
                        {
                            
                            if (mainWindow.DataContext is ViewModels.MainViewModel viewModel)
                            {
                                
                                if (viewModel.CurrentLobby?.LobbySkins != null)
                                {
                                    
                                    foreach (var skin in viewModel.CurrentLobby.LobbySkins)
                                    {
                                    }
                                    
                                    var lobbySkin = viewModel.CurrentLobby.LobbySkins.FirstOrDefault(s => s.SkinName == skinName);
                                    if (lobbySkin != null)
                                    {
                                        lobbySkin.IsSelected = true;
                                        
                                        mainWindow.UpdateSelectedSkinsCount();
                                        
                                        _ = viewModel.UpdateWrightProfileAsync();
                                        
                                        TriggerNewModsInLobbyDetected();
                                        
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
                        else
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }

        public void TriggerNewModsInLobbyDetected()
        {
            NewModsInLobbyDetected?.Invoke();
        }

        private void RemoveSkinFromWrightProfile(string skinName)
        {
            try
            {
                var wrightProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riot Games", "League of Legends", "WrightSkins", "ALR", "Wright.profile");
                
                if (File.Exists(wrightProfilePath))
                {
                    RemoveSkinFromProfileFile(wrightProfilePath, skinName);
                }
                else
                {
                }

            }
            catch (Exception ex)
            {
            }
        }

        private void RemoveSkinFromProfileFile(string profilePath, string skinName)
        {
            try
            {
                if (!File.Exists(profilePath))
                    return;

                var lines = File.ReadAllLines(profilePath).ToList();
                bool removed = false;
                
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (!string.IsNullOrEmpty(lines[i]) && lines[i].Trim().Equals(skinName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        string removedLine = lines[i];
                        lines.RemoveAt(i);
                        removed = true;
                    }
                }
                
                if (removed)
                {
                    File.WriteAllLines(profilePath, lines);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ForceSkinDeselect(string skinId, string skinName)
        {
            try
            {

}
            catch (Exception ex)
            {
            }
        }

        private void ClearSkinSelection(string skinId, string skinName)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
            }
        }

        private async Task ProcessExistingSkinFileData(string skinName, string fileData, long fileSize, string originalName)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    UpdateSkinDownloadProgress(skinName, 20);
                });

                byte[] fileBytes = Convert.FromBase64String(fileData);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    UpdateSkinDownloadProgress(skinName, 40);
                });
                
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string skinsDirectory = Path.Combine(localAppData, "Riot Games", "League of Legends", "WrightSkins", "ALR");
                
                if (!Directory.Exists(skinsDirectory))
                {
                    Directory.CreateDirectory(skinsDirectory);
                }
                
                string tempFilePath = Path.Combine(skinsDirectory, $"temp_{originalName}");
                await File.WriteAllBytesAsync(tempFilePath, fileBytes);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    UpdateSkinDownloadProgress(skinName, 60);
                });
                
                string extractPath = Path.Combine(skinsDirectory, skinName);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    UpdateSkinDownloadProgress(skinName, 80);
                });
                
                using (var archive = ZipFile.OpenRead(tempFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string fullPath = Path.Combine(extractPath, entry.FullName);
                        string? directoryPath = Path.GetDirectoryName(fullPath);
                        
                        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                        
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(fullPath, overwrite: true);
                        }
                    }
                }
                
                File.Delete(tempFilePath);
                
                string installedJsonPath = Path.Combine(skinsDirectory, "installed.json");
                await AddToInstalledJsonAsync(installedJsonPath, skinName, skinName.GetHashCode().ToString(), fileSize);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    
                    CompleteSkinDownload(skinName);
                    
                    ExistingSkinFileReceived?.Invoke(skinName, true);
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    
                    CompleteSkinDownload(skinName);
                });
            }
        }

        private void UpdateSkinDownloadProgress(string skinName, int percentage)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    try
                    {
                        var lobbySkin = FindLobbySkinByName(skinName);
                        if (lobbySkin != null)
                        {
                            lobbySkin.UpdateDownloadProgress(percentage);
                        }
                    }
                    catch (Exception dispatcherEx)
                    {
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }
        
        private void CompleteSkinDownload(string skinName)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    try
                    {
                        var lobbySkin = FindLobbySkinByName(skinName);
                        if (lobbySkin != null)
                        {
                            lobbySkin.CompleteDownload();
                        }
                    }
                    catch (Exception dispatcherEx)
                    {
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }
        
        private void ResetSkinDownloadStatus(string skinName)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    try
                    {
                        var lobbySkin = FindLobbySkinByName(skinName);
                        if (lobbySkin != null)
                        {
                            lobbySkin.ResetDownloadStatus();
                        }
                    }
                    catch (Exception dispatcherEx)
                    {
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }
        
        private void QueueSkinForDownload(string skinName)
        {
            try
            {
                var lobbySkin = FindLobbySkinByName(skinName);
                if (lobbySkin != null)
                {
                    lobbySkin.QueueForDownload();
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        private void StartSkinDownload(string skinName)
        {
            try
            {
                var lobbySkin = FindLobbySkinByName(skinName);
                if (lobbySkin != null)
                {
                    lobbySkin.StartDownload();
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        private Models.LobbySkin? FindLobbySkinByName(string skinName)
        {
            try
            {
                Models.LobbySkin? result = null;
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    try
                    {
                        var mainWindow = System.Windows.Application.Current?.MainWindow as MainWindow;
                        if (mainWindow?.DataContext is ViewModels.MainViewModel viewModel &&
                            viewModel.CurrentLobby?.LobbySkins != null)
                        {
                            result = viewModel.CurrentLobby.LobbySkins
                                .FirstOrDefault(s => s.SkinName.Equals(skinName, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                    catch (Exception dispatcherEx)
                    {
                    }
                });
                
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _socket?.Dispose();
        }
    }

    public class InstalledSkinEntry
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string author { get; set; } = string.Empty;
        public string version { get; set; } = string.Empty;
        public bool uptodate { get; set; } = true;
        public DateTime installDate { get; set; }
        public string champion { get; set; } = string.Empty;
        public string wadFile { get; set; } = string.Empty;
        public string imageCard { get; set; } = string.Empty;
        public bool isChampion { get; set; }
        public bool isBuilded { get; set; }
        public bool isCustom { get; set; }
        public bool IsSelected { get; set; }
        public bool fromLobby { get; set; }
        public double fileSizeMB { get; set; }
    }
}


