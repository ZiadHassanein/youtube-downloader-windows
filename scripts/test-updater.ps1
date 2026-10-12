$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$fixture = Join-Path $root ('build\updater-' + [Guid]::NewGuid().ToString('N'))
$stage = Join-Path $fixture 'stage'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$target = Join-Path $fixture 'UpdateHarness.exe'
& $compiler /nologo /target:exe /main:UpdateHarness "/out:$target" /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $root 'src\Updates.cs') (Join-Path $root 'tests\UpdateHarness.cs')
if ($LASTEXITCODE -ne 0) { throw 'Update harness build failed.' }
& $compiler /nologo /target:winexe "/out:$(Join-Path $stage 'YouTubeDownloader.exe')" (Join-Path $root 'tests\UpdateTarget.cs')
if ($LASTEXITCODE -ne 0) { throw 'Update target build failed.' }
@('SetupTools.cmd','README.txt','update.zip') | ForEach-Object { 'fixture' | Set-Content -LiteralPath (Join-Path $stage $_) }
$preferences = '{"Theme":"Dark","Recent":["saved-video.mp4"]}'
$preferences | Set-Content -LiteralPath (Join-Path $fixture 'settings.json')
'keep' | Set-Content -LiteralPath (Join-Path $fixture 'saved-video.mp4')
$before = (Get-FileHash -LiteralPath $target).Hash
& $target $stage
$marker = Join-Path $fixture 'restarted.txt'
for ($attempt=0; $attempt -lt 80 -and -not (Test-Path -LiteralPath $marker); $attempt++) { Start-Sleep -Milliseconds 250 }
if (-not (Test-Path -LiteralPath $marker)) { throw 'Updated app did not restart.' }
if ((Get-FileHash -LiteralPath $target).Hash -eq $before) { throw 'App executable was not replaced.' }
if ((Get-Content -LiteralPath (Join-Path $fixture 'settings.json') -Raw).Trim() -ne $preferences) { throw 'Preferences changed during update.' }
if ((Get-Content -LiteralPath (Join-Path $fixture 'saved-video.mp4') -Raw).Trim() -ne 'keep') { throw 'Saved media changed during update.' }
Write-Output 'PASS: Update replaces the app, restarts it, and preserves preferences and saved media.'
