# GenApp COBOL Program Inventory

`base/src` altındaki 31 `.cbl` programının her birinin ne işe yaradığını ve hangi
katmanda olduğunu listeler. Her programın ilk satırlarındaki açıklama yorumları ve
kodu okunarak çıkarılmıştır. Hangi programın hangisini çağırdığı burada **yazılmadı**
— o, çağrı grafiği görevinde (#11) ele alınacak.

Katmanlar:
- **Sunum** — BMS ekranlarını (3270) süren, kullanıcıdan girdi alıp iş katmanını çağıran programlar.
- **İş** — Tek bir iş kuralını/komut (request-id) kümesini yöneten, veri katmanına LINK eden programlar.
- **Veri Erişimi** — Doğrudan Db2 (`EXEC SQL`) veya VSAM KSDS (`EXEC CICS READ/WRITE/REWRITE/DELETE FILE`) ile konuşan programlar.
- **Destek** — İş akışının parçası olmayan, hata günlüğü / kurulum / istatistik gibi yardımcı programlar.

## Sunum (5)

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGTESTC1 | 347 | Müşteri işlemleri (ekle/sorgula/güncelle) için 3270 menü ekranını sürer; kullanıcı girdisini alıp ilgili iş katmanı programını (`LGACUS01`/`LGICUS01`/`LGUCUS01`) çağırır. |
| LGTESTP1 | 318 | Motor (araç) poliçe işlemleri için 3270 menü ekranını sürer. |
| LGTESTP2 | 300 | Endowment (birikim) poliçe işlemleri için 3270 menü ekranını sürer. |
| LGTESTP3 | 299 | House (konut) poliçe işlemleri için 3270 menü ekranını sürer. |
| LGTESTP4 | 318 | Commercial (ticari) poliçe işlemleri için 3270 menü ekranını sürer. |

## İş (7)

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGACUS01 | 179 | Yeni müşteri ekleme iş mantığı; veri katmanındaki DB2 ve VSAM programlarını sırayla çağırır. |
| LGICUS01 | 166 | Müşteri sorgulama iş mantığı; müşteri numarasına göre DB2'den detayları getirir. |
| LGUCUS01 | 172 | Müşteri bilgisi güncelleme iş mantığı. |
| LGAPOL01 | 169 | Poliçe ekleme iş mantığı; `CA-REQUEST-ID`'ye göre (Endowment/House/Motor/Commercial) dağıtım yapıp tek bir DB2 veri-erişim programına (`LGAPDB01`) LINK eder. |
| LGIPOL01 | 139 | Poliçe sorgulama iş mantığı; poliçe tipine göre dağıtım yapıp `LGIPDB01`'e LINK eder. |
| LGDPOL01 | 186 | Poliçe silme iş mantığı; poliçe tipine göre dağıtım yapıp `LGDPDB01`'e LINK eder. |
| LGUPOL01 | 201 | Poliçe güncelleme iş mantığı; poliçe tipine göre dağıtım yapıp `LGUPDB01`'e LINK eder. |

## Veri Erişimi (15)

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGACDB01 | 328 | Yeni müşterinin ad/adres/doğum tarihi bilgilerini DB2 `CUSTOMER` tablosuna ekler (`INSERT`). |
| LGACDB02 | 225 | Yeni müşterinin şifresini DB2 `CUSTOMER_SECURE` tablosuna ekler (varsayılan şifre MD5 checksum). |
| LGACVS01 | 121 | Yeni müşteri kaydını VSAM KSDS `KSDSCUST` dosyasına yazar (`WRITE`). |
| LGICDB01 | 245 | Müşteri detaylarını DB2 `CUSTOMER` tablosundan okur (`SELECT`). |
| LGICVS01 | 230 | VSAM KSDS `KSDSCUST` dosyasından rastgele bir müşteri numarası döndürür (test/demo amaçlı anahtar üretici). |
| LGUCDB01 | 222 | Müşteri detaylarını DB2 `CUSTOMER` tablosunda günceller (`UPDATE`). |
| LGUCVS01 | 136 | VSAM KSDS `KSDSCUST` kaydını günceller (`REWRITE`). |
| LGAPDB01 | 595 | Yeni poliçeyi DB2 `POLICY` tablosuna ve poliçe tipine özgü tabloya (`ENDOWMENT`/`HOUSE`/`MOTOR`/`COMMERCIAL`) ekler. |
| LGAPVS01 | 188 | Yeni poliçe kaydını VSAM KSDS `KSDSPOLY` dosyasına yazar. |
| LGIPDB01 | 1030 | Poliçe detaylarını DB2 `POLICY` ve ilgili tip tablosundan okur; dört poliçe tipinin hepsini ve (Commercial için) imleç tabanlı arama varyantlarını destekler — dosyadaki en büyük program. |
| LGIPVS01 | 149 | VSAM KSDS `KSDSPOLY` dosyasından rastgele bir poliçe/müşteri numarası döndürür (test/demo amaçlı anahtar üretici); girdi parametresi poliçe tipine göre anahtarı belirler. |
| LGDPDB01 | 245 | İlgili satırı DB2 `POLICY` tablosundan siler; yabancı anahtar nedeniyle silme poliçe tipine özgü tabloya da yansır. |
| LGDPVS01 | 132 | VSAM KSDS `KSDSPOLY` kaydını siler (`DELETE`). |
| LGUPDB01 | 535 | Poliçeyi günceller; imleç tabanlı iyimser kilitleme (`DECLARE ... FOR UPDATE`, zaman damgası karşılaştırması) ile `POLICY` ve poliçe tipine özgü tabloyu günceller. |
| LGUPVS01 | 206 | VSAM KSDS `KSDSPOLY` kaydını günceller (`REWRITE`). |

## Destek (4)

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGSTSQ | 126 | Hata/tanı mesajlarını bir CICS geçici depolama kuyruğuna (TSQ, varsayılan `GENAERRS`) yazar; iş akışındaki programlar hata durumunda buna LINK eder. |
| LGASTAT1 | 140 | CICS Event Processing üzerinden tetiklenir; istek sayaçlarını bir kontrol TSQ'da (`GENACNTL`) biriktirerek kullanım istatistiği tutar. |
| LGSETUP | 533 | Tek seferlik kurulum/bakım programı: DB2 veritabanı geri yüklendiğinde `GENACNTL` TSQ'sunu silip yeni düşük/yüksek müşteri numarası sınırlarını ve `GENACUSTNUM` adlı sayacı yeniden kurar. |
| LGWEBST5 | 802 | "Business Monitor" için tekrarlayan (60 saniyede bir) transaction; sayaçlardan değer toplayıp paylaşılan istatistik TSQ'suna yazar. |

## Toplam kontrolü

5 sunum + 7 iş + 15 veri erişimi + 4 destek = **31** ✓ (`base/src` altındaki `.cbl` dosya sayısıyla eşleşiyor)
