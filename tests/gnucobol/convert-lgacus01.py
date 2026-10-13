#!/usr/bin/env python3
"""Converts the LGACUS01 customer-add chain for GnuCOBOL (no CICS, no Db2).

Reads  work/lgacus01.cbl, lgacdb01.cbl, lgacdb02.cbl, lgacvs01.cbl  (unchanged
copies of base/src/) and writes work/<name>-gc.cbl next to them.

The conversion follows tests/gnucobol/README.md (method) and runs/lgacus01.md
(rows added for the commands this chain needs). Every EXEC statement must match
one known pattern; anything else stops the script. At the end the number of
statements replaced per kind is compared with the number counted by reading the
sources (EXPECTED below), and no 'EXEC ' may be left in a code line.

Usage (from tests/gnucobol):  python convert-lgacus01.py
"""
import pathlib
import re
import sys

WORK = pathlib.Path(__file__).resolve().parent / "work"
PROGRAMS = ["lgacus01", "lgacdb01", "lgacdb02", "lgacvs01"]

# Statements counted by reading each source (not by this script).
EXPECTED = {
    "lgacus01": {"RETURN": 2, "ABEND": 1, "ASKTIME": 1, "FORMATTIME": 1,
                 "LINK-LGSTSQ": 3, "LINK": 1},
    "lgacdb01": {"RETURN": 4, "ABEND": 1, "ASKTIME": 1, "FORMATTIME": 1,
                 "LINK-LGSTSQ": 3, "LINK": 2, "GET-COUNTER": 1,
                 "INCLUDE": 2, "INSERT-CUSTOMER": 1, "INSERT-CUSTOMER-DEFAULT": 1,
                 "SET-IDENTITY": 1},
    "lgacdb02": {"RETURN": 3, "ABEND": 1, "ASKTIME": 1, "FORMATTIME": 1,
                 "LINK-LGSTSQ": 3, "INCLUDE": 1, "INSERT-SECURE": 1},
    "lgacvs01": {"RETURN": 1, "ABEND": 1, "ASKTIME": 1, "FORMATTIME": 1,
                 "LINK-LGSTSQ": 3, "WRITE-FILE": 1},
}

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
EIBRESP2_LINE = "       01  EIBRESP2                    PIC S9(8) COMP-5 IS EXTERNAL."
SAVE_LINE = "       01  WS-SAVE-EIBCALEN            PIC S9(4) COMP-5."

# column lists of the INSERTs, checked against the VALUES order
CUSTOMER_COLS = ["CUSTOMERNUMBER", "FIRSTNAME", "LASTNAME", "DATEOFBIRTH",
                 "HOUSENAME", "HOUSENUMBER", "POSTCODE", "PHONEMOBILE",
                 "PHONEHOME", "EMAILADDRESS"]
CUSTOMER_VALS = ["DB2-CUSTOMERNUM-INT", "CA-FIRST-NAME", "CA-LAST-NAME", "CA-DOB",
                 "CA-HOUSE-NAME", "CA-HOUSE-NUM", "CA-POSTCODE",
                 "CA-PHONE-MOBILE", "CA-PHONE-HOME", "CA-EMAIL-ADDRESS"]
SECURE_COLS = ["CUSTOMERNUMBER", "CUSTOMERPASS", "STATE_INDICATOR", "PASS_CHANGES"]
SECURE_VALS = ["DB2-CUSTOMERNUM-INT", "D2-CUSTSECR-PASS", "D2-CUSTSECR-STATE",
               "DB2-CUSTOMERCNT-INT"]


def fail(msg):
    sys.exit("convert-lgacus01: " + msg)


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


def call(out, indent, name, args, period):
    """CALL name USING args..., wrapped to 72 columns."""
    lines = []
    cur = "CALL " + name + " USING"
    for a in args:
        if len(indent + cur + " " + a) > 72:
            lines.append(cur)
            cur = "    " + a
        else:
            cur += " " + a
    lines.append(cur)
    if period:
        lines[-1] += "."
    for l in lines:
        put(out, indent, l)


def convert(name):
    raw = (WORK / (name + ".cbl")).read_bytes().decode("latin-1")
    if "\r\n" not in raw:
        fail(name + ": expected CRLF line endings")
    lines = raw.split("\r\n")
    counts = {}

    def count(kind):
        counts[kind] = counts.get(kind, 0) + 1

    out = []
    uses_link = False
    uses_resp2 = False
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
            g = None

            if re.match(r"^EXEC SQL INCLUDE (SQLCA|LGCMAREA) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL INCLUDE (\w+) END-EXEC$", body, re.I)
                put(out, indent, "COPY " + g.group(1).upper() + ".")
                count("INCLUDE")
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
            elif re.match(r"^EXEC CICS LINK PROGRAM\(([\w-]+)\) COMMAREA\(([\w-]+)\) LENGTH\((\d+)\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC CICS LINK PROGRAM\(([\w-]+)\) COMMAREA\(([\w-]+)\) LENGTH\((\d+)\) END-EXEC$", body, re.I)
                prog, area, ln = g.group(1), g.group(2), g.group(3)
                display(out, indent, "STUB EXEC CICS LINK PROGRAM(%s) LENGTH(%s)" % (prog, ln))
                put(out, indent, "MOVE EIBCALEN TO WS-SAVE-EIBCALEN")
                put(out, indent, "MOVE %s TO EIBCALEN" % ln)
                call(out, indent, prog, [area], False)
                put(out, indent, "MOVE WS-SAVE-EIBCALEN TO EIBCALEN")
                put(out, indent, "IF ABEND-FLAG = 'Y'")
                put(out, indent + "    ", "GOBACK")
                put(out, indent, "END-IF" + P)
                uses_link = True
                count("LINK")
            elif re.match(r"^EXEC CICS GET COUNTER\(([\w-]+)\) POOL\(([\w-]+)\) VALUE\(([\w-]+)\) RESP\(([\w-]+)\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC CICS GET COUNTER\(([\w-]+)\) POOL\(([\w-]+)\) VALUE\(([\w-]+)\) RESP\(([\w-]+)\) END-EXEC$", body, re.I)
                display(out, indent, "STUB EXEC CICS GET COUNTER(%s) POOL(%s)" % (g.group(1), g.group(2)))
                call(out, indent, "'CICSCNT'", [g.group(4), g.group(3)], period)
                count("GET-COUNTER")
            elif re.match(r"^EXEC CICS WRITE FILE\('KSDSCUST'\) FROM\(([\w-]+)\) LENGTH\(([\w-]+)\) RIDFLD\(([\w-]+)\) KEYLENGTH\(10\) RESP\(([\w-]+)\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC CICS WRITE FILE\('KSDSCUST'\) FROM\(([\w-]+)\) LENGTH\(([\w-]+)\) RIDFLD\(([\w-]+)\) KEYLENGTH\(10\) RESP\(([\w-]+)\) END-EXEC$", body, re.I)
                if g.group(1).upper() != g.group(3).upper():
                    fail("WRITE FILE: FROM and RIDFLD differ (key would not be the first 10 bytes)")
                display(out, indent, "STUB EXEC CICS WRITE FILE(KSDSCUST)")
                call(out, indent, "'CICSWRF'", [g.group(1), g.group(2), g.group(4)], period)
                uses_resp2 = True
                count("WRITE-FILE")
            elif re.match(r"^EXEC SQL INSERT INTO (\w+) \((.*)\) VALUES \((.*)\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL INSERT INTO (\w+) \((.*)\) VALUES \((.*)\) END-EXEC$", body, re.I)
                table = g.group(1).upper()
                cols = [c.strip().upper() for c in g.group(2).split(",")]
                vals = [v.strip() for v in g.group(3).split(",")]
                hosts = [v[1:].upper() for v in vals if v.startswith(":")]
                if table == "CUSTOMER" and vals[0].upper() == "DEFAULT":
                    if cols != CUSTOMER_COLS or hosts != CUSTOMER_VALS[1:]:
                        fail("INSERT CUSTOMER DEFAULT: column/value order not as expected")
                    display(out, indent, "STUB EXEC SQL INSERT INTO CUSTOMER (DEFAULT)")
                    call(out, indent, "'DB2INSD'", ["SQLCA"] + hosts, False)
                    count("INSERT-CUSTOMER-DEFAULT")
                elif table == "CUSTOMER":
                    if cols != CUSTOMER_COLS or hosts != CUSTOMER_VALS:
                        fail("INSERT CUSTOMER: column/value order not as expected")
                    display(out, indent, "STUB EXEC SQL INSERT INTO CUSTOMER")
                    call(out, indent, "'DB2INSC'", ["SQLCA"] + hosts, False)
                    count("INSERT-CUSTOMER")
                elif table == "CUSTOMER_SECURE":
                    if cols != SECURE_COLS or hosts != SECURE_VALS:
                        fail("INSERT CUSTOMER_SECURE: column/value order not as expected")
                    display(out, indent, "STUB EXEC SQL INSERT INTO CUSTOMER_SECURE")
                    call(out, indent, "'DB2INSS'", ["SQLCA"] + hosts, False)
                    count("INSERT-SECURE")
                else:
                    fail("INSERT into unknown table: " + table)
                if period:
                    out[-1] += "."
                    if len(out[-1]) > 72:
                        fail("period pushed line past column 72")
            elif re.match(r"^EXEC SQL SET :([\w-]+) = IDENTITY_VAL_LOCAL\(\) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL SET :([\w-]+) = IDENTITY_VAL_LOCAL\(\) END-EXEC$", body, re.I)
                display(out, indent, "STUB EXEC SQL SET :%s = IDENTITY_VAL_LOCAL()" % g.group(1))
                call(out, indent, "'DB2IDENT'", ["SQLCA", g.group(1)], period)
                count("SET-IDENTITY")
            else:
                fail(name + ": unhandled EXEC statement: " + stmt)
            i += 1
            continue

        # DFHRESP is a translator macro; NORMAL is 0
        if not is_comment:
            line = re.sub(r"DFHRESP\(NORMAL\)", "0", line, flags=re.I)
        # the COMMAREA arrives through USING, not through CICS
        if re.match(r"\s*PROCEDURE DIVISION\.\s*$", line, re.I):
            line = "       PROCEDURE DIVISION USING DFHCOMMAREA."
        out.append(line)
        if re.match(r"\s*WORKING-STORAGE SECTION\.\s*$", line, re.I):
            out.append("")
            out.extend(EIB_BLOCK)
            out.append("__DECL_MARK__")
        i += 1

    decl = []
    if uses_resp2:
        decl.append(EIBRESP2_LINE)
    if uses_link:
        decl.append(SAVE_LINE)
    marker = out.index("__DECL_MARK__")
    out[marker:marker + 1] = decl

    text = "\r\n".join(out)
    # checks
    leftover = [l for l in out if len(l) > 6 and l[6] != "*" and re.match(r"\s*EXEC\s", l, re.I)]
    if leftover:
        fail(name + ": EXEC left: " + leftover[0])
    if "DFHRESP" in "\n".join(l for l in out if len(l) > 6 and l[6] != "*").upper():
        fail(name + ": DFHRESP left")
    too_long = [l for l in out if len(l) > 72 and not (len(l) > 6 and l[6] == "*")]
    if too_long:
        fail(name + ": line past column 72: " + too_long[0])
    if counts != EXPECTED[name]:
        fail("%s: replaced %s, expected %s" % (name, counts, EXPECTED[name]))
    (WORK / (name + "-gc.cbl")).write_bytes(text.encode("latin-1"))
    return counts


def main():
    for n in PROGRAMS:
        counts = convert(n)
        total = sum(counts.values())
        print("%s-gc.cbl: %d EXEC statements replaced %s" % (n, total, dict(sorted(counts.items()))))
    print("OK: no EXEC left in any converted program, counts match the sources")


if __name__ == "__main__":
    main()
