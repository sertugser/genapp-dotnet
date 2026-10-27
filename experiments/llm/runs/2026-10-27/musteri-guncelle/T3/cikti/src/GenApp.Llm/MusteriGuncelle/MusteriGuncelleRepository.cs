using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriGuncelle;

public sealed class MusteriGuncelleRepository
{
    private readonly string _connectionString;

    public MusteriGuncelleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public sealed record MusteriAlanlari(
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string HouseName,
        string HouseNumber,
        string Postcode,
        string PhoneMobile,
        string PhoneHome,
        string Email);

    // lgucdb01.cbl UPDATE-CUSTOMER-INFO karşılığı; tek SQL cümlesi olduğu için ayrı transaction açılmaz.
    public async Task<MusteriGuncelleResponse?> GuncelleAsync(
        int customerNumber, MusteriAlanlari m, CancellationToken cancellationToken)
    {
        const string sql = """
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

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = m.FirstName });
        command.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = m.LastName });
        command.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = m.DateOfBirth });
        command.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = m.HouseName });
        command.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = m.HouseNumber });
        command.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = m.Postcode });
        command.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = m.PhoneMobile });
        command.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = m.PhoneHome });
        command.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = m.Email });
        command.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = customerNumber });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MusteriGuncelleResponse
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
            Email = Metin(reader, 9),
        };
    }

    // Cevaptaki metinlerin sonundaki boşluklar kırpılır; null metin "" olarak döner (API tasarımı 1).
    private static string Metin(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).TrimEnd();
}
