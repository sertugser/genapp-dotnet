# 17 — Ticari poliçe sorgula (Inquire Commercial Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Ticari poliçe sorgula: Db2 `POLICY` ve `COMMERCIAL` tablolarının join'inden okunan poliçe COMMAREA'ya yazılır. Dört biçim vardır ve istek kodundan anlaşılır: `01ICOM` (müşteri + poliçe, tek SELECT), `02ICOM` (yalnızca poliçe, tek SELECT), `03ICOM` (müşteri, `Cust_Cursor`), `05ICOM` (posta kodu, `Zip_Cursor`). Hiçbir biçim veri değiştirmez. |
| Programlar (çağrı zinciri) | `LGIPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGIPDB01` (Db2 SELECT / cursor). Hata yollarında ikisi de `LGSTSQ`'ya LINK eder. `LGIPVS01` zincirde değildir: `LGIPOL01` yalnızca `LGIPDB01`'e (`lgipol01.cbl:91`), `LGIPDB01` yalnızca `LGSTSQ`'ya LINK eder (`lgipdb01.cbl:1010`, `lgipdb01.cbl:1018`, `lgipdb01.cbl:1024`). Programlar dört poliçe türüne de hizmet eder; yalnızca ticari kollar ve ortak kısımlar tahmin edildi, endowment/konut/motor kolları "başka tür, dal alınmaz" diye işaretlendi. |
| Okunan COBOL dosyaları | `base/src/lgipol01.cbl` (1-139), `base/src/lgipdb01.cbl` (1-1030), `base/src/lgstsq.cbl` (1-126), `base/src/lgcmarea.cpy` (1-103), `base/src/lgpolicy.cpy` (1-107). |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` (`db2cre.jcl:161-176`) ve `COMMERCIAL` (`db2cre.jcl:313-336`) tanımları; müşteri 1, 5, 6 (`db2cre.jcl:437`, `db2cre.jcl:573`, `db2cre.jcl:607`); poliçeler 10 (`db2cre.jcl:777-786`), 9 (`db2cre.jcl:801-810`), 7 (`db2cre.jcl:897-901`), 3 (`db2cre.jcl:969-970`); `HOUSE` 6 ve 7 (`db2cre.jcl:1051-1057`, `db2cre.jcl:1068`); `COMMERCIAL` 10 (`db2cre.jcl:1184-1203`) ve 9 (`db2cre.jcl:1227-1246`). `base/cntl/cobol.jcl` (`cobol.jcl:44-50`): derleme seçenekleri (B3). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `docs/adr/0004-legacy-test-harness.md` (`0004-legacy-test-harness.md:44-46`, `0004-legacy-test-harness.md:50-52`, `0004-legacy-test-harness.md:56-58`, `0004-legacy-test-harness.md:107-109`, `0004-legacy-test-harness.md:115-116`, `0004-legacy-test-harness.md:135-138`): yalnızca B4, B8, C grubu, [CICS bilgisi], Kod gözlemleri 2, 16 ve 18 ile Test sırası için. **Kodla doğrulanmadı.** |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-16 |
| Kaynak commit (`git rev-parse HEAD`) | `13d0e97c05c6a63cbb6ee9ad0af44cbce5e06d66` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır). Etiketsiz kaynaklar [kod]'dur.
- **[belge, kodla doğrulanmadı]**: `base/` ya da `docs/` altındaki açıklama metinleri (`.md`). Belgeye dayanan bir ifade kodda
  doğrulanmamıştır; hiçbir kesin tahmin yalnızca belgeye dayanmaz.
- **[Db2 bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen Db2 davranışı: satır yokken `SELECT`/`FETCH`/`DELETE` `SQLCODE = 100`
  döner; yabancı anahtar ihlali `SQLCODE = -530` döner ve satır eklenmez; `ON DELETE CASCADE` bağlı satırları siler. Sonuç koddan,
  davranış Db2'den gelir. Bu dosyada kullanımı: eşleşen satır olmayan `SELECT INTO` ve cursor'ın son satırından sonraki ya da boş
  sonuçtaki `FETCH` `100` döner.
- **[CICS bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen CICS davranışı. Bu dosyada üç madde: 1, 2 ve 4 (3. madde konut dosyası 10'a özgü: SYNCPOINT ROLLBACK);
  `16-commercial-add.md` ile aynı:
  1. `LINK … LENGTH(n)` ile çağrılan program `EIBCALEN = n` görür. Bu yüzden zincirdeki alt programın uzunluk kontrolü en dıştaki
     çağıranın uzunluğuna bakmaz.
  2. `EXEC CICS ABEND` görevi sonlandırır; `HANDLE ABEND` olmadığı için denetim çağırana dönmez. Görevin kesinleşmemiş Db2 ve
     VSAM değişiklikleri geri alınır (`TEMPLATE.md` madde 7; VSAM dosyaları `LOG(UNDO)` ile tanımlı: `adef121.jcl:20`, `:45`).
  4. LINK edilen programdaki `EXEC CICS RETURN` denetimi LINK eden programa, `LINK` deyiminden sonraki deyime döndürür; çağrılan
     programın COMMAREA'ya yazdıkları çağırana görünür.

  Düzenek bu davranışları ölçmez ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:107-109`); düzenekteki karşılıkları B1
  (LINK), B8 (ABEND) ve `RETURN` → `GOBACK` ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:44-46`).
- **C (belirsizlik grubu)**: sonucu kodda tanımlı olmayan, çalışma ortamına bağlı davranış: gerçek CICS (görev sonu, LINK hatası,
  COMMAREA ötesine yazma), gerçek Db2 (sıralamasız cursor'ın satır sırası, DATE ve
  TIMESTAMP'in karakter biçimi, NULL, hata sonrası cursor durumu, kodda yazmayan hata kodları) ya da derleyici. "(C)" işaretli hücre
  **belirsiz**dir: ne ön koşul koduyla ne [Db2 bilgisi] ya da [CICS bilgisi] ile kesinleştirilmiştir ve düzenekte ölçülmez.
  [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:107-109`.

Ön koşul kodları test düzeneğinin sağlaması gereken koşullardır. Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul
sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te en dıştaki çağıranın `LENGTH(32500)` tamponuyla geçirir; `EIBCALEN` yalnızca bildirilen uzunluktur, tamponun `EIBCALEN` ötesi boşlukla doldurulur. `EIBCALEN` ötesine yapılan yazmalar da bu tampona düşer ve çağıran onları görür. `EIBCALEN = 0` ile çağrılan programa da 32500 baytlık boşluk tamponu geçirilir ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:46`). **Bu dosyada:** 17-14, 17-31, 17-40; ayrıca belirsiz 17-41'in girdisinde. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. Bu dosyada: 17-03 (`lgipdb01.cbl:999`, hiçbir SQL deyimi çalışmadan). |
| B3 | Derleyici sayı taşımasında PICTURE'ın hane sayısına göre keser (10 haneli numara → 9 hane). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. Bu dosyada: 17-22, 17-32. |
| B4 | Düzenek satırda istenen `SQLCODE`/`RESP`'i üretir; **aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir. Kayıt ekleyip silebilir. Host değişkenlerine yalnızca satırda yazılan değeri koyar; başarısız deyimde host değişkenine yazmaz. Host değişkeni kullanmayan başarılı SQL deyiminin `0`, dosya komutunun `NORMAL` dönmesi de B4'e bağlıdır. [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:56-58`, `0004-legacy-test-harness.md:135-138`. **Bu dosyada:** başarılı `SELECT INTO` ve `FETCH`'in host değişkenlerine satırın sütun değerleri (`db2cre.jcl`'deki ya da satırda "seçilmiş değer" diye verilen değerler) yazılır; cursor'da satırlar senaryonun yazdığı sırayla gelir. Satırlar: 17-04, 17-09 … 17-12, 17-14 … 17-17, 17-20 … 17-40, 17-42. |
| B8 | Düzenekte `EXEC CICS ABEND` mesaj + `GOBACK`'e dönüşür: abend eden program çağırana döner ve COMMAREA'ya en son yazılan dönüş kodu görünür. [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:44-46`. [CICS bilgisi] 2: gerçek CICS'te ABEND görevi sonlandırır; orada çağıranın kodu görmesi belirsizdir (C). Bu dosyada: 17-02 ve 17-03 (`LGCA`; COMMAREA olmadığı için dönüş kodu görünmez, düzenekte görülen ABEND mesajı ve `GOBACK`'tir). |

(B5, B6 ve B7 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası, ticari görünüm** (`lgcmarea.cpy:10-13`, `lgcmarea.cpy:34-43`, `lgcmarea.cpy:77-94`). İki program da bu
   düzeni kullanır (`lgipol01.cbl:62-63`, `lgipdb01.cbl:217-220`).

   | Bayt | Alan | PIC | Satır |
   |---|---|---|---|
   | 1-6 | `CA-REQUEST-ID` | X(6) | `lgcmarea.cpy:10` |
   | 7-8 | `CA-RETURN-CODE` | 9(2) | `lgcmarea.cpy:11` |
   | 9-18 | `CA-CUSTOMER-NUM` | 9(10) | `lgcmarea.cpy:12` |
   | 19-28 | `CA-POLICY-NUM` | 9(10) | `lgcmarea.cpy:35` |
   | 29-38 | `CA-ISSUE-DATE` | X(10) | `lgcmarea.cpy:38` |
   | 39-48 | `CA-EXPIRY-DATE` | X(10) | `lgcmarea.cpy:39` |
   | 49-74 | `CA-LASTCHANGED` | X(26) | `lgcmarea.cpy:40` |
   | 75-84 | `CA-BROKERID` | 9(10) | `lgcmarea.cpy:41` |
   | 85-94 | `CA-BROKERSREF` | X(10) | `lgcmarea.cpy:42` |
   | 95-100 | `CA-PAYMENT` | 9(6) | `lgcmarea.cpy:43` |
   | 101-355 | `CA-B-Address` | X(255) | `lgcmarea.cpy:78` |
   | 356-363 | `CA-B-Postcode` | X(8) | `lgcmarea.cpy:79` |
   | 364-385 | `CA-B-Latitude`, `CA-B-Longitude` | X(11) + X(11) | `lgcmarea.cpy:80-81` |
   | 386-640 | `CA-B-Customer` | X(255) | `lgcmarea.cpy:82` |
   | 641-895 | `CA-B-PropType` | X(255) | `lgcmarea.cpy:83` |
   | 896-943 | dört risk: Peril 9(4) + Premium 9(8), sırasıyla Fire, Crime, Flood, Weather | | `lgcmarea.cpy:84-91` |
   | 944-947 | `CA-B-Status` | 9(4) | `lgcmarea.cpy:92` |
   | 948-1202 | `CA-B-RejectReason` | X(255) | `lgcmarea.cpy:93` |
   | 1203-32500 | `CA-B-FILLER` (ilk 5 baytı `'FINAL'` için) | X(31298) | `lgcmarea.cpy:94` |

   `DB2-POLICY-COMMON` (`lgpolicy.cpy:45-51`) 72 bayttır ve `CA-POLICY-COMMON`'a (29-100. baytlar) tek parça taşınır;
   `DB2-COMMERCIAL` (`lgpolicy.cpy:83-99`) 1102 bayttır ve `CA-COMMERCIAL(1:WS-COMM-LEN)`'e (101-1202. baytlar; `lgpolicy.cpy:22`)
   taşınır.

2. **Uzunluk hesabı ve `WS-REQUIRED-CA-LEN`.** 01ICOM ve 02ICOM `WS-CA-HEADERTRAILER-LEN = 33` (`lgipdb01.cbl:73`) ile
   `WS-FULL-COMM-LEN = 1174`'ü (`lgpolicy.cpy:27`) toplar: **1207** (`lgipdb01.cbl:684-685`, `lgipdb01.cbl:788-789`). 1207,
   `'FINAL'`'ın son baytıdır (`CA-B-FILLER(1:5)` = 1203-1207. baytlar). `WS-REQUIRED-CA-LEN` `VALUE +0` ile başlar
   (`lgipdb01.cbl:74`) ve yalnızca `ADD` ile kurulur. Kodla doğrulandı: her çağrıda MAINLINE'ın EVALUATE'inde (`lgipdb01.cbl:277-310`)
   tek bir kol çalışır, her paragraf bir kez PERFORM edilir (`lgipdb01.cbl:293`, `lgipdb01.cbl:297`, `lgipdb01.cbl:301`,
   `lgipdb01.cbl:305`) ve ticari yollarda başka `ADD` yoktur; bu yüzden tek çağrıda birikme olmaz. Aynı süreçte ikinci bir çağrı
   alanı `0`'a döndürmeden 1207 daha eklerdi; düzenek her durumu ayrı süreçte çalıştırır ([belge, kodla doğrulanmadı]
   `0004-legacy-test-harness.md:50-52`). Tahminler `VALUE` başlangıç durumuna göre yazıldı.

3. **Çağrı zinciri ve dönüş akışı.** LGIPOL01 yalnızca `EIBCALEN = 0`'ı denetler (`lgipol01.cbl:79-83`); uzunluk ve istek kodu
   kontrolü yoktur. `'00'` yazar (`lgipol01.cbl:86`), LGIPDB01'i `LENGTH(32500)` ile LINK eder (`lgipol01.cbl:91-94`) ve `RETURN` eder
   (`lgipol01.cbl:96`; [CICS bilgisi] 4). LGIPDB01'in MAINLINE'ı EVALUATE'ten sonra `End-Program` paragrafına düşer ve orada `RETURN`
   eder (`lgipdb01.cbl:310-313`); cursor hatalarında aynı paragrafa `PERFORM END-PROGRAM` ile gidilir (`lgipdb01.cbl:846`,
   `lgipdb01.cbl:858`, `lgipdb01.cbl:929`, `lgipdb01.cbl:941`). LINK'te `RESP` yoktur; LINK hatasının sonucu kodda tanımlı değildir (C).

4. **Sürpriz 1: tarihler `POLICY`'den değil, `COMMERCIAL`'dan gelir.** Dört biçimin dördünde de SELECT/FETCH `COMMERCIAL`'ın
   `RequestDate`, `StartDate` ve `RenewalDate` sütunlarını sırasıyla `DB2-LASTCHANGED`, `DB2-ISSUEDATE` ve `DB2-EXPIRYDATE`'e okur
   (01ICOM `lgipdb01.cbl:633-635` → `lgipdb01.cbl:652-654`; 02ICOM `lgipdb01.cbl:737-739` → `lgipdb01.cbl:758-760`; 03ICOM
   `lgipdb01.cbl:94-96` → `lgipdb01.cbl:870-872`; 05ICOM `lgipdb01.cbl:125-127` → `lgipdb01.cbl:952-954`). Bunlar
   `CA-POLICY-COMMON` ile `CA-LASTCHANGED`, `CA-ISSUE-DATE`, `CA-EXPIRY-DATE` olarak döner (`lgipdb01.cbl:703`). `POLICY`'nin
   `issueDate`, `expiryDate`, `lastChanged` sütunları hiç okunmaz. Örnek veride değerler farklıdır: poliçe 10 için dönen
   `'2011-08-25'`, `'2011-08-24'`, `'2011-08-23 10:44:34'` (`db2cre.jcl:1186`, `db2cre.jcl:1187`, `db2cre.jcl:1185`); `POLICY`'deki
   `'2011-08-22'`, `'2012-08-21'`, `'2011-08-22 12:13:01'` (`db2cre.jcl:779`, `db2cre.jcl:780`, `db2cre.jcl:782`). LGAPDB01 ile
   eklenen poliçede iki tablo aynı değeri taşır (`16-commercial-add.md` Kod gözlemleri 7), fark orada görünmez.

5. **Sürpriz 2: `BROKERID`, `BROKERSREF` ve `PAYMENT` okunmaz.** Dört ticari SELECT/FETCH'in sütun listesinde bu üç sütun yoktur
   (`lgipdb01.cbl:632-651`, `lgipdb01.cbl:735-755`, `lgipdb01.cbl:91-112`, `lgipdb01.cbl:122-143`). Ticari paragraflarda diğer
   türlerdeki `IF IND-BROKERID … MOVE DB2-BROKERID-INT TO DB2-BROKERID` blokları (örneğin `lgipdb01.cbl:397-402`) da yoktur.
   `DB2-POLICY` başta `INITIALIZE` edilir (`lgipdb01.cbl:245`): `DB2-BROKERID` 9(10) → sıfır, `DB2-BROKERSREF` X(10) → boşluk,
   `DB2-PAYMENT` 9(6) → sıfır (`lgpolicy.cpy:49-51`). Bu değerler `CA-POLICY-COMMON` ile çağırana gider: **dönüşte her zaman**
   `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000`. Örnek ticari poliçelerde değerler zaten 0/boştur
   (`db2cre.jcl:783-785`, `db2cre.jcl:807-809`); fark yalnızca sıfırdan farklı veriyle görünür (17-16).

6. **Sürpriz 3: uzunluk kontrolü veriden sonra gelir ve zincirde hiç tetiklenmez.** 01ICOM ve 02ICOM'da 1207 kontrolü yalnızca
   SELECT başarılıysa yapılır (`lgipdb01.cbl:680-691`, `lgipdb01.cbl:784-795`): kısa COMMAREA ile olmayan bir satır sorulursa `'98'`
   değil `'01'` döner (17-13). `'98'` yolunda veri COMMAREA'ya taşınmaz ve `'FINAL'` yazılmaz (`RETURN` önce gelir:
   `lgipdb01.cbl:691`, `lgipdb01.cbl:795`). Zincirde LGIPDB01 32500 görür ([CICS bilgisi] 1), LGIPOL01'in de uzunluk kontrolü yoktur:
   100 baytlık bir istemciye 1207 bayt yazılır (17-14). Aralık olarak: LGIPDB01'e doğrudan 28-1206 bayt → `'98'` (17-42); LGIPOL01'e 1-27 bayt →
   belirsiz (17-41). 03ICOM ve 05ICOM'da uzunluk kontrolü hiç yoktur (`lgipdb01.cbl:835-913`,
   `lgipdb01.cbl:919-990`).

7. **Sürpriz 4: 02ICOM müşteri girdisini yok sayar ve müşteri numarasını yalnızca başarıda geri yazar.** WHERE yalnızca poliçe
   numarasına bakar (`lgipdb01.cbl:778-781`); `CustomerNumber` SELECT edilir (`lgipdb01.cbl:736`, `lgipdb01.cbl:757`) ve
   `CA-Customer-Num`'a taşınır (`lgipdb01.cbl:797`). Taşıma uzunluk kontrolünün ELSE kolundadır: `'98'` yolunda müşteri numarası
   girdideki gibi kalır (17-21). 01ICOM ve 02ICOM `CA-POLICY-NUM`'a hiç yazmaz.

8. **Sürpriz 5: cursor biçimleri tek kayıt taşır.** 03ICOM ve 05ICOM her başarılı FETCH'te aynı alanların üzerine yazar:
   `CA-CUSTOMER-NUM`, `CA-POLICY-NUM`, `CA-POLICY-COMMON`, `CA-COMMERCIAL(1:1102)` (`lgipdb01.cbl:901-904`,
   `lgipdb01.cbl:983-986`). Birden çok satırda COMMAREA'da yalnızca son FETCH edilen satır kalır. Cursor'larda ORDER BY yoktur
   (`lgipdb01.cbl:89-118`, `lgipdb01.cbl:120-149`); gerçek Db2'de hangi satırın son olduğu belirsizdir (C), düzenekte senaryonun
   sırasıdır (B4). Bir kayıt boyunda (1202 bayt) tanımlı `ICOM-Record` (`lgipdb01.cbl:222`) ve `TSAREA` (`lgipdb01.cbl:21-27`) hiçbir
   deyimde kullanılmaz. Cursor biçimleri `'FINAL'` yazmaz ve boş sonuçta `'01'` döndürmez: ilk FETCH `100` dönerse hiçbir alan
   değişmez, dönüş kodu `lgipdb01.cbl:258`'deki `'00'` kalır.

9. **Sürpriz 6: 20 satır sınırı yalnızca 03ICOM'dadır.** `ICOM-Record-Count` sıfırlanır (`lgipdb01.cbl:838`) ve her başarılı FETCH'te
   artar; sayaç 20'yi geçince `SQLCODE` 17'ye zorlanır (`lgipdb01.cbl:906-909`). Bu, 21. satır COMMAREA'ya yazıldıktan **sonra** olur;
   döngü `SQLCODE > 0` ile biter (`lgipdb01.cbl:849-850`). 22 satırlı bir sonuçta 21 FETCH yapılır ve COMMAREA'da 21. satır kalır
   (17-26); 20 satırda 21. FETCH `100` döner ve 20. satır kalır (17-27). 05ICOM'da sayaç yoktur (`lgipdb01.cbl:946-990`): 22 satırda
   23 FETCH yapılır, 22. satır kalır (17-36).

10. **Sürpriz 7: negatif FETCH `SQLCODE`'u yutulur.** Döngü koşulu `UNTIL SQLCODE > 0`'dır (`lgipdb01.cbl:850`, `lgipdb01.cbl:933`);
    negatif kod döngüyü bitirmez, taşıma yapılmaz (`lgipdb01.cbl:891`, `lgipdb01.cbl:973`) ve hiçbir dönüş kodu ya da hata mesajı
    yazılmaz. Senaryo sonraki FETCH'te `100` verirse sonuç `'00'` olur (17-30, 17-39). Hata kalıcıysa döngü kodla sonlanmaz; düzenekte
    sonsuz döngü olur, gerçek Db2'de sonraki FETCH'lerin ne döneceği Db2'ye bağlıdır (C).

11. **Sürpriz 8: 05ICOM müşteri numarasını ezer ve posta kodunu COMMAREA'dan okur.** `Zip_Cursor`'ın tek koşulu
    `Commercial.Zipcode = :CA-B-POSTCODE`'dur (`lgipdb01.cbl:147-148`): host değişkeni doğrudan COMMAREA'nın 356-363. baytlarıdır.
    Müşteri filtresi yoktur; girdideki `CA-CUSTOMER-NUM` yok sayılır ve FETCH edilen satırın müşterisiyle ezilir
    (`lgipdb01.cbl:983`; 17-33). `EIBCALEN` 363'ten kısaysa posta kodu tampondan okunur (17-40).

12. **Sürpriz 9: yorum-kod çelişkileri.** Gerçek akış her yerde koda göre yazıldı.
    - `lgipdb01.cbl:271-273`: "only endowment policy inquires are supported"; kod yedi istek kodunu işler (`lgipdb01.cbl:279-305`).
    - `lgipdb01.cbl:711`, `lgipdb01.cbl:816`: "Non-zero SQLCODE from first SQL FETCH statement"; deyim `SELECT INTO`'dur.
    - `lgipdb01.cbl:818`: "invalid customer / policy number"; 02ICOM müşteriye bakmaz (Kod gözlemleri 7).
    - `lgipdb01.cbl:832-833`: "obtain matching customer policy records" (çoğul); yalnızca son kayıt kalır (Kod gözlemleri 8).
    - `lgipdb01.cbl:76-77`: `END-POLICY-POS` "end of data indicator" için; yalnızca endowment kullanır (`lgipdb01.cbl:385`,
      `lgipdb01.cbl:417`), ticari sabit `CA-B-FILLER(1:5)` kullanır.
    - `lgipdb01.cbl:200-202`: gösterge değişkenleri olmadan NULL sütun okunursa deyimin `-305` ile başarısız olacağını söyler; ticari
      deyimlerde hiç gösterge yoktur (Kod gözlemleri 17).
    - `lgipol01.cbl:8-9`: "Endowment, House or Motor"; ticari yoktur.
    - `lgipol01.cbl:123`: "Write 90 bytes or as much as we have of commarea"; LGIPOL01'de bu bloğa ulaşılamaz, tek çağrı yeri
      `EIBCALEN = 0` yoludur (`lgipol01.cbl:79-81`, `lgipol01.cbl:124`).

13. **İstek kodu büyük harfe çevrilmiş bir kopyada karşılaştırılır.** `MOVE FUNCTION UPPER-CASE(CA-REQUEST-ID) TO WS-REQUEST-ID`
    (`lgipdb01.cbl:275`, `lgipdb01.cbl:81`) ve EVALUATE `WS-REQUEST-ID` üzerindedir (`lgipdb01.cbl:277`). `CA-REQUEST-ID`'ye yazılmaz:
    `'01icom'` 01ICOM olarak işlenir ve dönüşte `'01icom'` kalır (17-10). LGIPOL01 istek koduna hiç bakmaz; tanınmayan kod (örneğin
    `'04ICOM'`, dört biçimin numaralarında boşluk vardır) OTHER koluna düşer (`lgipdb01.cbl:307-308`).

14. **Join ve `policyType`.** Dört biçim de `POLICY` ile `COMMERCIAL`'ı `POLICYNUMBER` üzerinden join eder (`lgipdb01.cbl:113-117`,
    `lgipdb01.cbl:144-148`, `lgipdb01.cbl:671-677`, `lgipdb01.cbl:777-781`); `POLICY.policyType` hiçbir deyimde okunmaz. Sonuç yalnızca
    `COMMERCIAL` satırının varlığına bağlıdır. Örnek veride ([kod] `db2cre.jcl`):

    | Anahtar | Eşleşen `COMMERCIAL` satırı | Kaynak |
    |---|---|---|
    | Müşteri 1 | 1: poliçe 10 | `db2cre.jcl:777-778`, `db2cre.jcl:1184` |
    | Müşteri 5 | 1: poliçe 9. Poliçe 3 (motor) `COMMERCIAL` satırı olmadığı için girmez | `db2cre.jcl:801-802`, `db2cre.jcl:1227`, `db2cre.jcl:969-970` |
    | Müşteri 6 | 0. Poliçe 7'nin `policyType`'ı `'C'`, ama `HOUSE` satırı var, `COMMERCIAL` satırı yok | `db2cre.jcl:897-901`, `db2cre.jcl:1068` |
    | Posta kodu `'SO212JN␣'` | 1: poliçe 10 | `db2cre.jcl:1189` |
    | Posta kodu `'BB104BX␣'` | 1: poliçe 9 | `db2cre.jcl:1232` |
    | Posta kodu `'SO211UP␣'` (`HOUSE` 6) | 0 | `db2cre.jcl:1057` |

15. **`'FINAL'` işareti yalnızca 01ICOM ve 02ICOM'un başarılı yolunda yazılır** (`lgipdb01.cbl:708`, `lgipdb01.cbl:813`): IF/ELSE'in
    dışındadır ama `'98'` kolu daha önce `RETURN` eder. Hata yollarında (`'01'`, `'90'`) ve cursor biçimlerinde yazılmaz.

16. **Hata mesajları.** LGIPOL01 `ERROR-MSG` 45 bayttır (`lgipol01.cbl:41-46`: `EM-DATE`, ␣, `EM-TIME`, `' LGIPOL01'`, `EM-VARIABLE`
    X(21)). LGIPDB01 `ERROR-MSG` 87 bayttır (`lgipdb01.cbl:48-60`: `EM-DATE`, ␣, `EM-TIME`, `' LGIPDB01'`, `' CNUM='`, `EM-CUSNUM`,
    `' PNUM='`, `EM-POLNUM`, `EM-SQLREQ` X(16), `' SQLCODE='`, `EM-SQLRC` +9(5)). `EM-CUSNUM` ve `EM-POLNUM` girdiden alınır
    (`lgipdb01.cbl:266-267`), FETCH'in yazdığı değerden değil. Ticari paragraflar `EM-SQLREQ`'e 19 baytlık `' SELECT Commercial '`
    taşır (`lgipdb01.cbl:629`, `lgipdb01.cbl:732`, `lgipdb01.cbl:837`, `lgipdb01.cbl:921`); alan 16 bayt olduğu için
    `' SELECT Commerci'` kalır. `CA-ERROR-MSG` 99 bayttır (`lgipol01.cbl:48-50`, `lgipdb01.cbl:62-64`): `'COMMAREA='` + COMMAREA'nın ilk
    90 baytı ya da `EIBCALEN` < 91 ise ilk `EIBCALEN` baytı (`lgipdb01.cbl:1015-1027`). Tarih ve saat düzenekte boştur ([belge, kodla
    doğrulanmadı] `0004-legacy-test-harness.md:108-109`); satırlarda tahmin edilmez. Kuyruk kaydının biçimi (satırlarda tahmin
    edilmez) `16-commercial-add.md` Kod gözlemleri 14 ile aynıdır: LGSTSQ 90 bayt kopyalar (`lgstsq.cbl:48`, `lgstsq.cbl:70`) ve
    `EIBCALEN + 5` bayt yazar (`lgstsq.cbl:71`, `lgstsq.cbl:90`, `lgstsq.cbl:94-99`, `lgstsq.cbl:105-111`); 99 baytlık mesajda bu
    95 baytlık `WRITE-MSG`'yi (`lgstsq.cbl:25-28`) 9 bayt aşar (C).

17. **Gösterge değişkeni yok; NULL sütun (C).** `COMMERCIAL`'ın sütunları NULL olabilir (`db2cre.jcl:315-333`), ama ticari deyimlerde
    gösterge değişkeni yoktur. NULL bir sütunun sonucu Db2'ye bağlıdır (C; yorum `lgipdb01.cbl:200-202` `-305` der); 01ICOM/02ICOM'da
    `'90'` yoluna, cursor'larda negatif FETCH yoluna (Kod gözlemleri 10) düşer. Satır yazılmadı.

18. **Tarih ve zaman damgası biçimi (C).** Host değişkenleri `X(10)` ve `X(26)`'dır (`lgpolicy.cpy:46-48`). B4 satırlarında host
    değişkenine `db2cre.jcl`'deki değer yazılır (`'2011-08-25'`; `'2011-08-23 10:44:34'` + ␣×7); gerçek Db2'nin DATE ve TIMESTAMP
    karakter biçimi kurulum seçeneklerine bağlıdır (C). Düzenek her durumu ayrı süreçte çalıştırır ([belge, kodla doğrulanmadı]
    `0004-legacy-test-harness.md:50-52`).

19. **Sayı alanlarının taşınması.** `CA-CUSTOMER-NUM` ve `CA-POLICY-NUM` 9(10), `DB2-CUSTOMERNUM-INT` ve `DB2-POLICYNUM-INT` S9(9)
    COMP'a taşınır (`lgipdb01.cbl:167-168`, `lgipdb01.cbl:263-264`); 10 haneli değer kesilir (B3). Hata mesajı kesmeden alır
    (`lgipdb01.cbl:266-267`). Cursor'larda `DB2-CUSTOMERNUM-INT` hem `Cust_Cursor`'ın girdisi (`lgipdb01.cbl:117`) hem FETCH'in hedefidir
    (`lgipdb01.cbl:868`); FETCH aynı müşteriyi yazar, sonucu değiştirmez.

## Tahmin tablosu

Ortak girdi: aksi yazılmadıkça çağrılan program LGIPOL01'dir, `EIBCALEN = 32500`, girdide sayılmayan COMMAREA alanları boşluktur
(`CA-B-FILLER` dahil). Ticari satır değerleri "17-04 girdi ve beklenen değerler" ile Kod gözlemleri 14'teki kaynaklardandır.

### Ortak

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 17-01 | `CA-REQUEST-ID = '04ICOM'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000010`. | `CA-RETURN-CODE = '99'` (`lgipdb01.cbl:308`) | COMMAREA'nın geri kalanı değişmez. SQL deyimi çalışmaz; LGSTSQ'ya LINK yok. | `lgipol01.cbl:86-96`, `lgipdb01.cbl:275-277`, `lgipdb01.cbl:307-313`; [CICS bilgisi] 1 ve 4 | Arayüz (COMMAREA) kuralı |
| 17-02 | LGIPOL01, `EIBCALEN = 0` (COMMAREA yok). | Yok (COMMAREA yok); `ABEND 'LGCA'` (`lgipol01.cbl:82`), düzenekte mesaj + `GOBACK` (B8) | LGSTSQ'ya 1 LINK: `ERROR-MSG`, 45 bayt: `' LGIPOL01'` + `' NO COMMAREA RECEIVED'`. LGIPDB01 çağrılmaz. | `lgipol01.cbl:79-83`, `lgipol01.cbl:107-122`, `lgipol01.cbl:124` | Arayüz (COMMAREA) kuralı |
| 17-03 | LGIPDB01 **doğrudan**, `EIBCALEN = 0`. | Yok (COMMAREA yok); `ABEND 'LGCA'` (`lgipdb01.cbl:254`), düzenekte mesaj + `GOBACK` (B8) | LGSTSQ'ya 1 LINK: `ERROR-MSG`, 87 bayt: `' LGIPDB01'` + `' NO COMMAREA RECEIVED'` + ␣×36 + `'+00000'` (`EM-SQLRC`, B2). SQL deyimi çalışmaz. | `lgipdb01.cbl:251-255`, `lgipdb01.cbl:999`, `lgipdb01.cbl:1010-1015` | Arayüz (COMMAREA) kuralı |

### 01ICOM (müşteri + poliçe, tek SELECT)

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 17-04 | `CA-REQUEST-ID = '01ICOM'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000010` (poliçe 10, müşteri 1: `db2cre.jcl:777-778`; `COMMERCIAL` 10 var: `db2cre.jcl:1184`). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. `CA-CUSTOMER-NUM`, `CA-POLICY-NUM` değişmez. `CA-ISSUE-DATE = '2011-08-25'`, `CA-EXPIRY-DATE = '2011-08-24'`, `CA-LASTCHANGED = '2011-08-23 10:44:34'` + ␣×7 (`COMMERCIAL`'dan). `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000`. Ticari alanlar `COMMERCIAL` 10'un değerleri; `CA-B-FILLER(1:5) = 'FINAL'` (alan alan: "17-04 girdi ve beklenen değerler"). LGSTSQ'ya LINK yok. | `lgipdb01.cbl:245`, `lgipdb01.cbl:291-293`, `lgipdb01.cbl:631-680`, `lgipdb01.cbl:684-689`, `lgipdb01.cbl:693-708`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-05 | 17-04 ile aynı, ama `CA-CUSTOMER-NUM = 0000000005` (poliçe 10'un müşterisi 1'dir: `db2cre.jcl:778`). | `CA-RETURN-CODE = '01'` (`lgipdb01.cbl:714`) | COMMAREA'nın geri kalanı değişmez; `'FINAL'` yok. LGSTSQ'ya LINK yok. | `lgipdb01.cbl:671-677`, `lgipdb01.cbl:712-714`; [Db2 bilgisi] (`100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-06 | 17-04 ile aynı, ama `CA-CUSTOMER-NUM = 0000000006`, `CA-POLICY-NUM = 0000000007` (poliçe 7: `policyType = 'C'`, `HOUSE` satırı var, `COMMERCIAL` satırı yok: `db2cre.jcl:897-901`, `db2cre.jcl:1068`). | `CA-RETURN-CODE = '01'` (`lgipdb01.cbl:714`) | 17-05 gibi. `policyType` okunmaz. | `lgipdb01.cbl:671-677`, `lgipdb01.cbl:712-714`; [Db2 bilgisi] (`100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-07 | 17-04 ile aynı, ama `CA-POLICY-NUM = 0000000000` (poliçe 0 yok: örnek poliçeler 1-10, yeni poliçeler 1000001'den başlar: `db2cre.jcl:162-163`). | `CA-RETURN-CODE = '01'` (`lgipdb01.cbl:714`) | 17-05 gibi. | `lgipdb01.cbl:676-677`, `lgipdb01.cbl:712-714`; [Db2 bilgisi] (`100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-08 | 17-04 ile aynı, ama `CA-CUSTOMER-NUM = 0000000000` (müşteri 0 yok: örnek müşteriler 1-10 `db2cre.jcl:437` … `db2cre.jcl:743`, yeni müşteriler 1000001'den: `db2cre.jcl:111-112`). | `CA-RETURN-CODE = '01'` (`lgipdb01.cbl:714`) | 17-05 gibi. | `lgipdb01.cbl:674-675`, `lgipdb01.cbl:712-714`; [Db2 bilgisi] (`100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-09 | 17-04 ile aynı; SELECT `SQLCODE = -904` (B4; `0` ve `100` dışındaki her değer aynı yolu izler). | `CA-RETURN-CODE = '90'` (`lgipdb01.cbl:717`) | COMMAREA'nın geri kalanı değişmez; `'FINAL'` yok. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' LGIPDB01'`, `' CNUM=0000000001'`, `' PNUM=0000000010'`, `' SELECT Commerci'`, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın 1-90. baytları (7-8. baytlar `'90'`). | `lgipdb01.cbl:629`, `lgipdb01.cbl:715-720`, `lgipdb01.cbl:997-1013`, `lgipdb01.cbl:1022-1027` | İş kuralı |
| 17-10 | 17-04 ile aynı, ama `CA-REQUEST-ID = '01icom'` (küçük harf). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | 17-04 ile aynı (B4); `CA-REQUEST-ID = '01icom'` kalır. | `lgipdb01.cbl:275-277`, `lgipdb01.cbl:291-293` | Arayüz (COMMAREA) kuralı |
| 17-11 | LGIPDB01 **doğrudan**, `EIBCALEN = 1206`; 17-04'ün girdisi (satır var). | `CA-RETURN-CODE = '98'` (`lgipdb01.cbl:690`) | B4 (SELECT `0`). Veri COMMAREA'ya taşınmaz: 29-1207. baytlar girdideki gibi; `'FINAL'` yok. LGSTSQ'ya LINK yok. | `lgipdb01.cbl:680`, `lgipdb01.cbl:684-691` | Arayüz (COMMAREA) kuralı |
| 17-12 | LGIPDB01 **doğrudan**, `EIBCALEN = 1207`; 17-04'ün girdisi. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | 17-04 ile aynı (B4); `'FINAL'` 1203-1207. baytlarda (COMMAREA'nın son 5 baytı). | `lgipdb01.cbl:689`, `lgipdb01.cbl:703-708` | Arayüz (COMMAREA) kuralı |
| 17-13 | LGIPDB01 **doğrudan**, `EIBCALEN = 1206`; 17-05'in girdisi (müşteri 5, poliçe 10: satır yok). | `CA-RETURN-CODE = '01'` (`lgipdb01.cbl:714`) | COMMAREA'nın geri kalanı değişmez. Uzunluk kontrolüne gelinmez. | `lgipdb01.cbl:680`, `lgipdb01.cbl:712-714`; [Db2 bilgisi] (`100`) | Arayüz (COMMAREA) kuralı |
| 17-14 | LGIPOL01, `EIBCALEN = 100`; ilk 100 bayt 17-04'ün girdisi. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | 17-04 ile aynı (B4); 101-1207. baytlar (ticari alanlar ve `'FINAL'`) 100 baytlık alanın ötesine, tampona yazılır (B1; gerçek CICS'te sonucu C). | `lgipol01.cbl:79-94`, `lgipdb01.cbl:689`, `lgipdb01.cbl:704-708`; [CICS bilgisi] 1 ve 4 | Arayüz (COMMAREA) kuralı |
| 17-15 | LGIPDB01 **doğrudan**, `EIBCALEN = 28`; ilk 28 bayt 17-04'ün girdisi; SELECT `SQLCODE = -904` (B4). | `CA-RETURN-CODE = '90'` (`lgipdb01.cbl:717`) | LGSTSQ'ya 2 LINK: (1) `ERROR-MSG` 17-09 gibi; (2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın 1-28. baytları + ␣×62 (`EIBCALEN < 91` kolu). | `lgipdb01.cbl:715-720`, `lgipdb01.cbl:1015-1021` | Arayüz (COMMAREA) kuralı |
| 17-16 | 17-04 ile aynı; `POLICY` 10'da `brokerId = 1234`, `brokersReference = 'BRK0000001'`, `payment = 500` (seçilmiş değerler, kaynak yok; PIC sınırları içinde: `lgcmarea.cpy:41-43`). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | 17-04 ile aynı (B4): `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000`. `POLICY`'deki değerler okunmaz; düzenekte de gerçek Db2'de de sonuç değişmez. | `lgipdb01.cbl:245`, `lgipdb01.cbl:632-651`, `lgipdb01.cbl:703`; `lgpolicy.cpy:49-51` | İş kuralı |

### 02ICOM (yalnızca poliçe, tek SELECT)

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 17-17 | `CA-REQUEST-ID = '02ICOM'`, `CA-CUSTOMER-NUM = 0000000001` (poliçe 9'un müşterisi değil), `CA-POLICY-NUM = 0000000009` (poliçe 9, müşteri 5: `db2cre.jcl:801-802`; `COMMERCIAL` 9: `db2cre.jcl:1227`). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. `CA-CUSTOMER-NUM = 0000000005` (geri yazılır); `CA-POLICY-NUM` değişmez. `CA-ISSUE-DATE = '2011-08-01'`, `CA-EXPIRY-DATE = '2011-07-30'`, `CA-LASTCHANGED = '2011-07-17 13:13:09'` + ␣×7 (`db2cre.jcl:1229`, `db2cre.jcl:1230`, `db2cre.jcl:1228`). `CA-BROKERID = 0000000000`, `CA-BROKERSREF` = ␣×10, `CA-PAYMENT = 000000`. Ticari alanlar `COMMERCIAL` 9'un değerleri (`db2cre.jcl:1231-1246`): `'TURF MOOR'`, `'BB104BX␣'`, `'53.78847'` + ␣×3, `'-2.230325'` + ␣×2, `'Burnley Football Club'`, `'STADIUM'`, `0001`/`00044000`, `0002`/`00025000`, `0003`/`00015000`, `0004`/`00006000`, `0001`, `'ACTIVE'` (metinler sağdan boşlukla). `CA-B-FILLER(1:5) = 'FINAL'`. | `lgipdb01.cbl:295-297`, `lgipdb01.cbl:734-784`, `lgipdb01.cbl:788-793`, `lgipdb01.cbl:797-813`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-18 | 17-17 ile aynı, ama `CA-POLICY-NUM = 0000000007` (poliçe 7: `COMMERCIAL` satırı yok: `db2cre.jcl:897-901`, `db2cre.jcl:1068`). | `CA-RETURN-CODE = '01'` (`lgipdb01.cbl:819`) | COMMAREA'nın geri kalanı değişmez (`CA-CUSTOMER-NUM` girdideki gibi). LGSTSQ'ya LINK yok. | `lgipdb01.cbl:777-781`, `lgipdb01.cbl:817-819`; [Db2 bilgisi] (`100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-19 | 17-17 ile aynı, ama `CA-POLICY-NUM = 0000000003` (poliçe 3, motor, müşteri 5: `db2cre.jcl:969-973`; `COMMERCIAL` satırı yok). | `CA-RETURN-CODE = '01'` (`lgipdb01.cbl:819`) | 17-18 gibi. | `lgipdb01.cbl:777-781`, `lgipdb01.cbl:817-819`; [Db2 bilgisi] (`100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-20 | 17-17 ile aynı; SELECT `SQLCODE = -904` (B4; `0` ve `100` dışındaki her değer aynı yolu izler). | `CA-RETURN-CODE = '90'` (`lgipdb01.cbl:822`) | COMMAREA'nın geri kalanı değişmez. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' CNUM=0000000001'`, `' PNUM=0000000009'`, `' SELECT Commerci'`, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG` (7-8. baytlar `'90'`). | `lgipdb01.cbl:732`, `lgipdb01.cbl:820-825`, `lgipdb01.cbl:997-1013`, `lgipdb01.cbl:1022-1027` | İş kuralı |
| 17-21 | LGIPDB01 **doğrudan**, `EIBCALEN = 1206`; 17-17'nin girdisi. | `CA-RETURN-CODE = '98'` (`lgipdb01.cbl:794`) | B4 (SELECT `0`). `CA-CUSTOMER-NUM = 0000000001` kalır (geri yazma ELSE kolunda); veri taşınmaz; `'FINAL'` yok. | `lgipdb01.cbl:784`, `lgipdb01.cbl:788-797` | Arayüz (COMMAREA) kuralı |
| 17-22 | 17-17 ile aynı, ama `CA-POLICY-NUM = 1000000009`. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | `DB2-POLICYNUM-INT = 9` (B3) → poliçe 9 bulunur; 17-17 ile aynı veri (B4), `CA-POLICY-NUM = 1000000009` kalır. | `lgipdb01.cbl:264`, `lgipdb01.cbl:780-781`, `lgipdb01.cbl:797` | Arayüz (COMMAREA) kuralı |

### 03ICOM (müşteri, `Cust_Cursor`)

Çok satırlı satırlarda (17-25, 17-26, 17-27) satırlar B4 ile kurulur. Satır 1 poliçe 9'dur (`db2cre.jcl:801-802`,
`db2cre.jcl:1227-1246`). Satır *k* (*k* ≥ 2): `PolicyNumber = 1000000 + (k − 1)`, `Customer = 'SATIR-kk'` (iki haneli *k*; seçilmiş
değerler, kaynak yok), diğer bütün sütunlar ve müşteri numarası (5) poliçe 9 ile aynı. Senaryo satırları bu sırayla verir.

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 17-23 | `CA-REQUEST-ID = '03ICOM'`, `CA-CUSTOMER-NUM = 0000000005`, `CA-POLICY-NUM = 0000000000`. Müşteri 5'in tam bir `COMMERCIAL` satırı var (poliçe 9; poliçe 3 motor: Kod gözlemleri 14). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. 2 FETCH (2.'si `100`). `CA-CUSTOMER-NUM = 0000000005`, `CA-POLICY-NUM = 0000000009`; ortak alanlar ve ticari alanlar 17-17'deki gibi (`COMMERCIAL` 9). `CA-B-FILLER` değişmez (`'FINAL'` yok). LGSTSQ'ya LINK yok. | `lgipdb01.cbl:299-301`, `lgipdb01.cbl:838-854`, `lgipdb01.cbl:865-904`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-24 | 17-23 ile aynı, ama `CA-CUSTOMER-NUM = 0000000006` (müşteri 6'nın `COMMERCIAL` satırı yok: poliçe 7, Kod gözlemleri 14). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4 (OPEN, CLOSE `0`). 1 FETCH (`100`, [Db2 bilgisi]). COMMAREA'nın geri kalanı değişmez; `'01'` yok, `'FINAL'` yok. | `lgipdb01.cbl:849-850`, `lgipdb01.cbl:891`, `lgipdb01.cbl:852-859`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-25 | 17-23 ile aynı; senaryo 2 satır verir (satır 1: poliçe 9; satır 2: poliçe 1000001, `'SATIR-02'`). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. 3 FETCH. COMMAREA'da yalnızca satır 2: `CA-POLICY-NUM = 0001000001`, `CA-B-Customer = 'SATIR-02'` + ␣×247, diğer alanlar 17-23 gibi. Gerçek Db2'de hangi satırın son geleceği belirsiz (C). | `lgipdb01.cbl:89-118`, `lgipdb01.cbl:891-904` | İş kuralı |
| 17-26 | 17-23 ile aynı; senaryo 22 satır verir. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. **21 FETCH**; 22. satır FETCH edilmez. COMMAREA'da satır 21: `CA-POLICY-NUM = 0001000020`, `CA-B-Customer = 'SATIR-21'` + ␣×247. CLOSE çalışır. Gerçek Db2'de sıra C. | `lgipdb01.cbl:838`, `lgipdb01.cbl:849-850`, `lgipdb01.cbl:906-909` | İş kuralı |
| 17-27 | 17-23 ile aynı; senaryo 20 satır verir. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. 21 FETCH (21.'si `100`). COMMAREA'da satır 20: `CA-POLICY-NUM = 0001000019`, `CA-B-Customer = 'SATIR-20'` + ␣×247. Sayaç 20'yi geçmez. Gerçek Db2'de sıra C. | `lgipdb01.cbl:906-909`, `lgipdb01.cbl:891`, `lgipdb01.cbl:850` | İş kuralı |
| 17-28 | 17-23 ile aynı; OPEN `SQLCODE = -904` (B4; `0` dışındaki her değer aynı yolu izler). | `CA-RETURN-CODE = '89'` (`lgipdb01.cbl:844`) | FETCH ve CLOSE çalışmaz; COMMAREA'nın geri kalanı değişmez. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' CNUM=0000000005'`, `' PNUM=0000000000'`, `' SELECT Commerci'`, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG` (7-8. baytlar `'89'`). | `lgipdb01.cbl:837`, `lgipdb01.cbl:840-847`, `lgipdb01.cbl:312-313` | İş kuralı |
| 17-29 | 17-23 ile aynı (1 satır); CLOSE `SQLCODE = -904` (B4). | `CA-RETURN-CODE = '88'` (`lgipdb01.cbl:856`) | COMMAREA'da 17-23'teki satır (poliçe 9). LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' CNUM=0000000005'`, `' PNUM=0000000000'` (girdi), `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG`: `'COMMAREA='` + 1-90. baytlar (7-8. baytlar `'88'`, 9-28. baytlar `0000000005` ve `0000000009`). | `lgipdb01.cbl:852-859`, `lgipdb01.cbl:266-267`, `lgipdb01.cbl:1022-1027` | İş kuralı |
| 17-30 | 17-23 ile aynı; 1. FETCH `SQLCODE = -904`, 2. FETCH `100` (B4). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | 2 FETCH; COMMAREA'nın geri kalanı değişmez; hata mesajı yok (negatif kod kontrol edilmez). Gerçek Db2'de hatadan sonraki FETCH'in ne döneceği belirsiz (C). | `lgipdb01.cbl:849-850`, `lgipdb01.cbl:891` | İş kuralı |
| 17-31 | LGIPDB01 **doğrudan**, `EIBCALEN = 28`; 17-23'ün girdisi. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. Uzunluk kontrolü yok: 9-28. baytlar `0000000005`, `0000000009`; 29-1202. baytlar (poliçe 9'un ortak ve ticari alanları) 28 baytlık alanın ötesine, tampona yazılır (B1; gerçek CICS'te sonucu C). | `lgipdb01.cbl:835-913` | Arayüz (COMMAREA) kuralı |
| 17-32 | 17-23 ile aynı, ama `CA-CUSTOMER-NUM = 1000000001`. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | `DB2-CUSTOMERNUM-INT = 1` (B3) → müşteri 1'in satırı (poliçe 10; B4). `CA-CUSTOMER-NUM = 0000000001` (FETCH ezer), `CA-POLICY-NUM = 0000000010`, ortak ve ticari alanlar 17-04 gibi; `'FINAL'` yok. | `lgipdb01.cbl:263`, `lgipdb01.cbl:116-117`, `lgipdb01.cbl:901-904` | Arayüz (COMMAREA) kuralı |

### 05ICOM (posta kodu, `Zip_Cursor`)

Çok satırlı satırlarda (17-35, 17-36) satırlar 03ICOM'daki gibi kurulur; bütün satırların posta kodu poliçe 9'unki (`'BB104BX'`,
`db2cre.jcl:1232`) olduğu için ikisi de bu posta koduyla sorgulanır.

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 17-33 | `CA-REQUEST-ID = '05ICOM'`, `CA-CUSTOMER-NUM = 0000000005`, `CA-POLICY-NUM = 0000000000`, `CA-B-Postcode = 'SO212JN␣'` (`db2cre.jcl:1189`). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. 2 FETCH. Poliçe 10'un satırı: `CA-CUSTOMER-NUM = 0000000001` (girdideki 5 ezilir), `CA-POLICY-NUM = 0000000010`, ortak ve ticari alanlar 17-04 gibi; `'FINAL'` yok. | `lgipdb01.cbl:303-305`, `lgipdb01.cbl:144-148`, `lgipdb01.cbl:923-937`, `lgipdb01.cbl:947-986`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 17-34 | 17-33 ile aynı, ama `CA-B-Postcode = 'SO211UP␣'` (`HOUSE` 6'nın posta kodu: `db2cre.jcl:1057`; `COMMERCIAL`'da yok). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4 (OPEN, CLOSE `0`). 1 FETCH (`100`, [Db2 bilgisi]). COMMAREA'nın geri kalanı değişmez. | `lgipdb01.cbl:144-148`, `lgipdb01.cbl:932-933`, `lgipdb01.cbl:973` | İş kuralı |
| 17-35 | 17-33 ile aynı, ama `CA-B-Postcode = 'BB104BX␣'`; senaryo 2 satır verir (satır 1: poliçe 9; satır 2: poliçe 1000001, `'SATIR-02'`). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. 3 FETCH. COMMAREA'da yalnızca satır 2: `CA-CUSTOMER-NUM = 0000000005`, `CA-POLICY-NUM = 0001000001`, `CA-B-Customer = 'SATIR-02'` + ␣×247. Gerçek Db2'de sıra C. | `lgipdb01.cbl:120-149`, `lgipdb01.cbl:973-986` | İş kuralı |
| 17-36 | 17-35 ile aynı; senaryo 22 satır verir. | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4. **23 FETCH** (sınır yok; 23.'sü `100`). COMMAREA'da satır 22: `CA-POLICY-NUM = 0001000021`, `CA-B-Customer = 'SATIR-22'` + ␣×247. Gerçek Db2'de sıra C. | `lgipdb01.cbl:932-933`, `lgipdb01.cbl:946-990` | İş kuralı |
| 17-37 | 17-33 ile aynı; OPEN `SQLCODE = -904` (B4). | `CA-RETURN-CODE = '89'` (`lgipdb01.cbl:927`) | FETCH ve CLOSE çalışmaz; COMMAREA'nın geri kalanı değişmez. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' CNUM=0000000005'`, `' PNUM=0000000000'`, `' SELECT Commerci'`, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG` (7-8. baytlar `'89'`). | `lgipdb01.cbl:921`, `lgipdb01.cbl:923-930`, `lgipdb01.cbl:312-313` | İş kuralı |
| 17-38 | 17-33 ile aynı (1 satır); CLOSE `SQLCODE = -904` (B4). | `CA-RETURN-CODE = '88'` (`lgipdb01.cbl:939`) | COMMAREA'da 17-33'teki satır (poliçe 10, müşteri 1). LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' CNUM=0000000005'` (girdi), `' PNUM=0000000000'`, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG` (7-8. baytlar `'88'`, 9-28. baytlar `0000000001` ve `0000000010`). | `lgipdb01.cbl:935-942`, `lgipdb01.cbl:266-267`, `lgipdb01.cbl:1022-1027` | İş kuralı |
| 17-39 | 17-33 ile aynı; 1. FETCH `SQLCODE = -904`, 2. FETCH `100` (B4). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | 2 FETCH; COMMAREA'nın geri kalanı değişmez; hata mesajı yok. Gerçek Db2'de hatadan sonraki FETCH'in ne döneceği belirsiz (C). | `lgipdb01.cbl:932-933`, `lgipdb01.cbl:973` | İş kuralı |
| 17-40 | LGIPDB01 **doğrudan**, `EIBCALEN = 100`; ilk 100 bayt 17-33'ün girdisi; `CA-B-Postcode` (356-363. baytlar) tamponda boşluk (B1). | `CA-RETURN-CODE = '00'` (`lgipdb01.cbl:258`) | B4 (OPEN, CLOSE `0`). `Zip_Cursor`'ın host değişkeni ␣×8; boş posta kodlu `COMMERCIAL` satırı yok (`db2cre.jcl:1189`, `db2cre.jcl:1232`), 1 FETCH (`100`, [Db2 bilgisi]). COMMAREA'nın geri kalanı değişmez. | `lgipdb01.cbl:147-148`, `lgipdb01.cbl:919-944` | Arayüz (COMMAREA) kuralı |
| 17-41 | LGIPOL01 çağrılır. `EIBCALEN` 1 ile 27 arasında; `CA-REQUEST-ID = '01ICOM'` (`EIBCALEN` ≥ 6 ise), `EIBCALEN` ötesi tamponda boşluk (B1). | belirsiz | belirsiz | `lgipol01.cbl:79-94`, `lgipdb01.cbl:263-264`, `lgipdb01.cbl:275-277` | Arayüz (COMMAREA) kuralı |
| 17-42 | LGIPDB01 **doğrudan** çağrılır. `EIBCALEN` 28 ile 1206 arasında (sınır değerler: `28` ve `1206`; 17-11'in genellemesi). İlk 28 bayt 17-04'ün girdisi (satır var). | `CA-RETURN-CODE = '98'` (`lgipdb01.cbl:690`) | B4 (SELECT `0`). Veri COMMAREA'ya taşınmaz; `'FINAL'` yok; LGSTSQ'ya LINK yok. | `lgipdb01.cbl:263-264`, `lgipdb01.cbl:680`, `lgipdb01.cbl:684-691` | Arayüz (COMMAREA) kuralı |

## 17-04 girdi ve beklenen değerler

COMMAREA'nın alan alan girdisi ve dönüşte beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir. Dönüş değerleri B4'e göre
(host değişkenlerine `db2cre.jcl`'deki sütun değerleri yazılır); tarih ve zaman damgası biçimi C (Kod gözlemleri 18).

| COMMAREA alanı | Girdi | Dönüşte beklenen | Dönüş değerinin kaynağı | Alanı kullanan / dolduran COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` | `'01ICOM'` | değişmez | – | `lgipdb01.cbl:275`, `lgipdb01.cbl:291` |
| `CA-RETURN-CODE` | ␣×2 | `'00'` | – | `lgipol01.cbl:86`, `lgipdb01.cbl:258` |
| `CA-CUSTOMER-NUM` | `0000000001` | değişmez | girdi: `db2cre.jcl:778` | `lgipdb01.cbl:263`, `lgipdb01.cbl:674-675` |
| `CA-POLICY-NUM` | `0000000010` | değişmez | girdi: `db2cre.jcl:777` | `lgipdb01.cbl:264`, `lgipdb01.cbl:676-677` |
| `CA-ISSUE-DATE` | ␣×10 | `'2011-08-25'` | `COMMERCIAL.StartDate`: `db2cre.jcl:1186` | `lgipdb01.cbl:634`, `lgipdb01.cbl:653`, `lgipdb01.cbl:703` |
| `CA-EXPIRY-DATE` | ␣×10 | `'2011-08-24'` | `COMMERCIAL.RenewalDate`: `db2cre.jcl:1187` | `lgipdb01.cbl:635`, `lgipdb01.cbl:654`, `lgipdb01.cbl:703` |
| `CA-LASTCHANGED` | ␣×26 | `'2011-08-23 10:44:34'` + ␣×7 | `COMMERCIAL.RequestDate`: `db2cre.jcl:1185` | `lgipdb01.cbl:633`, `lgipdb01.cbl:652`, `lgipdb01.cbl:703` |
| `CA-BROKERID` | ␣×10 | `0000000000` | `INITIALIZE`: `lgipdb01.cbl:245`, `lgpolicy.cpy:49` | `lgipdb01.cbl:703` |
| `CA-BROKERSREF` | ␣×10 | ␣×10 | `INITIALIZE`: `lgipdb01.cbl:245`, `lgpolicy.cpy:50` | `lgipdb01.cbl:703` |
| `CA-PAYMENT` | ␣×6 | `000000` | `INITIALIZE`: `lgipdb01.cbl:245`, `lgpolicy.cpy:51` | `lgipdb01.cbl:703` |
| `CA-B-Address` | ␣×255 | `'HURSLEY PARK'` + ␣×243 | `db2cre.jcl:1188` | `lgipdb01.cbl:655`, `lgipdb01.cbl:704` |
| `CA-B-Postcode` | ␣×8 | `'SO212JN'` + ␣×1 | `db2cre.jcl:1189` | `lgipdb01.cbl:656`, `lgipdb01.cbl:704` |
| `CA-B-Latitude` | ␣×11 | `'51.026701'` + ␣×2 | `db2cre.jcl:1190` | `lgipdb01.cbl:657`, `lgipdb01.cbl:704` |
| `CA-B-Longitude` | ␣×11 | `'-1.398903'` + ␣×2 | `db2cre.jcl:1191` | `lgipdb01.cbl:658`, `lgipdb01.cbl:704` |
| `CA-B-Customer` | ␣×255 | `'IBM'` + ␣×252 | `db2cre.jcl:1192` | `lgipdb01.cbl:659`, `lgipdb01.cbl:704` |
| `CA-B-PropType` | ␣×255 | `'OFFICES'` + ␣×248 | `db2cre.jcl:1193` | `lgipdb01.cbl:660`, `lgipdb01.cbl:704` |
| `CA-B-FirePeril` | ␣×4 | `0003` | `db2cre.jcl:1194` | `lgipdb01.cbl:661`, `lgipdb01.cbl:693` |
| `CA-B-FirePremium` | ␣×8 | `00008000` | `db2cre.jcl:1195` | `lgipdb01.cbl:662`, `lgipdb01.cbl:694` |
| `CA-B-CrimePeril` | ␣×4 | `0003` | `db2cre.jcl:1196` | `lgipdb01.cbl:663`, `lgipdb01.cbl:695` |
| `CA-B-CrimePremium` | ␣×8 | `00025000` | `db2cre.jcl:1197` | `lgipdb01.cbl:664`, `lgipdb01.cbl:696` |
| `CA-B-FloodPeril` | ␣×4 | `0005` | `db2cre.jcl:1198` | `lgipdb01.cbl:665`, `lgipdb01.cbl:697` |
| `CA-B-FloodPremium` | ␣×8 | `00002000` | `db2cre.jcl:1199` | `lgipdb01.cbl:666`, `lgipdb01.cbl:698` |
| `CA-B-WeatherPeril` | ␣×4 | `0004` | `db2cre.jcl:1200` | `lgipdb01.cbl:667`, `lgipdb01.cbl:699` |
| `CA-B-WeatherPremium` | ␣×8 | `00005000` | `db2cre.jcl:1201` | `lgipdb01.cbl:668`, `lgipdb01.cbl:700` |
| `CA-B-Status` | ␣×4 | `0001` | `db2cre.jcl:1202` | `lgipdb01.cbl:669`, `lgipdb01.cbl:701` |
| `CA-B-RejectReason` | ␣×255 | `'ACTIVE'` + ␣×249 | `db2cre.jcl:1203` | `lgipdb01.cbl:670`, `lgipdb01.cbl:704` |
| `CA-B-FILLER(1:5)` | ␣×5 | `'FINAL'` | sabit | `lgipdb01.cbl:708` |
| `CA-B-FILLER(6:31293)` | ␣×31293 | değişmez | – | kullanılmaz |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları. "Başka tür" maddelerine tahmin yazılmadı; bu maddeler,
dalın alınmadığı bir ticari teste bağlandı. Ulaşılamayan dallar "kapsam dışı: ulaşılamaz" gerekçesiyle tabloda tutuldu.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| 1 | `lgipol01.cbl:79-83`: `EIBCALEN = 0` → mesaj + `ABEND 'LGCA'` (ortak) | 17-02 | |
| 2 | `lgipol01.cbl:86`: `'00'` (ortak) | 17-01, 17-04 | |
| 3 | `lgipol01.cbl:79-94`: uzunluk ve istek kodu kontrolü yok (ortak) | 17-01, 17-14, 17-41 | Kod gözlemleri 3 |
| 4 | `lgipol01.cbl:91-94`: `LINK LGIPDB01 LENGTH(32500)` (ortak) | 17-04 | LINK hatası kapsam dışı: kodda dal yok, sonuç C. |
| 5 | `lgipol01.cbl:96`: RETURN (ortak) | 17-04, 17-05, 17-09 | [CICS bilgisi] 4 |
| 6 | `lgipol01.cbl:107-122`: WRITE-ERROR-MESSAGE, `ERROR-MSG` 45 bayt (ortak) | 17-02 | |
| 7 | `lgipol01.cbl:124-138`: `EIBCALEN > 0` / `< 91` (ortak) | yanlış kolu 17-02 | Doğru kolu (`lgipol01.cbl:125-137`) kapsam dışı: ulaşılamaz; tek çağrı yeri `EIBCALEN = 0` yolu. |
| 8 | `lgipdb01.cbl:243-245`: `INITIALIZE`; `DB2-BROKERID` 0, `DB2-BROKERSREF` ␣, `DB2-PAYMENT` 0 (ortak) | 17-04, 17-16 | Kod gözlemleri 5 |
| 9 | `lgipdb01.cbl:251-255`: `EIBCALEN = 0` → `ABEND 'LGCA'` (ortak) | 17-03 | Zincirde ulaşılmaz (madde 1 önce yakalar); yalnızca doğrudan çağrı. |
| 10 | `lgipdb01.cbl:258`: `'00'` (ortak) | 17-04, 17-24 | |
| 11 | `lgipdb01.cbl:263-264`: müşteri/poliçe → S9(9) COMP (ortak) | 17-22, 17-32, 17-41 | B3 |
| 12 | `lgipdb01.cbl:266-267`: `EM-CUSNUM`, `EM-POLNUM` ← girdi (ortak) | 17-09, 17-20, 17-28, 17-29, 17-38 | |
| 13 | `lgipdb01.cbl:275`: UPPER-CASE kopyaya; `CA-REQUEST-ID` değişmez (ortak) | 17-10 | Kod gözlemleri 13 |
| 14 | `lgipdb01.cbl:279-289`: `'01IEND'`, `'01IHOU'`, `'01IMOT'` (başka tür) | 17-04 | Dal alınmaz. |
| 15 | `lgipdb01.cbl:307-308`: OTHER → `'99'` (ortak) | 17-01 | |
| 16 | `lgipdb01.cbl:312-313`: `End-Program`, RETURN (ortak) | 17-04, 17-28, 17-37 | |
| 17 | `lgipdb01.cbl:997-1013`: WRITE-ERROR-MESSAGE, `ERROR-MSG` 87 bayt (ortak) | 17-03, 17-09, 17-15, 17-20, 17-28, 17-29, 17-37, 17-38 | |
| 18 | `lgipdb01.cbl:1015`, `lgipdb01.cbl:1022-1027`: `EIBCALEN` ≥ 91 kolu (ortak) | 17-09, 17-20, 17-28, 17-29, 17-37, 17-38; yanlış kolu 17-03 | |
| 19 | `lgipdb01.cbl:1016-1021`: `EIBCALEN < 91` kolu (ortak) | 17-15 | Zincirde ulaşılmaz; yalnızca doğrudan çağrı. |
| 20 | `lgipdb01.cbl:21-27`, `lgipdb01.cbl:222`, `lgipdb01.cbl:78`, `lgipdb01.cbl:203-207`: `TSAREA`, `ICOM-Record`, `END-POLICY-POS`, gösterge değişkenleri (ortak) | – | Kapsam dışı: ticari yolda kullanılmayan tanım (Kod gözlemleri 8, 12, 17). |
| 21 | `lgipdb01.cbl:291-293`: `WHEN '01ICOM'` (ticari) | 17-04 | |
| 22 | `lgipdb01.cbl:671-677`: join, müşteri + poliçe; `policyType` okunmaz (ticari) | 17-04, 17-05, 17-06, 17-07, 17-08 | Kod gözlemleri 14 |
| 23 | `lgipdb01.cbl:633-635`, `lgipdb01.cbl:652-654`: RequestDate/StartDate/RenewalDate eşlemesi (ticari) | 17-04 | Kod gözlemleri 4 |
| 24 | `lgipdb01.cbl:632-651`, `lgipdb01.cbl:703`: BROKERID/BROKERSREF/PAYMENT okunmaz (ticari) | 17-04, 17-16 | Kod gözlemleri 5 |
| 25 | `lgipdb01.cbl:680`: `IF SQLCODE = 0` (ticari) | 17-04 | |
| 26 | `lgipdb01.cbl:684-691`: 1207 kontrolü SELECT'ten sonra → `'98'` (ticari) | 17-11, 17-42; geçen kol 17-12; kontrole gelinmeyen yol 17-13 | Zincirde ulaşılamaz ([CICS bilgisi] 1); yalnızca doğrudan çağrı. |
| 27 | `lgipdb01.cbl:693-701`: COMP → görüntülü (ticari) | 17-04 | |
| 28 | `lgipdb01.cbl:703-704`: ortak ve ticari alanlar; müşteri/poliçe yazılmaz (ticari) | 17-04 | |
| 29 | `lgipdb01.cbl:708`: `'FINAL'` (ticari) | 17-04, 17-12; yazılmayan yol 17-11 | Kod gözlemleri 15 |
| 30 | `lgipdb01.cbl:712-714`: `100` → `'01'` (ticari) | 17-05, 17-06, 17-07, 17-08, 17-13 | |
| 31 | `lgipdb01.cbl:715-720`: diğer → `'90'` + mesaj (ticari) | 17-09, 17-15 | |
| 32 | `lgipdb01.cbl:295-297`: `WHEN '02ICOM'` (ticari) | 17-17 | |
| 33 | `lgipdb01.cbl:777-781`: join, yalnızca poliçe; `policyType` okunmaz (ticari) | 17-17, 17-18, 17-19 | Kod gözlemleri 7, 14 |
| 34 | `lgipdb01.cbl:737-739`, `lgipdb01.cbl:758-760`: tarih eşlemesi (ticari) | 17-17 | |
| 35 | `lgipdb01.cbl:808`: ortak alanlar `INITIALIZE` değerinde (ticari) | 17-17 | |
| 36 | `lgipdb01.cbl:784`, `lgipdb01.cbl:788-795`: 1207 kontrolü → `'98'` (ticari) | 17-21 | Zincirde ulaşılamaz; yalnızca doğrudan çağrı. |
| 37 | `lgipdb01.cbl:797`: müşteri numarası geri yazılır, yalnızca ELSE kolunda (ticari) | 17-17, 17-21, 17-22 | |
| 38 | `lgipdb01.cbl:798-809`: taşımalar (ticari) | 17-17 | |
| 39 | `lgipdb01.cbl:813`: `'FINAL'` (ticari) | 17-17 | |
| 40 | `lgipdb01.cbl:817-819`: `100` → `'01'` (ticari) | 17-18, 17-19 | |
| 41 | `lgipdb01.cbl:820-825`: diğer → `'90'` (ticari) | 17-20 | |
| 42 | `lgipdb01.cbl:299-301`: `WHEN '03ICOM'` (ticari) | 17-23 | |
| 43 | `lgipdb01.cbl:89-118`: `Cust_Cursor`, ORDER BY yok, `policyType` okunmaz (ticari) | 17-23, 17-24, 17-25 | |
| 44 | `lgipdb01.cbl:838`: `ICOM-Record-Count = 0` (ticari) | 17-26 | |
| 45 | `lgipdb01.cbl:840-847`: OPEN; ≠ 0 → `'89'` + mesaj + `END-PROGRAM` (ticari) | 17-28 | |
| 46 | `lgipdb01.cbl:849-850`: `WITH TEST AFTER UNTIL SQLCODE > 0` (ticari) | 17-23, 17-24 | |
| 47 | `lgipdb01.cbl:865-889`: FETCH (ticari) | 17-23 | |
| 48 | `lgipdb01.cbl:891-904`: taşımalar; her satır öncekinin üzerine yazar (ticari) | 17-23, 17-25 | Kod gözlemleri 8 |
| 49 | `lgipdb01.cbl:906-909`: 20'yi geçince `SQLCODE = 17` (ticari) | 17-26; sınır 17-27 | Kod gözlemleri 9 |
| 50 | `lgipdb01.cbl:891`, `lgipdb01.cbl:850`: `100` → döngü biter, son satır kalır (ticari) | 17-23, 17-24, 17-27 | |
| 51 | `lgipdb01.cbl:891`, `lgipdb01.cbl:850`: negatif FETCH, döngü bitmez (ticari) | 17-30 | Sürekli negatif kod kapsam dışı: düzenekte sonlanmaz (Kod gözlemleri 10, Açık sorular 2). |
| 52 | `lgipdb01.cbl:852-859`: CLOSE; ≠ 0 → `'88'` + mesaj + `END-PROGRAM` (ticari) | 17-29 | |
| 53 | `lgipdb01.cbl:835-913`: uzunluk kontrolü, `'FINAL'` ve `'01'` yok (ticari) | 17-24, 17-31 | |
| 54 | `lgipdb01.cbl:303-305`: `WHEN '05ICOM'` (ticari) | 17-33 | |
| 55 | `lgipdb01.cbl:120-149`: `Zip_Cursor`, girdi `CA-B-POSTCODE`, müşteri filtresi yok (ticari) | 17-33, 17-34, 17-40 | Kod gözlemleri 11 |
| 56 | `lgipdb01.cbl:923-930`: OPEN → `'89'` (ticari) | 17-37 | |
| 57 | `lgipdb01.cbl:932-933`: döngü (ticari) | 17-33, 17-34 | |
| 58 | `lgipdb01.cbl:947-971`: FETCH (ticari) | 17-33 | |
| 59 | `lgipdb01.cbl:973-986`: taşımalar; `CA-CUSTOMER-NUM` ezilir (ticari) | 17-33, 17-35 | |
| 60 | `lgipdb01.cbl:946-990`: satır sınırı yok (ticari) | 17-36 | Kod gözlemleri 9 |
| 61 | `lgipdb01.cbl:973`, `lgipdb01.cbl:933`: `100` → biter (ticari) | 17-33, 17-34 | |
| 62 | `lgipdb01.cbl:973`, `lgipdb01.cbl:933`: negatif FETCH (ticari) | 17-39 | Sürekli negatif kod kapsam dışı: düzenekte sonlanmaz. |
| 63 | `lgipdb01.cbl:935-942`: CLOSE → `'88'` (ticari) | 17-38 | |
| 64 | `lgipdb01.cbl:919-990`: uzunluk kontrolü, `'FINAL'` ve `'01'` yok (ticari) | 17-34, 17-40 | |
| 65 | `lgstsq.cbl:60-66`: `ASSIGN SYSID`, `ASSIGN INVOKINGPROG` (ortak) | 17-02, 17-03, 17-09, 17-15, 17-20, 17-28, 17-29, 17-37, 17-38 | Satırlarda yalnızca LGSTSQ'ya giden LINK tahmin edildi (Kod gözlemleri 16). |
| 66 | `lgstsq.cbl:68-71`: LINK ile çağrılma kolu (ortak) | 17-02, 17-03, 17-09, 17-15, 17-20, 17-28, 17-29, 17-37, 17-38 | Aynı not. |
| 67 | `lgstsq.cbl:72-79`: `RECEIVE` kolu (ortak) | – | Kapsam dışı: ulaşılamaz; LGSTSQ bu zincirde yalnızca LINK ile çağrılır (`lgipol01.cbl:119`, `lgipdb01.cbl:1010` ve ikinci LINK'ler). `16-commercial-add.md` kapsam maddesi 51 ile aynı gerekçe. |
| 68 | `lgstsq.cbl:83-88`: `'Q='` öneki (ortak) | – | Kapsam dışı: ulaşılamaz; gönderilen mesajlar `EM-DATE` ile ya da `'COMMAREA='` ile başlar. |
| 69 | `lgstsq.cbl:90`, `lgstsq.cbl:94-99`, `lgstsq.cbl:105-111`: uzunluk + 5; TD ve TS kuyruğa yazma (ortak) | 17-02, 17-03, 17-09, 17-15, 17-20, 17-28, 17-29, 17-37, 17-38 | Kuyruk kaydı satırlarda tahmin edilmez (Kod gözlemleri 16). |
| 70 | `lgstsq.cbl:113-119`: `'R'` → `SEND` (ortak) | – | Kapsam dışı: ulaşılamaz (madde 67 ile aynı neden). |
| 71 | `lgstsq.cbl:121-122`: RETURN (ortak) | 17-02, 17-03, 17-09, 17-15, 17-20, 17-28, 17-29, 17-37, 17-38 | |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları"). B4 satırlarında son sütun iki durumu ayrı yazar:
**düzenekte** (senaryo değer vermez ya da host değişkenine yazmazsa) ve **gerçek Db2'de**.

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 17-02 | B8 | ABEND mesajı ve `GOBACK`; LGSTSQ'ya 1 LINK | Gerçek CICS'te görev `LGCA` ile sonlanır ([CICS bilgisi] 2); COMMAREA olmadığı için dönüş kodu zaten yoktur. |
| 17-03 | B2, B8 | ABEND mesajı ve `GOBACK`; mesajda `'+00000'` | B2 sağlanmazsa `EM-SQLRC` SQLCA'daki değeri gösterir; gerçek CICS'te görev sonlanır ([CICS bilgisi] 2). |
| 17-04 | B4, [CICS bilgisi] 1 ve 4 | `'00'`, `COMMERCIAL` 10'un değerleri, `'FINAL'` | Düzenek host değişkenlerine yazmazsa ticari alanlar ve tarihler `INITIALIZE` değerleriyle (sıfır ve boşluk; `lgipdb01.cbl:245`, `lgipdb01.cbl:292`) döner, `'FINAL'` yine yazılır. Gerçek Db2'de tarih ve zaman damgası biçimi farklı olabilir (C). |
| 17-09 | B4 | `'90'` | `SQLCODE = 0` → 17-04; `100` → 17-05. |
| 17-10 | B4 | 17-04 ile aynı, `CA-REQUEST-ID = '01icom'` | 17-04 ile aynı; `CA-REQUEST-ID`'nin değişmemesi ön koşula bağlı değildir. |
| 17-11 | B4 | `'98'`, veri yok | SELECT `100` dönerse `'01'` (17-13 yolu). |
| 17-12 | B4 | 17-04 ile aynı, `'FINAL'` son 5 baytta | 17-04 ile aynı. |
| 17-14 | B1, B4, [CICS bilgisi] 1 ve 4 | `'00'`; 101-1207. baytlar tampona yazılır | B1 sağlanmazsa 100. baytın ötesine yazmanın sonucu düzeneğe bağlıdır; gerçek CICS'te C. |
| 17-15 | B4 | `'90'`; `CA-DATA` = 1-28. baytlar + boşluk | `SQLCODE = 0` → 1207 kontrolü `'98'` verir (17-11 yolu); `100` → `'01'`. |
| 17-16 | B4 (+ düzenek `POLICY` 10'u seçilmiş değerlerle kurar) | `CA-BROKERID = 0`, ␣, `CA-PAYMENT = 0` | Kurulum yapılmasa da sonuç aynıdır (değerler okunmaz); B4'ün host değişkeni eki için 17-04'e bakın. |
| 17-17 | B4, [CICS bilgisi] 1 ve 4 | `'00'`, `CA-CUSTOMER-NUM = 0000000005`, `COMMERCIAL` 9'un değerleri | Düzenek host değişkenlerine yazmazsa `CA-CUSTOMER-NUM = 0000000001` olur (`DB2-CUSTOMERNUM-INT` girdiden 1 alır, `lgipdb01.cbl:263`, ve SELECT yazmadığı için 1 kalır), ticari alanlar ve tarihler `INITIALIZE` değerleriyle döner. Gerçek Db2'de tarih ve zaman damgası biçimi farklı olabilir (C). |
| 17-20 | B4 | `'90'` | `SQLCODE = 0` → 17-17; `100` → 17-18. |
| 17-21 | B4 | `'98'`; `CA-CUSTOMER-NUM = 0000000001` kalır | SELECT `100` dönerse `'01'`. |
| 17-22 | B3, B4 | `'00'`, poliçe 9 bulunur | Kesme olmazsa `DB2-POLICYNUM-INT = 1000000009`; bu poliçe yoksa `100` [Db2 bilgisi] → `'01'`. |
| 17-23 | B4, [CICS bilgisi] 1 ve 4 | `'00'`, poliçe 9'un satırı | Düzenek FETCH host değişkenlerine yazmazsa alanlar `INITIALIZE` değerleriyle (müşteri 5, poliçe 0) döner. |
| 17-24 | B4, [CICS bilgisi] 1 ve 4 | `'00'`, COMMAREA değişmez | OPEN ≠ 0 → `'89'` (17-28 yolu); CLOSE ≠ 0 → `'88'` (17-29 yolu). |
| 17-25 | B4 | Satır 2 kalır | Gerçek Db2'de sıra C: COMMAREA'da satır 1 ya da satır 2 kalabilir. |
| 17-26 | B4 | 21 FETCH, satır 21 kalır | Gerçek Db2'de hangi 21 satırın ve hangi sırayla FETCH edileceği C; FETCH sayısı ve sınır ön koşula bağlı değildir. |
| 17-27 | B4 | 21 FETCH, satır 20 kalır | Gerçek Db2'de sıra C. |
| 17-28 | B4 | `'89'` | `SQLCODE = 0` → 17-23. |
| 17-29 | B4 | `'88'`, poliçe 9'un satırı | `SQLCODE = 0` → 17-23. |
| 17-30 | B4 | `'00'`, veri yok | Senaryo sonraki FETCH'te de negatif verirse döngü bitmez (Kod gözlemleri 10). Gerçek Db2'de sonuç C. |
| 17-31 | B1, B4 | `'00'`; 29-1202. baytlar tampona yazılır | B1 sağlanmazsa 28. baytın ötesine yazmanın sonucu düzeneğe bağlıdır; gerçek CICS'te C. |
| 17-32 | B3, B4 | `'00'`, müşteri 1'in satırı, `CA-CUSTOMER-NUM = 0000000001` | Kesme olmazsa `DB2-CUSTOMERNUM-INT = 1000000001`; bu müşterinin satırı yoksa 1 FETCH (`100`, [Db2 bilgisi]), `'00'`, COMMAREA değişmez. |
| 17-33 | B4, [CICS bilgisi] 1 ve 4 | `'00'`, poliçe 10'un satırı, `CA-CUSTOMER-NUM = 0000000001` | Düzenek FETCH host değişkenlerine yazmazsa alanlar `INITIALIZE` değerleriyle döner. |
| 17-34 | B4, [CICS bilgisi] 1 ve 4 | `'00'`, COMMAREA değişmez | OPEN ≠ 0 → `'89'`; CLOSE ≠ 0 → `'88'`. |
| 17-35 | B4 | Satır 2 kalır | Gerçek Db2'de sıra C. |
| 17-36 | B4 | 23 FETCH, satır 22 kalır | Gerçek Db2'de sıra C; FETCH sayısı ön koşula bağlı değildir. |
| 17-37 | B4 | `'89'` | `SQLCODE = 0` → 17-33. |
| 17-38 | B4 | `'88'`, poliçe 10'un satırı | `SQLCODE = 0` → 17-33. |
| 17-39 | B4 | `'00'`, veri yok | 17-30 ile aynı. |
| 17-40 | B1, B4 | `'00'`, eşleşme yok | B1 sağlanmazsa posta kodu tampondaki 8 bayttır; eşleşen satır varsa onun verisi döner. |
| 17-42 | B4 | `'98'`, veri yok | SELECT `100` dönerse `'01'` (17-13 yolu). |

## Belirsiz satırlar

Bütün satırı belirsiz olan yalnızca 17-41'dir. Diğer satırlarda yalnızca belirtilen hücre belirsizdir; satırın kalanı ön koşulludur.

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 17-04 | Ön koşul sağlanmazsa sütunu: gerçek Db2'de `CA-ISSUE-DATE`, `CA-EXPIRY-DATE` ve `CA-LASTCHANGED`'in karakter biçimi | Host değişkenleri X(10) ve X(26)'dır (`lgpolicy.cpy:46-48`); DATE ve TIMESTAMP'in karakter gösterimi Db2 kurulumuna bağlıdır (C; Kod gözlemleri 18). Düzenekte değer B4 ile tanımlıdır. |
| 17-14 | Beklenen veri: gerçek CICS'te 100 baytlık alanın ötesine yazılan baytların sonucu | LGIPOL01 uzunluk denetlemez ve 32500 ile LINK eder (`lgipol01.cbl:91-94`); gerçek CICS'te COMMAREA'nın ötesinin ne olduğu çalışma ortamına bağlıdır (C). |
| 17-17 | Ön koşul sağlanmazsa sütunu: gerçek Db2'de `CA-ISSUE-DATE`, `CA-EXPIRY-DATE` ve `CA-LASTCHANGED`'in karakter biçimi | Host değişkenleri X(10) ve X(26)'dır (`lgpolicy.cpy:46-48`); DATE ve TIMESTAMP'in karakter gösterimi Db2 kurulumuna bağlıdır (C; Kod gözlemleri 18). Düzenekte değer B4 ile tanımlıdır. |
| 17-25 | Beklenen veri: gerçek Db2'de hangi satırın COMMAREA'da kalacağı | `Cust_Cursor`'da ORDER BY yok (`lgipdb01.cbl:89-118`); satır sırası Db2'ye bağlıdır (C). |
| 17-26 | Beklenen veri: gerçek Db2'de 21. satırın hangisi olacağı | Aynı neden (C). |
| 17-27 | Beklenen veri: gerçek Db2'de 20. satırın hangisi olacağı | Aynı neden (C). |
| 17-30 | Beklenen veri: gerçek Db2'de negatif FETCH'ten sonraki davranış | Kod negatif kodu denetlemez (`lgipdb01.cbl:850`, `lgipdb01.cbl:891`); hatadan sonra cursor'ın durumu ve sonraki FETCH'in sonucu Db2'ye bağlıdır (C). |
| 17-31 | Beklenen veri: gerçek CICS'te 28 baytlık alanın ötesine yazılan baytların sonucu | 03ICOM uzunluk denetlemez (`lgipdb01.cbl:835-913`); sonuç C. |
| 17-35 | Beklenen veri: gerçek Db2'de hangi satırın kalacağı | `Zip_Cursor`'da ORDER BY yok (`lgipdb01.cbl:120-149`; C). |
| 17-36 | Beklenen veri: gerçek Db2'de 22. satırın hangisi olacağı | Aynı neden (C). |
| 17-39 | Beklenen veri: gerçek Db2'de negatif FETCH'ten sonraki davranış | 17-30 ile aynı neden (`lgipdb01.cbl:933`, `lgipdb01.cbl:973`; C). |
| 17-41 | Dönüş kodu ve beklenen veri | LGIPOL01 uzunluk denetlemez (`lgipol01.cbl:79-94`), LGIPDB01 32500 görür ([CICS bilgisi] 1). `lgipdb01.cbl:263-264` müşteri ve poliçe numaralarını (9-28. baytlar) kısmen ya da tamamen tampondan, boşluk içerirken COMP'a taşır (derleyiciye bağlı, C). `EIBCALEN` < 6 iken istek kodu da tampondan okunur (`lgipdb01.cbl:275-277`). Dönüş kodu `EIBCALEN`'in ötesine yazılır (gerçek CICS'te C). |

## Test sırası ve veri bağımlılıkları

Öneri; sıra ve veri sıfırlama kararı ekibindir. Gerçek veri (ya da veri durumunu tutan bir düzenek) için geçerlidir; düzenek her
satırın veri durumunu B4 ile kendisi kurarsa satırlar birbirinden bağımsızdır ve her durum ayrı süreçte çalışır
([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:50-52`).

1. **Sorgu veri değiştirmez.** Bu dosyadaki hiçbir satır `POLICY`, `COMMERCIAL` ya da VSAM'a yazmaz; satırlar birbirinin verisini
   bozmaz. Veriyi bozan yalnızca çok satırlı ve 17-16 satırlarının **kurulumudur** (madde 3, 4).
2. **Başlangıç verisi.** `db2cre.jcl` ile yüklenmiş Db2. Satırların dayandığı örnek veri: müşteri 1, 5, 6; poliçe 3, 7, 9, 10;
   `COMMERCIAL` 9 ve 10; `HOUSE` 6 ve 7 (Kod gözlemleri 14). Satırların beklediği sayılar:
   - 17-23 ve 17-29: müşteri 5'in **tam 1** ticari satırı (poliçe 9).
   - 17-33 ve 17-38: `'SO212JN'` için **tam 1** satır (poliçe 10); 17-32: müşteri 1 için tam 1 satır (poliçe 10).
   - 17-24: müşteri 6 için 0 satır; 17-34: `'SO211UP'` için 0 satır; 17-40: boş posta kodu için 0 satır.
   - 17-05, 17-07, 17-08, 17-13: poliçe 0, müşteri 0 ve "müşteri 5 + poliçe 10" eşleşmesi yoktur.
3. **Çok satırlı satırlar (17-25, 17-26, 17-27, 17-35, 17-36).** Düzenek satırları B4 ile verir. Gerçek veride müşteri 5 ve
   `'BB104BX'` için poliçe 9'a ek olarak 1, 21, 19, 1 ve 21 satır eklenmesi gerekir (ek satırlar: poliçe 1000001'den başlayarak,
   `Customer = 'SATIR-kk'`). Ekleme `01ACOM` ile yapılırsa ek satırların tarihleri ve `RequestDate`'i eklenen değerlerdir
   (`16-commercial-add.md` Kod gözlemleri 7) ve satır 1 dışındaki ortak alanlar poliçe 9'unkinden farklı olabilir; satırlardaki
   beklenen veri yalnızca ayırt edici alanlar (`CA-POLICY-NUM`, `CA-B-Customer`) için yazıldı. Bu ekler 17-23, 17-29, 17-30 ve
   17-35 … 17-39'un veri durumunu bozar. **Sıfırlama noktaları:** her çok satırlı satırdan önce kurulum, sonra `db2cre.jcl` verisine
   dönüş.
4. **17-16.** `POLICY` 10'un `brokerId`, `brokersReference` ve `payment` sütunları seçilmiş değerlerle kurulur. Bu dosyadaki diğer
   satırların sonucu değişmez (sütunlar okunmaz), ama başka dilimlerin poliçe 10 testleri etkilenir; satırdan sonra eski değerlere
   (`db2cre.jcl:783-785`) dönülmelidir.
5. **Önceki dilim.** `16-commercial-add.md`'nin ekleme satırlarından sonra sıfırlama yapılmazsa müşteri 1'in ve `'SO212JN'`'nin ikinci
   bir ticari satırı olur (poliçe 1000001). O durumda 17-32 ve 17-33'te COMMAREA'da hangi satırın kalacağı belirsiz olur (C) ve
   FETCH sayısı 3'e çıkar. 17-04 (poliçe 10'a özgü) etkilenmez.
6. **Sonraki dilim.** Silme testleri poliçeleri yok eder; sorgu satırları silmeden önce ya da veri yeniden yüklendikten sonra
   çalışmalıdır.
7. **Önerilen sıra.** Önce SQL çalıştırmayan satırlar (17-01, 17-02, 17-03), sonra örnek veriyle çalışan tek satırlı sorgular
   (17-04 … 17-15, 17-17 … 17-24, 17-28 … 17-34, 17-37 … 17-42), en son kurulum gerektirenler (17-16, 17-25, 17-26, 17-27, 17-35,
   17-36), her birinden sonra sıfırlama.

## Açık sorular

Ekip kararı gerektiren, tahminleri etkileyen sorular. Tahminler bu soruların cevabını varsaymaz; varsayım gereken yerde ön koşul
kodu kullanıldı.

1. **`-911` örnek değeri.** Karar verildi: "diğer hata" örneği olarak iş birimini geri almayan `-904` kullanılır; satırlar
   buna göre güncellendi. (`-911` kilitlenme kodudur ve yeni sistemde `503`'e eşlenir, ADR 0002 Karar 8.)
2. **Bulgu: negatif FETCH döngüsü sonlanmaz.** 03ICOM ve 05ICOM'un FETCH döngüsü yalnızca pozitif `SQLCODE` ile biter
   (`lgipdb01.cbl:850`, `lgipdb01.cbl:933`); negatif kod hiçbir yerde denetlenmez. Kalıcı bir FETCH hatası düzenekte sonsuz döngüye
   yol açar ("kapsam dışı: düzenekte sonlanmaz"), gerçek Db2'de sonucu C'dir. Düzenek bu durumu nasıl sınırlamalı (FETCH sayısına
   üst sınır mı)? C# tarafı aynı davranışı mı (hata yutma) yoksa bir hata kodu mu üretmeli?
3. **Bilinçli sapma adayı: ortak alanlar.** Ticari sorgu `BROKERID`, `BROKERSREF` ve `PAYMENT`'ı hiç okumaz ve her zaman 0/boş döner
   (Kod gözlemleri 5, 17-16). C# tarafı `POLICY`'deki gerçek broker ve ödeme değerlerini döndürürse bu bilinçli bir sapma adayıdır;
   ekip kararı gerekir.
4. **Tarihlerin kaynağı.** Dönen `CA-ISSUE-DATE`, `CA-EXPIRY-DATE` ve `CA-LASTCHANGED` `COMMERCIAL`'dan gelir (Kod gözlemleri 4).
   C# tarafı `POLICY`'nin tarihlerini döndürürse örnek veride (poliçe 9 ve 10) sonuç farklıdır; bu da bir sapma adayıdır.
5. **Çok satırlı sonuçların eşdeğerliği.** COBOL COMMAREA'da yalnızca son satırı döndürür ve 03ICOM'da 21 satırda durur; 05ICOM'da
   sınır yoktur. C# tarafı liste döndürürse eşdeğerlik testleri neyi karşılaştırmalı (son satır mı, satır sayısı mı)?
6. **COMMAREA ötesine yazma.** 17-14 ve 17-31'de program istemcinin bildirdiği uzunluğun ötesine yazar. Düzenek bunu B1 tamponuyla
   gösterir; gerçek CICS'te sonucu C'dir. Ekip bunu bir arayüz bulgusu olarak mı kaydetmeli?
7. **Tarih ve zaman damgası biçimi.** B4 satırları `db2cre.jcl`'deki yazımı kullanır; gerçek Db2'nin DATE ve TIMESTAMP karakter
   biçimi (Kod gözlemleri 18) karşılaştırma testlerinde nasıl normalleştirilmeli?
8. **NULL sütunlar.** Ticari deyimlerde gösterge değişkeni yoktur (Kod gözlemleri 17). NULL içeren bir `COMMERCIAL` satırı
   01ICOM/02ICOM'da `'90'`, cursor'larda negatif FETCH yoluna düşer (yorum: `lgipdb01.cbl:200-202`). Ekip NULL içeren bir senaryo
   istiyor mu?
