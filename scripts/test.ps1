$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$build = Join-Path $projectRoot 'build'
New-Item -ItemType Directory -Path $build -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testExe = Join-Path $build 'CoreTests.exe'
& $compiler /nologo /target:exe "/out:$testExe" /reference:System.Web.Extensions.dll (Join-Path $projectRoot 'src\Core.cs') (Join-Path $projectRoot 'tests\CoreTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'The test build failed.' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
$interfaceExe = Join-Path $build 'InterfaceChecks.exe'
& $compiler /nologo /target:exe /main:InterfaceChecks "/out:$interfaceExe" /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $projectRoot 'src\Core.cs') (Join-Path $projectRoot 'src\App.cs') (Join-Path $projectRoot 'src\Theme.cs') (Join-Path $projectRoot 'src\Updates.cs') (Join-Path $projectRoot 'tests\InterfaceChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'The interface check build failed.' }
& $interfaceExe
if ($LASTEXITCODE -ne 0) { throw 'Interface checks failed.' }
& (Join-Path $PSScriptRoot 'test-updater.ps1')
