namespace SaigonStays.API.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

public static class Constants
{
    public static class Roles
    {
        public const string Admin = "ADMIN";
        public const string Staff = "STAFF";
        public const string Guest = "GUEST";
    }

    public static class BookingStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string CheckedIn = "CHECKED_IN";
        public const string Cancelled = "CANCELLED";
    }

    public static class RoomStatus
    {
        public const string Available = "AVAILABLE";
        public const string Cleaning = "CLEANING";
        public const string Maintenance = "MAINTENANCE";
    }
}
