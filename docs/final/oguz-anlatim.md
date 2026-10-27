# Final sunumu: GnuCOBOL ve LLM deneyi (Oğuz)

Bu dosya, [`oguz-slaytlar.pptx`](oguz-slaytlar.pptx) ile birlikte kullanılır. Slaytlar `slaytlari-uret.py` ile üretildi
ve konuşmacı notlarını içerir. Her sayının kaynağı [`docs/report/gnucobol-ve-llm.md`](../report/gnucobol-ve-llm.md)
bölümü ve onun dayandığı dosyalardır.

## 1. Slaytlar ve süre

| Slayt | Konu | Ana mesaj | Tahmini süre |
|---|---|---|---|
| 1/3 | GnuCOBOL düzeneği | CICS ve Db2 olmadan eski kodu sahte parçalarla çalıştırdık | ~1 dk |
| 2/3 | Tahmin doğruluğu | Puanlanan 53 tahmin satırının 53'ü tuttu; bu gerçek Db2/CICS'i ölçmez | ~1 dk |
| 3/3 | LLM deneyi | LLM'in 5 operasyonu aynı 10 testten 10'unu geçti; sonuç 18 operasyona genellenmez | ~1 dk |
| (ADR) | ADR 0004'ün anlatımı | Aşağıda, 2 dakika | ~2 dk |

Süreler **tahmindir**: aşağıdaki metin dakikada yaklaşık 130 kelimeyle okunduğu varsayılarak hesaplandı. Ekibin tam
provasında ölçülen süre bu tabloya yazılmalıdır (bu dosyayı hazırlarken prova yapılmadı).

Sertuğ'un final sunumu dosyası bu dalda yoktu, bu yüzden slaytlar ayrı dosya olarak eklendi. Birleştirirken
`oguz-slaytlar.pptx` içindeki 3 slayt ana sunuma kopyalanabilir (konuşmacı notları slaytla gelir).

## 2. ADR 0004'ü 2 dakikada anlatmak

Kaynak: [`docs/adr/0004-legacy-test-harness.md`](../adr/0004-legacy-test-harness.md). Aşağıdaki metin ADR'nin
dört bölümünü (sorun, karar, kapsam, bedel) sırayla anlatır; kelimesi kelimesine ezberlenmek için değil, kendi
cümlelerinle anlatman için bir iskelettir.

> **Sorun.** GenApp'i .NET'e taşımak için eski programın gerçekte ne yaptığını bilmemiz gerekiyordu. Test ekibi
> COBOL'u okuyarak tahmin yazdı; ama kodu okumak, tahminin doğru olduğunu göstermez, çalıştırmak gerekir. Bizde
> z/OS, CICS ve Db2 yok. GnuCOBOL ile ilk denememizde 12 hata çıktı ve hepsi CICS bağımlılığıydı: CICS'in programa
> kendiliğinden verdiği alanlar, `EXEC CICS` komutları ve veritabanı komutları.
>
> **Karar.** Denediğimiz programı kendi başına çalıştırıp dışındaki her şeyi sahte parçalarla değiştirdik; buna
> bağımlılık izolasyonu diyoruz. CICS alanları programın kendi değişkeni oldu, `EXEC CICS` komutları GnuCOBOL
> karşılıklarına döndü, çağırdığı programların yerine stub yazdık, CICS'in rolünü de bir sürücü program oynadı. En
> önemli kural şu: stub sonuç uydurmaz. Ekranda gördüğümüz sonuç, programın kendi kodundan gelmelidir. Veritabanı
> komutlarında ise sahte bir hata kodu veriyoruz ve sonuç, "Db2 bu kodu döndürürse program şunu yapar" diye koşullu
> okunuyor.
>
> **Ne ölçüyoruz, ne ölçmüyoruz.** Programın karar yollarını ve dönüş kodlarını ölçüyoruz. Örneğin ilk denemede LGAPOL01, 10 baytlık veri aldığında `98`, tam boy 32500 baytlık veri aldığında `00` döndürdü. Gerçek Db2'nin, VSAM'ın ve
> CICS'in davranışını ölçemiyoruz, çünkü stub bizim varsayımımız. Çalıştırdığımız kod da orijinalin birebir kendisi
> değil, kopyası.
>
> **Bedel.** Her yeni program için kopya, stub ve sürücü yazmak gerekiyor. Bu yüzden müşteri ve motor sorgulama gibi
> birkaç programı ölçebildik; hepsini değil.

Kelime sayısı: yaklaşık 200 (dakikada 130 kelimeyle ≈ 1,5 dakika; sorulara ve slaytı göstermeye pay kalır). ADR'nin durumu "Önerildi"dir; bunu sorulursa açıkça söyle.

### 2.1 Mimari kararın tek cümlesi

> Eski kodu gerçek ortamı olmadan çalıştırmak için, denenen programın dışındaki her şeyi sonuç uydurmayan sahte
> parçalarla değiştirdik; böylece ölçtüğümüz şey yalnızca programın kendi mantığı oldu.

## 3. Beklenen sorular ve cevaplar

Cevapların dayanağı parantez içindedir; dayanağı olmayan bir şey söyleme.

| Soru | Cevap | Dayanak |
|---|---|---|
| Neden gerçek CICS ve Db2 kurmadınız? | Erişimimiz yoktu; açık kaynak emülatörleri bu çalışmada değerlendirmedik. | ADR 0004, "Değerlendirilen alternatifler" |
| Stub ile mock arasındaki fark? | Mock, doğru çağrının doğru bilgiyle yapılıp yapılmadığını kendisi denetler. Bizim stub'lar sadece "çağrıldı" ve ne aldığını yazdırır; denetimi gözle yaparız. | ADR 0004, "Terimler" |
| 53/53 neden mükemmel? Bu şüpheli değil mi? | 10 satır puanlanmadı, çünkü tahminin kendisi "belirsiz" dedi. Ayrıca düzenek gerçek Db2'yi ölçmez; tahmin ve düzenek aynı varsayımı paylaşırsa fark görünmez. Tek kişi ölçtü; kurallar çalıştırmadan önce commit'lendi, betik bilerek bozulmuş çıktılarla sınandı. | `tests/results/prediction-accuracy.md`, `tests/results/discrepancies.md` |
| Kaç program çalıştırdınız? | Üçü ölçüm: LGICUS01, LGACUS01, LGIPOL01 (LGIPDB01 ayrıca denendi). Güncelle, sil, konut, hayat, ticari programlarının kaydı yok. | `tests/gnucobol/runs/`, rapor 3. bölüm |
| LLM neden bu kadar iyi çıktı? | Bu kadarını söyleyemem: ölçtüğümüz yalnızca 10 test, ve ikisi de %100 olduğu için testler LLM'i bizim kodumuzdan ayıramadı. Bilinen etkenler: modele API belgesi (bilinçli sapmalar dahil) verildi; GenApp açık bir IBM örneği, model eğitiminde görmüş olabilir. | `tests/results/llm-comparison.md` bölüm 1 |
| Neden sadece 5 operasyon? | Plan 18 operasyon × 3 tekrardı. API kredisi 16 çağrıda bitti; üç tekrarı tamamlanan 5 operasyon ölçüldü. Seçim rastgele değil, çalıştırma sırasıyla müşteri ve motor grubu kaldı. | `experiments/llm/README.md` Değişiklik kaydı, `experiments/llm/runs/2026-10-27/README.md` |
| LLM hiç hata yapmadı mı? | Dondurulmuş testlerde ve ölçüm dışı 16 ek yoklamada hata bulunmadı. Daha zor davranışlar (güncellemede kodun ezilmesi, silmede tür kontrolü, ticari liste sorguları) çevirtilmedi. | `tests/results/llm-comparison.md` bölüm 1 ve 2 |
| Sonuçlar "LLM bizim süreçle aynı iyilikte" demek mi? | Hayır. İki kodun yalnızca çıktısı karşılaştırıldı; sürecin katkısı ve kod kalitesi ölçülmedi. | `tests/results/llm-comparison.md` bölüm 3 |
| Test hile olabilir mi (LLM bizim kodu mu kullandı)? | Hayır: modele bizim C# kodumuz verilmedi, ve negatif kontrolde elle yazılmış action'lar devre dışıyken LLM dosyası olmadan testler 0/10 geçti. | `experiments/llm/README.md` bölüm 3, `experiments/llm/results.md` |

## 4. Provaya katılım

Ekibin tam provasına katılmak ve kendi bölümünün süresini tutmak benim (Oğuz) yapacağım iştir; bu dosyada tahmini
süreler vardır, ölçülen süreler yoktur. Prova sonrası "Tahmini süre" sütununu ölçülen sürelerle güncelle.
