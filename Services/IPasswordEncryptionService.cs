namespace TransactionApi.Services
{
    /// <summary>
    /// Service for encrypting/masking passwords in logs
    /// </summary>
    public interface IPasswordEncryptionService
    {
        /// <summary>
        /// Encrypts a password for secure logging
        /// </summary>
        string EncryptForLogging(string password);

        /// <summary>
        /// Masks sensitive data in JSON strings for logging
        /// </summary>
        string MaskSensitiveData(string jsonContent);
    }
}

