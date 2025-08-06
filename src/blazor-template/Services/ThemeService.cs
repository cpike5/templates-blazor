using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using BlazorTemplate.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;

namespace BlazorTemplate.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly NavigationManager _navigationManager;
    private string _currentTheme = "default";
    private bool _isDarkMode = false;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private TaskCompletionSource<bool>? _initializationTask;

    public ThemeService(IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _userManager = userManager;
        _dbContext = dbContext;
        _navigationManager = navigationManager;
    }

    public event Action? OnThemeChanged;

    public string CurrentTheme => _currentTheme;
    public bool IsDarkMode => _isDarkMode;

    public async Task<bool> EnsureInitializedAsync()
    {
        if (_isInitialized)
            return true;

        await _initSemaphore.WaitAsync();
        try
        {
            if (_isInitialized)
                return true;

            _initializationTask = new TaskCompletionSource<bool>();
            
            // Load theme synchronously from server-side sources first
            await LoadThemeFromServerSideSourcesAsync();
            
            _isInitialized = true;
            _initializationTask.SetResult(true);
            return true;
        }
        finally
        {
            _initSemaphore.Release();
        }
    }

    public async Task InitializeAsync(bool afterRender = false)
    {
        if (!afterRender)
        {
            await EnsureInitializedAsync();
            return;
        }

        // Wait for initialization to complete if it's in progress
        if (_initializationTask != null && !_initializationTask.Task.IsCompleted)
        {
            await _initializationTask.Task;
        }

        if (!_isInitialized)
        {
            await EnsureInitializedAsync();
        }

        try
        {
            // After render, sync with localStorage and apply theme
            await SyncWithLocalStorageAsync();
            await ApplyThemeAsync();
            OnThemeChanged?.Invoke();
        }
        catch
        {
            // Fallback to default theme if JavaScript is not available
            await ApplyThemeAsync();
        }
    }

    private async Task LoadThemeFromServerSideSourcesAsync()
    {
        try
        {
            // Try to load from database first for authenticated users
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(authState.User);
                if (user?.ThemePreference != null)
                {
                    LoadUserThemeAsync(user.ThemePreference);
                    return;
                }
            }
        }
        catch
        {
            // Continue with defaults if database access fails
        }
        
        // Set defaults if no user preference found
        _currentTheme = "default";
        _isDarkMode = false;
    }

    private async Task SyncWithLocalStorageAsync()
    {
        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "app-theme");
            var savedDarkMode = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "app-dark-mode");
            
            // Only update from localStorage if we don't have user preferences from database
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            bool hasUserPreference = false;
            
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(authState.User);
                hasUserPreference = user?.ThemePreference != null;
            }
            
            if (!hasUserPreference)
            {
                _currentTheme = !string.IsNullOrEmpty(savedTheme) ? savedTheme : "default";
                
                if (bool.TryParse(savedDarkMode, out var darkMode))
                {
                    _isDarkMode = darkMode;
                }
            }
            else
            {
                // Ensure localStorage matches database preference
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "app-theme", _currentTheme);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "app-dark-mode", _isDarkMode.ToString());
            }
        }
        catch
        {
            // JavaScript not available, keep server-side values
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        if (_currentTheme == theme) return;
        
        _currentTheme = theme;
        
        try
        {
            // Save to database for authenticated users
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(authState.User);
                if (user != null)
                {
                    var themePreference = SerializeThemePreference(_currentTheme, _isDarkMode);
                    user.ThemePreference = themePreference;
                    await _userManager.UpdateAsync(user);
                }
            }

            // Save to localStorage as backup/fallback
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "app-theme", theme);
            }
            catch
            {
                // JavaScript not available, skip localStorage
            }
            
            // Refresh the page to apply server-side theme
            _navigationManager.Refresh();
        }
        catch
        {
            // Ignore errors but still try to refresh
            _navigationManager.Refresh();
        }
    }

    public async Task ToggleDarkModeAsync()
    {
        _isDarkMode = !_isDarkMode;
        
        try
        {
            // Save to database for authenticated users
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(authState.User);
                if (user != null)
                {
                    var themePreference = SerializeThemePreference(_currentTheme, _isDarkMode);
                    user.ThemePreference = themePreference;
                    await _userManager.UpdateAsync(user);
                }
            }

            // Save to localStorage as backup/fallback
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "app-dark-mode", _isDarkMode.ToString());
            }
            catch
            {
                // JavaScript not available, skip localStorage
            }
            
            // Refresh the page to apply server-side theme
            _navigationManager.Refresh();
        }
        catch
        {
            // Ignore errors but still try to refresh
            _navigationManager.Refresh();
        }
    }

    private async Task ApplyThemeAsync()
    {
        try
        {
            var themeClass = "";
            
            // Only add theme class if it's not the default theme
            if (_currentTheme != "default")
            {
                themeClass = _currentTheme;
            }
            
            // Add dark mode class if enabled
            if (_isDarkMode)
            {
                if (!string.IsNullOrEmpty(themeClass))
                {
                    themeClass += " dark-mode";
                }
                else
                {
                    themeClass = "dark-mode";
                }
            }

            await _jsRuntime.InvokeVoidAsync("setRootTheme", themeClass);
        }
        catch
        {
            // Ignore JS errors
        }
    }

    private Task LoadUserThemeAsync(string themePreference)
    {
        try
        {
            var parts = themePreference.Split('|');
            if (parts.Length >= 1)
            {
                _currentTheme = parts[0];
            }
            if (parts.Length >= 2 && bool.TryParse(parts[1], out var darkMode))
            {
                _isDarkMode = darkMode;
            }
        }
        catch
        {
            // Fallback to defaults if parsing fails
            _currentTheme = "default";
            _isDarkMode = false;
        }
        
        return Task.CompletedTask;
    }

    private string SerializeThemePreference(string theme, bool isDarkMode)
    {
        return $"{theme}|{isDarkMode}";
    }

    public List<ThemeOption> GetAvailableThemes()
    {
        return new List<ThemeOption>
        {
            new("default", "Professional Neutral", "#475569"),
            new("executive-purple", "Executive Purple", "#782789"),
            new("sunset-rose", "Sunset Rose", "#ec4899"),
            new("trust-blue", "Trust Blue", "#1e40af"),
            new("growth-green", "Growth Green", "#16a34a"),
            new("innovation-orange", "Innovation Orange", "#ea580c"),
            new("midnight-teal", "Midnight Teal", "#0f766e"),
            new("heritage-burgundy", "Heritage Burgundy", "#be123c"),
            new("platinum-gray", "Platinum Gray", "#374151"),
            new("deep-navy", "Deep Navy", "#1e3a8a")
        };
    }
}

public record ThemeOption(string Value, string Name, string PrimaryColor);