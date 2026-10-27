### dosya: src/GenApp.Api/Llm/MusteriGuncelle/CustomerUpdateController.cs
```csharp
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
```

### dosya: src/GenApp.Api/Llm/MusteriGuncelle/UpdateCustomerRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// PUT /api/customers/{customerNumber} istek gövdesi. Uzunluklar lgcmarea.cpy'deki alan uzunluklarıdır.
/// Gövdede gönderilen customerNumber yok sayılır (bilinmeyen alan olarak deserileştirilmez).
/// </summary>
public sealed class UpdateCustomerRequest
{
    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? FirstName { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? LastName { get; init; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına gitmeden 400 ile reddedilir
    [Required]
    public DateOnly? DateOfBirth { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? HouseName { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(4)]
    public string? HouseNumber { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Postcode { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneMobile { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneHome { get; init; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(100)]
    public string? Email { get; init; }
}
```

### dosya: src/GenApp.Api/Llm/MusteriGuncelle/CustomerResponse.cs
```csharp
namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Müşteri cevabı. Metinlerin sonundaki boşluklar kırpılır, boş metin "" olarak döner.
/// </summary>
public sealed class CustomerResponse
{
    public int CustomerNumber { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public string HouseName { get; init; } = string.Empty;
    public string HouseNumber { get; init; } = string.Empty;
    public string Postcode { get; init; } = string.Empty;
    public string PhoneMobile { get; init; } = string.Empty;
    public string PhoneHome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
```

### dosya: src/GenApp.Api/Llm/MusteriGuncelle/CustomerUpdateService.cs
```csharp
using Npgsql;

namespace GenApp.Llm.MusteriGuncelle;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public sealed record CustomerUpdateData(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string Email);

public sealed record CustomerUpdateResult(string ReturnCode, CustomerResponse? Customer, bool DatabaseBusy = false);

/// <summary>
/// LGUCDB01'in UPDATE-CUSTOMER-INFO paragrafının karşılığı. LGUCVS01 (VSAM yeniden yazımı) yeni sistemde yoktur.
/// Müşteri güncellemesinde eşzamanlı güncelleme kontrolü yoktur; eski sistemde de yoktur.
/// </summary>
public sealed class CustomerUpdateService
{
    // 40P01: deadlock_detected, 55P03: lock_not_available, 57014: query_canceled (statement_timeout)
    private static readonly HashSet<string> BusySqlStates = ["40P01", "55P03", "57014"];

    private readonly CustomerUpdateRepository _repository;
    private readonly ILogger<CustomerUpdateService> _logger;

    public CustomerUpdateService(CustomerUpdateRepository repository, ILogger<CustomerUpdateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CustomerUpdateResult> UpdateAsync(int customerNumber, CustomerUpdateData data, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _repository.UpdateAsync(customerNumber, data, cancellationToken);

            // SQLCODE = 100 → '01' (lgucdb01.cbl:173-174); bilinçli sapma: VSAM'a gidilmez, 404 döner
            if (customer is null)
                return new CustomerUpdateResult(ReturnCodes.NotFound, null);

            return new CustomerUpdateResult(ReturnCodes.Success, customer);
        }
        catch (PostgresException ex) when (BusySqlStates.Contains(ex.SqlState))
        {
            _logger.LogError(ex, "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST SQLSTATE={SqlState} (kilitlenme/zaman aşımı)",
                customerNumber, ex.SqlState);
            return new CustomerUpdateResult(ReturnCodes.DatabaseError, null, DatabaseBusy: true);
        }
        catch (NpgsqlException ex) when (ex is not PostgresException && ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST zaman aşımı", customerNumber);
            return new CustomerUpdateResult(ReturnCodes.DatabaseError, null, DatabaseBusy: true);
        }
        catch (NpgsqlException ex)
        {
            // SQLCODE 0 ve 100 dışında → '90' ve hata kuyruğuna yazım (lgucdb01.cbl:175-178); kuyruk yerine ILogger
            _logger.LogError(ex, "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST veritabanı hatası", customerNumber);
            return new CustomerUpdateResult(ReturnCodes.DatabaseError, null);
        }
    }
}
```

### dosya: src/GenApp.Api/Llm/MusteriGuncelle/CustomerUpdateRepository.cs
```csharp
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// customer tablosunu günceller (lgucdb01.cbl UPDATE CUSTOMER ... WHERE CUSTOMERNUMBER = ...).
/// </summary>
public sealed class CustomerUpdateRepository
{
    private const string UpdateSql = """
        UPDATE customer
           SET first_name    = @first_name,
               last_name     = @last_name,
               date_of_birth = @date_of_birth,
               house_name    = @house_name,
               house_number  = @house_number,
               postcode      = @postcode,
               phone_mobile  = @phone_mobile,
               phone_home    = @phone_home,
               email_address = @email_address
         WHERE customer_number = @customer_number
        RETURNING customer_number, first_name, last_name, date_of_birth, house_name,
                  house_number, postcode, phone_mobile, phone_home, email_address
        """;

    private readonly string _connectionString;

    public CustomerUpdateRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Güncellenen kaydı döner; kayıt yoksa null.</summary>
    public async Task<CustomerResponse?> UpdateAsync(int customerNumber, CustomerUpdateData data, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        CustomerResponse? result = null;

        await using (var command = new NpgsqlCommand(UpdateSql, connection, transaction))
        {
            command.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = customerNumber });
            command.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = data.FirstName });
            command.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = data.LastName });
            command.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = data.DateOfBirth });
            command.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = data.HouseName });
            command.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = data.HouseNumber });
            command.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = data.Postcode });
            command.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = data.PhoneMobile });
            command.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = data.PhoneHome });
            command.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = data.Email });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                result = new CustomerResponse
                {
                    CustomerNumber = reader.GetInt32(0),
                    FirstName = Text(reader, 1),
                    LastName = Text(reader, 2),
                    DateOfBirth = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
                    HouseName = Text(reader, 4),
                    HouseNumber = Text(reader, 5),
                    Postcode = Text(reader, 6),
                    PhoneMobile = Text(reader, 7),
                    PhoneHome = Text(reader, 8),
                    Email = Text(reader, 9)
                };
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static string Text(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal).TrimEnd();
}
```

### dosya: src/GenApp.Api/Llm/MusteriGuncelle/ValidationErrorFilterAttribute.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Hatalı istekleri API tasarımındaki biçimle (type: validation-error, errors listesi) 400 olarak döndürür.
/// [ApiController]'ın otomatik model doğrulama filtresinden (Order -2000) önce çalışır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidationErrorFilterAttribute : ActionFilterAttribute
{
    public ValidationErrorFilterAttribute()
    {
        Order = -3000;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
            return;

        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
            Detail = "İstekte eksik ya da hatalı alanlar var."
        };

        var result = new BadRequestObjectResult(problem);
        result.ContentTypes.Add("application/problem+json");
        context.Result = result;
    }
}
```