namespace GenApp.Llm.MusteriSorgula;

/// <summary>Eski CA-RETURN-CODE değerleri.</summary>
public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public enum InquiryOutcome
{
    /// <summary>SQLCODE 0 → "00".</summary>
    Found,
    /// <summary>SQLCODE 100 → "01".</summary>
    NotFound,
    /// <summary>Kilitlenme / zaman aşımı (eskide -913 → "01"; bilinçli sapma: 503 + "90").</summary>
    DatabaseBusy,
    /// <summary>Diğer SQLCODE → "90".</summary>
    DatabaseError,
}

public sealed class CustomerInquiryResult
{
    public InquiryOutcome Outcome { get; }
    public CustomerResponse? Customer { get; }

    public string ReturnCode => Outcome switch
    {
        InquiryOutcome.Found => ReturnCodes.Success,
        InquiryOutcome.NotFound => ReturnCodes.NotFound,
        _ => ReturnCodes.DatabaseError,
    };

    private CustomerInquiryResult(InquiryOutcome outcome, CustomerResponse? customer)
    {
        Outcome = outcome;
        Customer = customer;
    }

    public static CustomerInquiryResult Found(CustomerResponse customer) => new(InquiryOutcome.Found, customer);
    public static CustomerInquiryResult NotFound() => new(InquiryOutcome.NotFound, null);
    public static CustomerInquiryResult Busy() => new(InquiryOutcome.DatabaseBusy, null);
    public static CustomerInquiryResult Error() => new(InquiryOutcome.DatabaseError, null);
}
