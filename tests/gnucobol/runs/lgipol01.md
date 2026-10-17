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
| Karşılaştırma betiği | `runs/lgipol01-karsilastir.sh` |
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

(Çalıştırmadan sonra doldurulacak.)
