@echo off
setlocal
cd /d "%~dp0"
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
