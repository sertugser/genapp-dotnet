namespace GenApp.Application;

// What a service returns: the legacy return code and, when it is 00, the value.
public sealed record OperationResult<T>(string ReturnCode, T? Value)
{
    public bool IsSuccess => ReturnCode == ReturnCodes.Success;

    public static OperationResult<T> Success(T value) => new(ReturnCodes.Success, value);

    public static OperationResult<T> Failure(string returnCode) => new(returnCode, default);
}
