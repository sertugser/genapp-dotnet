# Bizim kod ile LLM kodu: eşdeğerlik testi karşılaştırması

| Alan | Değer |
|---|---|
| Tarih | 2026-10-27 |
| Sorumlu | Oğuz |
| Dal | `oguz/llm-table` |
| Soru | Süreçle (analiz, tahmin, test, inceleme) yazdığımız kod ile bir LLM'in tek seferde yazdığı kod aynı eşdeğerlik testlerinden kaçını geçiyor? |
| Elle yazılan kod | `src/` (`main`, `6e3dbb7`) |
| LLM kodu | `experiments/llm/runs/2026-10-27/` (`claude-opus-5-5`; her operasyon temiz oturumda, tek atış, geri bildirimsiz; üç bağımsız tekrar T1, T2, T3) |
| Test seti | `tests/GenApp.EquivalenceTests` (`6e3dbb7`), değiştirilmedi |
| Ham veri | `experiments/llm/sonuclar/` (`.json` ve `.trx`); düzenek ve doğrulama: [`experiments/llm/results.md`](../../experiments/llm/results.md) |

## 1. Sonuç tablosu

Hücreler **geçen / toplam** testtir. LLM sütunlarında incelenen operasyonun elle yazılmış action'ı devre dışı, yerine
LLM'in dosyaları vardır; testlerin kurulum adımındaki diğer operasyonlar elle yazılmış koddur (README bölüm 6, "A. İzole").

| Operasyon | Bizim kod | LLM T1 | LLM T2 | LLM T3 |
|---|---|---|---|---|
| `musteri-ekle` | 1/1 | 1/1 | 1/1 | 1/1 |
| `musteri-sorgula` | 2/2 | 2/2 | 2/2 | 2/2 |
| `musteri-guncelle` | 1/1 | 1/1 | 1/1 | 1/1 |
| `motor-ekle` | 2/2 | 2/2 | 2/2 | 2/2 |
| `motor-sorgula` | 4/4 | 4/4 | 4/4 | 4/4 |
| **Toplam** | **10/10 (%100)** | **10/10 (%100)** | **10/10 (%100)** | **10/10 (%100)** |

Fark (LLM − bizim kod): bütün operasyonlarda ve bütün tekrarlarda **0**. 15 LLM çıktısının hepsi derlendi.
Bütünleşik düzende (beş operasyonun LLM kodu birlikte, README "B") T1, T2 ve T3 için de 10/10 geçti
(`sonuclar/B-T1.json`, `B-T2.json`, `B-T3.json`).

**Düzeneğin doğrulanması:** Elle yazılmış beş action devre dışıyken ve LLM dosyası yokken aynı testlerden 0/10 geçti
(`sonuclar/kontrol.json`). Yani yukarıdaki geçişler LLM dosyalarından gelir.

### Sınır: bu sonuç neyi kapsıyor, neyi kapsamıyor

- **Sonuç bu beş operasyon içindir, 18 operasyona genellenmez.** Kapsam rastgele seçilmedi: çalıştırma ekip kararıyla 16 çağrıda durduruldu
  ve çalıştırma sırasında ilk gelen, üç tekrarı da tamamlanan operasyonlar (müşteri ve motor grubu) ölçüldü
  ([`experiments/llm/README.md`](../../experiments/llm/README.md), Değişiklik kaydı). Hayat, konut ve ticari operasyonlar
  ile motor güncelle ve sil çevirtilmedi ya da ölçülmedi.
- **En karmaşık davranışlar kapsam dışında kaldı.** Bunlar `docs/api-design.md` §3'te "bilinçli sapma" olarak yazılan,
  eski kodun kendi niyetiyle ayrıştığı yerlerdir:
  - **Güncellemede kodun ezilmesi:** poliçe güncellemede `02` (başkası değiştirdi), `01` ve `90` yazıldıktan sonra imleç
    kapatılırken `00` ile ezilir (`lgupdb01.cbl:346`, `:360`, `:371`). `motor-guncelle` yalnızca T1 olarak üretildi,
    bu testlerle çalıştırılmadı.
  - **Silmede tür kontrolü:** poliçe silme, poliçenin türüne bakmadan siler (`lgdpdb01.cbl:189-194`); hiçbir `*-sil`
    operasyonu LLM'e çevirtilmedi.
  - **Ticari liste sorguları:** ticari sorgulamanın müşteri ve posta koduyla yapılan biçimleri (17c, 17d), eski kodda
    kayıtları üst üste yazar; `ticari-sorgula` çevirtilmedi.
- **Test seti küçük ve tavana vurdu.** Beş operasyon için yalnızca 10 test var. İki taraf da %100 olduğundan testler
  LLM'i bizim kodumuzdan ayıramadı. "Eşdeğer" demek yerine "bu testlerle fark yakalanmadı" demek doğrudur.
- **Girdiler aynı değil, tam adil de değil.** LLM'e `docs/api-design.md` bilinçli sapmalarıyla verildi; elle yazan ekip
  de görmüştü. LLM'in COBOL'u kendi başına yorumlayıp yorumlayamadığı bu deneyde ayrıca ölçülmedi.
  GenApp açık bir IBM örneğidir; modelin eğitiminde görmüş olması dışlanamaz.
- **Aynı model ailesi.** Bizim kodumuz da Claude Code yardımıyla yazıldı. Karşılaştırma insan ile yapay zekâ
  arasında değil, analiz, tahmin, test ve incelemeyle adım adım yazılan kod ile COBOL'dan tek seferde, düzeltmesiz
  çevrilen kod arasındadır.
- **Tek model, tek tarih** (`claude-opus-5-5`, 2026-10-27).

## 2. LLM'in hataları

### 2.1 Dondurulmuş test setinde

**Hata yok.** 15 çalıştırmanın hiçbirinde geçmeyen test, derleme hatası, çöken API ya da zaman aşımı olmadı. Bu yüzden
sınıflandırılacak bir hata çıkmadı. Sınıflar önceden şöyle tanımlandı; hepsi 0:

| Hata türü | Anlamı | Sayı |
|---|---|---|
| Dönüş kodunu ya da HTTP durumunu yanlış eşleme | Yanlış `returnCode`/`type`/HTTP kodu (örn. `01` yerine `500`) | 0 |
| Bir kuralı atlama | COBOL'daki ya da belgedeki bir kontrol uygulanmadı (örn. büyük harfe çevirme, uzunluk sınırı) | 0 |
| Alanı yanlış eşleme | Alan adı, tür, dolgu ya da kırpma yanlış | 0 |
| Belgeyi yanlış okuma | Belgedeki bir sapma ya da davranış yanlış uygulandı | 0 |
| Derleme hatası | Çıktı derlenmedi | 0 |
| Çalışma zamanı hatası | API çöktü ya da zaman aşımı | 0 |

### 2.2 Ek yoklama (ölçüm dışı)

Dondurulmuş testler az olduğu için, testlerin ölçmediği ama `docs/api-design.md`'de yazılı 16 davranışı ayrıca
yokladım (`experiments/llm/yoklama/YoklamaTests.cs`). **Bunlar ana ölçüye girmez** ve test seti dondurulduktan sonra
yazıldı; yalnızca, "hata yok" sonucunun testlerin darlığından mı geldiğini anlamak için. Yoklamalar önce elle yazılmış
kodda denendi (16/16 geçti, `sonuclar/Ybaz.json`); böylece beklentiler belgeyle tutarlı. Her yoklama, belgedeki bölümüne
dayanır.

| Operasyon | Yoklama (belge) | Sayı |
|---|---|---|
| `musteri-ekle` | posta kodu büyük harfe çevrilir (§1); 11 karakterli `firstName` → 400 (§4.1); geçersiz doğum tarihi → 400 (§3); istekteki `customerNumber` yok sayılır (§1); boş metin `""` döner (§1) | 5 |
| `musteri-sorgula` | sayı olmayan numara → 404 (§1); 10 haneli numara kesilmez, kayıt yoksa 404 / `01` (§3) | 2 |
| `musteri-guncelle` | olmayan müşteri → 404 / `01` (§3); güncellemede posta kodu büyük harf (§1); 21 karakterli `lastName` → 400 (§4.1) | 3 |
| `motor-ekle` | 16 karakterli `make` → 400 (§4.3); istekteki `policyNumber` yok sayılır (§1); geçersiz tarih → 400 (§1) | 3 |
| `motor-sorgula` | başka türden (konut) poliçe → 404 / `01` (§3); sayı olmayan poliçe numarası → 404 (§1); alanlar eklenenle aynı (§1) | 3 |

| | Bizim kod | LLM T1 | LLM T2 | LLM T3 |
|---|---|---|---|---|
| Yoklama (geçen / çalıştırılan) | 16/16 | 16/16 | 16/16 | 16/16 |

Dosyalar: `sonuclar/Y-<operasyon>-<tekrar>.json` ve `Ybaz.json`. Bu yoklamalarda da **LLM hatası bulunamadı**.

### 2.3 Kod okumasından gözlemler (hata değil)

Testlerin ve yoklamaların göremediği bir noktaya kodu okuyarak baktım: müşteri eklemede eski sistem iki tabloya yazar
(`CUSTOMER` ve `CUSTOMER_SECURE`, `lgacdb01.cbl`, `lgacdb02.cbl`). `docs/database-schema.md` bunu iki tablo olarak
taşır. Üç LLM çıktısının üçü de (`musteri-ekle` T1, T2, T3) bir transaction içinde iki `INSERT` yapıp hata durumunda
`Rollback` çağırıyor. Bu bir gözlemdir: davranışı çalıştırarak doğrulamadım, ölçüme girmez.

`musteri-guncelle` T1 ve T3 açık transaction kullanmıyor; tek bir `UPDATE` deyimi kullandıkları için bu bir sorun değildir.

## 3. Bu sonuç ne söyler, ne söylemez

- **Söyler:** Bu beş operasyonda, bu 10 testle ve ek 16 belge davranışıyla, LLM'in üç bağımsız çıktısı da bizim kodumuzla
  aynı sonucu verdi.
- **Söylemez:** LLM'in bizim sürecimizle aynı kaliteyi ürettiğini; daha zor operasyonlarda (güncelle, sil, ticari liste
  sorguları) aynı sonucu vereceğini; ya da testlerin tam eşdeğerliği kanıtladığını. Test seti ve yoklama, testten
  geçen ama ölçülmeyen davranışları göremez (ADR 0005 Karar 2). Sürecin (analiz, tahmin, inceleme) bu sonuçta
  katkısı da bu deneyle ölçülmedi: iki kodun yalnızca **çıktısı** karşılaştırıldı, nasıl üretildikleri değil.
- **Sonraki adım:** Karşılaştırmayı anlamlı hâle getirmek için testi olmayan ya da çevirtilmeyen operasyonlar
  (özellikle güncelle ve sil) çevirtilip çalıştırılmalı.
