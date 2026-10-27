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
