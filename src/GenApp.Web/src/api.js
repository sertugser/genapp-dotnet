// The only way the web interface reaches the system: the REST API of docs/api-design.md.
// Errors arrive as RFC 9457 Problem Details (section 3); they are turned into plain sentences here,
// never shown as legacy return codes (SRS NFR-14).

const CUSTOMERS = "/api/customers";

async function readJson(response) {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

// ASP.NET reports invalid fields as "FirstName" or "$.firstName" with English messages; the form uses the JSON
// names and Turkish sentences.
function translate(message = "") {
  if (/required/i.test(message)) return "Bu alan zorunlu.";
  if (/length/i.test(message)) return "İzin verilen uzunluğu aşıyor.";
  return "Geçersiz değer.";
}

function fieldErrors(problem) {
  const errors = {};
  for (const [key, messages] of Object.entries(problem?.errors ?? {})) {
    const name = key.replace(/^\$\./, "");
    errors[name.charAt(0).toLowerCase() + name.slice(1)] = translate(messages?.[0]);
  }
  return errors;
}

function messageFor(status) {
  switch (status) {
    case 400:
      return "Bazı alanlar hatalı. İşaretli alanları kontrol edin.";
    case 404:
      return "Bu numarayla kayıtlı müşteri bulunamadı.";
    case 503:
      return "Sistem şu an meşgul. Biraz sonra yeniden deneyin.";
    default:
      return "Beklenmeyen bir hata oluştu, işlem yapılmadı.";
  }
}

async function call(method, url, body) {
  let response;
  try {
    response = await fetch(url, {
      method,
      headers: { Accept: "application/json", ...(body ? { "Content-Type": "application/json" } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    return { ok: false, message: "Sunucuya ulaşılamadı. Bağlantıyı kontrol edin.", errors: {} };
  }

  const data = await readJson(response);
  if (response.ok) return { ok: true, data };
  return { ok: false, status: response.status, message: messageFor(response.status), errors: fieldErrors(data) };
}

export const customersApi = {
  inquire: (number) => call("GET", `${CUSTOMERS}/${number}`),
  add: (customer) => call("POST", CUSTOMERS, customer),
  update: (number, customer) => call("PUT", `${CUSTOMERS}/${number}`, customer),
};
