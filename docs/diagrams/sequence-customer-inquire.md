# Sıralama Diyagramı: Müşteri Sorgula

"Müşteri Sorgula" operasyonunun, kullanıcının 3270 ekranında müşteri numarasını
girmesinden sonuçların ekrana dönmesine kadar olan tam akışı. Kaynak:
`base/src/lgtestc1.cbl` (ekran, seçenek `'1'`), `lgicus01.cbl` (iş), `lgicdb01.cbl`
(veri erişimi, `SELECT ... FROM CUSTOMER`).

Veri taşıyıcısı tüm katmanlarda **aynı COMMAREA** (`DFHCOMMAREA`/`COMM-AREA`,
`LGCMAREA` kopya kitabı) — her LINK çağrısında aynı alan aktarılıp üzerine yazılıyor,
ayrı giriş/çıkış parametreleri yok.

## Normal akış (müşteri bulundu)

```mermaid
sequenceDiagram
    actor Kullanıcı
    participant LGTESTC1 as LGTESTC1<br/>(Sunum)
    participant LGICUS01 as LGICUS01<br/>(İş)
    participant LGICDB01 as LGICDB01<br/>(Veri Erişimi)
    participant Db2 as Db2<br/>CUSTOMER tablosu

    Kullanıcı->>LGTESTC1: Müşteri numarası girer, Enter (seçenek '1')
    LGTESTC1->>LGTESTC1: CA-REQUEST-ID = '01ICUS'<br/>CA-CUSTOMER-NUM = girilen numara
    LGTESTC1->>LGICUS01: EXEC CICS LINK<br/>COMMAREA(COMM-AREA)

    LGICUS01->>LGICUS01: Commarea uzunluğunu kontrol eder
    LGICUS01->>LGICDB01: EXEC CICS LINK<br/>COMMAREA(DFHCOMMAREA) - aynı alanı aktarır

    LGICDB01->>Db2: SELECT FIRSTNAME, LASTNAME, DATEOFBIRTH,<br/>HOUSENAME, HOUSENUMBER, POSTCODE,<br/>PHONEMOBILE, PHONEHOME, EMAILADDRESS<br/>FROM CUSTOMER<br/>WHERE CUSTOMERNUMBER = :DB2-CUSTOMERNUMBER-INT
    Db2-->>LGICDB01: SQLCODE = 0, satır verisi

    LGICDB01->>LGICDB01: CA-FIRST-NAME, CA-LAST-NAME, ... host<br/>değişkenlerinden COMMAREA'ya yazılır<br/>CA-RETURN-CODE = '00'
    LGICDB01-->>LGICUS01: RETURN (COMMAREA güncellenmiş halde)
    LGICUS01-->>LGTESTC1: RETURN (COMMAREA değişmeden geçer)

    LGTESTC1->>LGTESTC1: CA-RETURN-CODE = '00' → CA-FIRST-NAME,<br/>CA-LAST-NAME, CA-DOB, ... ekran alanlarına taşınır
    LGTESTC1-->>Kullanıcı: Müşteri bilgileri ekranda gösterilir
```

## Alternatif akışlar

**Müşteri bulunamadı** (`SQLCODE = 100` veya `-913`):

```mermaid
sequenceDiagram
    participant LGICDB01 as LGICDB01
    participant Db2 as Db2

    LGICDB01->>Db2: SELECT ... WHERE CUSTOMERNUMBER = :DB2-CUSTOMERNUMBER-INT
    Db2-->>LGICDB01: SQLCODE = 100 (satır yok)
    LGICDB01->>LGICDB01: CA-RETURN-CODE = '01'
    Note over LGICDB01: LGICUS01 ve LGTESTC1'e aynen geri döner;<br/>LGTESTC1, CA-RETURN-CODE > 0 görünce<br/>NO-DATA etiketine atlar, hata mesajı gösterir
```

**Beklenmeyen Db2 hatası** (diğer her SQLCODE):

```mermaid
sequenceDiagram
    participant LGICDB01 as LGICDB01
    participant Db2 as Db2
    participant LGSTSQ as LGSTSQ<br/>(Destek)

    LGICDB01->>Db2: SELECT ...
    Db2-->>LGICDB01: SQLCODE = beklenmeyen bir değer
    LGICDB01->>LGICDB01: CA-RETURN-CODE = '90'
    LGICDB01->>LGSTSQ: EXEC CICS LINK<br/>(hata mesajı + SQLCODE, GENAERRS TSQ'suna yazılsın diye)
    LGSTSQ-->>LGICDB01: RETURN
    Note over LGICDB01: EXEC CICS RETURN - çağıran zincire '90' ile döner
```

## Dönen değerin anlamı (`CA-RETURN-CODE`)

| Değer | Anlamı |
|---|---|
| `'00'` | Başarılı, müşteri bulundu, bilgiler COMMAREA'da |
| `'01'` | Müşteri numarası bulunamadı |
| `'90'` | Beklenmeyen Db2 hatası (`LGSTSQ` ile loglanır) |
| `'98'` | Gelen COMMAREA, gereken minimum uzunluktan küçük |
