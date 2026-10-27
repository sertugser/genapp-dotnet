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
