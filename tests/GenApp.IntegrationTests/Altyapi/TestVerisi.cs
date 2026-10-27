namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// İstek gövdeleri. Alan adları ve türleri docs/api-design.md §4'ten (AD:165-252), tarih biçimi §1'den (AD:17) gelir;
/// motor değerleri AD §6 örneğinden (AD:299-315) alınmıştır, diğer türler aynı biçimde kurulmuştur. Örnek veriye
/// (db2cre.jcl) dayanmaz. Her çağrı yeni bir sözlük döndürür; testler birbirinin verisini değiştiremez.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Metinler en fazla uzunlukların (AD §4) içindedir ve sonlarında boşluk yoktur (AD:18). Boş metin kullanılmaz:
/// AD:20 boş metnin cevaptaki halini tanımlar ama veritabanındaki halini tanımlamaz.</item>
/// <item>AD §6 örneğindeki <c>brokerId: 0</c>, <c>brokersReference: ""</c>, <c>payment: 0</c> yerine sıfır ve boş
/// olmayan değerler kullanılır; böylece doğrulama gerçekten bir değer okur.</item>
/// <item>Benzersizlik yalnızca sorguda anahtar olarak kullanılan değerler için gerekir (posta kodu Z); diğer alanlar
/// sabittir, çünkü testler kendi müşteri ve poliçe numaralarıyla sorgular.</item>
/// </list>
/// </remarks>
internal static class TestVerisi
{
    /// <summary>Büyük harf ve rakamlardan oluşan, <paramref name="uzunluk"/> karakterlik rastgele metin (en fazla 32).</summary>
    public static string Benzersiz(int uzunluk) => Guid.NewGuid().ToString("N")[..uzunluk].ToUpperInvariant();

    /// <summary>
    /// 8 karakterlik (AD:216, :238 en fazla 8), büyük harfli, teste özgü posta kodu. Ticari posta koduyla sorguda (17d)
    /// anahtar Z olarak kullanılır. Ticari posta kodunun büyük harfe çevrilip çevrilmediği ve sorgunun büyük/küçük harfe
    /// duyarlılığı tanımsız olduğu için yalnızca büyük harf kullanılır.
    /// </summary>
    public static string PostaKodu() => Benzersiz(8);

    /// <summary>Müşteri ekle/güncelle gövdesi (AD §4.1, örnek AD:272-282).</summary>
    public static Dictionary<string, object> Musteri() => new()
    {
        ["firstName"] = "Ayse",                 // en fazla 10
        ["lastName"] = "Yilmaz",                // 20
        ["dateOfBirth"] = "1990-04-12",
        ["houseName"] = "Gul Apartmani",        // 20
        ["houseNumber"] = "12",                 // 4
        ["postcode"] = PostaKodu(),             // 8
        ["phoneMobile"] = "05551234567",        // 20
        ["phoneHome"] = "03121234567",          // 20
        ["email"] = "ayse@example.com",         // 100
    };

    /// <summary>Müşteri güncellemesinde gönderilen, <see cref="Musteri"/>'den her alanı farklı değerler.</summary>
    public static Dictionary<string, object> MusteriYeni() => new()
    {
        ["firstName"] = "Mehmet",
        ["lastName"] = "Demir",
        ["dateOfBirth"] = "1975-11-03",
        ["houseName"] = "Lale Sitesi",
        ["houseNumber"] = "7A",
        ["postcode"] = PostaKodu(),
        ["phoneMobile"] = "05329876543",
        ["phoneHome"] = "02161234567",
        ["email"] = "mehmet@example.com",
    };

    /// <summary>Poliçelerin ortak alanları (AD §4.2), ekleme değerleri.</summary>
    public static Dictionary<string, object> OrtakAlanlar() => new()
    {
        ["issueDate"] = "2026-01-01",           // AD:300
        ["expiryDate"] = "2027-01-01",          // AD:301
        ["brokerId"] = 1234,                    // en fazla 10 hane
        ["brokersReference"] = "BR-0001",       // 10
        ["payment"] = 450,                      // 6 hane
    };

    /// <summary>Poliçelerin ortak alanları, güncelleme değerleri (her alan farklı).</summary>
    public static Dictionary<string, object> OrtakAlanlarYeni() => new()
    {
        ["issueDate"] = "2026-02-01",
        ["expiryDate"] = "2027-02-01",
        ["brokerId"] = 5678,
        ["brokersReference"] = "BR-0002",
        ["payment"] = 990,
    };

    /// <summary>Motor poliçesi ekle gövdesi (AD §4.3; değerler AD:306-314).</summary>
    public static Dictionary<string, object> Motor() => Birlestir(OrtakAlanlar(), new()
    {
        ["make"] = "Ford",
        ["model"] = "Focus",
        ["value"] = 15000,
        ["registrationNumber"] = "06AB123",     // 7
        ["colour"] = "Blue",                    // 8
        ["cc"] = 1600,                          // 4 hane
        ["manufactureDate"] = "2020-05-01",
        ["premium"] = 450,
        ["accidents"] = 0,
    });

    /// <summary>Motor poliçesi, güncelleme değerleri (her alan farklı).</summary>
    public static Dictionary<string, object> MotorYeni() => Birlestir(OrtakAlanlarYeni(), new()
    {
        ["make"] = "Renault",
        ["model"] = "Clio",
        ["value"] = 12000,
        ["registrationNumber"] = "34CD456",
        ["colour"] = "Red",
        ["cc"] = 1200,
        ["manufactureDate"] = "2019-03-15",
        ["premium"] = 520,
        ["accidents"] = 1,
    });

    /// <summary>Konut poliçesi ekle gövdesi (AD §4.4).</summary>
    public static Dictionary<string, object> Konut() => Birlestir(OrtakAlanlar(), new()
    {
        ["propertyType"] = "Flat",              // 15
        ["bedrooms"] = 3,                       // 3 hane
        ["value"] = 250000,                     // 8 hane
        ["houseName"] = "Gul Apartmani",        // 20
        ["houseNumber"] = "12",                 // 4
        ["postcode"] = "AB12CD",                // 8
    });

    /// <summary>Konut poliçesi, güncelleme değerleri (her alan farklı).</summary>
    public static Dictionary<string, object> KonutYeni() => Birlestir(OrtakAlanlarYeni(), new()
    {
        ["propertyType"] = "Detached",
        ["bedrooms"] = 5,
        ["value"] = 480000,
        ["houseName"] = "Lale Evi",
        ["houseNumber"] = "3",
        ["postcode"] = "EF34GH",
    });

    /// <summary>Hayat sigortası poliçesi ekle gövdesi (AD §4.5; tek karakterlik alanların yorumu eski koda bırakılmış, AD:230-231).</summary>
    public static Dictionary<string, object> Hayat() => Birlestir(OrtakAlanlar(), new()
    {
        ["withProfits"] = "Y",                  // 1
        ["equities"] = "N",                     // 1
        ["managedFund"] = "Y",                  // 1
        ["fundName"] = "Fon A",                 // 10
        ["term"] = 20,                          // 2 hane
        ["sumAssured"] = 100000,                // 6 hane
        ["lifeAssured"] = "Ayse Yilmaz",        // 31
    });

    /// <summary>Hayat sigortası poliçesi, güncelleme değerleri (her alan farklı).</summary>
    public static Dictionary<string, object> HayatYeni() => Birlestir(OrtakAlanlarYeni(), new()
    {
        ["withProfits"] = "N",
        ["equities"] = "Y",
        ["managedFund"] = "N",
        ["fundName"] = "Fon B",
        ["term"] = 15,
        ["sumAssured"] = 250000,
        ["lifeAssured"] = "Mehmet Demir",
    });

    /// <summary>Ticari poliçe ekle gövdesi (AD §4.6). Posta kodu teste özgüdür (17d'nin anahtarı Z).</summary>
    public static Dictionary<string, object> Ticari(string postaKodu) => Birlestir(OrtakAlanlar(), new()
    {
        ["address"] = "Ataturk Bulvari 100 Ankara", // 255
        ["postcode"] = postaKodu,               // 8
        ["latitude"] = "39.9208N",              // 11
        ["longitude"] = "32.8541E",             // 11
        ["customer"] = "Ornek Ticaret Ltd",     // 255
        ["propertyType"] = "Office",            // 255
        ["firePeril"] = 1,                      // 4 hane
        ["firePremium"] = 500,                  // 8 hane
        ["crimePeril"] = 2,
        ["crimePremium"] = 300,
        ["floodPeril"] = 3,
        ["floodPremium"] = 200,
        ["weatherPeril"] = 4,
        ["weatherPremium"] = 100,
        ["status"] = 1,                         // 4 hane
        ["rejectionReason"] = "Yok",            // 255
    });

    /// <summary>
    /// Poliçe güncelleme gövdesi: yeni değerler ve sorgulamadan gelen <c>lastChanged</c>, değiştirilmeden (AD §5, örnek
    /// AD:296-315).
    /// </summary>
    public static Dictionary<string, object> GuncellemeGovdesi(Dictionary<string, object> yeniDegerler, string lastChanged) =>
        Birlestir(yeniDegerler, new() { ["lastChanged"] = lastChanged });

    private static Dictionary<string, object> Birlestir(Dictionary<string, object> ilk, Dictionary<string, object> ikinci)
    {
        foreach (var (alan, deger) in ikinci)
        {
            ilk[alan] = deger;
        }
        return ilk;
    }
}
