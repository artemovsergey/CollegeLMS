using System.Text.Json.Serialization;

namespace CollegeLMS.API.Response;

public class Result<T>
{
    [JsonInclude]
    public bool IsSuccess { get; private set; }

    [JsonInclude]
    public T? Data { get; private set; }

    [JsonInclude]
    public string? ErrorMessage { get; private set; }

    [JsonInclude]
    public int StatusCode { get; private set; }

    public Result() { }

    public static Result<T> Ok(T data) =>
        new()
        {
            IsSuccess = true,
            Data = data,
            StatusCode = 200,
        };

    public static Result<T> Fail(string error, int statusCode = 400) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = error,
            StatusCode = statusCode,
        };

    public static implicit operator Result<T>(T data) => Ok(data);
}

public class Result
{
    [JsonInclude]
    public bool IsSuccess { get; private set; }

    [JsonInclude]
    public string? ErrorMessage { get; private set; }

    [JsonInclude]
    public int StatusCode { get; private set; }

    public Result() { }

    public static Result Ok() => new() { IsSuccess = true, StatusCode = 200 };

    public static Result Fail(string error, int statusCode = 400) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = error,
            StatusCode = statusCode,
        };
}
