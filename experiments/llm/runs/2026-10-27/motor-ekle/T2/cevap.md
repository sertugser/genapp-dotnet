### dosya: src/GenApp.Llm/MotorEkle/MotorEkleController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Motor poliçesi ekleme (eski: LGAPOL01 → LGAPDB01 → LGAPVS01, istek kodu 01AMOT).
/// </summary>
[ApiController]
[Route("api/customers/{customerNumber:int}/motor-policies")]
public sealed class MotorEkleController : ControllerBase
{
    private readonly MotorEkleServisi _servis;

    public MotorEkleController(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _servis = new MotorEkleServisi(configuration, loggerFactory.CreateLogger<MotorEkleServisi>());
    }

    [HttpPost]
    [MotorEkleDogrulamaFiltresi]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MotorPoliceCevabi), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ekle(
        [FromRoute] int customerNumber,
        [FromBody] MotorEkleIstegi istek,
        CancellationToken cancellationToken)
    {
        var sonuc = await _servis.EkleAsync(customerNumber, istek, cancellationToken);

        if (sonuc.ReturnCode == DonusKodlari.Basarili && sonuc.Police is not null)
        {
            return Created(
                $"/api/customers/{customerNumber}/motor-policies/{sonuc.Police.PolicyNumber}",
                sonuc.Police);
        }

        if (sonuc.VeritabaniMesgul)
        {
            return Problem(StatusCodes.Status503ServiceUnavailable, "database-busy",
                "Veritabanı meşgul",
                "Veritabanı kilitlenmesi ya da zaman aşımı oluştu; istek daha sonra tekrar denenmelidir.",
                DonusKodlari.VeritabaniHatasi);
        }

        return sonuc.ReturnCode switch
        {
            DonusKodlari.MusteriBulunamadi => Problem(StatusCodes.Status404NotFound, "customer-not-found",
                "Müşteri bulunamadı",
                $"{customerNumber} numaralı müşteri bulunamadı.",
                DonusKodlari.MusteriBulunamadi),
            _ => Problem(StatusCodes.Status500InternalServerError, "database-error",
                "Veritabanı hatası",
                "Poliçe eklenirken veritabanı hatası oluştu.",
                DonusKodlari.VeritabaniHatasi),
        };
    }

    private ObjectResult Problem(int status, string type, string title, string detail, string returnCode)
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

### dosya: src/GenApp.Llm/MotorEkle/MotorEkleDogrulamaFiltresi.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Model doğrulaması başarısızsa API tasarımındaki biçimde (type: validation-error, errors listesi) 400 döner.
/// [ApiController]'ın otomatik 400 filtresinden (Order -2000) önce çalışır.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class MotorEkleDogrulamaFiltresiAttribute : Attribute, IActionFilter, IOrderedFilter
{
    public int Order => -3000;

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
            Detail = "İstekte eksik ya da hatalı alanlar var.",
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

### dosya: src/GenApp.Llm/MotorEkle/MotorEkleIstegi.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Motor poliçesi ekleme isteği. Uzunluk ve hane sınırları lgcmarea.cpy'deki alanlardır.
/// "Yalnızca cevapta" olan alanlar (policyNumber, customerNumber) ve lastChanged tanımlı değildir;
/// gönderilirse yok sayılır (LGAPDB01 LASTCHANGED için CURRENT TIMESTAMP kullanır).
/// </summary>
public sealed class MotorEkleIstegi
{
    // CA-ISSUE-DATE PIC X(10)
    [Required]
    public DateOnly? IssueDate { get; set; }

    // CA-EXPIRY-DATE PIC X(10)
    [Required]
    public DateOnly? ExpiryDate { get; set; }

    // CA-BROKERID PIC 9(10)
    [Required]
    [Range(0L, 9_999_999_999L)]
    public long? BrokerId { get; set; }

    // CA-BROKERSREF PIC X(10)
    [Required(AllowEmptyStrings = true)]
    [StringLength(10)]
    public string? BrokersReference { get; set; }

    // CA-PAYMENT PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Payment { get; set; }

    // CA-M-MAKE PIC X(15)
    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Make { get; set; }

    // CA-M-MODEL PIC X(15)
    [Required(AllowEmptyStrings = true)]
    [StringLength(15)]
    public string? Model { get; set; }

    // CA-M-VALUE PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Value { get; set; }

    // CA-M-REGNUMBER PIC X(7)
    [Required(AllowEmptyStrings = true)]
    [StringLength(7)]
    public string? RegistrationNumber { get; set; }

    // CA-M-COLOUR PIC X(8)
    [Required(AllowEmptyStrings = true)]
    [StringLength(8)]
    public string? Colour { get; set; }

    // CA-M-CC PIC 9(4)
    [Required]
    [Range(0, 9_999)]
    public int? Cc { get; set; }

    // CA-M-MANUFACTURED PIC X(10)
    [Required]
    public DateOnly? ManufactureDate { get; set; }

    // CA-M-PREMIUM PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Premium { get; set; }

    // CA-M-ACCIDENTS PIC 9(6)
    [Required]
    [Range(0, 999_999)]
    public int? Accidents { get; set; }
}
```

### dosya: src/GenApp.Llm/MotorEkle/MotorPoliceCevabi.cs
```csharp
namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Motor poliçesi cevabı (API tasarımı 4.2 ve 4.3).
/// </summary>
public sealed class MotorPoliceCevabi
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

### dosya: src/GenApp.Llm/MotorEkle/DonusKodlari.cs
```csharp
namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Bu operasyonda oluşabilen eski dönüş kodları (CA-RETURN-CODE).
/// </summary>
public static class DonusKodlari
{
    public const string Basarili = "00";
    public const string MusteriBulunamadi = "70";
    public const string VeritabaniHatasi = "90";
}

/// <summary>
/// Servis sonucu: dönüş kodu, başarılıysa poliçe; kilitlenme/zaman aşımı ayrıca işaretlenir (Karar 8).
/// </summary>
public sealed record MotorEkleSonucu(string ReturnCode, MotorPoliceCevabi? Police, bool VeritabaniMesgul = false);
```

### dosya: src/GenApp.Llm/MotorEkle/MotorEkleServisi.cs
```csharp
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MotorEkle;

/// <summary>
/// LGAPDB01'in 01AMOT yolunun karşılığı: POLICY ve MOTOR satırları tek transaction'da eklenir.
/// LGAPVS01'in VSAM yazımının karşılığı yoktur (VSAM yazımı Db2 yazımında zaten vardır).
/// </summary>
public sealed class MotorEkleServisi
{
    private const string LastChangedBicimi = "yyyy-MM-dd HH:mm:ss.ffffff";

    private readonly string _baglantiDizesi;
    private readonly ILogger _logger;

    public MotorEkleServisi(IConfiguration configuration, ILogger logger)
    {
        _baglantiDizesi = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
        _logger = logger;
    }

    public async Task<MotorEkleSonucu> EkleAsync(int musteriNo, MotorEkleIstegi istek, CancellationToken ct)
    {
        NpgsqlConnection? baglanti = null;
        NpgsqlTransaction? tx = null;
        try
        {
            baglanti = new NpgsqlConnection(_baglantiDizesi);
            await baglanti.OpenAsync(ct);
            tx = await baglanti.BeginTransactionAsync(ct);

            // INSERT-POLICY (lgapdb01.cbl): poliçe numarasını veritabanı verir, LASTCHANGED = CURRENT TIMESTAMP
            int policeNo;
            DateTime sonDegisiklik;
            try
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO policy
                        (customer_number, issue_date, expiry_date, policy_type, last_changed,
                         broker_id, brokers_reference, payment)
                    VALUES
                        (@customer_number, @issue_date, @expiry_date, 'M', LOCALTIMESTAMP,
                         @broker_id, @brokers_reference, @payment)
                    RETURNING policy_number, last_changed
                    """, baglanti, tx);
                cmd.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = musteriNo });
                cmd.Parameters.Add(new NpgsqlParameter("issue_date", NpgsqlDbType.Date) { Value = istek.IssueDate!.Value });
                cmd.Parameters.Add(new NpgsqlParameter("expiry_date", NpgsqlDbType.Date) { Value = istek.ExpiryDate!.Value });
                // broker_id integer kolonudur; 10 haneli değer integer'a sığmazsa veritabanı hatası (90) olur (MOVE CA-BROKERID TO DB2-BROKERID-INT)
                cmd.Parameters.Add(new NpgsqlParameter("broker_id", NpgsqlDbType.Bigint) { Value = istek.BrokerId!.Value });
                cmd.Parameters.Add(new NpgsqlParameter("brokers_reference", NpgsqlDbType.Varchar) { Value = istek.BrokersReference ?? "" });
                cmd.Parameters.Add(new NpgsqlParameter("payment", NpgsqlDbType.Integer) { Value = istek.Payment!.Value });

                await using var okuyucu = await cmd.ExecuteReaderAsync(ct);
                await okuyucu.ReadAsync(ct);
                policeNo = okuyucu.GetInt32(0);
                sonDegisiklik = okuyucu.GetDateTime(1);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                // SQLCODE -530 (yabancı anahtar ihlali: müşteri yok) → '70'
                HataYaz(musteriNo, null, "INSERT POLICY", ex);
                await GeriAlAsync(tx);
                return new MotorEkleSonucu(DonusKodlari.MusteriBulunamadi, null);
            }
            catch (Exception ex) when (VeritabaniHatasiMi(ex))
            {
                HataYaz(musteriNo, null, "INSERT POLICY", ex);
                await GeriAlAsync(tx);
                return new MotorEkleSonucu(DonusKodlari.VeritabaniHatasi, null, MesgulMu(ex));
            }

            // INSERT-MOTOR (lgapdb01.cbl): hata olursa ABEND LGSQ ile POLICY eklemesi de geri alınır → '90'
            try
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO motor
                        (policy_number, make, model, value, reg_number, colour, cc,
                         year_of_manufacture, premium, accidents)
                    VALUES
                        (@policy_number, @make, @model, @value, @reg_number, @colour, @cc,
                         @year_of_manufacture, @premium, @accidents)
                    """, baglanti, tx);
                cmd.Parameters.Add(new NpgsqlParameter("policy_number", NpgsqlDbType.Integer) { Value = policeNo });
                cmd.Parameters.Add(new NpgsqlParameter("make", NpgsqlDbType.Varchar) { Value = istek.Make ?? "" });
                cmd.Parameters.Add(new NpgsqlParameter("model", NpgsqlDbType.Varchar) { Value = istek.Model ?? "" });
                cmd.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Integer) { Value = istek.Value!.Value });
                cmd.Parameters.Add(new NpgsqlParameter("reg_number", NpgsqlDbType.Varchar) { Value = istek.RegistrationNumber ?? "" });
                cmd.Parameters.Add(new NpgsqlParameter("colour", NpgsqlDbType.Varchar) { Value = istek.Colour ?? "" });
                cmd.Parameters.Add(new NpgsqlParameter("cc", NpgsqlDbType.Smallint) { Value = (short)istek.Cc!.Value });
                cmd.Parameters.Add(new NpgsqlParameter("year_of_manufacture", NpgsqlDbType.Date) { Value = istek.ManufactureDate!.Value });
                cmd.Parameters.Add(new NpgsqlParameter("premium", NpgsqlDbType.Integer) { Value = istek.Premium!.Value });
                cmd.Parameters.Add(new NpgsqlParameter("accidents", NpgsqlDbType.Integer) { Value = istek.Accidents!.Value });
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception ex) when (VeritabaniHatasiMi(ex))
            {
                HataYaz(musteriNo, policeNo, "INSERT MOTOR", ex);
                await GeriAlAsync(tx);
                return new MotorEkleSonucu(DonusKodlari.VeritabaniHatasi, null, MesgulMu(ex));
            }

            try
            {
                await tx.CommitAsync(ct);
            }
            catch (Exception ex) when (VeritabaniHatasiMi(ex))
            {
                HataYaz(musteriNo, policeNo, "COMMIT", ex);
                return new MotorEkleSonucu(DonusKodlari.VeritabaniHatasi, null, MesgulMu(ex));
            }

            var police = new MotorPoliceCevabi
            {
                PolicyNumber = policeNo,
                CustomerNumber = musteriNo,
                IssueDate = istek.IssueDate!.Value,
                ExpiryDate = istek.ExpiryDate!.Value,
                LastChanged = sonDegisiklik.ToString(LastChangedBicimi, CultureInfo.InvariantCulture),
                BrokerId = istek.BrokerId!.Value,
                BrokersReference = (istek.BrokersReference ?? "").TrimEnd(),
                Payment = istek.Payment!.Value,
                Make = (istek.Make ?? "").TrimEnd(),
                Model = (istek.Model ?? "").TrimEnd(),
                Value = istek.Value!.Value,
                RegistrationNumber = (istek.RegistrationNumber ?? "").TrimEnd(),
                Colour = (istek.Colour ?? "").TrimEnd(),
                Cc = istek.Cc!.Value,
                ManufactureDate = istek.ManufactureDate!.Value,
                Premium = istek.Premium!.Value,
                Accidents = istek.Accidents!.Value,
            };
            return new MotorEkleSonucu(DonusKodlari.Basarili, police);
        }
        catch (Exception ex) when (VeritabaniHatasiMi(ex))
        {
            // Bağlantı açılamaması ya da transaction başlatılamaması: genel veritabanı hatası '90'
            HataYaz(musteriNo, null, "CONNECT", ex);
            return new MotorEkleSonucu(DonusKodlari.VeritabaniHatasi, null, MesgulMu(ex));
        }
        finally
        {
            if (tx is not null)
            {
                await tx.DisposeAsync();
            }
            if (baglanti is not null)
            {
                await baglanti.DisposeAsync();
            }
        }
    }

    private static bool VeritabaniHatasiMi(Exception ex) =>
        ex is NpgsqlException or TimeoutException or InvalidCastException or OverflowException;

    private static bool MesgulMu(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is PostgresErrorCodes.DeadlockDetected
                or PostgresErrorCodes.LockNotAvailable
                or PostgresErrorCodes.QueryCanceled;
        }
        return ex is TimeoutException || ex.InnerException is TimeoutException;
    }

    private static async Task GeriAlAsync(NpgsqlTransaction tx)
    {
        try
        {
            await tx.RollbackAsync();
        }
        catch (Exception)
        {
            // Geri alma başarısız olursa bağlantı kapanınca PostgreSQL transaction'ı zaten geri alır
        }
    }

    // WRITE-ERROR-MESSAGE (LGSTSQ / GENAERRS kuyruğu) karşılığı
    private void HataYaz(int musteriNo, int? policeNo, string sqlIstegi, Exception ex)
    {
        var sqlState = (ex as PostgresException)?.SqlState ?? "-";
        _logger.LogError(ex,
            "LGAPDB01 CNUM={CustomerNumber} PNUM={PolicyNumber} {SqlRequest} SQLSTATE={SqlState}",
            musteriNo, policeNo?.ToString(CultureInfo.InvariantCulture) ?? "", sqlIstegi, sqlState);
    }
}
```