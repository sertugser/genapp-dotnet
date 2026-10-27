using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MotorEkle;

public sealed record MotorPolicyInsertData(
    int CustomerNumber,
    DateOnly IssueDate,
    DateOnly ExpiryDate,
    long BrokerId,
    string BrokersReference,
    int Payment,
    string Make,
    string Model,
    int Value,
    string RegistrationNumber,
    string Colour,
    short Cc,
    DateOnly ManufactureDate,
    int Premium,
    int Accidents);

public enum InsertStatus
{
    Success,
    CustomerNotFound,
    PolicyInsertFailed,
    MotorInsertFailed,
}

public sealed record InsertOutcome(InsertStatus Status, int PolicyNumber, DateTime LastChanged, Exception? Error);

public sealed class MotorPolicyAddRepository
{
    private readonly string _connectionString;

    public MotorPolicyAddRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Db2 + VSAM iki aşamalı commit'in yerini tek PostgreSQL transaction'ı alır; VSAM yazımı (LGAPVS01) yoktur.
    public async Task<InsertOutcome> InsertAsync(MotorPolicyInsertData data, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int policyNumber;
        DateTime lastChanged;

        // INSERT-POLICY: numarayı veritabanı üretir, LASTCHANGED = CURRENT TIMESTAMP
        await using (var cmd = new NpgsqlCommand(
            """
            INSERT INTO policy
                (customer_number, issue_date, expiry_date, policy_type, last_changed,
                 broker_id, brokers_reference, payment)
            VALUES
                (@customer_number, @issue_date, @expiry_date, 'M', CURRENT_TIMESTAMP,
                 @broker_id, @brokers_reference, @payment)
            RETURNING policy_number, last_changed
            """, connection, transaction))
        {
            cmd.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = data.CustomerNumber });
            cmd.Parameters.Add(new NpgsqlParameter("issue_date", NpgsqlDbType.Date) { Value = data.IssueDate });
            cmd.Parameters.Add(new NpgsqlParameter("expiry_date", NpgsqlDbType.Date) { Value = data.ExpiryDate });
            // CA-BROKERID 10 hanelidir, kolon integer'dır; sınırı aşan değer PostgreSQL hatasıyla '90' olur (COBOL'da sessiz kesme vardı).
            cmd.Parameters.Add(new NpgsqlParameter("broker_id", NpgsqlDbType.Bigint) { Value = data.BrokerId });
            cmd.Parameters.Add(new NpgsqlParameter("brokers_reference", NpgsqlDbType.Varchar) { Value = data.BrokersReference });
            cmd.Parameters.Add(new NpgsqlParameter("payment", NpgsqlDbType.Integer) { Value = data.Payment });

            try
            {
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                await reader.ReadAsync(cancellationToken);
                policyNumber = reader.GetInt32(0);
                lastChanged = reader.GetDateTime(1);
            }
            catch (PostgresException ex)
            {
                await SafeRollbackAsync(transaction);
                // SQLCODE -530 karşılığı: yabancı anahtar ihlali (müşteri yok) → '70'
                var status = ex.SqlState == PostgresErrorCodes.ForeignKeyViolation
                    ? InsertStatus.CustomerNotFound
                    : InsertStatus.PolicyInsertFailed;
                return new InsertOutcome(status, 0, default, ex);
            }
        }

        // INSERT-MOTOR; kolon adları Db2 adlarının snake_case karşılığı varsayılmıştır (REGNUMBER, YEAROFMANUFACTURE).
        await using (var cmd = new NpgsqlCommand(
            """
            INSERT INTO motor
                (policy_number, make, model, value, reg_number, colour, cc,
                 year_of_manufacture, premium, accidents)
            VALUES
                (@policy_number, @make, @model, @value, @reg_number, @colour, @cc,
                 @year_of_manufacture, @premium, @accidents)
            """, connection, transaction))
        {
            cmd.Parameters.Add(new NpgsqlParameter("policy_number", NpgsqlDbType.Integer) { Value = policyNumber });
            cmd.Parameters.Add(new NpgsqlParameter("make", NpgsqlDbType.Varchar) { Value = data.Make });
            cmd.Parameters.Add(new NpgsqlParameter("model", NpgsqlDbType.Varchar) { Value = data.Model });
            cmd.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Integer) { Value = data.Value });
            cmd.Parameters.Add(new NpgsqlParameter("reg_number", NpgsqlDbType.Varchar) { Value = data.RegistrationNumber });
            cmd.Parameters.Add(new NpgsqlParameter("colour", NpgsqlDbType.Varchar) { Value = data.Colour });
            cmd.Parameters.Add(new NpgsqlParameter("cc", NpgsqlDbType.Smallint) { Value = data.Cc });
            cmd.Parameters.Add(new NpgsqlParameter("year_of_manufacture", NpgsqlDbType.Date) { Value = data.ManufactureDate });
            cmd.Parameters.Add(new NpgsqlParameter("premium", NpgsqlDbType.Integer) { Value = data.Premium });
            cmd.Parameters.Add(new NpgsqlParameter("accidents", NpgsqlDbType.Integer) { Value = data.Accidents });

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException ex)
            {
                // ABEND LGSQ karşılığı: POLICY satırı geri alınır, '90'
                await SafeRollbackAsync(transaction);
                return new InsertOutcome(InsertStatus.MotorInsertFailed, 0, default, ex);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new InsertOutcome(InsertStatus.Success, policyNumber, lastChanged, null);
    }

    private static async Task SafeRollbackAsync(NpgsqlTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (NpgsqlException)
        {
            // Bağlantı kopmuşsa geri alma zaten sunucu tarafında gerçekleşir.
        }
    }
}
