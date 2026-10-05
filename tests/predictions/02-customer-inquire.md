# 02 — Müşteri sorgula (Inquire Customer)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Müşteri sorgula: müşteri numarasıyla Db2 `CUSTOMER` tablosundan müşteri bilgisi okunur |
| Programlar (çağrı zinciri) | `LGICUS01` (iş mantığı) → `EXEC CICS LINK` → `LGICDB01` (Db2 erişimi). Hata yollarında ikisi de `LGSTSQ`'ya LINK eder (`LGSTSQ` 2026-10-05 denetiminde okundu: Kod gözlemleri 9). |
| Okunan COBOL dosyaları | `base/src/lgicus01.cbl`, `base/src/lgicdb01.cbl`, `base/src/lgstsq.cbl`, `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy`, `base/src/lgsetup.cbl` (yalnızca sayaç tanımı, 02-02 için) |
| Test verisi kaynağı | `base/cntl/db2cre.jcl` (02-01 ve 02-02'deki somut değerler, 02-15'teki sütun tanımları), `base/cntl/cobol.jcl` (02-13: derleme seçenekleri). COBOL kodu değil. |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-03 (ilk sürüm), 2026-10-05 (kapsam denetimi) |
| Kaynak commit (`git rev-parse HEAD`) | `61c635f25b88a92aa3ee92e799c9039522c2c8d6` (ilk sürüm: `e3813c206d2f4102b032a0fdda80b0bc14556b87`) |

## Kaynak etiketleri ve ön koşul kodları

2026-10-05 A/B güncellemesinde eklenen ya da değişen tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL, kurulum betiği ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge]**: `base/` altındaki açıklama metinleri (`.md`). Bu dosyada belgeye dayanan tahmin yok.
- Etiketsiz kaynaklar [kod]'dur.

Ön koşul kodları test düzeneğinin vereceği kararlardır. Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul
sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması) kendisi kurar. |

(B5 ve B6 bu dosyada kullanılmıyor.)

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
   çalışır (02-09, 02-10, 02-11, 02-17).
4. **LGICUS01, LGICDB01'in dönüş kodunu kontrol etmez.** LINK'ten sonra doğrudan
   `EXEC CICS RETURN` yapar (`lgicus01.cbl:109`, `:128`, `:115`). LGICDB01'in yazdığı
   `CA-RETURN-CODE` çağırana olduğu gibi gider.
5. **`CA-REQUEST-ID` iki programda da hiç okunmaz.** Mainline'da kontrol edilen tek şey
   `EIBCALEN`'dir (`lgicus01.cbl:87-107`, `lgicdb01.cbl:119-143`).
6. **Somut test verisi `db2cre.jcl`'den alındı.** 02-01'deki müşteri numarası ve değerler ile 02-02'deki
   numaranın seçim gerekçesi, Db2 tablolarını oluşturup örnek veriyi yükleyen `base/cntl/db2cre.jcl`'den geliyor
   (02-02 için ayrıca `lgsetup.cbl:53`).
   Bu değerler yalnızca test veritabanı bu JCL ile yüklendiyse ve sonradan değiştirilmediyse
   geçerlidir. Müşteri tablosunun metin sütunları sabit uzunlukludur (`CHAR(n)`,
   `db2cre.jcl:113-121`) ve COMMAREA alanları da `PIC X(n)` olduğu için değerler sağdan boşlukla
   tamamlanmış olarak beklenir.
7. **Dönüş kodları.**

   | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
   |---|---|---|---|
   | `00` | `lgicus01.cbl:93`, `lgicdb01.cbl:126`, `:194` | Başlangıç / müşteri bulundu | Evet |
   | `01` | `lgicdb01.cbl:196` | `SQLCODE = 100` | Evet |
   | `01` | `lgicdb01.cbl:198` | `SQLCODE = -913` | Evet |
   | `90` | `lgicdb01.cbl:200` | Diğer SQLCODE değerleri | Evet |
   | `98` | `lgicus01.cbl:105` | COMMAREA kısa | Evet |
   | `98` | `lgicdb01.cbl:141` | COMMAREA kısa | Yalnızca LGICDB01 doğrudan çağrılırsa |
   | ABEND `LGCA` | `lgicus01.cbl:90`, `lgicdb01.cbl:122` | COMMAREA yok | — |

8. **Hata mesajı içerikleri.**
   - `ERROR-MSG` uzunlukları: LGICUS01 45 bayt (`lgicus01.cbl:41-46`), LGICDB01 71 bayt (`lgicdb01.cbl:40-50`).
     İkinci mesaj `CA-ERROR-MSG` ikisinde de 99 bayttır: `'COMMAREA='` + 90 bayt (`lgicus01.cbl:48-50`,
     `lgicdb01.cbl:52-54`).
   - LGICDB01'in mesajında program adı olarak `' LGICUS01'` yazar (`lgicdb01.cbl:44`).
   - LGICDB01'de `' CNUM='` alanı doludur: `EM-CUSNUM` ← `CA-CUSTOMER-NUM` (`lgicdb01.cbl:148`). `EM-SQLREQ` ise
     hiçbir satırda doldurulmaz ve 16 boşluk kalır (`lgicdb01.cbl:48`).
   - LGICDB01'in `EIBCALEN = 0` mesajı: 21 karakterlik `' NO COMMAREA RECEIVED'`, 47 baytlık `EM-VARIABLE`
     grubuna taşınır ve `' CNUM='` ile `' SQLCODE='` sabitlerini ezer (`lgicdb01.cbl:45-50`, `:120`).
     Sonra `MOVE SQLCODE TO EM-SQLRC` grubun son 6 baytını ezer (`lgicdb01.cbl:214`). Henüz SQL çalışmadığı
     için bu SQLCODE'un değeri kodda yok.
   - `WRITE-ERROR-MESSAGE` içindeki `IF EIBCALEN > 0` / `< 91` / `ELSE` dalları COMMAREA'nın ne kadarının
     gönderileceğini seçer (`lgicus01.cbl:151-165`, `lgicdb01.cbl:230-244`). LGICDB01 zincirden çağrıldığında
     `EIBCALEN = 32500` olduğu için `ELSE` dalı (`lgicdb01.cbl:237-242`) çalışır. `< 91` dalına
     (`lgicdb01.cbl:231-236`) yalnızca LGICDB01 doğrudan `EIBCALEN = 90` ile çağrılıp `WHEN OTHER` yoluna
     düşülürse ulaşılır (02-17).
9. **LGSTSQ kuyruk kaydı** (`base/src/lgstsq.cbl`).
   - LINK ile çağrıldığında mesaj `COMMA-DATA`'dan alınır ve uzunluk `EIBCALEN` olur (`lgstsq.cbl:68-71`).
     LINK'te `INVOKINGPROG`'un dolu gelmesi CICS'in kendi tanımıdır, kodda yazmıyor. Terminal yolu
     (`lgstsq.cbl:72-80`) ve `SEND TEXT` (`lgstsq.cbl:113-119`) bu zincirden çalışmaz.
   - Bu operasyonun mesajları tarih ya da `'COMMAREA='` ile başlar. `'Q='` koşulu (`lgstsq.cbl:83`) tutmaz,
     kuyruk adı `GENAERRS` kalır (`lgstsq.cbl:82`).
   - Kayıt = `SYSID` (4) + boşluk + mesaj. Uzunluk = gelen uzunluk + 5 (`lgstsq.cbl:25-28`, `:71`, `:90`).
     Aynı kayıt TD `CSMT`'ye ve TS `GENAERRS`'e yazılır (`lgstsq.cbl:94-111`). İki yazmanın `RESP`'i alınır
     ama kontrol edilmez.
   - `COMMA-DATA` 90 bayt (`lgstsq.cbl:48`). 99 baytlık `CA-ERROR-MSG` geldiğinde yalnızca ilk 90 bayt alınır:
     `'COMMAREA='` + COMMAREA'nın ilk **81** baytı (`lgstsq.cbl:70`). Kayıt uzunluğu 99 + 5 = 104, ama
     `WRITE-MSG` 95 bayt (`lgstsq.cbl:25-28`). Kaydın son 9 baytı `WRITE-MSG`'nin dışından okunur.
   - `ERROR-MSG` mesajları (45 ve 71 bayt) kayda eksiksiz girer.
10. **LGICUS01'in COMMAREA dökümü ölü kod.** LGICUS01'de `WRITE-ERROR-MESSAGE` yalnızca `EIBCALEN = 0`
    dalından çağrılır (`lgicus01.cbl:89`). İçindeki `IF EIBCALEN > 0` (`lgicus01.cbl:151`) her zaman yanlıştır.
    `lgicus01.cbl:152-164` arasına hiçbir girdiyle ulaşılamaz.
11. **WORKING-STORAGE kalıcılığı.**
    - **Birikme yok.** İki program da gerekli uzunluğu her çağrıda önce `MOVE WS-CUSTOMER-LEN TO
      WS-REQUIRED-CA-LEN` ile kurar, sonra 18 ekler (`lgicus01.cbl:102-103`, `lgicdb01.cbl:137-138`).
      Müşteri ekle programlarından farkı budur: LGACUS01 ve LGACDB01, `VALUE +0` olan alanın üstüne ekler
      (`01-customer-add.md`, Kod gözlemleri 16).
    - Sayaç ya da bayrak gibi çağrıdan çağrıya taşınan bir alan yok. `WS-HEADER` (`lgicus01.cbl:79`,
      `lgicdb01.cbl:108`) ve `DB2-IN-INTEGERS` (`lgicdb01.cbl:131`) her çağrıda sıfırlanır. `EM-CUSNUM`
      (`lgicdb01.cbl:148`) ve `CA-DATA` (`lgicdb01.cbl:232`, `:238`) her kullanımda baştan yazılır.
    - Tek uç durum: LGICDB01'in `EIBCALEN = 0` dalı, `EM-VARIABLE`'daki `' CNUM='` ve `' SQLCODE='`
      sabitlerini ezer (`lgicdb01.cbl:120`) ve hiçbir satır bunları geri yazmaz. Bu dal ABEND ile biter
      (`lgicdb01.cbl:122`). Düzenekte ABEND süreci bitirmez ve LGICDB01 aynı WORKING-STORAGE ile tekrar
      çağrılırsa, sonraki `90` hata mesajlarında şunlar görünür: `' CNUM='` yerine `' NO CO'`, `EM-SQLREQ`
      yerine `'EIVED'` + 11 boşluk, `' SQLCODE='` yerine 9 boşluk. Hepsi `' NO COMMAREA RECEIVED'`
      metninden kalır. Düzeneğin bunu nasıl ele alacağı ekip kararıdır (Sercan, Oğuz).

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 02-01 | LGICUS01 çağrılır. `EIBCALEN = 32500` (en az 233). `CA-CUSTOMER-NUM = 0000000001` (müşteri 1: `db2cre.jcl:437`). Db2 yalnızca `db2cre.jcl` ile yüklenmiş. | `CA-RETURN-CODE = 00` | Müşteri 1'in bilgileri döner: Andrew Pandy (`db2cre.jcl:438-439`). Dokuz alanın tam değerleri ve kaynakları aşağıdaki **"02-01 beklenen değerler"** tablosunda.<br>`CA-NUM-POLICIES = 000`. Müşteri 1'in `policy` tablosunda poliçesi olduğu halde (poliçe 10: `db2cre.jcl:777-778`) LGICUS01 bu alanı sıfırlar, LGICDB01 doldurmaz.<br>`CA-REQUEST-ID`, `CA-CUSTOMER-NUM`, `CA-POLICY-DATA` değişmez.<br>`CA-DOB`'un biçimi: **belirsiz**. | `lgicus01.cbl:93-94`, `:109`, `:122-125`<br>`lgicdb01.cbl:126`, `:146`, `:169-190`, `:193-194` | İş kuralı |
| 02-02 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM = 0000000000`. Db2 yalnızca `db2cre.jcl` ile yüklenmiş. `LGSE 0` ile çalıştırılmadıysa (`lgsetup.cbl:128-135`) 0 hiçbir yoldan atanmaz [kod]: JCL'deki numaralar 1-10 (`db2cre.jcl:437`, `:471`, `:505`, `:539`, `:573`, `:607`, `:641`, `:675`, `:709`, `:743`), sayaç 11'den başlar (`lgsetup.cbl:53`, `:183-187`), kimlik değeri 1000001'den başlar (`db2cre.jcl:111-112`). Bu yüzden ekleme testleri önce çalışsa da müşteri 0 oluşmaz. Tek istisna `IDENTITY_VAL_LOCAL` uç durumu (`01-customer-add.md`, Kod gözlemleri 17). (11 kullanılmıyor: ilk başarılı ekleme müşteri 11'i oluşturur, `01-customer-add.md` Kod gözlemleri 17.) SELECT, `SQLCODE = 100` döner. | `CA-RETURN-CODE = 01` | Kod müşteri alanlarına (19-233. baytlar) değer taşımaz ve onları temizlemez. Alanlarda ne kalacağı **belirsiz**.<br>`CA-NUM-POLICIES = 000` (LGICUS01).<br>Hata mesajı yazılmaz. | `lgicdb01.cbl:169-190`, `:195-196`<br>`lgicus01.cbl:94` | İş kuralı |
| 02-03 | LGICUS01 çağrılır. `EIBCALEN = 32500`. [B4] Düzenek SELECT'te `SQLCODE = -913` üretir. | `CA-RETURN-CODE = 01` (bulunamadı ile aynı kod) | Müşteri alanları: **belirsiz** (02-02 ile aynı neden).<br>`CA-NUM-POLICIES = 000`.<br>Hata mesajı yazılmaz: bu dalda `WRITE-ERROR-MESSAGE` çağrılmaz. | `lgicdb01.cbl:197-198`<br>`lgicus01.cbl:94` | İş kuralı |
| 02-04 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM = N`. [B4] Düzenek SELECT'te 0, 100 ve -913 dışında bir `SQLCODE` (negatif ya da 100 dışı pozitif) üretir. | `CA-RETURN-CODE = 90` | Müşteri alanları: **belirsiz**.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır. Her mesaj TD `CSMT`'ye ve TS `GENAERRS`'e yazılır. İkinci mesajdan kuyruğa COMMAREA'nın yalnızca ilk 81 baytı girer (kayıt biçimi: Kod gözlemleri 9).<br>(1) `ERROR-MSG`: tarih, saat, program adı olarak `' LGICUS01'` (LGICDB01'in kendi mesajında bile bu ad yazar), `' CNUM='` + N, `EM-SQLREQ` boşluk (hiç doldurulmuyor), `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı (`EIBCALEN = 32500`, `ELSE` dalı). | `lgicdb01.cbl:199-202`, `:212-228`, `:237-242`<br>`lgicdb01.cbl:44`, `:46-50`, `:148`<br>`lgstsq.cbl:68-71`, `:82-83`, `:94-111` | İş kuralı |
| 02-05 | LGICUS01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGICDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya `' NO COMMAREA RECEIVED'` içeren `ERROR-MSG` gönderilir. COMMAREA içeriği gönderilmez (`EIBCALEN > 0` koşulu sağlanmaz). | `lgicus01.cbl:87-91`, `:88`, `:135-149`, `:151` | Arayüz (COMMAREA) kuralı |
| 02-06 | LGICUS01 çağrılır. `EIBCALEN` 8 ile 89 arasında (sınır değer: `89`). | `CA-RETURN-CODE = 98` | Müşteri alanları doldurulmaz, LGICDB01 çağrılmaz, hata mesajı yazılmaz (`lgicus01.cbl:104-107` arasında `WRITE-ERROR-MESSAGE` yok).<br>Not: Uzunluk kontrolünden **önce** `MOVE '00' TO CA-NUM-POLICIES` çalışır. Bu alan 91-93. baytlarda olduğu için yazma gelen COMMAREA'nın dışına düşer. [B1] Yazma çağıranın tamponuna düşer: tamponun 91-93. baytları `000` olur (ön koşullu) [kod] `lgicus01.cbl:94`. | `lgicus01.cbl:93-94`, `:102-107`<br>`lgicus01.cbl:57`, `lgpolicy.cpy:17` | Arayüz (COMMAREA) kuralı |
| 02-07 | LGICUS01 çağrılır. `EIBCALEN` 1 ile 7 arasında. | [B1] Tamponun 7-8. baytları `98` olur (ön koşullu) [kod] `lgicus01.cbl:105`. Kod `98`'i yazıp `RETURN` yapar. Alan 7-8. baytlarda olduğu için gelen COMMAREA'nın dışında kalır (1-6'da tamamen, 7'de yarısı) ve B1 ile çağıranın tamponuna düşer.<br>Ön koşulsuz kesin olan: `EIBCALEN = 7` ise COMMAREA'nın 7. baytı `9` olur, yani `98`'in ilk hanesi. | Kesin: LGICDB01 çağrılmaz, hata mesajı yazılmaz (`lgicus01.cbl:104-107` arasında `WRITE-ERROR-MESSAGE` yok). 1-6. baytlar (`CA-REQUEST-ID`) değişmez.<br>[B1] COMMAREA dışına yapılan üç yazma çağıranın tamponuna düşer (ön koşullu). Sonuç tamponda: 7-8. baytlar `98`, 91-93. baytlar `000` [kod] `lgicus01.cbl:93-94`, `lgicus01.cbl:105`. | `lgcmarea.cpy:10-11`<br>`lgicus01.cbl:87`, `:93-94`, `:104-107` | Arayüz (COMMAREA) kuralı |
| 02-08 | LGICUS01 çağrılır. `EIBCALEN = 90` (kabul edilen en küçük uzunluk). Müşteri `CUSTOMER` tablosunda var. | `CA-RETURN-CODE = 00` | 90 baytın içinde kalan `CA-FIRST-NAME` … `CA-POSTCODE` dolar (02-01'deki eşleşmeyle).<br>`CA-NUM-POLICIES`, `CA-PHONE-MOBILE`, `CA-PHONE-HOME`, `CA-EMAIL-ADDRESS` (91-233. baytlar) gelen COMMAREA'nın dışına yazılır, çünkü LGICUS01 her zaman `LENGTH(32500)` verir. [B1] Bu yazmalar çağıranın tamponuna düşer (ön koşullu): tamponda `CA-NUM-POLICIES = 000`, telefonlar ve e-posta 02-01'deki değerler olur [kod] `lgicus01.cbl:94`, `lgicdb01.cbl:185-187`.<br>91-232 arası uzunluklarda da benzer durum olur. LGICDB01 doğrudan bu uzunluklarla çağrılırsa da aynısı geçerli. | `lgicus01.cbl:102-104`, `:122-125`<br>`lgicdb01.cbl:179-187`<br>`lgcmarea.cpy:16-25` | Arayüz (COMMAREA) kuralı |
| 02-09 | **LGICDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 71 baytlık `ERROR-MSG`. İçeriği: tarih, saat, `' LGICUS01'`, `' NO COMMAREA RECEIVED'` + 20 boşluk + 6 baytlık `EM-SQLRC`. Taşıma 47 baytlık `EM-VARIABLE` grubunun tamamını ezdiği için `' CNUM='` ve `' SQLCODE='` sabitleri mesajda yer almaz. [B2] `EM-SQLRC` = `+00000` (ön koşullu; henüz SQL çalışmadı ve SQLCA'nın kaynağı `base/` altında yok: Kod gözlemleri 8) [kod] `lgicdb01.cbl:214`. COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz).<br>LGICUS01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 3). | `lgicdb01.cbl:119-123`, `:44-50`, `:212-230` | Arayüz (COMMAREA) kuralı |
| 02-10 | **LGICDB01 doğrudan** çağrılır. `EIBCALEN` 8 ile 89 arasında (sınır değer: `89`). | `CA-RETURN-CODE = 98` | SQL çalışmaz, müşteri alanları doldurulmaz.<br>LGICUS01'den farklı olarak `CA-NUM-POLICIES`'e dokunulmaz.<br>1-7 bayt için 02-07'deki belirsizlik burada da geçerli. | `lgicdb01.cbl:126`, `:137-143`<br>`lgicdb01.cbl:59`, `lgpolicy.cpy:17` | Arayüz (COMMAREA) kuralı |
| 02-11 | **LGICDB01 doğrudan** çağrılır. `EIBCALEN` en az 233. `CA-CUSTOMER-NUM = N`, müşteri `CUSTOMER` tablosunda var. | `CA-RETURN-CODE = 00` | 02-01'deki dokuz alan aynı şekilde dolar.<br>`CA-NUM-POLICIES` **değişmez**: LGICDB01 bu alana yazmaz, sıfırlama LGICUS01'de yapılır. | `lgicdb01.cbl:126`, `:169-194`<br>(karşılaştırma: `lgicus01.cbl:94`) | Arayüz (COMMAREA) kuralı |
| 02-12 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID` herhangi bir değer (örn. boşluk ya da başka bir operasyonun kimliği). Müşteri var. | `CA-RETURN-CODE = 00` (02-01 ile aynı) | 02-01 ile aynı. İstek kimliği sonucu etkilemez. | `lgicus01.cbl:87-109`<br>`lgicdb01.cbl:119-154` | Arayüz (COMMAREA) kuralı |
| 02-13 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM = 1000000001` (10 haneli). Db2 yalnızca `db2cre.jcl` ile yüklenmiş. | [B3] `CA-RETURN-CODE = 00` (ön koşullu) [kod] `lgicdb01.cbl:192-194`. | [B3] `CA-CUSTOMER-NUM` 10 hanelik `PIC 9(10)`, onu alan `DB2-CUSTOMERNUMBER-INT` ise 9 hanelik `PIC S9(9) COMP` [kod] `lgcmarea.cpy:12`, `lgicdb01.cbl:72`, `:146`. B3 ile en soldaki hane kesilir, numara `1` olur ve **müşteri 1 sorgulanır** (ön koşullu): dönen alanlar 02-01 ile aynı (Andrew Pandy). `CA-CUSTOMER-NUM` girdideki gibi `1000000001` kalır.<br>B3 sağlanmazsa sonuç **belirsiz**. | `lgcmarea.cpy:12`<br>`lgicdb01.cbl:72`, `:146`, `:192-194`<br>`cobol.jcl:44-50` | Arayüz (COMMAREA) kuralı |
| 02-14 | LGICUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM` sayısal değil (örn. boşluk). | **belirsiz** | **belirsiz**. İki programda da sayısallık kontrolü yok. Sayısal olmayan değer doğrudan `DB2-CUSTOMERNUMBER-INT`'e taşınır. | `lgicus01.cbl:87-109`<br>`lgicdb01.cbl:146` | Arayüz (COMMAREA) kuralı |
| 02-15 | LGICUS01 çağrılır. `EIBCALEN = 32500`. Müşteri satırı var ama seçilen sütunlardan biri NULL. | **belirsiz** | **belirsiz**. SELECT'te NULL gösterge (indicator) değişkeni yok. Db2'nin bu durumda hangi `SQLCODE`'u döndüreceği kodda yok. 0, 100 ve -913 dışında bir kod dönerse 02-04 yoluna (`90`) düşer.<br>Seçilen sütunların hiçbirinde `NOT NULL` yok (`db2cre.jcl:113-121`), yani tablo bu durumu kabul eder. `db2cre.jcl`'nin örnek müşterilerinde NULL yok (her sütuna değer veriliyor: örn. `db2cre.jcl:437-446`); test için ayrıca oluşturulması gerekir. | `lgicdb01.cbl:179-187`, `:192-203` | İş kuralı |
| 02-16 | LGICUS01 çağrılır. `EIBCALEN = 32500`. LGICDB01'e yapılan LINK başarısız olur (örn. program tanımlı değil). | **belirsiz** | **belirsiz**. LINK'te `RESP`/`NOHANDLE` yok, programda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgicus01.cbl:122-125` | Arayüz (COMMAREA) kuralı |
| 02-17 | **LGICDB01 doğrudan** çağrılır. `EIBCALEN = 90` (uzunluk kontrolünü geçen ve `< 91` koşulunu sağlayan tek değer). `CA-CUSTOMER-NUM = N`. [B4] Düzenek SELECT'te 0, 100 ve -913 dışında bir `SQLCODE` üretir. | `CA-RETURN-CODE = 90` | Müşteri alanları: **belirsiz** (02-04 ile aynı neden; ayrıca bu alanların 94-233. baytları gelen COMMAREA'nın dışında).<br>`CA-NUM-POLICIES`'e dokunulmaz.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: 02-04 ile aynı (`' LGICUS01'`, `' CNUM='` + N, `EM-SQLREQ` boşluk, `' SQLCODE='` + SQLCODE).<br>(2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, yani gelen 90 baytın tamamı. Kuyruğa bunun ilk 81 baytı girer (Kod gözlemleri 9). Bu dala yalnızca bu yolla ulaşılır. | `lgicdb01.cbl:137-140`, `:146-148`, `:169-190`, `:199-202`<br>`lgicdb01.cbl:214`, `:225-236` | Arayüz (COMMAREA) kuralı |

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

## Kapsam tablosu

2026-10-05 kapsam denetiminde onaylanan envanterin her maddesi ve onu kapsayan test numaraları.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| IC-01 | `lgicus01.cbl:87-91` `IF EIBCALEN = 0` → ABEND `LGCA` | 02-05 | |
| IC-02 | `lgicus01.cbl:93-94` `'00'` → `CA-RETURN-CODE`, `CA-NUM-POLICIES` (kontrolden önce) | 02-01, 02-06, 02-07 | 02-06/02-07: alan dışına yazma |
| IC-03 | `lgicus01.cbl:102-103` gerekli uzunluk = 72 + 18 = 90 | 02-06, 02-08 | 89 reddedilir, 90 kabul edilir |
| IC-04 | `lgicus01.cbl:104-107` `IF EIBCALEN < 90` → `98` | 02-06 | |
| IC-05 | `lgicus01.cbl:109`, `:122-125` LINK LGICDB01 `LENGTH(32500)` | 02-01, 02-08, 02-16 | |
| IC-06 | `lgicus01.cbl:114-115` RETURN, LGICDB01'in dönüş kodu kontrol edilmez | 02-01, 02-02, 02-04 | |
| IC-07 | `lgicus01.cbl:151-165` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 02-05 | Dış `IF` yanlış. İç dallar (`lgicus01.cbl:152-164`) ölü kod, hiçbir girdiyle ulaşılamaz (Kod gözlemleri 10) |
| ID-01 | `lgicdb01.cbl:119-123` `IF EIBCALEN = 0` → ABEND `LGCA` | 02-09 | Yalnızca doğrudan çağrı |
| ID-02 | `lgicdb01.cbl:126` `'00'` → `CA-RETURN-CODE` | 02-01, 02-11 | |
| ID-03 | `lgicdb01.cbl:137-143` `IF EIBCALEN < 90` → `98` | 02-10 | Yalnızca doğrudan çağrı |
| ID-04 | `lgicdb01.cbl:146` `CA-CUSTOMER-NUM` → `DB2-CUSTOMERNUMBER-INT` | 02-01, 02-13, 02-14 | |
| ID-05 | `lgicdb01.cbl:148` `CA-CUSTOMER-NUM` → `EM-CUSNUM` | 02-04, 02-17 | |
| ID-06 | `lgicdb01.cbl:169-190` SELECT, NULL göstergesi yok | 02-01, 02-15 | |
| ID-07 | `lgicdb01.cbl:192-194` `WHEN 0` → `00` | 02-01, 02-11 | |
| ID-08 | `lgicdb01.cbl:195-196` `WHEN 100` → `01` | 02-02 | |
| ID-09 | `lgicdb01.cbl:197-198` `WHEN -913` → `01` | 02-03 | |
| ID-10 | `lgicdb01.cbl:199-202` `WHEN OTHER` → `90` | 02-04, 02-17 | 02-15 de bu yola düşebilir |
| ID-11 | `lgicdb01.cbl:159-161` RETURN | 02-01 | |
| ID-12 | `lgicdb01.cbl:230-244` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 02-04 (`ELSE`), 02-17 (`< 91`), 02-09 (dış `IF` yanlış) | |
| SQ-01 | `lgstsq.cbl:68-71` `IF WS-INVOKEPROG NOT = SPACES` (LINK yolu) | 02-04, 02-05, 02-09, 02-17 | |
| SQ-02 | `lgstsq.cbl:72-80` `ELSE` (terminalden RECEIVE) | 02-04 (dal alınmaz) | **Kapsam dışı: bu zincirden ulaşılamaz.** İki program da LGSTSQ'yu LINK ile çağırır. |
| SQ-03 | `lgstsq.cbl:82-88` `IF` mesaj `'Q='` ile başlıyor | 02-04 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** Mesajlar tarih ya da `'COMMAREA='` ile başlar. |
| SQ-04 | `lgstsq.cbl:94-99` WRITEQ TD `CSMT`, RESP kontrol edilmez | 02-04 | |
| SQ-05 | `lgstsq.cbl:105-111` WRITEQ TS `GENAERRS`, RESP kontrol edilmez | 02-04 | |
| SQ-06 | `lgstsq.cbl:113-119` `IF WS-FLAG = 'R'` → SEND TEXT | 02-04 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** `'R'` yalnızca SQ-02 yolunda atanır. |
| N-01 | `CA-REQUEST-ID` okunmaz | 02-12 | Kod gözlemleri 5 |
| N-02 | `CA-CUSTOMER-NUM` için sayısallık ve aralık kontrolü yok | 02-13, 02-14 | |
| N-03 | LINK'te `RESP` yok | 02-16 | |
| N-04 | SELECT'te NULL göstergesi yok | 02-15 | |
| N-05 | `-913`, "bulunamadı" ile aynı kodu (`01`) alır | 02-03 | |
| N-06 | `CA-NUM-POLICIES` gerçek poliçe sayısını yansıtmaz | 02-01 | |

## Ön koşullu satırlar

2026-10-05 A/B güncellemesinde belirsizden ön koşullu tahmine çevrilen satırlar. Ön koşul kodları dosyanın başında
("Kaynak etiketleri ve ön koşul kodları").

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 02-03 | B4 | Girdi düzenekle kurulur; tahmin (`01`) değişmez | Girdi kurulamaz: `-913`'ün testte nasıl üretileceği kodda yok |
| 02-04 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | Girdi kurulamaz |
| 02-06 | B1 | Tamponun 91-93. baytları `000` | Alan dışına yazmanın etkisi belirsiz |
| 02-07 | B1 | Tamponun 7-8. baytları `98`, 91-93. baytları `000` | Dönüş kodu ve alan dışına yazmaların etkisi belirsiz |
| 02-08 | B1 | Tamponda `CA-NUM-POLICIES = 000`, telefonlar ve e-posta 02-01'deki gibi | 91-233. baytlar belirsiz |
| 02-09 | B2 | `EM-SQLRC = +00000` | `EM-SQLRC` belirsiz |
| 02-13 | B3 | Müşteri 1 sorgulanır: `00` ve 02-01'deki değerler | Dönüş kodu ve veri belirsiz |
| 02-17 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | Girdi kurulamaz |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 02-01 | Beklenen veri (yalnızca `CA-DOB` biçimi) | `DATEOFBIRTH` sütunu `DATE` türünde (`db2cre.jcl:115`) ve doğrudan `PIC X(10)` alana okunuyor (`lgicdb01.cbl:172`, `:181`). Değer `'1950-07-11'` olarak ekleniyor (`db2cre.jcl:440`), ama okunurken hangi biçimde geleceği Db2 ayarına bağlı ve kodda yazmıyor. |
| 02-02 | Beklenen veri | SQLCODE 100 dalında kod alanlara dokunmuyor (`lgicdb01.cbl:195-196`). Db2'nin "satır yok" durumunda host değişkenlerine yazıp yazmadığı kodda görünmüyor. |
| 02-03 | Beklenen veri | Veri için 02-02 ile aynı neden. |
| 02-04 | Beklenen veri (müşteri alanları) | Hata durumunda SELECT'in alanlara ne yazdığı kodda görünmüyor. |
| 02-14 | Dönüş kodu, beklenen veri | Sayısal olmayan veri sayısal alana taşınıyor ve hiçbir doğrulama yok. Sonuç tanımsız. |
| 02-15 | Dönüş kodu, beklenen veri | Kodda NULL göstergesi yok. Db2'nin döndüreceği SQLCODE kodda görünmüyor. |
| 02-16 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil. |
| 02-17 | Beklenen veri (müşteri alanları) | Hata durumunda SELECT'in alanlara ne yazdığı kodda görünmüyor. Ayrıca bu alanların 94-233. baytları gelen 90 baytlık COMMAREA'nın dışında. |
