### dosya: src/GenApp.Api/Llm/MotorEkle/MotorEkleController.cs
```csharp
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
```

### dosya: src/GenApp.Api/Llm/MotorEkle/MotorEkleRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Motor poliçesi ekleme isteği (01AMOT). policyNumber, customerNumber ve lastChanged
/// yalnızca cevapta olduğundan burada yoktur; gönderilirse yok sayılır.
/// </summary>
public sealed class MotorEkleRequest
{
    // Db2 DATE sütunu geçersiz/boş tarihi kabul etmez (90); hatalı girdi veritabanına ulaşmadan reddedilir.
    [Required]
    public DateOnly? IssueDate { get; set; }

    [Required]
    public DateOnly? ExpiryDate { get; set; }

    // CA-BROKERID PIC 9(10); integer sütununa sığmayan değer veritabanı hatası (90) olur.
    [Range(0L, 9_999_999_999L)]
    public long BrokerId { get; set; }

    [MaxLength(10)]
    public string? BrokersReference { get; set; }

    [Range(0, 999_999)]
    public int Payment { get; set; }

    [MaxLength(15)]
    public string? Make { get; set; }

    [MaxLength(15)]
    public string? Model { get; set; }

    [Range(0, 999_999)]
    public int Value { get; set; }

    [MaxLength(7)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(8)]
    public string? Colour { get; set; }

    [Range(0, 9_999)]
    public int Cc { get; set; }

    // CA-M-MANUFACTURED, Db2 DATE sütununa (YEAROFMANUFACTURE) yazılır; geçersiz değer Db2'de 90 olurdu.
    [Required]
    public DateOnly? ManufactureDate { get; set; }

    [Range(0, 999_999)]
    public int Premium { get; set; }

    [Range(0, 999_999)]
    public int Accidents { get; set; }
}
```

### dosya: src/GenApp.Api/Llm/MotorEkle/MotorPolicyResponse.cs
```csharp
namespace GenApp.Llm.MotorEkle;

public sealed record MotorPolicyResponse(
    int PolicyNumber,
    int CustomerNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    string LastChanged,
    long BrokerId,
    string BrokersReference,
    int Payment,
    string Make,
    string Model,
    int Value,
    string RegistrationNumber,
    string Colour,
    int Cc,
    DateOnly ManufactureDate,
    int Premium,
    int Accidents);
```

### dosya: src/GenApp.Api/Llm/MotorEkle/ReturnCodes.cs
```csharp
namespace GenApp.Llm.MotorEkle;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string CustomerNotFound = "70";
    public const string DatabaseError = "90";
}

public sealed record MotorEkleResult(string ReturnCode, bool Busy, MotorPolicyResponse? Policy)
{
    public static MotorEkleResult Ok(MotorPolicyResponse policy) => new(ReturnCodes.Success, false, policy);
    public static MotorEkleResult CustomerNotFound() => new(ReturnCodes.CustomerNotFound, false, null);
    public static MotorEkleResult DatabaseError() => new(ReturnCodes.DatabaseError, false, null);
    public static MotorEkleResult DatabaseBusy() => new(ReturnCodes.DatabaseError, true, null);
}
```

### dosya: src/GenApp.Api/Llm/MotorEkle/MotorEkleService.cs
```csharp
namespace GenApp.Llm.MotorEkle;

/// <summary>
/// LGAPOL01 + LGAPDB01 (01AMOT) karşılığı. LGAPVS01'in VSAM yazımının karşılığı yoktur;
/// veri zaten PostgreSQL'e yazılır.
/// </summary>
public sealed class MotorEkleService
{
    private readonly MotorEkleRepository _repository;

    public MotorEkleService(MotorEkleRepository repository)
    {
        _repository = repository;
    }

    public async Task<MotorEkleResult> AddAsync(int customerNumber, MotorEkleRequest request, CancellationToken ct)
    {
        var data = new NewMotorPolicy(
            CustomerNumber: customerNumber,
            IssueDate: request.IssueDate!.Value,
            ExpiryDate: request.ExpiryDate!.Value,
            BrokerId: request.BrokerId,
            BrokersReference: request.BrokersReference ?? string.Empty,
            Payment: request.Payment,
            Make: request.Make ?? string.Empty,
            Model: request.Model ?? string.Empty,
            Value: request.Value,
            RegistrationNumber: request.RegistrationNumber ?? string.Empty,
            Colour: request.Colour ?? string.Empty,
            Cc: (short)request.Cc,
            ManufactureDate: request.ManufactureDate!.Value,
            Premium: request.Premium,
            Accidents: request.Accidents);

        return await _repository.InsertAsync(data, ct);
    }
}

public sealed record NewMotorPolicy(
    int CustomerNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    long BrokerId,
    string BrokersReference,
    int Payment,
    string Make,
    string Model,
    int Value,
    string RegistrationNumber,
    string Colour,
    short Cc,
    DateOnly ManufactureDate,
    int Premium,
    int Accidents);
```

### dosya: src/GenApp.Api/Llm/MotorEkle/MotorEkleRepository.cs
```csharp
using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MotorEkle;

public sealed class MotorEkleRepository
{
    private const string LastChangedFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    private readonly string _connectionString;
    private readonly ILogger<MotorEkleRepository> _logger;

    public MotorEkleRepository(string connectionString, ILogger<MotorEkleRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<MotorEkleResult> InsertAsync(NewMotorPolicy p, CancellationToken ct)
    {
        NpgsqlConnection? conn = null;
        NpgsqlTransaction? tx = null;
        try
        {
            conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            tx = await conn.BeginTransactionAsync(ct);

            // INSERT-POLICY (lgapdb01.cbl): numarayı veritabanı verir, LASTCHANGED = CURRENT TIMESTAMP.
            int policyNumber;
            DateTime lastChanged;
            try
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO policy
                        (customer_number, issue_date, expiry_date, policy_type, last_changed,
                         broker_id, brokers_reference, payment)
                    VALUES
                        (@customer_number, @issue_date, @expiry_date, 'M', CURRENT_TIMESTAMP,
                         @broker_id, @brokers_reference, @payment)
                    RETURNING policy_number, last_changed
                    """, conn, tx);
                cmd.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = p.CustomerNumber });
                cmd.Parameters.Add(new NpgsqlParameter("issue_date", NpgsqlDbType.Date) { Value = p.IssueDate });
                cmd.Parameters.Add(new NpgsqlParameter("expiry_date", NpgsqlDbType.Date) { Value = p.ExpiryDate });
                cmd.Parameters.Add(new NpgsqlParameter("broker_id", NpgsqlDbType.Bigint) { Value = p.BrokerId });
                cmd.Parameters.Add(new NpgsqlParameter("brokers_reference", NpgsqlDbType.Varchar) { Value = p.BrokersReference });
                cmd.Parameters.Add(new NpgsqlParameter("payment", NpgsqlDbType.Integer) { Value = p.Payment });

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                await reader.ReadAsync(ct);
                policyNumber = reader.GetInt32(0);
                lastChanged = reader.GetDateTime(1);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                // SQLCODE -530 → '70' (lgapdb01.cbl INSERT-POLICY)
                _logger.LogWarning(ex, "LGAPDB01 INSERT POLICY CNUM={CustomerNumber}: müşteri bulunamadı (70)", p.CustomerNumber);
                await SafeRollbackAsync(tx);
                return MotorEkleResult.CustomerNotFound();
            }

            // INSERT-MOTOR: hata olursa ABEND LGSQ ile POLICY satırı da geri alınır.
            await using (var cmd = new NpgsqlCommand(
                """
                INSERT INTO motor
                    (policy_number, make, model, value, reg_number, colour, cc,
                     year_of_manufacture, premium, accidents)
                VALUES
                    (@policy_number, @make, @model, @value, @reg_number, @colour, @cc,
                     @year_of_manufacture, @premium, @accidents)
                """, conn, tx))
            {
                cmd.Parameters.Add(new NpgsqlParameter("policy_number", NpgsqlDbType.Integer) { Value = policyNumber });
                cmd.Parameters.Add(new NpgsqlParameter("make", NpgsqlDbType.Varchar) { Value = p.Make });
                cmd.Parameters.Add(new NpgsqlParameter("model", NpgsqlDbType.Varchar) { Value = p.Model });
                cmd.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Integer) { Value = p.Value });
                cmd.Parameters.Add(new NpgsqlParameter("reg_number", NpgsqlDbType.Varchar) { Value = p.RegistrationNumber });
                cmd.Parameters.Add(new NpgsqlParameter("colour", NpgsqlDbType.Varchar) { Value = p.Colour });
                cmd.Parameters.Add(new NpgsqlParameter("cc", NpgsqlDbType.Smallint) { Value = p.Cc });
                cmd.Parameters.Add(new NpgsqlParameter("year_of_manufacture", NpgsqlDbType.Date) { Value = p.ManufactureDate });
                cmd.Parameters.Add(new NpgsqlParameter("premium", NpgsqlDbType.Integer) { Value = p.Premium });
                cmd.Parameters.Add(new NpgsqlParameter("accidents", NpgsqlDbType.Integer) { Value = p.Accidents });
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);

            return MotorEkleResult.Ok(new MotorPolicyResponse(
                PolicyNumber: policyNumber,
                CustomerNumber: p.CustomerNumber,
                IssueDate: p.IssueDate,
                ExpiryDate: p.ExpiryDate,
                LastChanged: lastChanged.ToString(LastChangedFormat, CultureInfo.InvariantCulture),
                BrokerId: p.BrokerId,
                BrokersReference: p.BrokersReference.TrimEnd(),
                Payment: p.Payment,
                Make: p.Make.TrimEnd(),
                Model: p.Model.TrimEnd(),
                Value: p.Value,
                RegistrationNumber: p.RegistrationNumber.TrimEnd(),
                Colour: p.Colour.TrimEnd(),
                Cc: p.Cc,
                ManufactureDate: p.ManufactureDate,
                Premium: p.Premium,
                Accidents: p.Accidents));
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            _logger.LogError(ex, "LGAPDB01 CNUM={CustomerNumber}: veritabanı meşgul/zaman aşımı (90, 503)", p.CustomerNumber);
            await SafeRollbackAsync(tx);
            return MotorEkleResult.DatabaseBusy();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "LGAPDB01 CNUM={CustomerNumber}: veritabanı hatası (90)", p.CustomerNumber);
            await SafeRollbackAsync(tx);
            return MotorEkleResult.DatabaseError();
        }
        catch (InvalidCastException ex)
        {
            _logger.LogError(ex, "LGAPDB01 CNUM={CustomerNumber}: veritabanı hatası (90)", p.CustomerNumber);
            await SafeRollbackAsync(tx);
            return MotorEkleResult.DatabaseError();
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
            if (conn is not null) await conn.DisposeAsync();
        }
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is TimeoutException) return true;
        if (ex is PostgresException pg)
        {
            return pg.SqlState is PostgresErrorCodes.DeadlockDetected
                or PostgresErrorCodes.LockNotAvailable
                or PostgresErrorCodes.QueryCanceled
                or PostgresErrorCodes.SerializationFailure;
        }
        return ex is NpgsqlException && ex.InnerException is TimeoutException;
    }

    private async Task SafeRollbackAsync(NpgsqlTransaction? tx)
    {
        if (tx is null) return;
        try
        {
            await tx.RollbackAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geri alma sırasında hata");
        }
    }
}
```

### dosya: src/GenApp.Api/Llm/MotorEkle/MotorEkleValidationFilter.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Geçersiz model durumunda 400'ü API tasarımındaki biçimde döner (type: validation-error, errors listesi).
/// ApiController'ın kendi filtresinden (Order -2000) önce çalışır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class MotorEkleValidationFilterAttribute : ActionFilterAttribute
{
    public MotorEkleValidationFilterAttribute()
    {
        Order = -3000;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
            Detail = "İstekte eksik ya da hatalı alanlar var.",
        };

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        context.Result = result;
    }
}
```