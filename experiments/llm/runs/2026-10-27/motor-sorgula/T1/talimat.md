Sen deneyimli bir C# geliştiricisisin. Aşağıda IBM'in COBOL/CICS örnek uygulaması GenApp'ten bir operasyonun
COBOL kodu var. Bu operasyonu, GenApp'in yeni ASP.NET Core (.NET 10) sürümü için C#'a çevirmeni istiyorum.

## Görev
Operasyon: motor-sorgula
İstek kodu: 01IMOT
HTTP adresi ve yöntemi: GET /api/customers/{customerNumber}/motor-policies/{policyNumber}

Yalnızca bu operasyonu yaz. Başka operasyonları yazma.

## Kurallar
1. Davranış COBOL kodundan çıkarılır: dönüş kodları, hangi koşulda hangisinin döndüğü, girdi kontrolleri,
   eşzamanlı güncelleme kontrolü. Dönüş kodları API tasarımındaki tabloya göre HTTP cevabına çevrilir.
2. İstek ve cevap biçimi, adres, durum kodları ve hata gövdesi (`returnCode` alanı dâhil) "API tasarımı"
   belgesine birebir uymalıdır. Belgeyle COBOL arasında çelişki varsa belgede "bilinçli sapma" olarak
   yazılan davranışı uygula.
3. Veritabanı PostgreSQL'dir; tablo ve kolonlar "Veritabanı şeması" belgesindedir. Bağlantı dizesi
   `ConnectionStrings__GenApp` yapılandırmasından okunur. Şemayı değiştirme, migration yazma.
4. VSAM ve CICS'e özgü şeyler (EXEC CICS, COMMAREA, SYNCPOINT, kuyruklar) yeni sistemde yoktur; bunların
   işlevi belgede anlatıldığı gibi karşılanır (örn. VSAM yazımı Db2 yazımında zaten vardır).
5. Çıktın bir ASP.NET Core controller'ı ve ihtiyaç duyduğun diğer sınıflardır. Hepsi
   `GenApp.Llm.MotorSorgula` ad alanında olsun, başka operasyonun kodundan bağımsız çalışsın,
   controller'ın adresi tam yazılsın. Projede `Program` sınıfı hazırdır; sen yazma.
6. Yalnızca verilen belgelere ve COBOL koduna dayan. Bilmediğin bir davranışı uydurma; belirsizse en yakın
   COBOL satırına göre karar ver ve kodda tek satır yorumla belirt.

## Çıktı biçimi
Her dosya için önce yolunu bir başlık satırında (`### dosya: <yol>`), sonra içeriğini tek bir ```csharp
bloğunda ver. Dosya dışında açıklama yazma.

## Belgeler
### API tasarımı
# API tasarımı

Bu belge, GenApp'in yeni sürümündeki REST API'nin adreslerini, istek ve cevap biçimini ve eski
dönüş kodlarının HTTP karşılıklarını tanımlar. Kararların gerekçeleri
[ADR 0002](adr/0002-rest-api.md)'dedir. Gereksinimler için bkz. [SRS](srs.md).

Bu belge hangi koşulda hangi sonucun döneceğini **tanımlamaz**. O kurallar eski koddan çıkarılır
ve eşdeğerlik testleriyle doğrulanır.

## 1. Genel kurallar

| Konu | Kural |
|---|---|
| Kök adres | Bütün adresler `/api` ile başlar. |
| Biçim | İstek ve cevap gövdeleri JSON'dur (`application/json`). Alan adları camelCase yazılır. |
| Numaralar | Müşteri ve poliçe numaraları tam sayıdır (`1000001`); başta sıfır yazılmaz. |
| Tarihler | `yyyy-MM-dd` biçimindedir (`2026-10-15`). |
| Metinler | Cevaplardaki metinlerin sonundaki boşluklar kırpılır (`"Andrew"`, `"Andrew    "` değil). |
| Posta kodu | Müşteri ekle ve güncellede `postcode` büyük harfe çevrilerek kaydedilir (`"pi101oo"` → `"PI101OO"`). Eski sistemde bunu ekran programı yapar (`lgtestc1.cbl:126-127`, `:188-189`); yeni sistemde ekranın yerini API aldığı için API yapar. |
| Boş metin | Metin alanları boş gönderilebilir; eski sistem de boş değeri kontrol etmeden kaydeder. Boş bir metin cevapta `""` olarak döner, `null` dönmez. |
| Yalnızca cevapta olan alanlar | 4. bölümde "yalnızca cevapta" diye işaretli alanlar (`customerNumber`, `policyNumber`) istek gövdesinde gönderilirse yok sayılır; istek reddedilmez. Eski sistem de müşteri eklerken gelen numarayı yok sayıp yeni numara üretir (`lgacdb01.cbl:285`). |
| Uzunluk sınırları | Metin alanlarının en fazla uzunluğu eski sistemdeki alan uzunluğuyla aynıdır (bkz. 4). |
| Eşzamanlılık | Poliçe güncellemesi, sorgulamada dönen `lastChanged` değeriyle yapılır (bkz. 5). |
| Hata gövdesi | Hatalar RFC 9457 "Problem Details" biçiminde döner ve eski dönüş kodunu taşır (bkz. 3). |
| Açıklama sayfası | Geliştirme ortamında API'nin OpenAPI açıklaması `/swagger` adresinde görülebilir. |

## 2. Operasyonlar

`{customerNumber}` müşteri numarasını, `{policyNumber}` poliçe numarasını gösterir.

### 2.1 Müşteri

| # | Operasyon | Yöntem | Adres | Başarılı cevap | Eski program | İstek kodu |
|---|---|---|---|---|---|---|
| 1 | Müşteri ekle | POST | `/api/customers` | 201 + müşteri | LGACUS01 | `01ACUS` |
| 2 | Müşteri sorgula | GET | `/api/customers/{customerNumber}` | 200 + müşteri | LGICUS01 | `01ICUS` |
| 3 | Müşteri güncelle | PUT | `/api/customers/{customerNumber}` | 200 + müşteri | LGUCUS01 | `01UCUS` |

### 2.2 Motor poliçesi

| # | Operasyon | Yöntem | Adres | Başarılı cevap | Eski program | İstek kodu |
|---|---|---|---|---|---|---|
| 4 | Ekle | POST | `/api/customers/{customerNumber}/motor-policies` | 201 + poliçe | LGAPOL01 | `01AMOT` |
| 5 | Sorgula | GET | `/api/customers/{customerNumber}/motor-policies/{policyNumber}` | 200 + poliçe | LGIPOL01 | `01IMOT` |
| 6 | Güncelle | PUT | `/api/customers/{customerNumber}/motor-policies/{policyNumber}` | 200 + poliçe | LGUPOL01 | `01UMOT` |
| 7 | Sil | DELETE | `/api/customers/{customerNumber}/motor-policies/{policyNumber}` | 204 | LGDPOL01 | `01DMOT` |

### 2.3 Konut poliçesi

| # | Operasyon | Yöntem | Adres | Başarılı cevap | Eski program | İstek kodu |
|---|---|---|---|---|---|---|
| 8 | Ekle | POST | `/api/customers/{customerNumber}/house-policies` | 201 + poliçe | LGAPOL01 | `01AHOU` |
| 9 | Sorgula | GET | `/api/customers/{customerNumber}/house-policies/{policyNumber}` | 200 + poliçe | LGIPOL01 | `01IHOU` |
| 10 | Güncelle | PUT | `/api/customers/{customerNumber}/house-policies/{policyNumber}` | 200 + poliçe | LGUPOL01 | `01UHOU` |
| 11 | Sil | DELETE | `/api/customers/{customerNumber}/house-policies/{policyNumber}` | 204 | LGDPOL01 | `01DHOU` |

### 2.4 Hayat sigortası poliçesi (endowment)

| # | Operasyon | Yöntem | Adres | Başarılı cevap | Eski program | İstek kodu |
|---|---|---|---|---|---|---|
| 12 | Ekle | POST | `/api/customers/{customerNumber}/endowment-policies` | 201 + poliçe | LGAPOL01 | `01AEND` |
| 13 | Sorgula | GET | `/api/customers/{customerNumber}/endowment-policies/{policyNumber}` | 200 + poliçe | LGIPOL01 | `01IEND` |
| 14 | Güncelle | PUT | `/api/customers/{customerNumber}/endowment-policies/{policyNumber}` | 200 + poliçe | LGUPOL01 | `01UEND` |
| 15 | Sil | DELETE | `/api/customers/{customerNumber}/endowment-policies/{policyNumber}` | 204 | LGDPOL01 | `01DEND` |

### 2.5 Ticari poliçe

| # | Operasyon | Yöntem | Adres | Başarılı cevap | Eski program | İstek kodu |
|---|---|---|---|---|---|---|
| 16 | Ekle | POST | `/api/customers/{customerNumber}/commercial-policies` | 201 + poliçe | LGAPOL01 | `01ACOM` |
| 17a | Sorgula: müşteri ve poliçe numarasıyla | GET | `/api/customers/{customerNumber}/commercial-policies/{policyNumber}` | 200 + poliçe | LGIPOL01 | `01ICOM` |
| 17b | Sorgula: poliçe numarasıyla | GET | `/api/commercial-policies/{policyNumber}` | 200 + poliçe | LGIPOL01 | `02ICOM` |
| 17c | Sorgula: müşteri numarasıyla | GET | `/api/customers/{customerNumber}/commercial-policies` | 200 + poliçe listesi | LGIPOL01 | `03ICOM` |
| 17d | Sorgula: posta koduyla | GET | `/api/commercial-policies?postcode={postcode}` | 200 + poliçe listesi | LGIPOL01 | `05ICOM` |
| 18 | Sil | DELETE | `/api/customers/{customerNumber}/commercial-policies/{policyNumber}` | 204 | LGDPOL01 | `01DCOM` |

Ticari poliçe için güncelleme adresi yoktur; eski sistemde bu işlem yoktur.

**Eşdeğerlik testleri için operasyon adları** (`tests/active-operations.txt` ve
`[Trait("Operasyon", ...)]`): `musteri-ekle`, `musteri-sorgula`, `musteri-guncelle`, `motor-ekle`,
`motor-sorgula`, `motor-guncelle`, `motor-sil`, `konut-…`, `hayat-…`, `ticari-ekle`,
`ticari-sorgula` (17a–17d'nin hepsi), `ticari-sil`.

## 3. Dönüş kodlarının HTTP karşılığı

Başarılı işlemler yukarıdaki tablolardaki kodla döner (200, 201 ya da 204). Hatalı işlemler
aşağıdaki tabloya göre döner. Hata gövdesindeki `returnCode` alanı eski sistemin dönüş kodunu
taşır; böylece aynı HTTP koduna düşen farklı durumlar (örneğin `01` ve `70`) ayırt edilebilir ve
eşdeğerlik testleri eski kodla doğrudan karşılaştırma yapabilir.

| Eski kod | Anlamı (eski sistemde) | HTTP | `type` |
|---|---|---|---|
| `00` | Başarılı | 200 / 201 / 204 | (hata gövdesi yok) |
| `01` | Kayıt bulunamadı | 404 Not Found | `not-found` |
| `02` | Kayıt, okunduktan sonra başkası tarafından değiştirilmiş | 409 Conflict | `concurrency-conflict` |
| `70` | Poliçe eklenirken müşteri bulunamadı | 404 Not Found | `customer-not-found` |
| `80` | VSAM dosyasına yazılamadı | 500 Internal Server Error | `storage-error` |
| `81` | VSAM dosyasından okunamadı ya da silinemedi | 500 Internal Server Error | `storage-error` |
| `82` | VSAM kaydı güncellenemedi | 500 Internal Server Error | `storage-error` |
| `88` | Veritabanı imleci kapatılamadı | 500 Internal Server Error | `database-error` |
| `89` | Veritabanı imleci açılamadı | 500 Internal Server Error | `database-error` |
| `90` | Veritabanı hatası | 500 Internal Server Error | `database-error` |
| `98` | COMMAREA beklenenden kısa | 400 Bad Request | `validation-error` |
| `99` | İstek kodu tanınmadı | (oluşmaz) | — |

Notlar:
- **`80`, `81`, `82`:** Yeni sistemde VSAM için ayrı bir depolama yoktur (bkz. `docs/database-schema.md`; kesin karar ADR 0003'te). Bu kodlar eşleme tablosunda
  bütünlük için yer alır; yeni sistemde oluşmaları beklenmez.
- **`98`:** Yeni sistemde COMMAREA yoktur. Eksik ya da hatalı alan içeren istekler ASP.NET Core'un
  doğrulamasıyla 400 olarak reddedilir ve gövdede `returnCode` yerine hatalı alanların listesi
  (`errors`) döner. COMMAREA uzunluk kuralları yalnızca GnuCOBOL'da test edilir.
- **`99`:** Yeni sistemde işlem türünü adres belirler; tanınmayan bir işlem için istek kodu
  gönderilemez. Var olmayan bir adres ASP.NET Core tarafından 404 olarak döner.
- **Kilitlenme ve zaman aşımı:** Bütün operasyonlarda veritabanı kilitlenmesi ya da zaman aşımı
  `503 Service Unavailable` (`type: database-busy`, `returnCode: "90"`) olarak döner. Eski sistemden
  bilinçli farkı ADR 0002'de (Karar 8) açıklanmıştır.

### Bilinçli sapmalar ve korunan davranışlar

Yeni sistem eski sistemle aynı davranır. Aşağıdaki durumlarda eski kodun yaptığı ile niyeti
ayrışıyor ya da eski davranış yeni sistemde oluşamıyor; burada ne yapıldığı tek tek yazılmıştır.
Gerekçeler ADR 0002'dedir (Karar 8 ve 9). Eşdeğerlik ölçümünde bu satırlar "bilinçli sapma" olarak
raporlanır, hata sayılmaz.

| Durum | Eski sistem | Yeni sistem | Tür |
|---|---|---|---|
| Poliçe güncelle: `lastChanged` uyuşmuyor | `02` yazılır (`lgupdb01.cbl:346`), imleç kapatılırken `00` ile ezilir (`:360`, `:371`). Db2'deki kayıt güncellenmez, ama ardından `LGUPVS01` çağrılır (`:209`) ve VSAM kaydını reddedilen isteğin verileriyle yeniden yazar (`lgupvs01.cbl:139-159`); çağırana `00` döner | `409`, `returnCode: "02"` | Bilinçli sapma |
| Poliçe güncelle: poliçe yok | `01` yazılır (`lgupdb01.cbl:352`), aynı şekilde `00` ile ezilir. Ardından `LGUPVS01` kaydı VSAM'da bulamaz, `81` yazar ve program `LGV3` ile durur (`lgupvs01.cbl:139-151`); çağırana cevap dönmez | `404`, `returnCode: "01"` | Bilinçli sapma |
| Poliçe güncelle: poliçe okunurken veritabanı hatası | `90` yazılır (`lgupdb01.cbl:354`), aynı şekilde `00` ile ezilir. Db2'deki kayıt güncellenmez; ardından `LGUPVS01` VSAM kaydını yeniden yazar ve çağırana `00` döner (kayıt VSAM'da yoksa `81` ile durur, `lgupvs01.cbl:139-151`) | `500`, `returnCode: "90"` | Bilinçli sapma |
| Poliçe güncelle: tür tablosundaki satır güncellenemedi | `01` ya da `90` yazılır (konut: `lgupdb01.cbl:444-451`; motor: `:485-492`), imleç kapatılırken `00` ile ezilir ve program döner (`:302-307`); hiçbir şey güncellenmez, VSAM'a gidilmez, çağırana `00` döner | `404` / `"01"` ya da `500` / `"90"` | Bilinçli sapma |
| Poliçe güncelle: imleç kapatılırken `-501` | `-501` de `00` sayılır (`lgupdb01.cbl:372-375`). Bu yol, `POLICY` güncellemesinden sonraki `SELECT` başarısız olup `SYNCPOINT ROLLBACK` imleci kapattığında oluşur (`:336-342`): yazılan `90` ezilir, her şey geri alınmıştır ama çağırana `00` döner | `500`, `returnCode: "90"` | Bilinçli sapma |
| Müşteri güncelle: müşteri yok | Db2'de `01` yazılır (`lgucdb01.cbl:173-174`), ardından VSAM okuması başarısız olur ve program `LGV1` ile durur (`lgucvs01.cbl:77-81`); çağırana cevap dönmez | `404`, `returnCode: "01"` | Bilinçli sapma (yeni sistemde VSAM yok) |
| Kilitlenme ve zaman aşımı | Müşteri sorgulamada `01` (`lgicdb01.cbl:197-198`); diğer operasyonlarda o adımın genel hata kodu (çoğunlukla `90`, ticari liste sorgularında `88`/`89`) | `503`, `returnCode: "90"` | Bilinçli sapma (Karar 8) |
| Müşteri ekle/güncelle: doğum tarihi boş ya da geçerli bir tarih değil | Db2'nin `DATE` sütunu (`db2cre.jcl:115`) değeri kabul etmez ve işlem `90` ile biter; boş değerin sonucu tahminde belirsizdir (01-09) | `400`, `type: validation-error` | Bilinçli sapma (hatalı girdi veritabanına ulaşmadan reddedilir) |
| Poliçe sil: poliçe yok | `90` döner (`lgdpdb01.cbl:198-199`); kodun hemen üstündeki yorum "bulunamadı başarılı sayılsın" der, ama kod bunu yapmaz | `500`, `returnCode: "90"` | **Korunan davranış:** eski sistemle aynı |
| Poliçe sorgula, güncelle, sil: poliçe başka türden (örneğin motor poliçesi konut adresinden) | Programlar `POLICY.POLICYTYPE`'ı hiç okumaz. **Sorgula:** tür tablosunda satır yoksa zaten `01` döner (konut: `lgipdb01.cbl:469-475`, `:512-514`); satır varsa türü farklı olsa da veri döner (örnek veride poliçe 7: türü `C`, ama `HOUSE` satırı var, `db2cre.jcl:901`, `:1068-1074`). **Güncelle:** tür tablosu güncellemesi satır bulamaz, `01` yazılır ve `00` ile ezilir (bir üst satırdaki yol); çağırana `00` döner. **Sil:** poliçe türüne bakılmadan silinir (`lgdpdb01.cbl:189-194`), tür tablosundaki satır yabancı anahtar kuralıyla gider; ardından VSAM kaydı adresteki türün harfiyle aranır, bulunamaz ve `81` döner (`lgdpvs01.cbl:77-91`); Db2'deki silme geri alınmaz | `policyType` adresteki türle uyuşmuyorsa (konut `H`, motor `M`, hayat `E`, ticari `C`) işlem yapılmaz: `404`, `returnCode: "01"` | Bilinçli sapma (yanlış türden poliçenin değişmesini ya da silinmesini önler) |
| Müşteri ya da poliçe numarası 10 haneli | Numaralar Db2'ye gitmeden önce 9 haneli `PIC S9(9) COMP` alanlara taşınır (örneğin `lgapdb01.cbl:90`, `:176`; `lgupdb01.cbl:87-88`; `lgdpdb01.cbl:82-83`). Derleme seçeneklerinde `TRUNC` belirtilmediği için (`cobol.jcl:44-50`) derleyicinin varsayılanıyla en soldaki hane kesilir: `1000000004` müşteri 4 olur ve işlem o müşteri üzerinde yapılır (tahmin 08-14) | Numara kesilmez. O numarada kayıt yoksa `404`, `returnCode: "01"` (poliçe eklemede müşteri yoksa `"70"`). Tam sayı sınırını (2147483647) aşan bir numara adresle eşleşmez ve ASP.NET Core'un 404'ü döner (bkz. `99` notu) | Bilinçli sapma (başka müşterinin kaydının değişmesini önler) |
| Poliçe güncelle: `payment` | `POLICY` güncellemesinin `SET` listesinde `PAYMENT` yoktur (`lgupdb01.cbl:319-325`); gönderilen ödeme yok sayılır, kayıttaki değer kalır (tahmin 10-18) | İstekteki `payment` yok sayılır; kayıttaki değer değişmez ve cevapta kayıttaki değer döner | **Korunan davranış:** eski sistemle aynı |
| Ticari sorgula, müşteri ya da posta koduyla (17c, 17d): birden fazla kayıt | Her `FETCH` aynı COMMAREA alanlarının üzerine yazar (`lgipdb01.cbl:901-904`, `:983-986`); çağırana yalnızca son kayıt kalır. Müşteri numarasıyla sorgu en fazla 21 kayıt okur (`:906-908`), posta koduyla sorguda sınır yoktur | Bulunan kayıtların hepsi liste olarak döner | Bilinçli sapma (eski davranış kayıtları kaybettirir) |
| Ticari sorgula, müşteri ya da posta koduyla (17c, 17d): kayıt yok | Döngü `SQLCODE = 100` ile biter, dönüş kodu `00` kalır (`lgipdb01.cbl:850`, `:933`) | `200` ve boş liste (`[]`) | **Korunan davranış:** `00`'ın karşılığı |
| Ticari sorgula, müşteri ya da posta koduyla (17c, 17d): `FETCH` sırasında veritabanı hatası | Döngü `SQLCODE > 0` olana kadar sürer (`lgipdb01.cbl:850`, `:933`); negatif `SQLCODE` döngüyü bitirmez, program aynı hatayı almaya devam eder | `500`, `returnCode: "90"` (kilitlenme ve zaman aşımında `503`, Karar 8) | Bilinçli sapma (eski davranış bitmeyen bir döngüdür, kopyalanamaz) |
| Ticari sorgula (17a-17d): `brokerId`, `brokersReference`, `payment` | Ticari sorguların hiçbiri bu sütunları okumaz (örneğin `lgipdb01.cbl:630-678`, `:866-888`); çağırana veritabanındaki değerler gitmez | Kayıttaki değerler döner | Bilinçli sapma (ortak alanlar dört türde de aynı anlamı taşır, bkz. 4.2) |
| Konut ekle ve güncelle: `propertyType` boş, `bedrooms` 0, `value` 0 | Hiçbir programda konut alanı denetimi yoktur; değerler olduğu gibi kaydedilir (`lgapdb01.cbl:405-425`; `HOUSE` tablosunda `CHECK` kısıtı yok, `db2cre.jcl:240-250`; tahmin 08-04) | Kabul edilir ve kaydedilir | **Korunan davranış:** eski sistemle aynı |

### Hata gövdesi örneği

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "not-found",
  "title": "Kayıt bulunamadı",
  "status": 404,
  "detail": "1000123 numaralı müşteri bulunamadı.",
  "returnCode": "01"
}
```

## 4. Veri alanları

Uzunluklar eski COMMAREA'daki (`base/src/lgcmarea.cpy`) alan uzunluklarıdır. Daha uzun bir değer
gönderilirse istek 400 ile reddedilir.

### 4.1 Müşteri

| JSON alanı | Tür | En fazla | Eski alan |
|---|---|---|---|
| `customerNumber` | tam sayı | — | `CA-CUSTOMER-NUM` (yalnızca cevapta) |
| `firstName` | metin | 10 | `CA-FIRST-NAME` |
| `lastName` | metin | 20 | `CA-LAST-NAME` |
| `dateOfBirth` | tarih | — | `CA-DOB` |
| `houseName` | metin | 20 | `CA-HOUSE-NAME` |
| `houseNumber` | metin | 4 | `CA-HOUSE-NUM` |
| `postcode` | metin | 8 | `CA-POSTCODE` |
| `phoneMobile` | metin | 20 | `CA-PHONE-MOBILE` |
| `phoneHome` | metin | 20 | `CA-PHONE-HOME` |
| `email` | metin | 100 | `CA-EMAIL-ADDRESS` |

### 4.2 Poliçe: ortak alanlar (dört türde de)

| JSON alanı | Tür | En fazla | Eski alan |
|---|---|---|---|
| `policyNumber` | tam sayı | — | `CA-POLICY-NUM` (yalnızca cevapta) |
| `customerNumber` | tam sayı | — | `CA-CUSTOMER-NUM` (yalnızca cevapta) |
| `issueDate` | tarih | — | `CA-ISSUE-DATE` |
| `expiryDate` | tarih | — | `CA-EXPIRY-DATE` |
| `lastChanged` | metin | — | `CA-LASTCHANGED` (bkz. 5) |
| `brokerId` | tam sayı | 10 hane | `CA-BROKERID` |
| `brokersReference` | metin | 10 | `CA-BROKERSREF` |
| `payment` | tam sayı | 6 hane | `CA-PAYMENT` |

### 4.3 Motor poliçesi

| JSON alanı | Tür | En fazla | Eski alan |
|---|---|---|---|
| `make` | metin | 15 | `CA-M-MAKE` |
| `model` | metin | 15 | `CA-M-MODEL` |
| `value` | tam sayı | 6 hane | `CA-M-VALUE` |
| `registrationNumber` | metin | 7 | `CA-M-REGNUMBER` |
| `colour` | metin | 8 | `CA-M-COLOUR` |
| `cc` | tam sayı | 4 hane | `CA-M-CC` |
| `manufactureDate` | tarih | — | `CA-M-MANUFACTURED` |
| `premium` | tam sayı | 6 hane | `CA-M-PREMIUM` |
| `accidents` | tam sayı | 6 hane | `CA-M-ACCIDENTS` |

### 4.4 Konut poliçesi

| JSON alanı | Tür | En fazla | Eski alan |
|---|---|---|---|
| `propertyType` | metin | 15 | `CA-H-PROPERTY-TYPE` |
| `bedrooms` | tam sayı | 3 hane | `CA-H-BEDROOMS` |
| `value` | tam sayı | 8 hane | `CA-H-VALUE` |
| `houseName` | metin | 20 | `CA-H-HOUSE-NAME` |
| `houseNumber` | metin | 4 | `CA-H-HOUSE-NUMBER` |
| `postcode` | metin | 8 | `CA-H-POSTCODE` |

### 4.5 Hayat sigortası poliçesi

| JSON alanı | Tür | En fazla | Eski alan |
|---|---|---|---|
| `withProfits` | metin | 1 | `CA-E-WITH-PROFITS` |
| `equities` | metin | 1 | `CA-E-EQUITIES` |
| `managedFund` | metin | 1 | `CA-E-MANAGED-FUND` |
| `fundName` | metin | 10 | `CA-E-FUND-NAME` |
| `term` | tam sayı | 2 hane | `CA-E-TERM` |
| `sumAssured` | tam sayı | 6 hane | `CA-E-SUM-ASSURED` |
| `lifeAssured` | metin | 31 | `CA-E-LIFE-ASSURED` |

`withProfits`, `equities` ve `managedFund` eski sistemdeki gibi tek karakterlik metin olarak
taşınır; değerlerin yorumu eski koda bırakılır.

### 4.6 Ticari poliçe

| JSON alanı | Tür | En fazla | Eski alan |
|---|---|---|---|
| `address` | metin | 255 | `CA-B-Address` |
| `postcode` | metin | 8 | `CA-B-Postcode` |
| `latitude` | metin | 11 | `CA-B-Latitude` |
| `longitude` | metin | 11 | `CA-B-Longitude` |
| `customer` | metin | 255 | `CA-B-Customer` |
| `propertyType` | metin | 255 | `CA-B-PropType` |
| `firePeril` | tam sayı | 4 hane | `CA-B-FirePeril` |
| `firePremium` | tam sayı | 8 hane | `CA-B-FirePremium` |
| `crimePeril` | tam sayı | 4 hane | `CA-B-CrimePeril` |
| `crimePremium` | tam sayı | 8 hane | `CA-B-CrimePremium` |
| `floodPeril` | tam sayı | 4 hane | `CA-B-FloodPeril` |
| `floodPremium` | tam sayı | 8 hane | `CA-B-FloodPremium` |
| `weatherPeril` | tam sayı | 4 hane | `CA-B-WeatherPeril` |
| `weatherPremium` | tam sayı | 8 hane | `CA-B-WeatherPremium` |
| `status` | tam sayı | 4 hane | `CA-B-Status` |
| `rejectionReason` | metin | 255 | `CA-B-RejectReason` |

## 5. Eşzamanlı güncelleme

Poliçe sorgulamasının cevabında `lastChanged` alanı döner. Güncelleme isteği bu değeri aynen geri
gönderir; değerin biçimini sunucu belirler, istemci onu değiştirmeden kullanır. Sunucu, kayıttaki değerle istekteki değeri karşılaştırır; kayıt arada değişmişse
güncelleme yapılmaz ve `409 Conflict` (`returnCode: "02"`) döner. İstemci kaydı yeniden sorgulayıp
tekrar denemelidir. Bu, eski sistemdeki `LASTCHANGED` alanının karşılığıdır. Eski kod bu durumda
`02` yazar ama çağırana `00` döndürür; yeni sistemin `409` döndürmesi bilinçli bir sapmadır (bkz. 3,
"Bilinçli sapmalar ve korunan davranışlar"). Müşteri güncellemesinde böyle bir kontrol yoktur; eski
sistemde de yoktur.

## 6. Örnek istekler

**Müşteri ekle**

```http
POST /api/customers
Content-Type: application/json

{
  "firstName": "Ayşe",
  "lastName": "Yılmaz",
  "dateOfBirth": "1990-04-12",
  "houseName": "",
  "houseNumber": "12",
  "postcode": "06830",
  "phoneMobile": "05551234567",
  "phoneHome": "",
  "email": "ayse@example.com"
}
```

```http
HTTP/1.1 201 Created
Location: /api/customers/1000001
Content-Type: application/json

{ "customerNumber": 1000001, "firstName": "Ayşe", "lastName": "Yılmaz", ... }
```

**Motor poliçesi güncelle**

```http
PUT /api/customers/1000001/motor-policies/1000005
Content-Type: application/json

{
  "issueDate": "2026-01-01",
  "expiryDate": "2027-01-01",
  "lastChanged": "2026-10-15 10:22:31.123456",
  "brokerId": 0,
  "brokersReference": "",
  "payment": 0,
  "make": "Ford",
  "model": "Focus",
  "value": 15000,
  "registrationNumber": "06AB123",
  "colour": "Blue",
  "cc": 1600,
  "manufactureDate": "2020-05-01",
  "premium": 450,
  "accidents": 0
}
```


### Veritabanı şeması
# Veritabanı şeması (Db2 → PostgreSQL)

GenApp'in 8 Db2 tablosunun PostgreSQL karşılığı ve bu eşlemede alınan kararlar. Şema EF Core migration'ı ile
oluşturulur (`src/GenApp.Infrastructure/Migrations/`); kaynak tanımlar `base/cntl/db2cre.jcl` dosyasındadır.

İlgili görevler: #15 (bu şema), #75 (ADR 0003: Db2 ve VSAM'den PostgreSQL'e veri ve transaction kararı).

## Tablo eşlemesi

| Db2 tablosu (`db2cre.jcl`) | PostgreSQL tablosu | C# sınıfı (`GenApp.Domain`) |
|---|---|---|
| `customer` (:110-122) | `customer` | `Customers.Customer` |
| `customer_secure` (:130-138) | `customer_secure` | `Customers.CustomerSecure` |
| `policy` (:161-176) | `policy` | `Policies.Policy` |
| `endowment` (:203-216) | `endowment` | `Policies.EndowmentPolicy` |
| `house` (:240-250) | `house` | `Policies.HousePolicy` |
| `motor` (:275-288) | `motor` | `Policies.MotorPolicy` |
| `commercial` (:313-336) | `commercial` | `Policies.CommercialPolicy` |
| `claim` (:361-373) | `claim` | `Claims.Claim` |

Tablo adları Db2'deki gibidir. Kolon adları snake_case'e çevrilir (`firstName` → `first_name`, `phonehome` → `phone_home`).

## Tip eşlemesi

| Db2 | PostgreSQL | C# | Not |
|---|---|---|---|
| `CHAR(n)` | `varchar(n)` | `string?` | Aşağıya bakın |
| `VARCHAR(n)` | `varchar(n)` | `string?` | Yalnızca `endowment.paddingData` (32606) |
| `INTEGER` | `integer` | `int` | |
| `SMALLINT` | `smallint` | `short` | |
| `DATE` | `date` | `DateOnly` | |
| `TIMESTAMP` | `timestamp without time zone` | `DateTime` | Aşağıya bakın |

`NOT NULL` olmayan Db2 kolonları C#'ta nullable'dır (`?`).

**`CHAR(n)` neden `varchar(n)`?** Db2'nin `CHAR(n)` tipi değeri boşlukla doldurur (`"Ali       "`). PostgreSQL'de `char(n)`
kullanılsaydı bu boşluklar C#'a da gelir, `== "Ali"` karşılaştırmaları başarısız olurdu. Uzunluk sınırı `varchar(n)` ile
korunur. COBOL'un sabit genişlikli alanlarından gelen boşluk doldurma ve kesme davranışı veritabanının değil COMMAREA
modelinin (#20) ve girdi kontrollerinin konusudur. PostgreSQL sınırı aşan değeri **kesmez, hata verir**
(`value too long for type character varying(n)`); COBOL ise `MOVE` sırasında sessizce keser.

**`TIMESTAMP` neden açıkça `timestamp without time zone`?** Npgsql, C# `DateTime` tipini varsayılan olarak
`timestamp with time zone` tipine eşler. Db2 `TIMESTAMP` saat dilimi tutmadığı için tip açıkça belirtilmiştir
(`policy.last_changed`, `commercial.request_date`). Bu kolonlara yazılan `DateTime` değerlerinin `Kind` özelliği `Utc`
olmamalıdır; Npgsql `Utc` değeri saat dilimsiz kolona yazmayı reddeder.

## Korunan davranışlar

- **Numaralar 1000001'den başlar.** `customer.customer_number`, `policy.policy_number`, `claim.claim_number`:
  `GENERATED BY DEFAULT AS IDENTITY (START WITH 1000001)`. `BY DEFAULT` sayesinde örnek veri belirli numaralarla
  eklenebilir.
- **Sayaçlar transaction'a dahil değildir.** `ROLLBACK` edilen bir ekleme numarayı geri vermez; sonraki kayıt bir
  sonraki numarayı alır (Db2'de de aynısı geçerli, `CACHE 20` ayrıca boşluk oluşturabilir). Tahminlerdeki "ilk kimlik
  değeri `START WITH` değeridir" ön koşulu (B6) için test veritabanında sayaçlar sıfırlanmalıdır:
  `ALTER TABLE customer ALTER COLUMN customer_number RESTART WITH 1000001;`
- **Yedi yabancı anahtarın hepsi `ON DELETE CASCADE`.** `LGDPDB01` poliçe silerken yalnızca `policy` tablosundan siler
  ve tür tablosundaki satırın silinmesini bu kurala bırakır (`lgdpdb01.cbl:183-184`).
- **İyimser kilitleme: `policy.last_changed`.** `NOT NULL DEFAULT CURRENT_TIMESTAMP`, EF Core'da concurrency token.
  `LGUPDB01` güncellemeden önce COMMAREA'daki zaman damgasını veritabanındakiyle karşılaştırır (`lgupdb01.cbl:278`),
  farklıysa `CA-RETURN-CODE = '02'` döner (`:346`), eşitse günceller ve zaman damgasını yeniler (`:322`). EF Core
  `UPDATE ... WHERE last_changed = @eski` üretir; eşleşme yoksa `DbUpdateConcurrencyException` fırlatır. Bunun `02`
  dönüş koduna çevrilmesi ve güncellemede zaman damgasının yenilenmesi operasyon kodunun (#31 ve sonrası) işidir.
- **`policy.customer_number` indeksi.** Db2'deki `iPolicy2` (`db2cre.jcl:183`) EF Core tarafından yabancı anahtar
  indeksi olarak otomatik oluşturulur (`ix_policy_customer_number`).

## VSAM dosyaları

GenApp müşteri ve poliçe kayıtlarını Db2'ye ek olarak iki VSAM KSDS dosyasına da yazar:

| Dosya | Anahtar | Kayıt | Yazan / okuyan programlar |
|---|---|---|---|
| `KSDSCUST` | Müşteri no, 10 bayt (`adef121.jcl:24`) | 225 bayt (`adef121.jcl:25`, `lgacvs01.cbl:49`) | `LGACVS01`, `LGUCVS01`, `LGICVS01` |
| `KSDSPOLY` | Poliçe türü (1) + müşteri no (10) + poliçe no (10) = 21 bayt (`adef121.jcl:49`, `lgapvs01.cbl:26-29`) | 64 bayt (`adef121.jcl:50`) | `LGAPVS01`, `LGUPVS01`, `LGDPVS01`, `LGIPVS01` |

**Yeni şemada VSAM için ayrı tablo yoktur.** `KSDSCUST` kayıtlarının karşılığı `customer`, `KSDSPOLY` kayıtlarının
karşılığı `policy` ve ilgili tür tablosudur. Gerekçe:

- VSAM kayıtları Db2'deki verinin kopyasıdır (`KSDSPOLY` ayrıca kısaltılmış bir kopyadır); yeni bilgi taşımaz.
- `base/Architecture.md:48-49`: *"The application uses a two-phase commit to store customer and policy data in Db2 and,
  if successful, it updates VSAM. The requirement for a VSAM file is not a best practice, but is provided as a way to
  test two-phase commit processing in CICS."*
- Program envanterine göre kapsamdaki sorgulama operasyonları Db2'den okur (`LGICDB01`, `LGIPDB01`). VSAM'dan okuyan
  `LGICVS01` ve `LGIPVS01` rastgele kayıt seçen test/demo yardımcılarıdır.

**ADR 0003'e (#75) bırakılanlar:**
- VSAM'ın kaldırılmasıyla Db2 + VSAM iki aşamalı commit'in yerini tek bir PostgreSQL transaction'ının alıp almayacağı
  ve hata durumlarında (GenApp'te VSAM yazımı başarısız olursa dönen kodlar) davranışın nasıl korunacağı.
- **Tutarsızlık:** `KSDSCUST` kayıt boyutu VSAM tanımında ve `LGACVS01`'de 225 bayt, CICS dosya tanımında ise 82 bayt
  (`cdef121.jcl:139`). Şemayı etkilemiyor ama analizde not edilmeli.

## `claim` tablosu

Tablo oluşturulmuştur ama **GenApp'te hiçbir program kullanmaz**: hiçbir COBOL programında `claim` tablosuna SQL
erişimi yoktur, `db2cre.jcl`'de örnek verisi yoktur. Hazırlık izleri vardır: COMMAREA'da hasar alanları
(`lgcmarea.cpy:95-100`), BMS'de "Claim Inquiry / Claim Add" ekranı (`ssmap.bms:603+`) ve `LGIPDB01`'de kullanılmayan
bir değişken (`lgipdb01.cbl:169`). Kapsamdaki 18 operasyonun hiçbiri bu tabloya dokunmaz.

## Yerelde kurulum

    docker compose up -d postgres
    dotnet tool restore
    dotnet ef database update --project src/GenApp.Infrastructure --startup-project src/GenApp.Api


### Mimari belge (bölüm 4 ve 8)
## 4. Katmanlar ve bağımlılık kuralları

Ayrıntı ve gerekçe: [ADR 0001](adr/0001-layered-architecture.md).

```
GenApp.Api ──────────────► GenApp.Application ──► GenApp.Domain
    │                              ▲                    ▲
    └──► GenApp.Infrastructure ────┴────────────────────┘
```

| Katman | Görevi | Referans verebileceği |
|---|---|---|
| `GenApp.Domain` | Varlıklar ve kendi verisine bakan kurallar; hiçbir pakete bağlı değil | — |
| `GenApp.Application` | Operasyon servisleri, girdi kuralları, dönüş kodu üretimi, transaction sınırı; veri erişim arayüzleri | Domain |
| `GenApp.Infrastructure` | EF Core, `GenAppDbContext`, repository ve unit of work uygulamaları, migration'lar | Application, Domain |
| `GenApp.Api` | Controller'lar, istek/cevap tipleri, dönüş kodu → HTTP eşlemesi, web arayüzü, DI kayıtları | Application, Infrastructure (yalnızca kayıt için) |

Temel kurallar:

- Bağımlılıklar içe, Domain'e doğru akar (NFR-09); kural `ProjectReference`'larla derleyicide korunur.
- Application veri erişimi için arayüz tanımlar, Infrastructure uygular (Dependency Inversion).
- Transaction'ı Application, `IUnitOfWork` üzerinden açar (ADR 0003 Karar 2).
- Dönüş kodunu (`00`, `01`, `02`, `90`...) Application üretir, HTTP'ye Api çevirir (ADR 0002).
- Bütün kayıtlar `Scoped`'dır: bir HTTP isteği = bir operasyon = bir `DbContext` = bir transaction.

## 8. Eski sistemden yeni sisteme eşleme

### 8.1 Katman ve programlar

| Eski | Yeni | Not |
|---|---|---|
| 3270 ekranları: LGTESTC1 (`SSC1`, müşteri) | Web arayüzünde müşteri ekranı + `CustomersController` | |
| LGTESTP1 (`SSP1`, motor), LGTESTP2 (`SSP2`, hayat), LGTESTP3 (`SSP3`, konut), LGTESTP4 (`SSP4`, ticari) | Poliçe türü başına bir ekran + bir controller | |
| BMS haritası (`ssmap.bms`) | HTML formları | |
| LGACUS01, LGICUS01, LGUCUS01 | `CustomerService` (ekle, sorgula, güncelle) | |
| LGAPOL01, LGIPOL01, LGUPOL01, LGDPOL01 | `MotorPolicyService`, `HousePolicyService`, `EndowmentPolicyService`, `CommercialPolicyService` | Eskide poliçe türü `CA-REQUEST-ID` ile seçilir, yenide adresle |
| LGACDB01, LGACDB02, LGICDB01, LGUCDB01 | `CustomerRepository` | |
| LGAPDB01, LGIPDB01, LGUPDB01, LGDPDB01 | `PolicyRepository` | |
| LGACVS01, LGUCVS01, LGAPVS01, LGUPVS01, LGDPVS01 | karşılığı yok | VSAM yazımı Db2 yazımında zaten var (ADR 0003 Karar 1) |
| LGICVS01, LGIPVS01 | karşılığı yok | Rastgele kayıt seçen test yardımcıları, kapsam dışı |
| LGSTSQ (hata kuyruğu `GENAERRS`) | ASP.NET Core loglama (`ILogger`) | SRS Karar 1, NFR-15 |
| LGASTAT1, LGSETUP, LGWEBST5 | karşılığı yok | Kapsam dışı (SRS 6) |

### 8.2 Kavramlar

| Eski | Yeni | Kaynak |
|---|---|---|
| `EXEC CICS LINK` | Yapıcıya verilen arayüz üzerinden metot çağrısı (DI) | ADR 0001 |
| COMMAREA (`lgcmarea.cpy`, 32.500 bayt, tek ortak alan) | API'de JSON istek/cevap tipleri; Domain'de COMMAREA modeli (#20) | ADR 0002, #20 |
| `CA-REQUEST-ID` (`01AMOT` gibi) | Adres + HTTP yöntemi | ADR 0002 Karar 2 |
| `CA-RETURN-CODE` | Servis sonucundaki dönüş kodu → HTTP durum kodu + `returnCode` | ADR 0002 Karar 5 |
| Db2 | PostgreSQL | ADR 0003 |
| VSAM (`KSDSCUST`, `KSDSPOLY`) | karşılığı yok (`customer`, `policy` + tür tabloları) | ADR 0003 Karar 1 |
| CICS unit of work, iki aşamalı commit, `SYNCPOINT ROLLBACK`, kasıtlı ABEND | Application'da `IUnitOfWork` ile tek PostgreSQL transaction'ı | ADR 0003 Karar 2 |
| `LASTCHANGED` karşılaştırması (`lgupdb01.cbl:278`) | EF Core concurrency token | ADR 0003 Karar 3 |
| CICS named counter (`GENACUSTNUM`) / Db2 identity | PostgreSQL identity (1000001'den) | ADR 0003 Karar 4 |
| CICS transaction kodları (`SSC1`, `SSP1`...) | Web arayüzündeki sayfalar | |
| `EIBCALEN` kontrolü (COMMAREA yoksa ABEND, kısaysa `98`) | ASP.NET Core model doğrulaması (400) | Bölüm 9 |


## COBOL kodu
### base/src/lgipol01.cbl
```cobol
      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      *                    Inquire Policy                              *
      *                                                                *
      *  Business logic for policy inquire                             *
      *   To obtain full details of an individual policy:              *
      *     Endowment, House or Motor                                  *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGIPOL01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
      *
       DATA DIVISION.

       WORKING-STORAGE SECTION.

      *----------------------------------------------------------------*
      * Common defintions                                              *
      *----------------------------------------------------------------*
      * Run time (debug) infomation for this invocation
        01  WS-HEADER.
           03 WS-EYECATCHER            PIC X(16)
                                        VALUE 'LGIPOL01------WS'.
           03 WS-TRANSID               PIC X(4).
           03 WS-TERMID                PIC X(4).
           03 WS-TASKNUM               PIC 9(7).
           03 WS-FILLER                PIC X.
           03 WS-ADDR-DFHCOMMAREA      USAGE is POINTER.
           03 WS-CALEN                 PIC S9(4) COMP.

      * Variables for time/date processing
       01  ABS-TIME                    PIC S9(8) COMP VALUE +0.
       01  TIME1                       PIC X(8)  VALUE SPACES.
       01  DATE1                       PIC X(10) VALUE SPACES.

      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGIPOL01'.
           03 EM-VARIABLE              PIC X(21) VALUE SPACES.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
      *----------------------------------------------------------------*

       01 LGIPDB01                     PIC x(8) Value 'LGIPDB01'.
      *----------------------------------------------------------------*
             COPY LGPOLICY.

      *----------------------------------------------------------------*
      ******************************************************************
      *    L I N K A G E     S E C T I O N
      ******************************************************************
       LINKAGE SECTION.
       01  DFHCOMMAREA.
             COPY LGCMAREA.
      ******************************************************************
      *    P R O C E D U R E S
      ******************************************************************
       PROCEDURE DIVISION.

      *----------------------------------------------------------------*
       MAINLINE SECTION.
      *
           INITIALIZE WS-HEADER.
      *
           MOVE EIBTRNID TO WS-TRANSID.
           MOVE EIBTRMID TO WS-TERMID.
           MOVE EIBTASKN TO WS-TASKNUM.
      *
      * If NO commarea received issue an ABEND
           IF EIBCALEN IS EQUAL TO ZERO
               MOVE ' NO COMMAREA RECEIVED' TO EM-VARIABLE
               PERFORM WRITE-ERROR-MESSAGE
               EXEC CICS ABEND ABCODE('LGCA') NODUMP END-EXEC
           END-IF

      * initialize commarea return code to zero
           MOVE '00' TO CA-RETURN-CODE
           MOVE EIBCALEN TO WS-CALEN.
           SET WS-ADDR-DFHCOMMAREA TO ADDRESS OF DFHCOMMAREA.
      *

           EXEC CICS LINK Program(LGIPDB01)
               Commarea(DFHCOMMAREA)
               Length(32500)
           END-EXEC.

           EXEC CICS RETURN END-EXEC.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*

      *================================================================*
      * Procedure to write error message to Queues                     *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
      * Obtain and format current time and date
           EXEC CICS ASKTIME ABSTIME(ABS-TIME)
           END-EXEC
           EXEC CICS FORMATTIME ABSTIME(ABS-TIME)
                     MMDDYYYY(DATE1)
                     TIME(TIME1)
           END-EXEC
           MOVE DATE1 TO EM-DATE
           MOVE TIME1 TO EM-TIME
      * Write output message to TDQ
           EXEC CICS LINK PROGRAM('LGSTSQ')
                     COMMAREA(ERROR-MSG)
                     LENGTH(LENGTH OF ERROR-MSG)
           END-EXEC.
      * Write 90 bytes or as much as we have of commarea to TDQ
           IF EIBCALEN > 0 THEN
             IF EIBCALEN < 91 THEN
               MOVE DFHCOMMAREA(1:EIBCALEN) TO CA-DATA
               EXEC CICS LINK PROGRAM('LGSTSQ')
                         COMMAREA(CA-ERROR-MSG)
                         LENGTH(LENGTH OF CA-ERROR-MSG)
               END-EXEC
             ELSE
               MOVE DFHCOMMAREA(1:90) TO CA-DATA
               EXEC CICS LINK PROGRAM('LGSTSQ')
                         COMMAREA(CA-ERROR-MSG)
                         LENGTH(LENGTH OF CA-ERROR-MSG)
               END-EXEC
             END-IF
           END-IF.
           EXIT.
```

### base/src/lgipdb01.cbl
```cobol
       PROCESS SQL
      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2021                             *
      *                                                                *
      *                    Inquire Policy                              *
      *                                                                *
      *   To obtain full details of an individual policy:              *
      *     Endowment, House, Motor, Commercial                        *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGIPDB01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
      *
       DATA DIVISION.

       WORKING-STORAGE SECTION.

        01 TSAREA.
          03  TSAREAPOSTCODE           Pic X(8).
          03  Filler                   Pic X Value Spaces.
          03  TSAREACUSTNUM            Pic 9(10).
          03  Filler                   Pic X Value Spaces.
          03  TSAREAPOLNUM             Pic 9(10).
          03  Filler                   Pic X Value Spaces.
      *----------------------------------------------------------------*
      * Common defintions                                              *
      *----------------------------------------------------------------*
      * Run time (debug) infomation for this invocation
        01  WS-HEADER.
           03 WS-EYECATCHER            PIC X(16)
                                        VALUE 'LGIPDB01------WS'.
           03 WS-TRANSID               PIC X(4).
           03 WS-TERMID                PIC X(4).
           03 WS-TASKNUM               PIC 9(7).
           03 WS-FILLER                PIC X.
           03 WS-ADDR-DFHCOMMAREA      USAGE is POINTER.
           03 WS-CALEN                 PIC S9(4) COMP.

      * Variables for time/date processing
       01  ABS-TIME                    PIC S9(8) COMP VALUE +0.
       01  TIME1                       PIC X(8)  VALUE SPACES.
       01  DATE1                       PIC X(10) VALUE SPACES.

      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGIPDB01'.
           03 EM-VARIABLE.
             05 FILLER                 PIC X(6)  VALUE ' CNUM='.
             05 EM-CUSNUM              PIC X(10)  VALUE SPACES.
             05 FILLER                 PIC X(6)  VALUE ' PNUM='.
             05 EM-POLNUM              PIC X(10)  VALUE SPACES.
             05 EM-SQLREQ              PIC X(16) VALUE SPACES.
             05 FILLER                 PIC X(9)  VALUE ' SQLCODE='.
             05 EM-SQLRC               PIC +9(5) USAGE DISPLAY.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      * Definitions required for data manipulation                     *
      *----------------------------------------------------------------*
       01 MINUS-ONE                    PIC S9(4) COMP VALUE -1.

       01  WS-COMMAREA-LENGTHS.
           03 WS-CA-HEADERTRAILER-LEN  PIC S9(4) COMP VALUE +33.
           03 WS-REQUIRED-CA-LEN       PIC S9(4)      VALUE +0.

      * Position of first char in commarea after end of policy data
      * for use when placing end of data indicator
       01  END-POLICY-POS              PIC S9(4) COMP VALUE +1.

       01  ICOM-Record-Count           PIC S9(4) COMP.
       01  WS-Request-ID               Pic X(6).

      *----------------------------------------------------------------*
      *    DB2 CONTROL
      *----------------------------------------------------------------*
      *----------------------------------------------------------------*
      * Declare Cursors
      *----------------------------------------------------------------*
           EXEC SQL
             DECLARE Cust_Cursor Insensitive Scroll Cursor For
             SELECT
                   CustomerNumber,
                   Policy.PolicyNumber,
                   RequestDate,
                   StartDate,
                   RenewalDate,
                   Address,
                   Zipcode,
                   LatitudeN,
                   LongitudeW,
                   Customer,
                   PropertyType,
                   FirePeril,
                   FirePremium,
                   CrimePeril,
                   CrimePremium,
                   FloodPeril,
                   FloodPremium,
                   WeatherPeril,
                   WeatherPremium,
                   Status,
                   RejectionReason
             FROM  POLICY,COMMERCIAL
             WHERE ( POLICY.POLICYNUMBER =
                        Commercial.POLICYNUMBER AND
                     Policy.CustomerNumber =
                        :DB2-CUSTOMERNUM-INT  )
           END-EXEC.
      *----------------------------------------------------------------*
           EXEC SQL
             DECLARE Zip_Cursor Insensitive Scroll Cursor For
             SELECT
                   CustomerNumber,
                   Policy.PolicyNumber,
                   RequestDate,
                   StartDate,
                   RenewalDate,
                   Address,
                   Zipcode,
                   LatitudeN,
                   LongitudeW,
                   Customer,
                   PropertyType,
                   FirePeril,
                   FirePremium,
                   CrimePeril,
                   CrimePremium,
                   FloodPeril,
                   FloodPremium,
                   WeatherPeril,
                   WeatherPremium,
                   Status,
                   RejectionReason
             FROM  POLICY,COMMERCIAL
             WHERE ( POLICY.POLICYNUMBER =
                        Commercial.POLICYNUMBER   AND
                     Commercial.Zipcode =
                        :CA-B-POSTCODE  )
           END-EXEC.

      * SQLCA DB2 communications area
           EXEC SQL
               INCLUDE SQLCA
           END-EXEC.


      *----------------------------------------------------------------*
      * Definitions required by SQL statement                          *
      *   DB2 datatypes to COBOL equivalents                           *
      *     SMALLINT    :   PIC S9(4) COMP                             *
      *     INTEGER     :   PIC S9(9) COMP                             *
      *     DATE        :   PIC X(10)                                  *
      *     TIMESTAMP   :   PIC X(26)                                  *
      *----------------------------------------------------------------*
      * Host variables for input to DB2 integer types
       01 DB2-IN-INTEGERS.
           03 DB2-CUSTOMERNUM-INT      PIC S9(9) COMP VALUE +0.
           03 DB2-POLICYNUM-INT        PIC S9(9) COMP VALUE +0.
           03 DB2-CLAIMNUM-INT         PIC S9(9) COMP VALUE +0.
       01 DB2-OUT-INTEGERS.
           03 DB2-BROKERID-INT         PIC S9(9) COMP.
           03 DB2-PAYMENT-INT          PIC S9(9) COMP.
           03 DB2-E-TERM-SINT          PIC S9(4) COMP.
           03 DB2-E-SUMASSURED-INT     PIC S9(9) COMP.
           03 DB2-E-PADDING-LEN        PIC S9(9) COMP.
           03 DB2-H-BEDROOMS-SINT      PIC S9(4) COMP.
           03 DB2-H-VALUE-INT          PIC S9(9) COMP.
           03 DB2-M-VALUE-INT          PIC S9(9) COMP.
           03 DB2-M-CC-SINT            PIC S9(4) COMP.
           03 DB2-M-PREMIUM-INT        PIC S9(9) COMP.
           03 DB2-M-ACCIDENTS-INT      PIC S9(9) COMP.
           03 DB2-B-FirePeril-Int      PIC S9(4) COMP.
           03 DB2-B-FirePremium-Int    PIC S9(9) COMP.
           03 DB2-B-CrimePeril-Int     PIC S9(4) COMP.
           03 DB2-B-CrimePremium-Int   PIC S9(9) COMP.
           03 DB2-B-FloodPeril-Int     PIC S9(4) COMP.
           03 DB2-B-FloodPremium-Int   PIC S9(9) COMP.
           03 DB2-B-WeatherPeril-Int   PIC S9(4) COMP.
           03 DB2-B-WeatherPremium-Int PIC S9(9) COMP.
           03 DB2-B-Status-Int         PIC S9(4) COMP.
           03 DB2-C-Paid-int           PIC S9(9) COMP.
           03 DB2-C-Value-int          PIC S9(9) COMP.

      * Policy descriptions to receive output from DB2
      * Must be an SQL INCLUDE so available to SQL pre-compiler
           EXEC SQL
             INCLUDE LGPOLICY
           END-EXEC.

      * Indicator variables for columns which could return nulls
      *   if these are not specified and SQL FETCH tries to return
      *   null value for a listed column it will FAIL with SQLCODE=-305
       77  IND-BROKERID                PIC S9(4) COMP.
       77  IND-BROKERSREF              PIC S9(4) COMP.
       77  IND-PAYMENT                 PIC S9(4) COMP.
       77  IND-E-PADDINGDATA           PIC S9(4) COMP.
       77  IND-E-PADDINGDATAL          PIC S9(4) COMP.

      *----------------------------------------------------------------*


      ******************************************************************
      *    L I N K A G E     S E C T I O N
      ******************************************************************
       LINKAGE SECTION.

       01  DFHCOMMAREA.
           EXEC SQL
             INCLUDE LGCMAREA
           END-EXEC.

       01  ICOM-Record                 Pic X(1202).

      ******************************************************************
      *    P R O C E D U R E S
      ******************************************************************
       PROCEDURE DIVISION.

      *----------------------------------------------------------------*
       MAINLINE SECTION.

      *----------------------------------------------------------------*
      * Common code                                                    *
      *----------------------------------------------------------------*
      * initialize working storage variables
           INITIALIZE WS-HEADER.
      * set up general variable
           MOVE EIBTRNID TO WS-TRANSID.
           MOVE EIBTRMID TO WS-TERMID.
           MOVE EIBTASKN TO WS-TASKNUM.
      *----------------------------------------------------------------*
      * initialize DB2 host variables
           INITIALIZE DB2-IN-INTEGERS.
           INITIALIZE DB2-OUT-INTEGERS.
           INITIALIZE DB2-POLICY.

      *---------------------------------------------------------------*
      * Check commarea and obtain required details                    *
      *---------------------------------------------------------------*
      * If NO commarea received issue an ABEND
           IF EIBCALEN IS EQUAL TO ZERO
             MOVE ' NO COMMAREA RECEIVED' TO EM-VARIABLE
             PERFORM WRITE-ERROR-MESSAGE
             EXEC CICS ABEND ABCODE('LGCA') NODUMP END-EXEC
           END-IF

      * initialize commarea return code to zero
           MOVE '00' TO CA-RETURN-CODE
           MOVE EIBCALEN TO WS-CALEN
           SET WS-ADDR-DFHCOMMAREA TO ADDRESS OF DFHCOMMAREA

      * Convert commarea customer & policy nums to DB2 integer format
           MOVE CA-CUSTOMER-NUM TO DB2-CUSTOMERNUM-INT
           MOVE CA-POLICY-NUM   TO DB2-POLICYNUM-INT
      * and save in error msg field incase required
           MOVE CA-CUSTOMER-NUM TO EM-CUSNUM
           MOVE CA-POLICY-NUM   TO EM-POLNUM

      *----------------------------------------------------------------*
      * Check which policy type is being requested                     *
      * This is not actually required whilst only endowment policy     *
      * inquires are supported, but will make future expansion simpler *
      *----------------------------------------------------------------*
      * Upper case value passed in Request Id field                    *
           MOVE FUNCTION UPPER-CASE(CA-REQUEST-ID) TO WS-REQUEST-ID

           EVALUATE WS-REQUEST-ID

             WHEN '01IEND'
               INITIALIZE DB2-ENDOWMENT
               PERFORM GET-ENDOW-DB2-INFO

             WHEN '01IHOU'
               INITIALIZE DB2-HOUSE
               PERFORM GET-HOUSE-DB2-INFO

             WHEN '01IMOT'
               INITIALIZE DB2-MOTOR
               PERFORM GET-MOTOR-DB2-INFO

             WHEN '01ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-1

             WHEN '02ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-2

             WHEN '03ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-3

             WHEN '05ICOM'
               INITIALIZE DB2-COMMERCIAL
               PERFORM GET-COMMERCIAL-DB2-INFO-5

             WHEN OTHER
               MOVE '99' TO CA-RETURN-CODE

           END-EVALUATE.

       End-Program.
           EXEC CICS RETURN END-EXEC.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*


      *================================================================*
      * Use Select on join of Policy and Endowment tables to obtain    *
      * single row that matches customer and policy number requested.  *
      * NOTE: because we do not know length of varchar yet - we must   *
      *      put output of DB2 SELECT into working storage to avoid    *
      *      risk of overwriting CICS storage beyond commarea.         *
      *================================================================*
       GET-ENDOW-DB2-INFO.

           MOVE ' SELECT ENDOW ' TO EM-SQLREQ
           EXEC SQL
             SELECT  ISSUEDATE,
                     EXPIRYDATE,
                     LASTCHANGED,
                     BROKERID,
                     BROKERSREFERENCE,
                     PAYMENT,
                     WITHPROFITS,
                     EQUITIES,
                     MANAGEDFUND,
                     FUNDNAME,
                     TERM,
                     SUMASSURED,
                     LIFEASSURED,
                     PADDINGDATA,
                     LENGTH(PADDINGDATA)
             INTO  :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-LASTCHANGED,
                   :DB2-BROKERID-INT INDICATOR :IND-BROKERID,
                   :DB2-BROKERSREF INDICATOR :IND-BROKERSREF,
                   :DB2-PAYMENT-INT INDICATOR :IND-PAYMENT,
                   :DB2-E-WITHPROFITS,
                   :DB2-E-EQUITIES,
                   :DB2-E-MANAGEDFUND,
                   :DB2-E-FUNDNAME,
                   :DB2-E-TERM-SINT,
                   :DB2-E-SUMASSURED-INT,
                   :DB2-E-LIFEASSURED,
                   :DB2-E-PADDINGDATA INDICATOR :IND-E-PADDINGDATA,
                   :DB2-E-PADDING-LEN INDICATOR :IND-E-PADDINGDATAL
             FROM  POLICY,ENDOWMENT
             WHERE ( POLICY.POLICYNUMBER =
                        ENDOWMENT.POLICYNUMBER   AND
                     POLICY.CUSTOMERNUMBER =
                        :DB2-CUSTOMERNUM-INT             AND
                     POLICY.POLICYNUMBER =
                        :DB2-POLICYNUM-INT               )
           END-EXEC

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-ENDOW-LEN       TO WS-REQUIRED-CA-LEN

      *----------------------------------------------------------------*
      *      Specific code to allow for length of VACHAR data
      *      check whether PADDINGDATA field is non-null
      *        and calculate length of endowment policy
      *        and position of free space in commarea after policy data
      *----------------------------------------------------------------*
             IF IND-E-PADDINGDATAL NOT EQUAL MINUS-ONE
               ADD DB2-E-PADDING-LEN TO WS-REQUIRED-CA-LEN
               ADD DB2-E-PADDING-LEN TO END-POLICY-POS
             END-IF

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               EXEC CICS RETURN END-EXEC
             ELSE
      *        Length is sufficent so move data to commarea
      *        Move Integer fields to required length numerics
      *        Don't move null fields
               IF IND-BROKERID NOT EQUAL MINUS-ONE
                 MOVE DB2-BROKERID-INT    TO DB2-BROKERID
               END-IF
               IF IND-PAYMENT NOT EQUAL MINUS-ONE
                 MOVE DB2-PAYMENT-INT TO DB2-PAYMENT
               END-IF
      *----------------------------------------------------------------*
               MOVE DB2-E-TERM-SINT       TO DB2-E-TERM
               MOVE DB2-E-SUMASSURED-INT  TO DB2-E-SUMASSURED

               MOVE DB2-POLICY-COMMON     TO CA-POLICY-COMMON
               MOVE DB2-ENDOW-FIXED
                   TO CA-ENDOWMENT(1:WS-ENDOW-LEN)
               IF IND-E-PADDINGDATA NOT EQUAL MINUS-ONE
                 MOVE DB2-E-PADDINGDATA TO
                     CA-E-PADDING-DATA(1:DB2-E-PADDING-LEN)
               END-IF
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-E-PADDING-DATA(END-POLICY-POS:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF

           END-IF.
           EXIT.

      *================================================================*
      * Use Select on join of Policy and House tables to obtain        *
      * single row that matches customer and policy number requested.  *
      * NOTE: because we do not know length of varchar yet - we must   *
      *      put output of DB2 SELECT into working storage to avoid    *
      *      risk of overwriting CICS storage beyond commarea.         *
      *================================================================*
       GET-HOUSE-DB2-INFO.

           MOVE ' SELECT HOUSE ' TO EM-SQLREQ
           EXEC SQL
             SELECT  ISSUEDATE,
                     EXPIRYDATE,
                     LASTCHANGED,
                     BROKERID,
                     BROKERSREFERENCE,
                     PAYMENT,
                     PROPERTYTYPE,
                     BEDROOMS,
                     VALUE,
                     HOUSENAME,
                     HOUSENUMBER,
                     POSTCODE
             INTO  :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-LASTCHANGED,
                   :DB2-BROKERID-INT INDICATOR :IND-BROKERID,
                   :DB2-BROKERSREF INDICATOR :IND-BROKERSREF,
                   :DB2-PAYMENT-INT INDICATOR :IND-PAYMENT,
                   :DB2-H-PROPERTYTYPE,
                   :DB2-H-BEDROOMS-SINT,
                   :DB2-H-VALUE-INT,
                   :DB2-H-HOUSENAME,
                   :DB2-H-HOUSENUMBER,
                   :DB2-H-POSTCODE
             FROM  POLICY,HOUSE
             WHERE ( POLICY.POLICYNUMBER =
                        HOUSE.POLICYNUMBER   AND
                     POLICY.CUSTOMERNUMBER =
                        :DB2-CUSTOMERNUM-INT             AND
                     POLICY.POLICYNUMBER =
                        :DB2-POLICYNUM-INT               )
           END-EXEC

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-HOUSE-LEN       TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               EXEC CICS RETURN END-EXEC
             ELSE
      *        Length is sufficent so move data to commarea
      *        Move Integer fields to required length numerics
      *        Don't move null fields
               IF IND-BROKERID NOT EQUAL MINUS-ONE
                 MOVE DB2-BROKERID-INT  TO DB2-BROKERID
               END-IF
               IF IND-PAYMENT NOT EQUAL MINUS-ONE
                 MOVE DB2-PAYMENT-INT TO DB2-PAYMENT
               END-IF
               MOVE DB2-H-BEDROOMS-SINT TO DB2-H-BEDROOMS
               MOVE DB2-H-VALUE-INT     TO DB2-H-VALUE

               MOVE DB2-POLICY-COMMON   TO CA-POLICY-COMMON
               MOVE DB2-HOUSE           TO CA-HOUSE(1:WS-HOUSE-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-H-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF

           END-IF.
           EXIT.

      *================================================================*
      * Use Select on join of Policy and Motor tables to obtain        *
      * single row that matches customer and policy number requested.  *
      *================================================================*
       GET-MOTOR-DB2-INFO.

           MOVE ' SELECT MOTOR ' TO EM-SQLREQ
           EXEC SQL
             SELECT  ISSUEDATE,
                     EXPIRYDATE,
                     LASTCHANGED,
                     BROKERID,
                     BROKERSREFERENCE,
                     PAYMENT,
                     MAKE,
                     MODEL,
                     VALUE,
                     REGNUMBER,
                     COLOUR,
                     CC,
                     YEAROFMANUFACTURE,
                     PREMIUM,
                     ACCIDENTS
             INTO  :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-LASTCHANGED,
                   :DB2-BROKERID-INT INDICATOR :IND-BROKERID,
                   :DB2-BROKERSREF INDICATOR :IND-BROKERSREF,
                   :DB2-PAYMENT-INT INDICATOR :IND-PAYMENT,
                   :DB2-M-MAKE,
                   :DB2-M-MODEL,
                   :DB2-M-VALUE-INT,
                   :DB2-M-REGNUMBER,
                   :DB2-M-COLOUR,
                   :DB2-M-CC-SINT,
                   :DB2-M-MANUFACTURED,
                   :DB2-M-PREMIUM-INT,
                   :DB2-M-ACCIDENTS-INT
             FROM  POLICY,MOTOR
             WHERE ( POLICY.POLICYNUMBER =
                        MOTOR.POLICYNUMBER   AND
                     POLICY.CUSTOMERNUMBER =
                        :DB2-CUSTOMERNUM-INT             AND
                     POLICY.POLICYNUMBER =
                        :DB2-POLICYNUM-INT               )
           END-EXEC

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-MOTOR-LEN       TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               EXEC CICS RETURN END-EXEC
             ELSE
      *        Length is sufficent so move data to commarea
      *        Move Integer fields to required length numerics
      *        Don't move null fields
               IF IND-BROKERID NOT EQUAL MINUS-ONE
                 MOVE DB2-BROKERID-INT TO DB2-BROKERID
               END-IF
               IF IND-PAYMENT NOT EQUAL MINUS-ONE
                 MOVE DB2-PAYMENT-INT    TO DB2-PAYMENT
               END-IF
               MOVE DB2-M-CC-SINT      TO DB2-M-CC
               MOVE DB2-M-VALUE-INT    TO DB2-M-VALUE
               MOVE DB2-M-PREMIUM-INT  TO DB2-M-PREMIUM
               MOVE DB2-M-ACCIDENTS-INT TO DB2-M-ACCIDENTS
               MOVE DB2-M-PREMIUM-INT  TO CA-M-PREMIUM
               MOVE DB2-M-ACCIDENTS-INT TO CA-M-ACCIDENTS

               MOVE DB2-POLICY-COMMON  TO CA-POLICY-COMMON
               MOVE DB2-MOTOR          TO CA-MOTOR(1:WS-MOTOR-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-M-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF

           END-IF.
           EXIT.

      *================================================================*
      * Use Select on join of Policy and Commer table to obtain        *
      * single row that matches customer and policy number requested.  *
      *================================================================*
       GET-Commercial-DB2-INFO-1.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ

           EXEC SQL
             SELECT
                   RequestDate,
                   StartDate,
                   RenewalDate,
                   Address,
                   Zipcode,
                   LatitudeN,
                   LongitudeW,
                   Customer,
                   PropertyType,
                   FirePeril,
                   FirePremium,
                   CrimePeril,
                   CrimePremium,
                   FloodPeril,
                   FloodPremium,
                   WeatherPeril,
                   WeatherPremium,
                   Status,
                   RejectionReason
             INTO  :DB2-LASTCHANGED,
                   :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-B-Address,
                   :DB2-B-Postcode,
                   :DB2-B-Latitude,
                   :DB2-B-Longitude,
                   :DB2-B-Customer,
                   :DB2-B-PropType,
                   :DB2-B-FirePeril-Int,
                   :DB2-B-FirePremium-Int,
                   :DB2-B-CrimePeril-Int,
                   :DB2-B-CrimePremium-Int,
                   :DB2-B-FloodPeril-Int,
                   :DB2-B-FloodPremium-Int,
                   :DB2-B-WeatherPeril-Int,
                   :DB2-B-WeatherPremium-Int,
                   :DB2-B-Status-Int,
                   :DB2-B-RejectReason
             FROM  POLICY,COMMERCIAL
             WHERE ( POLICY.POLICYNUMBER = 
                        COMMERCIAL.POLICYNUMBER   AND
                     POLICY.CUSTOMERNUMBER =
                        :DB2-CUSTOMERNUM-INT             AND
                     POLICY.POLICYNUMBER =
                        :DB2-POLICYNUM-INT               )
           END-EXEC

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-COMM-LEN        TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               EXEC CICS RETURN END-EXEC
             ELSE
               MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
               MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
               MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
               MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
               MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
               MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
               MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
               MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
               MOVE DB2-B-Status-Int         TO DB2-B-Status
      *
               MOVE DB2-POLICY-COMMON  TO CA-POLICY-COMMON
               MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-B-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.


           EXIT.

      *================================================================*
      * Use Select on Policy number only to obtain                     *
      * single row that matches the requested ploicy.                  *
      *================================================================*
       GET-Commercial-DB2-INFO-2.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ

           EXEC SQL
             SELECT
                   CustomerNumber,
                   RequestDate,
                   StartDate,
                   RenewalDate,
                   Address,
                   Zipcode,
                   LatitudeN,
                   LongitudeW,
                   Customer,
                   PropertyType,
                   FirePeril,
                   FirePremium,
                   CrimePeril,
                   CrimePremium,
                   FloodPeril,
                   FloodPremium,
                   WeatherPeril,
                   WeatherPremium,
                   Status,
                   RejectionReason
             INTO
                   :DB2-CUSTOMERNUM-INT,
                   :DB2-LASTCHANGED,
                   :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-B-Address,
                   :DB2-B-Postcode,
                   :DB2-B-Latitude,
                   :DB2-B-Longitude,
                   :DB2-B-Customer,
                   :DB2-B-PropType,
                   :DB2-B-FirePeril-Int,
                   :DB2-B-FirePremium-Int,
                   :DB2-B-CrimePeril-Int,
                   :DB2-B-CrimePremium-Int,
                   :DB2-B-FloodPeril-Int,
                   :DB2-B-FloodPremium-Int,
                   :DB2-B-WeatherPeril-Int,
                   :DB2-B-WeatherPremium-Int,
                   :DB2-B-Status-Int,
                   :DB2-B-RejectReason
             FROM  POLICY,COMMERCIAL
             WHERE ( POLICY.POLICYNUMBER =
                        COmmercial.POLICYNUMBER   AND
                     POLICY.POLICYNUMBER =
                        :DB2-POLICYNUM-INT               )
           END-EXEC

           IF SQLCODE = 0
      *      Select was successful

      *      Calculate size of commarea required to return all data
             ADD WS-CA-HEADERTRAILER-LEN TO WS-REQUIRED-CA-LEN
             ADD WS-FULL-COMM-LEN        TO WS-REQUIRED-CA-LEN

      *      if commarea received is not large enough ...
      *        set error return code and return to caller
             IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
               MOVE '98' TO CA-RETURN-CODE
               EXEC CICS RETURN END-EXEC
             ELSE
               MOVE DB2-CustomerNum-Int      TO CA-Customer-Num
               MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
               MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
               MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
               MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
               MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
               MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
               MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
               MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
               MOVE DB2-B-Status-Int         TO DB2-B-Status
      *
               MOVE DB2-POLICY-COMMON  TO CA-POLICY-COMMON
               MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)
             END-IF

      *      Mark the end of the policy data
             MOVE 'FINAL' TO CA-B-FILLER(1:5)

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
      *        No rows found - invalid customer / policy number
               MOVE '01' TO CA-RETURN-CODE
             ELSE
      *        something has gone wrong
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.


           EXIT.

      *================================================================*
      * Use Cursor to obtain matching customer policy records          *
      * from commercial table                                          *
      *================================================================*
       GET-Commercial-DB2-INFO-3.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ
           Move 0 To ICOM-Record-Count

           EXEC SQL
             OPEN Cust_Cursor
           END-EXEC.
           IF SQLCODE NOT EQUAL 0
             MOVE '89' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Perform GET-Commercial-DB2-INFO-3-Cur
                     With Test after Until SQLCODE > 0

           EXEC SQL
             Close Cust_Cursor
           END-EXEC.
           IF SQLCODE NOT EQUAL 0
             MOVE '88' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Exit.

       GET-Commercial-DB2-INFO-3-Cur.

           EXEC SQL
             Fetch Cust_Cursor
             INTO
                   :DB2-CUSTOMERNUM-INT,
                   :DB2-POLICYNUM-INT,
                   :DB2-LASTCHANGED,
                   :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-B-Address,
                   :DB2-B-Postcode,
                   :DB2-B-Latitude,
                   :DB2-B-Longitude,
                   :DB2-B-Customer,
                   :DB2-B-PropType,
                   :DB2-B-FirePeril-Int,
                   :DB2-B-FirePremium-Int,
                   :DB2-B-CrimePeril-Int,
                   :DB2-B-CrimePremium-Int,
                   :DB2-B-FloodPeril-Int,
                   :DB2-B-FloodPremium-Int,
                   :DB2-B-WeatherPeril-Int,
                   :DB2-B-WeatherPremium-Int,
                   :DB2-B-Status-Int,
                   :DB2-B-RejectReason
           END-EXEC

           If SQLCODE = 0
             MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
             MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
             MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
             MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
             MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
             MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
             MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
             MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
             MOVE DB2-B-Status-Int         TO DB2-B-Status
             MOVE DB2-CustomerNum-Int      TO CA-CUSTOMER-NUM
             MOVE DB2-PolicyNum-Int        TO CA-POLICY-NUM
             MOVE DB2-POLICY-COMMON        TO CA-POLICY-COMMON
             MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)

             Add 1 To ICOM-Record-Count
             If ICOM-Record-Count > 20
               Move 17 To SQLCODE
             End-If
           End-If


           EXIT.

      *================================================================*
      * Use Cursor to obtain matching rows                             *
      * from commercial table                                          *
      *================================================================*
       GET-Commercial-DB2-INFO-5.

           MOVE ' SELECT Commercial ' TO EM-SQLREQ

           EXEC SQL
             OPEN Zip_Cursor
           END-EXEC.
           IF SQLCODE NOT EQUAL 0
             MOVE '89' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Perform GET-Commercial-DB2-INFO-5-Cur
                     With Test after Until SQLCODE > 0

           EXEC SQL
             Close Zip_Cursor
           END-EXEC.
           IF SQLCODE NOT EQUAL 0
             MOVE '88' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             PERFORM END-PROGRAM
           END-IF.

           Exit.

       GET-Commercial-DB2-INFO-5-Cur.
           EXEC SQL
             Fetch Zip_Cursor
             INTO
                   :DB2-CUSTOMERNUM-INT,
                   :DB2-POLICYNUM-INT,
                   :DB2-LASTCHANGED,
                   :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-B-Address,
                   :DB2-B-Postcode,
                   :DB2-B-Latitude,
                   :DB2-B-Longitude,
                   :DB2-B-Customer,
                   :DB2-B-PropType,
                   :DB2-B-FirePeril-Int,
                   :DB2-B-FirePremium-Int,
                   :DB2-B-CrimePeril-Int,
                   :DB2-B-CrimePremium-Int,
                   :DB2-B-FloodPeril-Int,
                   :DB2-B-FloodPremium-Int,
                   :DB2-B-WeatherPeril-Int,
                   :DB2-B-WeatherPremium-Int,
                   :DB2-B-Status-Int,
                   :DB2-B-RejectReason
           END-EXEC

           If SQLCODE = 0
             MOVE DB2-B-FirePeril-Int      TO DB2-B-FirePeril
             MOVE DB2-B-FirePremium-Int    TO DB2-B-FirePremium
             MOVE DB2-B-CrimePeril-Int     TO DB2-B-CrimePeril
             MOVE DB2-B-CrimePremium-Int   TO DB2-B-CrimePremium
             MOVE DB2-B-FloodPeril-Int     TO DB2-B-FloodPeril
             MOVE DB2-B-FloodPremium-Int   TO DB2-B-FloodPremium
             MOVE DB2-B-WeatherPeril-Int   TO DB2-B-WeatherPeril
             MOVE DB2-B-WeatherPremium-Int TO DB2-B-WeatherPremium
             MOVE DB2-B-Status-Int         TO DB2-B-Status
             MOVE DB2-CustomerNum-Int      TO CA-CUSTOMER-NUM
             MOVE DB2-PolicyNum-Int        TO CA-POLICY-NUM
             MOVE DB2-POLICY-COMMON        TO CA-POLICY-COMMON
             MOVE DB2-COMMERCIAL     TO CA-COMMERCIAL(1:WS-COMM-LEN)
           End-If


           EXIT.

      *================================================================*
      * Procedure to write error message to TD QUEUE(CSMT)             *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
           MOVE SQLCODE TO EM-SQLRC
      * Obtain and format current time and date
           EXEC CICS ASKTIME ABSTIME(ABS-TIME)
           END-EXEC
           EXEC CICS FORMATTIME ABSTIME(ABS-TIME)
                     MMDDYYYY(DATE1)
                     TIME(TIME1)
           END-EXEC
           MOVE DATE1 TO EM-DATE
           MOVE TIME1 TO EM-TIME
      * Write output message to TDQ
           EXEC CICS LINK PROGRAM('LGSTSQ')
                     COMMAREA(ERROR-MSG)
                     LENGTH(LENGTH OF ERROR-MSG)
           END-EXEC.
      * Write 90 bytes or as much as we have of commarea to TDQ
           IF EIBCALEN > 0 THEN
             IF EIBCALEN < 91 THEN
               MOVE DFHCOMMAREA(1:EIBCALEN) TO CA-DATA
               EXEC CICS LINK PROGRAM('LGSTSQ')
                         COMMAREA(CA-ERROR-MSG)
                         LENGTH(LENGTH OF CA-ERROR-MSG)
               END-EXEC
             ELSE
               MOVE DFHCOMMAREA(1:90) TO CA-DATA
               EXEC CICS LINK PROGRAM('LGSTSQ')
                         COMMAREA(CA-ERROR-MSG)
                         LENGTH(LENGTH OF CA-ERROR-MSG)
               END-EXEC
             END-IF
           END-IF.
           EXIT.
```

### base/src/lgcmarea.cpy
```cobol
      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      *               COPYBOOK for COMMAREA structure                  *
      *                                                                *
      *   This commarea can be used for all functions                  *
      *                                                                *
      ******************************************************************
           03 CA-REQUEST-ID            PIC X(6).
           03 CA-RETURN-CODE           PIC 9(2).
           03 CA-CUSTOMER-NUM          PIC 9(10).
           03 CA-REQUEST-SPECIFIC      PIC X(32482).
      *    Fields used in INQ All and ADD customer
           03 CA-CUSTOMER-REQUEST REDEFINES CA-REQUEST-SPECIFIC.
              05 CA-FIRST-NAME         PIC X(10).
              05 CA-LAST-NAME          PIC X(20).
              05 CA-DOB                PIC X(10).
              05 CA-HOUSE-NAME         PIC X(20).
              05 CA-HOUSE-NUM          PIC X(4).
              05 CA-POSTCODE           PIC X(8).
              05 CA-NUM-POLICIES       PIC 9(3).
              05 CA-PHONE-MOBILE       PIC X(20).
              05 CA-PHONE-HOME         PIC X(20).
              05 CA-EMAIL-ADDRESS      PIC X(100).
              05 CA-POLICY-DATA        PIC X(32267).
      *    Fields used in Customer security call
           03 CA-CUSTSECR-REQUEST REDEFINES CA-REQUEST-SPECIFIC.
              05 CA-CUSTSECR-PASS      PIC X(32).
              05 CA-CUSTSECR-COUNT     PIC X(4).
              05 CA-CUSTSECR-STATE     PIC X.
              05 CA-CUSTSECR-DATA      PIC X(32445).
      *    Fields used in INQ, UPD, ADD & DELETE policy
           03 CA-POLICY-REQUEST REDEFINES CA-REQUEST-SPECIFIC.
              05 CA-POLICY-NUM         PIC 9(10).
      *       Common policy details
              05 CA-POLICY-COMMON.
                 07 CA-ISSUE-DATE      PIC X(10).
                 07 CA-EXPIRY-DATE     PIC X(10).
                 07 CA-LASTCHANGED     PIC X(26).
                 07 CA-BROKERID        PIC 9(10).
                 07 CA-BROKERSREF      PIC X(10).
                 07 CA-PAYMENT         PIC 9(6).
              05 CA-POLICY-SPECIFIC    PIC X(32400).
      *       Endowment policy description
              05 CA-ENDOWMENT REDEFINES CA-POLICY-SPECIFIC.
                 07 CA-E-WITH-PROFITS    PIC X.
                 07 CA-E-EQUITIES        PIC X.
                 07 CA-E-MANAGED-FUND    PIC X.
                 07 CA-E-FUND-NAME       PIC X(10).
                 07 CA-E-TERM            PIC 99.
                 07 CA-E-SUM-ASSURED     PIC 9(6).
                 07 CA-E-LIFE-ASSURED    PIC X(31).
                 07 CA-E-PADDING-DATA    PIC X(32348).
      *       House policy description
              05 CA-HOUSE REDEFINES CA-POLICY-SPECIFIC.
                 07 CA-H-PROPERTY-TYPE   PIC X(15).
                 07 CA-H-BEDROOMS        PIC 9(3).
                 07 CA-H-VALUE           PIC 9(8).
                 07 CA-H-HOUSE-NAME      PIC X(20).
                 07 CA-H-HOUSE-NUMBER    PIC X(4).
                 07 CA-H-POSTCODE        PIC X(8).
                 07 CA-H-FILLER          PIC X(32342).
      *       Motor policy description
              05 CA-MOTOR REDEFINES CA-POLICY-SPECIFIC.
                 07 CA-M-MAKE            PIC X(15).
                 07 CA-M-MODEL           PIC X(15).
                 07 CA-M-VALUE           PIC 9(6).
                 07 CA-M-REGNUMBER       PIC X(7).
                 07 CA-M-COLOUR          PIC X(8).
                 07 CA-M-CC              PIC 9(4).
                 07 CA-M-MANUFACTURED    PIC X(10).
                 07 CA-M-PREMIUM         PIC 9(6).
                 07 CA-M-ACCIDENTS       PIC 9(6).
                 07 CA-M-FILLER          PIC X(32323).
      *       Commercial policy description
              05 CA-COMMERCIAL REDEFINES CA-POLICY-SPECIFIC.
                 07 CA-B-Address         PIC X(255).
                 07 CA-B-Postcode        PIC X(8).
                 07 CA-B-Latitude        PIC X(11).
                 07 CA-B-Longitude       PIC X(11).
                 07 CA-B-Customer        PIC X(255).
                 07 CA-B-PropType        PIC X(255).
                 07 CA-B-FirePeril       PIC 9(4).
                 07 CA-B-FirePremium     PIC 9(8).
                 07 CA-B-CrimePeril      PIC 9(4).
                 07 CA-B-CrimePremium    PIC 9(8).
                 07 CA-B-FloodPeril      PIC 9(4).
                 07 CA-B-FloodPremium    PIC 9(8).
                 07 CA-B-WeatherPeril    PIC 9(4).
                 07 CA-B-WeatherPremium  PIC 9(8).
                 07 CA-B-Status          PIC 9(4).
                 07 CA-B-RejectReason    PIC X(255).
                 07 CA-B-FILLER          PIC X(31298).
      *       CLAIM policy description
              05 CA-CLAIM      REDEFINES CA-POLICY-SPECIFIC.
                 07 CA-C-Num             PIC 9(10).
                 07 CA-C-Date            PIC X(10).
                 07 CA-C-Paid            PIC 9(8).
                 07 CA-C-Value           PIC 9(8).
                 07 CA-C-Cause           PIC X(255).
                 07 CA-C-Observations    PIC X(255).
                 07 CA-C-FILLER          PIC X(31854).
```

### base/src/lgpolicy.cpy
```cobol
      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      ******************************************************************
      *               COPYBOOK for Policy details                      *
      *                                                                *
      *   Structures to map values obtained from DB2 tables:           *
      *   Customer, Policy, Endowment, House and Motor.                *
      *                                                                *
      *   All lengths of policy fields will be defined here so that    *
      *   if any of the DB2 table contents change the lengths will     *
      *   only need to be changed here.                                *
      *                                                                *
      ******************************************************************
       01  WS-POLICY-LENGTHS.
           03 WS-CUSTOMER-LEN          PIC S9(4) COMP VALUE +72.
           03 WS-POLICY-LEN            PIC S9(4) COMP VALUE +72.
           03 WS-ENDOW-LEN             PIC S9(4) COMP VALUE +52.
           03 WS-HOUSE-LEN             PIC S9(4) COMP VALUE +58.
           03 WS-MOTOR-LEN             PIC S9(4) COMP VALUE +65.
           03 WS-COMM-LEN              PIC S9(4) COMP VALUE +1102.
           03 WS-CLAIM-LEN             PIC S9(4) COMP VALUE +546.
           03 WS-FULL-ENDOW-LEN        PIC S9(4) COMP VALUE +124.
           03 WS-FULL-HOUSE-LEN        PIC S9(4) COMP VALUE +130.
           03 WS-FULL-MOTOR-LEN        PIC S9(4) COMP VALUE +137.
           03 WS-FULL-COMM-LEN         PIC S9(4) COMP VALUE +1174.
           03 WS-FULL-CLAIM-LEN        PIC S9(4) COMP VALUE +618.
           03 WS-SUMRY-ENDOW-LEN       PIC S9(4) COMP VALUE +25.

       01  DB2-CUSTOMER.
           03 DB2-FIRSTNAME            PIC X(10).
           03 DB2-LASTNAME             PIC X(20).
           03 DB2-DATEOFBIRTH          PIC X(10).
           03 DB2-HOUSENAME            PIC X(20).
           03 DB2-HOUSENUMBER          PIC X(4).
           03 DB2-POSTCODE             PIC X(8).
           03 DB2-PHONE-MOBILE         PIC X(20).
           03 DB2-PHONE-HOME           PIC X(20).
           03 DB2-EMAIL-ADDRESS        PIC X(100).

       01  DB2-POLICY.
           03 DB2-POLICYTYPE           PIC X.
           03 DB2-POLICYNUMBER         PIC 9(10).
           03 DB2-POLICY-COMMON.
              05 DB2-ISSUEDATE         PIC X(10).
              05 DB2-EXPIRYDATE        PIC X(10).
              05 DB2-LASTCHANGED       PIC X(26).
              05 DB2-BROKERID          PIC 9(10).
              05 DB2-BROKERSREF        PIC X(10).
              05 DB2-PAYMENT           PIC 9(6).

       01  DB2-ENDOWMENT.
           03 DB2-ENDOW-FIXED.
              05 DB2-E-WITHPROFITS      PIC X.
              05 DB2-E-EQUITIES         PIC X.
              05 DB2-E-MANAGEDFUND      PIC X.
              05 DB2-E-FUNDNAME         PIC X(10).
              05 DB2-E-TERM             PIC 9(2).
              05 DB2-E-SUMASSURED       PIC 9(6).
              05 DB2-E-LIFEASSURED      PIC X(31).
           03 DB2-E-PADDINGDATA         PIC X(32611).

       01  DB2-HOUSE.
           03 DB2-H-PROPERTYTYPE       PIC X(15).
           03 DB2-H-BEDROOMS           PIC 9(3).
           03 DB2-H-VALUE              PIC 9(8).
           03 DB2-H-HOUSENAME          PIC X(20).
           03 DB2-H-HOUSENUMBER        PIC X(4).
           03 DB2-H-POSTCODE           PIC X(8).

       01  DB2-MOTOR.
           03 DB2-M-MAKE               PIC X(15).
           03 DB2-M-MODEL              PIC X(15).
           03 DB2-M-VALUE              PIC 9(6).
           03 DB2-M-REGNUMBER          PIC X(7).
           03 DB2-M-COLOUR             PIC X(8).
           03 DB2-M-CC                 PIC 9(4).
           03 DB2-M-MANUFACTURED       PIC X(10).
           03 DB2-M-PREMIUM            PIC 9(6).
           03 DB2-M-ACCIDENTS          PIC 9(6).

       01  DB2-COMMERCIAL.
           03 DB2-B-Address            PIC X(255).
           03 DB2-B-Postcode           PIC X(8).
           03 DB2-B-Latitude           PIC X(11).
           03 DB2-B-Longitude          PIC X(11).
           03 DB2-B-Customer           PIC X(255).
           03 DB2-B-PropType           PIC X(255).
           03 DB2-B-FirePeril          PIC 9(4).
           03 DB2-B-FirePremium        PIC 9(8).
           03 DB2-B-CrimePeril         PIC 9(4).
           03 DB2-B-CrimePremium       PIC 9(8).
           03 DB2-B-FloodPeril         PIC 9(4).
           03 DB2-B-FloodPremium       PIC 9(8).
           03 DB2-B-WeatherPeril       PIC 9(4).
           03 DB2-B-WeatherPremium     PIC 9(8).
           03 DB2-B-Status             PIC 9(4).
           03 DB2-B-RejectReason       PIC X(255).

       01  DB2-CLAIM.
           03 DB2-C-Num                PIC 9(10).
           03 DB2-C-Date               PIC X(10).
           03 DB2-C-Paid               PIC 9(8).
           03 DB2-C-Value              PIC 9(8).
           03 DB2-C-Cause              PIC X(255).
           03 DB2-C-Observations       PIC X(255).
```