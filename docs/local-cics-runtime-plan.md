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
- [x] **Step 1 — SQL bridge to PostgreSQL** — **done, but not via GixSQL's own runtime**
      (that part is upstream-broken, see progress log). Ended up writing our own ~250-line
      libpq-backed replacement for the handful of GixSQL runtime functions GenApp's SQL
      actually needs (`tests/local-runtime/sql-stub/genapp_sqlstub.cpp`), reusing only
      `gixpp` (the preprocessor, which works fine) to translate `EXEC SQL` into `CALL`
      statements. Confirmed working end-to-end: connect, DDL, parameterized INSERT, cursor
      declare/open/fetch, and all three COBOL host-variable types GenApp's SQL actually uses
      (alphanumeric, COMP binary int, and — for the vendor test program used to validate
      this, not needed by GenApp itself — COMP-3 packed decimal and zoned-display numeric).
      Run `tests/local-runtime/setup-sql-bridge.ps1` to set this up (needs `setup.ps1` run
      first). **Not supported, and not needed:** `UPDATE ... WHERE CURRENT OF` on a cursor —
      GenApp never holds a cursor open across its inquire/update COMMAREA round-trips (those
      are separate CICS transactions), it always does plain `UPDATE ... WHERE key = :x`
      instead, so this was deliberately not implemented. If that assumption turns out wrong
      once we preprocess the update programs, revisit `GIXSQLCursorOpen` in the stub (would
      need a real server-side `DECLARE ... CURSOR` + `BEGIN`/transaction instead of the
      current "run the query fully, buffer rows client-side" emulation).
- [x] **Step 2 — VSAM-equivalent storage** — **done**, folded into Step 3's stub (same file
      owns both, see below) rather than using GnuCOBOL's native `ORGANIZATION IS INDEXED`
      (the original idea) — an in-memory `std::map` keyed by record key, persisted to a flat
      file, turned out simpler and sidesteps picking/configuring a GnuCOBOL indexed-file
      backend (BDB/VBISAM/CISAM) sight unseen on a platform where today's track record with
      "sounds simple, turns out to have a Windows-specific gotcha" is 100%.
- [x] **Step 3 — CICS stub layer** — **done for the core path (WRITE), implemented but not
      yet individually exercised for READ/REWRITE/DELETE/GET-COUNTER/multi-hop LINK chains.**
      Turned out to split cleanly into two pieces instead of one:
      - `tests/local-runtime/cics-stub/preprocess_cics.py` — a **syntactic** translator
        (Python, not a runtime library) for LINK, RETURN, ABEND, ASKTIME/FORMATTIME, and
        GET COUNTER — these don't need a runtime call at all, they rewrite directly to plain
        COBOL (`CALL`, `GOBACK`, `FUNCTION CURRENT-DATE`, a forced bad `RESP`). Only the file
        verbs (READ/WRITE/REWRITE/DELETE) call into a runtime stub, because those genuinely
        need persistent state.
      - `tests/local-runtime/cics-stub/genapp_vsam_stub.cpp` — the runtime stub, exactly four
        functions (`VSAMREAD`/`WRITE`/`REWRITE`/`DELETE`), covering both `KSDSCUST` and
        `KSDSPOLY` by name. `GTEQ`/`GENERIC` browse reads (used by the policy-inquiry path)
        map directly onto `std::map::lower_bound` — no separate browse/cursor API needed.
      - Record layouts (`KSDSCUST` 225 bytes/10-byte key, `KSDSPOLY` 64 bytes/21-byte key)
        were read off the real `EXEC CICS WRITE` calls in `lgacvs01.cbl`/`lgapvs01.cbl`, not
        guessed — see progress log for the exact breakdown.
      - Validated end-to-end against the **real, unmodified** `lgacvs01.cbl` (Add Customer
        VSAM): preprocessed, compiled, linked, run, and the written record read back
        byte-for-byte correct. Two non-obvious COBOL bugs had to be found and fixed to get
        there — see progress log, worth reading before touching this again.
      - **Not yet exercised:** REWRITE, DELETE, GTEQ/GENERIC READ, and GET COUNTER's
        forced-fallback path are implemented the same way as WRITE but haven't each been run
        against a real program yet. `HANDLE CONDITION` was never seen in the data-access
        layer (only in the 3270 menu programs, which aren't used) so the preprocessor doesn't
        handle it — would need to if a future program turns out to need it (comments it out
        with a `FIXME` marker and keeps going, rather than silently doing the wrong thing).
- [x] **Step 4 — Proof of concept: DONE.** "Add Customer" runs fully end-to-end through the
      **real, unmodified** `lgacus01.cbl` → `lgacdb01.cbl` → (`lgacdb02.cbl` + `lgacvs01.cbl`)
      chain, via real `EXEC CICS LINK` calls (preprocessed, not bypassed), ending with
      `CA-RETURN-CODE = 00` and correct, consistent data in all three stores (PostgreSQL
      `CUSTOMER`, PostgreSQL `CUSTOMER_SECURE`, and the `KSDSCUST` VSAM-equivalent file).
      Needed a handful of additional fixes beyond Steps 1–3's individual pieces — see
      progress log, several are the kind of thing that'll bite again in Step 5 if forgotten.
- [~] **Step 5 — Extend to the remaining operations** — **6 of 18 done** (Customer
      Inquire/Add/Update, Motor Policy Add/Inquire/Delete — all via `genapp_menu.cbl`,
      options 1-6 — see progress log), **12 to go** (Motor Update +
      House/Endowment/Commercial Add/Inquire/Delete/Update-where-applicable). Reuses the
      Step 3/4 infrastructure (build recipe: `gixpp` on anything with `EXEC SQL`, then
      `preprocess_cics.py` on everything, in that order; link with both stub `.o`s). Known
      rough edges to expect for the remaining 14: `REWRITE` is now proven (Customer Update),
      but `DELETE`/`GTEQ`/`GENERIC` `READ` are implemented and still not individually
      exercised — policy delete and the "all policies for a customer" inquiry will be the
      first real test; `HANDLE CONDITION` isn't translated at all (not seen in the
      data-access layer so far — the preprocessor fails loudly with a `FIXME` comment if a
      policy program turns out to need it, not silently misfires); a new runtime function
      turned up per new SQL shape so far (`GIXSQLExecSelectIntoOne` for Inquire's
      non-cursor `SELECT...INTO`) — budget for the possibility that a policy-specific SQL
      shape needs another one; and the `CUSTOMER`/`KSDSCUST` numbering mismatch (Step 4,
      point 4) will recur for policy numbers against `KSDSPOLY` unless addressed first.
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
- **2026-10-01** — **PostgreSQL 18 installed** via MSYS2 (`mingw-w64-ucrt-x86_64-postgresql`,
  not the EDB installer — that needs admin rights we don't have; MSYS2's build runs as a
  plain user process via `pg_ctl`, no Windows service). Data dir: `C:/Users/hasan/pgdata-genapp`,
  port 5432, superuser `postgres`/`postgres` (local dev only, not meant to be reachable
  outside this machine), database `genapp` created. Start with:
  `pg_ctl -D C:/Users/hasan/pgdata-genapp -l C:/Users/hasan/pgdata-genapp/server.log start`
  (needs `C:\msys64\ucrt64\bin` on PATH). Not yet scripted into `setup.ps1` — do that
  alongside Step 2.
- **2026-10-01** — **GixSQL: blocked on an upstream Windows/mingw/x64 bug, not our
  misconfiguration.** What happened, in order:
  1. Downloaded the official `gixsql-binaries-windows-x64-mingw-1.0.20b-1.7z` portable
     package. `gixpp` (the ESQL preprocessor) works fine on its own.
  2. Linking a preprocessed program against the bundled `libgixsql.a` fails with pages of
     undefined `fmt::v9::*` symbols. This is a **known, acknowledged upstream bug**:
     [mridoni/gixsql#167 "Missing libfmt.dll / libfmt.a from Windows binary releases"](https://github.com/mridoni/gixsql/issues/167).
     Worked around it ourselves: cloned `fmt` at tag `9.1.0` (the exact version GixSQL's
     symbols are mangled against — a different fmt major version will NOT link, the
     namespace is literally part of the mangled name) and built `libfmt.a` with CMake+Ninja
     (`mingw-w64-ucrt-x86_64-cmake`/`ninja`, both installed). Linking
     `-lgixsql -lfmt -lstdc++` (in that order — static-lib link order matters, dependencies
     must come *after* what needs them) then failed on a second, separate gap: undefined
     `std::codecvt<wchar_t, char, int>` members. This one isn't fmt's fault — it's a
     genuine gap in mingw-w64's shipped `libstdc++.a` (the generic `codecvt` primary
     template's virtual bodies for a non-`mbstate_t` state type are declared in headers but
     never compiled into the prebuilt archive; `std::filesystem::path`'s internal UTF
     conversion helper needs exactly this specialization). Worked around by hand-writing
     explicit member specializations with trivial (but *correct*, not stub/no-op) ASCII
     passthrough bodies — see `do_out`/`do_in` doing a real char↔wchar_t copy loop, not
     just returning `error`; an earlier no-op version caused a different crash
     ("Result too large") because the path-conversion code path turned out to be live, not
     dead, even for a plain `pgsql://` connection string. This got a test program all the
     way through **preprocessing → compiling → linking** successfully.
  3. At **runtime**, `EXEC SQL CONNECT` now reliably crashes
     (`terminate called after throwing an instance of 'std::system_error' — what(): Result
     too large`), reproduced with a minimal connect-only program (no cursors, no tables) so
     it's not query complexity. No GixSQL trace log is written even with
     `GIXSQL_LOG_LEVEL=trace`, meaning it crashes before GixSQL's own logging initializes —
     i.e. very early in `GIXSQLConnect`, likely while loading/initializing the dynamically-
     loaded `libgixsql-pgsql.dll` driver. This matches a second-hand report found while
     researching #167: a user saw "access violation (C0000005) when loading
     libgixsql-pgsql.dll" on Windows, possibly related to the same fmt mismatch — except in
     our case the *driver* DLL (not the main lib) may still be built against a mismatched
     fmt internally, which we have no way to fix short of rebuilding GixSQL from source
     entirely (attempted separately — see below — and also blocked).
  4. Side-quest, also blocked: tried building GixSQL from source via MSYS2 (the officially
     documented route, which would avoid the fmt version-pinning problem entirely by
     compiling against whatever fmt MSYS2 ships). `git clone` the repo at tag `v1.0.20b`,
     `autoreconf -fi` fails with `undefined or overquoted macro: AC_MSG_ERROR`/`AS_IF` —
     looks like an `aclocal`/macro-archive issue independent of autoconf version (tried both
     2.73 and the project's own pinned 2.69 via `WANT_AUTOCONF`, same failure both times).
     Did not dig further given time already spent — if revisited, start here.
  - **Net result:** GnuCOBOL ↔ PostgreSQL via GixSQL is not working yet, blocked on what
    looks like a genuine upstream defect, not a setup mistake. Options going forward (for
    next session to pick up): (a) try switching the connection string to GixSQL's **ODBC**
    driver instead of its native pgsql driver — `mingw-w64-ucrt-x86_64-unixodbc` is already
    installed, this is a completely different code path and might sidestep the bug; (b)
    keep pushing on the from-source build (fix the autoreconf issue); (c) park GixSQL and
    reconsider the SQL-bridge approach entirely (e.g. write a much smaller custom stub that
    only implements the handful of SQL operations GenApp's `*db01`/`*db02` programs actually
    use, talking to Postgres via plain `libpq` directly, skipping GixSQL's generality).
  - **Superseded by the next entry** — decided to stop fighting GixSQL's own runtime
    entirely rather than keep patching it. The `fmt`/codecvt work above turned out to be a
    dead end specifically *because* it was in service of linking against GixSQL's broken
    `libgixsql.a` — once that approach was abandoned (see below), none of it was needed
    any more. Left unlisted/not committed; `C:\msys64\ucrt64\opt\fmt-src\` can be deleted
    next time MSYS2 opt is cleaned up, it's not referenced by anything going forward.
- **2026-10-01** — **Step 1 actually finished**, via a different route: instead of fixing
  or replacing GixSQL's runtime, reimplemented just the runtime functions GenApp needs,
  backed directly by libpq, and kept using `gixpp` (the preprocessor) since that part was
  never broken.
  - **How the API was determined (not guessed):** preprocessed both a GixSQL vendor example
    (`TSQL037A-PGSQL.cbl`, bundled in the gixsql package under `examples/`) and two real
    GenApp programs (`lgacdb01.cbl`, `lgicdb01.cbl`) with `gixpp -e -S`, then read the
    generated `.cbsql` output directly to see the exact `CALL "GIXSQLxxx" USING ...`
    signatures gixpp actually emits — this is authoritative (it's what gixpp really
    generates), not a guess from GixSQL's docs/source. 12 distinct functions cover
    everything GenApp uses: `GIXSQLConnect`, `GIXSQLConnectReset`, `GIXSQLStartSQL`,
    `GIXSQLEndSQL`, `GIXSQLExec`, `GIXSQLExecParams`, `GIXSQLSetSQLParams`,
    `GIXSQLSetResultParams`, `GIXSQLCursorDeclare`, `GIXSQLCursorOpen`,
    `GIXSQLCursorFetchOne`, `GIXSQLCursorClose`. SQL text already comes out with
    PostgreSQL-native `$1,$2,...` placeholders (gixpp's default `-z d` mode), so it can be
    hand straight to `PQexecParams` with no rewriting.
  - **Host variable type codes** (the `type` argument to `SetSQLParams`/`SetResultParams`),
    also read off real generated output, not guessed: **16** = alphanumeric (`PIC X`,
    space-padded), **23** = binary `COMP` integer. Confirmed by preprocessing
    `lgacdb01.cbl`'s real INSERT (customer first/last name etc. → 16; the `COMP`-converted
    customer number → 23) and `lgicdb01.cbl`'s real SELECT (same pattern for the WHERE-clause
    key). GenApp never uses zoned-display numeric (`PIC 9(n)` DISPLAY) or packed-decimal
    (`COMP-3`) as SQL host variables — every numeric key gets explicitly `MOVE`d into a
    `COMP` field first (see `lgacdb01.cbl`'s `DB2-CUSTOMERNUM-INT`). Types 1 (zoned) and 9
    (COMP-3) are still implemented in the stub, because the *vendor test program* used to
    validate all this needs them — just confirmed to be dead code for GenApp itself.
  - **`COMP` is big-endian**, confirmed empirically (not assumed) with a throwaway probe
    program (`01 N PIC S9(9) COMP VALUE 1`, dumped to a file, inspected with `od`) — GnuCOBOL
    defaults to mainframe-compatible byte order even on a little-endian x86 host.
  - **COMP-3 packing gotcha worth remembering if touched again:** the sign nibble is always
    the *low* nibble of the *last* byte; digit nibbles are the N digits immediately before
    it; if N is even there's one leading zero pad-nibble at the very front. Indexing nibble
    positions directly (not byte-pair-at-a-time) is the only way to get both parities right
    — an earlier byte-pair-at-a-time version silently misread the sign nibble as a digit for
    even N and produced garbage (`"101<"` instead of `"101"`) before this was caught by the
    vendor test program actually failing.
  - Validated end-to-end against `TSQL037A-PGSQL.cbl` (DDL, 10 parameterized INSERTs, cursor
    DECLARE/OPEN/FETCH reading real rows back correctly) — everything passed except the
    `WHERE CURRENT OF` update (see Step 1 checklist entry above for why that's fine to skip).
  - **Not yet done:** running this against a full real GenApp program end-to-end — can't,
    yet, because `lgacdb01.cbl` also calls `EXEC CICS LINK` (to `LGACVS01`/`LGACDB02`), which
    needs Step 3's CICS stub first. The SQL half is proven ready for that integration.
  - Committed: `tests/local-runtime/sql-stub/genapp_sqlstub.cpp`,
    `tests/local-runtime/copy/SQLCA.cpy` (standard SQLCA layout, hand-written — no official
    copy was bundled with gixpp despite its docs claiming one exists), and
    `tests/local-runtime/setup-sql-bridge.ps1` (installs Postgres + gixpp + compiles the
    stub; idempotent like `setup.ps1`).
- **2026-10-01** — **Steps 2+3: VSAM/CICS stub, validated against the real `lgacvs01.cbl`.**
  - **Record layouts**, read off real `EXEC CICS WRITE` calls, not guessed:
    `KSDSCUST` = `CA-CUSTOMER-NUM`(10) + `CA-FIRST-NAME`(10) + `CA-LAST-NAME`(20) +
    `CA-DOB`(10) + `CA-HOUSE-NAME`(20) + `CA-HOUSE-NUM`(4) + `CA-POSTCODE`(8) +
    `CA-NUM-POLICIES`(3) + `CA-PHONE-MOBILE`(20) + `CA-PHONE-HOME`(20) +
    `CA-EMAIL-ADDRESS`(100) = 225 bytes, first 10 = key (from `lgacvs01.cbl`'s
    `WRITE FILE('KSDSCUST') FROM(CA-Customer-Num) LENGTH(225)` — `FROM` starts at
    `CA-CUSTOMER-NUM`, and group items are contiguous in memory, so reading forward 225
    bytes from there spans exactly those fields). `KSDSPOLY` = request-id(1) +
    customer-num(10) + policy-num(10) = 21-byte key, + 43 bytes of policy-type-specific data
    (`REDEFINES`d per type in `lgapvs01.cbl`) = 64 bytes total. Confirmed both against
    `base/data/README.md`'s stated `LRECL=225`/`LRECL=64`.
  - **`base/data/*.txt` seed files are CRLF-separated 225/64-byte lines, not raw
    concatenated fixed-length records** — a plain `cp` into the working VSAM directory
    misaligns every record after the first against the stub's "pure fixed-length, no
    separator" reader. `setup-cics-stub.ps1` strips line endings when seeding instead of
    copying directly.
  - **Two non-obvious COBOL/GnuCOBOL bugs found via a real crash, not anticipated in
    advance** — both worth remembering if this is ever extended:
    1. **The `BY VALUE`/`BY REFERENCE` phrase in `CALL ... USING` sticks to every following
       operand until the next explicit `BY` phrase — it does not revert to `BY REFERENCE`
       by default.** `CALL "X" USING a BY VALUE b c` passes `c` the same way as `b` (by
       value), not by reference — and for an alphanumeric item that's nonsensical, so
       GnuCOBOL silently downgrades it to `BY CONTENT` instead (a different calling
       convention than our stub's plain-pointer `extern "C"` functions expect) with only a
       warning (`BY CONTENT assumed for alphanumeric item ...`), not an error. The effect
       was a native crash ("attempt to reference invalid memory address") inside the stub,
       several calls away from the actual mistake. Fix: every reference-mode argument in a
       generated `CALL` now says `BY REFERENCE` explicitly, matching what gixpp's own
       generated code already does for the SQL bridge (which is exactly why that one never
       hit this — the pattern was there to copy, just not recognized as load-bearing until
       this).
    2. **`BY VALUE` of a COBOL identifier passes that item's own native storage size (e.g. 2
       bytes for a typical `PIC S9(4) BINARY` length field), not a C `int`-sized 4 bytes** —
       passing `CUSTOMER-RECORD-SIZE` (a real `PIC S9(4) BINARY` constant in `lgacvs01.cbl`)
       straight through as `BY VALUE` corrupted every argument after it on the call stack.
       Bare integer *literals* don't have this problem (confirmed fine throughout the Step 1
       SQL bridge — gixpp only ever emits `BY VALUE <literal>`, never `BY VALUE <arbitrary
       identifier>`, which in hindsight was a second hint). Fix: `preprocess_cics.py`'s
       `by_value_numeric()` leaves literals alone but `MOVE`s any identifier into one of two
       fixed, purpose-declared `PIC 9(8) COMP-5` temps (`WS-GIX-LEN1`/`WS-GIX-LEN2`, native
       binary, sized to match an `int32_t` exactly) before passing that instead.
    3. (Not a bug in the usual sense, but caused the same crash symptom and took longest to
       find): **`PROCEDURE DIVISION.` with no `USING DFHCOMMAREA` clause compiles fine but
       leaves `DFHCOMMAREA` in the `LINKAGE SECTION` disconnected from the caller's
       argument** — every real GenApp program is written this way (real CICS wires
       `DFHCOMMAREA` up implicitly, no `USING` needed), so every reference to a COMMAREA
       field reads uninitialized memory unless this is patched. `preprocess_cics.py` now
       rewrites `PROCEDURE DIVISION.` to `PROCEDURE DIVISION USING DFHCOMMAREA.` whenever the
       `LINKAGE SECTION` declares `01 DFHCOMMAREA` — necessary for essentially every program
       in `base/src/`, not just this one.
  - Proof: a small driver program (`CALL "LGACVS01" USING COMM-AREA` with a filled-in test
    customer) ran against the **unmodified** `lgacvs01.cbl`, through
    `preprocess_cics.py` → `cobc` → linked with `genapp_vsam_stub.o`, and the customer record
    landed in the working `KSDSCUST.dat` byte-for-byte as expected, confirmed by reading the
    raw bytes back.
  - Committed: `tests/local-runtime/cics-stub/{genapp_vsam_stub.cpp,preprocess_cics.py}`,
    `tests/local-runtime/setup-cics-stub.ps1`. The working VSAM data directory
    (`tests/local-runtime/vsam-data/`) is gitignored — it's a mutable working copy seeded
    from `base/data/`, not source.
- **2026-10-01** — **Step 4: "Add Customer" proven end-to-end** through the unmodified
  `lgacus01.cbl` → `lgacdb01.cbl` → `lgacdb02.cbl` + `lgacvs01.cbl` chain. Build recipe: run
  `gixpp -e -S` on the two programs with `EXEC SQL` (`lgacdb01`, `lgacdb02`) first, then
  `preprocess_cics.py` on **all four** (order matters — gixpp leaves `EXEC CICS` alone, so
  it's always SQL pass then CICS pass, never the other way round), compile each as a module
  (`cobc -c`), and link them all together with a small driver program (fills the COMMAREA,
  `CALL "LGACUS01" USING COMM-AREA`, checks `CA-RETURN-CODE`) plus both stub `.o` files.
  Four more things had to be fixed to get from "the pieces work alone" to "the real chain
  works", none of them obvious in advance:
  1. **`preprocess_cics.py` wasn't preserving the trailing period** from the original
     `EXEC CICS ... END-EXEC.` — e.g. a translated `GOBACK` with no period silently merges
     into the *next* line as one unterminated COBOL sentence, which then fails to compile
     pointing at that next line (a paragraph header, usually), not at the real cause. Fixed
     by capturing `END-EXEC`'s trailing `.` (or its absence) in the regex and echoing it
     back after whatever the verb translates to.
  2. **Needed a trivial `LGSTSQ` stub** (`PROCEDURE DIVISION USING DUMMY. GOBACK.`) — every
     `WRITE-ERROR-MESSAGE` paragraph across `base/src` does `EXEC CICS LINK PROGRAM('LGSTSQ')
     COMMAREA(...)` to log to a TDQ we're not emulating. Dynamic `CALL "literal"` only
     resolves at runtime, so this doesn't block compiling/linking — it only matters if an
     error path actually gets hit, which turned out to be an excellent canary for bugs
     elsewhere (see next point: it's *how* the real bug below surfaced instead of as a silent
     wrong answer).
  3. **No program in `base/src` ever issues `EXEC SQL CONNECT`.** Real CICS+Db2 establishes
     the connection implicitly (`DB2CONN=YES` in the region's SIT overrides, via the Db2
     attachment facility) before the program ever runs — there is no mainframe equivalent of
     our driver issuing a connect. Without this, `GIXSQLExecParams` ran against a null
     `PGconn*`, which doesn't crash (libpq is defensive about it) but returns an error result
     with an **empty** message, which was its own small trap when first debugging this - the
     very first symptom was CA-RETURN-CODE 90 (SQL insert "failed") with no error text at
     all. Fixed by adding `ensure_connected()`, called at the top of every stub entry point
     that touches `g_conn` (`GIXSQLExec`, `GIXSQLExecParams`, `GIXSQLCursorOpen`), which
     connects lazily from the same `DATASRC`/`_USR`/`_PWD` env vars an explicit `CONNECT`
     would have used if `g_conn` isn't already live — mirrors the implicit-connection
     behaviour instead of requiring every driver to replicate it.
  4. **Not a code bug, a test-data coordination issue**: the fresh PostgreSQL `CUSTOMER`
     table's `IDENTITY` sequence starts at 1, and the seeded VSAM `KSDSCUST` working file
     already has customers numbered 1–10 (from `base/data/ksdscust.txt`) — so the very first
     auto-numbered insert collided with an existing VSAM record (`DUPREC` → `ABEND LGV0`,
     `CA-RETURN-CODE 80`). Nothing wrong with either store on its own; they just didn't agree
     on which numbers were taken. Worked around for this proof of concept by restarting the
     Postgres sequence well clear of the seed range (`ALTER TABLE CUSTOMER ALTER COLUMN
     CUSTOMERNUMBER RESTART WITH 1000`) - **not a real fix**, just unblocks this test; Step 5
     should either seed `CUSTOMER` from the same sample data as `KSDSCUST`, or accept that
     local runs start numbering from a high watermark clear of the seed data.
  - Added a `GENAPP_SQL_DEBUG=1` env var to `genapp_sqlstub.cpp` (prints `SQLCODE` + the
    libpq error message to stderr) while chasing fix #3 above — kept it in, it's cheap and
    was genuinely the thing that cut through guessing fastest; worth reaching for first next
    time something fails silently.
  - End state, verified by querying both databases and reading the raw VSAM bytes back, not
    just trusting `CA-RETURN-CODE = 00`: customer 1000 (JOHN SMITH) present and byte/field
    -identical across PostgreSQL `CUSTOMER`, PostgreSQL `CUSTOMER_SECURE` (including the
    hardcoded default password hash from `lgacdb01.cbl`, confirming the `LGACDB02` leg of the
    chain really ran), and `KSDSCUST.dat`.
- **2026-10-01** — **Interactive text-mode menu** (`tests/local-runtime/examples/genapp_menu.cbl`)
  standing in for the `SSMAPC1` 3270 screen (`base/src/ssmap.bms`) until the real ASP.NET Core
  frontend exists — same menu options/field order, plain `DISPLAY`/`ACCEPT` instead of
  `EXEC CICS SEND MAP`/`RECEIVE MAP`. Explicitly **not** a 3270 protocol reproduction (that
  would mean writing a BMS compiler and a TN3270 server — a project on the order of
  everything above, not a quick add-on); the user chose this scope explicitly over that one.
  Extended the proof of concept from just Add to all **three Customer operations**:
  - **Inquire** (`lgicus01` → `lgicdb01`): first time a plain (non-cursor)
    `SELECT col1,...,colN INTO :h1,...,:hN FROM t WHERE k=:p` was hit. gixpp compiles this to
    a call to **`GIXSQLExecSelectIntoOne`**, a function distinct from `GIXSQLExecParams` that
    hadn't shown up yet (Step 1's INSERT and Step 4's connect path never needed it). Added it
    to `genapp_sqlstub.cpp`: bind + execute like `ExecParams`, then fetch the first row
    straight into the registered result params (no cursor), mapping zero rows to `SQLCODE
    100` — GenApp's own "not found" convention, checked the same way throughout `base/src`.
  - **Update** (`lgucus01` → `lgucdb01` SQL `UPDATE ... WHERE CUSTOMERNUMBER = :x`, then
    `lgucvs01` VSAM `READ ... UPDATE` + `REWRITE`): first real exercise of `REWRITE` and of a
    `READ` using the `UPDATE` option — both already implemented in Steps 2/3 but unexercised
    until now; no changes needed, they just worked.
  - Verified with a real interactive session (piped input standing in for typing, since this
    environment has no live terminal to type into): inquired customer 1000, added a new
    customer (1001, auto-numbered correctly), updated customer 1000's name/address, exited
    cleanly. Cross-checked the end state directly in PostgreSQL afterward, not just the
    program's own "success" messages.
  - Committed: `tests/local-runtime/examples/genapp_menu.cbl`, plus the
    `GIXSQLExecSelectIntoOne` addition to `tests/local-runtime/sql-stub/genapp_sqlstub.cpp`.
- **2026-10-01** — **First policy operation: Motor Add** (`lgapol01` → `lgapdb01` →
  `lgapvs01`), proven the same way as Customer. Three more fixes needed, none seen on the
  Customer side:
  1. **`lgapdb01.cbl` has a pre-existing case-mismatch bug in its own source**:
     `03 DB2-M-PREMIUM-int PIC S9(9) COMP.` is declared lowercase, but referenced as
     `:DB2-M-PREMIUM-INT` (uppercase) in the `EXEC SQL INSERT`. Real COBOL compilers are
     case-insensitive about identifiers, so this has presumably always worked on a real
     mainframe - but gixpp's host-variable resolution is case-*sensitive* and fails to
     compile with `Cannot find host variable`. **`base/src/lgapdb01.cbl` was not touched** -
     fixed by normalizing the case only in the disposable working copy used for
     preprocessing (same spirit as the preprocessing step itself - base/ stays byte-for-byte
     original, only build-time copies are transformed).
  2. **Db2 dialect SQL that isn't valid PostgreSQL**: `CURRENT TIMESTAMP` (two keywords,
     Db2's "special register" syntax) fails in PostgreSQL, which wants `CURRENT_TIMESTAMP`
     (one token). Added a small `translate_db2_sql()` pass in `genapp_sqlstub.cpp`, applied
     to every statement right before it's sent to libpq (also covers `CURRENT DATE`/
     `CURRENT TIME` pre-emptively — same Db2 idiom, not yet hit but likely elsewhere in the
     policy programs). Separate from the existing `IDENTITY_VAL_LOCAL()` handling (that one
     needs its result routed into a host variable via `try_exec_set_assignment`, not just a
     text swap).
  3. **The type-23 (binary `COMP`) codec was silently wrong for anything smaller than
     `PIC S9(9)`.** Every type-23 field tested on the Customer side happened to be
     `PIC S9(9) COMP` (4 bytes), so a hardcoded 4-byte read/write never surfaced a problem.
     Motor's `DB2-M-CC-SINT` is `PIC S9(4) COMP` (GnuCOBOL's standard binary-size rule: 1-4
     digits -> 2 bytes, 5-9 -> 4 bytes, 10-18 -> 8 bytes) - gixpp reports this correctly via
     the `length` argument to `SetSQLParams`/`SetResultParams` (4 vs 9), which the codec just
     wasn't using. Reading 4 bytes from a field that only has 2 doesn't crash - it silently
     pulls in two bytes of whatever happens to follow in the record and produces a wrong but
     plausible-looking number (confirmed the hard way: CC came back as 104857600 instead of
     1600 - exactly what you get from 1600 shifted left 16 bits, i.e. two extra high bytes
     read from neighbouring storage). **This means every type-23 result silently trusted
     before this fix is suspect** - worth spot-checking Customer's numeric fields again,
     though they're all `PIC S9(9)` so they were likely fine. Fixed by sizing the read/write
     from `length` (the digit count) via `comp_byte_size()`, matching GnuCOBOL's own rule,
     instead of assuming 4 bytes always.
  - Verified end-to-end: `CA-RETURN-CODE = 00`, correct data (including the now-fixed `CC`
    value) in both the `MOTOR`/`POLICY` PostgreSQL tables and the `KSDSPOLY` VSAM-equivalent
    file, confirmed by querying/reading each directly, not just trusting the return code.
- **2026-10-01** — Wrote and tested `tests/local-runtime/setup.ps1`, scripting all of Step 0
  (idempotent — re-ran it after the manual install and it correctly skipped already-done
  steps, then passed the smoke test). Decided against Docker for now (see "Reproducibility"
  above) — plain PowerShell script instead, so teammates don't need anything beyond winget.
  This is what gets committed so a fresh clone doesn't repeat our manual trial-and-error.
- **2026-10-02** — **`tests/local-runtime/run.bat`**: a single double-clickable entry point
  chaining `setup.ps1` → `setup-sql-bridge.ps1` → `setup-cics-stub.ps1` → `build-menu.ps1` →
  launches `genapp_menu.exe`, so a teammate doesn't have to type five separate commands just
  to see the menu run. Caught a real bug on first real-world use (not caught by my own
  testing, which always had the right `PATH` already set from earlier in the same shell):
  **`setup.ps1` persists `C:\msys64\ucrt64\bin` to the User registry PATH for *future*
  terminals, but a `cmd.exe` window that was already open before that persisted doesn't pick
  it up** (normal Windows behaviour - env var changes don't propagate to already-running
  processes). `genapp_menu.exe` needs that directory on `PATH` to even start (`libpq.dll` and
  the rest), so launching it from such a window fails to start at all - silently, often as a
  GUI popup rather than console text, so the batch script saw nothing wrong and happily
  printed "Done." right after. Fixed two ways: `run.bat` now sets `PATH` explicitly itself
  rather than trusting the inherited environment, and it now checks `genapp_menu.exe`'s own
  exit code too (previously only the four setup/build steps were checked - a crash in the
  menu program itself would have been silently reported as success).
- **2026-10-02** — **Motor Policy Inquire** (`lgipol01` → `lgipdb01`, menu option 5), the 5th
  of 18 operations. Two new gixpp limitations found, both fixed only in the disposable
  `tests/local-runtime/build/lgipdb01.cbl` copy (never in `base/src`):
  - gixpp's ESQL parser doesn't accept Db2's `Insensitive Scroll` cursor modifiers (used by
    `Cust_Cursor`/`Zip_Cursor`, both only for the Commercial-search paths this operation
    doesn't exercise) - stripped to plain `DECLARE x CURSOR FOR`. Also one real case-mismatch
    bug like the `lgapdb01.cbl` one from Step 4: `Zip_Cursor`'s WHERE clause references
    `:CA-B-POSTCODE` but the shared `LGCMAREA.cpy` declares it `CA-B-Postcode` - gixpp's
    host-var lookup is case-sensitive, real COBOL isn't. Fixing the cursor syntax alone was
    enough to let `cobc` compile `lgipdb01.cbl` cleanly (the `GET-MOTOR-DB2-INFO` path never
    opens either cursor); the `INCLUDE SQLCA`/business logic the other `GET-*-DB2-INFO`
    paragraphs need does exercise them, so both fixes are now in `build-menu.ps1` regardless.
  - **Bigger find: gixpp mishandles `col INDICATOR :ind` on `SELECT...INTO`.** It registers
    the indicator as an ordinary extra `GIXSQLSetResultParams` slot but never adds a matching
    column to the real SQL text it sends to the DB - so for `GET-MOTOR-DB2-INFO`'s indicator
    trio (`BROKERID`/`BROKERSREFERENCE`/`PAYMENT`), the runtime ended up with 18 registered
    result slots against only 15 real Postgres columns, silently binding every field after the
    first indicator to the wrong column. Root-caused by comparing `PQnfields()` against
    `g_results.size()` and finding they didn't match, then confirming the actual generated SQL
    literal (`SQ0005`) really did only have 15 columns - gixpp's embedded-SQL translation
    doesn't inject the synthetic indicator column a real Db2/CLI driver would. Fixed by
    rewriting each `col INDICATOR :ind` into a real extra selected column
    (`col, CASE WHEN col IS NULL THEN -1 ELSE 0 END`) plus a plain (non-INDICATOR) host var in
    the `INTO` list, in the build-dir copy - gixpp then has nothing left to mishandle, and
    `genapp_sqlstub.cpp` needed zero changes for NULL handling. `BROKERID`/`BROKERSREFERENCE`/
    `PAYMENT` use this exact pattern verbatim in `GET-ENDOW`/`HOUSE`/`MOTOR-DB2-INFO`, so the
    fix in `build-menu.ps1` is a global replace across the whole file - will already be in
    place when House/Endowment Inquire are tackled.
  - Hit fixed-format COBOL's column-72 truncation again (same class of bug as Step 4's
    `cobol_call()` line-wrapping): the generated `CASE WHEN BROKERSREFERENCE IS NULL THEN -1
    ELSE 0 END,` line was 75 characters, so gixpp silently truncated it mid-string on read,
    producing invalid SQL (`...ELSE 0 E PAYMENT...`, missing `ND ,`) that only showed up by
    inspecting the generated `SQ0005` literal, not from any compiler error. Fixed by wrapping
    that one line across two in the `build-menu.ps1` replacement text.
  - New runtime function needed: `GIXSQLCursorDeclareParams` (declare-with-host-variables
    variant of `GIXSQLCursorDeclare`, used by `Cust_Cursor`/`Zip_Cursor` even though this
    operation never opens them - they're compiled unconditionally so the link fails without
    it). Implemented in `genapp_sqlstub.cpp`: captures `g_params` at declare time (gixpp emits
    `GIXSQLSetSQLParams` immediately before it, same `StartSQL`/`EndSQL` block) into the new
    `CursorState.params`, and `GIXSQLCursorOpen` now runs `PQexecParams` instead of plain
    `PQexec` whenever a cursor has bound params.
  - Verified end-to-end through the real menu (option 3 to add a motor policy, option 5 to
    inquire it back): every field round-tripped correctly, including the three
    indicator-bearing ones (`BROKERID`/`BROKERSREFERENCE`/`PAYMENT`), proving the column
    realignment fix actually works and not just that it compiles.
- **2026-10-02** — **Motor Policy Delete** (`lgdpol01` → `lgdpdb01` → `lgdpvs01`, menu
  option 6), the 6th of 18 operations. No gixpp issues this time (plain single-row `DELETE`,
  no cursor, no indicator) - both new programs compiled against `base/src` completely
  unmodified. Two real bugs found instead, both in test infrastructure, not business logic:
  - **`lgdpdb01.cbl` only `DELETE`s `FROM POLICY`**, relying - like the real Db2 schema must -
    on `MOTOR`'s foreign key having `ON DELETE CASCADE`. `schema.sql`'s `MOTOR` table didn't
    have that (plain `REFERENCES POLICY(POLICYNUMBER)`, default `NO ACTION`), so the first
    delete attempt against a motor policy would have failed with a FK violation. Added
    `ON DELETE CASCADE` to the `CREATE TABLE`, plus a `DROP`/`ADD CONSTRAINT` pair so an
    already-existing `MOTOR` table (every dev machine that ran setup before today) gets fixed
    too - `CREATE TABLE IF NOT EXISTS` doesn't retroactively change a table that already
    exists.
  - **Found while re-running `schema.sql` to apply that fix: `ALTER TABLE ... RESTART WITH`
    is not safe to rerun once real data exists.** `setup-sql-bridge.ps1` runs `schema.sql`
    every time (by design, so schema changes reach already-set-up machines) - but
    `RESTART WITH 5000` unconditionally rewinds the sequence back to 5000 even if policies
    past 5000 already exist, so the next INSERT collides (`duplicate key value violates
    unique constraint "policy_pkey"`) with whatever real row is sitting at 5001. This is
    exactly what happened testing Motor Add immediately after the CASCADE fix - looked like a
    regression in Motor Add itself at first, wasn't. Fixed by replacing both `RESTART WITH`
    statements with `SELECT setval(..., GREATEST(floor, max(column)+1), false)`, which only
    ever moves a sequence forward - safe to rerun indefinitely, unlike `RESTART WITH`.
  - Verified end-to-end through the real menu: added a motor policy, deleted it (option 6),
    confirmed gone via Inquire (`CA-RETURN-CODE=01`), then deleted the *same* policy number
    again and confirmed `CA-RETURN-CODE=81` (VSAM `NOTFND`) rather than a silent success -
    proving the `KSDSPOLY` VSAM record was actually removed the first time, not just the
    Postgres row.
