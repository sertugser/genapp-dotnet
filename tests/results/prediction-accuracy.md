# Tahmin doğruluğu: ilk ölçüm

| Alan | Değer |
|---|---|
| Tarih | 2026-10-13 |
| Sorumlu | Oğuz |
| Dal | `oguz/accuracy-1` |
| Soru | COBOL'u okuyarak yazılan tahminler ne kadar doğru? |
| Yöntem | Programlar GnuCOBOL'da sahte CICS ve Db2 ile çalıştırıldı (`tests/gnucobol/README.md`, ADR 0004); her tahmin satırı "tuttu / tutmadı / belirsiz" diye işaretlendi |
| Kayıtlar | `tests/gnucobol/runs/lgicus01.md` (LGICUS01, 2026-10-10, #99), `tests/gnucobol/runs/lgacus01.md` (LGACUS01, 2026-10-13) |

## Sonuç

| Tahmin dosyası | Çalıştırılan zincir | Satır | tuttu | tutmadı | belirsiz | **Ana ölçü** (tuttu ÷ puanlanan) | Yardımcı ölçü (tuttu ÷ bütün satırlar) | Kontrol |
|---|---|---|---|---|---|---|---|---|
| `02-customer-inquire.md` | LGICUS01 → LGICDB01 | 17 | 14 | 0 | 3 | 14/14 = **%100** | 14/17 = %82,4 | 113 |
| `01-customer-add.md` | LGACUS01 → LGACDB01 → LGACVS01, LGACDB02 | 25 | 21 | 0 | 4 | 21/21 = **%100** | 21/25 = %84,0 | 274 |
| **Toplam** | | **42** | **35** | **0** | **7** | **35/35 = %100** | **35/42 = %83,3** | **387** |

**İlk sayısal cevap:** ölçülen 42 tahmin satırının 35'i puanlandı ve 35'i tuttu; hiçbiri tutmadı. 7 satır, tahminin kendisi `belirsiz` dediği için puanlanmadı.
Bütün satırları paydaya katan kötümser ölçü %83,3'tür.

## Nasıl hesaplandı

- **Ana ölçü** = tutan satır sayısı ÷ puanlanan satır sayısı (tuttu + tutmadı). `belirsiz` satırlar paydada yoktur. Bu, `runs/lgicus01.md`'deki sayma biçimidir.
- **Yardımcı ölçü** = tutan satır sayısı ÷ ölçülen dosyalardaki bütün satırlar. `belirsiz` satırlar payda içinde, pay dışında. Kötümser alt sınırdır.
- Her ölçümde **kurallar çalıştırmadan önce** yazılıp commit'lendi (`runs/lgacus01.md` için `d33c00e`, 12:11; çalıştırma 12:20). Formül de o kayıttadır.
- Satırın kararı: puanlanan iddialardan biri tutmadıysa **tutmadı**; hepsi tuttuysa **tuttu**; puanlanan iddia yoksa **belirsiz**. Bir satırın birden çok durumu varsa hepsi tutmalıdır.
- Kontrol sayısı (üçüncü ölçü, yalnızca bilgi): satırların bayt düzeyindeki iddiaları. Karar satır düzeyindedir.

### Hesaba girmeyenler

| Ne | Neden |
|---|---|
| H02 LGAPOL01 denemesi (`runs/lgapol01-deneme.md`) | Tahminler yazılmadan önce yapıldı; yöntemi denemekti, ölçüm değil |
| `03-customer-update.md` (müşteri güncelle) | `main`'de, ama henüz çalıştırılmadı |
| `04`-`07` motor tahminleri | Tahmin PR'ı birleşmedi, çalıştırılmadı |
| Konut, hayat, ticari tahminleri | Henüz yazılmadı |

## Satır satır

| Dosya | tuttu | belirsiz |
|---|---|---|
| `02-customer-inquire.md` | 02-01 ... 02-13, 02-17 (14) | 02-14, 02-15, 02-16 (3) |
| `01-customer-add.md` | 01-01 ... 01-08, 01-10 ... 01-14, 01-16 ... 01-22, 01-25 (21) | 01-09, 01-15, 01-23, 01-24 (4) |

`belirsiz` satırların nedenleri `tests/results/discrepancies.md`'dedir.

## Bu yüzde ne söyler, ne söylemez

**Söyledikleri**

- Test ekibinin kodu okuyarak yazdığı müşteri sorgula ve müşteri ekle tahminleri, programların **COBOL mantığıyla** (uzunluk kontrolleri, dönüş kodları, alan eşlemesi ve dolgu, COMMAREA dışına yazma,
  hata mesajı içerikleri, hangi programın çağrıldığı ya da çağrılmadığı) çelişmedi.
- Tahmin yazarları bilmediklerini `belirsiz` diye işaretledi: 42 satırın 7'si (%16,7). Yani tahminler, emin olmadıkları yerde iddia etmedi; bu doğruluğu yükseltir ama dürüst bir davranıştır.

**Söylemedikleri**

1. **Düzenek gerçek sistem değildir.** Db2, VSAM, sayaç sunucusu ve LGSTSQ stub'dır. Satırlar, Db2'nin ya da VSAM'ın ne yaptığını değil, COBOL'un onlara **ne verdiğini** ölçer.
   "Satırın gerçekten oluşması", "birincil anahtar ihlali", "geri alma", "ABEND sonrası görevin sonlanması" ölçülmedi (`runs/lgacus01.md`, "Puanlanmayan iddialar").
2. **Tahminin ön koşulları (B1-B7) düzeneğin kararıdır.** Tahmin ve düzenek aynı varsayımı paylaştığında, ikisinin de gerçek CICS ve Db2'den farklı çıkması bu ölçümle görülemez
   (bkz. `discrepancies.md`, "Yöntemin göremeyeceği hata türü").
3. **Örnek küçük ve bağımsız değil.** Satırlar iki zincirden, aynı yazım biçimiyle çıkıyor. 35/35'e ikili dağılımla bakıldığında, satırlar bağımsız sayılırsa %95 güvenle gerçek oranın en az
   ~%91,8 olduğu söylenebilir (`0,05^(1/35)`). Bu **yalnızca yön gösterir**: satırlar bağımsız değil, iki dosya da test ekibinin aynı ayrıntı düzeyinde yazdığı müşteri operasyonları.
4. **Tek kişi ölçtü.** Kuralları, düzeneği ve karşılaştırma betiğini aynı kişi yazdı. Kuralların çalıştırmadan önce commit'lenmesi ve betiğin bilerek bozulmuş beklentilerle sınanması
   bunu hafifletir, ortadan kaldırmaz. Başka biri `runs/lgacus01-karsilastir.sh`'yi ve `runs/lgacus01-cikti.txt`'yi yeniden çalıştırabilir.
5. **%100, "tahminler kusursuz" demek değildir.** Ölçülen zincirler ve satırlar sınırlıdır; `03` (güncelle) ve motor tahminleri henüz ölçülmedi. Bu yüzde onlar için bir şey söylemez.

## Sonraki adımlar

- `03-customer-update.md` için LGUCUS01 zincirini çalıştırıp bu tabloya eklemek.
- Motor tahmin PR'ı birleştikten sonra `04`-`07`'yi ölçmek (bu dosyada yer ayrılmıştır).
- Gerçek Db2 ve CICS'e erişim olursa ölçülemeyen iddiaları ayrıca sınamak.
