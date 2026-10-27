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
| `docs/` | Requirements, API design, architecture and architecture decision records |
| `src/` | ASP.NET Core solution and the React web interface (in progress) |
| `tests/` | Test predictions, the GnuCOBOL harness, equivalence and database tests (in progress) |

## Scope

**In scope (18 operations):** customer add / inquire / update; motor, house, endowment and commercial policies – add / inquire / delete;
policy update for motor, house and endowment (the original code has no customer delete and no commercial update).

**Out of scope:** JCL batch jobs; setup and monitoring programs (LGSETUP, LGWEBST5); the optional CICS scenarios in `base/`
(web services, CICSPlex SM, Workload Simulator, business events); the CICS named counter – customer numbers come from
the database instead, which is GenApp's own fallback in LGACDB01; running on a real mainframe.

## Running the application

The only requirement is [Docker](https://docs.docker.com/get-docker/) (Docker Desktop on Windows and macOS).

    docker compose up --build

When the API has started, open:

- http://localhost:8080 – web interface
- http://localhost:8080/swagger – REST API description

`docker compose up` starts three services in order: `postgres` (PostgreSQL 18), `migrate` (creates the tables,
then exits) and `api` (REST API and web interface). The first run downloads the images and builds the API,
which takes a few minutes.

| Command | Effect |
|---|---|
| `Ctrl+C` or `docker compose down` | Stop the system; the data is kept |
| `docker compose down -v` | Stop and delete the database |
| `docker compose up -d postgres` | Start only the database, e.g. for `dotnet run` and the tests |

Ports 8080 and 5432 must be free on the computer.

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
