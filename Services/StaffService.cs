using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WrightLauncher.Utilities;

namespace WrightLauncher.Services
{
    public class StaffInfo
    {
        [JsonProperty("discord_id")]
        public string DiscordId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("custom_role_name")]
        public string CustomRoleName { get; set; }

        [JsonProperty("badge_color")]
        public string BadgeColor { get; set; }

        [JsonProperty("badge_icon")]
        public string BadgeIcon { get; set; }

        [JsonProperty("permissions")]
        public List<string> Permissions { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }
    }

    public class StaffCheckResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("is_staff")]
        public bool IsStaff { get; set; }

        [JsonProperty("staff_info")]
        public StaffInfo StaffInfo { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    public class StaffListResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("staff")]
        public List<StaffInfo> Staff { get; set; }

        [JsonProperty("roles")]
        public Dictionary<string, object> Roles { get; set; }

        [JsonProperty("permissions")]
        public Dictionary<string, List<string>> Permissions { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    public class PermissionCheckResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("has_permission")]
        public bool HasPermission { get; set; }

        [JsonProperty("staff_info")]
        public StaffInfo StaffInfo { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    public class AddStaffRequest
    {
        [JsonProperty("discord_id")]
        public string DiscordId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("custom_role_name")]
        public string CustomRoleName { get; set; }

        [JsonProperty("badge_color")]
        public string BadgeColor { get; set; }

        [JsonProperty("badge_icon")]
        public string BadgeIcon { get; set; }

        [JsonProperty("permissions")]
        public List<string> Permissions { get; set; }

        [JsonProperty("admin_discord_id")]
        public string AdminDiscordId { get; set; }
    }

    public static class StaffService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string SOCKET_SERVER_URL = WrightUtils.F;

        public static async Task<StaffCheckResponse> CheckStaffAsync(string discordId)
        {
            try
            {
                var response = await httpClient.GetAsync($"{SOCKET_SERVER_URL}/api/staff/check/{discordId}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<StaffCheckResponse>(content);
                }
                else
                {
                    return new StaffCheckResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {content}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new StaffCheckResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public static async Task<StaffListResponse> GetStaffListAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"{SOCKET_SERVER_URL}/api/staff/list");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<StaffListResponse>(content);
                }
                else
                {
                    return new StaffListResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {content}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new StaffListResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public static async Task<PermissionCheckResponse> CheckPermissionAsync(string discordId, string permission)
        {
            try
            {
                var requestData = new
                {
                    discord_id = discordId,
                    permission = permission
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{SOCKET_SERVER_URL}/api/staff/check-permission", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<PermissionCheckResponse>(responseContent);
                }
                else
                {
                    return new PermissionCheckResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PermissionCheckResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public static async Task<StaffCheckResponse> AddStaffAsync(AddStaffRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{SOCKET_SERVER_URL}/api/staff/add", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<StaffCheckResponse>(responseContent);
                }
                else
                {
                    return new StaffCheckResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new StaffCheckResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public static async Task<StaffCheckResponse> UpdateStaffAsync(string discordId, AddStaffRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"{SOCKET_SERVER_URL}/api/staff/update/{discordId}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<StaffCheckResponse>(responseContent);
                }
                else
                {
                    return new StaffCheckResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new StaffCheckResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public static async Task<StaffCheckResponse> RemoveStaffAsync(string discordId, string adminDiscordId)
        {
            try
            {
                var requestData = new { admin_discord_id = adminDiscordId };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{SOCKET_SERVER_URL}/api/staff/remove/{discordId}")
                {
                    Content = content
                };

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<StaffCheckResponse>(responseContent);
                }
                else
                {
                    return new StaffCheckResponse
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new StaffCheckResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public static async Task<bool> IsCurrentUserStaffAsync(string currentDiscordId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentDiscordId))
                    return false;

                var result = await CheckStaffAsync(currentDiscordId);
                return result.Success && result.IsStaff;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> CurrentUserHasPermissionAsync(string currentDiscordId, string permission)
        {
            try
            {
                if (string.IsNullOrEmpty(currentDiscordId))
                    return false;

                var result = await CheckPermissionAsync(currentDiscordId, permission);
                return result.Success && result.HasPermission;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<StaffInfo?> GetCurrentUserStaffInfoAsync(string currentDiscordId)
        {
            try
            {
                if (string.IsNullOrEmpty(currentDiscordId))
                    return null;

                var result = await CheckStaffAsync(currentDiscordId);
                return result.Success && result.IsStaff ? result.StaffInfo : null;
            }
            catch
            {
                return null;
            }
        }
    }
}


