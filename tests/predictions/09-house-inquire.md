# 09 — Konut poliçesi sorgula (Inquire House Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Konut poliçesi sorgula: müşteri ve poliçe numarasıyla Db2 `POLICY` ve `HOUSE` tablolarının birleşiminden tek satır okunur; ortak poliçe alanları ve konut alanları COMMAREA'ya yazılır. Poliçe türü COMMAREA'daki istek kodundan (`01IHOU`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGIPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGIPDB01` (Db2 SELECT). Hata yollarında ikisi de `LGSTSQ`'ya LINK eder. VSAM okunmaz: LGIPDB01 yalnızca LGSTSQ'ya LINK eder (`lgipdb01.cbl:1010-1027`); `lgipvs01.cbl` bu zincirde değildir. Programlar dört poliçe türüne de hizmet eder; yalnızca konut (`01IHOU`) kolu ve ortak kısımlar tahmin edildi, diğer kollar "başka tür, dal alınmaz" diye işaretlendi. |
| Okunan COBOL dosyaları | `base/src/lgipol01.cbl` (1-139), `base/src/lgipdb01.cbl` (1-1030), `base/src/lgstsq.cbl` (1-126), `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy`. Karşılaştırma için yalnızca `base/src/lgapdb01.cbl:184` (09-04). |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` ve `HOUSE` tanımları (`:161-185`, `:240-256`), müşteri numaraları 1-10 (`:437` … `:743`), poliçe 6, 7, 8 ve 1 (`:861-882`, `:885-906`, `:909-930`, `:933-954`), `HOUSE` 6, 7, 8 (`:1042-1091`), poliçe 1'in `MOTOR` satırı (`:1093-1114`). `base/cntl/cobol.jcl` (`:44-50`): derleme seçenekleri (B3). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `docs/api-design.md` (`:16`, `:23`, `:53`, `:109-111`), `base/Reference.md` (`:119-121`): yalnızca Kod gözlemleri 8 ve 16 ile 09-13'te. `docs/adr/0004-legacy-test-harness.md` (`:44-52`, `:56-60`, `:96-114`, `:135-138`): yalnızca B4'teki ek, B8, C grubu, [CICS bilgisi], Kod gözlemleri 14 ve 17 ile Açık sorular için. **Kodla doğrulanmadı.** |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-14 |
| Kaynak commit (`git rev-parse HEAD`) | `4a1893e06b85c6ffd2c015c90343d54d62b94b58` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge, kodla doğrulanmadı]**: `base/` ya da `docs/` altındaki açıklama metinleri. Belgeye dayanan bir ifade kodda
  doğrulanmamıştır; hiçbir kesin tahmin yalnızca belgeye dayanmaz.
- **[Db2 bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen Db2 davranışı (`11-house-delete.md` ile aynı etiket).
  Bu dosyada tek kullanımı: satır bulamayan bir `SELECT … INTO` `SQLCODE = 100` döner (09-05, 09-06, 09-07, 09-08, 09-22).
- **[CICS bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen CICS davranışı (`10-house-update.md` ile aynı etiket). Bu dosyada:
  (1) `LINK … LENGTH(n)` ile çağrılan program `EIBCALEN = n` görür; bu yüzden zincirdeki alt programın uzunluk kontrolü en dıştaki
  çağıranın uzunluğuna bakmaz ("zincirde `98` dönmez"); (2) `EXEC CICS ABEND` görevi sonlandırır ve `HANDLE ABEND` olmadığı için
  denetim çağırana dönmez ("görev sonlanır"). Düzenek bu davranışları ölçmez ([belge, kodla doğrulanmadı]
  `docs/adr/0004-legacy-test-harness.md:107-109`); düzenekteki karşılıkları B1 (LINK) ve B8 (ABEND).
- **C (belirsizlik grubu)**: sonucu kodda tanımlı olmayan, çalışma ortamına bağlı davranış: gerçek CICS (ABEND sonrası iş biriminin
  geri alınması, görev sonu, LINK hatası), gerçek Db2 (iş birimi, tarih ve zaman damgası biçimi, NULL, kodda yazmayan hata kodları),
  gerçek VSAM ya da derleyici (sayısal olmayan içeriğin `COMP` alana taşınması). "(C)" işaretli hücre **belirsiz**dir: ne ön koşul
  koduyla ne [Db2 bilgisi] ya da [CICS bilgisi] ile kesinleştirilmiştir ve düzenekte ölçülmez. [belge, kodla doğrulanmadı]
  `docs/adr/0004-legacy-test-harness.md:96-102`, `:107-114`.
- Etiketsiz kaynaklar [kod]'dur.

Ön koşul kodları test düzeneğinin vereceği kararlardır (kodlar `01-customer-add.md`, `02-customer-inquire.md` ve
`08-house-add.md` ile aynıdır). Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul sağlanmazsa tahmin geçersizdir
ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması) kendisi kurar. **Aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir; satırda yalnızca düzeneğin değiştirdiği değer yazılır. **Bu dosyada ayrıca:** düzenek host değişkenlerine yalnızca satırda yazılan değeri koyar. Veri döndüren başarılı satırlarda (09-01, 09-02, 09-03, 09-04, 09-10, 09-12, 09-15, 09-18, 09-21) bu değer poliçenin Db2'deki değerleridir. [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:56`, `:135-138`: stub yalnızca SQLCODE'u doldurur; veri döndüren deyimler açık soru. |
| B6 | Db2'de `POLICY` tablosuna `DEFAULT` ile daha önce hiç satır eklenmemiştir; ilk kimlik değeri `START WITH` değeridir (1000001: [kod] `db2cre.jcl:162-163`). Bu dosyada yalnızca 09-18'de, 08-01'in ürettiği numara için kullanılır. |
| B8 | Düzenekte `EXEC CICS ABEND` bir mesaj ve `GOBACK` ile değiştirilir: abend eden program çağırana döner ve COMMAREA'ya en son yazılan dönüş kodu görünür. [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:44-46`. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır; orada çağıranın kodu görmesi belirsizdir (C). Bu dosyada yalnızca `LGCA` satırlarında (09-14, 09-19): COMMAREA olmadığı için dönüş kodu görünmez, düzenekte görülen ABEND mesajı ve `GOBACK`'tir. |

(B5 ve B7 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası, konut görünümü** (`lgcmarea.cpy:10-13`, `:34-44`, `:56-63`). İki program da bu düzeni kullanır
   (`lgipol01.cbl:62-63`, `lgipdb01.cbl:217-220`). Bayt konumları `08-house-add.md` Kod gözlemleri 1'deki tabloyla aynıdır.
   Sorgulamada:
   - **Okunan baytlar:** 1-6 `CA-REQUEST-ID` (`lgipdb01.cbl:275`), 9-18 `CA-CUSTOMER-NUM` ve 19-28 `CA-POLICY-NUM`
     (`lgipdb01.cbl:263-264`). 29. bayttan sonraki girdiler hiçbir satırda okunmaz.
   - **Yazılan baytlar:** 7-8 `CA-RETURN-CODE`; başarı yolunda 29-100 `CA-POLICY-COMMON` (`lgipdb01.cbl:503`), 101-158
     `CA-HOUSE(1:58)` (`lgipdb01.cbl:504`), 159-163 `'FINAL'` (`lgipdb01.cbl:508`).
   - **Hiç yazılmayan baytlar:** 1-6, 9-28 ve 164-32500.
2. **Çağrı zinciri ve uzunluk.**
   - LGIPOL01'de **uzunluk kontrolü yok**. Program yalnızca `EIBCALEN = 0`'a bakar (`lgipol01.cbl:79-83`), `'00'` yazar (`:86`) ve
     LGIPDB01'i `Length(32500)` ile çağırır (`lgipol01.cbl:91-94`). Eklemede LGAPOL01'in yaptığı 28 baytlık kontrol
     (`lgapol01.cbl:113`) burada yok.
   - [CICS bilgisi] LGIPDB01 her zaman `EIBCALEN = 32500` görür. Bu yüzden `EIBCALEN = 0` kontrolüne (`lgipdb01.cbl:251-255`) ve 163'lük kontrole
     (`lgipdb01.cbl:487-489`) zincirden ulaşılamaz.
   - Gerekli uzunluk: `WS-CA-HEADERTRAILER-LEN` 33 (`lgipdb01.cbl:73`) + `WS-FULL-HOUSE-LEN` 130 (`lgpolicy.cpy:25`) = **163**
     (`lgipdb01.cbl:482-483`). 163, `'FINAL'` işaretinin son baytıdır (159-163).
   - **Sonuç:** zincirde çağıranın COMMAREA'sı 28-162 bayt olsa bile başarı yolu çalışır ve 29-163. baytlara yazar; bu yazmalar
     çağıranın bildirdiği alanın dışına düşer (09-15, ön koşul B1).
3. **Uzunluk kontrolü SELECT'ten sonra gelir.** Akış sırası: SELECT (`lgipdb01.cbl:444-476`) → `IF SQLCODE = 0` (`:478`) → gerekli
   uzunluk (`:482-483`) → `IF EIBCALEN < 163` (`:487`). Sonuçları (doğrudan çağrıda):
   - Kısa COMMAREA + kayıt var → `98` (09-20). SELECT yine de çalışmıştır; sorgu salt okuma olduğu için yan etkisi yoktur.
   - Kısa COMMAREA + kayıt yok → `98` değil **`01`** (09-22).
   - Kısa COMMAREA + SQL hatası → **`90`** ve hata mesajı (09-23).
   - `98` yolunda veri taşınmaz ve `'FINAL'` yazılmaz: RETURN (`:489`) `:503-508`'den önce gelir.
4. **İstek kodu ve `UPPER-CASE`.**
   - LGIPOL01 `CA-REQUEST-ID`'yi hiçbir satırda okumaz; her değeri LGIPDB01'e geçirir.
   - LGIPDB01 istek kodunu büyük harfe çevirip ayrı bir alana yazar: `MOVE FUNCTION UPPER-CASE(CA-REQUEST-ID) TO WS-REQUEST-ID`
     (`lgipdb01.cbl:275`, alan `:81`). `EVALUATE` bu kopya üzerinde yapılır (`:277`). `CA-REQUEST-ID` değişmez.
   - Sonuç: `'01ihou'` ya da `'01iHoU'` konut koluna gider (09-04). Eklemede karşılaştırma büyük/küçük harfe duyarlıydı
     (`lgapdb01.cbl:184`; 08-11).
   - `WHEN OTHER` → `'99'` (`lgipdb01.cbl:307-308`). Eklemeden farklı olarak hemen RETURN yapılmaz; akış `End-Program`
     paragrafına (`:312`) düşer ve oradan RETURN yapılır (`:313`). Mesaj yazılmaz, SQL çalışmaz.
5. **`policyType` hiç okunmaz.** `POLICYTYPE` ne `lgipol01.cbl`'de ne `lgipdb01.cbl`'de geçer (büyük/küçük harf duyarsız grep,
   sonuç boş). Konut SELECT'inin listesinde `POLICYTYPE` yok (`lgipdb01.cbl:445-456`); `WHERE` yalnızca iki tabloyu
   birleştirir ve müşteri ile poliçe numarasına bakar (`:470-475`). Bir poliçenin `01IHOU` ile dönmesi için `HOUSE` satırının
   olması yeterlidir.
   - **Poliçe 7:** `policyType = 'C'` (`db2cre.jcl:901`) ama `HOUSE` satırı var (`db2cre.jcl:1068-1074`). `01IHOU` ile `00` ve
     FARM verisi döner (09-03).
   - Ters yönde, `HOUSE` satırı olmayan bir poliçe (örn. motor poliçesi 1) `01IHOU` ile `01` döner (09-08).
   - **Not:** C# tarafında `policyType` kontrolü eklenirse bu bilinçli bir sapma olur; karar ekibindir.
6. **Sorgulamada `70` yok.** `'70'` iki programda da geçmez (grep sonucu boş). Kayıt bulunamayan her durum [Db2 bilgisi] `SQLCODE = 100` ile
   `01`'e gider (`lgipdb01.cbl:512-514`): poliçe yok (09-05), müşteri-poliçe eşleşmiyor (09-06), müşteri yok (09-07), poliçe başka
   türde (09-08). Eklemedeki `70` (`lgapdb01.cbl:295-296`) yalnızca yabancı anahtar ihlalinden gelir; SELECT'te yabancı anahtar
   kontrolü olmaz.
7. **Konut SELECT'i ve COMMAREA'ya taşıma** (`lgipdb01.cbl:441-523`).
   - Hazırlık: `INITIALIZE DB2-IN-INTEGERS`, `DB2-OUT-INTEGERS`, `DB2-POLICY` (`lgipdb01.cbl:243-245`) ve konut kolunda
     `INITIALIZE DB2-HOUSE` (`:284`). Sayısal alanlar 0, metin alanları boşluk olur.
   - SELECT: 12 sütun (`:445-456`) `DB2-*` alanlarına okunur (`:457-468`). `POLICY` ve `HOUSE`'un birincil anahtarı
     `policyNumber` olduğu için (`db2cre.jcl:173`, `:248`) en fazla bir satır döner.
   - Gösterge değişkenleri: `IND-BROKERID` ve `IND-PAYMENT` kontrol edilir (`lgipdb01.cbl:494-499`); gösterge -1 ise MOVE
     atlanır ve alan `INITIALIZE`'dan gelen 0 kalır (`:245`). **`IND-BROKERSREF` doldurulur (`:461`) ama hiçbir satırda kontrol
     edilmez** (grep: `:204`, `:350`, `:461`, `:552`). `brokersReference` NULL olursa `DB2-BROKERSREF`'te ne kalacağı Db2'nin
     NULL'da host değişkene dokunup dokunmamasına bağlıdır (C); bu durum için tahmin yazılmadı.
   - Göstergesi olmayan sütunlar: `ISSUEDATE`, `EXPIRYDATE`, `LASTCHANGED` ve altı `HOUSE` sütunu. Bunlardan biri NULL olursa
     SELECT'in hata vereceğini yalnızca yorum söyler (`:200-202`, "FAIL with SQLCODE=-305"); bu Db2 bilgisidir (C). Hata
     olursa `90` yolu çalışır. Örnek verideki konut satırlarında NULL yok (`db2cre.jcl:1051-1091`).
   - Sayısal dönüşümler: `DB2-H-BEDROOMS-SINT` (`S9(4) COMP`) → `DB2-H-BEDROOMS` 9(3), `DB2-H-VALUE-INT` (`S9(9) COMP`) →
     `DB2-H-VALUE` 9(8) (`lgipdb01.cbl:500-501`; alanlar `lgpolicy.cpy:66-67`). 999'dan büyük oda ya da 99999999'dan büyük
     değer B3 ile kesilir. Eklemeden gelen değerler (9(3), 9(8): `lgcmarea.cpy:58-59`) bu sınırları aşamaz. Ödeme için de aynı
     durum geçerli: `INTEGER` → 9(6) (`lgipdb01.cbl:498`).
   - Taşıma: `DB2-POLICY-COMMON` (72 bayt, `lgpolicy.cpy:45-51`) → `CA-POLICY-COMMON` (`lgcmarea.cpy:37-43`), iki düzen alan alan
     aynı (`lgipdb01.cbl:503`). `DB2-HOUSE` (58 bayt, `lgpolicy.cpy:64-70`) → `CA-HOUSE(1:WS-HOUSE-LEN)` (`lgipdb01.cbl:504`,
     `WS-HOUSE-LEN = 58`: `lgpolicy.cpy:20`); alan sırası `lgcmarea.cpy:57-62` ile aynı.
   - `'FINAL'` → `CA-H-FILLER(1:5)` (`lgipdb01.cbl:508`), yani 159-163. baytlar.
   - Ana programa dönüş: `GET-HOUSE-DB2-INFO` bitince akış `:312` → `:313` RETURN; LGIPOL01 RETURN yapar (`lgipol01.cbl:96`).
   - Düzenek notu: COMMAREA'ya taşınan değerler SELECT'in host değişkenlerinden gelir (`lgipdb01.cbl:457-468`, `:503-504`). Düzenek bu değişkenleri doldurmazsa `INITIALIZE` değerleri (`lgipdb01.cbl:243-245`, `:284`) taşınır; göstergelerin (`IND-*`, `lgipdb01.cbl:203-207`) ilk değeri yoktur. Bu yüzden veri döndüren başarılı satırlar B4'teki eke bağlıdır.
8. **`LASTCHANGED` akışı.**
   - `lgipdb01.cbl:245` `INITIALIZE DB2-POLICY` → `DB2-LASTCHANGED` boşluk (`lgpolicy.cpy:48`, X(26)).
   - `lgipdb01.cbl:447` SELECT listesinde `LASTCHANGED` → `:459` `INTO :DB2-LASTCHANGED`.
   - `lgipdb01.cbl:503` `DB2-POLICY-COMMON` → `CA-POLICY-COMMON`: `CA-LASTCHANGED` (`lgcmarea.cpy:40`) dolar. Bu yalnızca SELECT
     başarılıysa ve `EIBCALEN ≥ 163` ise olur.
   - `01`, `90`, `98` ve `99` yollarında `CA-LASTCHANGED` girdideki gibi kalır.
   - Karakter biçimi Db2'ye bağlıdır (Kod gözlemleri 9).
   - [belge, kodla doğrulanmadı] `docs/api-design.md:23`: poliçe güncellemesi sorgulamada dönen `lastChanged` değeriyle yapılır.
     Güncellemedeki karşılaştırma 10'da koddan doğrulanacaktır. Ekleme de bu sütunu aynı biçimde `CHAR(26)` alana okur
     (`lgapdb01.cbl:316-321`); bu yüzden 08-01'in döndürdüğü değerle 09-18'in döndürdüğü değer aynı olur.
9. **Tarih ve zaman damgası biçimi.** `ISSUEDATE` ve `EXPIRYDATE` `DATE` türünde (`db2cre.jcl:165-166`), `PIC X(10)` alanlara
   okunur (`lgpolicy.cpy:46-47`). `LASTCHANGED` `TIMESTAMP` türünde (`db2cre.jcl:168`), `PIC X(26)` alana okunur
   (`lgpolicy.cpy:48`). Dönen metnin biçimi Db2 ayarına bağlıdır ve kodda yazmıyor: **belirsiz** (C). Değerin kendisi (gün, ay,
   yıl; saat, dakika, saniye) JCL'den bellidir; tahmin satırlarında belli olan kısım ayrıca yazıldı. Aynı karar
   `02-customer-inquire.md`'de `CA-DOB` için verilmişti.
10. **Dönüş kodları ve anlamları.**

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgipol01.cbl:86`, `lgipdb01.cbl:258` | Başlangıç / başarı | Evet |
    | `01` | `lgipdb01.cbl:514` | [Db2 bilgisi] `SQLCODE = 100`: satır yok | Evet |
    | `90` | `lgipdb01.cbl:517` | Başka bir SQLCODE | Evet |
    | `98` | `lgipdb01.cbl:488` | Kayıt bulundu ama COMMAREA 163 bayttan kısa | Yalnızca LGIPDB01 doğrudan çağrılırsa |
    | `99` | `lgipdb01.cbl:308` | Bilinmeyen istek kodu | Evet |
    | ABEND `LGCA` | `lgipol01.cbl:82`, `lgipdb01.cbl:254` | COMMAREA yok | — |
    | `01`, `90`, `98`, `89`, `88` | `lgipdb01.cbl:391`, `:423`, `:426`, `:582`, `:612`, `:615`, `:690`, `:714`, `:717`, `:794`, `:819`, `:822`, `:844`, `:856`, `:927`, `:939` | Başka tür kolları | Başka tür: tahmin yazılmadı |

11. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGIPOL01 **45** bayt (`lgipol01.cbl:41-46`), LGIPDB01 **87** (`lgipdb01.cbl:48-60`). `CA-ERROR-MSG`
      ikisinde de 99 bayt (`lgipol01.cbl:48-50`, `lgipdb01.cbl:62-64`).
    - LGIPDB01 `EM-VARIABLE` (63 bayt): `' CNUM='` + `EM-CUSNUM`, `' PNUM='` + `EM-POLNUM` (ikisi de baştan dolar:
      `lgipdb01.cbl:266-267`; eklemede `PNUM` hata anında boştu), `EM-SQLREQ` = `' SELECT HOUSE '` (14 karakter, 16 bayta 2 boşlukla:
      `lgipdb01.cbl:443`), `' SQLCODE='` + `EM-SQLRC` (`:999`).
    - `EIBCALEN = 0` mesajları: LGIPOL01'de `' NO COMMAREA RECEIVED'` 21 baytlık `EM-VARIABLE`'a tam sığar (`lgipol01.cbl:46`,
      `:80`). LGIPDB01'de 63 baytlık gruba taşınır (`lgipdb01.cbl:252`), ardından `EM-SQLRC` grubun son 6 baytını ezer (`:999`):
      metin + 36 boşluk + 6 bayt.
    - `WRITE-ERROR-MESSAGE` içindeki `EIBCALEN` dalları:
      - LGIPOL01'de yordam yalnızca `EIBCALEN = 0` dalından çağrılır (`lgipol01.cbl:81`); dış `IF` (`:124`) her zaman yanlış,
        `:125-137` **ölü kod**.
      - LGIPDB01'de konut yolu yordamı `lgipdb01.cbl:253` (`EIBCALEN = 0`) ve `:519` (SELECT hatası) satırlarından çağırır. `:519`
        öncesinde uzunluk kontrolü olmadığı için **`< 91` dalı (`:1016-1021`) ulaşılabilirdir**: doğrudan çağrı, `EIBCALEN`
        28-90 ve SQL hatası (09-23). Eklemede bu dala ulaşılamıyordu (`08-house-add.md` Kod gözlemleri 12).
12. **LGSTSQ kuyruk kaydı.** `08-house-add.md` Kod gözlemleri 13 ile aynı kurallar (`lgstsq.cbl:25-28`, `:48`, `:68-71`,
    `:82-83`, `:90`, `:94-111`). Bu operasyondaki uzunluklar: 45 → 50, 87 → 92 (ikisi de eksiksiz); 99 baytlık
    `CA-ERROR-MSG` → 104: COMMAREA'nın yalnızca ilk **81** baytı kayda girer, son 9 bayt `WRITE-MSG`'nin dışından okunur.
    101 baytlık mesaj bu operasyonda yok.
13. **Salt okuma; `SYNCPOINT`, `ROLLBACK`, `HANDLE` yok.** İki programda da `INSERT`, `UPDATE`, `DELETE`, `SYNCPOINT`,
    `ROLLBACK`, `HANDLE` ya da `COMMIT` geçmez (büyük/küçük harf duyarsız grep, sonuç boş; `lgipol01.cbl:67-139`,
    `lgipdb01.cbl:227-1030`). Sorgulamanın Db2'de ya da VSAM'da yan etkisi yoktur; tek yan etki hata yollarındaki LGSTSQ
    kayıtlarıdır.
14. **WORKING-STORAGE kalıcılığı.**
    - `WS-REQUIRED-CA-LEN` `VALUE +0` ile başlar (`lgipdb01.cbl:74`) ve her başarılı SELECT'te 163 artar (`:482-483`). Kodda
      sıfırlama yok. Korunmuş WORKING-STORAGE'da ikinci doğrudan çağrının eşiği 326 olur. Zincirde LGIPDB01 32500 gördüğü için
      etkisi ancak çok sayıda çağrıdan sonra görülür (`S9(4)` alanının taşması dahil).
    - LGIPDB01'in `EIBCALEN = 0` dalı `' CNUM='`, `' PNUM='` ve `' SQLCODE='` sabitlerini ezer (Kod gözlemleri 11) ve hiçbir satır
      onları geri yazmaz; dal ABEND ile biter (`lgipdb01.cbl:254`).
    - Bu dosyadaki tahminler WORKING-STORAGE'ın `VALUE` değerleriyle başladığı duruma göre yazıldı. [belge, kodla doğrulanmadı]
      `docs/adr/0004-legacy-test-harness.md:50-52`: düzenekte her durum ayrı bir süreçte çalışır, çünkü CICS her görev için
      WORKING-STORAGE'ı yeniden kurar.
15. **Kullanılmayan alanlar.** `TSAREA` (`lgipdb01.cbl:21-27`) ve `ICOM-Record` (`:222`) hiçbir satırda kullanılmaz.
    `END-POLICY-POS` (`:78`) yalnızca endowment kolunda kullanılır (`:385`, `:417`). `WS-CALEN` doldurulur ama okunmaz
    (`lgipol01.cbl:87`, `lgipdb01.cbl:259`). LGIPOL01 `LGPOLICY`'yi kopyalar (`lgipol01.cbl:55`) ama alanlarını kullanmaz.
16. **Test verisi.** Bu değerler yalnızca Db2 `db2cre.jcl` ile yüklendiyse ve sonradan değiştirilmediyse geçerlidir.
    - Konut poliçeleri: 6 (müşteri 4; `POLICY` `db2cre.jcl:873-882`, `HOUSE` `:1051-1057`), 7 (müşteri 6; `:897-906`,
      `:1068-1074`), 8 (müşteri 9; `:921-930`, `:1085-1091`). [belge, kodla doğrulanmadı] `base/Reference.md:119-121` aynı üç poliçeyi
      konut diye listeler.
    - Poliçe 7'nin tutarsızlığı: Kod gözlemleri 5. Poliçe 7'nin `COMMERCIAL` satırı yok (ticari satırlar yalnızca 10 ve 9:
      `db2cre.jcl:1184`, `:1227`).
    - Motor poliçesi 1: müşteri 2 (`db2cre.jcl:945-946`), `policyType = 'M'` (`:949`), `MOTOR` satırı var (`:1105`), `HOUSE` satırı
      yok (`HOUSE` satırları yalnızca 6, 7, 8: `:1051`, `:1068`, `:1085`).
    - Poliçe numarası 0 hiçbir yoldan atanmaz: JCL 1-10'u verir (`db2cre.jcl:777` … `:993`), kimlik değeri 1000001'den başlar
      (`:162-163`).
    - Sorgulama VSAM okumadığı için `KSDSPOLY` örnek verisindeki tutarsızlıklar (`08-house-add.md` Kod gözlemleri 15) bu dosyayı
      etkilemez.
17. **Düzenek notu: veriye bağlı SQLCODE.** Düzenekte Db2 stub'ı yalnızca `SQLCODE`'u doldurur; değeri Db2 değil test senaryosu seçer.
    B4'ün "aksi söylenmedikçe SQLCODE 0" kuralı altında, [Db2 bilgisi] ile yazılan ve veriye bağlı olan SQLCODE'lar düzenekte senaryo
    girdisi olarak ayrıca kurulmalıdır: bu dosyada 09-05, 09-06, 09-07, 09-08 ve 09-22 (`SELECT` `100`, satır yok). Bu değerin gerçek
    Db2'yle örtüşmesi [Db2 bilgisi] kabulüdür; düzenek bunu ölçmez. [belge, kodla doğrulanmadı]
    `docs/adr/0004-legacy-test-harness.md:56-58`, `:100-101`. Aynı not: `11-house-delete.md` Kod gözlemleri 16.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 09-01 | LGIPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01IHOU'`. `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000006` (poliçe 6, müşteri 4: `db2cre.jcl:873-874`). 29. bayttan sonraki girdiler herhangi bir değer (okunmaz). Db2 yalnızca `db2cre.jcl` ile yüklenmiş. [B4] Düzenek SELECT'in host değişkenlerine poliçenin Db2'deki değerlerini koyar. | `CA-RETURN-CODE = 00` | [B4] Poliçe 6'nın ortak ve konut alanları döner (alan alan aşağıdaki **"09-01 beklenen değerler"** tablosunda):<br>• Tarihler: 1 Eylül 2011 ve 31 Aralık 2011; `CA-LASTCHANGED`: 2011-09-01 22:32:42. Metin biçimleri belirsiz (Kod gözlemleri 9).<br>• `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000`.<br>• `HOUSE` + ␣×10, `005`, `01500000`, ␣×20, `␣␣␣5`, `SO211UP␣`.<br>• `CA-H-FILLER(1:5) = 'FINAL'` (159-163. baytlar).<br>`CA-REQUEST-ID`, `CA-CUSTOMER-NUM`, `CA-POLICY-NUM` ve 164. bayttan sonrası değişmez. Hata mesajı yazılmaz. Db2'de değişiklik yok. | `lgipol01.cbl:86`, `:91-94`, `:96`<br>`lgipdb01.cbl:243-245`, `:258`, `:263-264`, `:275-285`, `:441-483`, `:487`, `:494-504`, `:508`, `:312-313` | İş kuralı |
| 09-02 | 09-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000009`, `CA-POLICY-NUM = 0000000008` (poliçe 8, müşteri 9: `db2cre.jcl:921-922`). [B4] Düzenek SELECT'in host değişkenlerine poliçenin Db2'deki değerlerini koyar. | `CA-RETURN-CODE = 00` | [B4] Poliçe 8'in alanları: tarihler 12 Ağustos 2011 ve 11 Ağustos 2012 (`db2cre.jcl:923-924`), `CA-LASTCHANGED` 2011-08-12 11:11:01 (`:926`); biçimler belirsiz. `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000` (`:927-929`).<br>`FLAT` + ␣×11, `001`, `00260000`, ␣×20, `␣12b`, `E15WW` + ␣×3 (`db2cre.jcl:1086-1091`). `'FINAL'` 159-163. baytlarda. | `lgipdb01.cbl:444-476`, `:494-504`, `:508` | İş kuralı |
| 09-03 | 09-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000006`, `CA-POLICY-NUM = 0000000007` (poliçe 7, müşteri 6: `db2cre.jcl:897-898`). Poliçe 7'nin `policyType`'ı `'C'` (`db2cre.jcl:901`), ama `HOUSE` satırı var (`db2cre.jcl:1068-1074`). [B4] Düzenek SELECT'in host değişkenlerine poliçenin Db2'deki değerlerini koyar. | `CA-RETURN-CODE = 00` | `policyType` hiçbir satırda okunmaz (Kod gözlemleri 5); [B4] konut verisi döner. Tarihler 5 Ekim 2011 ve 4 Ekim 2012 (`db2cre.jcl:899-900`), `CA-LASTCHANGED` 2011-10-09 16:50:00 (`:902`); biçimler belirsiz. `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000` (`:903-905`).<br>`FARM` + ␣×11, `008`, `03750000`, `HOME FARM` + ␣×11, `␣␣␣4`, `SO529ED` + ␣ (`db2cre.jcl:1069-1074`). `'FINAL'` 159-163. baytlarda.<br>**Not:** C# tarafında `policyType` kontrolü eklenirse bu satır bilinçli bir sapma olur; karar ekibindir (Açık sorular 3). | `lgipdb01.cbl:445-456`, `:469-475`, `:478`, `:503-504` | İş kuralı |
| 09-04 | 09-01 ile aynı, ama `CA-REQUEST-ID = '01ihou'` (küçük harf). B4 09-01'deki gibi. | `CA-RETURN-CODE = 00` | [B4] 09-01 ile aynı veri. İstek kodu `WS-REQUEST-ID`'ye büyük harfle kopyalanır ve konut koluna gider. `CA-REQUEST-ID` `'01ihou'` olarak kalır. Eklemede aynı girdi `99` döner (`lgapdb01.cbl:184`; 08-11). | `lgipdb01.cbl:81`, `:275`, `:277`, `:283-285` | Arayüz (COMMAREA) kuralı |
| 09-05 | 09-01 ile aynı, ama `CA-POLICY-NUM = 0000000000` (poliçe 0 hiçbir yoldan atanmaz: Kod gözlemleri 16). | `CA-RETURN-CODE = 01` | [Db2 bilgisi] SELECT satır bulamaz ve `SQLCODE = 100` döner. COMMAREA'da `CA-RETURN-CODE` dışında hiçbir bayt değişmez: ortak ve konut alanları girdideki gibi kalır, `'FINAL'` yazılmaz. Hata mesajı yazılmaz. | `lgipdb01.cbl:478`, `:510-514`, `:522` | İş kuralı |
| 09-06 | 09-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000009` (müşteri 9 var ama poliçe 6 müşteri 4'ün: `db2cre.jcl:873-874`, `:709`). | `CA-RETURN-CODE = 01` | 09-05 ile aynı ([Db2 bilgisi] satır yok → `SQLCODE = 100`). `WHERE` hem müşteri hem poliçe numarasını ister (`lgipdb01.cbl:472-475`); poliçe numarası tek başına yetmez. | `lgipdb01.cbl:470-475`, `:512-514` | İş kuralı |
| 09-07 | 09-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000000` (müşteri 0 yok: JCL 1-10'u verir, `db2cre.jcl:437` … `:743`). | `CA-RETURN-CODE = 01` | 09-05 ile aynı ([Db2 bilgisi] satır yok → `SQLCODE = 100`). Sorgulamada `70` yolu yok (Kod gözlemleri 6): müşterinin olmaması da yalnızca "satır yok" demektir. | `lgipdb01.cbl:470-475`, `:512-514` | İş kuralı |
| 09-08 | 09-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000002`, `CA-POLICY-NUM = 0000000001` (motor poliçesi 1: `db2cre.jcl:945-946`, `:949`). İstek kodu `'01IHOU'`. Poliçe 1'in `HOUSE` satırı yok (Kod gözlemleri 16). | `CA-RETURN-CODE = 01` | 09-05 ile aynı ([Db2 bilgisi] birleşim boş → `SQLCODE = 100`). `POLICY` satırı var ama `HOUSE` ile birleşim boş. Motor verisi dönmez. | `lgipdb01.cbl:469-471`, `:512-514` | İş kuralı |
| 09-09 | 09-01 ile aynı. [B4] Düzenek konut SELECT'inde `0` ve `100` dışında bir SQLCODE üretir. | `CA-RETURN-CODE = 90` | COMMAREA'nın veri baytları değişmez, `'FINAL'` yazılmaz.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır (kayıt biçimi: Kod gözlemleri 12).<br>(1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGIPDB01'`, `' CNUM=0000000004'`, `' PNUM=0000000006'`, `' SELECT HOUSE '` + ␣×2, `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı ([CICS bilgisi] LGIPDB01 `EIBCALEN = 32500` görür: `ELSE` dalı): `01IHOU`, `90`, `0000000004`, `0000000006` ve 29-90. baytların girdideki hali. Kuyruğa yalnızca ilk 81 baytı girer. | `lgipdb01.cbl:266-267`, `:443`, `:515-520`, `:997-1013`, `:1022-1027`<br>`lgipol01.cbl:96`<br>`lgstsq.cbl:68-71`, `:90`, `:94-111` | İş kuralı |
| 09-10 | 09-01 ile aynı. [B4] Düzenek poliçe 6'nın `POLICY` satırında `brokerId` ve `payment` sütunlarını NULL yapar; SELECT başarılı olur ve `IND-BROKERID` ile `IND-PAYMENT` -1 döner. Göstergenin NULL'da -1 olması Db2 bilgisidir, kodda yazmıyor; düzenek bunu kurar. Diğer host değişkenleri B4'teki ekle poliçe 6'nın Db2 değerleri. | `CA-RETURN-CODE = 00` | `lgipdb01.cbl:494-499`'daki iki MOVE atlanır. `DB2-BROKERID` ve `DB2-PAYMENT`, `INITIALIZE DB2-POLICY`'den (`:245`) gelen 0 değerini korur: `CA-BROKERID = 0000000000`, `CA-PAYMENT = 000000`.<br>Diğer bütün alanlar 09-01 ile aynı. Bu iki değer örnek verideki değerlerle (0) aynı olduğu için NULL ile 0 COMMAREA'da ayırt edilemez. | `lgipdb01.cbl:203-205`, `:245`, `:460-462`, `:494-499`, `:503` | İş kuralı |
| 09-11 | 09-01 ile aynı, ama `CA-REQUEST-ID` = ␣×6. | `CA-RETURN-CODE = 99` | `WHEN OTHER`: SQL çalışmaz, hata mesajı yazılmaz. COMMAREA'da `CA-RETURN-CODE` dışında hiçbir bayt değişmez. | `lgipol01.cbl:79-96`<br>`lgipdb01.cbl:275`, `:307-308`, `:312-313` | Arayüz (COMMAREA) kuralı |
| 09-12 | 09-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000004` (10 haneli). B4 09-01'deki gibi. | [B3] `CA-RETURN-CODE = 00` (ön koşullu) | [B3] `DB2-CUSTOMERNUM-INT` 9 hanelik `PIC S9(9) COMP` (`lgipdb01.cbl:167`); `MOVE` (`:263`) en soldaki haneyi keser ve değer 4 olur. [B4] Poliçe 6'nın verisi döner (09-01 ile aynı).<br>`CA-CUSTOMER-NUM` `1000000004` olarak kalır. Hata mesajı yazılmaz. | `lgipdb01.cbl:167`, `:263`, `:472-473`<br>`lgcmarea.cpy:12` | Arayüz (COMMAREA) kuralı |
| 09-13 | 09-01 ile aynı, ama `CA-POLICY-NUM` = ␣×10 (sayısal alanda sayısal olmayan içerik). | **belirsiz** | **belirsiz**. `lgipdb01.cbl:264` sayısal olmayan DISPLAY içeriğini `S9(9) COMP` alana taşır; sonucun ne olacağı (sıfır, başka bir değer ya da veri istisnası/abend) derleyiciye bağlıdır, kodda tanımlı değil (C). Değer 0 olursa 09-05 (`01`) geçerli olur.<br>[belge, kodla doğrulanmadı] Yeni sistemde poliçe numarası adreste tam sayı olarak gelir (`docs/api-design.md:16`, `:53`) ve COMMAREA kuralları yalnızca GnuCOBOL'da test edilir (`docs/api-design.md:109-111`): bu girdi yalnızca GnuCOBOL'da oluşabilir. | `lgipdb01.cbl:168`, `:264`<br>`lgcmarea.cpy:35` | Arayüz (COMMAREA) kuralı |
| 09-14 | LGIPOL01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgipol01.cbl:82`'den sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGIPDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGIPOL01'`, `' NO COMMAREA RECEIVED'`). Kayıt 50 bayt. COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz). | `lgipol01.cbl:41-46`, `:79-83`, `:110-122`, `:124` | Arayüz (COMMAREA) kuralı |
| 09-15 | LGIPOL01 çağrılır. `EIBCALEN` 28 ile 162 arasında (sınır değerler: `28` ve `162`). İlk 28 bayt 09-01 ile aynı (müşteri 4, poliçe 6). B4 09-01'deki gibi. | [B1] `CA-RETURN-CODE = 00` (ön koşullu). B1 sağlanmazsa belirsiz: COMMAREA dışına yazmanın sonucu ortama bağlı. | Kesin olanlar:<br>• LGIPOL01'de uzunluk kontrolü yok; [CICS bilgisi] LGIPDB01 `EIBCALEN = 32500` görür ve 163'lük kontrol geçer. **Zincirde `98` dönmez**; LGIPDB01 doğrudan çağrılsa `98` dönerdi (09-20).<br>• Anahtar alanlar (9-28. baytlar) gelen COMMAREA'nın içinde.<br>[B1] ve [B4] ile (ön koşullu): 09-01'deki değerler 29-163. baytlara yazılır; `EIBCALEN`'in ötesine düşen kısım çağıranın tamponuna gider ve çağıran onu görür. `'FINAL'` 159-163. baytlarda. | `lgipol01.cbl:79-96`<br>`lgipdb01.cbl:482-483`, `:487`, `:503-504`, `:508` | Arayüz (COMMAREA) kuralı |
| 09-16 | LGIPOL01 çağrılır. `EIBCALEN` 1 ile 27 arasında. | **belirsiz** | **belirsiz**. Kesin olan: LGIPOL01 `98` yazmaz (uzunluk kontrolü yok) ve `'00'`'ı yazar (`lgipol01.cbl:86`; `EIBCALEN` 1-7 iken bu yazma COMMAREA'nın dışına düşer). LGIPDB01 anahtar alanları (9-28. baytlar) kısmen gelen COMMAREA'nın dışından okur; B1 ile bile boşluklu sayısal alanın `COMP`'a taşınması 09-13'teki belirsizliğe düşer. | `lgipol01.cbl:86`, `:91-94`<br>`lgipdb01.cbl:263-264` | Arayüz (COMMAREA) kuralı |
| 09-17 | LGIPOL01 çağrılır. `EIBCALEN = 32500`. LGIPDB01'e yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. LINK'te `RESP`/`NOHANDLE` yok, programlarda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor (C). | `lgipol01.cbl:91-94` | Arayüz (COMMAREA) kuralı |
| 09-18 | 08-01 bu testten önce çalıştırılmış ve `00` dönmüş; arada bu poliçeye dokunan başka bir test yok. [B6] 08-01'in ürettiği numara P = `0001000001`. LGIPOL01 çağrılır: `EIBCALEN = 32500`, `CA-REQUEST-ID = '01IHOU'`, `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0001000001`. [B4] Düzenek SELECT'in host değişkenlerine 08-01'in eklediği satırın değerlerini koyar. | `CA-RETURN-CODE = 00` | [B4] 08-01'in girdileri geri döner (`08-house-add.md`, "08-01 girdi ve beklenen değerler"): tarihler 1 Eylül 2011 ve 31 Aralık 2011 (biçim belirsiz), `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000`, `HOUSE` + ␣×10, `005`, `01500000`, ␣×20, `␣␣␣5`, `SO211UP␣`, `'FINAL'`.<br>`CA-LASTCHANGED` = 08-01'in döndürdüğü değer: iki program da aynı sütunu aynı `CHAR(26)` türüne okur (Kod gözlemleri 8).<br>Test sırası: "Test sırası ve veri bağımlılıkları" bölümü (08-01 → 09-18 → 11-18). | `lgipdb01.cbl:444-476`, `:503-504`, `:508`<br>`lgapdb01.cbl:268-288`, `:308-321`, `:409-425` | İş kuralı |
| 09-19 | **LGIPDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgipdb01.cbl:254`'ten sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 87 baytlık `ERROR-MSG` (tarih, saat, `' LGIPDB01'`, `' NO COMMAREA RECEIVED'` + ␣×36 + 6 baytlık `EM-SQLRC`). [B2] `EM-SQLRC = +00000` (ön koşullu; SQLCA'nın kaynağı `base/` altında yok) [kod] `lgipdb01.cbl:999`. COMMAREA dökümü gönderilmez.<br>LGIPOL01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 2). | `lgipdb01.cbl:53-60`, `:251-255`, `:999`, `:1010-1015` | Arayüz (COMMAREA) kuralı |
| 09-20 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 162 arasında (sınır değer: `162`). İlk 28 bayt 09-01 ile aynı (müşteri 4, poliçe 6; kayıt var). [B4] SELECT `SQLCODE = 0`. | `CA-RETURN-CODE = 98` | SELECT başarılı olur, ardından uzunluk kontrolü `98` ile RETURN yapar (Kod gözlemleri 3). Veri taşınmaz, `'FINAL'` yazılmaz: COMMAREA'da `CA-RETURN-CODE` dışında hiçbir bayt değişmez. Hata mesajı yazılmaz. | `lgipdb01.cbl:444-483`, `:487-489` | Arayüz (COMMAREA) kuralı |
| 09-21 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN = 163` (kabul edilen en küçük uzunluk). İlk 28 bayt 09-01 ile aynı. B4 09-01'deki gibi. | `CA-RETURN-CODE = 00` | `163 < 163` yanlış, kontrol geçer. [B4] 09-01'deki veriler 29-158. baytlara, `'FINAL'` 159-163. baytlara yazılır; bütün yazmalar gelen COMMAREA'nın içinde kalır. | `lgipdb01.cbl:482-483`, `:487`, `:503-504`, `:508` | Arayüz (COMMAREA) kuralı |
| 09-22 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 162 arasında. `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000000` (kayıt yok). `CA-REQUEST-ID = '01IHOU'`. | `CA-RETURN-CODE = 01` (`98` değil) | Uzunluk kontrolü yalnızca başarılı SELECT'ten sonra yapılır. [Db2 bilgisi] Kayıt yoksa `SQLCODE = 100` döner ve `lgipdb01.cbl:482-489`'a hiç ulaşılmaz (Kod gözlemleri 3). Veri değişmez, mesaj yazılmaz. | `lgipdb01.cbl:478`, `:510-514` | Arayüz (COMMAREA) kuralı |
| 09-23 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (sınır değer: `90`). İlk 28 bayt 09-01 ile aynı. [B4] Düzenek konut SELECT'inde `0` ve `100` dışında bir SQLCODE üretir. | `CA-RETURN-CODE = 90` | Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: 09-09 ile aynı.<br>(2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır (Kod gözlemleri 11). | `lgipdb01.cbl:515-520`, `:1015-1021` | Arayüz (COMMAREA) kuralı |

## 09-01 beklenen değerler

COMMAREA'nın dönüşte beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir. Değerlerin kaynağı `db2cre.jcl`'deki poliçe 6
(`POLICY` `:861-882`, `HOUSE` `:1042-1057`).

| COMMAREA alanı | Beklenen | Değerin kaynağı (`db2cre.jcl`) | Alanı dolduran COBOL satırı |
|---|---|---|---|
| `CA-REQUEST-ID` X(6) | `01IHOU` (değişmez) | — | Yazılmıyor (`lgipdb01.cbl:275` yalnızca okur) |
| `CA-RETURN-CODE` 9(2) | `00` | — | `lgipol01.cbl:86`, `lgipdb01.cbl:258` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000004` (değişmez) | `:874` | Yazılmıyor |
| `CA-POLICY-NUM` 9(10) | `0000000006` (değişmez) | `:873` | Yazılmıyor |
| `CA-ISSUE-DATE` X(10) | 1 Eylül 2011; metin biçimi belirsiz (JCL'de `'2011-09-01'`) | `:875` (sütun `DATE`: `:165`) | `lgipdb01.cbl:457`, `:503` |
| `CA-EXPIRY-DATE` X(10) | 31 Aralık 2011; metin biçimi belirsiz (JCL'de `'2011-12-31'`) | `:876` (sütun `DATE`: `:166`) | `lgipdb01.cbl:458`, `:503` |
| `CA-LASTCHANGED` X(26) | 2011-09-01 22:32:42; 26 baytlık metin biçimi belirsiz | `:878` (sütun `TIMESTAMP`: `:168`) | `lgipdb01.cbl:459`, `:503` |
| `CA-BROKERID` 9(10) | `0000000000` | `:879` (sütun `INTEGER`: `:169`) | `lgipdb01.cbl:460`, `:494-496`, `:503` |
| `CA-BROKERSREF` X(10) | ␣×10 (JCL'de `''`) | `:880` (sütun `CHAR(10)`: `:170`) | `lgipdb01.cbl:461`, `:503` |
| `CA-PAYMENT` 9(6) | `000000` | `:881` (sütun `INTEGER`: `:171`) | `lgipdb01.cbl:462`, `:497-499`, `:503` |
| `CA-H-PROPERTY-TYPE` X(15) | `HOUSE` + ␣×10 | `:1052` (sütun `CHAR(15)`: `:242`) | `lgipdb01.cbl:463`, `:504` |
| `CA-H-BEDROOMS` 9(3) | `005` | `:1053` (sütun `SMALLINT`: `:243`) | `lgipdb01.cbl:464`, `:500`, `:504` |
| `CA-H-VALUE` 9(8) | `01500000` | `:1054` (sütun `INTEGER`: `:244`) | `lgipdb01.cbl:465`, `:501`, `:504` |
| `CA-H-HOUSE-NAME` X(20) | ␣×20 (JCL'de `' '`) | `:1055` (sütun `CHAR(20)`: `:245`) | `lgipdb01.cbl:466`, `:504` |
| `CA-H-HOUSE-NUMBER` X(4) | `␣␣␣5` | `:1056` (sütun `CHAR(4)`: `:246`) | `lgipdb01.cbl:467`, `:504` |
| `CA-H-POSTCODE` X(8) | `SO211UP` + ␣×1 (2. karakter O harfi) | `:1057` (sütun `CHAR(8)`: `:247`) | `lgipdb01.cbl:468`, `:504` |
| `CA-H-FILLER(1:5)` | `FINAL` | — | `lgipdb01.cbl:508` |
| `CA-H-FILLER(6:)` | Değişmez | — | Yazılmıyor |

Db2'de, VSAM'da ve kuyruklarda değişiklik yok (Kod gözlemleri 13).

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları. "Başka tür" maddelerine tahmin yazılmadı; bu
maddeler, dalın alınmadığı bir konut testine bağlandı.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| IP-01 | `lgipol01.cbl:79-83` `IF EIBCALEN = 0` → ABEND `LGCA` | 09-14 | |
| IP-02 | `lgipol01.cbl:86` `'00'` → `CA-RETURN-CODE` | 09-01, 09-16 | 09-16: 1-7 baytta alan dışına yazma |
| IP-03 | `lgipol01.cbl:72-96` uzunluk kontrolü yok | 09-15, 09-16 | Kod gözlemleri 2 |
| IP-04 | `lgipol01.cbl:87-88` `WS-CALEN`, adres | 09-01 | Satır çalışır; etkisi yok (Kod gözlemleri 15) |
| IP-05 | `lgipol01.cbl:91-94` LINK LGIPDB01 `Length(32500)`, `RESP` yok | 09-01, 09-15, 09-17 | |
| IP-06 | `lgipol01.cbl:96` RETURN; LGIPDB01'in dönüş kodu kontrol edilmez | 09-01, 09-05, 09-09 | |
| IP-07 | `lgipol01.cbl:107-122` `WRITE-ERROR-MESSAGE`, 45 baytlık `ERROR-MSG` | 09-14 | |
| IP-08 | `lgipol01.cbl:124-138` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 09-14 | Dış `IF` yanlış. İç dallar (`:125-137`) **kapsam dışı: ulaşılamaz**; yordam yalnızca `:81`'den çağrılıyor (Kod gözlemleri 11) |
| IP-09 | `CA-REQUEST-ID` LGIPOL01'de okunmaz | 09-11 | Kod gözlemleri 4 |
| ID-01 | `lgipdb01.cbl:243-245` `INITIALIZE` (tamsayılar, `DB2-POLICY`) | 09-01, 09-10 | 09-10: NULL'da 0 kalması |
| ID-02 | `lgipdb01.cbl:251-255` `IF EIBCALEN = 0` → ABEND `LGCA` | 09-19 | Yalnızca doğrudan çağrı |
| ID-03 | `lgipdb01.cbl:258` `'00'` → `CA-RETURN-CODE` | 09-01 | |
| ID-04 | `lgipdb01.cbl:263-264` müşteri/poliçe numarası → `S9(9) COMP` | 09-01, 09-12, 09-13 | 09-12 B3; 09-13 belirsiz |
| ID-05 | `lgipdb01.cbl:266-267` `EM-CUSNUM`, `EM-POLNUM` | 09-09 | |
| ID-06 | `lgipdb01.cbl:275` `UPPER-CASE` → `WS-REQUEST-ID` | 09-04 | Kod gözlemleri 4 |
| ID-07a | `lgipdb01.cbl:279-281` `WHEN '01IEND'` | 09-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| ID-07b | `lgipdb01.cbl:283-285` `WHEN '01IHOU'` → `INITIALIZE DB2-HOUSE`, `GET-HOUSE-DB2-INFO` | 09-01 | |
| ID-07c | `lgipdb01.cbl:287-289` `WHEN '01IMOT'` | 09-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| ID-07d | `lgipdb01.cbl:291-293` `WHEN '01ICOM'` | 09-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| ID-07e | `lgipdb01.cbl:295-297` `WHEN '02ICOM'` | 09-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| ID-07f | `lgipdb01.cbl:299-301` `WHEN '03ICOM'` | 09-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| ID-07g | `lgipdb01.cbl:303-305` `WHEN '05ICOM'` | 09-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| ID-07h | `lgipdb01.cbl:307-308` `WHEN OTHER` → `99` | 09-11 | Hemen RETURN yok; `:312`'ye düşer |
| ID-08 | `lgipdb01.cbl:312-313` `End-Program`, RETURN | 09-01, 09-11 | |
| ID-09 | `lgipdb01.cbl:443` `EM-SQLREQ` = `' SELECT HOUSE '` | 09-09 | |
| ID-10 | `lgipdb01.cbl:444-476` SELECT `POLICY,HOUSE`; `WHERE`'de `policyType` yok | 09-01, 09-02, 09-03, 09-06, 09-08 | Kod gözlemleri 5 |
| ID-11 | `lgipdb01.cbl:478` `IF SQLCODE = 0` / `:510` `ELSE` | 09-01 (doğru), 09-05 (yanlış) | |
| ID-12 | `lgipdb01.cbl:482-483` gerekli uzunluk 163 | 09-20, 09-21 | |
| ID-13 | `lgipdb01.cbl:487-489` `IF EIBCALEN < 163` → `98`, RETURN | 09-20 (reddedilir), 09-21 (kabul), 09-15 (zincirde 32500) | Zincirde hiç alınmaz (Kod gözlemleri 2, 3) |
| ID-14 | `lgipdb01.cbl:494-496` `IF IND-BROKERID ≠ -1` | 09-01 (doğru), 09-10 (yanlış) | |
| ID-15 | `lgipdb01.cbl:497-499` `IF IND-PAYMENT ≠ -1` | 09-01 (doğru), 09-10 (yanlış) | |
| ID-16 | `lgipdb01.cbl:461` `IND-BROKERSREF` kontrol edilmez | 09-10 | NULL `brokersReference` için tahmin yok; yalnızca Kod gözlemleri 7 (C) |
| ID-17 | `lgipdb01.cbl:500-501` `SMALLINT`/`INTEGER` → 9(3)/9(8) | 09-01 | Aralık dışı değer eklemeden gelemez (Kod gözlemleri 7) |
| ID-18 | `lgipdb01.cbl:503-504` `DB2-POLICY-COMMON`, `DB2-HOUSE` → COMMAREA | 09-01, 09-18 | `LASTCHANGED` akışı: Kod gözlemleri 8 |
| ID-19 | `lgipdb01.cbl:508` `'FINAL'` → `CA-H-FILLER(1:5)` | 09-01, 09-21 | |
| ID-20 | `lgipdb01.cbl:512-514` `SQLCODE = 100` → `01` | 09-05, 09-06, 09-07, 09-08, 09-22 | |
| ID-21 | `lgipdb01.cbl:515-520` başka SQLCODE → `90`, mesaj | 09-09, 09-23 | |
| ID-22 | `lgipdb01.cbl:997-1013` `WRITE-ERROR-MESSAGE`, 87 baytlık `ERROR-MSG` | 09-09, 09-19 | |
| ID-23 | `lgipdb01.cbl:1015-1029` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 09-09 (`ELSE`), 09-23 (`< 91`), 09-19 (dış `IF` yanlış) | |
| ID-24 | `lgipdb01.cbl:21-27`, `:222`, `:78` kullanılmayan alanlar | 09-01 | Kod gözlemleri 15 |
| ID-25 | `lgipdb01.cbl:74`, `:482-483` `WS-REQUIRED-CA-LEN` kalıcılığı | 09-20, 09-21 | Tahminler `VALUE` başlangıcına göre (Kod gözlemleri 14) |
| ID-BT | `lgipdb01.cbl:89-149` ticari cursor'lar; `:327-432` ENDOW; `:529-621` MOTOR; `:627-990` COMMERCIAL 1/2/3/5 (`89`, `88`, `PERFORM END-PROGRAM`) | 09-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı. Cursor'lar konut yolunda açılmaz |
| SQ-01 | `lgstsq.cbl:68-71` LINK yolu | 09-09, 09-14, 09-19, 09-23 | |
| SQ-02 | `lgstsq.cbl:72-80` `ELSE` (terminalden RECEIVE) | 09-09 (dal alınmaz) | **Kapsam dışı: bu zincirden ulaşılamaz.** İki program da LGSTSQ'yu LINK ile çağırır |
| SQ-03 | `lgstsq.cbl:82-88` `'Q='` kontrolü | 09-09 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** Mesajlar tarih ya da `'COMMAREA='` ile başlar |
| SQ-04 | `lgstsq.cbl:90`, `:94-99` +5, WRITEQ TD `CSMT`, RESP kontrol edilmez | 09-09 | |
| SQ-05 | `lgstsq.cbl:105-111` WRITEQ TS `GENAERRS`, RESP kontrol edilmez | 09-09 | |
| SQ-06 | `lgstsq.cbl:113-119` `IF WS-FLAG = 'R'` → SEND TEXT | 09-09 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** `'R'` yalnızca SQ-02 yolunda atanır |
| SQ-07 | `lgstsq.cbl:121-122` RETURN | 09-09 | |
| SQ-08 | Kayıt uzunlukları 45 → 50, 87 → 92, 99 → 104 (81 bayt) | 09-09, 09-14, 09-19 | Kod gözlemleri 12 |
| N-01 | `policyType` okunmaz; poliçe 7 konut olarak döner | 09-03 | Kod gözlemleri 5; C# için bilinçli sapma kararı ekibin |
| N-02 | Sorgulamada `70` yok | 09-07 | Kod gözlemleri 6 |
| N-03 | LINK'te `RESP` yok | 09-17 | |
| N-04 | Uzunluk kontrolü SELECT'ten sonra | 09-20, 09-22, 09-23 | Kod gözlemleri 3 |
| N-05 | Zincirde uzunluk kontrolü yok; başarı yolunda 29-163. baytlara yazılır | 09-15, 09-16 | Kod gözlemleri 2 |
| N-06 | İstek kodu büyük/küçük harfe duyarsız (eklemenin tersi) | 09-04 | Kod gözlemleri 4 |
| N-07 | 29. bayttan sonraki girdiler okunmaz; veri yalnızca başarı yolunda ezilir | 09-05, 09-09, 09-20 | Kod gözlemleri 1 |
| N-08 | `LASTCHANGED` akışı ve güncellemeyle bağı | 09-01, 09-18 | Kod gözlemleri 8 |
| N-09 | VSAM okunmaz | 09-01 | Kod gözlemleri 16 |
| N-10 | Salt okuma, `SYNCPOINT` yok | 09-01 | Kod gözlemleri 13 |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları"). B4 satırlarında son sütun iki durumu ayrı yazar: **düzenekte** (senaryo değeri vermez ya da host değişkenine yazmazsa; B4'ün varsayılanı `0` / `NORMAL`) ve **gerçek Db2/VSAM'de** (sonucu gerçek sistem verirse).

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 09-01 | B4 | `00`; poliçe 6'nın verisi ve `'FINAL'` | **Düzenekte:** host değişkeni eki sağlanmazsa dönüş kodu yine `00` (SQLCODE `0` → `lgipdb01.cbl:478`), ama `INITIALIZE` değerleri taşınır (`lgipdb01.cbl:243-245`, `:284`) ve göstergelerin ilk değeri olmadığı için (`:203-207`) dönen veri belirsiz; senaryo SQLCODE'u `100` verirse `01`, başka bir değer verirse `90`. **Gerçek Db2'de:** SELECT `0` dönerse değerler satırdan gelir ve tahmin aynıdır (metin biçimleri belirsiz: Kod gözlemleri 9); başka bir SQLCODE'da `01` ya da `90` |
| 09-02 | B4 | `00`; poliçe 8'in verisi | 09-01 ile aynı (poliçe 8: `db2cre.jcl:921-930`, `:1085-1091`) |
| 09-03 | B4 | `00`; poliçe 7'nin konut verisi | 09-01 ile aynı (poliçe 7: `db2cre.jcl:897-906`, `:1068-1074`) |
| 09-04 | B4 | `00`; 09-01 ile aynı veri, `CA-REQUEST-ID` küçük harf kalır | 09-01 ile aynı; büyük/küçük harf davranışı yine kesin |
| 09-09 | B4 | Girdi düzenekle kurulur; `90` ve iki mesaj | **Düzenekte:** senaryo SQLCODE vermezse SELECT `0` döner, 09-01 yolu (`00`). **Gerçek Db2'de:** girdi kurulamaz; örnek veride SELECT'i hataya düşürecek bir veri yolu yok |
| 09-10 | B4 | `00`; `CA-BROKERID = 0000000000`, `CA-PAYMENT = 000000` (`INITIALIZE`'dan) | **Düzenekte:** göstergeler kurulmazsa ilk değerleri olmadığı için (`lgipdb01.cbl:203-207`) MOVE'ların çalışıp çalışmayacağı belirsiz. **Gerçek Db2'de:** girdi kurulamaz; örnek veride bu sütunlar NULL değil (`db2cre.jcl:879`, `:881`): gösterge -1 olmaz, MOVE çalışır ve sütunun Db2'deki değeri döner |
| 09-12 | B3, B4 | `00`; poliçe 6'nın verisi | B3 yoksa dönüş kodu ve veri belirsiz: kesilmeyen değerin `S9(9) COMP`'a nasıl taşınacağı derleyiciye bağlı. B4 için 09-01'in son sütunu |
| 09-14 | B8 | Düzenekte ABEND `LGCA` mesajı ve `GOBACK`; dönüş kodu yok, LGIPDB01 çağrılmaz, tek LGSTSQ LINK | B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır. Yan etki aynı: LGSTSQ LINK'i ABEND'den önce yapılır (`lgipol01.cbl:81-82`) |
| 09-15 | B1, B4 | `00`; veri 29-163. baytlara, çağıranın tamponuna yazılır | B1 yoksa COMMAREA dışına yazmanın sonucu belirsiz; zincirde `98` dönmemesi yine kesin ([CICS bilgisi]). B4 için 09-01'in son sütunu |
| 09-18 | B4, B6 ve sıra (08-01 önce, arada bu poliçeye dokunan test yok) | `00`; 08-01'in girdileri ve 08-01'in döndürdüğü `CA-LASTCHANGED` | B6 yoksa poliçe numarası 08-01'in döndürdüğü değerle değiştirilir, tahmin aynen geçerli. 08-01 önce çalışmazsa ya da başarısızsa: **gerçek Db2'de** [Db2 bilgisi] `01` (09-05 yolu); **düzenekte** senaryo `100` vermezse SELECT `0` döner ve 09-01'in son sütunu geçerli. B4 için 09-01'in son sütunu |
| 09-19 | B2, B8 | `EM-SQLRC = +00000`; düzenekte ABEND `LGCA` mesajı ve `GOBACK` | B2 yoksa `EM-SQLRC` belirsiz. B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır; mesaj ABEND'den önce yazılır (`lgipdb01.cbl:253-254`) |
| 09-20 | B4 | SELECT `0`; `98`, veri taşınmaz | **Düzenekte:** senaryo `100` verirse `01` (09-22 yolu), başka bir SQLCODE verirse `90` ve iki mesaj (09-23 yolu). **Gerçek Db2'de:** poliçe 6 örnek veride var; sonuç SELECT'in SQLCODE'una bağlı, aynı üç yol |
| 09-21 | B4 | `00`; veri ve `'FINAL'` gelen alanın içinde | 09-01 ile aynı |
| 09-23 | B4 | Girdi düzenekle kurulur; `90`, `< 91` dalı | **Düzenekte:** senaryo SQLCODE vermezse SELECT `0` döner, 09-20 yolu (`98`). **Gerçek Db2'de:** girdi kurulamaz (SELECT'i hataya düşürecek veri yolu yok) |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 09-13 | Dönüş kodu, beklenen veri | Sayısal olmayan DISPLAY içeriğinin `COMP` alana taşınmasının sonucu derleyiciye bağlı (C). |
| 09-16 | Dönüş kodu, beklenen veri | Anahtar alanların bir kısmı COMMAREA'nın dışında; B1 ile bile boşluk içerik 09-13'teki belirsizliğe düşer. Kesin kısım: LGIPOL01 `98` yazmaz. |
| 09-17 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil (C). |
| 09-01, 09-02, 09-03, 09-04, 09-10, 09-12, 09-15, 09-18, 09-21 | Beklenen veri: yalnızca `CA-ISSUE-DATE`, `CA-EXPIRY-DATE` ve `CA-LASTCHANGED`'in metin biçimi (alan uzunlukları 10 ve 26 bayt kesin) | `DATE` ve `TIMESTAMP` değerlerinin karaktere dönüşüm biçimi Db2'ye bağlı (C; Kod gözlemleri 9). Değerlerin kendisi (tarih ve saat) JCL'den bellidir; bu satırların diğer hücreleri kesin ya da ön koşulludur. 09-18'de `CA-LASTCHANGED`'in 08-01'in döndürdüğü değere eşit olması biçimden bağımsızdır. |
| 09-09, 09-23 | Yan etki: `CA-ERROR-MSG` kuyruk kaydının son 9 baytı | 104 baytlık kaydın son 9 baytı `WRITE-MSG`'nin dışından okunur; içeriği WORKING-STORAGE yerleşimine bağlı (Kod gözlemleri 12). Mesajın kendisi kesindir. |

## Test sırası ve veri bağımlılıkları

Öneri; sıra ve veri sıfırlama kararı ekibindir. Bu bölüm `08-house-add.md`, `09-house-inquire.md`, `10-house-update.md` ve
`11-house-delete.md`'de aynıdır. Bağımlılıklar satırlardaki "Sıra" ön koşullarından ve testlerin değiştirdiği örnek veriden
çıkarıldı. Gerçek veri (ya da veri durumunu tutan bir düzenek) için geçerlidir; düzenek her satırın veri durumunu B4 ile kendisi
kurarsa veri bağımlılıkları kalkar, "Sıra" satırları ise önceki testin döndürdüğü değeri (numara, `CA-LASTCHANGED`) girdi olarak
almaya devam eder.

**Zorunlu sıralar**

| Sıra | Neden |
|---|---|
| 08-01 → 09-18 → 11-18 | 09-18, 08-01'in eklediği poliçeyi sorgular (B6: P = `0001000001`); 11-18 aynı poliçeyi siler. 11-18'den sonra 09-18 `01` döner. |
| 09-01 → 10-01 → 10-04 | 10-01, 09-01'in döndürdüğü `CA-LASTCHANGED` ile poliçe 6'yı günceller. 10-04 aynı (artık eski) değerle `02` yoluna düşmek için 10-01'den sonra çalışır. |
| 09-03 → 10-02 | 10-02, 09-03'ün döndürdüğü `CA-LASTCHANGED` ile poliçe 7'yi günceller. |
| 09-01 → 10-18, 10-19, 10-21, 10-27 | 10-01 gibi 09-01'in zaman damgasını kullanırlar. 10-01, 10-18, 10-19, 10-21 ve 10-27 aynı veride birbirini dışlar: biri poliçe 6'nın `lastChanged`'ini değiştirince diğerleri `02` yoluna düşer. Her birinden önce 09-01 yeniden çalıştırılmalı ya da veri sıfırlanmalı. |
| 11-01 → 11-07 | 11-07, poliçe 6'nın silinmiş olmasına dayanır. |

**Örnek veriyi değiştiren testler**

| Poliçe | Değiştiren ya da silen testler | Etkilenen testler |
|---|---|---|
| 6 (müşteri 4) | Db2'yi güncelleyenler: 10-01, 10-09, 10-14, 10-15, 10-16, 10-17, 10-18, 10-19, 10-21, 10-27 (ABEND ya da görev sonu olanlarda kalıcılık C). Yalnızca VSAM'ı yeniden yazanlar: 10-03, 10-04, 10-10, 10-13, 10-32. Silenler: 11-01, 11-03, 11-09, 11-11, 11-16 (Db2), 11-24 (VSAM). | Poliçe 6'nın JCL değerlerine dayanan 09 satırları (09-01, 09-04, 09-09, 09-10, 09-12, 09-15, 09-20, 09-21, 09-23), 10'un 10-01 girdisine dayanan satırları, 11-01'e dayanan 11 satırları. |
| 7 (müşteri 6) | 10-02 (Db2 ve VSAM), 11-02 (silme). | 09-03 (10-02'den sonra veri değişir, 11-02'den sonra `01`), 10-02 (11-02'den sonra satır yok). |
| 1 (motor, müşteri 2) | 11-06: `POLICY` 1 ve cascade ile `MOTOR` 1 silinir ([Db2 bilgisi]); VSAM'daki `M00000000020000000001` kalır. | 09-08 (yine `01`, ama artık `POLICY` satırı da yok), 10-07 (FETCH satır bulamaz: 10-05 yolu, `81`), motor dosyaları 04-07. |
| Yeni poliçe P | Her başarılı 08 eklemesi kimlik sayacını ilerletir (B6). | P = `0001000001` yalnızca taze yüklemeden sonraki ilk başarılı eklemede geçerlidir: 08'in numara döndüren satırları, 09-18, 11-18. |

**Önerilen sıra ve veri sıfırlama noktaları**

1. Taze yükleme (`db2cre.jcl`, `ksdspoly.txt`); 08-01, hemen ardından 09-18 ve 11-18.
2. Sıfırlama. 08'in diğer satırları; numara döndüren her satırdan önce taze yükleme ya da P'nin 08'in döndürdüğü numarayla
   düzeltilmesi.
3. Sıfırlama. 09'un bütün satırları (09-18 hariç); veri değiştirmezler.
4. 10: 09-01 → 10-01 → 10-04 ve 09-03 → 10-02. Poliçe 6'yı değiştiren diğer her 10 satırından önce sıfırlama ya da 09-01'in
   yeniden çalıştırılması.
5. Sıfırlama. 11 en sonda: 11-01 → 11-07; poliçe 6'yı silen diğer satırlardan önce sıfırlama. 11-06 motor poliçesi 1'i sildiği
   için 04-07'deki motor testlerinden sonra ya da ayrı bir yüklemeyle.

## Açık sorular

Ekip kararı gerektiren, tahminleri etkileyen sorular. Tahminler bu soruların cevabını varsaymaz; varsayım gereken yerde ön koşul
kodu kullanıldı.

1. **B4'teki host değişkeni eki ile "stub sonuç uydurmaz" kuralı.** [belge, kodla doğrulanmadı] ADR 0004'e göre stub dönüş kodu atamaz
   ve alan doldurmaz (`docs/adr/0004-legacy-test-harness.md:47-49`); veri bekleyen `SELECT … INTO` ve `FETCH` için stub'ın host
   değişkenlerine ne koyacağı açık sorudur ve bu kuralla çelişir (`:135-138`). Bu dosyada B4'teki ek SELECT'in host değişkenlerini
   poliçenin Db2'deki değerleriyle doldurur ve veri döndüren başarılı satırlar ona dayanır: 09-01, 09-02, 09-03, 09-04, 09-10, 09-12,
   09-15, 09-18, 09-21. Ek kabul edilmezse bu satırlarda "Ön koşullu satırlar" tablosundaki **düzenekte** durumu geçerli olur (Kod
   gözlemleri 7, düzenek notu).
2. **Zincir düzeneği mi, program başına bölme mi?** Zincir satırları iş katmanı, veri katmanı ve VSAM programının birlikte çalıştığını
   varsayar; dönüş kodunun çağırana ulaşması, alt programın `EIBCALEN`'i ([CICS bilgisi]) ve LGSTSQ kayıtları buna dayanır.
   [belge, kodla doğrulanmadı] ADR 0004'te iş katmanı testlerinde veri katmanının tamamı stub ile değişir
   (`docs/adr/0004-legacy-test-harness.md:59-60`) ve stub'ın arkasındaki sonuçlar ölçülmez (`:98-99`, `:103-106`). Bu durumda zincir
   satırları ya her program için ayrı satırlara bölünmeli (alt programın davranışı doğrudan çağrı satırlarıyla, üst programınki stub'a
   geçen COMMAREA ile ölçülür) ya da zinciri birlikte çalıştıran bir düzenek kurulmalıdır. Karar ekibindir.
3. **`policyType` kontrolü: bilinçli sapma adayı.** Sorgulama, güncelleme ve silme zincirlerinin hiçbiri `policyType`'a bakmaz; başka
   türden poliçeler konut istek koduyla sorgulanır, güncellenir ve silinir: 09-03, 10-02, 11-02 (poliçe 7, `policyType = 'C'`) ve
   11-06 (motor poliçesi 1). C# tarafında tür kontrolü eklenirse bu satırlar bilinçli sapma olarak işaretlenmelidir; tahminler
   COBOL'un davranışını yazar. Karar ekibindir.
