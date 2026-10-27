namespace GenApp.Llm.MusteriGuncelle;

/// <summary>
/// Eski sistemin dönüş kodları (CA-RETURN-CODE) — bu operasyonda oluşabilenler.
/// </summary>
public static class DonusKodlari
{
    public const string Basarili = "00";
    public const string KayitYok = "01";
    public const string VeritabaniHatasi = "90";
}

/// <summary>
/// Servis sonucu: dönüş kodu ve başarılıysa güncellenmiş müşteri.
/// </summary>
public sealed class MusteriGuncelleSonuc
{
    public required string ReturnCode { get; init; }

    public MusteriCevap? Musteri { get; init; }

    /// <summary>Kilitlenme ya da zaman aşımı (503, returnCode "90").</summary>
    public bool VeritabaniMesgul { get; init; }

    public static MusteriGuncelleSonuc Basarili(MusteriCevap musteri) =>
        new() { ReturnCode = DonusKodlari.Basarili, Musteri = musteri };

    public static MusteriGuncelleSonuc Hata(string returnCode) =>
        new() { ReturnCode = returnCode };

    public static MusteriGuncelleSonuc Mesgul() =>
        new() { ReturnCode = DonusKodlari.VeritabaniHatasi, VeritabaniMesgul = true };
}
