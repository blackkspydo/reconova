namespace Reconova.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public Dictionary<string, object>? Meta { get; set; }

    public static ApiResponse<T> Ok(T data, Dictionary<string, object>? meta = null)
        => new() { Success = true, Data = data, Meta = meta };

    public static ApiResponse<T> Fail(string code, string message)
        => new() { Success = false, Error = new ApiError { Code = code, Message = message } };
}

public class ApiResponse
{
    public bool Success { get; set; }
    public ApiError? Error { get; set; }

    public static ApiResponse Ok() => new() { Success = true };
    public static ApiResponse Fail(string code, string message)
        => new() { Success = false, Error = new ApiError { Code = code, Message = message } };
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Details { get; set; }
}
