using Npgsql;

namespace GenApp.Llm.MotorSorgula;

public sealed class MotorSorgulaRepository
{
    private readonly string _connectionString;

    public MotorSorgulaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // lgipdb01.cbl GET-MOTOR-DB2-INFO: POLICY ve MOTOR birleşimi, müşteri + poliçe numarasıyla tek satır
    private const string Sql = """
        SELECT p.policy_type,
               p.issue_date,
               p.expiry_date,
               p.last_changed,
               p.broker_id,
               p.brokers_reference,
               p.payment,
               m.make,
               m.model,
               m.value,
               m.reg_number,
               m.colour,
               m.cc,
               m.year_of_manufacture,
               m.premium,
               m.accidents
        FROM policy p
        JOIN motor m ON p.policy_number = m.policy_number
        WHERE p.customer_number = @customerNumber
          AND p.policy_number = @policyNumber
        """;

    public async Task<MotorPolicyRow?> FindAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("customerNumber", customerNumber);
        command.Parameters.AddWithValue("policyNumber", policyNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null; // SQLCODE = 100
        }

        return new MotorPolicyRow
        {
            PolicyType = GetString(reader, 0),
            IssueDate = GetDate(reader, 1),
            ExpiryDate = GetDate(reader, 2),
            LastChanged = reader.GetDateTime(3),
            BrokerId = GetInt(reader, 4),
            BrokersReference = GetString(reader, 5),
            Payment = GetInt(reader, 6),
            Make = GetString(reader, 7),
            Model = GetString(reader, 8),
            Value = GetInt(reader, 9),
            RegNumber = GetString(reader, 10),
            Colour = GetString(reader, 11),
            Cc = GetInt(reader, 12),
            YearOfManufacture = GetDate(reader, 13),
            Premium = GetInt(reader, 14),
            Accidents = GetInt(reader, 15)
        };
    }

    private static string? GetString(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);

    private static int? GetInt(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : Convert.ToInt32(r.GetValue(i));

    private static DateOnly? GetDate(NpgsqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetFieldValue<DateOnly>(i);
}
