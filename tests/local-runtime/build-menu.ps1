#Requires -Version 5.1
<#
Builds tests/local-runtime/examples/genapp_menu.cbl against the real,
unmodified base/src programs it drives (Customer Inquire/Add/Update, Motor
Policy Add), producing genapp_menu.exe.

Prerequisites (run once, in order): setup.ps1, setup-sql-bridge.ps1,
setup-cics-stub.ps1. Safe to re-run this script any time after that - it
always rebuilds from base/src fresh.
#>

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
$baseSrc = Join-Path $root "base\src"
$localRuntime = Join-Path $root "tests\local-runtime"
$build = Join-Path $localRuntime "build"
$ucrtBin = "C:\msys64\ucrt64\bin"
$gixppBin = "C:\msys64\ucrt64\opt\gixsql\gixsql-binaries-windows-x64-mingw\bin"

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

if (-not (Test-Path "$ucrtBin\cobc.exe")) { throw "GnuCOBOL not found - run setup.ps1 first." }
if (-not (Test-Path "$gixppBin\gixpp.exe")) { throw "gixpp not found - run setup-sql-bridge.ps1 first." }
if (-not (Test-Path (Join-Path $localRuntime "cics-stub\genapp_vsam_stub.o"))) {
    throw "genapp_vsam_stub.o not found - run setup-cics-stub.ps1 first."
}

New-Item -ItemType Directory -Force -Path $build | Out-Null
New-Item -ItemType Directory -Force -Path "$build\copy" | Out-Null
Copy-Item "$baseSrc\lgcmarea.cpy" "$build\copy\LGCMAREA.cpy" -Force
Copy-Item "$baseSrc\lgpolicy.cpy" "$build\copy\LGPOLICY.cpy" -Force
Copy-Item "$localRuntime\copy\SQLCA.cpy" "$build\copy\SQLCA.cpy" -Force

$env:PATH = "$ucrtBin;$env:PATH"
$env:COB_CONFIG_DIR = "$ucrtBin\..\share\gnucobol\config"

# Programs with EXEC SQL need gixpp first; programs with only EXEC CICS go
# straight to preprocess_cics.py. Order matters - gixpp leaves EXEC CICS
# alone, so it's always SQL pass then CICS pass, never the other way round.
$sqlPrograms = @("lgacdb01", "lgacdb02", "lgicdb01", "lgucdb01", "lgapdb01", "lgipdb01", "lgdpdb01", "lgupdb01")
$cicsOnlyPrograms = @("lgacus01", "lgacvs01", "lgicus01", "lgucus01", "lgucvs01", "lgapol01", "lgapvs01", "lgipol01", "lgdpol01", "lgdpvs01", "lgupol01", "lgupvs01")

Step "Copying base/src programs into the build dir (base/ itself is never touched)..."
foreach ($p in $sqlPrograms + $cicsOnlyPrograms) {
    Copy-Item "$baseSrc\$p.cbl" "$build\$p.cbl" -Force
}

# lgapdb01.cbl declares DB2-M-PREMIUM-int/DB2-M-ACCIDENTS-int lowercase but
# references them as -INT (uppercase) in its EXEC SQL INSERT. Real COBOL
# compilers are case-insensitive about identifiers so this has presumably
# always worked on a real mainframe; gixpp's host-variable lookup is
# case-sensitive and fails to compile without this. Fixed only in this
# disposable build-dir copy - see docs/local-cics-runtime-plan.md.
(Get-Content "$build\lgapdb01.cbl") `
    -replace "DB2-M-PREMIUM-int", "DB2-M-PREMIUM-INT" `
    -replace "DB2-M-ACCIDENTS-int", "DB2-M-ACCIDENTS-INT" |
    Set-Content "$build\lgapdb01.cbl"

# lgipdb01.cbl needs three fixes in this disposable build-dir copy, none of
# which touch base/src (see docs/local-cics-runtime-plan.md, Motor Inquire
# entry):
#  1. gixpp's ESQL parser doesn't support Db2's "Insensitive Scroll" cursor
#     modifiers (used only by the Commercial-search cursors, not by any path
#     Motor/House/Endowment Inquire need) - strip them, gixpp needs plain
#     "DECLARE x CURSOR FOR".
#  2. gixpp's host-variable lookup is case-sensitive; Zip_Cursor's WHERE
#     clause references :CA-B-POSTCODE but the shared LGCMAREA.cpy declares
#     it CA-B-Postcode.
#  3. gixpp mishandles "col INDICATOR :ind" on a SELECT...INTO: it registers
#     the indicator as its own ordinary result slot but never adds a
#     matching column to the actual SQL text sent to the DB, so every result
#     after the first indicator binds to the wrong Postgres column. Fix:
#     rewrite each "col INDICATOR :ind" into a real extra selected column
#     ("col, CASE WHEN col IS NULL THEN -1 ELSE 0 END") plus a plain host
#     var in the INTO list - gixpp then treats it as 18 ordinary columns
#     with nothing left to mishandle. BROKERID/BROKERSREFERENCE/PAYMENT use
#     this exact indicator pattern verbatim in GET-ENDOW/HOUSE/MOTOR-DB2-INFO,
#     so one global replace fixes all three paragraphs at once.
(Get-Content "$build\lgipdb01.cbl") `
    -replace "Insensitive Scroll Cursor For", "Cursor For" `
    -replace ":CA-B-POSTCODE", ":CA-B-Postcode" `
    -replace "^(\s+)BROKERID,$", "`$1BROKERID,`r`n`$1CASE WHEN BROKERID IS NULL THEN -1 ELSE 0 END," `
    -replace "^(\s+)BROKERSREFERENCE,$", "`$1BROKERSREFERENCE,`r`n`$1CASE WHEN BROKERSREFERENCE IS NULL`r`n`$1  THEN -1 ELSE 0 END," `
    -replace "^(\s+)PAYMENT,$", "`$1PAYMENT,`r`n`$1CASE WHEN PAYMENT IS NULL THEN -1 ELSE 0 END," `
    -replace ":DB2-BROKERID-INT INDICATOR :IND-BROKERID,", ":DB2-BROKERID-INT,`r`n                   :IND-BROKERID," `
    -replace ":DB2-BROKERSREF INDICATOR :IND-BROKERSREF,", ":DB2-BROKERSREF,`r`n                   :IND-BROKERSREF," `
    -replace ":DB2-PAYMENT-INT INDICATOR :IND-PAYMENT,", ":DB2-PAYMENT-INT,`r`n                   :IND-PAYMENT," |
    Set-Content "$build\lgipdb01.cbl"

Step "Preprocessing (gixpp for EXEC SQL, then preprocess_cics.py for EXEC CICS)..."
Push-Location $build
try {
    $env:PATH = "$gixppBin;$ucrtBin;$env:PATH"
    foreach ($p in $sqlPrograms) {
        & gixpp.exe -e -S -I copy -i "$p.cbl" -o "$p.sql.cbl"
        if ($LASTEXITCODE -ne 0) { throw "gixpp failed on $p.cbl" }
    }
    $env:PATH = "$ucrtBin;$env:PATH"
    foreach ($p in $sqlPrograms) {
        python3 "$localRuntime\cics-stub\preprocess_cics.py" "$p.sql.cbl" "$p.pp.cbl"
        if ($LASTEXITCODE -ne 0) { throw "preprocess_cics.py failed on $p.sql.cbl" }
    }
    foreach ($p in $cicsOnlyPrograms) {
        python3 "$localRuntime\cics-stub\preprocess_cics.py" "$p.cbl" "$p.pp.cbl"
        if ($LASTEXITCODE -ne 0) { throw "preprocess_cics.py failed on $p.cbl" }
    }

    Step "Compiling all programs to object files..."
    foreach ($p in $sqlPrograms + $cicsOnlyPrograms) {
        & cobc.exe -c -o "$p.o" "$p.pp.cbl" -I copy
        if ($LASTEXITCODE -ne 0) { throw "cobc failed on $p.pp.cbl" }
    }
    & cobc.exe -c -o lgstsq_stub.o "$localRuntime\cics-stub\lgstsq_stub.cbl"
    if ($LASTEXITCODE -ne 0) { throw "cobc failed on lgstsq_stub.cbl" }
    & cobc.exe -c -x -o menu.o "$localRuntime\examples\genapp_menu.cbl" -I copy
    if ($LASTEXITCODE -ne 0) { throw "cobc failed on genapp_menu.cbl" }

    Step "Linking genapp_menu.exe..."
    $objs = @("menu.o") + ($sqlPrograms + $cicsOnlyPrograms | ForEach-Object { "$_.o" }) + @("lgstsq_stub.o")
    & cobc.exe -x -o genapp_menu.exe @objs `
        "$localRuntime\sql-stub\genapp_sqlstub.o" `
        "$localRuntime\cics-stub\genapp_vsam_stub.o" `
        -L "$ucrtBin\..\lib" -lpq -lstdc++
    if ($LASTEXITCODE -ne 0) { throw "link failed" }
} finally {
    Pop-Location
}

$exePath = Join-Path $build "genapp_menu.exe"
Write-Host "`nBuilt: $exePath" -ForegroundColor Green
Write-Host "`nRun it with:" -ForegroundColor Green
Write-Host "  `$env:DATASRC = `"pgsql://127.0.0.1:5432/genapp`""
Write-Host "  `$env:DATASRC_USR = `"postgres`""
Write-Host "  `$env:DATASRC_PWD = `"postgres`""
Write-Host "  `$env:GENAPP_VSAM_DIR = `"$localRuntime\vsam-data`""
Write-Host "  & `"$exePath`""
Write-Host "`n(Postgres must be running - setup-sql-bridge.ps1 starts it, or: & `"$ucrtBin\pg_ctl.exe`" -D `"$env:USERPROFILE\pgdata-genapp`" start)"
