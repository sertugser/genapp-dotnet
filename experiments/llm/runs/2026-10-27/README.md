# Çalıştırma kaydı: 2026-10-27 (kısmi)

| Alan | Değer |
|---|---|
| Model (istenen / dönen) | `claude-opus-5-5` / `claude-opus-5-5` (her `meta.json`'da) |
| Tarih | 2026-10-27 |
| Tasarım commit'i | `52284cd` (`oguz/llm-design`, #77). Çalıştırma, tasarım PR'ı `main`'e birleşmeden **önce** yapıldı; kurallar commit'te önceden yazılıydı |
| Betik | `experiments/llm/generate.py`; talimatlar her klasördeki `talimat.md` (sha256 `meta.json`'da) |
| Ayarlar | Araç yok, varsayılan örnekleme ayarları, tek atış, geri bildirim yok |

## Durum: 16 / 54 çağrı tamamlandı

API kredisi bittiği için kalan çağrılar "kredi bakiyesi yetersiz" hatası aldı. Bu hatalı çağrıların cevabı yoktur ve
bu klasörde yer almaz; yeniden denendiklerinde kayda eklenecektir.

| Operasyon | Tamamlanan tekrar |
|---|---|
| `musteri-ekle`, `musteri-sorgula`, `musteri-guncelle` | T1, T2, T3 |
| `motor-ekle`, `motor-sorgula` | T1, T2, T3 |
| `motor-guncelle` | T1 |
| `motor-sil`, `konut-*`, `hayat-*`, `ticari-*` (12 operasyon) | henüz çalıştırılmadı |

Çıktılar elle değiştirilmemiştir. Testlerle çalıştırma H09'dadır (#47).
