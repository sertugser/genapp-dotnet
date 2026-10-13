namespace GenApp.Application.Persistence;

public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : Exception(message, innerException);
