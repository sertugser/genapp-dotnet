@echo off
REM One-click entry point: runs all setup steps (safe to re-run - each one
REM skips what's already done), builds genapp_menu.exe fresh from the real
REM base/src programs, then launches it interactively.
REM
REM Double-click this file, or run it from any directory - %~dp0 below
REM resolves paths relative to wherever this .bat file itself lives, not
REM the caller's current directory.

setlocal
set HERE=%~dp0
set PS=powershell -NoProfile -ExecutionPolicy Bypass -File
REM setup.ps1 persists this to the User registry for *future* terminals;
REM a cmd.exe window opened before that persisted (e.g. one left open from
REM before setup.ps1 ever ran) won't have picked it up yet. genapp_menu.exe
REM needs it on PATH to even start (libpq.dll and friends) - set it
REM explicitly here too rather than trusting the inherited environment.
set PATH=C:\msys64\ucrt64\bin;%PATH%

echo ==== Step 0: GnuCOBOL (MSYS2) ====
%PS% "%HERE%setup.ps1"
if errorlevel 1 goto :error

echo.
echo ==== Step 1: PostgreSQL + gixpp + SQL bridge ====
%PS% "%HERE%setup-sql-bridge.ps1"
if errorlevel 1 goto :error

echo.
echo ==== Step 2/3: VSAM/CICS stub ====
%PS% "%HERE%setup-cics-stub.ps1"
if errorlevel 1 goto :error

echo.
echo ==== Building genapp_menu.exe ====
%PS% "%HERE%build-menu.ps1"
if errorlevel 1 goto :error

echo.
echo ==== Starting GenApp ====
set DATASRC=pgsql://127.0.0.1:5432/genapp
set DATASRC_USR=postgres
set DATASRC_PWD=postgres
set GENAPP_VSAM_DIR=%HERE%vsam-data
"%HERE%build\genapp_menu.exe"
if errorlevel 1 (
    echo.
    echo genapp_menu.exe exited with an error ^(code %errorlevel%^) instead of
    echo a clean exit via menu option 0 - if no menu ever appeared above,
    echo it likely failed to start ^(missing DLL, or Postgres isn't running^).
    goto :error
)

echo.
echo Done.
pause
exit /b 0

:error
echo.
echo A step above failed - scroll up to see which one and why.
pause
exit /b 1
