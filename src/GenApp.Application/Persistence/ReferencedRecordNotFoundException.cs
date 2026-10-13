namespace GenApp.Application.Persistence;

public sealed class ReferencedRecordNotFoundException(string message, Exception innerException)
    : Exception(message, innerException);
