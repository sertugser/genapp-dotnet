### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleController.cs
```csharp
using System.Data.Common;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GenApp.Llm.MusteriEkle;

[Route("api/customers")]
[Produces("application/json")]
public sealed class MusteriEkleController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MusteriEkleController> _logger;

    public MusteriEkleController(IConfiguration configuration, ILogger<MusteriEkleController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // LGACUS01 -> LGACDB01 -> (LGACVS01: yeni sistemde karşılığı yok) -> LGACDB02
    [HttpPost]
    public async Task<IActionResult> MusteriEkle([FromBody] MusteriEkleIstek? istek, CancellationToken cancellationToken)
    {
        if (istek is null)
        {
            ModelState.AddModelError("$", "İstek gövdesi boş olamaz.");
        }

        DateOnly dogumTarihi = default;
        if (istek is not null)
        {
            // Bilinçli sapma: doğum tarihi boş ya da geçersizse veritabanına gitmeden 400 döner.
            if (string.IsNullOrWhiteSpace(istek.DateOfBirth))
            {
                ModelState.AddModelError(nameof(MusteriEkleIstek.DateOfBirth), "Doğum tarihi zorunludur.");
            }
            else if (!DateOnly.TryParseExact(istek.DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                         DateTimeStyles.None, out dogumTarihi))
            {
                ModelState.AddModelError(nameof(MusteriEkleIstek.DateOfBirth),
                    "Doğum tarihi yyyy-MM-dd biçiminde geçerli bir tarih olmalıdır.");
            }
        }

        if (!ModelState.IsValid || istek is null)
        {
            return DogrulamaHatasi(ModelState);
        }

        var yeniMusteri = new YeniMusteri(
            FirstName: istek.FirstName ?? string.Empty,
            LastName: istek.LastName ?? string.Empty,
            DateOfBirth: dogumTarihi,
            HouseName: istek.HouseName ?? string.Empty,
            HouseNumber: istek.HouseNumber ?? string.Empty,
            // Posta kodu büyük harfe çevrilerek kaydedilir (API tasarımı, eski sistemde lgtestc1.cbl:126-127).
            Postcode: (istek.Postcode ?? string.Empty).ToUpperInvariant(),
            PhoneMobile: istek.PhoneMobile ?? string.Empty,
            PhoneHome: istek.PhoneHome ?? string.Empty,
            Email: istek.Email ?? string.Empty);

        var connectionString = _configuration.GetConnectionString("GenApp");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("ConnectionStrings:GenApp yapılandırması bulunamadı.");
            return Hata(500, "database-error", "Veritabanı hatası", "Veritabanı bağlantısı yapılandırılmamış.", "90");
        }

        var depo = new MusteriEkleDeposu(connectionString, _logger);

        try
        {
            var musteri = await depo.EkleAsync(yeniMusteri, cancellationToken);
            var cevap = MusteriCevap.Olustur(musteri);
            return Created($"/api/customers/{cevap.CustomerNumber}", cevap);
        }
        catch (Exception ex) when (MusteriEkleDeposu.MesgulMu(ex))
        {
            // Bilinçli sapma (Karar 8): kilitlenme ve zaman aşımı 503 + "90".
            _logger.LogError(ex, "LGACDB01 INSERT CUSTOMER: veritabanı meşgul.");
            return Hata(503, "database-busy", "Veritabanı meşgul",
                "Veritabanı şu anda meşgul; işlem tamamlanamadı.", "90");
        }
        catch (Exception ex) when (ex is DbException or TimeoutException or InvalidOperationException)
        {
            // lgacdb01.cbl: INSERT CUSTOMER SQLCODE NOT EQUAL 0 -> '90'
            _logger.LogError(ex, "LGACDB01 INSERT CUSTOMER: veritabanı hatası.");
            return Hata(500, "database-error", "Veritabanı hatası",
                "Müşteri eklenirken veritabanı hatası oluştu.", "90");
        }
    }

    private static ObjectResult DogrulamaHatasi(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Type = "validation-error",
            Title = "İstek doğrulanamadı",
            Status = 400,
            Detail = "İstekte eksik ya da hatalı alanlar var."
        };
        var sonuc = new ObjectResult(problem) { StatusCode = 400 };
        sonuc.ContentTypes.Add("application/problem+json");
        return sonuc;
    }

    private static ObjectResult Hata(int status, string type, string title, string detail, string returnCode)
    {
        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = detail
        };
        problem.Extensions["returnCode"] = returnCode;
        var sonuc = new ObjectResult(problem) { StatusCode = status };
        sonuc.ContentTypes.Add("application/problem+json");
        return sonuc;
    }
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleIstek.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace GenApp.Llm.MusteriEkle;

// customerNumber istekte yoktur; gönderilirse JSON çözümlemesinde yok sayılır (lgacdb01.cbl:285).
public sealed class MusteriEkleIstek
{
    [StringLength(10)] // CA-FIRST-NAME PIC X(10)
    public string? FirstName { get; set; }

    [StringLength(20)] // CA-LAST-NAME PIC X(20)
    public string? LastName { get; set; }

    // CA-DOB PIC X(10); biçim denetimi controller'da yapılır (yyyy-MM-dd).
    public string? DateOfBirth { get; set; }

    [StringLength(20)] // CA-HOUSE-NAME PIC X(20)
    public string? HouseName { get; set; }

    [StringLength(4)] // CA-HOUSE-NUM PIC X(4)
    public string? HouseNumber { get; set; }

    [StringLength(8)] // CA-POSTCODE PIC X(8)
    public string? Postcode { get; set; }

    [StringLength(20)] // CA-PHONE-MOBILE PIC X(20)
    public string? PhoneMobile { get; set; }

    [StringLength(20)] // CA-PHONE-HOME PIC X(20)
    public string? PhoneHome { get; set; }

    [StringLength(100)] // CA-EMAIL-ADDRESS PIC X(100)
    public string? Email { get; set; }
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriCevap.cs
```csharp
namespace GenApp.Llm.MusteriEkle;

public sealed record MusteriCevap(
    int CustomerNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string Email)
{
    // Cevaptaki metinlerin sonundaki boşluklar kırpılır; boş metin "" döner, null dönmez.
    public static MusteriCevap Olustur(KayitliMusteri m) => new(
        m.CustomerNumber,
        Kirp(m.FirstName),
        Kirp(m.LastName),
        m.DateOfBirth,
        Kirp(m.HouseName),
        Kirp(m.HouseNumber),
        Kirp(m.Postcode),
        Kirp(m.PhoneMobile),
        Kirp(m.PhoneHome),
        Kirp(m.Email));

    private static string Kirp(string? deger) => (deger ?? string.Empty).TrimEnd();
}
```

### dosya: src/GenApp.Api/Llm/MusteriEkle/MusteriEkleDeposu.cs
```csharp
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriEkle;

public sealed record YeniMusteri(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string Email);

public sealed record KayitliMusteri(
    int CustomerNumber,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? HouseName,
    string? HouseNumber,
    string? Postcode,
    string? PhoneMobile,
    string? PhoneHome,
    string? Email);

// LGACDB01 + LGACDB02 karşılığı; tek PostgreSQL transaction'ı (ADR 0003 Karar 2).
public sealed class MusteriEkleDeposu
{
    // lgacdb01.cbl: varsayılan parola, sayaç ve durum (D2-CUSTSECR-PASS/COUNT/STATE)
    private const string VarsayilanParola = "5732fec825535eeafb8fac50fee3a8aa";
    private const int VarsayilanParolaDegisimSayisi = 0;
    private const string VarsayilanDurum = "N";

    private readonly string _connectionString;
    private readonly ILogger _logger;

    public MusteriEkleDeposu(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<KayitliMusteri> EkleAsync(YeniMusteri m, CancellationToken ct)
    {
        await using var baglanti = new NpgsqlConnection(_connectionString);
        await baglanti.OpenAsync(ct);
        await using var tx = await baglanti.BeginTransactionAsync(ct);

        // Müşteri numarası PostgreSQL identity ile üretilir (GENACUSTNUM / IDENTITY_VAL_LOCAL karşılığı).
        KayitliMusteri kayit;
        await using (var komut = new NpgsqlCommand(
            """
            INSERT INTO customer
                (first_name, last_name, date_of_birth, house_name, house_number,
                 postcode, phone_mobile, phone_home, email_address)
            VALUES
                (@first_name, @last_name, @date_of_birth, @house_name, @house_number,
                 @postcode, @phone_mobile, @phone_home, @email_address)
            RETURNING customer_number, first_name, last_name, date_of_birth, house_name,
                      house_number, postcode, phone_mobile, phone_home, email_address
            """, baglanti, tx))
        {
            komut.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = m.FirstName });
            komut.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = m.LastName });
            komut.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = m.DateOfBirth });
            komut.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = m.HouseName });
            komut.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = m.HouseNumber });
            komut.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = m.Postcode });
            komut.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = m.PhoneMobile });
            komut.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = m.PhoneHome });
            komut.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = m.Email });

            await using var okuyucu = await komut.ExecuteReaderAsync(ct);
            if (!await okuyucu.ReadAsync(ct))
            {
                throw new InvalidOperationException("INSERT CUSTOMER satır döndürmedi.");
            }

            kayit = new KayitliMusteri(
                okuyucu.GetInt32(0),
                okuyucu.IsDBNull(1) ? null : okuyucu.GetString(1),
                okuyucu.IsDBNull(2) ? null : okuyucu.GetString(2),
                okuyucu.IsDBNull(3) ? null : okuyucu.GetFieldValue<DateOnly>(3),
                okuyucu.IsDBNull(4) ? null : okuyucu.GetString(4),
                okuyucu.IsDBNull(5) ? null : okuyucu.GetString(5),
                okuyucu.IsDBNull(6) ? null : okuyucu.GetString(6),
                okuyucu.IsDBNull(7) ? null : okuyucu.GetString(7),
                okuyucu.IsDBNull(8) ? null : okuyucu.GetString(8),
                okuyucu.IsDBNull(9) ? null : okuyucu.GetString(9));
        }

        // LGACDB02: hata D2-RETURN-CODE'a '98' yazar ama LGACDB01 bunu CA-RETURN-CODE'a taşımaz; müşteri kalır ve çağırana '00' döner.
        await tx.SaveAsync("lgacdb02", ct);
        try
        {
            await using var komut = new NpgsqlCommand(
                """
                INSERT INTO customer_secure (customer_number, customer_pass, state_indicator, pass_changes)
                VALUES (@customer_number, @customer_pass, @state_indicator, @pass_changes)
                """, baglanti, tx);
            komut.Parameters.AddWithValue("customer_number", kayit.CustomerNumber);
            komut.Parameters.AddWithValue("customer_pass", VarsayilanParola);
            komut.Parameters.AddWithValue("state_indicator", VarsayilanDurum);
            komut.Parameters.AddWithValue("pass_changes", VarsayilanParolaDegisimSayisi);
            await komut.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException ex)
        {
            await tx.RollbackAsync("lgacdb02", ct);
            _logger.LogError(ex,
                "LGACDB02 INSERT SECURITY başarısız (CNUM={CustomerNumber}, SQLSTATE={SqlState}); eski sistemdeki gibi yok sayıldı.",
                kayit.CustomerNumber, ex.SqlState);
        }

        await tx.CommitAsync(ct);
        return kayit;
    }

    public static bool MesgulMu(Exception ex)
    {
        if (ex is TimeoutException || ex.InnerException is TimeoutException)
        {
            return true;
        }

        if (ex is PostgresException pg)
        {
            // 40P01 deadlock, 55P03 kilit alınamadı, 57014 zaman aşımı (statement_timeout), 40001 serileştirme hatası
            return pg.SqlState is "40P01" or "55P03" or "57014" or "40001";
        }

        return false;
    }
}
```