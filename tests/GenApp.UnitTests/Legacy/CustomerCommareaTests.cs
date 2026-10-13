using GenApp.Domain.Legacy;

namespace GenApp.UnitTests.Legacy;

/// <summary>
/// Müşteri istekleri: CA-CUSTOMER-REQUEST (lgcmarea.cpy:15-26) ve CA-CUSTSECR-REQUEST (:28-32).
/// Beklenen metinler alan alan, uzunlukları elle sayılarak yazılmıştır.
/// </summary>
public sealed class CustomerCommareaTests
{
    private static string Bosluk(int adet) => new(' ', adet);

    // Değerler müşteri 2'nin örnek verisi (db2cre.jcl:471-480).
    [Fact]
    public void Musteri_alanlari_kopya_kitaptaki_sirayla_ve_uzunlukla_yazilir()
    {
        var commarea = new CustomerCommarea
        {
            RequestId = "01ACUS",
            CustomerNumber = 2,
            FirstName = "Scott",
            LastName = "Tracey",
            DateOfBirth = "1965-09-30",
            HouseName = "Tracey Island",
            HouseNumber = "1",
            Postcode = "TB14TV",
            NumberOfPolicies = 3,
            PhoneMobile = "07700 900123",
            PhoneHome = "001 911911",
            EmailAddress = "REFROOM@TBHOLDINGS.COM",
        };

        var metin = commarea.ToFixedWidth();

        var beklenen =
            "01ACUS" + "00" + "0000000002"                  //   0: başlık (18)
            + "Scott" + Bosluk(5)                           //  18: CA-FIRST-NAME     X(10)
            + "Tracey" + Bosluk(14)                         //  28: CA-LAST-NAME      X(20)
            + "1965-09-30"                                  //  48: CA-DOB            X(10)
            + "Tracey Island" + Bosluk(7)                   //  58: CA-HOUSE-NAME     X(20)
            + "1" + Bosluk(3)                               //  78: CA-HOUSE-NUM      X(4)
            + "TB14TV" + Bosluk(2)                          //  82: CA-POSTCODE       X(8)
            + "003"                                         //  90: CA-NUM-POLICIES   9(3)
            + "07700 900123" + Bosluk(8)                    //  93: CA-PHONE-MOBILE   X(20)
            + "001 911911" + Bosluk(10)                     // 113: CA-PHONE-HOME     X(20)
            + "REFROOM@TBHOLDINGS.COM" + Bosluk(78);        // 133: CA-EMAIL-ADDRESS  X(100)

        Assert.Equal(beklenen, metin[..233]);
        Assert.Equal(Bosluk(32267), metin[233..]);          // 233: CA-POLICY-DATA    X(32267)
    }

    [Fact]
    public void Musteri_guvenlik_alanlari_kopya_kitaptaki_sirayla_ve_uzunlukla_yazilir()
    {
        var commarea = new CustomerSecurityCommarea
        {
            RequestId = "02ACUS",
            CustomerNumber = 1000001,
            Password = "5f4dcc3b5aa765d61d8327deb882cf99",
            Count = "12",
            State = "N",
        };

        var metin = commarea.ToFixedWidth();

        var beklenen =
            "02ACUS" + "00" + "0001000001"                  //  0: başlık (18)
            + "5f4dcc3b5aa765d61d8327deb882cf99"            // 18: CA-CUSTSECR-PASS   X(32)
            + "12" + Bosluk(2)                              // 50: CA-CUSTSECR-COUNT  X(4)
            + "N";                                          // 54: CA-CUSTSECR-STATE  X(1)

        Assert.Equal(beklenen, metin[..55]);
        Assert.Equal(Bosluk(32445), metin[55..]);           // 55: CA-CUSTSECR-DATA   X(32445)
    }
}
