using StorageHelper.Services.Data;
using System.Security.Cryptography;

namespace StorageHelper.Services
{
    public interface IAuthService
    {
        Task<bool> TryEnterManagerModeAsync(string password);
    }

    public partial class AuthService : IAuthService
    {
        private IConfigService _config;

        private const int ITERATIONS = 100_000;
        private const int LENGTH = 32;

        public AuthService(IConfigService config)
        {
            _config = config;
        }

        //Null password = new password
        public async Task<bool> TryEnterManagerModeAsync(string password)
        {
            var curPassword = _config.Current.Password;
            if(curPassword == null)
            {
                byte[] salt = RandomNumberGenerator.GetBytes(16);
                byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, ITERATIONS, HashAlgorithmName.SHA256, LENGTH);
                _config.Current.Password = new(Convert.ToHexString(hash), Convert.ToHexString(salt));

                await _config.SaveAsync();

                return true;
            }

            byte[] check = Rfc2898DeriveBytes.Pbkdf2(password, 
                Convert.FromHexString(curPassword.Salt), ITERATIONS, HashAlgorithmName.SHA256, LENGTH);
            bool time = CryptographicOperations.FixedTimeEquals(check, Convert.FromHexString(curPassword.Hash));

            return time;
        }
    }
}
