#!/bin/bash
# Builds the LGIPDB01 SQL-only test rig and (with "run") runs every case in its own process.
# Usage, from tests/gnucobol:
#   bash runs/lgipdb01-calistir.sh build          # convert + compile only
#   bash runs/lgipdb01-calistir.sh run > runs/lgipdb01-cikti.txt
# One case per process: WORKING-STORAGE keeps its values between CALLs in the same
# process, while CICS gives every task a fresh copy (see README, step 9).
set -u
export PATH=/c/msys64/ucrt64/bin:$PATH
cd "$(dirname "$0")/.." || exit 1
SRC=../../base/src
OUT=build/sql

# each case is "<REQUEST-ID> <SQLCODE>"
CASES=(
  "01IMOT 0" "01IMOT 100"
  "01IMOT -904" "01IMOT -305" "01IMOT -803" "01IMOT -811" "01IMOT -911"
  "01IMOT -913" "01IMOT -922" "01IMOT -30081" "01IMOT +304"
  "01IEND 0" "01IEND 100" "01IEND -904"
  "01IHOU 0" "01IHOU 100" "01IHOU -904"
  "01ICOM 0" "01ICOM 100" "01ICOM -904"
  "03ICOM 0" "03ICOM 100" "03ICOM -904"
  # added after the first run (same rules, information only): the statements that run had not reached
  "02ICOM 0" "02ICOM 100" "02ICOM -904" "05ICOM 0" "05ICOM 100" "05ICOM -904"
)

build() {
  python convert-lgipdb01.py >&2 || exit 1
  for f in lgipdb01.cbl lgpolicy.cpy lgcmarea.cpy; do
    cmp "$SRC/$f" "work/$f" >&2 || { echo "work/$f differs from base/src" >&2; exit 1; }
  done
  n=$(grep -c '^ *EXEC ' work/lgipdb01-sql.cbl)
  [ "$n" = 0 ] || { echo "work/lgipdb01-sql.cbl: $n EXEC lines left" >&2; exit 1; }
  mkdir -p "$OUT"
  cobc -m -I work -I stubs -o "$OUT/LGIPDB01.dll" work/lgipdb01-sql.cbl || exit 1
  cobc -m -o "$OUT/LGSTSQ.dll" stubs/lgstsq.cbl || exit 1
  cobc -m -I stubs -o "$OUT/DB2STUB.dll" stubs/db2stub.cbl || exit 1
  cobc -x -I work -o "$OUT/drv-lgipdb01.exe" work/drv-lgipdb01.cbl || exit 1
  echo "build OK ($(cobc --version | head -1))" >&2
}

run() {
  export COB_LIBRARY_PATH=$OUT
  echo "# $(cobc --version | head -1); one process per case"
  for c in "${CASES[@]}"; do
    echo "=================== $c"
    # 20 s and 400 lines per case: a case that never ends (see runs/lgipdb01.md, "05ICOM 0") is cut off
    # shellcheck disable=SC2086
    timeout 20 ./$OUT/drv-lgipdb01.exe $c 2>&1 | head -n 400
    echo "[exit=${PIPESTATUS[0]}]"
  done
}

case "${1:-}" in
  build) build ;;
  run) run ;;
  *) echo "usage: $0 build|run" >&2; exit 2 ;;
esac
