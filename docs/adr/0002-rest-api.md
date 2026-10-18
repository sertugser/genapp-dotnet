# ADR 0002: REST API ve dönüş kodu eşlemesi

- **Durum:** Önerildi
- **Tarih:** 2026-10-10 (güncelleme 2026-10-13: Karar 8 bütün operasyonlara genişletildi, Karar 9 eklendi; güncelleme 2026-10-16: Karar 9'a iki ezilme yolu, yanlış türden poliçe ve 10 haneli numara eklendi, iki korunan davranış yazıldı; güncelleme 2026-10-18: Karar 9'a ticari sorgu durumları eklendi)
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
döner (`lgicdb01.cbl:197-198`); poliçe güncellemede (`lgupdb01.cbl:262-263`) ve diğer
operasyonlarda o adımın genel hata kodu olarak döner: çoğunlukla `90`, ticari poliçe liste
sorgularında imleç adımlarında `88` ya da `89` (`lgipdb01.cbl:844`, `:856`). Yeni sistemde **bütün operasyonlarda**
PostgreSQL'in kilitlenme ve zaman aşımı hataları `503 Service Unavailable`
(`type: database-busy`, `returnCode: "90"`) olarak döner.

**9. Eski davranış korunacak; yalnızca aşağıdaki durumlarda bilerek farklı davranılacak.** Yeni
sistem eski sistemle aynı davranır. Yalnızca şu durumlarda farklı davranır ve her biri bilinçli sapma
olarak kaydedilir:

- **Eski kodun bir hata yüzünden kendi koyduğu dönüş kodunu ezdiği yerler.** Poliçe güncellemede
  `LGUPDB01`, çakışmada `02`, poliçe yoksa `01`, okuma hatasında `90` yazar (`lgupdb01.cbl:346`,
  `:352`, `:354`), ama ardından imleci kapatırken bunu `00` ile ezer (`:360`, `:371`). Çağıran
  "başarılı" görür, oysa kayıt güncellenmemiştir. Aynı ezilme iki yerde daha olur: tür tablosundaki
  satır güncellenemeyince (`:302-307`) ve `POLICY` güncellemesinden sonraki `SELECT` başarısız olup
  `SYNCPOINT ROLLBACK` imleci kapattığında `CLOSE`'un `-501` yolunda (`:336-342`, `:372-375`).
  Yeni sistem kodun yazdığı değeri döndürür: `409`/`"02"`, `404`/`"01"`, `500`/`"90"`.
- **Eski davranışın yeni sistemde oluşamadığı yerler.** Müşteri güncellemede müşteri yoksa eski
  sistem `01` yazar (`lgucdb01.cbl:173-174`), ama ardından VSAM okuması başarısız olur ve program
  çöker (`lgucvs01.cbl:77-81`). Yeni sistemde VSAM olmadığı için (ADR 0003) `404`/`"01"` döner.
- **Eski kodun girdiyi denetlemediği için başka bir kayıt üzerinde işlem yaptığı yerler.**
  - *Poliçe türü:* Poliçe programları `POLICY.POLICYTYPE`'ı hiç okumaz; bir motor poliçesi konut
    adresinden silinebilir (`lgdpdb01.cbl:189-194`). Yeni sistem poliçenin türü adresteki türle
    uyuşmuyorsa işlem yapmaz ve `404`/`"01"` döner (sorgula, güncelle ve silde).
  - *10 haneli numara:* Müşteri ve poliçe numaraları Db2'ye gitmeden 9 haneli alanlara taşınır ve en
    soldaki hane kesilir (`lgapdb01.cbl:90`, `:176`): `1000000004` müşteri 4 olur. Yeni sistem numarayı
    kesmez; o numarada kayıt yoksa `404` döner.
- **Ticari liste sorgularında eski davranışın veriyi kaybettirdiği ya da bitmediği yerler.** Müşteri
  ve posta koduyla sorguda her kayıt bir öncekinin üzerine yazılır ve yalnızca son kayıt kalır
  (`lgipdb01.cbl:901-904`, `:983-986`); yeni sistem bütün kayıtları liste olarak döndürür, kayıt
  yoksa boş liste ile `200` döner. `FETCH` hatasında eski döngü bitmez (`:850`, `:933`); yeni sistem
  `500`/`"90"` döner. Ticari sorgular `brokerId`, `brokersReference` ve `payment`'ı okumaz; yeni sistem
  kayıttaki değerleri döndürür.

Eski kodun yalnızca yorumda başka bir niyet belirttiği ama tutarlı biçimde çalıştığı yerlerde eski
davranış korunur. Örnek: var olmayan poliçe silinirken eski kodun yorumu "bulunamadı başarılı
sayılsın" der, ama kod `90` döndürür (`lgdpdb01.cbl:196-199`); yeni sistem de `500`/`"90"` döndürür.
Eski kodun tutarlı biçimde uyguladığı ama hata olup olmadığı kodda belli olmayan kurallar da
korunur: poliçe güncellemede ödeme (`payment`) değişmez (`lgupdb01.cbl:319-325`); konut poliçesinde
boş tür, 0 oda ve 0 değer denetlenmeden kaydedilir (`lgapdb01.cbl:405-425`).
Bütün liste [API tasarımı](../api-design.md) 3. bölümdedir.

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
  kendiliğinden üretilemediği için eşdeğerlik ölçümünü etkilemez. Kural bütün operasyonlara
  uygulanır, çünkü kilitlenmenin anlamı operasyona göre değişmez.
- **Neden Karar 9'daki sapmalar:** Poliçe güncellemede eski kod kaydı güncellemediği halde
  çağırana "başarılı" der; bunu kopyalamak kullanıcının değişikliğinin sessizce kaybolması demektir
  ve SRS 3.2 ile çelişir ("Değiştirilmişse güncellemeyi yapmaz ve bunu bildirir"). Kodun kendisi
  `02`, `01` ve `90` yazdığı için doğru davranış kodda zaten tanımlıdır; ezilmesi bir programlama hatasıdır. Müşteri güncellemedeki çökme ise VSAM'a
  bağlıdır ve VSAM'ın olmadığı yeni sistemde üretilemez. Poliçe türü ve 10 haneli numarada eski kod
  adresteki kayıt yerine başka bir kayıt üzerinde işlem yapar (yanlış türden poliçeyi siler, kesilen
  numaradaki müşteriye poliçe ekler); bunu kopyalamak kullanıcıya veri kaybettirir. Ödeme ve konut alanlarında ise eski davranış bir veri kaybına yol açmaz ve bilinçli bir
  iş kuralı olabilir; emin olunamadığı için korunur. Silmedeki `90` ise kodda tutarlı biçimde
  üretilir ve kullanıcıya veri kaybettirmez; niyet yalnızca bir yorumda geçtiği için eşdeğerlik
  önceliklidir ve davranış korunur.

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
- Eski sistemle bilinçli olarak farklı olduğumuz noktalar: metin kırpma (Karar 6), kilitlenme ve
  zaman aşımı (Karar 8) ve Karar 9'daki durumlar. Tam liste API tasarımının 3. bölümündedir.
  Eşdeğerlik ölçümünde bu durumlar hata sayılmaz, raporda "bilinçli sapmalar" olarak yazılır.
- `80`, `81`, `82` kodları VSAM'a aittir; yeni sistemde VSAM için ayrı bir depolama olmadığı için
  (bkz. `docs/database-schema.md`, kesin karar ADR 0003'te) bu kodlar pratikte oluşmayacaktır.
