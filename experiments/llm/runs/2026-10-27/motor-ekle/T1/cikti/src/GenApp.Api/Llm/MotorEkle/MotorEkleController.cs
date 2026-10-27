using Microsoft.AspNetCore.Mvc;

namespace GenApp.Llm.MotorEkle;

[ApiController]
[Route("api/customers/{customerNumber:int}/motor-policies")]
[MotorEkleValidationFilter]
[Produces("application/json")]
public sealed class MotorEkleController : ControllerBase
{
    private readonly MotorEkleService _service;

    public MotorEkleController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
        var repository = new MotorEkleRepository(connectionString, loggerFactory.CreateLogger<MotorEkleRepository>());
        _service = new MotorEkleService(repository);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MotorPolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AddMotorPolicy(
        [FromRoute] int customerNumber,
        [FromBody] MotorEkleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AddAsync(customerNumber, request, cancellationToken);

        if (result.ReturnCode == ReturnCodes.Success && result.Policy is not null)
        {
            var location = $"/api/customers/{customerNumber}/motor-policies/{result.Policy.PolicyNumber}";
            return Created(location, result.Policy);
        }

        if (result.Busy)
        {
            return ProblemResult(StatusCodes.Status503ServiceUnavailable, "database-busy",
                "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı nedeniyle işlem yapılamadı. Lütfen tekrar deneyin.",
                ReturnCodes.DatabaseError);
        }

        return result.ReturnCode switch
        {
            ReturnCodes.CustomerNotFound => ProblemResult(StatusCodes.Status404NotFound, "customer-not-found",
                "Müşteri bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.",
                ReturnCodes.CustomerNotFound),
            _ => ProblemResult(StatusCodes.Status500InternalServerError, "database-error",
                "Veritabanı hatası",
                "Poliçe eklenirken veritabanı hatası oluştu.",
                ReturnCodes.DatabaseError),
        };
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
