import { useEffect, useRef, useState } from "react";
import {
  LuSearch, LuPlus, LuPencil, LuSave, LuX, LuIdCard, LuMapPin, LuPhone,
  LuCircleCheck, LuCircleAlert, LuLoaderCircle, LuUserRound,
} from "react-icons/lu";
import { customersApi } from "./api.js";

// Customer screen: replaces the 3270 screen LGTESTC1 (map SSMAPC1 in base/src/ssmap.bms) and its three options
// (1 inquiry, 2 add, 4 update). Field lengths are the COMMAREA lengths of docs/api-design.md section 4.1.
const SECTIONS = [
  {
    title: "Kimlik",
    icon: LuIdCard,
    fields: [
      { name: "firstName", label: "Ad", max: 10 },
      { name: "lastName", label: "Soyad", max: 20 },
      { name: "dateOfBirth", label: "Doğum tarihi", type: "date" },
    ],
  },
  {
    title: "Adres",
    icon: LuMapPin,
    fields: [
      { name: "houseName", label: "Ev adı", max: 20 },
      { name: "houseNumber", label: "Ev no", max: 4 },
      { name: "postcode", label: "Posta kodu", max: 8, upper: true },
    ],
  },
  {
    title: "İletişim",
    icon: LuPhone,
    fields: [
      { name: "phoneHome", label: "Telefon (ev)", max: 20 },
      { name: "phoneMobile", label: "Telefon (cep)", max: 20 },
      { name: "email", label: "E-posta", max: 100, type: "email", wide: true },
    ],
  },
];

const FIELDS = SECTIONS.flatMap((section) => section.fields);
const EMPTY = Object.fromEntries(FIELDS.map((f) => [f.name, ""]));

// Request body of docs/api-design.md section 4.1: every text is sent, an empty one as "" (section 1).
function toRequest(draft) {
  const body = {};
  for (const { name, type } of FIELDS) body[name] = type === "date" ? draft[name] || null : draft[name] ?? "";
  return body;
}

function toDraft(customer) {
  return Object.fromEntries(FIELDS.map(({ name }) => [name, customer?.[name] ?? ""]));
}

function formatDate(value) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? "");
  return match ? `${match[3]}.${match[2]}.${match[1]}` : value;
}

function initials(customer) {
  const letters = `${customer.firstName?.[0] ?? ""}${customer.lastName?.[0] ?? ""}`;
  return letters.toLocaleUpperCase("tr-TR") || "?";
}

export default function CustomersPage() {
  const [query, setQuery] = useState("");
  const [mode, setMode] = useState("empty"); // empty | view | edit | create
  const [customer, setCustomer] = useState(null);
  const [draft, setDraft] = useState(EMPTY);
  const [errors, setErrors] = useState({});
  const [busy, setBusy] = useState(false);
  const [toasts, setToasts] = useState([]);
  const searchRef = useRef(null);
  const firstFieldRef = useRef(null);

  useEffect(() => searchRef.current?.focus(), []);
  useEffect(() => {
    if (mode === "edit" || mode === "create") firstFieldRef.current?.focus();
  }, [mode]);

  const toastId = useRef(0);

  function notify(kind, text) {
    const id = ++toastId.current;
    setToasts((list) => [...list, { id, kind, text }]);
    setTimeout(() => setToasts((list) => list.filter((t) => t.id !== id)), 4500);
  }

  async function inquire(event) {
    event.preventDefault();
    const number = query.trim();
    if (!/^\d{1,10}$/.test(number)) {
      notify("error", "Müşteri numarası yalnızca rakamlardan oluşmalı.");
      return;
    }
    setBusy(true);
    const result = await customersApi.inquire(number);
    setBusy(false);
    if (result.ok) {
      setCustomer(result.data);
      setErrors({});
      setMode("view");
    } else {
      notify("error", result.message);
    }
  }

  function startCreate() {
    setCustomer(null);
    setDraft(EMPTY);
    setErrors({});
    setMode("create");
  }

  function startEdit() {
    setDraft(toDraft(customer));
    setErrors({});
    setMode("edit");
  }

  function cancel() {
    setErrors({});
    setMode(customer ? "view" : "empty");
  }

  async function save(event) {
    event.preventDefault();
    setBusy(true);
    const result = mode === "create"
      ? await customersApi.add(toRequest(draft))
      : await customersApi.update(customer.customerNumber, toRequest(draft));
    setBusy(false);

    if (!result.ok) {
      setErrors(result.errors);
      notify("error", result.message);
      return;
    }
    setCustomer(result.data);
    setQuery(String(result.data.customerNumber));
    setErrors({});
    setMode("view");
    notify("ok", mode === "create"
      ? `Yeni müşteri eklendi. Müşteri no: ${result.data.customerNumber}.`
      : "Müşteri bilgileri güncellendi.");
  }

  const editing = mode === "edit" || mode === "create";

  return (
    <>
      <header className="page-head">
        <div>
          <h1>Müşteriler</h1>
          <p className="page-sub">Müşteri sorgulama, ekleme ve güncelleme</p>
        </div>
        <div className="toolbar">
          <form className="search" onSubmit={inquire} role="search">
            <LuSearch aria-hidden="true" />
            <input
              ref={searchRef}
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              inputMode="numeric"
              maxLength={10}
              placeholder="Müşteri no ile ara"
              aria-label="Müşteri numarası"
            />
            <button type="submit" className="btn btn-dark" disabled={busy}>Sorgula</button>
          </form>
          <button type="button" className="btn btn-primary" onClick={startCreate} disabled={busy}>
            <LuPlus aria-hidden="true" /> Yeni müşteri
          </button>
        </div>
      </header>

      {mode === "empty" && (
        <section className="empty">
          <span className="empty-icon"><LuUserRound aria-hidden="true" /></span>
          <h2>Bir müşteri seçin</h2>
          <p>Müşteri numarasıyla arayın ya da yeni müşteri ekleyin.</p>
        </section>
      )}

      {mode !== "empty" && (
        <form className="record" onSubmit={save} noValidate>
          <div className="record-head">
            {mode === "create" ? (
              <>
                <span className="avatar avatar-new"><LuPlus aria-hidden="true" /></span>
                <div>
                  <h2>Yeni müşteri</h2>
                  <p className="record-sub">Müşteri numarasını sistem verir.</p>
                </div>
              </>
            ) : (
              <>
                <span className="avatar">{initials(customer)}</span>
                <div>
                  <h2>{[customer.firstName, customer.lastName].filter(Boolean).join(" ") || "Adsız müşteri"}</h2>
                  <p className="record-sub">Müşteri no <span className="badge">{customer.customerNumber}</span></p>
                </div>
              </>
            )}
            <div className="record-actions">
              {mode === "view" && (
                <button type="button" className="btn btn-outline" onClick={startEdit}>
                  <LuPencil aria-hidden="true" /> Düzenle
                </button>
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

          {mode === "edit" && (
            <p className="note">Güncellemede dokuz alanın hepsi kaydedilir; boş bırakılan alan boş olarak yazılır.</p>
          )}

          <div className="sections">
            {SECTIONS.map(({ title, icon: Icon, fields }, sectionIndex) => (
              <fieldset key={title} className="card">
                <legend><Icon aria-hidden="true" /> {title}</legend>
                {fields.map((f, fieldIndex) => (
                  <div key={f.name} className={`field${f.wide ? " field-wide" : ""}`}>
                    <label htmlFor={f.name}>{f.label}</label>
                    {editing ? (
                      <>
                        <input
                          id={f.name}
                          ref={sectionIndex === 0 && fieldIndex === 0 ? firstFieldRef : undefined}
                          type={f.type ?? "text"}
                          maxLength={f.max}
                          className={f.upper ? "upper" : undefined}
                          value={draft[f.name]}
                          onChange={(e) => setDraft({ ...draft, [f.name]: e.target.value })}
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
                      <p id={f.name} className={`value${customer[f.name] ? "" : " value-empty"}`}>
                        {f.type === "date" ? formatDate(customer[f.name]) || "—" : customer[f.name] || "—"}
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
