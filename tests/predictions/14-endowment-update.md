# 14 — Hayat sigortası güncelle (Update Endowment Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Hayat sigortası (endowment) güncelle: müşteri ve poliçe numarasıyla bulunan `POLICY` satırı ile `ENDOWMENT` satırı COMMAREA'daki değerlerle güncellenir ve VSAM `KSDSPOLY` kaydı yeniden yazılır. Güncelleme, COMMAREA'daki `LASTCHANGED` değeri Db2'dekiyle aynıysa yapılır (iyimser kilit). Poliçe türü COMMAREA'daki istek kodundan (`01UEND`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGUPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGUPDB01` (Db2 imleci, `ENDOWMENT` ve `POLICY` UPDATE) → `LINK` → `LGUPVS01` (VSAM `READ UPDATE` + `REWRITE`). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. Programlar dört poliçe türüne de hizmet eder; yalnızca hayat sigortası (`01UEND`) kolları ve ortak kısımlar okundu. |
| Okunan COBOL dosyaları | `base/src/lgupol01.cbl`, `base/src/lgupdb01.cbl`, `base/src/lgupvs01.cbl`, `base/src/lgstsq.cbl`, `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy` |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` ve `ENDOWMENT` tanımları (`:161-178`, `:203-217`), müşteri 2, 3 ve 8 (`:471`, `:505`, `:675`), poliçe 1, 4 ve 5 ile hayat sigortası 4 ve 5'in verisi (`:945-954`, `:849-858`, `:825-834`, `:1033-1040`, `:1014-1021`). `base/cntl/adef121.jcl` ve `base/data/ksdspoly.txt`: `KSDSPOLY` tanımı ve başlangıç verisi. COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `base/Architecture.md` (yalnızca Kod gözlemleri 9; kodda doğrulanmadı), `docs/adr/0004-legacy-test-harness.md` (yalnızca Kod gözlemleri 13) |
| Hazırlayan | Oğuz (taslak: Claude Code) |
| Tarih | 2026-10-17 |
| Kaynak commit (`git rev-parse HEAD`) | `13d0e97c05c6a63cbb6ee9ad0af44cbce5e06d66` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge]**: `base/` altındaki ya da `docs/` altındaki açıklama metinleri. Belgeye dayanan bir tahmin kodda doğrulanmamıştır.
- **[CICS bilgisi]**: Kodda yazmayan ama ekipçe kesin kabul edilen CICS davranışı (`TEMPLATE.md` madde 7). Bu dosyada yalnızca
  `EXEC CICS ABEND` yollarında kullanılır: gerçek CICS'te görev sonlanır, çağırana dönüş kodu ulaşmaz ve görevin kesinleşmemiş
  Db2 ve VSAM değişiklikleri geri alınır (VSAM dosyaları `LOG(UNDO)` ile tanımlı: `adef121.jcl:20`, `:45`). Düzenekte `ABEND`
  yerine mesaj ve `GOBACK` gelir; `ABEND`'den önce yazılan dönüş kodu görünür ve geri alma yoktur.
- Etiketsiz kaynaklar [kod]'dur.

Ön koşul kodları test düzeneğinin vereceği kararlardır (kodlar `06-motor-update.md` ile aynıdır). Bir kodu taşıyan tahmin
**kesin değil, ön koşulludur**: ön koşul sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması, bir sütunun dolu olması) kendisi kurar. **Aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir; satırda yalnızca düzeneğin değiştirdiği değer yazılır. |

(B5 ve B6 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası, hayat sigortası görünümü** (`lgcmarea.cpy:10-13`, `:34-44`, `:46-54`). Üç program da bu düzeni kullanır
   (`lgupol01.cbl:73-74`, `lgupdb01.cbl:150-153`, `lgupvs01.cbl:89-90`). Hayat sigortası güncellemesinde alanlar:

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

2. **Uzunluk kontrolü yalnızca LGUPOL01'de ve yalnızca tanınan istek kodlarında vardır.**
   - LGUPOL01, `'01UEND'` için 28 (`lgupol01.cbl:62`) + 124 (`lgupol01.cbl:39`) = **152** ister (`:115-121`). 152 bayt, 1-152'yi, yani `CA-E-LIFE-ASSURED`'ın sonuna kadar olan
     **bütün sabit alanları** kapsar; `UPDATE ENDOWMENT` yalnızca bunları okur (`lgupdb01.cbl:390-391`, `:397-403`). Motordan farklı olarak kontrolün dışında kalan okunan alan yoktur.
   - LGUPOL01, LGUPDB01'i her zaman `LENGTH(32500)` ile çağırır (`lgupol01.cbl:157-160`). LGUPDB01'de uzunluk kontrolü yoktur: `WS-CA-HEADER-LEN` ve `WS-REQUIRED-CA-LEN` tanımlı ama hiç
     kullanılmaz (`lgupdb01.cbl:63-65`); yorum "check commarea length" der (`:201-204`) ama kod yoktur. LGUPDB01'in `EIBCALEN = 0` kontrolüne (`:183-187`) LGUPOL01 üzerinden ulaşılamaz.
   - LGUPDB01, LGUPVS01'i `LENGTH(225)` ile çağırır (`lgupdb01.cbl:209-212`). LGUPVS01'de `EIBCALEN` kontrolü yoktur.
3. **İstek kodu `99`'u çağırana ulaşmaz.** LGUPOL01 tanınmayan kodda `'99'` yazar (`lgupol01.cbl:139-140`) ama `RETURN` yapmaz; `PERFORM UPDATE-POLICY-DB2-INFO` her durumda çalışır (`:143`).
   LGUPDB01 başta `CA-RETURN-CODE`'u `'00'` yapar (`lgupdb01.cbl:190`) ve `99` kaybolur. LGUPDB01'in istek kodu `EVALUATE`'ında `WHEN OTHER` yoktur (`lgupdb01.cbl:283-300`): tanınmayan
   kodda tür tablosu (`ENDOWMENT`) güncellenmez, ama kod `POLICY` tablosunu güncellemeye devam eder (`:313-326`). Sonra LGUPVS01 çağrılır; VSAM tür harfi `CA-REQUEST-ID`'nin 4. karakterinden
   gelir (`lgupvs01.cbl:102`). İstek kodu karşılaştırması büyük/küçük harfe duyarlıdır (`UPPER-CASE` yok).
4. **LASTCHANGED kontrolü.** Akış (`lgupdb01.cbl:251-360`):
   - `POLICY_CURSOR` müşteri **ve** poliçe numarasıyla açılır (`:128-143`, `:256`) ve ilk satır okunur (`:273`).
   - `CA-LASTCHANGED` (X(26)) ile `DB2-LASTCHANGED` (X(26)) bire bir karşılaştırılır (`:278`).
   - Eşitse sırayla: `ENDOWMENT` UPDATE (`:286-288`), `POLICY` UPDATE (`:318-326`, `LASTCHANGED = CURRENT TIMESTAMP`), yeni zaman damgası COMMAREA'ya okunur (`:329-334`).
   - Eşit değilse `'02'` atanır (`:344-346`), hiçbir UPDATE çalışmaz.
   - **`'02'` çağırana ulaşmaz.** `'02'` atandıktan sonra program `PERFORM CLOSE-PCURSOR` (`:360`) çalıştırır. CLOSE başarılıysa (`SQLCODE = 0`) `CLOSE-PCURSOR` `CA-RETURN-CODE`'u `'00'`
     yapar (`:369-371`) ve `'02'` ezilir. Sonra LGUPDB01 yine LGUPVS01'i çağırır (`:209-212`; arada `RETURN` yok) ve VSAM kaydı yeni değerlerle yeniden yazılır. Sonuç: çağıran `'00'` görür,
     Db2 güncellenmemiştir, VSAM güncellenmiştir (14-02).
5. **`CLOSE-PCURSOR` dönüş kodunu ezer.** `CLOSE POLICY_CURSOR` sonrası (`lgupdb01.cbl:365-380`): `SQLCODE = 0` → `'00'`; `-501` → `'00'` ve `RETURN`; diğer → `'90'`, hata mesajı, `RETURN`.
   `PERFORM CLOSE-PCURSOR` iki yerden çağrılır: tür tablosu hatasında (`:305`, ardından `RETURN` `:306`) ve akışın sonunda (`:360`). Sonuçları aşağıdaki tabloda:

   | Yol | Atanan kod | `CLOSE` sonrası | LGUPVS01 çağrılır mı | Çağırana ulaşan |
   |---|---|---|---|---|
   | Başarı | `00` (`:261`) | `00` | Evet | `00` (VSAM başarılıysa) |
   | `LASTCHANGED` uyuşmuyor | `02` (`:346`) | `00` | Evet | `00` |
   | `FETCH` `SQLCODE = 100` | `01` (`:352`) | `00` | Evet | VSAM kaydı yoksa ABEND `LGV3`, varsa `00` |
   | `FETCH` diğer SQLCODE | `90` (`:354`) | `00` | Evet | `00` |
   | `ENDOWMENT` UPDATE `SQLCODE = 100` | `01` (`:411`) | `00` | Hayır (`RETURN` `:306`) | `00` |
   | `ENDOWMENT` UPDATE diğer SQLCODE | `90` (`:413`) | `00` | Hayır (`RETURN` `:306`) | `00` |
   | `POLICY` UPDATE hatası, ardından `SELECT` başarılı | — (kod atanmaz) | `00` | Evet | `00` |
   | `SELECT LASTCHANGED` hatası, `CLOSE` `0` | `90` (`:339`) | `00` | Evet | `00` |
   | `SELECT LASTCHANGED` hatası, `CLOSE` `-501` | `90` (`:339`) | `00`, `RETURN` (`:372-375`) | Hayır | `00` |
   | `OPEN` hatası | `90` (`:263`, `:267`) | `CLOSE` çağrılmaz, `RETURN` | Hayır | `90` |
   | `CLOSE` hatası (0 ve -501 dışında) | `90` (`:377`) | `90`, `RETURN` | Hayır | `90` |

6. **`POLICY` UPDATE'inin SQLCODE'u kontrol edilmez.** `UPDATE POLICY ... WHERE CURRENT OF` (`lgupdb01.cbl:318-326`) hemen ardından `SELECT LASTCHANGED` gelir ve kontrol edilen yalnızca
   `SELECT`'in `SQLCODE`'udur (`:329-336`). UPDATE hata verse de `SELECT` başarılıysa hata görünmez.
7. **`CA-PAYMENT` Db2'ye yazılmaz.** `MOVE CA-PAYMENT TO DB2-PAYMENT-INT` yapılır (`lgupdb01.cbl:314`) ama `POLICY` UPDATE'inin `SET` listesinde `PAYMENT` yoktur (`:320-324`); imleç de
   `PAYMENT`'ı seçmez ve "payment ya da commission alanlarını güncellemez" der (`:126-127`, `:130-143`). `POLICY` UPDATE'i yalnızca `ISSUEDATE`, `EXPIRYDATE`, `LASTCHANGED`, `BROKERID`,
   `BROKERSREFERENCE` sütunlarını yazar.
8. **`ENDOWMENT` UPDATE'i yedi sabit sütunu yazar, `PADDINGDATA`'yı yazmaz** (`lgupdb01.cbl:394-406`): `WITHPROFITS`, `EQUITIES`, `MANAGEDFUND`, `FUNDNAME`, `TERM`, `SUMASSURED`, `LIFEASSURED`.
   Kısmi güncelleme yok; boş gönderilen alan Db2'deki değeri boşlukla ezer. `CA-E-PADDING-DATA` hiçbir yerde okunmaz; `WS-VARY-FIELD` tanımlıdır (`lgupdb01.cbl:69-71`) ama güncellemede kullanılmaz. Yani
   COMMAREA'da ne gelirse gelsin `ENDOWMENT.paddingData` yerinde kalır. Hiçbir alan doğrulanmaz.
9. **Geri alma.** Programlarda tek `SYNCPOINT ROLLBACK` `lgupdb01.cbl:338`'dedir. LGUPVS01 abend ederse (`lgupvs01.cbl:151`, `:164`) gerçek CICS'te görevin
   kesinleşmemiş Db2 UPDATE'leri ve VSAM değişiklikleri geri alınır [CICS bilgisi] (`TEMPLATE.md` madde 7; `KSDSPOLY` `LOG(UNDO)` ile tanımlı: `adef121.jcl:45`).
   [belge] `base/Architecture.md:48` de bununla uyumludur. Düzenekte geri alma yoktur. "Geri alınır" diyen satırlar: 14-15, 14-16, 14-18, 14-30.
10. **VSAM kaydı** (`lgupvs01.cbl:26-52`, `:100-166`).
    - Anahtar 21 bayt: `CA-Request-ID(4:1)` + `CA-Customer-Num` + `CA-Policy-Num` (`:102-104`, `:137`). `'01UEND'` → `'E'`.
    - `READ ... UPDATE` önce kaydı okur: `INTO(WS-FileIn)` (1024 bayt), `LENGTH(WS-Commarea-Len)` = `EIBCALEN` = 225 (`:54`, `:100`, `:139-146`). Okunan kayıt hiçbir satırda kullanılmaz.
    - `REWRITE FROM(WF-Policy-Info) LENGTH(WS-Commarea-LenF)` (`:155-159`); `WS-Commarea-LenF` `VALUE 64` (`:24`). Yazılan 64 bayt: anahtar 21 + üç bayrak 3 + fon adı 10 + yaşam sigortalısı **30**
      (`:37-41`, `:113-118`). `CA-E-LIFE-ASSURED` 31 bayttır; **31. karakter VSAM'a yazılmaz** (`:118`). `CA-E-TERM`, `CA-E-SUM-ASSURED` ve `CA-E-PADDING-DATA` VSAM'a yazılmaz.
    - Küme `KEYS(21 0)`, `RECORDSIZE(64 64)` [kod] `adef121.jcl:49-50`. Başlangıç verisinde iki hayat sigortası kaydı: `ksdspoly.txt:3-4`.
    - LGUPVS01'in hata mesajında `EM-POLNUM` hiç doldurulmaz; `' PNUM='` yanında 10 boşluk kalır (`lgupvs01.cbl:184-186`; karşılaştırma: LGDPVS01 `lgdpvs01.cbl:110`).
11. **Dönüş kodları.**

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgupol01.cbl:105`, `lgupdb01.cbl:190`, `:261`, `:371`, `:373` | Başlangıç / `OPEN` ve `CLOSE` başarılı | Evet |
    | `01` | `lgupdb01.cbl:352`, `:411` | Kayıt yok | Hayır: `CLOSE` ezer (Kod gözlemleri 5) |
    | `02` | `lgupdb01.cbl:346` | `LASTCHANGED` uyuşmuyor | Hayır: `CLOSE` ezer (Kod gözlemleri 4) |
    | `90` | `lgupdb01.cbl:263`, `:267`, `:377` | `OPEN` / `CLOSE` hatası | Evet |
    | `90` | `lgupdb01.cbl:339`, `:354`, `:413` | `SELECT`, `FETCH`, `UPDATE` hatası | Hayır: `CLOSE` ezer |
    | `81` | `lgupvs01.cbl:149` | VSAM `READ` hatası | Alana yazılır, hemen ardından `ABEND LGV3`: gerçek CICS'te ulaşmaz, düzenekte görünür [CICS bilgisi] |
    | `82` | `lgupvs01.cbl:162` | VSAM `REWRITE` hatası | Alana yazılır, hemen ardından `ABEND LGV4`: gerçek CICS'te ulaşmaz, düzenekte görünür [CICS bilgisi] |
    | `98` | `lgupol01.cbl:119` | COMMAREA < 152 (`'01UEND'`) | Evet |
    | `99` | `lgupol01.cbl:140` | İstek kodu tanınmıyor | Hayır: `lgupdb01.cbl:190` ezer (Kod gözlemleri 3) |
    | ABEND `LGCA` | `lgupol01.cbl:102`, `lgupdb01.cbl:186` | COMMAREA yok | — |
    | ABEND `LGV3` | `lgupvs01.cbl:151` | VSAM `READ` hatası | — |
    | ABEND `LGV4` | `lgupvs01.cbl:164` | VSAM `REWRITE` hatası | — |

    (`lgupdb01.cbl:411`, `:413` `ENDOWMENT` kolundadır.)
12. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGUPOL01 45 bayt (`lgupol01.cbl:44-49`), LGUPDB01 87 bayt (`lgupdb01.cbl:39-51`), LGUPVS01 101 bayt (`lgupvs01.cbl:61-76`). `CA-ERROR-MSG` üçünde de 99 bayttır:
      `'COMMAREA='` + 90 bayt.
    - LGUPDB01'de `EM-SQLREQ` değerleri (16 bayta boşlukla tamamlanır): `' OPEN   PCURSOR '` (`:254`), `' FETCH  ROW   '` (`:229`), `' UPDATE ENDOW '` (`:393`), `' UPDATE POLICY  '` (`:317`),
      `' CLOSE  PCURSOR'` (`:364`). `EM-CUSNUM` ← `CA-CUSTOMER-NUM`, `EM-POLNUM` ← `CA-POLICY-NUM` (`:198-199`). `EM-SQLRC` `PIC +9(5)` (`:51`, `:504`): `-913` → `-00913`.
    - LGUPVS01'de sabit metin `' Re-write  KSDSPOLY '` (`lgupvs01.cbl:71-72`) **READ hatasında da** kullanılır.
    - LGUPDB01'in `EIBCALEN = 0` mesajı: 21 karakterlik `' NO COMMAREA RECEIVED'`, 63 baytlık `EM-VARIABLE` grubuna taşınır ve sabitleri ezer (`lgupdb01.cbl:44-51`, `:184`).
      Sonra `MOVE SQLCODE TO EM-SQLRC` son 6 baytı ezer (`:504`).
13. **`WRITE-ERROR-MESSAGE` dalları** (`IF EIBCALEN > 0` / `< 91` / `ELSE`) ve **WORKING-STORAGE kalıcılığı.**
    - LGUPOL01'de yalnızca `EIBCALEN = 0` dalından çağrılır (`lgupol01.cbl:101`); iç dallar (`lgupol01.cbl:186-199`) çalışmaz, ölü kod.
    - LGUPDB01 zincirden çağrıldığında `EIBCALEN = 32500`, LGUPVS01 `EIBCALEN = 225` görür; ikisinde de `ELSE` dalı çalışır. `< 91` dallarına (`lgupdb01.cbl:521-526`, `lgupvs01.cbl:192-197`)
      yalnızca doğrudan çağrıyla ulaşılır (14-24, 14-25).
    - `WS-REQUIRED-CA-LEN` (`lgupol01.cbl:63`) `VALUE +0` ve eşik hesabında üstüne eklenir. WORKING-STORAGE yeniden kurulmazsa ikinci çağrıda eşik 304 olur. Test düzeneği her durumu ayrı süreçte
      çalıştırır [belge] `docs/adr/0004-legacy-test-harness.md` (karar 4). Bu dosyadaki tahminler tek çağrı içindir. `WS-RETRY` (`lgupdb01.cbl:31`, `:173`) hiçbir yerde kullanılmaz.
14. **Test verisi** (`base/cntl/db2cre.jcl`). Bu değerler yalnızca test veritabanı bu JCL ile yüklendiyse ve sonradan değiştirilmediyse geçerlidir.
    - 14-01'de güncellenen satır poliçe 4 (müşteri 8: `:849-850`, hayat sigortası 4: `:1033-1040`). Yeni değerler poliçe 5'in ve hayat sigortası 5'in değerleridir (`:827-833`, `:1015-1021`).
      Böylece her girdinin JCL'de bir kaynağı var.
    - `CA-LASTCHANGED` girdisi, Db2'deki değerin kendisidir (poliçe 4 için JCL'de `'2011-08-05 10:45:19'`, `:854`). Dönen metnin biçimi belirsiz olduğundan test, bu değeri önce `13-01` ile okur ve
      aynen geri verir.
    - Olmayan poliçe için `0000000999` kullanılır (JCL poliçe numaralarını 1-10 verir, kimlik değeri 1000001'den başlar: `:162-163`). VSAM başlangıç verisinde de yoktur (`ksdspoly.txt:1-10`).
    - Poliçe 1 müşteri 2'ye ait bir motor poliçesidir (`:945-946`); `ENDOWMENT`'ta satırı yoktur (14-09).

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 14-01 | LGUPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01UEND'`. `CA-CUSTOMER-NUM = 0000000008`, `CA-POLICY-NUM = 0000000004` (poliçe 4, müşteri 8). `CA-LASTCHANGED` = Db2'deki değer (önce sorgulanıp aynen geri verilir). Ortak ve hayat sigortası alanlarında poliçe 5'in değerleri var (alan alan aşağıdaki **"14-01 girdi ve beklenen değerler"** tablosunda). `KSDSPOLY`'de `E0000000008` + `0000000004` anahtarlı kayıt var [kod] `ksdspoly.txt:4`. [B4] Tüm SQL ve dosya komutları başarılı. | `CA-RETURN-CODE = 00` | `CA-LASTCHANGED` yeni değer olur: güncelleme anındaki zaman damgası, eskisinden farklı (biçimi **belirsiz**). Diğer COMMAREA alanları girdideki gibi kalır.<br>Yan etkiler: `ENDOWMENT`'ta poliçe 4'ün yedi sütunu girdideki değerlerle değişir. `POLICY`'de poliçe 4'ün `issueDate`, `expiryDate`, `brokerId`, `brokersReference` sütunları değişir ve `lastChanged` güncelleme zamanı olur; `payment`, `customerNumber`, `policyType` değişmez. `KSDSPOLY`'deki kayıt 64 bayt olarak yeniden yazılır. Ayrıntı aşağıda **"14-01'den sonra Db2'de ve VSAM'da beklenen durum"** tablosunda. Hata mesajı yazılmaz. | `lgupol01.cbl:99`, `:105`, `:113`, `:115-121`, `:143`, `:157-160`<br>`lgupdb01.cbl:190`, `:195-196`, `:256-261`, `:273-278`, `:286-288`, `:302`, `:313-334`, `:336`, `:360`, `:369-371`, `:387-406`, `:209-216`<br>`lgupvs01.cbl:102-104`, `:113-118`, `:139-159` | İş kuralı |
| 14-02 | 14-01 ile aynı, ama `CA-LASTCHANGED` Db2'deki değerden **farklı**: kayıt, bu istemci sorgulayıp güncellemeden önce başka bir istemci tarafından güncellenmiş (ya da `CA-LASTCHANGED` boşluk). [B4] `CLOSE` `SQLCODE = 0`. | `CA-RETURN-CODE = 00`. `02` **çağırana ulaşmaz**: `lgupdb01.cbl:346`'da atanır, `CLOSE-PCURSOR` `:371`'de `00` ile ezer (Kod gözlemleri 4, 5). | Db2'de **hiçbir satır değişmez**: `ENDOWMENT` ve `POLICY` UPDATE'leri çalışmaz. `CA-LASTCHANGED` COMMAREA'da girdideki (eski) değerle kalır. Hata mesajı yazılmaz.<br>**VSAM kaydı yine de yeniden yazılır** (LGUPVS01 her durumda çağrılır, `lgupdb01.cbl:209-212`): `KSDSPOLY`'deki kayıt girdideki hayat sigortası değerleriyle değişir, Db2 değişmez. Db2 ile VSAM tutarsız kalır. | `lgupdb01.cbl:273-278`, `:344-347`, `:358-360`, `:362-371`, `:209-216`<br>`lgupvs01.cbl:139-159` | İş kuralı |
| 14-03 | 14-01 ile aynı, ama `CA-POLICY-NUM = 0000000999` (poliçe Db2'de yok, VSAM'da da yok). [B4] `FETCH` `SQLCODE = 100`, `CLOSE` `0`, VSAM `READ`'de `RESP ≠ NORMAL` (`NOTFND`). | LGUPDB01'in yazdığı `01`, `CLOSE`'da `00` ile ezilir; LGUPVS01 `81` yazar ve görev `ABEND` kodu `LGV3` ile (NODUMP) sonlanır. **Gerçek CICS:** dönüş kodu ulaşmaz [CICS bilgisi]. **Düzenekte:** `CA-RETURN-CODE = 81` görünür. | Db2'de hiçbir satır değişmez, VSAM'a yazılmaz. LGUPDB01 hata mesajı yazmaz (`100` dalında yok). **Poliçe bulunamadığında çağıran `01` görmez.**<br>Yan etki: LGUPVS01, `LGSTSQ`'ya iki LINK yapar. (1) `ERROR-MSG` (101 bayt): tarih, saat, `' LGUPVS01'`, `' PNUM='` + 10 boşluk, `' CNUM='` + `0000000008`, `' Re-write  KSDSPOLY '` (READ hatası olduğu halde), `' RESP='` + RESP, `' RESP2='` + RESP2. (2) `CA-ERROR-MSG`: `'COMMAREA='` + gelen 225 baytın ilk 90'ı (`EIBCALEN = 225`, `ELSE` dalı); içinde `81` var. | `lgupdb01.cbl:273-275`, `:349-352`, `:360`, `:369-371`, `:209-212`<br>`lgupvs01.cbl:139-153`, `:174-205` | İş kuralı |
| 14-04 | 14-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000003` (poliçe 4 müşteri 8'e ait: `db2cre.jcl:850`). [B4] `CLOSE` `0`, VSAM `READ`'de `RESP ≠ NORMAL` (anahtar `E0000000003` + `0000000004` VSAM'da yok). | 14-03 ile aynı (`ABEND LGV3`): gerçek CICS'te dönüş kodu ulaşmaz, düzenekte `81`. | 14-03 ile aynı: imleç müşteri numarasını da içerir (`lgupdb01.cbl:136-137`), eşleşen satır yoktur; başka müşterinin poliçesi güncellenemez. `' CNUM='` yanında `0000000003` yazılır. | `lgupdb01.cbl:128-137`, `:349-352`<br>`lgupvs01.cbl:102-104`, `:139-153` | İş kuralı |
| 14-05 | 14-03 ile aynı, ama VSAM'da anahtarı `E0000000008` + `0000000999` olan kayıt var, Db2'de poliçe 999 yok (tutarsız veri; başlangıç verisinde böyle bir durum yok [kod] `ksdspoly.txt:1-10`). [B4] `READ` ve `REWRITE` `NORMAL`. | `CA-RETURN-CODE = 00` | Db2'de hiçbir satır değişmez. **Db2'de poliçe olmadığı halde VSAM kaydı yeni değerlerle yeniden yazılır.** Hata mesajı yazılmaz. | `lgupdb01.cbl:349-352`, `:360`, `:369-371`, `:209-212`<br>`lgupvs01.cbl:139-159` | İş kuralı |
| 14-06 | 14-01 ile aynı, ama [B4] `OPEN POLICY_CURSOR`'da `SQLCODE = -913`. | `CA-RETURN-CODE = 90` | Hiçbir UPDATE çalışmaz, LGUPVS01 çağrılmaz (`RETURN`). `CA-LASTCHANGED` değişmez.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGUPDB01'`, `' CNUM='` + `0000000008`, `' PNUM='` + `0000000004`, `' OPEN   PCURSOR '`, `' SQLCODE='` + `-00913`. (2) `CA-ERROR-MSG`: COMMAREA'nın ilk 90 baytı (`EIBCALEN = 32500`, `ELSE` dalı). | `lgupdb01.cbl:254-265`, `:504-532` | İş kuralı |
| 14-07 | 14-01 ile aynı, ama [B4] `OPEN`'da 0 ve -913 dışında bir `SQLCODE` (örn. `-904`). | `CA-RETURN-CODE = 90` | 14-06 ile aynı; `EM-SQLRC` üretilen SQLCODE'dur. | `lgupdb01.cbl:266-269`, `:504-532` | İş kuralı |
| 14-08 | 14-01 ile aynı, ama [B4] `FETCH`'te 0 ve 100 dışında bir `SQLCODE`, `CLOSE` `0`; `READ` ve `REWRITE` `NORMAL`. | `CA-RETURN-CODE = 00`. `FETCH` hatasının `90`'ı `CLOSE`'da ezilir. | Db2'de hiçbir satır değişmez. **VSAM kaydı yeni değerlerle yeniden yazılır.**<br>Yan etki: LGUPDB01, `LGSTSQ`'ya iki LINK yapar. (1) `ERROR-MSG`: 14-06'daki biçimde, `EM-SQLREQ` `' FETCH  ROW   '`. (2) `CA-ERROR-MSG`: COMMAREA'nın ilk 90 baytı. | `lgupdb01.cbl:228-239`, `:349-357`, `:360`, `:369-371`, `:209-212` | İş kuralı |
| 14-09 | 14-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000002`, `CA-POLICY-NUM = 0000000001` (poliçe 1, müşteri 2'ye ait bir motor poliçesi; `ENDOWMENT`'ta satırı yok: `db2cre.jcl:945-946`). `CA-LASTCHANGED` poliçe 1'in Db2'deki değeri. İstek kodu `'01UEND'`. | `CA-RETURN-CODE = 00`. `ENDOWMENT` UPDATE'inin `01`'i `CLOSE`'da ezilir. | `ENDOWMENT` UPDATE'i `SQLCODE = 100` döner (satır yok). `POLICY` UPDATE'i çalışmaz ve LGUPVS01 çağrılmaz (`RETURN`): Db2 ve VSAM değişmez. **Çağıran `01` görmez; güncelleme hiç yapılmadığı halde `00` döner.** Hata mesajı yazılmaz (`100` dalında yok). | `lgupdb01.cbl:286-288`, `:408-411`, `:302-307`, `:362-371` | İş kuralı |
| 14-10 | 14-01 ile aynı, ama [B4] `ENDOWMENT` UPDATE'inde 0 ve 100 dışında bir `SQLCODE` (örn. `-904`), `CLOSE` `0`. | `CA-RETURN-CODE = 00`. `90` `CLOSE`'da ezilir. | Db2'de `ENDOWMENT` ve `POLICY` değişmez (UPDATE hata verdi, `POLICY` UPDATE'ine geçilmez), VSAM'a yazılmaz (`RETURN`).<br>Yan etki: LGUPDB01, `LGSTSQ`'ya iki LINK yapar. (1) `ERROR-MSG`: 14-06'daki biçimde, `EM-SQLREQ` `' UPDATE ENDOW '` + 2 boşluk. (2) `CA-ERROR-MSG`: COMMAREA'nın ilk 90 baytı. | `lgupdb01.cbl:393-418`, `:302-307`, `:362-371` | İş kuralı |
| 14-11 | 14-01 ile aynı, ama [B4] `POLICY` UPDATE'inde `SQLCODE ≠ 0` (örn. `-904`); sonraki `SELECT LASTCHANGED` başarılı (`0`). | `CA-RETURN-CODE = 00` | `POLICY` UPDATE'inin hatası görülmez: kontrol edilen yalnızca `SELECT`'in `SQLCODE`'udur (Kod gözlemleri 6). `ENDOWMENT` UPDATE'i yapılmıştır; `POLICY` satırı değişmez; `CA-LASTCHANGED` Db2'deki (eski) değerle döner. LGUPVS01 çağrılır, VSAM kaydı yeniden yazılır. Hata mesajı yazılmaz. | `lgupdb01.cbl:318-336`, `:360`, `:209-212` | İş kuralı |
| 14-12 | 14-01 ile aynı, ama [B4] `SELECT LASTCHANGED`'da `SQLCODE ≠ 0` (örn. `-904`); `CLOSE` `0`. | `CA-RETURN-CODE = 00`. `SELECT` hatasının `90`'ı `CLOSE`'da ezilir. | `EXEC CICS SYNCPOINT ROLLBACK` çalışır: o ana kadarki Db2 değişiklikleri (`ENDOWMENT`, `POLICY` UPDATE'leri) geri alınır. Sonra LGUPVS01 çağrılır ve VSAM kaydı yeni değerlerle yeniden yazılır (geri almadan sonra): Db2 eski değerlerde, VSAM yeni değerlerde kalır.<br>Yan etki: LGUPDB01, `LGSTSQ`'ya iki LINK yapar. (1) `ERROR-MSG`: `EM-SQLREQ` `' UPDATE POLICY  '`. (2) `CA-ERROR-MSG`: COMMAREA'nın ilk 90 baytı. | `lgupdb01.cbl:329-342`, `:360`, `:369-371`, `:209-212` | İş kuralı |
| 14-13 | 14-12 ile aynı, ama [B4] `CLOSE` `SQLCODE = -501` (imleç `ROLLBACK` ile kapanmıştır). | `CA-RETURN-CODE = 00` | 14-12'deki geri alma geçerlidir. `CLOSE-PCURSOR` `-501` dalında `00` yazar ve `RETURN` yapar: LGUPVS01 çağrılmaz, VSAM'a yazılmaz. Db2 ve VSAM eski değerlerinde kalır. | `lgupdb01.cbl:329-342`, `:360`, `:372-375` | İş kuralı |
| 14-14 | 14-01 ile aynı, ama [B4] `CLOSE`'da 0 ve -501 dışında bir `SQLCODE` (örn. `-904`). | `CA-RETURN-CODE = 90` | `ENDOWMENT` ve `POLICY` UPDATE'leri `CLOSE`'dan önce yapıldığı için Db2'de değişiklik vardır ve kodda geri alma yoktur. LGUPVS01 çağrılmaz (`RETURN`): VSAM değişmez, Db2 ile VSAM tutarsız kalır.<br>Yan etki: LGUPDB01, `LGSTSQ`'ya iki LINK yapar. (1) `ERROR-MSG`: `EM-SQLREQ` `' CLOSE  PCURSOR'` + 1 boşluk. (2) `CA-ERROR-MSG`: COMMAREA'nın ilk 90 baytı. | `lgupdb01.cbl:360`, `:376-379`, `:364` | İş kuralı |
| 14-15 | 14-01 ile aynı, ama [B4] VSAM `READ`'de `RESP ≠ NORMAL` (kayıt yok; örn. `KSDSPOLY`'den `E0000000008` + `0000000004` silinmiş). | `CA-RETURN-CODE`'a `81` yazılır, ardından görev `ABEND` kodu `LGV3` ile (NODUMP) sonlanır. **Gerçek CICS:** dönüş kodu ulaşmaz [CICS bilgisi]. **Düzenekte:** `CA-RETURN-CODE = 81` görünür. | Db2 UPDATE'leri önceden çalıştı; gerçek CICS'te abend olduğu için **geri alınır** ve poliçe 4 eski değerlerinde kalır (`db2cre.jcl:849-857`, `:1033-1040`) [CICS bilgisi] (Kod gözlemleri 9). Düzenekte geri alma yoktur. VSAM'a yazılmaz.<br>Yan etki: LGUPVS01'in iki mesajı (14-03'teki gibi, `' CNUM='` + `0000000008`). | `lgupdb01.cbl:209-212`<br>`lgupvs01.cbl:139-153`<br>[belge] `base/Architecture.md:48` | İş kuralı |
| 14-16 | 14-01 ile aynı, ama [B4] VSAM `READ` `NORMAL`, `REWRITE`'ta `RESP ≠ NORMAL`. | `CA-RETURN-CODE`'a `82` yazılır, ardından görev `ABEND` kodu `LGV4` ile (NODUMP) sonlanır. **Gerçek CICS:** dönüş kodu ulaşmaz [CICS bilgisi]. **Düzenekte:** `CA-RETURN-CODE = 82` görünür. | Gerçek CICS'te Db2 UPDATE'leri ve VSAM kaydı **geri alınır**: poliçe 4 eski değerlerinde, VSAM kaydı başlangıç halinde (`ksdspoly.txt:4`) kalır [CICS bilgisi]. Düzenekte geri alma yoktur. VSAM kümesi geri alınabilir tanımlı ([kod] `adef121.jcl:45`, `LOG(UNDO)`).<br>Yan etki: LGUPVS01'in iki mesajı. Metin yine `' Re-write  KSDSPOLY '`; `RESP` ve `RESP2` REWRITE'ınki. İkinci mesajda `82` var. | `lgupvs01.cbl:155-166`, `:174-205`<br>`adef121.jcl:45`<br>[belge] `base/Architecture.md:48` | İş kuralı |
| 14-17 | LGUPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01XEND'` (tanınmayan kod; 4. karakter `E`). Diğer girdiler 14-01 ile aynı. [B4] Tüm SQL ve dosya komutları başarılı. | `CA-RETURN-CODE = 00`. LGUPOL01'in `99`'u LGUPDB01'de `00` ile ezilir. | `POLICY` satırı güncellenir (`issueDate`, `expiryDate`, `brokerId`, `brokersReference`, `lastChanged`); **`ENDOWMENT` satırı güncellenmez** (`EVALUATE`'ta eşleşme yok). VSAM kaydı anahtar harfi `E` olduğu için bulunur ve girdideki hayat sigortası değerleriyle yeniden yazılır. `CA-LASTCHANGED` yeni zaman damgası olur. Hata mesajı yazılmaz. LGUPOL01 uzunluk kontrolü bu kodda yapılmaz. | `lgupol01.cbl:113`, `:139-140`, `:143`<br>`lgupdb01.cbl:190`, `:278`, `:283-300`, `:302`, `:313-334`, `:360`<br>`lgupvs01.cbl:102`, `:113-118`, `:139-159` | Arayüz (COMMAREA) kuralı |
| 14-18 | 14-17 ile aynı, ama `CA-REQUEST-ID = '01UXXX'` (4. karakter `X`). [B4] `READ`'de `RESP ≠ NORMAL`. | `CA-RETURN-CODE`'a `81` yazılır, ardından görev `ABEND` kodu `LGV3` ile (NODUMP) sonlanır. **Gerçek CICS:** dönüş kodu ulaşmaz [CICS bilgisi]. **Düzenekte:** `CA-RETURN-CODE = 81` görünür. | LGUPVS01 anahtar harfi `X` ile kayıt arar; `WHEN OTHER` dalı `WF-Policy-Data`'yı boşluk yapar (`lgupvs01.cbl:133-134`). `X` anahtarlı kayıt VSAM'da yok. `POLICY` UPDATE'i abend öncesi yapılmıştır; gerçek CICS'te abend ile geri alınır [CICS bilgisi], düzenekte kalır. Yan etki: LGUPVS01'in iki mesajı. | `lgupol01.cbl:139-143`<br>`lgupdb01.cbl:190`, `:283-300`, `:313-326`<br>`lgupvs01.cbl:102`, `:133-134`, `:139-153` | Arayüz (COMMAREA) kuralı |
| 14-19 | LGUPOL01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGUPDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGUPOL01'`, `' NO COMMAREA RECEIVED'`). COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz). | `lgupol01.cbl:99-103`, `:44-49`, `:169-186` | Arayüz (COMMAREA) kuralı |
| 14-20 | LGUPOL01 çağrılır. `EIBCALEN` 8 ile 151 arasında (sınır değer: `151`). `CA-REQUEST-ID = '01UEND'` (istek kodu ve dönüş kodu COMMAREA'nın içinde). | `CA-RETURN-CODE = 98` | LGUPDB01 ve LGUPVS01 çağrılmaz, hiçbir tablo değişmez, hata mesajı yazılmaz (`lgupol01.cbl:118-121` arasında `WRITE-ERROR-MESSAGE` yok). | `lgupol01.cbl:105`, `:115-121`<br>`lgupol01.cbl:39`, `:62` | Arayüz (COMMAREA) kuralı |
| 14-21 | LGUPOL01 çağrılır. `EIBCALEN` 6 ile 7 arasında. `CA-REQUEST-ID = '01UEND'` (tamamı içeride). | [B1] Tamponun 7-8. baytları `98` olur (ön koşullu) [kod] `lgupol01.cbl:119`. | Kod `98`'i yazıp `RETURN` yapar. `CA-RETURN-CODE` 7-8. baytlarda olduğu için gelen COMMAREA'nın dışında kalır (7'de yarısı) ve B1 ile çağıranın tamponuna düşer. LGUPDB01 çağrılmaz, hata mesajı yazılmaz.<br>`EIBCALEN` 1 ile 5 arasında istek kodu kısmen COMMAREA dışındadır; sonuç **belirsiz** (B1 ile boşluk okunur ve 14-17 yoluna benzer). | `lgcmarea.cpy:10-11`<br>`lgupol01.cbl:113`, `:115-121` | Arayüz (COMMAREA) kuralı |
| 14-22 | LGUPOL01 çağrılır. `EIBCALEN = 152` (kabul edilen en küçük uzunluk). `CA-REQUEST-ID = '01UEND'`, alanlar 14-01 ile aynı ve hepsi COMMAREA'nın içinde, `CA-LASTCHANGED` Db2'deki değer. [B4] Tüm SQL ve dosya komutları başarılı. | `98` **dönmez**. `CA-RETURN-CODE = 00`. | Uzunluk kontrolü geçilir (`152 < 152` yanlış). `UPDATE ENDOWMENT`'ın okuduğu bütün alanlar 152 baytın içinde olduğundan sonuç 14-01 ile aynıdır (motor güncellemesindeki `06-22`'den fark: orada `CA-M-PREMIUM` ve `CA-M-ACCIDENTS` kontrolün dışında kalıyordu). | `lgupol01.cbl:115-121`<br>`lgupdb01.cbl:390-391`, `:394-406`<br>`lgcmarea.cpy:47-53` | Arayüz (COMMAREA) kuralı |
| 14-23 | **LGUPDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). [B2] `SQLCODE = 0`. | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz, LGUPVS01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 87 baytlık `ERROR-MSG`. İçeriği: tarih, saat, `' LGUPDB01'`, `' NO COMMAREA RECEIVED'` + 36 boşluk + 6 baytlık `EM-SQLRC`. Taşıma 63 baytlık `EM-VARIABLE` grubunun tamamını ezdiği için `' CNUM='`, `' PNUM='`, `' SQLCODE='` sabitleri mesajda yer almaz. [B2] `EM-SQLRC = +00000` [kod] `lgupdb01.cbl:504`. COMMAREA dökümü gönderilmez.<br>LGUPOL01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 2). | `lgupdb01.cbl:183-187`, `:44-51`, `:502-520` | Arayüz (COMMAREA) kuralı |
| 14-24 | **LGUPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (örn. 50). `CA-REQUEST-ID = '01UEND'`, `CA-LASTCHANGED` (49-74. baytlar) kısmen COMMAREA dışında. [B4] `OPEN`'da `SQLCODE = -913`. | `CA-RETURN-CODE = 90` | LGUPDB01'de uzunluk kontrolü yok; `98` dönmez. Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG`: 14-06 ile aynı. (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır.<br>`OPEN` başarılı olsaydı sonuç **belirsiz** olurdu: `CA-LASTCHANGED`, `CA-ISSUE-DATE` vb. COMMAREA dışından okunur. | `lgupdb01.cbl:254-265`, `:520-526`<br>`lgupdb01.cbl:63-65`, `:201-204` | Arayüz (COMMAREA) kuralı |
| 14-25 | **LGUPVS01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (anahtardaki müşteri ve poliçe numaraları 9-28. baytlarda, içeride kalır). `CA-REQUEST-ID = '01UEND'`. [B4] `READ`'de `RESP ≠ NORMAL`. | `CA-RETURN-CODE`'a `81` yazılır, ardından görev `ABEND` kodu `LGV3` ile (NODUMP) sonlanır. Gerçek CICS'te dönüş kodu ulaşmaz [CICS bilgisi], düzenekte `81` görünür (14-03 ile aynı). | Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG`: 14-03'teki gibi. (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`. Bu dala yalnızca bu yolla ulaşılır. | `lgupvs01.cbl:100-104`, `:139-153`, `:191-197` | Arayüz (COMMAREA) kuralı |
| 14-26 | **LGUPVS01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | **belirsiz** | **belirsiz**. LGUPVS01'de `EIBCALEN` kontrolü yok. `READ`, var olmayan COMMAREA'daki alanları anahtar olarak kullanır. Sonuç kodda tanımlı değil. | `lgupvs01.cbl:100-104`, `:139-146` | Arayüz (COMMAREA) kuralı |
| 14-27 | 14-01 ile aynı, ama `CA-PAYMENT = 000123` (Db2'deki değer 0: `db2cre.jcl:857`). | `CA-RETURN-CODE = 00` | `POLICY.payment` **0 olarak kalır**: `PAYMENT` UPDATE'in `SET` listesinde yok. Diğer alanlar 14-01 gibi güncellenir. Sonraki bir sorgulama (`13`) `CA-PAYMENT = 000000` döndürür. | `lgupdb01.cbl:314`, `:320-324`, `:126-143` | İş kuralı |
| 14-28 | 14-01 ile aynı, ama [B4] poliçe 4'ün `ENDOWMENT.paddingData` değeri `ABCDEFGHIJ` ve girdideki `CA-E-PADDING-DATA` `ZZZZZZZZZZ`. | `CA-RETURN-CODE = 00` | `ENDOWMENT.paddingData` **`ABCDEFGHIJ` olarak kalır**: `UPDATE ENDOWMENT` listesinde `PADDINGDATA` yok ve `CA-E-PADDING-DATA` hiçbir yerde okunmuyor (Kod gözlemleri 8). Diğer yedi sütun 14-01 gibi güncellenir. | `lgupdb01.cbl:394-406`<br>`lgcmarea.cpy:54` | İş kuralı |
| 14-29 | 14-01 ile aynı, ama `CA-E-LIFE-ASSURED` = `ABCDEFGHIJKLMNOPQRSTUVWXYZ12345` (31 karakter). | `CA-RETURN-CODE = 00` | `ENDOWMENT.lifeAssured` (`CHAR(31)`) 31 karakterin hepsini alır. VSAM'da yaşam sigortalısı alanı 30 baytlık: kayıtta `ABCDEFGHIJKLMNOPQRSTUVWXYZ1234` yazılır, **31. karakter (`5`) VSAM'a yazılmaz**. Db2 ile VSAM aynı değeri taşımaz. | `lgcmarea.cpy:53`<br>`lgupvs01.cbl:41`, `:118`<br>`lgupdb01.cbl:403`<br>`db2cre.jcl:211` | İş kuralı |
| 14-30 | 14-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000008` (10 haneli). [B3] [B4] `READ`'de `RESP ≠ NORMAL` (`E1000000008` + `0000000004` anahtarlı kayıt VSAM'da yok). | [B3] (ön koşullu) `CA-RETURN-CODE`'a `81` yazılır, ardından görev `ABEND` kodu `LGV3` ile (NODUMP) sonlanır [kod] `lgupvs01.cbl:149-151`. **Gerçek CICS:** dönüş kodu ulaşmaz [CICS bilgisi]. **Düzenekte:** `CA-RETURN-CODE = 81` görünür. | [B3] `PIC 9(10)` → `PIC S9(9) COMP` taşımasında en soldaki hane kesilir ve `DB2-CUSTOMERNUM-INT = 8` olur: Db2'de **müşteri 8'in poliçe 4'ü** güncellenir (UPDATE'ler başarılı) [kod] `lgcmarea.cpy:12`, `lgupdb01.cbl:87`, `:195`. VSAM ise 10 haneli `1000000008`'i kesilmeden kullanır [kod] `lgupvs01.cbl:104`, bu anahtar VSAM'da yok. Db2 güncellemeleri gerçek CICS'te abend yüzünden **geri alınır** [CICS bilgisi]; düzenekte kalır.<br>B3 sağlanmazsa sonuç **belirsiz**. | `lgcmarea.cpy:12`<br>`lgupdb01.cbl:87`, `:195`<br>`lgupvs01.cbl:104`, `:139-153`<br>`cobol.jcl:44-50`, `ksdspoly.txt:3-4`<br>[belge] `base/Architecture.md:48` | Arayüz (COMMAREA) kuralı |
| 14-31 | 14-01 ile aynı, ama `CA-CUSTOMER-NUM` ya da `CA-POLICY-NUM` sayısal değil (örn. boşluk). | **belirsiz** | **belirsiz**. Hiçbir programda sayısallık kontrolü yok. Değer doğrudan `DB2-CUSTOMERNUM-INT`/`DB2-POLICYNUM-INT`'e taşınır ve VSAM anahtarı olarak da kullanılır. | `lgupdb01.cbl:195-196`<br>`lgupvs01.cbl:103-104` | Arayüz (COMMAREA) kuralı |
| 14-32 | 14-01 ile aynı. LGUPDB01'e ya da LGUPVS01'e yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. İki LINK'te de `RESP`/`NOHANDLE` yok, programlarda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgupol01.cbl:157-160`<br>`lgupdb01.cbl:209-212` | Arayüz (COMMAREA) kuralı |

## 14-01 girdi ve beklenen değerler

Poliçe 4 (müşteri 8), poliçe 5'in ve hayat sigortası 5'in değerleriyle güncellenir. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Girdi | Girdinin kaynağı (`db2cre.jcl`) | Dönüşte beklenen | Alanı kullanan COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` X(6) | `01UEND` | — (değer koddan: `lgupdb01.cbl:286`) | Değişmez | `lgupol01.cbl:115`, `lgupdb01.cbl:283`, `:286` |
| `CA-RETURN-CODE` 9(2) | Herhangi bir değer | — | `00` | `lgupol01.cbl:105`, `lgupdb01.cbl:190`, `:261`, `:371` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000008` | `:850` | Değişmez | `lgupdb01.cbl:195` |
| `CA-POLICY-NUM` 9(10) | `0000000004` | `:849` | Değişmez | `lgupdb01.cbl:196` |
| `CA-ISSUE-DATE` X(10) | `2011-10-01` | `:827` | Değişmez | `lgupdb01.cbl:320` |
| `CA-EXPIRY-DATE` X(10) | `2012-09-30` | `:828` | Değişmez | `lgupdb01.cbl:321` |
| `CA-LASTCHANGED` X(26) | Db2'deki değer (poliçe 4: `2011-08-05 10:45:19`, `:854`; biçim **belirsiz**) | `:854` | Yeni zaman damgası (eskisinden farklı; biçim **belirsiz**) | `lgupdb01.cbl:278`, `:322`, `:329-334` |
| `CA-BROKERID` 9(10) | `0000000000` | `:831` | Değişmez | `lgupdb01.cbl:313`, `:323` |
| `CA-BROKERSREF` X(10) | ␣×10 (JCL'de `''`) | `:832` | Değişmez | `lgupdb01.cbl:324` |
| `CA-PAYMENT` 9(6) | `000000` | `:833` | Değişmez (Db2'ye yazılmaz) | `lgupdb01.cbl:314` |
| `CA-E-WITH-PROFITS` X | `N` | `:1016` | Değişmez | `lgupdb01.cbl:397`, `lgupvs01.cbl:114` |
| `CA-E-EQUITIES` X | `N` | `:1015` | Değişmez | `lgupdb01.cbl:398`, `lgupvs01.cbl:115` |
| `CA-E-MANAGED-FUND` X | `N` | `:1017` | Değişmez | `lgupdb01.cbl:399`, `lgupvs01.cbl:116` |
| `CA-E-FUND-NAME` X(10) | `SHEPPA` + ␣×4 | `:1018` (sütun `CHAR(10)`: `:208`) | Değişmez | `lgupdb01.cbl:400`, `lgupvs01.cbl:117` |
| `CA-E-TERM` 99 | `10` | `:1019` | Değişmez | `lgupdb01.cbl:390`, `:401` |
| `CA-E-SUM-ASSURED` 9(6) | `050000` | `:1020` | Değişmez | `lgupdb01.cbl:391`, `:402` |
| `CA-E-LIFE-ASSURED` X(31) | `Shep` + ␣×27 | `:1021` (sütun `CHAR(31)`: `:211`) | Değişmez | `lgupdb01.cbl:403`, `lgupvs01.cbl:118` |
| `CA-E-PADDING-DATA` X(32348) | Herhangi bir değer | — | Değişmez | Kodda okunmuyor, yazılmıyor |

## 14-01'den sonra Db2'de ve VSAM'da beklenen durum

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `POLICY` | Poliçe 4'ün satırı: `policyNumber` = 4 ve `customerNumber` = 8 (değişmez), `issueDate` = 2011-10-01, `expiryDate` = 2012-09-30, `policyType` = `E` (değişmez), `lastChanged` = güncelleme anındaki zaman damgası, `brokerId` = 0, `brokersReference` = boşluk, `payment` = 0 (değişmez). Önceki `issueDate`/`expiryDate` değerleri (`db2cre.jcl:851-852`) kalmaz. Diğer poliçelerin satırları değişmez. | `lgupdb01.cbl:318-326` |
| `ENDOWMENT` | Poliçe 4'ün satırı: `withProfits` = `N`, `equities` = `N`, `managedFund` = `N`, `fundName` = `SHEPPA`, `term` = 10, `sumAssured` = 50000, `lifeAssured` = `Shep`. `paddingData` değişmez (NULL). Önceki değerler (`db2cre.jcl:1034-1040`) kalmaz. Hayat sigortası 5'in satırı değişmez. | `lgupdb01.cbl:394-406` |
| `KSDSPOLY` | Anahtarı `E0000000008` + `0000000004` olan kayıt 64 bayt ile yeniden yazılır: `E` + `0000000008` + `0000000004` + `N` + `N` + `N` + `SHEPPA` + ␣×4 + `Shep` + ␣×26. Kaydın başlangıç hali (`ksdspoly.txt:4`) tamamen ezilir. Diğer kayıtlar değişmez. | `lgupvs01.cbl:102-104`, `:113-118`, `:155-159` |
| `CUSTOMER` | Değişmez. Güncelleme zincirinde müşteri tablosuna giden SQL yok. | `lgupdb01.cbl:318-334`, `:394-406` |

## Kapsam tablosu

Koddaki her `IF`/`EVALUATE` dalının ve dönüş kodunun, onu kapsayan test numaraları.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| UO-01 | `lgupol01.cbl:99-103` `IF EIBCALEN = 0` → ABEND `LGCA` | 14-19 | |
| UO-02 | `lgupol01.cbl:105` `'00'` → `CA-RETURN-CODE` | 14-01, 14-20, 14-21 | 14-20/14-21: kontrolden önce yazılır |
| UO-03 | `lgupol01.cbl:115-121` `WHEN '01UEND'`, `IF EIBCALEN < 152` → `98` (doğru ve yanlış dal) | 14-20, 14-21 (doğru dal); 14-01, 14-22 (yanlış dal) | |
| UO-04 | `lgupol01.cbl:139-140` `WHEN OTHER` → `99` | 14-17, 14-18 | `99` çağırana ulaşmaz (Kod gözlemleri 3) |
| UO-05 | `lgupol01.cbl:143`, `:157-160` `PERFORM`, LINK LGUPDB01 `LENGTH(32500)` | 14-01, 14-17, 14-32 | |
| UO-06 | `lgupol01.cbl:169-201` `WRITE-ERROR-MESSAGE` | 14-19 | Dış `IF EIBCALEN > 0` yanlış. İç dallar (`lgupol01.cbl:186-199`) ölü kod (Kod gözlemleri 13) |
| UD-01 | `lgupdb01.cbl:183-187` `IF EIBCALEN = 0` → ABEND `LGCA` | 14-23 | Yalnızca doğrudan çağrı |
| UD-02 | `lgupdb01.cbl:190` `'00'` | 14-01, 14-17 | |
| UD-03 | `lgupdb01.cbl:195-199` `CA-CUSTOMER-NUM`, `CA-POLICY-NUM` → host değişkenleri, `EM-*` | 14-01, 14-04, 14-30, 14-31 | |
| UD-04 | `lgupdb01.cbl:254-270` `OPEN`, `EVALUATE SQLCODE` (0, -913, other) | 14-01 (`0`), 14-06 (`-913`), 14-07 (other) | |
| UD-05 | `lgupdb01.cbl:273-275`, `:349-358` `IF SQLCODE = 0` / `100` / diğer | 14-01 (0), 14-03 (100), 14-08 (diğer) | |
| UD-06 | `lgupdb01.cbl:278`, `:344-346` `IF CA-LASTCHANGED EQUAL TO DB2-LASTCHANGED` → `02` | 14-01 (doğru dal), 14-02 (yanlış dal) | |
| UD-07 | `lgupdb01.cbl:283-300` `EVALUATE CA-REQUEST-ID` | 14-01 (`WHEN '01UEND'`), 14-17, 14-18 (eşleşme yok) | Hayat sigortası dışı `WHEN` kolları kapsam dışı; `WHEN OTHER` yok |
| UD-08 | `lgupdb01.cbl:302-307` `IF CA-RETURN-CODE NOT EQUAL '00'` → `CLOSE`, `RETURN` | 14-09, 14-10 (doğru dal); 14-01 (yanlış dal) | |
| UD-09 | `lgupdb01.cbl:313-326` `POLICY` UPDATE (SQLCODE kontrolsüz) | 14-01, 14-11, 14-27 | Kod gözlemleri 6, 7 |
| UD-10 | `lgupdb01.cbl:329-342` `SELECT LASTCHANGED`, `IF SQLCODE NOT EQUAL 0` → `ROLLBACK`, `90` | 14-12, 14-13 (doğru dal); 14-01 (yanlış dal) | |
| UD-11 | `lgupdb01.cbl:360`, `:365-380` `CLOSE`, `EVALUATE SQLCODE` (0, -501, other) | 14-01 (`0`), 14-13 (`-501`), 14-14 (other) | |
| UD-12 | `lgupdb01.cbl:387-418` `UPDATE-ENDOW-DB2-INFO`, `IF SQLCODE NOT EQUAL 0` → `01` / `90` | 14-01 (yanlış dal), 14-09 (`100`), 14-10 (diğer) | |
| UD-13 | `lgupdb01.cbl:209-216` LINK LGUPVS01 `LENGTH(225)`, RETURN | 14-01, 14-02, 14-32 | |
| UD-14 | `lgupdb01.cbl:502-535` `WRITE-ERROR-MESSAGE` | 14-06 (`ELSE`), 14-24 (`< 91`), 14-23 (dış `IF` yanlış) | |
| UV-01 | `lgupvs01.cbl:100-104`, `:137` anahtar kurma | 14-01, 14-30, 14-31 | |
| UV-02 | `lgupvs01.cbl:106-135` `EVALUATE WF-Request-ID` | 14-01 (`WHEN 'E'`), 14-18 (`WHEN OTHER`) | `C`, `H`, `M` kolları kapsam dışı |
| UV-03 | `lgupvs01.cbl:139-153` `READ UPDATE`; `RESP ≠ NORMAL` → `81`, ABEND `LGV3` | 14-03, 14-15, 14-18, 14-25, 14-30 (doğru dal); 14-01 (yanlış dal) | |
| UV-04 | `lgupvs01.cbl:155-166` `REWRITE`; `RESP ≠ NORMAL` → `82`, ABEND `LGV4` | 14-16 (doğru dal); 14-01 (yanlış dal) | |
| UV-05 | `lgupvs01.cbl:174-206` `WRITE-ERROR-MESSAGE` | 14-03 (`ELSE`), 14-25 (`< 91`) | `EIBCALEN = 0` dalı: LGUPVS01'de `EIBCALEN` kontrolü yok (14-26) |
| SQ-01 | `lgstsq.cbl:68-71` LINK yolu | 14-03, 14-06, 14-08, 14-10, 14-12, 14-14, 14-15, 14-16, 14-19, 14-23, 14-24, 14-25 | |
| SQ-02 | `lgstsq.cbl:72-80` terminalden RECEIVE | — | **Kapsam dışı: bu zincirden ulaşılamaz.** LGSTSQ yalnızca LINK ile çağrılır. |
| SQ-03 | `lgstsq.cbl:94-111` WRITEQ TD `CSMT`, TS `GENAERRS` | 14-03, 14-06 | |
| N-01 | `LASTCHANGED` uyuşmazlığı çağırana iletilmez | 14-02 | Kod gözlemleri 4 |
| N-02 | Poliçe yokluğu (`01`) çağırana iletilmez | 14-03, 14-09 | Kod gözlemleri 5 |
| N-03 | `CA-PAYMENT` Db2'ye yazılmaz | 14-27 | Kod gözlemleri 7 |
| N-04 | `paddingData` güncellenmez | 14-28 | Kod gözlemleri 8 |
| N-05 | `LIFE-ASSURED` VSAM'da 30 bayta kesilir | 14-29 | Kod gözlemleri 10 |
| N-06 | `CA-CUSTOMER-NUM` kesilmesi (B3) | 14-30 | |
| N-07 | Sayısallık ve aralık kontrolü yok | 14-30, 14-31 | |
| N-08 | LINK'te `RESP` yok | 14-32 | |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları").

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 14-01 | B4 | Başarı yolu | Girdi kurulamaz |
| 14-02 | B4 | Girdi düzenekle kurulur (`CLOSE` `0`); tahmin (`00`) değişmez | Girdi kurulamaz |
| 14-03 | B4 | Girdi düzenekle kurulur (`FETCH` `100`, VSAM `READ` hatası); tahmin (`LGV3`) değişmez | Girdi kurulamaz |
| 14-04 … 14-08 | B4 | Girdi düzenekle kurulur; tahmin değişmez | Girdi kurulamaz |
| 14-10 … 14-14 | B4 | Girdi düzenekle kurulur; tahmin değişmez | Girdi kurulamaz |
| 14-15 | B4 | Girdi düzenekle kurulur (VSAM'dan poliçe 4'ün kaydı silinir); tahmin (`LGV3`) değişmez | Girdi kurulamaz: başlangıç verisinde VSAM kaydı var |
| 14-16 | B4 | Girdi düzenekle kurulur; tahmin (`LGV4`) değişmez | Girdi kurulamaz |
| 14-17, 14-18 | B4 | Girdi düzenekle kurulur | Girdi kurulamaz |
| 14-21 | B1 | Tamponun 7-8. baytları `98` | Dönüş kodu belirsiz |
| 14-22 | B4 | Başarı yolu | Girdi kurulamaz |
| 14-23 | B2 | `EM-SQLRC = +00000` | `EM-SQLRC` belirsiz |
| 14-24, 14-25 | B4 | Girdi düzenekle kurulur; `< 91` dalı | Girdi kurulamaz |
| 14-28 | B4 | Dolgusu olan satır düzenekle kurulur | Girdi kurulamaz: başlangıç verisinde dolgu NULL (`db2cre.jcl:1004-1012`) |
| 14-30 | B3, B4 | Db2'de müşteri 8'in poliçe 4'ü güncellenir, VSAM `READ` hatası → `LGV3` | Dönüş kodu ve veri belirsiz |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 14-01 | Beklenen veri (yalnızca `CA-LASTCHANGED` biçimi) | `lastChanged` sütunu `TIMESTAMP` türünde (`db2cre.jcl:168`) ve doğrudan `PIC X(26)` alana okunuyor (`lgupdb01.cbl:329-334`). Okunurken hangi biçimde geleceği Db2 ayarına bağlı ve kodda yazmıyor. |
| 14-21 | Dönüş kodu (`EIBCALEN` 1-5) | İstek kodu kısmen COMMAREA dışında; B1 ile boşluk okunur ama kodda tanımlı değil. |
| 14-24 | Dönüş kodu (`OPEN` başarılı olsaydı) | `CA-LASTCHANGED`, `CA-ISSUE-DATE` vb. COMMAREA dışından okunur. |
| 14-26 | Dönüş kodu, beklenen veri | LGUPVS01'de `EIBCALEN` kontrolü yok. COMMAREA olmadan alanlara erişimin sonucu kodda tanımlı değil. |
| 14-31 | Dönüş kodu, beklenen veri | Sayısal olmayan veri sayısal alana taşınıyor ve hiçbir doğrulama yok. Sonuç tanımsız. |
| 14-32 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil. |
