# Demo kontrolü

Final demosundan önce sistemin sıfırdan kurulup 18 operasyonun web arayüzünden sorunsuz çalıştığının kontrolü (#54).

| Alan | Değer |
|---|---|
| Tarih | 2026-10-28 |
| Kod | `main` (f662aad) ve bu PR'ın deneme betiği |
| Çalıştırma | [GitHub Actions, Demo kontrolü](https://github.com/sertugser/genapp-dotnet/actions/runs/40587837982) |
| Sonuç | **39/39 adım geçti**, bulunan hata yok |

## Nasıl yapıldı

1. Sistem boş bir makinede `docker compose down -v` ve `docker compose up --build` ile baştan kuruldu: PostgreSQL,
   migration, örnek veri (`base/cntl/db2cre.jcl`, #51) ve API ile web arayüzü.
2. Web arayüzü gerçek bir tarayıcıda (Chromium) kullanıcı gibi kullanıldı: alanlara yazıldı, düğmelere basıldı ve
   ekranda çıkan mesaj ya da kayıt okundu. API'ye doğrudan istek gönderilmedi. Adımlar
   [`tests/demo/demo-check.js`](../demo/demo-check.js) dosyasındadır.
3. Her adımın beklenen sonucu önceden yazıldı: mesajlar `src/GenApp.Web/src/api.js`'ten, örnek kayıtlar
   `db2cre.jcl`'den. Her adımın ekran görüntüsü çalıştırmanın `demo-check` artifact'ındadır.

Denenenler: 18 operasyonun hepsi (müşteri sorgula, ekle, güncelle; motor, konut ve hayat sigortası için sorgula,
ekle, güncelle, sil; ticari için ekle, sil ve sorgunun dört biçimi) ve hata durumları: olmayan kayıt, olmayan müşteriye
ekleme, harfli numara, boş zorunlu alan, aynı poliçenin iki sekmede güncellenmesi.

Kontrol tekrar çalıştırılabilir: GitHub'da Actions > Demo kontrolü > Run workflow. Demodan hemen önce bir kez daha
çalıştırılması önerilir.

## Sonuçlar

| No | Operasyon | Ne denendi | Beklenen | Görülen | Sonuç |
|---|---|---|---|---|---|
| 1 | Müşteri sorgula | Müşteri 1 sorgulandı (örnek veri) | Andrew Pandy, doğum tarihi 11.07.1950, posta kodu PI101OO | Andrew Pandy, doğum tarihi 11.07.1950, posta kodu PI101OO | Geçti |
| 2 | Müşteri sorgula | Olmayan müşteri 999999 sorgulandı | Bu numarayla kayıtlı müşteri bulunamadı. | Bu numarayla kayıtlı müşteri bulunamadı. | Geçti |
| 3 | Müşteri sorgula | Müşteri numarasına harf yazıldı ("abc") | Müşteri numarası yalnızca rakamlardan oluşmalı. | Müşteri numarası yalnızca rakamlardan oluşmalı. | Geçti |
| 4 | Müşteri ekle | Doğum tarihi boş bırakılarak müşteri eklenmeye çalışıldı | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + doğum tarihi alanında "Bu alan zorunlu." | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + doğum tarihi alanında "Bu alan zorunlu." | Geçti |
| 5 | Müşteri ekle | Bütün alanlar doldurulup yeni müşteri eklendi | "Yeni müşteri eklendi. Müşteri no: …" ve kayıt ekranda görünür | Yeni müşteri eklendi. Müşteri no: 1000001. Ekranda: Ayse Yilmaz, posta kodu AB12CD | Geçti |
| 6 | Müşteri güncelle | Yeni müşterinin telefonu değiştirildi, sonra yeniden sorgulandı | "Müşteri bilgileri güncellendi." ve yeni telefon kalıcı | Müşteri bilgileri güncellendi. Yeniden sorguda telefon: 0555 111 2233 | Geçti |
| 7 | Motor sorgula | Örnek poliçe 1 (müşteri 2) sorgulandı | Poliçe 1 ekranda, müşteri 2 | FORD KA (Poliçe no 1 Müşteri no 2) | Geçti |
| 8 | Motor sorgula | Poliçe 1 başka bir müşteriyle (1) sorgulandı | Bu müşteriye ait bu numarada bir motor poliçesi bulunamadı. | Bu müşteriye ait bu numarada bir motor poliçesi bulunamadı. | Geçti |
| 9 | Motor ekle | Olmayan müşteriye (999999) poliçe eklenmeye çalışıldı | Bu numarayla kayıtlı müşteri bulunamadı. | Bu numarayla kayıtlı müşteri bulunamadı. | Geçti |
| 10 | Motor ekle | Yeni müşteriye (1000001) bütün alanlar doldurularak poliçe eklendi | "Yeni poliçe eklendi. Poliçe no: …" ve poliçe ekranda | Yeni poliçe eklendi. Poliçe no: 1000002. (Poliçe no 1000002 Müşteri no 1000001) | Geçti |
| 11 | Motor ekle | Sayı alanı boş bırakılarak poliçe eklenmeye çalışıldı | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + alanda "Bu alan zorunlu." | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + alanda "Bu alan zorunlu." | Geçti |
| 12 | Motor güncelle | Yeni poliçenin "colour" alanı Kirmizi yapıldı, yeniden sorgulandı | "Poliçe güncellendi." ve yeni değer kalıcı | Poliçe güncellendi. Yeniden sorguda: Kirmizi | Geçti |
| 13 | Motor güncelle | Aynı poliçe iki sekmede açıldı; ilki kaydetti, ikincisi sonra kaydetmeye çalıştı | İkinci sekmede: Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. … | Birinci sekme: Poliçe güncellendi. İkinci sekme: Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. Kaydı yeniden sorgulayıp değişikliğinizi tekrar yapın. Kayıtta kalan: SEKME1 | Geçti |
| 14 | Motor sil | Yeni poliçe silindi, sonra yeniden sorgulandı | "1000002 numaralı poliçe silindi." ve sorguda "Bu müşteriye ait bu numarada bir motor poliçesi bulunamadı." | 1000002 numaralı poliçe silindi. Sorguda: Bu müşteriye ait bu numarada bir motor poliçesi bulunamadı. | Geçti |
| 15 | Konut sorgula | Örnek poliçe 8 (müşteri 9) sorgulandı | Poliçe 8 ekranda, müşteri 9 | FLAT (Poliçe no 8 Müşteri no 9) | Geçti |
| 16 | Konut sorgula | Poliçe 8 başka bir müşteriyle (1) sorgulandı | Bu müşteriye ait bu numarada bir konut poliçesi bulunamadı. | Bu müşteriye ait bu numarada bir konut poliçesi bulunamadı. | Geçti |
| 17 | Konut ekle | Olmayan müşteriye (999999) poliçe eklenmeye çalışıldı | Bu numarayla kayıtlı müşteri bulunamadı. | Bu numarayla kayıtlı müşteri bulunamadı. | Geçti |
| 18 | Konut ekle | Yeni müşteriye (1000001) bütün alanlar doldurularak poliçe eklendi | "Yeni poliçe eklendi. Poliçe no: …" ve poliçe ekranda | Yeni poliçe eklendi. Poliçe no: 1000004. (Poliçe no 1000004 Müşteri no 1000001) | Geçti |
| 19 | Konut ekle | Sayı alanı boş bırakılarak poliçe eklenmeye çalışıldı | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + alanda "Bu alan zorunlu." | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + alanda "Bu alan zorunlu." | Geçti |
| 20 | Konut güncelle | Yeni poliçenin "bedrooms" alanı 4 yapıldı, yeniden sorgulandı | "Poliçe güncellendi." ve yeni değer kalıcı | Poliçe güncellendi. Yeniden sorguda: 4 | Geçti |
| 21 | Konut güncelle | Aynı poliçe iki sekmede açıldı; ilki kaydetti, ikincisi sonra kaydetmeye çalıştı | İkinci sekmede: Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. … | Birinci sekme: Poliçe güncellendi. İkinci sekme: Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. Kaydı yeniden sorgulayıp değişikliğinizi tekrar yapın. Kayıtta kalan: SEKME1 | Geçti |
| 22 | Konut sil | Yeni poliçe silindi, sonra yeniden sorgulandı | "1000004 numaralı poliçe silindi." ve sorguda "Bu müşteriye ait bu numarada bir konut poliçesi bulunamadı." | 1000004 numaralı poliçe silindi. Sorguda: Bu müşteriye ait bu numarada bir konut poliçesi bulunamadı. | Geçti |
| 23 | Hayat sigortası sorgula | Örnek poliçe 4 (müşteri 8) sorgulandı | Poliçe 4 ekranda, müşteri 8 | J. MORRIS (Poliçe no 4 Müşteri no 8) | Geçti |
| 24 | Hayat sigortası sorgula | Poliçe 4 başka bir müşteriyle (1) sorgulandı | Bu müşteriye ait bu numarada bir hayat sigortası poliçesi bulunamadı. | Bu müşteriye ait bu numarada bir hayat sigortası poliçesi bulunamadı. | Geçti |
| 25 | Hayat sigortası ekle | Olmayan müşteriye (999999) poliçe eklenmeye çalışıldı | Bu numarayla kayıtlı müşteri bulunamadı. | Bu numarayla kayıtlı müşteri bulunamadı. | Geçti |
| 26 | Hayat sigortası ekle | Yeni müşteriye (1000001) bütün alanlar doldurularak poliçe eklendi | "Yeni poliçe eklendi. Poliçe no: …" ve poliçe ekranda | Yeni poliçe eklendi. Poliçe no: 1000006. (Poliçe no 1000006 Müşteri no 1000001) | Geçti |
| 27 | Hayat sigortası ekle | Sayı alanı boş bırakılarak poliçe eklenmeye çalışıldı | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + alanda "Bu alan zorunlu." | Bazı alanlar hatalı. İşaretli alanları kontrol edin. + alanda "Bu alan zorunlu." | Geçti |
| 28 | Hayat sigortası güncelle | Yeni poliçenin "term" alanı 25 yapıldı, yeniden sorgulandı | "Poliçe güncellendi." ve yeni değer kalıcı | Poliçe güncellendi. Yeniden sorguda: 25 | Geçti |
| 29 | Hayat sigortası güncelle | Aynı poliçe iki sekmede açıldı; ilki kaydetti, ikincisi sonra kaydetmeye çalıştı | İkinci sekmede: Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. … | Birinci sekme: Poliçe güncellendi. İkinci sekme: Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. Kaydı yeniden sorgulayıp değişikliğinizi tekrar yapın. Kayıtta kalan: SEKME1 | Geçti |
| 30 | Hayat sigortası sil | Yeni poliçe silindi, sonra yeniden sorgulandı | "1000006 numaralı poliçe silindi." ve sorguda "Bu müşteriye ait bu numarada bir hayat sigortası poliçesi bulunamadı." | 1000006 numaralı poliçe silindi. Sorguda: Bu müşteriye ait bu numarada bir hayat sigortası poliçesi bulunamadı. | Geçti |
| 31 | Ticari sorgula (müşteri + poliçe) | Örnek poliçe 10 (müşteri 1) sorgulandı | Poliçe 10 ekranda, müşteri 1 | IBM (Poliçe no 10 Müşteri no 1), posta kodu SO212JN | Geçti |
| 32 | Ticari sorgula (poliçe no) | Yalnız poliçe no 10 ile sorgulandı | Poliçe 10 ekranda | IBM (Poliçe no 10 Müşteri no 1) | Geçti |
| 33 | Ticari sorgula (müşteri no) | Müşteri 1'in ticari poliçeleri listelendi | Liste: en az bir poliçe, içinde poliçe 10 | 1 poliçe bulundu. Ayrıntı için bir satır seçin. Poliçeler: 10 | Geçti |
| 34 | Ticari sorgula (posta kodu) | Poliçe 10'un posta koduyla (SO212JN) arandı | Liste: içinde poliçe 10 | 1 poliçe bulundu. Ayrıntı için bir satır seçin. Poliçeler: 10 | Geçti |
| 35 | Ticari sorgula (posta kodu) | Hiçbir poliçede olmayan posta koduyla (ZZ99ZZ) arandı | "Kayıt bulunamadı" ekranı (hata mesajı değil) | Kayıt bulunamadı | Geçti |
| 36 | Ticari sorgula (müşteri + poliçe) | Poliçe 10 başka bir müşteriyle (2) sorgulandı | Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı. | Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı. | Geçti |
| 37 | Ticari ekle | Yeni müşteriye (1000001) bütün alanlar doldurularak ticari poliçe eklendi | "Yeni poliçe eklendi. Poliçe no: …" ve poliçe ekranda; ekranda Düzenle düğmesi yok | Yeni poliçe eklendi. Poliçe no: 1000007. Düzenle düğmesi: yok | Geçti |
| 38 | Ticari ekle | Olmayan müşteriye (999999) ticari poliçe eklenmeye çalışıldı | Bu numarayla kayıtlı müşteri bulunamadı. | Bu numarayla kayıtlı müşteri bulunamadı. | Geçti |
| 39 | Ticari sil | Yeni ticari poliçe silindi, sonra yeniden sorgulandı | "… numaralı poliçe silindi." ve sorguda "Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı." | 1000007 numaralı poliçe silindi. Sorguda: Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı. | Geçti |


## Gözlemler (hata değil)

- **Poliçe numaralarında boşluk var** (1000002, 1000004, 1000006, 1000007). Olmayan müşteriye (999999) ekleme
  denemesi başarısız olsa da PostgreSQL bir numara harcar ve bu numara geri verilmez. Kayıt eklenmez, yalnızca numara
  atlanır. Demoda yeni poliçenin numarası önceden söylenmemeli.
- **İkinci sekmenin değişikliği kaydedilmedi** (adım 13, 21, 29): kayıtta birinci sekmenin değeri (`SEKME1`) kaldı.
  Bu, `docs/api-design.md` bölüm 5'teki iyimser kilitlemenin beklenen sonucudur.
- **Örnek veride poliçe 7 arayüzde görünmez.** Türü ticari (`C`) olduğu hâlde konut tablosunda satırı var; adresteki
  türle uyuşmayan poliçeye işlem yapılmaz (`docs/api-design.md` bölüm 3, bilinçli sapma). Demoda bu poliçe
  kullanılmamalı.

## Demoda kullanılabilecek örnek kayıtlar

| Ekran | Müşteri no | Poliçe no | Ekranda |
|---|---|---|---|
| Müşteriler | 1 | | Andrew Pandy |
| Motor poliçesi | 2 | 1 | FORD KA |
| Konut poliçesi | 9 | 8 | FLAT |
| Hayat sigortası | 8 | 4 | J. MORRIS |
| Ticari poliçe | 1 | 10 | IBM, posta kodu SO212JN |
