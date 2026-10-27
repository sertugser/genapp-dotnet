### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GenApp.Llm.MusteriEkle;

/// <summary>
/// Müşteri ekle (01ACUS). Eski programlar: LGACUS01 → LGACDB01 (+ LGACVS01, LGACDB02).
/// </summary>
[Route("api/customers")]
[Produces("application/json")]
public sealed class MusteriEkleController : ControllerBase
{
    private readonly MusteriEkleService _service;

    public MusteriEkleController(IConfiguration configuration, ILogger<MusteriEkleService> logger)
    {
        _service = new MusteriEkleService(configuration, logger);
    }

    [HttpPost("")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AddCustomer([FromBody] CustomerCreateRequest? request, CancellationToken cancellationToken)
    {
        // EIBCALEN / COMMAREA uzunluk kontrolünün (98) karşılığı: model doğrulaması (400 + errors)
        if (request is null && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "İstek gövdesi boş olamaz.");
        }

        if (!ModelState.IsValid || request is null)
        {
            return ValidationError(ModelState);
        }

        var result = await _service.AddAsync(request, cancellationToken);

        return result.Outcome switch
        {
            MusteriEkleOutcome.Success => Created($"/api/customers/{result.Customer!.CustomerNumber}", result.Customer),
            MusteriEkleOutcome.DatabaseBusy => Problem(
                StatusCodes.Status503ServiceUnavailable,
                "database-busy",
                "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı nedeniyle müşteri eklenemedi. Daha sonra tekrar deneyin.",
                "90"),
            _ => Problem(
                StatusCodes.Status500InternalServerError,
                "database-error",
                "Veritabanı hatası",
                "Müşteri eklenirken veritabanı hatası oluştu.",
                "90"),
        };
    }

    private ObjectResult ValidationError(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
            Detail = "İstekte eksik ya da hatalı alanlar var.",
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

### dosya: src/GenApp.Api/Llm/MusteriEkle/CustomerCreateRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriEkle;

/// <summary>
/// Müşteri ekle isteği. Uzunluklar lgcmarea.cpy'deki CA-CUSTOMER-REQUEST alanlarındandır.
/// customerNumber gönderilirse yok sayılır (lgacdb01.cbl: numara her zaman yeniden üretilir).
/// </summary>
public sealed class CustomerCreateRequest
{
    [Required(AllowEmptyStrings = true)]
    [MaxLength(10)]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir
    [Required]
    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("houseName")]
    public string? HouseName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(4)]
    [JsonPropertyName("houseNumber")]
    public string? HouseNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(8)]
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("phoneMobile")]
    public string? PhoneMobile { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(20)]
    [JsonPropertyName("phoneHome")]
    public string? PhoneHome { get; set; }

    [Required(AllowEmptyStrings = true)]
    [MaxLength(100)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/CustomerResponse.cs
```csharp
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriEkle;

/// <summary>
/// Müşteri cevabı. Metinlerin sonundaki boşluklar kırpılır; boş metin "" döner.
/// </summary>
public sealed class CustomerResponse
{
    [JsonPropertyName("customerNumber")]
    public int CustomerNumber { get; init; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = "";

    [JsonPropertyName("dateOfBirth")]
    public DateOnly DateOfBirth { get; init; }

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

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleResult.cs
```csharp
namespace GenApp.Llm.MusteriEkle;

public enum MusteriEkleOutcome
{
    /// <summary>CA-RETURN-CODE = '00'</summary>
    Success,

    /// <summary>CA-RETURN-CODE = '90' (lgacdb01.cbl INSERT-CUSTOMER, SQLCODE NOT EQUAL 0)</summary>
    DatabaseError,

    /// <summary>Kilitlenme / zaman aşımı: 503, returnCode "90" (bilinçli sapma, Karar 8)</summary>
    DatabaseBusy,
}

public sealed record MusteriEkleResult(MusteriEkleOutcome Outcome, CustomerResponse? Customer)
{
    public string ReturnCode => Outcome == MusteriEkleOutcome.Success ? "00" : "90";
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleService.cs
```csharp
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriEkle;

/// <summary>
/// LGACUS01 + LGACDB01 + LGACDB02 iş mantığı. LGACVS01'in (VSAM yazımı) karşılığı yoktur;
/// veri zaten customer tablosundadır. Tek bir PostgreSQL transaction'ı kullanılır.
/// </summary>
public sealed class MusteriEkleService
{
    // lgacdb01.cbl: MOVE '5732fec825535eeafb8fac50fee3a8aa' TO D2-CUSTSECR-PASS
    private const string DefaultPassword = "5732fec825535eeafb8fac50fee3a8aa";
    // lgacdb01.cbl: MOVE 'N' TO D2-CUSTSECR-STATE
    private const string DefaultState = "N";
    // lgacdb01.cbl: MOVE '0000' TO D2-CUSTSECR-COUNT
    private const int DefaultPassChanges = 0;

    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public MusteriEkleService(IConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MusteriEkleResult> AddAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        var firstName = request.FirstName ?? "";
        var lastName = request.LastName ?? "";
        var dateOfBirth = request.DateOfBirth!.Value;
        var houseName = request.HouseName ?? "";
        var houseNumber = request.HouseNumber ?? "";
        // Posta kodu büyük harfe çevrilir (eskide lgtestc1.cbl:126-127, yenide API)
        var postcode = (request.Postcode ?? "").ToUpperInvariant();
        var phoneMobile = request.PhoneMobile ?? "";
        var phoneHome = request.PhoneHome ?? "";
        var email = request.Email ?? "";

        var connectionString = _configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");

        int customerNumber;

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            // INSERT-CUSTOMER: numara PostgreSQL identity ile üretilir (GENACUSTNUM sayacının karşılığı)
            await using (var insert = new NpgsqlCommand(
                """
                INSERT INTO customer
                    (first_name, last_name, date_of_birth, house_name, house_number,
                     postcode, phone_mobile, phone_home, email_address)
                VALUES
                    (@first_name, @last_name, @date_of_birth, @house_name, @house_number,
                     @postcode, @phone_mobile, @phone_home, @email_address)
                RETURNING customer_number
                """, connection, transaction))
            {
                insert.Parameters.Add(new NpgsqlParameter<string>("first_name", NpgsqlDbType.Varchar) { TypedValue = firstName });
                insert.Parameters.Add(new NpgsqlParameter<string>("last_name", NpgsqlDbType.Varchar) { TypedValue = lastName });
                insert.Parameters.Add(new NpgsqlParameter<DateOnly>("date_of_birth", NpgsqlDbType.Date) { TypedValue = dateOfBirth });
                insert.Parameters.Add(new NpgsqlParameter<string>("house_name", NpgsqlDbType.Varchar) { TypedValue = houseName });
                insert.Parameters.Add(new NpgsqlParameter<string>("house_number", NpgsqlDbType.Varchar) { TypedValue = houseNumber });
                insert.Parameters.Add(new NpgsqlParameter<string>("postcode", NpgsqlDbType.Varchar) { TypedValue = postcode });
                insert.Parameters.Add(new NpgsqlParameter<string>("phone_mobile", NpgsqlDbType.Varchar) { TypedValue = phoneMobile });
                insert.Parameters.Add(new NpgsqlParameter<string>("phone_home", NpgsqlDbType.Varchar) { TypedValue = phoneHome });
                insert.Parameters.Add(new NpgsqlParameter<string>("email_address", NpgsqlDbType.Varchar) { TypedValue = email });

                var scalar = await insert.ExecuteScalarAsync(cancellationToken);
                customerNumber = Convert.ToInt32(scalar);
            }

            // LGACDB02 (02ACUS): customer_secure satırı
            await transaction.SaveAsync("customer_secure", cancellationToken);
            try
            {
                await using var secure = new NpgsqlCommand(
                    """
                    INSERT INTO customer_secure
                        (customer_number, customer_pass, state_indicator, pass_changes)
                    VALUES
                        (@customer_number, @customer_pass, @state_indicator, @pass_changes)
                    """, connection, transaction);
                secure.Parameters.Add(new NpgsqlParameter<int>("customer_number", NpgsqlDbType.Integer) { TypedValue = customerNumber });
                secure.Parameters.Add(new NpgsqlParameter<string>("customer_pass", NpgsqlDbType.Varchar) { TypedValue = DefaultPassword });
                secure.Parameters.Add(new NpgsqlParameter<string>("state_indicator", NpgsqlDbType.Varchar) { TypedValue = DefaultState });
                secure.Parameters.Add(new NpgsqlParameter<int>("pass_changes", NpgsqlDbType.Integer) { TypedValue = DefaultPassChanges });
                await secure.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException ex) when (!IsBusy(ex))
            {
                // lgacdb02.cbl '98'i yalnızca D2-RETURN-CODE'a yazar; LGACDB01 bunu okumaz, çağırana '00' döner ve müşteri satırı kalır
                await transaction.RollbackAsync("customer_secure", cancellationToken);
                _logger.LogError(ex,
                    "LGACDB02 CNUM={CustomerNumber} INSERT SECURITY SQLSTATE={SqlState}",
                    customerNumber, ex.SqlState);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            _logger.LogWarning(ex, "LGACDB01 INSERT CUSTOMER: veritabanı kilitlenmesi ya da zaman aşımı");
            return new MusteriEkleResult(MusteriEkleOutcome.DatabaseBusy, null);
        }
        catch (NpgsqlException ex)
        {
            // lgacdb01.cbl INSERT-CUSTOMER: IF SQLCODE NOT EQUAL 0 → MOVE '90' TO CA-RETURN-CODE
            _logger.LogError(ex, "LGACDB01 INSERT CUSTOMER başarısız, dönüş kodu 90");
            return new MusteriEkleResult(MusteriEkleOutcome.DatabaseError, null);
        }

        var customer = new CustomerResponse
        {
            CustomerNumber = customerNumber,
            FirstName = firstName.TrimEnd(),
            LastName = lastName.TrimEnd(),
            DateOfBirth = dateOfBirth,
            HouseName = houseName.TrimEnd(),
            HouseNumber = houseNumber.TrimEnd(),
            Postcode = postcode.TrimEnd(),
            PhoneMobile = phoneMobile.TrimEnd(),
            PhoneHome = phoneHome.TrimEnd(),
            Email = email.TrimEnd(),
        };

        return new MusteriEkleResult(MusteriEkleOutcome.Success, customer);
    }

    private static bool IsBusy(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg &&
                pg.SqlState is PostgresErrorCodes.DeadlockDetected
                    or PostgresErrorCodes.LockNotAvailable
                    or PostgresErrorCodes.QueryCanceled
                    or PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }

            if (current is TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
```