# LGIPDB01 çalıştırması: EXEC SQL'i sahte SQLCODE ile değiştirip tahminle karşılaştırma

| Alan | Değer |
|---|---|
| Tarih | 2026-10-17 |
| Sorumlu | Oğuz |
| Dal | `oguz/gnucobol-sql` |
| Durum | **Yöntem denemesi ve karşılaştırma** (tahmin doğruluğu tablosuna yeni satır **eklemez**, aşağıya bak) |
| Karşılaştırılan tahmin | `tests/predictions/05-motor-inquire.md` (yalnızca `SQLCODE`'a bağlı satırlar: 05-01, 05-03, 05-06) |
| GnuCOBOL | 3.2.0, MSYS2 ucrt64 |
| Ham çıktı | `runs/lgipdb01-cikti.txt` |
| Karşılaştırma betiği | `runs/lgipdb01-karsilastir.sh` (çıktısı: `runs/lgipdb01-karsilastir-cikti.txt`) |
| Çalıştırma betiği | `runs/lgipdb01-calistir.sh` |
| Dönüşüm betiği | `convert-lgipdb01.py` |

> **Bu dosyanın ilk sürümü, hiçbir şey derlenmeden ya da çalıştırılmadan önce commit'lendi.** "Ölçüm kuralı: kontrol", "Değerlendirme kuralları",
> "Düzenek" ve "Çalıştırılacak durumlar" bölümleri o sürümdedir. "Sonuç" bölümleri çalıştırmadan sonra eklenmiştir. Kuralların çalıştırmadan
> önce yazıldığını `git log -- tests/gnucobol/runs/lgipdb01.md` gösterir.

## Bu kayıt ne yapar, ne yapmaz

- **Amaç:** İçinde `EXEC SQL` olan bir programı (LGIPDB01) GnuCOBOL'da çalıştırmak. Her `EXEC SQL` deyimi, `SQLCODE`'u test senaryosunun verdiği değere eşitleyen sahte bir parçayla
  değişir (`docs/adr/0004-legacy-test-harness.md`, karar 5). Böylece "Db2 şu `SQLCODE`'u döndürürse program hangi dönüş kodunu verir?" sorusu ölçülür.
- **Tahmin doğruluğuna etkisi yok.** 05-03 (`SQLCODE = 100`) ve 05-06 (`-904`) satırları LGIPOL01 zincirinde zaten ölçüldü (görev #28, `runs/lgipol01.md`). Aynı satırlar ikinci kez
  puanlanırsa doğruluk yüzdesi şişer. Bu kayıt onları **yeniden saymaz**; aynı iddiayı doğrudan LGIPDB01'de ve çok sayıda `SQLCODE` değeriyle sınar.
- **Yeni olan:** (1) tek bir sahte parçayla **bütün** `EXEC SQL` deyimlerinin (5 `SELECT`, `OPEN`, `FETCH`, `CLOSE`) değiştirilmesi; (2) `SQLCODE` taraması: 0, 100 ve dokuz farklı hata/uyarı kodu;
  (3) motor dışı kolların (hayat sigortası, konut, ticari) `SQLCODE`'a tepkisi, **bilgi için**.

## Ölçüm kuralı: kontrol

- Tahmin dosyası **çalıştırmadan önce** `main`'deydi: `206f9ac` (2026-10-13, #105). Dosya o günden beri değişmedi (`git log origin/main -- tests/predictions/05-motor-inquire.md` tek commit gösterir).
- Çalıştırma anındaki `main`: `13d0e97`. Tahminin dayandığı kaynak commit `99e4be8`; `git diff 99e4be8 origin/main -- base/` **boş**, tahmindeki satır numaraları geçerli.
- Bağımlılık #24 (motor tahmin tabloları) `main`'de. Bağımlılık #28 (LGIPOL01 ölçümü) `oguz/gnucobol-3` dalında, henüz `main`'de değil; bu kayıt ona **dayanmaz**, düzeneği baştan kurar
  (yalnızca değişmemiş kopyalar ve `stubs/lgstsq.cbl`, `stubs/sqlca.cpy` ortaktır).
- Tahmin dosyasını çalıştırmadan önce okudum. Sonuçlar tahminlerden önce kimseyle paylaşılmadı; tahmin dosyasına çalıştırmadan sonra dokunulmayacak.

## Değerlendirme kuralları (çalıştırmadan önce belirlendi)

1. **Sahte parça sonuç uydurmaz, yalnızca `SQLCODE`'u kurar.** Satırlar ve ana bilgisayar değişkenleri yerinde kalır (Db2'nin hata durumunda yaptığı gibi). Bu yüzden `SQLCODE = 0` iken COMMAREA'ya
   giden değerler Db2'nin değil, `INITIALIZE`'ın (sıfır/boşluk) değeridir: **veri hücreleri bu kayıtta puanlanmaz.**
2. **`SQLCODE`'un kendisi testin girdisidir**, programın ya da Db2'nin sonucu değil. Sonuç, "Db2 bu kodu döndürürse" biçiminde koşulludur (ADR 0004, karar 5; tahminin ön koşulu B4).
3. Her durum, tahminin hangi satırının **iddiasına** bağlandığıyla sınanır:
   - `SQLCODE = 0` → 05-01'in akış iddiaları: `CA-RETURN-CODE = 00`; 178-182. baytlar `FINAL`; 183-32500. baytlar değişmedi; istek no, müşteri no, poliçe no değişmedi; LGSTSQ'ya mesaj gitmedi;
     `SELECT` bir kez çalıştı.
   - `SQLCODE = 100` → 05-03: `CA-RETURN-CODE = 01`; 29-32500. baytlar boşluk kaldı; `FINAL` yok; LGSTSQ'ya mesaj gitmedi.
   - `SQLCODE` 0 ve 100 dışında bir değer `N` → 05-06 ("0 ve 100 dışında bir `SQLCODE` (örn. `-904`)", Kod gözlemleri 6: "Diğer SQLCODE'lar → `90`"): `CA-RETURN-CODE = 90`; 29-32500. baytlar
     boşluk kaldı; `FINAL` yok; LGSTSQ'ya **iki** mesaj: (1) 87 bayt: 15 boşluk + ` LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR ` + ␣×2 + ` SQLCODE=` + `N` (`PIC +9(5)`: işaret ve
     5 hane); (2) 99 bayt: `COMMAREA=` + COMMAREA'nın ilk 90 baytı (`01IMOT90` + `0000000002` + `0000000001` + ␣×62). Tahmin yalnızca `-904`'ü adıyla yazar; diğer kodlar **aynı kuralın uygulamasıdır**.
     `-305`, 05-18'in "göstergesiz sütunda NULL" girdisi değildir; burada `SQLCODE` doğrudan verilir. 05-18'in kendisi belirsiz kalır ve puanlanmaz.
4. Satırın (durumun) kararı: puanlanan iddialardan biri tutmadıysa **tutmadı**; hepsi tuttuysa **tuttu**.
5. **Ölçülemedi** (puanlanmaz): Db2'nin bu kodu gerçekte ne zaman döndüreceği, ana bilgisayar değişkenlerinin hata durumundaki gerçek davranışı, imleç davranışı, `NULL`/`-305`, `SQLSTATE`,
   `EXEC SQL` ön derleyicisinin yaptıkları, LGSTSQ'nun kuyruğa yazdığı kayıt, gerçek tarih ve saat (mesajların ilk 15 baytı boşluk), `< 91`/`ELSE` dalı (iki durum aynı metni üretir).
6. **Bilgi için** çalıştırılanlar (motor dışı kollar): gerçek sonuç yazılır, "tuttu/tutmadı" denmez. Bu kollar için `main`'de motor sorgusu dışında puanlanacak tahmin bu görevin kapsamında değil
   (hayat sigortası sorgusu tahmini henüz `main`'de değil).
7. Her durum ayrı süreçte çalışır (`WS-REQUIRED-CA-LEN` süreç içinde korunur: tahmin Kod gözlemleri 10).

**Sayılan şey:** durum (kod) başına karar ve iddia başına kontrol. Doğruluk yüzdesi **hesaplanmaz** (yukarıdaki gerekçe).

## Düzenek

| Parça | Dosya | Ne yapar |
|---|---|---|
| LGIPDB01 (kopya) | `work/lgipdb01-sql.cbl` | 28 `EXEC` deyimi betikle (`convert-lgipdb01.py`) değişir: 11 `EXEC SQL` deyimi sahte parçaya gider (5 `SELECT`, 2 `OPEN`, 2 `FETCH`, 2 `CLOSE`), 2 `DECLARE` kaldırılır, 3 `INCLUDE` → `COPY`, 12 `EXEC CICS` (6 `RETURN`, `ABEND`, `ASKTIME`, `FORMATTIME`, 3 `LINK LGSTSQ`) çevrilir |
| Orijinal kopya | `work/lgipdb01.cbl` | `base/src/lgipdb01.cbl`'den değişmemiş kopya (`cmp` ile doğrulanır) |
| Sahte Db2 | `stubs/db2stub.cbl` | `SQLCODE`'u sürücünün verdiği değere eşitler (`DB2STUB-SQLCODE`), başka hiçbir şeye dokunmaz |
| LGSTSQ stub'ı | `stubs/lgstsq.cbl` | Mesajı ve uzunluğunu ekrana yazar (önceki ölçümlerle aynı) |
| SQLCA | `stubs/sqlca.cpy` | Önceki ölçümlerle aynı |
| Sürücü | `work/drv-lgipdb01.cbl` | CICS'in rolünü oynar; **LGIPDB01'i doğrudan** çağırır; `<istek kodu> <SQLCODE> [EIBCALEN]` alır |

- **Neden doğrudan çağrı:** LGIPOL01 `SQLCODE`'a hiç bakmaz; yalnızca `LINK` eder (`lgipol01.cbl:91-96`). SQL davranışı LGIPDB01'dedir. Zincir #28'de ölçüldü.
- **B1:** sürücü tek 32500 baytlık tamponu kurar, `EIBCALEN` (varsayılan 32500) ötesini boşlukla doldurur. **B2:** `SQLCA` kopyasında `VALUE` yok, `SQLCODE` 0 ile başlar. **B3:** GnuCOBOL varsayılanı.
  **B4:** `SQLCODE`'u sürücü kurar.
- Dönüş kodu önceden `77` (programın hiç üretmediği değer; LGIPDB01 `99` üretir). Müşteri `0000000002`, poliçe `0000000001` (`db2cre.jcl:945-946`; yalnızca girdi, tablo yok).
- Kütüphaneler `build/sql/` altına derlenir (`build/` zaten `.gitignore`'da).

## Çalıştırılacak durumlar

Hepsi `<istek kodu> <SQLCODE>` olarak sürücüye verilir; `EIBCALEN = 32500`.

| Grup | Durumlar | Karşılaştırılan |
|---|---|---|
| Motor, `SQLCODE = 0` | `01IMOT 0` | 05-01'in akış iddiaları |
| Motor, `SQLCODE = 100` | `01IMOT 100` | 05-03 |
| Motor, hata/uyarı kodları | `01IMOT -904`, `-305`, `-803`, `-811`, `-911`, `-913`, `-922`, `-30081`, `+304` | 05-06'nın kuralı (`-904` tahminin adıyla yazdığı kod; geri kalanı aynı kuralın uygulaması) |
| Motor dışı kollar (bilgi için) | `01IEND`, `01IHOU`, `01ICOM`: 0, 100, -904. `03ICOM` (imleç): 0, 100, -904 | Puanlanmaz |

Toplam 11 + 12 = 23 durum. `01IXXX` ve `EIBCALEN` kontrolleri (05-08, 05-10, 05-12 …) `SQL` içermediği için bu kaydın konusu değil.

## Sonuç

(Çalıştırmadan sonra doldurulacak.)
