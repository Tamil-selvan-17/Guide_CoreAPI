using Domain.Interface;
using Domain.Models;
using GuidProject.BAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;

namespace GuidProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthenticationBL _authenticationBL;

        public AuthenticationController(AuthenticationBL authenticationBL)
        {
            _authenticationBL = authenticationBL;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AuthenticationModel authentication)
        {
            try
            {
                var result = await _authenticationBL.Login(authentication);
                return Ok(new { status = result.status, message = result.message, username = result.username, userid = result.userid });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthenticationModel register)
        {
            try
            {
                var result = await _authenticationBL.Register(register);
                return Ok(new { status = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            // ✅ Correctly delete the HttpOnly cookie by setting an expired timestamp
            Response.Cookies.Append("AuthToken", "", new CookieOptions
            {
                HttpOnly = true, // ✅ Ensure same settings as when it was created
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(-1) // ✅ Expire the cookie
            });

            return Ok(new { status = true, message = "Logged out successfully" });
        }

        [HttpGet("protected")]
        public IActionResult ProtectedRoute()
        {
            if (Request.Cookies.TryGetValue("AuthToken", out string? jwtToken) && !string.IsNullOrEmpty(jwtToken))
            {
                return Ok(new { status = true, message = "You are authenticated!" });
            }

            return Unauthorized(new { status = false, message = "Not authenticated!" });
        }
    }
}

