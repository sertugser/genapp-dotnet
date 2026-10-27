namespace GenApp.Llm.MusteriGuncelle;

public static class MusteriGuncelleReturnCodes
{
    public const string Basarili = "00";
    public const string KayitYok = "01";
    public const string VeritabaniHatasi = "90";
}

public sealed record MusteriGuncelleResult(
    string ReturnCode,
    MusteriGuncelleResponse? Musteri = null,
    bool VeritabaniMesgul = false);
