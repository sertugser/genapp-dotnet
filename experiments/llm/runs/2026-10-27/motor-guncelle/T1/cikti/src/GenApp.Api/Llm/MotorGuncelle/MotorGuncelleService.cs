using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MotorGuncelle;

/// <summary>
/// LGUPDB01'in motor yolu. Db2 imleci + SYNCPOINT yerine tek PostgreSQL transaction'ı;
/// VSAM yazımının (LGUPVS01) karşılığı yoktur.
/// </summary>
public sealed class MotorGuncelleService
{
    // Sorgulamada dönen lastChanged biçimi (API tasarımı, 6. bölüm örneği)
    internal const string LastChangedFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    private readonly string _connectionString;
    private readonly ILogger _logger;

    public MotorGuncelleService(string connectionString, ILogger logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<MotorGuncelleResult> UpdateAsync(
        int customerNumber, int policyNumber, MotorPolicyUpdateRequest request, CancellationToken ct)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            // 1) POLICY satırını kilitleyerek oku (POLICY_CURSOR ... FOR UPDATE)
            string? policyType;
            DateTime lastChanged;
            await using (var select = new NpgsqlCommand(
                """
                SELECT policy_type, last_changed
                FROM policy
                WHERE customer_number = @customerNumber AND policy_number = @policyNumber
                FOR UPDATE
                """, connection, transaction))
            {
                select.Parameters.AddWithValue("customerNumber", customerNumber);
                select.Parameters.AddWithValue("policyNumber", policyNumber);

                await using var reader = await select.ExecuteReaderAsync(ct);
                if (!await reader.ReadAsync(ct))
                {
                    // SQLCODE 100 -> '01' (lgupdb01.cbl:352); bilinçli sapma: 404 döner
                    await reader.DisposeAsync();
                    await transaction.RollbackAsync(ct);
                    return MotorGuncelleResult.NotFound();
                }

                policyType = reader.IsDBNull(0) ? null : reader.GetString(0);
                lastChanged = reader.GetDateTime(1);
            }

            // Bilinçli sapma: poliçe motor türünde değilse işlem yapılmaz, '01'
            if (!string.Equals(policyType?.Trim(), "M", StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(ct);
                return MotorGuncelleResult.NotFound();
            }

            // 2) Zaman damgası karşılaştırması (lgupdb01.cbl:278), metin olarak; uyuşmazsa '02' (:346), bilinçli sapma: 409
            var dbLastChanged = lastChanged.ToString(LastChangedFormat, CultureInfo.InvariantCulture);
            if (!string.Equals(request.LastChanged?.Trim(), dbLastChanged, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(ct);
                return MotorGuncelleResult.Conflict();
            }

            // 3) MOTOR tablosunu güncelle (UPDATE-MOTOR-DB2-INFO)
            await using (var updateMotor = new NpgsqlCommand(
                """
                UPDATE motor
                SET make = @make,
                    model = @model,
                    value = @value,
                    reg_number = @regNumber,
                    colour = @colour,
                    cc = @cc,
                    year_of_manufacture = @yearOfManufacture,
                    premium = @premium,
                    accidents = @accidents
                WHERE policy_number = @policyNumber
                """, connection, transaction))
            {
                updateMotor.Parameters.AddWithValue("make", request.Make ?? "");
                updateMotor.Parameters.AddWithValue("model", request.Model ?? "");
                updateMotor.Parameters.AddWithValue("value", request.Value!.Value);
                updateMotor.Parameters.AddWithValue("regNumber", request.RegistrationNumber ?? "");
                updateMotor.Parameters.AddWithValue("colour", request.Colour ?? "");
                updateMotor.Parameters.AddWithValue("cc", NpgsqlDbType.Smallint, (short)request.Cc!.Value);
                updateMotor.Parameters.AddWithValue("yearOfManufacture", NpgsqlDbType.Date, request.ManufactureDate!.Value);
                updateMotor.Parameters.AddWithValue("premium", request.Premium!.Value);
                updateMotor.Parameters.AddWithValue("accidents", request.Accidents!.Value);
                updateMotor.Parameters.AddWithValue("policyNumber", policyNumber);

                var affected = await updateMotor.ExecuteNonQueryAsync(ct);
                if (affected == 0)
                {
                    // SQLCODE 100 -> '01' (lgupdb01.cbl:485-492); bilinçli sapma: 404 döner
                    await transaction.RollbackAsync(ct);
                    return MotorGuncelleResult.NotFound();
                }
            }

            // 4) POLICY tablosunu güncelle ve zaman damgasını yenile (PAYMENT SET listesinde yok, :319-325)
            await using (var updatePolicy = new NpgsqlCommand(
                """
                UPDATE policy
                SET issue_date = @issueDate,
                    expiry_date = @expiryDate,
                    last_changed = clock_timestamp()::timestamp,
                    broker_id = @brokerId,
                    brokers_reference = @brokersReference
                WHERE policy_number = @policyNumber
                """, connection, transaction))
            {
                updatePolicy.Parameters.AddWithValue("issueDate", NpgsqlDbType.Date, request.IssueDate!.Value);
                updatePolicy.Parameters.AddWithValue("expiryDate", NpgsqlDbType.Date, request.ExpiryDate!.Value);
                // 9 haneyi aşan brokerId integer kolona sığmaz; veritabanı hatası '90' olur (en yakın: MOVE CA-BROKERID TO DB2-BROKERID-INT, :315)
                updatePolicy.Parameters.AddWithValue("brokerId", NpgsqlDbType.Bigint, request.BrokerId!.Value);
                updatePolicy.Parameters.AddWithValue("brokersReference", request.BrokersReference ?? "");
                updatePolicy.Parameters.AddWithValue("policyNumber", policyNumber);
                await updatePolicy.ExecuteNonQueryAsync(ct);
            }

            // 5) Yeni zaman damgasıyla güncel kaydı oku (lgupdb01.cbl:329-334); başarısızsa geri al ve '90' (:336-342)
            var policy = await ReadPolicyAsync(connection, transaction, policyNumber, ct);
            if (policy is null)
            {
                _logger.LogError("LGUPDB01 karşılığı: güncelleme sonrası poliçe okunamadı. CNUM={Customer} PNUM={Policy}",
                    customerNumber, policyNumber);
                await transaction.RollbackAsync(ct);
                return MotorGuncelleResult.DatabaseError(busy: false);
            }

            await transaction.CommitAsync(ct);
            return MotorGuncelleResult.Success(policy);
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            _logger.LogError(ex, "Motor poliçe güncelle: kilitlenme/zaman aşımı. CNUM={Customer} PNUM={Policy}",
                customerNumber, policyNumber);
            return MotorGuncelleResult.DatabaseError(busy: true);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Motor poliçe güncelle: veritabanı hatası. CNUM={Customer} PNUM={Policy}",
                customerNumber, policyNumber);
            return MotorGuncelleResult.DatabaseError(busy: false);
        }
    }

    private static async Task<MotorPolicyResponse?> ReadPolicyAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, int policyNumber, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT p.customer_number, p.policy_number, p.issue_date, p.expiry_date, p.last_changed,
                   p.broker_id, p.brokers_reference, p.payment,
                   m.make, m.model, m.value, m.reg_number, m.colour, m.cc,
                   m.year_of_manufacture, m.premium, m.accidents
            FROM policy p
            JOIN motor m ON m.policy_number = p.policy_number
            WHERE p.policy_number = @policyNumber
            """, connection, transaction);
        command.Parameters.AddWithValue("policyNumber", policyNumber);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new MotorPolicyResponse
        {
            CustomerNumber = reader.GetInt32(0),
            PolicyNumber = reader.GetInt32(1),
            IssueDate = reader.IsDBNull(2) ? null : reader.GetFieldValue<DateOnly>(2),
            ExpiryDate = reader.IsDBNull(3) ? null : reader.GetFieldValue<DateOnly>(3),
            LastChanged = reader.GetDateTime(4).ToString(LastChangedFormat, CultureInfo.InvariantCulture),
            BrokerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
            BrokersReference = Text(reader, 6),
            Payment = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            Make = Text(reader, 8),
            Model = Text(reader, 9),
            Value = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            RegistrationNumber = Text(reader, 11),
            Colour = Text(reader, 12),
            Cc = reader.IsDBNull(13) ? null : reader.GetInt16(13),
            ManufactureDate = reader.IsDBNull(14) ? null : reader.GetFieldValue<DateOnly>(14),
            Premium = reader.IsDBNull(15) ? null : reader.GetInt32(15),
            Accidents = reader.IsDBNull(16) ? null : reader.GetInt32(16),
        };
    }

    private static string Text(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).TrimEnd();

    // Karar 8: kilitlenme ve zaman aşımı 503 + '90'
    private static bool IsBusy(Exception ex) => ex switch
    {
        PostgresException pg => pg.SqlState is "40P01" or "55P03" or "57014" or "40001",
        NpgsqlException { InnerException: TimeoutException } => true,
        TimeoutException => true,
        _ => false,
    };
}
