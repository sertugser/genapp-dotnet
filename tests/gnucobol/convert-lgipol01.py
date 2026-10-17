#!/usr/bin/env python3
"""Converts the LGIPOL01 policy-inquire chain for GnuCOBOL (no CICS, no Db2).

Reads  work/lgipol01.cbl, lgipdb01.cbl  (unchanged copies of base/src/) and
writes work/<name>-gc.cbl next to them.

The conversion follows tests/gnucobol/README.md (method) and runs/lgipol01.md.
Every EXEC statement must match one known pattern; anything else stops the
script. At the end the number of statements replaced per kind is compared with
the number counted by reading the sources (EXPECTED below), and no 'EXEC ' may
be left in a code line.

The rig supports ONE Db2 statement: the motor SELECT (the 05-xx predictions
cover the motor branch). The other SELECTs and the cursor statements are
replaced by a call to DB2UNSUP, which stops the process with an error. They are
compiled, never run: a case that reaches one is not valid.

Usage (from tests/gnucobol):  python convert-lgipol01.py
"""
import pathlib
import re
import sys

WORK = pathlib.Path(__file__).resolve().parent / "work"
PROGRAMS = ["lgipol01", "lgipdb01"]

# Statements counted by reading each source (not by this script).
EXPECTED = {
    "lgipol01": {"RETURN": 1, "ABEND": 1, "ASKTIME": 1, "FORMATTIME": 1,
                 "LINK-LGSTSQ": 3, "LINK": 1},
    "lgipdb01": {"RETURN": 6, "ABEND": 1, "ASKTIME": 1, "FORMATTIME": 1,
                 "LINK-LGSTSQ": 3, "INCLUDE": 3, "DECLARE": 2,
                 "SELECT-MOTOR": 1, "SELECT-UNSUPPORTED": 4,
                 "CURSOR-UNSUPPORTED": 6},
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
SAVE_LINE = "       01  WS-SAVE-EIBCALEN            PIC S9(4) COMP-5."

# The motor SELECT: column list and INTO list (host variable, optional
# indicator), checked against the stub DB2MOTOR, which maps them by position.
MOTOR_COLS = ["ISSUEDATE", "EXPIRYDATE", "LASTCHANGED", "BROKERID",
              "BROKERSREFERENCE", "PAYMENT", "MAKE", "MODEL", "VALUE",
              "REGNUMBER", "COLOUR", "CC", "YEAROFMANUFACTURE", "PREMIUM",
              "ACCIDENTS"]
MOTOR_INTO = [("DB2-ISSUEDATE", None), ("DB2-EXPIRYDATE", None),
              ("DB2-LASTCHANGED", None),
              ("DB2-BROKERID-INT", "IND-BROKERID"),
              ("DB2-BROKERSREF", "IND-BROKERSREF"),
              ("DB2-PAYMENT-INT", "IND-PAYMENT"),
              ("DB2-M-MAKE", None), ("DB2-M-MODEL", None),
              ("DB2-M-VALUE-INT", None), ("DB2-M-REGNUMBER", None),
              ("DB2-M-COLOUR", None), ("DB2-M-CC-SINT", None),
              ("DB2-M-MANUFACTURED", None), ("DB2-M-PREMIUM-INT", None),
              ("DB2-M-ACCIDENTS-INT", None)]
MOTOR_WHERE = ("( POLICY.POLICYNUMBER = MOTOR.POLICYNUMBER AND "
               "POLICY.CUSTOMERNUMBER = :DB2-CUSTOMERNUM-INT AND "
               "POLICY.POLICYNUMBER = :DB2-POLICYNUM-INT )")


def fail(msg):
    sys.exit("convert-lgipol01: " + msg)


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


def parse_into(text):
    """':A, :B INDICATOR :C' -> [('A', None), ('B', 'C')]"""
    items = []
    for part in text.split(","):
        m = re.match(r"^\s*:([\w-]+)(?:\s+INDICATOR\s+:([\w-]+))?\s*$", part, re.I)
        if not m:
            fail("cannot read INTO item: " + part)
        items.append((m.group(1).upper(), m.group(2).upper() if m.group(2) else None))
    return items


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

            if re.match(r"^EXEC SQL INCLUDE (SQLCA|LGCMAREA|LGPOLICY) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL INCLUDE (\w+) END-EXEC$", body, re.I)
                put(out, indent, "COPY " + g.group(1).upper() + ".")
                count("INCLUDE")
            elif re.match(r"^EXEC SQL DECLARE (\w+) .* END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL DECLARE (\w+) ", body, re.I)
                put(out, "      ", "* GnuCOBOL: EXEC SQL DECLARE %s removed (no cursors)" % g.group(1))
                count("DECLARE")
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
            elif re.match(r"^EXEC SQL SELECT (.*) INTO (.*) FROM (.*) WHERE (.*) END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL SELECT (.*) INTO (.*) FROM (.*) WHERE (.*) END-EXEC$", body, re.I)
                cols = [c.strip().upper() for c in g.group(1).split(",")]
                into = parse_into(g.group(2))
                tables = re.sub(r"\s+", "", g.group(3)).upper()
                where = g.group(4).strip().upper()
                if tables == "POLICY,MOTOR":
                    if cols != MOTOR_COLS:
                        fail("motor SELECT: column list not as expected: %s" % cols)
                    if into != MOTOR_INTO:
                        fail("motor SELECT: INTO list not as expected: %s" % into)
                    if where != MOTOR_WHERE:
                        fail("motor SELECT: WHERE clause not as expected: " + where)
                    if len(cols) != len(into):
                        fail("motor SELECT: columns and INTO items differ in number")
                    args = []
                    for host, ind in into:
                        args.append(host)
                        if ind:
                            args.append(ind)
                    put(out, indent, "DISPLAY 'STUB EXEC SQL SELECT FROM POLICY,MOTOR'")
                    put(out, indent + "        ", "' CUSTOMERNUMBER=' DB2-CUSTOMERNUM-INT")
                    put(out, indent + "        ", "' POLICYNUMBER=' DB2-POLICYNUM-INT")
                    call(out, indent, "'DB2MOTOR'",
                         ["SQLCA", "DB2-CUSTOMERNUM-INT", "DB2-POLICYNUM-INT"] + args, period)
                    count("SELECT-MOTOR")
                else:
                    display(out, indent, "STUB EXEC SQL SELECT FROM %s (rig: not supported)" % tables)
                    call(out, indent, "'DB2UNSUP'", ["SQLCA"], period)
                    count("SELECT-UNSUPPORTED")
            elif re.match(r"^EXEC SQL (OPEN|CLOSE|FETCH) (\w+)( INTO .*)? END-EXEC$", body, re.I):
                g = re.match(r"^EXEC SQL (OPEN|CLOSE|FETCH) (\w+)", body, re.I)
                display(out, indent, "STUB EXEC SQL %s %s (rig: not supported)" % (g.group(1).upper(), g.group(2)))
                call(out, indent, "'DB2UNSUP'", ["SQLCA"], period)
                count("CURSOR-UNSUPPORTED")
            else:
                fail(name + ": unhandled EXEC statement: " + stmt)
            i += 1
            continue

        # the COMMAREA arrives through USING, not through CICS
        if re.match(r"\s*PROCEDURE DIVISION\.\s*$", line, re.I):
            line = "       PROCEDURE DIVISION USING DFHCOMMAREA."
        out.append(line)
        if re.match(r"\s*WORKING-STORAGE SECTION\.\s*$", line, re.I):
            out.append("")
            out.extend(EIB_BLOCK)
            out.append("__DECL_MARK__")
        i += 1

    decl = [SAVE_LINE] if uses_link else []
    marker = out.index("__DECL_MARK__")
    out[marker:marker + 1] = decl

    text = "\r\n".join(out)
    # checks
    leftover = [l for l in out if len(l) > 6 and l[6] != "*" and re.match(r"\s*EXEC\s", l, re.I)]
    if leftover:
        fail(name + ": EXEC left: " + leftover[0])
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
