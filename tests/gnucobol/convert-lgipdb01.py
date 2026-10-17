#!/usr/bin/env python3
"""Converts LGIPDB01 for GnuCOBOL (no CICS, no Db2): every EXEC SQL statement becomes a
call to the stub DB2STUB, which only sets SQLCODE.

Reads  work/lgipdb01.cbl  (unchanged copy of base/src/lgipdb01.cbl) and writes
work/lgipdb01-sql.cbl next to it.

The conversion follows tests/gnucobol/README.md (method) and runs/lgipdb01.md. Every EXEC
statement must match one known pattern; anything else stops the script. At the end the number
of statements replaced per kind is compared with the number counted by reading the source
(EXPECTED below), and no 'EXEC ' may be left in a code line.

Unlike convert-lgipol01.py (task #28), SELECT lists, INTO lists and WHERE clauses are NOT
checked: the statements are never run, only their SQLCODE is made up. That is the limit of
this method (see runs/lgipdb01.md).

Usage (from tests/gnucobol):  python convert-lgipdb01.py
"""
import pathlib
import re
import sys

WORK = pathlib.Path(__file__).resolve().parent / "work"
NAME = "lgipdb01"

# Statements counted by reading the source (not by this script).
EXPECTED = {"RETURN": 6, "ABEND": 1, "ASKTIME": 1, "FORMATTIME": 1, "LINK-LGSTSQ": 3,
            "INCLUDE": 3, "DECLARE": 2, "SQL-STUB": 11}

EIB_BLOCK = [
    "      *----------------------------------------------------------------*",
    "      * GnuCOBOL: EIB fields CICS would normally fill in, and fields   *",
    "      * of the test rig. EXTERNAL so the driver program can set them   *",
    "      * before the CALL.                                               *",
    "      *----------------------------------------------------------------*",
    "       01  EIBCALEN                    PIC S9(4) COMP-5 IS EXTERNAL.",
    "       01  EIBTRNID                    PIC X(4)  IS EXTERNAL.",
    "       01  EIBTRMID                    PIC X(4)  IS EXTERNAL.",
    "       01  EIBTASKN                    PIC S9(7) COMP-3 IS EXTERNAL.",
    "       01  ABEND-FLAG                  PIC X     IS EXTERNAL.",
]


def fail(msg):
    sys.exit("convert-lgipdb01: " + msg)


def put(out, indent, text):
    """Emit one source line; fixed format allows columns 8-72 only."""
    line = indent + text
    if len(line) > 72:
        fail("line longer than 72 columns: " + line)
    out.append(line)


def display(out, indent, literal):
    one = "DISPLAY '" + literal + "'"
    if len(indent + one) <= 72:
        put(out, indent, one)
        return
    # adjacent literals are shown one after the other, without a separator
    put(out, indent, "DISPLAY")
    width = 72 - len(indent) - 4 - 2
    for k in range(0, len(literal), width):
        put(out, indent + "    ", "'" + literal[k:k + width] + "'")


def sql_label(body):
    """Short text for the DISPLAY line: statement kind and, for SELECT, the table list."""
    m = re.match(r"^EXEC SQL SELECT .* FROM (.*?) WHERE .* END-EXEC$", body, re.I)
    if m:
        return "SELECT FROM " + re.sub(r"\s+", "", m.group(1)).upper()
    m = re.match(r"^EXEC SQL (OPEN|CLOSE|FETCH) (\w+)", body, re.I)
    if m:
        return m.group(1).upper() + " " + m.group(2)
    return None


def convert():
    raw = (WORK / (NAME + ".cbl")).read_bytes().decode("latin-1")
    if "\r\n" not in raw:
        fail(NAME + ": expected CRLF line endings")
    lines = raw.split("\r\n")
    counts = {}

    def count(kind):
        counts[kind] = counts.get(kind, 0) + 1

    out = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # drop the Db2 precompiler directive
        if line.strip().upper() == "PROCESS SQL":
            i += 1
            continue
        is_comment = len(line) > 6 and line[6] == "*"
        if not is_comment and re.match(r"\s*EXEC\s+(CICS|SQL)\b", line, re.I):
            block = [line]
            while "END-EXEC" not in block[-1].upper():
                i += 1
                block.append(lines[i])
            indent = re.match(r"\s*", block[0]).group(0)
            stmt = re.sub(r"\s+", " ", " ".join(b.strip() for b in block)).strip()
            m = re.match(r"^(.*END-EXEC)(\.?)$", stmt, re.I)
            if not m:
                fail("unexpected text after END-EXEC: " + stmt)
            body, period = m.group(1), (m.group(2) == ".")
            P = "." if period else ""

            if re.match(r"^EXEC SQL INCLUDE (SQLCA|LGCMAREA|LGPOLICY) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL INCLUDE (\w+) END-EXEC$", body, re.I)
                put(out, indent, "COPY " + g.group(1).upper() + ".")
                count("INCLUDE")
            elif re.match(r"^EXEC SQL DECLARE (\w+) .* END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL DECLARE (\w+) ", body, re.I)
                put(out, "      ", "* GnuCOBOL: EXEC SQL DECLARE %s removed (no cursors)" % g.group(1))
                count("DECLARE")
            elif re.match(r"^EXEC SQL (SELECT|OPEN|CLOSE|FETCH) .*END-EXEC$", body, re.I):
                label = sql_label(body)
                if label is None:
                    fail("cannot label SQL statement: " + stmt)
                display(out, indent, "STUB EXEC SQL " + label)
                put(out, indent, "CALL 'DB2STUB' USING SQLCA" + P)
                count("SQL-STUB")
            elif re.match(r"^EXEC CICS RETURN END-EXEC$", body, re.I):
                put(out, indent, "GOBACK" + P)
                count("RETURN")
            elif re.match(r"^EXEC CICS ABEND ABCODE\('\w+'\) NODUMP END-EXEC$", body, re.I):
                g = re.match(r"^EXEC CICS ABEND ABCODE\('(\w+)'\) NODUMP END-EXEC$", body, re.I)
                display(out, indent, "STUB EXEC CICS ABEND ABCODE(%s)" % g.group(1))
                put(out, indent, "MOVE 'Y' TO ABEND-FLAG")
                put(out, indent, "GOBACK" + P)
                count("ABEND")
            elif re.match(r"^EXEC CICS ASKTIME ABSTIME\(\S+\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC CICS ASKTIME ABSTIME\((\S+)\) END-EXEC$", body, re.I)
                display(out, indent, "STUB EXEC CICS ASKTIME ABSTIME(%s)" % g.group(1))
                count("ASKTIME")
            elif re.match(r"^EXEC CICS FORMATTIME ABSTIME\(\S+\) MMDDYYYY\(\S+\) TIME\(\S+\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC CICS FORMATTIME ABSTIME\((\S+)\) MMDDYYYY", body, re.I)
                display(out, indent, "STUB EXEC CICS FORMATTIME ABSTIME(%s)" % g.group(1))
                count("FORMATTIME")
            elif re.match(r"^EXEC CICS LINK PROGRAM\('LGSTSQ'\) COMMAREA\(([\w-]+)\) LENGTH\(LENGTH OF ([\w-]+)\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC CICS LINK PROGRAM\('LGSTSQ'\) COMMAREA\(([\w-]+)\) LENGTH\(LENGTH OF ([\w-]+)\) END-EXEC$", body, re.I)
                if g.group(1).upper() != g.group(2).upper():
                    fail("LGSTSQ COMMAREA and LENGTH OF differ: " + body)
                put(out, indent, "CALL 'LGSTSQ' USING " + g.group(1))
                put(out, indent + "          ", "BY CONTENT LENGTH OF " + g.group(2) + P)
                count("LINK-LGSTSQ")
            else:
                fail(NAME + ": unhandled EXEC statement: " + stmt)
            i += 1
            continue

        # the COMMAREA arrives through USING, not through CICS
        if re.match(r"\s*PROCEDURE DIVISION\.\s*$", line, re.I):
            line = "       PROCEDURE DIVISION USING DFHCOMMAREA."
        out.append(line)
        if re.match(r"\s*WORKING-STORAGE SECTION\.\s*$", line, re.I):
            out.append("")
            out.extend(EIB_BLOCK)
        i += 1

    text = "\r\n".join(out)
    # checks
    leftover = [l for l in out if len(l) > 6 and l[6] != "*" and re.match(r"\s*EXEC\s", l, re.I)]
    if leftover:
        fail(NAME + ": EXEC left: " + leftover[0])
    too_long = [l for l in out if len(l) > 72 and not (len(l) > 6 and l[6] == "*")]
    if too_long:
        fail(NAME + ": line past column 72: " + too_long[0])
    if counts != EXPECTED:
        fail("%s: replaced %s, expected %s" % (NAME, counts, EXPECTED))
    (WORK / (NAME + "-sql.cbl")).write_bytes(text.encode("latin-1"))
    return counts


def main():
    counts = convert()
    print("%s-sql.cbl: %d EXEC statements replaced %s" % (NAME, sum(counts.values()), dict(sorted(counts.items()))))
    print("OK: no EXEC left, counts match the source")


if __name__ == "__main__":
    main()
