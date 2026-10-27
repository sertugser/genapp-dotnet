using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri sorgula (LGICUS01 / LGICDB01, istek kodu 01ICUS).
/// </summary>
[ApiController]
public sealed class MusteriSorgulaController : ControllerBase
{
    private readonly MusteriSorgulaService _service;

    public MusteriSorgulaController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var repository = new MusteriSorgulaRepository(
            configuration,
            loggerFactory.CreateLogger<MusteriSorgulaRepository>());
        _service = new MusteriSorgulaService(repository);
    }

    // Tam sayı sınırını aşan numara :int kısıtıyla eşleşmez ve ASP.NET Core'un 404'ü döner (API tasarımı, bilinçli sapma).
    [HttpGet("/api/customers/{customerNumber:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MusteriSorgulaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Get(int customerNumber, CancellationToken cancellationToken)
    {
        var result = await _service.InquireAsync(customerNumber, cancellationToken);

        if (result.ReturnCode == ReturnCodes.Success && result.Customer is not null)
        {
            return Ok(result.Customer);
        }

        if (result.ReturnCode == ReturnCodes.NotFound)
        {
            return ProblemResult(
                StatusCodes.Status404NotFound,
                "not-found",
                "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.",
                ReturnCodes.NotFound);
        }

        if (result.DatabaseBusy)
        {
            return ProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "database-busy",
                "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu. Lütfen daha sonra tekrar deneyin.",
                ReturnCodes.DatabaseError);
        }

        return ProblemResult(
            StatusCodes.Status500InternalServerError,
            "database-error",
            "Veritabanı hatası",
            $"{customerNumber} numaralı müşteri sorgulanırken veritabanı hatası oluştu.",
            ReturnCodes.DatabaseError);
    }

    private static ObjectResult ProblemResult(int status, string type, string title, string detail, string returnCode)
    {
        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
        };
        problem.Extensions["returnCode"] = returnCode;

        var result = new ObjectResult(problem) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
