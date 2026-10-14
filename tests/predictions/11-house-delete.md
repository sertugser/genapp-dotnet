# 11 — Konut poliçesi sil (Delete House Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Konut poliçesi sil: müşteri ve poliçe numarasıyla bulunan `POLICY` satırı Db2'den silinir (bağlı `HOUSE` satırı yabancı anahtar nedeniyle birlikte gider) ve VSAM `KSDSPOLY` kaydı silinir. Poliçe türü COMMAREA'daki istek kodundan (`01DHOU`) anlaşılır. |
| Programlar (çağrı zinciri) | `LGDPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGDPDB01` (Db2 `DELETE`) → `LINK` → `LGDPVS01` (VSAM `DELETE`). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. Silmede türe özgü dal yoktur: dört istek kodu tek bir `IF`'te birlikte sayılır. Yalnızca `01DHOU` ile çalışan yol tahmin edildi; diğer üç kod "başka tür, dal alınmaz" diye işaretlendi. |
| Okunan COBOL dosyaları | `base/src/lgdpol01.cbl` (1-186), `base/src/lgdpdb01.cbl` (1-245), `base/src/lgdpvs01.cbl` (1-132), `base/src/lgstsq.cbl` (1-126), `base/src/lgcmarea.cpy`. Karşılaştırma için yalnızca `lgipdb01.cbl:275` (11-03). |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY`, `HOUSE` ve `MOTOR` tanımları (`:161-185`, `:240-256`, `:275-292`), poliçe 6, 7 ve 1 (`:861-882`, `:885-906`, `:933-954`), `HOUSE` 6 ve 7 (`:1042-1074`), `MOTOR` 1 (`:1093-1114`). `base/data/ksdspoly.txt` (`:1-10`): `KSDSPOLY` başlangıç verisi. `base/cntl/cobol.jcl` (`:44-50`): derleme seçenekleri (B3). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `docs/api-design.md` (`:55`, `:98`, `:133`) ve `docs/adr/0004-legacy-test-harness.md` (`:44-52`, `:56-60`, `:96-114`, `:135-138`): yalnızca Kod gözlemleri 6, 13 ve 16, B8, C grubu, [CICS bilgisi] ve Açık sorular için; tahminler koddan çıkarıldı, **kodla doğrulanmadı**. |
| Hazırlayan | Sercan (taslak: Claude Code) |
| Tarih | 2026-10-14 |
| Kaynak commit (`git rev-parse HEAD`) | `4a1893e06b85c6ffd2c015c90343d54d62b94b58` |

## Kaynak etiketleri ve ön koşul kodları

Bu dosyadaki tahminler kaynak türüyle etiketlidir:

- **[kod]**: COBOL kaynağı, copybook, JCL ya da bunların yüklediği veri dosyası (dosya + satır).
- **[Db2 bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen Db2 davranışı. Bu dosyada iki kullanımı: satır silmeyen bir `DELETE`
  `SQLCODE = 100` döner (11-04, 11-05, 11-07; 09'daki SELECT ile aynı varsayım); `ON DELETE CASCADE` tanımlı yabancı anahtarlar
  (`db2cre.jcl:249-250`, `:287-288`) `POLICY` satırı silinince bağlı satırları da siler (11-01, 11-02, 11-03, 11-06, 11-09, 11-11,
  11-16, 11-18).
- **[belge, kodla doğrulanmadı]**: `docs/` altındaki açıklama metinleri. Hiçbir tahmin belgeye dayanmaz.
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

Ön koşul kodları test düzeneğinin vereceği kararlardır (kodlar `08-house-add.md`, `09-house-inquire.md` ve `10-house-update.md`
ile aynıdır). Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul sağlanmazsa tahmin geçersizdir ve "Ön koşullu
satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te, en dıştaki çağıranın 32500 baytlık tamponunun adresiyle geçirir. `EIBCALEN` yalnızca bildirilen uzunluktur. Tamponun `EIBCALEN` ötesi boşlukla doldurulur. COMMAREA dışına yapılan yazmalar bu tampona düşer ve çağıran onları görür. |
| B2 | SQLCA `SQLCODE = 0` ile başlar. |
| B3 | Derleyici sayısal taşımalarda PICTURE'ın hane sayısına göre keser (Enterprise COBOL'da `TRUNC(STD)`, GnuCOBOL'da kesme açık). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. |
| B4 | Düzenek, satırda yazılan hata kodunu (SQLCODE / RESP) ya da veri durumunu (bir kaydın var ya da yok olması) kendisi kurar. **Aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir; satırda yalnızca düzeneğin değiştirdiği değer yazılır. Kaydı olmayan bir anahtarla yapılan VSAM DELETE'te düzenek `NORMAL` dışında bir RESP döndürür. |
| B6 | Db2'de `POLICY` tablosuna `DEFAULT` ile daha önce hiç satır eklenmemiştir; ilk kimlik değeri `START WITH` değeridir (1000001: [kod] `db2cre.jcl:162-163`). Bu dosyada yalnızca 11-18'de, 08-01'in ürettiği numara için. |
| B8 | Düzenekte `EXEC CICS ABEND` bir mesaj ve `GOBACK` ile değiştirilir: abend eden program çağırana döner ve COMMAREA'ya en son yazılan dönüş kodu görünür. [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:44-46`. [CICS bilgisi] Gerçek CICS'te ABEND görevi sonlandırır; orada çağıranın kodu görmesi belirsizdir (C). Bu dosyada yalnızca `LGCA` satırlarında (11-13, 11-19): COMMAREA olmadığı için dönüş kodu görünmez, düzenekte görülen ABEND mesajı ve `GOBACK`'tir. |

(B5 ve B7 bu dosyada kullanılmıyor.) **Sıra** ön koşulu: satırda adı geçen test önce çalıştırılmış ve beklenen sonucu
vermiştir; arada aynı poliçeye dokunan başka bir test yoktur. Sıra bağımlılıkları ve silme testlerinin örnek veriyi yok etmesi:
"Test sırası ve veri bağımlılıkları" bölümü.

## Kod gözlemleri

1. **COMMAREA bayt haritası:** `08-house-add.md` Kod gözlemleri 1 ile aynı (`lgcmarea.cpy:10-13`, `:34-44`). Üç program da bu
   düzeni kullanır (`lgdpol01.cbl:68-69`, `lgdpdb01.cbl:99-102`, `lgdpvs01.cbl:64-65`).
   - **Okunan baytlar:** yalnızca 1-28. İstek kodu (1-6) `lgdpol01.cbl:117-122` ve `lgdpdb01.cbl:160-163`'te okunur, LGDPVS01 yalnızca
     4. baytı okur (`lgdpvs01.cbl:77`). Müşteri (9-18) ve poliçe numarası (19-28) `lgdpdb01.cbl:149-150` ve `lgdpvs01.cbl:78-79`'da
     okunur. 29. bayttan sonraki hiçbir alan okunmaz.
   - **Yazılan baytlar:** 7-8 `CA-RETURN-CODE` ve 1-6 `CA-REQUEST-ID` (büyük harfe çevrilmiş hali: `lgdpol01.cbl:117`).
2. **Çağrı zinciri ve uzunluk.**
   - LGDPOL01 ve LGDPDB01 yalnızca 28 bayt ister (`lgdpol01.cbl:57`, `:107-110`; `lgdpdb01.cbl:66`, `:143-146`). Konuta özgü bir uzunluk
     yoktur; silme yalnızca 1-28. baytları kullanır.
   - LGDPOL01, LGDPDB01'i `LENGTH(32500)` ile çağırır (`lgdpol01.cbl:141-144`). [CICS bilgisi] LGDPDB01 her zaman `EIBCALEN = 32500` görür; bu yüzden
     LGDPDB01'in `EIBCALEN = 0` (`lgdpdb01.cbl:131-135`) ve 28'lik (`lgdpdb01.cbl:143-146`) kontrollerine zincirden ulaşılamaz.
   - LGDPDB01, LGDPVS01'i `LENGTH(32500)` ile çağırır (`lgdpdb01.cbl:168-171`); [CICS bilgisi] LGDPVS01
     `EIBCALEN = 32500` görür. LGDPVS01'de uzunluk kontrolü yok; `EIBCALEN` yalnızca
     `WS-Commarea-Len`'e kopyalanır (`lgdpvs01.cbl:75`) ve başka hiçbir satırda kullanılmaz.
3. **İstek kodu.**
   - Türe özgü dal yok. Dört kod tek bir `IF`'te birlikte sayılır: `lgdpol01.cbl:119-122` (`'01DHOU'` `:121`) ve `lgdpdb01.cbl:160-163`
     (`'01DHOU'` `:161`). Diğer üç kod da aynı yola gider; başka tür, tahmin yazılmadı.
   - **LGDPOL01 istek kodunu yerinde büyük harfe çevirir:** `MOVE FUNCTION UPPER-CASE(CA-REQUEST-ID) TO CA-REQUEST-ID`
     (`lgdpol01.cbl:117`). COMMAREA'nın 1-6. baytları değişir ve çağıran büyük harfli kodu geri görür (11-03). Bu, 28 bayt kontrolünden
     sonra yapılır (`lgdpol01.cbl:107` → `:117`): kısa COMMAREA'da kod çevrilmez (11-14).
   - Karşılaştırma: sorgulamada çevrilen bir kopyadır (`lgipdb01.cbl:275`); eklemede ve güncellemede çevirme yoktur (08-11, 10-19).
   - Tanınmayan kodda LGDPOL01 `'99'` yazar (`lgdpol01.cbl:123-124`); RETURN `lgdpol01.cbl:133`'te; mesaj yazılmaz, LGDPDB01 çağrılmaz.
   - **LGDPDB01'de büyük harfe çevirme yok.** LGDPDB01'in `'99'` dalına (`lgdpdb01.cbl:164-165`) zincirden ulaşılamaz: LGDPOL01 aynı dört
     kodu çevirdikten sonra süzer ve LGDPDB01 bu değeri değiştirmeden görür. Doğrudan çağrıda `'01dhou'` → `99` (11-21).
   - LGDPVS01 anahtarın ilk baytını `CA-Request-ID(4:1)` ile kurar (`lgdpvs01.cbl:77`): `01DHOU` → `'H'`.
4. **`policyType` okunmaz; başka türden poliçe `01DHOU` ile silinir.** `POLICYTYPE`, `'01'` ve `'70'` üç programda da geçmez
   (büyük/küçük harf duyarsız grep, sonuç boş). `DELETE` yalnızca müşteri ve poliçe numarasına bakar (`lgdpdb01.cbl:190-193`).
   - Poliçe 7 (`policyType = 'C'`, `db2cre.jcl:901`) konut olarak silinir (11-02).
   - Motor poliçesi 1 `01DHOU` ile silinir: `POLICY` 1 ve [Db2 bilgisi] cascade ile `MOTOR` 1 gider (`db2cre.jcl:287-288`). VSAM anahtarı `H` ile
     kurulduğu için kayıt bulunamaz ve `81` döner; `M00000000020000000001` kaydı (`ksdspoly.txt:8`) kalır (11-06).
   - **Not:** Başka türden bir poliçenin konut adresinden silinebilmesi C# tarafında bilinçli sapma adayıdır; karar ekibindir.
5. **DELETE ve cascade** (`lgdpdb01.cbl:186-204`).
   - `EM-SQLREQ = ' DELETE POLICY  '` (`lgdpdb01.cbl:188`). `DELETE FROM POLICY WHERE CUSTOMERNUMBER = … AND POLICYNUMBER = …`
     (`lgdpdb01.cbl:189-194`).
   - [Db2 bilgisi] Bağlı satırlar yabancı anahtarla birlikte silinir: `HOUSE` (`db2cre.jcl:249-250`), `MOTOR` (`:287-288`), `ENDOWMENT` (`:214-216`),
     `COMMERCIAL` (`:335-336`), `CLAIM` (`:371-373`), hepsi `ON DELETE CASCADE`. `lgdpdb01.cbl:183-184`'teki yorum da bunu söyler.
     Örnek veride `CLAIM` satırı yok (`db2cre.jcl`'de `claim` için `INSERT` yok).
   - `POLICY`'nin `CUSTOMER`'a bağlantısı ters yöndedir (`db2cre.jcl:174-176`): müşteri satırı silinmez.
   - **Müşteri eşleşmesi zorunludur.** Yanlış müşteri numarasıyla satır bulunamaz (11-05).
6. **Bulgu: yorum ile kod çelişiyor (`lgdpdb01.cbl:196-197` ↔ `:198-201`).**
   - Yorum: "Treat SQLCODE 0 and SQLCODE 100 (record not found) as successful - end result is record does not exist".
   - Kod: `IF SQLCODE NOT EQUAL 0` → `'90'`, mesaj, RETURN (`lgdpdb01.cbl:198-201`). `100` de bu dala düşer.
   - [Db2 bilgisi] Satır silmeyen `DELETE` `SQLCODE = 100` döner. Bu yüzden **silinecek poliçe yoksa sonuç `01` değil `90`'dır**
     (11-04, 11-05, 11-07).
   - [belge, kodla doğrulanmadı] `docs/api-design.md:133` aynı şeyi söyler ("`90` döner … yorum … der, ama kod bunu yapmaz"); belge kodla
     uyumludur. `docs/adr/0004-legacy-test-harness.md:100-101` bu varsayımı ("silinecek satır yokken 100 dönüp dönmediğini") ölçülmeyen
     bir varsayım olarak anar.
7. **Hata yollarının sırası.**
   - Db2 hatasında RETURN (`lgdpdb01.cbl:201`), LGDPVS01'e yapılan LINK'ten (`lgdpdb01.cbl:168`) önce gelir: VSAM'a gidilmez.
   - LGDPDB01, LGDPVS01'in dönüş koduna bakmadan RETURN yapar (`lgdpdb01.cbl:168-175`).
   - LGDPOL01'in `If CA-RETURN-CODE > 0` → RETURN'ü (`lgdpol01.cbl:127-129`) etkisizdir: kod `00` da olsa akış `lgdpol01.cbl:133`'teki
     RETURN'e varır. LGDPDB01 ve LGDPVS01'in yazdığı kod (`90`, `81`) olduğu gibi çağırana ulaşır.
8. **LGDPVS01'in VSAM silmesi** (`lgdpvs01.cbl:72-97`).
   - Anahtar 21 bayt: `WF-Request-ID` + müşteri + poliçe (`lgdpvs01.cbl:25-29`, `:77-79`). Kayıt alanı yok; yalnızca anahtar tanımlı.
   - `DELETE File('KSDSPOLY') Ridfld(WF-Policy-Key) KeyLength(21) RESP(WS-RESP)` (`lgdpvs01.cbl:81-85`).
   - `RESP ≠ NORMAL` → `EIBRESP2` → `WS-RESP2`, `'81'`, mesaj, RETURN (`lgdpvs01.cbl:86-91`). **ABEND yok**; güncellemede aynı durum
     `LGV3` ile bitiyordu (`10-house-update.md` Kod gözlemleri 11). `81` çağırana ulaşır.
   - Başarı yolunda dönüş koduna yazılmaz (`lgdpvs01.cbl:95-97`); zincirde LGDPDB01'in `00`'ı kalır, doğrudan çağrıda girdideki gibi
     kalır (11-24).
9. **Geri alma yok.** Üç programın PROCEDURE DIVISION'ında (`lgdpol01.cbl:75-186`, `lgdpdb01.cbl:108-245`, `lgdpvs01.cbl:69-132`)
   `SYNCPOINT`, `ROLLBACK`, `HANDLE` ya da `COMMIT` geçmez (grep sonucu boş). Tek ABEND `LGCA`'dır (`lgdpol01.cbl:98`, `lgdpdb01.cbl:134`).
   VSAM silmesi başarısız olursa (`81`) Db2'deki silme kodla geri alınmaz: Db2'de poliçe yok, VSAM'da kayıt var. Görev normal bittiği
   için silmenin kalıcı olması CICS'in görev sonu davranışına bağlıdır (C).
10. **Dönüş kodları ve anlamları.**

    | Kod | Nerede | Anlamı | Çağırana ulaşır mı |
    |---|---|---|---|
    | `00` | `lgdpol01.cbl:102`, `lgdpdb01.cbl:138` | Başlangıç / başarı | Evet |
    | `98` | `lgdpol01.cbl:108` | COMMAREA 28 bayttan kısa | Evet |
    | `98` | `lgdpdb01.cbl:144` | COMMAREA 28 bayttan kısa | Yalnızca LGDPDB01 doğrudan çağrılırsa |
    | `99` | `lgdpol01.cbl:124` | Bilinmeyen istek kodu | Evet |
    | `99` | `lgdpdb01.cbl:165` | Bilinmeyen istek kodu | Yalnızca LGDPDB01 doğrudan çağrılırsa |
    | `90` | `lgdpdb01.cbl:199` | `DELETE` `SQLCODE ≠ 0` (`100` dahil) | Evet |
    | `81` | `lgdpvs01.cbl:88` | VSAM DELETE hatası | Evet (ABEND yok) |
    | ABEND `LGCA` | `lgdpol01.cbl:98`, `lgdpdb01.cbl:134` | COMMAREA yok | — |

11. **Hata mesajı içerikleri.**
    - `ERROR-MSG` uzunlukları: LGDPOL01 **45** bayt (`lgdpol01.cbl:39-44`), LGDPDB01 **87** (`lgdpdb01.cbl:41-53`), LGDPVS01 **102**
      (`lgdpvs01.cbl:36-51`; `' Delete file KSDSPOLY'` 21 bayt). `CA-ERROR-MSG` üçünde de 99 bayt (`lgdpol01.cbl:46-48`,
      `lgdpdb01.cbl:55-57`, `lgdpvs01.cbl:53-55`).
    - LGDPDB01: `EM-CUSNUM` ve `EM-POLNUM` baştan dolar (`lgdpdb01.cbl:152-153`); `EM-SQLREQ = ' DELETE POLICY  '` (`lgdpdb01.cbl:188`);
      `EM-SQLRC` = SQLCODE (`lgdpdb01.cbl:214`).
    - LGDPVS01: `EM-CUSNUM` ve `EM-POLNUM` COMMAREA'dan dolar (`lgdpvs01.cbl:109-110`; güncellemedeki LGUPVS01 `EM-POLNUM`'u doldurmuyordu),
      RESP ve RESP2 (`lgdpvs01.cbl:111-112`).
    - `EIBCALEN = 0` mesajları: LGDPOL01'de `' NO COMMAREA RECEIVED'` 21 baytlık `EM-VARIABLE`'a tam sığar (`lgdpol01.cbl:44`, `:96`).
      LGDPDB01'de 63 baytlık gruba taşınır (`lgdpdb01.cbl:132`), `EM-SQLRC` son 6 baytı ezer (`lgdpdb01.cbl:214`): metin + 36 boşluk + 6 bayt.
    - `WRITE-ERROR-MESSAGE` içindeki `EIBCALEN` dalları:
      - LGDPOL01'de yordam yalnızca `lgdpol01.cbl:97`'den çağrılır; dış `IF` (`lgdpol01.cbl:171`) her zaman yanlış, `lgdpol01.cbl:172-184`
        **ölü kod**.
      - [CICS bilgisi] LGDPDB01'de zincirde hep `ELSE` (`lgdpdb01.cbl:237-242`); `< 91` dalı (`lgdpdb01.cbl:231-236`) doğrudan çağrıda ulaşılabilir (11-22).
      - [CICS bilgisi] LGDPVS01'de zincirde `ELSE` (`lgdpvs01.cbl:124-129`); `< 91` (`lgdpvs01.cbl:118-123`) yalnızca doğrudan çağrıda (11-25).
12. **LGSTSQ kuyruk kaydı.** `08-house-add.md` Kod gözlemleri 13 ile aynı kurallar (`lgstsq.cbl:25-28`, `:48`, `:68-71`, `:82-83`, `:90`,
    `:94-111`). Uzunluklar: 45 → 50, 87 → 92 (eksiksiz); 99 → 104: COMMAREA'nın yalnızca ilk **81** baytı girer, son 9 bayt `WRITE-MSG`'nin
    dışından okunur.
    - **LGDPVS01'in 102 baytlık mesajı → 107.** Mesajın 84-89. baytları RESP, 90-96. baytları `' RESP2='`, 97-102. baytları RESP2
      değeridir. Kayda ilk 90 bayt girer: kayıt `' RESP='` + RESP + `' '` ile biter; **`RESP2` etiketi de değeri de kayda girmez.** Son
      12 bayt `WRITE-MSG`'nin dışından okunur.
13. **WORKING-STORAGE kalıcılığı.** Silmede uzunluk birikimi yok: `WS-CA-HEADER-LEN` sabittir ve hiçbir satırda `ADD` yapılmaz
    (`lgdpol01.cbl:57`, `lgdpdb01.cbl:66`). Tek kalıcılık etkisi LGDPDB01'in `EIBCALEN = 0` dalının `EM-VARIABLE` sabitlerini ezmesidir
    (Kod gözlemleri 11; dal ABEND ile biter: `lgdpdb01.cbl:134`). Tahminler WORKING-STORAGE'ın `VALUE` değerleriyle başladığı duruma göre
    yazıldı. [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:50-52`: düzenekte her durum ayrı bir süreçte çalışır,
    çünkü CICS her görev için WORKING-STORAGE'ı yeniden kurar.
14. **Kullanılmayan alanlar.** LGDPVS01: `WS-Comm-Len`, `WS-STARTCODE`, `WS-SYSID` (`lgdpvs01.cbl:20-22`) ve `WS-Commarea-Len`
    (`lgdpvs01.cbl:23`; yalnızca `:75`'te yazılır). `WS-CALEN` doldurulur ama okunmaz (`lgdpol01.cbl:103`, `lgdpdb01.cbl:139`).
15. **Test verisi.** Bu değerler yalnızca Db2 `db2cre.jcl` ile, `KSDSPOLY` `ksdspoly.txt` ile yüklendiyse ve sonradan değiştirilmediyse
    geçerlidir.
    - Poliçe 6 (müşteri 4; `db2cre.jcl:873-874`; `HOUSE` `:1051-1057`; VSAM `ksdspoly.txt:5`), poliçe 7 (müşteri 6; `db2cre.jcl:897-898`;
      `HOUSE` `:1068-1074`; VSAM `ksdspoly.txt:6`), motor poliçesi 1 (müşteri 2; `db2cre.jcl:945-946`; `MOTOR` `:1105`; VSAM
      `ksdspoly.txt:8`).
    - `H00000000020000000001`, `H00000000040000000000`, `H10000000040000000006` anahtarları `ksdspoly.txt:1-10`'da yok.
    - Silme testleri örnek veriyi yok eder (poliçe 6, 7, 1); 08, 09 ve 10'daki testlerden sonra ya da ayrı bir veri yüklemesiyle
      çalışmalıdır ("Test sırası ve veri bağımlılıkları").
16. **Düzenek notu: veriye bağlı SQLCODE.** Düzenekte Db2 stub'ı yalnızca `SQLCODE`'u doldurur; değeri Db2 değil test senaryosu seçer.
    B4'ün "aksi söylenmedikçe SQLCODE 0" kuralı altında, [Db2 bilgisi] ile yazılan ve veriye bağlı olan SQLCODE'lar düzenekte senaryo
    girdisi olarak ayrıca kurulmalıdır: bu dosyada 11-04, 11-05 ve 11-07 (`DELETE` `100`, satır silinmez). Bu değerin gerçek Db2'yle
    örtüşmesi [Db2 bilgisi] kabulüdür; düzenek bunu ölçmez. Cascade silme de düzenekte ölçülmez: Db2 stub'ının arkasında tablo yoktur.
    [belge, kodla doğrulanmadı] `docs/adr/0004-legacy-test-harness.md:56-58`, `:100-106`. Aynı not: `08-house-add.md` ve
    `09-house-inquire.md` Kod gözlemleri 17, `10-house-update.md` Kod gözlemleri 20.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 11-01 | LGDPOL01 çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01DHOU'`. `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000006` (poliçe 6: `db2cre.jcl:873-874`). 29. bayttan sonrası herhangi bir değer (okunmaz). Db2 yalnızca `db2cre.jcl` ile, `KSDSPOLY` yalnızca `ksdspoly.txt` ile yüklenmiş. [B4] `DELETE` `SQLCODE = 0`, VSAM DELETE `NORMAL`. | `CA-RETURN-CODE = 00` | COMMAREA'da `CA-RETURN-CODE` dışında hiçbir bayt değişmez (`CA-REQUEST-ID` zaten büyük harf).<br>Yan etkiler: `POLICY` 6 silinir; [Db2 bilgisi] `HOUSE` 6 cascade ile gider (`ON DELETE CASCADE`: `db2cre.jcl:249-250`); `KSDSPOLY` kaydı `H00000000040000000006` (`ksdspoly.txt:5`) silinir. Müşteri 4'ün satırı kalır. Hata mesajı yazılmaz. (Alan alan: "11-01 girdi ve beklenen değerler".) | `lgdpol01.cbl:102`, `:107`, `:117-122`, `:126`, `:141-144`, `:127`, `:133`<br>`lgdpdb01.cbl:138`, `:143`, `:149-150`, `:160-163`, `:167`, `:188-198`, `:168-171`, `:175`<br>`lgdpvs01.cbl:77-79`, `:81-86`, `:95-97`<br>`db2cre.jcl:249-250` | İş kuralı |
| 11-02 | 11-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000006`, `CA-POLICY-NUM = 0000000007` (poliçe 7: `db2cre.jcl:897-898`; `policyType = 'C'`: `db2cre.jcl:901`). [B4] 11-01'deki gibi. | `CA-RETURN-CODE = 00` | `policyType` okunmaz (Kod gözlemleri 4). `POLICY` 7 silinir, [Db2 bilgisi] `HOUSE` 7 (`db2cre.jcl:1068-1074`) cascade ile gider, `KSDSPOLY` kaydı `H00000000060000000007` (`ksdspoly.txt:6`) silinir. Hata mesajı yazılmaz. Başka türden poliçe konut adresinden silinir: bilinçli sapma adayı (Açık sorular 3). | `lgdpdb01.cbl:189-194`<br>`lgdpvs01.cbl:77-86`<br>`db2cre.jcl:249-250` | İş kuralı |
| 11-03 | 11-01 ile aynı, ama `CA-REQUEST-ID = '01dhou'` (küçük harf). [B4] 11-01'deki gibi. | `CA-RETURN-CODE = 00` | `lgdpol01.cbl:117` kodu COMMAREA'da yerinde büyük harfe çevirir: dönüşte **`CA-REQUEST-ID = '01DHOU'`**. Geri kalanı 11-01 ile aynı (LGDPVS01 anahtarı `'H'` ile kurar). Sorgulamada aynı girdi `CA-REQUEST-ID`'yi küçük harf bırakır (`lgipdb01.cbl:275`; 09-04). | `lgdpol01.cbl:117-122`<br>`lgdpdb01.cbl:161`<br>`lgdpvs01.cbl:77` | Arayüz (COMMAREA) kuralı |
| 11-04 | 11-01 ile aynı, ama `CA-POLICY-NUM = 0000000000` (poliçe 0 Db2'de yok: JCL 1-10'u verir, `db2cre.jcl:777` … `:993`; kimlik değeri 1000001'den başlar: `db2cre.jcl:162-163`). | `CA-RETURN-CODE = 90` | [Db2 bilgisi] `DELETE` satır silmez ve `SQLCODE = 100` döner. Kod `100`'ü de hata sayar (Kod gözlemleri 6): `'90'`, mesaj, RETURN. LGDPVS01 çağrılmaz. Db2 ve VSAM değişmez. Sonuç `01` değildir.<br>Yan etki: `LGSTSQ`'ya iki LINK.<br>(1) `ERROR-MSG` (87 bayt): tarih, saat, `' LGDPDB01'`, `' CNUM=0000000004'`, `' PNUM=0000000000'`, `' DELETE POLICY  '`, `' SQLCODE=+00100'`.<br>(2) `CA-ERROR-MSG`: `'COMMAREA='` + ilk 90 bayt ([CICS bilgisi] LGDPDB01 `EIBCALEN = 32500` görür: `ELSE` dalı): `01DHOU`, `90`, `0000000004`, `0000000000` ve 29-90. baytların girdideki hali. Kuyruğa yalnızca ilk 81 baytı girer. | `lgdpdb01.cbl:152-153`, `:188-201`, `:212-228`, `:237-242`<br>`lgdpol01.cbl:127-129`<br>`lgstsq.cbl:68-71`, `:90`, `:94-111` | İş kuralı |
| 11-05 | 11-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000009` (müşteri 9 var: `db2cre.jcl:709`; poliçe 6 müşteri 4'ün: `db2cre.jcl:873-874`). | `CA-RETURN-CODE = 90` | `WHERE` müşteri ve poliçe numarasını birlikte ister (`lgdpdb01.cbl:192-193`): [Db2 bilgisi] satır silinmez, `SQLCODE = 100`. 11-04 ile aynı yol; mesajda `' CNUM=0000000009'`, `' PNUM=0000000006'`. Poliçe 6'ya dokunulmaz. | `lgdpdb01.cbl:190-201` | İş kuralı |
| 11-06 | 11-01 ile aynı, ama `CA-CUSTOMER-NUM = 0000000002`, `CA-POLICY-NUM = 0000000001` (motor poliçesi 1: `db2cre.jcl:945-946`, `:949`). İstek kodu `'01DHOU'`. [B4] `H00000000020000000001` anahtarı VSAM'da yok (motor kaydı `M…`: `ksdspoly.txt:8`); VSAM DELETE `NORMAL` dışında bir RESP döner. | `CA-RETURN-CODE = 81` | `DELETE` başarılı: **`POLICY` 1 silinir, [Db2 bilgisi] `MOTOR` 1 cascade ile gider** (`db2cre.jcl:287-288`). LGDPVS01 `H` anahtarlı kaydı bulamaz: `'81'`, mesaj, RETURN (ABEND yok). VSAM'daki `M00000000020000000001` kaydı kalır.<br>Db2 silmesi kodla geri alınmaz; kalıcılığı **belirsiz** (C; Kod gözlemleri 9).<br>Yan etki: LGDPVS01'in iki mesajı. (1) `ERROR-MSG` (102 bayt): tarih, saat, `' LGDPVS01'`, `' PNUM=0000000001'`, `' CNUM=0000000002'`, `' Delete file KSDSPOLY'`, `' RESP='` + RESP, `' RESP2='` + RESP2; kayda RESP2 girmez (Kod gözlemleri 12). (2) `CA-ERROR-MSG`: `'COMMAREA='` + ilk 90 bayt ([CICS bilgisi] LGDPVS01 `EIBCALEN = 32500` görür: `ELSE` dalı); 7-8. baytlarda `81`.<br>**Not:** Başka türden bir poliçe konut adresinden silinir; C# tarafında bilinçli sapma adayı, karar ekibindir (Açık sorular 3). | `lgdpdb01.cbl:189-194`, `:168-175`<br>`lgdpvs01.cbl:77-91`, `:99-116`, `:124-129`<br>`lgdpol01.cbl:127-129`<br>`db2cre.jcl:287-288` | İş kuralı |
| 11-07 | Sıra: 11-01 önce çalışmış ve `00` dönmüş. 11-01'in isteği tekrarlanır. | `CA-RETURN-CODE = 90` | Poliçe 6 artık yok. [Db2 bilgisi] `DELETE` satır silmez, `SQLCODE = 100`. 11-04 ile aynı yol; mesajda `' PNUM=0000000006'`. VSAM'a gidilmez. | `lgdpdb01.cbl:190-201` | İş kuralı |
| 11-08 | 11-01 ile aynı. [B4] `DELETE` `0` ve `100` dışında bir SQLCODE döner. | `CA-RETURN-CODE = 90` | Silme yapılmaz: `POLICY` 6, `HOUSE` 6 ve VSAM kaydı kalır. LGDPVS01 çağrılmaz. Yan etki: 11-04 ile aynı iki mesaj, SQLCODE düzeneğin değeri. | `lgdpdb01.cbl:198-201` | İş kuralı |
| 11-09 | 11-01 ile aynı. `DELETE` başarılı. [B4] VSAM DELETE `NORMAL` dışında bir RESP döner (örn. düzenek kaydı önceden siler). | `CA-RETURN-CODE = 81` | `POLICY` 6 ve ([Db2 bilgisi] cascade ile) `HOUSE` 6 silinmiştir; kod geri alma istemez, kalıcılığı **belirsiz** (C). VSAM kaydı silinemez.<br>Yan etki: LGDPVS01'in iki mesajı: `' PNUM=0000000006'`, `' CNUM=0000000004'`, `' Delete file KSDSPOLY'`, RESP (kayıtta RESP2 yok); `CA-ERROR-MSG`'de 7-8. baytlar `81`. | `lgdpvs01.cbl:81-91`, `:99-116`<br>`lgdpdb01.cbl:168-175`<br>`lgdpol01.cbl:127-129` | İş kuralı |
| 11-10 | 11-01 ile aynı, ama `CA-REQUEST-ID` = ␣×6. | `CA-RETURN-CODE = 99` | `UPPER-CASE` boşlukları değiştirmez. Dört kodun hiçbiri değil: `'99'`. LGDPDB01 çağrılmaz, Db2 ve VSAM değişmez, hata mesajı yazılmaz. COMMAREA'da `CA-RETURN-CODE` dışında hiçbir bayt değişmez. | `lgdpol01.cbl:117-124`, `:133` | Arayüz (COMMAREA) kuralı |
| 11-11 | 11-01 ile aynı, ama `CA-CUSTOMER-NUM = 1000000004` (10 haneli). [B4] `H10000000040000000006` anahtarı VSAM'da yok. | [B3] `CA-RETURN-CODE = 81` (ön koşullu) | [B3] `DB2-CUSTOMERNUM-INT` (`S9(9) COMP`, `lgdpdb01.cbl:82`) değeri 4 olur: müşteri 4'ün poliçe 6'sı silinir ([Db2 bilgisi] cascade ile `HOUSE` 6 da). VSAM anahtarı COMMAREA'dan kurulur (`1000000004`: `lgdpvs01.cbl:79`); kayıt bulunamaz → `81`. `H00000000040000000006` kaydı kalır. Db2 silmesinin kalıcılığı **belirsiz** (C). `CA-CUSTOMER-NUM` `1000000004` kalır. | `lgdpdb01.cbl:82`, `:149`, `:192`<br>`lgdpvs01.cbl:79`, `:81-91`<br>`lgcmarea.cpy:12` | Arayüz (COMMAREA) kuralı |
| 11-12 | 11-01 ile aynı, ama `CA-POLICY-NUM` = ␣×10 (sayısal alanda sayısal olmayan içerik). | **belirsiz** | **belirsiz**. `lgdpdb01.cbl:150` sayısal olmayan DISPLAY içeriği `S9(9) COMP` alana taşır; sonuç derleyiciye bağlı (C). Değer 0 olursa 11-04 (`90`) geçerli olur. | `lgdpdb01.cbl:83`, `:150`<br>`lgcmarea.cpy:35` | Arayüz (COMMAREA) kuralı |
| 11-13 | LGDPOL01 COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgdpol01.cbl:98`'den sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | COMMAREA olmadığı için veri dönmez. LGDPDB01 çağrılmaz.<br>Yan etki: `LGSTSQ`'ya tek LINK: 45 baytlık `ERROR-MSG` (tarih, saat, `' LGDPOL01'`, `' NO COMMAREA RECEIVED'`). Kayıt 50 bayt. COMMAREA dökümü gönderilmez. | `lgdpol01.cbl:39-44`, `:95-99`, `:157-169`, `:171` | Arayüz (COMMAREA) kuralı |
| 11-14 | LGDPOL01 çağrılır. `EIBCALEN` 8 ile 27 arasında (sınır değer: `27`). `CA-REQUEST-ID = '01dhou'` (küçük harf; çevrilmediğini göstermek için). | `CA-RETURN-CODE = 98` | LGDPDB01 çağrılmaz, hata mesajı yazılmaz. 28 bayt kontrolü büyük harfe çevirmeden önce RETURN yaptığı için `CA-REQUEST-ID` `'01dhou'` kalır. | `lgdpol01.cbl:57`, `:102`, `:107-110`, `:117` | Arayüz (COMMAREA) kuralı |
| 11-15 | LGDPOL01 çağrılır. `EIBCALEN` 1 ile 7 arasında. | [B1] Tamponun 7-8. baytları `98` olur (ön koşullu) [kod] `lgdpol01.cbl:108`. Ön koşulsuz kesin olan: `EIBCALEN = 7` ise COMMAREA'nın 7. baytı `9`. | Kesin: LGDPDB01 çağrılmaz, mesaj yazılmaz, istek kodu çevrilmez. | `lgcmarea.cpy:10-11`<br>`lgdpol01.cbl:102`, `:107-110` | Arayüz (COMMAREA) kuralı |
| 11-16 | LGDPOL01 çağrılır. `EIBCALEN = 28` (kabul edilen en küçük uzunluk). İlk 28 bayt 11-01 ile aynı. [B4] 11-01'deki gibi. | [B1] `CA-RETURN-CODE = 00` (ön koşullu) | Kesin: `28 < 28` yanlış, kontrol geçer. Silmenin okuduğu bütün alanlar 1-28. baytlarda.<br>[B1] LGDPOL01 28 baytlık COMMAREA'yı `LENGTH(32500)` ile geçirir; B1 ile bu düzeneğin tamponudur. Sonuç ve yan etkiler 11-01 ile aynı. | `lgdpol01.cbl:107`, `:141-144`<br>`lgdpdb01.cbl:168-171` | Arayüz (COMMAREA) kuralı |
| 11-17 | LGDPOL01 çağrılır. `EIBCALEN = 32500`. LGDPDB01'e ya da LGDPVS01'e yapılan LINK başarısız olur. | **belirsiz** | **belirsiz**. İki LINK'te de `RESP`/`NOHANDLE` yok, `HANDLE CONDITION` yok. Sonuç CICS'in varsayılan davranışına bağlı (C). | `lgdpol01.cbl:141-144`<br>`lgdpdb01.cbl:168-171` | Arayüz (COMMAREA) kuralı |
| 11-18 | Sıra: 08-01 önce çalışmış ve `00` dönmüş. [B6] 08-01'in ürettiği numara P = `0001000001`. LGDPOL01 çağrılır: `EIBCALEN = 32500`, `CA-REQUEST-ID = '01DHOU'`, `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0001000001`. [B4] 11-01'deki gibi. | `CA-RETURN-CODE = 00` | 08-01'in eklediği `POLICY` P silinir, [Db2 bilgisi] `HOUSE` P cascade ile gider; 08-01'in yazdığı `KSDSPOLY` kaydı `H00000000040001000001` silinir. Hata mesajı yazılmaz. Bundan sonra 09-18 `01` döner ("Test sırası ve veri bağımlılıkları"). | `lgdpdb01.cbl:189-198`, `:168-171`<br>`lgdpvs01.cbl:77-86`<br>`lgapvs01.cbl:99-101`, `:135-141` | İş kuralı |
| 11-19 | **LGDPDB01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | Dönüş kodu yok (COMMAREA yok). [B8] Düzenekte: ABEND `LGCA` bir mesaj ve `GOBACK`'tir; program `lgdpdb01.cbl:134`'ten sonra çalışmaz ve sürücüye döner (ön koşullu). [CICS bilgisi] Gerçek CICS'te görev `ABEND` kodu `LGCA` ile (NODUMP) sonlanır. | SQL çalışmaz.<br>Yan etki: tek LINK: 87 baytlık `ERROR-MSG` (tarih, saat, `' LGDPDB01'`, `' NO COMMAREA RECEIVED'` + ␣×36 + 6 baytlık `EM-SQLRC`). [B2] `EM-SQLRC = +00000` (ön koşullu) [kod] `lgdpdb01.cbl:214`. COMMAREA dökümü gönderilmez. | `lgdpdb01.cbl:46-53`, `:131-135`, `:214`, `:225-230` | Arayüz (COMMAREA) kuralı |
| 11-20 | **LGDPDB01 doğrudan** çağrılır. `EIBCALEN` 8 ile 27 arasında (sınır değer: `27`). | `CA-RETURN-CODE = 98` | SQL çalışmaz, LGDPVS01 çağrılmaz, hata mesajı yazılmaz. | `lgdpdb01.cbl:66`, `:138`, `:143-146` | Arayüz (COMMAREA) kuralı |
| 11-21 | **LGDPDB01 doğrudan** çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01dhou'` (küçük harf). Müşteri ve poliçe 11-01 ile aynı. | `CA-RETURN-CODE = 99` | LGDPDB01'de büyük harfe çevirme yok; karşılaştırma büyük/küçük harfe duyarlı. `DELETE` çalışmaz, LGDPVS01 çağrılmaz, mesaj yazılmaz. Poliçe 6 kalır. LGDPOL01 üzerinden bu yola ulaşılamaz (Kod gözlemleri 3). | `lgdpdb01.cbl:160-165`, `:175` | Arayüz (COMMAREA) kuralı |
| 11-22 | **LGDPDB01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (sınır değer: `90`). İlk 28 bayt 11-01 ile aynı. [B4] `DELETE` `0` dışında bir SQLCODE döner. | `CA-RETURN-CODE = 90` | Yan etki: iki LINK. (1) `ERROR-MSG`: 11-04 ile aynı biçim. (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca doğrudan çağrıyla ulaşılır. LGDPVS01 çağrılmaz. | `lgdpdb01.cbl:198-201`, `:230-236` | Arayüz (COMMAREA) kuralı |
| 11-23 | **LGDPVS01 doğrudan** COMMAREA'sız çağrılır (`EIBCALEN = 0`). | **belirsiz** | **belirsiz**. LGDPVS01'de `EIBCALEN` kontrolü yok; `lgdpvs01.cbl:77-79` var olmayan COMMAREA'dan okur. Sonuç kodda tanımlı değil. | `lgdpvs01.cbl:75-85` | Arayüz (COMMAREA) kuralı |
| 11-24 | **LGDPVS01 doğrudan** çağrılır. `EIBCALEN = 32500`. `CA-REQUEST-ID = '01DHOU'`, `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000006` (`H00000000040000000006` var: `ksdspoly.txt:5`). `CA-RETURN-CODE = 55` (seçilmiş değer: yazılmadığını göstermek için; kaynağı yok). [B4] VSAM DELETE `NORMAL`. | `CA-RETURN-CODE = 55` (değişmez) | VSAM kaydı `H00000000040000000006` silinir. Db2'ye dokunulmaz. Hata mesajı yazılmaz. LGDPVS01 başarı yolunda dönüş koduna yazmaz. | `lgdpvs01.cbl:77-86`, `:95-97` | Arayüz (COMMAREA) kuralı |
| 11-25 | **LGDPVS01 doğrudan** çağrılır. `EIBCALEN` 28 ile 90 arasında (anahtar alanları 1-28. baytlarda). `CA-REQUEST-ID = '01DHOU'`, `CA-CUSTOMER-NUM = 0000000004`, `CA-POLICY-NUM = 0000000000`. [B4] `H00000000040000000000` anahtarı VSAM'da yok; DELETE `NORMAL` dışında bir RESP döner. | `CA-RETURN-CODE = 81` | ABEND yok, RETURN yapılır.<br>Yan etki: iki LINK. (1) `ERROR-MSG` (102 bayt): `' PNUM=0000000000'`, `' CNUM=0000000004'`, `' Delete file KSDSPOLY'`, RESP, RESP2 (kayıtta RESP2 yok). (2) `EIBCALEN < 91` dalı: `'COMMAREA='` + `DFHCOMMAREA(1:EIBCALEN)`, `CA-DATA`'nın kalanı boşluk. Bu dala yalnızca bu yolla ulaşılır. | `lgdpvs01.cbl:77-91`, `:109-123` | Arayüz (COMMAREA) kuralı |

## 11-01 girdi ve beklenen değerler

COMMAREA'nın alan alan girdisi ve dönüşte beklenen hali. Silme yalnızca ilk 28 baytı kullanır.

| COMMAREA alanı | Girdi | Girdinin kaynağı (`db2cre.jcl`) | Dönüşte beklenen | Alanı kullanan / dolduran COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` X(6) | `01DHOU` | — (istek kodu: `lgdpol01.cbl:121`) | `01DHOU` (büyük harfe çevrilir; zaten büyük harf) | `lgdpol01.cbl:117-122`, `lgdpdb01.cbl:160-163`, `lgdpvs01.cbl:77` |
| `CA-RETURN-CODE` 9(2) | Herhangi bir değer | — | `00` | `lgdpol01.cbl:102`, `lgdpdb01.cbl:138` |
| `CA-CUSTOMER-NUM` 9(10) | `0000000004` | `:874` | Değişmez | `lgdpdb01.cbl:149`, `:192`; `lgdpvs01.cbl:79` |
| `CA-POLICY-NUM` 9(10) | `0000000006` | `:873` | Değişmez | `lgdpdb01.cbl:150`, `:193`; `lgdpvs01.cbl:78` |
| 29-32500. baytlar | Herhangi bir değer | — | Değişmez | Kodda okunmuyor, yazılmıyor |

## 11-01'den sonra Db2'de ve VSAM'da beklenen durum

| Hedef | Beklenen | COBOL satırı |
|---|---|---|
| `POLICY` (poliçe 6, `db2cre.jcl:861-882`) | Satır yok. | `lgdpdb01.cbl:189-194` |
| `HOUSE` (poliçe 6, `db2cre.jcl:1042-1057`) | Satır yok ([Db2 bilgisi] cascade: `db2cre.jcl:249-250`). | `lgdpdb01.cbl:189-194` |
| `KSDSPOLY` `H00000000040000000006` (`ksdspoly.txt:5`) | Kayıt yok. | `lgdpvs01.cbl:77-85` |
| `CUSTOMER` (müşteri 4) ve diğer poliçeler | Değişmez. | — |
| `GENAERRS` / `CSMT` | Kayıt yazılmaz. | — |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları. "Başka tür" maddelerine tahmin yazılmadı; bu
maddeler, dalın alınmadığı bir konut testine bağlandı.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| DP-01 | `lgdpol01.cbl:95-99` `IF EIBCALEN = 0` → ABEND `LGCA` | 11-13 | |
| DP-02 | `lgdpol01.cbl:102` `'00'` → `CA-RETURN-CODE` | 11-01, 11-15 | |
| DP-03 | `lgdpol01.cbl:57`, `:107-110` `IF EIBCALEN < 28` → `98` | 11-14 (reddedilir), 11-16 (kabul), 11-15 (alan dışı) | |
| DP-04 | `lgdpol01.cbl:117` `UPPER-CASE`, yerinde | 11-03, 11-10, 11-14 (çalışmaz) | Kod gözlemleri 3 |
| DP-05a | `lgdpol01.cbl:121` `'01DHOU'` | 11-01 | |
| DP-05b | `lgdpol01.cbl:119`, `:120`, `:122` `'01DEND'`, `'01DMOT'`, `'01DCOM'` | 11-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DP-05c | `lgdpol01.cbl:123-124` dördü de değil → `99` | 11-10 | |
| DP-06 | `lgdpol01.cbl:126`, `:139-146` LINK LGDPDB01 `LENGTH(32500)`, `RESP` yok | 11-01, 11-17 | |
| DP-07 | `lgdpol01.cbl:127-129` `If CA-RETURN-CODE > 0` → RETURN | 11-04 (doğru), 11-01 (yanlış) | Etkisiz: iki yol da RETURN'e varır (Kod gözlemleri 7) |
| DP-08 | `lgdpol01.cbl:133` RETURN | 11-01, 11-10 | |
| DP-09 | `lgdpol01.cbl:154-169` `WRITE-ERROR-MESSAGE`, 45 baytlık `ERROR-MSG` | 11-13 | |
| DP-10 | `lgdpol01.cbl:171-185` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 11-13 | Dış `IF` yanlış. İç dallar (`:172-184`) **kapsam dışı: ulaşılamaz**; yordam yalnızca `:97`'den çağrılıyor |
| DD-01 | `lgdpdb01.cbl:125` `INITIALIZE DB2-IN-INTEGERS` | 11-01 | |
| DD-02 | `lgdpdb01.cbl:131-135` `IF EIBCALEN = 0` → ABEND `LGCA` | 11-19 | Yalnızca doğrudan çağrı |
| DD-03 | `lgdpdb01.cbl:138` `'00'` | 11-01 | |
| DD-04 | `lgdpdb01.cbl:143-146` `IF EIBCALEN < 28` → `98` | 11-20 | Zincirde hiç alınmaz (32500) |
| DD-05 | `lgdpdb01.cbl:149-153` numaralar ve `EM-CUSNUM` / `EM-POLNUM` | 11-01, 11-11, 11-12 | |
| DD-06a | `lgdpdb01.cbl:161` `'01DHOU'` | 11-01 | |
| DD-06b | `lgdpdb01.cbl:160`, `:162`, `:163` diğer üç kod | 11-01 (dal alınmaz) | **Başka tür**: tahmin yazılmadı |
| DD-06c | `lgdpdb01.cbl:164-165` dördü de değil → `99` | 11-21 | Zincirden **ulaşılamaz** (LGDPOL01 süzer); yalnızca doğrudan çağrı |
| DD-07 | `lgdpdb01.cbl:188-194` `DELETE FROM POLICY` (müşteri `AND` poliçe), cascade | 11-01, 11-02, 11-05, 11-06, 11-18 | Kod gözlemleri 5 |
| DD-08 | `lgdpdb01.cbl:196-197` yorum: "0 ve 100 başarılı" | 11-04 | Kodla çelişir (Kod gözlemleri 6) |
| DD-09 | `lgdpdb01.cbl:198-202` `SQLCODE ≠ 0` → `90`, mesaj, RETURN | 11-04, 11-05, 11-07, 11-08, 11-22 | |
| DD-10 | `lgdpdb01.cbl:167-171` LINK LGDPVS01 `LENGTH(32500)`, dönüş kodu kontrol edilmez | 11-01, 11-06, 11-09 | |
| DD-11 | `lgdpdb01.cbl:175` RETURN | 11-01 | |
| DD-12 | `lgdpdb01.cbl:212-228` `WRITE-ERROR-MESSAGE`, 87 baytlık `ERROR-MSG` | 11-04, 11-19 | |
| DD-13 | `lgdpdb01.cbl:230-244` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 11-04 (`ELSE`), 11-22 (`< 91`), 11-19 (dış `IF` yanlış) | |
| DV-01 | `lgdpvs01.cbl:75` `EIBCALEN` kopyalanır, kontrol yok | 11-23 | |
| DV-02 | `lgdpvs01.cbl:77-79` anahtar | 11-01, 11-06, 11-11 | |
| DV-03 | `lgdpvs01.cbl:81-85` `DELETE File('KSDSPOLY')` | 11-01, 11-18, 11-24 | |
| DV-04 | `lgdpvs01.cbl:86-91` `RESP ≠ NORMAL` → `81`, mesaj, RETURN (ABEND yok) | 11-06, 11-09, 11-11, 11-25 | |
| DV-05 | `lgdpvs01.cbl:95-97` GOBACK; dönüş koduna yazılmaz | 11-01, 11-24 | |
| DV-06 | `lgdpvs01.cbl:99-116` `WRITE-ERROR-MESSAGE`, 102 baytlık `ERROR-MSG` | 11-06, 11-09, 11-25 | |
| DV-07 | `lgdpvs01.cbl:117-131` `WRITE-ERROR-MESSAGE` `EIBCALEN` dalları | 11-09 (`ELSE`), 11-25 (`< 91`) | Dış `IF` yanlış yalnızca `EIBCALEN = 0` iken; o yol belirsiz (11-23) |
| DV-08 | `lgdpvs01.cbl:20-23` kullanılmayan alanlar | 11-01 | Kod gözlemleri 14 |
| SQ-01 | `lgstsq.cbl:68-71` LINK yolu | 11-04, 11-06, 11-09, 11-13, 11-19, 11-22, 11-25 | |
| SQ-02 | `lgstsq.cbl:72-80` `ELSE` (terminalden RECEIVE) | 11-04 (dal alınmaz) | **Kapsam dışı: bu zincirden ulaşılamaz.** Üç program da LGSTSQ'yu LINK ile çağırır |
| SQ-03 | `lgstsq.cbl:82-88` `'Q='` kontrolü | 11-04 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz** |
| SQ-04 | `lgstsq.cbl:90`, `:94-99` +5, WRITEQ TD `CSMT` | 11-04 | |
| SQ-05 | `lgstsq.cbl:105-111` WRITEQ TS `GENAERRS` | 11-04 | |
| SQ-06 | `lgstsq.cbl:113-119` `IF WS-FLAG = 'R'` → SEND TEXT | 11-04 (yanlış dal) | Doğru dal **kapsam dışı: bu zincirden ulaşılamaz** |
| SQ-07 | `lgstsq.cbl:121-122` RETURN | 11-04 | |
| SQ-08 | Kayıt uzunlukları 45 → 50, 87 → 92, 99 → 104 (81 bayt) | 11-04, 11-13, 11-19 | Kod gözlemleri 12 |
| SQ-09 | LGDPVS01'in 102 baytlık mesajı → 107, RESP2 etiketi ve değeri kayda girmez | 11-06, 11-09, 11-25 | Kod gözlemleri 12 |
| N-01 | `policyType` okunmaz; başka türden poliçe `01DHOU` ile silinir | 11-02, 11-06 | Kod gözlemleri 4; C# için bilinçli sapma adayı |
| N-02 | `01` yok; bulunamayan poliçe `90` | 11-04, 11-05, 11-07 | Kod gözlemleri 6 |
| N-03 | Cascade silme | 11-01, 11-06 | Kod gözlemleri 5 |
| N-04 | Geri alma yok; VSAM hatasında Db2 silinmiş kalır | 11-06, 11-09, 11-11 | Kod gözlemleri 9 |
| N-05 | Yerinde `UPPER-CASE` (LGDPOL01); LGDPDB01 duyarlı | 11-03, 11-21 | Kod gözlemleri 3 |
| N-06 | Uzunluk kontrolü yalnızca 28 bayt | 11-14, 11-16 | Kod gözlemleri 2 |
| N-07 | Müşteri eşleşmesi zorunlu | 11-05 | Kod gözlemleri 5 |
| N-08 | LINK'lerde `RESP` yok | 11-17 | |
| N-09 | Yalnızca 1-28. baytlar okunur | 11-01, 11-03 | Kod gözlemleri 1 |
| N-10 | WORKING-STORAGE kalıcılığı | 11-19 | Tahminler `VALUE` başlangıcına göre (Kod gözlemleri 13) |

## Ön koşullu satırlar

Ön koşul kodları ve "sıra" ön koşulu dosyanın başında ("Kaynak etiketleri ve ön koşul kodları"). B4 satırlarında son sütun iki durumu ayrı yazar: **düzenekte** (senaryo değeri vermez ya da host değişkenine yazmazsa; B4'ün varsayılanı `0` / `NORMAL`) ve **gerçek Db2/VSAM'de** (sonucu gerçek sistem verirse).

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 11-01 | B4 | `00`; `POLICY` 6 silinir, `HOUSE` 6 cascade ile gider ([Db2 bilgisi]), VSAM kaydı silinir | **Düzenekte:** senaryo `DELETE`'e `0` dışında bir SQLCODE verirse `90` ve iki mesaj (11-04 / 11-08 yolu); VSAM DELETE'e `NORMAL` dışında RESP verirse `81` (11-09 yolu). **Gerçek Db2/VSAM'de:** poliçe 6 ve kaydı örnek veride var (`db2cre.jcl:873-874`, `ksdspoly.txt:5`); sonuç Db2'nin ve VSAM'ın döndürdüğü koda bağlı, kod yolları aynı |
| 11-02 | B4 | `00`; `POLICY` 7 silinir, `HOUSE` 7 cascade ile gider ([Db2 bilgisi]), VSAM kaydı silinir | 11-01 ile aynı (poliçe 7: `db2cre.jcl:897-898`, `ksdspoly.txt:6`) |
| 11-03 | B4 | `00`; `CA-REQUEST-ID` büyük harfe çevrilir, 11-01 ile aynı silme | 11-01 ile aynı; büyük harfe çevirme her yolda kesin (`lgdpol01.cbl:117` LGDPDB01'den önce çalışır) |
| 11-06 | B4 | `81`; `POLICY` 1 ve `MOTOR` 1 silinir, `M…` VSAM kaydı kalır | **Düzenekte:** senaryo VSAM DELETE'e RESP vermezse `NORMAL` döner: `00`. **Gerçek VSAM'da:** `H00000000020000000001` anahtarı yok (`ksdspoly.txt:8`'deki kayıt `M…`); DELETE'in RESP'i CICS'e bağlı (C). Db2 silmesi iki durumda da aynı ([Db2 bilgisi] cascade) |
| 11-07 | Sıra (11-01) | `90` | 11-01 çalışmadıysa poliçe 6 vardır ve 11-01 gibi silinir (`00`) |
| 11-08 | B4 | `90`; silme yok | **Düzenekte:** senaryo SQLCODE vermezse `DELETE` `0` döner, 11-01 yolu (`00`). **Gerçek Db2'de:** girdi kurulamaz; örnek veride `DELETE`'i hataya düşürecek bir veri yolu yok |
| 11-09 | B4 | `81`; Db2 silinmiş, VSAM kaydı kalır | **Düzenekte:** senaryo RESP vermezse 11-01 yolu (`00`). **Gerçek VSAM'da:** girdi kurulamaz (anahtar var: `ksdspoly.txt:5`) |
| 11-11 | B3, B4 | `81`; müşteri 4'ün poliçe 6'sı Db2'den silinir | B3 yoksa kesilmeyen değerin `S9(9) COMP`'a taşınması derleyiciye bağlı: dönüş kodu ve veri belirsiz. B4 yoksa: **düzenekte** VSAM DELETE `NORMAL` döner: `00`; **gerçek VSAM'da** `H10000000040000000006` anahtarı yok (`ksdspoly.txt:1-10`), DELETE'in RESP'i CICS'e bağlı (C) |
| 11-13 | B8 | Düzenekte ABEND `LGCA` mesajı ve `GOBACK`; dönüş kodu yok, LGDPDB01 çağrılmaz, tek LGSTSQ LINK | B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır. Yan etki aynı: LGSTSQ LINK'i ABEND'den önce yapılır (`lgdpol01.cbl:97-98`) |
| 11-15 | B1 | Tamponun 7-8. baytları `98` | Dönüş kodunun nereye düştüğü belirsiz; `EIBCALEN = 7` için 7. baytın `9` olması yine kesin |
| 11-16 | B1, B4 | 11-01 ile aynı | B1 yoksa belirsiz: `LENGTH(32500)` ile 28 baytlık alandan yapılan LINK'in ne okuyacağı ortama bağlı. B4 için 11-01'in son sütunu |
| 11-18 | B4, B6, sıra (08-01) | `00`; P numaralı poliçe Db2'den ve VSAM'dan silinir | B6 yoksa P, 08-01'in döndürdüğü numarayla değiştirilir, tahmin aynen geçerli. 08-01 çalışmadıysa: **gerçek Db2'de** [Db2 bilgisi] `90` (11-04 yolu); **düzenekte** senaryo `100` vermezse `DELETE` `0` döner ve VSAM'da kayıt olmadığı için (B4) `81` (11-06 yolu). B4 için 11-01'in son sütunu |
| 11-19 | B2, B8 | `EM-SQLRC = +00000`; düzenekte ABEND `LGCA` mesajı ve `GOBACK` | B2 yoksa `EM-SQLRC` belirsiz. B8 yoksa (gerçek CICS): [CICS bilgisi] görev `LGCA` ile sonlanır; mesaj ABEND'den önce yazılır (`lgdpdb01.cbl:133-134`) |
| 11-22 | B4 | `90`, `< 91` dalı | **Düzenekte:** senaryo SQLCODE vermezse `DELETE` `0` döner ve LGDPVS01 çağrılır; dönüş kodu VSAM DELETE'in sonucuna bağlı (`00` ya da `81`). **Gerçek Db2'de:** girdi kurulamaz |
| 11-24 | B4 | VSAM DELETE `NORMAL`; `55` değişmez, kayıt silinir | **Düzenekte:** senaryo `NORMAL` dışında RESP verirse `81` ve iki mesaj (`ELSE` dalı; 11-09 biçimi). **Gerçek VSAM'da:** anahtar var (`ksdspoly.txt:5`); sonuç VSAM'ın RESP'ine bağlı, `NORMAL` dışında aynı `81` yolu |
| 11-25 | B4 | `81`, `< 91` dalı, ABEND yok | **Düzenekte:** senaryo RESP vermezse `NORMAL` döner: dönüş kodu girdideki gibi kalır. **Gerçek VSAM'da:** anahtar yok (`ksdspoly.txt:1-10`); DELETE'in RESP'i CICS'e bağlı (C) |

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 11-12 | Dönüş kodu, beklenen veri | Sayısal olmayan DISPLAY içeriğinin `COMP` alana taşınmasının sonucu derleyiciye bağlı (C). |
| 11-17 | Dönüş kodu, beklenen veri | Kodda LINK hatası için işleme yok. Sonuç CICS'in varsayılan davranışı (C). |
| 11-23 | Dönüş kodu, beklenen veri | LGDPVS01'de `EIBCALEN` kontrolü yok; COMMAREA olmadan erişimin sonucu kodda tanımlı değil. |
| 11-06, 11-09, 11-11 | Beklenen veri: Db2 silmesinin kalıcılığı | Kod geri alma istemez ve görev normal biter; silmenin kalıcı olması CICS'in görev sonu davranışına bağlı (C; Kod gözlemleri 9). `81` kesindir. |
| 11-04, 11-05, 11-06, 11-07, 11-08, 11-09, 11-22, 11-25 | Yan etki: `CA-ERROR-MSG` kuyruk kaydının son 9 baytı; LGDPVS01 mesajında son 12 bayt | Kaydın `WRITE-MSG`'nin dışından okunan baytları WORKING-STORAGE yerleşimine bağlı (Kod gözlemleri 12). Mesajın kendisi kesindir. |

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
   değişkenlerine ne koyacağı açık sorudur ve bu kuralla çelişir (`:135-138`). Bu dosyada B4'te host değişkeni eki yok: `DELETE` veri
   döndürmez. Soru 08, 09 ve 10'daki satırları etkiler; 11-18, 08-01'in döndürdüğü numaraya dayandığı için dolaylı olarak etkilenir.
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
