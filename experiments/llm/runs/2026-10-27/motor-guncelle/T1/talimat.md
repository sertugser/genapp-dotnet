Sen deneyimli bir C# geliştiricisisin. Aşağıda IBM'in COBOL/CICS örnek uygulaması GenApp'ten bir operasyonun
COBOL kodu var. Bu operasyonu, GenApp'in yeni ASP.NET Core (.NET 10) sürümü için C#'a çevirmeni istiyorum.

## Görev
Operasyon: motor-guncelle
İstek kodu: 01UMOT
HTTP adresi ve yöntemi: PUT /api/customers/{customerNumber}/motor-policies/{policyNumber}

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
   `GenApp.Llm.MotorGuncelle` ad alanında olsun, başka operasyonun kodundan bağımsız çalışsın,
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
### base/src/lgupol01.cbl
```cobol
      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      *                     UPDATE policy details                      *
      *                                                                *
      * Business logic                                                 *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGUPOL01.
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
                                        VALUE 'LGUPOL01------WS'.
           03 WS-TRANSID               PIC X(4).
           03 WS-TERMID                PIC X(4).
           03 WS-TASKNUM               PIC 9(7).
           03 WS-FILLER                PIC X.
           03 WS-ADDR-DFHCOMMAREA      USAGE is POINTER.
           03 WS-CALEN                 PIC S9(4) COMP.

      * Variables for time/date processing
       01  WS-ABSTIME                  PIC S9(8) COMP VALUE +0.
       01  WS-TIME                     PIC X(8)  VALUE SPACES.
       01  WS-DATE                     PIC X(10) VALUE SPACES.

       01  WS-POLICY-LENGTHS.
           03 WS-FULL-ENDOW-LEN        PIC S9(4) COMP VALUE +124.
           03 WS-FULL-HOUSE-LEN        PIC S9(4) COMP VALUE +130.
           03 WS-FULL-MOTOR-LEN        PIC S9(4) COMP VALUE +137.

      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGUPOL01'.
           03 EM-VARIABLE              PIC X(21) VALUE SPACES.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      * Definitions required for data manipulation                     *
      *----------------------------------------------------------------*
      * Fields to be used to calculate minimum commarea length required
      * (for Endowment this does not allow for VARCHAR)
       01  WS-COMMAREA-LENGTHS.
           03 WS-CA-HEADER-LEN         PIC S9(4) COMP VALUE +28.
           03 WS-REQUIRED-CA-LEN       PIC S9(4) COMP VALUE +0.

       01 LGUPDB01                     PIC X(8) VALUE 'LGUPDB01'.

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
      * Check commarea and obtain required details                     *
      *----------------------------------------------------------------*
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

      *----------------------------------------------------------------*
      * Check which policy type is being requested                     *
      *   and chec commarea length                                     *
      *----------------------------------------------------------------*
           EVALUATE CA-REQUEST-ID

             WHEN '01UEND'
               ADD WS-CA-HEADER-LEN  TO WS-REQUIRED-CA-LEN
               ADD WS-FULL-ENDOW-LEN TO WS-REQUIRED-CA-LEN
               IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
                 MOVE '98' TO CA-RETURN-CODE
                 EXEC CICS RETURN END-EXEC
               END-IF

             WHEN '01UHOU'
               ADD WS-CA-HEADER-LEN  TO WS-REQUIRED-CA-LEN
               ADD WS-FULL-HOUSE-LEN TO WS-REQUIRED-CA-LEN
               IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
                 MOVE '98' TO CA-RETURN-CODE
                 EXEC CICS RETURN END-EXEC
               END-IF

             WHEN '01UMOT'
               ADD WS-CA-HEADER-LEN  TO WS-REQUIRED-CA-LEN
               ADD WS-FULL-MOTOR-LEN TO WS-REQUIRED-CA-LEN
               IF EIBCALEN IS LESS THAN WS-REQUIRED-CA-LEN
                 MOVE '98' TO CA-RETURN-CODE
                 EXEC CICS RETURN END-EXEC
               END-IF

             WHEN OTHER
               MOVE '99' TO CA-RETURN-CODE
           END-EVALUATE

           PERFORM UPDATE-POLICY-DB2-INFO.

      * Return to caller
       END-PROGRAM.
           EXEC CICS RETURN END-EXEC.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*

      *================================================================*
      *================================================================*
       UPDATE-POLICY-DB2-INFO.

           EXEC CICS LINK Program(LGUPDB01)
                Commarea(DFHCOMMAREA)
                LENGTH(32500)
           END-EXEC.

           EXIT.

      *================================================================*
      * Procedure to write error message to Queues                     *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
      * Obtain and format current time and date
           EXEC CICS ASKTIME ABSTIME(WS-ABSTIME)
           END-EXEC
           EXEC CICS FORMATTIME ABSTIME(WS-ABSTIME)
                     MMDDYYYY(WS-DATE)
                     TIME(WS-TIME)
           END-EXEC
           MOVE WS-DATE TO EM-DATE
           MOVE WS-TIME TO EM-TIME
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

### base/src/lgupdb01.cbl
```cobol
       PROCESS SQL
      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2021                             *
      *                                                                *
      *                     UPDATE policy details                      *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGUPDB01.
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
                                        VALUE 'LGUPDB01------WS'.
           03 WS-TRANSID               PIC X(4).
           03 WS-TERMID                PIC X(4).
           03 WS-TASKNUM               PIC 9(7).
           03 WS-FILLER                PIC X.
           03 WS-ADDR-DFHCOMMAREA      USAGE is POINTER.
           03 WS-CALEN                 PIC S9(4) COMP.
           03 WS-RETRY                 PIC X.

      * Variables for time/date processing
       01  WS-ABSTIME                  PIC S9(8) COMP VALUE +0.
       01  WS-TIME                     PIC X(8)  VALUE SPACES.
       01  WS-DATE                     PIC X(10) VALUE SPACES.

      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGUPDB01'.
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
      * Fields to be used to calculate minimum commarea length required
      * (for Endowment this does not allow for VARCHAR)
       01  WS-COMMAREA-LENGTHS.
           03 WS-CA-HEADER-LEN         PIC S9(4) COMP VALUE +28.
           03 WS-REQUIRED-CA-LEN       PIC S9(4) COMP VALUE +0.

      * Define a WS-VARYing length character string to contain actual
      * amount of data that will be updated in Varchar field
       01 WS-VARY-FIELD.
          49 WS-VARY-LEN               PIC S9(4) COMP.
          49 WS-VARY-CHAR              PIC X(3900).

      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      * Definitions required by SQL statement                          *
      *   DB2 datatypes to COBOL equivalents                           *
      *     SMALLINT    :   PIC S9(4) COMP                             *
      *     INTEGER     :   PIC S9(9) COMP                             *
      *     DATE        :   PIC X(10)                                  *
      *     TIMESTAMP   :   PIC X(26)                                  *
      *----------------------------------------------------------------*
      * Host variables for input to DB2 integer types
      * Any values specified in SQL stmts must be defined here so
      * available to SQL pre-compiler
       01 DB2-IN-INTEGERS.
          03 DB2-CUSTOMERNUM-INT       PIC S9(9) COMP.
          03 DB2-POLICYNUM-INT         PIC S9(9) COMP.
          03 DB2-BROKERID-INT          PIC S9(9) COMP.
          03 DB2-PAYMENT-INT           PIC S9(9) COMP.
          03 DB2-E-TERM-SINT           PIC S9(4) COMP.
          03 DB2-E-SUMASSURED-INT      PIC S9(9) COMP.
          03 DB2-H-BEDROOMS-SINT       PIC S9(4) COMP.
          03 DB2-H-VALUE-INT           PIC S9(9) COMP.
          03 DB2-M-VALUE-INT           PIC S9(9) COMP.
          03 DB2-M-CC-SINT             PIC S9(4) COMP.
          03 DB2-M-PREMIUM-INT         PIC S9(9) COMP.
          03 DB2-M-ACCIDENTS-INT       PIC S9(9) COMP.

      *  Host variables to store result of DB2 Fetch
      *  Must be an SQL INCLUDE so available to SQL pre-compiler
           EXEC SQL
             INCLUDE LGPOLICY
           END-EXEC.

      * Indicator variables for columns which could return nulls
      *   if these are not specified and SQL FETCH tries to return
      *   null value for a listed column it will FAIL with SQLCODE=
       77  IND-BROKERID                PIC S9(4) COMP.
       77  IND-BROKERSREF              PIC S9(4) COMP.
       77  IND-PAYMENT                 PIC S9(4) COMP.
       77  LGUPVS01                    Pic X(8) value 'LGUPVS01'.
      *----------------------------------------------------------------*

      *----------------------------------------------------------------*
      *    DB2 CONTROL
      *----------------------------------------------------------------*
      * SQLCA DB2 communications area
           EXEC SQL
             INCLUDE SQLCA
           END-EXEC.

      *----------------------------------------------------------------*
      * Declare Cursors
      *----------------------------------------------------------------*
      * Cursor to select details from policy table (with lock)
      * will NOT update payment or commission fields
           EXEC SQL
             DECLARE POLICY_CURSOR CURSOR WITH HOLD FOR
               SELECT ISSUEDATE,
                      EXPIRYDATE,
                      LASTCHANGED,
                      BROKERID,
                      BROKERSREFERENCE
               FROM POLICY
               WHERE ( CUSTOMERNUMBER = :DB2-CUSTOMERNUM-INT AND
                       POLICYNUMBER = :DB2-POLICYNUM-INT )
               FOR UPDATE OF ISSUEDATE,
                             EXPIRYDATE,
                             LASTCHANGED,
                             BROKERID,
                             BROKERSREFERENCE
           END-EXEC.

      ******************************************************************
      *    L I N K A G E     S E C T I O N
      ******************************************************************
       LINKAGE SECTION.

       01  DFHCOMMAREA.
           EXEC SQL
             INCLUDE LGCMAREA
           END-EXEC.


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
           MOVE SPACES   TO WS-RETRY.
      *----------------------------------------------------------------*
      * initialize DB2 host variables
           INITIALIZE DB2-POLICY.
           INITIALIZE DB2-IN-INTEGERS.

      *----------------------------------------------------------------*
      * Check commarea and obtain required details                     *
      *----------------------------------------------------------------*
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

      * Convert commarea customer & policy nums to DB2 integer format
           MOVE CA-CUSTOMER-NUM TO DB2-CUSTOMERNUM-INT
           MOVE CA-POLICY-NUM   TO DB2-POLICYNUM-INT
      * and save in error msg field incase required
           MOVE CA-CUSTOMER-NUM TO EM-CUSNUM
           MOVE CA-POLICY-NUM   TO EM-POLNUM

      *----------------------------------------------------------------*
      * Check which policy type is being requested                     *
      *   and check commarea length                                    *
      *----------------------------------------------------------------*

      *    Call procedure to update required tables
           PERFORM UPDATE-POLICY-DB2-INFO.

           EXEC CICS LINK Program(LGUPVS01)
                Commarea(DFHCOMMAREA)
                LENGTH(225)
           END-EXEC.

      * Return to caller
       END-PROGRAM.
           EXEC CICS RETURN END-EXEC.

       MAINLINE-EXIT.
           EXIT.
      *----------------------------------------------------------------*

      *================================================================*
      * Fetch a row from Policy tables using POLICY-CURSOR             *
      *   Host variables specified on INTO statement must correspond   *
      *   in order and size to columns specified on SELECT statement   *
      *   in CURSOR defintion.                                         *
      *================================================================*
       FETCH-DB2-POLICY-ROW.
           MOVE ' FETCH  ROW   ' TO EM-SQLREQ
           EXEC SQL
             FETCH POLICY_CURSOR
             INTO  :DB2-ISSUEDATE,
                   :DB2-EXPIRYDATE,
                   :DB2-LASTCHANGED,
                   :DB2-BROKERID-INT INDICATOR :IND-BROKERID,
                   :DB2-BROKERSREF INDICATOR :IND-BROKERSREF,
                   :DB2-PAYMENT-INT INDICATOR :IND-PAYMENT
           END-EXEC
           EXIT.

      *================================================================*
      * 1) Use SELECT FOR UPDATE to obtain a lock on the row in the    *
      *    policy table, check that Timestamp in database matches that *
      *    received in commarea:                                       *
      * 2a) if not: unlock DB2 record, abandon update & return to user *
      * 2b) if match: update policy specific table with data from      *
      *     commarea                                                   *
      * 3) update policy table with data from commarea and new         *
      *    timestamp (which releases row lock on policy table)         *
      *================================================================*
       UPDATE-POLICY-DB2-INFO.

      *    Open the cursor.
           MOVE ' OPEN   PCURSOR ' TO EM-SQLREQ
           EXEC SQL
             OPEN POLICY_CURSOR
           END-EXEC

           Evaluate SQLCODE
             When 0
               MOVE '00' TO CA-RETURN-CODE
             When -913
               MOVE '90' TO CA-RETURN-CODE
               PERFORM WRITE-ERROR-MESSAGE
               EXEC CICS RETURN END-EXEC
             When Other
               MOVE '90' TO CA-RETURN-CODE
               PERFORM WRITE-ERROR-MESSAGE
               EXEC CICS RETURN END-EXEC
           END-Evaluate.

      *    Fetch the first row (we only expect one matching row)
           PERFORM FETCH-DB2-POLICY-ROW

           IF SQLCODE = 0
      *      Fetch was successful
      *      Compare timestamp in commarea with that in DB2
             IF CA-LASTCHANGED EQUAL TO DB2-LASTCHANGED

      *----------------------------------------------------------------*
      *      Select for Update and Update specific policy type table   *
      *----------------------------------------------------------------*
             EVALUATE CA-REQUEST-ID

      *** Endowment ***
               WHEN '01UEND'
      *          Call routine to update Endowment table
                 PERFORM UPDATE-ENDOW-DB2-INFO

      *** House ***
               WHEN '01UHOU'
      *          Call routine to update Housetable
                 PERFORM UPDATE-HOUSE-DB2-INFO

      *** Motor ***
               WHEN '01UMOT'
      *          Call routine to update Motor table
                 PERFORM UPDATE-MOTOR-DB2-INFO

             END-EVALUATE
      *----------------------------------------------------------------*
              IF CA-RETURN-CODE NOT EQUAL '00'
      *         Update policy type specific table has failed
      *         So close cursor and return
                PERFORM CLOSE-PCURSOR
                EXEC CICS RETURN END-EXEC
              END-IF

      *----------------------------------------------------------------*
      *        Now update Policy table and set new timestamp           *
      *----------------------------------------------------------------*
      *        Move numeric commarea fields to integer format
               MOVE CA-BROKERID      TO DB2-BROKERID-INT
               MOVE CA-PAYMENT       TO DB2-PAYMENT-INT

      *        Update policy table details
               MOVE ' UPDATE POLICY  ' TO EM-SQLREQ
               EXEC SQL
                 UPDATE POLICY
                   SET ISSUEDATE        = :CA-ISSUE-DATE,
                       EXPIRYDATE       = :CA-EXPIRY-DATE,
                       LASTCHANGED      = CURRENT TIMESTAMP ,
                       BROKERID         = :DB2-BROKERID-INT,
                       BROKERSREFERENCE = :CA-BROKERSREF
                   WHERE CURRENT OF POLICY_CURSOR
               END-EXEC

      *        get value of assigned Timestamp for return in commarea
               EXEC SQL
                 SELECT LASTCHANGED
                   INTO :CA-LASTCHANGED
                   FROM POLICY
                   WHERE POLICYNUMBER = :DB2-POLICYNUM-INT
               END-EXEC

               IF SQLCODE NOT EQUAL 0
      *          Non-zero SQLCODE from Update of policy table
                   EXEC CICS SYNCPOINT ROLLBACK END-EXEC
                   MOVE '90' TO CA-RETURN-CODE
      *            Write error message to TD QUEUE(CSMT)
                   PERFORM WRITE-ERROR-MESSAGE
               END-IF

             ELSE
      *        Timestamps do not match (policy table v commarea)
               MOVE '02' TO CA-RETURN-CODE
             END-IF

           ELSE
      *      Non-zero SQLCODE from first SQL FETCH statement
             IF SQLCODE EQUAL 100
               MOVE '01' TO CA-RETURN-CODE
             ELSE
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.
      *    Now close the Cursor and we're done!
           PERFORM CLOSE-PCURSOR.

       CLOSE-PCURSOR.
      *    Now close the Cursor and we're done!
           MOVE ' CLOSE  PCURSOR' TO EM-SQLREQ
           EXEC SQL
             CLOSE POLICY_CURSOR
           END-EXEC.

           Evaluate SQLCODE
             When 0
               MOVE '00' TO CA-RETURN-CODE
             When -501
               MOVE '00' TO CA-RETURN-CODE
               MOVE '-501 detected c' TO EM-SQLREQ
               EXEC CICS RETURN END-EXEC
             When Other
               MOVE '90' TO CA-RETURN-CODE
               PERFORM WRITE-ERROR-MESSAGE
               EXEC CICS RETURN END-EXEC
           END-Evaluate.
           EXIT.

      *================================================================*
      * Update row in Endowment table which matches customer and       *
      * policy number requested.                                       *
      *================================================================*
       UPDATE-ENDOW-DB2-INFO.

      *    Move numeric commarea fields to DB2 Integer formats
           MOVE CA-E-TERM        TO DB2-E-TERM-SINT
           MOVE CA-E-SUM-ASSURED TO DB2-E-SUMASSURED-INT

           MOVE ' UPDATE ENDOW ' TO EM-SQLREQ
           EXEC SQL
             UPDATE ENDOWMENT
               SET
                 WITHPROFITS   = :CA-E-WITH-PROFITS,
                   EQUITIES    = :CA-E-EQUITIES,
                   MANAGEDFUND = :CA-E-MANAGED-FUND,
                   FUNDNAME    = :CA-E-FUND-NAME,
                   TERM        = :DB2-E-TERM-SINT,
                   SUMASSURED  = :DB2-E-SUMASSURED-INT,
                   LIFEASSURED = :CA-E-LIFE-ASSURED
               WHERE
                   POLICYNUMBER = :DB2-POLICYNUM-INT
           END-EXEC

           IF SQLCODE NOT EQUAL 0
      *      Non-zero SQLCODE from UPDATE statement
             IF SQLCODE EQUAL 100
               MOVE '01' TO CA-RETURN-CODE
             ELSE
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.
           EXIT.

      *================================================================*
      * Update row in House table which matches customer and           *
      * policy number requested.                                       *
      *================================================================*
       UPDATE-HOUSE-DB2-INFO.

      *    Move numeric commarea fields to DB2 Integer formats
           MOVE CA-H-BEDROOMS    TO DB2-H-BEDROOMS-SINT
           MOVE CA-H-VALUE       TO DB2-H-VALUE-INT

           MOVE ' UPDATE HOUSE ' TO EM-SQLREQ
           EXEC SQL
             UPDATE HOUSE
               SET
                    PROPERTYTYPE = :CA-H-PROPERTY-TYPE,
                    BEDROOMS     = :DB2-H-BEDROOMS-SINT,
                    VALUE        = :DB2-H-VALUE-INT,
                    HOUSENAME    = :CA-H-HOUSE-NAME,
                    HOUSENUMBER  = :CA-H-HOUSE-NUMBER,
                    POSTCODE     = :CA-H-POSTCODE
               WHERE
                    POLICYNUMBER = :DB2-POLICYNUM-INT
           END-EXEC

           IF SQLCODE NOT EQUAL 0
      *      Non-zero SQLCODE from UPDATE statement
             IF SQLCODE = 100
               MOVE '01' TO CA-RETURN-CODE
             ELSE
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.
           EXIT.

      *================================================================*
      * Update row in Motor table which matches customer and           *
      * policy number requested.                                       *
      *================================================================*
       UPDATE-MOTOR-DB2-INFO.

      *    Move numeric commarea fields to DB2 Integer formats
           MOVE CA-M-CC          TO DB2-M-CC-SINT
           MOVE CA-M-VALUE       TO DB2-M-VALUE-INT
           MOVE CA-M-PREMIUM     TO DB2-M-PREMIUM-INT
           MOVE CA-M-ACCIDENTS   TO DB2-M-ACCIDENTS-INT

           MOVE ' UPDATE MOTOR ' TO EM-SQLREQ
           EXEC SQL
             UPDATE MOTOR
               SET
                    MAKE              = :CA-M-MAKE,
                    MODEL             = :CA-M-MODEL,
                    VALUE             = :DB2-M-VALUE-INT,
                    REGNUMBER         = :CA-M-REGNUMBER,
                    COLOUR            = :CA-M-COLOUR,
                    CC                = :DB2-M-CC-SINT,
                    YEAROFMANUFACTURE = :CA-M-MANUFACTURED,
                    PREMIUM           = :DB2-M-PREMIUM-INT,
                    ACCIDENTS         = :DB2-M-ACCIDENTS-INT
               WHERE
                    POLICYNUMBER      = :DB2-POLICYNUM-INT
           END-EXEC

           IF SQLCODE NOT EQUAL 0
      *      Non-zero SQLCODE from UPDATE statement
             IF SQLCODE EQUAL 100
               MOVE '01' TO CA-RETURN-CODE
             ELSE
               MOVE '90' TO CA-RETURN-CODE
      *        Write error message to TD QUEUE(CSMT)
               PERFORM WRITE-ERROR-MESSAGE
             END-IF
           END-IF.
           EXIT.

      *================================================================*
      * Procedure to write error message to Queues                     *
      *   message will include Date, Time, Program Name, Customer      *
      *   Number, Policy Number and SQLCODE.                           *
      *================================================================*
       WRITE-ERROR-MESSAGE.
      * Save SQLCODE in message
           MOVE SQLCODE TO EM-SQLRC
      * Obtain and format current time and date
           EXEC CICS ASKTIME ABSTIME(WS-ABSTIME)
           END-EXEC
           EXEC CICS FORMATTIME ABSTIME(WS-ABSTIME)
                     MMDDYYYY(WS-DATE)
                     TIME(WS-TIME)
           END-EXEC
           MOVE WS-DATE TO EM-DATE
           MOVE WS-TIME TO EM-TIME
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

### base/src/lgupvs01.cbl
```cobol
      ******************************************************************
      *                                                                *
      * (C) Copyright IBM Corp. 2011, 2020                             *
      *                                                                *
      *                    Update Policy - VSAM                        *
      *                                                                *
      * VSAM KSDS Policy record Update                                 *
      *                                                                *
      ******************************************************************
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LGUPVS01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
      *
       DATA DIVISION.
       WORKING-STORAGE SECTION.

       01  WS-RESP                   PIC S9(8) COMP.
       01  WS-RESP2                  PIC S9(8) COMP.
       01  WS-Comm-Len               PIC S9(8) COMP.
       01  WS-STARTCODE              PIC XX Value spaces.
       01  WS-SYSID                  PIC X(4) Value spaces.
       01  WS-Commarea-Len           PIC S9(4) COMP Value 64.
       01  WS-Commarea-LenF          PIC S9(4) COMP Value 64.
      ******************************
       01  WF-Policy-Info.
         03  WF-Policy-Key.
           05  WF-Request-ID           Pic X.
           05  WF-Customer-Num         Pic X(10).
           05  WF-Policy-Num           Pic X(10).
         03 WF-Policy-Data             Pic X(43).
         03 WF-C-Policy-Data Redefines WF-Policy-Data.
           05  WF-B-Postcode           Pic X(8).
           05  WF-B-Status             Pic 9(4).
           05  WF-B-Customer           Pic X(31).
         03 WF-E-Policy-Data Redefines WF-Policy-Data.
           05  WF-E-WITH-PROFITS       Pic X.
           05  WF-E-EQUITIES           Pic X.
           05  WF-E-MANAGED-FUND       Pic X.
           05  WF-E-FUND-NAME          Pic X(10).
           05  WF-E-LIFE-ASSURED       Pic X(30).
         03 WF-H-Policy-Data Redefines WF-Policy-Data.
           05  WF-H-PROPERTY-TYPE      Pic X(15).
           05  WF-H-BEDROOMS           Pic 999.
           05  WF-H-VALUE              Pic 9(8).
           05  WF-H-POSTCODE           Pic X(8).
           05  WF-H-HOUSE-NAME         Pic X(9).
         03 WF-M-Policy-Data Redefines WF-Policy-Data.
           05  WF-M-MAKE               Pic X(15).
           05  WF-M-MODEL              Pic X(15).
           05  WF-M-VALUE              Pic 9(6).
           05  WF-M-REGNUMBER          Pic X(7).
      *
       01  WS-FileIn                   Pic X(1024) value Spaces.
      ******************************
      * Variables for time/date processing
       01  WS-ABSTIME                  PIC S9(8) COMP VALUE +0.
       01  WS-TIME                     PIC X(8)  VALUE SPACES.
       01  WS-DATE                     PIC X(10) VALUE SPACES.
      * Error Message structure
       01  ERROR-MSG.
           03 EM-DATE                  PIC X(8)  VALUE SPACES.
           03 FILLER                   PIC X     VALUE SPACES.
           03 EM-TIME                  PIC X(6)  VALUE SPACES.
           03 FILLER                   PIC X(9)  VALUE ' LGUPVS01'.
           03 EM-VARIABLE.
             05 FILLER                 PIC X(6)  VALUE ' PNUM='.
             05 EM-POLNUM              PIC X(10)  VALUE SPACES.
             05 FILLER                 PIC X(6)  VALUE ' CNUM='.
             05 EM-CUSNUM              PIC X(10)  VALUE SPACES.
             05 FILLER                 PIC X(20)
                                        Value ' Re-write  KSDSPOLY '.
             05 FILLER                 PIC X(6)  VALUE ' RESP='.
             05 EM-RESPRC              PIC +9(5) USAGE DISPLAY.
             05 FILLER                 PIC X(7)  VALUE ' RESP2='.
             05 EM-RESP2RC             PIC +9(5) USAGE DISPLAY.

       01  CA-ERROR-MSG.
           03 FILLER                   PIC X(9)  VALUE 'COMMAREA='.
           03 CA-DATA                  PIC X(90) VALUE SPACES.

      ******************************
       77 Eyecatcher               PIC X(16)
                                      Value 'Program LGUPVS01'.
      *****************************************************************
      *    L I N K A G E     S E C T I O N
      *****************************************************************
       LINKAGE SECTION.
       01  DFHCOMMAREA.
         Copy LGCMAREA.

      *----------------------------------------------------------------*
      *****************************************************************
       PROCEDURE DIVISION.

      *---------------------------------------------------------------*
       MAINLINE SECTION.
      *
      *---------------------------------------------------------------*
           Move EIBCALEN To WS-Commarea-Len.
      *---------------------------------------------------------------*
           Move CA-Request-ID(4:1) To WF-Request-ID
           Move CA-Policy-Num      To WF-Policy-Num
           Move CA-Customer-Num    To WF-Customer-Num

           Evaluate WF-Request-ID

             When 'C'
               Move CA-B-Postcode  To WF-B-Postcode
               Move CA-B-Status    To WF-B-Status
               Move CA-B-Customer  To WF-B-Customer

             When 'E'
               Move CA-E-WITH-PROFITS To  WF-E-WITH-PROFITS
               Move CA-E-EQUITIES     To  WF-E-EQUITIES
               Move CA-E-MANAGED-FUND To  WF-E-MANAGED-FUND
               Move CA-E-FUND-NAME    To  WF-E-FUND-NAME
               Move CA-E-LIFE-ASSURED To  WF-E-LIFE-ASSURED

             When 'H'
               Move CA-H-PROPERTY-TYPE To  WF-H-PROPERTY-TYPE
               Move CA-H-BEDROOMS      To  WF-H-BEDROOMS
               Move CA-H-VALUE         To  WF-H-VALUE
               Move CA-H-POSTCODE      To  WF-H-POSTCODE
               Move CA-H-HOUSE-NAME    To  WF-H-HOUSE-NAME

             When 'M'
               Move CA-M-MAKE          To  WF-M-MAKE
               Move CA-M-MODEL         To  WF-M-MODEL
               Move CA-M-VALUE         To  WF-M-VALUE
               Move CA-M-REGNUMBER     To  WF-M-REGNUMBER

             When Other
               Move Spaces To WF-Policy-Data
           End-Evaluate

           Move CA-Policy-Num      To WF-Policy-Num
      *---------------------------------------------------------------*
           Exec CICS Read File('KSDSPOLY')
                     Into(WS-FileIn)
                     Length(WS-Commarea-Len)
                     Ridfld(WF-Policy-Key)
                     KeyLength(21)
                     RESP(WS-RESP)
                     Update
           End-Exec.
           If WS-RESP Not = DFHRESP(NORMAL)
             Move EIBRESP2 To WS-RESP2
             MOVE '81' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             EXEC CICS ABEND ABCODE('LGV3') NODUMP END-EXEC
             EXEC CICS RETURN END-EXEC
           End-If.
      *---------------------------------------------------------------*
           Exec CICS ReWrite File('KSDSPOLY')
                     From(WF-Policy-Info)
                     Length(WS-Commarea-LenF)
                     RESP(WS-RESP)
           End-Exec.
           If WS-RESP Not = DFHRESP(NORMAL)
             Move EIBRESP2 To WS-RESP2
             MOVE '82' TO CA-RETURN-CODE
             PERFORM WRITE-ERROR-MESSAGE
             EXEC CICS ABEND ABCODE('LGV4') NODUMP END-EXEC
             EXEC CICS RETURN END-EXEC
           End-If.

      *---------------------------------------------------------------*

       A-EXIT.
           EXIT.
           GOBACK.
      *---------------------------------------------------------------*
       WRITE-ERROR-MESSAGE.
           EXEC CICS ASKTIME ABSTIME(WS-ABSTIME)
           END-EXEC
           EXEC CICS FORMATTIME ABSTIME(WS-ABSTIME)
                     MMDDYYYY(WS-DATE)
                     TIME(WS-TIME)
           END-EXEC
      *
           MOVE WS-DATE TO EM-DATE
           MOVE WS-TIME TO EM-TIME
           Move CA-Customer-Num To EM-Cusnum
           Move WS-RESP         To EM-RespRC
           Move WS-RESP2        To EM-Resp2RC
           EXEC CICS LINK PROGRAM('LGSTSQ')
                     COMMAREA(ERROR-MSG)
                     LENGTH(LENGTH OF ERROR-MSG)
           END-EXEC.
           IF EIBCALEN > 0 THEN
             IF EIBCALEN < 91 THEN
               MOVE DFHCOMMAREA(1:EIBCALEN) TO CA-DATA
               EXEC CICS LINK PROGRAM('LGSTSQ')
                         COMMAREA(CA-ERROR-MSG)
                         LENGTH(Length Of CA-ERROR-MSG)
               END-EXEC
             ELSE
               MOVE DFHCOMMAREA(1:90) TO CA-DATA
               EXEC CICS LINK PROGRAM('LGSTSQ')
                         COMMAREA(CA-ERROR-MSG)
                         LENGTH(Length Of CA-ERROR-MSG)
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