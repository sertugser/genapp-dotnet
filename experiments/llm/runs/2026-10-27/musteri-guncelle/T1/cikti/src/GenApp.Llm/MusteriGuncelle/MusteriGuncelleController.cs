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
