namespace Auth.Shared.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public int StatusCode { get; set; } = 200;

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation successful.", int statusCode = 200)
    {
        return new ApiResponse<T> { Success = true, Message = message, Data = data, StatusCode = statusCode };
    }

    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null, int statusCode = 400)
    {
        return new ApiResponse<T> { Success = false, Message = message, Errors = errors ?? new List<string>(), StatusCode = statusCode };
    }
}
