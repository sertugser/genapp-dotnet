using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// LGUCDB01'deki UPDATE CUSTOMER'ın PostgreSQL karşılığı.
/// </summary>
public sealed class MusteriGuncelleRepository
{
    // Kolon adları şema belgesindeki snake_case kuralıyla Db2 adlarından türetilmiştir.
    private const string GuncelleSql = """
        UPDATE customer
           SET first_name    = @first_name,
               last_name     = @last_name,
               date_of_birth = @date_of_birth,
               house_name    = @house_name,
               house_number  = @house_number,
               postcode      = @postcode,
               phone_mobile  = @phone_mobile,
               phone_home    = @phone_home,
               email_address = @email_address
         WHERE customer_number = @customer_number
        RETURNING customer_number, first_name, last_name, date_of_birth, house_name,
                  house_number, postcode, phone_mobile, phone_home, email_address
        """;

    private readonly string _connectionString;

    public MusteriGuncelleRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("GenApp")
            ?? throw new InvalidOperationException("ConnectionStrings__GenApp yapılandırması bulunamadı.");
    }

    /// <summary>
    /// Müşteriyi günceller; satır yoksa null döner (SQLCODE = 100 karşılığı).
    /// </summary>
    public async Task<MusteriCevap?> GuncelleAsync(
        int customerNumber, MusteriVerisi veri, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(GuncelleSql, connection);
        command.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = veri.FirstName });
        command.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = veri.LastName });
        command.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = veri.DateOfBirth });
        command.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = veri.HouseName });
        command.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = veri.HouseNumber });
        command.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = veri.Postcode });
        command.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = veri.PhoneMobile });
        command.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = veri.PhoneHome });
        command.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = veri.EmailAddress });
        command.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = customerNumber });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MusteriCevap
        {
            CustomerNumber = reader.GetInt32(0),
            FirstName = Metin(reader, 1),
            LastName = Metin(reader, 2),
            DateOfBirth = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
            HouseName = Metin(reader, 4),
            HouseNumber = Metin(reader, 5),
            Postcode = Metin(reader, 6),
            PhoneMobile = Metin(reader, 7),
            PhoneHome = Metin(reader, 8),
            Email = Metin(reader, 9)
        };
    }

    // Cevaplarda sondaki boşluklar kırpılır; boş metin null değil "" döner.
    private static string Metin(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).TrimEnd();
}
