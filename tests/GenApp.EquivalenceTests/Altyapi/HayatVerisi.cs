namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Hayat sigortası (endowment) poliçesi testlerinin girdileri. Değerler eski sistemin örnek verisinden
/// (base/cntl/db2cre.jcl) alınmıştır; tahmin dosyalarındaki girdi tabloları da aynı satırlara dayanır
/// (tests/predictions/12-endowment-add.md "12-01 girdi ve beklenen değerler", 13-endowment-inquire.md "13-01 beklenen
/// değerler", 14-endowment-update.md "14-01 girdi ve beklenen değerler"). JSON alan adları docs/api-design.md §4.2 ve
/// §4.5'ten, tarih biçimi §1'den gelir. Metinler sondaki boşluklar olmadan yazılır (ADR 0002 Karar 6). Tam sayı alanlar
/// <see cref="int"/>, metin alanlar <see cref="string"/> olarak tutulur; böylece JSON'a doğru türle (sayı ya da metin)
/// yazılır.
/// </summary>
/// <remarks>
/// <para>
/// <c>brokersReference</c> db2cre.jcl'de boştur (<c>''</c>). Boş gönderilir (api-design.md §1: metin alanları boş
/// gönderilebilir) ama cevapta doğrulanmaz: test ekibi boş metin beklentisi yazmama kararı verdi (bkz.
/// <see cref="KonutVerisi"/>). Testler bu alanı <see cref="BosGonderilenAlanlar"/> ile beklenen değerlerden çıkarır ve
/// yorumlarında "Doğrulanmayanlar" altında sayar.
/// </para>
/// <para>
/// <c>fundName</c> ve <c>lifeAssured</c> db2cre.jcl'de <c>CHAR(10)</c> ve <c>CHAR(31)</c> sütunlarına yazılır, değerler
/// sağa boşlukla tamamlanır (<c>'LIONTMR'</c> → 10 karakter). api-design.md §1 cevaplardaki sondaki boşlukların
/// kırpıldığını söyler; bu yüzden değerler boşluksuz yazılır (<c>"LIONTMR"</c>, <c>"J. MORRIS"</c>).
/// </para>
/// <para>
/// <c>lastChanged</c> ve <c>policyNumber</c> istekte gönderilmez: api-design.md §4.2 <c>policyNumber</c>'ı "yalnızca
/// cevapta" der; <c>lastChanged</c> yalnızca güncelleme isteğinde, sorgulamadan alınan değerle gönderilir (§5,
/// <see cref="PoliceYardimcilari.GuncellemeGovdesi"/>). Poliçenin <c>paddingData</c> sütunu API'de yoktur (§4.5); testler
/// onu ne gönderir ne doğrular. Her çağrı yeni bir sözlük döndürür; testler birbirinin verisini değiştiremez.
/// </para>
/// </remarks>
internal static class HayatVerisi
{
    /// <summary>Boş gönderilen ve cevapta doğrulanmayan alanlar (bkz. sınıf açıklaması).</summary>
    public static IReadOnlyList<string> BosGonderilenAlanlar { get; } = ["brokersReference"];

    /// <summary>31 karakterlik yaşam sigortalısı (<c>CA-E-LIFE-ASSURED</c> X(31), lgcmarea.cpy:53). 12-27 ve 14-29'un girdisi.</summary>
    public const string OtuzBirKarakterliYasamSigortalisi = "ABCDEFGHIJKLMNOPQRSTUVWXYZ12345";

    /// <summary>
    /// Poliçe 4 ve ENDOWMENT 4'ün değerleri (db2cre.jcl:851-857, :1034-1040). 12-01'in girdisi, 13-01'in beklenen
    /// değerleri; güncelleme ve silme testlerinde kurulan poliçe.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police4 => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-08-05",             // :851
        ["expiryDate"] = "2012-08-04",            // :852
        ["brokerId"] = 0,                         // :855
        ["brokersReference"] = "",                // :856 (''); doğrulanmaz, bkz. sınıf açıklaması
        ["payment"] = 0,                          // :857
        ["withProfits"] = "Y",                    // :1035
        ["equities"] = "Y",                       // :1034
        ["managedFund"] = "N",                    // :1036
        ["fundName"] = "LIONTMR",                 // :1037
        ["term"] = 5,                             // :1038
        ["sumAssured"] = 12500,                   // :1039
        ["lifeAssured"] = "J. MORRIS",            // :1040
    };

    /// <summary>
    /// 13-18'in girdisi: poliçe 4, ama <c>withProfits</c> <c>N</c> ve <c>equities</c> <c>Y</c>. Örnek veride iki bayrak her
    /// zaman aynıdır (13-endowment-inquire.md, 13-18); farklı değerler iki alanın yer değiştirip değiştirmediğini ayırt
    /// eder.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police4FarkliBayraklarla => new Dictionary<string, object>(Police4)
    {
        ["withProfits"] = "N",
        ["equities"] = "Y",
    };

    /// <summary>12-27'nin girdisi: poliçe 4, ama yaşam sigortalısı 31 karakter (12-endowment-add.md, 12-27).</summary>
    public static IReadOnlyDictionary<string, object> Police4OtuzBirKarakterliYasamSigortalisiyla =>
        new Dictionary<string, object>(Police4)
        {
            ["lifeAssured"] = OtuzBirKarakterliYasamSigortalisi,
        };

    /// <summary>
    /// Poliçe 5 ve ENDOWMENT 5'in değerleri (db2cre.jcl:827-833, :1015-1021). 13-02'nin ikinci veri kümesi ve 14-01'in
    /// güncelleme girdisi (14-endowment-update.md, "14-01 girdi ve beklenen değerler").
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police5 => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-10-01",             // :827
        ["expiryDate"] = "2012-09-30",            // :828
        ["brokerId"] = 0,                         // :831
        ["brokersReference"] = "",                // :832 (''); doğrulanmaz, bkz. sınıf açıklaması
        ["payment"] = 0,                          // :833
        ["withProfits"] = "N",                    // :1016
        ["equities"] = "N",                       // :1015
        ["managedFund"] = "N",                    // :1017
        ["fundName"] = "SHEPPA",                  // :1018
        ["term"] = 10,                            // :1019
        ["sumAssured"] = 50000,                   // :1020
        ["lifeAssured"] = "Shep",                 // :1021
    };

    /// <summary>
    /// 14-02'de eski <c>lastChanged</c> ile gönderilen ikinci (reddedilmesi beklenen) güncellemenin girdisi: 14-01'in
    /// isteği, ama hayat sigortası alanları ENDOWMENT 4'ün değerleri (db2cre.jcl:1034-1040).
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police5OrtakAlanlariHayat4 => new Dictionary<string, object>(Police5)
    {
        ["withProfits"] = "Y",                    // :1035
        ["equities"] = "Y",                       // :1034
        ["managedFund"] = "N",                    // :1036
        ["fundName"] = "LIONTMR",                 // :1037
        ["term"] = 5,                             // :1038
        ["sumAssured"] = 12500,                   // :1039
        ["lifeAssured"] = "J. MORRIS",            // :1040
    };

    /// <summary>14-27'nin güncelleme girdisi: 14-01'in isteği, ama <c>payment</c> 123 (Db2'deki değer 0, db2cre.jcl:857).</summary>
    public static IReadOnlyDictionary<string, object> Police5OdemeliYuzYirmiUc => new Dictionary<string, object>(Police5)
    {
        ["payment"] = 123,
    };

    /// <summary>14-29'un güncelleme girdisi: 14-01'in isteği, ama yaşam sigortalısı 31 karakter.</summary>
    public static IReadOnlyDictionary<string, object> Police5OtuzBirKarakterliYasamSigortalisiyla =>
        new Dictionary<string, object>(Police5)
        {
            ["lifeAssured"] = OtuzBirKarakterliYasamSigortalisi,
        };
}
