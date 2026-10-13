param([string]$Version='3.0.0')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $projectRoot 'build/dotnet-home'
$env:NUGET_PACKAGES=Join-Path $projectRoot 'build/packages'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'
if($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a three-part version.' }
$package=Join-Path $projectRoot "dist/ViFree-$Version-win-x64"
New-Item -ItemType Directory -Path $package -Force | Out-Null
& dotnet publish (Join-Path $projectRoot 'src/Downloader.Desktop/Downloader.Desktop.csproj') -c Release -r win-x64 --self-contained true --disable-build-servers -p:Version=$Version -p:AssemblyVersion="$Version.0" -p:FileVersion="$Version.0" -o $package
if($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD-PARTY-NOTICES.txt') -Destination $package -Force
$instructions=@'
ViFree by Ziad Hassanein
Windows 10/11 x64. Extract this ZIP into a writable folder and open ViFree.exe.
No separate .NET runtime, installer, WinGet or tool setup script is needed.
On the first pasted link, the app checks compatible installed media tools and privately prepares only missing tools. Internet access and approximately 400 MB of free space may be required. Progress appears in the app. Global installations are never modified.
Updates are manual: choose Check for updates and then Install when a newer verified desktop package is available. Copies of YouTube Downloader require one manual ZIP download to switch to ViFree. Existing preferences and downloaded files are retained.
Preferences are kept in LocalAppData/YouTubeDownloader. To import relocated v1 settings, choose Import settings. Existing settings and media are retained. Links are not saved.
Only download content you have permission to save. Protected and sign-in-only content is not supported. YouTube limits can still occur.
Source and releases: https://github.com/ZiadHassanein/ViFree
'@
[IO.File]::WriteAllText((Join-Path $package 'README.txt'),$instructions,[Text.UTF8Encoding]::new($false))
$files=@('ViFree.exe','README.txt','THIRD-PARTY-NOTICES.txt')
$manifest=@{Version=$Version;Architecture='win-x64';Files=@{}}
foreach($name in $files) { $manifest.Files[$name]=(Get-FileHash -LiteralPath (Join-Path $package $name) -Algorithm SHA256).Hash }
[IO.File]::WriteAllText((Join-Path $package 'package.json'),($manifest | ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
$zip=Join-Path $projectRoot "dist/ViFree-v$Version-win-x64.zip"
Compress-Archive -LiteralPath (($files+@('package.json')) | ForEach-Object { Join-Path $package $_ }) -DestinationPath $zip -Force
$sum=@{File=(Split-Path -Leaf $zip);Hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash}
[IO.File]::WriteAllText((Join-Path $projectRoot 'dist/checksums-desktop.json'),($sum | ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output "Packaged $zip"
