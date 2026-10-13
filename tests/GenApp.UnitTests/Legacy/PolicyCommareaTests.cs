using GenApp.Domain.Legacy;

namespace GenApp.UnitTests.Legacy;

/// <summary>
/// Poliçe istekleri: CA-POLICY-REQUEST (lgcmarea.cpy:34-44) ve onu REDEFINES eden beş tür (:46-103).
/// Ortak alanlar motor testinde ayrıntılı ölçülür; diğer testler türe özgü alana (100. karakterden sonrası) bakar.
/// Türe özgü değerler örnek veriden alınmıştır (db2cre.jcl); hasar tablosunun örnek verisi yoktur.
/// </summary>
public sealed class PolicyCommareaTests
{
    private static string Bosluk(int adet) => new(' ', adet);

    // Motor değerleri poliçe 1'in örnek verisi (db2cre.jcl:1093-1115). Ortak alanlar docs/api-design.md §6 örneğinden.
    [Fact]
    public void Motor_ortak_ve_ture_ozgu_alanlari_kopya_kitaptaki_sirayla_yazilir()
    {
        var commarea = new MotorPolicyCommarea
        {
            RequestId = "01AMOT",
            CustomerNumber = 1,
            PolicyNumber = 1,
            IssueDate = "2026-01-01",
            ExpiryDate = "2027-01-01",
            LastChanged = "2026-10-15 10:22:31.123456",
            BrokerId = 42,
            BrokersReference = "BR1",
            Payment = 100,
            Make = "FORD",
            Model = "KA",
            Value = 8500,
            RegistrationNumber = "LL60LOO",
            Colour = "ORANGE",
            Cc = 1000,
            ManufactureDate = "1999-07-19",
            Premium = 450,
            Accidents = 0,
        };

        var metin = commarea.ToFixedWidth();

        var beklenen =
            "01AMOT" + "00" + "0000000001"                  //   0: başlık (18)
            + "0000000001"                                  //  18: CA-POLICY-NUM      9(10)
            + "2026-01-01"                                  //  28: CA-ISSUE-DATE      X(10)
            + "2027-01-01"                                  //  38: CA-EXPIRY-DATE     X(10)
            + "2026-10-15 10:22:31.123456"                  //  48: CA-LASTCHANGED     X(26)
            + "0000000042"                                  //  74: CA-BROKERID        9(10)
            + "BR1" + Bosluk(7)                             //  84: CA-BROKERSREF      X(10)
            + "000100"                                      //  94: CA-PAYMENT         9(6)
            + "FORD" + Bosluk(11)                           // 100: CA-M-MAKE          X(15)
            + "KA" + Bosluk(13)                             // 115: CA-M-MODEL         X(15)
            + "008500"                                      // 130: CA-M-VALUE         9(6)
            + "LL60LOO"                                     // 136: CA-M-REGNUMBER     X(7)
            + "ORANGE" + Bosluk(2)                          // 143: CA-M-COLOUR        X(8)
            + "1000"                                        // 151: CA-M-CC            9(4)
            + "1999-07-19"                                  // 155: CA-M-MANUFACTURED  X(10)
            + "000450"                                      // 165: CA-M-PREMIUM       9(6)
            + "000000";                                     // 171: CA-M-ACCIDENTS     9(6)

        Assert.Equal(beklenen, metin[..177]);
        Assert.Equal(Bosluk(32323), metin[177..]);          // 177: CA-M-FILLER        X(32323)
    }

    // Poliçe 5 (db2cre.jcl:1004-1022). CA-E-PADDING-DATA dolgu değil, veri taşır (lgapdb01.cbl:344).
    [Fact]
    public void Hayat_alanlari_ve_padding_verisi_kopya_kitaptaki_sirayla_yazilir()
    {
        var commarea = new EndowmentPolicyCommarea
        {
            RequestId = "01AEND",
            WithProfits = "N",
            Equities = "N",
            ManagedFund = "N",
            FundName = "SHEPPA",
            Term = 10,
            SumAssured = 50000,
            LifeAssured = "Shep",
            PaddingData = "EXTRA",
        };

        var metin = commarea.ToFixedWidth();

        var beklenen =
            "N"                                             // 100: CA-E-WITH-PROFITS  X
            + "N"                                           // 101: CA-E-EQUITIES      X
            + "N"                                           // 102: CA-E-MANAGED-FUND  X
            + "SHEPPA" + Bosluk(4)                          // 103: CA-E-FUND-NAME     X(10)
            + "10"                                          // 113: CA-E-TERM          99
            + "050000"                                      // 115: CA-E-SUM-ASSURED   9(6)
            + "Shep" + Bosluk(27)                           // 121: CA-E-LIFE-ASSURED  X(31)
            + "EXTRA" + Bosluk(32343);                      // 152: CA-E-PADDING-DATA  X(32348)

        Assert.Equal(beklenen, metin[100..]);
    }

    // Poliçe 6 (db2cre.jcl:1042-1058). Ev numarasının baştaki boşlukları korunur ('   5').
    [Fact]
    public void Konut_alanlari_kopya_kitaptaki_sirayla_yazilir()
    {
        var commarea = new HousePolicyCommarea
        {
            RequestId = "01AHOU",
            PropertyType = "HOUSE",
            Bedrooms = 5,
            Value = 1500000,
            HouseName = "",
            HouseNumber = "   5",
            Postcode = "SO211UP",
        };

        var metin = commarea.ToFixedWidth();

        var beklenen =
            "HOUSE" + Bosluk(10)                            // 100: CA-H-PROPERTY-TYPE X(15)
            + "005"                                         // 115: CA-H-BEDROOMS      9(3)
            + "01500000"                                    // 118: CA-H-VALUE         9(8)
            + Bosluk(20)                                    // 126: CA-H-HOUSE-NAME    X(20)
            + "   5"                                        // 146: CA-H-HOUSE-NUMBER  X(4)
            + "SO211UP" + Bosluk(1);                        // 150: CA-H-POSTCODE      X(8)

        Assert.Equal(beklenen, metin[100..158]);
        Assert.Equal(Bosluk(32342), metin[158..]);          // 158: CA-H-FILLER        X(32342)
    }

    // Poliçe 10 (db2cre.jcl:1162-1204).
    [Fact]
    public void Ticari_alanlari_kopya_kitaptaki_sirayla_yazilir()
    {
        var commarea = new CommercialPolicyCommarea
        {
            RequestId = "01ACOM",
            Address = "HURSLEY PARK",
            Postcode = "SO212JN",
            Latitude = "51.026701",
            Longitude = "-1.398903",
            Customer = "IBM",
            PropertyType = "OFFICES",
            FirePeril = 3,
            FirePremium = 8000,
            CrimePeril = 3,
            CrimePremium = 25000,
            FloodPeril = 5,
            FloodPremium = 2000,
            WeatherPeril = 4,
            WeatherPremium = 5000,
            Status = 1,
            RejectionReason = "ACTIVE",
        };

        var metin = commarea.ToFixedWidth();

        var beklenen =
            "HURSLEY PARK" + Bosluk(243)                    //  100: CA-B-Address        X(255)
            + "SO212JN" + Bosluk(1)                         //  355: CA-B-Postcode       X(8)
            + "51.026701" + Bosluk(2)                       //  363: CA-B-Latitude       X(11)
            + "-1.398903" + Bosluk(2)                       //  374: CA-B-Longitude      X(11)
            + "IBM" + Bosluk(252)                           //  385: CA-B-Customer       X(255)
            + "OFFICES" + Bosluk(248)                       //  640: CA-B-PropType       X(255)
            + "0003" + "00008000"                           //  895: CA-B-FirePeril 9(4), FirePremium 9(8)
            + "0003" + "00025000"                           //  907: CA-B-CrimePeril, CrimePremium
            + "0005" + "00002000"                           //  919: CA-B-FloodPeril, FloodPremium
            + "0004" + "00005000"                           //  931: CA-B-WeatherPeril, WeatherPremium
            + "0001"                                        //  943: CA-B-Status         9(4)
            + "ACTIVE" + Bosluk(249);                       //  947: CA-B-RejectReason   X(255)

        Assert.Equal(beklenen, metin[100..1202]);
        Assert.Equal(Bosluk(31298), metin[1202..]);         // 1202: CA-B-FILLER         X(31298)
    }

    // Hasar: hiçbir operasyon kullanmaz (docs/database-schema.md), örnek verisi yoktur; değerler uydurmadır.
    [Fact]
    public void Hasar_alanlari_kopya_kitaptaki_sirayla_yazilir()
    {
        var commarea = new ClaimCommarea
        {
            ClaimNumber = 1,
            ClaimDate = "2026-03-01",
            Paid = 1500,
            Value = 2000,
            Cause = "FLOOD",
            Observations = "KITCHEN",
        };

        var metin = commarea.ToFixedWidth();

        var beklenen =
            "0000000001"                                    // 100: CA-C-Num           9(10)
            + "2026-03-01"                                  // 110: CA-C-Date          X(10)
            + "00001500"                                    // 120: CA-C-Paid          9(8)
            + "00002000"                                    // 128: CA-C-Value         9(8)
            + "FLOOD" + Bosluk(250)                         // 136: CA-C-Cause         X(255)
            + "KITCHEN" + Bosluk(248);                      // 391: CA-C-Observations  X(255)

        Assert.Equal(beklenen, metin[100..646]);
        Assert.Equal(Bosluk(31854), metin[646..]);          // 646: CA-C-FILLER        X(31854)
    }
}
