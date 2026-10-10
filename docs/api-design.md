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
- **Kilitlenme ve zaman aşımı:** Veritabanı kilitlenmesi ya da zaman aşımı `503 Service Unavailable`
  (`type: database-busy`, `returnCode: "90"`) olarak döner. Eski sistemden bilinçli farkı ADR 0002'de
  (Karar 8) açıklanmıştır.

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
tekrar denemelidir. Bu, eski sistemdeki `LASTCHANGED` alanının karşılığıdır.

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
