# Hayat sigortası poliçesi eşdeğerlik testleri: hangi tahmin satırı neden çevrildi, neden çevrilmedi

Kaynak: `tests/predictions/12-endowment-add.md`, `13-endowment-inquire.md`, `14-endowment-update.md`, `15-endowment-delete.md`.
Kurallar: [ADR 0005](../../../docs/adr/0005-test-architecture.md) Karar 2. Yalnızca **iş kuralı** satırları çevrilir; satırın dönüş kodu kesin
olmalı (tahminde `belirsiz` olmamalı) ve ön koşulu API'den kurulabilmelidir. API'den çevrilmiş bir satırla aynı görünen satır ayrıca çevrilmez.
Arayüz (COMMAREA) kuralları C#'a çevrilmez; onlar yalnızca GnuCOBOL'da test edilir (ADR 0005 Karar 7).

Dört dosyada 45 iş kuralı satırı var (12: 9, 13: 11, 14: 19, 15: 6). **18'i çevrildi, 27'si çevrilmedi.**

## Çevrilen 18 satır

| Tahmin | Test | Dosya | Beklenen |
|---|---|---|---|
| 12-01 | `LGAPDB01_satir_293_hayat_police_eklenir_00_doner` | `HayatEkleTests.cs` | `201` + poliçe |
| 12-15 | `LGAPDB01_satir_296_hayat_musteri_yok_70_doner` | `HayatEkleTests.cs` | `404`, `customer-not-found`, `70` |
| 12-21 | `LGAPDB01_satir_311_hayat_girdideki_numara_yok_sayilir_00_doner` | `HayatEkleTests.cs` | `201`; yeni `policyNumber` gönderilenden farklı |
| 12-27 | `LGAPDB01_satir_364_hayat_yasam_sigortalisi_31_karakter_00_doner` | `HayatEkleTests.cs` | `201`; `lifeAssured` 31 karakterin hepsi |
| 13-01 | `LGIPDB01_satir_370_hayat_police_var_00_doner` | `HayatSorgulaTests.cs` | `200` + poliçe |
| 13-02 | `LGIPDB01_satir_370_hayat_ikinci_police_var_00_doner` | `HayatSorgulaTests.cs` | `200` + poliçe (ikinci veri kümesi) |
| 13-03 | `LGIPDB01_satir_423_hayat_police_yok_01_doner` | `HayatSorgulaTests.cs` | `404`, `not-found`, `01` |
| 13-04 | `LGIPDB01_satir_423_hayat_musteri_police_eslesmiyor_01_doner` | `HayatSorgulaTests.cs` | `404`, `not-found`, `01` |
| 13-18 | `LGIPDB01_satir_408_hayat_bayraklar_yer_degistirmez_00_doner` | `HayatSorgulaTests.cs` | `200`; `withProfits` `N`, `equities` `Y` |
| 14-01 | `LGUPDB01_satir_278_hayat_zaman_damgasi_esit_00_doner` | `HayatGuncelleTests.cs` | `200` + poliçe, `lastChanged` değişir |
| 14-02 | `LGUPDB01_satir_346_hayat_zaman_damgasi_eski_02_doner` | `HayatGuncelleTests.cs` | `409`, `concurrency-conflict`, `02` (**bilinçli sapma**) |
| 14-03 | `LGUPDB01_satir_352_hayat_police_yok_01_doner` | `HayatGuncelleTests.cs` | `404`, `not-found`, `01` (**bilinçli sapma**) |
| 14-04 | `LGUPDB01_satir_352_hayat_musteri_police_eslesmiyor_01_doner` | `HayatGuncelleTests.cs` | `404`, `not-found`, `01` (**bilinçli sapma**) |
| 14-27 | `LGUPDB01_satir_320_hayat_odeme_guncellenmez_00_doner` | `HayatGuncelleTests.cs` | `200`; `payment` değişmez (**korunan davranış**) |
| 14-29 | `LGUPDB01_satir_403_hayat_yasam_sigortalisi_31_karakter_00_doner` | `HayatGuncelleTests.cs` | `200`; `lifeAssured` 31 karakterin hepsi |
| 15-01 | `LGDPDB01_satir_138_hayat_police_silinir_00_doner` | `HayatSilTests.cs` | `204` |
| 15-02 | `LGDPDB01_satir_199_hayat_police_yok_90_doner` | `HayatSilTests.cs` | `500`, `database-error`, `90` (**korunan davranış**) |
| 15-03 | `LGDPDB01_satir_199_hayat_musteri_police_eslesmiyor_90_doner` | `HayatSilTests.cs` | `500`, `database-error`, `90`; poliçe yerinde |

**14-02, 14-03, 14-04:** Eski kodda `02` ve `01`, `CLOSE-PCURSOR` (`lgupdb01.cbl:371`) tarafından `00` ile ezilir; 14-03 ve 14-04'te ardından VSAM okuması başarısız olur
(`ABEND LGV3`). Testler eski davranışı değil, `docs/api-design.md` §3 "Bilinçli sapmalar" satırlarındaki yeni beklentiyi ölçer. Konut testleriyle (10-04, 10-05, 10-06) aynı yaklaşım.

**14-27:** Konut testlerinde `payment` güncellemede doğrulanmıyordu (belgede tanımsızdı). `api-design.md` §3 şimdi "Poliçe güncelle: `payment`" satırını içeriyor; test onu ölçer.

## Çevrilmeyen 27 satır

| Neden | Satırlar |
|---|---|
| **Ön koşul API'den kurulamıyor:** düzenek bir `SQLCODE` ya da `RESP` üretiyor (13) | 12-17, 12-18, 13-06, 14-06, 14-07, 14-08, 14-10, 14-11, 14-12, 14-13, 14-14, 15-04, 15-05 |
| **VSAM'a özgü;** yeni sistemde VSAM yok (4) | 12-19, 14-05, 14-15, 14-16 |
| **Dönüş kodu belirsiz** (1) | 12-16 (`-530`'un yabancı anahtar ihlali olduğu COBOL'da yazmaz; 12-15 ile aynı girdi) |
| **API'den `NULL` kurulamıyor** (3) | 13-16, 13-17 (`brokerId`, `payment` `NULL`), 13-23 (`fundName` `NULL`; ayrıca dönüş kodu belirsiz) |
| **API'de alan yok:** `paddingData` `api-design.md` §4.5'te yok (2) | 13-19, 14-28 |
| **Boş metin davranışı tanımsız** (1) | 12-22 (boş `fundName`; ADR 0005 Açık sorular) |
| **Ön koşul başka dilimin operasyonuna bağlı** (2) | 13-05, 14-09 (poliçe 1: motor poliçesi). O dilimin kodu hazır olmayabilir; test kırmızı olur |
| **Eski kodun davranışı kasıtsız görünüyor, karar bekliyor** (1) | 15-06 (aşağıda) |

## Karar bekleyenler

1. **13-05, 14-09, motor poliçesine hayat adresinden istek.** `api-design.md` §3'teki "Poliçe sorgula, güncelle, sil: poliçe başka türden" satırı bu durumu `404` / `01` olarak tanımlar. Test, motor dilimine bağlı olduğu için yazılmadı (motor testlerinin 05-05 ve 06-09 kararıyla aynı).
2. **15-06, istek kodu ile poliçe türü uyuşmazlığı.** Yeni sistemde istek kodu yok, adres türü belirler (motor README'sindeki 07-06 ile aynı soru).
3. **12-22, kural doğrulaması.** Eski kod bitiş tarihi başlangıçtan önceyse, bayrak `X` ise, vade ya da teminat 0 ise de kaydeder. API'de bu alanların doğrulanıp doğrulanmayacağı tanımlı değil; boş `fundName` da bu satırda var.

## Doğrulanmayanlar (her testin yorumunda ayrıca yazılı)

VSAM `KSDSPOLY` kayıtları (yazma, yeniden yazma, silme ve 30 baytlık yaşam sigortalısı kesilmesi), LGSTSQ'ya yazılan hata mesajları, `COMMAREA` baytları (`FINAL` işareti dahil), `paddingData`,
`policyType`, tarih ve zaman damgalarının Db2 biçimi, yeni poliçe numarasının değeri (tahminde [B6] 1000001), `lastChanged`'ın değeri ve biçimi, `brokersReference` (boş gönderilir).

## Etkinleştirme

Testler `Operasyon` etiketlerini (`hayat-ekle`, `hayat-sorgula`, `hayat-guncelle`, `hayat-sil`) taşır. `tests/active-operations.txt` **değiştirilmedi**; operasyonun kodu hazır olunca adı oradan eklenir (`tests/README.md`).
Kurulum için `musteri-ekle` etkin olmalıdır. Sıra: `musteri-ekle`, `hayat-ekle`, `hayat-sorgula`, `hayat-guncelle`, `hayat-sil`.
