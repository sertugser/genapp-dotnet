# 08 — Konut poliçesi ekle (Add House Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Konut poliçesi ekle: yeni poliçe Db2 `POLICY` ve `HOUSE` tablolarına, konut kaydının bir bölümü de VSAM `KSDSPOLY` dosyasına yazılır. Poliçe türü COMMAREA'daki istek kodundan (`01AHOU`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGAPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGAPDB01` (Db2 `POLICY` + `HOUSE` INSERT) → `LINK` → `LGAPVS01` (VSAM `KSDSPOLY` WRITE). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. Programlar dört poliçe türüne de hizmet eder; yalnızca konut (`01AHOU`) kolları ve ortak kısımlar tahmin edildi, motor/endowment/ticari kolları "başka tür" diye işaretlendi. |
| Okunan COBOL dosyaları | `base/src/lgapol01.cbl` (1-169), `base/src/lgapdb01.cbl` (1-595), `base/src/lgapvs01.cbl` (1-188), `base/src/lgstsq.cbl` (1-126), `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy`. Karşılaştırma için yalnızca `base/src/lgipdb01.cbl:275` (08-11). |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` ve `HOUSE` tanımları (`:161-185`, `:240-256`), müşteri numaraları 1-10 (`:437` … `:743`), müşteri 4 (`:527-548`), poliçe 6 (`:861-882`) ve `HOUSE` 6 (`:1042-1057`). `base/cntl/adef121.jcl` (`:40-52`, `:65-73`) ve `base/data/ksdspoly.txt` (`:1-10`): `KSDSPOLY` tanımı ve başlangıç verisi. `base/cntl/cobol.jcl` (`:44-50`): derleme seçenekleri (B3). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `base/Architecture.md` (`:42-43`, `:48-50`, `:88-97`, `:109-110`), `base/Reference.md` (`:119-121`): yalnızca Kod gözlemleri 9 ve 15 ile 08-05, 08-08, 08-09'da. `docs/adr/0004-legacy-test-harness.md` (`:44-52`, `:56-60`, `:96-114`, `:135-138`): yalnızca B4, B8, C grubu, [CICS bilgisi], Kod gözlemleri 14 ve 17 ile Açık sorular için. **Kodla doğrulanmadı.** |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-14 |
| Kaynak commit (`git rev-parse HEAD`) | `4a1893e06b85c6ffd2c015c90343d54d62b94b58` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge, kodla doğrulanmadı]**: `base/` ya da `docs/` altındaki açıklama metinleri (`.md`). Belgeye dayanan bir ifade kodda doğrulanmamıştır;
  hiçbir kesin tahmin yalnızca belgeye dayanmaz.
- **[Db2 bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen Db2 davranışı (`11-house-delete.md` ile aynı etiket).
  Bu dosyada tek kullanımı: yabancı anahtar ihlali olan bir INSERT `SQLCODE = -530` döner (08-05).
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

Ön koşul kodları test düzeneğinin vereceği kararlardır (kodlar `01-customer-add.md` ve `02-customer-inquire.md` ile aynıdır,
B6 `POLICY` kimlik değeri için uyarlanmıştır). Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul sağlanmazsa
tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması) kendisi kurar. **Aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir; satırda yalnızca düzeneğin değiştirdiği değer yazılır. **Bu dosyada ayrıca:** düzenek host değişkenlerine yalnızca satırda yazılan değeri koyar; başarılı `SET ... IDENTITY_VAL_LOCAL()`'in (`lgapdb01.cbl:308-311`) host değişkenine yeni poliçe numarası (değeri: B6), başarılı `SELECT LASTCHANGED`'in (`lgapdb01.cbl:316-321`) host değişkenine INSERT anındaki zaman damgası (`09-house-inquire.md` ve `10-house-update.md`'deki ekle aynı türden). Bu ek, yeni `CA-POLICY-NUM` ya da `CA-LASTCHANGED` döndüren bütün satırlar için geçerlidir (08-01, 08-02, 08-03, 08-04, 08-08, 08-09, 08-14, 08-18, 08-23, 08-29). [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:56`, `:135-138`. |
| B6 | Db2'de `POLICY` tablosuna `DEFAULT` ile daha önce hiç satır eklenmemiştir; ilk kimlik değeri `START WITH` değeridir (1000001: [kod] `db2cre.jcl:162-163`). Örnek poliçeler 1-10 numarasını açıkça verir (`db2cre.jcl:777`, `:801`, `:825`, `:849`, `:873`, `:897`, `:921`, `:945`, `:969`, `:993`), kimlik sayacını ilerletmez. |
| B8 | Düzenekte `EXEC CICS ABEND` bir mesaj ve `GOBACK` ile değiştirilir: abend eden program çağırana döner ve COMMAREA'ya en son yazılan dönüş kodu görünür. [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:44-46`. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır; orada çağıranın kodu görmesi belirsizdir (C). Bu dosyada: 08-08 (`LGSQ`), 08-15 ve 08-21 (`LGCA`; COMMAREA olmadığı için dönüş kodu görünmez, düzenekte görülen ABEND mesajı ve `GOBACK`'tir). |

(B5 ve B7 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası, konut görünümü** (`lgcmarea.cpy:10-13`, `:34-44`, `:56-63`). Üç program da bu düzeni kullanır
   (`lgapol01.cbl:70-71`, `lgapdb01.cbl:134-137`, `lgapvs01.cbl:86-87`). Konut isteği için alanlar:

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
   | 101-115 | `CA-H-PROPERTY-TYPE` | X(15) | `:57` |
   | 116-118 | `CA-H-BEDROOMS` | 9(3) | `:58` |
   | 119-126 | `CA-H-VALUE` | 9(8) | `:59` |
   | 127-146 | `CA-H-HOUSE-NAME` | X(20) | `:60` |
   | 147-150 | `CA-H-HOUSE-NUMBER` | X(4) | `:61` |
   | 151-158 | `CA-H-POSTCODE` | X(8) | `:62` |
   | 159-32500 | `CA-H-FILLER` | X(32342) | `:63` |

2. **Uzunluk kontrolleri ve zincirdeki boşluk.**
   - LGAPOL01: gerekli uzunluk yalnızca `WS-CA-HEADER-LEN` = 28 (`lgapol01.cbl:59`, `:109`), yani istek kodu, dönüş kodu,
     müşteri ve poliçe numarası (1-28. baytlar). Kontrol: `lgapol01.cbl:113-116`. Konut uzunluğu bu programda eklenmez.
   - LGAPDB01: 28 (`lgapdb01.cbl:65`, `:182`) + `WS-FULL-HOUSE-LEN` 130 (`lgpolicy.cpy:25`; `lgapdb01.cbl:191`) = **158**,
     konut alanlarının sonu (`CA-H-POSTCODE` 151-158). Kontrol: `lgapdb01.cbl:210-213`.
   - LGAPVS01: uzunluk kontrolü yok; `EIBCALEN` yalnızca `WS-Commarea-Len`'e kopyalanır (`lgapvs01.cbl:97`) ve bu alan başka
     hiçbir satırda kullanılmaz (`lgapvs01.cbl:23`).
   - LGAPOL01, LGAPDB01'i `LENGTH(32500)` ile çağırır (`lgapol01.cbl:121-124`). [CICS bilgisi] LGAPDB01 her zaman `EIBCALEN = 32500` görür;
     bu yüzden LGAPDB01'in `EIBCALEN = 0` kontrolüne (`lgapdb01.cbl:165-169`) ve 158'lik kontrolüne (`:210-213`) zincirden
     ulaşılamaz. LGAPDB01 de LGAPVS01'i `LENGTH(32500)` ile çağırır (`lgapdb01.cbl:243-246`).
   - **Sonuç ([CICS bilgisi]): zincirde 28-157 baytlık COMMAREA reddedilmez** (08-18, 08-19). LGAPDB01 doğrudan çağrılsa
     reddederdi (08-22).
   - LGAPDB01 `CA-CUSTOMER-NUM`'u (9-18. baytlar) uzunluk kontrolünden (`lgapdb01.cbl:210`) önce okur: `:176` `S9(9) COMP`'a,
     `:179` `EM-CUSNUM`'a taşır. Doğrudan çağrıda `EIBCALEN < 18` iken alan kısmen ya da tamamen gelen COMMAREA'nın dışındadır
     (08-30); 08-22 ve 08-24 bu yüzden `EIBCALEN ≥ 18` ile yazıldı.
3. **Çağrılan programın dönüş kodu kontrol edilmez; LINK'lerde `RESP`/`NOHANDLE` yok.** LGAPOL01 LINK'ten sonra doğrudan
   RETURN yapar (`lgapol01.cbl:121-126`). LGAPDB01, LGAPVS01'in sonucuna bakmadan RETURN yapar (`lgapdb01.cbl:243-250`).
   LGAPDB01'in ya da LGAPVS01'in COMMAREA'ya yazdığı kod (`70`, `90`, `80`, `99`) olduğu gibi çağırana ulaşır.
4. **İstek kodu.**
   - LGAPOL01 `CA-REQUEST-ID`'yi hiçbir satırda okumaz (grep sonucu boş); her değeri LGAPDB01'e geçirir.
   - LGAPDB01 iki kez `EVALUATE CA-REQUEST-ID` yapar (`lgapdb01.cbl:184`, `:223`). Karşılaştırma büyük/küçük harfe duyarlıdır;
     LGIPDB01'deki `UPPER-CASE` dönüşümü (`lgipdb01.cbl:275`) eklemede yok (08-11).
   - İlk `EVALUATE`'in `WHEN OTHER`'ı `99` yazıp RETURN yapar (`lgapdb01.cbl:202-205`). Bu, uzunluk kontrolünden (`:210`)
     **önce** gelir: kısa COMMAREA ve bilinmeyen kodla `98` değil `99` döner (08-24).
   - İkinci `EVALUATE`'in `WHEN OTHER`'ına (`lgapdb01.cbl:237-239`) **hiçbir girdiyle ulaşılamaz**: `CA-REQUEST-ID` `:184` ile
     `:223` arasında değiştirilmez ve bilinmeyen kod `:205`'te zaten RETURN yapmıştır.
   - LGAPVS01 yalnızca 4. karaktere bakar: `CA-Request-ID(4:1)` (`lgapvs01.cbl:99`, `:103`). `01AHOU` → `'H'`.
5. **`policyType` eklemede yalnızca yazılır, hiç okunmaz.** LGAPDB01 `'H'`'yi istek kodundan türetir (`lgapdb01.cbl:192`, alan
   `lgpolicy.cpy:43`) ve INSERT'e koyar (`lgapdb01.cbl:274`, `:283`). Üç programda `POLICYTYPE`'ı okuyan başka satır yok (grep:
   yalnızca `:188`, `:192`, `:196`, `:200`, `:274`, `:283`). Bu yüzden poliçe 7'nin tutarsızlığı (Kod gözlemleri 15) eklemeyi
   etkilemez.
6. **Girdi alanlarının kullanımı.**
   - Alan doğrulaması yok: tarih, broker, ödeme, tür, oda, değer, ev adı/numarası, posta kodu hiçbir programda kontrol edilmez,
     doğrudan INSERT'e gider (`lgapdb01.cbl:264-288`, `:405-425`).
   - Girdideki `CA-POLICY-NUM` kullanılmaz: numara `DEFAULT` ile Db2'den gelir (`lgapdb01.cbl:279`) ve `IDENTITY_VAL_LOCAL()` ile
     alana yazılır (`:308-311`). Girdideki `CA-LASTCHANGED` da kullanılmaz: zaman damgası `CURRENT TIMESTAMP` (`:284`), alan
     `:316-321`'de ezilir (08-03).
   - Sayısal dönüşümler: `CA-CUSTOMER-NUM` 9(10) → `S9(9) COMP` (`:176`, `:90`), `CA-BROKERID` 9(10) → `S9(9) COMP` (`:264`, `:91`),
     `CA-PAYMENT` 9(6) → `S9(9) COMP` (`:265`, `:92`), `CA-H-VALUE` 9(8) → `S9(9) COMP` (`:405`, `:97`), `CA-H-BEDROOMS` 9(3) →
     `S9(4) COMP` (`:406`, `:96`). 10 haneli alanlar B3 ile kesilir (08-14); diğerleri sığar. Sayısal olmayan içerikte sonuç
     derleyiciye bağlıdır (08-13).
   - `COMMISSION` sütunu INSERT listesinde yok (`lgapdb01.cbl:269-278`); sütun `SMALLINT`, `NOT NULL` ya da `DEFAULT` yok
     (`db2cre.jcl:172`). Değeri Db2'ye kalır.
   - `CA-H-HOUSE-NUMBER` yalnızca Db2'ye gider (`lgapdb01.cbl:423`); VSAM kaydına yazılmaz (Kod gözlemleri 10).
7. **`INSERT-POLICY`'nin gerçek akışı** (`lgapdb01.cbl:261-322`).
   - `:290-305` `Evaluate SQLCODE`: `When 0` → `'00'` (`:292-293`) ve akış `:308`'den devam eder. `When -530` → `'70'`,
     `WRITE-ERROR-MESSAGE`, `EXEC CICS RETURN` (`:295-298`). `When Other` → `'90'`, aynı iki adım (`:300-303`).
   - Hata kollarındaki RETURN paragraftan değil programdan çıkar: kontrol LGAPOL01'de LINK'ten sonraya (`lgapol01.cbl:124`),
     oradan RETURN'e (`:126`) gider. `HOUSE` INSERT'i ve LGAPVS01 çalışmaz.
   - Bu noktada `EM-POLNUM` henüz yazılmamıştır (tek yazma `lgapdb01.cbl:313`); `VALUE SPACES` ile boşluktur (`lgapdb01.cbl:50`).
   - [Db2 bilgisi] `-530` yabancı anahtar ihlalinin kodudur; bu koddan çıkmaz, 08-05 buna dayanır. Kodda görülen: `POLICY.customerNumber`
     `CUSTOMER`'a yabancı anahtar (`db2cre.jcl:174-176`). [belge, kodla doğrulanmadı] `base/Architecture.md:109-110`: "you cannot
     create a policy if the customer record does not exist".
   - `lgapdb01.cbl:308-310` `SET :DB2-POLICYNUM-INT = IDENTITY_VAL_LOCAL()` ve `:316-321` `SELECT LASTCHANGED`: ikisinden sonra da SQLCODE
     kontrol edilmez.
   - **`IDENTITY_VAL_LOCAL` uç durumu:** SET sessizce başarısız olursa numara başlangıçtaki 0 değerinde kalır
     (`lgapdb01.cbl:117`, `:159`). `CA-POLICY-NUM = 0` olur; ardından `HOUSE` INSERT'i 0 numarasıyla yapılır ve `POLICY`'de 0 yoktur
     (yabancı anahtar `db2cre.jcl:249-250`). SET'in başarısız olup olmayacağı kodda yok: **belirsiz**. Düzenekte
     numarayı host değişkenine B4'teki ek koyar.
8. **`INSERT-HOUSE` hatasının gerçek akışı** (`lgapdb01.cbl:427-433`).
   - `:428` `'90'` COMMAREA'ya yazılır. `:429` iki LGSTSQ LINK'i yapılır; bu noktada `EM-POLNUM` yeni numarayı taşır (`:313`) ve
     COMMAREA'da yeni numara ile yeni `CA-LASTCHANGED` vardır (`:311`, `:316-321`).
   - `:431` `EXEC CICS ABEND ABCODE('LGSQ') NODUMP`. `:432`'deki RETURN ABEND'den sonra geldiği için **ulaşılamaz**.
     LGAPVS01'e yapılan LINK (`:243-246`) de çalışmaz.
   - Çağıranın `90`'ı görmesi: düzenekte B8 ile ABEND bir `GOBACK`'tir, LGAPDB01 LGAPOL01'e döner ve `lgapol01.cbl:126` RETURN
     yapar: `90` görünür (08-08). [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır; çağıranın `90`'ı görmesi C.
   - `lgapdb01.cbl:430`'daki "Issue Abend to cause backout of update to Policy table" **yalnızca yorumdur**; kodda `SYNCPOINT ROLLBACK` yok
     (Kod gözlemleri 9). `POLICY` satırının geri alınması CICS/Db2 iş birimi davranışına bağlıdır: **belirsiz** (08-08).
9. **Hiçbir programda `SYNCPOINT`, `ROLLBACK`, `HANDLE` ya da `COMMIT` yok.** Dört programın PROCEDURE DIVISION'ı tarandı
   (büyük/küçük harf duyarsız grep, sonuç boş):
   - `lgapol01.cbl:77-169`: CICS komutları ABEND (`:101`), RETURN (`:115`, `:126`), LINK (`:121-124`, `:149-152`, `:157-160`,
     `:163-166`), ASKTIME/FORMATTIME (`:140-145`).
   - `lgapdb01.cbl:143-595`: ABEND (`:168`, `:431`; başka tür: `:393`, `:477`, `:551`), RETURN (`:205`, `:212`, `:250`, `:298`,
     `:303`, `:432`), LINK (`:243-246`, `:575-578`, `:583-586`, `:589-592`); SQL: INSERT (`:268-288`, `:409-425`), SET (`:308-310`),
     SELECT (`:316-321`).
   - `lgapvs01.cbl:91-188`: WRITE (`:135-141`), RETURN (`:146`), LINK (`:169-172`, `:176-179`, `:182-185`). **ABEND de yok.**
   - `lgstsq.cbl:52-126`: ASSIGN (`:60-66`), RECEIVE (`:73-76`), WRITEQ TD (`:94-99`), WRITEQ TS (`:105-111`), SEND TEXT
     (`:114-119`), RETURN (`:121-122`).
   - Sonuç: ABEND `LGSQ` (08-08) ya da `80` (08-09) sonrasında Db2 satırlarına ne olacağı kodda yazmıyor.
     [belge, kodla doğrulanmadı] `base/Architecture.md:48-50`: müşteri ve poliçe verisi Db2'ye iki aşamalı onayla yazılır,
     "if successful, it updates VSAM"; VSAM yazması başarısız olursa ne olacağını söylemiyor.
   - [kod] `KSDSPOLY` kurtarılabilir tanımlı: `LOG(UNDO)` (`adef121.jcl:45`). Bu operasyonda etkisi yok: 08-08'de VSAM'a hiç
     yazılmaz, 08-09'da yazma başarısızdır.
10. **LGAPVS01'in VSAM kaydı** (`lgapvs01.cbl:25-46`, `:99-141`).
    - Anahtar 21 bayt: `WF-Request-ID` (`CA-Request-ID(4:1)`, konutta `'H'`) + müşteri (10) + poliçe (10) (`:26-29`, `:99-101`).
      `KSDSPOLY` tanımıyla aynı: `KEYS(21 0)`, `RECORDSIZE(64 64)` (`adef121.jcl:49-50`). [belge, kodla doğrulanmadı]
      `base/Architecture.md:88-97` aynı anahtar biçimini anlatır.
    - Konut verisi 43 bayt (`lgapvs01.cbl:41-46`): tür X(15), oda `999`, değer 9(8), posta kodu X(8), ev adı **X(9)**. `CA-H-HOUSE-NAME`
      X(20)'nin yalnızca ilk 9 baytı kayda girer (`:122`). Ev numarası kayda hiç yazılmaz (`:117-122`).
    - Yazma `FROM(WF-Policy-Info) LENGTH(64) RIDFLD(WF-Policy-Key) KEYLENGTH(21) RESP(WS-RESP)` (`lgapvs01.cbl:135-141`).
    - `RESP ≠ NORMAL` → `EIBRESP2` → `WS-RESP2`, `'80'`, `WRITE-ERROR-MESSAGE`, RETURN (`:142-147`). **ABEND yok** (müşteri
      eklemede LGACVS01 aynı durumda ABEND eder: `01-customer-add.md` Kod gözlemleri 10).
    - LGAPVS01 başarı yolunda `CA-RETURN-CODE`'a yazmaz; tek yazma `lgapvs01.cbl:144`'teki `'80'`. Zincirde değer LGAPDB01'in `'00'`'ı
      olarak kalır (`lgapdb01.cbl:293`); doğrudan çağrıda girdideki gibi kalır (08-26).
11. **Dönüş kodları ve anlamları.**

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgapol01.cbl:105`, `lgapdb01.cbl:172`, `:293` | Başlangıç / başarı | Evet |
    | `98` | `lgapol01.cbl:114` | COMMAREA 28 bayttan kısa | Evet |
    | `98` | `lgapdb01.cbl:211` | COMMAREA 158 bayttan kısa | Yalnızca LGAPDB01 doğrudan çağrılırsa |
    | `99` | `lgapdb01.cbl:204` | Bilinmeyen istek kodu | Evet |
    | `99` | `lgapdb01.cbl:239` | Bilinmeyen istek kodu | Hayır: ulaşılamaz (Kod gözlemleri 4) |
    | `70` | `lgapdb01.cbl:296` | `POLICY` INSERT `SQLCODE = -530` | Evet |
    | `90` | `lgapdb01.cbl:301` | `POLICY` INSERT başka SQLCODE | Evet |
    | `90` | `lgapdb01.cbl:428` | `HOUSE` INSERT hatası | Alana yazılır, hemen ardından `ABEND LGSQ` |
    | `80` | `lgapvs01.cbl:144` | VSAM WRITE hatası | Evet (ABEND yok) |
    | ABEND `LGCA` | `lgapol01.cbl:101`, `lgapdb01.cbl:168` | COMMAREA yok | — |
    | ABEND `LGSQ` | `lgapdb01.cbl:431` | `HOUSE` INSERT hatası | — |
    | `90` + ABEND `LGSQ` | `lgapdb01.cbl:390-393`, `:474-477`, `:548-551` | Başka tür INSERT hataları | Başka tür: tahmin yazılmadı |

12. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGAPOL01 **45** bayt (`lgapol01.cbl:41-46`), LGAPDB01 **87** (`lgapdb01.cbl:41-53`),
      LGAPVS01 **101** (`lgapvs01.cbl:58-73`). İkinci mesaj `CA-ERROR-MSG` üçünde de 99 bayttır: `'COMMAREA='` + 90 bayt
      (`lgapol01.cbl:48-50`, `lgapdb01.cbl:55-57`, `lgapvs01.cbl:75-77`).
    - LGAPDB01 `EM-VARIABLE` (63 bayt): `' CNUM='` + `EM-CUSNUM` (`lgapdb01.cbl:179`'da yazılır), `' PNUM='` + `EM-POLNUM` (yalnızca `:313`'te
      yazılır), `EM-SQLREQ` (`' INSERT POLICY'` `:267`, `' INSERT HOUSE '` `:408`; ikisi de 14 karakter, 16 bayta 2 boşlukla
      tamamlanır), `' SQLCODE='` + `EM-SQLRC` (`+9(5)`, `:53`; `:564`'te yazılır).
    - LGAPVS01 `EM-VARIABLE` (77 bayt): `' PNUM='` + `CA-Policy-Num`, `' CNUM='` + `CA-Customer-Num` (`lgapvs01.cbl:165-166`),
      `' Write file KSDSPOLY'`, `' RESP='` + `WS-RESP`, `' RESP2='` + `WS-RESP2` (`:167-168`).
    - `EIBCALEN = 0` mesajları: LGAPOL01'de 21 karakterlik `' NO COMMAREA RECEIVED'` 21 baytlık `EM-VARIABLE`'a tam sığar
      (`lgapol01.cbl:46`, `:99`). LGAPDB01'de aynı metin 63 baytlık gruba taşınır (`lgapdb01.cbl:166`); grup 42 boşlukla tamamlanır,
      sonra `MOVE SQLCODE TO EM-SQLRC` grubun son 6 baytını ezer (`:564`): metin + 36 boşluk + 6 bayt `EM-SQLRC`.
    - `WRITE-ERROR-MESSAGE` içindeki `IF EIBCALEN > 0` / `< 91` / `ELSE` dalları COMMAREA'nın ne kadarının gönderileceğini seçer
      (`lgapol01.cbl:154-168`, `lgapdb01.cbl:580-594`, `lgapvs01.cbl:173-187`):
      - LGAPOL01'de yordam yalnızca `EIBCALEN = 0` dalından çağrılır (`lgapol01.cbl:100`); dış `IF` her zaman yanlış, `:155-167`
        **ölü kod**.
      - LGAPDB01'de yordam `lgapdb01.cbl:167` (`EIBCALEN = 0`), `:297`, `:302`, `:429` (158'lik kontrol geçildikten sonra) satırlarından
        çağrılır. `< 91` dalı (`:581-586`) **hiçbir girdiyle ulaşılamaz**; zincirde hep `ELSE` alınır.
      - [CICS bilgisi] LGAPVS01'de `ELSE` zincirde, `< 91` yalnızca doğrudan çağrıda (08-27) alınır.
13. **LGSTSQ kuyruk kaydı** (`lgstsq.cbl`).
    - LINK ile çağrıldığında mesaj `COMMA-DATA`'dan alınır ve uzunluk `EIBCALEN` olur (`:68-71`). `INVOKINGPROG`'un LINK'te dolu
      gelmesi CICS'in tanımıdır, kodda yazmıyor.
    - Mesajlar tarih ya da `'COMMAREA='` ile başlar; `'Q='` koşulu (`:83`) tutmaz, kuyruk adı `GENAERRS` kalır (`:82`).
    - Kayıt = `SYSID` (4) + boşluk + mesaj; uzunluk = gelen uzunluk + 5 (`:25-28`, `:71`, `:90`). Aynı kayıt TD `CSMT`'ye ve
      TS `GENAERRS`'e yazılır (`:94-111`); iki yazmanın da `RESP`'i alınır ama kontrol edilmez.
    - `COMMA-DATA` 90 bayt (`:48`), `WRITE-MSG` 95 bayt (`:25-28`).
      - 45 ve 87 baytlık `ERROR-MSG`'ler kayda eksiksiz girer (kayıt 50 ve 92 bayt).
      - 99 baytlık `CA-ERROR-MSG`: yalnızca ilk 90 bayt alınır, yani `'COMMAREA='` + COMMAREA'nın ilk **81** baytı (`:70`).
        Kayıt 104 bayt; son 9 baytı `WRITE-MSG`'nin dışından okunur.
      - **LGAPVS01'in 101 baytlık `ERROR-MSG`'i:** yalnızca ilk 90 bayt alınır. Mesajın 89-95. baytları `' RESP2='`, 96-101.
        baytları RESP2 değeridir; kayda `' RESP='` + RESP + `' R'` girer, **RESP2 değeri kayda hiç girmez**. Kayıt 106 bayt; son 11
        baytı `WRITE-MSG`'nin dışından okunur.
      - `WRITE-MSG`'nin dışından okunan baytların içeriği WORKING-STORAGE yerleşimine bağlıdır: belirsiz.
14. **WORKING-STORAGE kalıcılığı.**
    - `WS-REQUIRED-CA-LEN` `VALUE +0` ile başlar (`lgapol01.cbl:60`, `lgapdb01.cbl:66`) ve her çağrıda üstüne eklenir
      (`lgapol01.cbl:109`; `lgapdb01.cbl:182`, `:191`). Kodda sıfırlama yok. Program aynı süreçte WORKING-STORAGE korunarak tekrar
      çağrılırsa LGAPOL01'in eşiği 56, 84 … olur; LGAPDB01'inki 316, 474 … olur.
    - LGAPDB01'in `EM-POLNUM`'u yalnızca `:313`'te yazılır: korunmuş WORKING-STORAGE'da bir `-530` mesajı (08-06) önceki çağrının
      poliçe numarasını taşır.
    - LGAPDB01'in `EIBCALEN = 0` dalı `' CNUM='`, `' PNUM='` ve `' SQLCODE='` sabitlerini ezer (Kod gözlemleri 12) ve hiçbir satır onları geri
      yazmaz; dal ABEND ile biter (`:168`). Düzenekte süreç sürerse sonraki mesajlarda bu sabitlerin yerinde eski metin görünür.
    - Bu dosyadaki tahminler WORKING-STORAGE'ın `VALUE` değerleriyle başladığı duruma göre yazıldı. [belge, kodla doğrulanmadı]
      `docs/adr/0004-legacy-test-harness.md:50-52`: düzenekte her durum ayrı bir süreçte çalışır, çünkü CICS her görev için
      WORKING-STORAGE'ı yeniden kurar.
15. **Test verisi.** Bu değerler yalnızca Db2 `db2cre.jcl` ile, `KSDSPOLY` `ksdspoly.txt` ile yüklendiyse ve sonradan
    değiştirilmediyse geçerlidir.
    - `POLICY` (`db2cre.jcl:161-185`): `policyNumber` kimlik sütunu, `START WITH 1000001` (`:162-163`); `customerNumber NOT NULL`
      (`:164`); `lastChanged TIMESTAMP NOT NULL WITH DEFAULT` (`:168`); `commission SMALLINT` (`:172`); birincil anahtar
      `policyNumber` (`:173`); `CUSTOMER`'a yabancı anahtar, `ON DELETE CASCADE` (`:174-176`); `customerNumber` üzerinde tekil olmayan
      indeks (`:183-185`).
    - `HOUSE` (`:240-256`): `propertyType CHAR(15)`, `bedrooms SMALLINT`, `value INTEGER`, `houseName CHAR(20)`,
      `houseNumber CHAR(4)`, `postcode CHAR(8)` (`:242-247`); birincil anahtar `policyNumber` (`:248`); `POLICY`'ye yabancı anahtar,
      `ON DELETE CASCADE` (`:249-250`). `CHECK` kısıtı yok.
    - Örnek konut poliçeleri: 6 (müşteri 4; `POLICY` `:873-882`, `HOUSE` `:1051-1057`), 7 (müşteri 6; `:897-906`, `:1068-1074`),
      8 (müşteri 9; `:921-930`, `:1085-1091`). Müşteriler 4, 6, 9 var (`:539`, `:607`, `:709`). [belge, kodla doğrulanmadı]
      `base/Reference.md:119-121` aynı üç poliçeyi konut diye listeler.
    - **Poliçe 7 tutarsız:** `HOUSE` satırı var (`db2cre.jcl:1068-1074`), `KSDSPOLY` kaydının anahtarı `H` ile başlar (`ksdspoly.txt:6`),
      ama `POLICY.policyType = 'C'` (`db2cre.jcl:901`). Poliçe 7'nin `COMMERCIAL` satırı yok (ticari satırlar yalnızca 10 ve 9:
      `:1184`, `:1227`). Eklemede `policyType` okunmadığı için (Kod gözlemleri 5) bu tutarsızlık 08'deki hiçbir tahmini etkilemez.
      Ana test verisi poliçe 6 (ve 8).
    - `KSDSPOLY` örnek kayıtları: konut kayıtları `ksdspoly.txt:5-7` (müşteri/poliçe 4/6, 6/7, 9/8). Kayıt düzenine göre
      (Kod gözlemleri 10) oda alanı kayıt 5'te `'5 0'`, kayıt 7'de `'1 0'` (sayısal değil); kayıt 6'da `004`, Db2'de ise 8
      (`db2cre.jcl:1070`). VSAM ile Db2 birbirini tutmuyor. Ekleme VSAM'dan okumadığı için bu tahminleri etkilemez; 08-26'da
      seçilen anahtarın dosyada olmadığını gösterir.
    - B6 sağlanırsa ilk eklemenin numarası 1000001 olur. 1000001 `KSDSPOLY`'de de yoktur (`ksdspoly.txt:1-10`: poliçe 1-10).
16. **`CA-LASTCHANGED` dönüş değeri.** Başarılı `POLICY` INSERT'inden sonra alan, satırın `lastChanged` sütunuyla doldurulur
    (`lgapdb01.cbl:316-321`); sütuna INSERT anındaki `CURRENT TIMESTAMP` yazılmıştır (`:284`). Alan 26 bayttır (`lgcmarea.cpy:40`).
    Değerin karakter biçimi (`TIMESTAMP`'in `CHAR`'a dönüşümü) Db2'ye bağlıdır, kodda yok: **belirsiz** (C grubu). Bu dosyada
    `CA-LASTCHANGED` döndüren bütün satırlar için geçerlidir.
17. **Düzenek notu: veriye bağlı SQLCODE.** Düzenekte Db2 stub'ı yalnızca `SQLCODE`'u doldurur; değeri Db2 değil test senaryosu seçer.
    B4'ün "aksi söylenmedikçe SQLCODE 0" kuralı altında, [Db2 bilgisi] ile yazılan ve veriye bağlı olan SQLCODE'lar düzenekte senaryo
    girdisi olarak ayrıca kurulmalıdır: bu dosyada 08-05 (`-530`, yabancı anahtar ihlali). Bu değerin gerçek Db2'yle örtüşmesi
    [Db2 bilgisi] kabulüdür; düzenek bunu ölçmez. [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:56-58`,
    `:100-101`. Aynı not: `11-house-delete.md` Kod gözlemleri 16.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 08-01 | LGAPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01AHOU'`. `CA-CUSTOMER-NUM = 0000000004` (müşteri 4 var: `db2cre.jcl:539`). `CA-POLICY-NUM = 0000000000`. Poliçe ve konut alanlarında poliçe 6'nın değerleri var (`db2cre.jcl:875-881`, `:1052-1057`; alan alan aşağıdaki **"08-01 girdi ve beklenen değerler"** tablosunda). Db2 yalnızca `db2cre.jcl` ile, `KSDSPOLY` yalnızca `ksdspoly.txt` ile yüklenmiş. | `CA-RETURN-CODE = 00` | [B4] [B6] `CA-POLICY-NUM = 0001000001` (ön koşullu) [kod] `db2cre.jcl:162-163`, `lgapdb01.cbl:308-311`. Aşağıda P = 0001000001.<br>[B4] `CA-LASTCHANGED` = INSERT anındaki `CURRENT TIMESTAMP`, 26 bayt (karakter biçimi belirsiz: Kod gözlemleri 16); düzenek `SELECT LASTCHANGED`'in host değişkenine (`lgapdb01.cbl:316-321`) yalnızca satırda yazılan değeri koyar.<br>Diğer bütün alanlar girdideki gibi kalır.<br>Yan etkiler: `POLICY`'ye P numaralı, `policyType = 'H'` satır; `HOUSE`'a P numaralı satır eklenir; `KSDSPOLY`'ye 64 baytlık kayıt yazılır (alan alan: "08-01'den sonra Db2'de ve VSAM'da beklenen durum"). Hata mesajı yazılmaz.<br>Not: Müşteri 4'ün zaten poliçe 6'sı var (`db2cre.jcl:873-874`); `POLICY.customerNumber` üzerinde tekillik yok (`db2cre.jcl:183-185`). Poliçe 6'ya dokunulmaz. | `lgapol01.cbl:105`, `:109`, `:113`, `:121-124`, `:126`<br>`lgapdb01.cbl:172`, `:176`, `:182`, `:190-192`, `:210`, `:219`, `:264-293`, `:308-321`, `:228-229`, `:405-427`, `:243-246`, `:250`<br>`lgapvs01.cbl:99-101`, `:117-122`, `:135-142`, `:151-153` | İş kuralı |
| 08-02 | 08-01 ile aynı, ama `CA-H-HOUSE-NAME = 'Picnic Basket'` + ␣×7 (20 bayt). Değerin kaynağı müşteri 4'ün adresindeki ev adı (`db2cre.jcl:543`); burada yalnızca 9 bayttan uzun, kaynağı belli bir metin olarak kullanılıyor. | `CA-RETURN-CODE = 00` | 08-01 ile aynı ([B4] [B6] P = `0001000001`). `HOUSE.houseName = 'Picnic Basket'` (20 baytın tamamı).<br>`KSDSPOLY` kaydının son 9 baytı (56-64) = `Picnic Ba`: ev adı VSAM'da 9 bayta kesilir. | `lgapdb01.cbl:422`<br>`lgapvs01.cbl:46`, `:122`<br>`lgcmarea.cpy:60` | İş kuralı |
| 08-03 | 08-01 ile aynı, ama `CA-POLICY-NUM = 0000000006` (var olan poliçe 6: `db2cre.jcl:873`) ve `CA-LASTCHANGED = '2011-09-01 22:32:42'` + ␣×7 (poliçe 6'nın değeri: `db2cre.jcl:878`). | `CA-RETURN-CODE = 00` | Girdideki iki değer de kullanılmaz. `CA-POLICY-NUM` yeni numarayla ezilir: [B4] [B6] `0001000001`. [B4] `CA-LASTCHANGED` yeni zaman damgasıyla ezilir (Kod gözlemleri 16).<br>Poliçe 6'nın `POLICY` ve `HOUSE` satırlarına dokunulmaz: INSERT numarayı `DEFAULT` ile alır. `KSDSPOLY` anahtarı yeni numarayla kurulur. Geri kalanı 08-01 ile aynı. | `lgapdb01.cbl:279`, `:284`, `:311`, `:316-321`<br>`lgapvs01.cbl:100` | İş kuralı |
| 08-04 | 08-01 ile aynı, ama `CA-H-PROPERTY-TYPE` = ␣×15, `CA-H-BEDROOMS = 000`, `CA-H-VALUE = 00000000`. | `CA-RETURN-CODE = 00` | Hiçbir programda konut alanı doğrulaması yok. `HOUSE` satırı tür boşluk, oda 0, değer 0 olarak eklenir. `HOUSE` tanımında `CHECK` kısıtı yok (`db2cre.jcl:240-250`); JCL de bir `CHAR` sütununa boşluk ekliyor (`db2cre.jcl:1055`).<br>`KSDSPOLY` kaydında tür ␣×15, oda `000`, değer `00000000`. Numara [B4] [B6] P = `0001000001`. Geri kalanı 08-01 ile aynı. | `lgapdb01.cbl:405-425`<br>`lgapvs01.cbl:117-122` | İş kuralı |
| 08-05 | 08-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000000`. Müşteri 0 `CUSTOMER`'da yok: JCL 1-10'u verir (`db2cre.jcl:437` … `:743`), kimlik değeri 1000001'den başlar (`:111-112`). `POLICY.customerNumber` için `CUSTOMER`'a yabancı anahtar var (`db2cre.jcl:174-176`). | `CA-RETURN-CODE = 70` | `POLICY` INSERT'i yabancı anahtar yüzünden başarısız olur. [Db2 bilgisi] Db2 yabancı anahtar ihlalinde `SQLCODE = -530` döner. `70`/`90` ayrımı kodda: `-530` → `70` (`lgapdb01.cbl:295-296`), başka bir SQLCODE → `90` (`lgapdb01.cbl:300-301`). [belge, kodla doğrulanmadı] `base/Architecture.md:109-110`: müşteri kaydı yoksa poliçe oluşturulamaz.<br>• `CA-POLICY-NUM` girdideki gibi kalır (`0000000000`), `CA-LASTCHANGED` değişmez.<br>• `HOUSE` INSERT'i yapılmaz, LGAPVS01 çağrılmaz: `HOUSE`'a ve `KSDSPOLY`'ye yazılmaz.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır (kayıt biçimi: Kod gözlemleri 13).<br>(1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGAPDB01'`, `' CNUM=0000000000'`, `' PNUM='` + ␣×10 (numara henüz yazılmadı), `' INSERT POLICY'` + ␣×2, `' SQLCODE=-00530'`.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı ([CICS bilgisi] LGAPDB01 `EIBCALEN = 32500` görür: `ELSE` dalı); 7-8. baytlarda `70`. | `lgapdb01.cbl:176`, `:268-288`, `:290-305`, `:564`, `:575-578`, `:587-592`<br>`lgapol01.cbl:126`<br>`db2cre.jcl:174-176` | İş kuralı |
| 08-06 | 08-01 ile aynı. [B4] Düzenek `POLICY` INSERT'inde `SQLCODE = -530` üretir. | `CA-RETURN-CODE = 70` | `CA-POLICY-NUM` (`0000000000`) ve `CA-LASTCHANGED` değişmez. `HOUSE`'a ve `KSDSPOLY`'ye yazılmaz.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGAPDB01'`, `' CNUM=0000000004'`, `' PNUM='` + ␣×10, `' INSERT POLICY'` + ␣×2, `' SQLCODE=-00530'`. Kayıt 92 bayt, eksiksiz.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + ilk 90 bayt: `01AHOU`, `70`, `0000000004`, `0000000000`, `2011-09-01`, `2011-12-31`, ␣×26, `0000000000`, ␣×6. Kuyruğa bunun yalnızca ilk 81 baytı girer (`CA-BROKERID`'in ilk 7 hanesine kadar; Kod gözlemleri 13). | `lgapdb01.cbl:47-53`, `:267`, `:290`, `:295-298`, `:564`, `:575-592`<br>`lgapol01.cbl:126`<br>`lgstsq.cbl:48`, `:68-71`, `:82-83`, `:90`, `:94-111` | İş kuralı |
| 08-07 | 08-01 ile aynı. [B4] Düzenek `POLICY` INSERT'inde `0` ve `-530` dışında bir SQLCODE üretir. | `CA-RETURN-CODE = 90` | 08-06 ile aynı: `CA-POLICY-NUM` ve `CA-LASTCHANGED` değişmez, `HOUSE`'a ve `KSDSPOLY`'ye yazılmaz, aynı iki mesaj yazılır. Farklar: mesajdaki SQLCODE düzeneğin değeri; `CA-ERROR-MSG`'nin 7-8. baytları `90`. | `lgapdb01.cbl:290`, `:300-303` | İş kuralı |
| 08-08 | 08-01 ile aynı. `POLICY` INSERT'i başarılı. [B4] Düzenek `HOUSE` INSERT'inde `SQLCODE ≠ 0` üretir. | [B8] `CA-RETURN-CODE = 90` (ön koşullu). LGAPDB01 `90` yazar (`lgapdb01.cbl:428`), iki mesajı yazar, ardından `ABEND LGSQ` (`:431`). B8 ile düzenekte ABEND bir `GOBACK`'tir: LGAPDB01 LGAPOL01'e döner, o da RETURN yapar (`lgapol01.cbl:126`) ve çağıran `90`'ı görür. `lgapdb01.cbl:432`'deki RETURN'e yine ulaşılmaz. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır ve zincirde `HANDLE ABEND` yok (Kod gözlemleri 9): orada çağıranın `90`'ı görmesi **belirsiz** (C). | LGAPVS01 çağrılmaz: `KSDSPOLY`'ye yazılmaz. `HOUSE` satırı yok.<br>Önceden eklenen `POLICY` satırının geri alınıp alınmadığı **belirsiz** (C): `:430`'daki "Issue Abend to cause backout of update to Policy table" yalnızca yorumdur, kodda `SYNCPOINT ROLLBACK` yok (Kod gözlemleri 8, 9). [belge, kodla doğrulanmadı] `base/Architecture.md:48-50`.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: tarih, saat, `' LGAPDB01'`, `' CNUM=0000000004'`, `' PNUM='` + [B4] [B6] `0001000001`, `' INSERT HOUSE '` + ␣×2, `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + ilk 90 bayt (`ELSE` dalı); içinde `90`, P ve [B4] yeni `CA-LASTCHANGED` de var. | `lgapdb01.cbl:311-313`, `:316-321`, `:408`, `:427-433`, `:564-592` | İş kuralı |
| 08-09 | 08-01 ile aynı. `POLICY` ve `HOUSE` INSERT'leri başarılı. [B4] Düzenek `KSDSPOLY` WRITE'ında `NORMAL` dışında bir RESP üretir. | `CA-RETURN-CODE = 80`. LGAPVS01 `80` yazıp RETURN yapar, ABEND yok (`lgapvs01.cbl:142-147`). LGAPDB01 (`lgapdb01.cbl:243-250`) ve LGAPOL01 (`lgapol01.cbl:121-126`) dönüş kodunu kontrol etmeden RETURN yapar; `80` çağırana ulaşır. | [B4] `CA-POLICY-NUM = P` ([B6] `0001000001`), [B4] `CA-LASTCHANGED` yeni zaman damgası. `KSDSPOLY` kaydı yazılmaz.<br>`POLICY` ve `HOUSE` satırları **kodla geri alınmaz** (`SYNCPOINT ROLLBACK` ya da ABEND yok: Kod gözlemleri 9). Bu satırların görev sonunda kalıcı olup olmadığı **belirsiz** (C: CICS'in görev sonu davranışı). Kalıcı olurlarsa Db2'de poliçe var, VSAM'da yok. [belge, kodla doğrulanmadı] `base/Architecture.md:48`: "if successful, it updates VSAM"; VSAM hatasında ne olacağını söylemiyor.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG` (101 bayt): tarih, saat, `' LGAPVS01'`, `' PNUM='` + P, `' CNUM=0000000004'`, `' Write file KSDSPOLY'`, `' RESP='` + RESP, `' RESP2='` + RESP2. Kuyruğa yalnızca ilk 90 bayt girer: kayıt `' RESP='` + RESP + `' R'` ile biter, **RESP2 değeri kayda girmez**. Kayıt 106 bayt; son 11 baytı `WRITE-MSG`'nin dışından okunur (içeriği belirsiz; Kod gözlemleri 13).<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + LGAPVS01'e gelen COMMAREA'nın ilk 90 baytı ([CICS bilgisi] LGAPVS01 `EIBCALEN = 32500` görür: `ELSE` dalı); 7-8. baytlarda `80`. | `lgapvs01.cbl:58-73`, `:135-147`, `:155-172`, `:180-185`<br>`lgapdb01.cbl:243-250`<br>`lgapol01.cbl:121-126`<br>`lgstsq.cbl:25-28`, `:48`, `:70-71`, `:90` | İş kuralı |
| 08-10 | 08-01 ile aynı, ama `CA-REQUEST-ID` = ␣×6. | `CA-RETURN-CODE = 99` | LGAPOL01 istek kodunu okumaz, LGAPDB01'e geçirir. LGAPDB01'in ilk `EVALUATE`'i `WHEN OTHER`'a düşer: `99` yazılır ve hemen RETURN yapılır. SQL çalışmaz, LGAPVS01 çağrılmaz, hata mesajı yazılmaz. `CA-POLICY-NUM` ve `CA-LASTCHANGED` değişmez.<br>İkinci `EVALUATE`'in `WHEN OTHER`'ı (`lgapdb01.cbl:237-239`) çalışmaz (Kod gözlemleri 4). | `lgapol01.cbl:98-126`<br>`lgapdb01.cbl:184`, `:202-205` | Arayüz (COMMAREA) kuralı |
| 08-11 | 08-01 ile aynı, ama `CA-REQUEST-ID = '01ahou'` (küçük harf). | `CA-RETURN-CODE = 99` | 08-10 ile aynı. Eklemede istek kodu büyük/küçük harfe duyarlı karşılaştırılır; LGIPDB01'deki `UPPER-CASE` dönüşümü (`lgipdb01.cbl:275`) burada yok. | `lgapdb01.cbl:184`, `:190`, `:202-205` | Arayüz (COMMAREA) kuralı |
| 08-12 | 08-01 ile aynı, ama `CA-ISSUE-DATE` = ␣×10. | **belirsiz**. COBOL tarihi doğrulamaz, değer doğrudan `DATE` sütununa gider (`db2cre.jcl:165`). Db2 reddederse `SQLCODE ≠ 0` olur ve SQLCODE'a göre `70` ya da `90` döner (`lgapdb01.cbl:295-303`); kabul ederse `00`. | **belirsiz**. Reddedilirse 08-28, kabul edilirse 08-29 geçerli. Düzenekte iki durum B4 ile kurulur: 08-28 (`90`) ve 08-29 (`00`). | `lgapdb01.cbl:272`, `:281`, `:290-305` | İş kuralı |
| 08-13 | 08-01 ile aynı, ama `CA-H-BEDROOMS` = ␣×3 ve `CA-H-VALUE` = ␣×8 (sayısal alanlarda sayısal olmayan içerik). | **belirsiz** | **belirsiz**. Kesin olan: `POLICY` INSERT'i bu alanlardan önce yapılır (`lgapdb01.cbl:219` → `:268-288`). `:405-406` sayısal olmayan DISPLAY içeriğini `COMP` alanlara taşır; sonucun ne olacağı (sıfır, başka bir değer ya da veri istisnası/abend) derleyiciye bağlıdır, kodda tanımlı değil. | `lgapdb01.cbl:96-97`, `:405-406`<br>`lgcmarea.cpy:58-59` | Arayüz (COMMAREA) kuralı |
| 08-14 | 08-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000004` (10 haneli). | [B3] `CA-RETURN-CODE = 00` (ön koşullu) | [B3] `DB2-CUSTOMERNUM-INT` 9 hanelik `PIC S9(9) COMP` (`lgapdb01.cbl:90`); `MOVE` (`:176`) en soldaki haneyi keser ve değer 4 olur. `POLICY` satırı **müşteri 4** için eklenir (müşteri 4 var: `db2cre.jcl:539`).<br>`CA-CUSTOMER-NUM` değişmez (`1000000004`). `KSDSPOLY` anahtarı ise COMMAREA'dan kurulur: `H` + `1000000004` + P (`lgapvs01.cbl:101`). Db2'deki müşteri (4) ile VSAM anahtarındaki müşteri (1000000004) farklı olur. Numara [B4] [B6] P = `0001000001`. | `lgapdb01.cbl:90`, `:176`, `:280`<br>`lgapvs01.cbl:101`<br>`lgcmarea.cpy:12` | Arayüz (COMMAREA) kuralı |
| 08-15 | LGAPOL01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgapol01.cbl:101`'den sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGAPDB01 çağrılmaz, Db2'ye ve VSAM'a yazılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGAPOL01'`, `' NO COMMAREA RECEIVED'`; metin 21 baytlık `EM-VARIABLE`'a tam sığar). Kayıt 50 bayt. COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz). | `lgapol01.cbl:41-46`, `:98-102`, `:140-152`, `:154` | Arayüz (COMMAREA) kuralı |
| 08-16 | LGAPOL01 çağrılır. `EIBCALEN` 8 ile 27 arasında (sınır değer: `27`). | `CA-RETURN-CODE = 98` | LGAPDB01 çağrılmaz, Db2'ye ve VSAM'a yazılmaz, hata mesajı yazılmaz (`lgapol01.cbl:113-116` arasında `WRITE-ERROR-MESSAGE` yok). LGAPOL01 `CA-RETURN-CODE` dışında hiçbir alana yazmaz. | `lgapol01.cbl:59-60`, `:105`, `:109`, `:113-116` | Arayüz (COMMAREA) kuralı |
| 08-17 | LGAPOL01 çağrılır. `EIBCALEN` 1 ile 7 arasında. | [B1] Tamponun 7-8. baytları `98` olur (ön koşullu) [kod] `lgapol01.cbl:114`. Alan 7-8. baytlarda olduğu için gelen COMMAREA'nın dışında kalır (1-6'da tamamen, 7'de yarısı).<br>Ön koşulsuz kesin olan: `EIBCALEN = 7` ise COMMAREA'nın 7. baytı `9` olur, yani `98`'in ilk hanesi. | Kesin: LGAPDB01 çağrılmaz, Db2'ye ve VSAM'a yazılmaz, hata mesajı yazılmaz. 1-6. baytlar (`CA-REQUEST-ID`) değişmez.<br>Müşteri eklemeden (LGACUS01) farklı olarak COMMAREA dışına başka bir yazma yok: LGAPOL01'in yazdığı tek alan `CA-RETURN-CODE` (`:105`, `:114`). | `lgcmarea.cpy:10-11`<br>`lgapol01.cbl:98`, `:105`, `:113-116` | Arayüz (COMMAREA) kuralı |
| 08-18 | LGAPOL01 çağrılır. `EIBCALEN` 126 ile 157 arasında (sınır değerler: `126` ve `157`). `EIBCALEN` içindeki girdiler 08-01 ile aynı. | [B1] `CA-RETURN-CODE = 00` (ön koşullu). B1 sağlanmazsa belirsiz: COMMAREA dışından okuma ortama bağlı. | Kesin olanlar:<br>• LGAPOL01 kontrolü geçer (`EIBCALEN ≥ 28`). [CICS bilgisi] LGAPDB01 `EIBCALEN = 32500` görür, 158'lik kontrol geçer. **Zincirde `98` dönmez**; LGAPDB01 doğrudan çağrılsa reddederdi (08-22).<br>• INSERT'lerin kullandığı sayısal alanların hepsi (`CA-H-VALUE`'nun sonu 126. bayt) gelen COMMAREA'nın içinde.<br>[B1] ile (ön koşullu):<br>• `EIBCALEN`'in ötesindeki metin alanları boşluk okunur. `EIBCALEN = 126` iken `CA-H-HOUSE-NAME`, `CA-H-HOUSE-NUMBER`, `CA-H-POSTCODE` boşluk olarak `HOUSE`'a, ev adı ve posta kodu boşluk olarak `KSDSPOLY`'ye yazılır.<br>• `EIBCALEN = 157` iken yalnızca posta kodunun 8. baytı dışarıda; girdide o bayt zaten boşluk olduğundan sonuç 08-01 ile aynı.<br>• Numara [B4] [B6] P = `0001000001`. | `lgapol01.cbl:109`, `:113`, `:121-124`<br>`lgapdb01.cbl:191`, `:210`, `:422-424`<br>`lgapvs01.cbl:121-122`<br>`lgcmarea.cpy:59-62` | Arayüz (COMMAREA) kuralı |
| 08-19 | LGAPOL01 çağrılır. `EIBCALEN` 28 ile 125 arasında (sınır değerler: `28` ve `125`). | **belirsiz**. Kesin olan ([CICS bilgisi]): `98` dönmez (08-18 ile aynı neden). | **belirsiz**. INSERT'lerin kullandığı tarihlerin ya da sayısal alanların bir kısmı gelen COMMAREA'nın dışında: `CA-ISSUE-DATE` 29-38, `CA-EXPIRY-DATE` 39-48, `CA-BROKERID` 75-84, `CA-PAYMENT` 95-100, `CA-H-BEDROOMS` 116-118, `CA-H-VALUE` 119-126. B1 ile bile boşluklu sayısal alanın `COMP`'a taşınması (08-13) ve boş tarihin Db2'ye gitmesi (08-12) belirsizdir. | `lgapol01.cbl:113`<br>`lgapdb01.cbl:210`, `:264-265`, `:281-282`, `:405-406`<br>`lgcmarea.cpy:38-43`, `:58-59` | Arayüz (COMMAREA) kuralı |
| 08-20 | LGAPOL01 çağrılır. `EIBCALEN = 32500`. LGAPDB01'e ya da LGAPVS01'e yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. İki LINK'te de `RESP`/`NOHANDLE` yok, programlarda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgapol01.cbl:121-124`<br>`lgapdb01.cbl:243-246` | Arayüz (COMMAREA) kuralı |
| 08-21 | **LGAPDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgapdb01.cbl:168`'den sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 87 baytlık `ERROR-MSG` (tarih, saat, `' LGAPDB01'`, `' NO COMMAREA RECEIVED'` + ␣×36 + 6 baytlık `EM-SQLRC`). [B2] `EM-SQLRC = +00000` (ön koşullu; SQLCA'nın kaynağı `base/` altında yok) [kod] `lgapdb01.cbl:564`. COMMAREA dökümü gönderilmez.<br>LGAPOL01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 2). | `lgapdb01.cbl:46-53`, `:165-169`, `:564`, `:575-580` | Arayüz (COMMAREA) kuralı |
| 08-22 | **LGAPDB01 doğrudan** çağrılır. `CA-REQUEST-ID = '01AHOU'`. `CA-CUSTOMER-NUM = 0000000004`. `EIBCALEN` 18 ile 157 arasında (sınır değerler: `18` ve `157`). 18'den küçük uzunluklar 08-30'da: `CA-CUSTOMER-NUM` uzunluk kontrolünden önce okunur (Kod gözlemleri 2). | `CA-RETURN-CODE = 98` | SQL çalışmaz, LGAPVS01 çağrılmaz, hata mesajı yazılmaz. Eşik 28 + 130 = 158 (Kod gözlemleri 2). Kontrolden önce çalışan `lgapdb01.cbl:176` ve `:179` `CA-CUSTOMER-NUM`'u (9-18. baytlar) gelen COMMAREA'nın içinden okur. | `lgapdb01.cbl:65-66`, `:172`, `:176`, `:179`, `:182`, `:190-191`, `:210-213`<br>`lgpolicy.cpy:25`<br>`lgcmarea.cpy:12` | Arayüz (COMMAREA) kuralı |
| 08-23 | **LGAPDB01 doğrudan** çağrılır. `EIBCALEN = 158` (kabul edilen en küçük uzunluk). Diğer girdiler 08-01 ile aynı. | [B1] `CA-RETURN-CODE = 00` (ön koşullu) | Kesin: `158 < 158` yanlış, kontrol geçer. LGAPDB01'in ve LGAPVS01'in okuduğu bütün alanlar 1-158. baytlarda.<br>[B1] LGAPDB01, LGAPVS01'e kendi 158 baytlık COMMAREA'sını `LENGTH(32500)` ile geçirir (`lgapdb01.cbl:243-246`); B1 ile bu, düzeneğin 32500 baytlık tamponudur. Sonuç ve yan etkiler 08-01 ile aynı ([B4] [B6] P = `0001000001`). | `lgapdb01.cbl:210`, `:243-246`<br>`lgcmarea.cpy:62` | Arayüz (COMMAREA) kuralı |
| 08-24 | **LGAPDB01 doğrudan** çağrılır. `EIBCALEN = 18`. `CA-REQUEST-ID` = ␣×6. `CA-CUSTOMER-NUM = 0000000004` (`lgapdb01.cbl:176` ve `:179` alanı istek kodundan önce okur; `EIBCALEN = 18` iken alan gelen COMMAREA'nın içinde). | `CA-RETURN-CODE = 99` (`98` değil) | İstek kodu uzunluktan **önce** kontrol edilir: ilk `EVALUATE` `WHEN OTHER`'da RETURN yapar, `lgapdb01.cbl:210`'a ulaşılmaz. SQL çalışmaz, hata mesajı yazılmaz. | `lgapdb01.cbl:176`, `:179`, `:184`, `:202-205`, `:210` | Arayüz (COMMAREA) kuralı |
| 08-25 | **LGAPVS01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | **belirsiz** | **belirsiz**. LGAPVS01'de `EIBCALEN` kontrolü yok (`lgapvs01.cbl:97` yalnızca kopyalar). `:99-101` var olmayan COMMAREA'dan `CA-Request-ID`, `CA-Policy-Num`, `CA-Customer-Num` okur. Sonuç kodda tanımlı değil. | `lgapvs01.cbl:97-101` | Arayüz (COMMAREA) kuralı |
| 08-26 | **LGAPVS01 doğrudan** çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01AHOU'`. `CA-RETURN-CODE = 55` (yazılmadığını göstermek için seçilmiş değer; kaynağı yok). `CA-CUSTOMER-NUM = 0000000004`. `CA-POLICY-NUM = 0000000099` (seçilmiş değer: `H00000000040000000099` anahtarı `ksdspoly.txt:1-10`'da yok). Konut alanları 08-01 ile aynı. `KSDSPOLY` yalnızca `ksdspoly.txt` ile yüklenmiş. [B4] WRITE `RESP = NORMAL`. | `CA-RETURN-CODE = 55` (değişmez). LGAPVS01 başarı yolunda dönüş koduna yazmaz. | `KSDSPOLY`'ye `H00000000040000000099` anahtarlı 64 baytlık kayıt yazılır: `HOUSE` + ␣×10, `005`, `01500000`, `SO211UP` + ␣, ␣×9. Hata mesajı yazılmaz. Db2'ye dokunulmaz. Diğer bütün alanlar değişmez. | `lgapvs01.cbl:97-101`, `:117-122`, `:135-142`, `:151-153` | Arayüz (COMMAREA) kuralı |
| 08-27 | **LGAPVS01 doğrudan** çağrılır. `CA-REQUEST-ID = '01AXOU'`. Gerekçe: dal seçimi yalnızca 4. karaktere bakar (`lgapvs01.cbl:99`, `:103`); `'X'` C/E/H/M'nin hiçbiri değildir, bu yüzden `When Other` veri alanını boşlukla doldurur (`:130-131`). Bu yolda COMMAREA'dan yalnızca 1-28. baytlar okunur; `EIBCALEN` dışından sayısal alan okunmaz. `EIBCALEN` 28 ile 90 arasında (anahtar alanları 9-28. baytlarda, içeride). [B4] Düzenek WRITE'ta `NORMAL` dışında bir RESP üretir. | `CA-RETURN-CODE = 80`. Bu programda ABEND yok, RETURN yapılır. | Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG` (101 bayt): tarih, saat, `' LGAPVS01'`, `' PNUM='` + `CA-POLICY-NUM`, `' CNUM='` + `CA-CUSTOMER-NUM`, `' Write file KSDSPOLY'`, RESP, RESP2. Kuyrukta RESP2 değeri yok (08-09 ile aynı kesilme).<br>(2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır.<br>Not: RESP `NORMAL` olsaydı kayıt `X` + müşteri + poliçe anahtarı ve 43 boşlukla yazılırdı. | `lgapvs01.cbl:99-103`, `:130-131`, `:135-147`, `:155-179` | Arayüz (COMMAREA) kuralı |
| 08-28 | 08-12 ile aynı girdi (`CA-ISSUE-DATE` = ␣×10). [B4] Düzenek bu `POLICY` INSERT'inde `0` ve `-530` dışında bir SQLCODE üretir (Db2'nin boş tarihi reddettiği durum). | `CA-RETURN-CODE = 90` | COBOL tarihi kontrol etmez; değer INSERT'e olduğu gibi gider (`lgapdb01.cbl:281`). Sonuç 08-07 ile aynı: `CA-POLICY-NUM` ve `CA-LASTCHANGED` değişmez, `HOUSE`'a ve `KSDSPOLY`'ye yazılmaz, aynı iki mesaj (`' CNUM=0000000004'`, `' INSERT POLICY'`, düzeneğin SQLCODE'u); `CA-ERROR-MSG`'nin 7-8. baytları `90`. | `lgapdb01.cbl:281`, `:290`, `:300-303` | İş kuralı |
| 08-29 | 08-12 ile aynı girdi. [B4] Düzenek bu `POLICY` INSERT'ini başarılı döndürür (`SQLCODE = 0`; Db2'nin boş tarihi kabul ettiği durum). | `CA-RETURN-CODE = 00` | Akış 08-01 ile aynı: COBOL'da tarih doğrulaması yok (Kod gözlemleri 6). [B4] [B6] `CA-POLICY-NUM = 0001000001`, [B4] `CA-LASTCHANGED` 08-01'deki gibi; `HOUSE` ve `KSDSPOLY` 08-01'deki gibi yazılır. `POLICY.issueDate` sütununa ne saklandığı Db2'ye bağlıdır (**belirsiz**, C; 08-12). | `lgapdb01.cbl:281`, `:290-293`, `:308-321`, `:405-427`, `:243-246` | İş kuralı |
| 08-30 | **LGAPDB01 doğrudan** çağrılır. `CA-REQUEST-ID = '01AHOU'`. `EIBCALEN` 8 ile 17 arasında (sınır değerler: `8` ve `17`). | **belirsiz**. `lgapdb01.cbl:176` `CA-CUSTOMER-NUM`'u (9-18. baytlar) uzunluk kontrolünden (`:210`) önce `S9(9) COMP`'a taşır; alan kısmen ya da tamamen gelen COMMAREA'nın dışındadır. [B1] ile dışarıdaki baytlar boşluktur ve alan sayısal olmayan içerik taşır: taşımanın sonucu derleyiciye bağlıdır (C; 08-13 ile aynı). B1 olmadan COMMAREA dışından okuma ortama bağlıdır. Taşıma hata vermezse `:210` `98` yazıp RETURN yapar. | **belirsiz**. Taşıma hata vermezse 08-22 ile aynı: SQL çalışmaz, LGAPVS01 çağrılmaz, hata mesajı yazılmaz. `:172` `'00'`'ı 7-8. baytlara (gelen COMMAREA'nın içinde) yazar; `:179` alanı `EM-CUSNUM`'a da kopyalar, mesaj yazılmadığı için etkisi yok. | `lgapdb01.cbl:172`, `:176`, `:179`, `:182`, `:190-191`, `:210-213`<br>`lgcmarea.cpy:12` | Arayüz (COMMAREA) kuralı |

## 08-01 girdi ve beklenen değerler

COMMAREA'nın alan alan girdisi ve dönüşte beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Girdi | Girdinin kaynağı (`db2cre.jcl`) | Dönüşte beklenen | Alanı kullanan / dolduran COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` X(6) | `01AHOU` | — (istek kodu: `lgapdb01.cbl:190`) | Değişmez | `lgapdb01.cbl:184`, `:223`; `lgapvs01.cbl:99` |
| `CA-RETURN-CODE` 9(2) | Herhangi bir değer | — | `00` | `lgapol01.cbl:105`, `lgapdb01.cbl:172`, `:293` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000004` | `:539` (poliçe 6'nın müşterisi: `:874`) | Değişmez | `lgapdb01.cbl:176`, `:280`; `lgapvs01.cbl:101` |
| `CA-POLICY-NUM` 9(10) | `0000000000` | — | `0001000001` (ön koşul B6) | `lgapdb01.cbl:308-311`; `lgapvs01.cbl:100` |
| `CA-ISSUE-DATE` X(10) | `2011-09-01` | `:875` (sütun `DATE`: `:165`) | Değişmez | `lgapdb01.cbl:281` |
| `CA-EXPIRY-DATE` X(10) | `2011-12-31` | `:876` (sütun `DATE`: `:166`) | Değişmez | `lgapdb01.cbl:282` |
| `CA-LASTCHANGED` X(26) | ␣×26 | — | INSERT anındaki `CURRENT TIMESTAMP`, 26 bayt (biçim belirsiz: Kod gözlemleri 16) | `lgapdb01.cbl:284`, `:316-321` |
| `CA-BROKERID` 9(10) | `0000000000` | `:879` (sütun `INTEGER`: `:169`) | Değişmez | `lgapdb01.cbl:264`, `:285` |
| `CA-BROKERSREF` X(10) | ␣×10 (JCL'de `''`) | `:880` (sütun `CHAR(10)`: `:170`) | Değişmez | `lgapdb01.cbl:286` |
| `CA-PAYMENT` 9(6) | `000000` | `:881` (sütun `INTEGER`: `:171`) | Değişmez | `lgapdb01.cbl:265`, `:287` |
| `CA-H-PROPERTY-TYPE` X(15) | `HOUSE` + ␣×10 | `:1052` (sütun `CHAR(15)`: `:242`) | Değişmez | `lgapdb01.cbl:419`; `lgapvs01.cbl:118` |
| `CA-H-BEDROOMS` 9(3) | `005` | `:1053` (sütun `SMALLINT`: `:243`) | Değişmez | `lgapdb01.cbl:406`, `:420`; `lgapvs01.cbl:119` |
| `CA-H-VALUE` 9(8) | `01500000` | `:1054` (sütun `INTEGER`: `:244`) | Değişmez | `lgapdb01.cbl:405`, `:421`; `lgapvs01.cbl:120` |
| `CA-H-HOUSE-NAME` X(20) | ␣×20 (JCL'de `' '`) | `:1055` (sütun `CHAR(20)`: `:245`) | Değişmez | `lgapdb01.cbl:422`; `lgapvs01.cbl:122` |
| `CA-H-HOUSE-NUMBER` X(4) | `␣␣␣5` | `:1056` (sütun `CHAR(4)`: `:246`) | Değişmez | `lgapdb01.cbl:423` (VSAM'a yazılmaz) |
| `CA-H-POSTCODE` X(8) | `SO211UP` + ␣×1. Karakterler S-O-2-1-1-U-P: 2. karakter O harfi | `:1057` (sütun `CHAR(8)`: `:247`) | Değişmez | `lgapdb01.cbl:424`; `lgapvs01.cbl:121` |
| `CA-H-FILLER` X(32342) | Herhangi bir değer | — | Değişmez | Kodda okunmuyor, yazılmıyor |

Poliçe 6'nın `commission` değeri (`db2cre.jcl:882`) COMMAREA'da yok; LGAPDB01 bu sütunu INSERT'e koymaz (Kod gözlemleri 6).

## 08-01'den sonra Db2'de ve VSAM'da beklenen durum

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `POLICY` | Yeni satır: `policyNumber` = P (B6: 1000001), `customerNumber` = 4, `issueDate` = 2011-09-01, `expiryDate` = 2011-12-31, `policyType` = `H`, `lastChanged` = INSERT anındaki zaman damgası, `brokerId` = 0, `brokersReference` = boşluk, `payment` = 0. `commission` INSERT listesinde yok; değeri Db2'ye kalır (`db2cre.jcl:172`). | `lgapdb01.cbl:268-288` |
| `HOUSE` | Yeni satır: `policyNumber` = P, `propertyType` = `HOUSE`, `bedrooms` = 5, `value` = 1500000, `houseName` = boşluk, `houseNumber` = `␣␣␣5`, `postcode` = `SO211UP`. | `lgapdb01.cbl:405-425` |
| `KSDSPOLY` | 64 baytlık kayıt: `H` + `0000000004` + `0001000001` (anahtar, 21 bayt) + `HOUSE` + ␣×10 + `005` + `01500000` + `SO211UP␣` + ␣×9 (ev adının ilk 9 baytı). | `lgapvs01.cbl:25-46`, `:99-101`, `:117-122`, `:135-141` |
| Poliçe 6 (`POLICY`, `HOUSE`, `KSDSPOLY`) | Değişmez. | — |
| `GENAERRS` / `CSMT` | Kayıt yazılmaz. | — |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları. "Başka tür" maddelerine tahmin yazılmadı; bu
maddeler, dalın alınmadığı bir konut testine bağlandı.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| PO-01 | `lgapol01.cbl:98-102` `IF EIBCALEN = 0` → ABEND `LGCA` | 08-15 | |
| PO-02 | `lgapol01.cbl:105` `'00'` → `CA-RETURN-CODE` (kontrolden önce) | 08-01, 08-16, 08-17 | |
| PO-03 | `lgapol01.cbl:59`, `:109` gerekli uzunluk 28 | 08-16, 08-18 | 27 reddedilir, 28 ve üstü geçer |
| PO-04 | `lgapol01.cbl:113-116` `IF EIBCALEN < 28` → `98` | 08-16, 08-17 | 08-17: alan dışına yazma (B1) |
| PO-05 | `lgapol01.cbl:121-124` LINK LGAPDB01 `LENGTH(32500)`, `RESP` yok | 08-01, 08-18, 08-20 | |
| PO-06 | `lgapol01.cbl:126` RETURN; LGAPDB01'in dönüş kodu kontrol edilmez | 08-01, 08-05, 08-09 | |
| PO-07 | `lgapol01.cbl:137-152` `WRITE-ERROR-MESSAGE`, 45 baytlık `ERROR-MSG` | 08-15 | |
| PO-08 | `lgapol01.cbl:154-168` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 08-15 | Dış `IF` yanlış. İç dallar (`:155-167`) **kapsam dışı: ulaşılamaz**; yordam yalnızca `:100`'den çağrılıyor (Kod gözlemleri 12) |
| DB-01 | `lgapdb01.cbl:165-169` `IF EIBCALEN = 0` → ABEND `LGCA` | 08-21 | Yalnızca doğrudan çağrı |
| DB-02 | `lgapdb01.cbl:172` `'00'` → `CA-RETURN-CODE` | 08-01 | |
| DB-03 | `lgapdb01.cbl:176`, `:179` müşteri numarası → `S9(9) COMP` ve `EM-CUSNUM` | 08-01, 08-14, 08-22, 08-30 | 08-14: B3 kesmesi; 08-30: uzunluk kontrolünden önce COMMAREA dışından okuma (belirsiz) |
| DB-04 | `lgapdb01.cbl:177` `DB2-C-PolicyNum-INT = 0` | 08-01 | Satır çalışır; konut yolunda etkisi yok |
| DB-05 | `lgapdb01.cbl:182` gerekli uzunluk 28'den başlar | 08-22, 08-23 | |
| DB-06a | `lgapdb01.cbl:186-188` `WHEN '01AEND'` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DB-06b | `lgapdb01.cbl:190-192` `WHEN '01AHOU'` → 158, `'H'` | 08-01, 08-22 | |
| DB-06c | `lgapdb01.cbl:194-196` `WHEN '01AMOT'` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DB-06d | `lgapdb01.cbl:198-200` `WHEN '01ACOM'` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DB-06e | `lgapdb01.cbl:202-205` `WHEN OTHER` → `99`, RETURN | 08-10, 08-11, 08-24 | Uzunluk kontrolünden önce |
| DB-07 | `lgapdb01.cbl:210-213` `IF EIBCALEN < 158` → `98` | 08-22 (reddedilir), 08-23 (kabul), 08-18 (zincirde 32500 görür), 08-30 (taşıma hata vermezse) | Zincirde hiç alınmaz |
| DB-08 | `lgapdb01.cbl:219`, `:264-267` `INSERT-POLICY` hazırlığı | 08-01 | |
| DB-09 | `lgapdb01.cbl:268-288` `INSERT POLICY` | 08-01, 08-05 | |
| DB-10a | `lgapdb01.cbl:292-293` `When 0` → `00` | 08-01, 08-29 | Kod gözlemleri 7 |
| DB-10b | `lgapdb01.cbl:295-298` `When -530` → `70`, mesaj, RETURN | 08-05 ([Db2 bilgisi]), 08-06 (B4) | Kod gözlemleri 7 |
| DB-10c | `lgapdb01.cbl:300-303` `When Other` → `90`, mesaj, RETURN | 08-07, 08-12, 08-28 | Kod gözlemleri 7 |
| DB-11 | `lgapdb01.cbl:308-310` `IDENTITY_VAL_LOCAL()`, SQLCODE kontrolü yok | 08-01 | Başarısızlığı belirsiz (Kod gözlemleri 7, uç durum) |
| DB-12 | `lgapdb01.cbl:311`, `:313` `CA-POLICY-NUM`, `EM-POLNUM` ← yeni numara | 08-01, 08-03 | |
| DB-13 | `lgapdb01.cbl:316-321` `SELECT LASTCHANGED`, SQLCODE kontrolü yok | 08-01, 08-03 | Biçim belirsiz (Kod gözlemleri 16) |
| DB-14a | `lgapdb01.cbl:225-226` `WHEN '01AEND'` → `INSERT-ENDOW` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DB-14b | `lgapdb01.cbl:228-229` `WHEN '01AHOU'` → `INSERT-HOUSE` | 08-01 | |
| DB-14c | `lgapdb01.cbl:231-232` `WHEN '01AMOT'` → `INSERT-MOTOR` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DB-14d | `lgapdb01.cbl:234-235` `WHEN '01ACOM'` → `INSERT-COMMERCIAL` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DB-14e | `lgapdb01.cbl:237-239` `WHEN OTHER` → `99` | 08-10 (dal alınmaz) | **Kapsam dışı: ulaşılamaz.** Bilinmeyen kod `:205`'te RETURN yapar (Kod gözlemleri 4) |
| DB-15 | `lgapdb01.cbl:405-406` konut sayısal dönüşümleri | 08-01, 08-13 | 08-13 belirsiz |
| DB-16 | `lgapdb01.cbl:408-425` `INSERT HOUSE` | 08-01, 08-02, 08-04 | |
| DB-17 | `lgapdb01.cbl:427-433` `IF SQLCODE ≠ 0` → `90`, mesaj, ABEND `LGSQ` | 08-08 | `:432` RETURN **kapsam dışı: ulaşılamaz** (ABEND'den sonra). Kod gözlemleri 8 |
| DB-18 | `lgapdb01.cbl:243-246` LINK LGAPVS01 `LENGTH(32500)`, dönüş kodu kontrol edilmez | 08-01, 08-09 | |
| DB-19 | `lgapdb01.cbl:250` RETURN | 08-01 | |
| DB-20 | `lgapdb01.cbl:562-578` `WRITE-ERROR-MESSAGE`, 87 baytlık `ERROR-MSG` | 08-05, 08-06, 08-21 | |
| DB-21 | `lgapdb01.cbl:580-594` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 08-06 (`ELSE`), 08-21 (dış `IF` yanlış) | `< 91` dalı (`:581-586`) **kapsam dışı: ulaşılamaz** (Kod gözlemleri 12) |
| DB-BT | `lgapdb01.cbl:327-397` `INSERT-ENDOW`, `:440-481` `INSERT-MOTOR`, `:486-555` `INSERT-COMMERCIAL` | 08-01 (paragraflar çağrılmaz) | **Başka tür**: tahmin yazılmadı |
| VS-01 | `lgapvs01.cbl:97` `EIBCALEN` kontrolü yok | 08-25 | |
| VS-02 | `lgapvs01.cbl:99-101` anahtar | 08-01, 08-14 | |
| VS-03a | `lgapvs01.cbl:105-108` `When 'C'` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| VS-03b | `lgapvs01.cbl:110-115` `When 'E'` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| VS-03c | `lgapvs01.cbl:117-122` `When 'H'` | 08-01, 08-02 | Ev adı 9 bayt, ev numarası yok |
| VS-03d | `lgapvs01.cbl:124-128` `When 'M'` | 08-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| VS-03e | `lgapvs01.cbl:130-131` `When Other` → boşluk | 08-27 | Zincirden **ulaşılamaz** (yalnızca dört geçerli kod gelir); doğrudan çağrıda alınır |
| VS-04 | `lgapvs01.cbl:135-141` WRITE `KSDSPOLY` | 08-01, 08-26 | |
| VS-05 | `lgapvs01.cbl:142-147` `RESP ≠ NORMAL` → `80`, mesaj, RETURN (ABEND yok) | 08-09, 08-27 | |
| VS-06 | `lgapvs01.cbl:151-153` GOBACK; dönüş koduna yazılmaz | 08-01, 08-26 | |
| VS-07 | `lgapvs01.cbl:155-172` `WRITE-ERROR-MESSAGE`, 101 baytlık `ERROR-MSG` | 08-09, 08-27 | |
| VS-08 | `lgapvs01.cbl:173-187` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 08-09 (`ELSE`), 08-27 (`< 91`) | Dış `IF` yanlış yalnızca `EIBCALEN = 0` iken; o yol belirsiz (08-25) |
| SQ-01 | `lgstsq.cbl:68-71` `IF WS-INVOKEPROG NOT = SPACES` (LINK yolu) | 08-05, 08-06, 08-08, 08-09, 08-15, 08-21, 08-27 | |
| SQ-02 | `lgstsq.cbl:72-80` `ELSE` (terminalden RECEIVE) | 08-06 (dal alınmaz) | **Kapsam dışı: bu zincirden ulaşılamaz.** Üç program da LGSTSQ'yu LINK ile çağırır |
| SQ-03 | `lgstsq.cbl:82-88` `IF` mesaj `'Q='` ile başlıyor | 08-06 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** Mesajlar tarih ya da `'COMMAREA='` ile başlar |
| SQ-04 | `lgstsq.cbl:90`, `:94-99` +5, WRITEQ TD `CSMT`, RESP kontrol edilmez | 08-06 | |
| SQ-05 | `lgstsq.cbl:105-111` WRITEQ TS `GENAERRS`, RESP kontrol edilmez | 08-06 | |
| SQ-06 | `lgstsq.cbl:113-119` `IF WS-FLAG = 'R'` → SEND TEXT | 08-06 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** `'R'` yalnızca SQ-02 yolunda atanır |
| SQ-07 | `lgstsq.cbl:121-122` RETURN | 08-06 | |
| SQ-08 | Kayıt uzunlukları 45 → 50, 87 → 92, 99 → 104 (81 bayt) | 08-06, 08-15, 08-21 | Kod gözlemleri 13 |
| SQ-09 | LGAPVS01'in 101 baytlık mesajı → 106, RESP2 kayda girmez | 08-09, 08-27 | Kod gözlemleri 13 |
| N-01 | Müşteri varlığı kodda kontrol edilmez; tek koruma yabancı anahtar | 08-05 | Kod gözlemleri 7 |
| N-02 | Alan doğrulaması yok | 08-04, 08-12, 08-13, 08-28, 08-29 | Kod gözlemleri 6 |
| N-03 | LINK'lerde `RESP` yok | 08-20 | Kod gözlemleri 3 |
| N-04 | `SYNCPOINT` / `ROLLBACK` / `HANDLE` yok | 08-08, 08-09 | Kod gözlemleri 9 (satır aralıklarıyla) |
| N-05 | Girdideki `CA-POLICY-NUM` ve `CA-LASTCHANGED` kullanılmaz | 08-03 | Kod gözlemleri 6 |
| N-06 | `policyType` eklemede yalnızca yazılır, okunmaz | 08-01 | Kod gözlemleri 5; poliçe 7 tutarsızlığı eklemeyi etkilemez |
| N-07 | Zincirde 28-157 bayt reddedilmez | 08-18, 08-19 | Kod gözlemleri 2 |
| N-08 | WORKING-STORAGE kalıcılığı | 08-01, 08-21 | Tahminler `VALUE` başlangıcına göre (Kod gözlemleri 14) |
| N-09 | İstek kodu büyük/küçük harfe duyarlı | 08-11 | Kod gözlemleri 4 |
| N-10 | `80` sonrasında Db2 ile VSAM tutarsız kalabilir | 08-09 | |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları"). B4 satırlarında son sütun iki durumu ayrı yazar: **düzenekte** (senaryo değeri vermez ya da host değişkenine yazmazsa; B4'ün varsayılanı `0` / `NORMAL`) ve **gerçek Db2/VSAM'de** (sonucu gerçek sistem verirse).

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 08-01 | B4, B6 | `CA-POLICY-NUM = 0001000001`; `POLICY`, `HOUSE` ve `KSDSPOLY`'de bu numara; `CA-LASTCHANGED` = INSERT anındaki zaman damgası | B6 yoksa numara Db2'nin ürettiği daha büyük bir değer, tam değeri belirsiz. B4 yoksa: **düzenekte** host değişkeni eki sağlanmazsa `CA-POLICY-NUM = 0` ve `CA-LASTCHANGED` girdideki gibi kalır (tablonun altındaki not); **gerçek Db2'de** iki değeri Db2 verir (biçim: Kod gözlemleri 16). `00` iki durumda da değişmez |
| 08-02 | B4, B6 | 08-01 ile aynı; VSAM'da `Picnic Ba` | Numara belirsiz (B6) ya da düzenekte 0 (B4 eki; tablonun altındaki not); kesilme tahmini değişmez |
| 08-03 | B4, B6 | Yeni numara `0001000001`; girdideki 6 yok sayılır | Girdinin yok sayılması yine geçerli; yeni numara belirsiz (B6) ya da düzenekte 0 (B4 eki; tablonun altındaki not) |
| 08-04 | B4, B6 | 08-01 ile aynı, tür boşluk, oda ve değer 0 | Numara belirsiz (B6) ya da düzenekte 0 (B4 eki; tablonun altındaki not) |
| 08-06 | B4 | Girdi düzenekle kurulur; tahmin (`70`) değişmez | **Düzenekte:** senaryo `-530` vermezse INSERT `0` döner, 08-01 yolu (`00`). **Gerçek Db2'de:** bu girdiyle kurulamaz (müşteri 4 var); aynı sonuç 08-05'te veri yoluyla oluşur ([Db2 bilgisi]) |
| 08-07 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | **Düzenekte:** senaryo SQLCODE vermezse 08-01 yolu (`00`). **Gerçek Db2'de:** örnek veride `POLICY` INSERT'ini `0` ve `-530` dışında bir koda düşürecek kesin bir veri yolu yok; boş tarih (08-12) bir aday, sonucu belirsiz |
| 08-08 | B4, B6, B8 | `90` çağırana ulaşır (düzenekte ABEND = `GOBACK`); mesajda P = `0001000001` | B4 yoksa: **düzenekte** `HOUSE` INSERT'i `0` döner, 08-01 yolu (`00`); **gerçek Db2'de** girdi kurulamaz (`HOUSE` INSERT'ini hataya düşürecek veri yolu yok: P, `HOUSE`'taki 6, 7, 8'den farklı). B6 yoksa P belirsiz. B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGSQ` ile sonlanır; çağıranın `90`'ı görmesi belirsiz (C) |
| 08-09 | B4, B6 | Girdi düzenekle kurulur; `80`; mesajda P = `0001000001` | B4 yoksa: **düzenekte** WRITE `NORMAL` döner, 08-01 yolu (`00`); **gerçek VSAM'da** girdi kurulamaz (yeni anahtar dosyada yok, yazma hatası için veri yolu yok). B6 yoksa P belirsiz |
| 08-14 | B3, B4, B6 | `00`; `POLICY` satırı müşteri 4 için; VSAM anahtarında `1000000004` | B3 yoksa dönüş kodu ve veri belirsiz: kesilmeyen değerin `S9(9) COMP`'a ve Db2'ye nasıl gideceği derleyiciye bağlı; B6 yoksa numara belirsiz; B4 eki için tablonun altındaki not |
| 08-15 | B8 | Düzenekte ABEND `LGCA` mesajı ve `GOBACK`; dönüş kodu yok, LGAPDB01 çağrılmaz, tek LGSTSQ LINK | B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır. Yan etki aynı: LGSTSQ LINK'i ABEND'den önce yapılır (`lgapol01.cbl:100-101`) |
| 08-17 | B1 | Tamponun 7-8. baytları `98` | Dönüş kodunun nereye düştüğü belirsiz; `EIBCALEN = 7` için 7. baytın `9` olması yine kesin |
| 08-18 | B1, B4, B6 | `00`; `EIBCALEN` ötesindeki metin alanları boşluk; numara `0001000001` | B1 yoksa dönüş kodu ve veri belirsiz (COMMAREA dışından okuma ortama bağlı); B6 yoksa numara belirsiz; B4 eki için tablonun altındaki not. Zincirde `98` dönmemesi yine kesin ([CICS bilgisi]) |
| 08-21 | B2, B8 | `EM-SQLRC = +00000`; düzenekte ABEND `LGCA` mesajı ve `GOBACK` | B2 yoksa `EM-SQLRC` belirsiz. B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır; mesaj ABEND'den önce yazılır (`lgapdb01.cbl:167-168`) |
| 08-23 | B1, B4, B6 | `00`; 08-01 ile aynı yan etkiler | B1 yoksa belirsiz: `LENGTH(32500)` ile 158 baytlık alandan yapılan LINK'in ne okuyacağı ortama bağlı; B6 yoksa numara belirsiz; B4 eki için tablonun altındaki not |
| 08-26 | B4 | WRITE `NORMAL`; `55` değişmez, kayıt yazılır | **Düzenekte:** senaryo `NORMAL` dışında bir RESP verirse `80` ve iki mesaj (`ELSE` dalı; 08-09 biçimi). **Gerçek VSAM'da:** anahtar örnek veride yok (`ksdspoly.txt:1-10`); sonuç VSAM'ın RESP'ine bağlı, `NORMAL` dışında aynı `80` yolu |
| 08-27 | B4 | Girdi düzenekle kurulur; `80`, `< 91` dalı | **Düzenekte:** senaryo RESP vermezse WRITE `NORMAL` döner: `X` anahtarlı kayıt yazılır, dönüş kodu girdideki gibi kalır. **Gerçek VSAM'da:** girdi kurulamaz (`X` anahtarı dosyada yok, yazma hatası için veri yolu yok) |
| 08-28 | B4 | `90` | **Düzenekte:** senaryo SQLCODE vermezse 08-29 yolu (`00`). **Gerçek Db2'de:** boş tarihin reddedilip reddedilmediği belirsiz (08-12) |
| 08-29 | B4, B6 | `00`; 08-01 ile aynı akış | **Düzenekte:** senaryo `0` dışında bir SQLCODE verirse 08-28 yolu (`90`). **Gerçek Db2'de:** davranış belirsiz (08-12). B6 yoksa numara belirsiz; B4 eki için tablonun altındaki not |

Not (B4'teki host değişkeni eki): **düzenekte** ek sağlanmazsa `DB2-POLICYNUM-INT` başlangıçtaki 0 değerinde kalır
(`lgapdb01.cbl:117`, `:159`), `CA-POLICY-NUM = 0` olur (`:311`) ve numarayı kullanan sonraki adımlar Kod gözlemleri 7'deki uç
durumdaki gibi 0 ile çalışır; `CA-LASTCHANGED` girdideki gibi kalır. **Gerçek Db2'de** numarayı `IDENTITY_VAL_LOCAL()`, zaman
damgasını `SELECT LASTCHANGED` verir (B6, Kod gözlemleri 16). Dönüş kodu tahminleri değişmez: üç programda bu iki alanın değerine bakan bir koşul yok,
yalnızca taşınırlar (`lgapdb01.cbl:311-320`, `lgapvs01.cbl:100`, `:166`). Geçerli olduğu satırlar: 08-01, 08-02, 08-03, 08-04, 08-08,
08-09, 08-14, 08-18, 08-23, 08-29.

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 08-08 | Beklenen veri: `POLICY` satırının geri alınması; gerçek CICS'te çağıranın `90`'ı görmesi | ABEND sonrası iş biriminin geri alınması CICS/Db2'ye bağlı (C); `lgapdb01.cbl:430` yalnızca yorum. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır; çağıranın `90`'ı görmesi C. Düzenekte B8 ile `90` görünür. |
| 08-09 | Beklenen veri: `POLICY` ve `HOUSE` satırlarının kalıcılığı; kuyruk kaydının son 11 baytı | Kod satırları geri almaz; görev sonunda kalıcı olmaları CICS'in görev sonu davranışına bağlı (C). Son 11 bayt `WRITE-MSG`'nin dışından okunur (Kod gözlemleri 13). `80` kesin. |
| 08-12 | Dönüş kodu, beklenen veri (gerçek Db2) | Boşluktan oluşan bir değerin `DATE` sütununa yazılıp yazılamayacağı ve yazılırsa ne saklanacağı Db2'ye bağlı (C). COBOL'da tarih doğrulaması yok. Düzenekte iki koşul: 08-28 (`90`), 08-29 (`00`; `issueDate` içeriği yine C). |
| 08-13 | Dönüş kodu, beklenen veri | Sayısal olmayan DISPLAY içeriğinin `COMP` alana taşınmasının sonucu derleyiciye bağlı (C). |
| 08-19 | Dönüş kodu, beklenen veri | INSERT'e giden tarih ve sayısal alanların bir kısmı COMMAREA'nın dışında; B1 ile bile boşluk içerikleri 08-12 ve 08-13'teki belirsizliğe düşer. Kesin kısım ([CICS bilgisi]): `98` dönmez. |
| 08-20 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil (C). |
| 08-30 | Dönüş kodu, beklenen veri | `CA-CUSTOMER-NUM` uzunluk kontrolünden önce kısmen ya da tamamen COMMAREA'nın dışından okunur (`lgapdb01.cbl:176`, `:179`); B1 ile bile boşluklu sayısal alanın `COMP`'a taşınması derleyiciye bağlı (C; 08-13). Taşıma hata vermezse `98`. |
| 08-25 | Dönüş kodu, beklenen veri | LGAPVS01'de `EIBCALEN` kontrolü yok. COMMAREA olmadan `CA-Request-ID`'ye erişimin sonucu kodda tanımlı değil. |
| 08-01, 08-02, 08-03, 08-04, 08-08, 08-09, 08-14, 08-18, 08-23, 08-29 | Beklenen veri: yalnızca `CA-LASTCHANGED`'in karakter biçimi (26 bayt uzunluğu kesin) | Db2 `TIMESTAMP` değerinin 26 baytlık karaktere dönüşüm biçimi Db2'ye bağlı (C; Kod gözlemleri 16). Bu satırların diğer hücreleri kesin ya da ön koşulludur. |
| 08-05, 08-06, 08-07, 08-08, 08-09, 08-27, 08-28 | Yan etki: `CA-ERROR-MSG` kuyruk kaydının son 9 baytı | 104 baytlık kaydın son 9 baytı `WRITE-MSG`'nin dışından okunur; içeriği WORKING-STORAGE yerleşimine bağlı (Kod gözlemleri 13). Mesajın kendisi kesindir. |

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
   değişkenlerine ne koyacağı açık sorudur ve bu kuralla çelişir (`:135-138`). Bu dosyada B4'teki ek `IDENTITY_VAL_LOCAL()` ve
   `SELECT LASTCHANGED` host değişkenlerini doldurur ve şu satırlar ona dayanır: 08-01, 08-02, 08-03, 08-04, 08-08, 08-09, 08-14,
   08-18, 08-23, 08-29. Ek kabul edilmezse bu satırlarda "Ön koşullu satırlar" tablosunun altındaki not geçerli olur.
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
