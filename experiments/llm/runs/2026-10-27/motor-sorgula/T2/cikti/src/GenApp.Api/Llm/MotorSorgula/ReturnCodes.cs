namespace GenApp.Llm.MotorSorgula;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string NotFound = "01";
    public const string DatabaseError = "90";
}

public sealed record MotorSorgulaResult(string ReturnCode, MotorPolicyResponse? Policy, bool DatabaseBusy = false)
{
    public static MotorSorgulaResult Ok(MotorPolicyResponse policy) => new(ReturnCodes.Success, policy);
    public static MotorSorgulaResult NotFound() => new(ReturnCodes.NotFound, null);
    public static MotorSorgulaResult DbError() => new(ReturnCodes.DatabaseError, null);
    public static MotorSorgulaResult Busy() => new(ReturnCodes.DatabaseError, null, true);
}
