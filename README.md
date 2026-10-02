# genapp-dotnet

**Test-driven, incremental modernization of the IBM CICS General Insurance Application (GenApp) from COBOL/CICS to ASP.NET Core.**

Advisor: Prof. Dr. Hakan Çağlar

## Goal

GenApp is IBM's sample insurance application (customers and policies) written in COBOL, running on CICS with Db2 and VSAM.
We analyse its `base` component statically, derive tests from the COBOL logic, check those tests by running selected
programs in GnuCOBOL, and rebuild its 18 core operations incrementally on ASP.NET Core + PostgreSQL.
Mutation testing (Stryker.NET) measures how many planted bugs our tests actually catch.

## Repository layout

| Folder | Content |
|---|---|
| `base/` | Original GenApp COBOL application from [cicsdev/cics-genapp](https://github.com/cicsdev/cics-genapp) (unchanged) |
| `legacy-analysis/` | Static analysis of the COBOL code (in progress) |
| `docs/` | Design notes and architecture decision records (planned) |
| `src/` | ASP.NET Core solution (planned) |
| `tests/` | Equivalence and integration tests (planned) |

## Scope

**In scope (18 operations):** customer add / inquire / update; motor, house, endowment and commercial policies – add / inquire / delete;
policy update for motor, house and endowment (the original code has no customer delete and no commercial update).

**Out of scope:** JCL batch jobs; setup and monitoring programs (LGSETUP, LGWEBST5); the optional CICS scenarios in `base/`
(web services, CICSPlex SM, Workload Simulator, business events); the CICS named counter – customer numbers come from
the database instead, which is GenApp's own fallback in LGACDB01; running on a real mainframe.

## Tech stack

C# · .NET (LTS) · ASP.NET Core · Entity Framework Core · PostgreSQL · xUnit · Stryker.NET · GnuCOBOL · Docker · GitHub Actions

## Team

| Name | Role |
|---|---|
| Sertuğ Ser | Project Manager |
| Hasan Basri Engin | Legacy Analysis |
| Efekan Egeli | Architecture & Backend |
| Oğuz Aladağ | Analysis & Testing |
| Sercan Furkan Gümüşdoğrayan | Testing & QA |

## License

The original GenApp code in `base/` is distributed under the [Eclipse Public License 2.0](LICENSE).
