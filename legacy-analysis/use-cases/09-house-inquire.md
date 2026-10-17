# UC-09 · House Policy Inquire

Customer number'ı ve policy number'ı verilen bir house (konut) policy'sinin bilgilerini Db2'deki
`POLICY` ve `HOUSE` table'larını birleştirerek okuyan operasyon. Hiçbir yere yazmaz; VSAM
dosyasını da okumaz. Dört policy tipi aynı programları kullandığı için bu belge yalnızca
house'a ait kısımları ve ortak kısımları anlatır; ortak kısımlar
[05-motor-inquire.md](05-motor-inquire.md) ile aynıdır. Bu belge, C# kodunu yazacak kişi için
eski sistemin davranışını satır satır tarif eder. Satır numaraları `base/src` altındaki
dosyalara aittir ve linkler ilgili satırı açar.

## Özet

| | |
|---|---|
| **Request ID** (`CA-REQUEST-ID`) | `01IHOU`. `LGIPOL01` kontrol etmez; `LGIPDB01` uppercase'e çevirip kontrol eder ve tanımazsa `99` döner |
| **Entry program** | `LGIPOL01` (business layer) |
| **Call chain** | `LGIPOL01` → `LGIPDB01` |
| **Okunduğu yer** | Db2 `POLICY` ve `HOUSE` table'larının join'i (write yok) |
| **Output** | Ortak policy field'ları ve 6 house field'ı COMMAREA'da, `CA-RETURN-CODE = '00'` |
| **Olası return code'lar** | `00`, `01`, `90`, `98`, `99` (ve ABEND `LGCA`) |

## Flow

```mermaid
%%{init: {"theme": "base", "look": "classic", "themeVariables": {"background": "#ffffff", "primaryColor": "#ffffff", "primaryTextColor": "#1f2328", "primaryBorderColor": "#8c959f", "lineColor": "#57606a", "textColor": "#1f2328", "titleColor": "#1f2328", "clusterBkg": "#ffffff", "clusterBorder": "#d0d7de", "edgeLabelBackground": "#ffffff", "fontSize": "15px"}}}%%
flowchart TD
    subgraph ZEMIN["HOUSE POLICY INQUIRE — LGIPOL01 → LGIPDB01"]
        direction TD
        S(["Request gelir<br/>01IHOU + customer number<br/>+ policy number"])
        Q1{"COMMAREA<br/>boş mu?"}
        Q2{"request ID<br/>uppercase'te<br/>01IHOU mu?"}
        Q3["SELECT POLICY + HOUSE<br/>WHERE customer number<br/>AND policy number"]
        Q4{"SQLCODE?"}
        Q5{"length<br/>163 byte'tan az mı?"}
        M["Field'lar COMMAREA'ya yazılır<br/>sonuna 'FINAL' işareti konur"]
        OK(["RC 00"])
        A1(["ABEND LGCA"])
        R99(["RC 99"])
        R01(["RC 01<br/>not found"])
        R90(["RC 90<br/>LGSTSQ ile log'lanır"])
        R98(["RC 98"])

        S --> Q1
        Q1 -->|evet| A1
        Q1 -->|hayır| Q2
        Q2 -->|evet| Q3 --> Q4
        Q2 -->|hayır| R99
        Q4 -->|100| R01
        Q4 -->|0| Q5
        Q4 -->|diğer| R90
        Q5 -->|evet| R98
        Q5 -->|hayır| M --> OK
    end

    classDef adim fill:#ffffff,stroke:#8c959f,color:#1f2328,stroke-width:1.5px
    classDef karar fill:#fff8c5,stroke:#d4a72c,color:#1f2328,stroke-width:1.5px
    classDef basari fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px
    classDef hata fill:#fee2e2,stroke:#dc2626,color:#7f1d1d,stroke-width:2px
    classDef giris fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px
    class S giris
    class Q3,M adim
    class Q1,Q2,Q4,Q5 karar
    class OK basari
    class A1,R99,R01,R90,R98 hata
    style ZEMIN fill:#ffffff,stroke:#d0d7de,color:#1f2328
```

## Input (COMMAREA)

Screen (`LGTESTP3`) bu operasyonu iki yerden call eder: menu 1 (sorgula) ve menu 4'ün ilk
adımı (güncellemeden önce mevcut bilgileri getirmek). İkisinde de yalnızca üç field
doldurulur ([lgtestp3.cbl:66-73](../../base/src/lgtestp3.cbl#L66-L73),
[155-162](../../base/src/lgtestp3.cbl#L155-L162)).

| Field | Type | Gelen value | Kullanım |
|---|---|---|---|
| `CA-REQUEST-ID` | `X(6)` | `'01IHOU'` | `LGIPDB01`'de house dalını seçer |
| `CA-CUSTOMER-NUM` | `9(10)` | screen'den | `WHERE POLICY.CUSTOMERNUMBER = ...` |
| `CA-POLICY-NUM` | `9(10)` | screen'den | `WHERE POLICY.POLICYNUMBER = ...` |

Diğer bütün field'lar input olarak kullanılmaz; output için doldurulur.

## Adım adım

| # | Program | Ne yapıyor | Kaynak |
|---:|---|---|---|
| 1 | `LGIPOL01` | COMMAREA hiç gelmediyse (`EIBCALEN = 0`) error log'a yazar ve `LGCA` koduyla ABEND eder. | [lgipol01.cbl:79-83](../../base/src/lgipol01.cbl#L79-L83) |
| 2 | `LGIPOL01` | `CA-RETURN-CODE = '00'` yapar. Length'e de request ID'ye de bakmaz. | [lgipol01.cbl:86](../../base/src/lgipol01.cbl#L86) |
| 3 | `LGIPOL01` | `LGIPDB01`'i aynı COMMAREA ile `LINK` eder, dönünce hiçbir şeye bakmadan return eder. | [lgipol01.cbl:91-96](../../base/src/lgipol01.cbl#L91-L96) |
| 4 | `LGIPDB01` | 1-2. adımları tekrarlar (ABEND `LGCA`, `'00'`). | [lgipdb01.cbl:251-258](../../base/src/lgipdb01.cbl#L251-L258) |
| 5 | `LGIPDB01` | Customer ve policy number'larını Db2 integer'ına çevirir. | [lgipdb01.cbl:263-264](../../base/src/lgipdb01.cbl#L263-L264) |
| 6 | `LGIPDB01` | Request ID'nin uppercase halini bir değişkene alır ve ona göre dal seçer: `'01IHOU'` house dalına gider, tanınmayan bir ID `'99'` olur. | [lgipdb01.cbl:275-310](../../base/src/lgipdb01.cbl#L275-L310) |
| 7 | `LGIPDB01` | `POLICY` ile `HOUSE`'u `POLICYNUMBER` üzerinden join eder ve customer number + policy number ile tek row okur: 6 ortak policy column'u ve 6 house column'u. | [lgipdb01.cbl:441-476](../../base/src/lgipdb01.cbl#L441-L476) |
| 8 | `LGIPDB01` | Row bulunduysa gereken length'i hesaplar: 33 + 130 = **163 byte**. COMMAREA daha kısaysa `'98'` ile return eder. | [lgipdb01.cbl:478-489](../../base/src/lgipdb01.cbl#L478-L489) |
| 9 | `LGIPDB01` | Integer column'ları COMMAREA'nın numeric field'larına çevirir. `NULL` gelen broker ID ve payment'ı atlar. Ortak policy field'larını ve house field'larını COMMAREA'ya yazar. | [lgipdb01.cbl:494-504](../../base/src/lgipdb01.cbl#L494-L504) |
| 10 | `LGIPDB01` | Policy data'sının bittiği yeri işaretlemek için `CA-H-FILLER`'ın ilk 5 byte'ına `'FINAL'` yazar. | [lgipdb01.cbl:508](../../base/src/lgipdb01.cbl#L508) |
| 11 | `LGIPDB01` | Row bulunamadıysa (`SQLCODE = 100`) `'01'`, başka bir error'da `'90'` yazar ve error'u log'a yazar. | [lgipdb01.cbl:510-522](../../base/src/lgipdb01.cbl#L510-L522) |
| 12 | `LGIPDB01` | Caller'a return eder. | [lgipdb01.cbl:312-313](../../base/src/lgipdb01.cbl#L312-L313) |

**163 byte nereden geliyor.** `WS-CA-HEADERTRAILER-LEN` 33 byte'tır
([lgipdb01.cbl:73](../../base/src/lgipdb01.cbl#L73)): 28 byte'lık header ve 5 byte'lık
`'FINAL'` işareti. Buna `WS-FULL-HOUSE-LEN` 130 eklenir
([lgpolicy.cpy:25](../../base/src/lgpolicy.cpy#L25)). House data'sı 158. byte'ta biter,
`'FINAL'` 159-163. byte'lara yazılır; yani kontrol yazılan alanın tamamını kapsar.

## Output (COMMAREA)

Başarılı (`'00'`) bir query'den sonra dolu gelen field'lar:

| Field | Type | Kaynak column | Not |
|---|---|---|---|
| `CA-ISSUE-DATE` | `X(10)` | `POLICY.ISSUEDATE` | Db2 `DATE`, string olarak döner |
| `CA-EXPIRY-DATE` | `X(10)` | `POLICY.EXPIRYDATE` | |
| `CA-LASTCHANGED` | `X(26)` | `POLICY.LASTCHANGED` | Db2'nin 26 karakterlik timestamp string'i. Update'te değiştirilmeden geri gönderilir |
| `CA-BROKERID` | `9(10)` | `POLICY.BROKERID` | Column `NULL` ise `0` |
| `CA-BROKERSREF` | `X(10)` | `POLICY.BROKERSREFERENCE` | Column `NULL` ise space |
| `CA-PAYMENT` | `9(6)` | `POLICY.PAYMENT` | Column `NULL` ise `0` |
| `CA-H-PROPERTY-TYPE` | `X(15)` | `HOUSE.PROPERTYTYPE` | |
| `CA-H-BEDROOMS` | `9(3)` | `HOUSE.BEDROOMS` | |
| `CA-H-VALUE` | `9(8)` | `HOUSE.VALUE` | |
| `CA-H-HOUSE-NAME` | `X(20)` | `HOUSE.HOUSENAME` | |
| `CA-H-HOUSE-NUMBER` | `X(4)` | `HOUSE.HOUSENUMBER` | |
| `CA-H-POSTCODE` | `X(8)` | `HOUSE.POSTCODE` | |
| `CA-H-FILLER(1:5)` | `X(5)` | — | Sabit `'FINAL'` |
| `CA-RETURN-CODE` | `9(2)` | — | `00` |

`CA-CUSTOMER-NUM` ve `CA-POLICY-NUM` değişmez. `POLICY.POLICYTYPE` ve `POLICY.COMMISSION`
okunmaz.

## Return code'lar

| Code | Koşul | Nerede set ediliyor | Caller ne görür |
|---|---|---|---|
| `00` | Row bulundu | [lgipdb01.cbl:258](../../base/src/lgipdb01.cbl#L258) | Field'lar dolu |
| `01` | Row yok (`SQLCODE = 100`): policy number yok, customer number policy'ninkiyle eşleşmiyor ya da policy house değil (`HOUSE`'ta row'u yok) | [lgipdb01.cbl:512-514](../../base/src/lgipdb01.cbl#L512-L514) | Screen "No data was returned." gösterir |
| `90` | Diğer her `SQLCODE`. Db2 deadlock/timeout (-913) ve house column'larından birinin `NULL` olması da buraya düşer | [lgipdb01.cbl:515-519](../../base/src/lgipdb01.cbl#L515-L519) | Error `LGSTSQ` ile log'lanır; screen aynı "No data was returned." mesajını gösterir |
| `98` | Row bulundu ama COMMAREA 163 byte'tan kısa | [lgipdb01.cbl:487-489](../../base/src/lgipdb01.cbl#L487-L489) | Screen her zaman 32.500 byte gönderdiği için pratikte oluşmaz |
| `99` | Request ID tanınmadı | [lgipdb01.cbl:307-308](../../base/src/lgipdb01.cbl#L307-L308) | `99` |
| — | COMMAREA hiç yok | [lgipol01.cbl:79-83](../../base/src/lgipol01.cbl#L79-L83) | ABEND `LGCA` |

Screen tarafı: `CA-RETURN-CODE > 0` ise code'un ne olduğuna bakmadan "No data was returned."
gösterir ([lgtestp3.cbl:74-76](../../base/src/lgtestp3.cbl#L74-L76),
[285-287](../../base/src/lgtestp3.cbl#L285-L287)). Menu 4'te bu durumda güncelleme
adımına hiç geçilmez. Başarılıysa 8 alanı ekrana yazar
([lgtestp3.cbl:78-89](../../base/src/lgtestp3.cbl#L78-L89)).

## Okunan data

**Db2 `POLICY`** ([db2cre.jcl:161-176](../../base/cntl/db2cre.jcl#L161-L176)) ve
**`HOUSE`** ([db2cre.jcl:240-252](../../base/cntl/db2cre.jcl#L240-L252)) table'ları, inner
join ile. `KSDSPOLY` VSAM dosyası bu operasyonda hiç okunmaz; policy bilgisinin tek kaynağı
Db2.

## C#'a taşırken dikkat edilecek davranışlar

1. **Policy tipi join ile kontrol ediliyor.** Query `POLICYTYPE`'a bakmıyor; `HOUSE`'la inner
   join yaptığı için house olmayan bir policy number'ı row döndürmez ve sonuç `01` olur.
   Customer number da eşleşmek zorunda: başka bir customer'ın policy'si `01` döner.
2. **Deadlock/timeout `90`.** Customer Inquire'ın aksine -913 `01` sayılmıyor, genel error
   olarak `90` dönüyor. Yeni sistemde `503` / `"90"` döner (ADR 0002, karar 8).
3. **`NULL` davranışı column'a göre değişiyor.** `BROKERID`, `BROKERSREFERENCE` ve `PAYMENT`
   için indicator variable var: `NULL` ise `0` ya da space döner
   ([lgipdb01.cbl:460-462](../../base/src/lgipdb01.cbl#L460-L462),
   [494-499](../../base/src/lgipdb01.cbl#L494-L499)). House column'larının hepsi Db2'de
   `NULL` olabildiği halde indicator'ları yok; biri `NULL` ise Db2 error verir ve sonuç `90`
   olur. Bu operasyonla eklenen policy'ler her column'u doldurduğu için pratikte sorun çıkmaz.
4. **Length, query'den sonra kontrol ediliyor.** Kısa bir COMMAREA ile gelen request de
   Db2'ye gidiyor; `98` ancak row bulunduktan sonra veriliyor. Motor'un aksine 163 byte'lık
   kontrol, yazılan alanın (`'FINAL'` dahil) tamamını kapsıyor.
5. **`'FINAL'` işareti** COMMAREA'ya özgü bir "data burada bitti" işareti; API cevabında
   karşılığı yok.
6. **Not found olunca field'lar temizlenmiyor.** `'01'` ya da `'90'` durumunda COMMAREA'daki
   policy field'larına dokunulmuyor; içlerinde caller'ın gönderdiği ne varsa kalıyor.
   Sadece return code güvenilir.
7. **Request ID küçük harfle de kabul ediliyor.** `LGIPDB01` karşılaştırmadan önce uppercase'e
   çeviriyor (`01ihou` çalışır). `LGIPOL01` ise ne length'e ne request ID'ye bakıyor.
8. **Numeric field'lar işaretsiz.** COMMAREA'daki `PIC 9` field'ları negatif value taşıyamaz;
   Db2'de negatif bir bedrooms ya da value varsa işareti kaybolur.
