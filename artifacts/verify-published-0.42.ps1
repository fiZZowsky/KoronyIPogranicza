$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$executable=Join-Path $PWD 'wydanie-0.42/KoronyIPogranicza.exe'
$results=[Collections.Generic.List[string]]::new()
function Diagnostic($mode,$suffix,$expected,$fallback,$forced){
    $folder=Join-Path $PWD ('artifacts/published-'+$suffix+'-0.42')
    $arguments=@($mode)
    if($fallback){$arguments+='--no-gpu'}
    if($forced){$arguments+='--smoke-force-failure'}
    if($suffix -eq 'user-scene-gpu'){$arguments+=@('--review-save',('"'+(Join-Path $PWD 'wydanie-0.40/saves/autosave.crown')+'"'))}
    $arguments+=('"'+$folder+'"')
    $timer=[Diagnostics.Stopwatch]::StartNew()
    $process=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if(-not $process.WaitForExit(240000)){Stop-Process -Id $process.Id -Force;throw "Diagnostic timeout: $suffix"}
    $report=Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
    $count=@($report | Where-Object {$_ -like 'PASS *'}).Count
    if($forced){if($process.ExitCode -ne 3 -or ($report -join "`n") -notmatch 'Intentional'){throw "Missing intentional failure: $suffix"}}
    elseif($process.ExitCode -ne 0 -or $count -ne $expected){throw "Diagnostic failed: $suffix; exit $($process.ExitCode); checks $count"}
    $line='{0}: {1} checks; exit {2}; {3:F1}s' -f $suffix,$count,$process.ExitCode,$timer.Elapsed.TotalSeconds
    $results.Add($line);$results | Set-Content artifacts/published-verification-0.42.txt -Encoding utf8;Write-Output $line
}
Diagnostic '--ui-smoke-character-motion' 'motion-gpu' 25 $false $false
Diagnostic '--ui-smoke-character-motion' 'motion-wpf' 15 $true $false
Diagnostic '--ui-smoke-settings' 'settings-gpu' 44 $false $false
Diagnostic '--ui-smoke-models' 'models-gpu' 36 $false $false
Diagnostic '--ui-smoke-rendering' 'rendering-gpu' 27 $false $false
Diagnostic '--ui-smoke-character-motion' 'motion-forced' 0 $false $true
