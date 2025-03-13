using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Repository.Services
{
    public class EncryptionService
    {
        private readonly IDataProtector _protector;
        public EncryptionService(IDataProtectionProvider provider, IConfiguration configuration)
        {
            string appName = configuration["DataProtection:ApplicationName"] ?? "DefaultApp";
            _protector = provider.CreateProtector($"{appName}.Encryption");
        }

        public string EncryptData(string plainText) => _protector.Protect(plainText);
        public string DecryptData(string encryptedData)
        {
            try { return _protector.Unprotect(encryptedData); }
            catch (CryptographicException) { throw new CryptographicException("Decryption failed. Key might have changed."); }
        }

        public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
        public bool VerifyPassword(string enteredPassword, string hashedPassword) => BCrypt.Net.BCrypt.Verify(enteredPassword, hashedPassword);

    }
}
