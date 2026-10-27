using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

public sealed record CustomerRecord(
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

public interface ICustomerInquiryRepository
{
    /// <summary>Müşteriyi okur; yoksa null döner.</summary>
    Task<CustomerRecord?> FindAsync(int customerNumber, CancellationToken cancellationToken);
}

/// <summary>LGICDB01'deki SELECT ... FROM CUSTOMER WHERE CUSTOMERNUMBER = ? karşılığı.</summary>
public sealed class NpgsqlCustomerInquiryRepository : ICustomerInquiryRepository
{
    private const string Sql = """
        SELECT customer_number,
               first_name,
               last_name,
               date_of_birth,
               house_name,
               house_number,
               postcode,
               phone_mobile,
               phone_home,
               email_address
          FROM customer
         WHERE customer_number = @customer_number
        """;

    private readonly string _connectionString;

    public NpgsqlCustomerInquiryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CustomerRecord?> FindAsync(int customerNumber, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("customer_number", customerNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CustomerRecord(
            reader.GetInt32(0),
            GetString(reader, 1),
            GetString(reader, 2),
            reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
            GetString(reader, 4),
            GetString(reader, 5),
            GetString(reader, 6),
            GetString(reader, 7),
            GetString(reader, 8),
            GetString(reader, 9));
    }

    private static string? GetString(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
