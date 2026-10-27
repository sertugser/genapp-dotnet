// The only way the web interface reaches the system: the REST API of docs/api-design.md.
// Errors arrive as RFC 9457 Problem Details (section 3); they are turned into plain sentences here,
// never shown as legacy return codes (SRS NFR-14).

const CUSTOMERS = "/api/customers";

// Policy types of docs/api-design.md sections 2.2 and 2.3: the address segment and the name used in messages.
export const POLICY_TYPES = {
  motor: { path: "motor-policies", name: "motor poliçesi" },
  house: { path: "house-policies", name: "konut poliçesi" },
};

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
  if (/between/i.test(message)) return "İzin verilen aralığın dışında.";
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

// notFound: the sentence for a 404 "not-found" of this request. The problem "type" (section 3) tells a missing
// customer ("customer-not-found", policy add) from a missing record. A 404 without a problem body comes from an
// address the API does not have yet (for example a policy type whose operations are not coded yet).
function messageFor(status, problem, notFound) {
  switch (status) {
    case 400:
      return "Bazı alanlar hatalı. İşaretli alanları kontrol edin.";
    case 404:
      if (problem?.type === "customer-not-found") return "Bu numarayla kayıtlı müşteri bulunamadı.";
      if (problem?.type === "not-found") return notFound;
      return "Bu işlem sistemde henüz yok.";
    case 503:
      return "Sistem şu an meşgul. Biraz sonra yeniden deneyin.";
    default:
      return "Beklenmeyen bir hata oluştu, işlem yapılmadı.";
  }
}

async function call(method, url, body, notFound = "Bu numarayla kayıtlı müşteri bulunamadı.") {
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
  return {
    ok: false,
    status: response.status,
    message: messageFor(response.status, data, notFound),
    errors: fieldErrors(data),
  };
}

export const customersApi = {
  inquire: (number) => call("GET", `${CUSTOMERS}/${number}`),
  add: (customer) => call("POST", CUSTOMERS, customer),
  update: (number, customer) => call("PUT", `${CUSTOMERS}/${number}`, customer),
};

// Policy add and inquiry (docs/api-design.md sections 2.2 and 2.3) for one policy type of POLICY_TYPES.
export function policiesApi(kind) {
  const { path, name } = POLICY_TYPES[kind];
  const base = (customer) => `${CUSTOMERS}/${customer}/${path}`;
  return {
    inquire: (customer, policy) =>
      call("GET", `${base(customer)}/${policy}`, undefined, `Bu müşteriye ait bu numarada bir ${name} bulunamadı.`),
    add: (customer, policy) => call("POST", base(customer), policy),
  };
}
