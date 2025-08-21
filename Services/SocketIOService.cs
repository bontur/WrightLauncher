using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SocketIOClient;
using Newtonsoft.Json;
using System.Text;
using WrightLauncher.Utilities;

namespace WrightLauncher.Services
{
    public class SocketIOService : IDisposable
    {
        private SocketIOClient.SocketIO _client;
        private string _currentLobbyCode;
        private int _currentUserId;
        private string _currentUsername;
        private bool _isConnected = false;

        public event Action<string, string> UserJoined;
        public event Action<string, string> UserLeft;
        public event Action<string, string, SkinData> SkinAdded;
        public event Action<string, string, string, int> FileRequest;
        public event Action<string, string, string, List<FileData>, int> FilesReceived;
        
        public event Action<int, string> FriendRequestReceived;
        public event Action<int, string> FriendRequestAccepted;
        public event Action<int, string> FriendRemoved;

        public SocketIOService()
        {
            _client = new SocketIOClient.SocketIO($"{WrightUtils.F}", new SocketIOOptions
            {
                ConnectionTimeout = TimeSpan.FromSeconds(10),
                Reconnection = true,
                ReconnectionAttempts = 5,
                ReconnectionDelay = 2000
            });

            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            _client.OnConnected += async (sender, e) =>
            {
                _isConnected = true;
                
                if (!string.IsNullOrEmpty(_currentLobbyCode))
                {
                    await JoinLobbyAsync(_currentLobbyCode, _currentUserId, _currentUsername);
                }
            };

            _client.OnDisconnected += (sender, e) =>
            {
                _isConnected = false;
            };

            _client.OnError += (sender, e) =>
            {
            };

            _client.On("user-joined", response =>
            {
                var data = response.GetValue<dynamic>();
                string userId = data.userId?.ToString();
                string username = data.username?.ToString();
                
                UserJoined?.Invoke(userId, username);
            });

            _client.On("user-left", response =>
            {
                var data = response.GetValue<dynamic>();
                string userId = data.userId?.ToString();
                string username = data.username?.ToString();
                
                UserLeft?.Invoke(userId, username);
            });

            _client.On("skin-added", response =>
            {
                var data = response.GetValue<dynamic>();
                string userId = data.userId?.ToString();
                string username = data.username?.ToString();
                var skinDataJson = data.skinData?.ToString();
                
                if (!string.IsNullOrEmpty(skinDataJson))
                {
                    try
                    {
                        var skinData = JsonConvert.DeserializeObject<SkinData>(skinDataJson);
                        SkinAdded?.Invoke(userId, username, skinData);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });

            _client.On("file-request", response =>
            {
                var data = response.GetValue<dynamic>();
                string fromUserId = data.fromUserId?.ToString();
                string fromUsername = data.fromUsername?.ToString();
                string skinName = data.skinName?.ToString();
                int requestId = data.requestId != null ? (int)data.requestId : 0;
                
                FileRequest?.Invoke(fromUserId, fromUsername, skinName, requestId);
            });

            _client.On("receive-skin-files", response =>
            {
                var data = response.GetValue<dynamic>();
                string fromUserId = data.fromUserId?.ToString();
                string fromUsername = data.fromUsername?.ToString();
                string skinName = data.skinName?.ToString();
                int requestId = data.requestId != null ? (int)data.requestId : 0;
                var filesJson = data.files?.ToString();
                
                if (!string.IsNullOrEmpty(filesJson))
                {
                    try
                    {
                        var files = JsonConvert.DeserializeObject<List<FileData>>(filesJson);
                        FilesReceived?.Invoke(fromUserId, fromUsername, skinName, files, requestId);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });

            _client.On("lobby-state", response =>
            {
                var data = response.GetValue<dynamic>();
                var users = data.users;
                var skins = data.skins;
                
            });

            _client.On("friend-request-received", response =>
            {
                var data = response.GetValue<dynamic>();
                int fromUserId = data.fromUserId != null ? (int)data.fromUserId : 0;
                string fromUsername = data.fromUsername?.ToString() ?? "Unknown";
                
                FriendRequestReceived?.Invoke(fromUserId, fromUsername);
            });

            _client.On("friend-request-accepted", response =>
            {
                var data = response.GetValue<dynamic>();
                int fromUserId = data.fromUserId != null ? (int)data.fromUserId : 0;
                string fromUsername = data.fromUsername?.ToString() ?? "Unknown";
                
                FriendRequestAccepted?.Invoke(fromUserId, fromUsername);
            });

            _client.On("friend-removed", response =>
            {
                var data = response.GetValue<dynamic>();
                int removedUserId = data.removedUserId != null ? (int)data.removedUserId : 0;
                string removedUsername = data.removedUsername?.ToString() ?? "Unknown";
                
                FriendRemoved?.Invoke(removedUserId, removedUsername);
            });
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (!_isConnected)
                {
                    await _client.ConnectAsync();
                }
                return _isConnected;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task JoinLobbyAsync(string lobbyCode, int userId, string username)
        {
            try
            {
                _currentLobbyCode = lobbyCode;
                _currentUserId = userId;
                _currentUsername = username;

                if (!_isConnected)
                {
                    await ConnectAsync();
                }

                var data = new
                {
                    lobbyCode,
                    userId,
                    username
                };

                await _client.EmitAsync("join-lobby", data);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task NotifySkinSelectedAsync(SkinData skinData)
        {
            try
            {
                if (!_isConnected || string.IsNullOrEmpty(_currentLobbyCode))
                {
                    return;
                }

                var data = new
                {
                    lobbyCode = _currentLobbyCode,
                    userId = _currentUserId,
                    skinData
                };

                await _client.EmitAsync("skin-selected", data);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task RequestSkinFilesAsync(string targetUserId, string skinName)
        {
            try
            {
                if (!_isConnected)
                {
                    return;
                }

                var data = new
                {
                    targetUserId,
                    skinName
                };

                await _client.EmitAsync("request-skin-files", data);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task SendSkinFilesAsync(string toUserId, string skinName, List<FileData> files, int requestId)
        {
            try
            {
                if (!_isConnected)
                {
                    return;
                }

                var data = new
                {
                    toUserId,
                    skinName,
                    files,
                    requestId
                };

                await _client.EmitAsync("send-skin-files", data);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task LeaveLobbyAsync()
        {
            try
            {
                if (_isConnected)
                {
                    await _client.EmitAsync("leave-lobby");
                }
                
                _currentLobbyCode = null;
                _currentUserId = 0;
                _currentUsername = null;
            }
            catch (Exception ex)
            {
            }
        }

        public void Dispose()
        {
            try
            {
                _client?.DisconnectAsync();
                _client?.Dispose();
            }
            catch (Exception ex)
            {
            }
        }
    }

    public class SkinData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Champion { get; set; }
        public string Version { get; set; }
        public bool IsBuilded { get; set; }
        public bool IsCustom { get; set; }
        public bool IsChroma { get; set; }
    }

    public class FileData
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public string Content { get; set; }
        public string Hash { get; set; }
    }
}

