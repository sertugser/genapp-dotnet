# tests/gnucobol

First check of running GenApp code in GnuCOBOL: what compiles as-is, and where it stops.

## Setup

- GnuCOBOL 3.2.0 (`cobc`), installed on Windows through MSYS2 (`mingw-w64-ucrt-x86_64-gnucobol`).
- A hello world program compiled and ran correctly (`cobc -x hello.cbl` printed `GnuCOBOL OK`).

## Experiment

`work/` holds unchanged copies of `base/src/lgapol01.cbl` (the policy-add business program) and
`base/src/lgcmarea.cpy` (the COMMAREA copybook it includes). From inside `work/`:

```
cobc -fsyntax-only -I . lgapol01.cbl
```

`-fsyntax-only` checks the source without producing an executable. The result is **12 errors, 0 warnings**.
Everything below comes from this one run.

## Errors and causes

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

## Copybook

- With `lgcmarea.cpy` next to the program, `Copy LGCMAREA.` (line 71) resolves and gives no error. COBOL's copybook
  lookup depends on the file name and the search path (`-I`).
- With the copybook removed, the run fails with `lgapol01.cbl:71: error: LGCMAREA: No such file or directory`.
- `DFHCOMMAREA` (line 70) is declared in the program itself, so it is not an error. It is the data area CICS passes to
  the program, and its layout comes from the copybook.

## Takeaway

Nothing fails because of the COBOL itself. All 12 errors come from the CICS environment that the program expects:
`EXEC CICS` commands (to be rewritten by a translator) and EIB fields (to be provided by CICS). Running GenApp in
GnuCOBOL therefore needs a replacement for these two things, plus a replacement for `EXEC SQL` in the programs that
touch Db2.
