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
