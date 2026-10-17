# 13 — Hayat sigortası sorgula (Inquire Endowment Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Hayat sigortası (endowment) sorgula: müşteri numarası ve poliçe numarasıyla Db2 `POLICY` ve `ENDOWMENT` tablolarının birleşiminden tek satır okunur. Poliçe türü COMMAREA'daki istek kodundan (`01IEND`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGIPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGIPDB01` (Db2 `SELECT`). Hata yollarında ikisi de `LGSTSQ`'ya LINK eder. Zincir VSAM'a hiç dokunmaz. Programlar dört poliçe türüne de hizmet eder; yalnızca hayat sigortası (`01IEND`) kolu ve ortak kısımlar okundu. |
| Okunan COBOL dosyaları | `base/src/lgipol01.cbl`, `base/src/lgipdb01.cbl`, `base/src/lgstsq.cbl`, `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy` |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` ve `ENDOWMENT` tanımları (`:161-178`, `:203-217`), müşteri 3 ve 8 (`:505`, `:675`), poliçe 4 ve 5 ile hayat sigortası 4 ve 5'in verisi (`:849-858`, `:825-834`, `:1033-1040`, `:1014-1021`). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `docs/adr/0004-legacy-test-harness.md` (yalnızca Kod gözlemleri 10) |
| Hazırlayan | Oğuz (taslak: Claude Code) |
| Tarih | 2026-10-17 |
| Kaynak commit (`git rev-parse HEAD`) | `13d0e97c05c6a63cbb6ee9ad0af44cbce5e06d66` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge]**: `base/` altındaki ya da `docs/` altındaki açıklama metinleri.
- Etiketsiz kaynaklar [kod]'dur.

Ön koşul kodları test düzeneğinin vereceği kararlardır (kodlar `05-motor-inquire.md` ile aynıdır). Bir kodu taşıyan tahmin
**kesin değil, ön koşulludur**: ön koşul sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması, bir sütunun NULL olması ya da belirli bir değer taşıması) kendisi kurar. |

(B5 ve B6 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası, hayat sigortası görünümü** (`lgcmarea.cpy:10-13`, `:34-44`, `:46-54`). Programlar bu düzeni kullanır (`lgipol01.cbl:62-63`, `lgipdb01.cbl:217-220`).
   Hayat sigortası sorgusunda alanlar:

   | Bayt | Alan | PIC | Satır |
   |---|---|---|---|
   | 1-6 | `CA-REQUEST-ID` | X(6) | `lgcmarea.cpy:10` |
   | 7-8 | `CA-RETURN-CODE` | 9(2) | `:11` |
   | 9-18 | `CA-CUSTOMER-NUM` | 9(10) | `:12` |
   | 19-28 | `CA-POLICY-NUM` | 9(10) | `:35` |
   | 29-38 | `CA-ISSUE-DATE` | X(10) | `:38` |
   | 39-48 | `CA-EXPIRY-DATE` | X(10) | `:39` |
   | 49-74 | `CA-LASTCHANGED` | X(26) | `:40` |
   | 75-84 | `CA-BROKERID` | 9(10) | `:41` |
   | 85-94 | `CA-BROKERSREF` | X(10) | `:42` |
   | 95-100 | `CA-PAYMENT` | 9(6) | `:43` |
   | 101 | `CA-E-WITH-PROFITS` | X | `:47` |
   | 102 | `CA-E-EQUITIES` | X | `:48` |
   | 103 | `CA-E-MANAGED-FUND` | X | `:49` |
   | 104-113 | `CA-E-FUND-NAME` | X(10) | `:50` |
   | 114-115 | `CA-E-TERM` | 99 | `:51` |
   | 116-121 | `CA-E-SUM-ASSURED` | 9(6) | `:52` |
   | 122-152 | `CA-E-LIFE-ASSURED` | X(31) | `:53` |
   | 153-32500 | `CA-E-PADDING-DATA` | X(32348) | `:54` |

2. **Gerekli uzunluk 157 (+ dolgu), `FINAL` işareti buna dahil.**
   - `WS-CA-HEADERTRAILER-LEN` = 33 (`lgipdb01.cbl:73`), `WS-REQUIRED-CA-LEN` önce 0 (`:74`). Hayat sigortası için `WS-FULL-ENDOW-LEN` = 124 eklenir (`lgpolicy.cpy:24`):
     33 + 124 = **157** (`lgipdb01.cbl:374-375`). 157, hayat sigortası sabit alanlarının bittiği 152. bayt + 5 bayttır; bu 5 baytın `FINAL` için ayrıldığı kodda yazmıyor, ama `FINAL` dolgusuz
     durumda tam 153-157. baytlara düşer (Kod gözlemleri 4).
   - `PADDINGDATA` NULL değilse (`LENGTH(PADDINGDATA)` göstergesi `-1` değilse) dolgu uzunluğu eşiğe eklenir ve `END-POLICY-POS`'a da eklenir (`:383-386`; `END-POLICY-POS` önce 1: `:78`).
     Eşik 157 + dolgu uzunluğu olur.
   - Kontrol `lgipdb01.cbl:390-392`. Hayat sigortasında 157 bayt, `FINAL` işaretini (153-157. baytlar, dolgu yoksa) de kapsar: **motordan farklı olarak** yazma COMMAREA'nın dışına taşmaz
     (karşılaştırma: `05-motor-inquire.md` Kod gözlemleri 2).
   - **LGIPOL01'de uzunluk kontrolü yoktur** ve LGIPDB01'i her zaman `Length(32500)` ile çağırır (`lgipol01.cbl:91-94`). LGIPDB01 her zaman `EIBCALEN = 32500` görür: kendi `EIBCALEN = 0`
     (`lgipdb01.cbl:251-255`) ve `98` (`:390-392`) kontrollerine LGIPOL01 üzerinden ulaşılamaz.
   - `98`, uzunluk kontrolünden önce `SELECT`'in başarılı olmasını ister (`lgipdb01.cbl:370`, `:390`): kayıt yoksa kısa COMMAREA'da da `01` döner.
3. **İstek kodu.** LGIPDB01 `CA-REQUEST-ID`'yi büyük harfe çevirip `WS-REQUEST-ID`'ye alır (`lgipdb01.cbl:275`); `CA-REQUEST-ID`'nin kendisi değişmez. `'01IEND'` hayat sigortası sorgusudur
   (`:279-281`); `WHEN OTHER` → `'99'` (`:307-308`) ve hemen `End-Program` (`RETURN`, `:312-313`). LGIPOL01 istek koduna bakmaz.
4. **Db2'den COMMAREA'ya aktarım** (`lgipdb01.cbl:243-245`, `:280`, `:397-417`).
   - `INITIALIZE` ile `DB2-IN-INTEGERS`, `DB2-OUT-INTEGERS` ve `DB2-POLICY` sıfırlanır/boşaltılır; hayat sigortası kolunda ayrıca `DB2-ENDOWMENT` (`:280`).
   - `CA-POLICY-COMMON` ← `DB2-POLICY-COMMON` (tarihler, zaman damgası, `DB2-BROKERID`, `DB2-BROKERSREF`, `DB2-PAYMENT`: `lgpolicy.cpy:45-51`; `lgipdb01.cbl:407`).
   - `CA-ENDOWMENT(1:52)` ← `DB2-ENDOW-FIXED` (52 bayt: bayrak, bayrak, bayrak, fon adı, vade, teminat, yaşam sigortalısı; `lgpolicy.cpy:54-61`; `lgipdb01.cbl:408-409`).
     `DB2-E-TERM` ve `DB2-E-SUMASSURED`, tam sayı host değişkenlerinden önce taşınır (`:404-405`).
   - Dolgu: `PADDINGDATA` NULL değilse `CA-E-PADDING-DATA(1:dolgu uzunluğu)`'na taşınır (`:410-413`); sonra `'FINAL'`, `CA-E-PADDING-DATA(END-POLICY-POS:5)`'e yazılır (`:417`):
     dolgu yoksa 153-157. baytlar, dolgu `n` bayt ise `153+n` … `157+n`. baytlar.
   - `CA-CUSTOMER-NUM`, `CA-POLICY-NUM` ve `CA-REQUEST-ID` yazılmaz; girdideki değerle kalır.
5. **NULL işleme.** `BROKERID`, `BROKERSREFERENCE`, `PAYMENT`, `PADDINGDATA` ve `LENGTH(PADDINGDATA)` için gösterge var (`lgipdb01.cbl:203-207`, `:349-351`, `:359-360`).
   - `BROKERID` ve `PAYMENT` göstergesi `-1` ise taşıma yapılmaz (`:397-402`); `INITIALIZE` ile sıfırlandıkları için (`:245`) COMMAREA'ya `0` gider.
   - `BROKERSREFERENCE` için gösterge tanımlı ama koşul yok (`:350`): NULL gelirse `DB2-BROKERSREF` `INITIALIZE`'dan kalan boşluktur.
   - Diğer seçilen sütunlarda gösterge yok; kod yorumuna göre NULL gelirse `FETCH` `SQLCODE = -305` ile başarısız olur (`:200-202`). `ENDOWMENT` tablosunda `policyNumber` dışında hiçbir
     sütun `NOT NULL` değildir (`db2cre.jcl:204-212`).
6. **Dönüş kodları.**

   | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
   |---|---|---|---|
   | `00` | `lgipol01.cbl:86`, `lgipdb01.cbl:258` | Başlangıç / `SELECT` başarılı | Evet |
   | `01` | `lgipdb01.cbl:423` | `SQLCODE = 100` (kayıt yok) | Evet |
   | `90` | `lgipdb01.cbl:426` | Diğer SQLCODE'lar | Evet |
   | `98` | `lgipdb01.cbl:391` | `SELECT` başarılı ama COMMAREA < 157 (+ dolgu) | Yalnızca LGIPDB01 doğrudan çağrılırsa |
   | `99` | `lgipdb01.cbl:308` | İstek kodu tanınmıyor | Evet |
   | ABEND `LGCA` | `lgipol01.cbl:82`, `lgipdb01.cbl:254` | COMMAREA yok | — |

7. **Sonuca etki etmeyen noktalar.** Sorgulama hiçbir tabloyu değiştirmez. Zincir VSAM'a bakmaz (`lgipdb01.cbl`'de `LGSTSQ` dışında LINK yok). `FINAL` işareti yalnızca `SELECT` başarılıysa ve
   (LGIPDB01 doğrudan çağrılıyorsa) uzunluk yeterliyse yazılır; `98` ve `RETURN`'den önce yazılmaz (`lgipdb01.cbl:390-392`, `:417`).
8. **Hata mesajı içerikleri.**
   - `ERROR-MSG` uzunlukları: LGIPOL01 45 bayt (`lgipol01.cbl:41-46`), LGIPDB01 87 bayt (`lgipdb01.cbl:48-60`). `CA-ERROR-MSG` ikisinde de 99 bayttır: `'COMMAREA='` + 90 bayt.
   - LGIPDB01: `EM-SQLREQ` = `' SELECT ENDOW '` + 2 boşluk (`:329`). `EM-CUSNUM` ← `CA-CUSTOMER-NUM`, `EM-POLNUM` ← `CA-POLICY-NUM` (`:266-267`). `EM-SQLRC` `PIC +9(5)` (`:60`, `:999`).
   - LGIPDB01'in `EIBCALEN = 0` mesajı: 21 karakterlik `' NO COMMAREA RECEIVED'`, 63 baytlık `EM-VARIABLE` grubuna taşınır ve sabitleri ezer (`lgipdb01.cbl:53-60`, `:252`).
     Sonra `MOVE SQLCODE TO EM-SQLRC` son 6 baytı ezer (`:999`).
9. **`WRITE-ERROR-MESSAGE` dalları** (`IF EIBCALEN > 0` / `< 91` / `ELSE`).
   - LGIPOL01'de yalnızca `EIBCALEN = 0` dalından çağrılır (`lgipol01.cbl:81`); iç dallar (`lgipol01.cbl:124-138`) çalışmaz, ölü kod.
   - LGIPDB01 zincirden çağrıldığında `EIBCALEN = 32500` olduğu için `ELSE` dalı çalışır (`lgipdb01.cbl:1022-1027`). `< 91` dalına (`lgipdb01.cbl:1016-1021`) yalnızca LGIPDB01 doğrudan
     `EIBCALEN < 91` ile çağrılıp `90` yoluna düşülürse ulaşılır (13-15).
10. **WORKING-STORAGE kalıcılığı.** `WS-REQUIRED-CA-LEN` `VALUE +0` ve her başarılı `SELECT`'te üstüne eklenir; `END-POLICY-POS` de `VALUE +1` ve dolguda üstüne eklenir (`lgipdb01.cbl:74`, `:78`, `:374-375`,
    `:384-385`). WORKING-STORAGE yeniden kurulmazsa ikinci çağrıda eşik 314 olur. Test düzeneği her durumu ayrı süreçte çalıştırır [belge] `docs/adr/0004-legacy-test-harness.md` (karar 4).
    Bu dosyadaki tahminler tek çağrı içindir.
11. **Test verisi** (`base/cntl/db2cre.jcl`). Bu değerler yalnızca test veritabanı bu JCL ile yüklendiyse ve sonradan değiştirilmediyse geçerlidir.
    - Hayat sigortası poliçeleri: poliçe 4 (müşteri 8: `:849-850`, tür `E`: `:853`; hayat sigortası 4: `:1033-1040`) ve poliçe 5 (müşteri 3: `:825-826`, tür `E`: `:829`; hayat sigortası 5: `:1014-1021`).
    - İki `ENDOWMENT` INSERT'i de `paddingData` sütununu listelemez (`:1004-1012`, `:1023-1031`): iki satırda da dolgu **NULL**'dır. Bu yüzden 13-01 ve 13-02'de `FINAL` 153-157. baytlardadır.
    - İki satırda da `equities` ile `withProfits` aynı değeri taşır (`Y`/`Y` ve `N`/`N`: `:1034-1035`, `:1015-1016`). JCL verisi iki alanın karışıp karışmadığını ayırt etmez; bunu 13-18 (B4) sınar.
    - Poliçe 1 müşteri 2'ye ait bir motor poliçesidir (`:945-946`); `ENDOWMENT`'ta satırı yoktur.
    - `db2cre.jcl`'de `brokerId` 0, `brokersReference` boş (`CHAR(10)`: `:170`) ve `payment` 0 (`:855-857`, `:831-833`).
    - Olmayan poliçe için `0000000999` kullanılır: JCL poliçe numaralarını 1-10 verir, kimlik değeri 1000001'den başlar (`:162-163`).

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 13-01 | LGIPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01IEND'`. `CA-CUSTOMER-NUM = 0000000008`, `CA-POLICY-NUM = 0000000004` (poliçe 4, müşteri 8: `db2cre.jcl:849-850`). Db2 yalnızca `db2cre.jcl` ile yüklenmiş. | `CA-RETURN-CODE = 00` | Poliçe 4 ve hayat sigortası 4'ün bilgileri döner (alan alan aşağıdaki **"13-01 beklenen değerler"** tablosunda): ortak alanlar, `Y` `Y` `N` `LIONTMR`, vade 5, teminat 12500, `J. MORRIS`. Dolgu NULL olduğundan `CA-E-PADDING-DATA`'nın ilk 5 baytı (153-157. baytlar) `FINAL` olur. `CA-REQUEST-ID`, `CA-CUSTOMER-NUM`, `CA-POLICY-NUM` değişmez. Tarih ve zaman damgası alanlarının biçimi **belirsiz**. Hata mesajı yazılmaz. | `lgipol01.cbl:79`, `:86`, `:91-96`<br>`lgipdb01.cbl:258`, `:263-264`, `:275`, `:279-281`, `:327-368`, `:370`, `:374-375`, `:383`, `:390`, `:397-417` | İş kuralı |
| 13-02 | 13-01 ile aynı, ama poliçe 5 ve müşteri 3 (`db2cre.jcl:825-826`; hayat sigortası 5: `:1014-1021`). Bu satır ikinci bir veri kümesinin doğru eşlendiğini sınar. | `CA-RETURN-CODE = 00` | `CA-E-WITH-PROFITS = N`, `CA-E-EQUITIES = N`, `CA-E-MANAGED-FUND = N`, `CA-E-FUND-NAME = SHEPPA` + ␣×4, `CA-E-TERM = 10`, `CA-E-SUM-ASSURED = 050000`, `CA-E-LIFE-ASSURED = Shep` + ␣×27. `CA-ISSUE-DATE` 1 Ekim 2011, `CA-EXPIRY-DATE` 30 Eylül 2012, `CA-LASTCHANGED` 1 Ekim 2011 09:31:22 (biçimler **belirsiz**). `CA-BROKERID = 0000000000`, `CA-BROKERSREF` ␣×10, `CA-PAYMENT = 000000`. `FINAL` işareti 153-157. baytlarda. | `lgipdb01.cbl:397-417`<br>`db2cre.jcl:827-833`, `:1015-1021` | İş kuralı |
| 13-03 | LGIPOL01 çağrılır. `EIBCALEN = 32500`, `CA-REQUEST-ID = '01IEND'`. `CA-CUSTOMER-NUM = 0000000008`, `CA-POLICY-NUM = 0000000999` (poliçe 999 yok). | `CA-RETURN-CODE = 01` | Alanlara dokunulmaz: `SELECT` satır bulmaz (`SQLCODE = 100`), `FINAL` yazılmaz. Hata mesajı yazılmaz (`100` dalında `WRITE-ERROR-MESSAGE` yok). | `lgipdb01.cbl:370`, `:419-423` | İş kuralı |
| 13-04 | 13-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000003` (poliçe 4 müşteri 8'e ait, müşteri 3'e değil: `db2cre.jcl:850`). | `CA-RETURN-CODE = 01` | 13-03 ile aynı: `WHERE` koşulu müşteri numarasını da içerir, eşleşen satır yoktur. | `lgipdb01.cbl:361-367`, `:419-423` | İş kuralı |
| 13-05 | 13-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000002`, `CA-POLICY-NUM = 0000000001` (poliçe 1, müşteri 2'ye ait bir motor poliçesi: `db2cre.jcl:945-946`; `ENDOWMENT`'ta satırı yok). | `CA-RETURN-CODE = 01` | 13-03 ile aynı: poliçe var ama `ENDOWMENT` birleşiminde satır yok. İstek kodu hayat sigortası olduğu için tür denetimi ayrıca yapılmaz; sonuç birleşimin boş çıkmasından gelir. | `lgipdb01.cbl:279-281`, `:361-367`, `:419-423` | İş kuralı |
| 13-06 | 13-01 ile aynı, ama [B4] `SELECT`'te 0 ve 100 dışında bir `SQLCODE` (örn. `-904`) üretilir. | `CA-RETURN-CODE = 90` | Alanlara dokunulmaz, `FINAL` yazılmaz.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGIPDB01'`, `' CNUM='` + `0000000008`, `' PNUM='` + `0000000004`, `' SELECT ENDOW '` + 2 boşluk, `' SQLCODE='` + `-00904`. (2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı (`EIBCALEN = 32500`, `ELSE` dalı). | `lgipdb01.cbl:266-267`, `:329`, `:424-428`, `:997-1027` | İş kuralı |
| 13-07 | 13-01 ile aynı, ama `CA-REQUEST-ID = '01iend'` (küçük harf). | `CA-RETURN-CODE = 00` | 13-01 ile aynı dönüş. İstek kodu yalnızca karşılaştırma için büyük harfe çevrilir; dönüşte `CA-REQUEST-ID` girdideki gibi küçük harfle kalır. Ekleme (`12-04`) küçük harfi reddeder, sorgulama kabul eder. | `lgipdb01.cbl:275`, `:279-281` | Arayüz (COMMAREA) kuralı |
| 13-08 | LGIPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01IXXX'` (tanınmayan kod; `'01AEND'` gibi başka bir işlemin kodu da aynı yola girer). | `CA-RETURN-CODE = 99` | Hiçbir SQL çalıştırılmaz, alanlara dokunulmaz, hata mesajı yazılmaz. | `lgipdb01.cbl:277`, `:307-308`, `:312-313` | Arayüz (COMMAREA) kuralı |
| 13-09 | LGIPOL01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGIPDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGIPOL01'`, `' NO COMMAREA RECEIVED'`). COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz). | `lgipol01.cbl:79-83`, `:41-46`, `:107-124` | Arayüz (COMMAREA) kuralı |
| 13-10 | **LGIPDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). [B2] `SQLCODE = 0`. | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 87 baytlık `ERROR-MSG`. İçeriği: tarih, saat, `' LGIPDB01'`, `' NO COMMAREA RECEIVED'` + 36 boşluk + 6 baytlık `EM-SQLRC`. Taşıma 63 baytlık `EM-VARIABLE` grubunun tamamını ezdiği için `' CNUM='`, `' PNUM='`, `' SQLCODE='` sabitleri mesajda yer almaz. [B2] `EM-SQLRC = +00000` [kod] `lgipdb01.cbl:999`. COMMAREA dökümü gönderilmez.<br>LGIPOL01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 2). | `lgipdb01.cbl:251-255`, `:53-60`, `:997-1015` | Arayüz (COMMAREA) kuralı |
| 13-11 | LGIPOL01 çağrılır. `EIBCALEN` 28 ile 156 arasında (örn. 100). Diğer girdiler 13-01 ile aynı; müşteri ve poliçe numaraları (9-28. baytlar) COMMAREA'nın içinde. | `CA-RETURN-CODE = 00`. `98` **dönmez**. | LGIPOL01 uzunluğa bakmaz ve LGIPDB01'i `Length(32500)` ile çağırır; LGIPDB01 `EIBCALEN = 32500` görür, uzunluk kontrolünü geçer. Veri 13-01'deki gibi yazılır. [B1] Yazmaların `EIBCALEN` ötesine düşen kısmı (en çok 157. bayta kadar, `FINAL` dahil) çağıranın tamponuna düşer ve çağıran bunları görür (ön koşullu) [kod] `lgipol01.cbl:91-94`. B1 sağlanmazsa COMMAREA dışına yazmanın etkisi belirsiz. | `lgipol01.cbl:91-94`<br>`lgipdb01.cbl:390-392`, `:397-417` | Arayüz (COMMAREA) kuralı |
| 13-12 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 156 arasında (sınır değer: `156`). Diğer girdiler 13-01 ile aynı; kayıt Db2'de var (dolgu NULL). | `CA-RETURN-CODE = 98` | `SELECT` çalışır ve satır bulunur, ama alanlar COMMAREA'ya taşınmaz ve `FINAL` yazılmaz (`98` ve `RETURN`, taşımalardan önce). Hata mesajı yazılmaz. LGIPOL01 üzerinden bu yola ulaşılamaz. | `lgipdb01.cbl:370`, `:374-375`, `:390-392`<br>`lgipdb01.cbl:73`, `lgpolicy.cpy:24` | Arayüz (COMMAREA) kuralı |
| 13-13 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN = 157` (kabul edilen en küçük uzunluk). Diğer girdiler 13-01 ile aynı; kayıt Db2'de var (dolgu NULL). | `CA-RETURN-CODE = 00` | Uzunluk kontrolü geçilir (`157 < 157` yanlış). Bütün alanlar 13-01 gibi yazılır ve `FINAL` 153-157. baytlara düşer: **COMMAREA'nın dışına yazma yoktur** (motor sorgusundaki `05-13`'ten fark). | `lgipdb01.cbl:374-375`, `:390-393`, `:397-417` | Arayüz (COMMAREA) kuralı |
| 13-14 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 156 arasında (örn. 100). `CA-POLICY-NUM = 0000000999` (poliçe yok). | `CA-RETURN-CODE = 01` | `98` **dönmez**: uzunluk kontrolü yalnızca `SELECT` başarılı olunca yapılır. Alanlara dokunulmaz. | `lgipdb01.cbl:370`, `:390`, `:419-423` | Arayüz (COMMAREA) kuralı |
| 13-15 | **LGIPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (örn. 50). [B4] `SELECT`'te 0 ve 100 dışında bir `SQLCODE` üretilir. | `CA-RETURN-CODE = 90` | Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG`: 13-06 ile aynı biçim. (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır. | `lgipdb01.cbl:424-428`, `:1015-1021` | Arayüz (COMMAREA) kuralı |
| 13-16 | 13-01 ile aynı, ama [B4] poliçe 4'ün `brokerId` sütunu NULL. | `CA-RETURN-CODE = 00` | `CA-BROKERID = 0000000000`: gösterge `-1` olduğu için `DB2-BROKERID` doldurulmaz, `INITIALIZE` ile sıfırdır. Diğer alanlar 13-01 ile aynı. | `lgipdb01.cbl:245`, `:349`, `:397-399`, `:407` | İş kuralı |
| 13-17 | 13-01 ile aynı, ama [B4] poliçe 4'ün `payment` sütunu NULL. | `CA-RETURN-CODE = 00` | `CA-PAYMENT = 000000`: gösterge `-1` olduğu için `DB2-PAYMENT` doldurulmaz. Diğer alanlar 13-01 ile aynı. | `lgipdb01.cbl:245`, `:351`, `:400-402`, `:407` | İş kuralı |
| 13-18 | 13-01 ile aynı, ama [B4] hayat sigortası 4'ün `withProfits` sütunu `N`, `equities` sütunu `Y` (JCL verisinde ikisi hep aynı). | `CA-RETURN-CODE = 00` | `CA-E-WITH-PROFITS = N` (101. bayt) ve `CA-E-EQUITIES = Y` (102. bayt): `SELECT` listesi ve `INTO` listesi bu iki sütunu aynı sırayla (`WITHPROFITS`, `EQUITIES`) yazar ve `CA-ENDOWMENT` düzeni de aynı sıradadır. Diğer alanlar 13-01 ile aynı. | `lgipdb01.cbl:337-338`, `:352-353`, `:408-409`<br>`lgpolicy.cpy:55-56`<br>`lgcmarea.cpy:47-48` | İş kuralı |
| 13-19 | 13-01 ile aynı, ama [B4] hayat sigortası 4'ün `paddingData` sütunu NULL değil: `ABCDEFGHIJ` (uzunluk 10). | `CA-RETURN-CODE = 00` | `CA-E-PADDING-DATA(1:10) = ABCDEFGHIJ` (153-162. baytlar); `FINAL` `CA-E-PADDING-DATA(11:5)`'e, yani **163-167. baytlara** yazılır (`END-POLICY-POS` = 1 + 10). Diğer alanlar 13-01 ile aynı. | `lgipdb01.cbl:78`, `:383-386`, `:410-413`, `:417` | İş kuralı |
| 13-20 | **LGIPDB01 doğrudan** çağrılır. 13-19'un Db2 durumu (dolgu uzunluğu 10). `EIBCALEN = 166`. | `CA-RETURN-CODE = 98` | Gerekli uzunluk 157 + 10 = 167 (`lgipdb01.cbl:374-375`, `:384`); `166 < 167`. Alanlar taşınmaz, `FINAL` yazılmaz. | `lgipdb01.cbl:374-375`, `:383-386`, `:390-392` | Arayüz (COMMAREA) kuralı |
| 13-21 | **LGIPDB01 doğrudan** çağrılır. 13-19'un Db2 durumu. `EIBCALEN = 167`. | `CA-RETURN-CODE = 00` | Uzunluk kontrolü geçilir (`167 < 167` yanlış). 13-19'daki gibi veri ve `FINAL` (163-167. baytlar) yazılır; hepsi COMMAREA içinde kalır. | `lgipdb01.cbl:384-385`, `:390-393`, `:410-417` | Arayüz (COMMAREA) kuralı |
| 13-22 | 13-01 ile aynı, ama [B4] hayat sigortası 4'ün `paddingData` sütunu NULL **değil**, uzunluğu **0** (boş `VARCHAR`). | **belirsiz** | **belirsiz**. Gösterge `-1` olmadığı için `MOVE DB2-E-PADDINGDATA TO CA-E-PADDING-DATA(1:DB2-E-PADDING-LEN)` çalışır ve uzunluk 0'dır (`lgipdb01.cbl:410-413`). Uzunluğu sıfır olan bir referans değiştirmenin sonucu kodda tanımlı değil. | `lgipdb01.cbl:383-386`, `:410-413` | Arayüz (COMMAREA) kuralı |
| 13-23 | 13-01 ile aynı, ama [B4] hayat sigortası 4'ün `fundName` sütunu NULL (göstergesi olmayan bir sütun). | **belirsiz** (kod yorumuna göre `90`) | Kod yorumu: gösterge olmayan sütunda NULL dönerse `FETCH` `SQLCODE = -305` ile başarısız olur [kod yorumu] `lgipdb01.cbl:200-202`. Öyleyse `SQLCODE` ne `0` ne `100` olur ve `90` yolu (13-06) çalışır. Db2'nin bu durumda gerçekte ne döndüreceği kodda tanımlı değil. | `lgipdb01.cbl:355`, `:424-428`<br>[kod yorumu] `lgipdb01.cbl:200-202`<br>`db2cre.jcl:208` | İş kuralı |
| 13-24 | 13-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000008` (10 haneli). [B3] | [B3] `CA-RETURN-CODE = 00` (ön koşullu) [kod] `lgipdb01.cbl:370`. | [B3] `PIC 9(10)` → `PIC S9(9) COMP` taşımasında en soldaki hane kesilir ve `DB2-CUSTOMERNUM-INT = 8` olur: **müşteri 8'in poliçe 4'ü** sorgulanır. Dönen alanlar 13-01 ile aynıdır. `CA-CUSTOMER-NUM` girdideki gibi `1000000008` kalır.<br>B3 sağlanmazsa sonuç **belirsiz**. | `lgcmarea.cpy:12`<br>`lgipdb01.cbl:167`, `:263`<br>`cobol.jcl:44-50` | Arayüz (COMMAREA) kuralı |
| 13-25 | 13-01 ile aynı, ama `CA-POLICY-NUM` ya da `CA-CUSTOMER-NUM` sayısal değil (örn. boşluk). | **belirsiz** | **belirsiz**. İki programda da sayısallık kontrolü yok. Sayısal olmayan değer doğrudan `DB2-CUSTOMERNUM-INT`/`DB2-POLICYNUM-INT`'e taşınır. | `lgipdb01.cbl:263-264` | Arayüz (COMMAREA) kuralı |
| 13-26 | LGIPOL01 çağrılır. `EIBCALEN = 32500`. 13-01 ile aynı, ama LGIPDB01'e yapılan LINK başarısız olur (örn. program tanımlı değil). | **belirsiz** | **belirsiz**. LINK'te `RESP`/`NOHANDLE` yok, programda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgipol01.cbl:91-94` | Arayüz (COMMAREA) kuralı |

## 13-01 beklenen değerler

Poliçe 4 (müşteri 8) için dönüşte COMMAREA'nın alan alan beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Beklenen değer | Değerin kaynağı (`db2cre.jcl`) | Alanı dolduran COBOL satırı |
|---|---|---|---|
| `CA-REQUEST-ID` X(6) | Girdideki değer (`01IEND`), değişmez | — | Kodda bu alana yazılmıyor |
| `CA-RETURN-CODE` 9(2) | `00` | — | `lgipdb01.cbl:258` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000008` (girdi, değişmez) | `:850` | `lgipdb01.cbl:263` (yalnızca okunur) |
| `CA-POLICY-NUM` 9(10) | `0000000004` (girdi, değişmez) | `:849` | `lgipdb01.cbl:264` (yalnızca okunur) |
| `CA-ISSUE-DATE` X(10) | 5 Ağustos 2011. JCL'de `'2011-08-05'` olarak ekleniyor, ama dönen metnin biçimi **belirsiz** | `:851` (sütun `DATE`: `:165`) | `lgipdb01.cbl:331`, `:346`, `:407` |
| `CA-EXPIRY-DATE` X(10) | 4 Ağustos 2012. JCL'de `'2012-08-04'`; dönen biçim **belirsiz** | `:852` (sütun `DATE`: `:166`) | `lgipdb01.cbl:332`, `:347`, `:407` |
| `CA-LASTCHANGED` X(26) | 5 Ağustos 2011 10:45:19. JCL'de `'2011-08-05 10:45:19'`; dönen biçim **belirsiz** | `:854` (sütun `TIMESTAMP`: `:168`) | `lgipdb01.cbl:333`, `:348`, `:407` |
| `CA-BROKERID` 9(10) | `0000000000` | `:855` | `lgipdb01.cbl:334`, `:349`, `:397-399`, `:407` |
| `CA-BROKERSREF` X(10) | ␣×10 | `:856` (sütun `CHAR(10)`: `:170`) | `lgipdb01.cbl:335`, `:350`, `:407` |
| `CA-PAYMENT` 9(6) | `000000` | `:857` | `lgipdb01.cbl:336`, `:351`, `:400-402`, `:407` |
| `CA-E-WITH-PROFITS` X | `Y` | `:1035` (sütun `CHAR(1)`: `:206`) | `lgipdb01.cbl:337`, `:352`, `:408-409` |
| `CA-E-EQUITIES` X | `Y` | `:1034` (sütun `CHAR(1)`: `:205`) | `lgipdb01.cbl:338`, `:353`, `:408-409` |
| `CA-E-MANAGED-FUND` X | `N` | `:1036` (sütun `CHAR(1)`: `:207`) | `lgipdb01.cbl:339`, `:354`, `:408-409` |
| `CA-E-FUND-NAME` X(10) | `LIONTMR` + ␣×3 | `:1037` (sütun `CHAR(10)`: `:208`) | `lgipdb01.cbl:340`, `:355`, `:408-409` |
| `CA-E-TERM` 99 | `05` | `:1038` | `lgipdb01.cbl:341`, `:356`, `:404`, `:408-409` |
| `CA-E-SUM-ASSURED` 9(6) | `012500` | `:1039` | `lgipdb01.cbl:342`, `:357`, `:405`, `:408-409` |
| `CA-E-LIFE-ASSURED` X(31) | `J. MORRIS` + ␣×22 | `:1040` (sütun `CHAR(31)`: `:211`) | `lgipdb01.cbl:343`, `:358`, `:408-409` |
| `CA-E-PADDING-DATA` X(32348) | İlk 5 bayt (153-157. baytlar) `FINAL`; gerisi girdideki gibi (dolgu NULL olduğu için hiçbir dolgu baytı taşınmaz) | `:1004-1012` (sütun listelenmiyor) | `lgipdb01.cbl:383`, `:410`, `:417` |

## Kapsam tablosu

Koddaki her `IF`/`EVALUATE` dalının ve dönüş kodunun, onu kapsayan test numaraları.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| IO-01 | `lgipol01.cbl:79-83` `IF EIBCALEN = 0` → ABEND `LGCA` | 13-09 | |
| IO-02 | `lgipol01.cbl:86` `'00'` → `CA-RETURN-CODE` | 13-01 | |
| IO-03 | `lgipol01.cbl:91-96` LINK LGIPDB01 `Length(32500)`, RETURN; uzunluk kontrolü yok | 13-01, 13-11, 13-26 | Kod gözlemleri 2 |
| IO-04 | `lgipol01.cbl:107-139` `WRITE-ERROR-MESSAGE` | 13-09 | Dış `IF EIBCALEN > 0` yanlış. İç dallar (`lgipol01.cbl:124-138`) ölü kod (Kod gözlemleri 9) |
| ID-01 | `lgipdb01.cbl:251-255` `IF EIBCALEN = 0` → ABEND `LGCA` | 13-10 | Yalnızca doğrudan çağrı |
| ID-02 | `lgipdb01.cbl:258` `'00'` | 13-01 | |
| ID-03 | `lgipdb01.cbl:263-267` `CA-CUSTOMER-NUM`, `CA-POLICY-NUM` → host değişkenleri, `EM-*` | 13-01, 13-06, 13-24, 13-25 | |
| ID-04 | `lgipdb01.cbl:275-310` `UPPER-CASE`, `EVALUATE WS-REQUEST-ID` | 13-01 (`WHEN '01IEND'`), 13-07 (küçük harf), 13-08 (`WHEN OTHER` → `99`) | Hayat sigortası dışı `WHEN` kolları kapsam dışı |
| ID-05 | `lgipdb01.cbl:327-368` `SELECT` (join, 3 koşul) | 13-01, 13-03, 13-04, 13-05 | |
| ID-06 | `lgipdb01.cbl:370` `IF SQLCODE = 0` | 13-01 (doğru dal), 13-03, 13-06 (yanlış dal) | |
| ID-07 | `lgipdb01.cbl:390-392` `IF EIBCALEN < WS-REQUIRED-CA-LEN` → `98` (doğru ve yanlış dal) | 13-12, 13-20 (doğru dal); 13-01, 13-13, 13-21 (yanlış dal) | Yalnızca doğrudan çağrı |
| ID-08 | `lgipdb01.cbl:383-386` `IF IND-E-PADDINGDATAL NOT EQUAL MINUS-ONE` | 13-19, 13-20, 13-21 (doğru dal); 13-01 (yanlış dal) | 13-22: belirsiz |
| ID-09 | `lgipdb01.cbl:397-399` `IF IND-BROKERID NOT EQUAL MINUS-ONE` | 13-01 (doğru dal), 13-16 (yanlış dal) | |
| ID-10 | `lgipdb01.cbl:400-402` `IF IND-PAYMENT NOT EQUAL MINUS-ONE` | 13-01 (doğru dal), 13-17 (yanlış dal) | |
| ID-11 | `lgipdb01.cbl:404-409` Db2 → COMMAREA taşımaları | 13-01, 13-02, 13-18 | |
| ID-12 | `lgipdb01.cbl:410-413` `IF IND-E-PADDINGDATA NOT EQUAL MINUS-ONE` → dolgu taşıma | 13-19, 13-21 (doğru dal); 13-01 (yanlış dal) | 13-22: belirsiz |
| ID-13 | `lgipdb01.cbl:417` `'FINAL'` işareti | 13-01, 13-02, 13-13, 13-19, 13-21 | |
| ID-14 | `lgipdb01.cbl:421-423` `IF SQLCODE EQUAL 100` → `01` | 13-03, 13-04, 13-05, 13-14 | |
| ID-15 | `lgipdb01.cbl:424-428` `ELSE` → `90` + hata mesajı | 13-06, 13-15, 13-23 | |
| ID-16 | `lgipdb01.cbl:997-1030` `WRITE-ERROR-MESSAGE` | 13-06 (`ELSE`), 13-15 (`< 91`), 13-10 (dış `IF` yanlış) | |
| SQ-01 | `lgstsq.cbl:68-71` LINK yolu | 13-06, 13-09, 13-10, 13-15 | |
| SQ-02 | `lgstsq.cbl:72-80` terminalden RECEIVE | — | **Kapsam dışı: bu zincirden ulaşılamaz.** LGSTSQ yalnızca LINK ile çağrılır. |
| SQ-03 | `lgstsq.cbl:94-111` WRITEQ TD `CSMT`, TS `GENAERRS` | 13-06 | |
| N-01 | `CA-REQUEST-ID` büyük/küçük harf duyarsız | 13-07 | |
| N-02 | Sayısallık ve aralık kontrolü yok | 13-24, 13-25 | |
| N-03 | LINK'te `RESP` yok | 13-26 | |
| N-04 | Göstergesiz sütunda NULL | 13-23 | Kod gözlemleri 5 |
| N-05 | Hayat sigortasında `FINAL` işareti COMMAREA içinde kalır (eşik 157) | 13-13, 13-21 | Kod gözlemleri 2 |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları").

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 13-06 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | Girdi kurulamaz |
| 13-10 | B2 | `EM-SQLRC = +00000` | `EM-SQLRC` belirsiz |
| 13-11 | B1 | COMMAREA dışına yazılan baytlar çağıranın tamponuna düşer | COMMAREA dışına yazmanın etkisi belirsiz |
| 13-15 | B4 | Girdi düzenekle kurulur; tahmin (`90`, `< 91` dalı) değişmez | Girdi kurulamaz |
| 13-16 | B4 | `brokerId` NULL satırı düzenekle kurulur; `CA-BROKERID = 0000000000` | Girdi kurulamaz: başlangıç verisinde NULL yok (`db2cre.jcl:855`) |
| 13-17 | B4 | `payment` NULL satırı düzenekle kurulur; `CA-PAYMENT = 000000` | Girdi kurulamaz: başlangıç verisinde NULL yok (`db2cre.jcl:857`) |
| 13-18 | B4 | `withProfits` `N`, `equities` `Y` satırı düzenekle kurulur | Girdi kurulamaz: başlangıç verisinde iki alan aynı değerde |
| 13-19 | B4 | Dolgusu olan satır düzenekle kurulur; `FINAL` 163-167. baytlarda | Girdi kurulamaz: başlangıç verisinde dolgu NULL (`db2cre.jcl:1004-1012`) |
| 13-20 | B4 | 13-19'un Db2 durumu düzenekle kurulur; tahmin (`98`) değişmez | Girdi kurulamaz |
| 13-21 | B4 | 13-19'un Db2 durumu düzenekle kurulur; tahmin (`00`) değişmez | Girdi kurulamaz |
| 13-22 | B4 | Boş ama NULL olmayan dolgu düzenekle kurulur | Girdi kurulamaz |
| 13-23 | B4 | `fundName` NULL satırı düzenekle kurulur | Girdi kurulamaz |
| 13-24 | B3 | Müşteri 8'in poliçe 4'ü sorgulanır: `00` ve 13-01'deki değerler | Dönüş kodu ve veri belirsiz |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 13-01 | Beklenen veri (yalnızca tarih ve zaman damgası biçimleri) | `issueDate`, `expiryDate` sütunları `DATE`, `lastChanged` sütunu `TIMESTAMP` türünde (`db2cre.jcl:165-168`) ve doğrudan `PIC X(10)`/`PIC X(26)` alanlara okunuyor (`lgipdb01.cbl:346-348`). Okunurken hangi biçimde geleceği Db2 ayarına bağlı ve kodda yazmıyor. |
| 13-02 | Beklenen veri (yalnızca tarih ve zaman damgası biçimleri) | 13-01 ile aynı neden. |
| 13-22 | Dönüş kodu, beklenen veri | Uzunluğu sıfır olan referans değiştirme (`CA-E-PADDING-DATA(1:0)`) kodda tanımlı değil. |
| 13-23 | Dönüş kodu | Db2'nin göstergesiz sütunda NULL için `-305` döndüreceği yalnızca kod yorumunda yazıyor; gerçek Db2 davranışı kodda yok. |
| 13-25 | Dönüş kodu, beklenen veri | Sayısal olmayan veri sayısal alana taşınıyor ve hiçbir doğrulama yok. Sonuç tanımsız. |
| 13-26 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil. |
