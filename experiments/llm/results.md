# LLM deneyi: eşdeğerlik testi sonuçları (H09)

| Alan | Değer |
|---|---|
| Sorumlu | Oğuz |
| Çalıştırma tarihi | 2026-10-27 |
| LLM çıktısı | `experiments/llm/runs/2026-10-27/` (Claude Opus 5.5, `claude-opus-5-5`; talimat ve model kaydı orada) |
| Test seti | `tests/GenApp.EquivalenceTests`, `6e3dbb7` (`main`) commit'inde dondurulmuş hâli; değiştirilmedi |
| Kapsam | `musteri-ekle`, `musteri-sorgula`, `musteri-guncelle`, `motor-ekle`, `motor-sorgula`, her biri 3 tekrar ([README](README.md), Değişiklik kaydı) |
| Araçlar | .NET SDK 10.0.403, PostgreSQL 18.6 (bu iş için ayrı kurulmuş geçici örnek, şema `dotnet ef database update` ile migration'lardan), betik `run_tests.py` |
| Ölçü | geçen test ÷ toplam test (README bölüm 6). Geçti = xUnit `Passed`; derleme hatası, zaman aşımı, çöken API, atlanan test = geçmedi |
| Terim | Bu dosyada "elle yazılan / elle yazılmış kod", ekibin analiz, tahmin, test ve inceleme süreciyle adım adım yazdığı koddur. Bu kod da yapay zekâ (Claude Code) yardımıyla yazıldı; karşılaştırma insan ile yapay zekâ arasında değil, denetimli süreç ile tek seferlik çeviri arasındadır |

## Sonuç

### Ana ölçü: izole düzen (A)

Her satırda incelenen operasyonun elle yazılmış action'ı devre dışı, yerine LLM'in dosyaları vardır; testlerin kurulum
adımlarındaki diğer operasyonlar elle yazılmış koddur. Hücreler **geçen / toplam**.

| Operasyon | Toplam test | Elle yazılan | LLM T1 | LLM T2 | LLM T3 | LLM ortalama | LLM − elle |
|---|---|---|---|---|---|---|---|
| `musteri-ekle` | 1 | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 (%100) | 0 |
| `musteri-sorgula` | 2 | 2/2 | 2/2 | 2/2 | 2/2 | 2/2 (%100) | 0 |
| `musteri-guncelle` | 1 | 1/1 | 1/1 | 1/1 | 1/1 | 1/1 (%100) | 0 |
| `motor-ekle` | 2 | 2/2 | 2/2 | 2/2 | 2/2 | 2/2 (%100) | 0 |
| `motor-sorgula` | 4 | 4/4 | 4/4 | 4/4 | 4/4 | 4/4 (%100) | 0 |
| **Genel** | **10** | **10/10 (%100)** | **10/10** | **10/10** | **10/10** | **10/10 (%100)** | **0** |

Geçmeyen test yok; 15 LLM çalıştırmasının hepsi derlendi.

### Yardımcı ölçü: bütünleşik düzen (B)

Kapsamdaki 5 operasyonun hepsi aynı tekrardan (T1, T2 ya da T3) gelen LLM kodudur, elle yazılmış action'ların beşi de
devre dışıdır; 10 testin hepsi çalışır.

| Tekrar | Derlendi | Geçen / toplam |
|---|---|---|
| T1 | evet | 10/10 |
| T2 | evet | 10/10 |
| T3 | evet | 10/10 |

### Ayrı olarak: `motor-guncelle` (ölçüme girmez)

`motor-guncelle` yalnızca T1 olarak çalıştırıldı ve kapsam dışıdır; bu çalıştırmada test edilmedi.

## Test başına kayıt

Bütün testler her çalıştırmada `Passed`. Testler (ADR 0005 Karar 3'e göre adlar COBOL satırını gösterir):

| Operasyon | Test |
|---|---|
| `musteri-ekle` | `LGACDB01_satir_156_musteri_eklenir_00_doner` |
| `musteri-sorgula` | `LGICDB01_satir_194_musteri_var_00_doner`, `LGICDB01_satir_196_kayit_yok_01_doner` |
| `musteri-guncelle` | `LGUCDB01_satir_124_musteri_var_00_doner` |
| `motor-ekle` | `LGAPDB01_satir_293_police_eklenir_00_doner`, `LGAPDB01_satir_296_musteri_yok_70_doner` |
| `motor-sorgula` | `LGIPDB01_satir_258_police_var_00_doner`, `LGIPDB01_satir_598_prim_ve_kaza_sifirdan_farkli_00_doner`, `LGIPDB01_satir_612_police_yok_01_doner`, `LGIPDB01_satir_612_baska_musterinin_policesi_01_doner` |

Her çalıştırmanın ham kaydı `sonuclar/<ad>.json` ve `sonuclar/<ad>.trx` dosyalarındadır (ad: `A-<operasyon>-<tekrar>`,
`B-<tekrar>`, `baz`, `kontrol`).

Başarısız test olmadığı için README bölüm 6'daki "başarısız testlerin ele alınması" listesi boştur. **Test hatası
şüphesi: yok.**

## Ölçümün geçerliliği: nasıl doğruladık

100 % sonuç, düzeneğin gerçekten LLM kodunu çalıştırdığı kanıtlanmadan okunmamalıdır. Üç kontrol yapıldı:

1. **Taban:** Hiçbir şey değiştirilmeden elle yazılmış kod aynı 10 testten 10'unu geçti (`sonuclar/baz.json`). Düzenek ve
   veritabanı sağlam.
2. **Negatif kontrol:** Beş elle yazılmış action devre dışıyken ve **hiç LLM dosyası yokken** aynı 10 testin
   **0'ı geçti** (`sonuclar/kontrol.json`; kurulum adımı 404 alır). Yani testlerin geçmesi elle yazılmış koddan değil,
   eklenen LLM dosyalarından gelir.
3. **Derleme:** LLM dosyaları, `cikti/` içindeki hâliyle (bayt bayt, elle değiştirilmeden) kopyalanıp derlendi.
   15 / 15 çalıştırma derlendi.

## Nasıl çalıştırıldı

Ana koda (`src/`, `tests/`) dokunulmadı; betik `git archive` ile bir çalışma kopyası çıkarır (`C:/w/agac`) ve her şeyi
orada yapar. Tekrar etmek için:

```
python experiments/llm/run_tests.py --dotnet <dotnet 10> --baglanti "<ConnectionStrings__GenApp>" --is-dizini C:/w
```

- **Elle yazılmış action'ı devre dışı bırakma** ([`harness/LlmIzoleKonvansiyonu.cs`](harness/LlmIzoleKonvansiyonu.cs)):
  action'ın kodu silinmez; adresi, ölü bir ön eke (`/llm-dislanan/...`) taşınır. Böylece gerçek adreste yalnızca
  LLM'in controller'ı yanıt verir ve elle yazılmış koddaki `CreatedAtAction(nameof(Inquire))` gibi bağlantılar
  çalışmaya devam eder. Devre dışı bırakılan action'lar (README bölüm 6: "hangi action'ın nasıl çıkarıldığı kayda
  yazılır"):

  | Operasyon | Devre dışı elle yazılmış action |
  |---|---|
  | `musteri-ekle` | `CustomersController.Add` |
  | `musteri-sorgula` | `CustomersController.Inquire` |
  | `musteri-guncelle` | `CustomersController.Update` |
  | `motor-ekle` | `MotorPoliciesController.Add` |
  | `motor-sorgula` | `MotorPoliciesController.Inquire` |

- Çalışma kopyasındaki tek değişiklik `Program.cs`'te `AddControllers()` çağrısına bu kuralın eklenmesi ve
  `LlmIzoleKonvansiyonu.cs`'in kopyalanmasıdır; LLM dosyaları `src/GenApp.Api/Llm/<operasyon>/` altına konur.
- Veritabanı çalıştırmalar arasında sıfırlanmadı (ADR 0005); her test kendi kaydını `POST` ile kurar.

## Bu sayı ne söyler, ne söylemez

- **Söyler:** Bu 5 operasyonda, bu 10 testle, 3 bağımsız LLM çıktısının her biri elle yazılmış kodla aynı sonucu verdi
  (fark 0).
- **Söylemez:**
  - **Test seti küçük.** 5 operasyon için yalnızca 10 test var; testler, tahmin tablolarındaki iş kurallarının yalnızca
    çevrilebilen bir kısmını ölçer (ADR 0005 Karar 2). Geçmek tam eşdeğerlik demek değildir; farkı yakalayacak test
    yoksa fark görünmez. **Tavan etkisi:** her iki taraf da %100 olduğu için testler LLM ile elle yazılmış kodu
    birbirinden ayıramadı.
  - **Seçilim yanlılığı:** Kapsam rastgele değil; çalıştırma sırasında ilk gelen ve üç tekrarı da tamamlanan
    müşteri ve motor operasyonlarıdır (README, Değişiklik kaydı). Sonuç hayat, konut ve ticari operasyonlara ve
    güncelle/sil gibi daha zor operasyonlara genellenmez.
  - **Aynı model ailesi:** Elle yazılan kod da Claude Code yardımıyla yazıldı. Fark, kodun nasıl yazıldığındadır:
    bizim tarafta analiz, tahmin tabloları, testler ve inceleme ile adım adım; LLM tarafında COBOL'dan tek seferde,
    geri bildirimsiz ve düzeltmesiz. Sonuç "yapay zekâ insan kadar iyi" diye okunamaz.
  - **Belge sızıntısı:** LLM'e `api-design.md` (bilinçli sapmalar dâhil) verildi; elle yazan ekip de görmüştü (README
    bölüm 8). Sonuç yalnızca COBOL'dan çevirinin göstereceğinden iyi olabilir.
  - **Eğitim verisi:** GenApp açık bir IBM örneğidir; modelin COBOL'u ya da çevirisini eğitiminde görüp görmediğini
    ölçemeyiz.
  - **Tek model, tek tarih:** Sonuç `claude-opus-5-5` için, 2026-10-27 içindir.
  - **Kod kalitesi ölçülmedi:** Okunabilirlik, bakım kolaylığı ve performans bu deneyin sorusu değildir.
