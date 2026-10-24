namespace EventBE.Api.Common;

public enum ServiceErrorType
{
    None,
    ValidationError,
    NotFound,
    Conflict
}

/// <summary>
/// Encapsulates the outcome of a service operation to decouple business rules from HTTP transport.
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string? ErrorMessage { get; private init; }
    public ServiceErrorType ErrorType { get; private init; }

    public static ServiceResult<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value,
        ErrorType = ServiceErrorType.None
    };

    public static ServiceResult<T> Failure(string errorMessage, ServiceErrorType errorType) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorType = errorType
    };
}

