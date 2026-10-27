using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MotorSorgula;

[ApiController]
[Route("api/customers/{customerNumber:int}/motor-policies")]
public sealed class MotorSorgulaController : ControllerBase
{
    private readonly MotorSorgulaService _service;

    public MotorSorgulaController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
        var repository = new MotorSorgulaRepository(connectionString);
        _service = new MotorSorgulaService(repository, loggerFactory.CreateLogger<MotorSorgulaService>());
    }

    [HttpGet("{policyNumber:int}")]
    [ProducesResponseType(typeof(MotorPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var result = await _service.InquireAsync(customerNumber, policyNumber, cancellationToken);

        if (result.ReturnCode == ReturnCodes.Success && result.Policy is not null)
        {
            return Ok(result.Policy);
        }

        return result.ReturnCode switch
        {
            ReturnCodes.NotFound => Problem(
                StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşterinin {policyNumber} numaralı motor poliçesi bulunamadı.",
                ReturnCodes.NotFound),
            _ when result.DatabaseBusy => Problem(
                StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu. Lütfen tekrar deneyin.",
                ReturnCodes.DatabaseError),
            _ => Problem(
                StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası",
                "Motor poliçesi okunurken veritabanı hatası oluştu.",
                ReturnCodes.DatabaseError)
        };
    }

    private ObjectResult Problem(int status, string type, string title, string detail, string returnCode)
    {
        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail
        };
        problem.Extensions["returnCode"] = returnCode;

        var result = new ObjectResult(problem) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
