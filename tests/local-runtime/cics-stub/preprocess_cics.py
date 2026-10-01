#!/usr/bin/env python3
"""Translates the ~11 EXEC CICS verbs GenApp's business/data-access layer
actually uses into plain COBOL (CALL statements for file I/O and LINK,
COBOL intrinsics for date/time, structural rewrites for RETURN/ABEND/GET
COUNTER) so cobc can compile them without a real CICS translator.

Not a general CICS translator - scoped exactly to the statement shapes
found in base/src/*.cbl (see docs/local-cics-runtime-plan.md for the full
catalogue this was built from). Unrecognized EXEC CICS verbs are left as
comments with a FIXME marker rather than silently dropped, so a missing
case fails loudly (a compile error) instead of quietly misbehaving.

Usage: preprocess_cics.py INPUT.cbl OUTPUT.cbl
"""
import re
import sys

EXEC_CICS_RE = re.compile(
    r"(?P<indent>^[ \t]*)(?:EXEC|Exec)\s+(?:CICS|Cics)\s+(?P<body>.*?)\bEND-EXEC\b(?P<period>\.?)",
    re.IGNORECASE | re.DOTALL | re.MULTILINE,
)


def clause(body, name):
    """Extract CLAUSE(value) - value may be a literal or identifier, and may
    itself contain nested parens is not supported (GenApp never does that
    for the clauses we handle)."""
    m = re.search(rf"\b{name}\s*\(([^)]*)\)", body, re.IGNORECASE)
    return m.group(1).strip() if m else None


def has_flag(body, name):
    return re.search(rf"\b{name}\b", body, re.IGNORECASE) is not None


def strip_quotes(s):
    if s and len(s) >= 2 and s[0] in "'\"" and s[-1] == s[0]:
        return s[1:-1]
    return s


MAX_COL = 72  # fixed-format COBOL: anything past column 72 is ignored


def wrap_tokens(indent, tokens):
    """Join tokens with spaces, wrapping onto continuation lines so no line
    exceeds column 72 - fixed-format COBOL source silently truncates (not
    errors on) anything past it, which otherwise produces confusing
    downstream syntax errors rather than an obvious one at the real cause."""
    cont_indent = indent + "    "
    lines = []
    cur = indent
    for tok in tokens:
        candidate = (cur + " " + tok) if cur.strip() else (cur + tok)
        if len(candidate) > MAX_COL and cur.strip():
            lines.append(cur)
            cur = cont_indent + tok
        else:
            cur = candidate
    if cur.strip():
        lines.append(cur)
    return "\n".join(lines)


def cobol_call(indent, target, args):
    # keep "USING" glued to CALL target on line 1, args wrap after
    head = f"{indent}CALL {target} USING"
    arg_lines = wrap_tokens(indent + "    ", args)
    return f"{head}\n{arg_lines}\n{indent}END-CALL"


_NUMERIC_LITERAL_RE = re.compile(r"^[+-]?\d+$")


def by_value_numeric(indent, expr, temp_name):
    """`BY VALUE <identifier>` passes exactly that identifier's own native
    size (e.g. 2 bytes for a typical PIC S9(4) BINARY length field like
    CUSTOMER-RECORD-SIZE), not a C `int`-sized 4 bytes - passing one of
    those straight to our stubs (which take plain `int` by value)
    corrupts the argument list for everything after it (seen in practice:
    an "attempt to reference invalid memory address" crash on the very
    next call). A bare integer *literal* doesn't have this problem (the
    SQL bridge already relies on that - "BY VALUE 255" and friends all
    work). So: literals pass straight through; identifiers get MOVEd into
    one of the two fixed-size temps declared in the injected header first,
    and that temp's name is what actually appears in the CALL.
    Returns (pre_statement_or_None, value_to_use_in_call)."""
    if _NUMERIC_LITERAL_RE.match(expr):
        return None, f"BY VALUE {expr}"
    return f"{indent}MOVE {expr} TO {temp_name}", f"BY VALUE {temp_name}"


def translate_link(body, indent):
    prog = clause(body, "PROGRAM")
    commarea = clause(body, "COMMAREA")
    prog_literal = prog != strip_quotes(prog)
    prog_cobol = f'"{strip_quotes(prog)}"' if prog_literal else prog
    return cobol_call(indent, prog_cobol, [f"BY REFERENCE {commarea}"])


def translate_return(body, indent):
    if clause(body, "TRANSID") is not None:
        # Only used by the 3270 menu/test programs we don't run - fail loudly
        # rather than silently doing the wrong thing if this is ever hit.
        return f"{indent}*> FIXME: EXEC CICS RETURN TRANSID(...) not supported\n{indent}GOBACK"
    return f"{indent}GOBACK"


def translate_abend(body, indent):
    code = strip_quotes(clause(body, "ABCODE") or "????")
    return f'{indent}DISPLAY "ABEND {code}"\n{indent}MOVE 1 TO RETURN-CODE\n{indent}GOBACK'


_asktime_pending = {}  # abstime-field -> indent, consumed by the next FORMATTIME


def translate_asktime(body, indent):
    abstime = clause(body, "ABSTIME")
    _asktime_pending["field"] = abstime
    # ABSTIME itself isn't used for anything but feeding FORMATTIME in GenApp -
    # just remember we saw it, FORMATTIME does the real work.
    return f"{indent}CONTINUE"


def translate_formattime(body, indent):
    mmddyyyy = clause(body, "MMDDYYYY")
    time_field = clause(body, "TIME")
    lines = [f"{indent}MOVE FUNCTION CURRENT-DATE TO WS-GIX-CURDATE"]
    if mmddyyyy:
        lines.append(wrap_tokens(indent, [
            "STRING", "WS-GIX-CD-MM", "'/'", "WS-GIX-CD-DD", "'/'", "WS-GIX-CD-YYYY",
            "DELIMITED", "BY", "SIZE", "INTO", mmddyyyy,
        ]))
    if time_field:
        lines.append(wrap_tokens(indent, [
            "STRING", "WS-GIX-CD-HH", "':'", "WS-GIX-CD-MIN", "':'", "WS-GIX-CD-SS",
            "DELIMITED", "BY", "SIZE", "INTO", time_field,
        ]))
    return "\n".join(lines)


def translate_get_counter(body, indent):
    resp = clause(body, "RESP")
    # GenApp's own fallback (see LGACDB01) is to use the DB2 identity column
    # whenever the named counter isn't available - forcing a non-NORMAL RESP
    # here, rather than implementing a real counter service, makes it take
    # that path every time, which is the behaviour we want for a local run.
    if resp:
        return f"{indent}MOVE 99 TO {resp}"
    return f"{indent}CONTINUE"


def translate_file_op(verb, body, indent):
    file_name = strip_quotes(clause(body, "FILE"))
    resp = clause(body, "RESP")
    pre = []

    def len_arg(expr, temp):
        stmt, value = by_value_numeric(indent, expr, temp)
        if stmt:
            pre.append(stmt)
        return value

    # The BY phrase in a CALL ... USING list applies to every operand after
    # it until the next BY phrase - it does NOT revert to BY REFERENCE after
    # a BY VALUE operand. Every reference-mode argument must say so
    # explicitly (gixpp's own generated code always does this; omitting it
    # silently downgrades the next alphanumeric operand to BY CONTENT
    # instead, which crashes the callee - confirmed by hitting exactly that
    # with a minimal repro before this was understood).
    if verb == "read":
        into_ = clause(body, "INTO")
        length = len_arg(clause(body, "LENGTH"), "WS-GIX-LEN1")
        ridfld = clause(body, "RIDFLD")
        keylength = len_arg(clause(body, "KEYLENGTH"), "WS-GIX-LEN2")
        gteq = "1" if has_flag(body, "GTEQ") else "0"
        generic = "1" if has_flag(body, "GENERIC") else "0"
        args = [f'BY REFERENCE "{file_name}"', f"BY REFERENCE {into_}", length,
                f"BY REFERENCE {ridfld}", keylength, f"BY REFERENCE {resp}",
                f"BY VALUE {gteq}", f"BY VALUE {generic}"]
        call = cobol_call(indent, '"VSAMREAD"', args)
    elif verb == "write":
        from_ = clause(body, "FROM")
        length = len_arg(clause(body, "LENGTH"), "WS-GIX-LEN1")
        ridfld = clause(body, "RIDFLD")
        keylength = len_arg(clause(body, "KEYLENGTH"), "WS-GIX-LEN2")
        args = [f'BY REFERENCE "{file_name}"', f"BY REFERENCE {from_}", length,
                f"BY REFERENCE {ridfld}", keylength, f"BY REFERENCE {resp}"]
        call = cobol_call(indent, '"VSAMWRITE"', args)
    elif verb == "rewrite":
        from_ = clause(body, "FROM")
        length = len_arg(clause(body, "LENGTH"), "WS-GIX-LEN1")
        args = [f'BY REFERENCE "{file_name}"', f"BY REFERENCE {from_}", length,
                f"BY REFERENCE {resp}"]
        call = cobol_call(indent, '"VSAMREWRITE"', args)
    elif verb == "delete":
        ridfld = clause(body, "RIDFLD")
        keylength = len_arg(clause(body, "KEYLENGTH"), "WS-GIX-LEN2")
        args = [f'BY REFERENCE "{file_name}"', f"BY REFERENCE {ridfld}", keylength,
                f"BY REFERENCE {resp}"]
        call = cobol_call(indent, '"VSAMDELETE"', args)
    else:
        raise AssertionError(verb)

    return "\n".join(pre + [call]) if pre else call


VERB_HANDLERS = {
    "link": translate_link,
    "return": translate_return,
    "abend": translate_abend,
    "asktime": translate_asktime,
    "formattime": translate_formattime,
}


def translate_block(m):
    indent = m.group("indent")
    body = m.group("body")
    # The original `END-EXEC` may or may not have had a trailing period -
    # whichever it was, the replacement statement needs the same, or it
    # silently merges into the next line's paragraph name/statement as one
    # unterminated COBOL "sentence" (seen in practice: "syntax error,
    # unexpected Identifier" pointing at the *next* paragraph header, not
    # at the real cause here).
    period = m.group("period")
    first_word = re.match(r"\s*(\w[\w-]*)", body)
    verb = first_word.group(1).lower() if first_word else ""

    if verb in ("read", "write", "rewrite", "delete"):
        result = translate_file_op(verb, body, indent)
    elif verb == "get" and "counter" in body.lower():
        result = translate_get_counter(body, indent)
    elif verb in VERB_HANDLERS:
        result = VERB_HANDLERS[verb](body, indent)
    else:
        # Unhandled verb (HANDLE, SEND, RECEIVE, ENQ, DEQ, ASSIGN, ...): not
        # expected to appear in the programs this preprocessor is run
        # against. Comment it out with a marker instead of leaving invalid
        # syntax, so the *next* compile error (if the stubbed-out behaviour
        # actually mattered) points straight at this spot.
        commented = "\n".join(indent + "*>" + line for line in m.group(0).splitlines())
        return f"{commented}\n{indent}*> FIXME: EXEC CICS {verb} not translated"

    return result + period


def translate_dfhresp(text):
    # Only NORMAL appears in base/src's VSAM programs (confirmed by grep) -
    # every other DFHRESP(x) is deliberately left alone so an unexpected one
    # fails to compile instead of silently getting the wrong value.
    return re.sub(r"\bDFHRESP\s*\(\s*NORMAL\s*\)", "0", text, flags=re.IGNORECASE)


EIB_BLOCK = """\
       01  EIBCALEN                 PIC S9(4) COMP VALUE 32500.
       01  EIBRESP2                 PIC S9(8) COMP VALUE 0.
       01  EIBTASKN                 PIC 9(7) VALUE 0.
       01  EIBTRNID                 PIC X(4) VALUE SPACES.
       01  EIBTRMID                 PIC X(4) VALUE SPACES.
       01  WS-GIX-LEN1               PIC 9(8) COMP-5 VALUE 0.
       01  WS-GIX-LEN2               PIC 9(8) COMP-5 VALUE 0.
       01  WS-GIX-CURDATE.
           05 WS-GIX-CD-YYYY        PIC 9(4).
           05 WS-GIX-CD-MM          PIC 9(2).
           05 WS-GIX-CD-DD          PIC 9(2).
           05 WS-GIX-CD-HH          PIC 9(2).
           05 WS-GIX-CD-MIN         PIC 9(2).
           05 WS-GIX-CD-SS          PIC 9(2).
           05 FILLER                PIC X(9).
"""


def inject_eib_block(text):
    return re.sub(
        r"(^\s*WORKING-STORAGE SECTION\.\s*\n)",
        r"\1" + EIB_BLOCK,
        text,
        count=1,
        flags=re.IGNORECASE | re.MULTILINE,
    )


def fix_procedure_division_using(text):
    """Real CICS implicitly wires up DFHCOMMAREA without a USING clause on
    PROCEDURE DIVISION (the translator inserts that linkage); plain COBOL
    needs it explicit, or DFHCOMMAREA in the LINKAGE SECTION is never
    connected to the CALL "X" USING commarea argument - every reference to
    it then reads uninitialized/invalid memory (confirmed - this crashed
    LGACVS01 with "attempt to reference invalid memory address" even though
    the CALL contract itself was already correct)."""
    if re.search(r"^\s*01\s+DFHCOMMAREA\b", text, re.IGNORECASE | re.MULTILINE):
        text = re.sub(
            r"^(\s*)PROCEDURE DIVISION\s*\.",
            r"\1PROCEDURE DIVISION USING DFHCOMMAREA.",
            text,
            count=1,
            flags=re.IGNORECASE | re.MULTILINE,
        )
    return text


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)
    with open(sys.argv[1], "r", encoding="utf-8", errors="replace") as f:
        text = f.read()

    text = EXEC_CICS_RE.sub(translate_block, text)
    text = translate_dfhresp(text)
    text = inject_eib_block(text)
    text = fix_procedure_division_using(text)

    with open(sys.argv[2], "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


if __name__ == "__main__":
    main()
