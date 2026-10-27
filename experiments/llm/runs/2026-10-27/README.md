# Çalıştırma kaydı: 2026-10-27 (kısmi)

| Alan | Değer |
|---|---|
| Model (istenen / dönen) | `claude-opus-5-5` / `claude-opus-5-5` (her `meta.json`'da) |
| Tarih | 2026-10-27 |
| Tasarım commit'i | `52284cd` (`oguz/llm-design`, #77). Çalıştırma, tasarım PR'ı `main`'e birleşmeden **önce** yapıldı; kurallar commit'te önceden yazılıydı |
| Betik | `experiments/llm/generate.py`; talimatlar her klasördeki `talimat.md` (sha256 `meta.json`'da) |
| Ayarlar | Araç yok, varsayılan örnekleme ayarları, tek atış, geri bildirim yok |

## Durum: 16 / 54 çağrı yapıldı, çalıştırma burada tamamlandı

Çalıştırma 16 çağrıdan sonra durduruldu; kalan 38 çağrı yapılmadı ve yapılmayacak. Ölçüm kapsamı, üç tekrarı da
tamamlanmış beş operasyondur (aşağıdaki tablonun ilk iki satırı). `motor-guncelle`'nin tek tekrarı (T1) kayıtta kalır,
ek bilgi olarak raporlanır ama ölçüme sayılmaz. Karar, LLM çıktıları testlerle hiç çalıştırılmadan alındı (bkz.
`../../README.md`, Değişiklik kaydı).

| Operasyon | Tamamlanan tekrar |
|---|---|
| `musteri-ekle`, `musteri-sorgula`, `musteri-guncelle` | T1, T2, T3 |
| `motor-ekle`, `motor-sorgula` | T1, T2, T3 |
| `motor-guncelle` | T1 |
| `motor-sil`, `konut-*`, `hayat-*`, `ticari-*` (12 operasyon) | çalıştırılmadı (kapsam dışı) |

Çıktılar elle değiştirilmemiştir. Testlerle çalıştırma H09'dadır (#47).
