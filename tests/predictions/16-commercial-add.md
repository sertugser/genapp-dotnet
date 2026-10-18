# 16 — Ticari poliçe ekle (Add Commercial Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Ticari poliçe ekle: yeni poliçe Db2 `POLICY` ve `COMMERCIAL` tablolarına, ticari kaydın bir bölümü de VSAM `KSDSPOLY` dosyasına yazılır. Poliçe türü COMMAREA'daki istek kodundan (`01ACOM`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGAPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGAPDB01` (Db2 `POLICY` + `COMMERCIAL` INSERT) → `LINK` → `LGAPVS01` (VSAM `KSDSPOLY` WRITE). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. Programlar dört poliçe türüne de hizmet eder; yalnızca ticari (`01ACOM`) kolları ve ortak kısımlar tahmin edildi, endowment/konut/motor kolları "başka tür, dal alınmaz" diye işaretlendi. |
| Okunan COBOL dosyaları | `base/src/lgapol01.cbl` (1-169), `base/src/lgapdb01.cbl` (1-595), `base/src/lgapvs01.cbl` (1-188), `base/src/lgstsq.cbl` (1-126), `base/src/lgcmarea.cpy` (1-103), `base/src/lgpolicy.cpy` (1-107). |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `CUSTOMER` kimlik tanımı (`db2cre.jcl:111-112`), `POLICY` (`db2cre.jcl:161-176`) ve `COMMERCIAL` (`db2cre.jcl:313-336`) tanımları, müşteri numaraları 1-10 (`db2cre.jcl:437` … `db2cre.jcl:743`), poliçe 10 (`db2cre.jcl:777-786`), `COMMERCIAL` 10 (`db2cre.jcl:1184-1203`), `COMMERCIAL` 9'dan üç metin (`db2cre.jcl:1231`, `db2cre.jcl:1235`, `db2cre.jcl:1236`). `base/cntl/adef121.jcl` (`adef121.jcl:40-52`, `adef121.jcl:65-73`) ve `base/data/ksdspoly.txt` (`ksdspoly.txt:1-2`): `KSDSPOLY` tanımı ve başlangıç verisi. `base/cntl/cobol.jcl` (`cobol.jcl:44-50`): derleme seçenekleri (B3). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `docs/adr/0004-legacy-test-harness.md` (`0004-legacy-test-harness.md:44-46`, `0004-legacy-test-harness.md:50-52`, `0004-legacy-test-harness.md:56-58`, `0004-legacy-test-harness.md:107-109`, `0004-legacy-test-harness.md:115-116`, `0004-legacy-test-harness.md:135-138`): yalnızca B4, B8, C grubu, [CICS bilgisi], Kod gözlemleri 14, 15 ve 17 ile Test sırası için. **Kodla doğrulanmadı.** |
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
  davranış Db2'den gelir. Bu dosyada tahmin satırlarında tek kullanımı 16-03'tür (var olmayan müşteriye `POLICY` INSERT'i `-530` döner); ayrıca 16-16'nın "Ön koşul sağlanmazsa" sütununda.
- **[CICS bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen CICS davranışı. Bu dosyada üç madde: 1, 2 ve 4 (3. madde konut dosyası 10'a özgü: SYNCPOINT ROLLBACK):
  1. `LINK … LENGTH(n)` ile çağrılan program `EIBCALEN = n` görür. Bu yüzden zincirdeki alt programın uzunluk kontrolü en dıştaki
     çağıranın uzunluğuna bakmaz.
  2. `EXEC CICS ABEND` görevi sonlandırır; `HANDLE ABEND` olmadığı için denetim çağırana dönmez. Görevin kesinleşmemiş Db2 ve
     VSAM değişiklikleri geri alınır (`TEMPLATE.md` madde 7; VSAM dosyaları `LOG(UNDO)` ile tanımlı: `adef121.jcl:20`, `:45`).
  4. LINK edilen programdaki `EXEC CICS RETURN` denetimi LINK eden programa, `LINK` deyiminden sonraki deyime döndürür; çağrılan
     programın COMMAREA'ya yazdıkları çağırana görünür.

  Düzenek bu davranışları ölçmez ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:107-109`); düzenekteki karşılıkları B1
  (LINK), B8 (ABEND) ve `RETURN` → `GOBACK` ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:44-46`).
- **C (belirsizlik grubu)**: sonucu kodda tanımlı olmayan, çalışma ortamına bağlı davranış: gerçek CICS (görev sonu, LINK hatası,
  COMMAREA'sız çağrı), gerçek Db2 (iş birimi, zaman damgası biçimi, NULL, kimlik değerinin
  harcanması, kodda yazmayan hata kodları), gerçek VSAM ya da derleyici (sayısal olmayan içeriğin `COMP` alana taşınması). "(C)"
  işaretli hücre **belirsiz**dir: ne ön koşul koduyla ne [Db2 bilgisi] ya da [CICS bilgisi] ile kesinleştirilmiştir ve düzenekte
  ölçülmez. [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:107-109`.

Ön koşul kodları test düzeneğinin sağlaması gereken koşullardır. Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul
sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te en dıştaki çağıranın `LENGTH(32500)` tamponuyla geçirir; `EIBCALEN` yalnızca bildirilen uzunluktur, tamponun `EIBCALEN` ötesi boşlukla doldurulur. `EIBCALEN` ötesine yapılan yazmalar da bu tampona düşer ve çağıran onları görür. `EIBCALEN = 0` ile çağrılan programa da 32500 baytlık boşluk tamponu geçirilir ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:46`). **Bu dosyada:** 16-11, 16-15, 16-17, 16-19, 16-20; ayrıca belirsiz 16-18, 16-21 ve 16-23'ün girdisinde. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. Bu dosyada: 16-14 (`lgapdb01.cbl:564`, hiçbir SQL deyimi çalışmadan). |
| B3 | Derleyici sayı taşımasında PICTURE'ın hane sayısına göre keser (10 haneli numara → 9 hane). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. Bu dosyada: 16-16. |
| B4 | Düzenek satırda istenen `SQLCODE`/`RESP`'i üretir; **aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir. Kayıt ekleyip silebilir. Host değişkenlerine yalnızca satırda yazılan değeri koyar; başarısız deyimde host değişkenine yazmaz. Host değişkeni kullanmayan başarılı SQL deyiminin `0`, dosya komutunun `NORMAL` dönmesi de B4'e bağlıdır. [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:56-58`, `0004-legacy-test-harness.md:135-138`. **Bu dosyada:** başarılı `SET ... IDENTITY_VAL_LOCAL()`'in (`lgapdb01.cbl:308-310`) host değişkenine yeni poliçe numarası (değeri: B6), başarılı `SELECT LASTCHANGED`'in (`lgapdb01.cbl:316-321`) host değişkenine yeni `POLICY` satırının INSERT anındaki zaman damgası yazılır. Satırlar: 16-01, 16-02, 16-04, 16-05, 16-06, 16-07, 16-11, 16-13, 16-15, 16-16, 16-17, 16-20. |
| B6 | `POLICY` kimlik değeri 1000001'den başlar ([kod] `db2cre.jcl:162-163`): satırdan önce `POLICY` tablosuna `DEFAULT` ile hiç satır eklenmemiştir. Örnek poliçeler 1-10 numarasını açıkça verir (`db2cre.jcl:777`, `db2cre.jcl:801`, `db2cre.jcl:825`, `db2cre.jcl:849`, `db2cre.jcl:873`, `db2cre.jcl:897`, `db2cre.jcl:921`, `db2cre.jcl:945`, `db2cre.jcl:969`, `db2cre.jcl:993`) ve sayacı ilerletmez. Bu dosyada: 16-01, 16-02, 16-05, 16-06, 16-07, 16-11, 16-13, 16-16, 16-20. Sayacın satırlar arasında ilerlemesi "Test sırası ve veri bağımlılıkları" bölümünde. |
| B8 | Düzenekte `EXEC CICS ABEND` mesaj + `GOBACK`'e dönüşür: abend eden program çağırana döner ve COMMAREA'ya en son yazılan dönüş kodu görünür. [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:44-46`. [CICS bilgisi] 2: gerçek CICS'te ABEND görevi sonlandırır; orada çağıranın kodu görmesi belirsizdir (C). Bu dosyada: 16-05 (`LGSQ`), 16-10 ve 16-14 (`LGCA`; COMMAREA olmadığı için dönüş kodu görünmez, düzenekte görülen ABEND mesajı ve `GOBACK`'tir). |

(B5 ve B7 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **COMMAREA bayt haritası, ticari görünüm** (`lgcmarea.cpy:10-13`, `lgcmarea.cpy:34-43`, `lgcmarea.cpy:77-94`). Üç program da bu
   düzeni kullanır (`lgapol01.cbl:70-71`, `lgapdb01.cbl:134-137`, `lgapvs01.cbl:86-87`).

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
   | 364-374 | `CA-B-Latitude` | X(11) | `lgcmarea.cpy:80` |
   | 375-385 | `CA-B-Longitude` | X(11) | `lgcmarea.cpy:81` |
   | 386-640 | `CA-B-Customer` | X(255) | `lgcmarea.cpy:82` |
   | 641-895 | `CA-B-PropType` | X(255) | `lgcmarea.cpy:83` |
   | 896-899 | `CA-B-FirePeril` | 9(4) | `lgcmarea.cpy:84` |
   | 900-907 | `CA-B-FirePremium` | 9(8) | `lgcmarea.cpy:85` |
   | 908-911 | `CA-B-CrimePeril` | 9(4) | `lgcmarea.cpy:86` |
   | 912-919 | `CA-B-CrimePremium` | 9(8) | `lgcmarea.cpy:87` |
   | 920-923 | `CA-B-FloodPeril` | 9(4) | `lgcmarea.cpy:88` |
   | 924-931 | `CA-B-FloodPremium` | 9(8) | `lgcmarea.cpy:89` |
   | 932-935 | `CA-B-WeatherPeril` | 9(4) | `lgcmarea.cpy:90` |
   | 936-943 | `CA-B-WeatherPremium` | 9(8) | `lgcmarea.cpy:91` |
   | 944-947 | `CA-B-Status` | 9(4) | `lgcmarea.cpy:92` |
   | 948-1202 | `CA-B-RejectReason` | X(255) | `lgcmarea.cpy:93` |
   | 1203-32500 | `CA-B-FILLER` | X(31298) | `lgcmarea.cpy:94` |

2. **Uzunluk hesapları.** LGAPOL01 yalnızca başlığı ister: `WS-CA-HEADER-LEN = 28` (`lgapol01.cbl:59`), gereken uzunluk 28
   (`lgapol01.cbl:109`). LGAPDB01 ticari için 28 (`lgapdb01.cbl:65`, `lgapdb01.cbl:182`) + `WS-FULL-COMM-LEN = 1174`
   (`lgpolicy.cpy:27`, `lgapdb01.cbl:199`) = **1202** ister; 1202, `CA-B-RejectReason`'ın son baytıdır (Kod gözlemleri 1). 1174,
   `WS-POLICY-LEN = 72` (`lgpolicy.cpy:18`) ile `WS-COMM-LEN = 1102`'nin (`lgpolicy.cpy:22`) toplamıyla tutarlıdır. İki program da
   gereken uzunluğu `ADD` ile kurar (`lgapol01.cbl:109`, `lgapdb01.cbl:182`, `lgapdb01.cbl:199`); `WS-REQUIRED-CA-LEN`'in değeri
   (`lgapol01.cbl:60`, `lgapdb01.cbl:66`) yalnızca her çağrıda WORKING-STORAGE yeniden kurulduğunda doğrudur (Kod gözlemleri 17).

3. **Çağrı zinciri ve dönüş akışı.** LGAPOL01 LGAPDB01'i `LENGTH(32500)` ile LINK eder (`lgapol01.cbl:121-124`) ve dönüşte yalnızca
   `RETURN` yapar (`lgapol01.cbl:126`): çağıranın gördüğü dönüş kodu alt programların bıraktığıdır ([CICS bilgisi] 4). LGAPDB01
   LGAPVS01'i yine `LENGTH(32500)` ile LINK eder (`lgapdb01.cbl:243-246`) ve `RETURN` yapar (`lgapdb01.cbl:250`). LGAPVS01 normal
   yolda CICS `RETURN` değil `GOBACK` ile biter (`lgapvs01.cbl:151-153`) ve dönüş koduna dokunmaz; başarılı eklemede kod
   `lgapdb01.cbl:293`'te yazılan `'00'` kalır. LINK'lerin hiçbirinde `RESP` ya da `HANDLE CONDITION` yok; LINK hatasının sonucu
   kodda tanımlı değildir (C) ve satır yazılmadı.

4. **Sürpriz 1: uzunluk kontrolünün yeri.** 1202 baytlık kontrol LGAPDB01'dedir (`lgapdb01.cbl:210-213`), ama LGAPDB01 zincirde
   `EIBCALEN = 32500` görür ([CICS bilgisi] 1; `lgapol01.cbl:123`); bu kontrol zincirde hiç `'98'` üretmez. Zincirde `'98'` yalnızca
   `EIBCALEN < 28` ile LGAPOL01'den gelir (`lgapol01.cbl:113-116`). 28 ≤ `EIBCALEN` < 1202 ile gelen istek kontrolden geçer ve alt
   programlar kısa COMMAREA'nın ötesini 32500 baytlık alan gibi okur (düzenekte B1: boşluk). 16-11 bunun iyi tarafını (1201 bayt
   → tam ekleme), 16-18 kötü tarafını (28 bayt → sonuç belirsiz) gösterir; 16-12 aynı 1201 baytın LGAPDB01'e doğrudan verildiğinde
   `'98'` aldığını gösterir. Aralık olarak: zincirde 947-1201 → `'00'` (16-20), 28-946 → belirsiz (16-21); LGAPDB01'e doğrudan 18-1201 →
   `'98'` (16-22), 8-17 → belirsiz (16-23). 947, son sayısal ticari alan `CA-B-Status`'un son baytıdır (`lgcmarea.cpy:92`).

5. **Sürpriz 2: iki `WHEN OTHER '99'`.** İlk EVALUATE'in OTHER kolu `'99'` yazar ve `RETURN` eder (`lgapdb01.cbl:202-205`).
   İkinci EVALUATE'in OTHER kolu (`lgapdb01.cbl:237-239`) **ulaşılamaz**: ikinci EVALUATE'e (`lgapdb01.cbl:223`) yalnızca dört geçerli
   koddan biriyle gelinir ve arada çalışan INSERT-POLICY (`lgapdb01.cbl:261-322`) `CA-REQUEST-ID`'yi değiştirmez. Ulaşılsaydı bile
   `'99'`'u yazıp LGAPVS01'e LINK ederdi, çünkü LINK EVALUATE'in dışındadır (`lgapdb01.cbl:243`).

6. **Sürpriz 3: istek kodu büyük harfe çevrilmez.** LGAPOL01 istek koduna hiç bakmaz; LGAPDB01 onu olduğu gibi karşılaştırır
   (`lgapdb01.cbl:184`, `lgapdb01.cbl:198`). `'01acom'` OTHER koluna düşer ve `'99'` alır (16-08). LGAPVS01 yalnızca 4. baytı kullanır
   (`lgapvs01.cbl:99`); ona ancak geçerli bir kodla gelinir.

7. **Sürpriz 4: RequestDate kaynağı.** `POLICY` satırı `LASTCHANGED = CURRENT TIMESTAMP` ile yazılır (`lgapdb01.cbl:275`,
   `lgapdb01.cbl:284`). Değer `SELECT LASTCHANGED` ile `CA-LASTCHANGED`'e okunur (`lgapdb01.cbl:316-321`) ve `COMMERCIAL.RequestDate`'e
   yazılır (`lgapdb01.cbl:503`, `lgapdb01.cbl:525`). Yani yeni `COMMERCIAL.RequestDate` = yeni `POLICY.lastChanged`.
   `StartDate` = `CA-ISSUE-DATE` (`lgapdb01.cbl:504`, `lgapdb01.cbl:526`) ve `RenewalDate` = `CA-EXPIRY-DATE` (`lgapdb01.cbl:505`,
   `lgapdb01.cbl:527`); aynı iki değer `POLICY`'ye de yazılır (`lgapdb01.cbl:281-282`). `SELECT LASTCHANGED`'in `SQLCODE`'u **kontrol
   edilmez**: sonraki ilk kontrol `lgapdb01.cbl:547`'dir ve INSERT COMMERCIAL'ın sonucuna bakar. SELECT başarısızsa `CA-LASTCHANGED`
   girdideki değerinde kalır ve RequestDate o olur (16-07). Zaman damgasının karakter biçimi Db2'ye bağlıdır (C); satırlarda biçim
   değil, iki alanın eşitliği tahmin edildi.

8. **`IDENTITY_VAL_LOCAL` sonucu kontrol edilmez** (`lgapdb01.cbl:308-311`). Başarısızsa host değişkeni `INITIALIZE` değerinde (0,
   `lgapdb01.cbl:117`, `lgapdb01.cbl:159`) kalabilir ve sonraki deyimler poliçe 0 ile çalışır. Sonuç birden çok Db2 varsayımına bağlı
   olduğu için satır yazılmadı; madde başarılı yolda 16-01 ile kapsandı.

9. **Sürpriz 5: örnek veri LGAPDB01 ile üretilmiş olamaz.** Kod `StartDate`'e `POLICY.issueDate`'in, `RequestDate`'e
   `POLICY.lastChanged`'in değerini yazar (Kod gözlemleri 7). Örnek veride `COMMERCIAL` 10'un `StartDate`'i `'2011-08-25'`
   (`db2cre.jcl:1186`), `POLICY` 10'un `issueDate`'i `'2011-08-22'` (`db2cre.jcl:779`); `RequestDate` `'2011-08-23 10:44:34'`
   (`db2cre.jcl:1185`), `lastChanged` `'2011-08-22 12:13:01'` (`db2cre.jcl:782`); `RenewalDate` `'2011-08-24'` (`db2cre.jcl:1187`)
   `StartDate`'ten öncedir. VSAM örneğinde Status baytları `'A  0'`, sayısal değil (`ksdspoly.txt:1`, `ksdspoly.txt:2`); kod buraya
   9(4) bir alan yazar (`lgapvs01.cbl:33`, `lgapvs01.cbl:107`). Poliçe 9'un VSAM müşteri metni `'Clarets Merchandise'`
   (`ksdspoly.txt:2`), Db2'deki `'Burnley Football Club'` (`db2cre.jcl:1235`). Bu yüzden 16-01'in tarihleri `POLICY` 10'dan alındı.

10. **Sürpriz 6: `70`/`90` akışı ve RETURN'ün çıktığı program.** INSERT POLICY'den sonra `SQLCODE` değerlendirilir
    (`lgapdb01.cbl:290-305`): `0` → `'00'`; `-530` → `'70'`, hata mesajı, `RETURN` (`lgapdb01.cbl:295-298`); diğer her değer → `'90'`,
    hata mesajı, `RETURN` (`lgapdb01.cbl:300-303`). `RETURN` LGAPDB01'den çıkar; denetim LGAPOL01'de LINK'ten sonraki deyime gelir ve
    LGAPOL01 de `RETURN` eder (`lgapol01.cbl:126`; [CICS bilgisi] 4). Bu yollarda `CA-POLICY-NUM` ve `CA-LASTCHANGED` girdideki gibi
    kalır, LGAPVS01 çağrılmaz. Mesajdaki `EM-POLNUM` boşluktur (`lgapdb01.cbl:50`), çünkü ancak `lgapdb01.cbl:313`'te dolar.

11. **Sürpriz 7: INSERT COMMERCIAL hatasında ABEND ve geri alma yorumu.** `SQLCODE` ≠ 0 → `'90'`, hata mesajı, `ABEND 'LGSQ'`
    (`lgapdb01.cbl:547-551`). Yorum "Issue Abend to cause backout of update to Policy table" der (`lgapdb01.cbl:550`); kodda `SYNCPOINT
    ROLLBACK` ya da `DELETE` yoktur; geri almayı `ABEND`'in kendisi yapar. Yorum gerçek CICS davranışıyla uyumludur: gerçek CICS'te
    çağırana `'90'` ulaşmaz ve `POLICY` satırı geri alınır ([CICS bilgisi] 2, `TEMPLATE.md` madde 7). Düzenekte (B8) LGAPDB01 `GOBACK` ile döner, LGAPOL01 `lgapol01.cbl:126`'dan döner, çağıran
    `'90'`'ı görür; `POLICY` satırı yerinde kalır, `COMMERCIAL` satırı yoktur, LGAPVS01 çağrılmaz. ABEND'den sonraki `RETURN`
    (`lgapdb01.cbl:552`) iki ortamda da ulaşılamaz.

12. **Sürpriz 8: `'80'` yolunda Db2 satırları geri alınmaz.** VSAM WRITE `RESP` ≠ `NORMAL` → `'80'`, hata mesajı, `RETURN`
    (`lgapvs01.cbl:142-147`). Bu noktada `POLICY` ve `COMMERCIAL` satırları yazılmıştır; hiçbir program onları silmez. Denetim
    LGAPDB01'e (`lgapdb01.cbl:250`), oradan LGAPOL01'e (`lgapol01.cbl:126`) döner. Gerçek ortamda satırların görev sonunda kalıcı
    olması CICS'in görev sonu davranışına bağlıdır (C).

13. **Sürpriz 9: LGAPVS01'in ticari kaydı.** Kayıt 64 bayttır (`lgapvs01.cbl:25-34`, `lgapvs01.cbl:137`; `adef121.jcl:50`),
    anahtar ilk 21 bayttır (`lgapvs01.cbl:26-29`, `lgapvs01.cbl:139`; `adef121.jcl:49`):

    | Bayt | Alan | Kaynağı |
    |---|---|---|
    | 1 | `WF-Request-ID` = `CA-Request-ID(4:1)` = `'C'` | `lgapvs01.cbl:99` |
    | 2-11 | `WF-Customer-Num` | `lgapvs01.cbl:101` |
    | 12-21 | `WF-Policy-Num` | `lgapvs01.cbl:100` |
    | 22-29 | `WF-B-Postcode` X(8) | `lgapvs01.cbl:32`, `lgapvs01.cbl:106` |
    | 30-33 | `WF-B-Status` 9(4) | `lgapvs01.cbl:33`, `lgapvs01.cbl:107` |
    | 34-64 | `WF-B-Customer` X(31) | `lgapvs01.cbl:34`, `lgapvs01.cbl:108` |

    `CA-B-Customer` 255 bayttır; VSAM'a yalnızca ilk 31 baytı yazılır (16-02). Adres, koordinatlar, mülk türü, riskler ve red nedeni
    VSAM'a yazılmaz. LGAPVS01'de `EIBCALEN = 0` ya da uzunluk kontrolü yoktur; `WS-Commarea-Len` (`lgapvs01.cbl:97`) hiç kullanılmaz
    (16-15, 16-17). EVALUATE'in OTHER kolu (`lgapvs01.cbl:130-131`) zincirde ulaşılamaz (LGAPDB01 LINK'e yalnızca dört geçerli kodla
    gelir); ona yalnızca ticari olmayan bir kodla doğrudan çağrıda girilir, bu dosyada kapsam dışıdır.

14. **Sürpriz 10: hata mesajları ve LGSTSQ kuyruk kaydı.** Her hata yolunda program `LGSTSQ`'ya önce `ERROR-MSG`'yi, sonra
    `EIBCALEN > 0` ise `CA-ERROR-MSG`'yi LINK eder; `LENGTH` her seferinde alanın kendi uzunluğudur.

    | Program | `ERROR-MSG` | Alanlar (sırayla) | `CA-ERROR-MSG` |
    |---|---|---|---|
    | LGAPOL01 | 45 bayt (`lgapol01.cbl:41-46`) | `EM-DATE`, ␣, `EM-TIME`, `' LGAPOL01'`, `EM-VARIABLE` X(21) | 99 bayt (`lgapol01.cbl:48-50`) |
    | LGAPDB01 | 87 bayt (`lgapdb01.cbl:41-53`) | `EM-DATE`, ␣, `EM-TIME`, `' LGAPDB01'`, `' CNUM='`, `EM-CUSNUM`, `' PNUM='`, `EM-POLNUM`, `EM-SQLREQ` X(16), `' SQLCODE='`, `EM-SQLRC` (+9(5)) | 99 bayt (`lgapdb01.cbl:55-57`) |
    | LGAPVS01 | 101 bayt (`lgapvs01.cbl:58-73`) | `EM-DATE`, ␣, `EM-TIME`, `' LGAPVS01'`, `' PNUM='`, `EM-POLNUM`, `' CNUM='`, `EM-CUSNUM`, `' Write file KSDSPOLY'`, `' RESP='`, `EM-RESPRC`, `' RESP2='`, `EM-RESP2RC` | 99 bayt (`lgapvs01.cbl:75-77`) |

    `CA-ERROR-MSG` = `'COMMAREA='` + COMMAREA'nın ilk 90 baytı (ya da `EIBCALEN` < 91 ise ilk `EIBCALEN` baytı). Tarih ve saat
    düzenekte boştur ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:108-109`); satırlarda tahmin edilmez.
    **Kuyruk kaydı biçimi (satırlarda tahmin edilmez):** LGSTSQ COMMAREA'nın 90 baytını `WRITE-MSG-MSG`'ye kopyalar
    (`lgstsq.cbl:48`, `lgstsq.cbl:70`), kaydın başına 4 baytlık SYSID ve bir boşluk koyar (`lgstsq.cbl:26-28`, `lgstsq.cbl:60`) ve
    `EIBCALEN + 5` bayt yazar (`lgstsq.cbl:71`, `lgstsq.cbl:90`), önce TD `CSMT`'ye, sonra TS `GENAERRS`'e (`lgstsq.cbl:40`,
    `lgstsq.cbl:82`, `lgstsq.cbl:94-99`, `lgstsq.cbl:105-111`; `RESP` kontrol edilmez). 45 ve 87 baytlık mesajlarda kayıt
    SYSID + ␣ + mesajın kendisidir. 99 ve 101 baytlık mesajlarda yazılan uzunluk (104, 106) 95 baytlık `WRITE-MSG`'yi
    (`lgstsq.cbl:25-28`) 9 ve 11 bayt aşar; o baytların içeriği bellek düzenine bağlıdır (C).

15. **Sürpriz 11: yorum-kod çelişkileri.** Gerçek akış her yerde koda göre yazıldı.
    - `lgapdb01.cbl:238`: "Request is not recognised" yorumunun bulunduğu kol ulaşılamaz (Kod gözlemleri 5).
    - `lgapol01.cbl:153`: "Write 90 bytes or as much as we have of commarea"; LGAPOL01'de bu bloğa ulaşılamaz, çünkü tek çağrı yeri
      `EIBCALEN = 0` yoludur (`lgapol01.cbl:98-100`, `lgapol01.cbl:154`). [belge, kodla doğrulanmadı] aynı gözlem:
      `0004-legacy-test-harness.md:115-116`.
    - `lgstsq.cbl:46`: "Delete policy commarea description"; alan bir hata mesajıdır (kopyala-yapıştır kalıntısı, davranışı etkilemez).
    - `lgapdb01.cbl:74`: "customer details length"; dahil edilen `LGPOLICY` poliçe uzunluklarını tanımlar (`lgpolicy.cpy:16-28`).

16. **Sayısal olmayan içerik (C).** Dokuz ticari sayısal alan (`lgapdb01.cbl:488-496`), `CA-BROKERID` ve `CA-PAYMENT`
    (`lgapdb01.cbl:264-265`) `COMP` alanlara taşınır; COMMAREA'da sayısal olmayan içerik (örneğin boşluk) varsa sonuç derleyiciye
    bağlıdır (C). Satır yazılmadı; 16-18'in belirsizliği bu yüzdendir.

17. **Düzenek her durumu ayrı süreçte çalıştırır** ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:50-52`). Bu yüzden
    `WS-REQUIRED-CA-LEN`'e yapılan `ADD`'ler (`lgapol01.cbl:109`, `lgapdb01.cbl:182`, `lgapdb01.cbl:199`) çağrılar arasında birikmez.

18. **Sayı alanlarının taşınması.** `CA-CUSTOMER-NUM` 9(10), `DB2-CUSTOMERNUM-INT` S9(9) COMP'a taşınır (`lgapdb01.cbl:90`,
    `lgapdb01.cbl:176`); 10 haneli değer kesilir (B3). VSAM anahtarı ve hata mesajı ise `CA-CUSTOMER-NUM`'u kesmeden alır
    (`lgapvs01.cbl:101`, `lgapdb01.cbl:179`). Yeni poliçe numarası S9(9) COMP'tan 9(10)'a taşınır (`lgapdb01.cbl:311`), oradan
    X(10)'a (`lgapvs01.cbl:29`, `lgapvs01.cbl:100`): `0001000001`.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 16-01 | LGAPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01ACOM'`. `CA-CUSTOMER-NUM = 0000000001` (müşteri 1 var: `db2cre.jcl:437`). `CA-POLICY-NUM = 0000000000`. Poliçe alanlarında poliçe 10'un, ticari alanlarda `COMMERCIAL` 10'un değerleri (alan alan: "16-01 girdi ve beklenen değerler"). | `CA-RETURN-CODE = '00'` (`lgapdb01.cbl:293`) | `CA-POLICY-NUM = 0001000001` (B4, B6); `CA-LASTCHANGED` = yeni `POLICY` satırının `lastChanged` değeri (B4); diğer alanlar değişmez. Db2'de `POLICY` 1000001 (`policyType = 'C'`) ve `COMMERCIAL` 1000001 (`RequestDate` = `CA-LASTCHANGED`), VSAM'da 64 baytlık `'C'` kaydı ("16-01'den sonra Db2'de ve VSAM'da beklenen durum"). LGSTSQ'ya LINK yok. | `lgapol01.cbl:105`, `lgapol01.cbl:121-126`, `lgapdb01.cbl:198-200`, `lgapdb01.cbl:268-321`, `lgapdb01.cbl:234-235`, `lgapdb01.cbl:486-547`, `lgapdb01.cbl:243-250`, `lgapvs01.cbl:99-108`, `lgapvs01.cbl:135-142`, `lgapvs01.cbl:151-153` | İş kuralı |
| 16-02 | 16-01 ile aynı, ama `CA-B-Customer = 'Burnley Football Club TURF MOOR STADIUM'` + ␣×216 (39 baytlık metin; parçaları `db2cre.jcl:1235`, `db2cre.jcl:1231`, `db2cre.jcl:1236`). | `CA-RETURN-CODE = '00'` (`lgapdb01.cbl:293`) | 16-01 gibi (`CA-POLICY-NUM = 0001000001`, B4, B6); `COMMERCIAL.Customer` 39 baytlık metnin tamamı; VSAM kaydının 34-64. baytları `'Burnley Football Club TURF MOOR'` (31 bayt), `' STADIUM'` VSAM'a yazılmaz. | `lgapdb01.cbl:532`, `lgapvs01.cbl:34`, `lgapvs01.cbl:108` | İş kuralı |
| 16-03 | 16-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000000` (müşteri 0 yok: örnek müşteriler 1-10, `db2cre.jcl:437` … `db2cre.jcl:743`; yeni müşteri numaraları 1000001'den başlar, `db2cre.jcl:111-112`). INSERT POLICY `SQLCODE = -530` [Db2 bilgisi]. | `CA-RETURN-CODE = '70'` (`lgapdb01.cbl:296`) | `CA-POLICY-NUM = 0000000000` ve `CA-LASTCHANGED` değişmez. `POLICY` ve `COMMERCIAL` satırı yok; LGAPVS01 çağrılmaz, VSAM kaydı yok. LGSTSQ'ya 2 LINK (LGAPDB01'den): (1) `ERROR-MSG`, 87 bayt: `' LGAPDB01'`, `' CNUM=0000000000'`, `' PNUM='` + ␣×10, `' INSERT POLICY'` + ␣×2, `' SQLCODE=-00530'`; (2) `CA-ERROR-MSG`, 99 bayt: `'COMMAREA='` + COMMAREA'nın 1-90. baytları (7-8. baytlar `'70'`). | `lgapdb01.cbl:179`, `lgapdb01.cbl:267`, `lgapdb01.cbl:295-298`, `lgapdb01.cbl:564-578`, `lgapdb01.cbl:587-592`, `lgapol01.cbl:126`; [Db2 bilgisi] (`-530`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 16-04 | 16-01 ile aynı; INSERT POLICY `SQLCODE = -904` (B4; `0` ve `-530` dışındaki her değer aynı yolu izler). | `CA-RETURN-CODE = '90'` (`lgapdb01.cbl:301`) | `CA-POLICY-NUM` ve `CA-LASTCHANGED` değişmez. `POLICY` ve `COMMERCIAL` satırı yok; VSAM kaydı yok. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' LGAPDB01'`, `' CNUM=0000000001'`, `' PNUM='` + ␣×10, `' INSERT POLICY'` + ␣×2, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG` (7-8. baytlar `'90'`). | `lgapdb01.cbl:300-303`, `lgapdb01.cbl:564-578`, `lgapdb01.cbl:587-592`, `lgapol01.cbl:126` | İş kuralı |
| 16-05 | 16-01 ile aynı; INSERT COMMERCIAL `SQLCODE = -904` (B4; `0` dışındaki her değer aynı yolu izler), önceki deyimler `0`. | `CA-RETURN-CODE = '90'` (`lgapdb01.cbl:548`); düzenekte (B8). Gerçek CICS'te çağırana ulaşmaz ([CICS bilgisi] 2) | `CA-POLICY-NUM = 0001000001` (B6); `CA-LASTCHANGED` = yeni `POLICY.lastChanged` (B4). `ABEND 'LGSQ'` (`lgapdb01.cbl:551`): düzenekte mesaj + `GOBACK`. `POLICY` 1000001 satırını kod silmez (düzenekte kalır; gerçek CICS'te geri alınması belirsiz, C). `COMMERCIAL` satırı yok; LGAPVS01 çağrılmaz, VSAM kaydı yok. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' CNUM=0000000001'`, `' PNUM=0001000001'`, `' INSERT COMMER'` + ␣×2, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG` (7-8. baytlar `'90'`, 19-28. baytlar `0001000001`). | `lgapdb01.cbl:311-313`, `lgapdb01.cbl:498`, `lgapdb01.cbl:547-551`, `lgapdb01.cbl:562-592`, `lgapol01.cbl:126` | İş kuralı |
| 16-06 | 16-01 ile aynı; VSAM WRITE `RESP = DUPREC` (B4; `NORMAL` dışındaki her değer aynı yolu izler). | `CA-RETURN-CODE = '80'` (`lgapvs01.cbl:144`) | `CA-POLICY-NUM = 0001000001` (B6); `CA-LASTCHANGED` = yeni `POLICY.lastChanged` (B4). `POLICY` ve `COMMERCIAL` 1000001 satırları yazılmış kalır; kod geri almaz (gerçek ortamda kalıcılığı C). VSAM kaydı yazılmaz. LGSTSQ'ya 2 LINK (LGAPVS01'den): (1) `ERROR-MSG`, 101 bayt: `' LGAPVS01'`, `' PNUM=0001000001'`, `' CNUM=0000000001'`, `' Write file KSDSPOLY'`, `' RESP='` + `WS-RESP` (düzeneğin DUPREC için verdiği değer), `' RESP2='` + `EIBRESP2`; (2) `CA-ERROR-MSG` (7-8. baytlar `'80'`). | `lgapvs01.cbl:142-147`, `lgapvs01.cbl:155-172`, `lgapvs01.cbl:180-185`, `lgapdb01.cbl:250`, `lgapol01.cbl:126` | İş kuralı |
| 16-07 | 16-01 ile aynı, ama `CA-LASTCHANGED = '2011-08-23 10:44:34'` + ␣×7 (`db2cre.jcl:1185`); `SELECT LASTCHANGED` `SQLCODE = -904` (B4; host değişkeni yazılmaz). | `CA-RETURN-CODE = '00'` (`lgapdb01.cbl:293`) | `CA-POLICY-NUM = 0001000001` (B6); `CA-LASTCHANGED` girdideki değer kalır. `COMMERCIAL` 1000001'in `RequestDate`'i girdideki `CA-LASTCHANGED`; `POLICY` 1000001'in `lastChanged`'i INSERT anının zaman damgası (ikisi farklı). VSAM kaydı 16-01 gibi. LGSTSQ'ya LINK yok (SELECT'in `SQLCODE`'u kontrol edilmez). | `lgapdb01.cbl:316-322`, `lgapdb01.cbl:525`, `lgapdb01.cbl:547` | İş kuralı |
| 16-08 | 16-01 ile aynı, ama `CA-REQUEST-ID = '01acom'` (küçük harf). | `CA-RETURN-CODE = '99'` (`lgapdb01.cbl:204`) | `CA-POLICY-NUM` değişmez. Db2 ve VSAM değişmez; LGAPVS01'e ve LGSTSQ'ya LINK yok. | `lgapdb01.cbl:184`, `lgapdb01.cbl:202-205`, `lgapol01.cbl:126`; [CICS bilgisi] 1 ve 4 | Arayüz (COMMAREA) kuralı |
| 16-09 | LGAPOL01 çağrılır. `EIBCALEN = 27`. İlk 27 bayt 16-01 gibi (`'01ACOM'`, müşteri 1). | `CA-RETURN-CODE = '98'` (`lgapol01.cbl:114`) | LGAPDB01'e LINK yok; Db2 ve VSAM değişmez; LGSTSQ'ya LINK yok. | `lgapol01.cbl:105`, `lgapol01.cbl:109`, `lgapol01.cbl:113-116` | Arayüz (COMMAREA) kuralı |
| 16-10 | LGAPOL01 çağrılır. `EIBCALEN = 0` (COMMAREA yok). | Yok (COMMAREA yok); `ABEND 'LGCA'` (`lgapol01.cbl:101`), düzenekte mesaj + `GOBACK` (B8) | LGSTSQ'ya 1 LINK: `ERROR-MSG`, 45 bayt: `' LGAPOL01'` + `' NO COMMAREA RECEIVED'`. LGAPDB01 çağrılmaz; Db2 ve VSAM değişmez. | `lgapol01.cbl:98-102`, `lgapol01.cbl:137-152`, `lgapol01.cbl:154` | Arayüz (COMMAREA) kuralı |
| 16-11 | LGAPOL01 çağrılır. `EIBCALEN = 1201`. İlk 1201 bayt 16-01 gibi; 1202. bayt (`CA-B-RejectReason`'ın son baytı) tamponda boşluk (B1; 16-01'de de boşluk). | `CA-RETURN-CODE = '00'` (`lgapdb01.cbl:293`) | 16-01 ile aynı (`CA-POLICY-NUM = 0001000001`, B4, B6; Db2 ve VSAM satırları). | `lgapol01.cbl:113`, `lgapol01.cbl:121-124`, `lgapdb01.cbl:199`, `lgapdb01.cbl:210` | Arayüz (COMMAREA) kuralı |
| 16-12 | LGAPDB01 **doğrudan** çağrılır. `EIBCALEN = 1201`. İlk 1201 bayt 16-01 gibi. | `CA-RETURN-CODE = '98'` (`lgapdb01.cbl:211`) | `CA-POLICY-NUM` değişmez. Db2 ve VSAM değişmez; LGAPVS01'e ve LGSTSQ'ya LINK yok. | `lgapdb01.cbl:182`, `lgapdb01.cbl:198-200`, `lgapdb01.cbl:210-213` | Arayüz (COMMAREA) kuralı |
| 16-13 | LGAPDB01 **doğrudan** çağrılır. `EIBCALEN = 1202`. 16-01'in verisi. | `CA-RETURN-CODE = '00'` (`lgapdb01.cbl:293`) | 16-01 ile aynı (`CA-POLICY-NUM = 0001000001`, B4, B6; Db2 ve VSAM satırları). | `lgapdb01.cbl:210`, `lgapdb01.cbl:219`, `lgapdb01.cbl:243-250` | Arayüz (COMMAREA) kuralı |
| 16-14 | LGAPDB01 **doğrudan** çağrılır. `EIBCALEN = 0` (COMMAREA yok). | Yok (COMMAREA yok); `ABEND 'LGCA'` (`lgapdb01.cbl:168`), düzenekte mesaj + `GOBACK` (B8) | LGSTSQ'ya 1 LINK: `ERROR-MSG`, 87 bayt: `' LGAPDB01'` + `' NO COMMAREA RECEIVED'` + ␣×36 + `'+00000'` (`EM-SQLRC`, B2). Db2 ve VSAM değişmez. | `lgapdb01.cbl:165-169`, `lgapdb01.cbl:564`, `lgapdb01.cbl:575-580` | Arayüz (COMMAREA) kuralı |
| 16-15 | LGAPVS01 **doğrudan** çağrılır. `EIBCALEN = 0`; düzenek 32500 baytlık boşluk tamponu geçirir (B1). | `CA-RETURN-CODE` = ␣␣ (değişmez; dönüş kodunu yalnızca `lgapvs01.cbl:144` yazar) | `CA-Request-ID(4:1)` = ␣ → OTHER kolu: `WF-Policy-Data` = ␣×43. `WRITE FILE` çalışır (B4: `RESP = NORMAL`), `GOBACK`; LGSTSQ'ya LINK yok. Anahtarın 1. baytı ␣; 2-21. baytları boşluk içeren 9(10) alanların X(10)'a taşınmasıyla kurulur, içeriği derleyiciye bağlıdır (C). Gerçek CICS'te `EIBCALEN = 0` iken COMMAREA yoktur; sonuç C. | `lgapvs01.cbl:97-103`, `lgapvs01.cbl:130-131`, `lgapvs01.cbl:135-142`, `lgapvs01.cbl:144`, `lgapvs01.cbl:151-153` | Arayüz (COMMAREA) kuralı |
| 16-16 | 16-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000001`. | `CA-RETURN-CODE = '00'` (`lgapdb01.cbl:293`) | `CA-CUSTOMER-NUM` değişmez; `CA-POLICY-NUM = 0001000001` (B6). `POLICY` 1000001'in `customerNumber`'ı 1 (B3). VSAM anahtarı `'C10000000010001000001'`. | `lgapdb01.cbl:176`, `lgapdb01.cbl:280`, `lgapvs01.cbl:101` | Arayüz (COMMAREA) kuralı |
| 16-17 | LGAPVS01 **doğrudan** çağrılır. `EIBCALEN = 90`. `CA-REQUEST-ID = '01ACOM'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0001000001`; 91. bayttan ötesi tamponda boşluk (B1). VSAM WRITE `RESP = DUPREC` (B4). | `CA-RETURN-CODE = '80'` (`lgapvs01.cbl:144`) | VSAM kaydı yazılmaz. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' LGAPVS01'`, `' PNUM=0001000001'`, `' CNUM=0000000001'`, `' Write file KSDSPOLY'`, `' RESP='` + `WS-RESP`, `' RESP2='` + `EIBRESP2`; (2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın 1-90. baytları (`EIBCALEN < 91` kolu). | `lgapvs01.cbl:142-147`, `lgapvs01.cbl:173-179` | Arayüz (COMMAREA) kuralı |
| 16-18 | LGAPOL01 çağrılır. `EIBCALEN = 28`. İlk 28 bayt 16-01 gibi (`'01ACOM'`, müşteri 1, `CA-POLICY-NUM = 0000000000`); 29-1202. baytlar tamponda boşluk (B1). | belirsiz (`'98'` değil) | belirsiz | `lgapol01.cbl:113`, `lgapol01.cbl:121-124`, `lgapdb01.cbl:210`, `lgapdb01.cbl:264-265`, `lgapdb01.cbl:281-282` | Arayüz (COMMAREA) kuralı |
| 16-19 | LGAPOL01 çağrılır. `EIBCALEN` 1 ile 7 arasında (sınır değer: `7`); 8. bayttan ötesi tamponda boşluk (B1). | `CA-RETURN-CODE` (tamponun 7-8. baytları) = `'98'` (`lgapol01.cbl:114`; B1) | `lgapol01.cbl:105`'teki `'00'` ve `lgapol01.cbl:114`'teki `'98'` `EIBCALEN`'in ötesine, tampona yazılır (B1). LINK yok; Db2 ve VSAM değişmez; LGSTSQ'ya LINK yok. Gerçek CICS'te COMMAREA'nın ötesine yazmanın sonucu C. | `lgapol01.cbl:98`, `lgapol01.cbl:105`, `lgapol01.cbl:109`, `lgapol01.cbl:113-116` | Arayüz (COMMAREA) kuralı |
| 16-20 | LGAPOL01 çağrılır. `EIBCALEN` 947 ile 1201 arasında (sınır değerler: `947` ve `1201`; 16-11'in genellemesi). `EIBCALEN` içindeki girdiler 16-01 ile aynı; ötesi tamponda boşluk (B1). | `CA-RETURN-CODE = '00'` (`lgapdb01.cbl:293`) | B4, B6: `CA-POLICY-NUM = 0001000001`, `CA-LASTCHANGED` = yeni `POLICY.lastChanged`. Bütün sayısal alanlar (sonuncusu `CA-B-Status`, 944-947. baytlar) COMMAREA'nın içindedir. `EIBCALEN`'in ötesindeki metin baytları (`CA-B-RejectReason`'ın tamamı ya da bir kısmı) boşluk olarak yazılır: `EIBCALEN = 947` ise `COMMERCIAL.RejectionReason` = ␣×255. VSAM kaydı 16-01 gibi (VSAM'a giden alanlar 356-363, 386-416 ve 944-947. baytlarda). | `lgapol01.cbl:113`, `lgapol01.cbl:121-124`, `lgapdb01.cbl:210`, `lgapdb01.cbl:488-496`, `lgapdb01.cbl:543`, `lgapvs01.cbl:106-108`; [CICS bilgisi] 1 ve 4 | Arayüz (COMMAREA) kuralı |
| 16-21 | LGAPOL01 çağrılır. `EIBCALEN` 28 ile 946 arasında (sınır değerler: `28` ve `946`; 16-18'in genellemesi). `EIBCALEN` içindeki girdiler 16-01 ile aynı; ötesi tamponda boşluk (B1). | belirsiz (`'98'` değil) | belirsiz | `lgapol01.cbl:113`, `lgapdb01.cbl:210`, `lgapdb01.cbl:264-265`, `lgapdb01.cbl:281-282`, `lgapdb01.cbl:488-496` | Arayüz (COMMAREA) kuralı |
| 16-22 | LGAPDB01 **doğrudan** çağrılır. `EIBCALEN` 18 ile 1201 arasında (sınır değerler: `18` ve `1201`; 16-12'nin genellemesi). `CA-REQUEST-ID = '01ACOM'`, `CA-CUSTOMER-NUM = 0000000001` (9-18. baytlar, `EIBCALEN`'in içinde). | `CA-RETURN-CODE = '98'` (`lgapdb01.cbl:211`) | Db2 ve VSAM değişmez; LGAPVS01'e ve LGSTSQ'ya LINK yok. | `lgapdb01.cbl:172`, `lgapdb01.cbl:176-182`, `lgapdb01.cbl:198-200`, `lgapdb01.cbl:210-213` | Arayüz (COMMAREA) kuralı |
| 16-23 | LGAPDB01 **doğrudan** çağrılır. `EIBCALEN` 8 ile 17 arasında (sınır değerler: `8` ve `17`). `CA-REQUEST-ID = '01ACOM'`; `CA-CUSTOMER-NUM`'un (9-18. baytlar) `EIBCALEN` ötesi tamponda boşluk (B1). | belirsiz | belirsiz | `lgapdb01.cbl:172`, `lgapdb01.cbl:176`, `lgapdb01.cbl:210-213` | Arayüz (COMMAREA) kuralı |

## 16-01 girdi ve beklenen değerler

COMMAREA'nın alan alan girdisi ve dönüşte beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Girdi | Girdinin kaynağı | Dönüşte beklenen | Alanı kullanan / dolduran COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` | `'01ACOM'` | istek kodu: `lgapdb01.cbl:198` | değişmez | `lgapdb01.cbl:184`, `lgapdb01.cbl:223`, `lgapvs01.cbl:99` |
| `CA-RETURN-CODE` | `'00'` (üzerine yazılır) | – | `'00'` | `lgapol01.cbl:105`, `lgapdb01.cbl:172`, `lgapdb01.cbl:293` |
| `CA-CUSTOMER-NUM` | `0000000001` | `db2cre.jcl:437`, `db2cre.jcl:778` | değişmez | `lgapdb01.cbl:176`, `lgapdb01.cbl:179`, `lgapvs01.cbl:101` |
| `CA-POLICY-NUM` | `0000000000` | – | `0001000001` (B4, B6) | `lgapdb01.cbl:311`, `lgapdb01.cbl:313`, `lgapvs01.cbl:100` |
| `CA-ISSUE-DATE` | `'2011-08-22'` | `db2cre.jcl:779` | değişmez | `lgapdb01.cbl:281`, `lgapdb01.cbl:526` |
| `CA-EXPIRY-DATE` | `'2012-08-21'` | `db2cre.jcl:780` | değişmez | `lgapdb01.cbl:282`, `lgapdb01.cbl:527` |
| `CA-LASTCHANGED` | ␣×26 | – | yeni `POLICY.lastChanged` (B4) | `lgapdb01.cbl:318`, `lgapdb01.cbl:525` |
| `CA-BROKERID` | `0000000000` | `db2cre.jcl:783` | değişmez | `lgapdb01.cbl:264` |
| `CA-BROKERSREF` | ␣×10 | `db2cre.jcl:784` (`''`) | değişmez | `lgapdb01.cbl:286` |
| `CA-PAYMENT` | `000000` | `db2cre.jcl:785` | değişmez | `lgapdb01.cbl:265` |
| `CA-B-Address` | `'HURSLEY PARK'` + ␣×243 | `db2cre.jcl:1188` | değişmez | `lgapdb01.cbl:528` |
| `CA-B-Postcode` | `'SO212JN'` + ␣×1 | `db2cre.jcl:1189` | değişmez | `lgapdb01.cbl:529`, `lgapvs01.cbl:106` |
| `CA-B-Latitude` | `'51.026701'` + ␣×2 | `db2cre.jcl:1190` | değişmez | `lgapdb01.cbl:530` |
| `CA-B-Longitude` | `'-1.398903'` + ␣×2 | `db2cre.jcl:1191` | değişmez | `lgapdb01.cbl:531` |
| `CA-B-Customer` | `'IBM'` + ␣×252 | `db2cre.jcl:1192` | değişmez | `lgapdb01.cbl:532`, `lgapvs01.cbl:108` |
| `CA-B-PropType` | `'OFFICES'` + ␣×248 | `db2cre.jcl:1193` | değişmez | `lgapdb01.cbl:533` |
| `CA-B-FirePeril` | `0003` | `db2cre.jcl:1194` | değişmez | `lgapdb01.cbl:488`, `lgapdb01.cbl:534` |
| `CA-B-FirePremium` | `00008000` | `db2cre.jcl:1195` | değişmez | `lgapdb01.cbl:489`, `lgapdb01.cbl:535` |
| `CA-B-CrimePeril` | `0003` | `db2cre.jcl:1196` | değişmez | `lgapdb01.cbl:490`, `lgapdb01.cbl:536` |
| `CA-B-CrimePremium` | `00025000` | `db2cre.jcl:1197` | değişmez | `lgapdb01.cbl:491`, `lgapdb01.cbl:537` |
| `CA-B-FloodPeril` | `0005` | `db2cre.jcl:1198` | değişmez | `lgapdb01.cbl:492`, `lgapdb01.cbl:538` |
| `CA-B-FloodPremium` | `00002000` | `db2cre.jcl:1199` | değişmez | `lgapdb01.cbl:493`, `lgapdb01.cbl:539` |
| `CA-B-WeatherPeril` | `0004` | `db2cre.jcl:1200` | değişmez | `lgapdb01.cbl:494`, `lgapdb01.cbl:540` |
| `CA-B-WeatherPremium` | `00005000` | `db2cre.jcl:1201` | değişmez | `lgapdb01.cbl:495`, `lgapdb01.cbl:541` |
| `CA-B-Status` | `0001` | `db2cre.jcl:1202` | değişmez | `lgapdb01.cbl:496`, `lgapdb01.cbl:542`, `lgapvs01.cbl:107` |
| `CA-B-RejectReason` | `'ACTIVE'` + ␣×249 | `db2cre.jcl:1203` | değişmez | `lgapdb01.cbl:543` |
| `CA-B-FILLER` | ␣×31298 | – | değişmez | kullanılmaz |

## 16-01'den sonra Db2'de ve VSAM'da beklenen durum

Gerçek Db2'de ve VSAM'da beklenen durum; düzenekte veri durumu B4'tür (düzenek veri durumunu senaryoya göre kurar).

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `POLICY` 1000001 | `customerNumber = 1`, `issueDate = 2011-08-22`, `expiryDate = 2012-08-21`, `policyType = 'C'`, `lastChanged` = INSERT anının `CURRENT TIMESTAMP`'i, `brokerId = 0`, `brokersReference` = ␣×10, `payment = 0`. `commission` INSERT'te verilmez; değeri Db2'ye bağlıdır (C), tahmin edilmez. | `lgapdb01.cbl:200`, `lgapdb01.cbl:268-288` |
| `COMMERCIAL` 1000001 | `RequestDate` = `POLICY` 1000001'in `lastChanged`'i (`CA-LASTCHANGED` üzerinden), `StartDate = 2011-08-22`, `RenewalDate = 2012-08-21`, `Address = 'HURSLEY PARK'`, `Zipcode = 'SO212JN'`, `LatitudeN = '51.026701'`, `LongitudeW = '-1.398903'`, `Customer = 'IBM'`, `PropertyType = 'OFFICES'`, `FirePeril = 3`, `FirePremium = 8000`, `CrimePeril = 3`, `CrimePremium = 25000`, `FloodPeril = 5`, `FloodPremium = 2000`, `WeatherPeril = 4`, `WeatherPremium = 5000`, `Status = 1`, `RejectionReason = 'ACTIVE'` (CHAR sütunlar sağdan boşlukla). | `lgapdb01.cbl:316-321`, `lgapdb01.cbl:488-496`, `lgapdb01.cbl:499-545` |
| `KSDSPOLY` | Yeni 64 baytlık kayıt: `'C'` + `'0000000001'` + `'0001000001'` + `'SO212JN'` + ␣×1 + `'0001'` + `'IBM'` + ␣×28. Anahtar: `'C00000000010001000001'`. | `lgapvs01.cbl:99-108`, `lgapvs01.cbl:135-141` |
| Mevcut veri | `POLICY` 1-10, `COMMERCIAL` 9-10 ve `KSDSPOLY`'nin 10 kaydı değişmez. | `lgapdb01.cbl:268-288` (yalnızca INSERT), `lgapvs01.cbl:135` (yalnızca WRITE) |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları. "Başka tür" maddelerine tahmin yazılmadı; bu maddeler,
dalın alınmadığı bir ticari teste bağlandı. Ulaşılamayan dallar "kapsam dışı: ulaşılamaz" gerekçesiyle tabloda tutuldu.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| 1 | `lgapol01.cbl:98-102`: `EIBCALEN = 0` → hata mesajı + `ABEND 'LGCA'` (ortak) | 16-10 | |
| 2 | `lgapol01.cbl:105`: `CA-RETURN-CODE = '00'` (ortak) | 16-01, 16-09, 16-19 | |
| 3 | `lgapol01.cbl:109`, `lgapol01.cbl:113-116`: `EIBCALEN < 28` → `'98'` + RETURN (ortak) | 16-09, 16-19; geçen kol 16-11, 16-18, 16-20, 16-21 | |
| 4 | `lgapol01.cbl:121-124`: `LINK LGAPDB01 LENGTH(32500)` (ortak) | 16-01 | LINK hatası kapsam dışı: kodda dal yok, sonuç C (Kod gözlemleri 3). |
| 5 | `lgapol01.cbl:126`: RETURN; çağıran alt programın kodunu görür (ortak) | 16-01, 16-03, 16-04, 16-05, 16-06, 16-08 | [CICS bilgisi] 4 |
| 6 | `lgapol01.cbl:137-152`: WRITE-ERROR-MESSAGE, `LINK LGSTSQ` (`ERROR-MSG`) (ortak) | 16-10 | |
| 7 | `lgapol01.cbl:154-168`: `EIBCALEN > 0` / `< 91` (ortak) | yanlış kolu 16-10 | Doğru kolu (`lgapol01.cbl:155-167`) kapsam dışı: ulaşılamaz; tek çağrı yeri `EIBCALEN = 0` yolu (Kod gözlemleri 15). |
| 8 | `lgapdb01.cbl:165-169`: `EIBCALEN = 0` → `ABEND 'LGCA'` (ortak) | 16-14 | Zincirde ulaşılmaz (madde 1 önce yakalar); yalnızca doğrudan çağrı. |
| 9 | `lgapdb01.cbl:172`: `'00'` (ortak) | 16-01, 16-12 | |
| 10 | `lgapdb01.cbl:176`: `CA-CUSTOMER-NUM` → S9(9) COMP (ortak) | 16-16, 16-23 | B3 |
| 11 | `lgapdb01.cbl:198-200`: 1. EVALUATE `'01ACOM'` → 1202, `'C'` (ticari) | 16-01 | |
| 12 | `lgapdb01.cbl:186-196`: 1. EVALUATE `'01AEND'`, `'01AHOU'`, `'01AMOT'` (başka tür) | 16-01 | Dal alınmaz. |
| 13 | `lgapdb01.cbl:202-205`: 1. EVALUATE OTHER → `'99'` + RETURN (ortak) | 16-08 | |
| 14 | `lgapdb01.cbl:184`: istek kodu büyük harfe çevrilmez (ortak) | 16-08 | Kod gözlemleri 6 |
| 15 | `lgapdb01.cbl:210-213`: `EIBCALEN < 1202` → `'98'` (ticari uzunluk) | 16-12, 16-22; geçen kol 16-13 | Zincirde ulaşılamaz: LGAPDB01 32500 görür ([CICS bilgisi] 1). |
| 16 | `lgapol01.cbl:113` ile `lgapdb01.cbl:210` arasındaki boşluk (ticari) | 16-11, 16-18, 16-20, 16-21 | Kod gözlemleri 4 |
| 17 | `lgapdb01.cbl:264-265`: `CA-BROKERID`, `CA-PAYMENT` → COMP (ortak) | 16-01 | Sayısal olmayan içerik C (Kod gözlemleri 16). |
| 18 | `lgapdb01.cbl:268-288`: INSERT POLICY (ortak) | 16-01 | |
| 19 | `lgapdb01.cbl:292-293`: `SQLCODE = 0` → `'00'` (ortak) | 16-01 | |
| 20 | `lgapdb01.cbl:295-298`: `-530` → `'70'` + mesaj + RETURN (ortak) | 16-03 | Kod gözlemleri 10 |
| 21 | `lgapdb01.cbl:300-303`: diğer `SQLCODE` → `'90'` + mesaj + RETURN (ortak) | 16-04 | |
| 22 | `lgapdb01.cbl:308-311`: `IDENTITY_VAL_LOCAL`, `SQLCODE` kontrolü yok (ortak) | 16-01 | Hata satırı yazılmadı (Kod gözlemleri 8). |
| 23 | `lgapdb01.cbl:316-321`: `SELECT LASTCHANGED`, `SQLCODE` kontrolü yok (ortak) | 16-01, 16-07 | |
| 24 | `lgapdb01.cbl:525-527`: RequestDate/StartDate/RenewalDate kaynağı (ticari) | 16-01, 16-07 | Kod gözlemleri 7 |
| 25 | `lgapdb01.cbl:234-235`: 2. EVALUATE `'01ACOM'` → INSERT-COMMERCIAL (ticari) | 16-01 | |
| 26 | `lgapdb01.cbl:225-232`: 2. EVALUATE `'01AEND'`, `'01AHOU'`, `'01AMOT'` (başka tür) | 16-01 | Dal alınmaz. |
| 27 | `lgapdb01.cbl:237-239`: 2. EVALUATE OTHER `'99'` (ortak) | – | Kapsam dışı: ulaşılamaz (Kod gözlemleri 5). |
| 28 | `lgapdb01.cbl:488-496`: dokuz ticari sayısal alan → COMP (ticari) | 16-01 | Sayısal olmayan içerik C (Kod gözlemleri 16). |
| 29 | `lgapdb01.cbl:499-545`: INSERT COMMERCIAL (ticari) | 16-01, 16-02 | |
| 30 | `lgapdb01.cbl:547-551`: `SQLCODE` ≠ 0 → `'90'` + mesaj + `ABEND 'LGSQ'` (ticari) | 16-05 | |
| 31 | `lgapdb01.cbl:550` ile `lgapdb01.cbl:551`: geri alma yorumu ile gerçek akış (ticari) | 16-05 | Yorum gerçek CICS davranışıyla uyumlu (Kod gözlemleri 11). |
| 32 | `lgapdb01.cbl:552`: ABEND'den sonraki RETURN (ticari) | – | Kapsam dışı: ulaşılamaz (Kod gözlemleri 11). |
| 33 | `lgapdb01.cbl:243-246`: `LINK LGAPVS01 LENGTH(32500)` (ortak) | 16-01 | LINK hatası kapsam dışı: kodda dal yok, sonuç C. |
| 34 | `lgapdb01.cbl:250`: RETURN (ortak) | 16-01, 16-06 | |
| 35 | `lgapdb01.cbl:562-578`: WRITE-ERROR-MESSAGE, `ERROR-MSG` 87 bayt (ortak) | 16-03, 16-04, 16-05, 16-14 | |
| 36 | `lgapdb01.cbl:580`, `lgapdb01.cbl:587-592`: `EIBCALEN > 0`, ELSE kolu (ortak) | 16-03, 16-04, 16-05; yanlış kolu 16-14 | |
| 37 | `lgapdb01.cbl:581-586`: `EIBCALEN < 91` kolu (ortak) | – | Kapsam dışı: ulaşılamaz. Çağrı yerleri ya `EIBCALEN` ≥ 1202 ister (`lgapdb01.cbl:297`, `lgapdb01.cbl:302`, `lgapdb01.cbl:549`) ya da `EIBCALEN = 0`'dır (`lgapdb01.cbl:167`). |
| 38 | `lgapvs01.cbl:97`: `EIBCALEN` kontrolü yok (ortak) | 16-15 | Zincirde ulaşılmaz; yalnızca doğrudan çağrı. |
| 39 | `lgapvs01.cbl:99-101`: anahtar `'C'` + müşteri + poliçe (ortak) | 16-01, 16-16 | |
| 40 | `lgapvs01.cbl:105-108`: `'C'` kolu; Customer 31 bayta kesilir (ticari) | 16-01, 16-02 | Kod gözlemleri 13 |
| 41 | `lgapvs01.cbl:110-128`: `'E'`, `'H'`, `'M'` kolları (başka tür) | 16-01 | Dal alınmaz. |
| 42 | `lgapvs01.cbl:130-131`: OTHER → veri boşluk (ortak) | – | Kapsam dışı: zincirde ulaşılamaz; yalnızca ticari olmayan bir kodla doğrudan çağrıda (Kod gözlemleri 13). |
| 43 | `lgapvs01.cbl:135-141`: `WRITE KSDSPOLY` (ortak) | 16-01 | |
| 44 | `lgapvs01.cbl:142-147`: `RESP` ≠ `NORMAL` → `'80'` + mesaj + RETURN (ortak) | 16-06, 16-17 | Kod gözlemleri 12 |
| 45 | `lgapvs01.cbl:151-153`: normal çıkış `GOBACK` (ortak) | 16-01 | |
| 46 | `lgapvs01.cbl:155-172`: WRITE-ERROR-MESSAGE, `ERROR-MSG` 101 bayt (ortak) | 16-06, 16-17 | |
| 47 | `lgapvs01.cbl:180-185`: `EIBCALEN` ≥ 91 kolu (ortak) | 16-06 | |
| 48 | `lgapvs01.cbl:174-179`: `EIBCALEN < 91` kolu (ortak) | 16-17 | Zincirde ulaşılmaz; yalnızca doğrudan çağrı. |
| 49 | `lgstsq.cbl:60-66`: `ASSIGN SYSID`, `ASSIGN INVOKINGPROG` (ortak) | 16-03, 16-04, 16-05, 16-06, 16-10, 16-14, 16-17 | Satırlarda yalnızca LGSTSQ'ya giden LINK tahmin edildi (Kod gözlemleri 14). |
| 50 | `lgstsq.cbl:68-71`: LINK ile çağrılma kolu (ortak) | 16-03, 16-04, 16-05, 16-06, 16-10, 16-14, 16-17 | Aynı not. |
| 51 | `lgstsq.cbl:72-79`: `RECEIVE` kolu (ortak) | – | Kapsam dışı: ulaşılamaz; LGSTSQ bu zincirde yalnızca LINK ile çağrılır (`lgapol01.cbl:149`, `lgapdb01.cbl:575`, `lgapvs01.cbl:169` ve ikinci LINK'ler). LINK ile çağrılan programda `INVOKINGPROG`'un dolu olması CICS davranışıdır; hiçbir tahmin satırı buna dayanmadığı için bu dosyada [CICS bilgisi] maddesi olarak tanımlanmadı. |
| 52 | `lgstsq.cbl:83-88`: `'Q='` öneki (ortak) | – | Kapsam dışı: ulaşılamaz; gönderilen mesajlar `EM-DATE` ile ya da `'COMMAREA='` ile başlar (Kod gözlemleri 14). |
| 53 | `lgstsq.cbl:90`, `lgstsq.cbl:94-99`, `lgstsq.cbl:105-111`: uzunluk + 5; TD ve TS kuyruğa yazma (ortak) | 16-03, 16-04, 16-05, 16-06, 16-10, 16-14, 16-17 | Kuyruk kaydı satırlarda tahmin edilmez (Kod gözlemleri 14). |
| 54 | `lgstsq.cbl:113-119`: `'R'` → `SEND` (ortak) | – | Kapsam dışı: ulaşılamaz (madde 51 ile aynı neden). |
| 55 | `lgstsq.cbl:121-122`: RETURN (ortak) | 16-03, 16-04, 16-05, 16-06, 16-10, 16-14, 16-17 | |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları"). B4 satırlarında son sütun iki durumu ayrı yazar:
**düzenekte** (senaryo değer vermez ya da host değişkenine yazmazsa) ve **gerçek Db2/VSAM'de**.

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 16-01 | B4, B6, [CICS bilgisi] 1 ve 4 | `'00'`, `CA-POLICY-NUM = 0001000001`, `CA-LASTCHANGED` = yeni `POLICY.lastChanged` | Düzenekte host değişkenlerine değer konmazsa `CA-POLICY-NUM = 0000000000` (`lgapdb01.cbl:117`, `lgapdb01.cbl:159`) ve `CA-LASTCHANGED` girdideki gibi kalır; dönüş kodu yine `'00'`. Gerçek Db2'de sayaç ilerlemişse numara 1000001'den büyüktür (B6). |
| 16-02 | B4, B6, [CICS bilgisi] 1 ve 4 | 16-01 gibi; VSAM'da Customer'ın ilk 31 baytı | 16-01 ile aynı; 31 bayt kesmesi ön koşula bağlı değildir. |
| 16-04 | B4 | `'90'` | `SQLCODE = 0` → 16-01; `-530` → 16-03. |
| 16-05 | B4, B6, B8 | Düzenekte `'90'`, `POLICY` satırı kalır | Gerçek CICS'te ABEND görevi sonlandırır: çağırana `'90'` ulaşmaz, `POLICY` satırı geri alınır ([CICS bilgisi] 2). `SQLCODE = 0` → 16-01. |
| 16-06 | B4, B6 | `'80'`, Db2 satırları kalır | `RESP = NORMAL` → 16-01. Gerçek ortamda Db2 satırlarının kalıcılığı C. |
| 16-07 | B4, B6 | `'00'`, `CA-LASTCHANGED` ve `RequestDate` girdideki değer | SELECT başarılıysa 16-01. Düzenek başarısız SELECT'te host değişkenine yazarsa `CA-LASTCHANGED` ve `RequestDate` o değer olur. |
| 16-10 | B8 | ABEND mesajı ve `GOBACK`; LGSTSQ'ya 1 LINK | Gerçek CICS'te görev `LGCA` ile sonlanır ([CICS bilgisi] 2); COMMAREA olmadığı için dönüş kodu zaten yoktur. |
| 16-11 | B1, B4, B6, [CICS bilgisi] 1 ve 4 | 16-01 ile aynı | B1 sağlanmazsa 1202. bayt tampondaki bayttır; `COMMERCIAL.RejectionReason`'ın 255. karakteri o olur, dönüş kodu yine `'00'`. |
| 16-13 | B4, B6, [CICS bilgisi] 4 | 16-01 ile aynı | 16-01 ile aynı. |
| 16-14 | B2, B8 | ABEND mesajı ve `GOBACK`; mesajda `'+00000'` | B2 sağlanmazsa `EM-SQLRC` SQLCA'daki değeri gösterir; gerçek CICS'te görev sonlanır ([CICS bilgisi] 2). |
| 16-15 | B1, B4 | Dönüş kodu yazılmaz (␣␣); OTHER kolu, boşluk anahtarla `WRITE`, `GOBACK` | Düzenek `EIBCALEN = 0`'da tampon vermezse COMMAREA adreslenemez; sonuç düzeneğe bağlıdır. `RESP` `NORMAL` değilse `'80'` (`lgapvs01.cbl:142-147`). Gerçek CICS'te COMMAREA yoktur (C). |
| 16-16 | B3, B4, B6, [CICS bilgisi] 1 ve 4 | `'00'`, `POLICY.customerNumber = 1` | Kesme olmazsa `DB2-CUSTOMERNUM-INT = 1000000001`; bu müşteri yoksa INSERT POLICY `-530` [Db2 bilgisi] → `'70'` (16-03 yolu). |
| 16-17 | B1, B4 | `'80'`; LGSTSQ'ya 2 LINK | `RESP = NORMAL` → kayıt yazılır ve dönüş kodu girdideki gibi kalır (LGAPVS01 `'00'` yazmaz). B1 sağlanmazsa `lgapvs01.cbl:106-108` 91. bayttan ötesini tampondan okur; kayıt yazılmadığı için sonuç değişmez. |
| 16-19 | B1 | Tamponun 7-8. baytları `'98'` | B1 sağlanmazsa `EIBCALEN`'in ötesine yazmanın sonucu düzeneğe bağlıdır; gerçek CICS'te C. |
| 16-20 | B1, B4, B6, [CICS bilgisi] 1 ve 4 | `'00'`; `EIBCALEN` ötesindeki metin baytları boşluk olarak yazılır | B1 sağlanmazsa `EIBCALEN` ötesindeki baytlar tampondaki değerlerdir; sayısal alanlar içeride olduğu için dönüş kodu yine `'00'`, yazılan metinler değişir. B4 ve B6 için 16-01'e bakın. |

## Belirsiz satırlar

Bütün satırı belirsiz olanlar 16-18, 16-21 ve 16-23'tür. Diğer satırlarda yalnızca belirtilen hücre belirsizdir; satırın kalanı ön koşulludur.

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 16-06 | Beklenen veri: `POLICY` ve `COMMERCIAL` satırlarının gerçek ortamda kalıcılığı | Kod satırları geri almaz (`lgapvs01.cbl:142-147`); görev sonunda kalıcı olmaları CICS'in görev sonu davranışına bağlı (C). |
| 16-15 | Beklenen veri: VSAM anahtarının 2-21. baytları; gerçek CICS'te sonuç | Anahtar baytları boşluk içeren 9(10) alanlardan X(10)'a taşınır (`lgapvs01.cbl:100-101`); içerik derleyiciye bağlıdır (C). Gerçek CICS'te `EIBCALEN = 0` iken COMMAREA yoktur (C). Düzenekteki dönüş kodu ve akış ön koşullu satırlarda (B1, B4). |
| 16-18 | Dönüş kodu ve beklenen veri (`'98'` olmadığı kesin) | 29-1202. baytlar boşluk (B1): `CA-BROKERID`, `CA-PAYMENT` (`lgapdb01.cbl:264-265`) ve ticari sayısal alanlar (`lgapdb01.cbl:488-496`) boşlukla `COMP`'a taşınır (derleyiciye bağlı, C); `CA-ISSUE-DATE` ve `CA-EXPIRY-DATE` boşlukla `DATE` sütunlarına yazılır (`lgapdb01.cbl:281-282`; Db2'ye bağlı, C). `'98'` değil: LGAPOL01 28'i geçirir (`lgapol01.cbl:113`), LGAPDB01 32500 görür ([CICS bilgisi] 1; `lgapdb01.cbl:210`), başka hiçbir satır `'98'` yazmaz. |
| 16-19 | Beklenen veri: gerçek CICS'te `EIBCALEN`'in ötesine yazılan `'00'` ve `'98'` | Kod uzunluk kontrolünden önce `'00'`, kontrolde `'98'` yazar (`lgapol01.cbl:105`, `lgapol01.cbl:114`); `EIBCALEN` < 8 iken bu baytlar COMMAREA'nın dışındadır. Gerçek CICS'te sonucu C. |
| 16-21 | Dönüş kodu ve beklenen veri (`'98'` olmadığı kesin) | `EIBCALEN` < 947 iken en az bir sayısal ticari alan (sonuncusu `CA-B-Status`, 944-947. baytlar: `lgcmarea.cpy:92`) tampondaki boşlukla COMP'a taşınır (`lgapdb01.cbl:488-496`; derleyiciye bağlı, C). `EIBCALEN` < 100 iken `CA-BROKERID`/`CA-PAYMENT`'ın (75-100. baytlar; `lgapdb01.cbl:264-265`), < 48 iken tarihlerin (`lgapdb01.cbl:281-282`; Db2'ye bağlı, C) bir kısmı ya da tamamı da boştur. `'98'` değil: 16-18 ile aynı gerekçe. |
| 16-23 | Dönüş kodu ve beklenen veri | `lgapdb01.cbl:176` `CA-CUSTOMER-NUM`'u (9-18. baytlar) kısmen ya da tamamen `EIBCALEN`'in ötesinden, boşluk içerirken S9(9) COMP'a taşır; sonucu derleyiciye bağlıdır (C). Program durmazsa kod yolu `'98'`'e gider (`lgapdb01.cbl:198-200`, `lgapdb01.cbl:210-213`); bu bir açıklamadır, tahmin değildir. |

## Test sırası ve veri bağımlılıkları

Öneri; sıra ve veri sıfırlama kararı ekibindir. Gerçek veri (ya da veri durumunu tutan bir düzenek) için geçerlidir; düzenek her
satırın veri durumunu B4 ile kendisi kurarsa satırlar birbirinden bağımsızdır ve her durum ayrı süreçte çalışır
([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:50-52`).

1. **Başlangıç verisi.** `db2cre.jcl` ile yüklenmiş Db2 ve `ksdspoly.txt`'den yüklenmiş `KSDSPOLY` (`adef121.jcl:65-73`).
   - **Müşteri 1 yüklü olmalı** (`db2cre.jcl:437`): 16-01, 16-02, 16-04, 16-05, 16-06, 16-07, 16-11, 16-13 ve 16-16 (kesmeden
     sonra müşteri 1) `POLICY.customerNumber` yabancı anahtarı için ona dayanır.
   - **Poliçe 10 (`POLICY` `db2cre.jcl:777-786`, `COMMERCIAL` `db2cre.jcl:1184-1203`) yüklü olmalı:** 16-01'in girdi değerlerinin
     kaynağıdır ve "16-01'den sonra beklenen durum" tablosunun "mevcut veri" satırı onu sayar. Kod bu satırları okumaz.
   - **Müşteri 0 olmamalı** (16-03): örnek müşteriler 1-10'dur ve yeni müşteri numaraları 1000001'den başlar (`db2cre.jcl:111-112`).
2. **Poliçe numarası sayacı (B6).** Her ekleme satırı `CA-POLICY-NUM = 0001000001` bekler; bu, satırdan önce Db2'nin (ve
   `KSDSPOLY`'nin) sıfırlandığı anlamına gelir. Sıfırlanmazsa:
   - `POLICY` INSERT'i başarılı olan satırlar sayacı birer ilerletir: 16-01, 16-02, 16-05, 16-06, 16-07, 16-11, 16-13, 16-16,
     16-20 (dokuz satır). Art arda çalışırlarsa numaralar 1000001, 1000002, … olur ve satırdaki numara buna göre okunur.
   - INSERT'i başarısız olan 16-03 ve 16-04'ün bir kimlik değeri harcayıp harcamadığı Db2'ye bağlıdır (C). Tanımdaki `CACHE 20`
     (`db2cre.jcl:163`) de numaralarda boşluk bırakabilir (C).
   - 16-08, 16-09, 16-10, 16-12, 16-14, 16-15, 16-17, 16-19, 16-22 Db2'ye hiç yazmaz; sayacı etkilemez. 16-18, 16-21 ve 16-23
     belirsizdir.
3. **VSAM ile Db2 birlikte sıfırlanmalı.** Db2 sıfırlanıp `KSDSPOLY` sıfırlanmazsa, 16-01'in yeniden çalıştırılması aynı anahtarı
   (`'C00000000010001000001'`) yazmaya çalışır; gerçek VSAM'de anahtar zaten olduğu için `DUPREC` beklenir (düzenekte B4) ve satır 16-06'nın yoluna (`'80'`) düşer. 16-16'nın
   anahtarı (`'C10000000010001000001'`) diğerlerinden ayrıdır.
4. **Önerilen sıra.** Önce Db2'ye yazmayan satırlar (16-08, 16-09, 16-10, 16-12, 16-14, 16-15, 16-17, 16-18, 16-19, 16-21, 16-22, 16-23), sonra her biri
   veri sıfırlamasıyla ekleme satırları (16-01, 16-02, 16-11, 16-13, 16-16, 16-20, 16-07), en son hata yolları (16-03, 16-04, 16-05, 16-06).
5. **Sonraki dilimler.** 16-01 sıfırlanmadan bırakılırsa müşteri 1'in iki ticari poliçesi olur (10 ve 1000001) ve `KSDSPOLY`'de
   yeni bir `'C'` kaydı bulunur; müşteri 1 ile yapılan ticari sorgu ve silme testlerinin veri durumu buna göre kurulmalıdır. Silme
   testleri eklenen satırları yok eder; ekleme satırları silmeden önce çalışmalıdır.

## Açık sorular

Ekip kararı gerektiren, tahminleri etkileyen sorular. Tahminler bu soruların cevabını varsaymaz; varsayım gereken yerde ön koşul
kodu kullanıldı.

1. **`-911` örnek değeri.** Karar verildi: "diğer hata" örneği olarak iş birimini geri almayan `-904` kullanılır; satırlar
   buna göre güncellendi. (`-911` kilitlenme kodudur ve yeni sistemde `503`'e eşlenir, ADR 0002 Karar 8.)
2. **ABEND sonrası geri alma.** Karar verildi (`TEMPLATE.md` madde 7): gerçek CICS'te geri alınır ([CICS bilgisi] 2); düzenekte
   ölçülmez, düzenekte görülen sonuç B8'dir.
3. **Kimlik sayacı.** Başarısız INSERT'lerin kimlik değeri harcayıp harcamadığı ve `CACHE 20`'nin etkisi (Test sırası 2) gerçek
   Db2'de sıfırlamasız çalıştırmada beklenen numaraları belirler. Ekip ekleme satırlarından önce her seferinde sıfırlamayı mı seçer?
4. **LGSTSQ'nun kendisi test edilecek mi?** Satırlar yalnızca LGSTSQ'ya gönderileni tahmin eder. Kuyruk kaydının 95 baytı aşan
   kısmı (Kod gözlemleri 14) ancak LGSTSQ ayrı derlenip TD/TS komutları da değiştirilirse görülür.
5. **`EIBCALEN = 0` ile doğrudan çağrı.** 16-15, B1'e eklenen cümleyle (düzenek `EIBCALEN = 0`'da da boşluk tamponu geçirir) ön koşullu
   yazıldı. Düzeneğin bunu gerçekten yapıp yapmadığı doğrulanmalı; yapmıyorsa 16-15 belirsize döner. Anahtar baytları ve gerçek
   CICS sonucu C kalır.
6. **Sayısal olmayan içerik.** Boşluğun `COMP` alana taşınması (16-18, Kod gözlemleri 16) GnuCOBOL ile Enterprise COBOL'da aynı
   sonucu vermeyebilir. Ekip bu satırı yalnızca GnuCOBOL gözlemi olarak mı kaydetmeli?
7. **Örnek veri ile kod uyuşmazlığı.** `COMMERCIAL` 9/10 ve `KSDSPOLY`'nin örnek kayıtları LGAPDB01/LGAPVS01 ile üretilmiş olamaz
   (Kod gözlemleri 9). Bu, C# tarafındaki başlangıç verisi ve karşılaştırma testleri için bilinmelidir: örnek veriden okunan bir
   ticari poliçenin `StartDate`'i `POLICY.issueDate`'ine eşit değildir.
