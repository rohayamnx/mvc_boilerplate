using System.ComponentModel.DataAnnotations;

namespace mvc_boilerplate.Models.UI
{
    /// <summary>
    /// Model for the StaffSelector ViewComponent.
    /// Mirrors the iform_hra StaffSelectorModel pattern — supports field prefixes
    /// for array binding (e.g., Participants[0].StaffId) and optional related fields
    /// (Department, Designation, ContactNo, StaffId display).
    /// </summary>
    public class StaffSelectorModel
    {
        /// <summary>
        /// Base name for form inputs (e.g., "Participants", "Accompanies", or empty for direct binding)
        /// </summary>
        public string FieldPrefix { get; set; } = "";

        /// <summary>
        /// Index for array-based form fields (e.g., 1 for Participants[1])
        /// </summary>
        public int? Index { get; set; }

        /// <summary>
        /// Unique identifier for this staff selector instance (used for DOM element IDs)
        /// </summary>
        public string InstanceId { get; set; } = "";

        /// <summary>
        /// Label text to display above the search input
        /// </summary>
        public string Label { get; set; } = "Choose Staff";

        /// <summary>
        /// Whether this field is required (shows asterisk)
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Whether the field is read-only
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Current selected staff ID (can be int or string)
        /// </summary>
        public object? StaffId { get; set; }

        /// <summary>
        /// Get StaffId as string for form binding
        /// </summary>
        public string? StaffIdString => StaffId?.ToString();

        /// <summary>
        /// Current selected staff name
        /// </summary>
        public string? StaffName { get; set; }

        /// <summary>
        /// Current department ID
        /// </summary>
        public string? DepartmentId { get; set; }

        /// <summary>
        /// Current department name
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Whether to show department field
        /// </summary>
        public bool ShowDepartment { get; set; } = false;

        /// <summary>
        /// Whether to show staff ID field as a visible input
        /// </summary>
        public bool ShowStaffId { get; set; } = false;

        /// <summary>
        /// Placeholder text for the search input
        /// </summary>
        public string Placeholder { get; set; } = "Type to search by name or staff ID...";

        /// <summary>
        /// Generate the base field name for form inputs
        /// </summary>
        public string GetFieldName(string fieldSuffix)
        {
            if (string.IsNullOrEmpty(FieldPrefix))
            {
                return fieldSuffix; // Direct binding (e.g., "StaffId")
            }

            if (Index.HasValue)
            {
                return $"{FieldPrefix}[{Index}].{fieldSuffix}"; // Array binding (e.g., "Participants[1].StaffId")
            }

            return $"{FieldPrefix}.{fieldSuffix}"; // Object binding (e.g., "Staff.StaffId")
        }

        /// <summary>
        /// Generate DOM element ID
        /// </summary>
        public string GetElementId(string suffix)
        {
            return string.IsNullOrEmpty(InstanceId) ? suffix : $"{suffix}_{InstanceId}";
        }
    }
}