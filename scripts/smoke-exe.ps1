param([string]$Version = '3.0.1')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $projectRoot "dist/ViFree-$Version-win-x64/ViFree.exe"
$root = Join-Path $projectRoot ('build/exe-only-' + [Guid]::NewGuid().ToString('N'))
$appFolder = Join-Path $root 'App'
$stateFolder = Join-Path $root 'State'
New-Item -ItemType Directory -Path $appFolder, (Join-Path $root 'empty-runtime') -Force | Out-Null
$executable = Join-Path $appFolder 'ViFree.exe'
Copy-Item -LiteralPath $source -Destination $executable
$start = [Diagnostics.ProcessStartInfo]::new($executable)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = 'Hidden'
$start.WorkingDirectory = $appFolder
$start.ArgumentList.Add('--smoke-test')
$start.ArgumentList.Add($stateFolder)
$start.Environment['DOTNET_ROOT'] = Join-Path $root 'empty-runtime'
$start.Environment['DOTNET_ROOT_X64'] = Join-Path $root 'empty-runtime'
$start.Environment['DOTNET_MULTILEVEL_LOOKUP'] = '0'
$process = [Diagnostics.Process]::Start($start)
try {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $marker = Join-Path $stateFolder 'startup-ready.txt'
    while (!(Test-Path -LiteralPath $marker) -and !$process.HasExited -and $timer.Elapsed.TotalSeconds -lt 30) {
        Start-Sleep -Milliseconds 100
    }
    if (!(Test-Path -LiteralPath $marker) -or $process.HasExited) { throw 'The EXE-only client did not render successfully.' }
    if (@(Get-ChildItem -LiteralPath $appFolder -File).Count -ne 1) { throw 'The smoke test required app companions.' }
    Write-Output 'PASS: Self-contained EXE renders from an isolated folder with an empty runtime root and no companion files.'
} finally {
    if (!$process.HasExited) { $process.Kill($true); $process.WaitForExit(5000) | Out-Null }
    $process.Dispose()
}
