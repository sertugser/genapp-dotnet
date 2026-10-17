# 12 — Hayat sigortası ekle (Add Endowment Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Hayat sigortası (endowment) ekle: yeni poliçe Db2 `POLICY` ve `ENDOWMENT` tablolarına, hayat kaydının bir bölümü de VSAM `KSDSPOLY` dosyasına yazılır. Poliçe türü COMMAREA'daki istek kodundan (`01AEND`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGAPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGAPDB01` (Db2 `POLICY` + `ENDOWMENT` INSERT) → `LINK` → `LGAPVS01` (VSAM yazma). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. Programlar dört poliçe türüne de hizmet eder; yalnızca hayat sigortası (`01AEND`) kolları ve ortak kısımlar okundu. |
| Okunan COBOL dosyaları | `base/src/lgapol01.cbl`, `base/src/lgapdb01.cbl`, `base/src/lgapvs01.cbl`, `base/src/lgstsq.cbl`, `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy` |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` ve `ENDOWMENT` tanımları (`:161-178`, `:203-217`), müşteri 8 (`:675`), poliçe 4 ve hayat sigortası 4'ün verisi (`:849-858`, `:1033-1040`). `base/cntl/adef121.jcl` ve `base/data/ksdspoly.txt`: `KSDSPOLY` tanımı ve başlangıç verisi. COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `base/Architecture.md` (yalnızca Kod gözlemleri 6; kodda doğrulanmadı), `docs/adr/0004-legacy-test-harness.md` (yalnızca Kod gözlemleri 13) |
| Hazırlayan | Oğuz (taslak: Claude Code) |
| Tarih | 2026-10-17 |
| Kaynak commit (`git rev-parse HEAD`) | `13d0e97c05c6a63cbb6ee9ad0af44cbce5e06d66` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge]**: `base/` altındaki ya da `docs/` altındaki açıklama metinleri. Belgeye dayanan bir tahmin kodda doğrulanmamıştır.
- Etiketsiz kaynaklar [kod]'dur.

Ön koşul kodları test düzeneğinin vereceği kararlardır (kodlar `04-motor-add.md` ile aynıdır). Bir kodu taşıyan tahmin
**kesin değil, ön koşulludur**: ön koşul sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda **ve aritmetik sonuçlarda** PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması) kendisi kurar. **Aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir; satırda yalnızca düzeneğin değiştirdiği değer yazılır. |
| B6 | Db2'de `POLICY` tablosuna `DEFAULT` ile daha önce hiç satır eklenmemiştir; ilk kimlik değeri `START WITH` değeridir (1000001: [kod] `db2cre.jcl:162-163`). Örnek poliçeler 1-10 numarasını açıkça verir, kimlik sayacını ilerletmez. |

(B5 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası, hayat sigortası görünümü** (`lgcmarea.cpy:10-13`, `:34-44`, `:46-54`). Üç program da bu düzeni kullanır
   (`lgapol01.cbl:70-71`, `lgapdb01.cbl:134-137`, `lgapvs01.cbl:86-87`). Hayat sigortası isteğinde alanlar:

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

2. **Uzunluk eşikleri iki programda farklıdır.**
   - LGAPOL01 yalnızca başlık uzunluğunu ister: `WS-CA-HEADER-LEN` = 28 (`lgapol01.cbl:59`), `WS-REQUIRED-CA-LEN` önce 0 (`:60`), 28 eklenir
     (`:109`). Kontrol `lgapol01.cbl:113-116`. Eşik: 28.
   - LGAPDB01 28'e hayat sigortası için `WS-FULL-ENDOW-LEN` = 124 ekler: toplam **152** (`lgapdb01.cbl:65-66`, `:182`, `:186-188`, `:210-213`;
     `lgpolicy.cpy:24`). 124 = 72 (ortak: `lgpolicy.cpy:18`) + 52 (hayat sigortası sabit kısmı: `lgpolicy.cpy:19`).
   - 152 bayt, 1-152'yi, yani `CA-E-LIFE-ASSURED`'ın sonuna kadar olan **bütün sabit alanları** kapsar. Motordan farklı olarak okunan hiçbir sabit alan kontrolün
     dışında kalmaz. `CA-E-PADDING-DATA` (153-) isteğe bağlıdır (Kod gözlemleri 3).
3. **`WS-VARY-LEN` ve `PADDINGDATA`.**
   - `INSERT-ENDOW` `WS-VARY-LEN` = `EIBCALEN` − `WS-REQUIRED-CA-LEN` hesaplar (`lgapdb01.cbl:339-340`). Bu noktada `WS-REQUIRED-CA-LEN` = 152 (`:182`, `:187`).
     `WS-VARY-LEN` `PIC S9(4) COMP` (`lgapdb01.cbl:71`): en çok 4 hane.
   - `WS-VARY-LEN > 0` ise (`:342`) `CA-E-PADDING-DATA` ilk `WS-VARY-LEN` bayt `WS-VARY-CHAR(1:WS-VARY-LEN)`'e taşınır (`:344-345`) ve `PADDINGDATA` dahil INSERT
     yapılır (`:346-366`). Değilse `PADDINGDATA` listede yoktur (`:368-386`); sütun `NULL` kalır (sütun `NOT NULL` değil, varsayılan yok: `db2cre.jcl:212`).
   - `WS-VARY-CHAR` `PIC X(3900)` (`lgapdb01.cbl:72`). `WS-VARY-LEN` > 3900 olursa taşıma `WS-VARY-FIELD`'in dışına taşar; sonuç kodda tanımlı değil.
   - **Zincirde `EIBCALEN` her zaman 32500'dür** (Kod gözlemleri 4). 32500 − 152 = 32348, 4 haneye sığmaz. [B3] En düşük 4 hane kalır: `WS-VARY-LEN` = **2348**. Bu yüzden
     zincirde `WS-VARY-LEN > 0` her zaman doğrudur ve **her zaman `PADDINGDATA`'lı INSERT çalışır**; `PADDINGDATA` uzunluğu 2348'dir ve içeriği COMMAREA'nın 153-2500. baytlarıdır.
     [B1] bu baytlar boşluktur. Aynı sayı B3 olmadan 32348 olurdu ve 3900'ü aşardı (belirsiz).
   - `PADDINGDATA`'sız INSERT'e (`:368-386`) yalnızca LGAPDB01 doğrudan `EIBCALEN = 152` ile çağrılınca ulaşılır.
4. **LGAPOL01, LGAPDB01'i her zaman `LENGTH(32500)` ile çağırır** (`lgapol01.cbl:121-124`). LGAPDB01 her zaman `EIBCALEN = 32500` görür. Bu yüzden LGAPDB01'in kendi "COMMAREA yok"
   (`lgapdb01.cbl:165-169`) ve "COMMAREA kısa / 152" (`:210-213`) kontrollerine LGAPOL01 üzerinden ulaşılamaz. Yalnızca LGAPDB01 doğrudan çağrılınca çalışırlar.
5. **Poliçe türü istek kodundan anlaşılır.** LGAPDB01 `CA-REQUEST-ID`'yi iki kez `EVALUATE` eder (`lgapdb01.cbl:184-207`, `:223-241`). `'01AEND'` → `DB2-POLICYTYPE = 'E'`
   (`:186-188`). Karşılaştırma büyük/küçük harfe duyarlıdır: `UPPER-CASE` yok. LGAPOL01 istek koduna hiç bakmaz. İkinci `WHEN OTHER` (`:237-240`) ölü koddur: tanınmayan kod daha önce
   `:202-205`'te `RETURN` yapar. LGAPVS01 poliçe türünü `CA-REQUEST-ID`'nin 4. karakterinden alır (`lgapvs01.cbl:99`): `'01AEND'` → `'E'`.
6. **Çağrı sırası ve geri alma.**
   - Sıra: `INSERT-POLICY` (`lgapdb01.cbl:219`), `INSERT-ENDOW` (`:225-226`), LGAPVS01'e LINK (`:243-246`).
   - `ENDOWMENT` INSERT hatasında program `ABEND LGSQ` yapar; yorum bunun `POLICY` INSERT'ini geri almak için olduğunu söyler (`lgapdb01.cbl:389-393`).
   - LGAPVS01 hatasında ABEND ya da `ROLLBACK` yoktur: `80` yazar ve `RETURN` yapar (`lgapvs01.cbl:142-147`). LGAPDB01 de dönüş kodunu kontrol etmeden `RETURN` yapar
     (`lgapdb01.cbl:243-250`). Programlar `POLICY` ve `ENDOWMENT` satırlarını geri almaz; `80`, çağırana ulaşır.
   - [belge] `base/Architecture.md:48`: uygulama müşteri ve poliçe verisini Db2'ye iki aşamalı onayla yazar, başarılıysa VSAM'ı günceller. Bu belge ifadesi VSAM hatasında geri alma
     yapıldığını söylemez. `12-19` bu yüzden "satırlar kalır" der; bu, kodda geri alma bulunmamasına dayanır, belgeye değil.
7. **`POLICY` INSERT'inin kuralları** (`lgapdb01.cbl:261-322`).
   - `policyNumber` `DEFAULT` ile verilir (`:279`); sütun `GENERATED BY DEFAULT AS IDENTITY (START WITH 1000001 …)` (`db2cre.jcl:162-163`). Girdideki `CA-POLICY-NUM` hiç okunmaz ve
     INSERT'e girmez. Yeni numara `IDENTITY_VAL_LOCAL()` ile alınır ve `CA-POLICY-NUM`'a yazılır (`:308-311`).
   - `lastChanged` = `CURRENT TIMESTAMP` (`:284`). Girdideki `CA-LASTCHANGED` kullanılmaz. Değer sonradan `SELECT` ile COMMAREA'ya konur (`:316-321`); bu `SELECT`'in SQLCODE'u hiç
     kontrol edilmez.
   - `commission` sütunu INSERT listesinde yoktur (`:270-278`, sütun: `db2cre.jcl:172`).
   - `CA-BROKERID` 10 haneli, host değişkeni `PIC S9(9) COMP` (`lgapdb01.cbl:91`, `:264`): B3 ile en soldaki hane kesilir.
   - `CA-CUSTOMER-NUM` de 10 hanelidir (`lgcmarea.cpy:12`), host değişkeni `PIC S9(9) COMP` (`lgapdb01.cbl:90`, `:176`): B3 ile en soldaki hane kesilir.
8. **Hiçbir alan doğrulanmaz.** Üç COBOL programında da tarih, fon adı, vade, teminat tutarı, `CA-E-WITH-PROFITS` gibi bayraklar için kontrol yok. `POLICY` ve `ENDOWMENT` tanımlarında `CHECK` kısıtı
   yok (`db2cre.jcl:161-176`, `:203-216`). Metin sütunları COMMAREA alanlarıyla aynı genişlikte: `fundName` `CHAR(10)`, `lifeAssured` `CHAR(31)` (`db2cre.jcl:208`, `:211`).
9. **VSAM kaydı** (`lgapvs01.cbl:25-40`, `:99-101`, `:110-115`, `:135-141`).
   - Kayıt 64 bayttır: anahtar 21 bayt (tür harfi 1 + müşteri no 10 + poliçe no 10) + veri 43 bayt. Hayat sigortası verisi: bayrak 1, bayrak 1, bayrak 1, fon adı 10, yaşam sigortalısı **30**
     (`lgapvs01.cbl:36-40`). `CA-E-LIFE-ASSURED` 31 bayttır (`lgcmarea.cpy:53`); VSAM alanı 30 olduğu için **31. karakter VSAM'a yazılmaz** (`lgapvs01.cbl:115`).
     `CA-E-TERM`, `CA-E-SUM-ASSURED` ve `CA-E-PADDING-DATA` VSAM'a yazılmaz.
   - Anahtardaki poliçe numarası, LGAPDB01'in `CA-POLICY-NUM`'a yazdığı yeni numaradır. Anahtardaki müşteri numarası `CA-CUSTOMER-NUM`'un 10 haneli halidir; Db2'ye gidenin kesilmiş hali değildir.
   - Küme `KEYS(21 0)`, `RECORDSIZE(64 64)` ile tanımlıdır [kod] `adef121.jcl:49-50`. Başlangıç verisinde iki hayat sigortası kaydı var: `ksdspoly.txt:3-4`.
10. **Dönüş kodları.**

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgapol01.cbl:105`, `lgapdb01.cbl:172`, `:293` | Başlangıç / `POLICY` INSERT başarılı | Evet |
    | `70` | `lgapdb01.cbl:296` | `POLICY` INSERT `SQLCODE = -530` | Evet |
    | `80` | `lgapvs01.cbl:144` | VSAM yazma hatası | Evet (ABEND yok) |
    | `90` | `lgapdb01.cbl:301` | `POLICY` INSERT'te diğer SQLCODE'lar | Evet |
    | `90` | `lgapdb01.cbl:390` | `ENDOWMENT` INSERT hatası | Belirsiz: hemen ardından `ABEND LGSQ` (`:393`) |
    | `98` | `lgapol01.cbl:114` | COMMAREA < 28 | Evet |
    | `98` | `lgapdb01.cbl:211` | COMMAREA < 152 | Yalnızca LGAPDB01 doğrudan çağrılırsa |
    | `99` | `lgapdb01.cbl:204` | İstek kodu tanınmıyor | Evet |
    | ABEND `LGCA` | `lgapol01.cbl:101`, `lgapdb01.cbl:168` | COMMAREA yok | — |
    | ABEND `LGSQ` | `lgapdb01.cbl:393` | `ENDOWMENT` INSERT hatası | — |

11. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGAPOL01 45 bayt (`lgapol01.cbl:41-46`), LGAPDB01 87 bayt (`lgapdb01.cbl:41-53`), LGAPVS01 101 bayt (`lgapvs01.cbl:58-73`). `CA-ERROR-MSG` üçünde de 99 bayttır:
      `'COMMAREA='` + 90 bayt.
    - LGAPDB01: `EM-SQLREQ` `POLICY` INSERT'inde `' INSERT POLICY'` + 2 boşluk (`:267`), `ENDOWMENT` INSERT'inde `' INSERT ENDOW '` + 2 boşluk (`:333`). `EM-CUSNUM` ← `CA-CUSTOMER-NUM` (`:179`).
      `EM-POLNUM` yalnızca `POLICY` INSERT başarılı olduktan sonra dolar (`:313`): `POLICY` INSERT hatasında 10 boşluktur (`:50`), `ENDOWMENT` INSERT hatasında yeni poliçe numarasıdır.
      `EM-SQLRC` `PIC +9(5)`: `-530` → `-00530` (`:53`, `:564`).
    - LGAPVS01: `' PNUM='` + poliçe no, `' CNUM='` + müşteri no, `' Write file KSDSPOLY'`, `' RESP='`, `' RESP2='` (`lgapvs01.cbl:63-73`, `:165-168`). `RESP2` ← `EIBRESP2` (`:143`).
    - LGAPDB01'in `EIBCALEN = 0` mesajı: 21 karakterlik `' NO COMMAREA RECEIVED'`, 63 baytlık `EM-VARIABLE` grubuna taşınır ve sabitleri ezer (`lgapdb01.cbl:46-53`, `:166`).
      Sonra `MOVE SQLCODE TO EM-SQLRC` son 6 baytı ezer (`:564`).
12. **`WRITE-ERROR-MESSAGE` dalları** (`IF EIBCALEN > 0` / `< 91` / `ELSE`).
    - LGAPOL01'de yalnızca `EIBCALEN = 0` dalından çağrılır (`lgapol01.cbl:100`). İçindeki `IF EIBCALEN > 0` her zaman yanlıştır: `lgapol01.cbl:155-167` arasına hiçbir girdiyle ulaşılamaz (ölü kod).
    - LGAPDB01'de hata çağrılarından biri `EIBCALEN = 0` dalındandır (iç dallar çalışmaz). Diğerleri, 152 sınırını geçtikten sonra yapılır, yani `EIBCALEN ≥ 152` iken. `< 91` dalına
      (`lgapdb01.cbl:581-586`) hiçbir girdiyle ulaşılamaz (ölü kod).
    - LGAPVS01 zincirde `EIBCALEN = 32500` görür, `ELSE` dalı çalışır (`lgapvs01.cbl:180-186`). `< 91` dalına yalnızca doğrudan çağrıyla ulaşılır (`12-25`).
13. **WORKING-STORAGE kalıcılığı.** `WS-REQUIRED-CA-LEN` `VALUE +0` ile tanımlı ve her çağrıda üstüne eklenir (`lgapol01.cbl:60`, `:109`; `lgapdb01.cbl:66`, `:182`, `:187`). WORKING-STORAGE aynı süreçte
    yeniden kurulmazsa ikinci çağrıda eşikler iki katına çıkar (LGAPOL01: 56, LGAPDB01: 304). Test düzeneği her durumu ayrı süreçte çalıştırır [belge] `docs/adr/0004-legacy-test-harness.md` (karar 4).
    Bu yüzden bu dosyadaki tahminler tek çağrı içindir.
14. **Test verisi** (`base/cntl/db2cre.jcl`). Bu değerler yalnızca test veritabanı bu JCL ile yüklendiyse ve sonradan değiştirilmediyse geçerlidir.
    - Müşteri 8: `db2cre.jcl:675`. 12-01'in ortak ve hayat sigortası değerleri, örnek poliçe 4'ün ve hayat sigortası 4'ün değerleridir (`db2cre.jcl:851-857`, `:1034-1040`). Böylece her girdinin
      JCL'de bir kaynağı var. Poliçe 4 zaten Db2'de bulunduğundan 12-01'de yeni satırın numarası 1000001'dir, 4 değil.
    - Olmayan müşteri için `0000000000` kullanılır (12-15): JCL 1-10 numaralarını verir.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 12-01 | LGAPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01AEND'`. `CA-CUSTOMER-NUM = 0000000008` (müşteri 8 Db2'de var: `db2cre.jcl:675`). Ortak ve hayat sigortası alanlarında örnek poliçe 4'ün değerleri var (alan alan aşağıdaki **"12-01 girdi ve beklenen değerler"** tablosunda). `CA-E-PADDING-DATA` boşluk [B1]. [B3] Zincirde `WS-VARY-LEN` 4 haneye kesilir (Kod gözlemleri 3). [B6] Db2'de `POLICY`'ye `DEFAULT` ile hiç satır eklenmemiş. [B4] `POLICY` INSERT, `ENDOWMENT` INSERT ve VSAM yazma başarılı (`SQLCODE = 0`, `RESP = NORMAL`). | `CA-RETURN-CODE = 00` | [B6] `CA-POLICY-NUM = 0001000001`. `CA-LASTCHANGED` = yeni `POLICY` satırının zaman damgası (biçimi **belirsiz**). Diğer COMMAREA alanları girdideki gibi kalır.<br>Yan etkiler: `POLICY`'de yeni satır (tür `E`), `ENDOWMENT`'ta yeni satır, `KSDSPOLY`'de 64 baytlık yeni kayıt (anahtar `E` + `0000000008` + `0001000001`). Ayrıntı aşağıda **"12-01'den sonra Db2'de ve VSAM'da beklenen durum"** tablosunda. Hata mesajı yazılmaz. | `lgapol01.cbl:98`, `:105`, `:113`, `:121-126`<br>`lgapdb01.cbl:172`, `:176`, `:184`, `:186-188`, `:210`, `:219`, `:225-226`, `:261-322`, `:327-397`, `:243-250`<br>`lgapvs01.cbl:99-101`, `:110-115`, `:135-147` | İş kuralı |
| 12-02 | 12-01 ile aynı. Soru: `ENDOWMENT.paddingData` sütununa ne yazılır? [B1] [B3] | `CA-RETURN-CODE = 00` | [B3] `WS-VARY-LEN` = 32348'in en düşük 4 hanesi = **2348** [kod] `lgapdb01.cbl:71`, `:339-340`. `WS-VARY-LEN > 0` olduğu için `PADDINGDATA`'lı INSERT çalışır: `PADDINGDATA` uzunluğu 2348, içeriği COMMAREA'nın 153-2500. baytları. [B1] Bu baytlar boşluk olduğundan sütunda 2348 boşluk olur. (`PADDINGDATA`'sız INSERT zincirde çalışmaz: 12-11.) | `lgapdb01.cbl:71`, `:339-340`, `:342-366`<br>`lgcmarea.cpy:54`<br>`cobol.jcl:44-50` | Arayüz (COMMAREA) kuralı |
| 12-03 | LGAPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01AXXX'` (tanınmayan kod). Diğer girdiler 12-01 ile aynı. | `CA-RETURN-CODE = 99` | LGAPDB01 `99` yazar ve `RETURN` yapar: hiçbir INSERT yapılmaz, LGAPVS01 çağrılmaz, hata mesajı yazılmaz. `CA-POLICY-NUM` değişmez. Dış katman LGAPOL01 de istek koduna bakmadığı için sonucu olduğu gibi döndürür. | `lgapdb01.cbl:184`, `:202-205`<br>`lgapol01.cbl:121-126` | Arayüz (COMMAREA) kuralı |
| 12-04 | 12-01 ile aynı, ama `CA-REQUEST-ID = '01aend'` (küçük harf). | `CA-RETURN-CODE = 99` | 12-03 ile aynı. LGAPDB01 istek kodunu büyük harfe çevirmez (`UPPER-CASE` yok); sorgulamadan (`13-07`) farklı. | `lgapdb01.cbl:184`, `:186`, `:202-205` | Arayüz (COMMAREA) kuralı |
| 12-05 | LGAPOL01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGAPDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGAPOL01'`, `' NO COMMAREA RECEIVED'`). COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz). | `lgapol01.cbl:98-102`, `:41-46`, `:137-155` | Arayüz (COMMAREA) kuralı |
| 12-06 | LGAPOL01 çağrılır. `EIBCALEN` 8 ile 27 arasında (sınır değer: `27`). | `CA-RETURN-CODE = 98` | LGAPDB01 çağrılmaz, hata mesajı yazılmaz (`lgapol01.cbl:113-116` arasında `WRITE-ERROR-MESSAGE` yok). Hiçbir tablo değişmez. | `lgapol01.cbl:105`, `:109`, `:113-116`<br>`lgapol01.cbl:59` | Arayüz (COMMAREA) kuralı |
| 12-07 | LGAPOL01 çağrılır. `EIBCALEN` 1 ile 7 arasında. | [B1] Tamponun 7-8. baytları `98` olur (ön koşullu) [kod] `lgapol01.cbl:114`. | Kod `98`'i yazıp `RETURN` yapar. `CA-RETURN-CODE` 7-8. baytlarda olduğu için gelen COMMAREA'nın dışında kalır (1-6'da tamamen, 7'de yarısı); B1 ile çağıranın tamponuna düşer. Ön koşulsuz kesin olan: `EIBCALEN = 7` ise COMMAREA'nın 7. baytı `9` olur. LGAPDB01 çağrılmaz, hata mesajı yazılmaz. | `lgcmarea.cpy:10-11`<br>`lgapol01.cbl:105`, `:113-116` | Arayüz (COMMAREA) kuralı |
| 12-08 | LGAPOL01 çağrılır. `EIBCALEN = 28` (kabul edilen en küçük uzunluk). `CA-REQUEST-ID = '01AEND'`, `CA-CUSTOMER-NUM = 0000000008`. | `98` **dönmez**. Asıl dönüş kodu **belirsiz**. | LGAPOL01 isteği geçirir (`28 < 28` yanlış) ve LGAPDB01'i `LENGTH(32500)` ile çağırır; LGAPDB01'in kendi 152 sınırı çalışmaz (Kod gözlemleri 4). 29. baytta başlayan her alan COMMAREA'nın dışından okunur. [B1] Hepsi boşluk olur (`CA-ISSUE-DATE` boşluk vb.). Boşluğun `DATE` sütununa yazılıp yazılamayacağı Db2'ye bağlı. | `lgapol01.cbl:113-116`, `:121-124`<br>`lgapdb01.cbl:210-213`, `:281-282` | Arayüz (COMMAREA) kuralı |
| 12-09 | **LGAPDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). [B2] `SQLCODE = 0`. | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz, LGAPVS01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 87 baytlık `ERROR-MSG`. İçeriği: tarih, saat, `' LGAPDB01'`, `' NO COMMAREA RECEIVED'` + 36 boşluk + 6 baytlık `EM-SQLRC`. Taşıma 63 baytlık `EM-VARIABLE` grubunun tamamını ezdiği için `' CNUM='`, `' PNUM='`, `' SQLCODE='` sabitleri mesajda yer almaz. [B2] `EM-SQLRC = +00000` [kod] `lgapdb01.cbl:564`. COMMAREA dökümü gönderilmez.<br>LGAPOL01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 4). | `lgapdb01.cbl:165-169`, `:46-53`, `:562-580` | Arayüz (COMMAREA) kuralı |
| 12-10 | **LGAPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 151 arasında (sınır değer: `151`). `CA-REQUEST-ID = '01AEND'`. | `CA-RETURN-CODE = 98` | Hiçbir INSERT yapılmaz, LGAPVS01 çağrılmaz, hata mesajı yazılmaz (`lgapdb01.cbl:210-213` arasında `WRITE-ERROR-MESSAGE` yok). `CA-POLICY-NUM` değişmez. LGAPOL01 üzerinden bu yola ulaşılamaz. | `lgapdb01.cbl:182`, `:186-187`, `:210-213`<br>`lgapdb01.cbl:65`, `lgpolicy.cpy:24` | Arayüz (COMMAREA) kuralı |
| 12-11 | **LGAPDB01 doğrudan** çağrılır. `EIBCALEN = 152` (kabul edilen en küçük uzunluk). Alanlar 12-01 ile aynı, hepsi COMMAREA'nın içinde. `CA-REQUEST-ID = '01AEND'`. [B4] [B6] Tüm SQL ve dosya komutları başarılı. | `CA-RETURN-CODE = 00` | Uzunluk kontrolü geçilir (`152 < 152` yanlış). `WS-VARY-LEN` = 152 − 152 = 0 olduğundan `PADDINGDATA`'sız INSERT çalışır (`lgapdb01.cbl:367-386`): `ENDOWMENT` satırında `paddingData` **NULL**. Motordan farklı olarak 152 bayt bütün sabit alanları kapsar, COMMAREA dışına okuma yoktur. `POLICY` satırı ve VSAM kaydı 12-01 gibi. | `lgapdb01.cbl:210-213`, `:339-342`, `:367-387`<br>`db2cre.jcl:212` | Arayüz (COMMAREA) kuralı |
| 12-12 | **LGAPDB01 doğrudan** çağrılır. `EIBCALEN = 162`. 153-162. baytlar `ABCDEFGHIJ`. Diğer girdiler 12-11 ile aynı. [B4] [B6] | `CA-RETURN-CODE = 00` | `WS-VARY-LEN` = 10. `PADDINGDATA`'lı INSERT çalışır: `ENDOWMENT.paddingData` = `ABCDEFGHIJ` (uzunluk 10). `CA-E-PADDING-DATA` 32348 bayttan ilk 10'u taşınır (`WS-VARY-CHAR(1:10)`). | `lgapdb01.cbl:339-345`, `:346-366` | Arayüz (COMMAREA) kuralı |
| 12-13 | **LGAPDB01 doğrudan** çağrılır. `EIBCALEN = 4052`. 153-4052. baytlar dolu (3900 bayt). Diğer girdiler 12-11 ile aynı. [B4] [B6] | `CA-RETURN-CODE = 00` | `WS-VARY-LEN` = 3900 = `WS-VARY-CHAR` boyu (sınır değer). `ENDOWMENT.paddingData` uzunluğu 3900, içeriği COMMAREA'nın 153-4052. baytları. | `lgapdb01.cbl:72`, `:339-345` | Arayüz (COMMAREA) kuralı |
| 12-14 | **LGAPDB01 doğrudan** çağrılır. `EIBCALEN = 4053` (`WS-VARY-LEN` = 3901). Diğer girdiler 12-13 ile aynı. | **belirsiz** | **belirsiz**. `WS-VARY-LEN` 3900'ü aşınca taşıma `WS-VARY-CHAR(1:WS-VARY-LEN)` alanı `WS-VARY-FIELD`'in dışına taşar; kodda bunu denetleyen bir şey yok. | `lgapdb01.cbl:72`, `:344-345` | Arayüz (COMMAREA) kuralı |
| 12-15 | LGAPOL01 çağrılır. `EIBCALEN = 32500`, `CA-REQUEST-ID = '01AEND'`, `CA-CUSTOMER-NUM = 0000000000` (olmayan müşteri). [B4] `POLICY` INSERT, `SQLCODE = -530` döner. | `CA-RETURN-CODE = 70` | `POLICY`'ye satır eklenmez, `ENDOWMENT` INSERT ve LGAPVS01 çağrılmaz (`RETURN`, ABEND yok). `CA-POLICY-NUM` değişmez (`IDENTITY_VAL_LOCAL` çalışmaz).<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGAPDB01'`, `' CNUM='` + `0000000000`, `' PNUM='` + 10 boşluk, `' INSERT POLICY'` + 2 boşluk, `' SQLCODE='` + `-00530`.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı (`EIBCALEN = 32500`, `ELSE` dalı). | `lgapdb01.cbl:267-298`, `:179`, `:564-578`, `:587-592`<br>`db2cre.jcl:174-176` | İş kuralı |
| 12-16 | 12-15 ile aynı, ama düzenek INSERT'i kendisi yapar: gerçek Db2, yabancı anahtar tanımı (`db2cre.jcl:174-176`) ile müşteri 0 yok. [B4] yok: SQLCODE'u düzenek kurmaz. | **belirsiz** | **belirsiz**. Kod yalnızca `-530`'u `70`'e eşler. Var olmayan müşteriye INSERT'te Db2'nin `-530` döndüreceği kodda yazmıyor. `-530` dönerse sonuç 12-15'tir. Başka bir kod dönerse 12-17 yolu (`90`) çalışır. | `lgapdb01.cbl:290-305` | İş kuralı |
| 12-17 | 12-01 ile aynı, ama [B4] `POLICY` INSERT'te 0 ve `-530` dışında bir `SQLCODE` (örn. `-904`) üretilir. | `CA-RETURN-CODE = 90` | `POLICY`'ye satır eklenmez; `ENDOWMENT` INSERT ve LGAPVS01 çağrılmaz (`RETURN`, ABEND yok). `CA-POLICY-NUM` değişmez.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır, 12-15 ile aynı iki mesaj; `EM-SQLRC` üretilen SQLCODE'dur (`-00904`), `CNUM` `0000000008`, `' PNUM='` + 10 boşluk. | `lgapdb01.cbl:300-303`, `:564-578` | İş kuralı |
| 12-18 | 12-01 ile aynı, ama [B4] `ENDOWMENT` INSERT'te `SQLCODE ≠ 0` üretilir (örn. `-803`). | Dönüş kodu yok. `CA-RETURN-CODE`'a `90` yazılır, ardından görev `ABEND` kodu `LGSQ` ile (NODUMP) sonlanır. Çağıranın `90`'ı görüp görmeyeceği **belirsiz**. | LGAPVS01 çağrılmaz, VSAM'a yazılmaz. Kod yorumuna göre ABEND, `POLICY` INSERT'inin geri alınmasını sağlar: yeni `POLICY` satırı kalmaz [kod yorumu] `lgapdb01.cbl:392`. `ENDOWMENT` satırı eklenmemiştir.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG`: 12-15'teki biçim, ama `' PNUM='` + **yeni poliçe numarası** (`0001000001` [B6]), `EM-SQLREQ` `' INSERT ENDOW '` + 2 boşluk. (2) `CA-ERROR-MSG`: COMMAREA'nın ilk 90 baytı; bayt 19-28 yeni poliçe numarasıdır. | `lgapdb01.cbl:308-313`, `:389-395`, `:333`, `:564-578`<br>[kod yorumu] `lgapdb01.cbl:392` | İş kuralı |
| 12-19 | 12-01 ile aynı, ama [B4] VSAM yazmada `RESP ≠ NORMAL` (örn. `DUPREC`: `KSDSPOLY`'de `E0000000008` + `0001000001` anahtarlı kayıt zaten var). | `CA-RETURN-CODE = 80` | `POLICY` ve `ENDOWMENT` satırları eklenmiştir ve **programca geri alınmaz** (VSAM hatasında ABEND ya da `ROLLBACK` yok: Kod gözlemleri 6). Db2 ile VSAM tutarsız kalır.<br>`CA-POLICY-NUM` ve `CA-LASTCHANGED` yeni değerlerle döner.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG` (101 bayt): tarih, saat, `' LGAPVS01'`, `' PNUM='` + `0001000001`, `' CNUM='` + `0000000008`, `' Write file KSDSPOLY'`, `' RESP='` + RESP, `' RESP2='` + RESP2. (2) `CA-ERROR-MSG`: COMMAREA'nın ilk 90 baytı (`EIBCALEN = 32500`, `ELSE` dalı). | `lgapvs01.cbl:135-147`, `:155-188`<br>`lgapdb01.cbl:243-250` | İş kuralı |
| 12-20 | 12-01 ile aynı, ama [B4] `POLICY` INSERT başarılı, sonraki `SELECT LASTCHANGED` `SQLCODE ≠ 0` döner. | `CA-RETURN-CODE = 00` | `SELECT`'in SQLCODE'u kontrol edilmez: program devam eder, `ENDOWMENT` INSERT ve VSAM yazma yapılır (12-01 gibi). `CA-LASTCHANGED`'ın ne kalacağı **belirsiz** (hata durumunda `SELECT`'in alanı ne yaptığı kodda yok). Hata mesajı yazılmaz. | `lgapdb01.cbl:316-322`, `:219-226` | Arayüz (COMMAREA) kuralı |
| 12-21 | 12-01 ile aynı, ama `CA-POLICY-NUM = 0000000004` (Db2'de var olan poliçe 4'ün numarası) ve `CA-LASTCHANGED` = poliçe 4'ün Db2'deki değeri (JCL'de `'2011-08-05 10:45:19'`, `db2cre.jcl:854`; dönen metnin biçimi **belirsiz**, bu yüzden test değeri önce `13-01` ile okuyup aynen verir). [B6] | `CA-RETURN-CODE = 00` | Girdideki iki alan yok sayılır: `CA-POLICY-NUM = 0001000001` ve `CA-LASTCHANGED` yeni satırın zaman damgası olur. Poliçe 4'ün `POLICY` ve `ENDOWMENT` satırları **değişmez**. | `lgapdb01.cbl:279`, `:284`, `:308-311`, `:316-321` | İş kuralı |
| 12-22 | 12-01 ile aynı, ama `CA-ISSUE-DATE = '2012-08-04'` ve `CA-EXPIRY-DATE = '2011-08-05'` (bitiş başlangıçtan önce), `CA-E-WITH-PROFITS = 'X'`, `CA-E-FUND-NAME` boşluk, `CA-E-TERM = 00`, `CA-E-SUM-ASSURED = 000000`. | `CA-RETURN-CODE = 00` | Hiçbir programda alan doğrulaması yok ve `POLICY`/`ENDOWMENT` tanımlarında kısıt yok. İki tabloya da girdideki değerler (tarihler ters, bayrak `X`, fon adı boşluk, vade ve tutar sıfır) olduğu gibi yazılır; VSAM kaydına da bayrak `X` ve boşluk fon adı gider. | `lgapdb01.cbl:268-288`, `:330-366`<br>`lgapvs01.cbl:110-115`, `:135-141`<br>`db2cre.jcl:161-176`, `:203-216` | İş kuralı |
| 12-23 | 12-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000008` (10 haneli). [B3] | [B3] `CA-RETURN-CODE = 00` (ön koşullu) [kod] `lgapdb01.cbl:292-293`. | [B3] `PIC 9(10)` → `PIC S9(9) COMP` taşımasında en soldaki hane kesilir ve `DB2-CUSTOMERNUM-INT = 8` olur: poliçe **müşteri 8** için eklenir [kod] `lgcmarea.cpy:12`, `lgapdb01.cbl:90`, `:176`. VSAM anahtarı ise 10 haneli `1000000008`'i kesilmeden kullanır [kod] `lgapvs01.cbl:101`: `KSDSPOLY`'de anahtar `E` + `1000000008` + `0001000001` olur; Db2'dekiyle tutarsız.<br>B3 sağlanmazsa sonuç **belirsiz**. | `lgcmarea.cpy:12`<br>`lgapdb01.cbl:90`, `:176`, `:280`<br>`lgapvs01.cbl:100-101`, `:135-141`<br>`cobol.jcl:44-50` | Arayüz (COMMAREA) kuralı |
| 12-24 | 12-01 ile aynı, ama `CA-CUSTOMER-NUM` sayısal değil (örn. boşluk). | **belirsiz** | **belirsiz**. İki programda da sayısallık kontrolü yok. Sayısal olmayan değer doğrudan `DB2-CUSTOMERNUM-INT`'e taşınır ve VSAM anahtarı olarak da kullanılır. | `lgapdb01.cbl:176`<br>`lgapvs01.cbl:101` | Arayüz (COMMAREA) kuralı |
| 12-25 | **LGAPVS01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (anahtardaki müşteri ve poliçe numaraları 9-28. baytlarda, içeride kalır). `CA-REQUEST-ID = '01AEND'`. [B4] Yazmada `RESP ≠ NORMAL`. | `CA-RETURN-CODE = 80` | Yan etki: `LGSTSQ`'ya iki LINK yapılır. (1) `ERROR-MSG`: 12-19'daki gibi. (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`; kalan `CA-DATA` boşluk. Bu dala yalnızca bu yolla ulaşılır.<br>Not: LGAPVS01'de `EIBCALEN` kontrolü yok. `EIBCALEN < 28` ise poliçe numarası kısmen COMMAREA dışında kalır; anahtar ve sonuç bu satırın kapsamı dışındadır. | `lgapvs01.cbl:97-101`, `:135-147`, `:173-179` | Arayüz (COMMAREA) kuralı |
| 12-26 | **LGAPVS01 doğrudan** çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01AXXX'` (4. karakter `X`; `C`, `E`, `H`, `M` dışında). `CA-CUSTOMER-NUM` ve `CA-POLICY-NUM` dolu. [B4] Yazma başarılı. | `CA-RETURN-CODE` **değişmez** (LGAPVS01 başarıda dönüş kodu yazmaz). | `KSDSPOLY`'ye 64 baytlık kayıt yazılır: anahtar `X` + müşteri no + poliçe no, veri kısmı 43 boşluk (`WHEN OTHER`). Hata mesajı yazılmaz. | `lgapvs01.cbl:99-101`, `:130-132`, `:135-141` | Arayüz (COMMAREA) kuralı |
| 12-27 | 12-01 ile aynı, ama `CA-E-LIFE-ASSURED` = `ABCDEFGHIJKLMNOPQRSTUVWXYZ12345` (31 karakter). | `CA-RETURN-CODE = 00` | `ENDOWMENT.lifeAssured` (`CHAR(31)`) 31 karakterin hepsini alır. VSAM'da yaşam sigortalısı alanı 30 baytlık: kayıtta `ABCDEFGHIJKLMNOPQRSTUVWXYZ1234` yazılır, **31. karakter (`5`) VSAM'a yazılmaz**. Db2 ile VSAM aynı değeri taşımaz. | `lgcmarea.cpy:53`<br>`lgapvs01.cbl:40`, `:115`<br>`lgapdb01.cbl:364`, `:385`<br>`db2cre.jcl:211` | İş kuralı |
| 12-28 | 12-01 ile aynı. LGAPDB01'e ya da LGAPVS01'e yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. İki LINK'te de `RESP`/`NOHANDLE` yok, programlarda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgapol01.cbl:121-124`<br>`lgapdb01.cbl:243-246` | Arayüz (COMMAREA) kuralı |

## 12-01 girdi ve beklenen değerler

Müşteri 8'e, örnek poliçe 4'ün ve hayat sigortası 4'ün değerleriyle yeni poliçe eklenir. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Girdi | Girdinin kaynağı (`db2cre.jcl`) | Dönüşte beklenen | Alanı kullanan COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` X(6) | `01AEND` | — (değer koddan: `lgapdb01.cbl:186`) | Değişmez | `lgapdb01.cbl:184`, `:186` |
| `CA-RETURN-CODE` 9(2) | Herhangi bir değer | — | `00` | `lgapol01.cbl:105`, `lgapdb01.cbl:172`, `:293` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000008` | `:675` | Değişmez | `lgapdb01.cbl:176`, `:179` |
| `CA-POLICY-NUM` 9(10) | Herhangi bir değer (örn. `0000000000`) | — (kullanılmaz) | [B6] `0001000001` | `lgapdb01.cbl:279`, `:308-311` |
| `CA-ISSUE-DATE` X(10) | `2011-08-05` | `:851` | Değişmez | `lgapdb01.cbl:281` |
| `CA-EXPIRY-DATE` X(10) | `2012-08-04` | `:852` | Değişmez | `lgapdb01.cbl:282` |
| `CA-LASTCHANGED` X(26) | Herhangi bir değer | — (kullanılmaz) | Yeni satırın zaman damgası (biçimi **belirsiz**) | `lgapdb01.cbl:284`, `:316-321` |
| `CA-BROKERID` 9(10) | `0000000000` | `:855` | Değişmez | `lgapdb01.cbl:264`, `:285` |
| `CA-BROKERSREF` X(10) | ␣×10 (JCL'de `''`) | `:856` | Değişmez | `lgapdb01.cbl:286` |
| `CA-PAYMENT` 9(6) | `000000` | `:857` | Değişmez | `lgapdb01.cbl:265`, `:287` |
| `CA-E-WITH-PROFITS` X | `Y` | `:1035` | Değişmez | `lgapdb01.cbl:358`, `lgapvs01.cbl:111` |
| `CA-E-EQUITIES` X | `Y` | `:1034` | Değişmez | `lgapdb01.cbl:359`, `lgapvs01.cbl:112` |
| `CA-E-MANAGED-FUND` X | `N` | `:1036` | Değişmez | `lgapdb01.cbl:360`, `lgapvs01.cbl:113` |
| `CA-E-FUND-NAME` X(10) | `LIONTMR` + ␣×3 | `:1037` (sütun `CHAR(10)`: `:208`) | Değişmez | `lgapdb01.cbl:361`, `lgapvs01.cbl:114` |
| `CA-E-TERM` 99 | `05` | `:1038` | Değişmez | `lgapdb01.cbl:330`, `:362` |
| `CA-E-SUM-ASSURED` 9(6) | `012500` | `:1039` | Değişmez | `lgapdb01.cbl:331`, `:363` |
| `CA-E-LIFE-ASSURED` X(31) | `J. MORRIS` + ␣×22 | `:1040` (sütun `CHAR(31)`: `:211`) | Değişmez | `lgapdb01.cbl:364`, `lgapvs01.cbl:115` |
| `CA-E-PADDING-DATA` X(32348) | Boşluk [B1] | — | Değişmez | `lgapdb01.cbl:344-345` |

## 12-01'den sonra Db2'de ve VSAM'da beklenen durum

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `POLICY` | Yeni satır: `policyNumber` = 1000001 [B6], `customerNumber` = 8, `issueDate` = 2011-08-05, `expiryDate` = 2012-08-04, `policyType` = `E`, `lastChanged` = INSERT anındaki zaman damgası, `brokerId` = 0, `brokersReference` = boşluk (`CHAR(10)`), `payment` = 0. `commission` için değer verilmez. Poliçe 4'ün satırı değişmez. | `lgapdb01.cbl:268-288`, `:188` |
| `ENDOWMENT` | Yeni satır: `policyNumber` = 1000001, `withProfits` = `Y`, `equities` = `Y`, `managedFund` = `N`, `fundName` = `LIONTMR`, `term` = 5, `sumAssured` = 12500, `lifeAssured` = `J. MORRIS` (`CHAR(31)`'e boşlukla tamamlanır), `paddingData`: 12-02 (uzunluk 2348). | `lgapdb01.cbl:346-366` |
| `KSDSPOLY` | Yeni 64 baytlık kayıt: `E` + `0000000008` + `0001000001` + `Y` + `Y` + `N` + `LIONTMR` + ␣×3 + `J. MORRIS` + ␣×21. Başlangıçtaki iki hayat sigortası kaydı (`ksdspoly.txt:3-4`) değişmez. | `lgapvs01.cbl:99-101`, `:110-115`, `:135-141` |
| `CUSTOMER` | Değişmez. Bu zincirde müşteri tablosuna giden SQL yok. | `lgapdb01.cbl:268-321`, `:346-386` |

## Kapsam tablosu

Koddaki her `IF`/`EVALUATE` dalının ve dönüş kodunun, onu kapsayan test numaraları.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| AO-01 | `lgapol01.cbl:98-102` `IF EIBCALEN = 0` → ABEND `LGCA` | 12-05 | |
| AO-02 | `lgapol01.cbl:105` `'00'` → `CA-RETURN-CODE` | 12-01, 12-06, 12-07 | 12-06/12-07: kontrolden önce yazılır |
| AO-03 | `lgapol01.cbl:109`, `:113-116` `IF EIBCALEN < 28` → `98` (doğru ve yanlış dal) | 12-06, 12-07 (doğru dal); 12-01, 12-08 (yanlış dal) | |
| AO-04 | `lgapol01.cbl:121-126` LINK LGAPDB01 `LENGTH(32500)`, RETURN | 12-01, 12-03, 12-08, 12-28 | |
| AO-05 | `lgapol01.cbl:137-169` `WRITE-ERROR-MESSAGE` | 12-05 | Dış `IF EIBCALEN > 0` yanlış. İç dallar (`lgapol01.cbl:155-167`) ölü kod (Kod gözlemleri 12) |
| AD-01 | `lgapdb01.cbl:165-169` `IF EIBCALEN = 0` → ABEND `LGCA` | 12-09 | Yalnızca doğrudan çağrı |
| AD-02 | `lgapdb01.cbl:172` `'00'` | 12-01, 12-10 | |
| AD-03 | `lgapdb01.cbl:176`, `:179` `CA-CUSTOMER-NUM` → `DB2-CUSTOMERNUM-INT`, `EM-CUSNUM` | 12-01, 12-15, 12-23, 12-24 | |
| AD-04 | `lgapdb01.cbl:184-207` `EVALUATE CA-REQUEST-ID` ilk | 12-01 (`WHEN '01AEND'`), 12-03, 12-04 (`WHEN OTHER` → `99`) | Hayat sigortası dışı `WHEN` kolları kapsam dışı |
| AD-05 | `lgapdb01.cbl:210-213` `IF EIBCALEN < WS-REQUIRED-CA-LEN` → `98` | 12-10 (doğru dal), 12-11 (sınır), 12-01 (yanlış dal) | Yalnızca doğrudan çağrı |
| AD-06 | `lgapdb01.cbl:219`, `:261-322` `INSERT-POLICY` | 12-01, 12-15, 12-17, 12-20, 12-21 | |
| AD-07 | `lgapdb01.cbl:292-293` `When 0` → `00` | 12-01 | |
| AD-08 | `lgapdb01.cbl:295-298` `When -530` → `70` | 12-15 | 12-16: düzenek olmadan belirsiz |
| AD-09 | `lgapdb01.cbl:300-303` `When Other` → `90` | 12-17 | |
| AD-10 | `lgapdb01.cbl:308-321` `IDENTITY_VAL_LOCAL`, `SELECT LASTCHANGED` (SQLCODE kontrolsüz) | 12-01, 12-20, 12-21 | |
| AD-11 | `lgapdb01.cbl:223-241` `EVALUATE CA-REQUEST-ID` ikinci | 12-01 (`WHEN '01AEND'`) | `WHEN OTHER` (`:237-240`) ölü kod (Kod gözlemleri 5) |
| AD-12 | `lgapdb01.cbl:339-342` `WS-VARY-LEN`, `IF WS-VARY-LEN > 0` | 12-02, 12-12, 12-13 (doğru dal); 12-11 (yanlış dal) | 12-14: belirsiz |
| AD-13 | `lgapdb01.cbl:346-366` `PADDINGDATA`'lı INSERT | 12-01, 12-02, 12-12, 12-13 | |
| AD-14 | `lgapdb01.cbl:367-386` `PADDINGDATA`'sız INSERT | 12-11 | Yalnızca doğrudan çağrı (Kod gözlemleri 3) |
| AD-15 | `lgapdb01.cbl:389-395` `IF SQLCODE NOT EQUAL 0` → `90`, ABEND `LGSQ` | 12-18 (doğru dal), 12-01 (yanlış dal) | |
| AD-16 | `lgapdb01.cbl:243-250` LINK LGAPVS01 `LENGTH(32500)`, RETURN | 12-01, 12-19, 12-28 | |
| AD-17 | `lgapdb01.cbl:562-595` `WRITE-ERROR-MESSAGE` | 12-09 (dış `IF` yanlış), 12-15, 12-17, 12-18 (`ELSE`) | `< 91` dalı (`:581-586`) ölü kod (Kod gözlemleri 12) |
| AV-01 | `lgapvs01.cbl:97-101` anahtar kurma | 12-01, 12-23, 12-24, 12-26 | |
| AV-02 | `lgapvs01.cbl:103-132` `EVALUATE WF-Request-ID` | 12-01 (`WHEN 'E'`), 12-26 (`WHEN OTHER`) | `C`, `H`, `M` kolları kapsam dışı |
| AV-03 | `lgapvs01.cbl:135-141` `WRITE FILE` | 12-01, 12-26 | |
| AV-04 | `lgapvs01.cbl:142-147` `RESP ≠ NORMAL` → `80` (doğru ve yanlış dal) | 12-19, 12-25 (doğru dal); 12-01 (yanlış dal) | |
| AV-05 | `lgapvs01.cbl:155-188` `WRITE-ERROR-MESSAGE` | 12-19 (`ELSE`), 12-25 (`< 91`) | `EIBCALEN = 0` dalı: LGAPVS01'de `EIBCALEN` kontrolü yok (Kod gözlemleri 12) |
| SQ-01 | `lgstsq.cbl:68-71` LINK yolu | 12-05, 12-09, 12-15, 12-17, 12-18, 12-19, 12-25 | |
| SQ-02 | `lgstsq.cbl:72-80` terminalden RECEIVE | — | **Kapsam dışı: bu zincirden ulaşılamaz.** LGSTSQ yalnızca LINK ile çağrılır. |
| SQ-03 | `lgstsq.cbl:94-111` WRITEQ TD `CSMT`, TS `GENAERRS` | 12-15 … 12-19 | |
| N-01 | Girdideki `CA-POLICY-NUM`, `CA-LASTCHANGED` yok sayılır | 12-21 | |
| N-02 | Alan doğrulaması yok | 12-22, 12-24 | Kod gözlemleri 8 |
| N-03 | VSAM hatasında Db2 satırları geri alınmaz | 12-19 | Kod gözlemleri 6 |
| N-04 | `CA-CUSTOMER-NUM` kesilmesi (B3) | 12-23 | Kod gözlemleri 7 |
| N-05 | `LIFE-ASSURED` VSAM'da 30 bayta kesilir | 12-27 | Kod gözlemleri 9 |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları").

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 12-01 | B3, B4, B6 | Başarı yolu; `CA-POLICY-NUM = 0001000001`; `WS-VARY-LEN` = 2348 | B6 yoksa numara belirsiz; B3 yoksa `WS-VARY-LEN` 3900'ü aşar, sonuç belirsiz (12-14) |
| 12-02 | B1, B3 | `paddingData` uzunluğu 2348, içeriği boşluk | Uzunluk ve içerik belirsiz |
| 12-07 | B1 | Tamponun 7-8. baytları `98` | Dönüş kodu belirsiz |
| 12-08 | B1 | Alanlar boşluk olarak okunur | Okunan değerler belirsiz |
| 12-09 | B2 | `EM-SQLRC = +00000` | `EM-SQLRC` belirsiz |
| 12-11 | B4, B6 | Başarı yolu; `paddingData` NULL | Girdi kurulamaz |
| 12-12 | B4, B6 | Başarı yolu; `paddingData` = `ABCDEFGHIJ` | Girdi kurulamaz |
| 12-13 | B4, B6 | Başarı yolu; `paddingData` uzunluğu 3900 | Girdi kurulamaz |
| 12-15 | B4 | Girdi düzenekle kurulur; tahmin (`70`) değişmez | Girdi kurulamaz: bkz. 12-16 |
| 12-17 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | Girdi kurulamaz |
| 12-18 | B4, B6 | Girdi düzenekle kurulur; tahmin (`90` + ABEND `LGSQ`) değişmez; mesajdaki poliçe no `0001000001` | Girdi kurulamaz; B6 yoksa mesajdaki numara belirsiz |
| 12-19 | B4, B6 | Girdi düzenekle kurulur; tahmin (`80`) değişmez; mesajdaki poliçe no `0001000001` | Girdi kurulamaz; B6 yoksa mesajdaki numara belirsiz |
| 12-20 | B4 | Girdi düzenekle kurulur; tahmin (`00`) değişmez | Girdi kurulamaz |
| 12-21 | B6 | Yeni numara `0001000001` | Yeni numara belirsiz |
| 12-23 | B3 | Poliçe müşteri 8 için eklenir, VSAM anahtarı `1000000008` | Dönüş kodu ve veri belirsiz |
| 12-25 | B4 | Girdi düzenekle kurulur; tahmin (`80`, `< 91` dalı) değişmez | Girdi kurulamaz |
| 12-26 | B4 | Girdi düzenekle kurulur | Girdi kurulamaz |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 12-01 | Beklenen veri (yalnızca `CA-LASTCHANGED` biçimi) | `lastChanged` sütunu `TIMESTAMP` türünde (`db2cre.jcl:168`) ve doğrudan `PIC X(26)` alana okunuyor (`lgapdb01.cbl:316-321`). Okunurken hangi biçimde geleceği Db2 ayarına bağlı ve kodda yazmıyor. |
| 12-08 | Dönüş kodu, beklenen veri | 29-152. baytlar COMMAREA'nın dışından okunuyor; INSERT'in kabul edilip edilmeyeceği Db2'ye ve ortama bağlı (`DATE` sütununa boşluk). |
| 12-14 | Dönüş kodu, beklenen veri | `WS-VARY-LEN` > 3900: taşıma `WS-VARY-CHAR` dışına yazar; kodda denetim yok. |
| 12-16 | Dönüş kodu, beklenen veri | Var olmayan müşteriye `POLICY` INSERT'inin hangi SQLCODE'u döndüreceği kodda yok. Kod yalnızca `-530`'u tanıyor. |
| 12-18 | Dönüş kodu (çağıranın `90`'ı görmesi) | `90` yazıldıktan hemen sonra `ABEND LGSQ` geliyor (`lgapdb01.cbl:389-393`) ve zincirde abend yakalama yok. |
| 12-20 | Beklenen veri (`CA-LASTCHANGED`) | Başarısız `SELECT ... INTO`'nun host değişkenine ne yazdığı kodda görünmüyor. |
| 12-24 | Dönüş kodu, beklenen veri | Sayısal olmayan veri sayısal alana taşınıyor ve hiçbir doğrulama yok. Sonuç tanımsız. |
| 12-28 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil. |
