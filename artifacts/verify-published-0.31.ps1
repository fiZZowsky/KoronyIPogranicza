$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$executable=Join-Path $PWD 'wydanie-0.31/KoronyIPogranicza.exe'
$modes=@(
    @('base','--ui-smoke',170),@('espionage','--ui-smoke-espionage',22),@('custody','--ui-smoke-custody',18),@('epoch','--ui-smoke-epochs',38),
    @('power','--ui-smoke-power',23),@('energy-case','--ui-smoke-energy-cases',22),@('three-dimensional','--ui-smoke-3d',34),@('photographic','--ui-smoke-photographic',21),
    @('landscape','--ui-smoke-landscape',18),@('river','--ui-smoke-rivers',19),@('environment','--ui-smoke-environment',22),@('models','--ui-smoke-models',27),
    @('rendering','--ui-smoke-rendering',27),@('settings','--ui-smoke-settings',44),@('district','--ui-smoke-districts',32),@('military-ai','--ui-smoke-military-ai',16),@('naval','--ui-smoke-navy',27),@('transport','--ui-smoke-transport',22)
)
$durations=[Collections.Generic.List[string]]::new();$failures=[Collections.Generic.List[string]]::new()
function Run-Diagnostic($arguments,$timeout){
    $timer=[Diagnostics.Stopwatch]::StartNew();$process=Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if(-not $process.WaitForExit($timeout*1000)){Stop-Process -Id $process.Id -Force;throw "Diagnostic timeout: $arguments"}
    [pscustomobject]@{Code=$process.ExitCode;Seconds=$timer.Elapsed.TotalSeconds}
}
foreach($mode in $modes){
    if($mode[0] -in 'three-dimensional','district','military-ai','epoch','naval','transport'){
        $folder=Join-Path $PWD ('artifacts/published-'+$mode[0]+'-ui-0.31')
        $result=Run-Diagnostic @($mode[1],('"'+$folder+'"')) 90
        $report=Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
        $count=@($report | Where-Object {$_ -like 'PASS *'}).Count
        if($result.Code -ne 0 -or $count -ne $mode[2]){throw "Published diagnostic failed: $($mode[0]); exit $($result.Code); checks $count"}
        $line='{0}: {1} checks; exit 0; {2:F1}s' -f $mode[0],$count,$result.Seconds;$durations.Add($line);Write-Output $line
        $durations | Set-Content -LiteralPath artifacts/published-durations-0.31.txt -Encoding utf8
    }
    $folder=Join-Path $PWD ('artifacts/intentional-'+$mode[0]+'-ui-0.31')
    $result=Run-Diagnostic @($mode[1],'--smoke-force-failure',('"'+$folder+'"')) 30
    if($result.Code -ne 3 -or (Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt') -Raw) -notmatch 'Intentional'){throw "Missing expected failure: $($mode[0])"}
    $failures.Add("$($mode[1]): exit 3; intentional failure reported");$failures | Set-Content -LiteralPath artifacts/process-exit-check-0.31.txt -Encoding utf8
}
$folder=Join-Path $PWD 'artifacts/fallback-three-dimensional-ui-0.31'
$result=Run-Diagnostic @('--ui-smoke-3d','--no-gpu',('"'+$folder+'"')) 90
$report=Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
if($result.Code -ne 0 -or @($report | Where-Object {$_ -like 'PASS *'}).Count -ne 34){throw 'WPF fallback failed'}
$line='WPF fallback: 34 checks; exit 0; {0:F1}s' -f $result.Seconds;$durations.Add($line);Write-Output $line
$durations | Set-Content -LiteralPath artifacts/published-durations-0.31.txt -Encoding utf8
$folder=Join-Path $PWD 'artifacts/fallback-military-ai-ui-0.31'
$result=Run-Diagnostic @('--ui-smoke-military-ai','--no-gpu',('"'+$folder+'"')) 90
$report=Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
if($result.Code -ne 0 -or @($report | Where-Object {$_ -like 'PASS *'}).Count -ne 16){throw 'WPF military assessment failed'}
$line='WPF military assessment: 16 checks; exit 0; {0:F1}s' -f $result.Seconds;$durations.Add($line);Write-Output $line
$durations | Set-Content -LiteralPath artifacts/published-durations-0.31.txt -Encoding utf8

$folder=Join-Path $PWD 'artifacts/fallback-naval-ui-0.31'
$result=Run-Diagnostic @('--ui-smoke-navy','--no-gpu',('"'+$folder+'"')) 90
$report=Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
if($result.Code -ne 0 -or @($report | Where-Object {$_ -like 'PASS *'}).Count -ne 27){throw 'WPF navy failed'}
$line='WPF navy: 27 checks; exit 0; {0:F1}s' -f $result.Seconds;$durations.Add($line);Write-Output $line
$durations | Set-Content -LiteralPath artifacts/published-durations-0.31.txt -Encoding utf8
$folder=Join-Path $PWD 'artifacts/fallback-transport-ui-0.31'
$result=Run-Diagnostic @('--ui-smoke-transport','--no-gpu',('"'+$folder+'"')) 90
$report=Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
if($result.Code -ne 0 -or @($report | Where-Object {$_ -like 'PASS *'}).Count -ne 22){throw 'WPF transport failed'}
$line='WPF transport: 22 checks; exit 0; {0:F1}s' -f $result.Seconds;$durations.Add($line);Write-Output $line
$durations | Set-Content -LiteralPath artifacts/published-durations-0.31.txt -Encoding utf8