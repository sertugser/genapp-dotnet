using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Npgsql;

namespace GenApp.IntegrationTests.Altyapi;

/// <summary>
/// Entegrasyon testlerinin paylaşılan ortamı: test sürecinin içinde ayağa kalkan API (<see cref="Istemci"/>) ve aynı
/// veritabanını ham SQL ile okuyan <see cref="Veritabani"/>. Dilim başına bir koleksiyon bu sınıfı paylaşır
/// (<see cref="Koleksiyon"/>); dilimler paralel, bir dilimin testleri sıralı çalışır.
/// </summary>
/// <remarks>
/// API ve doğrulama aynı bağlantı dizesini kullanır: <c>ConnectionStrings__GenApp</c> ortam değişkeni. API onu ASP.NET
/// Core'un varsayılan yapılandırmasından okur (ortam değişkenleri appsettings dosyalarından önceliklidir); burada
/// değişkenin tanımlı olduğu kontrol edilir ve doğrulama için aynı değer kullanılır. Kodda bağlantı dizesi ya da parola
/// yoktur. Şema önceden kurulmuş olmalıdır (<c>dotnet ef database update</c>, docs/database-schema.md "Yerelde kurulum").
/// Veritabanı sıfırlanmaz: her test kendi müşterisini ve poliçesini API üzerinden kurar ve yalnızca kendi
/// numaralarıyla sorgular.
/// </remarks>
public sealed class EntegrasyonFixture : IAsyncLifetime
{
    public const string BaglantiDizesiDegiskeni = "ConnectionStrings__GenApp";

    private readonly WebApplicationFactory<Program> _fabrika;
    private readonly NpgsqlDataSource _veriKaynagi;

    public EntegrasyonFixture()
    {
        var baglantiDizesi = Environment.GetEnvironmentVariable(BaglantiDizesiDegiskeni);
        if (string.IsNullOrWhiteSpace(baglantiDizesi))
        {
            throw new InvalidOperationException(
                $"{BaglantiDizesiDegiskeni} ortam değişkeni tanımlı değil. Entegrasyon testleri, şeması kurulmuş bir " +
                "PostgreSQL veritabanına bu değişkendeki bağlantı dizesiyle bağlanır (API ve veritabanı doğrulaması " +
                "aynı değeri kullanır). Örnek: Host=localhost;Port=5432;Database=genapp;Username=genapp;Password=... " +
                "Kurulum: tests/README.md.");
        }

        _fabrika = new ApiFabrikasi();
        _veriKaynagi = NpgsqlDataSource.Create(baglantiDizesi);
        Veritabani = new VeritabaniYardimcilari(_veriKaynagi);
    }

    /// <summary>API'yi HTTP ile çağıran istemci. <see cref="InitializeAsync"/>'ten sonra kullanılabilir.</summary>
    public HttpClient Istemci { get; private set; } = null!;

    public VeritabaniYardimcilari Veritabani { get; }

    public Task InitializeAsync()
    {
        // CreateClient API'yi ayağa kaldırır; bağlantı dizesi ya da başlangıç hatası burada görünür.
        Istemci = _fabrika.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Istemci?.Dispose();
        await _fabrika.DisposeAsync();
        await _veriKaynagi.DisposeAsync();
    }

    /// <summary>
    /// API'nin loglarından yalnızca uyarı ve hataları test çıktısına yazar. Uygulamanın Information düzeyindeki logları
    /// (EF Core'un ürettiği SQL dahil) test çıktısını kalabalıklaştırır ve testi yazana uygulamanın iç ayrıntısını
    /// gösterir. Sağlayıcıya özgü kural, uygulamanın yapılandırmasındaki kategori kurallarından önce uygulanır.
    /// </summary>
    private sealed class ApiFabrikasi : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureLogging(log =>
            {
                log.ClearProviders();
                log.AddConsole();
                log.AddFilter<ConsoleLoggerProvider>(null, LogLevel.Warning);
            });
    }
}
