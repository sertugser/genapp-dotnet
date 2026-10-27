using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GenApp.Llm.MotorEkle;

public sealed class MotorPolicyAddService
{
    private readonly MotorPolicyAddRepository _repository;
    private readonly ILogger<MotorPolicyAddService> _logger;

    public MotorPolicyAddService(MotorPolicyAddRepository repository, ILogger<MotorPolicyAddService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<MotorPolicyAddResult> AddAsync(int customerNumber, MotorPolicyAddRequest request,
        CancellationToken cancellationToken)
    {
        var data = new MotorPolicyInsertData(
            customerNumber,
            request.IssueDate!.Value,
            request.ExpiryDate!.Value,
            request.BrokerId!.Value,
            request.BrokersReference ?? "",
            request.Payment!.Value,
            request.Make ?? "",
            request.Model ?? "",
            request.Value!.Value,
            request.RegistrationNumber ?? "",
            request.Colour ?? "",
            (short)request.Cc!.Value,
            request.ManufactureDate!.Value,
            request.Premium!.Value,
            request.Accidents!.Value);

        InsertOutcome outcome;
        try
        {
            outcome = await _repository.InsertAsync(data, cancellationToken);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            // LGSTSQ (GENAERRS kuyruğu) karşılığı: loglama.
            _logger.LogError(ex, "LGAPDB01 motor poliçe ekleme: veritabanı hatası. CNUM={CustomerNumber}", customerNumber);
            return new MotorPolicyAddResult(ReturnCodes.DatabaseError, null, IsBusy(ex));
        }

        switch (outcome.Status)
        {
            case InsertStatus.CustomerNotFound:
                // lgapdb01 INSERT-POLICY: SQLCODE -530 (yabancı anahtar ihlali) → '70'
                _logger.LogWarning(outcome.Error, "LGAPDB01 INSERT POLICY: müşteri bulunamadı. CNUM={CustomerNumber}",
                    customerNumber);
                return new MotorPolicyAddResult(ReturnCodes.CustomerNotFound, null);

            case InsertStatus.PolicyInsertFailed:
            case InsertStatus.MotorInsertFailed:
                // INSERT POLICY 'When Other' → '90'; INSERT MOTOR hatası → '90' + ABEND LGSQ (POLICY satırı geri alınır)
                _logger.LogError(outcome.Error, "LGAPDB01 {Step}: veritabanı hatası. CNUM={CustomerNumber}",
                    outcome.Status == InsertStatus.PolicyInsertFailed ? "INSERT POLICY" : "INSERT MOTOR", customerNumber);
                return new MotorPolicyAddResult(ReturnCodes.DatabaseError, null,
                    outcome.Error is not null && IsBusy(outcome.Error));
        }

        var response = new MotorPolicyResponse
        {
            PolicyNumber = outcome.PolicyNumber,
            CustomerNumber = customerNumber,
            IssueDate = data.IssueDate,
            ExpiryDate = data.ExpiryDate,
            LastChanged = outcome.LastChanged.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
            BrokerId = data.BrokerId,
            BrokersReference = data.BrokersReference.TrimEnd(),
            Payment = data.Payment,
            Make = data.Make.TrimEnd(),
            Model = data.Model.TrimEnd(),
            Value = data.Value,
            RegistrationNumber = data.RegistrationNumber.TrimEnd(),
            Colour = data.Colour.TrimEnd(),
            Cc = data.Cc,
            ManufactureDate = data.ManufactureDate,
            Premium = data.Premium,
            Accidents = data.Accidents,
        };
        return new MotorPolicyAddResult(ReturnCodes.Success, response);
    }

    // API tasarımı: kilitlenme ve zaman aşımı → 503 database-busy, returnCode "90" (Karar 8).
    private static bool IsBusy(Exception ex)
    {
        for (var e = (Exception?)ex; e is not null; e = e.InnerException)
        {
            if (e is TimeoutException) return true;
            if (e is PostgresException pg &&
                pg.SqlState is PostgresErrorCodes.DeadlockDetected
                    or PostgresErrorCodes.LockNotAvailable
                    or PostgresErrorCodes.QueryCanceled
                    or PostgresErrorCodes.SerializationFailure)
                return true;
        }
        return false;
    }
}
