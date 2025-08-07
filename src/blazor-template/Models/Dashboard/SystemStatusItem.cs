namespace BlazorTemplate.Models.Dashboard
{
    /// <summary>
    /// Represents a system status item for monitoring various system components.
    /// </summary>
    public class SystemStatusItem
    {
        /// <summary>
        /// Name of the system component being monitored.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description providing details about the component status.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Current status of the system component.
        /// </summary>
        public SystemStatus Status { get; set; }
        
        /// <summary>
        /// Icon class (e.g., FontAwesome) to display for this component.
        /// </summary>
        public string Icon { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional metadata about the system component status.
        /// </summary>
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
    
    /// <summary>
    /// Enumeration of possible system status values.
    /// </summary>
    public enum SystemStatus
    {
        /// <summary>
        /// System component is operating normally.
        /// </summary>
        Healthy,
        
        /// <summary>
        /// System component has warnings but is still functional.
        /// </summary>
        Warning, 
        
        /// <summary>
        /// System component has errors or is not functioning.
        /// </summary>
        Error,
        
        /// <summary>
        /// System component status cannot be determined.
        /// </summary>
        Unknown
    }
}