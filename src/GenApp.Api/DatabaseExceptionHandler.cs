using GenApp.Application.Persistence;
using Microsoft.AspNetCore.Diagnostics;

namespace GenApp.Api;

// Database failures reach the Api as exceptions from any operation: 503 for deadlock and timeout
// (ADR 0002 decision 8), otherwise 500 with return code 90, as the legacy data programs return for any other SQLCODE.
internal sealed class DatabaseExceptionHandler(ILogger<DatabaseExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DatabaseException)
        {
            return false;
        }

        logger.LogError(exception, "Database failure");

        var problem = exception is DatabaseBusyException
            ? ReturnCodeProblems.DatabaseBusy()
            : ReturnCodeProblems.For(Application.ReturnCodes.DatabaseError);

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}
