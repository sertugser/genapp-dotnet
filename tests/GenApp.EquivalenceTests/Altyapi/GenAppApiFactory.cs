using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GenApp.EquivalenceTests.Altyapi;

/// <summary>
/// API'yi test sürecinin içinde ayağa kaldırır. Testler API'yi yalnızca bu sınıfın verdiği
/// <see cref="HttpClient"/> ile, HTTP üzerinden çağırır. API projesinden kullanılan tek tip
/// <c>Program</c>'dır; Program sınıfı API derlemesinden gelir.
/// </summary>
/// <remarks>
/// Veritabanı bağlantı dizesi <c>ConnectionStrings__GenApp</c> ortam değişkeninden gelir. ASP.NET Core'un
/// varsayılan yapılandırmasında ortam değişkenleri appsettings dosyalarından önceliklidir; API bu varsayılanı
/// kullandığı sürece değişkendeki bağlantı dizesi geçerli olur. Burada yalnızca tanımlı olduğu kontrol
/// edilir; kodda bağlantı dizesi ya da parola yoktur. Veritabanının şeması önceden kurulmuş olmalıdır
/// (<c>dotnet ef database update</c>, bkz. docs/database-schema.md). Veritabanı sıfırlanmaz: her test
/// kendi müşterisini POST ile kurar.
/// </remarks>
public sealed class GenAppApiFactory : WebApplicationFactory<Program>
{
    public const string BaglantiDizesiDegiskeni = "ConnectionStrings__GenApp";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BaglantiDizesiDegiskeni)))
        {
            throw new InvalidOperationException(
                $"{BaglantiDizesiDegiskeni} ortam değişkeni tanımlı değil. Eşdeğerlik testleri, şeması " +
                "kurulmuş bir PostgreSQL veritabanına bu değişkendeki bağlantı dizesiyle bağlanır.");
        }
    }
}
