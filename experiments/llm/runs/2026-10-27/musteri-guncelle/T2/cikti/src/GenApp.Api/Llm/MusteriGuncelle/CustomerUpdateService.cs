using Npgsql;

namespace GenApp.Llm.MusteriGuncelle;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public sealed record CustomerUpdateData(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string Email);

public sealed record CustomerUpdateResult(string ReturnCode, CustomerResponse? Customer, bool DatabaseBusy = false);

/// <summary>
/// LGUCDB01'in UPDATE-CUSTOMER-INFO paragrafının karşılığı. LGUCVS01 (VSAM yeniden yazımı) yeni sistemde yoktur.
/// Müşteri güncellemesinde eşzamanlı güncelleme kontrolü yoktur; eski sistemde de yoktur.
/// </summary>
public sealed class CustomerUpdateService
{
    // 40P01: deadlock_detected, 55P03: lock_not_available, 57014: query_canceled (statement_timeout)
    private static readonly HashSet<string> BusySqlStates = ["40P01", "55P03", "57014"];

    private readonly CustomerUpdateRepository _repository;
    private readonly ILogger<CustomerUpdateService> _logger;

    public CustomerUpdateService(CustomerUpdateRepository repository, ILogger<CustomerUpdateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CustomerUpdateResult> UpdateAsync(int customerNumber, CustomerUpdateData data, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _repository.UpdateAsync(customerNumber, data, cancellationToken);

            // SQLCODE = 100 → '01' (lgucdb01.cbl:173-174); bilinçli sapma: VSAM'a gidilmez, 404 döner
            if (customer is null)
                return new CustomerUpdateResult(ReturnCodes.NotFound, null);

            return new CustomerUpdateResult(ReturnCodes.Success, customer);
        }
        catch (PostgresException ex) when (BusySqlStates.Contains(ex.SqlState))
        {
            _logger.LogError(ex, "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST SQLSTATE={SqlState} (kilitlenme/zaman aşımı)",
                customerNumber, ex.SqlState);
            return new CustomerUpdateResult(ReturnCodes.DatabaseError, null, DatabaseBusy: true);
        }
        catch (NpgsqlException ex) when (ex is not PostgresException && ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST zaman aşımı", customerNumber);
            return new CustomerUpdateResult(ReturnCodes.DatabaseError, null, DatabaseBusy: true);
        }
        catch (NpgsqlException ex)
        {
            // SQLCODE 0 ve 100 dışında → '90' ve hata kuyruğuna yazım (lgucdb01.cbl:175-178); kuyruk yerine ILogger
            _logger.LogError(ex, "LGUCDB01 CNUM={CustomerNumber} UPDATE CUST veritabanı hatası", customerNumber);
            return new CustomerUpdateResult(ReturnCodes.DatabaseError, null);
        }
    }
}
