namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// xUnit koleksiyon adları. Her dilimin test sınıfları kendi koleksiyonundadır ve bir <see cref="EntegrasyonFixture"/>
/// paylaşır: koleksiyonlar paralel, bir koleksiyonun testleri sıralı çalışır. Testler yalnızca kendi kurdukları
/// numaralarla sorguladığı için dilimler arasında veri paylaşımı yoktur.
/// </summary>
public static class Koleksiyon
{
    public const string Altyapi = "Altyapi";
    public const string Musteri = "Musteri";
    public const string Motor = "Motor";
    public const string Konut = "Konut";
    public const string Hayat = "Hayat";
    public const string Ticari = "Ticari";
}

[CollectionDefinition(Koleksiyon.Altyapi)]
public sealed class AltyapiKoleksiyonu : ICollectionFixture<EntegrasyonFixture> { }

[CollectionDefinition(Koleksiyon.Musteri)]
public sealed class MusteriKoleksiyonu : ICollectionFixture<EntegrasyonFixture> { }

[CollectionDefinition(Koleksiyon.Motor)]
public sealed class MotorKoleksiyonu : ICollectionFixture<EntegrasyonFixture> { }

[CollectionDefinition(Koleksiyon.Konut)]
public sealed class KonutKoleksiyonu : ICollectionFixture<EntegrasyonFixture> { }

[CollectionDefinition(Koleksiyon.Hayat)]
public sealed class HayatKoleksiyonu : ICollectionFixture<EntegrasyonFixture> { }

[CollectionDefinition(Koleksiyon.Ticari)]
public sealed class TicariKoleksiyonu : ICollectionFixture<EntegrasyonFixture> { }
