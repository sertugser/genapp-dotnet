#!/bin/bash
# Builds the LGACUS01 test rig and (with "run") runs every case in its own process.
# Usage, from tests/gnucobol:
#   bash runs/lgacus01-calistir.sh build          # convert + compile only
#   bash runs/lgacus01-calistir.sh run > runs/lgacus01-cikti.txt
# One case per process: WORKING-STORAGE keeps its values between CALLs in the same
# process, while CICS gives every task a fresh copy (see README, step 9).
set -u
export PATH=/c/msys64/ucrt64/bin:$PATH
cd "$(dirname "$0")/.." || exit 1
SRC=../../base/src

CASES="C0101 C0102 C0102B C0103 C0104 C0105 C0106 C0107 C0108 C0109 C0110
C0111A C0111B C0112A C0112B C0113 C0114
D0116 D0117A D0117B D0117C D0117D D0118 D0119
D0120 D0121 D0122A D0122B D0125A D0125B"

build() {
  python convert-lgacus01.py >&2 || exit 1
  for f in lgacus01 lgacdb01 lgacdb02 lgacvs01; do
    cmp "$SRC/$f.cbl" "work/$f.cbl" >&2 || { echo "work/$f.cbl differs from base/src" >&2; exit 1; }
    n=$(grep -c '^ *EXEC ' "work/$f-gc.cbl")
    [ "$n" = 0 ] || { echo "work/$f-gc.cbl: $n EXEC lines left" >&2; exit 1; }
  done
  mkdir -p build
  for f in lgacus01 lgacdb01 lgacdb02 lgacvs01; do
    U=$(echo "$f" | tr a-z A-Z)
    cobc -m -I work -I stubs -o "build/$U.dll" "work/$f-gc.cbl" || exit 1
  done
  cobc -m -o build/LGSTSQ.dll stubs/lgstsq.cbl || exit 1
  for s in cicscnt cicswrf db2insc db2insd db2ident db2inss; do
    U=$(echo "$s" | tr a-z A-Z)
    cobc -m -I stubs -o "build/$U.dll" "stubs/$s.cbl" || exit 1
  done
  cobc -x -I work -o build/drv-lgacus01.exe work/drv-lgacus01.cbl || exit 1
  echo "build OK ($(cobc --version | head -1))" >&2
}

run() {
  export COB_LIBRARY_PATH=build
  echo "# $(cobc --version | head -1); one process per case"
  for c in $CASES; do
    echo "=================== $c"
    ./build/drv-lgacus01.exe "$c"
    echo "[exit=$?]"
  done
}

case "${1:-}" in
  build) build ;;
  run) run ;;
  *) echo "usage: $0 build|run" >&2; exit 2 ;;
esac
