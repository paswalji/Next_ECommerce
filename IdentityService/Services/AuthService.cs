using IdentityService.API.Models;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
            _context = context;
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
            try
            {
                var userExists = await _userManager.FindByNameAsync(model.UserName);
                if (userExists != null)
                    return "User already exists!";

                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                    return string.Join("; ", result.Errors.Select(e => e.Description));

                if (!await _roleManager.RoleExistsAsync(model.Role))
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));

                await _userManager.AddToRoleAsync(user, model.Role);

                return "User registered successfully!";
            }
            catch (Exception ex)
            {
                return $"Registration failed: {ex.Message}";
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginModel model, string ipAddress)
        {
            try
            {
                var user = await _userManager.Users.Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.UserName == model.UserName);

                if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                    return new AuthResponse { Message = "Invalid username or password" };

                var jwtToken = await GenerateJwtTokenAsync(user);
                var refreshToken = GenerateRefreshToken(ipAddress);

                user.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();

                return new AuthResponse
                {
                    Token = jwtToken,
                    RefreshToken = refreshToken.Token,
                    Expiration = DateTime.UtcNow.AddHours(3),
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse { Message = $"Login failed: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.RefreshTokens)
                    .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == token));

                if (user == null)
                    return new AuthResponse { Message = "Invalid token" };

                var existingToken = user.RefreshTokens.FirstOrDefault(t => t.Token == token);
                if (existingToken == null || !existingToken.IsActive)
                    return new AuthResponse { Message = "Token expired or revoked" };

                var newRefreshToken = GenerateRefreshToken(ipAddress);
                existingToken.Revoked = DateTime.UtcNow;
                existingToken.RevokedByIp = ipAddress;
                existingToken.ReplacedByToken = newRefreshToken.Token;

                user.RefreshTokens.Add(newRefreshToken);
                await _context.SaveChangesAsync();

                var newJwtToken = await GenerateJwtTokenAsync(user);

                return new AuthResponse
                {
                    Token = newJwtToken,
                    RefreshToken = newRefreshToken.Token,
                    Expiration = DateTime.UtcNow.AddHours(3),
                    Message = "Token refreshed"
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse { Message = $"Token refresh failed: {ex.Message}" };
            }
        }

        // ----- Helpers -----

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(string ipAddress)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }
    }
}
