using Microsoft.JSInterop;

namespace BlazorTemplate.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = "default";
    private bool _isDarkMode = false;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public event Action? OnThemeChanged;

    public string CurrentTheme => _currentTheme;
    public bool IsDarkMode => _isDarkMode;

    public async Task InitializeAsync(bool afterRender = false)
    {
        if (!afterRender)
        {
            return;
        }

        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "app-theme");
            var savedDarkMode = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "app-dark-mode");
            
            // Always update internal state from localStorage to ensure consistency
            _currentTheme = !string.IsNullOrEmpty(savedTheme) ? savedTheme : "default";
            
            if (bool.TryParse(savedDarkMode, out var darkMode))
            {
                _isDarkMode = darkMode;
            }
            else
            {
                _isDarkMode = false;
            }

            // Always apply the theme to ensure UI reflects localStorage state
            await ApplyThemeAsync();
            
            // Notify components of the current theme state
            OnThemeChanged?.Invoke();
        }
        catch
        {
            // Fallback to default theme if localStorage is not available
            _currentTheme = "default";
            _isDarkMode = false;
            await ApplyThemeAsync();
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        if (_currentTheme == theme) return;
        
        _currentTheme = theme;
        
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "app-theme", theme);
            await ApplyThemeAsync();
            
            OnThemeChanged?.Invoke();
        }
        catch
        {
            // Ignore JS errors
        }
    }

    public async Task ToggleDarkModeAsync()
    {
        _isDarkMode = !_isDarkMode;
        
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "app-dark-mode", _isDarkMode.ToString());
        await ApplyThemeAsync();
        
        OnThemeChanged?.Invoke();
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