using Microsoft.AspNetCore.Mvc;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Müşteri güncelle (eski: LGUCUS01 → LGUCDB01, istek kodu 01UCUS).
/// </summary>
[ApiController]
[Route("api/customers/{customerNumber:int}")]
[Produces("application/json")]
[ValidationErrorFilter]
public sealed class CustomerUpdateController : ControllerBase
{
    private readonly CustomerUpdateService _service;

    public CustomerUpdateController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
        var repository = new CustomerUpdateRepository(connectionString);
        _service = new CustomerUpdateService(repository, loggerFactory.CreateLogger<CustomerUpdateService>());
    }

    [HttpPut]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> UpdateCustomer(
        [FromRoute] int customerNumber,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var data = new CustomerUpdateData(
            FirstName: request.FirstName ?? string.Empty,
            LastName: request.LastName ?? string.Empty,
            DateOfBirth: request.DateOfBirth!.Value,
            HouseName: request.HouseName ?? string.Empty,
            HouseNumber: request.HouseNumber ?? string.Empty,
            // API tasarımı: posta kodu büyük harfe çevrilerek kaydedilir (lgtestc1.cbl:188-189)
            Postcode: (request.Postcode ?? string.Empty).ToUpperInvariant(),
            PhoneMobile: request.PhoneMobile ?? string.Empty,
            PhoneHome: request.PhoneHome ?? string.Empty,
            Email: request.Email ?? string.Empty);

        var result = await _service.UpdateAsync(customerNumber, data, cancellationToken);

        return result.ReturnCode switch
        {
            ReturnCodes.Success => Ok(result.Customer),
            ReturnCodes.NotFound => Problem(
                StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.", ReturnCodes.NotFound),
            _ when result.DatabaseBusy => Problem(
                StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu. İsteği daha sonra yeniden deneyin.",
                ReturnCodes.DatabaseError),
            _ => Problem(
                StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası",
                $"{customerNumber} numaralı müşteri güncellenirken veritabanı hatası oluştu.",
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
