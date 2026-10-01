#Requires -Version 5.1
<#
Installs GnuCOBOL on Windows via MSYS2, for running base/src/*.cbl locally without a
mainframe. See ../../docs/local-cics-runtime-plan.md for the full plan this supports.

Safe to re-run: each step checks whether it's already done before acting.
#>

$ErrorActionPreference = "Stop"

$msys2Root = "C:\msys64"
$ucrtBin = "$msys2Root\ucrt64\bin"
$cobcExe = "$ucrtBin\cobc.exe"
$cobConfigDir = "$msys2Root\ucrt64\share\gnucobol\config"
$msysBash = "$msys2Root\usr\bin\bash.exe"

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

# --- 1. MSYS2 itself ---------------------------------------------------
if (Test-Path $msysBash) {
    Step "MSYS2 already installed at $msys2Root, skipping."
} else {
    Step "Installing MSYS2 via winget..."
    winget install --id MSYS2.MSYS2 -e --accept-source-agreements --accept-package-agreements
}

# --- 2. Core MSYS2 update ------------------------------------------------
# First pass updates bash/msys2-runtime itself and may ask to close the shell -
# that's expected MSYS2 behaviour, not a failure. Running it twice (piping "y"
# to auto-confirm) reliably finishes the update.
Step "Updating MSYS2 core packages (runs twice, this is normal)..."
& cmd /c "echo y| `"$msysBash`" -lc `"pacman -Syu --noconfirm`"" | Out-Null
& cmd /c "echo y| `"$msysBash`" -lc `"pacman -Syu --noconfirm`"" | Out-Null

# --- 3. GnuCOBOL ----------------------------------------------------------
if (Test-Path $cobcExe) {
    Step "GnuCOBOL already installed at $cobcExe, skipping."
} else {
    Step "Installing GnuCOBOL (mingw-w64-ucrt-x86_64-gnucobol)..."
    # Note: the classic "mingw-w64-x86_64-gnucobol" (plain mingw64 repo) package no
    # longer exists in current MSYS2 - it now lives under the ucrt64 repo.
    & cmd /c "echo y| `"$msysBash`" -lc `"pacman -S --noconfirm mingw-w64-ucrt-x86_64-gnucobol`"" | Out-Null
}

# --- 4. Persist PATH + COB_CONFIG_DIR (User scope, no admin needed) ------
Step "Persisting PATH and COB_CONFIG_DIR..."
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$ucrtBin*") {
    $newPath = if ([string]::IsNullOrEmpty($userPath)) { $ucrtBin } else { "$userPath;$ucrtBin" }
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host "  Added $ucrtBin to user PATH (restart your terminal to pick it up)."
} else {
    Write-Host "  $ucrtBin already in user PATH."
}
[Environment]::SetEnvironmentVariable("COB_CONFIG_DIR", $cobConfigDir, "User")

# --- 5. Smoke test --------------------------------------------------------
# cobc.exe run outside an MSYS2 shell can't find its own config/gcc unless PATH
# and COB_CONFIG_DIR are set explicitly - hence step 4. We set them for this
# process only, since a freshly-opened terminal won't have picked up the
# persisted User values yet.
Step "Verifying with a smoke-test compile..."
$env:PATH = "$ucrtBin;$env:PATH"
$env:COB_CONFIG_DIR = $cobConfigDir
$tmp = New-Item -ItemType Directory -Force -Path "$env:TEMP\gnucobol-smoketest"
Set-Content -Path "$tmp\hello.cbl" -Encoding ascii -Value @'
       IDENTIFICATION DIVISION.
       PROGRAM-ID. HELLO.
       PROCEDURE DIVISION.
           DISPLAY "GnuCOBOL OK".
           STOP RUN.
'@
Push-Location $tmp
try {
    & $cobcExe -x -free -o hello.exe hello.cbl
    $output = & .\hello.exe
    if ($output -eq "GnuCOBOL OK") {
        Write-Host "`nSuccess: GnuCOBOL is installed and working." -ForegroundColor Green
    } else {
        throw "Unexpected smoke-test output: $output"
    }
} finally {
    Pop-Location
    Remove-Item -Recurse -Force $tmp
}

Write-Host "`nIf this is a new terminal session's first run, open a new terminal so the" -ForegroundColor Yellow
Write-Host "persisted PATH/COB_CONFIG_DIR take effect without manual exports." -ForegroundColor Yellow
