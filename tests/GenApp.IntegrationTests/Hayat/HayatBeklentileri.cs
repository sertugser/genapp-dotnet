using GenApp.IntegrationTests.Altyapi;

namespace GenApp.IntegrationTests.Hayat;

/// <summary>
/// <c>endowment</c> satırında beklenen değerler. Alanlar AD §4.5 (AD:222-228), sütun adları <see cref="Sema"/> (DS:21
/// kuralı, Db2 adları db2cre:204-211). API alanı ↔ sütun eşlemesi belgede yoktur (G2); hayatta API alan adları Db2
/// sütun adlarıyla birebir aynıdır. <c>withProfits</c>, <c>equities</c>, <c>managedFund</c> tek karakterlik metindir ve
/// yorumu eski koda bırakılmıştır (AD:230-231): yalnızca gönderilen değer geri okunur.
/// </summary>
/// <remarks>Doğrulanmayan: <c>padding_data</c> (API'de yok, değeri tanımsız).</remarks>
internal static class HayatBeklentileri
{
    public static Dictionary<string, object> Satir(IReadOnlyDictionary<string, object> gonderilen) => new()
    {
        [Sema.Endowment.WithProfits] = gonderilen["withProfits"],
        [Sema.Endowment.Equities] = gonderilen["equities"],
        [Sema.Endowment.ManagedFund] = gonderilen["managedFund"],
        [Sema.Endowment.FundName] = gonderilen["fundName"],
        [Sema.Endowment.Term] = gonderilen["term"],
        [Sema.Endowment.SumAssured] = gonderilen["sumAssured"],
        [Sema.Endowment.LifeAssured] = gonderilen["lifeAssured"],
    };
}
