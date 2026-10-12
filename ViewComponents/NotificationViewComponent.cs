using Microsoft.AspNetCore.Mvc;

namespace mvc_boilerplate.ViewComponents
{
    /// <summary>
    /// ViewComponent for rendering the shared global notification (toast) system.
    /// Invoke once from the layout:
    ///   @await Component.InvokeAsync("Notification")
    /// Then show toasts from any JS via the global helpers:
    ///   showSuccess(message), showError(message), showWarning(message),
    ///   showInfo(message), showNotification(message, type, duration).
    /// Types: 'success' | 'error' | 'warning' | 'info'.
    /// </summary>
    public class NotificationViewComponent : ViewComponent
    {
        /// <summary>
        /// Renders the notification container and registers the global
        /// NotificationSystem + helper functions.
        /// </summary>
        public IViewComponentResult Invoke()
        {
            return View("~/Views/Shared/Components/Notification/Default.cshtml");
        }
    }
}