namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Motor poliçesi testlerinin girdileri. Değerler eski sistemin örnek verisinden (base/cntl/db2cre.jcl) alınmıştır;
/// tahmin dosyalarındaki girdi tabloları da aynı satırlara dayanır. JSON alan adları docs/api-design.md §4.2 ve
/// §4.3'ten, tarih biçimi §1'den gelir. Metinler sondaki boşluklar olmadan yazılır (ADR 0002 Karar 6).
/// Tam sayı alanlar <see cref="int"/>, metin alanlar <see cref="string"/> olarak tutulur; böylece JSON'a doğru
/// türle (sayı ya da metin) yazılır.
/// </summary>
/// <remarks>
/// <para>
/// <c>brokersReference</c> db2cre.jcl'de boştur (<c>''</c>, :952, :976, :1000). docs/api-design.md boş metnin
/// <b>cevapta</b> <c>""</c> mi <c>null</c> mı döneceğini tanımlamıyor (ADR 0005, Açık sorular: boş metin). Testler
/// bu belirsizliğe bağlanmasın diye her poliçede boş olmayan bir değer gönderilir. Boş değerin saklanıp
/// geri döndüğü bu testlerde sınanmamaktadır.
/// </para>
/// <para>
/// <c>lastChanged</c> ve <c>policyNumber</c> istekte gönderilmez: api-design.md §4.2 <c>policyNumber</c>'ı "yalnızca
/// cevapta" der; <c>lastChanged</c> yalnızca güncelleme isteğinde, sorgulamadan alınan değerle gönderilir (§5).
/// Her çağrı yeni bir sözlük döndürür; testler birbirinin verisini değiştiremez.
/// </para>
/// </remarks>
internal static class MotorVerisi
{
    /// <summary>
    /// Poliçe 1 ve motor 1'in değerleri (db2cre.jcl:945-954, :1105-1114). 04-01'in girdisi, 05-01'in beklenen
    /// değerleri, 06-01 ve 07-01'in ön koşulu.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Ford => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-05-11",             // :947
        ["expiryDate"] = "2012-05-10",            // :948
        ["brokerId"] = 0,                         // :951
        ["brokersReference"] = "BRK0000001",      // :952'de boş; bkz. sınıf açıklaması
        ["payment"] = 0,                          // :953
        ["make"] = "FORD",                        // :1106
        ["model"] = "KA",                         // :1107
        ["value"] = 8500,                         // :1108
        ["registrationNumber"] = "LL60LOO",       // :1109; L-L-6-0-L-O-O: dördüncü karakter sıfır, son ikisi O harfi
        ["colour"] = "ORANGE",                    // :1110
        ["cc"] = 1000,                            // :1111
        ["manufactureDate"] = "1999-07-19",       // :1112 (yearofmanufacture)
        ["premium"] = 450,                        // :1113
        ["accidents"] = 0,                        // :1114
    };

    /// <summary>
    /// Poliçe 3 ve motor 3'ün değerleri (db2cre.jcl:969-978, :1128-1137). 05-02'nin girdisi (prim ve kaza sayısı
    /// sıfırdan farklı) ve 06-01'in güncelleme girdisi.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Dennis => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-09-08",             // :971
        ["expiryDate"] = "2012-09-07",            // :972
        ["brokerId"] = 0,                         // :975
        ["brokersReference"] = "BRK0000003",      // :976'da boş; bkz. sınıf açıklaması
        ["payment"] = 0,                          // :977
        ["make"] = "DENNIS",                      // :1129
        ["model"] = "ENGINE",                     // :1130
        ["value"] = 23500,                        // :1131
        ["registrationNumber"] = "FIRE1",         // :1132
        ["colour"] = "RED",                       // :1133
        ["cc"] = 5000,                            // :1134
        ["manufactureDate"] = "1973-05-13",       // :1135
        ["premium"] = 2850,                       // :1136
        ["accidents"] = 12,                       // :1137
    };

    /// <summary>
    /// Poliçe 2 ve motor 2'nin değerleri (db2cre.jcl:993-1002, :1151-1160). 06-02'de, eski zaman damgasıyla
    /// gönderilen ikinci (reddedilmesi beklenen) güncellemenin girdisi.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Beetle => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-10-31",             // :995
        ["expiryDate"] = "2012-10-30",            // :996
        ["brokerId"] = 0,                         // :999
        ["brokersReference"] = "BRK0000002",      // :1000'de boş; bkz. sınıf açıklaması
        ["payment"] = 0,                          // :1001
        ["make"] = "VOLKSWAGEN",                  // :1152
        ["model"] = "BEETLE",                     // :1153
        ["value"] = 600,                          // :1154
        ["registrationNumber"] = "A567WWR",       // :1155
        ["colour"] = "PURPLE",                    // :1156
        ["cc"] = 1200,                            // :1157
        ["manufactureDate"] = "1968-10-31",       // :1158
        ["premium"] = 700,                        // :1159
        ["accidents"] = 1,                        // :1160
    };
}
