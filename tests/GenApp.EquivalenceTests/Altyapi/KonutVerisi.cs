namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Konut poliçesi testlerinin girdileri. Değerler eski sistemin örnek verisinden (base/cntl/db2cre.jcl) alınmıştır;
/// tahmin dosyalarındaki girdi tabloları da aynı satırlara dayanır (tests/predictions/08-house-add.md "08-01 girdi ve
/// beklenen değerler", 10-house-update.md "10-01 girdi ve beklenen değerler"). JSON alan adları docs/api-design.md §4.2
/// ve §4.4'ten, tarih biçimi §1'den gelir. Metinler sondaki boşluklar olmadan yazılır (ADR 0002 Karar 6). Tam sayı alanlar
/// <see cref="int"/>, metin alanlar <see cref="string"/> olarak tutulur; böylece JSON'a doğru türle (sayı ya da metin)
/// yazılır.
/// </summary>
/// <remarks>
/// <para>
/// <c>houseName</c> ve <c>brokersReference</c> db2cre.jcl'de boştur (<c>' '</c>, <c>''</c>). Boş gönderilirler
/// (api-design.md §1: metin alanları boş gönderilebilir) ama cevapta doğrulanmazlar: test ekibi boş metin beklentisi
/// yazmama kararı verdi. Testler bu alanları <see cref="BosGonderilenAlanlar"/> ile beklenen değerlerden çıkarır ve
/// yorumlarında "Doğrulanmayanlar" altında sayar.
/// </para>
/// <para>
/// <c>houseNumber</c> db2cre.jcl'de baştaki boşluklarla yazılmıştır (<c>'   5'</c> :1056, <c>' 12b'</c> :1090).
/// api-design.md yalnızca sondaki boşlukların kırpıldığını söyler (§1); baştaki boşluklara ne olacağı tanımlı olmadığı
/// için değer baştaki boşluklar olmadan gönderilir (<c>"5"</c>, <c>"12b"</c>). Baştaki boşlukların korunması bu
/// testlerde sınanmamaktadır.
/// </para>
/// <para>
/// <c>lastChanged</c> ve <c>policyNumber</c> istekte gönderilmez: api-design.md §4.2 <c>policyNumber</c>'ı "yalnızca
/// cevapta" der; <c>lastChanged</c> yalnızca güncelleme isteğinde, sorgulamadan alınan değerle gönderilir (§5,
/// <see cref="PoliceYardimcilari.GuncellemeGovdesi"/>). Her çağrı yeni bir sözlük döndürür; testler birbirinin verisini
/// değiştiremez.
/// </para>
/// </remarks>
internal static class KonutVerisi
{
    /// <summary>Boş gönderilen ve cevapta doğrulanmayan alanlar (bkz. sınıf açıklaması).</summary>
    public static IReadOnlyList<string> BosGonderilenAlanlar { get; } = ["houseName", "brokersReference"];

    /// <summary>
    /// Poliçe 6 ve HOUSE 6'nın değerleri (db2cre.jcl:873-882, :1051-1057). 08-01'in girdisi, 09-01'in beklenen
    /// değerleri; güncelleme ve silme testlerinde kurulan poliçe.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police6 => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-09-01",             // :875
        ["expiryDate"] = "2011-12-31",            // :876
        ["brokerId"] = 0,                         // :879
        ["brokersReference"] = "",                // :880 (''); doğrulanmaz, bkz. sınıf açıklaması
        ["payment"] = 0,                          // :881
        ["propertyType"] = "HOUSE",               // :1052
        ["bedrooms"] = 5,                         // :1053
        ["value"] = 1500000,                      // :1054
        ["houseName"] = "",                       // :1055 (' '); doğrulanmaz, bkz. sınıf açıklaması
        ["houseNumber"] = "5",                    // :1056 ('   5'); baştaki boşluklar olmadan, bkz. sınıf açıklaması
        ["postcode"] = "SO211UP",                 // :1057; S-O-2-1-1-U-P: ikinci karakter O harfi
    };

    /// <summary>
    /// 08-02'nin girdisi: poliçe 6, ama <c>houseName</c> müşteri 4'ün adresindeki ev adı (db2cre.jcl:543). Tahmin bu
    /// değeri yalnızca 9 bayttan uzun, kaynağı belli bir metin olarak kullanır (08-house-add.md, 08-02).
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police6UzunEvAdiyla => new Dictionary<string, object>(Police6)
    {
        ["houseName"] = "Picnic Basket",          // :543; 13 karakter, en fazla 20 (api-design.md §4.4)
    };

    /// <summary>
    /// Poliçe 8 ve HOUSE 8'in değerleri (db2cre.jcl:921-930, :1085-1091). 10-01'in güncelleme girdisi (10-house-update.md,
    /// "10-01 girdi ve beklenen değerler").
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police8 => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-08-12",             // :923
        ["expiryDate"] = "2012-08-11",            // :924
        ["brokerId"] = 0,                         // :927
        ["brokersReference"] = "",                // :928 (''); doğrulanmaz, bkz. sınıf açıklaması
        // :929. Police6'daki değerle (0, :881) aynı: güncellemede POST'taki ödeme gönderilir. Ödemenin güncellenmemesi
        // (10-18) API'de tanımsız olduğu için atlandı; testler payment'ı güncelleme sonrasında doğrulamaz.
        ["payment"] = 0,
        ["propertyType"] = "FLAT",                // :1086
        ["bedrooms"] = 1,                         // :1087
        ["value"] = 260000,                       // :1088
        ["houseName"] = "",                       // :1089 (' '); doğrulanmaz, bkz. sınıf açıklaması
        ["houseNumber"] = "12b",                  // :1090 (' 12b'); baştaki boşluk olmadan, bkz. sınıf açıklaması
        ["postcode"] = "E15WW",                   // :1091
    };

    /// <summary>
    /// 10-04'te eski <c>lastChanged</c> ile gönderilen ikinci (reddedilmesi beklenen) güncellemenin girdisi: 10-01'in
    /// isteği, ama konut alanları HOUSE 6'nın değerleri (db2cre.jcl:1052-1057; 10-house-update.md, 10-04).
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police8OrtakAlanlariHouse6 => new Dictionary<string, object>(Police8)
    {
        ["propertyType"] = "HOUSE",               // :1052
        ["bedrooms"] = 5,                         // :1053
        ["value"] = 1500000,                      // :1054
        ["houseName"] = "",                       // :1055 (' '); doğrulanmaz, bkz. sınıf açıklaması
        ["houseNumber"] = "5",                    // :1056 ('   5'); baştaki boşluklar olmadan, bkz. sınıf açıklaması
        ["postcode"] = "SO211UP",                 // :1057
    };
}
