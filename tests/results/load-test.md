# Yük testi: performans gereksinimlerinin ölçümü

| Alan | Değer |
|---|---|
| Tarih | 2026-10-27 |
| Sorumlu | Efekan |
| Dal | `efekan/load-test` (#84) |
| Soru | SRS'teki performans gereksinimleri (NFR-01, NFR-02, NFR-03) tutuyor mu? |
| Araç | k6 1.3.0 (`grafana/k6:1.3.0` Docker imajı) |
| Betik | `tests/load/genapp-load.js` |
| Ham sonuç | `tests/results/load-test-summary.json` (k6'nın yazdığı değerler; bu dosyadaki sayılar oradan alındı) |
| Ölçülen kod | `main` @ `8156a22` (18 operasyonun hepsi kodda) |

## Gereksinimler

`docs/srs.md` bölüm 4.1:

| No | Gereksinim |
|---|---|
| NFR-01 | 20 eşzamanlı kullanıcı altında sorgulama isteklerinin %95'i 500 ms'den kısa sürede cevaplanır. |
| NFR-02 | 20 eşzamanlı kullanıcı altında ekleme, güncelleme ve silme isteklerinin %95'i 1 saniyeden kısa sürede cevaplanır. |
| NFR-03 | Yük testi boyunca hiçbir istek sunucu hatasıyla sonuçlanmaz. |

## Sonuç

| Gereksinim | Ölçülen | Hedef | Sonuç |
|---|---|---|---|
| NFR-01 (sorgulama) | p95 = **3,35 ms** | p95 < 500 ms | **tuttu** |
| NFR-02 (ekleme, güncelleme, silme) | p95 = **5,85 ms** | p95 < 1000 ms | **tuttu** |
| NFR-03 (sunucu hatası) | 891.940 isteğin **0**'ı 5xx | 0 | **tuttu** |

Üç gereksinim de tuttu. Ölçülen p95 süreleri hedeflerin yaklaşık 150'de biri (okuma) ve 170'te biri (yazma). Bu fark büyük olsa da aşağıdaki
sınırlamalarla birlikte okunmalıdır: ağ gecikmesi yoktu ve veritabanında az veri vardı.

İsteklerin tamamı (891.940 / 891.940) operasyonun belgelenmiş durum kodunu döndü (`docs/api-design.md`; ekleme
201, sorgulama ve güncelleme 200, silme 204). Eşzamanlılık çakışması (409 / 02) hiç olmadı, çünkü her kullanıcı
yalnızca kendi poliçesini değiştirdi.

### Süreler (ms)

| İstek grubu | Ortalama | Ortanca | p90 | p95 | En yavaş |
|---|---|---|---|---|---|
| **Okuma** (NFR-01) | 1,58 | 1,34 | 2,66 | **3,35** | 49,16 |
| motor poliçesi sorgula | 1,70 | 1,45 | 2,80 | 3,51 | 46,43 |
| müşteri sorgula | 1,47 | 1,24 | 2,51 | 3,17 | 49,16 |
| **Yazma** (NFR-02) | 3,32 | 2,94 | 4,87 | **5,85** | 199,64 |
| motor poliçesi ekle | 3,43 | 3,04 | 5,04 | 6,08 | 199,51 |
| motor poliçesi güncelle | 3,45 | 3,05 | 5,04 | 6,06 | 195,12 |
| motor poliçesi sil | 3,07 | 2,74 | 4,51 | 5,37 | 199,64 |

Yazma istekleri okumanın yaklaşık iki katı sürüyor; ekleme ve güncellemede transaction açılıp commit ediliyor
(ADR 0003). En yavaş istekler (yaklaşık 200 ms) birkaç tane ve p95'i etkilemiyor. Hangi anda olduklarını k6 özeti
göstermiyor; testin başındaki ısınma (bağlantı havuzu, JIT) olması muhtemel ama ölçülmedi.

| Sayı | Değer |
|---|---|
| Ölçülen istek | 891.940 (k6 toplamı 891.961; farktaki 21 istek hazırlığa ait, aşağıda) |
| İstek hızı | saniyede yaklaşık 7.400 |
| Tamamlanan tur (5 istek) | 178.388 |

## Yöntem

- **Yük:** 20 sanal kullanıcı 2 dakika boyunca aynı anda çalıştı (k6 `constant-vus`). Her kullanıcı şu turu
  durmadan tekrarladı: motor poliçesi ekle → motor poliçesini sorgula → müşteriyi sorgula → motor poliçesini
  güncelle (sorgudan dönen `lastChanged` ile, `docs/api-design.md` bölüm 5) → motor poliçesini sil. Her tur kendi
  poliçesini sildiği için tablo büyümedi.
- **Hazırlık (ölçülmedi):** Test başlamadan önce API'nin açılması beklendi (1 istek) ve her kullanıcı için bir
  müşteri eklendi (20 istek). Bu istekler `kind:setup` etiketini aldı ve eşiklere girmedi.
- **Bekleme süresi yok:** İstekler arasında bekleme yoktu. Gerçek bir kullanıcı ekranda düşünür ve bekler, bu
  yüzden yük 20 gerçek kullanıcının oluşturacağından çok daha ağırdı.
- **Neden bu operasyonlar:** Motor poliçesi, poliçe operasyonlarının ortak akışını kullanır
  (`PolicyService<TDetails>`); konut, hayat ve ticari poliçe aynı akıştan geçer. Müşteri sorgulama ayrı bir dilimdir.
  Böylece hem okuma hem de üç yazma türü (ekleme, güncelleme, silme) ölçüldü.
- **Karar k6'da:** Gereksinimler betiğe eşik olarak yazıldı; geçti/kaldı kararını k6 verdi:

  | Eşik | Gereksinim |
  |---|---|
  | `http_req_duration{kind:read}`: `p(95)<500` | NFR-01 |
  | `http_req_duration{kind:write}`: `p(95)<1000` | NFR-02 |
  | `server_errors`: `rate==0` (5xx alan istek oranı) | NFR-03 |
  | `checks`: `rate==1` (her istek beklenen durum kodunu döndü) | testin geçerliliği |

  Dört eşik de `ok: true` (`load-test-summary.json`, `thresholds`).

## Ortam

| Alan | Değer |
|---|---|
| Makine | Apple M2, 8 çekirdek, 8 GB bellek (macOS) |
| Docker | Docker Desktop, Docker 29.1.3, Compose 5.0.0; Docker'a 8 çekirdek ve 4 GB bellek ayrılmış |
| API | `docker compose up` ile çalışan imaj (`Dockerfile`, `dotnet publish --configuration Release`), `ASPNETCORE_ENVIRONMENT=Development` |
| Veritabanı | PostgreSQL 18 (`postgres:18`), aynı Docker'da |
| Yük üreten | k6, aynı Docker'da; API'ye compose ağı üzerinden `http://api:8080` ile bağlandı |

API logunda yükle ilgisiz tek bir hata satırı var: `libgssapi_krb5.so.2: cannot open shared object file`. Bu satır
API açılırken Npgsql'in Kerberos kütüphanesini aramasından çıkıyor. Kerberos ile giriş kullanılmadığı için
etkisizdir.

## Sınırlamalar

1. **Aynı makine:** k6, API ve PostgreSQL aynı makinede, aynı Docker'da çalıştı. Ağ gecikmesi yok denecek kadar
   azdı. Ayrı sunucularda her isteğe ağ süresi eklenir. Öte yandan k6 da aynı işlemcileri kullandığı için API'ye
   kalan işlemci gücü azaldı.
2. **Az veri:** Test sonunda veritabanında 322 müşteri ve 184 poliçe vardı (önceki testlerden kalanlar ve bu
   testin 20 müşterisi). Çok sayıda kayıt olduğunda sorgu süreleri değişebilir. Ticari poliçenin posta koduyla sorgusu (17d) bütün eşleşen kayıtları döndürdüğü için veri
   büyüdükçe yavaşlayabilir; bu testte ölçülmedi.
3. **Çakışma ölçülmedi:** Her kullanıcı kendi poliçesiyle çalıştı. Aynı poliçeyi aynı anda güncelleyen
   kullanıcılar (NFR-05, 409 / 02) bu testin konusu değil; iyimser kilitleme eşdeğerlik testlerinde sınanıyor (örneğin
   `Motor/MotorGuncelleTests.cs`, 409 / 02).
4. **Tek ölçüm:** Test bir kez çalıştırıldı. Sonuçların farkı hedeflere göre çok büyük olduğu için tekrar
   ölçümlerin kararı değiştirmesi beklenmez, ama bu bir varsayımdır.

## Nasıl tekrarlanır

Docker dışında bir şey gerekmez:

    docker compose up -d --build
    docker compose --profile load run --rm k6

`k6` servisi `load` profilindedir; normal `docker compose up` onu başlatmaz. Betik bitince sonuçlar ekrana ve
`tests/results/load-test-summary.json` dosyasına yazılır. Bir eşik tutmazsa k6 hata koduyla çıkar.

k6 kuruluysa, `dotnet run` ile açılan API'ye karşı da çalıştırılabilir:

    k6 run -e BASE_URL=http://localhost:5236 tests/load/genapp-load.js
