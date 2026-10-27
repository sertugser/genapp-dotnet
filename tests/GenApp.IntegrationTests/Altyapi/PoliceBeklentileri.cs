namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// Dört poliçe türünde ortak beklentiler: cevaptaki alanlar ve <c>policy</c> satırı. Kaynaklar: ortak alanlar AD §4.2
/// (AD:184-191), <c>policy_type</c> harfi AD:136, tarih biçimi AD:17, sütun adları <see cref="Sema"/>.
/// </summary>
/// <remarks>
/// Ortak alanların (<c>brokerId</c>, <c>brokersReference</c>, <c>payment</c>, tarihler) yazıldığı sütunlar yalnızca
/// <c>policy</c> tablosunda vardır; API alanı ↔ sütun eşlemesi belgede yoktur (G2) ve ad karşılığıyla kurulur.
/// <c>last_changed</c> değer olarak karşılaştırılmaz (biçimi sunucuya bağlı, AD:257); testler yalnızca "boş değil" ve
/// "önceki değerden farklı / aynı" kontrolü yapar.
/// </remarks>
internal static class PoliceBeklentileri
{
    /// <summary>
    /// Cevapta beklenen alanlar: gönderilen değerler (<c>lastChanged</c> hariç; onu sunucu belirler), <c>customerNumber</c>
    /// ve <c>policyNumber</c> (AD:184-185, yalnızca cevapta).
    /// </summary>
    public static Dictionary<string, object> Cevap(IReadOnlyDictionary<string, object> gonderilen, int n, int p)
    {
        var beklenen = new Dictionary<string, object>(gonderilen);
        beklenen.Remove("lastChanged");
        beklenen["customerNumber"] = n;
        beklenen["policyNumber"] = p;
        return beklenen;
    }

    /// <summary>
    /// <c>policy</c> satırında beklenen değerler (<c>policy_number</c> hariç; satır onunla okunur, <c>last_changed</c>
    /// hariç). <paramref name="tarihlerDahil"/> ticari poliçede <c>false</c>: tarihlerin yazıldığı sütun tanımsız (Aşama 0, G2).
    /// </summary>
    public static Dictionary<string, object> PolicySatiri(
        IReadOnlyDictionary<string, object> gonderilen, int n, PoliceTuru tur, bool tarihlerDahil = true)
    {
        var beklenen = new Dictionary<string, object>
        {
            [Sema.Policy.CustomerNumber] = n,
            [Sema.Policy.PolicyType] = tur.Harf,
            [Sema.Policy.BrokerId] = gonderilen["brokerId"],
            [Sema.Policy.BrokersReference] = gonderilen["brokersReference"],
            [Sema.Policy.Payment] = gonderilen["payment"],
        };
        if (tarihlerDahil)
        {
            beklenen[Sema.Policy.IssueDate] = Tarih(gonderilen["issueDate"]);
            beklenen[Sema.Policy.ExpiryDate] = Tarih(gonderilen["expiryDate"]);
        }
        return beklenen;
    }

    /// <summary>API'nin <c>yyyy-MM-dd</c> tarihini (AD:17) <c>date</c> sütunuyla karşılaştırılacak değere çevirir.</summary>
    public static DateOnly Tarih(object deger) => DateOnly.ParseExact((string)deger, "yyyy-MM-dd");

    /// <summary><c>policy.last_changed</c> NOT NULL (DS:58); değeri döndürür (biçim karşılaştırılmaz).</summary>
    public static async Task<DateTime> LastChangedBosDegilAsync(this VeritabaniYardimcilari veritabani, int p)
    {
        var deger = await veritabani.LastChangedOkuAsync(p);
        Assert.True(deger is not null, $"policy.last_changed boş (policy_number = {p}); DS:58 NOT NULL.");
        return deger!.Value;
    }
}
