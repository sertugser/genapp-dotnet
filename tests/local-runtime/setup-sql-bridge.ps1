#Requires -Version 5.1
<#
Sets up the SQL bridge used to run base/src's EXEC SQL statements against a
local PostgreSQL instance: downloads gixpp (GixSQL's ESQL *preprocessor*
only - its runtime library is NOT used, see sql-stub/genapp_sqlstub.cpp for
why) and builds our own libpq-backed replacement runtime.

Prerequisite: run setup.ps1 first (installs GnuCOBOL via MSYS2, which this
script also relies on for g++/libpq).

Safe to re-run: each step checks whether it's already done before acting.
#>

$ErrorActionPreference = "Stop"

$ucrtBin = "C:\msys64\ucrt64\bin"
$gixDir = "C:\msys64\ucrt64\opt\gixsql"
$gixppExe = "$gixDir\gixsql-binaries-windows-x64-mingw\bin\gixpp.exe"
$gixVersion = "1.0.20b"
$gixUrl = "https://github.com/mridoni/gixsql/releases/download/v$gixVersion/gixsql-binaries-windows-x64-mingw-$gixVersion-1.7z"

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

if (-not (Test-Path "C:\msys64\usr\bin\bash.exe")) {
    throw "MSYS2 not found - run setup.ps1 first."
}

# --- 1. PostgreSQL (MSYS2 package, not the EDB installer - that needs admin
#        rights we don't assume here; see docs/local-cics-runtime-plan.md) ---
if (Test-Path "$ucrtBin\pg_ctl.exe") {
    Step "PostgreSQL already installed, skipping."
} else {
    Step "Installing PostgreSQL via pacman..."
    & cmd /c "`"C:\msys64\usr\bin\bash.exe`" -lc `"pacman -S --noconfirm mingw-w64-ucrt-x86_64-postgresql`""
}

$pgData = "$env:USERPROFILE\pgdata-genapp"
if (Test-Path "$pgData\PG_VERSION") {
    Step "Postgres data dir already initialized at $pgData, skipping initdb."
} else {
    Step "Initializing Postgres data dir at $pgData..."
    $env:PATH = "$ucrtBin;$env:PATH"
    & "$ucrtBin\initdb.exe" -D $pgData -U postgres -E UTF8 --locale=C
}

Step "Starting Postgres (if not already running) and ensuring the genapp database exists..."
$env:PATH = "$ucrtBin;$env:PATH"
# pg_ctl/createdb print a benign notice (not a real failure) if already
# running / already exists; don't redirect their stderr (PowerShell 5.1
# wraps redirected native stderr as a terminating NativeCommandError) and
# don't let $ErrorActionPreference=Stop treat that notice as fatal.
$prevEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"
& "$ucrtBin\pg_ctl.exe" -D $pgData -l "$pgData\server.log" start
Start-Sleep -Seconds 1
& "$ucrtBin\createdb.exe" -U postgres -h 127.0.0.1 -p 5432 genapp
$ErrorActionPreference = $prevEAP
Write-Host "  (local dev only - superuser postgres/postgres, port 5432)"

# --- 2. gixpp (preprocessor only - its runtime .a/.dll are NOT used) --------
if (Test-Path $gixppExe) {
    Step "gixpp already installed, skipping."
} else {
    Step "Downloading gixpp $gixVersion..."
    New-Item -ItemType Directory -Force -Path $gixDir | Out-Null
    $archive = "$gixDir\gixsql.7z"
    Invoke-WebRequest -Uri $gixUrl -OutFile $archive
    & "C:\msys64\usr\lib\p7zip\7z.exe" x $archive "-o$gixDir" -y | Out-Null
    if (-not (Test-Path $gixppExe)) { throw "gixpp.exe not found after extraction - check $gixDir" }
}

# --- 3. Our libpq-backed GIXSQL-runtime replacement -------------------------
$stubDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$stubDir = Join-Path $stubDir "sql-stub"
$stubObj = "$stubDir\genapp_sqlstub.o"
Step "Compiling genapp_sqlstub.cpp..."
$env:PATH = "$ucrtBin;$env:PATH"
& "$ucrtBin\g++.exe" -c -std=c++17 -O2 -I "$ucrtBin\..\include" -o $stubObj "$stubDir\genapp_sqlstub.cpp"
if ($LASTEXITCODE -ne 0) { throw "genapp_sqlstub.cpp failed to compile" }

Write-Host "`nDone. To preprocess+compile a COBOL program against this bridge:" -ForegroundColor Green
Write-Host '  $env:PATH = "C:\msys64\ucrt64\bin;$env:PATH"'
Write-Host '  $env:COB_CONFIG_DIR = "C:\msys64\ucrt64\share\gnucobol\config"'
Write-Host "  gixpp.exe -e -S -I copy -i PROGRAM.cbl -o PROGRAM.cbsql   (gixpp needs its own bin dir on PATH too)"
Write-Host "  cobc.exe -x -o PROGRAM.exe PROGRAM.cbsql $stubObj -I copy -L C:\msys64\ucrt64\lib -lpq -lstdc++"
Write-Host "`nRuntime env vars the program reads: DATASRC=pgsql://127.0.0.1:5432/genapp, DATASRC_USR=postgres, DATASRC_PWD=postgres"
