using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Konut;

/// <summary>
/// <c>house</c> satırında beklenen değerler. Alanlar AD §4.4 (AD:211-216), sütun adları <see cref="Sema"/> (DS:21 kuralı,
/// Db2 adları db2cre:241-247). API alanı ↔ sütun eşlemesi belgede yoktur (G2); konutta API alan adları Db2 sütun
/// adlarıyla birebir aynıdır.
/// </summary>
internal static class KonutBeklentileri
{
    public static Dictionary<string, object> Satir(IReadOnlyDictionary<string, object> gonderilen) => new()
    {
        [Sema.House.PropertyType] = gonderilen["propertyType"],
        [Sema.House.Bedrooms] = gonderilen["bedrooms"],
        [Sema.House.Value] = gonderilen["value"],
        [Sema.House.HouseName] = gonderilen["houseName"],
        [Sema.House.HouseNumber] = gonderilen["houseNumber"],
        [Sema.House.Postcode] = gonderilen["postcode"],
    };
}
