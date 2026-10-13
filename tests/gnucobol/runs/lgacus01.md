# LGACUS01 çalıştırması: GnuCOBOL sonuçları ve test ekibinin tahminleri

| Alan | Değer |
|---|---|
| Tarih | 2026-10-13 |
| Sorumlu | Oğuz |
| Dal | `oguz/accuracy-1` |
| Durum | **Ölçüm çalıştırması** (tahmin doğruluğuna sayılır) |
| Karşılaştırılan tahmin | `tests/predictions/01-customer-add.md` (25 satır, 01-01 ... 01-25) |
| GnuCOBOL | 3.2.0, MSYS2 ucrt64 |
| Ham çıktı | `runs/lgacus01-cikti.txt` |
| Karşılaştırma betiği | `runs/lgacus01-karsilastir.sh` |
| Çalıştırma betiği | `runs/lgacus01-calistir.sh` |
| Dönüşüm betiği | `convert-lgacus01.py` |

> **Bu dosyanın ilk sürümü, hiçbir şey çalıştırılmadan önce commit'lendi.** "Ölçüm kuralı: kontrol", "Değerlendirme kuralları",
> "Düzenek" ve "Çalıştırılacak durumlar" bölümleri o sürümdedir. "Sonuç" bölümleri çalıştırmadan sonra eklenmiştir. Kuralların
> çalıştırmadan önce yazıldığını `git log -- tests/gnucobol/runs/lgacus01.md` gösterir.

## Ölçüm kuralı: kontrol

- Tahmin dosyası **çalıştırmadan önce** `main`'deydi: `236541e` (2026-10-05, #88). Dosya o günden beri değişmedi
  (`git log 236541e..origin/main -- tests/predictions/01-customer-add.md` boş).
- Tahminin dayandığı kaynak commit `61c635f` (dosya başlığı). `base/` o commit ile çalıştırma anındaki `main` (`99e4be8`) arasında
  değişmedi (`git diff 61c635f origin/main -- base/` boş). Tahmindeki satır numaraları geçerli.
- Tahminleri çalıştırmadan önce okudum. Düzeneği dosyanın **kendi** ön koşullarına (B1, B2, B4, B5, B6, B7; B3 bu dosyada yok) göre kurdum.
- Sonuçlar tahminlerden önce kimseyle paylaşılmadı. Tahmin dosyasına çalıştırmadan sonra dokunulmayacak.
- Bu çalıştırmaya H02'deki LGAPOL01 denemesi **katılmaz** (tahminlerden önce yapıldı). Motor (04-07) tahminleri henüz ölçülmedi.

## Değerlendirme kuralları (çalıştırmadan önce belirlendi)

Yöntem `runs/lgicus01.md`'dekiyle aynıdır. Her satır, tahminin **ölçülebilir iddialarına** ayrılır (dönüş kodu, veri, yan etki). Sonra:

1. İddia, düzeneğin gösterebildiği bir şeyse ve çıktıda bayt bayt tutuyorsa: **tuttu**. Tutmuyorsa: **tutmadı**.
2. Tahminin kendisi `belirsiz` dediği hücreler **puanlanmaz**. Gerçek sonuç yazılır ama "tuttu/tutmadı" denmez.
3. Düzeneğin çalıştırmadığı bir şeye dayanan iddialar **ölçülemedi** diye ayrı yazılır ve puanlanmaz: LGSTSQ'nun kuyruğa yazdığı kayıt, gerçek
   tarih ve saat, gerçek Db2/VSAM/CICS davranışı (satırın gerçekten oluşması, geri alma, abend sonrası görevin sonlanması, `[belge]` ifadeleri),
   `< 91` / `ELSE` dalı (iki dal aynı metni üretiyorsa çıktıdan ayırt edilemez).
4. Ön koşullu iddialar (B1, B4, B5, B6, B7), ön koşul düzenekte sağlandığı haliyle puanlanır.
5. Satırın kararı: puanlanan iddialardan biri tutmadıysa **tutmadı**; hepsi tuttuysa **tuttu**; puanlanan iddia yoksa **belirsiz**.
   Bir satırın birden çok durumu varsa (örn. 01-11: 89 ve 8 bayt) hepsi tutmalıdır.
6. Veri satırlarında düzeneğin tablosundan gelen değerler, tahminin kaynağıyla aynı JCL'den alınır. **Geçen bir veri iddiası Db2'nin ya da VSAM'ın
   sonucu değildir**: COBOL'un değerleri doğru yere ve doğru dolguyla taşıdığını, INSERT/WRITE'a doğru girdiyi verdiğini ölçer.

**Doğruluk yüzdesi** (çalıştırmadan önce sabitlendi):

- **Ana ölçü** = tutan satır sayısı ÷ puanlanan satır sayısı (tuttu + tutmadı). `belirsiz` satırlar paydada yoktur; LGICUS01 kaydında da böyleydi.
- **Yardımcı ölçü** = tutan satır sayısı ÷ ölçülen dosyalardaki bütün satırlar (`belirsiz` olanlar payda dahil, pay dışı). Kötümser alt sınırdır.
- Birleşik sonuç: `02-customer-inquire.md` (LGICUS01, `runs/lgicus01.md`) + `01-customer-add.md` (LGACUS01, bu kayıt). `03` ve motor tahminleri henüz ölçülmedi, hesaba girmez.
- Bilgi için: puanlanan iddia (kontrol) düzeyinde de sayılır, ama karar satır düzeyindedir.

## Düzenek

Yöntem `tests/gnucobol/README.md`'deki gibi; farklar aşağıda. Programlar ayrı modüller, sürücü çalıştırılabilir dosya. Her durum ayrı süreçte çalışır
(tahmin dosyası Kod gözlemleri 16: `WS-REQUIRED-CA-LEN` ve `LGAC-NCS` süreç içinde korunur).

| Parça | Dosya | Ne yapar |
|---|---|---|
| LGACUS01, LGACDB01, LGACDB02, LGACVS01 (kopya) | `work/lgac*-gc.cbl` | `EXEC CICS`, `EXEC SQL` ve `DFHRESP` komutları betikle değişti |
| Orijinal kopyalar | `work/lgacus01.cbl`, `lgacdb01.cbl`, `lgacdb02.cbl`, `lgacvs01.cbl` | `base/src/`'den değişmemiş kopya (`cmp` ile doğrulanır) |
| Sayaç stub'ı | `stubs/cicscnt.cbl` | `GET COUNTER` yerine; RESP ve değeri sürücü verir |
| Dosya yazma stub'ı | `stubs/cicswrf.cbl` | `WRITE FILE KSDSCUST` yerine; anahtarı ve 225 baytlık kaydı gösterir, RESP/RESP2'yi sürücü verir |
| Db2 stub'ları | `stubs/db2insc.cbl`, `db2insd.cbl`, `db2inss.cbl`, `db2ident.cbl` | Üç `INSERT` ve `IDENTITY_VAL_LOCAL` için; host değişkenlerini gösterir, yalnızca `SQLCODE` doldurur |
| SQLCA, LGSTSQ | `stubs/sqlca.cpy`, `stubs/lgstsq.cbl` | LGICUS01 çalıştırmasındakiler (değişmedi) |
| Sürücü | `work/drv-lgacus01.cbl` | CICS, sayaç ve Db2'nin rolünü oynar; her durum ayrı süreçte |

### Tahminin ön koşulları nasıl sağlanacak

| Kod | Tahminin istediği | Düzenekte |
|---|---|---|
| B1 | COMMAREA her LINK'te 32500 baytlık tek tamponun adresiyle geçer; `EIBCALEN` yalnızca bildirilen uzunluk; tamponun `EIBCALEN` ötesi boşluk | Sürücü tek 32500 baytlık tamponu kurar ve `EIBCALEN`'den sonrasını boşlukla doldurur; `CALL ... USING DFHCOMMAREA` aynı adresi geçirir. `LINK ... LENGTH(n)` karşılığı `EIBCALEN`'i `n` yapar, dönüşte geri yazar (LGICUS01 çalıştırmasındaki gibi). LGACDB02'ye `CDB2AREA` geçer (tahmin Kod gözlemleri 2). |
| B2 | `SQLCODE` 0 ile başlar | `SQLCA` kopyasında `VALUE` yok; GnuCOBOL sayısal alanı sıfırlar. D0116 ile doğrulanacak (mesajda `+00000`). |
| B4 | Hata kodunu ve veri durumunu düzenek kurar | Sürücü `EXTERNAL` alanlarla verir (aşağıda). Hata kodu için `SQLCODE`/`RESP` değerlerini ben seçtim; tahmin yalnızca "0 dışında" diyor. |
| B5 | `VALUE`'su olmayan WORKING-STORAGE alanları boşlukla başlar | GnuCOBOL'un varsayılanı (alfasayısal alan boşluk). 01-06'da `D2-CUSTSECR-DATA` ile doğrulanacak. |
| B6 | Db2'de `DEFAULT` ile daha önce hiç satır eklenmemiştir; ilk kimlik değeri `START WITH` değeridir | `db2insd` stub'ı `INSERT ... DEFAULT`'ta kimliği 1000001 yapar (`db2cre.jcl:111-112`), `db2ident` bunu döndürür. |
| B7 | `LGSE` parametresiz çalıştırıldı; sayaç K = 11 verir | `cicscnt` stub'ı sürücünün verdiği değeri (varsayılan 11) döndürür. Bu, **düzeneğin kararıdır** (tahmin B7'de istiyor); ölçülen, COBOL'un bu değeri `CA-CUSTOMER-NUM`'a taşıması ve INSERT'lere vermesidir. |

(B3 bu dosyada kullanılmıyor.)

### Değişen komutlar (README yöntem tablosuna eklenen satırlar)

README'nin "Anything else you meet ... Decide for each one what the stub should do and add a row here" kuralına göre, bu zincirde ilk kez karşılaşılanlar:

| Komut | Karşılığı | Neden |
|---|---|---|
| `EXEC CICS ABEND` **zincirin içinde** (LGACVS01'den LGACDB01'e) | `DISPLAY` + `MOVE 'Y' TO ABEND-FLAG` + `GOBACK`; her `LINK` karşılığı dönüşte `IF ABEND-FLAG = 'Y' GOBACK` yapar | Gerçek ABEND geri dönmez ve görevi bitirir. `GOBACK` tek başına çağıranı çalıştırmaya devam ettirirdi (LGACDB02 çağrılırdı; tahmin "çağrılmaz" diyor). Bayrak, görevin sonlanmasını sürücüye kadar yayar; sürücü yine de tamponu gösterebilir. LGICUS01'de abend doğrudan sürücüden çağrılan programdaydı, buna gerek yoktu. |
| `DFHRESP(NORMAL)` | `0` | CICS çevirici makrosu; `NORMAL` değeri 0'dır. |
| `EXEC CICS GET COUNTER` | `CALL 'CICSCNT' USING WS-RESP LastCustNum` | Sayaç sunucusu yok. |
| `EXEC CICS WRITE FILE` | `CALL 'CICSWRF' USING CA-Customer-Num CUSTOMER-RECORD-SIZE WS-RESP` | VSAM yok. Kayıt, `FROM` alanının ilk `LENGTH` baytıdır; anahtar ilk 10 bayt (`Ridfld` aynı alan). |
| `EIBRESP2` | `EXTERNAL` alan | LGACVS01 hata mesajına yazar; stub, RESP2'yi sürücüden alıp bu alana koyar. |
| `EXEC SQL INSERT INTO CUSTOMER` (sayaç numarasıyla) | `CALL 'DB2INSC' USING SQLCA <10 host değişkeni, VALUES sırasıyla>` | Db2 yok. |
| `EXEC SQL INSERT INTO CUSTOMER` (`DEFAULT`) | `CALL 'DB2INSD' USING SQLCA <9 host değişkeni>` | Db2 yok. |
| `EXEC SQL SET :x = IDENTITY_VAL_LOCAL()` | `CALL 'DB2IDENT' USING SQLCA DB2-CUSTOMERNUM-INT` | Db2 yok. |
| `EXEC SQL INSERT INTO CUSTOMER_SECURE` | `CALL 'DB2INSS' USING SQLCA <4 host değişkeni>` | Db2 yok. |

SQL stub'larının `SQLCODE` kararı: sürücünün `EXTERNAL` alanı (`FORCE-INSC`, `FORCE-INSD`, `FORCE-INSS`, `FORCE-IDENT`) 0 değilse onu verir. Aksi halde
`INSERT INTO CUSTOMER` ve `INSERT INTO CUSTOMER_SECURE` için müşteri numarası 1-10 arasındaysa (JCL'deki örnek satırlar: `db2cre.jcl:437-760`)
`SQLCODE = -803` (birincil anahtar çakışması) verir, değilse 0. `-803`'ü ben seçtim: tahmin "SQLCODE ≠ 0" diyor. Yani "numara zaten var" durumu veriyle kurulur,
hata kodunun kendisi düzeneğin varsayımıdır.

### Çalıştırılacak durumlar

C = sürücü → LGACUS01 → (LGACDB01 → LGACVS01, LGACDB02). D = sürücü → ilgili program doğrudan. Varsayılan: `EIBCALEN = 32500`, `CA-REQUEST-ID = 01ACUS`,
`CA-CUSTOMER-NUM = 0`, müşteri alanları = müşteri 1'in değerleri (`db2cre.jcl:438-446`), dönüş kodu önceden `99`, `CA-NUM-POLICIES` önceden `777`
(tampon `EIBCALEN`'den kısa değilse), sayaç `NORMAL` ve 11, tüm zorlanan hatalar 0. LGACDB02 durumlarında düzen `D2-*`: `02ACUS`, dönüş kodu `99`,
müşteri no 1, şifre `5732fec825535eeafb8fac50fee3a8aa` (`db2cre.jcl:455`), sayaç `0000`, durum `N`.

| Durum | Satır | Yol | Değişen girdi |
|---|---|---|---|
| C0101 | 01-01 | C | (varsayılan) |
| C0102 | 01-02 | C | sayaç RESP = 16 |
| C0102B | 01-02 | C | sayaç RESP = 16, `IDENTITY_VAL_LOCAL` `SET`'i `SQLCODE = -204` |
| C0103 | 01-03 | C | sayaç değeri 1 (`CUSTOMER`'da var) |
| C0104 | 01-04 | C | sayaç RESP = 16, `DEFAULT` INSERT'i `SQLCODE = -204` |
| C0105 | 01-05 | C | `WRITE FILE` RESP = 14, RESP2 = 2 |
| C0106 | 01-06 | C | `CUSTOMER_SECURE` INSERT'i `SQLCODE = -204` |
| C0107 | 01-07 | C | `CA-CUSTOMER-NUM = 1` |
| C0108 | 01-08 | C | ad ve soyad boşluk |
| C0109 | 01-09 | C | `CA-DOB` boşluk |
| C0110 | 01-10 | C | `EIBCALEN = 0` |
| C0111A, C0111B | 01-11 | C | `EIBCALEN = 89`, `8` |
| C0112A, C0112B | 01-12 | C | `EIBCALEN = 7`, `1` |
| C0113 | 01-13 | C | `EIBCALEN = 90` |
| C0114 | 01-14 | C | `CA-REQUEST-ID` boşluk |
| D0116 | 01-16 | D: LGACDB01 | `EIBCALEN = 0` |
| D0117A, B, C, D | 01-17 | D: LGACDB01 | `EIBCALEN = 89`, `8`, `7`, `1` |
| D0118 | 01-18 | D: LGACDB01 | `EIBCALEN = 90`, sayaç değeri 1 |
| D0119 | 01-19 | D: LGACDB01 | `EIBCALEN = 32500` |
| D0120 | 01-20 | D: LGACDB02 | `EIBCALEN = 0` |
| D0121 | 01-21 | D: LGACDB02 | `D2-REQUEST-ID` boşluk |
| D0122A, D0122B | 01-22 | D: LGACDB02 | `EIBCALEN = 55`, `90` |
| D0125A, D0125B | 01-25 | D: LGACVS01 | `EIBCALEN = 18`, `90`; `CA-CUSTOMER-NUM = 11`; `WRITE FILE` RESP = 14, RESP2 = 2 |

**Çalıştırılmayacak satırlar:** 01-15 (LINK hatası: `CALL`'un hatası CICS'in `LINK` hatasıyla aynı değil, bir şey ölçmez; tahmin zaten `belirsiz`), 01-23 ve 01-24
(COMMAREA dışı okuma sonucu kodda tanımlı değil; tahmin zaten `belirsiz`; düzenekteki tampon her zaman 32500 bayt olduğundan bir sonuç çıksa da anlamsız olurdu).

### Satır başına puanlanan iddialar

"Girdi tamponu" = sürücünün kurduğu tampon. Mesaj içeriklerinde tarih ve saat alanı (ilk 15 bayt) düzenekte boş.

| Satır | Puanlanan iddialar | Puanlanmayan |
|---|---|---|
| 01-01 | dönüş kodu `00`; `CA-CUSTOMER-NUM = 0000000011`; `CA-NUM-POLICIES = 000`; dokuz müşteri alanı, istek kimliği ve `CA-POLICY-DATA` değişmedi; çağrı sırası LGACDB01 (32500) → sayaç → INSERT(numaralı) → LGACVS01 (225) → LGACDB02 (32500); LGSTSQ'ya mesaj yok; `CUSTOMER` INSERT'ine 11 ve dokuz değer sütun sırasıyla verildi (telefonlar doğru alanda); `KSDSCUST` yazmasında anahtar `0000000011`, 225 baytlık kayıt = tamponun 9-233. baytları (`000` dahil), son 8 bayt boşluk; `CUSTOMER_SECURE` INSERT'ine (11, `5732fec8...`, `N`, 0) verildi | satırların gerçekten oluşması; sayacın 11 vermesi (B7, düzenek kararı) |
| 01-02 | C0102: `00`; `CA-CUSTOMER-NUM = 0001000001` (B6); mesaj yok; `DEFAULT` INSERT çağrıldı, numaralı INSERT çağrılmadı; `IDENTITY_VAL_LOCAL` çağrıldı; zincirin geri kalanı 01-01 gibi, numara 1000001. C0102B: `SET` hatası kontrol edilmez: dönüş kodu `00`, mesaj yok, LGACVS01 ve LGACDB02 çağrıldı, `CA-CUSTOMER-NUM = 0000000000` (Kod gözlemleri 17) | kimlik değerinin gerçek Db2'den gelmesi |
| 01-03 | `90`; `CA-CUSTOMER-NUM` girdideki gibi (`0000000000`); `CA-NUM-POLICIES = 000`; LGACVS01 ve LGACDB02 çağrılmadı; iki mesaj: (1) 71 bayt `' LGACDB01'`, `' CNUM='` + 10 boşluk, `' INSERT CUSTOMER'`, `' SQLCODE=-00803'`; (2) 99 bayt `COMMAREA=` + tamponun ilk 90 baytı (dönüş kodu `90` yazılmış halde) | LGSTSQ'nun kuyruk kaydı |
| 01-04 | `90`; numara değişmedi; `IDENTITY_VAL_LOCAL` çağrılmadı; LGACVS01 ve LGACDB02 çağrılmadı; iki mesaj 01-03'teki biçimde, `-00204` | LGSTSQ'nun kuyruk kaydı |
| 01-05 | `ABEND LGV0` komutuna ulaşıldı; LGACDB02 çağrılmadı; iki mesaj: (1) 85 bayt `' LGACVS01'`, `' CNUM=0000000011'`, `' Write file KSDSCUST'`, `' RESP=+00014'`, `' RESP2=+00002'`; (2) 99 bayt `COMMAREA=` + gelen 225 baytın ilk 90'ı, içinde dönüş kodu `80` ve numara `0000000011` | "geri alınır" (`[belge]`); çağıranın `80`'i görmesi (tahmin `belirsiz`); abend'in görevi bitirmesi |
| 01-06 | son `CA-RETURN-CODE = 00`; `CA-CUSTOMER-NUM = 0000000011`; `CUSTOMER` INSERT'i ve VSAM yazması yapıldı; `CUSTOMER_SECURE` INSERT'i çağrıldı; iki mesaj: (1) 71 bayt `' LGACDB02'`, `' CNUM='` + 10 boşluk, `' INSERT SECURITY'`, `' SQLCODE=-00204'`; (2) 99 bayt `COMMAREA=` + `02ACUS` `98` `0000000011` + şifre (32) + `0000` + `N` + 35 boşluk (B5) | kuyruğa giren 81 bayt (LGSTSQ) |
| 01-07 | `00`; `CA-CUSTOMER-NUM = 0000000011`; INSERT'e 11 verildi (müşteri 1'e değil); geri kalanı 01-01 | — |
| 01-08 | `00`; numara 11; INSERT'e ve VSAM kaydına boş ad ve soyad verildi; geri kalanı 01-01 | — |
| 01-09 | **puanlanmaz** (tahmin `belirsiz`); gerçek sonuç yazılır | hepsi |
| 01-10 | `ABEND LGCA`; LGACDB01 çağrılmadı; tek mesaj, 45 bayt `' LGACUS01'` + `' NO COMMAREA RECEIVED'`; COMMAREA dökümü gönderilmedi | — |
| 01-11 | iki durumda da `98`; LGACDB01 çağrılmadı; mesaj yok; tamponun 91-93. baytları `000` (B1) | — |
| 01-12 | iki durumda da 7-8. baytlar `98` (B1); 91-93. baytlar `000`; LGACDB01 çağrılmadı; mesaj yok; 1-6. baytlar değişmedi; `EIBCALEN = 7`'de 7. bayt `9` | — |
| 01-13 | `00` (B1); LGACDB01 `LENGTH(32500)` ile çağrıldı; INSERT'e verilen telefonlar ve e-posta boşluk; INSERT'e verilen ad ... posta kodu girdideki değerler; numara 11; `KSDSCUST` kaydında 91-93. bayt karşılığı `000`, 94-233 karşılığı boşluk | — |
| 01-14 | `00`; sonuç 01-01 ile aynı; `CA-REQUEST-ID` değişmedi | — |
| 01-15 | **çalıştırılmadı** (tahmin `belirsiz`) | hepsi |
| 01-16 | `ABEND LGCA`; sayaç ve SQL çağrılmadı; tek mesaj, 71 bayt `' LGACDB01'` + `' NO COMMAREA RECEIVED'` + 20 boşluk + `+00000` (B2) | — |
| 01-17 | `EIBCALEN` 89 ve 8: `98`; sayaç ve SQL çağrılmadı; mesaj yok; `CA-NUM-POLICIES`'e dokunulmadı. `EIBCALEN` 7 ve 1: 7-8. baytlar `98` (B1) | — |
| 01-18 | `90`; numara değişmedi; LGACVS01 ve LGACDB02 çağrılmadı; iki mesaj: (1) 01-03'teki biçim, `-00803`; (2) `COMMAREA=` + tamponun ilk 90 baytı | `< 91` dalının çalışması (iki dal aynı metni verir) |
| 01-19 | `00`; `CA-CUSTOMER-NUM = 0000000011`; `CA-NUM-POLICIES = 777` (değişmedi); LGACVS01 kaydında `777`; zincirin geri kalanı 01-01 gibi | — |
| 01-20 | `ABEND LGCA`; SQL çağrılmadı; tek mesaj, 71 bayt `' LGACDB02'` + `' NO COMMAREA RECEIVED'` + 20 boşluk + `+00000` (B2) | — |
| 01-21 | `D2-RETURN-CODE = 99`; INSERT çağrılmadı; mesaj yok | — |
| 01-22 | iki durumda da `D2-RETURN-CODE = 98`; iki mesaj: (1) 71 bayt `' LGACDB02'`, `' CNUM='` + 10 boşluk, `' INSERT SECURITY'`, `' SQLCODE=-00803'`; (2) `COMMAREA=` + `D2` tamponunun ilk 90 baytı (`EIBCALEN = 55`'te 55 bayt + boşluk, aynı metin) | `< 91` dalının çalışması |
| 01-23 | **çalıştırılmadı** (tahmin `belirsiz`) | hepsi |
| 01-24 | **çalıştırılmadı** (tahmin `belirsiz`) | hepsi |
| 01-25 | `ABEND LGV0` komutuna ulaşıldı; iki mesaj: (1) 85 bayt `' LGACVS01'`, `' CNUM=0000000011'`, `' Write file KSDSCUST'`, `' RESP=+00014'`, `' RESP2=+00002'`; (2) `COMMAREA=` + gelen baytların ilk 90'ı (dönüş kodu `80` yazılmış halde) | çağıranın `80`'i görmesi (tahmin `belirsiz`); `< 91` dalı |

Bunların dışında bir iddia ortaya çıkarsa (düzenek hatası, kural boşluğu), sonuç bölümünde "çalıştırma sırasında eklenen" diye ayrıca yazılır.

## Sonuç

Çalıştırma: 2026-10-13, 12:20, **tek sefer**: 30 durum, her biri ayrı süreçte, hepsi `exit=0`, `stderr` boş. Karşılaştırma betiği çalıştırmadan sonra bir kez
çalıştı: **274 kontrol, 0 başarısız**. Çalıştırmadan sonra ne tahmin dosyasına ne kurallara ne de düzeneğe dokunuldu (aşağıdaki "Düzenek düzeltmeleri" hepsi çalıştırmadan öncedir).

Bütün karşılaştırmalar `runs/lgacus01-karsilastir.sh` ile bayt bayt yapıldı; beklenen değerler yalnızca tahmin dosyasından geliyor. Betik satır kararını da kendisi üretir.

### Satır satır

| Test no | Durum | Gerçek sonuç | Karar |
|---|---|---|---|
| 01-01 | C0101 | `CA-RETURN-CODE = 00`; `CA-CUSTOMER-NUM = 0000000011`; `CA-NUM-POLICIES = 000`; dokuz alan, istek kimliği ve `CA-POLICY-DATA` değişmedi. Çağrı sırası LGACDB01 → sayaç → numaralı INSERT → LGACVS01 → LGACDB02; LGSTSQ'ya mesaj gitmedi. INSERT'e 11 ve dokuz değer sütun sırasıyla verildi (cep ve ev telefonu doğru alanda). VSAM anahtarı `0000000011`; 225 baytlık kayıt tamponun 9-233. baytlarıyla aynı (son 8 bayt boşluk). `CUSTOMER_SECURE` INSERT'ine (11, `5732fec8...`, `N`, 0) verildi. | **tuttu** (36 kontrol) |
| 01-02 | C0102, C0102B | C0102: `00`; `CA-CUSTOMER-NUM = 0001000001`; mesaj yok; `DEFAULT` INSERT ve `IDENTITY_VAL_LOCAL` çağrıldı, numaralı INSERT çağrılmadı; VSAM anahtarı ve `CUSTOMER_SECURE` numarası 1000001. C0102B: `SET` `-204` verdi, program kontrol etmedi: `00`, mesaj yok, LGACVS01 ve LGACDB02 çağrıldı, `CA-CUSTOMER-NUM = 0000000000` | **tuttu** (22) |
| 01-03 | C0103 | `90`; `CA-CUSTOMER-NUM = 0000000000`; `CA-NUM-POLICIES = 000`; yalnız LGACDB01'e LINK (LGACVS01 ve LGACDB02 yok); iki mesaj: 71 bayt `... LGACDB01 CNUM=` + 10 boşluk + ` INSERT CUSTOMER SQLCODE=-00803`; 99 bayt `COMMAREA=01ACUS90...` (ilk 90 bayt) | **tuttu** (9) |
| 01-04 | C0104 | `90`; numara değişmedi; `IDENTITY_VAL_LOCAL` çağrılmadı; yalnız LGACDB01 LINK'i; aynı iki mesaj biçimi, `-00204` | **tuttu** (9) |
| 01-05 | C0105 | `ABEND LGV0` komutuna ulaşıldı; LGACDB02 çağrılmadı, `CUSTOMER_SECURE` INSERT'i yok; iki mesaj: 85 bayt `... LGACVS01 CNUM=0000000011 Write file KSDSCUST RESP=+00014 RESP2=+00002`; 99 bayt `COMMAREA=01ACUS800000000011...` (dönüş kodu `80`, numara 11) | **tuttu** (8) |
| 01-06 | C0106 | son `CA-RETURN-CODE = 00`; `CA-CUSTOMER-NUM = 0000000011`; INSERT, VSAM yazması ve `CUSTOMER_SECURE` INSERT'i çağrıldı (`-204`); iki mesaj: 71 bayt `... LGACDB02 CNUM=` + 10 boşluk + ` INSERT SECURITY SQLCODE=-00204`; 99 bayt `COMMAREA=02ACUS98` + `0000000011` + şifre + `0000N` + 35 boşluk (B5) | **tuttu** (10) |
| 01-07 | C0107 | `00`; girdideki numara 1 yok sayıldı, `CA-CUSTOMER-NUM = 0000000011`; INSERT'e 11 verildi; geri kalanı 01-01 gibi | **tuttu** (23) |
| 01-08 | C0108 | `00`; numara 11; INSERT'e ve VSAM kaydına boş ad ve soyad verildi; geri kalanı 01-01 gibi | **tuttu** (22) |
| 01-09 | C0109 | Çalıştırıldı, **puanlanmadı** (tahmin `belirsiz`). Düzenek `CA-DOB` boşluğunu kabul etti (`00`, INSERT'e 10 boşluk gitti). Bu Db2'nin `DATE` sütununa boşluğu kabul edip etmeyeceğini söylemez. | **belirsiz** |
| 01-10 | C0110 | `ABEND LGCA`; LGACDB01 çağrılmadı; tek mesaj, 45 bayt `... LGACUS01 NO COMMAREA RECEIVED`; COMMAREA dökümü gönderilmedi | **tuttu** (5) |
| 01-11 | C0111A (89), C0111B (8) | ikisinde de `98`; LGACDB01 çağrılmadı; mesaj yok; tamponun 91-93. baytları `000` (önce `777`) | **tuttu** (8) |
| 01-12 | C0112A (7), C0112B (1) | ikisinde de tamponun 7-8. baytları `98`; 91-93. baytlar `000`; LGACDB01 çağrılmadı; mesaj yok; 1-6. baytlar değişmedi; `EIBCALEN = 7`'de 7. bayt `9` | **tuttu** (11) |
| 01-13 | C0113 | `00`; LGACDB01 `LENGTH(32500)` ile çağrıldı; INSERT'e ad ... posta kodu girdideki değerlerle, telefonlar ve e-posta boşlukla verildi; VSAM kaydında 91-93. bayt karşılığı `000`, 94-233 karşılığı boşluk; numara 11 | **tuttu** (23) |
| 01-14 | C0114 | `00`; sonuç 01-01 ile aynı; `CA-REQUEST-ID` (boşluk) değişmedi | **tuttu** (24) |
| 01-15 | çalıştırılmadı | `CALL`'un hatası (modül yok) CICS'in `LINK` hatasıyla aynı değil; bir şey ölçmezdi | **belirsiz / ölçülemedi** (tahmin zaten belirsizdi) |
| 01-16 | D0116 | `ABEND LGCA`; sayaç ve SQL çağrılmadı; tek mesaj, 71 bayt `... LGACDB01 NO COMMAREA RECEIVED` + 20 boşluk + `+00000` (B2) | **tuttu** (6) |
| 01-17 | D0117A (89), B (8), C (7), D (1) | A, B: `98`; sayaç ve SQL çağrılmadı; mesaj yok; `CA-NUM-POLICIES`'e dokunulmadı (boşluk kaldı). C, D: tamponun 7-8. baytları `98` | **tuttu** (14) |
| 01-18 | D0118 | `90`; numara değişmedi; LGACVS01 ve LGACDB02 çağrılmadı; iki mesaj: 71 bayt (`-00803`), 99 bayt `COMMAREA=` + ilk 90 bayt | **tuttu** (8) |
| 01-19 | D0119 | `00`; `CA-CUSTOMER-NUM = 0000000011`; `CA-NUM-POLICIES = 777` (değişmedi); VSAM kaydında `777`; LGACVS01 ve LGACDB02 çağrıldı | **tuttu** (6) |
| 01-20 | D0120 | `ABEND LGCA`; SQL çağrılmadı; tek mesaj, 71 bayt `... LGACDB02 NO COMMAREA RECEIVED` + 20 boşluk + `+00000` | **tuttu** (5) |
| 01-21 | D0121 | `D2-RETURN-CODE = 99`; INSERT çağrılmadı; mesaj yok | **tuttu** (3) |
| 01-22 | D0122A (55), D0122B (90) | ikisinde de `D2-RETURN-CODE = 98`; iki mesaj: 71 bayt (` INSERT SECURITY SQLCODE=-00803`), 99 bayt `COMMAREA=02ACUS980000000001` + şifre + `0000N` + 35 boşluk | **tuttu** (12) |
| 01-23 | çalıştırılmadı | COMMAREA dışı okumanın sonucu kodda tanımlı değil; düzenekte tampon her zaman 32500 bayt olduğundan çıkacak sonuç anlamsız olurdu | **belirsiz / ölçülemedi** (tahmin zaten belirsizdi) |
| 01-24 | çalıştırılmadı | aynı neden | **belirsiz / ölçülemedi** (tahmin zaten belirsizdi) |
| 01-25 | D0125A (18), D0125B (90) | ikisinde de `ABEND LGV0` komutuna ulaşıldı; iki mesaj: 85 bayt (` CNUM=0000000011`, `RESP=+00014`, `RESP2=+00002`), 99 bayt `COMMAREA=` + gelen baytların ilk 90'ı (dönüş kodu `80`; `EIBCALEN = 18`'de 18 bayt + boşluk) | **tuttu** (10) |

### Özet

| | Satır |
|---|---|
| tuttu | 21 |
| tutmadı | **0** |
| belirsiz (puanlanmadı) | 4 (01-09, 01-15, 01-23, 01-24) |

Puanlanan 21 satırın 21'i tuttu. Toplam 274 kontrolün hepsi geçti. Doğruluk hesabı `tests/results/prediction-accuracy.md`'dedir.

### Önkoşulların düzenekte doğrulanması

- **B2** (`SQLCODE` 0 ile başlar): D0116'da mesajda `+00000` görüldü; `SQLCA`'ya hiçbir stub dokunmadan.
- **B5** (`VALUE`'su olmayan alanlar boşluk): C0106'da `D2-CUSTSECR-DATA`'nın ilk 35 baytı boşluk çıktı.
- **B1**: C0111-C0113 ve D0117, `EIBCALEN` ötesindeki yazmaların (`98`, `000`) tamponda göründüğünü gösterdi.
- **B6, B7**: stub'lar sürücünün verdiği değerleri (1000001, 11) döndürür; bunlar düzeneğin kararıdır ve ölçülen COBOL'un onları taşımasıdır.

### Puanlanmayan iddialar (ölçülemedi)

Bunlar tuttu satırlarının içinde yazılı ama düzenek göstermediği için puanlanmadı:

- **LGSTSQ'nun kuyruğa yazdığı kayıt** (01-03 ... 01-06, 01-10, 01-16, 01-18, 01-20, 01-22, 01-25): TD `CSMT` ve TS `GENAERRS`'e yazma, kayıt uzunluğu 104, `WRITE-MSG`'nin 95 bayt olması, ikinci mesajdan yalnızca ilk 81 baytın girmesi. Ölçülen, LGSTSQ'ya **ne verildiği**.
- **Tarih ve saat:** `ASKTIME`/`FORMATTIME` `DISPLAY` oldu; mesajların ilk 15 baytı boş.
- **Gerçek Db2 ve VSAM:** satırların gerçekten oluşması, kimlik değerinin Db2'den gelmesi, birincil anahtar ihlalinde `-803` dönmesi (düzeneğin varsayımı), `DATE` sütununa değer yazılabilmesi, VSAM'ın `RESP` değerleri (14 ve 2'yi ben seçtim).
- **01-05'in "geri alınır" iddiası** (`[belge]` `base/Testing.md:29-30`) ve "çağıranın `80`'i görmesi" (tahmin `belirsiz`). Düzenekte bayrakla görev sonlanır ve sürücü tamponda `80`'i görür (C0105); bu CICS'in davranışı değil, düzeneğin kuralıdır.
- **ABEND'in görevi sonlandırması:** düzenekte `DISPLAY` + bayrak + `GOBACK` zinciri. "LGACDB02 çağrılmaz" (01-05) iddiasının geçmesi bu kurala bağlıdır: ölçülen, ABEND'e kadar gidilen yol ve `LGV0` kodudur.
- **`< 91` ile `ELSE` dalının ayrımı** (01-18, 01-22, 01-25): iki dal bu durumlarda aynı metni üretir. Hangi dalın çalıştığı çıktıdan görünmez (kod okuması: `EIBCALEN < 91`).
- **Ölü kod:** LGACUS01'in COMMAREA dökümü (`lgacus01.cbl:164-178`, tahmin Kod gözlemleri 15) **derlendi, hiç çalışmadı**.
- **Kod gözlemi 16'nın uç durumları** (aynı süreçte tekrar çağrıda eşiklerin 180/270 olması, `LGAC-NCS = 'NO'` kalıcılığı, `EM-VARIABLE` uç durumu): her durum ayrı süreçte çalıştığı için denenmedi.

## Bu sonuç ne söyler, ne söylemez

Söylediği: LGACUS01, LGACDB01, LGACDB02 ve LGACVS01'in **COBOL mantığı** (uzunluk kontrolleri, dönüş kodları, sayaç hatasının sessizce geçilmesi, `IDENTITY_VAL_LOCAL`'in kontrol edilmemesi,
girdideki müşteri numarasının yok sayılması, alan eşlemesi ve boşlukla tamamlama, COMMAREA dışına yazma, VSAM ve `CUSTOMER_SECURE` hatalarının çağırana taşınması ya da taşınmaması, hata
mesajı içerikleri) tahminlerin okuduğu gibi çalışıyor.

Söylemediği: Veri satırlarının (01-01, 01-07, 01-08, 01-13, 01-14, 01-19) **değerleri** düzeneğin stub'larına verilen girdiden geliyor. Bu satırlar Db2'nin ve VSAM'ın ne yaptığını değil, COBOL'un
INSERT ve WRITE'a **doğru girdiyi, doğru yerde ve doğru dolguyla** verdiğini ölçer. Db2'nin gerçek davranışı (birincil anahtar, `DATE` biçimi, NULL, kilit), VSAM'ın gerçek davranışı, CICS'in
ABEND ve geri alma davranışı ölçülmedi. Tahminin B1-B7 ön koşulları düzeneğin **kararı**; gerçek sistemde farklı çıkabilirler. Yani "21/21 tuttu", gerçek sistemde 21/21 olacağı anlamına gelmez.

## Düzenek düzeltmeleri

Hepsi **çalıştırmadan önce** yapıldı; hiçbiri çalıştırma çıktısına bakılarak yapılmadı (çıktıya, karşılaştırma betiği çalışana kadar bakılmadı).

1. **Dönüşüm betiği:** ilk çalıştırmada `IDENTITY_VAL_LOCAL` `DISPLAY` metni 72. sütunu aştığı için betik durdu. Uzun metni art arda gelen literallere bölecek şekilde düzelttim; dönüşümü yeniden ürettim.
2. **Karşılaştırma betiği (çalıştırmadan önce okurken bulundu):** (a) `get` işlevi, stub'ların `POSTCODE=[...]` gibi satırlarını `AFTER` satırlarıyla karıştırırdı; yalnız `AFTER` satırlarını okuyacak şekilde düzelttim. (b) `CUSTOMER_SECURE` numarası için beklenen biçim yanlıştı (`+00` + 9 hane); `PIC +9(9)` çıktısına göre düzelttim. (c) `tr -d '\r'`'de gerçek bir satır başı karakteri kalmıştı; düzelttim.
3. **Betiğin kendini sınaması:** karşılaştırma betiğinin bir şey ölçtüğünü, kopyalarında beklenen değeri bilerek bozarak gördüm: yanlış `K` (12) 30 kontrolü, bozuk bir mesaj biçimi 3 kontrolü, yanlış bir dönüş kodu 1 kontrolü başarısız yaptı.

Hiçbir düzeltme tahmini ya da programların mantığını değiştirmedi. Değerlendirme kuralları (yukarıdaki bölüm) ilk commit'ten beri değişmedi; yalnızca bu "Sonuç" bölümü sonradan eklendi.

## Bug kayıtları

Tutmayan satır olmadığı için Bug Tracker'a **bug etiketli kayıt açılmadı**. Puanlanmayan dört satır (01-09, 01-15, 01-23, 01-24) tahminin kendi `belirsiz` hücreleridir, bug değildir.
