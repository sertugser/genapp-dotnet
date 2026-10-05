# 01 — Müşteri ekle (Add Customer)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Müşteri ekle: yeni müşteri Db2 `CUSTOMER` tablosuna, VSAM `KSDSCUST` dosyasına ve varsayılan şifreyle Db2 `CUSTOMER_SECURE` tablosuna yazılır |
| Programlar (çağrı zinciri) | `LGACUS01` (iş mantığı) → `EXEC CICS LINK` → `LGACDB01` (sayaç + Db2 `CUSTOMER` INSERT) → `LINK` → `LGACVS01` (VSAM yazma) → `LINK` → `LGACDB02` (Db2 `CUSTOMER_SECURE` INSERT). Hata yollarında dördü de `LGSTSQ`'ya LINK eder. |
| Okunan COBOL dosyaları | `base/src/lgacus01.cbl`, `base/src/lgacdb01.cbl`, `base/src/lgacdb02.cbl`, `base/src/lgacvs01.cbl`, `base/src/lgstsq.cbl`, `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy`, `base/src/lgsetup.cbl` (yalnızca sayaç tanımı: Kod gözlemleri 17) |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: yalnızca `CUSTOMER` ve `CUSTOMER_SECURE` tanımları (`:110-144`) ve örnek veri (`:425-763`). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `base/Testing.md`, `base/Architecture.md` (yalnızca Kod gözlemleri 9 ve 17'de; kodda doğrulanmadı) |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-05 |
| Kaynak commit (`git rev-parse HEAD`) | `61c635f25b88a92aa3ee92e799c9039522c2c8d6` |

## Kaynak etiketleri ve ön koşul kodları

2026-10-05 A/B güncellemesinde eklenen ya da değişen tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL, kurulum betiği ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge]**: `base/` altındaki açıklama metinleri (`.md`). Belgeye dayanan bir tahmin kodda doğrulanmamıştır.
- Etiketsiz kaynaklar [kod]'dur.

Ön koşul kodları test düzeneğinin vereceği kararlardır. Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul
sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması) kendisi kurar. |
| B5 | `VALUE`'su olmayan WORKING-STORAGE alanları boşlukla başlar. |
| B6 | Db2'de `DEFAULT` ile daha önce hiç satır eklenmemiştir; ilk kimlik değeri `START WITH` değeridir. |
| B7 | `LGSE` parametresiz çalıştırıldı, 01-01'den hemen önce (diğer satırlarda: o testten hemen önce); arada başka `GET COUNTER` yok. K = 11 buna bağlı ([kod] `lgsetup.cbl:53`, `:183-187`). |

(B3 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası** (`lgcmarea.cpy:10-26`). LGACUS01, LGACDB01 ve LGACVS01 bu düzeni kullanır
   (`lgacus01.cbl:69-70`, `lgacdb01.cbl:117-120`, `lgacvs01.cbl:55-56`):

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

2. **LGACDB02 kendi COMMAREA düzenini kullanır.** `LGCMAREA` satırları yorum içinde (`lgacdb02.cbl:104-107`),
   düzen `lgacdb02.cbl:95-102`'de. LGACDB01 bu düzeni kendi `CDB2AREA` alanında kurar (`lgacdb01.cbl:58-65`)
   ve LGACDB02'ye onu geçirir. Müşterinin COMMAREA'sı LGACDB02'ye hiç gitmez.

   | Bayt | Alan | PIC |
   |---|---|---|
   | 1-6 | `D2-REQUEST-ID` | X(6) |
   | 7-8 | `D2-RETURN-CODE` | 9(2) |
   | 9-18 | `D2-CUSTOMER-NUM` | 9(10) |
   | 19-50 | `D2-CUSTSECR-PASS` | X(32) |
   | 51-54 | `D2-CUSTSECR-COUNT` | X(4) |
   | 55 | `D2-CUSTSECR-STATE` | X |
   | 56-32500 | `D2-CUSTSECR-DATA` | X(32445) |

3. **Minimum COMMAREA uzunluğu 90 bayttır.** Hesap: `WS-CA-HEADER-LEN` (18) + `WS-CUSTOMER-LEN` (72,
   `lgpolicy.cpy:17`). Kaynak: `lgacus01.cbl:56`, `:108-109`, `lgacdb01.cbl:83`, `:161-162`. Kontrol:
   `lgacus01.cbl:112`, `lgacdb01.cbl:165`. Bu 90 bayt yalnızca `CA-POSTCODE`'a kadar olan alanları kapsar.
   Ama LGACDB01, INSERT için 94-233. baytlardaki `CA-PHONE-MOBILE`, `CA-PHONE-HOME` ve `CA-EMAIL-ADDRESS`'i
   **okur** (`lgacdb01.cbl:241-243`). LGACUS01 ise 91-93. bayttaki `CA-NUM-POLICIES`'e **yazar** (`lgacus01.cbl:103`).
   LGACDB02'de uzunluk kontrolü hiç yok (`lgacdb02.cbl:131-148` arasında yalnızca `EIBCALEN = 0` kontrolü var).
   LGACVS01'de ise `EIBCALEN` doğrulama amacıyla kontrol edilmez (`lgacvs01.cbl:66` yalnızca kopyalar); hata mesajı
   yordamı onu yalnızca döküm boyunu seçmek için kullanır (`lgacvs01.cbl:106-107`, `lgucvs01.cbl:121-122`).
4. **Programlar birbirini sabit uzunlukla çağırır.**
   - LGACUS01 → LGACDB01: `LENGTH(32500)` (`lgacus01.cbl:134-137`). LGACDB01 her zaman `EIBCALEN = 32500`
     görür. Bu yüzden LGACDB01'in `EIBCALEN = 0` ve kısa COMMAREA kontrollerine (`lgacdb01.cbl:149-153`,
     `:165-168`) zincirden ulaşılamaz.
   - LGACDB01 → LGACVS01: `LENGTH(225)` (`lgacdb01.cbl:174-177`).
   - LGACDB01 → LGACDB02: `CDB2AREA` ile `LENGTH(32500)` (`lgacdb01.cbl:186-189`). `D2-REQUEST-ID` her zaman
     `'02ACUS'` (`lgacdb01.cbl:180`). Bu yüzden LGACDB02'nin `WHEN OTHER` dalına (`lgacdb02.cbl:149-151`)
     zincirden ulaşılamaz.
5. **Çağrılan programın dönüş kodu kontrol edilmez.** Hiçbir LINK'te `RESP`/`NOHANDLE` yok.
   - LGACUS01, LINK'ten sonra doğrudan `EXEC CICS RETURN` yapar (`lgacus01.cbl:119`, `:123`, `:134-137`).
   - LGACDB01, LGACVS01'in ve LGACDB02'nin sonucuna bakmaz (`lgacdb01.cbl:174-192`). LGACDB02'nin yazdığı
     `D2-RETURN-CODE`, `CDB2AREA` içinde kalır ve `CA-RETURN-CODE`'a hiç taşınmaz.
6. **`CA-REQUEST-ID` hiçbir programda okunmaz.** LGACUS01, LGACDB01 ve LGACVS01'in hiçbir satırı bu alana
   bakmaz ya da yazmaz.
7. **Girdideki `CA-CUSTOMER-NUM` kullanılmaz.** Numara sayaçtan (`lgacdb01.cbl:210`) ya da Db2 kimlik
   değerinden (`lgacdb01.cbl:263`, `:280-282`) gelir. Başarılı INSERT'ten sonra alana yazılır (`lgacdb01.cbl:285`).
   INSERT hata verirse `:285`'ten önce RETURN yapılır (`:248`, `:277`), alan girdideki gibi kalır.
8. **Alan doğrulaması yok.** Ad, soyad, doğum tarihi, adres, telefon ve e-posta hiçbir programda kontrol
   edilmez, doğrudan INSERT'e gider (`lgacdb01.cbl:235-243`, `:264-272`). `CA-DOB` X(10), `DATE` türündeki
   sütuna yazılır (`db2cre.jcl:115`); değeri kabul edip etmemek Db2'ye kalır. Diğer metin alanlarının
   genişliği tablo sütunlarıyla aynı (`lgcmarea.cpy:16-25` ↔ `db2cre.jcl:113-121`). Tabloda müşteri
   numarası dışında tekillik kuralı yok (`db2cre.jcl:122`), aynı kişi iki kez eklenebilir.
9. **Hiçbir programda `SYNCPOINT` ya da `ROLLBACK` yok.** LGACVS01 abend ederse (`lgacvs01.cbl:79`) önceden
   yapılan `CUSTOMER` INSERT'inin (`lgacdb01.cbl:222-244` ya da `:251-273`) geri alınıp alınmayacağı kodda yazmıyor.
   [belge] `base/Testing.md:29-30`: ekleme Db2'ye ve VSAM'a iki aşamalı onayla (two-phase commit) yazılır; "VSAM
   güncellemesi başarısız olursa Db2 güncellemesi geri alınır". `base/Architecture.md:48` aynı şeyi genel olarak söyler.
   Bu belge ifadesi kodda doğrulanmadı. 01-05'teki "geri alınır" tahmini buna dayanır.
10. **Dönüş kodları ve anlamları.** Aynı `98` kodu iki farklı anlamda kullanılıyor.

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgacus01.cbl:102`, `lgacdb01.cbl:156` | Başlangıç / başarı | Evet |
    | `98` | `lgacus01.cbl:113` | COMMAREA kısa | Evet |
    | `98` | `lgacdb01.cbl:166` | COMMAREA kısa | Yalnızca LGACDB01 doğrudan çağrılırsa |
    | `98` | `lgacdb02.cbl:179` | `CUSTOMER_SECURE` SQL hatası | Hayır (gözlem 5) |
    | `90` | `lgacdb01.cbl:246`, `:275` | `CUSTOMER` SQL hatası | Evet |
    | `99` | `lgacdb02.cbl:150` | Bilinmeyen istek kimliği | Hayır (gözlem 4) |
    | `80` | `lgacvs01.cbl:77` | VSAM yazma hatası | Alana yazılır, hemen ardından `ABEND LGV0` |
    | ABEND `LGCA` | `lgacus01.cbl:98`, `lgacdb01.cbl:152`, `lgacdb02.cbl:134` | COMMAREA yok | — |
    | ABEND `LGV0` | `lgacvs01.cbl:79` | VSAM yazma hatası | — |

11. **VSAM kaydının son 8 baytı geçirilen alanın dışında.** LGACVS01 kaydı `FROM(CA-Customer-Num)`
    `LENGTH(225)` ile yazar (`lgacvs01.cbl:68-74`, `:49`). Bu, COMMAREA'nın 9-233. baytlarıdır: `CA-CUSTOMER-NUM`
    … `CA-EMAIL-ADDRESS`, `CA-NUM-POLICIES` dahil. Anahtar ilk 10 bayttır (`KEYLENGTH(10)`). Ama LGACDB01
    LGACVS01'e yalnızca `LENGTH(225)`, yani 1-225. baytları geçirir (`lgacdb01.cbl:176`). Kaydın son 8 baytı
    (226-233, e-postanın son 8 karakteri) geçirilen alanın dışında kalır.
12. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGACUS01 45 bayt (`lgacus01.cbl:39-44`), LGACDB01 71 (`lgacdb01.cbl:46-56`),
      LGACDB02 71 (`lgacdb02.cbl:44-54`), LGACVS01 85 (`lgacvs01.cbl:30-43`). İkinci mesaj `CA-ERROR-MSG`
      dördünde de 99 bayttır: `'COMMAREA='` + 90 bayt.
    - LGACDB01 ve LGACDB02'de `' CNUM='` alanı hiç doldurulmaz: `EM-CUSNUM` (`lgacdb01.cbl:53`,
      `lgacdb02.cbl:51`) hiçbir satırda taşınmıyor ve boşluk kalır. LGACVS01 bu alanı doldurur (`lgacvs01.cbl:99`).
    - LGACDB01/LGACDB02'nin `EIBCALEN = 0` mesajı: 21 karakterlik `' NO COMMAREA RECEIVED'`, 47 baytlık
      `EM-VARIABLE` grubuna taşınır (`lgacdb01.cbl:150`, `lgacdb02.cbl:132`). Sonra `MOVE SQLCODE TO EM-SQLRC`
      grubun son 6 baytını ezer (`lgacdb01.cbl:297`, `lgacdb02.cbl:194`). Henüz SQL çalışmadığı için
      bu SQLCODE'un değeri kodda yok.
    - `WRITE-ERROR-MESSAGE` içindeki `IF EIBCALEN > 0` / `< 91` / `ELSE` dalları COMMAREA'nın ne kadarının
      gönderileceğini seçer (`lgacus01.cbl:164-178`, `lgacdb01.cbl:313-327`, `lgacdb02.cbl:210-224`,
      `lgacvs01.cbl:106-120`).
13. **LGSTSQ kuyruk kaydı** (`lgstsq.cbl`).
    - LINK ile çağrıldığında mesaj `COMMA-DATA`'dan alınır ve uzunluk `EIBCALEN` olur (`:68-71`). LINK'te
      `INVOKINGPROG`'un dolu gelmesi CICS'in kendi tanımıdır, kodda yazmıyor.
    - Bu operasyonun mesajları tarih ya da `'COMMAREA='` ile başlar. `'Q='` koşulu (`:83`) tutmaz,
      kuyruk adı `GENAERRS` kalır (`:82`).
    - Kayıt = `SYSID` (4) + boşluk + mesaj. Uzunluk = gelen uzunluk + 5 (`:25-28`, `:71`, `:90`). Aynı kayıt
      TD `CSMT`'ye ve TS `GENAERRS`'e yazılır (`:94-111`). Her iki yazmanın `RESP`'i alınır ama kontrol edilmez.
    - `COMMA-DATA` 90 bayt (`:48`). 99 baytlık `CA-ERROR-MSG` geldiğinde yalnızca ilk 90 bayt alınır: `'COMMAREA='`
      + COMMAREA'nın ilk **81** baytı (`:70`). Kayıt uzunluğu 99 + 5 = 104, ama `WRITE-MSG` 95 bayt (`:25-28`).
      Kaydın son 9 baytı `WRITE-MSG`'nin dışından okunur.
    - `ERROR-MSG` mesajları (45-85 bayt) kayda eksiksiz girer.
14. **Kullanılmayan alanlar.** `WS-CS-PASSWORD` (`'NewPass'`), `WS-CS-STATE` ve `WS-CA-COUNT` tanımlı
    (`lgacdb01.cbl:74-76`) ama hiçbir satırda kullanılmıyor. `CUSTOMER_SECURE`'a giden şifre `'NewPass'` değil,
    `lgacdb01.cbl:181-182`'deki 32 karakterlik sabit `'5732fec825535eeafb8fac50fee3a8aa'`. `db2cre.jcl`'deki
    örnek müşterilerin hepsinde de aynı değer var (örn. `db2cre.jcl:455`).
15. **LGACUS01'in COMMAREA dökümü ölü kod.** LGACUS01'de `WRITE-ERROR-MESSAGE` yalnızca `EIBCALEN = 0` dalından
    çağrılır (`lgacus01.cbl:97`). İçindeki `IF EIBCALEN > 0` (`:164`) her zaman yanlıştır. `:165-177` arasına
    hiçbir girdiyle ulaşılamaz.
16. **WORKING-STORAGE kalıcılığı.**
    - `WS-REQUIRED-CA-LEN`, `VALUE +0` ile başlar (`lgacus01.cbl:57`, `lgacdb01.cbl:84`). Her çağrıda üstüne
      18 + 72 eklenir (`lgacus01.cbl:108-109`, `lgacdb01.cbl:161-162`). Kodda sıfırlama yok.
    - `LGAC-NCS`, `VALUE 'ON'` ile başlar (`lgacdb01.cbl:73`). Sayaç hatasında `'NO'` olur (`:207`) ve hiçbir
      satır onu geri `'ON'` yapmaz.
    - Sonuç: Programlar aynı süreçte WORKING-STORAGE korunarak tekrar çağrılırsa, ikinci çağrıda eşik 180,
      üçüncüde 270 olur. Bir sayaç hatasından sonraki bütün eklemeler `DEFAULT` yoluna (`:250-283`) gider.
    - `EM-VARIABLE` uç durumu (LGACDB01, LGACDB02):
      - İki programın `EIBCALEN = 0` dalı, 21 karakterlik `' NO COMMAREA RECEIVED'` metnini 47 baytlık
        `EM-VARIABLE` grubuna taşır (`lgacdb01.cbl:150`, `lgacdb02.cbl:132`). Grubun tanımı: `lgacdb01.cbl:51-56`,
        `lgacdb02.cbl:49-54`.
      - Bu taşıma `' CNUM='` ve `' SQLCODE='` sabitlerini ezer, hiçbir satır onları geri yazmaz. Dal ABEND ile biter
        (`lgacdb01.cbl:152`, `lgacdb02.cbl:134`).
      - Düzenekte ABEND süreci bitirmez ve program aynı WORKING-STORAGE ile tekrar çağrılırsa, o programın sonraki
        hata mesajlarında şunlar görünür: `' CNUM='` yerine `' NO CO'`; `EM-CUSNUM` yerine `'MMAREA REC'` (bu iki
        programda `EM-CUSNUM` hiç doldurulmadığı için eski metin kalır, Kod gözlemleri 12); `' SQLCODE='` yerine
        9 boşluk. Bunlar 01-03, 01-04, 01-06, 01-18 ve 01-22'deki mesajları etkiler.
      - `EM-SQLREQ` ve `EM-SQLRC` etkilenmez, çünkü her hata yolunda yeniden yazılırlar (`lgacdb01.cbl:219`,
        `:297`; `lgacdb02.cbl:165`, `:194`).
      - Düzeneğin bunu nasıl ele alacağı ekip kararıdır.
    - Bu dosyadaki tahminler WORKING-STORAGE'ın `VALUE` değerleriyle başladığı duruma göre yazıldı. Düzeneğin
      bunu nasıl sağlayacağı (CANCEL / yeni süreç) ekip kararıdır (Sercan, Oğuz).
17. **Test verisi** (`base/cntl/db2cre.jcl`). Bu değerler yalnızca test veritabanı bu JCL ile yüklendiyse
    ve sonradan değiştirilmediyse geçerlidir.
    - `CUSTOMER` (`:110-124`): `customerNumber INTEGER NOT NULL GENERATED BY DEFAULT AS IDENTITY
      (START WITH 1000001, INCREMENT BY 1, CACHE 20)` (`:111-112`). Birincil anahtar `customerNumber` (`:122`).
    - `CUSTOMER_SECURE` (`:130-140`): `customerNumber INTEGER NOT NULL`, `customerPass CHAR(32)`,
      `state_indicator CHAR(1)`, `pass_changes INTEGER` (`:131-134`). Birincil anahtar `customerNumber` (`:135`).
      `CUSTOMER`'a yabancı anahtar, `ON DELETE CASCADE` (`:136-138`).
    - Örnek veri: 10 müşteri, numaraları elle verilmiş: 1-10 (`:437`, `:471`, `:505`, `:539`, `:573`, `:607`,
      `:641`, `:675`, `:709`, `:743`). Her birinin `CUSTOMER_SECURE` satırı var (`:454`, `:488`, `:522`, `:556`,
      `:590`, `:624`, `:658`, `:692`, `:726`, `:760`). Hepsinde değerler `'5732fec825535eeafb8fac50fee3a8aa'`,
      `'N'`, `0` (örn. `:454-457`).
    - Numaralar elle verildiği için kimlik değeri bu JCL'de hiç üretilmemiş. `DEFAULT` yolunda üretilecek ilk
      değer `START WITH`'e göre 1000001'dir (01-02, ön koşul B6).
    - **Sayacın başlangıç değeri** (`GENACUSTNUM` / havuz `GENA`, `lgacdb01.cbl:38-39`). Önceki sürümde "kodda da
      JCL'de de yok: belirsiz" yazıyordu. Bu yanlıştı: değer `base/src/lgsetup.cbl`'de, o dosya okunmamıştı.
      - [kod] LGSETUP programı sayacı siler ve `Value(LastCustNum)` ile yeniden tanımlar (`lgsetup.cbl:179-187`).
        `LastCustNum`'un başlangıç değeri `+0000011` (`lgsetup.cbl:53`). İşleme parametre verilirse o değer kullanılır
        (`lgsetup.cbl:128-135`). Programın açıklaması: "Recreate Named Counter GENACUSTNUM for next available customer"
        (`lgsetup.cbl:8`).
      - [belge] Uygulamayı denerken ilk adım `LGSE` işlemini çalıştırmaktır; bu işlem sayaç sunucusunu da temizler
        (`base/Testing.md:11-13`).
      - Sonuç: `LGSE` parametresiz çalıştırıldıysa ilk başarılı ekleme **müşteri 11**'i oluşturur (01-01). İlk
        `GET COUNTER`'ın 11'i döndürmesi `lgsetup.cbl:8`'deki açıklamaya ve CICS'in `GET COUNTER` tanımına dayanır.
      - Sayaç `CUSTOMER`'da zaten var olan bir numara döndürürse INSERT başarısız olur (01-03).
      - Bu yüzden 02 ve 03'teki "olmayan müşteri" satırları 11 yerine `0000000000` kullanır. JCL 1-10'u verir, sayaç
        11'den, kimlik değeri 1000001'den başlar; `LGSE 0` ile çalıştırılmadıysa (`lgsetup.cbl:128-135`) 0 hiçbir
        yoldan atanmaz. Tek istisna bir sonraki maddedeki
        `IDENTITY_VAL_LOCAL` uç durumudur.
      - **`IDENTITY_VAL_LOCAL` uç durumu.** `DEFAULT` yolunda numara `IDENTITY_VAL_LOCAL()` ile alınır ve bu SET'ten
        sonra SQLCODE kontrol edilmez (`lgacdb01.cbl:280-282`). SET sessizce başarısız olursa numara başlangıçtaki 0
        değerinde kalır (`lgacdb01.cbl:143`). 0 `CUSTOMER`'da yoktur, bu yüzden ardından gelen `CUSTOMER_SECURE` INSERT'i
        yabancı anahtar yüzünden hata verebilir (`db2cre.jcl:135-138`). SET'in başarısız olup olmayacağı kodda yok:
        **belirsiz**.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 01-01 | LGACUS01 çağrılır. `EIBCALEN = 32500`. `CA-CUSTOMER-NUM = 0000000000`. Müşteri alanlarında müşteri 1'in değerleri var (`db2cre.jcl:438-446`; alan alan aşağıdaki **"01-01 girdi ve beklenen değerler"** tablosunda). Db2 yalnızca `db2cre.jcl` ile yüklenmiş. `LGSE` parametresiz çalıştırılmış ve sayaç sunucusu tanımlı ([belge] `base/Testing.md:11-13`). [B7] Sayaç `NORMAL` döner ve K = 11 değerini verir ([kod] `lgsetup.cbl:53`, `:183-187`; Kod gözlemleri 17). 11 `CUSTOMER`'da yok. | `CA-RETURN-CODE = 00` | [B7] `CA-CUSTOMER-NUM = 0000000011` (ön koşullu) [kod] `lgsetup.cbl:53`, `:183-187`, `lgacdb01.cbl:210`, `:285`. Aşağıda K = 11.<br>`CA-NUM-POLICIES = 000`. Diğer bütün alanlar girdideki gibi kalır.<br>Yan etkiler: `CUSTOMER`'a K numaralı satır eklenir. `KSDSCUST`'a K anahtarlı 225 baytlık kayıt yazılır. Son 8 bayt [B1] boşluk olur, çünkü girdideki e-postanın son 8 karakteri boşluk (ön koşullu; Kod gözlemleri 11). `CUSTOMER_SECURE`'a (K, `'5732fec825535eeafb8fac50fee3a8aa'`, `'N'`, `0`) eklenir. Hata mesajı yazılmaz.<br>Not: Aynı kişi müşteri 1 olarak zaten var (`db2cre.jcl:437-446`). Kodda kişi tekrarı kontrolü yok, yeni kayıt açılır. | `lgacus01.cbl:102-103`, `:112`, `:119`, `:123`, `:134-137`<br>`lgacdb01.cbl:156`, `:206`, `:209-210`, `:221-245`, `:285`, `:174-189`, `:192`<br>`lgacvs01.cbl:68-75`, `:85-87`<br>`lgacdb02.cbl:138`, `:143-148`, `:166-178`, `:155`<br>`lgsetup.cbl:53`, `:183-187`<br>[belge] `base/Testing.md:11-13` | İş kuralı |
| 01-02 | 01-01 ile aynı, ama [B4] düzenek `GET COUNTER`'da `NORMAL` dışında bir RESP üretir. | `CA-RETURN-CODE = 00` | `CA-CUSTOMER-NUM` = Db2'nin `DEFAULT` ile ürettiği kimlik değeri. [B6] Bu değer `0001000001` olur (ön koşullu) [kod] `db2cre.jcl:111-112` (`START WITH 1000001`). B6 sağlanmazsa daha büyük bir değer olur ve tam değeri belirsizdir.<br>**Sayaç hatası sessizce geçilir:** hata mesajı yazılmaz, dönüş kodu değişmez. Numara Db2'ye bırakılır.<br>Yan etkiler 01-01 ile aynı, bu numarayla.<br>`IDENTITY_VAL_LOCAL()` sonrasında SQLCODE kontrol edilmez. | `lgacdb01.cbl:201-208`, `:221`, `:250-274`, `:280-282`, `:285` | İş kuralı |
| 01-03 | 01-01 ile aynı. Ön koşul: `LGSE 1` (parametre sayacın başlangıç değeri olur: [kod] `lgsetup.cbl:128-135`) ya da [B4] düzenek sayacı `CUSTOMER`'da zaten var olan bir numaraya kurar. Sayaç K döner ve K `CUSTOMER`'da zaten var (örn. K = 1: `db2cre.jcl:437`). `customerNumber` birincil anahtar olduğu için (`db2cre.jcl:122`) INSERT `SQLCODE ≠ 0` döner. | `CA-RETURN-CODE = 90` | `CA-CUSTOMER-NUM` girdideki gibi kalır (`0000000000`). `CA-NUM-POLICIES = 000`.<br>LGACVS01 ve LGACDB02 çağrılmaz: `KSDSCUST`'a ve `CUSTOMER_SECURE`'a yazılmaz.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır (kayıt biçimi: Kod gözlemleri 13).<br>(1) `ERROR-MSG`: tarih, saat, `' LGACDB01'`, `' CNUM='` + 10 boşluk (numara yazılmıyor), `' INSERT CUSTOMER'`, `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı (`EIBCALEN = 32500`, `ELSE` dalı). | `lgacdb01.cbl:219`, `:221-249`, `:297`, `:308-313`, `:320-325`<br>`lgstsq.cbl:68-71`, `:82-83`, `:94-111`, `:113` | İş kuralı |
| 01-04 | 01-01 ile aynı. Sayaç `NORMAL` dışında bir RESP döner (01-02 gibi). [B4] Düzenek, `DEFAULT` ile yapılan INSERT'te `SQLCODE ≠ 0` üretir. | `CA-RETURN-CODE = 90` | 01-03 ile aynı: `CA-CUSTOMER-NUM` değişmez, LGACVS01 ve LGACDB02 çağrılmaz, aynı iki hata mesajı yazılır.<br>`IDENTITY_VAL_LOCAL()` çalışmaz (RETURN ondan önce). | `lgacdb01.cbl:206-208`, `:250-278` | İş kuralı |
| 01-05 | 01-01 ile aynı. `CUSTOMER` INSERT başarılı, ama [B4] düzenek `KSDSCUST`'a yapılan WRITE'ta `NORMAL` dışında bir RESP üretir. | Dönüş kodu yok. LGACVS01 `CA-RETURN-CODE`'a `80` yazar, hemen ardından görev `ABEND` kodu `LGV0` ile (NODUMP) sonlanır. LGACDB01'de ve LGACUS01'de abend yakalama (`HANDLE ABEND`) yok. Çağıranın `80`'i görüp görmeyeceği **belirsiz**. | COMMAREA normal dönmez. LGACDB02 çağrılmaz: `CUSTOMER_SECURE` satırı yazılmaz.<br>Önceden eklenen `CUSTOMER` satırı **geri alınır**, satır kalmaz [belge] `base/Testing.md:29-30` (ekleme için açık ifade; kodda `SYNCPOINT`/`ROLLBACK` yok, kodda doğrulanmadı: Kod gözlemleri 9).<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: tarih, saat, `' LGACVS01'`, `' CNUM='` + K ([B7] `0000000011`), `' Write file KSDSCUST'`, `' RESP='` + RESP, `' RESP2='` + RESP2.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + LGACVS01'e gelen 225 baytın ilk 90'ı (`EIBCALEN = 225`, `ELSE` dalı). İçinde `80` ve K da var. | `lgacdb01.cbl:174-177`, `:186`<br>`lgacvs01.cbl:68-81`, `:99-105`, `:113-118`<br>[belge] `base/Testing.md:29-30` | İş kuralı |
| 01-06 | 01-01 ile aynı. `CUSTOMER` INSERT ve `KSDSCUST` WRITE başarılı, ama LGACDB02'deki `CUSTOMER_SECURE` INSERT `SQLCODE ≠ 0` döner. Zincirde bu durumu oluşturacak bir veri yolu yok; [B4] düzenek bu INSERT'te `SQLCODE ≠ 0` üretir. | `CA-RETURN-CODE = 00`<br>LGACDB02 kendi alanına `98` yazar, ama LGACDB01 bunu kontrol etmez ve çağırana taşımaz. **Şifre kaydı yazılamasa da çağıran başarı görür.** | `CA-CUSTOMER-NUM = K` ([B7] `0000000011`). `CUSTOMER` satırı ve `KSDSCUST` kaydı var, `CUSTOMER_SECURE` satırı yok.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: tarih, saat, `' LGACDB02'`, `' CNUM='` + 10 boşluk, `' INSERT SECURITY'`, `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + `CDB2AREA`'nın ilk 90 baytı: `'02ACUS'`, `'98'`, K ([B7] `0000000011`), şifre, `'0000'`, `'N'` ve `D2-CUSTSECR-DATA`'nın ilk 35 baytı: [B5] 35 boşluk (ön koşullu; alanın `VALUE`'su yok [kod] `lgacdb01.cbl:65`). Kuyruğa bu 90 bayttan yalnızca ilk 81'i girer, yani `D2-CUSTSECR-DATA`'nın 35 baytından 26'sı (81 bayt sınırı: [kod] `lgstsq.cbl:48`, `:70`; Kod gözlemleri 13). | `lgacdb01.cbl:179-189`, `:192`<br>`lgacdb02.cbl:138`, `:178-182`, `:205-208`, `:217-222` | İş kuralı |
| 01-07 | 01-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000001` (var olan müşteri 1: `db2cre.jcl:437`). | `CA-RETURN-CODE = 00` | Girdideki numara hiç kullanılmaz. `CA-CUSTOMER-NUM` yeni numarayla ezilir: [B7] `0000000011` (ön koşullu). Müşteri 1'e dokunulmaz. Geri kalanı 01-01 ile aynı (B1'e bağlı VSAM kuyruğu, yani kaydın son 8 baytı, hariç). | `lgacdb01.cbl:210`, `:234`, `:285` | İş kuralı |
| 01-08 | 01-01 ile aynı, ama `CA-FIRST-NAME` ve `CA-LAST-NAME` boşluk. | `CA-RETURN-CODE = 00` | Hiçbir programda alan doğrulaması yok. `CUSTOMER` satırı ad ve soyad boşluk olarak eklenir (JCL de bir `CHAR` sütununa boşluk ekliyor: `db2cre.jcl:441`). Numara [B7] `0000000011` (ön koşullu). Geri kalanı 01-01 ile aynı (B1'e bağlı VSAM kuyruğu, yani kaydın son 8 baytı, hariç). | `lgacus01.cbl:95-123`<br>`lgacdb01.cbl:222-245` | İş kuralı |
| 01-09 | 01-01 ile aynı, ama `CA-DOB` = 10 boşluk. | **belirsiz**. COBOL tarihi doğrulamaz, değer doğrudan `DATE` sütununa gider (`db2cre.jcl:115`). Db2 reddederse `90` (01-03'teki yol), kabul ederse `00`. | **belirsiz** | `lgacdb01.cbl:227`, `:237`, `:245-249` | İş kuralı |
| 01-10 | LGACUS01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGACDB01 çağrılmaz, Db2'ye ve VSAM'a yazılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGACUS01'`, `' NO COMMAREA RECEIVED'`). COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz). | `lgacus01.cbl:95-99`, `:159-164` | Arayüz (COMMAREA) kuralı |
| 01-11 | LGACUS01 çağrılır. `EIBCALEN` 8 ile 89 arasında (sınır değer: `89`). | `CA-RETURN-CODE = 98` | LGACDB01 çağrılmaz, Db2'ye ve VSAM'a yazılmaz, hata mesajı yazılmaz.<br>Not: Uzunluk kontrolünden **önce** `MOVE '00' TO CA-NUM-POLICIES` çalışır. Bu alan 91-93. baytlarda olduğu için yazma gelen COMMAREA'nın dışına düşer. [B1] Yazma çağıranın tamponuna düşer: tamponun 91-93. baytları `000` olur (ön koşullu) [kod] `lgacus01.cbl:103`. | `lgacus01.cbl:102-103`, `:108-109`, `:112-115`<br>`lgpolicy.cpy:17` | Arayüz (COMMAREA) kuralı |
| 01-12 | LGACUS01 çağrılır. `EIBCALEN` 1 ile 7 arasında. | [B1] Tamponun 7-8. baytları `98` olur (ön koşullu) [kod] `lgacus01.cbl:113`. Kod `98`'i yazıp `RETURN` yapar. Alan 7-8. baytlarda olduğu için gelen COMMAREA'nın dışında kalır (1-6'da tamamen, 7'de yarısı) ve B1 ile çağıranın tamponuna düşer.<br>Ön koşulsuz kesin olan: `EIBCALEN = 7` ise COMMAREA'nın 7. baytı `9` olur, yani `98`'in ilk hanesi. | Kesin: LGACDB01 çağrılmaz, Db2'ye ve VSAM'a yazılmaz, hata mesajı yazılmaz (`lgacus01.cbl:112-115` arasında `WRITE-ERROR-MESSAGE` yok). 1-6. baytlar (`CA-REQUEST-ID`) değişmez.<br>[B1] COMMAREA dışına yapılan üç yazma çağıranın tamponuna düşer (ön koşullu). Sonuç tamponda: 7-8. baytlar `98`, 91-93. baytlar `000` [kod] `lgacus01.cbl:102-103`, `lgacus01.cbl:113`. | `lgcmarea.cpy:10-11`<br>`lgacus01.cbl:95`, `:102-103`, `:112-115` | Arayüz (COMMAREA) kuralı |
| 01-13 | LGACUS01 çağrılır. `EIBCALEN = 90` (kabul edilen en küçük uzunluk). Diğer girdiler 01-01 ile aynı. | [B1] `CA-RETURN-CODE = 00` (ön koşullu). B1 ile INSERT'e giden 94-233. baytlar boşluktur; `CHAR` sütunlarına boşluk yazılabildiğini JCL de gösteriyor [kod] `db2cre.jcl:441`, `:479`.<br>B1 sağlanmazsa sonuç belirsiz: kod yollarına göre `00` (01-01 yolu) ya da `90` (01-03 yolu) dönebilir [kod] `lgacdb01.cbl:245-249`, ama COMMAREA dışından okuma ortama göre abend de üretebilir. | Kesin olanlar:<br>• Uzunluk kontrolü geçer (`90 < 90` yanlış). LGACDB01 çağrılır ve `EIBCALEN = 32500` görür.<br>• INSERT'e giden ad … posta kodu (19-90. baytlar) COMMAREA'nın içindedir, girdideki değerlerdir.<br>• `CA-CUSTOMER-NUM` (9-18. baytlar) içeridedir. Sonuç `00` olursa K olur.<br>[B1] ile (ön koşullu):<br>• LGACDB01 `CA-PHONE-MOBILE`, `CA-PHONE-HOME` ve `CA-EMAIL-ADDRESS`'i (94-233. baytlar) gelen COMMAREA'nın dışından, yani tampondan okur: üçü de boşluk. `CUSTOMER` satırında bu üç sütun boşluk olur [kod] `lgacdb01.cbl:241-243`.<br>• `KSDSCUST` kaydında 91-93. baytlar `000` (LGACUS01 bunu `lgacus01.cbl:103`'te tampona yazar), 94-233. baytlar boşluk.<br>• Müşteri numarası [B7] `0000000011`.<br>91-232 arası uzunluklarda, tamponun `EIBCALEN` ötesindeki baytlar için aynı kural geçerli. | `lgacus01.cbl:103`, `:112`, `:134-137`<br>`lgacdb01.cbl:165`, `:235-243`, `:245-249`, `:285`, `:174-177`<br>`lgcmarea.cpy:12`, `:16-25` | Arayüz (COMMAREA) kuralı |
| 01-14 | 01-01 ile aynı, ama `CA-REQUEST-ID` herhangi bir değer (örn. boşluk). | `CA-RETURN-CODE = 00` (01-01 ile aynı) | 01-01 ile aynı: numara [B7] `0000000011` (ön koşullu); B1'e bağlı VSAM kuyruğu (kaydın son 8 baytı) hariç. `CA-REQUEST-ID` hiçbir programda okunmaz ve değişmez. | `lgacus01.cbl:95-123`<br>`lgacdb01.cbl:149-192`<br>`lgacvs01.cbl:66-87` | Arayüz (COMMAREA) kuralı |
| 01-15 | LGACUS01 çağrılır. `EIBCALEN = 32500`. LGACDB01'e, LGACVS01'e ya da LGACDB02'ye yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. Üç LINK'te de `RESP`/`NOHANDLE` yok, programlarda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgacus01.cbl:134-137`<br>`lgacdb01.cbl:174-177`, `:186-189` | Arayüz (COMMAREA) kuralı |
| 01-16 | **LGACDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | Sayaç okunmaz, SQL çalışmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 71 baytlık `ERROR-MSG` (tarih, saat, `' LGACDB01'`, `' NO COMMAREA RECEIVED'` + 20 boşluk + 6 baytlık `EM-SQLRC`). [B2] `EM-SQLRC` = `+00000` (ön koşullu; SQLCA'nın kaynağı `base/` altında yok: Kod gözlemleri 12) [kod] `lgacdb01.cbl:297`. COMMAREA dökümü gönderilmez.<br>LGACUS01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 4). | `lgacdb01.cbl:149-153`, `:51-56`, `:297`, `:308-313` | Arayüz (COMMAREA) kuralı |
| 01-17 | **LGACDB01 doğrudan** çağrılır. `EIBCALEN` 8 ile 89 arasında (sınır değer: `89`). | `CA-RETURN-CODE = 98` | Sayaç okunmaz, SQL çalışmaz, hata mesajı yazılmaz.<br>LGACUS01'den farklı olarak `CA-NUM-POLICIES`'e dokunulmaz.<br>1-7 bayt için: [B1] tamponun 7-8. baytları `98` olur (ön koşullu; 01-12 ile aynı kural) [kod] `lgacdb01.cbl:166`. | `lgacdb01.cbl:156`, `:161-168` | Arayüz (COMMAREA) kuralı |
| 01-18 | **LGACDB01 doğrudan** çağrılır. `EIBCALEN = 90`. Ön koşul: `LGSE 1` ([kod] `lgsetup.cbl:128-135`) ya da [B4] düzenek sayacı `CUSTOMER`'da zaten var olan bir numaraya kurar. Sayaç K döner ve K `CUSTOMER`'da zaten var (örn. K = 1: `db2cre.jcl:437`, birincil anahtar `:122`). | `CA-RETURN-CODE = 90` | `CA-CUSTOMER-NUM` değişmez. LGACVS01 ve LGACDB02 çağrılmaz.<br>Yan etki: 01-03'teki gibi iki LINK yapılır, ama ikinci mesaj `EIBCALEN < 91` dalından gelir: `'COMMAREA='` + `DFHCOMMAREA(1:90)`. Bu dala yalnızca bu yolla ulaşılır. | `lgacdb01.cbl:165`, `:221-249`, `:313-319` | Arayüz (COMMAREA) kuralı |
| 01-19 | **LGACDB01 doğrudan** çağrılır. `EIBCALEN = 32500`. Alanlar 01-01'deki gibi, `CA-NUM-POLICIES` herhangi bir değer N. Sayaç K döner, K `CUSTOMER`'da yok. | `CA-RETURN-CODE = 00` | `CA-CUSTOMER-NUM = K`.<br>`CA-NUM-POLICIES` **değişmez** (N kalır): LGACDB01 bu alana yazmaz, sıfırlama LGACUS01'de yapılır. N, `KSDSCUST` kaydına da bu haliyle girer.<br>Diğer yan etkiler 01-01 ile aynı (B1'e bağlı VSAM kuyruğu, yani kaydın son 8 baytı, hariç). | `lgacdb01.cbl:156`, `:285`, `:174-177`<br>(karşılaştırma: `lgacus01.cbl:103`) | Arayüz (COMMAREA) kuralı |
| 01-20 | **LGACDB02 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 71 baytlık `ERROR-MSG` (tarih, saat, `' LGACDB02'`, `' NO COMMAREA RECEIVED'` + 20 boşluk + 6 baytlık `EM-SQLRC`). [B2] `EM-SQLRC` = `+00000` (ön koşullu) [kod] `lgacdb02.cbl:194`. COMMAREA dökümü gönderilmez.<br>LGACDB01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 4). | `lgacdb02.cbl:131-135`, `:49-54`, `:194`, `:205-210` | Arayüz (COMMAREA) kuralı |
| 01-21 | **LGACDB02 doğrudan** çağrılır. `EIBCALEN = 32500`. `D2-REQUEST-ID` `'02ACUS'` dışında bir değer (örn. boşluk). | `D2-RETURN-CODE = 99` | INSERT çalışmaz, hata mesajı yazılmaz.<br>LGACDB01 her zaman `'02ACUS'` gönderdiği için bu yola zincirden ulaşılamaz (Kod gözlemleri 4). | `lgacdb02.cbl:138`, `:143`, `:149-151` | Arayüz (COMMAREA) kuralı |
| 01-22 | **LGACDB02 doğrudan** çağrılır. `EIBCALEN` 55 ile 90 arasında. 55, INSERT'in kullandığı son alan `D2-CUSTSECR-STATE`'in bitişidir. Girdiler: `D2-REQUEST-ID = '02ACUS'`, `D2-CUSTOMER-NUM = 0000000001`, `D2-CUSTSECR-COUNT = '0000'`. Müşteri 1'in `CUSTOMER_SECURE` satırı zaten var (`db2cre.jcl:454-457`). Birincil anahtar (`db2cre.jcl:135`) yüzünden INSERT `SQLCODE ≠ 0` döner. | `D2-RETURN-CODE = 98` | Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: tarih, saat, `' LGACDB02'`, `' CNUM='` + 10 boşluk, `' INSERT SECURITY'`, `' SQLCODE='` + SQLCODE.<br>(2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır. | `lgacdb02.cbl:143-148`, `:166-182`, `:194`, `:205-216` | Arayüz (COMMAREA) kuralı |
| 01-23 | **LGACDB02 doğrudan** çağrılır. `EIBCALEN` 1 ile 54 arasında. `D2-REQUEST-ID = '02ACUS'`. | **belirsiz** | **belirsiz**. LGACDB02'de uzunluk kontrolü yok. INSERT'in kullandığı alanların (`D2-CUSTOMER-NUM` … `D2-CUSTSECR-STATE`, 9-55. baytlar) bir kısmı gelen COMMAREA'nın dışında kalır. 1-7 bayt için `D2-RETURN-CODE` da dışarıda. | `lgacdb02.cbl:95-102`, `:131-148`, `:166-176` | Arayüz (COMMAREA) kuralı |
| 01-24 | **LGACVS01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | **belirsiz** | **belirsiz**. LGACVS01'de `EIBCALEN` kontrolü yok. WRITE, var olmayan COMMAREA'daki `CA-Customer-Num`'u kullanır. Sonuç kodda tanımlı değil. | `lgacvs01.cbl:66-74` | Arayüz (COMMAREA) kuralı |
| 01-25 | **LGACVS01 doğrudan** çağrılır. `EIBCALEN` 18 ile 90 arasında (anahtar `CA-CUSTOMER-NUM` 9-18. baytlarda, içeride kalır). [B4] Düzenek WRITE'ta `NORMAL` dışında bir RESP üretir. | Dönüş kodu yok. `CA-RETURN-CODE`'a `80` yazılır, ardından görev `ABEND` kodu `LGV0` ile (NODUMP) sonlanır (01-05 ile aynı belirsizlik). | Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: `' LGACVS01'`, `' CNUM='` + `CA-CUSTOMER-NUM`, `' Write file KSDSCUST'`, RESP, RESP2.<br>(2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`. Bu dala yalnızca bu yolla ulaşılır.<br>Not: WRITE, 9. bayttan itibaren 225 bayt okur, yani gelen COMMAREA'nın dışına taşar. | `lgacvs01.cbl:68-81`, `:99-112` | Arayüz (COMMAREA) kuralı |

## 01-01 girdi ve beklenen değerler

COMMAREA'nın alan alan girdisi ve dönüşte beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Girdi | Girdinin kaynağı (`db2cre.jcl`) | Dönüşte beklenen | Alanı kullanan / dolduran COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` X(6) | Herhangi bir değer | — | Değişmez | Kodda okunmuyor, yazılmıyor |
| `CA-RETURN-CODE` 9(2) | Herhangi bir değer | — | `00` | `lgacus01.cbl:102`, `lgacdb01.cbl:156` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000000` | — | `0000000011` [kod] `lgsetup.cbl:53`, `:183-187` (ön koşul B7) | `lgacdb01.cbl:210`, `:285` |
| `CA-FIRST-NAME` X(10) | `Andrew` + ␣×4 | `:438` (sütun `CHAR(10)`: `:113`) | Değişmez | INSERT: `lgacdb01.cbl:225`, `:235` |
| `CA-LAST-NAME` X(20) | `Pandy` + ␣×15 | `:439` (sütun `CHAR(20)`: `:114`) | Değişmez | `lgacdb01.cbl:226`, `:236` |
| `CA-DOB` X(10) | `1950-07-11` | `:440` (sütun `DATE`: `:115`) | Değişmez | `lgacdb01.cbl:227`, `:237` |
| `CA-HOUSE-NAME` X(20) | ␣×20 (JCL'de `' '`) | `:441` (sütun `CHAR(20)`: `:116`) | Değişmez | `lgacdb01.cbl:228`, `:238` |
| `CA-HOUSE-NUM` X(4) | `34` + ␣×2 | `:442` (sütun `CHAR(4)`: `:117`) | Değişmez | `lgacdb01.cbl:229`, `:239` |
| `CA-POSTCODE` X(8) | `PI101OO` + ␣×1. Karakterler P-I-1-0-1-O-O: 4. karakter sıfır rakamı, son ikisi O harfi | `:443` (sütun `CHAR(8)`: `:118`) | Değişmez | `lgacdb01.cbl:230`, `:240` |
| `CA-NUM-POLICIES` 9(3) | Herhangi bir değer | — | `000` | `lgacus01.cbl:103` |
| `CA-PHONE-MOBILE` X(20) | `07799 123456` + ␣×8 | `:445` (sütun `phonemobile`, INSERT listesinde 9. sırada: `:434`; `CHAR(20)`: `:120`) | Değişmez | `lgacdb01.cbl:231`, `:241` |
| `CA-PHONE-HOME` X(20) | `01962 811234` + ␣×8 | `:444` (sütun `phonehome`, INSERT listesinde 8. sırada: `:433`; `CHAR(20)`: `:119`) | Değişmez | `lgacdb01.cbl:232`, `:242` |
| `CA-EMAIL-ADDRESS` X(100) | `A.Pandy@beebhouse.com` + ␣×79 | `:446` (sütun `Char(100)`: `:121`) | Değişmez | `lgacdb01.cbl:233`, `:243` |
| `CA-POLICY-DATA` X(32267) | Herhangi bir değer | — | Değişmez | Kodda okunmuyor, yazılmıyor |

Dikkat: `db2cre.jcl`'deki INSERT'te ev telefonu cep telefonundan **önce** geliyor (`:433-434`),
COMMAREA'da ise cep telefonu önce (`lgcmarea.cpy:23-24`). Değerler sıraya göre değil, sütun adına göre
eşleşir (`lgacdb01.cbl:231-232`, `:241-242`).

01-01'den sonra veritabanında ve VSAM'da beklenen durum:

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `CUSTOMER` | Yeni satır: `customerNumber` = 11 (B7), `firstName` = `Andrew`, `lastName` = `Pandy`, `dateOfBirth` = 1950-07-11, `houseName` = boşluk, `houseNumber` = `34`, `postcode` = `PI101OO`, `phonemobile` = `07799 123456`, `phonehome` = `01962 811234`, `emailaddress` = `A.Pandy@beebhouse.com`. Müşteri 1'in satırı değişmez. | `lgacdb01.cbl:222-244` |
| `KSDSCUST` | Anahtarı `0000000011` (B7) olan 225 baytlık kayıt: COMMAREA'nın 9-233. baytları. Sırasıyla `0000000011`, ad … posta kodu, `000`, cep telefonu, ev telefonu, e-posta. Son 8 bayt [B1] boşluk (ön koşullu; Kod gözlemleri 11). | `lgacvs01.cbl:49`, `:68-74` |
| `CUSTOMER_SECURE` | Yeni satır: `customerNumber` = 11 (B7), `customerPass` = `5732fec825535eeafb8fac50fee3a8aa`, `state_indicator` = `N`, `pass_changes` = 0. JCL'deki örnek satırlarla aynı değerler (`db2cre.jcl:454-457`). | `lgacdb01.cbl:179-184`<br>`lgacdb02.cbl:146-147`, `:166-176` |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| US-01 | `lgacus01.cbl:95-99` `IF EIBCALEN = 0` → ABEND `LGCA` | 01-10 | |
| US-02 | `lgacus01.cbl:102-103` `'00'` → `CA-RETURN-CODE`, `CA-NUM-POLICIES` (kontrolden önce) | 01-01, 01-11, 01-12 | 01-11/01-12: alan dışına yazma |
| US-03 | `lgacus01.cbl:108-109` gerekli uzunluk 90 | 01-11, 01-13 | 89 reddedilir, 90 kabul edilir |
| US-04 | `lgacus01.cbl:112-115` `IF EIBCALEN < 90` → `98` | 01-11 | |
| US-05 | `lgacus01.cbl:134-137` LINK LGACDB01 `LENGTH(32500)` | 01-01, 01-13, 01-15 | |
| US-06 | `lgacus01.cbl:123` LGACDB01'in dönüş kodu kontrol edilmez | 01-01, 01-03, 01-06 | |
| US-07 | `lgacus01.cbl:164-178` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 01-10 | Dış `IF` yanlış. İç dallar (`:165-177`) ölü kod, hiçbir girdiyle ulaşılamaz (Kod gözlemleri 15) |
| DB1-01 | `lgacdb01.cbl:149-153` `IF EIBCALEN = 0` → ABEND `LGCA` | 01-16 | Yalnızca doğrudan çağrı |
| DB1-02 | `lgacdb01.cbl:156` `'00'` → `CA-RETURN-CODE` | 01-01, 01-19 | |
| DB1-03 | `lgacdb01.cbl:161-168` `IF EIBCALEN < 90` → `98` | 01-17 | Yalnızca doğrudan çağrı |
| DB1-04 | `lgacdb01.cbl:201-208` sayaç RESP ≠ NORMAL | 01-02, 01-04 | |
| DB1-05 | `lgacdb01.cbl:209-210` sayaç başarılı | 01-01 | |
| DB1-06 | `lgacdb01.cbl:221-244` `IF LGAC-NCS = 'ON'` → INSERT (sayaç numarası) | 01-01, 01-03 | |
| DB1-07 | `lgacdb01.cbl:245-249` SQLCODE ≠ 0 (sayaç yolu) → `90` | 01-03, 01-18 | |
| DB1-08 | `lgacdb01.cbl:250-273` `ELSE` → INSERT (`DEFAULT`) | 01-02, 01-04 | |
| DB1-09 | `lgacdb01.cbl:274-278` SQLCODE ≠ 0 (`DEFAULT` yolu) → `90` | 01-04 | |
| DB1-10 | `lgacdb01.cbl:280-282` `IDENTITY_VAL_LOCAL()`, SQLCODE kontrolü yok | 01-02 | Başarısızlığı belirsiz (Kod gözlemleri 17, `IDENTITY_VAL_LOCAL` uç durumu) |
| DB1-11 | `lgacdb01.cbl:285` `CA-CUSTOMER-NUM` ← yeni numara | 01-01, 01-02, 01-07 | |
| DB1-12 | `lgacdb01.cbl:174-177` LINK LGACVS01 `LENGTH(225)` | 01-01, 01-05 | |
| DB1-13 | `lgacdb01.cbl:179-184` `CDB2AREA` doldurulur | 01-01, 01-06 | |
| DB1-14 | `lgacdb01.cbl:186-189` LINK LGACDB02, `D2-RETURN-CODE` kontrol edilmez | 01-06 | |
| DB1-15 | `lgacdb01.cbl:192` RETURN (`00`) | 01-01 | |
| DB1-16 | `lgacdb01.cbl:313-327` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 01-03 (`ELSE`), 01-18 (`< 91`), 01-16 (dış `IF` yanlış) | |
| DB2-01 | `lgacdb02.cbl:131-135` `IF EIBCALEN = 0` → ABEND `LGCA` | 01-20 | Yalnızca doğrudan çağrı |
| DB2-02 | `lgacdb02.cbl:138` `'00'` → `D2-RETURN-CODE` | 01-01, 01-21 | |
| DB2-03 | Uzunluk kontrolü yok | 01-23 | |
| DB2-04 | `lgacdb02.cbl:143-148` `WHEN '02ACUS'` → INSERT | 01-01, 01-22 | |
| DB2-05 | `lgacdb02.cbl:149-151` `WHEN OTHER` → `99` | 01-21 | Yalnızca doğrudan çağrı |
| DB2-06 | `lgacdb02.cbl:178-182` SQLCODE ≠ 0 → `98` | 01-06 (zincir), 01-22 (doğrudan) | |
| DB2-07 | `lgacdb02.cbl:155` RETURN (`00`) | 01-01 | |
| DB2-08 | `lgacdb02.cbl:210-224` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 01-06 (`ELSE`), 01-22 (`< 91`), 01-20 (dış `IF` yanlış) | |
| VS-01 | EIBCALEN kontrolü yok (`lgacvs01.cbl:66`) | 01-24 | |
| VS-02 | `lgacvs01.cbl:68-74` WRITE `KSDSCUST` | 01-01 | |
| VS-03 | `lgacvs01.cbl:75-81` RESP ≠ NORMAL → `80` + ABEND `LGV0` | 01-05, 01-25 | |
| VS-04 | `lgacvs01.cbl:85-87` başarı → GOBACK | 01-01 | |
| VS-05 | `lgacvs01.cbl:106-120` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 01-05 (`ELSE`), 01-25 (`< 91`) | |
| SQ-01 | `lgstsq.cbl:68-71` `IF WS-INVOKEPROG NOT = SPACES` (LINK yolu) | 01-03, 01-05, 01-06, 01-10, 01-16, 01-20, 01-22, 01-25 | |
| SQ-02 | `lgstsq.cbl:72-80` `ELSE` (terminalden RECEIVE) | 01-03 (dal alınmaz) | **Kapsam dışı: bu zincirden ulaşılamaz.** Dört program da LGSTSQ'yu LINK ile çağırır. |
| SQ-03 | `lgstsq.cbl:82-88` `IF` mesaj `'Q='` ile başlıyor | 01-03 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** Mesajlar tarih ya da `'COMMAREA='` ile başlar. |
| SQ-04 | `lgstsq.cbl:94-99` WRITEQ TD `CSMT`, RESP kontrol edilmez | 01-03 | |
| SQ-05 | `lgstsq.cbl:105-111` WRITEQ TS `GENAERRS`, RESP kontrol edilmez | 01-03 | |
| SQ-06 | `lgstsq.cbl:113-119` `IF WS-FLAG = 'R'` → SEND TEXT | 01-03 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** `'R'` yalnızca SQ-02 yolunda atanır. |
| N-01 | `CA-REQUEST-ID` okunmaz | 01-14 | Kod gözlemleri 6 |
| N-02 | Girdideki `CA-CUSTOMER-NUM` kullanılmaz | 01-07, 01-03 | Kod gözlemleri 7 |
| N-03 | Alan doğrulaması yok | 01-01, 01-08, 01-09 | Kod gözlemleri 8 |
| N-04 | LINK'lerde `RESP` yok | 01-15 | Kod gözlemleri 5 |
| N-05 | `SYNCPOINT` / `ROLLBACK` yok | 01-05 | Kod gözlemleri 9 |

## Ön koşullu satırlar

2026-10-05 A/B güncellemesinde belirsizden ön koşullu tahmine çevrilen satırlar. Ön koşul kodları dosyanın başında
("Kaynak etiketleri ve ön koşul kodları").

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 01-01 | B1, B7 | `CA-CUSTOMER-NUM = 0000000011`; `KSDSCUST` kaydının son 8 baytı boşluk | B7 yoksa numara sayacın o anki değeri, belirsiz; B1 yoksa son 8 bayt belirsiz: LINK'in COMMAREA'yı nasıl geçirdiğine bağlı (`lgacdb01.cbl:176`, `lgacvs01.cbl:68-70`) |
| 01-02 | B4, B6 | Girdi düzenekle kurulur; `CA-CUSTOMER-NUM = 0001000001` | B4 yoksa girdi kurulamaz; B6 yoksa daha büyük bir kimlik değeri, tam değeri belirsiz |
| 01-03 | `LGSE 1` ya da B4 | Girdi kurulur; tahmin (`90`) değişmez | Girdi kurulamaz: `LGSE` parametresizse sayaç 11 verir ve 11 `CUSTOMER`'da yok (01-01 yolu) |
| 01-04 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | Girdi kurulamaz: `DEFAULT` INSERT'inin hangi durumda hata vereceği kodda yok |
| 01-05 | B4, B7 | Girdi düzenekle kurulur; tahmin (`80` + ABEND `LGV0`) değişmez; hata mesajında K = `0000000011` | B4 yoksa girdi kurulamaz; B7 yoksa mesajdaki K belirsiz |
| 01-06 | B4, B5, B7 | Girdi düzenekle kurulur; dökümdeki `D2-CUSTSECR-DATA` baytları (35 bayt) boşluk; K = `0000000011` | B4 yoksa girdi kurulamaz (zincirde veri yolu yok: `db2cre.jcl:135-138`); B5 yoksa 35 bayt belirsiz (`lgacdb01.cbl:65`); B7 yoksa K belirsiz |
| 01-07 | B7 | `CA-CUSTOMER-NUM = 0000000011` (girdideki `0000000001` yok sayılır) | Girdideki numaranın yok sayılması yine geçerli; yeni numara belirsiz |
| 01-08 | B7 | 01-01 ile aynı, ad ve soyad boşluk; numara `0000000011` | Numara belirsiz |
| 01-11 | B1 | Tamponun 91-93. baytları `000` | Alan dışına yazmanın etkisi belirsiz |
| 01-12 | B1 | Tamponun 7-8. baytları `98`, 91-93. baytları `000` | Dönüş kodu ve alan dışına yazmaların etkisi belirsiz |
| 01-13 | B1, B7 | `00`; `CUSTOMER`'da telefonlar ve e-posta boşluk; VSAM'da 91-93 `000`, 94-233 boşluk; numara `0000000011` | B1 yoksa sonuç belirsiz (`00`, `90` ya da ortama göre abend), veri belirsiz; B7 yoksa numara belirsiz |
| 01-14 | B7 | 01-01 ile aynı; numara `0000000011` | Numara belirsiz |
| 01-16 | B2 | `EM-SQLRC = +00000` | `EM-SQLRC` belirsiz |
| 01-17 | B1 | 1-7 bayt için tamponun 7-8. baytları `98` | Belirsiz |
| 01-18 | `LGSE 1` ya da B4 | Girdi kurulur; tahmin (`90`, `< 91` dalı) değişmez | Girdi kurulamaz (01-03 ile aynı neden) |
| 01-20 | B2 | `EM-SQLRC = +00000` | `EM-SQLRC` belirsiz |
| 01-25 | B4 | Girdi düzenekle kurulur; tahmin (`80` + ABEND `LGV0`, `< 91` dalı) değişmez | Girdi kurulamaz |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 01-05 | Dönüş kodu (çağıranın `80`'i görmesi) | `80` yazıldıktan hemen sonra `ABEND LGV0` geliyor (`lgacvs01.cbl:77-79`) ve zincirde abend yakalama yok. Abend sonrası COMMAREA'nın çağırana dönüp dönmediği CICS'e bağlı. |
| 01-09 | Dönüş kodu, beklenen veri | Boşluktan oluşan bir değerin `DATE` sütununa yazılıp yazılamayacağı Db2'ye bağlı. COBOL'da tarih doğrulaması yok. |
| 01-15 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil. |
| 01-23 | Dönüş kodu, beklenen veri | LGACDB02'de uzunluk kontrolü yok. INSERT'e giden alanların bir kısmı COMMAREA'nın dışında. |
| 01-24 | Dönüş kodu, beklenen veri | LGACVS01'de `EIBCALEN` kontrolü yok. COMMAREA olmadan `CA-Customer-Num`'a erişimin sonucu kodda tanımlı değil. |
| 01-25 | Dönüş kodu (çağıranın `80`'i görmesi) | 01-05 ile aynı neden. |
