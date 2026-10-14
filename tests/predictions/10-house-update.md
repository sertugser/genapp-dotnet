# 10 — Konut poliçesi güncelle (Update House Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Konut poliçesi güncelle: müşteri ve poliçe numarasıyla `POLICY` satırı bir cursor ile kilitlenerek okunur; COMMAREA'daki `CA-LASTCHANGED` Db2'deki değerle aynıysa `HOUSE` satırı ve `POLICY` satırı (ödeme hariç) güncellenir; ardından VSAM `KSDSPOLY` kaydı yeniden yazılır. Poliçe türü COMMAREA'daki istek kodundan (`01UHOU`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGUPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGUPDB01` (cursor + Db2 UPDATE) → `LINK` → `LGUPVS01` (VSAM READ UPDATE + REWRITE). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. Programlar endowment, konut ve motor türlerine hizmet eder (ticari güncelleme kodu yok); yalnızca konut (`01UHOU`) kolları ve ortak kısımlar tahmin edildi, diğer kollar "başka tür, dal alınmaz" diye işaretlendi. |
| Okunan COBOL dosyaları | `base/src/lgupol01.cbl` (1-201), `base/src/lgupdb01.cbl` (1-535), `base/src/lgupvs01.cbl` (1-206), `base/src/lgstsq.cbl` (1-126), `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy`. Karşılaştırma için yalnızca `lgipdb01.cbl:459`, `:503` ve `lgapdb01.cbl:316-321` (`LASTCHANGED`'in kaynağı). |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` ve `HOUSE` tanımları (`:161-185`, `:240-256`), müşteri numaraları 1-10 (`:437` … `:743`), poliçe 6, 7, 8 ve 1 (`:861-882`, `:885-906`, `:909-930`, `:933-954`), `HOUSE` 6, 7, 8 (`:1042-1091`). `base/data/ksdspoly.txt` (`:1-10`) ve `base/cntl/adef121.jcl` (`:45`, `:49-50`): `KSDSPOLY` başlangıç verisi ve tanımı. `base/cntl/cobol.jcl` (`:44-50`): derleme seçenekleri (B3). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `docs/api-design.md` (`:23`, `:125-129`, `:244-250`): **güncelleme bölümü okundu, tahminler koddan çıkarıldı**; belgeyle kod arasındaki farklar Kod gözlemleri 18'de. `docs/adr/0004-legacy-test-harness.md` (`:44-52`, `:53-60`, `:96-114`, `:135-138`): yalnızca B4, B8, C grubu, [CICS bilgisi], Kod gözlemleri 5, 16 ve 20'deki düzenek notları ile Açık sorular için. **Kodla doğrulanmadı.** |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-14 |
| Kaynak commit (`git rev-parse HEAD`) | `4a1893e06b85c6ffd2c015c90343d54d62b94b58` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge, kodla doğrulanmadı]**: `base/` ya da `docs/` altındaki açıklama metinleri. Belgeye dayanan bir ifade kodda
  doğrulanmamıştır; hiçbir tahmin belgeye dayanmaz.
- **[Db2 bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen Db2 davranışı (`11-house-delete.md` ile aynı etiket). Bu dosyada:
  satır bulamayan FETCH (10-05, 10-06) ve satır güncellemeyen `UPDATE HOUSE` (10-07) `SQLCODE = 100` döner.
- **[CICS bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen CICS davranışı. Bu dosyada:
  (1) `LINK … LENGTH(n)` ile çağrılan program `EIBCALEN = n` görür; bu yüzden zincirdeki alt programın uzunluk kontrolü en dıştaki
  çağıranın uzunluğuna bakmaz ("zincirde `98` dönmez"); (2) `EXEC CICS ABEND` görevi sonlandırır ve `HANDLE ABEND` olmadığı için
  denetim çağırana dönmez ("görev sonlanır"). (3) `EXEC CICS SYNCPOINT ROLLBACK` o ana kadarki iş birimindeki Db2 değişikliklerini
  geri alır (10-10). Düzenek bu davranışları ölçmez ([belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:107-109`);
  düzenekteki karşılıkları B1 (LINK) ve B8 (ABEND).
- **C (belirsizlik grubu)**: sonucu kodda tanımlı olmayan, çalışma ortamına bağlı davranış: gerçek CICS (ABEND sonrası iş biriminin
  geri alınması, görev sonu, LINK hatası), gerçek Db2 (iş birimi, tarih ve zaman damgası biçimi, NULL, kodda yazmayan hata kodları),
  gerçek VSAM ya da derleyici (sayısal olmayan içeriğin `COMP` alana taşınması). "(C)" işaretli hücre **belirsiz**dir: ne ön koşul
  koduyla ne [Db2 bilgisi] ya da [CICS bilgisi] ile kesinleştirilmiştir ve düzenekte ölçülmez. [belge, kodla doğrulanmadı]
  `docs/adr/0004-legacy-test-harness.md:96-102`, `:107-114`.
- Etiketsiz kaynaklar [kod]'dur.

Ön koşul kodları test düzeneğinin vereceği kararlardır (kodlar `01-customer-add.md`, `02-customer-inquire.md`,
`08-house-add.md` ve `09-house-inquire.md` ile aynıdır). Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul
sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması) kendisi kurar. **Aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir; satırda yalnızca düzeneğin değiştirdiği değer yazılır. **Bu dosyada ayrıca:** satır varken FETCH `SQLCODE = 0` döner ve düzenek `DB2-LASTCHANGED`'i poliçenin Db2'deki `lastChanged` değeriyle (Db2'nin 26 baytlık karakter biçiminde) kurar (Kod gözlemleri 5). Kaydı olmayan bir anahtarla yapılan VSAM READ'de düzenek `NORMAL` dışında bir RESP döndürür. Düzenek host değişkenlerine yalnızca satırda yazılan değeri koyar: FETCH'te `DB2-LASTCHANGED`, başarılı `SELECT LASTCHANGED`'de yeni zaman damgası; hata dönen bir deyimde hiçbir şey. |
| B8 | Düzenekte `EXEC CICS ABEND` bir mesaj ve `GOBACK` ile değiştirilir: abend eden program çağırana döner ve COMMAREA'ya en son yazılan dönüş kodu görünür. [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:44-46`. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır; orada çağıranın kodu görmesi belirsizdir (C). Bu dosyada: `LGV3` / `LGV4` satırları (10-05, 10-06, 10-16, 10-17, 10-19, 10-20, 10-21, 10-33) ve `LGCA` satırları (10-23, 10-29; COMMAREA olmadığı için dönüş kodu görünmez, düzenekte görülen ABEND mesajı ve `GOBACK`'tir). |

(B5, B6 ve B7 bu dosyada kullanılmıyor.) **Sıra** ön koşulu: satırda adı geçen test önce çalıştırılmış ve beklenen sonucu
vermiştir; arada aynı poliçeye dokunan başka bir test yoktur. Sıra bağımlılıkları: "Test sırası ve veri bağımlılıkları" bölümü.

## Kod gözlemleri

1. **COMMAREA bayt haritası, konut görünümü:** `08-house-add.md` Kod gözlemleri 1 ile aynı (`lgcmarea.cpy:10-13`, `:34-44`,
   `:56-63`). Üç program da bu düzeni kullanır (`lgupol01.cbl:73-74`, `lgupdb01.cbl:150-153`, `lgupvs01.cbl:89-90`).
   - **Okunan baytlar:** 1-6 istek kodu (`lgupol01.cbl:113`, `lgupdb01.cbl:283`; LGUPVS01 yalnızca 4. baytı: `lgupvs01.cbl:102`),
     9-28 müşteri ve poliçe numarası (`lgupdb01.cbl:195-196`, `lgupvs01.cbl:103-104`), 29-100 ortak alanlar
     (`lgupdb01.cbl:278`, `:313-314`, `:320-324`), 101-158 konut alanları (`lgupdb01.cbl:427-428`, `:434-439`,
     `lgupvs01.cbl:121-125`).
   - **Yazılan baytlar:** 7-8 `CA-RETURN-CODE`; yalnızca başarı yolunda 49-74 `CA-LASTCHANGED` (`lgupdb01.cbl:331`).
2. **Çağrı zinciri ve uzunluk.**
   - LGUPOL01 konut için 28 (`lgupol01.cbl:62`) + 130 (`lgupol01.cbl:40`; programın kendi alanı, copybook'tan değil) = **158**
     bayt ister (`lgupol01.cbl:124-126`). Kısaysa `98` ve RETURN (`:127-128`).
   - `WHEN OTHER` yolunda uzunluk kontrolü yok (`lgupol01.cbl:139-140`; Kod gözlemleri 3).
   - LGUPOL01, LGUPDB01'i `LENGTH(32500)` ile çağırır (`lgupol01.cbl:157-160`): [CICS bilgisi] LGUPDB01 her zaman `EIBCALEN = 32500` görür.
   - **LGUPDB01'de uzunluk ve istek kodu kontrolü yok.** `WS-CA-HEADER-LEN` ve `WS-REQUIRED-CA-LEN` tanımlı
     (`lgupdb01.cbl:64-65`) ama hiçbir satırda kullanılmıyor; `:201-205` yalnızca yorum. `EIBCALEN = 0` kontrolüne
     (`lgupdb01.cbl:183-187`) zincirden ulaşılamaz.
   - LGUPDB01, LGUPVS01'i `LENGTH(225)` ile çağırır (`lgupdb01.cbl:209-212`); [CICS bilgisi]
     LGUPVS01 `EIBCALEN = 225` görür. LGUPVS01 `EIBCALEN`'i yalnızca READ'in uzunluğu
     olarak kullanır (`lgupvs01.cbl:100`, `:141`); uzunluk kontrolü yok.
3. **`lgupol01.cbl:139-143`: `WHEN OTHER` sonrası akış ve `99`'un ezilmesi.**
   - `:113` `EVALUATE CA-REQUEST-ID` büyük/küçük harfe duyarlıdır. Tanınmayan kodda `:140` `'99'` yazılır; **RETURN yok.**
   - `:141` `END-EVALUATE` → `:143` `PERFORM UPDATE-POLICY-DB2-INFO` → `:157-160` LINK LGUPDB01.
   - LGUPDB01 `lgupdb01.cbl:190` `MOVE '00' TO CA-RETURN-CODE`. Böylece **`99` zincirde hiçbir zaman çağırana ulaşmaz.**
   - LGUPDB01 cursor'u açar ve FETCH eder (`lgupdb01.cbl:254-273`). `:283-300` `EVALUATE`'inde `WHEN OTHER` yoktur; hiçbir kol
     çalışmaz ve `CA-RETURN-CODE` `'00'` kalır. Zaman damgası eşleşirse `:302` geçer ve `UPDATE POLICY` çalışır (`:318-326`):
     **ortak poliçe alanları güncellenir, `HOUSE` güncellenmez** (10-19).
   - LGUPVS01'de 4. karakter `'H'` değilse `When Other` veri alanını boşlukla doldurur (`lgupvs01.cbl:133-134`). Anahtar
     `KSDSPOLY`'de olmadığı için READ başarısız olur → `81` + ABEND `LGV3` (10-19, 10-20).
   - Ticari güncelleme için istek kodu tanımlı değildir (`lgupol01.cbl:113-141`, `lgupdb01.cbl:283-300`); `01UCOM` da `WHEN OTHER`'a
     düşer. Başka tür: tahmin yazılmadı.
4. **`policyType` okunmaz; `UPPER-CASE` ve `70` yok.** Üç programda da `POLICYTYPE`, `UPPER-CASE`, `LOWER-CASE` ya da `'70'`
   geçmez (büyük/küçük harf duyarsız grep, sonuç boş). Cursor yalnızca müşteri ve poliçe numarasıyla seçer
   (`lgupdb01.cbl:135-137`); `UPDATE POLICY` `POLICYTYPE`'a dokunmaz (`:319-325`). Poliçe 7 (`policyType = 'C'`,
   `db2cre.jcl:901`) `01UHOU` ile güncellenir ve türü `'C'` kalır (10-02). Sorgulamadaki `UPPER-CASE` (`lgipdb01.cbl:275`)
   güncellemede yok: `'01uhou'` tanınmaz (10-19).
5. **Cursor, FETCH ve 5/6 uyumsuzluğu.**
   - `POLICY_CURSOR` `WITH HOLD`, 5 sütun seçer (`ISSUEDATE`, `EXPIRYDATE`, `LASTCHANGED`, `BROKERID`, `BROKERSREFERENCE`),
     `FOR UPDATE OF` aynı 5 sütun (`lgupdb01.cbl:128-143`).
   - `FETCH-DB2-POLICY-ROW` **6** host değişkenine okur: fazladan `:DB2-PAYMENT-INT INDICATOR :IND-PAYMENT`
     (`lgupdb01.cbl:232-237`). Sütun ile host değişkeni sayısının farklı olmasının SQLCODE'a etkisi Db2'ye bağlıdır (C). FETCH
     satır varken `0` dönmezse başarı yolu hiç çalışmaz: `90` (`:354`) yazılır, CLOSE onu ezer (Kod gözlemleri 7) ve VSAM yine de
     güncellenir (10-13 ile aynı sonuç).
   - Bu dosyada FETCH'in başarılı sayıldığı bütün yollar B4 ile ön koşulludur: düzenek FETCH'i `0` döndürür ve `DB2-LASTCHANGED`'i
     Db2'deki değerle kurar.
   - [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:53-58`: GnuCOBOL düzeneğinde Db2 stub'ı yalnızca SQLCODE'u
     doldurur; değeri test senaryosu seçer. Bu durumda `DB2-LASTCHANGED`, `INITIALIZE DB2-POLICY` (`lgupdb01.cbl:176`) ile
     boşluk kalır ve zaman damgası karşılaştırması (`:278`) girdiye göre değişir. Düzeneğin bu alanı kurması gerekir (B4).
6. **`LASTCHANGED` akışı ve iyimser kilitlemenin etkisizliği.**
   - Girdi: `CA-LASTCHANGED` (X(26), `lgcmarea.cpy:40`). Doğru değer sorgulamanın ya da eklemenin döndürdüğü değerdir:
     `lgipdb01.cbl:459`, `:503` ve `lgapdb01.cbl:316-321` aynı sütunu aynı türe okur.
   - FETCH `DB2-LASTCHANGED`'e okur (`lgupdb01.cbl:234`; alan `lgpolicy.cpy:48`). `lgupdb01.cbl:278` 26 baytı birebir karşılaştırır.
   - Eşitse: konut `UPDATE`'i, ardından `UPDATE POLICY` `LASTCHANGED = CURRENT TIMESTAMP` (`:322`), ardından
     `SELECT LASTCHANGED INTO :CA-LASTCHANGED` (`:329-334`): yeni değer COMMAREA'ya döner.
   - Farklıysa: `:346` `'02'`. Db2'de hiçbir şey güncellenmez. Ama `:360` CLOSE `'02'`'yi `'00'` ile ezer (`:371`) ve `:209`
     LGUPVS01'i çağırır: **VSAM kaydı COMMAREA'daki konut verileriyle yeniden yazılır** (10-03). Çağıran `00` görür.
   - Sonuç: eşzamanlı bir güncelleme Db2'de engellenir ama çağırana bildirilmez; tek iz `CA-LASTCHANGED`'in girdideki gibi
     kalmasıdır. VSAM ile Db2 birbirinden ayrılır (10-04).
   - Düzenekte `DB2-LASTCHANGED` boş kalırsa (Kod gözlemleri 5), boşluklu `CA-LASTCHANGED` eşit sayılır; gerçek Db2'de
     `lastChanged` `NOT NULL`'dur (`db2cre.jcl:168`) ve hiçbir zaman boşluk olamaz.
7. **`lgupdb01.cbl:369-371`: `CLOSE-PCURSOR` önceki dönüş kodunu ezer.** `When 0` → `'00'` (`:370-371`). `When -501` → `'00'`,
   `EM-SQLREQ = '-501 detected c'` (mesaj yazılmaz) ve RETURN (`:372-375`). `When Other` → `'90'`, mesaj, RETURN (`:376-379`).

   | Önceki kod | Yazıldığı satır | CLOSE'a geliş | CLOSE `0` sonrası | CLOSE `-501` sonrası |
   |---|---|---|---|---|
   | `00` (başarı) | `lgupdb01.cbl:261` | `:360` | `00`, LGUPVS01 çağrılır (`:209`) | `00`, RETURN (`:375`), LGUPVS01 yok |
   | `02` | `lgupdb01.cbl:346` | `:360` | `00`, LGUPVS01 çağrılır | `00`, RETURN |
   | `01` (FETCH `100`) | `lgupdb01.cbl:352` | `:360` | `00`, LGUPVS01 çağrılır | `00`, RETURN |
   | `90` (FETCH hatası) | `lgupdb01.cbl:354` | `:360` | `00`, LGUPVS01 çağrılır | `00`, RETURN |
   | `90` (SELECT hatası, ROLLBACK) | `lgupdb01.cbl:339` | `:360` | `00`, LGUPVS01 çağrılır | `00`, RETURN |
   | `01` (`HOUSE` UPDATE `100`) | `lgupdb01.cbl:447` | `:305` | `00`, RETURN (`:306`) | `00`, RETURN |
   | `90` (`HOUSE` UPDATE hatası) | `lgupdb01.cbl:449` | `:305` | `00`, RETURN (`:306`) | `00`, RETURN |

   CLOSE `When Other` ise bütün satırlarda `90` ve RETURN olur; çağırana ulaşan tek `90` (OPEN hatası dışında) budur.
   `:360` CLOSE'dan sonra `UPDATE-POLICY-DB2-INFO` paragrafı biter (bir sonraki paragraf `:362`) ve akış `:207` → `:209`'a döner.
8. **`UPDATE POLICY` kontrol edilmez; tek `ROLLBACK` SELECT hatasındadır.**
   - `UPDATE POLICY … WHERE CURRENT OF POLICY_CURSOR` (`lgupdb01.cbl:318-326`) sonrasında SQLCODE kontrolü yok. `:336`'daki
     `IF SQLCODE NOT EQUAL 0` ondan sonra gelen `SELECT LASTCHANGED`'e (`:329-334`) aittir; `:337`'deki yorum ("from Update")
     yanlıştır.
   - `UPDATE POLICY` başarısız ama SELECT başarılı olursa `00`; `HOUSE` güncellenmiş, `POLICY` güncellenmemiş kalır (10-09).
   - SELECT başarısız olursa `EXEC CICS SYNCPOINT ROLLBACK` (`:338`; [CICS bilgisi] iş birimindeki Db2 değişikliklerini geri alır), `'90'`, mesaj (`EM-SQLREQ` hâlâ `' UPDATE POLICY  '`, `:317`).
     RETURN yoktur; akış `:360` CLOSE'a gider (10-10). Bu, üç programdaki **tek** geri alma deyimidir (grep:
     `SYNCPOINT|ROLLBACK|HANDLE|COMMIT` yalnızca `lgupdb01.cbl:338`).
   - Geri almadan sonra `WITH HOLD` cursor'un CLOSE'ta hangi SQLCODE'u döndüreceği Db2'ye bağlıdır (C). `0` ise `00` + VSAM yazılır;
     `-501` ise `00` + RETURN, VSAM yazılmaz; başka bir değer ise `90`.
9. **Ödeme güncellenmez.** `CA-PAYMENT` tamsayıya çevrilir (`lgupdb01.cbl:314`) ama `UPDATE POLICY`'nin `SET` listesinde `PAYMENT`
   yok (`:320-324`); cursor'un `FOR UPDATE OF` listesinde de yok (`:138-142`). `:127`'deki yorum ("will NOT update payment or
   commission fields") kodla uyumludur. `DB2-PAYMENT-INT` başka hiçbir satırda kullanılmaz (10-18).
10. **Konut UPDATE'i** (`lgupdb01.cbl:424-454`).
    - `CA-H-BEDROOMS` 9(3) → `S9(4) COMP`, `CA-H-VALUE` 9(8) → `S9(9) COMP` (`:427-428`; alanlar `:93-94`). Sayısal olmayan içerikte
      sonuç derleyiciye bağlı (10-22).
    - `UPDATE HOUSE SET` altı sütun, `WHERE POLICYNUMBER = :DB2-POLICYNUM-INT` (`:431-442`); müşteri koşulu yok, çünkü cursor müşteri
      ve poliçe numarasını zaten doğrulamıştır (`:136-137`).
    - `SQLCODE = 100` → `'01'` (`:446-447`); başka bir hata → `'90'` ve mesaj (`:448-451`). İki durumda da `:302-307` CLOSE ve RETURN
      yapar: `UPDATE POLICY` ve LGUPVS01 çalışmaz; CLOSE kodu `00` ile ezer (10-07, 10-08).
11. **LGUPVS01'in VSAM güncellemesi** (`lgupvs01.cbl:97-172`).
    - Anahtar: `CA-Request-ID(4:1)` + müşteri + poliçe (`:102-104`; `:137` poliçe numarasını tekrar taşır). Konut kaydının düzeni
      eklemeyle aynı (`:42-47`): ev adının yalnızca ilk 9 baytı, ev numarası yok (`08-house-add.md` Kod gözlemleri 10).
    - `When Other` (`lgupvs01.cbl:133-134`) eklemenin tersine zincirden ulaşılabilir: LGUPOL01'in `WHEN OTHER` yolu LGUPDB01'i, o da LGUPVS01'i
      çağırır (Kod gözlemleri 3).
    - `READ … INTO(WS-FileIn) LENGTH(WS-Commarea-Len) RIDFLD(WF-Policy-Key) KEYLENGTH(21) RESP(WS-RESP) UPDATE` (`:139-146`).
      `WS-Commarea-Len` zincirde 225'tir (`:100`). Okunan kaydın içeriği kullanılmaz: `REWRITE FROM(WF-Policy-Info)` 64 bayt
      (`:155-159`, uzunluk `:24`). Bu yüzden `ksdspoly.txt`'teki oda alanı tutarsızlıkları (`08-house-add.md` Kod gözlemleri 15)
      güncellemeyi etkilemez.
    - READ hatası → `RESP2`, `'81'`, mesaj, ABEND `LGV3` (`lgupvs01.cbl:147-153`); REWRITE hatası → `'82'`, mesaj, ABEND `LGV4` (`:160-166`).
      İkisinde de RETURN (`:152`, `:165`) ABEND'den sonra geldiği için ulaşılamaz.
    - LGUPVS01 başarı yolunda `CA-RETURN-CODE`'a yazmaz (`:170-172`); zincirde değer LGUPDB01'in `00`'ı kalır, doğrudan çağrıda
      girdideki gibi kalır (10-32).
12. **Geri alma ve ABEND sonrası durum.** Tek açık geri alma `lgupdb01.cbl:338`'dir (Kod gözlemleri 8). `HANDLE ABEND` yok.
    ABEND `LGV3` ya da `LGV4` (`lgupvs01.cbl:151`, `:164`) geldiğinde `HOUSE` ve `POLICY` UPDATE'leri zaten yapılmıştır; geri
    alınıp alınmadıkları CICS/Db2 iş birimi davranışına bağlıdır (C). [kod] `KSDSPOLY` kurtarılabilir tanımlı: `LOG(UNDO)`
    (`adef121.jcl:45`).
    Çağıranın `81`/`82`'yi görmesi: düzenekte B8 ile ABEND bir `GOBACK`'tir, LGUPVS01 LGUPDB01'e döner, akış `lgupdb01.cbl:216` ve
    `lgupol01.cbl:147` RETURN'lerinden geçer ve kod görünür. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır; çağıranın kodu
    görmesi C.
13. **Dönüş kodları ve anlamları.**

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgupol01.cbl:105`, `lgupdb01.cbl:190`, `:261`, `:371`, `:373` | Başlangıç / OPEN başarılı / CLOSE başarılı / CLOSE `-501` | Evet |
    | `98` | `lgupol01.cbl:127` | COMMAREA 158 bayttan kısa | Evet |
    | `99` | `lgupol01.cbl:140` | Bilinmeyen istek kodu | **Hayır**: `lgupdb01.cbl:190` her zaman ezer |
    | `02` | `lgupdb01.cbl:346` | `LASTCHANGED` uyuşmuyor | **Hayır**: CLOSE ezer (Kod gözlemleri 7) |
    | `01` | `lgupdb01.cbl:352` | FETCH `SQLCODE = 100` | **Hayır**: CLOSE ezer; ardından LGUPVS01 |
    | `01` | `lgupdb01.cbl:447` | `HOUSE` UPDATE `SQLCODE = 100` | **Hayır**: CLOSE ezer |
    | `90` | `lgupdb01.cbl:263`, `:267` | OPEN hatası (`-913` ya da başka) | Evet |
    | `90` | `lgupdb01.cbl:339` | SELECT hatası (ROLLBACK ile) | CLOSE `0`/`-501` ise **hayır** (C) |
    | `90` | `lgupdb01.cbl:354` | FETCH hatası | **Hayır**: CLOSE ezer |
    | `90` | `lgupdb01.cbl:449` | `HOUSE` UPDATE hatası | **Hayır**: CLOSE ezer |
    | `90` | `lgupdb01.cbl:377` | CLOSE hatası | Evet |
    | `81` | `lgupvs01.cbl:149` | VSAM READ hatası | Alana yazılır, hemen ardından ABEND `LGV3` (düzenekte B8 ile görünür) |
    | `82` | `lgupvs01.cbl:162` | VSAM REWRITE hatası | Alana yazılır, hemen ardından ABEND `LGV4` (düzenekte B8 ile görünür) |
    | ABEND `LGCA` | `lgupol01.cbl:102`, `lgupdb01.cbl:186` | COMMAREA yok | — |
    | ABEND `LGV3` / `LGV4` | `lgupvs01.cbl:151`, `:164` | VSAM READ / REWRITE hatası | — |
    | `98`, `01`, `90` | `lgupol01.cbl:119`, `:135`, `lgupdb01.cbl:411`, `:413`, `:488`, `:490` | Başka tür kolları | Başka tür: tahmin yazılmadı |

14. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGUPOL01 **45** bayt (`lgupol01.cbl:44-49`), LGUPDB01 **87** (`lgupdb01.cbl:39-51`), LGUPVS01 **101**
      (`lgupvs01.cbl:61-76`). `CA-ERROR-MSG` üçünde de 99 bayt (`lgupol01.cbl:51-53`, `lgupdb01.cbl:53-55`, `lgupvs01.cbl:78-80`).
    - LGUPDB01: `EM-CUSNUM` ve `EM-POLNUM` baştan dolar (`lgupdb01.cbl:198-199`). `EM-SQLREQ` adımlara göre: `' OPEN   PCURSOR '`
      (`:254`), `' FETCH  ROW   '` (`:229`), `' UPDATE POLICY  '` (`:317`; SELECT hatasında da bu kalır), `' CLOSE  PCURSOR'`
      (`:364`), `' UPDATE HOUSE '` (`:430`). `EM-SQLRC` = SQLCODE (`:504`).
    - LGUPVS01: `EM-POLNUM` **hiçbir satırda doldurulmaz** (yalnızca `lgupvs01.cbl:68`'de tanımlı): `' PNUM='` + 10 boşluk.
      `EM-CUSNUM` (`:184`), RESP ve RESP2 (`:185-186`). Metin READ ve REWRITE hatalarında aynı: `' Re-write  KSDSPOLY '` (`:71-72`).
    - `EIBCALEN = 0` mesajları: LGUPOL01'de `' NO COMMAREA RECEIVED'` 21 baytlık `EM-VARIABLE`'a tam sığar (`lgupol01.cbl:49`, `:100`).
      LGUPDB01'de 63 baytlık gruba taşınır (`lgupdb01.cbl:184`), `EM-SQLRC` son 6 baytı ezer (`:504`): metin + 36 boşluk + 6 bayt.
    - `WRITE-ERROR-MESSAGE` içindeki `EIBCALEN` dalları:
      - LGUPOL01'de yordam yalnızca `lgupol01.cbl:101`'den çağrılır; dış `IF` (`:186`) her zaman yanlış, `:187-199` **ölü kod**.
      - [CICS bilgisi] LGUPDB01'de zincirde hep `ELSE` (`lgupdb01.cbl:527-532`); `< 91` dalı (`:521-526`) doğrudan çağrıda ulaşılabilir (10-30).
      - [CICS bilgisi] LGUPVS01'de zincirde `EIBCALEN = 225` olduğundan `ELSE` (`lgupvs01.cbl:198-203`); `< 91` (`:192-197`) yalnızca doğrudan
        çağrıda (10-33).
15. **LGSTSQ kuyruk kaydı.** `08-house-add.md` Kod gözlemleri 13 ile aynı kurallar (`lgstsq.cbl:25-28`, `:48`, `:68-71`, `:82-83`,
    `:90`, `:94-111`). Uzunluklar: 45 → 50, 87 → 92 (eksiksiz); 99 → 104: COMMAREA'nın yalnızca ilk **81** baytı girer, son 9 bayt
    `WRITE-MSG`'nin dışından okunur. LGUPVS01'in 101 baytlık mesajı → 106: kayda `' RESP='` + RESP + `' R'` girer, **RESP2 değeri
    kayda girmez**; son 11 bayt `WRITE-MSG`'nin dışından okunur.
16. **WORKING-STORAGE kalıcılığı.**
    - LGUPOL01'in `WS-REQUIRED-CA-LEN` alanı `VALUE +0` ile başlar (`lgupol01.cbl:63`) ve her konut çağrısında 158 artar
      (`:124-125`). Kodda sıfırlama yok. Korunmuş WORKING-STORAGE'da ikinci çağrının eşiği 316 olur.
    - LGUPDB01'in `EIBCALEN = 0` dalı `EM-VARIABLE` sabitlerini ezer (Kod gözlemleri 14) ve dal ABEND ile biter (`lgupdb01.cbl:186`).
    - Bu dosyadaki tahminler WORKING-STORAGE'ın `VALUE` değerleriyle başladığı duruma göre yazıldı. [belge, kodla doğrulanmadı]
      `docs/adr/0004-legacy-test-harness.md:50-52`: düzenekte her durum ayrı bir süreçte çalışır, çünkü CICS her görev için
      WORKING-STORAGE'ı yeniden kurar.
17. **Kullanılmayan alanlar.** LGUPDB01: `WS-RETRY` (`lgupdb01.cbl:31`; yalnızca `:173`'te boşluk yazılır), `WS-VARY-FIELD`
    (`:69-71`), `WS-CA-HEADER-LEN` ve `WS-REQUIRED-CA-LEN` (`:64-65`), `DB2-PAYMENT-INT` (`:314`'te yazılır, okunmaz). LGUPVS01:
    `WS-Comm-Len`, `WS-STARTCODE`, `WS-SYSID` (`lgupvs01.cbl:20-22`).
18. **`docs/api-design.md` ile kod arasındaki farklar.** Belgenin güncelleme bölümü okundu; tahminler koddan çıkarıldı.

    | Durum | [belge, kodla doğrulanmadı] `docs/api-design.md` | [kod] |
    |---|---|---|
    | `lastChanged` uyuşmuyor | `docs/api-design.md:127`: "`02` yazılır, imleç kapatılırken `00` ile ezilir; **kayıt güncellenmez** ama çağırana `00` döner" | Ezme doğru (`lgupdb01.cbl:346`, `:360`, `:371`). Db2 güncellenmez, ama **VSAM kaydı yeniden yazılır**: `:209` LINK LGUPVS01, `lgupvs01.cbl:155-159` REWRITE (10-03). |
    | Poliçe yok | `docs/api-design.md:128`: "`01` yazılır, aynı şekilde `00` ile ezilir" | Ezme doğru (`lgupdb01.cbl:352`, `:371`). Ardından LGUPVS01 aynı anahtarla READ yapar; anahtar VSAM'da yoksa `81` + ABEND `LGV3` (`lgupvs01.cbl:147-151`). Çağıran `00` görmez (10-05). |
    | Poliçe okunurken veritabanı hatası | `docs/api-design.md:129`: "`90` yazılır, aynı şekilde `00` ile ezilir" | Ezme doğru (`lgupdb01.cbl:354`, `:371`). Ardından VSAM kaydı yeniden yazılır (`:209`; 10-13). Belgede yok. |
    | Belgede olmayan durumlar | — | `HOUSE` UPDATE'inin `01`/`90`'ı ezilir ve `00` döner (`lgupdb01.cbl:302-306`; 10-07, 10-08). SELECT hatasında ROLLBACK sonrası `00` (`:336-342`; 10-10). `99` her zaman ezilir (`:190`; 10-19, 10-20). |
    | Eşzamanlı güncelleme | `docs/api-design.md:244-250`: eski kod `02` yazar ama çağırana `00` döndürür | Kodla uyumlu; VSAM'ın yine güncellendiğini belge söylemiyor. |

19. **Test verisi.** Bu değerler yalnızca Db2 `db2cre.jcl` ile, `KSDSPOLY` `ksdspoly.txt` ile yüklendiyse ve sonradan değiştirilmediyse
    geçerlidir.
    - Konut poliçeleri: 6 (müşteri 4), 7 (müşteri 6), 8 (müşteri 9) (`db2cre.jcl:873-874`, `:897-898`, `:921-922`). VSAM kayıtları:
      `H00000000040000000006` (`ksdspoly.txt:5`), `H00000000060000000007` (`:6`), `H00000000090000000008` (`:7`).
    - Motor poliçesi 1: müşteri 2 (`db2cre.jcl:945-946`), `HOUSE` satırı yok (`HOUSE` satırları 6, 7, 8: `db2cre.jcl:1051`, `:1068`,
      `:1085`). VSAM kaydı `M00000000020000000001` (`ksdspoly.txt:8`); `H` ile başlayan anahtarı yok.
    - Poliçe 0, müşteri 0 ve müşteri 9 + poliçe 6 eşleşmesi Db2'de yok; `H00000000040000000000`, `H00000000090000000006` ve `H`
      dışında bir harfle başlayan konut anahtarları `ksdspoly.txt:1-10`'da yok.
    - Poliçe 7'nin tutarsızlığı: `09-house-inquire.md` Kod gözlemleri 5 ve 16.
20. **Düzenek notu: veriye bağlı SQLCODE.** Düzenekte Db2 stub'ı yalnızca `SQLCODE`'u doldurur; değeri Db2 değil test senaryosu seçer.
    B4'ün "aksi söylenmedikçe SQLCODE 0" kuralı altında, [Db2 bilgisi] ile yazılan ve veriye bağlı olan SQLCODE'lar düzenekte senaryo
    girdisi olarak ayrıca kurulmalıdır: bu dosyada 10-05 ve 10-06 (FETCH `100`), 10-07 (`UPDATE HOUSE` `100`). Bu değerin gerçek
    Db2'yle örtüşmesi [Db2 bilgisi] kabulüdür; düzenek bunu ölçmez. [belge, kodla doğrulanmadı]
    `docs/adr/0004-legacy-test-harness.md:56-58`, `:100-101`. Aynı not: `11-house-delete.md` Kod gözlemleri 16.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 10-01 | LGUPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01UHOU'`. `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000006` (`db2cre.jcl:873-874`). Sıra: 09-01 önce çalışmış; `CA-LASTCHANGED` = 09-01'in döndürdüğü değer. Yeni ortak alanlar poliçe 8'den, yeni konut alanları `HOUSE` 8'den (`db2cre.jcl:923-929`, `:1086-1091`; alan alan aşağıdaki **"10-01 girdi ve beklenen değerler"** tablosunda). [B4] FETCH `0` ve `DB2-LASTCHANGED` = poliçe 6'nın Db2 değeri. | `CA-RETURN-CODE = 00` | `CA-LASTCHANGED` = `UPDATE POLICY` anındaki `CURRENT TIMESTAMP`, 26 bayt (biçim belirsiz: `09-house-inquire.md` Kod gözlemleri 9). Diğer bütün alanlar girdideki gibi kalır.<br>Yan etkiler: `HOUSE` 6 ve `POLICY` 6 güncellenir (ödeme hariç), `KSDSPOLY` kaydı `H00000000040000000006` yeniden yazılır (alan alan: "10-01'den sonra Db2'de ve VSAM'da beklenen durum"). Hata mesajı yazılmaz. | `lgupol01.cbl:105`, `:113`, `:123-126`, `:143`, `:157-160`, `:147`<br>`lgupdb01.cbl:190`, `:195-196`, `:254-261`, `:273-278`, `:291-293`, `:302`, `:313`, `:318-336`, `:360`, `:369-371`, `:209-212`, `:216`, `:427-444`<br>`lgupvs01.cbl:102-104`, `:120-125`, `:139-147`, `:155-160`, `:170-172` | İş kuralı |
| 10-02 | 10-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000006`, `CA-POLICY-NUM = 0000000007` (poliçe 7: `db2cre.jcl:897-898`; `policyType = 'C'`: `:901`). Sıra: 09-03 önce çalışmış; `CA-LASTCHANGED` = 09-03'ün döndürdüğü değer. [B4] 10-01'deki gibi. | `CA-RETURN-CODE = 00` | `policyType` hiçbir satırda okunmaz (Kod gözlemleri 4). `HOUSE` 7 ve `POLICY` 7 10-01'deki değerlerle güncellenir; `POLICY.policyType` `'C'` kalır; bilinçli sapma adayı (Açık sorular 3). `KSDSPOLY` kaydı `H00000000060000000007` (`ksdspoly.txt:6`) yeniden yazılır. `CA-LASTCHANGED` yeni zaman damgası. | `lgupdb01.cbl:135-137`, `:319-325`, `:431-442`<br>`lgupvs01.cbl:102-104`, `:139-159` | İş kuralı |
| 10-03 | 10-01 ile aynı, ama `CA-LASTCHANGED` = ␣×26. Bu değer Db2'deki değerden farklıdır (`lastChanged` `TIMESTAMP NOT NULL`: `db2cre.jcl:168`). [B4] FETCH `0` ve `DB2-LASTCHANGED` = poliçe 6'nın Db2 değeri. Sıra gerekmez. | `CA-RETURN-CODE = 00` (`02` yazılır, CLOSE ezer) | Satır satır: `lgupdb01.cbl:278` eşit değil → `:346` `'02'` → `:360` CLOSE → `:371` `'00'` → `:209` LGUPVS01.<br>Db2: `POLICY` 6 ve `HOUSE` 6 **değişmez** (UPDATE çalışmaz). `CA-LASTCHANGED` girdideki gibi kalır (␣×26).<br>VSAM: `H00000000040000000006` kaydı COMMAREA'daki konut verileriyle (10-01'deki FLAT değerleri) **yeniden yazılır**; Db2 ile VSAM ayrılır.<br>Hata mesajı yazılmaz. B4 sağlanmaz ve FETCH hata verirse sonuç yine `00` olur, VSAM yine yazılır, ama `FETCH` mesajı eklenir (10-13). [belge] ile farkı: Kod gözlemleri 18. | `lgupdb01.cbl:278`, `:344-347`, `:360`, `:369-371`, `:209-212`<br>`lgupvs01.cbl:120-125`, `:139-159` | İş kuralı |
| 10-04 | Sıra: 09-01 ve 10-01 bu sırayla çalışmış ve `00` dönmüş. 10-01'in isteği tekrarlanır, ama `CA-LASTCHANGED` = **09-01'in döndürdüğü (eski) değer** ve konut alanları `HOUSE` 6'nın JCL değerleri (`HOUSE` + ␣×10, `005`, `01500000`, ␣×20, `␣␣␣5`, `SO211UP␣`: `db2cre.jcl:1052-1057`). [B4] FETCH `0` ve `DB2-LASTCHANGED` = Db2'deki değer (10-01'in yazdığı). | `CA-RETURN-CODE = 00` (`02` yazılır, CLOSE ezer) | 10-01 `lastChanged`'i değiştirdiği için eski değer eşleşmez (`lgupdb01.cbl:278`); akış 10-03 ile aynı.<br>Db2: `HOUSE` 6 ve `POLICY` 6 10-01'in değerlerinde kalır (FLAT …). `CA-LASTCHANGED` girdideki gibi kalır (eski değer).<br>VSAM: `H00000000040000000006` bu isteğin verileriyle yeniden yazılır (`HOUSE`, `005`, `01500000`, `SO211UP`). **Db2'de FLAT, VSAM'da HOUSE kalır.**<br>İyimser kilitleme Db2'yi korur ama çağırana bildirmez (Kod gözlemleri 6). | `lgupdb01.cbl:278`, `:322`, `:344-347`, `:360`, `:371`, `:209`<br>`lgupvs01.cbl:155-159` | İş kuralı |
| 10-05 | 10-01 ile aynı, ama `CA-POLICY-NUM = 0000000000` (poliçe 0 Db2'de yok). [Db2 bilgisi] FETCH satır bulamaz ve `SQLCODE = 100` döner. [B4] `H00000000040000000000` anahtarı VSAM'da yok (`ksdspoly.txt:1-10`); READ `NORMAL` dışında bir RESP döner. | [B8] `CA-RETURN-CODE = 81` (ön koşullu). `01` yazılır (`lgupdb01.cbl:352`), CLOSE `00` ile ezer (`:371`), LGUPVS01 READ hatasıyla `81` yazar (`lgupvs01.cbl:149`), ardından ABEND `LGV3` (`:151`); B8 ile düzenekte çağıran `81`'i görür. `01` çağırana hiç ulaşmaz. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | Db2'de değişiklik yok; LGUPDB01 bu yolda mesaj yazmaz.<br>Yan etki: LGUPVS01 `LGSTSQ`'ya iki LINK yapar.<br>(1) `ERROR-MSG` (101 bayt): tarih, saat, `' LGUPVS01'`, `' PNUM='` + ␣×10 (`EM-POLNUM` doldurulmaz), `' CNUM=0000000004'`, `' Re-write  KSDSPOLY '`, `' RESP='` + RESP, `' RESP2='` + RESP2. Kayda RESP2 değeri girmez (Kod gözlemleri 15).<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + LGUPVS01'e gelen COMMAREA'nın ilk 90 baytı ([CICS bilgisi] LGUPVS01 `EIBCALEN = 225` görür: `ELSE` dalı); 7-8. baytlarda `81`.<br>[belge] ile farkı: Kod gözlemleri 18. | `lgupdb01.cbl:275`, `:349-352`, `:360`, `:371`, `:209-212`<br>`lgupvs01.cbl:102-104`, `:139-151`, `:174-190`, `:198-203` | İş kuralı |
| 10-06 | 10-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000009` (müşteri 9 var: `db2cre.jcl:709`; poliçe 6 müşteri 4'ün). [Db2 bilgisi] FETCH satır bulamaz ve `SQLCODE = 100` döner: cursor müşteri ve poliçe numarasını birlikte ister (`lgupdb01.cbl:136-137`). [B4] `H00000000090000000006` anahtarı VSAM'da yok. | [B8] `CA-RETURN-CODE = 81` (ön koşullu); 10-05 ile aynı yol (`81` + ABEND `LGV3`). [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | 10-05 ile aynı; mesajda `' CNUM=0000000009'`. Poliçe 6'ya dokunulmaz. | `lgupdb01.cbl:136-137`, `:349-352`, `:371`<br>`lgupvs01.cbl:139-151` | İş kuralı |
| 10-07 | 10-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000002`, `CA-POLICY-NUM = 0000000001` (motor poliçesi 1: `db2cre.jcl:945-946`, `HOUSE` satırı yok). [B4] FETCH `0`; `CA-LASTCHANGED` ve `DB2-LASTCHANGED` poliçe 1'in Db2 değerine kurulur. [Db2 bilgisi] `UPDATE HOUSE` satır güncellemez ve `SQLCODE = 100` döner (poliçe 1'in `HOUSE` satırı yok). | `CA-RETURN-CODE = 00` (`01` yazılır, CLOSE ezer) | Satır satır: `lgupdb01.cbl:444-447` `'01'` → `:302` → `:305` CLOSE → `:371` `'00'` → `:306` RETURN.<br>`UPDATE POLICY` çalışmaz, LGUPVS01 çağrılmaz: `POLICY` 1, `MOTOR` 1 ve VSAM değişmez; `HOUSE` satırı açılmaz. `CA-LASTCHANGED` girdideki gibi kalır. Hata mesajı yazılmaz. | `lgupdb01.cbl:291-293`, `:431-447`, `:302-307`, `:369-371` | İş kuralı |
| 10-08 | 10-01 ile aynı. [B4] FETCH `0`, `CA-LASTCHANGED` = `DB2-LASTCHANGED`; `UPDATE HOUSE` `0` ve `100` dışında bir SQLCODE döner. | `CA-RETURN-CODE = 00` (`90` yazılır, CLOSE ezer) | `HOUSE` 6 ve `POLICY` 6 değişmez, LGUPVS01 çağrılmaz, `CA-LASTCHANGED` değişmez.<br>Yan etki: `LGSTSQ`'ya iki LINK.<br>(1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGUPDB01'`, `' CNUM=0000000004'`, `' PNUM=0000000006'`, `' UPDATE HOUSE '` + ␣×2, `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + ilk 90 bayt ([CICS bilgisi] LGUPDB01 `EIBCALEN = 32500` görür: `ELSE` dalı); 7-8. baytlarda `90`. Mesajlar yazıldıktan sonra CLOSE `00` yazar. | `lgupdb01.cbl:430`, `:444-451`, `:302-307`, `:369-371`, `:502-518`, `:527-532` | İş kuralı |
| 10-09 | 10-01 ile aynı. [B4] FETCH `0`, `CA-LASTCHANGED` = `DB2-LASTCHANGED`; `UPDATE POLICY` `SQLCODE ≠ 0` döner; diğer bütün deyimler `0`. | `CA-RETURN-CODE = 00` | `UPDATE POLICY`'nin SQLCODE'u kontrol edilmez (Kod gözlemleri 8). `HOUSE` 6 10-01'in değerleriyle güncellenir, `POLICY` 6 güncellenmez. `CA-LASTCHANGED` girdideki değerle aynı kalır (satır güncellenmediği için SELECT eski değeri okur; stub SELECT'i doldurmazsa alan zaten değişmez). VSAM 10-01'deki gibi yeniden yazılır. Hata mesajı yazılmaz. | `lgupdb01.cbl:318-336`, `:360`, `:371`, `:209`<br>`lgupvs01.cbl:155-159` | İş kuralı |
| 10-10 | 10-01 ile aynı. [B4] FETCH `0`, `CA-LASTCHANGED` = `DB2-LASTCHANGED`; `SELECT LASTCHANGED` `SQLCODE ≠ 0` döner; CLOSE dahil diğer bütün deyimler `0`. | `CA-RETURN-CODE = 00` (`90` yazılır, CLOSE ezer) | Satır satır: `lgupdb01.cbl:336` → `:338` `SYNCPOINT ROLLBACK` → `:339` `'90'` → `:341` mesaj → `:360` CLOSE → `:371` `'00'` → `:209` LGUPVS01.<br>Db2: [CICS bilgisi] `SYNCPOINT ROLLBACK` iş birimindeki değişiklikleri geri alır; `HOUSE` 6 ve `POLICY` 6'daki güncellemeler geri alınır. VSAM: kayıt 10-01'deki gibi yeniden yazılır (geri almadan sonra). [B4] Düzenek hata dönen `SELECT INTO`'da host değişkene yazmaz: `CA-LASTCHANGED` girdideki gibi kalır. Gerçek Db2'de alanın içeriği **belirsiz** (C).<br>Yan etki: LGUPDB01 iki mesaj yazar: `EM-SQLREQ` `' UPDATE POLICY  '` (SELECT'in adı değil), `' SQLCODE='` + SELECT'in SQLCODE'u; `CA-ERROR-MSG`'de 7-8. baytlar `90`.<br>Gerçek Db2'de CLOSE geri almadan sonra başka bir SQLCODE dönebilir: `-501` ise `00` ve RETURN, VSAM yazılmaz; başka değer ise `90` (Kod gözlemleri 8; C). | `lgupdb01.cbl:317`, `:329-342`, `:360`, `:369-371`, `:209` | İş kuralı |
| 10-11 | 10-01 ile aynı. [B4] OPEN `SQLCODE = -913` döner. | `CA-RETURN-CODE = 90` | FETCH, UPDATE ve CLOSE çalışmaz; LGUPVS01 çağrılmaz. Db2 ve VSAM değişmez, `CA-LASTCHANGED` değişmez.<br>Yan etki: `LGSTSQ`'ya iki LINK. (1) `ERROR-MSG`: `' CNUM=0000000004'`, `' PNUM=0000000006'`, `' OPEN   PCURSOR '`, `' SQLCODE=-00913'`. (2) `CA-ERROR-MSG`: ilk 90 bayt ([CICS bilgisi] LGUPDB01 `EIBCALEN = 32500` görür: `ELSE`); 7-8. baytlarda `90`. | `lgupdb01.cbl:254-265`, `:502-532`<br>`lgupol01.cbl:143`, `:147`<br>`lgstsq.cbl:68-71`, `:90`, `:94-111` | İş kuralı |
| 10-12 | 10-01 ile aynı. [B4] OPEN `0` ve `-913` dışında bir SQLCODE döner. | `CA-RETURN-CODE = 90` | 10-11 ile aynı; mesajdaki SQLCODE düzeneğin değeri. `-913` dalı ile `When Other` dalı aynı işi yapar (`lgupdb01.cbl:262-269`). | `lgupdb01.cbl:266-269` | İş kuralı |
| 10-13 | 10-01 ile aynı. [B4] FETCH `0` ve `100` dışında bir SQLCODE döner. | `CA-RETURN-CODE = 00` (`90` yazılır, CLOSE ezer) | Satır satır: `lgupdb01.cbl:353-356` `'90'` ve mesaj → `:360` CLOSE → `:371` `'00'` → `:209` LGUPVS01.<br>Db2 değişmez, `CA-LASTCHANGED` değişmez. VSAM kaydı COMMAREA'daki konut verileriyle yeniden yazılır.<br>Yan etki: LGUPDB01 iki mesaj yazar (`' FETCH  ROW   '` + ␣×2, SQLCODE; `CA-ERROR-MSG`'de `90`). [belge] ile farkı: Kod gözlemleri 18. | `lgupdb01.cbl:229`, `:349-358`, `:360`, `:371`, `:209`<br>`lgupvs01.cbl:155-159` | İş kuralı |
| 10-14 | 10-01 ile aynı. [B4] FETCH `0`, `CA-LASTCHANGED` = `DB2-LASTCHANGED`; `lgupdb01.cbl:360`'taki CLOSE `SQLCODE = -501` döner. | `CA-RETURN-CODE = 00` | `lgupdb01.cbl:372-375`: `'00'`, `EM-SQLREQ = '-501 detected c'` (mesaj yazılmaz), RETURN. **LGUPVS01 çağrılmaz**: `HOUSE` 6 ve `POLICY` 6 10-01'deki gibi güncellenir, `CA-LASTCHANGED` yeni değer; VSAM kaydı değişmez. | `lgupdb01.cbl:360`, `:364-375` | İş kuralı |
| 10-15 | 10-01 ile aynı. [B4] FETCH `0`, `CA-LASTCHANGED` = `DB2-LASTCHANGED`; `lgupdb01.cbl:360`'taki CLOSE `0` ve `-501` dışında bir SQLCODE döner. | `CA-RETURN-CODE = 90` | `lgupdb01.cbl:376-379`: `'90'`, mesaj (`' CLOSE  PCURSOR'` + ␣, SQLCODE), RETURN. LGUPVS01 çağrılmaz: VSAM değişmez. `HOUSE` 6 ve `POLICY` 6 güncellenmiştir, `CA-LASTCHANGED` yeni değer; kod geri alma istemez (kalıcılık CICS'in görev sonu davranışına bağlı, C). | `lgupdb01.cbl:364-367`, `:376-379` | İş kuralı |
| 10-16 | 10-01 ile aynı. Db2 adımları başarılı. [B4] LGUPVS01'deki READ `NORMAL` dışında bir RESP döner (örn. düzenek kaydı önceden siler). | [B8] `CA-RETURN-CODE = 81` (ön koşullu). `81` yazılır, ardından ABEND `LGV3` (`lgupvs01.cbl:147-151`). [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | `HOUSE` 6 ve `POLICY` 6 UPDATE'leri yapılmıştır; ABEND sonrasında geri alınıp alınmadıkları **belirsiz** (C; Kod gözlemleri 12).<br>Yan etki: LGUPVS01'in iki mesajı (10-05 ile aynı biçim; `' CNUM=0000000004'`). | `lgupvs01.cbl:139-153`, `:174-190` | İş kuralı |
| 10-17 | 10-01 ile aynı. Db2 adımları ve READ başarılı. [B4] REWRITE `NORMAL` dışında bir RESP döner. | [B8] `CA-RETURN-CODE = 82` (ön koşullu). `82` yazılır, ardından ABEND `LGV4` (`lgupvs01.cbl:160-164`). [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | Db2 güncellemelerinin geri alınması **belirsiz** (C). VSAM kaydı yeniden yazılmaz.<br>Yan etki: LGUPVS01'in iki mesajı; metin READ hatasıyla aynı (`' Re-write  KSDSPOLY '`), `CA-ERROR-MSG`'de 7-8. baytlar `82`. | `lgupvs01.cbl:155-166`, `:71-72` | İş kuralı |
| 10-18 | 10-01 ile aynı, ama `CA-PAYMENT = 000500` (seçilmiş değer: Db2'deki 0'dan farklı olsun diye; kaynağı yok). Sıra ve B4 10-01'deki gibi. | `CA-RETURN-CODE = 00` | 10-01 ile aynı; **`POLICY.payment` 0 kalır** (`db2cre.jcl:881`): `SET` listesinde `PAYMENT` yok (Kod gözlemleri 9). `CA-PAYMENT` COMMAREA'da `000500` kalır. | `lgupdb01.cbl:127`, `:138-142`, `:314`, `:319-325` | İş kuralı |
| 10-19 | 10-01 ile aynı, ama `CA-REQUEST-ID = '01uhou'` (küçük harf). Sıra ve B4 10-01'deki gibi (zaman damgası eşleşir). [B4] `h00000000040000000006` anahtarı VSAM'da yok. | [B8] `CA-RETURN-CODE = 81` (ön koşullu). `99` (`lgupol01.cbl:140`) LGUPDB01'de `00` ile ezilir (`lgupdb01.cbl:190`); LGUPVS01 `81` yazar, ardından ABEND `LGV3`. `99` hiç ulaşmaz. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | Satır satır: LGUPOL01 `WHEN OTHER` → uzunluk kontrolü yok → LINK LGUPDB01 → OPEN/FETCH → zaman damgası eşit → `:283-300` hiçbir kol çalışmaz → `:302` geçer → **`UPDATE POLICY` çalışır** → SELECT → CLOSE `00` → LGUPVS01: `'h'` `When Other` → veri alanı boşluk → READ hatası.<br>Db2: `POLICY` 6'nın ortak alanları 10-01'deki gibi güncellenir, **`HOUSE` 6 güncellenmez**; ABEND sonrasında geri alınması **belirsiz** (C).<br>Yan etki: yalnızca LGUPVS01'in iki mesajı. | `lgupol01.cbl:113`, `:139-143`<br>`lgupdb01.cbl:190`, `:283-302`, `:318-334`, `:360`, `:371`, `:209`<br>`lgupvs01.cbl:102`, `:106`, `:133-134`, `:139-151` | Arayüz (COMMAREA) kuralı |
| 10-20 | 10-01 ile aynı, ama `CA-REQUEST-ID` = ␣×6 ve `CA-LASTCHANGED` = ␣×26 (eşleşmez). [B4] FETCH `0`, `DB2-LASTCHANGED` = Db2 değeri; ` 00000000040000000006` (boşlukla başlayan) anahtar VSAM'da yok. | [B8] `CA-RETURN-CODE = 81` (ön koşullu). `99` → `00` (`lgupdb01.cbl:190`) → `02` (`:346`) → `00` (`:371`) → LGUPVS01 `81` + ABEND `LGV3`. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | Db2 değişmez. LGUPVS01'de `When Other` (4. bayt boşluk), READ hatası. Yan etki: yalnızca LGUPVS01'in iki mesajı. | `lgupol01.cbl:139-143`<br>`lgupdb01.cbl:190`, `:278`, `:346`, `:360`, `:371`<br>`lgupvs01.cbl:133-134`, `:139-151` | Arayüz (COMMAREA) kuralı |
| 10-21 | 10-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000004` (10 haneli). Sıra ve B4 10-01'deki gibi. [B4] `H10000000040000000006` anahtarı VSAM'da yok. | [B3] [B8] `CA-RETURN-CODE = 81` (ön koşullu). Db2 adımları başarılı (`00`), ardından LGUPVS01 `81` + ABEND `LGV3`. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | [B3] `DB2-CUSTOMERNUM-INT` (`S9(9) COMP`, `lgupdb01.cbl:87`) değeri 4 olur; cursor müşteri 4'ün poliçe 6'sını bulur ve Db2 10-01'deki gibi güncellenir. VSAM anahtarı COMMAREA'dan kurulur: `1000000004` (`lgupvs01.cbl:104`); kayıt bulunamaz. Db2 güncellemelerinin ABEND sonrasında geri alınması **belirsiz** (C). | `lgupdb01.cbl:87`, `:195`, `:136`<br>`lgupvs01.cbl:104`, `:139-151`<br>`lgcmarea.cpy:12` | Arayüz (COMMAREA) kuralı |
| 10-22 | 10-01 ile aynı, ama `CA-H-BEDROOMS` = ␣×3 (sayısal alanda sayısal olmayan içerik). | **belirsiz** | **belirsiz**. Kesin olan: OPEN, FETCH ve zaman damgası karşılaştırması bu alandan önce yapılır (`lgupdb01.cbl:254-278`). `:427` sayısal olmayan DISPLAY içeriği `COMP` alana taşır; sonuç derleyiciye bağlı (C). | `lgupdb01.cbl:93`, `:427`<br>`lgcmarea.cpy:58` | Arayüz (COMMAREA) kuralı |
| 10-23 | LGUPOL01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgupol01.cbl:102`'den sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGUPDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGUPOL01'`, `' NO COMMAREA RECEIVED'`). Kayıt 50 bayt. COMMAREA dökümü gönderilmez. | `lgupol01.cbl:44-49`, `:99-103`, `:172-184`, `:186` | Arayüz (COMMAREA) kuralı |
| 10-24 | LGUPOL01 çağrılır. `CA-REQUEST-ID = '01UHOU'`. `EIBCALEN` 8 ile 157 arasında (sınır değer: `157`). | `CA-RETURN-CODE = 98` | LGUPDB01 çağrılmaz, Db2'ye ve VSAM'a dokunulmaz, hata mesajı yazılmaz. | `lgupol01.cbl:62-63`, `:40`, `:123-129` | Arayüz (COMMAREA) kuralı |
| 10-25 | LGUPOL01 çağrılır. `EIBCALEN` 6 ya da 7; ilk 6 bayt `'01UHOU'` (istek kodu gelen COMMAREA'nın içinde). | [B1] Tamponun 7-8. baytları `98` olur (ön koşullu) [kod] `lgupol01.cbl:127`. Ön koşulsuz kesin olan: `EIBCALEN = 7` ise COMMAREA'nın 7. baytı `9`. | Kesin: LGUPDB01 çağrılmaz, mesaj yazılmaz, 1-6. baytlar değişmez. | `lgcmarea.cpy:10-11`<br>`lgupol01.cbl:105`, `:113`, `:123-129` | Arayüz (COMMAREA) kuralı |
| 10-26 | LGUPOL01 çağrılır. `EIBCALEN` 1 ile 5 arasında. | **belirsiz** | **belirsiz**. İstek kodunun bir kısmı gelen COMMAREA'nın dışında. B1 ile dışarıdaki baytlar boşluk olur ve kod `'01UHOU'` olarak tanınmaz: `WHEN OTHER` (`lgupol01.cbl:139-140`) → uzunluk kontrolü olmadan LGUPDB01 çağrılır; LGUPDB01 müşteri ve poliçe numarasını boşluklu alanlardan okur ve 10-22'deki belirsizliğe düşer. | `lgupol01.cbl:113`, `:139-143`<br>`lgupdb01.cbl:195-196` | Arayüz (COMMAREA) kuralı |
| 10-27 | LGUPOL01 çağrılır. `EIBCALEN = 158` (kabul edilen en küçük uzunluk). Diğer girdiler, sıra ve B4 10-01 ile aynı. | [B1] `CA-RETURN-CODE = 00` (ön koşullu) | Kesin: `158 < 158` yanlış, kontrol geçer. Okunan ve yazılan bütün alanlar 1-158. baytlarda.<br>[B1] LGUPOL01 kendi 158 baytlık COMMAREA'sını `LENGTH(32500)` ile geçirir; B1 ile bu düzeneğin tamponudur. Sonuç ve yan etkiler 10-01 ile aynı. | `lgupol01.cbl:124-126`, `:157-160`<br>`lgupdb01.cbl:209-212` | Arayüz (COMMAREA) kuralı |
| 10-28 | LGUPOL01 çağrılır. `EIBCALEN = 32500`. LGUPDB01'e ya da LGUPVS01'e yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. İki LINK'te de `RESP`/`NOHANDLE` yok, `HANDLE CONDITION` yok. Sonuç CICS'in varsayılan davranışına bağlı (C). | `lgupol01.cbl:157-160`<br>`lgupdb01.cbl:209-212` | Arayüz (COMMAREA) kuralı |
| 10-29 | **LGUPDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgupdb01.cbl:186`'dan sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: tek LINK: 87 baytlık `ERROR-MSG` (tarih, saat, `' LGUPDB01'`, `' NO COMMAREA RECEIVED'` + ␣×36 + 6 baytlık `EM-SQLRC`). [B2] `EM-SQLRC = +00000` (ön koşullu) [kod] `lgupdb01.cbl:504`. COMMAREA dökümü gönderilmez. | `lgupdb01.cbl:44-51`, `:183-187`, `:504`, `:515-520` | Arayüz (COMMAREA) kuralı |
| 10-30 | **LGUPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (sınır değer: `90`). `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000006`; `CA-REQUEST-ID` herhangi bir değer (LGUPDB01 okumaz). [B4] OPEN `0` dışında bir SQLCODE döner. | `CA-RETURN-CODE = 90` | LGUPDB01'de uzunluk ve istek kodu kontrolü olmadığı için OPEN'a ulaşılır (Kod gözlemleri 2). FETCH ve LGUPVS01 çalışmaz.<br>Yan etki: iki LINK. (1) `ERROR-MSG`: `' OPEN   PCURSOR '`, SQLCODE. (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca doğrudan çağrıyla ulaşılır. | `lgupdb01.cbl:254-269`, `:520-526` | Arayüz (COMMAREA) kuralı |
| 10-31 | **LGUPVS01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | **belirsiz** | **belirsiz**. LGUPVS01'de `EIBCALEN` kontrolü yok; `lgupvs01.cbl:102-104` var olmayan COMMAREA'dan okur, READ uzunluğu 0 olur (`:100`, `:141`). Sonuç kodda tanımlı değil. | `lgupvs01.cbl:100-104`, `:139-146` | Arayüz (COMMAREA) kuralı |
| 10-32 | **LGUPVS01 doğrudan** çağrılır. `EIBCALEN = 225`. `CA-REQUEST-ID = '01UHOU'`, `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000006` (`H00000000040000000006` anahtarı var: `ksdspoly.txt:5`). `CA-RETURN-CODE = 55` (seçilmiş değer: yazılmadığını göstermek için; kaynağı yok). Konut alanları 10-01 ile aynı. [B4] READ ve REWRITE `NORMAL`. | `CA-RETURN-CODE = 55` (değişmez) | READ ve REWRITE başarılı. `H00000000040000000006` kaydı yeniden yazılır: `FLAT` + ␣×11, `001`, `00260000`, `E15WW` + ␣×3, ␣×9. Hata mesajı yazılmaz. Db2'ye dokunulmaz. LGUPVS01 başarı yolunda dönüş koduna yazmaz. | `lgupvs01.cbl:100-104`, `:120-125`, `:139-160`, `:170-172` | Arayüz (COMMAREA) kuralı |
| 10-33 | **LGUPVS01 doğrudan** çağrılır. `CA-REQUEST-ID = '01UXOU'`. Gerekçe: dal seçimi yalnızca 4. karaktere bakar (`lgupvs01.cbl:102`, `:106`); `'X'` C/E/H/M'nin hiçbiri değildir, `When Other` veri alanını boşlukla doldurur (`:133-134`) ve COMMAREA'dan yalnızca 1-28. baytlar okunur. `EIBCALEN` 28 ile 90 arasında. `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000006`. [B4] `X00000000040000000006` anahtarı VSAM'da yok; READ `NORMAL` dışında bir RESP döner. | [B8] `CA-RETURN-CODE = 81` (ön koşullu). `81` yazılır, ardından ABEND `LGV3`; B8 ile LGUPVS01 düzeneğin sürücüsüne döner. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır: orada çağıranın kodu görmesi **belirsiz** (C). | Yan etki: iki LINK. (1) `ERROR-MSG` (101 bayt): `' PNUM='` + ␣×10, `' CNUM=0000000004'`, `' Re-write  KSDSPOLY '`, RESP, RESP2 (kayıtta RESP2 yok). (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır. | `lgupvs01.cbl:102-106`, `:133-134`, `:139-151`, `:174-197` | Arayüz (COMMAREA) kuralı |

## 10-01 girdi ve beklenen değerler

COMMAREA'nın alan alan girdisi ve dönüşte beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir. Yeni ortak alanlar poliçe 8'in,
yeni konut alanları `HOUSE` 8'in JCL değerleridir; güncellenen poliçe 6'dır.

| COMMAREA alanı | Girdi | Girdinin kaynağı (`db2cre.jcl`) | Dönüşte beklenen | Alanı kullanan / dolduran COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` X(6) | `01UHOU` | — (istek kodu: `lgupol01.cbl:123`) | Değişmez | `lgupol01.cbl:113`, `lgupdb01.cbl:283`, `lgupvs01.cbl:102` |
| `CA-RETURN-CODE` 9(2) | Herhangi bir değer | — | `00` | `lgupol01.cbl:105`, `lgupdb01.cbl:190`, `:261`, `:371` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000004` | `:874` | Değişmez | `lgupdb01.cbl:195`, `lgupvs01.cbl:104` |
| `CA-POLICY-NUM` 9(10) | `0000000006` | `:873` | Değişmez | `lgupdb01.cbl:196`, `lgupvs01.cbl:103`, `:137` |
| `CA-ISSUE-DATE` X(10) | `2011-08-12` | `:923` (poliçe 8) | Değişmez | `lgupdb01.cbl:320` |
| `CA-EXPIRY-DATE` X(10) | `2012-08-11` | `:924` (poliçe 8) | Değişmez | `lgupdb01.cbl:321` |
| `CA-LASTCHANGED` X(26) | 09-01'in döndürdüğü değer (poliçe 6'nın Db2 değeri; JCL'de `'2011-09-01 22:32:42'`) | `:878` | `UPDATE POLICY` anındaki `CURRENT TIMESTAMP`, 26 bayt (biçim belirsiz) | `lgupdb01.cbl:278`, `:322`, `:329-334` |
| `CA-BROKERID` 9(10) | `0000000000` | `:927` (poliçe 8) | Değişmez | `lgupdb01.cbl:313`, `:323` |
| `CA-BROKERSREF` X(10) | ␣×10 (JCL'de `''`) | `:928` (poliçe 8) | Değişmez | `lgupdb01.cbl:324` |
| `CA-PAYMENT` 9(6) | `000000` | `:929` (poliçe 8) | Değişmez | `lgupdb01.cbl:314` (Db2'ye yazılmaz) |
| `CA-H-PROPERTY-TYPE` X(15) | `FLAT` + ␣×11 | `:1086` (`HOUSE` 8) | Değişmez | `lgupdb01.cbl:434`; `lgupvs01.cbl:121` |
| `CA-H-BEDROOMS` 9(3) | `001` | `:1087` | Değişmez | `lgupdb01.cbl:427`, `:435`; `lgupvs01.cbl:122` |
| `CA-H-VALUE` 9(8) | `00260000` | `:1088` | Değişmez | `lgupdb01.cbl:428`, `:436`; `lgupvs01.cbl:123` |
| `CA-H-HOUSE-NAME` X(20) | ␣×20 (JCL'de `' '`) | `:1089` | Değişmez | `lgupdb01.cbl:437`; `lgupvs01.cbl:125` |
| `CA-H-HOUSE-NUMBER` X(4) | `␣12b` | `:1090` | Değişmez | `lgupdb01.cbl:438` (VSAM'a yazılmaz) |
| `CA-H-POSTCODE` X(8) | `E15WW` + ␣×3 | `:1091` | Değişmez | `lgupdb01.cbl:439`; `lgupvs01.cbl:124` |
| `CA-H-FILLER` X(32342) | Herhangi bir değer | — | Değişmez | Kodda okunmuyor, yazılmıyor |

## 10-01'den sonra Db2'de ve VSAM'da beklenen durum

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `POLICY` (poliçe 6) | `issueDate` = 2011-08-12, `expiryDate` = 2012-08-11, `lastChanged` = `UPDATE` anındaki zaman damgası, `brokerId` = 0, `brokersReference` = boşluk. Değişmeyenler: `customerNumber` = 4, `policyType` = `H`, `payment` = 0, `commission` = 0 (`db2cre.jcl:874`, `:877`, `:881-882`). | `lgupdb01.cbl:318-326` |
| `HOUSE` (poliçe 6) | `propertyType` = `FLAT`, `bedrooms` = 1, `value` = 260000, `houseName` = boşluk, `houseNumber` = `␣12b`, `postcode` = `E15WW`. | `lgupdb01.cbl:427-442` |
| `KSDSPOLY` `H00000000040000000006` | 64 bayt: anahtar (21) + `FLAT` + ␣×11 + `001` + `00260000` + `E15WW` + ␣×3 + ␣×9 (ev adının ilk 9 baytı). | `lgupvs01.cbl:26-47`, `:102-104`, `:120-125`, `:155-159` |
| `GENAERRS` / `CSMT` | Kayıt yazılmaz. | — |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları. "Başka tür" maddelerine tahmin yazılmadı; bu
maddeler, dalın alınmadığı bir konut testine bağlandı. Envanterdeki geçici numaralar dosyada bir kaydı: `EIBCALEN` 1-7 durumu
10-25 (6-7 bayt) ve 10-26 (1-5 bayt) olarak ikiye ayrıldı.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| UP-01 | `lgupol01.cbl:99-103` `IF EIBCALEN = 0` → ABEND `LGCA` | 10-23 | |
| UP-02 | `lgupol01.cbl:105` `'00'` → `CA-RETURN-CODE` | 10-01, 10-25 | |
| UP-03 | `lgupol01.cbl:113` `EVALUATE CA-REQUEST-ID` (büyük/küçük harfe duyarlı) | 10-01, 10-19, 10-26 | |
| UP-04a | `lgupol01.cbl:115-121` `WHEN '01UEND'` | 10-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| UP-04b | `lgupol01.cbl:123-129` `WHEN '01UHOU'` → 158, `98` | 10-24 (reddedilir), 10-27 (kabul), 10-25 (alan dışı) | |
| UP-04c | `lgupol01.cbl:131-137` `WHEN '01UMOT'` | 10-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| UP-04d | `lgupol01.cbl:139-140` `WHEN OTHER` → `99`, RETURN yok | 10-19, 10-20, 10-26 | `99` her zaman `lgupdb01.cbl:190`'da ezilir (Kod gözlemleri 3) |
| UP-05 | `lgupol01.cbl:143`, `:155-162` LINK LGUPDB01 `LENGTH(32500)`, `RESP` yok | 10-01, 10-28 | |
| UP-06 | `lgupol01.cbl:146-147` RETURN; LGUPDB01'in dönüş kodu kontrol edilmez | 10-01, 10-11 | |
| UP-07 | `lgupol01.cbl:169-184` `WRITE-ERROR-MESSAGE`, 45 baytlık `ERROR-MSG` | 10-23 | |
| UP-08 | `lgupol01.cbl:186-200` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 10-23 | Dış `IF` yanlış. İç dallar (`:187-199`) **kapsam dışı: ulaşılamaz**; yordam yalnızca `:101`'den çağrılıyor |
| UD-01 | `lgupdb01.cbl:168-177` başlangıç, `INITIALIZE DB2-POLICY` / `DB2-IN-INTEGERS` | 10-01 | Kod gözlemleri 5 (düzenek notu) |
| UD-02 | `lgupdb01.cbl:183-187` `IF EIBCALEN = 0` → ABEND `LGCA` | 10-29 | Yalnızca doğrudan çağrı |
| UD-03 | `lgupdb01.cbl:190` `'00'` (LGUPOL01'in `99`'unu ezer) | 10-01, 10-19 | |
| UD-04 | `lgupdb01.cbl:195-199` numaralar ve `EM-CUSNUM` / `EM-POLNUM` | 10-01, 10-21 | 10-21: B3 |
| UD-05 | LGUPDB01'de uzunluk ve istek kodu kontrolü yok (`lgupdb01.cbl:64-65` kullanılmıyor, `:201-205` yorum) | 10-19, 10-30 | Kod gözlemleri 2 |
| UD-06 | `lgupdb01.cbl:128-143` `POLICY_CURSOR` tanımı | 10-01 | |
| UD-07 | `lgupdb01.cbl:254-270` OPEN ve `Evaluate SQLCODE` | 10-01 (`0`), 10-11 (`-913`), 10-12 ve 10-30 (`When Other`) | |
| UD-08 | `lgupdb01.cbl:228-239` FETCH, 6 host değişkeni / 5 sütun | 10-01 | Uyumsuzluğun etkisi C (Kod gözlemleri 5) |
| UD-09 | `lgupdb01.cbl:275` `IF SQLCODE = 0` (FETCH) | 10-01 (doğru), 10-05 (yanlış) | |
| UD-10 | `lgupdb01.cbl:278` `IF CA-LASTCHANGED = DB2-LASTCHANGED` | 10-01 (eşit), 10-03 ve 10-04 (farklı) | Kod gözlemleri 6 |
| UD-11a | `lgupdb01.cbl:286-288` `WHEN '01UEND'` | 10-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| UD-11b | `lgupdb01.cbl:291-293` `WHEN '01UHOU'` → `UPDATE-HOUSE-DB2-INFO` | 10-01 | |
| UD-11c | `lgupdb01.cbl:296-298` `WHEN '01UMOT'` | 10-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| UD-11d | `lgupdb01.cbl:283-300` `WHEN OTHER` yok | 10-19 | Hiçbir kol çalışmaz, `UPDATE POLICY` yine çalışır |
| UD-12 | `lgupdb01.cbl:302-307` `IF CA-RETURN-CODE ≠ '00'` → CLOSE, RETURN | 10-07, 10-08 | Kod gözlemleri 7 |
| UD-13 | `lgupdb01.cbl:313-314` broker ve ödeme dönüşümü | 10-18 | Ödeme kullanılmaz (Kod gözlemleri 9) |
| UD-14 | `lgupdb01.cbl:317-326` `UPDATE POLICY`, SQLCODE kontrolü yok | 10-01, 10-09 | Kod gözlemleri 8 |
| UD-15 | `lgupdb01.cbl:329-334` `SELECT LASTCHANGED` | 10-01 | |
| UD-16 | `lgupdb01.cbl:336-342` SELECT hatası → `SYNCPOINT ROLLBACK`, `90`, mesaj | 10-10 | Tek geri alma (Kod gözlemleri 8) |
| UD-17 | `lgupdb01.cbl:344-346` zaman damgası farklı → `02` | 10-03, 10-04 | `02` her zaman ezilir |
| UD-18 | `lgupdb01.cbl:349-358` FETCH: `100` → `01`; başka → `90`, mesaj | 10-05, 10-06 (`100`), 10-13 (başka) | |
| UD-19 | `lgupdb01.cbl:360` `PERFORM CLOSE-PCURSOR` | 10-01, 10-03 | |
| UD-20 | `lgupdb01.cbl:362-381` `CLOSE-PCURSOR` `Evaluate SQLCODE` | 10-01, 10-03, 10-05 (`0`), 10-14 (`-501`), 10-15 (`When Other`) | Kod gözlemleri 7 |
| UD-21 | `lgupdb01.cbl:209-212` LINK LGUPVS01 `LENGTH(225)`, `RESP` yok | 10-01, 10-03, 10-05 | |
| UD-22 | `lgupdb01.cbl:427-428` konut sayısal dönüşümleri | 10-01, 10-22 | 10-22 belirsiz |
| UD-23 | `lgupdb01.cbl:430-442` `UPDATE HOUSE` | 10-01 | |
| UD-24 | `lgupdb01.cbl:444-453` `100` → `01`; başka → `90`, mesaj | 10-07 (`100`), 10-08 (başka) | |
| UD-25 | `lgupdb01.cbl:502-535` `WRITE-ERROR-MESSAGE` ve `EIBCALEN` dalları | 10-08 (`ELSE`), 10-30 (`< 91`), 10-29 (dış `IF` yanlış) | |
| UD-26 | `lgupdb01.cbl:31`, `:64-65`, `:69-71` kullanılmayan alanlar | 10-01 | Kod gözlemleri 17 |
| UD-BT | `lgupdb01.cbl:387-418` `UPDATE-ENDOW-DB2-INFO`, `:460-495` `UPDATE-MOTOR-DB2-INFO` | 10-01 (paragraflar çağrılmaz) | **Başka tür**: tahmin yazılmadı |
| UV-01 | `lgupvs01.cbl:100` `EIBCALEN` → READ uzunluğu, doğrulama yok | 10-31 | |
| UV-02 | `lgupvs01.cbl:102-104`, `:137` anahtar | 10-01, 10-21 | |
| UV-03a | `lgupvs01.cbl:108-111` `When 'C'` | 10-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| UV-03b | `lgupvs01.cbl:113-118` `When 'E'` | 10-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| UV-03c | `lgupvs01.cbl:120-125` `When 'H'` | 10-01 | Ev adı 9 bayt, ev numarası yok |
| UV-03d | `lgupvs01.cbl:127-131` `When 'M'` | 10-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| UV-03e | `lgupvs01.cbl:133-134` `When Other` → boşluk | 10-19, 10-20, 10-33 | Zincirden ulaşılabilir (Kod gözlemleri 11) |
| UV-04 | `lgupvs01.cbl:139-146` `READ UPDATE` | 10-01 | |
| UV-05 | `lgupvs01.cbl:147-153` READ `RESP ≠ NORMAL` → `81`, mesaj, ABEND `LGV3` | 10-05, 10-06, 10-16, 10-19, 10-20, 10-21, 10-33 | `:152` RETURN **kapsam dışı: ulaşılamaz** (ABEND'den sonra) |
| UV-06 | `lgupvs01.cbl:155-159` `REWRITE` | 10-01, 10-32 | |
| UV-07 | `lgupvs01.cbl:160-166` REWRITE `RESP ≠ NORMAL` → `82`, mesaj, ABEND `LGV4` | 10-17 | `:165` RETURN **kapsam dışı: ulaşılamaz** (ABEND'den sonra) |
| UV-08 | `lgupvs01.cbl:170-172` GOBACK; dönüş koduna yazılmaz | 10-01, 10-32 | |
| UV-09 | `lgupvs01.cbl:174-190` `WRITE-ERROR-MESSAGE`, 101 bayt, `PNUM` boş | 10-05, 10-16 | |
| UV-10 | `lgupvs01.cbl:191-205` `EIBCALEN` dalları | 10-05 (`ELSE`), 10-33 (`< 91`) | Dış `IF` yanlış yalnızca `EIBCALEN = 0` iken; o yol belirsiz (10-31) |
| SQ-01 | `lgstsq.cbl:68-71` LINK yolu | 10-05, 10-08, 10-11, 10-23, 10-29, 10-30, 10-33 | |
| SQ-02 | `lgstsq.cbl:72-80` `ELSE` (terminalden RECEIVE) | 10-11 (dal alınmaz) | **Kapsam dışı: bu zincirden ulaşılamaz.** Üç program da LGSTSQ'yu LINK ile çağırır |
| SQ-03 | `lgstsq.cbl:82-88` `'Q='` kontrolü | 10-11 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz** |
| SQ-04 | `lgstsq.cbl:90`, `:94-99` +5, WRITEQ TD `CSMT` | 10-11 | |
| SQ-05 | `lgstsq.cbl:105-111` WRITEQ TS `GENAERRS` | 10-11 | |
| SQ-06 | `lgstsq.cbl:113-119` `IF WS-FLAG = 'R'` → SEND TEXT | 10-11 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz** |
| SQ-07 | `lgstsq.cbl:121-122` RETURN | 10-11 | |
| SQ-08 | Kayıt uzunlukları 45 → 50, 87 → 92, 99 → 104 (81 bayt) | 10-11, 10-23, 10-29 | Kod gözlemleri 15 |
| SQ-09 | LGUPVS01'in 101 baytlık mesajı → 106, RESP2 kayda girmez | 10-05, 10-33 | Kod gözlemleri 15 |
| N-01 | `policyType` okunmaz | 10-02 | Kod gözlemleri 4 |
| N-02 | Büyük/küçük harfe duyarlı; `UPPER-CASE` yok | 10-19 | Kod gözlemleri 4 |
| N-03 | `99` çağırana ulaşmaz | 10-19, 10-20 | Kod gözlemleri 3 |
| N-04 | CLOSE `01` / `02` / `90`'ı ezer | 10-03, 10-05, 10-07, 10-08, 10-10, 10-13 | Kod gözlemleri 7 |
| N-05 | Db2 başarısızken VSAM yine güncellenir ya da `81` + ABEND | 10-03, 10-05, 10-13 | |
| N-06 | `02` görünmez; tek iz `CA-LASTCHANGED` | 10-03, 10-04 | Kod gözlemleri 6 |
| N-07 | `UPDATE POLICY` kontrol edilmez | 10-09, 10-10 | Kod gözlemleri 8 |
| N-08 | Tek açık geri alma `lgupdb01.cbl:338` | 10-10 | Kod gözlemleri 8, 12 |
| N-09 | Ödeme güncellenmez | 10-18 | Kod gözlemleri 9 |
| N-10 | LINK'lerde `RESP` yok | 10-28 | |
| N-11 | Uzunluk kontrolü yalnızca LGUPOL01'de (158) | 10-24, 10-27 | Kod gözlemleri 2 |
| N-12 | FETCH 5/6 uyumsuzluğu | 10-01 | C; Kod gözlemleri 5 |
| N-13 | `docs/api-design.md` ile farklar | 10-03, 10-05, 10-13 | Kod gözlemleri 18 |
| N-14 | `LASTCHANGED` akışı | 10-01, 10-04 | Kod gözlemleri 6 |
| N-15 | WORKING-STORAGE kalıcılığı | 10-24 | Tahminler `VALUE` başlangıcına göre (Kod gözlemleri 16) |

## Ön koşullu satırlar

Ön koşul kodları ve "sıra" ön koşulu dosyanın başında ("Kaynak etiketleri ve ön koşul kodları"). B4 satırlarında son sütun iki durumu ayrı yazar: **düzenekte** (senaryo değeri vermez ya da host değişkenine yazmazsa; B4'ün varsayılanı `0` / `NORMAL`) ve **gerçek Db2/VSAM'de** (sonucu gerçek sistem verirse).

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 10-01 | B4, sıra (09-01) | `00`; Db2 ve VSAM güncellenir, yeni `CA-LASTCHANGED` | B4 yoksa: **düzenekte** `DB2-LASTCHANGED` kurulmazsa boşluk kalır (`lgupdb01.cbl:176`) ve girdideki zaman damgası eşleşmez: 10-03 yolu (`00`, yalnızca VSAM); **gerçek Db2'de** FETCH'in 5 sütun / 6 host değişkeni uyumsuzluğundaki SQLCODE'u Db2'ye bağlı (C; Kod gözlemleri 5): `0` ise tahmin geçerli, hata ise 10-13 yolu (`00`, yalnızca VSAM). Sıra yoksa zaman damgası eşleşmeyebilir: 10-03 yolu (`00`, yalnızca VSAM) |
| 10-02 | B4, sıra (09-03) | `00`; poliçe 7 güncellenir, türü `C` kalır | 10-01 ile aynı |
| 10-03 | B4 | `00`; Db2 değişmez, VSAM yazılır, mesaj yok | **Düzenekte:** `DB2-LASTCHANGED` kurulmazsa boşluk kalır ve girdi (␣×26) **eşit** sayılır: 10-01 yolu, Db2 güncellenir (Kod gözlemleri 6). **Gerçek Db2'de:** FETCH `0` dönerse tahmin geçerli (`lastChanged` boş olamaz: `db2cre.jcl:168`); FETCH hata verirse sonuç yine `00`, VSAM yine yazılır, yalnızca `FETCH` mesajı eklenir (10-13) |
| 10-04 | B4, sıra (09-01 → 10-01) | `00`; Db2 10-01'in değerinde, VSAM bu isteğin değerinde | 10-01 çalışmadıysa eski zaman damgası eşleşir ve istek 10-01 gibi tam güncelleme yapar. B4 için 10-01'in son sütunu |
| 10-05 | B4, B8 | `81` çağırana ulaşır; Db2 değişmez | B4 yoksa: **düzenekte** senaryo FETCH'e `100` vermezse FETCH `0` döner, boş `DB2-LASTCHANGED` girdiyle eşleşmez ve akış yine LGUPVS01'e gider (`02` → CLOSE → `00`); senaryo READ'e RESP vermezse READ ve REWRITE `NORMAL` döner: `00`. **Gerçek VSAM'da** anahtar yok (`ksdspoly.txt:1-10`); READ'in RESP'i CICS'e bağlı (C). B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGV3` ile sonlanır; çağıranın `81`'i görmesi belirsiz (C) |
| 10-06 | B4, B8 | 10-05 ile aynı | 10-05 ile aynı |
| 10-07 | B4 | `00`; hiçbir şey güncellenmez | **Düzenekte:** `DB2-LASTCHANGED` kurulmazsa zaman damgası eşleşmez: 10-03 yolu, LGUPVS01 `H00000000020000000001` anahtarını bulamaz (`ksdspoly.txt:1-10`): `81` + ABEND `LGV3`. Senaryo `UPDATE HOUSE`'a `100` vermezse deyim `0` döner, `UPDATE POLICY` çalışır ve LGUPVS01 aynı anahtarı bulamaz: `81` + ABEND `LGV3` (Kod gözlemleri 20). **Gerçek Db2'de:** FETCH'in SQLCODE'u Db2'ye bağlı (Kod gözlemleri 5); `0` ise tahmin geçerli ([Db2 bilgisi] `UPDATE HOUSE` `100`) |
| 10-08 | B4 | `00` (`90` ezilir); iki mesaj | **Düzenekte:** senaryo SQLCODE vermezse `UPDATE HOUSE` `0` döner, 10-01 yolu. **Gerçek Db2'de:** girdi kurulamaz; `HOUSE` UPDATE'ini hataya düşürecek bir veri yolu yok |
| 10-09 | B4 | `00`; `HOUSE` güncellenir, `POLICY` güncellenmez | **Düzenekte:** senaryo SQLCODE vermezse 10-01 yolu. **Gerçek Db2'de:** girdi kurulamaz (veri yolu yok) |
| 10-10 | B4 | `00` (`90` ezilir); Db2 geri alınır ([CICS bilgisi]), VSAM yazılır, `CA-LASTCHANGED` girdideki gibi kalır | **Düzenekte:** senaryo SELECT'e hata vermezse 10-01 yolu. **Gerçek Db2'de:** girdi kurulamaz; kurulsa CLOSE'un SQLCODE'u ve `CA-LASTCHANGED`'in içeriği belirsiz (Kod gözlemleri 8) |
| 10-11 | B4 | `90`; hiçbir şey değişmez | **Düzenekte:** senaryo OPEN'a hata vermezse 10-01 yolu. **Gerçek Db2'de:** girdi örnek veriyle kurulamaz |
| 10-12 | B4 | `90`; hiçbir şey değişmez | 10-11 ile aynı |
| 10-13 | B4 | `00` (`90` ezilir); Db2 değişmez, VSAM yazılır | **Düzenekte:** senaryo FETCH'e hata vermezse 10-01 yolu (`DB2-LASTCHANGED` kurulmazsa 10-03 yolu). **Gerçek Db2'de:** FETCH'in 5/6 uyumsuzluğu bu yolu doğurabilir (Kod gözlemleri 5; C) |
| 10-14 | B4 | `00`; Db2 güncellenir, VSAM değişmez | **Düzenekte:** senaryo CLOSE'a `-501` vermezse 10-01 yolu. **Gerçek Db2'de:** girdi kurulamaz |
| 10-15 | B4 | `90`; Db2 güncellenmiş, VSAM değişmez | **Düzenekte:** senaryo CLOSE'a hata vermezse 10-01 yolu. **Gerçek Db2'de:** girdi kurulamaz |
| 10-16 | B4, B8 | `81` çağırana ulaşır; Db2 güncellemelerinin geri alınması C | **Düzenekte:** senaryo READ'e hata vermezse 10-01 yolu. **Gerçek VSAM'da:** girdi kurulamaz (anahtar var: `ksdspoly.txt:5`). B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGV3` ile sonlanır; çağıranın `81`'i görmesi belirsiz (C) |
| 10-17 | B4, B8 | `82` çağırana ulaşır; Db2 güncellemelerinin geri alınması C | **Düzenekte:** senaryo REWRITE'a hata vermezse 10-01 yolu. **Gerçek VSAM'da:** girdi kurulamaz (anahtar var: `ksdspoly.txt:5`). B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGV4` ile sonlanır; çağıranın `82`'yi görmesi belirsiz (C) |
| 10-18 | B4, sıra (09-01) | `00`; `payment` 0 kalır | 10-01 ile aynı; ödemenin yazılmaması her yolda geçerli |
| 10-19 | B4, B8, sıra (09-01) | `81` çağırana ulaşır; `POLICY` güncellenir, `HOUSE` güncellenmez; geri alma C | B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGV3` ile sonlanır; çağıranın `81`'i görmesi belirsiz (C). Sıra yoksa zaman damgası eşleşmez ve `POLICY` de güncellenmez. B4 yoksa: **düzenekte** `DB2-LASTCHANGED` için 10-01'in son sütunu; READ'e RESP verilmezse READ ve REWRITE `NORMAL` döner: `00`. **Gerçek VSAM'da** `h…` anahtarı yok (`ksdspoly.txt:1-10`); READ'in RESP'i CICS'e bağlı (C) |
| 10-20 | B4, B8 | `81` çağırana ulaşır; Db2 değişmez | B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGV3` ile sonlanır; çağıranın `81`'i görmesi belirsiz (C). B4 yoksa: **düzenekte** `DB2-LASTCHANGED` kurulmazsa boş kalır ve girdi (␣×26) eşit sayılır: `UPDATE POLICY` çalışır (10-19 gibi); READ'e RESP verilmezse READ ve REWRITE `NORMAL` döner: `00`. **Gerçek VSAM'da** boşlukla başlayan anahtar yok; READ'in RESP'i CICS'e bağlı (C) |
| 10-21 | B3, B4, B8, sıra (09-01) | `81` çağırana ulaşır; Db2'de müşteri 4'ün poliçe 6'sı güncellenir; geri alma C | B3 yoksa kesilmeyen değerin taşınması derleyiciye bağlı. B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGV3` ile sonlanır; çağıranın `81`'i görmesi belirsiz (C). B4 için 10-01 ve 10-19'un son sütunu |
| 10-23 | B8 | Düzenekte ABEND `LGCA` mesajı ve `GOBACK`; dönüş kodu yok, LGUPDB01 çağrılmaz, tek LGSTSQ LINK | B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır. Yan etki aynı: LGSTSQ LINK'i ABEND'den önce yapılır (`lgupol01.cbl:101-102`) |
| 10-25 | B1 | Tamponun 7-8. baytları `98` | Dönüş kodunun nereye düştüğü belirsiz; `EIBCALEN = 7` için 7. baytın `9` olması yine kesin |
| 10-27 | B1, B4, sıra (09-01) | 10-01 ile aynı | B1 yoksa belirsiz: `LENGTH(32500)` ile 158 baytlık alandan yapılan LINK'in ne okuyacağı ortama bağlı; B4 ve sıra için 10-01'in son sütunu |
| 10-29 | B2, B8 | `EM-SQLRC = +00000`; düzenekte ABEND `LGCA` mesajı ve `GOBACK` | B2 yoksa `EM-SQLRC` belirsiz. B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır; mesaj ABEND'den önce yazılır (`lgupdb01.cbl:185-186`) |
| 10-30 | B4 | `90`, `< 91` dalı | **Düzenekte:** senaryo OPEN'a hata vermezse akış FETCH ile devam eder; sonucu bu satır tahmin etmez. **Gerçek Db2'de:** girdi örnek veriyle kurulamaz |
| 10-32 | B4 | READ ve REWRITE `NORMAL`; `55` değişmez, kayıt yeniden yazılır | **Düzenekte:** senaryo READ'e `NORMAL` dışında RESP verirse `81` + ABEND `LGV3`, REWRITE'a verirse `82` + ABEND `LGV4` (B8 ile görünür; `ELSE` dalı). **Gerçek VSAM'da:** anahtar var (`ksdspoly.txt:5`); sonuç VSAM'ın RESP'ine bağlı, aynı iki yol |
| 10-33 | B4, B8 | `81` görünür, `< 91` dalı | B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGV3` ile sonlanır; çağıranın `81`'i görmesi belirsiz (C). B4 yoksa: **düzenekte** READ'e RESP verilmezse READ ve REWRITE `NORMAL` döner, dönüş kodu girdideki gibi kalır; **gerçek VSAM'da** `X…` anahtarı yok (`ksdspoly.txt:1-10`), READ'in RESP'i CICS'e bağlı (C) |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 10-05, 10-06, 10-16, 10-17, 10-19, 10-20, 10-21, 10-33 | Gerçek CICS'te çağıranın `81`/`82`'yi görmesi | [CICS bilgisi] ABEND (`lgupvs01.cbl:151`, `:164`) görevi sonlandırır ve zincirde abend yakalama yok; çağıranın kodu görmesi C. Düzenekte B8 ile kod görünür; bu satırlar ön koşulludur. |
| 10-16, 10-17 | Beklenen veri: Db2 güncellemelerinin geri alınması | ABEND sonrası iş biriminin geri alınması CICS/Db2'ye bağlı (C; Kod gözlemleri 12). |
| 10-19, 10-21 | Beklenen veri: `POLICY` güncellemesinin ABEND sonrasında geri alınması | 10-16 ile aynı neden. Kesin kısımlar: `99` ulaşmaz (10-19); hangi tabloların güncellendiği. |
| 10-22 | Dönüş kodu, beklenen veri | Sayısal olmayan DISPLAY içeriğinin `COMP` alana taşınmasının sonucu derleyiciye bağlı (C). |
| 10-26 | Dönüş kodu, beklenen veri | İstek kodunun bir kısmı COMMAREA'nın dışında; `WHEN OTHER` yolundan LGUPDB01'e boşluklu numaralarla gidilir (10-22'deki belirsizlik). |
| 10-28 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı (C). |
| 10-31 | Dönüş kodu, beklenen veri | LGUPVS01'de `EIBCALEN` kontrolü yok; COMMAREA olmadan erişimin sonucu kodda tanımlı değil. |
| 10-01, 10-02, 10-14, 10-15, 10-18, 10-27 | Beklenen veri: yalnızca yeni `CA-LASTCHANGED`'in karakter biçimi (26 bayt uzunluğu kesin) | Db2 `TIMESTAMP` değerinin 26 baytlık karaktere dönüşüm biçimi Db2'ye bağlı (C; `09-house-inquire.md` Kod gözlemleri 9). |
| 10-10 | Beklenen veri (gerçek Db2): `CA-LASTCHANGED`'in içeriği; VSAM'ın yazılması | Düzenekte B4 ile alan girdideki gibi kalır. Gerçek Db2'de başarısız `SELECT INTO`'nun alana ne yazacağı ve geri almadan sonraki CLOSE'un SQLCODE'u Db2'ye bağlı (C; Kod gözlemleri 8). Dönüş kodu da CLOSE'un SQLCODE'una bağlı: `0` ya da `-501` ise `00`, başka bir değer ise `90` (Kod gözlemleri 7, 8). |
| 10-15 | Beklenen veri: Db2 güncellemelerinin kalıcılığı | Kod geri alma istemez; görev sonunda kalıcı olmaları CICS'e bağlı (C). |
| 10-05, 10-08, 10-10, 10-11, 10-13, 10-30, 10-33 | Yan etki: `CA-ERROR-MSG` kuyruk kaydının son 9 baytı; LGUPVS01 mesajında son 11 bayt | Kaydın `WRITE-MSG`'nin dışından okunan baytları WORKING-STORAGE yerleşimine bağlı (Kod gözlemleri 15). Mesajın kendisi kesindir. |

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
   değişkenlerine ne koyacağı açık sorudur ve bu kuralla çelişir (`:135-138`). Bu dosyada B4'teki ek FETCH'in `DB2-LASTCHANGED`'ini ve
   başarılı `SELECT LASTCHANGED`'in yeni zaman damgasını doldurur; FETCH'i başarılı sayan bütün satırlar (Kod gözlemleri 5) ve yeni
   `CA-LASTCHANGED` döndüren satırlar ona dayanır. Ek kabul edilmezse `DB2-LASTCHANGED` boş kalır ve zaman damgası karşılaştırması
   girdiye göre değişir: "Ön koşullu satırlar" tablosundaki **düzenekte** durumu geçerli olur.
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
