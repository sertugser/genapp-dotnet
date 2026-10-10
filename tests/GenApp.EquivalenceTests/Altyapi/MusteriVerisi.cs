namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// Müşteri testlerinin girdileri. Değerler eski sistemin örnek verisinden (base/cntl/db2cre.jcl) alınmıştır;
/// tahmin dosyalarındaki girdi tabloları da aynı satırlara dayanır. JSON alan adları docs/api-design.md §4.1'den,
/// tarih biçimi §1'den gelir. Metinler sondaki boşluklar olmadan yazılır (ADR 0002 Karar 6).
/// </summary>
/// <remarks>
/// db2cre.jcl'deki INSERT'te ev telefonu cep telefonundan önce gelir (:433-434). Değerler sıraya göre değil,
/// sütun adına göre eşlenmiştir. Her çağrı yeni bir sözlük döndürür; testler birbirinin verisini değiştiremez.
/// </remarks>
internal static class MusteriVerisi
{
    /// <summary>
    /// Müşteri 1'in değerleri (db2cre.jcl:437-446). 01-01'in girdisi, 02-01'in beklenen değerleri ve
    /// 03-01'in ön koşulu.
    /// </summary>
    public static IReadOnlyDictionary<string, string> AndrewPandy => new Dictionary<string, string>
    {
        ["firstName"] = "Andrew",                 // :438
        ["lastName"] = "Pandy",                   // :439
        ["dateOfBirth"] = "1950-07-11",           // :440
        ["houseName"] = "",                       // :441 (' '); boş metin docs/api-design.md §6 örneğinde kabul ediliyor
        ["houseNumber"] = "34",                   // :442
        ["postcode"] = "PI101OO",                 // :443; P-I-1-0-1-O-O: dördüncü karakter sıfır, son ikisi O harfi
        ["phoneMobile"] = "07799 123456",         // :445 (phonemobile)
        ["phoneHome"] = "01962 811234",           // :444 (phonehome)
        ["email"] = "A.Pandy@beebhouse.com",      // :446
    };

    /// <summary>
    /// 03-01'in güncelleme girdisi: müşteri 2'nin değerleri (db2cre.jcl:471-480), <c>phoneMobile</c> hariç.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ScottTracey => new Dictionary<string, string>
    {
        ["firstName"] = "Scott",                  // :472
        ["lastName"] = "Tracey",                  // :473
        ["dateOfBirth"] = "1965-09-30",           // :474
        ["houseName"] = "Tracey Island",          // :475
        ["houseNumber"] = "1",                    // :476
        ["postcode"] = "TB14TV",                  // :477
        // JCL'de boş (:479). api-design.md boş metin davranışını tanımlamıyor; 03-01 testi bu belirsizliğe
        // bağlanmasın diye phoneMobile boş olmayan değerle gönderilir. Boş değerin ezilmesi (03 Kod gözlemleri 7)
        // bu testte sınanmamaktadır.
        ["phoneMobile"] = "07700 900123",
        ["phoneHome"] = "001 911911",             // :478 (phonehome)
        ["email"] = "REFROOM@TBHOLDINGS.COM",     // :480
    };
}
