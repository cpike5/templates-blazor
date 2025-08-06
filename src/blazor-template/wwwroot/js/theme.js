window.setRootTheme = (themeClass) => {
    const root = document.documentElement;
    
    // List of all theme classes to remove
    const themeClasses = [
        'executive-purple', 'sunset-rose', 'trust-blue', 'growth-green',
        'innovation-orange', 'midnight-teal', 'heritage-burgundy', 
        'platinum-gray', 'deep-navy', 'dark-mode'
    ];
    
    // Remove all existing theme classes
    themeClasses.forEach(cls => root.classList.remove(cls));
    
    // Apply new theme classes if provided
    if (themeClass && themeClass.trim()) {
        const classes = themeClass.trim().split(' ');
        classes.forEach(cls => {
            if (cls.trim()) {
                root.classList.add(cls.trim());
            }
        });
    }
};