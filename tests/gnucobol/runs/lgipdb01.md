# LGIPDB01 çalıştırması: EXEC SQL'i sahte SQLCODE ile değiştirip tahminle karşılaştırma

| Alan | Değer |
|---|---|
| Tarih | 2026-10-17 |
| Sorumlu | Oğuz |
| Dal | `oguz/gnucobol-sql` |
| Durum | **Yöntem denemesi ve karşılaştırma** (tahmin doğruluğu tablosuna yeni satır **eklemez**, aşağıya bak) |
| Karşılaştırılan tahmin | `tests/predictions/05-motor-inquire.md` (yalnızca `SQLCODE`'a bağlı satırlar: 05-01, 05-03, 05-06) |
| GnuCOBOL | 3.2.0, MSYS2 ucrt64 |
| Ham çıktı | `runs/lgipdb01-cikti.txt` |
| Karşılaştırma betiği | `runs/lgipdb01-karsilastir.sh` (çıktısı: `runs/lgipdb01-karsilastir-cikti.txt`) |
| Çalıştırma betiği | `runs/lgipdb01-calistir.sh` |
| Dönüşüm betiği | `convert-lgipdb01.py` |

> **Bu dosyanın ilk sürümü, hiçbir şey derlenmeden ya da çalıştırılmadan önce commit'lendi.** "Ölçüm kuralı: kontrol", "Değerlendirme kuralları",
> "Düzenek" ve "Çalıştırılacak durumlar" bölümleri o sürümdedir. "Sonuç" bölümleri çalıştırmadan sonra eklenmiştir. Kuralların çalıştırmadan
> önce yazıldığını `git log -- tests/gnucobol/runs/lgipdb01.md` gösterir.

## Bu kayıt ne yapar, ne yapmaz

- **Amaç:** İçinde `EXEC SQL` olan bir programı (LGIPDB01) GnuCOBOL'da çalıştırmak. Her `EXEC SQL` deyimi, `SQLCODE`'u test senaryosunun verdiği değere eşitleyen sahte bir parçayla
  değişir (`docs/adr/0004-legacy-test-harness.md`, karar 5). Böylece "Db2 şu `SQLCODE`'u döndürürse program hangi dönüş kodunu verir?" sorusu ölçülür.
- **Tahmin doğruluğuna etkisi yok.** 05-03 (`SQLCODE = 100`) ve 05-06 (`-904`) satırları LGIPOL01 zincirinde zaten ölçüldü (görev #28, `runs/lgipol01.md`). Aynı satırlar ikinci kez
  puanlanırsa doğruluk yüzdesi şişer. Bu kayıt onları **yeniden saymaz**; aynı iddiayı doğrudan LGIPDB01'de ve çok sayıda `SQLCODE` değeriyle sınar.
- **Yeni olan:** (1) tek bir sahte parçayla **bütün** `EXEC SQL` deyimlerinin (5 `SELECT`, `OPEN`, `FETCH`, `CLOSE`) değiştirilmesi; (2) `SQLCODE` taraması: 0, 100 ve dokuz farklı hata/uyarı kodu;
  (3) motor dışı kolların (hayat sigortası, konut, ticari) `SQLCODE`'a tepkisi, **bilgi için**.

## Ölçüm kuralı: kontrol

- Tahmin dosyası **çalıştırmadan önce** `main`'deydi: `206f9ac` (2026-10-13, #105). Dosya o günden beri değişmedi (`git log origin/main -- tests/predictions/05-motor-inquire.md` tek commit gösterir).
- Çalıştırma anındaki `main`: `13d0e97`. Tahminin dayandığı kaynak commit `99e4be8`; `git diff 99e4be8 origin/main -- base/` **boş**, tahmindeki satır numaraları geçerli.
- Bağımlılık #24 (motor tahmin tabloları) `main`'de. Bağımlılık #28 (LGIPOL01 ölçümü) `oguz/gnucobol-3` dalında, henüz `main`'de değil; bu kayıt ona **dayanmaz**, düzeneği baştan kurar
  (yalnızca değişmemiş kopyalar ve `stubs/lgstsq.cbl`, `stubs/sqlca.cpy` ortaktır).
- Tahmin dosyasını çalıştırmadan önce okudum. Sonuçlar tahminlerden önce kimseyle paylaşılmadı; tahmin dosyasına çalıştırmadan sonra dokunulmayacak.

## Değerlendirme kuralları (çalıştırmadan önce belirlendi)

1. **Sahte parça sonuç uydurmaz, yalnızca `SQLCODE`'u kurar.** Satırlar ve ana bilgisayar değişkenleri yerinde kalır (Db2'nin hata durumunda yaptığı gibi). Bu yüzden `SQLCODE = 0` iken COMMAREA'ya
   giden değerler Db2'nin değil, `INITIALIZE`'ın (sıfır/boşluk) değeridir: **veri hücreleri bu kayıtta puanlanmaz.**
2. **`SQLCODE`'un kendisi testin girdisidir**, programın ya da Db2'nin sonucu değil. Sonuç, "Db2 bu kodu döndürürse" biçiminde koşulludur (ADR 0004, karar 5; tahminin ön koşulu B4).
3. Her durum, tahminin hangi satırının **iddiasına** bağlandığıyla sınanır:
   - `SQLCODE = 0` → 05-01'in akış iddiaları: `CA-RETURN-CODE = 00`; 178-182. baytlar `FINAL`; 183-32500. baytlar değişmedi; istek no, müşteri no, poliçe no değişmedi; LGSTSQ'ya mesaj gitmedi;
     `SELECT` bir kez çalıştı.
   - `SQLCODE = 100` → 05-03: `CA-RETURN-CODE = 01`; 29-32500. baytlar boşluk kaldı; `FINAL` yok; LGSTSQ'ya mesaj gitmedi.
   - `SQLCODE` 0 ve 100 dışında bir değer `N` → 05-06 ("0 ve 100 dışında bir `SQLCODE` (örn. `-904`)", Kod gözlemleri 6: "Diğer SQLCODE'lar → `90`"): `CA-RETURN-CODE = 90`; 29-32500. baytlar
     boşluk kaldı; `FINAL` yok; LGSTSQ'ya **iki** mesaj: (1) 87 bayt: 15 boşluk + ` LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR ` + ␣×2 + ` SQLCODE=` + `N` (`PIC +9(5)`: işaret ve
     5 hane); (2) 99 bayt: `COMMAREA=` + COMMAREA'nın ilk 90 baytı (`01IMOT90` + `0000000002` + `0000000001` + ␣×62). Tahmin yalnızca `-904`'ü adıyla yazar; diğer kodlar **aynı kuralın uygulamasıdır**.
     `-305`, 05-18'in "göstergesiz sütunda NULL" girdisi değildir; burada `SQLCODE` doğrudan verilir. 05-18'in kendisi belirsiz kalır ve puanlanmaz.
4. Satırın (durumun) kararı: puanlanan iddialardan biri tutmadıysa **tutmadı**; hepsi tuttuysa **tuttu**.
5. **Ölçülemedi** (puanlanmaz): Db2'nin bu kodu gerçekte ne zaman döndüreceği, ana bilgisayar değişkenlerinin hata durumundaki gerçek davranışı, imleç davranışı, `NULL`/`-305`, `SQLSTATE`,
   `EXEC SQL` ön derleyicisinin yaptıkları, LGSTSQ'nun kuyruğa yazdığı kayıt, gerçek tarih ve saat (mesajların ilk 15 baytı boşluk), `< 91`/`ELSE` dalı (iki durum aynı metni üretir).
6. **Bilgi için** çalıştırılanlar (motor dışı kollar): gerçek sonuç yazılır, "tuttu/tutmadı" denmez. Bu kollar için `main`'de motor sorgusu dışında puanlanacak tahmin bu görevin kapsamında değil
   (hayat sigortası sorgusu tahmini henüz `main`'de değil).
7. Her durum ayrı süreçte çalışır (`WS-REQUIRED-CA-LEN` süreç içinde korunur: tahmin Kod gözlemleri 10).

**Sayılan şey:** durum (kod) başına karar ve iddia başına kontrol. Doğruluk yüzdesi **hesaplanmaz** (yukarıdaki gerekçe).

## Düzenek

| Parça | Dosya | Ne yapar |
|---|---|---|
| LGIPDB01 (kopya) | `work/lgipdb01-sql.cbl` | 28 `EXEC` deyimi betikle (`convert-lgipdb01.py`) değişir: 11 `EXEC SQL` deyimi sahte parçaya gider (5 `SELECT`, 2 `OPEN`, 2 `FETCH`, 2 `CLOSE`), 2 `DECLARE` kaldırılır, 3 `INCLUDE` → `COPY`, 12 `EXEC CICS` (6 `RETURN`, `ABEND`, `ASKTIME`, `FORMATTIME`, 3 `LINK LGSTSQ`) çevrilir |
| Orijinal kopya | `work/lgipdb01.cbl` | `base/src/lgipdb01.cbl`'den değişmemiş kopya (`cmp` ile doğrulanır) |
| Sahte Db2 | `stubs/db2stub.cbl` | `SQLCODE`'u sürücünün verdiği değere eşitler (`DB2STUB-SQLCODE`), başka hiçbir şeye dokunmaz |
| LGSTSQ stub'ı | `stubs/lgstsq.cbl` | Mesajı ve uzunluğunu ekrana yazar (önceki ölçümlerle aynı) |
| SQLCA | `stubs/sqlca.cpy` | Önceki ölçümlerle aynı |
| Sürücü | `work/drv-lgipdb01.cbl` | CICS'in rolünü oynar; **LGIPDB01'i doğrudan** çağırır; `<istek kodu> <SQLCODE> [EIBCALEN]` alır |

- **Neden doğrudan çağrı:** LGIPOL01 `SQLCODE`'a hiç bakmaz; yalnızca `LINK` eder (`lgipol01.cbl:91-96`). SQL davranışı LGIPDB01'dedir. Zincir #28'de ölçüldü.
- **B1:** sürücü tek 32500 baytlık tamponu kurar, `EIBCALEN` (varsayılan 32500) ötesini boşlukla doldurur. **B2:** `SQLCA` kopyasında `VALUE` yok, `SQLCODE` 0 ile başlar. **B3:** GnuCOBOL varsayılanı.
  **B4:** `SQLCODE`'u sürücü kurar.
- Dönüş kodu önceden `77` (programın hiç üretmediği değer; LGIPDB01 `99` üretir). Müşteri `0000000002`, poliçe `0000000001` (`db2cre.jcl:945-946`; yalnızca girdi, tablo yok).
- Kütüphaneler `build/sql/` altına derlenir (`build/` zaten `.gitignore`'da).

## Çalıştırılacak durumlar

Hepsi `<istek kodu> <SQLCODE>` olarak sürücüye verilir; `EIBCALEN = 32500`.

| Grup | Durumlar | Karşılaştırılan |
|---|---|---|
| Motor, `SQLCODE = 0` | `01IMOT 0` | 05-01'in akış iddiaları |
| Motor, `SQLCODE = 100` | `01IMOT 100` | 05-03 |
| Motor, hata/uyarı kodları | `01IMOT -904`, `-305`, `-803`, `-811`, `-911`, `-913`, `-922`, `-30081`, `+304` | 05-06'nın kuralı (`-904` tahminin adıyla yazdığı kod; geri kalanı aynı kuralın uygulaması) |
| Motor dışı kollar (bilgi için) | `01IEND`, `01IHOU`, `01ICOM`: 0, 100, -904. `03ICOM` (imleç): 0, 100, -904 | Puanlanmaz |

Toplam 11 + 12 = 23 durum. `01IXXX` ve `EIBCALEN` kontrolleri (05-08, 05-10, 05-12 …) `SQL` içermediği için bu kaydın konusu değil.

## Sonuç

Kurallar `8e70aa5` (2026-10-17 16:03) ile commit'lendi; düzenek ondan sonra yazıldı, derlendi ve çalıştırıldı. Hiçbir kural çalıştırmadan sonra değişmedi.
**Yöntem LGIPDB01 için çalıştı:** `EXEC SQL` bloklarının hepsi sahte `SQLCODE` parçasıyla değişti, program derlendi ve çalıştı; takıldığım bir derleme sorunu olmadı.
Planlanan 23 durumun 23'ü `exit=0` ile bitti. Bütün karşılaştırmalar `runs/lgipdb01-karsilastir.sh` ile bayt bayt yapıldı (beklenen değerler yalnızca `05-motor-inquire.md`'den ve yukarıdaki kurallardan):
**119 kontrol, 119 geçti.** Betiğin boş geçmediğini bozulmuş çıktılarla denedim (dönüş kodu, mesaj metni, `FINAL` işareti: üç bozulma, üçü de yakalandı).

### Motor sorgusu: `SQLCODE` → dönüş kodu

Girdi her durumda aynı: `01IMOT`, müşteri 2, poliçe 1, `EIBCALEN = 32500`; dönüş kodu önceden `77`. `SELECT` her durumda bir kez çalıştı.

| `SQLCODE` (sürücünün verdiği) | Tahmin | Gerçek `CA-RETURN-CODE` | LGSTSQ'ya giden | Karar |
|---|---|---|---|---|
| `0` | 05-01 akışı: `00`, `FINAL` | `00`; 178-182. baytlar `FINAL`; 183-32500 değişmedi; istek/müşteri/poliçe no değişmedi | mesaj yok | **tuttu** (veri hücreleri puanlanmadı, kural 1) |
| `100` | 05-03: `01` | `01`; 29-32500 boşluk kaldı; `FINAL` yok | mesaj yok | **tuttu** |
| `-904` | 05-06: `90` | `90`; alanlar boş; `FINAL` yok | iki mesaj, `SQLCODE=-00904` | **tuttu** |
| `-305` | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=-00305` | **tuttu** |
| `-803` | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=-00803` | **tuttu** |
| `-811` | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=-00811` | **tuttu** |
| `-911` | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=-00911` | **tuttu** |
| `-913` | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=-00913` | **tuttu** |
| `-922` | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=-00922` | **tuttu** |
| `-30081` | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=-30081` (5 haneye tam sığar) | **tuttu** |
| `+304` (uyarı kodu) | 05-06 kuralı | `90` | iki mesaj, `SQLCODE=+00304` | **tuttu** |

Hata kodu durumlarında ilk mesaj 87 bayt (15 boşluk + ` LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR `+␣×2` SQLCODE=`+kod), ikinci mesaj 99 bayt
(`COMMAREA=01IMOT90`+`0000000002`+`0000000001`+␣×62); bunların hepsi beklentiyle birebir aynı çıktı.

**Özet:** 11 durumun 11'i tuttu, 0 tutmadı. Bu sayılar **tahmin doğruluğuna eklenmez** (yukarıdaki gerekçe). Ne söylediği: "0 ve 100 dışındaki herhangi bir `SQLCODE` `90`'a gider"
kuralı yalnızca `-904` için değil, işaretli/işaretsiz ve farklı büyüklükte on kod için de tuttu; `SQLCODE`'un işareti ve büyüklüğü dönüş kodunu etkilemiyor, yalnızca mesajdaki sayıyı.
`+304`'ün `90` olması, programın uyarı kodunu da hata saymasıdır (tahminin "0 ve 100 dışında" ifadesiyle uyumlu).

### Bilgi için: motor dışı kollar (puanlanmadı)

Aynı sahte parça `SELECT`'lerin hepsini ve imleç deyimlerini değiştirdiği için motor dışı kollar da çalıştı. Bunlar için `main`'de bu görevin kapsamında puanlanacak bir tahmin yok (kural 6).

| İstek | `SQLCODE 0` | `100` | `-904` | Not |
|---|---|---|---|---|
| `01IEND` (hayat sigortası) | `00` | `01` | `90` | mesajda `SELECT ENDOW` |
| `01IHOU` (konut) | `00` | `01` | `90` | mesajda `SELECT HOUSE` |
| `01ICOM` (ticari, tek poliçe) | `00` | `01` | `90` | |
| `02ICOM` (ticari, poliçe numarasıyla) | `00` | `01` | `90` | çıktıda 1 `SELECT` |
| `03ICOM` (ticari, müşteri imleci) | `00` | `89` | `89` | `0`: `OPEN` + 21 `FETCH` + `CLOSE` = 23 sahte çağrı, döngü kodun kendi sınırıyla biter |
| `05ICOM` (ticari, posta kodu imleci) | **bitmedi** | `89` | `89` | `0`: sonsuz döngü, aşağıya bak |

- **İmleç `OPEN`'ında `100` de `89` olur:** `OPEN` sonrası `IF SQLCODE NOT EQUAL 0` her sıfırdan farklı değere `89` yazar ve `END-PROGRAM`'a gider (`lgipdb01.cbl:843-847`, `:926-930`). `100` burada
  "satır yok" olarak ayrılmaz. Gerçek Db2'nin `OPEN`'da `100` döndürüp döndürmeyeceği kodda yazmıyor (bu kayıtta ölçülemez); `OPEN` için `100`, sahte parçanın verdiği bir girdidir.
- **`03ICOM 0` neden biter:** her başarılı `FETCH` `ICOM-Record-Count`'u artırır ve 20'yi geçince `SQLCODE`'a `17` yazar (`lgipdb01.cbl:906-908`); döngü `SQLCODE > 0` olunca durur (`:849-850`).
- **Ek gözlem (ticari kol, hiçbir tahmine bağlı değil):** `EM-SQLREQ` `PIC X(16)` (`lgipdb01.cbl:58`), ticari `SELECT` metni `' SELECT Commercial '` 19 karakter (`:629`); hata mesajında `SELECT Commerci` olarak
  **16 bayta kesilir** (`01ICOM -904`, `03ICOM -904` çıktısında görülür).

### Takıldığım yer: `05ICOM 0` sonsuz döngü

- **Ne oldu:** `05ICOM` isteği `SQLCODE 0` ile verilince süreç hiç bitmedi. `FETCH Zip_Cursor` her seferinde sahte parçadan `SQLCODE = 0` alıyor, döngü bitmiyor; çıktı aynı iki satırı (`STUB EXEC SQL FETCH Zip_Cursor`,
  `STUB DB2STUB sets SQLCODE=0`) tekrarlıyor.
- **Neden:** Zip imleci döngüsü `Perform GET-Commercial-DB2-INFO-5-Cur With Test after Until SQLCODE > 0` ile `SQLCODE > 0` olana kadar döner (`lgipdb01.cbl:932-933`). Müşteri imlecinin döngüsünde 20 kayıtlık sınır
  vardır (`:906-908`); `GET-Commercial-DB2-INFO-5-Cur`'da böyle bir sayaç yoktur (`:946-990`). Gerçek Db2'de döngü satırlar bitince `100` ile durur. Sahte parça her `FETCH`'e `0` verdiği için satırlar hiç bitmez.
  Yani **bu bir düzenek sınırıdır** (sahte parça "satır bitti"yi bilmez), program hatası olarak kaydedilmedi.
- **Sonuç:** "her komuta aynı `SQLCODE`'u ver" yöntemi, `SQLCODE`'un sıraya bağlı değiştiği döngülerde (satır sayısı kadar `0`, sonra `100`) yetmez. Bu durumda sahte parçanın bir **sıra** (örn. "n kez `0`, sonra `100`") bilmesi gerekir; yapılmadı çünkü
  bu görevin kapsamı (motor sorgusu) bunu istemiyor ve ticari kol için `main`'de tahmin yok.
- **Olay:** bu durum çıktıyı sınırsız yazdığı için ilk denemede `runs/lgipdb01-cikti.txt` kısa sürede **1,1 GB**'a ulaştı; süreci öldürüp dosyayı sildim. Çalıştırma betiğine durum başına **20 saniye ve 400 satır** sınırı ekledim.
  Kesilen durum `[exit=124]` ile kayıtlıdır. Sınırdan sonra çıktı 37 KB.

### Çalıştırılan komutların kapsamı

`convert-lgipdb01.py` 28 `EXEC` deyimini çevirdi (sayılar kaynaktan sayılanla aynı).

| Komut | Çalıştı mı | Nerede |
|---|---|---|
| Motor `SELECT`, hayat sigortası, konut ve iki ticari `SELECT` (5 deyim) | evet | `01IMOT`, `01IEND`, `01IHOU`, `01ICOM`, `02ICOM` |
| `Cust_Cursor`: `OPEN`, `FETCH`, `CLOSE` | evet | `03ICOM 0` |
| `Zip_Cursor`: `OPEN`, `FETCH` | evet | `05ICOM` |
| `Zip_Cursor`: `CLOSE` | **hayır** | `05ICOM 0` hiç bitmedi, `CLOSE`'a gelinmedi |
| `ASKTIME`, `FORMATTIME`, `LINK LGSTSQ` (ilk ve ikinci) | evet | `-904` durumları |
| `RETURN` (`End-Program`) | evet | hepsi |
| `RETURN` (`98` kolları, beş tane), `ABEND`, `EIBCALEN < 91` dalı | **hayır** | `EIBCALEN` hep 32500; bu kaydın konusu değil (`runs/lgipol01.md` kapsıyor) |
| 3 `INCLUDE`, 2 `DECLARE CURSOR` | çalışma zamanı yok | `INCLUDE` → `COPY`, `DECLARE` kaldırıldı |

### Ne ölçüldü, ne ölçülemedi

**Ölçülen:** `SQLCODE` verilince programın **kendi** kararı: hangi dönüş kodunu yazdığı, hangi alanlara dokunduğu/dokunmadığı, `FINAL` işaretini yazıp yazmadığı, hata mesajının içeriği ve LGSTSQ'ya kaç mesaj gönderdiği.
Bu, tahminin "Db2 şu kodu döndürürse" koşullu iddialarının doğrudan karşılığıdır.

**Ölçülemeyen (bu yöntemle):**

- **Veri.** `SQLCODE = 0` iken sahte parça satır getirmez; COMMAREA'ya giden değerler `INITIALIZE`'ın sıfır/boşluğudur. `05-01` ve `05-02`'nin **veri** hücreleri ve ana bilgisayar değişkeni eşlemesi bu düzenekle sınanamaz
  (onları #28'in `DB2MOTOR` stub'ı sınar; o `SELECT`'in sütun ve `INTO` sırasını da kontrol eder, bu kayıtta kontrol yok).
- **Gerçek Db2 davranışı.** Db2'nin bir durumda gerçekte hangi `SQLCODE`'u döndüreceği (örn. göstergesiz sütunda `NULL` için `-305`: 05-18 hâlâ belirsiz), hata durumunda ana bilgisayar değişkenlerinin ne olduğu, `SQLSTATE`,
  kilit/zaman aşımı kodlarının ne zaman çıktığı, imleç davranışı.
- **`EXEC SQL` ön derleyicisi.** `PROCESS SQL` ve `INCLUDE`/`DECLARE` yönergelerinin Db2 tarafındaki etkisi (host variable–sütun tür uyumu, bağlama). Burada yalnızca metin düzeyinde çevrildi.
- **LGSTSQ'nun kuyruğa yazdığı kayıt ve gerçek tarih/saat** (önceki ölçümlerdeki gibi).

## Düzenek düzeltmeleri (çalıştırma sırasında)

1. **Altı bilgi durumu eklendi.** İlk çalıştırmada (23 durum) `02ICOM` ve `05ICOM` hiç çalışmadığı için bu iki `SELECT`/imleç deyiminin çalışıp çalışmadığı bilinmiyordu. `02ICOM` ve `05ICOM` için `0`, `100`, `-904`
   durumlarını ekledim (`runs/lgipdb01-calistir.sh`). Bunlar kurallarda zaten "bilgi için, puanlanmaz" sınıfındaydı; **puanlanan durumları etkilemez**. İlk 23 durumun çıktısı yeniden çalıştırmada **birebir aynı** (`diff` ile doğrulandı,
   423 satır).
2. **Zaman ve çıktı sınırı.** `05ICOM 0` yüzünden eklendi (yukarıda).
3. **Karşılaştırma betiği.** "Süreç normal bitti" denetimi yalnızca planlanan 23 durumu kapsıyor; eklenen altı durumun çıkış kodu bilgi bölümünde.

Hiçbir düzeltme tahmini, kuralları ya da programın mantığını değiştirmedi.

## Bug kayıtları

Tutmayan durum olmadığı için Bug Tracker'a **bug etiketli kayıt açılmadı**. `05ICOM 0` sonsuz döngüsü bir düzenek sınırıdır (sahte parça satır sayısını bilmez), tahmin satırı yoktur ve program hatası olarak
kaydedilmedi. Ticari kolda `EM-SQLREQ`'nun 16 bayta kesilmesi bir gözlemdir; hiçbir tahmin satırıyla çelişmediği için kayıt açılmadı.
