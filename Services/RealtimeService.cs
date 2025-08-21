using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using WrightLauncher.Utilities;

namespace WrightLauncher.Services
{
    public class RealtimeService : IDisposable
    {
        private static readonly string API_BASE_URL = WrightUtils.F;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _pollingCancellation;
        private Task _pollingTask;
        
        private string _currentLobbyCode;
        private string _currentUserId;
        private string _currentUsername;
        private string _lastMessageId;
        private bool _isActive = false;
        private readonly long _startupTimestamp;

        public event Action<string, string> UserJoined;
        public event Action<string, string> UserLeft;
        public event Action<string, string, RealtimeSkinData> SkinAdded;
        public event Action<string, string, string, string> FileRequest;
        public event Action<string, string, string, List<RealtimeFileData>, string> FilesReceived;
        
        public event Action<string, string, bool> FriendStatusChanged;
        public event Action<string, string, string> FriendAdded;
        public event Action<string> FriendRemoved;
        public event Action<string, string> FriendsListUpdateRequired;

        public RealtimeService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(35);
            _startupTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public async Task<bool> JoinLobbyAsync(string lobbyCode, string userId, string username)
        {
            try
            {

                var data = new Dictionary<string, string>
                {
                    ["action"] = "join",
                    ["lobbyCode"] = lobbyCode,
                    ["userId"] = userId,
                    ["username"] = username
                };

                var response = await PostAsync(data);
                if (response.success)
                {
                    _currentLobbyCode = lobbyCode;
                    _currentUserId = userId;
                    _currentUsername = username;
                    _isActive = true;

                    StartPolling();
                    
                    StartHeartbeat();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> JoinUserChannelAsync(string userId, string username)
        {
            try
            {

                _currentUserId = userId;
                _currentUsername = username;
                _isActive = true;

                _currentLobbyCode = $"user_{userId}";
                StartPolling();
                StartHeartbeat();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> AddSkinAsync(RealtimeSkinData skinData)
        {
            try
            {
                if (!_isActive || string.IsNullOrEmpty(_currentLobbyCode))
                {
                    return false;
                }

                var data = new Dictionary<string, string>
                {
                    ["action"] = "add-skin",
                    ["lobbyCode"] = _currentLobbyCode,
                    ["userId"] = _currentUserId,
                    ["username"] = _currentUsername,
                    ["skinData"] = JsonConvert.SerializeObject(skinData)
                };

                var response = await PostAsync(data);
                if (response.success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> RequestSkinAsync(string toUserId, string skinName)
        {
            try
            {
                if (!_isActive)
                {
                    return false;
                }

                var data = new Dictionary<string, string>
                {
                    ["action"] = "request-skin",
                    ["lobbyCode"] = _currentLobbyCode,
                    ["fromUserId"] = _currentUserId,
                    ["fromUsername"] = _currentUsername,
                    ["toUserId"] = toUserId,
                    ["skinName"] = skinName
                };

                var response = await PostAsync(data);
                if (response.success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> SendFilesAsync(string toUserId, string skinName, List<RealtimeFileData> files, string requestId)
        {
            try
            {
                if (!_isActive)
                {
                    return false;
                }

                var data = new Dictionary<string, string>
                {
                    ["action"] = "send-files",
                    ["lobbyCode"] = _currentLobbyCode,
                    ["fromUserId"] = _currentUserId,
                    ["fromUsername"] = _currentUsername,
                    ["toUserId"] = toUserId,
                    ["skinName"] = skinName,
                    ["files"] = JsonConvert.SerializeObject(files),
                    ["requestId"] = requestId
                };

                var response = await PostAsync(data);
                if (response.success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task LeaveLobbyAsync()
        {
            try
            {
                _isActive = false;

                _pollingCancellation?.Cancel();

                if (!string.IsNullOrEmpty(_currentLobbyCode))
                {
                    var data = new Dictionary<string, string>
                    {
                        ["action"] = "leave",
                        ["lobbyCode"] = _currentLobbyCode,
                        ["userId"] = _currentUserId,
                        ["username"] = _currentUsername
                    };

                    await PostAsync(data);
                }

                _currentLobbyCode = null;
                _currentUserId = null;
                _currentUsername = null;
                _lastMessageId = null;
            }
            catch (Exception ex)
            {
            }
        }

        private async Task ClearUserMessages()
        {
            try
            {
                var data = new Dictionary<string, string>
                {
                    ["action"] = "clear-user-messages",
                    ["userId"] = _currentUserId
                };
                
                var content = new FormUrlEncodedContent(data);
                var response = await WrightSkinsApiService.PostAsync(API_BASE_URL, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _lastMessageId = null;
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void StartPolling()
        {
            _pollingCancellation = new CancellationTokenSource();
            _pollingTask = Task.Run(async () =>
            {
                await ClearUserMessages();
                
                while (_isActive && !_pollingCancellation.Token.IsCancellationRequested)
                {
                    try
                    {
                        await PollForMessages();
                        await Task.Delay(1000, _pollingCancellation.Token);
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(5000, _pollingCancellation.Token);
                    }
                }
            }, _pollingCancellation.Token);
        }

        private async Task PollForMessages()
        {
            try
            {
                var url = $"{API_BASE_URL}?action=poll&lobbyCode={Uri.EscapeDataString(_currentLobbyCode)}&userId={Uri.EscapeDataString(_currentUserId)}";
                if (!string.IsNullOrEmpty(_lastMessageId))
                {
                    url += $"&lastMessageId={Uri.EscapeDataString(_lastMessageId)}";
                }

                var response = await WrightSkinsApiService.GetStringAsync(url);
                var result = JsonConvert.DeserializeObject<RealtimePollResponse>(response);

                if (result.success && result.messages != null)
                {
                    if (result.messages.Count > 0)
                    {
                    }
                    
                    foreach (var message in result.messages)
                    {
                        ProcessMessage(message);
                        _lastMessageId = message.id;
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
            }
        }

        private void ProcessMessage(RealtimeMessage message)
        {
            try
            {
                if (message.timestamp < _startupTimestamp)
                {
                    return;
                }
                
                switch (message.type)
                {
                    case "user-joined":
                        var joinData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        UserJoined?.Invoke(joinData.userId?.ToString(), joinData.username?.ToString());
                        break;

                    case "user-left":
                        var leftData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        UserLeft?.Invoke(leftData.userId?.ToString(), leftData.username?.ToString());
                        break;

                    case "skin-added":
                        var skinData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        var skin = JsonConvert.DeserializeObject<RealtimeSkinData>(skinData.skinData?.ToString() ?? "{}");
                        SkinAdded?.Invoke(skinData.userId?.ToString(), skinData.username?.ToString(), skin);
                        break;

                    case "file-request":
                        var requestData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        if (requestData.toUserId?.ToString() == _currentUserId)
                        {
                            FileRequest?.Invoke(requestData.fromUserId?.ToString(), requestData.fromUsername?.ToString(), 
                                              requestData.skinName?.ToString(), requestData.requestId?.ToString());
                        }
                        break;

                    case "files-sent":
                        var filesData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        if (filesData.toUserId?.ToString() == _currentUserId)
                        {
                            var files = JsonConvert.DeserializeObject<List<RealtimeFileData>>(filesData.files?.ToString() ?? "[]");
                            FilesReceived?.Invoke(filesData.fromUserId?.ToString(), filesData.fromUsername?.ToString(),
                                                filesData.skinName?.ToString(), files, filesData.requestId?.ToString());
                        }
                        break;

                    case "friend-status-changed":
                        var friendStatusData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        FriendStatusChanged?.Invoke(friendStatusData.friendId?.ToString(), friendStatusData.friendUsername?.ToString(), 
                                                  (bool)(friendStatusData.isOnline ?? false));
                        break;

                    case "friend-added":
                        var friendAddData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        FriendAdded?.Invoke(friendAddData.friendId?.ToString(), friendAddData.friendUsername?.ToString(),
                                          friendAddData.friendAvatarUrl?.ToString());
                        break;

                    case "friend-removed":
                        var friendRemoveData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        FriendRemoved?.Invoke(friendRemoveData.friendId?.ToString());
                        break;

                    case "friends-list-update-required":
                        var updateData = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(message.data));
                        FriendsListUpdateRequired?.Invoke(updateData.reason?.ToString(), updateData.friendId?.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void StartHeartbeat()
        {
            Task.Run(async () =>
            {
                while (_isActive)
                {
                    try
                    {
                        var data = new Dictionary<string, string>
                        {
                            ["action"] = "heartbeat",
                            ["lobbyCode"] = _currentLobbyCode,
                            ["userId"] = _currentUserId
                        };

                        await PostAsync(data);
                        await Task.Delay(30000);
                    }
                    catch (Exception ex)
                    {
                        await Task.Delay(5000);
                    }
                }
            });
        }

        private async Task<RealtimeResponse> PostAsync(Dictionary<string, string> data)
        {
            try
            {
                var content = new FormUrlEncodedContent(data);
                var response = await WrightSkinsApiService.PostAsync(API_BASE_URL, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                return JsonConvert.DeserializeObject<RealtimeResponse>(responseString);
            }
            catch (Exception ex)
            {
                return new RealtimeResponse { success = false, message = ex.Message };
            }
        }

        public void Dispose()
        {
            _isActive = false;
            _pollingCancellation?.Cancel();
            _pollingTask?.Wait(5000);
            _httpClient?.Dispose();
        }
    }

    public class RealtimeResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public dynamic lobby { get; set; }
    }

    public class RealtimePollResponse : RealtimeResponse
    {
        public List<RealtimeMessage> messages { get; set; }
    }

    public class RealtimeMessage
    {
        public string id { get; set; }
        public string type { get; set; }
        public dynamic data { get; set; }
        public long timestamp { get; set; }
    }

    public class RealtimeSkinData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Champion { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsBuilded { get; set; }
        public bool IsCustom { get; set; }
        public bool IsChroma { get; set; }
        public string ImageCard { get; set; } = "";
    }

    public class RealtimeFileData
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
        public string Content { get; set; } = "";
        public string Hash { get; set; } = "";
    }
}

