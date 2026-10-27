### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorSorgulaController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MotorSorgula;

[ApiController]
[Route("api/customers/{customerNumber:int}/motor-policies/{policyNumber:int}")]
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

    [HttpGet]
    [ProducesResponseType(typeof(MotorPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        var result = await _service.InquireAsync(customerNumber, policyNumber, cancellationToken);

        return result.Outcome switch
        {
            MotorSorgulaOutcome.Success => Ok(MotorPolicyResponse.From(result.Row!)),
            MotorSorgulaOutcome.NotFound => ProblemResult(
                StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşterinin {policyNumber} numaralı motor poliçesi bulunamadı.",
                ReturnCodes.NotFound),
            MotorSorgulaOutcome.DatabaseBusy => ProblemResult(
                StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu; istek daha sonra tekrar denenmelidir.",
                ReturnCodes.DatabaseError),
            _ => ProblemResult(
                StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası",
                "Poliçe okunurken veritabanı hatası oluştu.",
                ReturnCodes.DatabaseError)
        };
    }

    private static ObjectResult ProblemResult(int status, string type, string title, string detail, string returnCode)
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
namespace GenApp.Llm.MotorSorgula;

public sealed class MotorPolicyResponse
{
    private const string LastChangedFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    public int PolicyNumber { get; init; }
    public int CustomerNumber { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateOnly ExpiryDate { get; init; }
    public string LastChanged { get; init; } = "";
    public long BrokerId { get; init; }
    public string BrokersReference { get; init; } = "";
    public int Payment { get; init; }
    public string Make { get; init; } = "";
    public string Model { get; init; } = "";
    public int Value { get; init; }
    public string RegistrationNumber { get; init; } = "";
    public string Colour { get; init; } = "";
    public int Cc { get; init; }
    public DateOnly ManufactureDate { get; init; }
    public int Premium { get; init; }
    public int Accidents { get; init; }

    public static MotorPolicyResponse From(MotorPolicyRow row) => new()
    {
        PolicyNumber = row.PolicyNumber,
        CustomerNumber = row.CustomerNumber,
        IssueDate = row.IssueDate,
        ExpiryDate = row.ExpiryDate,
        LastChanged = row.LastChanged.ToString(LastChangedFormat, System.Globalization.CultureInfo.InvariantCulture),
        BrokerId = row.BrokerId,
        BrokersReference = Trim(row.BrokersReference),
        Payment = row.Payment,
        Make = Trim(row.Make),
        Model = Trim(row.Model),
        Value = row.Value,
        RegistrationNumber = Trim(row.RegistrationNumber),
        Colour = Trim(row.Colour),
        Cc = row.Cc,
        ManufactureDate = row.ManufactureDate,
        Premium = row.Premium,
        Accidents = row.Accidents
    };

    private static string Trim(string? value) => (value ?? "").TrimEnd();
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorSorgulaService.cs
```csharp
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public enum MotorSorgulaOutcome
{
    Success,
    NotFound,
    DatabaseError,
    DatabaseBusy
}

public sealed record MotorSorgulaResult(MotorSorgulaOutcome Outcome, string ReturnCode, MotorPolicyRow? Row);

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
            // Bilinçli sapma (Karar 8): kilitlenme/zaman aşımı 503 + "90"
            _logger.LogWarning(ex, "LGIPDB01 SELECT MOTOR CNUM={Customer} PNUM={Policy}: veritabanı meşgul", customerNumber, policyNumber);
            return new MotorSorgulaResult(MotorSorgulaOutcome.DatabaseBusy, ReturnCodes.DatabaseError, null);
        }
        catch (Exception ex) when (ex is NpgsqlException or NullColumnException or InvalidCastException)
        {
            // lgipdb01.cbl GET-MOTOR-DB2-INFO: SQLCODE 0 ya da 100 dışında '90' ve hata mesajı (WRITE-ERROR-MESSAGE → ILogger)
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={Customer} PNUM={Policy}: veritabanı hatası", customerNumber, policyNumber);
            return new MotorSorgulaResult(MotorSorgulaOutcome.DatabaseError, ReturnCodes.DatabaseError, null);
        }

        // lgipdb01.cbl GET-MOTOR-DB2-INFO: SQLCODE = 100 → '01'
        if (row is null)
        {
            return new MotorSorgulaResult(MotorSorgulaOutcome.NotFound, ReturnCodes.NotFound, null);
        }

        // Bilinçli sapma: poliçe türü motor ('M') değilse 404 / "01"
        if (!string.Equals(row.PolicyType?.Trim(), MotorPolicyType, StringComparison.Ordinal))
        {
            return new MotorSorgulaResult(MotorSorgulaOutcome.NotFound, ReturnCodes.NotFound, null);
        }

        return new MotorSorgulaResult(MotorSorgulaOutcome.Success, ReturnCodes.Success, row);
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            // 40P01 deadlock_detected, 55P03 lock_not_available, 57014 query_canceled (statement_timeout)
            return pg.SqlState is "40P01" or "55P03" or "57014";
        }
        if (ex is NpgsqlException npg && npg.InnerException is TimeoutException)
        {
            return true;
        }
        return ex is TimeoutException;
    }
}
```

### dosya: src/GenApp.Api/Llm/MotorSorgula/MotorSorgulaRepository.cs
```csharp
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

public sealed record MotorPolicyRow(
    int PolicyNumber,
    int CustomerNumber,
    string? PolicyType,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    DateTime LastChanged,
    long BrokerId,
    string? BrokersReference,
    int Payment,
    string? Make,
    string? Model,
    int Value,
    string? RegistrationNumber,
    string? Colour,
    int Cc,
    DateOnly ManufactureDate,
    int Premium,
    int Accidents);

/// <summary>Gösterge değişkeni olmayan bir kolon NULL döndü (Db2'de SQLCODE -305).</summary>
public sealed class NullColumnException : Exception
{
    public NullColumnException(string column) : base($"'{column}' kolonu NULL döndü (SQLCODE -305 karşılığı).") { }
}

public sealed class MotorSorgulaRepository
{
    private readonly string _connectionString;

    public MotorSorgulaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // lgipdb01.cbl GET-MOTOR-DB2-INFO: POLICY ile MOTOR birleşimi, müşteri ve poliçe numarasıyla tek satır
    private const string Sql = """
        SELECT p.policy_number,
               p.customer_number,
               p.policy_type,
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
          JOIN motor m ON m.policy_number = p.policy_number
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
            return null;
        }

        return new MotorPolicyRow(
            PolicyNumber: reader.GetInt32(0),
            CustomerNumber: reader.GetInt32(1),
            PolicyType: reader.IsDBNull(2) ? null : reader.GetString(2),
            IssueDate: Required<DateOnly>(reader, 3, "issue_date"),
            ExpiryDate: Required<DateOnly>(reader, 4, "expiry_date"),
            LastChanged: Required<DateTime>(reader, 5, "last_changed"),
            // lgipdb01.cbl: IND-BROKERID = -1 ise alan taşınmaz, INITIALIZE değeri (0) kalır
            BrokerId: reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
            // lgipdb01.cbl: IND-BROKERSREF ile NULL kabul edilir, INITIALIZE değeri (boşluk) kalır
            BrokersReference: reader.IsDBNull(7) ? "" : reader.GetString(7),
            // lgipdb01.cbl: IND-PAYMENT = -1 ise alan taşınmaz, INITIALIZE değeri (0) kalır
            Payment: reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            // Aşağıdaki kolonların COBOL'da gösterge değişkeni yok; NULL gelirse SQLCODE -305 → '90'
            Make: Required<string>(reader, 9, "make"),
            Model: Required<string>(reader, 10, "model"),
            Value: Required<int>(reader, 11, "value"),
            RegistrationNumber: Required<string>(reader, 12, "reg_number"),
            Colour: Required<string>(reader, 13, "colour"),
            Cc: Required<short>(reader, 14, "cc"),
            ManufactureDate: Required<DateOnly>(reader, 15, "year_of_manufacture"),
            Premium: Required<int>(reader, 16, "premium"),
            Accidents: Required<int>(reader, 17, "accidents"));
    }

    private static T Required<T>(NpgsqlDataReader reader, int ordinal, string column)
    {
        if (reader.IsDBNull(ordinal))
        {
            throw new NullColumnException(column);
        }
        return reader.GetFieldValue<T>(ordinal);
    }
}
```