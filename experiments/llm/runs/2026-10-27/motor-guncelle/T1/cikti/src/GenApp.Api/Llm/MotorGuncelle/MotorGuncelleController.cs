using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MotorGuncelle;

/// <summary>
/// Motor poliçesi güncelle (eski: LGUPOL01 + LGUPDB01, istek kodu 01UMOT).
/// </summary>
[Route("api/customers/{customerNumber:int}/motor-policies/{policyNumber:int}")]
[Produces("application/json")]
public sealed class MotorGuncelleController : ControllerBase
{
    private readonly MotorGuncelleService _service;

    public MotorGuncelleController(IConfiguration configuration, ILogger<MotorGuncelleService> logger)
    {
        var connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
        _service = new MotorGuncelleService(connectionString, logger);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromRoute] int customerNumber,
        [FromRoute] int policyNumber,
        [FromBody] MotorPolicyUpdateRequest? request,
        CancellationToken cancellationToken)
    {
        // Eski 98 (COMMAREA kısa) yerine model doğrulaması: 400 + errors
        if (request is null && ModelState.IsValid)
        {
            ModelState.AddModelError("body", "İstek gövdesi gönderilmedi.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationError(ModelState);
        }

        var result = await _service.UpdateAsync(customerNumber, policyNumber, request!, cancellationToken);

        return result.ReturnCode switch
        {
            "00" => Ok(result.Policy),
            "01" => Problem(404, "not-found", "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşterinin {policyNumber} numaralı motor poliçesi bulunamadı.", "01"),
            "02" => Problem(409, "concurrency-conflict", "Kayıt başkası tarafından değiştirilmiş",
                $"{policyNumber} numaralı poliçe okunduktan sonra değiştirilmiş. Kaydı yeniden sorgulayıp tekrar deneyin.", "02"),
            _ when result.DatabaseBusy => Problem(503, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu. Daha sonra tekrar deneyin.", "90"),
            _ => Problem(500, "database-error", "Veritabanı hatası",
                "Poliçe güncellenirken veritabanı hatası oluştu.", "90"),
        };
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

    private static ObjectResult ValidationError(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = 400,
        };

        var result = new ObjectResult(problem) { StatusCode = 400 };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
