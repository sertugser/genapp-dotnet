### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Müşteri güncelle (eski: LGUCUS01 → LGUCDB01 → LGUCVS01, istek kodu 01UCUS).
/// </summary>
[Route("api/customers/{customerNumber:int}")]
public sealed class MusteriGuncelleController : ControllerBase
{
    private readonly MusteriGuncelleService _service;

    public MusteriGuncelleController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        // Program sınıfına kayıt eklenemediği için bağımlılıklar burada kurulur.
        var repository = new MusteriGuncelleRepository(configuration);
        _service = new MusteriGuncelleService(repository, loggerFactory.CreateLogger<MusteriGuncelleService>());
    }

    [HttpPut]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MusteriCevap), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Guncelle(
        [FromRoute] int customerNumber,
        [FromBody] MusteriGuncelleIstek? istek,
        CancellationToken cancellationToken)
    {
        if (istek is null && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "İstek gövdesi boş olamaz.");
        }

        if (!ModelState.IsValid || istek is null)
        {
            return DogrulamaHatasi(ModelState);
        }

        var sonuc = await _service.GuncelleAsync(customerNumber, istek, cancellationToken);

        if (sonuc.ReturnCode == DonusKodlari.Basarili && sonuc.Musteri is not null)
        {
            return Ok(sonuc.Musteri);
        }

        return HataCevabi(customerNumber, sonuc);
    }

    private static ObjectResult DogrulamaHatasi(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Type = "validation-error",
            Title = "İstek geçersiz",
            Status = StatusCodes.Status400BadRequest,
            Detail = "İstekte eksik ya da hatalı alanlar var."
        };

        var sonuc = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        sonuc.ContentTypes.Add("application/problem+json");
        return sonuc;
    }

    private static ObjectResult HataCevabi(int customerNumber, MusteriGuncelleSonuc sonuc)
    {
        int status;
        string type;
        string title;
        string detail;

        if (sonuc.VeritabaniMesgul)
        {
            status = StatusCodes.Status503ServiceUnavailable;
            type = "database-busy";
            title = "Veritabanı meşgul";
            detail = "Veritabanı kilitlenmesi ya da zaman aşımı oluştu; istek daha sonra yeniden denenmelidir.";
        }
        else
        {
            switch (sonuc.ReturnCode)
            {
                case DonusKodlari.KayitYok:
                    status = StatusCodes.Status404NotFound;
                    type = "not-found";
                    title = "Kayıt bulunamadı";
                    detail = $"{customerNumber} numaralı müşteri bulunamadı.";
                    break;
                default:
                    status = StatusCodes.Status500InternalServerError;
                    type = "database-error";
                    title = "Veritabanı hatası";
                    detail = $"{customerNumber} numaralı müşteri güncellenirken veritabanı hatası oluştu.";
                    break;
            }
        }

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail
        };
        problem.Extensions["returnCode"] = sonuc.ReturnCode;

        var result = new ObjectResult(problem) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleIstek.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// PUT /api/customers/{customerNumber} istek gövdesi. Uzunluklar lgcmarea.cpy'deki alan uzunluklarıdır.
/// </summary>
public sealed class MusteriGuncelleIstek
{
    // Yalnızca cevapta olan alan: gönderilirse yok sayılır, istek reddedilmez (API tasarımı, bölüm 1).
    [JsonPropertyName("customerNumber")]
    public JsonElement? CustomerNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir.
    [Required]
    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("houseName")]
    public string? HouseName { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(4)]
    [JsonPropertyName("houseNumber")]
    public string? HouseNumber { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("phoneMobile")]
    public string? PhoneMobile { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(20)]
    [JsonPropertyName("phoneHome")]
    public string? PhoneHome { get; set; }

    [Required(AllowEmptyStrings = true)]
    [StringLength(100)]
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriCevap.cs
```csharp
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Başarılı güncellemenin cevabı (200 + müşteri).
/// </summary>
public sealed class MusteriCevap
{
    [JsonPropertyName("customerNumber")]
    public int CustomerNumber { get; init; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = "";

    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; init; }

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

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleSonuc.cs
```csharp
namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Eski sistemin dönüş kodları (CA-RETURN-CODE) — bu operasyonda oluşabilenler.
/// </summary>
public static class DonusKodlari
{
    public const string Basarili = "00";
    public const string KayitYok = "01";
    public const string VeritabaniHatasi = "90";
}

/// <summary>
/// Servis sonucu: dönüş kodu ve başarılıysa güncellenmiş müşteri.
/// </summary>
public sealed class MusteriGuncelleSonuc
{
    public required string ReturnCode { get; init; }

    public MusteriCevap? Musteri { get; init; }

    /// <summary>Kilitlenme ya da zaman aşımı (503, returnCode "90").</summary>
    public bool VeritabaniMesgul { get; init; }

    public static MusteriGuncelleSonuc Basarili(MusteriCevap musteri) =>
        new() { ReturnCode = DonusKodlari.Basarili, Musteri = musteri };

    public static MusteriGuncelleSonuc Hata(string returnCode) =>
        new() { ReturnCode = returnCode };

    public static MusteriGuncelleSonuc Mesgul() =>
        new() { ReturnCode = DonusKodlari.VeritabaniHatasi, VeritabaniMesgul = true };
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriVerisi.cs
```csharp
namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Veritabanına yazılacak müşteri alanları (LGUCDB01'deki UPDATE CUSTOMER SET listesi).
/// </summary>
public sealed record MusteriVerisi(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string EmailAddress);
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleService.cs
```csharp
using Npgsql;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// LGUCUS01 + LGUCDB01 iş mantığı. LGUCVS01'in (VSAM yeniden yazımı) karşılığı yoktur;
/// müşteri yoksa VSAM'daki LGV1 ABEND'i yerine 404/"01" döner (bilinçli sapma).
/// </summary>
public sealed class MusteriGuncelleService
{
    // PostgreSQL SQLSTATE: deadlock, serialization failure, lock not available, statement timeout/cancel.
    private static readonly HashSet<string> MesgulKodlari = ["40P01", "40001", "55P03", "57014"];

    private readonly MusteriGuncelleRepository _repository;
    private readonly ILogger<MusteriGuncelleService> _logger;

    public MusteriGuncelleService(MusteriGuncelleRepository repository, ILogger<MusteriGuncelleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MusteriGuncelleSonuc> GuncelleAsync(
        int customerNumber, MusteriGuncelleIstek istek, CancellationToken cancellationToken)
    {
        // Posta kodu büyük harfe çevrilerek kaydedilir (API tasarımı; eskide lgtestc1.cbl:188-189).
        var veri = new MusteriVerisi(
            FirstName: istek.FirstName ?? "",
            LastName: istek.LastName ?? "",
            DateOfBirth: istek.DateOfBirth!.Value,
            HouseName: istek.HouseName ?? "",
            HouseNumber: istek.HouseNumber ?? "",
            Postcode: (istek.Postcode ?? "").ToUpperInvariant(),
            PhoneMobile: istek.PhoneMobile ?? "",
            PhoneHome: istek.PhoneHome ?? "",
            EmailAddress: istek.Email ?? "");

        try
        {
            var kayit = await _repository.GuncelleAsync(customerNumber, veri, cancellationToken);

            // SQLCODE = 100 → '01' (lgucdb01: UPDATE-CUSTOMER-INFO)
            if (kayit is null)
            {
                return MusteriGuncelleSonuc.Hata(DonusKodlari.KayitYok);
            }

            return MusteriGuncelleSonuc.Basarili(kayit);
        }
        catch (NpgsqlException ex) when (MesgulMu(ex))
        {
            _logger.LogWarning(ex,
                "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST: veritabanı kilitlenmesi ya da zaman aşımı", customerNumber);
            return MusteriGuncelleSonuc.Mesgul();
        }
        catch (NpgsqlException ex)
        {
            // SQLCODE ≠ 0 ve ≠ 100 → '90' ve hata kuyruğuna yazım (WRITE-ERROR-MESSAGE → ILogger)
            _logger.LogError(ex,
                "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST: veritabanı hatası", customerNumber);
            return MusteriGuncelleSonuc.Hata(DonusKodlari.VeritabaniHatasi);
        }
    }

    private static bool MesgulMu(NpgsqlException ex)
    {
        if (ex is PostgresException pg && MesgulKodlari.Contains(pg.SqlState))
        {
            return true;
        }

        return ex.InnerException is TimeoutException;
    }
}
```

### dosya: src/GenApp.Llm/MusteriGuncelle/MusteriGuncelleRepository.cs
```csharp
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// LGUCDB01'deki UPDATE CUSTOMER'ın PostgreSQL karşılığı.
/// </summary>
public sealed class MusteriGuncelleRepository
{
    // Kolon adları şema belgesindeki snake_case kuralıyla Db2 adlarından türetilmiştir.
    private const string GuncelleSql = """
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

    public MusteriGuncelleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
    }

    /// <summary>
    /// Müşteriyi günceller; satır yoksa null döner (SQLCODE = 100 karşılığı).
    /// </summary>
    public async Task<MusteriCevap?> GuncelleAsync(
        int customerNumber, MusteriVerisi veri, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(GuncelleSql, connection);
        command.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = veri.FirstName });
        command.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = veri.LastName });
        command.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = veri.DateOfBirth });
        command.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = veri.HouseName });
        command.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = veri.HouseNumber });
        command.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = veri.Postcode });
        command.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = veri.PhoneMobile });
        command.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = veri.PhoneHome });
        command.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = veri.EmailAddress });
        command.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = customerNumber });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MusteriCevap
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
            Email = Metin(reader, 9)
        };
    }

    // Cevaplarda sondaki boşluklar kırpılır; boş metin null değil "" döner.
    private static string Metin(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).TrimEnd();
}
```