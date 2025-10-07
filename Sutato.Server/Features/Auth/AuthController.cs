using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sutato.Shared.Features.Auth;
using Sutato.Shared.Features.Common.Constants;
using System.Security.Claims;

namespace Sutato.Server.Features.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly ApiSettings _apiSettings;

        public AuthController(TokenService tokenService, IOptions<ApiSettings> apiSettings)
        {
            _tokenService = tokenService;
            _apiSettings = apiSettings.Value;
        }

        [HttpPost("generate-token")]
        public IActionResult GenerateToken(
            [FromHeader] string username,
            [FromHeader] string email,
            [FromHeader] string mobileNo,
            [FromHeader] string role,
            [FromHeader(Name = "ApiKey")] string apiKey)
        {
            if (apiKey != _apiSettings.Key)
                return Unauthorized("Invalid API Key");

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(mobileNo) ||
                string.IsNullOrWhiteSpace(role))
                return BadRequest("Missing required headers");

            if (role != UserRoles.SysAdmin && role != UserRoles.Admin &&
                role != UserRoles.User && role != UserRoles.Guest)
                return BadRequest("Invalid role");

            var token = _tokenService.GenerateToken(username, email, mobileNo, role);
            return Ok(new
            {
                token,
                expiresAt = DateTime.UtcNow.AddMinutes(30)
            });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] Shared.Features.Auth.RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Token is required.");

            // 1. Validate old token
            var principal = _tokenService.ValidateToken(request.Token);
            if (principal == null)
                return BadRequest("Invalid token.");

            // 2. Extract claims from the old token
            var username = principal.FindFirst("username")?.Value;
            var email = principal.FindFirst("email")?.Value;
            var mobileNo = principal.FindFirst("mobileNo")?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
                return BadRequest("Invalid claims in token.");

            // 3. Generate new token
            var newToken = _tokenService.GenerateToken(username, role, email, mobileNo);

            return Ok(new
            {
                token = newToken,
                expiresAt = DateTime.UtcNow.AddMinutes(30)
            });
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] Shared.Features.Auth.LoginRequest request)
        {
            // Static user check for now
            if (request.Username == "admin" && request.Password == "1234")
            {
                var token = _tokenService.GenerateToken(
                    request.Username,
                    "Admin",
                    "denaro@example.com",
                    "09123456789"
                );

                return Ok(new
                {
                    token,
                    expiresAt = DateTime.UtcNow.AddMinutes(30)
                });
            }
            else if (request.Username == "user" && request.Password == "1234")
            {
                var token = _tokenService.GenerateToken(
                    request.Username,
                    "user",
                    "denaro@example.com",
                    "09123456789"
                );

                return Ok(new
                {
                    token,
                    expiresAt = DateTime.UtcNow.AddMinutes(30)
                });
            }

            return Unauthorized("Invalid credentials");
        }


    }
}
