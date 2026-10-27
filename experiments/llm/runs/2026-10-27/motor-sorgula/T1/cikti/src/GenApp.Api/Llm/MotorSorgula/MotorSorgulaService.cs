using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public enum MotorSorgulaOutcome
{
    Success,
    NotFound,
    DatabaseError,
    DatabaseBusy
}

public sealed record MotorSorgulaResult(MotorSorgulaOutcome Outcome, string ReturnCode, MotorPolicyRow? Row);

public sealed class MotorSorgulaService
{
    private const string MotorPolicyType = "M";

    private readonly MotorSorgulaRepository _repository;
    private readonly ILogger<MotorSorgulaService> _logger;

    public MotorSorgulaService(MotorSorgulaRepository repository, ILogger<MotorSorgulaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MotorSorgulaResult> InquireAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        MotorPolicyRow? row;
        try
        {
            row = await _repository.FindAsync(customerNumber, policyNumber, cancellationToken);
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            // Bilinçli sapma (Karar 8): kilitlenme/zaman aşımı 503 + "90"
            _logger.LogWarning(ex, "LGIPDB01 SELECT MOTOR CNUM={Customer} PNUM={Policy}: veritabanı meşgul", customerNumber, policyNumber);
            return new MotorSorgulaResult(MotorSorgulaOutcome.DatabaseBusy, ReturnCodes.DatabaseError, null);
        }
        catch (Exception ex) when (ex is NpgsqlException or NullColumnException or InvalidCastException)
        {
            // lgipdb01.cbl GET-MOTOR-DB2-INFO: SQLCODE 0 ya da 100 dışında '90' ve hata mesajı (WRITE-ERROR-MESSAGE → ILogger)
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={Customer} PNUM={Policy}: veritabanı hatası", customerNumber, policyNumber);
            return new MotorSorgulaResult(MotorSorgulaOutcome.DatabaseError, ReturnCodes.DatabaseError, null);
        }

        // lgipdb01.cbl GET-MOTOR-DB2-INFO: SQLCODE = 100 → '01'
        if (row is null)
        {
            return new MotorSorgulaResult(MotorSorgulaOutcome.NotFound, ReturnCodes.NotFound, null);
        }

        // Bilinçli sapma: poliçe türü motor ('M') değilse 404 / "01"
        if (!string.Equals(row.PolicyType?.Trim(), MotorPolicyType, StringComparison.Ordinal))
        {
            return new MotorSorgulaResult(MotorSorgulaOutcome.NotFound, ReturnCodes.NotFound, null);
        }

        return new MotorSorgulaResult(MotorSorgulaOutcome.Success, ReturnCodes.Success, row);
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            // 40P01 deadlock_detected, 55P03 lock_not_available, 57014 query_canceled (statement_timeout)
            return pg.SqlState is "40P01" or "55P03" or "57014";
        }
        if (ex is NpgsqlException npg && npg.InnerException is TimeoutException)
        {
            return true;
        }
        return ex is TimeoutException;
    }
}
