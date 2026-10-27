using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri sorgula (LGICUS01 / LGICDB01, istek kodu 01ICUS).
/// </summary>
[ApiController]
[Route("api/customers")]
public sealed class MusteriSorgulaController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MusteriSorgulaController> _logger;

    public MusteriSorgulaController(IConfiguration configuration, ILogger<MusteriSorgulaController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // int kısıtı: 2147483647'yi aşan numara adresle eşleşmez, ASP.NET Core 404 döner (API tasarımı, bilinçli sapma).
    [HttpGet("{customerNumber:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Get(int customerNumber, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings:GenApp yapılandırılmamış.");

        var service = new CustomerInquiryService(
            new NpgsqlCustomerInquiryRepository(connectionString),
            _logger);

        var result = await service.InquireAsync(customerNumber, cancellationToken);

        return result.Outcome switch
        {
            InquiryOutcome.Success => Ok(CustomerResponse.From(result.Customer!)),
            InquiryOutcome.NotFound => Problem(
                StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.", result.ReturnCode),
            InquiryOutcome.DatabaseBusy => Problem(
                StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu; istek daha sonra tekrar denenmelidir.", result.ReturnCode),
            _ => Problem(
                StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası",
                $"{customerNumber} numaralı müşteri okunurken veritabanı hatası oluştu.", result.ReturnCode),
        };
    }

    private ObjectResult Problem(int status, string type, string title, string detail, string returnCode)
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

/// <summary>Müşteri cevabı (API tasarımı 4.1).</summary>
public sealed record CustomerResponse
{
    [JsonPropertyName("customerNumber")] public int CustomerNumber { get; init; }
    [JsonPropertyName("firstName")] public string FirstName { get; init; } = "";
    [JsonPropertyName("lastName")] public string LastName { get; init; } = "";
    [JsonPropertyName("dateOfBirth")] public DateOnly? DateOfBirth { get; init; }
    [JsonPropertyName("houseName")] public string HouseName { get; init; } = "";
    [JsonPropertyName("houseNumber")] public string HouseNumber { get; init; } = "";
    [JsonPropertyName("postcode")] public string Postcode { get; init; } = "";
    [JsonPropertyName("phoneMobile")] public string PhoneMobile { get; init; } = "";
    [JsonPropertyName("phoneHome")] public string PhoneHome { get; init; } = "";
    [JsonPropertyName("email")] public string Email { get; init; } = "";

    public static CustomerResponse From(CustomerRecord c) => new()
    {
        CustomerNumber = c.CustomerNumber,
        FirstName = Clean(c.FirstName),
        LastName = Clean(c.LastName),
        DateOfBirth = c.DateOfBirth,
        HouseName = Clean(c.HouseName),
        HouseNumber = Clean(c.HouseNumber),
        Postcode = Clean(c.Postcode),
        PhoneMobile = Clean(c.PhoneMobile),
        PhoneHome = Clean(c.PhoneHome),
        Email = Clean(c.Email),
    };

    // Sondaki boşluklar kırpılır, boş/NULL metin "" döner (API tasarımı 1).
    private static string Clean(string? value) => (value ?? "").TrimEnd(' ');
}
