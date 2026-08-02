namespace LifeBalance.Notifications.Shared.Wrappers;

public class Response<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public Response()
    {
    }

    public Response(T data, string message = "Success")
    {
        Success = true;
        Message = message;
        Data = data;
    }

    public static Response<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
