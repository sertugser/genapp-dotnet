using System.Security.Cryptography;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Ticari poliçe testlerinin girdileri. Değerler eski sistemin örnek verisinden (base/cntl/db2cre.jcl) alınmıştır; tahmin
/// dosyalarındaki girdi tabloları da aynı satırlara dayanır (tests/predictions/16-commercial-add.md "16-01 girdi ve beklenen
/// değerler", 17-commercial-inquire.md "17-04 girdi ve beklenen değerler" ve 17-17). JSON alan adları docs/api-design.md §4.2
/// ve §4.6'dan, tarih biçimi §1'den gelir. Metinler sondaki boşluklar olmadan yazılır (ADR 0002 Karar 6). Tam sayı alanlar
/// <see cref="int"/>, metin alanlar <see cref="string"/> olarak tutulur; böylece JSON'a doğru türle (sayı ya da metin) yazılır.
/// </summary>
/// <remarks>
/// <para>
/// <b>Tarihler POLICY tablosundan alınır, COMMERCIAL'dan değil.</b> Örnek verideki COMMERCIAL tarihleri LGAPDB01 ile
/// üretilmiş olamaz: COMMERCIAL 10'da yenileme tarihi (2011-08-24, :1187) başlangıçtan (2011-08-25, :1186) önce ve
/// başlangıç POLICY 10'un issueDate'inden (2011-08-22, :779) farklıdır (16-commercial-add.md Kod gözlemleri 9). 16-01 de bu
/// yüzden POLICY 10'un tarihlerini kullanır. LGAPDB01 eklemede aynı iki değeri hem POLICY'ye hem COMMERCIAL'a yazar
/// (16 Kod gözlemleri 7); bu yüzden POST ile kurulan poliçede eski sistem de sorgulamada gönderilen tarihleri döndürürdü
/// (17 Kod gözlemleri 4: sorgu tarihleri COMMERCIAL'dan okur). Tarihlerin hangi tablodan geldiği belgede tanımsızdır ve bu
/// testlerde ölçülmez.
/// </para>
/// <para>
/// <c>brokersReference</c> db2cre.jcl'de boştur (<c>''</c>, :784, :808). Boş gönderilir (api-design.md §1: metin alanları boş
/// gönderilebilir) ama cevapta doğrulanmaz: test ekibi boş metin beklentisi yazmama kararı verdi. Testler bu alanı
/// <see cref="BosGonderilenAlanlar"/> ile beklenen değerlerden çıkarır ve yorumlarında "Doğrulanmayanlar" altında sayar.
/// <see cref="Police10BrokerDegerleriyle"/>'de alan boş değildir ve doğrulanır.
/// </para>
/// <para>
/// <c>lastChanged</c> ve <c>policyNumber</c> istekte gönderilmez (api-design.md §4.2: <c>policyNumber</c> "yalnızca cevapta";
/// ticari poliçede güncelleme yoktur, §2.5). Her çağrı yeni bir sözlük döndürür; testler birbirinin verisini değiştiremez.
/// </para>
/// </remarks>
internal static class TicariVerisi
{
    /// <summary>Boş gönderilen ve cevapta doğrulanmayan alanlar (bkz. sınıf açıklaması).</summary>
    public static IReadOnlyList<string> BosGonderilenAlanlar { get; } = ["brokersReference"];

    /// <summary>
    /// Poliçe 10 (db2cre.jcl:777-786) ve COMMERCIAL 10'un (:1184-1203) değerleri; tarihler POLICY 10'dan (bkz. sınıf
    /// açıklaması). 16-01'in girdisi; 17a ve 17c sorgularında ve silme testlerinde kurulan poliçe.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police10 => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-08-22",             // :779 (POLICY 10; COMMERCIAL 10'un StartDate'i değil)
        ["expiryDate"] = "2012-08-21",            // :780 (POLICY 10; COMMERCIAL 10'un RenewalDate'i değil)
        ["brokerId"] = 0,                         // :783
        ["brokersReference"] = "",                // :784 (''); doğrulanmaz, bkz. sınıf açıklaması
        ["payment"] = 0,                          // :785
        ["address"] = "HURSLEY PARK",             // :1188
        ["postcode"] = "SO212JN",                 // :1189; S-O-2-1-2-J-N: ikinci karakter O harfi
        ["latitude"] = "51.026701",               // :1190
        ["longitude"] = "-1.398903",              // :1191
        ["customer"] = "IBM",                     // :1192
        ["propertyType"] = "OFFICES",             // :1193
        ["firePeril"] = 3,                        // :1194
        ["firePremium"] = 8000,                   // :1195
        ["crimePeril"] = 3,                       // :1196
        ["crimePremium"] = 25000,                 // :1197
        ["floodPeril"] = 5,                       // :1198
        ["floodPremium"] = 2000,                  // :1199
        ["weatherPeril"] = 4,                     // :1200
        ["weatherPremium"] = 5000,                // :1201
        ["status"] = 1,                           // :1202
        ["rejectionReason"] = "ACTIVE",           // :1203
    };

    /// <summary>
    /// Poliçe 9 (db2cre.jcl:801-810) ve COMMERCIAL 9'un (:1227-1246) değerleri; tarihler POLICY 9'dan (bkz. sınıf
    /// açıklaması). 17-17'nin (17b) ve 17d sorgularının kurulan poliçesi; 17-25 ve 17-35'te satır 1.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police9 => new Dictionary<string, object>
    {
        ["issueDate"] = "2011-09-23",             // :803 (POLICY 9; COMMERCIAL 9'un StartDate'i değil)
        ["expiryDate"] = "2012-09-22",            // :804 (POLICY 9; COMMERCIAL 9'un RenewalDate'i değil)
        ["brokerId"] = 0,                         // :807
        ["brokersReference"] = "",                // :808 (''); doğrulanmaz, bkz. sınıf açıklaması
        ["payment"] = 0,                          // :809
        ["address"] = "TURF MOOR",                // :1231
        ["postcode"] = "BB104BX",                 // :1232
        ["latitude"] = "53.78847",                // :1233
        ["longitude"] = "-2.230325",              // :1234
        ["customer"] = "Burnley Football Club",   // :1235
        ["propertyType"] = "STADIUM",             // :1236
        ["firePeril"] = 1,                        // :1237
        ["firePremium"] = 44000,                  // :1238
        ["crimePeril"] = 2,                       // :1239
        ["crimePremium"] = 25000,                 // :1240
        ["floodPeril"] = 3,                       // :1241
        ["floodPremium"] = 15000,                 // :1242
        ["weatherPeril"] = 4,                     // :1243
        ["weatherPremium"] = 6000,                // :1244
        ["status"] = 1,                           // :1245
        ["rejectionReason"] = "ACTIVE",           // :1246
    };

    /// <summary>
    /// 16-02'nin girdisi: poliçe 10, ama <c>customer</c> 39 karakterlik bir metin. Parçaları COMMERCIAL 9'dan:
    /// "Burnley Football Club" (:1235) + " " + "TURF MOOR" (:1231) + " " + "STADIUM" (:1236); 16-commercial-add.md, 16-02.
    /// Tahmin bu değeri VSAM kaydının 31 baytını aşan bir metin olarak kullanır.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police10UzunIsletmeAdiyla => new Dictionary<string, object>(Police10)
    {
        ["customer"] = "Burnley Football Club TURF MOOR STADIUM", // 39 karakter, en fazla 255 (api-design.md §4.6)
    };

    /// <summary>
    /// 17-16'nın girdisi: poliçe 10, ama ortak alanlarda sıfırdan farklı değerler. Seçilmiş değerler, kaynak yok
    /// (17-commercial-inquire.md, 17-16; tahmin bunları PIC sınırları içinde seçer, lgcmarea.cpy:41-43). api-design.md §4.2
    /// sınırları içindedir: brokerId en fazla 10 hane, brokersReference en fazla 10 karakter, payment en fazla 6 hane.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Police10BrokerDegerleriyle => new Dictionary<string, object>(Police10)
    {
        ["brokerId"] = 1234,                      // seçilmiş değer (17-16)
        ["brokersReference"] = "BRK0000001",      // seçilmiş değer (17-16); 10 karakter
        ["payment"] = 500,                        // seçilmiş değer (17-16)
    };

    /// <summary>
    /// 17-25 ve 17-35'te satır 2'nin <c>customer</c> değeri. Seçilmiş değer, kaynak yok (17-commercial-inquire.md, 03ICOM
    /// bölümü: satır k'nin <c>Customer</c>'ı <c>'SATIR-kk'</c>, diğer bütün sütunlar poliçe 9 ile aynı).
    /// </summary>
    public const string Satir2IsletmeAdi = "SATIR-02";

    private const string PostaKoduKarakterleri = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Teste özgü posta kodu: tam 8 karakter, büyük harf ve rakam. <b>Seçilmiş değer.</b> Gerekçe:
    /// <list type="bullet">
    /// <item>Posta koduyla sorgu (17d) bütün müşterilerin poliçelerini arar ve veritabanı sıfırlanmaz. Örnek verideki posta
    /// kodları (SO212JN, BB104BX) her çalıştırmada eklenen poliçelerde de bulunur; tahminlerin "tam 1 satır" ve "0 satır"
    /// koşulları ancak başka hiçbir kayıtta olmayan bir posta koduyla kurulabilir.</item>
    /// <item>8 karakter, <c>CA-B-Postcode</c>'un (X(8)) ve api-design.md §4.6'daki sınırın (8) tamamıdır. Eski sistemde
    /// <c>CHAR(8)</c> sütun ve host değişkeni sağdan boşlukla doldurulur (17 Kod gözlemleri 11); tam 8 karakterde sondaki
    /// boşluğun karşılaştırmaya etkisi sorusu doğmaz. Bu sınıftaki ve MusteriVerisi/KonutVerisi'ndeki sabit posta kodları
    /// 8 karakterden kısadır (SO212JN, BB104BX, SO211UP, E15WW, PI101OO, TB14TV); üretilen kod bunlarla çakışamaz.</item>
    /// <item>Büyük harf: büyük harfe çevirme api-design.md §1'de yalnızca müşteri için tanımlıdır; ticari posta kodunun
    /// çevrilip çevrilmeyeceği tanımsızdır. Değer zaten büyük harf olduğu için iki durumda da kayıttaki değer aynıdır.</item>
    /// <item>36^8 (yaklaşık 2,8 × 10^12) olası değer; iki testin aynı kodu üretmesi pratikte beklenmez.</item>
    /// </list>
    /// </summary>
    public static string OzguPostaKodu() => RandomNumberGenerator.GetString(PostaKoduKarakterleri, 8);
}
