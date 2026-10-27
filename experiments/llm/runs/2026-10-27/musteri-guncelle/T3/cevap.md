### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MusteriGuncelle;

// [ApiController] bilerek kullanılmadı: otomatik 400 cevabının "type" alanı "validation-error" olmalı ve Program değiştirilemiyor.
[Route("api/customers")]
[Produces("application/json")]
public sealed class MusteriGuncelleController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MusteriGuncelleController> _logger;

    public MusteriGuncelleController(IConfiguration configuration, ILogger<MusteriGuncelleController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPut("{customerNumber:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MusteriGuncelleResponse), 200)]
    public async Task<IActionResult> Guncelle(
        [FromRoute] int customerNumber,
        [FromBody] MusteriGuncelleRequest? request,
        CancellationToken cancellationToken)
    {
        // EIBCALEN/98 karşılığı: eksik ya da hatalı alan içeren istek veritabanına gitmeden 400 ile reddedilir.
        if (!ModelState.IsValid || request is null)
        {
            if (request is null && ModelState.IsValid)
            {
                ModelState.AddModelError("request", "İstek gövdesi boş olamaz.");
            }
            return ValidationHatasi();
        }

        var connectionString = _configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings:GenApp yapılandırması bulunamadı.");

        var service = new MusteriGuncelleService(new MusteriGuncelleRepository(connectionString), _logger);
        var sonuc = await service.GuncelleAsync(customerNumber, request, cancellationToken);

        return sonuc.ReturnCode switch
        {
            MusteriGuncelleReturnCodes.Basarili => Ok(sonuc.Musteri),
            MusteriGuncelleReturnCodes.KayitYok => Problem(404, "not-found", "Kayıt bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.", MusteriGuncelleReturnCodes.KayitYok),
            MusteriGuncelleReturnCodes.VeritabaniHatasi when sonuc.VeritabaniMesgul => Problem(503, "database-busy",
                "Veritabanı meşgul", "Veritabanı kilitlenmesi ya da zaman aşımı oluştu; daha sonra tekrar deneyin.",
                MusteriGuncelleReturnCodes.VeritabaniHatasi),
            _ => Problem(500, "database-error", "Veritabanı hatası",
                "Müşteri güncellenirken veritabanı hatası oluştu.", MusteriGuncelleReturnCodes.VeritabaniHatasi),
        };
    }

    private IActionResult ValidationHatasi()
    {
        var problem = new ValidationProblemDetails(ModelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = 400,
            Detail = "İstekte eksik ya da hatalı alanlar var.",
        };
        var result = new ObjectResult(problem) { StatusCode = 400 };
        result.ContentTypes.Clear();
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private static IActionResult Problem(int status, string type, string title, string detail, string returnCode)
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
        result.ContentTypes.Clear();
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleRequest.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MusteriGuncelle;

// customerNumber istek gövdesinde yoktur; gönderilirse JSON çözümlemesinde yok sayılır (API tasarımı 1).
public sealed class MusteriGuncelleRequest
{
    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir.
    [Required]
    public DateOnly? DateOfBirth { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? HouseName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(4)]
    public string? HouseNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Postcode { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneMobile { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    public string? PhoneHome { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(100)]
    public string? Email { get; set; }
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleResponse.cs
```csharp
namespace GenApp.Llm.MusteriGuncelle;

public sealed class MusteriGuncelleResponse
{
    public int CustomerNumber { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public DateOnly? DateOfBirth { get; init; }
    public string HouseName { get; init; } = "";
    public string HouseNumber { get; init; } = "";
    public string Postcode { get; init; } = "";
    public string PhoneMobile { get; init; } = "";
    public string PhoneHome { get; init; } = "";
    public string Email { get; init; } = "";
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleResult.cs
```csharp
namespace GenApp.Llm.MusteriGuncelle;

public static class MusteriGuncelleReturnCodes
{
    public const string Basarili = "00";
    public const string KayitYok = "01";
    public const string VeritabaniHatasi = "90";
}

public sealed record MusteriGuncelleResult(
    string ReturnCode,
    MusteriGuncelleResponse? Musteri = null,
    bool VeritabaniMesgul = false);
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleService.cs
```csharp
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriGuncelle;

public sealed class MusteriGuncelleService
{
    private readonly MusteriGuncelleRepository _repository;
    private readonly ILogger _logger;

    public MusteriGuncelleService(MusteriGuncelleRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MusteriGuncelleResult> GuncelleAsync(
        int customerNumber, MusteriGuncelleRequest request, CancellationToken cancellationToken)
    {
        var girdi = new MusteriGuncelleRepository.MusteriAlanlari(
            FirstName: request.FirstName ?? "",
            LastName: request.LastName ?? "",
            DateOfBirth: request.DateOfBirth!.Value,
            HouseName: request.HouseName ?? "",
            HouseNumber: request.HouseNumber ?? "",
            // Posta kodu büyük harfe çevrilerek kaydedilir (eskide lgtestc1.cbl:188-189, yenide API).
            Postcode: (request.Postcode ?? "").ToUpperInvariant(),
            PhoneMobile: request.PhoneMobile ?? "",
            PhoneHome: request.PhoneHome ?? "",
            Email: request.Email ?? "");

        try
        {
            var musteri = await _repository.GuncelleAsync(customerNumber, girdi, cancellationToken);

            // lgucdb01.cbl: SQLCODE = 100 → '01'. VSAM adımı (LGUCVS01) yeni sistemde yok; LGV1 ABEND yerine 404 döner.
            if (musteri is null)
            {
                return new MusteriGuncelleResult(MusteriGuncelleReturnCodes.KayitYok);
            }

            return new MusteriGuncelleResult(MusteriGuncelleReturnCodes.Basarili, musteri);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException && !cancellationToken.IsCancellationRequested)
        {
            var mesgul = VeritabaniMesgulMu(ex);
            // WRITE-ERROR-MESSAGE (LGSTSQ) karşılığı: hata loglanır.
            _logger.LogError(ex, "LGUCDB01 UPDATE CUST başarısız. CNUM={CustomerNumber} SQLSTATE={SqlState}",
                customerNumber, (ex as PostgresException)?.SqlState);
            return new MusteriGuncelleResult(MusteriGuncelleReturnCodes.VeritabaniHatasi, VeritabaniMesgul: mesgul);
        }
    }

    private static bool VeritabaniMesgulMu(Exception ex)
    {
        if (ex is TimeoutException || ex.InnerException is TimeoutException)
        {
            return true;
        }

        if (ex is PostgresException pg)
        {
            return pg.SqlState is
                PostgresErrorCodes.DeadlockDetected or
                PostgresErrorCodes.LockNotAvailable or
                PostgresErrorCodes.QueryCanceled;
        }

        return false;
    }
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleRepository.cs
```csharp
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriGuncelle;

public sealed class MusteriGuncelleRepository
{
    private readonly string _connectionString;

    public MusteriGuncelleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public sealed record MusteriAlanlari(
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string HouseName,
        string HouseNumber,
        string Postcode,
        string PhoneMobile,
        string PhoneHome,
        string Email);

    // lgucdb01.cbl UPDATE-CUSTOMER-INFO karşılığı; tek SQL cümlesi olduğu için ayrı transaction açılmaz.
    public async Task<MusteriGuncelleResponse?> GuncelleAsync(
        int customerNumber, MusteriAlanlari m, CancellationToken cancellationToken)
    {
        const string sql = """
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

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = m.FirstName });
        command.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = m.LastName });
        command.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = m.DateOfBirth });
        command.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = m.HouseName });
        command.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = m.HouseNumber });
        command.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = m.Postcode });
        command.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = m.PhoneMobile });
        command.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = m.PhoneHome });
        command.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = m.Email });
        command.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = customerNumber });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MusteriGuncelleResponse
        {
            CustomerNumber = reader.GetInt32(0),
            FirstName = Metin(reader, 1),
            LastName = Metin(reader, 2),
            DateOfBirth = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
            HouseName = Metin(reader, 4),
            HouseNumber = Metin(reader, 5),
            Postcode = Metin(reader, 6),
            PhoneMobile = Metin(reader, 7),
            PhoneHome = Metin(reader, 8),
            Email = Metin(reader, 9),
        };
    }

    // Cevaptaki metinlerin sonundaki boşluklar kırpılır; null metin "" olarak döner (API tasarımı 1).
    private static string Metin(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).TrimEnd();
}
```