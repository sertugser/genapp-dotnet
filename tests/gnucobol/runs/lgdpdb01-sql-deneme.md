# LGDPDB01 trial run: replacing EXEC SQL (GnuCOBOL, stubbed CICS and Db2)

| Field | Value |
|---|---|
| Date | 2026-10-08 (H02) |
| Owner | Oğuz |
| Branch | `oguz/gnucobol-sql-trial` |
| GnuCOBOL | 3.2.0, MSYS2 ucrt64 toolchain |
| Status | **Trial. Not measured.** |

## Why this run

`runs/lgapol01-deneme.md` tried the method on a program with `EXEC CICS` only. ADR 0004 left open how to
handle `EXEC SQL`. LGDPDB01 (delete policy, data layer) has 3 `EXEC SQL` and 10 `EXEC CICS` commands,
and is small.

## Measurement status

Not part of the prediction accuracy figure. `tests/predictions/` on `origin/main` holds only the customer
add, inquire and update predictions (`01`-`03`). None covers the policy delete program, so no prediction
existed that this run could have influenced. **Do not use this output when a prediction for LGDPDB01 is
written later** (same reason as in the LGAPOL01 trial).

## Original does not compile

`cobc -fsyntax-only -I work work/lgdpdb01.cbl`: **40 errors, 1 warning**. The warning is
`ignoring unknown directive: 'PROCESS SQL'` (line 1). The first errors are the two `EXEC SQL INCLUDE ... END-EXEC`
blocks (lines 90, 100); the rest follow from them (undefined `EIB*` fields, `EXEC CICS`, `SQLCODE`).

## What was changed (`diff work/lgdpdb01.cbl work/lgdpdb01-gc.cbl`)

| # | Original | GnuCOBOL version | Line |
|---|---|---|---|
| 1 | `PROCESS SQL` | removed | 1 |
| 2 | (EIB provided by CICS) | four `EIB*` fields in WORKING-STORAGE, `EXTERNAL` | 119-121, 131, 139, 143, 230-232 |
| 3 | `EXEC SQL INCLUDE SQLCA END-EXEC` | `COPY SQLCA.` (`stubs/sqlca.cpy`) | 90-92 |
| 4 | `EXEC SQL INCLUDE LGCMAREA END-EXEC` | `COPY LGCMAREA.` | 100-102 |
| 5 | `EXEC SQL DELETE FROM POLICY WHERE ...` | `DISPLAY` of the host variables + `CALL 'DB2STUB' USING SQLCA` | 189-194 |
| 6 | `EXEC CICS ABEND` | `DISPLAY` + `GOBACK` | 134 |
| 7 | `EXEC CICS RETURN` (x3) | `GOBACK` | 145, 175, 201 |
| 8 | `EXEC CICS LINK PROGRAM(LGDPVS01) ... LENGTH(32500)` | `CALL LGDPVS01 USING DFHCOMMAREA` | 168 |
| 9 | `EXEC CICS ASKTIME`, `FORMATTIME` | `DISPLAY` | 216, 218 |
| 10 | `EXEC CICS LINK PROGRAM('LGSTSQ')` (x3) | `CALL 'LGSTSQ' USING ... BY CONTENT LENGTH OF ...` | 225, 233, 239 |
| 11 | `PROCEDURE DIVISION.` | `PROCEDURE DIVISION USING DFHCOMMAREA.` | 108 |

All 3 `EXEC SQL` and all 10 `EXEC CICS` commands are replaced (`grep -c "^ *EXEC " work/lgdpdb01-gc.cbl` prints `0`).

## Stubs and driver

- `stubs/sqlca.cpy`: minimal SQLCA, same field names and types as the Db2 one, no `VALUE` clauses.
- `stubs/db2stub.cbl`: stands in for the SQL statement. Sets `SQLCODE` to the value in the external
  field `DB2STUB-SQLCODE`. **That value is chosen by the test case, not by Db2.**
- `stubs/lgdpvs01.cbl`: VSAM layer stub. Prints that it was reached.
- `stubs/lgstsq.cbl`: as in the LGAPOL01 trial.
- `work/drv-lgdpdb01.cbl`: sets the EIB fields, builds the COMMAREA (`01DMOT`, customer 1, policy 1,
  return code preset to `99`), sets `DB2STUB-SQLCODE`, calls the program.

## Build

From `tests/gnucobol/`, with `export PATH=/c/msys64/ucrt64/bin:$PATH`:

```
cobc -m -I work -I stubs -o build/LGDPDB01.dll work/lgdpdb01-gc.cbl
cobc -m          -I stubs -o build/DB2STUB.dll stubs/db2stub.cbl
cobc -m -I work           -o build/LGDPVS01.dll stubs/lgdpvs01.cbl
cobc -m                   -o build/LGSTSQ.dll   stubs/lgstsq.cbl
cobc -x -I work           -o build/drv-lgdpdb01.exe work/drv-lgdpdb01.cbl
```

No errors and no warnings.

## Runs

`export COB_LIBRARY_PATH=build`, one process per case. The stub SQLCODE is the test input.

| Case | Stub SQLCODE | EIBCALEN | Return code | Path |
|---|---|---|---|---|
| `DELOK` | 0 | 32500 | **00** | SQL stub called, then LGDPVS01 stub called |
| `DEL100` | 100 | 32500 | **90** | error message written, LGDPVS01 not called |
| `DELFAIL` | -911 | 32500 | **90** | same as `DEL100` |
| `BADID` (`01XXXX`) | not called | 32500 | **99** | no SQL, no LGDPVS01 |
| `SHORT` | not called | 10 | **98** | returns at the length check |

Output of `DEL100` (the last message is shortened):

```
DRIVER case=DEL100   EIBCALEN=+32500 stub SQLCODE=+0000000100 return code before=99
STUB EXEC SQL DELETE FROM POLICY CUSTOMERNUMBER=+000000001 POLICYNUMBER=+000000001
STUB DB2STUB sets SQLCODE=       100
STUB EXEC CICS ASKTIME ABSTIME(WS-ABSTIME)
STUB EXEC CICS FORMATTIME ABSTIME(WS-ABSTIME)
STUB LGSTSQ length=0000000087 message=[                LGDPDB01 CNUM=0000000001 PNUM=0000000001 DELETE POLICY   SQLCODE=+00100]
STUB LGSTSQ length=0000000099 message=[COMMAREA=01DMOT9000000000010000000001  ...]
DRIVER return code after =90
```

## What this shows

- The `EXEC SQL` replacement works: the program compiles, runs, and reacts to the SQLCODE the stub
  gives. The `00` in `DELOK` comes from LGDPDB01 itself (`MOVE '00'`); the LGDPVS01 stub changes nothing.
- **Comment and code disagree.** Lines 196-197 say "Treat SQLCODE 0 and SQLCODE 100 (record not found) as
  successful", but the code is `IF SQLCODE NOT EQUAL 0`. With SQLCODE 100 the program returns `90`
  (`DEL100`). This is what the code does with that input. Whether Db2 gives 100 when the row does not
  exist was **not** checked here: it is outside what this harness can see.
- Here the two `LGSTSQ` calls with `CA-ERROR-MSG` are reachable (the SQL error path performs
  `WRITE-ERROR-MESSAGE` with `EIBCALEN > 0`). The `ELSE` branch (`EIBCALEN` 91 and up) ran in `DEL100`
  and `DELFAIL`. The `IF EIBCALEN < 91` branch (`EIBCALEN` 28 to 90) was **not** run.
- The `EXEC CICS ABEND` replacement and the `EIBCALEN = 0` path were not run in this trial (same
  replacement as in the LGAPOL01 trial, where it ran).

## What this does not show

- The SQL statement text is not checked: a typo in the table or column name would not be found.
- Foreign keys: the original comment says the delete spreads to the policy type table through
  FOREIGN KEY. The stub does nothing of the kind.
- Output host variables (`SELECT ... INTO`, `FETCH`) were not tried. LGDPDB01 only has a `DELETE`.
- `SQLCODE` values other than 0, 100 and -911 were not tried. All non-zero values give the same result here.

## Problems met

- None in the build. The syntax check was clean on the first try after writing the files. All commands
  were run from the same shell with the ucrt64 toolchain first on `PATH` (see `README.md`, Setup).
