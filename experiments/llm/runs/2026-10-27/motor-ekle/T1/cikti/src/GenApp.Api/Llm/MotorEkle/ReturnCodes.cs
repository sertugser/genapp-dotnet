namespace GenApp.Llm.MotorEkle;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string CustomerNotFound = "70";
    public const string DatabaseError = "90";
}

public sealed record MotorEkleResult(string ReturnCode, bool Busy, MotorPolicyResponse? Policy)
{
    public static MotorEkleResult Ok(MotorPolicyResponse policy) => new(ReturnCodes.Success, false, policy);
    public static MotorEkleResult CustomerNotFound() => new(ReturnCodes.CustomerNotFound, false, null);
    public static MotorEkleResult DatabaseError() => new(ReturnCodes.DatabaseError, false, null);
    public static MotorEkleResult DatabaseBusy() => new(ReturnCodes.DatabaseError, true, null);
}
