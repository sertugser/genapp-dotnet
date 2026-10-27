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
