### dosya: src/GenApp.Llm/MusteriSorgula/MusteriSorgulaController.cs
```csharp
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
```

### dosya: src/GenApp.Llm/MusteriSorgula/CustomerInquiryService.cs
```csharp
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

public enum InquiryOutcome
{
    Success,
    NotFound,
    DatabaseBusy,
    DatabaseError,
}

public sealed record CustomerInquiryResult(InquiryOutcome Outcome, string ReturnCode, CustomerRecord? Customer)
{
    public static CustomerInquiryResult Found(CustomerRecord c) => new(InquiryOutcome.Success, ReturnCodes.Success, c);
    public static CustomerInquiryResult NotFound() => new(InquiryOutcome.NotFound, ReturnCodes.NotFound, null);
    public static CustomerInquiryResult Busy() => new(InquiryOutcome.DatabaseBusy, ReturnCodes.DatabaseError, null);
    public static CustomerInquiryResult Error() => new(InquiryOutcome.DatabaseError, ReturnCodes.DatabaseError, null);
}

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

/// <summary>LGICUS01 + LGICDB01 iş mantığı.</summary>
public sealed class CustomerInquiryService
{
    private readonly ICustomerInquiryRepository _repository;
    private readonly ILogger _logger;

    public CustomerInquiryService(ICustomerInquiryRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CustomerInquiryResult> InquireAsync(int customerNumber, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _repository.FindAsync(customerNumber, cancellationToken);

            // SQLCODE 0 → '00', SQLCODE 100 → '01' (lgicdb01.cbl GET-CUSTOMER-INFO)
            return customer is null
                ? CustomerInquiryResult.NotFound()
                : CustomerInquiryResult.Found(customer);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsLockOrTimeout(ex))
        {
            // Eski sistemde -913 → '01'; yeni sistemde bilinçli sapma: 503 + '90' (ADR 0002 Karar 8).
            _logger.LogWarning(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER kilitlenme/zaman aşımı SQLSTATE={SqlState}",
                customerNumber, (ex as PostgresException)?.SqlState);
            return CustomerInquiryResult.Busy();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidCastException)
        {
            // When Other → '90' + WRITE-ERROR-MESSAGE (LGSTSQ yerine ILogger).
            _logger.LogError(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER SQLSTATE={SqlState}",
                customerNumber, (ex as PostgresException)?.SqlState);
            return CustomerInquiryResult.Error();
        }
    }

    private static bool IsLockOrTimeout(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is
                "40P01"   // deadlock_detected
                or "55P03" // lock_not_available
                or "57014" // query_canceled (statement_timeout / lock_timeout)
                or "40001"; // serialization_failure
        }

        return ex is NpgsqlException { InnerException: TimeoutException } || ex is TimeoutException;
    }
}
```

### dosya: src/GenApp.Llm/MusteriSorgula/CustomerInquiryRepository.cs
```csharp
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

public sealed record CustomerRecord(
    int CustomerNumber,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? HouseName,
    string? HouseNumber,
    string? Postcode,
    string? PhoneMobile,
    string? PhoneHome,
    string? Email);

public interface ICustomerInquiryRepository
{
    /// <summary>Müşteriyi okur; yoksa null döner.</summary>
    Task<CustomerRecord?> FindAsync(int customerNumber, CancellationToken cancellationToken);
}

/// <summary>LGICDB01'deki SELECT ... FROM CUSTOMER WHERE CUSTOMERNUMBER = ? karşılığı.</summary>
public sealed class NpgsqlCustomerInquiryRepository : ICustomerInquiryRepository
{
    private const string Sql = """
        SELECT customer_number,
               first_name,
               last_name,
               date_of_birth,
               house_name,
               house_number,
               postcode,
               phone_mobile,
               phone_home,
               email_address
          FROM customer
         WHERE customer_number = @customer_number
        """;

    private readonly string _connectionString;

    public NpgsqlCustomerInquiryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CustomerRecord?> FindAsync(int customerNumber, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("customer_number", customerNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CustomerRecord(
            reader.GetInt32(0),
            GetString(reader, 1),
            GetString(reader, 2),
            reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
            GetString(reader, 4),
            GetString(reader, 5),
            GetString(reader, 6),
            GetString(reader, 7),
            GetString(reader, 8),
            GetString(reader, 9));
    }

    private static string? GetString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
```