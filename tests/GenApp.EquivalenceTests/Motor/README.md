# Motor poliçesi eşdeğerlik testleri: hangi tahmin satırı neden çevrildi, neden çevrilmedi

Kaynak: `tests/predictions/04-motor-add.md`, `05-motor-inquire.md`, `06-motor-update.md`, `07-motor-delete.md`.
Kurallar: [ADR 0005](../../../docs/adr/0005-test-architecture.md) Karar 2. Yalnızca **iş kuralı** satırları çevrilir; satırın dönüş kodu kesin
olmalı (tahminde `belirsiz` olmamalı) ve ön koşulu API'den kurulabilmelidir. API'den çevrilmiş bir satırla aynı görünen satır ayrıca çevrilmez.
Arayüz (COMMAREA) kuralları C#'a çevrilmez; onlar yalnızca GnuCOBOL'da test edilir (ADR 0005 Karar 7).

Dört dosyada 40 iş kuralı satırı var (04: 8, 05: 9, 06: 17, 07: 6). **9'u çevrildi, 31'i çevrilmedi.**

## Çevrilen 9 satır

| Tahmin | Test | Dosya | Beklenen |
|---|---|---|---|
| 04-01 | `LGAPDB01_satir_293_police_eklenir_00_doner` | `MotorEkleTests.cs` | `201` + poliçe |
| 04-11 | `LGAPDB01_satir_296_musteri_yok_70_doner` | `MotorEkleTests.cs` | `404`, `customer-not-found`, `70` |
| 05-01 | `LGIPDB01_satir_258_police_var_00_doner` | `MotorSorgulaTests.cs` | `200` + poliçe |
| 05-02 | `LGIPDB01_satir_598_prim_ve_kaza_sifirdan_farkli_00_doner` | `MotorSorgulaTests.cs` | `200` + poliçe (prim ve kaza sıfırdan farklı) |
| 05-03 | `LGIPDB01_satir_612_police_yok_01_doner` | `MotorSorgulaTests.cs` | `404`, `not-found`, `01` |
| 05-04 | `LGIPDB01_satir_612_baska_musterinin_policesi_01_doner` | `MotorSorgulaTests.cs` | `404`, `not-found`, `01` |
| 06-01 | `LGUPDB01_satir_371_police_guncellenir_00_doner` | `MotorGuncelleTests.cs` | `200` + poliçe, `lastChanged` değişir |
| 06-02 | `LGUPDB01_satir_346_lastchanged_uyusmuyor_02_doner` | `MotorGuncelleTests.cs` | `409`, `concurrency-conflict`, `02` (**bilinçli sapma**, aşağıda) |
| 07-01 | `LGDPDB01_satir_138_police_silinir_00_doner` | `MotorSilTests.cs` | `204`; sonra `GET` `404`; müşteri yerinde |

**04-11 hakkında:** Tahminin girdisi, düzeneğin `-530` üretmesidir (B4). Bu testte B4'ün karşılığı "var olmayan müşteri"dir. `-530`'un yabancı anahtar ihlali olduğu COBOL'da yazmaz (04-12 bu yüzden belirsiz),
ama `POLICY` tablosunun müşteriye yabancı anahtarı vardır (`db2cre.jcl:174-176`) ve `docs/api-design.md` §3 `70`'i "poliçe eklenirken müşteri bulunamadı" diye tanımlar.

## Çevrilmeyen 31 satır

| Neden | Satırlar |
|---|---|
| **Ön koşul API'den kurulamıyor:** düzenek bir `SQLCODE` ya da `RESP` üretiyor (12) | 04-13, 04-14, 05-06, 06-06, 06-07, 06-08, 06-10, 06-11, 06-12, 06-13, 06-14, 07-04 |
| **VSAM'a özgü;** yeni sistemde VSAM yok (5) | 04-15, 06-05, 06-15, 06-16, 07-05 |
| **Dönüş kodu belirsiz** (4) | 04-12 (ayrıca 04-11 ile aynı girdi), 05-18 (ayrıca `NULL` sütun kurulamaz), 06-03, 06-04 (`ABEND LGV3`; çağıranın `81`'i görmesi belirsiz) |
| **API'den `NULL` kurulamıyor** (2) | 05-16, 05-17 (`brokerId`, `payment` `NULL`) |
| **İstekte alan yok:** `policyNumber` "yalnızca cevapta" (`api-design.md` §4.2) (1) | 04-17 |
| **Boş metin davranışı tanımsız** (1) | 04-18 (boş `make` ve `model`; ADR 0005 Açık sorular) |
| **Ön koşul başka dilimin operasyonuna bağlı** (2) | 05-05 (poliçe 4: hayat sigortası), 06-09 (aynı; ayrıca eski kodun sonucu `00`, `01` `CLOSE-PCURSOR`'da ezilir). O dilimin kodu hazır olmayabilir; test kırmızı olur |
| **Eski kodun davranışı kasıtsız görünüyor, karar bekliyor** (4) | 06-27, 07-02, 07-03, 07-06 (aşağıda) |

## Karar bekleyenler

Bunlar çevrilmedi ya da çevrilirken eski kodun davranışından ayrıldı; hangisinin doğru olduğu bu dosyada karara bağlanmadı.

1. **06-02, LASTCHANGED uyuşmazlığı (test yazıldı, bilinçli sapma).** Eski kod `02`'yi `lgupdb01.cbl:346`'da atar ama `CLOSE-PCURSOR` (`:371`) `00` ile ezer: çağıran `00` görür, Db2 güncellenmez, VSAM yine yazılır
   (tahmin 06-02, Kod gözlemleri 4-5). SRS (`docs/srs.md:124-126`), `api-design.md` §5 ve ADR 0002 Karar 7 `409` / `02` ister. Test **belgenin** istediğini ölçer; tahmini birebir çevirmek
   "eski zaman damgasıyla PUT `200` döner" beklentisini yazmak olurdu. Seçenekler: (a) sapmayı kabul et ve "bilinçli sapmalar" listesine ekle (ADR 0002 Sonuçlar), (b) eski davranışı taklit et, testi değiştir.
2. **07-02 ve 07-03, olmayan poliçeyi silmek.** Eski kodda yorum "SQLCODE 100 başarı sayılır" der (`lgdpdb01.cbl:196-197`) ama kod `IF SQLCODE NOT EQUAL 0` ile `100`'ü de `90` yapar (`:198-202`).
   Eşleme `90` → `500` `database-error` olurdu; olmayan bir poliçe için bu beklenen bir sonuç görünmüyor. `api-design.md` bu durumu tanımlamıyor. `404` `not-found` mu, `204` mü (silinmiş sayılır), `500` mü?
3. **06-27, `payment` güncellemede yazılmaz.** Eski program `CA-PAYMENT`'ı taşır ama `UPDATE POLICY` listesinde `PAYMENT` yoktur (`lgupdb01.cbl:314`, `:320-324`). `api-design.md` PUT örneğinde `payment` gönderilir. 06-01 bu kuralı sınamamak için
   `payment`'ı kurulandakiyle aynı (0) gönderir. Yeni sistem `payment`'ı güncellemeli mi?
4. **06-03, 06-04, olmayan poliçeye ya da başka müşterinin poliçesine `PUT`.** Eski kodda sonuç `ABEND LGV3` (belirsiz). `404` / `01` beklenir mi? (ADR 0005'teki 03-02 sorusunun aynısı.)
5. **07-06, istek kodu ile poliçe türü uyuşmazlığı.** Eski kod `01DEND` ile bir motor poliçesini siler (`DELETE` türe bakmaz, `lgdpdb01.cbl:189-194`), sonra VSAM `81` verir. Yeni sistemde istek kodu yok, adres türü belirler: motor poliçesi
   `.../endowment-policies/{n}` adresinden silinmeye çalışılırsa `404` doğal sonuç görünüyor.
6. **04-17, 04-18.** İstekte `policyNumber` gelirse yok mu sayılır, `400` mü? Bitiş tarihi başlangıçtan önceyse (eski kod doğrulamaz) kabul edilir mi? Boş `make`/`model` kabul edilir mi?
7. **Boş `brokersReference`.** `db2cre.jcl`'de boştur, testler boş olmayan değer gönderir. `api-design.md` boş metnin cevapta `""` mi `null` mı döneceğini tanımlamıyor.

## Doğrulanmayanlar (her testin yorumunda ayrıca yazılı)

VSAM `KSDSPOLY` kayıtları (yazma, yeniden yazma, silme), LGSTSQ'ya yazılan hata mesajları ve kuyruk kaydı, `COMMAREA` baytları (`FINAL` işareti dahil), `commission` sütunu, tarih ve zaman damgalarının Db2 biçimi,
yeni poliçe numarasının değeri (tahminde [B6] 1000001), `lastChanged`'ın değeri ve biçimi. Bunlar API'den gözlemlenemez ya da yeni sistemde karşılığı yoktur.

## Etkinleştirme

Testler `Operasyon` etiketlerini (`motor-ekle`, `motor-sorgula`, `motor-guncelle`, `motor-sil`) taşır. `tests/active-operations.txt` **değiştirilmedi**; operasyonun kodu hazır olunca adı oradan eklenir (`tests/README.md`).
Kurulum için `musteri-ekle` (ve silme testinde `musteri-sorgula`) etkin olmalıdır. Sıra: `musteri-ekle`, `musteri-sorgula`, `motor-ekle`, `motor-sorgula`, `motor-guncelle`, `motor-sil`.
