namespace BlazorTemplate.Services
{
    /// <summary>
    /// Service for managing toast notifications across the application
    /// </summary>
    public interface IToastService
    {
        /// <summary>
        /// Event fired when a new toast notification should be displayed
        /// </summary>
        event Action<ToastMessage>? OnToastRequested;

        /// <summary>
        /// Shows a success toast notification
        /// </summary>
        /// <param name="title">Toast title</param>
        /// <param name="message">Toast message</param>
        /// <param name="duration">Duration in milliseconds (optional)</param>
        void ShowSuccess(string title, string message, int? duration = null);

        /// <summary>
        /// Shows an error toast notification
        /// </summary>
        /// <param name="title">Toast title</param>
        /// <param name="message">Toast message</param>
        /// <param name="duration">Duration in milliseconds (optional)</param>
        void ShowError(string title, string message, int? duration = null);

        /// <summary>
        /// Shows a warning toast notification
        /// </summary>
        /// <param name="title">Toast title</param>
        /// <param name="message">Toast message</param>
        /// <param name="duration">Duration in milliseconds (optional)</param>
        void ShowWarning(string title, string message, int? duration = null);

        /// <summary>
        /// Shows an info toast notification
        /// </summary>
        /// <param name="title">Toast title</param>
        /// <param name="message">Toast message</param>
        /// <param name="duration">Duration in milliseconds (optional)</param>
        void ShowInfo(string title, string message, int? duration = null);
    }

    /// <summary>
    /// Toast message data
    /// </summary>
    public class ToastMessage
    {
        public ToastType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Duration { get; set; }
    }

    /// <summary>
    /// Types of toast notifications
    /// </summary>
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }
}