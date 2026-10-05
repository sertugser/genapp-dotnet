# 02 — Müşteri sorgula (Inquire Customer)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Müşteri sorgula: müşteri numarasıyla Db2 `CUSTOMER` tablosundan müşteri bilgisi okunur |
| Programlar (çağrı zinciri) | `LGICUS01` (iş mantığı) → `EXEC CICS LINK` → `LGICDB01` (Db2 erişimi). Hata yollarında ikisi de `LGSTSQ`'ya LINK eder (`LGSTSQ` bu tahmin için okunmadı). |
| Okunan COBOL dosyaları | `base/src/lgicus01.cbl`, `base/src/lgicdb01.cbl`, `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy` |
| Test verisi kaynağı | `base/cntl/db2cre.jcl` (yalnızca 02-01 ve 02-02'deki somut değerler için; COBOL kodu değil) |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-03 |
| Kaynak commit (`git rev-parse HEAD`) | `e3813c206d2f4102b032a0fdda80b0bc14556b87` |

## Kod gözlemleri

1. **COMMAREA bayt haritası** (`lgcmarea.cpy:10-26`). Bu operasyonda kullanılan alanlar:

   | Bayt | Alan | PIC |
   |---|---|---|
   | 1-6 | `CA-REQUEST-ID` | X(6) |
   | 7-8 | `CA-RETURN-CODE` | 9(2) |
   | 9-18 | `CA-CUSTOMER-NUM` | 9(10) |
   | 19-28 | `CA-FIRST-NAME` | X(10) |
   | 29-48 | `CA-LAST-NAME` | X(20) |
   | 49-58 | `CA-DOB` | X(10) |
   | 59-78 | `CA-HOUSE-NAME` | X(20) |
   | 79-82 | `CA-HOUSE-NUM` | X(4) |
   | 83-90 | `CA-POSTCODE` | X(8) |
   | 91-93 | `CA-NUM-POLICIES` | 9(3) |
   | 94-113 | `CA-PHONE-MOBILE` | X(20) |
   | 114-133 | `CA-PHONE-HOME` | X(20) |
   | 134-233 | `CA-EMAIL-ADDRESS` | X(100) |
   | 234-32500 | `CA-POLICY-DATA` | X(32267) |

2. **Minimum COMMAREA uzunluğu 90 bayttır.** Hesap: `WS-CUSTOMER-LEN` (72, `lgpolicy.cpy:17`)
   + `WS-CA-HEADERTRAILER-LEN` (18, `lgicus01.cbl:57` / `lgicdb01.cbl:59`). Kontrol:
   `lgicus01.cbl:102-104`, `lgicdb01.cbl:137-140`. Bu 90 bayt yalnızca `CA-POSTCODE`'a kadar olan
   alanları kapsar. Programın yazdığı `CA-NUM-POLICIES`, `CA-PHONE-MOBILE`, `CA-PHONE-HOME` ve
   `CA-EMAIL-ADDRESS` (91-233. baytlar) bu kontrolün dışında kalır.
3. **LGICUS01, LGICDB01'i her zaman `LENGTH(32500)` ile çağırır** (`lgicus01.cbl:122-125`).
   LGICUS01'e gelen gerçek uzunluk ne olursa olsun LGICDB01 `EIBCALEN = 32500` görür. Bu yüzden
   LGICDB01'in kendi "COMMAREA yok" ve "COMMAREA kısa" kontrollerine (`lgicdb01.cbl:119-123`,
   `:137-143`) LGICUS01 üzerinden ulaşılamaz. Bu yollar yalnızca LGICDB01 doğrudan çağrılınca
   çalışır (02-09, 02-10, 02-11).
4. **LGICUS01, LGICDB01'in dönüş kodunu kontrol etmez.** LINK'ten sonra doğrudan
   `EXEC CICS RETURN` yapar (`lgicus01.cbl:109`, `:128`, `:115`). LGICDB01'in yazdığı
   `CA-RETURN-CODE` çağırana olduğu gibi gider.
5. **`CA-REQUEST-ID` iki programda da hiç okunmaz.** Mainline'da kontrol edilen tek şey
   `EIBCALEN`'dir (`lgicus01.cbl:87-107`, `lgicdb01.cbl:119-143`).
6. **Somut test verisi `db2cre.jcl`'den alındı.** 02-01 ve 02-02'deki müşteri numaraları ve
   değerler, Db2 tablolarını oluşturup örnek veriyi yükleyen `base/cntl/db2cre.jcl`'den geliyor.
   Bu değerler yalnızca test veritabanı bu JCL ile yüklendiyse ve sonradan değiştirilmediyse
   geçerlidir. Müşteri tablosunun metin sütunları sabit uzunlukludur (`CHAR(n)`,
   `db2cre.jcl:113-121`) ve COMMAREA alanları da `PIC X(n)` olduğu için değerler sağdan boşlukla
   tamamlanmış olarak beklenir.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 02-01 | LGICUS01 çağrılır. `EIBCALEN = 32500` (en az 233). `CA-CUSTOMER-NUM = 0000000001` (müşteri 1: `db2cre.jcl:437`). Db2 yalnızca `db2cre.jcl` ile yüklenmiş. | `CA-RETURN-CODE = 00` | Müşteri 1'in bilgileri döner: Andrew Pandy (`db2cre.jcl:438-439`). Dokuz alanın tam değerleri ve kaynakları aşağıdaki **"02-01 beklenen değerler"** tablosunda.<br>`CA-NUM-POLICIES = 000`. Müşteri 1'in `policy` tablosunda poliçesi olduğu halde (poliçe 10: `db2cre.jcl:777-778`) LGICUS01 bu alanı sıfırlar, LGICDB01 doldurmaz.<br>`CA-REQUEST-ID`, `CA-CUSTOMER-NUM`, `CA-POLICY-DATA` değişmez.<br>`CA-DOB`'un biçimi: **belirsiz**. | `lgicus01.cbl:93-94`, `:109`, `:122-125`<br>`lgicdb01.cbl:126`, `:146`, `:169-190`, `:193-194` | İş kuralı |
| 02-02 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM = 0000000011`. Db2 yalnızca `db2cre.jcl` ile yüklenmiş. Bu JCL'deki 10 müşteri kaydının numaraları 1-10 (`db2cre.jcl:437`, `:471`, `:505`, `:539`, `:573`, `:607`, `:641`, `:675`, `:709`, `:743`), yani 11 yok. SELECT, `SQLCODE = 100` döner. | `CA-RETURN-CODE = 01` | Kod müşteri alanlarına (19-233. baytlar) değer taşımaz ve onları temizlemez. Alanlarda ne kalacağı **belirsiz**.<br>`CA-NUM-POLICIES = 000` (LGICUS01).<br>Hata mesajı yazılmaz. | `lgicdb01.cbl:169-190`, `:195-196`<br>`lgicus01.cbl:94` | İş kuralı |
| 02-03 | LGICUS01 çağrılır. `EIBCALEN = 32500`. SELECT, `SQLCODE = -913` döner (bu durumun nasıl oluşturulacağı kodda yok). | `CA-RETURN-CODE = 01` (bulunamadı ile aynı kod) | Müşteri alanları: **belirsiz** (02-02 ile aynı neden).<br>`CA-NUM-POLICIES = 000`.<br>Hata mesajı yazılmaz: bu dalda `WRITE-ERROR-MESSAGE` çağrılmaz. | `lgicdb01.cbl:197-198`<br>`lgicus01.cbl:94` | İş kuralı |
| 02-04 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM = N`. SELECT, 0, 100 ve -913 dışında bir `SQLCODE` döner (negatif ya da 100 dışı pozitif). | `CA-RETURN-CODE = 90` | Müşteri alanları: **belirsiz**.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: tarih, saat, program adı olarak `' LGICUS01'` (LGICDB01'in kendi mesajında bile bu ad yazar), `' CNUM='` + N, `EM-SQLREQ` boşluk (hiç doldurulmuyor), `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı. | `lgicdb01.cbl:199-202`, `:212-228`, `:237-242`<br>`lgicdb01.cbl:44`, `:46-50`, `:148` | İş kuralı |
| 02-05 | LGICUS01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGICDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya `' NO COMMAREA RECEIVED'` içeren `ERROR-MSG` gönderilir. COMMAREA içeriği gönderilmez (`EIBCALEN > 0` koşulu sağlanmaz). | `lgicus01.cbl:87-91`, `:88`, `:135-149`, `:151` | Arayüz (COMMAREA) kuralı |
| 02-06 | LGICUS01 çağrılır. `EIBCALEN` 8 ile 89 arasında (sınır değer: `89`). | `CA-RETURN-CODE = 98` | Müşteri alanları doldurulmaz, LGICDB01 çağrılmaz.<br>Not: Uzunluk kontrolünden **önce** `MOVE '00' TO CA-NUM-POLICIES` çalışır. Bu alan 91-93. baytlarda olduğu için yazma gelen COMMAREA'nın dışına düşer. Etkisi **belirsiz**. | `lgicus01.cbl:93-94`, `:102-107`<br>`lgicus01.cbl:57`, `lgpolicy.cpy:17` | Arayüz (COMMAREA) kuralı |
| 02-07 | LGICUS01 çağrılır. `EIBCALEN` 1 ile 7 arasında. | **belirsiz**. Kod `CA-RETURN-CODE`'a `98` yazar, ama bu alan 7-8. baytlarda olduğu için gelen COMMAREA'nın dışında kalır. | LGICDB01 çağrılmaz. Diğer etkiler **belirsiz**. | `lgcmarea.cpy:10-11`<br>`lgicus01.cbl:93-94`, `:104-106` | Arayüz (COMMAREA) kuralı |
| 02-08 | LGICUS01 çağrılır. `EIBCALEN = 90` (kabul edilen en küçük uzunluk). Müşteri `CUSTOMER` tablosunda var. | `CA-RETURN-CODE = 00` | 90 baytın içinde kalan `CA-FIRST-NAME` … `CA-POSTCODE` dolar (02-01'deki eşleşmeyle).<br>`CA-NUM-POLICIES`, `CA-PHONE-MOBILE`, `CA-PHONE-HOME`, `CA-EMAIL-ADDRESS` (91-233. baytlar) gelen COMMAREA'nın dışına yazılır, çünkü LGICUS01 her zaman `LENGTH(32500)` verir. Çağıranın bunları görüp görmeyeceği ve bitişik belleğe etkisi **belirsiz**.<br>91-232 arası uzunluklarda da benzer durum olur. LGICDB01 doğrudan bu uzunluklarla çağrılırsa da aynısı geçerli. | `lgicus01.cbl:102-104`, `:122-125`<br>`lgicdb01.cbl:179-187`<br>`lgcmarea.cpy:16-25` | Arayüz (COMMAREA) kuralı |
| 02-09 | **LGICDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: `LGSTSQ`'ya `' NO COMMAREA RECEIVED'` içeren `ERROR-MSG` gönderilir.<br>LGICUS01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 3). | `lgicdb01.cbl:119-123`, `:212-228`, `:230` | Arayüz (COMMAREA) kuralı |
| 02-10 | **LGICDB01 doğrudan** çağrılır. `EIBCALEN` 8 ile 89 arasında (sınır değer: `89`). | `CA-RETURN-CODE = 98` | SQL çalışmaz, müşteri alanları doldurulmaz.<br>LGICUS01'den farklı olarak `CA-NUM-POLICIES`'e dokunulmaz.<br>1-7 bayt için 02-07'deki belirsizlik burada da geçerli. | `lgicdb01.cbl:126`, `:137-143`<br>`lgicdb01.cbl:59`, `lgpolicy.cpy:17` | Arayüz (COMMAREA) kuralı |
| 02-11 | **LGICDB01 doğrudan** çağrılır. `EIBCALEN` en az 233. `CA-CUSTOMER-NUM = N`, müşteri `CUSTOMER` tablosunda var. | `CA-RETURN-CODE = 00` | 02-01'deki dokuz alan aynı şekilde dolar.<br>`CA-NUM-POLICIES` **değişmez**: LGICDB01 bu alana yazmaz, sıfırlama LGICUS01'de yapılır. | `lgicdb01.cbl:126`, `:169-194`<br>(karşılaştırma: `lgicus01.cbl:94`) | Arayüz (COMMAREA) kuralı |
| 02-12 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID` herhangi bir değer (örn. boşluk ya da başka bir operasyonun kimliği). Müşteri var. | `CA-RETURN-CODE = 00` (02-01 ile aynı) | 02-01 ile aynı. İstek kimliği sonucu etkilemez. | `lgicus01.cbl:87-109`<br>`lgicdb01.cbl:119-154` | Arayüz (COMMAREA) kuralı |
| 02-13 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM` 10 haneli (1000000000 veya üstü). | **belirsiz** | **belirsiz**. `CA-CUSTOMER-NUM` 10 hanelik `PIC 9(10)`, onu alan `DB2-CUSTOMERNUMBER-INT` ise 9 hanelik `PIC S9(9) COMP`. Taşıma sırasında en soldaki hanenin kesilip kesilmeyeceği kodda belirtilmemiş, derleyici ayarına bağlı. Kesilirse başka bir müşteri numarası sorgulanabilir. | `lgcmarea.cpy:12`<br>`lgicdb01.cbl:72`, `:146` | Arayüz (COMMAREA) kuralı |
| 02-14 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM` sayısal değil (örn. boşluk). | **belirsiz** | **belirsiz**. İki programda da sayısallık kontrolü yok. Sayısal olmayan değer doğrudan `DB2-CUSTOMERNUMBER-INT`'e taşınır. | `lgicus01.cbl:87-109`<br>`lgicdb01.cbl:146` | Arayüz (COMMAREA) kuralı |
| 02-15 | LGICUS01 çağrılır. `EIBCALEN = 32500`. Müşteri satırı var ama seçilen sütunlardan biri NULL. | **belirsiz** | **belirsiz**. SELECT'te NULL gösterge (indicator) değişkeni yok. Db2'nin bu durumda hangi `SQLCODE`'u döndüreceği kodda yok. 0, 100 ve -913 dışında bir kod dönerse 02-04 yoluna (`90`) düşer. | `lgicdb01.cbl:179-187`, `:192-203` | İş kuralı |
| 02-16 | LGICUS01 çağrılır. `EIBCALEN = 32500`. LGICDB01'e yapılan LINK başarısız olur (örn. program tanımlı değil). | **belirsiz** | **belirsiz**. LINK'te `RESP`/`NOHANDLE` yok, programda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgicus01.cbl:122-125` | Arayüz (COMMAREA) kuralı |

## 02-01 beklenen değerler

Müşteri 1 için dönüşte COMMAREA'nın alan alan beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Beklenen değer | Değerin kaynağı (`db2cre.jcl`) | Alanı dolduran COBOL satırı |
|---|---|---|---|
| `CA-REQUEST-ID` X(6) | Girdideki değer, değişmez | — | Kodda bu alana yazılmıyor |
| `CA-RETURN-CODE` 9(2) | `00` | — | `lgicdb01.cbl:193-194` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000001` (girdi, değişmez) | `:437` | `lgicdb01.cbl:146` (yalnızca okunur) |
| `CA-FIRST-NAME` X(10) | `Andrew` + ␣×4 | `:438` (sütun `CHAR(10)`: `:113`) | `lgicdb01.cbl:170`, `:179` |
| `CA-LAST-NAME` X(20) | `Pandy` + ␣×15 | `:439` (sütun `CHAR(20)`: `:114`) | `lgicdb01.cbl:171`, `:180` |
| `CA-DOB` X(10) | 11 Temmuz 1950. JCL'de `'1950-07-11'` olarak ekleniyor, ama dönen metnin biçimi **belirsiz** | `:440` (sütun `DATE`: `:115`) | `lgicdb01.cbl:172`, `:181` |
| `CA-HOUSE-NAME` X(20) | ␣×20 (JCL'de `' '` ekleniyor) | `:441` (sütun `CHAR(20)`: `:116`) | `lgicdb01.cbl:173`, `:182` |
| `CA-HOUSE-NUM` X(4) | `34` + ␣×2 | `:442` (sütun `CHAR(4)`: `:117`) | `lgicdb01.cbl:174`, `:183` |
| `CA-POSTCODE` X(8) | `PI101OO` + ␣×1. Karakterler P-I-1-0-1-O-O: 4. karakter sıfır rakamı, son ikisi O harfi | `:443` (sütun `CHAR(8)`: `:118`) | `lgicdb01.cbl:175`, `:184` |
| `CA-NUM-POLICIES` 9(3) | `000` | Müşteri 1'in poliçesi var (poliçe 10, müşteri 1: `:777-778`) ama kod bunu saymıyor | `lgicus01.cbl:94` |
| `CA-PHONE-MOBILE` X(20) | `07799 123456` + ␣×8 | `:445` (sütun `phonemobile`, INSERT listesinde 9. sırada: `:434`; `CHAR(20)`: `:120`) | `lgicdb01.cbl:176`, `:185` |
| `CA-PHONE-HOME` X(20) | `01962 811234` + ␣×8 | `:444` (sütun `phonehome`, INSERT listesinde 8. sırada: `:433`; `CHAR(20)`: `:119`) | `lgicdb01.cbl:177`, `:186` |
| `CA-EMAIL-ADDRESS` X(100) | `A.Pandy@beebhouse.com` + ␣×79 | `:446` (sütun `Char(100)`: `:121`) | `lgicdb01.cbl:178`, `:187` |
| `CA-POLICY-DATA` X(32267) | Girdideki değer, değişmez | — | Kodda bu alana yazılmıyor |

Dikkat: `db2cre.jcl`'deki INSERT'te ev telefonu cep telefonundan **önce** geliyor (`:433-434`),
COMMAREA'da ise cep telefonu önce (`lgcmarea.cpy:23-24`). Değerler sıraya göre değil, sütun
adına göre eşleşir (`lgicdb01.cbl:176-177`, `:185-186`).

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 02-01 | Beklenen veri (yalnızca `CA-DOB` biçimi) | `DATEOFBIRTH` sütunu `DATE` türünde (`db2cre.jcl:115`) ve doğrudan `PIC X(10)` alana okunuyor (`lgicdb01.cbl:172`, `:181`). Değer `'1950-07-11'` olarak ekleniyor (`db2cre.jcl:440`), ama okunurken hangi biçimde geleceği Db2 ayarına bağlı ve kodda yazmıyor. |
| 02-02 | Beklenen veri | SQLCODE 100 dalında kod alanlara dokunmuyor (`lgicdb01.cbl:195-196`). Db2'nin "satır yok" durumunda host değişkenlerine yazıp yazmadığı kodda görünmüyor. |
| 02-03 | Beklenen veri; girdinin nasıl oluşturulacağı | Veri için 02-02 ile aynı neden. Ayrıca `-913`'ün ne anlama geldiği ve testte nasıl üretileceği kodda açıklanmıyor. Kod sadece onu `01`'e eşliyor. |
| 02-04 | Beklenen veri (müşteri alanları) | Hata durumunda SELECT'in alanlara ne yazdığı kodda görünmüyor. |
| 02-06 | Beklenen veri (not kısmı) | `lgicus01.cbl:94` uzunluk kontrolünden önce çalışıyor ve COMMAREA dışına yazıyor. Bunun bellekte neye denk geldiği CICS'e ve GnuCOBOL test düzeneğine bağlı. |
| 02-07 | Dönüş kodu, beklenen veri | `CA-RETURN-CODE` 7-8. baytlarda. 7 bayttan kısa COMMAREA'da `98` alanın dışına yazılıyor. |
| 02-08 | Beklenen veri (91-233. baytlar) | Minimum uzunluk kontrolü 90 bayt istiyor ama program 233. bayta kadar yazıyor. LGICUS01 her zaman `LENGTH(32500)` geçiyor. Fazla baytların nereye yazılacağı ortama bağlı. |
| 02-13 | Dönüş kodu, beklenen veri | `PIC 9(10)` → `PIC S9(9) COMP` taşımasında kesilme olup olmadığı derleyici ayarına bağlı. Kodda bu ayar yok. |
| 02-14 | Dönüş kodu, beklenen veri | Sayısal olmayan veri sayısal alana taşınıyor ve hiçbir doğrulama yok. Sonuç tanımsız. |
| 02-15 | Dönüş kodu, beklenen veri | Kodda NULL göstergesi yok. Db2'nin döndüreceği SQLCODE kodda görünmüyor. |
| 02-16 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil. |
