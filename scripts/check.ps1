param([string]$CaptureDirectory)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $projectRoot 'build/dotnet-home'
$env:NUGET_PACKAGES=Join-Path $projectRoot 'build/packages'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
foreach($project in @('Downloader.ProcessFixture','Downloader.Tests','Downloader.WpfTests')) {
    & dotnet build (Join-Path $projectRoot "tests/$project/$project.csproj") -c Release --disable-build-servers -m:1
    if($LASTEXITCODE -ne 0) { throw "$project build failed." }
}
$fixture=Join-Path $projectRoot 'tests/Downloader.ProcessFixture/bin/Release/net10.0-windows'
& (Join-Path $projectRoot 'tests/Downloader.Tests/bin/Release/net10.0-windows/Downloader.Tests.exe') $fixture
if($LASTEXITCODE -ne 0) { throw 'Core or infrastructure checks failed.' }
if(!$CaptureDirectory) { $CaptureDirectory=Join-Path $projectRoot 'build/ui-captures' }
& (Join-Path $projectRoot 'tests/Downloader.WpfTests/bin/Release/net10.0-windows/win-x64/Downloader.WpfTests.exe') $CaptureDirectory
if($LASTEXITCODE -ne 0) { throw 'WPF checks failed.' }
& node --test (Join-Path $projectRoot 'tests/browser.test.cjs') (Join-Path $projectRoot 'tests/browser-video.test.cjs') (Join-Path $projectRoot 'tests/docs.test.cjs')
if($LASTEXITCODE -ne 0) { throw 'Browser companion checks failed.' }
