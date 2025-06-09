using Domain.Interface;
using Domain.Models;
using Microsoft.Win32;

namespace GuidProject.BAL
{
    public class AuthenticationBL
    {
        
        private readonly IAuthentication _authenticationRepo;

        public AuthenticationBL(IAuthentication authenticationRepo)
        {
            _authenticationRepo = authenticationRepo;
        }
        public async Task<(bool status, string message, string username, long userid)> Login(AuthenticationModel authentication)
        {
            // Basic input validation before calling the repository
            if (string.IsNullOrWhiteSpace(authentication.Username) || string.IsNullOrWhiteSpace(authentication.Password))
            {
                return (false, "Username and Password are required.", string.Empty, 0);
            }

            var result = await _authenticationRepo.Login(authentication);

            // Additional business logic after repository call
            if (!result.status)
            {
                return (false, "Invalid credentials. Please check your username and password.", string.Empty, 0);
            }

            return result; // If login is successful, return result from repository
        }

        public async Task<(bool status, string message)> Register(AuthenticationModel register)
        {
            // ✅ Basic validation
            if (string.IsNullOrWhiteSpace(register.Username) || string.IsNullOrWhiteSpace(register.Password))
            {
                return (false, "Username and Password are required.");
            }

            if (register.Password.Length < 6)
            {
                return (false, "Password must be at least 6 characters long.");
            }

            // ✅ Ensure Register method is awaited properly inside an async method
            var result = await _authenticationRepo.Register(register);

            if (result.status)
            {
                // ✅ Additional logic (e.g., sending welcome emails)
            }

            return result;
        }

    }
}
