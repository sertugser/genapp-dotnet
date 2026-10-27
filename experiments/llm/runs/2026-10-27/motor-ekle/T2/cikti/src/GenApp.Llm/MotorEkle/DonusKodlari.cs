namespace GenApp.Llm.MotorEkle;

/// <summary>
/// Bu operasyonda oluşabilen eski dönüş kodları (CA-RETURN-CODE).
/// </summary>
public static class DonusKodlari
{
    public const string Basarili = "00";
    public const string MusteriBulunamadi = "70";
    public const string VeritabaniHatasi = "90";
}

/// <summary>
/// Servis sonucu: dönüş kodu, başarılıysa poliçe; kilitlenme/zaman aşımı ayrıca işaretlenir (Karar 8).
/// </summary>
public sealed record MotorEkleSonucu(string ReturnCode, MotorPoliceCevabi? Police, bool VeritabaniMesgul = false);
