# UC-06 · Motor Policy Update

Var olan bir motor policy'sinin bilgilerini Db2'deki `MOTOR` ve `POLICY` table'larında ve
`KSDSPOLY` VSAM dosyasında güncelleyen operasyon. Policy'nin, kullanıcı onu sorguladıktan
sonra başkası tarafından değiştirilip değiştirilmediğini `LASTCHANGED` timestamp'i ile
kontrol eder (optimistic locking). Dört policy tipi aynı programları kullandığı için bu belge
yalnızca motor'a ait kısımları ve ortak kısımları anlatır. Bu belge, C# kodunu yazacak kişi
için eski sistemin davranışını satır satır tarif eder. Satır numaraları `base/src` altındaki
dosyalara aittir ve linkler ilgili satırı açar.

## Özet

| | |
|---|---|
| **Request ID** (`CA-REQUEST-ID`) | `01UMOT`. `LGUPOL01` length seçimi için kontrol eder; `LGUPDB01` tip table'ını seçmek için kullanır |
| **Entry program** | `LGUPOL01` (business layer) |
| **Call chain** | `LGUPOL01` → `LGUPDB01` → `LGUPVS01` |
| **Yazıldığı yerler** | Db2 `MOTOR` table, Db2 `POLICY` table, VSAM `KSDSPOLY` |
| **Output** | Yeni timestamp `CA-LASTCHANGED`'de, `CA-RETURN-CODE = '00'` |
| **Kodun set ettiği return code'lar** | `00`, `01`, `02`, `90`, `98`, `99`, `81`, `82` |
| **Caller'ın gördüğü return code'lar** | `00`, `90`, `98` (ve ABEND `LGCA`, `LGV3`, `LGV4`). `01`, `02` ve `99` set edilir ama caller'a ulaşmaz; bkz. "Return code'lar" |

## Flow

```mermaid
%%{init: {"theme": "base", "look": "classic", "themeVariables": {"background": "#ffffff", "primaryColor": "#ffffff", "primaryTextColor": "#1f2328", "primaryBorderColor": "#8c959f", "lineColor": "#57606a", "textColor": "#1f2328", "titleColor": "#1f2328", "clusterBkg": "#ffffff", "clusterBorder": "#d0d7de", "edgeLabelBackground": "#ffffff", "fontSize": "15px"}}}%%
flowchart TD
    subgraph ZEMIN["MOTOR POLICY UPDATE — LGUPOL01 → LGUPDB01 → LGUPVS01"]
        direction TD
        S(["Request gelir<br/>01UMOT + yeni bilgiler<br/>+ inquire'dan gelen LASTCHANGED"])
        K{"Ön kontroller"}
        F["OPEN cursor + FETCH<br/>SELECT ... FOR UPDATE"]
        C{"Sonuç?"}
        UM["UPDATE MOTOR"]
        Q6{"başarılı mı?"}
        UP["UPDATE POLICY<br/>LASTCHANGED yenilenir<br/>SQLCODE'a bakılmaz"]
        SEL["SELECT LASTCHANGED<br/>INTO CA-LASTCHANGED"]
        Q7{"SELECT<br/>başarılı mı?"}
        X["RC 01, 02 ya da 90<br/>set edilir"]
        CL["CLOSE cursor<br/>RC = 00 olur<br/>önceki code ezilir"]
        VS["LINK LGUPVS01<br/>READ + REWRITE KSDSPOLY"]
        Q8{"VSAM<br/>başarılı mı?"}
        OK(["Caller RC 00 görür"])
        A1(["ABEND LGCA"])
        R98(["RC 98"])
        R90(["RC 90<br/>hiçbir şey yazılmaz"])
        RM(["RC 01/90 set edilir<br/>CLOSE ile 00 olur<br/>hiçbir şey yazılmaz"])
        RB(["SYNCPOINT ROLLBACK<br/>RC 90 set edilir<br/>CLOSE ile 00 olur"])
        A2(["ABEND LGV3 / LGV4<br/>her şey rollback"])

        S --> K
        K -->|"COMMAREA yok"| A1
        K -->|"length kısa<br/>165 byte"| R98
        K -->|geçti| F --> C
        C -->|"LASTCHANGED<br/>eşit"| UM --> Q6
        C -->|"row yok<br/>FETCH error<br/>LASTCHANGED farklı"| X
        C -->|"OPEN error"| R90
        Q6 -->|evet| UP --> SEL --> Q7
        Q6 -->|hayır| RM
        Q7 -->|hayır| RB
        Q7 -->|evet| CL
        X --> CL
        CL --> VS --> Q8
        Q8 -->|evet| OK
        Q8 -->|hayır| A2
    end

    classDef adim fill:#ffffff,stroke:#8c959f,color:#1f2328,stroke-width:1.5px
    classDef karar fill:#fff8c5,stroke:#d4a72c,color:#1f2328,stroke-width:1.5px
    classDef basari fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px
    classDef hata fill:#fee2e2,stroke:#dc2626,color:#7f1d1d,stroke-width:2px
    classDef giris fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px
    classDef uyari fill:#ffedd5,stroke:#ea580c,color:#7c2d12,stroke-width:2px
    class S giris
    class F,UM,UP,SEL,VS adim
    class K,C,Q6,Q7,Q8 karar
    class OK basari
    class A1,R98,R90,A2 hata
    class X,CL,RM,RB uyari
    style ZEMIN fill:#ffffff,stroke:#d0d7de,color:#1f2328
```

Turuncu kutular, kodun bir return code set ettiği ama bu code'un caller'a ulaşmadığı yerlerdir.

## Input (COMMAREA)

Menu 4 iki adımlıdır ([lgtestp1.cbl:169-234](../../base/src/lgtestp1.cbl#L169-L234)):

1. Screen önce `01IMOT` ile Motor Policy Inquire'ı call eder
   ([05-motor-inquire.md](05-motor-inquire.md)) ve gelen bilgileri ekrana yazar.
2. Kullanıcı bilgileri düzenleyip Enter'a basınca screen, **aynı COMMAREA** üzerinde aşağıdaki
   field'ları doldurur ve `LGUPOL01`'i call eder. `CA-POLICY-NUM` ve `CA-LASTCHANGED`'e
   dokunmaz; ikisi de 1. adımdaki inquire'dan kalır.

Screen bu iki adımı aynı task içinde yapar: `SEND MAP`'ten sonra `RECEIVE MAP` ile
kullanıcının cevabını bekler ([lgtestp1.cbl:192-198](../../base/src/lgtestp1.cbl#L192-L198)).
Bu bekleme sırasında Db2'de hiçbir lock tutulmaz.

| Field | Type | Gelen value | Kullanım |
|---|---|---|---|
| `CA-REQUEST-ID` | `X(6)` | `'01UMOT'` | `LGUPOL01`'de length, `LGUPDB01`'de tip table'ı seçimi |
| `CA-CUSTOMER-NUM` | `9(10)` | screen'den | Cursor'ın `WHERE`'i ve VSAM key |
| `CA-POLICY-NUM` | `9(10)` | inquire'dan | Cursor'ın `WHERE`'i, `MOTOR` update'inin `WHERE`'i ve VSAM key |
| `CA-LASTCHANGED` | `X(26)` | **inquire'dan, değiştirilmeden** | Db2'deki value ile karşılaştırılır |
| `CA-ISSUE-DATE`, `CA-EXPIRY-DATE` | `X(10)` | screen'den | `POLICY.ISSUEDATE`, `POLICY.EXPIRYDATE` |
| `CA-BROKERID` | `9(10)` | **her zaman `0`** | `POLICY.BROKERID` |
| `CA-BROKERSREF` | `X(10)` | **her zaman space** | `POLICY.BROKERSREFERENCE` |
| `CA-PAYMENT` | `9(6)` | **her zaman `0`** | Kullanılmaz; `PAYMENT` update edilmez |
| `CA-M-MAKE` … `CA-M-ACCIDENTS` | (bkz. [04-motor-add.md](04-motor-add.md)) | screen'den | 9 `MOTOR` column'u |

Minimum length, Add'deki gibi 28 + 137 = **165 byte**
([lgupol01.cbl:131-137](../../base/src/lgupol01.cbl#L131-L137)).

## Adım adım

| # | Program | Ne yapıyor | Kaynak |
|---:|---|---|---|
| 1 | `LGUPOL01` | COMMAREA hiç gelmediyse error log'a yazar ve `LGCA` koduyla ABEND eder. | [lgupol01.cbl:99-103](../../base/src/lgupol01.cbl#L99-L103) |
| 2 | `LGUPOL01` | `CA-RETURN-CODE = '00'` yapar. | [lgupol01.cbl:105](../../base/src/lgupol01.cbl#L105) |
| 3 | `LGUPOL01` | Request ID `'01UMOT'` ise gereken length 165 byte; daha kısaysa `'98'` ile return eder. Tanınmayan bir ID'de `'99'` yazar ama **return etmez** (madde 8). | [lgupol01.cbl:113-141](../../base/src/lgupol01.cbl#L113-L141) |
| 4 | `LGUPOL01` | `LGUPDB01`'i aynı COMMAREA ile `LINK` eder, dönünce return eder. | [lgupol01.cbl:143-160](../../base/src/lgupol01.cbl#L143-L160) |
| 5 | `LGUPDB01` | 1-2. adımları tekrarlar (ABEND `LGCA`, `'00'`). Customer ve policy number'larını Db2 integer'ına çevirir. | [lgupdb01.cbl:183-196](../../base/src/lgupdb01.cbl#L183-L196) |
| 6 | `LGUPDB01` | `POLICY_CURSOR`'ı açar. Cursor, customer number ve policy number'a göre `POLICY` row'unu `FOR UPDATE` ile seçer, yani row'u bu task için kilitler. `OPEN` başarısızsa (-913 dahil) `'90'`, error log ve hemen return; VSAM'a gidilmez. | [lgupdb01.cbl:128-143](../../base/src/lgupdb01.cbl#L128-L143), [253-270](../../base/src/lgupdb01.cbl#L253-L270) |
| 7 | `LGUPDB01` | `FETCH` ile row'u okur: issue date, expiry date, `LASTCHANGED`, broker ID, broker's reference. Row yoksa `'01'`, başka bir error'da `'90'` set eder ve 13. adıma atlar. | [lgupdb01.cbl:228-239](../../base/src/lgupdb01.cbl#L228-L239), [273](../../base/src/lgupdb01.cbl#L273), [349-358](../../base/src/lgupdb01.cbl#L349-L358) |
| 8 | `LGUPDB01` | `CA-LASTCHANGED`'i Db2'deki `LASTCHANGED` ile karşılaştırır. Farklıysa `'02'` set eder ve 13. adıma atlar; hiçbir table update edilmez. | [lgupdb01.cbl:278](../../base/src/lgupdb01.cbl#L278), [344-346](../../base/src/lgupdb01.cbl#L344-L346) |
| 9 | `LGUPDB01` | Eşitse `MOTOR` table'ını günceller: 9 motor column'u, `WHERE POLICYNUMBER`. Row yoksa `'01'`, başka bir error'da `'90'` set eder. | [lgupdb01.cbl:283-300](../../base/src/lgupdb01.cbl#L283-L300), [460-494](../../base/src/lgupdb01.cbl#L460-L494) |
| 10 | `LGUPDB01` | 9. adım `'00'` değilse cursor'ı kapatır ve return eder. `POLICY` ve VSAM'a gidilmez. | [lgupdb01.cbl:302-307](../../base/src/lgupdb01.cbl#L302-L307) |
| 11 | `LGUPDB01` | `POLICY` row'unu cursor üzerinden günceller (`WHERE CURRENT OF`): issue date, expiry date, broker ID, broker's reference ve `LASTCHANGED = CURRENT TIMESTAMP`. Bu `UPDATE`'in `SQLCODE`'u kontrol edilmez. | [lgupdb01.cbl:313-326](../../base/src/lgupdb01.cbl#L313-L326) |
| 12 | `LGUPDB01` | Yeni `LASTCHANGED`'i `SELECT` ile `CA-LASTCHANGED`'e okur. Bu `SELECT` başarısızsa `SYNCPOINT ROLLBACK` yapar, `'90'` set eder ve error'u log'a yazar. | [lgupdb01.cbl:328-342](../../base/src/lgupdb01.cbl#L328-L342) |
| 13 | `LGUPDB01` | `CLOSE-PCURSOR` ile cursor'ı kapatır. `CLOSE` başarılıysa `CA-RETURN-CODE = '00'` yapar. `CLOSE` -501 verirse (cursor zaten kapalı) yine `'00'` yapar ve return eder. Başka bir error'da `'90'` yapar ve return eder. | [lgupdb01.cbl:360-381](../../base/src/lgupdb01.cbl#L360-L381) |
| 14 | `LGUPDB01` → `LGUPVS01` | `LGUPVS01`'i `LENGTH(225)` ile call eder. O da `KSDSPOLY` record'unu `READ UPDATE` ile okur (başarısızsa `'81'` + ABEND `LGV3`), sonra make, model, value ve reg number ile `REWRITE` eder (başarısızsa `'82'` + ABEND `LGV4`). | [lgupdb01.cbl:209-212](../../base/src/lgupdb01.cbl#L209-L212), [lgupvs01.cbl:102-166](../../base/src/lgupvs01.cbl#L102-L166) |
| 15 | `LGUPDB01` | Caller'a return eder. | [lgupdb01.cbl:215-216](../../base/src/lgupdb01.cbl#L215-L216) |

## LASTCHANGED kontrolü

**Ne işe yarıyor.** İki kullanıcı aynı policy'yi aynı anda değiştirmeye çalışırsa, ikincinin
birincinin değişikliğini fark etmeden ezmesini engellemek. `POLICY` table'ındaki
`LASTCHANGED` column'u, row'un en son ne zaman değiştiğini tutan bir timestamp'tir. Add'de
ve her başarılı update'te `CURRENT TIMESTAMP` ile yenilenir
([lgapdb01.cbl:284](../../base/src/lgapdb01.cbl#L284),
[lgupdb01.cbl:322](../../base/src/lgupdb01.cbl#L322)). Bu kontrol yalnızca `LGUPDB01`'de var;
customer update'te bir karşılığı yok.

**Nasıl çalışıyor:**

1. **Inquire:** `LASTCHANGED`'in o anki value'su `CA-LASTCHANGED`'e gelir. Bu, Db2'nin 26
   karakterlik timestamp string'idir.
2. **Bekleme:** Kullanıcı ekranda düzenleme yaparken value COMMAREA'da bekler. Bu sırada
   Db2'de bir lock tutulmaz.
3. **Lock:** Update'te `LGUPDB01` row'u `FOR UPDATE` cursor'ıyla okur. Böylece
   karşılaştırma ile update arasında başka bir task row'u değiştiremez.
4. **Karşılaştırma:** İki value 26 byte'lık string olarak birebir karşılaştırılır
   ([lgupdb01.cbl:278](../../base/src/lgupdb01.cbl#L278)).
5. **Sonuç:**
   - **Eşitse:** Policy, kullanıcı onu okuduğundan beri değişmemiştir. Update yapılır ve
     `LASTCHANGED` yenilenir; yeni value `CA-LASTCHANGED` ile geri döner
     ([lgupdb01.cbl:329-334](../../base/src/lgupdb01.cbl#L329-L334)).
   - **Farklıysa:** Arada başka biri policy'yi değiştirmiştir. Hiçbir table update edilmez
     ve `'02'` set edilir ([lgupdb01.cbl:346](../../base/src/lgupdb01.cbl#L346)).

**Örnek senaryo:**

| Zaman | A | B | Db2'deki `LASTCHANGED` |
|---|---|---|---|
| 10:00 | Policy'yi sorgular, `T1` alır | Policy'yi sorgular, `T1` alır | `T1` |
| 10:02 | Kaydeder: `T1 = T1`, update yapılır | | `T2` |
| 10:03 | | Kaydeder: elindeki `T1 ≠ T2`, `'02'` set edilir | `T2` |

**Eski sistemde `'02'` kullanıcıya ulaşmıyor.**
1. `'02'` set edildikten sonra 13. adımdaki `CLOSE-PCURSOR`, cursor'ı başarıyla kapattığı için
   return code'u `'00'` yapıyor ([lgupdb01.cbl:360](../../base/src/lgupdb01.cbl#L360),
   [369-371](../../base/src/lgupdb01.cbl#L369-L371)).
2. Ardından `LGUPVS01`, B'nin bilgileriyle VSAM record'unu yeniden yazıyor.
3. Screen `'00'` gördüğü için B'ye "Motor Policy Updated" gösteriyor.

Sonuç: B'nin değişikliği Db2'ye yazılmadı, A'nınki korundu, ama B bunu bilmiyor ve VSAM'da
B'nin bilgileri duruyor. ADR 0002 karar 9 bu durumu bilinçli sapma olarak kaydediyor: yeni
sistem `409` / `"02"` döndürecek.

**Yeni sistemde.** Kontrol, `policy.last_changed` column'u üzerinde EF Core optimistic
locking ile yapılacak (ADR 0003, karar 3):
- **Client:** Inquire cevabındaki `lastChanged` değerini update request'inde değiştirmeden
  geri gönderir.
- **Kontrol:** Bu value EF Core'a original value olarak verilir. Üretilen
  `UPDATE ... WHERE last_changed = @client_degeri` hiçbir row'u değiştirmezse
  `DbUpdateConcurrencyException` oluşur ve bu `02` / 409'a çevrilir.
- **Kilit:** Eski sistemdeki `FOR UPDATE` lock'unun karşılığı bu koşullu `UPDATE`'tir;
  ayrı bir lock gerekmez.

## Return code'lar

| Code | Koşul | Nerede set ediliyor | Caller ne görür |
|---|---|---|---|
| `00` | Her şey başarılı | [lgupdb01.cbl:190](../../base/src/lgupdb01.cbl#L190), [371](../../base/src/lgupdb01.cbl#L371) | `00`, yeni timestamp `CA-LASTCHANGED`'de |
| `98` | COMMAREA 165 byte'tan kısa | [lgupol01.cbl:134-137](../../base/src/lgupol01.cbl#L134-L137) | `98`. `LGUPDB01` call edilmez. Screen her zaman 32.500 byte gönderdiği için pratikte oluşmaz. |
| `90` | Cursor açılamadı (Db2 deadlock/timeout -913 dahil) | [lgupdb01.cbl:259-270](../../base/src/lgupdb01.cbl#L259-L270) | `90`. Hiçbir yere yazılmaz. |
| `02` | `LASTCHANGED` uyuşmuyor | [lgupdb01.cbl:344-346](../../base/src/lgupdb01.cbl#L344-L346) | **`00`.** Db2 güncellenmez ama VSAM record'u yeni bilgilerle yeniden yazılır. |
| `01` | `FETCH` row bulamadı: policy yok ya da customer number eşleşmiyor | [lgupdb01.cbl:351-352](../../base/src/lgupdb01.cbl#L351-L352) | `00`'a döner. Ardından VSAM `READ` normalde record'u bulamaz (key aynı customer ve policy number'dan oluşur) ve **ABEND `LGV3`** olur. |
| `90` | `FETCH` başka bir error verdi | [lgupdb01.cbl:353-357](../../base/src/lgupdb01.cbl#L353-L357) | **`00`.** Db2 güncellenmez; VSAM record'u varsa yeniden yazılır. |
| `01` / `90` | `MOTOR` update'i row bulamadı ya da error verdi | [lgupdb01.cbl:485-494](../../base/src/lgupdb01.cbl#L485-L494) | **`00`.** 10. adımda cursor kapatılırken ezilir; hiçbir yere yazılmamıştır. |
| `90` | `POLICY` update'inden sonraki `SELECT` başarısız | [lgupdb01.cbl:336-342](../../base/src/lgupdb01.cbl#L336-L342) | **`00`.** `SYNCPOINT ROLLBACK` cursor'ı da kapattığı için `CLOSE` -501 alır ve -501 `'00'` yapılır ([372-375](../../base/src/lgupdb01.cbl#L372-L375)). Her şey rollback edilmiştir. |
| `90` | `CLOSE` -501 dışında bir error verdi | [lgupdb01.cbl:376-379](../../base/src/lgupdb01.cbl#L376-L379) | `90`. VSAM'a gidilmez. Önceki Db2 update'leri rollback edilmez; screen update'te rollback yapmıyor. |
| `99` | Request ID tanınmadı | [lgupol01.cbl:139-140](../../base/src/lgupol01.cbl#L139-L140) | **`00`.** `LGUPOL01` return etmez; `LGUPDB01` başta `'00'` yazar ve devam eder (madde 8). |
| `81` | VSAM `READ` başarısız | [lgupvs01.cbl:147-153](../../base/src/lgupvs01.cbl#L147-L153) | ABEND `LGV3`. Db2 update'leri dahil her şey rollback edilir. |
| `82` | VSAM `REWRITE` başarısız | [lgupvs01.cbl:160-166](../../base/src/lgupvs01.cbl#L160-L166) | ABEND `LGV4`. Her şey rollback edilir. |
| — | COMMAREA hiç yok | [lgupol01.cbl:99-103](../../base/src/lgupol01.cbl#L99-L103) | ABEND `LGCA` |

Screen tarafı: `CA-RETURN-CODE > 0` ise "Error Updating Motor Policy" gösterir ve **rollback
yapmaz**. Başarılıysa "Motor Policy Updated" gösterir
([lgtestp1.cbl:220-234](../../base/src/lgtestp1.cbl#L220-L234),
[296-298](../../base/src/lgtestp1.cbl#L296-L298)).

## Yazılan data

Yazma sırası: önce `MOTOR`, sonra `POLICY`, en son VSAM.

**Db2 `MOTOR` table** ([db2cre.jcl:275-288](../../base/cntl/db2cre.jcl#L275-L288)) — 9 motor
column'unun hepsi request'teki value'larla değiştirilir, `WHERE POLICYNUMBER =
CA-POLICY-NUM` ([lgupdb01.cbl:469-483](../../base/src/lgupdb01.cbl#L469-L483)).

**Db2 `POLICY` table** ([db2cre.jcl:161-176](../../base/cntl/db2cre.jcl#L161-L176)) — cursor'ın
kilitlediği row ([lgupdb01.cbl:318-326](../../base/src/lgupdb01.cbl#L318-L326)):

| Column | Yazılan value |
|---|---|
| `ISSUEDATE`, `EXPIRYDATE` | `CA-ISSUE-DATE`, `CA-EXPIRY-DATE` |
| `LASTCHANGED` | `CURRENT TIMESTAMP` |
| `BROKERID`, `BROKERSREFERENCE` | `CA-BROKERID`, `CA-BROKERSREF` (screen'den gelirse `0` ve space) |
| `PAYMENT`, `COMMISSION`, `POLICYTYPE`, `CUSTOMERNUMBER` | **Değişmez** ([lgupdb01.cbl:126-127](../../base/src/lgupdb01.cbl#L126-L127)) |

**VSAM `KSDSPOLY`** — key (`'M'` + customer number + policy number) ile okunan record'a make,
model, value ve reg number yeniden yazılır
([lgupvs01.cbl:102-159](../../base/src/lgupvs01.cbl#L102-L159)). Record yapısı için bkz.
[04-motor-add.md](04-motor-add.md).

## C#'a taşırken dikkat edilecek davranışlar

1. **Kodun set ettiği code'u döndür.** `CLOSE-PCURSOR`'ın return code'u ezmesi ADR 0002
   karar 9'da bilinçli sapma olarak kayıtlı: yeni sistem `02` için 409, `01` için 404, `90`
   için 500 döndürecek. ADR'de satır 346, 352 ve 354'ün yanında şu iki ezilme yolu da kayıtlı;
   ikisi de aynı ilkeyle ele alınır:
   - `MOTOR` update'i başarısız olunca ([lgupdb01.cbl:302-307](../../base/src/lgupdb01.cbl#L302-L307)).
   - `SYNCPOINT ROLLBACK`'ten sonraki -501 yolunda ([372-375](../../base/src/lgupdb01.cbl#L372-L375)).
2. **`POLICY` update'inin error'u kontrol edilmiyor.** 12. adımdaki `IF SQLCODE NOT EQUAL 0`,
   `UPDATE`'in değil ondan sonraki `SELECT`'in sonucuna bakıyor; yorumu ise "Update of policy
   table" diyor. `POLICY` update'i başarısız olur ama `SELECT` başarılı olursa:
   - `MOTOR` güncellenmiş, `POLICY` güncellenmemiş kalır.
   - `LASTCHANGED` yenilenmez.
   - Caller `00` görür.

   Örneğin geçersiz bir issue date bu duruma yol açar. Yeni sistemde iki update tek
   transaction'da olacağı için (ADR 0003) bu kısmi update oluşamaz. Geçersiz date'ler zaten
   API validation'ında 400 ile reddedilir.
3. **Policy tipi kontrol edilmiyor.** Cursor `POLICYTYPE`'a bakmıyor. `01UMOT` başka tipte
   bir policy number'ıyla gelirse ve `LASTCHANGED` eşleşirse, `MOTOR` update'i row bulamaz
   ve sonuç `01` olur (eski sistemde ezilip `00`). Screen'den bu duruma gelinemez, çünkü
   inquire adımı motor olmayan bir policy'yi zaten `01` ile durduruyor. API'de ise mümkün;
   C# kodu policy'nin motor olduğunu kontrol edip `01` / 404 döndürmeli.
4. **Update edilmeyen column'lar.** `PAYMENT` ve `COMMISSION` hiç update edilmiyor.
   `BROKERID` ve `BROKERSREFERENCE` ise gelen value ile eziliyor; screen bunları her zaman
   `0` ve space gönderiyor.
5. **Rollback davranışı.** Screen update hatasında `SYNCPOINT ROLLBACK` yapmıyor;
   `LGUPDB01` yalnızca 12. adımda kendisi rollback yapıyor. VSAM error'larında ise ABEND
   her şeyi geri alıyor. Yeni sistemde bunların karşılığı tek bir transaction.
6. **Timestamp birebir eşit olmalı.** Karşılaştırma 26 karakterlik string üzerinden
   yapılıyor. Yeni sistemde PostgreSQL mikrosaniye tuttuğu için C# tarafında üretilen
   timestamp mikrosaniyeye kırpılmalı; aksi halde client'a dönen value database'dekinden
   farklı olur ve sonraki update haksız yere `02` alır (ADR 0003, karar 3).
7. **Deadlock/timeout.** Cursor açılırken -913 `90` olarak dönüyor. Yeni sistemde `503` /
   `"90"` (ADR 0002, karar 8).
8. **Tanınmayan request ID `00`'a dönüyor.** `LGUPOL01` `99` yazıp return etmediği için
   `LGUPDB01` çalışıyor ve başta `'00'` yazıyor. `LASTCHANGED` eşleşirse tip table'ını
   seçemediği için onu atlayıp doğrudan `POLICY` update'ine geçiyor. Bu ortak kodun davranışı; motor için screen her zaman `01UMOT`
   gönderdiğinden oluşmuyor. Yeni sistemde de operasyonu URL belirlediği için oluşamaz.
