namespace GenApp.Llm.MusteriEkle;

public static class DonusKodlari
{
    public const string Basarili = "00";
    public const string VeritabaniHatasi = "90";
}

public sealed record MusteriEkleSonucu(string ReturnCode, MusteriCevabi? Musteri, bool VeritabaniMesgul)
{
    public static MusteriEkleSonucu Basarili(MusteriCevabi musteri) =>
        new(DonusKodlari.Basarili, musteri, false);

    public static MusteriEkleSonucu Hata(bool mesgul) =>
        new(DonusKodlari.VeritabaniHatasi, null, mesgul);
}
