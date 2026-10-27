### dosya: src/GenApp.Api/Llm/MusteriSorgula/CustomerInquiryController.cs
```csharp
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
```

### dosya: src/GenApp.Api/Llm/MusteriSorgula/CustomerResponse.cs
```csharp
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri cevabı (API tasarımı 4.1).
/// </summary>
public sealed class CustomerResponse
{
    [JsonPropertyName("customerNumber")]
    public int CustomerNumber { get; init; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = "";

    /// <summary>yyyy-MM-dd biçiminde.</summary>
    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("houseName")]
    public string HouseName { get; init; } = "";

    [JsonPropertyName("houseNumber")]
    public string HouseNumber { get; init; } = "";

    [JsonPropertyName("postcode")]
    public string Postcode { get; init; } = "";

    [JsonPropertyName("phoneMobile")]
    public string PhoneMobile { get; init; } = "";

    [JsonPropertyName("phoneHome")]
    public string PhoneHome { get; init; } = "";

    [JsonPropertyName("email")]
    public string Email { get; init; } = "";
}
```

### dosya: src/GenApp.Api/Llm/MusteriSorgula/CustomerInquiryResult.cs
```csharp
namespace GenApp.Llm.MusteriSorgula;

/// <summary>Eski CA-RETURN-CODE değerleri.</summary>
public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public enum InquiryOutcome
{
    /// <summary>SQLCODE 0 → "00".</summary>
    Found,
    /// <summary>SQLCODE 100 → "01".</summary>
    NotFound,
    /// <summary>Kilitlenme / zaman aşımı (eskide -913 → "01"; bilinçli sapma: 503 + "90").</summary>
    DatabaseBusy,
    /// <summary>Diğer SQLCODE → "90".</summary>
    DatabaseError,
}

public sealed class CustomerInquiryResult
{
    public InquiryOutcome Outcome { get; }
    public CustomerResponse? Customer { get; }

    public string ReturnCode => Outcome switch
    {
        InquiryOutcome.Found => ReturnCodes.Success,
        InquiryOutcome.NotFound => ReturnCodes.NotFound,
        _ => ReturnCodes.DatabaseError,
    };

    private CustomerInquiryResult(InquiryOutcome outcome, CustomerResponse? customer)
    {
        Outcome = outcome;
        Customer = customer;
    }

    public static CustomerInquiryResult Found(CustomerResponse customer) => new(InquiryOutcome.Found, customer);
    public static CustomerInquiryResult NotFound() => new(InquiryOutcome.NotFound, null);
    public static CustomerInquiryResult Busy() => new(InquiryOutcome.DatabaseBusy, null);
    public static CustomerInquiryResult Error() => new(InquiryOutcome.DatabaseError, null);
}
```

### dosya: src/GenApp.Api/Llm/MusteriSorgula/CustomerInquiryService.cs
```csharp
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// LGICUS01 + LGICDB01 iş mantığı: müşteriyi numarasıyla okur ve dönüş kodunu üretir.
/// </summary>
public sealed class CustomerInquiryService
{
    private readonly CustomerInquiryRepository _repository;
    private readonly ILogger<CustomerInquiryService> _logger;

    public CustomerInquiryService(CustomerInquiryRepository repository, ILogger<CustomerInquiryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CustomerInquiryResult> InquireAsync(int customerNumber, CancellationToken cancellationToken)
    {
        try
        {
            var row = await _repository.FindAsync(customerNumber, cancellationToken);

            // lgicdb01.cbl: When 100 → '01'
            if (row is null)
                return CustomerInquiryResult.NotFound();

            // lgicdb01.cbl: When 0 → '00'
            return CustomerInquiryResult.Found(Map(row));
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            // lgicdb01.cbl: When -913 → '01'; bilinçli sapma (Karar 8): 503 + "90"
            _logger.LogWarning(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER: veritabanı kilitlenmesi ya da zaman aşımı",
                customerNumber);
            return CustomerInquiryResult.Busy();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
        {
            // lgicdb01.cbl: When Other → '90' + WRITE-ERROR-MESSAGE (LGSTSQ yerine ILogger)
            _logger.LogError(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER: veritabanı hatası (SQLSTATE={SqlState})",
                customerNumber, (ex as PostgresException)?.SqlState);
            return CustomerInquiryResult.Error();
        }
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is
                PostgresErrorCodes.DeadlockDetected or      // 40P01
                PostgresErrorCodes.LockNotAvailable or      // 55P03
                PostgresErrorCodes.QueryCanceled;           // 57014 (statement_timeout)
        }

        // Npgsql komut zaman aşımı
        return ex is NpgsqlException && ex.InnerException is TimeoutException;
    }

    private static CustomerResponse Map(CustomerRow row) => new()
    {
        CustomerNumber = row.CustomerNumber,
        FirstName = Clean(row.FirstName),
        LastName = Clean(row.LastName),
        DateOfBirth = row.DateOfBirth?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        HouseName = Clean(row.HouseName),
        HouseNumber = Clean(row.HouseNumber),
        Postcode = Clean(row.Postcode),
        PhoneMobile = Clean(row.PhoneMobile),
        PhoneHome = Clean(row.PhoneHome),
        Email = Clean(row.EmailAddress),
    };

    // Sondaki boşluklar kırpılır; NULL metin "" döner (Db2'de göstergesiz NULL okunamazdı, yeni sistemde boş sayılır).
    private static string Clean(string? value) => value?.TrimEnd(' ') ?? "";
}
```

### dosya: src/GenApp.Api/Llm/MusteriSorgula/CustomerInquiryRepository.cs
```csharp
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

public sealed record CustomerRow(
    int CustomerNumber,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? HouseName,
    string? HouseNumber,
    string? Postcode,
    string? PhoneMobile,
    string? PhoneHome,
    string? EmailAddress);

/// <summary>
/// LGICDB01'deki SELECT ... FROM CUSTOMER WHERE CUSTOMERNUMBER = ? karşılığı.
/// </summary>
public sealed class CustomerInquiryRepository
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

    public CustomerInquiryRepository(IConfiguration configuration)
    {
        // ConnectionStrings__GenApp ortam değişkeni ConnectionStrings:GenApp olarak okunur.
        _connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings:GenApp yapılandırılmamış.");
    }

    public async Task<CustomerRow?> FindAsync(int customerNumber, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("customer_number", customerNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new CustomerRow(
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