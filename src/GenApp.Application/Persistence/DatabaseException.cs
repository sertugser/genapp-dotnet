namespace GenApp.Application.Persistence;

// Any other database failure: the legacy data programs return 90 for every unexpected SQLCODE
// (e.g. lgicdb01.cbl:199-202). Infrastructure throws it so no EF Core or Npgsql type leaks upwards (ADR 0001).
public class DatabaseException(string message, Exception innerException) : Exception(message, innerException);

// Deadlock or timeout. ADR 0002 decision 8: 503 with return code 90 in every operation.
public sealed class DatabaseBusyException(string message, Exception innerException)
    : DatabaseException(message, innerException);
