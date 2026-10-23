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
$spySmokeDirectory = Join-Path $reportDirectory 'espionage-ui'
$spyProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-espionage', ('"' + $spySmokeDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $spyProcess.WaitForExit(60000)) {
    Stop-Process -Id $spyProcess.Id -Force
    throw 'Test szpiegostwa przekroczył limit 60 sekund.'
}
if ($spyProcess.ExitCode -ne 0) { throw 'Test szpiegostwa WPF nie przeszedł. Sprawdź artifacts/espionage-ui/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $spySmokeDirectory 'ui-smoke.txt')

$custodySmokeDirectory = Join-Path $reportDirectory 'custody-ui'
$custodyProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-custody', ('"' + $custodySmokeDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $custodyProcess.WaitForExit(60000)) {
    Stop-Process -Id $custodyProcess.Id -Force
    throw 'Test jeńców wywiadu przekroczył limit 60 sekund.'
}
if ($custodyProcess.ExitCode -ne 0) { throw 'Test jeńców WPF nie przeszedł. Sprawdź artifacts/custody-ui/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $custodySmokeDirectory 'ui-smoke.txt')
$epochDirectory = Join-Path $reportDirectory 'epoch-ui'
$epochProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-epochs', ('"' + $epochDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $epochProcess.WaitForExit(60000)) {
    Stop-Process -Id $epochProcess.Id -Force
    throw 'Test epok przekroczył limit 60 sekund.'
}
if ($epochProcess.ExitCode -ne 0) { throw 'Test epok WPF nie przeszedł. Sprawdź artifacts/epoch-ui/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $epochDirectory 'ui-smoke.txt')