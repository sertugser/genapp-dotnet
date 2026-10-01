# Local CICS/VSAM/Db2 runtime for GenApp base — plan & progress log

## Scope note — what this will and will not give us

This produces a **text/batch-driven backend harness**: the real `base/src/*.cbl` business
and data-access programs, running end-to-end against a real local PostgreSQL + indexed
files, exercised by our own small driver programs (fill COMMAREA, `CALL` in, check the
result) — no 3270 screens (we deliberately skip `SEND MAP`/`RECEIVE MAP`, see scope analysis
below). **It is not a frontend and not "the app running as a whole" in the UI sense** — there
is no browser, no REST API, nothing to click. That's a separate, later, not-yet-started
deliverable: `src/GenApp.Api` (REST endpoints + simple web UI, per `src/README.md`). This
harness is groundwork/input for that work (you need to know the COBOL's real behaviour before
you can reimplement it correctly in C#), not a replacement for it.

## Reproducibility — how teammates get this without redoing the manual work

Decided **2026-10-01**: commit a **setup script** (`tests/local-runtime/setup.ps1`) that
automates the install, rather than Docker — Docker isn't installed on this machine and
pulling it in now (WSL2 backend, likely a restart) would block progress for a reproducibility
nice-to-have. Revisit Docker once the harness actually works end-to-end (Step 4) — `base/`'s
own root README already lists Docker in the tech stack, so this is a deferral, not a
rejection.

## Goal

Run the **unmodified** COBOL business + data-access layers in `base/src/` end-to-end on a
local Windows machine (no mainframe, no CICS TS license), by replacing CICS and Db2 calls
with a thin local stub/runtime — not by rewriting the 31 original programs. This is the
"selected programs are run in GnuCOBOL, with CICS and Db2 calls replaced by stubs" approach
already named in the root `README.md` and `tests/README.md`, just scoped to cover all 18
operations instead of one-off spot checks.

`base/` stays byte-for-byte unchanged (per the root README: "Original GenApp COBOL
application ... (unchanged)"). All new code (stub runtime, batch drivers, build scripts)
lives outside `base/`, in `tests/local-runtime/` (settled 2026-10-01 — that's where
`setup.ps1` already lives), never inside `base/src/`.

## Why this approach (decision log, so we don't re-research)

Explored and rejected/parked, in order:

| Option | Verdict | Reason |
|---|---|---|
| IBM Z Xplore | Parked | Free, real live IBM Z access, but all evidence points to a shared, guided/badge-based sandbox. No confirmation it lets you submit your own JCL / deploy this exact repo. |
| Rocket Software Enterprise Developer (full CICS+VSAM+Db2+JCL support, confirmed) | Parked | "Get Started" → routes to **"Talk to an expert"**, no self-service trial/download or pricing. Sales-gated, timeline unknown. |
| Rocket COBOL Academic Program | Not sufficient | Free, but (a) requires the **university/professor** to register, not the student, and (b) only grants **Visual COBOL Personal Edition**, which has no confirmed CICS/Enterprise Server support. |
| Raincode COBOL compiler | Compiler itself is free & self-serve | Compiles EXEC CICS/EXEC SQL syntax natively, no license fee. |
| Raincode QIX (actual CICS runtime) | Parked | Needs an activation key obtained by **emailing Raincode directly** (info@raincode.com) — not self-serve either. |
| OpenKicks (real CICS API impl., incl. BMS) | Deprioritized | Proprietary beta, access only via emailing the vendor, Windows support exists but not actively maintained/tested by them. |
| IBM zPDT / zD&T | Dead end | IBM ending it: end-of-sale 2025-12-31, end-of-support 2026-12-31. Was ISV-partner-only anyway. |
| Hercules + old MVS 3.8j (TK4-) | Dead end | Can't run the CICS TS version GenApp needs; 1970s-era target OS. |
| **GnuCOBOL + our own CICS/VSAM stub + GixSQL for SQL** | **Chosen** | Zero registration, zero cost, fully local, no waiting on any vendor. Scope turned out to be bounded once we actually counted the CICS/SQL verbs in use (see below) — not "build a CICS emulator from scratch". |

Key enabler found: **GixSQL** (https://github.com/mridoni/gixsql) — free, open-source ESQL
preprocessor + runtime for GnuCOBOL, talks to PostgreSQL (our actual target DB per the
migration goal) directly. This removes the hardest unknown (Db2/SQL access) for free.

## Scope analysis (grounded — counted directly from base/src, not guessed)

- 31 COBOL programs total in `base/src/`.
- Distinct `EXEC CICS` verbs found across all of them (case-insensitive scan): **ABEND,
  ASKTIME, ASSIGN, DEFINE, DELETE, DELETEQ, DEQ, ENQ, FORMATTIME, GET, HANDLE, LINK, QUERY,
  READ, READQ, RECEIVE, REWRITE, SEND, START, SYNCPOINT, WRITE, WRITEQ** (23 verbs).
- Distinct `EXEC SQL` verbs: **CLOSE, DECLARE, DELETE, FETCH, INCLUDE, INSERT, OPEN, SELECT,
  SET, UPDATE** (10 verbs, standard cursor-based embedded SQL — GixSQL handles all of these).
- Of the 23 CICS verbs, only **~11 matter for running business logic end-to-end**:
  `LINK, RETURN, ABEND, ASKTIME, FORMATTIME, GET (COUNTER), READ, WRITE, REWRITE, DELETE,
  HANDLE CONDITION`. The rest (`SEND`/`RECEIVE` MAP, `HANDLE AID`, `ASSIGN`, `ENQ`/`DEQ`,
  `READQ`/`WRITEQ`/`DELETEQ`, `SYNCPOINT`, `QUERY`, `DEFINE`, `START`) belong to the 3270/BMS
  screen layer, the statistics/logging side-channel (`lgstsq.cbl`, the `GENACNTL` TSQ counter
  in `lgtestc1.cbl`), or the already-out-of-scope `lgwebst5.cbl`/`lgsetup.cbl` support
  programs — we don't need to stub these to run the 18 business operations.
- VSAM access (`READ`/`WRITE`/`REWRITE`/`DELETE FILE(...)`) maps directly onto GnuCOBOL's
  **native `ORGANIZATION IS INDEXED` file support** — no extra tooling, just define
  `KSDSCUST`/`KSDSPOLY` as indexed files and seed them from `base/data/ksdscust.txt` and
  `base/data/ksdspoly.txt`.
- Special case — **`GET COUNTER`** (used in `lgacdb01.cbl`, `lgastat1.cbl`): the root
  README already documents that GenApp's own fallback (in `LGACDB01`) is to use the DB2
  identity column when the named counter is unavailable (`LGAC-NCS` flag). So our stub
  doesn't need to implement a real counter service — just return a non-`NORMAL` `RESP` and
  the existing program logic takes the DB2 path automatically. Confirmed by reading
  `lgacdb01.cbl` lines 199–211.
- The 3270 menu/test programs (`lgtestc1.cbl`, `lgtestp1.cbl`..`lgtestp4.cbl`) won't be
  reused as-is (they need `SEND MAP`/`RECEIVE MAP`/`HANDLE AID`). Instead we write our own
  small batch "driver" programs per operation that fill the shared `COMM-AREA` (layout in
  `base/src/lgcmarea.cpy`) directly and `CALL`/`LINK` into the business programs — same
  pattern `lgtestc1.cbl` already uses (e.g. lines 86–146 for customer add/inquire), just
  without the terminal I/O.

### Program inventory (for reference while wiring the stub)

- **Business layer (7):** `lgacus01` (Add Customer), `lgicus01` (Inquire Customer),
  `lgucus01` (Update Customer), `lgapol01` (Add Policy), `lgipol01` (Inquire Policy),
  `lgdpol01` (Delete Policy), `lgupol01` (Update Policy — motor/house/endowment only).
  Policy programs are generic across motor/house/endowment/commercial via a type field in
  the COMMAREA (copybooks `lgpolicy.cpy`, `polloo2.cpy`, `pollook.cpy`) — confirm exact
  discriminator when we get to the policy operations.
- **Data-access layer (15):** `lgac{db01,db02,vs01}` / `lgic{db01,vs01}` /
  `lguc{db01,vs01}` (customer, DB2 + VSAM variants) and `lgap{db01,vs01}` /
  `lgip{db01,vs01}` / `lgdp{db01,vs01}` / `lgup{db01,vs01}` (policy).
- **Support (4):** `lgastat1` (statistics), `lgsetup`, `lgstsq` (TSQ helper, linked from
  error-handling paragraphs), `lgwebst5` (web services — out of scope per root README).
- **Test/menu (5):** `lgtestc1`, `lgtestp1`..`lgtestp4` — reference for COMMAREA usage,
  not reused directly (see above).

### The 18 target operations (from root README, our "done" checklist later)

Customer: Add, Inquire, Update.
Policy × {Motor, House, Endowment, Commercial}: Add, Inquire, Delete.
Policy Update × {Motor, House, Endowment} (no commercial update, no customer delete — matches
original app).

## Step plan

- [x] **Step 0 — Install GnuCOBOL** on Windows (`cobc` on PATH). Verify with a trivial
      "hello world" compile. **Done 2026-10-01**, now scripted and committed as
      `tests/local-runtime/setup.ps1` (idempotent — safe to re-run) — see progress log.
- [ ] **Step 1 — Install GixSQL** (standalone `gixpp` preprocessor, no need for full Gix-IDE)
      and a local **PostgreSQL** instance. Derive the 8 Db2 table schemas from the `EXEC SQL`
      statements in the `*db01`/`*db02` programs (this doubles as input for
      `legacy-analysis/data-dictionary.md` later) and create them in Postgres.
- [ ] **Step 2 — Define VSAM-equivalent indexed files** (`KSDSCUST`, `KSDSPOLY`) for
      GnuCOBOL and seed them from `base/data/ksdscust.txt` / `ksdspoly.txt`.
- [ ] **Step 3 — Build the CICS stub layer**: a small preprocessing pass that rewrites the
      ~11 needed `EXEC CICS ... END-EXEC` patterns into `CALL`s, plus a hand-written COBOL
      "CICS stub" subprogram implementing them (LINK → native CALL; file verbs → native
      INDEXED I/O; ASKTIME/FORMATTIME → COBOL intrinsics; GET COUNTER → always-fail RESP;
      ABEND/RETURN/HANDLE CONDITION → structural equivalents). Goes in
      `tests/local-runtime/`, next to `setup.ps1`.
- [ ] **Step 4 — Proof of concept**: get **Add Customer** running end-to-end — compile/link
      `lgacus01` + `lgacdb01` + `lgacdb02` + `lgacvs01` + `lgstsq` + stub layer, drive it with
      a tiny batch program, confirm a row lands in Postgres **and** the indexed VSAM-equivalent
      file. This validates the whole pipeline before investing in the rest.
- [ ] **Step 5 — Extend to the remaining 17 operations**, reusing the Step 3 infrastructure.
- [ ] **Step 6 (stretch) — Wire into `tests/GenApp.EquivalenceTests`** per the project's own
      testing plan (return codes, optimistic locking via LASTCHANGED, input checks).

Parked in parallel, not blocking: the Rocket Enterprise Developer "talk to an expert"
request can still be submitted and left to simmer — if it ever comes through, it's a
higher-fidelity (but unnecessary now) alternative.

## Progress log

- **2026-10-01** — Researched local CICS runtime options, ruled out all vendor paths for
  now (see decision log above), found GixSQL, counted the actual CICS/SQL verb scope, wrote
  this plan. Nothing installed yet — Step 0 is next.
- **2026-10-01** — **Step 0 done.** Installed GnuCOBOL 3.2.0 via MSYS2 (not a standalone
  installer — cleaner/scriptable):
  1. `winget install --id MSYS2.MSYS2 -e` → installs to `C:\msys64`.
  2. `pacman -Syu` (core update, needs to run twice — it kills/restarts the shell after
     updating `bash`/`msys2-runtime` itself, that's expected).
  3. `pacman -S mingw-w64-ucrt-x86_64-gnucobol` — **note:** the classic
     `mingw-w64-x86_64-gnucobol` (plain `mingw64` repo) package no longer exists in current
     MSYS2; it's under the `ucrt64` repo now. Installs to `C:\msys64\ucrt64\bin\cobc.exe`.
  4. **Gotcha:** `cobc.exe` run directly (outside an MSYS2 shell) can't find its own config
     and fails with `configuration error: .../config\default.conf: No such file or
     directory`, and separately needs `gcc` (also in `ucrt64\bin`) on `PATH` to actually link
     the executable. Fixed by persisting two **User** environment variables (not system-wide,
     no admin needed): `PATH` += `C:\msys64\ucrt64\bin`, and
     `COB_CONFIG_DIR=C:\msys64\ucrt64\share\gnucobol\config`. New terminals pick these up
     automatically; only needed to export them manually inside this session's own shell.
  5. Verified: compiled and ran a trivial `DISPLAY "GnuCOBOL OK".` program successfully.
  - **Not yet done:** compiling any real `base/src/*.cbl` file — those will fail immediately
    on the `EXEC CICS`/`EXEC SQL` statements until Step 3's stub/translator exists. That's
    expected, not a regression.
- **2026-10-01** — Wrote and tested `tests/local-runtime/setup.ps1`, scripting all of Step 0
  (idempotent — re-ran it after the manual install and it correctly skipped already-done
  steps, then passed the smoke test). Decided against Docker for now (see "Reproducibility"
  above) — plain PowerShell script instead, so teammates don't need anything beyond winget.
  This is what gets committed so a fresh clone doesn't repeat our manual trial-and-error.
