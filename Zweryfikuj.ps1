$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$buildOptions = @('-m:1', '-c', 'Release', '-p:MSBuildEnableWorkloadResolver=false', '-p:TargetPlatformDisplayName=Windows', '-p:NuGetAudit=false')
dotnet build KoronyIPogranicza.slnx @buildOptions --nologo
if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się.' }
$reportDirectory = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
dotnet run --project tests/Crown.Specs -c Release --no-build | Tee-Object -FilePath (Join-Path $reportDirectory 'core-specs.txt')
if ($LASTEXITCODE -ne 0) { throw 'Specyfikacje silnika nie przeszły.' }
$gameExecutable = Join-Path $PSScriptRoot 'src/Crown.Desktop/bin/Release/net10.0-windows/KoronyIPogranicza.exe'
$smokeDirectory = Join-Path $reportDirectory 'ui'
$gameProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke', ('"' + $smokeDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $gameProcess.WaitForExit(60000)) {
    Stop-Process -Id $gameProcess.Id -Force
    throw 'Test interfejsu przekroczył limit 60 sekund.'
}
if ($gameProcess.ExitCode -ne 0) { throw 'Test integracji WPF nie przeszedł. Sprawdź artifacts/ui/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $smokeDirectory 'ui-smoke.txt')
