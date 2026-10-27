import { useState } from "react";
import { LuUsers, LuCar, LuHouse, LuHeartPulse, LuBuilding2 } from "react-icons/lu";
import CustomersPage from "./CustomersPage.jsx";
import PoliciesPage from "./PoliciesPage.jsx";

// Screens of the new system and the legacy CICS transaction each one replaces (docs/architecture.md section 8).
const SCREENS = [
  { id: "customers", label: "Müşteriler", legacy: "SSC1", icon: LuUsers, ready: true },
  { id: "motor", label: "Motor poliçesi", legacy: "SSP1", icon: LuCar, ready: true },
  { id: "house", label: "Konut poliçesi", legacy: "SSP3", icon: LuHouse, ready: true },
  { id: "endowment", label: "Hayat sigortası", legacy: "SSP2", icon: LuHeartPulse, ready: true },
  { id: "commercial", label: "Ticari poliçe", legacy: "SSP4", icon: LuBuilding2, ready: true },
];

export default function App() {
  const [current, setCurrent] = useState("customers");

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">G</span>
          <div>
            <p className="brand-name">GenApp</p>
            <p className="brand-sub">Sigorta işlemleri</p>
          </div>
        </div>
        <nav aria-label="Ekranlar">
          {SCREENS.map(({ id, label, legacy, icon: Icon, ready }) => {
            const active = id === current;
            return (
              <a
                key={id}
                href={ready ? `#${id}` : undefined}
                onClick={ready ? (e) => { e.preventDefault(); setCurrent(id); } : undefined}
                className={`nav-item${active ? " active" : ""}${ready ? "" : " disabled"}`}
                aria-current={active ? "page" : undefined}
                aria-disabled={ready ? undefined : "true"}
              >
                <Icon aria-hidden="true" />
                <span>{label}</span>
                {ready ? <code title="Eski sistemdeki CICS işlem kodu">{legacy}</code> : <small>yakında</small>}
              </a>
            );
          })}
        </nav>
      </aside>
      <main className="content">
        {current === "customers" ? <CustomersPage /> : <PoliciesPage key={current} kind={current} />}
      </main>
    </div>
  );
}
