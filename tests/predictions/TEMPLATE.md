# Tahmin Tablosu Şablonu

Test ekibinin (Sercan, Oğuz) bütün tahminleri aynı biçimde yazması için ortak şablon.

**Tahmin:** COBOL kodunu okuyarak "şu girdiyi verirsem program şu sonucu döndürür" diye
önceden yazılan beklentidir. Her tahmin iki yerde kullanılır:

1. C# testlerine dönüştürülür.
2. COBOL gerçekten çalıştırıldığında (GnuCOBOL) tahminin doğru çıkıp çıkmadığı ölçülür.

## Kural türleri

Her tahmin satırı aşağıdaki iki türden **yalnızca birine** atanır.

- **Arayüz (COMMAREA) kuralı:** Programlar arasında bilgiyi taşıyan COMMAREA'nın kendisiyle
  ilgilidir (örn. COMMAREA kısa gelirse `98` dönmesi). Sadece GnuCOBOL çalıştırmalarında
  kullanılacak.
- **İş kuralı:** Sigorta işinin kendisiyle ilgilidir (örn. müşteri bulunamazsa `01` dönmesi).
  Hem GnuCOBOL'da hem C# testlerinde kullanılacak.

## Yazım kuralları

1. Tahmin **yalnızca COBOL kodu okunarak** çıkarılır. Kodda görülmeyen hiçbir şey yazılmaz.
2. Emin olunmayan hücreye `belirsiz` yazılır ve nedeni aşağıdaki "Belirsiz satırlar"
   bölümünde açıklanır. Tahmin uydurulmaz.
3. Her satırın "Tahminin dayandığı COBOL satırı" hücresi `DOSYA:satır` biçimindedir
   (örn. `lgicus01.cbl:120` veya `lgicus01.cbl:120-125`). Satır numarası yazılmadan önce
   dosya açılıp o satırın gerçekten orada olduğu doğrulanır.
4. Bir satır tek bir kod yolunu (tek bir dönüş kodu sonucunu) anlatır. Aynı dönüş kodunu
   üreten farklı yollar ayrı satırlara yazılır.
5. Dönüş kodu, COBOL'daki alanın kendisiyle birlikte yazılır (örn. `CA-RETURN-CODE = '01'`).
   Alan adı kodda nasıl geçiyorsa öyle yazılır.
6. Test no biçimi: `<operasyon no>-<sıra>` (örn. `02-01`, `02-02`). Operasyon no, dosya
   adındaki önekle aynıdır.

## Dosya başlığı

Her tahmin dosyası (`NN-operasyon-adi.md`) bu tabloyla başlar:

| Alan | Değer |
|---|---|
| Operasyon | |
| Programlar (çağrı zinciri) | |
| Okunan COBOL dosyaları | |
| Hazırlayan | |
| Tarih | |
| Kaynak commit (`git rev-parse HEAD`) | |

## Kod gözlemleri (isteğe bağlı)

Birden fazla satırı etkileyen ve koddan doğrudan okunan bilgiler buraya yazılır (örn. COMMAREA
alanlarının bayt konumları, minimum uzunluk hesabı, programlar arası çağrı biçimi). Her madde
yine `DOSYA:satır` kaynağıyla yazılır.

## Tahmin tablosu

| Test no | Girdi | Beklenen dönüş kodu | Beklenen veri | Tahminin dayandığı COBOL satırı | Kural türü |
|---|---|---|---|---|---|
| NN-01 | | | | `dosya.cbl:satır` | Arayüz (COMMAREA) kuralı / İş kuralı |

Sütun açıklamaları:

- **Girdi:** Programa verilen COMMAREA alanları ve veritabanının o andaki durumu
  (örn. "müşteri no = 1, Db2'de bu müşteri var").
- **Beklenen dönüş kodu:** Programın döndürdüğü dönüş kodu alanı ve değeri.
- **Beklenen veri:** Dönüşte COMMAREA'da dolu olması beklenen alanlar. Veri dönmüyorsa
  "Yok" ya da "değişmez" yazılır. Bilinmiyorsa `belirsiz`.
- **Tahminin dayandığı COBOL satırı:** Sonucu belirleyen satır(lar). Birden fazla dosya
  gerekiyorsa hepsi yazılır.
- **Kural türü:** `Arayüz (COMMAREA) kuralı` ya da `İş kuralı`.

## Belirsiz satırlar

| Test no | Belirsiz olan hücre | Neden belirsiz |
|---|---|---|
| | | |
