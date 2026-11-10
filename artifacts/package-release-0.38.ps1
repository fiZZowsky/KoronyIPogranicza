$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$release = Join-Path $PWD 'wydanie-0.38'
$files = @(Get-ChildItem -LiteralPath $release -File | Where-Object {$_.Name -notin 'settings.json','settings.json.tmp'} | Sort-Object Name)
if ($files.Count -ne 14) { throw "Expected 14 release files; got $($files.Count)" }
if ($files | Where-Object { $_.Extension -in '.crown','.bak','.tmp' }) { throw 'Unexpected campaign or temporary file' }
foreach ($binary in @('KoronyIPogranicza.exe','KoronyIPogranicza.dll','Crown.Core.dll')) {
    $source = Join-Path $PWD ('src/Crown.Desktop/bin/Release/net10.0-windows/' + $binary)
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath (Join-Path $release $binary)).Hash) { throw "Binary mismatch: $binary" }
}
$baselineRows = Get-Content -LiteralPath artifacts/autosave-baselines-0.38.json -Raw | ConvertFrom-Json
foreach ($row in $baselineRows) {
    if ((Get-FileHash -LiteralPath $row.Path).Hash -ne $row.Hash) { throw "Autosave changed: $($row.Version)" }
}
$baseline = ($baselineRows | Where-Object Version -eq '0.37').Hash
if ((Get-FileHash -LiteralPath (Join-Path $release 'saves/autosave.crown')).Hash -ne $baseline) { throw 'Copied autosave differs from 0.37' }
$previous = @{
    '0.37'='FCD6CC4F7511DCD40C12A953E1981EAAD1335FEC6D4BC8A4B2773A86AC4103A2'
    '0.36'='1F4EC0F4DCA4315DD0ADA76FB293697A201591950E7B542AA9F26F0E4B528CD0'
    '0.35'='1EC03938279A8B274DDBEE76CB20D5E68B613A1F43701F7496CDAFF912DDEBFC'
    '0.34'='9E9CAF16C6DB4C1670EA93E3CA7E73F372AE17AAF86C987F9BF8A3F28000B18A'
    '0.33'='08B91AD38AB260EA5604737A8703EBA8C873D5A8D00824F2F3010731302CEF52'
    '0.32'='412581FA3C6899937CA85F9A02595DE67A923DFBE98AF824E03F69787805837A'
    '0.31'='F7813664410A5A2642E2E29E9F25EDC1DD761FE3A97C370D4949B63F72420AA4'
    '0.30'='722178CFB04D8AF8AF6A78724E6F9373620BB818293899EFA0D1767523CD82F4'
    '0.29'='5139E662D4D7F48509C8077EED935C5D5E72E4F20972C603620E058EBD2F2599'
    '0.28'='7C34472A0F731A25F47D4BC75EFD97805EE53C2EA035B1106626A32DFD9380DF'
    '0.27'='31891CEEA4E69077C7050A66DCE2B0830A751B427F97FAE67D4556422C14F25F'
    '0.26'='3C7B205DF57A68C445599D4FC1C8A939836D274B5DC24651B0169C221DD37CFA'
    '0.25'='8905950E7C55D513E7E3378B34156B8BCECF38CE1FF8D5E608C56C22B08E856A'
    '0.24'='8ABB55029A18ACBF1B3C37C1FA0DEEC41F59F33843996C7D054CF49E132793B3'
    '0.23'='595DA855BCAF5EE19A1D0674A1B319343BCE87B9047A209C1FB71841A1180DDB'
    '0.22'='7CC646B765DB69E555629E4584586FC9C403936604DF8A3FD51D6C37CAE46D39'
}
foreach ($version in $previous.Keys) {
    if ((Get-FileHash -LiteralPath "artifacts/Korony-i-Pogranicza-$version-Windows.zip").Hash -ne $previous[$version]) { throw "Older ZIP changed: $version" }
}
Add-Type -AssemblyName System.IO.Compression
$zipPath = Join-Path $PWD 'artifacts/Korony-i-Pogranicza-0.38-Windows.zip'
$stream = [IO.File]::Open($zipPath,[IO.FileMode]::CreateNew)
$archive = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $entry = $archive.CreateEntry('wydanie-0.38/' + $file.Name,[IO.Compression.CompressionLevel]::Optimal)
        $output = $entry.Open(); $input = $file.OpenRead()
        try { $input.CopyTo($output) } finally { $input.Dispose(); $output.Dispose() }
    }
} finally { $archive.Dispose(); $stream.Dispose() }
$inputZip = [IO.File]::OpenRead($zipPath)
$readArchive = [IO.Compression.ZipArchive]::new($inputZip,[IO.Compression.ZipArchiveMode]::Read)
try {
    if ($readArchive.Entries.Count -ne 14) { throw 'Unexpected ZIP entry count' }
    foreach ($entry in $readArchive.Entries) {
        if ($entry.FullName -ne 'wydanie-0.38/' + $entry.Name) { throw 'Unexpected ZIP path' }
        $sourceHash = (Get-FileHash -LiteralPath (Join-Path $release $entry.Name)).Hash
        $entryStream = $entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
        try { $zipHash = [Convert]::ToHexString($sha.ComputeHash($entryStream)) } finally { $sha.Dispose(); $entryStream.Dispose() }
        if ($sourceHash -ne $zipHash) { throw "ZIP contents mismatch: $($entry.Name)" }
    }
} finally { $readArchive.Dispose(); $inputZip.Dispose() }
$info = Get-Item -LiteralPath $zipPath
@(
    'Korony i Pogranicza 0.38; Windows; framework-dependent .NET 10 Desktop',
    "Bytes: $($info.Length)",
    'Entries: 14; all under wydanie-0.38/; every file SHA256 matches release',
    "SHA256: $((Get-FileHash -LiteralPath $zipPath).Hash)",
    'No saves, personal settings, audio cache, backups or temporary files included.',
    'EXE, Desktop DLL and Crown.Core DLL match final source build.',
    "Previous autosaves match the per-file snapshot; the 0.38 copy matches 0.37: $baseline",
    'Older ZIPs 0.37, 0.36, 0.35, 0.34, 0.33, 0.32, 0.31, 0.30, 0.29, 0.28, 0.27, 0.26, 0.25, 0.24, 0.23 and 0.22 match recorded SHA256 baselines.'
) | Set-Content -LiteralPath artifacts/package-0.38.txt -Encoding utf8
Get-Content -LiteralPath artifacts/package-0.38.txt
