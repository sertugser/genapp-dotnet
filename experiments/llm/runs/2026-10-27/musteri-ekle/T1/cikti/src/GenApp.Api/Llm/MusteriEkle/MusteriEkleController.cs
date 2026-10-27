using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GenApp.Llm.MusteriEkle;

/// <summary>
/// Müşteri ekle (01ACUS). Eski programlar: LGACUS01 → LGACDB01 (+ LGACVS01, LGACDB02).
/// </summary>
[Route("api/customers")]
[Produces("application/json")]
public sealed class MusteriEkleController : ControllerBase
{
    private readonly MusteriEkleService _service;

    public MusteriEkleController(IConfiguration configuration, ILogger<MusteriEkleService> logger)
    {
        _service = new MusteriEkleService(configuration, logger);
    }

    [HttpPost("")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AddCustomer([FromBody] CustomerCreateRequest? request, CancellationToken cancellationToken)
    {
        // EIBCALEN / COMMAREA uzunluk kontrolünün (98) karşılığı: model doğrulaması (400 + errors)
        if (request is null && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "İstek gövdesi boş olamaz.");
        }

        if (!ModelState.IsValid || request is null)
        {
            return ValidationError(ModelState);
        }

        var result = await _service.AddAsync(request, cancellationToken);

        return result.Outcome switch
        {
            MusteriEkleOutcome.Success => Created($"/api/customers/{result.Customer!.CustomerNumber}", result.Customer),
            MusteriEkleOutcome.DatabaseBusy => Problem(
                StatusCodes.Status503ServiceUnavailable,
                "database-busy",
                "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı nedeniyle müşteri eklenemedi. Daha sonra tekrar deneyin.",
                "90"),
            _ => Problem(
                StatusCodes.Status500InternalServerError,
                "database-error",
                "Veritabanı hatası",
                "Müşteri eklenirken veritabanı hatası oluştu.",
                "90"),
        };
    }

    private ObjectResult ValidationError(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
            Detail = "İstekte eksik ya da hatalı alanlar var.",
        };

        var result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private static ObjectResult Problem(int status, string type, string title, string detail, string returnCode)
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
