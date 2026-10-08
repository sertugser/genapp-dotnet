# ADR 0004: Eski COBOL kodunu CICS ve Db2 olmadan çalıştırmak için test düzeneği

| Alan | Değer |
|---|---|
| Durum | Önerildi |
| Tarih | 2026-10-08 |
| Yazan | Oğuz |
| İlgili iş | #12 (LGAPOL01 ile deneme) |

## Bağlam

GenApp, IBM'in z/OS üzerindeki CICS bölgesinde çalışan bir COBOL uygulamasıdır; veriyi Db2 ve
VSAM'de tutar (`docs/system-overview.md`). Biz bunu .NET'e taşıyoruz. Elimizde ne CICS ne Db2 var.

Taşımanın doğru olup olmadığını anlamak için eski programın **gerçekte** ne yaptığını bilmemiz
gerekiyor. Test ekibi COBOL kodunu okuyarak önceden tahmin yazıyor (`tests/predictions/`), sonra
COBOL'u çalıştırıp tahminin tutup tutmadığına bakıyoruz. Tahmin tabloları iki tür kural ayırıyor:
COMMAREA arayüz kuralları (yalnızca COBOL çalıştırmalarında ölçülür) ve iş kuralları (COBOL'da ve
C# testlerinde ölçülür). Yani COBOL'u bir yerde çalıştırabilmemiz gerekiyor.

Sorun: GenApp programları normal bir COBOL derleyicisinde derlenmiyor. GnuCOBOL 3.2.0 ile
`lgapol01.cbl` üzerindeki ilk denemede 12 hata çıktı ve hepsinin nedeni CICS'e bağımlılıktı
(`tests/gnucobol/README.md`, "First compile check"):

- **EIB alanları** (`EIBCALEN`, `EIBTRNID`, `EIBTRMID`, `EIBTASKN`): CICS her programa kendiliğinden
  verir, kaynakta tanımlı değildir.
- **`EXEC CICS` komutları**: COBOL değildir; CICS çeviricisi bunları derlemeden önce gerçek
  çağrılara çevirir. GnuCOBOL'da çevirici yoktur.
- **`EXEC SQL` komutları**: Veri katmanındaki programlarda vardır ve Db2 ister. Henüz denenmedi, çünkü ilk
  deneme iş katmanındaki bir program üzerindeydi.

Ayrıca bir iş katmanı programı tek başına yaşamaz: LGAPOL01 veri katmanındaki LGAPDB01'i ve hata
kuyruğuna yazan LGSTSQ'yu çağırır.

## Karar

Programı kendi başına çalıştırıp dışındaki her şeyi **sahte parçalarla** değiştiriyoruz. Bu,
**bağımlılık izolasyonudur**: denediğimiz programı, ona bağlı sistemlerden ayırırız ki sonuç yalnızca
o programın koduna bağlı olsun.

1. **EIB alanları programın kendi değişkenleri olur.** Programın bir kopyasına (`work/*-gc.cbl`)
   dört alan `WORKING-STORAGE` içinde, CICS'in verdiği türlerle tanımlanır. `EXTERNAL` olarak
   tanımlanırlar ki sürücü onları programa vermeden önce doldurabilsin.
2. **`EXEC CICS` komutları GnuCOBOL karşılıklarıyla değişir.** `RETURN` → `GOBACK`. `LINK` → sahte alt
   programa `CALL`. `ASKTIME`, `FORMATTIME`, `ABEND` → ekrana yazan `DISPLAY` (`ABEND` için ardından
   `GOBACK`, çünkü gerçek ABEND geri dönmez). COMMAREA artık `PROCEDURE DIVISION USING` ile gelir.
3. **Bağlı programların yerine stub yazılır** (`stubs/`). Stub aynı `PROGRAM-ID` ve aynı COMMAREA
   yapısıyla çağrılır, çağrıldığını ve ne aldığını ekrana yazar. **Stub sonuç uydurmaz:** dönüş
   kodu atamaz, alan doldurmaz. Ekranda görünen sonuç, denenen programın kendi kodundan gelmelidir.
4. **Bir sürücü program CICS'in rolünü oynar** (`work/drv-*.cbl`). EIB değerlerini verir, COMMAREA'yı
   kurar, programı çağırır, dönüş kodunu yazar. Her durum ayrı bir süreçte çalışır, çünkü CICS her
   görev için `WORKING-STORAGE`'ı yeniden kurar; GnuCOBOL aynı süreçte çağrılar arasında değerleri korur.
5. **`EXEC SQL` için de aynı ilke geçerlidir:** veri katmanı programı çağıran iş katmanı testlerinde
   veri katmanının tamamı stub ile değişir (LGAPDB01'de yapıldığı gibi). Veri katmanı programlarının
   kendisini çalıştırmak için SQL komutları düzeyinde bir karşılık gerekir; bu **henüz denenmedi ve
   karar verilmedi** (bkz. Açık sorular).

Adım adım yöntem `tests/gnucobol/README.md` içindedir; her program için aynı adımlar izlenir.

### Değerlendirilen alternatifler

- **Sadece kodu okumak (çalıştırmamak):** Tahminin doğru olduğunu doğrulayamayız; ölçüm olmaz.
- **Gerçek CICS ve Db2 kurmak (z/OS ya da emülatör):** Erişimimiz yok. Ticari ya da açık kaynak
  CICS emülatörleri bu çalışmada değerlendirilmedi.
- **Yalnızca C# tarafını yazmak:** Karşılaştıracağımız bir referans davranış kalmaz.

## Sonuçlar

### Terimler

- **Stub (sahte parça):** Gerçek bağımlılığın yerine konan, hazır ve sabit davranan parça. Biz yalnızca
  çıktısını kendimiz okuruz. LGAPDB01 ve LGSTSQ stub'larımız böyledir.
- **Mock:** Stub'a ek olarak, kendisine **doğru çağrının doğru bilgiyle yapılıp yapılmadığını**
  kendisi denetleyen ve yanlışsa testi başarısız sayan parçadır. Bizim stub'larımız "çağrıldı" diye
  yazdırır ama hiçbir şeyi otomatik doğrulamaz; bunu gözle okuruz. Bu yüzden mock değil, stub'dır. C#
  tarafında, bir arayüzü taklit edip "şu çağrıldı mı" diye doğrulayan testler mock olacaktır.

### Neyi ölçebiliyoruz

H02 denemesinde (`tests/gnucobol/runs/lgapol01-deneme.md`) şunlar gösterildi:

- COMMAREA arayüz kuralları: LGAPOL01, 10 baytlık COMMAREA ile `98`, 32500 baytlık ile `00` döndürdü.
- Programın karar yolları: hangi girdide hangi dala girdiği, hangi durumda bağlı programı çağırdığı ya
  da çağırmadığı.
- Bağlı programa ne verdiği: stub aldığı COMMAREA'yı gösterir.

Bunlar programın kendi kodunun davranışıdır; varsayımımıza bağlı değildir.

### Neyi ölçemiyoruz

- **Db2, VSAM ve kuyrukların gerçek davranışı.** Stub bizim varsayımımızdır. Gerçek Db2'nin hata
  kodlarını, kısıtlarını (örn. olmayan müşteriye poliçe eklenememesi) ya da kilitleme davranışını
  görmeyiz. LGAPDB01 stub'ı hiçbir şey eklemediği için `00` sonucu LGAPOL01'den gelir ve gerçek
  LGAPDB01 hakkında hiçbir şey söylemez.
- **Stub'ın arkasındaki her şey.** Stub'a girdikten sonraki dönüş kodları, yazılan kayıtlar, üretilen
  müşteri numarası ölçülmez. Bir stub'a dönüş kodu atamak ölçümü varsayıma çevirir ve bunu yapmayız.
  O sonuç gerekiyorsa, davranış bir test ekibi kararı olarak ayrıca yazılır ve ölçüm sayılmaz.
- **CICS çalışma zamanının davranışı.** ABEND sonrası görev sonlanması, `LINK` ile COMMAREA'nın
  aktarılma biçimi, işlem ve bölge sınırları bizim karşılıklarımızla birebir aynı olmayabilir. Tarih
  ve saat de sahtedir (mesajlarda boş).
- **Çalıştırılan kod, orijinal kodun birebir kendisi değildir.** Kopyada EIB tanımları, `USING`, `CALL`
  ve `DISPLAY` var. Derleyiciye bağlı farklar da var: `COMP` bir alana `32500` taşınınca GnuCOBOL `2500`
  tutuyor (denendi), bu yüzden `EIBCALEN` `COMP-5` tanımlandı. z/OS derleyicisinde aynı
  davranışın olduğunu doğrulamadık. Her program için `diff work/X.cbl work/X-gc.cbl` çıktısı, hangi
  değişikliklerin yapıldığını gösterir ve run kaydına eklenir.
- **Ulaşılamayan kod.** Bir komutun yerine konan karşılığı hiçbir girdiyle çalışmıyorsa yalnızca derlenmiş
  olur (LGAPOL01'de `IF EIBCALEN > 0` içindeki iki `LGSTSQ` çağrısı). Run kaydında bu açıkça yazılır.

### Kullanım kuralları

- **Ölçüm sırası.** Ölçülecek bir çalıştırma, ilgili tahmin dosyası `main`'e girmeden yapılmaz ve sonuç
  tahminlerden önce paylaşılmaz. Aksi hâlde tahmin sonucun kopyasına döner. Yöntemi denemek için
  yapılan çalıştırmalar ölçüme girmez ve kayıtlarında bu açıkça yazılır; o sonuç, o program için sonradan
  yazılacak bir tahminde kullanılmaz.
- **Stub'lar varsayımdır ve bu belgelenir.** Her stub'ın ne yaptığı ve ne yapmadığı kodunda yazar. Bir
  stub'a davranış eklemek ayrı bir karar gerektirir.

### Bu kararın maliyeti

- Her yeni program için kopya, stub'lar ve sürücü yazılır. Kopyanın orijinalden farkı elle korunur;
  orijinal değişirse kopya güncellenmelidir.
- Stub sayısı büyürse, aynı stub'ı kullanan testlerin hepsi o stub'ın varsayımına bağlı olur.

## Açık sorular

- **SQL komutları düzeyinde karşılık.** Veri katmanı programlarını (`EXEC SQL` içerenler) çalıştırmak
  istiyorsak, elimizde Db2 ve SQL çeviricisi olmadığı için ya SQL komutlarını `CALL`'a çevirip sahte
  bir veri deposuna bağlamak ya da bu programları yalnızca C# tarafında test etmek gerekir. İkisi de
  denenmedi; karar bu ADR'nin kapsamı dışındadır.
- Stub'a davranış eklemek gerektiğinde bunun nasıl onaylanacağı (kimin kararı, nerede yazılacağı).

## Kaynaklar

- `tests/gnucobol/README.md`: adım adım yöntem ve ilk derleme denemesi
- `tests/gnucobol/runs/lgapol01-deneme.md`: LGAPOL01 deneme çalıştırması (ölçüme dahil değil)
- `tests/predictions/TEMPLATE.md`: tahmin şablonu ve kural türleri
- `docs/system-overview.md`: GenApp'in katmanları
