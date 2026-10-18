# 18 — Ticari poliçe sil (Delete Commercial Policy)

Şablon: [TEMPLATE.md](TEMPLATE.md)

| Alan | Değer |
|---|---|
| Operasyon | Ticari poliçe sil: Db2 `POLICY` satırı silinir, `COMMERCIAL` satırı yabancı anahtar cascade'iyle gider, VSAM `KSDSPOLY` kaydı silinir. Poliçe türü COMMAREA'daki istek kodundan (`01DCOM`) anlaşılır; ama silmede ticariye özgü bir dal yoktur: `'01DCOM'` dört kodlu tek bir koşulun parçasıdır. |
| Programlar (çağrı zinciri) | `LGDPOL01` (iş mantığı) → `EXEC CICS LINK` → `LGDPDB01` (Db2 `POLICY` DELETE) → `LINK` → `LGDPVS01` (VSAM `KSDSPOLY` DELETE). Hata yollarında üçü de `LGSTSQ`'ya LINK eder. Programlar dört poliçe türüne de hizmet eder; ortak kısımlar ve `01DCOM` tahmin edildi, `01DEND`/`01DHOU`/`01DMOT` "başka tür, dal alınmaz" diye işaretlendi. |
| Okunan COBOL dosyaları | `base/src/lgdpol01.cbl` (1-186), `base/src/lgdpdb01.cbl` (1-245), `base/src/lgdpvs01.cbl` (1-132), `base/src/lgstsq.cbl` (1-126), `base/src/lgcmarea.cpy` (1-103). |
| Test verisi kaynağı | `base/cntl/db2cre.jcl`: `POLICY` (`db2cre.jcl:161-176`) ve alt tablo cascade tanımları (`HOUSE` `db2cre.jcl:249-250`, `MOTOR` `db2cre.jcl:287-288`, `COMMERCIAL` `db2cre.jcl:335-336`, `CLAIM` `db2cre.jcl:371-373`); müşteri numaraları (`db2cre.jcl:111-112`, `db2cre.jcl:437` … `db2cre.jcl:743`); poliçeler 10 (`db2cre.jcl:777-786`), 9 (`db2cre.jcl:801-810`), 7 (`db2cre.jcl:897-901`), 3 (`db2cre.jcl:969-973`); `HOUSE` 7 (`db2cre.jcl:1059-1068`), `MOTOR` 3 (`db2cre.jcl:1116-1128`), `COMMERCIAL` 10 (`db2cre.jcl:1184-1203`) ve 9 (`db2cre.jcl:1227-1246`). `base/cntl/adef121.jcl` (`adef121.jcl:40-52`, `adef121.jcl:65-73`) ve `base/data/ksdspoly.txt` (`ksdspoly.txt:1`, `ksdspoly.txt:2`, `ksdspoly.txt:6`, `ksdspoly.txt:9`): `KSDSPOLY` tanımı ve başlangıç verisi. `base/cntl/cobol.jcl` (`cobol.jcl:44-50`): derleme seçenekleri (B3). COBOL kodu değil. |
| Kullanılan belgeler ([belge]) | `docs/adr/0004-legacy-test-harness.md` (`0004-legacy-test-harness.md:44-46`, `0004-legacy-test-harness.md:50-52`, `0004-legacy-test-harness.md:56-58`, `0004-legacy-test-harness.md:100-101`, `0004-legacy-test-harness.md:107-109`): yalnızca B4, B8, C grubu, [CICS bilgisi], Kod gözlemleri 9 ve 15 ile Test sırası için. `docs/api-design.md` (`api-design.md:107-108`): yalnızca Açık sorular 2 için. **Kodla doğrulanmadı.** |
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
  davranış Db2'den gelir. Bu dosyada iki kullanımı: (1) **DELETE satır bulamazsa `SQLCODE = 100` döner** (18-10, 18-11); (2) `POLICY`
  satırı silinince `COMMERCIAL`, `HOUSE` ve `MOTOR` satırı `ON DELETE CASCADE` ile silinir (`db2cre.jcl:335-336`,
  `db2cre.jcl:249-250`, `db2cre.jcl:287-288`; 18-08, 18-09, 18-13, 18-14, 18-15, 18-17).
- **[CICS bilgisi]**: kodda yazmayan ama ekipçe kesin kabul edilen CICS davranışı. Bu dosyada üç madde: 1, 2 ve 4 (3. madde konut dosyası 10'a özgü: SYNCPOINT ROLLBACK);
  `16-commercial-add.md` ve `17-commercial-inquire.md` ile aynı:
  1. `LINK … LENGTH(n)` ile çağrılan program `EIBCALEN = n` görür. Bu yüzden zincirdeki alt programın uzunluk kontrolü en dıştaki
     çağıranın uzunluğuna bakmaz.
  2. `EXEC CICS ABEND` görevi sonlandırır; `HANDLE ABEND` olmadığı için denetim çağırana dönmez. Görevin kesinleşmemiş Db2 ve
     VSAM değişiklikleri geri alınır (`TEMPLATE.md` madde 7; VSAM dosyaları `LOG(UNDO)` ile tanımlı: `adef121.jcl:20`, `:45`).
  4. LINK edilen programdaki `EXEC CICS RETURN` denetimi LINK eden programa, `LINK` deyiminden sonraki deyime döndürür; çağrılan
     programın COMMAREA'ya yazdıkları çağırana görünür.

  Düzenek bu davranışları ölçmez ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:107-109`); düzenekteki karşılıkları B1
  (LINK), B8 (ABEND) ve `RETURN` → `GOBACK` ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:44-46`).
- **C (belirsizlik grubu)**: sonucu kodda tanımlı olmayan, çalışma ortamına bağlı davranış: gerçek CICS (görev sonunda iş biriminin
  kalıcı olması ya da geri alınması, LINK hatası, COMMAREA'sız çağrı), gerçek Db2 (iş birimi, kodda yazmayan hata kodları), gerçek
  VSAM ya da bellek düzeni (kuyruk kaydının `WRITE-MSG` ötesi baytları). "(C)" işaretli hücre **belirsiz**dir: ne ön koşul koduyla ne
  [Db2 bilgisi] ya da [CICS bilgisi] ile kesinleştirilmiştir ve düzenekte ölçülmez. [belge, kodla doğrulanmadı]
  `0004-legacy-test-harness.md:107-109`.

Ön koşul kodları test düzeneğinin sağlaması gereken koşullardır. Bir kodu taşıyan tahmin **kesin değil, ön koşulludur**: ön koşul
sağlanmazsa tahmin geçersizdir ve "Ön koşullu satırlar" tablosundaki son sütun geçerli olur.

| Kod | Ön koşul |
|---|---|
| B1 | Düzenek COMMAREA'yı her LINK'te en dıştaki çağıranın `LENGTH(32500)` tamponuyla geçirir; `EIBCALEN` yalnızca bildirilen uzunluktur, tamponun `EIBCALEN` ötesi boşlukla doldurulur. `EIBCALEN` ötesine yapılan yazmalar da bu tampona düşer ve çağıran onları görür. `EIBCALEN = 0` ile çağrılan programa da 32500 baytlık boşluk tamponu geçirilir ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:46`). **Bu dosyada:** 18-07, 18-19. Diğer silme satırları ona dayanmaz: silme programları yalnızca ilk 28 baytı işler, hata mesajı da en çok `EIBCALEN` kadarını kopyalar (`lgdpdb01.cbl:231-238`, `lgdpvs01.cbl:118-125`). |
| B2 | SQLCA `SQLCODE = 0` ile başlar. Bu dosyada: 18-04 (`lgdpdb01.cbl:214`, hiçbir SQL deyimi çalışmadan). |
| B3 | Derleyici sayı taşımasında PICTURE'ın hane sayısına göre keser (10 haneli numara → 9 hane). Derleme seçeneklerinde `TRUNC` belirtilmemiş [kod] `cobol.jcl:44-50`. Bu dosyada: 18-15. |
| B4 | Düzenek satırda istenen `SQLCODE`/`RESP`'i üretir; **aksi söylenmedikçe** her SQL deyiminin `SQLCODE`'u `0`, her dosya komutunun `RESP`'i `NORMAL`'dir. Kayıt ekleyip silebilir. Host değişkenlerine yalnızca satırda yazılan değeri koyar; başarısız deyimde host değişkenine yazmaz. Host değişkeni kullanmayan başarılı SQL deyiminin `0`, dosya komutunun `NORMAL` dönmesi de B4'e bağlıdır. [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:56-58`, `0004-legacy-test-harness.md:135-138`. **Bu dosyada:** host değişkeni döndüren deyim yoktur. `NOTFND` satırlarında **düzenek `RESP = NOTFND` üretir**; bu, gerçek VSAM'de anahtar dosyada olmadığı için beklenen sonuçtur (Kod gözlemleri 5, 8). Satırlar: 18-07, 18-08, 18-09, 18-12 … 18-18. |
| B8 | Düzenekte `EXEC CICS ABEND` mesaj + `GOBACK`'e dönüşür: abend eden program çağırana döner ve COMMAREA'ya en son yazılan dönüş kodu görünür. [belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:44-46`. [CICS bilgisi] 2: gerçek CICS'te ABEND görevi sonlandırır; orada çağıranın kodu görmesi belirsizdir (C). Bu dosyada: 18-02 ve 18-04 (`LGCA`; COMMAREA olmadığı için dönüş kodu görünmez, düzenekte görülen ABEND mesajı ve `GOBACK`'tir). |

(B5, B6 ve B7 bu dosyada kullanılmıyor.)

## Kod gözlemleri

1. **Silme yalnızca COMMAREA başlığını kullanır** (`lgcmarea.cpy:10-12`, `lgcmarea.cpy:35`): `CA-REQUEST-ID` 1-6, `CA-RETURN-CODE`
   7-8, `CA-CUSTOMER-NUM` 9-18, `CA-POLICY-NUM` 19-28. Üç program da bu düzeni kullanır (`lgdpol01.cbl:68-69`, `lgdpdb01.cbl:99-102`,
   `lgdpvs01.cbl:64-65`). Ticari alanlar (101-1202. baytlar) hiç okunmaz ve yazılmaz.

2. **Uzunluk kontrolü.** LGDPOL01 ve LGDPDB01 aynı sabite bakar: `WS-CA-HEADER-LEN = 28` (`lgdpol01.cbl:57`, `lgdpdb01.cbl:66`);
   `EIBCALEN < 28` → `'98'` (`lgdpol01.cbl:107-110`, `lgdpdb01.cbl:143-146`). Türe göre bir uzunluk yoktur ve gereken uzunluk `ADD`
   ile kurulmaz (16 ve 17'deki `WS-REQUIRED-CA-LEN` birikmesi burada yoktur). LGDPDB01 zincirde 32500 görür ([CICS bilgisi] 1;
   `lgdpol01.cbl:143`), bu yüzden kendi kontrolü yalnızca doğrudan çağrıda tetiklenir. LGDPVS01'de uzunluk kontrolü yoktur.

3. **Çağrı zinciri ve dönüş akışı.** LGDPOL01 geçerli kodda LGDPDB01'i `LENGTH(32500)` ile LINK eder (`lgdpol01.cbl:126`,
   `lgdpol01.cbl:139-146`). LGDPDB01 DELETE başarılıysa LGDPVS01'i `LENGTH(32500)` ile LINK eder (`lgdpdb01.cbl:167-171`) ve `RETURN`
   eder (`lgdpdb01.cbl:175`); DELETE başarısızsa hata paragrafının içinden `RETURN` eder (`lgdpdb01.cbl:201`) ve LGDPVS01 çağrılmaz.
   LGDPVS01 normal yolda `GOBACK` ile biter (`lgdpvs01.cbl:95-97`) ve dönüş koduna dokunmaz. Denetim LGDPOL01'e döner ([CICS bilgisi]
   3). LINK'lerde `RESP` yoktur; LINK hatasının sonucu kodda tanımlı değildir (C).

4. **Sürpriz 1: istek kodu yerinde ve uzunluk kontrolünden sonra büyük harfe çevrilir.** `MOVE FUNCTION UPPER-CASE(CA-REQUEST-ID)
   TO CA-REQUEST-ID` (`lgdpol01.cbl:117`) `'98'` kontrolünden (`lgdpol01.cbl:107-110`) sonra gelir. Sonuçları:
   - `EIBCALEN < 28` ile gelen küçük harfli kod değişmeden döner (18-03).
   - Geçerli uzunlukta çağıran kodu büyük harfle geri alır, `'99'` alsa bile (`'02dcom'` → `'02DCOM'`, 18-01).
   - LGDPDB01 kodu büyük harfe çevirmez (`lgdpdb01.cbl:160-163`): doğrudan çağrıda `'01dcom'` `'99'` alır (18-06). Zincirde
     LGDPDB01'in `'99'` koluna ulaşılamaz, çünkü LGDPOL01 aynı dört kodu (`lgdpol01.cbl:119-122`) büyük harfle süzüp geçirir.

5. **Sürpriz 2: ticari kod başka türden bir poliçeyi siler.** DELETE'in tek koşulu müşteri ve poliçe numarasıdır
   (`lgdpdb01.cbl:189-194`); `POLICY.policyType`'a ve `COMMERCIAL` satırının varlığına bakılmaz, istek kodu SQL'e girmez.
   `'01DCOM'` ile:
   - **Poliçe 7** (müşteri 6, `policyType = 'C'`: `db2cre.jcl:897-901`; `HOUSE` satırı var: `db2cre.jcl:1068`; `COMMERCIAL` satırı
     yok) silinir, `HOUSE` 7 cascade ile gider [Db2 bilgisi].
   - **Poliçe 3** (müşteri 5, motor: `db2cre.jcl:969-973`; `MOTOR` 3: `db2cre.jcl:1128`) silinir, `MOTOR` 3 cascade ile gider.

   VSAM anahtarı istek kodunun 4. baytıyla kurulur (`lgdpvs01.cbl:77`): `'C'`. Bu poliçelerin anahtarları `'C00000000060000000007'`
   ve `'C00000000050000000003'` olur; dosyadaki kayıtlar ise `'H00000000060000000007'` (`ksdspoly.txt:6`) ve
   `'M00000000050000000003'` (`ksdspoly.txt:9`) anahtarlıdır. Gerçek VSAM'de anahtar bulunmaz (düzenekte B4: `NOTFND`) ve `'81'` döner
   (`lgdpvs01.cbl:86-91`). Sonuç: Db2'de poliçe ve alt tablo satırı yoktur, VSAM'da `H`/`M` kaydı yetim kalır (18-13, 18-14;
   Açık sorular 2).

6. **Sürpriz 3: `SQLCODE = 100` de hatadır; yorum ile kod çelişir.** Yorum "Treat SQLCODE 0 and SQLCODE 100 (record not found) as
   successful" der (`lgdpdb01.cbl:196-197`). Kod yalnızca `0`'ı başarı sayar: `IF SQLCODE NOT EQUAL 0` → `'90'`, hata mesajı, `RETURN`
   (`lgdpdb01.cbl:198-202`). **Yorum-kod çelişkisi:** olmayan bir poliçeyi silmek ([Db2 bilgisi]: DELETE satır bulamazsa `100`)
   `'90'` ve `SQLCODE=+00100` içeren bir hata mesajı üretir; LGDPVS01 çağrılmaz (18-10, 18-11). ADR de bu noktayı ölçülmeyen bir Db2
   varsayımı olarak anar ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:100-101`).

7. **Sürpriz 4: geri alma yoktur.** Üç programda `SYNCPOINT`, `ROLLBACK`, `HANDLE` ya da `COMMIT` yoktur; tek `ABEND`, COMMAREA'sız
   çağrıdaki `LGCA`'dır (`lgdpol01.cbl:98`, `lgdpdb01.cbl:134`). `'81'` yolunda (`lgdpvs01.cbl:86-91`) Db2 silmesi yapılmıştır ve hiçbir
   program onu geri almaz; `ABEND` de yapılmaz. Silmenin gerçek ortamda kalıcı olması görev sonu davranışına bağlıdır (C;
   `16-commercial-add.md` Kod gözlemleri 12 ile aynı durum).

8. **Sürpriz 5: 10 haneli müşteri numarası Db2 ile VSAM'ı ayırır.** LGDPDB01 `CA-CUSTOMER-NUM` 9(10)'u S9(9) COMP'a taşır
   (`lgdpdb01.cbl:82`, `lgdpdb01.cbl:149`; B3: `1000000001` → 1). LGDPVS01 anahtarı kesmeden kurar (`lgdpvs01.cbl:28`,
   `lgdpvs01.cbl:79`): `'C10000000010000000010'`. Müşteri `1000000001` + poliçe 10 ile Db2'de poliçe 10 silinir, VSAM anahtarı bulunmaz
   ve `'81'` döner; gerçek `'C00000000010000000010'` kaydı (`ksdspoly.txt:1`) yerinde kalır (18-15).

9. **Sürpriz 6: hata mesajı uzunlukları ve RESP2'nin kaybı.**

   | Program | `ERROR-MSG` | Alanlar (sırayla, bayt) | `CA-ERROR-MSG` |
   |---|---|---|---|
   | LGDPOL01 | 45 bayt (`lgdpol01.cbl:39-44`) | `EM-DATE` 1-8, ␣, `EM-TIME` 10-15, `' LGDPOL01'` 16-24, `EM-VARIABLE` X(21) 25-45 | 99 bayt (`lgdpol01.cbl:46-48`) |
   | LGDPDB01 | 87 bayt (`lgdpdb01.cbl:41-53`) | … `' LGDPDB01'` 16-24, `' CNUM='` 25-30, `EM-CUSNUM` 31-40, `' PNUM='` 41-46, `EM-POLNUM` 47-56, `EM-SQLREQ` 57-72, `' SQLCODE='` 73-81, `EM-SQLRC` 82-87 | 99 bayt (`lgdpdb01.cbl:55-57`) |
   | LGDPVS01 | **102 bayt** (`lgdpvs01.cbl:36-51`) | … `' LGDPVS01'` 16-24, `' PNUM='` 25-30, `EM-POLNUM` 31-40, `' CNUM='` 41-46, `EM-CUSNUM` 47-56, `' Delete file KSDSPOLY'` 57-77, `' RESP='` 78-83, `EM-RESPRC` 84-89, `' RESP2='` 90-96, `EM-RESP2RC` 97-102 | 99 bayt (`lgdpvs01.cbl:53-55`) |

   LGDPDB01 `EM-SQLREQ`'e `' DELETE POLICY  '` (16 bayt) yazar (`lgdpdb01.cbl:188`); `EM-CUSNUM` ve `EM-POLNUM` girdiden alınır
   (`lgdpdb01.cbl:152-153`). LGDPVS01 `EM-CUSNUM`, `EM-POLNUM`, `EM-RESPRC` ve `EM-RESP2RC`'yi hata paragrafında doldurur
   (`lgdpvs01.cbl:87`, `lgdpvs01.cbl:109-112`). **RESP2 kuyruğa ulaşmaz:** LGSTSQ COMMAREA'nın yalnızca 90 baytını `WRITE-MSG-MSG`'ye
   kopyalar (`lgstsq.cbl:48`, `lgstsq.cbl:70`). LGDPVS01 mesajının 90. baytı `' RESP2='`'nin ilk baytıdır; etiketin geri kalanı ve
   RESP2 değeri (91-102. baytlar) kayda girmez. Kaydın uzunluğu `EIBCALEN + 5` = 107'dir (`lgstsq.cbl:71`, `lgstsq.cbl:90`,
   `lgstsq.cbl:94-99`, `lgstsq.cbl:105-111`) ve 95 baytlık `WRITE-MSG`'yi (`lgstsq.cbl:25-28`) 12 bayt aşar; o baytlar mesajın
   kendisi değil, bellekte `WRITE-MSG`'den sonra gelen alanlardır (C). Satırlarda yalnızca LGSTSQ'ya gönderilen mesaj tahmin edilir;
   tarih ve saat düzenekte boştur ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:108-109`).

10. **Sürpriz 7: `If CA-RETURN-CODE > 0` gereksizdir.** LGDPOL01 LINK'ten sonra dönüş kodu sıfırdan büyükse `RETURN` eder
    (`lgdpol01.cbl:127-129`); değilse `END-IF`'ten sonra yine `RETURN` eder (`lgdpol01.cbl:133`). İki kolun sonucu aynıdır; dal yalnızca
    kapsam için ayrı madde olarak tutuldu.

11. **Sürpriz 8: diğer yorum notları.**
    - `lgdpol01.cbl:54-55` ve `lgdpdb01.cbl:63-64`: "for Endowment this does not allow for VARCHAR"; iki program da yalnızca 28 baytlık
      başlığı denetler, türe göre uzunluk yoktur.
    - `lgdpol01.cbl:170`: "Write 90 bytes or as much as we have of commarea"; LGDPOL01'de bu bloğa ulaşılamaz, tek çağrı yeri
      `EIBCALEN = 0` yoludur (`lgdpol01.cbl:95-97`, `lgdpol01.cbl:171`).
    - `lgdpdb01.cbl:182-184`: "because of FOREIGN KEY definitions the delete should be propagated"; Db2 tanımıyla tutarlıdır
      (`db2cre.jcl:335-336`), çelişki değildir.
    - `POLICY`'ye bağlı `CLAIM` tablosu da cascade'lidir (`db2cre.jcl:371-373`); örnek veride `CLAIM` satırı yoktur, bu yüzden
      satırlarda geçmez.

12. **VSAM anahtarı ve LGDPVS01.** Anahtar 21 bayttır (`lgdpvs01.cbl:25-29`, `lgdpvs01.cbl:83`; `adef121.jcl:49`):
    `CA-Request-ID(4:1)` + `CA-Customer-Num` + `CA-Policy-Num` (`lgdpvs01.cbl:77-79`). Ticari örnek kayıtların anahtarları
    `'C00000000010000000010'` (poliçe 10, `ksdspoly.txt:1`) ve `'C00000000050000000009'` (poliçe 9, `ksdspoly.txt:2`); `'01DCOM'` ile
    kurulan anahtar bunlarla aynıdır, bu yüzden ticari örnek kayıtlar silinebilir. LGDPVS01'de `EIBCALEN = 0` ya da uzunluk kontrolü
    yoktur; `WS-Commarea-Len` (`lgdpvs01.cbl:75`) hiç kullanılmaz (18-07, 18-18).

13. **Silinecek örnek veri.**

    | Poliçe | Müşteri | `POLICY` | Alt tablo | VSAM anahtarı |
    |---|---|---|---|---|
    | 10 | 1 | `db2cre.jcl:777-786` | `COMMERCIAL` `db2cre.jcl:1184-1203` | `'C00000000010000000010'` (`ksdspoly.txt:1`) |
    | 9 | 5 | `db2cre.jcl:801-810` | `COMMERCIAL` `db2cre.jcl:1227-1246` | `'C00000000050000000009'` (`ksdspoly.txt:2`) |
    | 7 | 6 | `db2cre.jcl:897-901` (`'C'`) | `HOUSE` `db2cre.jcl:1059-1068` | `'H00000000060000000007'` (`ksdspoly.txt:6`) |
    | 3 | 5 | `db2cre.jcl:969-973` (`'M'`) | `MOTOR` `db2cre.jcl:1116-1128` | `'M00000000050000000003'` (`ksdspoly.txt:9`) |

14. **Sayı alanlarının taşınması.** `CA-CUSTOMER-NUM` ve `CA-POLICY-NUM` 9(10), `DB2-CUSTOMERNUM-INT` ve `DB2-POLICYNUM-INT` S9(9)
    COMP'a taşınır (`lgdpdb01.cbl:82-83`, `lgdpdb01.cbl:149-150`); 10 haneli değer kesilir (B3). Hata mesajları ve VSAM anahtarı
    kesmeden alır (`lgdpdb01.cbl:152-153`, `lgdpvs01.cbl:78-79`).

15. **Düzenek her durumu ayrı süreçte çalıştırır** ([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:50-52`). Silme
    programlarında birikebilecek bir sayaç ya da `ADD` yoktur; bu madde yalnızca 16 ve 17 ile tutarlılık için yazıldı.

## Tahmin tablosu

Ortak girdi: aksi yazılmadıkça çağrılan program LGDPOL01'dir, `EIBCALEN = 32500`, girdide sayılmayan COMMAREA alanları boşluktur.

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| 18-01 | `CA-REQUEST-ID = '02dcom'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000010`. | `CA-RETURN-CODE = '99'` (`lgdpol01.cbl:124`) | `CA-REQUEST-ID = '02DCOM'` (yerinde büyük harf). LGDPDB01'e LINK yok; Db2 ve VSAM değişmez; LGSTSQ'ya LINK yok. | `lgdpol01.cbl:102`, `lgdpol01.cbl:107`, `lgdpol01.cbl:117-124`, `lgdpol01.cbl:133` | Arayüz (COMMAREA) kuralı |
| 18-02 | LGDPOL01, `EIBCALEN = 0` (COMMAREA yok). | Yok (COMMAREA yok); `ABEND 'LGCA'` (`lgdpol01.cbl:98`), düzenekte mesaj + `GOBACK` (B8) | LGSTSQ'ya 1 LINK: `ERROR-MSG`, 45 bayt: `' LGDPOL01'` + `' NO COMMAREA RECEIVED'`. LGDPDB01 çağrılmaz. | `lgdpol01.cbl:95-99`, `lgdpol01.cbl:154-169`, `lgdpol01.cbl:171` | Arayüz (COMMAREA) kuralı |
| 18-03 | LGDPOL01, `EIBCALEN = 27`; `CA-REQUEST-ID = '01dcom'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM` ilk 9 bayt `000000001`. | `CA-RETURN-CODE = '98'` (`lgdpol01.cbl:108`) | `CA-REQUEST-ID = '01dcom'` kalır (büyük harfe çevirme kontrolden sonra). LINK yok; Db2 ve VSAM değişmez. | `lgdpol01.cbl:102`, `lgdpol01.cbl:107-110`, `lgdpol01.cbl:117` | Arayüz (COMMAREA) kuralı |
| 18-04 | LGDPDB01 **doğrudan**, `EIBCALEN = 0`. | Yok (COMMAREA yok); `ABEND 'LGCA'` (`lgdpdb01.cbl:134`), düzenekte mesaj + `GOBACK` (B8) | LGSTSQ'ya 1 LINK: `ERROR-MSG`, 87 bayt: `' LGDPDB01'` + `' NO COMMAREA RECEIVED'` + ␣×36 + `'+00000'` (`EM-SQLRC`, B2). SQL deyimi çalışmaz. | `lgdpdb01.cbl:131-135`, `lgdpdb01.cbl:214`, `lgdpdb01.cbl:225-230` | Arayüz (COMMAREA) kuralı |
| 18-05 | LGDPDB01 **doğrudan**, `EIBCALEN = 27`; ilk 27 bayt 18-03 gibi, ama `'01DCOM'`. | `CA-RETURN-CODE = '98'` (`lgdpdb01.cbl:144`) | DELETE çalışmaz; LGDPVS01'e LINK yok; Db2 ve VSAM değişmez. | `lgdpdb01.cbl:138`, `lgdpdb01.cbl:143-146` | Arayüz (COMMAREA) kuralı |
| 18-06 | LGDPDB01 **doğrudan**, `EIBCALEN = 32500`; `CA-REQUEST-ID = '01dcom'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000010`. | `CA-RETURN-CODE = '99'` (`lgdpdb01.cbl:165`) | `CA-REQUEST-ID = '01dcom'` kalır (LGDPDB01 büyük harfe çevirmez). DELETE çalışmaz; LGDPVS01'e LINK yok; Db2 ve VSAM değişmez. | `lgdpdb01.cbl:143`, `lgdpdb01.cbl:160-165`, `lgdpdb01.cbl:175` | Arayüz (COMMAREA) kuralı |
| 18-07 | LGDPVS01 **doğrudan**, `EIBCALEN = 0`; düzenek 32500 baytlık boşluk tamponu geçirir (B1). | `CA-RETURN-CODE` = ␣␣ (değişmez; dönüş kodunu yalnızca `lgdpvs01.cbl:88` yazar) | Anahtarın 1. baytı `CA-Request-ID(4:1)` = ␣. `DELETE FILE` çalışır (B4: `RESP = NORMAL`), `GOBACK`; LGSTSQ'ya LINK yok. Anahtarın 2-21. baytları boşluk içeren 9(10) alanların X(10)'a taşınmasıyla kurulur, içeriği derleyiciye bağlıdır (C). Gerçek CICS'te `EIBCALEN = 0` iken COMMAREA yoktur; sonuç C. | `lgdpvs01.cbl:75-86`, `lgdpvs01.cbl:88`, `lgdpvs01.cbl:95-97` | Arayüz (COMMAREA) kuralı |
| 18-08 | `CA-REQUEST-ID = '01DCOM'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000010` (poliçe 10, müşteri 1: `db2cre.jcl:777-778`). | `CA-RETURN-CODE = '00'` (`lgdpol01.cbl:102`, `lgdpdb01.cbl:138`) | B4 (DELETE `0`, DELETE FILE `NORMAL`). COMMAREA'nın geri kalanı değişmez. `POLICY` 10 silinir; `COMMERCIAL` 10 cascade ile silinir [Db2 bilgisi]; `KSDSPOLY`'de `'C00000000010000000010'` silinir. LGSTSQ'ya LINK yok ("18-08 girdi ve beklenen değerler"). | `lgdpol01.cbl:117-133`, `lgdpdb01.cbl:149-153`, `lgdpdb01.cbl:160-175`, `lgdpdb01.cbl:189-198`, `lgdpvs01.cbl:77-86`, `lgdpvs01.cbl:95-97`; `db2cre.jcl:335-336`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 18-09 | 18-08 ile aynı, ama `CA-CUSTOMER-NUM = 0000000005`, `CA-POLICY-NUM = 0000000009` (`db2cre.jcl:801-802`). | `CA-RETURN-CODE = '00'` (`lgdpdb01.cbl:138`) | B4. `POLICY` 9 ve cascade ile `COMMERCIAL` 9 silinir [Db2 bilgisi]; `KSDSPOLY`'de `'C00000000050000000009'` (`ksdspoly.txt:2`) silinir. | `lgdpdb01.cbl:189-198`, `lgdpvs01.cbl:77-86`; `db2cre.jcl:335-336`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 18-10 | 18-08 ile aynı, ama `CA-CUSTOMER-NUM = 0000000005` (poliçe 10'un müşterisi 1'dir: `db2cre.jcl:778`). **DELETE satır bulamazsa `SQLCODE = 100` döner [Db2 bilgisi].** | `CA-RETURN-CODE = '90'` (`lgdpdb01.cbl:199`) | Hiçbir satır silinmez; LGDPVS01'e LINK yok. LGSTSQ'ya 2 LINK (LGDPDB01'den): (1) `ERROR-MSG`, 87 bayt: `' LGDPDB01'`, `' CNUM=0000000005'`, `' PNUM=0000000010'`, `' DELETE POLICY  '`, `' SQLCODE=+00100'`; (2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın 1-90. baytları (7-8. baytlar `'90'`). Yorum (`lgdpdb01.cbl:196-197`) bunun başarı sayılacağını söyler; kod hata sayar. | `lgdpdb01.cbl:189-194`, `lgdpdb01.cbl:198-202`, `lgdpdb01.cbl:212-228`, `lgdpdb01.cbl:237-242`, `lgdpol01.cbl:127-128`; [Db2 bilgisi] (DELETE `100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 18-11 | 18-08 ile aynı, ama `CA-POLICY-NUM = 0000000000` (poliçe 0 yok: örnek poliçeler 1-10, yeni poliçeler 1000001'den: `db2cre.jcl:162-163`). **DELETE satır bulamazsa `SQLCODE = 100` döner [Db2 bilgisi].** | `CA-RETURN-CODE = '90'` (`lgdpdb01.cbl:199`) | 18-10 gibi; mesajda `' CNUM=0000000001'`, `' PNUM=0000000000'`, `' SQLCODE=+00100'`. | `lgdpdb01.cbl:192-193`, `lgdpdb01.cbl:198-202`; [Db2 bilgisi] (DELETE `100`), [CICS bilgisi] 1 ve 4 | İş kuralı |
| 18-12 | 18-08 ile aynı; DELETE `SQLCODE = -904` (B4; `0` dışındaki her değer aynı yolu izler). | `CA-RETURN-CODE = '90'` (`lgdpdb01.cbl:199`) | Hiçbir satır silinmez (B4); LGDPVS01'e LINK yok. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' CNUM=0000000001'`, `' PNUM=0000000010'`, `' DELETE POLICY  '`, `' SQLCODE=-00904'`; (2) `CA-ERROR-MSG` (7-8. baytlar `'90'`). | `lgdpdb01.cbl:198-202`, `lgdpdb01.cbl:212-228`, `lgdpdb01.cbl:237-242` | İş kuralı |
| 18-13 | `CA-REQUEST-ID = '01DCOM'`, `CA-CUSTOMER-NUM = 0000000006`, `CA-POLICY-NUM = 0000000007` (poliçe 7: `policyType = 'C'`, `HOUSE` satırı var, `COMMERCIAL` yok: `db2cre.jcl:897-901`, `db2cre.jcl:1068`). DELETE FILE `RESP = NOTFND` (B4; gerçek VSAM'de `'C00000000060000000007'` dosyada olmadığı için beklenen; `NORMAL` dışındaki her değer aynı yolu izler). | `CA-RETURN-CODE = '81'` (`lgdpvs01.cbl:88`) | B4 (DELETE `0`). `POLICY` 7 silinir, `HOUSE` 7 cascade ile silinir [Db2 bilgisi]; kod silmeyi geri almaz (gerçek ortamda kalıcılığı C). `KSDSPOLY`'de `'H00000000060000000007'` (`ksdspoly.txt:6`) yetim kalır. LGSTSQ'ya 2 LINK (LGDPVS01'den): (1) `ERROR-MSG`, 102 bayt: `' LGDPVS01'`, `' PNUM=0000000007'`, `' CNUM=0000000006'`, `' Delete file KSDSPOLY'`, `' RESP='` + `WS-RESP` (düzeneğin NOTFND için verdiği değer), `' RESP2='` + `EIBRESP2`; (2) `CA-ERROR-MSG` (7-8. baytlar `'81'`). | `lgdpdb01.cbl:189-198`, `lgdpdb01.cbl:168-171`, `lgdpvs01.cbl:77-91`, `lgdpvs01.cbl:99-116`, `lgdpvs01.cbl:124-129`; `db2cre.jcl:249-250`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 18-14 | 18-13 ile aynı, ama `CA-CUSTOMER-NUM = 0000000005`, `CA-POLICY-NUM = 0000000003` (poliçe 3, motor: `db2cre.jcl:969-973`, `db2cre.jcl:1128`). DELETE FILE `RESP = NOTFND` (B4; gerçek VSAM'de `'C00000000050000000003'` dosyada olmadığı için beklenen). | `CA-RETURN-CODE = '81'` (`lgdpvs01.cbl:88`) | B4. `POLICY` 3 silinir, `MOTOR` 3 cascade ile silinir [Db2 bilgisi] (kalıcılığı C). `KSDSPOLY`'de `'M00000000050000000003'` (`ksdspoly.txt:9`) yetim kalır. LGSTSQ'ya 2 LINK: `ERROR-MSG`'de `' PNUM=0000000003'`, `' CNUM=0000000005'`; `CA-ERROR-MSG` (7-8. baytlar `'81'`). | `lgdpdb01.cbl:189-198`, `lgdpvs01.cbl:77-91`; `db2cre.jcl:287-288`; [CICS bilgisi] 1 ve 4 | İş kuralı |
| 18-15 | 18-08 ile aynı, ama `CA-CUSTOMER-NUM = 1000000001`. DELETE FILE `RESP = NOTFND` (B4; gerçek VSAM'de `'C10000000010000000010'` dosyada olmadığı için beklenen). | `CA-RETURN-CODE = '81'` (`lgdpvs01.cbl:88`) | `DB2-CUSTOMERNUM-INT = 1` (B3) → DELETE `0` (B4): `POLICY` 10 ve cascade ile `COMMERCIAL` 10 silinir [Db2 bilgisi] (kalıcılığı C). VSAM anahtarı kesilmez: `'C10000000010000000010'` [kod] (`lgdpvs01.cbl:77-79`); `'C00000000010000000010'` (`ksdspoly.txt:1`) yetim kalır. `CA-CUSTOMER-NUM = 1000000001` kalır. LGSTSQ'ya 2 LINK: `ERROR-MSG`'de `' PNUM=0000000010'`, `' CNUM=1000000001'`. | `lgdpdb01.cbl:149`, `lgdpdb01.cbl:192`, `lgdpvs01.cbl:77-91`; [CICS bilgisi] 1 ve 4 | Arayüz (COMMAREA) kuralı |
| 18-16 | LGDPDB01 **doğrudan**, `EIBCALEN = 28`; `CA-REQUEST-ID = '01DCOM'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000010`; DELETE `SQLCODE = -904` (B4). | `CA-RETURN-CODE = '90'` (`lgdpdb01.cbl:199`) | Hiçbir satır silinmez; LGDPVS01'e LINK yok. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG` 18-12 gibi; (2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın 1-28. baytları + ␣×62 (`EIBCALEN < 91` kolu). | `lgdpdb01.cbl:143`, `lgdpdb01.cbl:198-202`, `lgdpdb01.cbl:230-236` | Arayüz (COMMAREA) kuralı |
| 18-17 | LGDPOL01, `EIBCALEN = 28`; `CA-REQUEST-ID = '01dcom'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000010`. | `CA-RETURN-CODE = '00'` (`lgdpdb01.cbl:138`) | `CA-REQUEST-ID = '01DCOM'` (yerinde büyük harf). B4: 18-08 ile aynı silme (`POLICY` 10, cascade ile `COMMERCIAL` 10 [Db2 bilgisi], `'C00000000010000000010'`). 28 bayt silme için yeterlidir. | `lgdpol01.cbl:107`, `lgdpol01.cbl:117-126`, `lgdpdb01.cbl:160-171`, `lgdpvs01.cbl:77-86`; [CICS bilgisi] 1 ve 4 | Arayüz (COMMAREA) kuralı |
| 18-18 | LGDPVS01 **doğrudan**, `EIBCALEN = 50`; `CA-REQUEST-ID = '01DCOM'`, `CA-RETURN-CODE = '00'`, `CA-CUSTOMER-NUM = 0000000001`, `CA-POLICY-NUM = 0000000000`. DELETE FILE `RESP = NOTFND` (B4; gerçek VSAM'de `'C00000000010000000000'` dosyada olmadığı için beklenen). | `CA-RETURN-CODE = '81'` (`lgdpvs01.cbl:88`) | VSAM kaydı silinmez. LGSTSQ'ya 2 LINK: (1) `ERROR-MSG`: `' PNUM=0000000000'`, `' CNUM=0000000001'`, `' Delete file KSDSPOLY'`; (2) `CA-ERROR-MSG`: `'COMMAREA='` + COMMAREA'nın 1-50. baytları + ␣×40 (`EIBCALEN < 91` kolu). | `lgdpvs01.cbl:77-91`, `lgdpvs01.cbl:117-123` | Arayüz (COMMAREA) kuralı |
| 18-19 | LGDPOL01 çağrılır. `EIBCALEN` 1 ile 7 arasında (sınır değer: `7`); 8. bayttan ötesi tamponda boşluk (B1). | `CA-RETURN-CODE` (tamponun 7-8. baytları) = `'98'` (`lgdpol01.cbl:108`; B1) | `lgdpol01.cbl:102`'deki `'00'` ve `lgdpol01.cbl:108`'deki `'98'` `EIBCALEN`'in ötesine, tampona yazılır (B1). İstek kodu büyük harfe çevrilmez (`lgdpol01.cbl:117`'ye gelinmez). LINK yok; Db2 ve VSAM değişmez. Gerçek CICS'te COMMAREA'nın ötesine yazmanın sonucu C. | `lgdpol01.cbl:95`, `lgdpol01.cbl:102`, `lgdpol01.cbl:107-110` | Arayüz (COMMAREA) kuralı |

## 18-08 girdi ve beklenen değerler

COMMAREA'nın alan alan girdisi ve dönüşte beklenen hali. "␣×n" sağdaki n adet boşluğu gösterir.

| COMMAREA alanı | Girdi | Girdinin kaynağı | Dönüşte beklenen | Alanı kullanan / dolduran COBOL satırı |
|---|---|---|---|---|
| `CA-REQUEST-ID` | `'01DCOM'` | istek kodu: `lgdpol01.cbl:122`, `lgdpdb01.cbl:162` | değişmez (zaten büyük harf) | `lgdpol01.cbl:117-122`, `lgdpdb01.cbl:160-163`, `lgdpvs01.cbl:77` |
| `CA-RETURN-CODE` | ␣×2 | – | `'00'` | `lgdpol01.cbl:102`, `lgdpdb01.cbl:138` |
| `CA-CUSTOMER-NUM` | `0000000001` | `db2cre.jcl:778` | değişmez | `lgdpdb01.cbl:149`, `lgdpdb01.cbl:152`, `lgdpvs01.cbl:79` |
| `CA-POLICY-NUM` | `0000000010` | `db2cre.jcl:777` | değişmez | `lgdpdb01.cbl:150`, `lgdpdb01.cbl:153`, `lgdpvs01.cbl:78` |
| 29-32500. baytlar | ␣×32472 | – | değişmez | kullanılmaz |

### 18-08'den sonra Db2'de ve VSAM'da beklenen durum

Gerçek Db2'de ve VSAM'da beklenen durum; düzenekte veri durumu B4'tür (düzenek veri durumunu senaryoya göre kurar).

| Hedef | Beklenen | Kaynak |
|---|---|---|
| `POLICY` 10 | Yok (silindi; B4: DELETE `0`). | `lgdpdb01.cbl:189-194` |
| `COMMERCIAL` 10 | Yok (`ON DELETE CASCADE` [Db2 bilgisi]). | `db2cre.jcl:335-336` |
| `KSDSPOLY` `'C00000000010000000010'` | Yok (silindi; B4: `RESP = NORMAL`). | `lgdpvs01.cbl:77-85`, `ksdspoly.txt:1` |
| Müşteri 1 | Değişmez (müşteri silinmez; `POLICY`'nin yabancı anahtarı müşteriden poliçeye doğru cascade'lidir, tersi değil [Db2 bilgisi]). | `db2cre.jcl:174-176` |
| Diğer veri | `POLICY` 1-9, `COMMERCIAL` 9 ve `KSDSPOLY`'nin diğer 9 kaydı değişmez. | `lgdpdb01.cbl:192-193` (tek satır koşulu) |

## Kapsam tablosu

Onaylanan kapsam envanterinin her maddesi ve onu kapsayan test numaraları. "Başka tür" maddelerine tahmin yazılmadı; bu maddeler,
dalın alınmadığı bir ticari teste bağlandı. Ulaşılamayan dallar "kapsam dışı: ulaşılamaz" gerekçesiyle tabloda tutuldu.

| Envanter no | Madde | Test no | Not |
|---|---|---|---|
| 1 | `lgdpol01.cbl:95-99`: `EIBCALEN = 0` → mesaj + `ABEND 'LGCA'` (ortak) | 18-02 | |
| 2 | `lgdpol01.cbl:102`: `'00'` (ortak) | 18-01, 18-08, 18-19 | |
| 3 | `lgdpol01.cbl:107-110`: `EIBCALEN < 28` → `'98'` + RETURN (ortak) | 18-03, 18-19; geçen kol 18-17 | |
| 4 | `lgdpol01.cbl:117`: istek kodu yerinde büyük harf, uzunluk kontrolünden sonra (ortak) | 18-01, 18-03, 18-17 | Kod gözlemleri 4 |
| 5 | `lgdpol01.cbl:119-124`: dört kodun hiçbiri değilse `'99'` (ortak; `'01DCOM'` `lgdpol01.cbl:122`) | 18-01 | |
| 6 | `lgdpol01.cbl:119-121`: `'01DEND'`, `'01DMOT'`, `'01DHOU'` (başka tür) | 18-08 | Dal alınmaz. |
| 7 | `lgdpol01.cbl:126`, `lgdpol01.cbl:139-146`: `LINK LGDPDB01 LENGTH(32500)` (ortak) | 18-08 | LINK hatası kapsam dışı: kodda dal yok, sonuç C. |
| 8 | `lgdpol01.cbl:127-129`: `If CA-RETURN-CODE > 0` → RETURN (ortak) | doğru kolu 18-10; yanlış kolu 18-08 | Kod gözlemleri 10 |
| 9 | `lgdpol01.cbl:133`: RETURN (ortak) | 18-01, 18-08 | |
| 10 | `lgdpol01.cbl:154-169`: WRITE-ERROR-MESSAGE, `ERROR-MSG` 45 bayt (ortak) | 18-02 | |
| 11 | `lgdpol01.cbl:171-185`: `EIBCALEN > 0` / `< 91` (ortak) | yanlış kolu 18-02 | Doğru kolu (`lgdpol01.cbl:172-184`) kapsam dışı: ulaşılamaz; tek çağrı yeri `EIBCALEN = 0` yolu (Kod gözlemleri 11). |
| 12 | `lgdpdb01.cbl:125`: `INITIALIZE DB2-IN-INTEGERS` (ortak) | 18-08 | |
| 13 | `lgdpdb01.cbl:131-135`: `EIBCALEN = 0` → `ABEND 'LGCA'` (ortak) | 18-04 | Zincirde ulaşılmaz (madde 1 önce yakalar); yalnızca doğrudan çağrı. |
| 14 | `lgdpdb01.cbl:138`: `'00'` (ortak) | 18-05, 18-08 | |
| 15 | `lgdpdb01.cbl:143-146`: `EIBCALEN < 28` → `'98'` (ortak) | 18-05 | Zincirde ulaşılamaz ([CICS bilgisi] 1); yalnızca doğrudan çağrı. |
| 16 | `lgdpdb01.cbl:149-150`: müşteri/poliçe → S9(9) COMP (ortak) | 18-15 | B3 |
| 17 | `lgdpdb01.cbl:152-153`: `EM-CUSNUM`, `EM-POLNUM` ← girdi (ortak) | 18-10, 18-11, 18-12 | |
| 18 | `lgdpdb01.cbl:160-165`: dört kodun hiçbiri değilse `'99'`; büyük harfe çevirme yok (ortak) | 18-06 | Zincirde ulaşılamaz (LGDPOL01 aynı kodları süzer); yalnızca doğrudan çağrı. |
| 19 | `lgdpdb01.cbl:160-163`: `'01DEND'`, `'01DHOU'`, `'01DMOT'` (başka tür) | 18-08 | Dal alınmaz. |
| 20 | `lgdpdb01.cbl:189-194`: DELETE, müşteri VE poliçe; `policyType` ve `COMMERCIAL` okunmaz (ortak) | 18-08, 18-10, 18-13, 18-14 | Kod gözlemleri 5 |
| 21 | `db2cre.jcl:335-336`, `db2cre.jcl:249-250`, `db2cre.jcl:287-288`: alt tablo cascade'i [Db2 bilgisi] | 18-08, 18-09, 18-13, 18-14, 18-15, 18-17 | |
| 22 | `lgdpdb01.cbl:198-202`: `SQLCODE` ≠ 0 → `'90'` + mesaj + RETURN; `100` dahil (ortak) | 18-10, 18-11, 18-12, 18-16 | |
| 23 | `lgdpdb01.cbl:196-197` ile `lgdpdb01.cbl:198`: yorum-kod çelişkisi (ortak) | 18-10, 18-11 | Kod gözlemleri 6 |
| 24 | `lgdpdb01.cbl:168-171`: `LINK LGDPVS01 LENGTH(32500)`, yalnızca DELETE başarılıysa (ortak) | 18-08 | LINK hatası kapsam dışı: kodda dal yok, sonuç C. |
| 25 | `lgdpdb01.cbl:175`: RETURN (ortak) | 18-06, 18-08 | |
| 26 | `lgdpdb01.cbl:212-228`: WRITE-ERROR-MESSAGE, `ERROR-MSG` 87 bayt (ortak) | 18-04, 18-10, 18-11, 18-12, 18-16 | |
| 27 | `lgdpdb01.cbl:230`, `lgdpdb01.cbl:237-242`: `EIBCALEN` ≥ 91 kolu (ortak) | 18-10, 18-12; yanlış kolu 18-04 | |
| 28 | `lgdpdb01.cbl:231-236`: `EIBCALEN < 91` kolu (ortak) | 18-16 | Zincirde ulaşılmaz; yalnızca doğrudan çağrı. |
| 29 | `lgdpvs01.cbl:75`: `EIBCALEN` kontrolü yok (ortak) | 18-07 | Zincirde ulaşılmaz; yalnızca doğrudan çağrı. |
| 30 | `lgdpvs01.cbl:77-79`: anahtar `'C'` + müşteri + poliçe (ortak) | 18-08, 18-15 | Kod gözlemleri 12 |
| 31 | `lgdpvs01.cbl:81-85`: `DELETE FILE('KSDSPOLY')` (ortak) | 18-08 | |
| 32 | `lgdpvs01.cbl:86-91`: `RESP` ≠ `NORMAL` → `'81'` + mesaj + RETURN; ABEND yok (ortak) | 18-13, 18-14, 18-15, 18-18 | Kod gözlemleri 7 |
| 33 | `lgdpvs01.cbl:95-97`: normal çıkış `GOBACK` (ortak) | 18-08 | |
| 34 | `lgdpvs01.cbl:99-116`: WRITE-ERROR-MESSAGE, `EM-POLNUM`/`EM-CUSNUM` doldurulur, `ERROR-MSG` 102 bayt (ortak) | 18-13, 18-14, 18-15, 18-18 | Kod gözlemleri 9 |
| 35 | `lgdpvs01.cbl:117`, `lgdpvs01.cbl:124-129`: `EIBCALEN` ≥ 91 kolu (ortak) | 18-13, 18-14, 18-15 | |
| 36 | `lgdpvs01.cbl:118-123`: `EIBCALEN < 91` kolu (ortak) | 18-18 | Zincirde ulaşılmaz; yalnızca doğrudan çağrı. |
| 37 | `lgstsq.cbl:60-66`: `ASSIGN SYSID`, `ASSIGN INVOKINGPROG` (ortak) | 18-02, 18-04, 18-10, 18-11, 18-12, 18-13, 18-14, 18-15, 18-16, 18-18 | Satırlarda yalnızca LGSTSQ'ya giden LINK tahmin edildi (Kod gözlemleri 9). |
| 38 | `lgstsq.cbl:68-71`: LINK ile çağrılma kolu; 90 bayt kopyalanır (ortak) | 18-02, 18-04, 18-10, 18-11, 18-12, 18-13, 18-14, 18-15, 18-16, 18-18 | LGDPVS01 mesajının RESP2'si bu kopyada kaybolur (Kod gözlemleri 9). |
| 39 | `lgstsq.cbl:72-79`: `RECEIVE` kolu (ortak) | – | Kapsam dışı: ulaşılamaz; LGSTSQ bu zincirde yalnızca LINK ile çağrılır (`lgdpol01.cbl:166`, `lgdpdb01.cbl:225`, `lgdpvs01.cbl:113` ve ikinci LINK'ler). `16-commercial-add.md` kapsam maddesi 51 ile aynı gerekçe. |
| 40 | `lgstsq.cbl:83-88`: `'Q='` öneki (ortak) | – | Kapsam dışı: ulaşılamaz; gönderilen mesajlar `EM-DATE` ile ya da `'COMMAREA='` ile başlar. |
| 41 | `lgstsq.cbl:90`, `lgstsq.cbl:94-99`, `lgstsq.cbl:105-111`: uzunluk + 5; TD ve TS kuyruğa yazma (ortak) | 18-02, 18-04, 18-10, 18-11, 18-12, 18-13, 18-14, 18-15, 18-16, 18-18 | Kuyruk kaydı satırlarda tahmin edilmez (Kod gözlemleri 9). |
| 42 | `lgstsq.cbl:113-119`: `'R'` → `SEND` (ortak) | – | Kapsam dışı: ulaşılamaz (madde 39 ile aynı neden). |
| 43 | `lgstsq.cbl:121-122`: RETURN (ortak) | 18-02, 18-04, 18-10, 18-11, 18-12, 18-13, 18-14, 18-15, 18-16, 18-18 | |

## Ön koşullu satırlar

Ön koşul kodları dosyanın başında ("Kaynak etiketleri ve ön koşul kodları"). B4 satırlarında son sütun iki durumu ayrı yazar:
**düzenekte** (senaryo değer vermezse; B4'ün varsayılanı `0` / `NORMAL`) ve **gerçek Db2/VSAM'de**.

| Test no | Ön koşul | Ön koşulla tahmin | Ön koşul sağlanmazsa |
|---|---|---|---|
| 18-02 | B8 | ABEND mesajı ve `GOBACK`; LGSTSQ'ya 1 LINK | Gerçek CICS'te görev `LGCA` ile sonlanır ([CICS bilgisi] 2); COMMAREA olmadığı için dönüş kodu zaten yoktur. |
| 18-04 | B2, B8 | ABEND mesajı ve `GOBACK`; mesajda `'+00000'` | B2 sağlanmazsa `EM-SQLRC` SQLCA'daki değeri gösterir; gerçek CICS'te görev sonlanır ([CICS bilgisi] 2). |
| 18-07 | B1, B4 | Dönüş kodu yazılmaz (␣␣); boşluk anahtarla `DELETE FILE`, `GOBACK` | Düzenek `EIBCALEN = 0`'da tampon vermezse COMMAREA adreslenemez; sonuç düzeneğe bağlıdır. `RESP` `NORMAL` değilse `'81'` (`lgdpvs01.cbl:86-91`). Gerçek CICS'te COMMAREA yoktur (C). |
| 18-08 | B4, [Db2 bilgisi], [CICS bilgisi] 1 ve 4 | `'00'`; poliçe 10 Db2'den ve VSAM'dan silinir | DELETE `100` dönerse `'90'` (18-10 yolu); DELETE FILE `NORMAL` dönmezse `'81'` (18-13 yolu). |
| 18-09 | B4, [Db2 bilgisi], [CICS bilgisi] 1 ve 4 | `'00'`; poliçe 9 Db2'den ve VSAM'dan silinir | 18-08 ile aynı. |
| 18-12 | B4 | `'90'`; hiçbir şey silinmez | `SQLCODE = 0` → 18-08; `100` → 18-10. |
| 18-13 | B4, [Db2 bilgisi], [CICS bilgisi] 1 ve 4 | `'81'`; Db2'de poliçe 7 ve `HOUSE` 7 silinir, VSAM `H` kaydı kalır | Düzenek `NORMAL` verirse dönüş kodu `'00'` olur; gerçek VSAM'de `'C…'` anahtarlı kayıt olmadığı için `NOTFND` beklenir. Gerçek ortamda Db2 silmesinin kalıcılığı C. |
| 18-14 | B4, [Db2 bilgisi], [CICS bilgisi] 1 ve 4 | `'81'`; Db2'de poliçe 3 ve `MOTOR` 3 silinir, VSAM `M` kaydı kalır | 18-13 ile aynı. |
| 18-15 | B3, B4, [Db2 bilgisi], [CICS bilgisi] 1 ve 4 | `'81'`; Db2'de poliçe 10 silinir, VSAM `C/1/10` kaydı kalır | Kesme olmazsa `DB2-CUSTOMERNUM-INT = 1000000001`; bu müşterinin poliçe 10'u yoktur → DELETE `100` [Db2 bilgisi] → `'90'` (18-10 yolu), hiçbir şey silinmez. VSAM anahtarının kesilmemesi ön koşula bağlı değildir [kod]. |
| 18-16 | B4 | `'90'`; `CA-DATA` = 1-28. baytlar + boşluk | `SQLCODE = 0` → silme yapılır ve LGDPVS01 çağrılır (18-08 yolu). |
| 18-17 | B4, [Db2 bilgisi], [CICS bilgisi] 1 ve 4 | `'00'`; `CA-REQUEST-ID = '01DCOM'`; poliçe 10 silinir | 18-08 ile aynı; büyük harfe çevirme ön koşula bağlı değildir. |
| 18-18 | B4 | `'81'`; `CA-DATA` = 1-50. baytlar + boşluk | Düzenek `NORMAL` verirse dönüş kodu girdideki `'00'` kalır (LGDPVS01 `'00'` yazmaz). |
| 18-19 | B1 | Tamponun 7-8. baytları `'98'` | B1 sağlanmazsa `EIBCALEN`'in ötesine yazmanın sonucu düzeneğe bağlıdır; gerçek CICS'te C. |

## Belirsiz satırlar

Bu dosyada tamamı belirsiz satır yoktur. Aşağıdaki satırlarda yalnızca belirtilen hücre belirsizdir; satırın kalanı ön koşulludur.

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| 18-07 | Beklenen veri: VSAM anahtarının 2-21. baytları; gerçek CICS'te sonuç | Anahtar baytları boşluk içeren 9(10) alanlardan X(10)'a taşınır (`lgdpvs01.cbl:78-79`); içerik derleyiciye bağlıdır (C). Gerçek CICS'te `EIBCALEN = 0` iken COMMAREA yoktur (C). Düzenekteki dönüş kodu ve akış ön koşullu satırlarda (B1, B4). |
| 18-13 | Beklenen veri: `POLICY` 7 ve `HOUSE` 7 silmesinin gerçek ortamda kalıcılığı | `'81'` yolunda kod Db2 silmesini geri almaz ve `SYNCPOINT` yapmaz (`lgdpvs01.cbl:86-91`, Kod gözlemleri 7); görev sonunda kalıcı olması CICS'in görev sonu davranışına bağlıdır (C). |
| 18-14 | Beklenen veri: `POLICY` 3 ve `MOTOR` 3 silmesinin gerçek ortamda kalıcılığı | 18-13 ile aynı neden (C). |
| 18-15 | Beklenen veri: `POLICY` 10 ve `COMMERCIAL` 10 silmesinin gerçek ortamda kalıcılığı | 18-13 ile aynı neden (C). |
| 18-19 | Beklenen veri: gerçek CICS'te `EIBCALEN`'in ötesine yazılan `'00'` ve `'98'` | `lgdpol01.cbl:102`, `lgdpol01.cbl:108`; `EIBCALEN` < 8 iken bu baytlar COMMAREA'nın dışındadır. Gerçek CICS'te sonucu C. |

## Test sırası ve veri bağımlılıkları

Öneri; sıra ve veri sıfırlama kararı ekibindir. Gerçek veri (ya da veri durumunu tutan bir düzenek) için geçerlidir; düzenek her
satırın veri durumunu B4 ile kendisi kurarsa satırlar birbirinden bağımsızdır ve her durum ayrı süreçte çalışır
([belge, kodla doğrulanmadı] `0004-legacy-test-harness.md:50-52`).

1. **Silme veriyi yok eder.** Yıkıcı satırlar ve sildikleri:

   | Satır | Db2'den silinen | VSAM'dan silinen | VSAM'da yetim kalan |
   |---|---|---|---|
   | 18-08 | `POLICY` 10, `COMMERCIAL` 10 | `'C00000000010000000010'` | – |
   | 18-09 | `POLICY` 9, `COMMERCIAL` 9 | `'C00000000050000000009'` | – |
   | 18-13 | `POLICY` 7, `HOUSE` 7 | – | `'H00000000060000000007'` |
   | 18-14 | `POLICY` 3, `MOTOR` 3 | – | `'M00000000050000000003'` |
   | 18-15 | `POLICY` 10, `COMMERCIAL` 10 | – | `'C00000000010000000010'` |
   | 18-17 | `POLICY` 10, `COMMERCIAL` 10 | `'C00000000010000000010'` | – |

   Yıkıcı olmayan satırlar: 18-01 … 18-07, 18-10, 18-11, 18-12, 18-16, 18-18, 18-19 (18-12 ve 18-16'daki `-904` yalnızca düzenekte
   üretilir; 18-18'in anahtarı dosyada yoktur).
2. **Sıfırlama.** Yıkıcı satırlar en sonda çalışır ve **her birinden önce Db2 (`db2cre.jcl`) ve `KSDSPOLY` (`adef121.jcl:65-73`,
   `ksdspoly.txt`) birlikte yeniden yüklenir.** Yalnızca Db2 yüklenirse 18-13/18-14/18-15'in bıraktığı yetim kayıtlar ve 18-08/18-09/
   18-17'de silinen VSAM kayıtları tutarsız kalır; yalnızca VSAM yüklenirse silinmiş poliçelerin VSAM kayıtları geri gelir.
3. **16 ve 17 ile çakışmalar.**
   - **Poliçe 10** (18-08, 18-15, 18-17): `16-commercial-add.md` 16-01'in girdi kaynağı ve "mevcut veri" satırı (Test sırası 1 poliçe
     10'un yüklü olmasını ister); `17-commercial-inquire.md` 17-04 … 17-16 (01ICOM), 17-32 (müşteri 1), 17-33 ve 17-38
     (`'SO212JN'`).
   - **Poliçe 9** (18-09): 17-17 … 17-22 (02ICOM), 17-23 … 17-31 (müşteri 5 için tam 1 satır), 17-35 … 17-39 (`'BB104BX'`).
   - **Poliçe 7** (18-13): 17-06, 17-18 ve 17-24 `COMMERCIAL` satırı olmadığı için zaten `'01'`/boş sonuç bekler; silmeden sonra da
     sonuç değişmez. Ama `HOUSE` 7 ve poliçe 7 silinir, konut dilimi testleri bozulur.
   - **Poliçe 3** (18-14): 17-19 `'01'` bekler, silmeden sonra da değişmez; 17-23'ün müşteri 5 sonucu (yalnızca poliçe 9) değişmez.
     Ama `MOTOR` 3 ve poliçe 3 silinir, motor dilimi testleri bozulur.
   - 17'nin çok satırlı kurulumları (17-25 …) 18'den önce sıfırlanmış olmalıdır; silme yalnızca girdi poliçesini etkiler.
4. **16-01 ile eklenen poliçenin silinmesi.** `16-commercial-add.md` 16-01'den sonra sıfırlama yapılmazsa, `'01DCOM'` + müşteri 1 +
   poliçe 1000001 18-08 ile aynı kod yolundan geçer: `POLICY` 1000001 ve cascade ile `COMMERCIAL` 1000001 silinir; LGAPVS01'in yazdığı
   `'C00000000010001000001'` anahtarı (`16-commercial-add.md` "16-01'den sonra beklenen durum") LGDPVS01'in kurduğu anahtarla aynıdır,
   kayıt silinir ve `'00'` döner. Ayrı satır yazılmadı.
5. **Önerilen sıra.** 16 → 17 → 18'in yıkıcı olmayan satırları → 18'in yıkıcı satırları (her birinden önce Db2 ve `KSDSPOLY` birlikte
   yeniden yüklenir).

## Açık sorular

Ekip kararı gerektiren, tahminleri etkileyen sorular. Tahminler bu soruların cevabını varsaymaz; varsayım gereken yerde ön koşul
kodu kullanıldı.

1. **`-911` örnek değeri.** Karar verildi: "diğer hata" örneği olarak iş birimini geri almayan `-904` kullanılır; satırlar
   buna göre güncellendi. (`-911` kilitlenme kodudur ve yeni sistemde `503`'e eşlenir, ADR 0002 Karar 8.)
2. **Bulgu: eski sistem iki depoyu tutarsız bırakabilir.** Db2 silmesi ile VSAM silmesi ayrı programlarda, geri alma olmadan yapılır
   (Kod gözlemleri 7). `'01DCOM'` ile poliçe 7 ya da 3 silinince Db2'de poliçe ve alt tablo satırı gider, VSAM'da `H`/`M` kaydı yetim
   kalır (18-13, 18-14); 10 haneli müşteri numarasıyla da aynı şey ticari kayıtta olur (18-15). Dönüş kodu `'81'` olsa da Db2 silmesi
   geri alınmaz. C# tarafında silme tek bir transaction'da mı (ya da tek bir depoda mı) çözülüyor? Çözülüyorsa bu, `'81'` yolunun
   davranışında bilinçli bir sapmadır ve ekip kararı gerekir. [belge, kodla doğrulanmadı] `api-design.md:107-108`: yeni sistemde VSAM
   için ayrı bir depolama yoktur; bu durumda iki depo arasındaki tutarsızlık yeni sistemde oluşamaz ve `'81'` yolunun karşılığı yoktur.
3. **Bilinçli sapma adayı: olmayan poliçeyi silmek.** Kod `SQLCODE = 100`'ü hata sayar ve `'90'` döner; yorum başarı sayılması
   gerektiğini söyler (Kod gözlemleri 6). C# tarafı olmayan bir poliçenin silinmesini başarı (idempotent silme) sayarsa COBOL'dan
   ayrılır. Hangi davranış hedeflenmeli?
4. **Bilinçli sapma adayı: türden bağımsız silme.** Ticari silme kodu başka türden bir poliçeyi siler (Kod gözlemleri 5). C# tarafı
   istek türüyle poliçe türünü karşılaştırırsa COBOL'dan ayrılır.
5. **RESP2 kaybı.** LGDPVS01'in hata mesajındaki RESP2, LGSTSQ'nun 90 baytlık kopyası yüzünden kuyruğa ulaşmaz (Kod gözlemleri 9).
   Bunun ölçülmesi LGSTSQ'nun ayrıca test edilmesine bağlıdır (`16-commercial-add.md` Açık sorular 4).
6. **Gerçek ortamda kalıcılık.** 18-13, 18-14 ve 18-15'te Db2 silmesinin görev sonunda kalıcı olup olmadığı düzenekte ölçülmez (C).
   Ekip bunu gerçek CICS/Db2 ile mi ölçmeli?
7. **`EIBCALEN = 0` ile doğrudan çağrı.** 18-07, B1'e eklenen cümleyle ön koşullu yazıldı; düzeneğin bunu gerçekten yapıp yapmadığı
   doğrulanmalı (`16-commercial-add.md` Açık sorular 5 ile aynı).
