using GenApp.Domain.Legacy;

namespace GenApp.UnitTests.Legacy;

/// <summary>
/// Sabit genişlikli metinden alan okuma. Sondaki boşluklar alanın genişliğine aittir ve kırpılır
/// (ADR 0002 Karar 6 ile aynı mantık); baştaki boşluklar veridir ve korunur.
/// </summary>
public sealed class CommareaReaderTests
{
    [Theory]
    [InlineData("Andrew    ", "Andrew")]
    [InlineData("   5    ", "   5")]
    [InlineData("          ", "")]
    public void Metin_alaninin_sonundaki_bosluklar_kirpilir(string alan, string beklenen)
    {
        var okuyucu = new CommareaReader(alan);

        Assert.Equal(beklenen, okuyucu.Text(alan.Length));
    }

    [Theory]
    [InlineData("0042", 42)]
    [InlineData("0000000000", 0)]
    [InlineData("1000000001", 1000000001)]
    public void Sayi_alani_sayiya_cevrilir(string alan, long beklenen)
    {
        var okuyucu = new CommareaReader(alan);

        Assert.Equal(beklenen, okuyucu.Number(alan.Length));
    }

    // Sayı alanına rakam dışı bir değer gelmesi bozuk veridir; sessizce 0 sayılmaz.
    [Theory]
    [InlineData("12A4")]
    [InlineData("    ")]
    [InlineData("-042")]
    public void Sayi_alaninda_rakam_disi_karakter_hata_verir(string alan)
    {
        var okuyucu = new CommareaReader(alan);

        Assert.Throws<FormatException>(() => okuyucu.Number(alan.Length));
    }

    [Fact]
    public void Alanlar_sirayla_okunur()
    {
        var okuyucu = new CommareaReader("AB 07C");

        Assert.Equal("AB", okuyucu.Text(3));
        Assert.Equal(7, okuyucu.Number(2));
        Assert.Equal("C", okuyucu.Text(1));
    }
}
