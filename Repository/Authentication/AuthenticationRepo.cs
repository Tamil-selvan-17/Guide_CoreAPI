using Domain.Interface;
using Domain.Models;
using Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Repository.Authentication
{
    public class AuthenticationRepo : IAuthentication
    {
        private readonly Your_DEVContext _aDbManager;
        private readonly EncryptionService _encryptionService;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthenticationRepo> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private bool _disposed = false;

        public AuthenticationRepo(Your_DEVContext aDbManager, EncryptionService encryptionService,
                                  JwtService jwtService, ILogger<AuthenticationRepo> logger,
                                  IHttpContextAccessor httpContextAccessor)
        {
            _aDbManager = aDbManager;
            _encryptionService = encryptionService;
            _jwtService = jwtService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool status, string message, string username, long userid)> Login(AuthenticationModel authentication)
        {
            try
            {
                var loginDetails = await _aDbManager.tbl_logins
                    .AsNoTracking()
                    .Where(x => x.login_name == authentication.Username)
                    .Select(x => new { x.login_name, x.login_pass, x.login_id })
                    .FirstOrDefaultAsync();

                if (loginDetails == null)
                {
                    _logger.LogWarning("Login failed: User not found ({Username})", authentication.Username);
                    return (false, "Invalid User", string.Empty, 0);
                }

                if (!_encryptionService.VerifyPassword(authentication.Password, loginDetails.login_pass))
                {
                    _logger.LogWarning("Login failed: Incorrect password for user ({Username})", authentication.Username);
                    return (false, "Incorrect Password", loginDetails.login_name, 0);
                }

                SetAuthTokenCookie("Admin", loginDetails.login_name);
                _logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully.",loginDetails.login_name, loginDetails.login_id);


                return (true, "Login Success", loginDetails.login_name, loginDetails.login_id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user {Username}.", authentication.Username);

                throw; // Let GlobalExceptionHandler handle it
            }
        }

        // ✅ Separate method for setting the auth token and cookie
        private void SetAuthTokenCookie(string userrole, string username)
        {
            var token = _jwtService.GenerateToken(username, userrole);
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(30)
            });
        }

        public async Task<(bool status, string message)> Register(AuthenticationModel register)
        {
            try
            {
                var existingUser = await _aDbManager.tbl_logins
                    .AsNoTracking()
                    .Where(x => x.login_name == register.Username)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: User ({Username}) already exists", register.Username);
                    return (false, "User is already registered!");
                }

                var registerUser = new tbl_login
                {
                    login_name = register.Username,
                    login_pass = _encryptionService.HashPassword(register.Password)
                };

                await _aDbManager.tbl_logins.AddAsync(registerUser);
                await _aDbManager.SaveChangesAsync();

                _logger.LogInformation("User {Username} registered successfully", register.Username);
                return (true, "User is registered! Please login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration ({Username})", register.Username);
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _aDbManager != null)
                {
                    _aDbManager.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
