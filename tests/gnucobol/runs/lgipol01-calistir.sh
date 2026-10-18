#!/bin/bash
# Builds the LGIPOL01 test rig and (with "run") runs every case in its own process.
# Usage, from tests/gnucobol:
#   bash runs/lgipol01-calistir.sh build          # convert + compile only
#   bash runs/lgipol01-calistir.sh run > runs/lgipol01-cikti.txt
# One case per process: WORKING-STORAGE keeps its values between CALLs in the same
# process, while CICS gives every task a fresh copy (see README, step 9).
set -u
export PATH=/c/msys64/ucrt64/bin:$PATH
cd "$(dirname "$0")/.." || exit 1
SRC=../../base/src

CASES="C0501 C0502 C0503 C0504 C0505 C0506 C0507 C0508 C0509 D0510
C0511A C0511B C0511C D0512A D0512B D0513 D0514 D0515
C0516 C0517 C0519 C0520A C0520B"

build() {
  python convert-lgipol01.py >&2 || exit 1
  for f in lgipol01 lgipdb01 lgpolicy.cpy lgcmarea.cpy; do
    case "$f" in
      *.cpy) orig="$f" ;;
      *) orig="$f.cbl" ;;
    esac
    cmp "$SRC/$orig" "work/$orig" >&2 || { echo "work/$orig differs from base/src" >&2; exit 1; }
  done
  for f in lgipol01 lgipdb01; do
    n=$(grep -c '^ *EXEC ' "work/$f-gc.cbl")
    [ "$n" = 0 ] || { echo "work/$f-gc.cbl: $n EXEC lines left" >&2; exit 1; }
  done
  mkdir -p build
  for f in lgipol01 lgipdb01; do
    U=$(echo "$f" | tr a-z A-Z)
    cobc -m -I work -I stubs -o "build/$U.dll" "work/$f-gc.cbl" || exit 1
  done
  cobc -m -o build/LGSTSQ.dll stubs/lgstsq.cbl || exit 1
  for s in db2motor db2unsup; do
    U=$(echo "$s" | tr a-z A-Z)
    cobc -m -I stubs -o "build/$U.dll" "stubs/$s.cbl" || exit 1
  done
  cobc -x -I work -o build/drv-lgipol01.exe work/drv-lgipol01.cbl || exit 1
  echo "build OK ($(cobc --version | head -1))" >&2
}

run() {
  export COB_LIBRARY_PATH=build
  echo "# $(cobc --version | head -1); one process per case"
  for c in $CASES; do
    echo "=================== $c"
    ./build/drv-lgipol01.exe "$c"
    echo "[exit=$?]"
  done
}

case "${1:-}" in
  build) build ;;
  run) run ;;
  *) echo "usage: $0 build|run" >&2; exit 2 ;;
esac
