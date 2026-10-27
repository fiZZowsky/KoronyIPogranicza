$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$executable = Join-Path $PWD 'wydanie-0.26/KoronyIPogranicza.exe'
$modes = @(
    @('base','--ui-smoke',170,180),
    @('espionage','--ui-smoke-espionage',22,60),
    @('custody','--ui-smoke-custody',18,60),
    @('epoch','--ui-smoke-epochs',38,60),
    @('power','--ui-smoke-power',23,60),
    @('energy-case','--ui-smoke-energy-cases',22,60),
    @('three-dimensional','--ui-smoke-3d',34,60),
    @('photographic','--ui-smoke-photographic',21,60),
    @('landscape','--ui-smoke-landscape',18,60),
    @('river','--ui-smoke-rivers',19,60),
    @('environment','--ui-smoke-environment',22,60),
    @('models','--ui-smoke-models',27,60),
    @('rendering','--ui-smoke-rendering',27,60)
)
$durations = [Collections.Generic.List[string]]::new()
$failures = [Collections.Generic.List[string]]::new()
function Run-Diagnostic($arguments, $timeout) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit($timeout * 1000)) {
        Stop-Process -Id $process.Id -Force
        throw "Diagnostic timed out: $arguments"
    }
    [pscustomobject]@{ Code=$process.ExitCode; Seconds=$timer.Elapsed.TotalSeconds }
}
foreach ($mode in $modes) {
    $folder = Join-Path $PWD ('artifacts/published-' + $mode[0] + '-ui-0.26')
    $result = Run-Diagnostic @($mode[1], ('"' + $folder + '"')) $mode[3]
    if ($result.Code -ne 0) { throw "Published diagnostic failed: $($mode[0]), exit $($result.Code)" }
    $report = Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
    $count = @($report | Where-Object { $_ -like 'PASS *' }).Count
    if ($count -ne $mode[2]) { throw "Unexpected check count for $($mode[0]): $count" }
    $line = '{0}: {1} checks; exit {2}; {3:F1}s' -f $mode[0],$count,$result.Code,$result.Seconds
    $durations.Add($line)
    $durations | Set-Content -LiteralPath artifacts/published-durations-0.26.txt -Encoding utf8
    Write-Output $line
    $negativeFolder = Join-Path $PWD ('artifacts/intentional-' + $mode[0] + '-ui-0.26')
    $negative = Run-Diagnostic @($mode[1],'--smoke-force-failure',('"' + $negativeFolder + '"')) 30
    if ($negative.Code -ne 3) { throw "Expected exit 3 for $($mode[0]), got $($negative.Code)" }
    $negativeReport = Get-Content -LiteralPath (Join-Path $negativeFolder 'ui-smoke.txt') -Raw
    if ($negativeReport -notmatch 'Intentional') { throw "Missing intentional failure evidence for $($mode[0])" }
    $failures.Add("$($mode[1]): exit 3; intentional failure reported")
    $failures | Set-Content -LiteralPath artifacts/process-exit-check-0.26.txt -Encoding utf8
}
$folder = Join-Path $PWD 'artifacts/fallback-three-dimensional-ui-0.26'
$result = Run-Diagnostic @('--ui-smoke-3d','--no-gpu',('"' + $folder + '"')) 90
$report = Get-Content -LiteralPath (Join-Path $folder 'ui-smoke.txt')
if ($result.Code -ne 0 -or @($report | Where-Object { $_ -like 'PASS *' }).Count -ne 34) { throw 'WPF fallback failed' }
$durations.Add(('WPF fallback: 34 checks; exit 0; {0:F1}s' -f $result.Seconds))
$durations | Set-Content -LiteralPath artifacts/published-durations-0.26.txt -Encoding utf8
Write-Output $durations[-1]
