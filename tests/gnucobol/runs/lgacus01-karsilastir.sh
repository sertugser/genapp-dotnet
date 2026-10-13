#!/bin/bash
# Compares runs/lgacus01-cikti.txt with expectations built ONLY from
# tests/predictions/01-customer-add.md (the "01-01 girdi ve beklenen degerler" table,
# the rows 01-01 ... 01-25 and the code observations they point to).
# Usage (from tests/gnucobol): bash runs/lgacus01-karsilastir.sh
# Output: one OK/FAIL line per check, then one verdict per prediction row.
L="${1:-runs/lgacus01-cikti.txt}"
sp() { printf '%*s' "$1" ''; }
declare -A RP RF            # per-row pass / fail counters
CUR=""
begin() { CUR="$1"; RP[$CUR]=${RP[$CUR]:-0}; RF[$CUR]=${RF[$CUR]:-0}; echo "--- $1 ($2)"; }
chk() { # name, expected, actual
  if [ "$2" == "$3" ]; then RP[$CUR]=$((RP[$CUR]+1)); echo "OK    $1"
  else RF[$CUR]=$((RF[$CUR]+1)); echo "FAIL  $1"; echo "      expected=[$2]"; echo "      actual  =[$3]"; fi; }
sec() { awk -v c="=================== $1" '$0==c{f=1;next} /^===================/{f=0} f' "$L" | tr -d '\r'; }
get() { sec "$1" | grep '^AFTER' | grep -F "$2" | head -1 | sed 's/^[^[]*\[//; s/\]$//'; }        # AFTER line, between [ ]
sget() { sec "$1" | grep -F "$2=[" | head -1 | sed 's/^[^[]*\[//; s/\]$//'; }     # stub line "X=[...]"
nget() { sec "$1" | grep -F "$2=" | head -1 | sed 's/^.*=//'; }                   # stub line "X=+0000"
msgs() { sec "$1" | grep 'STUB LGSTSQ' | sed 's/^[^[]*\[//; s/\]$//'; }
has() { sec "$1" | grep -c -F "$2"; }
hasre() { sec "$1" | grep -c -E "$2"; }
links() { sec "$1" | grep -o 'STUB EXEC CICS LINK PROGRAM([A-Z0-9]*)' | sed 's/.*(\(.*\))/\1/' | tr '\n' ' '; }
ex() { sec "$1" | grep -c -F '[exit=0]'; }

# ---- values from the prediction ("01-01 girdi ve beklenen degerler") ----
FN="Andrew$(sp 4)"; LN="Pandy$(sp 15)"; DOB="1950-07-11"; HN="$(sp 20)"; HNUM="34$(sp 2)"; PC="PI101OO$(sp 1)"
MOB="07799 123456$(sp 8)"; HOME="01962 811234$(sp 8)"; EMAIL="A.Pandy@beebhouse.com$(sp 79)"
PASS="5732fec825535eeafb8fac50fee3a8aa"
K="0000000011"                      # B7
# first 90 bytes of the customer COMMAREA: request id, rc, customer no, names ... postcode
ca90() { printf '%s' "01ACUS$1$2${FN}${LN}${DOB}${HN}${HNUM}${PC}"; }
# 225-byte KSDSCUST record = COMMAREA bytes 9-233 (Kod gozlemleri 11)
vrec() { # key numpol first last mobile home email
  printf '%s' "$1${3-$FN}${4-$LN}${DOB}${HN}${HNUM}${PC}$2${5-$MOB}${6-$HOME}${7-$EMAIL}"; }
# LGSTSQ message 1 of LGACDB01 / LGACDB02 (71 bytes); date and time are blank in the rig
m1_db01() { printf '%s' "$(sp 15) LGACDB01 CNUM=$(sp 10) INSERT CUSTOMER SQLCODE=$1"; }
m1_db02() { printf '%s' "$(sp 15) LGACDB02 CNUM=$(sp 10) INSERT SECURITY SQLCODE=$1"; }
m1_vs01() { printf '%s' "$(sp 15) LGACVS01 CNUM=$1 Write file KSDSCUST RESP=+00014 RESP2=+00002"; }
m_noca() { printf '%s' "$(sp 15) $1 NO COMMAREA RECEIVED$2"; }

inserts_ok() { # case, customer-number-text, first, last, mob, home, email   (CUSTOMER INSERT inputs)
  local c=$1
  chk "INSC number" "$2" "$(nget $c 'STUB DB2INSC CUSTOMERNUMBER')"
  chk "INSC FIRSTNAME" "${3-$FN}" "$(sget $c 'STUB DB2INSC FIRSTNAME')"
  chk "INSC LASTNAME" "${4-$LN}" "$(sget $c 'STUB DB2INSC LASTNAME')"
  chk "INSC DATEOFBIRTH" "$DOB" "$(sget $c 'STUB DB2INSC DATEOFBIRTH')"
  chk "INSC HOUSENAME" "$HN" "$(sget $c 'STUB DB2INSC HOUSENAME')"
  chk "INSC HOUSENUMBER" "$HNUM" "$(sget $c 'STUB DB2INSC HOUSENUMBER')"
  chk "INSC POSTCODE" "$PC" "$(sget $c 'STUB DB2INSC POSTCODE')"
  chk "INSC PHONEMOBILE (mobile column gets the mobile value)" "${5-$MOB}" "$(sget $c 'STUB DB2INSC PHONEMOBILE')"
  chk "INSC PHONEHOME (home column gets the home value)" "${6-$HOME}" "$(sget $c 'STUB DB2INSC PHONEHOME')"
  chk "INSC EMAILADDRESS" "${7-$EMAIL}" "$(sget $c 'STUB DB2INSC EMAILADDRESS')"
}
unchanged_fields() { # case  (nine customer fields, request id, policy data unchanged)
  local c=$1
  chk "FIRST-NAME" "$FN" "$(get $c FIRST-NAME)"; chk "LAST-NAME" "$LN" "$(get $c LAST-NAME)"
  chk "DOB" "$DOB" "$(get $c DOB)"; chk "HOUSE-NAME" "$HN" "$(get $c HOUSE-NAME)"
  chk "HOUSE-NUM" "$HNUM" "$(get $c HOUSE-NUM)"; chk "POSTCODE" "$PC" "$(get $c POSTCODE)"
  chk "PHONE-MOBILE" "$MOB" "$(get $c PHONE-MOBILE)"; chk "PHONE-HOME" "$HOME" "$(get $c PHONE-HOME)"
  chk "EMAIL" "$EMAIL" "$(get $c EMAIL)"
  chk "POLICY-DATA unchanged" 1 "$(has $c 'POLICY-DATA = all spaces')"
}
chain_ok() { # case, customer-number-text, vsam record   (VSAM write and CUSTOMER_SECURE insert)
  local c=$1 rec
  chk "link order" "LGACDB01 LGACVS01 LGACDB02 " "$(links $c)"
  rec=$(sget $c 'STUB CICSWRF record')
  chk "VSAM key" "$(printf '%s' "$2" | sed 's/^+//')" "$(sget $c 'STUB CICSWRF key')"
  chk "VSAM record length 225" 225 "${#rec}"
  chk "VSAM record" "$3" "$rec"
  chk "VSAM record last 8 bytes blank (B1)" "$(sp 8)" "${rec: -8}"
  chk "SECURE customerNumber" "+${2: -9}" "$(nget $c 'STUB DB2INSS customerNumber')"
  chk "SECURE customerPass" "$PASS" "$(sget $c 'STUB DB2INSS customerPass')"
  chk "SECURE state_indicator" "N" "$(sget $c 'STUB DB2INSS state_indicator')"
  chk "SECURE pass_changes" "+000000000" "$(nget $c 'STUB DB2INSS pass_changes')"
}

# =============================== 01-01 ===============================
begin 01-01 C0101
chk "rc" "00" "$(get C0101 RETURN-CODE)"
chk "CUSTOMER-NUM = K (B7)" "$K" "$(get C0101 CUSTOMER-NUM)"
chk "NUM-POLICIES" "000" "$(get C0101 NUM-POLICIES)"
chk "REQUEST-ID unchanged" "01ACUS" "$(get C0101 REQUEST-ID)"
unchanged_fields C0101
chk "counter called once" 1 "$(has C0101 'STUB CICSCNT answers')"
chk "numbered INSERT called once, DEFAULT INSERT not called" "1 0" "$(has C0101 'STUB DB2INSC answers') $(has C0101 'STUB DB2INSD answers')"
inserts_ok C0101 "+000000011"
chain_ok C0101 "$K" "$(vrec $K 000)"
chk "no LGSTSQ message" 0 "$(has C0101 'STUB LGSTSQ')"
# =============================== 01-02 ===============================
begin 01-02 "C0102, C0102B"
chk "rc" "00" "$(get C0102 RETURN-CODE)"
chk "CUSTOMER-NUM = DEFAULT identity (B6)" "0001000001" "$(get C0102 CUSTOMER-NUM)"
chk "no LGSTSQ message" 0 "$(has C0102 'STUB LGSTSQ')"
chk "DEFAULT INSERT called once, numbered INSERT not called" "1 0" "$(has C0102 'STUB DB2INSD answers') $(has C0102 'STUB DB2INSC answers')"
chk "IDENTITY_VAL_LOCAL called once" 1 "$(has C0102 'STUB DB2IDENT answers')"
chain_ok C0102 "0001000001" "$(vrec 0001000001 000)"
chk "INSD FIRSTNAME" "$FN" "$(sget C0102 'STUB DB2INSD FIRSTNAME')"
chk "INSD PHONEMOBILE" "$MOB" "$(sget C0102 'STUB DB2INSD PHONEMOBILE')"
chk "INSD EMAILADDRESS" "$EMAIL" "$(sget C0102 'STUB DB2INSD EMAILADDRESS')"
echo "(C0102B: SET fails, SQLCODE is not checked)"
chk "rc" "00" "$(get C0102B RETURN-CODE)"
chk "SET answered -204" 1 "$(hasre C0102B 'DB2IDENT answers SQLCODE= *-204$')"
chk "no LGSTSQ message" 0 "$(has C0102B 'STUB LGSTSQ')"
chk "chain continued" "LGACDB01 LGACVS01 LGACDB02 " "$(links C0102B)"
chk "CUSTOMER-NUM stays 0 (Kod gozlemleri 17)" "0000000000" "$(get C0102B CUSTOMER-NUM)"
# =============================== 01-03 ===============================
begin 01-03 C0103
chk "rc" "90" "$(get C0103 RETURN-CODE)"
chk "CUSTOMER-NUM unchanged" "0000000000" "$(get C0103 CUSTOMER-NUM)"
chk "NUM-POLICIES" "000" "$(get C0103 NUM-POLICIES)"
chk "LGACVS01 and LGACDB02 not called" "LGACDB01 " "$(links C0103)"
m=$(msgs C0103); a=$(echo "$m" | sed -n 1p); b=$(echo "$m" | sed -n 2p)
chk "message count" 2 "$(echo "$m" | wc -l)"
chk "msg1 (71 bytes)" "$(m1_db01 -00803)" "$a"; chk "msg1 length" 71 "${#a}"
chk "msg2 ('COMMAREA=' + first 90 bytes, rc 90)" "COMMAREA=$(ca90 90 0000000000)" "$b"; chk "msg2 length" 99 "${#b}"
# =============================== 01-04 ===============================
begin 01-04 C0104
chk "rc" "90" "$(get C0104 RETURN-CODE)"
chk "CUSTOMER-NUM unchanged" "0000000000" "$(get C0104 CUSTOMER-NUM)"
chk "IDENTITY_VAL_LOCAL not called" 0 "$(has C0104 'STUB DB2IDENT')"
chk "LGACVS01 and LGACDB02 not called" "LGACDB01 " "$(links C0104)"
m=$(msgs C0104); a=$(echo "$m" | sed -n 1p); b=$(echo "$m" | sed -n 2p)
chk "message count" 2 "$(echo "$m" | wc -l)"
chk "msg1 (71 bytes)" "$(m1_db01 -00204)" "$a"; chk "msg1 length" 71 "${#a}"
chk "msg2" "COMMAREA=$(ca90 90 0000000000)" "$b"; chk "msg2 length" 99 "${#b}"
# =============================== 01-05 ===============================
begin 01-05 C0105
chk "ABEND LGV0 reached" 1 "$(has C0105 'STUB EXEC CICS ABEND ABCODE(LGV0)')"
chk "LGACDB02 not called" "LGACDB01 LGACVS01 " "$(links C0105)"
chk "no CUSTOMER_SECURE insert" 0 "$(has C0105 'STUB DB2INSS')"
m=$(msgs C0105); a=$(echo "$m" | sed -n 1p); b=$(echo "$m" | sed -n 2p)
chk "message count" 2 "$(echo "$m" | wc -l)"
chk "msg1 (85 bytes)" "$(m1_vs01 $K)" "$a"; chk "msg1 length" 85 "${#a}"
chk "msg2 ('COMMAREA=' + first 90 bytes, rc 80, K)" "COMMAREA=$(ca90 80 $K)" "$b"; chk "msg2 length" 99 "${#b}"
# =============================== 01-06 ===============================
begin 01-06 C0106
chk "final rc" "00" "$(get C0106 RETURN-CODE)"
chk "CUSTOMER-NUM = K" "$K" "$(get C0106 CUSTOMER-NUM)"
chk "CUSTOMER INSERT and VSAM write done, then SECURE insert" "LGACDB01 LGACVS01 LGACDB02 " "$(links C0106)"
chk "INSC called once" 1 "$(has C0106 'STUB DB2INSC answers')"
chk "SECURE insert called once and answered -204" 1 "$(hasre C0106 'DB2INSS answers SQLCODE= *-204$')"
m=$(msgs C0106); a=$(echo "$m" | sed -n 1p); b=$(echo "$m" | sed -n 2p)
chk "message count" 2 "$(echo "$m" | wc -l)"
chk "msg1 (71 bytes)" "$(m1_db02 -00204)" "$a"; chk "msg1 length" 71 "${#a}"
chk "msg2 (CDB2AREA: 02ACUS, 98, K, pass, 0000, N, 35 blanks (B5))" "COMMAREA=02ACUS98${K}${PASS}0000N$(sp 35)" "$b"; chk "msg2 length" 99 "${#b}"
# =============================== 01-07 ===============================
begin 01-07 C0107
chk "rc" "00" "$(get C0107 RETURN-CODE)"
chk "CUSTOMER-NUM overwritten with K" "$K" "$(get C0107 CUSTOMER-NUM)"
chk "INSERT used K, not the input number" "+000000011" "$(nget C0107 'STUB DB2INSC CUSTOMERNUMBER')"
unchanged_fields C0107
chain_ok C0107 "$K" "$(vrec $K 000)"
chk "no LGSTSQ message" 0 "$(has C0107 'STUB LGSTSQ')"
# =============================== 01-08 ===============================
begin 01-08 C0108
chk "rc" "00" "$(get C0108 RETURN-CODE)"
chk "CUSTOMER-NUM" "$K" "$(get C0108 CUSTOMER-NUM)"
inserts_ok C0108 "+000000011" "$(sp 10)" "$(sp 20)"
chain_ok C0108 "$K" "$(vrec $K 000 "$(sp 10)" "$(sp 20)")"
chk "no LGSTSQ message" 0 "$(has C0108 'STUB LGSTSQ')"
# =============================== 01-09 ===============================
begin 01-09 "C0109, not scored"
echo "(01-09 not scored: prediction belirsiz; actual rc=[$(get C0109 RETURN-CODE)] INSC DATEOFBIRTH=[$(sget C0109 'STUB DB2INSC DATEOFBIRTH')])"
# =============================== 01-10 ===============================
begin 01-10 C0110
chk "ABEND LGCA reached" 1 "$(has C0110 'STUB EXEC CICS ABEND ABCODE(LGCA)')"
chk "LGACDB01 not called" "" "$(links C0110)"
m=$(msgs C0110); chk "message count" 1 "$(echo "$m" | wc -l)"
chk "msg (45 bytes)" "$(m_noca LGACUS01 '')" "$m"; chk "msg length" 45 "${#m}"
# =============================== 01-11 ===============================
begin 01-11 "C0111A, C0111B"
for c in C0111A C0111B; do
  chk "$c rc" "98" "$(get $c RETURN-CODE)"; chk "$c LGACDB01 not called" "" "$(links $c)"
  chk "$c no message" 0 "$(has $c 'STUB LGSTSQ')"; chk "$c B1 bytes 91-93" "000" "$(get $c NUM-POLICIES)"
done
# =============================== 01-12 ===============================
begin 01-12 "C0112A, C0112B"
for c in C0112A C0112B; do
  chk "$c B1 bytes 7-8" "98" "$(get $c RETURN-CODE)"; chk "$c LGACDB01 not called" "" "$(links $c)"
  chk "$c no message" 0 "$(has $c 'STUB LGSTSQ')"; chk "$c B1 bytes 91-93" "000" "$(get $c NUM-POLICIES)"
done
chk "EIBCALEN=7: byte 7 is 9" "9" "$(get C0112A RETURN-CODE | cut -c1)"
chk "bytes 1-6 unchanged (EIBCALEN=7)" "01ACUS" "$(get C0112A REQUEST-ID)"
chk "bytes 1-6 unchanged (EIBCALEN=1: '0' + blanks)" "0$(sp 5)" "$(get C0112B REQUEST-ID)"
# =============================== 01-13 ===============================
begin 01-13 C0113
chk "rc (B1)" "00" "$(get C0113 RETURN-CODE)"
chk "LGACDB01 called with LENGTH(32500)" 1 "$(has C0113 'PROGRAM(LGACDB01) LENGTH(32500)')"
inserts_ok C0113 "+000000011" "$FN" "$LN" "$(sp 20)" "$(sp 20)" "$(sp 100)"
chain_ok C0113 "$K" "$(vrec $K 000 "$FN" "$LN" "$(sp 20)" "$(sp 20)" "$(sp 100)")"
chk "CUSTOMER-NUM" "$K" "$(get C0113 CUSTOMER-NUM)"
chk "no LGSTSQ message" 0 "$(has C0113 'STUB LGSTSQ')"
# =============================== 01-14 ===============================
begin 01-14 C0114
chk "rc" "00" "$(get C0114 RETURN-CODE)"
chk "CUSTOMER-NUM" "$K" "$(get C0114 CUSTOMER-NUM)"
chk "REQUEST-ID unchanged (blank)" "$(sp 6)" "$(get C0114 REQUEST-ID)"
chk "NUM-POLICIES" "000" "$(get C0114 NUM-POLICIES)"
inserts_ok C0114 "+000000011"
chain_ok C0114 "$K" "$(vrec $K 000)"
chk "no LGSTSQ message" 0 "$(has C0114 'STUB LGSTSQ')"
# =============================== 01-15 ===============================
begin 01-15 "not run, prediction belirsiz"
# =============================== 01-16 ===============================
begin 01-16 D0116
chk "ABEND LGCA reached" 1 "$(has D0116 'STUB EXEC CICS ABEND ABCODE(LGCA)')"
chk "counter not called" 0 "$(has D0116 'STUB CICSCNT')"; chk "no SQL" 0 "$(has D0116 'STUB DB2')"
m=$(msgs D0116); chk "message count" 1 "$(echo "$m" | wc -l)"
chk "msg (71 bytes, B2 +00000)" "$(m_noca LGACDB01 "$(sp 20)+00000")" "$m"; chk "msg length" 71 "${#m}"
# =============================== 01-17 ===============================
begin 01-17 "D0117A-D"
for c in D0117A D0117B; do
  chk "$c rc" "98" "$(get $c RETURN-CODE)"; chk "$c counter not called" 0 "$(has $c 'STUB CICSCNT')"
  chk "$c no SQL" 0 "$(has $c 'STUB DB2')"; chk "$c no message" 0 "$(has $c 'STUB LGSTSQ')"
  chk "$c NUM-POLICIES untouched" "$(sp 3)" "$(get $c NUM-POLICIES)"
done
for c in D0117C D0117D; do chk "$c B1 bytes 7-8" "98" "$(get $c RETURN-CODE)"; chk "$c no SQL" 0 "$(has $c 'STUB DB2')"; done
# =============================== 01-18 ===============================
begin 01-18 D0118
chk "rc" "90" "$(get D0118 RETURN-CODE)"
chk "CUSTOMER-NUM unchanged" "0000000000" "$(get D0118 CUSTOMER-NUM)"
chk "LGACVS01 and LGACDB02 not called" "" "$(links D0118)"
m=$(msgs D0118); a=$(echo "$m" | sed -n 1p); b=$(echo "$m" | sed -n 2p)
chk "message count" 2 "$(echo "$m" | wc -l)"
chk "msg1 (71 bytes)" "$(m1_db01 -00803)" "$a"; chk "msg1 length" 71 "${#a}"
chk "msg2 ('COMMAREA=' + DFHCOMMAREA(1:90))" "COMMAREA=$(ca90 90 0000000000)" "$b"; chk "msg2 length" 99 "${#b}"
# =============================== 01-19 ===============================
begin 01-19 D0119
chk "rc" "00" "$(get D0119 RETURN-CODE)"
chk "CUSTOMER-NUM = K" "$K" "$(get D0119 CUSTOMER-NUM)"
chk "NUM-POLICIES unchanged (777)" "777" "$(get D0119 NUM-POLICIES)"
chk "link order (LGACDB01 called directly)" "LGACVS01 LGACDB02 " "$(links D0119)"
rec=$(sget D0119 'STUB CICSWRF record')
chk "VSAM record carries 777" "$(vrec $K 777)" "$rec"
chk "SECURE customerNumber" "+000000011" "$(nget D0119 'STUB DB2INSS customerNumber')"
# =============================== 01-20 ===============================
begin 01-20 D0120
chk "ABEND LGCA reached" 1 "$(has D0120 'STUB EXEC CICS ABEND ABCODE(LGCA)')"
chk "no SQL" 0 "$(has D0120 'STUB DB2')"
m=$(msgs D0120); chk "message count" 1 "$(echo "$m" | wc -l)"
chk "msg (71 bytes, B2 +00000)" "$(m_noca LGACDB02 "$(sp 20)+00000")" "$m"; chk "msg length" 71 "${#m}"
# =============================== 01-21 ===============================
begin 01-21 D0121
chk "D2-RETURN-CODE" "99" "$(get D0121 D2-RETURN-CODE)"
chk "no SQL" 0 "$(has D0121 'STUB DB2')"; chk "no message" 0 "$(has D0121 'STUB LGSTSQ')"
# =============================== 01-22 ===============================
begin 01-22 "D0122A, D0122B"
for c in D0122A D0122B; do
  chk "$c D2-RETURN-CODE" "98" "$(get $c D2-RETURN-CODE)"
  m=$(msgs $c); a=$(echo "$m" | sed -n 1p); b=$(echo "$m" | sed -n 2p)
  chk "$c message count" 2 "$(echo "$m" | wc -l)"
  chk "$c msg1 (71 bytes)" "$(m1_db02 -00803)" "$a"; chk "$c msg1 length" 71 "${#a}"
  chk "$c msg2" "COMMAREA=02ACUS980000000001${PASS}0000N$(sp 35)" "$b"; chk "$c msg2 length" 99 "${#b}"
done
# =============================== 01-23, 01-24 ===============================
begin 01-23 "not run, prediction belirsiz"
begin 01-24 "not run, prediction belirsiz"
# =============================== 01-25 ===============================
begin 01-25 "D0125A, D0125B"
for c in D0125A D0125B; do
  chk "$c ABEND LGV0 reached" 1 "$(has $c 'STUB EXEC CICS ABEND ABCODE(LGV0)')"
  m=$(msgs $c); a=$(echo "$m" | sed -n 1p); b=$(echo "$m" | sed -n 2p)
  chk "$c message count" 2 "$(echo "$m" | wc -l)"
  chk "$c msg1 (85 bytes)" "$(m1_vs01 $K)" "$a"; chk "$c msg1 length" 85 "${#a}"
done
chk "D0125A msg2 (EIBCALEN=18: 18 bytes + blanks, rc 80)" "COMMAREA=01ACUS80${K}$(sp 72)" "$(msgs D0125A | sed -n 2p)"
chk "D0125B msg2 (first 90 bytes, rc 80)" "COMMAREA=$(ca90 80 $K)" "$(msgs D0125B | sed -n 2p)"

echo
echo "every case ended with exit=0:"
for c in C0101 C0102 C0102B C0103 C0104 C0105 C0106 C0107 C0108 C0109 C0110 C0111A C0111B C0112A C0112B C0113 C0114 D0116 D0117A D0117B D0117C D0117D D0118 D0119 D0120 D0121 D0122A D0122B D0125A D0125B; do
  [ "$(ex $c)" = 1 ] || echo "  case $c did NOT end with exit=0"; done
echo
echo "=== verdict per prediction row (01-customer-add.md) ==="
for r in 01-01 01-02 01-03 01-04 01-05 01-06 01-07 01-08 01-09 01-10 01-11 01-12 01-13 01-14 01-15 01-16 01-17 01-18 01-19 01-20 01-21 01-22 01-23 01-24 01-25; do
  p=${RP[$r]:-0}; f=${RF[$r]:-0}
  if [ "$p" = 0 ] && [ "$f" = 0 ]; then v="belirsiz (puanlanmadi / calistirilmadi)"
  elif [ "$f" = 0 ]; then v="tuttu"; else v="TUTMADI"; fi
  echo "$r  $v  (pass=$p fail=$f)"; done
tp=0; tf=0; for r in "${!RP[@]}"; do tp=$((tp+RP[$r])); tf=$((tf+RF[$r])); done
echo; echo "TOTAL checks: PASS=$tp FAIL=$tf"
