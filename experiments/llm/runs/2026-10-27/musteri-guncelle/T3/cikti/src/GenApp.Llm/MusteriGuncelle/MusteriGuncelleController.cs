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
