namespace GenApp.Llm.MotorGuncelle;

/// <summary>Operasyon sonucu: eski CA-RETURN-CODE karşılığı ve başarılıysa güncel poliçe.</summary>
public sealed record MotorGuncelleResult(string ReturnCode, MotorPolicyResponse? Policy = null, bool DatabaseBusy = false)
{
    public static MotorGuncelleResult Success(MotorPolicyResponse policy) => new("00", policy);
    public static MotorGuncelleResult NotFound() => new("01");
    public static MotorGuncelleResult Conflict() => new("02");
    public static MotorGuncelleResult DatabaseError(bool busy) => new("90", null, busy);
}
