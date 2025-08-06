using BlazorTemplate.Data;
using BlazorTemplate.DTO.Api;
using System.Security.Claims;

namespace BlazorTemplate.Services
{
    public interface IJwtTokenService
    {
        Task<TokenResponse> GenerateTokenAsync(ApplicationUser user);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);
        ClaimsPrincipal? GetPrincipalFromToken(string token);
        Task<bool> IsTokenValidAsync(string token);
        Task CleanExpiredTokensAsync();
    }
}