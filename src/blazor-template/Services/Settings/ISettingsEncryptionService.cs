namespace BlazorTemplate.Services.Settings
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive settings data
    /// </summary>
    public interface ISettingsEncryptionService
    {
        /// <summary>
        /// Encrypts a plain text string
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Encrypted string</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts an encrypted string
        /// </summary>
        /// <param name="cipherText">Encrypted text</param>
        /// <returns>Decrypted plain text</returns>
        string Decrypt(string cipherText);

        /// <summary>
        /// Determines if a setting key should be encrypted
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <returns>True if the setting should be encrypted</returns>
        bool ShouldEncrypt(string key);
    }
}