using System.Globalization;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// LGIPDB01 GET-MOTOR-DB2-INFO karşılığı: POLICY ve MOTOR tablolarının birleşiminden tek satır okur.
/// </summary>
public sealed class MotorPolicyRepository
{
    private readonly string _connectionString;

    public MotorPolicyRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Bilinçli sapma: policy_type 'M' değilse satır dönmez ve 01 (bulunamadı) olur (API tasarımı 3).
    private const string Sql = """
        SELECT p.issue_date,
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
           AND p.policy_type = 'M'
        """;

    public async Task<MotorPolicyResponse?> FindAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("customerNumber", customerNumber);
        command.Parameters.AddWithValue("policyNumber", policyNumber);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            // SQLCODE = 100 karşılığı
            return null;
        }

        return new MotorPolicyResponse
        {
            PolicyNumber = policyNumber,
            CustomerNumber = customerNumber,
            IssueDate = reader.IsDBNull(0) ? null : reader.GetFieldValue<DateOnly>(0),
            ExpiryDate = reader.IsDBNull(1) ? null : reader.GetFieldValue<DateOnly>(1),
            LastChanged = reader.IsDBNull(2)
                ? ""
                : reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
            // NULL ise COBOL alanı taşımaz; INITIALIZE DB2-POLICY'den kalan 0/boşluk döner (lgipdb01 GET-MOTOR-DB2-INFO)
            BrokerId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            BrokersReference = Text(reader, 4),
            Payment = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            Make = Text(reader, 6),
            Model = Text(reader, 7),
            Value = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            RegistrationNumber = Text(reader, 9),
            Colour = Text(reader, 10),
            Cc = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader.GetValue(11), CultureInfo.InvariantCulture),
            ManufactureDate = reader.IsDBNull(12) ? null : reader.GetFieldValue<DateOnly>(12),
            Premium = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
            Accidents = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
        };
    }

    private static string Text(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).TrimEnd();
}
