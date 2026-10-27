namespace GenApp.Llm.MotorSorgula;

/// <summary>
/// Servis sonucu: eski CA-RETURN-CODE karşılığı ve (başarılıysa) poliçe verisi.
/// </summary>
public sealed class MotorPolicyInquiryResult
{
    public string ReturnCode { get; }
    public bool DatabaseBusy { get; }
    public MotorPolicyResponse? Policy { get; }

    private MotorPolicyInquiryResult(string returnCode, bool databaseBusy, MotorPolicyResponse? policy)
    {
        ReturnCode = returnCode;
        DatabaseBusy = databaseBusy;
        Policy = policy;
    }

    public static MotorPolicyInquiryResult Success(MotorPolicyResponse policy) => new("00", false, policy);
    public static MotorPolicyInquiryResult NotFound() => new("01", false, null);
    public static MotorPolicyInquiryResult DatabaseError() => new("90", false, null);
    public static MotorPolicyInquiryResult Busy() => new("90", true, null);
}
