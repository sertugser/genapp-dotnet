using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorSorgula;

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
            // API tasarımı: kilitlenme ve zaman aşımı 503 / "90" (bilinçli sapma, Karar 8)
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı meşgul", customerNumber, policyNumber);
            return MotorSorgulaResult.Busy();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidCastException)
        {
            // lgipdb01.cbl: SQLCODE 0 ve 100 dışında '90' + WRITE-ERROR-MESSAGE
            _logger.LogError(ex, "LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: veritabanı hatası", customerNumber, policyNumber);
            return MotorSorgulaResult.DbError();
        }

        if (row is null)
        {
            // lgipdb01.cbl: SQLCODE = 100 → '01'
            return MotorSorgulaResult.NotFound();
        }

        // Bilinçli sapma: poliçe türü adresteki türle (motor 'M') uyuşmuyorsa '01'
        if (!string.Equals(row.PolicyType?.TrimEnd(), MotorPolicyType, StringComparison.Ordinal))
        {
            return MotorSorgulaResult.NotFound();
        }

        // Motor ve tarih kolonları göstergesiz (INDICATOR'sız) okunur; NULL gelirse Db2 -305 verir → '90'
        if (row.IssueDate is null || row.ExpiryDate is null || row.Make is null || row.Model is null ||
            row.Value is null || row.RegNumber is null || row.Colour is null || row.Cc is null ||
            row.YearOfManufacture is null || row.Premium is null || row.Accidents is null)
        {
            _logger.LogError("LGIPDB01 SELECT MOTOR CNUM={CustomerNumber} PNUM={PolicyNumber}: göstergesiz kolonda NULL (SQLCODE=-305)", customerNumber, policyNumber);
            return MotorSorgulaResult.DbError();
        }

        var response = new MotorPolicyResponse
        {
            PolicyNumber = policyNumber,
            CustomerNumber = customerNumber,
            IssueDate = row.IssueDate.Value,
            ExpiryDate = row.ExpiryDate.Value,
            LastChanged = row.LastChanged.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
            // IND-BROKERID / IND-PAYMENT = -1 ise alan taşınmaz, INITIALIZE değeri (0) kalır
            BrokerId = row.BrokerId is null ? 0 : FitUnsigned(row.BrokerId.Value, 10),
            // NULL BROKERSREFERENCE: INITIALIZE edilmiş boşluklar kalır → ""
            BrokersReference = (row.BrokersReference ?? "").TrimEnd(),
            Payment = row.Payment is null ? 0 : (int)FitUnsigned(row.Payment.Value, 6),
            Make = row.Make.TrimEnd(),
            Model = row.Model.TrimEnd(),
            Value = (int)FitUnsigned(row.Value.Value, 6),
            RegistrationNumber = row.RegNumber.TrimEnd(),
            Colour = row.Colour.TrimEnd(),
            Cc = (int)FitUnsigned(row.Cc.Value, 4),
            ManufactureDate = row.YearOfManufacture.Value,
            Premium = (int)FitUnsigned(row.Premium.Value, 6),
            Accidents = (int)FitUnsigned(row.Accidents.Value, 6)
        };

        return MotorSorgulaResult.Ok(response);
    }

    // COBOL MOVE işaretli tam sayı → işaretsiz PIC 9(n): işaret düşer, soldaki fazla haneler kesilir
    private static long FitUnsigned(int value, int digits)
    {
        long abs = Math.Abs((long)value);
        long modulus = (long)Math.Pow(10, digits);
        return abs % modulus;
    }

    private static bool IsBusy(Exception ex)
    {
        if (ex is PostgresException pg)
        {
            return pg.SqlState is "40P01" // deadlock_detected
                or "55P03"                // lock_not_available
                or "57014";               // query_canceled (statement_timeout)
        }

        if (ex is NpgsqlException npg && npg.InnerException is TimeoutException)
        {
            return true;
        }

        return ex is TimeoutException;
    }
}
