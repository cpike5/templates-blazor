using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace BlazorTemplate.Services
{
    /// <summary>
    /// Service implementation for encrypting and decrypting sensitive settings
    /// </summary>
    public class SettingsEncryptionService : ISettingsEncryptionService
    {
        private readonly byte[] _key;
        private readonly ILogger<SettingsEncryptionService> _logger;

        // List of setting keys that should be encrypted
        private static readonly HashSet<string> EncryptedKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "Email.Password",
            "Integrations.GoogleClientSecret",
            "Integrations.MicrosoftClientSecret",
            "Email.SmtpPassword"
        };

        public SettingsEncryptionService(IConfiguration configuration, ILogger<SettingsEncryptionService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogDebug("Initializing SettingsEncryptionService");
            
            try
            {
                // Get encryption key from configuration or environment
                var keyString = configuration?["Site:Settings:EncryptionKey"] 
                             ?? Environment.GetEnvironmentVariable("SETTINGS_ENCRYPTION_KEY")
                             ?? "DefaultKeyForDevelopmentOnly!"; // Default for development only

                if (keyString == "DefaultKeyForDevelopmentOnly!")
                {
                    _logger.LogWarning("Using default encryption key. This should not be used in production! Consider setting Site:Settings:EncryptionKey or SETTINGS_ENCRYPTION_KEY environment variable");
                }
                else
                {
                    _logger.LogInformation("Using custom encryption key from configuration");
                }

                // Create a consistent 32-byte key from the provided string
                _key = CreateKey(keyString);
                
                _logger.LogDebug("SettingsEncryptionService initialized with {EncryptedKeyCount} encrypted setting keys configured",
                    EncryptedKeys.Count);
                _logger.LogTrace("Encrypted setting keys: {EncryptedKeys}", string.Join(", ", EncryptedKeys));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SettingsEncryptionService");
                throw;
            }
        }

        /// <summary>
        /// Encrypts a plain text value using AES encryption with a randomly generated IV
        /// </summary>
        /// <param name='plainText'>The plain text value to encrypt</param>
        /// <returns>Base64-encoded encrypted value with IV prepended</returns>
        /// <exception cref='InvalidOperationException'>Thrown when encryption fails</exception>
        public string Encrypt(string plainText)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (string.IsNullOrEmpty(plainText))
            {
                _logger.LogTrace("Encrypt called with null or empty plaintext, returning as-is");
                return plainText;
            }
            
            var plaintextLength = plainText.Length;
            _logger.LogTrace("Starting encryption of {PlaintextLength} character value", plaintextLength);

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.GenerateIV();
                
                _logger.LogTrace("AES encryption initialized with {KeySize}-bit key and {IvSize}-byte IV",
                    aes.KeySize, aes.IV.Length);

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                
                // Write IV to the beginning of the stream
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);
                
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                var encryptedData = msEncrypt.ToArray();
                var result = Convert.ToBase64String(encryptedData);
                
                stopwatch.Stop();
                _logger.LogDebug("Successfully encrypted {PlaintextLength} character value to {EncryptedLength} character base64 string in {ElapsedMs}ms",
                    plaintextLength, result.Length, stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to encrypt settings value (Length: {PlaintextLength}) after {ElapsedMs}ms",
                    plaintextLength, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("Failed to encrypt settings value", ex);
            }
        }

        /// <summary>
        /// Decrypts a base64-encoded encrypted value using AES decryption
        /// </summary>
        /// <param name='cipherText'>The base64-encoded encrypted value to decrypt</param>
        /// <returns>The original plain text value</returns>
        /// <exception cref='InvalidOperationException'>Thrown when decryption fails</exception>
        public string Decrypt(string cipherText)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (string.IsNullOrEmpty(cipherText))
            {
                _logger.LogTrace("Decrypt called with null or empty ciphertext, returning as-is");
                return cipherText;
            }
            
            var ciphertextLength = cipherText.Length;
            _logger.LogTrace("Starting decryption of {CiphertextLength} character base64 value", ciphertextLength);

            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                _logger.LogTrace("Converted base64 to {BufferLength} byte buffer", buffer.Length);
                
                using var aes = Aes.Create();
                aes.Key = _key;
                
                // Extract IV from the beginning of the buffer
                var iv = new byte[aes.BlockSize / 8];
                if (buffer.Length < iv.Length)
                {
                    throw new InvalidOperationException($"Encrypted data is too short. Expected at least {iv.Length} bytes for IV, got {buffer.Length} bytes.");
                }
                
                Array.Copy(buffer, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                _logger.LogTrace("Extracted {IvSize}-byte IV from encrypted data", iv.Length);

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                
                var result = srDecrypt.ReadToEnd();
                
                stopwatch.Stop();
                _logger.LogDebug("Successfully decrypted {CiphertextLength} character base64 string to {PlaintextLength} character value in {ElapsedMs}ms",
                    ciphertextLength, result.Length, stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to decrypt settings value (Length: {CiphertextLength}) after {ElapsedMs}ms",
                    ciphertextLength, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("Failed to decrypt settings value", ex);
            }
        }

        /// <summary>
        /// Determines whether a setting key should be encrypted based on the configured encrypted keys list
        /// </summary>
        /// <param name='key'>The setting key to check</param>
        /// <returns>True if the key should be encrypted, false otherwise</returns>
        public bool ShouldEncrypt(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogTrace("ShouldEncrypt called with null or empty key, returning false");
                return false;
            }
            
            var shouldEncrypt = EncryptedKeys.Contains(key);
            _logger.LogTrace("Encryption check for key {SettingKey}: {ShouldEncrypt}", key, shouldEncrypt);
            
            return shouldEncrypt;
        }

        /// <summary>
        /// Creates a consistent 32-byte encryption key from a string using SHA256 hashing
        /// </summary>
        /// <param name='keyString'>The input string to derive the key from</param>
        /// <returns>A 32-byte encryption key</returns>
        private static byte[] CreateKey(string keyString)
        {
            if (string.IsNullOrEmpty(keyString))
                throw new ArgumentException("Key string cannot be null or empty", nameof(keyString));
            
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        }
    }
}