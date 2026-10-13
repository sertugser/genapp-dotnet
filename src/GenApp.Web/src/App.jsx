import { LuUsers, LuCar, LuHouse, LuHeartPulse, LuBuilding2 } from "react-icons/lu";
import CustomersPage from "./CustomersPage.jsx";

// Screens of the new system and the legacy CICS transaction each one replaces (docs/architecture.md section 8).
const SCREENS = [
  { id: "customers", label: "Müşteriler", legacy: "SSC1", icon: LuUsers, ready: true },
  { id: "motor", label: "Motor poliçesi", legacy: "SSP1", icon: LuCar },
  { id: "house", label: "Konut poliçesi", legacy: "SSP3", icon: LuHouse },
  { id: "endowment", label: "Hayat sigortası", legacy: "SSP2", icon: LuHeartPulse },
  { id: "commercial", label: "Ticari poliçe", legacy: "SSP4", icon: LuBuilding2 },
];

export default function App() {
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
          {SCREENS.map(({ id, label, legacy, icon: Icon, ready }) => (
            <a
              key={id}
              href={ready ? "#" : undefined}
              className={`nav-item${ready ? " active" : " disabled"}`}
              aria-current={ready ? "page" : undefined}
              aria-disabled={ready ? undefined : "true"}
            >
              <Icon aria-hidden="true" />
              <span>{label}</span>
              {ready ? <code title="Eski sistemdeki CICS işlem kodu">{legacy}</code> : <small>yakında</small>}
            </a>
          ))}
        </nav>
        <p className="sidebar-foot">COBOL / CICS → ASP.NET Core</p>
      </aside>
      <main className="content">
        <CustomersPage />
      </main>
    </div>
  );
}
