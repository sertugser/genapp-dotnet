# GenApp Modernizasyonu: Yazılım Gereksinim Belirtimi (SRS)

| | |
|---|---|
| Proje | GenApp'in COBOL/CICS'ten ASP.NET Core ve PostgreSQL'e taşınması |
| Danışman | Prof. Dr. Hakan Çağlar |
| İlgili belgeler | [Sistem özeti](system-overview.md), [Use case diyagramı](diagrams/use-case.png), [Veritabanı şeması](database-schema.md), [ADR'ler](adr/) |

---

## 1. Giriş

### 1.1 Amaç
Bu belge, GenApp'in yeni sürümünün **ne yapması gerektiğini** tanımlar. Nasıl yapılacağı mimari
belgede (SDD) ve ADR'lerde anlatılır. Belge; yapım ekibi, test ekibi ve danışman için ortak başvuru
kaynağıdır.

### 1.2 Kapsam
GenApp, IBM'in CICS'i tanıtmak için hazırladığı örnek bir genel sigorta uygulamasıdır. Müşteri
kayıtlarını ve dört türde poliçeyi (motor, konut, hayat sigortası, ticari) yönetir. Eski sürüm
COBOL ile yazılmıştır; z/OS üzerinde CICS altında çalışır, veriyi Db2 ve VSAM'de tutar ve 3270
terminal ekranlarıyla kullanılır.

Yeni sürüm aynı 18 işlemi şu teknolojilerle sunar:
- **Sunucu tarafı:** ASP.NET Core ile yazılmış bir REST API
- **Veri:** PostgreSQL veritabanı
- **Kullanıcı arayüzü:** tarayıcıdan kullanılan bir web arayüzü
- **Çalıştırma:** Docker ile tek komutla ayağa kalkan kurulum

Projenin temel ilkesi **davranış eşdeğerliğidir:** Yeni sistem, aynı girdi için eski sistemle aynı
sonucu vermelidir. Bu, eski COBOL kodundan türetilen testlerle doğrulanır.

### 1.3 Terimler

| Terim | Anlamı |
|---|---|
| CICS | IBM'in ana bilgisayarlarda (mainframe) çalışan işlem sunucusu. Eski GenApp bunun içinde çalışır. |
| COMMAREA | Eski sistemde programların birbirine bilgi aktardığı ortak veri alanı. |
| Operasyon | Kullanıcının yapabildiği tek bir iş; örneğin "motor poliçesi ekle". GenApp'te 18 operasyon vardır. |
| Poliçe | Bir müşteriye ait sigorta sözleşmesi. Türleri: motor, konut, hayat sigortası (endowment), ticari. |
| Dönüş kodu | Eski sistemde her işlemin sonucunu bildiren iki haneli kod. Yeni sistemdeki karşılığı API tasarımında (ADR 0002) tanımlanır. |
| Dikey dilim | Bir müşteri ya da poliçe türüne ait operasyonların tamamı; örneğin motor poliçesinin ekle, sorgula, güncelle ve sil operasyonları. |
| Eşdeğerlik testi | Yeni sistemin, eski sistemle aynı girdiye aynı sonucu verdiğini kontrol eden test (back-to-back testing). |
| SRS / SDD / ADR | Gereksinim belgesi / mimari tasarım belgesi / mimari karar kaydı. |

### 1.4 Belgenin düzeni
Bölüm 2 sistemi genel olarak tanıtır. Bölüm 3 fonksiyonel gereksinimleri, Bölüm 4 fonksiyonel
olmayan gereksinimleri, Bölüm 5 dış arayüzleri anlatır. Bölüm 6 kapsam dışını, Bölüm 7 alınan
kapsam kararlarını, Bölüm 8 izlenebilirliği verir.

---

## 2. Genel tanım

### 2.1 Sistemin yeri
Yeni sistem, eski GenApp'in yerine geçen bağımsız bir uygulamadır. Eski sistemle canlı bağlantısı
yoktur. Eski sistemdeki örnek veriler (`base/data/`) yeni veritabanına bir kez aktarılır.

### 2.2 Kullanıcılar
Tek kullanıcı türü vardır: **sigorta çalışanı.** Müşteri kaydı açar, sorgular ve günceller;
poliçe ekler, sorgular, günceller ve siler. Kullanıcıların teknik bilgi sahibi olması beklenmez.

### 2.3 Çalışma ortamı
- Sunucu: .NET 10 (LTS), ASP.NET Core
- Veritabanı: PostgreSQL 18
- Kurulum: Docker ve Docker Compose
- İstemci: güncel bir web tarayıcısı

### 2.4 Kısıtlar
- Fonksiyonel davranış eski GenApp koduna (`base/src/`) göre belirlenir. Eski kod değiştirilmez.
- İşlem kümesi eski sistemle aynıdır: Eski kodda olmayan işlem eklenmez, var olan işlem çıkarılmaz.
- Proje Aralık 2026 sonunda teslim edilir.

### 2.5 Varsayımlar
- Sistem bir eğitim projesi olarak tek bir kurumda, güvenli bir ağ içinde çalışır.
- Eski sistemde kullanıcı girişini CICS sağlıyordu; uygulamanın kendisinde giriş ekranı yoktu.
  Yeni sistemde de kullanıcı girişi bu projenin kapsamında değildir (bkz. 6).

---

## 3. Fonksiyonel gereksinimler

Her operasyon için yalnızca ne yaptığı, ne aldığı ve ne döndürdüğü yazılmıştır. Hangi koşulda
hangi sonucun döneceği bu belgede yer almaz; o kurallar eski koddan test ekibi tarafından
bağımsız olarak çıkarılır.

**Bütün operasyonlar için ortak gereksinimler:**
- **FR-00a:** Her işlem sonunda sistem, işlemin başarılı olup olmadığını bildirir. Başarısız
  olduğunda nedeni (örneğin kaydın bulunamaması ya da veritabanı hatası) ayırt edilebilir biçimde
  bildirilir. Bu bildirimlerin API'deki karşılıkları ADR 0002'de tanımlanır.
- **FR-00b:** Bir işlem birden fazla tabloya yazıyorsa ya hepsi kaydedilir ya da hiçbiri
  kaydedilmez.

### 3.1 Müşteri işlemleri

**FR-01 Müşteri ekle**
- *Ne yapar:* Yeni bir müşteri kaydı oluşturur ve müşteriye benzersiz bir müşteri numarası verir.
  Her yeni müşteri için bir güvenlik kaydı da (varsayılan parola) oluşturulur.
- *Girdi:* ad, soyad, doğum tarihi, ev adı, ev numarası, posta kodu, cep telefonu, ev telefonu,
  e-posta adresi.
- *Çıktı:* işlemin sonucu ve yeni müşteri numarası.

**FR-02 Müşteri sorgula**
- *Ne yapar:* Numarası verilen müşterinin bilgilerini getirir.
- *Girdi:* müşteri numarası.
- *Çıktı:* işlemin sonucu; bulunduysa ad, soyad, doğum tarihi, ev adı, ev numarası, posta kodu,
  cep telefonu, ev telefonu, e-posta adresi.

**FR-03 Müşteri güncelle**
- *Ne yapar:* Var olan bir müşterinin bilgilerini değiştirir.
- *Girdi:* müşteri numarası ve yeni değerler (FR-01'deki alanlar).
- *Çıktı:* işlemin sonucu.

### 3.2 Poliçe işlemleri (ortak)

Dört poliçe türü aynı ortak bilgileri taşır:
- başlangıç tarihi, bitiş tarihi
- son değişiklik zamanı
- aracı (broker) numarası ve aracı referansı
- ödeme tutarı

Her poliçe bir müşteriye bağlıdır; var olmayan bir müşteriye poliçe eklenemez.

**Eşzamanlı değişiklik:** Bir poliçe güncellenirken sistem, kaydın kullanıcı tarafından
okunduğu andan sonra başka biri tarafından değiştirilip değiştirilmediğini kontrol eder.
Değiştirilmişse güncellemeyi yapmaz ve bunu bildirir.

### 3.3 Motor poliçesi

Türe özel bilgiler: araç markası, modeli, değeri, plaka numarası, rengi, motor hacmi (cc),
üretim tarihi, prim, kaza sayısı.

| No | Operasyon | Girdi | Çıktı |
|---|---|---|---|
| FR-04 | Motor poliçesi ekle | müşteri numarası, ortak bilgiler, türe özel bilgiler | sonuç ve yeni poliçe numarası |
| FR-05 | Motor poliçesi sorgula | müşteri numarası, poliçe numarası | sonuç; bulunduysa ortak ve türe özel bilgiler |
| FR-06 | Motor poliçesi güncelle | müşteri numarası, poliçe numarası, son değişiklik zamanı, yeni değerler | sonuç |
| FR-07 | Motor poliçesi sil | müşteri numarası, poliçe numarası | sonuç |

### 3.4 Konut poliçesi

Türe özel bilgiler: mülk türü, yatak odası sayısı, mülk değeri, ev adı, ev numarası, posta kodu.

| No | Operasyon | Girdi | Çıktı |
|---|---|---|---|
| FR-08 | Konut poliçesi ekle | müşteri numarası, ortak bilgiler, türe özel bilgiler | sonuç ve yeni poliçe numarası |
| FR-09 | Konut poliçesi sorgula | müşteri numarası, poliçe numarası | sonuç; bulunduysa ortak ve türe özel bilgiler |
| FR-10 | Konut poliçesi güncelle | müşteri numarası, poliçe numarası, son değişiklik zamanı, yeni değerler | sonuç |
| FR-11 | Konut poliçesi sil | müşteri numarası, poliçe numarası | sonuç |

### 3.5 Hayat sigortası poliçesi (endowment)

Türe özel bilgiler: kâra katılımlı olup olmadığı, hisse senedine yatırım yapıp yapmadığı,
yönetilen fona bağlı olup olmadığı, fon adı, vade (yıl), teminat tutarı, sigortalının adı.

| No | Operasyon | Girdi | Çıktı |
|---|---|---|---|
| FR-12 | Hayat sigortası poliçesi ekle | müşteri numarası, ortak bilgiler, türe özel bilgiler | sonuç ve yeni poliçe numarası |
| FR-13 | Hayat sigortası poliçesi sorgula | müşteri numarası, poliçe numarası | sonuç; bulunduysa ortak ve türe özel bilgiler |
| FR-14 | Hayat sigortası poliçesi güncelle | müşteri numarası, poliçe numarası, son değişiklik zamanı, yeni değerler | sonuç |
| FR-15 | Hayat sigortası poliçesi sil | müşteri numarası, poliçe numarası | sonuç |

### 3.6 Ticari poliçe

Türe özel bilgiler: işyeri adresi, posta kodu, enlem, boylam, müşteri (işletme) adı, mülk türü; yangın,
hırsızlık, sel ve hava koşulu riskleri ile her birinin primi; başvuru durumu ve ret nedeni.

| No | Operasyon | Girdi | Çıktı |
|---|---|---|---|
| FR-16 | Ticari poliçe ekle | müşteri numarası, ortak bilgiler, türe özel bilgiler | sonuç ve yeni poliçe numarası |
| FR-17 | Ticari poliçe sorgula | aşağıdaki dört arama biçiminden biri | sonuç; bulunduysa ortak ve türe özel bilgiler |
| FR-18 | Ticari poliçe sil | müşteri numarası, poliçe numarası | sonuç |

FR-17'nin dört arama biçimi:
1. müşteri numarası ve poliçe numarası ile
2. yalnızca poliçe numarası ile
3. yalnızca müşteri numarası ile
4. yalnızca işyeri posta kodu ile

Eski sistemde ticari poliçe güncelleme işlemi yoktur; yeni sistemde de yoktur.

---

## 4. Fonksiyonel olmayan gereksinimler

Bu gereksinimler ölçülebilir hedeflerdir. Performans hedefleri yük testi görevinde (#84)
ölçülür ve sonuçlar `tests/results/load-test.md` dosyasına yazılır.

### 4.1 Performans
- **NFR-01:** 20 eşzamanlı kullanıcı altında sorgulama isteklerinin %95'i 500 ms'den kısa sürede
  cevaplanır.
- **NFR-02:** 20 eşzamanlı kullanıcı altında ekleme, güncelleme ve silme isteklerinin %95'i
  1 saniyeden kısa sürede cevaplanır.
- **NFR-03:** Yük testi boyunca hiçbir istek sunucu hatasıyla sonuçlanmaz.

### 4.2 Güvenilirlik ve veri tutarlılığı
- **NFR-04:** Birden fazla tabloya yazan her işlem tek bir veritabanı işlemi (transaction)
  içinde yapılır; yarıda kalan bir işlem veriyi tutarsız bırakmaz.
- **NFR-05:** Aynı poliçe üzerinde eşzamanlı güncellemelerde son yazan, öncekinin değişikliğini
  fark etmeden ezemez (bkz. 3.2).
- **NFR-06:** Veritabanı kısıtları eski şemadaki ilişkileri korur; örneğin müşterisi olmayan
  poliçe kaydedilemez.

### 4.3 Taşınabilirlik
- **NFR-07:** Sistemin tamamı (API, web arayüzü, veritabanı) `docker compose up` komutuyla tek
  adımda çalıştırılabilir.
- **NFR-08:** Sistem Windows, macOS ve Linux'ta, Docker kurulu olması dışında ek ayar
  gerektirmeden çalışır.

### 4.4 Bakım kolaylığı ve test edilebilirlik
- **NFR-09:** Kod katmanlı mimariye göre düzenlenir (Api, Application, Domain, Infrastructure);
  bağımlılıklar içe, Domain katmanına doğru akar (ADR 0001).
- **NFR-10:** Her Pull Request'te kod otomatik olarak derlenir ve testler çalıştırılır (CI).
  Testleri geçmeyen değişiklik ana dala giremez.
- **NFR-11:** 18 operasyonun her biri için eski koddan türetilmiş eşdeğerlik testleri bulunur.
- **NFR-12:** Önemli teknik kararlar ADR olarak kayıt altına alınır.

### 4.5 Kullanılabilirlik
- **NFR-13:** Web arayüzünde 18 operasyonun her biri bir ekrandan yapılabilir.
- **NFR-14:** Hata durumlarında kullanıcıya teknik kod yerine anlaşılır bir mesaj gösterilir.

### 4.6 İzlenebilirlik (loglama)
- **NFR-15:** Veritabanı hataları ve beklenmeyen hatalar uygulama loguna tarih, saat, işlem adı
  ve ilgili kayıt numarasıyla yazılır (bkz. Karar 1).

---

## 5. Dış arayüz gereksinimleri

- **Kullanıcı arayüzü:** Tarayıcıdan kullanılan web ekranları. Müşteri ve dört poliçe türü için
  ayrı ekranlar bulunur.
- **Yazılım arayüzü:** JSON kullanan bir REST API. Adresler, HTTP yöntemleri ve sonuçların API'deki
  karşılıkları API tasarımında ve ADR 0002'de tanımlanır. API'nin otomatik açıklama sayfası
  (OpenAPI/Swagger) bulunur.
- **Veri arayüzü:** PostgreSQL veritabanı. Şema `docs/database-schema.md` dosyasında anlatılır.

---

## 6. Kapsam dışı

| Konu | Neden |
|---|---|
| Müşteri silme | Eski kodda yok. |
| Ticari poliçe güncelleme | Eski kodda yok. |
| Hasar (claim) işlemleri | Eski kodda bu tabloyu kullanan bir işlem yok (bkz. Karar 2). |
| JCL toplu işleri | Etkileşimli 18 işlemin parçası değiller. |
| Kurulum ve izleme programları (LGSETUP, LGASTAT1, LGWEBST5) | Eski sistemin bakım ve istatistik araçları; kullanıcı işlemi değiller. |
| İsteğe bağlı CICS senaryoları (web servisleri, CICSPlex SM, iş yükü simülatörü, olay işleme) | Uygulamanın kendisi değil, CICS özelliklerini göstermek için eklenmişler. |
| CICS adlandırılmış sayaç (named counter) | Müşteri numarası veritabanı tarafından üretilir; bu, eski sistemin de kendi yedek yoludur. |
| 3270 ekranları | Yerlerine web arayüzü gelir. |
| Gerçek ana bilgisayarda çalıştırma | Proje modern bir ortama taşımayı amaçlar. |
| Kullanıcı girişi ve yetkilendirme | Eski uygulamada da yoktu; CICS sağlıyordu (bkz. 2.5). |

---

## 7. Kapsam kararları

### Karar 1: Hata kuyruğu (LGSTSQ) yerine uygulama logu
- **Durum:** Eski sistemde Db2'ye yazılamayan kayıtlar ve diğer hatalar, LGSTSQ programı tarafından
  `GENAERRS` adlı geçici bir kuyruğa yazılır.
- **Karar:** Yeni sistemde ayrı bir hata kuyruğu tutulmaz. Hatalar, ASP.NET Core'un standart
  loglama altyapısıyla uygulama loguna yazılır (NFR-15).
- **Gerekçe:** IBM'in kendi belgesi de bu kuyruğun iyi bir uygulama olmadığını, yalnızca Db2
  sorunlarını kolay görmek için kullanıldığını söyler (`base/Architecture.md`). Uygulama logu aynı
  bilgiyi sağlar ve modern izleme araçlarıyla okunabilir.
- **Sonuç:** Hata kuyruğuna yazılan mesajlar eşdeğerlik testlerinin konusu değildir.

### Karar 2: Hasar (claim) tablosu
- **Durum:** Eski Db2 şemasında bir hasar tablosu tanımlıdır, ama 18 operasyonun hiçbiri bu tabloyu
  kullanmaz.
- **Karar:** Tablo, şemanın eski sistemle bire bir aynı olması için veritabanında oluşturulur
  (#92). Ancak bu tabloyla ilgili bir işlem gereksinimi yoktur ve API'de hasar işlemi bulunmaz.
- **Gerekçe:** Tabloyu şemada tutmak veri modelinin eksiksiz taşındığını gösterir ve ileride
  hasar işlemleri eklenirse hazır olur. Kullanılmayan bir işlemi yazmak ise kapsam dışıdır.

---

## 8. İzlenebilirlik

| Gereksinim | Operasyon | Eski programlar (iş → veri) | Use case |
|---|---|---|---|
| FR-01 | Müşteri ekle | LGACUS01 → LGACDB01 → LGACDB02, LGACVS01 | Müşteri ekle |
| FR-02 | Müşteri sorgula | LGICUS01 → LGICDB01 | Müşteri sorgula |
| FR-03 | Müşteri güncelle | LGUCUS01 → LGUCDB01 → LGUCVS01 | Müşteri güncelle |
| FR-04, 08, 12, 16 | Poliçe ekle | LGAPOL01 → LGAPDB01 → LGAPVS01 | ... poliçesi ekle |
| FR-05, 09, 13, 17 | Poliçe sorgula | LGIPOL01 → LGIPDB01 | ... poliçesi sorgula |
| FR-06, 10, 14 | Poliçe güncelle | LGUPOL01 → LGUPDB01 → LGUPVS01 | ... poliçesi güncelle |
| FR-07, 11, 15, 18 | Poliçe sil | LGDPOL01 → LGDPDB01 → LGDPVS01 | ... poliçesi sil |

Poliçe türü, eski sistemde COMMAREA'daki istek kodundan anlaşılır (örneğin motor için `01AMOT`
ekle, `01IMOT` sorgula, `01UMOT` güncelle, `01DMOT` sil; konut `HOU`, hayat sigortası `END`,
ticari `COM`).
