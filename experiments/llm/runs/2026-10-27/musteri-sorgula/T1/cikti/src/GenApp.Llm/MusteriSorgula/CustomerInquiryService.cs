using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

public enum InquiryOutcome
{
    Success,
    NotFound,
    DatabaseBusy,
    DatabaseError,
}

public sealed record CustomerInquiryResult(InquiryOutcome Outcome, string ReturnCode, CustomerRecord? Customer)
{
    public static CustomerInquiryResult Found(CustomerRecord c) => new(InquiryOutcome.Success, ReturnCodes.Success, c);
    public static CustomerInquiryResult NotFound() => new(InquiryOutcome.NotFound, ReturnCodes.NotFound, null);
    public static CustomerInquiryResult Busy() => new(InquiryOutcome.DatabaseBusy, ReturnCodes.DatabaseError, null);
    public static CustomerInquiryResult Error() => new(InquiryOutcome.DatabaseError, ReturnCodes.DatabaseError, null);
}

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

/// <summary>LGICUS01 + LGICDB01 iş mantığı.</summary>
public sealed class CustomerInquiryService
{
    private readonly ICustomerInquiryRepository _repository;
    private readonly ILogger _logger;

    public CustomerInquiryService(ICustomerInquiryRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CustomerInquiryResult> InquireAsync(int customerNumber, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _repository.FindAsync(customerNumber, cancellationToken);

            // SQLCODE 0 → '00', SQLCODE 100 → '01' (lgicdb01.cbl GET-CUSTOMER-INFO)
            return customer is null
                ? CustomerInquiryResult.NotFound()
                : CustomerInquiryResult.Found(customer);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsLockOrTimeout(ex))
        {
            // Eski sistemde -913 → '01'; yeni sistemde bilinçli sapma: 503 + '90' (ADR 0002 Karar 8).
            _logger.LogWarning(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER kilitlenme/zaman aşımı SQLSTATE={SqlState}",
                customerNumber, (ex as PostgresException)?.SqlState);
            return CustomerInquiryResult.Busy();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidCastException)
        {
            // When Other → '90' + WRITE-ERROR-MESSAGE (LGSTSQ yerine ILogger).
            _logger.LogError(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER SQLSTATE={SqlState}",
                customerNumber, (ex as PostgresException)?.SqlState);
            return CustomerInquiryResult.Error();
        }
    }

    private static bool IsLockOrTimeout(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is
                "40P01"   // deadlock_detected
                or "55P03" // lock_not_available
                or "57014" // query_canceled (statement_timeout / lock_timeout)
                or "40001"; // serialization_failure
        }

        return ex is NpgsqlException { InnerException: TimeoutException } || ex is TimeoutException;
    }
}
