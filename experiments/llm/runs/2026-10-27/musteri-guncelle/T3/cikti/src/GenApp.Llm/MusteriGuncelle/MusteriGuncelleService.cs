using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriGuncelle;

public sealed class MusteriGuncelleService
{
    private readonly MusteriGuncelleRepository _repository;
    private readonly ILogger _logger;

    public MusteriGuncelleService(MusteriGuncelleRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MusteriGuncelleResult> GuncelleAsync(
        int customerNumber, MusteriGuncelleRequest request, CancellationToken cancellationToken)
    {
        var girdi = new MusteriGuncelleRepository.MusteriAlanlari(
            FirstName: request.FirstName ?? "",
            LastName: request.LastName ?? "",
            DateOfBirth: request.DateOfBirth!.Value,
            HouseName: request.HouseName ?? "",
            HouseNumber: request.HouseNumber ?? "",
            // Posta kodu büyük harfe çevrilerek kaydedilir (eskide lgtestc1.cbl:188-189, yenide API).
            Postcode: (request.Postcode ?? "").ToUpperInvariant(),
            PhoneMobile: request.PhoneMobile ?? "",
            PhoneHome: request.PhoneHome ?? "",
            Email: request.Email ?? "");

        try
        {
            var musteri = await _repository.GuncelleAsync(customerNumber, girdi, cancellationToken);

            // lgucdb01.cbl: SQLCODE = 100 → '01'. VSAM adımı (LGUCVS01) yeni sistemde yok; LGV1 ABEND yerine 404 döner.
            if (musteri is null)
            {
                return new MusteriGuncelleResult(MusteriGuncelleReturnCodes.KayitYok);
            }

            return new MusteriGuncelleResult(MusteriGuncelleReturnCodes.Basarili, musteri);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException && !cancellationToken.IsCancellationRequested)
        {
            var mesgul = VeritabaniMesgulMu(ex);
            // WRITE-ERROR-MESSAGE (LGSTSQ) karşılığı: hata loglanır.
            _logger.LogError(ex, "LGUCDB01 UPDATE CUST başarısız. CNUM={CustomerNumber} SQLSTATE={SqlState}",
                customerNumber, (ex as PostgresException)?.SqlState);
            return new MusteriGuncelleResult(MusteriGuncelleReturnCodes.VeritabaniHatasi, VeritabaniMesgul: mesgul);
        }
    }

    private static bool VeritabaniMesgulMu(Exception ex)
    {
        if (ex is TimeoutException || ex.InnerException is TimeoutException)
        {
            return true;
        }

        if (ex is PostgresException pg)
        {
            return pg.SqlState is
                PostgresErrorCodes.DeadlockDetected or
                PostgresErrorCodes.LockNotAvailable or
                PostgresErrorCodes.QueryCanceled;
        }

        return false;
    }
}
