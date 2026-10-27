# Ticari poliçe eşdeğerlik testleri: hangi tahmin satırı neden çevrildi, neden çevrilmedi

Kaynak: `tests/predictions/16-commercial-add.md`, `17-commercial-inquire.md`, `18-commercial-delete.md`.
Kurallar: [ADR 0005](../../../docs/adr/0005-test-architecture.md) Karar 2. Yalnızca **iş kuralı** satırları çevrilir; satırın dönüş kodu kesin
olmalı (tahminde `belirsiz` olmamalı) ve ön koşulu API'den kurulabilmelidir. API'den çevrilmiş bir satırla aynı görünen satır ayrıca çevrilmez.
Arayüz (COMMAREA) kuralları C#'a çevrilmez; onlar yalnızca GnuCOBOL'da test edilir (ADR 0005 Karar 7). `docs/api-design.md` bir davranışı
bilinçli sapma ya da korunan davranış olarak tanımlıyorsa test API'nin tanımlı davranışını ölçer; eski sistemin davranışı testin yorumunda yazılır.
Belgede tanımsız olan hiçbir şey beklentiye girmez.

Üç dosyada 84 satır var: 40 iş kuralı (16: 7, 17: 26, 18: 7), 44 arayüz kuralı. **18'i çevrildi, 66'sı çevrilmedi.**
Ticari poliçede güncelleme yoktur (`api-design.md` §2.5); güncelleme testi yazılmadı.

## Çevrilen 18 satır

| Tahmin | Test | Dosya | Operasyon | Beklenen |
|---|---|---|---|---|
| 16-01 | `LGAPDB01_satir_293_ticari_police_eklenir_00_doner` | `TicariEkleTests.cs` | `ticari-ekle` | `201` + poliçe |
| 16-02 | `LGAPDB01_satir_532_isletme_adi_kesilmez_00_doner` | `TicariEkleTests.cs` | `ticari-ekle` | `201`; `customer` 39 karakterin hepsi |
| 16-03 | `LGAPDB01_satir_296_ticari_musteri_yok_70_doner` | `TicariEkleTests.cs` | `ticari-ekle` | `404`, `customer-not-found`, `70` |
| 17-04 | `LGIPDB01_satir_680_police_var_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200` + poliçe (17a) |
| 17-05 | `LGIPDB01_satir_714_musteri_police_eslesmiyor_01_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `404`, `not-found`, `01` |
| 17-07 | `LGIPDB01_satir_714_police_yok_01_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `404`, `not-found`, `01` |
| 17-08 | `LGIPDB01_satir_714_musteri_yok_01_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `404`, `not-found`, `01` (`70` değil) |
| 17-16 | `LGIPDB01_satir_703_broker_ve_odeme_sifirdan_farkli_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200`; `brokerId`, `brokersReference`, `payment` kayıttaki değerler (**bilinçli sapma**) |
| 17-17 | `LGIPDB01_satir_784_yalnizca_police_numarasiyla_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200`; `customerNumber` poliçenin sahibi (17b) |
| 17-23 | `LGIPDB01_satir_891_musteri_numarasiyla_tek_police_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200` + 1 elemanlı liste (17c) |
| 17-24 | `LGIPDB01_satir_850_musteri_numarasiyla_kayit_yok_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200` + `[]` (**korunan davranış**) |
| 17-25 | `LGIPDB01_satir_901_musteri_numarasiyla_iki_police_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200` + 2 elemanlı liste, sırasız (**bilinçli sapma**) |
| 17-33 | `LGIPDB01_satir_973_posta_koduyla_tek_police_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200` + 1 elemanlı liste (17d); `customerNumber` poliçenin sahibi |
| 17-34 | `LGIPDB01_satir_933_posta_koduyla_kayit_yok_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200` + `[]` (**korunan davranış**) |
| 17-35 | `LGIPDB01_satir_983_posta_koduyla_iki_police_00_doner` | `TicariSorgulaTests.cs` | `ticari-sorgula` | `200` + 2 elemanlı liste, sırasız (**bilinçli sapma**) |
| 18-08 | `LGDPDB01_satir_138_ticari_police_silinir_00_doner` | `TicariSilTests.cs` | `ticari-sil` | `204` |
| 18-10 | `LGDPDB01_satir_199_ticari_musteri_police_eslesmiyor_90_doner` | `TicariSilTests.cs` | `ticari-sil` | `500`, `database-error`, `90` (**korunan davranış**); poliçe yerinde |
| 18-11 | `LGDPDB01_satir_199_ticari_police_yok_90_doner` | `TicariSilTests.cs` | `ticari-sil` | `500`, `database-error`, `90` (**korunan davranış**) |

Adlardaki "ticari" eki yalnızca motor ya da konut testleriyle çakışan adlarda vardır: 16-01, 16-03 ve 18-08 motorla, 18-10 ve 18-11 konutla.

## Eski sistem ile yeni API farkları

| Satırlar | Eski sistem | Testin ölçtüğü (`api-design.md` §3) |
|---|---|---|
| 17-16 | Ticari sorgu `BROKERID`, `BROKERSREF` ve `PAYMENT`'ı okumaz; her zaman 0 / boşluk / 0 döner (`lgipdb01.cbl:245`, `:703`) | Kayıttaki değerler döner (sapma tablosu, `:142`): bilinçli sapma |
| 17-23, 17-25, 17-35 | Liste sorgusunda her `FETCH` aynı COMMAREA alanlarının üzerine yazar; çağırana yalnızca son kayıt kalır (`lgipdb01.cbl:901-904`, `:983-986`) | Bulunan kayıtların hepsi liste olarak döner (`:139`): bilinçli sapma. Sıra doğrulanmaz |
| 17-24, 17-34 | Kayıt yoksa `00` döner, veri gelmez (`lgipdb01.cbl:850`, `:933`) | `200` ve `[]` (`:140`): korunan davranış |
| 18-10, 18-11 | Silinecek satır yoksa `SQLCODE = 100` da hata sayılır, `90` döner; üstteki yorum (`lgdpdb01.cbl:196-197`) başarı sayılmasını söyler | `500`, `database-error`, `90` (`:102`, `:135`): korunan davranış |
| 16-03 | Müşteri yoksa yabancı anahtar ihlali, `70` (`lgapdb01.cbl:296`) | `404`, `customer-not-found`, `70` (`:96`): aynı |

Tarihlerin kaynağı ölçülmez: eski sorgu tarihleri `COMMERCIAL`'dan okur (17 Kod gözlemleri 4), ama POST ile kurulan poliçede eski sistem de iki
tabloya aynı tarihleri yazar (16 Kod gözlemleri 7). Testler bu yüzden `POLICY` tarihlerini gönderir (`TicariVerisi`).

## Çevrilmeyen 66 satır

| Neden | Satırlar |
|---|---|
| **Arayüz (COMMAREA) kuralı** (44) | 16-08 … 16-23 (16); 17-01, 17-02, 17-03, 17-10, 17-11, 17-12, 17-13, 17-14, 17-15, 17-21, 17-22, 17-31, 17-32, 17-40, 17-41, 17-42 (16); 18-01 … 18-07, 18-15 … 18-19 (12). Belirsiz satırlar (16-18, 16-21, 16-23, 17-41) bunların içindedir |
| **Ön koşul API'den kurulamıyor:** düzenek bir `SQLCODE` ya da `RESP` üretiyor ya da yol `ABEND` ile bitiyor (14) | 16-04, 16-05, 16-06, 16-07, 17-09, 17-20, 17-28, 17-29, 17-30, 17-37, 17-38, 17-39, 18-12, 18-13 |
| **Poliçe 7 kurulamıyor** (2): türü `C`, ama `HOUSE` satırı var, `COMMERCIAL` satırı yok (`db2cre.jcl:897-901`, `:1068`) | 17-06, 17-18 |
| **Ön koşul başka dilimin operasyonuna bağlı** (2): motor poliçesi gerekir (`motor-ekle`, `motor-sorgula`) | 17-19, 18-14 |
| **API'den çevrilmiş bir satırla aynı görünür** (3) | 17-27 (17-25 gibi), 17-36 (17-35 gibi), 18-09 (18-08 gibi) |
| **Belgede tanımsız** (1): yeni sistem için 21 satır sınırı | 17-26 |

18-13 hem poliçe 7 hem VSAM (`81`) yüzünden kurulamaz; yalnızca bir kez, `SQLCODE`/`RESP` satırında sayıldı.

## Doğrulanmayanlar (her testin yorumunda ayrıca yazılı)

VSAM `KSDSPOLY` kayıtları (yazma, silme, müşteri metninin 31 bayta kesilmesi), LGSTSQ'ya yazılan hata mesajları, `'FINAL'` işareti, `policyType`,
yeni poliçe numarasının değeri (tahminde [B6] 1000001), `lastChanged`'in değeri ve biçimi, `brokersReference` (boş gönderilir), tarihlerin kaynak
tablosu, liste elemanlarının sırası, `COMMERCIAL` satırının cascade ile silinmesi.

**Silmenin kalıcılığı hiçbir testte ölçülmez.** 18-08'de silmeden sonra GET yapılmaz (ticari-sil testini ticari-sorgula'ya bağlamamak için).
18-10'daki GET başka bir poliçenin yerinde kaldığını ölçer; 18-11 hiç kurulmamış bir numarayı siler. Konuttaki 11-07'nin (silinmiş poliçeyi
ikinci kez silme) ticari karşılığı tahminlerde yoktur.

## Etkinleştirme

Testler `Operasyon` etiketlerini (`ticari-ekle`, `ticari-sorgula`, `ticari-sil`) taşır. `tests/active-operations.txt` **değiştirilmedi**;
operasyonun kodu hazır olunca adı oradan eklenir (`tests/README.md`). Sıra: `musteri-ekle`, `ticari-ekle`, `ticari-sorgula`, `ticari-sil`.

- `musteri-ekle`'ye bağlı olanlar: 16-03 ve 17-34 dışındaki bütün testler. 16-03 ve 17-34 hiçbir kurulum yapmaz.
- `ticari-ekle`'ye bağlı olanlar (poliçe POST ile kurulur): 17-04, 17-05, 17-08, 17-16, 17-17, 17-23, 17-25, 17-33, 17-35, 18-08, 18-10.
  17-07, 17-24 ve 18-11 yalnızca müşteri kurar.
- `ticari-sorgula`'ya bağlı olan: 18-10 (silinmemesi gereken poliçeyi GET ile okur).

Testler yalnızca derlendi, **hiç çalıştırılmadı**: ticari operasyonların kodu henüz yok; etkinleştirilmeden çalıştırılırlarsa kırmızı olurlar.
