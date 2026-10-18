#!/bin/bash
# Compares runs/lgipol01-cikti.txt with expectations built ONLY from
# tests/predictions/05-motor-inquire.md. Usage (from tests/gnucobol): bash runs/lgipol01-karsilastir.sh
# Date, timestamp and manufacture-date fields are NOT compared (the prediction says belirsiz).
L="${1:-runs/lgipol01-cikti.txt}"
sp() { printf '%*s' "$1" ''; }
pass=0; fail=0
chk() { # name, expected, actual
  if [ "$2" == "$3" ]; then pass=$((pass+1)); echo "OK    $1"; else fail=$((fail+1)); echo "FAIL  $1"; echo "      expected=[$2]"; echo "      actual  =[$3]"; fi; }
blk() { awk -v c="=================== $1" '$0==c{f=1;next} /^===================/{f=0} f' "$L"; }
get() { # case, label -> content between [ and ] of the AFTER line
  blk "$1" | grep -F "$2" | head -1 | sed 's/^[^[]*\[//; s/\]$//'; }
msgs() { blk "$1" | grep 'STUB LGSTSQ' | sed 's/^[^[]*\[//; s/\]$//'; }
has() { blk "$1" | grep -c -F "$2"; }

COMMON_SELECT='STUB EXEC SQL SELECT FROM POLICY,MOTOR'
LINKTXT='STUB EXEC CICS LINK PROGRAM(LGIPDB01) LENGTH(32500)'

# every case: process ended normally, rig did not hit an unsupported statement
for c in C0501 C0502 C0503 C0504 C0505 C0506 C0507 C0508 C0509 D0510 C0511A C0511B C0511C D0512A D0512B D0513 D0514 D0515 C0516 C0517 C0519 C0520A C0520B; do
  chk "$c exit=0" 1 "$(has $c '[exit=0]')"
  chk "$c no RIG ERROR" 0 "$(has $c 'RIG ERROR')"
done

# "05-01 beklenen değerler" without the date/time fields and without return code
v1() { # case, tag
  local c=$1 t=$2
  chk "$t BROKERID"     "0000000000"    "$(get $c 'BROKERID')"
  chk "$t BROKERSREF"   "$(sp 10)"      "$(get $c 'BROKERSREF')"
  chk "$t PAYMENT"      "000000"        "$(get $c 'PAYMENT')"
  chk "$t M-MAKE"       "FORD$(sp 11)"  "$(get $c 'M-MAKE')"
  chk "$t M-MODEL"      "KA$(sp 13)"    "$(get $c 'M-MODEL')"
  chk "$t M-VALUE"      "008500"        "$(get $c 'M-VALUE')"
  chk "$t M-REGNUMBER"  "LL60LOO"       "$(get $c 'M-REGNUMBER')"
  chk "$t M-COLOUR"     "ORANGE$(sp 2)" "$(get $c 'M-COLOUR')"
  chk "$t M-CC"         "1000"          "$(get $c 'M-CC')"
  chk "$t M-PREMIUM"    "000450"        "$(get $c 'M-PREMIUM')"
  chk "$t M-ACCIDENTS"  "000000"        "$(get $c 'M-ACCIDENTS')"
  chk "$t FINAL mark"   "FINAL"         "$(get $c 'FINAL-MARK')"
  chk "$t bytes 183-32500 untouched" 1  "$(has $c 'bytes 183-32500 = all spaces')"
}
# 05-02 values (policy 3, customer 5)
v3() { # case, tag
  local c=$1 t=$2
  chk "$t BROKERID"     "0000000000"      "$(get $c 'BROKERID')"
  chk "$t BROKERSREF"   "$(sp 10)"        "$(get $c 'BROKERSREF')"
  chk "$t PAYMENT"      "000000"          "$(get $c 'PAYMENT')"
  chk "$t M-MAKE"       "DENNIS$(sp 9)"   "$(get $c 'M-MAKE')"
  chk "$t M-MODEL"      "ENGINE$(sp 9)"   "$(get $c 'M-MODEL')"
  chk "$t M-VALUE"      "023500"          "$(get $c 'M-VALUE')"
  chk "$t M-REGNUMBER"  "FIRE1$(sp 2)"    "$(get $c 'M-REGNUMBER')"
  chk "$t M-COLOUR"     "RED$(sp 5)"      "$(get $c 'M-COLOUR')"
  chk "$t M-CC"         "5000"            "$(get $c 'M-CC')"
  chk "$t M-PREMIUM"    "002850"          "$(get $c 'M-PREMIUM')"
  chk "$t M-ACCIDENTS"  "000012"          "$(get $c 'M-ACCIDENTS')"
  chk "$t FINAL mark"   "FINAL"           "$(get $c 'FINAL-MARK')"
  chk "$t bytes 183-32500 untouched" 1    "$(has $c 'bytes 183-32500 = all spaces')"
}
inputs() { # case, tag, request-id, cust, pol: bytes 1-6, 9-18, 19-28 as expected
  chk "$2 REQUEST-ID"   "$3" "$(get $1 'REQUEST-ID')"
  chk "$2 CUSTOMER-NUM" "$4" "$(get $1 'CUSTOMER-NUM')"
  chk "$2 POLICY-NUM"   "$5" "$(get $1 'POLICY-NUM')"
}
nomsg() { chk "$2 no LGSTSQ message" 0 "$(has $1 'STUB LGSTSQ')"; }
untouched() { chk "$2 bytes 29-32500 untouched, no FINAL" 1 "$(has $1 'AFTER  bytes 29-32500 = all spaces')"; }
selects() { chk "$2 SELECT run $3 time(s)" "$3" "$(has $1 "$COMMON_SELECT")"; }
chained() { chk "$2 LGIPOL01 linked LGIPDB01" 1 "$(has $1 "$LINKTXT")"; }

echo "--- 05-01 (C0501)"; chk "rc" "00" "$(get C0501 RETURN-CODE)"; v1 C0501 05-01
inputs C0501 05-01 01IMOT 0000000002 0000000001; nomsg C0501 05-01; chained C0501 05-01; selects C0501 05-01 1
chk "05-01 no ABEND" "N" "$(get C0501 ABEND-FLAG)"
echo "(05-01 dates not scored: prediction belirsiz; actual ISSUE-DATE=[$(get C0501 ISSUE-DATE)] EXPIRY-DATE=[$(get C0501 EXPIRY-DATE)] LASTCHANGED=[$(get C0501 LASTCHANGED)] M-MANUFACTURED=[$(get C0501 M-MANUFACTURED)])"
echo "--- 05-02 (C0502)"; chk "rc" "00" "$(get C0502 RETURN-CODE)"; v3 C0502 05-02
inputs C0502 05-02 01IMOT 0000000005 0000000003; nomsg C0502 05-02
echo "(05-02 dates not scored: prediction belirsiz; actual ISSUE-DATE=[$(get C0502 ISSUE-DATE)] EXPIRY-DATE=[$(get C0502 EXPIRY-DATE)] LASTCHANGED=[$(get C0502 LASTCHANGED)] M-MANUFACTURED=[$(get C0502 M-MANUFACTURED)])"
for t in "C0503 05-03 0000000002 0000000999" "C0504 05-04 0000000003 0000000001" "C0505 05-05 0000000008 0000000004"; do
  set -- $t
  echo "--- $2 ($1)"; chk "rc" "01" "$(get $1 RETURN-CODE)"; untouched $1 $2; nomsg $1 $2
  inputs $1 $2 01IMOT $3 $4; selects $1 $2 1
  chk "$2 FINAL mark not written" "$(sp 5)" "$(get $1 'FINAL-MARK')"
done
echo "--- 05-06 (C0506)"; chk "rc" "90" "$(get C0506 RETURN-CODE)"; untouched C0506 05-06
chk "05-06 FINAL mark not written" "$(sp 5)" "$(get C0506 'FINAL-MARK')"
m=$(msgs C0506); m1=$(echo "$m" | sed -n 1p); m2=$(echo "$m" | sed -n 2p)
chk "05-06 msg count" 2 "$(echo "$m" | wc -l)"
chk "05-06 msg1 (87 bytes, date/time blanked by stub)" "$(sp 15) LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR $(sp 2) SQLCODE=-00904" "$m1"
chk "05-06 msg1 length" 87 "${#m1}"
chk "05-06 msg2 ('COMMAREA=' + first 90 bytes)" "COMMAREA=01IMOT900000000002""0000000001$(sp 62)" "$m2"
chk "05-06 msg2 length" 99 "${#m2}"
echo "--- 05-07 (C0507)"; chk "rc" "00" "$(get C0507 RETURN-CODE)"; v1 C0507 05-07
inputs C0507 05-07 01imot 0000000002 0000000001; nomsg C0507 05-07
echo "--- 05-08 (C0508)"; chk "rc" "99" "$(get C0508 RETURN-CODE)"; chk "no SELECT" 0 "$(has C0508 'STUB EXEC SQL')"
untouched C0508 05-08; nomsg C0508 05-08; inputs C0508 05-08 01IXXX 0000000002 0000000001
echo "--- 05-09 (C0509)"; m=$(msgs C0509); chk "msg count" 1 "$(echo "$m" | wc -l)"
chk "05-09 msg (45 bytes)" "$(sp 15) LGIPOL01 NO COMMAREA RECEIVED" "$m"; chk "05-09 msg length" 45 "${#m}"
chk "05-09 ABEND LGCA shown" 1 "$(has C0509 'STUB EXEC CICS ABEND ABCODE(LGCA)')"
chk "05-09 LGIPDB01 not linked" 0 "$(has C0509 "$LINKTXT")"; chk "05-09 no SELECT" 0 "$(has C0509 'STUB EXEC SQL')"
chk "05-09 buffer unchanged" 1 "$(has C0509 'AFTER  bytes 1-32500 = all spaces')"
echo "--- 05-10 (D0510)"; m=$(msgs D0510); chk "msg count" 1 "$(echo "$m" | wc -l)"
chk "05-10 msg (87 bytes, B2 +00000)" "$(sp 15) LGIPDB01 NO COMMAREA RECEIVED$(sp 36)+00000" "$m"; chk "05-10 msg length" 87 "${#m}"
chk "05-10 ABEND LGCA shown" 1 "$(has D0510 'STUB EXEC CICS ABEND ABCODE(LGCA)')"; chk "05-10 no SELECT" 0 "$(has D0510 'STUB EXEC SQL')"
chk "05-10 buffer unchanged" 1 "$(has D0510 'AFTER  bytes 1-32500 = all spaces')"
for c in C0511A C0511B C0511C; do echo "--- 05-11 ($c)"; chk "rc" "00" "$(get $c RETURN-CODE)"; v1 $c "05-11/$c"
  inputs $c "05-11/$c" 01IMOT 0000000002 0000000001; nomsg $c "05-11/$c"; chained $c "05-11/$c"; selects $c "05-11/$c" 1; done
for c in D0512A D0512B; do echo "--- 05-12 ($c)"; chk "rc" "98" "$(get $c RETURN-CODE)"; selects $c "05-12/$c" 1
  untouched $c "05-12/$c"; chk "05-12/$c FINAL mark not written" "$(sp 5)" "$(get $c 'FINAL-MARK')"; nomsg $c "05-12/$c"; done
echo "--- 05-13 (D0513)"; chk "rc" "00" "$(get D0513 RETURN-CODE)"; v1 D0513 05-13; nomsg D0513 05-13
echo "--- 05-14 (D0514)"; chk "rc" "01" "$(get D0514 RETURN-CODE)"; selects D0514 05-14 1; untouched D0514 05-14; nomsg D0514 05-14
echo "--- 05-15 (D0515)"; chk "rc" "90" "$(get D0515 RETURN-CODE)"; untouched D0515 05-15
m=$(msgs D0515); m1=$(echo "$m" | sed -n 1p); m2=$(echo "$m" | sed -n 2p)
chk "05-15 msg count" 2 "$(echo "$m" | wc -l)"
chk "05-15 msg1 (same as 05-06 msg1)" "$(sp 15) LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR $(sp 2) SQLCODE=-00904" "$m1"
chk "05-15 msg2 ('COMMAREA=' + first 50 bytes + 40 spaces)" "COMMAREA=01IMOT900000000002""0000000001$(sp 62)" "$m2"
chk "05-15 msg2 length" 99 "${#m2}"
echo "(05-15: which branch ran (< 91 or ELSE) cannot be told from the output: not scored)"
echo "--- 05-16 (C0516)"; chk "rc" "00" "$(get C0516 RETURN-CODE)"; v1 C0516 05-16; nomsg C0516 05-16
echo "--- 05-17 (C0517)"; chk "rc" "00" "$(get C0517 RETURN-CODE)"; v1 C0517 05-17; nomsg C0517 05-17
echo "--- 05-19 (C0519)"; chk "rc" "00" "$(get C0519 RETURN-CODE)"
chk "05-19 SELECT got customer 2 (B3)" 1 "$(has C0519 'CUSTOMERNUMBER=+000000002')"
v1 C0519 05-19; inputs C0519 05-19 01IMOT 1000000002 0000000001; nomsg C0519 05-19
echo "--- 05-20 (C0520A, C0520B): NOT scored (prediction belirsiz); actual:"
for c in C0520A C0520B; do echo "  $c rc=[$(get $c RETURN-CODE)] BEFORE=[$(blk $c | grep 'BEFORE bytes 1-28' | sed 's/^[^[]*\[//; s/\]$//')]"; blk $c | grep -E 'STUB EXEC SQL|STUB DB2MOTOR' | sed 's/^/    /'; done
echo; echo "PASS=$pass FAIL=$fail"
