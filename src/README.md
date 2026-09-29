# src

ASP.NET Core solution (planned structure):

- `GenApp.Api` – REST endpoints and a simple web UI (replace the 3270/BMS screens)
- `GenApp.Application` – customer and policy services (replace the 7 business programs LGxCUS01 / LGxPOL01)
- `GenApp.Domain` – entities and business rules
- `GenApp.Infrastructure` – EF Core + PostgreSQL repositories (replace the 15 Db2 and VSAM data-access programs)
