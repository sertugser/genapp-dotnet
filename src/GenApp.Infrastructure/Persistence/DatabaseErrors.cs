using System.Data.Common;
using GenApp.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GenApp.Infrastructure.Persistence;

// Turns database failures into the Application's exception types (ADR 0001 decision 3).
internal static class DatabaseErrors
{
    // Runs one database call and turns its failure into the Application's exception types.
    public static async Task<T> RunAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception e) when (IsDatabaseError(e))
        {
            throw Translate(e);
        }
    }

    public static async Task RunAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception e) when (IsDatabaseError(e))
        {
            throw Translate(e);
        }
    }

    // EF Core wraps transient failures (deadlock, lost connection) in an InvalidOperationException, so the whole
    // chain is searched.
    public static bool IsDatabaseError(Exception e) => Chain(e).Any(c => c is DbException or DbUpdateException);

    public static DatabaseException Translate(Exception e) =>
        IsBusy(e)
            ? new DatabaseBusyException("The database is busy (deadlock or timeout).", e)
            : new DatabaseException("Database error.", e);

    // ADR 0002 decision 8: deadlock (40P01), lock timeout (55P03), statement timeout (57014) and Npgsql's own
    // command timeout. Each of these replaces Db2's SQLCODE -913.
    private static bool IsBusy(Exception e) =>
        Chain(e).Any(c => c
            is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.LockNotAvailable or PostgresErrorCodes.QueryCanceled }
            or NpgsqlException { InnerException: TimeoutException });

    private static IEnumerable<Exception> Chain(Exception e)
    {
        for (var current = e; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }
}
