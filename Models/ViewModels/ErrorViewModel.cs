namespace mvc_boilerplate.Models.ViewModels
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string? ErrorMessage { get; set; }

        public bool ShowErrorMessage => !string.IsNullOrEmpty(ErrorMessage);

        public int? StatusCode { get; set; }

        public string GetErrorType() => StatusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => "Error"
        };

        public string GetErrorDescription() => StatusCode switch
        {
            400 => "The request was invalid or malformed.",
            401 or 403 => "You do not have permission to access this resource. Please contact your administrator if you believe this is an error.",
            404 => "The page you are looking for might have been removed, had its name changed, or is temporarily unavailable.",
            500 => "We encountered an unexpected error while processing your request. We're working to fix this issue.",
            502 => "There was a problem communicating with the server.",
            503 => "The service is temporarily unavailable due to scheduled maintenance or high server load. We apologize for the inconvenience.",
            _ => "We're sorry, something went wrong. Please try again later or contact support if the problem persists."
        };

        public string GetErrorDetailsTitle() => StatusCode switch
        {
            404 => "Possible Reasons",
            401 or 403 => "What You Can Do",
            500 => "Recommended Actions",
            503 => "Information",
            _ => "What happened?"
        };
    }
}
