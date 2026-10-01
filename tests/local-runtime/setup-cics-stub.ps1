#Requires -Version 5.1
<#
Sets up the CICS verb stub (VSAM-equivalent file I/O + LINK/RETURN/ABEND/
ASKTIME/FORMATTIME/GET-COUNTER translation) used to run base/src's business
and data-access programs without a real CICS region: compiles
genapp_vsam_stub.cpp and seeds a working VSAM data directory from
base/data/*.txt (read-only source - base/ itself is never touched or
written to; the working copy lives in $GENAPP_VSAM_DIR).

Prerequisite: run setup.ps1 first (GnuCOBOL via MSYS2) and have Python 3 on
PATH (used for preprocess_cics.py and the seed conversion below).

Safe to re-run: compiling is idempotent; re-seeding only happens if the
working files don't already exist (re-run with -ReseedVsamData to force
starting over from base/data's sample rows).
#>
param(
    [switch]$ReseedVsamData
)

$ErrorActionPreference = "Stop"
$ucrtBin = "C:\msys64\ucrt64\bin"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$stubDir = Join-Path $scriptDir "cics-stub"
$vsamDir = Join-Path $scriptDir "vsam-data"
$baseData = Join-Path (Split-Path -Parent (Split-Path -Parent $scriptDir)) "base\data"

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

if (-not (Test-Path "$ucrtBin\g++.exe")) {
    throw "GnuCOBOL/MSYS2 toolchain not found - run setup.ps1 first."
}

Step "Compiling genapp_vsam_stub.cpp..."
$env:PATH = "$ucrtBin;$env:PATH"
& "$ucrtBin\g++.exe" -c -std=c++17 -O2 -o "$stubDir\genapp_vsam_stub.o" "$stubDir\genapp_vsam_stub.cpp"
if ($LASTEXITCODE -ne 0) { throw "genapp_vsam_stub.cpp failed to compile" }

New-Item -ItemType Directory -Force -Path $vsamDir | Out-Null
$custDat = Join-Path $vsamDir "KSDSCUST.dat"
$polyDat = Join-Path $vsamDir "KSDSPOLY.dat"

if ($ReseedVsamData -or -not (Test-Path $custDat) -or -not (Test-Path $polyDat)) {
    Step "Seeding working VSAM files from base/data (base/ itself is untouched)..."
    # base/data/*.txt are CRLF-separated 225/64-byte lines (see base/data/README.md
    # for the RECFM=FB/LRECL); the stub expects pure concatenated fixed-length
    # records with no separators, so this strips the line endings rather than
    # just copying the files - a straight copy silently misaligns every record
    # after the first (confirmed the hard way before this script existed).
    python3 -c @"
def convert(src, dst, reclen):
    with open(src, 'rb') as f:
        data = f.read()
    lines = [l for l in data.replace(b'\r\n', b'\n').split(b'\n') if l]
    for l in lines:
        assert len(l) == reclen, f'{src}: line length {len(l)} != {reclen}'
    with open(dst, 'wb') as f:
        for l in lines:
            f.write(l)
    print(dst, len(lines), 'records')

convert(r'$baseData\ksdscust.txt', r'$custDat', 225)
convert(r'$baseData\ksdspoly.txt', r'$polyDat', 64)
"@
} else {
    Step "Working VSAM files already seeded at $vsamDir, skipping (-ReseedVsamData to redo)."
}

Write-Host "`nDone. To preprocess+compile a GenApp program against this stub:" -ForegroundColor Green
Write-Host "  python3 $stubDir\preprocess_cics.py PROGRAM.cbl PROGRAM.pp.cbl"
Write-Host '  $env:PATH = "C:\msys64\ucrt64\bin;$env:PATH"'
Write-Host '  $env:COB_CONFIG_DIR = "C:\msys64\ucrt64\share\gnucobol\config"'
Write-Host "  cobc.exe -c -o PROGRAM.o PROGRAM.pp.cbl -I copy   (module; add -x too if it's the entry point)"
Write-Host "  cobc.exe -x -o PROGRAM.exe PROGRAM.o ...other .o's... $stubDir\genapp_vsam_stub.o -lstdc++"
Write-Host "`nRuntime env var the program needs: GENAPP_VSAM_DIR=$vsamDir"
