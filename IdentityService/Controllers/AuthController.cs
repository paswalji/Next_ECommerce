using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var result = await _authService.RegisterAsync(model);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Registration failed: {ex.Message}" });
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var response = await _authService.LoginAsync(model, ipAddress);

                if (string.IsNullOrWhiteSpace(response.Token))
                    return Unauthorized(new { message = response.Message });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Login failed: {ex.Message}" });
            }
        }

        // POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var result = await _authService.RefreshTokenAsync(request.Token, ipAddress);

                if (string.IsNullOrWhiteSpace(result.Token))
                    return Unauthorized(new { message = result.Message });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Token refresh failed: {ex.Message}" });
            }
        }

        // POST: api/auth/revoke-token
        [HttpPost("revoke-token")]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request, [FromServices] AppDbContext _context)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                var user = _context.Users
                    .Where(u => u.RefreshTokens.Any(t => t.Token == request.Token))
                    .FirstOrDefault();

                if (user == null)
                    return NotFound(new { message = "Token not found" });

                var token = user.RefreshTokens.FirstOrDefault(t => t.Token == request.Token);

                if (token == null || !token.IsActive)
                    return BadRequest(new { message = "Token is already revoked or expired" });

                token.Revoked = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Token revoked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Token revocation failed: {ex.Message}" });
            }
        }
    }
}
