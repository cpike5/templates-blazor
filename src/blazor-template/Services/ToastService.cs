using System.Diagnostics;

namespace BlazorTemplate.Services
{
    /// <summary>
    /// Service implementation for managing toast notifications
    /// </summary>
    public class ToastService : IToastService
    {
        private readonly ILogger<ToastService> _logger;
        private static int _toastCounter = 0;
        
        public event Action<ToastMessage>? OnToastRequested;
        
        public ToastService(ILogger<ToastService> logger)
        {
            _logger = logger;
            _logger.LogDebug("ToastService initialized");
        }

        /// <summary>
        /// Shows a success toast notification
        /// </summary>
        /// <param name='title'>Toast title</param>
        /// <param name='message'>Toast message content</param>
        /// <param name='duration'>Duration in milliseconds (optional, defaults to 5000ms)</param>
        public void ShowSuccess(string title, string message, int? duration = null)
        {
            var actualDuration = duration ?? 5000;
            _logger.LogInformation("Showing success toast: {Title} - {Message} (Duration: {Duration}ms)",
                title, message, actualDuration);
            ShowToast(ToastType.Success, title, message, actualDuration);
        }

        /// <summary>
        /// Shows an error toast notification
        /// </summary>
        /// <param name='title'>Toast title</param>
        /// <param name='message'>Toast message content</param>
        /// <param name='duration'>Duration in milliseconds (optional, defaults to 8000ms)</param>
        public void ShowError(string title, string message, int? duration = null)
        {
            var actualDuration = duration ?? 8000;
            _logger.LogWarning("Showing error toast: {Title} - {Message} (Duration: {Duration}ms)",
                title, message, actualDuration);
            ShowToast(ToastType.Error, title, message, actualDuration);
        }

        /// <summary>
        /// Shows a warning toast notification
        /// </summary>
        /// <param name='title'>Toast title</param>
        /// <param name='message'>Toast message content</param>
        /// <param name='duration'>Duration in milliseconds (optional, defaults to 6000ms)</param>
        public void ShowWarning(string title, string message, int? duration = null)
        {
            var actualDuration = duration ?? 6000;
            _logger.LogInformation("Showing warning toast: {Title} - {Message} (Duration: {Duration}ms)",
                title, message, actualDuration);
            ShowToast(ToastType.Warning, title, message, actualDuration);
        }

        /// <summary>
        /// Shows an info toast notification
        /// </summary>
        /// <param name='title'>Toast title</param>
        /// <param name='message'>Toast message content</param>
        /// <param name='duration'>Duration in milliseconds (optional, defaults to 5000ms)</param>
        public void ShowInfo(string title, string message, int? duration = null)
        {
            var actualDuration = duration ?? 5000;
            _logger.LogInformation("Showing info toast: {Title} - {Message} (Duration: {Duration}ms)",
                title, message, actualDuration);
            ShowToast(ToastType.Info, title, message, actualDuration);
        }

        /// <summary>
        /// Internal method to create and dispatch toast notifications
        /// </summary>
        /// <param name='type'>Type of toast notification</param>
        /// <param name='title'>Toast title</param>
        /// <param name='message'>Toast message content</param>
        /// <param name='duration'>Duration in milliseconds</param>
        private void ShowToast(ToastType type, string title, string message, int duration)
        {
            var toastId = Interlocked.Increment(ref _toastCounter);
            
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(title))
                {
                    _logger.LogWarning("Toast notification #{ToastId} has empty title, using default", toastId);
                    title = "Notification";
                }
                
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Toast notification #{ToastId} has empty message", toastId);
                    message = "No message provided";
                }
                
                if (duration <= 0)
                {
                    _logger.LogWarning("Toast notification #{ToastId} has invalid duration {Duration}ms, using default", toastId, duration);
                    duration = 5000;
                }
                
                var toastMessage = new ToastMessage
                {
                    Type = type,
                    Title = title,
                    Message = message,
                    Duration = duration
                };
                
                var subscriberCount = OnToastRequested?.GetInvocationList().Length ?? 0;
                _logger.LogTrace("Dispatching toast notification #{ToastId} ({Type}) to {SubscriberCount} subscribers",
                    toastId, type, subscriberCount);
                
                if (subscriberCount == 0)
                {
                    _logger.LogWarning("Toast notification #{ToastId} dispatched but no subscribers are listening", toastId);
                }
                
                OnToastRequested?.Invoke(toastMessage);
                
                _logger.LogDebug("Toast notification #{ToastId} ({Type}: {Title}) successfully dispatched", 
                    toastId, type, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch toast notification #{ToastId} ({Type}: {Title})", 
                    toastId, type, title);
                throw;
            }
        }
    }
}