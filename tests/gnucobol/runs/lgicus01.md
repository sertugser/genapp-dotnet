# LGICUS01 çalıştırması: GnuCOBOL sonuçları ve test ekibinin tahminleri

| Alan | Değer |
|---|---|
| Tarih | 2026-10-10 |
| Sorumlu | Oğuz |
| Dal | `oguz/gnucobol-lgicus01` |
| Durum | **Ölçüm çalıştırması** (tahmin doğruluğuna sayılır) |
| Karşılaştırılan tahmin | `tests/predictions/02-customer-inquire.md` (17 satır, 02-01 ... 02-17) |
| GnuCOBOL | 3.2.0, MSYS2 ucrt64 |
| Ham çıktı | `runs/lgicus01-cikti.txt` |
| Karşılaştırma betiği | `runs/lgicus01-karsilastir.sh` (113 kontrol) |

## Ölçüm kuralı: kontrol

- Tahmin dosyası **çalıştırmadan önce** `main`'deydi: ilk sürüm `83b7710` (2026-10-03), son hâli `236541e` (2026-10-05, #88).
  Dosya 5 Ekim'den beri değişmedi (`git log 236541e..origin/main -- tests/predictions` boş).
- Tahminin dayandığı kaynak commit `61c635f`. `base/` o commit ile çalıştırma anındaki `main` (`2723dad`) arasında
  değişmedi (`git diff 61c635f origin/main -- base/` boş). Tahmindeki satır numaraları geçerli.
- Sonuçlar, tahminlerden önce kimseyle paylaşılmadı. Tahmin dosyasına çalıştırmadan sonra dokunulmadı.
- Tahmin dosyasını çalıştırmadan önce okudum. Düzeneği dosyanın **kendi** ön koşullarına (B1-B4) göre kurdum; bunun
  dışında düzeneği tahmine uydurmadım. Düzeneğin kurulmasında bulduğum hatalar "Düzenek düzeltmeleri" bölümünde.

## Değerlendirme kuralları (çalıştırmadan önce belirlendi)

Her satır, tahminin **ölçülebilir iddialarına** ayrıldı (dönüş kodu, veri, yan etki). Sonra:

- İddia, düzeneğin gösterebildiği bir şeyse ve çıktıda bayt bayt tutuyorsa: **tuttu**. Tutmuyorsa: **tutmadı**.
- Tahminin kendisi `belirsiz` dediği hücreler **puanlanmaz**. Gerçek sonucu yazılır, ama "tuttu/tutmadı" denmez; tahmin bir iddiada bulunmamıştır.
- Düzeneğin çalıştırmadığı bir şeye dayanan iddialar (LGSTSQ'nun kuyruğa yazdığı kayıt, gerçek tarih ve saat, gerçek Db2 ve CICS
  davranışı) **ölçülemedi** diye ayrı yazılır ve puanlanmaz.
- Satırın kararı: puanlanan iddialardan biri tutmadıysa **tutmadı**; hepsi tuttuysa **tuttu**; puanlanan iddia yoksa **belirsiz**.

## Düzenek

Yöntem `tests/gnucobol/README.md`'deki gibi; farklar aşağıda. Programlar ayrı modüller, sürücü çalıştırılabilir dosya.

| Parça | Dosya | Ne yapar |
|---|---|---|
| LGICUS01 (kopya) | `work/lgicus01-gc.cbl` | 9 `EXEC CICS` komutu değişti |
| LGICDB01 (kopya) | `work/lgicdb01-gc.cbl` | 9 `EXEC CICS`, 2 `EXEC SQL INCLUDE`, 1 `EXEC SQL SELECT` değişti; `PROCESS SQL` satırı silindi |
| Db2 `SELECT` stub'ı | `stubs/db2cust.cbl` | `CUSTOMER` tablosunu taklit eder (aşağıda) |
| SQLCA | `stubs/sqlca.cpy` | En küçük SQLCA, Db2'ninkiyle aynı alan adları |
| LGSTSQ stub'ı | `stubs/lgstsq.cbl` | Mesajı ve uzunluğunu ekrana yazar (H02'deki gibi) |
| Sürücü | `work/drv-lgicus01.cbl` | CICS ve Db2'nin rolünü oynar; her durum ayrı süreçte |

Orijinaller `work/lgicus01.cbl`, `work/lgicdb01.cbl`, `work/lgpolicy.cpy` (değişmemiş kopya). Değişiklikler:
`diff <(tr -d '\r' < work/lgicus01.cbl) <(tr -d '\r' < work/lgicus01-gc.cbl)` (LGICDB01 için aynısı). Dönüşümü elle değil, betikle yaptım
(satır sonu ve mantık satırları aynen korundu). Her iki kopyada `grep -c "^ *EXEC "` sonucu `0`.

### Tahminin ön koşulları nasıl sağlandı

| Kod | Tahminin istediği | Düzenekte |
|---|---|---|
| B1 | COMMAREA her LINK'te 32500 baytlık tek tamponun adresiyle geçer; `EIBCALEN` yalnızca bildirilen uzunluk; tamponun `EIBCALEN` ötesi boşluk | Sürücü tek 32500 baytlık tamponu kurar, `EIBCALEN`'den sonrasını boşlukla doldurur. `CALL ... USING DFHCOMMAREA` aynı adresi geçirir. |
| B2 | `SQLCODE` 0 ile başlar | `SQLCA` kopyasında `VALUE` yok; GnuCOBOL sayısal alanı sıfırlar. **D0209 ile doğrulandı** (mesajda `+00000`). |
| B3 | Sayısal `MOVE`'da hane sayısına göre kesme | GnuCOBOL'un varsayılanı. **C0213 ile doğrulandı** (`1000000001` → müşteri 1 sorgulandı). |
| B4 | Hata kodunu ve veri durumunu düzenek kurar | Sürücü `DB2FORCE-SQLCODE` verir (0 = kullanma). Sıfırsa `stubs/db2cust.cbl` içindeki `CUSTOMER` tablosuna bakılır: yalnız **müşteri 1** var (`db2cre.jcl:437-446` değerleri); başka numara `SQLCODE 100` döner. |

Zincirde `LINK ... LENGTH(32500)` karşılığı: `EIBCALEN` tüm programlarda ortak (`EXTERNAL`) olduğundan, LGICUS01 `LINK` yerine
`MOVE 32500 TO EIBCALEN` → `CALL LGICDB01` → eski değeri geri yazar. Böylece LGICDB01 `EIBCALEN = 32500` görür (tahminin Kod gözlemi 3).
Bu **düzeneğin** bir kuralıdır, programın değil; H02'de gerek duymamıştım.

## Çalıştırılan durumlar

| Durum | Yol | `EIBCALEN` | Girdi |
|---|---|---|---|
| C0201 | sürücü → LGICUS01 → LGICDB01 | 32500 | müşteri 1 |
| C0202 | aynı | 32500 | müşteri 0 (tabloda yok) |
| C0203 | aynı | 32500 | müşteri 1, Db2 `-913` verir |
| C0204 | aynı | 32500 | müşteri 1, Db2 `-204` verir |
| C0205 | aynı | 0 | |
| C0206A, C0206B | aynı | 89, 8 | |
| C0207A, C0207B | aynı | 7, 1 | |
| C0208 | aynı | 90 | müşteri 1 |
| C0212 | aynı | 32500 | `CA-REQUEST-ID = 01AEND`, müşteri 1 |
| C0213 | aynı | 32500 | müşteri no `1000000001` |
| C0214 | aynı | 32500 | müşteri no alanı boşluk |
| D0209 | sürücü → **LGICDB01** | 0 | |
| D0210A, D0210B | LGICDB01 | 89, 8 | |
| D0211 | LGICDB01 | 233 | müşteri 1; `CA-NUM-POLICIES` önceden `777` |
| D0217 | LGICDB01 | 90 | müşteri 1, Db2 `-204` verir |

`-204`'ü ben seçtim: tahmin "0, 100, -913 dışında bir SQLCODE" diyor (B4). Tüm durumlarda `CA-REQUEST-ID = 01ICUS` (aksi yazılmadıkça),
dönüş kodu önceden `99`, `CA-NUM-POLICIES` önceden `777` (tampon `EIBCALEN`'den kısa değilse).

## Sonuç: satır satır

Bütün karşılaştırmalar `runs/lgicus01-karsilastir.sh` ile bayt bayt yapıldı (beklenen değerler yalnızca tahmin dosyasından).

| Test no | Durum | Gerçek sonuç | Karar |
|---|---|---|---|
| 02-01 | C0201 | `CA-RETURN-CODE = 00`; Andrew / Pandy / `34  ` / `PI101OO ` / `07799 123456`+8 boşluk (cep) / `01962 811234`+8 boşluk (ev) / `A.Pandy@beebhouse.com`+79 boşluk; `CA-NUM-POLICIES = 000`; istek no, müşteri no ve `CA-POLICY-DATA` değişmedi. Cep ve ev telefonu doğru alana düştü. | **tuttu** (`CA-DOB` puanlanmadı: tahmin belirsiz; düzeneğin tablosundaki değer `1950-07-11`) |
| 02-02 | C0202 | `01`; müşteri alanlarına dokunulmadı (boşluk kaldı); `CA-NUM-POLICIES = 000`; LGSTSQ'ya mesaj gitmedi | **tuttu** (alanlar belirsizdi) |
| 02-03 | C0203 | `01`; `CA-NUM-POLICIES = 000`; mesaj yok; alanlara dokunulmadı | **tuttu** (alanlar belirsizdi) |
| 02-04 | C0204 | `90`; LGSTSQ'ya iki mesaj: (1) 71 bayt `... LGICUS01 CNUM=0000000001`+16 boşluk+` SQLCODE=-00204`; (2) 99 bayt `COMMAREA=01ICUS900000000001`+72 boşluk | **tuttu** (alanlar belirsizdi) |
| 02-05 | C0205 | ABEND `LGCA` görüldü; LGICDB01 çağrılmadı (`SELECT` yok); LGSTSQ'ya tek mesaj, 45 bayt `... LGICUS01 NO COMMAREA RECEIVED`; COMMAREA dökümü gönderilmedi; tampon değişmedi | **tuttu** |
| 02-06 | C0206A (89), C0206B (8) | ikisinde de `98`; LGICDB01 çağrılmadı; mesaj yok; müşteri alanları boş; tamponun 91-93. baytları `000` (önce boşluk) | **tuttu** |
| 02-07 | C0207A (7), C0207B (1) | ikisinde de tamponun 7-8. baytları `98`; 91-93. baytlar `000`; LGICDB01 çağrılmadı; mesaj yok; 1-6. baytlar değişmedi; `EIBCALEN = 7`'de 7. bayt `9` | **tuttu** |
| 02-08 | C0208 | `00`; 90 baytlık alanlar (ad ... posta kodu) doldu; tamponda `CA-NUM-POLICIES = 000`, iki telefon ve e-posta 02-01'deki gibi | **tuttu** |
| 02-09 | D0209 | ABEND `LGCA`; SQL çalışmadı; LGSTSQ'ya tek mesaj, 71 bayt: `... LGICUS01 NO COMMAREA RECEIVED`+20 boşluk+`+00000`; COMMAREA dökümü yok | **tuttu** (`+00000` B2 ile) |
| 02-10 | D0210A (89), D0210B (8) | ikisinde de `98`; SQL çalışmadı; alanlar boş; `CA-NUM-POLICIES`'e dokunulmadı (boşluk kaldı) | **tuttu** |
| 02-11 | D0211 | `00`; dokuz alan 02-01'deki gibi; `CA-NUM-POLICIES` değişmedi (`777`) | **tuttu** |
| 02-12 | C0212 | `00`; sonuç 02-01 ile aynı | **tuttu** |
| 02-13 | C0213 | `00`; `SELECT` müşteri **1** ile çalıştı; alanlar 02-01'deki gibi; `CA-CUSTOMER-NUM` `1000000001` kaldı | **tuttu** (B3 ile) |
| 02-14 | C0214 | Müşteri no alanı boşluk: Db2'ye giden değer `0`, `SQLCODE 100`, dönüş kodu `01`, alanlara dokunulmadı | **belirsiz** (tahmin iddia etmiyordu). Bu, GnuCOBOL'un boşluğu `0` olarak taşımasıdır; başka derleyicide aynı olacağı söylenemez. |
| 02-15 | çalıştırılmadı | Düzenek bir NULL sütun ya da NULL için SQLCODE üretemez | **belirsiz / ölçülemedi** (tahmin zaten belirsizdi) |
| 02-16 | çalıştırılmadı | `CALL`'un hatası (modül yok) CICS'in `LINK` hatasıyla aynı değil; bir şey ölçmezdi | **belirsiz / ölçülemedi** (tahmin zaten belirsizdi) |
| 02-17 | D0217 | `90`; `CA-NUM-POLICIES`'e dokunulmadı; LGSTSQ'ya iki mesaj: (1) 02-04'tekiyle aynı 71 bayt; (2) `COMMAREA=` + gelen 90 bayt (99 bayt) | **tuttu** (alanlar belirsizdi) |

### Özet

| | Satır |
|---|---|
| tuttu | 14 (02-01 ... 02-13, 02-17) |
| tutmadı | **0** |
| belirsiz (puanlanmadı) | 3 (02-14, 02-15, 02-16) |

Puanlanan 14 satırın 14'ü tuttu. Toplam 113 bayt düzeyinde kontrolün hepsi geçti.

### Puanlanmayan iddialar (ölçülemedi)

Bunlar tuttu satırlarının içinde yazılı ama düzenek göstermediği için puanlanmadı:

- **LGSTSQ'nun kuyruğa yazdığı kayıt** (02-04, 02-17): TD `CSMT` ve TS `GENAERRS`'e yazma, kayıt uzunluğu 104, `WRITE-MSG`'nin 95 bayt olması,
  ikinci mesajdan yalnızca ilk 81 baytın girmesi. LGSTSQ gerçek programı çalışmadı, stub'ı çalıştı. Ölçülen, LGSTSQ'ya **ne verildiği**.
- **Tarih ve saat:** `ASKTIME`/`FORMATTIME` `DISPLAY` oldu; mesajlardaki ilk 15 bayt boşluk. Tahmin "tarih, saat" diyor.
- **`< 91` dalı** (02-17): çıktı `ELSE` dalıyla aynı metni verir (ikisi de `EIBCALEN = 90` iken aynı 90 baytı taşır). Hangi dalın çalıştığı çıktıdan görünmüyor;
  `EIBCALEN = 90 < 91` olduğu için `< 91` dalı çalışır (kod okuması, ölçüm değil).
- **ABEND'in görevi sonlandırması:** düzenekte `DISPLAY` + `GOBACK`. Ölçülen, ABEND'e kadar gidilen yol ve `LGCA` kodu.
- **LGICUS01'in COMMAREA dökümü** (`lgicus01.cbl:151-164`): tahmin ölü kod diyor (Kod gözlemleri 10). Bu iki `LGSTSQ` çağrısı **derlendi, hiç çalışmadı**.
- **Kod gözlemi 11'in uç durumu** (ABEND sonrası aynı `WORKING-STORAGE` ile yeniden çağrı): her durum ayrı süreçte çalıştığı için denenmedi.

## Bu sonuç ne söyler, ne söylemez

Söylediği: LGICUS01 ve LGICDB01'in **COBOL mantığı** (uzunluk kontrolleri, dönüş kodları, alan eşlemesi ve boşlukla tamamlama, COMMAREA dışına yazma,
hata mesajı içerikleri) tahminlerin okuduğu gibi çalışıyor. Test ekibinin kodu okuyarak yazdığı tahminlerin bu kısmı doğruydu.

Söylemediği: Veri satırlarının (02-01, 02-08, 02-11, 02-12, 02-13) **değerleri** düzeneğin Db2 stub'ından geliyor ve tahminin kaynağıyla aynı JCL'den
alındı. Bu satırlar Db2'nin ne döndürdüğünü değil, COBOL'un dönenleri COMMAREA'ya **doğru yere ve doğru dolgu ile** taşıdığını ölçer.
Db2'nin gerçek davranışı (NULL, kilit, `DATE` biçimi, `-913`'ün ne zaman döndüğü) ve CICS çalışma zamanı ölçülmedi. Tahminin B1-B4 ön koşulları
düzeneğin **kararı**; gerçek CICS ve Db2'de farklı çıkabilirler. Yani "14/14 tuttu", gerçek sistemde 14/14 olacağı anlamına gelmez.

## Düzenek düzeltmeleri (çalıştırma sırasında)

1. **Dönüşüm betiği (çalıştırmadan önce):** değiştirilen ilk satırlar fazladan girintili çıktı ve sabit biçimde 72. sütundan taşıyordu. Betiği düzeltip
   dosyaları yeniden ürettim; hiçbir şey çalıştırılmadan önceydi.
2. **Sürücünün `EIBCALEN` yazısı:** ilk çalıştırmada `EIBCALEN= 2500` yazdı (32500 olmalıydı). Nedeni yazdırma alanının resminin dar olmasıydı (`-(4)9`);
   programlara giden değer doğruydu. Resmi `-(5)9` yaptım ve **18 durumun hepsini yeniden çalıştırdım**. İki çıktı arasındaki tek fark bu satırdı
   (`diff` ile doğrulandı). Kayıtta ikinci çalıştırma var.
3. **Karşılaştırma betiği:** 99 baytlık ikinci mesaj için beklenen boşluk sayısını 70 yazmıştım (doğrusu 72); iki kontrol bu yüzden başarısız göründü. Betikteki
   hesabı düzelttim (tahmin dosyasındaki 99 bayt ve ilk 90 bayt bilgisine göre: 9 + 18 + 72 = 99). Programın sonucu değişmedi.

Hiçbir düzeltme tahmini ya da programın mantığını değiştirmedi.

## Bug kayıtları

Tutmayan satır olmadığı için Bug Tracker'a **bug etiketli kayıt açılmadı**. Puanlanmayan üç satır (02-14, 02-15, 02-16) tahminin kendi
`belirsiz` hücreleridir, bug değildir.
