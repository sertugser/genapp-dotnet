using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// LGICUS01 + LGICDB01 iş mantığı: müşteriyi numarasıyla okur ve dönüş kodunu üretir.
/// </summary>
public sealed class CustomerInquiryService
{
    private readonly CustomerInquiryRepository _repository;
    private readonly ILogger<CustomerInquiryService> _logger;

    public CustomerInquiryService(CustomerInquiryRepository repository, ILogger<CustomerInquiryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CustomerInquiryResult> InquireAsync(int customerNumber, CancellationToken cancellationToken)
    {
        try
        {
            var row = await _repository.FindAsync(customerNumber, cancellationToken);

            // lgicdb01.cbl: When 100 → '01'
            if (row is null)
                return CustomerInquiryResult.NotFound();

            // lgicdb01.cbl: When 0 → '00'
            return CustomerInquiryResult.Found(Map(row));
        }
        catch (Exception ex) when (IsBusy(ex))
        {
            // lgicdb01.cbl: When -913 → '01'; bilinçli sapma (Karar 8): 503 + "90"
            _logger.LogWarning(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER: veritabanı kilitlenmesi ya da zaman aşımı",
                customerNumber);
            return CustomerInquiryResult.Busy();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
        {
            // lgicdb01.cbl: When Other → '90' + WRITE-ERROR-MESSAGE (LGSTSQ yerine ILogger)
            _logger.LogError(ex,
                "LGICUS01 CNUM={CustomerNumber} SELECT CUSTOMER: veritabanı hatası (SQLSTATE={SqlState})",
                customerNumber, (ex as PostgresException)?.SqlState);
            return CustomerInquiryResult.Error();
        }
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is
                PostgresErrorCodes.DeadlockDetected or      // 40P01
                PostgresErrorCodes.LockNotAvailable or      // 55P03
                PostgresErrorCodes.QueryCanceled;           // 57014 (statement_timeout)
        }

        // Npgsql komut zaman aşımı
        return ex is NpgsqlException && ex.InnerException is TimeoutException;
    }

    private static CustomerResponse Map(CustomerRow row) => new()
    {
        CustomerNumber = row.CustomerNumber,
        FirstName = Clean(row.FirstName),
        LastName = Clean(row.LastName),
        DateOfBirth = row.DateOfBirth?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        HouseName = Clean(row.HouseName),
        HouseNumber = Clean(row.HouseNumber),
        Postcode = Clean(row.Postcode),
        PhoneMobile = Clean(row.PhoneMobile),
        PhoneHome = Clean(row.PhoneHome),
        Email = Clean(row.EmailAddress),
    };

    // Sondaki boşluklar kırpılır; NULL metin "" döner (Db2'de göstergesiz NULL okunamazdı, yeni sistemde boş sayılır).
    private static string Clean(string? value) => value?.TrimEnd(' ') ?? "";
}
