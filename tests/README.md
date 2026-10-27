# tests

Planned test projects:

- `GenApp.EquivalenceTests` – tests derived from the COBOL logic that capture the legacy behaviour: return codes
  (00, 01, 02, 70, 80, 81, 82, 88, 89, 90, 98, 99), optimistic locking with LASTCHANGED, and input checks.
  Selected programs are also run in GnuCOBOL, with CICS and Db2 calls replaced by stubs, to check these tests.
- `GenApp.IntegrationTests` – API + PostgreSQL tests
- Mutation testing with Stryker.NET measures how many planted bugs the tests catch

## Aktif operasyonlar (CI)

`tests/active-operations.txt`, CI'da hangi operasyonların eşdeğerlik testlerinin çalışacağını belirler. Testler
koddan önce `main`'e girdiği için, kodu henüz yazılmamış bir operasyonun testi çalışırsa başarısız olur. Bu yüzden CI
yalnızca bu dosyada adı geçen operasyonların testlerini çalıştırır.

- Her satıra bir operasyon adı yazılır.
- Dosya boşsa eşdeğerlik testleri çalışmaz.
- Bir operasyonun kodu hazır olunca adı bu dosyaya eklenir. Dosyadaki her adın `Operasyon` etiketli en az bir testi
  olmalı; aksi halde CI başarısız olur.

Biçim kuralları:

- Boş satırlar ve `#` ile başlayan satırlar yok sayılır.
- Adlar küçük harf ve tireyle yazılır (örn. `musteri-sorgula`). Bu kurala uymayan bir satır CI'ı başarısız yapar.

Örnek dosya:

```
# Müşteri
musteri-sorgula
musteri-ekle
```

Geçerli 18 ad:

```
musteri-sorgula
musteri-ekle
musteri-guncelle
motor-ekle
motor-sorgula
motor-guncelle
motor-sil
konut-ekle
konut-sorgula
konut-guncelle
konut-sil
hayat-ekle
hayat-sorgula
hayat-guncelle
hayat-sil
ticari-ekle
ticari-sorgula
ticari-sil
```

Eşdeğerlik testleri xUnit v2 ile yazılır. CI testleri `Operasyon` etiketine göre seçer, bu yüzden her test
operasyonunun adını bu etiketle taşır:

```csharp
[Fact]
[Trait("Operasyon", "musteri-sorgula")]
public void Musteri_sorgula_var_olan_musteri_00_doner()
{
    // ...
}
```

## Entegrasyon testleri

`GenApp.IntegrationTests`, API'nin PostgreSQL ile birlikte baştan sona çalıştığını gösterir (HTTP → API → uygulama →
PostgreSQL). Testler API'yi `WebApplicationFactory<Program>` ile test sürecinin içinde ayağa kaldırır,
`docs/api-design.md`'deki adreslere istek gönderir ve cevabı doğrular. Ardından sonucun veritabanında oluştuğunu ham
SQL ile kontrol eder (Npgsql; uygulamanın DbContext'i, repository'leri ve entity'leri kullanılmaz). Beklenen değerler
koddan değil belgelerden gelir: `docs/api-design.md`, `docs/srs.md`, `docs/database-schema.md`, `docs/adr/`.

Beş dilimin (müşteri, motor, konut, hayat, ticari) 18 operasyonunun her biri en az bir testte yer alır.
`Altyapi/SmokeTests.cs` ayrıca API'nin ve doğrulama yolunun veritabanına bağlandığını, şemadaki tablo ve sütunların
belgeyle uyduğunu kontrol eder.

### Yerelde çalıştırma

Testler şeması kurulmuş bir PostgreSQL ister. Bağlantı dizesi `ConnectionStrings__GenApp` ortam değişkeninden okunur;
API ve veritabanı doğrulaması aynı değeri kullanır. Değişken tanımlı değilse testler anlaşılır bir mesajla durur.
Değişkeni her komutta aynı satırda verin, kabuk oturumuna güvenmeyin.

```bash
# 1. Ayrı bir PostgreSQL 18 container'ı (CI ile aynı imaj ve değerler). Örnekte port 5434; bilgisayarınızda boş
#    olan bir port seçin (5432, docker compose ya da yerel bir PostgreSQL tarafından kullanılıyor olabilir).
docker run -d --name genapp-it-pg \
  -e POSTGRES_DB=genapp -e POSTGRES_USER=genapp -e POSTGRES_PASSWORD=genapp_dev \
  -p 127.0.0.1:5434:5432 \
  --health-cmd "pg_isready -U genapp -d genapp" --health-interval 2s --health-retries 15 \
  postgres:18
until [ "$(docker inspect -f '{{.State.Health.Status}}' genapp-it-pg)" = healthy ]; do sleep 1; done

# 2. Şemayı kur (docs/database-schema.md, "Yerelde kurulum")
dotnet tool restore
ConnectionStrings__GenApp="Host=localhost;Port=5434;Database=genapp;Username=genapp;Password=genapp_dev" \
  dotnet ef database update --project src/GenApp.Infrastructure --startup-project src/GenApp.Api

# 3. Testleri çalıştır (CI'daki komutla aynı)
ConnectionStrings__GenApp="Host=localhost;Port=5434;Database=genapp;Username=genapp;Password=genapp_dev" \
  dotnet test tests/GenApp.IntegrationTests/GenApp.IntegrationTests.csproj -- RunConfiguration.TreatNoTestsAsError=true

# Temiz bir veritabanıyla yeniden başlamak için: docker rm -f genapp-it-pg, ardından 1. adım
```

- Tek bir dilim için ad alanıyla süzün: `--filter "FullyQualifiedName~GenApp.IntegrationTests.Motor."`. `~Motor` tek
  başına adında "motor" geçen bir konut testini de yakalar.
- Şemanın belgeyle karşılaştırması (bulunamayan sütunlar, gerçek şemadaki tablo ve sütunlar) smoke testinin
  çıktısındadır: `--filter "FullyQualifiedName~Smoke" --logger "console;verbosity=detailed"`.

### CI'da

`.github/workflows/ci.yml`'deki "Entegrasyon testlerini çalıştır (API + PostgreSQL)" adımı, migration ve eşdeğerlik
testlerinden sonra projenin bütün testlerini çalıştırır. İş düzeyindeki `ConnectionStrings__GenApp` değişkenini ve
`postgres:18` servisini kullanır; eşdeğerlik testleriyle aynı `genapp` veritabanıdır. Hiç test bulunamazsa
`TreatNoTestsAsError=true` adımı başarısız yapar.

### Etiketler

Her test operasyonunu eşdeğerlik testleriyle aynı `[Trait("Operasyon", "<ad>")]` etiketiyle taşır (geçerli 18 ad
yukarıda). Entegrasyon testleri `tests/active-operations.txt`'e **bağlı değildir**: 18 operasyonun hepsinin kodu
olduğu için CI projeyi filtresiz çalıştırır. Smoke testleri operasyon testi olmadığı için etiket taşımaz. Her dilimin
testleri kendi xUnit koleksiyonundadır (`Altyapi/Koleksiyon.cs`): dilimler paralel, bir dilimin testleri sıralı
çalışır.

### Veri ve loglar

- Veritabanı testler arasında sıfırlanmaz ve örnek veri içerebilir. Her test kendi müşterisini ve poliçesini API
  üzerinden kurar. "Tam N satır" beklentileri yalnızca testin kendi müşteri numarası, poliçe numarası ya da teste özgü
  posta koduyla kurulur; bu yüzden testler tekrar çalıştırılabilir.
- Atanmayan numara olarak `999999999` kullanılır. İstekteki `customerNumber`'ın yok sayıldığını sınayan test
  `999999998` kullanır.
- Test çıktısında API logları yalnızca uyarı ve hata düzeyindedir. Olmayan müşteriye poliçe ekleyen testlerde
  (`70`), ayrıntılı çıktıda yabancı anahtar ihlali (`fk_policy_customer_customer_number`) ve stack trace görünür. Bu
  beklenen davranıştır: API ihlali yakalar, `404`/`"70"` döndürür ve hatayı loglar (SRS NFR-15).
