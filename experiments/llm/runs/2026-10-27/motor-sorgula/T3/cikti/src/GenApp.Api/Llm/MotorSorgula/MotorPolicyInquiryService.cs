using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// LGIPOL01 + LGIPDB01 (istek kodu 01IMOT) iş mantığı; dönüş kodunu üretir.
/// </summary>
public sealed class MotorPolicyInquiryService
{
    private readonly MotorPolicyRepository _repository;
    private readonly ILogger _logger;

    public MotorPolicyInquiryService(MotorPolicyRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MotorPolicyInquiryResult> InquireAsync(int customerNumber, int policyNumber, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _repository.FindAsync(customerNumber, policyNumber, cancellationToken);
            return policy is null
                ? MotorPolicyInquiryResult.NotFound()
                : MotorPolicyInquiryResult.Success(policy);
        }
        catch (Exception ex) when (IsBusy(ex) && !cancellationToken.IsCancellationRequested)
        {
            // Bilinçli sapma (Karar 8): kilitlenme / zaman aşımı 503, returnCode 90
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı meşgul", customerNumber, policyNumber);
            return MotorPolicyInquiryResult.Busy();
        }
        catch (NpgsqlException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // WRITE-ERROR-MESSAGE karşılığı: hata loglanır, 90 döner
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı hatası", customerNumber, policyNumber);
            return MotorPolicyInquiryResult.DatabaseError();
        }
        catch (InvalidOperationException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Bağlantı/okuma sırasında oluşan diğer veritabanı hataları da 90 sayılır (lgipdb01 SQLCODE <> 0 ve <> 100)
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı hatası", customerNumber, policyNumber);
            return MotorPolicyInquiryResult.DatabaseError();
        }
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is "40P01" // deadlock_detected
                or "55P03"               // lock_not_available
                or "57014";              // query_canceled (statement_timeout)
        }

        if (ex is NpgsqlException npgsql)
        {
            return npgsql.InnerException is TimeoutException;
        }

        return ex is TimeoutException;
    }
}
