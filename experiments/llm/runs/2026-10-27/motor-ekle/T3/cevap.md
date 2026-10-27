### dosya: src/GenApp.Llm/MotorEkle/MotorPolicyAddController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MotorEkle;

// [ApiController] bilinçli olarak kullanılmadı: otomatik 400 cevabı yerine "validation-error" türlü gövde üretmek için
// model doğrulaması aksiyon içinde yapılır (Program sınıfına ve diğer operasyonlara dokunmadan).
[Route("api/customers/{customerNumber:int}/motor-policies")]
public sealed class MotorPolicyAddController : ControllerBase
{
    private readonly MotorPolicyAddService _service;

    public MotorPolicyAddController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
        var repository = new MotorPolicyAddRepository(connectionString);
        _service = new MotorPolicyAddService(repository, loggerFactory.CreateLogger<MotorPolicyAddService>());
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MotorPolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AddAsync(
        [FromRoute] int customerNumber,
        [FromBody] MotorPolicyAddRequest? request,
        CancellationToken cancellationToken)
    {
        // EIBCALEN / '98' karşılığı: eksik ya da hatalı alan içeren istek 400 ile reddedilir.
        if (!ModelState.IsValid || request is null)
        {
            if (request is null && ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "İstek gövdesi boş olamaz.");
            }
            return ValidationError(ModelState);
        }

        var result = await _service.AddAsync(customerNumber, request, cancellationToken);

        if (result.ReturnCode == ReturnCodes.Success && result.Policy is not null)
        {
            var location = $"/api/customers/{customerNumber}/motor-policies/{result.Policy.PolicyNumber}";
            return Created(location, result.Policy);
        }

        if (result.DatabaseBusy)
        {
            return Problem(StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı nedeniyle işlem yapılamadı. Lütfen tekrar deneyin.",
                ReturnCodes.DatabaseError);
        }

        return result.ReturnCode switch
        {
            ReturnCodes.CustomerNotFound => Problem(StatusCodes.Status404NotFound, "customer-not-found",
                "Müşteri bulunamadı", $"{customerNumber} numaralı müşteri bulunamadı.", ReturnCodes.CustomerNotFound),
            _ => Problem(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatası",
                "Poliçe eklenirken veritabanı hatası oluştu.", ReturnCodes.DatabaseError),
        };
    }

    private static ObjectResult ValidationError(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
        };
        var result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private static ObjectResult Problem(int status, string type, string title, string detail, string returnCode)
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

### dosya: src/GenApp.Llm/MotorEkle/MotorPolicyAddRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MotorEkle;

// policyNumber, customerNumber ve lastChanged istekte gelirse yok sayılır (yalnızca cevapta / sunucu üretir).
public sealed class MotorPolicyAddRequest
{
    [Required]
    public DateOnly? IssueDate { get; set; }

    [Required]
    public DateOnly? ExpiryDate { get; set; }

    // Eklemede CA-LASTCHANGED kullanılmaz; değer veritabanında CURRENT TIMESTAMP ile üretilir (lgapdb01 INSERT-POLICY).
    public string? LastChanged { get; set; }

    [Required]
    [Range(typeof(long), "0", "9999999999")]
    public long? BrokerId { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? BrokersReference { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Payment { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Make { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Model { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Value { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(7)]
    public string? RegistrationNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Colour { get; set; }

    [Required]
    [Range(0, 9999)]
    public int? Cc { get; set; }

    [Required]
    public DateOnly? ManufactureDate { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Premium { get; set; }

    [Required]
    [Range(0, 999999)]
    public int? Accidents { get; set; }
}
```

### dosya: src/GenApp.Llm/MotorEkle/MotorPolicyResponse.cs
```csharp
namespace GenApp.Llm.MotorEkle;

public sealed class MotorPolicyResponse
{
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
}
```

### dosya: src/GenApp.Llm/MotorEkle/ReturnCodes.cs
```csharp
namespace GenApp.Llm.MotorEkle;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string CustomerNotFound = "70";
    public const string DatabaseError = "90";
}

public sealed record MotorPolicyAddResult(string ReturnCode, MotorPolicyResponse? Policy, bool DatabaseBusy = false);
```

### dosya: src/GenApp.Llm/MotorEkle/MotorPolicyAddService.cs
```csharp
using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorEkle;

public sealed class MotorPolicyAddService
{
    private readonly MotorPolicyAddRepository _repository;
    private readonly ILogger<MotorPolicyAddService> _logger;

    public MotorPolicyAddService(MotorPolicyAddRepository repository, ILogger<MotorPolicyAddService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MotorPolicyAddResult> AddAsync(int customerNumber, MotorPolicyAddRequest request,
        CancellationToken cancellationToken)
    {
        var data = new MotorPolicyInsertData(
            customerNumber,
            request.IssueDate!.Value,
            request.ExpiryDate!.Value,
            request.BrokerId!.Value,
            request.BrokersReference ?? "",
            request.Payment!.Value,
            request.Make ?? "",
            request.Model ?? "",
            request.Value!.Value,
            request.RegistrationNumber ?? "",
            request.Colour ?? "",
            (short)request.Cc!.Value,
            request.ManufactureDate!.Value,
            request.Premium!.Value,
            request.Accidents!.Value);

        InsertOutcome outcome;
        try
        {
            outcome = await _repository.InsertAsync(data, cancellationToken);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            // LGSTSQ (GENAERRS kuyruğu) karşılığı: loglama.
            _logger.LogError(ex, "LGAPDB01 motor poliçe ekleme: veritabanı hatası. CNUM={CustomerNumber}", customerNumber);
            return new MotorPolicyAddResult(ReturnCodes.DatabaseError, null, IsBusy(ex));
        }

        switch (outcome.Status)
        {
            case InsertStatus.CustomerNotFound:
                // lgapdb01 INSERT-POLICY: SQLCODE -530 (yabancı anahtar ihlali) → '70'
                _logger.LogWarning(outcome.Error, "LGAPDB01 INSERT POLICY: müşteri bulunamadı. CNUM={CustomerNumber}",
                    customerNumber);
                return new MotorPolicyAddResult(ReturnCodes.CustomerNotFound, null);

            case InsertStatus.PolicyInsertFailed:
            case InsertStatus.MotorInsertFailed:
                // INSERT POLICY 'When Other' → '90'; INSERT MOTOR hatası → '90' + ABEND LGSQ (POLICY satırı geri alınır)
                _logger.LogError(outcome.Error, "LGAPDB01 {Step}: veritabanı hatası. CNUM={CustomerNumber}",
                    outcome.Status == InsertStatus.PolicyInsertFailed ? "INSERT POLICY" : "INSERT MOTOR", customerNumber);
                return new MotorPolicyAddResult(ReturnCodes.DatabaseError, null,
                    outcome.Error is not null && IsBusy(outcome.Error));
        }

        var response = new MotorPolicyResponse
        {
            PolicyNumber = outcome.PolicyNumber,
            CustomerNumber = customerNumber,
            IssueDate = data.IssueDate,
            ExpiryDate = data.ExpiryDate,
            LastChanged = outcome.LastChanged.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
            BrokerId = data.BrokerId,
            BrokersReference = data.BrokersReference.TrimEnd(),
            Payment = data.Payment,
            Make = data.Make.TrimEnd(),
            Model = data.Model.TrimEnd(),
            Value = data.Value,
            RegistrationNumber = data.RegistrationNumber.TrimEnd(),
            Colour = data.Colour.TrimEnd(),
            Cc = data.Cc,
            ManufactureDate = data.ManufactureDate,
            Premium = data.Premium,
            Accidents = data.Accidents,
        };
        return new MotorPolicyAddResult(ReturnCodes.Success, response);
    }

    // API tasarımı: kilitlenme ve zaman aşımı → 503 database-busy, returnCode "90" (Karar 8).
    private static bool IsBusy(Exception ex)
    {
        for (var e = (Exception?)ex; e is not null; e = e.InnerException)
        {
            if (e is TimeoutException) return true;
            if (e is PostgresException pg &&
                pg.SqlState is PostgresErrorCodes.DeadlockDetected
                    or PostgresErrorCodes.LockNotAvailable
                    or PostgresErrorCodes.QueryCanceled
                    or PostgresErrorCodes.SerializationFailure)
                return true;
        }
        return false;
    }
}
```

### dosya: src/GenApp.Llm/MotorEkle/MotorPolicyAddRepository.cs
```csharp
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MotorEkle;

public sealed record MotorPolicyInsertData(
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

public enum InsertStatus
{
    Success,
    CustomerNotFound,
    PolicyInsertFailed,
    MotorInsertFailed,
}

public sealed record InsertOutcome(InsertStatus Status, int PolicyNumber, DateTime LastChanged, Exception? Error);

public sealed class MotorPolicyAddRepository
{
    private readonly string _connectionString;

    public MotorPolicyAddRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Db2 + VSAM iki aşamalı commit'in yerini tek PostgreSQL transaction'ı alır; VSAM yazımı (LGAPVS01) yoktur.
    public async Task<InsertOutcome> InsertAsync(MotorPolicyInsertData data, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int policyNumber;
        DateTime lastChanged;

        // INSERT-POLICY: numarayı veritabanı üretir, LASTCHANGED = CURRENT TIMESTAMP
        await using (var cmd = new NpgsqlCommand(
            """
            INSERT INTO policy
                (customer_number, issue_date, expiry_date, policy_type, last_changed,
                 broker_id, brokers_reference, payment)
            VALUES
                (@customer_number, @issue_date, @expiry_date, 'M', CURRENT_TIMESTAMP,
                 @broker_id, @brokers_reference, @payment)
            RETURNING policy_number, last_changed
            """, connection, transaction))
        {
            cmd.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = data.CustomerNumber });
            cmd.Parameters.Add(new NpgsqlParameter("issue_date", NpgsqlDbType.Date) { Value = data.IssueDate });
            cmd.Parameters.Add(new NpgsqlParameter("expiry_date", NpgsqlDbType.Date) { Value = data.ExpiryDate });
            // CA-BROKERID 10 hanelidir, kolon integer'dır; sınırı aşan değer PostgreSQL hatasıyla '90' olur (COBOL'da sessiz kesme vardı).
            cmd.Parameters.Add(new NpgsqlParameter("broker_id", NpgsqlDbType.Bigint) { Value = data.BrokerId });
            cmd.Parameters.Add(new NpgsqlParameter("brokers_reference", NpgsqlDbType.Varchar) { Value = data.BrokersReference });
            cmd.Parameters.Add(new NpgsqlParameter("payment", NpgsqlDbType.Integer) { Value = data.Payment });

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                await reader.ReadAsync(cancellationToken);
                policyNumber = reader.GetInt32(0);
                lastChanged = reader.GetDateTime(1);
            }
            catch (PostgresException ex)
            {
                await SafeRollbackAsync(transaction);
                // SQLCODE -530 karşılığı: yabancı anahtar ihlali (müşteri yok) → '70'
                var status = ex.SqlState == PostgresErrorCodes.ForeignKeyViolation
                    ? InsertStatus.CustomerNotFound
                    : InsertStatus.PolicyInsertFailed;
                return new InsertOutcome(status, 0, default, ex);
            }
        }

        // INSERT-MOTOR; kolon adları Db2 adlarının snake_case karşılığı varsayılmıştır (REGNUMBER, YEAROFMANUFACTURE).
        await using (var cmd = new NpgsqlCommand(
            """
            INSERT INTO motor
                (policy_number, make, model, value, reg_number, colour, cc,
                 year_of_manufacture, premium, accidents)
            VALUES
                (@policy_number, @make, @model, @value, @reg_number, @colour, @cc,
                 @year_of_manufacture, @premium, @accidents)
            """, connection, transaction))
        {
            cmd.Parameters.Add(new NpgsqlParameter("policy_number", NpgsqlDbType.Integer) { Value = policyNumber });
            cmd.Parameters.Add(new NpgsqlParameter("make", NpgsqlDbType.Varchar) { Value = data.Make });
            cmd.Parameters.Add(new NpgsqlParameter("model", NpgsqlDbType.Varchar) { Value = data.Model });
            cmd.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Integer) { Value = data.Value });
            cmd.Parameters.Add(new NpgsqlParameter("reg_number", NpgsqlDbType.Varchar) { Value = data.RegistrationNumber });
            cmd.Parameters.Add(new NpgsqlParameter("colour", NpgsqlDbType.Varchar) { Value = data.Colour });
            cmd.Parameters.Add(new NpgsqlParameter("cc", NpgsqlDbType.Smallint) { Value = data.Cc });
            cmd.Parameters.Add(new NpgsqlParameter("year_of_manufacture", NpgsqlDbType.Date) { Value = data.ManufactureDate });
            cmd.Parameters.Add(new NpgsqlParameter("premium", NpgsqlDbType.Integer) { Value = data.Premium });
            cmd.Parameters.Add(new NpgsqlParameter("accidents", NpgsqlDbType.Integer) { Value = data.Accidents });

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException ex)
            {
                // ABEND LGSQ karşılığı: POLICY satırı geri alınır, '90'
                await SafeRollbackAsync(transaction);
                return new InsertOutcome(InsertStatus.MotorInsertFailed, 0, default, ex);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new InsertOutcome(InsertStatus.Success, policyNumber, lastChanged, null);
    }

    private static async Task SafeRollbackAsync(NpgsqlTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (NpgsqlException)
        {
            // Bağlantı kopmuşsa geri alma zaten sunucu tarafında gerçekleşir.
        }
    }
}
```