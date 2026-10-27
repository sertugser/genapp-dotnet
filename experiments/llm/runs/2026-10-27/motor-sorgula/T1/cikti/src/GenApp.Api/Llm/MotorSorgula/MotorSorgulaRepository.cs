using Npgsql;

namespace GenApp.Llm.MotorSorgula;

public sealed record MotorPolicyRow(
    int PolicyNumber,
    int CustomerNumber,
    string? PolicyType,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    DateTime LastChanged,
    long BrokerId,
    string? BrokersReference,
    int Payment,
    string? Make,
    string? Model,
    int Value,
    string? RegistrationNumber,
    string? Colour,
    int Cc,
    DateOnly ManufactureDate,
    int Premium,
    int Accidents);

/// <summary>Gösterge değişkeni olmayan bir kolon NULL döndü (Db2'de SQLCODE -305).</summary>
public sealed class NullColumnException : Exception
{
    public NullColumnException(string column) : base($"'{column}' kolonu NULL döndü (SQLCODE -305 karşılığı).") { }
}

public sealed class MotorSorgulaRepository
{
    private readonly string _connectionString;

    public MotorSorgulaRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // lgipdb01.cbl GET-MOTOR-DB2-INFO: POLICY ile MOTOR birleşimi, müşteri ve poliçe numarasıyla tek satır
    private const string Sql = """
        SELECT p.policy_number,
               p.customer_number,
               p.policy_type,
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
          JOIN motor m ON m.policy_number = p.policy_number
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
            return null;
        }

        return new MotorPolicyRow(
            PolicyNumber: reader.GetInt32(0),
            CustomerNumber: reader.GetInt32(1),
            PolicyType: reader.IsDBNull(2) ? null : reader.GetString(2),
            IssueDate: Required<DateOnly>(reader, 3, "issue_date"),
            ExpiryDate: Required<DateOnly>(reader, 4, "expiry_date"),
            LastChanged: Required<DateTime>(reader, 5, "last_changed"),
            // lgipdb01.cbl: IND-BROKERID = -1 ise alan taşınmaz, INITIALIZE değeri (0) kalır
            BrokerId: reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
            // lgipdb01.cbl: IND-BROKERSREF ile NULL kabul edilir, INITIALIZE değeri (boşluk) kalır
            BrokersReference: reader.IsDBNull(7) ? "" : reader.GetString(7),
            // lgipdb01.cbl: IND-PAYMENT = -1 ise alan taşınmaz, INITIALIZE değeri (0) kalır
            Payment: reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            // Aşağıdaki kolonların COBOL'da gösterge değişkeni yok; NULL gelirse SQLCODE -305 → '90'
            Make: Required<string>(reader, 9, "make"),
            Model: Required<string>(reader, 10, "model"),
            Value: Required<int>(reader, 11, "value"),
            RegistrationNumber: Required<string>(reader, 12, "reg_number"),
            Colour: Required<string>(reader, 13, "colour"),
            Cc: Required<short>(reader, 14, "cc"),
            ManufactureDate: Required<DateOnly>(reader, 15, "year_of_manufacture"),
            Premium: Required<int>(reader, 16, "premium"),
            Accidents: Required<int>(reader, 17, "accidents"));
    }

    private static T Required<T>(NpgsqlDataReader reader, int ordinal, string column)
    {
        if (reader.IsDBNull(ordinal))
        {
            throw new NullColumnException(column);
        }
        return reader.GetFieldValue<T>(ordinal);
    }
}
