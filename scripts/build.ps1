param([string]$Version = '1.0.8',[string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $projectRoot 'dist'
$package = if([string]::IsNullOrWhiteSpace($PackageDirectory)) { Join-Path $dist "YouTubeDownloader-$Version-windows-x64" } else { [IO.Path]::GetFullPath($PackageDirectory) }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Install .NET Framework 4.8 before building.' }
New-Item -ItemType Directory -Path $package -Force | Out-Null
$source = Join-Path $projectRoot 'src'
$assets = Join-Path $projectRoot 'assets'
$executable = Join-Path $package 'YouTubeDownloader.exe'
& $compiler /nologo /codepage:65001 /target:winexe /platform:x64 "/out:$executable" "/win32icon:$(Join-Path $assets 'app.ico')" "/resource:$(Join-Path $assets 'header-logo.png'),YouTubeDownloader.Logo" "/resource:$(Join-Path $assets 'app.ico'),YouTubeDownloader.Icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $source 'Core.cs') (Join-Path $source 'App.cs') (Join-Path $source 'Theme.cs') (Join-Path $source 'Texts.cs') (Join-Path $source 'Updates.cs') (Join-Path $source 'AssemblyInfo.cs')
if ($LASTEXITCODE -ne 0) { throw 'The app build failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SetupTools.cmd') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $package 'README.txt') -Force
$zip = Join-Path $dist "YouTubeDownloader-v$Version-windows-x64.zip"
$files = @('YouTubeDownloader.exe','SetupTools.cmd','README.txt') | ForEach-Object { Join-Path $package $_ }
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
Get-FileHash -LiteralPath $zip -Algorithm SHA256 | Select-Object Hash,@{Name='File';Expression={Split-Path -Leaf $_.Path}} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dist 'checksums.json') -Encoding UTF8
Write-Output "Built $executable"
Write-Output "Packaged $zip"
