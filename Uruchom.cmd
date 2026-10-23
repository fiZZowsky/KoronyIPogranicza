@echo off
setlocal
cd /d "%~dp0"
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
