using System.Globalization;
using Npgsql;
using NpgsqlTypes;

namespace GenApp.Llm.MotorEkle;

public sealed class MotorEkleRepository
{
    private const string LastChangedFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    private readonly string _connectionString;
    private readonly ILogger<MotorEkleRepository> _logger;

    public MotorEkleRepository(string connectionString, ILogger<MotorEkleRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<MotorEkleResult> InsertAsync(NewMotorPolicy p, CancellationToken ct)
    {
        NpgsqlConnection? conn = null;
        NpgsqlTransaction? tx = null;
        try
        {
            conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            tx = await conn.BeginTransactionAsync(ct);

            // INSERT-POLICY (lgapdb01.cbl): numarayı veritabanı verir, LASTCHANGED = CURRENT TIMESTAMP.
            int policyNumber;
            DateTime lastChanged;
            try
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    INSERT INTO policy
                        (customer_number, issue_date, expiry_date, policy_type, last_changed,
                         broker_id, brokers_reference, payment)
                    VALUES
                        (@customer_number, @issue_date, @expiry_date, 'M', CURRENT_TIMESTAMP,
                         @broker_id, @brokers_reference, @payment)
                    RETURNING policy_number, last_changed
                    """, conn, tx);
                cmd.Parameters.Add(new NpgsqlParameter("customer_number", NpgsqlDbType.Integer) { Value = p.CustomerNumber });
                cmd.Parameters.Add(new NpgsqlParameter("issue_date", NpgsqlDbType.Date) { Value = p.IssueDate });
                cmd.Parameters.Add(new NpgsqlParameter("expiry_date", NpgsqlDbType.Date) { Value = p.ExpiryDate });
                cmd.Parameters.Add(new NpgsqlParameter("broker_id", NpgsqlDbType.Bigint) { Value = p.BrokerId });
                cmd.Parameters.Add(new NpgsqlParameter("brokers_reference", NpgsqlDbType.Varchar) { Value = p.BrokersReference });
                cmd.Parameters.Add(new NpgsqlParameter("payment", NpgsqlDbType.Integer) { Value = p.Payment });

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                await reader.ReadAsync(ct);
                policyNumber = reader.GetInt32(0);
                lastChanged = reader.GetDateTime(1);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                // SQLCODE -530 → '70' (lgapdb01.cbl INSERT-POLICY)
                _logger.LogWarning(ex, "LGAPDB01 INSERT POLICY CNUM={CustomerNumber}: müşteri bulunamadı (70)", p.CustomerNumber);
                await SafeRollbackAsync(tx);
                return MotorEkleResult.CustomerNotFound();
            }

            // INSERT-MOTOR: hata olursa ABEND LGSQ ile POLICY satırı da geri alınır.
            await using (var cmd = new NpgsqlCommand(
                """
                INSERT INTO motor
                    (policy_number, make, model, value, reg_number, colour, cc,
                     year_of_manufacture, premium, accidents)
                VALUES
                    (@policy_number, @make, @model, @value, @reg_number, @colour, @cc,
                     @year_of_manufacture, @premium, @accidents)
                """, conn, tx))
            {
                cmd.Parameters.Add(new NpgsqlParameter("policy_number", NpgsqlDbType.Integer) { Value = policyNumber });
                cmd.Parameters.Add(new NpgsqlParameter("make", NpgsqlDbType.Varchar) { Value = p.Make });
                cmd.Parameters.Add(new NpgsqlParameter("model", NpgsqlDbType.Varchar) { Value = p.Model });
                cmd.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Integer) { Value = p.Value });
                cmd.Parameters.Add(new NpgsqlParameter("reg_number", NpgsqlDbType.Varchar) { Value = p.RegistrationNumber });
                cmd.Parameters.Add(new NpgsqlParameter("colour", NpgsqlDbType.Varchar) { Value = p.Colour });
                cmd.Parameters.Add(new NpgsqlParameter("cc", NpgsqlDbType.Smallint) { Value = p.Cc });
                cmd.Parameters.Add(new NpgsqlParameter("year_of_manufacture", NpgsqlDbType.Date) { Value = p.ManufactureDate });
                cmd.Parameters.Add(new NpgsqlParameter("premium", NpgsqlDbType.Integer) { Value = p.Premium });
                cmd.Parameters.Add(new NpgsqlParameter("accidents", NpgsqlDbType.Integer) { Value = p.Accidents });
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);

            return MotorEkleResult.Ok(new MotorPolicyResponse(
                PolicyNumber: policyNumber,
                CustomerNumber: p.CustomerNumber,
                IssueDate: p.IssueDate,
                ExpiryDate: p.ExpiryDate,
                LastChanged: lastChanged.ToString(LastChangedFormat, CultureInfo.InvariantCulture),
                BrokerId: p.BrokerId,
                BrokersReference: p.BrokersReference.TrimEnd(),
                Payment: p.Payment,
                Make: p.Make.TrimEnd(),
                Model: p.Model.TrimEnd(),
                Value: p.Value,
                RegistrationNumber: p.RegistrationNumber.TrimEnd(),
                Colour: p.Colour.TrimEnd(),
                Cc: p.Cc,
                ManufactureDate: p.ManufactureDate,
                Premium: p.Premium,
                Accidents: p.Accidents));
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            _logger.LogError(ex, "LGAPDB01 CNUM={CustomerNumber}: veritabanı meşgul/zaman aşımı (90, 503)", p.CustomerNumber);
            await SafeRollbackAsync(tx);
            return MotorEkleResult.DatabaseBusy();
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "LGAPDB01 CNUM={CustomerNumber}: veritabanı hatası (90)", p.CustomerNumber);
            await SafeRollbackAsync(tx);
            return MotorEkleResult.DatabaseError();
        }
        catch (InvalidCastException ex)
        {
            _logger.LogError(ex, "LGAPDB01 CNUM={CustomerNumber}: veritabanı hatası (90)", p.CustomerNumber);
            await SafeRollbackAsync(tx);
            return MotorEkleResult.DatabaseError();
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
            if (conn is not null) await conn.DisposeAsync();
        }
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is TimeoutException) return true;
        if (ex is PostgresException pg)
        {
            return pg.SqlState is PostgresErrorCodes.DeadlockDetected
                or PostgresErrorCodes.LockNotAvailable
                or PostgresErrorCodes.QueryCanceled
                or PostgresErrorCodes.SerializationFailure;
        }
        return ex is NpgsqlException && ex.InnerException is TimeoutException;
    }

    private async Task SafeRollbackAsync(NpgsqlTransaction? tx)
    {
        if (tx is null) return;
        try
        {
            await tx.RollbackAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geri alma sırasında hata");
        }
    }
}
