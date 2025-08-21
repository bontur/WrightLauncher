using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using WrightLauncher.Models;
using WrightLauncher.Utilities;

namespace WrightLauncher.Services
{
    public class DiscordService
    {
        private static string _appKey => WrightUtils.A;
        private static string _apiSecret => WrightUtils.B;
        private static string _callbackUrl => WrightUtils.C;
        private static string _serviceBase => WrightUtils.F;
        
        private readonly HttpClient _httpClient;
        private string? _accessToken;
        private string? _refreshToken;

        public event EventHandler<DiscordUser>? UserAuthenticated;
        public event EventHandler? AuthenticationFailed;

        public DiscordService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            try
            {
                var savedRefreshToken = await ConfigService.GetDiscordRefreshTokenAsync();
                if (string.IsNullOrEmpty(savedRefreshToken))
                {
                    return false;
                }

                var success = await RefreshAccessTokenAsync(savedRefreshToken);
                if (success)
                {
                    var savedUser = await ConfigService.GetDiscordUserAsync();
                    if (savedUser != null)
                    {
                        UserAuthenticated?.Invoke(this, savedUser);
                        return true;
                    }
                }

                await ConfigService.ClearDiscordInfoAsync();
                return false;
            }
            catch (Exception ex)
            {
                await ConfigService.ClearDiscordInfoAsync();
                return false;
            }
        }

        public void StartAuthentication()
        {
            try
            {
                var scope = "identify guilds";
                var authUrl = $"https://discord.com/oauth2/authorize?" +
                    $"client_id={_appKey}&" +
                    $"redirect_uri={Uri.EscapeDataString(_callbackUrl)}&" +
                    $"response_type=code&" +
                    $"scope={Uri.EscapeDataString(scope)}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                StartLocalServer();
            }
            catch (Exception ex)
            {
                AuthenticationFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void StartLocalServer()
        {
            try
            {
                var listener = new System.Net.HttpListener();
                listener.Prefixes.Add("http://localhost:23014/");
                listener.Start();

                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                var code = request.QueryString["code"];
                
                if (!string.IsNullOrEmpty(code))
                {
                    var successHtml = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Discord Connection Successful - WrightSkins</title>
    <link href=""https:
    <style>
        :root {
            --dashboard-bg: #0a0a0f;
            --dashboard-card: #111118;
            --dashboard-border: #1a1a24;
            --dashboard-accent: #6366f1;
            --dashboard-text: #ffffff;
            --dashboard-text-muted: #9ca3af;
            --dashboard-success: #10b981;
            --dashboard-gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            --dashboard-glow: 0 0 20px rgba(99, 102, 241, 0.3);
            --discord-color: #5865f2;
        }
        
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            background: var(--dashboard-bg);
            color: var(--dashboard-text);
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow-x: hidden;
            position: relative;
        }
        
        body::before {
            content: '';
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: linear-gradient(135deg, var(--dashboard-bg) 0%, #0f0f1a 100%);
            z-index: -2;
        }
        
        body::after {
            content: '';
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: 
                radial-gradient(circle at 20% 80%, rgba(99, 102, 241, 0.1) 0%, transparent 50%),
                radial-gradient(circle at 80% 20%, rgba(118, 75, 162, 0.1) 0%, transparent 50%),
                radial-gradient(circle at 40% 40%, rgba(88, 101, 242, 0.05) 0%, transparent 50%);
            z-index: -1;
        }
        
        .success-container {
            background: var(--dashboard-card);
            border: 1px solid var(--dashboard-border);
            border-radius: 24px;
            padding: 4rem 3rem;
            max-width: 500px;
            width: 90%;
            text-align: center;
            position: relative;
            overflow: hidden;
            backdrop-filter: blur(20px);
            box-shadow: 
                0 25px 50px rgba(0, 0, 0, 0.5),
                0 0 40px rgba(99, 102, 241, 0.1);
            animation: slideInUp 0.8s cubic-bezier(0.4, 0, 0.2, 1);
        }
        
        .success-container::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: linear-gradient(135deg, rgba(99, 102, 241, 0.05) 0%, rgba(118, 75, 162, 0.05) 100%);
            z-index: -1;
        }
        
        @keyframes slideInUp {
            from {
                opacity: 0;
                transform: translateY(40px) scale(0.95);
            }
            to {
                opacity: 1;
                transform: translateY(0) scale(1);
            }
        }
        
        .success-icon {
            width: 80px;
            height: 80px;
            margin: 0 auto 2rem;
            background: linear-gradient(135deg, var(--dashboard-success) 0%, #0d9488 100%);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            animation: bounce 1s ease-out 0.5s both;
        }
        
        .success-icon::before {
            content: '';
            position: absolute;
            top: -4px;
            left: -4px;
            right: -4px;
            bottom: -4px;
            background: linear-gradient(135deg, var(--dashboard-success) 0%, #0d9488 100%);
            border-radius: 50%;
            opacity: 0.3;
            z-index: -1;
            animation: pulse 2s infinite;
        }
        
        @keyframes bounce {
            0% { transform: scale(0.3) rotate(-180deg); opacity: 0; }
            50% { transform: scale(1.1) rotate(-90deg); }
            100% { transform: scale(1) rotate(0deg); opacity: 1; }
        }
        
        @keyframes pulse {
            0%, 100% { transform: scale(1); opacity: 0.3; }
            50% { transform: scale(1.1); opacity: 0.1; }
        }
        
        .checkmark {
            width: 32px;
            height: 32px;
            color: white;
            stroke-width: 3;
        }
        
        .success-title {
            font-size: 2rem;
            font-weight: 800;
            margin-bottom: 1rem;
            background: linear-gradient(135deg, #ffffff 0%, #10b981 50%, #06b6d4 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            animation: fadeIn 0.8s ease-out 0.4s both;
        }
        
        .success-subtitle {
            font-size: 1.1rem;
            color: var(--dashboard-text-muted);
            margin-bottom: 2rem;
            line-height: 1.6;
            animation: fadeIn 0.8s ease-out 0.6s both;
        }
        
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
        
        .btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            padding: 1rem 1.5rem;
            border-radius: 12px;
            font-weight: 600;
            text-decoration: none;
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            border: none;
            cursor: pointer;
            font-size: 0.95rem;
            position: relative;
            overflow: hidden;
            gap: 0.5rem;
            font-family: 'Inter', sans-serif;
        }
        
        .btn-secondary {
            background: transparent;
            color: var(--dashboard-text-muted);
            border: 1px solid var(--dashboard-border);
        }
        
        .btn-secondary:hover {
            background: rgba(255, 255, 255, 0.05);
            color: var(--dashboard-text);
            border-color: var(--dashboard-accent);
            transform: translateY(-1px);
        }
        
        .countdown-container {
            margin-top: 1.5rem;
            padding: 1rem;
            background: rgba(255, 255, 255, 0.03);
            border-radius: 12px;
            border: 1px solid var(--dashboard-border);
            animation: fadeIn 0.8s ease-out 1.2s both;
        }
        
        .countdown-text {
            font-size: 0.85rem;
            color: var(--dashboard-text-muted);
            margin-bottom: 0.5rem;
        }
        
        .countdown-timer {
            font-size: 1.1rem;
            font-weight: 700;
            color: var(--dashboard-accent);
        }
        
        .floating {
            animation: float 3s ease-in-out infinite;
        }
        
        @keyframes float {
            0%, 100% { transform: translateY(0px); }
            50% { transform: translateY(-10px); }
        }
    </style>
</head>
<body>
    <div class=""success-container"">
        <div class=""success-icon floating"">
            <svg class=""checkmark"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M5 13l4 4L19 7""/>
            </svg>
        </div>
        <h1 class=""success-title"">Discord Connection Successful!</h1>
        <p class=""success-subtitle"">Your Discord account has been successfully linked to WrightLauncher. You can now access more features and join with your friends.</p>
        <button class=""btn btn-secondary"" onclick=""closeTab()"">
            <svg width=""16"" height=""16"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M6 18L18 6M6 6l12 12""/>
            </svg>
            Close This Tab
        </button>
        <div class=""countdown-container"">
            <div class=""countdown-text"">This tab will automatically close in:</div>
            <div class=""countdown-timer"" id=""countdown"">5 seconds</div>
        </div>
    </div>
    <script>
        let countdown = 5;
        const countdownElement = document.getElementById('countdown');
        const timer = setInterval(() => {
            countdown--;
            countdownElement.textContent = countdown + ' seconds';
            if (countdown <= 0) {
                clearInterval(timer);
                closeTab();
            }
        }, 1000);
        
        function closeTab() {
            try {
                window.close();
            } catch (e) {
                countdownElement.textContent = 'Please close this tab manually';
                countdownElement.style.color = '#f59e0b';
            }
        }
        
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' || e.key === 'Enter') {
                closeTab();
            }
        });
    </script>
</body>
</html>";
                    
                    var buffer = Encoding.UTF8.GetBytes(successHtml);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=utf-8";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();

                    await ExchangeCodeForToken(code);
                }
                else
                {
                    var errorHtml = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Discord Connection Failed - WrightSkins</title>
    <link href=""https:
    <style>
        :root {
            --dashboard-bg: #0a0a0f;
            --dashboard-card: #111118;
            --dashboard-border: #1a1a24;
            --dashboard-accent: #6366f1;
            --dashboard-text: #ffffff;
            --dashboard-text-muted: #9ca3af;
            --dashboard-danger: #ef4444;
            --dashboard-warning: #f59e0b;
            --dashboard-gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            --dashboard-error-gradient: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
            --dashboard-glow: 0 0 20px rgba(239, 68, 68, 0.3);
            --discord-color: #5865f2;
        }
        
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            background: var(--dashboard-bg);
            color: var(--dashboard-text);
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow-x: hidden;
            position: relative;
        }
        
        body::before {
            content: '';
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: linear-gradient(135deg, var(--dashboard-bg) 0%, #0f0f1a 100%);
            z-index: -2;
        }
        
        body::after {
            content: '';
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: 
                radial-gradient(circle at 20% 80%, rgba(239, 68, 68, 0.1) 0%, transparent 50%),
                radial-gradient(circle at 80% 20%, rgba(220, 38, 38, 0.1) 0%, transparent 50%),
                radial-gradient(circle at 40% 40%, rgba(239, 68, 68, 0.05) 0%, transparent 50%);
            z-index: -1;
        }
        
        .error-container {
            background: var(--dashboard-card);
            border: 1px solid var(--dashboard-border);
            border-radius: 24px;
            padding: 4rem 3rem;
            max-width: 500px;
            width: 90%;
            text-align: center;
            position: relative;
            overflow: hidden;
            backdrop-filter: blur(20px);
            box-shadow: 
                0 25px 50px rgba(0, 0, 0, 0.5),
                0 0 40px rgba(239, 68, 68, 0.1);
            animation: slideInUp 0.8s cubic-bezier(0.4, 0, 0.2, 1);
        }
        
        .error-container::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: linear-gradient(135deg, rgba(239, 68, 68, 0.05) 0%, rgba(220, 38, 38, 0.05) 100%);
            z-index: -1;
        }
        
        @keyframes slideInUp {
            from {
                opacity: 0;
                transform: translateY(40px) scale(0.95);
            }
            to {
                opacity: 1;
                transform: translateY(0) scale(1);
            }
        }
        
        .error-icon {
            width: 80px;
            height: 80px;
            margin: 0 auto 2rem;
            background: var(--dashboard-error-gradient);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            animation: shake 0.8s ease-out 0.5s both;
        }
        
        .error-icon::before {
            content: '';
            position: absolute;
            top: -4px;
            left: -4px;
            right: -4px;
            bottom: -4px;
            background: var(--dashboard-error-gradient);
            border-radius: 50%;
            opacity: 0.3;
            z-index: -1;
            animation: pulse 2s infinite;
        }
        
        @keyframes shake {
            0% { transform: scale(0.3) rotate(-10deg); opacity: 0; }
            25% { transform: scale(1.1) rotate(5deg); }
            50% { transform: scale(0.95) rotate(-3deg); }
            75% { transform: scale(1.05) rotate(2deg); }
            100% { transform: scale(1) rotate(0deg); opacity: 1; }
        }
        
        @keyframes pulse {
            0%, 100% { transform: scale(1); opacity: 0.3; }
            50% { transform: scale(1.1); opacity: 0.1; }
        }
        
        .error-mark {
            width: 32px;
            height: 32px;
            color: white;
            stroke-width: 3;
        }
        
        .error-title {
            font-size: 2rem;
            font-weight: 800;
            margin-bottom: 1rem;
            background: linear-gradient(135deg, #ffffff 0%, #ef4444 50%, #dc2626 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            background-clip: text;
            animation: fadeIn 0.8s ease-out 0.4s both;
        }
        
        .error-subtitle {
            font-size: 1.1rem;
            color: var(--dashboard-text-muted);
            margin-bottom: 2rem;
            line-height: 1.6;
            animation: fadeIn 0.8s ease-out 0.6s both;
        }
        
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
        
        .btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            padding: 1rem 1.5rem;
            border-radius: 12px;
            font-weight: 600;
            text-decoration: none;
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
            border: none;
            cursor: pointer;
            font-size: 0.95rem;
            position: relative;
            overflow: hidden;
            gap: 0.5rem;
            font-family: 'Inter', sans-serif;
        }
        
        .btn-secondary {
            background: transparent;
            color: var(--dashboard-text-muted);
            border: 1px solid var(--dashboard-border);
        }
        
        .btn-secondary:hover {
            background: rgba(255, 255, 255, 0.05);
            color: var(--dashboard-text);
            border-color: var(--dashboard-accent);
            transform: translateY(-1px);
        }
        
        .countdown-container {
            margin-top: 1.5rem;
            padding: 1rem;
            background: rgba(255, 255, 255, 0.03);
            border-radius: 12px;
            border: 1px solid var(--dashboard-border);
            animation: fadeIn 0.8s ease-out 1.2s both;
        }
        
        .countdown-text {
            font-size: 0.85rem;
            color: var(--dashboard-text-muted);
            margin-bottom: 0.5rem;
        }
        
        .countdown-timer {
            font-size: 1.1rem;
            font-weight: 700;
            color: var(--dashboard-danger);
        }
        
        .floating {
            animation: float 3s ease-in-out infinite;
        }
        
        @keyframes float {
            0%, 100% { transform: translateY(0px); }
            50% { transform: translateY(-8px); }
        }
    </style>
</head>
<body>
    <div class=""error-container"">
        <div class=""error-icon floating"">
            <svg class=""error-mark"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M6 18L18 6M6 6l12 12""/>
            </svg>
        </div>
        <h1 class=""error-title"">Discord Connection Failed!</h1>
        <p class=""error-subtitle"">Unable to establish connection with Discord. Please check your internet connection and try again.</p>
        <button class=""btn btn-secondary"" onclick=""closeTab()"">
            <svg width=""16"" height=""16"" fill=""none"" stroke=""currentColor"" viewBox=""0 0 24 24"">
                <path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M6 18L18 6M6 6l12 12""/>
            </svg>
            Close This Tab
        </button>
        <div class=""countdown-container"">
            <div class=""countdown-text"">This tab will automatically close in:</div>
            <div class=""countdown-timer"" id=""countdown"">8 seconds</div>
        </div>
    </div>
    <script>
        let countdown = 8;
        const countdownElement = document.getElementById('countdown');
        const timer = setInterval(() => {
            countdown--;
            countdownElement.textContent = countdown + ' seconds';
            if (countdown <= 0) {
                clearInterval(timer);
                closeTab();
            }
        }, 1000);
        
        function closeTab() {
            try {
                window.close();
            } catch (e) {
                countdownElement.textContent = 'Please close this tab manually';
                countdownElement.style.color = '#f59e0b';
            }
        }
        
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                closeTab();
            }
        });
    </script>
</body>
</html>";
                    
                    var buffer = Encoding.UTF8.GetBytes(errorHtml);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=utf-8";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();

                    AuthenticationFailed?.Invoke(this, EventArgs.Empty);
                }

                listener.Stop();
            }
            catch
            {
                AuthenticationFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task ExchangeCodeForToken(string code)
        {
            try
            {
                var tokenData = new Dictionary<string, string>
                {
                    ["client_id"] = _appKey,
                    ["client_secret"] = _apiSecret,
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = _callbackUrl
                };

                var formContent = new FormUrlEncodedContent(tokenData);
                var tokenResponse = await _httpClient.PostAsync($"{_serviceBase}/oauth2/token", formContent);
                
                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    var tokenResult = JsonConvert.DeserializeObject<dynamic>(tokenJson);
                    
                    _accessToken = tokenResult?.access_token;
                    _refreshToken = tokenResult?.refresh_token;
                    
                    if (!string.IsNullOrEmpty(_accessToken))
                    {
                        await GetUserInfo();
                    }
                    else
                    {
                        AuthenticationFailed?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    AuthenticationFailed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                AuthenticationFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task GetUserInfo()
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                    return;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                var userResponse = await _httpClient.GetAsync($"{_serviceBase}/users/@me");
                
                if (userResponse.IsSuccessStatusCode)
                {
                    var userJson = await userResponse.Content.ReadAsStringAsync();
                    var userData = JsonConvert.DeserializeObject<dynamic>(userJson);
                    
                    if (userData != null)
                    {
                        var discordUser = new DiscordUser
                        {
                            Id = userData.id,
                            Username = userData.username,
                            Discriminator = userData.discriminator ?? "0",
                            GlobalName = userData.global_name,
                            Avatar = new DiscordAvatar
                            {
                                Id = userData.avatar ?? "",
                                Link = GetAvatarUrl(userData.id?.ToString(), userData.avatar?.ToString()),
                                IsAnimated = userData.avatar?.ToString()?.StartsWith("a_") == true
                            }
                        };

                        if (!string.IsNullOrEmpty(_refreshToken))
                        {
                            await ConfigService.SaveDiscordInfoAsync(discordUser, _refreshToken);
                        }

                        UserAuthenticated?.Invoke(this, discordUser);
                    }
                    else
                    {
                        AuthenticationFailed?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    AuthenticationFailed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                AuthenticationFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        private string GetAvatarUrl(string? userId, string? avatarHash)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(avatarHash))
            {
                return "https://cdn.discordapp.com/embed/avatars/0.png";
            }

            var extension = avatarHash.StartsWith("a_") ? "gif" : "png";
            return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.{extension}?size=128";
        }

        public async Task<bool> ValidateTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                    return false;

                var userResponse = await _httpClient.GetAsync($"{_serviceBase}/users/@me");
                return userResponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async void Disconnect()
        {
            _accessToken = null;
            _refreshToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            await ConfigService.ClearDiscordInfoAsync();
        }

        private async Task<bool> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var tokenData = new Dictionary<string, string>
                {
                    ["client_id"] = _appKey,
                    ["client_secret"] = _apiSecret,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken
                };

                var formContent = new FormUrlEncodedContent(tokenData);
                var tokenResponse = await _httpClient.PostAsync($"{_serviceBase}/oauth2/token", formContent);
                
                if (tokenResponse.IsSuccessStatusCode)
                {
                    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                    var tokenResult = JsonConvert.DeserializeObject<dynamic>(tokenJson);
                    
                    _accessToken = tokenResult?.access_token;
                    _refreshToken = tokenResult?.refresh_token ?? refreshToken;
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                    
                    return !string.IsNullOrEmpty(_accessToken);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}




