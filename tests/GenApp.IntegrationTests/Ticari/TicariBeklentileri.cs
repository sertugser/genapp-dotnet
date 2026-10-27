using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Ticari;

/// <summary>
/// <c>commercial</c> satırında beklenen değerler. Alanlar AD §4.6 (AD:237-252), sütun adları <see cref="Sema"/> (DS:21
/// kuralı, Db2 adları db2cre:318-333). Üç eşleme belgede yoktur (G2) ve smoke testinde bulunan sütunlara ad karşılığıyla
/// kurulmuştur: <c>postcode</c> (eski alan <c>CA-B-Postcode</c>, AD:238) → <c>zipcode</c>; <c>latitude</c>
/// (<c>CA-B-Latitude</c>, AD:239) → <c>latitude_n</c>; <c>longitude</c> (<c>CA-B-Longitude</c>, AD:240) →
/// <c>longitude_w</c>. Tutmazlarsa BELGE BELİRSİZ olarak raporlanır; beklenti değiştirilmez.
/// </summary>
/// <remarks>
/// Doğrulanmayanlar: <c>issueDate</c>/<c>expiryDate</c>'in yazıldığı sütun (<c>policy.issue_date</c>/<c>expiry_date</c>
/// ya da <c>commercial.start_date</c>/<c>renewal_date</c>; belgede tanımsız, G2) ve <c>commercial.request_date</c>
/// (değeri tanımsız). Tarihler yalnızca API cevabında doğrulanır.
/// </remarks>
internal static class TicariBeklentileri
{
    public static Dictionary<string, object> Satir(IReadOnlyDictionary<string, object> gonderilen) => new()
    {
        [Sema.Commercial.Address] = gonderilen["address"],
        [Sema.Commercial.Zipcode] = gonderilen["postcode"],      // G2
        [Sema.Commercial.LatitudeN] = gonderilen["latitude"],    // G2
        [Sema.Commercial.LongitudeW] = gonderilen["longitude"],  // G2
        [Sema.Commercial.Customer] = gonderilen["customer"],
        [Sema.Commercial.PropertyType] = gonderilen["propertyType"],
        [Sema.Commercial.FirePeril] = gonderilen["firePeril"],
        [Sema.Commercial.FirePremium] = gonderilen["firePremium"],
        [Sema.Commercial.CrimePeril] = gonderilen["crimePeril"],
        [Sema.Commercial.CrimePremium] = gonderilen["crimePremium"],
        [Sema.Commercial.FloodPeril] = gonderilen["floodPeril"],
        [Sema.Commercial.FloodPremium] = gonderilen["floodPremium"],
        [Sema.Commercial.WeatherPeril] = gonderilen["weatherPeril"],
        [Sema.Commercial.WeatherPremium] = gonderilen["weatherPremium"],
        [Sema.Commercial.Status] = gonderilen["status"],
        [Sema.Commercial.RejectionReason] = gonderilen["rejectionReason"],
    };
}
