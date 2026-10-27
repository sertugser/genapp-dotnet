### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorSorgulaController.cs
```csharp
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
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorPolicyResponse.cs
```csharp
using System.Text.Json.Serialization;

namespace GenApp.Llm.MotorSorgula;

public sealed class MotorPolicyResponse
{
    [JsonPropertyName("policyNumber")] public int PolicyNumber { get; init; }
    [JsonPropertyName("customerNumber")] public int CustomerNumber { get; init; }
    [JsonPropertyName("issueDate")] public DateOnly IssueDate { get; init; }
    [JsonPropertyName("expiryDate")] public DateOnly ExpiryDate { get; init; }
    [JsonPropertyName("lastChanged")] public string LastChanged { get; init; } = "";
    [JsonPropertyName("brokerId")] public long BrokerId { get; init; }
    [JsonPropertyName("brokersReference")] public string BrokersReference { get; init; } = "";
    [JsonPropertyName("payment")] public int Payment { get; init; }
    [JsonPropertyName("make")] public string Make { get; init; } = "";
    [JsonPropertyName("model")] public string Model { get; init; } = "";
    [JsonPropertyName("value")] public int Value { get; init; }
    [JsonPropertyName("registrationNumber")] public string RegistrationNumber { get; init; } = "";
    [JsonPropertyName("colour")] public string Colour { get; init; } = "";
    [JsonPropertyName("cc")] public int Cc { get; init; }
    [JsonPropertyName("manufactureDate")] public DateOnly ManufactureDate { get; init; }
    [JsonPropertyName("premium")] public int Premium { get; init; }
    [JsonPropertyName("accidents")] public int Accidents { get; init; }
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/ReturnCodes.cs
```csharp
namespace GenApp.Llm.MotorSorgula;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public sealed record MotorSorgulaResult(string ReturnCode, MotorPolicyResponse? Policy, bool DatabaseBusy = false)
{
    public static MotorSorgulaResult Ok(MotorPolicyResponse policy) => new(ReturnCodes.Success, policy);
    public static MotorSorgulaResult NotFound() => new(ReturnCodes.NotFound, null);
    public static MotorSorgulaResult DbError() => new(ReturnCodes.DatabaseError, null);
    public static MotorSorgulaResult Busy() => new(ReturnCodes.DatabaseError, null, true);
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorPolicyRow.cs
```csharp
namespace GenApp.Llm.MotorSorgula;

public sealed class MotorPolicyRow
{
    public string? PolicyType { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public DateTime LastChanged { get; init; }
    public int? BrokerId { get; init; }
    public string? BrokersReference { get; init; }
    public int? Payment { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Value { get; init; }
    public string? RegNumber { get; init; }
    public string? Colour { get; init; }
    public int? Cc { get; init; }
    public DateOnly? YearOfManufacture { get; init; }
    public int? Premium { get; init; }
    public int? Accidents { get; init; }
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorSorgulaRepository.cs
```csharp
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

public sealed class MotorSorgulaRepository
{
    private readonly string _connectionString;

    public MotorSorgulaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // lgipdb01.cbl GET-MOTOR-DB2-INFO: POLICY ve MOTOR birleşimi, müşteri + poliçe numarasıyla tek satır
    private const string Sql = """
        SELECT p.policy_type,
               p.issue_date,
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
        """;

    public async Task<MotorPolicyRow?> FindAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("customerNumber", customerNumber);
        command.Parameters.AddWithValue("policyNumber", policyNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null; // SQLCODE = 100
        }

        return new MotorPolicyRow
        {
            PolicyType = GetString(reader, 0),
            IssueDate = GetDate(reader, 1),
            ExpiryDate = GetDate(reader, 2),
            LastChanged = reader.GetDateTime(3),
            BrokerId = GetInt(reader, 4),
            BrokersReference = GetString(reader, 5),
            Payment = GetInt(reader, 6),
            Make = GetString(reader, 7),
            Model = GetString(reader, 8),
            Value = GetInt(reader, 9),
            RegNumber = GetString(reader, 10),
            Colour = GetString(reader, 11),
            Cc = GetInt(reader, 12),
            YearOfManufacture = GetDate(reader, 13),
            Premium = GetInt(reader, 14),
            Accidents = GetInt(reader, 15)
        };
    }

    private static string? GetString(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);

    private static int? GetInt(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : Convert.ToInt32(r.GetValue(i));

    private static DateOnly? GetDate(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetFieldValue<DateOnly>(i);
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorSorgulaService.cs
```csharp
using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

public sealed class MotorSorgulaService
{
    private const string MotorPolicyType = "M";

    private readonly MotorSorgulaRepository _repository;
    private readonly ILogger<MotorSorgulaService> _logger;

    public MotorSorgulaService(MotorSorgulaRepository repository, ILogger<MotorSorgulaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MotorSorgulaResult> InquireAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        MotorPolicyRow? row;
        try
        {
            row = await _repository.FindAsync(customerNumber, policyNumber, cancellationToken);
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            // API tasarımı: kilitlenme ve zaman aşımı 503 / "90" (bilinçli sapma, Karar 8)
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı meşgul", customerNumber, policyNumber);
            return MotorSorgulaResult.Busy();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidCastException)
        {
            // lgipdb01.cbl: SQLCODE 0 ve 100 dışında '90' + WRITE-ERROR-MESSAGE
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı hatası", customerNumber, policyNumber);
            return MotorSorgulaResult.DbError();
        }

        if (row is null)
        {
            // lgipdb01.cbl: SQLCODE = 100 → '01'
            return MotorSorgulaResult.NotFound();
        }

        // Bilinçli sapma: poliçe türü adresteki türle (motor 'M') uyuşmuyorsa '01'
        if (!string.Equals(row.PolicyType?.TrimEnd(), MotorPolicyType, StringComparison.Ordinal))
        {
            return MotorSorgulaResult.NotFound();
        }

        // Motor ve tarih kolonları göstergesiz (INDICATOR'sız) okunur; NULL gelirse Db2 -305 verir → '90'
        if (row.IssueDate is null || row.ExpiryDate is null || row.Make is null || row.Model is null ||
            row.Value is null || row.RegNumber is null || row.Colour is null || row.Cc is null ||
            row.YearOfManufacture is null || row.Premium is null || row.Accidents is null)
        {
            _logger.LogError("LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: göstergesiz kolonda NULL (SQLCODE=-305)", customerNumber, policyNumber);
            return MotorSorgulaResult.DbError();
        }

        var response = new MotorPolicyResponse
        {
            PolicyNumber = policyNumber,
            CustomerNumber = customerNumber,
            IssueDate = row.IssueDate.Value,
            ExpiryDate = row.ExpiryDate.Value,
            LastChanged = row.LastChanged.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
            // IND-BROKERID / IND-PAYMENT = -1 ise alan taşınmaz, INITIALIZE değeri (0) kalır
            BrokerId = row.BrokerId is null ? 0 : FitUnsigned(row.BrokerId.Value, 10),
            // NULL BROKERSREFERENCE: INITIALIZE edilmiş boşluklar kalır → ""
            BrokersReference = (row.BrokersReference ?? "").TrimEnd(),
            Payment = row.Payment is null ? 0 : (int)FitUnsigned(row.Payment.Value, 6),
            Make = row.Make.TrimEnd(),
            Model = row.Model.TrimEnd(),
            Value = (int)FitUnsigned(row.Value.Value, 6),
            RegistrationNumber = row.RegNumber.TrimEnd(),
            Colour = row.Colour.TrimEnd(),
            Cc = (int)FitUnsigned(row.Cc.Value, 4),
            ManufactureDate = row.YearOfManufacture.Value,
            Premium = (int)FitUnsigned(row.Premium.Value, 6),
            Accidents = (int)FitUnsigned(row.Accidents.Value, 6)
        };

        return MotorSorgulaResult.Ok(response);
    }

    // COBOL MOVE işaretli tam sayı → işaretsiz PIC 9(n): işaret düşer, soldaki fazla haneler kesilir
    private static long FitUnsigned(int value, int digits)
    {
        long abs = Math.Abs((long)value);
        long modulus = (long)Math.Pow(10, digits);
        return abs % modulus;
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is "40P01" // deadlock_detected
                or "55P03"                // lock_not_available
                or "57014";               // query_canceled (statement_timeout)
        }

        if (ex is NpgsqlException npg && npg.InnerException is TimeoutException)
        {
            return true;
        }

        return ex is TimeoutException;
    }
}
```