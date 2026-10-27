### dosya: src/GenApp.Llm/MusteriSorgula/MusteriSorgulaController.cs
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri sorgula (LGICUS01 / LGICDB01, istek kodu 01ICUS).
/// </summary>
[ApiController]
public sealed class MusteriSorgulaController : ControllerBase
{
    private readonly MusteriSorgulaService _service;

    public MusteriSorgulaController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var repository = new MusteriSorgulaRepository(
            configuration,
            loggerFactory.CreateLogger<MusteriSorgulaRepository>());
        _service = new MusteriSorgulaService(repository);
    }

    // Tam sayı sınırını aşan numara :int kısıtıyla eşleşmez ve ASP.NET Core'un 404'ü döner (API tasarımı, bilinçli sapma).
    [HttpGet("/api/customers/{customerNumber:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MusteriSorgulaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> Get(int customerNumber, CancellationToken cancellationToken)
    {
        var result = await _service.InquireAsync(customerNumber, cancellationToken);

        if (result.ReturnCode == ReturnCodes.Success && result.Customer is not null)
        {
            return Ok(result.Customer);
        }

        if (result.ReturnCode == ReturnCodes.NotFound)
        {
            return ProblemResult(
                StatusCodes.Status404NotFound,
                "not-found",
                "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.",
                ReturnCodes.NotFound);
        }

        if (result.DatabaseBusy)
        {
            return ProblemResult(
                StatusCodes.Status503ServiceUnavailable,
                "database-busy",
                "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu. Lütfen daha sonra tekrar deneyin.",
                ReturnCodes.DatabaseError);
        }

        return ProblemResult(
            StatusCodes.Status500InternalServerError,
            "database-error",
            "Veritabanı hatası",
            $"{customerNumber} numaralı müşteri sorgulanırken veritabanı hatası oluştu.",
            ReturnCodes.DatabaseError);
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

### dosya: src/GenApp.Llm/MusteriSorgula/MusteriSorgulaService.cs
```csharp
namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Eski dönüş kodları (CA-RETURN-CODE).
/// </summary>
public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

/// <summary>
/// Operasyon sonucu: dönüş kodu ve (başarılıysa) müşteri.
/// DatabaseBusy: kilitlenme/zaman aşımı (eskide -913); dönüş kodu "90" olarak taşınır, HTTP 503'e çevrilir.
/// </summary>
public sealed record MusteriSorgulaResult(string ReturnCode, MusteriSorgulaResponse? Customer, bool DatabaseBusy = false)
{
    public static MusteriSorgulaResult Found(MusteriSorgulaResponse customer) => new(ReturnCodes.Success, customer);
    public static MusteriSorgulaResult NotFound() => new(ReturnCodes.NotFound, null);
    public static MusteriSorgulaResult Busy() => new(ReturnCodes.DatabaseError, null, DatabaseBusy: true);
    public static MusteriSorgulaResult DatabaseError() => new(ReturnCodes.DatabaseError, null);
}

/// <summary>
/// LGICUS01 karşılığı. COMMAREA uzunluk kontrolü (98) yeni sistemde yoktur; numarayı adres taşır.
/// </summary>
public sealed class MusteriSorgulaService
{
    private readonly MusteriSorgulaRepository _repository;

    public MusteriSorgulaService(MusteriSorgulaRepository repository)
    {
        _repository = repository;
    }

    public Task<MusteriSorgulaResult> InquireAsync(int customerNumber, CancellationToken cancellationToken)
    {
        // LGICUS01 yalnızca LGICDB01'i çağırır (GET-CUSTOMER-INFO); başka iş kuralı yoktur.
        return _repository.GetCustomerAsync(customerNumber, cancellationToken);
    }
}
```

### dosya: src/GenApp.Llm/MusteriSorgula/MusteriSorgulaRepository.cs
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// LGICDB01 karşılığı: customer tablosundan tek müşteri okur.
/// </summary>
public sealed class MusteriSorgulaRepository
{
    private const string Sql = """
        SELECT first_name,
               last_name,
               date_of_birth,
               house_name,
               house_number,
               postcode,
               phone_mobile,
               phone_home,
               email_address
          FROM customer
         WHERE customer_number = @customerNumber
        """;

    private readonly IConfiguration _configuration;
    private readonly ILogger<MusteriSorgulaRepository> _logger;

    public MusteriSorgulaRepository(IConfiguration configuration, ILogger<MusteriSorgulaRepository> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MusteriSorgulaResult> GetCustomerAsync(int customerNumber, CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(Sql, connection);
            command.Parameters.AddWithValue("customerNumber", customerNumber);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                // SQLCODE 100 -> '01'
                return MusteriSorgulaResult.NotFound();
            }

            var customer = new MusteriSorgulaResponse
            {
                CustomerNumber = customerNumber,
                FirstName = ReadText(reader, 0),
                LastName = ReadText(reader, 1),
                DateOfBirth = reader.IsDBNull(2) ? null : reader.GetFieldValue<DateOnly>(2),
                HouseName = ReadText(reader, 3),
                HouseNumber = ReadText(reader, 4),
                Postcode = ReadText(reader, 5),
                PhoneMobile = ReadText(reader, 6),
                PhoneHome = ReadText(reader, 7),
                Email = ReadText(reader, 8),
            };

            // SQLCODE 0 -> '00'
            return MusteriSorgulaResult.Found(customer);
        }
        catch (PostgresException ex) when (IsBusy(ex.SqlState))
        {
            // Eskide -913 -> '01' (lgicdb01.cbl); API tasarımı Karar 8: 503 + "90".
            _logger.LogWarning(ex, "LGICUS01 CNUM={CustomerNumber} veritabanı meşgul SQLSTATE={SqlState}",
                customerNumber, ex.SqlState);
            return MusteriSorgulaResult.Busy();
        }
        catch (NpgsqlException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning(ex, "LGICUS01 CNUM={CustomerNumber} veritabanı zaman aşımı", customerNumber);
            return MusteriSorgulaResult.Busy();
        }
        catch (NpgsqlException ex)
        {
            // When Other -> '90' + WRITE-ERROR-MESSAGE (LGSTSQ yerine ILogger)
            _logger.LogError(ex, "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER veritabanı hatası", customerNumber);
            return MusteriSorgulaResult.DatabaseError();
        }
    }

    private static bool IsBusy(string sqlState) => sqlState is
        PostgresErrorCodes.DeadlockDetected or
        PostgresErrorCodes.LockNotAvailable or
        PostgresErrorCodes.QueryCanceled or
        PostgresErrorCodes.SerializationFailure;

    // Boş metin "" döner, null dönmez; sondaki boşluklar kırpılır (API tasarımı, 1).
    private static string ReadText(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal).TrimEnd();
}
```

### dosya: src/GenApp.Llm/MusteriSorgula/MusteriSorgulaResponse.cs
```csharp
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Müşteri cevabı (API tasarımı 4.1).
/// </summary>
public sealed class MusteriSorgulaResponse
{
    [JsonPropertyName("customerNumber")]
    public int CustomerNumber { get; init; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;

    // DateOnly, System.Text.Json ile yyyy-MM-dd biçiminde yazılır.
    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; init; }

    [JsonPropertyName("houseName")]
    public string HouseName { get; init; } = string.Empty;

    [JsonPropertyName("houseNumber")]
    public string HouseNumber { get; init; } = string.Empty;

    [JsonPropertyName("postcode")]
    public string Postcode { get; init; } = string.Empty;

    [JsonPropertyName("phoneMobile")]
    public string PhoneMobile { get; init; } = string.Empty;

    [JsonPropertyName("phoneHome")]
    public string PhoneHome { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
```