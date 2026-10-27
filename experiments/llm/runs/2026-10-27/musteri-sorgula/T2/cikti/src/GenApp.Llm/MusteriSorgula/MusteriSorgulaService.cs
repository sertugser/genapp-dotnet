namespace GenApp.Llm.MusteriSorgula;

/// <summary>
/// Eski dönüş kodları (CA-RETURN-CODE).
/// </summary>
public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

/// <summary>
/// Operasyon sonucu: dönüş kodu ve (başarılıysa) müşteri.
/// DatabaseBusy: kilitlenme/zaman aşımı (eskide -913); dönüş kodu "90" olarak taşınır, HTTP 503'e çevrilir.
/// </summary>
public sealed record MusteriSorgulaResult(string ReturnCode, MusteriSorgulaResponse? Customer, bool DatabaseBusy = false)
{
    public static MusteriSorgulaResult Found(MusteriSorgulaResponse customer) => new(ReturnCodes.Success, customer);
    public static MusteriSorgulaResult NotFound() => new(ReturnCodes.NotFound, null);
    public static MusteriSorgulaResult Busy() => new(ReturnCodes.DatabaseError, null, DatabaseBusy: true);
    public static MusteriSorgulaResult DatabaseError() => new(ReturnCodes.DatabaseError, null);
}

/// <summary>
/// LGICUS01 karşılığı. COMMAREA uzunluk kontrolü (98) yeni sistemde yoktur; numarayı adres taşır.
/// </summary>
public sealed class MusteriSorgulaService
{
    private readonly MusteriSorgulaRepository _repository;

    public MusteriSorgulaService(MusteriSorgulaRepository repository)
    {
        _repository = repository;
    }

    public Task<MusteriSorgulaResult> InquireAsync(int customerNumber, CancellationToken cancellationToken)
    {
        // LGICUS01 yalnızca LGICDB01'i çağırır (GET-CUSTOMER-INFO); başka iş kuralı yoktur.
        return _repository.GetCustomerAsync(customerNumber, cancellationToken);
    }
}
