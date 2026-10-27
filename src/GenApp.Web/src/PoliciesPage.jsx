import { useEffect, useRef, useState } from "react";
import {
  LuSearch, LuPlus, LuPencil, LuTrash2, LuSave, LuX, LuArrowLeft, LuCalendarDays, LuCar, LuGauge, LuHouse,
  LuMapPin, LuBriefcase, LuHeartPulse, LuPiggyBank, LuShieldCheck, LuBuilding2, LuFlame, LuClipboardList,
  LuCircleCheck, LuCircleAlert, LuLoaderCircle, LuFileText, LuListX,
} from "react-icons/lu";
import { policiesApi, commercialSearchApi } from "./api.js";

// Policy screens: replace the 3270 screens LGTESTP1 (motor, map SSMAPP1), LGTESTP2 (endowment, SSMAPP2),
// LGTESTP3 (house, SSMAPP3) and LGTESTP4 (commercial, SSMAPP4) in base/src/ssmap.bms: 1 inquiry, 2 add,
// 3 delete, 4 update (the commercial policy has no update, docs/api-design.md section 2.5). Field names and lengths
// are docs/api-design.md sections 4.2-4.6. "digits" is the number of digits of a numeric COMMAREA field.
// brokerId, brokersReference and payment are not on the legacy screens, which always send 0, spaces and 0
// (lgtestp1.cbl:100-102, lgtestp3.cbl:95-97); the API requires them, so they start with the same values.
const DATES = {
  title: "Poliçe dönemi",
  icon: LuCalendarDays,
  fields: [
    { name: "issueDate", label: "Başlangıç tarihi", type: "date" },
    { name: "expiryDate", label: "Bitiş tarihi", type: "date" },
  ],
};

const BROKER = {
  title: "Aracı ve ödeme",
  icon: LuBriefcase,
  fields: [
    { name: "brokerId", label: "Aracı no", digits: 9, initial: "0" },
    { name: "brokersReference", label: "Aracı referansı", max: 10 },
    { name: "payment", label: "Ödeme", digits: 6, initial: "0" },
  ],
};

export const POLICY_SCREENS = {
  motor: {
    title: "Motor poliçesi",
    newLabel: "Yeni motor poliçesi",
    icon: LuCar,
    heading: (p) => [p.make, p.model].filter(Boolean).join(" ") || "Motor poliçesi",
    sections: [
      DATES,
      {
        title: "Araç",
        icon: LuCar,
        fields: [
          { name: "make", label: "Marka", max: 15 },
          { name: "model", label: "Model", max: 15 },
          { name: "registrationNumber", label: "Plaka", max: 7 },
          { name: "colour", label: "Renk", max: 8 },
          { name: "manufactureDate", label: "Üretim tarihi", type: "date" },
        ],
      },
      {
        title: "Değer ve prim",
        icon: LuGauge,
        fields: [
          { name: "value", label: "Araç değeri", digits: 6 },
          { name: "cc", label: "Motor hacmi (cc)", digits: 4 },
          { name: "premium", label: "Prim", digits: 6 },
          { name: "accidents", label: "Kaza sayısı", digits: 6 },
        ],
      },
      BROKER,
    ],
  },
  house: {
    title: "Konut poliçesi",
    newLabel: "Yeni konut poliçesi",
    icon: LuHouse,
    heading: (p) => [p.propertyType, p.houseName].filter(Boolean).join(" · ") || "Konut poliçesi",
    sections: [
      DATES,
      {
        title: "Konut",
        icon: LuHouse,
        fields: [
          { name: "propertyType", label: "Konut tipi", max: 15 },
          { name: "bedrooms", label: "Yatak odası", digits: 3 },
          { name: "value", label: "Konut değeri", digits: 8 },
        ],
      },
      {
        title: "Adres",
        icon: LuMapPin,
        fields: [
          { name: "houseName", label: "Ev adı", max: 20 },
          { name: "houseNumber", label: "Ev no", max: 4 },
          { name: "postcode", label: "Posta kodu", max: 8 },
        ],
      },
      BROKER,
    ],
  },
  // Section 4.5. withProfits, equities and managedFund are one-character texts whose meaning is left to the
  // legacy code, so the screen does not restrict their values.
  endowment: {
    title: "Hayat sigortası",
    newLabel: "Yeni hayat sigortası poliçesi",
    icon: LuHeartPulse,
    heading: (p) => p.lifeAssured || p.fundName || "Hayat sigortası poliçesi",
    sections: [
      DATES,
      {
        title: "Fon",
        icon: LuPiggyBank,
        fields: [
          { name: "fundName", label: "Fon adı", max: 10 },
          { name: "withProfits", label: "Kâr paylı", max: 1 },
          { name: "equities", label: "Hisse senedi", max: 1 },
          { name: "managedFund", label: "Yönetilen fon", max: 1 },
        ],
      },
      {
        title: "Teminat",
        icon: LuShieldCheck,
        fields: [
          { name: "lifeAssured", label: "Sigortalı", max: 31 },
          { name: "term", label: "Süre (yıl)", digits: 2 },
          { name: "sumAssured", label: "Teminat tutarı", digits: 6 },
        ],
      },
      BROKER,
    ],
  },
  // Section 4.6.
  commercial: {
    title: "Ticari poliçe",
    newLabel: "Yeni ticari poliçe",
    icon: LuBuilding2,
    updatable: false,
    heading: (p) => p.customer || p.address || "Ticari poliçe",
    sections: [
      DATES,
      {
        title: "Mülk",
        icon: LuBuilding2,
        fields: [
          { name: "customer", label: "Müşteri adı", max: 255 },
          { name: "propertyType", label: "Mülk tipi", max: 255 },
          { name: "address", label: "Adres", max: 255 },
          { name: "postcode", label: "Posta kodu", max: 8 },
          { name: "latitude", label: "Enlem", max: 11 },
          { name: "longitude", label: "Boylam", max: 11 },
        ],
      },
      {
        title: "Riskler ve primler",
        icon: LuFlame,
        fields: [
          { name: "firePeril", label: "Yangın riski", digits: 4 },
          { name: "firePremium", label: "Yangın primi", digits: 8 },
          { name: "crimePeril", label: "Hırsızlık riski", digits: 4 },
          { name: "crimePremium", label: "Hırsızlık primi", digits: 8 },
          { name: "floodPeril", label: "Sel riski", digits: 4 },
          { name: "floodPremium", label: "Sel primi", digits: 8 },
          { name: "weatherPeril", label: "Hava olayı riski", digits: 4 },
          { name: "weatherPremium", label: "Hava olayı primi", digits: 8 },
        ],
      },
      {
        title: "Durum",
        icon: LuClipboardList,
        fields: [
          { name: "status", label: "Durum", digits: 4 },
          { name: "rejectionReason", label: "Ret nedeni", max: 255 },
        ],
      },
      BROKER,
    ],
  },
};

// The four commercial inquiries of section 2.5 and the search fields each one needs.
const COMMERCIAL_SEARCHES = {
  customerPolicy: { label: "Müşteri + poliçe no", customer: true, policy: true },
  policy: { label: "Poliçe no", policy: true },
  customer: { label: "Müşteri no", customer: true },
  postcode: { label: "Posta kodu", postcode: true },
};

const NUMBER = /^\d{1,10}$/;

function fieldsOf(screen) {
  return screen.sections.flatMap((section) => section.fields);
}

function emptyDraft(screen) {
  return Object.fromEntries(fieldsOf(screen).map((f) => [f.name, f.initial ?? ""]));
}

function toDraft(screen, policy) {
  return Object.fromEntries(fieldsOf(screen).map(({ name }) => [name, policy[name] == null ? "" : String(policy[name])]));
}

// Request body of sections 4.2-4.6: every field is sent. Texts may be empty (section 1); an empty number or date is
// sent as null, so the API reports it as a required field instead of the screen guessing a value.
function toRequest(screen, draft) {
  const body = {};
  for (const { name, type, digits } of fieldsOf(screen)) {
    const value = draft[name] ?? "";
    if (type === "date") body[name] = value || null;
    else if (digits) body[name] = value === "" ? null : Number(value);
    else body[name] = value;
  }
  return body;
}

function formatDate(value) {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value ?? "");
  return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
}

// lastChanged is returned in the server's format (section 5); only shown here, never edited.
function formatTimestamp(value) {
  const match = /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})/.exec(value ?? "");
  return match ? `${match[3]}.${match[2]}.${match[1]} ${match[4]}:${match[5]}` : value;
}

function display(field, value) {
  if (value === null || value === undefined || value === "") return "—";
  if (field.type === "date") return formatDate(value);
  if (field.digits) return Number(value).toLocaleString("tr-TR");
  return value;
}

export default function PoliciesPage({ kind }) {
  const screen = POLICY_SCREENS[kind];
  const api = policiesApi(kind);
  const updatable = screen.updatable !== false;

  const [searchBy, setSearchBy] = useState("customerPolicy");
  const [customerQuery, setCustomerQuery] = useState("");
  const [policyQuery, setPolicyQuery] = useState("");
  const [postcodeQuery, setPostcodeQuery] = useState("");
  const [mode, setMode] = useState("empty"); // empty | list | view | create | edit
  const [results, setResults] = useState(null);
  const [policy, setPolicy] = useState(null);
  const [customerNumber, setCustomerNumber] = useState("");
  const [draft, setDraft] = useState(() => emptyDraft(screen));
  const [errors, setErrors] = useState({});
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [busy, setBusy] = useState(false);
  const [toasts, setToasts] = useState([]);
  const customerRef = useRef(null);
  const newCustomerRef = useRef(null);
  const firstFieldRef = useRef(null);
  const toastId = useRef(0);

  useEffect(() => customerRef.current?.focus(), []);
  useEffect(() => {
    if (mode === "create") newCustomerRef.current?.focus();
    if (mode === "edit") firstFieldRef.current?.focus();
  }, [mode]);

  function notify(kindOfToast, text) {
    const id = ++toastId.current;
    setToasts((list) => [...list, { id, kind: kindOfToast, text }]);
    setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), 6000);
  }

  function show(found) {
    setPolicy(found);
    setErrors({});
    setConfirmDelete(false);
    setMode("view");
  }

  async function inquire(event) {
    event.preventDefault();
    const search = kind === "commercial" ? COMMERCIAL_SEARCHES[searchBy] : COMMERCIAL_SEARCHES.customerPolicy;
    const customer = customerQuery.trim();
    const number = policyQuery.trim();
    const postcode = postcodeQuery.trim();

    if ((search.customer && !NUMBER.test(customer)) || (search.policy && !NUMBER.test(number))) {
      notify("error", search.customer && search.policy
        ? "Müşteri ve poliçe numarası yalnızca rakamlardan oluşmalı."
        : "Numara yalnızca rakamlardan oluşmalı.");
      return;
    }
    if (search.postcode && postcode === "") {
      notify("error", "Aramak için bir posta kodu girin.");
      return;
    }

    setBusy(true);
    const result = search.postcode ? await commercialSearchApi.byPostcode(postcode)
      : !search.policy ? await commercialSearchApi.byCustomer(customer)
      : !search.customer ? await commercialSearchApi.byPolicy(number)
      : await api.inquire(customer, number);
    setBusy(false);

    if (!result.ok) {
      notify("error", result.message);
      return;
    }
    if (Array.isArray(result.data)) {
      setResults(result.data);
      setPolicy(null);
      setMode("list");
    } else {
      setResults(null);
      show(result.data);
    }
  }

  function startCreate() {
    setPolicy(null);
    setCustomerNumber(customerQuery.trim());
    setDraft(emptyDraft(screen));
    setErrors({});
    setConfirmDelete(false);
    setMode("create");
  }

  function startEdit() {
    setDraft(toDraft(screen, policy));
    setErrors({});
    setConfirmDelete(false);
    setMode("edit");
  }

  function cancel() {
    setErrors({});
    setMode(policy ? "view" : results ? "list" : "empty");
  }

  async function save(event) {
    event.preventDefault();
    if (mode === "edit") {
      setBusy(true);
      const result = await api.update(policy.customerNumber, policy.policyNumber, {
        ...toRequest(screen, draft),
        lastChanged: policy.lastChanged,
      });
      setBusy(false);
      if (!result.ok) {
        setErrors(result.errors);
        notify("error", result.message);
        return;
      }
      show(result.data);
      notify("ok", "Poliçe güncellendi.");
      return;
    }

    const customer = customerNumber.trim();
    if (!NUMBER.test(customer)) {
      setErrors({ customerNumber: "Müşteri numarası yalnızca rakamlardan oluşmalı." });
      notify("error", "Poliçenin ekleneceği müşterinin numarasını girin.");
      return;
    }
    setBusy(true);
    const result = await api.add(customer, toRequest(screen, draft));
    setBusy(false);

    if (!result.ok) {
      setErrors(result.errors);
      notify("error", result.message);
      return;
    }
    setResults(null);
    setCustomerQuery(String(result.data.customerNumber));
    setPolicyQuery(String(result.data.policyNumber));
    show(result.data);
    notify("ok", `Yeni poliçe eklendi. Poliçe no: ${result.data.policyNumber}.`);
  }

  async function remove() {
    setBusy(true);
    const result = await api.remove(policy.customerNumber, policy.policyNumber);
    setBusy(false);
    setConfirmDelete(false);
    if (!result.ok) {
      notify("error", result.message);
      return;
    }
    notify("ok", `${policy.policyNumber} numaralı poliçe silindi.`);
    const remaining = results?.filter((p) => p.policyNumber !== policy.policyNumber) ?? null;
    setResults(remaining);
    setPolicy(null);
    setMode(remaining ? "list" : "empty");
  }

  function setField(field, value) {
    setDraft({ ...draft, [field.name]: field.digits ? value.replace(/\D/g, "") : value });
  }

  const Icon = screen.icon;
  const editing = mode === "create" || mode === "edit";
  const search = kind === "commercial" ? COMMERCIAL_SEARCHES[searchBy] : COMMERCIAL_SEARCHES.customerPolicy;
  const subtitle = updatable
    ? `${screen.title} sorgulama, ekleme, güncelleme ve silme`
    : `${screen.title} sorgulama, ekleme ve silme`;

  return (
    <>
      <header className="page-head">
        <div>
          <h1>{screen.title}</h1>
          <p className="page-sub">{subtitle}</p>
        </div>
        <div className="toolbar">
          <form className="search" onSubmit={inquire} role="search">
            <LuSearch aria-hidden="true" />
            {kind === "commercial" && (
              <select
                value={searchBy}
                onChange={(e) => setSearchBy(e.target.value)}
                aria-label="Sorgu biçimi"
                className="search-mode"
              >
                {Object.entries(COMMERCIAL_SEARCHES).map(([id, { label }]) => (
                  <option key={id} value={id}>{label}</option>
                ))}
              </select>
            )}
            {search.customer && (
              <input
                ref={customerRef}
                value={customerQuery}
                onChange={(e) => setCustomerQuery(e.target.value)}
                inputMode="numeric"
                maxLength={10}
                placeholder="Müşteri no"
                aria-label="Müşteri numarası"
                className="search-short"
              />
            )}
            {search.policy && (
              <input
                value={policyQuery}
                onChange={(e) => setPolicyQuery(e.target.value)}
                inputMode="numeric"
                maxLength={10}
                placeholder="Poliçe no"
                aria-label="Poliçe numarası"
                className="search-short"
              />
            )}
            {search.postcode && (
              <input
                value={postcodeQuery}
                onChange={(e) => setPostcodeQuery(e.target.value.toLocaleUpperCase("en-US"))}
                maxLength={8}
                placeholder="Posta kodu"
                aria-label="Posta kodu"
                className="search-short"
              />
            )}
            <button type="submit" className="btn btn-dark" disabled={busy}>Sorgula</button>
          </form>
          <button type="button" className="btn btn-primary" onClick={startCreate} disabled={busy}>
            <LuPlus aria-hidden="true" /> {screen.newLabel}
          </button>
        </div>
      </header>

      {mode === "empty" && (
        <section className="empty">
          <span className="empty-icon"><Icon aria-hidden="true" /></span>
          <h2>Bir poliçe seçin</h2>
          <p>
            {kind === "commercial"
              ? "Sorgu biçimini seçip arayın ya da yeni poliçe ekleyin."
              : "Müşteri ve poliçe numarasıyla arayın ya da yeni poliçe ekleyin."}
          </p>
        </section>
      )}

      {mode === "list" && results.length === 0 && (
        <section className="empty">
          <span className="empty-icon"><LuListX aria-hidden="true" /></span>
          <h2>Kayıt bulunamadı</h2>
          <p>Bu aramaya uyan ticari poliçe yok.</p>
        </section>
      )}

      {mode === "list" && results.length > 0 && (
        <section className="list-card">
          <p className="list-count">{results.length} poliçe bulundu. Ayrıntı için bir satır seçin.</p>
          <table className="list">
            <thead>
              <tr>
                <th>Poliçe no</th>
                <th>Müşteri no</th>
                <th>Müşteri adı</th>
                <th>Adres</th>
                <th>Posta kodu</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {results.map((p) => (
                <tr key={p.policyNumber} onClick={() => show(p)}>
                  <td>
                    <button type="button" className="link" onClick={(e) => { e.stopPropagation(); show(p); }}>
                      {p.policyNumber}
                    </button>
                  </td>
                  <td>{p.customerNumber}</td>
                  <td>{p.customer || "—"}</td>
                  <td>{p.address || "—"}</td>
                  <td>{p.postcode || "—"}</td>
                  <td>{p.status ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {(mode === "view" || editing) && (
        <form className="record" onSubmit={save} noValidate>
          <div className="record-head">
            {mode === "create" ? (
              <>
                <span className="avatar avatar-new"><LuPlus aria-hidden="true" /></span>
                <div>
                  <h2>{screen.newLabel}</h2>
                  <p className="record-sub">Poliçe numarasını sistem verir.</p>
                </div>
                <div className="field field-inline">
                  <label htmlFor="customerNumber">Müşteri no</label>
                  <input
                    id="customerNumber"
                    ref={newCustomerRef}
                    value={customerNumber}
                    onChange={(e) => setCustomerNumber(e.target.value.replace(/\D/g, ""))}
                    inputMode="numeric"
                    maxLength={10}
                    aria-invalid={errors.customerNumber ? "true" : undefined}
                    aria-describedby={errors.customerNumber ? "customerNumber-error" : undefined}
                  />
                  {errors.customerNumber && (
                    <span id="customerNumber-error" className="field-error">{errors.customerNumber}</span>
                  )}
                </div>
              </>
            ) : (
              <>
                <span className="avatar"><Icon aria-hidden="true" /></span>
                <div>
                  <h2>{screen.heading(policy)}</h2>
                  <p className="record-sub">
                    Poliçe no <span className="badge">{policy.policyNumber}</span>
                    Müşteri no <span className="badge">{policy.customerNumber}</span>
                  </p>
                  {policy.lastChanged && (
                    <p className="record-sub">
                      <LuFileText aria-hidden="true" /> Son değişiklik: {formatTimestamp(policy.lastChanged)}
                    </p>
                  )}
                </div>
              </>
            )}
            <div className="record-actions">
              {mode === "view" && !confirmDelete && (
                <>
                  {results && (
                    <button type="button" className="btn btn-ghost" onClick={() => setMode("list")}>
                      <LuArrowLeft aria-hidden="true" /> Listeye dön
                    </button>
                  )}
                  {updatable && (
                    <button type="button" className="btn btn-outline" onClick={startEdit}>
                      <LuPencil aria-hidden="true" /> Düzenle
                    </button>
                  )}
                  <button type="button" className="btn btn-danger-outline" onClick={() => setConfirmDelete(true)}>
                    <LuTrash2 aria-hidden="true" /> Sil
                  </button>
                </>
              )}
              {editing && (
                <>
                  <button type="button" className="btn btn-ghost" onClick={cancel} disabled={busy}>
                    <LuX aria-hidden="true" /> Vazgeç
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={busy}>
                    {busy ? <LuLoaderCircle className="spin" aria-hidden="true" /> : <LuSave aria-hidden="true" />} Kaydet
                  </button>
                </>
              )}
            </div>
          </div>

          {mode === "view" && confirmDelete && (
            <div className="confirm" role="alertdialog" aria-labelledby="confirm-text">
              <LuCircleAlert aria-hidden="true" />
              <p id="confirm-text">
                {policy.policyNumber} numaralı poliçe kalıcı olarak silinecek. Bu işlem geri alınamaz.
              </p>
              <button type="button" className="btn btn-ghost" onClick={() => setConfirmDelete(false)} disabled={busy}>
                Vazgeç
              </button>
              <button type="button" className="btn btn-danger" onClick={remove} disabled={busy}>
                {busy ? <LuLoaderCircle className="spin" aria-hidden="true" /> : <LuTrash2 aria-hidden="true" />} Evet, sil
              </button>
            </div>
          )}

          {mode === "edit" && (
            <p className="note">
              Güncellemede bütün alanlar kaydedilir; boş bırakılan metin boş olarak yazılır. Ödeme alanı eski
              sistemdeki gibi güncellemede değişmez.
            </p>
          )}

          <div className="sections sections-4">
            {screen.sections.map(({ title, icon: SectionIcon, fields: sectionFields }, sectionIndex) => (
              <fieldset key={title} className="card">
                <legend><SectionIcon aria-hidden="true" /> {title}</legend>
                {sectionFields.map((f, fieldIndex) => (
                  <div key={f.name} className="field">
                    <label htmlFor={f.name}>{f.label}</label>
                    {editing ? (
                      <>
                        <input
                          id={f.name}
                          ref={sectionIndex === 0 && fieldIndex === 0 ? firstFieldRef : undefined}
                          type={f.type ?? "text"}
                          inputMode={f.digits ? "numeric" : undefined}
                          maxLength={f.max ?? f.digits}
                          value={draft[f.name]}
                          onChange={(e) => setField(f, e.target.value)}
                          aria-invalid={errors[f.name] ? "true" : undefined}
                          aria-describedby={errors[f.name] ? `${f.name}-error` : undefined}
                        />
                        <div className="field-meta">
                          {errors[f.name]
                            ? <span id={`${f.name}-error`} className="field-error">{errors[f.name]}</span>
                            : <span />}
                          {f.max && <span className="counter">{draft[f.name].length}/{f.max}</span>}
                        </div>
                      </>
                    ) : (
                      <p id={f.name} className={`value${display(f, policy[f.name]) === "—" ? " value-empty" : ""}`}>
                        {display(f, policy[f.name])}
                      </p>
                    )}
                  </div>
                ))}
              </fieldset>
            ))}
          </div>
        </form>
      )}

      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast toast-${t.kind}`}>
            {t.kind === "ok" ? <LuCircleCheck aria-hidden="true" /> : <LuCircleAlert aria-hidden="true" />}
            <span>{t.text}</span>
          </div>
        ))}
      </div>
    </>
  );
}
