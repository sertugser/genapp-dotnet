# tests/gnucobol

Running GenApp COBOL programs in GnuCOBOL without CICS. This file has two parts: the **method**
(step by step, reusable for any other program) and the **first compile check** that led to it.

Folder layout:

| Folder | Content |
|---|---|
| `work/` | `lgapol01.cbl`, `lgcmarea.cpy`: unchanged copies of `base/src/`. `lgapol01-gc.cbl`: the GnuCOBOL version of LGAPOL01. `drv-lgapol01.cbl`: its driver. |
| `stubs/` | Fake replacements for the programs LGAPOL01 `LINK`s to (`lgapdb01.cbl`, `lgstsq.cbl`). |
| `runs/` | One record per run (`lgapol01-deneme.md`). |
| `build/` | Compiler output. Not committed (see `.gitignore`). |

## Setup

- GnuCOBOL 3.2.0 (`cobc`), installed on Windows through MSYS2 (`mingw-w64-ucrt-x86_64-gnucobol`).
- A hello world program compiled and ran correctly (`cobc -x hello.cbl` printed `GnuCOBOL OK`).
- **Put the MSYS2 toolchain first on `PATH` before every session** (Git Bash example):

  ```
  export PATH=/c/msys64/ucrt64/bin:$PATH
  ```

  Git Bash has its own `gcc` under `/mingw64/bin`. `cobc` picks it up, and the build then fails with
  exit code 1 and **no message at all**, even though `cobc -fsyntax-only` passes. `cobc -v` shows the
  failing `gcc` line. With the ucrt64 toolchain first, `which gcc` prints `/c/msys64/ucrt64/bin/gcc`.

## Method: run a CICS program in GnuCOBOL

GnuCOBOL does not know CICS. The program expects two things from CICS: the **EIB fields** (filled in
by CICS) and the **`EXEC CICS` commands** (rewritten by the CICS translator). The method supplies both
by hand. Example name used below: `PROG` = the program, here `LGAPOL01`.

### 1. Copy the program, keep the original

Copy `base/src/prog.cbl` and every copybook it includes into `work/`. Keep the original copy
untouched and edit a second file (`prog-gc.cbl`). Then `diff` shows every change you made:

```
diff <(tr -d '\r' < work/lgapol01.cbl) <(tr -d '\r' < work/lgapol01-gc.cbl)
```

Keep the `PROGRAM-ID` unchanged, because other programs `CALL` it by that name. The sources use CRLF
line endings; keep them so the diff stays small.

### 2. Find what the program needs

```
grep -n "EIB" work/prog.cbl
grep -n -A3 "EXEC CICS" work/prog.cbl
```

List every EIB field used and every `EXEC CICS` command, with its line number. Count the commands
and check the count at the end (LGAPOL01: 9 commands, 4 EIB fields).

### 3. Declare the EIB fields in WORKING-STORAGE

Add only the fields the program uses, with the types CICS gives them:

```cobol
       01  EIBCALEN   PIC S9(4) COMP-5 IS EXTERNAL.
       01  EIBTRNID   PIC X(4)         IS EXTERNAL.
       01  EIBTRMID   PIC X(4)         IS EXTERNAL.
       01  EIBTASKN   PIC S9(7) COMP-3 IS EXTERNAL.
```

- `EXTERNAL` makes the field shared by name between the program and the driver, so the driver can
  set it before the `CALL`. The driver declares the same four lines.
- Use `COMP-5` for `EIBCALEN`. With plain `COMP`, GnuCOBOL cuts the value to the number of digits in
  the `PIC`: `MOVE 32500` into `PIC S9(4) COMP` gives `2500` (checked), which would make a
  full-size COMMAREA look short.

### 4. Make the COMMAREA arrive

Under CICS the COMMAREA arrives without a `USING` clause. Under GnuCOBOL it has to be passed:

```cobol
       PROCEDURE DIVISION USING DFHCOMMAREA.
```

### 5. Replace each `EXEC CICS` command

| Command | Replacement | Why |
|---|---|---|
| `RETURN` | `GOBACK` | Ends the program and gives control back to the caller. |
| `LINK PROGRAM(X) COMMAREA(A)` | `CALL X USING A` (a literal `'X'` or the field that holds the name) | The called program is a stub (step 6). If the original has `LENGTH(n)`, pass it as an extra `BY CONTENT` parameter when the stub needs it (`BY CONTENT LENGTH OF A`). LGAPDB01 gets no length: its stub does not use it. |
| `ASKTIME`, `FORMATTIME` | `DISPLAY 'STUB EXEC CICS ...'` | Time is not needed for the result. The target fields keep their initial value (`SPACES`). |
| `ABEND` | `DISPLAY 'STUB EXEC CICS ABEND ...'` followed by `GOBACK` | A real `ABEND` never returns. Without the `GOBACK` the program would run on after the abend. |

Anything else you meet (`READ`, `WRITE`, `SEND`, `XCTL`, `EXEC SQL`...) is not covered yet. Decide
for each one what the stub should do and add a row here.

### 6. Write a stub for each linked program

One file per program in `stubs/`, same `PROGRAM-ID` as the real one, same copybook for the
COMMAREA. A stub reports that it was reached and what it received. **It must not invent results**:
it does not set return codes or fill fields, so what you see in the output comes from the program under test.

```cobol
       PROGRAM-ID. LGAPDB01.
       LINKAGE SECTION.
       01  DFHCOMMAREA.
             Copy LGCMAREA.
       PROCEDURE DIVISION USING DFHCOMMAREA.
           DISPLAY 'STUB LGAPDB01 reached, CA-REQUEST-ID=' CA-REQUEST-ID
           GOBACK.
```

### 7. Write the driver

A small program that plays the role of CICS. It must:

1. declare the four EIB fields (`EXTERNAL`, same as the program),
2. set `EIBTRNID`, `EIBTRMID`, `EIBTASKN` and `EIBCALEN`,
3. build the COMMAREA (`Copy LGCMAREA` under a `01`),
4. `CALL` the program with the COMMAREA,
5. `DISPLAY` the return code field (`CA-RETURN-CODE`) afterwards.

Take the case from the command line (`ACCEPT ... FROM COMMAND-LINE`), so one executable covers
all cases. For a "too short" case, pass a separate, really short area (10 bytes) and set
`EIBCALEN` to its length, so the program cannot read or write past what the caller supplied.
Set the return code to a value the program never produces (`99`) before the call; a changed value
then proves the program wrote it.

### 8. Build

From `tests/gnucobol/`. The programs are separate modules, the way CICS programs are separate load
modules; only the driver is an executable. The module file name must equal the `PROGRAM-ID`,
because `CALL` looks it up by that name.

```
mkdir -p build
cobc -m -I work -o build/LGAPOL01.dll work/lgapol01-gc.cbl
cobc -m -I work -o build/LGAPDB01.dll stubs/lgapdb01.cbl
cobc -m         -o build/LGSTSQ.dll   stubs/lgstsq.cbl
cobc -x -I work -o build/drv-lgapol01.exe work/drv-lgapol01.cbl
```

`-I work` is where `Copy LGCMAREA` is found. Check for errors before building with
`cobc -fsyntax-only -I work work/prog-gc.cbl`: it must print nothing.

### 9. Run

```
export COB_LIBRARY_PATH=build        # where CALL finds the modules
./build/drv-lgapol01.exe SHORT
./build/drv-lgapol01.exe OK
./build/drv-lgapol01.exe NONE
```

- **One case per process.** `WORKING-STORAGE` keeps its values between `CALL`s in the same process,
  while CICS gives every task a fresh copy. LGAPOL01 does `ADD WS-CA-HEADER-LEN TO WS-REQUIRED-CA-LEN`,
  so a second call in the same process would compare against 56 instead of 28. If a driver ever needs
  several calls, `CANCEL` the program between them.
- Record each run in `runs/`: build commands, inputs, full output.

### 10. Check the result, then check the method

- Every `EXEC CICS` command is gone: `grep -c "^ *EXEC CICS" work/prog-gc.cbl` must print `0`. (The
  words `STUB EXEC CICS` stay inside the `DISPLAY` texts, which is why the pattern starts at the line start.)
- Every replaced command was run at least once. Otherwise the replacement is only known to compile,
  not to work. Work out which branch holds each command and add a case for it. LGAPOL01 needs a third
  case (`NONE`): `ASKTIME`, `FORMATTIME`, `ABEND` and the first `LGSTSQ` call sit behind
  `EIBCALEN = 0`. Two more `LGSTSQ` calls (inside `IF EIBCALEN > 0` in `WRITE-ERROR-MESSAGE`) cannot
  be reached, because the only `PERFORM WRITE-ERROR-MESSAGE` happens when `EIBCALEN` is zero. They are
  compiled but never run; say so in the run record.

### Limits

- This checks what the COBOL code does with the COMMAREA, not what the stubbed programs do. A result
  that depends on `LGAPDB01` (database inserts) needs a stub that behaves like it, and that is a
  decision for the test team, because it adds assumptions.
- Time is stubbed, so error messages carry blank date and time.
- `EXEC SQL` is not handled.

## First compile check (before the method)

`work/` holds unchanged copies of `base/src/lgapol01.cbl` (the policy-add business program) and
`base/src/lgcmarea.cpy` (the COMMAREA copybook it includes). From inside `work/`:

```
cobc -fsyntax-only -I . lgapol01.cbl
```

`-fsyntax-only` checks the source without producing an executable. The result is **12 errors, 0 warnings**.
Everything below comes from this one run.

### Errors and causes

| Line(s) | Error | Cause |
|---|---|---|
| 88-91 | `'EIBTRNID'`, `'EIBTRMID'`, `'EIBTASKN'`, `'EIBCALEN'` is not defined | **EIB fields.** CICS fills the EXEC Interface Block (EIB) in for every program. It is not declared in the source, so `cobc` sees undefined names. cobc reports an undefined name once, but `EIBCALEN` is also used on lines 98, 113, 154-156. |
| 101 | `'EXEC' is not defined`, syntax error | **EXEC CICS ABEND.** `EXEC CICS ... END-EXEC` is not COBOL; the CICS translator rewrites it into real calls before compiling. GnuCOBOL has no CICS translator, so it reads `EXEC` as an unknown word. |
| 115 | `'CICS'`, `'END-EXEC'` is not defined, `'END-EXEC' is not a file name`, syntax error at `END-IF` (line 116) | **EXEC CICS RETURN.** Same cause. The parser then misreads `RETURN` as the COBOL `RETURN` statement (sort/merge files), which is why it asks for a file name. |
| 121 | syntax error, unexpected Identifier | **EXEC CICS LINK PROGRAM(LGAPDB01)** with `COMMAREA` and `LENGTH`. Same cause. This call is how the business layer calls the data layer. |
| 126 | syntax error, `unknown statement 'EXEC'` | **EXEC CICS RETURN** (final). Same cause. |

The source has more `EXEC CICS` blocks (ASKTIME and FORMATTIME on lines 140-145, LINK to `LGSTSQ` on lines 149, 157 and 163).
`cobc` did not list them. These are most likely hidden behind the earlier syntax errors, so fixing the first ones
would uncover them.

### Copybook

- With `lgcmarea.cpy` next to the program, `Copy LGCMAREA.` (line 71) resolves and gives no error. COBOL's copybook
  lookup depends on the file name and the search path (`-I`).
- With the copybook removed, the run fails with `lgapol01.cbl:71: error: LGCMAREA: No such file or directory`.
- `DFHCOMMAREA` (line 70) is declared in the program itself, so it is not an error. It is the data area CICS passes to
  the program, and its layout comes from the copybook.

### Takeaway

Nothing fails because of the COBOL itself. All 12 errors come from the CICS environment that the program expects:
`EXEC CICS` commands (to be rewritten by a translator) and EIB fields (to be provided by CICS). Running GenApp in
GnuCOBOL therefore needs a replacement for these two things, plus a replacement for `EXEC SQL` in the programs that
touch Db2. The method above is that replacement.

## Measured runs (comparing with `tests/predictions/`)

A measured run follows everything above, plus the rules below. They come from the LGICUS01 run
(`runs/lgicus01.md`).

### Before the run

1. The prediction file for the program must be on `main` (`git log origin/main -- tests/predictions/<file>`), and must not change after the run.
2. `git diff <prediction source commit> origin/main -- base/` must be empty, or the line numbers in the prediction are wrong.
3. Write the rules for "tuttu / tutmadı / belirsiz / ölçülemedi" **before** running (see `runs/lgicus01.md`).
4. Read the preconditions of the prediction file (for 02: B1-B4) and build the driver to them. They override step 7 above.

### What changes compared with a trial run

- **One 32500-byte buffer (prediction precondition B1).** The driver always passes the same full buffer. `EIBCALEN` is only the length it
  reports; bytes after `EIBCALEN` are spaces. Do not pass a separate short area: the writes outside the reported length must land in the
  caller's buffer, because the prediction describes them.
- **`LINK ... LENGTH(n)` also changes `EIBCALEN`.** The EIB fields are shared by all programs here (`EXTERNAL`), but under CICS the called
  program gets its own `EIBCALEN = n`. So `EXEC CICS LINK PROGRAM(X) COMMAREA(A) LENGTH(n)` becomes
  `MOVE EIBCALEN TO WS-SAVE-EIBCALEN`, `MOVE n TO EIBCALEN`, `CALL X USING A`, `MOVE WS-SAVE-EIBCALEN TO EIBCALEN`.
  Without it the called program sees the caller's length (the LGAPOL01 and LGDPDB01 trials did not notice, because their stubs never read `EIBCALEN`).
- **`SELECT ... INTO`:** replace the statement with a `DISPLAY` of the input host variable and
  `CALL 'DB2CUST' USING SQLCA <input host variable> <every INTO variable, in INTO order>`. `stubs/db2cust.cbl` shows the pattern: it
  either answers with a forced `SQLCODE` (external field `DB2FORCE-SQLCODE`, set by the driver) or looks the key up in a table written in the
  stub. The mapping from the SELECT list to the INTO list is done by hand in the stub, so check it against the SQL text.
- **Source of the table values:** the same file the prediction names (here `base/cntl/db2cre.jcl`). Only load the rows a test needs.
- **A pass is not a Db2 result.** Rows that compare table data check that the COBOL moves and pads the values correctly. They do not check Db2.

### Scripted conversion

For a large program, change the copy with a script, not by hand, and check with `diff` and
`grep -c "^ *EXEC "` (must print `0`). Watch columns 8-72: a replaced line that is indented too much goes past column 72 and is cut off.

### Compare with a script

`runs/lgicus01-karsilastir.sh` builds every expected value from the prediction file and compares it with `runs/lgicus01-cikti.txt` byte by byte
(`[ ]` brackets in the output show trailing spaces). Keep the raw output in `runs/` next to the record.
