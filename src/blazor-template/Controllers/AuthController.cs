using BlazorTemplate.Data;
using BlazorTemplate.DTO;
using BlazorTemplate.DTO.Api;
using BlazorTemplate.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;

namespace BlazorTemplate.Controllers
{
    public class AuthController : ApiControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and return JWT token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest<LoginResponse>("Invalid login request", GetModelStateErrors());
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                    return Unauthorized<LoginResponse>("Invalid email or password");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
                
                if (!result.Succeeded)
                {
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Login attempt for locked account: {Email}", request.Email);
                        return Unauthorized<LoginResponse>("Account is locked out");
                    }
                    
                    if (result.RequiresTwoFactor)
                    {
                        return Unauthorized<LoginResponse>("Two-factor authentication required");
                    }
                    
                    _logger.LogWarning("Failed login attempt for: {Email}", request.Email);
                    return Unauthorized<LoginResponse>("Invalid email or password");
                }

                if (!user.EmailConfirmed)
                {
                    return Unauthorized<LoginResponse>("Email not confirmed");
                }

                // Generate JWT token
                var tokenResponse = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    CreatedAt = DateTime.UtcNow, // Note: Default IdentityUser doesn't have CreatedAt
                    PhoneNumber = user.PhoneNumber,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    Roles = roles.ToList()
                };

                var loginResponse = new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresAt = tokenResponse.ExpiresAt,
                    User = userDto
                };

                _logger.LogInformation("User {Email} logged in successfully via API", user.Email);
                return Success(loginResponse, "Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API login for email: {Email}", request.Email);
                return Error<LoginResponse>("An error occurred during login", 500);
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest<TokenResponse>("Invalid refresh token request", GetModelStateErrors());
            }

            try
            {
                var tokenResponse = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken);
                return Success(tokenResponse, "Token refreshed successfully");
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Invalid refresh token used");
                return Unauthorized<TokenResponse>("Invalid refresh token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return Error<TokenResponse>("An error occurred during token refresh", 500);
            }
        }

        /// <summary>
        /// Revoke refresh token
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Revoke([FromBody] RevokeTokenRequest request)
        {
            try
            {
                await _jwtTokenService.RevokeTokenAsync(request.RefreshToken);
                return Success("Token revoked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token revocation");
                return Error("An error occurred during token revocation", 500);
            }
        }

        /// <summary>
        /// Get current user profile information
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFound<UserDto>("User not found");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    CreatedAt = DateTime.UtcNow, // Note: Default IdentityUser doesn't have CreatedAt
                    PhoneNumber = user.PhoneNumber,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    Roles = roles.ToList()
                };

                return Success(userDto, "Profile retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile for user: {UserId}", GetCurrentUserId());
                return Error<UserDto>("An error occurred while retrieving profile", 500);
            }
        }

        /// <summary>
        /// Logout user (client-side token removal, server-side revocation if refresh token provided)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RevokeTokenRequest? request = null)
        {
            try
            {
                // If refresh token is provided, revoke it
                if (request != null && !string.IsNullOrEmpty(request.RefreshToken))
                {
                    await _jwtTokenService.RevokeTokenAsync(request.RefreshToken);
                }

                _logger.LogInformation("User {UserId} logged out via API", GetCurrentUserId());
                return Success("Logout successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API logout for user: {UserId}", GetCurrentUserId());
                return Error("An error occurred during logout", 500);
            }
        }
    }
}