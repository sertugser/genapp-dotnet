### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleController.cs
```csharp
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Llm.MusteriEkle;

[ApiController]
[Route("api/customers")]
[DogrulamaHatasiFiltresi]
public sealed class MusteriEkleController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public MusteriEkleController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MusteriCevabi), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> MusteriEkle([FromBody] MusteriEkleIstegi istek)
    {
        var connectionString = _configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");

        var servis = new MusteriEkleServisi(connectionString, _loggerFactory.CreateLogger<MusteriEkleServisi>());
        var sonuc = await servis.EkleAsync(istek);

        if (sonuc.ReturnCode == DonusKodlari.Basarili && sonuc.Musteri is not null)
        {
            return Created($"/api/customers/{sonuc.Musteri.CustomerNumber}", sonuc.Musteri);
        }

        if (sonuc.VeritabaniMesgul)
        {
            return Problem503();
        }

        return Problem500();
    }

    private ObjectResult Problem500()
    {
        var problem = new ProblemDetails
        {
            Type = "database-error",
            Title = "Veritabanı hatası",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Müşteri kaydedilirken veritabanı hatası oluştu."
        };
        problem.Extensions["returnCode"] = DonusKodlari.VeritabaniHatasi;
        return ProblemSonucu(problem);
    }

    private ObjectResult Problem503()
    {
        var problem = new ProblemDetails
        {
            Type = "database-busy",
            Title = "Veritabanı meşgul",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = "Veritabanı kilitlenmesi ya da zaman aşımı nedeniyle müşteri kaydedilemedi."
        };
        problem.Extensions["returnCode"] = DonusKodlari.VeritabaniHatasi;
        return ProblemSonucu(problem);
    }

    private static ObjectResult ProblemSonucu(ProblemDetails problem)
    {
        var result = new ObjectResult(problem) { StatusCode = problem.Status };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleIstegi.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriEkle;

// customerNumber bilinçli olarak yoktur: gönderilirse yok sayılır (lgacdb01.cbl:285, yeni numara üretilir).
public sealed class MusteriEkleIstegi
{
    [JsonPropertyName("firstName")]
    [MaxLength(10)]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    [MaxLength(20)]
    public string? LastName { get; set; }

    // Bilinçli sapma: boş ya da geçersiz doğum tarihi veritabanına ulaşmadan 400 ile reddedilir.
    [JsonPropertyName("dateOfBirth")]
    [Required]
    public DateOnly? DateOfBirth { get; set; }

    [JsonPropertyName("houseName")]
    [MaxLength(20)]
    public string? HouseName { get; set; }

    [JsonPropertyName("houseNumber")]
    [MaxLength(4)]
    public string? HouseNumber { get; set; }

    [JsonPropertyName("postcode")]
    [MaxLength(8)]
    public string? Postcode { get; set; }

    [JsonPropertyName("phoneMobile")]
    [MaxLength(20)]
    public string? PhoneMobile { get; set; }

    [JsonPropertyName("phoneHome")]
    [MaxLength(20)]
    public string? PhoneHome { get; set; }

    [JsonPropertyName("email")]
    [MaxLength(100)]
    public string? Email { get; set; }
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriCevabi.cs
```csharp
using System.Text.Json.Serialization;

namespace GenApp.Llm.MusteriEkle;

public sealed class MusteriCevabi
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

### dosya: src/GenApp.Api/Llm/MusteriEkle/DonusKodlari.cs
```csharp
namespace GenApp.Llm.MusteriEkle;

public static class DonusKodlari
{
    public const string Basarili = "00";
    public const string VeritabaniHatasi = "90";
}

public sealed record MusteriEkleSonucu(string ReturnCode, MusteriCevabi? Musteri, bool VeritabaniMesgul)
{
    public static MusteriEkleSonucu Basarili(MusteriCevabi musteri) =>
        new(DonusKodlari.Basarili, musteri, false);

    public static MusteriEkleSonucu Hata(bool mesgul) =>
        new(DonusKodlari.VeritabaniHatasi, null, mesgul);
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleServisi.cs
```csharp
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriEkle;

public sealed class MusteriEkleServisi
{
    // lgacdb01.cbl: LGACDB02'ye gönderilen sabit değerler ('5732fec8...', '0000', 'N')
    private const string VarsayilanSifre = "5732fec825535eeafb8fac50fee3a8aa";
    private const string VarsayilanDurum = "N";
    private const int VarsayilanSifreDegisimSayisi = 0;

    private readonly string _connectionString;
    private readonly ILogger<MusteriEkleServisi> _logger;

    public MusteriEkleServisi(string connectionString, ILogger<MusteriEkleServisi> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<MusteriEkleSonucu> EkleAsync(MusteriEkleIstegi istek)
    {
        // Db2 CHAR alanları sondaki boşlukları ayırt etmez; değerler sondaki boşluklar kırpılarak saklanır.
        var firstName = Metin(istek.FirstName);
        var lastName = Metin(istek.LastName);
        var dateOfBirth = istek.DateOfBirth!.Value;
        var houseName = Metin(istek.HouseName);
        var houseNumber = Metin(istek.HouseNumber);
        // API tasarımı: posta kodu büyük harfe çevrilerek kaydedilir (lgtestc1.cbl:126-127).
        var postcode = Metin(istek.Postcode).ToUpperInvariant();
        var phoneMobile = Metin(istek.PhoneMobile);
        var phoneHome = Metin(istek.PhoneHome);
        var email = Metin(istek.Email);

        NpgsqlConnection? connection = null;
        NpgsqlTransaction? transaction = null;
        try
        {
            connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            transaction = await connection.BeginTransactionAsync();

            // INSERT-CUSTOMER (lgacdb01.cbl): named counter yerine PostgreSQL identity numarayı üretir (ADR 0003 Karar 4).
            int customerNumber;
            await using (var cmd = new NpgsqlCommand(
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
                cmd.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = firstName });
                cmd.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = lastName });
                cmd.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = dateOfBirth });
                cmd.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = houseName });
                cmd.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = houseNumber });
                cmd.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = postcode });
                cmd.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = phoneMobile });
                cmd.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = phoneHome });
                cmd.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = email });

                var sonuc = await cmd.ExecuteScalarAsync();
                customerNumber = Convert.ToInt32(sonuc);
            }

            // LGACVS01 (VSAM yazımı) yeni sistemde yoktur; customer satırı onun karşılığıdır (ADR 0003 Karar 1).

            await GuvenlikKaydiEkleAsync(connection, transaction, customerNumber);

            await transaction.CommitAsync();

            return MusteriEkleSonucu.Basarili(new MusteriCevabi
            {
                CustomerNumber = customerNumber,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                HouseName = houseName,
                HouseNumber = houseNumber,
                Postcode = postcode,
                PhoneMobile = phoneMobile,
                PhoneHome = phoneHome,
                Email = email
            });
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            // lgacdb01.cbl INSERT-CUSTOMER: SQLCODE <> 0 ise '90' (kilitlenme/zaman aşımı: 503, Karar 8).
            _logger.LogError(ex, "LGACDB01 INSERT CUSTOMER başarısız. returnCode=90");
            if (transaction is not null)
            {
                try { await transaction.RollbackAsync(); }
                catch (Exception rbEx) { _logger.LogWarning(rbEx, "Geri alma başarısız."); }
            }
            return MusteriEkleSonucu.Hata(MesgulMu(ex));
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
            if (connection is not null) await connection.DisposeAsync();
        }
    }

    private async Task GuvenlikKaydiEkleAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int customerNumber)
    {
        const string savepoint = "customer_secure_ekle";
        await transaction.SaveAsync(savepoint);
        try
        {
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO customer_secure
                    (customer_number, customer_pass, state_indicator, pass_changes)
                VALUES
                    (@customer_number, @customer_pass, @state_indicator, @pass_changes)
                """, connection, transaction);
            cmd.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = customerNumber });
            cmd.Parameters.Add(new NpgsqlParameter("customer_pass", NpgsqlDbType.Varchar) { Value = VarsayilanSifre });
            cmd.Parameters.Add(new NpgsqlParameter("state_indicator", NpgsqlDbType.Varchar) { Value = VarsayilanDurum });
            cmd.Parameters.Add(new NpgsqlParameter("pass_changes", NpgsqlDbType.Integer) { Value = VarsayilanSifreDegisimSayisi });
            await cmd.ExecuteNonQueryAsync();
            await transaction.ReleaseAsync(savepoint);
        }
        catch (NpgsqlException ex)
        {
            // lgacdb02.cbl: hata '98' yalnızca D2-RETURN-CODE'a yazılır, CA-RETURN-CODE'a geçmez; müşteri kaydı kalır ve çağırana '00' döner.
            _logger.LogError(ex, "LGACDB02 INSERT SECURITY başarısız. CNUM={CustomerNumber} D2-RETURN-CODE=98", customerNumber);
            await transaction.RollbackAsync(savepoint);
        }
    }

    private static string Metin(string? deger) => (deger ?? "").TrimEnd(' ');

    private static bool MesgulMu(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is TimeoutException) return true;
            if (e is PostgresException pg &&
                pg.SqlState is PostgresErrorCodes.DeadlockDetected
                    or PostgresErrorCodes.LockNotAvailable
                    or PostgresErrorCodes.QueryCanceled
                    or PostgresErrorCodes.SerializationFailure)
            {
                return true;
            }
        }
        return false;
    }
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/DogrulamaHatasiFiltresiAttribute.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GenApp.Llm.MusteriEkle;

// API tasarımı 3 ('98' notu): hatalı alanlı istekler 400, type "validation-error" ve "errors" listesiyle döner.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DogrulamaHatasiFiltresiAttribute : Attribute, IActionFilter, IOrderedFilter
{
    // [ApiController]'ın ModelStateInvalidFilter'ından (-2000) önce çalışır.
    public int Order => int.MinValue;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

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

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
```