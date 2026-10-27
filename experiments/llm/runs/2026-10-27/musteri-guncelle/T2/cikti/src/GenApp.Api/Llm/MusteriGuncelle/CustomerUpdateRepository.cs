using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// customer tablosunu günceller (lgucdb01.cbl UPDATE CUSTOMER ... WHERE CUSTOMERNUMBER = ...).
/// </summary>
public sealed class CustomerUpdateRepository
{
    private const string UpdateSql = """
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

    public CustomerUpdateRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Güncellenen kaydı döner; kayıt yoksa null.</summary>
    public async Task<CustomerResponse?> UpdateAsync(int customerNumber, CustomerUpdateData data, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        CustomerResponse? result = null;

        await using (var command = new NpgsqlCommand(UpdateSql, connection, transaction))
        {
            command.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = customerNumber });
            command.Parameters.Add(new NpgsqlParameter("first_name", NpgsqlDbType.Varchar) { Value = data.FirstName });
            command.Parameters.Add(new NpgsqlParameter("last_name", NpgsqlDbType.Varchar) { Value = data.LastName });
            command.Parameters.Add(new NpgsqlParameter("date_of_birth", NpgsqlDbType.Date) { Value = data.DateOfBirth });
            command.Parameters.Add(new NpgsqlParameter("house_name", NpgsqlDbType.Varchar) { Value = data.HouseName });
            command.Parameters.Add(new NpgsqlParameter("house_number", NpgsqlDbType.Varchar) { Value = data.HouseNumber });
            command.Parameters.Add(new NpgsqlParameter("postcode", NpgsqlDbType.Varchar) { Value = data.Postcode });
            command.Parameters.Add(new NpgsqlParameter("phone_mobile", NpgsqlDbType.Varchar) { Value = data.PhoneMobile });
            command.Parameters.Add(new NpgsqlParameter("phone_home", NpgsqlDbType.Varchar) { Value = data.PhoneHome });
            command.Parameters.Add(new NpgsqlParameter("email_address", NpgsqlDbType.Varchar) { Value = data.Email });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                result = new CustomerResponse
                {
                    CustomerNumber = reader.GetInt32(0),
                    FirstName = Text(reader, 1),
                    LastName = Text(reader, 2),
                    DateOfBirth = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
                    HouseName = Text(reader, 4),
                    HouseNumber = Text(reader, 5),
                    Postcode = Text(reader, 6),
                    PhoneMobile = Text(reader, 7),
                    PhoneHome = Text(reader, 8),
                    Email = Text(reader, 9)
                };
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static string Text(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal).TrimEnd();
}
