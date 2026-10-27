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
$baseTimer = [Diagnostics.Stopwatch]::StartNew()
while (-not $gameProcess.WaitForExit(1000)) {
    if ($baseTimer.Elapsed.TotalSeconds -le 180) { continue }
    Stop-Process -Id $gameProcess.Id -Force
    throw 'Test interfejsu 3D przekroczył limit 180 sekund.'
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

$powerDirectory = Join-Path $reportDirectory 'power-ui'
$powerProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-power', ('"' + $powerDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $powerProcess.WaitForExit(60000)) {
    Stop-Process -Id $powerProcess.Id -Force
    throw 'Test energetyki przekroczył limit 60 sekund.'
}
if ($powerProcess.ExitCode -ne 0) { throw 'Test energetyki WPF nie przeszedł. Sprawdź artifacts/power-ui/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $powerDirectory 'ui-smoke.txt')

$energyCaseDirectory = Join-Path $reportDirectory 'energy-case-ui'
$energyCaseProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-energy-cases', ('"' + $energyCaseDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $energyCaseProcess.WaitForExit(60000)) {
    Stop-Process -Id $energyCaseProcess.Id -Force
    throw 'Test kryzysów energetycznych przekroczył limit 60 sekund.'
}
if ($energyCaseProcess.ExitCode -ne 0) { throw 'Test kryzysów WPF nie przeszedł. Sprawdź artifacts/energy-case-ui/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $energyCaseDirectory 'ui-smoke.txt')
$threeDimensionalDirectory = Join-Path $reportDirectory 'three-dimensional-ui'
$threeDimensionalProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-3d', ('"' + $threeDimensionalDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $threeDimensionalProcess.WaitForExit(60000)) {
    Stop-Process -Id $threeDimensionalProcess.Id -Force
    throw 'Test sceny 3D przekroczył limit 60 sekund.'
}
if ($threeDimensionalProcess.ExitCode -ne 0) { throw 'Test sceny 3D nie przeszedł. Sprawdź artifacts/three-dimensional-ui/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $threeDimensionalDirectory 'ui-smoke.txt')
$photographicDirectory = Join-Path $reportDirectory 'photographic-ui-0.27'
$photographicProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-photographic', ('"' + $photographicDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $photographicProcess.WaitForExit(60000)) {
    Stop-Process -Id $photographicProcess.Id -Force
    throw 'Test oprawy GPU przekroczył limit 60 sekund.'
}
if ($photographicProcess.ExitCode -ne 0) { throw 'Test oprawy GPU nie przeszedł. Sprawdź artifacts/photographic-ui-0.27/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $photographicDirectory 'ui-smoke.txt')

$landscapeDirectory = Join-Path $reportDirectory 'landscape-ui-0.27'
$landscapeProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-landscape', ('"' + $landscapeDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $landscapeProcess.WaitForExit(60000)) {
    Stop-Process -Id $landscapeProcess.Id -Force
    throw 'Test krajobrazu przekroczył limit 60 sekund.'
}
if ($landscapeProcess.ExitCode -ne 0) { throw 'Test krajobrazu nie przeszedł. Sprawdź artifacts/landscape-ui-0.27/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $landscapeDirectory 'ui-smoke.txt')
$riverDirectory = Join-Path $reportDirectory 'river-ui-0.27'
$riverProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-rivers', ('"' + $riverDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $riverProcess.WaitForExit(60000)) {
    Stop-Process -Id $riverProcess.Id -Force
    throw 'Test rzek i mostów przekroczył limit 60 sekund.'
}
if ($riverProcess.ExitCode -ne 0) { throw 'Test rzek i mostów nie przeszedł. Sprawdź artifacts/river-ui-0.27/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $riverDirectory 'ui-smoke.txt')
$environmentDirectory = Join-Path $reportDirectory 'environment-ui-0.27'
$environmentProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-environment', ('"' + $environmentDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $environmentProcess.WaitForExit(60000)) {
    Stop-Process -Id $environmentProcess.Id -Force
    throw 'Test pór roku i pogody przekroczył limit 60 sekund.'
}
if ($environmentProcess.ExitCode -ne 0) { throw 'Test pór roku i pogody nie przeszedł. Sprawdź artifacts/environment-ui-0.27/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $environmentDirectory 'ui-smoke.txt')

$modelsDirectory = Join-Path $reportDirectory 'models-ui-0.27'
$modelsProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-models', ('"' + $modelsDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $modelsProcess.WaitForExit(60000)) {
    Stop-Process -Id $modelsProcess.Id -Force
    throw 'Test modeli i oznaczeń zasobów przekroczył limit 60 sekund.'
}
if ($modelsProcess.ExitCode -ne 0) { throw 'Test modeli i oznaczeń zasobów nie przeszedł. Sprawdź artifacts/models-ui-0.27/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $modelsDirectory 'ui-smoke.txt')

$renderingDirectory = Join-Path $reportDirectory 'rendering-ui-0.27'
$renderingProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-rendering', ('"' + $renderingDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $renderingProcess.WaitForExit(60000)) {
    Stop-Process -Id $renderingProcess.Id -Force
    throw 'Test widoczności i szczegółów renderowania przekroczył limit 60 sekund.'
}
if ($renderingProcess.ExitCode -ne 0) { throw 'Test renderowania nie przeszedł. Sprawdź artifacts/rendering-ui-0.27/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $renderingDirectory 'ui-smoke.txt')

$settingsDirectory = Join-Path $reportDirectory 'settings-ui-0.27'
$settingsProcess = Start-Process -FilePath $gameExecutable -ArgumentList @('--ui-smoke-settings', ('"' + $settingsDirectory + '"')) -WindowStyle Hidden -PassThru
if (-not $settingsProcess.WaitForExit(90000)) {
    Stop-Process -Id $settingsProcess.Id -Force
    throw 'Test ustawień i menu pauzy przekroczył limit 90 sekund.'
}
if ($settingsProcess.ExitCode -ne 0) { throw 'Test ustawień nie przeszedł. Sprawdź artifacts/settings-ui-0.27/ui-smoke.txt.' }
Get-Content -LiteralPath (Join-Path $settingsDirectory 'ui-smoke.txt')
