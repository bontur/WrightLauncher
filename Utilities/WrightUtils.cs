using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace WrightLauncher.Utilities
{
    public static class WrightUtils
    {
        // Simple XOR key for local data obfuscation (change this in your fork)
        private static readonly byte[] Key = { 0x57, 0x72, 0x69, 0x67, 0x68, 0x74, 0x4C, 0x61, 0x75, 0x6E, 0x63, 0x68, 0x65, 0x72 };

        public static string Obfuscate(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] result = new byte[inputBytes.Length];

            for (int i = 0; i < inputBytes.Length; i++)
            {
                result[i] = (byte)(inputBytes[i] ^ Key[i % Key.Length]);
            }

            return Convert.ToBase64String(result);
        }

        public static string Deobfuscate(string obfuscatedInput)
        {
            if (string.IsNullOrEmpty(obfuscatedInput))
                return string.Empty;

            try
            {
                byte[] inputBytes = Convert.FromBase64String(obfuscatedInput);
                byte[] result = new byte[inputBytes.Length];

                for (int i = 0; i < inputBytes.Length; i++)
                {
                    result[i] = (byte)(inputBytes[i] ^ Key[i % Key.Length]);
                }

                return Encoding.UTF8.GetString(result);
            }
            catch
            {
                return string.Empty;
            }
        }

        // Configuration values - set these in your local config file
        public static string A => GetConfigValue("DiscordClientId", "YOUR_DISCORD_CLIENT_ID");
        public static string B => GetConfigValue("DiscordClientSecret", "YOUR_DISCORD_CLIENT_SECRET");
        public static string C => GetConfigValue("DiscordCallbackUrl", "YOUR_DISCORD_CALLBACK_URL");
        public static string D => GetConfigValue("ApiEndpoint1", "YOUR_API_ENDPOINT_1");
        public static string E => GetConfigValue("ApiEndpoint2", "YOUR_API_ENDPOINT_2");
        public static string F => GetConfigValue("ServiceBaseUrl", "YOUR_SERVICE_BASE_URL");
        public static string K => GetConfigValue("ApiUsername", "YOUR_API_USERNAME");
        public static string L => GetConfigValue("ApiPassword", "YOUR_API_PASSWORD");
        public static string M => GetConfigValue("ApiAuth", "YOUR_API_AUTH");

        private static string GetConfigValue(string key, string defaultValue)
        {
            // Try to get from environment variable first
            var envValue = Environment.GetEnvironmentVariable($"WRIGHT_LAUNCHER_{key.ToUpper()}");
            if (!string.IsNullOrEmpty(envValue))
                return envValue;

            // Try to get from config file
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (config?.ContainsKey(key) == true)
                        return config[key];
                }
            }
            catch
            {
                // Config file read failed, use default
            }

            return defaultValue;
        }

        public static string UpdateCheck => DoubleDeobfuscate("VEYgGTotKzE9ACs5cWUcLyUOGT8HLzNGHykxGiAJ");
        
        public static string UpdateMessage => DoubleDeobfuscate("VEYgGTo+AzEUIigDC1kyKy4BMS4GEyAtNAY9CG9VFgo7ACgzHhIfKyUsMS8rGQY7NhkuVCM6HDlgLyYZAy8jGTo7HTcbJiEtHz4JbzsGMycENDoeOzM1KykePSstGQ47MDcgVE46HTYgKyJ1DyMsNz87JS06EjYjGQY8KyEOHyteKCUKJhoLKRcfKyUpMz8bMBw=");
        
        public static string UpdateAvailable => DoubleDeobfuscate("VEYgGTo+AzE4IDEeDxk+HQs+Kjo6BA==");
        
        public static string AllTags => DoubleDeobfuscate("VjuwGT8wGTsqCiEZJzExGz4SPhUHJhYlFCIe");
        
        public static string TagsSelected => DoubleDeobfuscate("VjuwGT8wGTsqCiEZJzExBRkBOR0gPi8jEx4gKyoUMy8rOAExCQ==");
        
        public static string YouTubeInvalid => DoubleDeobfuscate("VjuwGT8wGTsqCiEZJzExADkyGT8aMikDFyItJh8VJyYEKyJPOS4=");
        
        public static string LobbyInvite => DoubleDeobfuscate("VjuwGT8wGTsqCiEZJzExFDsoAAEtJWYhMBoKNigCHjARVS4=");

        private static string DoubleDeobfuscate(string input)
        {
            string firstDecode = Deobfuscate(input);
            return Deobfuscate(firstDecode);
        }
    }
}


