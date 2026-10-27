using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri sorgula (eski: LGICUS01 → LGICDB01, istek kodu 01ICUS).
/// </summary>
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public sealed class CustomerInquiryController : ControllerBase
{
    private readonly CustomerInquiryService _service;

    public CustomerInquiryController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        // Başka operasyonlardan bağımsız çalışmak için servis burada kurulur (DI kaydı gerektirmez).
        var repository = new CustomerInquiryRepository(configuration);
        _service = new CustomerInquiryService(repository, loggerFactory.CreateLogger<CustomerInquiryService>());
    }

    [HttpGet("{customerNumber:int}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> GetCustomer(int customerNumber, CancellationToken cancellationToken)
    {
        var result = await _service.InquireAsync(customerNumber, cancellationToken);

        return result.Outcome switch
        {
            InquiryOutcome.Found => Ok(result.Customer),
            InquiryOutcome.NotFound => ProblemResult(
                StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.", ReturnCodes.NotFound),
            InquiryOutcome.DatabaseBusy => ProblemResult(
                StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu. İsteği daha sonra tekrar deneyin.",
                ReturnCodes.DatabaseError),
            _ => ProblemResult(
                StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası",
                $"{customerNumber} numaralı müşteri okunurken veritabanı hatası oluştu.",
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
