using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Musteri;

/// <summary>
/// Müşteri testlerinde gönderilen gövdeden beklenen API cevabını ve <c>customer</c> satırını kurar. Beklentiler yalnızca
/// belgelerden gelir: alanlar AD §4.1, posta kodunun büyük harfe çevrilerek kaydedilmesi AD:19, tarih biçimi AD:17,
/// sütun adları <see cref="Sema"/> (DS:21 kuralı).
/// </summary>
/// <remarks>
/// API alanı ↔ sütun eşlemesi belgede yoktur (Aşama 0, G2); eşleme alan adının Db2 sütun adına karşılığıdır
/// (<c>email</c> ↔ <c>emailaddress</c> → <c>email_address</c>, smoke testinde bulundu). Bir alan tutmazsa BELGE
/// BELİRSİZ olarak raporlanır.
/// </remarks>
internal static class MusteriBeklentileri
{
    /// <summary>Cevapta beklenen alanlar: gönderilen değerler, <c>postcode</c> büyük harfle (AD:19).</summary>
    public static Dictionary<string, object> Cevap(IReadOnlyDictionary<string, object> gonderilen)
    {
        var beklenen = new Dictionary<string, object>(gonderilen);
        beklenen["postcode"] = ((string)gonderilen["postcode"]).ToUpperInvariant();
        return beklenen;
    }

    /// <summary><c>customer</c> satırında beklenen değerler (<c>customer_number</c> hariç; satır onunla okunur).</summary>
    public static Dictionary<string, object> Satir(IReadOnlyDictionary<string, object> gonderilen) => new()
    {
        [Sema.Customer.FirstName] = gonderilen["firstName"],
        [Sema.Customer.LastName] = gonderilen["lastName"],
        [Sema.Customer.DateOfBirth] = DateOnly.ParseExact((string)gonderilen["dateOfBirth"], "yyyy-MM-dd"),
        [Sema.Customer.HouseName] = gonderilen["houseName"],
        [Sema.Customer.HouseNumber] = gonderilen["houseNumber"],
        [Sema.Customer.Postcode] = ((string)gonderilen["postcode"]).ToUpperInvariant(), // AD:19 "kaydedilir"
        [Sema.Customer.PhoneMobile] = gonderilen["phoneMobile"],
        [Sema.Customer.PhoneHome] = gonderilen["phoneHome"],
        [Sema.Customer.EmailAddress] = gonderilen["email"],
    };

    /// <summary>
    /// Küçük harf içerdiği kesin olan, 8 karakterlik (AD:175) benzersiz posta kodu. <see cref="TestVerisi.PostaKodu"/>
    /// yalnızca rakamlardan oluşabileceği için başa harf eklenir; aksi halde büyük harfe çevirme sınanmamış olurdu.
    /// </summary>
    public static string KucukHarfliPostaKodu() => ("pk" + TestVerisi.Benzersiz(6)).ToLowerInvariant();
}
