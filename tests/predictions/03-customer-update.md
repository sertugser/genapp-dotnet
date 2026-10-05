# 03 — Müşteri güncelle (Update Customer)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Müşteri güncelle: müşteri numarasıyla Db2 `CUSTOMER` satırının 9 sütunu ve VSAM `KSDSCUST` kaydı COMMAREA'daki değerlerle yeniden yazılır |
| Programlar (çağrı zinciri) | `LGUCUS01` (iş mantığı) → `EXEC CICS LINK` → `LGUCDB01` (Db2 `CUSTOMER` UPDATE) → `LINK` → `LGUCVS01` (VSAM `READ UPDATE` + `REWRITE`). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. |
| Okunan COBOL dosyaları | `base/src/lgucus01.cbl`, `base/src/lgucdb01.cbl`, `base/src/lgucvs01.cbl`, `base/src/lgstsq.cbl`, `base/src/lgcmarea.cpy`, `base/src/lgpolicy.cpy`, `base/src/lgsetup.cbl` (yalnızca sayaç tanımı, Kod gözlemleri 18 için) |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `CUSTOMER` tanımı (`:110-124`), müşteri 1 ve 2'nin verisi (`:425-480`), müşteri numaraları 1-10. `base/cntl/adef121.jcl`: VSAM `KSDSCUST` tanımı ve yüklemesi. `base/data/ksdscust.txt`: VSAM başlangıç verisi (yükleme zinciri: Kod gözlemleri 13). `base/cntl/cdef121.jcl`, `cdef122.jcl`, `cdef123.jcl`: CICS dosya tanımları. `base/cntl/cobol.jcl`: derleme seçenekleri (03-16). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `base/Architecture.md`, `base/Testing.md`, `base/data/README.md` (Kod gözlemleri 9 ve 13; kodda doğrulanmadı) |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-05 |
| Kaynak commit (`git rev-parse HEAD`) | `61c635f25b88a92aa3ee92e799c9039522c2c8d6` |

## Kaynak etiketleri ve ön koşul kodları

2026-10-05 A/B güncellemesinde eklenen ya da değişen tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL, kurulum betiği ya da bunların yüklediği veri dosyası (dosya + satır).
- **[belge]**: `base/` altındaki açıklama metinleri (`.md`). Belgeye dayanan bir tahmin kodda doğrulanmamıştır.
  Genel ifadeye dayanıp güncelleme için kodda doğrulanmamış olanlar ayrıca
  "[belge, genel ifade, güncelleme için kodda doğrulanmadı]" diye işaretlidir.
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

1. **COMMAREA bayt haritası** (`lgcmarea.cpy:10-26`). Üç program da bu düzeni kullanır (`lgucus01.cbl:73-74`,
   `lgucdb01.cbl:89-92`, `lgucvs01.cbl:56-57`):

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

2. **Hiçbir programda COMMAREA uzunluk kontrolü yok; bu zincirde `98` hiç üretilmez.**
   - LGUCUS01, `WS-CA-HEADER-LEN` (+28), `WS-REQUIRED-CA-LEN` ve `WS-VARY-FIELD`'ı tanımlar
     (`lgucus01.cbl:56-58`, `:62-64`), ama hiçbir satırda kullanmaz.
   - LGUCDB01, `LGPOLICY`'yi dahil eder (`lgucdb01.cbl:76-78`), ama `WS-CUSTOMER-LEN`'i (`lgpolicy.cpy:17`)
     kullanmaz.
   - LGUCVS01, `EIBCALEN`'i yalnızca READ uzunluğu olarak kopyalar (`lgucvs01.cbl:67`, `:71`).
   - Programlar ise 233. bayta kadar okur: UPDATE'te `lgucdb01.cbl:158-166`, REWRITE'ta `lgucvs01.cbl:85-87`.
     Bu yüzden `EIBCALEN ≥ 1` olan her COMMAREA kabul edilir. İstek kimliği `'01UCUS'` ise güncelleme çalışır.
3. **Programlar birbirini sabit uzunlukla çağırır.**
   - LGUCUS01 → LGUCDB01: `LENGTH(32500)` (`lgucus01.cbl:128-131`). LGUCDB01'in `EIBCALEN = 0` kontrolüne
     (`lgucdb01.cbl:117-121`) zincirden ulaşılamaz.
   - LGUCDB01 → LGUCVS01: `LENGTH(225)` (`lgucdb01.cbl:136-139`).
4. **LGUCVS01, UPDATE'in sonucu ne olursa olsun çağrılır.** `UPDATE-CUSTOMER-INFO`, `01` ya da `90` yazar ama
   RETURN yapmaz (`lgucdb01.cbl:171-181`). Ardından `lgucdb01.cbl:136` her durumda çalışır. Sonuçları:
   - Db2'de satır yoksa (`01`) ya da UPDATE hata verdiyse (`90`) VSAM kaydı yine de yeniden yazılır.
     **Db2 ile VSAM tutarsız kalabilir** (N-07).
   - VSAM tarafı hata verirse LGUCVS01, `CA-RETURN-CODE`'u `81` ya da `82` ile ezer ve görev abend eder.
     Bu durumda `01` ya da `90` çağırana ulaşmaz.
5. **Çağrılan programın dönüş kodu kontrol edilmez, LINK'lerde `RESP`/`NOHANDLE` yok** (N-04).
   LGUCUS01 LINK'ten sonra doğrudan RETURN yapar (`lgucus01.cbl:116-120`, `:128-131`). LGUCDB01 de LGUCVS01'in
   sonucuna bakmadan RETURN yapar (`lgucdb01.cbl:136-143`).
6. **`CA-REQUEST-ID` yalnızca LGUCUS01'de kontrol edilir** (N-01). `'01UCUS'` değilse `99` döner
   (`lgucus01.cbl:110-113`). LGUCDB01 ve LGUCVS01 bu alana bakmaz.
7. **Güncelleme 9 sütunun hepsini yazar, alan doğrulaması yok** (N-02). UPDATE'in `SET` listesi
   `lgucdb01.cbl:157-166`.
   - Boş gönderilen alan, Db2'deki değeri boşlukla ezer. Kısmi güncelleme yok.
   - `CA-DOB` X(10), `DATE` sütununa yazılır (`db2cre.jcl:115`); değeri kabul edip etmemek Db2'ye kalır.
   - Diğer metin alanlarının genişliği sütunlarla aynı (`lgcmarea.cpy:16-25` ↔ `db2cre.jcl:113-121`).
   - `CA-NUM-POLICIES` UPDATE'e girmez, ama VSAM kaydına girer (gözlem 12).
8. **`CA-CUSTOMER-NUM` doğrulanmaz** (N-03). `PIC 9(10)` alan, `PIC S9(9) COMP` olan `DB2-CUSTOMERNUM-INT`'e
   taşınır (`lgucdb01.cbl:37`, `:129`). Taşımada kesilme olup olmayacağı derleyici ayarına bağlı. Sayısallık
   kontrolü de yok. VSAM ise anahtar olarak 10 haneli `CA-Customer-Num`'u doğrudan kullanır (`lgucvs01.cbl:72-73`).
   Kesilme olursa Db2 ile VSAM farklı müşteri numaralarını hedefleyebilir.
9. **Hiçbir programda `SYNCPOINT` ya da `ROLLBACK` yok** (N-05). LGUCVS01 abend ederse (`lgucvs01.cbl:81`,
   `:94`) önceden yapılan Db2 UPDATE'inin geri alınıp alınmayacağı kodda yazmıyor. VSAM kümesi `LOG(UNDO)` ile
   tanımlı (`adef121.jcl:20`), ama CICS'in abend sonrası ne yapacağı kodda değil.
   [belge, genel ifade, güncelleme için kodda doğrulanmadı] `base/Architecture.md:48`: uygulama müşteri ve poliçe
   verisini Db2'ye iki aşamalı onayla (two-phase commit) yazar, başarılıysa VSAM'ı günceller. `base/Testing.md:29-30`
   bunu yalnızca ekleme için açıkça söyler: "VSAM güncellemesi başarısız olursa Db2 güncellemesi geri alınır".
   03-06, 03-07, 03-08 ve 03-16'daki "Db2 geri alınır" tahminleri bu belge ifadelerine dayanır.
10. **`-913` için ayrı bir dal yok** (N-06). Güncellemede 0 ve 100 dışındaki her `SQLCODE` `90` olur
    (`lgucdb01.cbl:171-179`). Sorgulamada ise `-913`, `01`'e eşleniyor (`lgicdb01.cbl:197-198`).
11. **Dönüş kodları.** Bu zincirde `98` yok (gözlem 2).

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgucus01.cbl:105`, `lgucdb01.cbl:124` | Başlangıç / başarı | Evet |
    | `99` | `lgucus01.cbl:111` | İstek kimliği `'01UCUS'` değil | Evet |
    | `01` | `lgucdb01.cbl:174` | UPDATE `SQLCODE = 100` | Yalnızca VSAM tarafı başarılıysa |
    | `90` | `lgucdb01.cbl:176` | UPDATE'te diğer SQLCODE'lar | Yalnızca VSAM tarafı başarılıysa |
    | `81` | `lgucvs01.cbl:79` | VSAM READ hatası | Alana yazılır, hemen ardından `ABEND LGV1` |
    | `82` | `lgucvs01.cbl:92` | VSAM REWRITE hatası | Alana yazılır, hemen ardından `ABEND LGV2` |
    | ABEND `LGCA` | `lgucus01.cbl:102`, `lgucdb01.cbl:120` | COMMAREA yok | — |
    | ABEND `LGV1` | `lgucvs01.cbl:81` | VSAM READ hatası | — |
    | ABEND `LGV2` | `lgucvs01.cbl:94` | VSAM REWRITE hatası | — |

12. **VSAM kaydı.**
    - LGUCVS01 önce kaydı `READ … UPDATE` ile okur: `INTO(WS-Customer-Area)` (1024 bayt), `LENGTH(WS-Commarea-Len)`,
      yani `EIBCALEN` (`lgucvs01.cbl:24`, `:67-76`). Okunan kayıt hiçbir satırda kullanılmaz. Eski ve yeni değer
      karşılaştırılmaz.
    - Sonra `REWRITE FROM(CA-Customer-Num) LENGTH(225)` ile yazar (`lgucvs01.cbl:50`, `:85-89`). Yazılan kayıt
      COMMAREA'nın 9-233. baytlarıdır.
    - LGUCDB01, LGUCVS01'e yalnızca `LENGTH(225)`, yani 1-225. baytları geçirir (`lgucdb01.cbl:138`). Kaydın son
      8 baytı (226-233, e-postanın son 8 karakteri) geçirilen alanın dışında kalır.
    - Kayda `CA-NUM-POLICIES` de girer. LGUCUS01 üzerinden gelindiğinde bu değer `'000'`'dır (`lgucus01.cbl:106`).
13. **VSAM tanımı ve başlangıç içeriği** (`base/cntl/`).
    - Küme `adef121.jcl:15-27`'de tanımlı: `KEYS(10 0)` (`:24`), `RECORDSIZE(225 225)` (`:25`), `LOG(UNDO)` (`:20`).
      Anahtar ve kayıt uzunluğu kodla aynı.
    - **Başlangıç içeriği.** Önceki sürümde "repoda yok, belirsiz" yazıyordu. Bu yanlıştı: arama yalnızca
      `base/cntl/` içinde yapılmıştı, `base/data/` görülmemişti.
      - [kod] İçerik `REPRO` ile `<KSDSCUS>` veri kümesinden yüklenir (`adef121.jcl:55-63`). Yer tutucu
        `base/exec/mac1.rexx:48`'de değiştirilir; değeri `PDSHLQ || ".KSDSCUST.TXT"` (`base/exec/cust1.rexx:55`).
        `base/bin/install.sh:36`, `base/data/ksdscust.txt` dosyasını `${GENAPP}.KSDSCUST.TXT` veri kümesine kopyalar.
        `PDSHLQ` ile `GENAPP` öneklerinin aynı değere bağlandığı satır ayrıca doğrulanmadı.
      - [belge] `base/data/README.md:9`: `ksdscust.txt` uygulamanın örnek verisidir; kayıt uzunluğu 225
        (`base/data/README.md:30`).
      - [kod] `base/data/ksdscust.txt`: 10 kayıt, her biri 225 bayt, anahtarları `0000000001` … `0000000010`
        (`ksdscust.txt:1-10`). `0000000000` ve `0000000011` anahtarlı kayıt yok.
      - VSAM'daki başlangıç verisi Db2'dekinin aynısı değil. Müşteri 1 örneği (`ksdscust.txt:1` ile
        `db2cre.jcl:438-446`):
        - ad ve soyad büyük harf (`ANDREW`, `PANDY`),
        - posta kodu 6 karakter (`PI101O`; Db2'de `PI101OO`),
        - cep telefonu alanında `01962 811234`, ev telefonu alanında `07799 123456` (Db2'de tersi),
        - `NUM-POLICIES` alanı boşluk.

        03-01 kaydın tamamını yeniden yazdığı için bu farklar güncellemenin sonucunu etkilemez.
    - CICS dosya tanımları `Recordsize(82)`, `Keylength(10)`, `Update(Yes)` diyor (`cdef121.jcl:137-142`,
      `cdef122.jcl:231-236`, `cdef123.jcl:263-268`). 82, küme tanımındaki 225'ten farklı. Bunun davranışa etkisi
      kodda yok: **belirsiz**.
14. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGUCUS01 45 bayt (`lgucus01.cbl:39-44`), LGUCDB01 71 bayt (`lgucdb01.cbl:56-66`),
      LGUCVS01 85 bayt (`lgucvs01.cbl:31-44`). İkinci mesaj `CA-ERROR-MSG` üçünde de 99 bayttır: `'COMMAREA='`
      + 90 bayt.
    - LGUCDB01: `EM-SQLREQ` = `' UPDATE CUST  '` (14 karakter) + 2 boşluk (`lgucdb01.cbl:64`, `:154`).
      `EM-CUSNUM` ← `CA-CUSTOMER-NUM` (`lgucdb01.cbl:131`).
    - LGUCVS01: `EM-CUSNUM` ← `CA-Customer-Num` (`lgucvs01.cbl:114`). Mesajdaki sabit metin
      `' Re-write  KSDSCUST '` (`lgucvs01.cbl:39-40`) ve bu metin **READ hatasında da** (`lgucvs01.cbl:80`)
      REWRITE hatasında da (`lgucvs01.cbl:93`) aynen kullanılır.
    - LGUCDB01'in `EIBCALEN = 0` mesajı: 21 karakterlik `' NO COMMAREA RECEIVED'`, 47 baytlık `EM-VARIABLE`
      grubuna taşınır ve `' CNUM='` ile `' SQLCODE='` sabitlerini ezer (`lgucdb01.cbl:61-66`, `:118`). Sonra
      `MOVE SQLCODE TO EM-SQLRC` son 6 baytı ezer (`lgucdb01.cbl:191`). Henüz SQL çalışmadığı için bu SQLCODE'un
      değeri kodda yok.
    - `WRITE-ERROR-MESSAGE` içindeki `IF EIBCALEN > 0` / `< 91` / `ELSE` dalları COMMAREA'nın ne kadarının
      gönderileceğini seçer (`lgucus01.cbl:157-171`, `lgucdb01.cbl:207-221`, `lgucvs01.cbl:121-135`). Zincirde
      LGUCDB01 `EIBCALEN = 32500`, LGUCVS01 `EIBCALEN = 225` görür; ikisinde de `ELSE` dalı çalışır. `< 91`
      dallarına yalnızca doğrudan çağrıyla ulaşılır (03-20, 03-23).
15. **LGSTSQ kuyruk kaydı** (`base/src/lgstsq.cbl`).
    - LINK ile çağrıldığında mesaj `COMMA-DATA`'dan alınır ve uzunluk `EIBCALEN` olur (`lgstsq.cbl:68-71`).
      LINK'te `INVOKINGPROG`'un dolu gelmesi CICS'in kendi tanımıdır, kodda yazmıyor. Terminal yolu
      (`lgstsq.cbl:72-80`) ve `SEND TEXT` (`lgstsq.cbl:113-119`) bu zincirden çalışmaz.
    - Bu operasyonun mesajları tarih ya da `'COMMAREA='` ile başlar. `'Q='` koşulu (`lgstsq.cbl:83`) tutmaz,
      kuyruk adı `GENAERRS` kalır (`lgstsq.cbl:82`).
    - Kayıt = `SYSID` (4) + boşluk + mesaj. Uzunluk = gelen uzunluk + 5 (`lgstsq.cbl:25-28`, `:71`, `:90`). Aynı
      kayıt TD `CSMT`'ye ve TS `GENAERRS`'e yazılır (`lgstsq.cbl:94-111`). İki yazmanın `RESP`'i alınır ama
      kontrol edilmez.
    - `COMMA-DATA` 90 bayt (`lgstsq.cbl:48`). 99 baytlık `CA-ERROR-MSG` geldiğinde yalnızca ilk 90 bayt alınır:
      `'COMMAREA='` + COMMAREA'nın ilk **81** baytı (`lgstsq.cbl:70`). Kayıt uzunluğu 99 + 5 = 104, ama
      `WRITE-MSG` 95 bayt (`lgstsq.cbl:25-28`). Kaydın son 9 baytı `WRITE-MSG`'nin dışından okunur.
    - `ERROR-MSG` mesajları (45, 71 ve 85 bayt) kayda eksiksiz girer.
16. **LGUCUS01'in COMMAREA dökümü ölü kod.** LGUCUS01'de `WRITE-ERROR-MESSAGE` yalnızca `EIBCALEN = 0` dalından
    çağrılır (`lgucus01.cbl:101`). İçindeki `IF EIBCALEN > 0` (`lgucus01.cbl:157`) her zaman yanlıştır.
    `lgucus01.cbl:158-170` arasına hiçbir girdiyle ulaşılamaz.
17. **WORKING-STORAGE kalıcılığı.**
    - **Birikme yok.** `WS-REQUIRED-CA-LEN` hiç kullanılmıyor (gözlem 2). Sayaç ya da bayrak gibi bir alan yok.
      `WS-RETRY` her çağrıda boşlukla sıfırlanıyor (`lgucdb01.cbl:112`) ve başka hiçbir yerde kullanılmıyor.
      `DB2-CUSTOMERNUM-INT` her çağrıda yeniden yazılıyor (`lgucdb01.cbl:129`).
    - Tek uç durum: LGUCDB01'in `EIBCALEN = 0` dalı, `EM-VARIABLE`'daki `' CNUM='` ve `' SQLCODE='` sabitlerini
      ezer (`lgucdb01.cbl:118`) ve hiçbir satır bunları geri yazmaz. Bu dal ABEND ile biter (`lgucdb01.cbl:120`).
      Düzenekte ABEND süreci bitirmez ve LGUCDB01 aynı WORKING-STORAGE ile tekrar çağrılırsa, sonraki `90` hata
      mesajlarında `' CNUM='` yerine `' NO CO'`, `' SQLCODE='` yerine 9 boşluk görünür. `EM-CUSNUM`
      (`lgucdb01.cbl:131`), `EM-SQLREQ` (`lgucdb01.cbl:154`) ve `EM-SQLRC` (`lgucdb01.cbl:191`) her çağrıda yeniden
      yazıldığı için etkilenmez.
    - Düzeneğin bunu nasıl ele alacağı ekip kararıdır (Sercan, Oğuz).
18. **Test verisi** (`base/cntl/db2cre.jcl`). Bu değerler yalnızca test veritabanı bu JCL ile yüklendiyse ve
    sonradan değiştirilmediyse geçerlidir.
    - Müşteri numaraları 1-10 (`db2cre.jcl:437`, `:471`, `:505`, `:539`, `:573`, `:607`, `:641`, `:675`, `:709`,
      `:743`). 11 yok.
    - 03-01'de güncellenen satır müşteri 1 (`db2cre.jcl:437-446`), yeni değerler müşteri 2'nin değerleri
      (`db2cre.jcl:472-480`). Böylece her girdinin JCL'de bir kaynağı var.
    - **Olmayan müşteri için `0000000000` kullanılır** (03-02, 03-03, 03-23). Sayaç 11'den başlar
      ([kod] `lgsetup.cbl:53`, `:183-187`), bu yüzden 01'deki ilk başarılı ekleme müşteri 11'i hem Db2'ye hem VSAM'a
      yazar (`01-customer-add.md`, Kod gözlemleri 17). 0 ise, `LGSE 0` ile çalıştırılmadıysa (`lgsetup.cbl:128-135`), hiçbir yoldan atanmaz: JCL 1-10'u verir, kimlik değeri
      1000001'den başlar (`db2cre.jcl:111-112`). VSAM başlangıç verisinde de 0 yok (`ksdscust.txt:1-10`).

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 03-01 | LGUCUS01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01UCUS'` (`lgucus01.cbl:110`). `CA-CUSTOMER-NUM = 0000000001` (müşteri 1: `db2cre.jcl:437`). Müşteri alanlarında müşteri 2'nin değerleri var (`db2cre.jcl:472-480`; alan alan aşağıdaki **"03-01 girdi ve beklenen değerler"** tablosunda). Db2 yalnızca `db2cre.jcl` ile yüklenmiş. `KSDSCUST`'ta `0000000001` anahtarlı kayıt var [kod] `ksdscust.txt:1` (yükleme zinciri: Kod gözlemleri 13). Ön koşul: READ ve REWRITE `NORMAL` döner. | `CA-RETURN-CODE = 00` | `CA-NUM-POLICIES = 000`. Diğer bütün alanlar girdideki gibi kalır.<br>Yan etkiler: `CUSTOMER`'da müşteri 1'in 9 sütunu girdideki değerlerle değişir. `KSDSCUST`'taki kayıt COMMAREA'nın 9-233. baytlarıyla yeniden yazılır. Son 8 bayt [B1] boşluk olur, çünkü girdideki e-postanın son 8 karakteri boşluk (ön koşullu; Kod gözlemleri 12). `CUSTOMER_SECURE`'a dokunulmaz. Hata mesajı yazılmaz. | `lgucus01.cbl:105-106`, `:110-113`, `:116`, `:120`, `:128-131`<br>`lgucdb01.cbl:124`, `:129`, `:154-171`, `:136-139`, `:143`<br>`lgucvs01.cbl:69-77`, `:85-90`, `:100-102`<br>`ksdscust.txt:1` | İş kuralı |
| 03-02 | 03-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000000`. Müşteri 0 Db2'de yok ve hiçbir yoldan atanmaz [kod] (Kod gözlemleri 18); UPDATE `SQLCODE = 100` döner. `KSDSCUST`'ta da `0000000000` anahtarlı kayıt yok [kod] `ksdscust.txt:1-10`. [B4] Düzenek, kaydı olmayan anahtarla yapılan READ'de `NORMAL` dışında bir RESP döndürür. | Dönüş kodu yok. LGUCDB01 `CA-RETURN-CODE`'a `01` yazar, ama LGUCVS01 her durumda çağrılır. READ başarısız olunca LGUCVS01 alanı `81` ile ezer, ardından görev `ABEND` kodu `LGV1` ile (NODUMP) sonlanır. **Müşteri bulunamadığında çağıran `01` görmez.** Çağıranın `81`'i görüp görmeyeceği **belirsiz**. | Db2'de hiçbir satır değişmez, VSAM'a yazılmaz.<br>LGUCDB01 hata mesajı yazmaz (`SQLCODE = 100` dalında yok).<br>Yan etki: LGUCVS01, `LGSTSQ`'ya iki LINK yapar (kayıt biçimi: Kod gözlemleri 15).<br>(1) `ERROR-MSG` (85 bayt): tarih, saat, `' LGUCVS01'`, `' CNUM='` + `0000000000`, `' Re-write  KSDSCUST '` (READ hatası olduğu halde), `' RESP='` + RESP, `' RESP2='` + RESP2.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + gelen 225 baytın ilk 90'ı (`EIBCALEN = 225`, `ELSE` dalı). İçinde `81` var. | `lgucdb01.cbl:171-174`, `:136-139`<br>`lgucvs01.cbl:69-83`, `:112-120`, `:128-133` | İş kuralı |
| 03-03 | 03-02 ile aynı: Db2'de müşteri 0 yok, UPDATE `SQLCODE = 100` döner. Ama [B4] düzenek testten önce `KSDSCUST`'a `0000000000` anahtarlı bir kayıt ekler, Db2'ye eklemez. Böylece Db2 ile VSAM tutarsız olur; başlangıç verisinde böyle bir durum yok [kod] `ksdscust.txt:1-10`. READ ve REWRITE `NORMAL` döner. | `CA-RETURN-CODE = 01` | Db2'de hiçbir satır değişmez.<br>**Db2'de müşteri olmadığı halde VSAM kaydı yeni değerlerle yeniden yazılır** (Kod gözlemleri 4).<br>`CA-NUM-POLICIES = 000`. Hata mesajı yazılmaz. | `lgucdb01.cbl:171-174`, `:136-139`<br>`lgucvs01.cbl:69-77`, `:85-90`, `:100-102` | İş kuralı |
| 03-04 | 03-01 ile aynı. [B4] Düzenek UPDATE'te 0 ve 100 dışında bir `SQLCODE` üretir. VSAM READ ve REWRITE `NORMAL` döner. | `CA-RETURN-CODE = 90` | Müşteri 1'in Db2 satırının durumu **belirsiz** (kod yalnızca SQLCODE'a bakıyor).<br>**Db2 güncellenemediği halde VSAM kaydı yeni değerlerle yeniden yazılır** (Kod gözlemleri 4).<br>Yan etki: LGUCDB01, `LGSTSQ`'ya iki LINK yapar.<br>(1) `ERROR-MSG` (71 bayt): tarih, saat, `' LGUCDB01'`, `' CNUM='` + `0000000001`, `' UPDATE CUST  '` + 2 boşluk, `' SQLCODE='` + SQLCODE.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın ilk 90 baytı (`EIBCALEN = 32500`, `ELSE` dalı). | `lgucdb01.cbl:129-131`, `:154`, `:171-179`, `:191`, `:202-207`, `:214-219`, `:136-139`<br>`lgucvs01.cbl:85-90`, `:100-102` | İş kuralı |
| 03-05 | 03-01 ile aynı. [B4] Düzenek UPDATE'te `SQLCODE = -913` üretir. VSAM READ ve REWRITE `NORMAL` döner. | `CA-RETURN-CODE = 90` | 03-04 ile aynı.<br>Sorgulamadan farklı: orada `-913`, `01`'e eşleniyor (02-03). Güncellemede `-913` için ayrı bir dal yok (Kod gözlemleri 10). | `lgucdb01.cbl:171-179`<br>(karşılaştırma: `lgicdb01.cbl:197-198`) | İş kuralı |
| 03-06 | 03-04 ile aynı ([B4] düzenek UPDATE'te 0 ve 100 dışında bir `SQLCODE` üretir), ama [B4] düzenek VSAM READ'de de `NORMAL` dışında bir RESP üretir. | Dönüş kodu yok. LGUCDB01'in yazdığı `90`, LGUCVS01'de `81` ile ezilir, ardından görev `ABEND` kodu `LGV1` ile (NODUMP) sonlanır. Çağıranın `81`'i görüp görmeyeceği **belirsiz**. | Db2: müşteri 1'in satırı **değişmez**; abend olduğu için güncelleme geri alınır [belge, genel ifade, güncelleme için kodda doğrulanmadı] `base/Architecture.md:48` (Kod gözlemleri 9). VSAM'a yazılmaz.<br>Yan etki: `LGSTSQ`'ya dört LINK yapılır: LGUCDB01'in iki mesajı (03-04'teki gibi) ve LGUCVS01'in iki mesajı (03-02'deki gibi, `' CNUM='` + `0000000001`). | `lgucdb01.cbl:175-178`, `:136-139`<br>`lgucvs01.cbl:77-81`<br>[belge] `base/Architecture.md:48` | İş kuralı |
| 03-07 | 03-01 ile aynı. UPDATE başarılı (`SQLCODE = 0`). [B4] Düzenek testten önce `KSDSCUST`'tan `0000000001` kaydını siler (başlangıç verisinde var: [kod] `ksdscust.txt:1`); READ `NORMAL` dışında bir RESP döner. | Dönüş kodu yok. `CA-RETURN-CODE`'a `81` yazılır, ardından görev `ABEND` kodu `LGV1` ile (NODUMP) sonlanır. Çağıranın `81`'i görüp görmeyeceği **belirsiz**. | Db2 UPDATE'i önceden çalıştı; abend olduğu için **geri alınır** ve müşteri 1'in satırı eski değerlerinde kalır ([kod] `db2cre.jcl:438-446`) [belge, genel ifade, güncelleme için kodda doğrulanmadı] `base/Architecture.md:48` (Kod gözlemleri 9). VSAM'a yazılmaz.<br>Yan etki: LGUCVS01'in iki mesajı (03-02'deki gibi, `' CNUM='` + `0000000001`). | `lgucdb01.cbl:171`, `:136-139`<br>`lgucvs01.cbl:69-83`<br>`ksdscust.txt:1`, `db2cre.jcl:438-446`<br>[belge] `base/Architecture.md:48` | İş kuralı |
| 03-08 | 03-01 ile aynı. UPDATE başarılı, VSAM READ `NORMAL`. [B4] Düzenek REWRITE'ta `NORMAL` dışında bir RESP üretir. | Dönüş kodu yok. `CA-RETURN-CODE`'a `82` yazılır, ardından görev `ABEND` kodu `LGV2` ile (NODUMP) sonlanır. Çağıranın `82`'yi görüp görmeyeceği **belirsiz**. | Db2 UPDATE'i ve VSAM kaydı **geri alınır**: müşteri 1'in Db2 satırı eski değerlerinde ([kod] `db2cre.jcl:438-446`), VSAM kaydı başlangıç halinde ([kod] `ksdscust.txt:1`) kalır [belge, genel ifade, güncelleme için kodda doğrulanmadı] `base/Architecture.md:48`. VSAM kümesi geri alınabilir olarak tanımlı ([kod] `adef121.jcl:20`, `LOG(UNDO)`) (Kod gözlemleri 9).<br>Yan etki: LGUCVS01'in iki mesajı. Metin yine `' Re-write  KSDSCUST '`, RESP ve RESP2 REWRITE'ınki. İkinci mesajda `82` var. | `lgucvs01.cbl:85-96`, `:112-120`, `:128-133`<br>`adef121.jcl:20`, `ksdscust.txt:1`, `db2cre.jcl:438-446`<br>[belge] `base/Architecture.md:48` | İş kuralı |
| 03-09 | LGUCUS01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID` `'01UCUS'` dışında bir değer (örn. boşluk). Diğer girdiler 03-01 ile aynı. | `CA-RETURN-CODE = 99` | LGUCDB01 çağrılmaz: Db2 ve VSAM değişmez. Hata mesajı yazılmaz.<br>`CA-NUM-POLICIES = 000` (kontrolden önce yazılır). Diğer alanlar değişmez. | `lgucus01.cbl:105-106`, `:110-113`, `:119-120` | Arayüz (COMMAREA) kuralı |
| 03-10 | 03-01 ile aynı, ama `CA-FIRST-NAME` ve `CA-LAST-NAME` boşluk. | `CA-RETURN-CODE = 00` | Hiçbir programda alan doğrulaması yok ve UPDATE 9 sütunun hepsini yazar. Müşteri 1'in `firstName` ve `lastName` sütunları boşlukla ezilir. Önceki değerler: `Andrew`, `Pandy` (`db2cre.jcl:438-439`). VSAM kaydında da bu alanlar boşluk olur. Geri kalanı 03-01 ile aynı (B1'e bağlı VSAM kuyruğu, yani kaydın son 8 baytı, hariç). | `lgucdb01.cbl:155-169` | İş kuralı |
| 03-11 | 03-01 ile aynı, ama `CA-DOB` = 10 boşluk. | **belirsiz**. COBOL tarihi doğrulamaz, değer doğrudan `DATE` sütununa gider (`db2cre.jcl:115`). Db2 reddederse `90` (03-04 yolu: VSAM yine de yazılır ve kayıttaki doğum tarihi boşluk olur). Kabul ederse `00`. | **belirsiz** | `lgucdb01.cbl:160`, `:171-179`, `:136-139` | İş kuralı |
| 03-12 | LGUCUS01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGUCDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGUCUS01'`, `' NO COMMAREA RECEIVED'`). COMMAREA dökümü gönderilmez (`EIBCALEN > 0` sağlanmaz). | `lgucus01.cbl:99-103`, `:152-157` | Arayüz (COMMAREA) kuralı |
| 03-13 | LGUCUS01 çağrılır. `EIBCALEN` 1 ile 5 arasında. | [B1] Tamponun 7-8. baytları `99` olur (ön koşullu) [kod] `lgucus01.cbl:110-111`. | Uzunluk yüzünden **reddedilmez** (kontrol yok). `CA-REQUEST-ID` (1-6. baytlar) kısmen COMMAREA dışında kalır, `'01UCUS'` karşılaştırması dışarıdaki baytları da okur.<br>[B1] Dışarıdaki baytlar boşluk olduğu için kimliğin 6. baytı boşluktur ve kimlik `'01UCUS'`'a eşit olamaz (ön koşullu). `99` yolu çalışır: LGUCDB01 çağrılmaz, Db2 ve VSAM değişmez, hata mesajı yazılmaz. Sonuç tamponda: 7-8. baytlar `99`, 91-93. baytlar `000` [kod] `lgucus01.cbl:105-106`, `:110-113`. | `lgcmarea.cpy:10-11`<br>`lgucus01.cbl:105-106`, `:110-113` | Arayüz (COMMAREA) kuralı |
| 03-14 | LGUCUS01 çağrılır. `EIBCALEN` 6 ile 17 arasında. `CA-REQUEST-ID = '01UCUS'`. | **belirsiz** | **Reddedilmez.** Uzunluk kontrolü yok; istek kimliği COMMAREA'nın içinde ve doğru olduğu için LGUCDB01 `LENGTH(32500)` ile çağrılır. `98` dönmez (bu zincirde hiç yok).<br>Belirsiz kısımlar: `CA-CUSTOMER-NUM` (9-18) kısmen ya da tamamen COMMAREA dışında, yani hangi müşterinin güncelleneceği **belirsiz**. UPDATE'e giden bütün müşteri alanları (19-233) ve VSAM anahtarı ile kaydı da dışarıda. `EIBCALEN` 6-7'de `CA-RETURN-CODE` da kısmen dışarıda. | `lgucus01.cbl:56-58`, `:110-113`, `:128-131`<br>`lgucdb01.cbl:129`, `:155-168` | Arayüz (COMMAREA) kuralı |
| 03-15a | LGUCUS01 çağrılır. `EIBCALEN` 58 ile 232 arasında (doğum tarihi, 49-58. baytlar, COMMAREA'nın içinde). `CA-REQUEST-ID = '01UCUS'`, `CA-CUSTOMER-NUM = 0000000001`, COMMAREA'nın içinde kalan alanlarda 03-01'in değerleri var. Müşteri 1 Db2'de ve VSAM'da var ([kod] `db2cre.jcl:437`, `ksdscust.txt:1`). READ ve REWRITE `NORMAL` döner. | [B1] `CA-RETURN-CODE = 00` (ön koşullu). **Reddedilmez.**<br>B1 sağlanmazsa sonuç belirsiz: kod yollarına göre `00` ya da `90` dönebilir [kod] `lgucdb01.cbl:171-179`, ama COMMAREA dışından okuma ortama göre abend de üretebilir. | Kesin:<br>• Güncellenen satır müşteri 1 (numara COMMAREA'nın içinde).<br>• COMMAREA içindeki baytlar girdideki değerlerle yazılır.<br>[B1] ile (ön koşullu):<br>• Her sütunun `EIBCALEN`'in ötesine düşen kısmı tampondan, yani boşluk olarak okunur ve Db2'ye yazılır. `CHAR` sütunlarına boşluk yazılabildiğini JCL de gösteriyor [kod] `db2cre.jcl:441`, `:479`. Örneğin `EIBCALEN = 90`'da telefonlar ve e-posta boşluk olur.<br>• VSAM kaydında `EIBCALEN`'in ötesi boşluk; yalnız 91-93. baytlar her durumda `000` (LGUCUS01 bunu `lgucus01.cbl:106`'da tampona yazar). | `lgucus01.cbl:106`, `:110-113`, `:128-131`<br>`lgucdb01.cbl:155-171`, `:136-139`<br>`lgucvs01.cbl:85-89` | Arayüz (COMMAREA) kuralı |
| 03-15b | 03-15a ile aynı, ama `EIBCALEN` 18 ile 57 arasında (doğum tarihi en az kısmen COMMAREA dışında). | **belirsiz**. **Reddedilmez.** Kod yollarına göre UPDATE başarılıysa `00`, hata verirse `90` dönebilir (VSAM yine de yazılır), ama COMMAREA dışından okuma ortama göre abend de üretebilir. | Kesin:<br>• Güncellenen satır müşteri 1 (numara COMMAREA'nın içinde).<br>• COMMAREA içindeki alanlar girdideki değerlerle yazılır.<br>Belirsiz:<br>• `EIBCALEN`'in ötesindeki alanlar dışarıdan okunup Db2'ye ve VSAM'a yazılır; doğum tarihi de bunların arasında. B1 olsa bile boşluk ya da yarım bir tarih `DATE` sütununa gider ve sonucu Db2 belirler.<br>• `CA-NUM-POLICIES` (91-93) COMMAREA dışına yazılır. | `lgucus01.cbl:106`, `:110-113`, `:128-131`<br>`lgucdb01.cbl:160`, `:155-171`, `:136-139`<br>`lgucvs01.cbl:85-89` | Arayüz (COMMAREA) kuralı |
| 03-16 | 03-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000001` (10 haneli). [B4] Düzenek, kaydı olmayan anahtarla yapılan READ'de `NORMAL` dışında bir RESP döndürür (03-02 gibi). | [B3] Dönüş kodu yok (ön koşullu): `CA-RETURN-CODE`'a `81` yazılır, ardından görev `ABEND` kodu `LGV1` ile (NODUMP) sonlanır [kod] `lgucvs01.cbl:77-81`. Çağıranın `81`'i görüp görmeyeceği **belirsiz** (03-02 ile aynı). | [B3] `PIC 9(10)` → `PIC S9(9) COMP` taşımasında en soldaki hane kesilir ve `DB2-CUSTOMERNUM-INT = 1` olur [kod] `lgcmarea.cpy:12`, `lgucdb01.cbl:37`, `:129`. Db2'de **müşteri 1** güncellenir (UPDATE başarılı).<br>VSAM ise anahtar olarak 10 haneli `1000000001`'i kesilmeden kullanır [kod] `lgucvs01.cbl:72-73`. Bu anahtar VSAM'da yok [kod] `ksdscust.txt:1-10`, bu yüzden READ başarısız olur.<br>Müşteri 1'in Db2 güncellemesi abend yüzünden **geri alınır** [belge, genel ifade, güncelleme için kodda doğrulanmadı] `base/Architecture.md:48` (Kod gözlemleri 9).<br>Yan etki: LGUCVS01'in iki mesajı (03-02'deki gibi, `' CNUM='` + `1000000001`).<br>B3 sağlanmazsa sonuç **belirsiz**. | `lgcmarea.cpy:12`<br>`lgucdb01.cbl:37`, `:129`, `:171`<br>`lgucvs01.cbl:72-73`, `:77-81`<br>`cobol.jcl:44-50`, `ksdscust.txt:1-10`<br>[belge] `base/Architecture.md:48` | Arayüz (COMMAREA) kuralı |
| 03-17 | 03-01 ile aynı, ama `CA-CUSTOMER-NUM` sayısal değil (örn. boşluk). | **belirsiz** | **belirsiz**. Hiçbir programda sayısallık kontrolü yok. Değer doğrudan `DB2-CUSTOMERNUM-INT`'e taşınır ve VSAM anahtarı olarak da kullanılır. | `lgucdb01.cbl:129`<br>`lgucvs01.cbl:72` | Arayüz (COMMAREA) kuralı |
| 03-18 | 03-01 ile aynı. LGUCDB01'e ya da LGUCVS01'e yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. İki LINK'te de `RESP`/`NOHANDLE` yok, programlarda `HANDLE CONDITION` da yok. Sonuç CICS'in varsayılan davranışına bağlı ve kodda yazmıyor. | `lgucus01.cbl:128-131`<br>`lgucdb01.cbl:136-139` | Arayüz (COMMAREA) kuralı |
| 03-19 | **LGUCDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok. Görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz, LGUCVS01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK yapılır: 71 baytlık `ERROR-MSG` (tarih, saat, `' LGUCDB01'`, `' NO COMMAREA RECEIVED'` + 20 boşluk + 6 baytlık `EM-SQLRC`). [B2] `EM-SQLRC` = `+00000` (ön koşullu; SQLCA'nın kaynağı `base/` altında yok: Kod gözlemleri 14) [kod] `lgucdb01.cbl:191`. COMMAREA dökümü gönderilmez.<br>LGUCUS01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 3). | `lgucdb01.cbl:117-121`, `:61-66`, `:191`, `:202-207` | Arayüz (COMMAREA) kuralı |
| 03-20 | **LGUCDB01 doğrudan** çağrılır. `EIBCALEN` 18 ile 90 arasında. `CA-REQUEST-ID` herhangi bir değer. `CA-CUSTOMER-NUM = 0000000001`. [B4] Düzenek UPDATE'te 0 ve 100 dışında bir `SQLCODE` üretir. VSAM READ ve REWRITE `NORMAL` döner. | `CA-RETURN-CODE = 90` | **Reddedilmez:** LGUCDB01'de uzunluk kontrolü yok.<br>Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: 03-04 ile aynı.<br>(2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır.<br>[B1] VSAM kaydının `EIBCALEN` ötesindeki baytları boşluk olur (ön koşullu) [kod] `lgucvs01.cbl:85-89`. | `lgucdb01.cbl:124`, `:171-179`, `:207-213`, `:136-139` | Arayüz (COMMAREA) kuralı |
| 03-21 | **LGUCDB01 doğrudan** çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID` `'01UCUS'` dışında bir değer (örn. boşluk). `CA-NUM-POLICIES` herhangi bir değer N. Diğer girdiler ve ön koşullar 03-01 ile aynı. | `CA-RETURN-CODE = 00` | LGUCDB01 istek kimliğine bakmaz. Güncelleme 03-01'deki gibi yapılır (Kod gözlemleri 6; B1'e bağlı VSAM kuyruğu, yani kaydın son 8 baytı, hariç).<br>`CA-NUM-POLICIES` **değişmez** (N kalır): sıfırlama LGUCUS01'de yapılır. N, VSAM kaydına da bu haliyle girer. | `lgucdb01.cbl:124-134`, `:136-139`<br>(karşılaştırma: `lgucus01.cbl:106`, `:110`) | Arayüz (COMMAREA) kuralı |
| 03-22 | **LGUCVS01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | **belirsiz** | **belirsiz**. LGUCVS01'de `EIBCALEN` kontrolü yok. READ, var olmayan COMMAREA'daki `CA-Customer-Num`'u anahtar olarak kullanır ve READ uzunluğu 0 olur. Sonuç kodda tanımlı değil. | `lgucvs01.cbl:67-76` | Arayüz (COMMAREA) kuralı |
| 03-23 | **LGUCVS01 doğrudan** çağrılır. `EIBCALEN` 18 ile 90 arasında (anahtar `CA-CUSTOMER-NUM` 9-18. baytlarda, içeride kalır). `CA-CUSTOMER-NUM = 0000000000`: bu anahtar VSAM'da yok [kod] `ksdscust.txt:1-10`. [B4] Düzenek, kaydı olmayan anahtarla yapılan READ'de `NORMAL` dışında bir RESP döndürür (03-02 gibi). | Dönüş kodu yok. `CA-RETURN-CODE`'a `81` yazılır, ardından görev `ABEND` kodu `LGV1` ile (NODUMP) sonlanır (03-02 ile aynı belirsizlik). | Yan etki: `LGSTSQ`'ya iki LINK yapılır.<br>(1) `ERROR-MSG`: `' LGUCVS01'`, `' CNUM='` + `CA-CUSTOMER-NUM`, `' Re-write  KSDSCUST '`, RESP, RESP2.<br>(2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`. Bu dala yalnızca bu yolla ulaşılır.<br>Not: READ uzunluğu `EIBCALEN`'dir ve 225 baytlık kayıttan (`adef121.jcl:25`) küçüktür. Anahtar VSAM'da olmadığı için bu soru sonucu değiştirmez: READ her durumda başarısız olur. | `lgucvs01.cbl:67-83`, `:112-127` | Arayüz (COMMAREA) kuralı |

## 03-01 girdi ve beklenen değerler

Müşteri 1'in kaydı, müşteri 2'nin değerleriyle güncellenir. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Girdi | Girdinin kaynağı (`db2cre.jcl`) | Dönüşte beklenen | Alanı kullanan COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` X(6) | `01UCUS` | — (değer koddan: `lgucus01.cbl:110`) | Değişmez | `lgucus01.cbl:110` |
| `CA-RETURN-CODE` 9(2) | Herhangi bir değer | — | `00` | `lgucus01.cbl:105`, `lgucdb01.cbl:124` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000001` | `:437` | Değişmez | `lgucdb01.cbl:129`, `:168`, `lgucvs01.cbl:72` |
| `CA-FIRST-NAME` X(10) | `Scott` + ␣×5 | `:472` (sütun `CHAR(10)`: `:113`) | Değişmez | `lgucdb01.cbl:158` |
| `CA-LAST-NAME` X(20) | `Tracey` + ␣×14 | `:473` (sütun `CHAR(20)`: `:114`) | Değişmez | `lgucdb01.cbl:159` |
| `CA-DOB` X(10) | `1965-09-30` | `:474` (sütun `DATE`: `:115`) | Değişmez | `lgucdb01.cbl:160` |
| `CA-HOUSE-NAME` X(20) | `Tracey Island` + ␣×7 | `:475` (sütun `CHAR(20)`: `:116`) | Değişmez | `lgucdb01.cbl:161` |
| `CA-HOUSE-NUM` X(4) | `1` + ␣×3 | `:476` (sütun `CHAR(4)`: `:117`) | Değişmez | `lgucdb01.cbl:162` |
| `CA-POSTCODE` X(8) | `TB14TV` + ␣×2 | `:477` (sütun `CHAR(8)`: `:118`) | Değişmez | `lgucdb01.cbl:163` |
| `CA-NUM-POLICIES` 9(3) | Herhangi bir değer | — | `000` | `lgucus01.cbl:106` |
| `CA-PHONE-MOBILE` X(20) | ␣×20 (JCL'de `' '`) | `:479` (sütun `phonemobile`, INSERT listesinde 9. sırada: `:468`; `CHAR(20)`: `:120`) | Değişmez | `lgucdb01.cbl:164` |
| `CA-PHONE-HOME` X(20) | `001 911911` + ␣×10 | `:478` (sütun `phonehome`, INSERT listesinde 8. sırada: `:467`; `CHAR(20)`: `:119`) | Değişmez | `lgucdb01.cbl:165` |
| `CA-EMAIL-ADDRESS` X(100) | `REFROOM@TBHOLDINGS.COM` + ␣×78 | `:480` (sütun `Char(100)`: `:121`) | Değişmez | `lgucdb01.cbl:166` |
| `CA-POLICY-DATA` X(32267) | Herhangi bir değer | — | Değişmez | Kodda okunmuyor, yazılmıyor |

Dikkat: `db2cre.jcl`'deki INSERT'te ev telefonu cep telefonundan **önce** geliyor (`:467-468`),
COMMAREA'da ise cep telefonu önce (`lgcmarea.cpy:23-24`). UPDATE değerleri sıraya göre değil, sütun adına göre
eşleştirir (`lgucdb01.cbl:164-165`).

03-01'den sonra veritabanında ve VSAM'da beklenen durum:

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `CUSTOMER` | Müşteri 1'in satırı: `customerNumber` = 1 (değişmez), `firstName` = `Scott`, `lastName` = `Tracey`, `dateOfBirth` = 1965-09-30, `houseName` = `Tracey Island`, `houseNumber` = `1`, `postcode` = `TB14TV`, `phonemobile` = boşluk, `phonehome` = `001 911911`, `emailaddress` = `REFROOM@TBHOLDINGS.COM`. Önceki değerler (`db2cre.jcl:438-446`) kalmaz. Müşteri 2'nin satırı değişmez. | `lgucdb01.cbl:155-169` |
| `KSDSCUST` | Anahtarı `0000000001` olan kayıt 225 bayt ile yeniden yazılır: COMMAREA'nın 9-233. baytları. Sırasıyla `0000000001`, ad … posta kodu, `000`, cep telefonu, ev telefonu, e-posta. Son 8 bayt [B1] boşluk (ön koşullu; Kod gözlemleri 12). Kaydın başlangıç hali (`ksdscust.txt:1`) tamamen ezilir. | `lgucvs01.cbl:85-89` |
| `CUSTOMER_SECURE` | Değişmez. Güncelleme zincirinde bu tabloya giden SQL yok; LGUCDB01'deki tek SQL `CUSTOMER` UPDATE'i. | `lgucdb01.cbl:155-169` |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| UU-01 | `lgucus01.cbl:99-103` `IF EIBCALEN = 0` → ABEND `LGCA` | 03-12 | |
| UU-02 | `lgucus01.cbl:105-106` `'00'` → `CA-RETURN-CODE`, `CA-NUM-POLICIES` | 03-01, 03-09, 03-13 | |
| UU-03 | Uzunluk kontrolü yok (`lgucus01.cbl:56-58`, `:62-64` kullanılmıyor) | 03-13, 03-14, 03-15a, 03-15b | Kısa COMMAREA reddedilmez |
| UU-04 | `lgucus01.cbl:110-113` `IF CA-REQUEST-ID NOT = '01UCUS'` → `99` | 03-09 (doğru dal), 03-01 (yanlış dal) | |
| UU-05 | `lgucus01.cbl:116`, `:128-131` LINK LGUCDB01 `LENGTH(32500)` | 03-01, 03-18 | |
| UU-06 | `lgucus01.cbl:119-120` RETURN, dönüş kodu kontrol edilmez | 03-01, 03-03, 03-04 | |
| UU-07 | `lgucus01.cbl:157-171` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 03-12 | Dış `IF` yanlış. İç dallar (`lgucus01.cbl:158-170`) ölü kod, hiçbir girdiyle ulaşılamaz (Kod gözlemleri 16) |
| UD-01 | `lgucdb01.cbl:117-121` `IF EIBCALEN = 0` → ABEND `LGCA` | 03-19 | Yalnızca doğrudan çağrı |
| UD-02 | `lgucdb01.cbl:124` `'00'` → `CA-RETURN-CODE` | 03-01, 03-21 | |
| UD-03 | Uzunluk kontrolü yok (`LGPOLICY` dahil ama kullanılmıyor; `WS-RETRY` kullanılmıyor) | 03-14, 03-15a, 03-15b, 03-20 | |
| UD-04 | `lgucdb01.cbl:129` `CA-CUSTOMER-NUM` → `DB2-CUSTOMERNUM-INT` | 03-01, 03-16, 03-17 | |
| UD-05 | `lgucdb01.cbl:131` `CA-CUSTOMER-NUM` → `EM-CUSNUM` | 03-04, 03-20 | |
| UD-06 | `lgucdb01.cbl:154-169` UPDATE, 9 sütunun hepsi | 03-01, 03-10 | |
| UD-07 | `lgucdb01.cbl:171` `IF SQLCODE NOT EQUAL 0` (yanlış dal → `00` kalır) | 03-01 | |
| UD-08 | `lgucdb01.cbl:173-174` `IF SQLCODE = 100` → `01` | 03-02, 03-03 | |
| UD-09 | `lgucdb01.cbl:175-178` `ELSE` → `90` + hata mesajı, RETURN yok | 03-04, 03-05, 03-06, 03-20 | |
| UD-10 | `lgucdb01.cbl:136-139` LINK LGUCVS01 `LENGTH(225)`, her durumda | 03-01, 03-02, 03-03, 03-04, 03-18 | |
| UD-11 | `lgucdb01.cbl:142-143` RETURN | 03-01 | |
| UD-12 | `lgucdb01.cbl:207-221` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 03-04 (`ELSE`), 03-20 (`< 91`), 03-19 (dış `IF` yanlış) | |
| UV-01 | `lgucvs01.cbl:67` EIBCALEN kontrolü yok, READ uzunluğu = EIBCALEN | 03-22, 03-23 | |
| UV-02 | `lgucvs01.cbl:69-76` `READ … UPDATE` | 03-01, 03-02 | |
| UV-03 | `lgucvs01.cbl:77-83` RESP ≠ NORMAL (READ) → `81` + ABEND `LGV1` | 03-02, 03-06, 03-07, 03-23 | |
| UV-04 | `lgucvs01.cbl:85-89` `REWRITE` | 03-01, 03-08 | |
| UV-05 | `lgucvs01.cbl:90-96` RESP ≠ NORMAL (REWRITE) → `82` + ABEND `LGV2` | 03-08 | |
| UV-06 | `lgucvs01.cbl:100-102` başarı → GOBACK, dönüş kodu değişmez | 03-01, 03-03, 03-04 | |
| UV-07 | `lgucvs01.cbl:121-135` `WRITE-ERROR-MESSAGE` EIBCALEN dalları | 03-02 (`ELSE`), 03-23 (`< 91`) | |
| SQ-01 | `lgstsq.cbl:68-71` `IF WS-INVOKEPROG NOT = SPACES` (LINK yolu) | 03-02, 03-04, 03-12, 03-19 | |
| SQ-02 | `lgstsq.cbl:72-80` `ELSE` (terminalden RECEIVE) | 03-04 (dal alınmaz) | **Kapsam dışı: bu zincirden ulaşılamaz.** Üç program da LGSTSQ'yu LINK ile çağırır. |
| SQ-03 | `lgstsq.cbl:82-88` `IF` mesaj `'Q='` ile başlıyor | 03-04 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** Mesajlar tarih ya da `'COMMAREA='` ile başlar. |
| SQ-04 | `lgstsq.cbl:94-99` WRITEQ TD `CSMT`, RESP kontrol edilmez | 03-04 | |
| SQ-05 | `lgstsq.cbl:105-111` WRITEQ TS `GENAERRS`, RESP kontrol edilmez | 03-04 | |
| SQ-06 | `lgstsq.cbl:113-119` `IF WS-FLAG = 'R'` → SEND TEXT | 03-04 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz.** `'R'` yalnızca SQ-02 yolunda atanır. |
| N-01 | `CA-REQUEST-ID` yalnızca LGUCUS01'de kontrol edilir | 03-09, 03-21 | Kod gözlemleri 6 |
| N-02 | Alan doğrulaması yok, UPDATE 9 sütunu da yazar | 03-10, 03-11 | Kod gözlemleri 7 |
| N-03 | `CA-CUSTOMER-NUM` için sayısallık ve aralık kontrolü yok | 03-16, 03-17 | Kod gözlemleri 8 |
| N-04 | LINK'lerde `RESP` yok | 03-18 | Kod gözlemleri 5 |
| N-05 | `SYNCPOINT` / `ROLLBACK` yok | 03-07, 03-08 | Kod gözlemleri 9 |
| N-06 | `-913` için ayrı dal yok (`90`) | 03-05 | Kod gözlemleri 10 |
| N-07 | Db2 ile VSAM tutarsız kalabilir | 03-03, 03-04 | Kod gözlemleri 4 |

## Ön koşullu satırlar

2026-10-05 A/B güncellemesinde belirsizden ön koşullu tahmine çevrilen satırlar. Ön koşul kodları dosyanın başında
("Kaynak etiketleri ve ön koşul kodları").

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 03-01 | B1 | `KSDSCUST` kaydının son 8 baytı boşluk | Bu 8 bayt belirsiz: LINK'in COMMAREA'yı nasıl geçirdiğine bağlı (`lgucdb01.cbl:138`, `lgucvs01.cbl:85-87`) |
| 03-02 | B4 | Girdi düzenekle kurulur; tahmin (`81` + ABEND `LGV1`) değişmez | Girdi kurulamaz |
| 03-03 | B4 | Girdi düzenekle kurulur; tahmin (`01`, VSAM yine de yazılır) değişmez | Girdi kurulamaz: başlangıç verisinde böyle bir durum yok |
| 03-04 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | Girdi kurulamaz |
| 03-05 | B4 | Girdi düzenekle kurulur; tahmin (`90`) değişmez | Girdi kurulamaz |
| 03-06 | B4 | Girdi düzenekle kurulur; tahmin (`81` + ABEND `LGV1`) değişmez | B4 sağlanmazsa: girdi kurulamaz |
| 03-07 | B4 | Girdi düzenekle kurulur (VSAM'dan müşteri 1'in kaydı silinir) | Girdi kurulamaz: başlangıç verisinde müşteri 1'in VSAM kaydı var |
| 03-08 | B4 | Girdi düzenekle kurulur | Girdi kurulamaz |
| 03-13 | B1 | Tamponda 7-8. baytlar `99`, 91-93. baytlar `000`; LGUCDB01 çağrılmaz | Dönüş kodu ve veri belirsiz |
| 03-15a | B1 | `00`; sütunların `EIBCALEN` ötesindeki kısmı Db2'de ve VSAM'da boşluk | Sonuç belirsiz (`00`, `90` ya da ortama göre abend); veri belirsiz |
| 03-16 | B3, B4 | Db2'de müşteri 1 güncellenir; VSAM READ başarısız olur → `81` + ABEND `LGV1`; Db2 güncellemesi geri alınır ([belge]) | Dönüş kodu ve veri belirsiz |
| 03-19 | B2 | `EM-SQLRC = +00000` | `EM-SQLRC` belirsiz |
| 03-20 | B4, B1 | Girdi düzenekle kurulur; VSAM kaydının `EIBCALEN` ötesi boşluk | B4 yoksa girdi kurulamaz; B1 yoksa bu baytlar belirsiz |
| 03-23 | B4 | Girdi düzenekle kurulur; tahmin (`81` + ABEND `LGV1`, `< 91` dalı) değişmez | Girdi kurulamaz |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 03-02 | Dönüş kodu (çağıranın `81`'i görmesi) | `81` yazıldıktan hemen sonra `ABEND LGV1` geliyor (`lgucvs01.cbl:79-81`) ve zincirde abend yakalama yok. |
| 03-04 | Beklenen veri (Db2 satırı) | Hata durumunda satırın ne halde kalacağı kodda görünmüyor; kod yalnızca SQLCODE'a bakıyor. |
| 03-06 | Dönüş kodu (çağıranın `81`'i görmesi) | 03-02 ile aynı neden. |
| 03-07 | Dönüş kodu (çağıranın `81`'i görmesi) | 03-02 ile aynı neden. |
| 03-08 | Dönüş kodu (çağıranın `82`'yi görmesi) | 03-02 ile aynı neden (`lgucvs01.cbl:92-94`). |
| 03-11 | Dönüş kodu, beklenen veri | Boşluktan oluşan bir değerin `DATE` sütununa yazılıp yazılamayacağı Db2'ye bağlı. COBOL'da tarih doğrulaması yok. |
| 03-14 | Dönüş kodu, beklenen veri | Müşteri numarası ve bütün müşteri alanları COMMAREA dışında. Hangi satırın hangi değerlerle güncelleneceği ortama bağlı. |
| 03-15b | Dönüş kodu (`00`, `90` ya da abend); beklenen veri (`EIBCALEN` ötesindeki alanlar) | Dışarıdaki baytlar UPDATE'e ve REWRITE'a gidiyor; doğum tarihi de bunların arasında. Bunların içeriği ve UPDATE'i bozup bozmayacağı ortama ve Db2'ye bağlı. Kod UPDATE sonrası yalnızca `0` / `100` / diğer ayrımı yapıyor (`lgucdb01.cbl:171-179`), müşteri satırı var olduğu için kod yollarına göre sonuç `00` ya da `90`; ama COMMAREA dışından okuma ortama göre abend de üretebilir. |
| 03-16 | Dönüş kodu (çağıranın `81`'i görmesi) | 03-02 ile aynı neden. |
| 03-17 | Dönüş kodu, beklenen veri | Sayısal olmayan veri sayısal alana taşınıyor ve hiçbir doğrulama yok. Sonuç tanımsız. |
| 03-18 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı ve kodda tanımlı değil. |
| 03-22 | Dönüş kodu, beklenen veri | LGUCVS01'de `EIBCALEN` kontrolü yok. COMMAREA olmadan `CA-Customer-Num`'a erişimin sonucu kodda tanımlı değil. |
| 03-23 | Dönüş kodu (çağıranın `81`'i görmesi) | 03-02 ile aynı neden. |
