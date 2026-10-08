# tests

Planned test projects:

- `GenApp.EquivalenceTests` – tests derived from the COBOL logic that capture the legacy behaviour: return codes
  (00, 01, 02, 70, 80, 81, 82, 88, 89, 90, 98, 99), optimistic locking with LASTCHANGED, and input checks.
  Selected programs are also run in GnuCOBOL, with CICS and Db2 calls replaced by stubs, to check these tests.
- `GenApp.IntegrationTests` – API + PostgreSQL tests
- Mutation testing with Stryker.NET measures how many planted bugs the tests catch

## Aktif operasyonlar (CI)

`tests/active-operations.txt`, CI'da hangi operasyonların eşdeğerlik testlerinin çalışacağını belirler. Testler
koddan önce `main`'e girdiği için, kodu henüz yazılmamış bir operasyonun testi çalışırsa başarısız olur. Bu yüzden CI
yalnızca bu dosyada adı geçen operasyonların testlerini çalıştırır.

- Her satıra bir operasyon adı yazılır.
- Dosya boşsa eşdeğerlik testleri çalışmaz.
- Bir operasyonun kodu hazır olunca adı bu dosyaya eklenir. Dosyadaki her adın `Operasyon` etiketli en az bir testi
  olmalı; aksi halde CI başarısız olur.

Biçim kuralları:

- Boş satırlar ve `#` ile başlayan satırlar yok sayılır.
- Adlar küçük harf ve tireyle yazılır (örn. `musteri-sorgula`). Bu kurala uymayan bir satır CI'ı başarısız yapar.

Örnek dosya:

```
# Müşteri
musteri-sorgula
musteri-ekle
```

Geçerli 18 ad:

```
musteri-sorgula
musteri-ekle
musteri-guncelle
motor-ekle
motor-sorgula
motor-guncelle
motor-sil
konut-ekle
konut-sorgula
konut-guncelle
konut-sil
hayat-ekle
hayat-sorgula
hayat-guncelle
hayat-sil
ticari-ekle
ticari-sorgula
ticari-sil
```

Eşdeğerlik testleri xUnit v2 ile yazılır. CI testleri `Operasyon` etiketine göre seçer, bu yüzden her test
operasyonunun adını bu etiketle taşır:

```csharp
[Fact]
[Trait("Operasyon", "musteri-sorgula")]
public void Musteri_sorgula_var_olan_musteri_00_doner()
{
    // ...
}
```
