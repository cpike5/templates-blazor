using BlazorTemplate.Data;
using BlazorTemplate.DTO.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BlazorTemplate.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<JwtTokenService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<TokenResponse> GenerateTokenAsync(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
            
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, 
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                    ClaimValueTypes.Integer64)
            };

            // Add role claims
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30");
            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                TokenType = "Bearer"
            };
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || !token.IsActive)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            // Revoke old token
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            
            // Generate new tokens
            var newTokenResponse = await GenerateTokenAsync(token.User);
            
            // Replace old refresh token
            token.ReplacedByToken = newTokenResponse.RefreshToken;
            
            await _context.SaveChangesAsync();

            return newTokenResponse;
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token != null && token.IsActive)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);
            }
        }

        public ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = false, // We check expiration separately
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                
                if (validatedToken is not JwtSecurityToken jwtToken || 
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate JWT token");
                return null;
            }
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var principal = GetPrincipalFromToken(token);
            if (principal == null) return false;

            var jwtToken = new JwtSecurityToken(token);
            return jwtToken.ValidTo > DateTime.UtcNow;
        }

        public async Task CleanExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiryDate <= DateTime.UtcNow || rt.IsRevoked)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
            }
        }

        private async Task<string> GenerateRefreshTokenAsync(string userId)
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);
            var refreshToken = Convert.ToBase64String(randomBytes);

            var expirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
            
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(expirationDays),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return refreshToken;
        }
    }
}