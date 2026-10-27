@echo off
setlocal
cd /d "%~dp0"
if exist "wydanie-0.26\KoronyIPogranicza.exe" (
    start "" "wydanie-0.26\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.25\KoronyIPogranicza.exe" (
    start "" "wydanie-0.25\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.24\KoronyIPogranicza.exe" (
    start "" "wydanie-0.24\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.23\KoronyIPogranicza.exe" (
    start "" "wydanie-0.23\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.22\KoronyIPogranicza.exe" (
    start "" "wydanie-0.22\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.21\KoronyIPogranicza.exe" (
    start "" "wydanie-0.21\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.20\KoronyIPogranicza.exe" (
    start "" "wydanie-0.20\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.19\KoronyIPogranicza.exe" (
    start "" "wydanie-0.19\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.18\KoronyIPogranicza.exe" (
    start "" "wydanie-0.18\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.17\KoronyIPogranicza.exe" (
    start "" "wydanie-0.17\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.16\KoronyIPogranicza.exe" (
    start "" "wydanie-0.16\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.15\KoronyIPogranicza.exe" (
    start "" "wydanie-0.15\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.14\KoronyIPogranicza.exe" (
    start "" "wydanie-0.14\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.13\KoronyIPogranicza.exe" (
    start "" "wydanie-0.13\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.12\KoronyIPogranicza.exe" (
    start "" "wydanie-0.12\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.11\KoronyIPogranicza.exe" (
    start "" "wydanie-0.11\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.10\KoronyIPogranicza.exe" (
    start "" "wydanie-0.10\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.9\KoronyIPogranicza.exe" (
    start "" "wydanie-0.9\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.8\KoronyIPogranicza.exe" (
    start "" "wydanie-0.8\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.7\KoronyIPogranicza.exe" (
    start "" "wydanie-0.7\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.6\KoronyIPogranicza.exe" (
    start "" "wydanie-0.6\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.5\KoronyIPogranicza.exe" (
    start "" "wydanie-0.5\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.4\KoronyIPogranicza.exe" (
    start "" "wydanie-0.4\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.3\KoronyIPogranicza.exe" (
    start "" "wydanie-0.3\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie-0.2\KoronyIPogranicza.exe" (
    start "" "wydanie-0.2\KoronyIPogranicza.exe"
    exit /b 0
)
if exist "wydanie\KoronyIPogranicza.exe" (
    start "" "wydanie\KoronyIPogranicza.exe"
    exit /b 0
)
dotnet run --project "src\Crown.Desktop" -c Release -p:MSBuildEnableWorkloadResolver=false -p:TargetPlatformDisplayName=Windows
if errorlevel 1 pause
