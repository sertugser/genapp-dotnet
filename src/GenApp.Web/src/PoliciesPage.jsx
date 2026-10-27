import { useEffect, useRef, useState } from "react";
import {
  LuSearch, LuPlus, LuSave, LuX, LuCalendarDays, LuCar, LuGauge, LuHouse, LuMapPin, LuBriefcase,
  LuCircleCheck, LuCircleAlert, LuLoaderCircle, LuFileText,
} from "react-icons/lu";
import { policiesApi } from "./api.js";

// Policy screens: replace the 3270 screens LGTESTP1 (motor, map SSMAPP1) and LGTESTP3 (house, map SSMAPP3) in
// base/src/ssmap.bms, options 1 (inquiry) and 2 (add). Field names and lengths are docs/api-design.md sections 4.2
// (common), 4.3 (motor) and 4.4 (house). "digits" is the number of digits of a numeric COMMAREA field.
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
    subtitle: "Motor poliçesi sorgulama ve ekleme",
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
    subtitle: "Konut poliçesi sorgulama ve ekleme",
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
};

const NUMBER = /^\d{1,10}$/;

function fieldsOf(screen) {
  return screen.sections.flatMap((section) => section.fields);
}

function emptyDraft(screen) {
  return Object.fromEntries(fieldsOf(screen).map((f) => [f.name, f.initial ?? ""]));
}

// Request body of sections 4.2-4.4: every field is sent. Texts may be empty (section 1); an empty number or date is
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

  const [customerQuery, setCustomerQuery] = useState("");
  const [policyQuery, setPolicyQuery] = useState("");
  const [mode, setMode] = useState("empty"); // empty | view | create
  const [policy, setPolicy] = useState(null);
  const [customerNumber, setCustomerNumber] = useState("");
  const [draft, setDraft] = useState(() => emptyDraft(screen));
  const [errors, setErrors] = useState({});
  const [busy, setBusy] = useState(false);
  const [toasts, setToasts] = useState([]);
  const customerRef = useRef(null);
  const newCustomerRef = useRef(null);
  const toastId = useRef(0);

  useEffect(() => customerRef.current?.focus(), []);
  useEffect(() => {
    if (mode === "create") newCustomerRef.current?.focus();
  }, [mode]);

  function notify(kindOfToast, text) {
    const id = ++toastId.current;
    setToasts((list) => [...list, { id, kind: kindOfToast, text }]);
    setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), 4500);
  }

  async function inquire(event) {
    event.preventDefault();
    const customer = customerQuery.trim();
    const number = policyQuery.trim();
    if (!NUMBER.test(customer) || !NUMBER.test(number)) {
      notify("error", "Müşteri ve poliçe numarası yalnızca rakamlardan oluşmalı.");
      return;
    }
    setBusy(true);
    const result = await api.inquire(customer, number);
    setBusy(false);
    if (result.ok) {
      setPolicy(result.data);
      setErrors({});
      setMode("view");
    } else {
      notify("error", result.message);
    }
  }

  function startCreate() {
    setPolicy(null);
    setCustomerNumber(customerQuery.trim());
    setDraft(emptyDraft(screen));
    setErrors({});
    setMode("create");
  }

  function cancel() {
    setErrors({});
    setMode(policy ? "view" : "empty");
  }

  async function save(event) {
    event.preventDefault();
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
    setPolicy(result.data);
    setCustomerQuery(String(result.data.customerNumber));
    setPolicyQuery(String(result.data.policyNumber));
    setErrors({});
    setMode("view");
    notify("ok", `Yeni poliçe eklendi. Poliçe no: ${result.data.policyNumber}.`);
  }

  function setField(field, value) {
    setDraft({ ...draft, [field.name]: field.digits ? value.replace(/\D/g, "") : value });
  }

  const Icon = screen.icon;

  return (
    <>
      <header className="page-head">
        <div>
          <h1>{screen.title}</h1>
          <p className="page-sub">{screen.subtitle}</p>
        </div>
        <div className="toolbar">
          <form className="search" onSubmit={inquire} role="search">
            <LuSearch aria-hidden="true" />
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
            <input
              value={policyQuery}
              onChange={(e) => setPolicyQuery(e.target.value)}
              inputMode="numeric"
              maxLength={10}
              placeholder="Poliçe no"
              aria-label="Poliçe numarası"
              className="search-short"
            />
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
          <p>Müşteri ve poliçe numarasıyla arayın ya da yeni poliçe ekleyin.</p>
        </section>
      )}

      {mode !== "empty" && (
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
            {mode === "create" && (
              <div className="record-actions">
                <button type="button" className="btn btn-ghost" onClick={cancel} disabled={busy}>
                  <LuX aria-hidden="true" /> Vazgeç
                </button>
                <button type="submit" className="btn btn-primary" disabled={busy}>
                  {busy ? <LuLoaderCircle className="spin" aria-hidden="true" /> : <LuSave aria-hidden="true" />} Kaydet
                </button>
              </div>
            )}
          </div>

          <div className="sections sections-4">
            {screen.sections.map(({ title, icon: SectionIcon, fields: sectionFields }) => (
              <fieldset key={title} className="card">
                <legend><SectionIcon aria-hidden="true" /> {title}</legend>
                {sectionFields.map((f) => (
                  <div key={f.name} className="field">
                    <label htmlFor={f.name}>{f.label}</label>
                    {mode === "create" ? (
                      <>
                        <input
                          id={f.name}
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
