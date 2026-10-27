namespace GenApp.Llm.MusteriEkle;

public sealed record MusteriCevap(
    int CustomerNumber,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string HouseName,
    string HouseNumber,
    string Postcode,
    string PhoneMobile,
    string PhoneHome,
    string Email)
{
    // Cevaptaki metinlerin sonundaki boşluklar kırpılır; boş metin "" döner, null dönmez.
    public static MusteriCevap Olustur(KayitliMusteri m) => new(
        m.CustomerNumber,
        Kirp(m.FirstName),
        Kirp(m.LastName),
        m.DateOfBirth,
        Kirp(m.HouseName),
        Kirp(m.HouseNumber),
        Kirp(m.Postcode),
        Kirp(m.PhoneMobile),
        Kirp(m.PhoneHome),
        Kirp(m.Email));

    private static string Kirp(string? deger) => (deger ?? string.Empty).TrimEnd();
}
