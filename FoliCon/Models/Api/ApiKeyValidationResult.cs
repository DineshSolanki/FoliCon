namespace FoliCon.Models.Api;

public class ApiKeyValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; }
    public bool IsNetworkError { get; init; }

    public static ApiKeyValidationResult Success() => new() { IsValid = true, Message = "OK" };
    public static ApiKeyValidationResult Fail(string reason) => new() { IsValid = false, Message = reason, IsNetworkError = false };
    public static ApiKeyValidationResult NetworkError(string detail) => new() { IsValid = false, Message = detail, IsNetworkError = true };
}
