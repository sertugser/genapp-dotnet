using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// Motor poliçesi sorgula (LGIPOL01, istek kodu 01IMOT).
/// </summary>
[ApiController]
public sealed class MotorPolicyInquiryController : ControllerBase
{
    private readonly MotorPolicyInquiryService _service;

    public MotorPolicyInquiryController(IConfiguration configuration, ILogger<MotorPolicyInquiryController> logger)
    {
        var connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings:GenApp yapılandırması bulunamadı.");
        _service = new MotorPolicyInquiryService(new MotorPolicyRepository(connectionString), logger);
    }

    [HttpGet("/api/customers/{customerNumber:int}/motor-policies/{policyNumber:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MotorPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Get(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var result = await _service.InquireAsync(customerNumber, policyNumber, cancellationToken);

        if (result.ReturnCode == "00" && result.Policy is not null)
        {
            return Ok(result.Policy);
        }

        if (result.DatabaseBusy)
        {
            return ProblemResult(StatusCodes.Status503ServiceUnavailable, "database-busy",
                "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu; işlem daha sonra tekrar denenmelidir.",
                result.ReturnCode);
        }

        return result.ReturnCode switch
        {
            "01" => ProblemResult(StatusCodes.Status404NotFound, "not-found",
                "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşterinin {policyNumber} numaralı motor poliçesi bulunamadı.",
                "01"),
            _ => ProblemResult(StatusCodes.Status500InternalServerError, "database-error",
                "Veritabanı hatası",
                "Motor poliçesi okunurken veritabanı hatası oluştu.",
                result.ReturnCode),
        };
    }

    private ObjectResult ProblemResult(int status, string type, string title, string detail, string returnCode)
    {
        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail,
        };
        problem.Extensions["returnCode"] = returnCode;

        var objectResult = new ObjectResult(problem) { StatusCode = status };
        objectResult.ContentTypes.Add("application/problem+json");
        return objectResult;
    }
}
