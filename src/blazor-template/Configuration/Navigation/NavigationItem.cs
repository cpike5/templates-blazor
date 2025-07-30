namespace BlazorTemplate.Configuration.Navigation
{
    public class NavigationItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Href { get; set; }
        public string Icon { get; set; } = "fas fa-circle";
        public List<string> RequiredRoles { get; set; } = new();
        public List<NavigationItem> Children { get; set; } = new();
        public bool IsGroup => Children.Any();
        public bool IsVisible { get; set; } = true;
        public int Order { get; set; } = 0;
    }
}
