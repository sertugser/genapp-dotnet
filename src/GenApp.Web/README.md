# GenApp.Web

Web arayüzü: React 19, esbuild ile derlenir. Ekranlar yalnızca REST API'yi çağırır
([API tasarımı](../../docs/api-design.md)); veritabanına erişmez.

Derlenmiş dosyalar `src/GenApp.Api/wwwroot/assets/` altına yazılır ve API tarafından sunulur.
Bu dosyalar repoya eklenir; böylece arayüzü sadece görmek isteyen biri Node.js kurmadan
`dotnet run --project src/GenApp.Api` ile açabilir.

## Geliştirme

Gerekenler: Node.js 22 veya üstü.

```
cd src/GenApp.Web
npm install
npm run build     # bir kez derler
npm run watch     # dosya değiştikçe yeniden derler
```

Arayüzde bir değişiklik yapan PR, `npm run build` çıktısını (`wwwroot/assets/`) da içermelidir.

| Dosya | Ne |
|---|---|
| `src/main.jsx` | Giriş noktası |
| `src/App.jsx` | Kenar menü ve sayfa düzeni |
| `src/CustomersPage.jsx` | Müşteri ekranı (eski LGTESTC1 / SSMAPC1) |
| `src/PoliciesPage.jsx` | Motor ve konut poliçesi ekranları: sorgula ve ekle (eski LGTESTP1 / SSMAPP1, LGTESTP3 / SSMAPP3) |
| `src/api.js` | API çağrıları ve hata mesajları |
| `src/styles.css` | Görünüm |
