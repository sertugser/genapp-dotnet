### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorPolicyResponse.cs
```csharp
namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// Motor poliçesi sorgulama cevabı (API tasarımı 4.2 ve 4.3).
/// </summary>
public sealed record MotorPolicyResponse
{
    public int PolicyNumber { get; init; }
    public int CustomerNumber { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string LastChanged { get; init; } = "";
    public int BrokerId { get; init; }
    public string BrokersReference { get; init; } = "";
    public int Payment { get; init; }
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public int Value { get; init; }
    public string RegistrationNumber { get; init; } = "";
    public string Colour { get; init; } = "";
    public int Cc { get; init; }
    public DateOnly? ManufactureDate { get; init; }
    public int Premium { get; init; }
    public int Accidents { get; init; }
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorPolicyInquiryResult.cs
```csharp
namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// Servis sonucu: eski CA-RETURN-CODE karşılığı ve (başarılıysa) poliçe verisi.
/// </summary>
public sealed class MotorPolicyInquiryResult
{
    public string ReturnCode { get; }
    public bool DatabaseBusy { get; }
    public MotorPolicyResponse? Policy { get; }

    private MotorPolicyInquiryResult(string returnCode, bool databaseBusy, MotorPolicyResponse? policy)
    {
        ReturnCode = returnCode;
        DatabaseBusy = databaseBusy;
        Policy = policy;
    }

    public static MotorPolicyInquiryResult Success(MotorPolicyResponse policy) => new("00", false, policy);
    public static MotorPolicyInquiryResult NotFound() => new("01", false, null);
    public static MotorPolicyInquiryResult DatabaseError() => new("90", false, null);
    public static MotorPolicyInquiryResult Busy() => new("90", true, null);
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorPolicyRepository.cs
```csharp
using System.Globalization;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// LGIPDB01 GET-MOTOR-DB2-INFO karşılığı: POLICY ve MOTOR tablolarının birleşiminden tek satır okur.
/// </summary>
public sealed class MotorPolicyRepository
{
    private readonly string _connectionString;

    public MotorPolicyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Bilinçli sapma: policy_type 'M' değilse satır dönmez ve 01 (bulunamadı) olur (API tasarımı 3).
    private const string Sql = """
        SELECT p.issue_date,
               p.expiry_date,
               p.last_changed,
               p.broker_id,
               p.brokers_reference,
               p.payment,
               m.make,
               m.model,
               m.value,
               m.reg_number,
               m.colour,
               m.cc,
               m.year_of_manufacture,
               m.premium,
               m.accidents
          FROM policy p
          JOIN motor m ON p.policy_number = m.policy_number
         WHERE p.customer_number = @customerNumber
           AND p.policy_number = @policyNumber
           AND p.policy_type = 'M'
        """;

    public async Task<MotorPolicyResponse?> FindAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("customerNumber", customerNumber);
        command.Parameters.AddWithValue("policyNumber", policyNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            // SQLCODE = 100 karşılığı
            return null;
        }

        return new MotorPolicyResponse
        {
            PolicyNumber = policyNumber,
            CustomerNumber = customerNumber,
            IssueDate = reader.IsDBNull(0) ? null : reader.GetFieldValue<DateOnly>(0),
            ExpiryDate = reader.IsDBNull(1) ? null : reader.GetFieldValue<DateOnly>(1),
            LastChanged = reader.IsDBNull(2)
                ? ""
                : reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
            // NULL ise COBOL alanı taşımaz; INITIALIZE DB2-POLICY'den kalan 0/boşluk döner (lgipdb01 GET-MOTOR-DB2-INFO)
            BrokerId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            BrokersReference = Text(reader, 4),
            Payment = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            Make = Text(reader, 6),
            Model = Text(reader, 7),
            Value = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            RegistrationNumber = Text(reader, 9),
            Colour = Text(reader, 10),
            Cc = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader.GetValue(11), CultureInfo.InvariantCulture),
            ManufactureDate = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateOnly>(12),
            Premium = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
            Accidents = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
        };
    }

    private static string Text(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).TrimEnd();
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorPolicyInquiryService.cs
```csharp
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// LGIPOL01 + LGIPDB01 (istek kodu 01IMOT) iş mantığı; dönüş kodunu üretir.
/// </summary>
public sealed class MotorPolicyInquiryService
{
    private readonly MotorPolicyRepository _repository;
    private readonly ILogger _logger;

    public MotorPolicyInquiryService(MotorPolicyRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MotorPolicyInquiryResult> InquireAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _repository.FindAsync(customerNumber, policyNumber, cancellationToken);
            return policy is null
                ? MotorPolicyInquiryResult.NotFound()
                : MotorPolicyInquiryResult.Success(policy);
        }
        catch (Exception ex) when (IsBusy(ex) && !cancellationToken.IsCancellationRequested)
        {
            // Bilinçli sapma (Karar 8): kilitlenme / zaman aşımı 503, returnCode 90
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı meşgul", customerNumber, policyNumber);
            return MotorPolicyInquiryResult.Busy();
        }
        catch (NpgsqlException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // WRITE-ERROR-MESSAGE karşılığı: hata loglanır, 90 döner
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı hatası", customerNumber, policyNumber);
            return MotorPolicyInquiryResult.DatabaseError();
        }
        catch (InvalidOperationException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Bağlantı/okuma sırasında oluşan diğer veritabanı hataları da 90 sayılır (lgipdb01 SQLCODE <> 0 ve <> 100)
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı hatası", customerNumber, policyNumber);
            return MotorPolicyInquiryResult.DatabaseError();
        }
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is "40P01" // deadlock_detected
                or "55P03"               // lock_not_available
                or "57014";              // query_canceled (statement_timeout)
        }

        if (ex is NpgsqlException npgsql)
        {
            return npgsql.InnerException is TimeoutException;
        }

        return ex is TimeoutException;
    }
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorPolicyInquiryController.cs
```csharp
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
```