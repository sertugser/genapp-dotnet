# Eski kodu çalıştırmak ve LLM ile karşılaştırmak: GnuCOBOL düzeneği ve LLM deneyi

**Yazan:** Oğuz Aladağ

Bu bölüm projenin iki işini anlatır. Birincisi, elimizde CICS ve Db2 olmadığı halde GenApp'in COBOL programlarını
**çalıştırmayı** nasıl başardığımız ve bununla test ekibinin tahminlerinin doğruluğunu nasıl ölçtüğümüz (GnuCOBOL
düzeneği). İkincisi, aynı operasyonları bir yapay zekâ modeline (LLM) tek seferde C#'a çevirtip bizim kodumuzla aynı
testlerle karşılaştırdığımız deney. Bölümün tek başına okunabilmesi için önce kısa bir zemin veriyorum.

Bölümdeki her sayı ve iddia repodaki bir dosyaya dayanır; dosya yolları bağlantı olarak verilmiştir. Bir sonucun
dayandığı dosyada yoksa bölümde yer almaz. Bölümde belirtilen toplamlar, kayıtlardaki sayıların toplanmasıdır ve
hesap gösterilmiştir.

## 1. Zemin

GenApp, IBM'in CICS'i tanıtmak için hazırladığı örnek bir sigorta uygulamasıdır; COBOL ile yazılmıştır, z/OS üzerinde
**CICS** (işlem sunucusu) altında çalışır ve veriyi **Db2** (veritabanı) ile **VSAM** (dosya) içinde tutar
([sistem özeti](../system-overview.md)). Programlar birbirine bilgi **COMMAREA** adlı ortak bir veri alanı ile verir
([SRS](../srs.md), 1.3). Projenin amacı bu uygulamanın 18 operasyonunu ASP.NET Core ve PostgreSQL ile yeniden yazmak
ve yeni sistemin eskiyle **aynı davranışı** gösterdiğini testlerle kanıtlamaktır (davranış eşdeğerliği,
[SRS](../srs.md) 1.2).

Bunun için eski sistemin ne yaptığını bilmemiz gerekir. İki kaynağımız vardı ve ikisi de eksiktir:

- **Kodu okumak.** Test ekibi COBOL'u okuyup her operasyon için "şu girdide program şu dönüş kodunu verir" diye
  **tahmin** yazdı ([`tests/predictions/`](../../tests/predictions/), şablon:
  [`TEMPLATE.md`](../../tests/predictions/TEMPLATE.md)). Okumak, tahminin doğru olduğunu göstermez.
- **Kodu çalıştırmak.** Gerçek z/OS, CICS ve Db2 erişimimiz yok ([ADR 0004](../adr/0004-legacy-test-harness.md),
  "Değerlendirilen alternatifler"). Bu yüzden COBOL'u başka yerde çalıştırmamız gerekti.

Bu bölümün birinci yarısı ikinci kaynağı, ikinci yarısı ise LLM'in yazdığı kodun nasıl ölçüldüğünü anlatır.

## 2. Sahte CICS yöntemi

### 2.1 Sorun

GenApp programları normal bir COBOL derleyicisinde derlenmez. GnuCOBOL 3.2.0 ile `lgapol01.cbl` üzerindeki ilk
denemede 12 hata çıktı ve hepsinin nedeni CICS'e bağımlılıktı ([ADR 0004](../adr/0004-legacy-test-harness.md),
"Bağlam"; [`tests/gnucobol/README.md`](../../tests/gnucobol/README.md), "First compile check"):

- **EIB alanları** (`EIBCALEN`, `EIBTRNID`, `EIBTRMID`, `EIBTASKN`): CICS her programa kendiliğinden verir, kaynakta
  tanımlı değildir.
- **`EXEC CICS` komutları:** COBOL değildir; CICS'in çevirici aracı bunları derlemeden önce gerçek çağrılara çevirir.
  GnuCOBOL'da çevirici yoktur.
- **`EXEC SQL` komutları:** Veri katmanı programlarında vardır ve Db2 ister. Kurulumumuzda Db2 ön derleyicisi yoktur
  ve GnuCOBOL 3.2.0 `EXEC SQL` kabul etmez.
- Bir iş katmanı programı tek başına yaşamaz: LGAPOL01 veri katmanındaki LGAPDB01'i ve hata kuyruğuna yazan
  LGSTSQ'yu çağırır.

### 2.2 Karar: bağımlılık izolasyonu

Denediğimiz programı kendi başına çalıştırıp dışındaki her şeyi **sahte parçalarla** değiştirdik. Amaç, sonucun
yalnızca o programın koduna bağlı olmasıdır ([ADR 0004](../adr/0004-legacy-test-harness.md), "Karar"):

1. **EIB alanları** programın kopyasında `WORKING-STORAGE` içinde, CICS'in verdiği türlerle tanımlanır
   (`EXTERNAL`, böylece sürücü onları doldurabilir). `EIBCALEN` `COMP-5` tanımlanır; düz `COMP` ile GnuCOBOL
   `32500` değerini `2500`'e kırptığı için (denendi), tam boy bir COMMAREA kısa görünürdü.
2. **`EXEC CICS` komutları** GnuCOBOL karşılıklarıyla değişir: `RETURN` → `GOBACK`; `LINK` → sahte alt programa
   `CALL`; `ASKTIME`, `FORMATTIME`, `ABEND` → ekrana yazan `DISPLAY` (`ABEND`'den sonra `GOBACK`, çünkü gerçek ABEND
   geri dönmez). COMMAREA `PROCEDURE DIVISION USING` ile gelir.
3. **Bağlı programların yerine stub (sahte parça) yazılır.** Stub aynı `PROGRAM-ID` ve aynı COMMAREA yapısıyla
   çağrılır, ne aldığını ekrana yazar. **Stub sonuç uydurmaz:** dönüş kodu atamaz, alan doldurmaz. Ekranda görünen
   sonuç denenen programın kendi kodundan gelmelidir.
4. **Bir sürücü program CICS'in rolünü oynar.** EIB değerlerini verir, COMMAREA'yı kurar, programı çağırır, dönüş
   kodunu yazar. Her durum **ayrı bir süreçte** çalışır, çünkü CICS her görev için `WORKING-STORAGE`'ı yeniden
   kurar, GnuCOBOL ise aynı süreçte çağrılar arasında değerleri korur.
5. **`EXEC SQL` komutları deyim deyim değişir.** `INCLUDE SQLCA` → `COPY` (Db2'ninkiyle aynı alan adları olan en
   küçük bir kopya, `stubs/sqlca.cpy`); `EXEC SQL <deyim>` → ana bilgisayar değişkenlerini gösterip `DB2STUB`'a
   `CALL`. Db2 stub'ı yalnızca `SQLCODE`'u doldurur ve değeri **test senaryosu** seçer. Sonuç bu yüzden "Db2 şu
   `SQLCODE`'u döndürürse program şunu yapar" biçiminde, **koşulludur**.

Adım adım yöntem [`tests/gnucobol/README.md`](../../tests/gnucobol/README.md)'dedir; her program için aynı adımlar
izlenir. Değiştirilen her kopyanın, değişmemiş bir kopyayla `diff`'i her çalıştırma kaydına eklenir.
Stub'lar [`tests/gnucobol/stubs/`](../../tests/gnucobol/stubs/) altındadır (Db2'nin tablolarını taklit eden
`db2cust.cbl`, `db2motor.cbl`; sayaç ve VSAM için `cicscnt.cbl`, `cicswrf.cbl`; hata kuyruğu için `lgstsq.cbl`
ve diğerleri).

**Alternatifler** ([ADR 0004](../adr/0004-legacy-test-harness.md)): yalnızca kodu okumak (tahmini doğrulayamayız),
gerçek CICS ve Db2 kurmak (erişimimiz yok), yalnızca C# tarafını yazmak (karşılaştıracağımız referans davranış
kalmaz). ADR'nin durumu "Önerildi"dir.

### 2.3 Ne ölçebiliyoruz, ne ölçemiyoruz

**Ölçüyoruz** ([ADR 0004](../adr/0004-legacy-test-harness.md), "Neyi ölçebiliyoruz"): COMMAREA arayüz kurallarını
(H02 denemesinde LGAPOL01, 10 baytlık COMMAREA ile `98`, 32500 baytlıkla `00` döndürdü), programın karar yollarını
ve bağlı programa ne verdiğini. Bunlar programın kendi kodunun davranışıdır.

**Ölçemiyoruz** (aynı yer, "Neyi ölçemiyoruz"):

- **Db2, VSAM ve kuyrukların gerçek davranışı.** Stub bizim varsayımımızdır; seçtiğimiz `SQLCODE` de öyle.
  Satırın gerçekten oluşması, birincil anahtar ihlali, geri alma ve ABEND sonrası görevin sonlanması ölçülmez.
- **CICS çalışma zamanının davranışı** (`LINK` ile COMMAREA aktarımı, işlem ve bölge sınırları). Tarih ve saat
  sahtedir.
- **Çalıştırılan kod orijinalin birebir kendisi değildir:** kopyada EIB tanımları, `USING`, `CALL` ve `DISPLAY` var.
  Derleyiciye bağlı farklar olabilir; z/OS derleyicisinde aynı davranışı doğrulamadık.

Bunu bir **ölçüm kuralıyla** dengeledik: bir ölçüm çalıştırması, ilgili tahmin dosyası `main`'e girmeden yapılmaz ve
sonuç tahminlerden önce paylaşılmaz; aksi hâlde tahmin sonucun kopyasına dönerdi ([ADR 0004](../adr/0004-legacy-test-harness.md),
"Kullanım kuralları"). `lgacus01.md`, `lgipol01.md` ve `lgipdb01.md` kayıtlarında değerlendirme kuralları ve formül
**çalıştırmadan önce** commit'lendi; kayıtların başındaki not bunu belirtir ve `git log -- tests/gnucobol/runs/<kayıt>.md`
gösterir.

## 3. Çalıştırılan programlar

Beş çalıştırma kaydı vardır ([`tests/gnucobol/runs/`](../../tests/gnucobol/runs/)). Üçü **ölçüm** çalıştırmasıdır
(tahmin doğruluğuna sayılır), biri H02'deki yöntem denemesi, biri de yöntem denemesi ve karşılaştırmadır.

| Kayıt | Tarih | Çalıştırılan zincir | Karşılaştırılan tahmin | Statü |
|---|---|---|---|---|
| [`lgapol01-deneme.md`](../../tests/gnucobol/runs/lgapol01-deneme.md) | 2026-10-08 | LGAPOL01 (9 `EXEC CICS` komutu çevrildi) | yok | Deneme, ölçüm değil. Tahminler yazılmadan önce yapıldığı için sonucu sayıya girmez |
| [`lgicus01.md`](../../tests/gnucobol/runs/lgicus01.md) | 2026-10-10 | LGICUS01 → LGICDB01 | `02-customer-inquire.md` (17 satır) | **Ölçüm** (113 kontrol) |
| [`lgacus01.md`](../../tests/gnucobol/runs/lgacus01.md) | 2026-10-13 | LGACUS01 → LGACDB01 → LGACVS01, LGACDB02 | `01-customer-add.md` (25 satır) | **Ölçüm** (30 durum, 274 kontrol) |
| [`lgipol01.md`](../../tests/gnucobol/runs/lgipol01.md) | 2026-10-17 | LGIPOL01 → LGIPDB01 | `05-motor-inquire.md` (21 satır) | **Ölçüm** (23 durum, 299 kontrol) |
| [`lgipdb01.md`](../../tests/gnucobol/runs/lgipdb01.md) | 2026-10-17 | LGIPDB01 (28 `EXEC` deyimi, hepsi sahte `SQLCODE` parçasıyla) | `05-motor-inquire.md` (yalnızca `SQLCODE`'a bağlı 05-01, 05-03, 05-06) | Yöntem denemesi ve karşılaştırma; doğruluk tablosuna yeni satır **eklemez** (23 durum, 119 kontrol) |

`EXEC SQL`'in sahte `SQLCODE` ile değiştirilmesi LGIPDB01 için çalıştı: bütün `EXEC SQL` blokları (5 `SELECT`,
`OPEN`, `FETCH`, `CLOSE`) tek bir parçayla değişti ve program derlenip çalıştı. `SQLCODE` taramasında 0, 100 ve
dokuz farklı hata ya da uyarı kodu denendi; 11 durumun 11'inde program tahminin kuralına uygun dönüş kodunu verdi:
`0` için `00`, `100` için `01`, diğer dokuz kodun (`-904`, `-305`, `-803`, `-811`, `-911`, `-913`, `-922`, `-30081`,
`+304`) hepsi için `90` ([`lgipdb01.md`](../../tests/gnucobol/runs/lgipdb01.md), "Sonuç"). Bu sayılar, aynı satırlar
LGIPOL01 zincirinde zaten ölçüldüğü için tahmin doğruluğuna **eklenmez**.

Müşteri güncelle (LGUCUS01), motor güncelle ve sil, konut, hayat ve ticari programlarının bu düzenekte çalıştırma
kaydı **yoktur**; yani bu tahminler ölçülmedi ([`prediction-accuracy.md`](../../tests/results/prediction-accuracy.md),
"Hesaba girmeyenler").

## 4. Tahmin doğruluğu

### 4.1 Nasıl hesapladık

Her tahmin satırı, tahminin **ölçülebilir iddialarına** ayrıldı (dönüş kodu, veri, yan etki) ve çıktıyla bayt bayt
karşılaştırıldı. Bir satırın kararı: puanlanan iddialardan biri tutmadıysa **tutmadı**, hepsi tuttuysa **tuttu**,
puanlanan iddia yoksa **belirsiz** ([`prediction-accuracy.md`](../../tests/results/prediction-accuracy.md),
"Nasıl hesaplandı").

- **Ana ölçü** = tuttu ÷ puanlanan (tuttu + tutmadı). Tahminin kendisinin `belirsiz` dediği hücreler puanlanmaz.
- **Yardımcı ölçü** = tuttu ÷ ölçülen dosyalardaki bütün satırlar. `belirsiz` satırlar paydadadır; kötümser alt sınırdır.
- Düzeneğin gösteremediği iddialar (kuyruğa yazılan kayıt, gerçek Db2/VSAM/CICS davranışı) puanlanmaz,
  "ölçülemedi" diye ayrı yazılır.

### 4.2 Sonuç

| Tahmin dosyası | Satır | tuttu | tutmadı | belirsiz | Ana ölçü | Yardımcı ölçü | Kaynak |
|---|---|---|---|---|---|---|---|
| `02-customer-inquire.md` | 17 | 14 | 0 | 3 | 14/14 = %100 | 14/17 = %82,4 | `prediction-accuracy.md`, `runs/lgicus01.md` |
| `01-customer-add.md` | 25 | 21 | 0 | 4 | 21/21 = %100 | 21/25 = %84,0 | `prediction-accuracy.md`, `runs/lgacus01.md` |
| `05-motor-inquire.md` | 21 | 18 | 0 | 3 | 18/18 = %100 | 18/21 = %85,7 | `runs/lgipol01.md`, "Özet" |
| **Toplam** | **63** | **53** | **0** | **10** | **53/53 = %100** | **53/63 = %84,1** | toplam: 14+21+18, 3+4+3, 17+25+21 |

İlk iki satır [`prediction-accuracy.md`](../../tests/results/prediction-accuracy.md)'deki ilk ölçümdür (toplamı
35/35, 35/42). Üçüncü satır sonradan eklendi ve o dosyada henüz yoktur; sayılar çalıştırma kaydındadır. Son satırın
toplamları bu bölümde kayıtların toplanmasıyla bulundu, bir dosyada tek sayı olarak yazılı değildir.

Puanlanmayan 10 satır tahminin kendi `belirsiz` hücreleridir; nedenleri
[`discrepancies.md`](../../tests/results/discrepancies.md)'de ve `runs/lgipol01.md`'dedir (örneğin `NULL` sütunda
Db2'nin vereceği `SQLCODE`, `LINK` hatasının sonucu, sayısal olmayan numara). Tutmayan satır olmadığı için "bug"
etiketli bir kayıt açılmadı ve hata sınıflandırması boş kaldı ([`discrepancies.md`](../../tests/results/discrepancies.md),
"Tutmayan satırlar").

### 4.3 Bu yüzde ne söyler, ne söylemez

**Söyler:** Test ekibinin kodu okuyarak yazdığı müşteri ekle, müşteri sorgula ve motor sorgula tahminleri, programların
**COBOL mantığıyla** (uzunluk kontrolleri, dönüş kodları, alan eşlemesi ve dolgu, hangi programın çağrıldığı)
çelişmedi. Tahmin yazarları bilmediklerini `belirsiz` diye işaretledi, yani emin olmadıkları yerde iddia etmediler.

**Söylemez** ([`prediction-accuracy.md`](../../tests/results/prediction-accuracy.md), "Söylemedikleri";
[`discrepancies.md`](../../tests/results/discrepancies.md)):

1. **Düzenek gerçek sistem değildir.** Satırlar Db2'nin ya da VSAM'ın ne yaptığını değil, COBOL'un onlara **ne
   verdiğini** ölçer.
2. **Ortak varsayım görülemez.** Tahminin ön koşulları (B1–B7: tamponun 32500 bayt olması, sayısal `MOVE`'da kesme,
   sayaç değerleri) düzenekte yazıldıkları gibi kurulur. Tahmin ve düzenek aynı varsayımı paylaşırsa ikisinin de
   gerçek sistemden farklı çıkması bu ölçümle görülemez.
3. **Örnek küçük ve bağımsız değil.** Satırlar üç zincirden, aynı yazım biçimiyle çıkıyor. Müşteri ölçümü için
   `prediction-accuracy.md` ikili dağılımla %95 güvenle gerçek oranın en az ~%91,8 olduğunu hesaplar ve bunun
   **yalnızca yön gösterdiğini** belirtir.
4. **Tek kişi ölçtü.** Kuralları, düzeneği ve karşılaştırma betiğini aynı kişi yazdı. Kuralların çalıştırmadan önce
   commit'lenmesi ve betiğin bilerek bozulmuş çıktılarla sınanması (`runs/lgipol01.md` ve `runs/lgipdb01.md`'de üçer
   bozulma, hepsi yakalandı) bunu hafifletir, ortadan kaldırmaz.
5. **%100, "tahminler kusursuz" demek değildir.** Güncelle, sil, konut, hayat ve ticari tahminleri ölçülmedi.

## 5. LLM deneyi

### 5.1 Soru ve tasarım

**Soru:** Bir LLM, COBOL kodunu ve mimari belgedeki arayüzleri görerek operasyonları C#'a çevirirse, bizim elle
yazdığımız kodla aynı eşdeğerlik testlerinden kaçını geçer? Karşılaştırdığımız şey iki yaklaşımın **çıktısıdır**: bir
tarafta analiz, tahmin, test ve inceleme ile yazılan kodumuz, öbür tarafta modelin tek seferde yazdığı kod. Kurallar
deney başlamadan yazıldı ([`experiments/llm/README.md`](../../experiments/llm/README.md)); çalıştırmadan sonra ölçüt,
test ya da talimat sessizce değiştirilmedi, tek değişiklik kapsamın daraltılmasıdır (5.2).

| Konu | Karar | Kaynak |
|---|---|---|
| Model | Claude Opus 5.5, `claude-opus-5-5` (takma ad değil, sabit kimlik); araç kullanımı kapalı, varsayılan örnekleme ayarları | README bölüm 2 |
| Modele verilenler | Operasyonun COBOL program zinciri ve ortak copybook'lar, `docs/api-design.md`, `docs/database-schema.md`, `docs/architecture.md` bölüm 4 ve 8 | README bölüm 3 |
| Verilmeyenler | Bizim C# kodumuz, testler, tahmin tabloları, analizler, ADR'ler | README bölüm 3 |
| Talimat | Her operasyon için aynı şablon; her operasyon temiz bir oturum, tek atış, geri bildirim yok, elle düzeltme yok | README bölüm 5 |
| Tekrar | Her operasyon için 3 bağımsız tekrar (T1, T2, T3); üçü de raporlanır, en iyisi seçilmez | README bölüm 5.2 |
| Başarı ölçütü | Geçen test ÷ toplam test; eşik yok | README bölüm 6 |

Kodu üreten betik [`experiments/llm/generate.py`](../../experiments/llm/generate.py)'dir; her çağrının talimatı,
modelin ham cevabı ve kullanım bilgisi `experiments/llm/runs/2026-10-27/<operasyon>/<tekrar>/` altındadır.

### 5.2 Ne yapıldı, kapsam nasıl daraldı

Plan 18 operasyon × 3 tekrar = 54 çağrıydı. API kredisi bitince **16 çağrı** tamamlandı; kalan 38 çağrı "kredi
yetersiz" hatası aldı ([`runs/2026-10-27/README.md`](../../experiments/llm/runs/2026-10-27/README.md)). Üç tekrarı da
tamamlanan beş operasyon ölçüm kapsamına alındı: `musteri-ekle`, `musteri-sorgula`, `musteri-guncelle`, `motor-ekle`,
`motor-sorgula` (15 çağrı). `motor-guncelle` yalnızca T1 olarak çalıştı ve ölçüme girmez. Karar README'nin değişiklik
kaydındadır ve sonuç görülmeden alındı. **Seçim rastgele değildir:** çalıştırma sırasında ilk gelen müşteri ve motor
operasyonları kaldı; bu bir seçilim yanlılığıdır.

### 5.3 Testlerle çalıştırma

LLM çıktısı bizim `src/` kodumuza karıştırılmadı. [`experiments/llm/run_tests.py`](../../experiments/llm/run_tests.py)
`main`'in bir kopyasını çıkarır ve her çalıştırmayı orada yapar. **İzole düzende** (ana ölçü) incelenen operasyonun
elle yazılmış action'ı devre dışı bırakılır, yerine LLM'in dosyaları konur; testlerin kurulum adımlarındaki diğer
operasyonlar bizim kodumuzdur, böylece bir operasyonun hatası diğerlerinin puanını düşürmez. Test seti
`tests/GenApp.EquivalenceTests`'in `6e3dbb7` commit'indeki hâlidir ve değiştirilmedi
([`experiments/llm/results.md`](../../experiments/llm/results.md)).

### 5.4 Sonuç

Hücreler geçen / toplam testtir ([`tests/results/llm-comparison.md`](../../tests/results/llm-comparison.md), bölüm 1).

| Operasyon | Bizim kod | LLM T1 | LLM T2 | LLM T3 |
|---|---|---|---|---|
| `musteri-ekle` | 1/1 | 1/1 | 1/1 | 1/1 |
| `musteri-sorgula` | 2/2 | 2/2 | 2/2 | 2/2 |
| `musteri-guncelle` | 1/1 | 1/1 | 1/1 | 1/1 |
| `motor-ekle` | 2/2 | 2/2 | 2/2 | 2/2 |
| `motor-sorgula` | 4/4 | 4/4 | 4/4 | 4/4 |
| **Toplam** | **10/10** | **10/10** | **10/10** | **10/10** |

- 15 LLM çıktısının hepsi derlendi ve 10 testin 10'unu geçti; fark (LLM − bizim kod) her yerde 0.
- Beş operasyonun LLM kodu birlikte kurulduğunda da (bütünleşik düzen) T1, T2 ve T3 için 10/10 geçti.
- **Düzenek doğrulaması:** elle yazılmış action'lar devre dışıyken ve LLM dosyası yokken aynı testler 0/10 geçti
  (`experiments/llm/sonuclar/kontrol.json`). Yani geçişler LLM dosyalarından gelir.
- **Hata sınıflandırması:** dondurulmuş test setinde LLM hatası yok. Testlerin darlığından kaynaklanıp kaynaklanmadığını
  anlamak için `docs/api-design.md`'de yazılı 16 ek davranışı (büyük harfe çevirme, uzunluk sınırları, geçersiz tarih,
  başka türden poliçe vb.) ölçüm dışı olarak yokladım: elle yazılmış kod 16/16, LLM'in üç tekrarının üçü de 16/16
  geçti (`llm-comparison.md`, bölüm 2.2). Yani bu yoklamada da hata bulunamadı.

### 5.5 Sınırlar

Bu sonuç **bu beş operasyon içindir, 18 operasyona genellenmez.** Özellikle üç grup en karmaşık davranış kapsam dışında
kaldı (`llm-comparison.md`, bölüm 1):

- **güncellemede kodun ezilmesi** (poliçe güncellemede `02`, `01` ve `90` yazıldıktan sonra imleç kapatılırken `00`
  ile ezilir, `lgupdb01.cbl:346`, `:360`, `:371`);
- **silmede tür kontrolü** (poliçe silme poliçenin türüne bakmadan siler, `lgdpdb01.cbl:189-194`);
- **ticari liste sorguları** (müşteri ya da posta koduyla sorgularda eski kod kayıtları üst üste yazar).

Diğer sınırlar:

- **Test seti küçük ve tavana vurdu.** Beş operasyon için 10 test var; iki taraf da %100 olduğundan testler LLM'i
  bizim kodumuzdan ayıramadı. "Eşdeğer" yerine "bu testlerle fark yakalanmadı" demek doğrudur.
- **Girdiler tam adil değil.** LLM'e `docs/api-design.md` bilinçli sapmalarıyla verildi (elle yazan ekip de görmüştü).
  GenApp açık bir IBM örneğidir; modelin COBOL'u ya da çevirisini eğitiminde görmüş olması dışlanamaz.
- **Süreç ölçülmedi.** Analiz, tahmin ve incelemenin katkısı bu deneyle ölçülmedi; yalnızca iki kodun çıktısı
  karşılaştırıldı.
- **Tek model, tek tarih** (`claude-opus-5-5`, 2026-10-27), 15 çağrı.

## 6. Özet

- Elimizde CICS ve Db2 olmadan COBOL programlarını GnuCOBOL'da, `EXEC CICS` ve `EXEC SQL` komutlarını sahte
  parçalarla değiştirerek çalıştırabildik ([ADR 0004](../adr/0004-legacy-test-harness.md)).
- Üç programı ölçüm olarak çalıştırdık (LGICUS01, LGACUS01, LGIPOL01). Puanlanan 53 tahmin satırının 53'ü tuttu,
  10 satır tahminin kendi `belirsiz` demesi nedeniyle puanlanmadı. Bu, tahminlerin COBOL mantığıyla çelişmediğini
  gösterir; gerçek CICS ve Db2'yi ölçmez.
- LLM'in tek seferde yazdığı beş operasyon, bizim kodumuzla aynı 10 testten 10'unu geçti (üç tekrarın üçünde de).
  Sonuç bu beş operasyon içindir; en zor davranışlar ölçülmedi.
