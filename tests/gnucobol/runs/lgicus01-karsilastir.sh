#!/bin/bash
# Compares runs/lgicus01-cikti.txt with expectations built ONLY from
# tests/predictions/02-customer-inquire.md. Usage (from tests/gnucobol): bash runs/lgicus01-karsilastir.sh
L="${1:-runs/lgicus01-cikti.txt}"
sp() { printf '%*s' "$1" ''; }
pass=0; fail=0
chk() { # name, expected, actual
  if [ "$2" == "$3" ]; then pass=$((pass+1)); echo "OK    $1"; else fail=$((fail+1)); echo "FAIL  $1"; echo "      expected=[$2]"; echo "      actual  =[$3]"; fi; }
get() { # case, label-prefix  -> content between [ and ] of the AFTER line
  awk -v c="=================== $1" '$0==c{f=1;next} /^===================/{f=0} f' "$L" | grep -F "$2" | head -1 | sed 's/^[^[]*\[//; s/\]$//'; }
msgs() { awk -v c="=================== $1" '$0==c{f=1;next} /^===================/{f=0} f' "$L" | grep 'STUB LGSTSQ' | sed 's/^[^[]*\[//; s/\]$//'; }
has() { awk -v c="=================== $1" '$0==c{f=1;next} /^===================/{f=0} f' "$L" | grep -c -F "$2"; }

cust1() { # 02-01 expected values (prediction table "02-01 beklenen değerler")
  local c=$1 t=$2
  chk "$t FIRST-NAME"   "Andrew$(sp 4)"  "$(get $c 'FIRST-NAME')"
  chk "$t LAST-NAME"    "Pandy$(sp 15)"  "$(get $c 'LAST-NAME')"
  chk "$t HOUSE-NAME"   "$(sp 20)"       "$(get $c 'HOUSE-NAME')"
  chk "$t HOUSE-NUM"    "34$(sp 2)"      "$(get $c 'HOUSE-NUM')"
  chk "$t POSTCODE"     "PI101OO$(sp 1)" "$(get $c 'POSTCODE')"
  chk "$t PHONE-MOBILE" "07799 123456$(sp 8)" "$(get $c 'PHONE-MOBILE')"
  chk "$t PHONE-HOME"   "01962 811234$(sp 8)" "$(get $c 'PHONE-HOME')"
  chk "$t EMAIL"        "A.Pandy@beebhouse.com$(sp 79)" "$(get $c 'EMAIL')"
}
echo "--- 02-01 (C0201)"; chk "rc" "00" "$(get C0201 RETURN-CODE)"; cust1 C0201 02-01
chk "02-01 NUM-POLICIES" "000" "$(get C0201 NUM-POLICIES)"; chk "02-01 REQUEST-ID" "01ICUS" "$(get C0201 REQUEST-ID)"
chk "02-01 CUSTOMER-NUM" "0000000001" "$(get C0201 CUSTOMER-NUM)"; chk "02-01 POLICY-DATA unchanged" 1 "$(has C0201 'POLICY-DATA = all spaces')"
echo "(02-01 DOB not scored: prediction belirsiz; actual [$(get C0201 DOB)])"
echo "--- 02-02 (C0202)"; chk "rc" "01" "$(get C0202 RETURN-CODE)"; chk "NUM-POLICIES" "000" "$(get C0202 NUM-POLICIES)"; chk "no error message" 0 "$(has C0202 'STUB LGSTSQ')"
echo "--- 02-03 (C0203)"; chk "rc" "01" "$(get C0203 RETURN-CODE)"; chk "NUM-POLICIES" "000" "$(get C0203 NUM-POLICIES)"; chk "no error message" 0 "$(has C0203 'STUB LGSTSQ')"
echo "--- 02-04 (C0204)"; chk "rc" "90" "$(get C0204 RETURN-CODE)"
m=$(msgs C0204); m1=$(echo "$m" | sed -n 1p); m2=$(echo "$m" | sed -n 2p)
chk "msg count" 2 "$(echo "$m" | wc -l)"
chk "msg1 (71 bytes, date/time blanked by stub)" "$(sp 15) LGICUS01 CNUM=0000000001$(sp 16) SQLCODE=-00204" "$m1"
chk "msg1 length" 71 "${#m1}"
chk "msg2 ('COMMAREA=' + first 90 bytes)" "COMMAREA=01ICUS900000000001$(sp 72)" "$m2"
chk "msg2 length" 99 "${#m2}"
chk "NUM-POLICIES" "000" "$(get C0204 NUM-POLICIES)"
echo "--- 02-05 (C0205)"; m=$(msgs C0205); chk "msg count" 1 "$(echo "$m" | wc -l)"
chk "msg (45 bytes)" "$(sp 15) LGICUS01 NO COMMAREA RECEIVED" "$m"; chk "msg length" 45 "${#m}"
chk "ABEND LGCA shown" 1 "$(has C0205 'STUB EXEC CICS ABEND ABCODE(LGCA)')"; chk "LGICDB01 not called (no SELECT)" 0 "$(has C0205 'STUB EXEC SQL')"
chk "buffer unchanged (rc)" "$(sp 2)" "$(get C0205 RETURN-CODE)"
for c in C0206A C0206B; do echo "--- 02-06 ($c)"; chk "rc" "98" "$(get $c RETURN-CODE)"; chk "no SELECT" 0 "$(has $c 'STUB EXEC SQL')"; chk "no message" 0 "$(has $c 'STUB LGSTSQ')"; chk "B1 bytes 91-93" "000" "$(get $c NUM-POLICIES)"; chk "FIRST-NAME not filled" "$(sp 10)" "$(get $c FIRST-NAME)"; done
for c in C0207A C0207B; do echo "--- 02-07 ($c)"; chk "B1 bytes 7-8" "98" "$(get $c RETURN-CODE)"; chk "no SELECT" 0 "$(has $c 'STUB EXEC SQL')"; chk "no message" 0 "$(has $c 'STUB LGSTSQ')"; chk "B1 bytes 91-93" "000" "$(get $c NUM-POLICIES)"; done
chk "02-07 EIBCALEN=7: byte 7 is 9" "9" "$(get C0207A RETURN-CODE | cut -c1)"
chk "02-07 bytes 1-6 unchanged (C0207A)" "01ICUS" "$(get C0207A REQUEST-ID)"
chk "02-07 bytes 1-6 unchanged (C0207B, before = 0+spaces)" "0$(sp 5)" "$(get C0207B REQUEST-ID)"
echo "--- 02-08 (C0208)"; chk "rc" "00" "$(get C0208 RETURN-CODE)"; cust1 C0208 02-08; chk "B1 NUM-POLICIES" "000" "$(get C0208 NUM-POLICIES)"
echo "--- 02-09 (D0209)"; m=$(msgs D0209); chk "msg count" 1 "$(echo "$m" | wc -l)"
chk "msg (71 bytes, B2 +00000)" "$(sp 15) LGICUS01 NO COMMAREA RECEIVED$(sp 20)+00000" "$m"; chk "msg length" 71 "${#m}"
chk "ABEND shown" 1 "$(has D0209 'STUB EXEC CICS ABEND ABCODE(LGCA)')"; chk "no SELECT" 0 "$(has D0209 'STUB EXEC SQL')"
for c in D0210A D0210B; do echo "--- 02-10 ($c)"; chk "rc" "98" "$(get $c RETURN-CODE)"; chk "no SELECT" 0 "$(has $c 'STUB EXEC SQL')"; chk "NUM-POLICIES untouched" "$(sp 3)" "$(get $c NUM-POLICIES)"; chk "FIRST-NAME not filled" "$(sp 10)" "$(get $c FIRST-NAME)"; done
echo "--- 02-11 (D0211)"; chk "rc" "00" "$(get D0211 RETURN-CODE)"; cust1 D0211 02-11; chk "NUM-POLICIES unchanged (777)" "777" "$(get D0211 NUM-POLICIES)"
echo "--- 02-12 (C0212)"; chk "rc" "00" "$(get C0212 RETURN-CODE)"; cust1 C0212 02-12; chk "NUM-POLICIES" "000" "$(get C0212 NUM-POLICIES)"
echo "--- 02-13 (C0213)"; chk "rc" "00" "$(get C0213 RETURN-CODE)"; chk "SELECT used customer 1" 1 "$(has C0213 'CUSTOMERNUMBER=+000000001')"; cust1 C0213 02-13; chk "CUSTOMER-NUM stays" "1000000001" "$(get C0213 CUSTOMER-NUM)"
echo "--- 02-17 (D0217)"; chk "rc" "90" "$(get D0217 RETURN-CODE)"; chk "NUM-POLICIES untouched" "$(sp 3)" "$(get D0217 NUM-POLICIES)"
m=$(msgs D0217); m1=$(echo "$m" | sed -n 1p); m2=$(echo "$m" | sed -n 2p); chk "msg count" 2 "$(echo "$m" | wc -l)"
chk "msg1" "$(sp 15) LGICUS01 CNUM=0000000001$(sp 16) SQLCODE=-00204" "$m1"
chk "msg2 ('COMMAREA=' + all 90 bytes)" "COMMAREA=01ICUS900000000001$(sp 72)" "$m2"; chk "msg2 length" 99 "${#m2}"
echo; echo "PASS=$pass FAIL=$fail"
