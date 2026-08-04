namespace webShopping.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? Path { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }
        public bool ShowDetails { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
