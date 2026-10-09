# LGAPOL01 trial run (GnuCOBOL, stubbed CICS)

| Field | Value |
|---|---|
| Date | 2026-10-08 (H02) |
| Owner | Oğuz |
| Branch | `oguz/gnucobol-lgapol01` |
| GnuCOBOL | 3.2.0, MSYS2 ucrt64 toolchain |
| Status | **Trial. Not measured.** |

## Measurement status

This run checks that the stub method works. It is **not** part of the prediction accuracy figure.

- `tests/predictions/` on `main` holds only `01-customer-add.md`, `02-customer-inquire.md` and
  `03-customer-update.md` (checked on `origin/main` before the run). None is about LGAPOL01, so no
  prediction existed that this run could have influenced.
- Consequence: **the output below must not be used when a prediction for LGAPOL01 is written later.**
  The run was made before that prediction, so a prediction written now would be a copy of this result
  and would not count as a measurement. If LGAPOL01 is to be measured, it needs a different, not yet
  seen program path, or the team decides how to handle this.
- The results were not shared with anyone before the prediction rule was checked. Nothing on `main` was
  changed.

## What was changed (`diff work/lgapol01.cbl work/lgapol01-gc.cbl`)

| # | Original | GnuCOBOL version | Line in `lgapol01.cbl` |
|---|---|---|---|
| 1 | (EIB provided by CICS) | `EIBCALEN`, `EIBTRNID`, `EIBTRMID`, `EIBTASKN` declared in WORKING-STORAGE, `EXTERNAL` | 88-91, 98, 113 |
| 2 | `EXEC CICS ABEND ABCODE('LGCA') NODUMP` | `DISPLAY` + `GOBACK` | 101 |
| 3 | `EXEC CICS RETURN` (length too short) | `GOBACK` | 115 |
| 4 | `EXEC CICS LINK PROGRAM(LGAPDB01) COMMAREA LENGTH(32500)` | `CALL LGAPDB01 USING DFHCOMMAREA` | 121 |
| 5 | `EXEC CICS RETURN` (final) | `GOBACK` | 126 |
| 6 | `EXEC CICS ASKTIME` | `DISPLAY` | 140 |
| 7 | `EXEC CICS FORMATTIME` | `DISPLAY` | 142 |
| 8-10 | `EXEC CICS LINK PROGRAM('LGSTSQ')` (three times) | `CALL 'LGSTSQ' USING ... BY CONTENT LENGTH OF ...` | 149, 157, 163 |

All 9 `EXEC CICS` commands are replaced (`grep -c "^ *EXEC CICS" work/lgapol01-gc.cbl` prints `0`).
Changes beyond the list in the task, needed for the program to work:

- `PROCEDURE DIVISION USING DFHCOMMAREA`: without it the COMMAREA never reaches the program.
- `GOBACK` after the ABEND `DISPLAY`: a real ABEND does not return.
- `LGAPDB01` is called without the `LENGTH(32500)` value; the stub does not use it.
- `LGSTSQ` calls pass the message length as a second parameter (the stub needs it to print only the message).
- `EIBCALEN` is `COMP-5`, not `COMP` (see `README.md`, step 3).

The `PROCEDURE DIVISION` logic is otherwise unchanged. `WS-CALEN` stays `PIC S9(4) COMP`, so it should hold
`2500` instead of `32500` in the OK case (same truncation as in `README.md` step 3; not printed); it is only a debug field and is not used for any decision.

## Stubs and driver

- `stubs/lgapdb01.cbl`: prints `STUB LGAPDB01 reached` with `CA-REQUEST-ID` and `CA-RETURN-CODE`. Changes nothing.
- `stubs/lgstsq.cbl`: prints the message it receives.
- `work/drv-lgapol01.cbl`: sets `EIBTRNID=CGPL`, `EIBTRMID=T001`, `EIBTASKN=1`, builds the COMMAREA, calls
  LGAPOL01, prints `CA-RETURN-CODE` before and after. The return code is set to `99` before the call.

## Build

From `tests/gnucobol/`, with `export PATH=/c/msys64/ucrt64/bin:$PATH`:

```
cobc -m -I work -o build/LGAPOL01.dll work/lgapol01-gc.cbl
cobc -m -I work -o build/LGAPDB01.dll stubs/lgapdb01.cbl
cobc -m         -o build/LGSTSQ.dll   stubs/lgstsq.cbl
cobc -x -I work -o build/drv-lgapol01.exe work/drv-lgapol01.cbl
```

No errors and no warnings.

## Runs

`export COB_LIBRARY_PATH=build`, one process per case.

### 1. Short COMMAREA: 10 bytes (`EIBCALEN = 10`, required 28)

```
DRIVER case=SHORT EIBCALEN=+00010 return code before=99
DRIVER return code after =98
```

Return code **98**. LGAPOL01 set `00`, found 10 < 28, set `98` and returned (`GOBACK`). LGAPDB01 was not called.

### 2. Sufficient COMMAREA: 32500 bytes (`EIBCALEN = 32500`, `01AMOT`, customer 1, policy 1)

```
DRIVER case=OK EIBCALEN=+32500 return code before=99
STUB LGAPDB01 reached, CA-REQUEST-ID=01AMOT CA-RETURN-CODE=00
DRIVER return code after =00
```

Return code **00**. The `00` was written by LGAPOL01 itself (`MOVE '00' TO CA-RETURN-CODE`); the stub
does not touch the COMMAREA. It says nothing about the real LGAPDB01.

### 3. Extra: no COMMAREA (`EIBCALEN = 0`)

Not requested in the task. Added because the `ASKTIME`, `FORMATTIME`, `ABEND` and first `LGSTSQ`
replacements can only be reached this way.

```
DRIVER case=NONE EIBCALEN=+00000 return code before=99
STUB EXEC CICS ASKTIME ABSTIME(ABS-TIME)
STUB EXEC CICS FORMATTIME ABSTIME(ABS-TIME)
STUB LGSTSQ length=0000000045 message=[                LGAPOL01 NO COMMAREA RECEIVED]
STUB EXEC CICS ABEND ABCODE(LGCA)
DRIVER return code after =99
```

The program takes the "no COMMAREA" branch, writes the error message, and stops at the (stubbed) ABEND.
Return code stays `99` because `CA-RETURN-CODE` is set after this check. Date and time in the message
are blank, because ASKTIME/FORMATTIME are stubbed.

## Result

| Replacement | Executed |
|---|---|
| RETURN (short) | yes (run 1) |
| LINK LGAPDB01 | yes (run 2) |
| RETURN (final) | yes (run 2) |
| ASKTIME, FORMATTIME, ABEND | yes (run 3) |
| LINK LGSTSQ, first call (ERROR-MSG) | yes (run 3) |
| LINK LGSTSQ, second and third call (CA-ERROR-MSG) | **no**, compiled only. They sit inside `IF EIBCALEN > 0`, but `WRITE-ERROR-MESSAGE` is performed only when `EIBCALEN = 0`, so they cannot be reached. |

7 of 9 replaced commands were executed. The method works: LGAPOL01 compiles, links with the stubs, and
returns `98` and `00` for the short and the sufficient COMMAREA.

Two things the method does not cover: the behaviour of the real LGAPDB01 (database inserts), and any
`EXEC SQL`. Both are listed in `README.md` under "Limits".

## Problems met

- First build failed with exit code 1 and no message: the `gcc` from Git Bash (`/mingw64/bin`) was used
  instead of the MSYS2 one. `cobc -v` showed the `gcc` line; fixed by putting `/c/msys64/ucrt64/bin`
  first on `PATH`. Written into `README.md` (Setup).
- First `LGSTSQ` stub printed 99 bytes of the 45-byte message and showed garbage after it. Fixed by
  passing the length as a second parameter. The runs above are from the fixed build.
