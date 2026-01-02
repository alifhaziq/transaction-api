using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TransactionApi.Services
{
    /// <summary>
    /// Service for encrypting passwords and masking sensitive data in logs
    /// </summary>
    public class PasswordEncryptionService : IPasswordEncryptionService
    {
        private readonly string _encryptionKey;

        public PasswordEncryptionService(IConfiguration configuration)
        {
            // Get encryption key from configuration or use default
            _encryptionKey = configuration["Logging:EncryptionKey"] ?? "TransactionAPI@2026!SecureKey#";
        }

        /// <summary>
        /// Encrypts a password using AES encryption for secure logging
        /// </summary>
        public string EncryptForLogging(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "[EMPTY]";

            try
            {
                using (Aes aes = Aes.Create())
                {
                    // Derive key and IV from the encryption key
                    byte[] key = DeriveKey(_encryptionKey, 32); // 256-bit key
                    byte[] iv = DeriveKey(_encryptionKey, 16);  // 128-bit IV

                    aes.Key = key;
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(password);
                            }
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch
            {
                return "[ENCRYPTION_ERROR]";
            }
        }

        /// <summary>
        /// Masks sensitive data in JSON strings (passwords, signatures)
        /// </summary>
        public string MaskSensitiveData(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
                return jsonContent;

            try
            {
                // Parse JSON and mask sensitive fields
                var doc = JsonDocument.Parse(jsonContent);
                var maskedJson = MaskJsonElement(doc.RootElement);
                return JsonSerializer.Serialize(maskedJson, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                // If JSON parsing fails, use regex to mask sensitive fields
                return MaskSensitiveDataWithRegex(jsonContent);
            }
        }

        private object MaskJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        // Mask sensitive fields
                        if (IsSensitiveField(property.Name))
                        {
                            if (property.Name.Equals("partnerpassword", StringComparison.OrdinalIgnoreCase))
                            {
                                // Encrypt password
                                var passwordValue = property.Value.GetString() ?? "";
                                obj[property.Name] = $"[ENCRYPTED:{EncryptForLogging(passwordValue)}]";
                            }
                            else
                            {
                                // Mask other sensitive fields
                                obj[property.Name] = "[MASKED]";
                            }
                        }
                        else
                        {
                            obj[property.Name] = MaskJsonElement(property.Value);
                        }
                    }
                    return obj;

                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(MaskJsonElement(item));
                    }
                    return array;

                case JsonValueKind.String:
                    return element.GetString() ?? "";

                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return element.ToString();
            }
        }

        private bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[]
            {
                "password", "partnerpassword", "sig", "signature",
                "token", "apikey", "secret", "authorization"
            };

            return sensitiveFields.Any(sf => 
                fieldName.Contains(sf, StringComparison.OrdinalIgnoreCase));
        }

        private string MaskSensitiveDataWithRegex(string content)
        {
            // Mask password fields
            content = Regex.Replace(content, 
                @"""(partner)?password""\s*:\s*""([^""]+)""", 
                match => {
                    var fieldName = match.Groups[1].Value;
                    var password = match.Groups[2].Value;
                    var encrypted = EncryptForLogging(password);
                    return $"\"{fieldName}password\": \"[ENCRYPTED:{encrypted}]\"";
                }, 
                RegexOptions.IgnoreCase);

            // Mask signature fields
            content = Regex.Replace(content, 
                @"""(sig|signature)""\s*:\s*""([^""]+)""", 
                "\"$1\": \"[MASKED]\"", 
                RegexOptions.IgnoreCase);

            return content;
        }

        private byte[] DeriveKey(string password, int keyLength)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] key = new byte[keyLength];
                Array.Copy(hash, key, keyLength);
                return key;
            }
        }
    }
}

