# ADR 0001: Katmanlı mimari ve proje yapısı

- **Durum:** Önerildi
- **Tarih:** 2026-10-13
- **Yazan:** Efekan
- **İlgili:** #74, [SRS](../srs.md) (NFR-04, NFR-09), [ADR 0002](0002-rest-api.md) (REST API ve dönüş kodu eşlemesi),
  [ADR 0003](0003-data-and-transactions.md) (veri ve transaction), [ADR 0005](0005-test-architecture.md) (test mimarisi)

## Bağlam

Eski GenApp üç katmandan oluşur ([sistem özeti](../system-overview.md)):

1. **Sunum** (LGTESTC1, LGTESTP1–P4): 3270 ekranı, kullanıcının girdiği veriyi COMMAREA'ya yazar.
2. **İş** (LGACUS01, LGAPOL01 gibi 7 program): COMMAREA'yı kontrol eder, veri katmanını çağırır.
3. **Veri** (LGACDB01, LGACVS01 gibi 15 program): Db2'ye ve VSAM'a okuyup yazar. 

Katmanlar birbirini `EXEC CICS LINK` ile çağırır ve aralarında tek bir ortak veri bloğu (COMMAREA,
`base/src/lgcmarea.cpy`) gezer. Bu ayrım kodda bir kural olarak değil, programların adlandırılması ve
alışkanlıkla korunur; örneğin hangi programın veritabanına dokunabileceğini derleyici denetlemez.

Yeni sistemi 18 operasyon boyunca beş kişi yazacak. Kodun nereye yazılacağına ve hangi parçanın hangisini
kullanabileceğine baştan karar vermemiz gerekiyordu. Kararı etkileyen noktalar:

1. **NFR-09:** Kod Api, Application, Domain ve Infrastructure katmanlarına ayrılır; bağımlılıklar içe,
   Domain'e doğru akar.
2. **ADR 0003:** Transaction sınırı Application katmanındadır; controller ve veri erişim sınıfı transaction
   açmaz. Ama ADR 0003'teki örnek kod EF Core'u (`db.Database.BeginTransactionAsync`) doğrudan kullanır.
   Application'ın EF Core'u nasıl göreceği açık kalmıştı.
3. **ADR 0002:** Dışarıya HTTP ve JSON ile konuşulur; eski dönüş kodları (`00`, `01`, `02`, `90`...) cevapta
   `returnCode` olarak korunur.
4. **ADR 0005:** Eşdeğerlik testleri API'yi dışarıdan çağırır; iç yapıya bağlı değildir. Yapım ekibi kendi
   birim testlerini yazabilmelidir.
5. Çözüm iskeleti #9'da dört projeyle kuruldu ve şema #15'te Infrastructure'a yazıldı. Bu ADR o yapıyı
   kayda geçirir ve kurallarını koyar.

## Karar

### 1. Dört proje, her biri bir katman

| Proje | Ne içerir | Eski sistemdeki karşılığı |
|---|---|---|
| `GenApp.Domain` | Varlıklar (`Customer`, `Policy`, `MotorPolicy`...) ve yalnızca kendi verisine bakan kurallar. Hiçbir pakete ve projeye bağlı değildir. | Db2 tablolarının ve COMMAREA alanlarının anlamı |
| `GenApp.Application` | Operasyon servisleri (müşteri ekle, poliçe sorgula...), girdi kontrolleri, dönüş kodlarının üretilmesi, transaction sınırı. Veri erişimi için **arayüzler** tanımlar. | 7 iş programı (`LGxCUS01`, `LGxPOL01`) |
| `GenApp.Infrastructure` | EF Core, `GenAppDbContext`, tablo eşlemeleri, migration'lar ve Application'daki veri erişim arayüzlerinin uygulamaları. | 15 veri programı (`*DB01`, `*DB02`, `*VS01`) |
| `GenApp.Api` | HTTP uç noktaları, JSON istek/cevap tipleri, dönüş kodu → HTTP durum kodu eşlemesi (ADR 0002), web arayüzü, uygulamanın başlatılması. | 5 sunum programı (3270 ekranları) |

### 2. Bağımlılık yönü

```
GenApp.Api ──────────────► GenApp.Application ──► GenApp.Domain
    │                              ▲                    ▲
    └──► GenApp.Infrastructure ────┴────────────────────┘
```

| Proje | Referans verebileceği projeler |
|---|---|
| `GenApp.Domain` | hiçbiri |
| `GenApp.Application` | `Domain` |
| `GenApp.Infrastructure` | `Application`, `Domain` |
| `GenApp.Api` | `Application`, `Infrastructure` (yalnızca kayıt için, bkz. Karar 4) |

Okların hepsi içe, Domain'e doğrudur. **Infrastructure'dan Application'a giden ok bilinçlidir:** Application
"bana poliçe okuyup yazan bir şey lazım" diye bir arayüz tanımlar, Infrastructure bu arayüzü EF Core ile
uygular (**bağımlılığın tersine çevrilmesi**, Dependency Inversion). Böylece iş kodu PostgreSQL'i ve EF Core'u
hiç bilmez; veri katmanı ise iş kodunun istediği sözleşmeye uyar.

Bu kurallar `.csproj` dosyalarındaki `ProjectReference`'larla zorlanır: izin verilmeyen bir referans eklenmeden
kod derlenmez. Eski sistemde alışkanlıkla korunan ayrım, yeni sistemde derleyici tarafından korunur.

### 3. Veri erişimi: repository ve unit of work arayüzleri

- **Repository arayüzleri Application'dadır** (`ICustomerRepository`, `IPolicyRepository` gibi). Metotları
  operasyonların ihtiyacına göre adlandırılır (`GetMotorPolicyAsync`, `AddAsync`...), genel amaçlı bir
  `IRepository<T>` yazılmaz.
- **Uygulamaları Infrastructure'dadır** (`PolicyRepository` gibi) ve `GenAppDbContext`'i kullanır.
- **Transaction için `IUnitOfWork` arayüzü Application'dadır.** Transaction başlatma ve kaydetme metotlarını
  taşır; uygulaması Infrastructure'da EF Core'un `Database.BeginTransactionAsync` ve `SaveChangesAsync`
  çağrılarına yönlenir. ADR 0003'ün "transaction'ı Application açar" kuralı bu arayüz üzerinden uygulanır;
  repository'ler transaction açmaz ve `SaveChanges` çağırmaz.
- **EF Core'a ait hatalar Infrastructure'da çevrilir.** Örneğin `DbUpdateConcurrencyException`
  (ADR 0003 Karar 3) Infrastructure'da yakalanır ve Application'da tanımlı bir hata tipine dönüştürülür;
  servis bunu dönüş kodu `02`'ye çevirir. Application'a EF Core tipi sızmaz.

### 4. Dependency injection

**Dependency injection (DI)**, bir sınıfın ihtiyaç duyduğu parçaları kendisinin oluşturması yerine dışarıdan,
genellikle yapıcı metodunda (constructor) almasıdır. Örneğin poliçe servisi `new PolicyRepository(...)` yazmaz;
yapıcısında `IPolicyRepository` ister ve ASP.NET Core'un DI kabı ona uygun nesneyi verir. Servis yalnızca
arayüzü bilir, hangi sınıfın geldiğini bilmez.

- **Her katman kendi kaydını yapar.** Application'da `AddApplication()`, Infrastructure'da
  `AddInfrastructure(connectionString)` uzantı metotları vardır (`AddInfrastructure` #15'te yazıldı).
  Bir katmana yeni bir sınıf eklendiğinde kaydı da o katmanın metoduna eklenir.
- **Tek birleştirme noktası `GenApp.Api/Program.cs`'tir** (composition root). Api, Infrastructure'a yalnızca
  `AddInfrastructure` çağrısı için referans verir; controller'lar ve uç noktalar Infrastructure sınıflarını
  kullanmaz, yalnızca Application servislerini ister.
- **Ömürler:** `GenAppDbContext`, repository'ler, `IUnitOfWork` ve servisler `Scoped` kaydedilir: her HTTP
  isteği için bir tane oluşturulur ve istek bitince atılır. Böylece bir operasyonun bütün parçaları aynı
  `DbContext`'i ve aynı transaction'ı paylaşır (ADR 0003 Karar 2: bir istek = bir operasyon = bir
  transaction).

### 5. Katmanlar arasında veri ve dönüş kodu

- **Api, Domain varlıklarını doğrudan JSON olarak döndürmez.** İstek ve cevap tipleri Api'dedir; HTTP
  sözleşmesi (ADR 0002, [API tasarımı](../api-design.md)) varlıklar değişse de sabit kalır.
- **Dönüş kodunu Application üretir, HTTP'ye Api çevirir.** Servis sonucu eski dönüş kodunu (`00`, `01`,
  `02`, `90`...) taşır; çünkü bu kodlar iş kuralıdır ve eski iş programları tarafından üretilir. Api bu kodu
  ADR 0002'deki tabloya göre HTTP durum koduna ve Problem Details gövdesine çevirir. Böylece eşleme tek
  yerdedir ve servisler HTTP'yi bilmez.

## Gerekçe

- **Neden dört proje:** Klasörler değil projeler kullanıldığı için bağımlılık kuralı derleyici tarafından
  denetlenir (Karar 2). Proje sayısı, SRS'teki katmanlarla (NFR-09) ve eski sistemin sunum/iş/veri
  ayrımıyla bire bir eşleşir; ekibin hangi kodu nereye yazacağı tartışmaya açık kalmaz.
- **Neden repository + unit of work:** Application'ın EF Core'u görmeden transaction sınırını tutabilmesinin
  yolu budur (Bağlam 2). Ayrıca servisler sahte (fake) repository'lerle, veritabanı olmadan birim testlenebilir.
  #25 görevi de poliçe verisi için Infrastructure'da ayrı bir veri erişim sınıfı ister.
- **Neden operasyona özgü repository metotları:** Eski veri programları da operasyona özgüdür (LGIPDB01
  istek koduna göre her poliçe türü için ayrı sorgu paragrafı çalıştırır, `lgipdb01.cbl:279-305`). Genel `IRepository<T>` çoğu operasyonda servis kodunun içine
  sorgu mantığı taşır ve EF Core'un `IQueryable`'ını Application'a sızdırır.
- **Neden DI:** ASP.NET Core zaten DI üzerine kurulu; EF Core `DbContext`'i de bu yolla verilir. Ek bir
  kütüphane gerekmez. Kayıtların katman başına bir metotta toplanması `Program.cs`'i kısa tutar.
- **Neden `Scoped`:** `DbContext` thread-safe değildir ve istek başına kullanılmak üzere tasarlanmıştır.
  Repository'ler ve `IUnitOfWork` aynı `DbContext`'i paylaşmazsa bir transaction'a giremez.

## Değerlendirilen seçenekler

| Seçenek | Neden seçilmedi |
|---|---|
| Tek proje, katmanlar klasör olarak | Bağımlılık kuralını derleyici denetlemez; eski sistemdeki gibi alışkanlığa kalır. NFR-09'u kanıtlamak zorlaşır. |
| Application'ın `GenApp.Infrastructure`'a doğrudan referans vermesi | Ok yönü bozulur (NFR-09); iş kodu PostgreSQL'e bağlanır, veritabanısız test edilemez. |
| Application'da `IGenAppDbContext` arayüzü (Application EF Core paketine bağlanır) | Daha az kod ve ADR 0003 örneği aynen kullanılır; ama iş kodu EF Core'a bağımlı olur, sorgular servislerin içine dağılır ve #25'in istediği ayrı veri erişim sınıfı oluşmaz. |
| Genel `IRepository<T>` | Bkz. Gerekçe; operasyonlara özgü sorgular servise taşınır. |
| Mikroservisler (müşteri ve poliçe ayrı servis) | 18 operasyon ve tek veritabanı için gereğinden karmaşık; servisler arası transaction NFR-04'ü zorlaştırır. Eski sistem de tek CICS bölgesinde çalışır. |

## Sonuçlar

**Olumlu**
- Bütün ekip aynı kuralla yazar: yeni bir operasyon = Application'da bir servis metodu, gerekiyorsa bir
  repository metodu (arayüz Application'da, uygulama Infrastructure'da) ve Api'de bir uç nokta.
- İzin verilmeyen bağımlılık derlenmez.
- Transaction sınırı ve dönüş kodu üretimi tek katmanda (Application), HTTP eşlemesi tek katmanda (Api).
- Servisler veritabanı olmadan birim testlenebilir; eşdeğerlik testleri (ADR 0005) iç yapıdan bağımsız kalır.

**Olumsuz ve kabul ettiklerimiz**
- **Daha fazla dosya:** Her veri erişimi için bir arayüz ve bir uygulama, her uç nokta için ayrı istek ve
  cevap tipleri yazılır.
- **Eşlemeler elle yazılır:** Api tipi ↔ Application sonucu ↔ Domain varlığı arasındaki dönüşüm kodu
  tekrar eden bir iştir. Ek bir eşleme kütüphanesi (AutoMapper gibi) şimdilik kullanılmaz.
- **Api'nin Infrastructure'a referansı:** Yalnızca `AddInfrastructure` için gereklidir, ama derleyici
  Api'deki başka bir kodun Infrastructure sınıflarını kullanmasını engellemez. Bu, kod incelemesinde
  denetlenir.
- **Domain varlıkları şu an yalnızca veri taşır** (#15). Girdi kontrolleri ve dönüş kodu kuralları
  Application servislerinde başlar; bir kural yalnızca tek bir varlığın kendi verisine bakıyorsa zamanla
  Domain'e taşınabilir.
- **Mevcut kodda henüz uygulanmayanlar:** `AddApplication()`, repository arayüzleri ve `IUnitOfWork` henüz
  yazılmadı. İlk kullanan iş (#25 poliçe veri erişimi ve müşteri dilimi kodu) bunları ekler.
