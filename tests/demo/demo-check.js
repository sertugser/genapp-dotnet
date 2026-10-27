// Demo check (#54): drives the web interface in a real browser against a system started from scratch with
// "docker compose up --build" and the sample data of base/cntl/db2cre.jcl. Every step uses the screens the way a
// user would (typing, clicking, reading the message on screen); nothing calls the API directly.
//
// Run: BASE_URL=http://localhost:8080 OUT=<folder> node tests/demo/demo-check.js
// Writes <folder>/results.json, <folder>/results.md and one screenshot per step. Exits 1 if a step fails.
// The expected sentences come from src/GenApp.Web/src/api.js and the screens; the expected data from db2cre.jcl.

const fs = require("fs");
const path = require("path");
const { chromium } = require("playwright");

const BASE = process.env.BASE_URL || "http://localhost:8080";
const OUT = process.env.OUT || path.join(__dirname, "out");
fs.mkdirSync(OUT, { recursive: true });

const rows = [];

// Records every toast the page shows, so a step can read the message its own action produced.
function watchToasts() {
  window.__toasts = [];
  new MutationObserver((mutations) => {
    for (const m of mutations) {
      for (const node of m.addedNodes) {
        if (node.nodeType !== 1) continue;
        const toasts = node.matches(".toast") ? [node] : [...node.querySelectorAll(".toast")];
        for (const t of toasts) window.__toasts.push(t.textContent.trim());
      }
    }
  }).observe(document, { childList: true, subtree: true });
}

async function openPage(browser) {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  await page.addInitScript(watchToasts);
  await page.goto(BASE);
  await page.locator("h1").waitFor();
  return page;
}

// Runs the action and returns the text of the first toast that appears after it.
async function toastOf(page, action) {
  const before = await page.evaluate(() => window.__toasts.length);
  await action();
  await page.waitForFunction((n) => window.__toasts.length > n, before, { timeout: 15000 });
  return page.evaluate((n) => window.__toasts[n], before);
}

async function step(page, operation, tried, expected, check) {
  const no = rows.length + 1;
  let seen;
  let ok;
  try {
    ({ seen, ok } = await check());
  } catch (error) {
    seen = `Adım tamamlanamadı: ${String(error.message).split("\n")[0]}`;
    ok = false;
  }
  const file = `${String(no).padStart(2, "0")}.png`;
  await page.screenshot({ path: path.join(OUT, file), fullPage: true }).catch(() => {});
  rows.push({ no, operation, tried, expected, seen, ok, file });
  console.log(`${ok ? "GEÇTİ" : "KALDI"} ${no} ${operation}: ${seen}`);
}

const go = (page, label) => page.locator("nav a", { hasText: label }).click();
const heading = (page) => page.locator(".record-head h2").innerText();
const value = (page, id) => page.locator(`p#${id}`).innerText();
const clickSave = (page) => page.locator("button[type=submit]", { hasText: "Kaydet" }).click();
const btn = (page, name) => page.getByRole("button", { name, exact: true });
const CUSTOMER_Q = 'input[aria-label="Müşteri numarası"]';
const POLICY_Q = 'input[aria-label="Poliçe numarası"]';
const POSTCODE_Q = 'input[aria-label="Posta kodu"]';
const shows = (sub, policy, customer) =>
  new RegExp(`Poliçe no\\s*${policy}\\b`).test(sub) && new RegExp(`Müşteri no\\s*${customer}\\b`).test(sub);

async function fill(page, values) {
  for (const [id, v] of Object.entries(values)) await page.locator(`input#${id}`).fill(String(v));
}

async function inquirePolicy(page, customer, policy) {
  await page.locator(CUSTOMER_Q).fill(String(customer));
  await page.locator(POLICY_Q).fill(String(policy));
  await btn(page, "Sorgula").click();
}

const DATES = { issueDate: "2026-11-01", expiryDate: "2027-10-31" };
const BROKER = { brokerId: 0, brokersReference: "", payment: 0 };

// The add form, the field changed by the update, and the seed policy each type is inquired with
// (db2cre.jcl: motor 1 of customer 2, house 8 of customer 9, endowment 4 of customer 8).
const POLICIES = [
  {
    nav: "Motor poliçesi", op: "Motor", seed: { customer: 2, policy: 1 }, newLabel: "Yeni motor poliçesi",
    form: { ...DATES, make: "Renault", model: "Clio", registrationNumber: "06AB123", colour: "Beyaz",
      manufactureDate: "2022-03-15", value: 450000, cc: 1461, premium: 12500, accidents: 0, ...BROKER },
    update: { field: "colour", value: "Kirmizi" },
    numeric: "premium",
  },
  {
    nav: "Konut poliçesi", op: "Konut", seed: { customer: 9, policy: 8 }, newLabel: "Yeni konut poliçesi",
    form: { ...DATES, propertyType: "Daire", bedrooms: 3, value: 2500000, houseName: "Bilim Sitesi",
      houseNumber: "12", postcode: "06830", ...BROKER },
    update: { field: "bedrooms", value: "4" },
    numeric: "value",
  },
  {
    nav: "Hayat sigortası", op: "Hayat sigortası", seed: { customer: 8, policy: 4 },
    newLabel: "Yeni hayat sigortası poliçesi",
    form: { ...DATES, fundName: "Emeklilik", withProfits: "Y", equities: "N", managedFund: "Y",
      lifeAssured: "Deneme Kisi", term: 20, sumAssured: 150000, ...BROKER },
    update: { field: "term", value: "25" },
    numeric: "sumAssured",
  },
];

const COMMERCIAL_FORM = {
  ...DATES, customer: "Deneme Ltd", propertyType: "Depo", address: "Bilim Cad. 1", postcode: "06830",
  latitude: "39.8400", longitude: "32.7500", firePeril: 10, firePremium: 1000, crimePeril: 20, crimePremium: 2000,
  floodPeril: 5, floodPremium: 500, weatherPeril: 7, weatherPremium: 700, status: 0, rejectionReason: "", ...BROKER,
};

async function main() {
  const browser = await chromium.launch();
  const page = await openPage(browser);
  let newCustomer;

  // ---------------- Customers (operations 1-3) ----------------
  await step(page, "Müşteri sorgula", "Müşteri 1 sorgulandı (örnek veri)",
    "Andrew Pandy, doğum tarihi 11.07.1950, posta kodu PI101OO", async () => {
      await page.locator(CUSTOMER_Q).fill("1");
      await btn(page, "Sorgula").click();
      const h = await heading(page);
      const seen = `${h}, doğum tarihi ${await value(page, "dateOfBirth")}, posta kodu ${await value(page, "postcode")}`;
      return { seen, ok: seen === "Andrew Pandy, doğum tarihi 11.07.1950, posta kodu PI101OO" };
    });

  await step(page, "Müşteri sorgula", "Olmayan müşteri 999999 sorgulandı",
    "Bu numarayla kayıtlı müşteri bulunamadı.", async () => {
      await page.locator(CUSTOMER_Q).fill("999999");
      const seen = await toastOf(page, () => btn(page, "Sorgula").click());
      return { seen, ok: seen === "Bu numarayla kayıtlı müşteri bulunamadı." };
    });

  await step(page, "Müşteri sorgula", "Müşteri numarasına harf yazıldı (\"abc\")",
    "Müşteri numarası yalnızca rakamlardan oluşmalı.", async () => {
      await page.locator(CUSTOMER_Q).fill("abc");
      const seen = await toastOf(page, () => btn(page, "Sorgula").click());
      return { seen, ok: seen === "Müşteri numarası yalnızca rakamlardan oluşmalı." };
    });

  await step(page, "Müşteri ekle", "Doğum tarihi boş bırakılarak müşteri eklenmeye çalışıldı",
    "Bazı alanlar hatalı. İşaretli alanları kontrol edin. + doğum tarihi alanında \"Bu alan zorunlu.\"", async () => {
      await btn(page, "Yeni müşteri").click();
      await fill(page, { firstName: "Ayse", lastName: "Yilmaz" });
      const toast = await toastOf(page, () => clickSave(page));
      const field = await page.locator("#dateOfBirth-error").innerText();
      return { seen: `${toast} + doğum tarihi alanında "${field}"`,
        ok: toast === "Bazı alanlar hatalı. İşaretli alanları kontrol edin." && field === "Bu alan zorunlu." };
    });

  await step(page, "Müşteri ekle", "Bütün alanlar doldurulup yeni müşteri eklendi",
    "\"Yeni müşteri eklendi. Müşteri no: …\" ve kayıt ekranda görünür", async () => {
      await fill(page, { dateOfBirth: "1990-04-23", houseName: "Bilim Sitesi", houseNumber: "12",
        postcode: "ab12cd", phoneHome: "0312 000 0000", phoneMobile: "0555 000 0000", email: "ayse@example.com" });
      const toast = await toastOf(page, () => clickSave(page));
      newCustomer = (/Müşteri no: (\d+)/.exec(toast) || [])[1];
      const h = await heading(page);
      const pc = await value(page, "postcode");
      return { seen: `${toast} Ekranda: ${h}, posta kodu ${pc}`,
        ok: Boolean(newCustomer) && h === "Ayse Yilmaz" && pc === "AB12CD" };
    });

  await step(page, "Müşteri güncelle", "Yeni müşterinin telefonu değiştirildi, sonra yeniden sorgulandı",
    "\"Müşteri bilgileri güncellendi.\" ve yeni telefon kalıcı", async () => {
      await btn(page, "Düzenle").click();
      await fill(page, { phoneMobile: "0555 111 2233" });
      const toast = await toastOf(page, () => clickSave(page));
      await page.locator(CUSTOMER_Q).fill(newCustomer);
      await btn(page, "Sorgula").click();
      await page.locator("p#phoneMobile", { hasText: "0555 111 2233" }).waitFor({ timeout: 5000 }).catch(() => {});
      const phone = await value(page, "phoneMobile");
      return { seen: `${toast} Yeniden sorguda telefon: ${phone}`,
        ok: toast === "Müşteri bilgileri güncellendi." && phone === "0555 111 2233" };
    });

  // ---------------- Motor, house, endowment (operations 4-15) ----------------
  for (const p of POLICIES) {
    await go(page, p.nav);
    await page.locator("h1", { hasText: p.nav }).waitFor();
    const name = p.op === "Hayat sigortası" ? "hayat sigortası poliçesi" : `${p.op.toLocaleLowerCase("tr-TR")} poliçesi`;
    let number;

    await step(page, `${p.op} sorgula`, `Örnek poliçe ${p.seed.policy} (müşteri ${p.seed.customer}) sorgulandı`,
      `Poliçe ${p.seed.policy} ekranda, müşteri ${p.seed.customer}`, async () => {
        await inquirePolicy(page, p.seed.customer, p.seed.policy);
        const sub = await page.locator(".record-head .record-sub").first().innerText();
        const h = await heading(page);
        return { seen: `${h} (${sub.replace(/\s+/g, " ")})`,
          ok: shows(sub, p.seed.policy, p.seed.customer) };
      });

    await step(page, `${p.op} sorgula`, `Poliçe ${p.seed.policy} başka bir müşteriyle (1) sorgulandı`,
      `Bu müşteriye ait bu numarada bir ${name} bulunamadı.`, async () => {
        const seen = await toastOf(page, () => inquirePolicy(page, 1, p.seed.policy));
        return { seen, ok: seen === `Bu müşteriye ait bu numarada bir ${name} bulunamadı.` };
      });

    await step(page, `${p.op} ekle`, "Olmayan müşteriye (999999) poliçe eklenmeye çalışıldı",
      "Bu numarayla kayıtlı müşteri bulunamadı.", async () => {
        await btn(page, p.newLabel).click();
        await fill(page, { customerNumber: "999999", ...p.form });
        const seen = await toastOf(page, () => clickSave(page));
        return { seen, ok: seen === "Bu numarayla kayıtlı müşteri bulunamadı." };
      });

    await step(page, `${p.op} ekle`, `Yeni müşteriye (${newCustomer}) bütün alanlar doldurularak poliçe eklendi`,
      "\"Yeni poliçe eklendi. Poliçe no: …\" ve poliçe ekranda", async () => {
        await fill(page, { customerNumber: newCustomer });
        const toast = await toastOf(page, () => clickSave(page));
        number = (/Poliçe no: (\d+)/.exec(toast) || [])[1];
        const sub = await page.locator(".record-head .record-sub").first().innerText();
        return { seen: `${toast} (${sub.replace(/\s+/g, " ")})`, ok: Boolean(number) && shows(sub, number, newCustomer) };
      });

    await step(page, `${p.op} ekle`, "Sayı alanı boş bırakılarak poliçe eklenmeye çalışıldı",
      "Bazı alanlar hatalı. İşaretli alanları kontrol edin. + alanda \"Bu alan zorunlu.\"", async () => {
        await btn(page, p.newLabel).click();
        await fill(page, { customerNumber: newCustomer, ...p.form, [p.numeric]: "" });
        const toast = await toastOf(page, () => clickSave(page));
        const field = await page.locator(`#${p.numeric}-error`).innerText();
        await btn(page, "Vazgeç").click();
        return { seen: `${toast} + alanda "${field}"`,
          ok: toast === "Bazı alanlar hatalı. İşaretli alanları kontrol edin." && field === "Bu alan zorunlu." };
      });

    await step(page, `${p.op} güncelle`, `Yeni poliçenin "${p.update.field}" alanı ${p.update.value} yapıldı, yeniden sorgulandı`,
      "\"Poliçe güncellendi.\" ve yeni değer kalıcı", async () => {
        await inquirePolicy(page, newCustomer, number);
        await btn(page, "Düzenle").click();
        await fill(page, { [p.update.field]: p.update.value });
        const toast = await toastOf(page, () => clickSave(page));
        await inquirePolicy(page, newCustomer, number);
        await page.waitForTimeout(500);
        const v = await value(page, p.update.field);
        return { seen: `${toast} Yeniden sorguda: ${v}`, ok: toast === "Poliçe güncellendi." && v === p.update.value };
      });

    await step(page, `${p.op} güncelle`, "Aynı poliçe iki sekmede açıldı; ilki kaydetti, ikincisi sonra kaydetmeye çalıştı",
      "İkinci sekmede: Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. …", async () => {
        const other = await openPage(browser);
        await go(other, p.nav);
        await inquirePolicy(other, newCustomer, number);
        await btn(other, "Düzenle").click();
        await btn(page, "Düzenle").click();
        await fill(page, { brokersReference: "SEKME1" });
        const first = await toastOf(page, () => clickSave(page));
        await fill(other, { brokersReference: "SEKME2" });
        const second = await toastOf(other, () => clickSave(other));
        await other.screenshot({ path: path.join(OUT, `${String(rows.length + 1).padStart(2, "0")}-sekme2.png`), fullPage: true });
        await other.close();
        await inquirePolicy(page, newCustomer, number);
        await page.waitForTimeout(500);
        const kept = await value(page, "brokersReference");
        return { seen: `Birinci sekme: ${first} İkinci sekme: ${second} Kayıtta kalan: ${kept}`,
          ok: first === "Poliçe güncellendi." && second.startsWith("Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi.")
            && kept === "SEKME1" };
      });

    await step(page, `${p.op} sil`, "Yeni poliçe silindi, sonra yeniden sorgulandı",
      `"${number} numaralı poliçe silindi." ve sorguda "Bu müşteriye ait bu numarada bir ${name} bulunamadı."`, async () => {
        await btn(page, "Sil").click();
        const toast = await toastOf(page, () => btn(page, "Evet, sil").click());
        const again = await toastOf(page, () => inquirePolicy(page, newCustomer, number));
        return { seen: `${toast} Sorguda: ${again}`,
          ok: toast === `${number} numaralı poliçe silindi.` && again === `Bu müşteriye ait bu numarada bir ${name} bulunamadı.` };
      });
  }

  // ---------------- Commercial (operations 16-18; inquiry in four forms) ----------------
  await go(page, "Ticari poliçe");
  await page.locator("h1", { hasText: "Ticari poliçe" }).waitFor();
  const mode = (label) => page.getByLabel("Sorgu biçimi").selectOption({ label });
  let postcode;
  let commercialNumber;

  await step(page, "Ticari sorgula (müşteri + poliçe)", "Örnek poliçe 10 (müşteri 1) sorgulandı",
    "Poliçe 10 ekranda, müşteri 1", async () => {
      await mode("Müşteri + poliçe no");
      await inquirePolicy(page, 1, 10);
      const sub = await page.locator(".record-head .record-sub").first().innerText();
      postcode = (await value(page, "postcode")).trim();
      return { seen: `${await heading(page)} (${sub.replace(/\s+/g, " ")}), posta kodu ${postcode}`,
        ok: shows(sub, 10, 1) };
    });

  await step(page, "Ticari sorgula (poliçe no)", "Yalnız poliçe no 10 ile sorgulandı",
    "Poliçe 10 ekranda", async () => {
      await mode("Poliçe no");
      await page.locator(POLICY_Q).fill("10");
      await btn(page, "Sorgula").click();
      await page.waitForTimeout(500);
      const sub = await page.locator(".record-head .record-sub").first().innerText();
      return { seen: `${await heading(page)} (${sub.replace(/\s+/g, " ")})`, ok: /Poliçe no\s*10\b/.test(sub) };
    });

  await step(page, "Ticari sorgula (müşteri no)", "Müşteri 1'in ticari poliçeleri listelendi",
    "Liste: en az bir poliçe, içinde poliçe 10", async () => {
      await mode("Müşteri no");
      await page.locator(CUSTOMER_Q).fill("1");
      await btn(page, "Sorgula").click();
      const count = await page.locator(".list-count").innerText();
      const numbers = await page.locator("table.list tbody tr td:first-child").allInnerTexts();
      return { seen: `${count} Poliçeler: ${numbers.join(", ")}`, ok: numbers.includes("10") };
    });

  await step(page, "Ticari sorgula (posta kodu)", `Poliçe 10'un posta koduyla (${postcode}) arandı`,
    "Liste: içinde poliçe 10", async () => {
      await mode("Posta kodu");
      await page.locator(POSTCODE_Q).fill(postcode);
      await btn(page, "Sorgula").click();
      const count = await page.locator(".list-count").innerText();
      const numbers = await page.locator("table.list tbody tr td:first-child").allInnerTexts();
      return { seen: `${count} Poliçeler: ${numbers.join(", ")}`, ok: numbers.includes("10") };
    });

  await step(page, "Ticari sorgula (posta kodu)", "Hiçbir poliçede olmayan posta koduyla (ZZ99ZZ) arandı",
    "\"Kayıt bulunamadı\" ekranı (hata mesajı değil)", async () => {
      await page.locator(POSTCODE_Q).fill("ZZ99ZZ");
      await btn(page, "Sorgula").click();
      const h = await page.locator("section.empty h2").innerText();
      return { seen: h, ok: h === "Kayıt bulunamadı" };
    });

  await step(page, "Ticari sorgula (müşteri + poliçe)", "Poliçe 10 başka bir müşteriyle (2) sorgulandı",
    "Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı.", async () => {
      await mode("Müşteri + poliçe no");
      const seen = await toastOf(page, () => inquirePolicy(page, 2, 10));
      return { seen, ok: seen === "Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı." };
    });

  await step(page, "Ticari ekle", `Yeni müşteriye (${newCustomer}) bütün alanlar doldurularak ticari poliçe eklendi`,
    "\"Yeni poliçe eklendi. Poliçe no: …\" ve poliçe ekranda; ekranda Düzenle düğmesi yok", async () => {
      await btn(page, "Yeni ticari poliçe").click();
      await fill(page, { customerNumber: newCustomer, ...COMMERCIAL_FORM });
      const toast = await toastOf(page, () => clickSave(page));
      commercialNumber = (/Poliçe no: (\d+)/.exec(toast) || [])[1];
      const edit = await btn(page, "Düzenle").count();
      return { seen: `${toast} Düzenle düğmesi: ${edit ? "var" : "yok"}`, ok: Boolean(commercialNumber) && edit === 0 };
    });

  await step(page, "Ticari ekle", "Olmayan müşteriye (999999) ticari poliçe eklenmeye çalışıldı",
    "Bu numarayla kayıtlı müşteri bulunamadı.", async () => {
      await btn(page, "Yeni ticari poliçe").click();
      await fill(page, { customerNumber: "999999", ...COMMERCIAL_FORM });
      const seen = await toastOf(page, () => clickSave(page));
      await btn(page, "Vazgeç").click();
      return { seen, ok: seen === "Bu numarayla kayıtlı müşteri bulunamadı." };
    });

  await step(page, "Ticari sil", "Yeni ticari poliçe silindi, sonra yeniden sorgulandı",
    "\"… numaralı poliçe silindi.\" ve sorguda \"Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı.\"", async () => {
      await mode("Müşteri + poliçe no");
      await inquirePolicy(page, newCustomer, commercialNumber);
      await btn(page, "Sil").click();
      const toast = await toastOf(page, () => btn(page, "Evet, sil").click());
      const again = await toastOf(page, () => inquirePolicy(page, newCustomer, commercialNumber));
      return { seen: `${toast} Sorguda: ${again}`,
        ok: toast === `${commercialNumber} numaralı poliçe silindi.`
          && again === "Bu müşteriye ait bu numarada bir ticari poliçe bulunamadı." };
    });

  await browser.close();

  fs.writeFileSync(path.join(OUT, "results.json"), JSON.stringify(rows, null, 2));
  const cell = (s) => String(s).replace(/\|/g, "\\|").replace(/\n/g, " ");
  const passed = rows.filter((r) => r.ok).length;
  const md = [
    `**Demo kontrolü:** ${passed}/${rows.length} adım geçti.`,
    "",
    "| No | Operasyon | Ne denendi | Beklenen | Görülen | Sonuç |",
    "|---|---|---|---|---|---|",
    ...rows.map((r) => `| ${r.no} | ${cell(r.operation)} | ${cell(r.tried)} | ${cell(r.expected)} | ${cell(r.seen)} | ${r.ok ? "Geçti" : "**Kaldı**"} |`),
  ].join("\n");
  fs.writeFileSync(path.join(OUT, "results.md"), md + "\n");
  console.log(`\n${passed}/${rows.length} adım geçti.`);
  process.exit(passed === rows.length ? 0 : 1);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
