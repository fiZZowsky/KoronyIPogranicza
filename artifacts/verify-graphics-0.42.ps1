$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$exe=Join-Path $PWD 'src/Crown.Desktop/bin/Release/net10.0-windows/KoronyIPogranicza.exe'
$results=[Collections.Generic.List[string]]::new()
$modes=@(@('character-motion','--ui-smoke-character-motion',25),@('settings','--ui-smoke-settings',44),@('environment-detail','--ui-smoke-environment-detail',23),@('art','--ui-smoke-art-review',15),@('3d','--ui-smoke-3d',34),@('photographic','--ui-smoke-photographic',21),@('river','--ui-smoke-rivers',19),@('landscape','--ui-smoke-landscape',18),@('environment','--ui-smoke-environment',22),@('models','--ui-smoke-models',36),@('rendering','--ui-smoke-rendering',27),@('era-identity','--ui-smoke-era-identity',44),@('onboarding','--ui-smoke-onboarding',46))
foreach($mode in $modes){
 $folder=Join-Path $PWD ('artifacts/graphics-'+$mode[0]+'-0.42')
 $timer=[Diagnostics.Stopwatch]::StartNew()
 $process=Start-Process -FilePath $exe -ArgumentList @($mode[1],('"'+$folder+'"')) -WindowStyle Hidden -PassThru
 if(-not $process.WaitForExit(180000)){Stop-Process -Id $process.Id -Force;throw "Diagnostic timeout: $($mode[0])"}
 $report=Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
 $count=@($report | Where-Object {$_ -like 'PASS *'}).Count
 if($process.ExitCode -ne 0 -or $count -ne $mode[2]){throw "Diagnostic failed: $($mode[0]); exit $($process.ExitCode); checks $count"}
 $line='{0}: {1} checks; exit 0; {2:F1}s' -f $mode[0],$count,$timer.Elapsed.TotalSeconds
 $results.Add($line);$results | Set-Content artifacts/graphics-results-0.42.txt -Encoding utf8;Write-Output $line
}
