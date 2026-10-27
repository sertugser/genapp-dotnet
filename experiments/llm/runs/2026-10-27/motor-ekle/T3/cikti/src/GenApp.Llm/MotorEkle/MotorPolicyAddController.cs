using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MotorEkle;

// [ApiController] bilinçli olarak kullanılmadı: otomatik 400 cevabı yerine "validation-error" türlü gövde üretmek için
// model doğrulaması aksiyon içinde yapılır (Program sınıfına ve diğer operasyonlara dokunmadan).
[Route("api/customers/{customerNumber:int}/motor-policies")]
public sealed class MotorPolicyAddController : ControllerBase
{
    private readonly MotorPolicyAddService _service;

    public MotorPolicyAddController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
        var repository = new MotorPolicyAddRepository(connectionString);
        _service = new MotorPolicyAddService(repository, loggerFactory.CreateLogger<MotorPolicyAddService>());
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MotorPolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AddAsync(
        [FromRoute] int customerNumber,
        [FromBody] MotorPolicyAddRequest? request,
        CancellationToken cancellationToken)
    {
        // EIBCALEN / '98' karşılığı: eksik ya da hatalı alan içeren istek 400 ile reddedilir.
        if (!ModelState.IsValid || request is null)
        {
            if (request is null && ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "İstek gövdesi boş olamaz.");
            }
            return ValidationError(ModelState);
        }

        var result = await _service.AddAsync(customerNumber, request, cancellationToken);

        if (result.ReturnCode == ReturnCodes.Success && result.Policy is not null)
        {
            var location = $"/api/customers/{customerNumber}/motor-policies/{result.Policy.PolicyNumber}";
            return Created(location, result.Policy);
        }

        if (result.DatabaseBusy)
        {
            return Problem(StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı nedeniyle işlem yapılamadı. Lütfen tekrar deneyin.",
                ReturnCodes.DatabaseError);
        }

        return result.ReturnCode switch
        {
            ReturnCodes.CustomerNotFound => Problem(StatusCodes.Status404NotFound, "customer-not-found",
                "Müşteri bulunamadı", $"{customerNumber} numaralı müşteri bulunamadı.", ReturnCodes.CustomerNotFound),
            _ => Problem(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası",
                "Poliçe eklenirken veritabanı hatası oluştu.", ReturnCodes.DatabaseError),
        };
    }

    private static ObjectResult ValidationError(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
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
