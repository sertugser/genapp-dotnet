# ADR 0002: REST API ve dönüş kodu eşlemesi

- **Durum:** Önerildi
- **Tarih:** 2026-10-10
- **İlgili:** [API tasarımı](../api-design.md), [SRS](../srs.md), ADR 0001 (katmanlı mimari), ADR 0003 (veri ve transaction), ADR 0005 (test mimarisi)

## Bağlam

Eski GenApp'te kullanıcı 3270 ekranlarından işlem yapar. Ekran programı bir COMMAREA hazırlar,
içine işlemin türünü belirten bir istek kodu (`01AMOT`, `01ICUS` gibi) yazar ve iş programını
çağırır. Sonuç, COMMAREA'daki iki haneli bir dönüş koduyla (`00`, `01`, `90` gibi) geri gelir.

Yeni sistemde 3270 ekranlarının yerine bir web arayüzü gelecek, işlemler bir API üzerinden
yapılacak. Bu API'nin biçimine karar vermemiz gerekiyordu. Kararı etkileyen noktalar:

1. Web arayüzü ve otomatik testler aynı API'yi kullanacak.
2. Eşdeğerlik testleri API'yi dışarıdan çağıracak (ADR 0005). Testlerin, yeni sistemin cevabını
   eski sistemin dönüş koduyla karşılaştırabilmesi gerekiyor.
3. Eski sistemde 12 farklı dönüş kodu var; bunların bir kısmı aynı türden hatayı anlatıyor
   (örneğin `80`, `81`, `82` VSAM hataları).
4. Eski sistemde her poliçe işlemi müşteri numarası ve poliçe numarasıyla yapılıyor. Dört poliçe
   türü aynı programları kullanıyor, ama her türün alanları farklı.
5. Ekip ASP.NET Core kullanıyor ve proje kısa sürede bitecek.

## Karar

**1. API, JSON kullanan bir REST API olacak.**

**2. Adresler kaynak odaklı olacak.** Adresler işlemin adını değil, üzerinde işlem yapılan şeyi
(müşteri, poliçe) gösterir. İşlemin ne olduğu HTTP yönteminden anlaşılır: ekleme `POST`, sorgulama
`GET`, güncelleme `PUT`, silme `DELETE`.

**3. Poliçeler müşterinin altında, türlerine göre ayrı adreslerde olacak:**
`/api/customers/{customerNumber}/motor-policies/{policyNumber}`. Her poliçe bir müşteriye bağlı
olduğu ve eski sistem her poliçe işleminde müşteri numarasını istediği için poliçe adresi müşteri
adresinin altındadır. Türler ayrı adreslerdedir, çünkü her türün alanları farklıdır ve her adres
kendi sabit veri biçimini kullanabilir.

**4. Ticari poliçe sorgulamanın dört biçimi ayrı adreslerle karşılanacak.** Müşteri ve poliçe
numarasıyla tek kayıt, yalnızca poliçe numarasıyla tek kayıt, müşteri numarasıyla liste ve posta
koduyla liste (`?postcode=`). Tek kayıt dönen sorgular kaynak adresiyle, liste dönen sorgular
koleksiyon adresiyle yapılır.

**5. Hatalar RFC 9457 "Problem Details" biçiminde dönecek ve eski dönüş kodunu taşıyacak.**
HTTP durum kodu hatanın türünü söyler (404, 409, 500). Gövdedeki `returnCode` alanı ise eski
sistemin iki haneli kodunu taşır. Eşleme tablosu [API tasarımı](../api-design.md) belgesinin
3. bölümündedir. Özetle:

| Eski kod | HTTP |
|---|---|
| `00` | 200 / 201 / 204 |
| `01`, `70` | 404 |
| `02` | 409 |
| `80`, `81`, `82`, `88`, `89`, `90` | 500 |
| `98` | 400 (doğrulama hatası) |
| `99` | oluşmaz |

**6. Metinler kırpılmış döner.** Eski sistemde metin alanları sabit uzunluktadır ve sonları
boşlukla doldurulur (`"Andrew    "`). Veritabanında metinler `varchar` olarak saklandığı için
(bkz. `docs/database-schema.md`) API metinleri sonlarındaki boşluklar kırpılmış olarak döndürür
(`"Andrew"`). Eşdeğerlik testleri metinleri karşılaştırırken eski sistemin değerinin sonundaki
boşlukları yok sayar.

**7. Eşzamanlı güncelleme `lastChanged` alanıyla yapılacak.** Sorgulama cevabındaki `lastChanged`
değeri güncelleme isteğinde geri gönderilir; uyuşmazsa 409 döner. Bu, eski sistemdeki
`LASTCHANGED` kontrolünün karşılığıdır.

**8. Db2'ye özgü kilitlenme ve zaman aşımı durumu bilerek farklı ele alınacak.** Eski sistemde
müşteri sorgulamada Db2'nin kilitlenme/zaman aşımı kodu (SQLCODE -913) "bulunamadı" (`01`) olarak
döner. Yeni sistemde PostgreSQL'in kilitlenme ve zaman aşımı hataları `503 Service Unavailable`
(`type: database-busy`, `returnCode: "90"`) olarak döner.

## Gerekçe

- **Neden REST:** Web arayüzünün ve test araçlarının en kolay kullandığı yaklaşım. ASP.NET Core
  bunu doğrudan destekler ve OpenAPI açıklama sayfasını otomatik üretir. Ekibin öğrenme yükü en
  düşük seçenek.
- **Neden kaynak odaklı adresler:** Adres neyin üzerinde çalışıldığını, HTTP yöntemi ne yapıldığını
  söyler. Böylece 18 işlem, öğrenmesi kolay tek bir kalıba oturur. İstek kodlarını adrese taşımak
  (`/api/01IMOT`) eski sistemin iç ayrıntısını dışarı açardı.
- **Neden `returnCode` alanı:** HTTP durum kodları eski kodlardan daha kaba; örneğin `01` ve `70`
  ikisi de 404'tür. Eşdeğerlik testlerinin eski sistemle tam karşılaştırma yapabilmesi için eski
  kodun cevapta görünmesi gerekiyor. RFC 9457, standart bir hata gövdesine ek alan koymaya izin
  verir.
- **Neden kırpılmış metin:** JSON'da sonu boşluklu metinler istemcide sürekli kırpma gerektirir ve
  hata kaynağıdır. Sondaki boşluklar eski sistemde verinin anlamına değil, alanın sabit
  uzunluğuna aittir.
- **Neden -913 için farklı davranış:** Kilitlenme geçici bir durumdur; kullanıcıya "kayıt yok"
  demek yanlış bilgi verir ve kullanıcı var olan bir müşteriyi yeniden eklemeye kalkabilir.
  PostgreSQL'de Db2'nin bu koduyla bire bir aynı bir durum da yoktur. Bu durum testte
  kendiliğinden üretilemediği için eşdeğerlik ölçümünü etkilemez.

## Değerlendirilen seçenekler

| Seçenek | Neden seçilmedi |
|---|---|
| İstek kodlarını adres yapmak (`POST /api/01AMOT`) | Eski sistemin iç ayrıntısını dışarı açar, REST kalıbına uymaz, okunması zor. |
| Tek poliçe adresi, tür bir alanla belirtilir (`/api/policies` + `"type": "motor"`) | Her türün alanları farklı olduğu için tek adresin veri biçimi karmaşıklaşır ve doğrulaması zorlaşır. |
| gRPC | Tarayıcıdan doğrudan kullanılamaz, web arayüzü için ek katman gerekir. |
| GraphQL | 18 sabit işlem için gereğinden karmaşık; ekip için yeni bir öğrenme yükü. |
| Bütün hatalar için 200 + gövdede dönüş kodu | HTTP'nin hata kodlarını kullanmaz; araçlar ve tarayıcı hatayı tanıyamaz. |

## Sonuçlar

**Olumlu**
- 18 işlem tek ve tutarlı bir kalıba oturur; web arayüzü ve testler aynı adresleri kullanır.
- Eşdeğerlik testleri hem HTTP kodunu hem eski dönüş kodunu kontrol edebilir.
- OpenAPI sayfası API'nin güncel açıklamasını otomatik verir.

**Olumsuz ve kabul ettiklerimiz**
- COMMAREA'ya ait kurallar (`98` gibi) API üzerinden test edilemez; bunlar yalnızca GnuCOBOL'da
  test edilir.
- Metin kırpma ve -913 kararları, eski sistemle bilinçli olarak farklı olduğumuz iki noktadır.
  Raporda "bilinçli sapmalar" olarak yazılacaktır.
- `80`, `81`, `82` kodları VSAM'a aittir; yeni sistemde VSAM için ayrı bir depolama olmadığı için
  (bkz. `docs/database-schema.md`, kesin karar ADR 0003'te) bu kodlar pratikte oluşmayacaktır.
