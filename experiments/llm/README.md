# LLM deneyi: aynı 18 operasyonu bir yapay zekâ modeline çevirt

- **Durum:** Tasarım (çalıştırma #41'de yapılacak)
- **Tasarım tarihi:** 2026-10-27
- **Sorumlu:** Oğuz
- **İlgili:** H07, #41 (deneyin çalıştırılması), #73 ([mimari belge](../../docs/architecture.md))

## 1. Soru ve neden önceden yazıyoruz

**Soru:** Bir LLM, COBOL kodunu ve mimari belgedeki arayüzleri görerek 18 operasyonu C#'a çevirirse, bizim
elle yazdığımız kodla aynı eşdeğerlik testlerinden kaç tanesini geçer?

Kurallar deney başlamadan yazılmazsa sonuçlar sonradan yorumlanıp eğilip bükülebilir. Bu yüzden bu belge
**ilk çalıştırmadan önce `main`'e birleştirilir.** İlk çalıştırmadan sonra yapılan her değişiklik belgenin
sonundaki "Değişiklik kaydı"na tarihi ve gerekçesiyle yazılır; sonuç görüldükten sonra ölçüt, test ya da talimat
sessizce değiştirilmez.

## 2. Hangi LLM, hangi sürüm, hangi tarih

| Alan | Değer |
|---|---|
| Model | Claude Opus 5.5 |
| Sürüm (model kimliği) | `claude-opus-5-5`, sürüm sabitlenmiş kimlikle çağrılır; "latest" benzeri bir takma ad kullanılmaz |
| Erişim | Anthropic API, araç kullanımı (dosya okuma, kod çalıştırma, internet) **kapalı**; model yalnızca verilen metni görür |
| Ayarlar | Sağlayıcının varsayılan örnekleme ayarları; hiçbiri elle değiştirilmez. Değişkenliği ayarla değil tekrarla ölçeriz (bölüm 5) |
| Tasarım tarihi | 2026-10-27 |
| Çalıştırma tarihi | Bu belge `main`'e birleştikten sonra, #41'in ilk çalıştırma günü. Tarih, model kimliği ve test setinin commit'i `experiments/llm/runs/` altındaki kayda çalıştırmadan **önce** yazılıp commit'lenir |

Tek model kullanılır; ikinci bir model eklenirse aynı kurallarla ayrı bir çalıştırma olarak, değişiklik
kaydına yazılarak eklenir.

## 3. LLM'e ne verilir, ne verilmez

### Verilir

| Girdi | Nereden | Neden |
|---|---|---|
| Operasyonun COBOL program zinciri | `base/src` (bölüm 4'teki tablo) ve ortak `lgcmarea.cpy`, `lgpolicy.cpy` | Çevrilecek davranışın kaynağı. Ekibin okuduğu kodun aynısı |
| API tasarımı | [`docs/api-design.md`](../../docs/api-design.md): adresler, JSON alanları, dönüş kodu → HTTP eşlemesi, bilinçli sapmalar | Testlerin dayandığı sözleşme. LLM'in kodu aynı testlerle çalışsın diye şart |
| Veritabanı şeması | [`docs/database-schema.md`](../../docs/database-schema.md) | Tablolar, kolonlar, tipler |
| Mimari belge | [`docs/architecture.md`](../../docs/architecture.md) bölüm 4 (katmanlar), bölüm 8 (eski → yeni eşleme) | Katman kuralları ve eski kavramların yeni karşılığı |
| İskelet projenin tarifi | Bölüm 5.1 | Çıktının nereye oturacağını bilsin |

### Verilmez

- **Bizim C# kodumuz** (`src/`), hiçbir parçası. Verilirse karşılaştırma anlamsız olur.
- **Testlerin kendisi** (`tests/GenApp.EquivalenceTests`) ve sonuçları. Model testi görürse testlere göre yazar;
  beklenen davranışı belgeden çıkarması gerekir. Test ekibi de testleri koddan bağımsız yazdı (ADR 0005 Karar 4).
- **Tahmin tabloları ve analizler** (`tests/predictions`, `tests/gnucobol`, `legacy-analysis`,
  `tests/results`). Bunlar ekibin COBOL'u okuyarak çıkardığı sonuçlardır; verilirse model işi bizden devralmış olur.
- **ADR'ler** (ekibin gerekçeleri ve test kararları), SRS, başka operasyonların COBOL kodu (zincirde olmayanlar).
- Önceki çalıştırmaların çıktısı ve test hata mesajları (bölüm 5, "geri bildirim yok").

**Bilinen ödünleşme:** `api-design.md` bilinçli sapmaları da anlatır (örn. güncellemede `lastChanged`
uyuşmazlığı → `409`). Elle yazan ekip bu belgeyi gördüğü için LLM de görür; aynı girdiyle karşılaştırırız.
Bu, LLM'in sonucunu iyileştirebilir ve bölüm 8'de geçerlik sorunu olarak anılır.

## 4. Operasyonlar ve COBOL zincirleri

Operasyon adları `tests/active-operations.txt` ve testlerdeki `[Trait("Operasyon", ...)]` ile aynıdır.
Poliçe programları (`LGAPOL01` vb.) dört türü birden yönetir; LLM'e tam program verilir ve hangi isteğin
çevrileceği talimatta söylenir.

| Operasyon | İstek kodu | Verilen COBOL dosyaları (`base/src`) |
|---|---|---|
| `musteri-ekle` | `01ACUS` | lgacus01, lgacdb01, lgacdb02, lgacvs01 |
| `musteri-sorgula` | `01ICUS` | lgicus01, lgicdb01 |
| `musteri-guncelle` | `01UCUS` | lgucus01, lgucdb01, lgucvs01 |
| `motor-ekle` / `konut-ekle` / `hayat-ekle` / `ticari-ekle` | `01AMOT` / `01AHOU` / `01AEND` / `01ACOM` | lgapol01, lgapdb01, lgapvs01 |
| `motor-sorgula` / `konut-sorgula` / `hayat-sorgula` | `01IMOT` / `01IHOU` / `01IEND` | lgipol01, lgipdb01 |
| `ticari-sorgula` (4 biçim: 17a–17d) | `01ICOM`, `02ICOM`, `03ICOM`, `05ICOM` | lgipol01, lgipdb01 |
| `motor-guncelle` / `konut-guncelle` / `hayat-guncelle` | `01UMOT` / `01UHOU` / `01UEND` | lgupol01, lgupdb01, lgupvs01 |
| `motor-sil` / `konut-sil` / `hayat-sil` / `ticari-sil` | `01DMOT` / `01DHOU` / `01DEND` / `01DCOM` | lgdpol01, lgdpdb01, lgdpvs01 |

Her satırda ayrıca `lgcmarea.cpy` ve `lgpolicy.cpy` verilir. `ticari-guncelle` yoktur (eski sistemde de yok).

## 5. Çalıştırma düzeni

### 5.1 İskelet proje

LLM'in çıktısı, **bizim kodumuzu içermeyen** boş bir iskelete konur: `GenApp.Api` adlı, `Program.cs`'inde
`AddControllers()`, `MapControllers()` ve `public partial class Program` bulunan bir ASP.NET Core (.NET 10) projesi;
Npgsql ve EF Core paketleri yüklü. Veritabanı, bizim migration'larımızla önceden kurulmuştur (LLM migration
yazmaz); bağlantı dizesi `ConnectionStrings__GenApp` ortam değişkenindendir. Testler bu iskelet üzerinden, bizim
kodumuzdaki gibi `WebApplicationFactory<Program>` ile çalışır.

### 5.2 Talimat verme kuralları

- **Her operasyon için ayrı, temiz bir oturum.** Önceki operasyonun konuşması taşınmaz.
- **Tek atış, geri bildirim yok.** Model her operasyon için bir cevap verir. Derleme hatası ya da test sonucu
  modele gösterilmez, ikinci bir deneme yapılmaz. Derlenmeyen çıktı olduğu gibi puanlanır (0 geçen test).
- **Elle düzeltme yok.** Çıktıdaki dosyalar bir harfi bile değiştirilmeden iskelete kopyalanır; yalnızca
  dosya yolları ve kod blokları ayıklanır.
- **Bağımsızlık kuralı (çıktı biçimi).** Tüm tipler `GenApp.Llm.<OperasyonAdi>` ad alanında olur (örn.
  `GenApp.Llm.MusteriEkle`); başka operasyonun dosyasına bağımlı olunmaz; adres tam yazılır
  (`[Route("api/customers")]` gibi). Böylece 18 operasyon çakışmadan tek projede birleşir.
- **3 bağımsız tekrar** (T1, T2, T3) her operasyon için. Üçü de raporlanır; en iyisi seçilmez.

### 5.3 Talimat şablonu

Her operasyon için **aynen** aşağıdaki metin kullanılır; yalnızca `{...}` yerleri doldurulur. Operasyon
başına başka hiçbir cümle eklenmez.

````text
Sen deneyimli bir C# geliştiricisisin. Aşağıda IBM'in COBOL/CICS örnek uygulaması GenApp'ten bir operasyonun
COBOL kodu var. Bu operasyonu, GenApp'in yeni ASP.NET Core (.NET 10) sürümü için C#'a çevirmeni istiyorum.

## Görev
Operasyon: {OPERASYON_ADI}
İstek kodu: {ISTEK_KODU}
HTTP adresi ve yöntemi: {YONTEM_VE_ADRES}

Yalnızca bu operasyonu yaz. Başka operasyonları yazma.

## Kurallar
1. Davranış COBOL kodundan çıkarılır: dönüş kodları, hangi koşulda hangisinin döndüğü, girdi kontrolleri,
   eşzamanlı güncelleme kontrolü. Dönüş kodları API tasarımındaki tabloya göre HTTP cevabına çevrilir.
2. İstek ve cevap biçimi, adres, durum kodları ve hata gövdesi (`returnCode` alanı dâhil) "API tasarımı"
   belgesine birebir uymalıdır. Belgeyle COBOL arasında çelişki varsa belgede "bilinçli sapma" olarak
   yazılan davranışı uygula.
3. Veritabanı PostgreSQL'dir; tablo ve kolonlar "Veritabanı şeması" belgesindedir. Bağlantı dizesi
   `ConnectionStrings__GenApp` yapılandırmasından okunur. Şemayı değiştirme, migration yazma.
4. VSAM ve CICS'e özgü şeyler (EXEC CICS, COMMAREA, SYNCPOINT, kuyruklar) yeni sistemde yoktur; bunların
   işlevi belgede anlatıldığı gibi karşılanır (örn. VSAM yazımı Db2 yazımında zaten vardır).
5. Çıktın bir ASP.NET Core controller'ı ve ihtiyaç duyduğun diğer sınıflardır. Hepsi
   `GenApp.Llm.{OPERASYON_AD_ALANI}` ad alanında olsun, başka operasyonun kodundan bağımsız çalışsın,
   controller'ın adresi tam yazılsın. Projede `Program` sınıfı hazırdır; sen yazma.
6. Yalnızca verilen belgelere ve COBOL koduna dayan. Bilmediğin bir davranışı uydurma; belirsizse en yakın
   COBOL satırına göre karar ver ve kodda tek satır yorumla belirt.

## Çıktı biçimi
Her dosya için önce yolunu bir başlık satırında (`### dosya: <yol>`), sonra içeriğini tek bir ```csharp
bloğunda ver. Dosya dışında açıklama yazma.

## Belgeler
### API tasarımı
{API_TASARIMI_METNI}

### Veritabanı şeması
{VERITABANI_SEMASI_METNI}

### Mimari belge (bölüm 4 ve 8)
{MIMARI_BELGE_BOLUMLERI}

## COBOL kodu
{COBOL_DOSYALARI}   <!-- her dosya "### base/src/<ad>" başlığıyla, bölüm 4'teki tabloya göre -->
````

Doldurma kuralları: `{API_TASARIMI_METNI}`, `{VERITABANI_SEMASI_METNI}` ve `{MIMARI_BELGE_BOLUMLERI}`
repodaki dosyaların, çalıştırma commit'indeki hâlinin kelimesi kelimesine kopyasıdır (yorumsuz, kısaltmasız).
`{COBOL_DOSYALARI}` bölüm 4'teki tabloya göredir. Talimatın tam hâli (doldurulmuş) her çalıştırma kaydına
eklenir.

## 6. Başarı ölçütü

**Ana ölçü: LLM çıktısının, aynı eşdeğerlik testlerinden kaçını geçtiği.**

> geçen test sayısı ÷ toplam test sayısı

- **Test seti:** `tests/GenApp.EquivalenceTests`, çalıştırma commit'inde **dondurulmuş** hâli. Toplam, o commit'te
  `dotnet test --list-tests` ile sayılır ve çalıştırma kaydına yazılır. Deney sırasında hiçbir test eklenmez,
  silinmez, değiştirilmez.
- **Geçti** = xUnit sonucu `Passed`. Derleme hatası, zaman aşımı, çöken API, atlanan test = **geçmedi**.
- **Operasyon başına** (`Operasyon` etiketine göre) ve **genel** olarak hesaplanır; her tekrar (T1, T2, T3) ayrı
  satırdır, yanında ortalama verilir.
- **Karşılaştırma tabanı:** bizim elle yazdığımız kodun **aynı commit'te, aynı test setiyle** geçtiği sayı
  (aynı biçimde operasyon başına ve genel). Rapor `LLM − elle` farkını verir.
- **Kapsam:** Yalnızca hem testi hem de elle yazılmış kodu çalıştırma commit'inde `main`'de olan operasyonlar
  hesaba girer. Dışarıda kalanlar ve nedeni kayıtta listelenir (bugün hayat ve ticari kodu ile ticari testleri
  `main`'de eksiktir). Dışarıda kalan operasyon için LLM yine çalıştırılır, ama ana ölçüye katılmaz.
- **Eşik yok, yorum yok:** "başarılı" ya da "başarısız" demek için önceden bir eşik koymuyoruz; ana ölçü sayı
  olarak raporlanır. Sonuç görüldükten sonra eşik uydurulmaz.

### İki ölçüm düzeni

| Düzen | Nasıl | Ne için |
|---|---|---|
| **A. İzole (ana ölçü)** | Elle yazılmış projede yalnızca incelenen operasyonun action'ı çıkarılır, yerine LLM'in dosyaları konur. Testlerin kurulum adımlarında kullandığı diğer operasyonlar bizim kodumuzdur | Bir operasyonun hatası diğerlerinin puanını düşürmesin (örn. `musteri-ekle` bozuksa her test kırılır) |
| **B. Bütünleşik (yardımcı)** | 18 operasyonun LLM çıktısı iskelet projede birleştirilir, tüm testler çalışır | LLM'in bütün sistemi uçtan uca kurup kuramadığını görmek |

Ana ölçü A'dır; B yan bilgi olarak raporlanır. Hangi action'ın nasıl çıkarıldığı çalıştırma kaydına yazılır.

### Yardımcı bilgiler (başarı ölçütü değildir)

İlk denemede derlenen operasyon sayısı, çıktı satır sayısı, süre ve token sayısı. Kod kalitesi, okunabilirlik ve
performans **ölçülmez**; bu deneyin sorusu eşdeğerliktir.

### Başarısız testlerin ele alınması

Geçmeyen her test için dosyaya şunlar yazılır: test adı (COBOL satırını gösterir, ADR 0005 Karar 3), kısa neden
(derleme, yanlış dönüş kodu, yanlış HTTP durumu, eksik alan, çöktü) ve bunun COBOL'un mu belgenin mi yanlış
okunmasından kaynaklandığı. Bir testin beklentisi hatalı göründüğünde ("test hatası şüphesi") test **düzeltilmez**;
sayı olduğu gibi kalır, şüphe ayrı bir listede raporlanır.

## 7. Çıktılar ve kayıt

`experiments/llm/runs/<tarih>/` altında: doldurulmuş talimatlar, modelin ham cevapları, iskelete konan dosyalar,
test çıktıları (`dotnet test` kaydı) ve sonuç tablosu (`results.md`). Ham cevaplar bir daha düzenlenmez.
Sonuç tablosunun biçimi:

| Operasyon | Toplam test | Elle yazılan | LLM T1 | LLM T2 | LLM T3 | LLM ortalama | LLM − elle |
|---|---|---|---|---|---|---|---|

## 8. Geçerlik sorunları ve açık sorular

- **Belge sızıntısı:** `api-design.md` bilinçli sapmaları anlatır; elle yazan ekip de görmüştü, bu yüzden
  LLM'e de verilir. Sonuç, "yalnızca COBOL'dan" çevirinin göstereceğinden iyi çıkabilir.
- **Eğitim verisi:** GenApp açık bir IBM örneğidir; model COBOL kodunu ya da çevirisini eğitiminde görmüş olabilir.
  Bunu ölçemeyiz, yalnızca not ederiz.
- **Tek model, 3 tekrar:** Sonuç bu modelin bu tarihteki hâli içindir; genelleme yapılmaz.
- **Test kapsamı:** Testler 47 test yöntemiyle (bugün) davranışın bir kısmını ölçer (ADR 0005 Karar 2). Geçmek
  eşdeğerlik demek değildir; geçmemek de COBOL'un yanlış çevrildiğini her zaman göstermez.
- **Açık:** Model değiştirilecekse ya da ikinci model eklenecekse karar ekipçe verilir ve aşağıya yazılır.

## Değişiklik kaydı

| Tarih | Değişiklik | Gerekçe |
|---|---|---|
| 2026-10-27 | İlk sürüm | H07 |
| 2026-10-27 | Çalıştırma 16 çağrıda tamamlandı. Ölçüm kapsamı 18 operasyondan, üç tekrarı da yapılmış beş operasyona daraltıldı: `musteri-ekle`, `musteri-sorgula`, `musteri-guncelle`, `motor-ekle`, `motor-sorgula`. `motor-guncelle` (yalnızca T1) ek bilgi olarak raporlanır, ölçüme sayılmaz; kalan 12 operasyon çalıştırılmaz. Sonuç bu beş operasyon içindir, 18 operasyona genellenmez. Bölüm 1'deki soru ve bölüm 4'teki tablo bu kapsamla okunur. | Ekip kararı. Karar, LLM çıktıları testlerle hiç çalıştırılmadan alındı; sonuç görülerek operasyon seçilmedi. |
