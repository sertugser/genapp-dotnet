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
