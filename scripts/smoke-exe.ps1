param([string]$Version = '4.1.0')
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
$bundleFolder=Join-Path $root 'runtime-bundle'
$traceFile=Join-Path $root 'host-trace.txt'
$start.Environment['DOTNET_BUNDLE_EXTRACT_BASE_DIR']=$bundleFolder
$start.Environment['DOTNET_HOST_TRACE']='1'
$start.Environment['DOTNET_HOST_TRACEFILE']=$traceFile
$process = [Diagnostics.Process]::Start($start)
try {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $marker = Join-Path $stateFolder 'startup-ready.txt'
    while (!(Test-Path -LiteralPath $marker) -and !$process.HasExited -and $timer.Elapsed.TotalSeconds -lt 30) {
        Start-Sleep -Milliseconds 100
    }
    if (!(Test-Path -LiteralPath $marker) -or $process.HasExited) { throw 'The EXE-only client did not render successfully.' }
    if (@(Get-ChildItem -LiteralPath $appFolder -File).Count -ne 1) { throw 'The smoke test required app companions.' }
    $runtimeModule=@($process.Modules | Where-Object ModuleName -eq 'coreclr.dll')[0]
    if (!(Test-Path -LiteralPath $traceFile)) { throw 'The .NET host trace is missing.' }
    # The host keeps its trace open; finish our own smoke process before reading its receipt.
    $process.Kill($true)
    $process.WaitForExit(5000) | Out-Null
    $trace=[IO.File]::ReadAllText($traceFile)
    $extractedRuntime=$runtimeModule -and $runtimeModule.FileName.StartsWith($bundleFolder+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)
    $internalRuntime= !$runtimeModule -and $trace.Contains("CoreCLR path = '', CoreCLR dir = ''") -and $trace.Contains('Using internal fxr') -and $trace.Contains('Using internal hostpolicy')
    if (!$trace.Contains('Executing as a self-contained app') -or $trace.Contains('Executing as a framework-dependent app') -or !($extractedRuntime -or $internalRuntime)) { throw 'The app did not use its bundled runtime.' }
    Write-Output 'PASS: EXE-only startup renders using the self-contained runtime embedded in its package.'
    Write-Output "Runtime proof: $traceFile"
} finally {
    if (!$process.HasExited) { $process.Kill($true); $process.WaitForExit(5000) | Out-Null }
    $process.Dispose()
}
