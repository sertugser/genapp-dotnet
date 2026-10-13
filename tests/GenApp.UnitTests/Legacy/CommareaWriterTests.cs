using GenApp.Domain.Legacy;

namespace GenApp.UnitTests.Legacy;

/// <summary>
/// COBOL'un sabit genişlikli alana MOVE kuralları. Beklenen değerler elle yazılmıştır.
/// </summary>
public sealed class CommareaWriterTests
{
    // PIC X(n): sola dayalı, kısa değer sağdan boşlukla doldurulur, uzun değer sağdan kesilir.
    [Theory]
    [InlineData("Ali", 10, "Ali       ")]
    [InlineData("Christopher", 10, "Christophe")]
    [InlineData("Andrew", 6, "Andrew")]
    [InlineData(null, 5, "     ")]
    public void Metin_alani_sagdan_doldurulur_ya_da_kesilir(string? deger, int uzunluk, string beklenen)
    {
        var yazici = new CommareaWriter();

        yazici.Text(deger, uzunluk);

        Assert.Equal(beklenen, yazici.ToString());
    }

    // PIC 9(n): sağa dayalı, kısa değer soldan sıfırla doldurulur, uzun değerin soldaki haneleri atılır.
    // 1000000001 → 9(9): tahmin 02-13'teki durum (ADR 0005 Karar 7), müşteri 1'e dönüşür.
    // İşaretsiz alana negatif değer taşındığında işaret düşer.
    [Theory]
    [InlineData(5, 3, "005")]
    [InlineData(1234, 3, "234")]
    [InlineData(1000000001, 9, "000000001")]
    [InlineData(0, 2, "00")]
    [InlineData(-42, 4, "0042")]
    public void Sayi_alani_soldan_sifirla_doldurulur_ya_da_soldan_kesilir(long deger, int uzunluk, string beklenen)
    {
        var yazici = new CommareaWriter();

        yazici.Number(deger, uzunluk);

        Assert.Equal(beklenen, yazici.ToString());
    }

    [Fact]
    public void Alanlar_yazildiklari_sirayla_art_arda_eklenir()
    {
        var yazici = new CommareaWriter();

        yazici.Text("AB", 3);
        yazici.Number(7, 2);
        yazici.Text("C", 1);

        Assert.Equal("AB 07C", yazici.ToString());
    }
}
