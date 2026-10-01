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

echo.
echo Done.
pause
exit /b 0

:error
echo.
echo A step above failed - scroll up to see which one and why.
pause
exit /b 1
