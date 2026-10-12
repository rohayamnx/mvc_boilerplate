namespace mvc_boilerplate.Models.UI;

/// <summary>
/// Represents a single breadcrumb navigation item.
/// </summary>
public class BreadcrumbItem
{
    /// <summary>The display label of the breadcrumb.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional URL. When null or empty the item is rendered as plain text
    /// (typically the last / active crumb).
    /// </summary>
    public string? Url { get; set; }

    public BreadcrumbItem() { }

    public BreadcrumbItem(string label, string? url = null)
    {
        Label = label;
        Url  = url;
    }
}
