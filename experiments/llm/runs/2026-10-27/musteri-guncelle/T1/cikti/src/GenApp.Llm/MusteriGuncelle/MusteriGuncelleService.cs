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
