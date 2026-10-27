namespace GenApp.Llm.MotorEkle;

public static class ReturnCodes
{
    public const string Success = "00";
    public const string CustomerNotFound = "70";
    public const string DatabaseError = "90";
}

public sealed record MotorPolicyAddResult(string ReturnCode, MotorPolicyResponse? Policy, bool DatabaseBusy = false);
