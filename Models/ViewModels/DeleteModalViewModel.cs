namespace mvc_boilerplate.Models
{
    /// <summary>
    /// ViewModel for the shared DeleteModal component
    /// Provides configurable settings for delete confirmation dialogs
    /// </summary>
    public class DeleteModalViewModel
    {
        /// <summary>
        /// Unique identifier for the modal element
        /// </summary>
        public string ModalId { get; set; } = "deleteModal";

        /// <summary>
        /// Unique identifier for the delete form element
        /// </summary>
        public string FormId { get; set; } = "deleteForm";

        /// <summary>
        /// ID of the element that will display the item name/identifier
        /// </summary>
        public string ItemNameElementId { get; set; } = "deleteItemName";

        /// <summary>
        /// Title of the delete confirmation modal
        /// </summary>
        public string Title { get; set; } = "Delete Item";

        /// <summary>
        /// Message/content displayed in the modal
        /// Supports HTML content via @Html.Raw()
        /// </summary>
        public string Message { get; set; } = "Are you sure you want to delete this item? This action cannot be undone.";

        /// <summary>
        /// Text for the cancel button
        /// </summary>
        public string CancelButtonText { get; set; } = "Cancel";

        /// <summary>
        /// Text for the delete button
        /// </summary>
        public string DeleteButtonText { get; set; } = "Delete";

        /// <summary>
        /// The base action URL for the delete request (e.g., "/MasterForm/Delete")
        /// The item ID will be appended to this URL
        /// </summary>
        public string DeleteAction { get; set; } = "";
    }
}
