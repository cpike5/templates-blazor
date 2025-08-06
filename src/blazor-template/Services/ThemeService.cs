using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using BlazorTemplate.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace BlazorTemplate.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<ThemeService> _logger;
    private string _currentTheme = "default";
    private bool _isDarkMode = false;
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private TaskCompletionSource<bool>? _initializationTask;
    private readonly Dictionary<string, string> _themeClassMap;
    private static readonly Regex _validCssClassRegex = new(@"^[a-zA-Z_][\w\-]*$", RegexOptions.Compiled);

    public ThemeService(IJSRuntime jsRuntime, AuthenticationStateProvider authStateProvider, UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, NavigationManager navigationManager, ILogger<ThemeService> logger)
    {
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _userManager = userManager;
        _dbContext = dbContext;
        _navigationManager = navigationManager;
        _logger = logger;
        _themeClassMap = InitializeThemeMapping();
    }

    public event Action? OnThemeChanged;

    public string CurrentTheme => _currentTheme;
    public bool IsDarkMode => _isDarkMode;

    /// <summary>
    /// Initializes the mapping between theme names and their corresponding CSS classes.
    /// </summary>
    private Dictionary<string, string> InitializeThemeMapping()
    {
        return new Dictionary<string, string>
        {
            { "default", "" }, // No class for default theme
            { "executive-purple", "executive-purple" },
            { "sunset-rose", "sunset-rose" },
            { "trust-blue", "trust-blue" },
            { "growth-green", "growth-green" },
            { "innovation-orange", "innovation-orange" },
            { "midnight-teal", "midnight-teal" },
            { "heritage-burgundy", "heritage-burgundy" },
            { "platinum-gray", "platinum-gray" },
            { "deep-navy", "deep-navy" },
            { "financial-blue", "financial-blue" },
            { "tech-slate", "tech-slate" },
            { "medical-teal", "medical-teal" },
            { "executive-charcoal", "executive-charcoal" }
        };
    }

    /// <summary>
    /// Gets the CSS class string that should be applied to the document root for server-side rendering.
    /// Validates CSS class names to prevent XSS attacks.
    /// </summary>
    public string GetThemeClassForRendering()
    {
        var classes = new List<string>();
        
        // Add theme class if not default
        if (!string.IsNullOrEmpty(_currentTheme) && _currentTheme != "default")
        {
            if (_themeClassMap.TryGetValue(_currentTheme, out var themeClass) && !string.IsNullOrEmpty(themeClass))
            {
                // Validate CSS class name to prevent XSS
                if (IsValidCssClassName(themeClass))
                {
                    classes.Add(themeClass);
                }
                else
                {
                    _logger.LogWarning("Invalid CSS class name detected and filtered: {ThemeClass}", themeClass);
                }
            }
        }
        
        // Add dark mode class if enabled (always safe since it's hardcoded)
        if (_isDarkMode)
        {
            classes.Add("dark-mode");
        }
        
        return string.Join(" ", classes);
    }

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
                    await LoadUserThemeAsync(user.ThemePreference);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load theme from database, using defaults");
        }
        
        // Set defaults if no user preference found (thread-safe)
        await _stateLock.WaitAsync();
        try
        {
            _currentTheme = "default";
            _isDarkMode = false;
        }
        finally
        {
            _stateLock.Release();
        }
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
                // Thread-safe state updates from localStorage
                await _stateLock.WaitAsync();
                try
                {
                    _currentTheme = !string.IsNullOrEmpty(savedTheme) ? savedTheme : "default";
                    
                    if (bool.TryParse(savedDarkMode, out var darkMode))
                    {
                        _isDarkMode = darkMode;
                    }
                }
                finally
                {
                    _stateLock.Release();
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

    /// <summary>
    /// Sets the current theme after validating it exists in the available themes.
    /// </summary>
    /// <param name="theme">The theme identifier to set</param>
    public async Task SetThemeAsync(string theme)
    {
        // Validate input
        if (string.IsNullOrEmpty(theme))
        {
            _logger.LogWarning("Attempted to set null or empty theme, ignoring request");
            return;
        }
        
        if (!_themeClassMap.ContainsKey(theme))
        {
            _logger.LogWarning("Attempted to set invalid theme: {Theme}. Available themes: {AvailableThemes}", theme, string.Join(", ", _themeClassMap.Keys));
            return;
        }
        
        if (_currentTheme == theme) return;
        
        await _stateLock.WaitAsync();
        try
        {
            _currentTheme = theme;
        }
        finally
        {
            _stateLock.Release();
        }
        
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
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to save theme to localStorage, JavaScript may not be available");
            }
            
            // Refresh the page to apply server-side theme
            _navigationManager.Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save theme preference for theme: {Theme}", theme);
            _navigationManager.Refresh();
        }
    }

    /// <summary>
    /// Toggles dark mode on or off with thread safety.
    /// </summary>
    public async Task ToggleDarkModeAsync()
    {
        await _stateLock.WaitAsync();
        try
        {
            _isDarkMode = !_isDarkMode;
        }
        finally
        {
            _stateLock.Release();
        }
        
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
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to save dark mode preference to localStorage, JavaScript may not be available");
            }
            
            // Refresh the page to apply server-side theme
            _navigationManager.Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save dark mode preference, isDarkMode: {IsDarkMode}", _isDarkMode);
            _navigationManager.Refresh();
        }
    }

    private async Task ApplyThemeAsync()
    {
        try
        {
            // Get the complete CSS class string for this theme configuration
            var themeClasses = GetThemeClassForRendering();
            
            // Use the improved JavaScript function to apply theme classes
            await _jsRuntime.InvokeVoidAsync("themeManager.setRootTheme", themeClasses);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Modern theme manager not available, trying legacy function");
            try
            {
                await _jsRuntime.InvokeVoidAsync("setRootTheme", GetThemeClassForRendering());
            }
            catch (Exception legacyEx)
            {
                _logger.LogDebug(legacyEx, "Legacy theme function also failed, theme may not apply on client-side");
            }
        }
    }

    private async Task LoadUserThemeAsync(string themePreference)
    {
        await _stateLock.WaitAsync();
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse theme preference: {ThemePreference}, using defaults", themePreference);
            _currentTheme = "default";
            _isDarkMode = false;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    /// <summary>
    /// Validates that a CSS class name follows safe naming conventions to prevent XSS.
    /// </summary>
    /// <param name="className">The CSS class name to validate</param>
    /// <returns>True if the class name is valid and safe</returns>
    private bool IsValidCssClassName(string className)
    {
        if (string.IsNullOrEmpty(className))
            return false;
            
        return _validCssClassRegex.IsMatch(className);
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
            new("financial-blue", "Corporate Finance", "#1a365d"),
            new("tech-slate", "Modern Tech", "#475569"),
            new("medical-teal", "Healthcare Professional", "#0d9488"),
            new("executive-charcoal", "Executive Consulting", "#374151"),
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