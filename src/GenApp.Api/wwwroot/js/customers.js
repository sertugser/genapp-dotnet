// Customer screen (legacy LGTESTC1 / map SSMAPC1). Talks only to the REST API in docs/api-design.md section 2.1:
//   1 Sorgula  -> GET  /api/customers/{customerNumber}
//   2 Ekle     -> POST /api/customers
//   4 Güncelle -> PUT  /api/customers/{customerNumber}
// Messages are plain sentences, never the legacy return code (SRS NFR-14).
"use strict";

const API = "/api/customers";
const TEXT_FIELDS = ["firstName", "lastName", "houseName", "houseNumber", "postcode", "phoneHome", "phoneMobile", "email"];
const DATA_FIELDS = ["dateOfBirth", ...TEXT_FIELDS];

const HINTS = {
  inquire: "Müşteri numarasını yazıp Enter'a basın.",
  add: "Bilgileri doldurup Enter'a basın. Müşteri numarasını sistem verir.",
  update: "Önce 1 ile müşteriyi sorgulayın, bilgileri değiştirip 4 ile gönderin. Dokuz alanın hepsi yazılır.",
};

const form = document.getElementById("customer-form");
const statusLine = document.getElementById("status");
const hint = document.getElementById("option-hint");
const submitButton = document.getElementById("submit");
const numberInput = document.getElementById("customerNumber");

const field = (name) => document.getElementById(name);
const labelOf = (name) => document.querySelector(`label[for="${name}"]`)?.textContent ?? name;
const currentOption = () => form.elements.option.value;

function setStatus(message, kind) {
  statusLine.textContent = message;
  statusLine.className = "status" + (kind ? " " + kind : "");
}

function clearInvalid() {
  for (const input of form.querySelectorAll("[aria-invalid]")) input.removeAttribute("aria-invalid");
}

function applyOption() {
  const option = currentOption();
  hint.textContent = HINTS[option];
  numberInput.disabled = option === "add";
  if (option === "add") numberInput.value = "";
  for (const name of DATA_FIELDS) field(name).readOnly = option === "inquire";
  clearInvalid();
  (option === "add" ? field("firstName") : numberInput).focus();
}

function fill(customer) {
  numberInput.value = customer.customerNumber ?? "";
  for (const name of DATA_FIELDS) field(name).value = customer[name] ?? "";
}

function clearFields() {
  numberInput.value = "";
  for (const name of DATA_FIELDS) field(name).value = "";
  clearInvalid();
  setStatus("Hazır.");
}

// Request body of docs/api-design.md section 4.1. Every text is sent, empty ones as "" (section 1).
function requestBody() {
  const body = {};
  for (const name of TEXT_FIELDS) body[name] = field(name).value;
  body.dateOfBirth = field("dateOfBirth").value || null;
  return body;
}

function readCustomerNumber() {
  const value = numberInput.value.trim();
  if (!/^\d{1,10}$/.test(value)) {
    numberInput.setAttribute("aria-invalid", "true");
    setStatus("Müşteri numarası yalnızca rakamlardan oluşmalı.", "error");
    numberInput.focus();
    return null;
  }
  return Number(value);
}

async function readJson(response) {
  try { return await response.json(); } catch { return null; }
}

// Turns an error response (RFC 9457 Problem Details, docs/api-design.md section 3) into a sentence.
async function explain(response) {
  const problem = await readJson(response);
  switch (response.status) {
    case 400: {
      const names = Object.keys(problem?.errors ?? {})
        .map((key) => key.replace(/^\$\./, ""))
        .map((key) => key.charAt(0).toLowerCase() + key.slice(1))
        .filter((name) => field(name));
      for (const name of names) field(name).setAttribute("aria-invalid", "true");
      return names.length
        ? "Şu alanları kontrol edin: " + names.map(labelOf).join(", ") + "."
        : "İstek geçersiz. Alanları kontrol edin.";
    }
    case 404:
      return "Bu numarayla kayıtlı müşteri bulunamadı.";
    case 503:
      return "Sistem şu an meşgul. Biraz sonra yeniden deneyin.";
    default:
      return "Beklenmeyen bir hata oluştu, işlem yapılmadı.";
  }
}

async function send(method, url, body) {
  return fetch(url, {
    method,
    headers: body ? { "Content-Type": "application/json", Accept: "application/json" } : { Accept: "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
}

async function run() {
  clearInvalid();
  const option = currentOption();
  let response;

  if (option === "add") {
    response = await send("POST", API, requestBody());
    if (response.status === 201) {
      const customer = await readJson(response);
      fill(customer);
      return setStatus(`Yeni müşteri eklendi. Müşteri no: ${customer.customerNumber}.`, "ok");
    }
  } else {
    const number = readCustomerNumber();
    if (number === null) return;
    if (option === "inquire") {
      response = await send("GET", `${API}/${number}`);
      if (response.ok) {
        fill(await readJson(response));
        return setStatus("Müşteri bilgileri getirildi.", "ok");
      }
    } else {
      response = await send("PUT", `${API}/${number}`, requestBody());
      if (response.ok) {
        fill(await readJson(response));
        return setStatus("Müşteri bilgileri güncellendi.", "ok");
      }
    }
  }

  setStatus(await explain(response), "error");
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  submitButton.disabled = true;
  setStatus("İşleniyor…");
  try {
    await run();
  } catch {
    setStatus("Sunucuya ulaşılamadı. Bağlantıyı kontrol edin.", "error");
  } finally {
    submitButton.disabled = false;
  }
});

for (const radio of form.elements.option) radio.addEventListener("change", applyOption);
document.getElementById("clear").addEventListener("click", () => { clearFields(); applyOption(); });

// Like the legacy option field: typing 1, 2 or 4 outside an input selects the option.
document.addEventListener("keydown", (event) => {
  if (event.target instanceof HTMLInputElement && event.target.type !== "radio") return;
  const option = { "1": "inquire", "2": "add", "4": "update" }[event.key];
  if (!option) return;
  form.elements.option.value = option;
  applyOption();
});

applyOption();
