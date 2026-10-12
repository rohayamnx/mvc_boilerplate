using Microsoft.AspNetCore.Mvc;
using mvc_boilerplate.Models;

namespace mvc_boilerplate.ViewComponents
{
    /// <summary>
    /// ViewComponent for rendering a reusable delete confirmation modal.
    /// Invoke from any view:
    ///   @await Component.InvokeAsync("DeleteModal", new DeleteModalViewModel {
    ///       ModalId = "deleteXModal",
    ///       FormId = "deleteXForm",
    ///       Title = "Delete X",
    ///       Message = "Are you sure you want to delete <strong id='xName'></strong>?",
    ///       DeleteAction = Url.Action("Delete", "X")!,
    ///       ItemNameElementId = "xName"
    ///   })
    /// Then open it from JS via the generated openDeleteModal_&lt;ModalId&gt;(itemId, itemName).
    /// </summary>
    public class DeleteModalViewComponent : ViewComponent
    {
        /// <summary>
        /// Invokes the DeleteModal component with the provided configuration.
        /// </summary>
        /// <param name="model">Configuration model for the delete modal</param>
        /// <returns>The rendered view component</returns>
        public IViewComponentResult Invoke(DeleteModalViewModel model)
        {
            return View("~/Views/Shared/Components/DeleteModal/Default.cshtml", model);
        }
    }
}