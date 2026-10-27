# GenApp.SampleData

GenApp'in Db2 örnek verilerini (`base/cntl/db2cre.jcl`'deki `INSERT` deyimleri) yeni sistemin PostgreSQL veritabanına
yükleyen araç (#51).

- **Yüklenen tablolar:** `customer`, `policy`, `motor`, `house`, `endowment`, `commercial`. Müşteri ve poliçe
  numaraları aynen korunur (1–10); sistemin verdiği numaralar 1000001'den başladığı için çakışmaz.
- **Yüklenmeyenler:** `customer_secure`'un örnek satırları #51'in kapsamında değildir. `base/data` altındaki VSAM
  dosyaları kullanılmaz; kaynak Db2 örnek verisidir (ADR 0003, karar 1).
- **Tekrar çalıştırma:** Numarası veritabanında olan satır atlanır; araç kayıtları çoğaltmaz ve var olanları
  değiştirmez.
- **Sayı kontrolü:** Yüklemeden sonra her tablodaki örnek satır sayısını `db2cre.jcl`'dekiyle karşılaştırır ve bir
  tablo yazdırır. Sayılardan biri tutmazsa çıkış kodu 1'dir.

Tablo ve kolon adları API'nin kullandığı EF Core modelinden (`GenAppDbContext`) alınır; Db2 kolon adları C#
property adlarıyla büyük/küçük harf dışında aynıdır (`phonehome` → `PhoneHome` → `phone_home`).

## Çalıştırma

`docker compose up` bu aracı migration'lardan sonra ve API'den önce kendiliğinden çalıştırır (`seed` servisi).

Elle çalıştırmak için şema kurulmuş olmalı:

    docker compose up -d postgres
    dotnet ef database update --project src/GenApp.Infrastructure --startup-project src/GenApp.Api
    ConnectionStrings__GenApp="Host=localhost;Port=5432;Database=genapp;Username=genapp;Password=genapp_dev" \
        dotnet run --project tools/GenApp.SampleData

`db2cre.jcl` çalışılan klasörde ya da üst klasörlerinde `base/cntl/` altında aranır; başka bir yerdeyse
`-- --jcl <yol>` ile verilir.
