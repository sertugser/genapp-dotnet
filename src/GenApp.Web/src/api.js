// The only way the web interface reaches the system: the REST API of docs/api-design.md.
// Errors arrive as RFC 9457 Problem Details (section 3); they are turned into plain sentences here,
// never shown as legacy return codes (SRS NFR-14).

const CUSTOMERS = "/api/customers";

// Policy types of docs/api-design.md sections 2.2-2.5: the address segment and the name used in messages.
export const POLICY_TYPES = {
  motor: { path: "motor-policies", name: "motor poliçesi" },
  house: { path: "house-policies", name: "konut poliçesi" },
  endowment: { path: "endowment-policies", name: "hayat sigortası poliçesi" },
  commercial: { path: "commercial-policies", name: "ticari poliçe" },
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

// The sentence shown for each error of section 3, chosen by the HTTP status and the problem "type".
// messages.notFound: a 404 "not-found" of this request (which record is missing).
// messages.serverError: a 500 of this request, when the operation gives it a meaning of its own (see policy delete).
// A 404 without a problem body comes from an address the API does not have yet (a policy type whose operations
// are not coded yet).
function messageFor(status, problem, messages) {
  switch (status) {
    case 400:
      return "Bazı alanlar hatalı. İşaretli alanları kontrol edin.";
    case 404:
      if (problem?.type === "customer-not-found") return "Bu numarayla kayıtlı müşteri bulunamadı.";
      if (problem?.type === "not-found") return messages.notFound;
      return "Bu işlem sistemde henüz yok.";
    case 409:
      return "Bu kayıt siz açtıktan sonra başka biri tarafından değiştirildi. Kaydı yeniden sorgulayıp değişikliğinizi tekrar yapın.";
    case 500:
      if (messages.serverError && problem?.type === "database-error") return messages.serverError;
      if (problem?.type === "database-error" || problem?.type === "storage-error") {
        return "Veritabanında bir hata oluştu, işlem yapılmadı. Sorun sürerse sistem yöneticisine bildirin.";
      }
      return "Beklenmeyen bir hata oluştu, işlem yapılmadı.";
    case 503:
      return "Sistem şu an meşgul. Biraz sonra yeniden deneyin.";
    default:
      return "Beklenmeyen bir hata oluştu, işlem yapılmadı.";
  }
}

async function call(method, url, body, messages = { notFound: "Bu numarayla kayıtlı müşteri bulunamadı." }) {
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
    message: messageFor(response.status, data, messages),
    errors: fieldErrors(data),
  };
}

export const customersApi = {
  inquire: (number) => call("GET", `${CUSTOMERS}/${number}`),
  add: (customer) => call("POST", CUSTOMERS, customer),
  update: (number, customer) => call("PUT", `${CUSTOMERS}/${number}`, customer),
};

// Policy operations (docs/api-design.md sections 2.2-2.5) for one policy type of POLICY_TYPES.
export function policiesApi(kind) {
  const { path, name } = POLICY_TYPES[kind];
  const base = (customer) => `${CUSTOMERS}/${customer}/${path}`;
  const notFound = { notFound: `Bu müşteriye ait bu numarada bir ${name} bulunamadı.` };
  return {
    inquire: (customer, policy) => call("GET", `${base(customer)}/${policy}`, undefined, notFound),
    add: (customer, policy) => call("POST", base(customer), policy),
    // The body carries the lastChanged value of the inquiry (section 5); 409 when someone changed it meanwhile.
    update: (customer, number, policy) =>
      call("PUT", `${base(customer)}/${number}`, policy, { notFound: `${notFound.notFound} Silinmiş olabilir.` }),
    // Deleting a policy that does not exist returns 500 / "90", as in the legacy system (section 3, "Korunan
    // davranış"); here that 500 is most likely a missing policy, so the sentence says both.
    remove: (customer, number) =>
      call("DELETE", `${base(customer)}/${number}`, undefined, {
        ...notFound,
        serverError: `${name.charAt(0).toLocaleUpperCase("tr-TR")}${name.slice(1)} silinemedi. Poliçe bulunamamış ya da veritabanında bir hata oluşmuş olabilir.`,
      }),
  };
}

// The three other commercial policy inquiries of section 2.5 (17b-17d). The lists come back empty, not as 404,
// when nothing is found (section 3, "Korunan davranış").
export const commercialSearchApi = {
  byPolicy: (number) =>
    call("GET", `/api/commercial-policies/${number}`, undefined, { notFound: "Bu numarada bir ticari poliçe bulunamadı." }),
  byCustomer: (customer) => call("GET", `${CUSTOMERS}/${customer}/commercial-policies`),
  byPostcode: (postcode) => call("GET", `/api/commercial-policies?postcode=${encodeURIComponent(postcode)}`),
};
