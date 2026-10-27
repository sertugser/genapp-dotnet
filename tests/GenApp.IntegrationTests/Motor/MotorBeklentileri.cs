using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Motor;

/// <summary>
/// <c>motor</c> satırında beklenen değerler. Alanlar AD §4.3 (AD:197-205), sütun adları <see cref="Sema"/> (DS:21 kuralı,
/// Db2 adları db2cre:276-285). İki eşleme belgede yoktur (G2) ve smoke testinde bulunan sütunlara ad karşılığıyla
/// kurulmuştur: <c>registrationNumber</c> (eski alan <c>CA-M-REGNUMBER</c>, AD:200) → <c>reg_number</c>;
/// <c>manufactureDate</c> (<c>CA-M-MANUFACTURED</c>, AD:203) → <c>year_of_manufacture</c> (<c>date</c>). Tutmazlarsa
/// BELGE BELİRSİZ olarak raporlanır; beklenti değiştirilmez.
/// </summary>
internal static class MotorBeklentileri
{
    public static Dictionary<string, object> Satir(IReadOnlyDictionary<string, object> gonderilen) => new()
    {
        [Sema.Motor.Make] = gonderilen["make"],
        [Sema.Motor.Model] = gonderilen["model"],
        [Sema.Motor.Value] = gonderilen["value"],
        [Sema.Motor.RegNumber] = gonderilen["registrationNumber"],                            // G2
        [Sema.Motor.Colour] = gonderilen["colour"],
        [Sema.Motor.Cc] = gonderilen["cc"],
        [Sema.Motor.YearOfManufacture] = PoliceBeklentileri.Tarih(gonderilen["manufactureDate"]), // G2
        [Sema.Motor.Premium] = gonderilen["premium"],
        [Sema.Motor.Accidents] = gonderilen["accidents"],
    };
}
