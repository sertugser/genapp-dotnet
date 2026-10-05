# GenApp COBOL Program Envanteri

## Özet

`base/src` klasöründe 31 adet `.cbl` programı var. Her biri dört katmandan
birine giriyor: **sunum**, **iş**, **veri erişimi**, **destek**. Bu dosya her
programı tek tek açıp başındaki açıklama yorumlarını ve kodunu okuyarak
hazırlandı; programlar arası çağrı ilişkileri kasıtlı olarak buraya
**yazılmadı** — o, çağrı grafiği görevinin (Issue #11) konusu.

| Katman | Program sayısı |
|---|---:|
| Sunum | 5 |
| İş | 7 |
| Veri Erişimi | 15 |
| Destek | 4 |
| **Toplam** | **31** |

Toplam, `base/src` altındaki gerçek `.cbl` dosya sayısıyla birebir eşleşiyor.

## Katmanlar nasıl ayırt edildi

- **Sunum** — 3270 ekranını (BMS map) süren, kullanıcıdan girdi alıp uygun iş
  katmanı programını çağıran programlar. İsimleri `lgtest*` ile başlıyor.
- **İş** — Tek bir iş kuralını yöneten, kendisi hiç Db2/VSAM'a dokunmadan
  veri katmanındaki programı `EXEC CICS LINK` ile çağıran programlar.
  İsimleri `*cus01`/`*pol01` ile bitiyor (ekleme/sorgulama/güncelleme/silme +
  müşteri/poliçe).
- **Veri Erişimi** — Doğrudan Db2'ye (`EXEC SQL`) veya VSAM KSDS dosyalarına
  (`EXEC CICS READ/WRITE/REWRITE/DELETE FILE`) erişen programlar. İsim kalıbı
  bu katmanda çok net: `*db01`/`*db02` → Db2, `*vs01` → VSAM.
- **Destek** — Normal iş akışının bir parçası olmayan, hata günlüğü /
  kurulum / istatistik gibi yan görevleri yürüten programlar.

## Sunum (5)

3270 menü ekranlarını süren programlar. Her biri bir CICS transaction'ına
karşılık gelir ve kullanıcının seçtiği işleme göre ilgili iş katmanı
programını çağırır.

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGTESTC1 | 347 | Müşteri menüsünü (ekle / sorgula / güncelle) ekrana basar ve kullanıcının seçimine göre `LGACUS01`, `LGICUS01` veya `LGUCUS01` iş programını çağırır. |
| LGTESTP1 | 318 | Motor (araç) poliçesi menüsünü ekrana basar. |
| LGTESTP2 | 300 | Endowment (birikimli hayat sigortası) poliçesi menüsünü ekrana basar. |
| LGTESTP3 | 299 | House (konut) poliçesi menüsünü ekrana basar. |
| LGTESTP4 | 318 | Commercial (ticari) poliçe menüsünü ekrana basar. |

## İş (7)

Sunum katmanından çağrılan, bir iş kuralını uçtan uca yöneten programlar.
Poliçeyle ilgili dört program (ekle/sorgula/güncelle/sil) tek bir veri
erişim programına, `CA-REQUEST-ID` alanındaki değere göre (Endowment /
House / Motor / Commercial) dağıtım yaparak LINK eder — yani dört poliçe
tipi için ayrı ayrı iş programı yok, aynı program hepsini yönetiyor.

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGACUS01 | 179 | Yeni müşteri ekleme işini yürütür: veri katmanındaki Db2 ve VSAM programlarını sırayla çağırıp sonucu sunuma döner. |
| LGICUS01 | 166 | Müşteri numarasına göre müşteri bilgisini sorgular; veri katmanından gelen sonucu sunuma döner. |
| LGUCUS01 | 172 | Var olan bir müşterinin bilgilerini günceller. |
| LGAPOL01 | 169 | Yeni poliçe ekleme işini yürütür; poliçe tipine göre `LGAPDB01`'e dağıtım yapar. |
| LGIPOL01 | 139 | Poliçe sorgulama işini yürütür; poliçe tipine göre `LGIPDB01`'e dağıtım yapar. |
| LGDPOL01 | 186 | Poliçe silme işini yürütür; poliçe tipine göre `LGDPDB01`'e dağıtım yapar. |
| LGUPOL01 | 201 | Poliçe güncelleme işini yürütür; poliçe tipine göre `LGUPDB01`'e dağıtım yapar. |

## Veri Erişimi (15)

Db2 veya VSAM'a doğrudan dokunan tek katman. Müşteri ve poliçe için ayrı
ayrı Db2 ve VSAM programı var; bu yüzden katmandaki program sayısı iş
katmanının iki katından fazla.

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGACDB01 | 328 | Yeni müşterinin adını, adresini ve doğum tarihini Db2 `CUSTOMER` tablosuna ekler. |
| LGACDB02 | 225 | Yeni müşterinin şifresini Db2 `CUSTOMER_SECURE` tablosuna ekler (varsayılan şifre, MD5 checksum olarak üretilir). |
| LGACVS01 | 121 | Yeni müşteri kaydını VSAM KSDS `KSDSCUST` dosyasına yazar. |
| LGICDB01 | 245 | Müşteri bilgilerini Db2 `CUSTOMER` tablosundan okur. |
| LGICVS01 | 230 | VSAM KSDS `KSDSCUST` dosyasından rastgele bir müşteri numarası seçip döner — gerçek bir sorgu değil, test/demo senaryoları için örnek müşteri üreten bir yardımcı program. |
| LGUCDB01 | 222 | Müşteri bilgilerini Db2 `CUSTOMER` tablosunda günceller. |
| LGUCVS01 | 136 | VSAM KSDS `KSDSCUST` kaydını günceller. |
| LGAPDB01 | 595 | Yeni poliçeyi hem Db2 `POLICY` tablosuna hem de poliçe tipine özgü tabloya (`ENDOWMENT`, `HOUSE`, `MOTOR` veya `COMMERCIAL`) ekler. |
| LGAPVS01 | 188 | Yeni poliçe kaydını VSAM KSDS `KSDSPOLY` dosyasına yazar. |
| LGIPDB01 | 1030 | Poliçe bilgilerini Db2 `POLICY` ve ilgili tip tablosundan okur; dört poliçe tipinin hepsini ve Commercial için imleç (cursor) tabanlı arama varyantlarını tek başına destekler. Envanterdeki en büyük program. |
| LGIPVS01 | 149 | VSAM KSDS `KSDSPOLY` dosyasından rastgele bir poliçe/müşteri çifti seçip döner (poliçe tipi parametre olarak verilir) — LGICVS01 ile aynı mantıkta bir test/demo yardımcı programı. |
| LGDPDB01 | 245 | İlgili satırı Db2 `POLICY` tablosundan siler; yabancı anahtar ilişkisi sayesinde silme, poliçe tipine özgü tabloya da otomatik yansır. |
| LGDPVS01 | 132 | VSAM KSDS `KSDSPOLY` kaydını siler. |
| LGUPDB01 | 535 | Poliçeyi günceller. İmleç tabanlı iyimser kilitleme kullanır: önce satırı `FOR UPDATE` ile kilitler, zaman damgasını karşılaştırır, uyuşuyorsa `POLICY` ve poliçe tipine özgü tabloyu günceller. |
| LGUPVS01 | 206 | VSAM KSDS `KSDSPOLY` kaydını günceller. |

## Destek (4)

İş akışının dışında kalan, çoğunlukla izleme/bakım amaçlı programlar.
`README.md`'de de belirtildiği gibi bunlar .NET'e taşınacak kapsamın
dışında tutuluyor (`LGSETUP`, `LGWEBST5`).

| Program | Satır | Ne yapıyor |
|---|---:|---|
| LGSTSQ | 126 | Hata/tanı mesajlarını bir CICS geçici depolama kuyruğuna (TSQ, varsayılan adı `GENAERRS`) yazar. İş akışındaki programlar, bir hatayla karşılaştıklarında bu programa LINK eder. |
| LGASTAT1 | 140 | CICS Event Processing tarafından tetiklenir; gelen isteklerin sayısını bir kontrol TSQ'sunda (`GENACNTL`) biriktirerek kullanım istatistiği tutar. |
| LGSETUP | 533 | Tek seferlik kurulum/bakım programı. Db2 veritabanı yedekten geri yüklendiğinde `GENACNTL` TSQ'sunu silip yeni düşük/yüksek müşteri numarası sınırlarını ve `GENACUSTNUM` sayacını baştan kurar. |
| LGWEBST5 | 802 | "Business Monitor" panosu için 60 saniyede bir çalışan tekrarlayan bir transaction; sayaçlardan değer toplayıp paylaşılan istatistik TSQ'suna yazar. |
