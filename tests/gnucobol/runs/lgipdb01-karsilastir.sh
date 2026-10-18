#!/bin/bash
# Compares runs/lgipdb01-cikti.txt with expectations built ONLY from
# tests/predictions/05-motor-inquire.md (rows 05-01, 05-03, 05-06) and the rules of runs/lgipdb01.md.
# Usage (from tests/gnucobol): bash runs/lgipdb01-karsilastir.sh
# Data cells are NOT compared with SQLCODE 0: the stub fills no host variables (rule 1).
L="${1:-runs/lgipdb01-cikti.txt}"
sp() { printf '%*s' "$1" ''; }
pass=0; fail=0
chk() { # name, expected, actual
  if [ "$2" == "$3" ]; then pass=$((pass+1)); echo "OK    $1"; else fail=$((fail+1)); echo "FAIL  $1"; echo "      expected=[$2]"; echo "      actual  =[$3]"; fi; }
blk() { awk -v c="=================== $1" '$0==c{f=1;next} /^===================/{f=0} f' "$L"; }
get() { blk "$1" | grep -F "$2" | head -1 | sed 's/^[^[]*\[//; s/\]$//'; }
msgs() { blk "$1" | grep 'STUB LGSTSQ' | sed 's/^[^[]*\[//; s/\]$//'; }
has() { blk "$1" | grep -c -F "$2"; }
fmt() { # SQLCODE -> PIC +9(5) text
  local n=$1; local s='+'; case "$n" in -*) s='-'; n=${n#-};; +*) n=${n#+};; esac
  printf '%s%05d' "$s" "$n"; }

SEL='STUB EXEC SQL SELECT FROM POLICY,MOTOR'
CASES_ALL=("01IMOT 0" "01IMOT 100" "01IMOT -904" "01IMOT -305" "01IMOT -803" "01IMOT -811" "01IMOT -911" "01IMOT -913" "01IMOT -922" "01IMOT -30081" "01IMOT +304"
  "01IEND 0" "01IEND 100" "01IEND -904" "01IHOU 0" "01IHOU 100" "01IHOU -904" "01ICOM 0" "01ICOM 100" "01ICOM -904" "03ICOM 0" "03ICOM 100" "03ICOM -904")   # the 23 cases planned before the run
for c in "${CASES_ALL[@]}"; do chk "[$c] process ended normally" 1 "$(has "$c" '[exit=0]')"; done

c="01IMOT 0"; echo "--- 05-01 akış iddiaları (SQLCODE 0): [$c]"
chk "rc" "00" "$(get "$c" RETURN-CODE)"
chk "FINAL mark at 178-182" "FINAL" "$(get "$c" FINAL-MARK)"
chk "bytes 183-32500 untouched" 1 "$(has "$c" 'bytes 183-32500 = all spaces')"
chk "REQUEST-ID unchanged" "01IMOT" "$(get "$c" REQUEST-ID)"
chk "CUSTOMER-NUM unchanged" "0000000002" "$(get "$c" CUSTOMER-NUM)"
chk "POLICY-NUM unchanged" "0000000001" "$(get "$c" POLICY-NUM)"
chk "no LGSTSQ message" 0 "$(has "$c" 'STUB LGSTSQ')"
chk "SELECT run once" 1 "$(has "$c" "$SEL")"
chk "no ABEND" "N" "$(get "$c" ABEND-FLAG)"

c="01IMOT 100"; echo "--- 05-03 (SQLCODE 100): [$c]"
chk "rc" "01" "$(get "$c" RETURN-CODE)"
chk "bytes 29-32500 untouched" 1 "$(has "$c" 'bytes 29-32500 = all spaces')"
chk "FINAL not written" "$(sp 5)" "$(get "$c" FINAL-MARK)"
chk "no LGSTSQ message" 0 "$(has "$c" 'STUB LGSTSQ')"
chk "SELECT run once" 1 "$(has "$c" "$SEL")"
chk "REQUEST-ID unchanged" "01IMOT" "$(get "$c" REQUEST-ID)"

for n in -904 -305 -803 -811 -911 -913 -922 -30081 +304; do
  c="01IMOT $n"; echo "--- 05-06 kuralı (SQLCODE $n): [$c]"
  chk "$n rc" "90" "$(get "$c" RETURN-CODE)"
  chk "$n bytes 29-32500 untouched" 1 "$(has "$c" 'bytes 29-32500 = all spaces')"
  chk "$n FINAL not written" "$(sp 5)" "$(get "$c" FINAL-MARK)"
  chk "$n SELECT run once" 1 "$(has "$c" "$SEL")"
  m=$(msgs "$c"); m1=$(echo "$m" | sed -n 1p); m2=$(echo "$m" | sed -n 2p)
  chk "$n msg count" 2 "$(echo "$m" | wc -l)"
  chk "$n msg1 (87 bytes, date/time blanked by stub)" "$(sp 15) LGIPDB01 CNUM=0000000002 PNUM=0000000001 SELECT MOTOR $(sp 2) SQLCODE=$(fmt $n)" "$m1"
  chk "$n msg1 length" 87 "${#m1}"
  chk "$n msg2 ('COMMAREA=' + first 90 bytes)" "COMMAREA=01IMOT900000000002""0000000001$(sp 62)" "$m2"
  chk "$n msg2 length" 99 "${#m2}"
done

echo "--- Bilgi için (puanlanmaz): motor dışı kollar"
for c in "01IEND 0" "01IEND 100" "01IEND -904" "01IHOU 0" "01IHOU 100" "01IHOU -904" "01ICOM 0" "01ICOM 100" "01ICOM -904" "03ICOM 0" "03ICOM 100" "03ICOM -904" "02ICOM 0" "02ICOM 100" "02ICOM -904" "05ICOM 0" "05ICOM 100" "05ICOM -904"; do
  echo "  [$c] rc=[$(get "$c" RETURN-CODE)] SQL stubs run=$(has "$c" 'STUB DB2STUB sets') messages=$(has "$c" 'STUB LGSTSQ') $(blk "$c" | grep -o 'exit=[0-9]*') firstmsg=[$(msgs "$c" | sed -n 1p | cut -c16-60)]"
done
echo; echo "PASS=$pass FAIL=$fail"
