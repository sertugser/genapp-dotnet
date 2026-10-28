# Mutasyon testi

| Alan | Değer |
|---|---|
| Tarih | 2026-10-28 |
| Görev | #53 |
| Soru | Testlerimiz koda konan küçük hataları ne kadar yakalıyor? |
| Araç | Stryker.NET 4.13.0 |
| Ölçülen kod | `src/` (Domain, Application, Infrastructure, Api); EF Core migration'ları (otomatik üretilen kod) hariç |
| Kod sürümü | `main` @ `692947d` (18 operasyon, 65 eşdeğerlik ve 33 entegrasyon testi) |
| Çalıştırma | [GitHub Actions, Mutasyon testi](https://github.com/sertugser/genapp-dotnet/actions/runs/40637471410) (`.github/workflows/mutation.yml`) |
| Ham veri | Çalıştırmanın `mutation-*` artifact'ları (html ve json rapor); yakalanmayan mutantların tam listesi: [`mutation-survivors.md`](mutation-survivors.md) |

## Nasıl ölçüldü

Stryker.NET koda tek tek küçük hatalar (mutant) koyar; örneğin `&&` yerine `||`, `==` yerine `!=`, bir satırı siler ya da bir
metni boşaltır. Her mutant için testleri çalıştırır:

- Bir test kırılırsa mutant **yakalandı** (`Killed`; zaman aşımı `Timeout` da yakalandı sayılır).
- Testler yine geçerse mutant **yakalanmadı** (`Survived`).
- Hiçbir test o satırı çalıştırmıyorsa **kapsanmadı** (`NoCoverage`; yakalanmadı sayılır).

**Mutasyon skoru** = yakalanan ÷ (yakalanan + yakalanmayan + kapsanmayan). Derlenmeyen (`CompileError`) ve Stryker'ın
kendiliğinden atladığı (`Ignored`) mutantlar sayılmaz. Bu, Stryker'ın kendi formülüdür.

İki test takımı ayrı ölçüldü:

| Takım | İçindeki testler | Neden |
|---|---|---|
| **Eşdeğerlik** | `tests/GenApp.EquivalenceTests` (65 test) | COBOL'dan türetilen testler; araştırmanın asıl ölçüsü |
| **Bütün testler** | Eşdeğerlik + entegrasyon (33) + birim + veritabanı (57) | Projenin bütün test gücü |

Testler aynı veritabanını paylaştığı için mutantlar tek tek (eşzamanlılık 1) denendi. Ayar yalnızca çalıştırma makinesinde
test projelerine ölçülen projenin doğrudan referansını ekler (testler ona zaten Api üzerinden dolaylı bağlı; Stryker doğrudan
referans istiyor). Depodaki kod ve testler değiştirilmedi.

## Sonuç

| Proje | Eşdeğerlik testleri | Bütün testler |
|---|---|---|
| GenApp.Domain | %0,0 (0/143) | %67,1 (96/143) |
| GenApp.Application | %83,3 (45/54) | %88,9 (48/54) |
| GenApp.Infrastructure | %36,0 (49/136) | %46,3 (63/136) |
| GenApp.Api | %57,3 (75/131) | %58,0 (76/131) |
| **Toplam** | **%36,4 (169/464)** | **%61,0 (283/464)** |
| **Toplam, Domain hariç** (aşağıya bakın) | **%52,6 (169/321)** | **%58,3 (187/321)** |

Durumların tam dağılımı:

| Proje | Takım | Yakalandı | Yakalanmadı | Kapsanmadı | Derlenmedi | Atlandı |
|---|---|---|---|---|---|---|
| Domain | eşdeğerlik | 0 | 143 | 0 | 2 | 17 |
| Domain | bütün | 96 | 47 | 0 | 2 | 17 |
| Application | eşdeğerlik | 45 | 7 | 2 | 0 | 12 |
| Application | bütün | 48 | 5 | 1 | 0 | 12 |
| Infrastructure | eşdeğerlik | 49 | 69 | 18 | 4 | 27 |
| Infrastructure | bütün | 63 | 68 | 5 | 4 | 27 |
| Api | eşdeğerlik | 75 | 39 | 17 | 0 | 30 |
| Api | bütün | 76 | 38 | 17 | 0 | 30 |

Zaman aşımına uğrayan mutant olmadı.

## Yakalanmayan mutantlar nerede

Eşdeğerlik testlerinin yakalamadığı 295 mutant (yakalanmadı + kapsanmadı) altı gruba ayrılır. Bütün testlerde bu sayı 181'dir.

| Grup | Eşdeğerlik | Bütün testler | Ne anlama geliyor |
|---|---|---|---|
| **1. COMMAREA modeli** (`src/GenApp.Domain/Legacy/`) | 143 | 47 | Eski COMMAREA'nın bayt düzenini tutan model. API bu kodu hiç kullanmıyor (`src/` içinde başka hiçbir yerden çağrılmıyor), bu yüzden API üzerinden çalışan eşdeğerlik testleri onu **ölçemez**. Domain'in bütün geçerli mutantları burada. Birim testleri 143'ün 96'sını yakalıyor. Bu yüzden yukarıda "Domain hariç" skor ayrıca verildi. |
| **2. EF Core yapılandırması** (`Infrastructure/Persistence/Configurations/`) | 64 | 59 | Çoğu bir yapılandırma satırının silinmesi (örneğin sütun uzunluğu). Veritabanı şeması migration'larla önceden kurulduğu için bu satırlar çalışma anında davranışı değiştirmiyor. Büyük kısmı **davranışı değiştirmeyen** (eşdeğer) mutantlardır; testle yakalanmaları beklenmez. |
| **3. Hata mesajı metinleri** (`ReturnCodeProblems`, `PolicyProblems`, `ResponseText`) | 31 | 31 | Hata cevabındaki açıklama metninin boşaltılması. Testler hatanın türünü (`type`) ve dönüş kodunu (`returnCode`) doğruluyor, açıklama cümlesini doğrulamıyor. Bilinçli bir seçim; istenirse metinler de test edilebilir. |
| **4. Veritabanı hata yolları** (`DatabaseErrors`, `DatabaseExceptionHandler`, `throw DatabaseErrors.Translate`) | 24 | 18 | Veritabanı hata verdiğinde (bağlantı kopması, kilit gibi) çalışan kod. Normal testlerde bu hatalar oluşmuyor; tetiklemek için veritabanını bilerek bozan testler gerekir. **Gerçek boşluk.** |
| **5. Başlangıç ayarları** (`Program.cs`) | 10 | 10 | Swagger, loglama gibi ayarlar. İş kuralı değil. |
| **6. İş kuralları ve veri erişimi** | 23 | 16 | Asıl önemli grup; aşağıda tek tek. **Gerçek boşluk.** |

### 6. grup: iş kuralı boşlukları (eksik testler için liste)

Eşdeğerlik testlerinin yakalamadığı 23 mutant. "Bütün" sütunu, diğer testlerin (entegrasyon, birim) onu yakalayıp yakalamadığıdır.

| Dilim | Yer | Ne değiştirildi | Testlerin kaçırdığı durum | Bütün |
|---|---|---|---|---|
| Müşteri | `CustomerService.cs:31` | Yeni müşterinin `StateIndicator` değeri `"N"` yerine `""` | Müşteri eklenince güvenlik kaydının durum alanı | yakalanmadı |
| Müşteri | `CustomerService.cs:34` | Güvenlik kaydının kaydedilmesi silindi | Müşteri eklenince güvenlik kaydı oluşuyor mu | yakalandı |
| Müşteri | `CustomerService.cs:56` | Güncellemede "müşteri yok" dalı hiç çalışmıyor | Olmayan müşteriyi güncelleme (`404`/`01`) | yakalandı |
| Müşteri | `CustomersController.cs:46` | Güncelleme sonucu hep "başarılı" | Aynı durum | yakalandı |
| Müşteri | `CustomersController.cs:52` | "Müşteri bulunamadı" cümlesi boş | Hata açıklaması (3. gruba benzer) | yakalanmadı |
| Ticari | `CommercialPolicyService.cs:22` (2 mutant) | Poliçe türü kontrolü kaldırıldı ya da bozuldu | Başka türden poliçeyi ticari adresinden sorgulama/silme (`404`/`01`, bilinçli sapma) | yakalanmadı |
| Ticari | `CommercialPoliciesController.cs:56`, `:58` | Poliçe numarasıyla sorguda "bulunamadı" dalı | Olmayan ticari poliçeyi yalnız numarayla sorgulama | yakalanmadı |
| Ticari | `CommercialPolicyResponse.cs:15` | `RequestDate` boşsa kullanılan yedek değer | Ticari poliçede `RequestDate` boş olduğunda `lastChanged` | yakalanmadı |
| Ticari | `CommercialPolicyService.cs:15` | Güncelleme yok hatasının metni | Hiç çağrılmayan kod (ticari güncelleme yok) | kapsanmadı |
| Poliçe (ortak) | `PolicyService.cs:117` | Poliçe türü kontrolü hep "doğru" | Başka türden poliçeyi yanlış adresten sorgulama, güncelleme, silme (`404`/`01`, bilinçli sapma) | yakalanmadı |
| Poliçe (ortak) | `PolicyService.cs:107` | Tür bulunduktan sonra silinen satır yoksa yine "başarılı" | Yalnızca iki kişi aynı anda silerken oluşur; normal testle tetiklenmesi zor | yakalanmadı |
| Poliçe (ortak) | `PolicyService.cs:102` | Silmede tür uyuşmazlığı dalı boşaltıldı | Başka türden poliçeyi yanlış adresten silme (`404`/`01`, bilinçli sapma); hiçbir eşdeğerlik testi bu yolu çalıştırmıyor | yakalandı |
| Poliçe (ortak) | `PolicyRepository.cs:90`, `:109` | Müşteri **ve** poliçe no koşulu **veya** oldu | Başka müşterinin poliçesini güncelleme ya da silme | yakalandı |
| Ticari | `PolicyRepository.cs:56` | `SingleOrDefault` yerine `Single` (kayıt yoksa hata) | Olmayan ticari poliçeyi yalnız numarayla sorgulama | yakalanmadı |
| Poliçe (ortak) | `PolicyRepository.cs:117` | Zaman damgası yuvarlaması bozuldu | `lastChanged` hassasiyeti | yakalandı |
| Poliçe (ortak) | `UnitOfWork.cs:19` | Çakışma hatasının metni | Hata açıklaması (3. gruba benzer) | yakalanmadı |
| Motor | `MotorPoliciesController.cs:73` | Silme sonucu hep "başarılı" | Olmayan motor poliçesini silme | yakalanmadı |
| Ortak | `PolicyRepository.cs:18`, `:83`, `UnitOfWork.cs:27` | Bilinmeyen tür ve veritabanı hatası yolları | 4. grupla aynı | kapsanmadı |

En dikkat çekenler:

- **Yanlış türden poliçe** (`PolicyService.cs:117`, `CommercialPolicyService.cs:22`): `docs/api-design.md` §3'te bilinçli sapma
  olarak yazdığımız "tür uyuşmazsa `404`/`01`" kuralını hiçbir test sınamıyor. Kural kaldırılsa testler yine geçiyor.
- **Olmayan motor poliçesini silme** (`MotorPoliciesController.cs:73`): motor silmenin tek testi başarılı silmeyi sınıyor;
  hata cevabı dönmese de testler geçiyor. Konut, hayat ve ticari silmede bu durum test ediliyor.
- **Olmayan ticari poliçeyi yalnız numarayla sorgulama** (`PolicyRepository.cs:56`, `CommercialPoliciesController.cs:56`):
  `404` yerine hata ya da boş cevap dönse de testler geçiyor.
- **Başka müşterinin poliçesi** (`PolicyRepository.cs:90`, `:109`): eşdeğerlik testleri kaçırıyor, entegrasyon testleri yakalıyor.

## Bu skor ne söyler, ne söylemez

**Söyledikleri**

- API'nin iş kurallarının bulunduğu Application katmanında eşdeğerlik testleri mutantların %83'ünü yakalıyor.
- Testlerin kaçırdığı iş kuralı durumları somut olarak belli (yukarıdaki tablo). Bunlar #58 ve #57'de yazılacak eksik testlerin listesidir.
- Entegrasyon ve birim testleri eşdeğerlik testlerinin üzerine özellikle veri erişimi (Infrastructure) ve COMMAREA modelinde katkı yapıyor.

**Söylemedikleri**

- **Düşük toplam skor, testlerin kötü olduğu anlamına gelmez.** Yakalanmayanların büyük kısmı API'nin kullanmadığı kod
  (1. grup) ya da davranışı değiştirmeyen yapılandırma satırları (2. grup). Bu iki grubun hangi mutantlarının gerçekten
  eşdeğer olduğu tek tek incelenmedi; bu yüzden skordan ayrıca düşülmedi, yalnızca Domain'in tamamı ayrı gösterildi.
- **Yakalanan mutant, testin doğru davranışı sınadığını kanıtlamaz**; yalnızca o değişikliğin bir testi kırdığını gösterir.
- Stryker'ın atladığı (`Ignored`) ve derlenmeyen mutantlar skora girmez; atlananların nedeni Stryker'ın kendi kurallarıdır
  (örneğin log ve öznitelik metinleri).
- Sonuç tek bir çalıştırmadır (`692947d`). Kod ya da testler değiştikçe tekrar çalıştırılmalıdır: Actions > Mutasyon testi > Run workflow.

## Sonraki adım

- **#58 (Sercan):** müşteri, konut ve ticari dilimlerindeki boşluklar için eşdeğerlik testi yazılır, ardından ölçüm tekrarlanır.
- **#57 (Oğuz):** motor ve hayat sigortası dilimlerindeki boşluklar.
- Ortak poliçe satırları (`PolicyService`, `PolicyRepository`) bütün poliçe türlerini etkiler; her dilim kendi türü için yazar.
