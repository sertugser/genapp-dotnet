# Tahmin ile çalıştırma arasındaki farklar

| Alan | Değer |
|---|---|
| Tarih | 2026-10-13 |
| Sorumlu | Oğuz |
| Dal | `oguz/accuracy-1` |
| Kapsam | `02-customer-inquire.md` (`runs/lgicus01.md`) ve `01-customer-add.md` (`runs/lgacus01.md`) |
| Sayılar | `tests/results/prediction-accuracy.md` |

## Sınıflar

Tutmayan her satır şu sınıflardan birine konur:

| Sınıf | Anlamı |
|---|---|
| **Kod hatası** | Program, kendi yorumuna ya da tasarım amacına aykırı davranıyor (eski sistemin hatası). Tahmin kodu doğru okudu; farkın nedeni kodun kendisi. |
| **Test (tahmin) hatası** | Tahmin kodu yanlış okudu. Program kodunun dediğini yapıyor. |
| **İki ekibin aynı yanlış okuması** | Tahmin ve yapım ekibinin analizi aynı yanlış davranışı yazdı; çalıştırma ikisinden de farklı çıktı. |
| **Belirsiz** | Neden belirlenemedi (örn. düzenek farkı olabilir). |

## Tutmayan satırlar

**Yok.** İki ölçümde de puanlanan 35 satırın 35'i tuttu, tutmayan satır olmadığı için sınıflandırılacak bir şey çıkmadı:

| Dosya | tutmadı | Kod hatası | Test hatası | İki ekibin aynı yanlış okuması | Belirsiz |
|---|---|---|---|---|---|
| `02-customer-inquire.md` | 0 | 0 | 0 | 0 | 0 |
| `01-customer-add.md` | 0 | 0 | 0 | 0 | 0 |

Bug Tracker'a bu yüzden "bug" etiketli kayıt açılmadı. "İki ekibin aynı yanlış okuması" sınıfı için yapım ekibinin analizi (`legacy-analysis/`) ayrıca okunmadı; karşılaştıracak bir fark olmadığından gerekmedi.

## Puanlanmayan satırlar

Bunlar tutmayan satır değildir: tahminin kendisi o hücrelerde `belirsiz` dedi ve bir iddiada bulunmadı. Sınıfları: **belirsiz**.

| Test no | Neden belirsiz (tahminin sözü) | Çalıştırmada görülen | Not |
|---|---|---|---|
| 02-14 | Sayısal olmayan müşteri numarası: sonuç tanımsız | `01`, alanlara dokunulmadı (GnuCOBOL boşluğu `0` olarak taşıdı) | Başka derleyicide aynı olacağı söylenemez |
| 02-15 | `NULL` sütun: Db2'nin vereceği `SQLCODE` kodda yok | çalıştırılmadı | Düzenek `NULL` üretemez |
| 02-16 | `LINK` hatası: sonuç CICS'in varsayılanına bağlı | çalıştırılmadı | `CALL` hatası `LINK` hatası değildir |
| 01-09 | `CA-DOB` boşluğu: `DATE` sütununa yazılıp yazılamayacağı Db2'ye bağlı | `00`, INSERT'e 10 boşluk gitti | Stub tarih denetlemez; Db2 hakkında bilgi vermez |
| 01-15 | `LINK` hatası (üç LINK): sonuç CICS'in varsayılanına bağlı | çalıştırılmadı | `CALL` hatası `LINK` hatası değildir |
| 01-23 | `EIBCALEN` 1-54 ile LGACDB02: COMMAREA dışı okuma | çalıştırılmadı | Düzenekte tampon her zaman 32500 bayt; çıkacak sonuç anlamsız olurdu |
| 01-24 | COMMAREA'sız LGACVS01: sonuç kodda tanımlı değil | çalıştırılmadı | Aynı neden |

## Puanlanamayan iddialar

Farkların saklanabileceği yerler burasıdır: tuttu satırlarının içinde yazılı, ama düzenek göstermediği için ne tuttu ne tutmadı denebildi. Ayrıntıları ilgili kayıttadır.

| Alan | İddianın örneği | Satırlar | Kapanması için gereken |
|---|---|---|---|
| LGSTSQ'nun kuyruk kaydı | Kayıt uzunluğu 104; `WRITE-MSG` 95 bayt; ikinci mesajdan ilk 81 bayt | 02-04, 02-17; 01-03 ... 01-06, 01-10, 01-16, 01-18, 01-20, 01-22, 01-25 | Gerçek LGSTSQ ve CICS kuyrukları |
| Tarih ve saat | Mesajın ilk 15 baytı | mesaj içeren bütün satırlar | Gerçek `ASKTIME`/`FORMATTIME` |
| Gerçek Db2 | Satırın oluşması, kimlik değeri, birincil anahtar ihlalinde `-803`, `DATE` biçimi, `NULL`, `-913` | 02-01 ... 02-04, 02-13; 01-01 ... 01-04, 01-06, 01-22 | Gerçek Db2 |
| Gerçek VSAM | `RESP` değerleri, kaydın gerçekten yazılması, `DUPREC` | 01-01, 01-05, 01-25 | Gerçek VSAM |
| Geri alma | 01-05: "önceden eklenen `CUSTOMER` satırı geri alınır" (`[belge]` `base/Testing.md:29-30`; kodda `SYNCPOINT`/`ROLLBACK` yok) | 01-05 | Gerçek CICS, iki aşamalı onay |
| ABEND davranışı | Görevin sonlanması; çağıranın `80`'i görüp görmemesi | 01-05, 01-25 | Gerçek CICS |
| `< 91` ile `ELSE` dalı | İki dal aynı metni üretiyor | 02-17, 01-18, 01-22, 01-25 | Hangi dalın çalıştığını gösteren bir iz (örn. kapsam aracı) |
| Ölü kod | `lgicus01.cbl:151-164`, `lgacus01.cbl:164-178` içindeki COMMAREA dökümü | 02-05, 01-10 | Yok: bu kod hiçbir girdiyle çalışmaz (derlendi, çalışmadı) |
| Süreç içinde tekrar çağrı | `WS-REQUIRED-CA-LEN` birikmesi (180, 270), `LGAC-NCS = 'NO'` kalıcılığı, ABEND sonrası `EM-VARIABLE` uç durumu | `01-customer-add.md` Kod gözlemleri 16; `02-customer-inquire.md` Kod gözlemleri 11 | Düzenekte `CANCEL` olmadan ardışık çağrı |

## Yöntemin göremeyeceği hata türü

"İki ekibin aynı yanlış okuması" ve "iki ekibin gerçek sistemden farklı ortak varsayımı" bu yöntemle **görülemez**. Neden: tahminin ön koşulları düzenekte, yazıldıkları gibi **kurulur**.
Tahmin ile düzenek aynı şeyi varsayarsa, "tuttu" gerçek CICS ve Db2 hakkında bir şey söylemez. Bu dosyalarda bu türden ortak varsayımlar:

| Varsayım | Tahminde | Düzenekte | Gerçek sistemde farklı olabilir mi |
|---|---|---|---|
| B1 | COMMAREA her `LINK`'te aynı 32500 baytlık tamponla geçer; `EIBCALEN` ötesi boşluk | Sürücü tek tampon kurar, ötesini boşluk yapar | Evet olabilir: `LINK` ile COMMAREA'nın aktarılma biçimi düzenekte modellenmedi ve gerçek CICS'inkiyle birebir aynı olmayabilir (ADR 0004, "Neyi ölçemiyoruz") |
| B3 | Sayısal `MOVE`'da hane sayısına göre kesme | GnuCOBOL'un varsayılanı | Evet, derleyici seçeneğine bağlı (`TRUNC`); 02-13'ün sonucu buna bağlı |
| B4 | Hata kodunu (`SQLCODE`/`RESP`) düzenek kurar | Sürücü seçer (`-204`, `-803`, `RESP` 14 ve 2) | Gerçek Db2 ve VSAM hangi durumda hangi kodu verir, ölçülmedi |
| B6, B7 | İlk kimlik değeri 1000001; sayaç 11 verir | Stub'lar bu değerleri döndürür | Gerçek kurulumda farklı olabilir |
| `[belge]` iddiaları | "VSAM hatasında Db2 geri alınır" | Ölçülmedi | Belge ile kod uyuşmayabilir |

Bu açığı kapatmanın yolları: (1) gerçek CICS ve Db2'de çalıştırmak (erişim yok), (2) ortak varsayımları bağımsız bir üçüncü kaynakla (IBM belgeleri, derleyici kılavuzu) sınamak,
(3) yapım ekibinin analizini tahminlerle satır satır karşılaştırıp ortak ama belgesiz iddiaları işaretlemek (tahminler `main`'e girdiği için artık yapılabilir).

## Bu çalıştırmalarda bulunan düzenek sorunları

Bunlar tahminle çalıştırma arasındaki fark değildir; düzeneği kurarken bulunup çalıştırmadan önce düzeltildi (`runs/lgacus01.md`, "Düzenek düzeltmeleri"; LGICUS01 için `runs/lgicus01.md`).
Hiçbiri bir tahmini ya da programın mantığını değiştirmedi.
