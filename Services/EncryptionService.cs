using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WrightLauncher.Services
{
    public static class EncryptionService
    {
        private static readonly string MachineKey = Environment.MachineName + Environment.UserName + Environment.ProcessorCount;
        
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = DeriveKey(MachineKey);
                    aes.GenerateIV();
                    
                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                        
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                        }
                        
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = DeriveKey(MachineKey);
                    
                    byte[] iv = new byte[16];
                    Array.Copy(encryptedBytes, 0, iv, 0, 16);
                    aes.IV = iv;
                    
                    byte[] actualEncryptedData = new byte[encryptedBytes.Length - 16];
                    Array.Copy(encryptedBytes, 16, actualEncryptedData, 0, actualEncryptedData.Length);
                    
                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(actualEncryptedData))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private static byte[] DeriveKey(string source)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(source, Encoding.UTF8.GetBytes("WrightSkinsSalt2025"), 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32);
            }
        }
    }
}


