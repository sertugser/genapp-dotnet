# LGIPOL01 çalıştırması: GnuCOBOL sonuçları ve test ekibinin tahminleri

| Alan | Değer |
|---|---|
| Tarih | 2026-10-17 |
| Sorumlu | Oğuz |
| Dal | `oguz/gnucobol-3` |
| Durum | **Ölçüm çalıştırması** (tahmin doğruluğuna sayılır; ölçülen üçüncü program) |
| Karşılaştırılan tahmin | `tests/predictions/05-motor-inquire.md` (21 satır, 05-01 ... 05-21) |
| GnuCOBOL | 3.2.0, MSYS2 ucrt64 |
| Ham çıktı | `runs/lgipol01-cikti.txt` |
| Karşılaştırma betiği | `runs/lgipol01-karsilastir.sh` (çıktısı: `runs/lgipol01-karsilastir-cikti.txt`, 299 kontrol) |
| Çalıştırma betiği | `runs/lgipol01-calistir.sh` |
| Dönüşüm betiği | `convert-lgipol01.py` |

> **Bu dosyanın ilk sürümü, hiçbir şey derlenmeden ya da çalıştırılmadan önce commit'lendi.** "Ölçüm kuralı: kontrol", "Değerlendirme kuralları",
> "Düzenek" ve "Çalıştırılacak durumlar" bölümleri o sürümdedir. "Sonuç" bölümleri çalıştırmadan sonra eklenmiştir. Kuralların çalıştırmadan
> önce yazıldığını `git log -- tests/gnucobol/runs/lgipol01.md` gösterir.

## Ölçüm kuralı: kontrol

- Tahmin dosyası **çalıştırmadan önce** `main`'deydi: `206f9ac` (2026-10-13, #105, "Motor poliçesi (ekle, sorgula, güncelle, sil) tahmin tablolarını
  ekle"). Dosya o günden beri değişmedi (`git log --follow origin/main -- tests/predictions/05-motor-inquire.md` tek commit gösterir).
- Çalıştırma anındaki `main`: `13d0e97`. Bu görevin bağımlılıkları da orada: LGICUS01 ölçümü (`runs/lgicus01.md`) ve motor tahmin tabloları (#24).
- Tahminin dayandığı kaynak commit `99e4be8` (dosya başlığı). `git diff 99e4be8 origin/main -- base/` **boş**: `base/` o günden beri değişmedi.
  Tahmindeki satır numaraları geçerli.
- Tahmin dosyasını çalıştırmadan önce okudum. Düzeneği dosyanın **kendi** ön koşullarına (B1, B2, B3, B4; B5 ve B6 bu dosyada yok) göre kuracağım;
  bunun dışında düzeneği tahmine uydurmayacağım.
- Sonuçlar tahminlerden önce kimseyle paylaşılmadı. Tahmin dosyasına çalıştırmadan sonra dokunulmayacak.

## Değerlendirme kuralları (çalıştırmadan önce belirlendi)

Yöntem `runs/lgicus01.md` ve `runs/lgacus01.md`'dekiyle aynıdır. Her satır, tahminin **ölçülebilir iddialarına** ayrılır (dönüş kodu, veri, yan etki). Sonra:

1. İddia, düzeneğin gösterebildiği bir şeyse ve çıktıda bayt bayt tutuyorsa: **tuttu**. Tutmuyorsa: **tutmadı**.
2. Tahminin kendisi `belirsiz` dediği hücreler **puanlanmaz**. Gerçek sonuç yazılır ama "tuttu/tutmadı" denmez. Bu dosyada: tarih ve zaman damgası
   alanlarının biçimi (`CA-ISSUE-DATE`, `CA-EXPIRY-DATE`, `CA-LASTCHANGED`, `CA-M-MANUFACTURED`: 05-01, 05-02 ve onlarla aynı veriyi dönen satırlar)
   ve 05-18, 05-20, 05-21'in bütün hücreleri.
3. Düzeneğin çalıştırmadığı bir şeye dayanan iddialar **ölçülemedi** diye ayrı yazılır ve puanlanmaz: LGSTSQ'nun kuyruğa yazdığı kayıt (TD `CSMT`,
   TS `GENAERRS`), gerçek tarih ve saat (`ASKTIME`/`FORMATTIME` `DISPLAY` olur; mesajların ilk 15 baytı boşluk kalır), gerçek Db2/CICS davranışı
   (`DATE`/`TIMESTAMP` biçimi, `NULL` göstergesiz sütunda `-305`, ABEND'in görevi sonlandırması, `[belge]` ifadeleri), `< 91` / `ELSE` dalı
   (iki dal aynı metni üretiyorsa çıktıdan ayırt edilemez: B1'e uygun tamponda 51-90. baytlar boşluktur, `< 91` dalının doldurduğu boşlukla aynı. 05-15'te
   hangi dalın çalıştığı bu yüzden **puanlanmaz**; puanlanan, LGSTSQ'ya giden metindir).
4. Ön koşullu iddialar (B1, B2, B3, B4), ön koşul düzenekte sağlandığı haliyle puanlanır. Ön koşul sağlanamıyorsa o hücre **ölçülemedi** olur.
5. Satırın kararı: puanlanan iddialardan biri tutmadıysa **tutmadı**; hepsi tuttuysa **tuttu**; puanlanan iddia yoksa **belirsiz**.
   Bir satırın birden çok durumu varsa (örn. 05-11: `EIBCALEN` 100, 28, 169) hepsi tutmalıdır.
6. Veri satırlarında düzeneğin tablosundan gelen değerler, tahminin kaynağıyla aynı JCL'den (`base/cntl/db2cre.jcl`) alınır. **Geçen bir veri iddiası
   Db2'nin sonucu değildir**: COBOL'un Db2'den gelen değerleri doğru yere ve doğru dolguyla taşıdığını ve `SELECT`'e doğru girdiyi verdiğini ölçer.
   `WHERE` koşulunu ve birleşimi (05-03, 05-04, 05-05) Db2 değerlendirmez, stub değerlendirir; COBOL'un ölçülen kısmı, `SQLCODE = 100` ve `0`'a verdiği tepkidir.
   Dönüşüm betiği `SELECT` metnini okuyup sütun ve `WHERE` ana bilgisayar değişkeni sırasını stub'ın beklediğiyle karşılaştırır.
7. Her `SELECT`/dönüş kodu iddiası kendi sürecinde ölçülür (`WS-REQUIRED-CA-LEN` süreç içinde korunur; tahmin Kod gözlemleri 10).

**Doğruluk yüzdesi** (çalıştırmadan önce sabitlendi, `runs/lgicus01.md` ve `runs/lgacus01.md` ile aynı formül):

- **Ana ölçü** = tutan satır sayısı ÷ puanlanan satır sayısı (tuttu + tutmadı). `belirsiz` satırlar paydada yoktur.
- **Yardımcı ölçü** = tutan satır sayısı ÷ dosyadaki bütün satırlar (`belirsiz` olanlar paydada, payda değil). Kötümser alt sınırdır.
- Bilgi için: puanlanan iddia (kontrol) düzeyinde de sayılır, ama karar satır düzeyindedir.

## Düzenek

Yöntem `tests/gnucobol/README.md`'deki gibi; farklar aşağıda. Programlar ayrı modüller, sürücü çalıştırılabilir dosya. Her durum ayrı süreçte çalışır.

| Parça | Dosya | Ne yapar |
|---|---|---|
| LGIPOL01, LGIPDB01 (kopya) | `work/lgipol01-gc.cbl`, `work/lgipdb01-gc.cbl` | `EXEC CICS` ve `EXEC SQL` komutları betikle (`convert-lgipol01.py`) değişir |
| Orijinal kopyalar | `work/lgipol01.cbl`, `work/lgipdb01.cbl` | `base/src/`'den değişmemiş kopya (`cmp` ile doğrulanır) |
| Motor `SELECT` stub'ı | `stubs/db2motor.cbl` | `POLICY` ve `MOTOR` tablolarını taklit eder; birleşimi ve `WHERE`'i kendisi değerlendirir |
| Desteklenmeyen deyim stub'ı | `stubs/db2unsup.cbl` | Motor dışı `SELECT` ve imleç deyimleri için: çağrılırsa süreci hata koduyla durdurur, **sonuç uydurmaz** |
| LGSTSQ stub'ı | `stubs/lgstsq.cbl` | Önceki ölçümlerdeki gibi: mesajı ve uzunluğunu ekrana yazar |
| SQLCA | `stubs/sqlca.cpy` | Önceki ölçümlerdeki gibi |
| Sürücü | `work/drv-lgipol01.cbl` | CICS ve Db2'nin rolünü oynar; her durum ayrı süreçte |

### Tahminin ön koşulları nasıl sağlanacak

| Kod | Tahminin istediği | Düzenekte |
|---|---|---|
| B1 | COMMAREA her LINK'te 32500 baytlık tek tamponun adresiyle geçer; `EIBCALEN` yalnızca bildirilen uzunluk; tamponun `EIBCALEN` ötesi boşluk | Sürücü tek 32500 baytlık tamponu kurar, `EIBCALEN`'den sonrasını boşlukla doldurur. `CALL ... USING DFHCOMMAREA` aynı adresi geçirir. `LINK ... LENGTH(32500)` karşılığı `EIBCALEN`'i 32500 yapıp geri yazar (`README`, "Measured runs"). Ayrı kısa alan **verilmez**. |
| B2 | `SQLCODE` 0 ile başlar | `SQLCA` kopyasında `VALUE` yok; GnuCOBOL sayısal alanı sıfırlar. **05-10 ile doğrulanır** (mesajda `+00000`). |
| B3 | Sayısal `MOVE`'da hane sayısına göre kesme | GnuCOBOL'un varsayılanı. **05-19 ile doğrulanır** (stub `SELECT`'e gelen müşteri numarasını yazar). |
| B4 | Hata kodunu ve veri durumunu düzenek kurar | Sürücü `DB2FORCE-SQLCODE` (0 = kullanma) ve `DB2FORCE-NULL` (`B` = `brokerId` NULL, `P` = `payment` NULL) verir. Sıfırsa stub'taki tabloya bakılır. |

### Düzeneğin tablosu (`base/cntl/db2cre.jcl`)

`POLICY`: poliçe 1 (müşteri 2, `M`), poliçe 3 (müşteri 5, `M`), poliçe 4 (müşteri 8, `E`). `MOTOR`: motor 1 ve motor 3. Başka satır yüklenmez
("test için gereken satırlar"); poliçe 999 yok, poliçe 4'ün `MOTOR` satırı yok (05-03, 05-05 bunu ister). Değerler JCL'den harfi harfine alınır.

### `EXEC` komutları

LGIPOL01 ve LGIPDB01'in komutları `README`'deki satırlarla çevrilir. Yeni olanlar (bu çalıştırmada `README`'ye eklenecek): `EXEC SQL DECLARE ... CURSOR`
(kaldırılır, yorum satırı olur), `OPEN/FETCH/CLOSE` ve motor dışı `SELECT` (`CALL 'DB2UNSUP'`), `INDICATOR` değişkenli `SELECT ... INTO` (`CALL 'DB2MOTOR'`).
Dönüşüm betiği her `EXEC` deyimini adıyla tanır; tanımadığı birinde durur ve çevrilen deyim sayısını kaynaktan sayılanla karşılaştırır.

## Çalıştırılacak durumlar

Zincir: sürücü → LGIPOL01 → LGIPDB01 (`C...`). Doğrudan: sürücü → **LGIPDB01** (`D...`). Tüm durumlarda `CA-REQUEST-ID = 01IMOT` (aksi yazılmadıkça),
dönüş kodu önceden `77` (programın hiç üretmediği değer; LGIPDB01 `99` üretir), `EIBCALEN` 32500 (aksi yazılmadıkça), müşteri 2 ve poliçe 1,
Db2 durumu normal, tamponun geri kalanı boşluk.

| Durum | Satır | Yol | `EIBCALEN` | Girdi |
|---|---|---|---|---|
| C0501 | 05-01 | zincir | 32500 | müşteri 2, poliçe 1 |
| C0502 | 05-02 | zincir | 32500 | müşteri 5, poliçe 3 |
| C0503 | 05-03 | zincir | 32500 | müşteri 2, poliçe 999 |
| C0504 | 05-04 | zincir | 32500 | müşteri 3, poliçe 1 |
| C0505 | 05-05 | zincir | 32500 | müşteri 8, poliçe 4 |
| C0506 | 05-06 | zincir | 32500 | Db2 `-904` verir |
| C0507 | 05-07 | zincir | 32500 | `CA-REQUEST-ID = 01imot` |
| C0508 | 05-08 | zincir | 32500 | `CA-REQUEST-ID = 01IXXX` |
| C0509 | 05-09 | zincir | 0 | |
| D0510 | 05-10 | LGIPDB01 | 0 | |
| C0511A, C0511B, C0511C | 05-11 | zincir | 100, 28, 169 | |
| D0512A, D0512B | 05-12 | LGIPDB01 | 169, 28 | |
| D0513 | 05-13 | LGIPDB01 | 170 | |
| D0514 | 05-14 | LGIPDB01 | 100 | poliçe 999 |
| D0515 | 05-15 | LGIPDB01 | 50 | Db2 `-904` verir |
| C0516 | 05-16 | zincir | 32500 | poliçe 1'in `brokerId` NULL'ı |
| C0517 | 05-17 | zincir | 32500 | poliçe 1'in `payment` NULL'ı |
| C0519 | 05-19 | zincir | 32500 | müşteri no `1000000002` |
| C0520A, C0520B | 05-20 | zincir | 32500 | müşteri no alanı boşluk / poliçe no alanı boşluk (**bilgi için**, puanlanmaz) |

**Çalıştırılmayacaklar:**

- **05-18** (göstergesiz sütunda NULL): `-305` yalnızca bir kod yorumunda yazıyor; Db2'nin gerçekte ne döndüreceği kodda yok. Düzenek `-305`'i zorlarsa bu 05-06'dan
  farksız olur ve bir şey ölçmez. **belirsiz / ölçülemedi** (tahmin zaten belirsizdi).
- **05-21** (`LINK` başarısız): `CALL`'un hatası (modül yok) CICS'in `LINK` hatasıyla aynı değil; bir şey ölçmez. **belirsiz / ölçülemedi** (tahmin zaten belirsizdi).

## Satır başına puanlanan iddialar

`05-01 beklenen değerler` tablosunun alanları "V1" diye anılır: `CA-BROKERID = 0000000000`, `CA-BROKERSREF` ␣×10, `CA-PAYMENT = 000000`, `CA-M-MAKE = FORD`+␣×11,
`CA-M-MODEL = KA`+␣×13, `CA-M-VALUE = 008500`, `CA-M-REGNUMBER = LL60LOO`, `CA-M-COLOUR = ORANGE`+␣×2, `CA-M-CC = 1000`, `CA-M-PREMIUM = 000450`,
`CA-M-ACCIDENTS = 000000`, 178-182. baytlar `FINAL`, 183-32500. baytlar değişmedi. Tarih ve zaman damgası alanları puanlanmaz. "V3" 05-02'nin değerleri:
`DENNIS`+␣×9, `ENGINE`+␣×9, `023500`, `FIRE1`+␣×2, `RED`+␣×5, `5000`, `002850`, `000012`, `CA-BROKERID = 0000000000`, `CA-BROKERSREF` ␣×10, `CA-PAYMENT = 000000`, `FINAL`.
"Dokunulmadı" = 29-32500. baytlar boşluk (rc dışında).

| Satır | Puanlanan iddialar |
|---|---|
| 05-01 | rc `00`; V1; 1-6, 9-18, 19-28. baytlar değişmedi; LGSTSQ'ya mesaj gitmedi; LGIPDB01 çağrıldı (`SELECT` göründü) |
| 05-02 | rc `00`; V3; istek no, müşteri no, poliçe no değişmedi; mesaj yok |
| 05-03 | rc `01`; dokunulmadı; `FINAL` yok; mesaj yok |
| 05-04, 05-05 | 05-03 ile aynı |
| 05-06 | rc `90`; dokunulmadı; `FINAL` yok; LGSTSQ'ya **iki** mesaj: (1) 87 bayt: 15 boşluk + ` LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR `+␣×2` SQLCODE=-00904`; (2) 99 bayt: `COMMAREA=` + COMMAREA'nın ilk 90 baytı (`01IMOT90`+`0000000002`+`0000000001`+␣×62) |
| 05-07 | rc `00`; V1; `CA-REQUEST-ID` `01imot` kaldı |
| 05-08 | rc `99`; `SELECT` hiç çalışmadı; dokunulmadı; mesaj yok |
| 05-09 | ABEND `LGCA` göründü; LGIPDB01 çağrılmadı (`SELECT` yok); LGSTSQ'ya **tek** mesaj, 45 bayt: 15 boşluk + ` LGIPOL01 NO COMMAREA RECEIVED`; COMMAREA dökümü gönderilmedi; tampon değişmedi |
| 05-10 | ABEND `LGCA` göründü; `SELECT` yok; LGSTSQ'ya **tek** mesaj, 87 bayt: 15 boşluk + ` LGIPDB01 NO COMMAREA RECEIVED`+␣×36+`+00000`; `' CNUM='`/`' PNUM='`/`' SQLCODE='` sabitleri mesajda yok; döküm yok. **B2** |
| 05-11 | üç durumda da: rc `00` (`98` değil); V1 (**tamponda**, 171-182. baytlar dahil); LGIPDB01 çağrıldı; mesaj yok. **B1** |
| 05-12 | iki durumda da: rc `98`; `SELECT` çalıştı; 29-182. baytlar boşluk (taşıma yapılmadı); `FINAL` yok; mesaj yok |
| 05-13 | rc `00`; V1 tamponda (`CA-M-PREMIUM = 000450`, `CA-M-ACCIDENTS = 000000`, 178-182. baytlar `FINAL`); mesaj yok. **B1** |
| 05-14 | rc `01` (`98` değil); dokunulmadı; mesaj yok |
| 05-15 | rc `90`; LGSTSQ'ya **iki** mesaj: (1) 05-06'nın birincisiyle aynı 87 bayt; (2) 99 bayt: `COMMAREA=` + COMMAREA'nın ilk **50** baytı + ␣×40 (`< 91` dalının metni; hangi dalın çalıştığı çıktıdan görünmez, puanlanmaz). **B4** |
| 05-16 | rc `00`; `CA-BROKERID = 0000000000`; V1'in kalanı. **B4** |
| 05-17 | rc `00`; `CA-PAYMENT = 000000`; V1'in kalanı. **B4** |
| 05-18 | çalıştırılmayacak: **belirsiz / ölçülemedi** |
| 05-19 | rc `00`; `SELECT`'e giden müşteri numarası `2`; V1; `CA-CUSTOMER-NUM` `1000000002` kaldı. **B3** |
| 05-20 | puanlanmaz (tahmin belirsiz); gerçek sonuç bilgi için yazılır |
| 05-21 | çalıştırılmayacak: **belirsiz / ölçülemedi** |

Beklenen 21 satırın dağılımı (çalıştırmadan önceki **plan**, sonuç değil): 18 satır puanlanır (05-01 ... 05-17, 05-19), 3 satır puanlanmaz (05-18, 05-20, 05-21).

## Sonuç

Kurallar `fc54d66` (2026-10-17 14:52) ile commit'lendi; düzenek ondan sonra derlendi ve çalıştırıldı. Hiçbir kural çalıştırmadan sonra değişmedi.
23 durumun 23'ü `exit=0` ile bitti ve hiçbirinde `RIG ERROR` (desteklenmeyen Db2 deyimi) görülmedi. Bütün karşılaştırmalar `runs/lgipol01-karsilastir.sh`
ile bayt bayt yapıldı (beklenen değerler yalnızca `05-motor-inquire.md`'den): **299 kontrol, 299 geçti** (253'ü satırların iddiası, 46'sı "süreç normal bitti /
desteklenmeyen deyime girilmedi" denetimi). Betiğin boş geçmediğini bozulmuş çıktılarla denedim (üç bozulma, hepsi yakalandı).

### Satır satır

| Test no | Durum | Gerçek sonuç | Karar |
|---|---|---|---|
| 05-01 | C0501 | `00`; `FORD`+␣×11 / `KA`+␣×13 / `008500` / `LL60LOO` / `ORANGE`+␣×2 / `1000` / `000450` / `000000`; `CA-BROKERID = 0000000000`, `CA-BROKERSREF` ␣×10, `CA-PAYMENT = 000000`; 178-182. baytlar `FINAL`; 183-32500 değişmedi; istek no, müşteri no, poliçe no değişmedi; `SELECT`'e müşteri 2 / poliçe 1 gitti; LGSTSQ'ya mesaj gitmedi | **tuttu** (tarih alanları puanlanmadı: tahmin belirsiz; stub'ın JCL'den aldığı metinler `2011-05-11`, `2012-05-10`, `2011-05-11 17:12:17`, `1999-07-19`) |
| 05-02 | C0502 | `00`; `DENNIS`+␣×9 / `ENGINE`+␣×9 / `023500` / `FIRE1`+␣×2 / `RED`+␣×5 / `5000` / `002850` / `000012`; broker, ref, ödeme 0 / ␣ / 0; `FINAL`; mesaj yok | **tuttu** (tarihler puanlanmadı; metinler `2011-09-08`, `2012-09-07`, `2011-09-08 13:14:45`, `1973-05-13`) |
| 05-03 | C0503 | `01`; 29-32500. baytlar boşluk kaldı; `FINAL` yok; mesaj yok | **tuttu** |
| 05-04 | C0504 | `01`; aynı | **tuttu** |
| 05-05 | C0505 | `01`; aynı | **tuttu** |
| 05-06 | C0506 | `90`; alanlar boş, `FINAL` yok; LGSTSQ'ya iki mesaj: (1) 87 bayt, 15 boşluk + ` LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR `+␣×2` SQLCODE=-00904`; (2) 99 bayt, `COMMAREA=01IMOT90`+`0000000002`+`0000000001`+␣×62 | **tuttu** |
| 05-07 | C0507 | `00`; 05-01'deki değerler; `CA-REQUEST-ID` `01imot` kaldı | **tuttu** |
| 05-08 | C0508 | `99`; `SELECT` çalışmadı; alanlar boş; mesaj yok | **tuttu** |
| 05-09 | C0509 | ABEND `LGCA` görüldü; LGIPDB01 çağrılmadı (`SELECT` yok, `LINK` yok); LGSTSQ'ya tek mesaj, 45 bayt: 15 boşluk + ` LGIPOL01 NO COMMAREA RECEIVED`; döküm gönderilmedi; tampon (hepsi boşluk) değişmedi | **tuttu** |
| 05-10 | D0510 | ABEND `LGCA`; `SELECT` yok; LGSTSQ'ya tek mesaj, 87 bayt: 15 boşluk + ` LGIPDB01 NO COMMAREA RECEIVED`+␣×36+`+00000`; `CNUM`/`PNUM`/`SQLCODE` sabitleri yok; tampon değişmedi | **tuttu** (`+00000` B2 ile: ön koşul sağlandı) |
| 05-11 | C0511A (100), C0511B (28), C0511C (169) | üçünde de `00` (`98` değil); 05-01'deki bütün değerler tamponda, 171-182. baytlar dahil (`CA-M-PREMIUM` `000450`, `CA-M-ACCIDENTS` `000000`, `FINAL`); `SELECT` çalıştı; mesaj yok | **tuttu** (B1 ile: tampon 32500 bayt) |
| 05-12 | D0512A (169), D0512B (28) | ikisinde de `98`; `SELECT` çalıştı; 29-32500. baytlar boşluk; `FINAL` yok; mesaj yok | **tuttu** |
| 05-13 | D0513 (170) | `00`; `CA-M-PREMIUM = 000450`, `CA-M-ACCIDENTS = 000000`, 178-182. baytlar `FINAL` tamponda; 05-01'in öteki değerleri de; mesaj yok | **tuttu** (B1 ile) |
| 05-14 | D0514 (100, poliçe 999) | `01` (`98` değil); `SELECT` çalıştı; alanlar boş; mesaj yok | **tuttu** |
| 05-15 | D0515 (50, Db2 `-904`) | `90`; alanlar boş; LGSTSQ'ya iki mesaj: (1) 05-06'nın birincisiyle aynı 87 bayt; (2) 99 bayt, `COMMAREA=01IMOT90`+`0000000002`+`0000000001`+␣×62 | **tuttu** (hangi dalın çalıştığı puanlanmadı, aşağıda) |
| 05-16 | C0516 | `00`; `CA-BROKERID = 0000000000`; öteki değerler 05-01 gibi; mesaj yok | **tuttu** (B4 ile; aşağıdaki sınıra bak) |
| 05-17 | C0517 | `00`; `CA-PAYMENT = 000000`; öteki değerler 05-01 gibi; mesaj yok | **tuttu** (B4 ile; aşağıdaki sınıra bak) |
| 05-18 | çalıştırılmadı | Düzenek göstergesiz sütunda NULL ve onun `SQLCODE`'unu Db2 gibi üretemez; `-305`'i zorlamak 05-06'dan farksız olur | **belirsiz / ölçülemedi** (tahmin zaten belirsizdi) |
| 05-19 | C0519 | `00`; stub'a giden müşteri numarası `+000000002` (10 haneli `1000000002`'nin en sol hanesi kesildi); müşteri 2'nin poliçe 1'inin değerleri döndü; `CA-CUSTOMER-NUM` `1000000002` kaldı | **tuttu** (B3 ile: ön koşul sağlandı) |
| 05-20 | C0520A, C0520B | Müşteri no alanı boşluk: Db2'ye giden değer `0`, `SQLCODE 100`, dönüş kodu `01`. Poliçe no alanı boşluk: aynı (`POLICYNUMBER=0`, `100`, `01`). Alanlara dokunulmadı | **belirsiz** (tahmin iddia etmiyordu). Bu, GnuCOBOL'un boşluğu `0` olarak taşımasıdır; başka derleyicide aynı olacağı söylenemez. |
| 05-21 | çalıştırılmadı | `CALL`'un hatası (modül yok) CICS'in `LINK` hatasıyla aynı değil; bir şey ölçmezdi | **belirsiz / ölçülemedi** (tahmin zaten belirsizdi) |

### Özet

| | Satır |
|---|---|
| tuttu | 18 (05-01 ... 05-17, 05-19) |
| tutmadı | **0** |
| belirsiz (puanlanmadı) | 3 (05-18, 05-20, 05-21) |

- **Ana ölçü:** 18 / 18 = **%100**.
- **Yardımcı ölçü:** 18 / 21 = **%85,7** (`belirsiz` satırlar paydada).
- Kontrol düzeyi (bilgi için): 253 satır iddiası, 253'ü geçti.
- Plan ile sonuç aynı: 18 satır puanlanacaktı, 3 satır puanlanmayacaktı (yukarıdaki "Satır başına puanlanan iddialar" bölümünün sonu).

Bu, `tests/results/prediction-accuracy.md`'deki "motor tahminleri henüz ölçülmedi" satırının yerini alacak ilk veridir. O dosyaya dokunmadım: görev yalnızca bu kaydı istiyor.

### Puanlanmayan iddialar (ölçülemedi)

Bunlar tuttu satırlarının içinde yazılı ama düzenek göstermediği için puanlanmadı:

- **LGSTSQ'nun kuyruğa yazdığı kayıt** (05-06, 05-09, 05-10, 05-15): TD `CSMT` ve TS `GENAERRS`'e yazma. LGSTSQ'nun gerçek kodu çalışmadı, stub'ı çalıştı. Ölçülen, LGSTSQ'ya **ne verildiği**.
- **Tarih ve saat:** `ASKTIME`/`FORMATTIME` `DISPLAY` oldu; mesajlardaki ilk 15 bayt boşluk. Tahmin "tarih, saat" diyor.
- **`DATE`/`TIMESTAMP` biçimi** (05-01, 05-02): tahmin belirsiz dedi. Gördüğümüz metin stub'ın JCL'den aldığı sabit; gerçek Db2 başka bir biçim döndürebilir.
- **`< 91` / `ELSE` dalı** (05-06, 05-15): iki durumda LGSTSQ'ya giden metin aynı (B1'e uygun tamponda 51-90. baytlar boşluk). Hangi dalın çalıştığı çıktıdan görünmez;
  `EIBCALEN = 50 < 91` ve `EIBCALEN = 32500` olduğu için kodu okuyarak `< 91` ve `ELSE` dallarının çalıştığını söyleyebiliriz, ölçemeyiz.
- **ABEND'in görevi sonlandırması** (05-09, 05-10): düzenekte `DISPLAY` + `GOBACK`. Ölçülen, ABEND'e kadar gidilen yol ve `LGCA` kodu.
- **Gerçek CICS davranışı** (B1): COMMAREA dışına yazmanın gerçek CICS'te çağıranın tamponuna düşmesi. Düzenekte tampon 32500 bayt ve tek parça; B1 bunu şart koşuyor.
- **LGIPOL01'in COMMAREA dökümü** (`lgipol01.cbl:124-138`): tahmin ölü kod diyor (Kod gözlemleri 9). İki iç `LGSTSQ` çağrısı **derlendi, hiç çalışmadı**.
- **Tahmin Kod gözlemleri 10'un uç durumu** (aynı `WORKING-STORAGE` ile ikinci çağrıda eşik 340): her durum ayrı süreçte çalıştığı için denenmedi.

### Düzeneğin sınırları (sonucun nasıl okunacağı)

- **`WHERE` ve birleşim.** 05-03, 05-04 ve 05-05'te `SQLCODE = 100`'ü Db2 değil stub üretti (müşteri ve poliçe numarasının tabloyla eşleşmemesi). COBOL tarafında ölçülen: `SELECT`'e doğru
  değerlerin gitmesi (dönüşüm betiği `SELECT` metnindeki `WHERE` ana bilgisayar değişkenlerini doğruladı; çıktıda `CUSTOMERNUMBER=` ve `POLICYNUMBER=` göründü) ve `100`'e `01` ile tepki vermesi.
  Birleşim koşulunun gerçekten bu sonucu verdiği, yani poliçe 4'ün `MOTOR`'da satırının olmadığı bilgisi JCL'den geliyor (`db2cre.jcl`), Db2'den değil.
- **NULL satırları (05-16, 05-17) zayıf ayırır.** JCL'de `brokerId` ve `payment` zaten 0. NULL olduğunda da sonuç 0 olduğundan çıktı, "taşıma atlandı" ile "0 taşındı" arasını ayırmaz.
  Ölçülen: gösterge `-1` iken program hata vermeden `00` dönüyor ve alan sıfır. Stub, Db2 kuralına uyarak NULL'da ana bilgisayar değişkenine dokunmaz; ona 0 dışında bir değer koymak sonuç uydurmak olurdu.
- **Veri satırları** (05-01, 05-02, 05-07, 05-11, 05-13, 05-16, 05-17, 05-19) Db2'nin ne döndürdüğünü değil, COBOL'un dönenleri COMMAREA'ya **doğru yere ve doğru dolguyla** taşıdığını ölçer.
  Değerler tahminin kaynağıyla aynı JCL'den alındı.
- **Ön koşullar (B1-B4) düzeneğin kararıdır.** Gerçek CICS ve Db2'de farklı çıkabilirler. "18/18 tuttu", gerçek sistemde 18/18 olacağı anlamına gelmez.

### Çalıştırılan komutların kapsamı

Dönüşüm betiği 36 `EXEC` deyimini çevirdi (LGIPOL01: 8, LGIPDB01: 28; sayılar kaynaktan sayılanla aynı). Çıktıda her çevrilen komutun çalıştığı görülen yeri:

| Komut | Çalıştı mı | Nerede |
|---|---|---|
| LGIPOL01 `ABEND`, `ASKTIME`, `FORMATTIME`, ilk `LGSTSQ` | evet | C0509 |
| LGIPOL01 `LINK LGIPDB01`, son `RETURN` | evet | C0501 |
| LGIPOL01'in iki iç `LGSTSQ` çağrısı | **hayır** (ölü kod) | derlendi |
| LGIPDB01 `ABEND` | evet | D0510 |
| LGIPDB01 `ASKTIME`, `FORMATTIME`, ilk `LGSTSQ` | evet | C0506, D0515 |
| LGIPDB01 `WRITE-ERROR-MESSAGE`'ın ikinci `LGSTSQ` çağrısı (`< 91` ya da `ELSE` dalı) | evet, ama hangi dalın çalıştığı çıktıdan görünmez | C0506 (`EIBCALEN = 32500`, kodu okuyarak `ELSE`), D0515 (`EIBCALEN = 50`, kodu okuyarak `< 91`). İki dalın ayrı ayrı çalıştığı **ölçülmedi**. |
| LGIPDB01 motor `SELECT`, `End-Program` `RETURN`, motor `98` `RETURN` | evet | C0501, D0512 |
| LGIPDB01 öteki dört `RETURN` (endowment, house, commercial 1 ve 2'nin `98` kolları) | **hayır** | derlendi |
| LGIPDB01 dört motor dışı `SELECT`, altı imleç deyimi | **hayır** | derlendi; girilirse `DB2UNSUP` süreci durdurur |
| LGIPDB01 iki `DECLARE CURSOR`, üç `INCLUDE` | çalışma zamanı yok | `DECLARE` kaldırıldı, `INCLUDE` → `COPY` |

## Düzenek düzeltmeleri (çalıştırma sırasında)

Çalıştırma sırasında düzeltilen bir şey olmadı: düzenek ilk derlemede temiz derlendi ve ilk çalıştırmada 299 kontrolün 299'u geçti. Çalıştırmadan önce yapılan iki düzeltme:

1. **Kural metni** (kuralları commit'lemeden önce). "Değerlendirme kuralları" 3. madde ilk taslakta 05-15'te `< 91` ve `ELSE` dallarının ayırt edilebileceğini yazıyordu; B1'e uygun
   tamponda bunlar aynı metni üretir. Cümleyi düzelttim; `fc54d66` düzeltilmiş halidir.
2. **Stub'ın döngü değişkeni** (`fc54d66`'den sonra, ilk derlemeden önce). `PI` adını `IX` yaptım (GnuCOBOL'da `PI` bir işlev adıdır). Hiçbir şey derlenip çalıştırılmadan önceydi.

Hiçbir düzeltme tahmini ya da programın mantığını değiştirmedi.

## Bug kayıtları

Tutmayan satır olmadığı için Bug Tracker'a **bug etiketli kayıt açılmadı**. Puanlanmayan üç satır (05-18, 05-20, 05-21) tahminin kendi `belirsiz` hücreleridir, bug değildir.
