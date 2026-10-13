using GenApp.Application;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api;

// The single place where legacy return codes become HTTP responses (ADR 0002, docs/api-design.md section 3).
// Errors are RFC 9457 Problem Details carrying the legacy code in "returnCode".
public static class ReturnCodeProblems
{
    private sealed record Mapping(int Status, string Type, string Title);

    private static readonly Dictionary<string, Mapping> Mappings = new()
    {
        [ReturnCodes.NotFound] = new(StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı"),
        [ReturnCodes.ConcurrencyConflict] = new(StatusCodes.Status409Conflict, "concurrency-conflict", "Kayıt başkası tarafından değiştirilmiş"),
        [ReturnCodes.CustomerNotFound] = new(StatusCodes.Status404NotFound, "customer-not-found", "Müşteri bulunamadı"),
        ["80"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası"),
        ["81"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası"),
        ["82"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası"),
        ["88"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası"),
        ["89"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası"),
        [ReturnCodes.DatabaseError] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası"),
    };

    public static ProblemDetails For(string returnCode, string? detail = null)
    {
        if (!Mappings.TryGetValue(returnCode, out var mapping))
        {
            throw new ArgumentOutOfRangeException(nameof(returnCode), returnCode, "No HTTP mapping for this return code.");
        }

        return Create(mapping.Status, mapping.Type, mapping.Title, returnCode, detail);
    }

    // ADR 0002 decision 8: deadlock and timeout in every operation.
    public static ProblemDetails DatabaseBusy() =>
        Create(StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul", ReturnCodes.DatabaseError,
            "Veritabanı şu an meşgul, isteği daha sonra yeniden deneyin.");

    public static IActionResult ToResult(this ProblemDetails problem) =>
        new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" },
        };

    private static ProblemDetails Create(int status, string type, string title, string returnCode, string? detail)
    {
        var problem = new ProblemDetails { Type = type, Title = title, Status = status, Detail = detail };
        problem.Extensions["returnCode"] = returnCode;
        return problem;
    }
}
