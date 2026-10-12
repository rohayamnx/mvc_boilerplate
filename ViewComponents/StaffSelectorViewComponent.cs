using Microsoft.AspNetCore.Mvc;
using mvc_boilerplate.Models.UI;

namespace mvc_boilerplate.ViewComponents
{
    /// <summary>
    /// Reusable staff selector component with search functionality.
    /// Renders a searchable text input + dropdown that reads staff data
    /// from the server-rendered inline JSON (no /api/staffapi call).
    /// </summary>
    public class StaffSelectorViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(StaffSelectorModel model)
        {
            // Set default values if not provided
            model ??= new StaffSelectorModel();

            if (string.IsNullOrEmpty(model.InstanceId))
            {
                model.InstanceId = Guid.NewGuid().ToString("N")[..8]; // Short unique ID
            }

            return View(model);
        }
    }
}