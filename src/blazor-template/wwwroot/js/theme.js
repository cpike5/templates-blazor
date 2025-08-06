// Simple theme manager for client-side theme switching
window.themeManager = {
    themeClasses: [
        'executive-purple', 'sunset-rose', 'trust-blue', 'growth-green',
        'innovation-orange', 'midnight-teal', 'heritage-burgundy', 
        'platinum-gray', 'deep-navy', 'dark-mode'
    ],
    
    setRootTheme: (themeClass) => {
        const root = document.documentElement;
        
        // Remove all existing theme classes
        window.themeManager.themeClasses.forEach(cls => root.classList.remove(cls));
        
        // Apply new theme classes if provided
        if (themeClass && themeClass.trim()) {
            const classes = themeClass.trim().split(' ');
            classes.forEach(cls => {
                if (cls.trim()) {
                    root.classList.add(cls.trim());
                }
            });
        }
    }
};

// Backward compatibility
window.setRootTheme = window.themeManager.setRootTheme;