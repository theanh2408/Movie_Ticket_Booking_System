using System.Text.Json.Serialization;

namespace Backend.Models;

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(bool success, T? data, string message, object? errors = null)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
    }
}

public class ApiResponse : ApiResponse<object>
{
    public ApiResponse()
    {
    }

    public ApiResponse(bool success, object? data, string message, object? errors = null)
        : base(success, data, message, errors)
    {
    }

    public static ApiResponse<T> Ok<T>(T? data, string message = "Thao tác thành công")
    {
        return new ApiResponse<T>(true, data, message, null);
    }

    public static ApiResponse Ok(object? data = null, string message = "Thao tác thành công")
    {
        return new ApiResponse(true, data, message, null);
    }

    public static ApiResponse<T> Fail<T>(string message, object? errors = null, T? data = default)
    {
        return new ApiResponse<T>(false, data, message, errors ?? Array.Empty<object>());
    }

    public static ApiResponse Fail(string message, object? errors = null)
    {
        return new ApiResponse(false, null, message, errors ?? Array.Empty<object>());
    }
}
