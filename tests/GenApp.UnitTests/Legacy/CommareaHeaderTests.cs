using GenApp.Domain.Legacy;

namespace GenApp.UnitTests.Legacy;

/// <summary>
/// COMMAREA'nın her istekte ortak olan 18 baytlık başlığı ve toplam uzunluğu (lgcmarea.cpy:10-13).
/// </summary>
public sealed class CommareaHeaderTests
{
    // İsteğe özgü alanı olmayan test sınıfı: yalnızca başlığı ve doldurmayı ölçmek için.
    private sealed class SadeceBaslik : Commarea
    {
        protected override void WriteRequestSpecific(CommareaWriter writer)
        {
        }

        protected override void ReadRequestSpecific(CommareaReader reader)
        {
        }
    }

    // CA-REQUEST-ID X(6) + CA-RETURN-CODE 9(2) + CA-CUSTOMER-NUM 9(10).
    [Fact]
    public void Baslik_istek_kodu_donus_kodu_ve_musteri_numarasini_tasir()
    {
        var commarea = new SadeceBaslik { RequestId = "01ICUS", ReturnCode = 1, CustomerNumber = 1000001 };

        var metin = commarea.ToFixedWidth();

        Assert.Equal("01ICUS010001000001", metin[..18]);
    }

    // Toplam 32.500 bayt; yazılmayan alan boşluktur (32500 = 6 + 2 + 10 + 32482).
    [Fact]
    public void Commarea_32500_karakterdir_ve_kalan_alan_bosluktur()
    {
        var commarea = new SadeceBaslik { RequestId = "01ICUS" };

        var metin = commarea.ToFixedWidth();

        Assert.Equal(32500, metin.Length);
        Assert.Equal(new string(' ', 32482), metin[18..]);
    }

    [Fact]
    public void Baslik_metinden_okunur()
    {
        var metin = "01ICUS010001000001" + new string(' ', 32482);

        var commarea = Commarea.Parse<SadeceBaslik>(metin);

        Assert.Equal("01ICUS", commarea.RequestId);
        Assert.Equal(1, commarea.ReturnCode);
        Assert.Equal(1000001, commarea.CustomerNumber);
    }

    // Eski sistemde kısa COMMAREA 98 ile reddedilir (EIBCALEN kontrolü); model de tam 32.500 karakter ister.
    // 10 karakter: LGAPOL01 denemesindeki kısa COMMAREA (ADR 0005 Karar 7).
    [Theory]
    [InlineData(10)]
    [InlineData(32499)]
    [InlineData(32501)]
    public void Uzunlugu_32500_olmayan_metin_reddedilir(int uzunluk)
    {
        var metin = "01ICUS000001000001".PadRight(uzunluk)[..uzunluk];

        Assert.Throws<ArgumentException>(() => Commarea.Parse<SadeceBaslik>(metin));
    }
}
